using Avalonia;
using Avalonia.Media;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;
using Xunit;

namespace Flutter.Tests;

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
            var rootBuildCount = 0;
            var detailsBuildCount = 0;

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

            Assert.True(rootBuildCount >= 2);
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
            var sourceCreateRectTweenCalls = 0;
            var destinationCreateRectTweenCalls = 0;
            var tweenLerpCalls = 0;
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

            var now = Scheduler.CurrentSeconds;
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

    private static void AdvanceHeroTransition(WidgetRenderHarness harness, Size viewportSize)
    {
        var now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.016));
        harness.Pump(viewportSize);

        var afterStart = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(afterStart + 0.40));
        harness.Pump(viewportSize);
    }

    private static Route BuildHeroRoute(
        string routeName,
        Point heroOrigin,
        Color heroColor,
        Action onBuild,
        Action<NavigatorState> captureState,
        CreateRectTween? createRectTween = null)
    {
        return new BuilderPageRoute(
            builder: context =>
            {
                captureState(Navigator.Of(context));
                onBuild();
                return BuildHeroPage(routeName, heroOrigin, heroColor, createRectTween);
            },
            settings: new RouteSettings(Name: routeName));
    }

    private static Widget BuildHeroPage(
        string routeLabel,
        Point heroOrigin,
        Color heroColor,
        CreateRectTween? createRectTween = null)
    {
        return new Stack(
            children:
            [
                new Positioned(
                    left: heroOrigin.X,
                    top: heroOrigin.Y,
                    child: new Hero(
                        tag: SharedHeroTag,
                        createRectTween: createRectTween,
                        child: new DecoratedBox(
                            decoration: new BoxDecoration(
                                Color: heroColor,
                                BorderRadius: BorderRadius.Circular(12)),
                            child: new SizedBox(width: 44, height: 44)))),
                new Positioned(
                    left: 8,
                    top: 8,
                    child: new Text(routeLabel))
            ]);
    }

    private static RenderParagraph? FindParagraphByText(RenderObject? root, string text)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderParagraph paragraph && paragraph.Text == text)
        {
            return paragraph;
        }

        RenderParagraph? result = null;
        root.VisitChildren(child =>
        {
            if (result != null)
            {
                return;
            }

            result = FindParagraphByText(child, text);
        });

        return result;
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
