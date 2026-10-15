using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/binding.dart
// Mirrors widgets/binding_test.dart (observer callbacks, the "callbacks handle exceptions gracefully"
// group, navigation/back-gesture/memory-pressure channels) and binding_first_frame_rasterized_test.dart.

[Collection(SchedulerTestCollection.Name)]
public sealed class WidgetsBindingDartParityTests : IDisposable
{
    private readonly WidgetsBinding _binding = WidgetsBinding.Instance;
    private readonly List<FlutterErrorDetails> _errors = [];
    private readonly FlutterExceptionHandler? _previousOnError = FlutterError.OnError;
    private readonly Action<Action> _previousTimerRun = PlatformDispatcher.Instance.TimerRun;
    private readonly Queue<Action> _turns = [];
    private readonly List<WidgetsBindingObserver> _observers = [];

    public WidgetsBindingDartParityTests()
    {
        SchedulerBinding.ResetForTests();
        FlutterError.OnError = _errors.Add;
        PlatformDispatcher.Instance.TimerRun = _turns.Enqueue;
    }

    public void Dispose()
    {
        foreach (WidgetsBindingObserver observer in _observers)
        {
            while (_binding.RemoveObserver(observer))
            {
            }
        }

        FlutterError.OnError = _previousOnError;
        PlatformDispatcher.Instance.TimerRun = _previousTimerRun;
        _binding.ResetLifecycleStateForTests();
        SchedulerBinding.ResetForTests();
    }

    private T Observe<T>(T observer) where T : WidgetsBindingObserver
    {
        _binding.AddObserver(observer);
        _observers.Add(observer);
        return observer;
    }

    // ------------------------------------------------- callbacks handle exceptions gracefully

    public static TheoryData<string> DispatchedCallbacks =>
    [
        "didChangeMetrics",
        "didChangeTextScaleFactor",
        "didChangePlatformBrightness",
        "didChangeAppLifecycleState",
        "didChangeViewFocus",
        "didHaveMemoryPressure",
        "didRequestAppExit",
    ];

    [Theory]
    [MemberData(nameof(DispatchedCallbacks))]
    public async Task ObserverCallbacks_ReportAThrowingObserverAndKeepDispatching(string callback)
    {
        var log = new List<string>();
        Observe(new ThrowingObserver());
        Observe(new LoggingObserver(log));

        await Deliver(callback);

        Assert.Contains(callback, log);
        FlutterErrorDetails error = Assert.Single(_errors);
        Assert.Equal("widgets library", error.Library);
        Assert.Equal(
            $"while dispatching notifications for WidgetsBindingObserver.{callback}",
            error.Context!.ToString());
    }

    [Fact]
    public void RentrantObserverCallbacks_DoNotResultInExceptions()
    {
        // binding_test.dart: every dispatch iterates a snapshot of the observers.
        var observer = Observe(new RentrantObserver(_binding));

        PlatformDispatcher.Instance.OnMetricsChanged!();
        PlatformDispatcher.Instance.OnTextScaleFactorChanged!();
        PlatformDispatcher.Instance.OnPlatformBrightnessChanged!();
        _binding.HandleAccessibilityFeaturesChanged();
        _binding.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
        _binding.HandleViewFocusChanged(new ViewFocusEvent(0, ViewFocusState.Focused, ViewFocusDirection.Forward));
        _binding.HandleMemoryPressure();

        Assert.Empty(_errors);
        Assert.True(observer.RemoveSelf() > 1);
        Assert.Equal(0, observer.RemoveSelf());
    }

    [Fact]
    public async Task DidHaveMemoryPressure_IsCalledForTheMemoryPressureSystemMessage()
    {
        var log = new List<string>();
        Observe(new LoggingObserver(log));

        await SendMemoryPressure();

        Assert.Equal(["didHaveMemoryPressure"], log);
    }

    [Fact]
    public void PlatformDispatcherSlots_ReachTheWidgetsBindingHandlers()
    {
        // RendererBinding.initInstances installs the composed binding's handlers on the dispatcher.
        var log = new List<string>();
        Observe(new LoggingObserver(log));

        PlatformDispatcher.Instance.OnMetricsChanged!();
        PlatformDispatcher.Instance.OnTextScaleFactorChanged!();
        PlatformDispatcher.Instance.OnPlatformBrightnessChanged!();

        Assert.Equal(["didChangeMetrics", "didChangeTextScaleFactor", "didChangePlatformBrightness"], log);
    }

