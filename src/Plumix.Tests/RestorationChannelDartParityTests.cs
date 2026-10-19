using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/services/restoration_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class RestorationChannelDartParityTests : IDisposable
{
    private readonly PlatformBinaryMessenger _messenger = new();

    public RestorationChannelDartParityTests() => SchedulerBinding.ResetForTests();

    public void Dispose() => SchedulerBinding.ResetForTests();

    [Fact]
    public void RootBucket_SharesOneAsyncGetThenReturnsSynchronously()
    {
        int calls = 0;
        Channel.SetPlatformMethodCallHandler(call =>
        {
            Assert.Equal("get", call.Method);
            calls++;
            return Task.FromResult<object?>(Update(EncodedValue(10)));
        });
        using var manager = new RestorationManager(_messenger);

        Task<RestorationBucket?> first = manager.RootBucket;
        Assert.Same(first, manager.RootBucket);
        Assert.False(first.IsCompleted);
        Assert.Equal(1, calls);

        EventLoop.FlushMicrotasks();
        Assert.True(first.IsCompletedSuccessfully);
        Assert.Equal(10, Completed(first)!.Read<int>("value"));
        Assert.True(manager.RootBucket.IsCompletedSuccessfully);
        Assert.Same(Completed(first), Completed(manager.RootBucket));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void PushWhileGetIsPending_WinsOverLateEngineResponse()
    {
        var reply = new TaskCompletionSource<object?>();
        Channel.SetPlatformMethodCallHandler(_ => reply.Task);
        using var manager = new RestorationManager(_messenger);
        Task<RestorationBucket?> pending = manager.RootBucket;
        int notifications = 0;
        manager.AddListener(() => notifications++);

        Push(Update(EncodedValue(33)));
        Assert.True(pending.IsCompletedSuccessfully);
        RestorationBucket root = Completed(pending)!;
        Assert.Equal(33, root.Read<int>("value"));
        Assert.False(manager.IsReplacing);

        reply.SetResult(Update(EncodedValue(10)));
        EventLoop.FlushMicrotasks();

        Assert.Same(root, Completed(manager.RootBucket));
        Assert.Equal(33, root.Read<int>("value"));
        Assert.Equal(1, notifications);
        Assert.False(manager.IsReplacing);
    }

    [Fact]
    public void PushBeforeQueuedInlineGetResponse_AlsoWins()
    {
        Channel.SetPlatformMethodCallHandler(_ => Task.FromResult<object?>(Update(EncodedValue(10))));
        using var manager = new RestorationManager(_messenger);
        Task<RestorationBucket?> pending = manager.RootBucket;

        Push(Update(EncodedValue(33)));
        EventLoop.FlushMicrotasks();

        Assert.Equal(33, Completed(pending)!.Read<int>("value"));
        Assert.Same(Completed(pending), Completed(manager.RootBucket));
    }

    [Fact]
    public void PendingCallback_RunsAfterListenersAndKeepsItsCompletionValueUnderReentrantPush()
    {
        Channel.SetPlatformMethodCallHandler(_ => new TaskCompletionSource<object?>().Task);
        using var manager = new RestorationManager(_messenger);
        var log = new List<string>();
        RestorationBucket? delivered = null;
        RestorationBucket? firstRoot = null;
        manager.GetRootBucket(bucket =>
        {
            log.Add("callback");
            delivered = bucket;
        });
        bool replaced = false;
        manager.AddListener(() =>
        {
            log.Add("listener");
            if (!replaced)
            {
                replaced = true;
                firstRoot = Completed(manager.RootBucket);
                Assert.Equal(10, firstRoot!.Read<int>("value"));
                Push(Update(EncodedValue(33)));
            }
        });

        Push(Update(EncodedValue(10)));
        Assert.Null(delivered);
        Assert.Equal(["listener", "listener"], log);
        EventLoop.FlushMicrotasks();

        Assert.Equal(["listener", "listener", "callback"], log);
        Assert.Same(firstRoot, delivered);
        Assert.NotSame(delivered, Completed(manager.RootBucket));
        Assert.Equal(33, Completed(manager.RootBucket)!.Read<int>("value"));
    }

    [Fact]
    public void Awaiters_RunAfterSynchronousListenersAndBeforeTheirLaterMicrotasks()
    {
        Channel.SetPlatformMethodCallHandler(_ => Task.FromResult<object?>(Update(null)));
        using var manager = new RestorationManager(_messenger);
        var log = new List<string>();
        EventLoop.RunAsync(async () =>
        {
            await manager.RootBucket;
            log.Add("awaiter");
        });
        manager.AddListener(() =>
        {
            log.Add("listener");
            EventLoop.ScheduleMicrotask(() => log.Add("listener microtask"));
        });

        Assert.Empty(log);
        EventLoop.FlushMicrotasks();
        Assert.Equal(["listener", "awaiter", "listener microtask"], log);
    }

    [Fact]
    public void MissingPlugin_ResolvesDisabledAndNeverSerializes()
    {
        using var manager = new RestorationManager(_messenger);
        int notifications = 0;
        manager.AddListener(() => notifications++);
        Task<RestorationBucket?> pending = manager.RootBucket;
        EventLoop.FlushMicrotasks();

        Assert.True(pending.IsCompletedSuccessfully);
        Assert.Null(Completed(pending));
        Assert.Equal(0, notifications);
        Assert.True(manager.RootBucket.IsCompletedSuccessfully);
        Assert.Null(Completed(manager.RootBucket));
    }

    [Fact]
    public void Put_SerializesTheBucketAtFrameEndAndFlushDoesNotSendTwice()
    {
        var puts = new List<byte[]>();
        Channel.SetPlatformMethodCallHandler(call =>
        {
            if (call.Method == "put")
            {
                puts.Add(Assert.IsType<byte[]>(call.Arguments));
            }

            return Task.FromResult<object?>(call.Method == "get" ? Update(null) : null);
        });
        using var manager = new RestorationManager(_messenger);
        Task<RestorationBucket?> pending = manager.RootBucket;
        EventLoop.FlushMicrotasks();
        Completed(pending)!.Write("value", 42);
        SchedulerBinding.Instance.ScheduleFrame();
        manager.FlushData();
        Assert.Empty(puts);

        SchedulerBinding.PumpFrameForTests();
        var raw = Assert.IsType<Dictionary<object, object?>>(
            new StandardMessageCodec().DecodeMessage(ByteData.SublistView(Assert.Single(puts))));
        Assert.Equal(42, RawRestorationData.Values(raw)!["value"]);
        manager.FlushData();
        SchedulerBinding.PumpFrameForTests();
        Assert.Single(puts);
    }

    [Fact]
    public void ServicesBinding_OwnsItsManagerAndKeepsItsMessengerWhenAmbientBindingChanges()
    {
        ServicesBinding previous = ServicesBinding.Instance;
        var first = new ServicesBinding();
        var second = new ServicesBinding();
        try
        {
            ServicesBinding.Instance = first;
            Assert.Same(first.RestorationManager, RestorationManager.Instance);
            var host = new AndroidRestorationChannel(false, first.DefaultBinaryMessenger);
            host.SetRestorationData(EncodedValue(17));
            ServicesBinding.Instance = second;
            Task<RestorationBucket?> root = first.RestorationManager.RootBucket;
            EventLoop.FlushMicrotasks();
            Assert.Equal(17, Completed(root)!.Read<int>("value"));
            Assert.Same(second.RestorationManager, RestorationManager.Instance);
        }
        finally
        {
            first.RestorationManager.Dispose();
            second.RestorationManager.Dispose();
            ServicesBinding.Instance = previous;
        }
    }

    private static T Completed<T>(Task<T> task)
    {
        Assert.True(task.IsCompletedSuccessfully, "The future must have completed in the current microtask turn.");
        return task.GetAwaiter().GetResult();
    }

    private MethodChannel Channel => new(SystemChannels.Restoration.Name, binaryMessenger: _messenger);

    private void Push(object? update)
    {
        ByteData? reply = null;
        Task sent = _messenger.HandlePlatformMessage(
            Channel.Name,
            Channel.Codec.EncodeMethodCall(new MethodCall("push", update)),
            value => reply = value);
        Assert.True(sent.IsCompletedSuccessfully);
        Assert.NotNull(reply);
        Assert.Null(Channel.Codec.DecodeEnvelope(reply));
    }

    internal static Dictionary<string, object?> Update(byte[]? data) => new()
    {
        ["enabled"] = true,
        ["data"] = data,
    };

    internal static byte[] EncodedValue(int value) => new StandardMessageCodec().EncodeMessage(
        RawRestorationData.Build(values: new Dictionary<object, object?> { ["value"] = value }))!.ToUint8List();
}
