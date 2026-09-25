using Avalonia;
using Avalonia.Media;
using System.Collections;
using System.Globalization;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/editable_text.dart

namespace Plumix.Widgets;

public readonly record struct TextSelection(
    int BaseOffset,
    int ExtentOffset,
    TextAffinity Affinity = TextAffinity.Downstream,
    bool IsDirectional = false)
{
    public int Start => Math.Min(BaseOffset, ExtentOffset);

    public int End => Math.Max(BaseOffset, ExtentOffset);

    public bool IsCollapsed => BaseOffset == ExtentOffset;

    /// <summary>Whether this selection represents a valid position in the text.</summary>
    public bool IsValid => Start >= 0 && End >= 0;

    public static TextSelection Collapsed(int offset, TextAffinity affinity = TextAffinity.Downstream)
    {
        return new TextSelection(offset, offset, affinity);
    }

    /// <summary>A selection that is not in the text.</summary>
    public static TextSelection Invalid => new(-1, -1);

    /// <summary>The text inside this selection. Dart's <c>TextRange.textInside</c>, which
    /// <c>TextSelection</c> inherits.</summary>
    public string TextInside(string text) => text[Start..End];

    /// <summary>Creates a collapsed selection at the given text position.</summary>
    /// <remarks>Dart's <c>TextSelection.fromPosition</c>.</remarks>
    public static TextSelection FromPosition(TextPosition position) =>
        new(position.Offset, position.Offset, position.Affinity);

    /// <summary>The position at which the selection originates.</summary>
    /// <remarks>
    /// Dart's <c>TextSelection.base</c>: the affinity points into the selection, or is the
    /// selection's own affinity when it is collapsed or invalid.
    /// </remarks>
    public TextPosition Base
    {
        get
        {
            TextAffinity affinity;
            if (!IsValid || BaseOffset == ExtentOffset)
            {
                affinity = Affinity;
            }
            else if (BaseOffset < ExtentOffset)
            {
                affinity = TextAffinity.Downstream;
            }
            else
            {
                affinity = TextAffinity.Upstream;
            }

            return new TextPosition(BaseOffset, affinity);
        }
    }

    /// <summary>The position at which the selection terminates.</summary>
    /// <remarks>Dart's <c>TextSelection.extent</c>.</remarks>
    public TextPosition Extent
    {
        get
        {
            TextAffinity affinity;
            if (!IsValid || BaseOffset == ExtentOffset)
            {
                affinity = Affinity;
            }
            else if (BaseOffset < ExtentOffset)
            {
                affinity = TextAffinity.Upstream;
            }
            else
            {
                affinity = TextAffinity.Downstream;
            }

            return new TextPosition(ExtentOffset, affinity);
        }
    }

    /// <summary>This selection viewed as a text range.</summary>
    /// <remarks>Dart's <c>TextSelection extends TextRange</c>; a C# record struct cannot derive from
    /// another, so the conversion is explicit.</remarks>
    public TextRange AsTextRange() => new(Start, End);

    /// <summary>
    /// Expands the selection to include <paramref name="position"/>: Dart's
    /// <c>TextSelection.expandTo</c>. The selection's normalization is kept unless
    /// <paramref name="extentAtIndex"/> is set, which puts the extent at the position.
    /// </summary>
    public TextSelection ExpandTo(TextPosition position, bool extentAtIndex = false)
    {
        // If position is already within in the selection, there's nothing to do.
        if (position.Offset >= Start && position.Offset <= End)
        {
            return this;
        }

        bool normalized = BaseOffset <= ExtentOffset;
        if (position.Offset <= Start)
        {
            // Here the position is somewhere before the selection: ..|..[...]....
            if (extentAtIndex)
            {
                return this with { BaseOffset = End, ExtentOffset = position.Offset, Affinity = position.Affinity };
            }

            return this with
            {
                BaseOffset = normalized ? position.Offset : BaseOffset,
                ExtentOffset = normalized ? ExtentOffset : position.Offset,
            };
        }

        // Here the position is somewhere after the selection: ....[...]..|..
        if (extentAtIndex)
        {
            return this with { BaseOffset = Start, ExtentOffset = position.Offset, Affinity = position.Affinity };
        }

        return this with
        {
            BaseOffset = normalized ? BaseOffset : position.Offset,
            ExtentOffset = normalized ? position.Offset : ExtentOffset,
        };
    }

    /// <summary>
    /// Keeps the base and moves the extent to <paramref name="position"/>: Dart's
    /// <c>TextSelection.extendTo</c>.
    /// </summary>
    public TextSelection ExtendTo(TextPosition position)
    {
        // If the selection's extent is at the position already, then nothing happens.
        if (Extent.Equals(position))
        {
            return this;
        }

        return this with { ExtentOffset = position.Offset, Affinity = position.Affinity };
    }

    internal TextSelection Clamp(int textLength)
    {
        int clampedBaseOffset = Math.Clamp(BaseOffset, 0, textLength);
        int clampedExtentOffset = Math.Clamp(ExtentOffset, 0, textLength);
        return new TextSelection(clampedBaseOffset, clampedExtentOffset, Affinity, IsDirectional);
    }

    /// <inheritdoc/>
    /// <remarks>Dart's rules: every invalid selection is the same selection, and the affinity only
    /// participates while the selection is collapsed.</remarks>
    public bool Equals(TextSelection other)
    {
        if (!IsValid)
        {
            return !other.IsValid;
        }

        return other.BaseOffset == BaseOffset
               && other.ExtentOffset == ExtentOffset
               && (!IsCollapsed || other.Affinity == Affinity)
               && other.IsDirectional == IsDirectional;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        if (!IsValid)
        {
            return HashCode.Combine(-1, -1, TextAffinity.Downstream);
        }

        return HashCode.Combine(
            BaseOffset,
            ExtentOffset,
            IsCollapsed ? Affinity : TextAffinity.Downstream,
            IsDirectional);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (!IsValid)
        {
            return "TextSelection.invalid";
        }

        string affinity = $"TextAffinity.{char.ToLowerInvariant(Affinity.ToString()[0])}{Affinity.ToString()[1..]}";
        string directional = IsDirectional ? "true" : "false";
        return IsCollapsed
            ? $"TextSelection.collapsed(offset: {BaseOffset}, affinity: {affinity}, "
              + $"isDirectional: {directional})"
            : $"TextSelection(baseOffset: {BaseOffset}, extentOffset: {ExtentOffset}, "
              + $"isDirectional: {directional})";
    }
}

public readonly record struct TextRange(int Start, int End)
{
    /// A text range that starts and ends at `offset`.
    public static TextRange Collapsed(int offset) => new(offset, offset);

    /// A text range that contains nothing and is not in the text.
    public static TextRange Empty => new(-1, -1);

    public bool IsCollapsed => Start == End;

    /// Whether this range represents a valid position in the text.
    public bool IsValid => Start >= 0 && End >= 0;

    /// Whether the start of this range precedes the end.
    public bool IsNormalized => End >= Start;

    /// The text before this range.
    public string TextBefore(string text) => text[..Math.Max(0, Start)];

    /// The text after this range.
    public string TextAfter(string text) => text[Math.Min(text.Length, End)..];

    /// The text inside this range.
    public string TextInside(string text)
    {
        int start = Math.Clamp(Start, 0, text.Length);
        int end = Math.Clamp(End, start, text.Length);
        return text[start..end];
    }

    internal TextRange Clamp(int textLength)
    {
        return new TextRange(
            Start: Math.Clamp(Start, 0, textLength),
            End: Math.Clamp(End, 0, textLength));
    }

    /// <inheritdoc/>
    public override string ToString() => $"TextRange(start: {Start}, end: {End})";
}