    // ------------------------------------------------------------------- back gesture

    [Fact]
    public void HandleStartBackGesture_ThroughTheChannel_ReportsAThrowingObserver()
    {
        var log = new List<string>();
        Observe(new ThrowingObserver());
        Observe(new LoggingObserver(log, tracksBackGesture: true));

        object? started = InboundBackGesture("startBackGesture", StartArguments());

        Assert.Equal(true, started);
        Assert.Equal(["handleStartBackGesture"], log);
        FlutterErrorDetails error = Assert.Single(_errors);
        Assert.Equal(
            "while dispatching notifications for WidgetsBindingObserver.handleStartBackGesture",
            error.Context!.ToString());
    }

    [Theory]
    [InlineData("updateBackGestureProgress", "handleUpdateBackGestureProgress")]
    [InlineData("commitBackGesture", "handleCommitBackGesture")]
    [InlineData("cancelBackGesture", "handleCancelBackGesture")]
    public void TrackedBackGestureCallbacks_ReportAThrowingObserverAndKeepDispatching(
        string method,
        string callback)
    {
        var log = new List<string>();
        Observe(new ThrowingObserver(tracksBackGesture: true));
        Observe(new LoggingObserver(log, tracksBackGesture: true));
        Assert.Equal(true, InboundBackGesture("startBackGesture", StartArguments()));
        _errors.Clear();
        log.Clear();

        InboundBackGesture(
            method,
            method == "updateBackGestureProgress"
                ? new Dictionary<string, object?>
                {
                    ["touchOffset"] = new List<object?> { 100.0, 300.0 },
                    ["progress"] = 0.35,
                    ["swipeEdge"] = 0,
                }
                : null);

        Assert.Equal([callback], log);
        FlutterErrorDetails error = Assert.Single(_errors);
        Assert.Equal($"while dispatching notifications for WidgetsBindingObserver.{callback}", error.Context!.ToString());
    }

    [Fact]
    public void CommitAndCancel_KeepTheTrackingObserversUntilTheNextGesture()
    {
        var log = new List<string>();
        Observe(new LoggingObserver(log, tracksBackGesture: true));
        InboundBackGesture("startBackGesture", StartArguments());
        log.Clear();

        InboundBackGesture("commitBackGesture", null);
        InboundBackGesture("cancelBackGesture", null);

        Assert.Equal(["handleCommitBackGesture", "handleCancelBackGesture"], log);
    }

    [Fact]
    public void RemoveObserver_StopsTheObserverHearingTheCurrentBackGesture()
    {
        var log = new List<string>();
        LoggingObserver observer = Observe(new LoggingObserver(log, tracksBackGesture: true));
        InboundBackGesture("startBackGesture", StartArguments());
        log.Clear();

        _binding.RemoveObserver(observer);
        InboundBackGesture("cancelBackGesture", null);

        Assert.Empty(log);
    }

    [Fact]
    public void CommitBackGesture_WithoutATrackingObserver_PopsTheRoute()
    {
        var log = new List<string>();
        Observe(new LoggingObserver(log));

        InboundBackGesture("commitBackGesture", null);

        Assert.Equal(["didPopRoute"], log);
    }

    [Fact]
    public void UnknownBackGestureMethod_IsNotImplemented()
    {
        ByteData? reply = Send(SystemChannels.BackGesture, new MethodCall("unknown"));

        Assert.Null(reply);
    }

    // ------------------------------------------------------------------ status bar

    [Fact]
    public void StatusBarScrollToTop_ReachesEveryObserver_ReportingAThrowingOne()
    {
        var log = new List<string>();
        Observe(new ThrowingObserver());
        Observe(new LoggingObserver(log));

        Send(SystemChannels.StatusBar, new MethodCall("handleScrollToTop"));

        Assert.Equal(["handleStatusBarTap"], log);
        FlutterErrorDetails error = Assert.Single(_errors);
        Assert.Equal("handling status bar action", error.Context!.ToString());
    }

