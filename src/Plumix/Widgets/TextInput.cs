using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System.Globalization;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/editable_text.dart

namespace Plumix.Widgets;

public readonly record struct TextSelection(int BaseOffset, int ExtentOffset)
{
    public int Start => Math.Min(BaseOffset, ExtentOffset);

    public int End => Math.Max(BaseOffset, ExtentOffset);

    public bool IsCollapsed => BaseOffset == ExtentOffset;

    public static TextSelection Collapsed(int offset)
    {
        return new TextSelection(offset, offset);
    }

    internal TextSelection Clamp(int textLength)
    {
        int clampedBaseOffset = Math.Clamp(BaseOffset, 0, textLength);
        int clampedExtentOffset = Math.Clamp(ExtentOffset, 0, textLength);
        return new TextSelection(clampedBaseOffset, clampedExtentOffset);
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
}

public readonly record struct TextEditingValue
{
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
}

public class TextEditingController : ChangeNotifier
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

public sealed class EditableText : StatefulWidget
{
    public EditableText(
        TextEditingController controller,
        FocusNode? focusNode = null,
        string? placeholder = null,
        Action<string>? onChanged = null,
        bool autofocus = false,
        bool enabled = true,
        bool multiline = false,
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
        bool readOnly = false,
        bool obscureText = false,
        string obscuringCharacter = "•",
        int? maxLength = null,
        Action? onEditingComplete = null,
        Action<string>? onSubmitted = null,
        string? semanticsLabel = null,
        TextAlign textAlign = TextAlign.Start,
        TextDirection? textDirection = null,
        TextInputKeyboardType keyboardType = TextInputKeyboardType.Text,
        TextInputActionType textInputAction = TextInputActionType.Unspecified,
        TextCapitalization textCapitalization = TextCapitalization.None,
        SmartDashesType? smartDashesType = null,
        SmartQuotesType? smartQuotesType = null,
        Thickness? scrollPadding = null,
        bool autocorrect = true,
        bool enableSuggestions = true,
        bool canRequestFocus = true,
        FocusOnKeyEventCallback? onKeyEvent = null,
        bool enableInteractiveSelection = true,
        EditableTextContextMenuBuilder? contextMenuBuilder = null,
        TextMagnifierConfiguration? magnifierConfiguration = null,
        TextSelectionControls? selectionControls = null,
        bool showSelectionHandles = false,
        SpellCheckConfiguration? spellCheckConfiguration = null,
        Action<TextSelection, SelectionChangedCause?>? onSelectionChanged = null,
        bool rendererIgnoresPointer = false,
        Key? key = null) : base(key)
    {
        if (string.IsNullOrEmpty(obscuringCharacter) || obscuringCharacter.Length != 1)
            throw new ArgumentException("obscuringCharacter must contain exactly one UTF-16 character.", nameof(obscuringCharacter));
        if (maxLength.HasValue && maxLength.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength));
        Controller = controller;
        FocusNode = focusNode;
        Placeholder = placeholder;
        OnChanged = onChanged;
        Autofocus = autofocus;
        Enabled = enabled;
        Multiline = multiline;
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
        ReadOnly = readOnly;
        ObscureText = obscureText;
        ObscuringCharacter = obscuringCharacter;
        MaxLength = maxLength;
        OnEditingComplete = onEditingComplete;
        OnSubmitted = onSubmitted;
        SemanticsLabel = semanticsLabel;
        TextAlign = textAlign;
        TextDirection = textDirection;
        KeyboardType = keyboardType;
        TextInputAction = textInputAction;
        TextCapitalization = textCapitalization;
        SmartDashesType = smartDashesType ?? (obscureText ? SmartDashesType.Disabled : SmartDashesType.Enabled);
        SmartQuotesType = smartQuotesType ?? (obscureText ? SmartQuotesType.Disabled : SmartQuotesType.Enabled);
        ScrollPadding = scrollPadding ?? new Thickness(20);
        Autocorrect = autocorrect;
        EnableSuggestions = enableSuggestions;
        CanRequestFocus = canRequestFocus;
        OnKeyEvent = onKeyEvent;
        EnableInteractiveSelection = enableInteractiveSelection;
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
    }

    public TextEditingController Controller { get; }

    public FocusNode? FocusNode { get; }

    public string? Placeholder { get; }

    public Action<string>? OnChanged { get; }

    public bool Autofocus { get; }

    public bool Enabled { get; }

    public bool Multiline { get; }

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
    public bool ReadOnly { get; }
    public bool ObscureText { get; }
    public string ObscuringCharacter { get; }
    public int? MaxLength { get; }
    public Action? OnEditingComplete { get; }
    public Action<string>? OnSubmitted { get; }
    public string? SemanticsLabel { get; }
    public TextAlign TextAlign { get; }
    public TextDirection? TextDirection { get; }
    public TextInputKeyboardType KeyboardType { get; }
    public TextInputActionType TextInputAction { get; }
    public TextCapitalization TextCapitalization { get; }
    public SmartDashesType SmartDashesType { get; }
    public SmartQuotesType SmartQuotesType { get; }
    public Thickness ScrollPadding { get; }
    public bool Autocorrect { get; }
    public bool EnableSuggestions { get; }
    public bool CanRequestFocus { get; }
    public FocusOnKeyEventCallback? OnKeyEvent { get; }
    public bool EnableInteractiveSelection { get; }
    public EditableTextContextMenuBuilder? ContextMenuBuilder { get; }
    public TextMagnifierConfiguration MagnifierConfiguration { get; }
    public TextSelectionControls? SelectionControls { get; }
    public bool ShowSelectionHandles { get; }
    public SpellCheckConfiguration? SpellCheckConfiguration { get; }
    public Action<TextSelection, SelectionChangedCause?>? OnSelectionChanged { get; }
    public bool RendererIgnoresPointer { get; }

    public override State CreateState()
    {
        return new EditableTextState();
    }

    public sealed class EditableTextState : State, ITextSelectionDelegate
    {
        private TextEditingController? _controller;
        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private double? _verticalNavigationX;
        private int? _verticalNavigationColumn;
        private readonly ContextMenuController _contextMenuController = new();
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
                if (!Widget.ReadOnly && hasSelection)
                {
                    items.Add(new ContextMenuButtonItem(CutAndHide, ContextMenuButtonType.Cut));
                }
                if (hasSelection)
                {
                    items.Add(new ContextMenuButtonItem(CopyAndHide, ContextMenuButtonType.Copy));
                }
                if (!Widget.ReadOnly && !string.IsNullOrEmpty(TextClipboard.GetText()))
                {
                    items.Add(new ContextMenuButtonItem(PasteAndHide, ContextMenuButtonType.Paste));
                }
                if (!selectionCoversAll && controller.Text.Length > 0)
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

        public bool ContextMenuIsVisible => _contextMenuController.IsShown;

        public TextEditingValue CurrentTextEditingValue => _controller!.Value;

        public SpellCheckResults? SpellCheckResults => _spellCheckResults;

        public TextSelectionOverlay? SelectionOverlay => _selectionOverlay;

        public override void InitState()
        {
            AttachController(Widget.Controller);
            AttachFocusNode(Widget.FocusNode);
            _lastSelection = Widget.Controller.Selection;
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
            if (!Widget.EnableInteractiveSelection
                || oldEditableText.ContextMenuBuilder != Widget.ContextMenuBuilder)
            {
                HideToolbar();
            }
        }

        public override void Dispose()
        {
            _selectionOverlay?.Dispose();
            _selectionOverlay = null;
            _clipboardStatus.Dispose();
            _contextMenuController.Hide();
            DetachController();
            DetachFocusNode(disposeOwned: true);
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

            var style = Widget.Style;
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
                        showCursor: _focusNode.HasFocus && !showPlaceholder,
                        suggestionSpans: _spellCheckResults?.SuggestionSpans,
                        misspelledColor: Widget.SpellCheckConfiguration?.MisspelledTextStyle?.Color ?? Colors.Red,
                        key: _editableRenderKey)));
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
            _contextMenuController.Hide();
            _selectionOverlay?.HideToolbar();
            if (hideHandles) _selectionOverlay?.HideHandles();
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

            bool textChanged = !string.Equals(controller.Text, value.Text, StringComparison.Ordinal);
            controller.Value = value;
            _pendingSelectionCause = null;
            if (textChanged)
            {
                Widget.OnChanged?.Invoke(controller.Text);
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
                TextClipboard.SetText(controller.SelectedText);
                _pendingSelectionCause = SelectionChangedCause.Toolbar;
                if (controller.DeleteBackward())
                {
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
                _pendingSelectionCause = SelectionChangedCause.Toolbar;
                if (controller.Insert(LimitInsertion(value)))
                {
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

            if (!Widget.Enabled || !@event.IsDown)
            {
                return KeyEventResult.Ignored;
            }

            var controller = _controller!;
            string key = @event.Key;
            bool textChanged = false;
            bool keepVerticalNavigationX = false;
            bool isEditingShortcut = @event.IsControlPressed || @event.IsMetaPressed;
            bool isWordShortcut = @event.IsControlPressed || @event.IsAltPressed;
            bool isParagraphShortcut = Widget.Multiline && isWordShortcut;

            if (isEditingShortcut && string.Equals(key, "A", StringComparison.Ordinal))
            {
                _pendingSelectionCause = SelectionChangedCause.Keyboard;
                _ = controller.SelectAll();
                _pendingSelectionCause = null;
                _verticalNavigationX = null;
                _verticalNavigationColumn = null;
                return KeyEventResult.Handled;
            }

            if (isEditingShortcut && string.Equals(key, "C", StringComparison.Ordinal))
            {
                if (!controller.Selection.IsCollapsed)
                {
                    TextClipboard.SetText(controller.SelectedText);
                }

                _verticalNavigationX = null;
                _verticalNavigationColumn = null;
                return KeyEventResult.Handled;
            }

            if (isEditingShortcut && string.Equals(key, "X", StringComparison.Ordinal))
            {
                if (!controller.Selection.IsCollapsed)
                {
                    TextClipboard.SetText(controller.SelectedText);
                    textChanged = !Widget.ReadOnly && controller.DeleteBackward();
                    if (textChanged)
                    {
                        Widget.OnChanged?.Invoke(controller.Text);
                    }
                }

                _verticalNavigationX = null;
                _verticalNavigationColumn = null;
                return KeyEventResult.Handled;
            }

            if (isEditingShortcut && string.Equals(key, "V", StringComparison.Ordinal))
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
                        Widget.OnChanged?.Invoke(controller.Text);
                    }
                }

                _verticalNavigationX = null;
                _verticalNavigationColumn = null;
                return KeyEventResult.Handled;
            }

            if (string.Equals(key, "Back", StringComparison.Ordinal)
                || string.Equals(key, "Backspace", StringComparison.Ordinal))
            {
                textChanged = !Widget.ReadOnly && (isWordShortcut
                    ? controller.DeleteBackwardByWord()
                    : controller.DeleteBackward());
            }
            else if (string.Equals(key, "Delete", StringComparison.Ordinal))
            {
                textChanged = !Widget.ReadOnly && (isWordShortcut
                    ? controller.DeleteForwardByWord()
                    : controller.DeleteForward());
            }
            else if (string.Equals(key, "ArrowLeft", StringComparison.Ordinal)
                     || string.Equals(key, "Left", StringComparison.Ordinal))
            {
                _ = isWordShortcut
                    ? controller.MoveCaretToPreviousWord(extendSelection: @event.IsShiftPressed)
                    : controller.MoveCaretLeft(extendSelection: @event.IsShiftPressed);
            }
            else if (string.Equals(key, "ArrowRight", StringComparison.Ordinal)
                     || string.Equals(key, "Right", StringComparison.Ordinal))
            {
                _ = isWordShortcut
                    ? controller.MoveCaretToNextWord(extendSelection: @event.IsShiftPressed)
                    : controller.MoveCaretRight(extendSelection: @event.IsShiftPressed);
            }
            else if (Widget.Multiline
                     && (string.Equals(key, "ArrowUp", StringComparison.Ordinal)
                         || string.Equals(key, "Up", StringComparison.Ordinal)))
            {
                if (isParagraphShortcut)
                {
                    _ = controller.MoveCaretToParagraphStart(extendSelection: @event.IsShiftPressed);
                }
                else
                {
                    _ = MoveCaretVertical(moveDown: false, extendSelection: @event.IsShiftPressed);
                    keepVerticalNavigationX = true;
                }
            }
            else if (Widget.Multiline
                     && (string.Equals(key, "ArrowDown", StringComparison.Ordinal)
                         || string.Equals(key, "Down", StringComparison.Ordinal)))
            {
                if (isParagraphShortcut)
                {
                    _ = controller.MoveCaretToParagraphEnd(extendSelection: @event.IsShiftPressed);
                }
                else
                {
                    _ = MoveCaretVertical(moveDown: true, extendSelection: @event.IsShiftPressed);
                    keepVerticalNavigationX = true;
                }
            }
            else if (string.Equals(key, "Home", StringComparison.Ordinal))
            {
                _ = controller.MoveCaretToStart(extendSelection: @event.IsShiftPressed);
            }
            else if (string.Equals(key, "End", StringComparison.Ordinal))
            {
                _ = controller.MoveCaretToEnd(extendSelection: @event.IsShiftPressed);
            }
            else if (string.Equals(key, "Enter", StringComparison.Ordinal)
                     || string.Equals(key, "Return", StringComparison.Ordinal))
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
            else if (string.Equals(key, "Escape", StringComparison.Ordinal))
            {
                _ = controller.ClearComposing();
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
            bool changed = _controller!.Composing.HasValue
                ? _controller.CommitComposing(normalizedInput)
                : _controller.Insert(normalizedInput);
            if (changed)
            {
                _verticalNavigationX = null;
                _verticalNavigationColumn = null;
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
            bool changed = isCommit
                ? _controller!.CommitComposing(limitedText)
                : _controller!.SetComposing(limitedText);
            if (changed)
            {
                _verticalNavigationX = null;
                _verticalNavigationColumn = null;
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
                Configuration: new TextInputConfiguration(
                    KeyboardType: Widget.KeyboardType,
                    InputAction: Widget.TextInputAction,
                    Autocorrect: Widget.Autocorrect,
                    EnableSuggestions: Widget.EnableSuggestions,
                    ObscureText: Widget.ObscureText,
                    Multiline: Widget.Multiline,
                    TextCapitalization: Widget.TextCapitalization,
                    SmartDashesType: Widget.SmartDashesType,
                    SmartQuotesType: Widget.SmartQuotesType));
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
            if (_focusNode?.HasFocus == false)
            {
                _selectionOverlay?.Hide();
            }
            SetState(static () => { });
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

        private async Task RequestSpellCheckAsync()
        {
            SpellCheckConfiguration? configuration = Widget.SpellCheckConfiguration;
            if (configuration is not { SpellCheckEnabled: true } || string.IsNullOrEmpty(_controller!.Text)) return;
            ISpellCheckService? service = configuration.SpellCheckService;
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
        Color selectionColor,
        Color cursorColor,
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
        SelectionColor = selectionColor;
        CursorColor = cursorColor;
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
    public Color SelectionColor { get; }
    public Color CursorColor { get; }
    public bool ShowCursor { get; }
    public IReadOnlyList<SuggestionSpan> SuggestionSpans { get; }
    public Color MisspelledColor { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        var render = new RenderEditable(StartHandleLayerLink, EndHandleLayerLink, ToolbarLayerLink);
        Apply(render);
        return render;
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
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
        render.MaxLines = Multiline ? null : 1;
        render.SelectionColor = SelectionColor;
        render.CursorColor = CursorColor;
        render.ShowCursor = ShowCursor;
        render.SuggestionSpans = SuggestionSpans;
        render.MisspelledColor = MisspelledColor;
    }
}
