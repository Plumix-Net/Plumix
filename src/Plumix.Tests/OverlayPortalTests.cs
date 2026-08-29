using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/overlay.dart

/// <summary>
/// Covers the deferred-layout half of <c>OverlayPortal</c>: the render objects Flutter calls
/// <c>_RenderDeferredLayoutBox</c> and <c>_RenderLayoutSurrogateProxyBox</c>, and the theater
/// bookkeeping that keeps adding and removing an overlay child from dirtying the overlay's layout.
/// </summary>
public sealed class OverlayPortalTests
{
    [Fact]
    public void OverlayChild_IsADeferredLayoutBoxDeeperThanItsLayoutSurrogate()
    {
        var controller = new OverlayPortalController();
        controller.Show();
        using var harness = new PortalHarness(BuildEntry(controller));

        RenderDeferredLayoutBox deferred = harness.FindRenderObject<RenderDeferredLayoutBox>();
        RenderOverlayPortalSurrogate surrogate = harness.FindRenderObject<RenderOverlayPortalSurrogate>();
        RenderOverlayTheater theater = harness.FindRenderObject<RenderOverlayTheater>();

        Assert.Same(surrogate, deferred.LayoutSurrogate);
        Assert.Same(deferred, surrogate.DeferredLayoutChild);
        Assert.Same(theater, deferred.Parent);

        // The depth invariant is what makes the pipeline owner lay the deferred box out after both the
        // nodes it reads: `depth(theater) < depth(surrogate) < depth(deferredBox)`.
        Assert.True(theater.Depth < surrogate.Depth);
        Assert.True(surrogate.Depth < deferred.Depth);
    }

    [Fact]
    public void OverlayChild_IsLaidOutAfterTheLayoutSurrogate()
    {
        var order = new List<string>();
        var controller = new OverlayPortalController();
        controller.Show();
        using var harness = new PortalHarness(BuildEntry(
            controller,
            overlayChild: new LayoutProbe(() => order.Add("overlayChild"), new SizedBox(width: 10, height: 10)),
            child: new LayoutProbe(() => order.Add("child"), new SizedBox(width: 40, height: 30)),
            positioned: false));

        Assert.Equal(["child", "overlayChild"], order);

        // A second pass under different constraints has to keep the order: the deferred box is laid out
        // by the pipeline owner after the surrogate, never through the theater's tree walk.
        order.Clear();
        harness.Pump(new Size(200, 100));
        Assert.Equal(["child", "overlayChild"], order);
    }

    [Fact]
    public void OverlayChild_IsSizedToTheOverlayAndCanUsePositioned()
    {
        var controller = new OverlayPortalController();
        controller.Show();
        using var harness = new PortalHarness(BuildEntry(
            controller,
            overlayChild: new Positioned(
                left: 70,
                top: 20,
                width: 25,
                height: 15,
                child: new ColoredBox(Colors.MediumPurple))));

        RenderDeferredLayoutBox deferred = harness.FindRenderObject<RenderDeferredLayoutBox>();
        Assert.Equal(PortalHarness.ViewSize, deferred.Size);

        RenderColoredBox positioned = harness.FindRenderObject<RenderColoredBox>();
        Assert.Equal(new Size(25, 15), positioned.Size);
        Assert.Equal(new Point(70, 20), ((BoxParentData)positioned.parentData!).offset);
    }

    [Fact]
    public void AddingAndRemovingAnOverlayChild_DoesNotRelayoutTheOverlayMoreThanOnce()
    {
        var controller = new OverlayPortalController();
        using var harness = new PortalHarness(BuildEntry(controller));

        RenderOverlayTheater theater = harness.FindRenderObject<RenderOverlayTheater>();
        int layoutsBefore = harness.TheaterLayoutCount;

        controller.Show();
        harness.Pump();
        Assert.NotNull(harness.FindRenderObjects<RenderDeferredLayoutBox>().SingleOrDefault());
        Assert.Equal(layoutsBefore + 1, harness.TheaterLayoutCount);

        controller.Hide();
        harness.Pump();
        Assert.Empty(harness.FindRenderObjects<RenderDeferredLayoutBox>());
        Assert.Equal(layoutsBefore + 2, harness.TheaterLayoutCount);

        // The theater is clean afterwards: adopting or dropping a deferred child must not leave it
        // needing layout.
        Assert.False(theater.NeedsLayout);
    }

