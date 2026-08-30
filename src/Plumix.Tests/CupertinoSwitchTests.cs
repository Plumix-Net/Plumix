using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/switch_test.dart

public sealed class CupertinoSwitchTests
{
    private static readonly Size ViewSize = new(240.0, 120.0);

    [Fact]
    public void Constructor_ExposesDefaultsAliasesAndSourceAssertions()
    {
        var active = new WidgetStateColor(Colors.Green);
        var inactive = new WidgetStateColor(Colors.Gray);
        var value = new CupertinoSwitch(
            value: true,
            onChanged: _ => { },
            activeColor: active,
            trackColor: inactive);

        Assert.True(value.Value);
        Assert.NotNull(value.OnChanged);
#pragma warning disable CS0618
        Assert.Same(active, value.ActiveColor);
        Assert.Same(inactive, value.TrackColor);
#pragma warning restore CS0618
        Assert.Same(active, value.ActiveTrackColor);
        Assert.Same(inactive, value.InactiveTrackColor);
        Assert.False(value.Autofocus);
        Assert.Equal(DragStartBehavior.Start, value.DragStartBehavior);

        Assert.Throws<ArgumentException>(() => new CupertinoSwitch(
            value: false,
            onChanged: null,
            activeColor: Colors.Red,
            activeTrackColor: Colors.Blue));
        Assert.Throws<ArgumentException>(() => new CupertinoSwitch(
            value: false,
            onChanged: null,
            trackColor: Colors.Red,
            inactiveTrackColor: Colors.Blue));
        Assert.Throws<ArgumentException>(() => new CupertinoSwitch(
            value: false,
            onChanged: null,
            onActiveThumbImageError: (_, _) => { }));
        Assert.Throws<ArgumentException>(() => new CupertinoSwitch(
            value: false,
            onChanged: null,
            onInactiveThumbImageError: (_, _) => { }));
    }

