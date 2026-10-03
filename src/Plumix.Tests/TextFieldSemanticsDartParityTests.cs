// Dart parity source: material_ui/lib/src/text_field.dart (_TextFieldState.build semantics),
// flutter/packages/flutter/lib/src/widgets/editable_text.dart, flutter/packages/flutter/lib/src/rendering/editable.dart
// Ports the semantics cases of material-ui-src/test/text_field_test.dart.

using Avalonia;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using static Plumix.Tests.SemanticsMatchers;
using MaterialWidget = Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class TextFieldSemanticsDartParityTests : IDisposable
{
    private const SemanticsFlags TextFieldFlags = SemanticsFlags.IsTextField
                                                  | SemanticsFlags.IsFocusable
                                                  | SemanticsFlags.HasEnabledState
                                                  | SemanticsFlags.IsEnabled;

    private readonly List<OverlayEntry> _entries = [];
    private readonly FrameworkDartTester _tester;

    public TextFieldSemanticsDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        // flutter_test: defaultTargetPlatform == android.
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        _tester = new FrameworkDartTester(fakeGestureTimers: true, semanticsEnabled: true, registerTestTextInput: true);
    }

    public void Dispose()
    {
        foreach (OverlayEntry entry in _entries)
        {
            entry.Remove();
            entry.Dispose();
        }

        _entries.Clear();
        _tester.Dispose();
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    // text_field_test.dart's `overlay`.
    private Widget Overlay(Widget child)
    {
        var entry = new OverlayEntry(builder: context => new Center(child: new MaterialWidget(child: child)));
        _entries.Add(entry);
        return new Localizations(
            locale: new Locale("en", "US"),
            delegates:
            [
                DefaultWidgetsLocalizations.Delegate,
                DefaultMaterialLocalizations.Delegate,
                DefaultCupertinoLocalizations.Delegate,
            ],
            child: new DefaultTextEditingShortcuts(
                child: new Directionality(
                    TextDirection.Ltr,
                    new MediaQuery(
                        data: new MediaQueryData(Size: new Size(800.0, 600.0)),
                        child: new Plumix.Widgets.Overlay(initialEntries: [entry])))));
    }

    // `includesNodeWith(inputType:, currentValueLength:, flags:)`.
    private static bool IncludesTextFieldNode(SemanticsTester semantics, SemanticsFlags flags) =>
        semantics.NodesWith(currentValueLength: 0, flags: flags)
            .Any(node => node.GetSemanticsData().InputType == SemanticsInputType.Text);

    // Flutter: 'text_field_test.dart: TextField identifies as text field in semantics'
    [Fact]
    public void TextFieldIdentifiesAsTextFieldInSemantics()
    {
        FrameworkDartTester tester = _tester;
        var semantics = new SemanticsTester(tester);

        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new Center(child: new TextField(maxLength: 10)))));

        Assert.True(IncludesTextFieldNode(semantics, TextFieldFlags));

        semantics.Dispose();
    }

    // Flutter: 'text_field_test.dart: Disabled text field does not have tap action'
    [Fact]
    public void DisabledTextFieldDoesNotHaveTapAction()
    {
        FrameworkDartTester tester = _tester;
        var semantics = new SemanticsTester(tester);
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new Center(child: new TextField(maxLength: 10, enabled: false)))));

        Assert.False(semantics.IncludesNodeWith(actions: SemanticsActions.Tap | SemanticsActions.Focus));
        semantics.Dispose();
    }

    // Flutter: 'text_field_test.dart: Disabled text field semantics node still contains value'
    [Fact]
    public void DisabledTextFieldSemanticsNodeStillContainsValue()
    {
        FrameworkDartTester tester = _tester;
        var semantics = new SemanticsTester(tester);
        var controller = new TextEditingController("text");

        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new Center(
                    child: new TextField(controller: controller, maxLength: 10, enabled: false)))));

        Assert.True(semantics.IncludesNodeWith(actions: SemanticsActions.None, value: "text"));
        semantics.Dispose();
        controller.Dispose();
    }

    // Flutter: 'text_field_test.dart: Readonly text field does not have tap action'
    [Fact]
    public void ReadonlyTextFieldDoesNotHaveTapAction()
    {
        FrameworkDartTester tester = _tester;
        var semantics = new SemanticsTester(tester);

        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new Center(child: new TextField(maxLength: 10, readOnly: true)))));

        Assert.False(semantics.IncludesNodeWith(actions: SemanticsActions.Tap | SemanticsActions.Focus));

        semantics.Dispose();
    }

    // Flutter: 'text_field_test.dart: Read only TextField identifies as read only text field in semantics'
    [Fact]
    public void ReadOnlyTextFieldIdentifiesAsReadOnlyTextFieldInSemantics()
    {
        FrameworkDartTester tester = _tester;
        var semantics = new SemanticsTester(tester);

        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new Center(child: new TextField(maxLength: 10, readOnly: true)))));

        Assert.True(IncludesTextFieldNode(semantics, TextFieldFlags | SemanticsFlags.IsReadOnly));

        semantics.Dispose();
    }

    // Flutter: 'text_field_test.dart: TextField semantics always include label when no hint is given'
    [Fact]
    public void TextFieldSemanticsAlwaysIncludeLabelWhenNoHintIsGiven()
    {
        FrameworkDartTester tester = _tester;
        var semantics = new SemanticsTester(tester);
        var controller = new TextEditingController("value");
        Key key = new UniqueKey();

        tester.PumpWidget(Overlay(new TextField(
            key: key,
            controller: controller,
            decoration: new InputDecoration(labelText: "label"))));

        SemanticsNode node = tester.GetSemantics(Find.ByKey(key));

        Assert.Equal("label", node.Label);
        Assert.Equal("value", node.Value);

        // Focus text field.
        tester.Tap(Find.ByKey(key));
        tester.Pump();

        Assert.Equal("label", node.Label);
        Assert.Equal("value", node.Value);
        semantics.Dispose();
        controller.Dispose();
    }

    // Flutter: 'text_field_test.dart: TextField semantics only include hint when it is visible'
    [Fact]
    public void TextFieldSemanticsOnlyIncludeHintWhenItIsVisible()
    {
        FrameworkDartTester tester = _tester;
        var semantics = new SemanticsTester(tester);
        var controller = new TextEditingController("value");
        Key key = new UniqueKey();

        tester.PumpWidget(Overlay(new TextField(
            key: key,
            controller: controller,
            decoration: new InputDecoration(hintText: "hint"))));

        SemanticsNode node = tester.GetSemantics(Find.ByKey(key));

        Assert.Equal(string.Empty, node.Label);
        Assert.Equal("value", node.Value);

        // Focus text field.
        tester.Tap(Find.ByKey(key));
        tester.Pump();

        Assert.Equal(string.Empty, node.Label);
        Assert.Equal("value", node.Value);

        // Clear the Text.
        tester.EnterText(Find.ByType<TextField>(), string.Empty);
        tester.PumpAndSettle();

        Assert.Equal(string.Empty, node.Value);
        Assert.Equal("hint", node.Label);

        semantics.Dispose();
        controller.Dispose();
    }

    // Flutter: 'text_field_test.dart: TextField hintText is not duplicated in semantics'
    [Fact]
    public void TextFieldHintTextIsNotDuplicatedInSemantics()
    {
        FrameworkDartTester tester = _tester;
        var semantics = new SemanticsTester(tester);
        var controller = new TextEditingController();
        Key key = new UniqueKey();

        tester.PumpWidget(new MaterialApp(
            home: new Scaffold(
                body: new Form(
                    child: new TextField(
                        key: key,
                        controller: controller,
                        decoration: new InputDecoration(labelText: "Search", hintText: "Search Google Pay"))))));

        // Focus the text field so the hint Text widget becomes visible
        // and merges into the semantics tree.
        tester.Tap(Find.ByKey(key));
        tester.PumpAndSettle();

        SemanticsNode node = tester.GetSemantics(Find.ByType<EditableText>());
        Assert.Contains("Search Google Pay", node.Label);
        Assert.Empty(node.Hint);

        semantics.Dispose();
        controller.Dispose();
    }

    // Flutter: 'text_field_test.dart: TextField passes errorText to semantics hint'
    [Fact]
    public void TextFieldPassesErrorTextToSemanticsHint()
    {
        FrameworkDartTester tester = _tester;
        var semantics = new SemanticsTester(tester);
        var controller = new TextEditingController();
        Key key = new UniqueKey();

        tester.PumpWidget(Overlay(new TextField(
            key: key,
            controller: controller,
            decoration: new InputDecoration(labelText: "Email", errorText: "Email is required"))));

        SemanticsNode node = tester.GetSemantics(Find.ByKey(key));
        Assert.Equal("Email is required", node.Hint);

        semantics.Dispose();
        controller.Dispose();
    }

    // Flutter: 'text_field_test.dart: TextField errorText and hintText do not concatenate in semantics'
    // When both errorText and hintText are present, they should end up in
    // separate semantics properties: hintText in label (via Text widget merge),
    // errorText in hint (via explicit Semantics wrapper).
    [Fact]
    public void TextFieldErrorTextAndHintTextDoNotConcatenateInSemantics()
    {
        FrameworkDartTester tester = _tester;
        var semantics = new SemanticsTester(tester);
        var controller = new TextEditingController();
        Key key = new UniqueKey();

        tester.PumpWidget(new MaterialApp(
            home: new Scaffold(
                body: new Form(
                    child: new TextField(
                        key: key,
                        controller: controller,
                        decoration: new InputDecoration(
                            labelText: "Email",
                            hintText: "Enter your email",
                            errorText: "Email is required"))))));

        // Focus the text field so the hint Text widget becomes visible.
        tester.Tap(Find.ByKey(key));
        tester.PumpAndSettle();

        SemanticsNode node = tester.GetSemantics(Find.ByType<EditableText>());
        // hintText merges into label via the Text widget's markAsMergeUp.
        Assert.Contains("Enter your email", node.Label);
        // errorText is in the hint property via the explicit Semantics wrapper.
        Assert.Equal("Email is required", node.Hint);

        semantics.Dispose();
        controller.Dispose();
    }

    // Flutter: 'text_field_test.dart: when receives SemanticsAction.focus while already focused, shows keyboard'
    [Theory]
    [MemberData(nameof(TargetPlatformVariant.AllData), MemberType = typeof(TargetPlatformVariant))]
    public void WhenReceivesSemanticsActionFocusWhileAlreadyFocusedShowsKeyboard(TargetPlatform platform)
    {
        using IDisposable _ = TargetPlatformVariant.Override(platform);
        FrameworkDartTester tester = _tester;
        var semantics = new SemanticsTester(tester);
        SemanticsOwner semanticsOwner = tester.RenderView.Owner!.SemanticsOwner!;
        var focusNode = new FocusNode();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new Center(child: new TextField(focusNode: focusNode)))));
        focusNode.RequestFocus();
        tester.PumpAndSettle();

        tester.TestTextInput.Log.Clear();
        Assert.True(focusNode.HasFocus);
        // Dart performs the action on node id 4, the text field's node.
        semanticsOwner.PerformAction(tester.GetSemantics(Find.ByType<EditableText>()).Id, SemanticsActions.Focus);
        tester.PumpAndSettle();
        Assert.True(focusNode.HasFocus);
        Assert.Equal("TextInput.show", Assert.Single(tester.TestTextInput.Log).Method);

        semantics.Dispose();
        focusNode.Dispose();
    }

    // Flutter: 'text_field_test.dart: when receives SemanticsAction.focus while focused but read-only, does not
    // show keyboard'
    [Theory]
    [MemberData(nameof(TargetPlatformVariant.AllData), MemberType = typeof(TargetPlatformVariant))]
    public void WhenReceivesSemanticsActionFocusWhileFocusedButReadOnlyDoesNotShowKeyboard(TargetPlatform platform)
    {
        using IDisposable _ = TargetPlatformVariant.Override(platform);
        FrameworkDartTester tester = _tester;
        var semantics = new SemanticsTester(tester);
        SemanticsOwner semanticsOwner = tester.RenderView.Owner!.SemanticsOwner!;
        var focusNode = new FocusNode();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new Center(child: new TextField(focusNode: focusNode, readOnly: true)))));
        focusNode.RequestFocus();
        tester.PumpAndSettle();

        tester.TestTextInput.Log.Clear();
        Assert.True(focusNode.HasFocus);
        semanticsOwner.PerformAction(tester.GetSemantics(Find.ByType<EditableText>()).Id, SemanticsActions.Focus);
        tester.PumpAndSettle();
        Assert.True(focusNode.HasFocus);
        Assert.Empty(tester.TestTextInput.Log);

        semantics.Dispose();
        focusNode.Dispose();
    }

    // Flutter: 'text_field_test.dart: Can activate TextField with explicit controller via semantics '
    // Regression test for https://github.com/flutter/flutter/issues/17801
    [Fact(Skip = "Blocked on editable_text.dart's TextEditingController.text setter: Dart resets the selection to "
        + "TextSelection.collapsed(offset: -1) (invalid), so the unfocused field has no moveCursor actions; "
        + "Plumix's controller clamps every selection and the setter puts a caret at the end (DIVERGENCES.md, "
        + "TextEditingValue row), which adds moveCursorBackwardByCharacter/Word before the tap.")]
    public void CanActivateTextFieldWithExplicitControllerViaSemantics()
    {
        const string textInTextField = "Hello";
        FrameworkDartTester tester = _tester;
        // text_field_test.dart's setUp fills the mock clipboard.
        using var clipboard = new MockClipboardPlatform();
        Clipboard.SetData(new ClipboardData("Clipboard data"));

        var semantics = new SemanticsTester(tester);
        SemanticsOwner semanticsOwner = tester.RenderView.Owner!.SemanticsOwner!;
        var controller = new TextEditingController();
        controller.Text = textInTextField;
        Key key = new UniqueKey();

        tester.PumpWidget(Overlay(new TextField(key: key, controller: controller)));

        SemanticsNode rootChild = Assert.Single(semantics.RootNode.Children);
        SemanticsNode inputField = Assert.Single(rootChild.Children);
        ExpectSemantics(
            inputField,
            MatchesSemantics(
                isTextField: true,
                isFocusable: true,
                hasEnabledState: true,
                isEnabled: true,
                hasTapAction: true,
                hasFocusAction: true,
                value: textInTextField,
                inputType: SemanticsInputType.Text,
                currentValueLength: 5,
                textDirection: TextDirection.Ltr));

        semanticsOwner.PerformAction(inputField.Id, SemanticsActions.Tap);
        tester.Pump();

        Assert.Same(inputField, Assert.Single(Assert.Single(semantics.RootNode.Children).Children));
        ExpectSemantics(
            inputField,
            MatchesSemantics(
                isTextField: true,
                isFocusable: true,
                hasEnabledState: true,
                isEnabled: true,
                isFocused: true,
                hasTapAction: true,
                hasFocusAction: true,
                hasMoveCursorBackwardByCharacterAction: true,
                hasMoveCursorBackwardByWordAction: true,
                hasSetSelectionAction: true,
                hasSetTextAction: true,
                hasPasteAction: true,
                value: textInTextField,
                inputType: SemanticsInputType.Text,
                currentValueLength: 5,
                textDirection: TextDirection.Ltr));
        Assert.Equal(
            new TextSelection(textInTextField.Length, textInTextField.Length),
            inputField.GetSemanticsData().TextSelection);

        semantics.Dispose();
        controller.Dispose();
    }
}
