// Dart parity source: material_ui/lib/src/slider.dart
// Mirrors material-ui-src/test/slider_test.dart (lines 4024-5675)

using System.Globalization;
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
public sealed class SliderDartParityTestsC : IDisposable
{
    private readonly bool _previousDisableShadows;

    public SliderDartParityTestsC()
    {
        FocusManager.Instance.ResetForTests();
        // flutter_test runs every test with `debugDisableShadows == true`.
        _previousDisableShadows = RenderingDebug.DisableShadows;
        RenderingDebug.DisableShadows = true;
    }

    public void Dispose()
    {
        RenderingDebug.DisableShadows = _previousDisableShadows;
        FocusManager.Instance.ResetForTests();
    }

    // Flutter: "Overlay remains when Slider thumb is interacted"
    [Fact]
    public void OverlayRemainsWhenSliderThumbIsInteracted()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        double value = 0.5;
        var overlayColor = new Color(0xffff0000);
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder(
                        (context, setState) => new Slider(
                            value: value,
                            overlayColor: new WidgetStatePropertyAll<Color?>(overlayColor),
                            onChanged: newValue => setState(() => value = newValue)))))));

        // Slider does not have overlay when enabled and not tapped.
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: overlayColor));
        Point sliderCenter = tester.GetCenter(tester.ElementOfType<Slider>());
        // Tap and hold down on the thumb to keep it active.
        TestGesture gesture = tester.CreateGesture();
        gesture.AddPointer();
        gesture.Down(sliderCenter);
        tester.PumpAndSettle();
        PaintAssert.Paints(MaterialOf(tester), PaintPattern.Paints.Circle(color: overlayColor));
        // Hover on the slider but outside the thumb.
        gesture.MoveTo(tester.GetTopLeft(tester.ElementOfType<Slider>()));
        tester.PumpAndSettle();
        PaintAssert.Paints(MaterialOf(tester), PaintPattern.Paints.Circle(color: overlayColor));
        // Tap up on the slider.
        gesture.Up();
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: overlayColor));
    }

    // Flutter: "Overlay appear only when hovered on the thumb on desktop"
    [Theory]
    [MemberData(nameof(DesktopPlatforms))]
    public void OverlayAppearOnlyWhenHoveredOnTheThumbOnDesktop(TargetPlatform targetPlatform)
    {
        using PlatformOverride platform = new(targetPlatform);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        double value = 0.5;
        var overlayColor = new Color(0xffff0000);

        Widget BuildApp(bool enabled = true) => new MaterialApp(
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder(
                        (context, setState) => new Slider(
                            value: value,
                            overlayColor: new WidgetStatePropertyAll<Color?>(overlayColor),
                            onChanged: enabled ? newValue => setState(() => value = newValue) : null)))));

        tester.PumpWidget(BuildApp());

        // Slider does not have overlay when enabled and not hovered.
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: overlayColor));

        // Hover on the slider but outside the thumb.
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetTopLeft(tester.ElementOfType<Slider>()));

        tester.PumpWidget(BuildApp());
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: overlayColor));

        // Hover on the thumb.
        gesture.MoveTo(tester.GetCenter(tester.ElementOfType<Slider>()));
        tester.PumpAndSettle();
        PaintAssert.Paints(MaterialOf(tester), PaintPattern.Paints.Circle(color: overlayColor));

        // Hover on the slider but outside the thumb.
        gesture.MoveTo(tester.GetBottomRight(tester.ElementOfType<Slider>()));
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: overlayColor));
    }

    // Flutter: "Overlay remains when Slider is in focus on desktop"
    [Theory]
    [MemberData(nameof(DesktopPlatforms))]
    public void OverlayRemainsWhenSliderIsInFocusOnDesktop(TargetPlatform targetPlatform)
    {
        using PlatformOverride platform = new(targetPlatform);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        double value = 0.5;
        var overlayColor = new Color(0xffff0000);
        using var focusNode = new FocusNode();

        Widget BuildApp(bool enabled = true) => new MaterialApp(
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder(
                        (context, setState) => new Slider(
                            value: value,
                            focusNode: focusNode,
                            overlayColor: new WidgetStatePropertyAll<Color?>(overlayColor),
                            onChanged: enabled ? newValue => setState(() => value = newValue) : null)))));

        tester.PumpWidget(BuildApp());

        // Slider does not have overlay when enabled and not tapped.
        tester.PumpAndSettle();
        Assert.False(focusNode.HasFocus);
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: overlayColor));

        Point sliderCenter = tester.GetCenter(tester.ElementOfType<Slider>());
        var tapLocation = new Point(sliderCenter.X + 50, sliderCenter.Y);

        // Tap somewhere to bring overlay.
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.Down(tapLocation);
        gesture.Up();
        focusNode.RequestFocus();
        tester.PumpAndSettle();
        Assert.True(focusNode.HasFocus);
        PaintAssert.Paints(MaterialOf(tester), PaintPattern.Paints.Circle(color: overlayColor));

        tapLocation = new Point(sliderCenter.X - 50, sliderCenter.Y);
        gesture.Down(tapLocation);
        gesture.Up();
        tester.PumpAndSettle();
        Assert.True(focusNode.HasFocus);
        // Overlay is removed when adjusted with a tap.
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: overlayColor));
    }

    // Regression test for https://github.com/flutter/flutter/issues/123313, which only occurs on desktop platforms.
    // Flutter: "Value indicator disappears after adjusting the slider on desktop"
    [Theory]
    [MemberData(nameof(DesktopPlatforms))]
    public void ValueIndicatorDisappearsAfterAdjustingTheSliderOnDesktop(TargetPlatform targetPlatform)
    {
        using PlatformOverride platform = new(targetPlatform);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var theme = new ThemeData();
        const double currentValue = 0.5;
        tester.PumpWidget(new MaterialApp(
            theme: theme,
            home: new MaterialWidget(
                child: new Center(
                    child: new Slider(
                        value: currentValue,
                        divisions: 5,
                        label: currentValue.ToString("F1", CultureInfo.InvariantCulture),
                        onChanged: _ => { })))));

        // Slider does not show value indicator initially.
        tester.PumpAndSettle();
        RenderObject valueIndicatorBox = OverlayRenderObject(tester);
        PaintAssert.DoesNotPaint(
            valueIndicatorBox,
            PaintPattern.Paints.Scale().Path(color: theme.ColorScheme.Primary));

        Point sliderCenter = tester.GetCenter(tester.ElementOfType<Slider>());
        var tapLocation = new Point(sliderCenter.X + 50, sliderCenter.Y);

        // Tap the slider by mouse to bring up the value indicator.
        TapAt(tester, tapLocation, PointerDeviceKind.Mouse);
        tester.PumpAndSettle();

        // Value indicator is visible.
        valueIndicatorBox = OverlayRenderObject(tester);
        PaintAssert.Paints(valueIndicatorBox, PaintPattern.Paints.Scale().Path(color: theme.ColorScheme.Primary));

        // Wait for the value indicator to disappear.
        PumpAndSettle(tester, TimeSpan.FromSeconds(2));

        // Value indicator is no longer visible.
        PaintAssert.DoesNotPaint(
            valueIndicatorBox,
            PaintPattern.Paints.Scale().Path(color: theme.ColorScheme.Primary));
    }

    // Flutter: "Value indicator remains when Slider is in focus on desktop"
    [Theory]
    [MemberData(nameof(DesktopPlatforms))]
    public void ValueIndicatorRemainsWhenSliderIsInFocusOnDesktop(TargetPlatform targetPlatform)
    {
        using PlatformOverride platform = new(targetPlatform);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        double value = 0.5;
        using var focusNode = new FocusNode();

#pragma warning disable CS0618 // The Dart test uses the deprecated ShowValueIndicator.always.
        Widget BuildApp(bool enabled = true) => new MaterialApp(
            theme: new ThemeData(sliderTheme: new SliderThemeData(ShowValueIndicator: ShowValueIndicator.Always)),
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder(
                        (context, setState) => new Slider(
                            value: value,
                            focusNode: focusNode,
                            divisions: 5,
                            label: value.ToString("F1", CultureInfo.InvariantCulture),
                            onChanged: enabled ? newValue => setState(() => value = newValue) : null)))));
