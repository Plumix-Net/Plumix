using System.Globalization;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/editable_text.dart

namespace Plumix.Widgets;

/// Dart's `_ApplyTextBoundary`.
internal delegate TextPosition ApplyTextBoundary(TextPosition extent, bool forward, TextBoundary textBoundary);

public sealed partial class EditableText
{
    public sealed partial class EditableTextState
    {
        private Dictionary<Type, FlutterAction>? _actions;
        private CallbackAction<TransposeCharactersIntent>? _transposeCharactersAction;
        private CallbackAction<ReplaceTextIntent>? _replaceTextAction;
        private CallbackAction<UpdateSelectionIntent>? _updateSelectionAction;
        private UpdateTextSelectionVerticallyAction<DirectionalCaretMovementIntent>? _verticalSelectionUpdateAction;
        private bool _hadFocusOnTapDown;

        private UpdateTextSelectionVerticallyAction<DirectionalCaretMovementIntent> VerticalSelectionUpdateAction =>
            _verticalSelectionUpdateAction ??=
                new UpdateTextSelectionVerticallyAction<DirectionalCaretMovementIntent>(this);

        // --------------------------------------------------------- text boundaries

        private TextPosition MoveBeyondTextBoundary(TextPosition extent, bool forward, TextBoundary textBoundary)
        {
            AssertNonNegative(extent);
            int newOffset = forward
                ? textBoundary.GetTrailingTextBoundaryAt(extent.Offset) ?? EditingValue.Text.Length
                // if x is a boundary defined by `textBoundary`, most textBoundaries (except
                // LineBreaker) guarantees `x == textBoundary.getLeadingTextBoundaryAt(x)`.
                // Use x - 1 here to make sure we don't get stuck at the fixed point x.
                : textBoundary.GetLeadingTextBoundaryAt(extent.Offset - 1) ?? 0;
            return new TextPosition(newOffset);
        }

        // Returns the TextPosition that is a text boundary (as defined by `textBoundary`) and is in
        // the given direction from the given `extent`.
        private TextPosition MoveToTextBoundary(TextPosition extent, bool forward, TextBoundary textBoundary)
        {
            AssertNonNegative(extent);
            int caretOffset;
            switch (extent.Affinity)
            {
                case TextAffinity.Upstream:
                    if (extent.Offset < 1 && !forward)
                    {
                        return new TextPosition(0);
                    }

                    // When the text affinity is upstream, the caret is associated with the grapheme
                    // before the code unit at `extent.offset`.
                    caretOffset = Math.Max(0, extent.Offset - 1);
                    break;
                default:
                    caretOffset = extent.Offset;
                    break;
            }

            // The line boundary range does not include some control characters (most notably, Line
            // Feed), in which case there's `x ∉ getTextBoundaryAt(x)`. In case `caretOffset` points
            // to one such control character, we define that these control characters themselves
            // are still part of the previous line, but also exclude them from the line boundary
            // range since they're non-printing. IOW, no additional processing needed since the LF
            // character is already not in the line boundary range.
            return forward
                ? new TextPosition(
                    textBoundary.GetTrailingTextBoundaryAt(caretOffset) ?? EditingValue.Text.Length,
                    TextAffinity.Upstream)
                : new TextPosition(textBoundary.GetLeadingTextBoundaryAt(caretOffset) ?? 0);
        }

        private static void AssertNonNegative(TextPosition extent)
        {
            if (Constants.KDebugMode && extent.Offset < 0)
            {
                throw new AssertionError("extent.offset >= 0");
            }
        }

        // --------------------------------------------------------- Text Editing Actions

        // The Handling of the default text editing shortcuts. This set of actions is intended to be
        // overridable by users of the EditableText, so the actions are created lazily
        // (`late final` in Dart) because `_makeOverridable` needs the state's context.

        private TextBoundary CharacterBoundaryFor() =>
            Widget.ObscureText ? new CodePointBoundary(EditingValue.Text) : new CharacterBoundary(EditingValue.Text);

        private TextBoundary NextWordBoundary() =>
            Widget.ObscureText ? DocumentBoundaryFor() : RenderEditableObject.WordBoundaries.MoveByWordBoundary;

        private TextBoundary Linebreak() =>
            Widget.ObscureText ? DocumentBoundaryFor() : new LineBoundary(RenderEditableObject);

        private TextBoundary ParagraphBoundaryFor() => new ParagraphBoundary(EditingValue.Text);

        private TextBoundary DocumentBoundaryFor() => new DocumentBoundary(EditingValue.Text);

