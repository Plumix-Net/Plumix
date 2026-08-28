using System.Collections;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/test/services/text_input_test.dart
// flutter/packages/flutter/test/services/text_editing_delta_test.dart
[Collection(SchedulerTestCollection.Name)]
public sealed class TextInputServiceTests : IDisposable
{
    private const string TestText = "From a false proposition, anything follows.";

    private readonly List<MethodCall> _log = [];
    private readonly List<(Exception Exception, string Context)> _errors = [];

    private readonly FlutterExceptionHandler? _previousOnError;

    public TextInputServiceTests()
    {
        UI.TextInput.DebugReset();
        UI.TextInput.EnsureInitialized();
        SystemChannels.TextInput.SetPlatformMethodCallHandler(call =>
        {
            _log.Add(call);
            return Task.FromResult<object?>(null);
        });
        _previousOnError = FlutterError.OnError;
        FlutterError.OnError = details => _errors.Add(
            ((Exception)details.Exception, details.Context!.ToString()));
    }

    public void Dispose()
    {
        FlutterError.OnError = _previousOnError;
        SystemChannels.TextInput.SetPlatformMethodCallHandler(null);
        UI.TextInput.DebugReset();
        Scheduler.FlushMicrotasks();
    }

    // -------------------------------------------------------------- TextSelection

    [Fact]
    public void TextSelection_TheInvalidSelectionIsASingleton()
    {
        var a = new TextSelection(-1, 0, TextAffinity.Downstream, IsDirectional: true);
        var b = new TextSelection(123, -1, TextAffinity.Upstream);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal("TextSelection.invalid", a.ToString());
    }

    [Fact]
    public void TextSelection_AffinityDoesNotAffectEquivalenceWhenNotCollapsed()
    {
        var a = new TextSelection(1, 2);
        var b = new TextSelection(1, 2, TextAffinity.Upstream);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(TextSelection.Collapsed(1), TextSelection.Collapsed(1, TextAffinity.Upstream));
    }

    [Fact]
    public void TextSelection_ToStringMatchesDart()
    {
        Assert.Equal(
            "TextSelection.collapsed(offset: 7, affinity: TextAffinity.downstream, isDirectional: false)",
            TextSelection.Collapsed(7).ToString());
        Assert.Equal(
            "TextSelection(baseOffset: 1, extentOffset: 5, isDirectional: false)",
            new TextSelection(1, 5).ToString());
        Assert.Equal("TextRange(start: 6, end: 7)", new TextRange(6, 7).ToString());
    }

    // ---------------------------------------------------- TextEditingValue.Replaced

    [Fact]
    public void TextEditingValue_ReplacedDeletesTheSelection()
    {
        var selection = new TextSelection(5, 13);
        var value = new TextEditingValue(TestText, selection);

        TextEditingValue replaced = value.Replaced(selection.AsTextRange(), string.Empty);

        Assert.Equal("From proposition, anything follows.", replaced.Text);
        Assert.Equal(TextSelection.Collapsed(5), replaced.Selection);
    }

    [Fact]
    public void TextEditingValue_ReplacedDeletesAReversedSelection()
    {
        var selection = new TextSelection(13, 5);
        var value = new TextEditingValue(TestText, selection);

        TextEditingValue replaced = value.Replaced(selection.AsTextRange(), string.Empty);

        Assert.Equal("From proposition, anything follows.", replaced.Text);
        Assert.Equal(TextSelection.Collapsed(5), replaced.Selection);
    }

    [Fact]
    public void TextEditingValue_ReplacedInsertsAtACollapsedRange()
    {
        var value = new TextEditingValue(TestText, TextSelection.Collapsed(5));

        TextEditingValue replaced = value.Replaced(TextRange.Collapsed(5), "AA");

        Assert.Equal("From AAa false proposition, anything follows.", replaced.Text);
        Assert.Equal(TextSelection.Collapsed(7), replaced.Selection);
    }

    [Fact]
    public void TextEditingValue_ReplacedBeforeTheSelectionShiftsIt()
    {
        var value = new TextEditingValue(TestText, new TextSelection(13, 5));

        TextEditingValue replaced = value.Replaced(new TextRange(4, 5), "AA");

        Assert.Equal("FromAAa false proposition, anything follows.", replaced.Text);
        Assert.Equal(new TextSelection(14, 6), replaced.Selection);
    }

    [Fact]
    public void TextEditingValue_ReplacedAfterTheSelectionLeavesIt()
    {
        var value = new TextEditingValue(TestText, new TextSelection(13, 5));

        TextEditingValue replaced = value.Replaced(new TextRange(13, 14), "AA");

        Assert.Equal("From a false AAroposition, anything follows.", replaced.Text);
        Assert.Equal(new TextSelection(13, 5), replaced.Selection);
    }

    [Fact]
    public void TextEditingValue_ReplacedInsideTheSelectionAtItsBoundaries()
    {
        var value = new TextEditingValue(TestText, new TextSelection(13, 5));

        TextEditingValue atStart = value.Replaced(new TextRange(5, 6), "AA");
        Assert.Equal("From AA false proposition, anything follows.", atStart.Text);
        Assert.Equal(new TextSelection(14, 5), atStart.Selection);

        TextEditingValue atEnd = value.Replaced(new TextRange(12, 13), "AA");
        Assert.Equal("From a falseAAproposition, anything follows.", atEnd.Text);
        Assert.Equal(new TextSelection(14, 5), atEnd.Selection);
    }

    [Fact]
    public void TextEditingValue_ReplacedDeletesAroundTheSelection()
    {
        var value = new TextEditingValue(TestText, new TextSelection(13, 5));

        TextEditingValue after = value.Replaced(new TextRange(13, 14), string.Empty);
        Assert.Equal("From a false roposition, anything follows.", after.Text);
        Assert.Equal(new TextSelection(13, 5), after.Selection);

        TextEditingValue atStart = value.Replaced(new TextRange(5, 6), string.Empty);
        Assert.Equal("From  false proposition, anything follows.", atStart.Text);
        Assert.Equal(new TextSelection(12, 5), atStart.Selection);

        TextEditingValue atEnd = value.Replaced(new TextRange(12, 13), string.Empty);
        Assert.Equal("From a falseproposition, anything follows.", atEnd.Text);
        Assert.Equal(new TextSelection(12, 5), atEnd.Selection);
    }

    [Fact]
    public void TextEditingValue_ReplacedIgnoresAnInvalidRange()
    {
        var value = new TextEditingValue(TestText, new TextSelection(13, 5));

        Assert.Equal(value, value.Replaced(TextRange.Empty, "AA"));
    }

    [Fact]
    public void TextEditingValue_IsComposingRangeValid()
    {
        Assert.False(new TextEditingValue().IsComposingRangeValid);
        Assert.False(new TextEditingValue("test", composing: new TextRange(1, 0)).IsComposingRangeValid);
        Assert.True(new TextEditingValue("test", composing: new TextRange(1, 4)).IsComposingRangeValid);

        // Plumix's constructor clamps the composing range into the text, where Dart asserts instead,
        // so an out-of-bounds range never survives to be reported invalid (see DIVERGENCES.md).
        Assert.Equal(
            new TextRange(0, 4),
            new TextEditingValue("test", composing: new TextRange(-1, 4)).Composing);
    }