#pragma warning restore CS0618

        tester.PumpWidget(BuildApp());

        // Slider does not show value indicator without focus.
        tester.PumpAndSettle();
        Assert.False(focusNode.HasFocus);
        RenderObject valueIndicatorBox = OverlayRenderObject(tester);
        PaintAssert.DoesNotPaint(
            valueIndicatorBox,
            PaintPattern.Paints.Path(color: new Color(0xff000000)).Paragraph());

        Point sliderCenter = tester.GetCenter(tester.ElementOfType<Slider>());
        var tapLocation = new Point(sliderCenter.X + 50, sliderCenter.Y);

        // Tap somewhere to bring value indicator.
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.Down(tapLocation);
        gesture.Up();
        focusNode.RequestFocus();
        tester.PumpAndSettle();
        Assert.True(focusNode.HasFocus);
        valueIndicatorBox = OverlayRenderObject(tester);
        PaintAssert.Paints(valueIndicatorBox, PaintPattern.Paints.Path(color: new Color(0xff000000)).Paragraph());

        focusNode.Unfocus();
        tester.PumpAndSettle();
        Assert.False(focusNode.HasFocus);
        PaintAssert.DoesNotPaint(
            valueIndicatorBox,
            PaintPattern.Paints.Path(color: new Color(0xff000000)).Paragraph());
    }

    // Flutter: "showValueIndicator takes priority over theme"
    [Fact]
    public void ShowValueIndicatorTakesPriorityOverTheme()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);

        Widget BuildApp(ShowValueIndicator? themeShowValueIndicator, ShowValueIndicator? sliderShowValueIndicator) =>
            new MaterialApp(
                home: new MaterialWidget(
                    child: new Center(
                        child: new SliderTheme(
                            data: new SliderThemeData(
                                ValueIndicatorColor: Colors.Red,
                                ShowValueIndicator: themeShowValueIndicator),
                            child: new Slider(
                                value: 0.5,
                                label: "0.5",
                                onChanged: _ => { },
                                showValueIndicator: sliderShowValueIndicator)))));

        void CheckValueIndicator(bool isVisible)
        {
            // _RenderValueIndicator is the last render object in the tree.
            RenderObject valueIndicatorBox = AllRenderObjects(tester).Last();
            PaintPattern matcher = PaintPattern.Paints.Path(color: Colors.Red).Paragraph();
            if (isVisible)
            {
                PaintAssert.Paints(valueIndicatorBox, matcher);
            }
            else
            {
                PaintAssert.DoesNotPaint(valueIndicatorBox, matcher);
            }
        }

        tester.PumpWidget(BuildApp(ShowValueIndicator.Never, null));
        CheckValueIndicator(isVisible: false);

        tester.PumpWidget(BuildApp(ShowValueIndicator.Never, ShowValueIndicator.AlwaysVisible));
        CheckValueIndicator(isVisible: true);

        tester.PumpWidget(BuildApp(ShowValueIndicator.AlwaysVisible, ShowValueIndicator.Never));
        CheckValueIndicator(isVisible: false);
    }

    // Flutter: "Event on Slider should perform no-op if already unmounted"
    [Fact]
    public void EventOnSliderShouldPerformNoOpIfAlreadyUnmounted()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        // Test covering crashing found in Google internal issue b/192329942.
        double value = 0.0;
        using var shouldShowSliderListenable = new ValueNotifier<bool>(true);

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new StatefulBuilder(
                    (context, setState) => new MaterialWidget(
                        child: new Center(
                            child: new ValueListenableBuilder<bool>(
                                valueListenable: shouldShowSliderListenable,
                                builder: (context, shouldShowSlider, _) => new GestureDetector(
                                    behavior: HitTestBehavior.Translucent,
                                    // Note: it is important that `onTap` is non-null so
                                    // [GestureDetector] will register tap events.
                                    onTap: () => { },
                                    child: shouldShowSlider
                                        ? new Slider(
                                            value: value,
                                            onChanged: newValue => setState(() => value = newValue))
                                        : SizedBox.Expand()))))))));

        // Move Slider.
        TestGesture gesture = tester.StartGesture(
            RectCenter(tester.GetRect(tester.ElementOfType<Slider>())),
            PointerDeviceKind.Touch);
        gesture.MoveBy(new Vector(1.0, 0.0));
        tester.PumpAndSettle();

        // Hide Slider. Slider will dispose and unmount.
        shouldShowSliderListenable.Value = false;
        tester.PumpAndSettle();

        // Move Slider after unmounted.
        gesture.MoveBy(new Vector(1.0, 0.0));
        tester.PumpAndSettle();

        Assert.Null(tester.TakeException());
    }

    // Group: "Material 2"
    // These tests are only relevant for Material 2. Once Material 2
    // support is deprecated and the APIs are removed, these tests
    // can be deleted.

    // Flutter: "Slider can be hovered and has correct hover color" (group "Material 2")
    [Fact]
    public void Material2SliderCanBeHoveredAndHasCorrectHoverColor()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var theme = new ThemeData(useMaterial3: false);
        double value = 0.5;

        Widget BuildApp(bool enabled = true) => new MaterialApp(
            theme: theme,
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder(
                        (context, setState) => new Slider(
                            value: value,
                            onChanged: enabled ? newValue => setState(() => value = newValue) : null)))));

        Color hoverColor = theme.ColorScheme.Primary.WithOpacity(0.12);
        tester.PumpWidget(BuildApp());

        // Slider does not have overlay when enabled and not hovered.
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: hoverColor));

        // Start hovering.
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(tester.ElementOfType<Slider>()));

        // Slider has overlay when enabled and hovered.
        tester.PumpWidget(BuildApp());
        tester.PumpAndSettle();
        PaintAssert.Paints(MaterialOf(tester), PaintPattern.Paints.Circle(color: hoverColor));

        // Slider does not have an overlay when disabled and hovered.
        tester.PumpWidget(BuildApp(enabled: false));
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: hoverColor));
    }

    // Flutter: "Material2 - Slider is focusable and has correct focus color" (group "Material 2")
    [Fact]
    public void Material2SliderIsFocusableAndHasCorrectFocusColor()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        using var focusNode = new FocusNode(debugLabel: "Slider");
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var theme = new ThemeData(useMaterial3: false);
        double value = 0.5;

        Widget BuildApp(bool enabled = true) => new MaterialApp(
            theme: theme,
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder(
                        (context, setState) => new Slider(
                            value: value,
                            onChanged: enabled ? newValue => setState(() => value = newValue) : null,
                            autofocus: true,
                            focusNode: focusNode)))));

        Color focusColor = theme.ColorScheme.Primary.WithOpacity(0.12);
        tester.PumpWidget(BuildApp());

        // Check that the overlay shows when focused.
        tester.PumpAndSettle();
        Assert.True(focusNode.HasPrimaryFocus);
        PaintAssert.Paints(MaterialOf(tester), PaintPattern.Paints.Circle(color: focusColor));

        // Check that the overlay does not show when unfocused and disabled.
        tester.PumpWidget(BuildApp(enabled: false));
        tester.PumpAndSettle();
        Assert.False(focusNode.HasPrimaryFocus);
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: focusColor));
    }

    // Flutter: "Material2 - Slider is draggable and has correct dragged color" (group "Material 2")
    [Fact]
    public void Material2SliderIsDraggableAndHasCorrectDraggedColor()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        double value = 0.5;
        var theme = new ThemeData(useMaterial3: false);
        Key sliderKey = new UniqueKey();
        using var focusNode = new FocusNode();

        Widget BuildApp(bool enabled = true) => new MaterialApp(
            theme: theme,
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder(
                        (context, setState) => new Slider(
                            key: sliderKey,
                            value: value,
                            focusNode: focusNode,
                            onChanged: enabled ? newValue => setState(() => value = newValue) : null)))));

        Color dragColor = theme.ColorScheme.Primary.WithOpacity(0.12);
        tester.PumpWidget(BuildApp());

        // Slider does not have overlay when enabled and not dragged.
        tester.PumpAndSettle();
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: dragColor));

        // Start dragging.
        TestGesture drag = tester.StartGesture(
            tester.GetCenter(tester.ElementsWithKey(sliderKey).Single()),
            PointerDeviceKind.Touch);
        tester.Pump(GestureConstants.PressTimeout);

        // Less than configured touch slop, more than default touch slop
        drag.MoveBy(new Vector(19.0, 0));
        tester.Pump();

        // Slider has overlay when enabled and dragged.
        PaintAssert.Paints(MaterialOf(tester), PaintPattern.Paints.Circle(color: dragColor));

        drag.Up();
        tester.PumpAndSettle();

        // Slider without focus doesn't have overlay when enabled and dragged.
        Assert.False(focusNode.HasFocus);
        PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: dragColor));
    }

    // Group: "Slider.allowedInteraction"

    // Flutter: "SliderInteraction.tapOnly" (group "Slider.allowedInteraction")
    [Fact]
    public void SliderInteractionTapOnly()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        double value = 1.0;
        Key sliderKey = new UniqueKey();
        // (slider's left padding (overlayRadius), windowHeight / 2)
        var startOfTheSliderTrack = new Point(24, 300);
        var centerOfTheSlideTrack = new Point(400, 300);
        var logs = new List<string>();

        Widget BuildWidget() => new MaterialApp(
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder(
                        (_, setState) => new Slider(
                            value: value,
                            key: sliderKey,
                            allowedInteraction: SliderInteraction.TapOnly,
                            onChangeStart: _ => logs.Add("onChangeStart"),
                            onChanged: newValue =>
                            {
                                logs.Add("onChanged");
                                setState(() => value = newValue);
                            },
                            onChangeEnd: _ => logs.Add("onChangeEnd"))))));

        // allow tap only
        tester.PumpWidget(BuildWidget());

        Assert.Empty(logs);

        // test tap
        TestGesture gesture = tester.StartGesture(centerOfTheSlideTrack, PointerDeviceKind.Touch);
        tester.Pump();
        // changes from 1.0 -> 0.5
        Assert.Equal(0.5, value);
        Assert.Equal(["onChangeStart", "onChanged"], logs);

        // test slide
        gesture.MoveTo(startOfTheSliderTrack);
        tester.Pump();
        // has no effect, remains 0.5
        Assert.Equal(0.5, value);
        Assert.Equal(["onChangeStart", "onChanged"], logs);

        gesture.Up();
        tester.Pump();
        Assert.Equal(["onChangeStart", "onChanged", "onChangeEnd"], logs);
    }

    // Flutter: "SliderInteraction.tapAndSlide (default)" (group "Slider.allowedInteraction")
    [Fact]
    public void SliderInteractionTapAndSlideDefault()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        double value = 1.0;
        Key sliderKey = new UniqueKey();
        // (slider's left padding (overlayRadius), windowHeight / 2)
        var startOfTheSliderTrack = new Point(24, 300);
        var centerOfTheSlideTrack = new Point(400, 300);
        var endOfTheSliderTrack = new Point(800 - 24, 300);
        var logs = new List<string>();

        Widget BuildWidget() => new MaterialApp(
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder(
                        (_, setState) => new Slider(
                            value: value,
                            key: sliderKey,
                            onChangeStart: _ => logs.Add("onChangeStart"),
                            onChanged: newValue =>
                            {
                                logs.Add("onChanged");
                                setState(() => value = newValue);
                            },
                            onChangeEnd: _ => logs.Add("onChangeEnd"))))));

        tester.PumpWidget(BuildWidget());

        Assert.Empty(logs);

        // Test tap.
        TestGesture gesture = tester.StartGesture(centerOfTheSlideTrack, PointerDeviceKind.Touch);
        tester.Pump();
        // changes from 1.0 -> 0.5
        Assert.Equal(0.5, value);
        Assert.Equal(["onChangeStart", "onChanged"], logs);

        // test slide
        gesture.MoveTo(startOfTheSliderTrack);
        tester.Pump();
        // changes from 0.5 -> 0.0
        Assert.Equal(0.0, value);
        gesture.MoveTo(endOfTheSliderTrack);
        tester.Pump();
        // changes from 0.0 -> 1.0
        Assert.Equal(1.0, value);
        Assert.Equal(["onChangeStart", "onChanged", "onChanged", "onChanged"], logs);

        gesture.Up();
        tester.Pump();

        Assert.Equal(["onChangeStart", "onChanged", "onChanged", "onChanged", "onChangeEnd"], logs);
    }

    // Flutter: "SliderInteraction.slideOnly" (group "Slider.allowedInteraction")
    [Fact]
    public void SliderInteractionSlideOnly()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        const double overlayRadius = 23;
        Color overlayColor = Colors.Red;
        double value = 1.0;
        Key sliderKey = new UniqueKey();
        // (slider's left padding (overlayRadius), windowHeight / 2)
        var startOfTheSliderTrack = new Point(overlayRadius, 300);
        var centerOfTheSliderTrack = new Point(400, 300);
        var endOfTheSliderTrack = new Point(800 - overlayRadius, 300);
        // Dart's `Tween<double>(begin: start.dx, end: end.dx).transform`.
        double XPosThumb(double t) =>
            startOfTheSliderTrack.X + (endOfTheSliderTrack.X - startOfTheSliderTrack.X) * t;
        var logs = new List<string>();

        Widget BuildApp() => new MaterialApp(
            theme: new ThemeData(
                sliderTheme: new SliderThemeData(
                    OverlayColor: overlayColor,
                    OverlayShape: new RoundSliderOverlayShape(overlayRadius: overlayRadius))),
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder(
                        (_, setState) => new Slider(
                            value: value,
                            key: sliderKey,
                            allowedInteraction: SliderInteraction.SlideOnly,
                            onChangeStart: _ => logs.Add("onChangeStart"),
                            onChanged: newValue =>
                            {
                                logs.Add("onChanged");
                                setState(() => value = newValue);
                            },
                            onChangeEnd: _ => logs.Add("onChangeEnd"))))));

        tester.PumpWidget(BuildApp());

        RenderObject material = MaterialOf(tester);
        TimeSpan halfRadialReaction = RadialReactionDuration / 2;

        Assert.Empty(logs);

        // Test tap.
        TestGesture gesture = tester.StartGesture(centerOfTheSliderTrack, PointerDeviceKind.Touch);
        // Start animation.
        tester.Pump();
        // Go to mid-animation frame.
        tester.Pump(halfRadialReaction);
        PaintAssert.Paints(material, PaintPattern.Paints.Circle(color: overlayColor, x: XPosThumb(value)));
        // We have a non-linear asymmetric curve, so just verify the radius is not full.
        PaintAssert.DoesNotPaint(
            material,
            PaintPattern.Paints.Circle(color: overlayColor, radius: overlayRadius, x: XPosThumb(value)));

        // Finish animation.
        tester.PumpAndSettle();
        // Overlay drawn.
        PaintAssert.Paints(
            material,
            PaintPattern.Paints.Circle(color: overlayColor, radius: overlayRadius, x: XPosThumb(value)));
        // Has no other effect as tap is disabled, remains 1.0.
        Assert.Equal(1.0, value);
        Assert.Equal(["onChangeStart"], logs);

        // Test slide.
        gesture.MoveTo(startOfTheSliderTrack);
        tester.Pump();
        // Changes from 1.0 -> 0.5.
        Assert.Equal(0.5, value);
        // Overlay still there.
        PaintAssert.Paints(
            material,
            PaintPattern.Paints.Circle(color: overlayColor, radius: overlayRadius, x: XPosThumb(value)));
        gesture.MoveTo(endOfTheSliderTrack);
        tester.Pump();
        // Changes from 0.0 -> 1.0.
        Assert.Equal(1.0, value);
        Assert.Equal(["onChangeStart", "onChanged", "onChanged"], logs);
        // Overlay still there.
        PaintAssert.Paints(
            material,
            PaintPattern.Paints.Circle(color: overlayColor, radius: overlayRadius, x: XPosThumb(value)));

        gesture.Up();

        // Start release animation.
        tester.Pump();
        // Go to mid-animation frame.
        tester.Pump(halfRadialReaction);
        PaintAssert.Paints(material, PaintPattern.Paints.Circle(color: overlayColor, x: XPosThumb(value)));
        // Verify the radius is not full.
        PaintAssert.DoesNotPaint(
            material,
            PaintPattern.Paints.Circle(color: overlayColor, radius: overlayRadius, x: XPosThumb(value)));

        tester.PumpAndSettle();
        // No overlay drawn.
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: overlayColor, radius: overlayRadius));

        Assert.Equal(["onChangeStart", "onChanged", "onChanged", "onChangeEnd"], logs);
    }

    // Flutter: "SliderInteraction.slideThumb" (group "Slider.allowedInteraction")
    [Fact]
    public void SliderInteractionSlideThumb()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        double value = 1.0;
        Key sliderKey = new UniqueKey();
        // (slider's left padding (overlayRadius), windowHeight / 2)
        var startOfTheSliderTrack = new Point(24, 300);
        var centerOfTheSliderTrack = new Point(400, 300);
        var endOfTheSliderTrack = new Point(800 - 24, 300);
        var logs = new List<string>();

        Widget BuildApp() => new MaterialApp(
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder(
                        (_, setState) => new Slider(
                            value: value,
                            key: sliderKey,
                            allowedInteraction: SliderInteraction.SlideThumb,
                            onChangeStart: _ => logs.Add("onChangeStart"),
                            onChanged: newValue =>
                            {
                                logs.Add("onChanged");
                                setState(() => value = newValue);
                            },
                            onChangeEnd: _ => logs.Add("onChangeEnd"))))));

        tester.PumpWidget(BuildApp());

        Assert.Empty(logs);

        // test tap
        TestGesture gesture = tester.StartGesture(centerOfTheSliderTrack, PointerDeviceKind.Touch);
        tester.Pump();
        // has no effect, remains 1.0
        Assert.Equal(1.0, value);
        Assert.Empty(logs);

        // test slide
        gesture.MoveTo(startOfTheSliderTrack);
        tester.Pump();
        // has no effect, remains 1.0
        Assert.Equal(1.0, value);
        Assert.Empty(logs);

        // test slide thumb
        gesture.Up();
        gesture.Down(endOfTheSliderTrack); // where the thumb is
        tester.Pump();
        // has no effect, remains 1.0
        Assert.Equal(1.0, value);
        Assert.Equal(["onChangeStart"], logs);

        gesture.MoveTo(centerOfTheSliderTrack);
        tester.Pump();
        // changes from 1.0 -> 0.5
        Assert.Equal(0.5, value);
        Assert.Equal(["onChangeStart", "onChanged"], logs);

        // test tap inside overlay but not on thumb, then slide
        gesture.Up();
        // default overlay radius is 12, so 10 is inside the overlay
        gesture.Down(centerOfTheSliderTrack + new Vector(-10, 0));
        tester.Pump();
        // changes from 1.0 -> 0.5
        Assert.Equal(0.5, value);
        Assert.Equal(["onChangeStart", "onChanged", "onChangeEnd", "onChangeStart"], logs);

        gesture.MoveTo(endOfTheSliderTrack + new Vector(-10, 0));
        tester.Pump();
        // changes from 0.5 -> 1.0
        Assert.Equal(1.0, value);
        Assert.Equal(["onChangeStart", "onChanged", "onChangeEnd", "onChangeStart", "onChanged"], logs);

        gesture.Up();
        tester.Pump();

        Assert.Equal(
            ["onChangeStart", "onChanged", "onChangeEnd", "onChangeStart", "onChanged", "onChangeEnd"],
            logs);
    }

    // This is a regression test for https://github.com/flutter/flutter/issues/143524.
    // Flutter: "Discrete Slider.onChanged is called only once"
    [Fact]
    public void DiscreteSliderOnChangedIsCalledOnlyOnce()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        int onChangeCallbackCount = 0;
        tester.PumpWidget(new MaterialApp(
            home: new Scaffold(
                body: new Center(
                    child: new Slider(
                        max: 5,
                        divisions: 5,
                        value: 0,
                        onChanged: _ => onChangeCallbackCount++)))));

        TestGesture gesture = tester.StartGesture(
            tester.GetTopLeft(tester.ElementOfType<Slider>()),
            PointerDeviceKind.Touch);
        tester.Pump(GestureConstants.LongPressTimeout);
        gesture.MoveBy(new Vector(160.0, 0.0));
        gesture.MoveBy(new Vector(1.0, 0.0));
        gesture.MoveBy(new Vector(1.0, 0.0));
        Assert.Equal(1, onChangeCallbackCount);
    }

    // Flutter: "Skip drawing ValueIndicator shape when label painter text is null"
    [Fact]
    public void SkipDrawingValueIndicatorShapeWhenLabelPainterTextIsNull()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        double sliderValue = 10;

        tester.PumpWidget(new MaterialApp(
            home: new StatefulBuilder(
                (context, setState) => new MaterialWidget(
                    child: new Slider(
                        value: sliderValue,
                        max: 100,
                        label: sliderValue > 50 ? null : BindingBase.DartDoubleToString(sliderValue),
                        divisions: 10,
                        onChanged: newValue => setState(() => sliderValue = newValue))))));

        RenderObject valueIndicatorBox = OverlayRenderObject(tester);

        // Calculate a specific position on the Slider.
        Rect sliderRect = tester.GetRect(tester.ElementOfType<Slider>());
        var tapPositionLeft = new Point(sliderRect.Left + sliderRect.Width * 0.25, RectCenter(sliderRect).Y);
        var tapPositionRight = new Point(sliderRect.Left + sliderRect.Width * 0.75, RectCenter(sliderRect).Y);

        // Tap on the 25% position of the Slider.
        TapAt(tester, tapPositionLeft, PointerDeviceKind.Touch);
        tester.PumpAndSettle();
        Assert.Equal(2, PaintRecording.Record(valueIndicatorBox).CountCalls("drawPath"));

        // Tap on the 75% position of the Slider.
        TapAt(tester, tapPositionRight, PointerDeviceKind.Touch);
        tester.PumpAndSettle();
        Assert.Equal(1, PaintRecording.Record(valueIndicatorBox).CountCalls("drawPath"));
    }

    // Flutter: "Slider value indicator is shown when using arrow keys"
    [Theory]
    [MemberData(nameof(DesktopPlatforms))]
    public void SliderValueIndicatorIsShownWhenUsingArrowKeys(TargetPlatform targetPlatform)
    {
        using PlatformOverride platform = new(targetPlatform);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var theme = new ThemeData();
        double startValue = 0.0;
        double currentValue = 0.5;
        double endValue = 0.0;

        tester.PumpWidget(new MaterialApp(
            theme: theme,
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder(
                        (context, setState) => new Slider(
                            value: currentValue,
                            divisions: 5,
                            label: currentValue.ToString("F1", CultureInfo.InvariantCulture),
                            onChangeStart: newValue => setState(() => startValue = newValue),
                            onChanged: newValue => setState(() => currentValue = newValue),
                            onChangeEnd: newValue => setState(() => endValue = newValue),
                            autofocus: true))))));

        PaintPattern Indicator() => PaintPattern.Paints.Scale().Path(color: theme.ColorScheme.Primary);

        // Slider shows value indicator initially on focus.
        tester.PumpAndSettle();
        RenderObject valueIndicatorBox = OverlayRenderObject(tester);
        PaintAssert.Paints(valueIndicatorBox, Indicator());

        // Right arrow (increase)
        SendKeyEvent(LogicalKeyboardKey.ArrowRight);
        tester.PumpAndSettle();
        Assert.Equal(0.6, startValue);
        Assert.Equal("0.8", currentValue.ToString("F1", CultureInfo.InvariantCulture));
        Assert.Equal("0.8", endValue.ToString("F1", CultureInfo.InvariantCulture));

        // Value indicator is visible.
        valueIndicatorBox = OverlayRenderObject(tester);
        PaintAssert.Paints(valueIndicatorBox, Indicator());

        // Left arrow (decrease)
        SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
        tester.PumpAndSettle();
        Assert.Equal(0.8, startValue);
        Assert.Equal("0.6", currentValue.ToString("F1", CultureInfo.InvariantCulture));
        Assert.Equal("0.6", endValue.ToString("F1", CultureInfo.InvariantCulture));

        // Value indicator is still visible.
        valueIndicatorBox = OverlayRenderObject(tester);
        PaintAssert.Paints(valueIndicatorBox, Indicator());

        // Up arrow (increase)
        SendKeyEvent(LogicalKeyboardKey.ArrowUp);
        tester.PumpAndSettle();
        Assert.Equal(0.6, startValue);
        Assert.Equal("0.8", currentValue.ToString("F1", CultureInfo.InvariantCulture));
        Assert.Equal("0.8", endValue.ToString("F1", CultureInfo.InvariantCulture));

        // Value indicator is still visible.
        valueIndicatorBox = OverlayRenderObject(tester);
        PaintAssert.Paints(valueIndicatorBox, Indicator());

        // Down arrow (decrease)
        SendKeyEvent(LogicalKeyboardKey.ArrowDown);
        tester.PumpAndSettle();
        Assert.Equal(0.8, startValue);
        Assert.Equal("0.6", currentValue.ToString("F1", CultureInfo.InvariantCulture));
        Assert.Equal("0.6", endValue.ToString("F1", CultureInfo.InvariantCulture));

        // Value indicator is still visible.
        valueIndicatorBox = OverlayRenderObject(tester);
        PaintAssert.Paints(valueIndicatorBox, Indicator());
    }

    // Flutter: "Value indicator label is shown when focused"
    [Theory]
    [MemberData(nameof(DesktopPlatforms))]
    public void ValueIndicatorLabelIsShownWhenFocused(TargetPlatform targetPlatform)
    {
        using PlatformOverride platform = new(targetPlatform);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        double value = 0.5;
        using var focusNode = new FocusNode();

        Widget BuildApp() => new MaterialApp(
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder(
                        (context, setState) => new Slider(
                            value: value,
                            focusNode: focusNode,
                            divisions: 5,
                            label: value.ToString("F1", CultureInfo.InvariantCulture),
                            onChanged: newValue => setState(() => value = newValue))))));

        tester.PumpWidget(BuildApp());

        // Slider does not show value indicator without focus.
        tester.PumpAndSettle();
        Assert.False(focusNode.HasFocus);
        RenderObject valueIndicatorBox = OverlayRenderObject(tester);
        PaintAssert.DoesNotPaint(
            valueIndicatorBox,
            PaintPattern.Paints.Path(color: new Color(0xff000000)).Paragraph());

        focusNode.RequestFocus();
        tester.PumpAndSettle();
        Assert.True(focusNode.HasFocus);

        // Slider shows value indicator when focused.
        valueIndicatorBox = OverlayRenderObject(tester);
        PaintAssert.Paints(valueIndicatorBox, PaintPattern.Paints.Path(color: new Color(0xff000000)).Paragraph());
    }

    // Flutter: "Slider.padding can override the default Slider padding"
    [Fact]
    public void SliderPaddingCanOverrideTheDefaultSliderPadding()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);

        Widget BuildSlider(EdgeInsetsGeometry? padding = null) => new MaterialApp(
            home: new MaterialWidget(
                child: new Center(
                    child: new IntrinsicHeight(
                        child: new Slider(padding: padding, value: 0.5, onChanged: _ => { })))));

        RenderBox SliderRenderBox() => (RenderBox)AllRenderObjects(tester).First(o => o is RenderSlider);

        RenderObject SliderRender() => tester.ElementOfType<Slider>().FindRenderObject()!;

        // Test Slider height and tracks spacing with zero padding.
        tester.PumpWidget(BuildSlider(padding: EdgeInsets.Zero));
        tester.PumpAndSettle();

        // The height equals to the default thumb height.
        Assert.Equal(new Size(800, 20), SliderRenderBox().Size);
        PaintAssert.Paints(
            SliderRender(),
            PaintPattern.Paints
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBR(398.0, 8.0, 800.0, 12.0, Radius.Circular(2.0)))
                // Active track.
                .RRect(rrect: RRect.FromLTRBR(0.0, 7.0, 402.0, 13.0, Radius.Circular(3.0))));

        // Test Slider height and tracks spacing with directional padding.
        const double startPadding = 100;
        const double endPadding = 20;
        tester.PumpWidget(BuildSlider(padding: EdgeInsetsDirectional.Only(start: startPadding, end: endPadding)));
        tester.PumpAndSettle();

        Assert.Equal(new Size(800 - startPadding - endPadding, 20), SliderRenderBox().Size);
        PaintAssert.Paints(
            SliderRender(),
            PaintPattern.Paints
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBR(338.0, 8.0, 680.0, 12.0, Radius.Circular(2.0)))
                // Active track.
                .RRect(rrect: RRect.FromLTRBR(0.0, 7.0, 342.0, 13.0, Radius.Circular(3.0))));

        // Test Slider height and tracks spacing with top and bottom padding.
        const double topPadding = 100;
        const double bottomPadding = 20;
        const double trackHeight = 20;
        tester.PumpWidget(BuildSlider(padding: EdgeInsetsDirectional.Only(top: topPadding, bottom: bottomPadding)));
        tester.PumpAndSettle();

        Assert.Equal(
            new Size(800, topPadding + trackHeight + bottomPadding),
            tester.GetSize(tester.ElementOfType<Slider>()));
        Assert.Equal(new Size(800, 20), SliderRenderBox().Size);
        PaintAssert.Paints(
            SliderRender(),
            PaintPattern.Paints
                // Inactive track.
                .RRect(rrect: RRect.FromLTRBR(398.0, 8.0, 800.0, 12.0, Radius.Circular(2.0)))
                // Active track.
                .RRect(rrect: RRect.FromLTRBR(0.0, 7.0, 402.0, 13.0, Radius.Circular(3.0))));
    }

    // Flutter: "Default Slider when year2023 is false"
    [Fact]
    public void DefaultSliderWhenYear2023IsFalse()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        RenderingDebug.DisableShadows = false;
        try
        {
            var theme = new ThemeData();
            ColorScheme colorScheme = theme.ColorScheme;
            Color activeTrackColor = colorScheme.Primary;
            Color inactiveTrackColor = colorScheme.SecondaryContainer;
            Color secondaryActiveTrackColor = colorScheme.Primary.WithOpacity(0.54);
            Color disabledActiveTrackColor = colorScheme.OnSurface.WithOpacity(0.38);
            Color disabledInactiveTrackColor = colorScheme.OnSurface.WithOpacity(0.12);
            Color disabledSecondaryActiveTrackColor = colorScheme.OnSurface.WithOpacity(0.38);
            Color activeTickMarkColor = colorScheme.OnPrimary;
            Color inactiveTickMarkColor = colorScheme.OnSecondaryContainer;
            Color disabledActiveTickMarkColor = colorScheme.OnInverseSurface;
            Color disabledInactiveTickMarkColor = colorScheme.OnSurface;
            Color thumbColor = colorScheme.Primary;
            Color disabledThumbColor = colorScheme.OnSurface.WithOpacity(0.38);
            Color valueIndicatorColor = colorScheme.InverseSurface;
            double value = 0.45;

            Widget BuildApp(int? divisions = null, bool enabled = true)
            {
                Action<double>? onChanged = !enabled ? null : d => value = d;
                return new MaterialApp(
                    home: new Directionality(
                        TextDirection.Ltr,
                        new MaterialWidget(
                            child: new Center(
                                child: new Theme(
                                    data: theme,
                                    child: new Slider(
                                        year2023: false,
                                        value: value,
                                        secondaryTrackValue: 0.75,
                                        label: BindingBase.DartDoubleToString(value),
                                        divisions: divisions,
                                        onChanged: onChanged))))));
            }

            tester.PumpWidget(BuildApp());

            RenderObject material = MaterialOf(tester);

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
                    // Inactive track.
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

            // Test default colors for enabled slider.
            PaintAssert.Paints(material, PaintPattern.Paints.Circle().RRect(color: thumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle().Circle(color: disabledThumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledActiveTrackColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledInactiveTrackColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledSecondaryActiveTrackColor));

            // Test defaults colors for discrete slider.
            tester.PumpWidget(BuildApp(divisions: 3));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .RRect(color: activeTrackColor)
                    .RRect(color: inactiveTrackColor)
                    .RRect(color: secondaryActiveTrackColor)
                    .Circle(color: activeTickMarkColor)
                    .Circle(color: activeTickMarkColor)
                    .Circle(color: inactiveTickMarkColor)
                    .Circle(color: inactiveTickMarkColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: disabledThumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledActiveTrackColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledInactiveTrackColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledSecondaryActiveTrackColor));

            // Test defaults colors for disabled slider.
            tester.PumpWidget(BuildApp(enabled: false));
            tester.PumpAndSettle();
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .RRect(color: disabledActiveTrackColor)
                    .RRect(color: disabledInactiveTrackColor)
                    .RRect(color: disabledSecondaryActiveTrackColor));
            PaintAssert.Paints(material, PaintPattern.Paints.Circle().RRect(color: disabledThumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle().RRect(color: thumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: activeTrackColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: inactiveTrackColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: secondaryActiveTrackColor));

            // Test defaults colors for disabled discrete slider.
            tester.PumpWidget(BuildApp(divisions: 3, enabled: false));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .RRect(color: disabledActiveTrackColor)
                    .RRect(color: disabledInactiveTrackColor)
                    .RRect(color: disabledSecondaryActiveTrackColor)
                    .Circle(color: disabledActiveTickMarkColor)
                    .Circle(color: disabledActiveTickMarkColor)
                    .Circle(color: disabledInactiveTickMarkColor)
                    .Circle(color: disabledInactiveTickMarkColor)
                    .RRect(color: disabledThumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle().RRect(color: thumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: activeTrackColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: inactiveTrackColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: secondaryActiveTrackColor));

            tester.PumpWidget(BuildApp(divisions: 3));
            tester.PumpAndSettle();

            Point center = tester.GetCenter(tester.ElementOfType<Slider>());
            TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
            // Wait for value indicator animation to finish.
            tester.PumpAndSettle();

            RenderObject valueIndicatorBox = OverlayRenderObject(tester);
            PaintAssert.Paints(valueIndicatorBox, PaintPattern.Paints.Scale().RRect(color: valueIndicatorColor));
            gesture.Up();
        }
        finally
        {
            RenderingDebug.DisableShadows = true;
        }
    }

    // Flutter: "Slider value indicator text when year2023 is false"
    [Fact]
    public void SliderValueIndicatorTextWhenYear2023IsFalse()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        const double value = 50;
        var log = new List<InlineSpan>();
        var loggingValueIndicatorShape = new LoggingValueIndicatorShape(log);
        var theme = new ThemeData(sliderTheme: new SliderThemeData(ValueIndicatorShape: loggingValueIndicatorShape));

        Widget BuildSlider() => new MaterialApp(
            theme: theme,
            home: new MaterialWidget(
                child: new Center(
                    child: new Slider(
                        year2023: false,
                        max: 100.0,
                        divisions: 4,
                        label: $"{(int)Math.Round(value, MidpointRounding.AwayFromZero)}",
                        value: value,
                        onChanged: _ => { }))));

        // Normal text
        tester.PumpWidget(BuildSlider());
        Point center = tester.GetCenter(tester.ElementOfType<Slider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
        tester.PumpAndSettle();

        Assert.Equal("50", log[^1].ToPlainText());
        Assert.Equal(14.0, log[^1].Style!.FontSize);
        Assert.Equal(theme.ColorScheme.OnInverseSurface, log[^1].Style!.Color);

        gesture.Up();
        tester.PumpAndSettle();
    }

    // Flutter: "Can update renderObject when secondaryTrackValue is updated"
    [Fact]
    public void CanUpdateRenderObjectWhenSecondaryTrackValueIsUpdated()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var log = new List<Point?>();
        var loggingTrackShape = new LoggingRoundedRectSliderTrackShape(secondaryOffsetLog: log);
        var theme = new ThemeData(sliderTheme: new SliderThemeData(TrackShape: loggingTrackShape));

        Widget BuildSlider(double? secondaryTrackValue) => new MaterialApp(
            theme: theme,
            home: new MaterialWidget(
                child: new Center(
                    child: new Slider(
                        value: 0,
                        secondaryTrackValue: secondaryTrackValue,
                        onChanged: _ => { }))));

        tester.PumpWidget(BuildSlider(null));
        tester.PumpAndSettle();
        Assert.Null(log[^1]);

        tester.PumpWidget(BuildSlider(0.2));
        tester.PumpAndSettle();
        Assert.Equal(new Point(174.4, 300.0), log[^1]);

        tester.PumpWidget(BuildSlider(0.5));
        tester.PumpAndSettle();
        Assert.Equal(new Point(400.0, 300.0), log[^1]);
    }

    // Regression test for hhttps://github.com/flutter/flutter/issues/161805
    // Flutter: "Discrete Slider does not apply thumb padding in a non-rounded track shape"
    [Fact]
    public void DiscreteSliderDoesNotApplyThumbPaddingInANonRoundedTrackShape()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        // The default track left and right padding.
        const double sliderPadding = 24.0;
        var theme = new ThemeData(
            sliderTheme: new SliderThemeData(
                // Thumb padding is applied based on the track height.
                TrackHeight: 100,
                TrackShape: new RectangularSliderTrackShape()));

        Widget BuildSlider(double value) => new MaterialApp(
            theme: theme,
            home: new MaterialWidget(
                child: new SizedBox(
                    width: 300,
                    child: new Slider(value: value, max: 100, divisions: 100, onChanged: _ => { }))));

        tester.PumpWidget(BuildSlider(value: 0));

        RenderObject material = MaterialOf(tester);

        PaintAssert.Paints(
            material,
            PaintPattern.Paints.Circle(x: sliderPadding, y: 300.0, color: theme.ColorScheme.Primary));

        tester.PumpWidget(BuildSlider(value: 100));
        tester.PumpAndSettle();

        material = MaterialOf(tester);
        PaintAssert.Paints(
            material,
            PaintPattern.Paints.Circle(x: 800.0 - sliderPadding, y: 300.0, color: theme.ColorScheme.Primary));
    }

    // Flutter: "Slider does not crash at zero area"
    [Fact]
    public void SliderDoesNotCrashAtZeroArea()
    {
        using PlatformOverride platform = new(TargetPlatform.Android);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new MaterialApp(
            home: new Scaffold(
                body: new Center(
                    child: SizedBox.Shrink(child: new Slider(value: 1, onChanged: _ => { }))))));
        Assert.Equal(new Size(0, 0), tester.GetSize(tester.ElementOfType<Slider>()));
    }

    // ---- Harness helpers -------------------------------------------------------------------------

    // TargetPlatformVariant.desktop().
    public static TheoryData<TargetPlatform> DesktopPlatforms => new()
    {
        TargetPlatform.Linux,
        TargetPlatform.MacOS,
        TargetPlatform.Windows,
    };

    // Dart's `kRadialReactionDuration` (material_ui/lib/src/constants.dart).
    private static readonly TimeSpan RadialReactionDuration = TimeSpan.FromMilliseconds(100);

    // `Material.of(tester.element(find.byType(Slider)))`: the ink-feature render object that `paints`
    // is matched against.
    private static RenderObject MaterialOf(FrameworkDartTester tester) =>
        LookupBoundary.FindAncestorRenderObjectOfType<RenderInkFeatures>(
            tester.ElementOfType<Slider>())!;

    // `tester.renderObject(find.byType(Overlay))`.
    private static RenderObject OverlayRenderObject(FrameworkDartTester tester) =>
        tester.ElementOfType<Overlay>().FindRenderObject()!;

    // `tester.allRenderObjects`.
    private static IEnumerable<RenderObject> AllRenderObjects(FrameworkDartTester tester) =>
        tester.AllElements().OfType<RenderObjectElement>().Select(element => element.RenderObject);

    private static Point RectCenter(Rect rect) => new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

    // `tester.tapAt(location, kind: kind)`.
    private static void TapAt(FrameworkDartTester tester, Point location, PointerDeviceKind kind)
    {
        TestGesture gesture = tester.StartGesture(location, kind);
        gesture.Up();
    }

    // `tester.pumpAndSettle(duration)`: pumps `duration`-long frames until nothing is scheduled.
    private static void PumpAndSettle(FrameworkDartTester tester, TimeSpan step)
    {
        int count = 0;
        do
        {
            Assert.True(count++ < 1000, "pumpAndSettle timed out");
            tester.Pump(step);
        }
        while (Scheduler.HasScheduledFrame || Scheduler.TransientCallbackCount > 0);
    }

    // `tester.sendKeyEvent(key)`: a key down then a key up through the focus manager.
    private static void SendKeyEvent(LogicalKeyboardKey key)
    {
        FocusManager.Instance.HandleKeyEvent(KeySim.Down(key));
        Scheduler.FlushMicrotasks();
        FocusManager.Instance.HandleKeyEvent(KeySim.Up(key));
        Scheduler.FlushMicrotasks();
    }

    // flutter_test's `debugDefaultTargetPlatformOverride` for one test (android unless a variant says
    // otherwise).
    private sealed class PlatformOverride : IDisposable
    {
        private readonly TargetPlatform? _previous;

        public PlatformOverride(TargetPlatform platform)
        {
            _previous = PlatformDefaults.DebugTargetPlatformOverride;
            PlatformDefaults.DebugTargetPlatformOverride = platform;
        }

        public void Dispose() => PlatformDefaults.DebugTargetPlatformOverride = _previous;
    }

    /// <summary>A <see cref="RoundedRectSliderTrackShape"/> that logs its paint.</summary>
    private sealed class LoggingRoundedRectSliderTrackShape(List<Point?>? secondaryOffsetLog = null)
        : RoundedRectSliderTrackShape
    {
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
            secondaryOffsetLog?.Add(secondaryOffset);
            base.Paint(
                context,
                offset,
                parentBox,
                sliderTheme,
                enableAnimation,
                thumbCenter,
                textDirection,
                secondaryOffset,
                isEnabled,
                isDiscrete);
        }
    }

    // A value indicator shape to log labelPainter text.
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
}
