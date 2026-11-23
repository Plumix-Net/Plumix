using CoreGraphics;
using Foundation;
using ObjCRuntime;
using Plumix.Accessibility;
using Plumix.Rendering;
using UIKit;

namespace Plumix.iOS;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/darwin/ios/framework/Source/TextInputSemanticsObject.mm. An editable text
// field's element presents a UITextInput to UIKit: the text input plugin's view while the field is focused (so
// VoiceOver's rotor and editing commands act on the real editing state), else a read-only stand-in holding the node's
// value.

// The bindings declare UITextInput's results non-null where UIKit takes nil.
#pragma warning disable CS8764, CS8766

/// <summary>TextInputSemanticsObject.</summary>
internal sealed class TextInputSemanticsElement : SemanticsElement, IUITextInput
{
    // kUIAccessibilityTraitUndocumentedEmptyLine: an empty text view's trait, never wanted on a field.
    private const ulong UndocumentedEmptyLine = 0x800000000000;

    private readonly InactiveTextInput _inactiveTextInput = new();

    public TextInputSemanticsElement(IosAccessibilityBridge owner, int id)
        : base(owner, id, IOSSemanticsObjectKind.TextInput)
    {
    }

    private bool IsFocused => Node?.HasFlag(SemanticsFlags.IsFocused) == true;

    // textInputSurrogate.
    private UIView Surrogate => IsFocused && Owner.TextInputView is { } view ? view : _inactiveTextInput;

    private IUITextInput SurrogateInput => (IUITextInput)Surrogate;

    // setSemanticsNode: the focused field's input view points back at this element (so a stray VoiceOver focus on it
    // returns here) and gets a frame that makes it visible to UIKit.
    public override void NodeUpdated(AccessibilityNode node)
    {
        base.NodeUpdated(node);
        _inactiveTextInput.Text = node.Value;
        if (Owner.TextInputView is not { } textInput)
        {
            return;
        }

        if (node.HasFlag(SemanticsFlags.IsFocused))
        {
            textInput.BackingTextInputAccessibilityObject = this;
            textInput.Frame = new CGRect(0, 0, 1, 1);
        }
        else if (ReferenceEquals(textInput.BackingTextInputAccessibilityObject, this))
        {
            textInput.BackingTextInputAccessibilityObject = null;
        }
    }

    [Export("textInputView")]
    public UIView TextInputView => Surrogate;

    public override void AccessibilityElementDidBecomeFocused()
    {
        Surrogate.AccessibilityElementDidBecomeFocused();
        base.AccessibilityElementDidBecomeFocused();
    }

    public override void AccessibilityElementDidLoseFocus()
    {
        Surrogate.AccessibilityElementDidLoseFocus();
        base.AccessibilityElementDidLoseFocus();
    }

    [Export("accessibilityElementIsFocused")]
    public bool AccessibilityElementIsFocusedValue() => IsFocused;

    public override bool AccessibilityActivate() => Surrogate.AccessibilityActivate();

    public override string? AccessibilityLabel
    {
        get => base.AccessibilityLabel ?? Surrogate.AccessibilityLabel;
        set { }
    }

    public override string? AccessibilityHint
    {
        get => base.AccessibilityHint ?? Surrogate.AccessibilityHint;
        set { }
    }

    public override string? AccessibilityValue
    {
        get => base.AccessibilityValue ?? Surrogate.AccessibilityValue;
        set { }
    }

    public override ulong AccessibilityTraits
    {
        get => (base.AccessibilityTraits | (ulong)Surrogate.AccessibilityTraits) & ~UndocumentedEmptyLine;
        set { }
    }

    // UITextInput, forwarded to the surrogate.
    [Export("textInRange:")]
    public string? TextInRange(UITextRange range) => SurrogateInput.TextInRange(range);

    [Export("replaceRange:withText:")]
    public void ReplaceText(UITextRange range, string text) => SurrogateInput.ReplaceText(range, text);

    [Export("shouldChangeTextInRange:replacementText:")]
    public bool ShouldChangeTextInRange(UITextRange inRange, string replacementText) =>
        Surrogate is IosTextInputView view && view.ShouldChangeTextInRange(inRange, replacementText);

    [Export("selectedTextRange")]
    public UITextRange? SelectedTextRange
    {
        get => SurrogateInput.SelectedTextRange;
        set => SurrogateInput.SelectedTextRange = value!;
    }

    [Export("markedTextRange")]
    public UITextRange? MarkedTextRange => SurrogateInput.MarkedTextRange;

    [Export("markedTextStyle")]
    public NSDictionary? MarkedTextStyle
    {
        get => SurrogateInput.MarkedTextStyle;
        set => SurrogateInput.MarkedTextStyle = value!;
    }

