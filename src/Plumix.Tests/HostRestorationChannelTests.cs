using Plumix.UI;
using Xunit;

// C#-only host tests: Flutter's RestorationChannelTest.java and FlutterRestorationPluginTest.mm.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class HostRestorationChannelTests
{
    private readonly PlatformBinaryMessenger _messenger = new();

    [Fact]
    public void Android_DataBeforeGetIsCachedWithoutPushAndSurvivesRetrieval()
    {
        int pushes = 0;
        Channel.SetMethodCallHandler(_ =>
        {
            pushes++;
            return Task.FromResult<object?>(null);
        });
        var host = new AndroidRestorationChannel(true, _messenger);
        byte[] bytes = [1, 2, 3];
        host.SetRestorationData(bytes);

        Assert.Equal(0, pushes);
        AssertData(Get(), true, bytes, hasData: true);
        Assert.Equal(bytes, host.RestorationData);
        AssertData(Get(), true, bytes, hasData: true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Android_WaitingGetIsReleasedEvenWhenTheActivityHasNoSavedData(bool hasData)
    {
        var host = new AndroidRestorationChannel(true, _messenger);
        Task<object?> pending = Get();
        Assert.False(pending.IsCompleted);

        byte[]? data = hasData ? [1, 2, 3] : null;
        host.SetRestorationData(data);
        EventLoop.FlushMicrotasks();

        AssertData(pending, true, data, hasData: true);
        Assert.Equal(data, host.RestorationData);
    }

    [Fact]
    public void Android_PushChangesCacheOnlyAfterSuccessAcknowledgment()
    {
        var host = new AndroidRestorationChannel(false, _messenger);
        AssertData(Get(), true, null, hasData: true);
        var acknowledgment = new TaskCompletionSource<object?>();
        byte[] bytes = [3, 2, 1];
        Channel.SetMethodCallHandler(call =>
        {
            Assert.Equal("push", call.Method);
            var arguments = Assert.IsType<Dictionary<object, object?>>(call.Arguments);
            Assert.True((bool)arguments["enabled"]!);
            Assert.Equal(bytes, Assert.IsType<byte[]>(arguments["data"]));
            return acknowledgment.Task;
        });

        host.SetRestorationData(bytes);
        Assert.Null(host.RestorationData);
        acknowledgment.SetResult(null);
        EventLoop.FlushMicrotasks();
        Assert.Equal(bytes, host.RestorationData);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Android_FailedOrUnimplementedPushRetainsPreviousCache(bool error)
    {
        var host = new AndroidRestorationChannel(false, _messenger);
        byte[] original = [1];
        host.SetRestorationData(original);
        Get();
        Channel.SetMethodCallHandler(_ => throw (error
            ? new PlatformException("failed", "cannot restore")
            : new MissingPluginException()));

        host.SetRestorationData([2]);
        EventLoop.FlushMicrotasks();
        Assert.Equal(original, host.RestorationData);
    }

    [Fact]
    public void Android_PutUpdatesCacheAndClearDataDoesNotReenableWaiting()
    {
        var host = new AndroidRestorationChannel(true, _messenger);
        host.SetRestorationData(null);
        Put([1, 2]);
        Assert.Equal(new byte[] { 1, 2 }, host.RestorationData);
        host.ClearData();

        Assert.Null(host.RestorationData);
        AssertData(Get(), true, null, hasData: true);
        Assert.Throws<MissingPluginException>(() => Invoke("unknown").GetAwaiter().GetResult());
    }

    [Fact]
    public void IOS_DisabledRespondsImmediatelyAndOmitsData()
    {
        var host = new IOSRestorationPlugin(false, _messenger);
        host.RestorationData = [1, 2];
        AssertData(Get(), false, null, hasData: false);
    }

    [Fact]
    public void IOS_EnabledWaitsForDataAndCopiesTheNativeBuffer()
    {
        var host = new IOSRestorationPlugin(true, _messenger);
        Task<object?> pending = Get();
        Assert.False(pending.IsCompleted);
        byte[] source = [1, 2, 3];
        host.RestorationData = source;
        source[0] = 4;
        EventLoop.FlushMicrotasks();

        AssertData(pending, true, [1, 2, 3], hasData: true);
        AssertData(Get(), true, [1, 2, 3], hasData: true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IOS_CompletionWithoutSavedDataReleasesAndNeverDelaysAnotherGet(bool getFirst)
    {
        var host = new IOSRestorationPlugin(true, _messenger);
        Task<object?>? pending = getFirst ? Get() : null;
        host.MarkRestorationComplete();
        EventLoop.FlushMicrotasks();

        AssertData(pending ?? Get(), true, null, hasData: false);
        AssertData(Get(), true, null, hasData: false);
    }

    [Fact]
    public void IOS_PutAndResetPreserveThePluginProtocol()
    {
        var host = new IOSRestorationPlugin(true, _messenger);
        host.MarkRestorationComplete();
        Put([4, 5]);
        Assert.Equal(new byte[] { 4, 5 }, host.RestorationData);
        AssertData(Get(), true, [4, 5], hasData: true);

        host.Reset();
        Assert.Null(host.RestorationData);
        AssertData(Get(), true, null, hasData: false);
        Assert.Throws<MissingPluginException>(() => Invoke("unknown").GetAwaiter().GetResult());
    }

    [Fact]
    public void IOS_ResetDropsAnOldPendingRequestBeforeHotRestart()
    {
        var host = new IOSRestorationPlugin(true, _messenger);
        Task<object?> old = Get();
        host.Reset();
        host.MarkRestorationComplete();
        EventLoop.FlushMicrotasks();

        Assert.False(old.IsCompleted);
        AssertData(Get(), true, null, hasData: false);
    }

    private MethodChannel Channel => new(SystemChannels.Restoration.Name, binaryMessenger: _messenger);

    private Task<object?> Get() => Invoke("get");

    private void Put(byte[] data)
    {
        Task<object?> response = Invoke("put", data);
        Assert.True(response.IsCompletedSuccessfully);
        Assert.Null(response.Result);
    }

    private Task<object?> Invoke(string method, object? arguments = null)
    {
        using EventLoop.FrameworkThreadScope scope = EventLoop.EnterFrameworkThread();
        return Channel.InvokeMethod<object>(method, arguments);
    }

    private static void AssertData(Task<object?> response, bool enabled, byte[]? bytes, bool hasData)
    {
        Assert.True(response.IsCompletedSuccessfully);
        var result = Assert.IsType<Dictionary<object, object?>>(response.Result);
        Assert.Equal(enabled, result["enabled"]);
        Assert.Equal(hasData, result.ContainsKey("data"));
        if (hasData)
        {
            Assert.Equal(bytes, result["data"] as byte[]);
        }
    }
}
