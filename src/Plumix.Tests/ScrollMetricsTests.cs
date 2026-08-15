using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_metrics.dart (parity regression
// tests mapped from flutter/packages/flutter/test/widgets/notification_test.dart and
// flutter/packages/flutter/test/widgets/scroll_notification_test.dart)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ScrollMetricsTests
{
    private static FixedScrollMetrics Metrics(
        double? minScrollExtent = 0.0,
        double? maxScrollExtent = 100.0,
        double? pixels = 25.0,
        double? viewportDimension = 40.0,
        AxisDirection axisDirection = AxisDirection.Down,
        double devicePixelRatio = 1.0)
    {
        return new FixedScrollMetrics(
            minScrollExtent: minScrollExtent,
            maxScrollExtent: maxScrollExtent,
            pixels: pixels,
            viewportDimension: viewportDimension,
            axisDirection: axisDirection,
            devicePixelRatio: devicePixelRatio);
    }

    // ------------------------------------------------------------------ API and defaults

    [Fact]
    public void FixedScrollMetrics_ExposesEveryConstructorValue()
    {
        FixedScrollMetrics metrics = Metrics(
            minScrollExtent: 1.0,
            maxScrollExtent: 2.0,
            pixels: 3.0,
            viewportDimension: 4.0,
            axisDirection: AxisDirection.Right,
            devicePixelRatio: 5.0);

        Assert.Equal(1.0, metrics.MinScrollExtent);
        Assert.Equal(2.0, metrics.MaxScrollExtent);
        Assert.Equal(3.0, metrics.Pixels);
        Assert.Equal(4.0, metrics.ViewportDimension);
        Assert.Equal(AxisDirection.Right, metrics.AxisDirection);
        Assert.Equal(5.0, metrics.DevicePixelRatio);
        Assert.True(metrics.HasContentDimensions);
        Assert.True(metrics.HasPixels);
        Assert.True(metrics.HasViewportDimension);
    }

    [Fact]
    public void FixedScrollMetrics_ReportsMissingValuesThroughTheHasFlags()
    {
        FixedScrollMetrics none = Metrics(
            minScrollExtent: null,
            maxScrollExtent: null,
            pixels: null,
            viewportDimension: null);

        Assert.False(none.HasContentDimensions);
        Assert.False(none.HasPixels);
        Assert.False(none.HasViewportDimension);
        Assert.Throws<InvalidOperationException>(() => none.MinScrollExtent);
        Assert.Throws<InvalidOperationException>(() => none.MaxScrollExtent);
        Assert.Throws<InvalidOperationException>(() => none.Pixels);
        Assert.Throws<InvalidOperationException>(() => none.ViewportDimension);

        // Dart's `hasContentDimensions` needs both halves of the range.
        Assert.False(Metrics(maxScrollExtent: null).HasContentDimensions);
        Assert.False(Metrics(minScrollExtent: null).HasContentDimensions);
    }

    [Theory]
    [InlineData(AxisDirection.Up, Axis.Vertical)]
    [InlineData(AxisDirection.Down, Axis.Vertical)]
    [InlineData(AxisDirection.Left, Axis.Horizontal)]
    [InlineData(AxisDirection.Right, Axis.Horizontal)]
    public void FixedScrollMetrics_AxisFollowsTheAxisDirection(AxisDirection direction, Axis expected)
    {
        Assert.Equal(expected, Metrics(axisDirection: direction).Axis);
    }

    // ------------------------------------------------------------------ derived extents

    [Fact]
    public void FixedScrollMetrics_ExtentsSplitTheContentAroundTheViewport()
    {
        FixedScrollMetrics metrics = Metrics(pixels: 25.0);

        Assert.Equal(25.0, metrics.ExtentBefore);
        Assert.Equal(40.0, metrics.ExtentInside);
        Assert.Equal(75.0, metrics.ExtentAfter);
        Assert.Equal(140.0, metrics.ExtentTotal);
        Assert.False(metrics.OutOfRange);
        Assert.False(metrics.AtEdge);
    }

    [Fact]
    public void FixedScrollMetrics_ExtentInsideShrinksByTheOverscroll()
    {
        // Leading overscroll: 10 pixels of the viewport show empty space above the content.
        Assert.Equal(30.0, Metrics(pixels: -10.0).ExtentInside);

        // Trailing overscroll behaves symmetrically.
        Assert.Equal(30.0, Metrics(pixels: 110.0).ExtentInside);

        // The overscroll is clamped at the viewport dimension, so the value never goes negative.
        Assert.Equal(0.0, Metrics(pixels: -1000.0).ExtentInside);
        Assert.Equal(0.0, Metrics(pixels: 1000.0).ExtentInside);
    }

    [Fact]
    public void FixedScrollMetrics_ExtentTotalIgnoresTheCurrentOffset()
    {
        // Dart computes it from the range and the viewport, not from the three extents, so an
        // overscrolled position still reports the same total.
        Assert.Equal(140.0, Metrics(pixels: 25.0).ExtentTotal);
        Assert.Equal(140.0, Metrics(pixels: -50.0).ExtentTotal);
        Assert.Equal(140.0, Metrics(pixels: 150.0).ExtentTotal);
    }

    [Fact]
    public void FixedScrollMetrics_OutOfRangeAndAtEdgeFollowTheExtents()
    {
        Assert.True(Metrics(pixels: -0.1).OutOfRange);
        Assert.True(Metrics(pixels: 100.1).OutOfRange);
        Assert.False(Metrics(pixels: 0.0).OutOfRange);
        Assert.False(Metrics(pixels: 100.0).OutOfRange);

        // atEdge is an exact comparison in Dart, not a tolerance.
        Assert.True(Metrics(pixels: 0.0).AtEdge);
        Assert.True(Metrics(pixels: 100.0).AtEdge);
        Assert.False(Metrics(pixels: 0.0001).AtEdge);
        Assert.False(Metrics(pixels: 99.9999).AtEdge);
    }

    [Fact]
    public void FixedScrollMetrics_ToStringReportsTheThreeExtents()
    {
        Assert.Equal("FixedScrollMetrics(25.0..[40.0]..75.0)", Metrics().ToString());
    }

    // ------------------------------------------------------------------ copyWith

    [Fact]
    public void CopyWith_KeepsEveryValueWhenNothingIsOverridden()
    {
        FixedScrollMetrics source = Metrics(
            minScrollExtent: 1.0,
            maxScrollExtent: 2.0,
            pixels: 3.0,
            viewportDimension: 4.0,
            axisDirection: AxisDirection.Left,
            devicePixelRatio: 5.0);
        FixedScrollMetrics copy = source.CopyWith();

        Assert.NotSame(source, copy);
        Assert.Equal(1.0, copy.MinScrollExtent);
        Assert.Equal(2.0, copy.MaxScrollExtent);
        Assert.Equal(3.0, copy.Pixels);
        Assert.Equal(4.0, copy.ViewportDimension);
        Assert.Equal(AxisDirection.Left, copy.AxisDirection);
        Assert.Equal(5.0, copy.DevicePixelRatio);
    }

    [Fact]
    public void CopyWith_ReplacesOnlyTheSuppliedValues()
    {
        FixedScrollMetrics source = Metrics();

        Assert.Equal(-5.0, source.CopyWith(minScrollExtent: -5.0).MinScrollExtent);
        Assert.Equal(500.0, source.CopyWith(maxScrollExtent: 500.0).MaxScrollExtent);
        Assert.Equal(7.0, source.CopyWith(pixels: 7.0).Pixels);
        Assert.Equal(9.0, source.CopyWith(viewportDimension: 9.0).ViewportDimension);
        Assert.Equal(
            AxisDirection.Up,
            source.CopyWith(axisDirection: AxisDirection.Up).AxisDirection);
        Assert.Equal(3.0, source.CopyWith(devicePixelRatio: 3.0).DevicePixelRatio);

        // Everything else is carried over untouched.
        FixedScrollMetrics moved = source.CopyWith(pixels: 7.0);
        Assert.Equal(0.0, moved.MinScrollExtent);
        Assert.Equal(100.0, moved.MaxScrollExtent);
        Assert.Equal(40.0, moved.ViewportDimension);
    }

    [Fact]
    public void CopyWith_KeepsUnavailableValuesUnavailable()
    {
        FixedScrollMetrics source = Metrics(
            minScrollExtent: null,
            maxScrollExtent: null,
            pixels: null,
            viewportDimension: null);
        FixedScrollMetrics copy = source.CopyWith(pixels: 3.0);

        Assert.True(copy.HasPixels);
        Assert.Equal(3.0, copy.Pixels);
        Assert.False(copy.HasContentDimensions);
        Assert.False(copy.HasViewportDimension);
    }

    // ------------------------------------------------------------------ ScrollPosition

    [Fact]
    public void ScrollPosition_ImplementsTheMetricsContract()
    {
        using var position = new ScrollPosition(initialPixels: 25);
        position.AxisDirection = AxisDirection.Right;
        position.DevicePixelRatio = 2.0;

        Assert.True(position.HasPixels);
        Assert.False(position.HasContentDimensions);
        Assert.False(position.HasViewportDimension);

        position.ApplyViewportDimension(40);
        position.ApplyContentDimensions(0, 100);

        Assert.True(position.HasContentDimensions);
        Assert.True(position.HasViewportDimension);
        Assert.Equal(Axis.Horizontal, position.Axis);
        Assert.Equal(25.0, position.ExtentBefore);
        Assert.Equal(40.0, position.ExtentInside);
        Assert.Equal(75.0, position.ExtentAfter);
        Assert.Equal(140.0, position.ExtentTotal);
        Assert.False(position.AtEdge);
        Assert.False(position.OutOfRange);
    }

    [Fact]
    public void ScrollPosition_CopyWithSnapshotsTheLiveMetrics()
    {
        using var position = new ScrollPosition(initialPixels: 25);
        position.AxisDirection = AxisDirection.Up;
        position.DevicePixelRatio = 2.5;
        position.ApplyViewportDimension(40);
        position.ApplyContentDimensions(0, 100);

        IScrollMetrics snapshot = position.CopyWith();
        Assert.IsType<FixedScrollMetrics>(snapshot);
        Assert.Equal(25.0, snapshot.Pixels);
        Assert.Equal(0.0, snapshot.MinScrollExtent);
        Assert.Equal(100.0, snapshot.MaxScrollExtent);
        Assert.Equal(40.0, snapshot.ViewportDimension);
        Assert.Equal(AxisDirection.Up, snapshot.AxisDirection);
        Assert.Equal(2.5, snapshot.DevicePixelRatio);

        // The snapshot does not follow the position afterwards.
        position.JumpTo(60);
        Assert.Equal(25.0, snapshot.Pixels);
        Assert.Equal(60.0, position.Pixels);

        // The overrides Dart's copyWith accepts are honoured on a position too.
        Assert.Equal(7.0, position.CopyWith(pixels: 7.0).Pixels);
    }

    [Fact]
    public void ScrollPosition_CopyWithBeforeLayoutLeavesTheMissingValuesUnavailable()
    {
        // A page position is Flutter's own `ScrollPosition(initialPixels: null)` caller: it derives
        // its offset from the first viewport dimension instead of carrying one from the start.
        using var controller = new PageController();
        var position = new PagePosition();

        IScrollMetrics snapshot = position.CopyWith();
        Assert.False(snapshot.HasPixels);
        Assert.False(snapshot.HasContentDimensions);
        Assert.False(snapshot.HasViewportDimension);
        position.Dispose();
    }

    // ------------------------------------------------------------------ subclasses

    /// <remarks>
    /// Dart's <c>_NestedScrollMetrics</c> extends <c>FixedScrollMetrics</c> and adds three values its
    /// <c>copyWith</c> carries over. C# forbids widening an override's parameter list, so those three
    /// move to a separate overload; both forms must keep every inherited value.
    /// </remarks>
    [Fact]
    public void NestedScrollMetrics_IsAFixedScrollMetricsThatCarriesItsRange()
    {
        var metrics = new NestedScrollMetrics(
            minScrollExtent: 0.0,
            maxScrollExtent: 100.0,
            pixels: 25.0,
            viewportDimension: 40.0,
            axisDirection: AxisDirection.Down,
            devicePixelRatio: 2.0,
            minRange: 10.0,
            maxRange: 90.0,
            correctionOffset: -5.0);

        Assert.IsAssignableFrom<FixedScrollMetrics>(metrics);
        Assert.Equal(25.0, metrics.ExtentBefore);
        Assert.Equal(40.0, metrics.ExtentInside);
        Assert.Equal(10.0, metrics.MinRange);
        Assert.Equal(90.0, metrics.MaxRange);
        Assert.Equal(-5.0, metrics.CorrectionOffset);

        // The inherited override keeps the three extra values.
        NestedScrollMetrics moved = metrics.CopyWith(pixels: 60.0);
        Assert.Equal(60.0, moved.Pixels);
        Assert.Equal(2.0, moved.DevicePixelRatio);
        Assert.Equal(10.0, moved.MinRange);
        Assert.Equal(90.0, moved.MaxRange);
        Assert.Equal(-5.0, moved.CorrectionOffset);

        // The widened overload replaces only what it is given.
        NestedScrollMetrics ranged = metrics.CopyWith(
            minRange: 1.0,
            maxRange: null,
            correctionOffset: null);
        Assert.Equal(1.0, ranged.MinRange);
        Assert.Equal(90.0, ranged.MaxRange);
        Assert.Equal(-5.0, ranged.CorrectionOffset);
        Assert.Equal(25.0, ranged.Pixels);
    }
}
