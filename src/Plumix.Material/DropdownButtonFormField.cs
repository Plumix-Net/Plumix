using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/dropdown.dart

public sealed class DropdownButtonFormField<T> : FormField<T>
{
    public DropdownButtonFormField(
        IReadOnlyList<DropdownMenuItem<T>>? items,
        Action<T?>? onChanged,
        DropdownButtonBuilder? selectedItemBuilder = null,
        T? value = default,
        T? initialValue = default,
        Widget? hint = null,
        Widget? disabledHint = null,
        Action? onTap = null,
        int elevation = 8,
        TextStyle? style = null,
        Widget? icon = null,
        Color? iconDisabledColor = null,
        Color? iconEnabledColor = null,
        double iconSize = 24.0,
        bool isDense = true,
        bool isExpanded = false,
        double? itemHeight = null,
        Color? focusColor = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        Color? dropdownColor = null,
        InputDecoration? decoration = null,
        FormFieldSetter<T>? onSaved = null,
        FormFieldValidator<T>? validator = null,
        FormFieldErrorBuilder? errorBuilder = null,
        string? forceErrorText = null,
        AutovalidateMode autovalidateMode = AutovalidateMode.Disabled,
        double? menuMaxHeight = null,
        bool? enableFeedback = null,
        AlignmentGeometry? alignment = null,
        BorderRadius? borderRadius = null,
        EdgeInsetsGeometry? padding = null,
        bool barrierDismissible = true,
        MouseCursor? mouseCursor = null,
        MouseCursor? dropdownMenuItemMouseCursor = null,
        Key? key = null)
        : base(
            builder: field => BuildField(
                (DropdownButtonFormFieldState<T>)field,
                items,
                onChanged,
                selectedItemBuilder,
                hint,
                disabledHint,
                onTap,
                elevation,
                style,
                icon,
                iconDisabledColor,
                iconEnabledColor,
                iconSize,
                isDense,
                isExpanded,
                itemHeight,
                focusColor,
                focusNode,
                autofocus,
                dropdownColor,
                decoration ?? new InputDecoration(),
                errorBuilder,
                menuMaxHeight,
                enableFeedback,
                alignment,
                borderRadius,
                padding,
                barrierDismissible,
                mouseCursor,
                dropdownMenuItemMouseCursor),
            onSaved: onSaved,
            forceErrorText: forceErrorText,
            validator: validator,
            errorBuilder: errorBuilder,
            initialValue: ResolveInitialValue(items, value, initialValue, decoration, errorBuilder),
            autovalidateMode: autovalidateMode,
            key: key)
    {
        if (itemHeight.HasValue && itemHeight.Value < DropdownConstants.MenuItemHeight)
        {
            throw new ArgumentOutOfRangeException(
                nameof(itemHeight),
                $"itemHeight must be greater than or equal to {DropdownConstants.MenuItemHeight}.");
        }

        Items = items;
        OnChanged = onChanged;
        SelectedItemBuilder = selectedItemBuilder;
        Value = value;
        ExplicitInitialValue = initialValue;
        Hint = hint;
        DisabledHint = disabledHint;
        OnTap = onTap;
        Elevation = elevation;
        Style = style;
        Icon = icon;
        IconDisabledColor = iconDisabledColor;
        IconEnabledColor = iconEnabledColor;
        IconSize = iconSize;
        IsDense = isDense;
        IsExpanded = isExpanded;
        ItemHeight = itemHeight;
        FocusColor = focusColor;
        FocusNode = focusNode;
        Autofocus = autofocus;
        DropdownColor = dropdownColor;
        Decoration = decoration ?? new InputDecoration();
        MenuMaxHeight = menuMaxHeight;
        EnableFeedback = enableFeedback;
        Alignment = alignment ?? (AlignmentGeometry)AlignmentDirectional.CenterStart;
        BorderRadius = borderRadius;
        Padding = padding;
        BarrierDismissible = barrierDismissible;
        MouseCursor = mouseCursor;
        DropdownMenuItemMouseCursor = dropdownMenuItemMouseCursor;
    }

    public IReadOnlyList<DropdownMenuItem<T>>? Items { get; }
    public Action<T?>? OnChanged { get; }
    public DropdownButtonBuilder? SelectedItemBuilder { get; }
    public T? Value { get; }
    public T? ExplicitInitialValue { get; }
    public Widget? Hint { get; }
    public Widget? DisabledHint { get; }
    public Action? OnTap { get; }
    public int Elevation { get; }
    public TextStyle? Style { get; }
    public Widget? Icon { get; }
    public Color? IconDisabledColor { get; }
    public Color? IconEnabledColor { get; }
    public double IconSize { get; }
    public bool IsDense { get; }
    public bool IsExpanded { get; }
    public double? ItemHeight { get; }
    public Color? FocusColor { get; }
    public FocusNode? FocusNode { get; }
    public bool Autofocus { get; }
    public Color? DropdownColor { get; }
    public InputDecoration Decoration { get; }
    public double? MenuMaxHeight { get; }
    public bool? EnableFeedback { get; }
    public AlignmentGeometry Alignment { get; }
    public BorderRadius? BorderRadius { get; }
    public EdgeInsetsGeometry? Padding { get; }
    public bool BarrierDismissible { get; }
    public MouseCursor? MouseCursor { get; }
    public MouseCursor? DropdownMenuItemMouseCursor { get; }

