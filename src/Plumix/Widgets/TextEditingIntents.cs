using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/text_editing_intents.dart

/// A [Intent] to send the event straight to the engine.
public sealed class DoNothingAndStopPropagationTextIntent : Intent
{
}

/// A base [Intent] for text editing intents that move the selection in a
/// direction along the text.
public abstract class DirectionalTextEditingIntent : Intent
{
    protected DirectionalTextEditingIntent(bool forward)
    {
        Forward = forward;
    }

    /// Whether the input field, if applicable, should perform the text editing
    /// operation from the current caret location towards the end of the document.
    public bool Forward { get; }
}

/// A base [Intent] for moving the caret, optionally collapsing the selection.
public abstract class DirectionalCaretMovementIntent : DirectionalTextEditingIntent
{
    protected DirectionalCaretMovementIntent(
        bool forward,
        bool collapseSelection,
        bool collapseAtReversal = false,
        bool continuesAtWrap = false) : base(forward)
    {
        if (collapseSelection && collapseAtReversal)
        {
            throw new ArgumentException(
                "CollapseAtReversal can only be true when collapseSelection is false.",
                nameof(collapseAtReversal));
        }

        CollapseSelection = collapseSelection;
        CollapseAtReversal = collapseAtReversal;
        ContinuesAtWrap = continuesAtWrap;
    }

    /// Whether this [Intent] should make the selection collapsed after the movement.
    public bool CollapseSelection { get; }

    /// Whether to collapse the selection when it would otherwise reverse order.
    public bool CollapseAtReversal { get; }

    /// Whether to continue to the next line when the caret reaches a soft wrap.
    public bool ContinuesAtWrap { get; }
}

/// Deletes the character before or after the caret location, based on whether
/// [DirectionalTextEditingIntent.Forward] is true.
public sealed class DeleteCharacterIntent : DirectionalTextEditingIntent
{
    public DeleteCharacterIntent(bool forward) : base(forward)
    {
    }
}

/// Deletes from the current caret location to the previous or next word
/// boundary, based on whether [DirectionalTextEditingIntent.Forward] is true.
public sealed class DeleteToNextWordBoundaryIntent : DirectionalTextEditingIntent
{
    public DeleteToNextWordBoundaryIntent(bool forward) : base(forward)
    {
    }
}

/// Deletes from the current caret location to the previous or next soft or hard
/// line break, based on whether [DirectionalTextEditingIntent.Forward] is true.
public sealed class DeleteToLineBreakIntent : DirectionalTextEditingIntent
{
    public DeleteToLineBreakIntent(bool forward) : base(forward)
    {
    }
}

/// Extends, or moves the current selection from the current
/// [TextSelection.extent] position to the closest character boundary.
public sealed class ExtendSelectionByCharacterIntent : DirectionalCaretMovementIntent
{
    public ExtendSelectionByCharacterIntent(bool forward, bool collapseSelection)
        : base(forward, collapseSelection)
    {
    }
}

/// Extends, or moves the current selection from the current
/// [TextSelection.extent] position to the closest word boundary.
public sealed class ExtendSelectionToNextWordBoundaryIntent : DirectionalCaretMovementIntent
{
    public ExtendSelectionToNextWordBoundaryIntent(
        bool forward,
        bool collapseSelection,
        bool collapseAtReversal = false)
        : base(forward, collapseSelection, collapseAtReversal)
    {
    }
}

/// Extends, or moves the current selection from the current
/// [TextSelection.extent] position to the closest word boundary, or the
/// [TextSelection.base] position if it is closer in the given direction.
public sealed class ExtendSelectionToNextWordBoundaryOrCaretLocationIntent : DirectionalCaretMovementIntent
{
    public ExtendSelectionToNextWordBoundaryOrCaretLocationIntent(bool forward)
        : base(forward, collapseSelection: false, collapseAtReversal: true)
    {
    }
}

/// Expands the current selection to the document boundary in the direction
/// given by [DirectionalTextEditingIntent.Forward].
public sealed class ExpandSelectionToDocumentBoundaryIntent : DirectionalCaretMovementIntent
{
    public ExpandSelectionToDocumentBoundaryIntent(bool forward) : base(forward, collapseSelection: false)
    {
    }
}

