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
public sealed class MaterialTextFieldTests : IDisposable
{
    public MaterialTextFieldTests() => FocusManager.Instance.ResetForTests();
    public void Dispose() => FocusManager.Instance.ResetForTests();


    [Fact]
    public void TextField_EnforcesMaxLengthUpdatesCounterAndSubmits()
    {
        var controller = new TextEditingController();
        var focusNode = new FocusNode();
        string? changed = null;
        string? submitted = null;
        using var harness = new WidgetRenderHarness(Wrap(new TextField(
            controller: controller,
            focusNode: focusNode,
            autofocus: true,
            maxLength: 3,
            decoration: new InputDecoration(labelText: "Code"),
            onChanged: value => changed = value,
            onSubmitted: value => submitted = value)));
        harness.Pump(new Size(360, 120));

        Assert.True(FocusManager.Instance.HandleTextInput("a😀cdef"));
        harness.Pump(new Size(360, 120));
        Assert.Equal("a😀c", controller.Text);
        Assert.Equal("a😀c", changed);
        Assert.Contains(FindDescendants<RenderParagraph>(harness.RenderView), value => value.PlainText == "3/3");

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Enter)));
        Assert.Equal("a😀c", submitted);
    }

    [Fact]
    public void TextField_ReadOnlyObscureAndDisabledStatesPropagate()
    {
        var controller = new TextEditingController("secret");
        var focusNode = new FocusNode();
        using var harness = new WidgetRenderHarness(Wrap(new TextField(
            controller: controller,
            focusNode: focusNode,
            readOnly: true,
            obscureText: true,
            enabled: false,
            decoration: new InputDecoration(labelText: "Password"))));
        harness.Pump(new Size(360, 100));

        Assert.Contains(
            FindDescendants<RenderEditable>(harness.RenderView),
            value => value.Text == "••••••");
        Assert.False(FocusManager.Instance.HandleTextInput("x"));
        Assert.Equal("secret", controller.Text);
        var semantics = Assert.Single(FindDescendants<RenderSemanticsAnnotations>(harness.RenderView), value =>
            value.Flags.HasFlag(SemanticsFlags.IsTextField));
        Assert.False(semantics.Flags.HasFlag(SemanticsFlags.IsEnabled));
    }

    [Fact]
    public void TextField_ConstructorGuardsMatchFlutterContracts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextField(maxLines: 0));
        Assert.Throws<ArgumentException>(() => new TextField(maxLines: 1, minLines: 2));
        Assert.Throws<ArgumentException>(() => new TextField(expands: true));
        Assert.Throws<ArgumentException>(() => new TextField(obscureText: true, maxLines: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextField(maxLength: 0));
        Assert.NotNull(new TextField(maxLength: TextField.NoMaxLength));

        var editable = new TextField();
        Assert.True(editable.EnableInteractiveSelection);
        Assert.NotNull(editable.ContextMenuBuilder);
        Assert.Same(TextMagnifier.AdaptiveMagnifierConfiguration, editable.MagnifierConfiguration);
        Assert.False(new TextField(readOnly: true, obscureText: true).EnableInteractiveSelection);
    }

    [Fact]
    public void TextField_PointerDragUpdatesControllerSelection()
    {
        var controller = new TextEditingController("drag selection");
        SelectionChangedCause? cause = null;
        using var harness = new WidgetRenderHarness(Wrap(new SizedBox(
            width: 260,
            child: new TextField(
                controller: controller,
                decoration: null,
                useDecoration: false,
                onSelectionChanged: (_, nextCause) => cause = nextCause))));
        harness.Pump(new Size(360, 120));

        var binding = GestureBinding.Instance;
        DateTime now = DateTime.UtcNow;
        binding.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            211,
            PointerDeviceKind.Mouse,
            new Point(2, 10),
            PointerButtons.Primary,
            now));
        binding.HandlePointerEvent(harness.RenderView, new PointerMoveEvent(
            211,
            PointerDeviceKind.Mouse,
            new Point(90, 10),
            PointerButtons.Primary,
            down: true,
            now.AddMilliseconds(16)));
        binding.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            211,
            PointerDeviceKind.Mouse,
            new Point(90, 10),
            PointerButtons.None,
            now.AddMilliseconds(32)));

        Assert.False(controller.Selection.IsCollapsed);
        Assert.True(controller.Selection.End > controller.Selection.Start);
        Assert.Equal(SelectionChangedCause.Drag, cause);
    }

    [Fact]
    public void TextField_DecoratedPointerTapFocusesPlacesCaretAndAcceptsTextInput()
    {
        var controller = new TextEditingController();
        var focusNode = new FocusNode();
        using var harness = new WidgetRenderHarness(Wrap(new TextField(
            controller: controller,
            focusNode: focusNode,
            decoration: new InputDecoration(
                labelText: "Message",
                prefixIcon: new Icon(Icons.Email),
                border: new OutlineInputBorder()))));
        harness.Pump(new Size(360, 120));

        var binding = GestureBinding.Instance;
        DateTime now = DateTime.UtcNow;
        binding.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            212,
            PointerDeviceKind.Mouse,
            new Point(170, 44),
            PointerButtons.Primary,
            now));
        binding.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            212,
            PointerDeviceKind.Mouse,
            new Point(170, 44),
            PointerButtons.None,
            now.AddMilliseconds(16)));
        harness.Pump(new Size(360, 120));

        Assert.True(focusNode.HasFocus);
        Assert.Equal(TextSelection.Collapsed(0), controller.Selection);
        Assert.True(FocusManager.Instance.HandleTextInput("!"));
        Assert.Equal("!", controller.Text);
    }

    [Fact]
    public void TextFormField_DecoratedPointerTapFocusesAndAcceptsTextInput()
    {
        var controller = new TextEditingController();
        var focusNode = new FocusNode();
        using var harness = new WidgetRenderHarness(Wrap(new TextFormField(
            controller: controller,
            focusNode: focusNode,
            decoration: new InputDecoration(
                labelText: "Form message",
                border: new OutlineInputBorder()))));
        harness.Pump(new Size(360, 120));

        var binding = GestureBinding.Instance;
        DateTime now = DateTime.UtcNow;
        binding.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            213,
            PointerDeviceKind.Mouse,
            new Point(170, 44),
            PointerButtons.Primary,
            now));
        binding.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            213,
            PointerDeviceKind.Mouse,
            new Point(170, 44),
            PointerButtons.None,
            now.AddMilliseconds(16)));
        harness.Pump(new Size(360, 120));

        Assert.True(focusNode.HasFocus);
        Assert.True(FocusManager.Instance.HandleTextInput("form"));
        Assert.Equal("form", controller.Text);
    }

    [Fact]
    public void TextFormField_ExposesFlutterDefaultsAndConstructorContracts()
    {
        var field = new TextFormField(initialValue: "hello");
        Assert.Equal("hello", field.InitialValue);
        Assert.True(field.Enabled);
        Assert.Equal(AutovalidateMode.Disabled, field.AutovalidateMode);
        Assert.Equal(1, field.MaxLines);
        Assert.False(field.ReadOnly);
        Assert.False(field.Autofocus);
        Assert.NotNull(field.Decoration);
        Assert.True(field.EnableInteractiveSelection);
        Assert.NotNull(field.ContextMenuBuilder);
        Assert.Same(TextMagnifier.AdaptiveMagnifierConfiguration, field.MagnifierConfiguration);

        var controller = new TextEditingController("external");
        Assert.Throws<ArgumentException>(() => new TextFormField(controller: controller, initialValue: "duplicate"));
        Assert.Throws<ArgumentException>(() => new TextFormField(
            decoration: new InputDecoration(errorText: "fixed"),
            errorBuilder: (_, error) => new Text(error)));
        Assert.Throws<ArgumentException>(() => new TextFormField(expands: true));
        controller.Dispose();
    }

    [Fact]
    public void TextFormField_SynchronizesControllerValidationSaveAndReset()
    {
        var controller = new TextEditingController("initial");
        FormState? formState = null;
        int formChanged = 0;
        var changedValues = new List<string>();
        string? saved = null;
        using var harness = new WidgetRenderHarness(Wrap(new Form(
            onChanged: () => formChanged++,
            child: new Builder(context =>
            {
                formState = Form.Of(context);
                return new TextFormField(
                    controller: controller,
                    decoration: new InputDecoration(labelText: "Name"),
                    validator: value => string.IsNullOrEmpty(value) ? "Name is required" : null,
                    onSaved: value => saved = value,
                    onChanged: changedValues.Add);
            }))));
        harness.Pump(new Size(360, 140));
        var state = Assert.IsType<TextFormFieldState>(Assert.Single(formState!.Fields));

        controller.Text = string.Empty;
        harness.Pump(new Size(360, 140));
        Assert.Equal(string.Empty, state.Value);
        Assert.Equal(1, formChanged);
        Assert.False(formState.Validate());
        harness.Pump(new Size(360, 160));
        Assert.Contains(
            FindDescendants<RenderParagraph>(harness.RenderView),
            value => value.PlainText == "Name is required");

        formState.Save();
        Assert.Equal(string.Empty, saved);
        formState.Reset();
        harness.Pump(new Size(360, 140));
        Assert.Equal("initial", controller.Text);
        Assert.Equal("initial", state.Value);
        Assert.Contains("initial", changedValues);
        controller.Dispose();
    }

    [Fact]
    public void TextFormField_ErrorBuilderOverridesValidationTextWidget()
    {
        FormState? formState = null;
        using var harness = new WidgetRenderHarness(Wrap(new Form(
            child: new Builder(context =>
            {
                formState = Form.Of(context);
                return new TextFormField(
                    initialValue: string.Empty,
                    validator: _ => "raw error",
                    errorBuilder: (_, error) => new Text($"custom: {error}"));
            }))));
        harness.Pump(new Size(360, 140));
        Assert.False(formState!.Validate());
        harness.Pump(new Size(360, 160));
        Assert.Contains(
            FindDescendants<RenderParagraph>(harness.RenderView),
            value => value.PlainText == "custom: raw error");
        Assert.DoesNotContain(
            FindDescendants<RenderParagraph>(harness.RenderView),
            value => value.PlainText == "raw error");
    }

    [Fact]
    public void TextField_UsesPersistentRenderEditableGeometry()
    {
        var controller = new TextEditingController("hello world", new TextSelection(0, 5));
        using var harness = new WidgetRenderHarness(Wrap(new TextField(
            controller: controller,
            useDecoration: false)));
        harness.Pump(new Size(360, 100));

        RenderEditable editable = Assert.Single(FindDescendants<RenderEditable>(harness.RenderView));
        Assert.Equal("hello world", editable.PlainText);
        Assert.Equal(2, editable.GetEndpointsForSelection(controller.Selection).Count);
        Assert.True(editable.PreferredLineHeight > 0.0);
    }

    [Fact]
    public async Task TextField_CustomSpellCheckServiceUpdatesRetainedRenderResults()
    {
        var service = new TestSpellCheckService();
        var controller = new TextEditingController();
        using var harness = new WidgetRenderHarness(Wrap(new TextField(
            controller: controller,
            useDecoration: false,
            spellCheckConfiguration: new SpellCheckConfiguration(spellCheckService: service))));
        harness.Pump(new Size(360, 100));

        controller.Text = "wrold";
        await Task.Yield();
        harness.Pump(new Size(360, 100));

        RenderEditable editable = Assert.Single(FindDescendants<RenderEditable>(harness.RenderView));
        Assert.Equal("wrold", service.LastText);
        Assert.Single(editable.SuggestionSpans);
        Assert.Equal(new TextRange(0, 5), editable.SuggestionSpans[0].Range);
    }

    [Fact]
    public void DefaultSpellCheckService_MergesSortedResultsAndRetainsOlderDuplicate()
    {
        var oldAtTwo = new SuggestionSpan(new TextRange(2, 4), ["old"]);
        IReadOnlyList<SuggestionSpan> merged = DefaultSpellCheckService.MergeResults(
            [oldAtTwo, new SuggestionSpan(new TextRange(8, 10), ["late"])],
            [new SuggestionSpan(new TextRange(2, 4), ["new"]), new SuggestionSpan(new TextRange(5, 7), ["mid"])]);

        Assert.Equal([2, 5, 8], merged.Select(span => span.Range.Start));
        Assert.Same(oldAtTwo, merged[0]);
    }

    private static Widget Wrap(Widget child, ThemeData? theme = null) => new Directionality(
        TextDirection.Ltr,
        new MediaQuery(new MediaQueryData(Size: new Size(360, 640)), new Theme(theme ?? ThemeData.Light, child)));

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T value) result.Add(value);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private sealed class TestSpellCheckService : ISpellCheckService
    {
        public string? LastText { get; private set; }

        public Task<IReadOnlyList<SuggestionSpan>?> FetchSpellCheckSuggestions(Locale locale, string text)
        {
            LastText = text;
            IReadOnlyList<SuggestionSpan> result = [new SuggestionSpan(new TextRange(0, text.Length), ["world"])];
            return Task.FromResult<IReadOnlyList<SuggestionSpan>?>(result);
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly RootElement _root;
        private readonly PipelineOwner _pipeline;
        public WidgetRenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView); _pipeline.Attach(RenderView);
            _root = new RootElement(RenderView, widget); _root.Attach(_owner); _root.Mount(null, null); _owner.FlushBuild();
        }
        public RenderView RenderView { get; }
        public void Pump(Size size) { _owner.FlushBuild(); _pipeline.RequestLayout(); _pipeline.FlushLayout(size); _pipeline.FlushCompositingBits(); _pipeline.FlushPaint(); }
        public void Dispose() => _root.Unmount();

        private sealed class RootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view; private Element? _child;
            public RootElement(RenderView view, Widget widget) : base(widget) => _view = view;
            public override RenderObject? RenderObject => _child?.RenderObject;
            public override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            public override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            public override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            public override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            public override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _view.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_view.Child, child)) _view.Child = null; }
            public override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }
}
