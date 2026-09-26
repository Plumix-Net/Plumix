// Dart parity source: material_ui/lib/src/slider_theme.dart
// Mirrors material-ui-src/test/slider_theme_test.dart (lines 1769-3719)

using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MouseCursor = Plumix.UI.MouseCursor;
using TextDirection = Plumix.UI.TextDirection;
using MaterialSurface = Plumix.Material.Material;

namespace Plumix.Tests;

#pragma warning disable CS0618 // ShowValueIndicator.Always is deprecated in Dart too; the tests still use it.

[Collection(SchedulerTestCollection.Name)]
public sealed class SliderThemeDartParityTestsB : IDisposable
{
    private readonly bool _oldDisableShadows = RenderingDebug.DisableShadows;

    public SliderThemeDartParityTestsB()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        RenderingDebug.DisableShadows = _oldDisableShadows;
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    // Flutter: "The slider can skip all component painting except the value indicator"
    [Fact]
    public void SliderCanSkipAllComponentPaintingExceptTheValueIndicator()
    {
        using FrameworkDartTester tester = NewTester();
        // Pump a slider with just a value indicator.
        tester.PumpWidget(BuildApp(
            new ThemeData().SliderTheme.CopyWith(
                trackHeight: 0,
                overlayShape: SliderComponentShape.NoOverlay,
                thumbShape: SliderComponentShape.NoThumb,
                tickMarkShape: SliderTickMarkShape.NoTickMark,
                showValueIndicator: ShowValueIndicator.Always),
            value: 0.5,
            divisions: 4));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());
        RenderObject valueIndicatorBox = OverlayBox(tester);

        // Tap the center of the track and wait for animations to finish.
        Point center = tester.GetCenter(tester.ElementOfType<Slider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
        tester.PumpAndSettle();

        // Only 1 value indicator.
        Assert.Equal(0, PaintRecording.Record(material).CountCalls("drawRect"));
        Assert.Equal(0, PaintRecording.Record(material).CountCalls("drawCircle"));
        Assert.Equal(1, PaintRecording.Record(valueIndicatorBox).CountCalls("drawPath"));

        gesture.Up();
    }

    // Flutter: "PaddleSliderValueIndicatorShape skips all painting at zero scale"
    [Fact]
    public void PaddleSliderValueIndicatorShapeSkipsAllPaintingAtZeroScale()
    {
        using FrameworkDartTester tester = NewTester();
        // Pump a slider with just a value indicator.
        tester.PumpWidget(BuildApp(
            new ThemeData().SliderTheme.CopyWith(
                trackHeight: 0,
                overlayShape: SliderComponentShape.NoOverlay,
                thumbShape: SliderComponentShape.NoThumb,
                tickMarkShape: SliderTickMarkShape.NoTickMark,
                showValueIndicator: ShowValueIndicator.Always,
                valueIndicatorShape: new PaddleSliderValueIndicatorShape()),
            value: 0.5,
            divisions: 4));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());
        RenderObject valueIndicatorBox = OverlayBox(tester);

        // Tap the center of the track to kick off the animation of the value indicator.
        Point center = tester.GetCenter(tester.ElementOfType<Slider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);

        // Nothing to paint at scale 0.
        tester.Pump();
        Assert.Equal(0, PaintRecording.Record(material).CountCalls("drawRect"));
        Assert.Equal(0, PaintRecording.Record(material).CountCalls("drawCircle"));
        Assert.Equal(0, PaintRecording.Record(valueIndicatorBox).CountCalls("drawPath"));

        // Painting a path for the value indicator.
        tester.Pump(TimeSpan.FromMilliseconds(16));
        Assert.Equal(1, PaintRecording.Record(valueIndicatorBox).CountCalls("drawPath"));

