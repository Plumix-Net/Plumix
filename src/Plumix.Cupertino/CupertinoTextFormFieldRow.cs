using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/text_form_field_row.dart

/// <summary>A Cupertino form row containing a form field that wraps a borderless text field.</summary>
public sealed class CupertinoTextFormFieldRow : FormField<string>
{
    public CupertinoTextFormFieldRow(
        Widget? prefix = null,
        EdgeInsetsGeometry? padding = null,
        TextEditingController? controller = null,
        string? initialValue = null,
        FocusNode? focusNode = null,
        BoxDecoration? decoration = null,
        TextInputType? keyboardType = null,
        TextCapitalization textCapitalization = TextCapitalization.None,
        TextInputActionType? textInputAction = null,
        TextStyle? style = null,
        StrutStyle? strutStyle = null,
        TextDirection? textDirection = null,
        TextAlign textAlign = TextAlign.Start,
        TextAlignVertical? textAlignVertical = null,
        bool autofocus = false,
        bool readOnly = false,
        ToolbarOptions? toolbarOptions = null,
        bool? showCursor = null,
        string obscuringCharacter = "•",
        bool obscureText = false,
        bool autocorrect = true,
        SmartDashesType? smartDashesType = null,
        SmartQuotesType? smartQuotesType = null,
        bool enableSuggestions = true,
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
        IReadOnlyList<TextInputFormatter>? inputFormatters = null,
        bool? enabled = null,
        double cursorWidth = 2.0,
        double? cursorHeight = null,
        CupertinoDynamicColor? cursorColor = null,
        PlatformBrightness? keyboardAppearance = null,
        EdgeInsetsGeometry? scrollPadding = null,
        bool enableInteractiveSelection = true,
        TextSelectionControls? selectionControls = null,
        ScrollPhysics? scrollPhysics = null,
        IReadOnlyList<string>? autofillHints = null,
        AutovalidateMode autovalidateMode = AutovalidateMode.Disabled,
        string? placeholder = null,
        TextStyle? placeholderStyle = null,
        EditableTextContextMenuBuilder? contextMenuBuilder = null,
        SpellCheckConfiguration? spellCheckConfiguration = null,
        BoxHeightStyle? selectionHeightStyle = null,
        BoxWidthStyle? selectionWidthStyle = null,
        string? restorationId = null,
        Key? key = null)
        : base(
            builder: field => BuildField(
                (CupertinoTextFormFieldRowState)field,
                prefix,
                padding,
                focusNode,
                decoration,
                keyboardType,
                textCapitalization,
                textInputAction,
                style,
                strutStyle,
                textDirection,
                textAlign,
                textAlignVertical,
                autofocus,
                readOnly,
                toolbarOptions,
                showCursor,
                obscuringCharacter,
                obscureText,
                autocorrect,
                smartDashesType,
                smartQuotesType,
                enableSuggestions,
                maxLines,
                minLines,
                expands,
                maxLength,
                onChanged,
                onTap,
                onEditingComplete,
                onFieldSubmitted,
                inputFormatters,
                enabled ?? true,
                cursorWidth,
                cursorHeight,
                cursorColor,
                keyboardAppearance,
                scrollPadding,
                enableInteractiveSelection,
                selectionControls,
                scrollPhysics,
                autofillHints,
                placeholder,
                placeholderStyle,
                contextMenuBuilder,
                spellCheckConfiguration,
                selectionHeightStyle,
                selectionWidthStyle,
                restorationId),
            onSaved: onSaved,
            validator: validator,
            initialValue: ResolveInitialValue(controller, initialValue),
            enabled: enabled ?? true,
            autovalidateMode: autovalidateMode,
            restorationId: restorationId,
            key: key)
    {
        ValidateTextFieldArguments(obscuringCharacter, maxLines, minLines, expands, obscureText, maxLength);
        Prefix = prefix;
        Padding = padding;
        Controller = controller;
        OnChanged = onChanged;
    }

    /// <summary>A widget displayed at the start of the row.</summary>
    public Widget? Prefix { get; }

    /// <summary>Content padding passed directly to <see cref="CupertinoFormRow"/>.</summary>
    public EdgeInsetsGeometry? Padding { get; }

