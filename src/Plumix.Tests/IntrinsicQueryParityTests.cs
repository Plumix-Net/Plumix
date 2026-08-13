using Avalonia;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/test/rendering/cached_intrinsics_test.dart
// flutter/packages/flutter/test/rendering/flex_test.dart
// flutter/packages/flutter/lib/src/rendering/rotated_box.dart
// flutter/packages/flutter/lib/src/rendering/flow.dart
// flutter/packages/flutter/lib/src/rendering/image.dart
// flutter/packages/flutter/lib/src/rendering/custom_layout.dart
// flutter/packages/flutter/lib/src/rendering/flex.dart
// flutter/packages/flutter/lib/src/rendering/sliver_fill.dart
// flutter/packages/flutter/lib/src/widgets/sliver_resizing_header.dart
// material_ui/lib/src/floating_action_button.dart
public sealed class IntrinsicQueryParityTests
{
    [Fact]
    public void RenderBox_CachesQueryKindsAndSharesDryAndActualBaselineResults()
    {
        var box = new QueryRenderBox(new Size(40, 24), baseline: 9.0);
        BoxConstraints constraints = BoxConstraints.Tight(new Size(40, 24));
        box.Layout(constraints);

        Assert.Equal(11.0, box.GetMinIntrinsicWidth(24.0));
        Assert.Equal(11.0, box.GetMinIntrinsicWidth(24.0));
        Assert.Equal(12.0, box.GetMaxIntrinsicWidth(24.0));
        Assert.Equal(12.0, box.GetMaxIntrinsicWidth(24.0));
        Assert.Equal(21.0, box.GetMinIntrinsicHeight(40.0));
        Assert.Equal(21.0, box.GetMinIntrinsicHeight(40.0));
        Assert.Equal(22.0, box.GetMaxIntrinsicHeight(40.0));
        Assert.Equal(22.0, box.GetMaxIntrinsicHeight(40.0));
        Assert.Equal(1, box.MinWidthCount);
        Assert.Equal(1, box.MaxWidthCount);
        Assert.Equal(1, box.MinHeightCount);
        Assert.Equal(1, box.MaxHeightCount);

        Assert.Equal(new Size(40, 24), box.GetDryLayout(constraints));
        Assert.Equal(new Size(40, 24), box.GetDryLayout(constraints));
        Assert.Equal(1, box.DryLayoutCount);

        Assert.Equal(9.0, box.GetDryBaseline(constraints, TextBaseline.Alphabetic));
        Assert.Equal(9.0, box.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true));
        Assert.Equal(0, box.ActualBaselineCount);
    }

    [Fact]
    public void RenderBox_CachesNullBaselinesAndInvalidatesRelayoutBoundaryParent()
    {
        var child = new QueryRenderBox(new Size(30, 20), baseline: null);
        var parent = new CountingProxyBox(child);
        BoxConstraints constraints = BoxConstraints.Tight(new Size(30, 20));
        parent.Layout(constraints);

        Assert.Null(child.GetDryBaseline(constraints, TextBaseline.Ideographic));
        Assert.Null(child.GetDryBaseline(constraints, TextBaseline.Ideographic));
#if DEBUG
        Assert.Equal(3, child.DryBaselineCount);
#else
        Assert.Equal(1, child.DryBaselineCount);
#endif

        child.GetDryLayout(constraints);
        child.MarkNeedsLayout();
        parent.Layout(constraints);
        child.GetDryLayout(constraints);

        Assert.Equal(2, parent.LayoutCount);
        Assert.Equal(2, child.DryLayoutCount);
    }

    [Fact]
    public void RenderBox_DryLayoutRejectsReadingWetSize()
    {
        var box = new SizeReadingDryBox();
        box.Layout(BoxConstraints.Tight(new Size(20, 10)));

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => box.GetDryLayout(BoxConstraints.Tight(new Size(30, 15))));

        Assert.Contains("Size was accessed", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DirectDryOverrides_MatchRotatedFlowImageAndCustomLayoutAlgorithms()
    {
        var child = new QueryRenderBox(new Size(30, 20), baseline: 7.0);
        var rotated = new RenderRotatedBox(quarterTurns: 1, child);
        Assert.Equal(21.0, rotated.GetMinIntrinsicWidth(30.0));
        Assert.Equal(12.0, rotated.GetMaxIntrinsicHeight(20.0));
        Assert.Equal(new Size(20, 30), rotated.GetDryLayout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100)));

        var flowDelegate = new FixedFlowDelegate(new Size(120, 80));
        var flow = new RenderFlow(flowDelegate);
        Assert.Equal(120.0, flow.GetMaxIntrinsicWidth(30.0));
        Assert.Equal(80.0, flow.GetMaxIntrinsicHeight(50.0));
        Assert.Equal(new Size(100, 70), flow.GetDryLayout(new BoxConstraints(MaxWidth: 100, MaxHeight: 70)));

        var layoutDelegate = new FixedLayoutDelegate(new Size(120, 80));
        var custom = new RenderCustomMultiChildLayoutBox(layoutDelegate);
        Assert.Equal(120.0, custom.GetMaxIntrinsicWidth(30.0));
        Assert.Equal(80.0, custom.GetMaxIntrinsicHeight(50.0));
        Assert.Equal(new Size(100, 70), custom.GetDryLayout(new BoxConstraints(MaxWidth: 100, MaxHeight: 70)));
        Assert.Equal(0, layoutDelegate.PerformLayoutCount);

        var image = new RenderImage(width: 50.0, height: 25.0);
        Assert.Equal(50.0, image.GetMinIntrinsicWidth(100.0));
        Assert.Equal(25.0, image.GetMinIntrinsicHeight(100.0));
        Assert.Equal(new Size(50, 25), image.GetDryLayout(new BoxConstraints(MaxWidth: 80, MaxHeight: 80)));
    }

    [Fact]
    public void RenderFlex_ComputesMainCrossAndBaselineDryQueries()
    {
        var first = new QueryRenderBox(new Size(100, 100), baseline: 10.0);
        var second = new QueryRenderBox(new Size(100, 100), baseline: 20.0);
        var row = new RenderFlex(
            children: [first, second],
            direction: Axis.Horizontal,
            mainAxisSize: MainAxisSize.Min,
            spacing: 12.0);

        Assert.Equal(212.0, row.GetMinIntrinsicWidth(100.0));
        Assert.Equal(212.0, row.GetMaxIntrinsicWidth(100.0));
        Assert.Equal(100.0, row.GetMinIntrinsicHeight(212.0));
        Assert.Equal(new Size(212, 100), row.GetDryLayout(
            new BoxConstraints(MaxWidth: 300, MaxHeight: 200)));

        var baselineRow = new RenderFlex(
            children:
            [
                new QueryRenderBox(new Size(100, 20), baseline: 10.0),
                new QueryRenderBox(new Size(100, 30), baseline: 20.0)
            ],
            direction: Axis.Horizontal,
            mainAxisSize: MainAxisSize.Min,
            crossAxisAlignment: CrossAxisAlignment.Baseline,
            textBaseline: TextBaseline.Alphabetic);
        BoxConstraints baselineConstraints = new(MaxWidth: 200, MaxHeight: 100);

        Assert.Equal(new Size(200, 30), baselineRow.GetDryLayout(baselineConstraints));
        Assert.Equal(20.0, baselineRow.GetDryBaseline(baselineConstraints, TextBaseline.Alphabetic));
    }

    [Fact]
    public void SliverAndFabFallbacksUseSideEffectFreeQueriesBeforeWetLayout()
    {
        var fillChild = new QueryRenderBox(new Size(100, 240), baseline: null);
        var fill = new RenderSliverFillRemaining(fillChild);
        fill.LayoutWithSliverConstraints(CreateSliverConstraints());

        Assert.Equal(1, fillChild.MaxHeightCount);
        Assert.Equal(1, fillChild.LayoutCount);
        Assert.Equal(240.0, fill.Geometry.ScrollExtent);

        var headerChild = new QueryRenderBox(new Size(100, 300), baseline: null);
        var header = new RenderSliverResizingHeader { Child = headerChild };
        header.LayoutWithSliverConstraints(CreateSliverConstraints());

        Assert.Equal(1, headerChild.DryLayoutCount);
        Assert.Equal(1, headerChild.LayoutCount);
        Assert.Equal(300.0, header.Geometry.ScrollExtent);

        var overflowChild = new QueryRenderBox(new Size(200, 40), baseline: null);
        var overflow = new RenderFloatingActionButtonChildOverflowBox { Child = overflowChild };
        BoxConstraints overflowConstraints = new(MaxWidth: 150, MaxHeight: 60);
        Assert.Same(overflowChild, overflow.Child);
        Assert.Equal(
            new Size(200, 40),
            overflowChild.GetDryLayout(new BoxConstraints(
                MaxWidth: double.PositiveInfinity,
                MaxHeight: double.PositiveInfinity)));
        Assert.Equal(0.0, overflow.GetMinIntrinsicWidth(40.0));
        Assert.Equal(new Size(150, 40), overflow.GetDryLayout(overflowConstraints));
    }

    private static SliverConstraints CreateSliverConstraints()
    {
        return new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: 0.0,
            RemainingPaintExtent: 150.0,
            CrossAxisExtent: 100.0,
            ViewportMainAxisExtent: 200.0,
            RemainingCacheExtent: 150.0,
            AxisDirection: AxisDirection.Down,
            PrecedingScrollExtent: 50.0);
    }

    private sealed class QueryRenderBox(Size naturalSize, double? baseline) : RenderBox
    {
        public int LayoutCount { get; private set; }
        public int DryLayoutCount { get; private set; }
        public int DryBaselineCount { get; private set; }
        public int ActualBaselineCount { get; private set; }
        public int MinWidthCount { get; private set; }
        public int MaxWidthCount { get; private set; }
        public int MinHeightCount { get; private set; }
        public int MaxHeightCount { get; private set; }

        protected override double ComputeMinIntrinsicWidth(double height)
        {
            MinWidthCount++;
            return naturalSize.Width == 100.0 ? naturalSize.Width : 11.0;
        }

        protected override double ComputeMaxIntrinsicWidth(double height)
        {
            MaxWidthCount++;
            return naturalSize.Width == 100.0 || naturalSize.Width == 200.0 ? naturalSize.Width : 12.0;
        }

        protected override double ComputeMinIntrinsicHeight(double width)
        {
            MinHeightCount++;
            return naturalSize.Height == 100.0 ? naturalSize.Height : 21.0;
        }

        protected override double ComputeMaxIntrinsicHeight(double width)
        {
            MaxHeightCount++;
            return naturalSize.Height >= 100.0 ? naturalSize.Height : 22.0;
        }

        protected override Size ComputeDryLayout(BoxConstraints constraints)
        {
            DryLayoutCount++;
            return constraints.Constrain(naturalSize);
        }

        protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline textBaseline)
        {
            DryBaselineCount++;
            return baseline;
        }

        protected override double? ComputeDistanceToActualBaseline(TextBaseline textBaseline)
        {
            ActualBaselineCount++;
            return baseline;
        }

        protected override void PerformLayout()
        {
            LayoutCount++;
            Size = Constraints.Constrain(naturalSize);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class CountingProxyBox : RenderProxyBox
    {
        public CountingProxyBox(RenderBox child)
        {
            Child = child;
        }

        public int LayoutCount { get; private set; }

        protected override void PerformLayout()
        {
            LayoutCount++;
            base.PerformLayout();
        }

    }

    private sealed class SizeReadingDryBox : RenderBox
    {
        protected override Size ComputeDryLayout(BoxConstraints constraints) => Size;

        protected override void PerformLayout()
        {
            Size = Constraints.Smallest;
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class FixedFlowDelegate(Size size) : FlowDelegate
    {
        public override Size GetSize(BoxConstraints constraints) => size;

        public override void PaintChildren(FlowPaintingContext context)
        {
        }

        public override bool ShouldRepaint(FlowDelegate oldDelegate) => false;
    }

    private sealed class FixedLayoutDelegate(Size size) : MultiChildLayoutDelegate
    {
        public int PerformLayoutCount { get; private set; }

        public override Size GetSize(BoxConstraints constraints) => size;

        public override void PerformLayout(Size layoutSize)
        {
            PerformLayoutCount++;
        }

        public override bool ShouldRelayout(MultiChildLayoutDelegate oldDelegate) => false;
    }
}