    [Fact]
    public void AddingAnOverlayChild_RepaintsTheOverlayWithoutDirtyingItsLayout()
    {
        var controller = new OverlayPortalController();
        using var harness = new PortalHarness(BuildEntry(controller));
        RenderOverlayTheater theater = harness.FindRenderObject<RenderOverlayTheater>();

        harness.FlushPaintFlags();
        Assert.False(theater.NeedsPaint);

        controller.Show();
        harness.FlushBuild();

        // Flutter's `_RenderTheater._addDeferredChild` suppresses `markNeedsLayout` and issues an
        // explicit `markNeedsPaint` in its place.
        Assert.False(theater.NeedsLayout);
        Assert.True(theater.NeedsPaint);
    }

    [Fact]
    public void RemovingAnOverlayChild_RepaintsTheOverlayWithoutDirtyingItsLayout()
    {
        var controller = new OverlayPortalController();
        controller.Show();
        using var harness = new PortalHarness(BuildEntry(controller));
        RenderOverlayTheater theater = harness.FindRenderObject<RenderOverlayTheater>();

        harness.FlushPaintFlags();
        Assert.False(theater.NeedsPaint);

        controller.Hide();
        harness.FlushBuild();

        Assert.False(theater.NeedsLayout);
        Assert.True(theater.NeedsPaint);
    }

    [Fact]
    public void OverlayChild_RemainsReachableWhenThePortalIsNotARelayoutBoundary()
    {
        var controller = new OverlayPortalController();
        controller.Show();

        // A `Positioned` portal takes loose constraints and its parent uses its size, so there is no
        // relayout boundary between the theater and the portal.
        using var harness = new PortalHarness(BuildEntry(controller, positioned: false));

        RenderDeferredLayoutBox deferred = harness.FindRenderObject<RenderDeferredLayoutBox>();
        Assert.True(deferred.Attached);
        Assert.False(deferred.NeedsLayout);

        var visited = new List<RenderObject>();
        harness.FindRenderObject<RenderOverlayTheater>().VisitChildren(visited.Add);
        Assert.Contains(deferred, visited);
    }

    [Fact]
    public void OverlayChild_HitTestsAtItsOverlayPosition()
    {
        var controller = new OverlayPortalController();
        controller.Show();
        using var harness = new PortalHarness(BuildEntry(
            controller,
            overlayChild: new Positioned(
                left: 100,
                top: 60,
                width: 20,
                height: 20,
                child: new ColoredBox(Colors.MediumPurple))));

        RenderColoredBox overlayChild = harness.FindRenderObject<RenderColoredBox>();

        var hitInside = new BoxHitTestResult();
        harness.HitTest(hitInside, new Point(110, 70));
        Assert.Contains(hitInside.Path, entry => ReferenceEquals(entry.Target, overlayChild));

        var hitOutside = new BoxHitTestResult();
        harness.HitTest(hitOutside, new Point(10, 10));
        Assert.DoesNotContain(hitOutside.Path, entry => ReferenceEquals(entry.Target, overlayChild));
    }

    [Fact]
    public void OverlayChildLayoutBuilder_SeesTheAnchorGeometryOfTheSameFrame()
    {
        var controller = new OverlayPortalController();
        controller.Show();
        OverlayChildLayoutInfo? info = null;
        int builds = 0;

        var entry = new OverlayEntry(_ => new Stack(
            fit: StackFit.Expand,
            children:
            [
                new Positioned(
                    left: 20,
                    top: 10,
                    width: 40,
                    height: 30,
                    child: OverlayPortal.WithLayoutBuilder(
                        controller: controller,
                        overlayChildBuilder: (_, layoutInfo) =>
                        {
                            builds++;
                            info = layoutInfo;
                            return new SizedBox();
                        },
                        child: new SizedBox())),
            ]));

        using var harness = new PortalHarness(entry);

        Assert.Equal(1, builds);
        Assert.NotNull(info);
        Assert.Equal(new Size(40, 30), info.ChildSize);
        Assert.Equal(PortalHarness.ViewSize, info.OverlaySize);
        Assert.Equal(
            new Point(20, 10),
            MatrixUtils.TransformPoint(info.ChildPaintTransform, new Point()));

        // Unchanged layout info does not rebuild the overlay child.
        harness.FindRenderObject<RenderOverlayPortalLayoutBuilder>().MarkNeedsLayout();
        harness.Pump();
        Assert.Equal(1, builds);
    }

