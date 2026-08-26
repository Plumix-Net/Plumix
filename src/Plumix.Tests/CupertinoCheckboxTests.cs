using Avalonia;
using Avalonia.Media;
using Plumix.Gestures;
using Plumix;
using Plumix.Cupertino;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/checkbox_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoCheckboxTests
{
    private static readonly Size ViewSize = new(240.0, 120.0);

    [Fact]
    public void Constructor_ExposesDefaultsAndSourceAssertion()
    {
        var checkbox = new CupertinoCheckbox(value: false, onChanged: _ => { });

        Assert.Equal(14.0, CupertinoCheckbox.Width);
        Assert.False(checkbox.Tristate);
        Assert.False(checkbox.Autofocus);
        Assert.Null(checkbox.MouseCursor);
        Assert.Null(checkbox.FillColor);
        Assert.Null(checkbox.Side);
        Assert.Null(checkbox.Shape);
        Assert.Null(checkbox.TapTargetSize);

        Assert.Throws<ArgumentException>(() => new CupertinoCheckbox(value: null, onChanged: _ => { }));
        _ = new CupertinoCheckbox(value: null, onChanged: _ => { }, tristate: true);
    }

    [Fact]
    public void Semantics_ExposeCheckedEnabledMixedAndTap()
    {
        using var enabled = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: true, onChanged: _ => { })));
        SemanticsNode enabledRoot = Assert.IsType<SemanticsNode>(enabled.PumpAndGetSemantics(ViewSize));
        SemanticsNode enabledNode = Assert.IsType<SemanticsNode>(FindSemantics(
            enabledRoot,
            node => node.Flags.HasFlag(SemanticsFlags.HasCheckedState)));
        Assert.True(enabledNode.Flags.HasFlag(SemanticsFlags.IsChecked));
        Assert.True(enabledNode.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.NotNull(FindSemantics(enabledRoot, node => node.Actions.HasFlag(SemanticsActions.Tap)));

        using var disabled = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: false, onChanged: null)));
        SemanticsNode disabledRoot = Assert.IsType<SemanticsNode>(disabled.PumpAndGetSemantics(ViewSize));
        SemanticsNode disabledNode = Assert.IsType<SemanticsNode>(FindSemantics(
            disabledRoot,
            node => node.Flags.HasFlag(SemanticsFlags.HasCheckedState)));
        Assert.False(disabledNode.Flags.HasFlag(SemanticsFlags.IsChecked));
        Assert.False(disabledNode.Flags.HasFlag(SemanticsFlags.IsEnabled));

        using var mixed = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: null, onChanged: _ => { }, tristate: true)));
        SemanticsNode mixedRoot = Assert.IsType<SemanticsNode>(mixed.PumpAndGetSemantics(ViewSize));
        SemanticsNode mixedNode = Assert.IsType<SemanticsNode>(FindSemantics(
            mixedRoot,
            node => node.Flags.HasFlag(SemanticsFlags.HasCheckedState)));
        Assert.True(mixedNode.Flags.HasFlag(SemanticsFlags.IsCheckStateMixed));
        Assert.False(mixedNode.Flags.HasFlag(SemanticsFlags.IsChecked));
    }

    [Fact]
    public void Semantics_CanConfigureASemanticLabel()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: false, onChanged: _ => { }, semanticLabel: "checkbox")));
        SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));

        Assert.NotNull(FindSemantics(root, node => node.Label == "checkbox"));
    }

    [Fact]
    public void Tap_TogglesValue_AndDisabledCheckboxIgnoresInput()
    {
        bool? reported = null;
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: false, onChanged: value => reported = value)));

        GestureDetector detector = Assert.Single(
            harness.FindWidgets<GestureDetector>(),
            candidate => candidate.OnTap is not null);
        detector.OnTap!();
        Assert.Equal(true, reported);

        using var disabled = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: false, onChanged: null)));
        Assert.DoesNotContain(
            disabled.FindWidgets<GestureDetector>(),
            candidate => candidate.OnTap is not null);
        Assert.Contains(
            disabled.FindWidgets<FocusableActionDetector>(),
            candidate => !candidate.Enabled);
    }

    [Fact]
    public void Tristate_CyclesFalseToTrueToNullOnTap()
    {
        bool? value = null;
        bool? reported = null;

        Widget Build() => Wrap(new CupertinoCheckbox(
            value: value,
            tristate: true,
            onChanged: next => reported = next));

        using var harness = new CupertinoThemeTestHarness(Build());
        foreach (bool? expected in new bool?[] { false, true, null, false })
        {
            GestureDetector detector = Assert.Single(
                harness.FindWidgets<GestureDetector>(),
                candidate => candidate.OnTap is not null);
            detector.OnTap!();
            Assert.Equal(expected, reported);
            value = reported;
            harness.PumpWidget(Build());
        }
    }

    [Fact]
    public void Keyboard_SpaceAndEnterActivateTheCheckbox()
    {
        bool? reported = null;
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: true, onChanged: value => reported = value)));

        FocusableActionDetector detector = Assert.Single(harness.FindWidgets<FocusableActionDetector>());
        Assert.NotNull(detector.Shortcuts);
        Assert.Contains(new SingleActivator(LogicalKeyboardKey.Space), detector.Shortcuts!.Keys);
        // On web, checkboxes don't respond to the Enter key; the test host is not a browser.
        Assert.Contains(new SingleActivator(LogicalKeyboardKey.Enter), detector.Shortcuts!.Keys);

        var activate = Assert.IsAssignableFrom<FlutterAction<ActivateIntent>>(
            detector.Actions![typeof(ActivateIntent)]);
        activate.Invoke(new ActivateIntent());
        Assert.Equal(false, reported);
    }

    [Fact]
    public void TapTargetSize_DefaultsByPlatform_AndCanBeOverridden()
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        try
        {
            PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
            using var mobile = new CupertinoThemeTestHarness(Wrap(
                new CupertinoCheckbox(value: false, onChanged: _ => { })));
            Assert.Equal(
                new Size(44.0, 44.0),
                Assert.Single(mobile.FindWidgets<CustomPaint>()).Size);

            PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
            using var desktop = new CupertinoThemeTestHarness(Wrap(
                new CupertinoCheckbox(value: false, onChanged: _ => { })));
            Assert.Equal(
                new Size(14.0, 14.0),
                Assert.Single(desktop.FindWidgets<CustomPaint>()).Size);

            using var custom = new CupertinoThemeTestHarness(Wrap(
                new CupertinoCheckbox(
                    value: false,
                    onChanged: _ => { },
                    tapTargetSize: new Size(20.0, 20.0))));
            Assert.Equal(
                new Size(20.0, 20.0),
                Assert.Single(custom.FindWidgets<CustomPaint>()).Size);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void ShapeAndSide_ReachThePainter()
    {
        var shape = new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(5.0));
        var side = new BorderSide(Color.FromUInt32(0xFFF44336), 4.0);
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(
                value: false,
                onChanged: _ => { },
                shape: shape,
                side: side)));

        CupertinoCheckboxPainter painter = Painter(harness);
        Assert.Same(shape, painter.Shape);
        Assert.Equal(side, painter.Side);
    }

    [Fact]
    public void PlainSide_OnlyRendersWhenUnselected()
    {
        var side = new BorderSide(Color.FromUInt32(0xFFF44336), 4.0);
        using var selected = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: true, onChanged: _ => { }, side: side)));

        // Selected: the plain side falls back to the default (transparent, zero width) side.
        CupertinoCheckboxPainter painter = Painter(selected);
        Assert.Equal(0.0, painter.Side.Width);
        Assert.Equal(CupertinoColors.Transparent, painter.Side.Color);
    }

    [Fact]
    public void StatefulSide_ResolvesInTheSelectedState()
    {
        var selectedSide = new BorderSide(Colors.Purple, 3.0);
        var unselectedSide = new BorderSide(Colors.Gray, 1.0);
        WidgetStateBorderSide side = WidgetStateBorderSide.ResolveWith(states =>
            states.Contains(WidgetState.Selected) ? selectedSide : unselectedSide);

        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: true, onChanged: _ => { }, side: side)));

        Assert.Equal(selectedSide, Painter(harness).Side);
    }

    [Fact]
    public void MouseCursor_DefaultsToBasicOffWeb_AndClickOnWeb()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: false, onChanged: _ => { })));
        Assert.Equal(
            SystemMouseCursors.Basic,
            Assert.Single(harness.FindWidgets<FocusableActionDetector>()).MouseCursor);

        bool? previous = PlatformDefaults.DebugIsWebOverride;
        try
        {
            PlatformDefaults.DebugIsWebOverride = true;
            using var web = new CupertinoThemeTestHarness(Wrap(
                new CupertinoCheckbox(value: false, onChanged: _ => { })));
            Assert.Equal(
                SystemMouseCursors.Click,
                Assert.Single(web.FindWidgets<FocusableActionDetector>()).MouseCursor);

            using var webDisabled = new CupertinoThemeTestHarness(Wrap(
                new CupertinoCheckbox(value: false, onChanged: null)));
            Assert.Equal(
                SystemMouseCursors.Basic,
                Assert.Single(webDisabled.FindWidgets<FocusableActionDetector>()).MouseCursor);
        }
        finally
        {
            PlatformDefaults.DebugIsWebOverride = previous;
        }

        using var custom = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: false, onChanged: _ => { }, mouseCursor: SystemMouseCursors.Grab)));
        Assert.Equal(
            SystemMouseCursors.Grab,
            Assert.Single(custom.FindWidgets<FocusableActionDetector>()).MouseCursor);
    }

    [Fact]
    public void WidgetStateMouseCursor_ResolvesSelectedAndDisabledStates()
    {
        WidgetStateMouseCursor cursor = WidgetStateMouseCursor.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Disabled))
            {
                return SystemMouseCursors.Forbidden;
            }
            if (states.Contains(WidgetState.Focused))
            {
                return SystemMouseCursors.Grab;
            }
            if (states.Contains(WidgetState.Selected))
            {
                return SystemMouseCursors.Click;
            }
            return SystemMouseCursors.Basic;
        });

        using var selected = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: true, onChanged: _ => { }, mouseCursor: cursor)));
        Assert.Equal(
            SystemMouseCursors.Click,
            Assert.Single(selected.FindWidgets<FocusableActionDetector>()).MouseCursor);

        using var unselected = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: false, onChanged: _ => { }, mouseCursor: cursor)));
        Assert.Equal(
            SystemMouseCursors.Basic,
            Assert.Single(unselected.FindWidgets<FocusableActionDetector>()).MouseCursor);

        using var disabled = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: true, onChanged: null, mouseCursor: cursor)));
        Assert.Equal(
            SystemMouseCursors.Forbidden,
            Assert.Single(disabled.FindWidgets<FocusableActionDetector>()).MouseCursor);
    }

    [Fact]
    public void FillColor_ResolvesEnabledAndDisabled_AndBeatsActiveAndInactiveColors()
    {
        Color enabledFill = Color.FromUInt32(0xFF000001);
        Color disabledFill = Color.FromUInt32(0xFF000002);
        WidgetStateProperty<Color?> fillColor = WidgetStateProperty<Color?>.ResolveWith(states =>
            states.Contains(WidgetState.Disabled) ? disabledFill : enabledFill);

        using var enabled = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(
                value: true,
                onChanged: _ => { },
                activeColor: Color.FromUInt32(0xFF000003),
                inactiveColor: Color.FromUInt32(0xFF000004),
                fillColor: fillColor)));
        Assert.Equal(enabledFill, Painter(enabled).ActiveColor);

        using var disabled = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: true, onChanged: null, fillColor: fillColor)));
        Assert.Equal(disabledFill, Painter(disabled).ActiveColor);
    }

    [Fact]
    public void FillColor_ResolvesInTheHoveredState()
    {
        Color hoveredFill = Color.FromUInt32(0xFF000001);
        Color restingFill = Color.FromUInt32(0xFF000005);
        WidgetStateProperty<Color?> fillColor = WidgetStateProperty<Color?>.ResolveWith(states =>
            states.Contains(WidgetState.Hovered) ? hoveredFill : restingFill);

        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: true, onChanged: _ => { }, fillColor: fillColor)));
        FocusableActionDetector detector = Assert.Single(harness.FindWidgets<FocusableActionDetector>());

        detector.OnShowHoverHighlight!(true);
        harness.Pump(ViewSize);

        CupertinoCheckboxPainter painter = Painter(harness);
        Assert.True(painter.IsHovered);
        Assert.Equal(hoveredFill, painter.ActiveColor);
    }

    [Fact]
    public void DefaultColors_InLightAndDarkMode()
    {
        using var light = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: true, onChanged: _ => { })));
        CupertinoCheckboxPainter lightPainter = Painter(light);
        Assert.Equal(Color.FromUInt32(0xFF007AFF), lightPainter.ActiveColor);
        Assert.Equal(CupertinoColors.White, lightPainter.CheckColor);
        Assert.Equal(CupertinoColors.White, lightPainter.InactiveColor);
        Assert.True(lightPainter.IsActive);

        using var dark = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: true, onChanged: _ => { }),
            themeBrightness: PlatformBrightness.Dark));
        CupertinoCheckboxPainter darkPainter = Painter(dark);
        Assert.Equal(Color.FromUInt32(0xFF3264D7), darkPainter.ActiveColor);
        Assert.Equal(Color.FromUInt32(0xFFDEE8F8), darkPainter.CheckColor);
        Assert.Equal(PlatformBrightness.Dark, darkPainter.Brightness);
        // The unselected fill stays white; dark mode replaces it with the gradient at paint time.
        Assert.Equal(CupertinoColors.White, darkPainter.InactiveColor);
    }

    [Fact]
    public void DisabledDefaults_UseTranslucentFillCheckAndBorder()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: true, onChanged: null)));

        CupertinoCheckboxPainter painter = Painter(harness);
        Assert.False(painter.IsActive);
        Assert.Equal(Color.FromArgb(128, 255, 255, 255), painter.ActiveColor);
        Assert.Equal(Color.FromArgb(64, 0, 0, 0), painter.CheckColor);
        Assert.Equal(Color.FromArgb(13, 0, 0, 0), painter.Side.Color);
        Assert.Equal(1.0, painter.Side.Width);
    }

    [Fact]
    public void DefaultSide_IsGreyWhenUnselected_AndTransparentWhenSelected()
    {
        using var unselected = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: false, onChanged: _ => { })));
        CupertinoCheckboxPainter unselectedPainter = Painter(unselected);
        Assert.Equal(Color.FromArgb(255, 209, 209, 214), unselectedPainter.Side.Color);
        Assert.Equal(1.0, unselectedPainter.Side.Width);

        using var selected = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: true, onChanged: _ => { })));
        CupertinoCheckboxPainter selectedPainter = Painter(selected);
        Assert.Equal(CupertinoColors.Transparent, selectedPainter.Side.Color);
        Assert.Equal(0.0, selectedPainter.Side.Width);
    }

    [Fact]
    public void Focus_UsesHslFocusColorFormula_AndHonorsCustomFocusColor()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var node = new FocusNode();
            using var harness = new CupertinoThemeTestHarness(Wrap(
                new CupertinoCheckbox(value: true, onChanged: _ => { }, focusNode: node)));

            Scheduler.PumpFrameForTests();
            Assert.True(node.RequestFocus());
            harness.Pump(ViewSize);

            CupertinoCheckboxPainter painter = Painter(harness);
            Assert.True(painter.IsFocused);
            Color expected = HSLColor
                .FromColor(CupertinoCheckboxPainter.WithOpacity(
                    Color.FromUInt32(0xFF007AFF),
                    CupertinoConstants.CupertinoFocusColorOpacity))
                .WithLightness(CupertinoConstants.CupertinoFocusColorBrightness)
                .WithSaturation(CupertinoConstants.CupertinoFocusColorSaturation)
                .ToColor();
            Assert.Equal(expected, painter.EffectiveFocusColor);

            var custom = new FocusNode();
            Color testFocusColor = Color.FromUInt32(0xFFAABBCC);
            using var customHarness = new CupertinoThemeTestHarness(Wrap(
                new CupertinoCheckbox(
                    value: true,
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
    public void Pressed_SetsDownPosition_AndDefaultThemeBrightnessIsNull()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoCheckbox(value: false, onChanged: _ => { })));

        GestureDetector detector = Assert.Single(
            harness.FindWidgets<GestureDetector>(),
            candidate => candidate.OnTapDown is not null);
        detector.OnTapDown!(new TapDownDetails(
            globalPosition: new Point(7.0, 7.0),
            kind: PointerDeviceKind.Touch));
        harness.Pump(ViewSize);

        CupertinoCheckboxPainter painter = Painter(harness);
        Assert.NotNull(painter.DownPosition);
        // `CupertinoThemeData.brightness` defaults to null, so the pressed overlay uses the white
        // (non-light) branch even in a light app — asserted by Flutter's own pressed-state test.
        Assert.Null(painter.Brightness);
    }

    [Fact]
    public void ZeroArea_DoesNotCrashAndPaintsNothing()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new SizedBox(
                width: 0.0,
                height: 0.0,
                child: new CupertinoCheckbox(value: true, onChanged: _ => { })),
            center: false));

        harness.Pump(ViewSize);

        RenderCustomPaint render = Assert.IsType<RenderCustomPaint>(FindRender(harness.RenderView));
        Assert.Equal(default, render.Size);
    }

    private static CupertinoCheckboxPainter Painter(CupertinoThemeTestHarness harness)
    {
        return Assert.IsType<CupertinoCheckboxPainter>(
            Assert.Single(harness.FindWidgets<CustomPaint>()).Painter);
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
}
