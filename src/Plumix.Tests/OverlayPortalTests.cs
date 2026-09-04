using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
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

    [Fact]
    public void Theater_AdoptsOverlayChildrenWithoutListingThemAmongItsChildren()
    {
        var controller = new OverlayPortalController();
        controller.Show();
        using var harness = new PortalHarness(BuildEntry(controller));

        RenderOverlayTheater theater = harness.FindRenderObject<RenderOverlayTheater>();
        RenderDeferredLayoutBox overlayChild = harness.FindRenderObject<RenderDeferredLayoutBox>();

        // Flutter adopts the deferred box outside the theater's sibling chain, so the container API
        // never reports it - which is what makes removing one mid-walk safe.
        Assert.Equal(1, theater.ChildCount);
        Assert.Same(theater.FirstChild, theater.LastChild);
        Assert.NotSame(overlayChild, theater.FirstChild);
        Assert.Null(theater.ChildAfter(theater.FirstChild!));
        Assert.Same(theater, overlayChild.Parent);

        // The tree walk, on the other hand, has to reach it: attach, detach and redepth all run
        // through it.
        Assert.Equal<RenderObject>([theater.FirstChild!, overlayChild], VisitedChildren(theater));
        Assert.True(overlayChild.Attached);
    }

    [Fact]
    public void OverlayChild_PaintsAfterItsHostEntryAndBeforeTheNextEntry()
    {
        var order = new List<string>();
        var controller = new OverlayPortalController();
        controller.Show();
        var lower = new OverlayEntry(_ => new PaintProbe(
            () => order.Add("lower"),
            new Stack(
                fit: StackFit.Expand,
                children:
                [
                    new OverlayPortal(
                        controller: controller,
                        overlayChildBuilder: _ => new PaintProbe(() => order.Add("overlayChild")),
                        child: new SizedBox(width: 10, height: 10)),
                ])));
        var upper = new OverlayEntry(_ => new PaintProbe(() => order.Add("upper")));

        using var harness = new PortalHarness(lower, upper);
        harness.FlushPaint();

        Assert.Equal(["lower", "overlayChild", "upper"], order);
    }

    [Fact]
    public void OverlayChildrenOfOneEntry_PaintInAscendingZOrderAndShowBringsToTop()
    {
        var order = new List<string>();
        var first = new OverlayPortalController();
        var second = new OverlayPortalController();
        first.Show();
        second.Show();

        var entry = new OverlayEntry(_ => new Stack(
            fit: StackFit.Expand,
            children:
            [
                new OverlayPortal(
                    controller: first,
                    overlayChildBuilder: _ => new PaintProbe(() => order.Add("first")),
                    child: new SizedBox(width: 10, height: 10)),
                new OverlayPortal(
                    controller: second,
                    overlayChildBuilder: _ => new PaintProbe(() => order.Add("second")),
                    child: new SizedBox(width: 10, height: 10)),
            ]));

        using var harness = new PortalHarness(entry);
        harness.FlushPaint();
        Assert.Equal(["first", "second"], order);

        // Showing an already-showing portal hands it a fresh, larger z-order index, which moves its
        // location to the tail of its entry's sorted sibling list.
        order.Clear();
        first.Show();
        harness.FlushPaint();
        Assert.Equal(["second", "first"], order);
    }

    [Fact]
    public void ReparentingAPortal_KeepsItsOverlayChildAtTheSamePlaceInTheChildModel()
    {
        var order = new List<string>();
        var first = new OverlayPortalController();
        var second = new OverlayPortalController();
        first.Show();
        second.Show();

        var movedKey = new LabeledGlobalKey<OverlayPortalState>("moved portal");
        bool moved = false;
        Widget BuildFirstPortal() => new OverlayPortal(
            controller: first,
            overlayChildBuilder: _ => new PaintProbe(() => order.Add("first")),
            child: new SizedBox(width: 10, height: 10),
            key: movedKey);

        var entry = new OverlayEntry(_ => new Stack(
            fit: StackFit.Expand,
            children:
            [
                moved
                    ? BuildFirstPortal()
                    : new Padding(EdgeInsets.All(1), BuildFirstPortal()),
                new OverlayPortal(
                    controller: second,
                    overlayChildBuilder: _ => new PaintProbe(() => order.Add("second")),
                    child: new SizedBox(width: 10, height: 10)),
            ]));

        using var harness = new PortalHarness(entry);
        harness.FlushPaint();
        Assert.Equal(["first", "second"], order);

        // Deactivating and re-activating the portal detaches its overlay child from the layout
        // surrogate and puts it back; the location stays in the entry's sorted sibling list the whole
        // time, so the z-order - and therefore the paint order - is unchanged.
        order.Clear();
        moved = true;
        entry.MarkNeedsBuild();
        harness.FlushPaint();

        Assert.Equal(["first", "second"], order);
        RenderOverlayTheater theater = harness.FindRenderObject<RenderOverlayTheater>();
        Assert.Equal(1, theater.ChildCount);
        Assert.Equal(3, VisitedChildren(theater).Count);
        Assert.All(harness.FindRenderObjects<RenderDeferredLayoutBox>(), box => Assert.True(box.Attached));
    }

    [Fact]
    public void OverlayChild_HitTestsBeforeTheEntryItIsPaintedOver()
    {
        var controller = new OverlayPortalController();
        controller.Show();
        var entry = new OverlayEntry(_ => new Stack(
            fit: StackFit.Expand,
            children:
            [
                new ColoredBox(Colors.SeaGreen),
                new OverlayPortal(
                    controller: controller,
                    overlayChildBuilder: _ => new ColoredBox(Colors.MediumPurple),
                    child: new SizedBox(width: 10, height: 10)),
            ]));

        using var harness = new PortalHarness(entry);
        IReadOnlyList<RenderColoredBox> boxes = harness.FindRenderObjects<RenderColoredBox>();
        Assert.Equal(2, boxes.Count);

        var result = new BoxHitTestResult();
        harness.HitTest(result, new Point(20, 20));

        // Hit-test order is the exact reverse of the paint order, so the overlay child is reached
        // before the entry content it covers, and the theater stops there.
        Assert.Contains(result.Path, hit => ReferenceEquals(hit.Target, boxes[1]));
        Assert.DoesNotContain(result.Path, hit => ReferenceEquals(hit.Target, boxes[0]));
    }

    [Fact]
    public void OffstageEntry_KeepsItsOverlayChildOutOfSemanticsButStillInTheTreeWalk()
    {
        var controller = new OverlayPortalController();
        controller.Show();
        var hidden = new OverlayEntry(
            _ => new Stack(
                fit: StackFit.Expand,
                children:
                [
                    new OverlayPortal(
                        controller: controller,
                        overlayChildBuilder: _ => new SizedBox(width: 10, height: 10),
                        child: new SizedBox(width: 10, height: 10)),
                ]),
            maintainState: true);
        var opaque = new OverlayEntry(_ => new SizedBox(), opaque: true);

        using var harness = new PortalHarness(hidden, opaque);
        RenderOverlayTheater theater = harness.FindRenderObject<RenderOverlayTheater>();
        RenderDeferredLayoutBox overlayChild = harness.FindRenderObject<RenderDeferredLayoutBox>();

        Assert.Equal(1, theater.SkipCount);
        Assert.Contains(overlayChild, VisitedChildren(theater));
        Assert.DoesNotContain(overlayChild, SemanticsChildren(theater));
        Assert.Equal(theater.LastChild, Assert.Single(SemanticsChildren(theater)));

        // The offstage entry and the overlay child it hosts are described as offstage, numbered from
        // the first offstage entry.
        Assert.Equal(
            ["onstage 1", "offstage 1", "offstage 1 - 1"],
            theater.DebugDescribeChildren().Select(node => node.Name ?? string.Empty));
    }

    [Fact]
    public void Theater_DebugDescribesOverlayChildrenUnderTheEntryThatHostsThem()
    {
        var controller = new OverlayPortalController();
        controller.Show();
        using var harness = new PortalHarness(BuildEntry(controller));

        RenderOverlayTheater theater = harness.FindRenderObject<RenderOverlayTheater>();
        List<DiagnosticsNode> children = theater.DebugDescribeChildren();

        Assert.Equal(["onstage 1", "onstage 1 - 1", string.Empty], children.Select(node => node.Name ?? string.Empty));
        Assert.Equal("no offstage children", children[2].ToDescription());
    }

    [Fact]
    public void OverlayChildSemantics_AreTraversedUnderTheOverlayPortalThatShowsThem()
    {
        var controller = new OverlayPortalController();
        controller.Show();
        var entry = new OverlayEntry(_ => new Stack(
            fit: StackFit.Expand,
            children:
            [
                new Semantics(
                    container: true,
                    explicitChildNodes: true,
                    child: new OverlayPortal(
                        controller: controller,
                        overlayChildBuilder: _ => new Semantics(
                            label: "overlay child",
                            container: true,
                            child: new SizedBox(width: 10, height: 10)),
                        child: new Semantics(
                            label: "anchor",
                            child: new SizedBox(width: 10, height: 10)))),
            ]));

        using var harness = new PortalHarness(entry);
        SemanticsNode root = harness.FlushSemantics();

        SemanticsNode anchor = FindSemanticsNode(root, "anchor");
        SemanticsNode overlayChild = FindSemanticsNode(root, "overlay child");
        SemanticsNode deferredBoxNode = Assert.Single(
            root.Children.Where(node => node.Children.Contains(overlayChild)));

        // The overlay child's render parent is the theater, so it is a paint-order sibling of the
        // entry; its `TraversalChildIdentifier` names the `OverlayPortal` state, which the portal's own
        // node names as its traversal parent, so the owner grafts it there for traversal only.
        Assert.Empty(anchor.Children);
        Assert.Same(deferredBoxNode, Assert.Single(anchor.ChildrenInTraversalOrder));
        Assert.DoesNotContain(deferredBoxNode, root.ChildrenInTraversalOrder);
    }

    private static IReadOnlyList<RenderObject> VisitedChildren(RenderObject renderObject)
    {
        var children = new List<RenderObject>();
        renderObject.VisitChildren(children.Add);
        return children;
    }

    private static IReadOnlyList<RenderObject> SemanticsChildren(RenderObject renderObject)
    {
        var children = new List<RenderObject>();
        renderObject.VisitChildrenForSemantics(children.Add);
        return children;
    }

    private static SemanticsNode FindSemanticsNode(SemanticsNode root, string label)
    {
        if (root.Label == label)
        {
            return root;
        }

        foreach (SemanticsNode child in root.Children)
        {
            SemanticsNode? found = TryFind(child);
            if (found is not null)
            {
                return found;
            }
        }

        throw new InvalidOperationException($"No semantics node labelled '{label}'.");

        SemanticsNode? TryFind(SemanticsNode node)
        {
            return node.Label == label
                ? node
                : node.Children.Select(TryFind).FirstOrDefault(found => found is not null);
        }
    }

    private sealed class PaintProbe : SingleChildRenderObjectWidget
    {
        public PaintProbe(Action onPaint, Widget? child = null) : base(child)
        {
            OnPaint = onPaint;
        }

        public Action OnPaint { get; }

        public override RenderObject CreateRenderObject(BuildContext context)
        {
            return new RenderPaintProbe { OnPaint = OnPaint };
        }

        public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
        {
            ((RenderPaintProbe)renderObject).OnPaint = OnPaint;
        }
    }

    private sealed class RenderPaintProbe : RenderProxyBox
    {
        public Action? OnPaint { get; set; }

        public override void Paint(PaintingContext context, Point offset)
        {
            OnPaint?.Invoke();
            base.Paint(context, offset);
        }
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

        public override RenderObject CreateRenderObject(BuildContext context)
        {
            return new RenderLayoutProbe { OnPerformLayout = OnPerformLayout };
        }

        public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
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

        public PortalHarness(params OverlayEntry[] entries)
        {
            // Stack's default alignment is AlignmentDirectional.topStart, which needs a direction.
            _root = new TestRootElement(
                new Directionality(TextDirection.Ltr, new Overlay(initialEntries: entries)));
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

        public void FlushPaint()
        {
            Pump();
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public SemanticsNode FlushSemantics()
        {
            Pump();
            _pipeline.FlushSemantics();
            return Assert.IsType<SemanticsNode>(_pipeline.SemanticsOwner!.RootNode);
        }

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

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
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
