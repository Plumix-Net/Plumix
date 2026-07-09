using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System.Globalization;
using Plumix.Foundation;
using Plumix.Rendering;
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
        int clampedBaseOffset = Math.Clamp(BaseOffset, 0, textLength);
        int clampedExtentOffset = Math.Clamp(ExtentOffset, 0, textLength);
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
        bool canRequestFocus = true,
        FocusOnKeyEventCallback? onKeyEvent = null,
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
        CanRequestFocus = canRequestFocus;
        OnKeyEvent = onKeyEvent;
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
    public bool CanRequestFocus { get; }
    public FocusOnKeyEventCallback? OnKeyEvent { get; }

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
            string text = _controller!.Text;
            bool showPlaceholder = string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(Widget.Placeholder);
            string renderedText = Widget.ObscureText
                ? new string(Widget.ObscuringCharacter[0], text.Length)
                : text;
            string displayText = BuildDisplayText(
                text: renderedText,
                showPlaceholder: showPlaceholder,
                placeholder: Widget.Placeholder,
                hasFocus: _focusNode!.HasFocus,
                selection: _controller.Selection,
                composing: _controller.Composing);
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
                    child: new Text(
                        displayText,
                        fontFamily: style?.FontFamily,
                        fontSize: style?.FontSize ?? Widget.FontSize,
                        color: style?.Color ?? textColor,
                        fontWeight: style?.FontWeight,
                        fontStyle: style?.FontStyle,
                        height: style?.Height,
                        letterSpacing: style?.LetterSpacing,
                        textAlign: Widget.TextAlign,
                        textDirection: Widget.TextDirection ?? Directionality.Of(context),
                        softWrap: Widget.Multiline)));
            return new Semantics(
                label: Widget.SemanticsLabel,
                flags: SemanticsFlags.IsTextField
                       | (Widget.Enabled ? SemanticsFlags.IsEnabled : SemanticsFlags.None)
                       | (_focusNode.HasFocus ? SemanticsFlags.IsFocused : SemanticsFlags.None),
                onTap: Widget.Enabled ? () => _focusNode.RequestFocus() : null,
                child: result);
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
                CursorRectangle: cursorRectangle);
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
            SetState(static () => { });
        }

        private void HandleFocusNodeChanged()
        {
            _verticalNavigationX = null;
            _verticalNavigationColumn = null;
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
    }
}
