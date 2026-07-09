using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialAboutTests : IDisposable
{
    public MaterialAboutTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
        LicenseRegistry.Reset();
    }

    public void Dispose()
    {
        LicenseRegistry.Reset();
        GestureBinding.Instance.ResetForTests();
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void LicenseEntryWithLineBreaks_ParsesParagraphsAndIndentation()
    {
        var entry = new LicenseEntryWithLineBreaks(["sample"],
            "Heading line\ncontinued line\n\n   indented item\n   continuation\n\n            centered");

        Assert.Equal(["sample"], entry.Packages);
        Assert.Equal(
        [
            new LicenseParagraph("Heading line continued line", 0),
            new LicenseParagraph("indented item continuation", 1),
            new LicenseParagraph("centered", LicenseParagraph.CenteredIndent),
        ], entry.Paragraphs);
    }

    [Fact]
    public async Task LicenseRegistry_IsLazyOrderedAndResettable()
    {
        int calls = 0;
        LicenseRegistry.AddLicense(() =>
        {
            calls++;
            return [new LicenseEntryWithLineBreaks(["first"], "First license")];
        });
        LicenseRegistry.AddLicense(() => [new LicenseEntryWithLineBreaks(["second"], "Second license")]);

        Assert.Equal(0, calls);
        var entries = new List<LicenseEntry>();
        await foreach (var entry in LicenseRegistry.Licenses()) entries.Add(entry);
        Assert.Equal(1, calls);
        Assert.Equal(["first", "second"], entries.Select(entry => entry.Packages.Single()));

        LicenseRegistry.Reset();
        entries.Clear();
        await foreach (var entry in LicenseRegistry.Licenses()) entries.Add(entry);
        Assert.Empty(entries);
    }

    [Fact]
    public void AboutDialog_ComposesApplicationDetailsChildrenAndM3Actions()
    {
        var dialog = new AboutDialog(
            applicationName: "Plumix Demo",
            applicationVersion: "2.4.0",
            applicationIcon: new Icon(Icons.InfoOutline),
            applicationLegalese: "Copyright 2026",
            children: [new Text("Built with widgets")]);
        using var harness = new WidgetRenderHarness(BuildThemed(dialog));

        harness.Pump(new Size(640, 480));
        Assert.NotNull(FindParagraph(harness.RenderView, "Plumix Demo"));
        Assert.NotNull(FindParagraph(harness.RenderView, "2.4.0"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Copyright 2026"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Built with widgets"));
        Assert.NotNull(FindParagraph(harness.RenderView, "View licenses"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Close"));
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), padding =>
            padding.Padding == new Thickness(24, 0));
    }

    [Fact]
    public void AboutDialog_M2UppercasesActions_AndAdaptiveKeepsFields()
    {
        var adaptive = AboutDialog.Adaptive(applicationName: "Adaptive", applicationVersion: "1");
        Assert.Equal("Adaptive", adaptive.ApplicationName);
        Assert.Equal("1", adaptive.ApplicationVersion);

        using var harness = new WidgetRenderHarness(BuildThemed(
            new AboutDialog(applicationName: "Legacy"),
            ThemeData.Light with { UseMaterial3 = false }));
        harness.Pump(new Size(560, 420));
        Assert.NotNull(FindParagraph(harness.RenderView, "VIEW LICENSES"));
        Assert.NotNull(FindParagraph(harness.RenderView, "CLOSE"));
    }

    [Fact]
    public void AboutListTile_UsesLocalizedDefaultTitleAndDenseForwarding()
    {
        var tile = new AboutListTile(
            applicationName: "Plumix",
            icon: new Icon(Icons.InfoOutline),
            dense: true);
        Assert.True(tile.Dense);
        Assert.NotNull(tile.Icon);

        using var harness = new WidgetRenderHarness(BuildThemed(tile));
        harness.Pump(new Size(420, 100));
        Assert.NotNull(FindParagraph(harness.RenderView, "About Plumix"));
    }

    [Fact]
    public void ShowAboutDialog_PushesDialogAndClosePopsIt()
    {
        BuildContext? capturedContext = null;
        var navigator = new Navigator(
            onGenerateRoute: settings => new BuilderPageRoute(
                builder: context => new CaptureContext(
                    value => capturedContext = value,
                    new Text("Home")),
                settings: settings));
        using var harness = new WidgetRenderHarness(BuildThemed(navigator));
        harness.Pump(new Size(640, 480));

        Assert.NotNull(capturedContext);
        _ = AboutDialogs.ShowAboutDialog(capturedContext!.Value, applicationName: "Dialog app");
        harness.Pump(new Size(640, 480));
        Assert.NotNull(FindParagraph(harness.RenderView, "Dialog app"));

        var semantics = harness.PumpAndGetSemantics(new Size(640, 480));
        var actionNodes = FindNodes(semantics!, node => node.Actions.HasFlag(SemanticsActions.Tap)).ToArray();
        Assert.True(actionNodes.Length >= 2);
        Assert.True(harness.PerformSemanticsAction(actionNodes[^1].Id, SemanticsActions.Tap));
        harness.Pump(new Size(640, 480));
        Assert.Null(FindParagraph(harness.RenderView, "Dialog app"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Home"));
    }

    [Fact]
    public void ShowAboutDialog_DefaultsToRootNavigator()
    {
        BuildContext? innerContext = null;
        var outer = new Navigator(
            onGenerateRoute: settings => new BuilderPageRoute(
                builder: _ => new Navigator(
                    onGenerateRoute: innerSettings => new BuilderPageRoute(
                        builder: context => new CaptureContext(
                            value => innerContext = value,
                            new Text("Nested home")),
                        settings: innerSettings)),
                settings: settings));
        using var harness = new WidgetRenderHarness(BuildThemed(outer));
        harness.Pump(new Size(640, 480));

        Assert.True(innerContext.HasValue);
        _ = AboutDialogs.ShowAboutDialog(innerContext.Value, applicationName: "Root dialog");
        harness.Pump(new Size(640, 480));

        Assert.True(Navigator.Of(innerContext.Value, rootNavigator: true).CanPop);
        Assert.False(Navigator.Of(innerContext.Value).CanPop);
        Assert.NotNull(FindParagraph(harness.RenderView, "Root dialog"));
    }

    [Fact]
    public void LicensePage_GroupsSortsAndNavigatesToPackageParagraphs()
    {
        LicenseRegistry.AddLicense(() =>
        [
            new LicenseEntryWithLineBreaks(["app", "zeta"], "App license"),
            new LicenseEntryWithLineBreaks(["alpha"], "Alpha license"),
            new LicenseEntryWithLineBreaks(["zeta"], "Second zeta license"),
        ]);
        var navigator = new Navigator(
            onGenerateRoute: settings => new BuilderPageRoute(
                builder: _ => new LicensePage(
                    applicationName: "License Demo",
                    applicationVersion: "1.0",
                    applicationLegalese: "Legal"),
                settings: settings));
        using var harness = new WidgetRenderHarness(BuildThemed(navigator));

        PumpUntil(harness, new Size(700, 560), () => FindParagraph(harness.RenderView, "app") is not null);
        string[] paragraphs = FindDescendants<RenderParagraph>(harness.RenderView).Select(item => item.Text).ToArray();
        Assert.True(Array.IndexOf(paragraphs, "app") < Array.IndexOf(paragraphs, "alpha"));
        Assert.True(Array.IndexOf(paragraphs, "alpha") < Array.IndexOf(paragraphs, "zeta"));
        Assert.Contains("1 license", paragraphs);
        Assert.Contains("2 licenses", paragraphs);
        Assert.Contains("Powered by Flutter", paragraphs);

        var semantics = harness.PumpAndGetSemantics(new Size(700, 560));
        var packageNodes = FindNodes(semantics!, node => node.Actions.HasFlag(SemanticsActions.Tap)).ToArray();
        Assert.Contains(packageNodes, _ => true);
        Assert.True(harness.PerformSemanticsAction(packageNodes[0].Id, SemanticsActions.Tap));
        harness.Pump(new Size(700, 560));
        Assert.NotNull(FindParagraph(harness.RenderView, "App license"));
    }

    private static Widget BuildThemed(Widget child, ThemeData? theme = null) => new MediaQuery(
        new MediaQueryData(Size: new Size(700, 560)),
        new Directionality(
            TextDirection.Ltr,
            new MaterialLocalizationsScope(
                DefaultMaterialLocalizations.Instance,
                new Theme(theme ?? ThemeData.Light, child))));

    private static void PumpUntil(WidgetRenderHarness harness, Size size, Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            harness.Pump(size);
            Thread.Sleep(5);
        }
        harness.Pump(size);
        Assert.True(condition(), "Asynchronous license data did not finish loading.");
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T typed) result.Add(typed);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static IEnumerable<SemanticsNode> FindNodes(SemanticsNode node, Func<SemanticsNode, bool> predicate)
    {
        if (predicate(node)) yield return node;
        foreach (var child in node.Children)
        foreach (var result in FindNodes(child, predicate))
            yield return result;
    }

    private sealed class CaptureContext(Action<BuildContext> capture, Widget child) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            capture(context);
            return child;
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
            _rootElement.Mount(null, null);
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

        public bool PerformSemanticsAction(int id, SemanticsActions action) =>
            _pipeline.SemanticsOwner.PerformAction(id, action);

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
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = Assert.IsAssignableFrom<RenderBox>(child);
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }
}
