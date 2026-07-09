using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
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
    public void InputDecoration_ValidatesExclusiveSlotsAndCollapsedDefaults()
    {
        Assert.Throws<ArgumentException>(() => new InputDecoration(label: new Text("A"), labelText: "B"));
        Assert.Throws<ArgumentException>(() => new InputDecoration(prefix: new Text("A"), prefixText: "B"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputDecoration(errorMaxLines: 0));

        var collapsed = InputDecoration.Collapsed("Hint");
        Assert.True(collapsed.IsCollapsed);
        Assert.False(collapsed.Filled);
        Assert.Same(InputBorder.None, collapsed.Border);
        Assert.Equal(new Thickness(0), collapsed.ContentPadding);
    }

    [Fact]
    public void InputDecorator_BuildsLabelHintPrefixSuffixHelperAndCounter()
    {
        using var harness = new WidgetRenderHarness(Wrap(new InputDecorator(
            decoration: new InputDecoration(
                labelText: "Email",
                hintText: "name@example.com",
                prefixText: "<",
                suffixText: ">",
                helperText: "Helper",
                counterText: "0/20",
                border: new OutlineInputBorder()),
            isEmpty: true,
            child: new Text(""))));
        harness.Pump(new Size(360, 160));

        var text = FindDescendants<RenderParagraph>(harness.RenderView).Select(value => value.Text).ToList();
        Assert.Contains("Email", text);
        Assert.Contains("<", text);
        Assert.Contains(">", text);
        Assert.Contains("Helper", text);
        Assert.Contains("0/20", text);
        var painter = Assert.Single(FindDescendants<RenderCustomPaint>(harness.RenderView)).Painter;
        Assert.IsType<OutlineInputBorder>(Assert.IsType<InputBorderPainter>(painter).Border);
    }

    [Fact]
    public void InputDecorator_FocusAndErrorResolveBorderPrecedence()
    {
        var theme = ThemeData.Light with
        {
            InputDecorationTheme = new InputDecorationThemeData(
                Border: new OutlineInputBorder(),
                FillColor: Colors.LightBlue,
                Filled: true),
        };
        using var focused = new WidgetRenderHarness(Wrap(new InputDecorator(
            decoration: new InputDecoration(errorText: "Invalid"),
            isFocused: true,
            isEmpty: false,
            child: new Text("bad")), theme));
        focused.Pump(new Size(360, 120));

        var painter = Assert.IsType<InputBorderPainter>(Assert.Single(FindDescendants<RenderCustomPaint>(focused.RenderView)).Painter);
        Assert.Equal(theme.ErrorColor, painter.Border.BorderSide.Color);
        Assert.Equal(2, painter.Border.BorderSide.Width, precision: 3);
        Assert.Equal(Colors.LightBlue, painter.FillColor);
        Assert.Contains(FindDescendants<RenderParagraph>(focused.RenderView), value => value.Text == "Invalid");
    }

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
        Assert.Contains(FindDescendants<RenderParagraph>(harness.RenderView), value => value.Text == "3/3");

        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("Enter", true)));
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

        Assert.Contains(FindDescendants<RenderParagraph>(harness.RenderView), value => value.Text == "••••••");
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
        Assert.Contains(FindDescendants<RenderParagraph>(harness.RenderView), value => value.Text == "Name is required");

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
        Assert.Contains(FindDescendants<RenderParagraph>(harness.RenderView), value => value.Text == "custom: raw error");
        Assert.DoesNotContain(FindDescendants<RenderParagraph>(harness.RenderView), value => value.Text == "raw error");
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
            internal override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _view.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_view.Child, child)) _view.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }
}
