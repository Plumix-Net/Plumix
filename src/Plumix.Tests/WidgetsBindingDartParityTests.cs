using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/binding.dart
// Mirrors widgets/binding_test.dart (observer callbacks, the "callbacks handle exceptions gracefully"
// group, locale/navigation/back-gesture/memory-pressure channels, the element-mounting stack filters),
// binding_first_frame_rasterized_test.dart, and localizations_test.dart's LocalizationsResolver cases.

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
        PlatformDispatcher.Instance.SetLocalesTestValue(null);
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
        "didChangeLocales",
        "didHaveMemoryPressure",
        "didRequestAppExit",
        "didPopRoute",
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
    public async Task RentrantObserverCallbacks_DoNotResultInExceptions()
    {
        // binding_test.dart: every dispatch iterates a snapshot of the observers.
        var observer = Observe(new RentrantObserver(_binding));

        _binding.HandleAccessibilityFeaturesChanged();
        _binding.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
        _binding.HandleLocaleChanged();
        PlatformDispatcher.Instance.OnMetricsChanged!();
        PlatformDispatcher.Instance.OnPlatformBrightnessChanged!();
        PlatformDispatcher.Instance.OnTextScaleFactorChanged!();
        _binding.HandleMemoryPressure();
        Assert.True(await _binding.HandlePopRoute());
        Assert.True(await _binding.HandlePushRoute("/"));
        Assert.Equal(AppExitResponse.Exit, await _binding.HandleRequestAppExit());
        _binding.HandleViewFocusChanged(new ViewFocusEvent(0, ViewFocusState.Focused, ViewFocusDirection.Forward));
        EventLoop.FlushMicrotasks();

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

    [Fact]
    public void DidPushRouteInformation_Callback_ReceivesTheLocationAndState()
    {
        var observer = Observe(new PushRouteInformationObserver());

        InboundNavigation(new MethodCall(
            "pushRouteInformation",
            new Dictionary<string, object?> { ["location"] = "testRouteName", ["state"] = "state" }));

        Assert.Equal("testRouteName", observer.PushedRouteInformation!.Uri.OriginalString);
        Assert.Equal("state", observer.PushedRouteInformation.State);
    }

    [Fact]
    public void DidPushRouteInformation_Callback_CanHandleUrl()
    {
        var observer = Observe(new PushRouteInformationObserver());

        InboundNavigation(new MethodCall(
            "pushRouteInformation",
            new Dictionary<string, object?>
            {
                ["location"] = "http://hostname/abc?def=123&def=456#789",
                ["state"] = "state",
            }));

        // Dart also checks the deprecated `location`, which Plumix's RouteInformation does not carry.
        RouteInformation information = observer.PushedRouteInformation!;
        Assert.Equal("http://hostname/abc?def=123&def=456#789", information.Uri.OriginalString);
        Assert.Equal("state", information.State);
    }

    [Fact]
    public void PopRoute_NotHandledByAnObserver_RepliesFalse()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);

        Assert.Equal(false, InboundNavigation(new MethodCall("popRoute")));
        Assert.Contains("SystemNavigator.pop", platform.Methods);
    }

    [Fact]
    public async Task HandlePopRoute_ReportsAnErrorWhenSystemNavigatorPopFails()
    {
        using var platform = new MockMethodCallHandler(
            SystemChannels.Platform,
            call => call.Method == "SystemNavigator.pop"
                ? throw new InvalidOperationException("Platform error")
                : null);

        Assert.False(await _binding.HandlePopRoute());
        EventLoop.FlushMicrotasks();

        FlutterErrorDetails error = Assert.Single(_errors);
        Assert.Contains("Platform error", error.Exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("while popping route", error.Context!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void HandlePopRoute_AwaitsEachObserverBeforeAskingTheNext()
    {
        // Dart awaits every `didPopRoute` in turn: a pending answer is neither handled nor unhandled yet.
        var first = Observe(new PendingRouteObserver());
        var second = Observe(new PendingRouteObserver());

        Task<bool> handled = _binding.HandlePopRoute();
        Assert.False(handled.IsCompleted);
        Assert.Equal(1, first.PopCalls);
        Assert.Equal(0, second.PopCalls);

        first.Answer.SetResult(false);
        EventLoop.FlushMicrotasks();
        Assert.False(handled.IsCompleted);
        Assert.Equal(1, second.PopCalls);

        second.Answer.SetResult(true);
        Assert.True(TestBuildOwner.Complete(handled));
    }

    [Fact]
    public void PushRouteInformation_ThroughTheChannel_RepliesOnceAPendingObserverAnswers()
    {
        var observer = Observe(new PendingRouteObserver());

        ByteData? reply = null;
        ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
            SystemChannels.Navigation.Name,
            SystemChannels.Navigation.Codec.EncodeMethodCall(new MethodCall(
                "pushRouteInformation",
                new Dictionary<string, object?> { ["location"] = "/pending" })),
            data => reply = data);
        EventLoop.FlushMicrotasks();
        Assert.Null(reply);
        Assert.Equal("/pending", observer.PushedLocation);

        observer.Answer.SetResult(true);
        EventLoop.FlushMicrotasks();

        Assert.NotNull(reply);
        Assert.Equal(true, SystemChannels.Navigation.Codec.DecodeEnvelope(reply!));
    }

    // ------------------------------------------------------------------ locales

    [Fact]
    public void DidChangeLocales_ReceivesThePlatformLocales()
    {
        var observer = Observe(new LocalesObserver());
        IReadOnlyList<Locale> previous = PlatformDispatcher.Instance.Locales;
        try
        {
            PlatformDispatcher.Instance.UpdateLocales([new Locale("fr", "FR"), new Locale("en")]);

            Assert.Equal([new Locale("fr", "FR"), new Locale("en")], Assert.Single(observer.Calls)!);
            Assert.Equal(new Locale("fr", "FR"), PlatformDispatcher.Instance.Locale);

            // The engine's `_updateLocales` notifies nobody when the list did not change.
            PlatformDispatcher.Instance.UpdateLocales([new Locale("fr", "FR"), new Locale("en")]);
            Assert.Single(observer.Calls);
        }
        finally
        {
            PlatformDispatcher.Instance.UpdateLocales(previous);
        }
    }

    [Fact]
    public void Locale_IsUndeterminedWhileThePlatformReportsNoLocales()
    {
        var observer = Observe(new LocalesObserver());

        PlatformDispatcher.Instance.SetLocalesTestValue([]);

        Assert.Empty(PlatformDispatcher.Instance.Locales);
        Assert.Equal("und", PlatformDispatcher.Instance.Locale.LanguageCode);
        Assert.Empty(Assert.Single(observer.Calls)!);
    }

    [Fact]
    public void ComputePlatformResolvedLocale_AsksThePlatformResolver()
    {
        Func<IReadOnlyList<Locale>, Locale?>? previous = PlatformDispatcher.Instance.PlatformLocaleResolver;
        try
        {
            PlatformDispatcher.Instance.PlatformLocaleResolver = null;
            Assert.Null(_binding.ComputePlatformResolvedLocale([new Locale("en")]));

            IReadOnlyList<Locale>? asked = null;
            PlatformDispatcher.Instance.PlatformLocaleResolver = supported =>
            {
                asked = supported;
                return supported[^1];
            };
            Locale[] supportedLocales = [new Locale("en"), new Locale("de")];

            Assert.Equal(new Locale("de"), _binding.ComputePlatformResolvedLocale(supportedLocales));
            Assert.Same(supportedLocales, asked);
        }
        finally
        {
            PlatformDispatcher.Instance.PlatformLocaleResolver = previous;
        }
    }

    [Fact]
    public void WidgetsApp_ResolvesItsLocaleFromThePlatformLocales_AndFollowsChanges()
    {
        PlatformDispatcher.Instance.SetLocalesTestValue([new Locale("fr", "FR")]);
        using var tester = new FrameworkDartTester();
        Locale? resolved = null;
        tester.PumpWidget(new WidgetsApp(
            color: Colors.Blue,
            supportedLocales: [new Locale("en", "US"), new Locale("fr", "FR"), new Locale("de")],
            builder: (context, _) =>
            {
                resolved = Localizations.LocaleOf(context);
                return new SizedBox();
            }));
        Assert.Equal(new Locale("fr", "FR"), resolved);

        PlatformDispatcher.Instance.SetLocalesTestValue([new Locale("de", "AT")]);
        tester.Pump();

        Assert.Equal(new Locale("de"), resolved);
    }

    [Fact]
    public void LocalizationsResolverUpdate_NotifiesListenersWhenSupportedLocalesChange()
    {
        PlatformDispatcher.Instance.SetLocalesTestValue([new Locale("en", "US")]);
        using var resolver = new LocalizationsResolver(supportedLocales: [new Locale("en")]);
        bool notified = false;
        resolver.AddListener(() => notified = true);

        resolver.Update(
            locale: null,
            localeListResolutionCallback: null,
            localeResolutionCallback: null,
            localizationsDelegates: null,
            supportedLocales: [new Locale("de")]);

        Assert.True(notified);
    }

    [Fact]
    public void LocalizationsResolverUpdate_DoesNotNotifyWhenTheResolvedLocaleIsUnchanged()
    {
        PlatformDispatcher.Instance.SetLocalesTestValue([new Locale("en", "US")]);
        using var resolver = new LocalizationsResolver(supportedLocales: [new Locale("en"), new Locale("de")]);
        bool notified = false;
        resolver.AddListener(() => notified = true);

        // Update with the same effective supportedLocales shouldn't change resolved locale.
        resolver.Update(
            locale: null,
            localeListResolutionCallback: null,
            localeResolutionCallback: null,
            localizationsDelegates: null,
            supportedLocales: [new Locale("en"), new Locale("de")]);

        Assert.False(notified);
    }

    // ------------------------------------------------------------------ stack filters

    [DebugOnlyFact]
    public void DefaultStackFilter_ElidesFrameworkElementMountingStacks()
    {
        using var tester = new FrameworkDartTester();
        FlutterErrorDetails? errorDetails = null;
        FlutterExceptionHandler? oldHandler = FlutterError.OnError;
        FlutterError.OnError = details => errorDetails = details;
        try
        {
            tester.PumpWidget(new Directionality(
                TextDirection.Ltr,
                new TestStatefulWidget(new Builder(_ => new Opacity(
                    0.5,
                    child: new Builder(_ =>
                    {
                        DebugAssertions.Assert(false);
                        return new Text(string.Empty);
                    }))))));
        }
        finally
        {
            FlutterError.OnError = oldHandler;
        }

        Assert.IsAssignableFrom<AssertionError>(errorDetails!.Exception);
        const string toMatch = "...     Normal element mounting (";
        string rendered = errorDetails.ToString();
        Assert.Equal(1, CountOccurrences(rendered, toMatch));
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
            case "didChangeLocales":
                _binding.HandleLocaleChanged();
                break;
            case "didHaveMemoryPressure":
                await SendMemoryPressure();
                break;
            case "didPopRoute":
                Assert.True(await _binding.HandlePopRoute());
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

        public void DidChangeLocales(IReadOnlyList<Locale>? locales) =>
            throw new InvalidOperationException("Exception");

        public void DidHaveMemoryPressure() => throw new InvalidOperationException("Exception");

        // Dart's `didPopRoute` is `async`, so its exception surfaces from the awaited future.
        public Task<bool> DidPopRoute() => Task.FromException<bool>(new InvalidOperationException("Exception"));

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

        public void DidChangeLocales(IReadOnlyList<Locale>? locales) => log.Add("didChangeLocales");

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

        public void DidChangeLocales(IReadOnlyList<Locale>? locales) => binding.AddObserver(this);

        public void DidHaveMemoryPressure() => binding.AddObserver(this);

        public Task<bool> DidPopRoute()
        {
            binding.AddObserver(this);
            return Task.FromResult(true);
        }

        public Task<bool> DidPushRouteInformation(RouteInformation routeInformation)
        {
            binding.AddObserver(this);
            return Task.FromResult(true);
        }

        public Task<AppExitResponse> DidRequestAppExit()
        {
            binding.AddObserver(this);
            return Task.FromResult(AppExitResponse.Exit);
        }
    }

    private sealed class PendingRouteObserver : WidgetsBindingObserver
    {
        public TaskCompletionSource<bool> Answer { get; } = new();

        public int PopCalls { get; private set; }

        public string? PushedLocation { get; private set; }

        public Task<bool> DidPopRoute()
        {
            PopCalls += 1;
            return Answer.Task;
        }

        public Task<bool> DidPushRouteInformation(RouteInformation routeInformation)
        {
            PushedLocation = routeInformation.Uri.OriginalString;
            return Answer.Task;
        }
    }

    private sealed class LocalesObserver : WidgetsBindingObserver
    {
        public List<IReadOnlyList<Locale>?> Calls { get; } = [];

        public void DidChangeLocales(IReadOnlyList<Locale>? locales) => Calls.Add(locales);
    }

    private sealed class TestStatefulWidget(Widget child) : StatefulWidget
    {
        public Widget Child { get; } = child;

        public override State CreateState() => new TestStatefulWidgetState();
    }

    private sealed class TestStatefulWidgetState : State<TestStatefulWidget>
    {
        public override Widget Build(BuildContext context) => Widget.Child;
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        for (int index = text.IndexOf(value, StringComparison.Ordinal);
             index >= 0;
             index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
        {
            count += 1;
        }

        return count;
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