        private FlutterAction<T> MakeOverridable<T>(FlutterAction<T> defaultAction)
            where T : Intent =>
            FlutterAction.Overridable(context: Context, defaultAction: defaultAction);

        /// Transpose the characters on either side of the current selection.
        ///
        /// Dart's `_transposeCharacters`. It walks the grapheme clusters the way Dart's
        /// `CharacterRange` does.
        private object? TransposeCharacters(TransposeCharactersIntent intent)
        {
            string text = EditingValue.Text;
            TextSelection selection = EditingValue.Selection;
            int[] graphemeStarts = StringInfo.ParseCombiningCharacters(text);
            if (graphemeStarts.Length <= 1 || !selection.IsCollapsed || selection.BaseOffset == 0)
            {
                return null;
            }

            int caret = selection.BaseOffset;
            bool atEnd = caret == text.Length;
            // The index of the grapheme that starts at or contains the caret.
            int caretGrapheme = Array.FindLastIndex(graphemeStarts, start => start <= caret);
            if (caretGrapheme < 0)
            {
                return null;
            }

            int first;
            if (atEnd)
            {
                // Transpose the last two characters.
                first = graphemeStarts.Length - 2;
            }
            else
            {
                // Transpose the characters on either side of the caret.
                int currentGrapheme = graphemeStarts[caretGrapheme] == caret ? caretGrapheme : caretGrapheme + 1;
                first = currentGrapheme - 1;
                if (first < 0 || first + 1 >= graphemeStarts.Length)
                {
                    return null;
                }
            }

            int firstStart = graphemeStarts[first];
            int secondStart = graphemeStarts[first + 1];
            int secondEnd = first + 2 < graphemeStarts.Length ? graphemeStarts[first + 2] : text.Length;
            string firstCharacter = text[firstStart..secondStart];
            string secondCharacter = text[secondStart..secondEnd];
            UserUpdateTextEditingValue(
                new TextEditingValue(
                    text: text[..firstStart] + secondCharacter + firstCharacter + text[secondEnd..],
                    selection: TextSelection.Collapsed(secondEnd)),
                SelectionChangedCause.Keyboard);
            return null;
        }

        private CallbackAction<TransposeCharactersIntent> TransposeCharactersActionFor =>
            _transposeCharactersAction ??= new CallbackAction<TransposeCharactersIntent>(TransposeCharacters);

        private object? ReplaceTextCore(ReplaceTextIntent intent)
        {
            TextEditingValue oldValue = EditingValue;
            TextEditingValue newValue = intent.CurrentTextEditingValue.Replaced(
                intent.ReplacementRange,
                intent.ReplacementText);
            UserUpdateTextEditingValue(newValue, intent.Cause);

            // If there's no change in text and selection (e.g. when selecting and pasting identical
            // text), the widget won't be rebuilt on value update. Handle this by calling
            // _didChangeTextEditingValue() so caret and scroll updates can happen.
            if (newValue.Equals(oldValue))
            {
                DidChangeTextEditingValue();
            }

            return null;
        }

        private CallbackAction<ReplaceTextIntent> ReplaceTextAction =>
            _replaceTextAction ??= new CallbackAction<ReplaceTextIntent>(ReplaceTextCore);

        // Scrolls either to the beginning or end of the document depending on the intent's `forward`
        // parameter.
        private object? ScrollToDocumentBoundary(ScrollToDocumentBoundaryIntent intent)
        {
            if (intent.Forward)
            {
                BringIntoView(new TextPosition(EditingValue.Text.Length));
            }
            else
            {
                BringIntoView(new TextPosition(0));
            }

            return null;
        }

        /// Handles [ScrollIntent] by scrolling the [Scrollable] inside of [EditableText].
        private object? Scroll(ScrollIntent intent)
        {
            if (intent.Type != ScrollIncrementType.Page)
            {
                return null;
            }

            ScrollPosition position = EffectiveScrollController.Position;
            if (Widget.MaxLines == 1)
            {
                EffectiveScrollController.JumpTo(position.MaxScrollExtent);
                return null;
            }

            // If the field isn't scrollable, do nothing. For example, when the lines of text is less
            // than maxLines, the field has nothing to scroll.
            if (position.MaxScrollExtent == 0.0 && position.MinScrollExtent == 0.0)
            {
                return null;
            }

            ScrollableState? state = _scrollableKey.CurrentState;
            double increment = ScrollAction.GetDirectionalIncrement(state!, intent);
            double destination = ClampDouble(
                position.Pixels + increment,
                position.MinScrollExtent,
                position.MaxScrollExtent);
            if (destination == position.Pixels)
            {
                return null;
            }

