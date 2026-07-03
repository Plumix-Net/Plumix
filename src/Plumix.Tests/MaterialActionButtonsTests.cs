using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialActionButtonsTests
{
    [Fact]
    public void BackButtonIcon_UsesPlatformSpecificGlyph()
    {
        using var windows = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light with { Platform = TargetPlatform.Windows },
                new BackButtonIcon()));
        windows.Pump(new Size(80, 80));
        Assert.NotNull(FindParagraph(windows.RenderView, char.ConvertFromUtf32(Icons.ArrowBack.CodePoint)));

        using var ios = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light with { Platform = TargetPlatform.IOS },
                new BackButtonIcon()));
        ios.Pump(new Size(80, 80));
        Assert.NotNull(FindParagraph(ios.RenderView, char.ConvertFromUtf32(Icons.ArrowBackIosNewRounded.CodePoint)));
    }

    [Fact]
    public void ActionIconTheme_LocalOverrideWinsOverThemeData()
    {
        var theme = ThemeData.Light with
        {
            ActionIconTheme = new ActionIconThemeData(
                BackButtonIconBuilder: _ => new Text("theme-back"),
                CloseButtonIconBuilder: _ => new Text("theme-close"),
                DrawerButtonIconBuilder: _ => new Text("theme-drawer"),
                EndDrawerButtonIconBuilder: _ => new Text("theme-end-drawer")),
        };
        using var harness = new WidgetRenderHarness(
            new Theme(
                theme,
                new ActionIconTheme(
                    data: new ActionIconThemeData(
                        BackButtonIconBuilder: _ => new Text("local-back"),
                        CloseButtonIconBuilder: _ => new Text("local-close"),
                        DrawerButtonIconBuilder: _ => new Text("local-drawer"),
                        EndDrawerButtonIconBuilder: _ => new Text("local-end-drawer")),
                    child: new Row(children:
                    [
                        new BackButtonIcon(),
                        new CloseButtonIcon(),
                        new DrawerButtonIcon(),
                        new EndDrawerButtonIcon(),
                    ]))));

        harness.Pump(new Size(240, 80));

        Assert.NotNull(FindParagraph(harness.RenderView, "local-back"));
        Assert.NotNull(FindParagraph(harness.RenderView, "local-close"));
        Assert.NotNull(FindParagraph(harness.RenderView, "local-drawer"));
        Assert.NotNull(FindParagraph(harness.RenderView, "local-end-drawer"));
        Assert.Null(FindParagraph(harness.RenderView, "theme-back"));
        Assert.Null(FindParagraph(harness.RenderView, "theme-close"));
        Assert.Null(FindParagraph(harness.RenderView, "theme-drawer"));
        Assert.Null(FindParagraph(harness.RenderView, "theme-end-drawer"));
    }

    [Fact]
    public void DrawerButtons_UseLocalizedTooltipsAndCustomCallbacks()
    {
        var drawerPressed = 0;
        var endDrawerPressed = 0;
        using var harness = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light,
                new MaterialLocalizationsScope(
                    new TestMaterialLocalizations(),
                    new Row(children:
                    [
                        new DrawerButton(onPressed: () => drawerPressed++),
                        new EndDrawerButton(onPressed: () => endDrawerPressed++),
                    ]))));

        var semantics = harness.PumpAndGetSemantics(new Size(180, 80));
        Assert.NotNull(FindSemantics(semantics, node => node.Label == "Ouvrir le menu"));
        var buttons = FindAllSemantics(
            semantics,
            node => node.Flags.HasFlag(SemanticsFlags.IsButton)
                    && node.Actions.HasFlag(SemanticsActions.Tap));

        Assert.Equal(2, buttons.Count);
        Assert.True(buttons[0].PerformAction(SemanticsActions.Tap));
        Assert.True(buttons[1].PerformAction(SemanticsActions.Tap));
        Assert.Equal(1, drawerPressed);
        Assert.Equal(1, endDrawerPressed);
    }

    [Fact]
    public void DrawerButtons_DefaultCallbacks_OpenMatchingScaffoldDrawers()
    {
        ScaffoldState? scaffold = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light,
                new Scaffold(
                    drawer: new Drawer(child: new Text("start drawer")),
                    endDrawer: new Drawer(child: new Text("end drawer")),
                    body: new CaptureScaffoldState(
                        capture: state => scaffold = state,
                        child: new Row(children: [new DrawerButton(), new EndDrawerButton()])))));

        var semantics = harness.PumpAndGetSemantics(new Size(320, 180));
        var buttons = FindAllSemantics(
            semantics,
            node => node.Flags.HasFlag(SemanticsFlags.IsButton)
                    && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.Equal(2, buttons.Count);

        Assert.True(buttons[0].PerformAction(SemanticsActions.Tap));
        harness.Pump(new Size(320, 180));
        Assert.True(scaffold!.IsDrawerOpen);
        Assert.False(scaffold.IsEndDrawerOpen);

        scaffold.CloseDrawer();
        semantics = harness.PumpAndGetSemantics(new Size(320, 180));
        buttons = FindAllSemantics(
            semantics,
            node => node.Flags.HasFlag(SemanticsFlags.IsButton)
                    && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.True(buttons[1].PerformAction(SemanticsActions.Tap));
        harness.Pump(new Size(320, 180));
        Assert.False(scaffold.IsDrawerOpen);
        Assert.True(scaffold.IsEndDrawerOpen);
    }

    [Fact]
    public void BackAndCloseButtons_UseLocalizedTooltipsAndCustomCallbacks()
    {
        var backPressed = 0;
        var closePressed = 0;
        using var harness = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light,
                new MaterialLocalizationsScope(
                    new TestMaterialLocalizations(),
                    new Row(children:
                    [
                        new BackButton(onPressed: () => backPressed++),
                        new CloseButton(onPressed: () => closePressed++),
                    ]))));

        var semantics = harness.PumpAndGetSemantics(new Size(180, 80));
        var back = FindSemantics(semantics, node => node.Label == "Retour");
        var close = FindSemantics(semantics, node => node.Label == "Fermer");

        Assert.NotNull(back);
        Assert.NotNull(close);
        var buttons = FindAllSemantics(
            semantics,
            node => node.Flags.HasFlag(SemanticsFlags.IsButton)
                    && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.Equal(2, buttons.Count);
        Assert.True(buttons[0].PerformAction(SemanticsActions.Tap));
        Assert.True(buttons[1].PerformAction(SemanticsActions.Tap));
        Assert.Equal(1, backPressed);
        Assert.Equal(1, closePressed);
    }

    [Theory]
    [InlineData(false, "Back")]
    [InlineData(true, "Close")]
    public void ActionButton_DefaultCallback_MaybePopsNavigator(bool close, string tooltip)
    {
        NavigatorState? navigator = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light,
                new Navigator(
                    initialRoute: new BuilderPageRoute(
                        builder: context =>
                        {
                            navigator = Navigator.Of(context);
                            return new Text("Root");
                        }))));
        harness.Pump(new Size(180, 80));

        navigator!.Push(new BuilderPageRoute(
            builder: _ => close ? new CloseButton() : new BackButton()));
        var semantics = harness.PumpAndGetSemantics(new Size(180, 80));
        Assert.True(navigator.CanPop);

        var action = FindSemantics(semantics, node => node.Label == tooltip);
        Assert.NotNull(action);
        var button = FindSemantics(
            semantics,
            node => node.Flags.HasFlag(SemanticsFlags.IsButton)
                    && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(button);
        Assert.True(button!.PerformAction(SemanticsActions.Tap));
        harness.Pump(new Size(180, 80));
        Assert.False(navigator.CanPop);
        Assert.NotNull(FindParagraph(harness.RenderView, "Root"));
    }

    [Fact]
    public void BackButton_StyleIconColorOverridesLegacyColor()
    {
        var style = new ButtonStyle(
            IconColor: MaterialStateProperty<Color?>.All(Colors.Purple));
        using var harness = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light with { Platform = TargetPlatform.Windows },
                new BackButton(color: Colors.Red, style: style, onPressed: () => { })));

        harness.Pump(new Size(80, 80));

        var icon = FindParagraph(harness.RenderView, char.ConvertFromUtf32(Icons.ArrowBack.CodePoint));
        Assert.NotNull(icon);
        Assert.Equal(Colors.Purple, Assert.IsType<SolidColorBrush>(icon!.Foreground).Color);
    }

    [Fact]
    public void AndroidActionIcons_DuplicateTooltipLabelForPlatformSemantics()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light with { Platform = TargetPlatform.Android },
                new BackButton(onPressed: () => { })));

        harness.Pump(new Size(80, 80));

        var labels = FindDescendants<RenderSemanticsAnnotations>(harness.RenderView)
            .Count(semantics => semantics.Label == "Back");
        Assert.Equal(2, labels);
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T target) result.Add(target);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static SemanticsNode? FindSemantics(SemanticsNode? node, Func<SemanticsNode, bool> predicate)
    {
        if (node is null) return null;
        if (predicate(node)) return node;
        foreach (var child in node.Children)
        {
            var result = FindSemantics(child, predicate);
            if (result is not null) return result;
        }
        return null;
    }

    private static List<SemanticsNode> FindAllSemantics(
        SemanticsNode? node,
        Func<SemanticsNode, bool> predicate)
    {
        var result = new List<SemanticsNode>();
        if (node is null) return result;
        if (predicate(node)) result.Add(node);
        foreach (var child in node.Children)
        {
            result.AddRange(FindAllSemantics(child, predicate));
        }
        return result;
    }

    private sealed class TestMaterialLocalizations : MaterialLocalizations
    {
        public override string BackButtonTooltip => "Retour";
        public override string CloseButtonTooltip => "Fermer";
        public override string OpenAppDrawerTooltip => "Ouvrir le menu";
        public override string TabLabel(int tabIndex, int tabCount) => $"{tabIndex + 1}/{tabCount}";
    }

    private sealed class CaptureScaffoldState : StatelessWidget
    {
        private readonly Action<ScaffoldState> _capture;
        private readonly Widget _child;

        public CaptureScaffoldState(Action<ScaffoldState> capture, Widget child)
        {
            _capture = capture;
            _child = child;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(Scaffold.Of(context));
            return _child;
        }
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
