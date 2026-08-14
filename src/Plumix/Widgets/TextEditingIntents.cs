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
    public ExtendSelectionToNextWordBoundaryIntent(bool forward, bool collapseSelection)
        : base(forward, collapseSelection)
    {
    }
}

/// Extends, or moves the current selection from the current
/// [TextSelection.extent] position to the closest word boundary, or the
/// [TextSelection.base] position if it is closer in the given direction.
public sealed class ExtendSelectionToNextWordBoundaryOrCaretLocationIntent : DirectionalTextEditingIntent
{
    public ExtendSelectionToNextWordBoundaryOrCaretLocationIntent(bool forward) : base(forward)
    {
    }
}

/// Expands the current selection to the document boundary in the direction
/// given by [DirectionalTextEditingIntent.Forward].
public sealed class ExpandSelectionToDocumentBoundaryIntent : DirectionalTextEditingIntent
{
    public ExpandSelectionToDocumentBoundaryIntent(bool forward) : base(forward)
    {
    }
}

/// Expands the current selection to the closest line break in the direction
/// given by [DirectionalTextEditingIntent.Forward].
public sealed class ExpandSelectionToLineBreakIntent : DirectionalTextEditingIntent
{
    public ExpandSelectionToLineBreakIntent(bool forward) : base(forward)
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
public sealed class ScrollToDocumentBoundaryIntent : DirectionalCaretMovementIntent
{
    public ScrollToDocumentBoundaryIntent(bool forward, bool collapseSelection)
        : base(forward, collapseSelection)
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
