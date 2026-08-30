using Avalonia;
using Plumix;
using Plumix.Foundation;
using Plumix.Rendering;
using Xunit;

namespace Plumix.Tests;

/// Flutter parity coverage for `SliverGeometry` in `rendering/sliver.dart`.
public sealed class SliverGeometryTests
{
    [Fact]
    public void Constructor_UsesFlutterDefaults()
    {
        var zero = new SliverGeometry();

        Assert.Equal(SliverGeometry.Zero, zero);
        Assert.Equal(0.0, zero.ScrollExtent);
        Assert.Equal(0.0, zero.PaintExtent);
        Assert.Equal(0.0, zero.LayoutExtent);
        Assert.Equal(0.0, zero.MaxPaintExtent);
        Assert.Null(zero.CrossAxisExtent);
        Assert.Equal(0.0, zero.HitTestExtent);
        Assert.False(zero.Visible);
        Assert.False(zero.HasVisualOverflow);
        Assert.Null(zero.ScrollOffsetCorrection);
        Assert.Equal(0.0, zero.CacheExtent);

        var painted = new SliverGeometry(PaintExtent: 12.0, MaxPaintExtent: 12.0);
        Assert.Equal(12.0, painted.LayoutExtent);
        Assert.Equal(12.0, painted.HitTestExtent);
        Assert.Equal(12.0, painted.CacheExtent);
        Assert.True(painted.Visible);

        var cached = new SliverGeometry(
            PaintExtent: 12.0,
            LayoutExtent: 7.0,
            MaxPaintExtent: 12.0);
        Assert.Equal(7.0, cached.CacheExtent);
    }

    [Fact]
    public void Constructor_AllowsVisibilityAndHitTestingToDifferFromPaintExtent()
    {
        var hidden = new SliverGeometry(
            PaintExtent: 10.0,
            MaxPaintExtent: 10.0,
            HitTestExtent: 24.0,
            Visible: false);
        var visible = new SliverGeometry(Visible: true);

        Assert.False(hidden.Visible);
        Assert.Equal(24.0, hidden.HitTestExtent);
        Assert.True(visible.Visible);
        Assert.Equal(0.0, visible.PaintExtent);
    }

    [Fact]
    public void CopyWith_PreservesDerivedFieldsAndDropsScrollOffsetCorrection()
    {
        var source = new SliverGeometry(
            PaintExtent: 10.0,
            LayoutExtent: 8.0,
            MaxPaintExtent: 10.0,
            HitTestExtent: 12.0,
            Visible: false,
            ScrollOffsetCorrection: 3.0,
            CacheExtent: 9.0);

        SliverGeometry copy = source.CopyWith(paintExtent: 6.0);

        Assert.Equal(6.0, copy.PaintExtent);
        Assert.Equal(8.0, copy.LayoutExtent);
        Assert.Equal(12.0, copy.HitTestExtent);
        Assert.False(copy.Visible);
        Assert.Equal(9.0, copy.CacheExtent);
        Assert.Null(copy.ScrollOffsetCorrection);
    }

    [DebugOnlyFact]
    public void Diagnostics_MatchFlutterStrings()
    {
        Assert.Equal(
            "SliverGeometry(scrollExtent: 0.0, hidden, maxPaintExtent: 0.0)",
            new SliverGeometry().ToString());
        Assert.Equal(
            "SliverGeometry(scrollExtent: 100.0, paintExtent: 50.0, layoutExtent: 20.0, "
            + "maxPaintExtent: 0.0, cacheExtent: 20.0)",
            new SliverGeometry(
                ScrollExtent: 100.0,
                PaintExtent: 50.0,
                LayoutExtent: 20.0,
                Visible: true).ToString());
        Assert.Equal(
            "SliverGeometry(scrollExtent: 100.0, hidden, layoutExtent: 20.0, maxPaintExtent: 0.0, "
            + "cacheExtent: 20.0)",
            new SliverGeometry(ScrollExtent: 100.0, LayoutExtent: 20.0).ToString());
    }

