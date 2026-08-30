using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/button_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoButtonTests : IDisposable
{
    private static readonly Size ViewSize = new(400.0, 300.0);

    public CupertinoButtonTests() => Scheduler.ResetForTests();

    public void Dispose() => Scheduler.ResetForTests();

    [Fact]
    public void DefaultLayout_AppliesLargePaddingAndMinimumSize()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: null)));
        harness.Pump(ViewSize);

        // 0px child + 20px * 2 horizontal padding = 40px, raised to the 44px minimum tappable area.
        Assert.Equal(new Size(44.0, 44.0), ButtonBox(harness).Size);
    }

    [Fact]
    public void SizeGrowsWithChild()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(40.0, 10.0), onPressed: null)));
        harness.Pump(ViewSize);

        // 40px child + 20px * 2 = 80px wide; 10px + 16px * 2 = 42px tall, raised to the 44px minimum.
        Assert.Equal(new Size(80.0, 44.0), ButtonBox(harness).Size);
    }

    [Fact]
    public void MinSizeParameter_AppliesToBothDimensions()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: null, minSize: 60.0)));
        harness.Pump(ViewSize);

        Assert.Equal(new Size(60.0, 60.0), ButtonBox(harness).Size);
    }

    [Fact]
    public void MinimumSizeParameter_AppliesPerAxis_AndConflictsWithMinSize()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(
                child: Box(0.0, 0.0),
                onPressed: null,
                minimumSize: new Size(60.0, 100.0))));
        harness.Pump(ViewSize);

        Assert.Equal(new Size(60.0, 100.0), ButtonBox(harness).Size);

        Assert.Throws<ArgumentException>(() => new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: null,
            minSize: 10.0,
            minimumSize: new Size(60.0, 100.0)));
    }

    [Fact]
    public void SizeStyle_SelectsPaddingAndMinimumSize()
    {
        using var small = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: null,
            sizeStyle: CupertinoButtonSize.Small)));
        small.Pump(ViewSize);
        // 12px * 2 = 24px wide, raised to 28px; 6px * 2 = 12px tall, raised to 28px.
        Assert.Equal(new Size(28.0, 28.0), ButtonBox(small).Size);

        using var medium = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: null,
            sizeStyle: CupertinoButtonSize.Medium)));
        medium.Pump(ViewSize);
        Assert.Equal(new Size(32.0, 32.0), ButtonBox(medium).Size);

        using var large = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: null)));
        large.Pump(ViewSize);
        Assert.Equal(new Size(44.0, 44.0), ButtonBox(large).Size);
    }

    [Fact]
    public void CustomPadding_ReplacesTheSizeDefault()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(10.0, 10.0),
            onPressed: null,
            padding: new EdgeInsets(100.0, 100.0, 100.0, 100.0))));
        harness.Pump(ViewSize);

        Assert.Equal(new Size(210.0, 210.0), ButtonBox(harness).Size);
    }

    [Fact]
    public void BorderRadiusAndPaddingMaps_CoverEverySizeStyle()
    {
        foreach (CupertinoButtonSize size in Enum.GetValues<CupertinoButtonSize>())
        {
            Assert.True(CupertinoConstants.CupertinoButtonPadding.ContainsKey(size));
            Assert.True(CupertinoConstants.CupertinoButtonSizeBorderRadius.ContainsKey(size));
            Assert.True(CupertinoConstants.CupertinoButtonMinSize.ContainsKey(size));
        }

        Assert.Equal(
            BorderRadius.Circular(40.0),
            CupertinoConstants.CupertinoButtonSizeBorderRadius[CupertinoButtonSize.Small]);
        Assert.Equal(
            BorderRadius.Circular(12.0),
            CupertinoConstants.CupertinoButtonSizeBorderRadius[CupertinoButtonSize.Large]);
    }

    [Fact]
    public void BorderRadius_DefaultsBySizeStyle_AndCanBeOverridden()
    {
        using var large = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => { })));
        Assert.Equal(
            BorderRadius.Circular(12.0),
            ((RoundedSuperellipseBorder)Decoration(large).Shape).BorderRadius);

        using var small = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: () => { },
            sizeStyle: CupertinoButtonSize.Small)));
        Assert.Equal(
            BorderRadius.Circular(40.0),
            ((RoundedSuperellipseBorder)Decoration(small).Shape).BorderRadius);

        using var custom = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: () => { },
            borderRadius: BorderRadius.Circular(4.0))));
        Assert.Equal(
            BorderRadius.Circular(4.0),
            ((RoundedSuperellipseBorder)Decoration(custom).Shape).BorderRadius);
    }

    [Fact]
    public void Alignment_DefaultsToCenter_AndCanBeOverridden()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => { })));
        Assert.Equal((AlignmentGeometry)Alignment.Center, ChildAlign(harness).Alignment);

        using var leading = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: () => { },
            alignment: Alignment.CenterLeft)));
        Assert.Equal((AlignmentGeometry)Alignment.CenterLeft, ChildAlign(leading).Alignment);
    }

    [Fact]
    public void Enabled_TracksOnPressedAndOnLongPress()
    {
        Assert.False(new CupertinoButton(child: Box(0.0, 0.0), onPressed: null).Enabled);
        Assert.True(new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => { }).Enabled);
        Assert.True(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: null,
            onLongPress: () => { }).Enabled);
    }

    [DebugOnlyFact]
    public void DebugFillProperties_ReportsTheDisabledFlag()
    {
        var properties = new DiagnosticPropertiesBuilder();
        new CupertinoButton(child: Box(0.0, 0.0), onPressed: null).DebugFillProperties(properties);
        Assert.Contains(properties.Properties, property => property.Name == "enabled");

        Assert.Contains(
            "disabled",
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: null).ToString());
        Assert.DoesNotContain(
            "disabled",
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => { }).ToString());
    }

    [Fact]
    public void OnLongPress_RegistersALongPressRecognizerAndFires()
    {
        bool value = false;
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: null,
            onLongPress: () => value = !value)));
        harness.Pump(ViewSize);

        LongPressGestureRecognizer recognizer = Recognizer<LongPressGestureRecognizer>(harness);
        Assert.NotNull(recognizer.OnLongPress);
        recognizer.OnLongPress!();
        Assert.True(value);

        using var without = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => { })));
        Assert.DoesNotContain(
            typeof(LongPressGestureRecognizer),
            Assert.Single(without.FindWidgets<RawGestureDetector>()).Gestures!.Keys);
    }

    [Fact]
    public void DisabledButton_LeavesEveryTapCallbackNull()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: null)));
        harness.Pump(ViewSize);

        TapGestureRecognizer recognizer = Recognizer<TapGestureRecognizer>(harness);
        Assert.Null(recognizer.OnTapDown);
        Assert.Null(recognizer.OnTapUp);
        Assert.Null(recognizer.OnTapCancel);
        Assert.Null(recognizer.OnTapMove);
        Assert.False(Assert.Single(harness.FindWidgets<FocusableActionDetector>()).Enabled);
    }

    [Fact]
    public void TapUp_InvokesOnPressed_AndRespectsTheMoveSlop()
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        try
        {
            PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
            Assert.Equal(
                CupertinoConstants.CupertinoButtonTapMoveSlop,
                CupertinoButton.TapMoveSlop());

            int taps = 0;
            using var harness = new CupertinoThemeTestHarness(Wrap(
                new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => taps++)));
            harness.Pump(ViewSize);

            RenderBox box = ButtonBox(harness);
            Rect bounds = new(box.LocalToGlobal(default), box.Size);
            TapGestureRecognizer recognizer = Recognizer<TapGestureRecognizer>(harness);

            recognizer.OnTapDown!(Down(bounds.Center));
            recognizer.OnTapUp!(Up(bounds.Center));
            Assert.Equal(1, taps);

            // Just past the bottom-right corner plus the whole slop: outside, so no tap.
            double slop = CupertinoButton.TapMoveSlop();
            recognizer.OnTapDown!(Down(bounds.Center));
            recognizer.OnTapUp!(Up(new Point(bounds.Right, bounds.Bottom + slop)));
            Assert.Equal(1, taps);

            // One pixel back inside the inflated bounds: the tap counts.
            recognizer.OnTapDown!(Down(bounds.Center));
            recognizer.OnTapUp!(Up(new Point(bounds.Right, bounds.Bottom + slop - 1.0)));
            Assert.Equal(2, taps);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void TapMoveSlop_IsZeroOnDesktopPlatforms()
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        try
        {
            foreach (TargetPlatform platform in new[]
                     {
                         TargetPlatform.IOS, TargetPlatform.Android, TargetPlatform.Fuchsia,
                     })
            {
                PlatformDefaults.DebugTargetPlatformOverride = platform;
                Assert.Equal(70.0, CupertinoButton.TapMoveSlop());
            }

            foreach (TargetPlatform platform in new[]
                     {
                         TargetPlatform.MacOS, TargetPlatform.Linux, TargetPlatform.Windows,
                     })
            {
                PlatformDefaults.DebugTargetPlatformOverride = platform;
                Assert.Equal(0.0, CupertinoButton.TapMoveSlop());
            }
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void PressAndMove_FadesBackInOutsideTheSlopAndOutAgainInside()
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        try
        {
            PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
            using var harness = new CupertinoThemeTestHarness(Wrap(
                new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => { })));
            harness.Pump(ViewSize);

            RenderBox box = ButtonBox(harness);
            Rect bounds = new(box.LocalToGlobal(default), box.Size);
            double slop = CupertinoButton.TapMoveSlop();
            TapGestureRecognizer recognizer = Recognizer<TapGestureRecognizer>(harness);
            FadeTransition transition = Assert.Single(harness.FindWidgets<FadeTransition>());

            recognizer.OnTapDown!(Down(bounds.TopLeft));
            Settle();
            Assert.Equal(0.4, transition.Opacity.Value, 3);

            recognizer.OnTapMove!(Move(bounds.TopLeft - new Point(0.0, slop - 1.0)));
            Settle();
            Assert.Equal(0.4, transition.Opacity.Value, 3);

            recognizer.OnTapMove!(Move(bounds.TopLeft - new Point(0.0, slop + 1.0)));
            Settle();
            Assert.Equal(1.0, transition.Opacity.Value, 3);

            recognizer.OnTapMove!(Move(bounds.TopLeft - new Point(0.0, slop - 1.0)));
            Settle();
            Assert.Equal(0.4, transition.Opacity.Value, 3);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void TapCancel_ReleasesThePressedOpacity()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => { })));
        harness.Pump(ViewSize);

        RenderBox box = ButtonBox(harness);
        TapGestureRecognizer recognizer = Recognizer<TapGestureRecognizer>(harness);
        FadeTransition transition = Assert.Single(harness.FindWidgets<FadeTransition>());

        recognizer.OnTapDown!(Down(box.LocalToGlobal(default)));
        Settle();
        Assert.Equal(0.4, transition.Opacity.Value, 3);

        recognizer.OnTapCancel!();
        Settle();
        Assert.Equal(1.0, transition.Opacity.Value, 3);
    }

    [Fact]
    public void PressedOpacity_DefaultsToPointFour_AndHonorsTheParameter()
    {
        using var custom = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: () => { },
            pressedOpacity: 0.5)));
        custom.Pump(ViewSize);

        TapGestureRecognizer recognizer = Recognizer<TapGestureRecognizer>(custom);
        recognizer.OnTapDown!(Down(ButtonBox(custom).LocalToGlobal(default)));
        Settle();
        Assert.Equal(0.5, Assert.Single(custom.FindWidgets<FadeTransition>()).Opacity.Value, 3);

        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: () => { },
            pressedOpacity: 1.5));
    }

    [Fact]
    public void NullPressedOpacity_KeepsTheButtonFullyOpaqueWhilePressed()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: () => { },
            pressedOpacity: null)));
        harness.Pump(ViewSize);

        TapGestureRecognizer recognizer = Recognizer<TapGestureRecognizer>(harness);
        recognizer.OnTapDown!(Down(ButtonBox(harness).LocalToGlobal(default)));
        Settle();
        Assert.Equal(1.0, Assert.Single(harness.FindWidgets<FadeTransition>()).Opacity.Value, 3);
    }

    [Fact]
    public void Semantics_MarksTheButtonAndExposesATapAction()
    {
        int taps = 0;
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => taps++)));
        SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));

        SemanticsNode button = Assert.IsType<SemanticsNode>(FindSemantics(
            root,
            node => node.Flags.HasFlag(SemanticsFlags.IsButton)));
        Assert.True(button.Actions.HasFlag(SemanticsActions.Tap));
        Assert.True(button.PerformAction(SemanticsActions.Tap));
        Assert.Equal(1, taps);
    }

    [Fact]
    public void ActivateIntent_InvokesOnPressed()
    {
        int taps = 0;
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => taps++)));

        FocusableActionDetector detector = Assert.Single(harness.FindWidgets<FocusableActionDetector>());
        var activate = Assert.IsAssignableFrom<FlutterAction<ActivateIntent>>(
            detector.Actions![typeof(ActivateIntent)]);
        activate.Invoke(new ActivateIntent());
        Assert.Equal(1, taps);
    }

    [Fact]
    public void CanSpecifyColors_AndTheDisabledColorTakesOverWhenDisabled()
    {
        Color background = Color.FromUInt32(0xFF0000FF);
        Color disabled = Color.FromUInt32(0xFF00FF00);

        using var enabled = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: () => { },
            color: background,
            disabledColor: disabled)));
        Assert.Equal(background, Decoration(enabled).Color);

        using var off = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: null,
            color: background,
            disabledColor: disabled)));
        Assert.Equal(disabled, Decoration(off).Color);
    }

    [Fact]
    public void PlainButtonWithoutAColor_HasNoBackground()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: null)));

        // Dart ignores `disabledColor` unless the button also has a `color`.
        Assert.Null(Decoration(harness).Color);
    }

    [Fact]
    public void CanSpecifyDynamicColors()
    {
        CupertinoDynamicColor background = CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xFF123456),
            Color.FromUInt32(0xFF654321));
        CupertinoDynamicColor inactive = CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xFF111111),
            Color.FromUInt32(0xFF222222));

        using var dark = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(
                child: Box(0.0, 0.0),
                onPressed: () => { },
                color: background,
                disabledColor: inactive),
            PlatformBrightness.Dark));
        Assert.Equal(Color.FromUInt32(0xFF654321), Decoration(dark).Color);

        using var light = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: null,
            color: background,
            disabledColor: inactive)));
        Assert.Equal(Color.FromUInt32(0xFF111111), Decoration(light).Color);
    }

    [Fact]
    public void PlainStyle_UsesThePrimaryColorForTextAndNoBackground()
    {
        using var light = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => { })));
        Assert.Equal(CupertinoColors.ActiveBlue.Color, TextStyleOf(light).Color);
        Assert.Null(Decoration(light).Color);

        using var dark = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => { }),
            PlatformBrightness.Dark));
        Assert.Equal(CupertinoColors.SystemBlue.DarkColor, TextStyleOf(dark).Color);
    }

    [Fact]
    public void TintedStyle_BlendsThePrimaryColorPerBrightness()
    {
        using var light = new CupertinoThemeTestHarness(Wrap(
            CupertinoButton.Tinted(child: Box(0.0, 0.0), onPressed: () => { })));
        Assert.Equal(CupertinoColors.ActiveBlue.Color, TextStyleOf(light).Color);
        Assert.Equal(
            WithOpacity(CupertinoColors.ActiveBlue.Color, 0.12),
            Decoration(light).Color);

        using var dark = new CupertinoThemeTestHarness(Wrap(
            CupertinoButton.Tinted(child: Box(0.0, 0.0), onPressed: () => { }),
            PlatformBrightness.Dark));
        Assert.Equal(CupertinoColors.SystemBlue.DarkColor, TextStyleOf(dark).Color);
        Assert.Equal(
            WithOpacity(CupertinoColors.ActiveBlue.DarkColor, 0.26),
            Decoration(dark).Color);
    }

    [Fact]
    public void FilledStyle_UsesTheContrastingForegroundAndSolidBackground()
    {
        using var light = new CupertinoThemeTestHarness(Wrap(
            CupertinoButton.Filled(child: Box(0.0, 0.0), onPressed: () => { })));
        Assert.Equal(CupertinoColors.White, TextStyleOf(light).Color);
        Assert.Equal(CupertinoColors.ActiveBlue.Color, Decoration(light).Color);

        using var dark = new CupertinoThemeTestHarness(Wrap(
            CupertinoButton.Filled(child: Box(0.0, 0.0), onPressed: () => { }),
            PlatformBrightness.Dark));
        Assert.Equal(CupertinoColors.White, TextStyleOf(dark).Color);
        Assert.Equal(CupertinoColors.SystemBlue.DarkColor, Decoration(dark).Color);

        using var custom = new CupertinoThemeTestHarness(Wrap(CupertinoButton.Filled(
            child: Box(0.0, 0.0),
            onPressed: () => { },
            color: CupertinoColors.SystemRed)));
        Assert.Equal(CupertinoColors.SystemRed.Color, Decoration(custom).Color);
    }

    [Fact]
    public void DisabledPlainButton_FallsBackToTheTertiaryLabelForeground()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: null)));

        Assert.Equal(CupertinoColors.TertiaryLabel.Color, TextStyleOf(harness).Color);
    }

    [Fact]
    public void ForegroundColor_OverridesTextAndIconColorsInEveryState()
    {
        Color foreground = Color.FromUInt32(0xFF5500FF);

        using var enabled = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: () => { },
            foregroundColor: foreground)));
        Assert.Equal(foreground, TextStyleOf(enabled).Color);
        Assert.Equal(foreground, ButtonIconTheme(enabled).Color);

        using var disabled = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: null,
            foregroundColor: foreground)));
        Assert.Equal(foreground, TextStyleOf(disabled).Color);

        using var filled = new CupertinoThemeTestHarness(Wrap(CupertinoButton.Filled(
            child: Box(0.0, 0.0),
            onPressed: () => { },
            foregroundColor: foreground)));
        Assert.Equal(foreground, TextStyleOf(filled).Color);
    }

    [Fact]
    public void IconThemeSize_TracksTheFontSize_AndFallsBackWhenItIsNull()
    {
        using var sized = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => { })));
        double fontSize = Assert.IsType<double>(TextStyleOf(sized).FontSize);
        Assert.Equal(fontSize * 1.2, ButtonIconTheme(sized).Size);

        using var unsized = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => { }),
            textTheme: new CupertinoTextThemeData(actionTextStyle: new TextStyle())));
        Assert.Null(TextStyleOf(unsized).FontSize);
        Assert.Equal(
            CupertinoConstants.CupertinoButtonDefaultIconSize,
            ButtonIconTheme(unsized).Size);
    }

    [Fact]
    public void SmallSizeStyle_UsesTheSmallActionTextStyle()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(
                child: Box(0.0, 0.0),
                onPressed: () => { },
                sizeStyle: CupertinoButtonSize.Small),
            textTheme: new CupertinoTextThemeData(
                actionTextStyle: new TextStyle(FontSize: 30.0),
                actionSmallTextStyle: new TextStyle(FontSize: 11.0))));

        Assert.Equal(11.0, TextStyleOf(harness).FontSize);
    }

    [Fact]
    public void Focus_DrawsNoBorderUntilFocused_AndUsesTheDefaultFocusColor()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => { })));
        Assert.Equal(BorderSide.None, ((RoundedSuperellipseBorder)Decoration(harness).Shape).Side);

        Assert.Single(harness.FindWidgets<FocusableActionDetector>())
            .OnShowFocusHighlight!(true);
        harness.Layout(ViewSize);

        BorderSide side = ((RoundedSuperellipseBorder)Decoration(harness).Shape).Side;
        Assert.Equal(3.5, side.Width);
        Assert.Equal(BorderSide.StrokeAlignOutside, side.StrokeAlign);
        Assert.Equal(
            HSLColor.FromColor(WithOpacity(
                    CupertinoColors.ActiveBlue.Color,
                    CupertinoConstants.CupertinoFocusColorOpacity))
                .WithLightness(CupertinoConstants.CupertinoFocusColorBrightness)
                .WithSaturation(CupertinoConstants.CupertinoFocusColorSaturation)
                .ToColor(),
            side.Color);
    }

    [Fact]
    public void Focus_UsesTheConfiguredFocusColor()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: () => { },
            focusColor: CupertinoColors.SystemGreen.Color)));

        Assert.Single(harness.FindWidgets<FocusableActionDetector>())
            .OnShowFocusHighlight!(true);
        harness.Layout(ViewSize);

        Assert.Equal(
            CupertinoColors.SystemGreen.Color,
            ((RoundedSuperellipseBorder)Decoration(harness).Shape).Side.Color);
    }

    [Fact]
    public void DisabledButton_NeverDrawsTheFocusBorder()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: null)));

        Assert.Single(harness.FindWidgets<FocusableActionDetector>())
            .OnShowFocusHighlight!(true);
        harness.Layout(ViewSize);

        Assert.Equal(BorderSide.None, ((RoundedSuperellipseBorder)Decoration(harness).Shape).Side);
    }

    [Fact]
    public void FocusNodeAutofocusAndOnFocusChange_ReachTheFocusableActionDetector()
    {
        var node = new FocusNode();
        bool? focused = null;
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: () => { },
            focusNode: node,
            autofocus: true,
            onFocusChange: value => focused = value)));

        FocusableActionDetector detector = Assert.Single(harness.FindWidgets<FocusableActionDetector>());
        Assert.Same(node, detector.FocusNode);
        Assert.True(detector.Autofocus);
        detector.OnFocusChange!(true);
        Assert.True(focused);
    }

    [Fact]
    public void MouseCursor_DefersOffWeb_AndClicksOnWebWhenEnabled()
    {
        using var enabled = new CupertinoThemeTestHarness(Wrap(
            new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => { })));
        Assert.Equal(MouseCursor.Defer, ButtonCursor(enabled));

        bool? previous = PlatformDefaults.DebugIsWebOverride;
        try
        {
            PlatformDefaults.DebugIsWebOverride = true;
            using var web = new CupertinoThemeTestHarness(Wrap(
                new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => { })));
            Assert.Equal(
                SystemMouseCursors.Click,
                ButtonCursor(web));

            using var disabled = new CupertinoThemeTestHarness(Wrap(
                new CupertinoButton(child: Box(0.0, 0.0), onPressed: null)));
            Assert.Equal(
                MouseCursor.Defer,
                ButtonCursor(disabled));
        }
        finally
        {
            PlatformDefaults.DebugIsWebOverride = previous;
        }
    }

    [Fact]
    public void MouseCursor_ResolvesTheDisabledPressedAndFocusedStates()
    {
        WidgetStateMouseCursor cursor = WidgetStateMouseCursor.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Disabled))
            {
                return SystemMouseCursors.Forbidden;
            }

            if (states.Contains(WidgetState.Pressed))
            {
                return SystemMouseCursors.Grab;
            }

            return states.Contains(WidgetState.Focused)
                ? SystemMouseCursors.Text
                : SystemMouseCursors.Basic;
        });

        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: () => { },
            mouseCursor: cursor)));
        harness.Pump(ViewSize);
        Assert.Equal(SystemMouseCursors.Basic, ButtonCursor(harness));

        Assert.Single(harness.FindWidgets<FocusableActionDetector>()).OnShowFocusHighlight!(true);
        harness.Layout(ViewSize);
        Assert.Equal(SystemMouseCursors.Text, ButtonCursor(harness));

        Recognizer<TapGestureRecognizer>(harness).OnTapDown!(Down(default));
        harness.Layout(ViewSize);
        Assert.Equal(SystemMouseCursors.Grab, ButtonCursor(harness));

        using var disabled = new CupertinoThemeTestHarness(Wrap(new CupertinoButton(
            child: Box(0.0, 0.0),
            onPressed: null,
            mouseCursor: cursor)));
        Assert.Equal(
            SystemMouseCursors.Forbidden,
            ButtonCursor(disabled));
    }

    [Fact]
    public void ZeroArea_DoesNotThrow()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new SizedBox(
                width: 0.0,
                height: 0.0,
                child: new CupertinoButton(child: Box(0.0, 0.0), onPressed: () => { }))));
        harness.Pump(ViewSize);

        Assert.Equal(default, ButtonBox(harness).Size);
    }

    private static void Settle()
    {
        // Two frames past the longest of the fade-in (180ms) and fade-out (120ms) durations.
        Scheduler.PumpFrameForTests(Scheduler.CurrentFrameTimeStamp + TimeSpan.FromMilliseconds(1));
        Scheduler.PumpFrameForTests(Scheduler.CurrentFrameTimeStamp + TimeSpan.FromMilliseconds(400));
    }

    private static TapDownDetails Down(Point position) =>
        new(globalPosition: position, kind: PointerDeviceKind.Touch);

    private static TapUpDetails Up(Point position) =>
        new(kind: PointerDeviceKind.Touch, globalPosition: position);

    private static TapMoveDetails Move(Point position) =>
        new(kind: PointerDeviceKind.Touch, globalPosition: position);

    private static Widget Box(double width, double height) =>
        new SizedBox(width: width, height: height);

    private static T Recognizer<T>(CupertinoThemeTestHarness harness) where T : GestureRecognizer
    {
        RawGestureDetector detector = Assert.Single(harness.FindWidgets<RawGestureDetector>());
        var factory = (GestureRecognizerFactory<T>)detector.Gestures![typeof(T)];
        T recognizer = factory.Constructor();
        factory.Initializer(recognizer);
        return recognizer;
    }

    /// <summary>
    /// The cursor of the button's own <see cref="MouseRegion"/>. Pre-order puts it first;
    /// <see cref="FocusableActionDetector"/> builds a second one below it.
    /// </summary>
    private static MouseCursor? ButtonCursor(CupertinoThemeTestHarness harness) =>
        harness.FindWidgets<MouseRegion>()[0].Cursor;

    private static ShapeDecoration Decoration(CupertinoThemeTestHarness harness) =>
        Assert.IsType<ShapeDecoration>(Assert.Single(harness.FindWidgets<DecoratedBox>()).Decoration);

    private static Align ChildAlign(CupertinoThemeTestHarness harness) =>
        Assert.Single(harness.FindWidgets<Align>(), candidate => candidate.WidthFactor == 1.0);

    private static TextStyle TextStyleOf(CupertinoThemeTestHarness harness) =>
        Assert.Single(harness.FindWidgets<DefaultTextStyle>()).Style;

    private static IconThemeData ButtonIconTheme(CupertinoThemeTestHarness harness) =>
        harness.FindWidgets<IconTheme>()[^1].Data;

    private static RenderBox ButtonBox(CupertinoThemeTestHarness harness) =>
        Assert.IsType<RenderConstrainedBox>(FindRender<RenderConstrainedBox>(harness.RenderView));

    private static T? FindRender<T>(RenderObject? root) where T : RenderObject
    {
        if (root is null or T)
        {
            return root as T;
        }

        T? result = null;
        root.VisitChildren(child => result ??= FindRender<T>(child));
        return result;
    }

    private static Color WithOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Clamp(
            (int)Math.Round(byte.MaxValue * Math.Clamp(opacity, 0.0, 1.0)),
            0,
            byte.MaxValue);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
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
        CupertinoTextThemeData? textTheme = null)
    {
        return new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: themeBrightness ?? PlatformBrightness.Light),
            child: new Directionality(
                TextDirection.Ltr,
                new CupertinoTheme(
                    new CupertinoThemeData(brightness: themeBrightness, textTheme: textTheme),
                    new Center(child: child))));
    }
}
