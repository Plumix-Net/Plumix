using Avalonia.Media;
using System.Globalization;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/text_field.dart

public delegate Widget TextFieldCounterBuilder(
    BuildContext context,
    int currentLength,
    int? maxLength,
    bool isFocused);

public sealed class TextField : StatefulWidget
{
    public const int NoMaxLength = -1;

    public TextField(
        TextEditingController? controller = null,
        FocusNode? focusNode = null,
        InputDecoration? decoration = null,
        bool useDecoration = true,
        TextStyle? style = null,
        TextAlign textAlign = TextAlign.Start,
        TextAlignVertical? textAlignVertical = null,
        TextDirection? textDirection = null,
        TextInputType? keyboardType = null,
        TextInputAction? textInputAction = null,
        bool autocorrect = true,
        bool enableSuggestions = true,
        bool readOnly = false,
        bool autofocus = false,
        string obscuringCharacter = "•",
        bool obscureText = false,
        int? maxLines = 1,
        int? minLines = null,
        bool expands = false,
        int? maxLength = null,
        Action<string>? onChanged = null,
        Action? onEditingComplete = null,
        Action<string>? onSubmitted = null,
        bool? enabled = null,
        Action? onTap = null,
        MouseCursor? mouseCursor = null,
        TextFieldCounterBuilder? buildCounter = null,
        bool canRequestFocus = true,
        FocusOnKeyEventCallback? onKeyEvent = null,
        bool? enableInteractiveSelection = null,
        TextSelectionControls? selectionControls = null,
        EditableTextContextMenuBuilder? contextMenuBuilder = null,
        SpellCheckConfiguration? spellCheckConfiguration = null,
        TextMagnifierConfiguration? magnifierConfiguration = null,
        Action<TextSelection, SelectionChangedCause?>? onSelectionChanged = null,
        Key? key = null) : base(key)
    {
        if (string.IsNullOrEmpty(obscuringCharacter) || obscuringCharacter.Length != 1)
            throw new ArgumentException("obscuringCharacter must contain exactly one UTF-16 character.", nameof(obscuringCharacter));
        if (maxLines.HasValue && maxLines.Value <= 0) throw new ArgumentOutOfRangeException(nameof(maxLines));
        if (minLines.HasValue && minLines.Value <= 0) throw new ArgumentOutOfRangeException(nameof(minLines));
        if (maxLines.HasValue && minLines.HasValue && minLines.Value > maxLines.Value)
            throw new ArgumentException("minLines cannot be greater than maxLines.", nameof(minLines));
        if (expands && (maxLines.HasValue || minLines.HasValue))
            throw new ArgumentException("minLines and maxLines must be null when expands is true.", nameof(expands));
        if (obscureText && maxLines != 1) throw new ArgumentException("Obscured fields cannot be multiline.", nameof(obscureText));
        if (maxLength.HasValue && maxLength.Value != NoMaxLength && maxLength.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength));

