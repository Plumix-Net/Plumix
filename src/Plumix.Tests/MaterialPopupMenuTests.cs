using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using RelativeRect = Plumix.Rendering.RelativeRect;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialPopupMenuTests : IDisposable
{
    public MaterialPopupMenuTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void PopupMenuButtonAndItem_ExposeFlutterDefaultsAndValidateContracts()
    {
        var item = new PopupMenuItem<string>(new Text("One"), value: "one");
        Assert.True(item.Enabled);
        Assert.Equal(48, item.Height);
        Assert.True(item.Represents("one"));
        Assert.False(item.Represents("two"));

        var checkedItem = new CheckedPopupMenuItem<string>(new Text("Checked"), value: "checked");
        Assert.False(checkedItem.Checked);
        Assert.True(checkedItem.Enabled);
        Assert.Equal(48, checkedItem.Height);

        var divider = new PopupMenuDivider();
        Assert.Equal(16, divider.Height);
        Assert.False(divider.Represents("anything"));

        var button = new PopupMenuButton<string>(_ => [item]);
        Assert.True(button.Enabled);
        Assert.Equal(new Thickness(8), button.Padding);
        Assert.Equal(Clip.None, button.ClipBehavior);
        Assert.False(button.UseRootNavigator);

        Assert.Throws<ArgumentException>(() => new PopupMenuButton<string>(
            _ => [item],
            child: new Text("child"),
            icon: new Icon(Icons.MoreVert)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PopupMenuButton<string>(_ => [item], elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PopupMenuItem<string>(new Text("bad"), height: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CheckedPopupMenuItem<string>(new Text("bad"), height: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PopupMenuDivider(height: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PopupMenuDivider(thickness: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PopupMenuThemeData(Elevation: -1));
        Assert.Throws<ArgumentException>(() =>
        {
            _ = PopupMenus.ShowMenu(
                default,
                Array.Empty<PopupMenuEntry<string>>(),
                position: new RelativeRect(0, 0, 0, 0));
        });
    }

    [Fact]
    public void PopupMenuDivider_DelegatesGeometryAndColorToDivider()
    {
        var radius = BorderRadius.Circular(5);
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new PopupMenuDivider(
                height: 20,
                thickness: 5,
                indent: 7,
                endIndent: 9,
                radius: radius,
                color: Colors.Orange)));
        harness.Pump(new Size(240, 80));

        var line = Assert.Single(FindDescendants<RenderDividerLine>(harness.RenderView));
        Assert.Equal(5, line.Thickness);
        Assert.Equal(7, line.Indent);
        Assert.Equal(9, line.EndIndent);
        Assert.Equal(radius, line.Radius);
        Assert.Equal(Colors.Orange, line.Color);
        Assert.Equal(20, harness.RenderView.Child!.Size.Height);
    }

    [Fact]
    public void CheckedPopupMenuItem_UsesCheckmarkSelectedStyleAndCheckboxSemantics()
    {
        var labelStyle = MaterialStateProperty<TextStyle?>.ResolveWith(states =>
            states.HasFlag(MaterialState.Selected)
                ? new TextStyle(FontSize: 24, Color: Colors.Red)
                : new TextStyle(FontSize: 20, Color: Colors.Orange));
        using var checkedHarness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new CheckedPopupMenuItem<string>(
                new Text("Checked item"),
                value: "checked",
                @checked: true,
                labelTextStyle: labelStyle)));
        var checkedSemantics = checkedHarness.PumpAndGetSemantics(new Size(280, 100));

        var checkedText = FindParagraph(checkedHarness.RenderView, "Checked item");
        Assert.NotNull(checkedText);
        Assert.Equal(24, checkedText!.FontSize);
        Assert.Equal(Colors.Red, Assert.IsType<SolidColorBrush>(checkedText.Foreground).Color);
        Assert.NotNull(FindParagraph(checkedHarness.RenderView, char.ConvertFromUtf32(Icons.Done.CodePoint)));
        Assert.NotNull(FindSemantics(checkedSemantics, node =>
            node.Role == SemanticsRole.MenuItemCheckbox
            && node.Flags.HasFlag(SemanticsFlags.IsButton)
            && node.Flags.HasFlag(SemanticsFlags.IsEnabled)
            && node.Flags.HasFlag(SemanticsFlags.HasCheckedState)
            && node.Flags.HasFlag(SemanticsFlags.IsChecked)));

        using var uncheckedHarness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new CheckedPopupMenuItem<string>(
                new Text("Unchecked item"),
                labelTextStyle: labelStyle)));
        var uncheckedSemantics = uncheckedHarness.PumpAndGetSemantics(new Size(280, 100));
        var uncheckedText = FindParagraph(uncheckedHarness.RenderView, "Unchecked item");
        Assert.NotNull(uncheckedText);
        Assert.Equal(20, uncheckedText!.FontSize);
        Assert.Equal(Colors.Orange, Assert.IsType<SolidColorBrush>(uncheckedText.Foreground).Color);
        Assert.Null(FindParagraph(uncheckedHarness.RenderView, char.ConvertFromUtf32(Icons.Done.CodePoint)));
        Assert.NotNull(FindSemantics(uncheckedSemantics, node =>
            node.Role == SemanticsRole.MenuItemCheckbox
            && node.Flags.HasFlag(SemanticsFlags.HasCheckedState)
            && !node.Flags.HasFlag(SemanticsFlags.IsChecked)));

        using var m2Harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light with { UseMaterial3 = false },
            new CheckedPopupMenuItem<string>(new Text("M2 checked"), @checked: true)));
        m2Harness.Pump(new Size(280, 100));
        Assert.Equal(
            ThemeData.Light.TextTheme.TitleMedium.FontSize,
            FindParagraph(m2Harness.RenderView, "M2 checked")!.FontSize);
    }

    [Fact]
    public void PopupMenuItem_UsesM3AndM2TypographyPaddingAndDisabledSemantics()
    {
        using var m3 = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new PopupMenuItem<string>(new Text("M3 item"), value: "m3")));
        var m3Semantics = m3.PumpAndGetSemantics(new Size(240, 80));
        Assert.Contains(FindDescendants<RenderPadding>(m3.RenderView), value => value.Padding == new Thickness(12, 0));
        Assert.Equal(ThemeData.Light.TextTheme.LabelLarge.FontSize, FindParagraph(m3.RenderView, "M3 item")!.FontSize);
        Assert.NotNull(FindSemantics(m3Semantics, node =>
            node.Flags.HasFlag(SemanticsFlags.IsButton)
            && node.Flags.HasFlag(SemanticsFlags.IsEnabled)));

        using var m2 = new WidgetRenderHarness(Wrap(
            ThemeData.Light with { UseMaterial3 = false },
            new PopupMenuItem<string>(new Text("M2 disabled"), enabled: false)));
        var m2Semantics = m2.PumpAndGetSemantics(new Size(240, 80));
        Assert.Contains(FindDescendants<RenderPadding>(m2.RenderView), value => value.Padding == new Thickness(16, 0));
        Assert.Equal(ThemeData.Light.TextTheme.TitleMedium.FontSize, FindParagraph(m2.RenderView, "M2 disabled")!.FontSize);
        Assert.NotNull(FindSemantics(m2Semantics, node => node.Flags.HasFlag(SemanticsFlags.IsButton)));
        Assert.Null(FindSemantics(m2Semantics, node => node.Actions.HasFlag(SemanticsActions.Tap)));
    }

    [Fact]
    public async Task ShowMenu_UsesPositionThemeSurfaceShrinkWrapAndCompletesSelection()
    {
        BuildContext captured = default;
        int itemTapCount = 0;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light with { Platform = TargetPlatform.Android },
            new Navigator(new BuilderPageRoute(context => new CaptureContext(
                value => captured = value,
                new Text("Home"))))));
        harness.Pump(new Size(500, 360));

        var result = PopupMenus.ShowMenu(
            captured,
            items:
            [
                new PopupMenuItem<string>(new Text("First"), value: "first", onTap: () => itemTapCount++),
                new PopupMenuItem<string>(new Text("Second"), value: "second"),
            ],
            position: new RelativeRect(40, 30, 380, 290));
        PumpAnimation();
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));

        var layout = Assert.Single(FindDescendants<RenderPopupMenuPositionLayout>(harness.RenderView));
        Assert.Equal(40, ((BoxParentData)layout.Child!.parentData!).offset.X, precision: 3);
        Assert.Equal(30, ((BoxParentData)layout.Child.parentData!).offset.Y, precision: 3);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == ThemeData.Light.SurfaceContainerColor
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(4)
            && box.Decoration.BoxShadows is not null);
        var viewport = Assert.Single(FindDescendants<RenderSingleChildViewport>(harness.RenderView));
        Assert.True(viewport.Size.Height < 360);
        Assert.NotNull(FindSemantics(semantics, node =>
            node.Label == "Popup menu"
            && node.Flags.HasFlag(SemanticsFlags.ScopesRoute)
            && node.Flags.HasFlag(SemanticsFlags.NamesRoute)));

        var itemAction = FindSemantics(semantics, node =>
            node.Actions.HasFlag(SemanticsActions.Tap)
            && node.Label != "Dismiss menu");
        Assert.True(itemAction is not null, DumpSemantics(semantics));
        Assert.True(itemAction!.PerformAction(SemanticsActions.Tap));
        Assert.Equal(1, itemTapCount);
        PumpAnimation();
        harness.Pump(new Size(500, 360));
        Assert.Equal("first", await result);
        Assert.Null(FindParagraph(harness.RenderView, "First"));
    }

    [Fact]
    public async Task CheckedPopupMenuItem_TapFadesCheckmarkInBeforeRouteCloses()
    {
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(context => new CaptureContext(
                value => captured = value,
                new Text("Home"))))));
        harness.Pump(new Size(500, 360));

        var result = PopupMenus.ShowMenu<string>(
            captured,
            items:
            [
                new CheckedPopupMenuItem<string>(
                    new Text("Toggle"),
                    value: "toggle",
                    @checked: false),
            ],
            position: new RelativeRect(40, 30, 380, 290));
        PumpAnimation();
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        var action = FindSemantics(semantics, node =>
            node.Role == SemanticsRole.MenuItemCheckbox
            && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(action);
        Assert.True(action!.PerformAction(SemanticsActions.Tap));

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.075));
        harness.Pump(new Size(500, 360));
        Assert.NotNull(FindParagraph(harness.RenderView, char.ConvertFromUtf32(Icons.Done.CodePoint)));
        Assert.Contains(FindDescendants<RenderOpacity>(harness.RenderView), opacity =>
            opacity.Opacity > 0.35 && opacity.Opacity < 0.65);

        PumpAnimation();
        harness.Pump(new Size(500, 360));
        Assert.Equal("toggle", await result);
    }

    [Fact]
    public void ShowMenu_WidgetValuesOverrideLocalAndGlobalPopupMenuThemes()
    {
        BuildContext captured = default;
        var global = ThemeData.Light with
        {
            PopupMenuTheme = new PopupMenuThemeData(
                Color: Colors.Green,
                Shape: ShapeBorder.RoundedRectangle(6),
                Elevation: 2),
        };
        using var harness = new WidgetRenderHarness(Wrap(
            global,
            new Navigator(new BuilderPageRoute(context => new PopupMenuTheme(
                new PopupMenuThemeData(
                    Color: Colors.Purple,
                    Shape: ShapeBorder.RoundedRectangle(10),
                    MenuPadding: new Thickness(5)),
                new CaptureContext(value => captured = value, new Text("Home")))))));
        harness.Pump(new Size(500, 360));
        _ = PopupMenus.ShowMenu(
            captured,
            items: [new PopupMenuItem<string>(new Text("Override"), value: "override")],
            position: new RelativeRect(20, 20, 400, 290),
            color: Colors.Orange,
            shape: ShapeBorder.RoundedRectangle(3),
            elevation: 0,
            menuPadding: new Thickness(7));
        PumpAnimation();
        harness.Pump(new Size(500, 360));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == Colors.Orange
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(3)
            && box.Decoration.BoxShadows is null);
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), padding =>
            padding.Padding == new Thickness(7));
    }

    [Fact]
    public async Task PopupMenuButton_AnchorsUnderButtonSkipsDisabledKeyboardItemAndReportsCancel()
    {
        int opened = 0;
        int canceled = 0;
        string? selected = null;
        var selectedCompletion = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var canceledCompletion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new Align(
                alignment: Alignment.TopLeft,
                child: new PopupMenuButton<string>(
                    itemBuilder: _ =>
                    [
                        new PopupMenuItem<string>(new Text("One"), value: "one"),
                        new PopupMenuDivider(),
                        new PopupMenuItem<string>(new Text("Disabled"), value: "disabled", enabled: false),
                        new PopupMenuItem<string>(new Text("Three"), value: "three"),
                    ],
                    onOpened: () => opened++,
                    onSelected: value =>
                    {
                        selected = value;
                        selectedCompletion.TrySetResult(value);
                    },
                    onCanceled: () =>
                    {
                        canceled++;
                        canceledCompletion.TrySetResult();
                    },
                    position: PopupMenuPosition.Under,
                    offset: new Vector(5, 7),
                    child: new SizedBox(width: 80, height: 32, child: new Text("OPEN"))))))));
        var initialSemantics = harness.PumpAndGetSemantics(new Size(500, 360));
        var openAction = FindSemantics(initialSemantics, node => node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(openAction);
        Assert.True(openAction!.PerformAction(SemanticsActions.Tap));
        Assert.Equal(1, opened);
        PumpAnimation();
        harness.Pump(new Size(500, 360));

        var positionLayout = Assert.Single(FindDescendants<RenderPopupMenuPositionLayout>(harness.RenderView));
        Assert.Equal(5, positionLayout.Position.Left, precision: 3);
        Assert.Equal(39, positionLayout.Position.Top, precision: 3);
        var expandedSemantics = harness.PumpAndGetSemantics(new Size(500, 360));
        Assert.True(FindSemantics(expandedSemantics, node =>
            node.Flags.HasFlag(SemanticsFlags.HasExpandedState)
            && node.Flags.HasFlag(SemanticsFlags.IsExpanded)) is not null, DumpSemantics(expandedSemantics));

        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("ArrowDown", true)));
        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("Enter", true)));
        PumpAnimation();
        harness.Pump(new Size(500, 360));
        await selectedCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("three", selected);

        var reopenSemantics = harness.PumpAndGetSemantics(new Size(500, 360));
        var reopen = FindSemantics(reopenSemantics, node => node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(reopen);
        Assert.True(reopen!.PerformAction(SemanticsActions.Tap));
        PumpAnimation();
        var menuSemantics = harness.PumpAndGetSemantics(new Size(500, 360));
        var barrier = FindSemantics(menuSemantics, node => node.Label == "Dismiss menu");
        Assert.NotNull(barrier);
        Assert.True(barrier!.PerformAction(SemanticsActions.Tap));
        PumpAnimation();
        harness.Pump(new Size(500, 360));
        await canceledCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, canceled);
    }

    private static Widget Wrap(ThemeData theme, Widget child) =>
        new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(Size: new Size(500, 360)),
                new Theme(theme, child)));

    private static void PumpAnimation()
    {
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);

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
        if (node is null || predicate(node)) return node;
        foreach (var child in node.Children)
        {
            var result = FindSemantics(child, predicate);
            if (result is not null) return result;
        }
        return null;
    }

    private static string DumpSemantics(SemanticsNode? node, int depth = 0)
    {
        if (node is null) return "<null>";
        string line = $"{new string(' ', depth * 2)}label={node.Label ?? "<null>"}; flags={node.Flags}; actions={node.Actions}";
        return string.Join("\n", new[] { line }.Concat(node.Children.Select(child => DumpSemantics(child, depth + 1))));
    }

    private sealed class CaptureContext : StatelessWidget
    {
        private readonly Action<BuildContext> _capture;
        private readonly Widget _child;

        public CaptureContext(Action<BuildContext> capture, Widget child)
        {
            _capture = capture;
            _child = child;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(context);
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