public readonly record struct TextEditingValue
{
    /// <summary>Creates an empty editing value.</summary>
    /// <remarks>A struct's implicit parameterless constructor wins over one whose parameters are all
    /// optional, so without this <c>new TextEditingValue()</c> would zero-initialize instead of
    /// running the defaulting rules.</remarks>
    public TextEditingValue()
        : this(string.Empty, null, null)
    {
    }

    public TextEditingValue(
        string text = "",
        TextSelection? selection = null,
        TextRange? composing = null)
    {
        Text = text ?? string.Empty;
        Selection = (selection ?? TextSelection.Collapsed(Text.Length)).Clamp(Text.Length);
        TextRange? normalizedComposing = composing?.Clamp(Text.Length);
        Composing = normalizedComposing is { IsCollapsed: false } ? normalizedComposing : null;
    }

    public string Text { get; }

    public TextSelection Selection { get; }

    public TextRange? Composing { get; }

    /// <summary>Whether the composing region is a valid range within the text.</summary>
    public bool IsComposingRangeValid =>
        Composing is { IsValid: true, IsNormalized: true } composing && composing.End <= Text.Length;

    /// <summary>Creates a copy of this value with the given fields replaced.</summary>
    public TextEditingValue CopyWith(
        string? text = null,
        TextSelection? selection = null,
        TextRange? composing = null)
    {
        return new TextEditingValue(
            text: text ?? Text,
            selection: selection ?? Selection,
            composing: composing ?? Composing);
    }

    /// <summary>
    /// Returns a value with the text in <paramref name="replacementRange"/> replaced by
    /// <paramref name="replacementString"/>, with the selection and composing region adjusted.
    /// </summary>
    public TextEditingValue Replaced(TextRange replacementRange, string replacementString)
    {
        if (!replacementRange.IsValid)
        {
            return this;
        }

        string newText = string.Concat(
            Text[..replacementRange.Start],
            replacementString,
            Text[replacementRange.End..]);
        if (replacementRange.End - replacementRange.Start == replacementString.Length)
        {
            return CopyWith(text: newText);
        }

        int AdjustIndex(int originalIndex)
        {
            int replacedLength =
                originalIndex <= replacementRange.Start && originalIndex < replacementRange.End
                    ? 0
                    : replacementString.Length;
            int removedLength =
                Math.Clamp(originalIndex, replacementRange.Start, replacementRange.End) - replacementRange.Start;
            return originalIndex + replacedLength - removedLength;
        }

        return new TextEditingValue(
            text: newText,
            selection: new TextSelection(
                BaseOffset: AdjustIndex(Selection.BaseOffset),
                ExtentOffset: AdjustIndex(Selection.ExtentOffset)),
            composing: Composing is { } composing
                ? new TextRange(AdjustIndex(composing.Start), AdjustIndex(composing.End))
                : null);
    }

    /// <summary>The JSON payload the host exchanges with the framework.</summary>
    public Dictionary<string, object?> ToJson()
    {
        string affinity = Selection.Affinity == TextAffinity.Upstream
            ? "TextAffinity.upstream"
            : "TextAffinity.downstream";
        return new Dictionary<string, object?>
        {
            ["text"] = Text,
            ["selectionBase"] = Selection.BaseOffset,
            ["selectionExtent"] = Selection.ExtentOffset,
            ["selectionAffinity"] = affinity,
            ["selectionIsDirectional"] = Selection.IsDirectional,
            ["composingBase"] = Composing?.Start ?? -1,
            ["composingExtent"] = Composing?.End ?? -1,
        };
    }

    /// <summary>Creates a value from the host's JSON payload.</summary>
    public static TextEditingValue FromJson(IDictionary json)
    {
        ArgumentNullException.ThrowIfNull(json);
        string text = json["text"] as string ?? string.Empty;
        int selectionBase = ReadInt(json, "selectionBase", -1);
        int selectionExtent = ReadInt(json, "selectionExtent", -1);
        int composingBase = ReadInt(json, "composingBase", -1);
        int composingExtent = ReadInt(json, "composingExtent", -1);
        TextAffinity affinity = ReadAffinity(json) ?? TextAffinity.Downstream;
        bool isDirectional = json.Contains("selectionIsDirectional")
                             && json["selectionIsDirectional"] is true;
        TextSelection? selection = selectionBase < 0 && selectionExtent < 0
            ? null
            : new TextSelection(
                Math.Max(0, selectionBase),
                Math.Max(0, selectionExtent),
                affinity,
                isDirectional);
        TextRange? composing = composingBase < 0 || composingExtent < 0
            ? null
            : new TextRange(composingBase, composingExtent);
        return new TextEditingValue(text, selection, composing);
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"TextEditingValue(text: ┤{Text}├, selection: {Selection}, "
        + $"composing: {Composing ?? TextRange.Empty})";

    private static TextAffinity? ReadAffinity(IDictionary json)
    {
        object? value = json.Contains("selectionAffinity") ? json["selectionAffinity"] : null;
        return value as string switch
        {
            "TextAffinity.downstream" => TextAffinity.Downstream,
            "TextAffinity.upstream" => TextAffinity.Upstream,
            _ => null,
        };
    }

    private static int ReadInt(IDictionary json, string key, int fallback)
    {
        object? value = json.Contains(key) ? json[key] : null;
        return value is null ? fallback : Convert.ToInt32(value);
    }
}

public class TextEditingController : ChangeNotifier, IValueListenable<TextEditingValue>
{
    private string _text;
    private TextSelection _selection;
    private TextRange? _composing;

    public TextEditingController(
        string text = "",
        TextSelection? selection = null,
        TextRange? composing = null)
    {
        _text = text ?? string.Empty;
        _selection = (selection ?? TextSelection.Collapsed(_text.Length)).Clamp(_text.Length);
        _composing = composing?.Clamp(_text.Length);
    }

    public string Text
    {
        get => _text;
        set
        {
            string next = value ?? string.Empty;
            SetValue(
                text: next,
                selection: TextSelection.Collapsed(next.Length),
                composing: null);
        }
    }

    public TextSelection Selection
    {
        get => _selection;
        set => SetValue(text: _text, selection: value, composing: _composing);
    }

    public TextEditingValue Value
    {
        get => new(_text, _selection, _composing);
        set => SetValue(value.Text, value.Selection, value.Composing);
    }

    public static TextEditingController FromValue(TextEditingValue? value)
    {
        TextEditingValue initialValue = value ?? new TextEditingValue();
        return new TextEditingController(
            initialValue.Text,
            initialValue.Selection,
            initialValue.Composing);
    }

    public TextRange? Composing
    {
        get => _composing;
        set => SetValue(text: _text, selection: _selection, composing: value);
    }

    public void SetValue(
        string text,
        TextSelection? selection = null,
        TextRange? composing = null)
    {
        string normalizedText = text ?? string.Empty;
        var normalizedSelection = (selection ?? _selection).Clamp(normalizedText.Length);
        var normalizedComposing = composing?.Clamp(normalizedText.Length);
        if (normalizedComposing.HasValue && normalizedComposing.Value.IsCollapsed)
        {
            normalizedComposing = null;
        }

        if (string.Equals(_text, normalizedText, StringComparison.Ordinal)
            && _selection.Equals(normalizedSelection)
            && Nullable.Equals(_composing, normalizedComposing))
        {
            return;
        }

        _text = normalizedText;
        _selection = normalizedSelection;
        _composing = normalizedComposing;
        NotifyListeners();
    }

    /// <summary>Builds a <see cref="TextSpan"/> from the current editing value.</summary>
    /// <remarks>
    /// Dart's <c>TextEditingController.buildTextSpan</c>. By default makes text in the composing range
    /// appear as underlined. Descendants can override this method to customize the appearance of
    /// text.
    /// </remarks>
    public virtual TextSpan BuildTextSpan(BuildContext context, TextStyle? style, bool withComposing)
    {
        TextEditingValue value = Value;
        System.Diagnostics.Debug.Assert(
            value.Composing is not { IsValid: true } || !withComposing || value.IsComposingRangeValid);
        // If the composing range is out of range for the current text, ignore it to preserve the
        // tree integrity, otherwise in release mode a RangeError will be thrown and this EditableText
        // will be built with a broken subtree.
        bool composingRegionOutOfRange = !value.IsComposingRangeValid || !withComposing;

        if (composingRegionOutOfRange)
        {
            return new TextSpan(style: style, text: Text);
        }

        var underline = new TextStyle(Decoration: Plumix.UI.TextDecoration.Underline);
        TextStyle composingStyle = style?.Merge(underline) ?? underline;
        TextRange composing = value.Composing!.Value;
        return new TextSpan(
            style: style,
            children:
            [
                new TextSpan(text: composing.TextBefore(value.Text)),
                new TextSpan(style: composingStyle, text: composing.TextInside(value.Text)),
                new TextSpan(text: composing.TextAfter(value.Text)),
            ]);
    }

    public bool SelectAll()
    {
        return UpdateSelection(new TextSelection(0, _text.Length));
    }

    public bool MoveCaretLeft(bool extendSelection = false)
    {
        if (!extendSelection && !_selection.IsCollapsed)
        {
            return UpdateSelection(TextSelection.Collapsed(_selection.Start));
        }

        int nextExtentOffset = FindPreviousTextElementBoundary(_selection.ExtentOffset);
        var nextSelection = extendSelection
            ? new TextSelection(_selection.BaseOffset, nextExtentOffset)
            : TextSelection.Collapsed(nextExtentOffset);
        return UpdateSelection(nextSelection);
    }

    public bool MoveCaretRight(bool extendSelection = false)
    {
        if (!extendSelection && !_selection.IsCollapsed)
        {
            return UpdateSelection(TextSelection.Collapsed(_selection.End));
        }

        int nextExtentOffset = FindNextTextElementBoundary(_selection.ExtentOffset);
        var nextSelection = extendSelection
            ? new TextSelection(_selection.BaseOffset, nextExtentOffset)
            : TextSelection.Collapsed(nextExtentOffset);
        return UpdateSelection(nextSelection);
    }

    public bool MoveCaretToStart(bool extendSelection = false)
    {
        var nextSelection = extendSelection
            ? new TextSelection(_selection.BaseOffset, 0)
            : TextSelection.Collapsed(0);
        return UpdateSelection(nextSelection);
    }

    public bool MoveCaretToEnd(bool extendSelection = false)
    {
        var nextSelection = extendSelection
            ? new TextSelection(_selection.BaseOffset, _text.Length)
            : TextSelection.Collapsed(_text.Length);
        return UpdateSelection(nextSelection);
    }

    public bool MoveCaretToPreviousWord(bool extendSelection = false)
    {
        if (!extendSelection && !_selection.IsCollapsed)
        {
            return UpdateSelection(TextSelection.Collapsed(_selection.Start));
        }

        int nextExtentOffset = FindPreviousWordBoundary(_selection.ExtentOffset);
        var nextSelection = extendSelection
            ? new TextSelection(_selection.BaseOffset, nextExtentOffset)
            : TextSelection.Collapsed(nextExtentOffset);
        return UpdateSelection(nextSelection);
    }

    public bool MoveCaretToNextWord(bool extendSelection = false)
    {
        if (!extendSelection && !_selection.IsCollapsed)
        {
            return UpdateSelection(TextSelection.Collapsed(_selection.End));
        }

        int nextExtentOffset = FindNextWordBoundary(_selection.ExtentOffset, includeWordAfterSeparator: false);
        var nextSelection = extendSelection
            ? new TextSelection(_selection.BaseOffset, nextExtentOffset)
            : TextSelection.Collapsed(nextExtentOffset);
        return UpdateSelection(nextSelection);
    }

    public bool MoveCaretToParagraphStart(bool extendSelection = false)
    {
        if (!extendSelection && !_selection.IsCollapsed)
        {
            return UpdateSelection(TextSelection.Collapsed(_selection.Start));
        }

        int nextExtentOffset = FindParagraphStart(_selection.ExtentOffset);
        var nextSelection = extendSelection
            ? new TextSelection(_selection.BaseOffset, nextExtentOffset)
            : TextSelection.Collapsed(nextExtentOffset);
        return UpdateSelection(nextSelection);
    }

    public bool MoveCaretToParagraphEnd(bool extendSelection = false)
    {
        if (!extendSelection && !_selection.IsCollapsed)
        {
            return UpdateSelection(TextSelection.Collapsed(_selection.End));
        }

        int nextExtentOffset = FindParagraphEnd(_selection.ExtentOffset);
        var nextSelection = extendSelection
            ? new TextSelection(_selection.BaseOffset, nextExtentOffset)
            : TextSelection.Collapsed(nextExtentOffset);
        return UpdateSelection(nextSelection);
    }

    public bool Insert(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        int start = _selection.Start;
        int end = _selection.End;
        string nextText = _text[..start] + text + _text[end..];
        int caretOffset = start + text.Length;
        return ApplyAndReportChange(
            text: nextText,
            selection: TextSelection.Collapsed(caretOffset),
            composing: null);
    }

    public bool SetComposing(string text)
    {
        string composingText = text ?? string.Empty;
        int rangeStart = _composing?.Start ?? _selection.Start;
        int rangeEnd = _composing?.End ?? _selection.End;
        int clampedStart = Math.Clamp(rangeStart, 0, _text.Length);
        int clampedEnd = Math.Clamp(rangeEnd, 0, _text.Length);

        string nextText = _text[..clampedStart] + composingText + _text[clampedEnd..];
        int composingEnd = clampedStart + composingText.Length;
        var nextComposing = new TextRange(clampedStart, composingEnd);
        var collapsedSelection = TextSelection.Collapsed(composingEnd);
        return ApplyAndReportChange(nextText, collapsedSelection, nextComposing);
    }

    public bool CommitComposing(string text)
    {
        if (!_composing.HasValue)
        {
            return Insert(text);
        }

        string composingText = text ?? string.Empty;
        var currentComposing = _composing.Value.Clamp(_text.Length);
        string nextText = _text[..currentComposing.Start] + composingText + _text[currentComposing.End..];
        var collapsedSelection = TextSelection.Collapsed(currentComposing.Start + composingText.Length);
        return ApplyAndReportChange(nextText, collapsedSelection, composing: null);
    }

    public bool ClearComposing()
    {
        if (!_composing.HasValue)
        {
            return false;
        }

        return ApplyAndReportChange(_text, _selection, composing: null);
    }

    public bool DeleteBackward()
    {
        if (_selection.IsCollapsed && _selection.ExtentOffset <= 0)
        {
            return false;
        }

        int start = _selection.Start;
        int end = _selection.End;
        if (start == end)
        {
            start = FindPreviousTextElementBoundary(end);
        }

        string nextText = _text[..start] + _text[end..];
        return ApplyAndReportChange(
            text: nextText,
            selection: TextSelection.Collapsed(start),
            composing: null);
    }

    public bool DeleteBackwardByWord()
    {
        if (!_selection.IsCollapsed)
        {
            return DeleteBackward();
        }

        int end = _selection.ExtentOffset;
        if (end <= 0)
        {
            return false;
        }

        int start = FindPreviousWordBoundary(end);
        if (start >= end)
        {
            return false;
        }

        string nextText = _text[..start] + _text[end..];
        return ApplyAndReportChange(
            text: nextText,
            selection: TextSelection.Collapsed(start),
            composing: null);
    }

    public bool DeleteForward()
    {
        if (_selection.IsCollapsed && _selection.ExtentOffset >= _text.Length)
        {
            return false;
        }

        int start = _selection.Start;
        int end = _selection.End;
        if (start == end)
        {
            end = FindNextTextElementBoundary(start);
        }

        string nextText = _text[..start] + _text[end..];
        return ApplyAndReportChange(
            text: nextText,
            selection: TextSelection.Collapsed(start),
            composing: null);
    }

    public bool DeleteForwardByWord()
    {
        if (!_selection.IsCollapsed)
        {
            return DeleteForward();
        }

        int start = _selection.ExtentOffset;
        if (start >= _text.Length)
        {
            return false;
        }

        int end = FindNextWordBoundary(start, includeWordAfterSeparator: true);
        if (end <= start)
        {
            return false;
        }

        string nextText = _text[..start] + _text[end..];
        return ApplyAndReportChange(
            text: nextText,
            selection: TextSelection.Collapsed(start),
            composing: null);
    }

    public void Clear()
    {
        SetValue(
            text: string.Empty,
            selection: TextSelection.Collapsed(0),
            composing: null);
    }

    public string SelectedText
    {
        get
        {
            int start = _selection.Start;
            int end = _selection.End;
            if (start >= end)
            {
                return string.Empty;
            }

            return _text[start..end];
        }
    }

    private bool UpdateSelection(TextSelection nextSelection)
    {
        return ApplyAndReportChange(_text, nextSelection, composing: null);
    }

    private int FindPreviousWordBoundary(int offset)
    {
        int index = Math.Clamp(offset, 0, _text.Length);
        while (index > 0 && !IsWordCharacter(_text[index - 1]))
        {
            index--;
        }

        while (index > 0 && IsWordCharacter(_text[index - 1]))
        {
            index--;
        }

        return index;
    }

    private int FindNextWordBoundary(int offset, bool includeWordAfterSeparator)
    {
        int index = Math.Clamp(offset, 0, _text.Length);
        if (index >= _text.Length)
        {
            return _text.Length;
        }

        if (IsWordCharacter(_text[index]))
        {
            while (index < _text.Length && IsWordCharacter(_text[index]))
            {
                index++;
            }

            return index;
        }

        while (index < _text.Length && !IsWordCharacter(_text[index]))
        {
            index++;
        }

        if (!includeWordAfterSeparator)
        {
            return index;
        }

        while (index < _text.Length && IsWordCharacter(_text[index]))
        {
            index++;
        }

        return index;
    }

    private static bool IsWordCharacter(char character)
    {
        return char.IsLetterOrDigit(character) || character == '_';
    }

    private int FindPreviousTextElementBoundary(int offset)
    {
        int index = Math.Clamp(offset, 0, _text.Length);
        if (index <= 0 || string.IsNullOrEmpty(_text))
        {
            return 0;
        }

        int[] boundaries = StringInfo.ParseCombiningCharacters(_text);
        if (boundaries.Length == 0)
        {
            return Math.Max(0, index - 1);
        }

        int previous = 0;
        foreach (int boundary in boundaries)
        {
            if (boundary >= index)
            {
                break;
            }

            previous = boundary;
        }

        return previous;
    }

    private int FindNextTextElementBoundary(int offset)
    {
        int index = Math.Clamp(offset, 0, _text.Length);
        if (index >= _text.Length || string.IsNullOrEmpty(_text))
        {
            return _text.Length;
        }

        int[] boundaries = StringInfo.ParseCombiningCharacters(_text);
        if (boundaries.Length == 0)
        {
            return Math.Min(_text.Length, index + 1);
        }

        for (int i = 0; i < boundaries.Length; i++)
        {
            int start = boundaries[i];
            int end = i + 1 < boundaries.Length ? boundaries[i + 1] : _text.Length;
            if (index < end)
            {
                return end;
            }
        }

        return _text.Length;
    }

    private int FindParagraphStart(int offset)
    {
        int index = Math.Clamp(offset, 0, _text.Length);
        if (index <= 0)
        {
            return 0;
        }

        int searchFrom = index - 1;
        if (searchFrom >= 0 && _text[searchFrom] == '\n')
        {
            searchFrom -= 1;
        }

        if (searchFrom < 0)
        {
            return 0;
        }

        int lastNewline = _text.LastIndexOf('\n', searchFrom);
        return lastNewline < 0 ? 0 : lastNewline + 1;
    }

    private int FindParagraphEnd(int offset)
    {
        int index = Math.Clamp(offset, 0, _text.Length);
        if (index >= _text.Length)
        {
            return _text.Length;
        }

        int searchFrom = _text[index] == '\n' ? index + 1 : index;
        if (searchFrom >= _text.Length)
        {
            return _text.Length;
        }

        int nextNewline = _text.IndexOf('\n', searchFrom);
        return nextNewline < 0 ? _text.Length : nextNewline;
    }

    private bool ApplyAndReportChange(
        string text,
        TextSelection selection,
        TextRange? composing)
    {
        string previousText = _text;
        var previousSelection = _selection;
        var previousComposing = _composing;
        SetValue(text, selection, composing);
        return !string.Equals(previousText, _text, StringComparison.Ordinal)
               || !previousSelection.Equals(_selection)
               || !Nullable.Equals(previousComposing, _composing);
    }
}

/// <summary>Legacy switches controlling which editing commands appear in a text toolbar. Every
/// switch defaults to false, like Dart's deprecated <c>ToolbarOptions</c>.</summary>
public sealed record ToolbarOptions(
    bool Copy = false,
    bool Cut = false,
    bool Paste = false,
    bool SelectAll = false)
{
    /// <summary>An instance with no options set to true.</summary>
    public static ToolbarOptions Empty { get; } = new();
}

/// <summary>Configures rich-content insertion offered by the platform text input service.</summary>
public sealed record ContentInsertionConfiguration(
    IReadOnlyList<string>? AllowedMimeTypes = null,
    Action<string>? OnContentInserted = null);

public sealed partial class EditableText : StatefulWidget
{
    public EditableText(
        TextEditingController controller,
        FocusNode? focusNode = null,
        UndoHistoryController? undoController = null,
        string? placeholder = null,
        Action<string>? onChanged = null,
        bool autofocus = false,
        bool enabled = true,
        bool multiline = false,
        int? minLines = null,
        int? maxLines = null,
        bool expands = false,
        double fontSize = 14,
        Color? textColor = null,
        Color? placeholderColor = null,
        Color? backgroundColor = null,
        Color? focusedBackgroundColor = null,
        Color? cursorColor = null,
        Color? selectionColor = null,
        MouseCursor? mouseCursor = null,
        Thickness? padding = null,
        TextStyle? style = null,
        StrutStyle? strutStyle = null,
        bool readOnly = false,
        bool obscureText = false,
        string obscuringCharacter = "•",
        int? maxLength = null,
        Action? onEditingComplete = null,
        Action<string>? onSubmitted = null,
        string? semanticsLabel = null,
        TextAlign textAlign = TextAlign.Start,
        TextDirection? textDirection = null,
        TextInputType? keyboardType = null,
        TextInputActionType? textInputAction = null,
        TextCapitalization textCapitalization = TextCapitalization.None,
        SmartDashesType? smartDashesType = null,
        SmartQuotesType? smartQuotesType = null,
        Thickness? scrollPadding = null,
        ScrollController? scrollController = null,
        ScrollPhysics? scrollPhysics = null,
        bool? autocorrect = null,
        bool enableSuggestions = true,
        bool canRequestFocus = true,
        FocusOnKeyEventCallback? onKeyEvent = null,
        IReadOnlyList<TextInputFormatter>? inputFormatters = null,
        bool? showCursor = null,
        double cursorWidth = 1.0,
        double? cursorHeight = null,
        Radius? cursorRadius = null,
        BoxHeightStyle selectionHeightStyle = BoxHeightStyle.Tight,
        BoxWidthStyle selectionWidthStyle = BoxWidthStyle.Tight,
        bool cursorOpacityAnimates = false,
        Point? cursorOffset = null,
        bool paintCursorAboveText = false,
        bool enableInteractiveSelection = true,
        bool? selectAllOnFocus = null,
        ToolbarOptions? toolbarOptions = null,
        EditableTextContextMenuBuilder? contextMenuBuilder = null,
        TextMagnifierConfiguration? magnifierConfiguration = null,
        TextSelectionControls? selectionControls = null,
        bool showSelectionHandles = false,
        Action? onSelectionHandleTapped = null,
        SpellCheckConfiguration? spellCheckConfiguration = null,
        Action<TextSelection, SelectionChangedCause?>? onSelectionChanged = null,
        bool rendererIgnoresPointer = false,
        IReadOnlyList<string>? autofillHints = null,
        string? autofillHintText = null,
        IAutofillClient? autofillClient = null,
        PlatformBrightness keyboardAppearance = PlatformBrightness.Light,
        bool enableIMEPersonalizedLearning = true,
        bool? enableInlinePrediction = null,
        ContentInsertionConfiguration? contentInsertionConfiguration = null,
        bool scribbleEnabled = true,
        bool stylusHandwritingEnabled = DefaultStylusHandwritingEnabled,
        Clip clipBehavior = Clip.HardEdge,
        Color? backgroundCursorColor = null,
        Color? autocorrectionTextRectColor = null,
        ScrollBehavior? scrollBehavior = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        string? restorationId = null,
        Action<PointerDownEvent>? onTapOutside = null,
        Action<PointerUpEvent>? onTapUpOutside = null,
        object? groupId = null,
        Key? key = null) : base(key)
    {
        if (string.IsNullOrEmpty(obscuringCharacter) || obscuringCharacter.Length != 1)
            throw new ArgumentException("obscuringCharacter must contain exactly one UTF-16 character.", nameof(obscuringCharacter));
        if (maxLength.HasValue && maxLength.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength));
        if (maxLines.HasValue && maxLines.Value <= 0) throw new ArgumentOutOfRangeException(nameof(maxLines));
        if (minLines.HasValue && minLines.Value <= 0) throw new ArgumentOutOfRangeException(nameof(minLines));
        if (maxLines.HasValue && minLines.HasValue && minLines.Value > maxLines.Value)
            throw new ArgumentException("minLines cannot be greater than maxLines.", nameof(minLines));
        if (expands && (minLines.HasValue || maxLines.HasValue))
            throw new ArgumentException("minLines and maxLines must be null when expands is true.", nameof(expands));
        Controller = controller;
        FocusNode = focusNode;
        UndoController = undoController;
        Placeholder = placeholder;
        OnChanged = onChanged;
        Autofocus = autofocus;
        Enabled = enabled;
        Multiline = multiline;
        MinLines = minLines;
        MaxLines = multiline ? maxLines : 1;
        Expands = expands;
        FontSize = fontSize;
        TextColor = textColor ?? Colors.Black;
        PlaceholderColor = placeholderColor ?? Color.Parse("#FF757575");
        BackgroundColor = backgroundColor ?? Color.Parse("#FFF5F5F5");
        FocusedBackgroundColor = focusedBackgroundColor ?? Color.Parse("#FFE8F0FE");
        CursorColor = cursorColor;
        SelectionColor = selectionColor;
        MouseCursor = mouseCursor;
        Padding = padding ?? new Thickness(8, 6);
        Style = style;
        StrutStyle = strutStyle;
        ReadOnly = readOnly;
        ObscureText = obscureText;
        ObscuringCharacter = obscuringCharacter;
        MaxLength = maxLength;
        OnEditingComplete = onEditingComplete;
        OnSubmitted = onSubmitted;
        SemanticsLabel = semanticsLabel;
        TextAlign = textAlign;
        TextDirection = textDirection;
        AutofillHints = ReferenceEquals(autofillHints, AutofillDisabled) ? null : autofillHints ?? [];
        AutofillClient = autofillClient;
        KeyboardType = keyboardType ?? InferKeyboardType(AutofillHints, multiline);
        TextInputAction = textInputAction;
        TextCapitalization = textCapitalization;
        SmartDashesType = smartDashesType ?? (obscureText ? SmartDashesType.Disabled : SmartDashesType.Enabled);
        SmartQuotesType = smartQuotesType ?? (obscureText ? SmartQuotesType.Disabled : SmartQuotesType.Enabled);
        ScrollPadding = scrollPadding ?? new Thickness(20);
        ScrollController = scrollController;
        ScrollPhysics = scrollPhysics;
        Autocorrect = autocorrect ?? InferAutocorrect(AutofillHints);
        EnableSuggestions = enableSuggestions;
        CanRequestFocus = canRequestFocus;
        OnKeyEvent = onKeyEvent;
        // Dart prepends the single-line formatter to a one-line field's formatters.
        InputFormatters = MaxLines == 1
            ? [FilteringTextInputFormatter.SingleLineFormatter, .. inputFormatters ?? []]
            : inputFormatters;
        OnTapOutside = onTapOutside;
        OnTapUpOutside = onTapUpOutside;
        GroupId = groupId ?? typeof(EditableText);
        ShowCursor = showCursor;
        CursorWidth = cursorWidth;
        CursorHeight = cursorHeight;
        CursorRadius = cursorRadius ?? Radius.Zero;
        SelectionHeightStyle = selectionHeightStyle;
        SelectionWidthStyle = selectionWidthStyle;
        CursorOpacityAnimates = cursorOpacityAnimates;
        CursorOffset = cursorOffset ?? default;
        PaintCursorAboveText = paintCursorAboveText;
        EnableInteractiveSelection = enableInteractiveSelection;
        SelectAllOnFocus = selectAllOnFocus ?? DefaultSelectAllOnFocus();
        ToolbarOptions = selectionControls is ITextSelectionHandleControls && toolbarOptions is null
            ? ToolbarOptions.Empty
            : toolbarOptions ?? DefaultToolbarOptions(readOnly, obscureText);
        ContextMenuBuilder = contextMenuBuilder;
        MagnifierConfiguration = magnifierConfiguration ?? TextMagnifierConfiguration.Disabled;
        SelectionControls = selectionControls;
        ShowSelectionHandles = showSelectionHandles;
        OnSelectionHandleTapped = onSelectionHandleTapped;
        SpellCheckConfiguration = spellCheckConfiguration;
        if (spellCheckConfiguration is { SpellCheckEnabled: true, MisspelledTextStyle: null })
        {
            throw new ArgumentException(
                "An enabled spellCheckConfiguration must specify misspelledTextStyle.",
                nameof(spellCheckConfiguration));
        }
        OnSelectionChanged = onSelectionChanged;
        RendererIgnoresPointer = rendererIgnoresPointer;
        AutofillHintText = autofillHintText;
        KeyboardAppearance = keyboardAppearance;
        EnableIMEPersonalizedLearning = enableIMEPersonalizedLearning;
        EnableInlinePrediction = enableInlinePrediction;
        ContentInsertionConfiguration = contentInsertionConfiguration;
        ScribbleEnabled = scribbleEnabled;
        StylusHandwritingEnabled = stylusHandwritingEnabled;
        ClipBehavior = clipBehavior;
        BackgroundCursorColor = backgroundCursorColor;
        AutocorrectionTextRectColor = autocorrectionTextRectColor;
        ScrollBehavior = scrollBehavior;
        DragStartBehavior = dragStartBehavior;
        RestorationId = restorationId;
    }

    public TextEditingController Controller { get; }

    public FocusNode? FocusNode { get; }
    public UndoHistoryController? UndoController { get; }

    public string? Placeholder { get; }

    public Action<string>? OnChanged { get; }

    public bool Autofocus { get; }

    public bool Enabled { get; }

    public bool Multiline { get; }
    public int? MinLines { get; }
    public int? MaxLines { get; }
    public bool Expands { get; }

    public double FontSize { get; }

    public Color TextColor { get; }

    public Color PlaceholderColor { get; }

    public Color BackgroundColor { get; }

    public Color FocusedBackgroundColor { get; }
    public Color? CursorColor { get; }
    public Color? SelectionColor { get; }
    public MouseCursor? MouseCursor { get; }

    public Thickness Padding { get; }
    public TextStyle? Style { get; }
    public StrutStyle? StrutStyle { get; }
    public bool ReadOnly { get; }
    public bool ObscureText { get; }
    public string ObscuringCharacter { get; }
    public int? MaxLength { get; }
    public Action? OnEditingComplete { get; }
    public Action<string>? OnSubmitted { get; }
    public string? SemanticsLabel { get; }
    public TextAlign TextAlign { get; }
    public TextDirection? TextDirection { get; }
    public TextInputType KeyboardType { get; }
    public TextInputActionType? TextInputAction { get; }
    public TextCapitalization TextCapitalization { get; }
    public SmartDashesType SmartDashesType { get; }
    public SmartQuotesType SmartQuotesType { get; }
    public Thickness ScrollPadding { get; }
    public ScrollController? ScrollController { get; }
    public ScrollPhysics? ScrollPhysics { get; }
    public bool Autocorrect { get; }
    public bool EnableSuggestions { get; }
    public bool CanRequestFocus { get; }
    public FocusOnKeyEventCallback? OnKeyEvent { get; }

    /// <summary>Formatters applied, in order, to every user-driven edit.</summary>
    public IReadOnlyList<TextInputFormatter>? InputFormatters { get; }

    /// <summary>Called for each tap down that occurs outside of the <see cref="TextFieldTapRegion"/>
    /// group while the field is focused; null invokes <see cref="EditableTextTapOutsideIntent"/>.</summary>
    public Action<PointerDownEvent>? OnTapOutside { get; }

    /// <summary>Called for each tap up that occurs outside of the <see cref="TextFieldTapRegion"/>
    /// group after a tap down outside of it; null invokes <see cref="EditableTextTapUpOutsideIntent"/>.
    /// </summary>
    public Action<PointerUpEvent>? OnTapUpOutside { get; }

    /// <summary>The group identifier for the <see cref="TextFieldTapRegion"/> of this field.</summary>
    public object GroupId { get; }

    public bool? ShowCursor { get; }
    public double CursorWidth { get; }
    /// <summary>Overrides the caret height; <c>null</c> uses the preferred line height.</summary>
    public double? CursorHeight { get; }
    public Radius CursorRadius { get; }
    public BoxHeightStyle SelectionHeightStyle { get; }
    public BoxWidthStyle SelectionWidthStyle { get; }
    public bool CursorOpacityAnimates { get; }
    public Point CursorOffset { get; }
    public bool PaintCursorAboveText { get; }
    public bool EnableInteractiveSelection { get; }

    /// <summary>Whether to enable user interface affordances for changing the text selection.
    /// Dart's <c>selectionEnabled</c>.</summary>
    public bool SelectionEnabled => EnableInteractiveSelection;
    public bool SelectAllOnFocus { get; }
    public ToolbarOptions ToolbarOptions { get; }
    public EditableTextContextMenuBuilder? ContextMenuBuilder { get; }
    public TextMagnifierConfiguration MagnifierConfiguration { get; }
    public TextSelectionControls? SelectionControls { get; }
    public bool ShowSelectionHandles { get; }
    public SpellCheckConfiguration? SpellCheckConfiguration { get; }
    public Action<TextSelection, SelectionChangedCause?>? OnSelectionChanged { get; }
    public bool RendererIgnoresPointer { get; }

    /// <summary>
    /// A list of strings that helps the autofill service identify the type of this text input.
    /// </summary>
    /// <remarks>
    /// <c>null</c> disables autofill for this field. Dart spells that as <c>autofillHints: null</c>
    /// and defaults the parameter to <c>const &lt;String&gt;[]</c>; C# cannot express a non-null
    /// default for an optional reference parameter, so the constructor treats an omitted argument as
    /// the empty list and <see cref="AutofillDisabled"/> as Dart's <c>null</c>.
    /// </remarks>
    public IReadOnlyList<string>? AutofillHints { get; }

    /// <summary>The <see cref="IAutofillClient"/> that directs the autofill of this field, when it
    /// is not the <see cref="EditableTextState"/> itself.</summary>
    public IAutofillClient? AutofillClient { get; }
    public string? AutofillHintText { get; }
    public PlatformBrightness KeyboardAppearance { get; }
    public bool EnableIMEPersonalizedLearning { get; }
    public bool? EnableInlinePrediction { get; }
    public ContentInsertionConfiguration? ContentInsertionConfiguration { get; }
    public bool ScribbleEnabled { get; }
    public bool StylusHandwritingEnabled { get; }
    public Clip ClipBehavior { get; }

    /// <summary>The color of the regular cursor while the floating cursor is shown (Dart's
    /// <c>backgroundCursorColor</c>, typically <c>CupertinoColors.inactiveGray</c>).</summary>
    /// <remarks>Dart declares it <c>required</c>; Plumix keeps it optional like the widget's other
    /// C#-defaulted colors, and a null color paints no background caret.</remarks>
    public Color? BackgroundCursorColor { get; }

    /// <summary>The color of the rect the iOS keyboard asks for before autocorrecting a range;
    /// null disables painting it.</summary>
    public Color? AutocorrectionTextRectColor { get; }

    /// <summary>The <see cref="Widgets.ScrollBehavior"/> of the field's own <see cref="Scrollable"/>.
    /// Null copies the inherited behavior with scrollbars only when multiline and no overscroll.
    /// </summary>
    public ScrollBehavior? ScrollBehavior { get; }

    /// <summary>How the field's <see cref="Scrollable"/> starts a drag.</summary>
    public DragStartBehavior DragStartBehavior { get; }

    /// <summary>Called when the user taps on a selection handle. Dart's
    /// <c>onSelectionHandleTapped</c>.</summary>
    public Action? OnSelectionHandleTapped { get; }

    /// <summary>The default value for <see cref="StylusHandwritingEnabled"/>.</summary>
    public const bool DefaultStylusHandwritingEnabled = true;

    /// <summary>Restoration id of the field's <see cref="Scrollable"/> (its scroll offset).</summary>
    public string? RestorationId { get; }

    /// <summary>Pass as <c>autofillHints</c> to disable autofill, the way Dart passes <c>null</c>.
    /// </summary>
    public static IReadOnlyList<string> AutofillDisabled { get; } = new List<string>();

    /// <summary>Dart's <c>EditableText._inferKeyboardType</c>.</summary>
    internal static TextInputType InferKeyboardType(
        IReadOnlyList<string>? autofillHints,
        bool multiline)
    {
        if (autofillHints is null || autofillHints.Count == 0)
        {
            return multiline ? TextInputType.Multiline : TextInputType.Text;
        }

        string effectiveHint = autofillHints[0];
        if (!OperatingSystem.IsBrowser())
        {
            switch (PlatformDefaults.TargetPlatform)
            {
                case TargetPlatform.IOS:
                case TargetPlatform.MacOS:
                    if (AppleKeyboardTypes.TryGetValue(effectiveHint, out TextInputType? appleType))
                    {
                        return appleType;
                    }

                    break;
                default:
                    break;
            }
        }

        if (multiline)
        {
            return TextInputType.Multiline;
        }

        return InferredKeyboardTypes.TryGetValue(effectiveHint, out TextInputType? inferred)
            ? inferred
            : TextInputType.Text;
    }

    /// <summary>Dart's <c>EditableText._inferAutocorrect</c>.</summary>
    internal static bool InferAutocorrect(IReadOnlyList<string>? autofillHints)
    {
        if (autofillHints is null || autofillHints.Count == 0 || OperatingSystem.IsBrowser())
        {
            return true;
        }

        if (PlatformDefaults.TargetPlatform != TargetPlatform.IOS)
        {
            return true;
        }

        // username, password and newPassword are password related hints; newUsername is not
        // supported on iOS. Autocorrect is turned off so the password bar does not flash.
        bool passwordRelatedHint = autofillHints.Any(
            hint => hint is UI.AutofillHints.Username
                or UI.AutofillHints.Password
                or UI.AutofillHints.NewPassword);
        return !passwordRelatedHint;
    }

    private static readonly Dictionary<string, TextInputType> AppleKeyboardTypes = new()
    {
        [UI.AutofillHints.AddressCity] = TextInputType.Name,
        [UI.AutofillHints.AddressCityAndState] = TextInputType.Name,
        [UI.AutofillHints.AddressState] = TextInputType.Name,
        [UI.AutofillHints.CountryName] = TextInputType.Name,
        [UI.AutofillHints.CreditCardNumber] = TextInputType.Number,
        [UI.AutofillHints.Email] = TextInputType.EmailAddress,
        [UI.AutofillHints.FamilyName] = TextInputType.Name,
        [UI.AutofillHints.FullStreetAddress] = TextInputType.Name,
        [UI.AutofillHints.GivenName] = TextInputType.Name,
        [UI.AutofillHints.JobTitle] = TextInputType.Name,
        [UI.AutofillHints.Location] = TextInputType.Name,
        [UI.AutofillHints.MiddleName] = TextInputType.Name,
        [UI.AutofillHints.Name] = TextInputType.Name,
        [UI.AutofillHints.NamePrefix] = TextInputType.Name,
        [UI.AutofillHints.NameSuffix] = TextInputType.Name,
        [UI.AutofillHints.NewPassword] = TextInputType.Text,
        [UI.AutofillHints.NewUsername] = TextInputType.Text,
        [UI.AutofillHints.Nickname] = TextInputType.Name,
        [UI.AutofillHints.OneTimeCode] = TextInputType.Number,
        [UI.AutofillHints.OrganizationName] = TextInputType.Text,
        [UI.AutofillHints.Password] = TextInputType.Text,
        [UI.AutofillHints.PostalCode] = TextInputType.Name,
        [UI.AutofillHints.StreetAddressLine1] = TextInputType.Name,
        [UI.AutofillHints.StreetAddressLine2] = TextInputType.Name,
        [UI.AutofillHints.Sublocality] = TextInputType.Name,
        [UI.AutofillHints.TelephoneNumber] = TextInputType.Name,
        [UI.AutofillHints.Url] = TextInputType.Url,
        [UI.AutofillHints.Username] = TextInputType.Text,
    };

    private static readonly Dictionary<string, TextInputType> InferredKeyboardTypes = new()
    {
        [UI.AutofillHints.AddressCity] = TextInputType.StreetAddress,
        [UI.AutofillHints.AddressCityAndState] = TextInputType.StreetAddress,
        [UI.AutofillHints.AddressState] = TextInputType.StreetAddress,
        [UI.AutofillHints.Birthday] = TextInputType.Datetime,
        [UI.AutofillHints.BirthdayDay] = TextInputType.Datetime,
        [UI.AutofillHints.BirthdayMonth] = TextInputType.Datetime,
        [UI.AutofillHints.BirthdayYear] = TextInputType.Datetime,
        [UI.AutofillHints.CountryCode] = TextInputType.Number,
        [UI.AutofillHints.CountryName] = TextInputType.Text,
        [UI.AutofillHints.CreditCardExpirationDate] = TextInputType.Datetime,
        [UI.AutofillHints.CreditCardExpirationDay] = TextInputType.Datetime,
        [UI.AutofillHints.CreditCardExpirationMonth] = TextInputType.Datetime,
        [UI.AutofillHints.CreditCardExpirationYear] = TextInputType.Datetime,
        [UI.AutofillHints.CreditCardFamilyName] = TextInputType.Name,
        [UI.AutofillHints.CreditCardGivenName] = TextInputType.Name,
        [UI.AutofillHints.CreditCardMiddleName] = TextInputType.Name,
        [UI.AutofillHints.CreditCardName] = TextInputType.Name,
        [UI.AutofillHints.CreditCardNumber] = TextInputType.Number,
        [UI.AutofillHints.CreditCardSecurityCode] = TextInputType.Number,
        [UI.AutofillHints.CreditCardType] = TextInputType.Text,
        [UI.AutofillHints.Email] = TextInputType.EmailAddress,
        [UI.AutofillHints.FamilyName] = TextInputType.Name,
        [UI.AutofillHints.FullStreetAddress] = TextInputType.StreetAddress,
        [UI.AutofillHints.Gender] = TextInputType.Text,
        [UI.AutofillHints.GivenName] = TextInputType.Name,
        [UI.AutofillHints.Impp] = TextInputType.Url,
        [UI.AutofillHints.JobTitle] = TextInputType.Text,
        [UI.AutofillHints.Language] = TextInputType.Text,
        [UI.AutofillHints.Location] = TextInputType.StreetAddress,
        [UI.AutofillHints.MiddleInitial] = TextInputType.Name,
        [UI.AutofillHints.MiddleName] = TextInputType.Name,
        [UI.AutofillHints.Name] = TextInputType.Name,
        [UI.AutofillHints.NamePrefix] = TextInputType.Name,
        [UI.AutofillHints.NameSuffix] = TextInputType.Name,
        [UI.AutofillHints.NewPassword] = TextInputType.Text,
        [UI.AutofillHints.NewUsername] = TextInputType.Text,
        [UI.AutofillHints.Nickname] = TextInputType.Text,
        [UI.AutofillHints.OneTimeCode] = TextInputType.Text,
        [UI.AutofillHints.OrganizationName] = TextInputType.Text,
        [UI.AutofillHints.Password] = TextInputType.Text,
        [UI.AutofillHints.Photo] = TextInputType.Text,
        [UI.AutofillHints.PostalAddress] = TextInputType.StreetAddress,
        [UI.AutofillHints.PostalAddressExtended] = TextInputType.StreetAddress,
        [UI.AutofillHints.PostalAddressExtendedPostalCode] = TextInputType.Number,
        [UI.AutofillHints.PostalCode] = TextInputType.Number,
        [UI.AutofillHints.StreetAddressLevel1] = TextInputType.StreetAddress,
        [UI.AutofillHints.StreetAddressLevel2] = TextInputType.StreetAddress,
        [UI.AutofillHints.StreetAddressLevel3] = TextInputType.StreetAddress,
        [UI.AutofillHints.StreetAddressLevel4] = TextInputType.StreetAddress,
        [UI.AutofillHints.StreetAddressLine1] = TextInputType.StreetAddress,
        [UI.AutofillHints.StreetAddressLine2] = TextInputType.StreetAddress,
        [UI.AutofillHints.StreetAddressLine3] = TextInputType.StreetAddress,
        [UI.AutofillHints.Sublocality] = TextInputType.StreetAddress,
        [UI.AutofillHints.TelephoneNumber] = TextInputType.Phone,
        [UI.AutofillHints.TelephoneNumberAreaCode] = TextInputType.Phone,
        [UI.AutofillHints.TelephoneNumberCountryCode] = TextInputType.Phone,
        [UI.AutofillHints.TelephoneNumberDevice] = TextInputType.Phone,
        [UI.AutofillHints.TelephoneNumberExtension] = TextInputType.Phone,
        [UI.AutofillHints.TelephoneNumberLocal] = TextInputType.Phone,
        [UI.AutofillHints.TelephoneNumberLocalPrefix] = TextInputType.Phone,
        [UI.AutofillHints.TelephoneNumberLocalSuffix] = TextInputType.Phone,
        [UI.AutofillHints.TelephoneNumberNational] = TextInputType.Phone,
        [UI.AutofillHints.TransactionAmount] = TextInputType.NumberWithOptions(isDecimal: true),
        [UI.AutofillHints.TransactionCurrency] = TextInputType.Text,
        [UI.AutofillHints.Url] = TextInputType.Url,
        [UI.AutofillHints.Username] = TextInputType.Text,
    };

    public override State CreateState()
    {
        return new EditableTextState();
    }

    private static bool DefaultSelectAllOnFocus()
    {
        if (OperatingSystem.IsBrowser())
        {
            return true;
        }

        return PlatformDefaults.TargetPlatform is TargetPlatform.Linux
            or TargetPlatform.MacOS
            or TargetPlatform.Windows;
    }

    private static ToolbarOptions DefaultToolbarOptions(bool readOnly, bool obscureText)
    {
        if (obscureText)
        {
            return readOnly
                // No point in even offering "Select All" in a read-only obscured field.
                ? ToolbarOptions.Empty
                // Writable, but obscured.
                : new ToolbarOptions(SelectAll: true, Paste: true);
        }

        return readOnly
            // Read-only, not obscured.
            ? new ToolbarOptions(SelectAll: true, Copy: true)
            // Writable, not obscured.
            : new ToolbarOptions(Copy: true, Cut: true, SelectAll: true, Paste: true);
    }

    public sealed partial class EditableTextState
        : State<EditableText>, ITextSelectionDelegate, IAutofillClient, ITextInputClient, WidgetsBindingObserver
    {
        private AutofillGroupState? _currentAutofillScope;
        private TextInputConnection? _textInputConnection;

        private TextEditingController? _controller;
        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private readonly ValueNotifier<bool> _cursorVisibilityNotifier = new(false);
        private readonly GlobalKey _editableRenderKey = new EditableRenderKey(Guid.NewGuid());
        private RenderEditable? _renderEditable;
        private readonly GlobalKey<ScrollableState> _scrollableKey =
            new LabeledGlobalKey<ScrollableState>("EditableText scrollable");
        private ScrollController? _internalScrollController;
        private bool _showCaretOnScreenScheduled;
        private double _lastBottomViewInset;
        private bool _observingMetrics;
        private TextRange? _currentPromptRectRange;
        private Point? _startCaretCenter;
        private TextPosition? _lastTextPosition;
        private Point? _pointOffsetOrigin;
        private Point? _lastBoundedOffset;
        private AnimationController? _floatingCursorResetController;
        private ScrollNotificationObserverState? _scrollNotificationObserver;
        private (TextEditingValue Value, Rect SelectionBounds)? _dataWhenToolbarShowScheduled;
        private bool _listeningToScrollNotificationObserver;
        private bool _showToolbarOnScreenScheduled;
        private Orientation? _lastOrientation;
        private readonly bool _platformSupportsFadeOnScroll = PlatformDefaults.TargetPlatform switch
        {
            TargetPlatform.Android or TargetPlatform.IOS => true,
            _ => false,
        };

        private static readonly TimeSpan CaretAnimationDuration = TimeSpan.FromMilliseconds(100);
        private static readonly Curve CaretAnimationCurve = Curves.FastOutSlowIn;
        private static readonly TimeSpan FloatingCursorResetTime = TimeSpan.FromMilliseconds(125);
        private readonly LayerLink _startHandleLayerLink = new();
        private readonly LayerLink _endHandleLayerLink = new();
        private readonly LayerLink _toolbarLayerLink = new();
        private TextSelectionOverlay? _selectionOverlay;
        private SpellCheckResults? _spellCheckResults;
        private int _spellCheckRequest;

        private sealed class EditableRenderKey(Guid id) : GlobalKey
        {
            public Guid Id { get; } = id;
        }
        private bool _hadFocus;
        private bool _didAutoFocus;

        public TextSelectionToolbarAnchors ContextMenuAnchors
        {
            get
            {
                TextEditingController controller = _controller!;
                TextSelection selection = controller.Selection.Clamp(controller.Text.Length);
                Rect start = ResolveCursorRectangle(_focusNode!, controller.Text.Length, selection.Start);
                Rect end = ResolveCursorRectangle(_focusNode!, controller.Text.Length, selection.End);
                if (RenderEditable?.LastSecondaryTapDownPosition is Point secondaryTapDown)
                {
                    return new TextSelectionToolbarAnchors(secondaryTapDown);
                }

                Point primary = new(
                    (start.Center.X + end.Center.X) / 2.0,
                    Math.Min(start.Top, end.Top));
                Point secondary = new(
                    (start.Center.X + end.Center.X) / 2.0,
                    Math.Max(start.Bottom, end.Bottom));
                return new TextSelectionToolbarAnchors(primary, secondary);
            }
        }

        public bool ContextMenuIsVisible => _selectionOverlay?.ToolbarIsVisible ?? false;

        public TextEditingValue CurrentTextEditingValue => _controller!.Value;

        public SpellCheckResults? SpellCheckResults => _spellCheckResults;

        public TextSelectionOverlay? SelectionOverlay => _selectionOverlay;

        // ------------------------------------------------------------- autofill

        /// <inheritdoc/>
        public IAutofillScope? CurrentAutofillScope => _currentAutofillScope;

        /// <inheritdoc/>
        public string AutofillId => $"EditableText-{GetHashCode()}";

        /// <inheritdoc/>
        public TextInputConfiguration TextInputConfiguration
        {
            get
            {
                IReadOnlyList<string>? autofillHints = Widget.AutofillHints;
                AutofillConfiguration autofillConfiguration = autofillHints != null
                    ? new AutofillConfiguration(
                        uniqueIdentifier: AutofillId,
                        autofillHints: autofillHints,
                        currentEditingValue: CurrentTextEditingValue,
                        hintText: Widget.AutofillHintText)
                    : AutofillConfiguration.Disabled;
                return new TextInputConfiguration(
                    inputType: Widget.KeyboardType,
                    readOnly: Widget.ReadOnly,
                    obscureText: Widget.ObscureText,
                    autocorrect: Widget.Autocorrect,
                    smartDashesType: Widget.SmartDashesType,
                    smartQuotesType: Widget.SmartQuotesType,
                    enableSuggestions: Widget.EnableSuggestions,
                    enableInteractiveSelection: Widget.EnableInteractiveSelection,
                    inputAction: Widget.TextInputAction
                                 ?? (Equals(Widget.KeyboardType, TextInputType.Multiline)
                                     ? TextInputActionType.Newline
                                     : TextInputActionType.Done),
                    textCapitalization: Widget.TextCapitalization,
                    keyboardAppearance: Widget.KeyboardAppearance,
                    autofillConfiguration: autofillConfiguration,
                    enableIMEPersonalizedLearning: Widget.EnableIMEPersonalizedLearning,
                    enableInlinePrediction: Widget.EnableInlinePrediction);
            }
        }

        /// <inheritdoc/>
        public void Autofill(TextEditingValue newEditingValue) => UpdateEditingValue(newEditingValue);

        /// <inheritdoc/>
        public void PerformAction(TextInputActionType action)
        {
            switch (action)
            {
                case TextInputActionType.Newline:
                    // If this is a multiline EditableText, do nothing for a "newline" action; the
                    // newline is already inserted. Otherwise, finalize editing.
                    if (!Widget.Multiline)
                    {
                        FinalizeEditing(action, shouldUnfocus: true);
                    }

                    break;
                case TextInputActionType.Done:
                case TextInputActionType.Go:
                case TextInputActionType.Next:
                case TextInputActionType.Previous:
                case TextInputActionType.Search:
                case TextInputActionType.Send:
                    FinalizeEditing(action, shouldUnfocus: true);
                    break;
                case TextInputActionType.ContinueAction:
                case TextInputActionType.EmergencyCall:
                case TextInputActionType.Join:
                case TextInputActionType.None:
                case TextInputActionType.Route:
                case TextInputActionType.Unspecified:
                    // Finalize editing, but don't give up focus because this keyboard action does
                    // not imply the user is done inputting information.
                    FinalizeEditing(action, shouldUnfocus: false);
                    break;
            }
        }

        private void FinalizeEditing(TextInputActionType action, bool shouldUnfocus)
        {
            // Take any actions necessary now that the user has completed editing.
            if (Widget.OnEditingComplete is { } onEditingComplete)
            {
                try
                {
                    onEditingComplete();
                }
                catch (Exception exception)
                {
                    FlutterError.ReportError(new FlutterErrorDetails(
                        exception: exception,
                        stack: exception.StackTrace,
                        library: "widgets",
                        context: new ErrorDescription($"while calling onEditingComplete for {action}")));
                }
            }
            else
            {
                // Default behavior if the developer did not provide an onEditingComplete callback:
                // finalize editing and remove focus, or move it to the next/previous field.
                _ = _controller!.ClearComposing();
                if (shouldUnfocus)
                {
                    switch (action)
                    {
                        case TextInputActionType.Next:
                            _ = _focusNode!.NextFocus();
                            break;
                        case TextInputActionType.Previous:
                            _ = _focusNode!.PreviousFocus();
                            break;
                        default:
                            _focusNode!.Unfocus();
                            break;
                    }
                }
            }

            Action<string>? onSubmitted = Widget.OnSubmitted;
            if (onSubmitted is null)
            {
                return;
            }

            // Invoke optional callback with the user's submitted content.
            try
            {
                onSubmitted(_controller!.Text);
            }
            catch (Exception exception)
            {
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    stack: exception.StackTrace,
                    library: "widgets",
                    context: new ErrorDescription($"while calling onSubmitted for {action}")));
            }

            // If `shouldUnfocus` is true, the text field should no longer be focused after the
            // microtask queue is drained. But in case the developer cancelled the focus change in
            // the `onSubmitted` callback by focusing this input field again, reset the soft keyboard.
            if (shouldUnfocus)
            {
                ScheduleRestartConnection();
            }
        }

        private void ScheduleRestartConnection()
        {
            if (_restartConnectionScheduled)
            {
                return;
            }

            _restartConnectionScheduled = true;
            Scheduler.ScheduleMicrotask(RestartConnectionIfNeeded);
        }

        // Discards the current TextInputConnection and establishes a new one, so the platform's
        // input control is reset.
        private void RestartConnectionIfNeeded()
        {
            _restartConnectionScheduled = false;
            if (!HasInputConnection || !ShouldCreateInputConnection)
            {
                return;
            }

            _textInputConnection!.Close();
            _textInputConnection = null;
            OpenInputConnection();
        }


        /// <inheritdoc/>
        public void ConnectionClosed()
        {
            if (HasInputConnection)
            {
                _textInputConnection!.ConnectionClosedReceived();
                _textInputConnection = null;
                _lastKnownRemoteTextEditingValue = null;
                _focusNode!.Unfocus();
            }
        }

        /// <inheritdoc/>
        TextEditingValue? ITextInputClient.CurrentTextEditingValue =>
            _controller is null ? null : _controller.Value;

        /// <inheritdoc/>
        /// <remarks>Plumix has no <c>ProcessTextService</c>-style private command surface, so this
        /// is a no-op the way Flutter's own <c>EditableText</c> leaves it.</remarks>
        public void PerformPrivateCommand(string action, IDictionary data)
        {
        }

        /// <inheritdoc/>
        void ITextInputClient.ShowToolbar() => ShowToolbar();

        private IAutofillClient EffectiveAutofillClient => Widget.AutofillClient ?? this;

        private bool NeedsAutofill =>
            EffectiveAutofillClient.TextInputConfiguration.AutofillConfiguration.Enabled;

        private void OpenInputConnection()
        {
            if (!ShouldCreateInputConnection)
            {
                return;
            }

            if (_textInputConnection is { Attached: true })
            {
                _textInputConnection.Show();
                return;
            }

            TextInputConfiguration configuration = EffectiveAutofillClient.TextInputConfiguration;
            _textInputConnection = NeedsAutofill && _currentAutofillScope != null
                ? _currentAutofillScope.Attach(this, configuration)
                : UI.TextInput.Attach(this, configuration);
            if (RenderEditable is { HasSize: true, Attached: true })
            {
                UpdateSizeAndTransform();
            }

            SchedulePeriodicPostFrameCallbacks();
            TextEditingValue localValue = CurrentTextEditingValue;
            _lastKnownRemoteTextEditingValue = localValue;
            _textInputConnection.SetEditingState(localValue);
            _textInputConnection.Show();
            if (NeedsAutofill)
            {
                _textInputConnection.RequestAutofill();
            }
        }

        private void CloseInputConnection()
        {
            _textInputConnection?.Close();
            _textInputConnection = null;
            _lastKnownRemoteTextEditingValue = null;
            _scribbleCacheKey = null;
            RemoveTextPlaceholder();
        }

        private void UpdateAutofillRegistration()
        {
            AutofillGroupState? newAutofillGroup = AutofillGroup.MaybeOf(Context);
            if (ReferenceEquals(_currentAutofillScope, newAutofillGroup))
            {
                return;
            }

            _currentAutofillScope?.Unregister(AutofillId);
            _currentAutofillScope = newAutofillGroup;
            _currentAutofillScope?.Register(EffectiveAutofillClient);
        }

        public override void DidChangeDependencies()
        {
            base.DidChangeDependencies();
            UpdateAutofillRegistration();

            if (!_didAutoFocus && Widget.Autofocus)
            {
                // Dart autofocuses from a post-frame callback after `_flagInternalFocus()`; the
                // Focus widget below autofocuses the node, so only the flag is set here (see
                // docs/ai/DIVERGENCES.md).
                _didAutoFocus = true;
                FlagInternalFocus();
            }

            DidChangeDependenciesForCursor();
            DidChangeDependenciesForContextMenu();
        }

        public override void InitState()
        {
            _liveTextInputStatus?.AddListener(OnChangedLiveTextInputStatus);
            ClipboardStatus.AddListener(OnChangedClipboardStatus);
            AttachController(Widget.Controller);
            AttachFocusNode(Widget.FocusNode);
            _appLifecycleListener = new AppLifecycleListener(onResume: OnResume);
        }

        public override void DidUpdateWidget(EditableText oldWidget)
        {
            var oldEditableText = (EditableText)oldWidget;
            if (!ReferenceEquals(oldEditableText.Controller, Widget.Controller))
            {
                DetachController();
                AttachController(Widget.Controller);
            }

            if (!ReferenceEquals(oldEditableText.FocusNode, Widget.FocusNode))
            {
                DetachFocusNode(disposeOwned: true);
                AttachFocusNode(Widget.FocusNode);
            }
            if (!ReferenceEquals(oldEditableText.AutofillClient, Widget.AutofillClient))
            {
                _currentAutofillScope?.Unregister(oldEditableText.AutofillClient?.AutofillId ?? AutofillId);
                _currentAutofillScope?.Register(EffectiveAutofillClient);
            }

            DidUpdateWidgetForSelectionOverlay(oldEditableText);
            if (oldEditableText.ShowCursor != Widget.ShowCursor)
            {
                StartOrStopCursorTimerIfNeeded();
            }

            UpdateClipboardStatusAfterWidgetUpdate();

            if (PlatformDefaults.IsWeb && HasInputConnection && oldEditableText.ReadOnly != Widget.ReadOnly)
            {
                _textInputConnection!.UpdateConfig(EffectiveAutofillClient.TextInputConfiguration);
            }

            if (HasInputConnection)
            {
                bool obscureTextChanged = oldEditableText.ObscureText != Widget.ObscureText;
                if (obscureTextChanged || !Equals(oldEditableText.KeyboardType, Widget.KeyboardType))
                {
                    if (obscureTextChanged)
                    {
                        // When obscureText is toggled, reset its state so the last character is not
                        // visible between the state changes.
                        _obscureShowCharTicksPending = 0;
                        _obscureLatestCharIndex = null;
                    }

                    _textInputConnection!.UpdateConfig(EffectiveAutofillClient.TextInputConfiguration);
                }
            }

            // Spell check is re-inferred whenever an input that feeds IsPasswordInput changes.
            if (oldEditableText.SpellCheckConfiguration != Widget.SpellCheckConfiguration
                || oldEditableText.ObscureText != Widget.ObscureText
                || !Equals(oldEditableText.KeyboardType, Widget.KeyboardType)
                || !AutofillHintsEqual(oldEditableText.AutofillHints, Widget.AutofillHints))
            {
                if (SpellCheckEnabled)
                {
                    if (!string.IsNullOrEmpty(TextEditingValue.Text))
                    {
                        Scheduler.RunAsync(() => RequestSpellCheckAsync());
                    }
                }
                else
                {
                    _spellCheckResults = null;
                }
            }
        }

        private static bool AutofillHintsEqual(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            return a is not null && b is not null && a.SequenceEqual(b, StringComparer.Ordinal);
        }

        public override void Dispose()
        {
            FocusManager.Instance.RemoveListener(UnflagInternalFocus);
            _internalScrollController?.Dispose();
            _floatingCursorResetController?.Dispose();
            _floatingCursorResetController = null;
            CloseInputConnection();
            _currentAutofillScope?.Unregister(AutofillId);
            _currentAutofillScope = null;
            _selectionOverlay?.Dispose();
            _selectionOverlay = null;
            _liveTextInputStatus?.RemoveListener(OnChangedLiveTextInputStatus);
            _liveTextInputStatus?.Dispose();
            ClipboardStatus.RemoveListener(OnChangedClipboardStatus);
            ClipboardStatus.Dispose();
            _appLifecycleListener?.Dispose();
            FocusManager.Instance.RemoveListener(ResetJustResumed);
            DetachController();
            DetachFocusNode(disposeOwned: true);
            _cursorTimer?.Cancel();
            _cursorTimer = null;
            _backingCursorBlinkOpacityController?.Dispose();
            StopObservingMetrics();
            _cursorVisibilityNotifier.Dispose();
            DisposeScrollNotificationObserver();

            base.Dispose();
        }

        /// Whether the field shows its <see cref="EditableText.Placeholder"/> instead of its text.
        private bool ShowPlaceholder =>
            string.IsNullOrEmpty(_controller!.Text) && !string.IsNullOrEmpty(Widget.Placeholder);

        /// The style the text is laid out with: <see cref="EditableText.Style"/> over the widget's
        /// C#-only font size and text color.
        private TextStyle EffectiveTextStyle(bool showPlaceholder)
        {
            TextStyle style = Widget.Style ?? new TextStyle();
            return style.CopyWith(
                fontSize: style.FontSize ?? Widget.FontSize,
                color: showPlaceholder ? Widget.PlaceholderColor : style.Color ?? Widget.TextColor);
        }

        /// Builds the text span the editable displays. Dart's `EditableTextState.buildTextSpan`:
        /// obscured text is replaced by the obscuring character, received spell check results style the
        /// misspelled words, and otherwise the controller builds the span, underlining the composing
        /// region while the field is focused and editable. A shown placeholder is C#-only.
        public TextSpan BuildTextSpan() => BuildTextSpan(ShowPlaceholder);

        private TextSpan BuildTextSpan(bool showPlaceholder)
        {
            TextStyle style = EffectiveTextStyle(showPlaceholder);
            if (showPlaceholder)
            {
                return new TextSpan(style: style, text: Widget.Placeholder);
            }

            TextEditingValue value = _controller!.Value;
            if (Widget.ObscureText)
            {
                return new TextSpan(style: style, text: BuildObscuredText(value.Text));
            }

            if (BuildTextSpanWithScribblePlaceholder(style) is { } placeholderSpan)
            {
                return placeholderSpan;
            }

            bool withComposing = !Widget.ReadOnly && _focusNode!.HasFocus;
            if (_spellCheckResults is { } spellCheckResults
                && Widget.SpellCheckConfiguration?.MisspelledTextStyle is { } misspelledTextStyle)
            {
                bool composingRegionOutOfRange = !value.IsComposingRangeValid || !withComposing;
                return SpellCheckTextSpans.BuildTextSpanWithSpellCheckSuggestions(
                    value,
                    composingRegionOutOfRange,
                    style,
                    misspelledTextStyle,
                    spellCheckResults);
            }

            return _controller.BuildTextSpan(Context, style, withComposing);
        }

        /// Dart's `EditableTextState._cursorColor`: the cursor color faded by the blink opacity.
        private Color CursorColor(DefaultSelectionStyle selectionStyle)
        {
            Color cursorColor = Widget.CursorColor ?? selectionStyle.CursorColor ?? Widget.TextColor;
            _resolvedCursorColor = cursorColor;
            double effectiveOpacity = Math.Min(cursorColor.A / 255.0, CursorBlinkOpacityController.Value);
            return cursorColor.WithOpacity(effectiveOpacity);
        }

        public override Widget Build(BuildContext context)
        {
            DefaultSelectionStyle selectionStyle = DefaultSelectionStyle.Of(context);
            bool showPlaceholder = ShowPlaceholder;
            var backgroundColor = _focusNode!.HasFocus ? Widget.FocusedBackgroundColor : Widget.BackgroundColor;
            TextStyle style = EffectiveTextStyle(showPlaceholder);
            UpdateCursorVisibility();

            Widget BuildEditable(ViewportOffset offset) => new EditableRenderObjectWidget(
                key: _editableRenderKey,
                inlineSpan: BuildTextSpan(showPlaceholder),
                value: showPlaceholder
                    ? new TextEditingValue(selection: TextSelection.Collapsed(0))
                    : _controller!.Value,
                startHandleLayerLink: _startHandleLayerLink,
                endHandleLayerLink: _endHandleLayerLink,
                cursorColor: CursorColor(selectionStyle),
                showCursor: _cursorVisibilityNotifier,
                forceLine: true,
                readOnly: Widget.ReadOnly,
                hasFocus: _focusNode.HasFocus,
                maxLines: Widget.MaxLines,
                minLines: Widget.MinLines,
                expands: Widget.Expands,
                strutStyle: Widget.StrutStyle?.InheritFromTextStyle(style)
                            ?? StrutStyle.FromTextStyle(style, forceStrutHeight: true),
                selectionColor: Widget.SelectionColor ?? selectionStyle.SelectionColor,
                textScaler: MediaQuery.TextScalerOf(context),
                textAlign: Widget.TextAlign,
                textDirection: Widget.TextDirection ?? Directionality.Of(context),
                obscuringCharacter: Widget.ObscuringCharacter,
                obscureText: Widget.ObscureText,
                offset: offset,
                rendererIgnoresPointer: Widget.RendererIgnoresPointer,
                cursorWidth: Widget.CursorWidth,
                cursorHeight: Widget.CursorHeight,
                cursorRadius: Widget.CursorRadius == default ? null : Widget.CursorRadius,
                cursorOffset: Widget.CursorOffset,
                paintCursorAboveText: Widget.PaintCursorAboveText,
                selectionHeightStyle: Widget.SelectionHeightStyle,
                selectionWidthStyle: Widget.SelectionWidthStyle,
                enableInteractiveSelection: Widget.EnableInteractiveSelection,
                textSelectionDelegate: this,
                devicePixelRatio: MediaQuery.MaybeDevicePixelRatioOf(context) ?? 1.0,
                clipBehavior: Widget.ClipBehavior,
                backgroundCursorColor: Widget.BackgroundCursorColor,
                promptRectRange: _currentPromptRectRange,
                promptRectColor: Widget.AutocorrectionTextRectColor);

            Widget BuildViewport(ViewportOffset offset) => new CompositedTransformTarget(
                link: _toolbarLayerLink,
                child: new Semantics(
                    onCopy: SemanticsOnCopy(Widget.SelectionControls),
                    onCut: SemanticsOnCut(Widget.SelectionControls),
                    onPaste: SemanticsOnPaste(Widget.SelectionControls),
                    child: new ScribbleFocusable(
                        editableKey: _editableRenderKey,
                        enabled: StylusHandwritingEnabled,
                        focusNode: _focusNode,
                        updateSelectionRects: () =>
                        {
                            OpenInputConnection();
                            UpdateSelectionRects(force: true);
                        },
                        child: new SizeChangedLayoutNotifier(
                            child: BuildEditable(offset)))));

            return new EditableTextCompositionCallback(
                compositeCallback: CompositeCallback,
                enabled: HasInputConnection,
                child: new Actions(
                    actions: ActionsMap,
                    child: new Builder(builder: actionsContext => new TextFieldTapRegion(
                        groupId: Widget.GroupId,
                        onTapOutside: _focusNode.HasFocus
                            ? @event => OnTapOutside(actionsContext, @event)
                            : null,
                        onTapUpOutside: @event => OnTapUpOutside(actionsContext, @event),
                        debugLabel: Constants.KReleaseMode ? null : "EditableText",
                        child: new MouseRegion(
                            cursor: Widget.MouseCursor
                                    ?? selectionStyle.MouseCursor
                                    ?? SystemMouseCursors.Text,
                            child: new UndoHistory<TextEditingValue>(
                                value: Widget.Controller,
                                onTriggered: value =>
                                    UserUpdateTextEditingValue(value, SelectionChangedCause.Keyboard),
                                shouldChangeUndoStack: ShouldChangeUndoStack,
                                undoStackModifier: value =>
                                    PlatformDefaults.TargetPlatform == TargetPlatform.Android
                                        ? new TextEditingValue(value.Text, value.Selection)
                                        : value,
                                focusNode: _focusNode,
                                controller: Widget.UndoController,
                                child: new Semantics(
                                    label: Widget.SemanticsLabel,
                                    textField: true,
                                    enabled: Widget.Enabled ? true : null,
                                    focused: _focusNode.HasFocus ? true : null,
                                    onTap: Widget.Enabled ? () => _focusNode.RequestFocus() : null,
                                    child: new Focus(
                                        focusNode: _focusNode,
                                        includeSemantics: false,
                                        autofocus: Widget.Autofocus,
                                        canRequestFocus: Widget.Enabled && Widget.CanRequestFocus,
                                        onKeyEvent: Widget.OnKeyEvent,
                                        onTextInput: HandleTextInput,
                                        onTextComposition: HandleTextComposition,
                                        onTextInputState: HandleTextInputState,
                                        onTextSelectionChanged: HandleTextSelectionChanged,
                                        debugLabel: Constants.KReleaseMode ? null : "EditableText",
                                        child: new Container(
                                            color: backgroundColor,
                                            padding: Widget.Padding,
                                            child: BuildScrollable(context, BuildViewport))))))))));
        }

        /// Dart's `shouldChangeUndoStack` for the text field's `UndoHistory`.
        private bool ShouldChangeUndoStack(TextEditingValue oldValue, TextEditingValue newValue)
        {
            if (!newValue.Selection.IsValid)
            {
                return false;
            }

            // `UndoHistory` hands a value type's missing previous value over as `default`.
            if (oldValue.Equals(default(TextEditingValue)))
            {
                return true;
            }

            switch (PlatformDefaults.TargetPlatform)
            {
                case TargetPlatform.IOS:
                case TargetPlatform.MacOS:
                case TargetPlatform.Fuchsia:
                case TargetPlatform.Linux:
                case TargetPlatform.Windows:
                    // Composing text is not counted in history coalescing.
                    if (_controller!.Value.Composing is { IsCollapsed: false })
                    {
                        return false;
                    }

                    break;
                case TargetPlatform.Android:
                    // Gboard on Android puts non-CJK words in composing regions. Coalesce composing
                    // text in order to allow the saving of partial words in that case.
                    break;
            }

            return !string.Equals(oldValue.Text, newValue.Text, StringComparison.Ordinal)
                   || !Nullable.Equals(oldValue.Composing, newValue.Composing);
        }

        /// <summary>Express interest in interacting with the keyboard. Dart's
        /// <c>requestKeyboard</c>: opens the input connection of a focused field, or focuses it.</summary>
        public void RequestKeyboard()
        {
            if (_focusNode!.HasFocus)
            {
                OpenInputConnection();
            }
            else if (Widget.Enabled && Widget.CanRequestFocus)
            {
                FlagInternalFocus();
                _focusNode.RequestFocus();
            }
        }

        public TextEditingValue TextEditingValue => _controller!.Value;

        private void AttachController(TextEditingController controller)
        {
            _controller = controller;
            _controller.AddListener(DidChangeTextEditingValue);
        }

        private void DetachController()
        {
            if (_controller == null)
            {
                return;
            }

            _controller.RemoveListener(DidChangeTextEditingValue);
            _controller = null;
        }

        private void AttachFocusNode(FocusNode? externalNode)
        {
            _focusNode = externalNode ?? new FocusNode();
            _ownsFocusNode = externalNode is null;
            _hadFocus = _focusNode.HasFocus;
            _focusNode.AddListener(HandleFocusNodeChanged);
        }

        private void DetachFocusNode(bool disposeOwned)
        {
            if (_focusNode == null)
            {
                return;
            }

            _focusNode.RemoveListener(HandleFocusNodeChanged);

            if (_ownsFocusNode)
            {
                _focusNode.OnKeyEvent = null;
                _focusNode.OnTextInput = null;
                _focusNode.OnTextComposition = null;
                _focusNode.OnTextInputState = null;
                _focusNode.OnTextSelectionChanged = null;
            }

            if (disposeOwned && _ownsFocusNode)
            {
                _focusNode.Dispose();
            }

            _focusNode = null;
            _ownsFocusNode = false;
        }

        // The host adapter delivers typed text and IME composition through the focus node (see the
        // `TextInputConnection` row in `docs/ai/DIVERGENCES.md`). Each handler plays the platform
        // text input plugin: it computes the platform's next editing state and sends it through
        // `UpdateEditingValue`, exactly like an inbound `TextInputClient.updateEditingState`.
        private bool HandleTextInput(FocusNode node, string text)
        {
            if (!Widget.Enabled || Widget.ReadOnly || string.IsNullOrEmpty(text))
            {
                return false;
            }

            string normalizedInput = Widget.Multiline
                ? text
                : text.Replace("\r", string.Empty, StringComparison.Ordinal)
                    .Replace("\n", string.Empty, StringComparison.Ordinal);
            normalizedInput = LimitInsertion(normalizedInput);
            if (string.IsNullOrEmpty(normalizedInput))
            {
                return false;
            }

            return SendPlatformEdit(controller => controller.Composing.HasValue
                ? controller.CommitComposing(normalizedInput)
                : controller.Insert(normalizedInput));
        }

        private bool HandleTextComposition(FocusNode node, string text, bool isCommit)
        {
            if (!Widget.Enabled || Widget.ReadOnly)
            {
                return false;
            }

            string limitedText = LimitInsertion(text);
            return SendPlatformEdit(controller => isCommit
                ? controller.CommitComposing(limitedText)
                : controller.SetComposing(limitedText));
        }

        /// Applies <paramref name="edit"/> to a copy of the current value, as the platform's own
        /// editing model would, and sends the result through <see cref="UpdateEditingValue"/>.
        private bool SendPlatformEdit(Func<TextEditingController, bool> edit)
        {
            TextEditingValue before = EditingValue;
            using var platformModel = TextEditingController.FromValue(before);
            if (!edit(platformModel))
            {
                return false;
            }

            UpdateEditingValue(platformModel.Value);
            return !EditingValue.Equals(before);
        }

        private FocusTextInputState? HandleTextInputState(FocusNode node)
        {
            var controller = _controller!;
            string text = controller.Text;
            var selection = controller.Selection.Clamp(text.Length);
            var cursorRectangle = ResolveCursorRectangle(node, text.Length, selection.ExtentOffset);
            return new FocusTextInputState(
                SurroundingText: text,
                SelectionBaseOffset: selection.BaseOffset,
                SelectionExtentOffset: selection.ExtentOffset,
                CursorRectangle: cursorRectangle,
                Configuration: TextInputConfiguration);
        }

        private bool HandleTextSelectionChanged(FocusNode node, int baseOffset, int extentOffset)
        {
            if (!Widget.Enabled)
            {
                return false;
            }

            TextEditingValue value = EditingValue;
            int textLength = value.Text.Length;
            var nextSelection = new TextSelection(
                BaseOffset: Math.Clamp(baseOffset, 0, textLength),
                ExtentOffset: Math.Clamp(extentOffset, 0, textLength));
            if (value.Selection.Equals(nextSelection))
            {
                return false;
            }

            // The platform IME moved the selection: Dart's `updateEditingValue`.
            UpdateEditingValue(value.CopyWith(selection: nextSelection));
            return !value.Selection.Equals(EditingValue.Selection);
        }

        /// The caret rect of <paramref name="caretOffset"/> in root coordinates, for the IME and the
        /// context menu anchors. Dart reads it from `renderEditable.getLocalRectForCaret` and the
        /// editable's transform (`_updateCaretRectIfNeeded`, `contextMenuAnchors`).
        private Rect ResolveCursorRectangle(FocusNode node, int textLength, int caretOffset)
        {
            int clampedCaretOffset = Math.Clamp(caretOffset, 0, textLength);
            if (RenderEditable is { HasSize: true } renderEditable)
            {
                Rect localRect = renderEditable.GetLocalRectForCaret(new TextPosition(clampedCaretOffset));
                Matrix4 transform = renderEditable.TryGetTransformFromRoot(out Matrix4 value)
                    ? value
                    : Matrix4.Identity();
                return RenderObject.TransformRect(transform, localRect);
            }

            // Not laid out yet: the field's own rect, inset by its padding, stands in for the caret.
            Rect fieldRect = node.ResolveTraversalRect() ?? new Rect(0, 0, 1, 1);
            return new Rect(
                fieldRect.X + Widget.Padding.Left,
                fieldRect.Y + Widget.Padding.Top,
                1,
                Math.Max(1, fieldRect.Height - Widget.Padding.Top - Widget.Padding.Bottom));
        }

        private void HandleFocusNodeChanged()
        {
            bool hasFocus = _focusNode?.HasFocus == true;
            StartOrStopCursorTimerIfNeeded();
            if (hasFocus)
            {
                OpenInputConnection();
                StartObservingMetrics();
                if (!Widget.ReadOnly)
                {
                    ScheduleShowCaretOnScreen(withAnimation: true);
                }

                if (!_hadFocus && AdjustedSelectionWhenFocused() is { } updatedSelection)
                {
                    HandleSelectionChanged(updatedSelection, null);
                }
            }
            else
            {
                _selectionOverlay?.Hide();
                CloseInputConnection();
                StopObservingMetrics();
                _currentPromptRectRange = null;
            }
            _hadFocus = hasFocus;
            SetState(static () => { });
        }

        // Whether the next focus change comes from this field itself (a tap, `requestKeyboard`),
        // which must not select the text the way a focus traversal does.
        private bool _nextFocusChangeIsInternal;

        /// Dart's `_flagInternalFocus`: flags the next focus change as internal until the focus
        /// manager reports it.
        private void FlagInternalFocus()
        {
            _nextFocusChangeIsInternal = true;
            FocusManager.Instance.AddListener(UnflagInternalFocus);
        }

        private void UnflagInternalFocus()
        {
            _nextFocusChangeIsInternal = false;
            FocusManager.Instance.RemoveListener(UnflagInternalFocus);
        }

        /// Dart's `_adjustedSelectionWhenFocused`: a focus traversal into a one-line field selects
        /// all of it where the platform does, unless the focus comes back after the app resumed; an
        /// invalid selection becomes a caret at the end.
        private TextSelection? AdjustedSelectionWhenFocused()
        {
            TextSelection? selection = null;
            bool shouldSelectAll = Widget.SelectAllOnFocus
                                   && Widget.SelectionEnabled
                                   && !Widget.Multiline
                                   && !_nextFocusChangeIsInternal
                                   && !_justResumed;
            // Reset _justResumed as soon as it has been consumed.
            _justResumed = false;
            if (shouldSelectAll)
            {
                // On native web and desktop platforms, single line <input> tags select all when
                // receiving focus.
                selection = new TextSelection(0, EditingValue.Text.Length);
            }
            else if (!EditingValue.Selection.IsValid)
            {
                // Place cursor at the end if the selection is invalid when we receive focus.
                selection = TextSelection.Collapsed(EditingValue.Text.Length);
            }

            return selection;
        }

        private string LimitInsertion(string insertion)
        {
            if (!Widget.MaxLength.HasValue || string.IsNullOrEmpty(insertion)) return insertion;
            var controller = _controller!;
            var selection = controller.Selection.Clamp(controller.Text.Length);
            string retained = controller.Text.Remove(selection.Start, selection.End - selection.Start);
            int remaining = Math.Max(0, Widget.MaxLength.Value - new StringInfo(retained).LengthInTextElements);
            if (new StringInfo(insertion).LengthInTextElements <= remaining) return insertion;
            if (remaining == 0) return string.Empty;
            var enumerator = StringInfo.GetTextElementEnumerator(insertion);
            int end = 0;
            for (int index = 0; index < remaining && enumerator.MoveNext(); index++)
                end = enumerator.ElementIndex + enumerator.GetTextElement().Length;
            return insertion[..end];
        }

        private static string BuildDisplayText(
            string text,
            bool showPlaceholder,
            string? placeholder,
            bool hasFocus,
            TextSelection selection,
            TextRange? composing)
        {
            if (showPlaceholder)
            {
                return placeholder ?? string.Empty;
            }

            if (!hasFocus)
            {
                return text;
            }

            if (composing.HasValue)
            {
                var composingRange = composing.Value.Clamp(text.Length);
                if (!composingRange.IsCollapsed)
                {
                    return text[..composingRange.Start] + "{" + text[composingRange.Start..composingRange.End] + "}" + text[composingRange.End..];
                }
            }

            var clampedSelection = selection.Clamp(text.Length);
            if (clampedSelection.IsCollapsed)
            {
                int caretOffset = clampedSelection.ExtentOffset;
                return text[..caretOffset] + "|" + text[caretOffset..];
            }

            int start = clampedSelection.Start;
            int end = clampedSelection.End;
            return text[..start] + "[" + text[start..end] + "]" + text[end..];
        }

        public SuggestionSpan? FindSuggestionSpanAtCursorIndex(int cursorIndex)
        {
            return _spellCheckResults?.SuggestionSpans.FirstOrDefault(span =>
                span.Range.Start <= cursorIndex && span.Range.End >= cursorIndex);
        }

        public void ReplaceText(TextRange range, string replacement)
        {
            TextRange normalized = range.Clamp(_controller!.Text.Length);
            string next = _controller.Text[..normalized.Start] + replacement + _controller.Text[normalized.End..];
            UserUpdateTextEditingValue(
                new TextEditingValue(next, TextSelection.Collapsed(normalized.Start + replacement.Length)),
                SelectionChangedCause.Toolbar);
            HideToolbar();
        }

        /// Dart's `late final renderEditable`: found once through the editable's key, then kept, so a
        /// callback that runs while the element is inactive (the floating cursor's reset tick, a
        /// post-frame callback) still reaches it.
        private RenderEditable? RenderEditable =>
            _renderEditable ??= _editableRenderKey.CurrentContext?.FindRenderObject() as RenderEditable;

        /// Dart's `EditableText._isPasswordInput`: spell check never runs on password input,
        /// whatever the [SpellCheckConfiguration] says.
        private bool IsPasswordInput =>
            Widget.ObscureText
            || Equals(Widget.KeyboardType, TextInputType.VisiblePassword)
            || Widget.AutofillHints?.Any(
                hint => hint is UI.AutofillHints.Password or UI.AutofillHints.NewPassword) == true;

        /// Whether spell check is enabled for this field. Dart resolves this once through
        /// `EditableText._inferSpellCheckConfiguration`.
        public bool SpellCheckEnabled =>
            Widget.SpellCheckConfiguration is { SpellCheckEnabled: true } && !IsPasswordInput;

        private async Task RequestSpellCheckAsync(string? text = null)
        {
            SpellCheckConfiguration? configuration = Widget.SpellCheckConfiguration;
            text ??= _controller!.Text;
            if (!SpellCheckEnabled || string.IsNullOrEmpty(text)) return;
            ISpellCheckService? service = configuration!.SpellCheckService;
            if (service is null && DefaultSpellCheckService.PlatformHandler is not null)
            {
                service = new DefaultSpellCheckService();
            }
            if (service is null) return;
            int request = ++_spellCheckRequest;
            Locale locale = Localizations.MaybeLocaleOf(Context)
                            ?? Locale.FromCultureInfo(CultureInfo.CurrentUICulture);
            IReadOnlyList<SuggestionSpan>? suggestions = await service
                .FetchSpellCheckSuggestions(locale, text)
                ;
            if (!Mounted || request != _spellCheckRequest || suggestions is null) return;
            _spellCheckResults = new SpellCheckResults(text, suggestions);
            SetState(static () => { });
        }
    }
}

