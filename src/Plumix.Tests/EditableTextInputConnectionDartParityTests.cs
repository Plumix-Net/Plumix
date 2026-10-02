using System.Collections;
using Avalonia;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Material;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using AvaloniaInputElement = Avalonia.Input.InputElement;
using AvaloniaTextInputEventArgs = Avalonia.Input.TextInputEventArgs;
using AvaloniaTextSelection = Avalonia.Input.TextInput.TextSelection;
using TextSelection = Plumix.Widgets.TextSelection;

namespace Plumix.Tests;

/// <summary>
/// EditableText's text input connection against a mocked and against the real host platform side:
/// the <c>editable_text_test.dart</c> cases about the configuration's view id, private commands and
/// the style sent to the platform, <c>text_field_test.dart</c>'s <c>maxLength</c> cases (Dart's
/// <c>TextField</c> enforces it with a <c>LengthLimitingTextInputFormatter</c>), and the host's IME
/// round trip through <see cref="HostTextInputPlugin"/>.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class EditableTextInputConnectionDartParityTests : IDisposable
{
    private readonly TextEditingController _controller = new();
    private readonly FocusNode _focusNode = new();

    public EditableTextInputConnectionDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        Plumix.UI.TextInput.DebugReset();
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        Plumix.UI.TextInput.DebugReset();
        FocusManager.Instance.ResetForTests();
        _focusNode.Dispose();
        _controller.Dispose();
    }

    private EditableText Field(
        TextStyle? style = null,
        AppPrivateCommandCallback? onAppPrivateCommand = null,
        bool multiline = false) =>
        new(
            controller: _controller,
            focusNode: _focusNode,
            style: style ?? new TextStyle(FontSize: 14),
            multiline: multiline,
            onAppPrivateCommand: onAppPrivateCommand);

    private FrameworkDartTester PumpFocused(Widget field)
    {
        var tester = new FrameworkDartTester();
        tester.PumpWidget(new TestWidgetsApp(
            home: new Align(
                alignment: Alignment.TopLeft,
                child: new SizedBox(width: 400, height: 300, child: field))));
        _focusNode.RequestFocus();
        tester.Pump();
        return tester;
    }

    // Flutter: 'editable_text_test.dart: EditableText sends viewId to config'
    [Fact]
    public void EditableTextSendsViewIdToConfig()
    {
        using var textInput = new TestTextInput();
        using FrameworkDartTester tester = PumpFocused(Field());

        EditableText.EditableTextState state = tester.State<EditableText.EditableTextState>();
        Assert.Equal(tester.View.ViewId, state.TextInputConfiguration.ViewId);
        Assert.Equal(tester.View.ViewId, Convert.ToInt32(textInput.SetClientArgs!["viewId"]));
    }

    // Flutter: 'editable_text_test.dart: onAppPrivateCommand does not throw'
    [Fact]
    public void OnAppPrivateCommandDoesNotThrowAndReachesTheCallback()
    {
        string? receivedAction = null;
        IDictionary? receivedData = null;
        using var textInput = new TestTextInput();
        using FrameworkDartTester tester = PumpFocused(Field(onAppPrivateCommand: (action, data) =>
        {
            receivedAction = action;
            receivedData = data;
        }));
        _controller.Text = "test";

        TestTextInput.Send(
            "TextInputClient.performPrivateCommand",
            new List<object?>
            {
                -1, // The magic client id that points to the current client.
                new Dictionary<string, object?>
                {
                    ["action"] = "actionCommand",
                    ["data"] = new Dictionary<string, object?> { ["input_context"] = "abcdefg" },
                },
            });

        Assert.Equal("actionCommand", receivedAction);
        Assert.Equal("abcdefg", receivedData!["input_context"]);
    }

    [Fact]
    public void OpeningTheConnectionSendsTheStyleBeforeTheEditingStateAndShow()
    {
        using var textInput = new TestTextInput();
        using FrameworkDartTester tester = PumpFocused(Field());

        // Dart's `_openInputConnection`: setClient, then (after the size and transform) updateStyle,
        // setEditingState and show.
        List<string> methods = textInput.Log.Select(call => call.Method).ToList();
        int setClient = methods.IndexOf("TextInput.setClient");
        int setStyle = methods.IndexOf("TextInput.setStyle");
        int setEditingState = methods.IndexOf("TextInput.setEditingState");
        int show = methods.IndexOf("TextInput.show");
        Assert.True(setClient >= 0 && setClient < setStyle, string.Join(", ", methods));
        Assert.True(setStyle < setEditingState && setEditingState < show, string.Join(", ", methods));
    }

    // Flutter: 'editable_text_test.dart: text styling info is sent on style update'
    [Fact]
    public void TextStylingInfoIsSentOnStyleUpdate()
    {
        var textStyle1 = new TextStyle(FontSize: 20.0, FontFamily: "RobotoMono", FontWeight: FontWeight.SemiBold);
        var textStyle2 = new TextStyle(
            FontSize: 20.0,
            FontFamily: "Raleway",
            FontWeight: FontWeight.Bold,
            LetterSpacing: 1.0,
            WordSpacing: 2.0);
        TextStyle currentTextStyle = textStyle1;
        StateSetter? setState = null;
        using var textInput = new TestTextInput();
        using FrameworkDartTester tester = PumpFocused(new StatefulBuilder((_, setter) =>
        {
            setState = setter;
            return Field(style: currentTextStyle);
        }));

        textInput.Log.Clear();
        setState!(() => currentTextStyle = textStyle2);
        tester.Pump();

        // Updated styling information should be sent via TextInput.setStyle method.
        MethodCall setStyle = textInput.Log.First(call => call.Method == "TextInput.setStyle");
        var arguments = (IDictionary)setStyle.Arguments!;
        Assert.Equal(20.0, Convert.ToDouble(arguments["fontSize"]));
        Assert.Equal("Raleway", arguments["fontFamily"]);
        Assert.Equal(6, Convert.ToInt32(arguments["fontWeightIndex"]));
        Assert.Equal(4, Convert.ToInt32(arguments["textAlignIndex"]));
        Assert.Equal(1, Convert.ToInt32(arguments["textDirectionIndex"]));
        Assert.Equal(1.0, Convert.ToDouble(arguments["letterSpacing"]));
        Assert.Equal(2.0, Convert.ToDouble(arguments["wordSpacing"]));
        Assert.Equal(
            tester.State<EditableText.EditableTextState>().RenderEditableObject.PreferredLineHeight,
            Convert.ToDouble(arguments["lineHeight"]));
    }

    // ------------------------------------------------------------ the host plugin

    [Fact]
    public void HostPluginTypesComposesAndCommitsIntoTheField()
    {
        using FrameworkDartTester tester = PumpFocused(Field());
        var plugin = new HostTextInputPlugin();
        plugin.Attach();
        try
        {
            Assert.Equal(tester.View.ViewId, plugin.ViewId);

            plugin.InsertText("ab");
            Assert.Equal("ab", _controller.Text);
            Assert.Equal(TextSelection.Collapsed(2), _controller.Selection);

            plugin.SetMarkedText("ni", 2);
            Assert.Equal("abni", _controller.Text);
            Assert.Equal(new TextRange(2, 4), _controller.Composing);

            plugin.InsertText("に");
            Assert.Equal("abに", _controller.Text);
            Assert.Null(_controller.Composing);
            Assert.Equal(TextSelection.Collapsed(3), _controller.Selection);

            Assert.True(plugin.SetSelection(0, 1));
            Assert.Equal(new TextSelection(0, 1), _controller.Selection);
        }
        finally
        {
            plugin.Detach();
        }
    }

    [Fact]
    public void HostPluginFollowsTheFrameworksEditingState()
    {
        using FrameworkDartTester tester = PumpFocused(Field());
        var plugin = new HostTextInputPlugin();
        plugin.Attach();
        try
        {
            // A framework-side edit reaches the plugin through TextInput.setEditingState.
            _controller.Value = new TextEditingValue("hello", TextSelection.Collapsed(1));
            tester.Pump();
            Assert.Equal("hello", plugin.ActiveModel!.GetText());
            Assert.Equal(new TextInputModel.Range(1, 1), plugin.ActiveModel.Selection);

            plugin.InsertText("X");
            Assert.Equal("hXello", _controller.Text);
        }
        finally
        {
            plugin.Detach();
        }

        // Losing focus clears the client.
        plugin.Attach();
        _focusNode.Unfocus();
        tester.Pump();
        Assert.Null(plugin.ActiveModel);
        plugin.Detach();
    }

    [Fact]
    public void SingleLineFieldsDropNewlinesTheHostTypes()
    {
        using FrameworkDartTester tester = PumpFocused(Field());

        Assert.True(HostTextInput.InsertText("a\nb"));

        Assert.Equal("ab", _controller.Text);
    }

    [Fact]
    public void ReadOnlyFieldsOpenNoConnectionSoTheHostTypesNothing()
    {
        var tester = new FrameworkDartTester();
        using (tester)
        {
            tester.PumpWidget(new TestWidgetsApp(
                home: new EditableText(controller: _controller, focusNode: _focusNode, readOnly: true)));
            _focusNode.RequestFocus();
            tester.Pump();

            Assert.False(HostTextInput.InsertText("x"));
            Assert.Equal(string.Empty, _controller.Text);
        }
    }

    // ------------------------------------------------------------- TextField.maxLength

    private FrameworkDartTester PumpTextField(TextField field)
    {
        var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new Center(child: new Material.Material(child: field))));
        _focusNode.RequestFocus();
        tester.Pump();
        return tester;
    }

    // Flutter: 'text_field_test.dart: maxLength limits input.'
    [Fact]
    public void MaxLengthLimitsInput()
    {
        using var textInput = new TestTextInput();
        using FrameworkDartTester tester = PumpTextField(
            new TextField(controller: _controller, focusNode: _focusNode, maxLength: 10));

        textInput.EnterText("0123456789101112");
        Assert.Equal("0123456789", _controller.Text);
    }

    // Flutter: 'text_field_test.dart: maxLength limits input with surrogate pairs.'
    [Fact]
    public void MaxLengthLimitsInputWithSurrogatePairs()
    {
        using var textInput = new TestTextInput();
        using FrameworkDartTester tester = PumpTextField(
            new TextField(controller: _controller, focusNode: _focusNode, maxLength: 10));

        const string surrogatePair = "😆";
        textInput.EnterText($"{surrogatePair}0123456789101112");
        Assert.Equal($"{surrogatePair}012345678", _controller.Text);
    }

    // Flutter: 'text_field_test.dart: maxLength limits input with grapheme clusters.'
    [Fact]
    public void MaxLengthLimitsInputWithGraphemeClusters()
    {
        using var textInput = new TestTextInput();
        using FrameworkDartTester tester = PumpTextField(
            new TextField(controller: _controller, focusNode: _focusNode, maxLength: 10));

        const string graphemeCluster = "👨‍👩‍👦";
        textInput.EnterText($"{graphemeCluster}0123456789101112");
        Assert.Equal($"{graphemeCluster}012345678", _controller.Text);
    }

    // Flutter: 'text_field_test.dart: maxLength limits input in the center of a maxed-out field.'
    [Fact]
    public void MaxLengthLimitsInputInTheCenterOfAMaxedOutField()
    {
        const string testValue = "0123456789";
        using var textInput = new TestTextInput();
        using FrameworkDartTester tester = PumpTextField(
            new TextField(controller: _controller, focusNode: _focusNode, maxLength: 10));

        // Max out the character limit in the field.
        textInput.EnterText(testValue);
        Assert.Equal(testValue, _controller.Text);

        // Entering more characters at the end does nothing.
        textInput.EnterText($"{testValue}9999999");
        Assert.Equal(testValue, _controller.Text);

        // Entering text in the middle of the field also does nothing.
        textInput.EnterText("0123455555555556789");
        Assert.Equal(testValue, _controller.Text);
    }

    // Flutter: 'text_field_test.dart: maxLength still works with other formatters'
    [Fact]
    public void MaxLengthStillWorksWithOtherFormatters()
    {
        using var textInput = new TestTextInput();
        using FrameworkDartTester tester = PumpTextField(new TextField(
            controller: _controller,
            focusNode: _focusNode,
            maxLength: 10,
            inputFormatters:
            [
                FilteringTextInputFormatter.Deny(new System.Text.RegularExpressions.Regex("[a-z]"), "#"),
            ]));

        textInput.EnterText("a一b二c三\nd四e五f六");
        // The default single line formatter replaces \n with empty string.
        Assert.Equal("#一#二#三#四#五", _controller.Text);
    }

    [Fact]
    public void MaxLengthEnforcementNoneDoesNotLimitInput()
    {
        using var textInput = new TestTextInput();
        using FrameworkDartTester tester = PumpTextField(new TextField(
            controller: _controller,
            focusNode: _focusNode,
            maxLength: 3,
            maxLengthEnforcement: MaxLengthEnforcement.None));

        textInput.EnterText("12345");
        Assert.Equal("12345", _controller.Text);
    }

    // ------------------------------------------------------------------ PlumixHost

    [Fact]
    public void WidgetHostServesTheImeFromThePlugin()
    {
        var host = new WidgetHost();
        try
        {
            host.RootWidget = new Directionality(
                Plumix.UI.TextDirection.Ltr,
                new Align(
                    alignment: Alignment.TopLeft,
                    child: new SizedBox(
                        width: 200,
                        child: new EditableText(controller: _controller, focusNode: _focusNode))));
            host.FlushPipelineForTests(new Size(300, 200));
            Assert.Null(RequestClient(host));

            _focusNode.RequestFocus();
            Scheduler.FlushMicrotasks();
            host.FlushPipelineForTests(new Size(300, 200));
            Scheduler.PumpFrameForTests();

            TextInputMethodClient client = Assert.IsAssignableFrom<TextInputMethodClient>(RequestClient(host));
            Assert.True(client.SupportsPreedit);

            // Committed text arrives as Avalonia's TextInput event.
            host.RaiseEvent(new AvaloniaTextInputEventArgs
            {
                RoutedEvent = AvaloniaInputElement.TextInputEvent,
                Text = "abcd",
            });
            Assert.Equal("abcd", _controller.Text);
            Assert.True(client.SupportsSurroundingText);
            Assert.Equal("abcd", client.SurroundingText);
            Assert.Equal(new AvaloniaTextSelection(4, 4), client.Selection);

            client.SetPreeditText("ni", 1);
            Assert.Equal("abcdni", _controller.Text);
            Assert.Equal(new TextRange(4, 6), _controller.Composing);
            Assert.Equal(TextSelection.Collapsed(5), _controller.Selection);

            client.SetPreeditText(null);
            Assert.Equal("abcd", _controller.Text);
            Assert.Null(_controller.Composing);

            client.Selection = new AvaloniaTextSelection(1, 3);
            Assert.Equal(new TextSelection(1, 3), _controller.Selection);

            host.FlushPipelineForTests(new Size(300, 200));
            Scheduler.PumpFrameForTests();
            Rect cursor = client.CursorRectangle;
            Assert.True(cursor.Height > 0, cursor.ToString());
        }
        finally
        {
            host.RootWidget = null;
            Scheduler.PumpFrameForTests();
        }
    }

    private static TextInputMethodClient? RequestClient(PlumixHost host)
    {
        var request = new TextInputMethodClientRequestedEventArgs
        {
            RoutedEvent = AvaloniaInputElement.TextInputMethodClientRequestedEvent,
        };
        host.RaiseEvent(request);
        return request.Client;
    }
}