            EffectiveScrollController.JumpTo(destination);
            return null;
        }

        private object? UpdateSelection(UpdateSelectionIntent intent)
        {
            int length = intent.CurrentTextEditingValue.Text.Length;
            if (Constants.KDebugMode && (intent.NewSelection.Start > length || intent.NewSelection.End > length))
            {
                throw new AssertionError(
                    $"invalid selection: {intent.NewSelection}: it must not exceed the current text length {length}");
            }

            BringIntoView(intent.NewSelection.Extent);
            UserUpdateTextEditingValue(
                intent.CurrentTextEditingValue.CopyWith(selection: intent.NewSelection),
                intent.Cause);
            return null;
        }

        private CallbackAction<UpdateSelectionIntent> UpdateSelectionAction =>
            _updateSelectionAction ??= new CallbackAction<UpdateSelectionIntent>(UpdateSelection);

        private object? HideToolbarIfVisible(DismissIntent intent)
        {
            if (_selectionOverlay?.ToolbarIsVisible ?? false)
            {
                HideToolbar(hideHandles: false);
                return null;
            }

            return Actions.Invoke(Context, intent);
        }

        /// The default behavior used if <see cref="EditableText.OnTapOutside"/> is null.
        ///
        /// The `event` argument is the [PointerDownEvent] that caused the notification.
        private void DefaultOnTapOutside(BuildContext context, PointerDownEvent @event)
        {
            _ = Actions.Invoke(context, new EditableTextTapOutsideIntent(_focusNode!, @event));
        }

        private void OnTapOutside(BuildContext context, PointerDownEvent @event)
        {
            _hadFocusOnTapDown = true;

            if (Widget.OnTapOutside is { } onTapOutside)
            {
                onTapOutside(@event);
            }
            else
            {
                DefaultOnTapOutside(context, @event);
            }
        }

        private void OnTapUpOutside(BuildContext context, PointerUpEvent @event)
        {
            if (!_hadFocusOnTapDown)
            {
                return;
            }

            // Reset to false so that subsequent events doesn't trigger the callback based on old
            // information.
            _hadFocusOnTapDown = false;

            if (Widget.OnTapUpOutside is { } onTapUpOutside)
            {
                onTapUpOutside(@event);
            }
            else
            {
                DefaultOnTapUpOutside(context, @event);
            }
        }

        /// The default behavior used if <see cref="EditableText.OnTapUpOutside"/> is null.
        private void DefaultOnTapUpOutside(BuildContext context, PointerUpEvent @event)
        {
            _ = Actions.Invoke(context, new EditableTextTapUpOutsideIntent(_focusNode!, @event));
        }