    [Export("setMarkedText:selectedRange:")]
    public void SetMarkedText(string? markedText, NSRange selectedRange) =>
        SurrogateInput.SetMarkedText(markedText!, selectedRange);

    [Export("unmarkText")]
    public void UnmarkText() => SurrogateInput.UnmarkText();

    [Export("beginningOfDocument")]
    public UITextPosition BeginningOfDocument => SurrogateInput.BeginningOfDocument;

    [Export("endOfDocument")]
    public UITextPosition EndOfDocument => SurrogateInput.EndOfDocument;

    [Export("inputDelegate", ArgumentSemantic.Weak)]
    public NSObject? WeakInputDelegate
    {
        get => SurrogateInput.WeakInputDelegate;
        set => SurrogateInput.WeakInputDelegate = value!;
    }

    [Export("tokenizer")]
    public NSObject WeakTokenizer => SurrogateInput.WeakTokenizer;

    [Export("textRangeFromPosition:toPosition:")]
    public UITextRange GetTextRange(UITextPosition fromPosition, UITextPosition toPosition) =>
        SurrogateInput.GetTextRange(fromPosition, toPosition);

    [Export("positionFromPosition:offset:")]
    public UITextPosition? GetPosition(UITextPosition fromPosition, nint offset) =>
        SurrogateInput.GetPosition(fromPosition, offset);

    [Export("positionFromPosition:inDirection:offset:")]
    public UITextPosition? GetPosition(UITextPosition fromPosition, UITextLayoutDirection inDirection, nint offset) =>
        SurrogateInput.GetPosition(fromPosition, inDirection, offset);

    [Export("comparePosition:toPosition:")]
    public NSComparisonResult ComparePosition(UITextPosition first, UITextPosition second) =>
        SurrogateInput.ComparePosition(first, second);

    [Export("offsetFromPosition:toPosition:")]
    public nint GetOffsetFromPosition(UITextPosition fromPosition, UITextPosition toPosition) =>
        SurrogateInput.GetOffsetFromPosition(fromPosition, toPosition);

    [Export("positionWithinRange:farthestInDirection:")]
    public UITextPosition? GetPositionWithinRange(UITextRange range, UITextLayoutDirection direction) =>
        SurrogateInput.GetPositionWithinRange(range, direction);

    [Export("characterRangeByExtendingPosition:inDirection:")]
    public UITextRange? GetCharacterRange(UITextPosition byExtendingPosition, UITextLayoutDirection direction) =>
        SurrogateInput.GetCharacterRange(byExtendingPosition, direction);

    [Export("baseWritingDirectionForPosition:inDirection:")]
    public NSWritingDirection GetBaseWritingDirection(UITextPosition forPosition, UITextStorageDirection direction) =>
        SurrogateInput.GetBaseWritingDirection(forPosition, direction);

    [Export("setBaseWritingDirection:forRange:")]
    public void SetBaseWritingDirectionforRange(NSWritingDirection writingDirection, UITextRange range) =>
        SurrogateInput.SetBaseWritingDirectionforRange(writingDirection, range);

    [Export("firstRectForRange:")]
    public CGRect GetFirstRectForRange(UITextRange range) => SurrogateInput.GetFirstRectForRange(range);

    [Export("caretRectForPosition:")]
    public CGRect GetCaretRectForPosition(UITextPosition? position) => SurrogateInput.GetCaretRectForPosition(position);

    [Export("closestPositionToPoint:")]
    public UITextPosition GetClosestPositionToPoint(CGPoint point) => SurrogateInput.GetClosestPositionToPoint(point);

    [Export("closestPositionToPoint:withinRange:")]
    public UITextPosition GetClosestPositionToPoint(CGPoint point, UITextRange withinRange) =>
        SurrogateInput.GetClosestPositionToPoint(point, withinRange);

    [Export("selectionRectsForRange:")]
    public UITextSelectionRect[] GetSelectionRects(UITextRange range) => SurrogateInput.GetSelectionRects(range);

    [Export("characterRangeAtPoint:")]
    public UITextRange GetCharacterRangeAtPoint(CGPoint point) => SurrogateInput.GetCharacterRangeAtPoint(point);

    [Export("insertText:")]
    public void InsertText(string text) => SurrogateInput.InsertText(text);

    [Export("deleteBackward")]
    public void DeleteBackward() => SurrogateInput.DeleteBackward();

    [Export("hasText")]
    public bool HasText => SurrogateInput.HasText;

    public override void Cut(NSObject? sender) => Surrogate.Cut(sender);

    public override void Copy(NSObject? sender) => Surrogate.Copy(sender);

    public override void Paste(NSObject? sender) => Surrogate.Paste(sender);

    public override void SelectAll(NSObject? sender) => Surrogate.SelectAll(sender);
}

