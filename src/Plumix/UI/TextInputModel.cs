using System.Text;

// C#-only infrastructure; no Dart parity source. A 1:1 port of the engine's platform-side editing
// model, `engine/src/flutter/shell/platform/common/text_input_model.{h,cc}` and `text_range.h`,
// which the Windows and Linux embedders (and, through the same class, the macOS one) use to apply
// IME and keyboard edits before reporting them to the framework over `flutter/textinput`. Plumix
// hosts are the engine, so `HostTextInputPlugin` keeps one of these per attached client.

namespace Plumix.UI;

/// <summary>
/// Handles underlying text input state, using a simple model. Port of the engine's
/// <c>flutter::TextInputModel</c>.
/// </summary>
/// <remarks>Ignores special states like "insert mode", as the engine does.</remarks>
public sealed class TextInputModel
{
    private string _text = string.Empty;
    private Range _selection = new(0);
    private Range _composingRange = new(0);
    private bool _composing;

    /// <summary>
    /// A range of text in UTF-16 code units, with a base and an extent. Port of the engine's
    /// <c>flutter::TextRange</c> (<c>shell/platform/common/text_range.h</c>), which is directional,
    /// unlike the framework's <see cref="Plumix.Widgets.TextRange"/>.
    /// </summary>
    public readonly record struct Range(int Base, int Extent)
    {
        /// <summary>A collapsed range at <paramref name="position"/>.</summary>
        public Range(int position)
            : this(position, position)
        {
        }

        /// <summary>The lesser of <see cref="Base"/> and <see cref="Extent"/>.</summary>
        public int Start => Math.Min(Base, Extent);

        /// <summary>The greater of <see cref="Base"/> and <see cref="Extent"/>.</summary>
        public int End => Math.Max(Base, Extent);

        /// <summary>The position of a collapsed range.</summary>
        public int Position
        {
            get
            {
                System.Diagnostics.Debug.Assert(Base == Extent, "position() of a non-collapsed range");
                return Extent;
            }
        }

        /// <summary>The number of code units in the range.</summary>
        public int Length => End - Start;

        /// <summary>Whether <see cref="Base"/> equals <see cref="Extent"/>.</summary>
        public bool Collapsed => Base == Extent;

        /// <summary>Whether <see cref="Base"/> is after <see cref="Extent"/>.</summary>
        public bool Reversed => Base > Extent;

        /// <summary>A copy whose start (the lesser end) is <paramref name="position"/>: the engine's
        /// <c>set_start</c>.</summary>
        public Range WithStart(int position) =>
            Base <= Extent ? this with { Base = position } : this with { Extent = position };

        /// <summary>A copy whose end (the greater end) is <paramref name="position"/>: the engine's
        /// <c>set_end</c>.</summary>
        public Range WithEnd(int position) =>
            Base <= Extent ? this with { Extent = position } : this with { Base = position };

        /// <summary>Whether <paramref name="position"/> lies within the range, ends included.</summary>
        public bool Contains(int position) => position >= Start && position <= End;

        /// <summary>Whether <paramref name="range"/> lies within this range, ends included.</summary>
        public bool Contains(Range range) => range.Start >= Start && range.End <= End;
    }

    /// <summary>A range covering the entire text.</summary>
    public Range TextRange => new(0, _text.Length);

    /// <summary>The current selection.</summary>
    public Range Selection => _selection;

    /// <summary>The composing range. If not in composing mode, a collapsed range at position 0.</summary>
    public Range ComposingRange => _composingRange;

    /// <summary>Whether multi-step input composing mode is active.</summary>
    public bool Composing => _composing;

    /// <summary>
    /// The currently editable text range: the composing range in composing mode, otherwise a range
    /// covering the entire text.
    /// </summary>
    private Range EditableRange => _composing ? _composingRange : TextRange;

    /// <summary>
    /// Sets the text, as well as the selection and the composing region.
    /// </summary>
    /// <remarks>
    /// This method is typically used to update the model's editing state when the framework sends
    /// its latest text editing state. The text is always replaced; the ranges are only applied when
    /// both lie within it.
    /// </remarks>
    public bool SetText(string text, Range? selection = null, Range? composingRange = null)
    {
        Range newSelection = selection ?? new Range(0);
        Range newComposingRange = composingRange ?? new Range(0);
        _text = text;
        if (!TextRange.Contains(newSelection) || !TextRange.Contains(newComposingRange))
        {
            return false;
        }

        _selection = newSelection;
        _composingRange = newComposingRange;
        _composing = !newComposingRange.Collapsed;
        return true;
    }

