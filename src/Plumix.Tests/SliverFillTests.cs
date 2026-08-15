using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/sliver_fill.dart
// flutter/packages/flutter/lib/src/rendering/sliver_fill.dart

public sealed class SliverFillTests
{
    [Fact]
    public void SliverFillViewport_ExposesSourceDefaultsGuardsAndPaddingComposition()
    {
        var childDelegate = new SliverChildListDelegate([new SizedBox(), new SizedBox()]);
        var widget = new SliverFillViewport(childDelegate);

        Assert.Same(childDelegate, widget.Delegate);
        Assert.Equal(1.0, widget.ViewportFraction);
        Assert.True(widget.PadEnds);
        Assert.True(widget.AllowImplicitScrolling);
        Assert.Throws<ArgumentNullException>(() => new SliverFillViewport(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SliverFillViewport(childDelegate, viewportFraction: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SliverFillViewport(childDelegate, viewportFraction: double.NaN));

        var padded = Assert.IsType<SliverFractionalPadding>(new SliverFillViewport(
            childDelegate,
            viewportFraction: 0.5).Build(default));
        Assert.Equal(0.25, padded.ViewportFraction);
        var fill = Assert.IsType<SliverFillViewportRenderObjectWidget>(padded.Child);
        Assert.Equal(0.5, fill.ViewportFraction);
        Assert.True(fill.AllowImplicitScrolling);

        var unpadded = Assert.IsType<SliverFractionalPadding>(new SliverFillViewport(
            childDelegate,
            viewportFraction: 0.5,
            padEnds: false,
            allowImplicitScrolling: false).Build(default));
        Assert.Equal(0.0, unpadded.ViewportFraction);
        Assert.False(Assert.IsType<SliverFillViewportRenderObjectWidget>(unpadded.Child).AllowImplicitScrolling);

        var initialRenderWidget = new SliverFillViewportRenderObjectWidget(
            childDelegate,
            viewportFraction: 0.5,
            allowImplicitScrolling: false);
        var renderObject = Assert.IsType<RenderSliverFillViewport>(initialRenderWidget.CreateRenderObject(default));
        var updatedRenderWidget = new SliverFillViewportRenderObjectWidget(
            childDelegate,
            viewportFraction: 0.75,
            allowImplicitScrolling: true);
        updatedRenderWidget.UpdateRenderObject(default, renderObject);
        Assert.Equal(0.75, renderObject.ViewportFraction);
        Assert.True(renderObject.AllowImplicitScrolling);
    }

    [Fact]
    public void RenderSliverFillViewport_SizesChildrenFromViewportFractionAndLaysOutLazily()
    {
        var manager = new TestChildManager(childCount: 6);
        var sliver = new RenderSliverFillViewport(
            viewportFraction: 0.5,
            childManager: manager);
        manager.AttachOwner(sliver);

        sliver.LayoutWithSliverConstraints(CreateConstraints(
            remainingPaintExtent: 200,
            viewportMainAxisExtent: 200,
            remainingCacheExtent: 200));

        Assert.Equal(600, sliver.Geometry.ScrollExtent);
        Assert.Equal(200, sliver.Geometry.PaintExtent);
        Assert.Equal(100, sliver.ItemExtent);
        Assert.Equal([0, 1], ActiveIndices(sliver));
        Assert.All(ActiveChildren(sliver), child => Assert.Equal(new Size(100, 100), child.Size));

        sliver.LayoutWithSliverConstraints(CreateConstraints(
            scrollOffset: 200,
            remainingPaintExtent: 200,
            viewportMainAxisExtent: 200,
            remainingCacheExtent: 200));

        Assert.Equal([2, 3], ActiveIndices(sliver));
        Assert.Equal(new Point(0, 0), ParentData(sliver.FirstChild!).offset);
    }

    [Fact]
    public void RenderSliverFillViewport_RestrictsSemanticsToVisibleChildrenWhenImplicitScrollingIsDisabled()
    {
        var manager = new TestChildManager(childCount: 5);
        var sliver = new RenderSliverFillViewport(
            viewportFraction: 0.5,
            allowImplicitScrolling: false,
            childManager: manager);
        manager.AttachOwner(sliver);
        sliver.LayoutWithSliverConstraints(CreateConstraints(
            remainingPaintExtent: 200,
            viewportMainAxisExtent: 200,
            cacheOrigin: 0,
            remainingCacheExtent: 400));

        var visited = new List<RenderObject>();
        sliver.VisitChildrenForSemantics(child => visited.Add(child));
        Assert.Equal(2, visited.Count);

        sliver.AllowImplicitScrolling = true;
        visited.Clear();
        sliver.VisitChildrenForSemantics(child => visited.Add(child));
        Assert.Equal(4, visited.Count);
    }

    [Fact]
    public void RenderSliverFractionalPadding_ResolvesAgainstCurrentViewportExtentAndAxis()
    {
        var verticalChild = new RecordingSliver(scrollExtent: 200);
        var vertical = new RenderSliverFractionalPadding(0.25, verticalChild);
        vertical.LayoutWithSliverConstraints(CreateConstraints(
            remainingPaintExtent: 200,
            viewportMainAxisExtent: 200));

        Assert.Equal(300, vertical.Geometry.ScrollExtent);
        Assert.Equal(new Point(0, 50), ((SliverPhysicalParentData)verticalChild.parentData!).offset);

        var horizontalChild = new RecordingSliver(scrollExtent: 120);
        var horizontal = new RenderSliverFractionalPadding(0.1, horizontalChild);
        horizontal.LayoutWithSliverConstraints(CreateConstraints(
            axis: Axis.Horizontal,
            crossAxisExtent: 80,
            remainingPaintExtent: 300,
            viewportMainAxisExtent: 300));

        Assert.Equal(180, horizontal.Geometry.ScrollExtent);
        Assert.Equal(new Point(30, 0), ((SliverPhysicalParentData)horizontalChild.parentData!).offset);
    }

    [Fact]
    public void SliverFillRemaining_SelectsTheThreeSourceRenderPaths()
    {
        var child = new SizedBox(height: 20);
        var scrollable = new SliverFillRemaining(child).Build(default);
        var nonScrollable = new SliverFillRemaining(child, hasScrollBody: false).Build(default);
        var overscroll = new SliverFillRemaining(
            child,
            hasScrollBody: false,
            fillOverscroll: true).Build(default);

        Assert.IsType<SliverFillRemainingWithScrollable>(scrollable);
        Assert.IsType<SliverFillRemainingWithoutScrollable>(nonScrollable);
        Assert.IsType<SliverFillRemainingAndOverscroll>(overscroll);
        Assert.True(new SliverFillRemaining().HasScrollBody);
        Assert.False(new SliverFillRemaining().FillOverscroll);
    }

    [Fact]
    public void RenderSliverFillRemainingWithoutScrollable_FillsRemainderButDefersToLargerChild()
    {
        var smallChild = new NaturalSizeBox(new Size(100, 40));
        var small = new RenderSliverFillRemaining(smallChild);
        small.LayoutWithSliverConstraints(CreateConstraints(
            remainingPaintExtent: 150,
            viewportMainAxisExtent: 200,
            precedingScrollExtent: 50,
            remainingCacheExtent: 150));

        Assert.Equal(new Size(100, 150), smallChild.Size);
        Assert.Equal(150, small.Geometry.ScrollExtent);
        Assert.Equal(150, small.Geometry.PaintExtent);

        var largeChild = new NaturalSizeBox(new Size(100, 240));
        var large = new RenderSliverFillRemaining(largeChild);
        large.LayoutWithSliverConstraints(CreateConstraints(
            remainingPaintExtent: 150,
            viewportMainAxisExtent: 200,
            precedingScrollExtent: 50,
            remainingCacheExtent: 150));

        Assert.Equal(new Size(100, 240), largeChild.Size);
        Assert.Equal(240, large.Geometry.ScrollExtent);
        Assert.Equal(150, large.Geometry.PaintExtent);
        Assert.True(large.Geometry.HasVisualOverflow);
    }

    [Fact]
    public void RenderSliverFillRemainingWithoutScrollable_DefersToChildAfterViewportWasPreceded()
    {
        var child = new NaturalSizeBox(new Size(100, 64));
        var sliver = new RenderSliverFillRemaining(child);
        sliver.LayoutWithSliverConstraints(CreateConstraints(
            remainingPaintExtent: 100,
            viewportMainAxisExtent: 200,
            precedingScrollExtent: 260,
            remainingCacheExtent: 100));

        Assert.Equal(64, child.Size.Height);
        Assert.Equal(64, sliver.Geometry.ScrollExtent);
    }

    [Fact]
    public void RenderSliverFillRemainingWithScrollable_UsesRemainingPaintAndViewportScrollExtent()
    {
        var child = new ExpandingBox();
        var sliver = new RenderSliverFillRemainingWithScrollable(child);
        sliver.LayoutWithSliverConstraints(CreateConstraints(
            remainingPaintExtent: 120,
            viewportMainAxisExtent: 200,
            overlap: -20,
            remainingCacheExtent: 200));

        Assert.Equal(new Size(100, 140), child.Size);
        Assert.Equal(200, sliver.Geometry.ScrollExtent);
        Assert.Equal(120, sliver.Geometry.PaintExtent);
        Assert.Equal(120, sliver.Geometry.LayoutExtent);
        Assert.True(sliver.Geometry.HasVisualOverflow);
    }

    [Fact]
    public void RenderSliverFillRemainingWithScrollable_UsesCacheExtentForZeroPaintExtent()
    {
        var child = new ExpandingBox();
        var sliver = new RenderSliverFillRemainingWithScrollable(child);
        sliver.LayoutWithSliverConstraints(CreateConstraints(
            scrollOffset: 120,
            remainingPaintExtent: 0,
            viewportMainAxisExtent: 200,
            cacheOrigin: -120,
            remainingCacheExtent: 120));

        Assert.Equal(120, child.Size.Height);
        Assert.Equal(0, sliver.Geometry.PaintExtent);
        Assert.Equal(120, sliver.Geometry.CacheExtent);
    }

    [Fact]
    public void RenderSliverFillRemainingAndOverscroll_StretchesChildToOverscrollPaintExtent()
    {
        var child = new ExpandingBox();
        var sliver = new RenderSliverFillRemainingAndOverscroll(child);
        sliver.LayoutWithSliverConstraints(CreateConstraints(
            remainingPaintExtent: 180,
            viewportMainAxisExtent: 200,
            overlap: -40,
            precedingScrollExtent: 50,
            remainingCacheExtent: 180));

        Assert.Equal(new Size(100, 220), child.Size);
        Assert.Equal(150, sliver.Geometry.ScrollExtent);
        Assert.Equal(180, sliver.Geometry.PaintExtent);
        Assert.Equal(180, sliver.Geometry.LayoutExtent);
        Assert.Equal(220, sliver.Geometry.MaxPaintExtent);
    }

    [Fact]
    public void RenderSliverFillRemaining_SupportsHorizontalAxisAndReverseParentData()
    {
        var child = new NaturalSizeBox(new Size(40, 80));
        var sliver = new RenderSliverFillRemaining(child);
        sliver.LayoutWithSliverConstraints(CreateConstraints(
            axis: Axis.Horizontal,
            axisDirection: AxisDirection.Left,
            crossAxisExtent: 80,
            scrollOffset: 30,
            remainingPaintExtent: 170,
            viewportMainAxisExtent: 200,
            remainingCacheExtent: 170));

        Assert.Equal(new Size(200, 80), child.Size);
        // A reversed axis measures the scroll offset from the trailing end of the child, so a filled
        // sliver whose scroll extent is the viewport extent keeps its box at the sliver's origin.
        Assert.Equal(new Point(0, 0), ((BoxParentData)child.parentData!).offset);
    }

    private static SliverConstraints CreateConstraints(
        Axis axis = Axis.Vertical,
        AxisDirection axisDirection = AxisDirection.Down,
        double scrollOffset = 0.0,
        double remainingPaintExtent = 200.0,
        double crossAxisExtent = 100.0,
        double viewportMainAxisExtent = 200.0,
        double cacheOrigin = 0.0,
        double remainingCacheExtent = 200.0,
        double overlap = 0.0,
        double precedingScrollExtent = 0.0)
    {
        return new SliverConstraints(
            Axis: axis,
            ScrollOffset: scrollOffset,
            RemainingPaintExtent: remainingPaintExtent,
            CrossAxisExtent: crossAxisExtent,
            ViewportMainAxisExtent: viewportMainAxisExtent,
            CacheOrigin: cacheOrigin,
            RemainingCacheExtent: remainingCacheExtent,
            AxisDirection: axisDirection,
            Overlap: overlap,
            PrecedingScrollExtent: precedingScrollExtent);
    }

    private static IReadOnlyList<int> ActiveIndices(RenderSliverMultiBoxAdaptor sliver)
    {
        return ActiveChildren(sliver)
            .Select(child => ((SliverMultiBoxAdaptorParentData)child.parentData!).Index)
            .ToArray();
    }

    private static IEnumerable<RenderBox> ActiveChildren(RenderSliverMultiBoxAdaptor sliver)
    {
        for (RenderBox? child = sliver.FirstChild; child != null; child = sliver.ChildAfter(child))
        {
            yield return child;
        }
    }

    private static SliverMultiBoxAdaptorParentData ParentData(RenderBox child)
    {
        return (SliverMultiBoxAdaptorParentData)child.parentData!;
    }

    private sealed class NaturalSizeBox(Size naturalSize) : RenderBox
    {
        protected override double ComputeMaxIntrinsicWidth(double height) => naturalSize.Width;

        protected override double ComputeMaxIntrinsicHeight(double width) => naturalSize.Height;

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Constrain(naturalSize);

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(naturalSize);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class ExpandingBox : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(Constraints.Biggest);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class RecordingSliver(double scrollExtent) : RenderSliver
    {
        protected override void PerformSliverLayout(SliverConstraints constraints)
        {
            double paintExtent = Math.Min(scrollExtent, constraints.RemainingPaintExtent);
            Geometry = new SliverGeometry(
                ScrollExtent: scrollExtent,
                PaintExtent: paintExtent,
                LayoutExtent: paintExtent,
                MaxPaintExtent: scrollExtent,
                CacheExtent: paintExtent);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class TestChildManager(int childCount) : IRenderSliverBoxChildManager
    {
        private readonly Dictionary<int, RenderBox> _children = [];
        private readonly Dictionary<RenderBox, int> _indices = [];
        private RenderSliverMultiBoxAdaptor _owner = null!;

        public int? ChildCount => childCount;

        public void AttachOwner(RenderSliverMultiBoxAdaptor owner)
        {
            _owner = owner;
        }

        public bool CreateChild(int index, RenderBox? after)
        {
            if (index < 0 || index >= childCount)
            {
                return false;
            }

            if (_children.ContainsKey(index))
            {
                return true;
            }

            var child = new NaturalSizeBox(new Size(10, 10));
            _children[index] = child;
            _indices[child] = index;
            _owner.Insert(child, after);
            return true;
        }

        public void RemoveChild(RenderBox child)
        {
            if (!_indices.Remove(child, out int index))
            {
                return;
            }

            _children.Remove(index);
            _owner.Remove(child);
        }

        public void DidAdoptChild(RenderBox child)
        {
            if (_indices.TryGetValue(child, out int index))
            {
                ((SliverMultiBoxAdaptorParentData)child.parentData!).Index = index;
            }
        }

        public void SetDidUnderflow(bool value)
        {
        }
    }
}
