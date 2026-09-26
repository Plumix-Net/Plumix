// Dart parity source: material_ui/lib/src/slider.dart
// Mirrors material-ui-src/test/slider_test.dart (lines 1-2373)
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
using Xunit.Sdk;
using FontWeight = Avalonia.Media.FontWeight;
using MaterialWidget = Plumix.Material.Material;
using TextDirection = Plumix.UI.TextDirection;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SliderDartParityTestsA : IDisposable
{
    public SliderDartParityTestsA()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    public static TheoryData<TargetPlatform> AndroidFuchsiaLinux =>
        new(TargetPlatform.Android, TargetPlatform.Fuchsia, TargetPlatform.Linux);

    public static TheoryData<TargetPlatform> IOSAndMacOS => new(TargetPlatform.IOS, TargetPlatform.MacOS);

    // flutter_test runs every test with `debugDefaultTargetPlatformOverride == TargetPlatform.android`.
    private static FrameworkDartTester CreateTester(TargetPlatform platform = TargetPlatform.Android)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        // flutter_test resets the semantics id counter before every test.
        SemanticsNode.DebugResetSemanticsIdCounter();
        return new FrameworkDartTester(fakeGestureTimers: true);
    }

    // Flutter: "The initial value should respect the discrete value"
    [Fact]
    public void InitialValueShouldRespectTheDiscreteValue()
    {
        using FrameworkDartTester tester = CreateTester();
        var sliderKey = new UniqueKey();
        double value = 0.20;
        var log = new List<Point>();
        var loggingThumb = new LoggingThumbShape(log);
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new StatefulBuilder((context, setState) =>
                {
                    SliderThemeData sliderTheme = SliderTheme.Of(context).CopyWith(thumbShape: loggingThumb);
                    return new MaterialWidget(
                        child: new Center(
                            child: new SliderTheme(
                                data: sliderTheme,
                                child: new Slider(
                                    key: sliderKey,
                                    value: value,
                                    divisions: 4,
                                    onChanged: newValue => setState(() => value = newValue)))));
                }))));

        Assert.Equal(0.20, value);
        Assert.Single(log);
        Assert.Equal(new Point(213.0, 300.0), log[0]);
    }

    // Flutter: "Slider can move when tapped (LTR)"
    [Fact]
    public void SliderCanMoveWhenTappedLtr()
    {
        using FrameworkDartTester tester = CreateTester();
        var sliderKey = new UniqueKey();
        double value = 0.0;
        double? startValue = null;
        double? endValue = null;

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new StatefulBuilder((context, setState) => new MaterialWidget(
                    child: new Center(
                        child: new Slider(
                            key: sliderKey,
                            value: value,
                            onChanged: newValue => setState(() => value = newValue),
                            onChangeStart: v => startValue = v,
                            onChangeEnd: v => endValue = v)))))));

        Assert.Equal(0.0, value);
        Tap(tester, tester.ElementsWithKey(sliderKey).Single());
        Assert.Equal(0.5, value);
        Assert.Equal(0.0, startValue);
        Assert.Equal(0.5, endValue);
        startValue = null;
        endValue = null;
        tester.Pump(); // No animation should start.
        Assert.Equal(0, Scheduler.TransientCallbackCount);

        Point topLeft = tester.GetTopLeft(tester.ElementsWithKey(sliderKey).Single());
        Point bottomRight = tester.GetBottomRight(tester.ElementsWithKey(sliderKey).Single());

        Point target = topLeft + (bottomRight - topLeft) / 4.0;
        TapAt(tester, target);
        Assert.Equal(0.25, value, 0.05);
        Assert.Equal(0.5, startValue);
        Assert.NotNull(endValue);
        Assert.Equal(0.25, endValue!.Value, 0.05);
        tester.Pump(); // No animation should start.
        Assert.Equal(0, Scheduler.TransientCallbackCount);
    }

    // Flutter: "Slider can move when tapped (RTL)"
    [Fact]
    public void SliderCanMoveWhenTappedRtl()
    {
        using FrameworkDartTester tester = CreateTester();
        var sliderKey = new UniqueKey();
        double value = 0.0;

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Rtl,
                child: new StatefulBuilder((context, setState) => new MaterialWidget(
                    child: new Center(
                        child: new Slider(
                            key: sliderKey,
                            value: value,
                            onChanged: newValue => setState(() => value = newValue))))))));

        Assert.Equal(0.0, value);
        Tap(tester, tester.ElementsWithKey(sliderKey).Single());
        Assert.Equal(0.5, value);
        tester.Pump(); // No animation should start.
        Assert.Equal(0, Scheduler.TransientCallbackCount);

        Point topLeft = tester.GetTopLeft(tester.ElementsWithKey(sliderKey).Single());
        Point bottomRight = tester.GetBottomRight(tester.ElementsWithKey(sliderKey).Single());

        Point target = topLeft + (bottomRight - topLeft) / 4.0;
        TapAt(tester, target);
        Assert.Equal(0.75, value, 0.05);
        tester.Pump(); // No animation should start.
        Assert.Equal(0, Scheduler.TransientCallbackCount);
    }

    // Flutter: "Slider doesn't send duplicate change events if tapped on the same value" (L339)
    [Fact]
    public void SliderDoesNotSendDuplicateChangeEventsIfTappedOnTheSameValue()
    {
        using FrameworkDartTester tester = CreateTester();
        var sliderKey = new UniqueKey();
        double value = 0.0;
        double startValue = double.NaN;
        double endValue = double.NaN;
        int updates = 0;
        int startValueUpdates = 0;
        int endValueUpdates = 0;

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new StatefulBuilder((context, setState) => new MaterialWidget(
                    child: new Center(
                        child: new Slider(
                            key: sliderKey,
                            value: value,
                            onChanged: newValue => setState(() =>
                            {
                                updates++;
                                value = newValue;
                            }),
                            onChangeStart: v =>
                            {
                                startValueUpdates++;
                                startValue = v;
                            },
                            onChangeEnd: v =>
                            {
                                endValueUpdates++;
                                endValue = v;
                            })))))));

        Assert.Equal(0.0, value);
        Tap(tester, tester.ElementsWithKey(sliderKey).Single());
        Assert.Equal(0.5, value);
        Assert.Equal(0.0, startValue);
        Assert.Equal(0.5, endValue);
        tester.Pump();
        Tap(tester, tester.ElementsWithKey(sliderKey).Single());
        Assert.Equal(0.5, value);
        tester.Pump();
        Assert.Equal(1, updates);
        Assert.Equal(2, startValueUpdates);
        Assert.Equal(2, endValueUpdates);
    }

    // Flutter: "Value indicator shows for a bit after being tapped"
    [Fact]
    public void ValueIndicatorShowsForABitAfterBeingTapped()
    {
        using FrameworkDartTester tester = CreateTester();
        var sliderKey = new UniqueKey();
        double value = 0.0;

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new StatefulBuilder((context, setState) => new MaterialWidget(
                    child: new Center(
                        child: new Slider(
                            key: sliderKey,
                            value: value,
                            divisions: 4,
                            onChanged: newValue => setState(() => value = newValue))))))));

        Assert.Equal(0.0, value);
        Tap(tester, tester.ElementsWithKey(sliderKey).Single());
        Assert.Equal(0.5, value);
        tester.Pump(TimeSpan.FromMilliseconds(100));
        // Starts with the position animation and value indicator
        Assert.Equal(2, Scheduler.TransientCallbackCount);
        tester.Pump(TimeSpan.FromMilliseconds(100));
        // Value indicator is longer than position.
        Assert.Equal(1, Scheduler.TransientCallbackCount);
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(0, Scheduler.TransientCallbackCount);
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(0, Scheduler.TransientCallbackCount);
        tester.Pump(TimeSpan.FromMilliseconds(100));
        // Shown for long enough, value indicator is animated closed.
        Assert.Equal(1, Scheduler.TransientCallbackCount);
        tester.Pump(TimeSpan.FromMilliseconds(101));
        Assert.Equal(0, Scheduler.TransientCallbackCount);
    }

    // Flutter: "Discrete Slider repaints and animates when dragged"
    [Fact]
    public void DiscreteSliderRepaintsAndAnimatesWhenDragged() => DiscreteSliderRepaintsWhenDraggedBody();

    // Flutter: "Slider doesn't send duplicate change events if tapped on the same value" (L518)
    [Fact]
    public void SliderDoesNotSendDuplicateChangeEventsIfTappedOnTheSameValue2()
    {
        using FrameworkDartTester tester = CreateTester();
        var sliderKey = new UniqueKey();
        double value = 0.0;
        int updates = 0;

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new StatefulBuilder((context, setState) => new MaterialWidget(
                    child: new Center(
                        child: new Slider(
                            key: sliderKey,
                            value: value,
                            onChanged: newValue => setState(() =>
                            {
                                updates++;
                                value = newValue;
                            }))))))));

        Assert.Equal(0.0, value);
        Tap(tester, tester.ElementsWithKey(sliderKey).Single());
        Assert.Equal(0.5, value);
        tester.Pump();
        Tap(tester, tester.ElementsWithKey(sliderKey).Single());
        Assert.Equal(0.5, value);
        tester.Pump();
        Assert.Equal(1, updates);
    }

    // Flutter: "Discrete Slider repaints when dragged"
    [Fact]
    public void DiscreteSliderRepaintsWhenDragged() => DiscreteSliderRepaintsWhenDraggedBody();

    // The Dart file carries this body twice, verbatim (L448 and L561).
    private static void DiscreteSliderRepaintsWhenDraggedBody()
    {
        using FrameworkDartTester tester = CreateTester();
        var sliderKey = new UniqueKey();
        double value = 0.0;
        var log = new List<Point>();
        var loggingThumb = new LoggingThumbShape(log);
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new StatefulBuilder((context, setState) =>
                {
                    SliderThemeData sliderTheme = SliderTheme.Of(context).CopyWith(thumbShape: loggingThumb);
                    return new MaterialWidget(
                        child: new Center(
                            child: new SliderTheme(
                                data: sliderTheme,
                                child: new Slider(
                                    key: sliderKey,
                                    value: value,
                                    divisions: 4,
                                    onChanged: newValue => setState(() => value = newValue)))));
                }))));

        var expectedLog = new List<Point>
        {
            new(26.0, 300.0),
            new(26.0, 300.0),
            new(400.0, 300.0),
        };
        TestGesture gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementsWithKey(sliderKey).Single()),
            PointerDeviceKind.Touch);
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(0.5, value);
        Assert.Equal(3, log.Count);
        Assert.Equal(expectedLog, log);
        gesture.MoveBy(new Vector(-500.0, 0.0));
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(10));
        Assert.Equal(0.0, value);
        Assert.Equal(5, log.Count);
        Assert.Equal(386.6, log[^1].X, 0.1);
        // With no more gesture or value changes, the thumb position should still
        // be redrawn in the animated position.
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(10));
        Assert.Equal(0.0, value);
        Assert.Equal(7, log.Count);
        Assert.Equal(344.8, log[^1].X, 0.1);
        // Final position.
        tester.Pump(TimeSpan.FromMilliseconds(80));
        expectedLog.Add(new Point(26.0, 300.0));
        Assert.Equal(0.0, value);
        Assert.Equal(8, log.Count);
        Assert.Equal(26.0, log[^1].X, 0.1);
        gesture.Up();
    }

    // Flutter: "Slider take on discrete values"
    [Fact]
    public void SliderTakeOnDiscreteValues()
    {
        using FrameworkDartTester tester = CreateTester();
        var sliderKey = new UniqueKey();
        double value = 0.0;

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new StatefulBuilder((context, setState) => new MaterialWidget(
                    child: new Center(
                        child: new SizedBox(
                            width: 144.0 + 2 * 16.0, // _kPreferredTotalWidth
                            child: new Slider(
                                key: sliderKey,
                                max: 100.0,
                                divisions: 10,
                                value: value,
                                onChanged: newValue => setState(() => value = newValue)))))))));

        Assert.Equal(0.0, value);
        Tap(tester, tester.ElementsWithKey(sliderKey).Single());
        Assert.Equal(50.0, value);
        tester.Drag(tester.ElementsWithKey(sliderKey).Single(), new Vector(5.0, 0.0));
        Assert.Equal(50.0, value);
        tester.Drag(tester.ElementsWithKey(sliderKey).Single(), new Vector(40.0, 0.0));
        Assert.Equal(80.0, value);

        tester.Pump(); // Starts animation.
        Assert.True(Scheduler.TransientCallbackCount > 0);
        tester.Pump(TimeSpan.FromMilliseconds(200));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        tester.Pump(TimeSpan.FromMilliseconds(200));
        // Animation complete.
        Assert.Equal(0, Scheduler.TransientCallbackCount);
    }

    // Flutter: "Slider can be given zero values"
    [Fact]
    public void SliderCanBeGivenZeroValues()
    {
        using FrameworkDartTester tester = CreateTester();
        var log = new List<double>();
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Slider(value: 0.0, onChanged: newValue => log.Add(newValue))))));

        Tap(tester, tester.ElementOfType<Slider>());
        Assert.Equal([0.5], log);
        log.Clear();

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Slider(value: 0.0, max: 0.0, onChanged: newValue => log.Add(newValue))))));

        Tap(tester, tester.ElementOfType<Slider>());
        Assert.Empty(log);
        log.Clear();
    }

    // Flutter: "Slider can tap in vertical scroller"
    [Fact]
    public void SliderCanTapInVerticalScroller()
    {
        using FrameworkDartTester tester = CreateTester();
        double value = 0.0;
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new ListView(
                        children:
                        [
                            new Slider(value: value, onChanged: newValue => value = newValue),
                            new Container(height: 2000.0),
                        ])))));

        Tap(tester, tester.ElementOfType<Slider>());
        Assert.Equal(0.5, value);
    }

    // Flutter: "Slider drags immediately (LTR)"
    [Fact]
    public void SliderDragsImmediatelyLtr()
    {
        using FrameworkDartTester tester = CreateTester();
        double value = 0.0;
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Center(
                        child: new Slider(value: value, onChanged: newValue => value = newValue))))));

        Point center = tester.GetCenter(tester.ElementOfType<Slider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);

        Assert.Equal(0.5, value);

        gesture.MoveBy(new Vector(1.0, 0.0));

        Assert.True(value > 0.5);

        gesture.Up();
    }

    // Flutter: "Slider drags immediately (RTL)"
    [Fact]
    public void SliderDragsImmediatelyRtl()
    {
        using FrameworkDartTester tester = CreateTester();
        double value = 0.0;
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Rtl,
                child: new MaterialWidget(
                    child: new Center(
                        child: new Slider(value: value, onChanged: newValue => value = newValue))))));

        Point center = tester.GetCenter(tester.ElementOfType<Slider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);

        Assert.Equal(0.5, value);

        gesture.MoveBy(new Vector(1.0, 0.0));

        Assert.True(value < 0.5);

        gesture.Up();
    }

    // Flutter: "Slider onChangeStart and onChangeEnd fire once"
    [Fact]
    public void SliderOnChangeStartAndOnChangeEndFireOnce()
    {
        // Regression test for https://github.com/flutter/flutter/issues/28115
        using FrameworkDartTester tester = CreateTester();
        int startFired = 0;
        int endFired = 0;
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Center(
                        child: new GestureDetector(
                            onHorizontalDragUpdate: _ => { },
                            child: new Slider(
                                value: 0.0,
                                onChanged: _ => { },
                                onChangeStart: _ => startFired += 1,
                                onChangeEnd: _ => endFired += 1)))))));

        tester.TimedDrag(tester.ElementOfType<Slider>(), new Vector(20.0, 0.0), TimeSpan.FromMilliseconds(100));

        Assert.Equal(1, startFired);
        Assert.Equal(1, endFired);
    }

    // Flutter: "Slider sizing"
    [Fact]
    public void SliderSizing()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(child: new Center(child: new Slider(value: 0.5, onChanged: null))))));
        Assert.Equal(new Size(800.0, 600.0), SliderBox(tester).Size);

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Center(child: new IntrinsicWidth(child: new Slider(value: 0.5, onChanged: null)))))));
        Assert.Equal(new Size(144.0 + 2.0 * 24.0, 600.0), SliderBox(tester).Size);

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Center(
                        child: new OverflowBox(
                            maxWidth: double.PositiveInfinity,
                            maxHeight: double.PositiveInfinity,
                            child: new Slider(value: 0.5, onChanged: null)))))));
        Assert.Equal(new Size(144.0 + 2.0 * 24.0, 48.0), SliderBox(tester).Size);
    }

    // Flutter: "Slider respects textScaleFactor"
    [Fact]
    public void SliderRespectsTextScaleFactor()
    {
        bool previousDisableShadows = RenderingDebug.DisableShadows;
        RenderingDebug.DisableShadows = false;
        try
        {
            using FrameworkDartTester tester = CreateTester();
            var sliderKey = new UniqueKey();
            double value = 0.0;

            Widget BuildSlider(
                double textScaleFactor,
                bool isDiscrete = true,
                ShowValueIndicator show = ShowValueIndicator.OnlyForDiscrete)
            {
                return new MaterialApp(
                    theme: new ThemeData(useMaterial3: false),
                    home: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new StatefulBuilder((context, setState) => new MediaQuery(
                            data: new MediaQueryData(TextScaler: TextScaler.Linear(textScaleFactor)),
                            child: new MaterialWidget(
                                child: new Theme(
                                    // Dart: `Theme.of(context).copyWith(sliderTheme: ...)`.
                                    data: Theme.Of(context) with
                                    {
                                        SliderTheme = Theme.Of(context).SliderTheme.CopyWith(showValueIndicator: show),
                                    },
                                    child: new Center(
                                        child: new OverflowBox(
                                            maxWidth: double.PositiveInfinity,
                                            maxHeight: double.PositiveInfinity,
                                            child: new Slider(
                                                key: sliderKey,
                                                max: 100.0,
                                                divisions: isDiscrete ? 10 : null,
                                                label: $"{Math.Round(value, MidpointRounding.AwayFromZero)}",
                                                value: value,
                                                onChanged: newValue => setState(() => value = newValue))))))))));
            }

            void ExpectIndicator(double leftX)
            {
                PaintAssert.Paints(
                    tester.ElementOfType<Overlay>().FindRenderObject()!,
                    PaintPattern.Paints
                        .Path(
                            includes:
                            [
                                new Point(0.0, 0.0),
                                new Point(0.0, -8.0),
                                new Point(leftX, -16.0),
                                new Point(-216.0, -16.0),
                            ],
                            color: new Color(0xf55f5f5f))
                        .Paragraph());
            }

            tester.PumpWidget(BuildSlider(textScaleFactor: 1.0));
            Point center = tester.GetCenter(tester.ElementOfType<Slider>());
            TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
            tester.PumpAndSettle();

            ExpectIndicator(-276.0);

            gesture.Up();
            tester.PumpAndSettle();

            tester.PumpWidget(BuildSlider(textScaleFactor: 2.0));
            center = tester.GetCenter(tester.ElementOfType<Slider>());
            gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
            tester.PumpAndSettle();

            ExpectIndicator(-304.0);

            gesture.Up();
            tester.PumpAndSettle();

            // Check continuous
            tester.PumpWidget(BuildSlider(
                textScaleFactor: 1.0,
                isDiscrete: false,
                show: ShowValueIndicator.OnlyForContinuous));
            center = tester.GetCenter(tester.ElementOfType<Slider>());
            gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
            tester.PumpAndSettle();

            ExpectIndicator(-276.0);

            gesture.Up();
            tester.PumpAndSettle();

            tester.PumpWidget(BuildSlider(
                textScaleFactor: 2.0,
                isDiscrete: false,
                show: ShowValueIndicator.OnlyForContinuous));
            center = tester.GetCenter(tester.ElementOfType<Slider>());
            gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
            tester.PumpAndSettle();

            ExpectIndicator(-276.0);

            gesture.Up();
            tester.PumpAndSettle();
        }
        finally
        {
            RenderingDebug.DisableShadows = previousDisableShadows;
        }
    }

    // Flutter: "Slider value indicator respects bold text"
    [Fact]
    public void SliderValueIndicatorRespectsBoldText()
    {
        using FrameworkDartTester tester = CreateTester();
        var sliderKey = new UniqueKey();
        double value = 0.0;
        var log = new List<InlineSpan>();
        var loggingValueIndicatorShape = new LoggingValueIndicatorShape(log);

        Widget BuildSlider(bool boldText = false)
        {
            return new MaterialApp(
                home: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new StatefulBuilder((context, setState) => new MediaQuery(
                        data: new MediaQueryData(BoldText: boldText),
                        child: new MaterialWidget(
                            child: new Theme(
                                // Dart: `Theme.of(context).copyWith(sliderTheme: ...)`.
                                data: Theme.Of(context) with
                                {
                                    SliderTheme = Theme.Of(context).SliderTheme.CopyWith(
#pragma warning disable CS0618 // ShowValueIndicator.Always is deprecated, as in Dart.
                                        showValueIndicator: ShowValueIndicator.Always,
#pragma warning restore CS0618
                                        valueIndicatorShape: loggingValueIndicatorShape),
                                },
                                child: new Center(
                                    child: new OverflowBox(
                                        maxWidth: double.PositiveInfinity,
                                        maxHeight: double.PositiveInfinity,
                                        child: new Slider(
                                            key: sliderKey,
                                            max: 100.0,
                                            divisions: 4,
                                            label: $"{Math.Round(value, MidpointRounding.AwayFromZero)}",
                                            value: value,
                                            onChanged: newValue => setState(() => value = newValue))))))))));
        }

        // Normal text
        tester.PumpWidget(BuildSlider());
        Point center = tester.GetCenter(tester.ElementOfType<Slider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
        tester.PumpAndSettle();

        Assert.Equal("50", log[^1].ToPlainText());
        Assert.Equal(FontWeight.Medium, log[^1].Style!.FontWeight);

        gesture.Up();
        tester.PumpAndSettle();

        // Bold text
        tester.PumpWidget(BuildSlider(boldText: true));
        center = tester.GetCenter(tester.ElementOfType<Slider>());
        gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
        tester.PumpAndSettle();

        Assert.Equal("50", log[^1].ToPlainText());
        Assert.Equal(FontWeight.Bold, log[^1].Style!.FontWeight);

        gesture.Up();
        tester.PumpAndSettle();
    }

    // Flutter: "Tick marks are skipped when they are too dense"
    [Fact]
    public void TickMarksAreSkippedWhenTheyAreTooDense()
    {
        using FrameworkDartTester tester = CreateTester();
        Widget BuildSlider(int divisions)
        {
            return new MaterialApp(
                home: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new MaterialWidget(
                        child: new Center(
                            child: new Slider(
                                max: 100.0,
                                divisions: divisions,
                                value: 0.25,
                                onChanged: _ => { })))));
        }

        // Pump a slider with a reasonable amount of divisions to verify that the
        // tick marks are drawn when the number of tick marks is not too dense.
        tester.PumpWidget(BuildSlider(divisions: 4));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        // 5 tick marks and a thumb.
        Assert.Equal(6, PaintRecording.Record(material).CountCalls("drawCircle"));

        // 200 divisions will produce a tick interval off less than 6,
        // which would be too dense to draw.
        tester.PumpWidget(BuildSlider(divisions: 200));

        // No tick marks are drawn because they are too dense, but the thumb is
        // still drawn.
        Assert.Equal(1, PaintRecording.Record(material).CountCalls("drawCircle"));
    }

    // Flutter: "Slider has correct animations when reparented"
    [Fact]
    public void SliderHasCorrectAnimationsWhenReparented()
    {
        using FrameworkDartTester tester = CreateTester();
        Key sliderKey = new LabeledGlobalKey<State>("A");
        double value = 0.0;

        Widget BuildSlider(int parents)
        {
            Widget CreateParents(int count, StateSetter setState)
            {
                Widget slider = new Slider(
                    key: sliderKey,
                    value: value,
                    divisions: 4,
                    onChanged: newValue => setState(() => value = newValue));

                for (int i = 0; i < count; ++i)
                {
                    slider = new Column(children: [slider]);
                }

                return slider;
            }

            return new MaterialApp(
                home: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new StatefulBuilder((context, setState) =>
                        new MaterialWidget(child: CreateParents(parents, setState)))));
        }

        PaintPattern Ticks(PaintPattern pattern) => pattern
            .Circle(x: 26.0, y: 24.0, radius: 1.0)
            .Circle(x: 213.0, y: 24.0, radius: 1.0)
            .Circle(x: 400.0, y: 24.0, radius: 1.0)
            .Circle(x: 587.0, y: 24.0, radius: 1.0)
            .Circle(x: 774.0, y: 24.0, radius: 1.0);

        void TestReparenting(bool reparent)
        {
            RenderObject material = MaterialOf(tester.ElementOfType<Slider>());
            Point center = tester.GetCenter(tester.ElementOfType<Slider>());
            // Move to 0.0.
            TestGesture gesture = tester.StartGesture(default, PointerDeviceKind.Touch);
            tester.Pump();
            gesture.Up();
            tester.PumpAndSettle();
            Assert.Equal(0, Scheduler.TransientCallbackCount);
            PaintAssert.Paints(material, Ticks(PaintPattern.Paints).Circle(x: 26.0, y: 24.0, radius: 10.0));

            gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
            tester.Pump();
            // Wait for animations to start.
            tester.Pump(TimeSpan.FromMilliseconds(25));
            Assert.Equal(2, Scheduler.TransientCallbackCount);
            PaintAssert.Paints(
                material,
                Ticks(PaintPattern.Paints.Circle(x: 112.7431640625, y: 24.0, radius: 5.687664985656738))
                    .Circle(x: 112.7431640625, y: 24.0, radius: 10.0));

            // Reparenting in the middle of an animation should do nothing.
            if (reparent)
            {
                tester.PumpWidget(BuildSlider(2));
            }

            // Move a little further in the animations.
            tester.Pump(TimeSpan.FromMilliseconds(10));
            Assert.Equal(2, Scheduler.TransientCallbackCount);
            PaintAssert.Paints(
                material,
                Ticks(PaintPattern.Paints.Circle(x: 191.130521774292, y: 24.0, radius: 12.0))
                    .Circle(x: 191.130521774292, y: 24.0, radius: 10.0));
            // Wait for animations to finish.
            tester.PumpAndSettle();
            Assert.Equal(0, Scheduler.TransientCallbackCount);
            PaintAssert.Paints(
                material,
                Ticks(PaintPattern.Paints.Circle(x: 400.0, y: 24.0, radius: 24.0))
                    .Circle(x: 400.0, y: 24.0, radius: 10.0));
            gesture.Up();
            tester.PumpAndSettle();
            Assert.Equal(0, Scheduler.TransientCallbackCount);
            PaintAssert.Paints(material, Ticks(PaintPattern.Paints).Circle(x: 400.0, y: 24.0, radius: 10.0));
        }

        tester.PumpWidget(BuildSlider(1));
        // Do it once without reparenting in the middle of an animation
        TestReparenting(false);
        // Now do it again with reparenting in the middle of an animation.
        TestReparenting(true);
    }

    private const SemanticsFlags EnabledSliderFlags = SemanticsFlags.HasEnabledState
                                                      | SemanticsFlags.IsEnabled
                                                      | SemanticsFlags.IsFocusable
                                                      | SemanticsFlags.IsSlider;

    private const SemanticsFlags DisabledSliderFlags = SemanticsFlags.HasEnabledState
                                                       | SemanticsFlags.IsFocusable
                                                       | SemanticsFlags.IsSlider;

    private const SemanticsActions EnabledSliderActions = SemanticsActions.Increase
                                                          | SemanticsActions.Decrease
                                                          | SemanticsActions.Focus;

    // The MaterialApp scaffolding every semantics expectation below nests the slider in.
    private static TestSemantics AppRoot(int routeChildId, params TestSemantics[] sliderNodes) =>
        TestSemantics.Root(
        [
            new TestSemantics(
                id: 1,
                textDirection: TextDirection.Ltr,
                children:
                [
                    new TestSemantics(
                        id: 2,
                        children:
                        [
                            new TestSemantics(
                                id: 3,
                                flags: SemanticsFlags.ScopesRoute,
                                children: [new TestSemantics(id: routeChildId, children: sliderNodes)]),
                        ]),
                ]),
        ]);

    // Flutter: "Slider Semantics" (variant: android, fuchsia, linux)
    [Theory]
    [MemberData(nameof(AndroidFuchsiaLinux))]
    public void SliderSemanticsAndroidFuchsiaLinux(TargetPlatform platform)
    {
        using FrameworkDartTester tester = CreateTester(platform);
        using var semantics = new SemanticsTester(tester);

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(child: new Slider(value: 0.5, onChanged: _ => { })))));

        tester.PumpAndSettle();

        semantics.ExpectHasSemantics(AppRoot(
            4,
            new TestSemantics(id: 6),
            new TestSemantics(
                id: 5,
                flags: EnabledSliderFlags,
                actions: EnabledSliderActions,
                value: "50%",
                increasedValue: "55%",
                decreasedValue: "45%",
                textDirection: TextDirection.Ltr)));

        // Disable slider
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(child: new Slider(value: 0.5, onChanged: null)))));

        TestSemantics disabled = AppRoot(
            7,
            new TestSemantics(id: 6),
            new TestSemantics(
                id: 5,
                flags: DisabledSliderFlags,
                value: "50%",
                increasedValue: "55%",
                decreasedValue: "45%",
                textDirection: TextDirection.Ltr));
        semantics.ExpectHasSemantics(disabled);

        tester.Pump();
        semantics.ExpectHasSemantics(disabled);
    }

    // Flutter: "Slider Semantics" (variant: iOS, macOS)
    [Theory]
    [MemberData(nameof(IOSAndMacOS))]
    public void SliderSemanticsIOSMacOS(TargetPlatform platform)
    {
        using FrameworkDartTester tester = CreateTester(platform);
        using var semantics = new SemanticsTester(tester);

        tester.PumpWidget(new MaterialApp(
            home: new Theme(
                data: new ThemeData(),
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new MaterialWidget(
                        child: new Slider(value: 100.0, max: 200.0, onChanged: _ => { }))))));

        semantics.ExpectHasSemantics(AppRoot(
            4,
            new TestSemantics(id: 6),
            new TestSemantics(
                id: 5,
                flags: EnabledSliderFlags,
                actions: EnabledSliderActions,
                value: "50%",
                increasedValue: "60%",
                decreasedValue: "40%",
                textDirection: TextDirection.Ltr)));

        // Disable slider
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(child: new Slider(value: 0.5, onChanged: null)))));

        semantics.ExpectHasSemantics(AppRoot(
            7,
            new TestSemantics(id: 9),
            new TestSemantics(
                id: 8,
                flags: DisabledSliderFlags,
                value: "50%",
                increasedValue: "60%",
                decreasedValue: "40%",
                textDirection: TextDirection.Ltr)));
    }

    // Flutter: "Slider Semantics" (variant: windows)
    [Fact]
    public void SliderSemanticsWindows()
    {
        using FrameworkDartTester tester = CreateTester(TargetPlatform.Windows);
        using var semantics = new SemanticsTester(tester);

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(child: new Slider(value: 0.5, onChanged: _ => { })))));

        tester.PumpAndSettle();

        semantics.ExpectHasSemantics(AppRoot(
            4,
            new TestSemantics(id: 6),
            new TestSemantics(
                id: 5,
                flags: EnabledSliderFlags,
                actions: EnabledSliderActions | SemanticsActions.DidGainAccessibilityFocus,
                value: "50%",
                increasedValue: "55%",
                decreasedValue: "45%",
                textDirection: TextDirection.Ltr)));

        // Disable slider
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(child: new Slider(value: 0.5, onChanged: null)))));

        TestSemantics disabled = AppRoot(
            7,
            new TestSemantics(id: 6),
            new TestSemantics(
                id: 5,
                flags: DisabledSliderFlags,
                actions: SemanticsActions.DidGainAccessibilityFocus,
                value: "50%",
                increasedValue: "55%",
                decreasedValue: "45%",
                textDirection: TextDirection.Ltr));
        semantics.ExpectHasSemantics(disabled);

        tester.Pump();
        semantics.ExpectHasSemantics(disabled);
    }

    // Flutter: "Slider semantics with custom formatter"
    [Fact]
    public void SliderSemanticsWithCustomFormatter()
    {
        using FrameworkDartTester tester = CreateTester();
        using var semantics = new SemanticsTester(tester);

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Slider(
                        value: 40.0,
                        max: 200.0,
                        divisions: 10,
                        semanticFormatterCallback: v => DartRound(v).ToString(),
                        onChanged: _ => { })))));

        semantics.ExpectHasSemantics(AppRoot(
            4,
            new TestSemantics(
                id: 5,
                flags: EnabledSliderFlags,
                actions: EnabledSliderActions,
                value: "40",
                increasedValue: "60",
                decreasedValue: "20",
                textDirection: TextDirection.Ltr),
            new TestSemantics(id: 6)));
    }

    // Regression test for https://github.com/flutter/flutter/issues/101868
    // Flutter: "Slider.label info should not write to semantic node"
    [Fact]
    public void SliderLabelInfoShouldNotWriteToSemanticNode()
    {
        using FrameworkDartTester tester = CreateTester();
        using var semantics = new SemanticsTester(tester);

        const string label = "Bingo";
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MaterialWidget(
                    child: new Slider(
                        value: 40.0,
                        max: 200.0,
                        divisions: 10,
                        semanticFormatterCallback: v => DartRound(v).ToString(),
                        onChanged: _ => { },
                        label: label)))));

        semantics.ExpectHasSemantics(AppRoot(
            4,
            new TestSemantics(
                id: 5,
                flags: EnabledSliderFlags,
                actions: EnabledSliderActions,
                label: "Bingo",
                value: "40",
                increasedValue: "60",
                decreasedValue: "20",
                textDirection: TextDirection.Ltr),
            new TestSemantics(id: 6)));
    }

    // Flutter: "Material3 - Slider is focusable and has correct focus color"
    [Fact]
    public void Material3SliderIsFocusableAndHasCorrectFocusColor()
    {
        using FrameworkDartTester tester = CreateTester();
        var focusNode = new FocusNode(debugLabel: "Slider");
        try
        {
            FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
            var theme = new ThemeData();
            double value = 0.5;
            Widget BuildApp(bool enabled = true)
            {
                return new MaterialApp(
                    theme: theme,
                    home: new MaterialWidget(
                        child: new Center(
                            child: new StatefulBuilder((context, setState) => new Slider(
                                value: value,
                                onChanged: enabled ? newValue => setState(() => value = newValue) : null,
                                autofocus: true,
                                focusNode: focusNode)))));
            }

            tester.PumpWidget(BuildApp());

            // Check that the overlay shows when focused.
            tester.PumpAndSettle();
            Assert.True(focusNode.HasPrimaryFocus);
            PaintAssert.Paints(
                MaterialOf(tester.ElementOfType<Slider>()),
                PaintPattern.Paints.Circle(color: theme.ColorScheme.Primary.WithOpacity(0.1)));

            // Check that the overlay does not show when unfocused and disabled.
            tester.PumpWidget(BuildApp(enabled: false));
            tester.PumpAndSettle();
            Assert.False(focusNode.HasPrimaryFocus);
            PaintAssert.DoesNotPaint(
                MaterialOf(tester.ElementOfType<Slider>()),
                PaintPattern.Paints.Circle(color: theme.ColorScheme.Primary.WithOpacity(0.1)));
        }
        finally
        {
            focusNode.Dispose();
        }
    }

    // Flutter: "Slider has correct focus color from overlayColor property"
    [Fact]
    public void SliderHasCorrectFocusColorFromOverlayColorProperty()
    {
        using FrameworkDartTester tester = CreateTester();
        var focusNode = new FocusNode(debugLabel: "Slider");
        try
        {
            FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
            double value = 0.5;
            Widget BuildApp(bool enabled = true)
            {
                return new MaterialApp(
                    home: new MaterialWidget(
                        child: new Center(
                            child: new StatefulBuilder((context, setState) => new Slider(
                                value: value,
                                // Dart passes `WidgetStateColor.resolveWith`; `Slider.OverlayColor` is a
                                // `WidgetStateProperty<Color?>`, which a C# `WidgetStateColor` is not.
                                overlayColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                                    states.Contains(WidgetState.Focused)
                                        ? MaterialColors.Purple.Shade500
                                        : MaterialColors.Transparent),
                                onChanged: enabled ? newValue => setState(() => value = newValue) : null,
                                autofocus: true,
                                focusNode: focusNode)))));
            }

            tester.PumpWidget(BuildApp());

            // Check that the overlay shows when focused.
            tester.PumpAndSettle();
            Assert.True(focusNode.HasPrimaryFocus);
            PaintAssert.Paints(
                MaterialOf(tester.ElementOfType<Slider>()),
                PaintPattern.Paints.Circle(color: MaterialColors.Purple.Shade500));

            // Check that the overlay does not show when focused and disabled.
            tester.PumpWidget(BuildApp(enabled: false));
            tester.PumpAndSettle();
            Assert.False(focusNode.HasPrimaryFocus);
            PaintAssert.DoesNotPaint(
                MaterialOf(tester.ElementOfType<Slider>()),
                PaintPattern.Paints.Circle(color: MaterialColors.Purple.Shade500));
        }
        finally
        {
            focusNode.Dispose();
        }
    }

    // Flutter: "Slider can be hovered and has correct hover color"
    [Fact]
    public void SliderCanBeHoveredAndHasCorrectHoverColor()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var theme = new ThemeData();
        double value = 0.5;
        Widget BuildApp(bool enabled = true)
        {
            return new MaterialApp(
                theme: theme,
                home: new MaterialWidget(
                    child: new Center(
                        child: new StatefulBuilder((context, setState) => new Slider(
                            value: value,
                            onChanged: enabled ? newValue => setState(() => value = newValue) : null)))));
        }

        tester.PumpWidget(BuildApp());

        // Slider does not have overlay when enabled and not hovered.
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(
            MaterialOf(tester.ElementOfType<Slider>()),
            PaintPattern.Paints.Circle(color: MaterialColors.Orange.Shade500));

        // Start hovering.
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(tester.ElementOfType<Slider>()));

        // Slider has overlay when enabled and hovered.
        tester.PumpWidget(BuildApp());
        tester.PumpAndSettle();
        PaintAssert.Paints(
            MaterialOf(tester.ElementOfType<Slider>()),
            PaintPattern.Paints.Circle(color: theme.ColorScheme.Primary.WithOpacity(0.08)));

        // Slider still shows correct hovered color after pressing/dragging
        gesture.Down(tester.GetCenter(tester.ElementOfType<Slider>()));
        tester.Pump();
        gesture.Up();
        tester.PumpAndSettle();
        gesture.MoveTo(new Point(0.0, 100.0));
        tester.PumpAndSettle();
        gesture.MoveTo(tester.GetCenter(tester.ElementOfType<Slider>()));
        tester.PumpAndSettle();
        PaintAssert.Paints(
            MaterialOf(tester.ElementOfType<Slider>()),
            PaintPattern.Paints.Circle(color: theme.ColorScheme.Primary.WithOpacity(0.08)));

        // Slider does not have an overlay when disabled and hovered.
        tester.PumpWidget(BuildApp(enabled: false));
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(
            MaterialOf(tester.ElementOfType<Slider>()),
            PaintPattern.Paints.Circle(color: MaterialColors.Orange.Shade500));
    }

    // Flutter: "Slider has correct hovered color from overlayColor property"
    [Fact]
    public void SliderHasCorrectHoveredColorFromOverlayColorProperty()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        double value = 0.5;
        Widget BuildApp(bool enabled = true)
        {
            return new MaterialApp(
                home: new MaterialWidget(
                    child: new Center(
                        child: new StatefulBuilder((context, setState) => new Slider(
                            value: value,
                            // Dart passes `WidgetStateColor.resolveWith`; `Slider.OverlayColor` is a
                            // `WidgetStateProperty<Color?>`, which a C# `WidgetStateColor` is not.
                            overlayColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                                states.Contains(WidgetState.Hovered)
                                    ? MaterialColors.Cyan.Shade500
                                    : MaterialColors.Transparent),
                            onChanged: enabled ? newValue => setState(() => value = newValue) : null)))));
        }

        tester.PumpWidget(BuildApp());

        // Slider does not have overlay when enabled and not hovered.
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(
            MaterialOf(tester.ElementOfType<Slider>()),
            PaintPattern.Paints.Circle(color: MaterialColors.Cyan.Shade500));

        // Start hovering.
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(tester.ElementOfType<Slider>()));

        // Slider has overlay when enabled and hovered.
        tester.PumpWidget(BuildApp());
        tester.PumpAndSettle();
        PaintAssert.Paints(
            MaterialOf(tester.ElementOfType<Slider>()),
            PaintPattern.Paints.Circle(color: MaterialColors.Cyan.Shade500));

        // Slider does not have an overlay when disabled and hovered.
        tester.PumpWidget(BuildApp(enabled: false));
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(
            MaterialOf(tester.ElementOfType<Slider>()),
            PaintPattern.Paints.Circle(color: MaterialColors.Cyan.Shade500));
    }

    // Flutter: "Material3 - Slider is draggable and has correct dragged color"
    [Fact]
    public void Material3SliderIsDraggableAndHasCorrectDraggedColor()
    {
        using FrameworkDartTester tester = CreateTester();
        var focusNode = new FocusNode();
        try
        {
            FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
            double value = 0.5;
            var theme = new ThemeData();
            var sliderKey = new UniqueKey();

            Widget BuildApp(bool enabled = true)
            {
                return new MaterialApp(
                    theme: theme,
                    home: new MaterialWidget(
                        child: new Center(
                            child: new StatefulBuilder((context, setState) => new Slider(
                                key: sliderKey,
                                value: value,
                                focusNode: focusNode,
                                onChanged: enabled ? newValue => setState(() => value = newValue) : null)))));
            }

            tester.PumpWidget(BuildApp());

            // Slider does not have overlay when enabled and not dragged.
            tester.PumpAndSettle();
            PaintAssert.DoesNotPaint(
                MaterialOf(tester.ElementOfType<Slider>()),
                PaintPattern.Paints.Circle(color: theme.ColorScheme.Primary.WithOpacity(0.1)));

            // Start dragging.
            TestGesture drag = tester.StartGesture(
                tester.GetCenter(tester.ElementsWithKey(sliderKey).Single()),
                PointerDeviceKind.Touch);
            tester.Pump(GestureConstants.PressTimeout);

            // Less than configured touch slop, more than default touch slop
            drag.MoveBy(new Vector(19.0, 0));
            tester.Pump();

            // Slider has overlay when enabled and dragged.
            PaintAssert.Paints(
                MaterialOf(tester.ElementOfType<Slider>()),
                PaintPattern.Paints.Circle(color: theme.ColorScheme.Primary.WithOpacity(0.1)));

            drag.Up();
            tester.PumpAndSettle();

            // Slider without focus doesn't have overlay when enabled and dragged.
            Assert.False(focusNode.HasFocus);
            PaintAssert.DoesNotPaint(
                MaterialOf(tester.ElementOfType<Slider>()),
                PaintPattern.Paints.Circle(color: theme.ColorScheme.Primary.WithOpacity(0.1)));

            // Slider has overlay when enabled, dragged and focused.
            focusNode.RequestFocus();
            tester.PumpAndSettle();

            Assert.True(focusNode.HasFocus);
            PaintAssert.Paints(
                MaterialOf(tester.ElementOfType<Slider>()),
                PaintPattern.Paints.Circle(color: theme.ColorScheme.Primary.WithOpacity(0.1)));
        }
        finally
        {
            focusNode.Dispose();
        }
    }

    // Flutter: "Slider has correct dragged color from overlayColor property"
    [Fact]
    public void SliderHasCorrectDraggedColorFromOverlayColorProperty()
    {
        using FrameworkDartTester tester = CreateTester();
        var focusNode = new FocusNode();
        try
        {
            FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
            double value = 0.5;
            var sliderKey = new UniqueKey();

            Widget BuildApp(bool enabled = true)
            {
                return new MaterialApp(
                    home: new MaterialWidget(
                        child: new Center(
                            child: new StatefulBuilder((context, setState) => new Slider(
                                key: sliderKey,
                                value: value,
                                focusNode: focusNode,
                                // Dart passes `WidgetStateColor.resolveWith`; `Slider.OverlayColor` is a
                                // `WidgetStateProperty<Color?>`, which a C# `WidgetStateColor` is not.
                                overlayColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                                    states.Contains(WidgetState.Dragged)
                                        ? MaterialColors.Lime.Shade500
                                        : MaterialColors.Transparent),
                                onChanged: enabled ? newValue => setState(() => value = newValue) : null)))));
            }

            tester.PumpWidget(BuildApp());

            // Slider does not have overlay when enabled and not dragged.
            tester.PumpAndSettle();
            PaintAssert.DoesNotPaint(
                MaterialOf(tester.ElementOfType<Slider>()),
                PaintPattern.Paints.Circle(color: MaterialColors.Lime.Shade500));

            // Start dragging.
            TestGesture drag = tester.StartGesture(
                tester.GetCenter(tester.ElementsWithKey(sliderKey).Single()),
                PointerDeviceKind.Touch);
            tester.Pump(GestureConstants.PressTimeout);

            // Less than configured touch slop, more than default touch slop
            drag.MoveBy(new Vector(19.0, 0));
            tester.Pump();

            // Slider has overlay when enabled and dragged.
            PaintAssert.Paints(
                MaterialOf(tester.ElementOfType<Slider>()),
                PaintPattern.Paints.Circle(color: MaterialColors.Lime.Shade500));

            drag.Up();
            tester.PumpAndSettle();

            // Slider without focus doesn't have overlay when enabled and dragged.
            Assert.False(focusNode.HasFocus);
            PaintAssert.DoesNotPaint(
                MaterialOf(tester.ElementOfType<Slider>()),
                PaintPattern.Paints.Circle(color: MaterialColors.Lime.Shade500));
        }
        finally
        {
            focusNode.Dispose();
        }
    }


    // Dart's `double.round()`: half away from zero.
    private static int DartRound(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

    // flutter_test's `tester.tapAt`: a down and an up at `location`, no pump.
    private static void TapAt(FrameworkDartTester tester, Point location)
    {
        TestGesture gesture = tester.StartGesture(location, PointerDeviceKind.Touch);
        gesture.Up();
    }

    // flutter_test's `tester.tap(finder)`.
    private static void Tap(FrameworkDartTester tester, Element element) => TapAt(tester, tester.GetCenter(element));

    // `tester.renderObject<RenderBox>(find.byType(Slider))`.
    private static RenderBox SliderBox(FrameworkDartTester tester) =>
        (RenderBox)tester.ElementOfType<Slider>().FindRenderObject()!;

    // `Material.of(context)` as a paint target: the Material's ink-feature render object.
    private static RenderObject MaterialOf(Element element) =>
        LookupBoundary.FindAncestorRenderObjectOfType<RenderInkFeatures>(element)
        ?? throw new XunitException("no Material ancestor");

    /// <summary>slider_test.dart: <c>LoggingThumbShape</c>, a thumb shape that also logs its repaint center.</summary>
    private sealed class LoggingThumbShape(List<Point> log) : SliderComponentShape
    {
        public override Size GetPreferredSize(bool isEnabled, bool isDiscrete) => new(10.0, 10.0);

        public override void Paint(
            PaintingContext context,
            Point center,
            Animation<double> activationAnimation,
            Animation<double> enableAnimation,
            bool isDiscrete,
            TextPainter labelPainter,
            RenderBox parentBox,
            SliderThemeData sliderTheme,
            TextDirection textDirection,
            double value,
            double textScaleFactor,
            Size sizeWithOverflow)
        {
            log.Add(center);
            var thumbPaint = new Avalonia.Media.SolidColorBrush(MaterialColors.Red);
            context.Canvas.DrawCircle(thumbPaint, null, center, 5.0);
        }
    }

    /// <summary>slider_test.dart: <c>LoggingValueIndicatorShape</c>, logs the label painter's text.</summary>
    private sealed class LoggingValueIndicatorShape(List<InlineSpan> logLabel) : SliderComponentShape
    {
        public override Size GetPreferredSize(bool isEnabled, bool isDiscrete) => new(10.0, 10.0);

        public override void Paint(
            PaintingContext context,
            Point center,
            Animation<double> activationAnimation,
            Animation<double> enableAnimation,
            bool isDiscrete,
            TextPainter labelPainter,
            RenderBox parentBox,
            SliderThemeData sliderTheme,
            TextDirection textDirection,
            double value,
            double textScaleFactor,
            Size sizeWithOverflow)
        {
            logLabel.Add(labelPainter.Text!);
        }
    }

    /// <summary>semantics_tester.dart's <c>SemanticsTester</c>: keeps semantics on for the view.</summary>
    private sealed class SemanticsTester : IDisposable
    {
        private readonly FrameworkDartTester _tester;
        private SemanticsHandle? _handle;

        public SemanticsTester(FrameworkDartTester tester)
        {
            _tester = tester;
            PipelineOwner owner = ViewPipelineOwner(tester);
            bool createsOwner = owner.SemanticsOwner is null;
            _handle = owner.EnsureSemantics();
            if (createsOwner)
            {
                tester.RenderView.ClearSemantics();
                tester.RenderView.ScheduleInitialSemantics();
            }
        }

        private SemanticsOwner Owner => ViewPipelineOwner(_tester).SemanticsOwner!;

        /// <summary>
        /// Dart's <c>expect(semantics, hasSemantics(expected, ignoreRect: true, ignoreTransform: true))</c>
        /// (ids compared, traversal child order: hasSemantics' default).
        /// </summary>
        public void ExpectHasSemantics(TestSemantics expected)
        {
            // flutter_test's frame ends with `flushSemantics`; make sure the last pump's is done.
            ViewPipelineOwner(_tester).FlushSemantics();
            string? failure = expected.Match(Owner.RootNode!, "root");
            if (failure != null)
            {
                throw new XunitException($"{failure}\n{Owner.DebugDumpTree()}");
            }
        }

        public void Dispose()
        {
            _handle?.Dispose();
            _handle = null;
        }

        private static PipelineOwner ViewPipelineOwner(FrameworkDartTester tester) => tester.RenderView.Owner!;
    }

    /// <summary>semantics_tester.dart's <c>TestSemantics</c>, reduced to what these tests compare.</summary>
    private sealed class TestSemantics(
        int id,
        SemanticsFlags flags = SemanticsFlags.None,
        SemanticsActions actions = SemanticsActions.None,
        string label = "",
        string value = "",
        string increasedValue = "",
        string decreasedValue = "",
        TextDirection? textDirection = null,
        IReadOnlyList<TestSemantics>? children = null)
    {
        private readonly IReadOnlyList<TestSemantics> _children = children ?? [];

        public static TestSemantics Root(IReadOnlyList<TestSemantics> children) => new(0, children: children);

        public string? Match(SemanticsNode node, string path)
        {
            SemanticsData data = node.GetSemanticsData();
            var errors = new StringBuilder();
            if (node.Id != id)
            {
                errors.Append($" expected node id {id} but found id {node.Id};");
            }

            if (data.Flags != flags)
            {
                errors.Append($" flags: expected {flags} but found {data.Flags};");
            }

            if (data.Actions != actions)
            {
                errors.Append($" actions: expected {actions} but found {data.Actions};");
            }

            if (data.Label != label)
            {
                errors.Append($" label: expected \"{label}\" but found \"{data.Label}\";");
            }

            if (data.Value != value)
            {
                errors.Append($" value: expected \"{value}\" but found \"{data.Value}\";");
            }

            if (data.IncreasedValue != increasedValue)
            {
                errors.Append($" increasedValue: expected \"{increasedValue}\" but found \"{data.IncreasedValue}\";");
            }

            if (data.DecreasedValue != decreasedValue)
            {
                errors.Append($" decreasedValue: expected \"{decreasedValue}\" but found \"{data.DecreasedValue}\";");
            }

            if (data.Hint != string.Empty || data.Tooltip != string.Empty)
            {
                errors.Append(" expected no hint/tooltip;");
            }

            if (textDirection != null && textDirection != data.TextDirection)
            {
                errors.Append($" textDirection: expected {textDirection} but found {data.TextDirection};");
            }

            if ((data.Label != string.Empty || data.Value != string.Empty || data.Hint != string.Empty
                 || data.IncreasedValue != string.Empty || data.DecreasedValue != string.Empty)
                && data.TextDirection == null)
            {
                errors.Append(" a node with a label, value, or hint must have a textDirection;");
            }

            IReadOnlyList<SemanticsNode> nodeChildren =
                node.DebugListChildrenInOrder(DebugSemanticsDumpOrder.TraversalOrder);
            int childrenCount = node.MergeAllDescendantsIntoThisNode ? 0 : nodeChildren.Count;
            if (_children.Count != childrenCount)
            {
                errors.Append($" expected {_children.Count} children but found {childrenCount};");
            }

            if (errors.Length > 0)
            {
                return $"Node #{node.Id} at {path}:{errors}";
            }

            for (int i = 0; i < _children.Count; i += 1)
            {
                string? childFailure = _children[i].Match(nodeChildren[i], $"{path}/{i}");
                if (childFailure != null)
                {
                    return childFailure;
                }
            }

            return null;
        }
    }
}
