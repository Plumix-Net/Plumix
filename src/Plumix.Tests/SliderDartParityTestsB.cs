// Dart parity source: material_ui/lib/src/slider.dart
// Mirrors material-ui-src/test/slider_test.dart (lines 2374-4023)

using System.Reflection;
using System.Text;
using Avalonia;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using Xunit.Sdk;
using MaterialWidget = Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SliderDartParityTestsB : IDisposable
{
    private readonly FocusHighlightStrategy _previousHighlightStrategy;
    private readonly bool _previousDisableShadows;

    public SliderDartParityTestsB()
    {
        // flutter_test runs every test with `debugDefaultTargetPlatformOverride == android` and
        // `debugDisableShadows == true`.
        FocusManager.Instance.ResetForTests();
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        _previousHighlightStrategy = FocusManager.Instance.HighlightStrategy;
        _previousDisableShadows = RenderingDebug.DisableShadows;
        RenderingDebug.DisableShadows = true;
    }

    public void Dispose()
    {
        RenderingDebug.DisableShadows = _previousDisableShadows;
        FocusManager.Instance.HighlightStrategy = _previousHighlightStrategy;
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    public static TheoryData<TargetPlatform> NonApplePlatforms =>
        new(TargetPlatform.Android, TargetPlatform.Fuchsia, TargetPlatform.Linux, TargetPlatform.Windows);

    public static TheoryData<TargetPlatform> ApplePlatforms => new(TargetPlatform.IOS, TargetPlatform.MacOS);

    // Flutter: "OverlayColor property is correctly applied when activeColor is also provided"
    [Fact]
    public void OverlayColorPropertyIsCorrectlyAppliedWhenActiveColorIsAlsoProvided()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var focusNode = new FocusNode(debugLabel: "Slider");
        try
        {
            FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
            double value = 0.5;
            var activeColor = new Color(0xffff0000);
            var overlayColor = new Color(0xff0000ff);

            Widget BuildApp(bool enabled = true)
            {
                return new MaterialApp(
                    home: new MaterialWidget(
                        child: new Center(
                            child: new StatefulBuilder((context, setState) => new Slider(
                                value: value,
                                activeColor: activeColor,
                                overlayColor: new WidgetStatePropertyAll<Color?>(overlayColor),
                                onChanged: enabled
                                    ? newValue => setState(() => value = newValue)
                                    : null,
                                focusNode: focusNode)))));
            }

            tester.PumpWidget(BuildApp());
            tester.PumpAndSettle();

            // Check that thumb color is using active color.
            PaintAssert.Paints(MaterialOf(tester), PaintPattern.Paints.Circle(color: activeColor));

            focusNode.RequestFocus();
            tester.PumpAndSettle();

            // Check that the overlay shows when focused.
            Assert.True(focusNode.HasPrimaryFocus);
            PaintAssert.Paints(MaterialOf(tester), PaintPattern.Paints.Circle(color: overlayColor));

            // Check that the overlay does not show when focused and disabled.
            tester.PumpWidget(BuildApp(enabled: false));
            tester.PumpAndSettle();
            Assert.False(focusNode.HasPrimaryFocus);
            PaintAssert.DoesNotPaint(MaterialOf(tester), PaintPattern.Paints.Circle(color: overlayColor));
        }
        finally
        {
            tester.Dispose();
            focusNode.Dispose();
        }
    }

    // Flutter: "Slider can be incremented and decremented by keyboard shortcuts - LTR"
    // (variant: android, fuchsia, linux, windows)
    [Theory]
    [MemberData(nameof(NonApplePlatforms))]
    public void SliderCanBeIncrementedAndDecrementedByKeyboardShortcutsLtr(TargetPlatform platform)
    {
        KeyboardShortcutsTest(
            platform,
            TextDirection.Ltr,
            [(0.5, 0.55, 0.55), (0.55, 0.5, 0.5), (0.5, 0.55, 0.55), (0.55, 0.5, 0.5)]);
    }

    // Flutter: "Slider can be incremented and decremented by keyboard shortcuts - LTR"
    // (variant: iOS, macOS)
    [Theory]
    [MemberData(nameof(ApplePlatforms))]
    public void SliderCanBeIncrementedAndDecrementedByKeyboardShortcutsLtrApple(TargetPlatform platform)
    {
        KeyboardShortcutsTest(
            platform,
            TextDirection.Ltr,
            [(0.5, 0.6, 0.6), (0.6, 0.5, 0.5), (0.5, 0.6, 0.6), (0.6, 0.5, 0.5)]);
    }

    // Flutter: "Slider can be incremented and decremented by keyboard shortcuts - RTL"
    // (variant: android, fuchsia, linux, windows)
    [Theory]
    [MemberData(nameof(NonApplePlatforms))]
    public void SliderCanBeIncrementedAndDecrementedByKeyboardShortcutsRtl(TargetPlatform platform)
    {
        KeyboardShortcutsTest(
            platform,
            TextDirection.Rtl,
            [(0.5, 0.45, 0.45), (0.45, 0.5, 0.5), (0.5, 0.55, 0.55), (0.55, 0.5, 0.5)]);
    }

    // Flutter: "Slider can be incremented and decremented by keyboard shortcuts - RTL"
    // (variant: iOS, macOS)
    [Theory]
    [MemberData(nameof(ApplePlatforms))]
    public void SliderCanBeIncrementedAndDecrementedByKeyboardShortcutsRtlApple(TargetPlatform platform)
    {
        KeyboardShortcutsTest(
            platform,
            TextDirection.Rtl,
            [(0.5, 0.4, 0.4), (0.4, 0.5, 0.5), (0.5, 0.6, 0.6), (0.6, 0.5, 0.5)]);
    }

    private static void KeyboardShortcutsTest(
        TargetPlatform platform,
        TextDirection textDirection,
        (double Start, double Current, double End)[] expected)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        double startValue = 0.0;
        double currentValue = 0.5;
        double endValue = 0.0;
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder((context, setState) =>
                    {
                        Widget slider = new Slider(
                            value: currentValue,
                            onChangeStart: newValue => setState(() => startValue = newValue),
                            onChanged: newValue => setState(() => currentValue = newValue),
                            onChangeEnd: newValue => setState(() => endValue = newValue),
                            autofocus: true);
                        return textDirection == TextDirection.Rtl
                            ? new Directionality(TextDirection.Rtl, slider)
                            : slider;
                    })))));
        tester.PumpAndSettle();

        LogicalKeyboardKey[] keys =
        [
            LogicalKeyboardKey.ArrowRight,
            LogicalKeyboardKey.ArrowLeft,
            LogicalKeyboardKey.ArrowUp,
            LogicalKeyboardKey.ArrowDown,
        ];
        for (int i = 0; i < keys.Length; i++)
        {
            KeySim.SendKeyCombination(keys[i]);
            tester.PumpAndSettle();
            Assert.Equal(expected[i].Start, startValue);
            Assert.Equal(expected[i].Current, currentValue);
            Assert.Equal(expected[i].End, endValue);
        }
    }

    // Flutter: "In directional nav, Slider can be navigated out of by using up and down arrows"
    [Fact]
    public void InDirectionalNavSliderCanBeNavigatedOutOfByUsingUpAndDownArrows()
    {
        var shortcuts = new Dictionary<ShortcutActivator, Intent>
        {
            [new SingleActivator(LogicalKeyboardKey.ArrowLeft)] = new DirectionalFocusIntent(TraversalDirection.Left),
            [new SingleActivator(LogicalKeyboardKey.ArrowRight)] =
                new DirectionalFocusIntent(TraversalDirection.Right),
            [new SingleActivator(LogicalKeyboardKey.ArrowDown)] = new DirectionalFocusIntent(TraversalDirection.Down),
            [new SingleActivator(LogicalKeyboardKey.ArrowUp)] = new DirectionalFocusIntent(TraversalDirection.Up),
        };

        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        double topSliderValue = 0.5;
        double bottomSliderValue = 0.5;
        tester.PumpWidget(new MaterialApp(
            home: new Shortcuts(
                shortcuts: shortcuts,
                child: new MaterialWidget(
                    child: new Center(
                        child: new StatefulBuilder((context, setState) => new MediaQuery(
                            data: new MediaQueryData(NavigationMode: NavigationMode.Directional),
                            child: new Column(
                                children:
                                [
                                    new Slider(
                                        value: topSliderValue,
                                        onChanged: newValue => setState(() => topSliderValue = newValue),
                                        autofocus: true),
                                    new Slider(
                                        value: bottomSliderValue,
                                        onChanged: newValue => setState(() => bottomSliderValue = newValue)),
                                ]))))))));
        tester.PumpAndSettle();

        void Press(LogicalKeyboardKey key, double top, double bottom)
        {
            KeySim.SendKeyCombination(key);
            tester.PumpAndSettle();
            Assert.Equal(top, topSliderValue);
            Assert.Equal(bottom, bottomSliderValue);
        }

        // The top slider is auto-focused and can be adjusted with left and right arrow keys.
        Press(LogicalKeyboardKey.ArrowRight, 0.55, 0.5);
        Press(LogicalKeyboardKey.ArrowLeft, 0.5, 0.5);

        // Pressing the down-arrow key moves focus down to the bottom slider
        Press(LogicalKeyboardKey.ArrowDown, 0.5, 0.5);

        // The bottom slider is now focused and can be adjusted with left and right arrow keys.
        Press(LogicalKeyboardKey.ArrowRight, 0.5, 0.55);
        Press(LogicalKeyboardKey.ArrowLeft, 0.5, 0.5);

        // Pressing the up-arrow key moves focus back up to the top slider
        Press(LogicalKeyboardKey.ArrowUp, 0.5, 0.5);

        // The top slider is now focused again and can be adjusted with left and right arrow keys.
        Press(LogicalKeyboardKey.ArrowRight, 0.55, 0.5);
        Press(LogicalKeyboardKey.ArrowLeft, 0.5, 0.5);
    }

    // Flutter: "Slider gains keyboard focus when it gains semantics focus on Windows"
    // (variant: windows)
    [Fact]
    public void SliderGainsKeyboardFocusWhenItGainsSemanticsFocusOnWindows()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Windows;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        SemanticsNode.DebugResetSemanticsIdCounter();
        var semantics = new SemanticsTester(tester);
        var focusNode = new FocusNode();
        try
        {
            tester.PumpWidget(new MaterialApp(
                home: new MaterialWidget(
                    child: new Slider(value: 0.5, onChanged: _ => { }, focusNode: focusNode))));
            semantics.Flush();

            semantics.ExpectHasSemantics(new TestSemantics(
                id: 0,
                children:
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
                                        children:
                                        [
                                            new TestSemantics(
                                                id: 4,
                                                children:
                                                [
                                                    new TestSemantics(id: 6),
                                                    new TestSemantics(
                                                        id: 5,
                                                        flags: SemanticsFlags.HasEnabledState
                                                               | SemanticsFlags.IsEnabled
                                                               | SemanticsFlags.IsFocusable
                                                               | SemanticsFlags.IsSlider,
                                                        actions: SemanticsActions.Focus
                                                                 | SemanticsActions.Increase
                                                                 | SemanticsActions.Decrease
                                                                 | SemanticsActions.DidGainAccessibilityFocus,
                                                        value: "50%",
                                                        increasedValue: "55%",
                                                        decreasedValue: "45%",
                                                        textDirection: TextDirection.Ltr),
                                                ]),
                                        ]),
                                ]),
                        ]),
                ]));

            Assert.False(focusNode.HasFocus);
            semantics.Owner.PerformAction(5, SemanticsActions.DidGainAccessibilityFocus);
            tester.PumpAndSettle();
            Assert.True(focusNode.HasFocus);
        }
        finally
        {
            semantics.Dispose();
            tester.Dispose();
            focusNode.Dispose();
        }
    }

    // group: "Value indicator appears and disappears when it should:"
    // Regression test for https://github.com/flutter/flutter/issues/180767
    private sealed class ValueIndicatorGroup
    {
        public ValueIndicatorGroup()
        {
            BaseTheme = new ThemeData(platform: TargetPlatform.Android, primarySwatch: MaterialColors.Blue);
            BaseSliderTheme = BaseTheme.SliderTheme.CopyWith(
                valueIndicatorColor: MaterialColors.Red,
                valueIndicatorShape: new FixedSizeCircle());
        }

        public ThemeData BaseTheme { get; }

        public SliderThemeData BaseSliderTheme { get; }

        public double Value { get; set; } = 0.45;

        public Widget BuildApp(SliderThemeData sliderTheme, int? divisions = null, bool enabled = true)
        {
            Action<double>? onChanged = enabled ? d => Value = d : null;
            return new MaterialApp(
                home: new Directionality(
                    TextDirection.Ltr,
                    new MaterialWidget(
                        child: new Center(
                            child: new Theme(
                                data: BaseTheme,
                                child: new SliderTheme(
                                    data: sliderTheme,
                                    child: new Slider(
                                        value: Value,
                                        label: BindingBase.DartDoubleToString(Value),
                                        divisions: divisions,
                                        onChanged: onChanged)))))));
        }

        public void ExpectValueIndicator(
            FrameworkDartTester tester,
            bool visibleWhenDragged,
            bool visibleWhenReleased,
            SliderThemeData theme,
            int? divisions = null,
            bool enabled = true)
        {
            void ExpectIndicatorVisible(bool isVisible)
            {
                // _RenderValueIndicator is the last render object in the tree.
                RenderObject valueIndicatorBox = tester.AllElements()[^1].FindRenderObject()!;
                PaintPattern pattern = PaintPattern.Paints
                    .Circle(color: theme.ValueIndicatorColor)
                    .Paragraph();
                if (isVisible)
                {
                    PaintAssert.Paints(valueIndicatorBox, pattern);
                }
                else
                {
                    PaintAssert.DoesNotPaint(valueIndicatorBox, pattern);
                }
            }

            tester.PumpWidget(BuildApp(sliderTheme: theme, divisions: divisions, enabled: enabled));
            ExpectIndicatorVisible(visibleWhenReleased);

            Point center = tester.GetCenter(tester.ElementOfType<Slider>());
            TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
            tester.PumpAndSettle();
            ExpectIndicatorVisible(visibleWhenDragged);

            gesture.Up();
            tester.PumpAndSettle();
            ExpectIndicatorVisible(visibleWhenReleased);

            // Reset state to avoid state leak.
            tester.PumpWidget(new Container());
        }
    }

    // Flutter: "showValueIndicator set to onlyForDiscrete" (group: Value indicator appears and
    // disappears when it should:)
    [Fact]
    public void ShowValueIndicatorSetToOnlyForDiscrete()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var group = new ValueIndicatorGroup();

        // The default value is onlyForDiscrete. No modification is needed.
        SliderThemeData sliderTheme = group.BaseSliderTheme.CopyWith();
        group.ExpectValueIndicator(tester, theme: sliderTheme, divisions: 3,
            visibleWhenDragged: true, visibleWhenReleased: false);
        group.ExpectValueIndicator(tester, theme: sliderTheme, divisions: 3, enabled: false,
            visibleWhenDragged: false, visibleWhenReleased: false);
        group.ExpectValueIndicator(tester, theme: sliderTheme,
            visibleWhenDragged: false, visibleWhenReleased: false);
        group.ExpectValueIndicator(tester, theme: sliderTheme, enabled: false,
            visibleWhenDragged: false, visibleWhenReleased: false);
    }

    // Flutter: "showValueIndicator set to onlyForContinuous" (group: Value indicator appears and
    // disappears when it should:)
    [Fact]
    public void ShowValueIndicatorSetToOnlyForContinuous()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var group = new ValueIndicatorGroup();
        SliderThemeData sliderTheme = group.BaseSliderTheme.CopyWith(
            showValueIndicator: ShowValueIndicator.OnlyForContinuous);
        group.ExpectValueIndicator(tester, theme: sliderTheme, divisions: 3,
            visibleWhenDragged: false, visibleWhenReleased: false);
        group.ExpectValueIndicator(tester, theme: sliderTheme, divisions: 3, enabled: false,
            visibleWhenDragged: false, visibleWhenReleased: false);
        group.ExpectValueIndicator(tester, theme: sliderTheme,
            visibleWhenDragged: true, visibleWhenReleased: false);
        group.ExpectValueIndicator(tester, theme: sliderTheme, enabled: false,
            visibleWhenDragged: false, visibleWhenReleased: false);
    }

    // Flutter: "showValueIndicator set to onDrag" (group: Value indicator appears and disappears
    // when it should:)
    [Fact]
    public void ShowValueIndicatorSetToOnDrag()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var group = new ValueIndicatorGroup();
        SliderThemeData sliderTheme = group.BaseSliderTheme.CopyWith(
            showValueIndicator: ShowValueIndicator.OnDrag);
        group.ExpectValueIndicator(tester, theme: sliderTheme, divisions: 3,
            visibleWhenDragged: true, visibleWhenReleased: false);
        group.ExpectValueIndicator(tester, theme: sliderTheme, divisions: 3, enabled: false,
            visibleWhenDragged: false, visibleWhenReleased: false);
        group.ExpectValueIndicator(tester, theme: sliderTheme,
            visibleWhenDragged: true, visibleWhenReleased: false);
        group.ExpectValueIndicator(tester, theme: sliderTheme, enabled: false,
            visibleWhenDragged: false, visibleWhenReleased: false);
    }

    // Flutter: "showValueIndicator set to never" (group: Value indicator appears and disappears
    // when it should:)
    [Fact]
    public void ShowValueIndicatorSetToNever()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var group = new ValueIndicatorGroup();
        SliderThemeData sliderTheme = group.BaseSliderTheme.CopyWith(
            showValueIndicator: ShowValueIndicator.Never);
        group.ExpectValueIndicator(tester, theme: sliderTheme, divisions: 3,
            visibleWhenDragged: false, visibleWhenReleased: false);
        group.ExpectValueIndicator(tester, theme: sliderTheme, divisions: 3, enabled: false,
            visibleWhenDragged: false, visibleWhenReleased: false);
        group.ExpectValueIndicator(tester, theme: sliderTheme,
            visibleWhenDragged: false, visibleWhenReleased: false);
        group.ExpectValueIndicator(tester, theme: sliderTheme, enabled: false,
            visibleWhenDragged: false, visibleWhenReleased: false);
    }

    // Flutter: "showValueIndicator set to alwaysVisible" (group: Value indicator appears and
    // disappears when it should:)
    [Fact]
    public void ShowValueIndicatorSetToAlwaysVisible()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var group = new ValueIndicatorGroup();
        SliderThemeData sliderTheme = group.BaseSliderTheme.CopyWith(
            showValueIndicator: ShowValueIndicator.AlwaysVisible);
        group.ExpectValueIndicator(tester, theme: sliderTheme, divisions: 3,
            visibleWhenDragged: true, visibleWhenReleased: true);
        group.ExpectValueIndicator(tester, theme: sliderTheme, divisions: 3, enabled: false,
            visibleWhenDragged: false, visibleWhenReleased: false);
        group.ExpectValueIndicator(tester, theme: sliderTheme,
            visibleWhenDragged: true, visibleWhenReleased: true);
        group.ExpectValueIndicator(tester, theme: sliderTheme, enabled: false,
            visibleWhenDragged: false, visibleWhenReleased: false);
    }

    // Flutter: "Slider doesn't start any animations after dispose"
    [Fact]
    public void SliderDoesNotStartAnyAnimationsAfterDispose()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        Key sliderKey = new UniqueKey();
        double value = 0.0;
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new StatefulBuilder((context, setState) => new MaterialWidget(
                    child: new Center(
                        child: new Slider(
                            key: sliderKey,
                            value: value,
                            divisions: 4,
                            onChanged: newValue => setState(() => value = newValue))))))));

        TestGesture gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementsWithKey(sliderKey).Single()),
            PointerDeviceKind.Touch);
        tester.PumpAndSettle();
        Assert.Equal(0.5, value);
        gesture.MoveBy(new Vector(-500.0, 0.0));
        tester.PumpAndSettle();
        // Change the tree to dispose the original widget.
        tester.PumpWidget(new Container());
        Assert.Equal(1, tester.PumpAndSettle());
        gesture.Up();
    }

    // Flutter: "Slider removes value indicator from overlay if Slider gets disposed without value
    // indicator animation completing."
    [Fact]
    public void SliderRemovesValueIndicatorFromOverlayIfDisposedWithoutAnimationCompleting()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        Key sliderKey = new UniqueKey();
        var fillColor = new Color(0xf55f5f5f);
        double value = 0.0;

        Widget BuildApp(int? divisions = null)
        {
            return new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new Scaffold(
                    body: new Builder(
                        // The builder is used to pass the context from the MaterialApp widget
                        // to the [Navigator]. This context is required in order for the
                        // Navigator to work.
                        context => new Column(
                            children:
                            [
                                new Slider(
                                    key: sliderKey,
                                    max: 100.0,
                                    divisions: divisions,
                                    label: $"{Math.Round(value, MidpointRounding.AwayFromZero)}",
                                    value: value,
                                    onChanged: newValue => value = newValue),
                                new ElevatedButton(
                                    child: new Text("Next"),
                                    onPressed: () =>
                                    {
                                        Navigator.Of(context).PushReplacement(
                                            new MaterialPageRoute(
                                                builder: innerContext => new ElevatedButton(
                                                    child: new Text("Inner page"),
                                                    onPressed: () => Navigator.Of(innerContext).Pop())));
                                    }),
                            ]))));
        }

        tester.PumpWidget(BuildApp(divisions: 3));

        RenderObject valueIndicatorBox = tester.ElementOfType<Overlay>().FindRenderObject()!;
        Point topRight = tester.GetTopLeft(tester.ElementOfType<Slider>())
                         + new Vector(tester.GetSize(tester.ElementOfType<Slider>()).Width - 24, 0);
        TestGesture gesture = tester.StartGesture(topRight, PointerDeviceKind.Touch);
        // Wait for value indicator animation to finish.
        tester.PumpAndSettle();

        Assert.Single(tester.ElementsOfType<Slider>());
        PaintAssert.Paints(
            valueIndicatorBox,
            PaintPattern.Paints
                // Represents the raised button with text, next.
                .Path(color: MaterialColors.Black)
                .Paragraph()
                // Represents the Slider.
                .Path(color: fillColor)
                .Paragraph());

        IReadOnlyList<CanvasCall> calls = PaintRecording.Record(valueIndicatorBox);
        Assert.Equal(4, calls.CountCalls("drawPath"));
        Assert.Equal(2, calls.CountCalls("drawParagraph"));

        tester.Tap(tester.ElementsWithText("Next").Single());
        tester.PumpAndSettle();

        Assert.Empty(tester.ElementsOfType<Slider>());
        PaintAssert.DoesNotPaint(
            valueIndicatorBox,
            PaintPattern.Paints.Path(color: fillColor).Paragraph());

        // Represents the ElevatedButton with inner Text, inner page.
        calls = PaintRecording.Record(valueIndicatorBox);
        Assert.Equal(2, calls.CountCalls("drawPath"));
        Assert.Equal(1, calls.CountCalls("drawParagraph"));

        // Don't stop holding the value indicator.
        gesture.Up();
        tester.PumpAndSettle();
    }

    // Flutter: "Slider.adaptive"
    [Fact]
    public void SliderAdaptive()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        double value = 0.5;

        Widget BuildFrame(TargetPlatform platform)
        {
            return new MaterialApp(
                theme: new ThemeData(platform: platform),
                home: new StatefulBuilder((context, setState) => new MaterialWidget(
                    child: new Center(
                        child: Slider.Adaptive(
                            value: value,
                            onChanged: newValue => setState(() => value = newValue))))));
        }

        foreach (TargetPlatform platform in new[] { TargetPlatform.IOS, TargetPlatform.MacOS })
        {
            value = 0.5;
            tester.PumpWidget(BuildFrame(platform));
            Assert.Single(tester.ElementsOfType<Slider>());
            Assert.Single(tester.ElementsOfType<CupertinoSlider>());

            Assert.Equal(0.5, value);
            TestGesture gesture = tester.StartGesture(
                tester.GetCenter(tester.ElementOfType<CupertinoSlider>()),
                PointerDeviceKind.Touch);
            // Drag to the right end of the track.
            gesture.MoveBy(new Vector(600.0, 0.0));
            Assert.Equal(1.0, value);
            gesture.Up();
        }

        foreach (TargetPlatform platform in new[]
                 {
                     TargetPlatform.Android, TargetPlatform.Fuchsia, TargetPlatform.Linux, TargetPlatform.Windows,
                 })
        {
            value = 0.5;
            tester.PumpWidget(BuildFrame(platform));
            tester.PumpAndSettle(); // Finish the theme change animation.
            Assert.Single(tester.ElementsOfType<Slider>());
            Assert.Empty(tester.ElementsOfType<CupertinoSlider>());

            Assert.Equal(0.5, value);
            TestGesture gesture = tester.StartGesture(
                tester.GetCenter(tester.ElementOfType<Slider>()),
                PointerDeviceKind.Touch);
            // Drag to the right end of the track.
            gesture.MoveBy(new Vector(600.0, 0.0));
            Assert.Equal(1.0, value);
            gesture.Up();
        }
    }

    // Flutter: "Slider respects height from theme"
    [Fact]
    public void SliderRespectsHeightFromTheme()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        Key sliderKey = new UniqueKey();
        double value = 0.0;
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new StatefulBuilder((context, setState) =>
                {
                    SliderThemeData sliderTheme = SliderTheme.Of(context)
                        .CopyWith(tickMarkShape: new TallSliderTickMarkShape());
                    return new MaterialWidget(
                        child: new Center(
                            child: new IntrinsicHeight(
                                child: new SliderTheme(
                                    data: sliderTheme,
                                    child: new Slider(
                                        key: sliderKey,
                                        value: value,
                                        divisions: 4,
                                        onChanged: newValue => setState(() => value = newValue))))));
                }))));

        var renderObject = (RenderBox)tester.ElementOfType<Slider>().FindRenderObject()!;
        Assert.Equal(200, renderObject.Size.Height);
    }

    // Flutter: "Slider changes mouse cursor when hovered"
    [Fact]
    public void SliderChangesMouseCursorWhenHovered()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);

        // Test Slider() constructor
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new MaterialWidget(
                    child: new Center(
                        child: new MouseRegion(
                            cursor: SystemMouseCursors.Forbidden,
                            child: new Slider(
                                mouseCursor: SystemMouseCursors.Text,
                                value: 0.5,
                                onChanged: _ => { })))))));

        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer(location: tester.GetCenter(tester.ElementOfType<Slider>()));

        tester.Pump();

        Assert.Equal(SystemMouseCursors.Text, ActiveCursor(gesture));

        // Test Slider.adaptive() constructor
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new MaterialWidget(
                    child: new Center(
                        child: new MouseRegion(
                            cursor: SystemMouseCursors.Forbidden,
                            child: Slider.Adaptive(
                                mouseCursor: SystemMouseCursors.Text,
                                value: 0.5,
                                onChanged: _ => { })))))));

        Assert.Equal(SystemMouseCursors.Text, ActiveCursor(gesture));

        // Test default cursor
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new MaterialWidget(
                    child: new Center(
                        child: new MouseRegion(
                            cursor: SystemMouseCursors.Forbidden,
                            child: new Slider(value: 0.5, onChanged: _ => { })))))));

        Assert.Equal(SystemMouseCursors.Click, ActiveCursor(gesture));
        gesture.RemovePointer();
    }

    // Flutter: "Slider WidgetStateMouseCursor resolves correctly"
    [Fact]
    public void SliderWidgetStateMouseCursorResolvesCorrectly()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        MouseCursor systemDefaultCursor = SystemMouseCursors.Basic;
        MouseCursor regularCursor = SystemMouseCursors.Click;
        MouseCursor disabledCursor = SystemMouseCursors.Forbidden;
        MouseCursor focusedCursor = SystemMouseCursors.Precise;
        MouseCursor hoveredCursor = SystemMouseCursors.Grab;
        MouseCursor draggedCursor = SystemMouseCursors.Move;

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
                                    child: new Slider(
                                        mouseCursor: new StateDependentMouseCursor(
                                            disabled: disabledCursor,
                                            focused: focusedCursor,
                                            hovered: hoveredCursor,
                                            dragged: draggedCursor,
                                            regular: regularCursor),
                                        value: 0.5,
                                        onChanged: enabled ? _ => { } : null)),
                            ]))));
        }

        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        try
        {
            // System default.
            gesture.AddPointer(location: default);
            tester.Pump();
            Assert.Equal(systemDefaultCursor, ActiveCursor(gesture));

            // Disabled.
            tester.PumpWidget(BuildFrame(enabled: false));
            gesture.MoveTo(tester.GetCenter(tester.ElementOfType<Slider>()));
            tester.Pump();
            Assert.Equal(disabledCursor, ActiveCursor(gesture));

            // Regular.
            tester.PumpWidget(BuildFrame(enabled: true));
            Assert.Equal(regularCursor, ActiveCursor(gesture));

            // Hovered.
            tester.Pump();
            Assert.Equal(hoveredCursor, ActiveCursor(gesture));

            // Dragged.
            gesture.Down(tester.GetCenter(tester.ElementOfType<Slider>()));
            gesture.MoveBy(new Vector(20.0, 0.0));
            tester.Pump();
            Assert.Equal(draggedCursor, ActiveCursor(gesture));

            // Hovered.
            gesture.Up();
            tester.Pump();
            Assert.Equal(hoveredCursor, ActiveCursor(gesture));

            // Focused.
            KeySim.SendKeyCombination(LogicalKeyboardKey.Tab);
            tester.Pump();
            Assert.Equal(focusedCursor, ActiveCursor(gesture));

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

    // Flutter: "Slider implements debugFillProperties"
    [Fact]
    public void SliderImplementsDebugFillProperties()
    {
        var builder = new DiagnosticPropertiesBuilder();

        new Slider(
            activeColor: MaterialColors.Blue,
            divisions: 10,
            inactiveColor: MaterialColors.Grey,
            secondaryActiveColor: MaterialColors.BlueGrey,
            label: "Set a value",
            max: 100.0,
            onChanged: null,
            value: 50.0,
            secondaryTrackValue: 75.0).DebugFillProperties(builder);

        List<string> description = builder.Properties
            .Where(node => !node.IsFiltered(DiagnosticLevel.Info))
            .Select(node => node.ToString())
            .ToList();

        Assert.Equal(
            [
                "value: 50.0",
                "secondaryTrackValue: 75.0",
                "disabled",
                "min: 0.0",
                "max: 100.0",
                "divisions: 10",
                "label: \"Set a value\"",
                $"activeColor: MaterialColor(primary value: {new Color(0xff2196f3)})",
                $"inactiveColor: MaterialColor(primary value: {new Color(0xff9e9e9e)})",
                $"secondaryActiveColor: MaterialColor(primary value: {new Color(0xff607d8b)})",
            ],
            description);
    }

    // Flutter: "Slider track paints correctly when the shape is rectangular"
    [Fact]
    public void SliderTrackPaintsCorrectlyWhenTheShapeIsRectangular()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(
                sliderTheme: new SliderThemeData(TrackShape: new RectangularSliderTrackShape())),
            home: new Directionality(
                TextDirection.Ltr,
                new MaterialWidget(child: new Center(child: new Slider(value: 0.5, onChanged: null))))));

        RenderSlider renderObject = FirstRenderSlider(tester);

        // The active track rect should start at 24.0 pixels,
        // and there should not have a gap between active and inactive track.
        PaintAssert.Paints(
            renderObject,
            PaintPattern.Paints
                .Rect(rect: new Rect(new Point(24.0, 298.0), new Point(400.0, 302.0))) // active track Rect.
                .Rect(rect: new Rect(new Point(400.0, 298.0), new Point(776.0, 302.0)))); // inactive track Rect.
    }

    // Flutter: "SliderTheme change should trigger re-layout"
    [Fact]
    public void SliderThemeChangeShouldTriggerReLayout()
    {
        // Regression test for https://github.com/flutter/flutter/issues/118955
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        double sliderValue = 0.0;

        Widget BuildFrame(ThemeMode themeMode)
        {
            return new MaterialApp(
                themeMode: themeMode,
                theme: new ThemeData(brightness: Brightness.Light),
                darkTheme: new ThemeData(brightness: Brightness.Dark),
                home: new Directionality(
                    TextDirection.Ltr,
                    new MaterialWidget(
                        child: new Center(
                            child: SizedBox.Square(
                                dimension: 10.0,
                                child: new Slider(
                                    value: sliderValue,
                                    label: "label",
                                    onChanged: value => sliderValue = value))))));
        }

        tester.PumpWidget(BuildFrame(ThemeMode.Light));

        RenderSlider renderObject = FirstRenderSlider(tester);
        Assert.False(renderObject.DebugNeedsLayout);

        tester.PumpWidget(BuildFrame(ThemeMode.Dark));
        PumpBuildPhase(tester, TimeSpan.FromMilliseconds(100)); // to let the theme animate

        Assert.True(renderObject.DebugNeedsLayout);

        // Pump the rest of the frames to complete the test.
        tester.PumpAndSettle();
    }

    // Flutter: "Slider can be painted in a narrower constraint"
    [Fact]
    public void SliderCanBePaintedInANarrowerConstraint()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new MaterialWidget(
                    child: new Center(
                        child: SizedBox.Square(
                            dimension: 10.0,
                            child: new Slider(value: 0.5, onChanged: null)))))));

        RenderSlider renderObject = FirstRenderSlider(tester);

        PaintAssert.Paints(
            renderObject,
            PaintPattern.Paints
                // Inactive track RRect.
                .RRect(rrect: RRect.FromLTRBR(3.0, 3.0, 24.0, 7.0, Radius.Circular(2.0)))
                // Active track RRect.
                .RRect(rrect: RRect.FromLTRBR(-14.0, 2.0, 7.0, 8.0, Radius.Circular(3.0)))
                // Thumb.
                .Circle(x: 5.0, y: 5.0, radius: 10.0));
    }

    // Flutter: "Update the divisions and value at the same time for Slider"
    [Fact]
    public void UpdateTheDivisionsAndValueAtTheSameTimeForSlider()
    {
        // Regress test for https://github.com/flutter/flutter/issues/65943
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);

        Widget BuildFrame(double maxValue)
        {
            return new MaterialApp(
                home: new MaterialWidget(
                    child: new Center(
                        child: Slider.Adaptive(
                            value: 5,
                            max: maxValue,
                            divisions: (int)maxValue,
                            onChanged: _ => { }))));
        }

        tester.PumpWidget(BuildFrame(10));

        RenderSlider renderObject = FirstRenderSlider(tester);

        // Update the divisions from 10 to 15, the thumb should be paint at the correct position.
        tester.PumpWidget(BuildFrame(15));
        tester.PumpAndSettle(); // Finish the animation.

        List<CanvasCall> rrects = PaintRecording.Record(renderObject)
            .Where(call => call.Method == "drawRRect")
            .ToList();
        Assert.True(rrects.Count >= 2, "expected at least two drawRRect calls");
        RRect activeTrackRRect = rrects[1].RRect!.Value;

        const double padding = 4.0;
        // The thumb should at one-third(5 / 15) of the Slider.
        // The right of the active track shape is the position of the thumb.
        // 24.0 is the default margin, (800.0 - 24.0 - 24.0) is the slider's width.
        Assert.True(NearEqual(
            activeTrackRRect.Right,
            ((800.0 - 24.0 - 24.0 + (padding / 2)) * (5.0 / 15.0)) + 24.0 + (padding / 2),
            0.01));
    }

    // Flutter: "Slider paints thumbColor"
    [Fact]
    public void SliderPaintsThumbColor()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var color = new Color(0xffffc107);

        Widget sliderAdaptive = new MaterialApp(
            theme: new ThemeData(platform: TargetPlatform.IOS),
            home: new MaterialWidget(
                child: new Slider(value: 0, onChanged: _ => { }, thumbColor: color)));

        tester.PumpWidget(sliderAdaptive);
        tester.PumpAndSettle();

        PaintAssert.Paints(MaterialOf(tester), PaintPattern.Paints.Circle(color: color));
    }

    // Flutter: "Slider.adaptive paints thumbColor on Android"
    [Fact]
    public void SliderAdaptivePaintsThumbColorOnAndroid()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var color = new Color(0xffffc107);

        Widget sliderAdaptive = new MaterialApp(
            theme: new ThemeData(platform: TargetPlatform.Android),
            home: new MaterialWidget(
                child: Slider.Adaptive(value: 0, onChanged: _ => { }, thumbColor: color)));

        tester.PumpWidget(sliderAdaptive);
        tester.PumpAndSettle();

        PaintAssert.Paints(MaterialOf(tester), PaintPattern.Paints.Circle(color: color));
    }

    // Flutter: "If thumbColor is null, it defaults to CupertinoColors.white"
    [Fact]
    public void IfThumbColorIsNullItDefaultsToCupertinoColorsWhite()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        Widget sliderAdaptive = new MaterialApp(
            theme: new ThemeData(platform: TargetPlatform.IOS),
            home: new MaterialWidget(child: Slider.Adaptive(value: 0, onChanged: _ => { })));

        tester.PumpWidget(sliderAdaptive);
        tester.PumpAndSettle();

        PaintAssert.Paints(
            MaterialOf(tester, tester.ElementOfType<CupertinoSlider>()),
            PaintPattern.Paints
                .RRect()
                .RRect()
                .RRect()
                .RRect()
                .RRect()
                .RRect(color: CupertinoColors.White));
    }

    // Flutter: "Slider.adaptive passes thumbColor to CupertinoSlider"
    [Fact]
    public void SliderAdaptivePassesThumbColorToCupertinoSlider()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var color = new Color(0xffffc107);

        Widget sliderAdaptive = new MaterialApp(
            theme: new ThemeData(platform: TargetPlatform.IOS),
            home: new MaterialWidget(
                child: Slider.Adaptive(value: 0, onChanged: _ => { }, thumbColor: color)));

        tester.PumpWidget(sliderAdaptive);
        tester.PumpAndSettle();

        PaintAssert.Paints(
            MaterialOf(tester, tester.ElementOfType<CupertinoSlider>()),
            PaintPattern.Paints
                .RRect()
                .RRect()
                .RRect()
                .RRect()
                .RRect()
                .RRect(color: color));
    }

    // Flutter: "Drag gesture uses provided gesture settings"
    // Regression test for https://github.com/flutter/flutter/issues/103566
    [Fact]
    public void DragGestureUsesProvidedGestureSettings()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        double value = 0.5;
        bool dragStarted = false;
        Key sliderKey = new UniqueKey();

        Widget BuildApp(double touchSlop, Action<double>? onChangeEnd)
        {
            return new MaterialApp(
                home: new Directionality(
                    TextDirection.Ltr,
                    new StatefulBuilder((context, setState) => new MaterialWidget(
                        child: new Center(
                            child: new GestureDetector(
                                behavior: HitTestBehavior.DeferToChild,
                                onHorizontalDragStart: _ => dragStarted = true,
                                child: new MediaQuery(
                                    data: MediaQuery.Of(context).CopyWith(
                                        gestureSettings: new DeviceGestureSettings(TouchSlop: touchSlop)),
                                    child: new Slider(
                                        value: value,
                                        key: sliderKey,
                                        onChanged: newValue => setState(() => value = newValue),
                                        onChangeEnd: onChangeEnd))))))));
        }

        tester.PumpWidget(BuildApp(20, null));

        TestGesture drag = tester.StartGesture(
            tester.GetCenter(tester.ElementsWithKey(sliderKey).Single()),
            PointerDeviceKind.Touch);
        tester.Pump(GestureConstants.PressTimeout);

        // Less than configured touch slop, more than default touch slop
        drag.MoveBy(new Vector(19.0, 0));
        tester.Pump();

        Assert.Equal(0.5, value);
        Assert.True(dragStarted);

        dragStarted = false;

        drag.Up();
        tester.PumpAndSettle();

        drag = tester.StartGesture(
            tester.GetCenter(tester.ElementsWithKey(sliderKey).Single()),
            PointerDeviceKind.Touch);
        tester.Pump(GestureConstants.PressTimeout);

        bool sliderEnd = false;

        tester.PumpWidget(BuildApp(10, _ => sliderEnd = true));

        // More than touch slop.
        drag.MoveBy(new Vector(12.0, 0));

        drag.Up();
        tester.PumpAndSettle();

        Assert.True(sliderEnd);
        Assert.False(dragStarted);
    }

    // Flutter: "Slider does not request focus when the value is changed"
    // Regression test for https://github.com/flutter/flutter/issues/139281
    [Fact]
    public void SliderDoesNotRequestFocusWhenTheValueIsChanged()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        double value = 0.5;
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new Center(
                    child: new StatefulBuilder((context, setState) => new Slider(
                        value: value,
                        onChanged: newValue => setState(() => value = newValue)))))));
        // Initially, the slider does not have focus whe enabled and not tapped.
        tester.PumpAndSettle();
        Assert.Equal(0.5, value);
        // Get FocusNode from the state of the slider to include auto-generated FocusNode.
        FocusNode focusNode = tester.State<SliderState>().FocusNode;
        // The slider does not have focus.
        Assert.False(focusNode.HasFocus);
        Point sliderCenter = tester.GetCenter(tester.ElementOfType<Slider>());
        var tapLocation = new Point(sliderCenter.X + 50, sliderCenter.Y);
        // Tap on the slider to change the value.
        TestGesture gesture = tester.CreateGesture();
        gesture.AddPointer();
        gesture.Down(tapLocation);
        gesture.Up();
        tester.PumpAndSettle();
        Assert.NotEqual(0.5, value);
        // The slider does not have focus after the value is changed.
        Assert.False(focusNode.HasFocus);
    }

    // ---------------------------------------------------------------------------------------------
    // Harness helpers
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Dart's <c>Material.of(tester.element(find.byType(Slider)))</c> used as a <c>paints</c> target:
    /// the ink-features render object (Dart's <c>_RenderInkFeatures</c> is the controller itself).
    /// </summary>
    private static RenderObject MaterialOf(FrameworkDartTester tester, Element? element = null)
    {
        element ??= tester.ElementOfType<Slider>();
        RenderObject? current = element.FindRenderObject();
        while (current is not null and not RenderInkFeatures)
        {
            current = current.Parent;
        }

        return current ?? throw new XunitException("No Material ancestor.");
    }

    private static RenderSlider FirstRenderSlider(FrameworkDartTester tester) =>
        tester.AllElements().Select(element => element.FindRenderObject()).OfType<RenderSlider>().First();

    private static MouseCursor? ActiveCursor(TestGesture gesture) =>
        RendererBinding.Instance.MouseTracker.DebugDeviceActiveCursor(gesture.Pointer);

    // Dart's `nearEqual` (physics/utils.dart).
    private static bool NearEqual(double a, double b, double epsilon) =>
        (a > b - epsilon && a < b + epsilon) || a == b;

    private static readonly FieldInfo TesterClock =
        typeof(FrameworkDartTester).GetField("_clock", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>
    /// Dart's <c>tester.pump(duration, EnginePhase.build)</c>: the frame runs the transient callbacks,
    /// then only <c>buildScope</c> and <c>finalizeTree</c> (TestWidgetsFlutterBinding.drawFrame).
    /// </summary>
    private static void PumpBuildPhase(FrameworkDartTester tester, TimeSpan duration)
    {
        var clock = (TimeSpan)TesterClock.GetValue(tester)!;
        clock += duration;
        TesterClock.SetValue(tester, clock);
        RendererBinding binding = RendererBinding.Instance;
        binding.EnsurePersistentFrameCallback();
        binding.DrawFrameOverrideForTests = () =>
        {
            tester.Owner.BuildScope(tester.Root);
            tester.Owner.FinalizeTree();
        };
        try
        {
            Scheduler.HandleBeginFrame(clock);
            Scheduler.FlushMicrotasks();
            Scheduler.HandleDrawFrame();
        }
        finally
        {
            binding.DrawFrameOverrideForTests = null;
        }
    }

    // ---------------------------------------------------------------------------------------------
    // File-level test classes from slider_test.dart
    // ---------------------------------------------------------------------------------------------

    /// <summary>slider_test.dart's <c>TallSliderTickMarkShape</c>.</summary>
    private sealed class TallSliderTickMarkShape : SliderTickMarkShape
    {
        public override Size GetPreferredSize(SliderThemeData sliderTheme, bool isEnabled) => new(10.0, 200.0);

        public override void Paint(
            PaintingContext context,
            Point center,
            RenderBox parentBox,
            SliderThemeData sliderTheme,
            Animation<double> enableAnimation,
            Point thumbCenter,
            bool isEnabled,
            TextDirection textDirection)
        {
            var paint = new Paint { Color = MaterialColors.Red };
            context.Canvas.DrawRect(new Rect(center.X, center.Y, 10.0, 20.0), paint);
        }
    }

    /// <summary>slider_test.dart's <c>_StateDependentMouseCursor</c>.</summary>
    private sealed class StateDependentMouseCursor(
        MouseCursor disabled,
        MouseCursor focused,
        MouseCursor hovered,
        MouseCursor dragged,
        MouseCursor regular) : WidgetStateMouseCursor("_StateDependentMouseCursor")
    {
        public override MouseCursor Resolve(IReadOnlySet<WidgetState> states)
        {
            if (states.Contains(WidgetState.Disabled))
            {
                return disabled;
            }

            if (states.Contains(WidgetState.Focused))
            {
                return focused;
            }

            if (states.Contains(WidgetState.Dragged))
            {
                return dragged;
            }

            if (states.Contains(WidgetState.Hovered))
            {
                return hovered;
            }

            return regular;
        }
    }

    /// <summary>
    /// slider_test.dart's <c>_FixedSizeCircle</c>: lets the tests verify that a <c>Slider</c> removes
    /// the value indicator painter after the animation is dismissed.
    /// </summary>
    private sealed class FixedSizeCircle : SliderComponentShape
    {
        private const double CircleDiameter = 40.0;

        public override Size GetPreferredSize(bool isEnabled, bool isDiscrete) =>
            new(CircleDiameter, CircleDiameter);

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
            Canvas canvas = context.Canvas;
            var paint = new Avalonia.Media.SolidColorBrush(
                sliderTheme.ValueIndicatorColor ?? MaterialColors.Purple);

            canvas.DrawCircle(paint, null, center, CircleDiameter / 2);
            labelPainter.Paint(canvas, center);
        }
    }

    // ---------------------------------------------------------------------------------------------
    // semantics_tester.dart
    // ---------------------------------------------------------------------------------------------

    /// <summary>semantics_tester.dart's <c>SemanticsTester</c>: keeps semantics on for the view.</summary>
    private sealed class SemanticsTester : IDisposable
    {
        private readonly FrameworkDartTester _tester;
        private SemanticsHandle? _handle;

        public SemanticsTester(FrameworkDartTester tester)
        {
            _tester = tester;
            PipelineOwner owner = tester.RenderView.Owner!;
            bool createsOwner = owner.SemanticsOwner is null;
            _handle = owner.EnsureSemantics();
            if (createsOwner)
            {
                tester.RenderView.ClearSemantics();
                tester.RenderView.ScheduleInitialSemantics();
            }
        }

        public SemanticsOwner Owner => _tester.RenderView.Owner!.SemanticsOwner!;

        /// <summary>
        /// flutter_test's frame runs <c>flushSemantics</c> after compositing; the C# tester's frame
        /// stops at composite.
        /// </summary>
        public void Flush() => _tester.RenderView.Owner!.FlushSemantics();

        /// <summary>
        /// Dart's <c>expect(semantics, hasSemantics(expected, ignoreRect: true, ignoreTransform: true))</c>
        /// in traversal child order (the hasSemantics default).
        /// </summary>
        public void ExpectHasSemantics(TestSemantics expected)
        {
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
    }

    /// <summary>semantics_tester.dart's <c>TestSemantics</c> (rects and transforms ignored).</summary>
    private sealed class TestSemantics(
        int? id = null,
        SemanticsFlags flags = SemanticsFlags.None,
        SemanticsActions actions = SemanticsActions.None,
        string label = "",
        string value = "",
        string increasedValue = "",
        string decreasedValue = "",
        string hint = "",
        TextDirection? textDirection = null,
        IReadOnlyList<TestSemantics>? children = null)
    {
        private readonly IReadOnlyList<TestSemantics> _children = children ?? [];

        public string? Match(SemanticsNode node, string path)
        {
            SemanticsData data = node.GetSemanticsData();
            var errors = new StringBuilder();
            if (id is { } expectedId && expectedId != node.Id)
            {
                errors.Append($" expected id {expectedId} but found id {node.Id};");
            }

            if (data.Flags != flags)
            {
                errors.Append($" flags: expected {flags} but found {data.Flags};");
            }

            if (data.Actions != actions)
            {
                errors.Append($" actions: expected {actions} but found {data.Actions};");
            }

            if (data.AttributedLabel.String != label)
            {
                errors.Append($" label: expected \"{label}\" but found \"{data.AttributedLabel.String}\";");
            }

            if (data.AttributedValue.String != value)
            {
                errors.Append($" value: expected \"{value}\" but found \"{data.AttributedValue.String}\";");
            }

            if (data.AttributedIncreasedValue.String != increasedValue)
            {
                errors.Append(
                    $" increasedValue: expected \"{increasedValue}\" but found "
                    + $"\"{data.AttributedIncreasedValue.String}\";");
            }

            if (data.AttributedDecreasedValue.String != decreasedValue)
            {
                errors.Append(
                    $" decreasedValue: expected \"{decreasedValue}\" but found "
                    + $"\"{data.AttributedDecreasedValue.String}\";");
            }

            if (data.AttributedHint.String != hint || data.Tooltip != string.Empty)
            {
                errors.Append(" unexpected hint/tooltip;");
            }

            if (textDirection != null && textDirection != data.TextDirection)
            {
                errors.Append($" textDirection: expected {textDirection} but found {data.TextDirection};");
            }

            if ((data.AttributedLabel.String != string.Empty
                 || data.AttributedValue.String != string.Empty
                 || data.AttributedHint.String != string.Empty
                 || data.AttributedIncreasedValue.String != string.Empty
                 || data.AttributedDecreasedValue.String != string.Empty)
                && data.TextDirection == null)
            {
                errors.Append(" a node with a label, value, or hint must have a textDirection;");
            }

            if (data.TextSelection != null)
            {
                errors.Append($" expected no text selection but found {data.TextSelection};");
            }

            if (data.Role != SemanticsRole.None)
            {
                errors.Append($" role: expected None but found {data.Role};");
            }

            if (data.MaxValueLength != null || data.CurrentValueLength != null)
            {
                errors.Append(" expected no maxValueLength/currentValueLength;");
            }

            IReadOnlyList<SemanticsNode> ordered =
                node.DebugListChildrenInOrder(DebugSemanticsDumpOrder.TraversalOrder);
            int childrenCount = node.MergeAllDescendantsIntoThisNode ? 0 : ordered.Count;
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
                string? childFailure = _children[i].Match(ordered[i], $"{path}/{i}");
                if (childFailure != null)
                {
                    return childFailure;
                }
            }

            return null;
        }
    }
}
