using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Cupertino;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/radio_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoRadioTests
{
    private static readonly Size ViewSize = new(240.0, 120.0);
    private static readonly Size CupertinoRadioSize = new(18.0, 18.0);

    [Fact]
    public void Constructor_ExposesDefaults()
    {
        var radio = new CupertinoRadio<string>(value: "a", groupValue: "b", onChanged: _ => { });

        Assert.Equal("a", radio.Value);
        Assert.False(radio.Toggleable);
        Assert.False(radio.UseCheckmarkStyle);
        Assert.False(radio.Autofocus);
        Assert.Null(radio.MouseCursor);
        Assert.Null(radio.ActiveColor);
        Assert.Null(radio.InactiveColor);
        Assert.Null(radio.FillColor);
        Assert.Null(radio.FocusColor);
        Assert.Null(radio.FocusNode);
        Assert.Null(radio.Enabled);
        Assert.Null(radio.GroupRegistry);
    }

    [Fact]
    public void Tap_ReportsValueOnceUnselected_AndIsInertWhenSelectedOrWithoutCallback()
    {
        var log = new List<string?>();
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "b", onChanged: log.Add)));

        Tap(harness);
        Assert.Equal(["a"], log);
        log.Clear();

        harness.PumpWidget(Wrap(new CupertinoRadio<string>(
            value: "a",
            groupValue: "a",
            onChanged: log.Add,
            activeColor: CupertinoColors.SystemGreen.Value)));
        Tap(harness);
        Assert.Empty(log);

        // No `onChanged` and no registry: the radio is disabled and has no tap handler at all.
        harness.PumpWidget(Wrap(new CupertinoRadio<string>(value: "a", groupValue: "b")));
        Assert.DoesNotContain(
            harness.FindWidgets<GestureDetector>(),
            candidate => candidate.OnTap is not null);
        Assert.Empty(log);
    }

    [Fact]
    public void Enabled_False_SuppressesTapEvenWithOnChanged()
    {
        var log = new List<string?>();
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "b", onChanged: log.Add, enabled: false)));

        Assert.DoesNotContain(
            harness.FindWidgets<GestureDetector>(),
            candidate => candidate.OnTap is not null);
        Assert.Empty(log);
        Assert.Contains(harness.FindWidgets<FocusableActionDetector>(), candidate => !candidate.Enabled);
    }

    [Fact]
    public void Enabled_True_WithoutCallbackOrRegistry_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var harness = new CupertinoThemeTestHarness(Wrap(
                new CupertinoRadio<string>(value: "a", groupValue: "b", enabled: true)));
        });
    }

    [Fact]
    public void Toggleable_ReportsNullWhenTheSelectedRadioIsTappedAgain()
    {
        var log = new List<string?>();
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "b", onChanged: log.Add, toggleable: true)));

        Tap(harness);
        Assert.Equal(["a"], log);
        log.Clear();

        harness.PumpWidget(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "a", onChanged: log.Add, toggleable: true)));
        Tap(harness);
        Assert.Equal([null], log);
        log.Clear();

        harness.PumpWidget(Wrap(
            new CupertinoRadio<string>(value: "a", onChanged: log.Add, toggleable: true)));
        Tap(harness);
        Assert.Equal(["a"], log);
    }

    [Fact]
    public void RadioGroupAncestor_DrivesSelectionAndEnablesTheRadio()
    {
        string? changed = null;
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new RadioGroup<string>(
                groupValue: "a",
                onChanged: value => changed = value,
                child: new CupertinoRadio<string>(value: "b"))));

        Assert.False(Painter(harness).Value);
        Tap(harness);
        Assert.Equal("b", changed);
    }

    [Fact]
    public void Semantics_ExposeMutuallyExclusiveCheckedAndEnabledState()
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        try
        {
            PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
            using var selected = new CupertinoThemeTestHarness(Wrap(
                new CupertinoRadio<string>(value: "a", groupValue: "a", onChanged: _ => { })));
            SemanticsNode selectedRoot = Assert.IsType<SemanticsNode>(selected.PumpAndGetSemantics(ViewSize));
            SemanticsNode node = Assert.IsType<SemanticsNode>(FindSemantics(
                selectedRoot,
                candidate => candidate.Flags.HasFlag(SemanticsFlags.IsInMutuallyExclusiveGroup)));
            Assert.True(node.Flags.HasFlag(SemanticsFlags.HasCheckedState));
            Assert.True(node.Flags.HasFlag(SemanticsFlags.IsChecked));
            Assert.True(node.Flags.HasFlag(SemanticsFlags.IsEnabled));
            // Apple platforms additionally vocalize the selection through the selected flag.
            Assert.True(node.Flags.HasFlag(SemanticsFlags.HasSelectedState));
            Assert.True(node.Flags.HasFlag(SemanticsFlags.IsSelected));
            Assert.NotNull(FindSemantics(selectedRoot, candidate => candidate.Actions.HasFlag(SemanticsActions.Tap)));

            using var disabled = new CupertinoThemeTestHarness(Wrap(
                new CupertinoRadio<string>(value: "a", groupValue: "b")));
            SemanticsNode disabledRoot = Assert.IsType<SemanticsNode>(disabled.PumpAndGetSemantics(ViewSize));
            SemanticsNode disabledNode = Assert.IsType<SemanticsNode>(FindSemantics(
                disabledRoot,
                candidate => candidate.Flags.HasFlag(SemanticsFlags.IsInMutuallyExclusiveGroup)));
            Assert.False(disabledNode.Flags.HasFlag(SemanticsFlags.IsChecked));
            Assert.False(disabledNode.Flags.HasFlag(SemanticsFlags.IsEnabled));
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void Semantics_UnselectedRadioIsVocalizedThroughAHintOnApplePlatforms()
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        try
        {
            PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
            using var unselected = new CupertinoThemeTestHarness(WrapLocalized(
                new CupertinoRadio<string>(value: "b", groupValue: "a", onChanged: _ => { })));
            SemanticsNode root = Assert.IsType<SemanticsNode>(unselected.PumpAndGetSemantics(ViewSize));
            Assert.NotNull(FindSemantics(root, candidate => candidate.Hint == "Unselected"));

            using var selected = new CupertinoThemeTestHarness(WrapLocalized(
                new CupertinoRadio<string>(value: "a", groupValue: "a", onChanged: _ => { })));
            SemanticsNode selectedRoot = Assert.IsType<SemanticsNode>(selected.PumpAndGetSemantics(ViewSize));
            Assert.Null(FindSemantics(selectedRoot, candidate => !string.IsNullOrEmpty(candidate.Hint)));

            PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
            using var android = new CupertinoThemeTestHarness(WrapLocalized(
                new CupertinoRadio<string>(value: "b", groupValue: "a", onChanged: _ => { })));
            SemanticsNode androidRoot = Assert.IsType<SemanticsNode>(android.PumpAndGetSemantics(ViewSize));
            Assert.Null(FindSemantics(androidRoot, candidate => !string.IsNullOrEmpty(candidate.Hint)));
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void Keyboard_SpaceAndEnterActivateTheRadio()
    {
        var log = new List<string?>();
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "b", onChanged: log.Add)));

        FocusableActionDetector detector = Assert.Single(harness.FindWidgets<FocusableActionDetector>());
        Assert.NotNull(detector.Shortcuts);
        Assert.Contains(new SingleActivator(LogicalKeyboardKey.Space), detector.Shortcuts!.Keys);
        // On web, radios don't respond to the Enter key; the test host is not a browser.
        Assert.Contains(new SingleActivator(LogicalKeyboardKey.Enter), detector.Shortcuts!.Keys);

        var activate = Assert.IsAssignableFrom<FlutterAction<ActivateIntent>>(
            detector.Actions![typeof(ActivateIntent)]);
        activate.Invoke(new ActivateIntent());
        Assert.Equal(["a"], log);
    }

    [Fact]
    public void Size_IsTheFixedCupertinoRadioSquare()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "a", onChanged: _ => { })));

        Assert.Equal(new Size(18.0, 18.0), Assert.Single(harness.FindWidgets<CustomPaint>()).Size);
    }

    [Fact]
    public void UseCheckmarkStyle_ReachesThePainterOnlyWhenSelected()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "a", onChanged: _ => { })));
        Assert.False(Painter(harness).CheckmarkStyle);

        harness.PumpWidget(Wrap(new CupertinoRadio<string>(
            value: "a",
            groupValue: "b",
            useCheckmarkStyle: true,
            onChanged: _ => { })));
        harness.Pump(ViewSize);
        CupertinoRadioPainter unselected = Painter(harness);
        Assert.True(unselected.CheckmarkStyle);
        Assert.False(unselected.Value);
        // The checkmark style suppresses the circle entirely; an off radio paints nothing at all.
        Assert.True(PaintIsEmpty(unselected, CupertinoRadioSize));

        harness.PumpWidget(Wrap(new CupertinoRadio<string>(
            value: "a",
            groupValue: "a",
            useCheckmarkStyle: true,
            onChanged: _ => { })));
        harness.Pump(ViewSize);
        CupertinoRadioPainter selected = Painter(harness);
        Assert.True(selected.CheckmarkStyle);
        Assert.True(selected.Value);
    }

    [Fact]
    public void DarkMode_PaintsAnOpacityGradientInsteadOfAFlatCircle()
    {
        using var dark = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "b", onChanged: _ => { }),
            themeBrightness: PlatformBrightness.Dark));
        Assert.Equal(PlatformBrightness.Dark, Painter(dark).Brightness);
        Assert.False(PaintIsEmpty(Painter(dark), CupertinoRadioSize));

        using var light = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "b", onChanged: _ => { }),
            themeBrightness: PlatformBrightness.Light));
        Assert.False(PaintIsEmpty(Painter(light), CupertinoRadioSize));
    }

    [Fact]
    public void DefaultColors_InLightMode()
    {
        using var selected = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "a", onChanged: _ => { })));
        CupertinoRadioPainter selectedPainter = Painter(selected);
        Assert.Equal(Color.FromUInt32(0xFF007AFF), selectedPainter.ActiveColor);
        Assert.Equal(CupertinoColors.White, selectedPainter.FillColor);
        Assert.Equal(CupertinoColors.White, selectedPainter.InactiveColor);
        // Selected and enabled: the border is dropped.
        Assert.Equal(CupertinoColors.Transparent, selectedPainter.BorderColor);
        Assert.True(selectedPainter.IsActive);

        using var unselected = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "b", onChanged: _ => { })));
        CupertinoRadioPainter unselectedPainter = Painter(unselected);
        Assert.Equal(CupertinoColors.White, unselectedPainter.InactiveColor);
        Assert.Equal(Color.FromArgb(255, 209, 209, 214), unselectedPainter.BorderColor);
    }

    [Fact]
    public void DefaultColors_InDarkMode()
    {
        using var selected = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "a", onChanged: _ => { }),
            themeBrightness: PlatformBrightness.Dark));
        CupertinoRadioPainter selectedPainter = Painter(selected);
        Assert.Equal(Color.FromArgb(255, 50, 100, 215), selectedPainter.ActiveColor);
        Assert.Equal(Color.FromArgb(255, 222, 232, 248), selectedPainter.FillColor);
        Assert.Equal(PlatformBrightness.Dark, selectedPainter.Brightness);

        using var unselected = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "b", onChanged: _ => { }),
            themeBrightness: PlatformBrightness.Dark));
        Assert.Equal(Color.FromArgb(64, 0, 0, 0), Painter(unselected).BorderColor);
    }

    [Fact]
    public void DisabledDefaults_UseTranslucentOuterInnerAndBorderColors()
    {
        using var light = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "a")));
        CupertinoRadioPainter lightPainter = Painter(light);
        Assert.False(lightPainter.IsActive);
        Assert.Equal(Color.FromArgb(128, 255, 255, 255), lightPainter.ActiveColor);
        Assert.Equal(Color.FromArgb(128, 255, 255, 255), lightPainter.InactiveColor);
        Assert.Equal(Color.FromArgb(64, 0, 0, 0), lightPainter.FillColor);
        Assert.Equal(Color.FromArgb(64, 0, 0, 0), lightPainter.BorderColor);

        using var dark = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "a"),
            themeBrightness: PlatformBrightness.Dark));
        Assert.Equal(Color.FromArgb(64, 255, 255, 255), Painter(dark).FillColor);
        // The disabled border stays black in both brightnesses.
        Assert.Equal(Color.FromArgb(64, 0, 0, 0), Painter(dark).BorderColor);
    }

    [Fact]
    public void ActiveInactiveAndFillColors_OverrideTheDefaults()
    {
        Color activeColor = Color.FromUInt32(0x0000000A);
        Color fillColor = Color.FromUInt32(0x0000000B);
        Color inactiveColor = Color.FromUInt32(0x0000000C);

        using var unselected = new CupertinoThemeTestHarness(Wrap(new CupertinoRadio<string>(
            value: "a",
            groupValue: "b",
            onChanged: _ => { },
            activeColor: activeColor,
            inactiveColor: inactiveColor,
            fillColor: fillColor)));
        CupertinoRadioPainter unselectedPainter = Painter(unselected);
        Assert.Equal(inactiveColor, unselectedPainter.InactiveColor);
        Assert.Equal(activeColor, unselectedPainter.ActiveColor);
        Assert.Equal(Color.FromArgb(255, 209, 209, 214), unselectedPainter.BorderColor);
        // `fillColor` only paints the inner dot, which an unselected radio does not draw.
        Assert.Equal(CupertinoColors.White, unselectedPainter.FillColor);

        using var selected = new CupertinoThemeTestHarness(Wrap(new CupertinoRadio<string>(
            value: "a",
            groupValue: "a",
            onChanged: _ => { },
            activeColor: activeColor,
            inactiveColor: inactiveColor,
            fillColor: fillColor)));
        CupertinoRadioPainter selectedPainter = Painter(selected);
        Assert.Equal(activeColor, selectedPainter.ActiveColor);
        Assert.Equal(fillColor, selectedPainter.FillColor);
    }

    [Fact]
    public void Pressed_SetsDownPosition_AndDefaultThemeBrightnessIsNull()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "b", onChanged: _ => { })));

        GestureDetector detector = Assert.Single(
            harness.FindWidgets<GestureDetector>(),
            candidate => candidate.OnTapDown is not null);
        detector.OnTapDown!(new PointerDownEvent(
            1,
            PointerDeviceKind.Touch,
            new Point(9.0, 9.0),
            PointerButtons.Primary,
            DateTime.UtcNow));
        harness.Pump(ViewSize);

        CupertinoRadioPainter painter = Painter(harness);
        Assert.NotNull(painter.DownPosition);
        // `CupertinoThemeData.brightness` defaults to null, so the pressed overlay takes the white
        // (non-light) branch even in a light app.
        Assert.Null(painter.Brightness);
    }

    [Fact]
    public void Focus_UsesHslFocusColorFormula_AndHonorsCustomFocusColor()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var node = new FocusNode();
            using var harness = new CupertinoThemeTestHarness(Wrap(
                new CupertinoRadio<string>(value: "a", groupValue: "a", onChanged: _ => { }, focusNode: node)));

            Scheduler.PumpFrameForTests();
            Assert.True(node.RequestFocus());
            harness.Pump(ViewSize);

            CupertinoRadioPainter painter = Painter(harness);
            Assert.True(painter.Focused);
            // The focus highlight also drops the border, exactly like the selected state.
            Assert.Equal(CupertinoColors.Transparent, painter.BorderColor);
            Color expected = HSLColor
                .FromColor(CupertinoRadioPainter.WithOpacity(
                    Color.FromUInt32(0xFF007AFF),
                    CupertinoConstants.CupertinoFocusColorOpacity))
                .WithLightness(CupertinoConstants.CupertinoFocusColorBrightness)
                .WithSaturation(CupertinoConstants.CupertinoFocusColorSaturation)
                .ToColor();
            Assert.Equal(expected, painter.EffectiveFocusColor);

            var custom = new FocusNode();
            Color testFocusColor = Color.FromUInt32(0x0000000A);
            using var customHarness = new CupertinoThemeTestHarness(Wrap(
                new CupertinoRadio<string>(
                    value: "a",
                    groupValue: "a",
                    onChanged: _ => { },
                    focusColor: testFocusColor,
                    focusNode: custom)));
            Scheduler.PumpFrameForTests();
            Assert.True(custom.RequestFocus());
            customHarness.Pump(ViewSize);
            Assert.Equal(testFocusColor, Painter(customHarness).EffectiveFocusColor);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void MouseCursor_DefaultsToBasicOffWeb_AndClickOnWeb()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "a", onChanged: _ => { })));
        Assert.Equal(
            SystemMouseCursors.Basic,
            Assert.Single(harness.FindWidgets<FocusableActionDetector>()).MouseCursor);

        bool? previous = PlatformDefaults.DebugIsWebOverride;
        try
        {
            PlatformDefaults.DebugIsWebOverride = true;
            using var web = new CupertinoThemeTestHarness(Wrap(
                new CupertinoRadio<string>(value: "a", groupValue: "a", onChanged: _ => { })));
            Assert.Equal(
                SystemMouseCursors.Click,
                Assert.Single(web.FindWidgets<FocusableActionDetector>()).MouseCursor);

            using var webDisabled = new CupertinoThemeTestHarness(Wrap(
                new CupertinoRadio<string>(value: "a", groupValue: "a")));
            Assert.Equal(
                SystemMouseCursors.Basic,
                Assert.Single(webDisabled.FindWidgets<FocusableActionDetector>()).MouseCursor);
        }
        finally
        {
            PlatformDefaults.DebugIsWebOverride = previous;
        }

        using var custom = new CupertinoThemeTestHarness(Wrap(new CupertinoRadio<string>(
            value: "a",
            groupValue: "a",
            onChanged: _ => { },
            mouseCursor: SystemMouseCursors.Forbidden)));
        Assert.Equal(
            SystemMouseCursors.Forbidden,
            Assert.Single(custom.FindWidgets<FocusableActionDetector>()).MouseCursor);
    }

    [Fact]
    public void WidgetStateMouseCursor_ResolvesDisabledAndFocusedStates()
    {
        WidgetStateMouseCursor cursor = WidgetStateMouseCursor.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Disabled))
            {
                return SystemMouseCursors.Forbidden;
            }
            if (states.Contains(WidgetState.Focused))
            {
                return SystemMouseCursors.Basic;
            }
            return SystemMouseCursors.Click;
        });

        using var enabled = new CupertinoThemeTestHarness(Wrap(new CupertinoRadio<string>(
            value: "a",
            groupValue: "a",
            onChanged: _ => { },
            mouseCursor: cursor)));
        Assert.Equal(
            SystemMouseCursors.Click,
            Assert.Single(enabled.FindWidgets<FocusableActionDetector>()).MouseCursor);

        using var disabled = new CupertinoThemeTestHarness(Wrap(
            new CupertinoRadio<string>(value: "a", groupValue: "a", mouseCursor: cursor)));
        Assert.Equal(
            SystemMouseCursors.Forbidden,
            Assert.Single(disabled.FindWidgets<FocusableActionDetector>()).MouseCursor);

        FocusManager.Instance.ResetForTests();
        try
        {
            var node = new FocusNode();
            using var focused = new CupertinoThemeTestHarness(Wrap(new CupertinoRadio<string>(
                value: "a",
                groupValue: "a",
                onChanged: _ => { },
                mouseCursor: cursor,
                focusNode: node)));
            Scheduler.PumpFrameForTests();
            Assert.True(node.RequestFocus());
            focused.Pump(ViewSize);
            Assert.Equal(
                SystemMouseCursors.Basic,
                Assert.Single(focused.FindWidgets<FocusableActionDetector>()).MouseCursor);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void ZeroArea_DoesNotCrashAndPaintsNothing()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new SizedBox(
                width: 0.0,
                height: 0.0,
                child: new CupertinoRadio<string>(value: "a", groupValue: "a")),
            center: false));

        harness.Pump(ViewSize);

        RenderCustomPaint render = Assert.IsType<RenderCustomPaint>(FindRender(harness.RenderView));
        Assert.Equal(default, render.Size);
    }

    private static void Tap(CupertinoThemeTestHarness harness)
    {
        GestureDetector detector = Assert.Single(
            harness.FindWidgets<GestureDetector>(),
            candidate => candidate.OnTap is not null);
        detector.OnTap!();
    }

    private static CupertinoRadioPainter Painter(CupertinoThemeTestHarness harness)
    {
        return Assert.IsType<CupertinoRadioPainter>(
            Assert.Single(harness.FindWidgets<CustomPaint>()).Painter);
    }

    private static bool PaintIsEmpty(CupertinoRadioPainter painter, Size size)
    {
        var root = new ContainerLayer();
        painter.Paint(new PaintingContext(root), size);
        return root.Children.Count == 0
               || root.Children.All(child => child is PictureLayer { IsEmpty: true });
    }

    private static RenderObject? FindRender(RenderObject? root)
    {
        if (root is null or RenderCustomPaint)
        {
            return root;
        }

        RenderObject? result = null;
        root.VisitChildren(child => result ??= FindRender(child));
        return result;
    }

    private static SemanticsNode? FindSemantics(SemanticsNode node, Func<SemanticsNode, bool> predicate)
    {
        if (predicate(node))
        {
            return node;
        }
        foreach (SemanticsNode child in node.Children)
        {
            SemanticsNode? result = FindSemantics(child, predicate);
            if (result is not null)
            {
                return result;
            }
        }
        return null;
    }

    private static Widget Wrap(
        Widget child,
        PlatformBrightness? themeBrightness = null,
        bool center = true)
    {
        Widget content = center ? new Center(child: child) : child;
        // `CupertinoThemeData.brightness` stays null unless set, exactly as in a plain CupertinoApp.
        return new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: themeBrightness ?? PlatformBrightness.Light),
            child: new Directionality(
                TextDirection.Ltr,
                new CupertinoTheme(
                    new CupertinoThemeData(brightness: themeBrightness),
                    content)));
    }

    private static Widget WrapLocalized(Widget child)
    {
        return new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: PlatformBrightness.Light),
            child: new Directionality(
                TextDirection.Ltr,
                new Localizations(
                    locale: new Locale("en", "US"),
                    delegates: [DefaultWidgetsLocalizations.Delegate],
                    child: new CupertinoTheme(
                        new CupertinoThemeData(),
                        new Center(child: child)))));
    }
}