        private Dictionary<Type, FlutterAction> ActionsMap => _actions ??= new Dictionary<Type, FlutterAction>
        {
            [typeof(DoNothingAndStopPropagationTextIntent)] = new DoNothingAction(consumesKey: false),
            [typeof(ReplaceTextIntent)] = ReplaceTextAction,
            [typeof(UpdateSelectionIntent)] = UpdateSelectionAction,
            [typeof(DirectionalFocusIntent)] = DirectionalFocusAction.ForTextField(),
            [typeof(DismissIntent)] = new CallbackAction<DismissIntent>(HideToolbarIfVisible),

            // Delete
            [typeof(DeleteCharacterIntent)] = MakeOverridable(
                new DeleteTextAction<DeleteCharacterIntent>(this, CharacterBoundaryFor, MoveBeyondTextBoundary)),
            [typeof(DeleteToNextWordBoundaryIntent)] = MakeOverridable(
                new DeleteTextAction<DeleteToNextWordBoundaryIntent>(this, NextWordBoundary, MoveBeyondTextBoundary)),
            [typeof(DeleteToLineBreakIntent)] = MakeOverridable(
                new DeleteTextAction<DeleteToLineBreakIntent>(this, Linebreak, MoveToTextBoundary)),

            // Extend/Move Selection
            [typeof(ExtendSelectionByCharacterIntent)] = MakeOverridable(
                new UpdateTextSelectionAction<ExtendSelectionByCharacterIntent>(
                    this,
                    CharacterBoundaryFor,
                    MoveBeyondTextBoundary,
                    ignoreNonCollapsedSelection: false)),
            [typeof(ExtendSelectionToNextWordBoundaryIntent)] = MakeOverridable(
                new UpdateTextSelectionAction<ExtendSelectionToNextWordBoundaryIntent>(
                    this,
                    NextWordBoundary,
                    MoveBeyondTextBoundary,
                    ignoreNonCollapsedSelection: true)),
            [typeof(ExtendSelectionToNextParagraphBoundaryIntent)] = MakeOverridable(
                new UpdateTextSelectionAction<ExtendSelectionToNextParagraphBoundaryIntent>(
                    this,
                    ParagraphBoundaryFor,
                    MoveBeyondTextBoundary,
                    ignoreNonCollapsedSelection: true)),
            [typeof(ExtendSelectionToLineBreakIntent)] = MakeOverridable(
                new UpdateTextSelectionAction<ExtendSelectionToLineBreakIntent>(
                    this,
                    Linebreak,
                    MoveToTextBoundary,
                    ignoreNonCollapsedSelection: true)),
            [typeof(ExtendSelectionVerticallyToAdjacentLineIntent)] =
                MakeOverridable<DirectionalCaretMovementIntent>(VerticalSelectionUpdateAction),
            [typeof(ExtendSelectionVerticallyToAdjacentPageIntent)] =
                MakeOverridable<DirectionalCaretMovementIntent>(VerticalSelectionUpdateAction),
            [typeof(ExtendSelectionToNextParagraphBoundaryOrCaretLocationIntent)] = MakeOverridable(
                new UpdateTextSelectionAction<ExtendSelectionToNextParagraphBoundaryOrCaretLocationIntent>(
                    this,
                    ParagraphBoundaryFor,
                    MoveBeyondTextBoundary,
                    ignoreNonCollapsedSelection: true)),
            [typeof(ExtendSelectionToDocumentBoundaryIntent)] = MakeOverridable(
                new UpdateTextSelectionAction<ExtendSelectionToDocumentBoundaryIntent>(
                    this,
                    DocumentBoundaryFor,
                    MoveBeyondTextBoundary,
                    ignoreNonCollapsedSelection: true)),
            [typeof(ExtendSelectionToNextWordBoundaryOrCaretLocationIntent)] = MakeOverridable(
                new UpdateTextSelectionAction<ExtendSelectionToNextWordBoundaryOrCaretLocationIntent>(
                    this,
                    NextWordBoundary,
                    MoveBeyondTextBoundary,
                    ignoreNonCollapsedSelection: true)),
            [typeof(ScrollToDocumentBoundaryIntent)] = MakeOverridable(
                new WebComposingDisablingCallbackAction<ScrollToDocumentBoundaryIntent>(
                    this,
                    ScrollToDocumentBoundary)),
            [typeof(ScrollIntent)] = new CallbackAction<ScrollIntent>(Scroll),

            // Expand Selection
            [typeof(ExpandSelectionToLineBreakIntent)] = MakeOverridable(
                new UpdateTextSelectionAction<ExpandSelectionToLineBreakIntent>(
                    this,
                    Linebreak,
                    MoveToTextBoundary,
                    ignoreNonCollapsedSelection: true,
                    isExpand: true)),
            [typeof(ExpandSelectionToDocumentBoundaryIntent)] = MakeOverridable(
                new UpdateTextSelectionAction<ExpandSelectionToDocumentBoundaryIntent>(
                    this,
                    DocumentBoundaryFor,
                    MoveToTextBoundary,
                    ignoreNonCollapsedSelection: true,
                    isExpand: true,
                    extentAtIndex: true)),

            // Copy Paste
            [typeof(SelectAllTextIntent)] = MakeOverridable(new SelectAllAction(this)),
            [typeof(CopySelectionTextIntent)] = MakeOverridable(new CopySelectionAction(this)),
            [typeof(PasteTextIntent)] = MakeOverridable(new PasteSelectionAction(this)),

            [typeof(TransposeCharactersIntent)] = MakeOverridable(TransposeCharactersActionFor),
            [typeof(EditableTextTapOutsideIntent)] = MakeOverridable(new EditableTextTapOutsideAction()),
            [typeof(EditableTextTapUpOutsideIntent)] = MakeOverridable(new EditableTextTapUpOutsideAction()),
        };

        // The members the action classes below read; Dart reaches the state's private members from
        // the same library.
        internal TextEditingValue ValueForActions => EditingValue;

        internal TextSelectionOverlay? SelectionOverlayForActions => _selectionOverlay;

        internal TextBoundary CharacterBoundaryForActions() => CharacterBoundaryFor();

        internal void PasteTextWithReportingForActions(SelectionChangedCause cause) =>
            Scheduler.RunAsync(() => PasteTextWithReportingAsync(cause));

