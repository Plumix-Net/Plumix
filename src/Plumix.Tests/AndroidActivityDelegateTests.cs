using System.Globalization;
using Avalonia;
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
}