    /// <summary>The external text controller, or null when the state owns a restorable controller.</summary>
    public TextEditingController? Controller { get; }

    /// <summary>Called after user-initiated text changes and after the form field is reset.</summary>
    public Action<string>? OnChanged { get; }

    public override State CreateState() => new CupertinoTextFormFieldRowState();

    private static string ResolveInitialValue(TextEditingController? controller, string? initialValue)
    {
        if (controller is not null && initialValue is not null)
        {
            throw new ArgumentException(
                "initialValue must be null when controller is provided.",
                nameof(initialValue));
        }

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
        {
            throw new ArgumentException(
                "obscuringCharacter must contain exactly one UTF-16 character.",
                nameof(obscuringCharacter));
        }
        if (maxLines.HasValue && maxLines.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLines));
        }
        if (minLines.HasValue && minLines.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minLines));
        }
        if (maxLines.HasValue && minLines.HasValue && minLines.Value > maxLines.Value)
        {
            throw new ArgumentException("minLines cannot be greater than maxLines.", nameof(minLines));
        }
        if (expands && (maxLines.HasValue || minLines.HasValue))
        {
            throw new ArgumentException("minLines and maxLines must be null when expands is true.", nameof(expands));
        }
        if (obscureText && maxLines != 1)
        {
            throw new ArgumentException("Obscured fields cannot be multiline.", nameof(obscureText));
        }
        if (maxLength.HasValue && maxLength.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength));
        }
    }

    private static Widget BuildField(
        CupertinoTextFormFieldRowState state,
        Widget? prefix,
        EdgeInsetsGeometry? padding,
        FocusNode? focusNode,
        BoxDecoration? decoration,
        TextInputType? keyboardType,
        TextCapitalization textCapitalization,
        TextInputActionType? textInputAction,
        TextStyle? style,
        StrutStyle? strutStyle,
        TextDirection? textDirection,
        TextAlign textAlign,
        TextAlignVertical? textAlignVertical,
        bool autofocus,
        bool readOnly,
        ToolbarOptions? toolbarOptions,
        bool? showCursor,
        string obscuringCharacter,
        bool obscureText,
        bool autocorrect,
        SmartDashesType? smartDashesType,
        SmartQuotesType? smartQuotesType,
        bool enableSuggestions,
        int? maxLines,
        int? minLines,
        bool expands,
        int? maxLength,
        Action<string>? onChanged,
        Action? onTap,
        Action? onEditingComplete,
        Action<string>? onFieldSubmitted,
        IReadOnlyList<TextInputFormatter>? inputFormatters,
        bool enabled,
        double cursorWidth,
        double? cursorHeight,
        CupertinoDynamicColor? cursorColor,
        PlatformBrightness? keyboardAppearance,
        EdgeInsetsGeometry? scrollPadding,
        bool enableInteractiveSelection,
        TextSelectionControls? selectionControls,
        ScrollPhysics? scrollPhysics,
        IReadOnlyList<string>? autofillHints,
        string? placeholder,
        TextStyle? placeholderStyle,
        EditableTextContextMenuBuilder? contextMenuBuilder,
        SpellCheckConfiguration? spellCheckConfiguration,
        BoxHeightStyle? selectionHeightStyle,
        BoxWidthStyle? selectionWidthStyle,
        string? restorationId)
    {
        return new CupertinoFormRow(
            prefix: prefix,
            padding: padding,
            error: state.ErrorText is null ? null : new Text(state.ErrorText),
            child: new UnmanagedRestorationScope(
                bucket: state.Bucket,
                child: CupertinoTextField.Borderless(
                    restorationId: restorationId,
                    controller: state.EffectiveController,
                    focusNode: focusNode,
                    keyboardType: keyboardType,
                    decoration: decoration,
                    textInputAction: textInputAction,
                    style: style,
                    strutStyle: strutStyle,
                    textAlign: textAlign,
                    textAlignVertical: textAlignVertical,
                    textCapitalization: textCapitalization,
                    textDirection: textDirection,
                    autofocus: autofocus,
                    toolbarOptions: toolbarOptions,
                    readOnly: readOnly,
                    showCursor: showCursor,
                    obscuringCharacter: obscuringCharacter,
                    obscureText: obscureText,
                    autocorrect: autocorrect,
                    smartDashesType: smartDashesType,
                    smartQuotesType: smartQuotesType,
                    enableSuggestions: enableSuggestions,
                    maxLines: maxLines,
                    minLines: minLines,
                    expands: expands,
                    maxLength: maxLength,
                    onChanged: value => state.HandleTextFieldChanged(value, onChanged),
                    onTap: onTap,
                    onEditingComplete: onEditingComplete,
                    onSubmitted: onFieldSubmitted,
                    inputFormatters: inputFormatters,
                    enabled: enabled,
                    cursorWidth: cursorWidth,
                    cursorHeight: cursorHeight,
                    cursorColor: cursorColor,
                    scrollPadding: scrollPadding,
                    scrollPhysics: scrollPhysics,
                    keyboardAppearance: keyboardAppearance,
                    enableInteractiveSelection: enableInteractiveSelection,
                    selectionControls: selectionControls,
                    autofillHints: autofillHints,
                    placeholder: placeholder,
                    placeholderStyle: placeholderStyle,
                    contextMenuBuilder: contextMenuBuilder,
                    spellCheckConfiguration: spellCheckConfiguration,
                    selectionHeightStyle: selectionHeightStyle,
                    selectionWidthStyle: selectionWidthStyle)));
    }
}