        /// Dart's `_textEditingValueforTextLayoutMetrics`: the value the editable was last laid out
        /// with.
        internal TextEditingValue TextEditingValueForTextLayoutMetrics
        {
            get
            {
                Widget? widget = _editableRenderKey.CurrentContext?.Widget;
                if (widget is not EditableRenderObjectWidget editableWidget)
                {
                    throw new InvalidOperationException("_Editable must be mounted.");
                }

                return editableWidget.Value;
            }
        }
    }
}

/// Dart's `_CodePointBoundary`: a text boundary that uses code points as logical boundaries. A code
/// point represents a single character. This may be smaller than what is represented by a user-
/// perceived character, or grapheme. For example, a single grapheme (in this case a Unicode
/// extended grapheme cluster) like "👨‍👩‍👦" consists of five code points: the man emoji, a zero
/// width joiner, the woman emoji, another zero width joiner, and the boy emoji. The [String] has a
/// length of eight because each emoji consists of two code units. Code units are the smallest
/// unit of a [String] in Dart.
internal sealed class CodePointBoundary(string text) : TextBoundary
{
    private readonly string _text = text;

    // Returns true if the given position falls in the center of a surrogate pair.
    private bool BreaksSurrogatePair(int position)
    {
        if (Constants.KDebugMode && !(position > 0 && position < _text.Length && _text.Length > 1))
        {
            throw new AssertionError("position > 0 && position < _text.length && _text.length > 1");
        }

        return TextPainter.IsHighSurrogate(_text[position - 1]) && TextPainter.IsLowSurrogate(_text[position]);
    }

    public override int? GetLeadingTextBoundaryAt(int position)
    {
        if (_text.Length == 0 || position < 0)
        {
            return null;
        }

        if (position == 0)
        {
            return 0;
        }

        if (position >= _text.Length)
        {
            return _text.Length;
        }

        if (_text.Length <= 1)
        {
            return position;
        }

        return BreaksSurrogatePair(position) ? position - 1 : position;
    }

    public override int? GetTrailingTextBoundaryAt(int position)
    {
        if (_text.Length == 0 || position >= _text.Length)
        {
            return null;
        }

        if (position < 0)
        {
            return 0;
        }

        if (position == _text.Length - 1)
        {
            return _text.Length;
        }

        if (_text.Length <= 1)
        {
            return position;
        }

        return BreaksSurrogatePair(position + 1) ? position + 2 : position + 1;
    }
}

/// Dart's `_DeleteTextAction`: deletes the text a directional intent spans.
internal sealed class DeleteTextAction<T> : ContextAction<T> where T : DirectionalTextEditingIntent
{
    private readonly EditableText.EditableTextState _state;
    private readonly Func<TextBoundary> _getTextBoundary;
    private readonly ApplyTextBoundary _applyTextBoundary;

    public DeleteTextAction(
        EditableText.EditableTextState state,
        Func<TextBoundary> getTextBoundary,
        ApplyTextBoundary applyTextBoundary)
    {
        _state = state;
        _getTextBoundary = getTextBoundary;
        _applyTextBoundary = applyTextBoundary;
    }

    private void HideToolbarIfTextChanged(ReplaceTextIntent intent)
    {
        if (_state.SelectionOverlayForActions is not { ToolbarIsVisible: true })
        {
            return;
        }

        TextEditingValue oldValue = intent.CurrentTextEditingValue;
        TextEditingValue newValue = intent.CurrentTextEditingValue.Replaced(
            intent.ReplacementRange,
            intent.ReplacementText);
        if (!string.Equals(oldValue.Text, newValue.Text, StringComparison.Ordinal))
        {
            // Hide the toolbar if the text was changed, but only hide the toolbar overlay; the
            // selection handle's visibility will be handled by `_handleSelectionChanged`.
            _state.HideToolbar(hideHandles: false);
        }
    }

