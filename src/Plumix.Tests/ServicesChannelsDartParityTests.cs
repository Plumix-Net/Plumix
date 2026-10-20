using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/services/binding.dart
// Flutter tests: services/channel_buffers_test.dart, services/binding_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ServicesChannelsDartParityTests : IDisposable
{
    private readonly ServicesBinding _previous = ServicesBinding.Instance;
    private readonly FlutterExceptionHandler? _onError = FlutterError.OnError;
    private readonly List<FlutterErrorDetails> _errors = [];
    private readonly ServicesBinding _binding;

    public ServicesChannelsDartParityTests()
    {
        _ = WidgetsBinding.Instance;
        EventLoop.FlushMicrotasks();
        _binding = new ServicesBinding();
        ServicesBinding.Instance = _binding;
        FlutterError.OnError = _errors.Add;
        EventLoop.FlushMicrotasks();
    }

    public void Dispose()
    {
        EventLoop.FlushMicrotasks();
        FlutterError.OnError = _onError;
        ServicesBinding.Instance = _previous;
        WidgetsBinding.Instance.ResetLifecycleStateForTests();
        SchedulerBinding.Instance.ResetInternalState();
    }

    [Fact]
    public void DefaultMessengerCompletesInjectionBeforeBufferedMessageIsRepliedTo()
    {
        var codec = new StringCodec();
        ByteData? response = null;
        bool replied = false;
        Task injected = _binding.DefaultBinaryMessenger.HandlePlatformMessage("early", codec.EncodeMessage("hello"),
            data =>
            {
                response = data;
                replied = true;
            });
        Assert.True(injected.IsCompletedSuccessfully);
        Assert.False(replied);
        var channel = new BasicMessageChannel<string?>("early", codec);
        channel.SetMessageHandler(message => Task.FromResult<string?>($"{message} world"));
        Assert.False(replied);
        EventLoop.FlushMicrotasks();
        Assert.True(replied);
        Assert.Equal("hello world", codec.DecodeMessage(response));
    }

    [Fact]
    public void HandlerFailureIsReportedAndRepliesNullThenLaterMessagesStillWork()
    {
        var messenger = (PlatformBinaryMessenger)_binding.DefaultBinaryMessenger;
        messenger.SetMessageHandler("failed", _ => throw new InvalidOperationException("broken"));
        EventLoop.FlushMicrotasks();
        bool replied = false;
        messenger.HandlePlatformMessage("failed", null, data =>
        {
            Assert.Null(data);
            replied = true;
        });
        Assert.True(replied);
        FlutterErrorDetails error = Assert.Single(_errors);
        Assert.Equal("services library", error.Library);
        Assert.Equal("during a platform message callback", error.Context!.ToDescription());
        Assert.Equal("broken", Assert.IsType<InvalidOperationException>(error.Exception).Message);
        messenger.SetMessageHandler("failed", _ => Task.FromResult<ByteData?>(new ByteData(3)));
        ByteData? reply = null;
        messenger.HandlePlatformMessage("failed", null, data => reply = data);
        Assert.Equal(3, reply!.LengthInBytes);
    }

    [Fact]
    public void AsyncHandlerFailureReturnsThroughFrameworkMicrotasks()
    {
        var completion = new TaskCompletionSource<ByteData?>();
        _binding.DefaultBinaryMessenger.SetMessageHandler("async", _ => completion.Task);
        EventLoop.FlushMicrotasks();
        bool replied = false;
        _binding.DefaultBinaryMessenger.HandlePlatformMessage("async", null, _ => replied = true);
        Assert.False(replied);
        completion.SetException(new InvalidOperationException("async failure"));
        EventLoop.FlushMicrotasks();
        Assert.True(replied);
        Assert.Single(_errors);
    }

    [Fact]
    public void DrainStartsAsyncHandlersInFifoOrderWithoutWaitingForReplies()
    {
        _binding.ChannelBuffers.Resize("async", 2);
        var first = new TaskCompletionSource<ByteData?>();
        var started = new List<int>();
        var replied = new List<int>();
        for (int i = 0; i < 2; i++)
        {
            int index = i;
            var message = new ByteData(1);
            message.SetUint8(0, (byte)i);
            _binding.DefaultBinaryMessenger.HandlePlatformMessage("async", message, _ => replied.Add(index));
        }

        _binding.DefaultBinaryMessenger.SetMessageHandler("async", data =>
        {
            int value = data!.GetUint8(0);
            started.Add(value);
            return value == 0 ? first.Task : Task.FromResult<ByteData?>(null);
        });
        EventLoop.FlushMicrotasks();
        Assert.Equal([0, 1], started);
        Assert.Equal([1], replied);
        first.SetResult(null);
        EventLoop.FlushMicrotasks();
        Assert.Equal([1, 0], replied);
    }

    [Fact]
    public void DispatcherHandlesControlMessagesAndAlwaysRepliesEvenOnMalformedControls()
    {
        var codec = new StandardMethodCodec();
        int replies = 0;
        PlatformDispatcher.Instance.DispatchPlatformMessage(ChannelBuffers.KControlChannelName,
            codec.EncodeMethodCall(new MethodCall("resize", new object[] { "early", 2 })), _ => replies++);
        Assert.Equal(1, replies);
        Assert.ThrowsAny<Exception>(() => PlatformDispatcher.Instance.DispatchPlatformMessage(
            ChannelBuffers.KControlChannelName, new ByteData(0), _ => replies++));
        Assert.Equal(2, replies);
        var log = new List<int>();
        _binding.DefaultBinaryMessenger.HandlePlatformMessage("early", new ByteData(1), _ => { });
        _binding.DefaultBinaryMessenger.HandlePlatformMessage("early", new ByteData(2), _ => { });
        _binding.DefaultBinaryMessenger.SetMessageHandler("early", data =>
        {
            log.Add(data!.LengthInBytes);
            return null;
        });
        EventLoop.FlushMicrotasks();
        Assert.Equal([1, 2], log);
    }

    [Fact]
    public void DispatcherSendsToHostAndGuardsTheResponseContext()
    {
        var local = new AsyncLocal<string?>();
        local.Value = "registered";
        _binding.DefaultBinaryMessenger.SetPlatformMessageHandler("host", data => Task.FromResult(data));
        ByteData message = new(2);
        PlatformDispatcher.Instance.SendPlatformMessage("host", message, data =>
        {
            Assert.Same(message, data);
            Assert.Equal("registered", local.Value);
            throw new InvalidOperationException("callback failure");
        });
        Assert.Equal("callback failure",
            Assert.IsType<InvalidOperationException>(Assert.Single(_errors).Exception).Message);
    }

    [Fact]
    public void AsyncHostTransportErrorsCompleteTheOutgoingFuture()
    {
        _binding.DefaultBinaryMessenger.SetPlatformMessageHandler("host",
            _ => Task.FromException<ByteData?>(new InvalidOperationException("host failed")));
        Task<ByteData?> send = _binding.DefaultBinaryMessenger.Send("host", null)!;
        Assert.True(send.IsFaulted);
        Assert.Equal("host failed", Assert.Throws<InvalidOperationException>(
            () => send.GetAwaiter().GetResult()).Message);
    }

#pragma warning disable CS0618
    [Fact]
    public void DeprecatedDispatcherListenerTakesPrecedenceExceptForBufferControlMessages()
    {
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        PlatformMessageCallback? previous = dispatcher.OnPlatformMessage;
        var log = new List<string>();
        try
        {
            dispatcher.OnPlatformMessage = (name, _, callback) =>
            {
                log.Add(name);
                callback(null);
            };
            dispatcher.DispatchPlatformMessage("intercepted", null, _ => { });
            dispatcher.DispatchPlatformMessage(ChannelBuffers.KControlChannelName,
                new StandardMethodCodec().EncodeMethodCall(
                    new MethodCall("resize", new object[] { "intercepted", 2 })), _ => { });
            Assert.Equal(["intercepted"], log);
        }
        finally
        {
            dispatcher.OnPlatformMessage = previous;
        }
    }
#pragma warning restore CS0618

    [Fact]
    public void InitialLifecycleIsLatchedOnFirstReadAndIgnoredAfterLifecycleIsKnown()
    {
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        string previous = dispatcher.InitialLifecycleState;
        WidgetsBinding.Instance.ResetLifecycleStateForTests();
        SchedulerBinding.Instance.ResetInternalState();
        try
        {
            dispatcher.ResetInitialLifecycleStateForTests("AppLifecycleState.paused");
            _binding.ReadInitialLifecycleStateFromNativeWindow();
            Assert.Equal(AppLifecycleState.Paused, WidgetsBinding.Instance.LifecycleState);
            dispatcher.UpdateInitialLifecycleState("AppLifecycleState.resumed");
            Assert.Equal("AppLifecycleState.paused", dispatcher.InitialLifecycleState);
            _binding.ReadInitialLifecycleStateFromNativeWindow();
            Assert.Equal(AppLifecycleState.Paused, WidgetsBinding.Instance.LifecycleState);
        }
        finally
        {
            dispatcher.ResetInitialLifecycleStateForTests(previous, accessed: true);
        }
    }

    [Fact]
    public void LifecycleChannelSynthesizesIntermediateTransitionsAndReturnsNull()
    {
        WidgetsBinding.Instance.ResetLifecycleStateForTests();
        var observer = new LifecycleObserver();
        WidgetsBinding.Instance.AddObserver(observer);
        try
        {
            DeliverLifecycle("AppLifecycleState.paused");
            DeliverLifecycle("AppLifecycleState.resumed");
            DeliverLifecycle("AppLifecycleState.detached");
            Assert.Equal(
                [AppLifecycleState.Paused, AppLifecycleState.Hidden, AppLifecycleState.Inactive,
                    AppLifecycleState.Resumed, AppLifecycleState.Inactive, AppLifecycleState.Hidden,
                    AppLifecycleState.Paused, AppLifecycleState.Detached], observer.States);
            Assert.False(SchedulerBinding.Instance.FramesEnabled);
            DeliverLifecycle("AppLifecycleState.resumed");
            Assert.True(SchedulerBinding.Instance.FramesEnabled);
        }
        finally
        {
            WidgetsBinding.Instance.RemoveObserver(observer);
        }
    }

    [DebugOnlyFact]
    public void EvictExtensionDispatchesToTheServicesAndPaintingBindings()
    {
        var binding = new RecordingServicesBinding();
        ServicesBinding.Instance = binding;
        ServiceExtensionCallback extension = BindingBase.RegisteredCallbacks["evict"];
        Dictionary<string, object?> result = extension(new Dictionary<string, string> { ["value"] = "test" }).Result;
        Assert.Equal("test", binding.Bundle.Evicted);
        Assert.Equal(string.Empty, result["value"]);
    }

    private void DeliverLifecycle(string message)
    {
        bool replied = false;
        _binding.DefaultBinaryMessenger.HandlePlatformMessage(SystemChannels.Lifecycle.Name,
            SystemChannels.Lifecycle.Codec.EncodeMessage(message), data =>
            {
                Assert.Null(data);
                replied = true;
            });
        Assert.True(replied);
    }

    private sealed class LifecycleObserver : WidgetsBindingObserver
    {
        public List<AppLifecycleState> States { get; } = [];
        public void DidChangeAppLifecycleState(AppLifecycleState state) => States.Add(state);
    }

    private sealed class RecordingServicesBinding : ServicesBinding
    {
        public RecordingBundle Bundle { get; } = new();
        public override AssetBundle RootBundle => Bundle;
    }

    private sealed class RecordingBundle : AssetBundle
    {
        public string? Evicted { get; private set; }
        public override Task<Stream> LoadAsync(string key, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public override void Evict(string key) => Evicted = key;
    }
}
