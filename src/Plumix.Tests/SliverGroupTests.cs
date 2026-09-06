using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class SliverGroupTests
{
    [Fact]
    public void SliverGroupWidgets_ExposeFlutterContractsAndGuards()
    {
        var child = new SliverToBoxAdapter(new SizedBox(height: 20));
        var constrained = new SliverConstrainedCrossAxis(80, child);
        var expanded = new SliverCrossAxisExpanded(2, child);
        var crossGroup = new SliverCrossAxisGroup([constrained, expanded]);
        var mainGroup = new SliverMainAxisGroup([child]);

        Assert.Equal(80, constrained.MaxExtent);
        Assert.Same(child, constrained.Sliver);
        Assert.Equal(2, expanded.Flex);
        Assert.Same(child, expanded.Sliver);
        Assert.Equal([constrained, expanded], crossGroup.Slivers);
        Assert.Equal([child], mainGroup.Slivers);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SliverConstrainedCrossAxis(-1, child));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SliverCrossAxisExpanded(0, child));
        Assert.Throws<ArgumentNullException>(() => new SliverCrossAxisGroup(null!));
        Assert.Throws<ArgumentNullException>(() => new SliverMainAxisGroup(null!));
    }

    [Fact]
    public void SliverCrossAxisGroup_WiresDefaultZeroAndExpandedParentData()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new SliverCrossAxisGroup(
            [
                new RecordingSliverWidget(),
                new SliverConstrainedCrossAxis(80, new RecordingSliverWidget()),
                new SliverCrossAxisExpanded(2, new RecordingSliverWidget()),
            ]));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        var group = Assert.IsType<RenderSliverCrossAxisGroup>(root.ChildElement!.RenderObject);
        RenderSliver first = group.FirstChild!;
        RenderSliver second = group.ChildAfter(first)!;
        RenderSliver third = group.ChildAfter(second)!;

        Assert.Equal(1, ((SliverPhysicalParentData)first.parentData!).CrossAxisFlex);
        Assert.Equal(0, ((SliverPhysicalParentData)second.parentData!).CrossAxisFlex);
        Assert.Equal(2, ((SliverPhysicalParentData)third.parentData!).CrossAxisFlex);
        root.Unmount();
    }

    [Fact]
    public void RenderSliverConstrainedCrossAxis_UsesSmallerCrossAxisExtentAndForwardsGeometry()
    {
        var child = new RecordingSliver(scrollExtent: 140, requestedCrossAxisExtent: null);
        var constrained = new RenderSliverConstrainedCrossAxis(maxExtent: 80, sliver: child);
        SliverConstraints constraints = CreateConstraints(crossAxisExtent: 120);

        constrained.LayoutWithSliverConstraints(constraints);

        Assert.Equal(80, child.LastConstraints.CrossAxisExtent);
        Assert.Equal(80, constrained.Geometry.CrossAxisExtent);
        Assert.Equal(child.Geometry.ScrollExtent, constrained.Geometry.ScrollExtent);
        Assert.Equal(child.Geometry.PaintExtent, constrained.Geometry.PaintExtent);
        Assert.Equal(default, ((SliverPhysicalParentData)child.parentData!).offset);
    }

    [Fact]
    public void RenderSliverCrossAxisGroup_AllocatesInflexibleThenProportionalFlexExtents()
    {
        var first = new RecordingSliver(scrollExtent: 120);
        var constrainedChild = new RecordingSliver(scrollExtent: 160, requestedCrossAxisExtent: 80);
        var constrained = new RenderSliverConstrainedCrossAxis(80, constrainedChild);
        var expanded = new RecordingSliver(scrollExtent: 100);
        var group = new RenderSliverCrossAxisGroup();
        group.Insert(first);
        group.Insert(constrained, after: first);
        group.Insert(expanded, after: constrained);
        ((SliverPhysicalParentData)constrained.parentData!).CrossAxisFlex = 0;
        ((SliverPhysicalParentData)expanded.parentData!).CrossAxisFlex = 2;

        group.LayoutWithSliverConstraints(CreateConstraints(crossAxisExtent: 380));

        Assert.Equal(100, first.LastConstraints.CrossAxisExtent, precision: 3);
        Assert.Equal(80, constrainedChild.LastConstraints.CrossAxisExtent, precision: 3);
        Assert.Equal(200, expanded.LastConstraints.CrossAxisExtent, precision: 3);
        Assert.Equal(new Point(0, 0), ((SliverPhysicalParentData)first.parentData!).offset);
        Assert.Equal(new Point(100, 0), ((SliverPhysicalParentData)constrained.parentData!).offset);
        Assert.Equal(new Point(180, 0), ((SliverPhysicalParentData)expanded.parentData!).offset);
        Assert.Equal(160, group.Geometry.ScrollExtent);
    }

    [Fact]
    public void RenderSliverCrossAxisGroup_CorrectsChildrenPaintingPastGroupScrollExtent()
    {
        var shortChild = new RecordingSliver(scrollExtent: 60, paintExtentOverride: 50);
        var longChild = new RecordingSliver(scrollExtent: 100, paintExtentOverride: 50);
        var group = new RenderSliverCrossAxisGroup();
        group.Insert(shortChild);
        group.Insert(longChild, after: shortChild);

        group.LayoutWithSliverConstraints(CreateConstraints(
            scrollOffset: 80,
            remainingPaintExtent: 50,
            crossAxisExtent: 200));

        Assert.Equal(new Point(0, -30), ((SliverPhysicalParentData)shortChild.parentData!).offset);
        Assert.Equal(new Point(100, -30), ((SliverPhysicalParentData)longChild.parentData!).offset);
    }

    [Fact]
    public void RenderSliverCrossAxisGroup_RejectsConstrainedChildrenThatExhaustExtent()
    {
        using var renderErrors = RenderErrorRethrowScope.Enter();
        var first = new RecordingSliver(scrollExtent: 20, requestedCrossAxisExtent: 100);
        var second = new RecordingSliver(scrollExtent: 20, requestedCrossAxisExtent: 20);
        var group = new RenderSliverCrossAxisGroup();
        group.Insert(first);
        group.Insert(second, after: first);
        ((SliverPhysicalParentData)first.parentData!).CrossAxisFlex = 0;
        ((SliverPhysicalParentData)second.parentData!).CrossAxisFlex = 0;

        Assert.Throws<InvalidOperationException>(() =>
            group.LayoutWithSliverConstraints(CreateConstraints(crossAxisExtent: 100)));
    }

    [Fact]
    public void RenderSliverMainAxisGroup_LaysOutSequentiallyAndAggregatesGeometry()
    {
        var first = new RecordingSliver(scrollExtent: 80);
        var second = new RecordingSliver(scrollExtent: 120);
        var group = new RenderSliverMainAxisGroup();
        group.Insert(first);
        group.Insert(second, after: first);

        group.LayoutWithSliverConstraints(CreateConstraints(
            scrollOffset: 50,
            remainingPaintExtent: 150,
            crossAxisExtent: 100,
            remainingCacheExtent: 190,
            precedingScrollExtent: 30));

        Assert.Equal(50, first.LastConstraints.ScrollOffset);
        Assert.Equal(0, second.LastConstraints.ScrollOffset);
        Assert.Equal(30, first.LastConstraints.PrecedingScrollExtent);
        Assert.Equal(110, second.LastConstraints.PrecedingScrollExtent);
        Assert.Equal(new Point(0, 0), ((SliverPhysicalParentData)first.parentData!).offset);
        Assert.Equal(new Point(0, 30), ((SliverPhysicalParentData)second.parentData!).offset);
        Assert.Equal(200, group.Geometry.ScrollExtent);
        Assert.Equal(150, group.Geometry.PaintExtent);
        Assert.Equal(150, group.Geometry.LayoutExtent);
        Assert.Equal(200, group.Geometry.MaxPaintExtent);
    }

    [Fact]
    public void RenderSliverMainAxisGroup_ConfinesPinnedChildToGroupScrollExtent()
    {
        var body = new RecordingSliver(scrollExtent: 100);
        var pinned = new RecordingSliver(
            scrollExtent: 40,
            paintExtentOverride: 40,
            maxScrollObstructionExtent: 40);
        var group = new RenderSliverMainAxisGroup();
        group.Insert(pinned);
        group.Insert(body, after: pinned);

        group.LayoutWithSliverConstraints(CreateConstraints(
            scrollOffset: 125,
            remainingPaintExtent: 100,
            crossAxisExtent: 100));

        Assert.Equal(140, group.Geometry.ScrollExtent);
        Assert.Equal(15, group.Geometry.PaintExtent);
        Assert.Equal(new Point(0, -40), ((SliverPhysicalParentData)pinned.parentData!).offset);
        Assert.Equal(0, group.ChildScrollOffset(pinned));
        Assert.Equal(0, group.ChildScrollOffset(body));
    }

    [Fact]
    public void RenderSliverMainAxisGroup_ForwardsScrollOffsetCorrectionImmediately()
    {
        var correcting = new RecordingSliver(scrollExtent: 0, scrollOffsetCorrection: -12);
        var unreachable = new RecordingSliver(scrollExtent: 80);
        var group = new RenderSliverMainAxisGroup();
        group.Insert(correcting);
        group.Insert(unreachable, after: correcting);

        group.LayoutWithSliverConstraints(CreateConstraints());

        Assert.Equal(-12, group.Geometry.ScrollOffsetCorrection);
        Assert.Equal(default, unreachable.LastConstraints);
    }

    [Fact]
    public void RenderSliverGroups_PaintChildrenInFlutterOrder()
    {
        var crossPaintOrder = new List<string>();
        var crossFirst = new RecordingSliver(40, paintOrder: crossPaintOrder, name: "first");
        var crossSecond = new RecordingSliver(40, paintOrder: crossPaintOrder, name: "second");
        var crossGroup = new RenderSliverCrossAxisGroup();
        crossGroup.Insert(crossFirst);
        crossGroup.Insert(crossSecond, after: crossFirst);
        PaintSliver(crossGroup, new Size(200, 100));

        var mainPaintOrder = new List<string>();
        var mainFirst = new RecordingSliver(40, paintOrder: mainPaintOrder, name: "first");
        var mainSecond = new RecordingSliver(40, paintOrder: mainPaintOrder, name: "second");
        var mainGroup = new RenderSliverMainAxisGroup();
        mainGroup.Insert(mainFirst);
        mainGroup.Insert(mainSecond, after: mainFirst);
        PaintSliver(mainGroup, new Size(100, 100));

        Assert.Equal(["first", "second"], crossPaintOrder);
        Assert.Equal(["second", "first"], mainPaintOrder);
    }

    private static void PaintSliver(RenderSliver sliver, Size size)
    {
        var viewport = new RenderViewport(offset: ViewportOffset.Zero());
        viewport.Insert(sliver);
        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(size);
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
    }

    private static SliverConstraints CreateConstraints(
        double scrollOffset = 0.0,
        double remainingPaintExtent = 200.0,
        double crossAxisExtent = 100.0,
        double remainingCacheExtent = 200.0,
        double precedingScrollExtent = 0.0)
    {
        return new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: scrollOffset,
            RemainingPaintExtent: remainingPaintExtent,
            CrossAxisExtent: crossAxisExtent,
            ViewportMainAxisExtent: remainingPaintExtent,
            RemainingCacheExtent: remainingCacheExtent,
            PrecedingScrollExtent: precedingScrollExtent);
    }

    private sealed class RecordingSliver : RenderSliver
    {
        private readonly double _scrollExtent;
        private readonly double? _requestedCrossAxisExtent;
        private readonly double? _paintExtentOverride;
        private readonly double _maxScrollObstructionExtent;
        private readonly double _scrollOffsetCorrection;
        private readonly List<string>? _paintOrder;
        private readonly string? _name;

        public RecordingSliver(
            double scrollExtent,
            double? requestedCrossAxisExtent = null,
            double? paintExtentOverride = null,
            double maxScrollObstructionExtent = 0.0,
            double scrollOffsetCorrection = 0.0,
            List<string>? paintOrder = null,
            string? name = null)
        {
            _scrollExtent = scrollExtent;
            _requestedCrossAxisExtent = requestedCrossAxisExtent;
            _paintExtentOverride = paintExtentOverride;
            _maxScrollObstructionExtent = maxScrollObstructionExtent;
            _scrollOffsetCorrection = scrollOffsetCorrection;
            _paintOrder = paintOrder;
            _name = name;
        }

        public SliverConstraints LastConstraints { get; private set; }

        protected override void PerformSliverLayout(SliverConstraints constraints)
        {
            LastConstraints = constraints;
            if (Math.Abs(_scrollOffsetCorrection) > 0.0001)
            {
                Geometry = new SliverGeometry(ScrollOffsetCorrection: _scrollOffsetCorrection);
                return;
            }

            double paintExtent = _paintExtentOverride
                ?? CalculatePaintOffset(constraints, from: 0.0, to: _scrollExtent);
            double cacheExtent = CalculateCacheOffset(constraints, from: 0.0, to: _scrollExtent);
            Geometry = new SliverGeometry(
                ScrollExtent: _scrollExtent,
                PaintExtent: paintExtent,
                LayoutExtent: paintExtent,
                MaxPaintExtent: _scrollExtent,
                CacheExtent: cacheExtent,
                MaxScrollObstructionExtent: _maxScrollObstructionExtent,
                CrossAxisExtent: _requestedCrossAxisExtent);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            if (_paintOrder != null && _name != null)
            {
                _paintOrder.Add(_name);
            }
        }
    }

    private sealed class RecordingSliverWidget : LeafRenderObjectWidget
    {
        public override RenderObject CreateRenderObject(BuildContext context)
        {
            return new RecordingSliver(scrollExtent: 20);
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

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild(force: true);
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
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }
}
