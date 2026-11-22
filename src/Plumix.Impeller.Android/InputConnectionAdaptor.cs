using Android.Content;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Java.Lang;
using Plumix.UI;
using KeyEvent = Android.Views.KeyEvent;
using ClipboardManager = Android.Content.ClipboardManager;
using Math = System.Math;

namespace Plumix.Impeller.Android;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/android/io/flutter/plugin/editing/InputConnectionAdaptor.java: the IME
// edits the mirrored editable (AndroidEditingState) through BaseInputConnection's own implementations, each
// wrapped in a batch edit; the editable then reports the change to the framework. Not ported: the keyboard
// delegate (sendKeyEvent goes straight to handleKeyEvent, since this host does not send key events to the
// framework yet, which is also why Delete is handled here), the up/down arrows (they need a text layout),
// cursor anchor info, commitContent, private commands and Scribe.

/// <summary>The IME's connection to the text input client.</summary>
internal sealed class InputConnectionAdaptor : BaseInputConnection
{
    // PlatformPlugin's clip label for the clipboard actions.
    private const string ClipLabel = "text label?";

    // InputConnection.GET_EXTRACTED_TEXT_MONITOR and EditorInfo.IME_MASK_ACTION.
    private const int ExtractedTextMonitor = 1;
    private const int ImeMaskAction = 0xFF;

    private readonly View _view;
    private readonly AndroidEditingState _editable;
    private readonly EditorInfo _editorInfo;
    private readonly HostTextInputPlugin _plugin;
    private readonly InputMethodManager _imm;
    private ExtractedTextRequest? _extractRequest;
    private int _batchEditNestDepth;

    public InputConnectionAdaptor(
        View view,
        AndroidEditingState editable,
        EditorInfo editorInfo,
        HostTextInputPlugin plugin,
        InputMethodManager imm)
        : base(view, true)
    {
        _view = view;
        _editable = editable;
        _editorInfo = editorInfo;
        _plugin = plugin;
        _imm = imm;
        _editable.Changed += DidChangeEditingState;
    }

    public override IEditable? Editable => _editable.Editable;

    public override bool BeginBatchEdit()
    {
        _editable.BeginBatchEdit();
        _batchEditNestDepth++;
        return base.BeginBatchEdit();
    }

    public override bool EndBatchEdit()
    {
        bool result = base.EndBatchEdit();
        _batchEditNestDepth--;
        _editable.EndBatchEdit();
        return result;
    }

    public override bool DeleteSurroundingText(int beforeLength, int afterLength) =>
        _editable.SelectionStart == -1 || base.DeleteSurroundingText(beforeLength, afterLength);

    public override bool SetComposingText(ICharSequence? text, int newCursorPosition)
    {
        BeginBatchEdit();
        bool result = text is null || text.Length() == 0
            ? base.CommitText(text, newCursorPosition)
            : base.SetComposingText(text, newCursorPosition);
        EndBatchEdit();
        return result;
    }

    public override ExtractedText? GetExtractedText(ExtractedTextRequest? request, GetTextFlags flags)
    {
        // Text monitoring: DidChangeEditingState keeps the IME's extracted text current while it is on.
        _extractRequest = ((int)flags & ExtractedTextMonitor) != 0 ? request : null;
        return ExtractedText(request);
    }

    public override void CloseConnection()
    {
        base.CloseConnection();
        _editable.Changed -= DidChangeEditingState;
        for (; _batchEditNestDepth > 0; _batchEditNestDepth--)
        {
            EndBatchEdit();
        }
    }

    public override bool SetSelection(int start, int end)
    {
        BeginBatchEdit();
        bool result = base.SetSelection(start, end);
        EndBatchEdit();
        return result;
    }

    public override bool SendKeyEvent(KeyEvent? e) => e is not null && HandleKeyEvent(e);

