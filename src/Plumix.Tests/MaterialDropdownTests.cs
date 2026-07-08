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

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialDropdownTests : IDisposable
{
    public MaterialDropdownTests()
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
    public void DropdownButtonAndMenuItem_ExposeFlutterDefaultsAndValidateContracts()
    {
        var item = new DropdownMenuItem<string>(new Text("One"), value: "one");
        Assert.Equal("one", item.Value);
        Assert.True(item.Enabled);
        Assert.Null(item.OnTap);

        var button = new DropdownButton<string>([item], _ => { }, value: "one");
        Assert.Equal(8, button.Elevation);
        Assert.Equal(24, button.IconSize);
        Assert.False(button.IsDense);
        Assert.False(button.IsExpanded);
        Assert.Equal(48, button.ItemHeight);
        Assert.True(button.BarrierDismissible);
        Assert.False(button.Autofocus);

        Assert.Throws<ArgumentException>(() => new DropdownButton<string>(
            [
                new DropdownMenuItem<string>(new Text("A"), value: "same"),
                new DropdownMenuItem<string>(new Text("B"), value: "same"),
            ],
            _ => { },
            value: "same"));
        Assert.Throws<ArgumentException>(() => new DropdownButton<string>(
            [new DropdownMenuItem<string>(new Text("A"), value: "a")],
            _ => { },
            value: "missing"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DropdownButton<string>([item], _ => { }, itemHeight: 47));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DropdownButton<string>([item], _ => { }, iconSize: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DropdownButton<string>([item], _ => { }, menuWidth: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DropdownButton<string>([item], _ => { }, menuMaxHeight: -1));
    }

    [Fact]
    public void DropdownButton_UsesSelectedHintDisabledHintAndLargestItemGeometry()
    {
        var items = new DropdownMenuItem<string>[]
        {
            new(new SizedBox(width: 40, child: new Text("Short")), value: "short"),
            new(new SizedBox(width: 180, child: new Text("Longest")), value: "long"),
        };
        using var selected = new WidgetRenderHarness(Wrap(
            new DropdownButton<string>(items, _ => { }, value: "short")));
        var selectedSemantics = selected.PumpAndGetSemantics(new Size(400, 160));
        Assert.NotNull(FindParagraph(selected.RenderView, "Short"));
        var indexed = Assert.Single(FindDescendants<RenderIndexedStack>(selected.RenderView));
        Assert.Equal(0, indexed.Index);
        Assert.True(indexed.Size.Width >= 180);
        Assert.Equal(48, indexed.Size.Height);
        Assert.NotNull(FindSemantics(selectedSemantics, node => node.Label == "Short"));
        Assert.Null(FindSemantics(selectedSemantics, node => node.Label == "Longest"));

        using var hint = new WidgetRenderHarness(Wrap(
            new DropdownButton<string>(items, _ => { }, hint: new Text("Choose"))));
        hint.Pump(new Size(400, 160));
        Assert.NotNull(FindParagraph(hint.RenderView, "Choose"));
        Assert.Equal(2, Assert.Single(FindDescendants<RenderIndexedStack>(hint.RenderView)).Index);

        using var disabled = new WidgetRenderHarness(Wrap(
            new DropdownButton<string>(items, null,
                hint: new Text("Fallback"),
                disabledHint: new Text("Disabled"))));
        var semantics = disabled.PumpAndGetSemantics(new Size(400, 160));
        Assert.NotNull(FindParagraph(disabled.RenderView, "Disabled"));
        Assert.Equal(2, Assert.Single(FindDescendants<RenderIndexedStack>(disabled.RenderView)).Index);
        var disabledNode = FindSemantics(semantics, node => node.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.NotNull(disabledNode);
        Assert.False(disabledNode!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.False(disabledNode.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void DropdownButton_SelectedItemBuilderDensePaddingAndUnderlinePolicyMatchSourceComposition()
    {
        var items = new DropdownMenuItem<string>[]
        {
            new(new Text("Menu one"), value: "one"),
            new(new Text("Menu two"), value: "two"),
        };
        using var harness = new WidgetRenderHarness(Wrap(
            new DropdownButtonHideUnderline(
                new ButtonTheme(
                    new ButtonThemeData(AlignedDropdown: true),
                    new DropdownButton<string>(
                        items,
                        _ => { },
                        selectedItemBuilder: _ => [new Text("Selected one"), new Text("Selected two")],
                        value: "two",
                        isDense: true,
                        padding: new Thickness(3))))));
        harness.Pump(new Size(400, 160));

        Assert.NotNull(FindParagraph(harness.RenderView, "Selected two"));
        Assert.Equal(1, Assert.Single(FindDescendants<RenderIndexedStack>(harness.RenderView)).Index);
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), value => value.Padding == new Thickness(3));
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), value => value.Padding == new Thickness(16, 0, 4, 0));
        Assert.DoesNotContain(FindDescendants<RenderColoredBox>(harness.RenderView),
            box => box.Color == Color.Parse("#FFBDBDBD"));

        Assert.Throws<InvalidOperationException>(() => new WidgetRenderHarness(Wrap(
            new DropdownButton<string>(
                items,
                _ => { },
                selectedItemBuilder: _ => [new Text("Only one")],
                value: "one"))));
    }

    [Fact]
    public async Task DropdownButton_OpensPositionedMenuAndCompletesKeyboardSelectionSkippingDisabled()
    {
        var buttonTap = 0;
        var firstTap = 0;
        string? selected = null;
        Widget page = new Align(
            alignment: Alignment.TopLeft,
            child: new DropdownButton<string>(
                items:
                [
                    new DropdownMenuItem<string>(new Text("One"), value: "one", onTap: () => firstTap++),
                    new DropdownMenuItem<string>(new Text("Disabled"), value: "disabled", enabled: false),
                    new DropdownMenuItem<string>(new Text("Three"), value: "three"),
                ],
                onChanged: value => selected = value,
                value: "one",
                onTap: () => buttonTap++,
                dropdownColor: Colors.Orange,
                menuWidth: 190,
                menuMaxHeight: 120,
                borderRadius: BorderRadius.Circular(9)));
        using var harness = new WidgetRenderHarness(Wrap(
            new Navigator(new BuilderPageRoute(_ => page))));
        var closedSemantics = harness.PumpAndGetSemantics(new Size(500, 360));
        var open = FindSemantics(closedSemantics, node =>
            node.Flags.HasFlag(SemanticsFlags.HasExpandedState)
            && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(open);
        Assert.True(open!.PerformAction(SemanticsActions.Tap));
        Assert.Equal(1, buttonTap);
        PumpAnimation();
        var openedSemantics = harness.PumpAndGetSemantics(new Size(500, 360));

        var layout = Assert.Single(FindDescendants<RenderDropdownMenuPositionLayout<string>>(harness.RenderView));
        Assert.Equal(190, layout.Child!.Size.Width, precision: 3);
        Assert.True(layout.Child.Size.Height <= 120.01);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == Colors.Orange
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(9));
        Assert.NotNull(FindSemantics(openedSemantics, node =>
            node.Role == SemanticsRole.Menu
            && node.Label == "Popup menu"));

        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("ArrowDown", true)));
        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("Enter", true)));
        PumpAnimation();
        harness.Pump(new Size(500, 360));
        await WaitForConditionAsync(() => selected is not null);
        Assert.Equal("three", selected);
        Assert.Equal(0, firstTap);
    }

    [Fact]
    public async Task DropdownButton_ItemTapRunsBeforeNullableSelectionAndBarrierPolicyIsHonored()
    {
        var itemTap = 0;
        var changed = false;
        string? value = "one";
        using var harness = new WidgetRenderHarness(Wrap(
            new Navigator(new BuilderPageRoute(_ => new DropdownButton<string>(
                items:
                [
                    new DropdownMenuItem<string>(new Text("None"), value: null, onTap: () => itemTap++),
                    new DropdownMenuItem<string>(new Text("One"), value: "one"),
                ],
                onChanged: next =>
                {
                    Assert.Equal(1, itemTap);
                    value = next;
                    changed = true;
                },
                value: value,
                barrierDismissible: false)))));
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        Assert.True(FindSemantics(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap))!
            .PerformAction(SemanticsActions.Tap));
        PumpAnimation();
        var openSemantics = harness.PumpAndGetSemantics(new Size(500, 360));
        Assert.Null(FindSemantics(openSemantics, node => node.Label == "Dismiss"));

        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("ArrowUp", true)));
        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("Enter", true)));
        PumpAnimation();
        harness.Pump(new Size(500, 360));
        await WaitForConditionAsync(() => changed);
        Assert.Null(value);
        Assert.Equal(1, itemTap);
    }

    [Fact]
    public void DropdownButton_MenuUsesThreeStageRevealAndMeasuresVariableItemHeights()
    {
        Widget page = new Align(
            alignment: Alignment.TopLeft,
            child: new DropdownButton<string>(
                items:
                [
                    new DropdownMenuItem<string>(
                        new SizedBox(height: 72, child: new Text("Tall")),
                        value: "tall"),
                    new DropdownMenuItem<string>(new Text("Normal"), value: "normal"),
                ],
                onChanged: _ => { },
                value: "tall",
                itemHeight: null));
        using var harness = new WidgetRenderHarness(Wrap(
            new Navigator(new BuilderPageRoute(_ => page))));
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        Assert.True(FindSemantics(semantics, node =>
            node.Flags.HasFlag(SemanticsFlags.HasExpandedState)
            && node.Actions.HasFlag(SemanticsActions.Tap))!.PerformAction(SemanticsActions.Tap));

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.03));
        harness.Pump(new Size(500, 360));
        var reveal = Assert.Single(FindDescendants<RenderDropdownMenuReveal>(harness.RenderView));
        Assert.InRange(reveal.RevealRect.Height, 47.9, 48.1);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.09));
        harness.Pump(new Size(500, 360));
        reveal = Assert.Single(FindDescendants<RenderDropdownMenuReveal>(harness.RenderView));
        Assert.True(reveal.RevealRect.Height > 48);

        PumpAnimation();
        harness.Pump(new Size(500, 360));
        var layout = Assert.Single(FindDescendants<RenderDropdownMenuPositionLayout<string>>(harness.RenderView));
        Assert.True(layout.Route.ItemHeights[0] >= 72);
        Assert.Equal(layout.Child!.Size.Height, reveal.Size.Height, precision: 3);
    }

    [Fact]
    public void DropdownButton_AutofocusAndKeyboardActivationOpenTheRoute()
    {
        var focusNode = new FocusNode();
        using (var harness = new WidgetRenderHarness(Wrap(
                   new Navigator(new BuilderPageRoute(_ => new DropdownButton<string>(
                       items: [new DropdownMenuItem<string>(new Text("One"), value: "one")],
                       onChanged: _ => { },
                       value: "one",
                       focusNode: focusNode,
                       autofocus: true))))))
        {
            harness.Pump(new Size(500, 360));
            Assert.True(focusNode.HasFocus);
            Assert.Same(focusNode, FocusManager.Instance.PrimaryFocus);
            Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("Enter", true)));
            PumpAnimation();
            var semantics = harness.PumpAndGetSemantics(new Size(500, 360));
            Assert.NotNull(FindSemantics(semantics, node => node.Role == SemanticsRole.Menu));
        }
        focusNode.Dispose();
    }

    [Fact]
    public void DropdownButtonFormField_ExposesFlutterDefaultsAndValidatesContracts()
    {
        var items = new[] { new DropdownMenuItem<string>(new Text("One"), value: "one") };
        var field = new DropdownButtonFormField<string>(items, _ => { }, initialValue: "one");
        Assert.True(field.IsDense);
        Assert.False(field.IsExpanded);
        Assert.Null(field.ItemHeight);
        Assert.Equal(8, field.Elevation);
        Assert.Equal(24, field.IconSize);
        Assert.True(field.BarrierDismissible);
        Assert.NotNull(field.Decoration);

        Assert.Throws<ArgumentException>(() => new DropdownButtonFormField<string>(items, _ => { }, initialValue: "missing"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DropdownButtonFormField<string>(items, _ => { }, itemHeight: 47));
        Assert.Throws<ArgumentException>(() => new DropdownButtonFormField<string>(
            items,
            _ => { },
            decoration: new InputDecoration(errorText: "fixed"),
            errorBuilder: (_, error) => new Text(error)));
    }

    [Fact]
    public void DropdownButtonFormField_ValidationChangeOrderingAndResetMatchSource()
    {
        var items = new[]
        {
            new DropdownMenuItem<string>(new Text("One"), value: "one"),
            new DropdownMenuItem<string>(new Text("Two"), value: "two"),
        };
        var callbacks = new List<string>();
        FormState? formState = null;
        using var harness = new WidgetRenderHarness(Wrap(new Form(
            onChanged: () => callbacks.Add("form"),
            child: new Builder(context =>
            {
                formState = Form.Of(context);
                return new DropdownButtonFormField<string>(
                    items,
                    value => callbacks.Add($"field:{value}"),
                    initialValue: "one",
                    decoration: new InputDecoration(labelText: "Choice"),
                    validator: value => value == "one" ? "Choose another" : null);
            }))));
        harness.Pump(new Size(420, 160));
        var state = Assert.IsType<DropdownButtonFormFieldState<string>>(Assert.Single(formState!.Fields));

        Assert.False(formState.Validate());
        harness.Pump(new Size(420, 180));
        Assert.Contains(FindDescendants<RenderParagraph>(harness.RenderView), value => value.Text == "Choose another");

        state.DidChange("two");
        Assert.Equal(new[] { "form", "field:two" }, callbacks.TakeLast(2));
        Assert.Equal("two", state.Value);
        Assert.True(formState.Validate());

        formState.Reset();
        harness.Pump(new Size(420, 160));
        Assert.Equal("one", state.Value);
        Assert.Equal(new[] { "form", "field:one", "form" }, callbacks.TakeLast(3));
    }

    [Fact]
    public void DropdownButtonFormField_UsesDecorationHintAndCustomErrorWidget()
    {
        FormState? formState = null;
        using var harness = new WidgetRenderHarness(Wrap(new Form(
            child: new Builder(context =>
            {
                formState = Form.Of(context);
                return new DropdownButtonFormField<string>(
                    [new DropdownMenuItem<string>(new Text("One"), value: "one")],
                    _ => { },
                    decoration: new InputDecoration(hintText: "Pick one"),
                    validator: _ => "Required",
                    errorBuilder: (_, error) => new Text($"custom {error}"));
            }))));
        harness.Pump(new Size(420, 140));
        Assert.NotNull(FindParagraph(harness.RenderView, "Pick one"));

        Assert.False(formState!.Validate());
        harness.Pump(new Size(420, 180));
        Assert.NotNull(FindParagraph(harness.RenderView, "custom Required"));
    }

    private static Widget Wrap(Widget child) => new Directionality(
        TextDirection.Ltr,
        new MediaQuery(
            new MediaQueryData(Size: new Size(500, 360)),
            new Theme(ThemeData.Light, child)));

    private static void PumpAnimation()
    {
        var now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.35));
    }

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++) await Task.Delay(10);
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