    /// <summary>Attempts to set the text selection.</summary>
    /// <remarks>
    /// Returns false if the selection is not within the bounds of the text. While in composing mode,
    /// the selection is restricted to the composing range; otherwise, it is restricted to the length
    /// of the text.
    /// </remarks>
    public bool SetSelection(Range range)
    {
        if (_composing && !range.Collapsed)
        {
            return false;
        }

        if (!EditableRange.Contains(range))
        {
            return false;
        }

        _selection = range;
        return true;
    }

    /// <summary>Attempts to set the composing range.</summary>
    /// <remarks>
    /// Returns false if the range is out of range for the text, or if not composing. The selection
    /// collapses <paramref name="cursorOffset"/> code units after the start of the range.
    /// </remarks>
    public bool SetComposingRange(Range range, int cursorOffset)
    {
        if (!_composing || !TextRange.Contains(range))
        {
            return false;
        }

        _composingRange = range;
        _selection = new Range(range.Start + cursorOffset);
        return true;
    }

    /// <summary>Begins IME composing mode.</summary>
    /// <remarks>
    /// Resets the composing base and extent to the selection start. The existing selection is
    /// preserved in case composing is aborted with no changes. Until <see cref="EndComposing"/> is
    /// called, any further changes to selection base and extent are restricted to the composing
    /// range.
    /// </remarks>
    public void BeginComposing()
    {
        _composing = true;
        _composingRange = new Range(_selection.Start);
    }

    /// <summary>Replaces the composing range with new text, and sets the selection.</summary>
    /// <remarks>
    /// The given <paramref name="text"/> replaces text within the current composing range, or the
    /// current selection if the text wasn't composing. The composing range is adjusted to the length
    /// of <paramref name="text"/>, and <paramref name="selection"/> describes the new selection range,
    /// relative to the start of the new composing range. Without a selection, it collapses at the end
    /// of the composing text.
    /// </remarks>
    public void UpdateComposingText(string text, Range? selection = null)
    {
        Range relativeSelection = selection ?? new Range(text.Length);
        if (text.Length == 0 && _composingRange.Collapsed)
        {
            return;
        }

        Range rangeToDelete = _composingRange.Collapsed ? _selection : _composingRange;
        _text = string.Concat(
            _text.AsSpan(0, rangeToDelete.Start),
            text,
            _text.AsSpan(rangeToDelete.Start + rangeToDelete.Length));
        _composingRange = _composingRange.WithEnd(_composingRange.Start + text.Length);
        _selection = new Range(
            relativeSelection.Start + _composingRange.Start,
            relativeSelection.Extent + _composingRange.Start);
    }

    /// <summary>Commits the composing range to the string.</summary>
    /// <remarks>Collapses the composing base and extent to the end of the range.</remarks>
    public void CommitComposing()
    {
        if (_composingRange.Collapsed)
        {
            return;
        }

        _composingRange = new Range(_composingRange.End);
        _selection = _composingRange;
    }

    /// <summary>Ends IME composing mode.</summary>
    /// <remarks>Collapses the composing base and offset to 0.</remarks>
    public void EndComposing()
    {
        _composing = false;
        _composingRange = new Range(0);
    }

    /// <summary>Adds a Unicode code point.</summary>
    /// <remarks>
    /// Either inserts at the cursor (when selection base and extent are the same), or deletes the
    /// selected text, replacing it with the given code point.
    /// </remarks>
    public void AddCodePoint(int codePoint)
    {
        if (codePoint <= 0xFFFF)
        {
            AddText(((char)codePoint).ToString());
        }
        else
        {
            int toDecompose = codePoint - 0x10000;
            AddText(new string([(char)((toDecompose >> 10) + 0xd800), (char)((toDecompose % 0x400) + 0xdc00)]));
        }
    }

