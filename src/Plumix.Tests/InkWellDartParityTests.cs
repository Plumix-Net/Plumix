// Dart parity source: material_ui/lib/src/ink_well.dart
// Mirrors material-ui-src/test/ink_well_test.dart

using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MaterialWidget = Plumix.Material.Material;
using Path = Plumix.UI.Path;
using TextDirection = Plumix.UI.TextDirection;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class InkWellDartParityTests : IDisposable
{
    private static readonly TimeSpan LongPressDuration =
        TimeSpan.FromMilliseconds(500) + TimeSpan.FromMilliseconds(100); // kLongPressTimeout + kPressTimeout

    public InkWellDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
    }

    // Flutter: "InkWell gestures control test"
    [Fact]
    public void InkWellGesturesControlTest()
    {
        using FrameworkDartTester tester = CreateTester();
        var log = new List<string>();

        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Center(
                        child: new InkWell(
                            onTap: () => log.Add("tap"),
                            onDoubleTap: () => log.Add("double-tap"),
                            onLongPress: () => log.Add("long-press"),
                            onLongPressUp: () => log.Add("long-press-up"),
                            onTapDown: _ => log.Add("tap-down"),
                            onTapUp: _ => log.Add("tap-up"),
                            onTapCancel: () => log.Add("tap-cancel"))))));

        Tap(tester, InkWellOf(tester));

        Assert.Empty(log);

        tester.Pump(TimeSpan.FromSeconds(1));

        Assert.Equal(["tap-down", "tap-up", "tap"], log);
        log.Clear();

        Tap(tester, InkWellOf(tester));
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Tap(tester, InkWellOf(tester));

        Assert.Equal(["double-tap"], log);
        log.Clear();

        LongPress(tester, InkWellOf(tester));

        Assert.Equal(["tap-down", "tap-cancel", "long-press", "long-press-up"], log);

        log.Clear();
        TestGesture gesture = StartGesture(tester, tester.GetRect(InkWellOf(tester)).Center);
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(["tap-down"], log);
        gesture.Up();
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(["tap-down", "tap-up", "tap"], log);

        log.Clear();
        gesture = StartGesture(tester, tester.GetRect(InkWellOf(tester)).Center);
        tester.Pump(TimeSpan.FromMilliseconds(100));
        gesture.MoveBy(new Vector(0.0, 200.0));
        gesture.Cancel();
        Assert.Equal(["tap-down", "tap-cancel"], log);
    }

    // Flutter: "InkWell only onTapDown enables gestures"
    [Fact]
    public void InkWellOnlyOnTapDownEnablesGestures()
    {
        // Regression test for https://github.com/flutter/flutter/issues/96030
        using FrameworkDartTester tester = CreateTester();
        bool downTapped = false;
        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Center(child: new InkWell(onTapDown: _ => downTapped = true)))));

        Tap(tester, InkWellOf(tester));
        Assert.True(downTapped);
    }

    // Flutter: "InkWell invokes activation actions when expected"
    [Fact]
    public void InkWellInvokesActivationActionsWhenExpected()
    {
        using FrameworkDartTester tester = CreateTester();
        var log = new List<string>();

        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new Shortcuts(
                    shortcuts: new Dictionary<ShortcutActivator, Intent>
                    {
                        [new SingleActivator(LogicalKeyboardKey.Space)] = new ActivateIntent(),
                        [new SingleActivator(LogicalKeyboardKey.Enter)] = new ButtonActivateIntent(),
                    },
                    child: new MaterialWidget(
                        child: new Center(child: new InkWell(autofocus: true, onTap: () => log.Add("tap")))))));

        tester.SendKeyEvent(LogicalKeyboardKey.Space);
        tester.Pump();
        Assert.Equal(["tap"], log);
        log.Clear();
        tester.SendKeyEvent(LogicalKeyboardKey.Enter);
        tester.Pump();
        Assert.Equal(["tap"], log);
    }

    // Flutter: "InkWell onLongPressUp callback is triggered"
    [Fact]
    public void InkWellOnLongPressUpCallbackIsTriggered()
    {
        using FrameworkDartTester tester = CreateTester();
        bool wasCalled = false;

        tester.PumpWidget(
            new MaterialApp(
                home: new MaterialWidget(
                    child: new InkWell(
                        onLongPress: () => { },
                        onLongPressUp: () => wasCalled = true,
                        child: new SizedBox(width: 100, height: 100)))));

        TestGesture gesture = StartGesture(tester, tester.GetCenter(InkWellOf(tester)));
        tester.Pump(TimeSpan.FromSeconds(1));
        gesture.Up();
        tester.PumpAndSettle();

        Assert.True(wasCalled);
    }

    // Flutter: "long-press and tap on disabled should not throw"
    [Fact]
    public void LongPressAndTapOnDisabledShouldNotThrow()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(child: new InkWell()))));
        Tap(tester, InkWellOf(tester));
        tester.Pump(TimeSpan.FromSeconds(1));
        LongPress(tester, InkWellOf(tester));
        tester.Pump(TimeSpan.FromSeconds(1));
    }

    // Flutter: "ink well changes color on hover"
    [Fact]
    public void InkWellChangesColorOnHover()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new InkWell(
                                hoverColor: new Color(0xff00ff00),
                                splashColor: new Color(0xffff0000),
                                focusColor: new Color(0xff0000ff),
                                highlightColor: new Color(0xf00fffff),
                                onTap: () => { },
                                onLongPress: () => { },
                                onLongPressUp: () => { },
                                onHover: _ => { }))))));
        TestGesture gesture = tester.CreateGesture(PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(tester.ElementOfType<SizedBox>()));
        tester.PumpAndSettle();
        RenderObject inkFeatures = GetInkFeatures(tester);
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.Rect(rect: Ltrb(350.0, 250.0, 450.0, 350.0), color: new Color(0xff00ff00)));
    }

    // Flutter: "ink well changes color on hover with overlayColor"
    [Fact]
    public void InkWellChangesColorOnHoverWithOverlayColor()
    {
        // Same test as 'ink well changes color on hover' except that the
        // hover color is specified with the overlayColor parameter.
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new InkWell(
                                overlayColor: HoverFocusPressedOverlay(new Color(0xf00fffff)),
                                onTap: () => { },
                                onLongPress: () => { },
                                onLongPressUp: () => { },
                                onHover: _ => { }))))));
        TestGesture gesture = tester.CreateGesture(PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(tester.ElementOfType<SizedBox>()));
        tester.PumpAndSettle();
        RenderObject inkFeatures = GetInkFeatures(tester);
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.Rect(rect: Ltrb(350.0, 250.0, 450.0, 350.0), color: new Color(0xff00ff00)));
    }

    // Flutter: "ink response changes color on focus"
    [Fact]
    public void InkResponseChangesColorOnFocus()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var focusNode = new FocusNode(debugLabel: "Ink Focus");
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new InkWell(
                                focusNode: focusNode,
                                hoverColor: new Color(0xff00ff00),
                                splashColor: new Color(0xffff0000),
                                focusColor: new Color(0xff0000ff),
                                highlightColor: new Color(0xf00fffff),
                                onTap: () => { },
                                onLongPress: () => { },
                                onLongPressUp: () => { },
                                onHover: _ => { }))))));
        tester.PumpAndSettle();
        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "drawRect"));
        focusNode.RequestFocus();
        tester.PumpAndSettle();
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.Rect(rect: Ltrb(350.0, 250.0, 450.0, 350.0), color: new Color(0xff0000ff)));
        focusNode.Dispose();
    }

    // Flutter: "ink response changes color on focus with overlayColor"
    [Fact]
    public void InkResponseChangesColorOnFocusWithOverlayColor()
    {
        // Same test as 'ink well changes color on focus' except that the
        // hover color is specified with the overlayColor parameter.
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var focusNode = new FocusNode(debugLabel: "Ink Focus");
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new InkWell(
                                focusNode: focusNode,
                                overlayColor: HoverFocusPressedOverlay(new Color(0xf00fffff)),
                                highlightColor: new Color(0xf00fffff),
                                onTap: () => { },
                                onLongPress: () => { },
                                onLongPressUp: () => { },
                                onHover: _ => { }))))));
        tester.PumpAndSettle();
        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "drawRect"));
        focusNode.RequestFocus();
        tester.PumpAndSettle();
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.Rect(rect: Ltrb(350.0, 250.0, 450.0, 350.0), color: new Color(0xff0000ff)));
        focusNode.Dispose();
    }

    // Flutter: "ink well changes color on pressed with overlayColor"
    [Fact]
    public void InkWellChangesColorOnPressedWithOverlayColor()
    {
        using FrameworkDartTester tester = CreateTester();
        var pressedColor = new Color(0xffdd00ff);

        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Container(
                        alignment: Alignment.TopLeft,
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new InkWell(
                                splashFactory: NoSplash.SplashFactory,
                                overlayColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                                    states.Contains(WidgetState.Pressed)
                                        ? pressedColor
                                        : new Color(0xffbadbad)), // Shouldn't happen.
                                onTap: () => { }))))));
        tester.PumpAndSettle();
        TestGesture gesture = StartGesture(tester, tester.GetRect(InkWellOf(tester)).Center);
        RenderObject inkFeatures = GetInkFeatures(tester);
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.Rect(rect: Ltrb(0, 0, 100, 100), color: pressedColor.WithAlpha(0)));
        tester.PumpAndSettle(); // Let the press highlight animation finish.
        PaintAssert.Paints(inkFeatures, PaintPattern.Paints.Rect(rect: Ltrb(0, 0, 100, 100), color: pressedColor));
        gesture.Up();
    }

    // Flutter: "Ink well overlayColor resolution respects WidgetState.selected" / "when focused"
    [Fact]
    public void InkWellOverlayColorResolutionRespectsSelectedWhenFocused()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        using var focusNode = new FocusNode(debugLabel: "Ink Focus");
        using var statesController = new WidgetStatesController([WidgetState.Selected]);

        tester.PumpWidget(SelectedBoilerplate(statesController, focusNode));
        tester.PumpAndSettle();

        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "drawRect"));
        focusNode.RequestFocus();
        tester.PumpAndSettle();

        PaintAssert.Paints(inkFeatures, PaintPattern.Paints.Rect(rect: SelectedInkRect, color: SelectedFocusedColor));
    }

    // Flutter: "Ink well overlayColor resolution respects WidgetState.selected" / "when hovered"
    [Fact]
    public void InkWellOverlayColorResolutionRespectsSelectedWhenHovered()
    {
        using FrameworkDartTester tester = CreateTester();
        using var statesController = new WidgetStatesController([WidgetState.Selected]);
        tester.PumpWidget(SelectedBoilerplate(statesController));
        tester.PumpAndSettle();

        TestGesture gesture = tester.CreateGesture(PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(tester.ElementOfType<SizedBox>()));
        tester.PumpAndSettle();

        RenderObject inkFeatures = GetInkFeatures(tester);
        PaintAssert.Paints(inkFeatures, PaintPattern.Paints.Rect(rect: SelectedInkRect, color: SelectedHoveredColor));
    }

    // Flutter: "Ink well overlayColor resolution respects WidgetState.selected" / "when pressed"
    [Fact]
    public void InkWellOverlayColorResolutionRespectsSelectedWhenPressed()
    {
        using FrameworkDartTester tester = CreateTester();
        using var statesController = new WidgetStatesController([WidgetState.Selected]);
        tester.PumpWidget(SelectedBoilerplate(statesController));
        tester.PumpAndSettle();

        TestGesture gesture = StartGesture(tester, tester.GetRect(InkWellOf(tester)).Center);
        RenderObject inkFeatures = GetInkFeatures(tester);
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.Rect(rect: SelectedInkRect, color: SelectedPressedColor.WithAlpha(0)));
        tester.PumpAndSettle(); // Let the press highlight animation finish.
        PaintAssert.Paints(inkFeatures, PaintPattern.Paints.Rect(rect: SelectedInkRect, color: SelectedPressedColor));
        gesture.Up();
    }

    // Flutter: "ink response splashColor matches splashColor parameter"
    [Fact]
    public void InkResponseSplashColorMatchesSplashColorParameter()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTouch;
        var focusNode = new FocusNode(debugLabel: "Ink Focus");
        var splashColor = new Color(0xffff0000);
        tester.PumpWidget(
            new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new MaterialWidget(
                    child: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new Center(
                            child: new Focus(
                                focusNode: focusNode,
                                child: SizedBox.Square(
                                    dimension: 100,
                                    child: new InkWell(
                                        hoverColor: new Color(0xff00ff00),
                                        splashColor: splashColor,
                                        focusColor: new Color(0xff0000ff),
                                        highlightColor: new Color(0xf00fffff),
                                        onTap: () => { },
                                        onLongPress: () => { },
                                        onLongPressUp: () => { },
                                        onHover: _ => { }))))))));
        tester.PumpAndSettle();
        TestGesture gesture = StartGesture(tester, tester.GetRect(InkWellOf(tester)).Center);
        tester.Pump(TimeSpan.FromMilliseconds(200)); // unconfirmed splash is well underway
        RenderObject inkFeatures = GetInkFeatures(tester);
        PaintAssert.Paints(inkFeatures, PaintPattern.Paints.Circle(x: 50, y: 50, color: splashColor));
        gesture.Up();
        focusNode.Dispose();
    }

    // Flutter: "ink response splashColor matches resolved overlayColor for WidgetState.pressed"
    [Fact]
    public void InkResponseSplashColorMatchesResolvedOverlayColorForPressed()
    {
        // Same test as 'ink response splashColor matches splashColor
        // parameter' except that the splash color is specified with the
        // overlayColor parameter.
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTouch;
        var focusNode = new FocusNode(debugLabel: "Ink Focus");
        var splashColor = new Color(0xffff0000);
        tester.PumpWidget(
            new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new MaterialWidget(
                    child: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new Center(
                            child: new Focus(
                                focusNode: focusNode,
                                child: SizedBox.Square(
                                    dimension: 100,
                                    child: new InkWell(
                                        overlayColor: HoverFocusPressedOverlay(splashColor),
                                        onTap: () => { },
                                        onLongPress: () => { },
                                        onLongPressUp: () => { },
                                        onHover: _ => { }))))))));
        tester.PumpAndSettle();
        TestGesture gesture = StartGesture(tester, tester.GetRect(InkWellOf(tester)).Center);
        tester.Pump(TimeSpan.FromMilliseconds(200)); // unconfirmed splash is well underway
        RenderObject inkFeatures = GetInkFeatures(tester);
        PaintAssert.Paints(inkFeatures, PaintPattern.Paints.Circle(x: 50, y: 50, color: splashColor));
        gesture.Up();
        focusNode.Dispose();
    }

    // Flutter: "ink response uses radius for focus highlight"
    [Fact]
    public void InkResponseUsesRadiusForFocusHighlight()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var focusNode = new FocusNode(debugLabel: "Ink Focus");
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new InkResponse(
                                focusNode: focusNode,
                                radius: 20,
                                focusColor: new Color(0xff0000ff),
                                onTap: () => { }))))));
        tester.PumpAndSettle();
        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "drawCircle"));
        focusNode.RequestFocus();
        tester.PumpAndSettle();
        PaintAssert.Paints(inkFeatures, PaintPattern.Paints.Circle(radius: 20, color: new Color(0xff0000ff)));
        focusNode.Dispose();
    }

    // Flutter: "InkWell uses borderRadius for focus highlight"
    [Fact]
    public void InkWellUsesBorderRadiusForFocusHighlight()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var focusNode = new FocusNode(debugLabel: "Ink Focus");
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new InkWell(
                                focusNode: focusNode,
                                borderRadius: BorderRadius.All(Radius.Circular(10)),
                                focusColor: new Color(0xff0000ff),
                                onTap: () => { }))))));
        tester.PumpAndSettle();
        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "drawRRect"));

        focusNode.RequestFocus();
        tester.PumpAndSettle();
        Assert.Equal(1, CountCalls(inkFeatures, "drawRRect"));

        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.RRect(
                rrect: RRect.FromLTRBR(350.0, 250.0, 450.0, 350.0, Radius.Circular(10)),
                color: new Color(0xff0000ff)));
        focusNode.Dispose();
    }

    // Flutter: "InkWell uses borderRadius for hover highlight"
    [Fact]
    public void InkWellUsesBorderRadiusForHoverHighlight()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new MouseRegion(
                                child: new InkWell(
                                    borderRadius: BorderRadius.All(Radius.Circular(10)),
                                    hoverColor: new Color(0xff00ff00),
                                    onTap: () => { })))))));
        tester.PumpAndSettle();
        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "drawRRect"));

        // Hover the ink well.
        TestGesture gesture = tester.CreateGesture(PointerDeviceKind.Mouse);
        gesture.AddPointer(location: tester.GetRect(InkWellOf(tester)).Center);
        tester.PumpAndSettle();
        Assert.Equal(1, CountCalls(inkFeatures, "drawRRect"));

        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.RRect(
                rrect: RRect.FromLTRBR(350.0, 250.0, 450.0, 350.0, Radius.Circular(10)),
                color: new Color(0xff00ff00)));
    }

    // Flutter: "InkWell customBorder clips for focus highlight"
    [Fact]
    public void InkWellCustomBorderClipsForFocusHighlight()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var focusNode = new FocusNode(debugLabel: "Ink Focus");
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Align(
                        alignment: Alignment.TopLeft,
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new MouseRegion(
                                child: new InkWell(
                                    focusNode: focusNode,
                                    borderRadius: BorderRadius.All(Radius.Circular(10)),
                                    customBorder: new CircleBorder(),
                                    hoverColor: new Color(0xff00ff00),
                                    onTap: () => { })))))));
        tester.PumpAndSettle();
        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "clipPath"));
        Assert.Equal(0, CountCalls(inkFeatures, "drawRRect"));

        focusNode.RequestFocus();
        tester.PumpAndSettle();
        Assert.Equal(1, CountCalls(inkFeatures, "clipPath"));
        Assert.Equal(1, CountCalls(inkFeatures, "drawRRect"));

        // Create a rounded rectangle path with a radius that makes it similar to the custom border circle.
        Rect expectedClipRect = Ltrb(0, 0, 100, 100);
        Path expectedClipPath = RoundedPath(expectedClipRect, 50.0);
        // The ink well custom border path should match the rounded rectangle path.
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.ClipPath(expectedClipPath, expectedClipRect.Inflate(20.0), sampleSize: 100));
        focusNode.Dispose();
    }

    // Flutter: "InkWell customBorder clips for hover highlight"
    [Fact]
    public void InkWellCustomBorderClipsForHoverHighlight()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Align(
                        alignment: Alignment.TopLeft,
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new MouseRegion(
                                child: new InkWell(
                                    borderRadius: BorderRadius.All(Radius.Circular(10)),
                                    customBorder: new CircleBorder(),
                                    hoverColor: new Color(0xff00ff00),
                                    onTap: () => { })))))));
        tester.PumpAndSettle();
        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "clipPath"));
        Assert.Equal(0, CountCalls(inkFeatures, "drawRRect"));

        // Hover the ink well.
        TestGesture gesture = tester.CreateGesture(PointerDeviceKind.Mouse);
        gesture.AddPointer(location: tester.GetRect(InkWellOf(tester)).Center);
        tester.PumpAndSettle();
        Assert.Equal(1, CountCalls(inkFeatures, "clipPath"));
        Assert.Equal(1, CountCalls(inkFeatures, "drawRRect"));

        // Create a rounded rectangle path with a radius that makes it similar to the custom border circle.
        Rect expectedClipRect = Ltrb(0, 0, 100, 100);
        Path expectedClipPath = RoundedPath(expectedClipRect, 50.0);
        // The ink well custom border path should match the rounded rectangle path.
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.ClipPath(expectedClipPath, expectedClipRect.Inflate(20.0), sampleSize: 100));
    }

    // Flutter: "InkResponse radius can be updated"
    [Fact]
    public void InkResponseRadiusCanBeUpdated()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var focusNode = new FocusNode(debugLabel: "Ink Focus");
        Widget Boilerplate(double radius)
        {
            return new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new InkResponse(
                                focusNode: focusNode,
                                radius: radius,
                                focusColor: new Color(0xff0000ff),
                                onTap: () => { })))));
        }

        tester.PumpWidget(Boilerplate(10));
        tester.PumpAndSettle();
        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "drawCircle"));

        focusNode.RequestFocus();
        tester.PumpAndSettle();
        Assert.Equal(1, CountCalls(inkFeatures, "drawCircle"));
        PaintAssert.Paints(inkFeatures, PaintPattern.Paints.Circle(radius: 10, color: new Color(0xff0000ff)));

        tester.PumpWidget(Boilerplate(20));
        tester.PumpAndSettle();
        Assert.Equal(1, CountCalls(inkFeatures, "drawCircle"));
        PaintAssert.Paints(inkFeatures, PaintPattern.Paints.Circle(radius: 20, color: new Color(0xff0000ff)));
        focusNode.Dispose();
    }

    // Flutter: "InkResponse highlightShape can be updated"
    [Fact]
    public void InkResponseHighlightShapeCanBeUpdated()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var focusNode = new FocusNode(debugLabel: "Ink Focus");
        Widget Boilerplate(BoxShape shape)
        {
            return new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new InkResponse(
                                focusNode: focusNode,
                                highlightShape: shape,
                                borderRadius: BorderRadius.All(Radius.Circular(10)),
                                focusColor: new Color(0xff0000ff),
                                onTap: () => { })))));
        }

        tester.PumpWidget(Boilerplate(BoxShape.Circle));
        tester.PumpAndSettle();
        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "drawCircle"));
        Assert.Equal(0, CountCalls(inkFeatures, "drawRRect"));

        focusNode.RequestFocus();
        tester.PumpAndSettle();
        Assert.Equal(1, CountCalls(inkFeatures, "drawCircle"));
        Assert.Equal(0, CountCalls(inkFeatures, "drawRRect"));

        tester.PumpWidget(Boilerplate(BoxShape.Rectangle));
        tester.PumpAndSettle();
        Assert.Equal(0, CountCalls(inkFeatures, "drawCircle"));
        Assert.Equal(1, CountCalls(inkFeatures, "drawRRect"));
        focusNode.Dispose();
    }

    // Flutter: "InkWell borderRadius can be updated"
    [Fact]
    public void InkWellBorderRadiusCanBeUpdated()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var focusNode = new FocusNode(debugLabel: "Ink Focus");
        Widget Boilerplate(BorderRadius borderRadius)
        {
            return new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new InkWell(
                                focusNode: focusNode,
                                borderRadius: borderRadius,
                                focusColor: new Color(0xff0000ff),
                                onTap: () => { })))));
        }

        tester.PumpWidget(Boilerplate(BorderRadius.All(Radius.Circular(10))));
        tester.PumpAndSettle();
        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "drawRRect"));

        focusNode.RequestFocus();
        tester.PumpAndSettle();
        Assert.Equal(1, CountCalls(inkFeatures, "drawRRect"));
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.RRect(
                rrect: RRect.FromLTRBR(350.0, 250.0, 450.0, 350.0, Radius.Circular(10)),
                color: new Color(0xff0000ff)));

        tester.PumpWidget(Boilerplate(BorderRadius.All(Radius.Circular(30))));
        tester.PumpAndSettle();
        Assert.Equal(1, CountCalls(inkFeatures, "drawRRect"));
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.RRect(
                rrect: RRect.FromLTRBR(350.0, 250.0, 450.0, 350.0, Radius.Circular(30)),
                color: new Color(0xff0000ff)));
        focusNode.Dispose();
    }

    // Flutter: "InkWell customBorder can be updated"
    [Fact]
    public void InkWellCustomBorderCanBeUpdated()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var focusNode = new FocusNode(debugLabel: "Ink Focus");
        Widget Boilerplate(BorderRadius borderRadius)
        {
            return new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Align(
                        alignment: Alignment.TopLeft,
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new MouseRegion(
                                child: new InkWell(
                                    focusNode: focusNode,
                                    customBorder: new RoundedRectangleBorder(borderRadius: borderRadius),
                                    hoverColor: new Color(0xff00ff00),
                                    onTap: () => { }))))));
        }

        tester.PumpWidget(Boilerplate(BorderRadius.All(Radius.Circular(20))));
        tester.PumpAndSettle();
        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "clipPath"));

        focusNode.RequestFocus();
        tester.PumpAndSettle();
        Assert.Equal(1, CountCalls(inkFeatures, "clipPath"));

        Rect expectedClipRect = Ltrb(0, 0, 100, 100);
        Path expectedClipPath = RoundedPath(expectedClipRect, 20);
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.ClipPath(expectedClipPath, expectedClipRect.Inflate(20.0), sampleSize: 100));

        tester.PumpWidget(Boilerplate(BorderRadius.All(Radius.Circular(40))));
        tester.PumpAndSettle();
        expectedClipPath = RoundedPath(expectedClipRect, 40);
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.ClipPath(expectedClipPath, expectedClipRect.Inflate(20.0), sampleSize: 100));
        focusNode.Dispose();
    }

    // Flutter: "InkWell splash customBorder can be updated"
    [Fact]
    public void InkWellSplashCustomBorderCanBeUpdated()
    {
        // Regression test for https://github.com/flutter/flutter/issues/121626.
        using FrameworkDartTester tester = CreateTester();
        var focusNode = new FocusNode(debugLabel: "Ink Focus");
        Widget Boilerplate(BorderRadius borderRadius)
        {
            return new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new MaterialWidget(
                    child: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new Align(
                            alignment: Alignment.TopLeft,
                            child: SizedBox.Square(
                                dimension: 100,
                                child: new MouseRegion(
                                    child: new InkWell(
                                        focusNode: focusNode,
                                        customBorder: new RoundedRectangleBorder(borderRadius: borderRadius),
                                        onTap: () => { })))))));
        }

        tester.PumpWidget(Boilerplate(BorderRadius.All(Radius.Circular(20))));
        tester.PumpAndSettle();

        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "clipPath"));

        TestGesture gesture = StartGesture(tester, tester.GetRect(InkWellOf(tester)).Center);
        tester.Pump(TimeSpan.FromMilliseconds(200)); // Unconfirmed splash is well underway.
        Assert.Equal(2, CountCalls(inkFeatures, "clipPath")); // Splash and highlight.

        Rect expectedClipRect = Ltrb(0, 0, 100, 100);
        Path expectedClipPath = RoundedPath(expectedClipRect, 20);

        // Check that the splash and the highlight are correctly clipped.
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints
                .ClipPath(expectedClipPath, expectedClipRect.Inflate(20.0), sampleSize: 100)
                .ClipPath(expectedClipPath, expectedClipRect.Inflate(20.0), sampleSize: 100));

        tester.PumpWidget(Boilerplate(BorderRadius.All(Radius.Circular(40))));
        tester.PumpAndSettle();
        expectedClipPath = RoundedPath(expectedClipRect, 40);

        // Check that the splash and the highlight are correctly clipped.
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints
                .ClipPath(expectedClipPath, expectedClipRect.Inflate(20.0), sampleSize: 100)
                .ClipPath(expectedClipPath, expectedClipRect.Inflate(20.0), sampleSize: 100));

        gesture.Up();
        focusNode.Dispose();
    }

    // Flutter: "ink response doesn't change color on focus when on touch device"
    [Fact]
    public void InkResponseDoesNotChangeColorOnFocusWhenOnTouchDevice()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTouch;
        var focusNode = new FocusNode(debugLabel: "Ink Focus");
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new InkWell(
                                focusNode: focusNode,
                                hoverColor: new Color(0xff00ff00),
                                splashColor: new Color(0xffff0000),
                                focusColor: new Color(0xff0000ff),
                                highlightColor: new Color(0xf00fffff),
                                onTap: () => { },
                                onLongPress: () => { },
                                onLongPressUp: () => { },
                                onHover: _ => { }))))));
        tester.PumpAndSettle();
        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "drawRect"));
        focusNode.RequestFocus();
        tester.PumpAndSettle();
        Assert.Equal(0, CountCalls(inkFeatures, "drawRect"));
        focusNode.Dispose();
    }

    // Flutter: "InkWell.mouseCursor changes cursor on hover"
    [Fact]
    public void InkWellMouseCursorChangesCursorOnHover()
    {
        using FrameworkDartTester tester = CreateTester();
        TestGesture gesture = tester.CreateGesture(PointerDeviceKind.Mouse);
        gesture.AddPointer(location: new Point(1, 1));

        // Test argument works
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new MouseRegion(
                        cursor: SystemMouseCursors.Forbidden,
                        child: new InkWell(mouseCursor: SystemMouseCursors.Cell, onTap: () => { })))));

        Assert.Equal(SystemMouseCursors.Cell, ActiveCursor(gesture));

        // Test default of InkWell()
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new MouseRegion(
                        cursor: SystemMouseCursors.Forbidden,
                        child: new InkWell(onTap: () => { })))));

        // kIsWeb ? SystemMouseCursors.click : SystemMouseCursors.basic
        Assert.Equal(SystemMouseCursors.Basic, ActiveCursor(gesture));

        // Test disabled
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new MouseRegion(cursor: SystemMouseCursors.Forbidden, child: new InkWell()))));

        Assert.Equal(SystemMouseCursors.Basic, ActiveCursor(gesture));

        // Test default of InkResponse()
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new MouseRegion(
                        cursor: SystemMouseCursors.Forbidden,
                        child: new InkResponse(onTap: () => { })))));

        // kIsWeb ? SystemMouseCursors.click : SystemMouseCursors.basic
        Assert.Equal(SystemMouseCursors.Basic, ActiveCursor(gesture));

        // Test disabled
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new MouseRegion(cursor: SystemMouseCursors.Forbidden, child: new InkResponse()))));

        Assert.Equal(SystemMouseCursors.Basic, ActiveCursor(gesture));
    }

    // Flutter: "InkResponse containing selectable text changes mouse cursor when hovered"
    [Fact]
    public void InkResponseContainingSelectableTextChangesMouseCursorWhenHovered()
    {
        // Regression test for https://github.com/flutter/flutter/issues/104595.
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialApp(
                home: new SelectionArea(
                    child: new MaterialWidget(
                        child: new InkResponse(onTap: () => { }, child: new Text("button"))))));

        TestGesture gesture = tester.CreateGesture(PointerDeviceKind.Mouse);
        gesture.AddPointer(location: tester.GetCenter(tester.ElementOfType<Text>()));

        tester.Pump();

        // kIsWeb ? SystemMouseCursors.click : SystemMouseCursors.basic
        Assert.Equal(SystemMouseCursors.Basic, ActiveCursor(gesture));
    }

    // Flutter: "feedback" / "enabled (default)"
    [Fact]
    public void FeedbackEnabledDefault()
    {
        using FrameworkDartTester tester = CreateTester();
        using var feedback = new FeedbackTester();
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: new InkWell(onTap: () => { }, onLongPress: () => { }, onLongPressUp: () => { })))));
        Tap(tester, InkWellOf(tester));
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(1, feedback.ClickSoundCount);
        Assert.Equal(0, feedback.HapticCount);

        Tap(tester, InkWellOf(tester));
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(2, feedback.ClickSoundCount);
        Assert.Equal(0, feedback.HapticCount);

        LongPress(tester, InkWellOf(tester));
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(2, feedback.ClickSoundCount);
        Assert.Equal(1, feedback.HapticCount);
    }

    // Flutter: "feedback" / "disabled"
    [Fact]
    public void FeedbackDisabled()
    {
        using FrameworkDartTester tester = CreateTester();
        using var feedback = new FeedbackTester();
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: new InkWell(
                            onTap: () => { },
                            onLongPress: () => { },
                            onLongPressUp: () => { },
                            enableFeedback: false)))));
        Tap(tester, InkWellOf(tester));
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(0, feedback.ClickSoundCount);
        Assert.Equal(0, feedback.HapticCount);

        LongPress(tester, InkWellOf(tester));
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(0, feedback.ClickSoundCount);
        Assert.Equal(0, feedback.HapticCount);
    }

    // Flutter: "splashing survives scrolling when keep-alive is enabled"
    [Fact]
    public void SplashingSurvivesScrollingWhenKeepAliveIsEnabled()
    {
        using FrameworkDartTester tester = CreateTester();

        void RunTest(bool keepAlive)
        {
            tester.PumpWidget(
                new MaterialApp(
                    theme: new ThemeData(useMaterial3: false),
                    home: new MaterialWidget(
                        child: new CompositedTransformFollower(
                            // forces a layer, which makes the paints easier to separate out
                            link: new LayerLink(),
                            child: new ListView(
                                addAutomaticKeepAlives: keepAlive,
                                dragStartBehavior: DragStartBehavior.Down,
                                children:
                                [
                                    new SizedBox(
                                        height: 500.0,
                                        child: new InkWell(onTap: () => { }, child: new Placeholder())),
                                    new SizedBox(height: 500.0),
                                    new SizedBox(height: 500.0),
                                ])))));
            PaintAssert.DoesNotPaint(PhysicalModelChild(tester), PaintPattern.Paints.Circle());
            Tap(tester, InkWellOf(tester));
            tester.Pump();
            tester.Pump(TimeSpan.FromMilliseconds(10));
            PaintAssert.Paints(PhysicalModelChild(tester), PaintPattern.Paints.Circle());
            tester.Drag(tester.ElementOfType<ListView>(), new Vector(0.0, -1000.0));
            tester.Pump(TimeSpan.FromMilliseconds(10));
            tester.Drag(tester.ElementOfType<ListView>(), new Vector(0.0, 1000.0));
            tester.Pump(TimeSpan.FromMilliseconds(10));
            if (keepAlive)
            {
                PaintAssert.Paints(PhysicalModelChild(tester), PaintPattern.Paints.Circle());
            }
            else
            {
                PaintAssert.DoesNotPaint(PhysicalModelChild(tester), PaintPattern.Paints.Circle());
            }
        }

        RunTest(true);
        RunTest(false);
    }

    // Flutter: "excludeFromSemantics"
    [Fact]
    public void ExcludeFromSemantics()
    {
        using FrameworkDartTester tester = CreateTester();
        using var semantics = new SemanticsTester(tester);

        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(child: new InkWell(onTap: () => { }, child: new Text("Button")))));
        Assert.True(semantics.IncludesNodeWith(
            label: "Button",
            actions: SemanticsActions.Tap | SemanticsActions.Focus));

        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new InkWell(onTap: () => { }, excludeFromSemantics: true, child: new Text("Button")))));
        Assert.False(semantics.IncludesNodeWith(
            label: "Button",
            actions: SemanticsActions.Tap | SemanticsActions.Focus));
    }

    // Flutter: "ink response doesn't focus when disabled"
    [Fact]
    public void InkResponseDoesNotFocusWhenDisabled()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTouch;
        var focusNode = new FocusNode(debugLabel: "Ink Focus");
        GlobalKey childKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new InkWell(
                        autofocus: true,
                        onTap: () => { },
                        onLongPress: () => { },
                        onLongPressUp: () => { },
                        onHover: _ => { },
                        focusNode: focusNode,
                        child: new Container(key: childKey)))));
        tester.PumpAndSettle();
        Assert.True(focusNode.HasPrimaryFocus);
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new InkWell(focusNode: focusNode, child: new Container(key: childKey)))));
        tester.PumpAndSettle();
        Assert.False(focusNode.HasPrimaryFocus);
        focusNode.Dispose();
    }

    // Flutter: "ink response accepts focus when disabled in directional navigation mode"
    [Fact]
    public void InkResponseAcceptsFocusWhenDisabledInDirectionalNavigationMode()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTouch;
        var focusNode = new FocusNode(debugLabel: "Ink Focus");
        GlobalKey childKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                child: new MediaQuery(
                    data: new MediaQueryData(NavigationMode: NavigationMode.Directional),
                    child: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new InkWell(
                            autofocus: true,
                            onTap: () => { },
                            onLongPress: () => { },
                            onLongPressUp: () => { },
                            onHover: _ => { },
                            focusNode: focusNode,
                            child: new Container(key: childKey))))));
        tester.PumpAndSettle();
        Assert.True(focusNode.HasPrimaryFocus);
        tester.PumpWidget(
            new MaterialWidget(
                child: new MediaQuery(
                    data: new MediaQueryData(NavigationMode: NavigationMode.Directional),
                    child: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new InkWell(focusNode: focusNode, child: new Container(key: childKey))))));
        tester.PumpAndSettle();
        Assert.True(focusNode.HasPrimaryFocus);
        focusNode.Dispose();
    }

    // Flutter: "ink response doesn't hover when disabled"
    [Fact]
    public void InkResponseDoesNotHoverWhenDisabled()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTouch;
        var focusNode = new FocusNode(debugLabel: "Ink Focus");
        GlobalKey childKey = new LabeledGlobalKey<State>(null);
        bool hovering = false;
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: SizedBox.Square(
                        dimension: 100,
                        child: new InkWell(
                            autofocus: true,
                            onTap: () => { },
                            onLongPress: () => { },
                            onHover: value => hovering = value,
                            focusNode: focusNode,
                            child: new SizedBox(key: childKey))))));
        tester.PumpAndSettle();
        Assert.True(focusNode.HasPrimaryFocus);
        TestGesture gesture = tester.CreateGesture(PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(tester.ElementsWithKey(childKey).Single()));
        tester.PumpAndSettle();
        Assert.True(hovering);

        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: SizedBox.Square(
                        dimension: 100,
                        child: new InkWell(
                            focusNode: focusNode,
                            onHover: value => hovering = value,
                            child: new SizedBox(key: childKey))))));

        tester.PumpAndSettle();
        Assert.False(focusNode.HasPrimaryFocus);
        focusNode.Dispose();
    }

    // Flutter: "When ink wells are nested, only the inner one is triggered by tap splash"
    [Fact]
    public void WhenInkWellsAreNestedOnlyTheInnerOneIsTriggeredByTapSplash()
    {
        using FrameworkDartTester tester = CreateTester();
        GlobalKey middleKey = new LabeledGlobalKey<State>(null);
        GlobalKey innerKey = new LabeledGlobalKey<State>(null);

        tester.PumpWidget(
            new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new MaterialWidget(
                    child: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new Center(
                            child: PaddedInkWell(
                                child: PaddedInkWell(
                                    key: middleKey,
                                    child: PaddedInkWell(
                                        key: innerKey,
                                        child: new SizedBox(width: 50, height: 50)))))))));
        RenderObject material = MaterialOf(tester.ElementsWithKey(innerKey).Single());

        // Press
        TestGesture gesture = StartGesture(tester, tester.GetCenter(tester.ElementsWithKey(innerKey).Single()));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, CountCalls(material, "drawCircle"));

        // Up
        gesture.Up();
        tester.PumpAndSettle();
        PaintAssert.PaintsNothing(material);

        // Press again
        gesture.Down(tester.GetCenter(tester.ElementsWithKey(innerKey).Single()));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, CountCalls(material, "drawCircle"));

        // Cancel
        gesture.Cancel();
        tester.PumpAndSettle();
        PaintAssert.PaintsNothing(material);

        // Press again
        gesture.Down(tester.GetCenter(tester.ElementsWithKey(innerKey).Single()));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, CountCalls(material, "drawCircle"));

        // Use a second pointer to press
        TestGesture gesture2 = StartGesture(tester, tester.GetCenter(tester.ElementsWithKey(innerKey).Single()));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, CountCalls(material, "drawCircle"));
        gesture2.Up();
    }

    // Flutter: "Reparenting parent should allow both inkwells to show splash afterwards"
    [Fact]
    public void ReparentingParentShouldAllowBothInkWellsToShowSplashAfterwards()
    {
        using FrameworkDartTester tester = CreateTester();
        GlobalKey middleKey = new LabeledGlobalKey<State>(null);
        GlobalKey innerKey = new LabeledGlobalKey<State>(null);

        tester.PumpWidget(
            new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new MaterialWidget(
                    child: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new Align(
                            alignment: Alignment.TopLeft,
                            child: new SizedBox(
                                width: 200,
                                height: 100,
                                child: new Row(
                                    children:
                                    [
                                        PaddedInkWell(key: middleKey, child: PaddedInkWell(key: innerKey)),
                                        new SizedBox(),
                                    ])))))));
        RenderObject material = MaterialOf(tester.ElementsWithKey(innerKey).Single());

        // Press
        TestGesture gesture1 = StartGesture(tester, tester.GetCenter(tester.ElementsWithKey(innerKey).Single()));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, CountCalls(material, "drawCircle"));

        // Reparent parent
        tester.PumpWidget(
            new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new MaterialWidget(
                    child: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new Align(
                            alignment: Alignment.TopLeft,
                            child: new SizedBox(
                                width: 200,
                                height: 100,
                                child: new Row(
                                    children:
                                    [
                                        PaddedInkWell(key: innerKey),
                                        PaddedInkWell(key: middleKey),
                                    ])))))));

        // Up
        gesture1.Up();
        tester.PumpAndSettle();
        PaintAssert.PaintsNothing(material);

        // Press the previous parent
        gesture1.Down(tester.GetCenter(tester.ElementsWithKey(middleKey).Single()));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, CountCalls(material, "drawCircle"));

        // Use a second pointer to press the previous child
        TestGesture gesture2 = StartGesture(tester, tester.GetCenter(tester.ElementsWithKey(innerKey).Single()));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(2, CountCalls(material, "drawCircle"));

        // Finish gesture to release resources.
        gesture1.Up();
        gesture2.Up();
        tester.PumpAndSettle();
    }

    // Flutter: "Parent inkwell does not block child inkwells from splashes"
    [Fact]
    public void ParentInkWellDoesNotBlockChildInkWellsFromSplashes()
    {
        using FrameworkDartTester tester = CreateTester();
        GlobalKey middleKey = new LabeledGlobalKey<State>(null);
        GlobalKey innerKey = new LabeledGlobalKey<State>(null);

        tester.PumpWidget(
            new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new MaterialWidget(
                    child: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new Center(
                            child: PaddedInkWell(
                                child: PaddedInkWell(
                                    key: middleKey,
                                    child: PaddedInkWell(
                                        key: innerKey,
                                        child: new SizedBox(width: 50, height: 50)))))))));
        RenderObject material = MaterialOf(tester.ElementsWithKey(innerKey).Single());

        // Press middle
        TestGesture gesture1 = StartGesture(
            tester,
            tester.GetTopLeft(tester.ElementsWithKey(middleKey).Single()) + new Vector(1, 1));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, CountCalls(material, "drawCircle"));

        // Press inner
        TestGesture gesture2 = StartGesture(tester, tester.GetCenter(tester.ElementsWithKey(innerKey).Single()));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(2, CountCalls(material, "drawCircle"));

        // Finish gesture to release resources.
        gesture1.Up();
        gesture2.Up();
        tester.PumpAndSettle();
    }

    // Flutter: "Parent inkwell can count the number of pressed children to prevent splash"
    [Fact]
    public void ParentInkWellCanCountTheNumberOfPressedChildrenToPreventSplash()
    {
        using FrameworkDartTester tester = CreateTester();
        GlobalKey parentKey = new LabeledGlobalKey<State>(null);
        GlobalKey leftKey = new LabeledGlobalKey<State>(null);
        GlobalKey rightKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new MaterialWidget(
                    child: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new Center(
                            child: SizedBox.Square(
                                dimension: 100,
                                child: new InkWell(
                                    key: parentKey,
                                    onTap: () => { },
                                    child: new Center(
                                        child: new SizedBox(
                                            width: 100,
                                            height: 50,
                                            child: new Row(
                                                children:
                                                [
                                                    SizedBox.Square(
                                                        dimension: 50,
                                                        child: new InkWell(key: leftKey, onTap: () => { })),
                                                    SizedBox.Square(
                                                        dimension: 50,
                                                        child: new InkWell(key: rightKey, onTap: () => { })),
                                                ]))))))))));
        RenderObject material = MaterialOf(tester.ElementsWithKey(leftKey).Single());

        Point parentPosition = tester.GetTopLeft(tester.ElementsWithKey(parentKey).Single()) + new Vector(1, 1);

        // Press left child
        TestGesture gesture1 = StartGesture(tester, tester.GetCenter(tester.ElementsWithKey(leftKey).Single()));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, CountCalls(material, "drawCircle"));

        // Press right child
        TestGesture gesture2 = StartGesture(tester, tester.GetCenter(tester.ElementsWithKey(rightKey).Single()));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(2, CountCalls(material, "drawCircle"));

        // Press parent
        TestGesture gesture3 = StartGesture(tester, parentPosition);
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(2, CountCalls(material, "drawCircle"));
        gesture3.Up();

        // Release left child
        gesture1.Up();
        tester.PumpAndSettle();
        Assert.Equal(1, CountCalls(material, "drawCircle"));

        // Press parent
        gesture3.Down(parentPosition);
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, CountCalls(material, "drawCircle"));
        gesture3.Up();

        // Release right child
        gesture2.Up();
        tester.PumpAndSettle();
        Assert.Equal(0, CountCalls(material, "drawCircle"));

        // Press parent
        gesture3.Down(parentPosition);
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, CountCalls(material, "drawCircle"));
        gesture3.Up();
    }

    // Flutter: "When ink wells are reparented, the old parent can display splash while the new parent can not"
    [Fact]
    public void WhenInkWellsAreReparentedTheOldParentCanDisplaySplashWhileTheNewParentCanNot()
    {
        using FrameworkDartTester tester = CreateTester();
        GlobalKey innerKey = new LabeledGlobalKey<State>(null);
        GlobalKey leftKey = new LabeledGlobalKey<State>(null);
        GlobalKey rightKey = new LabeledGlobalKey<State>(null);

        Widget DoubleInkWellRow(
            double leftWidth,
            double rightWidth,
            Widget? leftChild = null,
            Widget? rightChild = null)
        {
            return new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new MaterialWidget(
                    child: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new Align(
                            alignment: Alignment.TopLeft,
                            child: new SizedBox(
                                width: leftWidth + rightWidth,
                                height: 100,
                                child: new Row(
                                    children:
                                    [
                                        new SizedBox(
                                            width: leftWidth,
                                            height: 100,
                                            child: new InkWell(
                                                key: leftKey,
                                                onTap: () => { },
                                                child: new Center(
                                                    child: new SizedBox(
                                                        width: leftWidth,
                                                        height: 50,
                                                        child: leftChild)))),
                                        new SizedBox(
                                            width: rightWidth,
                                            height: 100,
                                            child: new InkWell(
                                                key: rightKey,
                                                onTap: () => { },
                                                child: new Center(
                                                    child: new SizedBox(
                                                        width: leftWidth,
                                                        height: 50,
                                                        child: rightChild)))),
                                    ]))))));
        }

        tester.PumpWidget(
            DoubleInkWellRow(
                leftWidth: 110,
                rightWidth: 90,
                leftChild: new InkWell(key: innerKey, onTap: () => { })));
        RenderObject material = MaterialOf(tester.ElementsWithKey(innerKey).Single());

        // Press inner
        TestGesture gesture = StartGesture(tester, new Point(100, 50));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, CountCalls(material, "drawCircle"));

        // Switch side
        tester.PumpWidget(
            DoubleInkWellRow(
                leftWidth: 90,
                rightWidth: 110,
                rightChild: new InkWell(key: innerKey, onTap: () => { })));
        Assert.Equal(0, CountCalls(material, "drawCircle"));

        // A second pointer presses inner
        TestGesture gesture2 = StartGesture(tester, new Point(100, 50));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, CountCalls(material, "drawCircle"));

        gesture.Up();
        gesture2.Up();
        tester.PumpAndSettle();

        // Press inner
        gesture.Down(new Point(100, 50));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, CountCalls(material, "drawCircle"));

        // Press left
        gesture2.Down(new Point(50, 50));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(2, CountCalls(material, "drawCircle"));

        gesture.Up();
        gesture2.Up();
    }

    // Flutter: "Ink wells's splash starts before tap is confirmed and disappear after tap is canceled"
    [Fact]
    public void InkWellsSplashStartsBeforeTapIsConfirmedAndDisappearsAfterTapIsCanceled()
    {
        using FrameworkDartTester tester = CreateTester();
        GlobalKey innerKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new MaterialWidget(
                    child: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new GestureDetector(
                            onHorizontalDragStart: _ => { },
                            child: new Center(
                                child: SizedBox.Square(
                                    dimension: 100,
                                    child: new InkWell(
                                        onTap: () => { },
                                        child: new Center(
                                            child: SizedBox.Square(
                                                dimension: 50,
                                                child: new InkWell(key: innerKey, onTap: () => { })))))))))));
        RenderObject material = MaterialOf(tester.ElementsWithKey(innerKey).Single());

        // Press
        TestGesture gesture = StartGesture(tester, tester.GetCenter(tester.ElementsWithKey(innerKey).Single()));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, CountCalls(material, "drawCircle"));

        // Scroll upward
        gesture.MoveBy(new Vector(0, -100));
        tester.PumpAndSettle();
        PaintAssert.PaintsNothing(material);

        // Up
        gesture.Up();
        tester.PumpAndSettle();
        PaintAssert.PaintsNothing(material);

        // Press again
        gesture.Down(tester.GetCenter(tester.ElementsWithKey(innerKey).Single()));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, CountCalls(material, "drawCircle"));
    }

    // Flutter: "disabled and hovered inkwell responds to mouse-exit"
    [Fact]
    public void DisabledAndHoveredInkWellRespondsToMouseExit()
    {
        using FrameworkDartTester tester = CreateTester();
        int onHoverCount = 0;
        bool hover = false;

        Widget BuildFrame(bool enabled)
        {
            return new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new InkWell(
                                onTap: enabled ? () => { } : null,
                                onHover: value =>
                                {
                                    onHoverCount += 1;
                                    hover = value;
                                })))));
        }

        tester.PumpWidget(BuildFrame(enabled: true));
        TestGesture gesture = tester.CreateGesture(PointerDeviceKind.Mouse);
        gesture.AddPointer();

        gesture.MoveTo(tester.GetCenter(InkWellOf(tester)));
        tester.PumpAndSettle();
        Assert.Equal(1, onHoverCount);
        Assert.True(hover);

        tester.PumpWidget(BuildFrame(enabled: false));
        tester.PumpAndSettle();
        gesture.MoveTo(new Point(0, 0));
        // Even though the InkWell has been disabled, the mouse-exit still
        // causes onHover(false) to be called.
        Assert.Equal(2, onHoverCount);
        Assert.False(hover);

        gesture.MoveTo(tester.GetCenter(InkWellOf(tester)));
        tester.PumpAndSettle();
        // We no longer see hover events because the InkWell is disabled
        // and it's no longer in the "hovering" state.
        Assert.Equal(2, onHoverCount);
        Assert.False(hover);

        tester.PumpWidget(BuildFrame(enabled: true));
        tester.PumpAndSettle();
        // The InkWell was enabled while it contained the mouse, however
        // we do not call onHover() because it may call setState().
        Assert.Equal(2, onHoverCount);
        Assert.False(hover);

        gesture.MoveTo(tester.GetCenter(InkWellOf(tester)) - new Vector(1, 1));
        tester.PumpAndSettle();
        // Moving the mouse a little within the InkWell doesn't change anything.
        Assert.Equal(2, onHoverCount);
        Assert.False(hover);
    }

    // Flutter: "hovered ink well draws a transparent highlight when disabled"
    [Fact]
    public void HoveredInkWellDrawsATransparentHighlightWhenDisabled()
    {
        using FrameworkDartTester tester = CreateTester();
        Widget BuildFrame(bool enabled)
        {
            return new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new InkWell(
                                onTap: enabled ? () => { } : null,
                                onHover: _ => { },
                                hoverColor: new Color(0xff00ff00))))));
        }

        tester.PumpWidget(BuildFrame(enabled: true));
        TestGesture gesture = tester.CreateGesture(PointerDeviceKind.Mouse);
        gesture.AddPointer();

        // Hover the enabled InkWell.
        gesture.MoveTo(tester.GetCenter(InkWellOf(tester)));
        tester.PumpAndSettle();
        PaintAssert.Paints(
            MaterialRenderObject(tester),
            PaintPattern.Paints.Rect(color: new Color(0xff00ff00), rect: Ltrb(350.0, 250.0, 450.0, 350.0)));

        // Disable the hovered InkWell.
        tester.PumpWidget(BuildFrame(enabled: false));
        tester.PumpAndSettle();
        PaintAssert.Paints(
            MaterialRenderObject(tester),
            PaintPattern.Paints.Rect(color: new Color(0x0000ff00), rect: Ltrb(350.0, 250.0, 450.0, 350.0)));
    }

    // Flutter: "Changing InkWell.enabled should not trigger TextButton setState()"
    [Fact]
    public void ChangingInkWellEnabledShouldNotTriggerTextButtonSetState()
    {
        using FrameworkDartTester tester = CreateTester();
        Widget BuildFrame(bool enabled)
        {
            return new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: new TextButton(onPressed: enabled ? () => { } : null, child: new Text("button")))));
        }

        tester.PumpWidget(BuildFrame(enabled: false));

        TestGesture gesture = tester.CreateGesture(PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(tester.ElementOfType<TextButton>()));
        tester.PumpAndSettle();

        // Rebuilding the button with enabled:true causes InkWell.didUpdateWidget()
        // to be called per the change in its enabled flag. If onHover() was called,
        // this test would crash.
        tester.PumpWidget(BuildFrame(enabled: true));
        tester.PumpAndSettle();

        // Rebuild again, with enabled:false
        gesture.MoveBy(new Vector(1, 1));
        tester.PumpWidget(BuildFrame(enabled: false));
        tester.PumpAndSettle();
        Assert.Null(tester.TakeException());
    }

    // Flutter: "InkWell does not attach semantics handler for onTap if it was not provided an onTap handler"
    [Fact]
    public void InkWellDoesNotAttachSemanticsHandlerForOnTapIfItWasNotProvidedAnOnTapHandler()
    {
        using FrameworkDartTester tester = CreateTester();
        using var semantics = new SemanticsTester(tester);
        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Center(
                        child: new InkWell(
                            onLongPress: () => { },
                            onLongPressUp: () => { },
                            child: new Text("Foo"))))));

        SemanticsMatchers.ExpectSemantics(
            tester.GetSemantics(Find.BySemanticsLabel("Foo")),
            SemanticsMatchers.MatchesSemantics(
                label: "Foo",
                hasFocusAction: true,
                hasLongPressAction: true,
                isFocusable: true,
                textDirection: TextDirection.Ltr));

        // Add tap handler and confirm addition to semantic actions.
        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Center(
                        child: new InkWell(
                            onLongPress: () => { },
                            onLongPressUp: () => { },
                            onTap: () => { },
                            child: new Text("Foo"))))));

        SemanticsMatchers.ExpectSemantics(
            tester.GetSemantics(Find.BySemanticsLabel("Foo")),
            SemanticsMatchers.MatchesSemantics(
                label: "Foo",
                hasTapAction: true,
                hasFocusAction: true,
                hasLongPressAction: true,
                isFocusable: true,
                textDirection: TextDirection.Ltr));
    }

    // Flutter: "InkWell highlight should not survive after [onTapDown, onDoubleTap] sequence"
    [Fact]
    public void InkWellHighlightShouldNotSurviveAfterTapDownDoubleTapSequence()
    {
        using FrameworkDartTester tester = CreateTester();
        var log = new List<string>();

        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Center(
                        child: new InkWell(
                            onTap: () => log.Add("tap"),
                            onDoubleTap: () => log.Add("double-tap"),
                            onTapDown: _ => log.Add("tap-down"),
                            onTapCancel: () => log.Add("tap-cancel"))))));

        Point tapLocation = tester.GetRect(InkWellOf(tester)).Center;

        TestGesture gesture = StartGesture(tester, tapLocation);
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(["tap-down"], log);
        gesture.Up();
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Tap(tester, InkWellOf(tester));
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(["tap-down", "double-tap"], log);

        tester.PumpAndSettle();
        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "drawRect"));
    }

    // Flutter: "InkWell splash should not survive after [onTapDown, onTapDown, onTapCancel, onDoubleTap] sequence"
    [Fact]
    public void InkWellSplashShouldNotSurviveAfterTapDownTapDownTapCancelDoubleTapSequence()
    {
        using FrameworkDartTester tester = CreateTester();
        var log = new List<string>();

        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Center(
                        child: new InkWell(
                            onTap: () => log.Add("tap"),
                            onDoubleTap: () => log.Add("double-tap"),
                            onTapDown: _ => log.Add("tap-down"),
                            onTapCancel: () => log.Add("tap-cancel"))))));

        Point tapLocation = tester.GetRect(InkWellOf(tester)).Center;

        TestGesture gesture1 = StartGesture(tester, tapLocation);
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(["tap-down"], log);
        gesture1.Up();
        tester.Pump(TimeSpan.FromMilliseconds(100));

        TestGesture gesture2 = StartGesture(tester, tapLocation);
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(["tap-down", "tap-down"], log);
        gesture2.Up();
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(["tap-down", "tap-down", "tap-cancel", "double-tap"], log);

        tester.PumpAndSettle();
        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "drawCircle"));
    }

    // Flutter: "InkWell disposes statesController"
    [Fact]
    public void InkWellDisposesStatesController()
    {
        using FrameworkDartTester tester = CreateTester();
        int tapCount = 0;
        Widget BuildFrame(WidgetStatesController? statesController)
        {
            return new MaterialApp(
                home: new Scaffold(
                    body: new Center(
                        child: new InkWell(
                            statesController: statesController,
                            onTap: () => tapCount += 1,
                            child: new Text("inkwell")))));
        }

        using var controller = new WidgetStatesController();
        int pressedCount = 0;
        controller.AddListener(() =>
        {
            if (controller.Value.Contains(WidgetState.Pressed))
            {
                pressedCount += 1;
            }
        });

        tester.PumpWidget(BuildFrame(controller));
        Tap(tester, InkWellOf(tester));
        tester.PumpAndSettle();
        Assert.Equal(1, tapCount);
        Assert.Equal(1, pressedCount);

        tester.PumpWidget(BuildFrame(null));
        Tap(tester, InkWellOf(tester));
        tester.PumpAndSettle();
        Assert.Equal(2, tapCount);
        Assert.Equal(1, pressedCount);

        tester.PumpWidget(BuildFrame(controller));
        Tap(tester, InkWellOf(tester));
        tester.PumpAndSettle();
        Assert.Equal(3, tapCount);
        Assert.Equal(2, pressedCount);
    }

    // Flutter: "ink well overlayColor opacity fades from 0xff when hover ends"
    [Fact]
    public void InkWellOverlayColorOpacityFadesFrom0xffWhenHoverEnds()
    {
        // Regression test for https://github.com/flutter/flutter/issues/110266
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialWidget(
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 100,
                            child: new InkWell(
                                overlayColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                                    states.Contains(WidgetState.Hovered) ? new Color(0xff00ff00) : null),
                                onTap: () => { },
                                onLongPress: () => { },
                                onLongPressUp: () => { },
                                onHover: _ => { }))))));
        TestGesture gesture = tester.CreateGesture(PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(tester.ElementOfType<SizedBox>()));
        tester.PumpAndSettle();
        gesture.MoveTo(new Point(10, 10)); // fade out the overlay
        tester.Pump(); // trigger the fade out animation
        RenderObject inkFeatures = GetInkFeatures(tester);
        // Fadeout begins with the MaterialStates.hovered overlay color
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.Rect(rect: Ltrb(350.0, 250.0, 450.0, 350.0), color: new Color(0xff00ff00)));
        // 50ms fadeout is 50% complete, overlay color alpha goes from 0xff to 0x80
        tester.Pump(TimeSpan.FromMilliseconds(25));
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.Rect(rect: Ltrb(350.0, 250.0, 450.0, 350.0), color: new Color(0x8000ff00)));
    }

    // Flutter: "InkWell secondary tap test"
    [Fact]
    public void InkWellSecondaryTapTest()
    {
        using FrameworkDartTester tester = CreateTester();
        var log = new List<string>();

        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Center(
                        child: new InkWell(
                            onSecondaryTap: () => log.Add("secondary-tap"),
                            onSecondaryTapDown: _ => log.Add("secondary-tap-down"),
                            onSecondaryTapUp: _ => log.Add("secondary-tap-up"),
                            onSecondaryTapCancel: () => log.Add("secondary-tap-cancel"))))));

        Tap(tester, InkWellOf(tester), PointerButtons.Secondary);

        Assert.Equal(["secondary-tap-down", "secondary-tap-up", "secondary-tap"], log);
        log.Clear();

        TestGesture gesture = StartGesture(tester, tester.GetCenter(InkWellOf(tester)), PointerButtons.Secondary);
        gesture.MoveTo(new Point(100, 100));
        gesture.Up();

        Assert.Equal(["secondary-tap-down", "secondary-tap-cancel"], log);
    }

    // Flutter: "InkWell secondary tap should not draw a splash when no secondary callbacks are defined"
    [Fact]
    public void InkWellSecondaryTapShouldNotDrawASplashWhenNoSecondaryCallbacksAreDefined()
    {
        // Regression test for https://github.com/flutter/flutter/issues/124328.
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(child: new Center(child: new InkWell(onTap: () => { })))));

        TestGesture gesture = StartGesture(tester, tester.GetRect(InkWellOf(tester)).Center, PointerButtons.Secondary);
        tester.Pump(TimeSpan.FromMilliseconds(200));

        // No splash should be painted.
        RenderObject inkFeatures = GetInkFeatures(tester);
        Assert.Equal(0, CountCalls(inkFeatures, "drawCircle"));

        gesture.Up();
    }

    // Flutter: "try out hoverDuration property"
    [Fact]
    public void TryOutHoverDurationProperty()
    {
        using FrameworkDartTester tester = CreateTester();
        var log = new List<string>();

        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Center(
                        child: new InkWell(
                            hoverDuration: TimeSpan.FromMilliseconds(1000),
                            onTap: () => log.Add("tap"))))));

        Tap(tester, InkWellOf(tester));
        tester.Pump(TimeSpan.FromSeconds(1));

        Assert.Equal(["tap"], log);
        log.Clear();
    }

    // Flutter: "InkWell activation action does not end immediately"
    [Fact]
    public void InkWellActivationActionDoesNotEndImmediately()
    {
        // Regression test for https://github.com/flutter/flutter/issues/132377.
        using FrameworkDartTester tester = CreateTester();
        var controller = new WidgetStatesController();

        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new Shortcuts(
                    shortcuts: new Dictionary<ShortcutActivator, Intent>
                    {
                        [new SingleActivator(LogicalKeyboardKey.Enter)] = new ButtonActivateIntent(),
                    },
                    child: new MaterialWidget(
                        child: new Center(
                            child: new InkWell(autofocus: true, onTap: () => { }, statesController: controller))))));

        // Invoke the InkWell activation action.
        tester.SendKeyEvent(LogicalKeyboardKey.Enter);

        // The InkWell is in pressed state.
        tester.Pump(TimeSpan.FromMilliseconds(99));
        Assert.Contains(WidgetState.Pressed, controller.Value);

        tester.PumpAndSettle();
        Assert.DoesNotContain(WidgetState.Pressed, controller.Value);

        controller.Dispose();
    }

    // Flutter: "InkResponse does not crash in zero area"
    [Fact]
    public void InkResponseDoesNotCrashInZeroArea()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialApp(
                home: new MaterialWidget(
                    child: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new Center(child: SizedBox.Shrink(child: new InkResponse()))))));
        Assert.Equal(new Size(0, 0), tester.GetSize(tester.ElementOfType<InkResponse>()));
    }

    // Flutter: "InkWell does not crash at zero area"
    [Fact]
    public void InkWellDoesNotCrashAtZeroArea()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialApp(
                home: new Scaffold(body: new Center(child: SizedBox.Shrink(child: new InkWell())))));
        Assert.Equal(new Size(0, 0), tester.GetSize(InkWellOf(tester)));
    }

    private static readonly Color SelectedHoveredColor = new(0xff00ff00);
    private static readonly Color SelectedFocusedColor = new(0xff0000ff);
    private static readonly Color SelectedPressedColor = new(0xff00ffff);
    private static readonly Rect SelectedInkRect = Ltrb(0, 0, 100, 100);

    // The `boilerplate` of the "Ink well overlayColor resolution respects WidgetState.selected" group.
    private static Widget SelectedBoilerplate(WidgetStatesController statesController, FocusNode? focusNode = null)
    {
        return new MaterialWidget(
            child: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new Align(
                    alignment: Alignment.TopLeft,
                    child: SizedBox.Square(
                        dimension: 100,
                        child: new InkWell(
                            splashFactory: NoSplash.SplashFactory,
                            focusNode: focusNode,
                            statesController: statesController,
                            overlayColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                            {
                                if (states.Contains(WidgetState.Selected))
                                {
                                    if (states.Contains(WidgetState.Pressed))
                                    {
                                        return SelectedPressedColor;
                                    }

                                    if (states.Contains(WidgetState.Hovered))
                                    {
                                        return SelectedHoveredColor;
                                    }

                                    if (states.Contains(WidgetState.Focused))
                                    {
                                        return SelectedFocusedColor;
                                    }

                                    return new Color(0xffbadbad); // Shouldn't happen.
                                }

                                return Colors.Black;
                            }),
                            onTap: () => { })))));
    }

    // The overlayColor resolver the hover/focus/pressed overlayColor tests share.
    private static WidgetStateProperty<Color?> HoverFocusPressedOverlay(Color pressedColor)
    {
        return WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Hovered))
            {
                return new Color(0xff00ff00);
            }

            if (states.Contains(WidgetState.Focused))
            {
                return new Color(0xff0000ff);
            }

            if (states.Contains(WidgetState.Pressed))
            {
                return pressedColor;
            }

            return new Color(0xffbadbad); // Shouldn't happen.
        });
    }

    // The `paddedInkWell` helper of the nested ink well tests.
    private static InkWell PaddedInkWell(Key? key = null, Widget? child = null)
    {
        return new InkWell(
            key: key,
            onTap: () => { },
            child: new Padding(EdgeInsets.All(50), child: child));
    }

    // flutter_test runs every test on android, with gesture timers on the fake-async clock.
    private static FrameworkDartTester CreateTester() => new(fakeGestureTimers: true);

    private static Rect Ltrb(double left, double top, double right, double bottom) =>
        new(new Point(left, top), new Point(right, bottom));

    private static Path RoundedPath(Rect rect, double radius)
    {
        var path = new Path();
        path.AddRRect(RRect.FromRectAndRadius(rect, Radius.Circular(radius)));
        return path;
    }

    // The test file's `getInkFeatures`: the first `_RenderInkFeatures` in the render tree.
    private static RenderObject GetInkFeatures(FrameworkDartTester tester) =>
        InkFeatureProbe.Controllers(tester.RenderView).First();

    // `Material.of(element)`, which is the `_RenderInkFeatures` render box.
    private static RenderObject MaterialOf(Element element) => (RenderInkFeatures)MaterialWidget.Of(element);

    // `find.byType(Material)` as a paint matcher target: the Material's render object.
    private static RenderObject MaterialRenderObject(FrameworkDartTester tester) =>
        tester.ElementOfType<MaterialWidget>().FindRenderObject()!;

    // `tester.renderObject<RenderProxyBox>(find.byType(PhysicalModel)).child`.
    private static RenderObject PhysicalModelChild(FrameworkDartTester tester) =>
        ((RenderProxyBox)tester.ElementOfType<PhysicalModel>().FindRenderObject()!).Child!;

    private static Element InkWellOf(FrameworkDartTester tester) => tester.ElementOfType<InkWell>();

    private static int CountCalls(RenderObject renderObject, string method) =>
        PaintRecording.Record(renderObject).CountCalls(method);

    private static MouseCursor? ActiveCursor(TestGesture gesture) =>
        RendererBinding.Instance.MouseTracker.DebugDeviceActiveCursor(gesture.Pointer);

    // `tester.startGesture(location, buttons: ...)`: a touch pointer, down.
    private static TestGesture StartGesture(
        FrameworkDartTester tester,
        Point location,
        PointerButtons buttons = PointerButtons.Primary) =>
        tester.StartGesture(location, PointerDeviceKind.Touch, buttons);

    // `tester.tap(finder, buttons: ...)`: a down and an up at the center, no pump.
    private static void Tap(
        FrameworkDartTester tester,
        Element element,
        PointerButtons buttons = PointerButtons.Primary)
    {
        TestGesture gesture = StartGesture(tester, tester.GetCenter(element), buttons);
        gesture.Up();
    }

    // `tester.longPress(finder)`: down, `pump(kLongPressTimeout + kPressTimeout)`, up.
    private static void LongPress(FrameworkDartTester tester, Element element)
    {
        TestGesture gesture = StartGesture(tester, tester.GetCenter(element));
        tester.Pump(LongPressDuration);
        gesture.Up();
    }
}
