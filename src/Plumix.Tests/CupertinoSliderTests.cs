using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Cupertino;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MouseCursor = Plumix.Widgets.MouseCursor;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/slider_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoSliderTests : IDisposable
{
    private static readonly Size ViewSize = new(800.0, 500.0);
    private const double SliderWidth = 176.0;
    private const double SliderHeight = 44.0;
    private const double Unit = CupertinoThumbPainter.Radius;
    private const double TouchSlop = 18.0;

    /// <summary>The drag extent Dart divides the primary delta by: `width - 2 * (padding + radius)`.</summary>
    private const double Extent = SliderWidth - (2.0 * (8.0 + CupertinoThumbPainter.Radius));

    private static readonly Point TopLeft = new(
        (ViewSize.Width - SliderWidth) / 2.0,
        (ViewSize.Height - SliderHeight) / 2.0);

    public CupertinoSliderTests()
    {
        GestureBinding.Instance.ResetForTests();
        PlatformDefaults.DebugIsWebOverride = null;
        PlatformDefaults.DebugTargetPlatformOverride = null;
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        PlatformDefaults.DebugIsWebOverride = null;
        PlatformDefaults.DebugTargetPlatformOverride = null;
    }

    [Fact]
    public void Constructor_ExposesDartDefaultsAndAsserts()
    {
        var slider = new CupertinoSlider(value: 0.25, onChanged: _ => { });

        Assert.Equal(0.25, slider.Value);
        Assert.Equal(0.0, slider.Min);
        Assert.Equal(1.0, slider.Max);
        Assert.Null(slider.Divisions);
        Assert.Null(slider.ActiveColor);
        Assert.Equal(CupertinoColors.White, slider.ThumbColor.Value);
        Assert.Null(slider.OnChangeStart);
        Assert.Null(slider.OnChangeEnd);

        // assert(value >= min && value <= max)
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoSlider(value: 1.5, onChanged: null));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CupertinoSlider(value: 0.0, onChanged: null, min: 0.5, max: 1.0));
        // assert(divisions == null || divisions > 0)
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CupertinoSlider(value: 0.0, onChanged: null, divisions: 0));
    }

    [Fact]
    public void Layout_IsTheFixed176By44Box_AndSurvivesAZeroAreaParent()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSlider(0.5, _ => { })));
        harness.Pump(ViewSize);
        Assert.Equal(new Size(SliderWidth, SliderHeight), Render(harness).Size);

        using var zero = new CupertinoThemeTestHarness(Wrap(
            new SizedBox(width: 0.0, height: 0.0, child: new CupertinoSlider(0.0, _ => { }))));
        zero.Pump(ViewSize);
        Assert.Equal(default, Render(zero).Size);
    }

    [Fact]
    public void Tap_DoesNotMoveTheSlider_InBothDirections()
    {
        foreach (TextDirection direction in new[] { TextDirection.Ltr, TextDirection.Rtl })
        {
            double value = 0.0;
            using var harness = new CupertinoThemeTestHarness(WrapBuilder(
                (_, setState) => new CupertinoSlider(
                    value,
                    next => setState(() => value = next)),
                direction: direction));
            harness.Pump(ViewSize);

            // The centre of the slider is far away from the thumb, so `hitTestSelf` rejects it.
            Point center = TopLeft + new Vector(SliderWidth / 2.0, SliderHeight / 2.0);
            Down(harness, center);
            Up(harness, center, 16.0);
            harness.Pump(ViewSize);

            Assert.Equal(0.0, value);
        }
    }

    [Fact]
    public void Drag_MovesTheSlider_LTR()
    {
        double value = 0.0;
        double startValue = double.NaN;
        double endValue = double.NaN;
        using var harness = new CupertinoThemeTestHarness(WrapBuilder(
            (_, setState) => new CupertinoSlider(
                value,
                next => setState(() => value = next),
                onChangeStart: next => startValue = next,
                onChangeEnd: next => endValue = next)));
        harness.Pump(ViewSize);

        Assert.Equal(0.0, value);
        Drag(harness, ThumbCenter(0.0, TextDirection.Ltr), 3.0 * Unit);

        // The recognizer swallows the touch slop before it reports the first primary delta.
        double expected = ((3.0 * Unit) - TouchSlop) / Extent;
        Assert.Equal(0.0, startValue);
        Assert.Equal(expected, value, 10);
        Assert.Equal(expected, endValue, 10);
    }

    [Fact]
    public void Drag_MovesTheSlider_RTL()
    {
        double value = 0.0;
        double startValue = double.NaN;
        double endValue = double.NaN;
        using var harness = new CupertinoThemeTestHarness(WrapBuilder(
            (_, setState) => new CupertinoSlider(
                value,
                next => setState(() => value = next),
                onChangeStart: next => startValue = next,
                onChangeEnd: next => endValue = next),
            direction: TextDirection.Rtl));
        harness.Pump(ViewSize);

        Assert.Equal(0.0, value);
        Drag(harness, ThumbCenter(0.0, TextDirection.Rtl), -3.0 * Unit);

        double expected = ((3.0 * Unit) - TouchSlop) / Extent;
        Assert.Equal(0.0, startValue);
        Assert.Equal(expected, value, 10);
        Assert.Equal(expected, endValue, 10);
    }

    [Fact]
    public void OnChangeStartAndOnChangeEnd_AreCalledExactlyOncePerInteraction()
    {
        double value = 0.0;
        int starts = 0;
        int ends = 0;
        using var harness = new CupertinoThemeTestHarness(WrapBuilder(
            (_, setState) => new CupertinoSlider(
                value,
                next => setState(() => value = next),
                onChangeStart: _ => starts++,
                onChangeEnd: _ => ends++)));
        harness.Pump(ViewSize);

        Drag(harness, ThumbCenter(0.0, TextDirection.Ltr), 3.0 * Unit);

        Assert.Equal(1, starts);
        Assert.Equal(1, ends);
    }

    [Fact]
    public void Divisions_SnapTheReportedValueToTheNearestStep()
    {
        double value = 0.0;
        using var harness = new CupertinoThemeTestHarness(WrapBuilder(
            (_, setState) => new CupertinoSlider(
                value,
                next => setState(() => value = next),
                divisions: 5)));
        harness.Pump(ViewSize);

        // A drag long enough to land between the third and fourth division snaps to 0.6.
        Drag(harness, ThumbCenter(0.0, TextDirection.Ltr), TouchSlop + (0.58 * Extent));
        Assert.Equal(0.6, value, 10);
    }

    [Fact]
    public void HapticFeedback_IsEmittedAtTheEdgesOnIOS_AndDependsOnTheDragVelocity()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        double value = 0.0;
        using var harness = new CupertinoThemeTestHarness(WrapBuilder(
            (_, setState) => new CupertinoSlider(value, next => setState(() => value = next))));
        harness.Pump(ViewSize);

        // Creating the slider emits nothing.
        Assert.Empty(platform.Log);

        // Moving the slider inside its range emits nothing either.
        Drag(harness, ThumbCenter(0.0, TextDirection.Ltr), 50.0);
        Assert.True(value is > 0.0 and < 1.0);
        Assert.Empty(platform.Log);

        // Moving quickly all the way to the end: a medium impact.
        Drag(harness, ThumbCenter(value, TextDirection.Ltr), 1000.0);
        Assert.Equal(1.0, value);
        Assert.Equal(["HapticFeedback.vibrate"], platform.Methods);
        Assert.Equal("HapticFeedbackType.mediumImpact", platform.Log[0].Arguments);

        // Moving slowly all the way back to the start: a selection click.
        Drag(harness, ThumbCenter(1.0, TextDirection.Ltr), -(Extent + TouchSlop), moveMilliseconds: 1100.0);
        Assert.Equal(0.0, value);
        Assert.Equal(2, platform.Log.Count);
        Assert.Equal("HapticFeedbackType.selectionClick", platform.Log[1].Arguments);
    }

    [Fact]
    public void HapticFeedback_IsNotEmittedOnNonIOSPlatforms()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        double value = 0.0;
        using var harness = new CupertinoThemeTestHarness(WrapBuilder(
            (_, setState) => new CupertinoSlider(value, next => setState(() => value = next))));
        harness.Pump(ViewSize);

        Drag(harness, ThumbCenter(0.0, TextDirection.Ltr), 1000.0);

        Assert.Equal(1.0, value);
        Assert.Empty(platform.Log);
    }

    [Fact]
    public void Semantics_ExposeThePercentValueWithIncreaseAndDecreaseActions()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSlider(0.5, _ => { })));
        SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
        SemanticsNode node = Assert.IsType<SemanticsNode>(
            FindSemantics(root, candidate => candidate.Flags.HasFlag(SemanticsFlags.IsSlider)));

        Assert.Equal("50%", node.Value);
        Assert.Equal("60%", node.IncreasedValue);
        Assert.Equal("40%", node.DecreasedValue);
        Assert.True(node.Actions.HasFlag(SemanticsActions.Increase));
        Assert.True(node.Actions.HasFlag(SemanticsActions.Decrease));
        Assert.Equal(TextDirection.Ltr, node.TextDirection);

        // Disabling the slider keeps only the slider flag.
        harness.PumpWidget(Wrap(new CupertinoSlider(0.5, onChanged: null)));
        SemanticsNode disabledRoot = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
        SemanticsNode disabled = Assert.IsType<SemanticsNode>(
            FindSemantics(disabledRoot, candidate => candidate.Flags.HasFlag(SemanticsFlags.IsSlider)));
        Assert.Null(disabled.Value);
        Assert.False(disabled.Actions.HasFlag(SemanticsActions.Increase));
        Assert.False(disabled.Actions.HasFlag(SemanticsActions.Decrease));
    }

    [Fact]
    public void Semantics_FollowTheValueAcrossRebuilds()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSlider(0.5, _ => { })));
        SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
        Assert.Equal(
            "50%",
            Assert.IsType<SemanticsNode>(
                FindSemantics(root, candidate => candidate.Flags.HasFlag(SemanticsFlags.IsSlider))).Value);

        harness.PumpWidget(Wrap(new CupertinoSlider(0.6, _ => { })));
        SemanticsNode updatedRoot = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
        SemanticsNode updated = Assert.IsType<SemanticsNode>(
            FindSemantics(updatedRoot, candidate => candidate.Flags.HasFlag(SemanticsFlags.IsSlider)));
        Assert.Equal("60%", updated.Value);
        Assert.Equal("70%", updated.IncreasedValue);
        Assert.Equal("50%", updated.DecreasedValue);
    }

    [Fact]
    public void SemanticActions_StepByTheAdjustmentUnitOrByOneDivision()
    {
        var log = new List<double>();
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSlider(0.5, log.Add)));
        harness.Pump(ViewSize);

        RenderCupertinoSlider render = Render(harness);
        Invoke(render, SemanticsActions.Increase);
        Invoke(render, SemanticsActions.Decrease);
        Assert.Equal(2, log.Count);
        Assert.Equal(0.6, log[0], 10);
        Assert.Equal(0.4, log[1], 10);

        log.Clear();
        harness.PumpWidget(Wrap(new CupertinoSlider(0.5, log.Add, divisions: 4)));
        harness.Pump(ViewSize);
        Invoke(Render(harness), SemanticsActions.Increase);
        Assert.Equal([0.75], log);

        // A disabled slider publishes no actions at all.
        log.Clear();
        harness.PumpWidget(Wrap(new CupertinoSlider(0.5, onChanged: null)));
        harness.Pump(ViewSize);
        Assert.False(Configuration(Render(harness)).ActionHandlers.ContainsKey(SemanticsActions.Increase));
    }

    [Fact]
    public void ActiveColor_DefaultsToTheThemePrimaryColorAndFollowsBrightness()
    {
        using var light = new CupertinoThemeTestHarness(Wrap(new CupertinoSlider(0.5, _ => { })));
        light.Pump(ViewSize);
        Assert.Equal(CupertinoColors.SystemBlue.Color, Render(light).ActiveColor);

        using var dark = new CupertinoThemeTestHarness(Wrap(
            new CupertinoSlider(0.5, _ => { }),
            brightness: PlatformBrightness.Dark));
        dark.Pump(ViewSize);
        Assert.Equal(CupertinoColors.SystemBlue.DarkColor, Render(dark).ActiveColor);

        using var overridden = new CupertinoThemeTestHarness(Wrap(
            new CupertinoSlider(0.5, _ => { }, activeColor: CupertinoColors.ActiveGreen),
            brightness: PlatformBrightness.Dark));
        overridden.Pump(ViewSize);
        Assert.Equal(CupertinoColors.SystemGreen.DarkColor, Render(overridden).ActiveColor);
    }

    [Fact]
    public void ActiveColor_ResolvesEveryDynamicColorVariant()
    {
        var activeColor = new CupertinoDynamicColor(
            color: Color.FromUInt32(0x00000001),
            darkColor: Color.FromUInt32(0x00000002),
            highContrastColor: Color.FromUInt32(0x00000004),
            darkHighContrastColor: Color.FromUInt32(0x00000006),
            elevatedColor: Color.FromUInt32(0x00000003),
            darkElevatedColor: Color.FromUInt32(0x00000005),
            highContrastElevatedColor: Color.FromUInt32(0x00000007),
            darkHighContrastElevatedColor: Color.FromUInt32(0x00000008));

        (PlatformBrightness Brightness, CupertinoUserInterfaceLevelData Level, bool HighContrast, Color Expected)[]
            cases =
            [
                (PlatformBrightness.Light, CupertinoUserInterfaceLevelData.Base, false, activeColor.Color),
                (PlatformBrightness.Dark, CupertinoUserInterfaceLevelData.Base, false, activeColor.DarkColor),
                (PlatformBrightness.Dark, CupertinoUserInterfaceLevelData.Elevated, false,
                    activeColor.DarkElevatedColor),
                (PlatformBrightness.Dark, CupertinoUserInterfaceLevelData.Base, true,
                    activeColor.DarkHighContrastColor),
                (PlatformBrightness.Dark, CupertinoUserInterfaceLevelData.Elevated, true,
                    activeColor.DarkHighContrastElevatedColor),
                (PlatformBrightness.Light, CupertinoUserInterfaceLevelData.Base, true,
                    activeColor.HighContrastColor),
                (PlatformBrightness.Light, CupertinoUserInterfaceLevelData.Elevated, false,
                    activeColor.ElevatedColor),
                (PlatformBrightness.Light, CupertinoUserInterfaceLevelData.Elevated, true,
                    activeColor.HighContrastElevatedColor),
            ];

        foreach ((PlatformBrightness brightness, CupertinoUserInterfaceLevelData level, bool highContrast,
                     Color expected) in cases)
        {
            using var harness = new CupertinoThemeTestHarness(Wrap(
                new CupertinoSlider(0.5, _ => { }, activeColor: activeColor),
                brightness: brightness,
                level: level,
                highContrast: highContrast));
            harness.Pump(ViewSize);
            Assert.Equal(expected, Render(harness).ActiveColor);
        }
    }

    [Fact]
    public void TrackColor_IsSystemFillResolvedAgainstBrightness()
    {
        using var light = new CupertinoThemeTestHarness(Wrap(new CupertinoSlider(0.0, _ => { })));
        light.Pump(ViewSize);
        Assert.Equal(CupertinoColors.SystemFill.Color, Render(light).TrackColor);
        Assert.NotEqual(CupertinoColors.SystemFill.DarkColor, Render(light).TrackColor);

        using var dark = new CupertinoThemeTestHarness(Wrap(
            new CupertinoSlider(0.0, _ => { }),
            brightness: PlatformBrightness.Dark));
        dark.Pump(ViewSize);
        Assert.Equal(CupertinoColors.SystemFill.DarkColor, Render(dark).TrackColor);
    }

    [Fact]
    public void ThumbColor_DefaultsToWhiteAndCanBeOverridden()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSlider(0.0, _ => { })));
        harness.Pump(ViewSize);
        Assert.Equal(CupertinoColors.White, Render(harness).ThumbColor);

        harness.PumpWidget(Wrap(new CupertinoSlider(0.0, _ => { }, thumbColor: CupertinoColors.SystemPurple)));
        harness.Pump(ViewSize);
        Assert.Equal(CupertinoColors.SystemPurple.Color, Render(harness).ThumbColor);

        harness.PumpWidget(Wrap(new CupertinoSlider(0.0, _ => { }, thumbColor: CupertinoColors.ActiveOrange)));
        harness.Pump(ViewSize);
        Assert.Equal(CupertinoColors.SystemOrange.Color, Render(harness).ThumbColor);
    }

    [Fact]
    public void PaintedTrackSplit_JumpsWhenContinuousAndAnimatesWhenDiscrete()
    {
        using var continuous = new CupertinoThemeTestHarness(Wrap(new CupertinoSlider(0.0, _ => { })));
        continuous.Pump(ViewSize);
        Assert.Equal(0.0, Render(continuous).PositionValue);

        continuous.PumpWidget(Wrap(new CupertinoSlider(0.4, _ => { })));
        continuous.Pump(ViewSize);
        Assert.Equal(0.4, Render(continuous).PositionValue);

        using var discrete = new CupertinoThemeTestHarness(Wrap(new CupertinoSlider(0.0, _ => { }, divisions: 5)));
        discrete.Pump(ViewSize);
        RenderCupertinoSlider render = Render(discrete);

        discrete.PumpWidget(Wrap(new CupertinoSlider(0.4, _ => { }, divisions: 5)));
        discrete.Pump(ViewSize);
        // The thumb is positioned by `value`, but the track split lags behind on `_position`.
        Assert.Equal(0.4, render.Value);
        Assert.Equal(0.0, render.PositionValue);

        // `AnimateTo` scales the 500 ms transition by the covered fraction, so 0 -> 0.4 takes 200 ms.
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.1));
        Assert.True(render.PositionValue is > 0.0 and < 0.4);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.2));
        Assert.Equal(0.4, render.PositionValue, 10);
    }

    [Fact]
    public void MouseCursor_IsClickOnWebAndDeferredOtherwise_AndIsNeverMouseTracked()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSlider(0.5, _ => { })));
        harness.Pump(ViewSize);
        Assert.Equal(MouseCursor.Defer, Render(harness).Cursor);
        Assert.False(Render(harness).ValidForMouseTracker);

        PlatformDefaults.DebugIsWebOverride = true;
        using var web = new CupertinoThemeTestHarness(Wrap(new CupertinoSlider(0.5, _ => { })));
        web.Pump(ViewSize);
        Assert.Equal(SystemMouseCursors.Click, Render(web).Cursor);
    }

    [Fact]
    public void Disabled_SliderIgnoresPointerInput()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSlider(0.0, onChanged: null)));
        harness.Pump(ViewSize);

        Drag(harness, ThumbCenter(0.0, TextDirection.Ltr), 3.0 * Unit);

        Assert.Equal(0.0, Render(harness).Value);
    }

    private static void Invoke(RenderCupertinoSlider render, SemanticsActions action)
    {
        Configuration(render).ActionHandlers[action](null);
    }

    private static SemanticsConfiguration Configuration(RenderCupertinoSlider render)
    {
        var configuration = new SemanticsConfiguration();
        render.InvokeDescribeSemanticsConfiguration(configuration);
        return configuration;
    }

    private static RenderCupertinoSlider Render(CupertinoThemeTestHarness harness)
    {
        return Assert.IsType<RenderCupertinoSlider>(harness.FindRenderObject<CupertinoSlider>());
    }

    /// <summary>The global centre of the thumb for a normalized value, in the harness' layout.</summary>
    private static Point ThumbCenter(double value, TextDirection direction)
    {
        double visual = direction == TextDirection.Rtl ? 1.0 - value : value;
        double left = 8.0 + CupertinoThumbPainter.Radius;
        double right = SliderWidth - 8.0 - CupertinoThumbPainter.Radius;
        return TopLeft + new Vector(left + ((right - left) * visual), SliderHeight / 2.0);
    }

    private static void Drag(
        CupertinoThemeTestHarness harness,
        Point from,
        double delta,
        double moveMilliseconds = 16.0)
    {
        Point to = from + new Vector(delta, 0.0);
        Down(harness, from);
        harness.Pump(ViewSize);
        Move(harness, to, moveMilliseconds);
        harness.Pump(ViewSize);
        Up(harness, to, moveMilliseconds + 16.0);
        harness.Pump(ViewSize);
    }

    private static void Down(CupertinoThemeTestHarness harness, Point position)
    {
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(1, PointerDeviceKind.Touch, position, PointerButtons.Primary, Clock));
    }

    private static void Move(CupertinoThemeTestHarness harness, Point position, double milliseconds)
    {
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerMoveEvent(
                1,
                PointerDeviceKind.Touch,
                position,
                PointerButtons.Primary,
                true,
                Clock.AddMilliseconds(milliseconds)));
    }

    private static void Up(CupertinoThemeTestHarness harness, Point position, double milliseconds)
    {
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerUpEvent(
                1,
                PointerDeviceKind.Touch,
                position,
                PointerButtons.None,
                Clock.AddMilliseconds(milliseconds)));
    }

    /// <summary>A fixed epoch so every gesture in a test starts from the same timestamp.</summary>
    private static DateTime Clock { get; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static SemanticsNode? FindSemantics(SemanticsNode node, Func<SemanticsNode, bool> predicate)
    {
        if (predicate(node))
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            if (FindSemantics(child, predicate) is { } result)
            {
                return result;
            }
        }

        return null;
    }

    private static Widget Wrap(
        Widget child,
        PlatformBrightness brightness = PlatformBrightness.Light,
        CupertinoUserInterfaceLevelData level = CupertinoUserInterfaceLevelData.Base,
        bool highContrast = false,
        TextDirection direction = TextDirection.Ltr)
    {
        return new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: brightness, HighContrast: highContrast),
            child: new Directionality(
                direction,
                new CupertinoTheme(
                    new CupertinoThemeData(brightness: brightness),
                    new CupertinoUserInterfaceLevel(level, new Center(child: child)))));
    }

    private static Widget WrapBuilder(
        StatefulWidgetBuilder builder,
        TextDirection direction = TextDirection.Ltr)
    {
        return Wrap(new StatefulBuilder(builder), direction: direction);
    }
}
