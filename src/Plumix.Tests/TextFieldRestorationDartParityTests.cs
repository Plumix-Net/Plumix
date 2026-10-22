using Avalonia;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: material_ui/test/text_field_restoration_test.dart
// Dart parity source: material_ui/test/text_form_field_restoration_test.dart
// Dart parity source: cupertino_ui/test/text_field_restoration_test.dart
// Dart parity source: cupertino_ui/test/text_form_field_row_restoration_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class TextFieldRestorationDartParityTests : IDisposable
{
    public TextFieldRestorationDartParityTests() => SchedulerBinding.ResetForTests();

    public void Dispose() => SchedulerBinding.ResetForTests();

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    public void Fields_RestoreTextAndScrollAfterRestartAndOlderSnapshot(int kind, bool external)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Wrap(new RestorableField(kind, external)));
        tester.PumpAndSettle();
        Assert.Equal(string.Empty, Editor(tester).Controller.Text);
        Assert.Equal(0, Scroll(tester).Position.Pixels);

        string text = string.Join('\n', Enumerable.Repeat("Hello World! How are you? Life is good!", 12));
        tester.EnterText(Find.ByType<EditableText>(), text);
        tester.PumpAndSettle();
        Editor(tester).FocusNode!.Unfocus();
        tester.PumpAndSettle();
        Assert.True(Scroll(tester).Position.MaxScrollExtent >= 60);
        Scroll(tester).Position.JumpTo(60);
        tester.Pump();
        State previousState = tester.State<State>(Find.ByType<EditableText>());

        tester.RestartAndRestore();
        tester.PumpAndSettle();
        Assert.NotSame(previousState, tester.State<State>(Find.ByType<EditableText>()));
        Assert.Equal(text, Editor(tester).Controller.Text);
        Assert.Equal(60, Scroll(tester).Position.Pixels, precision: 3);
        Assert.Equal(TextRange.Empty, Editor(tester).Controller.Composing);

        TestRestorationData saved = tester.GetRestorationData();
        tester.EnterText(Find.ByType<EditableText>(), "Everything is awesome!!");
        tester.PumpAndSettle();
        Editor(tester).FocusNode!.Unfocus();
        tester.PumpAndSettle();
        Scroll(tester).Position.JumpTo(0);
        tester.Pump();
        Assert.NotEqual(60, Scroll(tester).Position.Pixels);

        tester.RestoreFrom(saved);
        tester.PumpAndSettle();
        Assert.Equal(text, Editor(tester).Controller.Text);
        Assert.Equal(60, Scroll(tester).Position.Pixels, precision: 3);
        Assert.Null(tester.TakeException());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Fields_UseTheirOwnNestedBucketsAndNullIdsMaskAncestorRestoration(int kind)
    {
        using var tester = new FrameworkDartTester();
        Widget Build(string? id) => Wrap(new Column(children:
        [
            Field(kind, id: id),
            Field(kind, id: id is null ? null : "second"),
        ]));
        tester.PumpWidget(Build("first"));
        tester.PumpAndSettle();
        var states = tester.StateList<ScrollableState>(Find.ByType<Scrollable>()).ToList();
        Assert.Equal(2, states.Count);
        Assert.NotSame(states[0].Bucket, states[1].Bucket);
        Assert.All(states, state => Assert.Equal("editable", state.Bucket!.RestorationId));
        var first = (RestorationState)tester.FirstState<State>(Find.ByWidgetPredicate(widget =>
            kind switch
            {
                0 => widget is TextField,
                1 => widget is CupertinoTextField,
                2 => widget is TextFormField,
                _ => widget is CupertinoTextFormFieldRow,
            }));
        Dictionary<object, object?> data = first.Bucket!.RawData;
        if (kind >= 2)
        {
            Assert.Equal("first", kind == 2
                ? tester.FirstWidget<TextField>(Find.ByType<TextField>()).RestorationId
                : tester.FirstWidget<CupertinoTextField>(Find.ByType<CupertinoTextField>()).RestorationId);
            data = RawRestorationData.Child(data, "first")!;
        }
        Assert.Same(states[0].Bucket!.RawData, RawRestorationData.Child(data, "editable"));
        Assert.Null(tester.TakeException());

        tester.PumpWidget(Build(null));
        tester.PumpAndSettle();
        Assert.All(tester.StateList<ScrollableState>(Find.ByType<Scrollable>()), state => Assert.Null(state.Bucket));
        Assert.Null(tester.TakeException());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ControllerChanges_PreserveFullValueAndRemoveObsoleteRestorationData(int kind)
    {
        using var tester = new FrameworkDartTester();
        using var first = TextEditingController.FromValue(new TextEditingValue(
            "external", new TextSelection(1, 5), new TextRange(0, 6)));
        using var second = new TextEditingController("replacement");
        Widget Build(TextEditingController? controller) => Wrap(Field(kind, controller));
        tester.PumpWidget(Build(first));
        tester.PumpAndSettle();
        TextEditingValue original = first.Value;
        State state = tester.State<State>(FieldFinder(kind));
        tester.PumpWidget(Build(null));
        tester.PumpAndSettle();
        Assert.Same(state, tester.State<State>(FieldFinder(kind)));
        TextEditingController owned = Editor(tester).Controller;
        Assert.NotSame(first, owned);
        Assert.Equal(original, owned.Value);
        var restorable = (RestorationState)state;
        Assert.Equal("external", restorable.Bucket!.Read<string>("controller"));
        first.Text = "detached";
        tester.Pump();
        Assert.Equal("external", owned.Text);

        tester.PumpWidget(Build(second));
        tester.PumpAndSettle();
        Assert.Same(second, Editor(tester).Controller);
        Assert.False(restorable.Bucket.Contains("controller"));
        if (kind >= 2)
        {
            Assert.Equal("replacement", ((FormFieldState<string>)state).Value);
        }
        tester.PumpWidget(new SizedBox());
        first.Text = "first survives";
        second.Text = "second survives";
        Assert.Equal("second survives", second.Text);
        Assert.Null(tester.TakeException());
    }

    [Theory]
    [InlineData(2, AutovalidateMode.Disabled, false)]
    [InlineData(2, AutovalidateMode.OnUserInteraction, false)]
    [InlineData(2, AutovalidateMode.Always, false)]
    [InlineData(2, AutovalidateMode.OnUserInteraction, true)]
    [InlineData(3, AutovalidateMode.Disabled, false)]
    [InlineData(3, AutovalidateMode.OnUserInteraction, false)]
    [InlineData(3, AutovalidateMode.Always, false)]
    [InlineData(3, AutovalidateMode.OnUserInteraction, true)]
    public void FormFields_RestoreValueErrorsAndInteractionAndResetToInitialValue(
        int kind, AutovalidateMode mode, bool formAncestor)
    {
        using var tester = new FrameworkDartTester();
        Widget field = Field(kind, initialValue: "foo", mode: mode, validator: value => $"{value}/error");
        tester.PumpWidget(Wrap(formAncestor ? new Form(child: field) : field));
        tester.PumpAndSettle();
        FormFieldState<string> State() => tester.State<FormFieldState<string>>(FieldFinder(kind));
        Assert.Equal(mode == AutovalidateMode.Always ? "foo/error" : null, State().ErrorText);

        foreach (string text in new[] { "bar", "Test", string.Empty })
        {
            State().Reset();
            tester.PumpAndSettle();
            tester.EnterText(Find.ByType<EditableText>(), text);
            tester.PumpAndSettle();
            Assert.Equal(mode == AutovalidateMode.Disabled ? null : $"{text}/error", State().ErrorText);
            Assert.True(State().HasInteractedByUser);
            Assert.False(State().Validate());
            tester.PumpAndSettle();
            TestRestorationData saved = tester.GetRestorationData();

            tester.RestartAndRestore();
            tester.PumpAndSettle();
            Assert.Equal(text, State().Value);
            Assert.Equal(text, Editor(tester).Controller.Text);
            Assert.Equal($"{text}/error", State().ErrorText);
            Assert.True(State().HasInteractedByUser);

            State().Reset();
            tester.PumpAndSettle();
            Assert.Equal("foo", State().Value);
            Assert.Equal("foo", Editor(tester).Controller.Text);
            Assert.False(State().HasInteractedByUser);
            Assert.Equal(mode == AutovalidateMode.Always ? "foo/error" : null, State().ErrorText);
            tester.RestartAndRestore();
            tester.PumpAndSettle();
            Assert.Equal("foo", State().Value);
            Assert.False(State().HasInteractedByUser);
            Assert.Equal(mode == AutovalidateMode.Always ? "foo/error" : null, State().ErrorText);

            tester.RestoreFrom(saved);
            tester.PumpAndSettle();
            Assert.Equal(text, State().Value);
            Assert.Equal($"{text}/error", State().ErrorText);
            Assert.True(State().HasInteractedByUser);
        }
        Assert.Null(tester.TakeException());
    }

    [Fact]
    public void MaterialForm_LocalProgrammaticTextChangesDoNotMarkUserInteraction()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Wrap(Field(2, initialValue: "foo")));
        tester.PumpAndSettle();
        FormFieldState<string> state = tester.State<FormFieldState<string>>(Find.ByType<TextFormField>());
        Editor(tester).Controller.Text = "programmatic";
        tester.Pump();
        Assert.False(state.HasInteractedByUser);
        Assert.Equal("foo", state.Value);
        tester.RestartAndRestore();
        tester.PumpAndSettle();
        state = tester.State<FormFieldState<string>>(Find.ByType<TextFormField>());
        Assert.Equal("programmatic", state.Value);
        Assert.False(state.HasInteractedByUser);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void FormFields_ExternalSelectionChangesDoNotMarkInteractionAndResetCallsOnChanged(int kind)
    {
        using var tester = new FrameworkDartTester();
        using var controller = new TextEditingController("foo");
        var changes = new List<string>();
        Widget Build() => Wrap(Field(kind, controller, onChanged: changes.Add));
        tester.PumpWidget(Build());
        tester.PumpAndSettle();
        FormFieldState<string> state = tester.State<FormFieldState<string>>(FieldFinder(kind));
        controller.Selection = new TextSelection(0, 2);
        tester.Pump();
        Assert.False(state.HasInteractedByUser);
        controller.Text = "changed";
        tester.Pump();
        Assert.Equal("changed", state.Value);
        Assert.True(state.HasInteractedByUser);
        tester.PumpWidget(Build());
        state.Reset();
        tester.PumpAndSettle();
        string expected = kind == 2 ? "foo" : "changed";
        Assert.Equal(expected, controller.Text);
        Assert.Equal(expected, Assert.Single(changes));
        Assert.False(state.HasInteractedByUser);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void FormFields_ExternalResetNotifiesFormBeforeAndAfterClearingInteraction(int kind)
    {
        using var tester = new FrameworkDartTester();
        using var controller = new TextEditingController("foo");
        var notifications = new List<bool>();
        FormFieldState<string>? state = null;
        tester.PumpWidget(Wrap(new Form(
            onChanged: () => notifications.Add(state!.HasInteractedByUser),
            child: Field(kind, controller))));
        tester.PumpAndSettle();
        state = tester.State<FormFieldState<string>>(FieldFinder(kind));
        controller.Text = "bar";
        notifications.Clear();
        state.Reset();
        Assert.Equal(new[] { true, false }, notifications);
        Assert.Equal("foo", controller.Text);
        Assert.Equal("foo", state.Value);
    }

    [Fact]
    public void CupertinoField_NonemptyOwnedControllerKeepsStateAliveUntilClearedOrDeactivated()
    {
        using var tester = new FrameworkDartTester();
        var handles = new List<KeepAliveHandle>();
        tester.PumpWidget(Wrap(new NotificationListener<KeepAliveNotification>(
            onNotification: notification =>
            {
                handles.Add(notification.Handle);
                return true;
            },
            child: Field(1))));
        tester.PumpAndSettle();
        Assert.Empty(handles);
        Editor(tester).Controller.Text = "keep";
        Assert.False(Assert.Single(handles).IsReleased);
        Editor(tester).Controller.Clear();
        Assert.True(handles[0].IsReleased);
        Editor(tester).Controller.Text = "again";
        Assert.Equal(2, handles.Count);
        Assert.False(handles[1].IsReleased);
        tester.PumpAndSettle();
        tester.RestartAndRestore();
        tester.PumpAndSettle();
        Assert.True(handles[1].IsReleased);
        Assert.Equal(3, handles.Count);
        Assert.False(handles[2].IsReleased);
        tester.PumpWidget(new SizedBox());
        Assert.True(handles[2].IsReleased);
        Assert.Null(tester.TakeException());
    }

    private static EditableText Editor(FrameworkDartTester tester) =>
        tester.Widget<EditableText>(Find.ByType<EditableText>());

    private static ScrollableState Scroll(FrameworkDartTester tester) =>
        tester.State<ScrollableState>(Find.ByType<Scrollable>());

    private static Finder FieldFinder(int kind) => kind switch
    {
        0 => Find.ByType<TextField>(),
        1 => Find.ByType<CupertinoTextField>(),
        2 => Find.ByType<TextFormField>(),
        _ => Find.ByType<CupertinoTextFormFieldRow>(),
    };

    private static Widget Wrap(Widget child) => new MaterialApp(
        restorationScopeId: "app",
        home: new Plumix.Material.Material(child: new Center(child: new SizedBox(width: 250, child: child))));

    private static Widget Field(
        int kind,
        TextEditingController? controller = null,
        string? id = "text",
        string? initialValue = null,
        AutovalidateMode mode = AutovalidateMode.Disabled,
        FormFieldValidator<string>? validator = null,
        Action<string>? onChanged = null) => kind switch
        {
            0 => new TextField(controller: controller, restorationId: id, maxLines: 3),
            1 => new CupertinoTextField(controller: controller, restorationId: id, maxLines: 3),
            2 => new TextFormField(
                controller: controller, restorationId: id, initialValue: initialValue, maxLines: 3,
                autovalidateMode: mode, validator: validator, onChanged: onChanged),
            _ => new CupertinoTextFormFieldRow(
                controller: controller, restorationId: id, initialValue: initialValue, maxLines: 3,
                autovalidateMode: mode, validator: validator, onChanged: onChanged),
        };

    private sealed class RestorableField(int kind, bool external) : StatefulWidget
    {
        public int Kind { get; } = kind;
        public bool External { get; } = external;
        public override State CreateState() => new RestorableFieldState();
    }

    private sealed class RestorableFieldState : RestorationState<RestorableField>
    {
        private readonly RestorableTextEditingController _controller = new();
        protected override string? RestorationId => "widget";

        protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore) =>
            RegisterForRestoration(_controller, "controller");

        public override Widget Build(BuildContext context) => new UnmanagedRestorationScope(
            bucket: Bucket,
            child: Field(Widget.Kind, Widget.External ? _controller.Value : null));

        public override void Dispose()
        {
            _controller.Dispose();
            base.Dispose();
        }
    }
}
