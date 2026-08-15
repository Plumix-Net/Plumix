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
                            return new TrackingRectTween(() => { });
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
                        return new TrackingRectTween(() => tweenLerpCalls += 1);
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
                        flightShuttleBuilder: (_, _, _, _) =>
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
                    flightShuttleBuilder: (_, _, _, _) =>
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
                        flightShuttleBuilder: (_, _, _, _) =>
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

            Assert.True(placeholderBuilderCalls > 0);
            Assert.NotNull(capturedPlaceholderSize);
            Assert.Equal(44, capturedPlaceholderSize!.Value.Width);
            Assert.Equal(44, capturedPlaceholderSize.Value.Height);
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
                            _ = begin;
                            _ = end;
                            rootCreateRectTweenCalls += 1;
                            return new TrackingRectTween(() => { });
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
                        _ = begin;
                        _ = end;
                        detailsCreateRectTweenCalls += 1;
                        return new TrackingRectTween(() => divertedTweenLerpCalls += 1);
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
    public void Navigator_InitialRoute_WithDuplicateHeroTagsInRouteSubtree_ThrowsInvalidOperationException()
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
                        initialRoute: BuildDuplicateHeroTagRoute(
                            routeName: "duplicate-tags")));
                harness.Pump(viewportSize);
            });

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

    private static void AdvanceHeroTransition(WidgetRenderHarness harness, Size viewportSize)
    {
        PumpHeroTransitionFrame(harness, viewportSize);

        double afterStart = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(afterStart + 0.40));
        harness.Pump(viewportSize);
    }

    private static void PumpHeroTransitionFrame(WidgetRenderHarness harness, Size viewportSize)
    {
        // The flight controller is created by the build this frame runs, so it takes its start
        // timestamp from the priming frame and only advances on the one after it.
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
        bool useNestedNavigator = false)
    {
        return new BuilderPageRoute(
            builder: context =>
            {
                captureState(Navigator.Of(context));
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
                        transitionOnUserGestures);
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
            initialRoute: new BuilderPageRoute(
                builder: _ => BuildHeroPage(
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
        return new BuilderPageRoute(
            builder: _ =>
                new Stack(
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
        return new BuilderPageRoute(
            builder: _ =>
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
        bool transitionOnUserGestures = false)
    {
        Widget heroWidget = new Hero(
            tag: SharedHeroTag,
            createRectTween: createRectTween,
            flightShuttleBuilder: flightShuttleBuilder,
            placeholderBuilder: placeholderBuilder,
            transitionOnUserGestures: transitionOnUserGestures,
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

    private sealed class TrackingRectTween : Tween<Rect>
    {
        private readonly Action _onLerp;

        public TrackingRectTween(Action onLerp)
        {
            _onLerp = onLerp ?? throw new ArgumentNullException(nameof(onLerp));
        }

        public override Rect Lerp(Rect a, Rect b, double t)
        {
            _onLerp();
            return a;
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly PipelineOwner _pipeline;
        private readonly HarnessRootElement _rootElement;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);

            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

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

            internal override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            internal override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, Slot);
            }

            internal override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild();
            }

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            internal override void VisitChildren(Action<Element> visitor)
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

            internal override void Unmount()
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
