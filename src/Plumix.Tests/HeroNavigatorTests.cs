using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class HeroNavigatorTests
{
    private const string SharedHeroTag = "shared-hero";

    [Fact]
    public void Navigator_Push_WithSharedHeroTag_ShowsBothRoutesDuringFlight_ThenSettlesToDestination()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;
            int rootBuildCount = 0;
            int detailsBuildCount = 0;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => rootBuildCount += 1,
                        captureState: state => navigatorState ??= state)));

            harness.Pump(viewportSize);

            Assert.NotNull(navigatorState);
            Assert.Equal(1, rootBuildCount);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "root-page"));

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => detailsBuildCount += 1,
                    captureState: _ => { }));
            harness.Pump(viewportSize);

            // Flutter's "Pushing opaque Route does not rebuild routes below": the route below stays mounted
            // through `maintainState` and keeps its cached page, so it is never rebuilt by the push.
            Assert.Equal(1, rootBuildCount);
            Assert.True(detailsBuildCount >= 1);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "root-page"));
            Assert.NotNull(FindParagraphByText(harness.RenderView, "details-page"));

            AdvanceHeroTransition(harness, viewportSize);

            Assert.Null(FindParagraphByText(harness.RenderView, "root-page"));
            Assert.NotNull(FindParagraphByText(harness.RenderView, "details-page"));
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Pop_WithSharedHeroTag_KeepsPoppedRouteDuringFlight_ThenDisposesIt()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state)));

            harness.Pump(viewportSize);
            Assert.NotNull(navigatorState);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { }));
            harness.Pump(viewportSize);
            AdvanceHeroTransition(harness, viewportSize);

            Assert.Null(FindParagraphByText(harness.RenderView, "root-page"));
            Assert.NotNull(FindParagraphByText(harness.RenderView, "details-page"));

            navigatorState.Pop();
            harness.Pump(viewportSize);

            Assert.NotNull(FindParagraphByText(harness.RenderView, "root-page"));
            Assert.NotNull(FindParagraphByText(harness.RenderView, "details-page"));

            AdvanceHeroTransition(harness, viewportSize);

            Assert.NotNull(FindParagraphByText(harness.RenderView, "root-page"));
            Assert.Null(FindParagraphByText(harness.RenderView, "details-page"));
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Push_UsesDestinationHeroCreateRectTween_ForFlightBounds()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;
            int sourceCreateRectTweenCalls = 0;
            int destinationCreateRectTweenCalls = 0;
            int tweenLerpCalls = 0;
            Rect? capturedBegin = null;
            Rect? capturedEnd = null;
            var sourceHeroOrigin = new Point(20, 160);
            var destinationHeroOrigin = new Point(238, 18);

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: sourceHeroOrigin,
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state,
                        createRectTween: (begin, end) =>
                        {
                            sourceCreateRectTweenCalls += 1;
                            return new TrackingRectTween(begin, end, () => { });
                        })));

            harness.Pump(viewportSize);
            Assert.NotNull(navigatorState);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: destinationHeroOrigin,
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { },
                    createRectTween: (begin, end) =>
                    {
                        destinationCreateRectTweenCalls += 1;
                        capturedBegin = begin;
                        capturedEnd = end;
                        return new TrackingRectTween(begin, end, () => tweenLerpCalls += 1);
                    }));
            harness.Pump(viewportSize);

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.016));
            harness.Pump(viewportSize);

            Assert.Equal(0, sourceCreateRectTweenCalls);
            Assert.Equal(1, destinationCreateRectTweenCalls);
            Assert.NotNull(capturedBegin);
            Assert.NotNull(capturedEnd);
            Assert.Equal(sourceHeroOrigin.X, capturedBegin!.Value.X);
            Assert.Equal(sourceHeroOrigin.Y, capturedBegin.Value.Y);
            Assert.Equal(44, capturedBegin.Value.Width);
            Assert.Equal(44, capturedBegin.Value.Height);
            Assert.Equal(destinationHeroOrigin.X, capturedEnd!.Value.X);
            Assert.Equal(destinationHeroOrigin.Y, capturedEnd.Value.Y);
            Assert.Equal(44, capturedEnd.Value.Width);
            Assert.Equal(44, capturedEnd.Value.Height);
            Assert.True(tweenLerpCalls > 0);

            AdvanceHeroTransition(harness, viewportSize);
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Push_UsesDestinationHeroFlightShuttleBuilder_WhenBothHeroesProvideBuilder()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;
            int sourceShuttleBuilderCalls = 0;
            int destinationShuttleBuilderCalls = 0;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state,
                        flightShuttleBuilder: (_, _, _, _, _) =>
                        {
                            sourceShuttleBuilderCalls += 1;
                            return new Text("source-shuttle");
                        })));

            harness.Pump(viewportSize);
            Assert.NotNull(navigatorState);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { },
                    flightShuttleBuilder: (_, _, _, _, _) =>
                    {
                        destinationShuttleBuilderCalls += 1;
                        return new Text("destination-shuttle");
                    }));
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            Assert.Equal(0, sourceShuttleBuilderCalls);
            Assert.True(destinationShuttleBuilderCalls > 0);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "destination-shuttle"));
            Assert.Null(FindParagraphByText(harness.RenderView, "source-shuttle"));

            AdvanceHeroTransition(harness, viewportSize);
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Push_UsesSourceHeroFlightShuttleBuilder_AsFallbackWhenDestinationBuilderIsMissing()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;
            int sourceShuttleBuilderCalls = 0;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state,
                        flightShuttleBuilder: (_, _, _, _, _) =>
                        {
                            sourceShuttleBuilderCalls += 1;
                            return new Text("source-fallback-shuttle");
                        })));

            harness.Pump(viewportSize);
            Assert.NotNull(navigatorState);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { }));
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            Assert.True(sourceShuttleBuilderCalls > 0);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "source-fallback-shuttle"));

            AdvanceHeroTransition(harness, viewportSize);
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Push_WithDisabledDestinationHeroMode_DoesNotStartHeroFlight()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state)));

            harness.Pump(viewportSize);
            Assert.NotNull(navigatorState);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { },
                    heroModeEnabled: false));
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            // No hero was invited, so nothing is hidden behind a placeholder mid-transition.
            Assert.Equal(0, CountHiddenPlaceholders(harness.RenderView));

            AdvanceHeroTransition(harness, viewportSize);

            Assert.Null(FindParagraphByText(harness.RenderView, "root-page"));
            Assert.NotNull(FindParagraphByText(harness.RenderView, "details-page"));
            Assert.Equal(0, CountHiddenPlaceholders(harness.RenderView));
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Push_WithNestedNavigatorHeroes_ShowsBothRoutesDuringFlight()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state,
                        useNestedNavigator: true)));

            harness.Pump(viewportSize);
            Assert.NotNull(navigatorState);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "root-page"));

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { },
                    useNestedNavigator: true));
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            Assert.NotNull(FindParagraphByText(harness.RenderView, "root-page"));
            Assert.NotNull(FindParagraphByText(harness.RenderView, "details-page"));

            AdvanceHeroTransition(harness, viewportSize);

            Assert.Null(FindParagraphByText(harness.RenderView, "root-page"));
            Assert.NotNull(FindParagraphByText(harness.RenderView, "details-page"));
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Pop_FromUserGesture_SkipsHeroFlight_WhenTransitionOnUserGesturesDisabled()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state)));

            harness.Pump(viewportSize);
            Assert.NotNull(navigatorState);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { }));
            harness.Pump(viewportSize);
            AdvanceHeroTransition(harness, viewportSize);

            Assert.True(navigatorState.MaybePopFromUserGesture());
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            // `transitionOnUserGestures` is false on both heroes, so `Hero._allHeroesFor` drops them and
            // no flight starts: nothing is hidden behind a placeholder while the pop runs.
            Assert.Equal(0, CountHiddenPlaceholders(harness.RenderView));

            AdvanceHeroTransition(harness, viewportSize);

            Assert.NotNull(FindParagraphByText(harness.RenderView, "root-page"));
            Assert.Null(FindParagraphByText(harness.RenderView, "details-page"));
            Assert.Equal(0, CountHiddenPlaceholders(harness.RenderView));
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Pop_FromUserGesture_UsesHeroFlight_WhenBothHeroesAllowGestureTransition()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state,
                        transitionOnUserGestures: true)));

            harness.Pump(viewportSize);
            Assert.NotNull(navigatorState);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { },
                    transitionOnUserGestures: true));
            harness.Pump(viewportSize);
            AdvanceHeroTransition(harness, viewportSize);

            Assert.True(navigatorState.MaybePopFromUserGesture());
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            Assert.NotNull(FindParagraphByText(harness.RenderView, "root-page"));
            Assert.NotNull(FindParagraphByText(harness.RenderView, "details-page"));

            AdvanceHeroTransition(harness, viewportSize);

            Assert.NotNull(FindParagraphByText(harness.RenderView, "root-page"));
            Assert.Null(FindParagraphByText(harness.RenderView, "details-page"));
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Push_UsesSourceHeroPlaceholderBuilder_DuringFlight()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;
            int placeholderBuilderCalls = 0;
            Size? capturedPlaceholderSize = null;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state,
                        placeholderBuilder: (context, size, child) =>
                        {
                            _ = context;
                            _ = child;
                            placeholderBuilderCalls += 1;
                            capturedPlaceholderSize = size;
                            return new Text("source-placeholder");
                        })));

            harness.Pump(viewportSize);
            Assert.NotNull(navigatorState);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { }));
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            Assert.True(placeholderBuilderCalls > 0);
            Assert.NotNull(capturedPlaceholderSize);
            Assert.Equal(44, capturedPlaceholderSize!.Value.Width);
            Assert.Equal(44, capturedPlaceholderSize.Value.Height);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "source-placeholder"));

            AdvanceHeroTransition(harness, viewportSize);

            Assert.Null(FindParagraphByText(harness.RenderView, "source-placeholder"));
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Pop_UsesDestinationHeroPlaceholderBuilder_DuringFlight()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;
            int placeholderBuilderCalls = 0;
            Size? capturedPlaceholderSize = null;
            Size? pushPlaceholderSize = null;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state,
                        placeholderBuilder: (context, size, child) =>
                        {
                            _ = context;
                            _ = child;
                            placeholderBuilderCalls += 1;
                            pushPlaceholderSize ??= size;
                            capturedPlaceholderSize = size;
                            return new Text("destination-placeholder");
                        })));

            harness.Pump(viewportSize);
            Assert.NotNull(navigatorState);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { }));
            harness.Pump(viewportSize);
            AdvanceHeroTransition(harness, viewportSize);

            navigatorState.Pop();
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            // The push left this hero's placeholder in place (`endFlight(keepPlaceholder: true)`), so the
            // pop's `startFlight` re-measures the hero while it is already showing that placeholder --
            // Dart measures `context.findRenderObject()` the same way.
            Assert.True(placeholderBuilderCalls > 0);
            Assert.NotNull(capturedPlaceholderSize);
            Assert.Equal(44, pushPlaceholderSize!.Value.Width);
            Assert.Equal(44, pushPlaceholderSize.Value.Height);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "destination-placeholder"));

            AdvanceHeroTransition(harness, viewportSize);

            Assert.Null(FindParagraphByText(harness.RenderView, "destination-placeholder"));
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Push_DefaultHeroPlaceholder_UsesOffstageChildForSourceHero()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state)));

            harness.Pump(viewportSize);
            Assert.NotNull(navigatorState);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { }));
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            Assert.True(CountHiddenPlaceholders(harness.RenderView) > 0);

            AdvanceHeroTransition(harness, viewportSize);

            Assert.Equal(0, CountHiddenPlaceholders(harness.RenderView));
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Pop_DefaultHeroPlaceholder_DoesNotUseOffstageChild()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state)));

            harness.Pump(viewportSize);
            Assert.NotNull(navigatorState);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { }));
            harness.Pump(viewportSize);
            AdvanceHeroTransition(harness, viewportSize);

            navigatorState.Pop();
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            Assert.Equal(0, CountHiddenPlaceholders(harness.RenderView));

            AdvanceHeroTransition(harness, viewportSize);
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_PushFlight_InterruptedByPop_DivertsActiveHeroFlight()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;
            int rootCreateRectTweenCalls = 0;
            int detailsCreateRectTweenCalls = 0;
            int divertedTweenLerpCalls = 0;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state,
                        createRectTween: (begin, end) =>
                        {
                            rootCreateRectTweenCalls += 1;
                            return new TrackingRectTween(begin, end, () => { });
                        })));

            harness.Pump(viewportSize);
            Assert.NotNull(navigatorState);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { },
                    createRectTween: (begin, end) =>
                    {
                        detailsCreateRectTweenCalls += 1;
                        return new TrackingRectTween(begin, end, () => divertedTweenLerpCalls += 1);
                    }));
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            Assert.Equal(1, detailsCreateRectTweenCalls);
            Assert.Equal(0, rootCreateRectTweenCalls);
            Assert.True(divertedTweenLerpCalls > 0);

            int tweenLerpCallsBeforePop = divertedTweenLerpCalls;
            navigatorState.Pop();
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            Assert.Equal(1, detailsCreateRectTweenCalls);
            Assert.Equal(0, rootCreateRectTweenCalls);
            Assert.True(divertedTweenLerpCalls > tweenLerpCallsBeforePop);

            AdvanceHeroTransition(harness, viewportSize);

            Assert.NotNull(FindParagraphByText(harness.RenderView, "root-page"));
            Assert.Null(FindParagraphByText(harness.RenderView, "details-page"));
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Push_WithDuplicateHeroTagsInRouteSubtree_ThrowsInvalidOperationException()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state)));
            harness.Pump(viewportSize);
            Assert.NotNull(navigatorState);

            // Dart asserts inside `Hero._allHeroesFor`, which only runs once a flight is being formed,
            // so the duplicate tag surfaces on the push and not while the route subtree is first built.
            navigatorState!.Push(BuildDuplicateHeroTagRoute(routeName: "duplicate-tags"));
            harness.Pump(viewportSize);

            var exception = Assert.Throws<InvalidOperationException>(
                () => PumpHeroTransitionFrame(harness, viewportSize));

            Assert.Contains("multiple heroes", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_InitialRoute_WithNestedHero_ThrowsInvalidOperationException()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                using var harness = new WidgetRenderHarness(
                    new Navigator(
                        initialRoute: BuildNestedHeroRoute(
                            routeName: "nested-hero")));
                harness.Pump(viewportSize);
            });

            Assert.Contains("cannot be the descendant of another Hero", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Push_SamplesFlightRectThroughDestinationHeroCurve()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;
            Animation<double>? destinationAnimation = null;
            RecordingRectTween? recordingTween = null;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state)));
            harness.Pump(viewportSize);
            Assert.NotNull(navigatorState);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { },
                    createRectTween: (begin, end) => recordingTween = new RecordingRectTween(begin, end),
                    curve: Curves.Linear,
                    captureAnimation: animation => destinationAnimation ??= animation));
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);
            Assert.NotNull(destinationAnimation);

            // A linear `Hero.curve` makes the flight sample the raw route animation value.
            double rawBefore = destinationAnimation!.Value;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.05));
            harness.Pump(viewportSize);
            double rawAfter = destinationAnimation.Value;

            Assert.True(rawAfter > rawBefore);
            Assert.NotNull(recordingTween);
            Assert.NotNull(recordingTween!.LastT);
            Assert.Equal(rawAfter, recordingTween.LastT!.Value, 3);

            AdvanceHeroTransition(harness, viewportSize);
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Push_DefaultHeroCurveIsFastOutSlowIn()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;
            Animation<double>? destinationAnimation = null;
            RecordingRectTween? recordingTween = null;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state)),
                new HeroController(
                    createRectTween: (begin, end) => recordingTween = new RecordingRectTween(begin, end)));
            harness.Pump(viewportSize);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { },
                    captureAnimation: animation => destinationAnimation ??= animation));
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.05));
            harness.Pump(viewportSize);

            Assert.NotNull(destinationAnimation);
            double raw = destinationAnimation!.Value;
            Assert.True(raw is > 0.0 and < 1.0);
            Assert.NotNull(recordingTween);
            Assert.NotNull(recordingTween!.LastT);
            Assert.Equal(Curves.FastOutSlowIn(raw), recordingTween.LastT!.Value, 3);

            AdvanceHeroTransition(harness, viewportSize);
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void HeroController_CreateRectTween_IsUsedWhenTheHeroSuppliesNone_AndLosesToTheDestinationHero()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;
            int controllerTweenCalls = 0;
            int heroTweenCalls = 0;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state)),
                new HeroController(
                    createRectTween: (begin, end) =>
                    {
                        controllerTweenCalls += 1;
                        return new TrackingRectTween(begin, end, () => { });
                    }));
            harness.Pump(viewportSize);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { }));
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            Assert.True(controllerTweenCalls > 0);
            AdvanceHeroTransition(harness, viewportSize);

            // The destination hero's own factory wins over the controller's.
            int controllerCallsAfterPush = controllerTweenCalls;
            navigatorState.Push(
                BuildHeroRoute(
                    routeName: "third-page",
                    heroOrigin: new Point(60, 60),
                    heroColor: Colors.SeaGreen,
                    onBuild: () => { },
                    captureState: _ => { },
                    createRectTween: (begin, end) =>
                    {
                        heroTweenCalls += 1;
                        return new TrackingRectTween(begin, end, () => { });
                    }));
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            Assert.Equal(1, heroTweenCalls);
            Assert.Equal(controllerCallsAfterPush, controllerTweenCalls);

            AdvanceHeroTransition(harness, viewportSize);
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Push_WithoutAHeroControllerScope_DoesNotFly()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;

            using var harness = new WidgetRenderHarness(
                HeroControllerScope.None(
                    new Navigator(
                        initialRoute: BuildHeroRoute(
                            routeName: "root-page",
                            heroOrigin: new Point(20, 160),
                            heroColor: Colors.OrangeRed,
                            onBuild: () => { },
                            captureState: state => navigatorState ??= state))));
            harness.Pump(viewportSize);
            Assert.NotNull(navigatorState);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { }));
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            // `HeroControllerScope.none` hides the controller the harness installed, so no hero is hidden
            // behind a placeholder and no flight overlay exists.
            Assert.Equal(0, CountHiddenPlaceholders(harness.RenderView));

            AdvanceHeroTransition(harness, viewportSize);

            Assert.Null(FindParagraphByText(harness.RenderView, "root-page"));
            Assert.NotNull(FindParagraphByText(harness.RenderView, "details-page"));
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void Navigator_Push_WithNoMatchingTagOnTheDestination_DoesNotFly()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state)));
            harness.Pump(viewportSize);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { },
                    tag: "a-different-tag"));
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            Assert.Equal(0, CountHiddenPlaceholders(harness.RenderView));

            AdvanceHeroTransition(harness, viewportSize);

            Assert.Equal(0, CountHiddenPlaceholders(harness.RenderView));
            Assert.NotNull(FindParagraphByText(harness.RenderView, "details-page"));
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    [Fact]
    public void HeroController_Dispose_DetachesFromTheNavigatorAndDropsInFlightHeroes()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();

        try
        {
            var viewportSize = new Size(320, 240);
            NavigatorState? navigatorState = null;

            using var harness = new WidgetRenderHarness(
                new Navigator(
                    initialRoute: BuildHeroRoute(
                        routeName: "root-page",
                        heroOrigin: new Point(20, 160),
                        heroColor: Colors.OrangeRed,
                        onBuild: () => { },
                        captureState: state => navigatorState ??= state)));
            harness.Pump(viewportSize);

            Assert.Same(navigatorState, harness.HeroController.Navigator);
            Assert.False(harness.HeroController.IsDisposed);

            navigatorState!.Push(
                BuildHeroRoute(
                    routeName: "details-page",
                    heroOrigin: new Point(238, 18),
                    heroColor: Colors.SteelBlue,
                    onBuild: () => { },
                    captureState: _ => { }));
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            Assert.True(CountHiddenPlaceholders(harness.RenderView) > 0);

            harness.HeroController.Dispose();

            Assert.True(harness.HeroController.IsDisposed);
            Assert.Null(harness.HeroController.Navigator);
        }
        finally
        {
            Scheduler.ResetForTests();
            NavigatorBackButtonDispatcher.ResetForTests();
        }
    }

    private static void AdvanceHeroTransition(WidgetRenderHarness harness, Size viewportSize)
    {
        PumpHeroTransitionFrame(harness, viewportSize);

        double afterStart = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(afterStart + 0.40));
        harness.Pump(viewportSize);
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.001));
        harness.Pump(viewportSize);
    }

    /// <summary>
    /// Runs the two frames a flight needs: the destination route builds (offstage, because its animation
    /// value is still 0), and the post-frame callback `HeroController` scheduled then measures both heroes,
    /// hides them behind placeholders and inserts the flight's overlay entry.
    /// </summary>
    private static void PumpHeroTransitionFrame(WidgetRenderHarness harness, Size viewportSize)
    {
        harness.Pump(viewportSize);
        AnimationPump.Prime();
        harness.Pump(viewportSize);
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.016));
        harness.Pump(viewportSize);
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.032));
        harness.Pump(viewportSize);
    }

    private static Route BuildHeroRoute(
        string routeName,
        Point heroOrigin,
        Color heroColor,
        Action onBuild,
        Action<NavigatorState> captureState,
        CreateRectTween? createRectTween = null,
        HeroFlightShuttleBuilder? flightShuttleBuilder = null,
        HeroPlaceholderBuilder? placeholderBuilder = null,
        bool heroModeEnabled = true,
        bool transitionOnUserGestures = false,
        bool useNestedNavigator = false,
        Curve? curve = null,
        Curve? reverseCurve = null,
        object? tag = null,
        Action<Animation<double>>? captureAnimation = null)
    {
        return new PageRouteBuilder(
            pageBuilder: (context, animation, _) =>
            {
                captureState(Navigator.Of(context));
                captureAnimation?.Invoke(animation);
                onBuild();
                return useNestedNavigator
                    ? BuildNestedNavigatorHeroPage(
                        routeName,
                        heroOrigin,
                        heroColor,
                        createRectTween,
                        flightShuttleBuilder,
                        placeholderBuilder,
                        heroModeEnabled,
                        transitionOnUserGestures)
                    : BuildHeroPage(
                        routeName,
                        heroOrigin,
                        heroColor,
                        createRectTween,
                        flightShuttleBuilder,
                        placeholderBuilder,
                        heroModeEnabled,
                        transitionOnUserGestures,
                        curve,
                        reverseCurve,
                        tag);
            },
            settings: new RouteSettings(Name: routeName));
    }

    private static Widget BuildNestedNavigatorHeroPage(
        string routeLabel,
        Point heroOrigin,
        Color heroColor,
        CreateRectTween? createRectTween = null,
        HeroFlightShuttleBuilder? flightShuttleBuilder = null,
        HeroPlaceholderBuilder? placeholderBuilder = null,
        bool heroModeEnabled = true,
        bool transitionOnUserGestures = false)
    {
        return new Navigator(
            initialRoute: new PageRouteBuilder(
                pageBuilder: (_, _, _) => BuildHeroPage(
                    routeLabel,
                    heroOrigin,
                    heroColor,
                    createRectTween,
                    flightShuttleBuilder,
                    placeholderBuilder,
                    heroModeEnabled,
                    transitionOnUserGestures),
                settings: new RouteSettings(Name: $"{routeLabel}-nested-inner")));
    }

    private static Route BuildDuplicateHeroTagRoute(string routeName)
    {
        return new PageRouteBuilder(
            pageBuilder: (_, _, _) =>
                new Stack(
                    textDirection: TextDirection.Ltr,
                    children:
                    [
                        new Positioned(
                            left: 20,
                            top: 160,
                            child: new Hero(
                                tag: SharedHeroTag,
                                child: new SizedBox(width: 44, height: 44))),
                        new Positioned(
                            left: 90,
                            top: 160,
                            child: new Hero(
                                tag: SharedHeroTag,
                                child: new SizedBox(width: 44, height: 44)))
                    ]),
            settings: new RouteSettings(Name: routeName));
    }

    private static Route BuildNestedHeroRoute(string routeName)
    {
        return new PageRouteBuilder(
            pageBuilder: (_, _, _) =>
                new Hero(
                    tag: "outer-hero",
                    child: new Hero(
                        tag: "inner-hero",
                        child: new SizedBox(width: 44, height: 44))),
            settings: new RouteSettings(Name: routeName));
    }

    private static Widget BuildHeroPage(
        string routeLabel,
        Point heroOrigin,
        Color heroColor,
        CreateRectTween? createRectTween = null,
        HeroFlightShuttleBuilder? flightShuttleBuilder = null,
        HeroPlaceholderBuilder? placeholderBuilder = null,
        bool heroModeEnabled = true,
        bool transitionOnUserGestures = false,
        Curve? curve = null,
        Curve? reverseCurve = null,
        object? tag = null)
    {
        Widget heroWidget = new Hero(
            tag: tag ?? SharedHeroTag,
            createRectTween: createRectTween,
            flightShuttleBuilder: flightShuttleBuilder,
            placeholderBuilder: placeholderBuilder,
            transitionOnUserGestures: transitionOnUserGestures,
            curve: curve,
            reverseCurve: reverseCurve,
            child: new DecoratedBox(
                decoration: new BoxDecoration(
                    Color: heroColor,
                    BorderRadius: BorderRadius.Circular(12)),
                child: new SizedBox(width: 44, height: 44)));

        if (!heroModeEnabled)
        {
            heroWidget = new HeroMode(
                enabled: false,
                child: heroWidget);
        }

        return new Stack(
            textDirection: TextDirection.Ltr,
            children:
            [
                new Positioned(
                    left: heroOrigin.X,
                    top: heroOrigin.Y,
                    child: heroWidget),
                new Positioned(
                    left: 8,
                    top: 8,
                    child: new Text(routeLabel))
            ]);
    }

    // Offstage overlay entries are skipped, matching the `skipOffstage: true` default of Flutter's finders:
    // a route below an opaque route stays mounted through `maintainState` but is neither laid out nor painted.
    private static RenderParagraph? FindParagraphByText(RenderObject? root, string text)
    {
        return OverlayVisibility.FindOnstage<RenderParagraph>(root, paragraph => paragraph.PlainText == text);
    }

    private static int CountDescendants<TRenderObject>(RenderObject? root) where TRenderObject : RenderObject
    {
        return OverlayVisibility.CountOnstage<TRenderObject>(root);
    }

    /// <summary>
    /// Hero placeholders that are actually hiding their child. Every modal route also composes an
    /// <see cref="Offstage"/> for <c>ModalRoute.offstage</c>, so only enabled ones count as placeholders.
    /// </summary>
    private static int CountHiddenPlaceholders(RenderObject? root)
    {
        int count = 0;
        OverlayVisibility.VisitOnstage(root, node =>
        {
            if (node is RenderOffstage { Offstage: true })
            {
                count += 1;
            }
        });

        return count;
    }

    /// <summary>A <see cref="RectTween"/> that records the progress the flight last sampled it at.</summary>
    private sealed class RecordingRectTween : RectTween
    {
        public RecordingRectTween(Rect begin, Rect end) : base(begin: begin, end: end)
        {
        }

        public double? LastT { get; private set; }

        public override Rect Lerp(Rect a, Rect b, double t)
        {
            LastT = t;
            return base.Lerp(a, b, t);
        }
    }

    /// <summary>A <see cref="RectTween"/> that records how often the flight sampled it.</summary>
    private sealed class TrackingRectTween : RectTween
    {
        private readonly Action _onLerp;

        public TrackingRectTween(Rect begin, Rect end, Action onLerp) : base(begin: begin, end: end)
        {
            _onLerp = onLerp ?? throw new ArgumentNullException(nameof(onLerp));
        }

        public override Rect Lerp(Rect a, Rect b, double t)
        {
            _onLerp();
            return base.Lerp(a, b, t);
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly PipelineOwner _pipeline;
        private readonly HarnessRootElement _rootElement;

        public WidgetRenderHarness(Widget rootWidget, HeroController? heroController = null)
        {
            // Stands in for MaterialApp/CupertinoApp, which is where Flutter installs the HeroController
            // a Navigator picks up through HeroControllerScope.
            HeroController = heroController ?? new HeroController();
            rootWidget = new HeroControllerScope(controller: HeroController, child: rootWidget);
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);

            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public HeroController HeroController { get; }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void Dispose()
        {
            _rootElement.Unmount();
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            public override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            public override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, Slot);
            }

            public override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild();
            }

            public override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            public override void VisitChildren(Action<Element> visitor)
            {
                if (_child != null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                if (slot != null)
                {
                    throw new InvalidOperationException("HarnessRootElement expects null slot.");
                }

                if (child is not RenderBox renderBox)
                {
                    throw new InvalidOperationException("HarnessRootElement can host only RenderBox.");
                }

                _renderView.Child = renderBox;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
                if (!Equals(oldSlot, newSlot))
                {
                    throw new InvalidOperationException("HarnessRootElement does not support non-null slot moves.");
                }
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (slot != null)
                {
                    throw new InvalidOperationException("HarnessRootElement expects null slot.");
                }

                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }

            public override void Unmount()
            {
                if (_child != null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }
}
