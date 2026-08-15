using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/test/widgets/autofill_group_test.dart
// flutter/packages/flutter/test/services/autofill_test.dart
[Collection(SchedulerTestCollection.Name)]
public sealed class AutofillTests : IDisposable
{
    private readonly List<MethodCall> _log = [];

    public AutofillTests()
    {
        FocusManager.Instance.ResetForTests();
        UI.TextInput.DebugReset();
        UI.TextInput.EnsureInitialized();
        SystemChannels.TextInput.SetPlatformMethodCallHandler(call =>
        {
            _log.Add(call);
            return Task.FromResult<object?>(null);
        });
    }

    public void Dispose()
    {
        SystemChannels.TextInput.SetPlatformMethodCallHandler(null);
        UI.TextInput.DebugReset();
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    // ------------------------------------------------------- AutofillConfiguration

    [Fact]
    public void AutofillConfiguration_DoesNotThrowIfTheHintListIsEmpty()
    {
        var configuration = new AutofillConfiguration(
            uniqueIdentifier: "id",
            autofillHints: [],
            currentEditingValue: new TextEditingValue());

        Assert.True(configuration.Enabled);
        Assert.Empty(configuration.AutofillHints);
        Assert.Null(configuration.HintText);
    }

    [Fact]
    public void AutofillConfiguration_DisabledCarriesFlutterDefaults()
    {
        AutofillConfiguration disabled = AutofillConfiguration.Disabled;

        Assert.False(disabled.Enabled);
        Assert.Equal(string.Empty, disabled.UniqueIdentifier);
        Assert.Empty(disabled.AutofillHints);
        Assert.Null(disabled.ToJson());
    }

    [Fact]
    public void AutofillConfiguration_EqualityAndHashCodeCompareEveryField()
    {
        var value = new TextEditingValue("value");
        AutofillConfiguration Make(string id, string hint, string? hintText) =>
            new(id, [hint], value, hintText);

        Assert.Equal(Make("id", AutofillHints.Username, null), Make("id", AutofillHints.Username, null));
        Assert.Equal(
            Make("id", AutofillHints.Username, null).GetHashCode(),
            Make("id", AutofillHints.Username, null).GetHashCode());
        Assert.NotEqual(Make("id", AutofillHints.Username, null), Make("other", AutofillHints.Username, null));
        Assert.NotEqual(Make("id", AutofillHints.Username, null), Make("id", AutofillHints.Password, null));
        Assert.NotEqual(Make("id", AutofillHints.Username, null), Make("id", AutofillHints.Username, "hint"));
        Assert.NotEqual(Make("id", AutofillHints.Username, null), AutofillConfiguration.Disabled);
    }

    [Fact]
    public void AutofillConfiguration_ToJsonEmitsFlutterKeys()
    {
        var configuration = new AutofillConfiguration(
            uniqueIdentifier: "field",
            autofillHints: [AutofillHints.Username],
            currentEditingValue: new TextEditingValue("bob"));

        Dictionary<string, object?> json = configuration.ToJson()!;

        Assert.Equal(["uniqueIdentifier", "hints", "editingValue"], json.Keys);
        Assert.Equal("field", json["uniqueIdentifier"]);
        Assert.Equal<IReadOnlyList<string>>([AutofillHints.Username], (IReadOnlyList<string>)json["hints"]!);
        var editingValue = (Dictionary<string, object?>)json["editingValue"]!;
        Assert.Equal("bob", editingValue["text"]);
        Assert.Equal(3, editingValue["selectionBase"]);
        Assert.Equal("TextAffinity.downstream", editingValue["selectionAffinity"]);
        Assert.Equal(-1, editingValue["composingBase"]);

        Dictionary<string, object?> withHint = new AutofillConfiguration(
            uniqueIdentifier: "field",
            autofillHints: [AutofillHints.Username],
            currentEditingValue: new TextEditingValue("bob"),
            hintText: "Your name").ToJson()!;
        Assert.Equal("Your name", withHint["hintText"]);
    }

    [Fact]
    public void TextInputConfiguration_ToJsonOmitsAutofillWhenDisabled()
    {
        Dictionary<string, object?> json = new TextInputConfiguration().ToJson();

        Assert.False(json.ContainsKey("autofill"));
        Assert.Null(json["viewId"]);
        Assert.Equal(false, json["readOnly"]);
        Assert.Equal("1", json["smartDashesType"]);
        Assert.Equal("TextInputAction.done", json["inputAction"]);
        Assert.Equal("TextCapitalization.none", json["textCapitalization"]);
        Assert.Equal("Brightness.light", json["keyboardAppearance"]);
        var inputType = (Dictionary<string, object?>)json["inputType"]!;
        Assert.Equal("TextInputType.text", inputType["name"]);

        Dictionary<string, object?> enabled = new TextInputConfiguration(
            autofillConfiguration: new AutofillConfiguration("id", [], new TextEditingValue())).ToJson();
        Assert.True(enabled.ContainsKey("autofill"));
    }

    // ------------------------------------------------------------- AutofillScope

    [Fact]
    public void AutofillScope_AttachSendsEveryClientConfigurationInRegistrationOrder()
    {
        var first = FakeAutofillClient.Enabled("field1", "one");
        var second = FakeAutofillClient.Enabled("field2", "two");
        var scope = new FakeAutofillScope();
        scope.Register(first);
        scope.Register(second);
        first.CurrentAutofillScope = scope;

        TextInputConnection connection = scope.Attach(first, first.TextInputConfiguration);

        MethodCall call = Assert.Single(_log);
        Assert.Equal("TextInput.setClient", call.Method);
        var arguments = (IReadOnlyList<object?>)call.Arguments!;
        Assert.Equal(1, arguments[0]);
        var configuration = (IReadOnlyDictionary<string, object?>)arguments[1]!;
        var fields = (IReadOnlyList<object?>)configuration["fields"]!;
        Assert.Equal(2, fields.Count);
        Assert.Equal(
            "field1",
            ((IReadOnlyDictionary<string, object?>)
                ((IReadOnlyDictionary<string, object?>)fields[0]!)["autofill"]!)["uniqueIdentifier"]);
        Assert.Equal(
            "field2",
            ((IReadOnlyDictionary<string, object?>)
                ((IReadOnlyDictionary<string, object?>)fields[1]!)["autofill"]!)["uniqueIdentifier"]);
        Assert.True(connection.Attached);
    }

    [Fact]
    public void AutofillScope_AttachRejectsAClientWithAutofillDisabled()
    {
        var enabled = FakeAutofillClient.Enabled("field1", "one");
        var disabled = new FakeAutofillClient("field2", new TextInputConfiguration());
        var scope = new FakeAutofillScope();
        scope.Register(enabled);
        scope.Register(disabled);

        Assert.Throws<InvalidOperationException>(
            () => scope.Attach(enabled, enabled.TextInputConfiguration));
    }

    [Fact]
    public void TextInput_UpdateEditingStateWithTagRoutesToTheTaggedClient()
    {
        var first = FakeAutofillClient.Enabled("field1", "one");
        var second = FakeAutofillClient.Enabled("field2", "two");
        var scope = new FakeAutofillScope();
        scope.Register(first);
        scope.Register(second);
        first.CurrentAutofillScope = scope;
        scope.Attach(first, first.TextInputConfiguration);

        // Flutter sends a client id of 0 while the connection id is 1; the tagged path ignores it.
        Inbound(new MethodCall(
            "TextInputClient.updateEditingStateWithTag",
            new List<object?>
            {
                0,
                new Dictionary<string, object?> { ["field2"] = new TextEditingValue("filled").ToJson() },
            }));

        Assert.Null(first.LatestAutofill);
        Assert.Equal("filled", second.LatestAutofill!.Value.Text);
    }

    [Fact]
    public void TextInput_UpdateEditingStateWithTagSkipsAnUnknownOrDisabledTag()
    {
        var client = FakeAutofillClient.Enabled("field1", "one");
        var disabled = new FakeAutofillClient("field2", new TextInputConfiguration());
        var scope = new FakeAutofillScope();
        scope.Register(client);
        scope.Register(disabled);
        client.CurrentAutofillScope = scope;
        UI.TextInput.Attach(client, client.TextInputConfiguration);

        Inbound(new MethodCall(
            "TextInputClient.updateEditingStateWithTag",
            new List<object?>
            {
                0,
                new Dictionary<string, object?>
                {
                    ["field2"] = new TextEditingValue("ignored").ToJson(),
                    ["missing"] = new TextEditingValue("ignored").ToJson(),
                },
            }));

        Assert.Null(disabled.LatestAutofill);
        Assert.Null(client.LatestAutofill);
    }

    [Fact]
    public void TextInput_FinishAutofillContextFansOutToEveryControl()
    {
        var control = new RecordingTextInputControl();
        UI.TextInput.SetInputControl(control);

        UI.TextInput.FinishAutofillContext();
        UI.TextInput.FinishAutofillContext(shouldSave: false);

        // The platform control stays registered while a custom control is installed, so both the
        // custom control and the channel see every request (Dart's `_inputControls` set).
        Assert.Equal([true, false], control.FinishCalls);
        Assert.Equal(2, _log.Count);

        _log.Clear();
        UI.TextInput.RestorePlatformInputControl();
        UI.TextInput.FinishAutofillContext(shouldSave: false);
        MethodCall call = Assert.Single(_log);
        Assert.Equal("TextInput.finishAutofillContext", call.Method);
        Assert.Equal(false, call.Arguments);
        Assert.Equal([true, false], control.FinishCalls);
    }

    // -------------------------------------------------------------- AutofillGroup

    [Fact]
    public void AutofillGroup_HasTheRightClients()
    {
        AutofillGroupState? outer = null;
        AutofillGroupState? inner = null;
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new AutofillGroup(new Builder(outerContext =>
            {
                outer = AutofillGroup.Of(outerContext);
                return new Column([
                    Field("outer", [AutofillHints.Username]),
                    Field("disabled", EditableText.AutofillDisabled),
                    new AutofillGroup(new Builder(innerContext =>
                    {
                        inner = AutofillGroup.Of(innerContext);
                        return Field("inner", [AutofillHints.Password]);
                    })),
                ]);
            }))));
        harness.Pump(new Size(400, 400));

