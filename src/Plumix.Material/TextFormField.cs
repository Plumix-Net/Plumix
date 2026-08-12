using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/text_form_field.dart

public sealed class TextFormField : FormField<string>
{
    public TextFormField(
        TextEditingController? controller = null,
        string? initialValue = null,
        FocusNode? focusNode = null,
        string? forceErrorText = null,
        InputDecoration? decoration = null,
        TextStyle? style = null,
        TextAlign textAlign = TextAlign.Start,
        TextDirection? textDirection = null,
        TextInputType? keyboardType = null,
        TextInputAction? textInputAction = null,
        bool readOnly = false,
        bool autofocus = false,
        string obscuringCharacter = "•",
        bool obscureText = false,
        int? maxLines = 1,
        int? minLines = null,
        bool expands = false,
        int? maxLength = null,
        Action<string>? onChanged = null,
        Action? onTap = null,
        Action? onEditingComplete = null,
        Action<string>? onFieldSubmitted = null,
        FormFieldSetter<string>? onSaved = null,
        FormFieldValidator<string>? validator = null,
        FormFieldErrorBuilder? errorBuilder = null,
        bool? enabled = null,
        TextFieldCounterBuilder? buildCounter = null,
        AutovalidateMode autovalidateMode = AutovalidateMode.Disabled,
        string? restorationId = null,
        MouseCursor? mouseCursor = null,
        bool canRequestFocus = true,
        bool? enableInteractiveSelection = null,
        EditableTextContextMenuBuilder? contextMenuBuilder = null,
        TextMagnifierConfiguration? magnifierConfiguration = null,
        Action<TextSelection, SelectionChangedCause?>? onSelectionChanged = null,
        Key? key = null)
        : base(
            builder: field => BuildField(
                (TextFormFieldState)field,
                focusNode,
                decoration ?? new InputDecoration(),
                style,
                textAlign,
                textDirection,
                keyboardType,
                textInputAction,
                readOnly,
                autofocus,
                obscuringCharacter,
                obscureText,
                maxLines,
                minLines,
                expands,
                maxLength,
                onChanged,
                onTap,
                onEditingComplete,
                onFieldSubmitted,
                enabled ?? decoration?.Enabled ?? true,
                buildCounter,
                mouseCursor,
                canRequestFocus,
                enableInteractiveSelection,
                contextMenuBuilder,
                magnifierConfiguration,
                onSelectionChanged,
                errorBuilder),
            onSaved: onSaved,
            forceErrorText: forceErrorText,
            validator: validator,
            errorBuilder: errorBuilder,
            initialValue: ResolveInitialValue(controller, initialValue, decoration, errorBuilder),
            enabled: enabled ?? decoration?.Enabled ?? true,
            autovalidateMode: autovalidateMode,
            restorationId: restorationId,
            key: key)
    {
        ValidateTextFieldArguments(obscuringCharacter, maxLines, minLines, expands, obscureText, maxLength);
        Controller = controller;
        InitialText = initialValue;
        FocusNode = focusNode;
        Decoration = decoration ?? new InputDecoration();
        Style = style;
        TextAlign = textAlign;
        TextDirection = textDirection;
        KeyboardType = keyboardType;
        TextInputAction = textInputAction;
        ReadOnly = readOnly;
        Autofocus = autofocus;
        ObscuringCharacter = obscuringCharacter;
        ObscureText = obscureText;
        MaxLines = maxLines;
        MinLines = minLines;
        Expands = expands;
        MaxLength = maxLength;
        OnChanged = onChanged;
        OnTap = onTap;
        OnEditingComplete = onEditingComplete;
        OnFieldSubmitted = onFieldSubmitted;
        ExplicitEnabled = enabled;
        BuildCounter = buildCounter;
        MouseCursor = mouseCursor;
        CanRequestFocus = canRequestFocus;
        EnableInteractiveSelection = enableInteractiveSelection ?? (!readOnly || !obscureText);
        ContextMenuBuilder = contextMenuBuilder ?? TextField.DefaultContextMenuBuilder;
        MagnifierConfiguration = magnifierConfiguration ?? TextMagnifier.AdaptiveMagnifierConfiguration;
        OnSelectionChanged = onSelectionChanged;
    }

    public TextEditingController? Controller { get; }
    public string? InitialText { get; }
    public FocusNode? FocusNode { get; }
    public InputDecoration Decoration { get; }
    public TextStyle? Style { get; }
    public TextAlign TextAlign { get; }
    public TextDirection? TextDirection { get; }
    public TextInputType? KeyboardType { get; }
    public TextInputAction? TextInputAction { get; }
    public bool ReadOnly { get; }
    public bool Autofocus { get; }
    public string ObscuringCharacter { get; }
    public bool ObscureText { get; }
    public int? MaxLines { get; }
    public int? MinLines { get; }
    public bool Expands { get; }
    public int? MaxLength { get; }
    public Action<string>? OnChanged { get; }
    public Action? OnTap { get; }
    public Action? OnEditingComplete { get; }
    public Action<string>? OnFieldSubmitted { get; }
    public bool? ExplicitEnabled { get; }
    public TextFieldCounterBuilder? BuildCounter { get; }
    public MouseCursor? MouseCursor { get; }
    public bool CanRequestFocus { get; }
    public bool EnableInteractiveSelection { get; }
    public EditableTextContextMenuBuilder? ContextMenuBuilder { get; }
    public TextMagnifierConfiguration MagnifierConfiguration { get; }
    public Action<TextSelection, SelectionChangedCause?>? OnSelectionChanged { get; }

    public override State CreateState() => new TextFormFieldState();

