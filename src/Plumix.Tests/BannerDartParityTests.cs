using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using BoxShadow = Plumix.Rendering.BoxShadow;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/banner.dart
// Mirrors flutter/packages/flutter/test/widgets/banner_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class BannerDartParityTests
{
    // The textDirection values below are intentionally sometimes different and sometimes the same as the
    // layoutDirection, to make sure that they don't affect the layout.
    [Theory]
    // "A Banner with a location of topStart paints in the top left (LTR)"
    [InlineData(TextDirection.Rtl, BannerLocation.TopStart, TextDirection.Ltr, false, false, -1)]
    // "A Banner with a location of topStart paints in the top right (RTL)"
    [InlineData(TextDirection.Ltr, BannerLocation.TopStart, TextDirection.Rtl, true, false, 1)]
    // "A Banner with a location of topEnd paints in the top right (LTR)"
    [InlineData(TextDirection.Ltr, BannerLocation.TopEnd, TextDirection.Ltr, true, false, 1)]
    // "A Banner with a location of topEnd paints in the top left (RTL)"
    [InlineData(TextDirection.Rtl, BannerLocation.TopEnd, TextDirection.Rtl, false, false, -1)]
    // "A Banner with a location of bottomStart paints in the bottom left (LTR)"
    [InlineData(TextDirection.Ltr, BannerLocation.BottomStart, TextDirection.Ltr, false, true, 1)]
    // "A Banner with a location of bottomStart paints in the bottom right (RTL)"
    [InlineData(TextDirection.Rtl, BannerLocation.BottomStart, TextDirection.Rtl, true, true, -1)]
    // "A Banner with a location of bottomEnd paints in the bottom right (LTR)"
    [InlineData(TextDirection.Rtl, BannerLocation.BottomEnd, TextDirection.Ltr, true, true, -1)]
    // "A Banner with a location of bottomEnd paints in the bottom left (RTL)"
    [InlineData(TextDirection.Ltr, BannerLocation.BottomEnd, TextDirection.Rtl, false, true, 1)]
    public void BannerPainterPaintsInTheExpectedCorner(
        TextDirection textDirection,
        BannerLocation location,
        TextDirection layoutDirection,
        bool right,
        bool bottom,
        int rotationSign)
    {
        var bannerPainter = new BannerPainter(
            message: "foo",
            textDirection: textDirection,
            location: location,
            layoutDirection: layoutDirection);
        var context = new TestRecordingPaintingContext();

        bannerPainter.Paint(context, new Size(1000.0, 1000.0));

        CanvasCall translateCommand = context.Calls.First(call => call.Method == "translate");
        if (right)
        {
            Assert.True(translateCommand.Dx > 900.0);
        }
        else
        {
            Assert.True(translateCommand.Dx < 100.0);
        }

        if (bottom)
        {
            Assert.True(translateCommand.Dy > 900.0);
        }
        else
        {
            Assert.True(translateCommand.Dy < 100.0);
        }

        CanvasCall rotateCommand = context.Calls.First(call => call.Method == "rotate");
        Assert.Equal(rotationSign * Math.PI / 4.0, rotateCommand.Radius);
        bannerPainter.Dispose();
    }

    // Flutter: "Banner widget"
    [Fact]
    public void BannerWidget()
    {
        bool oldDisableShadows = RenderingDebug.DisableShadows;
        RenderingDebug.DisableShadows = false;
        try
        {
            using var tester = new FrameworkDartTester();
            tester.PumpWidget(new Directionality(
                textDirection: TextDirection.Ltr,
                child: new Banner(message: "Hello", location: BannerLocation.TopEnd)));

            PaintAssert.Paints(
                tester.ElementOfType<CustomPaint>().FindRenderObject()!,
                ExpectedTopEndBanner(width: 800.0));
        }
        finally
        {
            RenderingDebug.DisableShadows = oldDisableShadows;
        }
    }

    // Flutter: "Banner widget in WidgetsApp". Plumix's `CheckedModeBanner` is the same Banner with
    // Dart's DEBUG message; it is painted only in debug builds, as in Dart.
    [DebugOnlyFact]
    public void CheckedModeBannerPaintsTheDebugBanner()
    {
        bool oldDisableShadows = RenderingDebug.DisableShadows;
        RenderingDebug.DisableShadows = false;
        try
        {
            using var tester = new FrameworkDartTester();
            tester.PumpWidget(new Directionality(
                textDirection: TextDirection.Ltr,
                child: new CheckedModeBanner(child: new Placeholder())));

            RenderObject customPaint = tester.ElementsOfType<CustomPaint>()
                .Select(element => element.FindRenderObject()!)
                .First(renderObject => ((RenderCustomPaint)renderObject).ForegroundPainter is BannerPainter);
            PaintAssert.Paints(customPaint, ExpectedTopEndBanner(width: 800.0));
        }
        finally
        {
            RenderingDebug.DisableShadows = oldDisableShadows;
        }
    }

    // Flutter: "Can configure shadow for Banner widget"
    [Fact]
    public void CanConfigureShadowForBannerWidget()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Banner(
                message: "Shadow banner",
                location: BannerLocation.TopEnd,
                shadow: new BoxShadow(color: new Color(0xFF008000), blurRadius: 8.0))));

        Element customPaint = Assert.Single(tester.ElementsOfType<CustomPaint>());
        var painter = Assert.IsType<BannerPainter>(((CustomPaint)customPaint.Widget).ForegroundPainter);
        Assert.Equal(new Color(0xFF008000), painter.Shadow.Color);
        Assert.Equal(8.0, painter.Shadow.BlurRadius);
    }

    // Flutter: "Banner does not crash at zero area"
    [Fact]
    public void BannerDoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Center(
                child: new SizedBox(
                    width: 0.0,
                    height: 0.0,
                    child: new Banner(
                        message: "X",
                        textDirection: TextDirection.Ltr,
                        location: BannerLocation.BottomEnd,
                        layoutDirection: TextDirection.Ltr)))));

        Assert.Equal(default, ((RenderBox)tester.ElementOfType<Banner>().FindRenderObject()!).Size);
    }

    // Flutter: "CheckedModeBanner does not crash at zero area"
    [Fact]
    public void CheckedModeBannerDoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Center(
                child: new SizedBox(
                    width: 0.0,
                    height: 0.0,
                    child: new CheckedModeBanner(child: new Text("X"))))));

        Assert.Equal(default, ((RenderBox)tester.ElementOfType<CheckedModeBanner>().FindRenderObject()!).Size);
    }

    // `BannerPainter.dispose` releases its text painter, like Dart's, and a disposed painter cannot paint.
    [Fact]
    public void DisposedBannerPainterCannotPaint()
    {
        var bannerPainter = new BannerPainter(
            message: "foo",
            textDirection: TextDirection.Rtl,
            location: BannerLocation.TopStart,
            layoutDirection: TextDirection.Ltr);
        bannerPainter.Paint(new TestRecordingPaintingContext(), new Size(100.0, 100.0));
        bannerPainter.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () => bannerPainter.Paint(new TestRecordingPaintingContext(), new Size(100.0, 100.0)));
    }

    private static PaintPattern ExpectedTopEndBanner(double width) => PaintPattern.Paints
        .Save()
        .Translate(x: width, y: 0.0)
        .Rotate(angle: Math.PI / 4.0)
        .Rect(rect: new Rect(-40.0, 28.0, 80.0, 12.0), color: new Color(0x7f000000), hasMaskFilter: true)
        .Rect(rect: new Rect(-40.0, 28.0, 80.0, 12.0), color: new Color(0xa0b71c1c), hasMaskFilter: false)
        .Paragraph(offset: new Point(-40.0, 29.0))
        .Restore();
}
