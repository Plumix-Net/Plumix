using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System.Collections;
using System.Globalization;
using Plumix.Foundation;
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

    /// <summary>This selection viewed as a text range.</summary>
    /// <remarks>Dart's <c>TextSelection extends TextRange</c>; a C# record struct cannot derive from
    /// another, so the conversion is explicit.</remarks>
    public TextRange AsTextRange() => new(Start, End);

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

/// <summary>Legacy switches controlling which editing commands appear in a text toolbar.</summary>
public sealed record ToolbarOptions(
    bool Copy = true,
    bool Cut = true,
    bool Paste = true,
    bool SelectAll = true);

/// <summary>Owns the undo/redo history associated with an editable field.</summary>
public sealed class UndoHistoryController : ChangeNotifier
{
}

/// <summary>Configures rich-content insertion offered by the platform text input service.</summary>
public sealed record ContentInsertionConfiguration(
    IReadOnlyList<string>? AllowedMimeTypes = null,
    Action<string>? OnContentInserted = null);

public sealed class EditableText : StatefulWidget
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
        TextInputActionType textInputAction = TextInputActionType.Unspecified,
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
        bool stylusHandwritingEnabled = true,
        Clip clipBehavior = Clip.HardEdge,
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
        InputFormatters = inputFormatters;
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
        ToolbarOptions = toolbarOptions ?? DefaultToolbarOptions(readOnly, obscureText);
        ContextMenuBuilder = contextMenuBuilder;
        MagnifierConfiguration = magnifierConfiguration ?? TextMagnifierConfiguration.Disabled;
        SelectionControls = selectionControls;
        ShowSelectionHandles = showSelectionHandles;
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
    public TextInputActionType TextInputAction { get; }
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
                ? new ToolbarOptions(false, false, false, false)
                : new ToolbarOptions(Copy: false, Cut: false, Paste: true, SelectAll: true);
        }

        return readOnly
            ? new ToolbarOptions(Copy: true, Cut: false, Paste: false, SelectAll: true)
            : new ToolbarOptions();
    }

    public sealed class EditableTextState : State, ITextSelectionDelegate, IAutofillClient, ITextInputClient
    {
        private AutofillGroupState? _currentAutofillScope;
        private TextInputConnection? _textInputConnection;

        private TextEditingController? _controller;
        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private double? _verticalNavigationX;
        private int? _verticalNavigationColumn;
        private readonly GlobalKey _editableRenderKey = new EditableRenderKey(Guid.NewGuid());
        private readonly LayerLink _startHandleLayerLink = new();
        private readonly LayerLink _endHandleLayerLink = new();
        private readonly LayerLink _toolbarLayerLink = new();
        private readonly ClipboardStatusNotifier _clipboardStatus = new();
        private TextSelectionOverlay? _selectionOverlay;
        private SpellCheckResults? _spellCheckResults;
        private int _spellCheckRequest;
        private PointerDeviceKind _lastPointerKind = PointerDeviceKind.Unknown;

        private sealed record EditableRenderKey(Guid Id) : GlobalKey;
        private Point? _lastPointerPosition;
        private int? _pointerSelectionAnchor;
        private TextSelection _lastSelection;
        private SelectionChangedCause? _pendingSelectionCause;
        private readonly Ticker _cursorTicker;
        private double _cursorOpacity = 1.0;
        private bool _hadFocus;

        public EditableTextState()
        {
            _cursorTicker = new Ticker(HandleCursorTick, "EditableText cursor");
        }

        private EditableText Widget => (EditableText)Element.Widget;

        public IReadOnlyList<ContextMenuButtonItem> ContextMenuButtonItems
        {
            get
            {
                TextEditingController controller = _controller!;
                bool hasSelection = !controller.Selection.IsCollapsed;
                bool selectionCoversAll = controller.Selection.Start == 0
                                          && controller.Selection.End == controller.Text.Length;
                var items = new List<ContextMenuButtonItem>();
                ToolbarOptions options = Widget.ToolbarOptions;
                if (options.Cut && !Widget.ReadOnly && !Widget.ObscureText && hasSelection)
                {
                    items.Add(new ContextMenuButtonItem(CutAndHide, ContextMenuButtonType.Cut));
                }
                if (options.Copy && !Widget.ObscureText && hasSelection)
                {
                    items.Add(new ContextMenuButtonItem(CopyAndHide, ContextMenuButtonType.Copy));
                }
                if (options.Paste && !Widget.ReadOnly && !string.IsNullOrEmpty(TextClipboard.GetText()))
                {
                    items.Add(new ContextMenuButtonItem(PasteAndHide, ContextMenuButtonType.Paste));
                }
                if (options.SelectAll && !selectionCoversAll && controller.Text.Length > 0)
                {
                    items.Add(new ContextMenuButtonItem(SelectAllAndHide, ContextMenuButtonType.SelectAll));
                }
                return items;
            }
        }

        public TextSelectionToolbarAnchors ContextMenuAnchors
        {
            get
            {
                TextEditingController controller = _controller!;
                TextSelection selection = controller.Selection.Clamp(controller.Text.Length);
                Rect start = ResolveCursorRectangle(_focusNode!, controller.Text.Length, selection.Start);
                Rect end = ResolveCursorRectangle(_focusNode!, controller.Text.Length, selection.End);
                Point primary = _lastPointerPosition ?? new Point(
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
                    inputAction: Widget.TextInputAction,
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
        public void UpdateEditingValue(TextEditingValue value)
        {
            TextEditingController? controller = _controller;
            if (controller is null || Widget.ReadOnly)
            {
                return;
            }

            if (controller.Value.Equals(value))
            {
                return;
            }

            controller.Value = value;
            Widget.OnChanged?.Invoke(controller.Text);
        }

        /// <inheritdoc/>
        public void PerformAction(TextInputActionType action)
        {
            switch (action)
            {
                case TextInputActionType.Done:
                case TextInputActionType.Go:
                case TextInputActionType.Send:
                case TextInputActionType.Search:
                    Widget.OnEditingComplete?.Invoke();
                    Widget.OnSubmitted?.Invoke(_controller!.Text);
                    break;
                default:
                    Widget.OnEditingComplete?.Invoke();
                    break;
            }
        }

        /// <inheritdoc/>
        public void ConnectionClosed() => _textInputConnection = null;

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
        /// <remarks>The floating cursor needs `RenderEditable`'s caret painting hooks, which the
        /// editable render object does not expose yet.</remarks>
        public void UpdateFloatingCursor(RawFloatingCursorPoint point)
        {
        }

        /// <inheritdoc/>
        /// <remarks>Plumix has no autocorrection prompt rect painting.</remarks>
        public void ShowAutocorrectionPromptRect(int start, int end)
        {
        }

        /// <inheritdoc/>
        void ITextInputClient.ShowToolbar() => ShowToolbar();

        private IAutofillClient EffectiveAutofillClient => Widget.AutofillClient ?? this;

        private bool NeedsAutofill =>
            EffectiveAutofillClient.TextInputConfiguration.AutofillConfiguration.Enabled;

        private void OpenInputConnection()
        {
            if (_textInputConnection is { Attached: true })
            {
                return;
            }

            TextInputConfiguration configuration = EffectiveAutofillClient.TextInputConfiguration;
            _textInputConnection = NeedsAutofill && _currentAutofillScope != null
                ? _currentAutofillScope.Attach(this, configuration)
                : UI.TextInput.Attach(this, configuration);
            _textInputConnection.SetEditingState(CurrentTextEditingValue);
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
        }

        public override void InitState()
        {
            AttachController(Widget.Controller);
            AttachFocusNode(Widget.FocusNode);
            _lastSelection = Widget.Controller.Selection;
            UpdateCursorTicker();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
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

            // Only a null <-> non-null change of contextMenuBuilder invalidates the overlay. If
            // only the identity of the closure changed (an inline lambda rebuilt every frame),
            // the shown toolbar is rebuilt instead so its overlay entry picks up the new closure.
            bool contextMenuPresenceChanged =
                (Widget.ContextMenuBuilder is null) != (oldEditableText.ContextMenuBuilder is null);
            if (!Widget.EnableInteractiveSelection || contextMenuPresenceChanged)
            {
                HideToolbar();
            }
            else if (_selectionOverlay is { ToolbarIsVisible: true }
                     && oldEditableText.ContextMenuBuilder != Widget.ContextMenuBuilder)
            {
                // Deferred to the next frame because ShowToolbar() needs a laid-out render tree,
                // and DidUpdateWidget runs before layout.
                Scheduler.AddPostFrameCallback(_ =>
                {
                    if (Mounted && _selectionOverlay is { ToolbarIsVisible: true })
                    {
                        _selectionOverlay.ShowToolbar();
                    }
                });
            }
            if (oldEditableText.CursorOpacityAnimates != Widget.CursorOpacityAnimates
                || oldEditableText.ShowCursor != Widget.ShowCursor)
            {
                UpdateCursorTicker();
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
                        _ = RequestSpellCheckAsync();
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
            CloseInputConnection();
            _currentAutofillScope?.Unregister(AutofillId);
            _currentAutofillScope = null;
            _selectionOverlay?.Dispose();
            _selectionOverlay = null;
            _clipboardStatus.Dispose();
            DetachController();
            DetachFocusNode(disposeOwned: true);
            _cursorTicker.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            DefaultSelectionStyle selectionStyle = DefaultSelectionStyle.Of(context);
            string text = _controller!.Text;
            bool showPlaceholder = string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(Widget.Placeholder);
            string renderedText = Widget.ObscureText
                ? new string(Widget.ObscuringCharacter[0], text.Length)
                : text;
            string displayText = showPlaceholder ? Widget.Placeholder ?? string.Empty : renderedText;
            var textColor = showPlaceholder ? Widget.PlaceholderColor : Widget.TextColor;
            var backgroundColor = _focusNode!.HasFocus ? Widget.FocusedBackgroundColor : Widget.BackgroundColor;

            TextStyle? style = Widget.Style;
            if (Widget.StrutStyle is { } strut)
            {
                style = (style ?? new TextStyle()).CopyWith(
                    fontFamily: strut.FontFamily,
                    fontSize: strut.FontSize,
                    fontWeight: strut.FontWeight,
                    fontStyle: strut.FontStyle,
                    height: strut.Height);
            }
            Widget result = new Focus(
                focusNode: _focusNode,
                autofocus: Widget.Autofocus,
                canRequestFocus: Widget.Enabled && Widget.CanRequestFocus,
                onKeyEvent: HandleKeyEvent,
                onTextInput: HandleTextInput,
                onTextComposition: HandleTextComposition,
                onTextInputState: HandleTextInputState,
                onTextSelectionChanged: HandleTextSelectionChanged,
                child: new Container(
                    color: backgroundColor,
                    padding: Widget.Padding,
                    child: new EditableRenderObjectWidget(
                        text: displayText,
                        selection: showPlaceholder ? TextSelection.Collapsed(0) : _controller.Selection,
                        composing: showPlaceholder ? null : _controller.Composing,
                        startHandleLayerLink: _startHandleLayerLink,
                        endHandleLayerLink: _endHandleLayerLink,
                        toolbarLayerLink: _toolbarLayerLink,
                        fontFamily: style?.FontFamily,
                        fontSize: style?.FontSize ?? Widget.FontSize,
                        color: style?.Color ?? textColor,
                        fontWeight: style?.FontWeight,
                        fontStyle: style?.FontStyle,
                        height: style?.Height,
                        letterSpacing: style?.LetterSpacing,
                        textAlign: Widget.TextAlign,
                        textDirection: Widget.TextDirection ?? Directionality.Of(context),
                        multiline: Widget.Multiline,
                        selectionColor: Widget.SelectionColor ?? selectionStyle.SelectionColor ?? default,
                        cursorColor: Widget.CursorColor ?? selectionStyle.CursorColor ?? Widget.TextColor,
                        minLines: Widget.MinLines,
                        maxLines: Widget.MaxLines,
                        expands: Widget.Expands,
                        cursorWidth: Widget.CursorWidth,
                        cursorHeight: Widget.CursorHeight,
                        cursorRadius: Widget.CursorRadius,
                        selectionHeightStyle: Widget.SelectionHeightStyle,
                        selectionWidthStyle: Widget.SelectionWidthStyle,
                        cursorOpacity: _cursorOpacity,
                        cursorOffset: Widget.CursorOffset,
                        paintCursorAboveText: Widget.PaintCursorAboveText,
                        showCursor: (Widget.ShowCursor ?? _focusNode.HasFocus) && !showPlaceholder,
                        suggestionSpans: _spellCheckResults?.SuggestionSpans,
                        misspelledColor: Widget.SpellCheckConfiguration?.MisspelledTextStyle?.Color ?? Colors.Red,
                        key: _editableRenderKey)));
            if (Widget.ClipBehavior != Clip.None)
            {
                result = new ClipRect(clipBehavior: Widget.ClipBehavior, child: result);
            }
            if (!Widget.RendererIgnoresPointer)
            {
                result = new Listener(
                    behavior: HitTestBehavior.Translucent,
                    onPointerDown: HandlePointerDown,
                    onPointerMove: HandlePointerMove,
                    onPointerUp: HandlePointerUp,
                    onPointerCancel: HandlePointerCancel,
                    child: result);
                result = new GestureDetector(
                    behavior: HitTestBehavior.Translucent,
                    onDoubleTap: HandleDoubleTap,
                    onLongPress: HandleLongPress,
                    onSecondaryTap: () => ShowToolbar(),
                    child: result);
            }

            result = new MouseRegion(
                cursor: Widget.MouseCursor
                        ?? selectionStyle.MouseCursor
                        ?? SystemMouseCursors.Text,
                child: result);

            return new Semantics(
                label: Widget.SemanticsLabel,
                flags: SemanticsFlags.IsTextField
                       | (Widget.Enabled ? SemanticsFlags.IsEnabled : SemanticsFlags.None)
                       | (_focusNode.HasFocus ? SemanticsFlags.IsFocused : SemanticsFlags.None),
                onTap: Widget.Enabled ? () => _focusNode.RequestFocus() : null,
                child: result);
        }

        public bool ShowToolbar()
        {
            if (!Widget.EnableInteractiveSelection
                || Widget.ContextMenuBuilder is null
                || ContextMenuButtonItems.Count == 0)
            {
                return false;
            }

            TextSelectionOverlay overlay = EnsureSelectionOverlay();
            overlay.ShowToolbar();
            return overlay.ToolbarIsVisible;
        }

        public void HideToolbar(bool hideHandles = true)
        {
            _selectionOverlay?.HideToolbar();
            if (hideHandles) _selectionOverlay?.HideHandles();
        }

        public void RequestKeyboard()
        {
            if (Widget.Enabled && !Widget.ReadOnly && _focusNode?.HasFocus == true)
            {
                OpenInputConnection();
            }
        }

        public TextEditingValue TextEditingValue => _controller!.Value;

        public bool CutEnabled => !Widget.ReadOnly && !Widget.ObscureText;

        public bool CopyEnabled => !Widget.ObscureText;

        public bool PasteEnabled => !Widget.ReadOnly;

        public bool SelectAllEnabled => Widget.EnableInteractiveSelection;

        public void UserUpdateTextEditingValue(TextEditingValue value, SelectionChangedCause? cause)
        {
            TextEditingController controller = _controller!;
            if (cause.HasValue)
            {
                _pendingSelectionCause = cause.Value;
            }

            TextEditingValue oldValue = controller.Value;
            bool textChanged = !string.Equals(controller.Text, value.Text, StringComparison.Ordinal);
            controller.Value = value;
            _pendingSelectionCause = null;
            if (textChanged)
            {
                ApplyInputFormatters(oldValue);
                Widget.OnChanged?.Invoke(controller.Text);
            }
        }

        /// <summary>
        /// Runs <see cref="EditableText.InputFormatters"/> over the value the user just produced.
        /// Dart funnels every user edit through <c>_formatAndSetValue</c>; Plumix mutates the
        /// controller in place, so the formatters run right after the mutation instead.
        /// </summary>
        private void ApplyInputFormatters(TextEditingValue oldValue)
        {
            IReadOnlyList<TextInputFormatter>? formatters = Widget.InputFormatters;
            if (formatters is null || formatters.Count == 0) return;
            TextEditingController controller = _controller!;
            TextEditingValue value = controller.Value;
            foreach (TextInputFormatter formatter in formatters)
            {
                value = formatter.FormatEditUpdate(oldValue, value);
            }

            if (!value.Equals(controller.Value))
            {
                controller.Value = value;
            }
        }

        public void CutSelection(SelectionChangedCause cause) => CutAndHide();

        public void CopySelection(SelectionChangedCause cause) => CopyAndHide();

        public void PasteText(SelectionChangedCause cause) => PasteAndHide();

        public void SelectAll(SelectionChangedCause cause)
        {
            _pendingSelectionCause = cause;
            _ = _controller!.SelectAll();
            _pendingSelectionCause = null;
        }

        private void AttachController(TextEditingController controller)
        {
            _controller = controller;
            _lastSelection = controller.Selection;
            _controller.AddListener(HandleControllerChanged);
        }

        private void DetachController()
        {
            if (_controller == null)
            {
                return;
            }

            _controller.RemoveListener(HandleControllerChanged);
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

        public void HandlePointerDown(PointerDownEvent @event)
        {
            if (!Widget.Enabled)
            {
                return;
            }

            _lastPointerPosition = @event.Position;
            _lastPointerKind = @event.Kind;
            if (!@event.Buttons.HasFlag(PointerButtons.Primary))
            {
                return;
            }

            _focusNode!.RequestFocus();
            int offset = GetTextPosition(@event.Position);
            _pointerSelectionAnchor = offset;
            SetSelection(TextSelection.Collapsed(offset), SelectionChangedCause.Tap);
            if (@event.Kind == PointerDeviceKind.Touch && Widget.ShowSelectionHandles)
            {
                ShowHandles();
            }
        }

        public void HandlePointerMove(PointerMoveEvent @event)
        {
            if (!Widget.EnableInteractiveSelection
                || !@event.Down
                || !@event.Buttons.HasFlag(PointerButtons.Primary)
                || !_pointerSelectionAnchor.HasValue)
            {
                return;
            }

            _lastPointerPosition = @event.Position;
            SetSelection(
                new TextSelection(
                    _pointerSelectionAnchor.Value,
                    GetTextPosition(@event.Position)),
                SelectionChangedCause.Drag);
            if (@event.Kind == PointerDeviceKind.Touch)
            {
                TextSelectionOverlay overlay = EnsureSelectionOverlay();
                overlay.ShowHandles();
                overlay.ShowMagnifier(@event.Position);
            }
        }

        public void HandlePointerUp(PointerUpEvent @event)
        {
            _lastPointerPosition = @event.Position;
            _pointerSelectionAnchor = null;
            _selectionOverlay?.HideMagnifier();
        }

        public void HandlePointerCancel(PointerCancelEvent @event)
        {
            _pointerSelectionAnchor = null;
            _selectionOverlay?.HideMagnifier();
        }

        public void HandleDoubleTap()
        {
            if (Widget.Enabled && Widget.EnableInteractiveSelection && _lastPointerPosition.HasValue)
            {
                SelectWordAt(_lastPointerPosition.Value, SelectionChangedCause.DoubleTap);
            }
        }

        public void HandleLongPress()
        {
            if (!Widget.Enabled || !Widget.EnableInteractiveSelection)
            {
                return;
            }

            if (_lastPointerPosition.HasValue && _controller!.Selection.IsCollapsed)
            {
                SelectWordAt(_lastPointerPosition.Value, SelectionChangedCause.LongPress);
            }
            if (_lastPointerKind is PointerDeviceKind.Touch or PointerDeviceKind.Stylus)
            {
                ShowHandles();
                if (_lastPointerPosition.HasValue) EnsureSelectionOverlay().ShowMagnifier(_lastPointerPosition.Value);
            }
            ShowToolbar();
        }

        private int GetTextPosition(Point globalPosition)
        {
            if (RenderEditable is { } renderEditable)
            {
                return renderEditable.GetPositionForPoint(globalPosition).Offset;
            }

            TextEditingController controller = _controller!;
            if (TryCreateTextLayout(_focusNode!, controller.Text, out TextLayout? layout, out Rect contentRect))
            {
                using (layout!)
                {
                    Point localPosition = new(
                        Math.Clamp(globalPosition.X - contentRect.X, 0, Math.Max(0, contentRect.Width)),
                        Math.Clamp(globalPosition.Y - contentRect.Y, 0, Math.Max(0, contentRect.Height)));
                    return Math.Clamp(
                        layout!.HitTestPoint(localPosition).TextPosition,
                        0,
                        controller.Text.Length);
                }
            }

            double characterWidth = Math.Max(1.0, Widget.FontSize * 0.6);
            int offset = (int)Math.Round(
                Math.Max(0, globalPosition.X - contentRect.X) / characterWidth);
            return Math.Clamp(offset, 0, controller.Text.Length);
        }

        private void SelectWordAt(
            Point globalPosition,
            SelectionChangedCause cause)
        {
            TextEditingController controller = _controller!;
            string text = controller.Text;
            if (text.Length == 0)
            {
                return;
            }

            int index = Math.Clamp(GetTextPosition(globalPosition), 0, text.Length - 1);
            bool whitespace = char.IsWhiteSpace(text[index]);
            int start = index;
            int end = index + 1;
            while (start > 0 && char.IsWhiteSpace(text[start - 1]) == whitespace)
            {
                start--;
            }
            while (end < text.Length && char.IsWhiteSpace(text[end]) == whitespace)
            {
                end++;
            }

            if (!whitespace)
            {
                while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
                {
                    start--;
                }
                while (end < text.Length && !char.IsWhiteSpace(text[end]))
                {
                    end++;
                }
            }

            SetSelection(new TextSelection(start, end), cause);
        }

        private void CutAndHide()
        {
            TextEditingController controller = _controller!;
            if (!Widget.ReadOnly && !controller.Selection.IsCollapsed)
            {
                TextEditingValue oldValue = controller.Value;
                TextClipboard.SetText(controller.SelectedText);
                _pendingSelectionCause = SelectionChangedCause.Toolbar;
                if (controller.DeleteBackward())
                {
                    ApplyInputFormatters(oldValue);
                    Widget.OnChanged?.Invoke(controller.Text);
                }
                _pendingSelectionCause = null;
            }
            HideToolbar();
        }

        private void CopyAndHide()
        {
            TextEditingController controller = _controller!;
            if (!controller.Selection.IsCollapsed)
            {
                TextClipboard.SetText(controller.SelectedText);
            }
            HideToolbar();
        }

        private void PasteAndHide()
        {
            string value = TextClipboard.GetText() ?? string.Empty;
            if (!Widget.ReadOnly && !string.IsNullOrEmpty(value))
            {
                TextEditingController controller = _controller!;
                TextEditingValue oldValue = controller.Value;
                _pendingSelectionCause = SelectionChangedCause.Toolbar;
                if (controller.Insert(LimitInsertion(value)))
                {
                    ApplyInputFormatters(oldValue);
                    Widget.OnChanged?.Invoke(controller.Text);
                }
                _pendingSelectionCause = null;
            }
            HideToolbar();
        }

        private void SelectAllAndHide()
        {
            _pendingSelectionCause = SelectionChangedCause.Toolbar;
            _controller!.SelectAll();
            _pendingSelectionCause = null;
            HideToolbar();
        }

        private void SetSelection(
            TextSelection selection,
            SelectionChangedCause cause)
        {
            _pendingSelectionCause = cause;
            _controller!.Selection = selection;
            _pendingSelectionCause = null;
        }

        private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
        {
            if (Widget.OnKeyEvent?.Invoke(node, @event) == KeyEventResult.Handled)
            {
                return KeyEventResult.Handled;
            }

            if (!Widget.Enabled || @event is not KeyDownEvent)
            {
                return KeyEventResult.Ignored;
            }

            var controller = _controller!;
            TextEditingValue valueBeforeKey = controller.Value;
            LogicalKeyboardKey key = @event.LogicalKey;
            bool textChanged = false;
            bool keepVerticalNavigationX = false;
            bool isEditingShortcut = HardwareKeyboard.Instance.IsControlPressed
                                     || HardwareKeyboard.Instance.IsMetaPressed;
            bool isWordShortcut = HardwareKeyboard.Instance.IsControlPressed
                                  || HardwareKeyboard.Instance.IsAltPressed;
            bool isParagraphShortcut = Widget.Multiline && isWordShortcut;

            if (isEditingShortcut && key.Equals(LogicalKeyboardKey.KeyA))
            {
                _pendingSelectionCause = SelectionChangedCause.Keyboard;
                _ = controller.SelectAll();
                _pendingSelectionCause = null;
                _verticalNavigationX = null;
                _verticalNavigationColumn = null;
                return KeyEventResult.Handled;
            }

            if (isEditingShortcut && key.Equals(LogicalKeyboardKey.KeyC))
            {
                if (!controller.Selection.IsCollapsed)
                {
                    TextClipboard.SetText(controller.SelectedText);
                }

                _verticalNavigationX = null;
                _verticalNavigationColumn = null;
                return KeyEventResult.Handled;
            }

            if (isEditingShortcut && key.Equals(LogicalKeyboardKey.KeyX))
            {
                if (!controller.Selection.IsCollapsed)
                {
                    TextClipboard.SetText(controller.SelectedText);
                    textChanged = !Widget.ReadOnly && controller.DeleteBackward();
                    if (textChanged)
                    {
                        ApplyInputFormatters(valueBeforeKey);
                        Widget.OnChanged?.Invoke(controller.Text);
                    }
                }

                _verticalNavigationX = null;
                _verticalNavigationColumn = null;
                return KeyEventResult.Handled;
            }

            if (isEditingShortcut && key.Equals(LogicalKeyboardKey.KeyV))
            {
                string pasteText = TextClipboard.GetText() ?? string.Empty;
                if (!Widget.ReadOnly && !string.IsNullOrEmpty(pasteText))
                {
                    pasteText = LimitInsertion(pasteText);
                    textChanged = controller.Composing.HasValue
                        ? controller.CommitComposing(pasteText)
                        : controller.Insert(pasteText);
                    if (textChanged)
                    {
                        ApplyInputFormatters(valueBeforeKey);
                        Widget.OnChanged?.Invoke(controller.Text);
                    }
                }

                _verticalNavigationX = null;
                _verticalNavigationColumn = null;
                return KeyEventResult.Handled;
            }

            if (key.Equals(LogicalKeyboardKey.Backspace))
            {
                textChanged = !Widget.ReadOnly && (isWordShortcut
                    ? controller.DeleteBackwardByWord()
                    : controller.DeleteBackward());
            }
            else if (key.Equals(LogicalKeyboardKey.Delete))
            {
                textChanged = !Widget.ReadOnly && (isWordShortcut
                    ? controller.DeleteForwardByWord()
                    : controller.DeleteForward());
            }
            else if (key.Equals(LogicalKeyboardKey.ArrowLeft))
            {
                _ = isWordShortcut
                    ? controller.MoveCaretToPreviousWord(extendSelection: HardwareKeyboard.Instance.IsShiftPressed)
                    : controller.MoveCaretLeft(extendSelection: HardwareKeyboard.Instance.IsShiftPressed);
            }
            else if (key.Equals(LogicalKeyboardKey.ArrowRight))
            {
                _ = isWordShortcut
                    ? controller.MoveCaretToNextWord(extendSelection: HardwareKeyboard.Instance.IsShiftPressed)
                    : controller.MoveCaretRight(extendSelection: HardwareKeyboard.Instance.IsShiftPressed);
            }
            else if (Widget.Multiline
                     && key.Equals(LogicalKeyboardKey.ArrowUp))
            {
                if (isParagraphShortcut)
                {
                    _ = controller.MoveCaretToParagraphStart(extendSelection: HardwareKeyboard.Instance.IsShiftPressed);
                }
                else
                {
                    _ = MoveCaretVertical(moveDown: false, extendSelection: HardwareKeyboard.Instance.IsShiftPressed);
                    keepVerticalNavigationX = true;
                }
            }
            else if (Widget.Multiline
                     && key.Equals(LogicalKeyboardKey.ArrowDown))
            {
                if (isParagraphShortcut)
                {
                    _ = controller.MoveCaretToParagraphEnd(extendSelection: HardwareKeyboard.Instance.IsShiftPressed);
                }
                else
                {
                    _ = MoveCaretVertical(moveDown: true, extendSelection: HardwareKeyboard.Instance.IsShiftPressed);
                    keepVerticalNavigationX = true;
                }
            }
            else if (key.Equals(LogicalKeyboardKey.Home))
            {
                _ = controller.MoveCaretToStart(extendSelection: HardwareKeyboard.Instance.IsShiftPressed);
            }
            else if (key.Equals(LogicalKeyboardKey.End))
            {
                _ = controller.MoveCaretToEnd(extendSelection: HardwareKeyboard.Instance.IsShiftPressed);
            }
            else if (key.Equals(LogicalKeyboardKey.Enter))
            {
                if (Widget.Multiline && !Widget.ReadOnly)
                {
                    textChanged = controller.Insert(LimitInsertion("\n"));
                }
                else
                {
                    Widget.OnEditingComplete?.Invoke();
                    Widget.OnSubmitted?.Invoke(controller.Text);
                }
            }
            else if (key.Equals(LogicalKeyboardKey.Escape))
            {
                // Dart's `EditableText` only cancels an in-flight composing region on escape and
                // otherwise lets the key bubble up to `DismissIntent`, which menus and dialogs use.
                if (!controller.ClearComposing()) return KeyEventResult.Ignored;
            }
            else
            {
                return KeyEventResult.Ignored;
            }

            if (!keepVerticalNavigationX)
            {
                _verticalNavigationX = null;
                _verticalNavigationColumn = null;
            }

            if (textChanged)
            {
                ApplyInputFormatters(valueBeforeKey);
                Widget.OnChanged?.Invoke(controller.Text);
            }

            return KeyEventResult.Handled;
        }

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
            if (string.IsNullOrEmpty(normalizedInput))
            {
                return false;
            }

            normalizedInput = LimitInsertion(normalizedInput);
            if (string.IsNullOrEmpty(normalizedInput)) return false;
            TextEditingValue oldValue = _controller!.Value;
            bool changed = _controller.Composing.HasValue
                ? _controller.CommitComposing(normalizedInput)
                : _controller.Insert(normalizedInput);
            if (changed)
            {
                _verticalNavigationX = null;
                _verticalNavigationColumn = null;
                ApplyInputFormatters(oldValue);
                Widget.OnChanged?.Invoke(_controller.Text);
            }

            return changed;
        }

        private bool HandleTextComposition(FocusNode node, string text, bool isCommit)
        {
            if (!Widget.Enabled || Widget.ReadOnly)
            {
                return false;
            }

            string limitedText = LimitInsertion(text);
            TextEditingValue oldValue = _controller!.Value;
            bool changed = isCommit
                ? _controller.CommitComposing(limitedText)
                : _controller.SetComposing(limitedText);
            if (changed)
            {
                _verticalNavigationX = null;
                _verticalNavigationColumn = null;
                ApplyInputFormatters(oldValue);
                Widget.OnChanged?.Invoke(_controller.Text);
            }

            return changed;
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

            var controller = _controller!;
            int textLength = controller.Text.Length;
            var nextSelection = new TextSelection(
                BaseOffset: Math.Clamp(baseOffset, 0, textLength),
                ExtentOffset: Math.Clamp(extentOffset, 0, textLength));
            var previousSelection = controller.Selection;
            if (previousSelection.Equals(nextSelection))
            {
                return false;
            }

            controller.Selection = nextSelection;
            _verticalNavigationX = null;
            _verticalNavigationColumn = null;
            return !previousSelection.Equals(controller.Selection);
        }

        private Rect ResolveCursorRectangle(FocusNode node, int textLength, int caretOffset)
        {
            if (TryCreateTextLayout(node, _controller!.Text, out var layout, out var contentRect))
            {
                using (layout!)
                {
                    int clampedCaretOffset = Math.Clamp(caretOffset, 0, textLength);
                    var hitRect = layout!.HitTestTextPosition(clampedCaretOffset);
                    double caretHeight = Math.Max(1, hitRect.Height);
                    return new Rect(
                        x: contentRect.X + hitRect.X,
                        y: contentRect.Y + hitRect.Y,
                        width: 1,
                        height: caretHeight);
                }
            }

            int clampedCaretForFallback = Math.Clamp(caretOffset, 0, textLength);
            double fallbackX = contentRect.X + Math.Min(contentRect.Width, clampedCaretForFallback * Math.Max(1, Widget.FontSize * 0.6));
            double fallbackHeight = Math.Max(1, Math.Min(contentRect.Height, Widget.FontSize * 1.2));
            return new Rect(fallbackX, contentRect.Y, 1, fallbackHeight);
        }

        private bool MoveCaretVertical(bool moveDown, bool extendSelection)
        {
            if (!Widget.Multiline)
            {
                return false;
            }

            var controller = _controller!;
            string text = controller.Text;
            if (!TryCreateTextLayout(_focusNode!, text, out var layout, out _))
            {
                return MoveCaretVerticalByLineModel(controller, text, moveDown, extendSelection);
            }

            using (layout!)
            {
                var clampedSelection = controller.Selection.Clamp(text.Length);
                int caretOffset = clampedSelection.ExtentOffset;
                var caretRect = layout!.HitTestTextPosition(caretOffset);
                double maxX = Math.Max(0, layout.WidthIncludingTrailingWhitespace);
                double targetX = Math.Clamp(_verticalNavigationX ?? caretRect.X, 0, maxX);
                double probeDelta = Math.Max(1, caretRect.Height * 0.5);
                double probeY = moveDown
                    ? caretRect.Y + caretRect.Height + probeDelta
                    : caretRect.Y - probeDelta;
                var hit = layout.HitTestPoint(new Point(targetX, probeY));
                int nextOffset = Math.Clamp(
                    hit.CharacterHit.FirstCharacterIndex + hit.CharacterHit.TrailingLength,
                    0,
                    text.Length);
                var nextSelection = extendSelection
                    ? new TextSelection(clampedSelection.BaseOffset, nextOffset)
                    : TextSelection.Collapsed(nextOffset);
                var previousSelection = controller.Selection;
                controller.Selection = nextSelection;
                _verticalNavigationX = targetX;
                _verticalNavigationColumn = null;
                return !previousSelection.Equals(controller.Selection);
            }
        }

        private bool MoveCaretVerticalByLineModel(
            TextEditingController controller,
            string text,
            bool moveDown,
            bool extendSelection)
        {
            var clampedSelection = controller.Selection.Clamp(text.Length);
            int caretOffset = clampedSelection.ExtentOffset;
            var lineStarts = new List<int> { 0 };
            for (int index = 0; index < text.Length; index++)
            {
                if (text[index] == '\n')
                {
                    lineStarts.Add(index + 1);
                }
            }

            int currentLineIndex = 0;
            for (int index = 1; index < lineStarts.Count; index++)
            {
                if (lineStarts[index] > caretOffset)
                {
                    break;
                }

                currentLineIndex = index;
            }

            int targetLineIndex = moveDown ? currentLineIndex + 1 : currentLineIndex - 1;
            if (targetLineIndex < 0 || targetLineIndex >= lineStarts.Count)
            {
                return false;
            }

            int currentLineStart = lineStarts[currentLineIndex];
            int currentLineEnd = currentLineIndex + 1 < lineStarts.Count
                ? lineStarts[currentLineIndex + 1] - 1
                : text.Length;
            int currentLineColumn = Math.Clamp(caretOffset - currentLineStart, 0, currentLineEnd - currentLineStart);
            int preferredColumn = _verticalNavigationColumn ?? currentLineColumn;

            int targetLineStart = lineStarts[targetLineIndex];
            int targetLineEnd = targetLineIndex + 1 < lineStarts.Count
                ? lineStarts[targetLineIndex + 1] - 1
                : text.Length;
            int targetLineLength = Math.Max(0, targetLineEnd - targetLineStart);
            int nextOffset = targetLineStart + Math.Min(preferredColumn, targetLineLength);
            var nextSelection = extendSelection
                ? new TextSelection(clampedSelection.BaseOffset, nextOffset)
                : TextSelection.Collapsed(nextOffset);
            var previousSelection = controller.Selection;
            controller.Selection = nextSelection;
            _verticalNavigationX = null;
            _verticalNavigationColumn = preferredColumn;
            return !previousSelection.Equals(controller.Selection);
        }

        private bool TryCreateTextLayout(
            FocusNode node,
            string text,
            out TextLayout? layout,
            out Rect contentRect)
        {
            contentRect = ResolveContentRect(node);
            double maxWidth = Widget.Multiline
                ? Math.Max(1, contentRect.Width)
                : double.PositiveInfinity;

            try
            {
                layout = new TextLayout(
                    text: text,
                    typeface: new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal),
                    fontSize: Widget.FontSize,
                    foreground: Brushes.Transparent,
                    textWrapping: Widget.Multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
                    maxWidth: maxWidth);
                return true;
            }
            catch (Exception exception) when (TextLayoutFallback.IsMissingFontManager(exception))
            {
                layout = null;
                return false;
            }
        }

        private Rect ResolveContentRect(FocusNode node)
        {
            var fieldRect = node.ResolveTraversalRect() ?? new Rect(
                x: 0,
                y: 0,
                width: 1,
                height: Math.Max(1, Widget.FontSize * 1.2 + Widget.Padding.Top + Widget.Padding.Bottom));
            return new Rect(
                x: fieldRect.X + Widget.Padding.Left,
                y: fieldRect.Y + Widget.Padding.Top,
                width: Math.Max(0, fieldRect.Width - Widget.Padding.Left - Widget.Padding.Right),
                height: Math.Max(1, fieldRect.Height - Widget.Padding.Top - Widget.Padding.Bottom));
        }

        private void HandleControllerChanged()
        {
            TextSelection selection = _controller!.Selection;
            if (RenderEditable is { } renderEditable)
            {
                string renderedText = Widget.ObscureText
                    ? new string(Widget.ObscuringCharacter[0], _controller.Text.Length)
                    : _controller.Text;
                renderEditable.Text = renderedText;
                renderEditable.Selection = selection;
                renderEditable.Composing = _controller.Composing;
            }
            if (!_lastSelection.Equals(selection))
            {
                _lastSelection = selection;
                Widget.OnSelectionChanged?.Invoke(selection, _pendingSelectionCause);
            }
            if (selection.IsCollapsed)
            {
                HideToolbar();
            }
            _selectionOverlay?.Update(_controller.Value);
            _ = RequestSpellCheckAsync();
            SetState(static () => { });
        }

        private void HandleFocusNodeChanged()
        {
            _verticalNavigationX = null;
            _verticalNavigationColumn = null;
            bool hasFocus = _focusNode?.HasFocus == true;
            if (hasFocus)
            {
                if (!_hadFocus
                    && Widget.SelectAllOnFocus
                    && Widget.EnableInteractiveSelection
                    && !Widget.Multiline)
                {
                    _ = _controller!.SelectAll();
                }
                OpenInputConnection();
            }
            else
            {
                _selectionOverlay?.Hide();
                CloseInputConnection();
            }
            _hadFocus = hasFocus;
            UpdateCursorTicker();
            SetState(static () => { });
        }

        private void UpdateCursorTicker()
        {
            bool shouldAnimate = Widget.CursorOpacityAnimates
                                 && (Widget.ShowCursor ?? _focusNode?.HasFocus == true);
            if (shouldAnimate && !_cursorTicker.IsActive)
            {
                _cursorOpacity = 1.0;
                _cursorTicker.Start();
            }
            else if (!shouldAnimate && _cursorTicker.IsActive)
            {
                _cursorTicker.Stop();
                _cursorOpacity = 1.0;
            }
        }

        private void HandleCursorTick(TimeSpan elapsed)
        {
            double milliseconds = elapsed.TotalMilliseconds % 1000.0;
            double opacity = milliseconds switch
            {
                <= 500.0 => 1.0,
                < 650.0 => 1.0 - ((milliseconds - 500.0) / 150.0),
                <= 850.0 => 0.0,
                _ => (milliseconds - 850.0) / 150.0,
            };
            if (Math.Abs(opacity - _cursorOpacity) < 0.0001)
            {
                return;
            }

            _cursorOpacity = Math.Clamp(opacity, 0.0, 1.0);
            if (Mounted)
            {
                SetState(static () => { });
            }
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

        public void ShowHandles() => EnsureSelectionOverlay().ShowHandles();

        private RenderEditable? RenderEditable =>
            _editableRenderKey.CurrentContext?.FindRenderObject() as RenderEditable;

        private TextSelectionOverlay EnsureSelectionOverlay()
        {
            RenderEditable renderEditable = RenderEditable
                ?? throw new InvalidOperationException("EditableText must be laid out before showing selection UI.");
            if (_selectionOverlay is not null) return _selectionOverlay;
            _clipboardStatus.Update();
            _selectionOverlay = new TextSelectionOverlay(
                value: _controller!.Value,
                context: Context,
                toolbarLayerLink: _toolbarLayerLink,
                startHandleLayerLink: _startHandleLayerLink,
                endHandleLayerLink: _endHandleLayerLink,
                renderObject: renderEditable,
                selectionControls: Widget.SelectionControls,
                handlesVisible: Widget.ShowSelectionHandles,
                selectionDelegate: this,
                clipboardStatus: _clipboardStatus,
                contextMenuBuilder: Widget.ContextMenuBuilder is null
                    ? null
                    : context => Widget.ContextMenuBuilder(context, this),
                magnifierConfiguration: Widget.MagnifierConfiguration);
            return _selectionOverlay;
        }

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

        private async Task RequestSpellCheckAsync()
        {
            SpellCheckConfiguration? configuration = Widget.SpellCheckConfiguration;
            if (!SpellCheckEnabled || string.IsNullOrEmpty(_controller!.Text)) return;
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
                .FetchSpellCheckSuggestions(locale, _controller.Text)
                .ConfigureAwait(false);
            if (!Mounted || request != _spellCheckRequest || suggestions is null) return;
            _spellCheckResults = new SpellCheckResults(_controller.Text, suggestions);
            SetState(static () => { });
        }
    }
}

internal sealed class EditableRenderObjectWidget : LeafRenderObjectWidget
{
    public EditableRenderObjectWidget(
        string text,
        TextSelection selection,
        TextRange? composing,
        LayerLink startHandleLayerLink,
        LayerLink endHandleLayerLink,
        LayerLink toolbarLayerLink,
        FontFamily? fontFamily,
        double fontSize,
        Color color,
        FontWeight? fontWeight,
        FontStyle? fontStyle,
        double? height,
        double? letterSpacing,
        TextAlign textAlign,
        TextDirection textDirection,
        bool multiline,
        int? minLines,
        int? maxLines,
        bool expands,
        Color selectionColor,
        Color cursorColor,
        double cursorWidth,
        double? cursorHeight,
        Radius cursorRadius,
        BoxHeightStyle selectionHeightStyle,
        BoxWidthStyle selectionWidthStyle,
        double cursorOpacity,
        Point cursorOffset,
        bool paintCursorAboveText,
        bool showCursor,
        IReadOnlyList<SuggestionSpan>? suggestionSpans,
        Color misspelledColor,
        Key? key = null) : base(key)
    {
        Text = text;
        Selection = selection;
        Composing = composing;
        StartHandleLayerLink = startHandleLayerLink;
        EndHandleLayerLink = endHandleLayerLink;
        ToolbarLayerLink = toolbarLayerLink;
        FontFamily = fontFamily;
        FontSize = fontSize;
        Color = color;
        FontWeight = fontWeight;
        FontStyle = fontStyle;
        Height = height;
        LetterSpacing = letterSpacing;
        TextAlign = textAlign;
        TextDirection = textDirection;
        Multiline = multiline;
        MinLines = minLines;
        MaxLines = maxLines;
        Expands = expands;
        SelectionColor = selectionColor;
        CursorColor = cursorColor;
        CursorWidth = cursorWidth;
        CursorHeight = cursorHeight;
        CursorRadius = cursorRadius;
        SelectionHeightStyle = selectionHeightStyle;
        SelectionWidthStyle = selectionWidthStyle;
        CursorOpacity = cursorOpacity;
        CursorOffset = cursorOffset;
        PaintCursorAboveText = paintCursorAboveText;
        ShowCursor = showCursor;
        SuggestionSpans = suggestionSpans ?? [];
        MisspelledColor = misspelledColor;
    }

    public string Text { get; }
    public TextSelection Selection { get; }
    public TextRange? Composing { get; }
    public LayerLink StartHandleLayerLink { get; }
    public LayerLink EndHandleLayerLink { get; }
    public LayerLink ToolbarLayerLink { get; }
    public FontFamily? FontFamily { get; }
    public double FontSize { get; }
    public Color Color { get; }
    public FontWeight? FontWeight { get; }
    public FontStyle? FontStyle { get; }
    public double? Height { get; }
    public double? LetterSpacing { get; }
    public TextAlign TextAlign { get; }
    public TextDirection TextDirection { get; }
    public bool Multiline { get; }
    public int? MinLines { get; }
    public int? MaxLines { get; }
    public bool Expands { get; }
    public Color SelectionColor { get; }
    public Color CursorColor { get; }
    public double CursorWidth { get; }
    public double? CursorHeight { get; }
    public Radius CursorRadius { get; }
    public BoxHeightStyle SelectionHeightStyle { get; }
    public BoxWidthStyle SelectionWidthStyle { get; }
    public double CursorOpacity { get; }
    public Point CursorOffset { get; }
    public bool PaintCursorAboveText { get; }
    public bool ShowCursor { get; }
    public IReadOnlyList<SuggestionSpan> SuggestionSpans { get; }
    public Color MisspelledColor { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        var render = new RenderEditable(StartHandleLayerLink, EndHandleLayerLink, ToolbarLayerLink);
        Apply(render);
        return render;
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        Apply((RenderEditable)renderObject);
    }

    private void Apply(RenderEditable render)
    {
        render.Text = Text;
        render.Selection = Selection;
        render.Composing = Composing;
        render.StartHandleLayerLink = StartHandleLayerLink;
        render.EndHandleLayerLink = EndHandleLayerLink;
        render.ToolbarLayerLink = ToolbarLayerLink;
        render.FontFamily = FontFamily ?? Avalonia.Media.FontFamily.Default;
        render.FontSize = FontSize;
        render.Foreground = new SolidColorBrush(Color);
        render.FontWeight = FontWeight ?? Avalonia.Media.FontWeight.Normal;
        render.FontStyle = FontStyle ?? Avalonia.Media.FontStyle.Normal;
        render.Height = Height;
        render.LetterSpacing = LetterSpacing ?? 0.0;
        render.TextAlign = TextAlign;
        render.TextDirection = TextDirection;
        render.Multiline = Multiline;
        render.MinLines = MinLines;
        render.MaxLines = MaxLines;
        render.Expands = Expands;
        render.SelectionColor = SelectionColor;
        render.CursorColor = CursorColor;
        render.CursorWidth = CursorWidth;
        render.CursorHeight = CursorHeight;
        render.CursorRadius = CursorRadius;
        render.SelectionHeightStyle = SelectionHeightStyle;
        render.SelectionWidthStyle = SelectionWidthStyle;
        render.CursorOpacity = CursorOpacity;
        render.CursorOffset = CursorOffset;
        render.PaintCursorAboveText = PaintCursorAboveText;
        render.ShowCursor = ShowCursor;
        render.SuggestionSpans = SuggestionSpans;
        render.MisspelledColor = MisspelledColor;
    }
}