    [DebugOnlyFact]
    public void DebugAssertIsValid_RejectsFlutterInvalidGeometries()
    {
        Assert.True(new SliverGeometry().DebugAssertIsValid());
        FlutterError layoutError = Assert.Throws<FlutterError>(
            () => new SliverGeometry(
                PaintExtent: 9.0,
                LayoutExtent: 10.0,
                MaxPaintExtent: 10.0).DebugAssertIsValid());
        Assert.Contains(
            "SliverGeometry is not valid: The \"layoutExtent\" exceeds the \"paintExtent\".",
            layoutError.Message,
            StringComparison.Ordinal);

        FlutterError paintError = Assert.Throws<FlutterError>(
            () => new SliverGeometry(PaintExtent: 9.0, MaxPaintExtent: 8.0).DebugAssertIsValid());
        Assert.Contains(
            "SliverGeometry is not valid: The \"maxPaintExtent\" is less than the \"paintExtent\".",
            paintError.Message,
            StringComparison.Ordinal);
        Assert.Throws<AssertionError>(() => new SliverGeometry(ScrollOffsetCorrection: 0.0));
    }

    [Fact]
    public void HitTest_UsesIndependentHitTestExtent()
    {
        var sliver = new HitTestSliver(new SliverGeometry(
            PaintExtent: 10.0,
            MaxPaintExtent: 10.0,
            HitTestExtent: 24.0));
        sliver.LayoutWithSliverConstraints(Constraints());

        var inside = new BoxHitTestResult();
        var outside = new BoxHitTestResult();

        Assert.True(sliver.HitTest(inside, new Point(20.0, 23.0)));
        Assert.Single(inside.Path);
        Assert.False(sliver.HitTest(outside, new Point(20.0, 24.0)));
        Assert.Empty(outside.Path);
    }

    [Fact]
    public void RenderSliverPadding_CombinesChildHitTestExtentWithPadding()
    {
        var child = new HitTestSliver(new SliverGeometry(
            ScrollExtent: 10.0,
            PaintExtent: 10.0,
            MaxPaintExtent: 10.0,
            HitTestExtent: 24.0));
        var padding = new RenderSliverPadding(new Thickness(0.0, 5.0, 0.0, 7.0), child);

        padding.LayoutWithSliverConstraints(Constraints());

        Assert.Equal(29.0, padding.Geometry.HitTestExtent);
    }

    [Fact]
    public void RenderProxySliver_PaintsAccordingToVisibleInsteadOfPaintExtent()
    {
        var hiddenChild = new HitTestSliver(new SliverGeometry(
            PaintExtent: 10.0,
            MaxPaintExtent: 10.0,
            Visible: false));
        var visibleChild = new HitTestSliver(new SliverGeometry(Visible: true));
        var hiddenProxy = new RenderSliverIgnorePointer(sliver: hiddenChild);
        var visibleProxy = new RenderSliverIgnorePointer(sliver: visibleChild);
        hiddenProxy.LayoutWithSliverConstraints(Constraints());
        visibleProxy.LayoutWithSliverConstraints(Constraints());

        hiddenProxy.Paint(new PaintingContext(new OffsetLayer()), default);
        visibleProxy.Paint(new PaintingContext(new OffsetLayer()), default);

        Assert.Equal(0, hiddenChild.PaintCount);
        Assert.Equal(1, visibleChild.PaintCount);
    }

    private static SliverConstraints Constraints() => new(
        Axis: Axis.Vertical,
        ScrollOffset: 0.0,
        RemainingPaintExtent: 100.0,
        CrossAxisExtent: 40.0,
        ViewportMainAxisExtent: 100.0,
        RemainingCacheExtent: 100.0);

    private sealed class HitTestSliver(SliverGeometry geometry) : RenderSliver
    {
        public int PaintCount { get; private set; }

        protected override void PerformSliverLayout(SliverConstraints constraints)
        {
            Geometry = geometry;
        }

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext context, Point offset)
        {
            PaintCount += 1;
        }
    }
}