    [Fact]
    public void TextEditingValue_JsonRoundTripsAffinityAndDirectionality()
    {
        var value = new TextEditingValue(
            "hi",
            new TextSelection(0, 2, TextAffinity.Upstream, IsDirectional: true));

        Dictionary<string, object?> json = value.ToJson();
        Assert.Equal("TextAffinity.upstream", json["selectionAffinity"]);
        Assert.Equal(true, json["selectionIsDirectional"]);

        TextEditingValue decoded = TextEditingValue.FromJson(json);
        Assert.Equal(TextAffinity.Upstream, decoded.Selection.Affinity);
        Assert.True(decoded.Selection.IsDirectional);
    }

    // ---------------------------------------------------------------- TextInputType

    [Fact]
    public void TextInputType_BasicStructure()
    {
        Assert.Equal(
            "TextInputType(name: TextInputType.text, signed: null, decimal: null)",
            TextInputType.Text.ToString());
        Assert.Equal(
            "TextInputType(name: TextInputType.address, signed: null, decimal: null)",
            TextInputType.StreetAddress.ToString());
        Assert.Equal(
            "TextInputType(name: TextInputType.number, signed: false, decimal: false)",
            TextInputType.Number.ToString());
        Assert.Equal(
            "TextInputType(name: TextInputType.twitter, signed: null, decimal: null)",
            TextInputType.Twitter.ToString());

        int[] expectedIndices = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
        Assert.Equal(expectedIndices, TextInputType.Values.Select(type => type.Index));
    }

    [Fact]
    public void TextInputType_Equality()
    {
        TextInputType signed = TextInputType.NumberWithOptions(signed: true);
        TextInputType signedAgain = TextInputType.NumberWithOptions(signed: true);
        TextInputType withDecimal = TextInputType.NumberWithOptions(isDecimal: true);
        TextInputType signedDecimal = TextInputType.NumberWithOptions(signed: true, isDecimal: true);

        Assert.NotEqual(TextInputType.Text, TextInputType.Number);
        Assert.Equal(TextInputType.Number, TextInputType.NumberWithOptions());
        Assert.NotEqual(TextInputType.Number, signed);
        Assert.Equal(signed, signedAgain);
        Assert.Equal(signed.GetHashCode(), signedAgain.GetHashCode());
        Assert.NotEqual(signed, withDecimal);
        Assert.NotEqual(signed, signedDecimal);
        Assert.NotEqual(withDecimal, signedDecimal);
    }

    [Fact]
    public void TextInputType_SerializesToJson()
    {
        Dictionary<string, object?> text = TextInputType.Text.ToJson();
        Assert.Equal(3, text.Count);
        Assert.Equal("TextInputType.text", text["name"]);
        Assert.Null(text["signed"]);
        Assert.Null(text["decimal"]);

        Dictionary<string, object?> number = TextInputType.NumberWithOptions(isDecimal: true).ToJson();
        Assert.Equal("TextInputType.number", number["name"]);
        Assert.Equal(false, number["signed"]);
        Assert.Equal(true, number["decimal"]);
    }

    [Fact]
    public void TextInputAction_RoundTripsThroughTheWireName()
    {
        Assert.Equal("TextInputAction.continueAction", TextInputActionType.ContinueAction.ToDartName());
        Assert.Equal("TextInputAction.emergencyCall", TextInputActionType.EmergencyCall.ToDartName());
        Assert.Equal("TextInputAction.newline", TextInputActionType.Newline.ToDartName());

        foreach (TextInputActionType action in Enum.GetValues<TextInputActionType>())
        {
            Assert.Equal(action, TextInputActions.Parse(action.ToDartName()));
        }

        Assert.Throws<ArgumentException>(() => TextInputActions.Parse("TextInputAction.nope"));
    }

    // ------------------------------------------------------- TextInputConfiguration

    [Fact]
    public void TextInputConfiguration_SetsExpectedDefaults()
    {
        var configuration = new TextInputConfiguration();

        Assert.Equal(TextInputType.Text, configuration.InputType);
        Assert.False(configuration.ReadOnly);
        Assert.False(configuration.ObscureText);
        Assert.False(configuration.EnableDeltaModel);
        Assert.True(configuration.Autocorrect);
        Assert.Null(configuration.ActionLabel);
        Assert.Equal(TextCapitalization.None, configuration.TextCapitalization);
        Assert.Equal(PlatformBrightness.Light, configuration.KeyboardAppearance);
        Assert.Null(configuration.EnableInlinePrediction);
        Assert.Null(configuration.ViewId);
        Assert.Empty(configuration.HintLocales);
    }

    [Fact]
    public void TextInputConfiguration_CopyWithAndEqualityAndHashCode()
    {
        var fixture = new TextInputConfiguration(
            viewId: 1,
            actionLabel: "label1",
            smartDashesType: SmartDashesType.Enabled,
            smartQuotesType: SmartQuotesType.Enabled,
            allowedMimeTypes: ["text/plain", "application/pdf"]);
        TextInputConfiguration copy = fixture.CopyWith();

        Assert.Equal(fixture, copy);
        Assert.Equal(fixture.GetHashCode(), copy.GetHashCode());
        Assert.Equal(fixture.ViewId, copy.ViewId);
        Assert.Equal(fixture.InputType, copy.InputType);
        Assert.Equal(fixture.ActionLabel, copy.ActionLabel);
        Assert.Equal(fixture.AllowedMimeTypes, copy.AllowedMimeTypes);

        TextInputConfiguration changed = fixture.CopyWith(inputType: TextInputType.EmailAddress);
        Assert.NotEqual(fixture, changed);
        Assert.Equal(TextInputType.EmailAddress, changed.InputType);
    }

    [Fact]
    public void TextInputConfiguration_ObscureTextDerivesSmartTypesOnlyOnConstruction()
    {
        var plain = new TextInputConfiguration();
        Assert.Equal(SmartDashesType.Enabled, plain.SmartDashesType);
        Assert.Equal(SmartQuotesType.Enabled, plain.SmartQuotesType);

        var obscured = new TextInputConfiguration(obscureText: true);
        Assert.Equal(SmartDashesType.Disabled, obscured.SmartDashesType);
        Assert.Equal(SmartQuotesType.Disabled, obscured.SmartQuotesType);

        // `copyWith` forwards the already-resolved values, so the derivation does not re-run.
        TextInputConfiguration copied = plain.CopyWith(obscureText: true);
        Assert.Equal(SmartDashesType.Enabled, copied.SmartDashesType);
    }

    [Fact]
    public void TextInputConfiguration_SerializesToJson()
    {
        Dictionary<string, object?> json = new TextInputConfiguration(
            readOnly: true,
            obscureText: true,
            autocorrect: false,
            actionLabel: "xyzzy").ToJson();

        var inputType = (Dictionary<string, object?>)json["inputType"]!;
        Assert.Equal(3, inputType.Count);
        Assert.Equal("TextInputType.text", inputType["name"]);
        Assert.Equal(true, json["readOnly"]);
        Assert.Equal(true, json["obscureText"]);
        Assert.Equal(false, json["autocorrect"]);
        Assert.Equal("xyzzy", json["actionLabel"]);
        Assert.Null(json["enableInlinePrediction"]);
        Assert.Equal(new List<string>(), json["hintLocales"]);

        Assert.Equal(
            true,
            new TextInputConfiguration(enableInlinePrediction: true).ToJson()["enableInlinePrediction"]);
        Assert.Equal(
            false,
            new TextInputConfiguration(enableInlinePrediction: false).ToJson()["enableInlinePrediction"]);
    }