    [Fact]
    public void Build_ComposesSourceNestingSizeAndEnabledOpacity()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSwitch(
            value: true,
            onChanged: _ => { })));

        harness.Pump(ViewSize);

        CustomPaint customPaint = Assert.Single(harness.FindWidgets<CustomPaint>());
        Assert.Equal(new Size(59.0, 39.0), customPaint.Size);
        Assert.IsType<CupertinoSwitchPainter>(customPaint.Painter);
        Assert.Equal(1.0, Assert.Single(harness.FindWidgets<Opacity>()).Value);
        Assert.Contains(harness.FindWidgets<FocusableActionDetector>(), detector => detector.Enabled);
        Assert.Contains(harness.FindWidgets<GestureDetector>(), detector => detector.ExcludeFromSemantics);
        Assert.Contains(harness.FindWidgets<Semantics>(), semantics => semantics.Toggled == true);
    }

    [Fact]
    public void DisabledSwitch_UsesHalfOpacityAndNoInteractiveTap()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSwitch(
            value: false,
            onChanged: null)));

        Assert.Equal(0.5, Assert.Single(harness.FindWidgets<Opacity>()).Value);
        Assert.DoesNotContain(harness.FindWidgets<GestureDetector>(), detector => detector.OnTap is not null);
        Assert.Contains(harness.FindWidgets<FocusableActionDetector>(), detector => !detector.Enabled);
    }

    [Fact]
    public void Defaults_ResolveForLightDarkAndAmbientThemeOptIn()
    {
        using var light = new CupertinoThemeTestHarness(Wrap(
            new CupertinoSwitch(value: true, onChanged: _ => { }),
            PlatformBrightness.Light));
        CupertinoSwitchPainter lightPainter = Painter(light);
        Assert.Equal(Color.FromUInt32(0xFF34C759), lightPainter.ActiveTrackColor);
        Assert.Equal(Color.FromUInt32(0x28787880), lightPainter.InactiveTrackColor);
        Assert.Equal(Colors.White, lightPainter.ActiveThumbColor);

        using var dark = new CupertinoThemeTestHarness(Wrap(
            new CupertinoSwitch(value: true, onChanged: _ => { }),
            PlatformBrightness.Dark));
        CupertinoSwitchPainter darkPainter = Painter(dark);
        Assert.Equal(Color.FromUInt32(0xFF30D158), darkPainter.ActiveTrackColor);
        Assert.Equal(Color.FromUInt32(0x51787880), darkPainter.InactiveTrackColor);

        Color primary = Color.FromUInt32(0xFF123456);
        using var themed = new CupertinoThemeTestHarness(Wrap(
            new CupertinoSwitch(value: true, onChanged: _ => { }, applyTheme: true),
            primaryColor: primary));
        Assert.Equal(primary, Painter(themed).ActiveTrackColor);
    }

    [Fact]
    public void DynamicColors_FollowSourceResolutionPaths()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoSwitch(
                value: false,
                onChanged: _ => { },
                activeTrackColor: CupertinoColors.SystemBlue,
                inactiveTrackColor: CupertinoColors.SystemBlue,
                onLabelColor: CupertinoColors.SystemGreen),
            PlatformBrightness.Dark,
            onOffSwitchLabels: true));
        CupertinoSwitchPainter painter = Painter(harness);

        Assert.Equal(Color.FromUInt32(0xFF007AFF), painter.ActiveTrackColor);
        Assert.Equal(Color.FromUInt32(0xFF0A84FF), painter.InactiveTrackColor);
        Assert.Equal(Color.FromUInt32(0xFF30D158), painter.OnLabelColor);
    }

    [Fact]
    public void StateProperties_ResolveSelectedDisabledOutlineAndWidth()
    {
        WidgetStateProperty<Color?> color = WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Selected)) return Colors.Blue;
            if (states.Contains(WidgetState.Disabled)) return Colors.Red;
            return Colors.Green;
        });
        WidgetStateProperty<double?> width = WidgetStateProperty<double?>.ResolveWith(states =>
            states.Contains(WidgetState.Selected) ? 4.0 : 2.0);

        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSwitch(
            value: false,
            onChanged: null,
            trackOutlineColor: color,
            trackOutlineWidth: width)));
        CupertinoSwitchPainter painter = Painter(harness);

        Assert.Equal(Colors.Blue, painter.ActiveOutlineColor);
        Assert.Equal(Colors.Red, painter.InactiveOutlineColor);
        Assert.Equal(4.0, painter.ActiveOutlineWidth);
        Assert.Equal(2.0, painter.InactiveOutlineWidth);
    }

    [Fact]
    public void SwitchLabels_UseMediaQueryPreferenceAndHighContrastColor()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoSwitch(value: false, onChanged: _ => { }),
            onOffSwitchLabels: true,
            highContrast: true));
        CupertinoSwitchPainter painter = Painter(harness);

        Assert.True(painter.ShowLabels);
        Assert.Equal(Colors.White, painter.OnLabelColor);
        Assert.Equal(Colors.White, painter.OffLabelColor);
    }

    [Fact]
    public void Tap_TogglesAndEmitsIOSLightImpactOnly()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        try
        {
            bool? changed = null;
            using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSwitch(
                value: false,
                onChanged: value => changed = value)));
            GestureDetector detector = Assert.Single(
                harness.FindWidgets<GestureDetector>(),
                candidate => candidate.OnTap is not null);

            detector.OnTap!();

            Assert.True(changed);
            MethodCall call = Assert.Single(platform.Log);
            Assert.Equal("HapticFeedback.vibrate", call.Method);
            Assert.Equal("HapticFeedbackType.lightImpact", call.Arguments);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void Focus_UsesThreePointFivePixelRingAndCallback()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var node = new FocusNode();
            var changes = new List<bool>();
            Color focus = Color.FromUInt32(0xFFABCDEF);
            using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSwitch(
                value: true,
                onChanged: _ => { },
                focusNode: node,
                onFocusChange: changes.Add,
                focusColor: focus)));

            Scheduler.PumpFrameForTests();
            node.RequestFocus();
            harness.Pump(ViewSize);
            CupertinoSwitchPainter painter = Painter(harness);

            Assert.True(painter.IsFocused);
            Assert.Equal(focus, painter.EffectiveFocusColor);
            Assert.Contains(true, changes);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Semantics_ExposeToggledEnabledAndTapAction()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSwitch(
            value: true,
            onChanged: _ => { })));

        SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
        SemanticsNode node = Assert.IsType<SemanticsNode>(FindSemantics(
            root,
            candidate => candidate.Flags.HasFlag(SemanticsFlags.HasToggledState)));

        Assert.True(node.Flags.HasFlag(SemanticsFlags.IsToggled));
        Assert.NotNull(FindSemantics(root, candidate => candidate.Actions.HasFlag(SemanticsActions.Tap)));
    }

    [Fact]
    public void TightZeroConstraints_DoNotCrashAndProduceZeroArea()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new SizedBox(
                width: 0.0,
                height: 0.0,
                child: new CupertinoSwitch(value: false, onChanged: _ => { })),
            center: false));

        harness.Pump(ViewSize);

        RenderCustomPaint render = Assert.Single(FindAll<RenderCustomPaint>(harness.RenderView));
        Assert.Equal(default, render.Size);
    }

    [Fact]
    public void ThumbPainter_ExposesExactSliderAndSwitchDefaults()
    {
        var slider = new CupertinoThumbPainter();
        CupertinoThumbPainter toggle = CupertinoThumbPainter.SwitchThumb();

        Assert.Equal(14.0, CupertinoThumbPainter.Radius);
        Assert.Equal(7.0, CupertinoThumbPainter.Extension);
        Assert.Equal(Colors.White, slider.Color);
        Assert.Equal(3, slider.Shadows.Count);
        Assert.Equal(Color.FromUInt32(0x26000000), slider.Shadows[0].Color);
        Assert.Equal(2, toggle.Shadows.Count);
        Assert.Equal(Color.FromUInt32(0x0F000000), toggle.Shadows[1].Color);
    }

    private static CupertinoSwitchPainter Painter(CupertinoThemeTestHarness harness)
    {
        return Assert.IsType<CupertinoSwitchPainter>(Assert.Single(harness.FindWidgets<CustomPaint>()).Painter);
    }

    private static Widget Wrap(
        Widget child,
        PlatformBrightness brightness = PlatformBrightness.Light,
        bool onOffSwitchLabels = false,
        bool highContrast = false,
        Color? primaryColor = null,
        bool center = true)
    {
        Widget content = center ? new Center(child: child) : child;
        return new MediaQuery(
            data: new MediaQueryData(
                PlatformBrightness: brightness,
                HighContrast: highContrast,
                OnOffSwitchLabels: onOffSwitchLabels),
            child: new Directionality(
                TextDirection.Ltr,
                new CupertinoTheme(
                    new CupertinoThemeData(
                        brightness: brightness,
                        primaryColor: primaryColor),
                    content)));
    }

    private static SemanticsNode? FindSemantics(
        SemanticsNode node,
        Func<SemanticsNode, bool> predicate)
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

    private static IReadOnlyList<T> FindAll<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }
        if (root is T match)
        {
            result.Add(match);
        }
        root.VisitChildren(child => result.AddRange(FindAll<T>(child)));
        return result;
    }
}
