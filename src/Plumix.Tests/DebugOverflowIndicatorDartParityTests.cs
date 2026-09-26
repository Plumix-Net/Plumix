using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/debug_overflow_indicator.dart
// Mirrors flutter/packages/flutter/test/rendering/debug_overflow_indicator_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class DebugOverflowIndicatorDartParityTests
{
    // Flutter: "overflow indicator is not shown when not overflowing"
    [Fact]
    public void OverflowIndicatorIsNotShownWhenNotOverflowing()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: new UnconstrainedBox(child: new SizedBox(width: 200.0, height: 200.0))));

        PaintAssert.DoesNotPaint(RenderOf<UnconstrainedBox>(tester), PaintPattern.Paints.Rect());
    }

    // Flutter: "overflow indicator is shown when overflowing"
    [DebugOnlyFact]
    public void OverflowIndicatorIsShownWhenOverflowing()
    {
        using var tester = new FrameworkDartTester();
        var box = new UnconstrainedBox(child: new SizedBox(width: 200.0, height: 200.0));
        tester.PumpWidget(new Center(child: new SizedBox(height: 100.0, child: box)));

        var exception = Assert.IsType<FlutterError>(tester.TakeException());
        Assert.Equal(DiagnosticLevel.Summary, exception.Diagnostics[0].Level);
        Assert.StartsWith("A RenderConstraintsTransformBox overflowed by ", exception.Diagnostics[0].ToString());
        PaintAssert.Paints(RenderOf<UnconstrainedBox>(tester), PaintPattern.Paints.Rect());

        tester.PumpWidget(new Center(child: new SizedBox(height: 100.0, child: box)));

        // Doesn't throw the exception a second time, because we didn't reset overflowReportNeeded.
        Assert.Null(tester.TakeException());
        PaintAssert.Paints(RenderOf<UnconstrainedBox>(tester), PaintPattern.Paints.Rect());
    }

    // Flutter: "overflow indicator is not shown when constraint size is zero."
    [Fact]
    public void OverflowIndicatorIsNotShownWhenConstraintSizeIsZero()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(
            child: new SizedBox(
                height: 0.0,
                child: new UnconstrainedBox(child: new SizedBox(width: 200.0, height: 200.0)))));

        PaintAssert.DoesNotPaint(RenderOf<UnconstrainedBox>(tester), PaintPattern.Paints.Rect());
    }

    // The mixin's regions and labels (debug_overflow_indicator.dart `_calculateOverflowRegions`): the top
    // marker is the container's upper tenth with its label 1px below the top centre, the bottom marker
    // the lower tenth with its label 8.5px above the bottom centre; top is painted before bottom.
    [DebugOnlyFact]
    public void TopAndBottomOverflowPaintMarkersAndCentredLabelsLikeDart()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(
            child: new SizedBox(
                width: 100.0,
                height: 100.0,
                child: new UnconstrainedBox(child: new SizedBox(width: 50.0, height: 200.0)))));
        var exception = Assert.IsType<FlutterError>(tester.TakeException());
        Assert.StartsWith(
            "A RenderConstraintsTransformBox overflowed by 50 pixels on the top and 50 pixels on the bottom.",
            exception.Diagnostics[0].ToString());

        PaintAssert.Paints(
            RenderOf<UnconstrainedBox>(tester),
            PaintPattern.Paints
                .Rect(rect: new Avalonia.Rect(0.0, 0.0, 100.0, 10.0))
                .Save()
                .Translate(x: 50.0, y: 1.0)
                .Rotate(angle: 0.0)
                .Rect(color: new Plumix.UI.Color(0xFFFFFFFF))
                .Paragraph()
                .Restore()
                .Rect(rect: new Avalonia.Rect(0.0, 90.0, 100.0, 10.0))
                .Save()
                .Translate(x: 50.0, y: 91.5)
                .Rotate(angle: 0.0)
                .Paragraph()
                .Restore());
    }

    // `_formatPixels`: whole pixels above 10, one decimal above 1, else three significant digits
    // (Dart's `toStringAsPrecision(3)`).
    [Theory]
    [InlineData(0.5, "0.500")]
    [InlineData(0.001234, "0.00123")]
    [InlineData(0.0000001, "1.00e-7")]
    [InlineData(9.9996, "10.0")]
    [InlineData(123.0, "123")]
    [InlineData(1234.0, "1.23e+3")]
    public void ToStringAsPrecisionMatchesDart(double value, string expected)
    {
        Assert.Equal(expected, Diagnostics.ToStringAsPrecision(value, 3));
    }

    private static RenderObject RenderOf<TWidget>(FrameworkDartTester tester)
        where TWidget : Widget => tester.ElementOfType<TWidget>().FindRenderObject()!;
}