/// <summary>The render object widget of <see cref="EditableText"/>: Dart's private <c>_Editable</c>,
/// which configures a <see cref="RenderEditable"/> and hosts the widgets of the text's
/// <see cref="WidgetSpan"/>s as its children.</summary>
internal sealed class EditableRenderObjectWidget : MultiChildRenderObjectWidget
{
    public EditableRenderObjectWidget(
        InlineSpan inlineSpan,
        TextEditingValue value,
        LayerLink startHandleLayerLink,
        LayerLink endHandleLayerLink,
        ValueNotifier<bool> showCursor,
        bool forceLine,
        bool readOnly,
        bool hasFocus,
        int? maxLines,
        bool expands,
        TextScaler textScaler,
        TextAlign textAlign,
        TextDirection textDirection,
        string obscuringCharacter,
        bool obscureText,
        ViewportOffset offset,
        double cursorWidth,
        Point cursorOffset,
        bool paintCursorAboveText,
        ITextSelectionDelegate textSelectionDelegate,
        double devicePixelRatio,
        Clip clipBehavior,
        Color? cursorColor = null,
        Color? backgroundCursorColor = null,
        TextHeightBehavior? textHeightBehavior = null,
        TextWidthBasis textWidthBasis = TextWidthBasis.Parent,
        int? minLines = null,
        StrutStyle? strutStyle = null,
        Color? selectionColor = null,
        string? locale = null,
        bool rendererIgnoresPointer = false,
        double? cursorHeight = null,
        Radius? cursorRadius = null,
        BoxHeightStyle selectionHeightStyle = BoxHeightStyle.Tight,
        BoxWidthStyle selectionWidthStyle = BoxWidthStyle.Tight,
        bool enableInteractiveSelection = true,
        TextRange? promptRectRange = null,
        Color? promptRectColor = null,
        Key? key = null)
        : base(WidgetSpan.ExtractFromInlineSpan(inlineSpan, textScaler), key)
    {
        InlineSpan = inlineSpan;
        Value = value;
        CursorColor = cursorColor;
        StartHandleLayerLink = startHandleLayerLink;
        EndHandleLayerLink = endHandleLayerLink;
        BackgroundCursorColor = backgroundCursorColor;
        ShowCursor = showCursor;
        ForceLine = forceLine;
        ReadOnly = readOnly;
        HasFocus = hasFocus;
        MaxLines = maxLines;
        MinLines = minLines;
        Expands = expands;
        StrutStyle = strutStyle;
        SelectionColor = selectionColor;
        TextScaler = textScaler;
        TextAlign = textAlign;
        TextDirection = textDirection;
        Locale = locale;
        ObscuringCharacter = obscuringCharacter;
        ObscureText = obscureText;
        TextHeightBehavior = textHeightBehavior;
        TextWidthBasis = textWidthBasis;
        Offset = offset;
        RendererIgnoresPointer = rendererIgnoresPointer;
        CursorWidth = cursorWidth;
        CursorHeight = cursorHeight;
        CursorRadius = cursorRadius;
        CursorOffset = cursorOffset;
        PaintCursorAboveText = paintCursorAboveText;
        SelectionHeightStyle = selectionHeightStyle;
        SelectionWidthStyle = selectionWidthStyle;
        EnableInteractiveSelection = enableInteractiveSelection;
        TextSelectionDelegate = textSelectionDelegate;
        DevicePixelRatio = devicePixelRatio;
        PromptRectRange = promptRectRange;
        PromptRectColor = promptRectColor;
        ClipBehavior = clipBehavior;
    }

