using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Cupertino;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// cupertino_ui/test/context_menu_test.dart
// cupertino_ui/test/context_menu_action_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoContextMenuTests : IDisposable
{
    private static readonly Size ViewSize = new(400.0, 600.0);

    public CupertinoContextMenuTests()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void Constructors_UseFlutterDefaultsAndRequireActions()
    {
        var action = new CupertinoContextMenuAction(new Text("Copy"));
        var child = new Text("Preview");
        var menu = new CupertinoContextMenu([action], child);

        Assert.Same(child, menu.Child);
        Assert.Same(action, Assert.Single(menu.Actions));
        Assert.False(menu.EnableHapticFeedback);
        Assert.Equal(12.0, CupertinoContextMenu.OpenBorderRadius);
        Assert.Equal(800.0 / 1135.0, CupertinoContextMenu.AnimationOpensAt);
        Assert.Equal(Color.FromUInt32(0xFFF1F1F1), CupertinoContextMenu.BackgroundColor.Color);
        Assert.Equal(Color.FromUInt32(0xFF212122), CupertinoContextMenu.BackgroundColor.DarkColor);

        var built = CupertinoContextMenu.WithBuilder(
            [action],
            (_, animation) => new SizedBox(width: 20.0 + animation.Value));
        Assert.Null(built.Child);
        Assert.Throws<ArgumentException>(() => new CupertinoContextMenu([], child));
        Assert.Throws<ArgumentException>(() => CupertinoContextMenu.WithBuilder([], (_, _) => child));
    }

    [Fact]
    public void Action_UsesExactLayoutStyleAndDynamicColors()
    {
        var action = new CupertinoContextMenuAction(
            child: new Text("Delete"),
            isDefaultAction: true,
            isDestructiveAction: true,
            trailingIcon: new IconData(0x2713));
        using var light = new CupertinoThemeTestHarness(WrapPlain(action, PlatformBrightness.Light));
        light.Pump(ViewSize);

        ConstrainedBox constrained = Assert.Single(light.FindWidgets<ConstrainedBox>());
        Assert.Equal(43.0, constrained.Constraints.MinHeight);
        Padding padding = Assert.Single(light.FindWidgets<Padding>());
        Assert.Equal(new Thickness(15.5, 8.0, 17.5, 8.0), padding.Insets);
        DefaultTextStyle textStyle = Assert.Single(light.FindWidgets<DefaultTextStyle>());
        Assert.Equal(16.0, textStyle.Style.FontSize);
        Assert.Equal(FontWeight.SemiBold, textStyle.Style.FontWeight);
        Assert.Equal(CupertinoColors.Label.Color, textStyle.Style.Color);
        Icon icon = Assert.Single(light.FindWidgets<Icon>());
        Assert.Equal(21.0, icon.Size);
        Assert.Equal(textStyle.Style.Color, icon.Color);

        using var dark = new CupertinoThemeTestHarness(WrapPlain(
            new CupertinoContextMenuAction(new Text("Copy")),
            PlatformBrightness.Dark));
        dark.Pump(ViewSize);
        Assert.Equal(
            Color.FromUInt32(0xFF212122),
            Assert.Single(dark.FindWidgets<ColoredBox>()).Color);
        Assert.Equal(
            CupertinoColors.Label.DarkColor,
            Assert.Single(dark.FindWidgets<DefaultTextStyle>()).Style.Color);
    }

    [Fact]
    public void Action_PressesImmediatelyAndInvokesOnlyItsEnabledCallback()
    {
        int presses = 0;
        var action = new CupertinoContextMenuAction(
            new Text("Copy"),
            onPressed: () => presses += 1);
        using var harness = new CupertinoThemeTestHarness(WrapPlain(action));
        harness.Pump(ViewSize);

        SendDown(harness.RenderView, new Point(30.0, 20.0), 41);
        harness.Pump(ViewSize);
        Assert.Equal(
            Color.FromUInt32(0xFFDDDDDD),
            Assert.Single(harness.FindWidgets<ColoredBox>()).Color);

        SendUp(harness.RenderView, new Point(30.0, 20.0), 41);
        harness.Pump(ViewSize);
        Assert.Equal(1, presses);
        Assert.Equal(
            Color.FromUInt32(0xFFF1F1F1),
            Assert.Single(harness.FindWidgets<ColoredBox>()).Color);

        using var disabled = new CupertinoThemeTestHarness(WrapPlain(
            new CupertinoContextMenuAction(new Text("Disabled"))));
        disabled.Pump(ViewSize);
        SendDown(disabled.RenderView, new Point(30.0, 20.0), 42);
        disabled.Pump(ViewSize);
        Assert.Equal(
            Color.FromUInt32(0xFFDDDDDD),
            Assert.Single(disabled.FindWidgets<ColoredBox>()).Color);
    }

    [Fact]
    public void ClosedMenu_PreservesPreviewGeometryAndZeroArea()
    {
        Widget preview = new SizedBox(width: 80.0, height: 60.0, child: new Text("Preview"));
        using var plain = new CupertinoThemeTestHarness(WrapPlain(preview));
        plain.Pump(ViewSize);
        RenderConstrainedBox plainBox = FindSizedBox(plain.RenderView, 80.0, 60.0);

        using var wrapped = new CupertinoThemeTestHarness(WrapPlain(new CupertinoContextMenu(
            actions: [new CupertinoContextMenuAction(new Text("Copy"))],
            child: preview)));
        wrapped.Pump(ViewSize);
        RenderConstrainedBox wrappedBox = FindSizedBox(wrapped.RenderView, 80.0, 60.0);
        Assert.Equal(plainBox.Size, wrappedBox.Size);
        Assert.Equal(plainBox.LocalToGlobal(default), wrappedBox.LocalToGlobal(default));

        using var zero = new CupertinoThemeTestHarness(WrapPlain(new SizedBox(
            width: 0.0,
            height: 0.0,
            child: new CupertinoContextMenu(
                actions: [new CupertinoContextMenuAction(new Text("Copy"))],
                child: new Text("Preview")))));
        zero.Pump(ViewSize);
        Assert.Equal(default, FindSizedBox(zero.RenderView, 0.0, 0.0).Size);
    }

    [Fact]
    public void Hold_InsertsScaledRootOverlayThenOpensFilteredScrollableRoute()
    {
        var actions = Enumerable.Range(0, 12)
            .Select(index => (Widget)new CupertinoContextMenuAction(new Text($"Action {index}")))
            .ToArray();
        using var harness = new CupertinoThemeTestHarness(WrapWithNavigator(new Align(
            alignment: Alignment.Center,
            child: new CupertinoContextMenu(
                actions: actions,
                child: new SizedBox(width: 80.0, height: 60.0, child: new Text("Preview"))))));
        harness.Pump(ViewSize);
        NavigatorState navigator = harness.FindState<NavigatorState>();
        int initialEntries = navigator.Overlay!.Entries.Count;

        SendDown(harness.RenderView, new Point(200.0, 300.0), 77);
        harness.Pump(ViewSize);
        Assert.Equal(initialEntries + 1, navigator.Overlay.Entries.Count);

        AnimationPump.Advance(0.85);
        harness.Pump(ViewSize);
        AnimationPump.Advance(0.40);
        harness.Pump(ViewSize);

        Assert.Equal(initialEntries + 2, navigator.Overlay.Entries.Count);
        Assert.Single(harness.FindWidgets<BackdropFilter>());
        Assert.Single(harness.FindWidgets<CupertinoScrollbar>());
        Assert.Single(harness.FindWidgets<SingleChildScrollView>());
        Assert.Equal(12, harness.FindWidgets<CupertinoContextMenuAction>().Count);
        IReadOnlyList<BorderRadius> radii = harness.FindWidgets<ClipRSuperellipse>()
            .Select(clip => clip.BorderRadius.Resolve(TextDirection.Ltr))
            .ToArray();
        Assert.Contains(BorderRadius.Zero, radii);
        Assert.Contains(BorderRadius.Circular(13.0), radii);
        Assert.Contains(
            harness.FindWidgets<SizedBox>(),
            box => box.Width == 250.0);
    }

    private static Widget WrapPlain(
        Widget child,
        PlatformBrightness brightness = PlatformBrightness.Light)
    {
        return WrapEnvironment(
            new Align(alignment: Alignment.TopLeft, child: child),
            brightness);
    }

    private static Widget WrapWithNavigator(Widget child)
    {
        return WrapEnvironment(new Navigator(new BuilderPageRoute(_ => child)));
    }

    private static Widget WrapEnvironment(
        Widget child,
        PlatformBrightness brightness = PlatformBrightness.Light)
    {
        return new MediaQuery(
            data: new MediaQueryData(
                Size: ViewSize,
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
                    new CupertinoTheme(new CupertinoThemeData(), child))));
    }

    private static RenderConstrainedBox FindSizedBox(RenderObject root, double width, double height)
    {
        return FindAll<RenderConstrainedBox>(root).Single(box =>
            box.AdditionalConstraints.MinWidth == width
            && box.AdditionalConstraints.MaxWidth == width
            && box.AdditionalConstraints.MinHeight == height
            && box.AdditionalConstraints.MaxHeight == height);
    }

    private static IReadOnlyList<T> FindAll<T>(RenderObject root) where T : RenderObject
    {
        var result = new List<T>();
        void Visit(RenderObject node)
        {
            if (node is T typed)
            {
                result.Add(typed);
            }

            node.VisitChildren(Visit);
        }

        Visit(root);
        return result;
    }

    private static void SendDown(RenderView view, Point position, int pointer)
    {
        GestureBinding.Instance.HandlePointerEvent(
            view,
            new PointerDownEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.Primary,
                DateTime.UtcNow));
    }

    private static void SendUp(RenderView view, Point position, int pointer)
    {
        GestureBinding.Instance.HandlePointerEvent(
            view,
            new PointerUpEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.None,
                DateTime.UtcNow));
    }
}