    // ------------------------------------------------------------------ navigation

    [Fact]
    public void DidPushRoute_IsCalledForPushRoute()
    {
        var observer = Observe(new PushRouteObserver());

        object? handled = InboundNavigation(new MethodCall("pushRoute", "testRouteName"));

        Assert.Equal(true, handled);
        Assert.Equal("testRouteName", observer.PushedRoute);
    }

    [Fact]
    public void DidPushRouteInformation_CallsDidPushRouteByDefault()
    {
        var observer = Observe(new PushRouteObserver());

        object? handled = InboundNavigation(new MethodCall(
            "pushRouteInformation",
            new Dictionary<string, object?> { ["location"] = "testRouteName", ["state"] = "state" }));

        Assert.Equal(true, handled);
        Assert.Equal("testRouteName", observer.PushedRoute);
    }

    [Theory]
    [InlineData("http://hostname", "/")]
    [InlineData("http://hostname/abc?def=123&def=456#789", "/abc?def=123&def=456#789")]
    public void DidPushRouteInformation_CallsDidPushRouteCorrectlyWhenHandlingUrl(string location, string expected)
    {
        var observer = Observe(new PushRouteObserver());

        InboundNavigation(new MethodCall(
            "pushRouteInformation",
            new Dictionary<string, object?> { ["location"] = location }));

        Assert.Equal(expected, observer.PushedRoute);
    }

    [Fact]
    public void DidPushRouteInformation_WithNullState_ReceivesNullState()
    {
        var observer = Observe(new PushRouteInformationObserver());

        InboundNavigation(new MethodCall(
            "pushRouteInformation",
            new Dictionary<string, object?> { ["location"] = "testRouteName", ["state"] = null }));

        Assert.Equal("testRouteName", observer.PushedRouteInformation!.Uri.OriginalString);
        Assert.Null(observer.PushedRouteInformation.State);
    }

    [Fact]
    public void PushRouteAndPushRouteInformation_NotHandledByAnObserver_ReplyFalse()
    {
        Observe(new LoggingObserver([]));

        Assert.Equal(false, InboundNavigation(new MethodCall("pushRoute", "testRouteName")));
        Assert.Equal(false, InboundNavigation(new MethodCall(
            "pushRouteInformation",
            new Dictionary<string, object?> { ["location"] = "testRouteName" })));
    }

    [Fact]
    public void DidPushRouteInformation_ThroughTheChannel_ReportsAThrowingObserver()
    {
        var log = new List<string>();
        Observe(new ThrowingObserver());
        Observe(new LoggingObserver(log));

        InboundNavigation(new MethodCall(
            "pushRouteInformation",
            new Dictionary<string, object?> { ["location"] = "testRouteName" }));

        Assert.Equal(["didPushRouteInformation"], log);
        FlutterErrorDetails error = Assert.Single(_errors);
        Assert.Equal(
            "while dispatching notifications for WidgetsBindingObserver.didPushRouteInformation",
            error.Context!.ToString());
    }

    // ------------------------------------------------------------- first frame rasterized

    [Fact]
    public void DeferredFrames_TriggerTheFirstFrameCallbackOnlyOnceAFrameWasSent()
    {
        // binding_first_frame_rasterized_test.dart: 'Deferred frames will trigger the first frame
        // callback'.
        _binding.ResetRootForTests();
        _binding.ResetFirstFrameForTests();
        RendererBinding.Instance.ResetFirstFrameSent();
        List<string> events = [];
        void OnEvent(string kind, IReadOnlyDictionary<string, object?> data) => events.Add(kind);
        BindingBase.EventPosted += OnEvent;
        var host = new WidgetHost();
        try
        {
            RendererBinding.Instance.DeferFirstFrame();
            host.RunApplication(new Placeholder());
            DrainTurns();

            // Simulates the engine completing a frame render.
            PlatformDispatcher.Instance.OnReportTimings!([]);
            Assert.False(_binding.FirstFrameRasterized);
            Assert.DoesNotContain("Flutter.FirstFrame", events);

            RendererBinding.Instance.AllowFirstFrame();
            DrainTurns();

            // Simulates the engine again.
            PlatformDispatcher.Instance.OnReportTimings!([]);
            Assert.True(_binding.FirstFrameRasterized);
            Assert.True(_binding.WaitUntilFirstFrameRasterized.IsCompleted);
            Assert.Single(events, static kind => kind == "Flutter.FirstFrame");
        }
        finally
        {
            BindingBase.EventPosted -= OnEvent;
            _binding.ResetRootForTests();
            RendererBinding.Instance.ResetFirstFrameSent();
        }
    }

