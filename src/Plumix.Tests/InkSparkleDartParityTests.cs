// Dart parity source: material_ui/lib/src/ink_sparkle.dart
// Mirrors material-ui-src/test/ink_sparkle_test.dart

using Plumix.UI;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;
using MaterialWidget = Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class InkSparkleDartParityTests
{
    private const string GoldenSkip =
        "matchesGoldenFile: Plumix has no golden-image comparison infrastructure, and InkSparkle cannot run "
        + "Flutter's fragment shader (it paints one radial-gradient rect), so the sparkle goldens cannot match.";

    // Flutter: "InkSparkle in a Button compiles and does not crash"
    [Fact]
    public void InkSparkleInAButtonCompilesAndDoesNotCrash()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialApp(
                home: new Scaffold(
                    body: new Center(
                        child: new ElevatedButton(
                            style: ElevatedButton.StyleFrom(splashFactory: InkSparkle.SplashFactory),
                            child: new Text("Sparkle!"),
                            onPressed: () => { })))));
        Element button = tester.ElementsWithText("Sparkle!").Single();
        Tap(tester, button);
        tester.Pump();
        tester.PumpAndSettle();
        Assert.Null(tester.TakeException());
    }

    // Flutter: "InkSparkle default splashFactory paints with drawRect when bounded"
    [Fact]
    public void InkSparkleDefaultSplashFactoryPaintsWithDrawRectWhenBounded()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialApp(
                home: new Scaffold(
                    body: new Center(
                        child: new InkWell(
                            splashFactory: InkSparkle.SplashFactory,
                            child: new Text("Sparkle!"),
                            onTap: () => { })))));
        Element button = tester.ElementsWithText("Sparkle!").Single();
        Tap(tester, button);
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(200));

        var material = (RenderInkFeatures)MaterialWidget.Of(button);
        Assert.Equal(1, PaintRecording.Record(material).CountCalls("drawRect"));

        Assert.Single(material.DebugInkFeatures!);

        tester.PumpAndSettle();
        // ink feature is disposed.
        Assert.Empty(material.DebugInkFeatures!);
    }

    // Flutter: "InkSparkle default splashFactory paints with drawPaint when unbounded"
    [Fact]
    public void InkSparkleDefaultSplashFactoryPaintsWithDrawPaintWhenUnbounded()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialApp(
                home: new Scaffold(
                    body: new Center(
                        child: new InkResponse(
                            splashFactory: InkSparkle.SplashFactory,
                            child: new Text("Sparkle!"),
                            onTap: () => { })))));
        Element button = tester.ElementsWithText("Sparkle!").Single();
        Tap(tester, button);
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(200));

        var material = (RenderInkFeatures)MaterialWidget.Of(button);
        Assert.Equal(1, PaintRecording.Record(material).CountCalls("drawPaint"));
    }

    /////////////
    // Goldens //
    /////////////

    // Flutter: "Material2 - InkSparkle renders with sparkles when top left of button is tapped"
    [Fact(Skip = GoldenSkip)]
    public void Material2InkSparkleRendersWithSparklesWhenTopLeftOfButtonIsTapped()
    {
    }

    // Flutter: "Material3 - InkSparkle renders with sparkles when top left of button is tapped"
    [Fact(Skip = GoldenSkip)]
    public void Material3InkSparkleRendersWithSparklesWhenTopLeftOfButtonIsTapped()
    {
    }

    // Flutter: "Material2 - InkSparkle renders with sparkles when center of button is tapped"
    [Fact(Skip = GoldenSkip)]
    public void Material2InkSparkleRendersWithSparklesWhenCenterOfButtonIsTapped()
    {
    }

    // Flutter: "Material3 - InkSparkle renders with sparkles when center of button is tapped"
    [Fact(Skip = GoldenSkip)]
    public void Material3InkSparkleRendersWithSparklesWhenCenterOfButtonIsTapped()
    {
    }

    // Flutter: "Material2 - InkSparkle renders with sparkles when bottom right of button is tapped"
    [Fact(Skip = GoldenSkip)]
    public void Material2InkSparkleRendersWithSparklesWhenBottomRightOfButtonIsTapped()
    {
    }

    // Flutter: "Material3 - InkSparkle renders with sparkles when bottom right of button is tapped"
    [Fact(Skip = GoldenSkip)]
    public void Material3InkSparkleRendersWithSparklesWhenBottomRightOfButtonIsTapped()
    {
    }

    private static FrameworkDartTester CreateTester() => new(fakeGestureTimers: true);

    // `tester.tap(finder)`: a down and an up at the center, no pump.
    private static void Tap(FrameworkDartTester tester, Element element) =>
        tester.StartGesture(tester.GetCenter(element), PointerDeviceKind.Touch).Up();
}