        Controller = controller;
        FocusNode = focusNode;
        Decoration = useDecoration ? decoration ?? new InputDecoration() : null;
        Style = style;
        TextAlign = textAlign;
        TextAlignVertical = textAlignVertical;
        TextDirection = textDirection;
        KeyboardType = keyboardType;
        TextInputAction = textInputAction;
        Autocorrect = autocorrect;
        EnableSuggestions = enableSuggestions;
        ReadOnly = readOnly;
        Autofocus = autofocus;
        ObscuringCharacter = obscuringCharacter;
        ObscureText = obscureText;
        MaxLines = maxLines;
        MinLines = minLines;
        Expands = expands;
        MaxLength = maxLength;
        OnChanged = onChanged;
        OnEditingComplete = onEditingComplete;
        OnSubmitted = onSubmitted;
        Enabled = enabled;
        OnTap = onTap;
        MouseCursor = mouseCursor;
        BuildCounter = buildCounter;
        CanRequestFocus = canRequestFocus;
        OnKeyEvent = onKeyEvent;
        EnableInteractiveSelection = enableInteractiveSelection ?? (!readOnly || !obscureText);
        SelectionControls = selectionControls;
        ContextMenuBuilder = contextMenuBuilder ?? DefaultContextMenuBuilder;
        SpellCheckConfiguration = spellCheckConfiguration;
        MagnifierConfiguration = magnifierConfiguration ?? TextMagnifier.AdaptiveMagnifierConfiguration;
        OnSelectionChanged = onSelectionChanged;
    }

    public TextEditingController? Controller { get; }
    public FocusNode? FocusNode { get; }
    public InputDecoration? Decoration { get; }
    public TextStyle? Style { get; }
    public TextAlign TextAlign { get; }
    public TextAlignVertical? TextAlignVertical { get; }
    public TextDirection? TextDirection { get; }
    public TextInputType? KeyboardType { get; }
    public TextInputAction? TextInputAction { get; }
    public bool Autocorrect { get; }
    public bool EnableSuggestions { get; }
    public bool ReadOnly { get; }
    public bool Autofocus { get; }
    public string ObscuringCharacter { get; }
    public bool ObscureText { get; }
    public int? MaxLines { get; }
    public int? MinLines { get; }
    public bool Expands { get; }
    public int? MaxLength { get; }
    public Action<string>? OnChanged { get; }
    public Action? OnEditingComplete { get; }
    public Action<string>? OnSubmitted { get; }
    public bool? Enabled { get; }
    public Action? OnTap { get; }
    public MouseCursor? MouseCursor { get; }
    public TextFieldCounterBuilder? BuildCounter { get; }
    public bool CanRequestFocus { get; }
    public FocusOnKeyEventCallback? OnKeyEvent { get; }
    public bool EnableInteractiveSelection { get; }
    public TextSelectionControls? SelectionControls { get; }
    public EditableTextContextMenuBuilder? ContextMenuBuilder { get; }
    public SpellCheckConfiguration? SpellCheckConfiguration { get; }
    public TextMagnifierConfiguration MagnifierConfiguration { get; }
    public Action<TextSelection, SelectionChangedCause?>? OnSelectionChanged { get; }

    public override State CreateState() => new TextFieldState();

    internal static Widget DefaultContextMenuBuilder(
        BuildContext context,
        EditableText.EditableTextState editableTextState)
    {
        return AdaptiveTextSelectionToolbar.EditableText(editableTextState);
    }

    public static TextStyle MaterialMisspelledTextStyle { get; } = new(Color: Colors.Red);

    public static Widget DefaultSpellCheckSuggestionsToolbarBuilder(
        BuildContext context,
        EditableText.EditableTextState editableTextState)
    {
        return SpellCheckSuggestionsToolbar.EditableText(editableTextState);
    }

    public static SpellCheckConfiguration InferAndroidSpellCheckConfiguration(
        SpellCheckConfiguration? configuration)
    {
        if (configuration is null || !configuration.SpellCheckEnabled) return SpellCheckConfiguration.Disabled;
        return configuration.CopyWith(
            misspelledTextStyle: configuration.MisspelledTextStyle ?? MaterialMisspelledTextStyle,
            spellCheckSuggestionsToolbarBuilder: configuration.SpellCheckSuggestionsToolbarBuilder
                                                    ?? DefaultSpellCheckSuggestionsToolbarBuilder);
    }

    private sealed class TextFieldState : State
    {
        private readonly GlobalKey<EditableText.EditableTextState> _editableTextKey =
            new GlobalObjectKey<EditableText.EditableTextState>(new object());
        private TextEditingController? _controller;
        private FocusNode? _focusNode;
        private bool _ownsController;
        private bool _ownsFocusNode;
        private bool _hovering;
        private IDisposable? _cursorHandle;
        private MouseCursor? _resolvedMouseCursor;
        private TextField Current => (TextField)StateWidget;

        public override void InitState()
        {
            AttachController(Current.Controller);
            AttachFocusNode(Current.FocusNode);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (TextField)oldWidget;
            if (!ReferenceEquals(old.Controller, Current.Controller)) { DetachController(); AttachController(Current.Controller); }
            if (!ReferenceEquals(old.FocusNode, Current.FocusNode)) { DetachFocusNode(); AttachFocusNode(Current.FocusNode); }
        }

        public override Widget Build(BuildContext context)
        {
            var theme = Theme.Of(context);
            DefaultSelectionStyle selectionStyle = DefaultSelectionStyle.Of(context);
            TextSelectionThemeData selectionTheme = TextSelectionTheme.Of(context);
            bool enabled = Current.Enabled ?? Current.Decoration?.Enabled ?? true;
            _resolvedMouseCursor = Current.MouseCursor
                                   ?? selectionStyle.MouseCursor
                                   ?? (enabled ? SystemMouseCursors.Text : SystemMouseCursors.Basic);
            Color cursorColor = selectionStyle.CursorColor
                                ?? selectionTheme.CursorColor
                                ?? theme.PrimaryColor;
            Color selectionColor = selectionStyle.SelectionColor
                                   ?? selectionTheme.SelectionColor
                                   ?? ApplyOpacity(theme.PrimaryColor, 0.40);
            var baseStyle = Current.Style ?? (theme.UseMaterial3 ? theme.TextTheme.BodyLarge : theme.TextTheme.TitleMedium);
            if (!enabled && Current.Style?.Color is null)
                baseStyle = baseStyle.CopyWith(color: ApplyOpacity(theme.OnSurfaceColor, 0.38));
            bool multiline = Current.MaxLines != 1;
            int? positiveMaxLength = Current.MaxLength is > 0 ? Current.MaxLength : null;
            SpellCheckConfiguration spellCheckConfiguration = InferAndroidSpellCheckConfiguration(
                Current.SpellCheckConfiguration);
            TextSelectionControls selectionControls = Current.SelectionControls
                                                       ?? MaterialTextSelectionHandleControls.Instance;

            Widget editable = new EditableText(
                controller: _controller!,
                focusNode: _focusNode,
                onChanged: Current.OnChanged,
                autofocus: Current.Autofocus,
                enabled: enabled,
                multiline: multiline,
                fontSize: baseStyle.FontSize ?? 16,
                textColor: baseStyle.Color ?? theme.OnSurfaceColor,
                backgroundColor: Colors.Transparent,
                focusedBackgroundColor: Colors.Transparent,
                cursorColor: cursorColor,
                selectionColor: selectionColor,
                mouseCursor: _resolvedMouseCursor,
                padding: new Avalonia.Thickness(0),
                style: baseStyle,
                readOnly: Current.ReadOnly,
                obscureText: Current.ObscureText,
                obscuringCharacter: Current.ObscuringCharacter,
                maxLength: positiveMaxLength,
                onEditingComplete: Current.OnEditingComplete,
                onSubmitted: Current.OnSubmitted,
                semanticsLabel: Current.Decoration?.LabelText ?? Current.Decoration?.HintText,
                textAlign: Current.TextAlign,
                textDirection: Current.TextDirection,
                keyboardType: ResolveKeyboardType(Current.KeyboardType, multiline),
                textInputAction: ResolveTextInputAction(Current.TextInputAction),
                autocorrect: Current.Autocorrect,
                enableSuggestions: Current.EnableSuggestions,
                canRequestFocus: Current.CanRequestFocus,
                onKeyEvent: Current.OnKeyEvent,
                enableInteractiveSelection: Current.EnableInteractiveSelection,
                contextMenuBuilder: Current.ContextMenuBuilder,
                selectionControls: Current.EnableInteractiveSelection ? selectionControls : null,
                spellCheckConfiguration: spellCheckConfiguration,
                magnifierConfiguration: Current.MagnifierConfiguration,
                onSelectionChanged: Current.OnSelectionChanged,
                rendererIgnoresPointer: true,
                key: _editableTextKey);

            if (multiline && !Current.Expands)
            {
                int lineCount = Current.MinLines ?? Math.Min(Current.MaxLines ?? 3, 3);
                editable = new SizedBox(height: Math.Max(24, lineCount * (baseStyle.FontSize ?? 16) * 1.35), child: editable);
            }

            Widget result;
            if (Current.Decoration is null)
            {
                result = editable;
            }
            else
            {
                string? generatedCounter = Current.MaxLength.HasValue
                    ? Current.MaxLength == NoMaxLength ? TextLength(_controller!.Text).ToString() : $"{TextLength(_controller!.Text)}/{Current.MaxLength}"
                    : null;
                var decoration = Current.Decoration.WithRuntime(enabled, Current.BuildCounter is null ? generatedCounter : null);
                if (Current.BuildCounter is not null)
                {
                    decoration = decoration.WithCounter(
                        Current.BuildCounter(context, TextLength(_controller!.Text), Current.MaxLength, _focusNode!.HasFocus));
                }
                result = new InputDecorator(
                    decoration: decoration,
                    baseStyle: baseStyle,
                    textAlign: Current.TextAlign,
                    textAlignVertical: Current.TextAlignVertical,
                    isFocused: _focusNode!.HasFocus,
                    isHovering: _hovering,
                    expands: Current.Expands,
                    isEmpty: string.IsNullOrEmpty(_controller!.Text),
                    child: editable);
            }

            result = new Listener(
                onPointerDown: @event => _editableTextKey.CurrentState?.HandlePointerDown(@event),
                onPointerMove: @event => _editableTextKey.CurrentState?.HandlePointerMove(@event),
                onPointerUp: @event => _editableTextKey.CurrentState?.HandlePointerUp(@event),
                onPointerCancel: @event => _editableTextKey.CurrentState?.HandlePointerCancel(@event),
                behavior: HitTestBehavior.Translucent,
                child: result);
            result = new GestureDetector(
                onTap: Current.OnTap,
                onDoubleTap: () => _editableTextKey.CurrentState?.HandleDoubleTap(),
                onLongPress: () => _editableTextKey.CurrentState?.HandleLongPress(),
                onSecondaryTap: () => _editableTextKey.CurrentState?.ShowToolbar(),
                behavior: HitTestBehavior.Translucent,
                child: result);
            return new Listener(
                onPointerEnter: _ => BeginHover(),
                onPointerExit: _ => EndHover(),
                onPointerHover: _ => BeginHover(),
                behavior: HitTestBehavior.Translucent,
                child: result);
        }

        public override void Dispose()
        {
            EndHover();
            DetachController();
            DetachFocusNode();
        }

        private void AttachController(TextEditingController? external)
        {
            _controller = external ?? new TextEditingController(); _ownsController = external is null;
            _controller.AddListener(Changed);
        }
        private void DetachController()
        {
            if (_controller is null) return; _controller.RemoveListener(Changed);
            if (_ownsController) _controller.Dispose(); _controller = null; _ownsController = false;
        }
        private void AttachFocusNode(FocusNode? external)
        {
            _focusNode = external ?? new FocusNode(); _ownsFocusNode = external is null; _focusNode.AddListener(Changed);
        }
        private void DetachFocusNode()
        {
            if (_focusNode is null) return; _focusNode.RemoveListener(Changed);
            if (_ownsFocusNode) _focusNode.Dispose(); _focusNode = null; _ownsFocusNode = false;
        }
        private void BeginHover()
        {
            if (!_hovering) SetState(() => _hovering = true);
            bool enabled = Current.Enabled ?? Current.Decoration?.Enabled ?? true;
            _cursorHandle ??= MouseCursorManager.PushCursor(
                _resolvedMouseCursor ?? (enabled ? SystemMouseCursors.Text : SystemMouseCursors.Basic));
        }
        private void EndHover()
        {
            _cursorHandle?.Dispose(); _cursorHandle = null;
            if (_hovering && Mounted) SetState(() => _hovering = false); else _hovering = false;
        }
        private void Changed() { if (Mounted) SetState(() => { }); }
        private static int TextLength(string value) => new StringInfo(value).LengthInTextElements;

        private static TextInputKeyboardType ResolveKeyboardType(TextInputType? keyboardType, bool multiline)
        {
            return keyboardType switch
            {
                TextInputType.Multiline => TextInputKeyboardType.Multiline,
                TextInputType.Number => TextInputKeyboardType.Number,
                TextInputType.Phone => TextInputKeyboardType.Phone,
                TextInputType.Datetime => TextInputKeyboardType.Datetime,
                TextInputType.EmailAddress => TextInputKeyboardType.EmailAddress,
                TextInputType.Url => TextInputKeyboardType.Url,
                TextInputType.Text => TextInputKeyboardType.Text,
                _ => multiline ? TextInputKeyboardType.Multiline : TextInputKeyboardType.Text,
            };
        }

        private static TextInputActionType ResolveTextInputAction(TextInputAction? textInputAction)
        {
            return textInputAction switch
            {
                global::Plumix.Material.TextInputAction.None => TextInputActionType.None,
                global::Plumix.Material.TextInputAction.Search => TextInputActionType.Search,
                global::Plumix.Material.TextInputAction.Done => TextInputActionType.Done,
                global::Plumix.Material.TextInputAction.Go => TextInputActionType.Go,
                global::Plumix.Material.TextInputAction.Next => TextInputActionType.Next,
                global::Plumix.Material.TextInputAction.Send => TextInputActionType.Send,
                _ => TextInputActionType.Unspecified,
            };
        }

        private static Color ApplyOpacity(Color c, double opacity) => Color.FromArgb((byte)Math.Round(c.A * Math.Clamp(opacity, 0, 1)), c.R, c.G, c.B);
    }
}
