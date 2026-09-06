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

    public CupertinoMenuAnchorTests()
    {
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
    public void LayoutAttachmentUsesSourceThresholdsGapAndExplicitPosition()
    {
        var anchor = new Rect(20.0, 20.0, 40.0, 20.0);
        CupertinoMenuAttachment upperLeft = CupertinoMenuLayoutDelegate.ResolveAttachment(
            anchor,
            new Size(400.0, 600.0),
            position: null);
        Assert.Equal(new Alignment(-1.0, -1.0), upperLeft.MenuAlignment);
        Assert.Equal(new Point(20.0, 48.0), upperLeft.AttachmentPoint);

        CupertinoMenuAttachment explicitPosition = CupertinoMenuLayoutDelegate.ResolveAttachment(
            anchor,
            new Size(400.0, 600.0),
            new Vector(10.0, 5.0));
        Assert.Equal(new Point(30.0, 25.0), explicitPosition.AttachmentPoint);
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
        Assert.Contains(harness.FindWidgets<Opacity>(), widget => widget.Value < 0.5);
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
        Assert.DoesNotContain(harness.FindWidgets<Opacity>(), widget => widget.Value < 0.5);
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
                            new Overlay(initialEntries: [new OverlayEntry(_ => child)]))))));
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