    [Fact]
    public void TextInputConfiguration_SerializesHintLocalesAsLanguageTags()
    {
        Dictionary<string, object?> json = new TextInputConfiguration(
            hintLocales: [new Locale("en", "US"), new Locale("ru")]).ToJson();

        Assert.Equal(new List<string> { "en-US", "ru" }, json["hintLocales"]);
    }

    // ------------------------------------------------------------- TextInputStyle

    [Fact]
    public void TextInputStyle_EqualityAndHashCode()
    {
        var a = new TextInputStyle(
            TextDirection.Ltr,
            TextAlign.Center,
            fontFamily: "Roboto",
            fontSize: 16.0,
            fontWeight: FontWeight.Bold,
            letterSpacing: 1.2,
            wordSpacing: 2.0,
            lineHeight: 24.0);
        var b = new TextInputStyle(
            TextDirection.Ltr,
            TextAlign.Center,
            fontFamily: "Roboto",
            fontSize: 16.0,
            fontWeight: FontWeight.Bold,
            letterSpacing: 1.2,
            wordSpacing: 2.0,
            lineHeight: 24.0);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, new TextInputStyle(TextDirection.Ltr, TextAlign.Center, fontFamily: "Arial"));
    }

    [Fact]
    public void TextInputStyle_SerializesToJson()
    {
        Dictionary<string, object?> json = new TextInputStyle(
            TextDirection.Ltr,
            TextAlign.Center,
            fontFamily: "Roboto",
            fontSize: 16.0,
            fontWeight: FontWeight.Bold,
            letterSpacing: 1.2,
            wordSpacing: 2.0,
            lineHeight: 24.0).ToJson();

        Assert.Equal("Roboto", json["fontFamily"]);
        Assert.Equal(16.0, json["fontSize"]);
        Assert.Equal(6, json["fontWeightIndex"]);
        Assert.Equal(2, json["textAlignIndex"]);
        Assert.Equal(1, json["textDirectionIndex"]);
        Assert.Equal(1.2, json["letterSpacing"]);
        Assert.Equal(2.0, json["wordSpacing"]);
        Assert.Equal(24.0, json["lineHeight"]);
    }

    [Fact]
    public void TextInputStyle_SerializesNullValues()
    {
        Dictionary<string, object?> json = new TextInputStyle(TextDirection.Ltr, TextAlign.Left).ToJson();

        Assert.Null(json["fontFamily"]);
        Assert.Null(json["fontSize"]);
        Assert.Null(json["fontWeightIndex"]);
        Assert.Null(json["letterSpacing"]);
        Assert.Null(json["wordSpacing"]);
        Assert.Null(json["lineHeight"]);
        Assert.Equal(0, json["textAlignIndex"]);
        Assert.Equal(1, json["textDirectionIndex"]);
    }

    // ------------------------------------------- SelectionRect / inserted content

    [Fact]
    public void SelectionRect_EqualityLeavesDirectionOutOfTheHash()
    {
        var ltr = new SelectionRect(1, new Rect(2, 3, 4, 5));
        var rtl = new SelectionRect(1, new Rect(2, 3, 4, 5), TextDirection.Rtl);

        Assert.NotEqual(ltr, rtl);
        Assert.Equal(ltr.GetHashCode(), rtl.GetHashCode());
        Assert.Equal("SelectionRect(1, 2, 3, 4, 5)", ltr.ToString());
    }

    [Fact]
    public void KeyboardInsertedContent_FromJson()
    {
        var content = KeyboardInsertedContent.FromJson(
            new Dictionary<string, object?>
            {
                ["mimeType"] = "image/gif",
                ["uri"] = "content://test.gif",
                ["data"] = new List<object?> { 0, 1, 0, 1 },
            });

        Assert.Equal("image/gif", content.MimeType);
        Assert.Equal("content://test.gif", content.Uri);
        Assert.Equal(new byte[] { 0, 1, 0, 1 }, content.Data);
        Assert.True(content.HasData);

        var empty = new KeyboardInsertedContent("image/gif", "content://test.gif");
        Assert.False(empty.HasData);
        Assert.False(new KeyboardInsertedContent("a", "b", []).HasData);
    }

    // -------------------------------------------------------------- inbound dispatch

    [Fact]
    public void TextInput_RespondsToReattachWithSetClient()
    {
        var client = new FakeTextInputClient(new TextEditingValue("test1"));
        var configuration = new TextInputConfiguration(inputAction: TextInputActionType.Done);
        UI.TextInput.Attach(client, configuration);

        MethodCall attach = Assert.Single(_log);
        Assert.Equal("TextInput.setClient", attach.Method);

        Inbound(new MethodCall("TextInputClient.requestExistingInputState"));

        Assert.Equal(3, _log.Count);
        Assert.Equal("TextInput.setClient", _log[1].Method);
        Assert.Equal("TextInput.setEditingState", _log[2].Method);
        var state = (IDictionary)_log[2].Arguments!;
        Assert.Equal("test1", state["text"]);
    }

    [Fact]
    public void TextInput_RespondsToReattachWithAnEmptyEditingValue()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        UI.TextInput.Attach(client, new TextInputConfiguration());
        _log.Clear();

        Inbound(new MethodCall("TextInputClient.requestExistingInputState"));

        Assert.Equal(2, _log.Count);
        var state = (IDictionary)_log[1].Arguments!;
        Assert.Equal(string.Empty, state["text"]);
        Assert.Equal("TextAffinity.downstream", state["selectionAffinity"]);
        Assert.Equal(false, state["selectionIsDirectional"]);
        Assert.Equal(-1, state["composingBase"]);
        Assert.Equal(-1, state["composingExtent"]);
    }

    [Fact]
    public void TextInput_ConnectionClosedNotifiesTheClientWithoutDroppingTheConnection()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        TextInputConnection connection = UI.TextInput.Attach(client, new TextInputConfiguration());

        Inbound(new MethodCall("TextInputClient.onConnectionClosed", new List<object?> { 1 }));

        Assert.Equal("connectionClosed", client.LatestMethodCall);
        Assert.True(connection.Attached);
    }

    [Fact]
    public void TextInput_ConnectionClosedReceivedDetachesWithoutNotifyingTheClient()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        TextInputConnection connection = UI.TextInput.Attach(client, new TextInputConfiguration());
        Assert.True(connection.Attached);

        connection.ConnectionClosedReceived();

        Assert.False(connection.Attached);
        Assert.Null(client.LatestMethodCall);

        Inbound(new MethodCall("TextInputClient.onFocusReceived", new List<object?> { 1 }));
        Assert.Equal("onFocusReceived", client.LatestMethodCall);
    }

    [Fact]
    public void TextInput_OnFocusReceivedIsDeliveredWhileStillConnected()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        TextInputConnection connection = UI.TextInput.Attach(client, new TextInputConfiguration());

        Inbound(new MethodCall("TextInputClient.onFocusReceived", new List<object?> { 1 }));

        Assert.Equal("onFocusReceived", client.LatestMethodCall);
        Assert.True(connection.Attached);
    }

    [Fact]
    public void TextInput_CommitContentRoutesToInsertContent()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        UI.TextInput.Attach(client, new TextInputConfiguration());

        Inbound(new MethodCall(
            "TextInputClient.performAction",
            new List<object?>
            {
                1,
                "TextInputAction.commitContent",
                new Dictionary<string, object?>
                {
                    ["mimeType"] = "image/gif",
                    ["data"] = new List<object?> { 0, 1, 0, 1, 0, 1, 0, 0, 0 },
                    ["uri"] = "content://test.gif",
                },
            }));

        Assert.Equal("commitContent", client.LatestMethodCall);
        Assert.Equal("image/gif", client.LatestContent!.MimeType);
        Assert.True(client.LatestContent.HasData);
    }

    [Fact]
    public void TextInput_PerformActionParsesEveryAction()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        UI.TextInput.Attach(client, new TextInputConfiguration());

        Inbound(new MethodCall(
            "TextInputClient.performAction",
            new List<object?> { 1, "TextInputAction.emergencyCall" }));

        Assert.Equal("performAction", client.LatestMethodCall);
        Assert.Equal(TextInputActionType.EmergencyCall, client.LatestAction);
    }

    [Fact]
    public void TextInput_PerformSelectorsCallsTheClientOncePerSelector()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        UI.TextInput.Attach(client, new TextInputConfiguration());

        Inbound(new MethodCall(
            "TextInputClient.performSelectors",
            new List<object?> { 1, new List<object?> { "selector1", "selector2" } }));

        Assert.Equal("performSelector", client.LatestMethodCall);
        Assert.Equal(["selector1", "selector2"], client.PerformedSelectors);
    }

    [Fact]
    public void TextInput_PerformPrivateCommandPassesAnEmptyMapWhenDataIsMissing()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        UI.TextInput.Attach(client, new TextInputConfiguration());

        Inbound(new MethodCall(
            "TextInputClient.performPrivateCommand",
            new List<object?> { 1, new Dictionary<string, object?> { ["action"] = "actionCommand" } }));

        Assert.Equal("performPrivateCommand", client.LatestMethodCall);
        Assert.Equal("actionCommand", client.LatestPrivateCommandAction);
        Assert.Empty(client.LatestPrivateCommandData!);
    }

    [Fact]
    public void TextInput_PerformPrivateCommandForwardsData()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        UI.TextInput.Attach(client, new TextInputConfiguration());

        Inbound(new MethodCall(
            "TextInputClient.performPrivateCommand",
            new List<object?>
            {
                1,
                new Dictionary<string, object?>
                {
                    ["action"] = "actionCommand",
                    ["data"] = new Dictionary<string, object?> { ["input_context"] = "abcdefg" },
                },
            }));

        Assert.Equal("abcdefg", client.LatestPrivateCommandData!["input_context"]);
    }

    [Fact]
    public void TextInput_FloatingCursorCoordinatesAreTypeCast()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        UI.TextInput.Attach(client, new TextInputConfiguration());

        Inbound(new MethodCall(
            "TextInputClient.updateFloatingCursor",
            new List<object?>
            {
                -1,
                "FloatingCursorDragState.update",
                new Dictionary<string, object?> { ["X"] = 2, ["Y"] = 3 },
            }));

        Assert.Empty(_errors);
        Assert.Equal(FloatingCursorDragState.Update, client.LatestFloatingCursor!.State);
        Assert.Equal(new Point(2, 3), client.LatestFloatingCursor.Offset);
    }

    [Fact]
    public void TextInput_FloatingCursorStartAndEndUseTheZeroOffset()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        UI.TextInput.Attach(client, new TextInputConfiguration());

        Inbound(new MethodCall(
            "TextInputClient.updateFloatingCursor",
            new List<object?>
            {
                1,
                "FloatingCursorDragState.start",
                new Dictionary<string, object?> { ["X"] = 9, ["Y"] = 9 },
            }));

        Assert.Equal(new Point(0, 0), client.LatestFloatingCursor!.Offset);
    }

    [Fact]
    public void TextInput_ShowAutocorrectionPromptRectShowToolbarAndPlaceholders()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        UI.TextInput.Attach(client, new TextInputConfiguration());

        Inbound(new MethodCall(
            "TextInputClient.showAutocorrectionPromptRect",
            new List<object?> { 1, 0, 1 }));
        Assert.Equal("showAutocorrectionPromptRect", client.LatestMethodCall);

        Inbound(new MethodCall("TextInputClient.showToolbar", new List<object?> { 1 }));
        Assert.Equal("showToolbar", client.LatestMethodCall);

        Inbound(new MethodCall(
            "TextInputClient.insertTextPlaceholder",
            new List<object?> { 1, 100.0, 100.0 }));
        Assert.Equal("insertTextPlaceholder", client.LatestMethodCall);
        Assert.Equal(new Size(100, 100), client.LatestPlaceholderSize);

        Inbound(new MethodCall("TextInputClient.removeTextPlaceholder", new List<object?> { 1 }));
        Assert.Equal("removeTextPlaceholder", client.LatestMethodCall);
    }

    [Fact]
    public void TextInput_IgnoresMessagesForAnotherClientId()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        UI.TextInput.Attach(client, new TextInputConfiguration());

        Inbound(new MethodCall("TextInputClient.showToolbar", new List<object?> { 999 }));

        Assert.Null(client.LatestMethodCall);
    }

    [Fact]
    public void TextInput_ReportsAndRethrowsUnknownMethods()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        UI.TextInput.Attach(client, new TextInputConfiguration());

        Inbound(new MethodCall("TextInputClient.unknownMethod", new List<object?> { 1 }));

        (Exception exception, string context) = Assert.Single(_errors);
        Assert.IsType<MissingPluginException>(exception);
        Assert.Equal("during method call TextInputClient.unknownMethod", context);
    }

    // ---------------------------------------------------------------------- deltas

    [Fact]
    public void TextInput_UpdateEditingStateWithDeltasRequiresADeltaClient()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        UI.TextInput.Attach(client, new TextInputConfiguration(enableDeltaModel: true));

        Inbound(new MethodCall(
            "TextInputClient.updateEditingStateWithDeltas",
            new List<object?>
            {
                1,
                new Dictionary<string, object?> { ["deltas"] = new List<object?>() },
            }));

        (Exception exception, string _) = Assert.Single(_errors);
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void TextInput_UpdateEditingStateWithDeltasDeliversEveryDelta()
    {
        var client = new FakeDeltaTextInputClient();
        UI.TextInput.Attach(client, new TextInputConfiguration(enableDeltaModel: true));

        Inbound(new MethodCall(
            "TextInputClient.updateEditingStateWithDeltas",
            new List<object?>
            {
                1,
                new Dictionary<string, object?>
                {
                    ["deltas"] = new List<object?>
                    {
                        InsertionJson,
                        NonTextUpdateJson,
                    },
                },
            }));

        Assert.Equal(2, client.LatestDeltas!.Count);
        Assert.IsType<TextEditingDeltaInsertion>(client.LatestDeltas[0]);
        Assert.IsType<TextEditingDeltaNonTextUpdate>(client.LatestDeltas[1]);
    }

    [Fact]
    public void TextEditingDelta_InsertionAtACollapsedSelection()
    {
        var delta = (TextEditingDeltaInsertion)TextEditingDelta.FromJson(InsertionJson);

        Assert.Equal(string.Empty, delta.OldText);
        Assert.Equal("let there be text", delta.TextInserted);
        Assert.Equal(0, delta.InsertionOffset);
        Assert.Equal(TextSelection.Collapsed(17), delta.Selection);
        Assert.Equal(TextRange.Empty, delta.Composing);
        Assert.Equal("let there be text", delta.Apply(new TextEditingValue()).Text);
    }

    [Fact]
    public void TextEditingDelta_InsertionAtTheEndOfTheComposingRegion()
    {
        var delta = (TextEditingDeltaInsertion)TextEditingDelta.FromJson(
            DeltaJson("hello worl", "world", 6, 10, selection: 11, composingBase: 6, composingExtent: 11));

        Assert.Equal("hello worl", delta.OldText);
        Assert.Equal("d", delta.TextInserted);
        Assert.Equal(10, delta.InsertionOffset);
        Assert.Equal(TextSelection.Collapsed(11), delta.Selection);
        Assert.Equal(new TextRange(6, 11), delta.Composing);

        TextEditingValue applied = delta.Apply(new TextEditingValue());
        Assert.Equal("hello world", applied.Text);
        Assert.Equal(new TextRange(6, 11), applied.Composing);
    }

    [Fact]
    public void TextEditingDelta_DeletionOfASingleCharacter()
    {
        var delta = (TextEditingDeltaDeletion)TextEditingDelta.FromJson(
            DeltaJson("let there be text.", string.Empty, 1, 2, selection: 1));

        Assert.Equal("e", delta.TextDeleted);
        Assert.Equal(new TextRange(1, 2), delta.DeletedRange);
        Assert.Equal(TextSelection.Collapsed(1), delta.Selection);
        Assert.Equal(TextRange.Empty, delta.Composing);
        Assert.Equal("lt there be text.", delta.Apply(new TextEditingValue()).Text);
    }

    [Fact]
    public void TextEditingDelta_DeletionAtTheEndOfTheComposingRegion()
    {
        var delta = (TextEditingDeltaDeletion)TextEditingDelta.FromJson(
            DeltaJson("hello world", "worl", 6, 11, selection: 10, composingBase: 6, composingExtent: 10));

        Assert.Equal("d", delta.TextDeleted);
        Assert.Equal(new TextRange(10, 11), delta.DeletedRange);
        Assert.Equal(TextSelection.Collapsed(10), delta.Selection);
        Assert.Equal(new TextRange(6, 10), delta.Composing);
        Assert.Equal("hello worl", delta.Apply(new TextEditingValue()).Text);
    }

    [Fact]
    public void TextEditingDelta_ReplacementWithLongerShorterAndSameText()
    {
        var longer = (TextEditingDeltaReplacement)TextEditingDelta.FromJson(
            DeltaJson("hello worfi", "working", 6, 11, selection: 13, composingBase: 6, composingExtent: 13));
        Assert.Equal("worfi", longer.TextReplaced);
        Assert.Equal("working", longer.ReplacementText);
        Assert.Equal(new TextRange(6, 11), longer.ReplacedRange);
        Assert.Equal(TextSelection.Collapsed(13), longer.Selection);
        Assert.Equal("hello working", longer.Apply(new TextEditingValue()).Text);

        var shorter = (TextEditingDeltaReplacement)TextEditingDelta.FromJson(
            DeltaJson("hello world", "h", 6, 11, selection: 7, composingBase: 6, composingExtent: 7));
        Assert.Equal("world", shorter.TextReplaced);
        Assert.Equal("h", shorter.ReplacementText);
        Assert.Equal("hello h", shorter.Apply(new TextEditingValue()).Text);

        var same = (TextEditingDeltaReplacement)TextEditingDelta.FromJson(
            DeltaJson("hello world", "words", 6, 11, selection: 11, composingBase: 6, composingExtent: 11));
        Assert.Equal("world", same.TextReplaced);
        Assert.Equal("words", same.ReplacementText);
        Assert.Equal("hello words", same.Apply(new TextEditingValue()).Text);
    }

    [Fact]
    public void TextEditingDelta_NonTextUpdate()
    {
        var delta = (TextEditingDeltaNonTextUpdate)TextEditingDelta.FromJson(
            DeltaJson("hello world", string.Empty, -1, -1, selection: 10, composingBase: 6, composingExtent: 11));

        Assert.Equal("hello world", delta.OldText);
        Assert.Equal(TextSelection.Collapsed(10), delta.Selection);
        Assert.Equal(new TextRange(6, 11), delta.Composing);

        // The incoming value is discarded entirely.
        TextEditingValue applied = delta.Apply(new TextEditingValue("ignored"));
        Assert.Equal("hello world", applied.Text);
        Assert.Equal(new TextRange(6, 11), applied.Composing);
    }

    [Fact]
    public void TextEditingDelta_OutOfBoundsDeltasFailToApply()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TextEditingDeltaInsertion(
                "hello worl",
                "d",
                11,
                TextSelection.Collapsed(11),
                TextRange.Empty).Apply(new TextEditingValue()));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TextEditingDeltaDeletion(
                "hello world",
                new TextRange(5, 12),
                TextSelection.Collapsed(5),
                TextRange.Empty).Apply(new TextEditingValue()));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TextEditingDeltaReplacement(
                "hello worl",
                "d",
                new TextRange(5, 11),
                TextSelection.Collapsed(5),
                TextRange.Empty).Apply(new TextEditingValue()));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TextEditingDeltaNonTextUpdate(
                "hello world",
                TextSelection.Collapsed(12),
                TextRange.Empty).Apply(new TextEditingValue()));
    }

    [Fact]
    public void TextEditingDelta_DebugFillPropertiesMatchDart()
    {
        var insertion = new TextEditingDeltaInsertion(
            "hello worl",
            "d",
            10,
            TextSelection.Collapsed(11),
            TextRange.Empty);

        Assert.Equal(
            [
                "oldText: hello worl",
                "textInserted: d",
                "insertionOffset: 10",
                "selection: TextSelection.collapsed(offset: 11, affinity: TextAffinity.downstream, "
                + "isDirectional: false)",
                "composing: TextRange(start: -1, end: -1)",
            ],
            Describe(insertion));

        var deletion = new TextEditingDeltaDeletion(
            "hello world",
            new TextRange(6, 10),
            TextSelection.Collapsed(6),
            TextRange.Empty);

        Assert.Equal(
            [
                "oldText: hello world",
                "textDeleted: worl",
                "deletedRange: TextRange(start: 6, end: 10)",
                "selection: TextSelection.collapsed(offset: 6, affinity: TextAffinity.downstream, "
                + "isDirectional: false)",
                "composing: TextRange(start: -1, end: -1)",
            ],
            Describe(deletion));

        var nonTextUpdate = new TextEditingDeltaNonTextUpdate(
            "hello world",
            TextSelection.Collapsed(7),
            new TextRange(6, 7));

        Assert.Equal(
            [
                "oldText: hello world",
                "selection: TextSelection.collapsed(offset: 7, affinity: TextAffinity.downstream, "
                + "isDirectional: false)",
                "composing: TextRange(start: 6, end: 7)",
            ],
            Describe(nonTextUpdate));
    }

    // ------------------------------------------------------------- TextInputControl

    [Fact]
    public void TextInputControl_GetsAttachedAndDetached()
    {
        var control = new RecordingTextInputControl();
        UI.TextInput.SetInputControl(control);
        var client = new FakeTextInputClient(new TextEditingValue());

        TextInputConnection connection = UI.TextInput.Attach(client, new TextInputConfiguration());
        Assert.Equal(["attach"], control.MethodCalls);

        connection.Close();
        Assert.Equal(["attach", "detach"], control.MethodCalls);
    }

    [Fact]
    public void TextInputControl_ReceivesTextInputStateChanges()
    {
        var control = new RecordingTextInputControl();
        UI.TextInput.SetInputControl(control);
        var client = new FakeTextInputClient(new TextEditingValue());
        TextInputConnection connection = UI.TextInput.Attach(client, new TextInputConfiguration());
        control.MethodCalls.Clear();

        connection.UpdateConfig(new TextInputConfiguration());
        Assert.Equal(["updateConfig"], control.MethodCalls);

        connection.SetEditingState(new TextEditingValue("hi"));
        Assert.Equal(["updateConfig", "setEditingState"], control.MethodCalls);

        connection.Close();
        Assert.Equal(["updateConfig", "setEditingState", "detach"], control.MethodCalls);
    }

    [Fact]
    public void TextInputControl_DoesNotInterfereWithPlatformTextInput()
    {
        var control = new RecordingTextInputControl();
        UI.TextInput.SetInputControl(control);
        var client = new FakeTextInputClient(new TextEditingValue());
        UI.TextInput.Attach(client, new TextInputConfiguration());
        _log.Clear();

        Inbound(new MethodCall(
            "TextInputClient.updateEditingState",
            new List<object?> { 1, new TextEditingValue().ToJson() }));

        Assert.Equal("updateEditingValue", client.LatestMethodCall);
        Assert.Equal(["attach", "setEditingState"], control.MethodCalls);
        Assert.Empty(_log);
    }

    [Fact]
    public void TextInputControl_BothInputControlsReceiveRequests()
    {
        var control = new RecordingTextInputControl();
        UI.TextInput.SetInputControl(control);
        var client = new FakeTextInputClient(new TextEditingValue());

        TextInputConnection connection = UI.TextInput.Attach(client, new TextInputConfiguration());
        Assert.Equal(["attach"], control.MethodCalls);
        Assert.Equal(TextInputType.Text, control.InputType);
        Assert.Single(_log);
        Assert.Equal("TextInput.setClient", _log[0].Method);
        var setClientArguments = (IList)_log[0].Arguments!;
        var attachedConfig = (IDictionary)setClientArguments[1]!;
        var noneType = (IDictionary)attachedConfig["inputType"]!;
        Assert.Equal("TextInputType.none", noneType["name"]);

        connection.Show();
        Assert.Equal(["attach", "show"], control.MethodCalls);
        Assert.Equal("TextInput.show", _log[^1].Method);

        connection.UpdateConfig(new TextInputConfiguration(inputType: TextInputType.Multiline));
        Assert.Equal(TextInputType.Multiline, control.InputType);
        Assert.Equal("TextInput.updateConfig", _log[^1].Method);

        connection.SetComposingRect(new Rect(0, 0, 0, 0));
        Assert.Equal("TextInput.setMarkedTextRect", _log[^1].Method);

        connection.SetCaretRect(new Rect(0, 0, 0, 0));
        Assert.Equal("TextInput.setCaretRect", _log[^1].Method);

        connection.SetEditableSizeAndTransform(default, Matrix4.Identity());
        Assert.Equal("TextInput.setEditableSizeAndTransform", _log[^1].Method);

        connection.SetSelectionRects(
            [new SelectionRect(1, new Rect(2, 3, 4, 5), TextDirection.Rtl)]);
        Assert.Equal("TextInput.setSelectionRects", _log[^1].Method);
        var rects = (IList)_log[^1].Arguments!;
        var first = (IList)rects[0]!;
        Assert.Equal([2.0, 3.0, 4.0, 5.0, 1, 0], first.Cast<object?>());

        connection.UpdateStyle(new TextInputStyle(TextDirection.Ltr, TextAlign.Left));
        Assert.Equal("TextInput.setStyle", _log[^1].Method);

        int beforeClose = _log.Count;
        connection.Close();
        Assert.Equal("TextInput.clearClient", _log[^1].Method);
        Assert.Equal(beforeClose + 1, _log.Count);

        // The hide is deferred to a microtask, exactly like Dart's `_scheduleHide`.
        Assert.DoesNotContain("hide", control.MethodCalls);
        Scheduler.FlushMicrotasks();
        Assert.Contains("hide", control.MethodCalls);
        Assert.Equal("TextInput.hide", _log[^1].Method);
    }

    [Fact]
    public void TextInputControl_ScheduledHideIsCancelledByANewConnection()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        TextInputConnection connection = UI.TextInput.Attach(client, new TextInputConfiguration());
        connection.Close();
        UI.TextInput.Attach(client, new TextInputConfiguration());
        _log.Clear();

        Scheduler.FlushMicrotasks();

        Assert.DoesNotContain(_log, call => call.Method == "TextInput.hide");
    }

    [Fact]
    public void TextInputControl_NotifiesChangesToTheAttachedClient()
    {
        var control = new RecordingTextInputControl();
        UI.TextInput.SetInputControl(control);
        var client = new FakeTextInputClient(new TextEditingValue());
        TextInputConnection connection = UI.TextInput.Attach(client, new TextInputConfiguration());

        UI.TextInput.SetInputControl(null);

        Assert.Equal("didChangeInputControl", client.LatestMethodCall);
        Assert.Same(control, client.OldControl);
        Assert.Null(client.NewControl);

        connection.Show();
        Assert.Equal("didChangeInputControl", client.LatestMethodCall);
    }

    [Fact]
    public void TextInput_UpdateEditingValueFromACustomControlExcludesThatControl()
    {
        var control = new RecordingTextInputControl();
        UI.TextInput.SetInputControl(control);
        var client = new FakeTextInputClient(new TextEditingValue());
        UI.TextInput.Attach(client, new TextInputConfiguration());
        control.MethodCalls.Clear();

        UI.TextInput.UpdateEditingValue(new TextEditingValue("typed"));

        Assert.Empty(control.MethodCalls);
        Assert.Equal("updateEditingValue", client.LatestMethodCall);
        Assert.Equal("typed", client.LatestValue!.Value.Text);
    }

    // ----------------------------------------------------------------- connection

    [Fact]
    public void TextInputConnection_DeduplicatesGeometryUpdates()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        TextInputConnection connection = UI.TextInput.Attach(client, new TextInputConfiguration());
        _log.Clear();

        connection.SetCaretRect(new Rect(1, 2, 3, 4));
        connection.SetCaretRect(new Rect(1, 2, 3, 4));
        Assert.Single(_log);

        connection.SetEditableSizeAndTransform(new Size(2, 3), Matrix4.Identity());
        connection.SetEditableSizeAndTransform(new Size(2, 3), Matrix4.Identity());
        Assert.Equal(2, _log.Count);

        connection.SetSelectionRects([new SelectionRect(0, new Rect(0, 0, 1, 1))]);
        connection.SetSelectionRects([new SelectionRect(0, new Rect(0, 0, 1, 1))]);
        Assert.Equal(3, _log.Count);
    }

    [Fact]
    public void TextInputConnection_SanitizesNonFiniteRects()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        TextInputConnection connection = UI.TextInput.Attach(client, new TextInputConfiguration());
        _log.Clear();

        connection.SetComposingRect(new Rect(0, 0, double.PositiveInfinity, 2));

        var arguments = (IDictionary)Assert.Single(_log).Arguments!;
        Assert.Equal(-1.0, arguments["width"]);
        Assert.Equal(-1.0, arguments["height"]);
        Assert.Equal(0.0, arguments["x"]);
        Assert.Equal(0.0, arguments["y"]);
    }

    [Fact]
    public void TextInputConnection_SendsTheEditableSizeAndTransformStorage()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        TextInputConnection connection = UI.TextInput.Attach(client, new TextInputConfiguration());
        _log.Clear();

        connection.SetEditableSizeAndTransform(new Size(10, 20), Matrix4.Identity());

        var arguments = (IDictionary)Assert.Single(_log).Arguments!;
        Assert.Equal(10.0, arguments["width"]);
        Assert.Equal(20.0, arguments["height"]);
        Assert.Equal(16, ((IList)arguments["transform"]!).Count);
    }

    // -------------------------------------------------------------------- scribble

    [Fact]
    public void TextInput_ScribbleInteractionBeganAndFinished()
    {
        var client = new FakeTextInputClient(new TextEditingValue());
        TextInputConnection connection = UI.TextInput.Attach(client, new TextInputConfiguration());

        Assert.False(connection.ScribbleInProgress);
        Inbound(new MethodCall("TextInputClient.scribbleInteractionBegan"));
        Assert.True(connection.ScribbleInProgress);
        Inbound(new MethodCall("TextInputClient.scribbleInteractionFinished"));
        Assert.False(connection.ScribbleInProgress);
    }

    [Fact]
    public void TextInput_FocusElementOnlyReachesTheNamedClient()
    {
        var target = new FakeScribbleClient("target", new Rect(0, 0, 100, 100));
        var other = new FakeScribbleClient("other", new Rect(0, 100, 100, 100));
        UI.TextInput.RegisterScribbleElement(target.ElementIdentifier, target);
        UI.TextInput.RegisterScribbleElement(other.ElementIdentifier, other);

        Inbound(new MethodCall(
            "TextInputClient.focusElement",
            new List<object?> { "target", 0.0, 0.0 }));

        Assert.Equal("onScribbleFocus", target.LatestMethodCall);
        Assert.Null(other.LatestMethodCall);
    }

    [Fact]
    public void TextInput_RequestElementsInRectReturnsOverlappingElements()
    {
        var first = new FakeScribbleClient("target1", new Rect(0, 0, 100, 100));
        var second = new FakeScribbleClient("target2", new Rect(0, 100, 100, 100));
        var away = new FakeScribbleClient("target3", new Rect(0, 500, 100, 100));
        UI.TextInput.RegisterScribbleElement(first.ElementIdentifier, first);
        UI.TextInput.RegisterScribbleElement(second.ElementIdentifier, second);
        UI.TextInput.RegisterScribbleElement(away.ElementIdentifier, away);

        object? reply = InboundResult(new MethodCall(
            "TextInputClient.requestElementsInRect",
            new List<object?> { 0.0, 50.0, 50.0, 100.0 }));

        var elements = (IList)reply!;
        Assert.Equal(2, elements.Count);
        Assert.Equal(["target1", 0.0, 0.0, 100.0, 100.0], ((IList)elements[0]!).Cast<object?>());
        Assert.Equal(["target2", 0.0, 100.0, 100.0, 100.0], ((IList)elements[1]!).Cast<object?>());
    }

    // --------------------------------------------------------------- error contexts

    [Theory]
    [InlineData("TextInput.setClient", "while attaching the text input client")]
    [InlineData("TextInput.clearClient", "while detaching the text input client")]
    [InlineData("TextInput.updateConfig", "while updating text input configuration")]
    [InlineData("TextInput.setEditingState", "while setting text input editing state")]
    [InlineData("TextInput.show", "while showing the text input client")]
    [InlineData("TextInput.hide", "while hiding the text input client")]
    [InlineData("TextInput.setEditableSizeAndTransform", "while setting text input size and transform")]
    [InlineData("TextInput.setMarkedTextRect", "while setting text input composing rect")]
    [InlineData("TextInput.setCaretRect", "while setting text input caret rect")]
    [InlineData("TextInput.setSelectionRects", "while setting text input selection rects")]
    [InlineData("TextInput.setStyle", "while updating text input style")]
    [InlineData("TextInput.requestAutofill", "while requesting autofill")]
    [InlineData("TextInput.finishAutofillContext", "while finishing autofill context")]
    public void TextInput_ReportsChannelFailuresWithTheDartContext(string failingMethod, string context)
    {
        SystemChannels.TextInput.SetPlatformMethodCallHandler(call =>
            call.Method == failingMethod
                ? Task.FromException<object?>(new PlatformException("error", $"Failed: {failingMethod}"))
                : Task.FromResult<object?>(null));

        var client = new FakeTextInputClient(new TextEditingValue());
        TextInputConnection connection = UI.TextInput.Attach(client, new TextInputConfiguration());
        connection.Show();
        connection.UpdateConfig(new TextInputConfiguration());
        connection.SetEditingState(new TextEditingValue("x"));
        connection.SetEditableSizeAndTransform(new Size(1, 1), Matrix4.Identity());
        connection.SetComposingRect(new Rect(0, 0, 1, 1));
        connection.SetCaretRect(new Rect(0, 0, 1, 1));
        connection.SetSelectionRects([new SelectionRect(0, new Rect(0, 0, 1, 1))]);
        connection.UpdateStyle(new TextInputStyle(TextDirection.Ltr, TextAlign.Left));
        connection.RequestAutofill();
        UI.TextInput.FinishAutofillContext();
        connection.Close();
        Scheduler.FlushMicrotasks();

        Assert.Contains(_errors, error => error.Context == context);
    }

    // ------------------------------------------------------------------- helpers

    private static Dictionary<string, object?> InsertionJson => DeltaJson(
        string.Empty,
        "let there be text",
        0,
        0,
        selection: 17);

    private static Dictionary<string, object?> NonTextUpdateJson => DeltaJson(
        "let there be text",
        string.Empty,
        -1,
        -1,
        selection: 10);

    private static Dictionary<string, object?> DeltaJson(
        string oldText,
        string deltaText,
        int deltaStart,
        int deltaEnd,
        int selection,
        int composingBase = -1,
        int composingExtent = -1)
    {
        return new Dictionary<string, object?>
        {
            ["oldText"] = oldText,
            ["deltaText"] = deltaText,
            ["deltaStart"] = deltaStart,
            ["deltaEnd"] = deltaEnd,
            ["selectionBase"] = selection,
            ["selectionExtent"] = selection,
            ["selectionAffinity"] = "TextAffinity.downstream",
            ["selectionIsDirectional"] = false,
            ["composingBase"] = composingBase,
            ["composingExtent"] = composingExtent,
        };
    }

    private static IReadOnlyList<string> Describe(TextEditingDelta delta)
    {
        var builder = new DiagnosticPropertiesBuilder();
        delta.DebugFillProperties(builder);
        return builder.Properties.Select(property => property.ToString()).ToList();
    }

    private void Inbound(MethodCall call)
    {
        SystemChannels.TextInput.BinaryMessenger.HandlePlatformMessage(
            SystemChannels.TextInput.Name,
            SystemChannels.TextInput.Codec.EncodeMethodCall(call),
            null);
    }

    private object? InboundResult(MethodCall call)
    {
        object? result = null;
        SystemChannels.TextInput.BinaryMessenger.HandlePlatformMessage(
            SystemChannels.TextInput.Name,
            SystemChannels.TextInput.Codec.EncodeMethodCall(call),
            reply => result = reply is null ? null : SystemChannels.TextInput.Codec.DecodeEnvelope(reply));
        return result;
    }

    private sealed class RecordingTextInputControl : TextInputControl
    {
        public List<string> MethodCalls { get; } = [];

        public TextInputType? InputType { get; private set; }

        public override void Attach(ITextInputClient client, TextInputConfiguration configuration)
        {
            MethodCalls.Add("attach");
            InputType = configuration.InputType;
        }

        public override void Detach(ITextInputClient client) => MethodCalls.Add("detach");

        public override void Show() => MethodCalls.Add("show");

        public override void Hide() => MethodCalls.Add("hide");

        public override void UpdateConfig(TextInputConfiguration configuration)
        {
            MethodCalls.Add("updateConfig");
            InputType = configuration.InputType;
        }

        public override void SetEditingState(TextEditingValue value) => MethodCalls.Add("setEditingState");

        public override void SetEditableSizeAndTransform(Size editableBoxSize, Matrix4 transform) =>
            MethodCalls.Add("setEditableSizeAndTransform");

        public override void SetComposingRect(Rect rect) => MethodCalls.Add("setComposingRect");

        public override void SetCaretRect(Rect rect) => MethodCalls.Add("setCaretRect");

        public override void SetSelectionRects(IReadOnlyList<SelectionRect> selectionRects) =>
            MethodCalls.Add("setSelectionRects");

        public override void UpdateStyle(TextInputStyle style) => MethodCalls.Add("updateStyle");

        public override void RequestAutofill() => MethodCalls.Add("requestAutofill");

        public override void FinishAutofillContext(bool shouldSave = true) =>
            MethodCalls.Add("finishAutofillContext");
    }

    private class FakeTextInputClient : ITextInputClient
    {
        public FakeTextInputClient(TextEditingValue value)
        {
            CurrentTextEditingValue = value;
        }

        public TextEditingValue? CurrentTextEditingValue { get; }

        public IAutofillScope? CurrentAutofillScope => null;

        public string? LatestMethodCall { get; protected set; }

        public TextEditingValue? LatestValue { get; private set; }

        public TextInputActionType? LatestAction { get; private set; }

        public KeyboardInsertedContent? LatestContent { get; private set; }

        public RawFloatingCursorPoint? LatestFloatingCursor { get; private set; }

        public Size? LatestPlaceholderSize { get; private set; }

        public string? LatestPrivateCommandAction { get; private set; }

        public IDictionary? LatestPrivateCommandData { get; private set; }

        public List<string> PerformedSelectors { get; } = [];

        public TextInputControl? OldControl { get; private set; }

        public TextInputControl? NewControl { get; private set; }

        public void UpdateEditingValue(TextEditingValue value)
        {
            LatestMethodCall = "updateEditingValue";
            LatestValue = value;
        }

        public void PerformAction(TextInputActionType action)
        {
            LatestMethodCall = "performAction";
            LatestAction = action;
        }

        public void PerformPrivateCommand(string action, IDictionary data)
        {
            LatestMethodCall = "performPrivateCommand";
            LatestPrivateCommandAction = action;
            LatestPrivateCommandData = data;
        }

        public void UpdateFloatingCursor(RawFloatingCursorPoint point)
        {
            LatestMethodCall = "updateFloatingCursor";
            LatestFloatingCursor = point;
        }

        public void ShowAutocorrectionPromptRect(int start, int end) =>
            LatestMethodCall = "showAutocorrectionPromptRect";

        public void ConnectionClosed() => LatestMethodCall = "connectionClosed";

        public void InsertContent(KeyboardInsertedContent content)
        {
            LatestMethodCall = "commitContent";
            LatestContent = content;
        }

        public bool OnFocusReceived()
        {
            LatestMethodCall = "onFocusReceived";
            return true;
        }

        public void DidChangeInputControl(TextInputControl? oldControl, TextInputControl? newControl)
        {
            LatestMethodCall = "didChangeInputControl";
            OldControl = oldControl;
            NewControl = newControl;
        }

        public void ShowToolbar() => LatestMethodCall = "showToolbar";

        public void InsertTextPlaceholder(Size size)
        {
            LatestMethodCall = "insertTextPlaceholder";
            LatestPlaceholderSize = size;
        }

        public void RemoveTextPlaceholder() => LatestMethodCall = "removeTextPlaceholder";

        public void PerformSelector(string selectorName)
        {
            LatestMethodCall = "performSelector";
            PerformedSelectors.Add(selectorName);
        }
    }

    private sealed class FakeDeltaTextInputClient : FakeTextInputClient, IDeltaTextInputClient
    {
        public FakeDeltaTextInputClient()
            : base(new TextEditingValue())
        {
        }

        public IReadOnlyList<TextEditingDelta>? LatestDeltas { get; private set; }

        public void UpdateEditingValueWithDeltas(IReadOnlyList<TextEditingDelta> textEditingDeltas)
        {
            LatestMethodCall = "updateEditingValueWithDeltas";
            LatestDeltas = textEditingDeltas;
        }
    }

    private sealed class FakeScribbleClient : IScribbleClient
    {
        public FakeScribbleClient(string elementIdentifier, Rect bounds)
        {
            ElementIdentifier = elementIdentifier;
            Bounds = bounds;
        }

        public string ElementIdentifier { get; }

        public Rect Bounds { get; }

        public string? LatestMethodCall { get; private set; }

        public void OnScribbleFocus(Point offset) => LatestMethodCall = "onScribbleFocus";

        public bool IsInScribbleRect(Rect rect) => Bounds.Intersects(rect);
    }
}
