using Avalonia;
using Avalonia.Media;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialNavigationSurfacesTests
{
    [Fact]
    public void NavigationBar_ValidatesDestinationCountIndexAndGeometry()
    {
        Assert.Throws<ArgumentException>(() => new NavigationBar(
            destinations: [Destination("One", Icons.Menu)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NavigationBar(
            destinations: BarDestinations(),
            selectedIndex: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NavigationBar(
            destinations: BarDestinations(),
            height: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NavigationBar(
            destinations: BarDestinations(),
            elevation: -1));
    }

    [Fact]
    public void NavigationBar_M3Defaults_UseSurfaceContainerAndSelectedTokens()
    {
        ColorScheme colors = ThemeData.Light.ColorScheme.CopyWith(
            surfaceContainer: Colors.DarkSlateBlue,
            secondaryContainer: Colors.DarkGreen,
            onSecondaryContainer: Colors.Gold,
            onSurface: Colors.Purple,
            onSurfaceVariant: Colors.CadetBlue);
        var theme = ThemeData.Light with
        {
            ColorScheme = colors,
        };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            BarHost(new NavigationBar(
                destinations: BarDestinations(),
                selectedIndex: 1))));

        harness.Pump(new Size(320, 160));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.DarkSlateBlue);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.DarkGreen);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.DarkGreen
                   && box.Decoration.BorderRadius?.Radius == 9999);
        Assert.Equal(colors.OnSurface,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "Two")!.Foreground).Color);
        Assert.Equal(Colors.CadetBlue,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "One")!.Foreground).Color);
        Assert.NotNull(FindParagraph(harness.RenderView, "selected-two"));
        Assert.Null(FindParagraph(harness.RenderView, "plain-two"));
    }

    [Fact]
    public void NavigationBar_ThemeAndWidgetOverrides_FollowWidgetThemeDefaultsPrecedence()
    {
        var theme = ThemeData.Light with
        {
            NavigationBarTheme = new NavigationBarThemeData(
                Height: 72,
                BackgroundColor: Colors.DarkGreen,
                IndicatorColor: Colors.Orange,
                LabelBehavior: NavigationDestinationLabelBehavior.AlwaysHide),
        };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            BarHost(new NavigationBar(
                destinations: BarDestinations(),
                backgroundColor: Colors.Purple,
                indicatorColor: Colors.Gold,
                height: 68,
                labelBehavior: NavigationDestinationLabelBehavior.AlwaysShow))));

        harness.Pump(new Size(320, 160));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.Purple);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.Gold);
        Assert.NotNull(FindParagraph(harness.RenderView, "One"));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinHeight == 68);
    }

    [Fact]
    public void NavigationSurfaces_M2Defaults_UseLegacyLabelGeometryAndIndicatorPolicy()
    {
        ColorScheme colors = ThemeData.Light.ColorScheme.CopyWith(
            secondary: Colors.DarkGreen,
            surface: Colors.DarkBlue,
            onSurface: Colors.Gold);
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            ColorScheme = colors,
        };
        using var barHarness = new WidgetRenderHarness(Wrap(
            theme,
            BarHost(new NavigationBar(destinations: BarDestinations()))));
        barHarness.Pump(new Size(320, 160));

        var expectedIndicator = NavigationSurfaceUtilities.WithOpacity(Colors.DarkGreen, 0.24);
        Color expectedBackground = ElevationOverlay.ColorWithOverlay(
            colors.Surface,
            colors.OnSurface,
            3.0);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(barHarness.RenderView),
            box => box.Decoration.Color == expectedBackground);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(barHarness.RenderView),
            box => box.Decoration.Color == expectedIndicator);
        Assert.Equal(11, FindParagraph(barHarness.RenderView, "One")!.FontSize);

        using var railHarness = new WidgetRenderHarness(Wrap(
            theme,
            RailHost(new NavigationRail(RailDestinations(), selectedIndex: 0))));
        railHarness.Pump(new Size(420, 320));

        Assert.Contains(FindDescendants<RenderConstrainedBox>(railHarness.RenderView),
            box => box.AdditionalConstraints.MinWidth == 72);
        Assert.DoesNotContain(FindDescendants<RenderDecoratedBox>(railHarness.RenderView),
            box => box.Decoration.Color == theme.ColorScheme.SecondaryContainer);
    }

    [Fact]
    public void NavigationBarThemeData_CopyWithAndLerp_CoverEveryStatefulField()
    {
        var begin = new NavigationBarThemeData(
            Height: 60,
            BackgroundColor: Colors.Black,
            Elevation: 0,
            ShadowColor: Colors.Black,
            SurfaceTintColor: Colors.Black,
            IndicatorColor: Colors.Black,
            IndicatorShape: new RoundedRectangleBorder(
                new BorderSide(Colors.Black, 1), Plumix.Rendering.BorderRadius.Circular(4)),
            LabelTextStyle: MaterialStateProperty<TextStyle?>.All(
                new TextStyle(Color: Colors.Black, FontSize: 10)),
            IconTheme: MaterialStateProperty<IconThemeData?>.All(
                new IconThemeData(Color: Colors.Black, Size: 16)),
            LabelBehavior: NavigationDestinationLabelBehavior.AlwaysHide,
            OverlayColor: MaterialStateProperty<Color?>.All(Colors.Black),
            LabelPadding: new Thickness(0, 2, 0, 0));
        NavigationBarThemeData end = begin.CopyWith(
            height: 80,
            backgroundColor: Colors.White,
            elevation: 4,
            shadowColor: Colors.White,
            surfaceTintColor: Colors.White,
            indicatorColor: Colors.White,
            indicatorShape: new RoundedRectangleBorder(
                new BorderSide(Colors.White, 3), Plumix.Rendering.BorderRadius.Circular(12)),
            labelTextStyle: MaterialStateProperty<TextStyle?>.All(
                new TextStyle(Color: Colors.White, FontSize: 20)),
            iconTheme: MaterialStateProperty<IconThemeData?>.All(
                new IconThemeData(Color: Colors.White, Size: 24)),
            labelBehavior: NavigationDestinationLabelBehavior.AlwaysShow,
            overlayColor: MaterialStateProperty<Color?>.All(Colors.White),
            labelPadding: new Thickness(0, 6, 0, 0));

        NavigationBarThemeData midpoint = NavigationBarThemeData.Lerp(begin, end, 0.5)!;
        MaterialState states = MaterialState.Selected | MaterialState.Hovered;

        Assert.Equal(70, midpoint.Height);
        Assert.Equal(2, midpoint.Elevation);
        Assert.Equal(Color.FromRgb(127, 127, 127), midpoint.BackgroundColor);
        Assert.Equal(8, ShapeBorderGeometry.ResolveRadius(midpoint.IndicatorShape).Radius);
        Assert.Equal(2, ShapeBorderGeometry.SideOrNone(midpoint.IndicatorShape).Width);
        Assert.Equal(15, midpoint.LabelTextStyle!.Resolve(states)!.FontSize);
        Assert.Equal(20, midpoint.IconTheme!.Resolve(states)!.Size);
        Assert.Equal(Color.FromRgb(127, 127, 127), midpoint.OverlayColor!.Resolve(states));
        Assert.Equal(new Thickness(0, 4, 0, 0), midpoint.LabelPadding);
        Assert.Equal(NavigationDestinationLabelBehavior.AlwaysShow, midpoint.LabelBehavior);
    }

    [Fact]
    public void ThemeData_Lerp_InterpolatesNavigationBarTheme()
    {
        var begin = ThemeData.Light with
        {
            NavigationBarTheme = new NavigationBarThemeData(
                Height: 60,
                IndicatorColor: Colors.Black),
        };
        var end = ThemeData.Dark with
        {
            NavigationBarTheme = new NavigationBarThemeData(
                Height: 80,
                IndicatorColor: Colors.White),
        };

        ThemeData midpoint = ThemeData.Lerp(begin, end, 0.5);

        Assert.Equal(70, midpoint.NavigationBarTheme.Height);
        Assert.Equal(Color.FromRgb(127, 127, 127), midpoint.NavigationBarTheme.IndicatorColor);
    }

    [Fact]
    public void NavigationBar_TapAndDisabledState_MatchDestinationContract()
    {
        int? selected = null;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            BarHost(new NavigationBar(
                destinations:
                [
                    Destination("One", Icons.Menu),
                    new NavigationDestination(new Text("disabled"), "Two", enabled: false),
                ],
                onDestinationSelected: index => selected = index))));
        harness.Pump(new Size(320, 160));

        Tap(harness.RenderView, new Point(80, 120), 100);
        Assert.Equal(0, selected);
        selected = null;
        Tap(harness.RenderView, new Point(240, 120), 101);
        Assert.Null(selected);
    }

    [Fact]
    public void NavigationBar_SemanticsExposeSelectionEnabledStateAndLocalizedIndex()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            BarHost(new NavigationBar(
                destinations: BarDestinations(),
                selectedIndex: 1,
                onDestinationSelected: _ => { }))));

        var root = harness.PumpAndGetSemantics(new Size(320, 160));

        // Dart's destination is `Semantics(enabled, button)` over a `Stack` holding the ink well and
        // a sibling `Semantics(label: tabLabel)`; the ink well keeps its own node in Plumix (see the
        // `InkResponse`/`Focus` row in `docs/ai/DIVERGENCES.md`).
        var selected = FindSemantics(
            root,
            node => node.Label?.Contains("Tab 2 of 2", StringComparison.Ordinal) == true);
        Assert.NotNull(selected);
        Assert.True(selected!.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.True(selected.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.True(selected.Flags.HasFlag(SemanticsFlags.IsButton));

        var tappable = FindSemantics(selected, node => node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(tappable);
        Assert.Contains("Two", tappable!.Label, StringComparison.Ordinal);
    }

    [Fact]
    public void NavigationBar_CarriesTheTabBarAndTabRolesDartDeclares()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            BarHost(new NavigationBar(
                destinations: BarDestinations(),
                selectedIndex: 1,
                onDestinationSelected: _ => { }))));

        var root = harness.PumpAndGetSemantics(new Size(320, 160));

        var bar = FindSemantics(root, node => node.Role == SemanticsRole.TabBar);
        Assert.NotNull(bar);

        var tabs = new List<SemanticsNode>();
        CollectSemantics(bar!, node => node.Role == SemanticsRole.Tab, tabs);
        Assert.Equal(2, tabs.Count);

        // The role does not fold into the flag set, so the destination still merges into one node
        // carrying its own button/selected flags and the localized index label.
        Assert.True(tabs[1].Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.True(tabs[1].Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.Contains("Tab 2 of 2", tabs[1].Label, StringComparison.Ordinal);
    }

    [Fact]
    public void NavigationBar_IndicatorInkRectTracksTheKeyedIconLayout()
    {
        Rect[] alwaysShow = NavigationBarInkRects(NavigationDestinationLabelBehavior.AlwaysShow);
        Rect[] alwaysHide = NavigationBarInkRects(NavigationDestinationLabelBehavior.AlwaysHide);
        Rect[] selectedOnly = NavigationBarInkRects(NavigationDestinationLabelBehavior.OnlyShowSelected);

        Assert.All(alwaysShow, rect => Assert.Equal(new Size(64.0, 32.0), rect.Size));
        Assert.All(alwaysHide, rect => Assert.Equal(new Size(64.0, 32.0), rect.Size));
        Assert.All(selectedOnly, rect => Assert.Equal(new Size(64.0, 32.0), rect.Size));
        Assert.True(alwaysShow[0].Y < alwaysHide[0].Y);
        Assert.Equal(alwaysShow[0].Y, selectedOnly[0].Y, precision: 3);
        Assert.Equal(alwaysHide[1].Y, selectedOnly[1].Y, precision: 3);
        Assert.Equal(alwaysShow[0].X, alwaysHide[0].X, precision: 3);
    }

    private static void CollectSemantics(
        SemanticsNode node,
        Func<SemanticsNode, bool> predicate,
        List<SemanticsNode> into)
    {
        if (predicate(node))
        {
            into.Add(node);
        }

        foreach (SemanticsNode child in node.Children)
        {
            CollectSemantics(child, predicate, into);
        }
    }

    [Fact]
    public void NavigationDrawer_ValidatesGeometryAndDestinationContract()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NavigationDrawer(
            children: DrawerDestinations(),
            elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NavigationDrawer(
            children: DrawerDestinations(),
            elevation: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NavigationDrawer(
            children: DrawerDestinations(),
            tilePadding: new Thickness(-1, 0, 0, 0)));

        var destination = new NavigationDrawerDestination(
            icon: new Icon(Icons.Menu),
            label: new Text("Home"));
        Assert.True(destination.Enabled);

        using var zeroSizeHarness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new NavigationDrawer(children: DrawerDestinations())));
        zeroSizeHarness.Pump(new Size(0, 0));
    }

    [Fact]
    public void NavigationDrawer_M3Defaults_UseSourceTokensAndSelectedVisuals()
    {
        ColorScheme colors = ThemeData.Light.ColorScheme.CopyWith(
            surfaceContainerLow: Colors.DarkSlateBlue,
            secondaryContainer: Colors.DarkGreen,
            onSecondaryContainer: Colors.Gold,
            onSurfaceVariant: Colors.CadetBlue);
        var theme = ThemeData.Light with
        {
            ColorScheme = colors,
        };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new NavigationDrawer(
                children: DrawerDestinations(),
                selectedIndex: 1)));

        harness.Pump(new Size(420, 320));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.DarkSlateBlue);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.DarkGreen);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.DarkGreen
                   && box.Decoration.BorderRadius?.Radius == 9999);
        // Dart paints a destination's `backgroundColor` with `Ink`, so it lands on the drawer's
        // material as an ink decoration rather than as a `DecoratedBox`.
        Assert.Contains(FindDescendants<RenderInkDecoration>(harness.RenderView),
            ink => (ink.Decoration as BoxDecoration)?.Color == Colors.Yellow);
        Assert.Equal(Colors.CadetBlue,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "Home")!.Foreground).Color);
        Assert.Equal(Colors.Gold,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "Explore")!.Foreground).Color);
        Assert.NotNull(FindParagraph(harness.RenderView, "selected-explore"));
        Assert.Null(FindParagraph(harness.RenderView, "plain-explore"));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinHeight == 56
                   && box.AdditionalConstraints.MaxHeight == 56);
    }

    [Fact]
    public void NavigationDrawerThemeData_CopyWithAndLerp_CoverSourceFields()
    {
        var begin = new NavigationDrawerThemeData(
            TileHeight: 48,
            BackgroundColor: Colors.Black,
            Elevation: 0,
            ShadowColor: Colors.Black,
            SurfaceTintColor: Colors.Black,
            IndicatorColor: Colors.Black,
            IndicatorShape: new RoundedRectangleBorder(
                new BorderSide(Colors.Black, 1), Plumix.Rendering.BorderRadius.Circular(4)),
            IndicatorSize: new Size(200, 40),
            LabelTextStyle: MaterialStateProperty<TextStyle?>.All(
                new TextStyle(Color: Colors.Black, FontSize: 10)),
            IconTheme: MaterialStateProperty<IconThemeData?>.All(
                new IconThemeData(Color: Colors.Black, Size: 16)));
        NavigationDrawerThemeData end = begin.CopyWith(
            tileHeight: 64,
            backgroundColor: Colors.White,
            elevation: 4,
            shadowColor: Colors.White,
            surfaceTintColor: Colors.White,
            indicatorColor: Colors.White,
            indicatorShape: new RoundedRectangleBorder(
                new BorderSide(Colors.White, 3), Plumix.Rendering.BorderRadius.Circular(12)),
            indicatorSize: new Size(300, 60),
            labelTextStyle: MaterialStateProperty<TextStyle?>.All(
                new TextStyle(Color: Colors.White, FontSize: 20)),
            iconTheme: MaterialStateProperty<IconThemeData?>.All(
                new IconThemeData(Color: Colors.White, Size: 24)));

        NavigationDrawerThemeData midpoint = NavigationDrawerThemeData.Lerp(begin, end, 0.5)!;
        MaterialState states = MaterialState.Selected | MaterialState.Hovered;

        Assert.Equal(56, midpoint.TileHeight);
        Assert.Equal(Color.FromRgb(127, 127, 127), midpoint.BackgroundColor);
        Assert.Equal(2, midpoint.Elevation);
        Assert.Equal(Color.FromRgb(127, 127, 127), midpoint.ShadowColor);
        Assert.Equal(Color.FromRgb(127, 127, 127), midpoint.SurfaceTintColor);
        Assert.Equal(Color.FromRgb(127, 127, 127), midpoint.IndicatorColor);
        Assert.Equal(8, ShapeBorderGeometry.ResolveRadius(midpoint.IndicatorShape).Radius);
        Assert.Equal(2, ShapeBorderGeometry.SideOrNone(midpoint.IndicatorShape).Width);
        Assert.Equal(begin.IndicatorSize, midpoint.IndicatorSize);
        Assert.Equal(15, midpoint.LabelTextStyle!.Resolve(states)!.FontSize);
        Assert.Equal(20, midpoint.IconTheme!.Resolve(states)!.Size);
    }

    [Fact]
    public void ThemeData_Lerp_InterpolatesNavigationDrawerTheme()
    {
        var begin = ThemeData.Light with
        {
            NavigationDrawerTheme = new NavigationDrawerThemeData(
                TileHeight: 48,
                IndicatorColor: Colors.Black),
        };
        var end = ThemeData.Dark with
        {
            NavigationDrawerTheme = new NavigationDrawerThemeData(
                TileHeight: 64,
                IndicatorColor: Colors.White),
        };

        ThemeData midpoint = ThemeData.Lerp(begin, end, 0.5);

        Assert.Equal(56, midpoint.NavigationDrawerTheme.TileHeight);
        Assert.Equal(Color.FromRgb(127, 127, 127), midpoint.NavigationDrawerTheme.IndicatorColor);
    }

    [Fact]
    public void NavigationDrawer_WidgetLocalAndGlobalThemePrecedenceIsApplied()
    {
        var global = ThemeData.Light with
        {
            NavigationDrawerTheme = new NavigationDrawerThemeData(
                BackgroundColor: Colors.DarkRed,
                IndicatorColor: Colors.Orange,
                TileHeight: 64,
                IndicatorSize: new Size(250, 44)),
        };
        var local = new NavigationDrawerThemeData(
            BackgroundColor: Colors.DarkGreen,
            IndicatorColor: Colors.Purple,
            TileHeight: 60,
            IndicatorSize: new Size(260, 48),
            LabelTextStyle: MaterialStateProperty<TextStyle?>.All(
                new TextStyle(Color: Colors.CadetBlue, FontSize: 13)),
            IconTheme: MaterialStateProperty<IconThemeData?>.All(
                new IconThemeData(Color: Colors.Gold, Size: 21)));
        using var harness = new WidgetRenderHarness(Wrap(
            global,
            new NavigationDrawerTheme(
                data: local,
                child: new NavigationDrawer(
                    children: DrawerDestinations(),
                    backgroundColor: Colors.DarkBlue,
                    indicatorColor: Colors.Yellow,
                    selectedIndex: 0))));

        harness.Pump(new Size(420, 320));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.DarkBlue);
        // Dart paints a destination's `backgroundColor` with `Ink`, so it lands on the drawer's
        // material as an ink decoration rather than as a `DecoratedBox`.
        Assert.Contains(FindDescendants<RenderInkDecoration>(harness.RenderView),
            ink => (ink.Decoration as BoxDecoration)?.Color == Colors.Yellow);
        Assert.Equal(Colors.CadetBlue,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "Home")!.Foreground).Color);
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinHeight == 60
                   && box.AdditionalConstraints.MaxHeight == 60);
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinWidth == 260);
    }

    [Fact]
    public void NavigationDrawer_SurfaceTintFlowsThroughDrawerMaterial()
    {
        var background = Colors.White;
        var tint = Colors.DarkRed;
        var expected = MaterialSurface.ApplySurfaceTint(background, tint, 1);
        var theme = ThemeData.Light with
        {
            NavigationDrawerTheme = new NavigationDrawerThemeData(
                BackgroundColor: background,
                SurfaceTintColor: tint,
                Elevation: 1),
        };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new NavigationDrawer(children: DrawerDestinations())));

        harness.Pump(new Size(420, 320));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == expected);
    }

    [Fact]
    public void NavigationDrawer_IndexingTapDisabledAndSemanticsMatchFlutter()
    {
        int? selected = null;
        ColorScheme colors = ThemeData.Light.ColorScheme.CopyWith(
            onSurfaceVariant: Colors.DarkGreen);
        var theme = ThemeData.Light with
        {
            ColorScheme = colors,
        };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new NavigationDrawer(
                header: new Text("Header"),
                footer: new Text("Footer"),
                selectedIndex: 1,
                onDestinationSelected: index => selected = index,
                children:
                [
                    new Text("Section"),
                    new NavigationDrawerDestination(
                        icon: new Icon(Icons.Menu),
                        label: new Text("Home")),
                    new Divider(),
                    new NavigationDrawerDestination(
                        icon: new Icon(Icons.InfoOutline),
                        label: new Text("Explore")),
                    new NavigationDrawerDestination(
                        icon: new Icon(Icons.Close),
                        label: new Text("Disabled"),
                        enabled: false),
                ])));
        harness.Pump(new Size(420, 360));

        Assert.NotNull(FindParagraph(harness.RenderView, "Header"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Footer"));
        Tap(harness.RenderView, new Point(150, 92), 104);
        Assert.Equal(0, selected);
        selected = null;
        Tap(harness.RenderView, new Point(150, 224), 105);
        Assert.Null(selected);

        var root = harness.PumpAndGetSemantics(new Size(420, 360));
        var selectedNode = FindSemantics(root,
            node => node.Label?.Contains("Explore\nTab 2 of 3", StringComparison.Ordinal) == true);
        Assert.NotNull(selectedNode);
        Assert.True(selectedNode!.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.False(selectedNode.Flags.HasFlag(SemanticsFlags.HasEnabledState));
        Assert.True(selectedNode.Actions.HasFlag(SemanticsActions.Tap));

        var disabledNode = FindSemantics(root,
            node => node.Label?.Contains("Disabled\nTab 3 of 3", StringComparison.Ordinal) == true);
        Assert.NotNull(disabledNode);
        Assert.False(disabledNode!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.False(disabledNode.Flags.HasFlag(SemanticsFlags.HasEnabledState));
        Assert.False(disabledNode.Actions.HasFlag(SemanticsActions.Tap));
        Color disabledColor = NavigationSurfaceUtilities.WithOpacity(colors.OnSurfaceVariant, 0.38);
        Assert.Equal(disabledColor,
            Assert.IsType<SolidColorBrush>(
                FindParagraph(harness.RenderView, "Disabled")!.Foreground).Color);
    }

    [Fact]
    public void NavigationRail_ValidatesIndexGeometryAndExtendedLabelContract()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NavigationRail(RailDestinations(), selectedIndex: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NavigationRail(RailDestinations(), selectedIndex: 0, elevation: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NavigationRail(RailDestinations(), selectedIndex: 0, groupAlignment: 1.1));
        Assert.Throws<ArgumentException>(() => new NavigationRail(
            RailDestinations(),
            selectedIndex: 0,
            extended: true,
            labelType: NavigationRailLabelType.All));
        Assert.Throws<ArgumentException>(() => new NavigationRail(
            RailDestinations(),
            selectedIndex: 0,
            minWidth: 100,
            minExtendedWidth: 90));
    }

    [Fact]
    public void NavigationRail_M3Defaults_UseIndicatorAndEightyPixelWidth()
    {
        ColorScheme colors = ThemeData.Light.ColorScheme.CopyWith(
            surface: Colors.DarkSlateBlue,
            secondaryContainer: Colors.DarkGreen,
            onSecondaryContainer: Colors.Gold,
            onSurface: Colors.Purple,
            onSurfaceVariant: Colors.CadetBlue);
        var theme = ThemeData.Light with
        {
            ColorScheme = colors,
        };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            RailHost(new NavigationRail(RailDestinations(), selectedIndex: 0))));

        harness.Pump(new Size(420, 320));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.DarkSlateBlue);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.DarkGreen);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.DarkGreen
                   && box.Decoration.BorderRadius?.Radius == 9999);
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinWidth == 80);
        Assert.NotNull(FindParagraph(harness.RenderView, "rail-selected-one"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Rail one"));
        Assert.Contains(FindDescendants<RenderVisibility>(harness.RenderView), visibility => !visibility.Visible);
    }

    [Fact]
    public void NavigationRail_M3IndicatorInkRectMatchesLabelAndCompactGeometry()
    {
        Rect[] allLabels = NavigationRailInkRects(new NavigationRail(
            CompactRailDestinations(),
            selectedIndex: 0,
            labelType: NavigationRailLabelType.All));
        Rect[] compact = NavigationRailInkRects(new NavigationRail(
            CompactRailDestinations(),
            selectedIndex: 0,
            minWidth: 50.0));

        Assert.All(allLabels, rect => Assert.Equal(new Rect(12.0, 0.0, 56.0, 32.0), rect));
        Assert.All(compact, rect => Assert.Equal(new Rect(-3.0, 6.0, 56.0, 32.0), rect));
    }

    [Fact]
    public void NavigationRail_UsesRailLevelIndicatorTokensLikePinnedDart()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            RailHost(new NavigationRail(
                destinations:
                [
                    new NavigationRailDestination(
                        new Icon(Icons.Menu),
                        new Text("One"),
                        indicatorColor: Colors.Yellow),
                    new NavigationRailDestination(new Icon(Icons.InfoOutline), new Text("Two")),
                ],
                selectedIndex: 0,
                indicatorColor: Colors.DarkGreen))));

        harness.Pump(new Size(420, 320));

        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.DarkGreen);
        Assert.DoesNotContain(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.Yellow);
    }

    [Theory]
    [InlineData(TextDirection.Ltr, 12.0)]
    [InlineData(TextDirection.Rtl, 188.0)]
    public void NavigationRail_ExtendedIndicatorInkRectTracksDirection(
        TextDirection textDirection,
        double expectedLeft)
    {
        Rect[] rects = NavigationRailInkRects(
            new NavigationRail(
                RailDestinations(),
                selectedIndex: 0,
                extended: true),
            textDirection);

        Assert.All(rects, rect => Assert.Equal(new Rect(expectedLeft, 6.0, 56.0, 32.0), rect));
    }

    [Fact]
    public void NavigationRail_LargeIconOffsetsIndicatorInkRectVertically()
    {
        Rect[] rects = NavigationRailInkRects(new NavigationRail(
            destinations:
            [
                new NavigationRailDestination(new Icon(Icons.Menu), new Text("One")),
                new NavigationRailDestination(new Icon(Icons.InfoOutline), new Text("Two")),
            ],
            selectedIndex: 0,
            selectedIconTheme: new IconThemeData(Size: 50.0),
            unselectedIconTheme: new IconThemeData(Size: 50.0)));

        Assert.All(rects, rect => Assert.Equal(new Rect(12.0, 15.0, 56.0, 32.0), rect));
    }

    [Fact]
    public void NavigationRail_M2Defaults_UseColorSchemeAndPreserveDefaultUnselectedIconOpacity()
    {
        ColorScheme colors = ThemeData.Light.ColorScheme.CopyWith(
            primary: Colors.DarkRed,
            surface: Colors.DarkBlue,
            onSurface: Colors.DarkGreen);
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            ColorScheme = colors,
            NavigationRailTheme = new NavigationRailThemeData(
                UnselectedIconTheme: new IconThemeData(Color: Colors.CadetBlue, Size: 24)),
        };
        IReadOnlyList<NavigationRailDestination> destinations =
        [
            new NavigationRailDestination(
                icon: new Icon(Icons.Menu),
                selectedIcon: new Icon(Icons.Menu),
                label: new Text("Rail one")),
            new NavigationRailDestination(
                icon: new Icon(Icons.InfoOutline),
                label: new Text("Rail two")),
        ];
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            RailHost(new NavigationRail(
                destinations,
                selectedIndex: 0,
                labelType: NavigationRailLabelType.All))));

        harness.Pump(new Size(420, 320));

        Color unselectedLabelColor = NavigationSurfaceUtilities.WithOpacity(colors.OnSurface, 0.64);
        Color unselectedIconColor = NavigationSurfaceUtilities.WithOpacity(Colors.CadetBlue, 0.64);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == colors.Surface);
        Assert.Equal(colors.Primary,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "Rail one")!.Foreground).Color);
        Assert.Equal(unselectedLabelColor,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "Rail two")!.Foreground).Color);
        Assert.Equal(colors.Primary,
            Assert.IsType<SolidColorBrush>(
                FindParagraph(harness.RenderView, char.ConvertFromUtf32(Icons.Menu.CodePoint))!.Foreground).Color);
        Assert.Equal(unselectedIconColor,
            Assert.IsType<SolidColorBrush>(
                FindParagraph(
                    harness.RenderView,
                    char.ConvertFromUtf32(Icons.InfoOutline.CodePoint))!.Foreground).Color);
    }

    [Fact]
    public void NavigationRailThemeData_CopyWithAndLerp_CoverEveryField()
    {
        var begin = new NavigationRailThemeData(
            BackgroundColor: Colors.Black,
            Elevation: 0,
            UnselectedLabelTextStyle: new TextStyle(Color: Colors.Black, FontSize: 10),
            SelectedLabelTextStyle: new TextStyle(Color: Colors.Black, FontSize: 12),
            UnselectedIconTheme: new IconThemeData(Color: Colors.Black, Size: 16, Opacity: 0.4),
            SelectedIconTheme: new IconThemeData(Color: Colors.Black, Size: 18, Opacity: 0.6),
            GroupAlignment: -1,
            LabelType: NavigationRailLabelType.None,
            UseIndicator: false,
            IndicatorColor: Colors.Black,
            IndicatorShape: new RoundedRectangleBorder(
                new BorderSide(Colors.Black, 1), Plumix.Rendering.BorderRadius.Circular(4)),
            MinWidth: 60,
            MinExtendedWidth: 200);
        NavigationRailThemeData end = begin.CopyWith(
            backgroundColor: Colors.White,
            elevation: 4,
            unselectedLabelTextStyle: new TextStyle(Color: Colors.White, FontSize: 20),
            selectedLabelTextStyle: new TextStyle(Color: Colors.White, FontSize: 22),
            unselectedIconTheme: new IconThemeData(Color: Colors.White, Size: 24, Opacity: 0.8),
            selectedIconTheme: new IconThemeData(Color: Colors.White, Size: 26, Opacity: 1.0),
            groupAlignment: 1,
            labelType: NavigationRailLabelType.All,
            useIndicator: true,
            indicatorColor: Colors.White,
            indicatorShape: new RoundedRectangleBorder(
                new BorderSide(Colors.White, 3), Plumix.Rendering.BorderRadius.Circular(12)),
            minWidth: 80,
            minExtendedWidth: 280);

        NavigationRailThemeData midpoint = NavigationRailThemeData.Lerp(begin, end, 0.5)!;

        Assert.Equal(Color.FromRgb(127, 127, 127), midpoint.BackgroundColor);
        Assert.Equal(2, midpoint.Elevation);
        Assert.Equal(15, midpoint.UnselectedLabelTextStyle!.FontSize);
        Assert.Equal(17, midpoint.SelectedLabelTextStyle!.FontSize);
        Assert.Equal(20, midpoint.UnselectedIconTheme!.Size);
        Assert.Equal(22, midpoint.SelectedIconTheme!.Size);
        Assert.Equal(0, midpoint.GroupAlignment);
        Assert.Equal(NavigationRailLabelType.All, midpoint.LabelType);
        Assert.True(midpoint.UseIndicator);
        Assert.Equal(Color.FromRgb(127, 127, 127), midpoint.IndicatorColor);
        Assert.Equal(8, ShapeBorderGeometry.ResolveRadius(midpoint.IndicatorShape).Radius);
        Assert.Equal(70, midpoint.MinWidth);
        Assert.Equal(240, midpoint.MinExtendedWidth);
    }

    [Fact]
    public void ThemeData_Lerp_InterpolatesNavigationRailTheme()
    {
        var begin = ThemeData.Light with
        {
            NavigationRailTheme = new NavigationRailThemeData(
                MinWidth: 60,
                IndicatorColor: Colors.Black),
        };
        var end = ThemeData.Dark with
        {
            NavigationRailTheme = new NavigationRailThemeData(
                MinWidth: 80,
                IndicatorColor: Colors.White),
        };

        ThemeData midpoint = ThemeData.Lerp(begin, end, 0.5);

        Assert.Equal(70, midpoint.NavigationRailTheme.MinWidth);
        Assert.Equal(Color.FromRgb(127, 127, 127), midpoint.NavigationRailTheme.IndicatorColor);
    }

    [Fact]
    public void NavigationRail_ExtendedAndLabelModesExposeLabelsAndThemeGeometry()
    {
        var theme = ThemeData.Light with
        {
            NavigationRailTheme = new NavigationRailThemeData(
                MinWidth: 76,
                MinExtendedWidth: 240,
                IndicatorColor: Colors.Orange,
                SelectedLabelTextStyle: new TextStyle(Color: Colors.Purple, FontSize: 13)),
        };
        using var extendedHarness = new WidgetRenderHarness(Wrap(
            theme,
            RailHost(new NavigationRail(
                RailDestinations(),
                selectedIndex: 0,
                extended: true))));
        extendedHarness.Pump(new Size(420, 320));

        Assert.NotNull(FindParagraph(extendedHarness.RenderView, "Rail one"));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(extendedHarness.RenderView),
            box => box.AdditionalConstraints.MinWidth == 240);

        using var allLabelsHarness = new WidgetRenderHarness(Wrap(
            theme,
            RailHost(new NavigationRail(
                RailDestinations(),
                selectedIndex: 0,
                labelType: NavigationRailLabelType.All,
                indicatorColor: Colors.Gold))));
        allLabelsHarness.Pump(new Size(420, 320));
        Assert.NotNull(FindParagraph(allLabelsHarness.RenderView, "Rail one"));
        Assert.NotNull(FindParagraph(allLabelsHarness.RenderView, "Rail two"));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(allLabelsHarness.RenderView),
            box => box.Decoration.Color == Colors.Gold);
    }

    [Fact]
    public void NavigationRail_TapDisabledAndSemanticsStatesAreWired()
    {
        int? selected = null;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            RailHost(new NavigationRail(
                destinations:
                [
                    RailDestination("Rail one", "rail-one", "rail-selected-one"),
                    new NavigationRailDestination(new Text("disabled"), new Text("Rail two"), disabled: true),
                ],
                selectedIndex: 0,
                onDestinationSelected: index => selected = index,
                labelType: NavigationRailLabelType.All))));
        harness.Pump(new Size(420, 320));

        Tap(harness.RenderView, new Point(40, 35), 102);
        Assert.Equal(0, selected);
        selected = null;
        Tap(harness.RenderView, new Point(40, 110), 103);
        Assert.Null(selected);

        var root = harness.PumpAndGetSemantics(new Size(420, 320));
        var first = FindSemantics(root, node => HasLabelPart(node, "Tab 1 of 2"));
        Assert.NotNull(first);
        Assert.True(first!.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.False(first.Flags.HasFlag(SemanticsFlags.HasEnabledState));
    }

    private static IReadOnlyList<NavigationDestination> BarDestinations() =>
    [
        Destination("One", Icons.Menu),
        new NavigationDestination(
            icon: new Text("plain-two"),
            selectedIcon: new Text("selected-two"),
            label: "Two"),
    ];

    private static NavigationDestination Destination(string label, IconData icon) =>
        new(new Icon(icon), label);

    private static IReadOnlyList<Widget> DrawerDestinations() =>
    [
        new NavigationDrawerDestination(
            icon: new Icon(Icons.Menu),
            label: new Text("Home")),
        new NavigationDrawerDestination(
            icon: new Text("plain-explore"),
            selectedIcon: new Text("selected-explore"),
            label: new Text("Explore"),
            backgroundColor: Colors.Yellow),
    ];

    private static IReadOnlyList<NavigationRailDestination> RailDestinations() =>
    [
        RailDestination("Rail one", "rail-one", "rail-selected-one"),
        RailDestination("Rail two", "rail-two", "rail-selected-two"),
    ];

    private static IReadOnlyList<NavigationRailDestination> CompactRailDestinations() =>
    [
        new NavigationRailDestination(new Icon(Icons.Menu), new Text("A")),
        new NavigationRailDestination(new Icon(Icons.InfoOutline), new Text("B")),
    ];

    private static NavigationRailDestination RailDestination(string label, string icon, string selectedIcon) =>
        new(new Text(icon), new Text(label), new Text(selectedIcon));

    private static Widget Wrap(ThemeData theme, Widget child) =>
        new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(Size: new Size(420, 320)),
                new Theme(theme, child)));

    private static Widget BarHost(Widget bar) => new Column(children: [new Expanded(new SizedBox()), bar]);

    private static Widget RailHost(Widget rail) => new Row(children: [rail, new Expanded(new SizedBox())]);

    private static Rect[] NavigationBarInkRects(NavigationDestinationLabelBehavior labelBehavior)
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            BarHost(new NavigationBar(
                destinations:
                [
                    Destination("One", Icons.Menu),
                    Destination("Two", Icons.InfoOutline),
                ],
                selectedIndex: 0,
                labelBehavior: labelBehavior))));
        harness.Pump(new Size(320, 160));
        return FindDescendants<RenderInkResponsePaint>(harness.RenderView)
            .Select(paint => paint.ResolvedInkRect)
            .ToArray();
    }

    private static Rect[] NavigationRailInkRects(
        NavigationRail rail,
        TextDirection textDirection = TextDirection.Ltr)
    {
        Widget content = new MediaQuery(
            new MediaQueryData(Size: new Size(420, 320)),
            new Theme(ThemeData.Light, RailHost(rail)));
        using var harness = new WidgetRenderHarness(new Directionality(textDirection, content));
        harness.Pump(new Size(420, 320));
        return FindDescendants<RenderInkResponsePaint>(harness.RenderView)
            .Select(paint => paint.ResolvedInkRect)
            .ToArray();
    }

    private static void Tap(RenderView view, Point point, int pointer)
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            var now = DateTime.UtcNow;
            binding.HandlePointerEvent(view, new PointerDownEvent(pointer, PointerDeviceKind.Mouse, point, PointerButtons.Primary, now));
            binding.HandlePointerEvent(view, new PointerUpEvent(pointer, PointerDeviceKind.Mouse, point, PointerButtons.None, now.AddMilliseconds(16)));
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.PlainText == text);

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T value) result.Add(value);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static SemanticsNode? FindSemantics(SemanticsNode? root, Func<SemanticsNode, bool> predicate)
    {
        if (root is null) return null;
        if (predicate(root)) return root;
        foreach (var child in root.Children)
        {
            var match = FindSemantics(child, predicate);
            if (match is not null) return match;
        }

        return null;
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner!.RootNode;
        }

        public void Dispose() => _rootElement.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) => _renderView = renderView;
            public override RenderObject? RenderObject => _child?.RenderObject;
            internal override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }

    /// <summary>
    /// Whether one of the node's merged label parts is <paramref name="part"/>. A merged node joins
    /// the labels it absorbed with a newline, exactly like Flutter's <c>_concatAttributedString</c>.
    /// </summary>
    private static bool HasLabelPart(SemanticsNode node, string part) =>
        node.Label?.Split('\n').Contains(part) == true;
}
