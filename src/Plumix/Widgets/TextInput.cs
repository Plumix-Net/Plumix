using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System.Globalization;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/editable_text.dart; flutter/packages/flutter/lib/src/widgets/text_field.dart (adapted)

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
        var clampedBaseOffset = Math.Clamp(BaseOffset, 0, textLength);
        var clampedExtentOffset = Math.Clamp(ExtentOffset, 0, textLength);
        return new TextSelection(clampedBaseOffset, clampedExtentOffset);
    }
}

public readonly record struct TextRange(int Start, int End)
{
    public bool IsCollapsed => Start == End;

    internal TextRange Clamp(int textLength)
    {
        return new TextRange(
            Start: Math.Clamp(Start, 0, textLength),
            End: Math.Clamp(End, 0, textLength));
    }
}

public sealed class TextEditingController : ChangeNotifier
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
            var next = value ?? string.Empty;
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
        var normalizedText = text ?? string.Empty;
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

        var nextExtentOffset = FindPreviousTextElementBoundary(_selection.ExtentOffset);
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

        var nextExtentOffset = FindNextTextElementBoundary(_selection.ExtentOffset);
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

        var nextExtentOffset = FindPreviousWordBoundary(_selection.ExtentOffset);
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

        var nextExtentOffset = FindNextWordBoundary(_selection.ExtentOffset, includeWordAfterSeparator: false);
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

        var nextExtentOffset = FindParagraphStart(_selection.ExtentOffset);
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

        var nextExtentOffset = FindParagraphEnd(_selection.ExtentOffset);
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

        var start = _selection.Start;
        var end = _selection.End;
        var nextText = _text[..start] + text + _text[end..];
        var caretOffset = start + text.Length;
        return ApplyAndReportChange(
            text: nextText,
            selection: TextSelection.Collapsed(caretOffset),
            composing: null);
    }

    public bool SetComposing(string text)
    {
        var composingText = text ?? string.Empty;
        var rangeStart = _composing?.Start ?? _selection.Start;
        var rangeEnd = _composing?.End ?? _selection.End;
        var clampedStart = Math.Clamp(rangeStart, 0, _text.Length);
        var clampedEnd = Math.Clamp(rangeEnd, 0, _text.Length);

        var nextText = _text[..clampedStart] + composingText + _text[clampedEnd..];
        var composingEnd = clampedStart + composingText.Length;
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

        var composingText = text ?? string.Empty;
        var currentComposing = _composing.Value.Clamp(_text.Length);
        var nextText = _text[..currentComposing.Start] + composingText + _text[currentComposing.End..];
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

        var start = _selection.Start;
        var end = _selection.End;
        if (start == end)
        {
            start = FindPreviousTextElementBoundary(end);
        }

        var nextText = _text[..start] + _text[end..];
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

        var end = _selection.ExtentOffset;
        if (end <= 0)
        {
            return false;
        }

        var start = FindPreviousWordBoundary(end);
        if (start >= end)
        {
            return false;
        }

        var nextText = _text[..start] + _text[end..];
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

        var start = _selection.Start;
        var end = _selection.End;
        if (start == end)
        {
            end = FindNextTextElementBoundary(start);
        }

        var nextText = _text[..start] + _text[end..];
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

        var start = _selection.ExtentOffset;
        if (start >= _text.Length)
        {
            return false;
        }

        var end = FindNextWordBoundary(start, includeWordAfterSeparator: true);
        if (end <= start)
        {
            return false;
        }

        var nextText = _text[..start] + _text[end..];
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
            var start = _selection.Start;
            var end = _selection.End;
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
        var index = Math.Clamp(offset, 0, _text.Length);
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
        var index = Math.Clamp(offset, 0, _text.Length);
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
        var index = Math.Clamp(offset, 0, _text.Length);
        if (index <= 0 || string.IsNullOrEmpty(_text))
        {
            return 0;
        }

        var boundaries = StringInfo.ParseCombiningCharacters(_text);
        if (boundaries.Length == 0)
        {
            return Math.Max(0, index - 1);
        }

        var previous = 0;
        foreach (var boundary in boundaries)
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
        var index = Math.Clamp(offset, 0, _text.Length);
        if (index >= _text.Length || string.IsNullOrEmpty(_text))
        {
            return _text.Length;
        }

        var boundaries = StringInfo.ParseCombiningCharacters(_text);
        if (boundaries.Length == 0)
        {
            return Math.Min(_text.Length, index + 1);
        }

        for (var i = 0; i < boundaries.Length; i++)
        {
            var start = boundaries[i];
            var end = i + 1 < boundaries.Length ? boundaries[i + 1] : _text.Length;
            if (index < end)
            {
                return end;
            }
        }

        return _text.Length;
    }

    private int FindParagraphStart(int offset)
    {
        var index = Math.Clamp(offset, 0, _text.Length);
        if (index <= 0)
        {
            return 0;
        }

        var searchFrom = index - 1;
        if (searchFrom >= 0 && _text[searchFrom] == '\n')
        {
            searchFrom -= 1;
        }

        if (searchFrom < 0)
        {
            return 0;
        }

        var lastNewline = _text.LastIndexOf('\n', searchFrom);
        return lastNewline < 0 ? 0 : lastNewline + 1;
    }

    private int FindParagraphEnd(int offset)
    {
        var index = Math.Clamp(offset, 0, _text.Length);
        if (index >= _text.Length)
        {
            return _text.Length;
        }

        var searchFrom = _text[index] == '\n' ? index + 1 : index;
        if (searchFrom >= _text.Length)
        {
            return _text.Length;
        }

        var nextNewline = _text.IndexOf('\n', searchFrom);
        return nextNewline < 0 ? _text.Length : nextNewline;
    }

    private bool ApplyAndReportChange(
        string text,
        TextSelection selection,
        TextRange? composing)
    {
        var previousText = _text;
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
        Thickness? padding = null,
        Key? key = null) : base(key)
    {
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
        Padding = padding ?? new Thickness(8, 6);
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

    public Thickness Padding { get; }

    public override State CreateState()
    {
        return new EditableTextState();
    }

    private sealed class EditableTextState : State
    {
        private TextEditingController? _controller;
        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private double? _verticalNavigationX;
        private int? _verticalNavigationColumn;

        private EditableText Widget => (EditableText)Element.Widget;

        public override void InitState()
        {
            AttachController(Widget.Controller);
            AttachFocusNode(Widget.FocusNode);
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
        }

        public override void Dispose()
        {
            DetachController();
            DetachFocusNode(disposeOwned: true);
        }

        public override Widget Build(BuildContext context)
        {
            var text = _controller!.Text;
            var showPlaceholder = string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(Widget.Placeholder);
            var displayText = BuildDisplayText(
                text: text,
                showPlaceholder: showPlaceholder,
                placeholder: Widget.Placeholder,
                hasFocus: _focusNode!.HasFocus,
                selection: _controller.Selection,
                composing: _controller.Composing);
            var textColor = showPlaceholder ? Widget.PlaceholderColor : Widget.TextColor;
            var backgroundColor = _focusNode!.HasFocus ? Widget.FocusedBackgroundColor : Widget.BackgroundColor;

            return new Focus(
                focusNode: _focusNode,
                autofocus: Widget.Autofocus,
                canRequestFocus: Widget.Enabled,
                onKeyEvent: HandleKeyEvent,
                onTextInput: HandleTextInput,
                onTextComposition: HandleTextComposition,
                onTextInputState: HandleTextInputState,
                onTextSelectionChanged: HandleTextSelectionChanged,
                child: new Container(
                    color: backgroundColor,
                    padding: Widget.Padding,
                    child: new Text(displayText, fontSize: Widget.FontSize, color: textColor)));
        }

        private void AttachController(TextEditingController controller)
        {
            _controller = controller;
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

        private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
        {
            if (!Widget.Enabled || !@event.IsDown)
            {
                return KeyEventResult.Ignored;
            }

            var controller = _controller!;
            var key = @event.Key;
            var textChanged = false;
            var keepVerticalNavigationX = false;
            var isEditingShortcut = @event.IsControlPressed || @event.IsMetaPressed;
            var isWordShortcut = @event.IsControlPressed || @event.IsAltPressed;
            var isParagraphShortcut = Widget.Multiline && isWordShortcut;

            if (isEditingShortcut && string.Equals(key, "A", StringComparison.Ordinal))
            {
                _ = controller.SelectAll();
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
                    textChanged = controller.DeleteBackward();
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
                var pasteText = TextClipboard.GetText() ?? string.Empty;
                if (!string.IsNullOrEmpty(pasteText))
                {
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
                textChanged = isWordShortcut
                    ? controller.DeleteBackwardByWord()
                    : controller.DeleteBackward();
            }
            else if (string.Equals(key, "Delete", StringComparison.Ordinal))
            {
                textChanged = isWordShortcut
                    ? controller.DeleteForwardByWord()
                    : controller.DeleteForward();
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
            else if (Widget.Multiline
                     && (string.Equals(key, "Enter", StringComparison.Ordinal)
                         || string.Equals(key, "Return", StringComparison.Ordinal)))
            {
                textChanged = controller.Insert("\n");
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
            if (!Widget.Enabled || string.IsNullOrEmpty(text))
            {
                return false;
            }

            var normalizedInput = Widget.Multiline
                ? text
                : text.Replace("\r", string.Empty, StringComparison.Ordinal)
                    .Replace("\n", string.Empty, StringComparison.Ordinal);
            if (string.IsNullOrEmpty(normalizedInput))
            {
                return false;
            }

            var changed = _controller!.Composing.HasValue
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
            if (!Widget.Enabled)
            {
                return false;
            }

            var changed = isCommit
                ? _controller!.CommitComposing(text)
                : _controller!.SetComposing(text);
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
            var text = controller.Text;
            var selection = controller.Selection.Clamp(text.Length);
            var cursorRectangle = ResolveCursorRectangle(node, text.Length, selection.ExtentOffset);
            return new FocusTextInputState(
                SurroundingText: text,
                SelectionBaseOffset: selection.BaseOffset,
                SelectionExtentOffset: selection.ExtentOffset,
                CursorRectangle: cursorRectangle);
        }

        private bool HandleTextSelectionChanged(FocusNode node, int baseOffset, int extentOffset)
        {
            if (!Widget.Enabled)
            {
                return false;
            }

            var controller = _controller!;
            var textLength = controller.Text.Length;
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
                    var clampedCaretOffset = Math.Clamp(caretOffset, 0, textLength);
                    var hitRect = layout!.HitTestTextPosition(clampedCaretOffset);
                    var caretHeight = Math.Max(1, hitRect.Height);
                    return new Rect(
                        x: contentRect.X + hitRect.X,
                        y: contentRect.Y + hitRect.Y,
                        width: 1,
                        height: caretHeight);
                }
            }

            var clampedCaretForFallback = Math.Clamp(caretOffset, 0, textLength);
            var fallbackX = contentRect.X + Math.Min(contentRect.Width, clampedCaretForFallback * Math.Max(1, Widget.FontSize * 0.6));
            var fallbackHeight = Math.Max(1, Math.Min(contentRect.Height, Widget.FontSize * 1.2));
            return new Rect(fallbackX, contentRect.Y, 1, fallbackHeight);
        }

        private bool MoveCaretVertical(bool moveDown, bool extendSelection)
        {
            if (!Widget.Multiline)
            {
                return false;
            }

            var controller = _controller!;
            var text = controller.Text;
            if (!TryCreateTextLayout(_focusNode!, text, out var layout, out _))
            {
                return MoveCaretVerticalByLineModel(controller, text, moveDown, extendSelection);
            }

            using (layout!)
            {
                var clampedSelection = controller.Selection.Clamp(text.Length);
                var caretOffset = clampedSelection.ExtentOffset;
                var caretRect = layout!.HitTestTextPosition(caretOffset);
                var maxX = Math.Max(0, layout.WidthIncludingTrailingWhitespace);
                var targetX = Math.Clamp(_verticalNavigationX ?? caretRect.X, 0, maxX);
                var probeDelta = Math.Max(1, caretRect.Height * 0.5);
                var probeY = moveDown
                    ? caretRect.Y + caretRect.Height + probeDelta
                    : caretRect.Y - probeDelta;
                var hit = layout.HitTestPoint(new Point(targetX, probeY));
                var nextOffset = Math.Clamp(
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
            var caretOffset = clampedSelection.ExtentOffset;
            var lineStarts = new List<int> { 0 };
            for (var index = 0; index < text.Length; index++)
            {
                if (text[index] == '\n')
                {
                    lineStarts.Add(index + 1);
                }
            }

            var currentLineIndex = 0;
            for (var index = 1; index < lineStarts.Count; index++)
            {
                if (lineStarts[index] > caretOffset)
                {
                    break;
                }

                currentLineIndex = index;
            }

            var targetLineIndex = moveDown ? currentLineIndex + 1 : currentLineIndex - 1;
            if (targetLineIndex < 0 || targetLineIndex >= lineStarts.Count)
            {
                return false;
            }

            var currentLineStart = lineStarts[currentLineIndex];
            var currentLineEnd = currentLineIndex + 1 < lineStarts.Count
                ? lineStarts[currentLineIndex + 1] - 1
                : text.Length;
            var currentLineColumn = Math.Clamp(caretOffset - currentLineStart, 0, currentLineEnd - currentLineStart);
            var preferredColumn = _verticalNavigationColumn ?? currentLineColumn;

            var targetLineStart = lineStarts[targetLineIndex];
            var targetLineEnd = targetLineIndex + 1 < lineStarts.Count
                ? lineStarts[targetLineIndex + 1] - 1
                : text.Length;
            var targetLineLength = Math.Max(0, targetLineEnd - targetLineStart);
            var nextOffset = targetLineStart + Math.Min(preferredColumn, targetLineLength);
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
            var maxWidth = Widget.Multiline
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
            SetState(static () => { });
        }

        private void HandleFocusNodeChanged()
        {
            _verticalNavigationX = null;
            _verticalNavigationColumn = null;
            SetState(static () => { });
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
                var caretOffset = clampedSelection.ExtentOffset;
                return text[..caretOffset] + "|" + text[caretOffset..];
            }

            var start = clampedSelection.Start;
            var end = clampedSelection.End;
            return text[..start] + "[" + text[start..end] + "]" + text[end..];
        }
    }
}