        Assert.Single(outer!.AutofillClients);
        Assert.Single(inner!.AutofillClients);
        Assert.NotSame(outer.AutofillClients.First(), inner.AutofillClients.First());
    }

    [Fact]
    public void AutofillGroup_NewClientsCanBeAddedAndRemovedFromAScope()
    {
        AutofillGroupState? group = null;
        StateSetter? setState = null;
        bool enabled = true;
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new AutofillGroup(new Builder(context =>
            {
                group = AutofillGroup.Of(context);
                return new StatefulBuilder((_, setter) =>
                {
                    setState = setter;
                    return new Column([
                        Field("always", [AutofillHints.Username]),
                        Field("toggled", enabled ? [AutofillHints.Password] : EditableText.AutofillDisabled),
                    ]);
                });
            }))));
        harness.Pump(new Size(400, 400));

        Assert.Equal(2, group!.AutofillClients.Count());

        setState!(() => enabled = false);
        harness.Pump(new Size(400, 400));
        Assert.Single(group.AutofillClients);

        setState(() => enabled = true);
        harness.Pump(new Size(400, 400));
        Assert.Equal(2, group.AutofillClients.Count());
    }

    [Fact]
    public void AutofillGroup_HasTheRightClientsAfterReparenting()
    {
        AutofillGroupState? outer = null;
        AutofillGroupState? inner = null;
        StateSetter? setState = null;
        bool nested = true;
        var movedKey = new ProbeKey(1);
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new AutofillGroup(new Builder(outerContext =>
            {
                outer = AutofillGroup.Of(outerContext);
                return new StatefulBuilder((_, setter) =>
                {
                    setState = setter;
                    Widget moved = Field("moved", [AutofillHints.Username], movedKey);
                    return new Column([
                        Field("outer", [AutofillHints.Email]),
                        nested ? new SizedBox() : moved,
                        new AutofillGroup(new Builder(innerContext =>
                        {
                            inner = AutofillGroup.Of(innerContext);
                            return new Column([
                                Field("inner", [AutofillHints.Password]),
                                nested ? moved : new SizedBox(),
                            ]);
                        })),
                    ]);
                });
            }))));
        harness.Pump(new Size(400, 600));

        Assert.Single(outer!.AutofillClients);
        Assert.Equal(2, inner!.AutofillClients.Count());

        setState!(() => nested = false);
        harness.Pump(new Size(400, 600));

        Assert.Equal(2, outer.AutofillClients.Count());
        Assert.Single(inner.AutofillClients);
    }

    [Fact]
    public void AutofillGroup_RegisteringTheSameIdTwiceKeepsTheFirstClient()
    {
        var scope = new FakeAutofillScope();
        var first = FakeAutofillClient.Enabled("shared", "one");
        var second = FakeAutofillClient.Enabled("shared", "two");
        AutofillGroupState state = new();

        state.Register(first);
        state.Register(second);

        Assert.Same(first, state.GetAutofillClient("shared"));
        Assert.Same(first, Assert.Single(state.AutofillClients));
        state.Unregister("shared");
        Assert.Null(state.GetAutofillClient("shared"));
        Assert.Empty(scope.AutofillClients);
    }

    [Fact]
    public void AutofillGroup_OfThrowsWithoutAnAncestor()
    {
        Exception? thrown = null;
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new Builder(context =>
            {
                Assert.Null(AutofillGroup.MaybeOf(context));
                thrown = Record.Exception(() => AutofillGroup.Of(context));
                return new SizedBox();
            })));
        harness.Pump(new Size(100, 100));

        Assert.IsType<InvalidOperationException>(thrown);
    }

    [Fact]
    public void AutofillGroup_BuildingAGroupFinishesNoAutofillContext()
    {
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new AutofillGroup(Field("field", [AutofillHints.Username]))));
        harness.Pump(new Size(200, 200));

        Assert.DoesNotContain(_log, call => call.Method == "TextInput.finishAutofillContext");
    }

    [Theory]
    [InlineData(AutofillContextAction.Commit, true)]
    [InlineData(AutofillContextAction.Cancel, false)]
    public void AutofillGroup_DisposingTheTopmostGroupFinishesTheAutofillContext(
        AutofillContextAction action,
        bool shouldSave)
    {
        StateSetter? setState = null;
        bool showGroup = true;
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new StatefulBuilder((_, setter) =>
            {
                setState = setter;
                return showGroup
                    ? new AutofillGroup(Field("field", [AutofillHints.Username]), onDisposeAction: action)
                    : new SizedBox();
            })));
        harness.Pump(new Size(200, 200));
        _log.Clear();

        setState!(() => showGroup = false);
        harness.Pump(new Size(200, 200));

        MethodCall call = Assert.Single(
            _log.Where(entry => entry.Method == "TextInput.finishAutofillContext"));
        Assert.Equal(shouldSave, call.Arguments);
    }

    [Fact]
    public void AutofillGroup_DisposingANestedGroupFinishesNoAutofillContext()
    {
        StateSetter? setState = null;
        bool showInner = true;
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new AutofillGroup(new StatefulBuilder((_, setter) =>
            {
                setState = setter;
                return showInner
                    ? new AutofillGroup(Field("inner", [AutofillHints.Username]))
                    : new SizedBox();
            }))));
        harness.Pump(new Size(200, 200));
        _log.Clear();

        setState!(() => showInner = false);
        harness.Pump(new Size(200, 200));

        Assert.DoesNotContain(_log, call => call.Method == "TextInput.finishAutofillContext");
    }

    // --------------------------------------------------------------- EditableText

    [Fact]
    public void EditableText_RequestsAutofillOnFocusByDefault()
    {
        var focusNode = new FocusNode();
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new AutofillGroup(new EditableText(new TextEditingController(), focusNode: focusNode))));
        harness.Pump(new Size(300, 100));
        _log.Clear();

        focusNode.RequestFocus();
        harness.Pump(new Size(300, 100));

        Assert.Contains(_log, call => call.Method == "TextInput.setClient");
        Assert.Contains(_log, call => call.Method == "TextInput.requestAutofill");
    }

    [Fact]
    public void EditableText_AutofillCanBeDisabled()
    {
        var focusNode = new FocusNode();
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new AutofillGroup(new EditableText(
                new TextEditingController(),
                focusNode: focusNode,
                autofillHints: EditableText.AutofillDisabled))));
        harness.Pump(new Size(300, 100));
        _log.Clear();

        focusNode.RequestFocus();
        harness.Pump(new Size(300, 100));

        Assert.Contains(_log, call => call.Method == "TextInput.setClient");
        Assert.DoesNotContain(_log, call => call.Method == "TextInput.requestAutofill");
    }

    [Fact]
    public void EditableText_AutofillWritesThroughToTheController()
    {
        var controller = new TextEditingController();
        AutofillGroupState? group = null;
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new AutofillGroup(new Builder(context =>
            {
                group = AutofillGroup.Of(context);
                return new EditableText(controller, autofillHints: [AutofillHints.Username]);
            }))));
        harness.Pump(new Size(300, 100));

        IAutofillClient client = Assert.Single(group!.AutofillClients);
        client.Autofill(new TextEditingValue("filled"));

        Assert.Equal("filled", controller.Text);
    }

    [Fact]
    public void EditableText_InfersKeyboardTypeFromAutofillHintsOnNonApplePlatforms()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;

        Assert.Equal(
            TextInputType.Text,
            EditableText.InferKeyboardType(null, multiline: false));
        Assert.Equal(
            TextInputType.Multiline,
            EditableText.InferKeyboardType([], multiline: true));
        Assert.Equal(
            TextInputType.EmailAddress,
            EditableText.InferKeyboardType([AutofillHints.Email], multiline: false));
        Assert.Equal(
            TextInputType.Phone,
            EditableText.InferKeyboardType([AutofillHints.TelephoneNumber], multiline: false));
        Assert.Equal(
            TextInputType.StreetAddress,
            EditableText.InferKeyboardType([AutofillHints.FullStreetAddress], multiline: false));
        Assert.Equal(
            TextInputType.Datetime,
            EditableText.InferKeyboardType([AutofillHints.Birthday], multiline: false));
        Assert.Equal(
            TextInputType.Name,
            EditableText.InferKeyboardType([AutofillHints.GivenName], multiline: false));
        Assert.Equal(
            TextInputType.Url,
            EditableText.InferKeyboardType([AutofillHints.Impp], multiline: false));
        Assert.Equal(
            TextInputType.Text,
            EditableText.InferKeyboardType(["not-a-hint"], multiline: false));

        // A multiline field ignores the hint table on non-Apple platforms.
        Assert.Equal(
            TextInputType.Multiline,
            EditableText.InferKeyboardType([AutofillHints.Email], multiline: true));
    }

    [Fact]
    public void EditableText_InfersKeyboardTypeFromAutofillHintsOnApplePlatforms()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;

        Assert.Equal(
            TextInputType.Name,
            EditableText.InferKeyboardType([AutofillHints.TelephoneNumber], multiline: false));
        Assert.Equal(
            TextInputType.Number,
            EditableText.InferKeyboardType([AutofillHints.OneTimeCode], multiline: false));
        Assert.Equal(
            TextInputType.Text,
            EditableText.InferKeyboardType([AutofillHints.Password], multiline: false));

        // The Apple table wins over the multiline short circuit.
        Assert.Equal(
            TextInputType.Name,
            EditableText.InferKeyboardType([AutofillHints.GivenName], multiline: true));

        // A hint outside the Apple table falls through to the general one.
        Assert.Equal(
            TextInputType.Datetime,
            EditableText.InferKeyboardType([AutofillHints.Birthday], multiline: false));
    }

    [Fact]
    public void EditableText_InfersAutocorrectFromPasswordHintsOnIOSOnly()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        Assert.False(EditableText.InferAutocorrect([AutofillHints.Password]));
        Assert.False(EditableText.InferAutocorrect([AutofillHints.NewPassword]));
        Assert.False(EditableText.InferAutocorrect([AutofillHints.Username]));
        Assert.True(EditableText.InferAutocorrect([AutofillHints.NewUsername]));
        Assert.True(EditableText.InferAutocorrect([]));
        Assert.True(EditableText.InferAutocorrect(null));

        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
        Assert.True(EditableText.InferAutocorrect([AutofillHints.Password]));
    }

    // ------------------------------------------------------------------- helpers

    private static Widget Field(string id, IReadOnlyList<string>? hints, Key? key = null) =>
        new EditableText(
            new TextEditingController(text: id),
            autofillHints: hints,
            key: key);

    private void Inbound(MethodCall call)
    {
        SystemChannels.TextInput.BinaryMessenger.HandlePlatformMessage(
            SystemChannels.TextInput.Name,
            SystemChannels.TextInput.Codec.EncodeMethodCall(call),
            null);
    }

    private sealed record ProbeKey(int Id) : GlobalKey<EditableText.EditableTextState>;

    private sealed class RecordingTextInputControl : TextInputControl
    {
        public List<bool> FinishCalls { get; } = [];

        public override void FinishAutofillContext(bool shouldSave = true) => FinishCalls.Add(shouldSave);
    }

    private sealed class FakeAutofillClient : IAutofillClient, ITextInputClient
    {
        public FakeAutofillClient(string autofillId, TextInputConfiguration configuration)
        {
            AutofillId = autofillId;
            TextInputConfiguration = configuration;
        }

        public static FakeAutofillClient Enabled(string autofillId, string text) =>
            new(
                autofillId,
                new TextInputConfiguration(
                    autofillConfiguration: new AutofillConfiguration(
                        uniqueIdentifier: autofillId,
                        autofillHints: [AutofillHints.Username],
                        currentEditingValue: new TextEditingValue(text))));

        public string AutofillId { get; }

        public TextInputConfiguration TextInputConfiguration { get; }

        public TextEditingValue? LatestAutofill { get; private set; }

        public IAutofillScope? CurrentAutofillScope { get; set; }

        public void Autofill(TextEditingValue newEditingValue) => LatestAutofill = newEditingValue;

        public void UpdateEditingValue(TextEditingValue value) => LatestAutofill = value;

        public void PerformAction(TextInputActionType action)
        {
        }

        public void ConnectionClosed()
        {
        }

        public TextEditingValue? CurrentTextEditingValue =>
            TextInputConfiguration.AutofillConfiguration.CurrentEditingValue;

        public void PerformPrivateCommand(string action, System.Collections.IDictionary data)
        {
        }

        public void UpdateFloatingCursor(RawFloatingCursorPoint point)
        {
        }

        public void ShowAutocorrectionPromptRect(int start, int end)
        {
        }
    }

    private sealed class FakeAutofillScope : IAutofillScope
    {
        private readonly List<IAutofillClient> _clients = [];

        public IEnumerable<IAutofillClient> AutofillClients => _clients;

        public void Register(IAutofillClient client) => _clients.Add(client);

        public IAutofillClient? GetAutofillClient(string autofillId) =>
            _clients.FirstOrDefault(client => client.AutofillId == autofillId);

        public TextInputConnection Attach(ITextInputClient trigger, TextInputConfiguration configuration) =>
            AutofillScopeMixin.Attach(this, trigger, configuration);
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

        public void Dispose() => _root.Unmount();

        private sealed class RootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;

            public RootElement(RenderView view, Widget widget) : base(widget)
            {
                _view = view;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            internal override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            internal override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, Slot);
            }

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot) =>
                _view.Child = (RenderBox)child;

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot) => _view.Child = null;
        }
    }
}