    public override object? Invoke(T intent, BuildContext? context)
    {
        TextEditingValue value = _state.ValueForActions;
        TextSelection selection = value.Selection;
        if (!selection.IsValid)
        {
            return null;
        }

        // Expands the selection to ensure the range covers full graphemes.
        TextBoundary atomicBoundary = _state.CharacterBoundaryForActions();
        if (!selection.IsCollapsed)
        {
            // Expands the selection to ensure the range covers full graphemes.
            var range = new TextRange(
                atomicBoundary.GetLeadingTextBoundaryAt(selection.Start) ?? value.Text.Length,
                atomicBoundary.GetTrailingTextBoundaryAt(selection.End - 1) ?? 0);
            var replaceTextIntent = new ReplaceTextIntent(value, string.Empty, range, SelectionChangedCause.Keyboard);
            HideToolbarIfTextChanged(replaceTextIntent);
            return Actions.Invoke(context!, replaceTextIntent);
        }

        int target = _applyTextBoundary(selection.Base, intent.Forward, _getTextBoundary()).Offset;

        var rangeToDelete = new TextSelection(
            BaseOffset: intent.Forward
                ? atomicBoundary.GetLeadingTextBoundaryAt(selection.BaseOffset) ?? value.Text.Length
                : atomicBoundary.GetTrailingTextBoundaryAt(selection.BaseOffset - 1) ?? 0,
            ExtentOffset: target);
        var deleteIntent = new ReplaceTextIntent(
            value,
            string.Empty,
            rangeToDelete.AsTextRange(),
            SelectionChangedCause.Keyboard);
        HideToolbarIfTextChanged(deleteIntent);
        return Actions.Invoke(context!, deleteIntent);
    }

    public override bool IsActionEnabled =>
        !_state.Widget.ReadOnly && _state.ValueForActions.Selection.IsValid;
}

/// Dart's `_UpdateTextSelectionAction`: moves, extends or expands the selection to a text boundary.
internal sealed class UpdateTextSelectionAction<T> : ContextAction<T> where T : DirectionalCaretMovementIntent
{
    private const int NewlineCodeUnit = 10;

    private readonly EditableText.EditableTextState _state;
    private readonly bool _ignoreNonCollapsedSelection;
    private readonly bool _isExpand;
    private readonly bool _extentAtIndex;
    private readonly Func<TextBoundary> _getTextBoundary;
    private readonly ApplyTextBoundary _applyTextBoundary;

    public UpdateTextSelectionAction(
        EditableText.EditableTextState state,
        Func<TextBoundary> getTextBoundary,
        ApplyTextBoundary applyTextBoundary,
        bool ignoreNonCollapsedSelection,
        bool isExpand = false,
        bool extentAtIndex = false)
    {
        _state = state;
        _getTextBoundary = getTextBoundary;
        _applyTextBoundary = applyTextBoundary;
        _ignoreNonCollapsedSelection = ignoreNonCollapsedSelection;
        _isExpand = isExpand;
        _extentAtIndex = extentAtIndex;
    }

    // Returns true iff the given position is at a wordwrap boundary in the upstream position.
    private bool IsAtWordwrapUpstream(TextPosition position)
    {
        var end = new TextPosition(
            _state.RenderEditableObject.GetLineAtOffset(position).End,
            TextAffinity.Upstream);
        string text = _state.ValueForActions.Text;
        return end.Equals(position) && end.Offset != text.Length && text[position.Offset] != NewlineCodeUnit;
    }

    // Returns true if the given position at a wordwrap boundary in the downstream position.
    private bool IsAtWordwrapDownstream(TextPosition position)
    {
        var start = new TextPosition(_state.RenderEditableObject.GetLineAtOffset(position).Start);
        string text = _state.ValueForActions.Text;
        return start.Equals(position) && start.Offset != 0 && text[position.Offset - 1] != NewlineCodeUnit;
    }

    public override object? Invoke(T intent, BuildContext? context)
    {
        TextEditingValue value = _state.ValueForActions;
        TextSelection selection = value.Selection;
        if (Constants.KDebugMode && !selection.IsValid)
        {
            throw new AssertionError("selection.isValid");
        }

        bool collapseSelection = intent.CollapseSelection || !_state.Widget.SelectionEnabled;
        if (!selection.IsCollapsed && !_ignoreNonCollapsedSelection && collapseSelection)
        {
            return Actions.Invoke(
                context!,
                new UpdateSelectionIntent(
                    value,
                    TextSelection.Collapsed(intent.Forward ? selection.End : selection.Start),
                    SelectionChangedCause.Keyboard));
        }

        TextPosition extent = selection.Extent;
        // If continuesAtWrap is true extent and is at the relevant wordwrap, then move it just
        // past the wordwrap.
        if (intent.ContinuesAtWrap)
        {
            if (intent.Forward && IsAtWordwrapUpstream(extent))
            {
                extent = new TextPosition(extent.Offset);
            }
            else if (!intent.Forward && IsAtWordwrapDownstream(extent))
            {
                extent = new TextPosition(extent.Offset, TextAffinity.Upstream);
            }
        }

        bool shouldTargetBase = _isExpand
                                && (intent.Forward
                                    ? selection.BaseOffset > selection.ExtentOffset
                                    : selection.BaseOffset < selection.ExtentOffset);
        TextPosition newExtent = _applyTextBoundary(
            shouldTargetBase ? selection.Base : extent,
            intent.Forward,
            _getTextBoundary());
        TextSelection newSelection = collapseSelection || (!_isExpand && newExtent.Offset == selection.BaseOffset)
            ? TextSelection.FromPosition(newExtent)
            : _isExpand
                ? selection.ExpandTo(newExtent, _extentAtIndex || selection.IsCollapsed)
                : selection.ExtendTo(newExtent);

        bool shouldCollapseToBase = intent.CollapseAtReversal
                                    && (selection.BaseOffset - selection.ExtentOffset)
                                    * (selection.BaseOffset - newSelection.ExtentOffset) < 0;
        TextSelection newRange = shouldCollapseToBase ? TextSelection.FromPosition(selection.Base) : newSelection;
        return Actions.Invoke(
            context!,
            new UpdateSelectionIntent(value, newRange, SelectionChangedCause.Keyboard));
    }

