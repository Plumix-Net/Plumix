using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using TextDirection = Plumix.UI.TextDirection;

// Dart parity source: flutter/packages/flutter/test/widgets/editable_text_test.dart;
// flutter/packages/flutter/test/services/text_input_test.dart;
// flutter/engine/src/flutter/testing/dart/text_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class TextEditingControllerDartParityTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hello")]
    public void InitialAndAssignedTextHaveNoSelectionOrComposing(string? text)
    {
        using var controller = new TextEditingController(text);
        Assert.Equal(text ?? string.Empty, controller.Text);
        Assert.Equal(TextSelection.Invalid, controller.Selection);
        Assert.Equal(TextRange.Empty, controller.Composing);

        controller.Value = new TextEditingValue("old", new TextSelection(0, 3), new TextRange(0, 3));
        controller.Text = text ?? string.Empty;
        Assert.Equal(new TextEditingValue(text ?? string.Empty), controller.Value);
    }

    [Fact]
    public void EmptyValueAndZeroInitializedStructHaveDartDefaultsAndEquality()
    {
        Assert.Equal(TextEditingValue.Empty, default(TextEditingValue));
        Assert.Equal(TextEditingValue.Empty.GetHashCode(), default(TextEditingValue).GetHashCode());
        Assert.Equal(TextSelection.Invalid, new TextEditingValue("hello").Selection);
        Assert.Equal(TextRange.Empty, new TextEditingValue("hello").Composing);
        Assert.Equal(
            "TextEditingValue(text: ┤├, selection: TextSelection.invalid, composing: TextRange(start: -1, end: -1))",
            TextEditingValue.Empty.ToString());
        using var emptyController = TextEditingController.FromValue();
        Assert.Equal(TextEditingValue.Empty, emptyController.Value);
    }

    [Fact]
    public void ValueAndFromValuePreserveRangesAndSelectionMetadata()
    {
        var value = new TextEditingValue("hello", new TextSelection(7, 9, TextAffinity.Upstream, true),
            new TextRange(-1, 9));
        using var controller = TextEditingController.FromValue(value);
        Assert.Equal(value, controller.Value);
        controller.Value = value.CopyWith(selection: TextSelection.Invalid, composing: TextRange.Collapsed(2));
        Assert.Equal(TextSelection.Invalid, controller.Selection);
        Assert.Equal(TextRange.Collapsed(2), controller.Composing);
        Assert.True(controller.Value.IsComposingRangeValid);
    }

    [DebugOnlyTheory]
    [InlineData(1, 0)]
    [InlineData(1, 9)]
    [InlineData(9, 9)]
    public void ValueAndFromValueAssertOnInvalidNonEmptyComposing(int start, int end)
    {
        var invalid = new TextEditingValue("test", composing: new TextRange(start, end));
        Assert.Throws<AssertionError>(() => TextEditingController.FromValue(invalid));
        using var controller = new TextEditingController("test");
        Assert.Throws<AssertionError>(() => controller.Value = invalid);
        Assert.Equal(new TextEditingValue("test"), controller.Value);
    }

    [Theory]
    [InlineData(5, 5, true)]
    [InlineData(4, 4, true)]
    [InlineData(12, 12, true)]
    [InlineData(5, 7, true)]
    [InlineData(7, 5, true)]
    [InlineData(2, 2, false)]
    [InlineData(14, 14, false)]
    [InlineData(1, 2, false)]
    [InlineData(13, 15, false)]
    [InlineData(-1, -1, false)]
    public void SelectionPreservesComposingOnlyWithinInclusiveBounds(int start, int end, bool preserve)
    {
        var composing = new TextRange(4, 12);
        using var controller = TextEditingController.FromValue(
            new TextEditingValue("text composing text", composing: composing));
        controller.Selection = new TextSelection(start, end);
        Assert.Equal(new TextSelection(start, end), controller.Selection);
        Assert.Equal(preserve ? composing : TextRange.Empty, controller.Composing);
    }

    [Theory]
    [InlineData(0, 6)]
    [InlineData(6, 0)]
    [InlineData(6, 6)]
    public void SelectionOverflowThrowsWithoutChangingValue(int start, int end)
    {
        using var controller = new TextEditingController("hello");
        TextEditingValue previous = controller.Value;
        var error = Assert.Throws<FlutterError>(() => controller.Selection = new TextSelection(start, end));
        Assert.Contains("invalid text selection:", error.ToString());
        Assert.Equal(previous, controller.Value);
        controller.Selection = TextSelection.Invalid;
        Assert.Equal(previous, controller.Value);
    }

    [Fact]
    public void ClearAndClearComposingNotifyOnlyForChangedValues()
    {
        using var controller = TextEditingController.FromValue(
            new TextEditingValue("hello", new TextSelection(2, 4), TextRange.Collapsed(3)));
        int notifications = 0;
        controller.AddListener(() => notifications++);
        controller.ClearComposing();
        Assert.Equal(new TextEditingValue("hello", new TextSelection(2, 4)), controller.Value);
        Assert.Equal(1, notifications);
        controller.ClearComposing();
        Assert.Equal(1, notifications);
        controller.Clear();
        Assert.Equal(new TextEditingValue(selection: TextSelection.Collapsed(0)), controller.Value);
        Assert.Equal(2, notifications);
        controller.Clear();
        Assert.Equal(2, notifications);
    }

    [Fact]
    public void ListenerCanSynchronouslyRewriteTextAndSelection()
    {
        using var controller = new TextEditingController();
        controller.AddListener(() =>
        {
            if (controller.Text == "12")
            {
                controller.Text = "5.5555";
                controller.Selection = TextSelection.Collapsed(6);
            }
        });
        controller.Text = "12";
        Assert.Equal(new TextEditingValue("5.5555", TextSelection.Collapsed(6)), controller.Value);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(1, 3)]
    [InlineData(2, 2)]
    public void BuildTextSpanKeepsBeforeInsideAfterAndUnderline(int start, int end)
    {
        using var controller = TextEditingController.FromValue(
            new TextEditingValue("test", composing: new TextRange(start, end)));
        var style = new TextStyle(FontSize: 18, Color: new Color(0xFF123456));
        TextSpan span = controller.BuildTextSpan(null!, style, withComposing: true);
        Assert.Same(style, span.Style);
        Assert.Null(span.Text);
        Assert.Equal(3, span.Children!.Count);
        Assert.Equal("test"[..start], Assert.IsType<TextSpan>(span.Children[0]).Text);
        TextSpan composing = Assert.IsType<TextSpan>(span.Children[1]);
        Assert.Equal("test"[start..end], composing.Text);
        Assert.Equal(TextDecoration.Underline, composing.Style!.Decoration);
        Assert.Equal(18, composing.Style.FontSize);
        Assert.Equal(style.Color, composing.Style.Color);
        Assert.Equal("test"[end..], Assert.IsType<TextSpan>(span.Children[2]).Text);
        TextSpan plain = controller.BuildTextSpan(null!, style, withComposing: false);
        Assert.Equal("test", plain.Text);
        Assert.Null(plain.Children);
    }

    [Fact]
    public void BuildTextSpanWithNoStyleUnderlinesComposingAndIgnoresInvalidRange()
    {
        using var controller = TextEditingController.FromValue(
            new TextEditingValue("test", composing: new TextRange(1, 3)));
        TextSpan span = controller.BuildTextSpan(null!, null, withComposing: true);
        Assert.Equal(TextDecoration.Underline, Assert.IsType<TextSpan>(span.Children![1]).Style!.Decoration);
        controller.Value = controller.Value.CopyWith(composing: new TextRange(-1, 3));
        Assert.Equal("test", controller.BuildTextSpan(null!, null, withComposing: true).Text);
    }

    [Fact]
    public void InvalidComposingAssertsInDebugAndBuildsPlainTextInProduction()
    {
        using var controller = new TextEditingController("test");
        var value = new TextEditingValue("test", composing: new TextRange(1, 9));
        if (Constants.KDebugMode)
        {
            Assert.Throws<AssertionError>(() => controller.Value = value);
            return;
        }

        controller.Value = value;
        Assert.Equal(value, controller.Value);
        TextSpan span = controller.BuildTextSpan(null!, null, withComposing: true);
        Assert.Equal("test", span.Text);
        Assert.Null(span.Children);
    }

    [Fact]
    public void OverriddenBuildTextSpanReceivesInheritedContext()
    {
        using var tester = new FrameworkDartTester();
        var color = new Color(0xFF010203);
        using var controller = new ContextController();
        tester.PumpWidget(new Directionality(TextDirection.Ltr,
            new DefaultTextStyle(new TextStyle(Color: color), new EditableText(
                controller,
                focusNode: new FocusNode(),
                style: new TextStyle(FontSize: 14.0, Color: new Color(0xFF000000)),
                cursorColor: new Color(0xFF000000),
                backgroundCursorColor: new Color(0xFF9E9E9E)))));
        Assert.Equal(color, controller.ContextColor);
    }

    [Fact]
    public void FocusAndFocusedTextResetRestoreCaretButUnfocusedResetStaysInvalid()
    {
        FocusManager.Instance.ResetForTests();
        using var tester = new FrameworkDartTester();
        using var focus = new FocusNode();
        using var controller = new TextEditingController("hello");
        tester.PumpWidget(new Directionality(TextDirection.Ltr,
            new Align(alignment: Alignment.TopLeft, child: new SizedBox(width: 200,
                child: new EditableText(
                    controller,
                    focusNode: focus,
                    selectAllOnFocus: false,
                    style: new TextStyle(FontSize: 14.0, Color: new Color(0xFF000000)),
                    cursorColor: new Color(0xFF000000),
                    backgroundCursorColor: new Color(0xFF9E9E9E))))));
        Assert.Equal(TextSelection.Invalid, controller.Selection);
        focus.RequestFocus();
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(5), controller.Selection);
        controller.Text = "new text";
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(8), controller.Selection);
        focus.Unfocus();
        tester.Pump();
        controller.Text = string.Empty;
        tester.Pump();
        Assert.Equal(TextSelection.Invalid, controller.Selection);
        var state = (EditableText.EditableTextState)((StatefulElement)tester.ElementOfType<EditableText>()).State;
        Assert.Equal(string.Empty, state.RenderEditableObject.Text!.ToPlainText());
    }

    [Fact]
    public void PublishedEditingHelpersHandleInvalidSelectionAndEmptyComposing()
    {
        using var controller = new TextEditingController("ab");
        Assert.True(controller.Insert("c"));
        Assert.Equal(new TextEditingValue("abc", TextSelection.Collapsed(3)), controller.Value);
        controller.Text = "ab";
        Assert.True(controller.SetComposing("x"));
        Assert.Equal("abx", controller.Text);
        Assert.Equal(new TextRange(2, 3), controller.Composing);
        Assert.True(controller.CommitComposing("y"));
        Assert.Equal(new TextEditingValue("aby", TextSelection.Collapsed(3)), controller.Value);
        controller.Text = "ab";
        Assert.False(controller.DeleteForward());
        Assert.Equal(new TextEditingValue("ab"), controller.Value);
    }

    [Theory]
    [InlineData(1, 2, "h", "llo", "e")]
    [InlineData(0, 0, "", "hello", "")]
    [InlineData(5, 5, "hello", "", "")]
    [InlineData(0, 5, "", "", "hello")]
    public void RangeSubstringsUseExactOffsets(int start, int end, string before, string after, string inside)
    {
        var range = new TextRange(start, end);
        Assert.Equal(before, range.TextBefore("hello"));
        Assert.Equal(after, range.TextAfter("hello"));
        Assert.Equal(inside, range.TextInside("hello"));
        var selection = new TextSelection(end, start);
        Assert.Equal(before, selection.TextBefore("hello"));
        Assert.Equal(after, selection.TextAfter("hello"));
        Assert.Equal(inside, selection.TextInside("hello"));
    }

    [Fact]
    public void RangeQueriesKeepInvalidCollapsedAndReversedRanges()
    {
        Assert.Equal(TextRange.Empty, TextRange.Collapsed(-1));
        Assert.True(TextRange.Empty.IsCollapsed);
        Assert.True(TextRange.Empty.IsNormalized);
        Assert.False(TextRange.Empty.IsValid);
        Assert.True(new TextRange(10, 0).IsValid);
        Assert.False(new TextRange(10, 0).IsNormalized);
        Assert.False(new TextRange(-1, 10).IsValid);
        Assert.True(new TextRange(-1, 10).IsNormalized);
        Assert.False(new TextRange(10, -1).IsNormalized);
        Assert.Throws<ArgumentOutOfRangeException>(() => TextRange.Empty.TextBefore("hello"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextRange(0, 6).TextInside("hello"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextRange(0, 6).TextAfter("hello"));
    }

    [DebugOnlyFact]
    public void RangeConstructorsAndSubstringsAssertDartPreconditions()
    {
        Assert.Throws<AssertionError>(() => new TextRange(-2, 0));
        Assert.Throws<AssertionError>(() => TextRange.Collapsed(-2));
        Assert.Throws<AssertionError>(() => new TextSelection(-2, 0));
        Assert.Throws<AssertionError>(() => TextSelection.Collapsed(-2));
        var reversed = new TextRange(2, 1);
        Assert.Throws<AssertionError>(() => reversed.TextBefore("hello"));
        Assert.Throws<AssertionError>(() => reversed.TextInside("hello"));
        Assert.Throws<AssertionError>(() => reversed.TextAfter("hello"));
    }

    [Fact]
    public void ValueCopiesJsonAndReplacementPreserveCollapsedComposingAndInvalidSelection()
    {
        var value = new TextEditingValue("hello", composing: TextRange.Collapsed(2));
        Assert.Equal(value, value.CopyWith());
        Assert.Equal(value, TextEditingValue.FromJson(value.ToJson()));
        Assert.Equal(TextRange.Collapsed(2), value.Replaced(new TextRange(0, 1), "H").Composing);
        TextEditingValue replaced = value.Replaced(new TextRange(0, 1), "Hi");
        Assert.Equal(TextSelection.Invalid, replaced.Selection);
        Assert.Equal(TextRange.Collapsed(3), replaced.Composing);
        Assert.Equal("Hiello", replaced.Text);
        Assert.Equal(TextRange.Empty, new TextEditingValue("hello").Replaced(TextRange.Collapsed(0), "x").Composing);
        Assert.Throws<ArgumentException>(() => TextEditingValue.FromJson(new Dictionary<string, object?>()));
        Assert.Throws<ArgumentException>(() => TextEditingValue.FromJson(new Dictionary<string, object?>
        {
            ["text"] = 123,
        }));
        Assert.Equal(new TextEditingValue("hello"), TextEditingValue.FromJson(new Dictionary<string, object?>
        {
            ["text"] = "hello",
            ["selectionAffinity"] = "unknown",
        }));
    }

    [Fact]
    public void ReplacementRejectsReversedAndOutOfBoundsRanges()
    {
        var value = new TextEditingValue("hello");
        Assert.Throws<ArgumentOutOfRangeException>(() => value.Replaced(new TextRange(3, 1), "x"));
        Assert.Throws<ArgumentOutOfRangeException>(() => value.Replaced(new TextRange(0, 6), "x"));
        Assert.Equal(value, value.Replaced(TextRange.Empty, "x"));
    }

    [Fact]
    public void SelectionCopyPreservesFieldsAndRecomputesRange()
    {
        var selection = new TextSelection(4, 2, TextAffinity.Upstream, true);
        Assert.Equal(selection, selection.CopyWith());
        Assert.Equal(new TextSelection(1, 2, TextAffinity.Upstream, true), selection.CopyWith(baseOffset: 1));
        Assert.Equal(new TextSelection(4, 6, TextAffinity.Downstream, false),
            selection.CopyWith(extentOffset: 6, affinity: TextAffinity.Downstream, isDirectional: false));
    }

    [Fact]
    public void FilteringPreservesInvalidSelectionAndValidSelectionMetadata()
    {
        var formatter = FilteringTextInputFormatter.DigitsOnly;
        var invalid = new TextEditingValue("a1b2", composing: TextRange.Empty);
        TextEditingValue result = formatter.FormatEditUpdate(TextEditingValue.Empty, invalid);
        Assert.Equal(new TextEditingValue("12"), result);
        var selection = new TextSelection(3, 1, TextAffinity.Upstream, true);
        result = formatter.FormatEditUpdate(TextEditingValue.Empty,
            new TextEditingValue("a1b2", selection, TextRange.Collapsed(2)));
        Assert.Equal(new TextSelection(1, 0, TextAffinity.Upstream, true), result.Selection);
        Assert.Equal(TextRange.Empty, result.Composing);
    }

    [Fact]
    public void FilteringNormalizesUnchangedInvalidSelectionAndCollapsedComposing()
    {
        var formatter = FilteringTextInputFormatter.SingleLineFormatter;
        var value = new TextEditingValue("text", new TextSelection(-1, 2), TextRange.Collapsed(2));
        TextEditingValue result = formatter.FormatEditUpdate(TextEditingValue.Empty, value);
        Assert.Equal(-1, result.Selection.BaseOffset);
        Assert.Equal(-1, result.Selection.ExtentOffset);
        Assert.Equal(TextRange.Empty, result.Composing);
        result = FilteringTextInputFormatter.DigitsOnly.FormatEditUpdate(TextEditingValue.Empty,
            new TextEditingValue("a1", TextSelection.Collapsed(1), new TextRange(0, 1)));
        Assert.Equal(TextRange.Empty, result.Composing);
    }

    [Fact]
    public void FilteringMapsBannedInteriorToReplacementEndAndKeepsEqualLengthOffsets()
    {
        var formatter = FilteringTextInputFormatter.Deny(new System.Text.RegularExpressions.Regex("o+"), "*");
        TextEditingValue result = formatter.FormatEditUpdate(TextEditingValue.Empty,
            new TextEditingValue("foo", TextSelection.Collapsed(2)));
        Assert.Equal(new TextEditingValue("f*", TextSelection.Collapsed(2)), result);
        formatter = FilteringTextInputFormatter.Deny(new System.Text.RegularExpressions.Regex("o+"), "**");
        result = formatter.FormatEditUpdate(TextEditingValue.Empty,
            new TextEditingValue("foo", TextSelection.Collapsed(2), new TextRange(1, 2)));
        Assert.Equal(new TextEditingValue("f**", TextSelection.Collapsed(2), new TextRange(1, 2)), result);
    }

    private sealed class ContextController : TextEditingController
    {
        public Color? ContextColor { get; private set; }

        public override TextSpan BuildTextSpan(BuildContext context, TextStyle? style, bool withComposing)
        {
            ContextColor = DefaultTextStyle.Of(context).Style.Color;
            return base.BuildTextSpan(context, style, withComposing);
        }
    }
}
