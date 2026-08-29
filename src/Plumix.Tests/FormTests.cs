using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class FormTests : IDisposable
{
    public FormTests() => FocusManager.Instance.ResetForTests();
    public void Dispose() => FocusManager.Instance.ResetForTests();

    [Fact]
    public void FormField_ValidateSaveResetAndClearErrorFollowFlutterLifecycle()
    {
        FormState? formState = null;
        FormFieldState<string>? fieldState = null;
        string? saved = null;
        int resets = 0;
        int formChanges = 0;
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new Form(
                onChanged: () => formChanges++,
                child: new Builder(context =>
                {
                    formState = Form.Of(context);
                    return new FormField<string>(
                        initialValue: "seed",
                        validator: value => string.IsNullOrWhiteSpace(value) ? "Required" : null,
                        onSaved: value => saved = value,
                        onReset: () => resets++,
                        builder: field =>
                        {
                            fieldState = field;
                            return new Text(field.ErrorText ?? field.Value ?? string.Empty);
                        });
                }))));
        harness.Pump(new Size(320, 120));

        Assert.Same(fieldState, Assert.Single(formState!.Fields));
        Assert.True(formState.Validate());
        fieldState!.DidChange(string.Empty);
        Assert.Equal(1, formChanges);
        Assert.False(formState.Validate());
        Assert.Equal("Required", fieldState.ErrorText);
        Assert.True(fieldState.HasInteractedByUser);
        Assert.Single(formState.ValidateGranularly());

        formState.Save();
        Assert.Equal(string.Empty, saved);
        formState.ClearError();
        Assert.Null(fieldState.ErrorText);
        Assert.False(fieldState.HasInteractedByUser);

        formState.Reset();
        Assert.Equal("seed", fieldState.Value);
        Assert.Equal(1, resets);
        Assert.True(formChanges >= 3);
    }

    [Fact]
    public void FormField_ForcedErrorOverridesValidatorAndMarksSemanticsInvalid()
    {
        FormFieldState<string>? fieldState = null;
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new FormField<string>(
                initialValue: "valid",
                forceErrorText: "Server error",
                validator: _ => null,
                builder: field =>
                {
                    fieldState = field;
                    return new Text(field.ErrorText ?? "ok");
                })));
        var semantics = harness.PumpAndGetSemantics(new Size(320, 80));

        Assert.False(fieldState!.IsValid);
        Assert.False(fieldState.Validate());
        Assert.Equal("Server error", fieldState.ErrorText);
        Assert.NotNull(FindSemantics(semantics, node => node.Flags.HasFlag(SemanticsFlags.IsInvalid)));
    }

    [Fact]
    public void FormField_OnUnfocusValidatesAfterDescendantLosesFocus()
    {
        var fieldFocus = new FocusNode();
        var nextFocus = new FocusNode();
        FormFieldState<string>? fieldState = null;
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new FormField<string>(
                initialValue: string.Empty,
                autovalidateMode: AutovalidateMode.OnUnfocus,
                validator: _ => "Lost focus",
                builder: field =>
                {
                    fieldState = field;
                    return new Focus(focusNode: fieldFocus, child: new Text("field"));
                })));
        harness.Pump(new Size(320, 80));

        Assert.True(fieldFocus.RequestFocus());
        Assert.Null(fieldState!.ErrorText);
        Assert.True(nextFocus.RequestFocus());
        harness.Pump(new Size(320, 80));
        Assert.Equal("Lost focus", fieldState.ErrorText);

        fieldFocus.Dispose();
        nextFocus.Dispose();
    }

    [Fact]
    public void Form_GlobalKeyExposesCurrentStateContextAndWidget()
    {
        var key = new LabeledGlobalKey<FormState>("form");
        using (var harness = new WidgetRenderHarness(new Directionality(
                   TextDirection.Ltr,
                   new Form(key: key, child: new Text("form")))))
        {
            harness.Pump(new Size(320, 80));
            Assert.NotNull(key.CurrentState);
            Assert.NotNull(key.CurrentContext);
            Assert.IsType<Form>(key.CurrentWidget);
        }

        Assert.Null(key.CurrentState);
        Assert.Null(key.CurrentContext);
        Assert.Null(key.CurrentWidget);
    }

    [Fact]
    public void FormField_RestoresErrorTextAndInteractionStateThroughItsRestorationId()
    {
        var manager = new MockRestorationManager();
        var rawData = RawRestorationData.Build();

        FormFieldState<string>? fieldState = null;
        Dictionary<object, object?>? snapshot = null;
        using (var harness = new WidgetRenderHarness(new UnmanagedRestorationScope(
                   bucket: RestorationBucket.Root(manager, rawData),
                   child: new Directionality(
                       TextDirection.Ltr,
                       new Form(child: new FormField<string>(
                           restorationId: "field",
                           initialValue: "seed",
                           validator: value => string.IsNullOrWhiteSpace(value) ? "Required" : null,
                           builder: field =>
                           {
                               fieldState = field;
                               return new Text(field.ErrorText ?? field.Value ?? string.Empty);
                           }))))))
        {
            harness.Pump(new Size(320, 120));
            fieldState!.DidChange(string.Empty);
            Assert.False(fieldState.Validate());
            Assert.Equal("Required", fieldState.ErrorText);
            manager.DoSerialization();
            snapshot = RestorationSerialization.CopyRestorationData(rawData);
        }

        Dictionary<object, object?> fieldData = RawRestorationData.Values(
            RawRestorationData.Child(snapshot!, "field")!)!;
        Assert.Equal("Required", fieldData["error_text"]);
        Assert.Equal(true, fieldData["has_interacted_by_user"]);

        FormFieldState<string>? restored = null;
        using var restart = new WidgetRenderHarness(new UnmanagedRestorationScope(
            bucket: RestorationBucket.Root(manager, snapshot),
            child: new Directionality(
                TextDirection.Ltr,
                new Form(child: new FormField<string>(
                    restorationId: "field",
                    initialValue: "seed",
                    validator: value => string.IsNullOrWhiteSpace(value) ? "Required" : null,
                    builder: field =>
                    {
                        restored = field;
                        return new Text(field.ErrorText ?? field.Value ?? string.Empty);
                    })))));
        restart.Pump(new Size(320, 120));

        Assert.NotSame(fieldState, restored);
        Assert.Equal("Required", restored!.ErrorText);
        Assert.True(restored.HasInteractedByUser);
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
        private readonly RootElement _root;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new RootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(null, null);
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

        public void Dispose() => _root.Unmount();

        private sealed class RootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;
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