    /// <summary>Adds text.</summary>
    /// <remarks>
    /// Either inserts at the cursor (when selection base and extent are the same), or deletes the
    /// selected text, replacing it with the given text. While composing, the text replaces the
    /// composing range.
    /// </remarks>
    public void AddText(string text)
    {
        DeleteSelected();
        if (_composing)
        {
            // Delete the current composing text, set the cursor to composing start.
            _text = _text.Remove(_composingRange.Start, _composingRange.Length);
            _selection = new Range(_composingRange.Start);
            _composingRange = _composingRange.WithEnd(_composingRange.Start + text.Length);
        }

        int position = _selection.Position;
        _text = _text.Insert(position, text);
        _selection = new Range(position + text.Length);
    }

    /// <summary>Deletes either the selection, or one character behind the cursor.</summary>
    /// <remarks>
    /// Deleting one character behind the cursor occurs when the selection base and extent are the
    /// same. When composing is active, deletions are restricted to the text between the composing
    /// base and extent. Returns whether any deletion actually occurred.
    /// </remarks>
    public bool Backspace()
    {
        if (DeleteSelected())
        {
            return true;
        }

        // There is no selection. Delete the preceding codepoint.
        int position = _selection.Position;
        if (position != EditableRange.Start)
        {
            int count = char.IsLowSurrogate(_text[position - 1]) ? 2 : 1;
            _text = _text.Remove(position - count, count);
            _selection = new Range(position - count);
            if (_composing)
            {
                _composingRange = _composingRange.WithEnd(_composingRange.End - count);
            }

            return true;
        }

        return false;
    }

    /// <summary>Deletes either the selection, or one character ahead of the cursor.</summary>
    /// <remarks>
    /// Deleting one character ahead of the cursor occurs when the selection base and extent are the
    /// same. When composing is active, deletions are restricted to text between the composing base
    /// and extent. Returns whether any deletion actually occurred.
    /// </remarks>
    public bool Delete()
    {
        if (DeleteSelected())
        {
            return true;
        }

        // There is no selection. Delete the following codepoint.
        int position = _selection.Position;
        if (position < EditableRange.End)
        {
            int count = char.IsHighSurrogate(_text[position]) ? 2 : 1;
            _text = _text.Remove(position, count);
            if (_composing)
            {
                _composingRange = _composingRange.WithEnd(_composingRange.End - count);
            }

            return true;
        }

        return false;
    }

    /// <summary>Deletes text near the cursor.</summary>
    /// <remarks>
    /// A section is made starting at <paramref name="offsetFromCursor"/> code points past the cursor
    /// (negative values go before the cursor). <paramref name="count"/> code points are removed. The
    /// selection may go outside the bounds of the available text and will result in only the part
    /// selection that covers the available text being deleted. The existing selection is ignored and
    /// removed after this operation. When composing is active, deletions are restricted to the
    /// composing range. Returns whether any deletion actually occurred.
    /// </remarks>
    public bool DeleteSurrounding(int offsetFromCursor, int count)
    {
        int maxPosition = EditableRange.End;
        int start = _selection.Extent;
        if (offsetFromCursor < 0)
        {
            for (int i = 0; i < -offsetFromCursor; i++)
            {
                // If requested start is before the available text then reduce the number of
                // characters to delete.
                if (start == EditableRange.Start)
                {
                    count = i;
                    break;
                }

                start -= char.IsLowSurrogate(_text[start - 1]) ? 2 : 1;
            }
        }
        else
        {
            for (int i = 0; i < offsetFromCursor && start != maxPosition; i++)
            {
                start += char.IsHighSurrogate(_text[start]) ? 2 : 1;
            }
        }

        int end = start;
        for (int i = 0; i < count && end != maxPosition; i++)
        {
            // The engine inspects the code unit at `start` here, not at `end`; kept as is.
            end += char.IsHighSurrogate(_text[start]) ? 2 : 1;
        }

        if (start == end)
        {
            return false;
        }

        int deletedLength = end - start;
        _text = _text.Remove(start, deletedLength);

        // Cursor moves only if deleted area is before it.
        _selection = new Range(offsetFromCursor <= 0 ? start : _selection.Start);

        // Adjust composing range.
        if (_composing)
        {
            _composingRange = _composingRange.WithEnd(_composingRange.End - deletedLength);
        }

        return true;
    }

