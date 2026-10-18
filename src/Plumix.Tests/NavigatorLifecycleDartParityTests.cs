using Avalonia;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/navigator.dart
// Dart parity source: flutter/packages/flutter/lib/src/widgets/routes.dart
// Ports navigator_test.dart/routes_test.dart's pointer, async-pop and transition lifecycle contracts.

namespace Plumix.Tests;

#pragma warning disable CS0618 // Exercise the pinned willPop contract, including asynchronous vetoes.

[Collection(SchedulerTestCollection.Name)]
public sealed class NavigatorLifecycleDartParityTests
{
    [Fact]
    public void GesturesBetweenPushAndBuildAreIgnored()
    {
        var log = new List<string>();
        using var harness = new Harness();
        harness.Home = new Row(crossAxisAlignment: Plumix.Rendering.CrossAxisAlignment.Start, children:
        [
            new GestureDetector(
                behavior: Plumix.Rendering.HitTestBehavior.Opaque,
                onTap: () =>
                {
                    log.Add("left");
                    harness.Navigator.Push(Page("next"));
                },
                child: new SizedBox(width: 100, height: 100)),
            new GestureDetector(
                behavior: Plumix.Rendering.HitTestBehavior.Opaque,
                onTap: () => log.Add("right"),
                child: new SizedBox(width: 100, height: 100)),
        ]);
        harness.Mount();
        harness.Tap(new Point(50, 50));
        harness.Tap(new Point(150, 50));
        Assert.Equal(["left"], log);

        harness.Tester.PumpAndSettle();
        harness.Navigator.Pop();
        harness.Tester.PumpAndSettle();
        harness.Tap(new Point(150, 50));
        Assert.Equal(["left", "right"], log);
    }

