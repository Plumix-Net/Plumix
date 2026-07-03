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
        var theme = ThemeData.Light with
        {
            SurfaceContainerColor = Colors.DarkSlateBlue,
            SecondaryContainerColor = Colors.DarkGreen,
            OnSecondaryContainerColor = Colors.Gold,
            OnSurfaceVariantColor = Colors.CadetBlue,
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
        Assert.Equal(theme.OnSurfaceColor,
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
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            SecondaryColor = Colors.DarkGreen,
        };
        using var barHarness = new WidgetRenderHarness(Wrap(
            theme,
            BarHost(new NavigationBar(destinations: BarDestinations()))));
        barHarness.Pump(new Size(320, 160));

        var expectedIndicator = NavigationSurfaceUtilities.WithOpacity(Colors.DarkGreen, 0.24);
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
            box => box.Decoration.Color == theme.SecondaryContainerColor);
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

        var selected = FindSemantics(root, node => node.Label?.Contains("Two, Tab 2 of 2", StringComparison.Ordinal) == true);
        Assert.NotNull(selected);
        Assert.True(selected!.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.True(selected.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.True(selected.Actions.HasFlag(SemanticsActions.Tap));
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
        var theme = ThemeData.Light with
        {
            SurfaceContainerLowColor = Colors.DarkSlateBlue,
            SecondaryContainerColor = Colors.DarkGreen,
            OnSecondaryContainerColor = Colors.Gold,
            OnSurfaceVariantColor = Colors.CadetBlue,
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
            box => box.Decoration.Color == Colors.Yellow);
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
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.Yellow);
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
        var expected = NavigationSurfaceUtilities.ApplySurfaceTint(background, tint, 1);
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
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
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
        Assert.True(selectedNode.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.True(selectedNode.Actions.HasFlag(SemanticsActions.Tap));

        var disabledNode = FindSemantics(root,
            node => node.Label?.Contains("Disabled\nTab 3 of 3", StringComparison.Ordinal) == true);
        Assert.NotNull(disabledNode);
        Assert.False(disabledNode!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.False(disabledNode.Actions.HasFlag(SemanticsActions.Tap));
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
        var theme = ThemeData.Light with
        {
            SurfaceColor = Colors.DarkSlateBlue,
            SecondaryContainerColor = Colors.DarkGreen,
            OnSecondaryContainerColor = Colors.Gold,
        };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            RailHost(new NavigationRail(RailDestinations(), selectedIndex: 0))));

        harness.Pump(new Size(420, 320));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.DarkSlateBlue);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.DarkGreen);
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinWidth == 80);
        Assert.NotNull(FindParagraph(harness.RenderView, "rail-selected-one"));
        Assert.Null(FindParagraph(harness.RenderView, "Rail one"));
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
        var first = FindSemantics(root, node => node.Label == "Tab 1 of 2");
        Assert.NotNull(first);
        Assert.True(first!.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.True(first.Flags.HasFlag(SemanticsFlags.IsEnabled));
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
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);

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
            return _pipeline.SemanticsOwner.RootNode;
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
}
