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

// Dart parity source: cupertino_ui/test/bottom_tab_bar_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoTabBarTests : IDisposable
{
    private static readonly Size ViewSize = new(320.0, 200.0);

    public CupertinoTabBarTests()
    {
        GestureBinding.Instance.ResetForTests();
        PlatformDefaults.DebugIsWebOverride = null;
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        PlatformDefaults.DebugIsWebOverride = null;
    }

    [Fact]
    public void Constructor_ValidatesAndUsesFlutterDefaults()
    {
        IReadOnlyList<BottomNavigationBarItem> items = BuildItems();
        var tabBar = new CupertinoTabBar(items);

        Assert.Same(items, tabBar.Items);
        Assert.Null(tabBar.OnTap);
        Assert.Equal(0, tabBar.CurrentIndex);
        Assert.Null(tabBar.BackgroundColor);
        Assert.Null(tabBar.ActiveColor);
        Assert.Same(CupertinoColors.InactiveGray, tabBar.InactiveColor);
        Assert.Equal(30.0, tabBar.IconSize);
        Assert.Equal(50.0, tabBar.Height);
        Assert.NotNull(tabBar.Border);
        Assert.Equal(50.0, tabBar.PreferredSize.Height);

        var borderless = new CupertinoTabBar(items, border: null);
        Assert.Null(borderless.Border);

        Action<int> callback = _ => { };
        var copy = tabBar.CopyWith(currentIndex: 1, height: 64.0, onTap: callback);
        Assert.Same(items, copy.Items);
        Assert.Equal(1, copy.CurrentIndex);
        Assert.Equal(64.0, copy.Height);
        Assert.Same(callback, copy.OnTap);
        Assert.Same(tabBar.Border, copy.Border);

        Assert.Throws<ArgumentException>(() => new CupertinoTabBar(
            [new BottomNavigationBarItem(new Text("Only"), "Only")]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoTabBar(items, currentIndex: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoTabBar(items, height: -1.0));
    }

    [Fact]
    public void Build_ResolvesColorsBorderIconsLabelsAndBlur()
    {
        CupertinoDynamicColor active = CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xFF123456),
            Color.FromUInt32(0xFF234567));
        CupertinoDynamicColor inactive = CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xFF654321),
            Color.FromUInt32(0xFF765432));
        var tabBar = new CupertinoTabBar(
            BuildItems(withActiveIcon: true),
            currentIndex: 1,
            activeColor: active,
            inactiveColor: inactive);

        using var lightHarness = new CupertinoThemeTestHarness(Wrap(tabBar));
        lightHarness.Pump(ViewSize);

        Assert.Single(FindAll<RenderBackdropFilter>(lightHarness.RenderView));
        RenderDecoratedBox lightDecoration = Assert.Single(FindAll<RenderDecoratedBox>(lightHarness.RenderView));
        var lightBox = Assert.IsType<BoxDecoration>(lightDecoration.DecorationValue);
        Assert.Equal(0xF0F9F9F9u, lightBox.Color!.Value.ToUInt32());
        Assert.Equal(0x4D000000u, Assert.IsType<Border>(lightBox.Border).Top.Color.ToUInt32());
        Assert.Null(FindParagraph(lightHarness.RenderView, "inactive-two"));
        Assert.NotNull(FindParagraph(lightHarness.RenderView, "active-two"));
        Assert.Equal(0xFF654321u, Foreground(FindParagraph(lightHarness.RenderView, "First")!).ToUInt32());
        Assert.Equal(0xFF123456u, Foreground(FindParagraph(lightHarness.RenderView, "active-two")!).ToUInt32());
        Assert.Equal(0xFF123456u, Foreground(FindParagraph(lightHarness.RenderView, "Second")!).ToUInt32());

        using var darkHarness = new CupertinoThemeTestHarness(Wrap(
            tabBar,
            brightness: PlatformBrightness.Dark));
        darkHarness.Pump(ViewSize);
        RenderDecoratedBox darkDecoration = Assert.Single(FindAll<RenderDecoratedBox>(darkHarness.RenderView));
        var darkBox = Assert.IsType<BoxDecoration>(darkDecoration.DecorationValue);
        Assert.Equal(0xF01D1D1Du, darkBox.Color!.Value.ToUInt32());
        Assert.Equal(0x29000000u, Assert.IsType<Border>(darkBox.Border).Top.Color.ToUInt32());
        Assert.Equal(0xFF765432u, Foreground(FindParagraph(darkHarness.RenderView, "First")!).ToUInt32());
        Assert.Equal(0xFF234567u, Foreground(FindParagraph(darkHarness.RenderView, "Second")!).ToUInt32());

        using var opaqueHarness = new CupertinoThemeTestHarness(Wrap(new CupertinoTabBar(
            BuildItems(),
            backgroundColor: Colors.White)));
        opaqueHarness.Pump(ViewSize);
        Assert.Empty(FindAll<RenderBackdropFilter>(opaqueHarness.RenderView));

        using var borderlessHarness = new CupertinoThemeTestHarness(Wrap(new CupertinoTabBar(
            BuildItems(),
            border: null,
            backgroundColor: Colors.White)));
        borderlessHarness.Pump(ViewSize);
        RenderDecoratedBox borderlessDecoration =
            Assert.Single(FindAll<RenderDecoratedBox>(borderlessHarness.RenderView));
        Assert.Null(Assert.IsType<BoxDecoration>(borderlessDecoration.DecorationValue).Border);
    }

    [Fact]
    public void Build_UsesCupertinoThemeForegroundDefaults()
    {
        var tabBar = new CupertinoTabBar(BuildItems(), currentIndex: 1);
        using var lightHarness = new CupertinoThemeTestHarness(Wrap(tabBar));
        lightHarness.Pump(ViewSize);
        Assert.Equal(0xFF999999u, Foreground(FindParagraph(lightHarness.RenderView, "First")!).ToUInt32());
        Assert.Equal(0xFF007AFFu, Foreground(FindParagraph(lightHarness.RenderView, "Second")!).ToUInt32());

        using var darkHarness = new CupertinoThemeTestHarness(Wrap(
            tabBar,
            brightness: PlatformBrightness.Dark));
        darkHarness.Pump(ViewSize);
        Assert.Equal(0xFF757575u, Foreground(FindParagraph(darkHarness.RenderView, "First")!).ToUInt32());
        Assert.Equal(0xFF0A84FFu, Foreground(FindParagraph(darkHarness.RenderView, "Second")!).ToUInt32());
    }

    [Fact]
    public void Build_AddsViewPaddingToHeightAndIgnoresKeyboardInsets()
    {
        var tabBar = new CupertinoTabBar(BuildItems(), height: 56.0);
        var media = new MediaQueryData(
            ViewPadding: new Thickness(0.0, 0.0, 0.0, 34.0),
            ViewInsets: new Thickness(0.0, 0.0, 0.0, 336.0));
        using var harness = new CupertinoThemeTestHarness(Wrap(tabBar, media: media, center: true));

        harness.Pump(ViewSize);

        RenderDecoratedBox decoration = Assert.Single(FindAll<RenderDecoratedBox>(harness.RenderView));
        Assert.Equal(90.0, decoration.Size.Height);
    }

    [Fact]
    public void Tap_ReportsTheTappedIndexWithoutChangingSelection()
    {
        int? tappedIndex = null;
        var tabBar = new CupertinoTabBar(
            BuildItems(),
            currentIndex: 1,
            onTap: index => tappedIndex = index);
        using var harness = new CupertinoThemeTestHarness(Wrap(tabBar));
        harness.Pump(ViewSize);

        DateTime now = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(
                pointer: 71,
                kind: PointerDeviceKind.Mouse,
                position: new Point(80.0, 100.0),
                buttons: PointerButtons.Primary,
                timestampUtc: now));
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerUpEvent(
                pointer: 71,
                kind: PointerDeviceKind.Mouse,
                position: new Point(80.0, 100.0),
                buttons: PointerButtons.None,
                timestampUtc: now.AddMilliseconds(16.0)));

        Assert.Equal(0, tappedIndex);
        Assert.Null(FindParagraph(harness.RenderView, "active-two"));
    }

    [Fact]
    public void Semantics_AnnounceIndexSelectionAndCustomLabel()
    {
        IReadOnlyList<BottomNavigationBarItem> items =
        [
            new BottomNavigationBarItem(
                icon: new Text("icon-one"),
                label: "A",
                semanticsLabel: "Custom A label"),
            new BottomNavigationBarItem(icon: new Text("icon-two"), label: "B"),
        ];
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoTabBar(items)));

        SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
        SemanticsNode first = Assert.IsType<SemanticsNode>(FindSemantics(root, "Custom A label"));
        SemanticsNode second = Assert.IsType<SemanticsNode>(FindSemantics(root, "B"));

        Assert.Equal("Tab 1 of 2", first.Hint);
        Assert.True(first.Flags.HasFlag(SemanticsFlags.HasSelectedState));
        Assert.True(first.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.Equal("Tab 2 of 2", second.Hint);
        Assert.True(second.Flags.HasFlag(SemanticsFlags.HasSelectedState));
        Assert.False(second.Flags.HasFlag(SemanticsFlags.IsSelected));
    }

    [Fact]
    public void LabelsMayBeNullAndWebItemsUseTheClickCursor()
    {
        PlatformDefaults.DebugIsWebOverride = true;
        int? tappedIndex = null;
        IReadOnlyList<BottomNavigationBarItem> items =
        [
            new BottomNavigationBarItem(new Text("first-icon"), "First"),
            new BottomNavigationBarItem(new Text("second-icon")),
        ];
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoTabBar(
            items,
            onTap: index => tappedIndex = index)));
        harness.Pump(ViewSize);

        Assert.Null(FindParagraph(harness.RenderView, "Second"));
        Assert.All(
            harness.FindWidgets<MouseRegion>(),
            region => Assert.Equal(SystemMouseCursors.Click, region.Cursor));

        DateTime now = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(
                pointer: 72,
                kind: PointerDeviceKind.Mouse,
                position: new Point(240.0, 100.0),
                buttons: PointerButtons.Primary,
                timestampUtc: now));
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerUpEvent(
                pointer: 72,
                kind: PointerDeviceKind.Mouse,
                position: new Point(240.0, 100.0),
                buttons: PointerButtons.None,
                timestampUtc: now.AddMilliseconds(16.0)));
        Assert.Equal(1, tappedIndex);
    }

    [Fact]
    public void ZeroArea_LaysOutWithoutCrashing()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new SizedBox(
                width: 0.0,
                height: 0.0,
                child: new CupertinoTabBar(BuildItems())),
            center: true));

        harness.Pump(ViewSize);

        RenderDecoratedBox decoration = Assert.Single(FindAll<RenderDecoratedBox>(harness.RenderView));
        Assert.Equal(default, decoration.Size);
    }

    private static IReadOnlyList<BottomNavigationBarItem> BuildItems(bool withActiveIcon = false)
    {
        return
        [
            new BottomNavigationBarItem(new Text("icon-one"), "First"),
            new BottomNavigationBarItem(
                icon: new Text("inactive-two"),
                label: "Second",
                activeIcon: withActiveIcon ? new Text("active-two") : null),
        ];
    }

    private static Widget Wrap(
        Widget child,
        PlatformBrightness brightness = PlatformBrightness.Light,
        MediaQueryData? media = null,
        bool center = true)
    {
        Widget content = center ? new Center(child: child) : child;
        return new MediaQuery(
            data: media ?? new MediaQueryData(PlatformBrightness: brightness),
            child: new Localizations(
                locale: new Locale("en", "US"),
                delegates:
                [
                    DefaultWidgetsLocalizations.Delegate,
                    DefaultCupertinoLocalizations.Delegate,
                ],
                child: new Directionality(
                    TextDirection.Ltr,
                    new CupertinoTheme(new CupertinoThemeData(brightness: brightness), content))));
    }

    private static Color Foreground(RenderParagraph paragraph)
    {
        return Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color;
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text)
    {
        return FindAll<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.PlainText == text);
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

    private static SemanticsNode? FindSemantics(SemanticsNode node, string label)
    {
        if (node.Label?.Split('\n').Contains(label) == true)
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            SemanticsNode? found = FindSemantics(child, label);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}