    private static string ResolveInitialValue(
        TextEditingController? controller,
        string? initialValue,
        InputDecoration? decoration,
        FormFieldErrorBuilder? errorBuilder)
    {
        if (controller is not null && initialValue is not null)
            throw new ArgumentException("initialValue must be null when controller is provided.", nameof(initialValue));
        if (errorBuilder is not null && decoration?.ErrorText is not null)
            throw new ArgumentException("errorBuilder and decoration.errorText cannot both be specified.", nameof(errorBuilder));
        return controller?.Text ?? initialValue ?? string.Empty;
    }

    private static void ValidateTextFieldArguments(
        string obscuringCharacter,
        int? maxLines,
        int? minLines,
        bool expands,
        bool obscureText,
        int? maxLength)
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
        if (maxLength.HasValue && maxLength.Value != TextField.NoMaxLength && maxLength.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength));
    }

    private static Widget BuildField(
        TextFormFieldState state,
        FocusNode? focusNode,
        InputDecoration decoration,
        TextStyle? style,
        TextAlign textAlign,
        TextDirection? textDirection,
        TextInputType? keyboardType,
        TextInputAction? textInputAction,
        bool readOnly,
        bool autofocus,
        string obscuringCharacter,
        bool obscureText,
        int? maxLines,
        int? minLines,
        bool expands,
        int? maxLength,
        Action<string>? onChanged,
        Action? onTap,
        Action? onEditingComplete,
        Action<string>? onFieldSubmitted,
        bool enabled,
        TextFieldCounterBuilder? buildCounter,
        MouseCursor? mouseCursor,
        bool canRequestFocus,
        bool? enableInteractiveSelection,
        EditableTextContextMenuBuilder? contextMenuBuilder,
        TextMagnifierConfiguration? magnifierConfiguration,
        Action<TextSelection, SelectionChangedCause?>? onSelectionChanged,
        FormFieldErrorBuilder? errorBuilder)
    {
        var effectiveDecoration = decoration;
        if (state.ErrorText is { } errorText)
        {
            var error = errorBuilder?.Invoke(state.Context, errorText);
            effectiveDecoration = effectiveDecoration.WithFormError(errorText, error);
        }

        return new TextField(
            controller: state.EffectiveController,
            focusNode: focusNode,
            decoration: effectiveDecoration,
            style: style,
            textAlign: textAlign,
            textDirection: textDirection,
            keyboardType: keyboardType,
            textInputAction: textInputAction,
            readOnly: readOnly,
            autofocus: autofocus,
            obscuringCharacter: obscuringCharacter,
            obscureText: obscureText,
            maxLines: maxLines,
            minLines: minLines,
            expands: expands,
            maxLength: maxLength,
            onChanged: value => state.HandleTextFieldChanged(value, onChanged),
            onTap: onTap,
            onEditingComplete: onEditingComplete,
            onSubmitted: onFieldSubmitted,
            enabled: enabled,
            buildCounter: buildCounter,
            mouseCursor: mouseCursor,
            canRequestFocus: canRequestFocus,
            enableInteractiveSelection: enableInteractiveSelection,
            contextMenuBuilder: contextMenuBuilder,
            magnifierConfiguration: magnifierConfiguration,
            onSelectionChanged: onSelectionChanged);
    }
}

public sealed class TextFormFieldState : FormFieldState<string>
{
    private TextEditingController? _controller;
    private bool _ownsController;
    private bool _suppressControllerChange;
    private string _initialValue = string.Empty;

    private TextFormField Current => (TextFormField)StateWidget;
    public TextEditingController EffectiveController => Current.Controller ?? _controller!;

    public override void InitState()
    {
        base.InitState();
        _initialValue = Current.InitialValue ?? Current.Controller?.Text ?? string.Empty;
        AttachController(Current.Controller, _initialValue);
        SetValue(EffectiveController.Text);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var old = (TextFormField)oldWidget;
        if (ReferenceEquals(old.Controller, Current.Controller)) return;

        string previousText = old.Controller?.Text ?? _controller?.Text ?? Value ?? string.Empty;
        DetachController(old.Controller);
        AttachController(Current.Controller, previousText);
        if (Current.Controller is not null) SetValue(Current.Controller.Text);
    }

    public override void Dispose()
    {
        DetachController(Current.Controller);
        base.Dispose();
    }

    public override void DidChange(string? value)
    {
        base.DidChange(value);
        string normalized = value ?? string.Empty;
        if (!string.Equals(EffectiveController.Text, normalized, StringComparison.Ordinal))
            EffectiveController.Text = normalized;
    }

    public override void Reset()
    {
        _suppressControllerChange = true;
        EffectiveController.Text = _initialValue;
        _suppressControllerChange = false;
        base.Reset();
        SetValue(_initialValue);
        Current.OnChanged?.Invoke(EffectiveController.Text);
    }

    internal void HandleTextFieldChanged(string value, Action<string>? onChanged)
    {
        if (!string.Equals(Value, value, StringComparison.Ordinal)) base.DidChange(value);
        onChanged?.Invoke(value);
    }

    private void AttachController(TextEditingController? external, string initialText)
    {
        _controller = external ?? new TextEditingController(initialText);
        _ownsController = external is null;
        _controller.AddListener(HandleControllerChanged);
    }

    private void DetachController(TextEditingController? external)
    {
        var controller = external ?? _controller;
        if (controller is null) return;
        controller.RemoveListener(HandleControllerChanged);
        if (_ownsController) controller.Dispose();
        _controller = null;
        _ownsController = false;
    }

    private void HandleControllerChanged()
    {
        if (_suppressControllerChange) return;
        if (!string.Equals(EffectiveController.Text, Value, StringComparison.Ordinal))
            base.DidChange(EffectiveController.Text);
    }
}