/// Expands the current selection to the closest line break in the direction
/// given by [DirectionalTextEditingIntent.Forward].
public sealed class ExpandSelectionToLineBreakIntent : DirectionalCaretMovementIntent
{
    public ExpandSelectionToLineBreakIntent(bool forward) : base(forward, collapseSelection: false)
    {
    }
}

/// Extends, or moves the current selection from the current
/// [TextSelection.extent] position to the closest line break.
public sealed class ExtendSelectionToLineBreakIntent : DirectionalCaretMovementIntent
{
    public ExtendSelectionToLineBreakIntent(
        bool forward,
        bool collapseSelection,
        bool collapseAtReversal = false,
        bool continuesAtWrap = false)
        : base(forward, collapseSelection, collapseAtReversal, continuesAtWrap)
    {
    }
}

/// Extends, or moves the current selection from the current
/// [TextSelection.extent] position to the closest position on the adjacent line.
public sealed class ExtendSelectionVerticallyToAdjacentLineIntent : DirectionalCaretMovementIntent
{
    public ExtendSelectionVerticallyToAdjacentLineIntent(bool forward, bool collapseSelection)
        : base(forward, collapseSelection)
    {
    }
}

/// Extends, or moves the current selection from the current
/// [TextSelection.extent] position to the closest position on the adjacent
/// page.
public sealed class ExtendSelectionVerticallyToAdjacentPageIntent : DirectionalCaretMovementIntent
{
    public ExtendSelectionVerticallyToAdjacentPageIntent(bool forward, bool collapseSelection)
        : base(forward, collapseSelection)
    {
    }
}

/// Extends, or moves the current selection from the current
/// [TextSelection.extent] position to the closest paragraph boundary.
public sealed class ExtendSelectionToNextParagraphBoundaryIntent : DirectionalCaretMovementIntent
{
    public ExtendSelectionToNextParagraphBoundaryIntent(bool forward, bool collapseSelection)
        : base(forward, collapseSelection)
    {
    }
}

/// Extends, or moves the current selection from the current
/// [TextSelection.extent] position to the closest paragraph boundary, or the
/// [TextSelection.base] position if it is closer in the given direction.
public sealed class ExtendSelectionToNextParagraphBoundaryOrCaretLocationIntent
    : DirectionalCaretMovementIntent
{
    public ExtendSelectionToNextParagraphBoundaryOrCaretLocationIntent(bool forward)
        : base(forward, collapseSelection: false)
    {
    }
}

/// Extends, or moves the current selection from the current
/// [TextSelection.extent] position to the start or the end of the document.
public sealed class ExtendSelectionToDocumentBoundaryIntent : DirectionalCaretMovementIntent
{
    public ExtendSelectionToDocumentBoundaryIntent(bool forward, bool collapseSelection)
        : base(forward, collapseSelection)
    {
    }
}

/// Scrolls to the beginning or end of the document depending on the
/// [DirectionalTextEditingIntent.Forward] property.
public sealed class ScrollToDocumentBoundaryIntent : DirectionalTextEditingIntent
{
    public ScrollToDocumentBoundaryIntent(bool forward) : base(forward)
    {
    }
}

/// Selects the entire text.
public sealed class SelectAllTextIntent : Intent
{
    public SelectAllTextIntent(SelectionChangedCause cause)
    {
        Cause = cause;
    }

    /// The [SelectionChangedCause] that triggered the intent.
    public SelectionChangedCause Cause { get; }
}

/// Copies the current text selection to the clipboard.
public sealed class CopySelectionTextIntent : Intent
{
    private CopySelectionTextIntent(SelectionChangedCause cause, bool collapseSelection)
    {
        Cause = cause;
        CollapseSelection = collapseSelection;
    }

    /// Creates an intent that copies the current selection without removing it.
    public static CopySelectionTextIntent Copy { get; } =
        new(SelectionChangedCause.Keyboard, collapseSelection: false);

    /// Creates an intent that cuts the current selection.
    public static CopySelectionTextIntent Cut(SelectionChangedCause cause) => new(cause, collapseSelection: true);

    /// The [SelectionChangedCause] that triggered the intent.
    public SelectionChangedCause Cause { get; }