        gesture.Up();
    }

    // Flutter: "Default slider value indicator shape skips all painting at zero scale"
    [Fact]
    public void DefaultSliderValueIndicatorShapeSkipsAllPaintingAtZeroScale()
    {
        using FrameworkDartTester tester = NewTester();
        // Pump a slider with just a value indicator.
        tester.PumpWidget(BuildApp(
            new ThemeData().SliderTheme.CopyWith(
                trackHeight: 0,
                overlayShape: SliderComponentShape.NoOverlay,
                thumbShape: SliderComponentShape.NoThumb,
                tickMarkShape: SliderTickMarkShape.NoTickMark,
                showValueIndicator: ShowValueIndicator.OnDrag),
            value: 0.5,
            divisions: 4));

        RenderObject valueIndicatorBox = OverlayBox(tester);

        // Tap the center of the track to kick off the animation of the value indicator.
        Point center = tester.GetCenter(tester.ElementOfType<Slider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);

        // Nothing to paint at scale 0.
        tester.Pump();
        Assert.Equal(0, PaintRecording.Record(valueIndicatorBox).CountCalls("drawPath"));

        // Painting a path for the value indicator.
        tester.Pump(TimeSpan.FromMilliseconds(16));
        Assert.Equal(1, PaintRecording.Record(valueIndicatorBox).CountCalls("drawPath"));

        gesture.Up();
    }

    // Flutter: "Default paddle range slider value indicator shape draws correctly"
    [Fact]
    public void DefaultPaddleRangeSliderValueIndicatorShapeDrawsCorrectly()
    {
        using FrameworkDartTester tester = NewTester();
        RenderingDebug.DisableShadows = false;
        var theme = new ThemeData(platform: TargetPlatform.Android, primarySwatch: MaterialColors.Blue);
        SliderThemeData sliderTheme = theme.SliderTheme.CopyWith(
            thumbColor: MaterialColors.Red.Shade500,
            showValueIndicator: ShowValueIndicator.Always,
            rangeValueIndicatorShape: new PaddleRangeSliderValueIndicatorShape());

        tester.PumpWidget(BuildRangeApp(sliderTheme));

        RenderObject valueIndicatorBox = OverlayBox(tester);

        Point center = tester.GetCenter(tester.ElementOfType<RangeSlider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
        // Wait for value indicator animation to finish.
        tester.PumpAndSettle();
        PaintAssert.Paints(
            valueIndicatorBox,
            PaintPattern.Paints
                // physical model
                .RRect()
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    24.0,
                    298.0,
                    24.0,
                    302.0,
                    topLeft: Radius.Circular(2.0),
                    bottomLeft: Radius.Circular(2.0)))
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    24.0,
                    298.0,
                    776.0,
                    302.0,
                    topRight: Radius.Circular(2.0),
                    bottomRight: Radius.Circular(2.0)))
                .RRect(rrect: RRect.FromLTRBR(22.0, 297.0, 26.0, 303.0, Radius.Circular(2.0)))
                .Circle(x: 24.0, y: 300.0)
                .Shadow(elevation: 1.0)
                .Circle(x: 24.0, y: 300.0)
                .Shadow(elevation: 6.0)
                .Circle(x: 24.0, y: 300.0));

        gesture.Up();
    }

    // Flutter: "Default paddle range slider value indicator shape draws correctly with debugDisableShadows"
    [Fact]
    public void DefaultPaddleRangeSliderValueIndicatorShapeDrawsCorrectlyWithDebugDisableShadows()
    {
        using FrameworkDartTester tester = NewTester();
        RenderingDebug.DisableShadows = true;
        var theme = new ThemeData(platform: TargetPlatform.Android, primarySwatch: MaterialColors.Blue);
        SliderThemeData sliderTheme = theme.SliderTheme.CopyWith(
            thumbColor: MaterialColors.Red.Shade500,
            showValueIndicator: ShowValueIndicator.Always,
            rangeValueIndicatorShape: new PaddleRangeSliderValueIndicatorShape());

        tester.PumpWidget(BuildRangeApp(sliderTheme));

        RenderObject valueIndicatorBox = OverlayBox(tester);

        Point center = tester.GetCenter(tester.ElementOfType<RangeSlider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
        // Wait for value indicator animation to finish.
        tester.PumpAndSettle();
        PaintAssert.Paints(
            valueIndicatorBox,
            PaintPattern.Paints
                // physical model
                .RRect()
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    24.0,
                    298.0,
                    24.0,
                    302.0,
                    topLeft: Radius.Circular(2.0),
                    bottomLeft: Radius.Circular(2.0)))
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    24.0,
                    298.0,
                    776.0,
                    302.0,
                    topRight: Radius.Circular(2.0),
                    bottomRight: Radius.Circular(2.0)))
                .RRect(rrect: RRect.FromLTRBR(22.0, 297.0, 26.0, 303.0, Radius.Circular(2)))
                .Circle(x: 24.0, y: 300.0)
                .Path(strokeWidth: 1.0 * 2.0, color: MaterialColors.Black)
                .Circle(x: 24.0, y: 300.0)
                .Path(strokeWidth: 6.0 * 2.0, color: MaterialColors.Black)
                .Circle(x: 24.0, y: 300.0));

        gesture.Up();
    }

    // Flutter: "PaddleRangeSliderValueIndicatorShape skips all painting at zero scale"
    [Fact]
    public void PaddleRangeSliderValueIndicatorShapeSkipsAllPaintingAtZeroScale()
    {
        using FrameworkDartTester tester = NewTester();
        RenderingDebug.DisableShadows = false;
        // Pump a slider with just a value indicator.
        tester.PumpWidget(BuildRangeApp(
            new ThemeData().SliderTheme.CopyWith(
                trackHeight: 0,
                rangeValueIndicatorShape: new PaddleRangeSliderValueIndicatorShape()),
            values: new RangeValues(0, 0.5),
            divisions: 4));

        RenderObject valueIndicatorBox = OverlayBox(tester);

        // Tap the center of the track to kick off the animation of the value indicator.
        Point center = tester.GetCenter(tester.ElementOfType<RangeSlider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);

        // No value indicator path to paint at scale 0.
        tester.Pump();
        Assert.Equal(0, PaintRecording.Record(valueIndicatorBox).CountCalls("drawPath"));

        // Painting a path for each value indicator.
        tester.Pump(TimeSpan.FromMilliseconds(16));
        Assert.Equal(2, PaintRecording.Record(valueIndicatorBox).CountCalls("drawPath"));

        gesture.Up();
    }

    // Flutter: "Default range indicator shape skips all painting at zero scale"
    [Fact]
    public void DefaultRangeIndicatorShapeSkipsAllPaintingAtZeroScale()
    {
        using FrameworkDartTester tester = NewTester();
        RenderingDebug.DisableShadows = false;
        // Pump a slider with just a value indicator.
        tester.PumpWidget(BuildRangeApp(
            new ThemeData().SliderTheme.CopyWith(
                trackHeight: 0,
                overlayShape: SliderComponentShape.NoOverlay,
                thumbShape: SliderComponentShape.NoThumb,
                tickMarkShape: SliderTickMarkShape.NoTickMark,
                showValueIndicator: ShowValueIndicator.Always),
            values: new RangeValues(0, 0.5),
            divisions: 4));

        RenderObject valueIndicatorBox = OverlayBox(tester);

        // Tap the center of the track to kick off the animation of the value indicator.
        Point center = tester.GetCenter(tester.ElementOfType<RangeSlider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);

        // No value indicator path to paint at scale 0.
        tester.Pump();
        Assert.Equal(0, PaintRecording.Record(valueIndicatorBox).CountCalls("drawPath"));

        // Painting a path for each value indicator.
        tester.Pump(TimeSpan.FromMilliseconds(16));
        Assert.Equal(2, PaintRecording.Record(valueIndicatorBox).CountCalls("drawPath"));

        gesture.Up();
    }

    // Flutter: "activeTrackRadius is taken into account when painting the border of the active track"
    [Fact]
    public void ActiveTrackRadiusIsTakenIntoAccountWhenPaintingTheBorderOfTheActiveTrack()
    {
        using FrameworkDartTester tester = NewTester();
        tester.PumpWidget(BuildApp(
            new ThemeData().SliderTheme.CopyWith(
                trackShape: new RoundedRectSliderTrackShapeWithCustomAdditionalActiveTrackHeight(
                    additionalActiveTrackHeight: 10.0)),
            value: 0.5));
        tester.PumpAndSettle();
        Point center = tester.GetCenter(tester.ElementOfType<Slider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
        PaintAssert.Paints(
            FoundRenderObject<Slider>(tester),
            PaintPattern.Paints
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBR(398.0, 298.0, 776.0, 302.0, Radius.Circular(2.0)))
                // Active track.
                .RRect(rrect: RRect.FromLTRBR(24.0, 293.0, 402.0, 307.0, Radius.Circular(7.0))));

        // Finish gesture to release resources.
        gesture.Up();
        tester.PumpAndSettle();
    }

    // Flutter: "The mouse cursor is themeable"
    [Fact]
    public void TheMouseCursorIsThemeable()
    {
        using FrameworkDartTester tester = NewTester();
        tester.PumpWidget(BuildApp(
            new ThemeData().SliderTheme.CopyWith(
                mouseCursor: new WidgetStatePropertyAll<MouseCursor?>(SystemMouseCursors.Text))));

        tester.PumpAndSettle();
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(tester.ElementOfType<Slider>()));
        tester.PumpAndSettle();
        Assert.Equal(
            SystemMouseCursors.Text,
            RendererBinding.Instance.MouseTracker.DebugDeviceActiveCursor(gesture.Pointer));
        gesture.RemovePointer();
    }

    // Flutter: "SliderTheme.allowedInteraction is themeable"
    [Fact]
    public void SliderThemeAllowedInteractionIsThemeable()
    {
        using FrameworkDartTester tester = NewTester();
        double value = 0.0;

        Widget BuildTestApp(
            bool isAllowedInteractionInThemeNull = false,
            bool isAllowedInteractionInSliderNull = false)
        {
            return new MaterialApp(
                home: new Scaffold(
                    body: new Center(
                        child: new SliderTheme(
                            data: new ThemeData().SliderTheme.CopyWith(
                                allowedInteraction: isAllowedInteractionInThemeNull
                                    ? null
                                    : SliderInteraction.SlideOnly),
                            child: new StatefulBuilder(
                                builder: (_, setState) => new Slider(
                                    value: value,
                                    allowedInteraction: isAllowedInteractionInSliderNull
                                        ? null
                                        : SliderInteraction.TapOnly,
                                    onChanged: newValue => setState(() => value = newValue)))))));
        }

        TestGesture gesture = tester.CreateGesture();

        // when theme and parameter are specified, parameter is used [tapOnly].
        tester.PumpWidget(BuildTestApp());
        // tap is allowed.
        value = 0.0;
        gesture.Down(tester.GetCenter(tester.ElementOfType<Slider>()));
        tester.Pump();
        Assert.Equal(0.5, value); // changes
        gesture.Up();
        // slide isn't allowed.
        value = 0.0;
        gesture.Down(tester.GetCenter(tester.ElementOfType<Slider>()));
        tester.Pump();
        gesture.MoveBy(new Vector(50, 0));
        Assert.Equal(0.0, value); // no change
        gesture.Up();

        // when only parameter is specified, parameter is used [tapOnly].
        tester.PumpWidget(BuildTestApp(isAllowedInteractionInThemeNull: true));
        // tap is allowed.
        value = 0.0;
        gesture.Down(tester.GetCenter(tester.ElementOfType<Slider>()));
        tester.Pump();
        Assert.Equal(0.5, value); // changes
        gesture.Up();
        // slide isn't allowed.
        value = 0.0;
        gesture.Down(tester.GetCenter(tester.ElementOfType<Slider>()));
        tester.Pump();
        gesture.MoveBy(new Vector(50, 0));
        Assert.Equal(0.0, value); // no change
        gesture.Up();

        // when theme is specified but parameter is null, theme is used [slideOnly].
        tester.PumpWidget(BuildTestApp(isAllowedInteractionInSliderNull: true));
        // tap isn't allowed.
        value = 0.0;
        gesture.Down(tester.GetCenter(tester.ElementOfType<Slider>()));
        tester.Pump();
        Assert.Equal(0.0, value); // no change
        gesture.Up();
        // slide isn't allowed.
        value = 0.0;
        gesture.Down(tester.GetCenter(tester.ElementOfType<Slider>()));
        tester.Pump();
        gesture.MoveBy(new Vector(50, 0));
        Assert.True(value > 0.0); // changes
        gesture.Up();

        // when both theme and parameter are null, default is used [tapAndSlide].
        tester.PumpWidget(BuildTestApp(
            isAllowedInteractionInSliderNull: true,
            isAllowedInteractionInThemeNull: true));
        // tap is allowed.
        value = 0.0;
        gesture.Down(tester.GetCenter(tester.ElementOfType<Slider>()));
        tester.Pump();
        Assert.Equal(0.5, value);
        gesture.Up();
        // slide is allowed.
        value = 0.0;
        gesture.Down(tester.GetCenter(tester.ElementOfType<Slider>()));
        tester.Pump();
        gesture.MoveBy(new Vector(50, 0));
        Assert.True(value > 0.0); // changes
        gesture.Up();
    }

    // Flutter: "Default value indicator color"
    [Fact]
    public void DefaultValueIndicatorColor()
    {
        using FrameworkDartTester tester = NewTester();
        RenderingDebug.DisableShadows = false;
        var theme = new ThemeData(platform: TargetPlatform.Android);
        Widget BuildTestApp(string value, double sliderValue = 0.5, TextScaler? textScaler = null)
        {
            return new MaterialApp(
                theme: theme,
                home: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new MediaQuery(
                        data: new MediaQueryData(TextScaler: textScaler ?? TextScaler.NoScaling),
                        child: new MaterialSurface(
                            child: new Row(
                                children:
                                [
                                    new Expanded(
                                        child: new Slider(
                                            value: sliderValue,
                                            label: value,
                                            divisions: 3,
                                            onChanged: _ => { })),
                                ])))));
        }

        tester.PumpWidget(BuildTestApp("1"));

        RenderObject valueIndicatorBox = OverlayBox(tester);

        Point center = tester.GetCenter(tester.ElementOfType<Slider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
        // Wait for value indicator animation to finish.
        tester.PumpAndSettle();
        PaintAssert.Paints(
            valueIndicatorBox,
            PaintPattern.Paints
                .RRect(color: new Color(0xfffef7ff))
                .RRect(color: new Color(0xffe6e0e9))
                .RRect(color: new Color(0xff6750a4))
                .Path(color: new Color(theme.ColorScheme.Primary.Value)));

        // Finish gesture to release resources.
        gesture.Up();
        tester.PumpAndSettle();
    }

    // Flutter: "RectangularSliderValueIndicatorShape supports SliderTheme.valueIndicatorStrokeColor"
    [Fact]
    public void RectangularSliderValueIndicatorShapeSupportsValueIndicatorStrokeColor()
    {
        ExpectSliderValueIndicatorStrokeColor(new RectangularSliderValueIndicatorShape());
    }

    // Flutter: "PaddleSliderValueIndicatorShape supports SliderTheme.valueIndicatorStrokeColor"
    [Fact]
    public void PaddleSliderValueIndicatorShapeSupportsValueIndicatorStrokeColor()
    {
        ExpectSliderValueIndicatorStrokeColor(new PaddleSliderValueIndicatorShape());
    }

    // Flutter: "DropSliderValueIndicatorShape supports SliderTheme.valueIndicatorStrokeColor"
    [Fact]
    public void DropSliderValueIndicatorShapeSupportsValueIndicatorStrokeColor()
    {
        ExpectSliderValueIndicatorStrokeColor(new DropSliderValueIndicatorShape());
    }

    // Flutter: "RectangularRangeSliderValueIndicatorShape supports SliderTheme.valueIndicatorStrokeColor"
    [Fact]
    public void RectangularRangeSliderValueIndicatorShapeSupportsValueIndicatorStrokeColor()
    {
        ExpectRangeValueIndicatorStrokeColor(
            new RectangularRangeSliderValueIndicatorShape(),
            new RangeValues(0, 0.5),
            overlapping: false);
    }

    // Flutter: "RectangularRangeSliderValueIndicatorShape supports SliderTheme.valueIndicatorStrokeColor on
    // overlapping indicator"
    [Fact]
    public void RectangularRangeSliderValueIndicatorShapeSupportsValueIndicatorStrokeColorOnOverlappingIndicator()
    {
        ExpectRangeValueIndicatorStrokeColor(
            new RectangularRangeSliderValueIndicatorShape(),
            new RangeValues(0.0, 0.0),
            overlapping: true);
    }

    // Flutter: "PaddleRangeSliderValueIndicatorShape supports SliderTheme.valueIndicatorStrokeColor"
    [Fact]
    public void PaddleRangeSliderValueIndicatorShapeSupportsValueIndicatorStrokeColor()
    {
        ExpectRangeValueIndicatorStrokeColor(
            new PaddleRangeSliderValueIndicatorShape(),
            new RangeValues(0, 0.5),
            overlapping: false);
    }

    // Flutter: "PaddleRangeSliderValueIndicatorShape supports SliderTheme.valueIndicatorStrokeColor on
    // overlapping indicator"
    [Fact]
    public void PaddleRangeSliderValueIndicatorShapeSupportsValueIndicatorStrokeColorOnOverlappingIndicator()
    {
        ExpectRangeValueIndicatorStrokeColor(
            new PaddleRangeSliderValueIndicatorShape(),
            new RangeValues(0, 0),
            overlapping: true);
    }

    // Flutter group: "RoundedRectSliderTrackShape"
    // Flutter: "Only draw active track if thumb center is higher than trackRect.left and track radius"
    [Fact]
    public void RoundedRectOnlyDrawActiveTrackIfThumbCenterIsHigherThanTrackRectLeftAndTrackRadius()
    {
        using FrameworkDartTester tester = NewTester();
        var sliderTheme = new SliderThemeData(TrackShape: new RoundedRectSliderTrackShape());
        tester.PumpWidget(BuildApp(sliderTheme));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBR(22.0, 298.0, 776.0, 302.0, Radius.Circular(2.0))));

        tester.PumpWidget(BuildApp(sliderTheme, value: 0.025));

        material = MaterialOf(tester.ElementOfType<Slider>());
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBR(40.8, 298.0, 776.0, 302.0, Radius.Circular(2.0)))
                // Active track.
                .RRect(rrect: RRect.FromLTRBR(24.0, 297.0, 44.8, 303.0, Radius.Circular(3.0))));
    }

    // Flutter group: "RoundedRectSliderTrackShape"
    // Flutter: "Only draw inactive track if thumb center is lower than trackRect.right and track radius"
    [Fact]
    public void RoundedRectOnlyDrawInactiveTrackIfThumbCenterIsLowerThanTrackRectRightAndTrackRadius()
    {
        using FrameworkDartTester tester = NewTester();
        var sliderTheme = new SliderThemeData(TrackShape: new RoundedRectSliderTrackShape());
        tester.PumpWidget(BuildApp(sliderTheme, value: 1.0));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Active track.
                .RRect(rrect: RRect.FromLTRBR(24.0, 297.0, 778.0, 303.0, Radius.Circular(3.0))));

        tester.PumpWidget(BuildApp(sliderTheme, value: 0.975));

        material = MaterialOf(tester.ElementOfType<Slider>());
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBR(755.2, 298.0, 776.0, 302.0, Radius.Circular(2.0)))
                // Active track.
                .RRect(rrect: RRect.FromLTRBR(24.0, 297.0, 759.2, 303.0, Radius.Circular(3.0))));
    }

    // Flutter: "Track shape isRounded defaults"
    [Fact]
    public void TrackShapeIsRoundedDefaults()
    {
        Assert.False(new RectangularSliderTrackShape().IsRounded);
        Assert.True(new RoundedRectSliderTrackShape().IsRounded);
        Assert.False(new RectangularRangeSliderTrackShape().IsRounded);
        Assert.True(new RoundedRectRangeSliderTrackShape().IsRounded);
    }

    // Flutter: "SliderThemeData.padding can override the default Slider padding"
    [Fact]
    public void SliderThemeDataPaddingCanOverrideTheDefaultSliderPadding()
    {
        using FrameworkDartTester tester = NewTester();
        Widget BuildSlider(EdgeInsetsGeometry? padding = null)
        {
            return new MaterialApp(
                theme: new ThemeData(sliderTheme: new SliderThemeData(Padding: padding)),
                home: new MaterialSurface(
                    child: new Center(
                        child: new IntrinsicHeight(child: new Slider(value: 0.5, onChanged: _ => { })))));
        }

        RenderBox SliderRenderBox() => AllRenderObjects(tester).OfType<RenderSlider>().First();

        // Test Slider height and tracks spacing with zero padding.
        tester.PumpWidget(BuildSlider(padding: EdgeInsets.Zero));
        tester.PumpAndSettle();

        // The height equals to the default thumb height.
        Assert.Equal(new Size(800, 20), SliderRenderBox().Size);
        PaintAssert.Paints(
            FoundRenderObject<Slider>(tester),
            PaintPattern.Paints
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBR(398.0, 8.0, 800.0, 12.0, Radius.Circular(2.0)))
                // Active track.
                .RRect(rrect: RRect.FromLTRBR(0.0, 7.0, 402.0, 13.0, Radius.Circular(3.0))));

        // Test Slider height and tracks spacing with directional padding.
        const double startPadding = 100;
        const double endPadding = 20;
        tester.PumpWidget(BuildSlider(
            padding: EdgeInsetsDirectional.Only(start: startPadding, end: endPadding)));
        tester.PumpAndSettle();

        Assert.Equal(new Size(800 - startPadding - endPadding, 20), SliderRenderBox().Size);
        PaintAssert.Paints(
            FoundRenderObject<Slider>(tester),
            PaintPattern.Paints
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBR(338.0, 8.0, 680.0, 12.0, Radius.Circular(2.0)))
                // Active track.
                .RRect(rrect: RRect.FromLTRBR(0.0, 7.0, 342.0, 13.0, Radius.Circular(3.0))));

        // Test Slider height and tracks spacing with top and bottom padding.
        const double topPadding = 100;
        const double bottomPadding = 20;
        const double trackHeight = 20;
        tester.PumpWidget(BuildSlider(
            padding: EdgeInsetsDirectional.Only(top: topPadding, bottom: bottomPadding)));
        tester.PumpAndSettle();

        Assert.Equal(
            new Size(800, topPadding + trackHeight + bottomPadding),
            tester.GetSize(tester.ElementOfType<Slider>()));
        Assert.Equal(new Size(800, 20), SliderRenderBox().Size);
        PaintAssert.Paints(
            FoundRenderObject<Slider>(tester),
            PaintPattern.Paints
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBR(398.0, 8.0, 800.0, 12.0, Radius.Circular(2.0)))
                // Active track.
                .RRect(rrect: RRect.FromLTRBR(0.0, 7.0, 402.0, 13.0, Radius.Circular(3.0))));
    }

    // Flutter: "SliderThemeData.padding can override the default RangeSlider padding"
    [Fact]
    public void SliderThemeDataPaddingCanOverrideTheDefaultRangeSliderPadding()
    {
        using FrameworkDartTester tester = NewTester();
        Widget BuildRangeSlider(EdgeInsetsGeometry? padding = null)
        {
            return new MaterialApp(
                theme: new ThemeData(sliderTheme: new SliderThemeData(Padding: padding)),
                home: new MaterialSurface(
                    child: new Center(
                        child: new IntrinsicHeight(
                            child: new RangeSlider(
                                values: new RangeValues(0, 1.0),
                                onChanged: _ => { })))));
        }

        RenderBox SliderRenderBox() => AllRenderObjects(tester).OfType<RenderRangeSlider>().First();

        PaintPattern ExpectedTracks(double right) => PaintPattern.Paints
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
                right - 10.0,
                8.0,
                right - 10.0,
                12.0,
                topRight: Radius.Circular(2.0),
                bottomRight: Radius.Circular(2.0)))
            // Active track.
            .RRect(rrect: RRect.FromLTRBR(8.0, 7.0, right - 8.0, 13.0, Radius.Circular(2.0)));

        // Test RangeSlider height and tracks spacing with zero padding.
        tester.PumpWidget(BuildRangeSlider(padding: EdgeInsets.Zero));
        tester.PumpAndSettle();

        // The height equals to the default thumb height.
        Assert.Equal(new Size(800, 20), SliderRenderBox().Size);
        // Dart: (10, 8, 10, 12), (790, 8, 790, 12), (8, 7, 792, 13).
        PaintAssert.Paints(FoundRenderObject<RangeSlider>(tester), ExpectedTracks(800.0));

        // Test RangeSlider height and tracks spacing with directional padding.
        const double startPadding = 100;
        const double endPadding = 20;
        tester.PumpWidget(BuildRangeSlider(
            padding: EdgeInsetsDirectional.Only(start: startPadding, end: endPadding)));
        tester.PumpAndSettle();

        Assert.Equal(new Size(800 - startPadding - endPadding, 20), SliderRenderBox().Size);
        // Dart: (10, 8, 10, 12), (670, 8, 670, 12), (8, 7, 672, 13).
        PaintAssert.Paints(FoundRenderObject<RangeSlider>(tester), ExpectedTracks(680.0));

        // Test RangeSlider height and tracks spacing with top and bottom padding.
        const double topPadding = 100;
        const double bottomPadding = 20;
        const double trackHeight = 20;
        tester.PumpWidget(BuildRangeSlider(
            padding: EdgeInsetsDirectional.Only(top: topPadding, bottom: bottomPadding)));
        tester.PumpAndSettle();

        Assert.Equal(
            new Size(800, topPadding + trackHeight + bottomPadding),
            tester.GetSize(tester.ElementOfType<RangeSlider>()));
        Assert.Equal(new Size(800, 20), SliderRenderBox().Size);
        PaintAssert.Paints(FoundRenderObject<RangeSlider>(tester), ExpectedTracks(800.0));
    }

    // Flutter: "Can customize Slider track gap when year2023 is false"
    [Fact]
    public void CanCustomizeSliderTrackGapWhenYear2023IsFalse()
    {
        using FrameworkDartTester tester = NewTester();
        Widget BuildSlider(double? trackGap = null)
        {
            return new MaterialApp(
                theme: new ThemeData(sliderTheme: new SliderThemeData(TrackGap: trackGap)),
                home: new MaterialSurface(
                    child: new Center(child: new Slider(year2023: false, value: 0.5, onChanged: _ => { }))));
        }

        tester.PumpWidget(BuildSlider(trackGap: 0));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        // Test default track shape.
        Radius trackOuterCornerRadius = Radius.Circular(8.0);
        Radius trackInnerCornerRadius = Radius.Circular(2.0);
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Active track.
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    24.0,
                    292.0,
                    400.0,
                    308.0,
                    topLeft: trackOuterCornerRadius,
                    topRight: trackInnerCornerRadius,
                    bottomRight: trackInnerCornerRadius,
                    bottomLeft: trackOuterCornerRadius))
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    400.0,
                    292.0,
                    776.0,
                    308.0,
                    topLeft: trackInnerCornerRadius,
                    topRight: trackOuterCornerRadius,
                    bottomRight: trackOuterCornerRadius,
                    bottomLeft: trackInnerCornerRadius)));

        tester.PumpWidget(BuildSlider(trackGap: 10));
        tester.PumpAndSettle();
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Active track.
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    24.0,
                    292.0,
                    390.0,
                    308.0,
                    topLeft: trackOuterCornerRadius,
                    topRight: trackInnerCornerRadius,
                    bottomRight: trackInnerCornerRadius,
                    bottomLeft: trackOuterCornerRadius))
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    410.0,
                    292.0,
                    776.0,
                    308.0,
                    topLeft: trackInnerCornerRadius,
                    topRight: trackOuterCornerRadius,
                    bottomRight: trackOuterCornerRadius,
                    bottomLeft: trackInnerCornerRadius)));
    }

    // Flutter: "Can customize RangeSlider track gap when year2023 is false"
    [Fact]
    public void CanCustomizeRangeSliderTrackGapWhenYear2023IsFalse()
    {
        using FrameworkDartTester tester = NewTester();
        Widget BuildRangeSlider(double? trackGap = null)
        {
            return new MaterialApp(
                theme: new ThemeData(sliderTheme: new SliderThemeData(TrackGap: trackGap)),
                home: new MaterialSurface(
                    child: new Center(
                        child: new RangeSlider(
                            year2023: false,
                            values: new RangeValues(25, 75),
                            max: 100,
                            onChanged: _ => { }))));
        }

        tester.PumpWidget(BuildRangeSlider(trackGap: 0));

        RenderObject material = MaterialOf(tester.ElementOfType<RangeSlider>());

        // Test default track shape.
        Radius trackOuterCornerRadius = Radius.Circular(8.0);
        Radius trackInnerCornerRadius = Radius.Circular(2.0);
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    24.0,
                    292.0,
                    212.0,
                    308.0,
                    topLeft: trackOuterCornerRadius,
                    topRight: trackInnerCornerRadius,
                    bottomRight: trackInnerCornerRadius,
                    bottomLeft: trackOuterCornerRadius))
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    588.0,
                    292.0,
                    776.0,
                    308.0,
                    topLeft: trackInnerCornerRadius,
                    topRight: trackOuterCornerRadius,
                    bottomRight: trackOuterCornerRadius,
                    bottomLeft: trackInnerCornerRadius))
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBR(212.0, 292.0, 588.0, 308.0, trackInnerCornerRadius)));

        tester.PumpWidget(BuildRangeSlider(trackGap: 10));
        tester.PumpAndSettle();
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    24.0,
                    292.0,
                    202.0,
                    308.0,
                    topLeft: trackOuterCornerRadius,
                    topRight: trackInnerCornerRadius,
                    bottomRight: trackInnerCornerRadius,
                    bottomLeft: trackOuterCornerRadius))
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    598.0,
                    292.0,
                    776.0,
                    308.0,
                    topLeft: trackInnerCornerRadius,
                    topRight: trackOuterCornerRadius,
                    bottomRight: trackOuterCornerRadius,
                    bottomLeft: trackInnerCornerRadius))
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBR(222.0, 292.0, 578.0, 308.0, trackInnerCornerRadius)));
    }

    // Flutter: "Can customize Slider thumb size when year2023 is false"
    [Fact]
    public void CanCustomizeSliderThumbSizeWhenYear2023IsFalse()
    {
        using FrameworkDartTester tester = NewTester();
        Widget BuildSlider(WidgetStateProperty<Size?>? thumbSize = null)
        {
            return new MaterialApp(
                theme: new ThemeData(sliderTheme: new SliderThemeData(ThumbSize: thumbSize)),
                home: new MaterialSurface(
                    child: new Center(child: new Slider(year2023: false, value: 0.5, onChanged: _ => { }))));
        }

        tester.PumpWidget(BuildSlider(thumbSize: new WidgetStatePropertyAll<Size?>(new Size(20, 20))));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .Circle()
                .RRect(rrect: RRect.FromLTRBR(390.0, 290.0, 410.0, 310.0, Radius.Circular(10.0))));

        tester.PumpWidget(BuildSlider(thumbSize: PressedOrAnyThumbSize()));
        tester.PumpAndSettle();

        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .Circle()
                .RRect(rrect: RRect.FromLTRBR(395.0, 295.0, 405.0, 305.0, Radius.Circular(5.0))));

        Point center = tester.GetCenter(tester.ElementOfType<Slider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
        tester.PumpAndSettle();

        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .Circle()
                .RRect(rrect: RRect.FromLTRBR(390.0, 295.0, 410.0, 305.0, Radius.Circular(5.0))));

        gesture.Up();
        tester.PumpAndSettle();
    }

    // Flutter: "Can customize RangeSlider thumbs size when year2023 is false"
    [Fact]
    public void CanCustomizeRangeSliderThumbsSizeWhenYear2023IsFalse()
    {
        using FrameworkDartTester tester = NewTester();
        Widget BuildRangeSlider(WidgetStateProperty<Size?>? thumbSize = null)
        {
            return new MaterialApp(
                theme: new ThemeData(sliderTheme: new SliderThemeData(ThumbSize: thumbSize)),
                home: new MaterialSurface(
                    child: new Center(
                        child: new RangeSlider(
                            year2023: false,
                            values: new RangeValues(25, 75),
                            max: 100,
                            onChanged: _ => { }))));
        }

        tester.PumpWidget(BuildRangeSlider(thumbSize: new WidgetStatePropertyAll<Size?>(new Size(20, 20))));

        RenderObject material = MaterialOf(tester.ElementOfType<RangeSlider>());
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .Circle()
                .RRect(rrect: RRect.FromLTRBR(202.0, 290.0, 222.0, 310.0, Radius.Circular(10.0)))
                .RRect(rrect: RRect.FromLTRBR(578.0, 290.0, 598.0, 310.0, Radius.Circular(10.0))));

        tester.PumpWidget(BuildRangeSlider(thumbSize: PressedOrAnyThumbSize()));
        tester.PumpAndSettle();

        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .Circle()
                .RRect(rrect: RRect.FromLTRBR(207.0, 295.0, 217.0, 305.0, Radius.Circular(5.0)))
                .RRect(rrect: RRect.FromLTRBR(583.0, 295.0, 593.0, 305.0, Radius.Circular(5.0))));

        Rect sliderRect = tester.GetRect(tester.ElementOfType<RangeSlider>());
        var topRight = new Point(sliderRect.Right, sliderRect.Top);
        TestGesture gesture = tester.StartGesture(topRight, PointerDeviceKind.Touch);
        tester.PumpAndSettle();

        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .Circle()
                .RRect(rrect: RRect.FromLTRBR(207.0, 295.0, 217.0, 305.0, Radius.Circular(5.0)))
                .RRect(rrect: RRect.FromLTRBR(583.0, 295.0, 593.0, 305.0, Radius.Circular(5.0))));

        gesture.Up();
        tester.PumpAndSettle();
    }

    // Flutter: "Opt into 2024 Slider appearance with SliderThemeData.year2023"
    [Fact]
    public void OptInto2024SliderAppearanceWithSliderThemeDataYear2023()
    {
        using FrameworkDartTester tester = NewTester();
        var theme = new ThemeData(sliderTheme: new SliderThemeData(Year2023: false));
        ColorScheme colorScheme = theme.ColorScheme;
        Color activeTrackColor = colorScheme.Primary;
        Color inactiveTrackColor = colorScheme.SecondaryContainer;
        const double value = 0.45;
        tester.PumpWidget(new MaterialApp(
            theme: theme,
            home: new MaterialSurface(
                child: new Center(
                    child: new Slider(value: value, onChanged: _ => { })))));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        // Test default track shape.
        Radius trackOuterCornerRadius = Radius.Circular(8.0);
        Radius trackInnerCornerRadius = Radius.Circular(2.0);
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Active track.
                .RRect(
                    rrect: RRect.FromLTRBAndCorners(
                        24.0,
                        292.0,
                        356.4,
                        308.0,
                        topLeft: trackOuterCornerRadius,
                        topRight: trackInnerCornerRadius,
                        bottomRight: trackInnerCornerRadius,
                        bottomLeft: trackOuterCornerRadius),
                    color: activeTrackColor)
                // Inctive track.
                .RRect(
                    rrect: RRect.FromLTRBAndCorners(
                        368.4,
                        292.0,
                        776.0,
                        308.0,
                        topLeft: trackInnerCornerRadius,
                        topRight: trackOuterCornerRadius,
                        bottomRight: trackOuterCornerRadius,
                        bottomLeft: trackInnerCornerRadius),
                    color: inactiveTrackColor));
    }

    // Flutter: "Slider.year2023 overrides SliderThemeData.year2023"
    [Fact]
    public void SliderYear2023OverridesSliderThemeDataYear2023()
    {
        using FrameworkDartTester tester = NewTester();
        var theme = new ThemeData(sliderTheme: new SliderThemeData(Year2023: false));
        ColorScheme colorScheme = theme.ColorScheme;
        Color activeTrackColor = colorScheme.Primary;
        Color inactiveTrackColor = colorScheme.SurfaceContainerHighest;
        const double value = 0.45;
        tester.PumpWidget(new MaterialApp(
            home: new MaterialSurface(
                child: new Center(
                    child: new Theme(
                        data: theme,
                        child: new Slider(year2023: true, value: value, onChanged: _ => { }))))));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        // Test default track shape.
        Radius activeTrackCornerRadius = Radius.Circular(3.0);
        Radius inactiveTrackCornerRadius = Radius.Circular(2.0);
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Active track.
                .RRect(
                    rrect: RRect.FromLTRBR(360.4, 298.0, 776.0, 302.0, inactiveTrackCornerRadius),
                    color: inactiveTrackColor)
                // Inctive track.
                .RRect(
                    rrect: RRect.FromLTRBR(24.0, 297.0, 364.4, 303.0, activeTrackCornerRadius),
                    color: activeTrackColor));
    }

    // Regression test for https://github.com/flutter/flutter/issues/161210
    // Flutter: "Slider with transparent track colors and custom track height can reach extreme ends"
    [Fact]
    public void SliderWithTransparentTrackColorsAndCustomTrackHeightCanReachExtremeEnds()
    {
        using FrameworkDartTester tester = NewTester();
        const double sliderPadding = 24.0;
        var theme = new ThemeData(
            sliderTheme: new SliderThemeData(
                TrackHeight: 100,
                ActiveTrackColor: MaterialColors.Transparent,
                InactiveTrackColor: MaterialColors.Transparent));

        Widget BuildSlider(double value)
        {
            return new MaterialApp(
                theme: theme,
                home: new MaterialSurface(
                    child: new SizedBox(
                        width: 300,
                        child: new Slider(value: value, onChanged: _ => { }))));
        }

        tester.PumpWidget(BuildSlider(value: 0));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        PaintAssert.Paints(
            material,
            PaintPattern.Paints.Circle(x: sliderPadding, y: 300.0, color: theme.ColorScheme.Primary));

        tester.PumpWidget(BuildSlider(value: 1));

        material = MaterialOf(tester.ElementOfType<Slider>());
        PaintAssert.Paints(
            material,
            PaintPattern.Paints.Circle(x: 800.0 - sliderPadding, y: 300.0, color: theme.ColorScheme.Primary));
    }

    // Regression test for https://github.com/flutter/flutter/issues/161210
    // Flutter: "RangeSlider with transparent track colors and custom track height can reach extreme ends"
    [Fact]
    public void RangeSliderWithTransparentTrackColorsAndCustomTrackHeightCanReachExtremeEnds()
    {
        using FrameworkDartTester tester = NewTester();
        const double sliderPadding = 24.0;
        var theme = new ThemeData(
            sliderTheme: new SliderThemeData(
                TrackHeight: 100,
                ActiveTrackColor: MaterialColors.Transparent,
                InactiveTrackColor: MaterialColors.Transparent));

        tester.PumpWidget(new MaterialApp(
            theme: theme,
            home: new MaterialSurface(
                child: new SizedBox(
                    width: 300,
                    child: new RangeSlider(
                        values: new RangeValues(0, 1),
                        onChanged: _ => { })))));

        RenderObject material = MaterialOf(tester.ElementOfType<RangeSlider>());

        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .Circle(x: sliderPadding, y: 300.0, color: theme.ColorScheme.Primary)
                .Circle(x: 800.0 - sliderPadding, y: 300.0, color: theme.ColorScheme.Primary));
    }

    // These tests are only relevant for Material 2. Once Material 2
    // support is deprecated and the APIs are removed, these tests
    // can be deleted.

    // Flutter group: "Material 2"
    // Flutter: "Slider defaults"
    [Fact]
    public void Material2SliderDefaults()
    {
        using FrameworkDartTester tester = NewTester();
        RenderingDebug.DisableShadows = false;
        var theme = new ThemeData(useMaterial3: false);
        const double trackHeight = 4.0;
        ColorScheme colorScheme = theme.ColorScheme;
        var activeTrackColor = new Color(colorScheme.Primary.Value);
        Color inactiveTrackColor = colorScheme.Primary.WithOpacity(0.24);
        Color secondaryActiveTrackColor = colorScheme.Primary.WithOpacity(0.54);
        Color disabledActiveTrackColor = colorScheme.OnSurface.WithOpacity(0.32);
        Color disabledInactiveTrackColor = colorScheme.OnSurface.WithOpacity(0.12);
        Color disabledSecondaryActiveTrackColor = colorScheme.OnSurface.WithOpacity(0.12);
        Color shadowColor = colorScheme.Shadow;
        var thumbColor = new Color(colorScheme.Primary.Value);
        Color disabledThumbColor = Color.AlphaBlend(colorScheme.OnSurface.WithOpacity(.38), colorScheme.Surface);
        Color activeTickMarkColor = colorScheme.OnPrimary.WithOpacity(0.54);
        Color inactiveTickMarkColor = colorScheme.Primary.WithOpacity(0.54);
        Color disabledActiveTickMarkColor = colorScheme.OnPrimary.WithOpacity(0.12);
        Color disabledInactiveTickMarkColor = colorScheme.OnSurface.WithOpacity(0.12);
        Color valueIndicatorColor = Color.AlphaBlend(
            colorScheme.OnSurface.WithOpacity(0.60),
            colorScheme.Surface.WithOpacity(0.90));

        double value = 0.45;
        Widget BuildTestApp(int? divisions = null, bool enabled = true)
        {
            Action<double>? onChanged = !enabled ? null : d => value = d;
            return new MaterialApp(
                theme: theme,
                home: new MaterialSurface(
                    child: new Center(
                        child: new Slider(
                            value: value,
                            secondaryTrackValue: 0.75,
                            label: BindingBase.DartDoubleToString(value),
                            divisions: divisions,
                            onChanged: onChanged))));
        }

        tester.PumpWidget(BuildTestApp());

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());
        RenderObject valueIndicatorBox = OverlayBox(tester);

        // Test default track height.
        Radius radius = Radius.Circular(trackHeight / 2);
        Radius activatedRadius = Radius.Circular((trackHeight + 2) / 2);
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .RRect(rrect: RRect.FromLTRBR(360.4, 298.0, 776.0, 302.0, radius), color: inactiveTrackColor)
                .RRect(rrect: RRect.FromLTRBR(24.0, 297.0, 364.4, 303.0, activatedRadius), color: activeTrackColor));

        // Test default colors for enabled slider.
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .RRect(color: inactiveTrackColor)
                .RRect(color: activeTrackColor)
                .RRect(color: secondaryActiveTrackColor));
        PaintAssert.Paints(material, PaintPattern.Paints.Shadow(color: shadowColor));
        PaintAssert.Paints(material, PaintPattern.Paints.Circle(color: thumbColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: disabledThumbColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledActiveTrackColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledInactiveTrackColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledSecondaryActiveTrackColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: activeTickMarkColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: inactiveTickMarkColor));

        // Test defaults colors for discrete slider.
        tester.PumpWidget(BuildTestApp(divisions: 3));
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .RRect(color: inactiveTrackColor)
                .RRect(color: activeTrackColor)
                .RRect(color: secondaryActiveTrackColor));
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .Circle(color: activeTickMarkColor)
                .Circle(color: activeTickMarkColor)
                .Circle(color: inactiveTickMarkColor)
                .Circle(color: inactiveTickMarkColor)
                .Shadow(color: MaterialColors.Black)
                .Circle(color: thumbColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: disabledThumbColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledActiveTrackColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledInactiveTrackColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledSecondaryActiveTrackColor));

        // Test defaults colors for disabled slider.
        tester.PumpWidget(BuildTestApp(enabled: false));
        tester.PumpAndSettle();
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .RRect(color: disabledInactiveTrackColor)
                .RRect(color: disabledActiveTrackColor)
                .RRect(color: disabledSecondaryActiveTrackColor));
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .Shadow(color: MaterialColors.Black)
                .Circle(color: disabledThumbColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: thumbColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: activeTrackColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: inactiveTrackColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: secondaryActiveTrackColor));

        // Test defaults colors for disabled discrete slider.
        tester.PumpWidget(BuildTestApp(divisions: 3, enabled: false));
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .Circle(color: disabledActiveTickMarkColor)
                .Circle(color: disabledActiveTickMarkColor)
                .Circle(color: disabledInactiveTickMarkColor)
                .Circle(color: disabledInactiveTickMarkColor)
                .Shadow(color: MaterialColors.Black)
                .Circle(color: disabledThumbColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: thumbColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: activeTrackColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: inactiveTrackColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: secondaryActiveTrackColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: activeTickMarkColor));
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: inactiveTickMarkColor));

        // Test the default color for value indicator.
        tester.PumpWidget(BuildTestApp(divisions: 3));
        Point center = tester.GetCenter(tester.ElementOfType<Slider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
        // Wait for value indicator animation to finish.
        tester.PumpAndSettle();
        Assert.Equal(2.0 / 3.0, value);
        PaintAssert.Paints(
            valueIndicatorBox,
            PaintPattern.Paints
                .Path(color: valueIndicatorColor)
                .Paragraph());
        gesture.Up();
        // Wait for value indicator animation to finish.
        tester.PumpAndSettle();
    }

    // Flutter group: "Material 2"
    // Flutter: "Default value indicator color"
    [Fact]
    public void Material2DefaultValueIndicatorColor()
    {
        using FrameworkDartTester tester = NewTester();
        RenderingDebug.DisableShadows = false;
        var theme = new ThemeData(useMaterial3: false, platform: TargetPlatform.Android);
        Widget BuildTestApp(string value, double sliderValue = 0.5, TextScaler? textScaler = null)
        {
            return new MaterialApp(
                theme: theme,
                home: new MediaQuery(
                    data: new MediaQueryData(TextScaler: textScaler ?? TextScaler.NoScaling),
                    child: new MaterialSurface(
                        child: new Row(
                            children:
                            [
                                new Expanded(
                                    child: new Slider(
                                        value: sliderValue,
                                        label: value,
                                        divisions: 3,
                                        onChanged: _ => { })),
                            ]))));
        }

        tester.PumpWidget(BuildTestApp("1"));

        RenderObject valueIndicatorBox = OverlayBox(tester);

        Point center = tester.GetCenter(tester.ElementOfType<Slider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
        // Wait for value indicator animation to finish.
        tester.PumpAndSettle();
        PaintAssert.Paints(
            valueIndicatorBox,
            PaintPattern.Paints
                .RRect(color: new Color(0xfffafafa))
                .RRect(color: new Color(0x3d2196f3))
                .RRect(color: new Color(0xff2196f3))
                // Test that the value indicator text is painted with the correct color.
                .Path(color: new Color(0xf55f5f5f)));

        // Finish gesture to release resources.
        gesture.Up();
        tester.PumpAndSettle();
    }

    // Shared body of the four "... supports SliderTheme.valueIndicatorStrokeColor" Slider tests.
    private static void ExpectSliderValueIndicatorStrokeColor(SliderComponentShape valueIndicatorShape)
    {
        using FrameworkDartTester tester = NewTester();
        var theme = new ThemeData(
            sliderTheme: new SliderThemeData(
                ShowValueIndicator: ShowValueIndicator.Always,
                ValueIndicatorShape: valueIndicatorShape,
                ValueIndicatorColor: new Color(0xff000001),
                ValueIndicatorStrokeColor: new Color(0xff000002)));

        const double value = 0.5;

        tester.PumpWidget(new MaterialApp(
            theme: theme,
            home: new MaterialSurface(
                child: new Center(
                    child: new Slider(
                        value: value,
                        label: BindingBase.DartDoubleToString(value),
                        onChanged: _ => { })))));

        RenderObject valueIndicatorBox = OverlayBox(tester);

        Point center = tester.GetCenter(tester.ElementOfType<Slider>());
        tester.StartGesture(center, PointerDeviceKind.Touch);
        // Wait for value indicator animation to finish.
        tester.PumpAndSettle();

        PaintAssert.Paints(
            valueIndicatorBox,
            PaintPattern.Paints
                .Path(color: theme.ColorScheme.Shadow) // shadow
                .Path(color: theme.SliderTheme.ValueIndicatorStrokeColor)
                .Path(color: theme.SliderTheme.ValueIndicatorColor));
    }

    // Shared body of the four "... RangeSliderValueIndicatorShape supports SliderTheme.valueIndicatorStrokeColor"
    // tests; the overlapping variants also set overlappingShapeStrokeColor.
    private static void ExpectRangeValueIndicatorStrokeColor(
        RangeSliderValueIndicatorShape rangeValueIndicatorShape,
        RangeValues values,
        bool overlapping)
    {
        using FrameworkDartTester tester = NewTester();
        var theme = new ThemeData(
            sliderTheme: new SliderThemeData(
                ShowValueIndicator: ShowValueIndicator.Always,
                RangeValueIndicatorShape: rangeValueIndicatorShape,
                ValueIndicatorColor: new Color(0xff000001),
                ValueIndicatorStrokeColor: new Color(0xff000002),
                OverlappingShapeStrokeColor: overlapping ? new Color(0xff000003) : null));

        tester.PumpWidget(new MaterialApp(
            theme: theme,
            home: new MaterialSurface(
                child: new Center(
                    child: new RangeSlider(
                        values: values,
                        labels: new RangeLabels(
                            BindingBase.DartDoubleToString(values.Start),
                            BindingBase.DartDoubleToString(values.End)),
                        onChanged: val => values = val)))));

        RenderObject valueIndicatorBox = OverlayBox(tester);

        Point center = tester.GetCenter(tester.ElementOfType<RangeSlider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
        // Wait for value indicator animation to finish.
        tester.PumpAndSettle();

        PaintAssert.Paints(
            valueIndicatorBox,
            PaintPattern.Paints
                .Path(color: theme.ColorScheme.Shadow) // shadow
                .Path(color: theme.ColorScheme.Shadow) // shadow
                .Path(color: theme.SliderTheme.ValueIndicatorStrokeColor)
                .Path(color: theme.SliderTheme.ValueIndicatorColor)
                .Path(color: overlapping
                    ? theme.SliderTheme.OverlappingShapeStrokeColor
                    : theme.SliderTheme.ValueIndicatorStrokeColor)
                .Path(color: theme.SliderTheme.ValueIndicatorColor));

        gesture.Up();
    }

    /// <summary>
    /// A tester with flutter_test's defaults: <c>debugDisableShadows == true</c> and
    /// <c>defaultTargetPlatform == android</c> (the override is async-local, so it is set on the test's
    /// own flow).
    /// </summary>
    private static FrameworkDartTester NewTester()
    {
        RenderingDebug.DisableShadows = true;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        return new FrameworkDartTester();
    }

    private static WidgetStateProperty<Size?> PressedOrAnyThumbSize() =>
        WidgetStateProperty<Size?>.FromMap(
        [
            new KeyValuePair<WidgetStatesConstraint, Size?>(WidgetState.Pressed, new Size(20, 20)),
            new KeyValuePair<WidgetStatesConstraint, Size?>(WidgetStatesConstraint.Any, new Size(10, 10)),
        ]);

    /// <summary>Dart's <c>Material.of(context)</c> as a render object: the nearest ink-features box.</summary>
    private static RenderObject MaterialOf(Element element)
    {
        RenderObject? node = element.FindRenderObject();
        while (node is not null and not RenderInkFeatures)
        {
            node = node.Parent;
        }

        return node ?? throw new InvalidOperationException("No Material ancestor.");
    }

    /// <summary>Dart's <c>tester.renderObject(find.byType(Overlay))</c>.</summary>
    private static RenderObject OverlayBox(FrameworkDartTester tester) =>
        tester.ElementOfType<Overlay>().FindRenderObject()!;

    /// <summary><c>expect(find.byType(T), paints..)</c> paints the found element's render object.</summary>
    private static RenderObject FoundRenderObject<TWidget>(FrameworkDartTester tester)
        where TWidget : Widget =>
        tester.ElementOfType<TWidget>().FindRenderObject()!;

    /// <summary>Dart's <c>tester.allRenderObjects</c>, depth-first from the render view.</summary>
    private static List<RenderObject> AllRenderObjects(FrameworkDartTester tester)
    {
        var result = new List<RenderObject>();
        void Visit(RenderObject node)
        {
            result.Add(node);
            node.VisitChildren(Visit);
        }

        Visit(tester.RenderView);
        return result;
    }

    /// <summary>Dart's file-level <c>_buildApp</c>.</summary>
    private static Widget BuildApp(
        SliderThemeData sliderTheme,
        double value = 0.0,
        double? secondaryTrackValue = null,
        bool enabled = true,
        int? divisions = null,
        FocusNode? focusNode = null)
    {
        Action<double>? onChanged = enabled ? d => value = d : null;
        return new MaterialApp(
            home: new Scaffold(
                body: new Center(
                    child: new SliderTheme(
                        data: sliderTheme,
                        child: new Slider(
                            value: value,
                            secondaryTrackValue: secondaryTrackValue,
                            label: BindingBase.DartDoubleToString(value),
                            onChanged: onChanged,
                            divisions: divisions,
                            focusNode: focusNode)))));
    }

    /// <summary>Dart's file-level <c>_buildRangeApp</c>.</summary>
    private static Widget BuildRangeApp(
        SliderThemeData sliderTheme,
        RangeValues? values = null,
        bool enabled = true,
        int? divisions = null)
    {
        RangeValues current = values ?? new RangeValues(0, 0);
        Action<RangeValues>? onChanged = enabled ? d => current = d : null;
        return new MaterialApp(
            home: new Scaffold(
                body: new Center(
                    child: new SliderTheme(
                        data: sliderTheme,
                        child: new RangeSlider(
                            values: current,
                            labels: new RangeLabels(
                                BindingBase.DartDoubleToString(current.Start),
                                BindingBase.DartDoubleToString(current.End)),
                            onChanged: onChanged,
                            divisions: divisions)))));
    }

    /// <summary>
    /// Dart's <c>RoundedRectSliderTrackShapeWithCustomAdditionalActiveTrackHeight</c>: paints with its own
    /// <see cref="AdditionalActiveTrackHeight"/> instead of the default 2.
    /// </summary>
    private sealed class RoundedRectSliderTrackShapeWithCustomAdditionalActiveTrackHeight(
        double additionalActiveTrackHeight) : RoundedRectSliderTrackShape
    {
        public double AdditionalActiveTrackHeight { get; } = additionalActiveTrackHeight;

        public override void Paint(
            PaintingContext context,
            Point offset,
            RenderBox parentBox,
            SliderThemeData sliderTheme,
            Animation<double> enableAnimation,
            Point thumbCenter,
            TextDirection textDirection,
            Point? secondaryOffset = null,
            bool isEnabled = false,
            bool isDiscrete = false)
        {
            Paint(
                context,
                offset,
                parentBox,
                sliderTheme,
                enableAnimation,
                thumbCenter,
                textDirection,
                secondaryOffset,
                isEnabled: false,
                isDiscrete: false,
                additionalActiveTrackHeight: AdditionalActiveTrackHeight);
        }
    }
}