    /// <summary>
    /// <c>handleKeyEvent</c>: arrows move the selection, Enter in a single-line field is the editor action,
    /// other printable keys replace the selection. Delete removes the selection or the character before it.
    /// </summary>
    public bool HandleKeyEvent(KeyEvent e)
    {
        if (e.Action != KeyEventActions.Down)
        {
            return false;
        }

        switch (e.KeyCode)
        {
            case Keycode.DpadLeft:
                return HandleHorizontalMovement(isLeft: true, e.IsShiftPressed);
            case Keycode.DpadRight:
                return HandleHorizontalMovement(isLeft: false, e.IsShiftPressed);
            case Keycode.Del:
                return HandleDelete();
            case Keycode.Enter or Keycode.NumpadEnter
                when (_editorInfo.InputType & InputTypes.TextFlagMultiLine) == 0:
                PerformEditorAction((ImeAction)((int)_editorInfo.ImeOptions & ImeMaskAction));
                return true;
        }

        int selectionStart = _editable.SelectionStart;
        int selectionEnd = _editable.SelectionEnd;
        int character = e.UnicodeChar;
        if (selectionStart < 0 || selectionEnd < 0 || character == 0)
        {
            return false;
        }

        int selectionMin = Math.Min(selectionStart, selectionEnd);
        int selectionMax = Math.Max(selectionStart, selectionEnd);
        BeginBatchEdit();
        if (selectionMin != selectionMax)
        {
            _editable.Editable.Delete(selectionMin, selectionMax);
        }

        _editable.Editable.Insert(selectionMin, char.ConvertFromUtf32(character));
        SetSelection(selectionMin + 1, selectionMin + 1);
        EndBatchEdit();
        return true;
    }

    public override bool PerformContextMenuAction(int id)
    {
        BeginBatchEdit();
        bool result = DoPerformContextMenuAction(id);
        EndBatchEdit();
        return result;
    }

    public override bool PerformEditorAction(ImeAction actionCode)
    {
        _plugin.PerformAction(actionCode switch
        {
            ImeAction.None => "TextInputAction.newline",
            ImeAction.Unspecified => "TextInputAction.unspecified",
            ImeAction.Go => "TextInputAction.go",
            ImeAction.Search => "TextInputAction.search",
            ImeAction.Send => "TextInputAction.send",
            ImeAction.Next => "TextInputAction.next",
            ImeAction.Previous => "TextInputAction.previous",
            _ => "TextInputAction.done",
        });
        return true;
    }

    // The ListenableEditingState watcher: the selection always (InputMethodManager drops repeats), the
    // extracted text while the IME monitors it.
    private void DidChangeEditingState()
    {
        _imm.UpdateSelection(
            _view,
            _editable.SelectionStart,
            _editable.SelectionEnd,
            _editable.ComposingStart,
            _editable.ComposingEnd);
        if (_extractRequest is { } request)
        {
            _imm.UpdateExtractedText(_view, request.Token, ExtractedText(request));
        }
    }

    private ExtractedText ExtractedText(ExtractedTextRequest? request) => new()
    {
        StartOffset = 0,
        PartialStartOffset = -1,
        PartialEndOffset = -1,
        SelectionStart = _editable.SelectionStart,
        SelectionEnd = _editable.SelectionEnd,
        Text = request is null || (request.Flags & GetTextFlags.WithStyles) == 0
            ? new Java.Lang.String(_editable.ToString())
            : _editable.Editable,
    };

    private bool HandleHorizontalMovement(bool isLeft, bool isShiftPressed)
    {
        int selectionStart = _editable.SelectionStart;
        int selectionEnd = _editable.SelectionEnd;
        if (selectionStart < 0 || selectionEnd < 0)
        {
            return false;
        }

        int newSelectionEnd = isLeft
            ? Math.Max(OffsetBefore(selectionEnd), 0)
            : Math.Min(OffsetAfter(selectionEnd), _editable.Editable.Length());
        bool shouldCollapse = selectionStart == selectionEnd && !isShiftPressed;
        SetSelection(shouldCollapse ? newSelectionEnd : selectionStart, newSelectionEnd);
        return true;
    }