    [Fact]
    public void PendingGesturesAreRejected()
    {
        int taps = 0;
        int cancels = 0;
        using var harness = new Harness();
        harness.Home = new Listener(
            onPointerCancel: _ => cancels++,
            child: new GestureDetector(
                behavior: Plumix.Rendering.HitTestBehavior.Opaque,
                onTap: () => taps++,
                child: new SizedBox(width: 200, height: 100)));
        harness.Mount();
        TestGesture gesture = harness.Tester.StartGesture(new Point(50, 50), PointerDeviceKind.Touch);
        harness.Navigator.Push(Page("next"));
        EventLoop.FlushMicrotasks();
        gesture.Up();
        Assert.Equal(1, cancels);
        Assert.Equal(0, taps);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MaybePopAwaitsTheScopedDecision(bool allow)
    {
        using var harness = new Harness();
        harness.Mount();
        var decision = new TaskCompletionSource<bool>();
        var route = Page("guarded", new WillPopScope(new Text("guarded"), () => decision.Task));
        harness.Navigator.Push(route);
        harness.Tester.PumpAndSettle();

        Task<bool> result = harness.Navigator.MaybePop("value");
        EventLoop.FlushMicrotasks();
        Assert.False(result.IsCompleted);
        Assert.Same(route, harness.Navigator.CurrentRoute);
        decision.SetResult(allow);
        Assert.True(TestBuildOwner.Complete(result));
        Assert.Equal(allow, route.Popped.IsCompleted);
        if (allow)
        {
            Assert.Equal("value", route.Popped.Result);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MaybePopForgetsAnAwaitWhenTheNavigatorOrTopRouteChanges(bool dispose)
    {
        using var harness = new Harness();
        harness.Mount();
        var decision = new TaskCompletionSource<bool>();
        var guarded = Page("guarded", new WillPopScope(new Text("guarded"), () => decision.Task));
        harness.Navigator.Push(guarded);
        harness.Tester.PumpAndSettle();
        Task<bool> result = harness.Navigator.MaybePop();
        EventLoop.FlushMicrotasks();
        var newer = Page("newer");
        if (dispose)
        {
            harness.Tester.PumpWidget(new SizedBox());
        }
        else
        {
            harness.Navigator.Push(newer);
        }

        decision.SetResult(true);
        Assert.True(TestBuildOwner.Complete(result));
        Assert.False(guarded.Popped.IsCompleted);
        if (!dispose)
        {
            Assert.Same(newer, harness.Navigator.CurrentRoute);
        }
    }

    [Fact]
    public void ScopedCallbacksRunInSnapshotOrderAndStopAtTheFirstVeto()
    {
        using var harness = new Harness();
        harness.Mount();
        var route = Page("guarded");
        harness.Navigator.Push(route);
        harness.Tester.PumpAndSettle();
        var log = new List<int>();
        var first = new TaskCompletionSource<bool>();
        WillPopCallback second = () =>
        {
            log.Add(2);
            return Task.FromResult(false);
        };
        route.AddScopedWillPopCallback(() =>
        {
            log.Add(1);
            route.RemoveScopedWillPopCallback(second);
            return first.Task;
        });
        route.AddScopedWillPopCallback(second);
        route.AddScopedWillPopCallback(() =>
        {
            log.Add(3);
            return Task.FromResult(true);
        });
        Task<bool> result = harness.Navigator.MaybePop();
        Assert.Equal([1], log);
        first.SetResult(true);
        Assert.True(TestBuildOwner.Complete(result));
        Assert.Equal([1, 2], log);
        Assert.False(route.Popped.IsCompleted);
    }

    [Fact]
    public void WillPopOnlyChecksLocalHistoryAndDidPopConsumesItBeforeAnyReverseAnimation()
    {
        using var harness = new Harness();
        harness.Mount();
        var route = Page("details", duration: 300);
        harness.Navigator.Push(route);
        harness.Tester.PumpAndSettle();
        int removals = 0;
        route.AddLocalHistoryEntry(new LocalHistoryEntry(() => removals++));
        Assert.Equal(RoutePopDisposition.Pop, TestBuildOwner.Complete(route.WillPop()));
        Assert.Equal(0, removals);
        Assert.True(route.WillHandlePopInternally);
        Assert.True(TestBuildOwner.Complete(harness.Navigator.MaybePop()));
        Assert.Equal(1, removals);
        Assert.Equal(AnimationStatus.Completed, route.Animation.Status);
        Assert.False(route.Popped.IsCompleted);
    }

    [Fact]
    public void PopUntilIgnoresScopedVetoAndConsumesLocalHistoryInLifoOrder()
    {
        using var harness = new Harness();
        harness.Mount();
        var route = Page("details");
        harness.Navigator.Push(route);
        harness.Tester.PumpAndSettle();
        route.AddScopedWillPopCallback(() => Task.FromResult(false));
        var log = new List<int>();
        route.AddLocalHistoryEntry(new LocalHistoryEntry(() => log.Add(1)));
        route.AddLocalHistoryEntry(new LocalHistoryEntry(() => log.Add(2)));
        int visits = 0;
        harness.Navigator.PopUntil(candidate =>
        {
            if (candidate == route) visits++;
            return candidate.IsFirst;
        });
        Assert.Equal([2, 1], log);
        Assert.Equal(3, visits);
        Assert.True(route.Popped.IsCompleted);
        Assert.Equal("home", harness.Navigator.CurrentRoute!.Settings.Name);
    }

    [Fact]
    public void PopUntilWithResultReturnsOnlyTheLastPoppedRoutesResult()
    {
        using var harness = new Harness();
        harness.Mount();
        Task<object?> firstResult = harness.Navigator.Push(Page("A"));
        harness.Tester.PumpAndSettle();
        Task<object?> secondResult = harness.Navigator.Push(Page("B"));
        harness.Tester.PumpAndSettle();
        harness.Navigator.PopUntilWithResult(route => route.IsFirst, true);
        Assert.True(Assert.IsType<bool>(firstResult.Result));
        Assert.Null(secondResult.Result);
        Assert.Equal("home", harness.Navigator.CurrentRoute!.Settings.Name);
    }

    [Fact]
    public void AbleToPopAllRoutesAndMaybePopBubblesFromAnEmptyHistory()
    {
        using var harness = new Harness();
        harness.Mount();
        harness.Navigator.Push(Page("A", duration: 300));
        harness.Tester.PumpAndSettle();
        harness.Navigator.PopUntil(_ => false);
        harness.Tester.PumpAndSettle();
        Assert.Null(harness.Navigator.CurrentRoute);
        Assert.False(TestBuildOwner.Complete(harness.Navigator.MaybePop()));
        Assert.Null(harness.Tester.TakeException());
    }

    [Fact]
    public void RemovingTheOnlyRouteDoesNotSeedInitialRoutesAgain()
    {
        using var harness = new Harness();
        harness.Mount();
        Route root = harness.Navigator.CurrentRoute!;
        harness.Navigator.RemoveRoute(root, "removed");
        harness.Tester.PumpAndSettle();
        Assert.Null(harness.Navigator.CurrentRoute);
        Assert.Equal("removed", root.Popped.Result);
    }

    [Fact]
    public void ForcedDisposalDoesNotCompletePoppedOrRemoveLocalHistory()
    {
        using var harness = new Harness();
        harness.Mount();
        Route root = harness.Navigator.CurrentRoute!;
        int removals = 0;
        root.AddLocalHistoryEntry(new LocalHistoryEntry(() => removals++));
        harness.Tester.PumpWidget(new SizedBox());
        Assert.False(root.Popped.IsCompleted);
        Assert.Equal(0, removals);
    }

    [Fact]
    public void PoppedCompletesBeforeCompletedAndUsesCurrentResultOnlyForPopped()
    {
        using var harness = new Harness();
        harness.Mount();
        var route = new ControlledRoute(duration: 300);
        harness.Navigator.Push(route);
        harness.Tester.PumpAndSettle();
        harness.Navigator.Pop();
        Assert.Equal("default", route.Popped.Result);
        Assert.False(route.Completed.IsCompleted);
        harness.Tester.PumpAndSettle();
        Assert.True(route.Completed.IsCompleted);
        Assert.Null(route.Completed.Result);
    }

    [Fact]
    public void AlreadyCompletedPushFuturesStillFinishInAMicrotask()
    {
        using var harness = new Harness();
        harness.Mount();
        var route = Page("instant");
        harness.Navigator.Push(route);
        RouteEntry entry = harness.Navigator.HistoryEntries.Last();
        Assert.Equal(RouteLifecycle.Pushing, entry.CurrentState);
        EventLoop.FlushMicrotasks();
        Assert.Equal(RouteLifecycle.Idle, entry.CurrentState);
    }

    [Fact]
    public void CancelingTheEnteringTickerSettlesTheRouteLifecycle()
    {
        using var harness = new Harness();
        harness.Mount();
        var route = new ControlledRoute(duration: 300);
        harness.Navigator.Push(route);
        Assert.Equal(RouteLifecycle.Pushing, harness.Navigator.HistoryEntries.Last().CurrentState);
        route.Driver.Stop(canceled: true);
        EventLoop.FlushMicrotasks();
        Assert.Equal(RouteLifecycle.Idle, harness.Navigator.HistoryEntries.Last().CurrentState);
    }

    [Fact]
    public void RoutesDoNotRebuildJustBecauseTheirAnimationsEnded()
    {
        using var harness = new Harness();
        harness.Mount();
        int builds = 0;
        var route = new PageRouteBuilder(
            pageBuilder: (_, _, _) =>
            {
                builds++;
                return new Text("details");
            },
            transitionDuration: TimeSpan.FromMilliseconds(300));
        harness.Navigator.PushReplacement(route);
        harness.Tester.PumpAndSettle();
        Assert.Equal(1, builds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SecondaryAnimationIsDismissedAfterTheNextRouteIsPoppedOrRemoved(bool remove)
    {
        using var harness = new Harness();
        harness.Mount();
        var first = new ControlledRoute(300);
        var next = new ControlledRoute(300);
        harness.Navigator.Push(first);
        harness.Tester.PumpAndSettle();
        harness.Navigator.Push(next);
        harness.Tester.PumpAndSettle();
        Assert.Same(next.Driver, first.SecondaryParent.Parent);
        if (remove) harness.Navigator.RemoveRoute(next);
        else harness.Navigator.Pop();
        harness.Tester.PumpAndSettle();
        Assert.Equal(0.0, first.SecondaryAnimation.Value);
        Assert.Equal(AnimationStatus.Dismissed, first.SecondaryAnimation.Status);
        Assert.NotSame(next.Driver, first.SecondaryParent.Parent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SecondaryTrainHoppingIsCleanedUpOnCompletionAndInterruption(bool interrupt)
    {
        using var harness = new Harness();
        harness.Mount();
        var first = new ControlledRoute(300);
        harness.Navigator.Push(first);
        harness.Tester.PumpAndSettle();
        var second = new ControlledRoute(300);
        harness.Navigator.Push(second);
        harness.Tester.Pump();
        harness.Tester.Pump(TimeSpan.FromMilliseconds(100));
        var third = new ControlledRoute(150);
        harness.Navigator.PushReplacement(third);
        harness.Tester.Pump();
        harness.Tester.Pump(TimeSpan.FromMilliseconds(1));
        TrainHoppingAnimation hopper = Assert.IsType<TrainHoppingAnimation>(first.SecondaryParent.Parent);
        Assert.Same(second.Driver, hopper.CurrentTrain);
        if (interrupt)
        {
            harness.Navigator.Pop();
        }
        else
        {
            harness.Tester.Pump(TimeSpan.FromMilliseconds(100));
            Assert.Same(third.Driver, first.SecondaryParent.Parent);
        }

        harness.Tester.PumpAndSettle();
        Assert.Null(hopper.CurrentTrain);
        if (!interrupt)
        {
            harness.Navigator.Pop();
            harness.Tester.PumpAndSettle();
        }

        Assert.Equal(AnimationStatus.Dismissed, first.SecondaryAnimation.Status);
    }

    [Fact]
    public void CustomAnimationAndControllerOwnershipAreHonored()
    {
        using var harness = new Harness();
        harness.Mount();
        var route = new ControlledRoute(300, customAnimation: true) { WillDisposeAnimationController = false };
        harness.Navigator.Push(route);
        harness.Tester.PumpAndSettle();
        Assert.Same(route.CustomAnimation, ((ProxyAnimation)route.Animation).Parent);
        AnimationController controller = route.Driver;
        harness.Navigator.Pop();
        harness.Tester.PumpAndSettle();
        controller.SetValue(0.5);
        controller.Dispose();
    }

    [Fact]
    public void TransitionRequestsLatencyOnlyWhileItIsAnimating()
    {
        using var harness = new Harness();
        harness.Mount();
        var route = new ControlledRoute(300);
        harness.Navigator.Push(route);
        Assert.Equal(DartPerformanceMode.Latency, SchedulerBinding.Instance.DebugGetRequestedPerformanceMode());
        harness.Tester.PumpAndSettle();
        Assert.Null(SchedulerBinding.Instance.DebugGetRequestedPerformanceMode());
        harness.Navigator.Pop();
        Assert.Equal(DartPerformanceMode.Latency, SchedulerBinding.Instance.DebugGetRequestedPerformanceMode());
        harness.Tester.PumpAndSettle();
        Assert.Null(SchedulerBinding.Instance.DebugGetRequestedPerformanceMode());
    }

    [Theory]
    [InlineData("push", true)]
    [InlineData("replacement", true)]
    [InlineData("replace", true)]
    [InlineData("remove", true)]
    [InlineData("pop", true)]
    [InlineData("removeBelow", false)]
    [InlineData("replaceBelow", false)]
    public void NavigationCancelsPointersOnlyWhenTheCurrentRouteChanges(string operation, bool cancels)
    {
        int cancellations = 0;
        using var harness = new Harness();
        harness.Mount();
        var route = Page("current", new Listener(
            behavior: Plumix.Rendering.HitTestBehavior.Opaque,
            onPointerCancel: _ => cancellations++,
            child: new SizedBox(width: 200, height: 100)));
        harness.Navigator.Push(route);
        harness.Tester.PumpAndSettle();
        TestGesture gesture = harness.Tester.StartGesture(new Point(50, 50), PointerDeviceKind.Touch);
        switch (operation)
        {
            case "push": harness.Navigator.Push(Page("new")); break;
            case "replacement": harness.Navigator.PushReplacement(Page("new")); break;
            case "replace": harness.Navigator.Replace(route, Page("new")); break;
            case "remove": harness.Navigator.RemoveRoute(route); break;
            case "pop": harness.Navigator.Pop(); break;
            case "removeBelow": harness.Navigator.RemoveRouteBelow(route); break;
            case "replaceBelow": harness.Navigator.ReplaceRouteBelow(route, Page("new")); break;
        }

        EventLoop.FlushMicrotasks();
        Assert.Equal(cancels ? 1 : 0, cancellations);
        gesture.Up();
    }

    [Fact]
    public void PageLocalHistoryPrecedesPageCanPopButFollowsRegisteredPopEntries()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<NavigatorState>("guarded page");
        tester.PumpWidget(new Directionality(TextDirection.Ltr, new Navigator(
            pages: [new LifecyclePage("guarded", canPop: false)], onDidRemovePage: _ => { }, key: key)));
        var route = (ModalRoute)key.CurrentState!.CurrentRoute!;
        route.AddLocalHistoryEntry(new LocalHistoryEntry());
        Assert.Equal(RoutePopDisposition.Pop, route.PopDisposition);
        var blocker = new BlockingPopEntry();
        route.RegisterPopEntry(blocker);
        Assert.Equal(RoutePopDisposition.DoNotPop, route.PopDisposition);
        Assert.True(TestBuildOwner.Complete(key.CurrentState.MaybePop()));
        Assert.True(route.WillHandlePopInternally);
        route.UnregisterPopEntry(blocker);
        Assert.True(TestBuildOwner.Complete(key.CurrentState.MaybePop()));
        Assert.False(route.WillHandlePopInternally);
        Assert.Equal(RoutePopDisposition.DoNotPop, route.PopDisposition);
    }

    [Fact]
    public void AddedPagesWaitForTheTopPageToFinishPushing()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<NavigatorState>("multi page");
        var home = new LifecyclePage("home");
        Widget Build(IReadOnlyList<Page> pages) => new Directionality(TextDirection.Ltr, new Navigator(
            pages: pages, onDidRemovePage: _ => { }, key: key));
        tester.PumpWidget(Build([home]));
        tester.PumpWidget(Build([home, new LifecyclePage("A"), new LifecyclePage("B")]));
        Assert.Equal(RouteLifecycle.Adding, key.CurrentState!.HistoryEntries[1].CurrentState);
        Assert.Equal(RouteLifecycle.Pushing, key.CurrentState.HistoryEntries[2].CurrentState);
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(RouteLifecycle.Adding, key.CurrentState.HistoryEntries[1].CurrentState);
        tester.PumpAndSettle();
        Assert.All(key.CurrentState.HistoryEntries, entry => Assert.Equal(RouteLifecycle.Idle, entry.CurrentState));
    }

    [Fact]
    public void PageBasedRouteCanPopBeforeItsPushFinishes()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<NavigatorState>("early page pop");
        var home = new LifecyclePage("home");
        var removed = new List<Page>();
        Widget Build(IReadOnlyList<Page> pages) => new Directionality(TextDirection.Ltr, new Navigator(
            pages: pages, onDidRemovePage: removed.Add, key: key));
        tester.PumpWidget(Build([home]));
        var second = new LifecyclePage("second");
        tester.PumpWidget(Build([home, second]));
        Route route = key.CurrentState!.CurrentRoute!;
        key.CurrentState.Pop("early");
        tester.PumpAndSettle();
        Assert.Equal([second], removed);
        Assert.Equal("early", route.Popped.Result);
        Assert.Equal("home", key.CurrentState.CurrentRoute!.Settings.Name);
    }

    [DebugOnlyFact]
    public void ObserverCallbacksCannotReenterTheHistoryFlush()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<NavigatorState>("locked navigator");
        bool rejected = false;
        var observer = new PushObserver(route =>
        {
            if (route.Settings.Name == "outer")
            {
                Assert.Throws<AssertionError>(() => { _ = key.CurrentState!.Push(Page("reentrant")); });
                rejected = true;
            }
        });
        tester.PumpWidget(new Directionality(TextDirection.Ltr, new Navigator(
            initialRoute: Page("home"), key: key, observers: [observer])));
        key.CurrentState!.Push(Page("outer"));
        Assert.True(rejected);
        Assert.Equal(2, key.CurrentState.HistoryEntries.Count);
    }

    [DebugOnlyFact]
    public void LegacyPopCallbackCannotReenterTheNavigator()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<NavigatorState>("legacy locked");
        bool rejected = false;
        tester.PumpWidget(new Directionality(TextDirection.Ltr, new Navigator(
            pages: [new LifecyclePage("home"), new LifecyclePage("second")],
            onPopPage: (_, _) =>
            {
                Assert.Throws<AssertionError>(() => { _ = key.CurrentState!.Push(Page("reentrant")); });
                rejected = true;
                return false;
            },
            key: key)));
        key.CurrentState!.Pop();
        Assert.True(rejected);
        Assert.Equal("second", key.CurrentState.CurrentRoute!.Settings.Name);
    }

    [Fact]
    public void NavigationEventEncodesUnsupportedArgumentsAsStrings()
    {
        using var harness = new Harness();
        harness.Mount();
        IReadOnlyDictionary<string, object?>? payload = null;
        void Capture(string kind, IReadOnlyDictionary<string, object?> data)
        {
            if (kind == "Flutter.Navigation") payload = data;
        }

        BindingBase.EventPosted += Capture;
        try
        {
            harness.Navigator.Push(new PageRouteBuilder(
                settings: new RouteSettings("arguments", new Dictionary<string, object?>
                {
                    ["custom"] = new ArgumentValue(),
                    ["unicode"] = "привет",
                }),
                pageBuilder: (_, _, _) => new SizedBox(),
                transitionDuration: TimeSpan.Zero));
            if (Constants.KReleaseMode)
            {
                Assert.Null(payload);
                return;
            }

            var route = Assert.IsType<Dictionary<string, object?>>(payload!["route"]);
            var settings = Assert.IsType<Dictionary<string, object?>>(route["settings"]);
            Assert.Equal("{\"custom\":\"custom value\",\"unicode\":\"привет\"}", settings["arguments"]);
        }
        finally
        {
            BindingBase.EventPosted -= Capture;
        }
    }

    [Fact]
    public void TypedDialogPoppedAndCompletedRemainSeparate()
    {
        var route = new RawDialogRoute<int>((_, _, _) => new SizedBox());
        Task<int> popped = route.Popped;
        Task<int> completed = route.Completed;
        route.DidComplete(42);
        Assert.Equal(42, popped.Result);
        Assert.False(completed.IsCompleted);
        route.Dispose();
        Assert.Equal(0, completed.Result);
    }

    private sealed class ArgumentValue
    {
        public override string ToString() => "custom value";
    }

    private sealed class PushObserver(Action<Route> callback) : NavigatorObserver
    {
        public override void DidPush(Route route, Route? previousRoute) => callback(route);
    }

    private sealed class BlockingPopEntry : PopEntry
    {
        public IValueListenable<bool> CanPopNotifier { get; } = new ValueNotifier<bool>(false);
    }

    private sealed record LifecyclePage : Page
    {
        public LifecyclePage(string name, bool canPop = true)
            : base(key: new ValueKey<string>(name), name: name, canPop: canPop) { }
        public override Route CreateRoute(BuildContext context) => new ControlledRoute(300, settings: this);
    }

    [Fact]
    public void MirroredLifecycleDemoRendersAtPhoneWidthAndAwaitsBackConfirmation()
    {
        using var tester = new FrameworkDartTester(logicalSize: new Size(350, 700));
        tester.PumpWidget(new TestWidgetsApp(home: new NavigatorDemoPage()));
        tester.PumpAndSettle();
        Assert.Null(tester.TakeException());
        tester.Tap(Assert.Single(tester.OnstageElementsWithText("Push page")));
        tester.PumpAndSettle();
        Assert.Single(tester.OnstageElementsWithText("Page 1: Back waits for Allow back."));
        tester.Tap(Assert.Single(tester.OnstageElementsWithText("Back")));
        tester.Pump();
        Assert.Single(tester.OnstageElementsWithText("Result: none; local entries: 0; waiting for Allow back"));
        tester.Tap(Assert.Single(tester.OnstageElementsWithText("Allow back")));
        tester.PumpAndSettle();
        Assert.Single(tester.OnstageElementsWithText("Result: page 1: back; local entries: 0; ready"));
        Assert.Null(tester.TakeException());
    }

    private static PageRouteBuilder Page(string name, Widget? child = null, int duration = 0) => new(
        settings: new RouteSettings(name),
        pageBuilder: (_, _, _) => child ?? new Text(name),
        transitionDuration: TimeSpan.FromMilliseconds(duration),
        reverseTransitionDuration: TimeSpan.FromMilliseconds(duration));

    private sealed class ControlledRoute(
        int duration,
        bool customAnimation = false,
        RouteSettings? settings = null) : PageRoute(settings)
    {
        public AnimationController Driver { get; private set; } = null!;
        public Animation<double>? CustomAnimation { get; private set; }
        public ProxyAnimation SecondaryParent => (ProxyAnimation)((ProxyAnimation)SecondaryAnimation).Parent!;
        public override object? CurrentResult => "default";
        public override TimeSpan TransitionDuration => TimeSpan.FromMilliseconds(duration);
        public override Widget BuildPage(BuildContext context) => new Text("controlled");
        protected override AnimationController CreateAnimationController() => Driver = base.CreateAnimationController();
        protected override Animation<double> CreateAnimation()
        {
            CustomAnimation = customAnimation ? new ProxyAnimation(Driver) : Driver.View;
            return CustomAnimation;
        }
    }

    private sealed class Harness : IDisposable
    {
        private readonly GlobalKey<NavigatorState> _key = new LabeledGlobalKey<NavigatorState>("nav lifecycle");
        public FrameworkDartTester Tester { get; } = new();
        public NavigatorState Navigator => _key.CurrentState!;
        public Widget Home { get; set; } = new Text("home");
        public void Mount() => Tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Navigator(initialRoute: Page("home", Home), key: _key)));
        public void Tap(Point position)
        {
            TestGesture gesture = Tester.StartGesture(position, PointerDeviceKind.Touch);
            gesture.Up();
            EventLoop.FlushMicrotasks();
        }
        public void Dispose() => Tester.Dispose();
    }
}

#pragma warning restore CS0618