    public override bool IsActionEnabled
    {
        get
        {
            if (PlatformDefaults.IsWeb
                && _state.Widget.SelectionEnabled
                && _state.ValueForActions.IsComposingRangeValid)
            {
                return false;
            }

            return _state.ValueForActions.Selection.IsValid;
        }
    }
}

/// Dart's `_UpdateTextSelectionVerticallyAction`: moves the caret a line or a page up or down,
/// keeping its horizontal position across consecutive moves.
internal sealed class UpdateTextSelectionVerticallyAction<T> : ContextAction<T>
    where T : DirectionalCaretMovementIntent
{
    private readonly EditableText.EditableTextState _state;

    private VerticalCaretMovementRun? _verticalMovementRun;
    private TextSelection? _runSelection;

    public UpdateTextSelectionVerticallyAction(EditableText.EditableTextState state)
    {
        _state = state;
    }

    public void StopCurrentVerticalRunIfSelectionChanges()
    {
        TextSelection? runSelection = _runSelection;
        if (runSelection is null)
        {
            if (Constants.KDebugMode && _verticalMovementRun is not null)
            {
                throw new AssertionError("_verticalMovementRun == null");
            }

            return;
        }

        _runSelection = _state.ValueForActions.Selection;
        TextSelection currentSelection = _state.Widget.Controller.Selection;
        bool continueCurrentRun = currentSelection.IsValid
                                  && currentSelection.IsCollapsed
                                  && currentSelection.BaseOffset == runSelection.Value.BaseOffset
                                  && currentSelection.ExtentOffset == runSelection.Value.ExtentOffset;
        if (!continueCurrentRun)
        {
            _verticalMovementRun = null;
            _runSelection = null;
        }
    }

    public override object? Invoke(T intent, BuildContext? context)
    {
        if (Constants.KDebugMode && !_state.ValueForActions.Selection.IsValid)
        {
            throw new AssertionError("state._value.selection.isValid");
        }

        bool collapseSelection = intent.CollapseSelection || !_state.Widget.SelectionEnabled;
        TextEditingValue value = _state.TextEditingValueForTextLayoutMetrics;
        if (!value.Selection.IsValid)
        {
            return null;
        }

        if (_verticalMovementRun is { IsValid: false })
        {
            _verticalMovementRun = null;
            _runSelection = null;
        }

        RenderEditable renderEditable = _state.RenderEditableObject;
        VerticalCaretMovementRun currentRun = _verticalMovementRun
                                              ?? renderEditable.StartVerticalCaretMovement(
                                                  renderEditable.Selection!.Value.Extent);

        bool shouldMove = intent is ExtendSelectionVerticallyToAdjacentPageIntent
            ? currentRun.MoveByOffset((intent.Forward ? 1.0 : -1.0) * renderEditable.Size.Height)
            : intent.Forward
                ? currentRun.MoveNext()
                : currentRun.MovePrevious();
        TextPosition newExtent = shouldMove
            ? currentRun.Current
            : intent.Forward
                ? new TextPosition(value.Text.Length)
                : new TextPosition(0);
        TextSelection newSelection = collapseSelection
            ? TextSelection.FromPosition(newExtent)
            : value.Selection.ExtendTo(newExtent);

        _ = Actions.Invoke(
            context!,
            new UpdateSelectionIntent(value, newSelection, SelectionChangedCause.Keyboard));
        if (_state.ValueForActions.Selection.Equals(newSelection))
        {
            _verticalMovementRun = currentRun;
            _runSelection = newSelection;
        }

        return null;
    }

    public override bool IsActionEnabled
    {
        get
        {
            if (PlatformDefaults.IsWeb
                && _state.Widget.SelectionEnabled
                && _state.ValueForActions.IsComposingRangeValid)
            {
                return false;
            }

            return _state.ValueForActions.Selection.IsValid;
        }
    }
}