    private static OverlayEntry BuildEntry(
        OverlayPortalController controller,
        Widget? overlayChild = null,
        Widget? child = null,
        bool positioned = true)
    {
        Widget portal = new OverlayPortal(
            controller: controller,
            overlayChildBuilder: _ => overlayChild ?? new SizedBox(width: 10, height: 10),
            child: child ?? new SizedBox(width: 40, height: 30));

        return new OverlayEntry(_ => new Stack(
            fit: StackFit.Expand,
            children:
            [
                positioned
                    ? new Positioned(left: 20, top: 10, width: 40, height: 30, child: portal)
                    : portal,
            ]));
    }

    private sealed class LayoutProbe : SingleChildRenderObjectWidget
    {
        public LayoutProbe(Action onPerformLayout, Widget? child = null) : base(child)
        {
            OnPerformLayout = onPerformLayout;
        }

        public Action OnPerformLayout { get; }

        internal override RenderObject CreateRenderObject(BuildContext context)
        {
            return new RenderLayoutProbe { OnPerformLayout = OnPerformLayout };
        }

        internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
        {
            ((RenderLayoutProbe)renderObject).OnPerformLayout = OnPerformLayout;
        }
    }

    private sealed class RenderLayoutProbe : RenderProxyBox
    {
        public Action? OnPerformLayout { get; set; }

        protected override void PerformLayout()
        {
            OnPerformLayout?.Invoke();
            base.PerformLayout();
        }
    }

    private sealed class PortalHarness : IDisposable
    {
        public static readonly Size ViewSize = new(240, 120);

        private readonly BuildOwner _owner = new();
        private readonly TestRootElement _root;
        private readonly RenderView _renderView;
        private readonly PipelineOwner _pipeline;
        private readonly CountingTheaterHost _host = new();

        public PortalHarness(OverlayEntry entry)
        {
            _root = new TestRootElement(new Overlay(initialEntries: [entry]));
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
            _renderView = new RenderView
            {
                Child = Assert.IsAssignableFrom<RenderBox>(_root.ChildElement?.RenderObject),
            };
            _pipeline = new PipelineOwner(_renderView);
            _pipeline.Attach(_renderView);
            Pump();
        }

        public int TheaterLayoutCount => _host.Count;

        public void Pump(Size? size = null)
        {
            _owner.FlushBuild();
            if (size is not null)
            {
                _renderView.MarkNeedsLayout();
            }

            _host.Observe(FindRenderObject<RenderOverlayTheater>());
            _pipeline.FlushLayout(size ?? ViewSize);
        }

        public void FlushBuild() => _owner.FlushBuild();

        /// <summary>Lays out and paints, so the dirty flags start from a known state.</summary>
        public void FlushPaintFlags()
        {
            Pump();
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void HitTest(BoxHitTestResult result, Point position)
        {
            ((RenderBox)_renderView.Child!).HitTest(result, position);
        }

        public T FindRenderObject<T>() where T : RenderObject
        {
            return Assert.IsType<T>(FindRenderObjects<T>().FirstOrDefault());
        }

        public IReadOnlyList<T> FindRenderObjects<T>() where T : RenderObject
        {
            var results = new List<T>();
            Visit(_renderView);
            return results;

            void Visit(RenderObject renderObject)
            {
                if (renderObject is T typed)
                {
                    results.Add(typed);
                }

                renderObject.VisitChildren(Visit);
            }
        }

        public void Dispose()
        {
            _root.Unmount();
        }
    }

    /// <summary>Counts the layout passes the theater has been through.</summary>
    private sealed class CountingTheaterHost
    {
        public int Count { get; private set; }

        public void Observe(RenderOverlayTheater theater)
        {
            Assert.NotNull(theater);
            Count += 1;
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public Element? ChildElement => _child;

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

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
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

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }
}
