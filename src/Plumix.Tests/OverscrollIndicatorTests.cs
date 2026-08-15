using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class OverscrollIndicatorTests : IDisposable
{
    private static readonly Size Viewport = new(240, 300);

    public OverscrollIndicatorTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void Indicators_ExposeFlutterDefaultsAndAxis()
    {
        var glow = new GlowingOverscrollIndicator(
            axisDirection: AxisDirection.Down,
            color: Colors.Blue);
        var stretch = new StretchingOverscrollIndicator(
            axisDirection: AxisDirection.Left);

        Assert.True(glow.ShowLeading);
        Assert.True(glow.ShowTrailing);
        Assert.Equal(Axis.Vertical, glow.Axis);
        Assert.Null(glow.Child);
        Assert.True(glow.NotificationPredicate(new ScrollUpdateNotification(Metrics())));
        Assert.False(glow.NotificationPredicate(new ScrollUpdateNotification(Metrics(), depth: 1)));

        Assert.Equal(Axis.Horizontal, stretch.Axis);
        Assert.Equal(Clip.HardEdge, stretch.ClipBehavior);
        Assert.Null(stretch.Child);
        Assert.True(stretch.NotificationPredicate(new ScrollUpdateNotification(Metrics())));
    }

    [Fact]
    public void StretchEffect_ValidatesStrengthAndResolvesDirectionalOrigin()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new StretchEffect(
            axis: Axis.Vertical,
            stretchStrength: -1.01,
            child: new SizedBox()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StretchEffect(
            axis: Axis.Vertical,
            stretchStrength: double.NaN,
            child: new SizedBox()));

        using var horizontal = new WidgetRenderHarness(Wrap(
            new StretchEffect(
                axis: Axis.Horizontal,
                stretchStrength: 0.25,
                child: new SizedBox(width: 40, height: 20)),
            TextDirection.Rtl));
        horizontal.Pump(Viewport);
        RenderTransform transform = horizontal.FindRenderObject<RenderTransform>();

        Assert.Equal(1.25, transform.Transform[0], precision: 6);
        Assert.Equal(1.0, transform.Transform[5], precision: 6);
        Assert.Equal(Alignment.CenterRight, transform.Alignment);
        Assert.Equal(FilterQuality.Medium, transform.FilterQuality);
    }

    [Fact]
    public void Glow_DispatchesConfirmationOncePerOverscrollSequenceAndHonorsPaintOffset()
    {
        int confirmations = 0;
        var emitterKey = new LabeledGlobalKey<NotificationEmitterState>("glow-emitter");
        using var harness = new WidgetRenderHarness(Wrap(
            new NotificationListener<OverscrollIndicatorNotification>(
                onNotification: notification =>
                {
                    confirmations += 1;
                    notification.PaintOffset = 24.0;
                    return false;
                },
                child: new GlowingOverscrollIndicator(
                    axisDirection: AxisDirection.Down,
                    color: Colors.Blue,
                    child: new NotificationEmitter(key: emitterKey)))));
        harness.Pump(Viewport);
        NotificationEmitterState emitter = Assert.IsType<NotificationEmitterState>(emitterKey.CurrentState);
        var details = new DragUpdateDetails(
            GlobalPosition: new Point(120, 20),
            LocalPosition: new Point(120, 20),
            Delta: new Point(0, 20),
            PrimaryDelta: 20);

        emitter.Dispatch(new OverscrollNotification(Metrics(), -30, dragDetails: details));
        emitter.Dispatch(new OverscrollNotification(Metrics(), -20, dragDetails: details));
        PumpAnimation(harness, TimeSpan.FromMilliseconds(100));

        GlowingOverscrollIndicatorState state = harness.FindState<GlowingOverscrollIndicatorState>();
        Assert.Equal(1, confirmations);
        Assert.Equal(24.0, state.LeadingController.PaintOffset);
        Assert.True(state.LeadingController.GlowOpacity > 0.0);
        Assert.Equal(0.0, state.TrailingController.GlowOpacity);

        emitter.Dispatch(new ScrollUpdateNotification(
            Metrics(),
            dragDetails: details,
            scrollDelta: 4));
        emitter.Dispatch(new OverscrollNotification(Metrics(), 20, dragDetails: details));

        Assert.Equal(2, confirmations);
    }

    [Fact]
    public void Glow_HonorsDisallowIndicatorAxisAndVisibilityPolicy()
    {
        var emitterKey = new LabeledGlobalKey<NotificationEmitterState>("blocked-glow-emitter");
        using var harness = new WidgetRenderHarness(Wrap(
            new NotificationListener<OverscrollIndicatorNotification>(
                onNotification: notification =>
                {
                    notification.DisallowIndicator();
                    return false;
                },
                child: new GlowingOverscrollIndicator(
                    axisDirection: AxisDirection.Down,
                    color: Colors.Red,
                    showLeading: false,
                    child: new NotificationEmitter(key: emitterKey)))));
        harness.Pump(Viewport);
        NotificationEmitterState emitter = Assert.IsType<NotificationEmitterState>(emitterKey.CurrentState);
        var details = new DragUpdateDetails(default, new Point(40, 20), default, 20);

        emitter.Dispatch(new OverscrollNotification(Metrics(), -20, dragDetails: details));
        emitter.Dispatch(new OverscrollNotification(
            Metrics(axisDirection: AxisDirection.Right),
            20,
            dragDetails: details));
        PumpAnimation(harness, TimeSpan.FromMilliseconds(100));

        GlowingOverscrollIndicatorState state = harness.FindState<GlowingOverscrollIndicatorState>();
        var customPaint = harness.FindRenderObject<RenderCustomPaint>();
        var painter = Assert.IsType<GlowingOverscrollIndicatorPainter>(customPaint.ForegroundPainter);
        Assert.Null(painter.LeadingController);
        Assert.Same(state.TrailingController, painter.TrailingController);
        Assert.Equal(GlowState.Idle, state.LeadingController.State);
        Assert.Equal(GlowState.Idle, state.TrailingController.State);
    }

    [Fact]
    public void GlowPainter_OrientsLeadingHorizontalGlowAtTheLeftEdge()
    {
        var controller = new GlowController(Colors.Blue, Axis.Horizontal);
        using var repaint = new MergedListenable(controller);
        var painter = new GlowingOverscrollIndicatorPainter(
            leadingController: controller,
            trailingController: null,
            axisDirection: AxisDirection.Right,
            repaint: repaint);
        controller.Pull(
            overscroll: 30,
            extent: 200,
            crossAxisOffset: 50,
            crossExtent: 100);
        AnimationPump.Advance(0.10);
        var rootLayer = new ContainerLayer();

        painter.Paint(new PaintingContext(rootLayer), new Size(240, 300));

        var orientation = Assert.IsType<TransformLayer>(Assert.Single(rootLayer.Children));
        Assert.Equal(0.0, orientation.Transform[0]);
        Assert.Equal(1.0, orientation.Transform[1]);
        Assert.Equal(1.0, orientation.Transform[4]);
        Assert.Equal(0.0, orientation.Transform[5]);
        controller.Dispose();
    }

    [Fact]
    public void Stretch_AppliesSourceStrengthOriginAndViewportClip()
    {
        var emitterKey = new LabeledGlobalKey<NotificationEmitterState>("stretch-emitter");
        using var harness = new WidgetRenderHarness(Wrap(
            new StretchingOverscrollIndicator(
                axisDirection: AxisDirection.Down,
                clipBehavior: Clip.AntiAlias,
                child: new NotificationEmitter(key: emitterKey))));
        harness.Pump(Viewport);
        NotificationEmitterState emitter = Assert.IsType<NotificationEmitterState>(emitterKey.CurrentState);
        var details = new DragUpdateDetails(default, new Point(120, 20), default, 40);

        emitter.Dispatch(new OverscrollNotification(
            Metrics(viewportDimension: 100),
            overscroll: -40,
            dragDetails: details));
        harness.Pump(Viewport);

        StretchingOverscrollIndicatorState state =
            harness.FindState<StretchingOverscrollIndicatorState>();
        RenderTransform transform = harness.FindRenderObject<RenderTransform>();
        RenderClipRect clip = harness.FindRenderObject<RenderClipRect>();
        Assert.True(state.StretchController.Overscroll < 0.0);
        Assert.True(transform.Transform[5] > 1.0);
        Assert.Equal(Alignment.TopCenter, transform.Alignment);
        Assert.Equal(FilterQuality.Medium, transform.FilterQuality);
        Assert.Equal(Clip.AntiAlias, clip.ClipBehavior);

        emitter.Dispatch(new ScrollEndNotification(
            Metrics(viewportDimension: 100),
            new DragEndDetails(Velocity.Zero, 0.0)));
        PumpAnimation(harness, TimeSpan.FromSeconds(2.1));

        transform = harness.FindRenderObject<RenderTransform>();
        clip = harness.FindRenderObject<RenderClipRect>();
        Assert.Equal(1.0, transform.Transform[5], precision: 6);
        Assert.Equal(Clip.None, clip.ClipBehavior);
    }

    [Fact]
    public void Stretch_DisallowAndDepthPredicatePreventTransform()
    {
        var emitterKey = new LabeledGlobalKey<NotificationEmitterState>("blocked-stretch-emitter");
        int confirmations = 0;
        using var harness = new WidgetRenderHarness(Wrap(
            new NotificationListener<OverscrollIndicatorNotification>(
                onNotification: notification =>
                {
                    confirmations += 1;
                    notification.DisallowIndicator();
                    return false;
                },
                child: new StretchingOverscrollIndicator(
                    axisDirection: AxisDirection.Down,
                    child: new NotificationEmitter(key: emitterKey)))));
        harness.Pump(Viewport);
        NotificationEmitterState emitter = Assert.IsType<NotificationEmitterState>(emitterKey.CurrentState);
        var details = new DragUpdateDetails(default, new Point(120, 20), default, 20);

        emitter.Dispatch(new OverscrollNotification(Metrics(), -20, dragDetails: details));
        emitter.Dispatch(new OverscrollNotification(
            Metrics(),
            -20,
            dragDetails: details,
            depth: 1));
        harness.Pump(Viewport);

        StretchingOverscrollIndicatorState state =
            harness.FindState<StretchingOverscrollIndicatorState>();
        Assert.Equal(1, confirmations);
        Assert.Equal(0.0, state.StretchController.Overscroll);
        Assert.Equal(1.0, harness.FindRenderObject<RenderTransform>().Transform[5]);
    }

    [Fact]
    public void MaterialScrollBehavior_SelectsStretchGlowAndDesktopPolicies()
    {
        Widget? materialThreeResult = null;
        Widget? desktopResult = null;
        var child = new SizedBox(width: 20, height: 20);
        var behavior = new MaterialScrollBehavior();
        var details = ScrollableDetails.Vertical(
            decorationClipBehavior: Clip.AntiAlias);
        var theme = ThemeData.Light with
        {
            Platform = TargetPlatform.Android,
            UseMaterial3 = true,
            SecondaryColor = Colors.Orange,
        };

        using var harness = new WidgetRenderHarness(Wrap(
            new Builder(context =>
            {
                materialThreeResult = behavior.BuildOverscrollIndicator(
                    context,
                    child,
                    details);
                desktopResult = new FixedPlatformMaterialScrollBehavior(
                    TargetPlatform.Windows).BuildOverscrollIndicator(
                    context,
                    child,
                    details);
                return materialThreeResult;
            }),
            theme: theme));
        harness.Pump(Viewport);

        var stretch = Assert.IsType<StretchingOverscrollIndicator>(materialThreeResult);
        Assert.Equal(Clip.AntiAlias, stretch.ClipBehavior);
        Assert.Same(child, stretch.Child);
        Assert.Same(child, desktopResult);

        Widget? glowResult = null;
        using var glowHarness = new WidgetRenderHarness(Wrap(
            new Builder(context =>
            {
                glowResult = behavior.BuildOverscrollIndicator(context, child, details);
                return glowResult;
            }),
            theme: theme with { UseMaterial3 = false }));
        glowHarness.Pump(Viewport);

        var glow = Assert.IsType<GlowingOverscrollIndicator>(glowResult);
        Assert.Equal(Colors.Orange, glow.Color);
    }

    [Fact]
    public void Glow_ReceivesRealScrollableDragDetails()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            new GlowingOverscrollIndicator(
                axisDirection: AxisDirection.Down,
                color: Colors.Blue,
                child: new ScrollConfiguration(
                    // Android is pinned explicitly: the glow indicator belongs to clamping physics,
                    // and the host platform default would otherwise supply bouncing physics, which
                    // absorb the overscroll instead of reporting it.
                    behavior: new FixedPlatformScrollBehavior(TargetPlatform.Android).CopyWith(
                        dragDevices: new HashSet<PointerDeviceKind>
                        {
                            PointerDeviceKind.Mouse,
                            PointerDeviceKind.Touch,
                        }),
                    child: ListView.Builder(
                        itemCount: 20,
                        itemExtent: 44,
                        addAutomaticKeepAlives: false,
                        itemBuilder: (_, index) =>
                            new SizedBox(height: 44, child: new Text($"row {index}")))))));
        harness.Pump(Viewport);
        var binding = GestureBinding.Instance;
        DateTime now = DateTime.UtcNow;

        binding.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(
                712,
                PointerDeviceKind.Mouse,
                new Point(120, 80),
                PointerButtons.Primary,
                now));
        binding.HandlePointerEvent(
            harness.RenderView,
            new PointerMoveEvent(
                712,
                PointerDeviceKind.Mouse,
                new Point(120, 120),
                PointerButtons.Primary,
                down: true,
                now.AddMilliseconds(16)));
        binding.HandlePointerEvent(
            harness.RenderView,
            new PointerMoveEvent(
                712,
                PointerDeviceKind.Mouse,
                new Point(120, 210),
                PointerButtons.Primary,
                down: true,
                now.AddMilliseconds(32)));
        PumpAnimation(harness, TimeSpan.FromMilliseconds(100));

        GlowingOverscrollIndicatorState state = harness.FindState<GlowingOverscrollIndicatorState>();
        Assert.True(state.LeadingController.GlowOpacity > 0.0);
        Assert.True(state.LeadingController.Displacement > 0.0);

        binding.HandlePointerEvent(
            harness.RenderView,
            new PointerUpEvent(
                712,
                PointerDeviceKind.Mouse,
                new Point(120, 210),
                PointerButtons.None,
                now.AddMilliseconds(48)));
    }

    private static FixedScrollMetrics Metrics(
        double viewportDimension = 200,
        AxisDirection axisDirection = AxisDirection.Down)
    {
        return new FixedScrollMetrics(
            minScrollExtent: 0,
            maxScrollExtent: 600,
            pixels: 0,
            viewportDimension: viewportDimension,
            axisDirection: axisDirection,
            devicePixelRatio: 1.0);
    }

    private static Widget Wrap(
        Widget child,
        TextDirection direction = TextDirection.Ltr,
        ThemeData? theme = null)
    {
        return new Directionality(
            direction,
            new MediaQuery(
                new MediaQueryData(Size: Viewport),
                new Theme(theme ?? ThemeData.Light, child)));
    }

    private static void PumpAnimation(WidgetRenderHarness harness, TimeSpan elapsed)
    {
        AnimationPump.Advance(elapsed.TotalSeconds);
        harness.Pump(Viewport);
    }

    private sealed class NotificationEmitter : StatefulWidget
    {
        public NotificationEmitter(Key? key = null) : base(key)
        {
        }

        public override State CreateState() => new NotificationEmitterState();
    }

    private sealed class NotificationEmitterState : State
    {
        public void Dispatch(Notification notification)
        {
            notification.Dispatch(Context);
        }

        public override Widget Build(BuildContext context)
        {
            return new SizedBox(width: 200, height: 200);
        }
    }

    private sealed class FixedPlatformScrollBehavior(TargetPlatform platform) : ScrollBehavior
    {
        public override TargetPlatform GetPlatform(BuildContext context) => platform;
    }

    private sealed class FixedPlatformMaterialScrollBehavior(TargetPlatform platform)
        : MaterialScrollBehavior
    {
        public override TargetPlatform GetPlatform(BuildContext context) => platform;
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

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

        public T FindState<T>() where T : State
        {
            var states = new List<T>();
            CollectStates(_rootElement, states);
            return Assert.Single(states);
        }

        public T FindRenderObject<T>() where T : RenderObject
        {
            T? result = FindRenderObject<T>(RenderView);
            return Assert.IsType<T>(result);
        }

        public void Dispose()
        {
            _rootElement.Unmount();
        }

        private static void CollectStates<T>(Element element, List<T> states) where T : State
        {
            if (element is StatefulElement stateful && stateful.State is T state)
            {
                states.Add(state);
            }

            element.VisitChildren(child => CollectStates(child, states));
        }

        private static T? FindRenderObject<T>(RenderObject? root) where T : RenderObject
        {
            if (root is null)
            {
                return null;
            }

            if (root is T match)
            {
                return match;
            }

            T? result = null;
            root.VisitChildren(child => result ??= FindRenderObject<T>(child));
            return result;
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
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(
                RenderObject child,
                object? oldSlot,
                object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }

            internal override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }
}