    public override State CreateState() => new DropdownButtonFormFieldState<T>();

    private static T? ResolveInitialValue(
        IReadOnlyList<DropdownMenuItem<T>>? items,
        T? value,
        T? initialValue,
        InputDecoration? decoration,
        FormFieldErrorBuilder? errorBuilder)
    {
        if (errorBuilder is not null && decoration?.ErrorText is not null)
        {
            throw new ArgumentException(
                "Declaring both errorBuilder and decoration.errorText is not supported.",
                nameof(errorBuilder));
        }

        T? result = initialValue is not null ? initialValue : value;
        DropdownButton<T>.ValidateSelection(items, result, "DropdownButton");
        return result;
    }

    private static Widget BuildField(
        DropdownButtonFormFieldState<T> state,
        IReadOnlyList<DropdownMenuItem<T>>? items,
        Action<T?>? onChanged,
        DropdownButtonBuilder? selectedItemBuilder,
        Widget? hint,
        Widget? disabledHint,
        Action? onTap,
        int elevation,
        TextStyle? style,
        Widget? icon,
        Color? iconDisabledColor,
        Color? iconEnabledColor,
        double iconSize,
        bool isDense,
        bool isExpanded,
        double? itemHeight,
        Color? focusColor,
        FocusNode? focusNode,
        bool autofocus,
        Color? dropdownColor,
        InputDecoration decoration,
        FormFieldErrorBuilder? errorBuilder,
        double? menuMaxHeight,
        bool? enableFeedback,
        AlignmentGeometry? alignment,
        BorderRadius? borderRadius,
        EdgeInsetsGeometry? padding,
        bool barrierDismissible,
        MouseCursor? mouseCursor,
        MouseCursor? dropdownMenuItemMouseCursor)
    {
        InputDecoration effectiveDecoration = decoration.ApplyDefaults(InputDecorationTheme.Of(state.Context));
        bool showSelectedItem = items is not null
                                && items.Any(item => EqualityComparer<T?>.Default.Equals(item.Value, state.Value));
        bool isDropdownEnabled = onChanged is not null && items is { Count: > 0 };
        // If [decoration] hintText is provided, use it as the default value for both [hint] and
        // [disabledHint].
        Widget? decorationHint = effectiveDecoration.HintText is null ? null : new Text(effectiveDecoration.HintText);
        Widget? effectiveHint = hint ?? decorationHint;
        Widget? effectiveDisabledHint = disabledHint ?? effectiveHint;
        bool isHintOrDisabledHintAvailable = isDropdownEnabled
            ? effectiveHint is not null
            : effectiveHint is not null || effectiveDisabledHint is not null;
        bool isEmpty = !showSelectedItem && !isHintOrDisabledHintAvailable;

        if (state.ErrorText is not null || effectiveDecoration.HintText is not null)
        {
            Widget? error = state.ErrorText is not null && errorBuilder is not null
                ? errorBuilder(state.Context, state.ErrorText)
                : null;
            effectiveDecoration = effectiveDecoration.WithFormError(
                state.ErrorText,
                error,
                clearHintText: effectiveDecoration.HintText is not null);
        }

        // An unfocusable Focus widget so that this widget can detect if its children have focus or not.
        return new Focus(
            canRequestFocus: false,
            skipTraversal: true,
            child: new DropdownButtonHideUnderline(
                new DropdownButton<T>(
                    items: items,
                    onChanged: onChanged is null ? null : state.DidChange,
                    selectedItemBuilder: selectedItemBuilder,
                    value: state.Value,
                    hint: effectiveHint,
                    disabledHint: effectiveDisabledHint,
                    onTap: onTap,
                    elevation: elevation,
                    style: style,
                    underline: null,
                    icon: icon,
                    iconDisabledColor: iconDisabledColor,
                    iconEnabledColor: iconEnabledColor,
                    iconSize: iconSize,
                    isDense: isDense,
                    isExpanded: isExpanded,
                    itemHeight: itemHeight,
                    menuWidth: null,
                    focusColor: focusColor,
                    focusNode: focusNode,
                    autofocus: autofocus,
                    dropdownColor: dropdownColor,
                    menuMaxHeight: menuMaxHeight,
                    enableFeedback: enableFeedback,
                    alignment: alignment,
                    borderRadius: borderRadius,
                    padding: padding,
                    barrierDismissible: barrierDismissible,
                    mouseCursor: mouseCursor,
                    dropdownMenuItemMouseCursor: dropdownMenuItemMouseCursor,
                    inputDecoration: effectiveDecoration,
                    isEmpty: isEmpty,
                    valueLabel: "DropdownButtonFormField",
                    key: null)));
    }
}

public sealed class DropdownButtonFormFieldState<T> : FormFieldState<T>
{
    private DropdownButtonFormField<T> Current => (DropdownButtonFormField<T>)StateWidget;

    public override void DidChange(T? value)
    {
        base.DidChange(value);
        Current.OnChanged?.Invoke(value);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var old = (DropdownButtonFormField<T>)oldWidget;
        if (!EqualityComparer<T?>.Default.Equals(old.InitialValue, Current.InitialValue))
        {
            SetValue(Current.InitialValue);
        }
    }

    public override void Reset()
    {
        base.Reset();
        Current.OnChanged?.Invoke(Value);
    }
}
