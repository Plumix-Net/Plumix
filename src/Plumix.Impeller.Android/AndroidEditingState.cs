using Android.Text;
using Android.Views;
using Android.Views.InputMethods;

namespace Plumix.Impeller.Android;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/android/io/flutter/plugin/editing/ListenableEditingState.java: the
// engine's mirror of the text input client, an Editable the IME edits through the InputConnection. Composing
// regions are the Editable's composing spans, set through a BaseInputConnection over it, as the engine does.
// Listeners hear one change per outermost batch edit instead of per span; the framework gets the batch as one
// delta (HostTextInputPlugin.UpdateEditingStateFromHost) rather than the engine's per-replace deltas.

/// <summary>
/// The text input client's text, selection and composing region as an Android <see cref="IEditable"/>.
/// </summary>
internal sealed class AndroidEditingState
{
    private readonly SpannableStringBuilder _text = new();
    private readonly SpanConnection _spanConnection;
    private int _batchEditNestDepth;
    private string _notifiedText = string.Empty;
    private (int SelectionStart, int SelectionEnd, int ComposingStart, int ComposingEnd) _notifiedRanges =
        (-1, -1, -1, -1);

    public AndroidEditingState(View view) => _spanConnection = new SpanConnection(view, _text);

    /// <summary>Raised after the outermost batch edit when the text, selection or composing region changed.</summary>
    public event Action? Changed;

    public IEditable Editable => _text;

    public int SelectionStart => Selection.GetSelectionStart(_text);

    public int SelectionEnd => Selection.GetSelectionEnd(_text);

    public int ComposingStart => BaseInputConnection.GetComposingSpanStart(_text);

    public int ComposingEnd => BaseInputConnection.GetComposingSpanEnd(_text);

    public override string ToString() => _text.ToString();

    public void BeginBatchEdit() => _batchEditNestDepth++;

    public void EndBatchEdit()
    {
        if (_batchEditNestDepth == 0)
        {
            return;
        }

        _batchEditNestDepth--;
        if (_batchEditNestDepth == 0)
        {
            NotifyIfChanged();
        }
    }

    /// <summary>
    /// <c>ListenableEditingState.setEditingState</c>: replaces everything with the framework's state. A negative
    /// selection removes it; an empty or negative composing region removes the composing spans.
    /// </summary>
    public void SetEditingState(
        string text,
        int selectionBase,
        int selectionExtent,
        int composingStart,
        int composingEnd)
    {
        BeginBatchEdit();
        _text.Replace(0, _text.Length(), text);
        if (selectionBase >= 0 && selectionExtent >= 0)
        {
            Selection.SetSelection(_text, selectionBase, selectionExtent);
        }
        else
        {
            Selection.RemoveSelection(_text);
        }

        // ListenableEditingState.setComposingRange.
        if (composingStart < 0 || composingStart >= composingEnd)
        {
            BaseInputConnection.RemoveComposingSpans(_text);
        }
        else
        {
            _spanConnection.SetComposingRegion(composingStart, composingEnd);
        }

        EndBatchEdit();
    }

    private void NotifyIfChanged()
    {
        string text = _text.ToString();
        var ranges = (SelectionStart, SelectionEnd, ComposingStart, ComposingEnd);
        if (text == _notifiedText && ranges == _notifiedRanges)
        {
            return;
        }

        _notifiedText = text;
        _notifiedRanges = ranges;
        Changed?.Invoke();
    }

    // ListenableEditingState's mDummyConnection: the composing spans go on through BaseInputConnection.
    private sealed class SpanConnection(View view, IEditable editable) : BaseInputConnection(view, true)
    {
        public override IEditable? Editable => editable;
    }
}
