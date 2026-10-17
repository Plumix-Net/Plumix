using System.Collections;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ports flutter/packages/flutter/test/widgets/editable_text_styles_test.dart, the bold text and text
// spacing override cases of editable_text_test.dart ('text styling info is sent on show keyboard',
// '... (bold override)', 'EditableText respects MediaQuery.boldText', 'EditableText respects
// MediaQueryData.lineHeightScaleFactorOverride, ...') and selectable_text_test.dart's 'SelectableText
// respects MediaQueryData.lineHeightScaleFactorOverride, ...'.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class EditableTextStylesDartParityTests : IDisposable
{
    private readonly FakeTextInputControl _control = new();
    private readonly TextEditingController _controller = new();
    private readonly FocusNode _focusNode = new();

    public EditableTextStylesDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        Plumix.UI.TextInput.DebugReset();
    }

    public void Dispose()
    {
        Plumix.UI.TextInput.RestorePlatformInputControl();
        Plumix.UI.TextInput.DebugReset();
        FocusManager.Instance.ResetForTests();
        _focusNode.Dispose();
        _controller.Dispose();
    }

    // editable_text_styles_test.dart's harness: MediaQuery > Directionality(ltr) > EditableText.
    private Widget StylesHarness(MediaQueryData data, TextStyle style) => new MediaQuery(
        data: data,
        child: new Directionality(
            TextDirection.Ltr,
            new EditableText(
                controller: _controller,
                focusNode: _focusNode,
                style: style,
                cursorColor: new Color(0xFF000000),
                backgroundCursorColor: new Color(0xFF000000))));

    private FrameworkDartTester PumpAndTap(MediaQueryData data, TextStyle style)
    {
        Plumix.UI.TextInput.SetInputControl(_control);
        var tester = new FrameworkDartTester();
        tester.PumpWidget(StylesHarness(data, style));
        tester.Tap(tester.ElementOfType<EditableText>());
        tester.Pump();
        return tester;
    }

    // Flutter: 'editable_text_styles_test.dart: EditableText updates style when boldText changes'
    [Fact]
    public void EditableTextUpdatesStyleWhenBoldTextChanges()
    {
        var style = new TextStyle(FontWeight: FontWeight.Normal);
        using FrameworkDartTester tester = PumpAndTap(new MediaQueryData(), style);
        Assert.NotNull(_control.LastStyle);
        Assert.Equal(FontWeight.Normal, _control.LastStyle!.FontWeight);

        _control.LastStyle = null;
        tester.PumpWidget(StylesHarness(new MediaQueryData(BoldText: true), style));
        tester.Pump();
        Assert.NotNull(_control.LastStyle);
        Assert.Equal(FontWeight.Bold, _control.LastStyle!.FontWeight);
    }

    // Flutter: 'editable_text_styles_test.dart: EditableText updates style when letterSpacingOverride
    // changes'
    [Fact]
    public void EditableTextUpdatesStyleWhenLetterSpacingOverrideChanges()
    {
        var style = new TextStyle(LetterSpacing: 1.0);
        using FrameworkDartTester tester = PumpAndTap(new MediaQueryData(), style);
        Assert.Equal(1.0, _control.LastStyle?.LetterSpacing);

        _control.LastStyle = null;
        tester.PumpWidget(StylesHarness(new MediaQueryData(LetterSpacingOverride: 5.0), style));
        tester.Pump();
        Assert.NotNull(_control.LastStyle);
        Assert.Equal(5.0, _control.LastStyle!.LetterSpacing);
    }

    // Flutter: 'editable_text_styles_test.dart: EditableText updates style when wordSpacingOverride
    // changes'
    [Fact]
    public void EditableTextUpdatesStyleWhenWordSpacingOverrideChanges()
    {
        var style = new TextStyle(WordSpacing: 2.0);
        using FrameworkDartTester tester = PumpAndTap(new MediaQueryData(), style);
        Assert.Equal(2.0, _control.LastStyle?.WordSpacing);

        _control.LastStyle = null;
        tester.PumpWidget(StylesHarness(new MediaQueryData(WordSpacingOverride: 10.0), style));
        tester.Pump();
        Assert.NotNull(_control.LastStyle);
        Assert.Equal(10.0, _control.LastStyle!.WordSpacing);
    }

    // Flutter: 'editable_text_styles_test.dart: EditableText updates style when
    // lineHeightScaleFactorOverride changes'
    [Fact]
    public void EditableTextUpdatesStyleWhenLineHeightScaleFactorOverrideChanges()
    {
        var style = new TextStyle(FontSize: 20.0, Height: 1.0);
        using FrameworkDartTester tester = PumpAndTap(new MediaQueryData(LineHeightScaleFactorOverride: 1.0), style);
        Assert.Equal(20.0, _control.LastStyle!.LineHeight);

        _control.LastStyle = null;
        tester.PumpWidget(StylesHarness(new MediaQueryData(LineHeightScaleFactorOverride: 2.0), style));
        tester.Pump();
        Assert.NotNull(_control.LastStyle);
        Assert.Equal(40.0, _control.LastStyle!.LineHeight);
    }

    private IDictionary FirstSetStyleOnShowKeyboard(MediaQueryData data)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MediaQuery(
            data: data,
            child: new EditableText(
                controller: _controller,
                focusNode: _focusNode,
                textDirection: TextDirection.Rtl,
                style: new TextStyle(FontSize: 20.0, FontFamily: "Roboto", FontWeight: FontWeight.SemiBold),
                cursorColor: Colors.Blue,
                backgroundCursorColor: Colors.Gray)));
        tester.ShowKeyboard(Find.ByType<EditableText>());
        MethodCall setStyle = tester.TestTextInput.Log.First(call => call.Method == "TextInput.setStyle");
        return (IDictionary)setStyle.Arguments!;
    }

    // Flutter: 'editable_text_test.dart: text styling info is sent on show keyboard'
    [Fact]
    public void TextStylingInfoIsSentOnShowKeyboard()
    {
        IDictionary arguments = FirstSetStyleOnShowKeyboard(new MediaQueryData());
        Assert.Equal(20.0, Convert.ToDouble(arguments["fontSize"]));
        Assert.Equal("Roboto", arguments["fontFamily"]);
        Assert.Equal(5, Convert.ToInt32(arguments["fontWeightIndex"]));
        Assert.Equal(4, Convert.ToInt32(arguments["textAlignIndex"]));
        Assert.Equal(0, Convert.ToInt32(arguments["textDirectionIndex"]));
        Assert.Null(arguments["letterSpacing"]);
        Assert.Null(arguments["wordSpacing"]);
        Assert.Equal(20.0, Convert.ToDouble(arguments["lineHeight"]));
    }

    // Flutter: 'editable_text_test.dart: text styling info is sent on show keyboard (bold override)'
    [Fact]
    public void TextStylingInfoIsSentOnShowKeyboardBoldOverride()
    {
        IDictionary arguments = FirstSetStyleOnShowKeyboard(new MediaQueryData(BoldText: true));
        Assert.Equal(20.0, Convert.ToDouble(arguments["fontSize"]));
        Assert.Equal("Roboto", arguments["fontFamily"]);
        // FontWeight.bold.index: w600 merged with bold is w700.
        Assert.Equal(6, Convert.ToInt32(arguments["fontWeightIndex"]));
        Assert.Equal(4, Convert.ToInt32(arguments["textAlignIndex"]));
        Assert.Equal(0, Convert.ToInt32(arguments["textDirectionIndex"]));
        Assert.Null(arguments["letterSpacing"]);
        Assert.Null(arguments["wordSpacing"]);
        Assert.Equal(20.0, Convert.ToDouble(arguments["lineHeight"]));
    }

    // Flutter: 'editable_text_test.dart: EditableText respects MediaQuery.boldText'
    [Fact]
    public void EditableTextRespectsMediaQueryBoldText()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                data: new MediaQueryData(BoldText: true),
                child: new EditableText(
                    controller: _controller,
                    focusNode: _focusNode,
                    style: new TextStyle(FontWeight: FontWeight.Normal),
                    cursorColor: Colors.Red,
                    backgroundCursorColor: Colors.Green))));

        _controller.Text = "foo";
        EditableText.EditableTextState state = tester.State<EditableText.EditableTextState>();
        Assert.Equal(FontWeight.Bold, state.BuildTextSpan().Style!.FontWeight);
    }

    // Flutter: 'editable_text_test.dart: EditableText respects
    // MediaQueryData.lineHeightScaleFactorOverride, MediaQueryData.letterSpacingOverride, and
    // MediaQueryData.wordSpacingOverride'
    [Fact]
    public void EditableTextRespectsTheTextSpacingOverrides()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                data: new MediaQueryData(
                    LineHeightScaleFactorOverride: 2.0,
                    LetterSpacingOverride: 2.0,
                    WordSpacingOverride: 2.0),
                child: new EditableText(
                    controller: _controller,
                    focusNode: _focusNode,
                    style: new TextStyle(FontWeight: FontWeight.Normal),
                    strutStyle: new StrutStyle(Height: 0.9),
                    cursorColor: Colors.Red,
                    backgroundCursorColor: Colors.Green))));

        _controller.Text = "foo";
        RenderEditable renderEditable = tester.State<EditableText.EditableTextState>().RenderEditableObject;
        Assert.Equal(2.0, renderEditable.Text?.Style?.Height);
        Assert.Equal(2.0, renderEditable.Text?.Style?.LetterSpacing);
        Assert.Equal(2.0, renderEditable.Text?.Style?.WordSpacing);
        Assert.Equal(2.0, renderEditable.StrutStyle?.Height);
    }

    // Flutter: 'selectable_text_test.dart: SelectableText respects
    // MediaQueryData.lineHeightScaleFactorOverride, MediaQueryData.letterSpacingOverride, and
    // MediaQueryData.wordSpacingOverride'
    [Fact]
    public void SelectableTextRespectsTheTextSpacingOverrides()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                data: new MediaQueryData(
                    LineHeightScaleFactorOverride: 2.0,
                    LetterSpacingOverride: 2.0,
                    WordSpacingOverride: 2.0),
                child: new SelectableText("hello world", strutStyle: new StrutStyle(Height: 0.9)))));

        RenderEditable renderEditable = tester.State<EditableText.EditableTextState>().RenderEditableObject;
        Assert.Equal(2.0, renderEditable.Text?.Style?.Height);
        Assert.Equal(2.0, renderEditable.Text?.Style?.LetterSpacing);
        Assert.Equal(2.0, renderEditable.Text?.Style?.WordSpacing);
        Assert.Equal(2.0, renderEditable.StrutStyle?.Height);
    }

    // editable_text_styles_test.dart's `FakeTextInputControl with TextInputControl`.
    private sealed class FakeTextInputControl : TextInputControl
    {
        public TextInputStyle? LastStyle { get; set; }

        public List<string> MethodCalls { get; } = [];

        public override void UpdateStyle(TextInputStyle style)
        {
            LastStyle = style;
            MethodCalls.Add("updateStyle");
        }
    }
}