    /// <summary>Attempts to move the cursor to the beginning.</summary>
    /// <remarks>
    /// If composing is active, the cursor is moved to the beginning of the composing range;
    /// otherwise, it is moved to the beginning of the text. Returns whether the cursor moved.
    /// </remarks>
    public bool MoveCursorToBeginning()
    {
        int minPosition = EditableRange.Start;
        if (_selection.Collapsed && _selection.Position == minPosition)
        {
            return false;
        }

        _selection = new Range(minPosition);
        return true;
    }

    /// <summary>Attempts to move the cursor to the end.</summary>
    /// <remarks>
    /// If composing is active, the cursor is moved to the end of the composing range; otherwise, it
    /// is moved to the end of the text. Returns whether the cursor moved.
    /// </remarks>
    public bool MoveCursorToEnd()
    {
        int maxPosition = EditableRange.End;
        if (_selection.Collapsed && _selection.Position == maxPosition)
        {
            return false;
        }

        _selection = new Range(maxPosition);
        return true;
    }

    /// <summary>Attempts to select text from the cursor position to the beginning.</summary>
    /// <remarks>
    /// If composing is active, the selection is applied to the beginning of the composing range;
    /// otherwise, it is applied to the beginning of the text. Returns whether the selection changed.
    /// </remarks>
    public bool SelectToBeginning()
    {
        int minPosition = EditableRange.Start;
        if (_selection.Collapsed && _selection.Position == minPosition)
        {
            return false;
        }

        _selection = new Range(_selection.Base, minPosition);
        return true;
    }

    /// <summary>Attempts to select text from the cursor position to the end.</summary>
    /// <remarks>
    /// If composing is active, the selection is applied to the end of the composing range;
    /// otherwise, it is applied to the end of the text. Returns whether the selection changed.
    /// </remarks>
    public bool SelectToEnd()
    {
        int maxPosition = EditableRange.End;
        if (_selection.Collapsed && _selection.Position == maxPosition)
        {
            return false;
        }

        _selection = new Range(_selection.Base, maxPosition);
        return true;
    }

    /// <summary>Attempts to move the cursor forward.</summary>
    /// <remarks>
    /// If a selection is active, moves to the end of the selection. If composing is active, motion is
    /// restricted to the composing range. Returns whether the cursor moved.
    /// </remarks>
    public bool MoveCursorForward()
    {
        // If there's a selection, move to the end of the selection.
        if (!_selection.Collapsed)
        {
            _selection = new Range(_selection.End);
            return true;
        }

        // Otherwise, move the cursor forward.
        int position = _selection.Position;
        if (position != EditableRange.End)
        {
            int count = char.IsHighSurrogate(_text[position]) ? 2 : 1;
            _selection = new Range(position + count);
            return true;
        }

        return false;
    }

    /// <summary>Attempts to move the cursor backward.</summary>
    /// <remarks>
    /// If a selection is active, moves to the start of the selection. If composing is active, motion
    /// is restricted to the composing range. Returns whether the cursor moved.
    /// </remarks>
    public bool MoveCursorBack()
    {
        // If there's a selection, move to the beginning of the selection.
        if (!_selection.Collapsed)
        {
            _selection = new Range(_selection.Start);
            return true;
        }

        // Otherwise, move the cursor backward.
        int position = _selection.Position;
        if (position != EditableRange.Start)
        {
            int count = char.IsLowSurrogate(_text[position - 1]) ? 2 : 1;
            _selection = new Range(position - count);
            return true;
        }

        return false;
    }

    /// <summary>The current text.</summary>
    public string GetText() => _text;

    /// <summary>The cursor position as a byte offset in the UTF-8 encoding of
    /// <see cref="GetText"/>.</summary>
    public int GetCursorOffset() => Encoding.UTF8.GetByteCount(_text.AsSpan(0, _selection.Extent));

    /// <summary>Deletes the current selection, if any.</summary>
    /// <remarks>
    /// Returns whether any text was deleted. The selection base and extent are reset to the start of
    /// the selected range.
    /// </remarks>
    private bool DeleteSelected()
    {
        if (_selection.Collapsed)
        {
            return false;
        }

        int start = _selection.Start;
        _text = _text.Remove(start, _selection.Length);
        _selection = new Range(start);
        if (_composing)
        {
            // This occurs only immediately after composing has begun with a selection.
            _composingRange = _selection;
        }

        return true;
    }
}