    [Fact]
    public async Task DidSendFirstFrameRasterizedEvent_ReportsTheRasterizedStateAsAString()
    {
        _binding.ResetFirstFrameForTests();
        ServiceExtensionCallback extension =
            BindingBase.RegisteredCallbacks[WidgetsServiceExtensions.DidSendFirstFrameRasterizedEvent.DartName()];

        Assert.Equal("false", (await extension(new Dictionary<string, string>()))["enabled"]);

        RendererBinding.Instance.ResetFirstFrameSent();
        _binding.DrawFrame();
        PlatformDispatcher.Instance.OnReportTimings!([]);

        Assert.Equal("true", (await extension(new Dictionary<string, string>()))["enabled"]);
    }

    // ------------------------------------------------------------------ helpers

    private async Task Deliver(string callback)
    {
        switch (callback)
        {
            case "didChangeMetrics":
                PlatformDispatcher.Instance.OnMetricsChanged!();
                break;
            case "didChangeTextScaleFactor":
                PlatformDispatcher.Instance.OnTextScaleFactorChanged!();
                break;
            case "didChangePlatformBrightness":
                PlatformDispatcher.Instance.OnPlatformBrightnessChanged!();
                break;
            case "didChangeAppLifecycleState":
                _binding.ResetLifecycleStateForTests();
                _binding.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
                break;
            case "didChangeViewFocus":
                _binding.HandleViewFocusChanged(
                    new ViewFocusEvent(0, ViewFocusState.Focused, ViewFocusDirection.Forward));
                break;
            case "didHaveMemoryPressure":
                await SendMemoryPressure();
                break;
            case "didRequestAppExit":
                Assert.Equal(AppExitResponse.Exit, await _binding.HandleRequestAppExit());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(callback));
        }
    }

    private static Task SendMemoryPressure() =>
        ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
            SystemChannels.System.Name,
            SystemChannels.System.Codec.EncodeMessage(
                new Dictionary<string, object?> { ["type"] = "memoryPressure" }),
            static _ => { });

    private static Dictionary<string, object?> StartArguments() => new()
    {
        ["touchOffset"] = new List<object?> { 5.0, 300.0 },
        ["progress"] = 0.0,
        ["swipeEdge"] = 0,
    };

    private static object? InboundBackGesture(string method, object? arguments)
    {
        ByteData? reply = Send(SystemChannels.BackGesture, new MethodCall(method, arguments));
        return reply is null ? null : SystemChannels.BackGesture.Codec.DecodeEnvelope(reply);
    }

    private static object? InboundNavigation(MethodCall call) =>
        SystemChannels.Navigation.Codec.DecodeEnvelope(Send(SystemChannels.Navigation, call)!);

    private static ByteData? Send(MethodChannel channel, MethodCall call)
    {
        ByteData? reply = null;
        ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
            channel.Name,
            channel.Codec.EncodeMethodCall(call),
            data => reply = data);
        EventLoop.FlushMicrotasks();
        return reply;
    }

    private void DrainTurns()
    {
        while (_turns.Count > 0)
        {
            _turns.Dequeue()();
        }
    }

    private sealed class ThrowingObserver(bool tracksBackGesture = false) : WidgetsBindingObserver
    {
        public void DidChangeMetrics() => throw new InvalidOperationException("Exception");

        public void DidChangeTextScaleFactor() => throw new InvalidOperationException("Exception");

        public void DidChangePlatformBrightness() => throw new InvalidOperationException("Exception");

        public void DidChangeAppLifecycleState(AppLifecycleState state) =>
            throw new InvalidOperationException("Exception");

        public void DidChangeViewFocus(ViewFocusEvent @event) => throw new InvalidOperationException("Exception");

        public void DidHaveMemoryPressure() => throw new InvalidOperationException("Exception");

        public Task<AppExitResponse> DidRequestAppExit() => throw new InvalidOperationException("Exception");

        public Task<bool> DidPushRouteInformation(RouteInformation routeInformation) =>
            throw new InvalidOperationException("Exception");

        public void HandleStatusBarTap() => throw new InvalidOperationException("Exception");

        public bool HandleStartBackGesture(PredictiveBackEvent backEvent) =>
            tracksBackGesture ? true : throw new InvalidOperationException("Exception");

        public void HandleUpdateBackGestureProgress(PredictiveBackEvent backEvent) =>
            throw new InvalidOperationException("Exception");

        public void HandleCommitBackGesture() => throw new InvalidOperationException("Exception");

        public void HandleCancelBackGesture() => throw new InvalidOperationException("Exception");
    }

    private sealed class LoggingObserver(List<string> log, bool tracksBackGesture = false) : WidgetsBindingObserver
    {
        public void DidChangeMetrics() => log.Add("didChangeMetrics");

        public void DidChangeTextScaleFactor() => log.Add("didChangeTextScaleFactor");

        public void DidChangePlatformBrightness() => log.Add("didChangePlatformBrightness");

        public void DidChangeAppLifecycleState(AppLifecycleState state) => log.Add("didChangeAppLifecycleState");

        public void DidChangeViewFocus(ViewFocusEvent @event) => log.Add("didChangeViewFocus");

        public void DidHaveMemoryPressure() => log.Add("didHaveMemoryPressure");

        public Task<AppExitResponse> DidRequestAppExit()
        {
            log.Add("didRequestAppExit");
            return Task.FromResult(AppExitResponse.Exit);
        }

        public Task<bool> DidPopRoute()
        {
            log.Add("didPopRoute");
            return Task.FromResult(true);
        }

        public Task<bool> DidPushRouteInformation(RouteInformation routeInformation)
        {
            log.Add("didPushRouteInformation");
            return Task.FromResult(false);
        }

        public void HandleStatusBarTap() => log.Add("handleStatusBarTap");

        public bool HandleStartBackGesture(PredictiveBackEvent backEvent)
        {
            log.Add("handleStartBackGesture");
            return tracksBackGesture;
        }

        public void HandleUpdateBackGestureProgress(PredictiveBackEvent backEvent) =>
            log.Add("handleUpdateBackGestureProgress");

        public void HandleCommitBackGesture() => log.Add("handleCommitBackGesture");

        public void HandleCancelBackGesture() => log.Add("handleCancelBackGesture");
    }

    private sealed class RentrantObserver(WidgetsBinding binding) : WidgetsBindingObserver
    {
        public int RemoveSelf()
        {
            int count = 0;
            while (binding.RemoveObserver(this))
            {
                count += 1;
            }

            return count;
        }

        public void DidChangeAccessibilityFeatures() => binding.AddObserver(this);

        public void DidChangeAppLifecycleState(AppLifecycleState state) => binding.AddObserver(this);

        public void DidChangeMetrics() => binding.AddObserver(this);

        public void DidChangePlatformBrightness() => binding.AddObserver(this);

        public void DidChangeTextScaleFactor() => binding.AddObserver(this);

        public void DidChangeViewFocus(ViewFocusEvent @event) => binding.AddObserver(this);

        public void DidHaveMemoryPressure() => binding.AddObserver(this);
    }

    private sealed class PushRouteObserver : WidgetsBindingObserver
    {
        public string? PushedRoute { get; private set; }

#pragma warning disable CS0672 // Overrides the deprecated callback on purpose, as binding_test.dart does.
        public Task<bool> DidPushRoute(string route)
#pragma warning restore CS0672
        {
            PushedRoute = route;
            return Task.FromResult(true);
        }
    }

    private sealed class PushRouteInformationObserver : WidgetsBindingObserver
    {
        public RouteInformation? PushedRouteInformation { get; private set; }

        public Task<bool> DidPushRouteInformation(RouteInformation routeInformation)
        {
            PushedRouteInformation = routeInformation;
            return Task.FromResult(true);
        }
    }
}