/// <summary>FlutterInactiveTextInput: the UITextInput of a field without input focus; it only reads its text.</summary>
[Register("PlumixInactiveTextInput")]
internal sealed class InactiveTextInput : UIView, IUITextInput
{
    private NSObject? _inputDelegate;

    public string Text { get; set; } = string.Empty;

    [Export("hasText")]
    public bool HasText => Text.Length > 0;

    [Export("textInRange:")]
    public string? TextInRange(UITextRange range)
    {
        if (range is not FlutterTextRange flutterRange || flutterRange.Range.Location == NSRange.NotFound)
        {
            return null;
        }

        int location = Math.Min(flutterRange.Location, Text.Length);
        return Text.Substring(location, Math.Min(flutterRange.Length, Text.Length - location));
    }

    [Export("replaceRange:withText:")]
    public void ReplaceText(UITextRange range, string text)
    {
    }

    [Export("selectedTextRange")]
    public UITextRange? SelectedTextRange { get; set; }

    [Export("markedTextRange")]
    public UITextRange? MarkedTextRange => null;

    [Export("markedTextStyle")]
    public NSDictionary? MarkedTextStyle { get; set; }

    [Export("setMarkedText:selectedRange:")]
    public void SetMarkedText(string? markedText, NSRange selectedRange)
    {
    }

    [Export("unmarkText")]
    public void UnmarkText()
    {
    }

    [Export("beginningOfDocument")]
    public UITextPosition BeginningOfDocument { get; } = new FlutterTextPosition(0);

    [Export("endOfDocument")]
    public UITextPosition EndOfDocument => new FlutterTextPosition(Text.Length);

    [Export("inputDelegate", ArgumentSemantic.Weak)]
    public NSObject? WeakInputDelegate
    {
        get => _inputDelegate;
        set => _inputDelegate = value;
    }

    [Export("tokenizer")]
    public NSObject WeakTokenizer => new UITextInputStringTokenizer(this);

    [Export("textRangeFromPosition:toPosition:")]
    public UITextRange GetTextRange(UITextPosition fromPosition, UITextPosition toPosition)
    {
        int from = (fromPosition as FlutterTextPosition)?.Index ?? 0;
        int to = (toPosition as FlutterTextPosition)?.Index ?? 0;
        return new FlutterTextRange(from, Math.Max(0, to - from));
    }

    [Export("positionFromPosition:offset:")]
    public UITextPosition? GetPosition(UITextPosition fromPosition, nint offset) => null;

    [Export("positionFromPosition:inDirection:offset:")]
    public UITextPosition? GetPosition(UITextPosition fromPosition, UITextLayoutDirection inDirection, nint offset) =>
        null;

    [Export("comparePosition:toPosition:")]
    public NSComparisonResult ComparePosition(UITextPosition first, UITextPosition second) =>
        NSComparisonResult.Same;

    [Export("offsetFromPosition:toPosition:")]
    public nint GetOffsetFromPosition(UITextPosition fromPosition, UITextPosition toPosition) => 0;

    [Export("positionWithinRange:farthestInDirection:")]
    public UITextPosition? GetPositionWithinRange(UITextRange range, UITextLayoutDirection direction) => null;

    [Export("characterRangeByExtendingPosition:inDirection:")]
    public UITextRange? GetCharacterRange(UITextPosition byExtendingPosition, UITextLayoutDirection direction) => null;

    [Export("baseWritingDirectionForPosition:inDirection:")]
    public NSWritingDirection GetBaseWritingDirection(UITextPosition forPosition, UITextStorageDirection direction) =>
        NSWritingDirection.Natural;

    [Export("setBaseWritingDirection:forRange:")]
    public void SetBaseWritingDirectionforRange(NSWritingDirection writingDirection, UITextRange range)
    {
    }

    [Export("firstRectForRange:")]
    public CGRect GetFirstRectForRange(UITextRange range) => CGRect.Empty;

    [Export("caretRectForPosition:")]
    public CGRect GetCaretRectForPosition(UITextPosition? position) => CGRect.Empty;

    [Export("closestPositionToPoint:")]
    public UITextPosition? GetClosestPositionToPoint(CGPoint point) => null;

    [Export("closestPositionToPoint:withinRange:")]
    public UITextPosition? GetClosestPositionToPoint(CGPoint point, UITextRange withinRange) => null;

    [Export("selectionRectsForRange:")]
    public UITextSelectionRect[] GetSelectionRects(UITextRange range) => [];

    [Export("characterRangeAtPoint:")]
    public UITextRange? GetCharacterRangeAtPoint(CGPoint point) => null;

    [Export("insertText:")]
    public void InsertText(string text)
    {
    }

    [Export("deleteBackward")]
    public void DeleteBackward()
    {
    }
}
