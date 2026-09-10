using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoMenuAnchorTests : IDisposable
{
    private static readonly Size ViewSize = new(390.0, 640.0);

    /// <summary>A device id of this file's own, so a stale hover from another test cannot leak in.</summary>
    private const int HoverDevice = 9147;

    public CupertinoMenuAnchorTests()
    {
        // `Scheduler.ResetForTests` drops any frame callback the gesture binding had already
        // scheduled, which would leave its mouse-tracker update flag stuck and swallow hover exits.
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
        WidgetsBinding.Instance.HandleAccessibilityFeaturesChanged(default);
    }

    public void Dispose()
    {
        WidgetsBinding.Instance.HandleAccessibilityFeaturesChanged(default);
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void Constructors_ExposeSourceDefaultsAndValidateSwipeConfiguration()
    {
        var child = new Text("Anchor");
        var item = new CupertinoMenuItem(child: new Text("Item"));
        var anchor = new CupertinoMenuAnchor(menuChildren: [item], child: child);

        Assert.False(anchor.ConstrainCrossAxis);
        Assert.False(anchor.ConsumeOutsideTaps);
        Assert.True(anchor.EnableSwipe);
        Assert.False(anchor.EnableLongPressToOpen);
        Assert.False(anchor.UseRootOverlay);
        Assert.Equal(EdgeInsetsGeometry.All(8.0), anchor.OverlayPadding);
        Assert.Null(anchor.Constraints);
        Assert.Same(child, anchor.Child);

        Assert.Null(item.Subtitle);
        Assert.Null(item.Leading);
        Assert.Null(item.Trailing);
        Assert.False(item.Autofocus);
        Assert.Equal(HitTestBehavior.Opaque, item.Behavior);
        Assert.True(item.RequestCloseOnActivate);
        Assert.True(item.RequestFocusOnHover);
        Assert.False(item.IsDestructiveAction);
        Assert.False(item.IsDivider);

        Assert.Throws<ArgumentException>(() => new CupertinoMenuAnchor(
            menuChildren: [item],
            enableSwipe: false,
            enableLongPressToOpen: true));
    }

    [Fact]
    public void Divider_UsesEightPixelHeightAndBrightnessResolvedDefaultColor()
    {
        var divider = new CupertinoMenuDivider();
        Assert.True(divider.IsDivider);
        Assert.False(divider.HasLeading(null!));

        using var light = new CupertinoThemeTestHarness(Wrap(divider));
        light.Pump(ViewSize);
        ColoredBox lightBox = Assert.Single(light.FindWidgets<ColoredBox>());
        Assert.Equal(Color.FromArgb(20, 0, 0, 0), lightBox.Color);
        Assert.Equal(8.0, Assert.Single(light.FindWidgets<SizedBox>()).Height);

        using var dark = new CupertinoThemeTestHarness(Wrap(divider, PlatformBrightness.Dark));
        dark.Pump(ViewSize);
        Assert.Equal(Color.FromArgb(41, 0, 0, 0), Assert.Single(dark.FindWidgets<ColoredBox>()).Color);
    }

    [Fact]
    public void Menu_InsertsImplicitDividersButNotAdjacentToExplicitDividers()
    {
        var controller = new MenuController();
        Widget menu = BuildAnchor(
            controller,
            [
                new CupertinoMenuItem(child: new Text("One")),
                new CupertinoMenuItem(child: new Text("Two")),
                new CupertinoMenuDivider(),
                new CupertinoMenuItem(child: new Text("Three")),
            ]);
        using var harness = Open(menu, controller);

        Assert.Single(harness.FindWidgets<CupertinoMenuImplicitDivider>());
        Assert.Single(harness.FindWidgets<CupertinoMenuDivider>());
    }

    [Theory]
    [InlineData(390.0, 1.0, 250.0)]
    [InlineData(800.0, 1.0, 262.0)]
    [InlineData(390.0, 28.0 / 17.0, 370.0)]
    [InlineData(800.0, 28.0 / 17.0, 343.0)]
    public void Menu_DefaultWidthMatchesScreenAndLargeTextTables(
        double screenWidth,
        double textScale,
        double expectedWidth)
    {
        var controller = new MenuController();
        Widget menu = BuildAnchor(
            controller,
            [new CupertinoMenuItem(child: new Text("Item"))],
            textScale: textScale,
            screenWidth: screenWidth);
        using var harness = Open(menu, controller, new Size(screenWidth, 640.0));

        Assert.Contains(
            harness.FindWidgets<ConstrainedBox>(),
            box => box.Constraints == BoxConstraints.TightFor(width: expectedWidth));
    }

    [Fact]
    public void Item_DefaultColorsResolveEnabledDisabledDestructiveAndDarkVariants()
    {
        using var enabled = new CupertinoThemeTestHarness(Wrap(new CupertinoMenuItem(
            child: new Text("Enabled"),
            onPressed: static () => { })));
        enabled.Pump(ViewSize);
        Assert.Equal(
            Color.FromArgb(245, 0, 0, 0),
            Assert.IsType<SolidColorBrush>(FindParagraph(enabled.RenderView, "Enabled").Foreground).Color);

        using var disabled = new CupertinoThemeTestHarness(Wrap(new CupertinoMenuItem(
            child: new Text("Disabled"))));
        disabled.Pump(ViewSize);
        Assert.Equal(
            CupertinoColors.SystemGrey.Color,
            Assert.IsType<SolidColorBrush>(FindParagraph(disabled.RenderView, "Disabled").Foreground).Color);

        using var destructive = new CupertinoThemeTestHarness(Wrap(new CupertinoMenuItem(
            child: new Text("Delete"),
            isDestructiveAction: true,
            onPressed: static () => { })));
        destructive.Pump(ViewSize);
        Assert.Equal(
            CupertinoColors.SystemRed.Color,
            Assert.IsType<SolidColorBrush>(FindParagraph(destructive.RenderView, "Delete").Foreground).Color);

        using var dark = new CupertinoThemeTestHarness(Wrap(
            new CupertinoMenuItem(child: new Text("Dark"), onPressed: static () => { }),
            PlatformBrightness.Dark));
        dark.Pump(ViewSize);
        Assert.Equal(
            Color.FromArgb(245, 255, 255, 255),
            Assert.IsType<SolidColorBrush>(FindParagraph(dark.RenderView, "Dark").Foreground).Color);
    }

    [Fact]
    public void Item_PressUpdatesDecorationInvokesCallbackAndRequestsMenuClose()
    {
        int pressed = 0;
        using var timers = new FakeGestureTimers();
        var controller = new MenuController();
        Widget menu = BuildAnchor(
            controller,
            [new CupertinoMenuItem(child: new Text("Press me"), onPressed: () => pressed++)]);
        using var harness = Open(menu, controller);
        RenderParagraph paragraph = FindParagraph(harness.RenderView, "Press me");
        Point position = paragraph.LocalToGlobal(new Point(2.0, 2.0));

        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(
                501,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.Primary,
                DateTime.UtcNow));
        // The pressed decoration follows the tap-down, which competing recognizers defer to the
        // kPressTimeout deadline, exactly like Flutter.
        timers.Elapse(GestureConstants.PressTimeout);
        harness.Pump(ViewSize);
        Assert.Contains(
            harness.FindWidgets<DecoratedBox>(),
            box => box.Decoration is BoxDecoration { Color: { } color } && color.A == 26);

        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerUpEvent(
                501,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.None,
                DateTime.UtcNow));
        harness.Pump(ViewSize);
        Assert.Equal(1, pressed);
        Settle(harness, ViewSize);
        Assert.False(controller.IsOpen);
    }

    [Fact]
    public void Item_CloseOnActivateCanBeDisabled()
    {
        int pressed = 0;
        var controller = new MenuController();
        Widget menu = BuildAnchor(
            controller,
            [
                new CupertinoMenuItem(
                    child: new Text("Stay open"),
                    requestCloseOnActivate: false,
                    onPressed: () => pressed++),
            ]);
        using var harness = Open(menu, controller);
        RenderParagraph paragraph = FindParagraph(harness.RenderView, "Stay open");
        Tap(harness.RenderView, paragraph.LocalToGlobal(new Point(2.0, 2.0)), pointer: 502);
        harness.Pump(ViewSize);

        Assert.Equal(1, pressed);
        Assert.True(controller.IsOpen);
    }

    [Fact]
    public void LeadingSiblingMakesPlainItemsReserveLeadingSpace()
    {
        var controller = new MenuController();
        Widget menu = BuildAnchor(
            controller,
            [
                new CupertinoMenuItem(
                    child: new Text("Leading"),
                    leading: new SizedBox(width: 10.0, height: 10.0)),
                new CupertinoMenuItem(child: new Text("Plain")),
            ]);
        using var harness = Open(menu, controller);
        RenderParagraph plain = FindParagraph(harness.RenderView, "Plain");
        double withLeading = plain.LocalToGlobal(default).X;

        var secondController = new MenuController();
        using var withoutHarness = Open(
            BuildAnchor(
                secondController,
                [
                    new CupertinoMenuItem(child: new Text("First")),
                    new CupertinoMenuItem(child: new Text("Plain")),
                ]),
            secondController);
        double withoutLeading = FindParagraph(withoutHarness.RenderView, "Plain").LocalToGlobal(default).X;

        Assert.Equal(16.0, withLeading - withoutLeading, precision: 4);
    }

    [Fact]
    public void LayoutAttachmentUsesSourceThresholdsAndGap()
    {
        var controller = new MenuController();
        Widget menu = BuildAnchor(controller, [new CupertinoMenuItem(child: new Text("Item"))]);
        using var harness = Open(menu, controller);

        // The 44x44 anchor is centred in a 390x640 view, so the vertical midpoint ratio is 0.5
        // (< 0.55) and the menu attaches below the anchor with the 8px gap; the horizontal midpoint
        // ratio is 0.5, which is neither < 0.4 nor > 0.6, so the transform origin is the anchor's
        // bottom centre.
        ScaleTransition scale = Assert.Single(harness.FindWidgets<ScaleTransition>());
        Assert.Equal(0.0, scale.Alignment.X, precision: 6);
        Assert.Equal((350.0 / 640.0 * 2.0) - 1.0, scale.Alignment.Y, precision: 6);
    }

    [Fact]
    public void OpenAndCloseReportAnimationStatusesAndDisabledAnimationsSkipTheFade()
    {
        var statuses = new List<AnimationStatus>();
        var controller = new MenuController();
        Widget anchor = BuildAnchor(
            controller,
            [new CupertinoMenuItem(child: new Text("Item"))],
            onAnimationStatusChanged: statuses.Add);
        using var harness = new CupertinoThemeTestHarness(anchor);
        harness.Pump(ViewSize);

        controller.Open();
        harness.Pump(ViewSize);
        Assert.True(controller.IsOpen);
        Assert.Equal([AnimationStatus.Forward], statuses);
        Assert.Contains(harness.FindWidgets<FadeTransition>(), widget => widget.Opacity.Value < 0.5);
        Settle(harness, ViewSize);
        Assert.Equal([AnimationStatus.Forward, AnimationStatus.Completed], statuses);

        controller.Close();
        harness.Pump(ViewSize);
        Assert.Contains(AnimationStatus.Reverse, statuses);
        Settle(harness, ViewSize);
        Assert.False(controller.IsOpen);
        Assert.Equal(AnimationStatus.Dismissed, statuses[^1]);

        WidgetsBinding.Instance.HandleAccessibilityFeaturesChanged(
            new AccessibilityFeatures(DisableAnimations: true));
        harness.Pump(ViewSize);
        controller.Open();
        harness.Pump(ViewSize);
        Assert.DoesNotContain(
            harness.FindWidgets<FadeTransition>(),
            widget => widget.Opacity.Value < 0.5);
    }

    [Theory]
    [InlineData(false, false, false, 5)]
    [InlineData(true, false, false, 4)]
    [InlineData(false, true, false, 3)]
    [InlineData(false, false, true, 4)]
    public void Menu_ImplicitDividerPlacementMatchesEveryAdjacency(
        bool firstIsDivider,
        bool middleIsDivider,
        bool lastIsDivider,
        int expectedChildren)
    {
        var controller = new MenuController();
        Widget menu = BuildAnchor(
            controller,
            [
                Entry(firstIsDivider, "One"),
                Entry(middleIsDivider, "Two"),
                Entry(lastIsDivider, "Three"),
            ]);
        using var harness = Open(menu, controller);

        Assert.Equal(expectedChildren, MenuColumnChildren(harness).Count);
    }

    [Fact]
    public void Menu_InsertsImplicitDividersAroundWidgetsThatAreNotMenuEntries()
    {
        var controller = new MenuController();
        Widget menu = BuildAnchor(
            controller,
            [
                new CupertinoMenuItem(child: new Text("One")),
                new SizedBox(height: 10.0),
                new CupertinoMenuItem(child: new Text("Two")),
            ]);
        using var harness = Open(menu, controller);

        IReadOnlyList<Widget> children = MenuColumnChildren(harness);
        Assert.Equal(5, children.Count);
        Assert.IsType<CupertinoMenuImplicitDivider>(children[1]);
        Assert.IsType<CupertinoMenuImplicitDivider>(children[3]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Item_DefaultDecorationResolvesEveryInteractionState(bool dark)
    {
        WidgetStateProperty<BoxDecoration> property = dark
            ? CupertinoMenuItem.KDefaultDarkDecoration
            : CupertinoMenuItem.KDefaultDecoration;
        byte red = dark ? (byte)255 : (byte)50;

        Assert.Equal(
            Color.FromArgb(26, red, red, red),
            property.Resolve(new HashSet<WidgetState> { WidgetState.Dragged }).Color);
        Assert.Equal(
            Color.FromArgb(26, red, red, red),
            property.Resolve(new HashSet<WidgetState> { WidgetState.Pressed }).Color);
        Assert.Equal(
            Color.FromArgb(19, red, red, red),
            property.Resolve(new HashSet<WidgetState> { WidgetState.Focused }).Color);
        Assert.Equal(
            Color.FromArgb(13, red, red, red),
            property.Resolve(new HashSet<WidgetState> { WidgetState.Hovered }).Color);
        Assert.Null(property.Resolve(new HashSet<WidgetState>()).Color);

        // Dart's ordered map puts `dragged` before `pressed`, `focused` and `hovered`.
        Assert.Equal(
            Color.FromArgb(26, red, red, red),
            property
                .Resolve(new HashSet<WidgetState> { WidgetState.Hovered, WidgetState.Dragged })
                .Color);
    }

    [Fact]
    public void Item_HoverReportsOnceEachWayAndRequestsFocusUnlessOptedOut()
    {
        var hovers = new List<bool>();
        var focusChanges = new List<bool>();
        var controller = new MenuController();
        Widget menu = BuildAnchor(
            controller,
            [
                new CupertinoMenuItem(
                    child: new Text("Hover me"),
                    onHover: hovers.Add,
                    onFocusChange: focusChanges.Add,
                    onPressed: static () => { }),
            ]);
        using var harness = Open(menu, controller);
        RenderParagraph paragraph = FindParagraph(harness.RenderView, "Hover me");
        Point inside = paragraph.LocalToGlobal(new Point(2.0, 2.0));

        Hover(harness, inside);
        Hover(harness, inside);
        Assert.Equal([true], hovers);
        Assert.Equal([true], focusChanges);

        Exit(harness);
        Exit(harness);
        Assert.Equal([true, false], hovers);
    }

    [Fact]
    public void Item_DisabledItemReportsNoHoverAndTakesNoFocus()
    {
        var hovers = new List<bool>();
        var focusChanges = new List<bool>();
        var controller = new MenuController();
        Widget menu = BuildAnchor(
            controller,
            [
                new CupertinoMenuItem(
                    child: new Text("Disabled"),
                    onHover: hovers.Add,
                    onFocusChange: focusChanges.Add),
            ]);
        using var harness = Open(menu, controller);
        Point inside = FindParagraph(harness.RenderView, "Disabled").LocalToGlobal(new Point(2.0, 2.0));

        Hover(harness, inside);
        Exit(harness);

        Assert.Empty(hovers);
        Assert.Empty(focusChanges);
    }

    [Fact]
    public void Item_RequestFocusOnHoverCanBeDisabled()
    {
        var focusChanges = new List<bool>();
        var controller = new MenuController();
        Widget menu = BuildAnchor(
            controller,
            [
                new CupertinoMenuItem(
                    child: new Text("No focus"),
                    requestFocusOnHover: false,
                    onFocusChange: focusChanges.Add,
                    onPressed: static () => { }),
            ]);
        using var harness = Open(menu, controller);
        Hover(harness, FindParagraph(harness.RenderView, "No focus").LocalToGlobal(new Point(2.0, 2.0)));

        Assert.Empty(focusChanges);
    }

    [Fact]
    public void Item_LargeTextModeDropsTheTrailingWidgetButKeepsTheLeadingOne()
    {
        var controller = new MenuController();
        Widget Menu(double textScale) => BuildAnchor(
            controller,
            [
                new CupertinoMenuItem(
                    child: new Text("Item"),
                    leading: new Text("L"),
                    trailing: new Text("T")),
            ],
            textScale: textScale);

        using var normal = Open(Menu(1.0), controller);
        Assert.Contains(FindAll<RenderParagraph>(normal.RenderView), p => p.PlainText == "T");
        Assert.Contains(FindAll<RenderParagraph>(normal.RenderView), p => p.PlainText == "L");

        var largeController = new MenuController();
        using var large = Open(
            BuildAnchor(
                largeController,
                [
                    new CupertinoMenuItem(
                        child: new Text("Item"),
                        leading: new Text("L"),
                        trailing: new Text("T")),
                ],
                textScale: 1.0 + (11.0 / 17.0)),
            largeController);
        Assert.DoesNotContain(FindAll<RenderParagraph>(large.RenderView), p => p.PlainText == "T");
        Assert.Contains(FindAll<RenderParagraph>(large.RenderView), p => p.PlainText == "L");
    }

    [Fact]
    public void Item_SubtitleUsesTheSubheadStepAndTheSubtitleColor()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoMenuItem(
            child: new Text("Title"),
            subtitle: new Text("Subtitle"),
            onPressed: static () => { })));
        harness.Pump(ViewSize);

        RenderParagraph subtitle = FindParagraph(harness.RenderView, "Subtitle");
        Assert.Equal(15.0, subtitle.Text.Style!.FontSize!.Value, precision: 6);
        Assert.Equal(
            Color.FromArgb(140, 0, 0, 0),
            Assert.IsType<SolidColorBrush>(subtitle.Foreground).Color);
    }

    [Fact]
    public void Item_ChildKeepsTheSeventeenPointBaseSizeAcrossDynamicTypeSteps()
    {
        foreach (double scale in new[] { 1.0, 1.5, 1.0 + (11.0 / 17.0) })
        {
            using var harness = new CupertinoThemeTestHarness(Wrap(
                new CupertinoMenuItem(child: new Text("Item"), onPressed: static () => { }),
                textScale: scale));
            harness.Pump(ViewSize);

            RenderParagraph child = FindParagraph(harness.RenderView, "Item");
            Assert.Equal(17.0, child.Text.Style!.FontSize!.Value, precision: 6);
        }
    }

    [Fact]
    public void Keyboard_ArrowKeysMoveFocusAndHomeEndJumpToTheBoundaries()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
        try
        {
            var controller = new MenuController();
            using var harness = Open(
                BuildAnchor(controller, [Pressable("a", autofocus: true), Pressable("b"), Pressable("c")]),
                controller);
            Assert.Equal("a", FocusedLabel(harness));

            PressKey(harness, LogicalKeyboardKey.ArrowDown);
            Assert.Equal("b", FocusedLabel(harness));
            PressKey(harness, LogicalKeyboardKey.ArrowDown);
            Assert.Equal("c", FocusedLabel(harness));
            PressKey(harness, LogicalKeyboardKey.ArrowUp);
            Assert.Equal("b", FocusedLabel(harness));

            PressKey(harness, LogicalKeyboardKey.End);
            Assert.Equal("c", FocusedLabel(harness));
            PressKey(harness, LogicalKeyboardKey.Home);
            Assert.Equal("a", FocusedLabel(harness));
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = null;
        }
    }

    [Theory]
    [InlineData(TargetPlatform.MacOS, false)]
    [InlineData(TargetPlatform.Windows, true)]
    public void Keyboard_FocusWrapsOnlyOffApplePlatforms(TargetPlatform platform, bool wraps)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        try
        {
            var controller = new MenuController();
            using var harness = Open(
                BuildAnchor(controller, [Pressable("a", autofocus: true), Pressable("b"), Pressable("c")]),
                controller);

            Assert.Equal("a", FocusedLabel(harness));
            PressKey(harness, LogicalKeyboardKey.ArrowUp);
            Assert.Equal(wraps ? "c" : "a", FocusedLabel(harness));

            PressKey(harness, LogicalKeyboardKey.End);
            Assert.Equal("c", FocusedLabel(harness));
            PressKey(harness, LogicalKeyboardKey.ArrowDown);
            Assert.Equal(wraps ? "a" : "c", FocusedLabel(harness));
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = null;
        }
    }

    [Fact]
    public void ReduceMotion_PinsTheScaleAtOneWhileTheFadeStillRuns()
    {
        WidgetsBinding.Instance.HandleAccessibilityFeaturesChanged(
            new AccessibilityFeatures(ReduceMotion: true));
        var controller = new MenuController();
        using var harness = new CupertinoThemeTestHarness(
            BuildAnchor(controller, [new CupertinoMenuItem(child: new Text("Item"))]));
        harness.Pump(ViewSize);

        controller.Open();
        harness.Pump(ViewSize);

        ScaleTransition scale = Assert.Single(harness.FindWidgets<ScaleTransition>());
        Assert.Equal(1.0, scale.Scale.Value, precision: 2);
        Assert.Contains(harness.FindWidgets<FadeTransition>(), widget => widget.Opacity.Value < 0.5);
    }

    [Fact]
    public void ShadowPainter_TracksTheFadeAnimationAndComparesBrightnessAndRepaintSource()
    {
        var fade = new ProxyAnimation(new ConstantAnimation<double>(0.5));
        var painter = new CupertinoMenuShadowPainter(PlatformBrightness.Light, fade);

        Assert.Equal(0.5, painter.ShadowAnimation, precision: 6);
        Assert.False(painter.ShouldRepaint(new CupertinoMenuShadowPainter(PlatformBrightness.Light, fade)));
        Assert.True(painter.ShouldRepaint(new CupertinoMenuShadowPainter(PlatformBrightness.Dark, fade)));
        Assert.True(painter.ShouldRepaint(
            new CupertinoMenuShadowPainter(PlatformBrightness.Light, new ProxyAnimation())));

        // The clamp keeps the unbounded spring's overshoot out of the shadow.
        var overshoot = new CupertinoMenuShadowPainter(
            PlatformBrightness.Light,
            new ProxyAnimation(new ConstantAnimation<double>(1.4)));
        Assert.Equal(1.0, overshoot.ShadowAnimation, precision: 6);
    }

    [Fact]
    public void ClampTweenClipsTheSpringOvershootBeforeTheCurve()
    {
        var clamp = new CupertinoMenuClampTween(0.0, 1.0);

        Assert.Equal(0.0, clamp.Transform(-0.3), precision: 6);
        Assert.Equal(0.4, clamp.Transform(0.4), precision: 6);
        Assert.Equal(1.0, clamp.Transform(1.2), precision: 6);
    }

    [Fact]
    public void AnimationProductMultipliesBothParents()
    {
        var product = new CupertinoMenuAnimationProduct(
            new ConstantAnimation<double>(0.5),
            new ConstantAnimation<double>(0.8));

        Assert.Equal(0.4, product.Value, precision: 6);
    }

    [Fact]
    public void Swipe_MovingOutsideTheMenuShrinksItAndReleasingReboundsToFullSize()
    {
        var controller = new MenuController();
        using var harness = Open(
            BuildAnchor(controller, [Pressable("a")]),
            controller);
        RenderParagraph paragraph = FindParagraph(harness.RenderView, "a");
        Point inside = paragraph.LocalToGlobal(new Point(2.0, 2.0));
        ScaleTransition scale = Assert.Single(harness.FindWidgets<ScaleTransition>());
        Assert.Equal(1.0, scale.Scale.Value, precision: 2);

        DateTime timestamp = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(
                701,
                PointerDeviceKind.Touch,
                inside,
                PointerButtons.Primary,
                timestamp));
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerMoveEvent(
                701,
                PointerDeviceKind.Touch,
                new Point(inside.X, inside.Y + 400.0),
                PointerButtons.Primary,
                false,
                timestamp.AddMilliseconds(20.0)));
        for (int frame = 0; frame < 40; frame++)
        {
            AnimationPump.Advance(0.016);
            harness.Pump(ViewSize);
        }

        double swiped = Assert.Single(harness.FindWidgets<ScaleTransition>()).Scale.Value;
        Assert.True(swiped < 0.95, $"Expected the menu to shrink while swiping away, got {swiped}.");
        Assert.True(swiped >= 0.8 - 0.01, $"Expected the 80% floor to hold, got {swiped}.");

        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerUpEvent(
                701,
                PointerDeviceKind.Touch,
                new Point(inside.X, inside.Y + 400.0),
                PointerButtons.None,
                timestamp.AddMilliseconds(700.0)));
        for (int frame = 0; frame < 40; frame++)
        {
            AnimationPump.Advance(0.016);
            harness.Pump(ViewSize);
        }

        Assert.Equal(1.0, Assert.Single(harness.FindWidgets<ScaleTransition>()).Scale.Value, precision: 2);
    }

    [Fact]
    public void Swipe_CanBeDisabled()
    {
        var controller = new MenuController();
        Widget menu = Wrap(
            new Center(
                child: new CupertinoMenuAnchor(
                    controller: controller,
                    enableSwipe: false,
                    menuChildren: [Pressable("a")],
                    child: new SizedBox(width: 44.0, height: 44.0, child: new Text("Open")))));
        using var harness = Open(menu, controller);
        Point inside = FindParagraph(harness.RenderView, "a").LocalToGlobal(new Point(2.0, 2.0));

        DateTime timestamp = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(
                702,
                PointerDeviceKind.Touch,
                inside,
                PointerButtons.Primary,
                timestamp));
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerMoveEvent(
                702,
                PointerDeviceKind.Touch,
                new Point(inside.X, inside.Y + 400.0),
                PointerButtons.Primary,
                false,
                timestamp.AddMilliseconds(20.0)));
        for (int frame = 0; frame < 30; frame++)
        {
            AnimationPump.Advance(0.016);
            harness.Pump(ViewSize);
        }

        Assert.Equal(1.0, Assert.Single(harness.FindWidgets<ScaleTransition>()).Scale.Value, precision: 3);
    }

    private static Widget Entry(bool isDivider, string label) =>
        isDivider ? new CupertinoMenuDivider() : new CupertinoMenuItem(child: new Text(label));

    private static CupertinoMenuItem Pressable(string label, bool autofocus = false) =>
        new(child: new Text(label), autofocus: autofocus, onPressed: static () => { });

    private static IReadOnlyList<Widget> MenuColumnChildren(CupertinoThemeTestHarness harness) =>
        harness.FindWidgets<Column>()
            .Single(column => column.MainAxisSize == MainAxisSize.Min)
            .Children;

    private static string? FocusedLabel(CupertinoThemeTestHarness harness)
    {
        FocusNode? primary = FocusManager.Instance.PrimaryFocus;
        if (primary?.Context is null)
        {
            return null;
        }

        RenderObject? renderObject = primary.Context.FindRenderObject();
        return FindAll<RenderParagraph>(renderObject).FirstOrDefault()?.PlainText;
    }

    private static void PressKey(CupertinoThemeTestHarness harness, LogicalKeyboardKey key)
    {
        FocusManager.Instance.HandleKeyEvent(KeySim.Down(key));
        FocusManager.Instance.HandleKeyEvent(KeySim.Up(key));
        harness.Pump(ViewSize);
    }

    private static void Hover(CupertinoThemeTestHarness harness, Point position)
    {
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerHoverEvent(
                HoverDevice,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.None,
                DateTime.UtcNow));
        harness.Pump(ViewSize);
    }

    /// <summary>Moves the mouse to the view's empty top-left corner, off every menu item.</summary>
    private static void Exit(CupertinoThemeTestHarness harness)
    {
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerHoverEvent(
                HoverDevice,
                PointerDeviceKind.Mouse,
                new Point(1.0, 1.0),
                PointerButtons.None,
                DateTime.UtcNow));
        harness.Pump(ViewSize);
        harness.Pump(ViewSize);
    }

    private static CupertinoThemeTestHarness Open(
        Widget widget,
        MenuController controller,
        Size? size = null)
    {
        var harness = new CupertinoThemeTestHarness(widget);
        harness.Pump(size ?? ViewSize);
        controller.Open();
        harness.Pump(size ?? ViewSize);
        Settle(harness, size ?? ViewSize);
        return harness;
    }

    private static void Settle(CupertinoThemeTestHarness harness, Size size)
    {
        AnimationPump.Advance(2.0);
        harness.Pump(size);
    }

    private static Widget BuildAnchor(
        MenuController controller,
        IReadOnlyList<Widget> items,
        double textScale = 1.0,
        double screenWidth = 390.0,
        CupertinoMenuAnimationStatusChangedCallback? onAnimationStatusChanged = null)
    {
        return Wrap(
            new Center(
                child: new CupertinoMenuAnchor(
                    controller: controller,
                    menuChildren: items,
                    onAnimationStatusChanged: onAnimationStatusChanged,
                    child: new SizedBox(width: 44.0, height: 44.0, child: new Text("Open")))),
            textScale: textScale,
            size: new Size(screenWidth, 640.0));
    }

    private static Widget Wrap(
        Widget child,
        PlatformBrightness brightness = PlatformBrightness.Light,
        double textScale = 1.0,
        Size? size = null)
    {
        return new MediaQuery(
            data: new MediaQueryData(
                Size: size ?? ViewSize,
                TextScaler: TextScaler.Linear(textScale),
                PlatformBrightness: brightness),
            child: new Localizations(
                locale: new Locale("en", "US"),
                delegates:
                [
                    DefaultWidgetsLocalizations.Delegate,
                    DefaultCupertinoLocalizations.Delegate,
                ],
                child: new Directionality(
                    TextDirection.Ltr,
                    new CupertinoUserInterfaceLevel(
                        CupertinoUserInterfaceLevelData.Base,
                        new CupertinoTheme(
                            new CupertinoThemeData(brightness: brightness),
                            new FocusTraversalGroup(
                                new Overlay(initialEntries: [new OverlayEntry(_ => child)])))))));
    }

    private static RenderParagraph FindParagraph(RenderObject? root, string text)
    {
        return FindAll<RenderParagraph>(root)
            .Single(paragraph => paragraph.PlainText == text);
    }

    private static IReadOnlyList<T> FindAll<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }

        if (root is T typed)
        {
            result.Add(typed);
        }

        root.VisitChildren(child => result.AddRange(FindAll<T>(child)));
        return result;
    }

    private static void Tap(RenderView view, Point position, int pointer)
    {
        DateTime timestamp = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(
            view,
            new PointerDownEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.Primary,
                timestamp));
        GestureBinding.Instance.HandlePointerEvent(
            view,
            new PointerUpEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.None,
                timestamp.AddMilliseconds(20.0)));
    }
}