internal sealed class CupertinoTextFormFieldRowState : FormFieldState<string>
{
    private RestorableTextEditingController? _controller;
    private bool _suppressControllerChange;

    private CupertinoTextFormFieldRow Current => (CupertinoTextFormFieldRow)StateWidget;

    internal TextEditingController EffectiveController => Current.Controller ?? _controller!.Value;

    protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
    {
        base.RestoreState(oldBucket, initialRestore);
        if (_controller is not null)
        {
            RegisterController();
        }
        SetValue(EffectiveController.Text);
    }

    public override void InitState()
    {
        base.InitState();
        if (Current.Controller is null)
        {
            CreateLocalController(
                Current.InitialValue is null
                    ? null
                    : new TextEditingValue(Current.InitialValue));
        }
        else
        {
            Current.Controller.AddListener(HandleControllerChanged);
        }
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var old = (CupertinoTextFormFieldRow)oldWidget;
        if (ReferenceEquals(Current.Controller, old.Controller))
        {
            return;
        }

        old.Controller?.RemoveListener(HandleControllerChanged);
        Current.Controller?.AddListener(HandleControllerChanged);

        if (old.Controller is not null && Current.Controller is null)
        {
            CreateLocalController(old.Controller.Value);
        }

        if (Current.Controller is not null)
        {
            SetValue(Current.Controller.Text);
            if (old.Controller is null)
            {
                UnregisterFromRestoration(_controller!);
                _controller!.Dispose();
                _controller = null;
            }
        }
    }

    public override void Dispose()
    {
        Current.Controller?.RemoveListener(HandleControllerChanged);
        _controller?.Dispose();
        base.Dispose();
    }

    public override void DidChange(string? value)
    {
        base.DidChange(value);
        if (value is not null && !string.Equals(EffectiveController.Text, value, StringComparison.Ordinal))
        {
            EffectiveController.Value = new TextEditingValue(value);
        }
    }

    public override void Reset()
    {
        _suppressControllerChange = true;
        EffectiveController.Value = new TextEditingValue(Current.InitialValue ?? string.Empty);
        _suppressControllerChange = false;
        base.Reset();
        Current.OnChanged?.Invoke(EffectiveController.Text);
    }

    internal void HandleTextFieldChanged(string value, Action<string>? onChanged)
    {
        DidChange(value);
        onChanged?.Invoke(value);
    }

    private void RegisterController()
    {
        RegisterForRestoration(_controller!, "controller");
    }

    private void CreateLocalController(TextEditingValue? value)
    {
        _controller = value is null
            ? new RestorableTextEditingController()
            : RestorableTextEditingController.FromValue(value.Value);
        if (!RestorePending)
        {
            RegisterController();
        }
    }

    private void HandleControllerChanged()
    {
        if (_suppressControllerChange)
        {
            return;
        }
        if (!string.Equals(EffectiveController.Text, Value, StringComparison.Ordinal))
        {
            DidChange(EffectiveController.Text);
        }
    }
}
