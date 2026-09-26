// Dart parity source: material_ui/lib/src/range_slider.dart
// Mirrors material-ui-src/test/range_slider_test.dart (lines 1985-4113)

using System.Globalization;
using System.Text;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MaterialWidget = Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class RangeSliderDartParityTestsB : IDisposable
{
    public RangeSliderDartParityTestsB()
    {
        FocusManager.Instance.ResetForTests();
        // flutter_test's `defaultTargetPlatform` is android.
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.Automatic;
        FocusManager.Instance.ResetForTests();
    }

    // Flutter: "RangeSlider.label info should not write to semantic node"
    [Fact]
    public void LabelInfoShouldNotWriteToSemanticNode()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        using var semantics = new SemanticsHandleScope(tester);
        tester.PumpWidget(new MaterialApp(
            home: new Theme(
                data: new ThemeData(),
                child: new Directionality(
                    TextDirection.Ltr,
                    new MaterialWidget(
                        child: new RangeSlider(
                            values: new RangeValues(10.0, 12.0),
                            max: 100.0,
                            onChanged: _ => { },
                            labels: new RangeLabels("Begin", "End")))))));

        PumpAndSettle(tester);

        ExpectSemantics(
            GetSemantics(SliderElement(tester)),
            RangeSliderSemantics(
                ThumbSemantics(value: "10%", increasedValue: "10%", decreasedValue: "5%", label: string.Empty),
                ThumbSemantics(value: "12%", increasedValue: "17%", decreasedValue: "12%", label: string.Empty)));
    }

    // Flutter: "Range Slider Semantics - ltr"
    [Fact]
    public void RangeSliderSemanticsLtr()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        using var semantics = new SemanticsHandleScope(tester);
        tester.PumpWidget(new MaterialApp(
            home: new Theme(
                data: new ThemeData(),
                child: new Directionality(
                    TextDirection.Ltr,
                    new MaterialWidget(
                        child: new RangeSlider(
                            values: new RangeValues(10.0, 30.0),
                            max: 100.0,
                            onChanged: _ => { }))))));

        PumpAndSettle(tester);

        SemanticsNode semanticsNode = GetSemantics(SliderElement(tester));
        ExpectSemantics(
            semanticsNode,
            RangeSliderSemantics(
                ThumbSemantics(
                    value: "10%",
                    increasedValue: "15%",
                    decreasedValue: "5%",
                    rect: Ltrb(75.2, 276.0, 123.2, 324.0)),
                ThumbSemantics(
                    value: "30%",
                    increasedValue: "35%",
                    decreasedValue: "25%",
                    rect: Ltrb(225.6, 276.0, 273.6, 324.0))));

        // Dart's workaround for https://github.com/flutter/flutter/issues/115079: the rounded thumb rects.
        Assert.Equal(
            [Ltrb(75.0, 276.0, 123.0, 324.0), Ltrb(226.0, 276.0, 274.0, 324.0)],
            RoundedThumbRects(semanticsNode));
    }

    // Flutter: "Range Slider Semantics - rtl"
    [Fact]
    public void RangeSliderSemanticsRtl()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        using var semantics = new SemanticsHandleScope(tester);
        tester.PumpWidget(new MaterialApp(
            home: new Theme(
                data: new ThemeData(),
                child: new Directionality(
                    TextDirection.Rtl,
                    new MaterialWidget(
                        child: new RangeSlider(
                            values: new RangeValues(10.0, 30.0),
                            max: 100.0,
                            onChanged: _ => { }))))));

        PumpAndSettle(tester);

        SemanticsNode semanticsNode = GetSemantics(SliderElement(tester));
        ExpectSemantics(
            semanticsNode,
            RangeSliderSemantics(
                ThumbSemantics(value: "10%", increasedValue: "15%", decreasedValue: "5%"),
                ThumbSemantics(value: "30%", increasedValue: "35%", decreasedValue: "25%")));

        Assert.Equal(
            [Ltrb(526.0, 276.0, 574.0, 324.0), Ltrb(677.0, 276.0, 725.0, 324.0)],
            RoundedThumbRects(semanticsNode));
    }

    // Flutter: "Range Slider implements debugFillProperties"
    [DebugOnlyFact]
    public void RangeSliderImplementsDebugFillProperties()
    {
        var builder = new DiagnosticPropertiesBuilder();

        new RangeSlider(
            activeColor: MaterialColors.Blue,
            divisions: 4,
            inactiveColor: MaterialColors.Grey,
            labels: new RangeLabels("lowerValue", "upperValue"),
            max: 100.0,
            onChanged: null,
            values: new RangeValues(25.0, 75.0)).DebugFillProperties(builder);

        List<string> description = builder.Properties
            .Where(node => !node.IsFiltered(DiagnosticLevel.Info))
            .Select(node => node.ToString())
            .ToList();

        Assert.Equal(
            [
                "valueStart: 25.0",
                "valueEnd: 75.0",
                "disabled",
                "min: 0.0",
                "max: 100.0",
                "divisions: 4",
                "labelStart: \"lowerValue\"",
                "labelEnd: \"upperValue\"",
                $"activeColor: MaterialColor(primary value: {new Color(0xff2196f3)})",
                $"inactiveColor: MaterialColor(primary value: {new Color(0xff9e9e9e)})",
            ],
            description);
    }

    // Flutter: "Range Slider can be painted in a narrower constraint when track shape is RoundedRectRange"
    [Fact]
    public void CanBePaintedInANarrowerConstraintWhenTrackShapeIsRoundedRectRange()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new MaterialWidget(
                    child: new Center(
                        child: new SizedBox(
                            height: 10.0,
                            width: 0.0,
                            child: new RangeSlider(values: new RangeValues(0.25, 0.5), onChanged: null)))))));

        RenderObject renderObject = AllRenderObjects(tester).OfType<RenderRangeSlider>().First();

        PaintAssert.Paints(
            renderObject,
            PaintPattern.Paints
                // left inactive track RRect
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    -24.0,
                    3.0,
                    -12.0,
                    7.0,
                    topLeft: Radius.Circular(2.0),
                    bottomLeft: Radius.Circular(2.0)))
                // right inactive track RRect
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    0.0,
                    3.0,
                    24.0,
                    7.0,
                    topRight: Radius.Circular(2.0),
                    bottomRight: Radius.Circular(2.0)))
                // active track RRect
                .RRect(rrect: RRect.FromLTRBR(-14.0, 2.0, 2.0, 8.0, Radius.Circular(2.0)))
                // thumbs
                .Circle(x: -12.0, y: 5.0, radius: 10.0)
                .Circle(x: 0.0, y: 5.0, radius: 10.0));
    }

    // Flutter: "Range Slider can be painted in a narrower constraint when track shape is Rectangular"
    [Fact]
    public void CanBePaintedInANarrowerConstraintWhenTrackShapeIsRectangular()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(
                sliderTheme: new SliderThemeData(RangeTrackShape: new RectangularRangeSliderTrackShape())),
            home: new Directionality(
                TextDirection.Ltr,
                new MaterialWidget(
                    child: new Center(
                        child: new SizedBox(
                            height: 10.0,
                            width: 0.0,
                            child: new RangeSlider(values: new RangeValues(0.25, 0.5), onChanged: null)))))));

        RenderObject renderObject = AllRenderObjects(tester).OfType<RenderRangeSlider>().First();

        // There should no gap between the inactive track and active track.
        PaintAssert.Paints(
            renderObject,
            PaintPattern.Paints
                // left inactive track RRect
                .Rect(rect: Ltrb(-24.0, 3.0, -12.0, 7.0))
                // active track RRect
                .Rect(rect: Ltrb(-12.0, 3.0, 0.0, 7.0))
                // right inactive track RRect
                .Rect(rect: Ltrb(0.0, 3.0, 24.0, 7.0))
                // thumbs
                .Circle(x: -12.0, y: 5.0, radius: 10.0)
                .Circle(x: 0.0, y: 5.0, radius: 10.0));
    }

    // Flutter: "Update the divisions and values at the same time for RangeSlider"
    [Fact]
    public void UpdateTheDivisionsAndValuesAtTheSameTime()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);

        // Regress test for https://github.com/flutter/flutter/issues/65943
        Widget BuildFrame(double maxValue)
        {
            return new MaterialApp(
                home: new MaterialWidget(
                    child: new Center(
                        child: new RangeSlider(
                            values: new RangeValues(5, 8),
                            max: maxValue,
                            divisions: (int)maxValue,
                            onChanged: _ => { }))));
        }

        tester.PumpWidget(BuildFrame(10));

        RenderObject renderObject = AllRenderObjects(tester).OfType<RenderRangeSlider>().First();

        // Update the divisions from 10 to 15, the thumbs should be paint at the correct position.
        tester.PumpWidget(BuildFrame(15));
        tester.PumpAndSettle(); // Finish the animation.

        // `paints..rrect()..rrect()..something(drawRRect)`: the third drawRRect.
        IReadOnlyList<CanvasCall> calls = PaintRecording.Record(renderObject);
        RRect activeTrackRRect = calls.Where(call => call.Method == "drawRRect").ElementAt(2).RRect!.Value;

        const double padding = 4.0;
        // The 1st thumb should at one-third(5 / 15) of the Slider.
        // The 2nd thumb should at (8 / 15) of the Slider.
        // The left of the active track shape is the position of the 1st thumb.
        // The right of the active track shape is the position of the 2nd thumb.
        // 24.0 is the default margin, (800.0 - 24.0 - 24.0 - padding) is the slider's width.
        // Where the padding value equals to the track height.
        Assert.True(NearEqual(activeTrackRRect.Left, ((800.0 - 24.0 - 24.0 - padding) * (5 / 15.0)) + 24.0, 0.01));
        Assert.True(NearEqual(
            activeTrackRRect.Right,
            ((800.0 - 24.0 - 24.0 - padding) * (8 / 15.0)) + 24.0 + padding,
            0.01));
    }

    // Flutter: "RangeSlider changes mouse cursor when hovered"
    [Fact]
    public void ChangesMouseCursorWhenHovered()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var values = new RangeValues(50, 70);

        // Test default cursor.
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new MaterialWidget(
                    child: new Center(
                        child: new MouseRegion(
                            cursor: SystemMouseCursors.Forbidden,
                            child: new RangeSlider(values: values, max: 100.0, onChanged: _ => { })))))));

        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer(location: tester.GetCenter(SliderElement(tester)));

        Assert.Equal(SystemMouseCursors.Click, ActiveCursor(gesture));

        // Test custom cursor.
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new MaterialWidget(
                    child: new Center(
                        child: new MouseRegion(
                            cursor: SystemMouseCursors.Forbidden,
                            child: new RangeSlider(
                                values: values,
                                max: 100.0,
                                mouseCursor: new WidgetStatePropertyAll<MouseCursor?>(SystemMouseCursors.Text),
                                onChanged: _ => { })))))));

        tester.Pump();
        Assert.Equal(SystemMouseCursors.Text, ActiveCursor(gesture));
    }

    // Flutter: "RangeSlider WidgetStateMouseCursor resolves correctly"
    [Fact]
    public void WidgetStateMouseCursorResolvesCorrectly()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var values = new RangeValues(20, 75);
        MouseCursor systemDefaultCursor = SystemMouseCursors.Basic;
        MouseCursor disabledCursor = SystemMouseCursors.Forbidden;
        MouseCursor draggedCursor = SystemMouseCursors.Move;
        MouseCursor hoveredCursor = SystemMouseCursors.Grab;

        Widget BuildFrame(bool enabled)
        {
            return new MaterialApp(
                home: new Directionality(
                    TextDirection.Ltr,
                    new MaterialWidget(
                        child: new Column(
                            mainAxisAlignment: MainAxisAlignment.Center,
                            children:
                            [
                                new Center(
                                    child: new StatefulBuilder((context, setState) => new RangeSlider(
                                        mouseCursor: WidgetStateProperty<MouseCursor?>.ResolveWith(states =>
                                        {
                                            if (states.Contains(WidgetState.Disabled))
                                            {
                                                return disabledCursor;
                                            }

                                            if (states.Contains(WidgetState.Dragged))
                                            {
                                                return draggedCursor;
                                            }

                                            if (states.Contains(WidgetState.Hovered))
                                            {
                                                return hoveredCursor;
                                            }

                                            return SystemMouseCursors.Click;
                                        }),
                                        values: values,
                                        max: 100.0,
                                        onChanged: enabled
                                            ? newValues => setState(() => values = newValues)
                                            : null))),
                            ]))));
        }

        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        try
        {
            // System default.
            gesture.AddPointer(location: default);
            tester.PumpWidget(BuildFrame(enabled: false));
            Assert.Equal(systemDefaultCursor, ActiveCursor(gesture));

            // Disabled.
            gesture.MoveTo(tester.GetCenter(SliderElement(tester)));
            tester.Pump();
            Assert.Equal(disabledCursor, ActiveCursor(gesture));

            // Hovered.
            tester.PumpWidget(BuildFrame(enabled: true));
            Assert.Equal(hoveredCursor, ActiveCursor(gesture));

            // Dragged.
            gesture.Down(tester.GetCenter(SliderElement(tester)));
            gesture.MoveBy(new Vector(20.0, 0.0));
            tester.Pump();
            Assert.Equal(draggedCursor, ActiveCursor(gesture));

            // Hovered.
            gesture.Up();
            tester.Pump();
            Assert.Equal(hoveredCursor, ActiveCursor(gesture));

            // System default.
            gesture.MoveTo(default);
            KeySim.SendKeyCombination(LogicalKeyboardKey.Tab);
            tester.Pump();
            Assert.Equal(systemDefaultCursor, ActiveCursor(gesture));
        }
        finally
        {
            gesture.RemovePointer();
        }
    }

    // Flutter: "RangeSlider can be hovered and has correct hover color"
    [Fact]
    public void CanBeHoveredAndHasCorrectHoverColor()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var values = new RangeValues(50, 70);
        var theme = new ThemeData();

        Widget BuildApp(bool enabled = true)
        {
            return new MaterialApp(
                home: new Directionality(
                    TextDirection.Ltr,
                    new StatefulBuilder((context, setState) => new MaterialWidget(
                        child: new Center(
                            child: new RangeSlider(
                                values: values,
                                max: 100.0,
                                onChanged: enabled
                                    ? newValues => setState(() => values = newValues)
                                    : null))))));
        }

        tester.PumpWidget(BuildApp());

        // RangeSlider does not have overlay when enabled and not hovered.
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(
            MaterialOf(tester),
            PaintPattern.Paints.Circle(color: theme.ColorScheme.Primary.WithOpacity(0.12)));

        // Start hovering.
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(SliderElement(tester)));

        // RangeSlider has overlay when enabled and hovered.
        tester.PumpWidget(BuildApp());
        tester.PumpAndSettle();
        PaintAssert.Paints(
            MaterialOf(tester),
            PaintPattern.Paints.Circle(color: theme.ColorScheme.Primary.WithOpacity(0.12)));

        // RangeSlider does not have an overlay when disabled and hovered.
        tester.PumpWidget(BuildApp(enabled: false));
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(
            MaterialOf(tester),
            PaintPattern.Paints.Circle(color: theme.ColorScheme.Primary.WithOpacity(0.12)));
    }

    // Flutter: "RangeSlider can be focused using keyboard focus"
    [Fact]
    public void CanBeFocusedUsingKeyboardFocus()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var values = new RangeValues(20, 80);
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new MaterialWidget(
                    child: new StatefulBuilder((context, setState) => new Center(
                        child: new RangeSlider(
                            values: values,
                            max: 100,
                            onChanged: newValues => setState(() => values = newValues),
                            onChangeStart: _ => { },
                            onChangeEnd: _ => { })))))));

        // Focus on the start thumb
        Assert.Single(tester.ElementsOfType<RangeSlider>());
        FocusNode startFocusNode = SliderState(tester).StartFocusNode;
        FocusNode endFocusNode = SliderState(tester).EndFocusNode;

        startFocusNode.RequestFocus();
        tester.PumpAndSettle();
        Assert.Same(startFocusNode, FocusManager.Instance.PrimaryFocus);

        // Tab to focus on the end thumb
        KeySim.SendKeyCombination(LogicalKeyboardKey.Tab);
        tester.PumpAndSettle();
        Assert.Same(endFocusNode, FocusManager.Instance.PrimaryFocus);
    }

    // Flutter: "Keyboard focus also changes semantics focus"
    [Fact]
    public void KeyboardFocusAlsoChangesSemanticsFocus()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        using var semantics = new SemanticsHandleScope(tester);
        tester.PumpWidget(new MaterialApp(
            home: new Theme(
                data: new ThemeData(),
                child: new Directionality(
                    TextDirection.Ltr,
                    new MaterialWidget(
                        child: new RangeSlider(
                            values: new RangeValues(10.0, 30.0),
                            max: 100.0,
                            onChanged: _ => { }))))));

        PumpAndSettle(tester);
        FocusNode startFocusNode = SliderState(tester).StartFocusNode;
        FocusNode endFocusNode = SliderState(tester).EndFocusNode;

        // Focus on the start thumb
        startFocusNode.RequestFocus();
        PumpAndSettle(tester);
        Assert.Same(startFocusNode, FocusManager.Instance.PrimaryFocus);

        SemanticsNode semanticsNode = GetSemantics(SliderElement(tester));
        ExpectSemantics(
            semanticsNode,
            RangeSliderSemantics(
                ThumbSemantics(
                    value: "10%",
                    increasedValue: "15%",
                    decreasedValue: "5%",
                    rect: Ltrb(75.2, 276.0, 123.2, 324.0),
                    isFocused: true),
                ThumbSemantics(
                    value: "30%",
                    increasedValue: "35%",
                    decreasedValue: "25%",
                    rect: Ltrb(225.6, 276.0, 273.6, 324.0))));

        // Tab to focus on the end thumb
        KeySim.SendKeyCombination(LogicalKeyboardKey.Tab);
        PumpAndSettle(tester);
        Assert.Same(endFocusNode, FocusManager.Instance.PrimaryFocus);

        ExpectSemantics(
            semanticsNode,
            RangeSliderSemantics(
                ThumbSemantics(
                    value: "10%",
                    increasedValue: "15%",
                    decreasedValue: "5%",
                    rect: Ltrb(75.2, 276.0, 123.2, 324.0)),
                ThumbSemantics(
                    value: "30%",
                    increasedValue: "35%",
                    decreasedValue: "25%",
                    rect: Ltrb(225.6, 276.0, 273.6, 324.0),
                    isFocused: true)));
    }

    // Flutter: "RangeSlider is draggable and has correct dragged color"
    [Fact]
    public void IsDraggableAndHasCorrectDraggedColor()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var values = new RangeValues(50, 70);
        var theme = new ThemeData();

        Widget BuildApp(bool enabled = true)
        {
            return new MaterialApp(
                home: new Directionality(
                    TextDirection.Ltr,
                    new StatefulBuilder((context, setState) => new MaterialWidget(
                        child: new Center(
                            child: new RangeSlider(
                                values: values,
                                max: 100.0,
                                onChanged: enabled
                                    ? newValues => setState(() => values = newValues)
                                    : null))))));
        }

        tester.PumpWidget(BuildApp());

        // RangeSlider does not have overlay when enabled and not dragged.
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(
            MaterialOf(tester),
            PaintPattern.Paints.Circle(color: theme.ColorScheme.Primary.WithOpacity(0.12)));

        // Start dragging.
        TestGesture drag = tester.StartGesture(tester.GetCenter(SliderElement(tester)), PointerDeviceKind.Touch);
        tester.Pump(GestureConstants.PressTimeout);

        // Less than configured touch slop, more than default touch slop
        drag.MoveBy(new Vector(19.0, 0));
        tester.Pump();

        // RangeSlider has overlay when enabled and dragged.
        PaintAssert.Paints(
            MaterialOf(tester),
            PaintPattern.Paints.Circle(color: theme.ColorScheme.Primary.WithOpacity(0.12)));
    }

    // Flutter: "RangeSlider overlayColor supports hovered and dragged states"
    [Fact]
    public void OverlayColorSupportsHoveredAndDraggedStates()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var values = new RangeValues(50, 70);
        var hoverColor = new Color(0xffff0000);
        var draggedColor = new Color(0xff0000ff);

        Widget BuildApp(bool enabled = true)
        {
            return new MaterialApp(
                home: new Directionality(
                    TextDirection.Ltr,
                    new StatefulBuilder((context, setState) => new MaterialWidget(
                        child: new Center(
                            child: new RangeSlider(
                                values: values,
                                max: 100.0,
                                overlayColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                                {
                                    if (states.Contains(WidgetState.Hovered))
                                    {
                                        return hoverColor;
                                    }

                                    if (states.Contains(WidgetState.Dragged))
                                    {
                                        return draggedColor;
                                    }

                                    return null;
                                }),
                                onChanged: enabled
                                    ? newValues => setState(() => values = newValues)
                                    : null,
                                onChangeStart: enabled ? _ => { } : null,
                                onChangeEnd: enabled ? _ => { } : null))))));
        }

        tester.PumpWidget(BuildApp());

        // RangeSlider does not have overlay when enabled and not hovered.
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: hoverColor));

        // Hover on the range slider but outside the thumb.
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetTopLeft(SliderElement(tester)));

        tester.PumpWidget(BuildApp());
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: hoverColor));

        // Hover on the thumb.
        gesture.MoveTo(tester.GetCenter(SliderElement(tester)));
        tester.PumpAndSettle();
        PaintAssert.Paints(MaterialOf(tester), PaintPattern.Paints.Circle(color: hoverColor));

        // Hover on the slider but outside the thumb.
        gesture.MoveTo(tester.GetBottomRight(SliderElement(tester)));
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: hoverColor));

        // Reset range slider values.
        values = new RangeValues(50, 70);

        // RangeSlider does not have overlay when enabled and not dragged.
        tester.PumpWidget(BuildApp());
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: draggedColor));

        // Start dragging.
        TestGesture drag = tester.StartGesture(tester.GetCenter(SliderElement(tester)), PointerDeviceKind.Touch);
        tester.Pump(GestureConstants.PressTimeout);

        // Less than configured touch slop, more than default touch slop.
        drag.MoveBy(new Vector(19.0, 0));
        tester.Pump();

        // RangeSlider has overlay when enabled and dragged.
        PaintAssert.Paints(MaterialOf(tester), PaintPattern.Paints.Circle(color: draggedColor));

        // Stop dragging.
        drag.Up();
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: draggedColor));
    }

    // Flutter: "RangeSlider onChangeStart and onChangeEnd fire once"
    [Fact]
    public void OnChangeStartAndOnChangeEndFireOnce()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);

        // Regression test for https://github.com/flutter/flutter/issues/128433
        int startFired = 0;
        int endFired = 0;
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new MaterialWidget(
                    child: new Center(
                        child: new GestureDetector(
                            onHorizontalDragUpdate: _ => { },
                            child: new RangeSlider(
                                values: new RangeValues(40, 80),
                                max: 100,
                                onChanged: _ => { },
                                onChangeStart: _ => startFired += 1,
                                onChangeEnd: _ => endFired += 1)))))));

        tester.TimedDragFrom(
            tester.GetTopLeft(SliderElement(tester)),
            new Vector(100.0, 0.0),
            TimeSpan.FromMilliseconds(500));

        Assert.Equal(1, startFired);
        Assert.Equal(1, endFired);
    }

    // Flutter: "RangeSlider in a ListView does not throw an exception"
    [Fact]
    public void InAListViewDoesNotThrowAnException()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);

        // Regression test for https://github.com/flutter/flutter/issues/126648
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new MaterialWidget(
                    child: new ListView(
                        children:
                        [
                            new SizedBox(height: 600, child: new Placeholder()),
                            new RangeSlider(
                                values: new RangeValues(40, 80),
                                max: 100,
                                onChanged: _ => { }),
                        ])))));

        // No exception should be thrown.
        Assert.Null(tester.TakeException());
    }

    // This is a regression test for https://github.com/flutter/flutter/issues/141953.
    // Flutter: "Semantic nodes do not throw an error after clearSemantics" (semanticsEnabled: false)
    [Fact]
    public void SemanticNodesDoNotThrowAnErrorAfterClearSemantics()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var semantics = new SemanticsHandleScope(tester);

        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new MaterialApp(
                home: new Scaffold(
                    body: new RangeSlider(
                        values: new RangeValues(40, 80),
                        max: 100,
                        onChanged: _ => { })))));

        // Dispose the semantics to trigger clearSemantics.
        semantics.Dispose();
        PumpAndSettle(tester);

        Assert.Null(tester.TakeException());

        // Initialize the semantics again.
        semantics = new SemanticsHandleScope(tester);
        PumpAndSettle(tester);

        Assert.Null(tester.TakeException());

        semantics.Dispose();
    }

    // Flutter: "Value indicator appears when it should"
    [Fact]
    public void ValueIndicatorAppearsWhenItShould()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var baseTheme = new ThemeData(platform: TargetPlatform.Android, primarySwatch: MaterialColors.Blue);
        SliderThemeData theme = baseTheme.SliderTheme.CopyWith(valueIndicatorColor: MaterialColors.Red);
        var value = new RangeValues(1, 5);

        Widget BuildApp(SliderThemeData sliderTheme, int? divisions = null, bool enabled = true)
        {
            Action<RangeValues>? onChanged = enabled ? d => value = d : null;
            return new MaterialApp(
                home: new MaterialWidget(
                    child: new Center(
                        child: new Theme(
                            data: baseTheme,
                            child: new SliderTheme(
                                data: sliderTheme,
                                child: new RangeSlider(
                                    values: value,
                                    max: 10,
                                    labels: new RangeLabels(DartDouble(value.Start), DartDouble(value.End)),
                                    divisions: divisions,
                                    onChanged: onChanged))))));
        }

        void ExpectValueIndicator(
            bool isVisible,
            SliderThemeData theme,
            int? divisions = null,
            bool enabled = true,
            bool dragged = true)
        {
            // Discrete enabled widget.
            tester.PumpWidget(BuildApp(sliderTheme: theme, divisions: divisions, enabled: enabled));
            Point center = tester.GetCenter(SliderElement(tester));
            TestGesture? gesture = null;
            if (dragged)
            {
                gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
            }

            // Wait for value indicator animation to finish.
            tester.PumpAndSettle();

            // _RenderValueIndicator is the last render object in the tree.
            RenderObject valueIndicatorBox = AllRenderObjects(tester).Last();
            PaintPattern pattern = PaintPattern.Paints
                .Path(color: theme.ValueIndicatorColor)
                .Paragraph();
            if (isVisible)
            {
                PaintAssert.Paints(valueIndicatorBox, pattern);
            }
            else
            {
                PaintAssert.DoesNotPaint(valueIndicatorBox, pattern);
            }

            if (dragged)
            {
                gesture!.Up();
            }
        }

        // Default (showValueIndicator set to onlyForDiscrete).
        ExpectValueIndicator(isVisible: true, theme: theme, divisions: 10);
        ExpectValueIndicator(isVisible: false, theme: theme, divisions: 10, enabled: false);
        ExpectValueIndicator(isVisible: false, theme: theme);
        ExpectValueIndicator(isVisible: false, theme: theme, enabled: false);
        ExpectValueIndicator(isVisible: false, theme: theme, divisions: 10, dragged: false);
        ExpectValueIndicator(isVisible: false, theme: theme, divisions: 3, enabled: false, dragged: false);
        ExpectValueIndicator(isVisible: false, theme: theme, dragged: false);
        ExpectValueIndicator(isVisible: false, theme: theme, enabled: false, dragged: false);

        // With showValueIndicator set to onlyForContinuous.
        theme = theme.CopyWith(showValueIndicator: ShowValueIndicator.OnlyForContinuous);
        ExpectValueIndicator(isVisible: false, theme: theme, divisions: 10);
        ExpectValueIndicator(isVisible: false, theme: theme, divisions: 10, enabled: false);
        ExpectValueIndicator(isVisible: true, theme: theme);
        ExpectValueIndicator(isVisible: false, theme: theme, enabled: false);
        ExpectValueIndicator(isVisible: false, theme: theme, divisions: 10, dragged: false);
        ExpectValueIndicator(isVisible: false, theme: theme, divisions: 3, enabled: false, dragged: false);
        ExpectValueIndicator(isVisible: false, theme: theme, dragged: false);
        ExpectValueIndicator(isVisible: false, theme: theme, enabled: false, dragged: false);

        // discrete enabled widget with showValueIndicator set to onDrag.
        theme = theme.CopyWith(showValueIndicator: ShowValueIndicator.OnDrag);
        ExpectValueIndicator(isVisible: true, theme: theme, divisions: 10);
        ExpectValueIndicator(isVisible: false, theme: theme, divisions: 10, enabled: false);
        ExpectValueIndicator(isVisible: true, theme: theme);
        ExpectValueIndicator(isVisible: false, theme: theme, enabled: false);
        ExpectValueIndicator(isVisible: false, theme: theme, divisions: 10, dragged: false);
        ExpectValueIndicator(isVisible: false, theme: theme, divisions: 3, enabled: false, dragged: false);
        ExpectValueIndicator(isVisible: false, theme: theme, dragged: false);
        ExpectValueIndicator(isVisible: false, theme: theme, enabled: false, dragged: false);

        // discrete enabled widget with showValueIndicator set to never.
        theme = theme.CopyWith(showValueIndicator: ShowValueIndicator.Never);
        ExpectValueIndicator(isVisible: false, theme: theme, divisions: 10);
        ExpectValueIndicator(isVisible: false, theme: theme, divisions: 10, enabled: false);
        ExpectValueIndicator(isVisible: false, theme: theme);
        ExpectValueIndicator(isVisible: false, theme: theme, enabled: false);
        ExpectValueIndicator(isVisible: false, theme: theme, divisions: 10, dragged: false);
        ExpectValueIndicator(isVisible: false, theme: theme, divisions: 3, enabled: false, dragged: false);
        ExpectValueIndicator(isVisible: false, theme: theme, dragged: false);
        ExpectValueIndicator(isVisible: false, theme: theme, enabled: false, dragged: false);

        // discrete enabled widget with showValueIndicator set to alwaysVisible.
        theme = theme.CopyWith(showValueIndicator: ShowValueIndicator.AlwaysVisible);
        ExpectValueIndicator(isVisible: true, theme: theme, divisions: 3);
        ExpectValueIndicator(isVisible: true, theme: theme, divisions: 3, enabled: false);
        ExpectValueIndicator(isVisible: true, theme: theme);
        ExpectValueIndicator(isVisible: true, theme: theme, enabled: false);
        ExpectValueIndicator(isVisible: true, theme: theme, divisions: 3, dragged: false);
        ExpectValueIndicator(isVisible: true, theme: theme, divisions: 3, enabled: false, dragged: false);
        ExpectValueIndicator(isVisible: true, theme: theme, dragged: false);
        ExpectValueIndicator(isVisible: true, theme: theme, enabled: false, dragged: false);
    }

    // Flutter: "RangeSlider overlay appears correctly for specific thumb interactions"
    [Fact]
    public void OverlayAppearsCorrectlyForSpecificThumbInteractions()
    {
        bool previousDisableShadows = RenderingDebug.DisableShadows;
        RenderingDebug.DisableShadows = true; // flutter_test's `debugDisableShadows` default.
        try
        {
            using var tester = new FrameworkDartTester(fakeGestureTimers: true);
            FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
            var values = new RangeValues(50, 70);
            var hoverColor = new Color(0xffff0000);
            var dragColor = new Color(0xff0000ff);

            Widget BuildApp()
            {
                return new MaterialApp(
                    home: new Directionality(
                        TextDirection.Ltr,
                        new StatefulBuilder((context, setState) => new MaterialWidget(
                            child: new Center(
                                child: new RangeSlider(
                                    values: values,
                                    max: 100.0,
                                    overlayColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                                    {
                                        if (states.Contains(WidgetState.Hovered))
                                        {
                                            return hoverColor;
                                        }

                                        if (states.Contains(WidgetState.Dragged))
                                        {
                                            return dragColor;
                                        }

                                        return null;
                                    }),
                                    onChanged: newValues => setState(() => values = newValues),
                                    onChangeStart: _ => { },
                                    onChangeEnd: _ => { }))))));
            }

            tester.PumpWidget(BuildApp());
            tester.PumpAndSettle();

            // Initial state - no overlay.
            PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: dragColor));

            // Drag start thumb to left.
            Point topThumbLocation = tester.GetCenter(SliderElement(tester));
            TestGesture dragStartThumb = tester.StartGesture(topThumbLocation, PointerDeviceKind.Touch);
            tester.Pump(GestureConstants.PressTimeout);
            dragStartThumb.MoveBy(new Vector(-20.0, 0));
            tester.PumpAndSettle();

            // Verify overlay is visible and shadow is visible on single thumb.
            PaintAssert.Paints(
                MaterialOf(tester),
                PaintPattern.Paints
                    .Circle(color: dragColor)
                    .Path(color: MaterialColors.Black, style: PaintingStyle.Stroke, strokeWidth: 2.0)
                    .Path(color: MaterialColors.Black, style: PaintingStyle.Stroke, strokeWidth: 12.0));

            // Move back and release.
            dragStartThumb.MoveBy(new Vector(20.0, 0));
            dragStartThumb.Up();
            tester.PumpAndSettle();

            // Verify overlay and shadow disappears
            PaintAssert.DoesNotPaint(
                MaterialOf(tester),
                PaintPattern.Paints
                    .Circle(color: dragColor)
                    .Path(color: MaterialColors.Black, style: PaintingStyle.Stroke, strokeWidth: 2.0)
                    .Path(color: MaterialColors.Black, style: PaintingStyle.Stroke, strokeWidth: 2.0));

            // Drag end thumb and return to original position.
            Point bottomThumbLocation = tester.GetCenter(SliderElement(tester)) + new Vector(220.0, 0.0);
            TestGesture dragEndThumb = tester.StartGesture(bottomThumbLocation, PointerDeviceKind.Touch);
            tester.Pump(GestureConstants.PressTimeout);
            dragEndThumb.MoveBy(new Vector(20.0, 0));
            tester.Pump(GestureConstants.PressTimeout);
            dragEndThumb.MoveBy(new Vector(-20.0, 0));
            dragEndThumb.Up();
            tester.PumpAndSettle();

            // Verify overlay disappears.
            PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: dragColor));

            // Hover on start thumb.
            TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
            gesture.AddPointer();
            gesture.MoveTo(topThumbLocation);
            tester.PumpAndSettle();

            // Verify overlay appears only for start thumb and no shadow is visible.
            PaintAssert.Paints(
                MaterialOf(tester),
                PaintPattern.Paints
                    .Circle(color: hoverColor)
                    .Path(color: MaterialColors.Black, style: PaintingStyle.Stroke, strokeWidth: 2.0)
                    .Path(color: MaterialColors.Black, style: PaintingStyle.Stroke, strokeWidth: 2.0));

            RenderObject renderObject = SliderElement(tester).FindRenderObject()!;
            // 2 thumbs, 1 overlay for hover, and 1 overlay for focus.
            Assert.Equal(4, PaintRecording.Record(renderObject).CountCalls("drawCircle"));

            // Move away from thumb
            gesture.MoveTo(TopRight(tester, SliderElement(tester)));
            tester.PumpAndSettle();

            // Verify overlay disappears
            PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: hoverColor));
        }
        finally
        {
            RenderingDebug.DisableShadows = previousDisableShadows;
        }
    }

    // Flutter: "RangeSlider.padding can override the default RangeSlider padding"
    [Fact]
    public void PaddingCanOverrideTheDefaultRangeSliderPadding()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);

        Widget BuildRangeSlider(EdgeInsetsGeometry? padding = null)
        {
            return new MaterialApp(
                home: new MaterialWidget(
                    child: new Center(
                        child: new IntrinsicHeight(
                            child: new RangeSlider(
                                padding: padding,
                                values: new RangeValues(0, 1.0),
                                onChanged: _ => { })))));
        }

        RenderBox SliderRenderBox() => AllRenderObjects(tester).OfType<RenderRangeSlider>().First();

        // Test RangeSlider height and tracks spacing with zero padding.
        tester.PumpWidget(BuildRangeSlider(padding: EdgeInsetsGeometry.Zero));
        tester.PumpAndSettle();

        // The height equals to the default thumb height.
        Assert.Equal(new Size(800, 20), SliderRenderBox().Size);
        PaintAssert.Paints(
            SliderElement(tester).FindRenderObject()!,
            PaintPattern.Paints
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    10.0,
                    8.0,
                    10.0,
                    12.0,
                    topLeft: Radius.Circular(2.0),
                    bottomLeft: Radius.Circular(2.0)))
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    790.0,
                    8.0,
                    790.0,
                    12.0,
                    topRight: Radius.Circular(2.0),
                    bottomRight: Radius.Circular(2.0)))
                // Active track.
                .RRect(rrect: RRect.FromLTRBR(8.0, 7.0, 792.0, 13.0, Radius.Circular(2.0))));

        // Test RangeSlider height and tracks spacing with directional padding.
        const double startPadding = 100;
        const double endPadding = 20;
        tester.PumpWidget(BuildRangeSlider(
            padding: EdgeInsetsGeometry.DirectionalOnly(start: startPadding, end: endPadding)));
        tester.PumpAndSettle();

        Assert.Equal(new Size(800 - startPadding - endPadding, 20), SliderRenderBox().Size);
        PaintAssert.Paints(
            SliderElement(tester).FindRenderObject()!,
            PaintPattern.Paints
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    10.0,
                    8.0,
                    10.0,
                    12.0,
                    topLeft: Radius.Circular(2.0),
                    bottomLeft: Radius.Circular(2.0)))
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    670.0,
                    8.0,
                    670.0,
                    12.0,
                    topRight: Radius.Circular(2.0),
                    bottomRight: Radius.Circular(2.0)))
                // Active track.
                .RRect(rrect: RRect.FromLTRBR(8.0, 7.0, 672.0, 13.0, Radius.Circular(2.0))));

        // Test RangeSlider height and tracks spacing with top and bottom padding.
        const double topPadding = 100;
        const double bottomPadding = 20;
        const double trackHeight = 20;
        tester.PumpWidget(BuildRangeSlider(
            padding: EdgeInsetsGeometry.DirectionalOnly(top: topPadding, bottom: bottomPadding)));
        tester.PumpAndSettle();

        Assert.Equal(new Size(800, topPadding + trackHeight + bottomPadding), tester.GetSize(SliderElement(tester)));
        Assert.Equal(new Size(800, 20), SliderRenderBox().Size);
        PaintAssert.Paints(
            SliderElement(tester).FindRenderObject()!,
            PaintPattern.Paints
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    10.0,
                    8.0,
                    10.0,
                    12.0,
                    topLeft: Radius.Circular(2.0),
                    bottomLeft: Radius.Circular(2.0)))
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    790.0,
                    8.0,
                    790.0,
                    12.0,
                    topRight: Radius.Circular(2.0),
                    bottomRight: Radius.Circular(2.0)))
                // Active track.
                .RRect(rrect: RRect.FromLTRBR(8.0, 7.0, 792.0, 13.0, Radius.Circular(2.0))));
    }

    // Regression test for hhttps://github.com/flutter/flutter/issues/161805
    // Flutter: "Discrete RangeSlider does not apply thumb padding in a non-rounded track shape"
    [Fact]
    public void DiscreteRangeSliderDoesNotApplyThumbPaddingInANonRoundedTrackShape()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);

        // The default track left and right padding.
        const double sliderPadding = 24.0;
        var theme = new ThemeData(
            sliderTheme: new SliderThemeData(
                // Thumb padding is applied based on the track height.
                TrackHeight: 100,
                RangeTrackShape: new RectangularRangeSliderTrackShape()));

        tester.PumpWidget(new MaterialApp(
            theme: theme,
            home: new MaterialWidget(
                child: new SizedBox(
                    width: 300,
                    child: new RangeSlider(
                        values: new RangeValues(0, 100),
                        max: 100,
                        divisions: 100,
                        onChanged: _ => { })))));

        RenderObject material = MaterialOf(tester);

        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Start thumb.
                .Circle(x: sliderPadding, y: 300.0, color: theme.ColorScheme.Primary)
                // End thumb.
                .Circle(x: 800.0 - sliderPadding, y: 300.0, color: theme.ColorScheme.Primary));
    }

    // Flutter: "Default RangeSlider when year2023 is false"
    [Fact]
    public void DefaultRangeSliderWhenYear2023IsFalse()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var theme = new ThemeData();
        ColorScheme colorScheme = theme.ColorScheme;
        Color activeTrackColor = colorScheme.Primary;
        Color inactiveTrackColor = colorScheme.SecondaryContainer;
        Color disabledActiveTrackColor = colorScheme.OnSurface.WithOpacity(0.38);
        Color disabledInactiveTrackColor = colorScheme.OnSurface.WithOpacity(0.12);
        Color activeTickMarkColor = colorScheme.OnPrimary;
        Color inactiveTickMarkColor = colorScheme.OnSecondaryContainer;
        Color disabledActiveTickMarkColor = colorScheme.OnInverseSurface;
        Color disabledInactiveTickMarkColor = colorScheme.OnSurface;
        Color thumbColor = colorScheme.Primary;
        Color disabledThumbColor = colorScheme.OnSurface.WithOpacity(0.38);
        Color valueIndicatorColor = colorScheme.InverseSurface;
        var values = new RangeValues(25.0, 75.0);

        Widget BuildApp(int? divisions = null, bool enabled = true)
        {
            Action<RangeValues>? onChanged = !enabled ? null : newValues => values = newValues;
            return new MaterialApp(
                home: new MaterialWidget(
                    child: new Center(
                        child: new Theme(
                            data: theme,
                            child: new RangeSlider(
#pragma warning disable CS0618 // Mirrors Flutter's deprecated `year2023` flag.
                                year2023: false,
#pragma warning restore CS0618
                                values: values,
                                max: 100,
                                labels: new RangeLabels(DartRound(values.Start), DartRound(values.End)),
                                divisions: divisions,
                                onChanged: onChanged)))));
        }

        tester.PumpWidget(BuildApp());

        RenderObject material = MaterialOf(tester);

        // Test default track shape.
        Radius trackOuterCornerRadius = Radius.Circular(8.0);
        Radius trackInnerCornerRadius = Radius.Circular(2.0);
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Inactive track.
                .RRect(
                    rrect: RRect.FromLTRBAndCorners(
                        24.0,
                        292.0,
                        206.0,
                        308.0,
                        topLeft: trackOuterCornerRadius,
                        topRight: trackInnerCornerRadius,
                        bottomRight: trackInnerCornerRadius,
                        bottomLeft: trackOuterCornerRadius),
                    color: inactiveTrackColor)
                // Inactive track.
                .RRect(
                    rrect: RRect.FromLTRBAndCorners(
                        594.0,
                        292.0,
                        776.0,
                        308.0,
                        topLeft: trackInnerCornerRadius,
                        topRight: trackOuterCornerRadius,
                        bottomRight: trackOuterCornerRadius,
                        bottomLeft: trackInnerCornerRadius),
                    color: inactiveTrackColor)
                // Active track.
                .RRect(
                    rrect: RRect.FromLTRBR(218.0, 292.0, 582.0, 308.0, trackInnerCornerRadius),
                    color: activeTrackColor));

        // Test default colors for enabled slider.
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .Circle()
                .Circle()
                .RRect(color: thumbColor)
                .RRect(color: thumbColor));
        PaintAssert.DoesNotPaint(
            material,
            PaintPattern.Paints
                .Circle()
                .Circle()
                .RRect(color: disabledThumbColor)
                .RRect(color: disabledThumbColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledActiveTrackColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledInactiveTrackColor));

        // Test defaults colors for discrete slider.
        tester.PumpWidget(BuildApp(divisions: 4));
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .RRect(color: inactiveTrackColor)
                .RRect(color: inactiveTrackColor)
                .RRect(color: activeTrackColor)
                .Circle(color: inactiveTickMarkColor)
                .Circle(color: activeTickMarkColor)
                .Circle(color: inactiveTickMarkColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledThumbColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledActiveTrackColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledInactiveTrackColor));

        // Test defaults colors for disabled slider.
        tester.PumpWidget(BuildApp(enabled: false));
        tester.PumpAndSettle();
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .RRect(color: disabledInactiveTrackColor)
                .RRect(color: disabledInactiveTrackColor)
                .RRect(color: disabledActiveTrackColor)
                .RRect(color: disabledThumbColor)
                .RRect(color: disabledThumbColor));
        PaintAssert.DoesNotPaint(
            material,
            PaintPattern.Paints
                .RRect(color: thumbColor)
                .RRect(color: thumbColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: activeTrackColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: inactiveTrackColor));

        // Test defaults colors for disabled discrete slider.
        tester.PumpWidget(BuildApp(divisions: 4, enabled: false));
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .RRect(color: disabledInactiveTrackColor)
                .RRect(color: disabledInactiveTrackColor)
                .RRect(color: disabledActiveTrackColor)
                .Circle(color: disabledInactiveTickMarkColor)
                .Circle(color: disabledActiveTickMarkColor)
                .Circle(color: disabledInactiveTickMarkColor)
                .RRect(color: disabledThumbColor)
                .RRect(color: disabledThumbColor));
        PaintAssert.DoesNotPaint(
            material,
            PaintPattern.Paints
                .RRect(color: thumbColor)
                .RRect(color: thumbColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: activeTrackColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: inactiveTrackColor));

        tester.PumpWidget(BuildApp(divisions: 4));
        tester.PumpAndSettle();

        Point topLeft = tester.GetTopLeft(SliderElement(tester));
        TestGesture gesture = tester.StartGesture(topLeft, PointerDeviceKind.Touch);
        // Wait for value indicator animation to finish.
        tester.PumpAndSettle();

        RenderObject valueIndicatorBox = tester.ElementOfType<Overlay>().FindRenderObject()!;
        PaintAssert.Paints(
            valueIndicatorBox,
            PaintPattern.Paints
                .Scale()
                .RRect(color: valueIndicatorColor));
        gesture.Up();
    }

    // Flutter: "RangeSlider value indicator text when year2023 is false"
    [Fact]
    public void RangeSliderValueIndicatorTextWhenYear2023IsFalse()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var values = new RangeValues(25.0, 75.0);
        var log = new List<InlineSpan>();
        var loggingValueIndicatorShape = new LoggingRangeSliderValueIndicatorShape(log);
        var theme = new ThemeData(
            sliderTheme: new SliderThemeData(RangeValueIndicatorShape: loggingValueIndicatorShape));

        tester.PumpWidget(new MaterialApp(
            theme: theme,
            home: new MaterialWidget(
                child: new Center(
                    child: new RangeSlider(
#pragma warning disable CS0618 // Mirrors Flutter's deprecated `year2023` flag.
                        year2023: false,
#pragma warning restore CS0618
                        values: values,
                        max: 100,
                        labels: new RangeLabels(DartRound(values.Start), DartRound(values.End)),
                        divisions: 4,
                        onChanged: _ => { })))));
        Point topLeft = tester.GetTopLeft(SliderElement(tester));
        TestGesture gesture = tester.StartGesture(topLeft, PointerDeviceKind.Touch);
        tester.PumpAndSettle();

        Assert.Equal("25", log.Last().ToPlainText());
        Assert.Equal(14.0, log.Last().Style!.FontSize);
        Assert.Equal(theme.ColorScheme.OnInverseSurface, log.Last().Style!.Color);

        gesture.Up();
        tester.PumpAndSettle();
    }

    // Flutter: "RangeSlider supports DropRangeSliderValueIndicatorShape"
    [Fact]
    public void RangeSliderSupportsDropRangeSliderValueIndicatorShape()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var values = new RangeValues(25.0, 75.0);
        var valueIndicatorColor = new Color(0XFFFF0000);
        var theme = new ThemeData(
            sliderTheme: new SliderThemeData(
                RangeValueIndicatorShape: new DropRangeSliderValueIndicatorShape(),
                ValueIndicatorColor: valueIndicatorColor));

        tester.PumpWidget(new MaterialApp(
            theme: theme,
            home: new MaterialWidget(
                child: new Center(
                    child: new RangeSlider(
#pragma warning disable CS0618 // Mirrors Flutter's deprecated `year2023` flag.
                        year2023: false,
#pragma warning restore CS0618
                        values: values,
                        max: 100,
                        labels: new RangeLabels(DartRound(values.Start), DartRound(values.End)),
                        divisions: 4,
                        onChanged: _ => { })))));
        Point topLeft = tester.GetTopLeft(SliderElement(tester));
        TestGesture gesture = tester.StartGesture(topLeft, PointerDeviceKind.Touch);
        tester.PumpAndSettle();

        RenderObject valueIndicatorBox = tester.ElementOfType<Overlay>().FindRenderObject()!;
        PaintAssert.Paints(valueIndicatorBox, PaintPattern.Paints.Path(color: valueIndicatorColor));

        gesture.Up();
        tester.PumpAndSettle();
    }

    // Flutter: "Value indicator appears on tap"
    [Fact]
    public void ValueIndicatorAppearsOnTap()
    {
        bool previousDisableShadows = RenderingDebug.DisableShadows;
        RenderingDebug.DisableShadows = true; // flutter_test's `debugDisableShadows` default.
        try
        {
            using var tester = new FrameworkDartTester(fakeGestureTimers: true);
            ThemeData theme = BuildTheme();
            SliderThemeData sliderTheme = theme.SliderTheme;
            var discreteValues = new RangeValues(20, 40);
            tester.PumpWidget(new MaterialApp(
                theme: theme,
                home: new MaterialWidget(
                    child: new RangeSlider(
                        labels: new RangeLabels(DartRound(discreteValues.Start), DartRound(discreteValues.End)),
                        values: discreteValues,
                        divisions: 5,
                        max: 100,
                        onChanged: _ => { }))));
            tester.Tap(SliderElement(tester));
            tester.PumpAndSettle();
            RenderObject valueIndicatorBox = tester.ElementOfType<Overlay>().FindRenderObject()!;
            PaintAssert.Paints(
                valueIndicatorBox,
                PaintPattern.Paints
                    .Path(color: MaterialColors.Black) // shadow
                    .Path(color: MaterialColors.Black) // shadow
                    .Path(color: sliderTheme.ValueIndicatorColor)
                    .Paragraph());
        }
        finally
        {
            RenderingDebug.DisableShadows = previousDisableShadows;
        }
    }

    // Flutter: "RangeSlider does not crash at zero area"
    [Fact]
    public void RangeSliderDoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new MaterialApp(
            home: new Scaffold(
                body: new Center(
                    child: SizedBox.Shrink(
                        child: new RangeSlider(values: new RangeValues(0, 1), onChanged: _ => { }))))));
        Assert.Equal(new Size(0, 0), tester.GetSize(SliderElement(tester)));
    }

    // Flutter: "RangeSlider taps should set focus on start/end thumbs"
    [Fact]
    public void RangeSliderTapsShouldSetFocusOnStartEndThumbs()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var values = new RangeValues(0.3, 0.7);

        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder((context, setState) => new RangeSlider(
                        values: values,
                        onChanged: newValues => setState(() => values = newValues),
                        onChangeStart: _ => { },
                        onChangeEnd: _ => { }))))));

        // Initial state: root focus scope has focus
        FocusNode? initialFocus = FocusManager.Instance.PrimaryFocus;
        Assert.NotNull(initialFocus);

        Point topLeft = tester.GetTopLeft(SliderElement(tester));
        Point bottomRight = tester.GetBottomRight(SliderElement(tester));

        // Tap near the start thumb (0.3)
        Point startThumbPos = topLeft + ((bottomRight - topLeft) * 0.3);
        TapAt(tester, startThumbPos);
        tester.Pump();

        // Verify focus changed to start thumb
        FocusNode startFocusNode = SliderState(tester).StartFocusNode;
        Assert.True(startFocusNode.HasFocus, "Start thumb should have focus after tap");
        Assert.Same(startFocusNode, FocusManager.Instance.PrimaryFocus);

        // Reset focus
        FocusManager.Instance.PrimaryFocus?.Unfocus();
        tester.Pump();

        // Tap near the end thumb (0.7)
        Point endThumbPos = topLeft + ((bottomRight - topLeft) * 0.7);
        TapAt(tester, endThumbPos);
        tester.Pump();

        // Verify focus changed to end thumb
        FocusNode endFocusNode = SliderState(tester).EndFocusNode;
        Assert.True(endFocusNode.HasFocus, "End thumb should have focus after tap");
        Assert.Same(endFocusNode, FocusManager.Instance.PrimaryFocus);
    }

    // Flutter: "RangeSlider drag should set focus on start/end thumbs"
    [Fact]
    public void RangeSliderDragShouldSetFocusOnStartEndThumbs()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var values = new RangeValues(0.3, 0.7);

        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder((context, setState) => new RangeSlider(
                        values: values,
                        onChanged: newValues => setState(() => values = newValues)))))));

        // Initial state
        FocusNode? initialFocus = FocusManager.Instance.PrimaryFocus;
        Assert.NotNull(initialFocus);

        Point topLeft = tester.GetTopLeft(SliderElement(tester));
        Point bottomRight = tester.GetBottomRight(SliderElement(tester));

        // Drag start thumb
        Point startThumbPos = topLeft + ((bottomRight - topLeft) * 0.3);
        TestGesture gesture = tester.StartGesture(startThumbPos, PointerDeviceKind.Touch);
        tester.Pump();

        // Verify focus on start drag
        FocusNode startFocusNode = SliderState(tester).StartFocusNode;
        Assert.True(startFocusNode.HasFocus, "Start thumb should have focus on drag start");
        Assert.Same(startFocusNode, FocusManager.Instance.PrimaryFocus);

        gesture.MoveBy(new Vector(10, 0));
        gesture.Up();
        tester.Pump();

        // Reset focus
        FocusManager.Instance.PrimaryFocus?.Unfocus();
        tester.Pump();

        // Drag end thumb
        Point endThumbPos = topLeft + ((bottomRight - topLeft) * 0.7);
        TestGesture endGesture = tester.StartGesture(endThumbPos, PointerDeviceKind.Touch);
        tester.Pump();

        // Verify focus on end drag
        FocusNode endFocusNode = SliderState(tester).EndFocusNode;
        Assert.True(endFocusNode.HasFocus, "End thumb should have focus on drag start");
        Assert.Same(endFocusNode, FocusManager.Instance.PrimaryFocus);

        endGesture.MoveBy(new Vector(-10, 0));
        endGesture.Up();
        tester.Pump();
    }

    // Flutter: "RangeSlider tap start thumb then tab should focus end thumb"
    [Fact]
    public void RangeSliderTapStartThumbThenTabShouldFocusEndThumb()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var values = new RangeValues(0.3, 0.7);

        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder((context, setState) => new RangeSlider(
                        values: values,
                        onChanged: newValues => setState(() => values = newValues)))))));

        Point topLeft = tester.GetTopLeft(SliderElement(tester));
        Point bottomRight = tester.GetBottomRight(SliderElement(tester));

        // Tap near the start thumb (0.3)
        Point startThumbPos = topLeft + ((bottomRight - topLeft) * 0.3);
        TapAt(tester, startThumbPos);
        tester.Pump();

        // Verify start thumb has focus
        FocusNode startFocusNode = SliderState(tester).StartFocusNode;
        Assert.True(startFocusNode.HasFocus, "Start thumb should have focus after tap");
        Assert.Same(startFocusNode, FocusManager.Instance.PrimaryFocus);

        // Press Tab
        KeySim.SendKeyCombination(LogicalKeyboardKey.Tab);
        tester.Pump();

        // Verify end thumb has focus
        FocusNode endFocusNode = SliderState(tester).EndFocusNode;
        Assert.True(endFocusNode.HasFocus, "End thumb should have focus after tab");
        Assert.Same(endFocusNode, FocusManager.Instance.PrimaryFocus);
    }

    // ---------------------------------------------------------------------------------------------
    // File-level helpers (range_slider_test.dart) and flutter_test stand-ins.
    // ---------------------------------------------------------------------------------------------

    // range_slider_test.dart's `buildTheme()`.
    private static ThemeData BuildTheme()
    {
        return new ThemeData(
            platform: TargetPlatform.Android,
            primarySwatch: MaterialColors.Blue,
            sliderTheme: new SliderThemeData(
                DisabledThumbColor: new Color(0xff000001),
                DisabledActiveTickMarkColor: new Color(0xff000002),
                DisabledActiveTrackColor: new Color(0xff000003),
                DisabledInactiveTickMarkColor: new Color(0xff000004),
                DisabledInactiveTrackColor: new Color(0xff000005),
                ActiveTrackColor: new Color(0xff000006),
                ActiveTickMarkColor: new Color(0xff000007),
                InactiveTrackColor: new Color(0xff000008),
                InactiveTickMarkColor: new Color(0xff000009),
                OverlayColor: new Color(0xff000010),
                ThumbColor: new Color(0xff000011),
                ValueIndicatorColor: new Color(0xff000012)));
    }

    // Dart's `double.toString()` for the label strings.
    private static string DartDouble(double value) =>
        value == Math.Floor(value) && Math.Abs(value) < 1e21
            ? value.ToString("0.0", CultureInfo.InvariantCulture)
            : value.ToString("R", CultureInfo.InvariantCulture);

    // Dart's `value.round().toString()`: rounds half away from zero.
    private static string DartRound(double value) =>
        ((long)Math.Round(value, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);

    // physics/utils.dart `nearEqual`.
    private static bool NearEqual(double a, double b, double epsilon) =>
        (a > b - epsilon && a < b + epsilon) || a == b;

    private static Rect Ltrb(double left, double top, double right, double bottom) =>
        new(new Point(left, top), new Point(right, bottom));

    private static Element SliderElement(FrameworkDartTester tester) => tester.ElementOfType<RangeSlider>();

    // `tester.state(find.byType(RangeSlider))`.
    private static RangeSlider.RangeSliderState SliderState(FrameworkDartTester tester) =>
        (RangeSlider.RangeSliderState)((StatefulElement)SliderElement(tester)).State;

    // `tester.getTopRight`.
    private static Point TopRight(FrameworkDartTester tester, Element element)
    {
        var box = (RenderBox)element.FindRenderObject()!;
        return box.LocalToGlobal(new Point(box.Size.Width, 0.0));
    }

    // `tester.tapAt`.
    private static void TapAt(FrameworkDartTester tester, Point location)
    {
        TestGesture gesture = tester.StartGesture(location, PointerDeviceKind.Touch);
        gesture.Up();
    }

    private static MouseCursor? ActiveCursor(TestGesture gesture) =>
        RendererBinding.Instance.MouseTracker.DebugDeviceActiveCursor(gesture.Pointer);

    // `Material.of(tester.element(find.byType(RangeSlider)))`: the nearest `_RenderInkFeatures`.
    private static RenderObject MaterialOf(FrameworkDartTester tester)
    {
        RenderObject? renderObject = SliderElement(tester).FindRenderObject();
        while (renderObject is not null and not RenderInkFeatures)
        {
            renderObject = renderObject.Parent;
        }

        return renderObject ?? throw new InvalidOperationException("No Material above the RangeSlider.");
    }

    // `tester.allRenderObjects`: depth-first, in `visitChildren` order.
    private static List<RenderObject> AllRenderObjects(FrameworkDartTester tester)
    {
        var result = new List<RenderObject>();
        void Visit(RenderObject renderObject)
        {
            result.Add(renderObject);
            renderObject.VisitChildren(Visit);
        }

        Visit(tester.RenderView);
        return result;
    }

    // flutter_test pumps flush semantics every frame; the C# tester's frame stops at composite.
    private static void PumpAndSettle(FrameworkDartTester tester)
    {
        tester.PumpAndSettle();
        tester.RenderView.Owner!.FlushSemantics();
    }

    // `tester.getSemantics(finder)`.
    private static SemanticsNode GetSemantics(Element element)
    {
        RenderObject? renderObject = element.FindRenderObject();
        SemanticsNode? result = renderObject?.DebugSemantics;
        while (renderObject != null && (result == null || result.IsMergedIntoParent))
        {
            renderObject = renderObject.Parent;
            result = renderObject?.DebugSemantics;
        }

        return result ?? throw new InvalidOperationException("No semantics node found.");
    }

    // The thumb rects of the three-level visitChildren walk in the ltr/rtl tests, rounded.
    private static List<Rect> RoundedThumbRects(SemanticsNode semanticsNode)
    {
        var rects = new List<Rect>();
        foreach (SemanticsNode level1 in semanticsNode.Children)
        {
            foreach (SemanticsNode level2 in level1.Children)
            {
                foreach (SemanticsNode node in level2.Children)
                {
                    // Round rect values to avoid floating point errors.
                    rects.Add(Ltrb(
                        Math.Round(node.Rect.Left, MidpointRounding.AwayFromZero),
                        Math.Round(node.Rect.Top, MidpointRounding.AwayFromZero),
                        Math.Round(node.Rect.Right, MidpointRounding.AwayFromZero),
                        Math.Round(node.Rect.Bottom, MidpointRounding.AwayFromZero)));
                }
            }
        }

        return rects;
    }

    // `matchesSemantics(scopesRoute: true, children: [matchesSemantics(children: [matchesSemantics(
    // children: [start, end])])])`.
    private static SemanticsMatcher RangeSliderSemantics(SemanticsMatcher start, SemanticsMatcher end) =>
        new(Flags: SemanticsFlags.ScopesRoute, Children: [new(Children: [new(Children: [start, end])])]);

    private static SemanticsMatcher ThumbSemantics(
        string value,
        string increasedValue,
        string decreasedValue,
        string? label = null,
        Rect? rect = null,
        bool isFocused = false)
    {
        SemanticsFlags flags = SemanticsFlags.IsEnabled
                               | SemanticsFlags.IsSlider
                               | SemanticsFlags.IsFocusable
                               | SemanticsFlags.HasEnabledState;
        if (isFocused)
        {
            flags |= SemanticsFlags.IsFocused;
        }

        return new SemanticsMatcher(
            Flags: flags,
            Actions: SemanticsActions.Increase | SemanticsActions.Decrease,
            Label: label,
            Value: value,
            IncreasedValue: increasedValue,
            DecreasedValue: decreasedValue,
            Rect: rect);
    }

    private static void ExpectSemantics(SemanticsNode node, SemanticsMatcher matcher)
    {
        if (matcher.Match(node, "node") is { } failure)
        {
            Assert.Fail($"{failure}\n{node.ToStringDeep()}");
        }
    }

    /// <summary>
    /// flutter_test's <c>matchesSemantics</c> for the arguments these tests pass: flags and actions are
    /// compared exactly (every unnamed one is expected false), the strings and rect only when given.
    /// </summary>
    private sealed record SemanticsMatcher(
        SemanticsFlags Flags = SemanticsFlags.None,
        SemanticsActions Actions = SemanticsActions.None,
        string? Label = null,
        string? Value = null,
        string? IncreasedValue = null,
        string? DecreasedValue = null,
        Rect? Rect = null,
        IReadOnlyList<SemanticsMatcher>? Children = null)
    {
        public string? Match(SemanticsNode node, string path)
        {
            SemanticsData data = node.GetSemanticsData();
            var errors = new StringBuilder();
            if (Label != null && Label != data.Label)
            {
                errors.Append($" label '{data.Label}' != '{Label}';");
            }

            if (Value != null && Value != data.Value)
            {
                errors.Append($" value '{data.Value}' != '{Value}';");
            }

            if (IncreasedValue != null && IncreasedValue != data.IncreasedValue)
            {
                errors.Append($" increasedValue '{data.IncreasedValue}' != '{IncreasedValue}';");
            }

            if (DecreasedValue != null && DecreasedValue != data.DecreasedValue)
            {
                errors.Append($" decreasedValue '{data.DecreasedValue}' != '{DecreasedValue}';");
            }

            if (Rect is { } rect
                && (rect.Left != data.Rect.Left
                    || rect.Top != data.Rect.Top
                    || rect.Right != data.Rect.Right
                    || rect.Bottom != data.Rect.Bottom))
            {
                errors.Append($" rect {data.Rect} != {rect};");
            }

            if (data.ValidationResult != SemanticsValidationResult.None)
            {
                errors.Append($" validationResult {data.ValidationResult};");
            }

            if (data.Actions != Actions)
            {
                errors.Append($" actions {data.Actions} != {Actions};");
            }

            if (data.Flags != Flags)
            {
                errors.Append($" flags {data.Flags} != {Flags};");
            }

            if (errors.Length > 0)
            {
                return $"{path}:{errors}";
            }

            if (Children != null)
            {
                IReadOnlyList<SemanticsNode> children = node.Children.ToList();
                if (children.Count != Children.Count)
                {
                    return $"{path}: expected {Children.Count} children, found {children.Count}";
                }

                for (int i = 0; i < children.Count; i++)
                {
                    if (Children[i].Match(children[i], $"{path}.children[{i}]") is { } failure)
                    {
                        return failure;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>semantics_tester.dart's <c>SemanticsTester</c>: keeps semantics on for the view.</summary>
    private sealed class SemanticsHandleScope : IDisposable
    {
        private SemanticsHandle? _handle;

        public SemanticsHandleScope(FrameworkDartTester tester)
        {
            PipelineOwner owner = tester.RenderView.Owner!;
            bool createsOwner = owner.SemanticsOwner is null;
            _handle = owner.EnsureSemantics();
            if (createsOwner)
            {
                // flutter_test's view clears every cached configuration before its initial semantics.
                tester.RenderView.ClearSemantics();
                tester.RenderView.ScheduleInitialSemantics();
            }
        }

        public void Dispose()
        {
            _handle?.Dispose();
            _handle = null;
        }
    }

    /// <summary>range_slider_test.dart's <c>LoggingRangeSliderValueIndicatorShape</c>: logs label text.</summary>
    private sealed class LoggingRangeSliderValueIndicatorShape(List<InlineSpan> logLabel)
        : RangeSliderValueIndicatorShape
    {
        public override Size GetPreferredSize(
            bool isEnabled,
            bool isDiscrete,
            TextPainter labelPainter,
            double textScaleFactor)
        {
            return new Size(10.0, 10.0);
        }

        public override void Paint(
            PaintingContext context,
            Point center,
            Animation<double> activationAnimation,
            Animation<double> enableAnimation,
            TextPainter labelPainter,
            RenderBox parentBox,
            SliderThemeData sliderTheme,
            bool? isDiscrete = null,
            bool? isOnTop = null,
            double? textScaleFactor = null,
            Size? sizeWithOverflow = null,
            TextDirection? textDirection = null,
            double? value = null,
            Thumb? thumb = null)
        {
            logLabel.Add(labelPainter.Text!);
        }
    }
}
