using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// `ModalRoute` owns its barrier the way Flutter's `createOverlayEntries` does: the barrier is a sibling
/// painted below the page, outside the route's transition, and sorted after the page in semantics.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class ModalRouteBarrierTests : IDisposable
{
    public ModalRouteBarrierTests()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void ModalRoute_BarrierDefaultsMatchFlutter()
    {
        var route = new TestPopupRoute(_ => new SizedBox());

        Assert.False(route.BarrierDismissible);
        Assert.True(route.SemanticsDismissible);
        Assert.Null(route.BarrierColor);
        Assert.Null(route.BarrierLabel);
        Assert.Equal(Curves.Ease(0.25), route.BarrierCurve(0.25), precision: 9);
    }

    [Fact]
    public void ModalRoute_WithoutABarrierColorStillBuildsATransparentBarrier()
    {
        var route = new TestPopupRoute(_ => new SizedBox());
        var barrier = Assert.IsType<ModalBarrier>(route.BuildModalBarrier());

        Assert.Null(barrier.Color);
        Assert.False(barrier.Dismissible);
        Assert.Null(barrier.SemanticsLabel);
        Assert.True(barrier.BarrierSemanticsDismissible);
    }

    [Fact]
    public void ModalRoute_BarrierColorFollowsTheBarrierCurve()
    {
        var route = new TestPopupRoute(
            _ => new SizedBox(),
            barrierColor: Colors.Black,
            transitionDuration: TimeSpan.FromMilliseconds(200));
        using var harness = PushRoute(route, out _);

        var barrier = Assert.IsType<AnimatedModalBarrier>(route.BuildModalBarrier());
        Assert.Equal(0, barrier.Color.Value!.Value.A);

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.001));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.1));
        harness.Pump(new Size(400, 300));

        double progress = route.Animation.Value;
        Assert.InRange(progress, 0.2, 0.8);
        Assert.Equal(
            (byte)Math.Round(255 * Curves.Ease(progress)),
            barrier.Color.Value!.Value.A);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.5));
        harness.Pump(new Size(400, 300));
        Assert.Equal(255, barrier.Color.Value!.Value.A);
    }

    [Fact]
    public void ModalRoute_BarrierIsPaintedBelowThePageAndOutsideItsTransition()
    {
        var route = new TestPopupRoute(
            _ => new SizedBox(width: 100, height: 100, child: new Text("Page")),
            barrierColor: Colors.Black);
        using var harness = PushRoute(route, out _);
        PumpAnimation();
        harness.Pump(new Size(400, 300));

        // The route's transition builder insets everything it wraps; the barrier still fills the view, so it
        // is a sibling of the page rather than a descendant of the transition.
        Assert.True(route.TransitionCallCount > 0);
        var barrier = Assert.Single(FindDescendants<RenderColoredBox>(harness.RenderView));
        var page = FindParagraph(harness.RenderView, "Page");
        Assert.NotNull(page);
        Assert.Equal(new Size(400, 300), barrier.Size);
        Assert.Equal(20, page.LocalToGlobal(default).X, precision: 3);
        Assert.True(PaintsBefore(harness.RenderView, barrier, page));
    }

    [Fact]
    public void ModalRoute_BarrierSemanticsSortAfterThePage()
    {
        var route = new TestPopupRoute(
            _ => new Semantics(label: "Page", container: true, child: new SizedBox(width: 50, height: 50)),
            barrierColor: Colors.Black,
            barrierDismissible: true,
            barrierLabel: "Dismiss");
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        try
        {
            using var harness = PushRoute(route, out _);
            PumpAnimation();
            var semantics = harness.PumpAndGetSemantics(new Size(400, 300));

            var labels = CollectSemantics(semantics)
                .Select(node => node.Label)
                .Where(label => label is "Page" or "Dismiss")
                .ToList();
            Assert.Equal(["Page", "Dismiss"], labels);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void ModalRoute_DismissibleBarrierPopsAndNonDismissibleKeepsTheRoute()
    {
        var dismissible = new TestPopupRoute(
            _ => new SizedBox(width: 40, height: 40, child: new Text("Page")),
            barrierColor: Colors.Black,
            barrierDismissible: true);
        using (var harness = PushRoute(dismissible, out var context))
        {
            PumpAnimation();
            harness.Pump(new Size(400, 300));
            TapAt(harness, new Point(390, 290));
            PumpAnimation();
            harness.Pump(new Size(400, 300));
            Assert.Null(FindParagraph(harness.RenderView, "Page"));
            Assert.False(Navigator.Of(context).CanPop);
        }

        var blocking = new TestPopupRoute(
            _ => new SizedBox(width: 40, height: 40, child: new Text("Page")),
            barrierColor: Colors.Black);
        using (var harness = PushRoute(blocking, out _))
        {
            PumpAnimation();
            harness.Pump(new Size(400, 300));
            TapAt(harness, new Point(390, 290));
            PumpAnimation();
            harness.Pump(new Size(400, 300));
            Assert.NotNull(FindParagraph(harness.RenderView, "Page"));
        }
    }

    [Fact]
    public void ModalRoute_BarrierIgnoresPointersWhileTheRouteAnimatesOut()
    {
        var route = new TestPopupRoute(
            _ => new SizedBox(width: 40, height: 40, child: new Text("Page")),
            barrierColor: Colors.Black,
            barrierDismissible: true,
            transitionDuration: TimeSpan.FromMilliseconds(400));
        using var harness = PushRoute(route, out var context);
        PumpAnimation();
        harness.Pump(new Size(400, 300));
        Assert.False(SingleIgnorePointer(harness).Ignoring);

        Navigator.Of(context).Pop();
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.001));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.1));
        harness.Pump(new Size(400, 300));

        Assert.Equal(AnimationStatus.Reverse, route.Animation.Status);
        Assert.True(SingleIgnorePointer(harness).Ignoring);
    }

    /// <summary>
    /// The route below owns a barrier too, so the colored one identifies the pushed route's barrier.
    /// </summary>
    private static RenderIgnorePointer SingleIgnorePointer(WidgetRenderHarness harness) => Assert.Single(
        FindDescendants<RenderIgnorePointer>(harness.RenderView),
        ignorePointer => FindDescendants<RenderColoredBox>(ignorePointer).Count > 0);

    private static bool PaintsBefore(RenderObject root, RenderObject first, RenderObject second)
    {
        var order = new List<RenderObject>();
        Visit(root);
        return order.IndexOf(first) < order.IndexOf(second);

        void Visit(RenderObject node)
        {
            order.Add(node);
            node.VisitChildren(Visit);
        }
    }

    private static WidgetRenderHarness PushRoute(Route route, out BuildContext context)
    {
        BuildContext captured = default;
        var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(Size: new Size(400, 300)),
                new Navigator(new BuilderPageRoute(_ => new CaptureContext(
                    value => captured = value,
                    child: new Text("Underlying")))))));
        harness.Pump(new Size(400, 300));
        Navigator.Of(captured).Push(route);
        harness.Pump(new Size(400, 300));
        context = captured;
        return harness;
    }

    private static void PumpAnimation()
    {
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.30));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.60));
    }

    private static void TapAt(WidgetRenderHarness harness, Point position)
    {
        var binding = GestureBinding.Instance;
        var now = DateTime.UtcNow;
        binding.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(99, PointerDeviceKind.Mouse, position, PointerButtons.Primary, now));
        binding.HandlePointerEvent(
            harness.RenderView,
            new PointerUpEvent(99, PointerDeviceKind.Mouse, position, PointerButtons.None, now.AddMilliseconds(20)));
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(value => value.PlainText == text);

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T target) result.Add(target);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static List<SemanticsNode> CollectSemantics(SemanticsNode? node)
    {
        var result = new List<SemanticsNode>();
        if (node is null) return result;
        result.Add(node);
        // Sort keys reorder the traversal order, which is what assistive technologies read.
        foreach (var child in node.ChildrenInTraversalOrder)
        {
            result.AddRange(CollectSemantics(child));
        }

        return result;
    }

    private sealed class TestPopupRoute : PopupRoute
    {
        private readonly WidgetBuilder _builder;
        private readonly TimeSpan _transitionDuration;

        public TestPopupRoute(
            WidgetBuilder builder,
            Color? barrierColor = null,
            bool barrierDismissible = false,
            string? barrierLabel = null,
            TimeSpan? transitionDuration = null)
        {
            _builder = builder;
            BarrierColor = barrierColor;
            BarrierDismissible = barrierDismissible;
            BarrierLabel = barrierLabel;
            _transitionDuration = transitionDuration ?? TimeSpan.FromMilliseconds(200);
        }

        public override Color? BarrierColor { get; }
        public override bool BarrierDismissible { get; }
        public override string? BarrierLabel { get; }
        public override TimeSpan TransitionDuration => _transitionDuration;
        public int TransitionCallCount { get; private set; }

        public override Widget BuildPage(BuildContext context) => _builder(context);

        public override Widget BuildTransitions(
            BuildContext context,
            Animation<double> animation,
            Animation<double> secondaryAnimation,
            Widget child)
        {
            TransitionCallCount++;
            return new Padding(new Thickness(20), child);
        }
    }

    private sealed class CaptureContext : StatelessWidget
    {
        private readonly Action<BuildContext> _capture;
        private readonly Widget _child;

        public CaptureContext(Action<BuildContext> capture, Widget child)
        {
            _capture = capture;
            _child = child;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(context);
            return _child;
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _root;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new HarnessRootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(null, null);
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

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner.RootNode;
        }

        public void Dispose() => _root.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;

            public HarnessRootElement(RenderView view, Widget widget) : base(widget) => _view = view;

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
                if (ReferenceEquals(_child, child)) _child = null;
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null) visitor(_child);
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot) => _view.Child = (RenderBox)child;

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_view.Child, child)) _view.Child = null;
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