    /// Whether the original text needs to be removed after being copied.
    public bool CollapseSelection { get; }
}

/// Pastes the current clipboard content into the text field.
public sealed class PasteTextIntent : Intent
{
    public PasteTextIntent(SelectionChangedCause cause)
    {
        Cause = cause;
    }

    /// The [SelectionChangedCause] that triggered the intent.
    public SelectionChangedCause Cause { get; }
}

/// An [Intent] that represents a user interaction that attempts to go back to
/// the previous editing state.
public sealed class RedoTextIntent : Intent
{
    public RedoTextIntent(SelectionChangedCause cause)
    {
        Cause = cause;
    }

    /// The [SelectionChangedCause] that triggered the intent.
    public SelectionChangedCause Cause { get; }
}

/// An [Intent] that represents a user interaction that attempts to modify the
/// current [TextEditingValue] in an input field.
public sealed class ReplaceTextIntent : Intent
{
    public ReplaceTextIntent(
        TextEditingValue currentTextEditingValue,
        string replacementText,
        TextRange replacementRange,
        SelectionChangedCause cause)
    {
        CurrentTextEditingValue = currentTextEditingValue;
        ReplacementText = replacementText;
        ReplacementRange = replacementRange;
        Cause = cause;
    }

    /// The [TextEditingValue] that this [Intent]'s action should perform on.
    public TextEditingValue CurrentTextEditingValue { get; }

    /// The text to replace the original text within the [ReplacementRange] with.
    public string ReplacementText { get; }

    /// The range of text in [CurrentTextEditingValue] that needs to be replaced.
    public TextRange ReplacementRange { get; }

    /// The cause of this [ReplaceTextIntent].
    public SelectionChangedCause Cause { get; }
}

/// An [Intent] that represents a user interaction that attempts to go back to
/// the previous editing state.
public sealed class UndoTextIntent : Intent
{
    public UndoTextIntent(SelectionChangedCause cause)
    {
        Cause = cause;
    }

    /// The [SelectionChangedCause] that triggered the intent.
    public SelectionChangedCause Cause { get; }
}

/// An [Intent] that represents a user interaction that attempts to change the
/// selection in an input field.
public sealed class UpdateSelectionIntent : Intent
{
    public UpdateSelectionIntent(
        TextEditingValue currentTextEditingValue,
        TextSelection newSelection,
        SelectionChangedCause cause)
    {
        CurrentTextEditingValue = currentTextEditingValue;
        NewSelection = newSelection;
        Cause = cause;
    }

    /// The [TextEditingValue] that this [Intent]'s action should perform on.
    public TextEditingValue CurrentTextEditingValue { get; }

    /// The new [TextSelection] the input field should adopt.
    public TextSelection NewSelection { get; }

    /// The cause of this [UpdateSelectionIntent].
    public SelectionChangedCause Cause { get; }
}

/// An [Intent] that represents a user interaction that attempts to swap the
/// characters immediately around the cursor.
public sealed class TransposeCharactersIntent : Intent
{
}

/// An [Intent] that is invoked when a pointer goes down outside of an
/// [EditableText].
public sealed class EditableTextTapOutsideIntent : Intent
{
    public EditableTextTapOutsideIntent(FocusNode focusNode, PointerDownEvent pointerDownEvent)
    {
        FocusNode = focusNode;
        PointerDownEvent = pointerDownEvent;
    }

    /// The [FocusNode] of the [EditableText] that this intent is associated with.
    public FocusNode FocusNode { get; }

    /// The pointer down event that triggered this intent.
    public PointerDownEvent PointerDownEvent { get; }
}

/// An [Intent] that is invoked when a pointer goes up outside of an
/// [EditableText].
public sealed class EditableTextTapUpOutsideIntent : Intent
{
    public EditableTextTapUpOutsideIntent(FocusNode focusNode, PointerUpEvent pointerUpEvent)
    {
        FocusNode = focusNode;
        PointerUpEvent = pointerUpEvent;
    }

    /// The [FocusNode] of the [EditableText] that this intent is associated with.
    public FocusNode FocusNode { get; }

    /// The pointer up event that triggered this intent.
    public PointerUpEvent PointerUpEvent { get; }
}