/// Dart's `_WebComposingDisablingCallbackAction`: disabled on the web while the IME composes.
internal sealed class WebComposingDisablingCallbackAction<T> : CallbackAction<T> where T : Intent
{
    private readonly EditableText.EditableTextState _state;

    public WebComposingDisablingCallbackAction(EditableText.EditableTextState state, Func<T, object?> onInvoke)
        : base(onInvoke)
    {
        _state = state;
    }

    public override bool IsActionEnabled
    {
        get
        {
            if (PlatformDefaults.IsWeb
                && _state.Widget.SelectionEnabled
                && _state.ValueForActions.IsComposingRangeValid)
            {
                return false;
            }

            return base.IsActionEnabled;
        }
    }
}

/// Dart's `_SelectAllAction`.
internal sealed class SelectAllAction(EditableText.EditableTextState state) : ContextAction<SelectAllTextIntent>
{
    public override object? Invoke(SelectAllTextIntent intent, BuildContext? context)
    {
        if (!state.Widget.SelectionEnabled)
        {
            return null;
        }

        TextEditingValue value = state.ValueForActions;
        return Actions.Invoke(
            context!,
            new UpdateSelectionIntent(value, new TextSelection(0, value.Text.Length), intent.Cause));
    }
}

/// Dart's `_CopySelectionAction`.
internal sealed class CopySelectionAction(EditableText.EditableTextState state)
    : ContextAction<CopySelectionTextIntent>
{
    public override object? Invoke(CopySelectionTextIntent intent, BuildContext? context)
    {
        TextSelection selection = state.ValueForActions.Selection;
        if (!selection.IsValid || selection.IsCollapsed)
        {
            return null;
        }

        if (!state.Widget.SelectionEnabled)
        {
            return null;
        }

        if (intent.CollapseSelection)
        {
            state.CutSelection(intent.Cause);
        }
        else
        {
            state.CopySelection(intent.Cause);
        }

        return null;
    }
}

/// Dart's `_PasteSelectionAction`.
internal sealed class PasteSelectionAction(EditableText.EditableTextState state) : ContextAction<PasteTextIntent>
{
    public override object? Invoke(PasteTextIntent intent, BuildContext? context)
    {
        if (!state.Widget.SelectionEnabled)
        {
            return null;
        }

        state.PasteTextWithReportingForActions(intent.Cause);
        return null;
    }
}

/// Dart's `_EditableTextTapOutsideAction`.
internal sealed class EditableTextTapOutsideAction : ContextAction<EditableTextTapOutsideIntent>
{
    public override object? Invoke(EditableTextTapOutsideIntent intent, BuildContext? context)
    {
        // The focus dropping behavior is only present on desktop platforms.
        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.Android:
            case TargetPlatform.IOS:
            case TargetPlatform.Fuchsia:
                // On mobile platforms, we don't unfocus on touch events unless they're in the web
                // browser, but we do unfocus for all other kinds of events.
                switch (intent.PointerDownEvent.Kind)
                {
                    case PointerDeviceKind.Touch:
                        if (PlatformDefaults.IsWeb)
                        {
                            intent.FocusNode.Unfocus();
                        }

                        break;
                    case PointerDeviceKind.Mouse:
                    case PointerDeviceKind.Stylus:
                    case PointerDeviceKind.InvertedStylus:
                    case PointerDeviceKind.Unknown:
                        intent.FocusNode.Unfocus();
                        break;
                    case PointerDeviceKind.Trackpad:
                        throw new NotImplementedException("Unexpected pointer down event for trackpad");
                }

                break;
            case TargetPlatform.Linux:
            case TargetPlatform.MacOS:
            case TargetPlatform.Windows:
                intent.FocusNode.Unfocus();
                break;
        }

        return null;
    }
}

/// Dart's `_EditableTextTapUpOutsideAction`.
internal sealed class EditableTextTapUpOutsideAction : ContextAction<EditableTextTapUpOutsideIntent>
{
    public override object? Invoke(EditableTextTapUpOutsideIntent intent, BuildContext? context)
    {
        // The default action is a no-op.
        return null;
    }
}