    // What the framework's DeleteCharacterIntent does for Backspace: the selection, else the character before.
    private bool HandleDelete()
    {
        int selectionStart = _editable.SelectionStart;
        int selectionEnd = _editable.SelectionEnd;
        if (selectionStart < 0 || selectionEnd < 0)
        {
            return false;
        }

        int selectionMin = Math.Min(selectionStart, selectionEnd);
        int selectionMax = Math.Max(selectionStart, selectionEnd);
        if (selectionMin == selectionMax)
        {
            selectionMin = Math.Max(OffsetBefore(selectionMax), 0);
        }

        if (selectionMin == selectionMax)
        {
            return true;
        }

        BeginBatchEdit();
        _editable.Editable.Delete(selectionMin, selectionMax);
        SetSelection(selectionMin, selectionMin);
        EndBatchEdit();
        return true;
    }

    // FlutterTextUtils.getOffsetBefore/After: grapheme boundaries.
    private int OffsetBefore(int offset)
    {
        Java.Text.BreakIterator iterator = Java.Text.BreakIterator.CharacterInstance!;
        iterator.SetText(_editable.ToString());
        int before = iterator.Preceding(Math.Min(offset, _editable.Editable.Length()));
        return before == Java.Text.BreakIterator.Done ? 0 : before;
    }

    private int OffsetAfter(int offset)
    {
        Java.Text.BreakIterator iterator = Java.Text.BreakIterator.CharacterInstance!;
        iterator.SetText(_editable.ToString());
        int after = iterator.Following(Math.Max(offset, 0));
        return after == Java.Text.BreakIterator.Done ? _editable.Editable.Length() : after;
    }

    private bool DoPerformContextMenuAction(int id)
    {
        IEditable editable = _editable.Editable;
        int selectionStart = _editable.SelectionStart;
        int selectionEnd = _editable.SelectionEnd;
        int selectionMin = Math.Min(selectionStart, selectionEnd);
        int selectionMax = Math.Max(selectionStart, selectionEnd);
        switch (id)
        {
            case global::Android.Resource.Id.SelectAll:
                SetSelection(0, editable.Length());
                return true;
            case global::Android.Resource.Id.Cut:
                if (selectionStart != selectionEnd)
                {
                    Clipboard?.PrimaryClip = ClipData.NewPlainText(
                        new Java.Lang.String(ClipLabel), editable.SubSequenceFormatted(selectionMin, selectionMax));
                    editable.Delete(selectionMin, selectionMax);
                    SetSelection(selectionMin, selectionMin);
                }

                return true;
            case global::Android.Resource.Id.Copy:
                if (selectionStart != selectionEnd)
                {
                    Clipboard?.PrimaryClip = ClipData.NewPlainText(
                        new Java.Lang.String(ClipLabel), editable.SubSequenceFormatted(selectionMin, selectionMax));
                }

                return true;
            case global::Android.Resource.Id.Paste:
                if (Clipboard?.PrimaryClip?.GetItemAt(0)?.CoerceToText(_view.Context) is { } textToPaste)
                {
                    selectionMin = Math.Max(0, selectionMin);
                    selectionMax = Math.Max(0, selectionMax);
                    if (selectionMin != selectionMax)
                    {
                        editable.Delete(selectionMin, selectionMax);
                    }

                    editable.Insert(selectionMin, textToPaste);
                    int newSelectionStart = selectionMin + textToPaste.Length;
                    SetSelection(newSelectionStart, newSelectionStart);
                }

                return true;
            default:
                return false;
        }
    }

    private ClipboardManager? Clipboard =>
        _view.Context?.GetSystemService(Context.ClipboardService) as ClipboardManager;
}
