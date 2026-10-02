// Dart parity source: material_ui/lib/src/ink_decoration.dart
// Mirrors material-ui-src/test/ink_paint_test.dart

using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MaterialWidget = Plumix.Material.Material;
using TextDirection = Plumix.UI.TextDirection;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class InkPaintDartParityTests : IDisposable
{
    private const string GoldenSkip =
        "matchesGoldenFile: Plumix has no golden-image comparison infrastructure, and InkSparkle's fragment "
        + "shader is approximated by a radial gradient, so the Material 3 splash goldens cannot be matched.";

    public InkPaintDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
    }

    // Flutter: "The Ink widget expands when no dimensions are set"
    [Fact]
    public void TheInkWidgetExpandsWhenNoDimensionsAreSet()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialWidget(child: new Ink()));
        Assert.Single(tester.ElementsOfType<Ink>());
        Assert.Equal(new Size(800.0, 600.0), tester.GetSize(tester.ElementOfType<Ink>()));
    }

    // Flutter: "The Ink widget fits the specified size"
    [Fact]
    public void TheInkWidgetFitsTheSpecifiedSize()
    {
        using FrameworkDartTester tester = CreateTester();
        const double height = 150.0;
        const double width = 200.0;
        tester.PumpWidget(
            new MaterialWidget(
                child: new Center(
                    // used to constrain to child's size
                    child: new Ink(height: height, width: width))));
        tester.PumpAndSettle();
        Assert.Single(tester.ElementsOfType<Ink>());
        Assert.Equal(new Size(width, height), tester.GetSize(tester.ElementOfType<Ink>()));
    }

    // Flutter: "The Ink widget expands on a unspecified dimension"
    [Fact]
    public void TheInkWidgetExpandsOnAnUnspecifiedDimension()
    {
        using FrameworkDartTester tester = CreateTester();
        const double height = 150.0;
        tester.PumpWidget(
            new MaterialWidget(
                child: new Center(
                    // used to constrain to child's size
                    child: new Ink(height: height))));
        tester.PumpAndSettle();
        Assert.Single(tester.ElementsOfType<Ink>());
        Assert.Equal(new Size(800, height), tester.GetSize(tester.ElementOfType<Ink>()));
    }

    // Flutter: "Material2 - InkWell widget renders an ink splash"
    [Fact]
    public void Material2InkWellWidgetRendersAnInkSplash()
    {
        using FrameworkDartTester tester = CreateTester();
        var splashColor = new Color(0xAA0000FF);
        BorderRadius borderRadius = BorderRadius.All(Radius.Circular(6.0));

        tester.PumpWidget(
            new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new MaterialWidget(
                    child: new Center(
                        child: new SizedBox(
                            width: 200.0,
                            height: 60.0,
                            child: new InkWell(
                                borderRadius: borderRadius,
                                splashColor: splashColor,
                                onTap: () => { }))))));

        Point center = tester.GetCenter(InkWellOf(tester));
        TestGesture gesture = StartGesture(tester, center);
        tester.Pump(); // start gesture
        tester.Pump(TimeSpan.FromMilliseconds(200)); // wait for splash to be well under way

        RenderObject box = MaterialOf(InkWellOf(tester));
        PaintAssert.Paints(
            box,
            PaintPattern.Paints
                .Translate(x: 0.0, y: 0.0)
                .Save()
                .Translate(x: 300.0, y: 270.0)
                .ClipRRect(rrect: RRect.FromLTRBR(0.0, 0.0, 200.0, 60.0, Radius.Circular(6.0)))
                .Circle(x: 100.0, y: 30.0, radius: 21.0, color: splashColor)
                .Restore());

        gesture.Up();
    }

    // Flutter: "Material3 - InkWell widget renders an ink splash"
    // The paint sequence (the non-web branch) is checked; the trailing
    // `matchesGoldenFile('m3_ink_well.renders.ink_splash.png')` is not (see GoldenSkip).
    [Fact]
    public void Material3InkWellWidgetRendersAnInkSplash()
    {
        using FrameworkDartTester tester = CreateTester();
        var inkWellKey = new ValueKey<string>("InkWell");
        var splashColor = new Color(0xAA0000FF);
        BorderRadius borderRadius = BorderRadius.All(Radius.Circular(6.0));

        tester.PumpWidget(
            new MaterialApp(
                home: new MaterialWidget(
                    child: new Center(
                        child: new SizedBox(
                            width: 200.0,
                            height: 60.0,
                            child: new InkWell(
                                key: inkWellKey,
                                borderRadius: borderRadius,
                                splashColor: splashColor,
                                onTap: () => { }))))));

        Point center = tester.GetCenter(InkWellOf(tester));
        TestGesture gesture = StartGesture(tester, center);
        tester.Pump(); // start gesture
        tester.Pump(TimeSpan.FromMilliseconds(200)); // wait for splash to be well under way

        RenderObject box = MaterialOf(InkWellOf(tester));
        PaintAssert.Paints(
            box,
            PaintPattern.Paints
                .Translate(x: 0.0, y: 0.0)
                .Save()
                .Translate(x: 300.0, y: 270.0)
                .ClipRRect(rrect: RRect.FromLTRBR(0.0, 0.0, 200.0, 60.0, Radius.Circular(6.0)))
                .Rect(rect: Ltrb(0.0, 0.0, 200, 60))
                .Restore());

        // Material 3 uses the InkSparkle which uses a shader, so we can't capture
        // the effect with paint methods. Use a golden test instead.
        // matchesGoldenFile('m3_ink_well.renders.ink_splash.png'): not portable (GoldenSkip).

        gesture.Up();
    }

    // Flutter: "The InkWell widget renders an ink ripple"
    [Fact]
    public void TheInkWellWidgetRendersAnInkRipple()
    {
        using FrameworkDartTester tester = CreateTester();
        var highlightColor = new Color(0xAAFF0000);
        var splashColor = new Color(0xB40000FF);
        BorderRadius borderRadius = BorderRadius.All(Radius.Circular(6.0));

        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100.0,
                            child: new InkWell(
                                borderRadius: borderRadius,
                                highlightColor: highlightColor,
                                splashColor: splashColor,
                                onTap: () => { },
                                radius: 100.0,
                                splashFactory: InkRipple.SplashFactory))))));

        Point tapDownOffset = tester.GetTopLeft(InkWellOf(tester));
        Point inkWellCenter = tester.GetCenter(InkWellOf(tester));
        TapAt(tester, tapDownOffset);
        tester.Pump(); // start gesture

        RenderObject box = MaterialOf(InkWellOf(tester));

        PaintPattern RipplePattern(Point expectedCenter, double expectedRadius, int expectedAlpha)
        {
            return PaintPattern.Paints
                .Translate(x: 0.0, y: 0.0)
                .Translate(x: tapDownOffset.X, y: tapDownOffset.Y)
                .Something(call => CircleMatches(call, expectedCenter, expectedRadius, expectedAlpha));
        }

        // Initially the ripple's center is where the tap occurred;
        // ripplePattern always add a translation of tapDownOffset.
        PaintAssert.Paints(box, RipplePattern(new Point(0, 0), 30.0, 0));

        // The ripple fades in for 75ms. During that time its alpha is eased from
        // 0 to the splashColor's alpha value and its center moves towards the
        // center of the ink well.
        tester.Pump(TimeSpan.FromMilliseconds(50));
        PaintAssert.Paints(box, RipplePattern(new Point(17.0, 17.0), 56.0, 120));

        // At 75ms the ripple has fade in: it's alpha matches the splashColor's
        // alpha and its center has moved closer to the ink well's center.
        tester.Pump(TimeSpan.FromMilliseconds(25));
        PaintAssert.Paints(box, RipplePattern(new Point(29.0, 29.0), 73.0, 180));

        // At this point the splash radius has expanded to its limit: 5 past the
        // ink well's radius parameter. The splash center has moved to its final
        // location at the inkwell's center and the fade-out is about to start.
        // The fade-out begins at 225ms = 50ms + 25ms + 150ms.
        tester.Pump(TimeSpan.FromMilliseconds(150));
        PaintAssert.Paints(box, RipplePattern((Point)(inkWellCenter - tapDownOffset), 105.0, 180));

        // After another 150ms the fade-out is complete.
        tester.Pump(TimeSpan.FromMilliseconds(150));
        PaintAssert.Paints(box, RipplePattern((Point)(inkWellCenter - tapDownOffset), 105.0, 0));
    }

    // Flutter: "Material2 - Does the Ink widget render anything"
    [Fact]
    public void Material2DoesTheInkWidgetRenderAnything()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new MaterialWidget(
                    child: new Center(
                        child: new Ink(
                            color: Colors.Blue,
                            width: 200.0,
                            height: 200.0,
                            child: new InkWell(splashColor: Colors.Green, onTap: () => { }))))));

        Point center = tester.GetCenter(InkWellOf(tester));
        TestGesture gesture = StartGesture(tester, center);
        tester.Pump(); // start gesture
        tester.Pump(TimeSpan.FromMilliseconds(200)); // wait for splash to be well under way

        RenderObject box = MaterialOf(InkWellOf(tester));
        PaintAssert.Paints(
            box,
            PaintPattern.Paints
                .Rect(rect: Ltrb(300.0, 200.0, 500.0, 400.0), color: new Color(Colors.Blue.Value))
                .Circle(color: new Color(Colors.Green.Value)));

        tester.PumpWidget(
            new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new MaterialWidget(
                    child: new Center(
                        child: new Ink(
                            color: Colors.Red,
                            width: 200.0,
                            height: 200.0,
                            child: new InkWell(splashColor: Colors.Green, onTap: () => { }))))));

        Assert.Same(box, MaterialOf(InkWellOf(tester)));

        PaintAssert.Paints(
            box,
            PaintPattern.Paints
                .Rect(rect: Ltrb(300.0, 200.0, 500.0, 400.0), color: new Color(Colors.Red.Value))
                .Circle(color: new Color(Colors.Green.Value)));

        tester.PumpWidget(
            new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new MaterialWidget(
                    child: new Center(
                        child: new InkWell(
                            // this is at a different depth in the tree so it's now a new InkWell
                            splashColor: Colors.Green,
                            onTap: () => { })))));

        Assert.Same(box, MaterialOf(InkWellOf(tester)));

        PaintAssert.DoesNotPaint(box, PaintPattern.Paints.Rect());
        PaintAssert.DoesNotPaint(box, PaintPattern.Paints.Circle());

        gesture.Up();
    }

    // Flutter: "Material3 - Does the Ink widget render anything"
    // The ink decoration rects are checked; the two `matchesGoldenFile('m3_ink.renders.anything.*.png')`
    // steps are not (see GoldenSkip).
    [Fact]
    public void Material3DoesTheInkWidgetRenderAnything()
    {
        using FrameworkDartTester tester = CreateTester();
        var inkWellKey = new ValueKey<string>("InkWell");
        tester.PumpWidget(
            new MaterialApp(
                home: new MaterialWidget(
                    child: new Center(
                        child: new Ink(
                            color: Colors.Blue,
                            width: 200.0,
                            height: 200.0,
                            child: new InkWell(key: inkWellKey, splashColor: Colors.Green, onTap: () => { }))))));

        Point center = tester.GetCenter(InkWellOf(tester));
        TestGesture gesture = StartGesture(tester, center);
        tester.Pump(); // start gesture
        tester.Pump(TimeSpan.FromMilliseconds(200)); // wait for splash to be well under way

        RenderObject box = MaterialOf(InkWellOf(tester));
        PaintAssert.Paints(
            box,
            PaintPattern.Paints.Rect(rect: Ltrb(300.0, 200.0, 500.0, 400.0), color: new Color(Colors.Blue.Value)));

        // Material 3 uses the InkSparkle which uses a shader, so we can't capture
        // the effect with paint methods. Use a golden test instead.
        // matchesGoldenFile('m3_ink.renders.anything.0.png'): not portable (GoldenSkip).

        tester.PumpWidget(
            new MaterialApp(
                home: new MaterialWidget(
                    child: new Center(
                        child: new Ink(
                            color: Colors.Red,
                            width: 200.0,
                            height: 200.0,
                            child: new InkWell(key: inkWellKey, splashColor: Colors.Green, onTap: () => { }))))));

        Assert.Same(box, MaterialOf(InkWellOf(tester)));

        PaintAssert.Paints(
            box,
            PaintPattern.Paints.Rect(rect: Ltrb(300.0, 200.0, 500.0, 400.0), color: new Color(Colors.Red.Value)));

        // matchesGoldenFile('m3_ink.renders.anything.1.png'): not portable (GoldenSkip).

        tester.PumpWidget(
            new MaterialApp(
                home: new MaterialWidget(
                    child: new Center(
                        child: new InkWell(
                            // This is at a different depth in the tree so it's now a new InkWell.
                            key: inkWellKey,
                            splashColor: Colors.Green,
                            onTap: () => { })))));

        Assert.Same(box, MaterialOf(InkWellOf(tester)));

        PaintAssert.DoesNotPaint(box, PaintPattern.Paints.Rect());
        PaintAssert.DoesNotPaint(box, PaintPattern.Paints.Rect());

        gesture.Up();
    }

    // Flutter: "The InkWell widget renders an SelectAction or ActivateAction-induced ink ripple"
    [Fact]
    public void TheInkWellWidgetRendersAnActivateActionInducedInkRipple()
    {
        using FrameworkDartTester tester = CreateTester();
        var highlightColor = new Color(0xAAFF0000);
        var splashColor = new Color(0xB40000FF);
        BorderRadius borderRadius = BorderRadius.All(Radius.Circular(6.0));

        using var focusNode = new FocusNode(debugLabel: "Test Node");
        void BuildTest(Intent intent)
        {
            tester.PumpWidget(
                new Shortcuts(
                    shortcuts: new Dictionary<ShortcutActivator, Intent>
                    {
                        [new SingleActivator(LogicalKeyboardKey.Space)] = intent,
                    },
                    child: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new MaterialWidget(
                            child: new Center(
                                child: SizedBox.Square(
                                    dimension: 100.0,
                                    child: new InkWell(
                                        borderRadius: borderRadius,
                                        highlightColor: highlightColor,
                                        splashColor: splashColor,
                                        focusNode: focusNode,
                                        onTap: () => { },
                                        radius: 100.0,
                                        splashFactory: InkRipple.SplashFactory)))))));
        }

        BuildTest(new ActivateIntent());
        focusNode.RequestFocus();
        tester.PumpAndSettle();

        Point topLeft = tester.GetTopLeft(InkWellOf(tester));
        var inkWellCenter = (Point)(tester.GetCenter(InkWellOf(tester)) - topLeft);

        PaintPattern RipplePattern(double expectedRadius, int expectedAlpha)
        {
            return PaintPattern.Paints
                .Translate(x: 0.0, y: 0.0)
                .Translate(x: topLeft.X, y: topLeft.Y)
                .Something(call => CircleMatches(call, inkWellCenter, expectedRadius, expectedAlpha));
        }

        BuildTest(new ActivateIntent());
        tester.PumpAndSettle();
        SendKeyEvent(LogicalKeyboardKey.Space);
        tester.Pump();

        RenderObject box = MaterialOf(InkWellOf(tester));

        // ripplePattern always add a translation of topLeft.
        PaintAssert.Paints(box, RipplePattern(30.0, 0));

        // The ripple fades in for 75ms. During that time its alpha is eased from
        // 0 to the splashColor's alpha value.
        tester.Pump(TimeSpan.FromMilliseconds(50));
        PaintAssert.Paints(box, RipplePattern(56.0, 120));

        // At 75ms the ripple has faded in: it's alpha matches the splashColor's
        // alpha.
        tester.Pump(TimeSpan.FromMilliseconds(25));
        PaintAssert.Paints(box, RipplePattern(73.0, 180));

        // At this point the splash radius has expanded to its limit: 5 past the
        // ink well's radius parameter. The fade-out is about to start.
        // The fade-out begins at 225ms = 50ms + 25ms + 150ms.
        tester.Pump(TimeSpan.FromMilliseconds(150));
        PaintAssert.Paints(box, RipplePattern(105.0, 180));

        // After another 150ms the fade-out is complete.
        tester.Pump(TimeSpan.FromMilliseconds(150));
        PaintAssert.Paints(box, RipplePattern(105.0, 0));
    }

    // Flutter: "Cancel an InkRipple that was disposed when its animation ended" (first of two)
    [Fact]
    public void CancelAnInkRippleThatWasDisposedWhenItsAnimationEnded()
    {
        // Regression test for https://github.com/flutter/flutter/issues/14391
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100.0,
                            child: new InkWell(
                                onTap: () => { },
                                radius: 100.0,
                                splashFactory: InkRipple.SplashFactory))))));

        Point tapDownOffset = tester.GetTopLeft(InkWellOf(tester));
        TapAt(tester, tapDownOffset);
        tester.Pump(); // start splash
        tester.Pump(TimeSpan.FromMilliseconds(375)); // _kFadeOutDuration, in_ripple.dart

        TestGesture gesture = StartGesture(tester, tapDownOffset);
        tester.Pump(); // start gesture
        gesture.MoveTo(new Point(0, 0));
        gesture.Up(); // generates a tap cancel
        tester.PumpAndSettle();
        Assert.Null(tester.TakeException());
    }

    // Flutter: "Cancel an InkRipple that was disposed when its animation ended" (second of two)
    [Fact]
    public void CancelAnInkRippleThatWasDisposedWhenItsAnimationEndedBeforeItStarted()
    {
        using FrameworkDartTester tester = CreateTester();
        var highlightColor = new Color(0xAAFF0000);
        var splashColor = new Color(0xB40000FF);

        // Regression test for https://github.com/flutter/flutter/issues/14391
        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100.0,
                            child: new InkWell(
                                splashColor: splashColor,
                                highlightColor: highlightColor,
                                onTap: () => { },
                                radius: 100.0,
                                splashFactory: InkRipple.SplashFactory))))));

        Point tapDownOffset = tester.GetTopLeft(InkWellOf(tester));
        TapAt(tester, tapDownOffset);
        tester.Pump(); // start splash
        // No delay here so _fadeInController.value=1.0 (InkRipple.dart)

        // Generate a tap cancel; Will cancel the ink splash before it started
        TestGesture gesture = StartGesture(tester, tapDownOffset);
        tester.Pump(); // start gesture
        gesture.MoveTo(new Point(0, 0));
        gesture.Up(); // generates a tap cancel

        RenderObject box = MaterialOf(InkWellOf(tester));
        PaintAssert.Paints(
            box,
            PaintPattern.Paints.Everything(call =>
            {
                if (call.Method != "drawCircle")
                {
                    return true;
                }

                if (call.Color!.Alpha == 0)
                {
                    return true;
                }

                throw new InvalidOperationException(
                    $"Expected: paint.color.alpha == 0, found: {call.Color!.Alpha}");
            }));
    }

    // Flutter: "The InkWell widget on OverlayPortal does not throw"
    [Fact]
    public void TheInkWellWidgetOnOverlayPortalDoesNotThrow()
    {
        using FrameworkDartTester tester = CreateTester();
        var controller = new OverlayPortalController();
        controller.Show();

        OverlayEntry? overlayEntry = null;
        overlayEntry = new OverlayEntry(
            builder: context => new Center(
                child: SizedBox.Square(
                    dimension: 100,
                    // The material partially overlaps the overlayChild.
                    // This is to verify that the `overlayChild`'s ink
                    // features aren't clipped by it.
                    child: new MaterialWidget(
                        color: Colors.Black,
                        child: new OverlayPortal(
                            controller: controller,
                            overlayChildBuilder: overlayContext => new Positioned(
                                right: 0,
                                bottom: 0,
                                child: new InkWell(
                                    splashColor: Colors.Red,
                                    onTap: () => { },
                                    child: SizedBox.Square(dimension: 100))))))));

        tester.PumpWidget(
            new Center(
                child: new RepaintBoundary(
                    child: SizedBox.Square(
                        dimension: 200,
                        child: new Directionality(
                            textDirection: TextDirection.Ltr,
                            child: new Overlay(initialEntries: [overlayEntry]))))));

        TestGesture gesture = StartGesture(tester, tester.GetCenter(InkWellOf(tester)));
        try
        {
            tester.Pump(); // start gesture
            tester.Pump(TimeSpan.FromSeconds(2));

            Assert.Null(tester.TakeException());
        }
        finally
        {
            gesture.Up();
            overlayEntry.Remove();
            overlayEntry.Dispose();
        }
    }

    // Flutter: "Material2 - Custom rectCallback renders an ink splash from its center"
    [Fact]
    public void Material2CustomRectCallbackRendersAnInkSplashFromItsCenter()
    {
        using FrameworkDartTester tester = CreateTester();
        var splashColor = new Color(0xff00ff00);

        Widget BuildWidget(InteractiveInkFeatureFactory? splashFactory = null)
        {
            return new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new MaterialWidget(
                    child: new Center(
                        child: new SizedBox(
                            width: 100.0,
                            height: 200.0,
                            child: new InkResponse(
                                splashColor: splashColor,
                                containedInkWell: true,
                                highlightShape: BoxShape.Rectangle,
                                splashFactory: splashFactory,
                                onTap: () => { })))));
        }

        tester.PumpWidget(BuildWidget());

        Point center = tester.GetCenter(SizedBoxAbove<InkResponse>(tester));
        TestGesture gesture = StartGesture(tester, center);
        tester.Pump(); // start gesture
        tester.PumpAndSettle(); // Finish rendering ink splash.

        RenderObject box = MaterialOf(tester.ElementOfType<InkResponse>());
        PaintAssert.Paints(box, PaintPattern.Paints.Circle(x: 50.0, y: 100.0, color: splashColor));

        gesture.Up();

        tester.PumpWidget(BuildWidget(splashFactory: new TestInkRippleFactory()));
        tester.PumpAndSettle(); // Finish rendering ink splash.

        gesture = StartGesture(tester, center);
        tester.Pump(); // start gesture
        tester.PumpAndSettle(); // Finish rendering ink splash.

        box = MaterialOf(tester.ElementOfType<InkResponse>());
        PaintAssert.Paints(box, PaintPattern.Paints.Circle(x: 50.0, y: 50.0, color: splashColor));
    }

    // Flutter: "Material3 - Custom rectCallback renders an ink splash from its center"
    [Fact(Skip = GoldenSkip)]
    public void Material3CustomRectCallbackRendersAnInkSplashFromItsCenter()
    {
    }

    // Flutter: "Ink with isVisible=false does not paint"
    [Fact]
    public void InkWithIsVisibleFalseDoesNotPaint()
    {
        using FrameworkDartTester tester = CreateTester();
        var testColor = new Color(0xffff1234);
        Widget InkWidget(bool isVisible)
        {
            return new MaterialWidget(
                child: Visibility.Maintain(
                    visible: isVisible,
                    child: new Ink(decoration: new BoxDecoration(Color: testColor))));
        }

        tester.PumpWidget(InkWidget(isVisible: true));
        RenderObject box = tester.ElementOfType<MaterialWidget>().FindRenderObject()!;
        PaintAssert.Paints(box, PaintPattern.Paints.Rect(color: testColor));

        tester.PumpWidget(InkWidget(isVisible: false));
        box = tester.ElementOfType<MaterialWidget>().FindRenderObject()!;
        PaintAssert.DoesNotPaint(box, PaintPattern.Paints.Rect(color: testColor));
    }

    // The `offsetsAreClose`/`radiiAreClose`/alpha check of the ripple tests' `something` predicate.
    private static bool CircleMatches(CanvasCall call, Point expectedCenter, double expectedRadius, int expectedAlpha)
    {
        if (call.Method != "drawCircle")
        {
            return false;
        }

        Point center = call.Center!.Value;
        double radius = call.Radius!.Value;
        int alpha = call.Color!.Alpha;
        double distance = Math.Sqrt(
            Math.Pow(center.X - expectedCenter.X, 2) + Math.Pow(center.Y - expectedCenter.Y, 2));
        if (distance < 1.0 && Math.Abs(radius - expectedRadius) < 1.0 && alpha == expectedAlpha)
        {
            return true;
        }

        throw new InvalidOperationException(
            $"Expected: center == {expectedCenter}, radius == {expectedRadius}, alpha == {expectedAlpha}\n"
            + $"Found: center == {center} radius == {radius} alpha == {alpha}");
    }

    // `find.byType(SizedBox)` in the rectCallback tests: the only SizedBox is the InkResponse's parent.
    private static Element SizedBoxAbove<TWidget>(FrameworkDartTester tester) where TWidget : Widget
    {
        Element? result = null;
        tester.ElementOfType<TWidget>().VisitAncestorElements(element =>
        {
            if (element.Widget is SizedBox)
            {
                result = element;
                return false;
            }

            return true;
        });
        return result!;
    }

    private static FrameworkDartTester CreateTester() => new(fakeGestureTimers: true);

    private static Rect Ltrb(double left, double top, double right, double bottom) =>
        new(new Point(left, top), new Point(right, bottom));

    private static Element InkWellOf(FrameworkDartTester tester) => tester.ElementOfType<InkWell>();

    // `Material.of(element) as RenderBox`.
    private static RenderObject MaterialOf(Element element) => (RenderInkFeatures)MaterialWidget.Of(element);

    private static TestGesture StartGesture(FrameworkDartTester tester, Point location) =>
        tester.StartGesture(location, PointerDeviceKind.Touch);

    // `tester.tapAt(location)`.
    private static void TapAt(FrameworkDartTester tester, Point location) => StartGesture(tester, location).Up();

    // `tester.sendKeyEvent(key)`.
    private static void SendKeyEvent(LogicalKeyboardKey key)
    {
        FocusManager.Instance.HandleKeyEvent(KeySim.Down(key));
        Scheduler.FlushMicrotasks();
        FocusManager.Instance.HandleKeyEvent(KeySim.Up(key));
        Scheduler.FlushMicrotasks();
    }

    // The test file's `_InkRippleFactory`: an InkRipple whose rect callback is a fixed 100x100 square.
    private sealed class TestInkRippleFactory : InteractiveInkFeatureFactory
    {
        public override InteractiveInkFeature Create(
            MaterialInkController controller,
            RenderBox referenceBox,
            Point position,
            Color color,
            TextDirection textDirection,
            bool containedInkWell = false,
            RectCallback? rectCallback = null,
            BorderRadius? borderRadius = null,
            ShapeBorder? customBorder = null,
            double? radius = null,
            Action? onRemoved = null)
        {
            return new InkRipple(
                controller: controller,
                referenceBox: referenceBox,
                position: position,
                color: color,
                containedInkWell: containedInkWell,
                rectCallback: () => new Rect(0, 0, 100, 100),
                borderRadius: borderRadius,
                customBorder: customBorder,
                radius: radius,
                onRemoved: onRemoved,
                textDirection: textDirection);
        }
    }
}
