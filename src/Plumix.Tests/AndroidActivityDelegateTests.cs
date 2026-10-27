using System.Globalization;
using Avalonia;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// C#-only host tests: the Activity side of Flutter's FlutterActivityAndFragmentDelegateTest.java
// (lifecycle, restoration and back forwarding) without an Android runtime.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class AndroidActivityDelegateTests : IDisposable
{
    private readonly PlatformBinaryMessenger _messenger = new();

    public AndroidActivityDelegateTests() => PlumixHost.ResetFrameworkHandlesBackForTests();

    public void Dispose()
    {
        PlumixHost.ResetFrameworkHandlesBackForTests();
        AndroidActivityDelegate.ResetForTests();
        new MethodChannel(SystemChannels.Restoration.Name).SetPlatformMethodCallHandler(null);
    }

    // FlutterView.sendUserSettingsToFlutter: Configuration.fontScale becomes the dispatcher's text scale
    // factor, and only a change notifies (the engine's _updateUserSettingsData).
    [Fact]
    public void ConfigurationChanged_ReportsFontScaleAsTextScaleFactor()
    {
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        Action? previous = dispatcher.OnTextScaleFactorChanged;
        int changes = 0;
        dispatcher.OnTextScaleFactorChanged = () => changes++;
        try
        {
            var activity = new AndroidActivityDelegate(new AndroidRestorationChannel(true, _messenger), _ => { });

            activity.OnConfigurationChanged(2f);
            activity.OnConfigurationChanged(2f);

            Assert.Equal(2.0, dispatcher.TextScaleFactor);
            Assert.Equal(1, changes);
            using var tester = new FrameworkDartTester();
            TextScaler scaler = MediaQueryData.FromView(tester.View).TextScaler;
            Assert.Equal(2.0, Assert.IsType<SystemTextScaler>(scaler).TextScaleFactor);
            Assert.Equal(28.0, scaler.Scale(14.0));
        }
        finally
        {
            dispatcher.UpdateTextScaleFactor(1.0);
            dispatcher.OnTextScaleFactorChanged = previous;
        }
    }

    // FlutterView.sendUserSettingsToFlutter on Android 14+: the configuration's getScaledFontSize reaches
    // the SystemTextScaler through PlatformDispatcher.scaleFontSize.
    [Fact]
    public void ConfigurationChanged_ReportsTheConfigurationsFontScalingCurve()
    {
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        try
        {
            var activity = new AndroidActivityDelegate(new AndroidRestorationChannel(true, _messenger), _ => { });

            activity.OnConfigurationChanged(2f, fontSize => fontSize <= 20 ? fontSize * 2 : 40 + (fontSize - 20));

            using var tester = new FrameworkDartTester();
            TextScaler scaler = MediaQueryData.FromView(tester.View).TextScaler;
            Assert.Equal(28.0, scaler.Scale(14.0));
            Assert.Equal(52.0, scaler.Scale(32.0));
        }
        finally
        {
            dispatcher.ResetScaledFontSizeForTests();
            dispatcher.UpdateTextScaleFactor(1.0);
        }
    }

    // FlutterView.sendViewportMetricsToFlutter's physicalTouchSlop (ViewConfiguration.getScaledTouchSlop)
    // becomes the view's gesture settings in logical pixels (DeviceGestureSettings.fromView).
    [Fact]
    public void ScaledTouchSlop_ReachesTheViewsGestureSettings()
    {
        var host = new MetricsHost();
        try
        {
            host.ArrangeContent(new Size(400, 800));
            Assert.Null(host.RootFlutterView.GestureSettings);
            Assert.Null(MediaQueryData.FromView(host.RootFlutterView).GestureSettings);

            var activity = new AndroidActivityDelegate(new AndroidRestorationChannel(true, _messenger), _ => { });
            activity.OnScaledTouchSlopChanged(24);
            // The host is not attached, so the slop arrives with its next metrics update.
            host.ArrangeContent(new Size(800, 400));

            Assert.Equal(new DeviceGestureSettings(TouchSlop: 24), host.RootFlutterView.GestureSettings);
            Assert.Equal(24, MediaQueryData.FromView(host.RootFlutterView).GestureSettings?.TouchSlop);
            Assert.Equal(48, MediaQueryData.FromView(host.RootFlutterView).GestureSettings?.PanSlop);
            Assert.Throws<ArgumentOutOfRangeException>(() => activity.OnScaledTouchSlopChanged(-1));
        }
        finally
        {
            PlumixHost.UpdatePhysicalTouchSlop(null);
        }
    }

    // LocalizationPlugin.sendLocalesToFlutter: the configuration's ordered locale list replaces the
    // single-culture list the host reports, including the report WidgetHost makes before runApp.
    [Fact]
    public void LocalesChanged_ReportsTheOrderedPreferredLanguages()
    {
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        IReadOnlyList<Locale> previousLocales = dispatcher.Locales;
        Action? previous = dispatcher.OnLocaleChanged;
        int changes = 0;
        dispatcher.OnLocaleChanged = () => changes++;
        try
        {
            var activity = new AndroidActivityDelegate(new AndroidRestorationChannel(true, _messenger), _ => { });
            Locale[] preferred = [new("de", "DE"), new("sr", "RS", "Latn"), new("en", "US")];

            activity.OnLocalesChanged(preferred);
            PlumixHost.ReportLocales();
            activity.OnLocalesChanged([]);

            Assert.Equal(preferred, dispatcher.Locales);
            Assert.Equal(new Locale("de", "DE"), dispatcher.Locale);
            Assert.Equal(1, changes);
        }
        finally
        {
            PlumixHost.ResetPlatformLocalesForTests();
            dispatcher.UpdateLocales(previousLocales);
            dispatcher.OnLocaleChanged = previous;
        }
    }

    [Fact]
    public void ActivityCallbacks_ReportFlutterLifecycleStates()
    {
        var states = new List<AppLifecycleState>();
        var activity = new AndroidActivityDelegate(new AndroidRestorationChannel(true, _messenger), states.Add);

        activity.OnCreate(null);
        activity.OnResume();
        activity.OnWindowFocusChanged(true);
        activity.OnWindowFocusChanged(false);
        activity.OnWindowFocusChanged(true);
        activity.OnPause();
        activity.OnStop();
        activity.OnDestroy();

        Assert.Equal(
            [
                AppLifecycleState.Resumed,
                AppLifecycleState.Inactive,
                AppLifecycleState.Resumed,
                AppLifecycleState.Inactive,
                AppLifecycleState.Paused,
                AppLifecycleState.Detached,
            ],
            states);
    }

    [Fact]
    public void OnCreate_ReleasesTheFrameworksWaitingRestorationRequest_AndSavedStateRoundTrips()
    {
        var restoration = new AndroidRestorationChannel(true, _messenger);
        var first = new AndroidActivityDelegate(restoration, static _ => { });
        Task<object?> pending = GetRestorationData();
        Assert.False(pending.IsCompleted);

        first.OnCreate(null);
        EventLoop.FlushMicrotasks();
        Assert.True(pending.IsCompletedSuccessfully);

        // The framework stores its bucket with `put`; the Activity saves what it last stored.
        byte[] saved = [4, 2];
        using (EventLoop.EnterFrameworkThread())
        {
            Assert.True(RestorationChannel().InvokeMethod<object>("put", saved).IsCompletedSuccessfully);
        }

        Assert.Equal(saved, first.OnSaveInstanceState());
    }

    [Fact]
    public void PublicConstructor_SharesOneRestorationChannelPerProcess()
    {
        byte[] saved = [7];
        new AndroidActivityDelegate().OnCreate(saved);

        Assert.Equal(saved, new AndroidActivityDelegate().OnSaveInstanceState());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PredictiveBack_ForwardsTheGesture_AndAnUnhandledPopFinishesThroughSystemNavigatorPop(
        bool routeHandlesBack)
    {
        var observer = new BackObserver(routeHandlesBack);
        WidgetsBinding.Instance.AddObserver(observer);
        var host = new TestHost();
        host.AttachPlatformChannelHandler();
        try
        {
            AndroidActivityDelegate activity = CreateActivity();
            int finished = 0;
            activity.PopSystemNavigator = () => finished++;
            activity.OnCreate(null);

            activity.OnBackStarted(BackEvent(0.25));
            activity.OnBackProgressed(BackEvent(0.5));
            activity.OnBackInvoked();
            EventLoop.FlushMicrotasks();

            // An untracked gesture commits as a plain pop (WidgetsBinding._handleCommitBackGesture); a pop
            // no observer took sends SystemNavigator.pop, which FlutterActivity answers with finish().
            Assert.Equal(
                routeHandlesBack ? ["start 0.25", "progress 0.5", "commit"] : ["start 0.25", "pop"],
                observer.Calls);
            Assert.Equal(routeHandlesBack ? 0 : 1, finished);
        }
        finally
        {
            host.DetachPlatformChannelHandler();
            WidgetsBinding.Instance.RemoveObserver(observer);
        }
    }

    [Fact]
    public void FrameworkHandlesBack_ReachesTheLatestActivity_AndIsReplayedToARecreatedOne()
    {
        var host = new TestHost();
        host.AttachPlatformChannelHandler();
        try
        {
            var first = new List<bool>();
            AndroidActivityDelegate firstActivity = CreateActivity();
            firstActivity.FrameworkHandlesBackChanged = first.Add;
            int firstPops = 0;
            firstActivity.PopSystemNavigator = () => firstPops++;
            firstActivity.OnCreate(null);
            Assert.Empty(first);

            SetFrameworkHandlesBack(true);
            SetFrameworkHandlesBack(false);
            SetFrameworkHandlesBack(true);
            Assert.Equal([true, false, true], first);

            firstActivity.OnDestroy();
            SetFrameworkHandlesBack(false);
            Assert.Equal(3, first.Count);
            // With no Activity attached the call is unimplemented, as on the desktop embedders.
            _ = Record.Exception(PopSystemNavigator);
            Assert.Equal(0, firstPops);

            // The framework does not resend its state to a new Activity; the delegate replays it.
            var second = new List<bool>();
            AndroidActivityDelegate secondActivity = CreateActivity();
            secondActivity.FrameworkHandlesBackChanged = second.Add;
            int secondPops = 0;
            secondActivity.PopSystemNavigator = () => secondPops++;
            secondActivity.OnCreate(null);
            Assert.Equal([false], second);

            PopSystemNavigator();
            Assert.Equal(1, secondPops);
        }
        finally
        {
            host.DetachPlatformChannelHandler();
        }
    }

    [Fact]
    public void BackCallback_StepsAsideWhileTheSoftKeyboardIsShown()
    {
        // The Activity's overlay-priority callback outranks the keyboard's default-priority one: kept
        // registered over a shown keyboard, back popped the route instead of hiding the keyboard.
        var host = new TestHost();
        host.AttachPlatformChannelHandler();
        try
        {
            var needed = new List<bool>();
            AndroidActivityDelegate activity = CreateActivity();
            activity.BackCallbackNeededChanged = needed.Add;
            activity.OnCreate(null);

            activity.OnImeVisibilityChanged(true);
            SetFrameworkHandlesBack(true);
            Assert.Empty(needed);

            activity.OnImeVisibilityChanged(false);
            activity.OnImeVisibilityChanged(false);
            Assert.Equal([true], needed);

            activity.OnImeVisibilityChanged(true);
            Assert.Equal([true, false], needed);

            activity.OnImeVisibilityChanged(false);
            SetFrameworkHandlesBack(false);
            activity.OnImeVisibilityChanged(true);
            activity.OnImeVisibilityChanged(false);
            Assert.Equal([true, false, true, false], needed);

            // A recreated Activity gets the replayed state with its own keyboard visibility.
            SetFrameworkHandlesBack(true);
            activity.OnDestroy();
            var recreated = new List<bool>();
            AndroidActivityDelegate next = CreateActivity();
            next.BackCallbackNeededChanged = recreated.Add;
            next.OnCreate(null);
            Assert.Equal([true], recreated);
        }
        finally
        {
            host.DetachPlatformChannelHandler();
        }
    }

    private AndroidActivityDelegate CreateActivity() =>
        new(new AndroidRestorationChannel(true, _messenger), static _ => { });

    private static void SetFrameworkHandlesBack(bool value) =>
        SystemChannels.Platform
            .InvokeMethod<object>("SystemNavigator.setFrameworkHandlesBack", value)
            .GetAwaiter()
            .GetResult();

    private static void PopSystemNavigator() =>
        SystemChannels.Platform.InvokeMethod<object>("SystemNavigator.pop").GetAwaiter().GetResult();

    private static PredictiveBackEvent BackEvent(double progress) =>
        new(progress: progress, swipeEdge: SwipeEdge.Left, touchOffset: new Point(1, 2));

    private MethodChannel RestorationChannel() =>
        new(SystemChannels.Restoration.Name, binaryMessenger: _messenger);

    private Task<object?> GetRestorationData()
    {
        using EventLoop.FrameworkThreadScope scope = EventLoop.EnterFrameworkThread();
        return RestorationChannel().InvokeMethod<object>("get");
    }

    private sealed class TestHost : PlumixHost
    {
        public new void AttachPlatformChannelHandler() => base.AttachPlatformChannelHandler();

        public new void DetachPlatformChannelHandler() => base.DetachPlatformChannelHandler();
    }

    private sealed class BackObserver(bool tracksGesture) : WidgetsBindingObserver
    {
        public List<string> Calls { get; } = [];

        public bool HandleStartBackGesture(PredictiveBackEvent backEvent)
        {
            Calls.Add(string.Create(CultureInfo.InvariantCulture, $"start {backEvent.Progress}"));
            return tracksGesture;
        }

        public void HandleUpdateBackGestureProgress(PredictiveBackEvent backEvent) =>
            Calls.Add(string.Create(CultureInfo.InvariantCulture, $"progress {backEvent.Progress}"));

        public void HandleCommitBackGesture() => Calls.Add("commit");

        public Task<bool> DidPopRoute()
        {
            Calls.Add("pop");
            return Task.FromResult(false);
        }
    }

    private sealed class MetricsHost : PlumixHost
    {
        public void ArrangeContent(Size size) => ArrangeOverride(size);
    }
}