    public InlineSpan InlineSpan { get; }
    public TextEditingValue Value { get; }
    public Color? CursorColor { get; }
    public LayerLink StartHandleLayerLink { get; }
    public LayerLink EndHandleLayerLink { get; }
    public Color? BackgroundCursorColor { get; }
    public ValueNotifier<bool> ShowCursor { get; }
    public bool ForceLine { get; }
    public bool ReadOnly { get; }
    public bool HasFocus { get; }
    public int? MaxLines { get; }
    public int? MinLines { get; }
    public bool Expands { get; }
    public StrutStyle? StrutStyle { get; }
    public Color? SelectionColor { get; }
    public TextScaler TextScaler { get; }
    public TextAlign TextAlign { get; }
    public TextDirection TextDirection { get; }
    public string? Locale { get; }
    public string ObscuringCharacter { get; }
    public bool ObscureText { get; }
    public TextHeightBehavior? TextHeightBehavior { get; }
    public TextWidthBasis TextWidthBasis { get; }
    public ViewportOffset Offset { get; }
    public bool RendererIgnoresPointer { get; }
    public double CursorWidth { get; }
    public double? CursorHeight { get; }
    public Radius? CursorRadius { get; }
    public Point CursorOffset { get; }
    public bool PaintCursorAboveText { get; }
    public BoxHeightStyle SelectionHeightStyle { get; }
    public BoxWidthStyle SelectionWidthStyle { get; }
    public bool EnableInteractiveSelection { get; }
    public ITextSelectionDelegate TextSelectionDelegate { get; }
    public double DevicePixelRatio { get; }
    public TextRange? PromptRectRange { get; }
    public Color? PromptRectColor { get; }
    public Clip ClipBehavior { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderEditable(
            text: InlineSpan,
            cursorColor: CursorColor,
            startHandleLayerLink: StartHandleLayerLink,
            endHandleLayerLink: EndHandleLayerLink,
            backgroundCursorColor: BackgroundCursorColor,
            showCursor: ShowCursor,
            forceLine: ForceLine,
            readOnly: ReadOnly,
            hasFocus: HasFocus,
            maxLines: MaxLines,
            minLines: MinLines,
            expands: Expands,
            strutStyle: StrutStyle,
            selectionColor: SelectionColor,
            textScaler: TextScaler,
            textAlign: TextAlign,
            textDirection: TextDirection,
            locale: Locale,
            selection: Value.Selection,
            offset: Offset,
            ignorePointer: RendererIgnoresPointer,
            obscuringCharacter: ObscuringCharacter,
            obscureText: ObscureText,
            textHeightBehavior: TextHeightBehavior,
            textWidthBasis: TextWidthBasis,
            cursorWidth: CursorWidth,
            cursorHeight: CursorHeight,
            cursorRadius: CursorRadius,
            cursorOffset: CursorOffset,
            paintCursorAboveText: PaintCursorAboveText,
            selectionHeightStyle: SelectionHeightStyle,
            selectionWidthStyle: SelectionWidthStyle,
            enableInteractiveSelection: EnableInteractiveSelection,
            textSelectionDelegate: TextSelectionDelegate,
            devicePixelRatio: DevicePixelRatio,
            promptRectRange: PromptRectRange,
            promptRectColor: PromptRectColor,
            clipBehavior: ClipBehavior);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var render = (RenderEditable)renderObject;
        render.Text = InlineSpan;
        render.CursorColor = CursorColor;
        render.StartHandleLayerLink = StartHandleLayerLink;
        render.EndHandleLayerLink = EndHandleLayerLink;
        render.BackgroundCursorColor = BackgroundCursorColor;
        render.ShowCursor = ShowCursor;
        render.ForceLine = ForceLine;
        render.ReadOnly = ReadOnly;
        render.HasFocus = HasFocus;
        render.MaxLines = MaxLines;
        render.MinLines = MinLines;
        render.Expands = Expands;
        render.StrutStyle = StrutStyle;
        render.SelectionColor = SelectionColor;
        render.TextScaler = TextScaler;
        render.TextAlign = TextAlign;
        render.TextDirection = TextDirection;
        render.Locale = Locale;
        render.Selection = Value.Selection;
        render.Offset = Offset;
        render.IgnorePointer = RendererIgnoresPointer;
        render.TextHeightBehavior = TextHeightBehavior;
        render.TextWidthBasis = TextWidthBasis;
        render.ObscuringCharacter = ObscuringCharacter;
        render.ObscureText = ObscureText;
        render.CursorWidth = CursorWidth;
        render.CursorHeight = CursorHeight;
        render.CursorRadius = CursorRadius;
        render.CursorOffset = CursorOffset;
        render.SelectionHeightStyle = SelectionHeightStyle;
        render.SelectionWidthStyle = SelectionWidthStyle;
        render.EnableInteractiveSelection = EnableInteractiveSelection;
        render.TextSelectionDelegate = TextSelectionDelegate;
        render.DevicePixelRatio = DevicePixelRatio;
        render.PaintCursorAboveText = PaintCursorAboveText;
        render.PromptRectColor = PromptRectColor;
        render.ClipBehavior = ClipBehavior;
        render.SetPromptRectRange(PromptRectRange);
    }
}
