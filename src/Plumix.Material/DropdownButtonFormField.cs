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
        double iconSize = 24,
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
        Thickness? padding = null,
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
        if (elevation < 0) throw new ArgumentOutOfRangeException(nameof(elevation));
        if (!double.IsFinite(iconSize) || iconSize < 0) throw new ArgumentOutOfRangeException(nameof(iconSize));
        if (itemHeight.HasValue && (!double.IsFinite(itemHeight.Value) || itemHeight.Value < 48))
            throw new ArgumentOutOfRangeException(nameof(itemHeight));
        if (menuMaxHeight.HasValue && (!double.IsFinite(menuMaxHeight.Value) || menuMaxHeight.Value <= 0))
            throw new ArgumentOutOfRangeException(nameof(menuMaxHeight));

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
    public Thickness? Padding { get; }
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
            throw new ArgumentException("errorBuilder and decoration.errorText cannot both be specified.", nameof(errorBuilder));
        var result = initialValue is not null ? initialValue : value;
        if (items is not null && items.Count > 0 && result is not null
            && items.Count(item => EqualityComparer<T?>.Default.Equals(item.Value, result)) != 1)
        {
            throw new ArgumentException("There must be exactly one dropdown item matching initialValue or value.", nameof(initialValue));
        }
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
        Thickness? padding,
        bool barrierDismissible,
        MouseCursor? mouseCursor,
        MouseCursor? dropdownMenuItemMouseCursor)
    {
        bool showSelectedItem = items?.Any(item =>
            EqualityComparer<T?>.Default.Equals(item.Value, state.Value)) == true;
        bool enabled = onChanged is not null && items is { Count: > 0 };
        var decorationHint = decoration.HintText is null ? null : new Text(decoration.HintText);
        var effectiveHint = hint ?? decorationHint;
        var effectiveDisabledHint = disabledHint ?? effectiveHint;
        bool hintAvailable = enabled
            ? effectiveHint is not null
            : effectiveHint is not null || effectiveDisabledHint is not null;
        bool isEmpty = !showSelectedItem && !hintAvailable;

        var effectiveDecoration = decoration;
        if (state.ErrorText is { } errorText || decoration.HintText is not null)
        {
            var error = state.ErrorText is null ? null : errorBuilder?.Invoke(state.Context, state.ErrorText);
            effectiveDecoration = decoration.WithFormError(state.ErrorText, error, clearHintText: true);
        }

        Widget button = new DropdownButton<T>(
            items: items,
            onChanged: onChanged is null ? null : state.DidChange,
            selectedItemBuilder: selectedItemBuilder,
            value: state.Value,
            hint: effectiveHint,
            disabledHint: effectiveDisabledHint,
            onTap: onTap,
            elevation: elevation,
            style: style,
            icon: icon,
            iconDisabledColor: iconDisabledColor,
            iconEnabledColor: iconEnabledColor,
            iconSize: iconSize,
            isDense: isDense,
            isExpanded: isExpanded,
            itemHeight: itemHeight,
            focusColor: focusColor,
            focusNode: state.EffectiveFocusNode,
            autofocus: autofocus,
            dropdownColor: dropdownColor,
            menuMaxHeight: menuMaxHeight,
            enableFeedback: enableFeedback,
            alignment: alignment,
            borderRadius: borderRadius,
            padding: padding,
            barrierDismissible: barrierDismissible,
            mouseCursor: mouseCursor,
            dropdownMenuItemMouseCursor: dropdownMenuItemMouseCursor);
        button = new DropdownButtonHideUnderline(button);

        return new InputDecorator(
            decoration: effectiveDecoration,
            baseStyle: style,
            isFocused: state.EffectiveFocusNode.HasFocus,
            isEmpty: isEmpty,
            child: button);
    }
}

public sealed class DropdownButtonFormFieldState<T> : FormFieldState<T>
{
    private FocusNode? _focusNode;
    private bool _ownsFocusNode;
    private DropdownButtonFormField<T> Current => (DropdownButtonFormField<T>)StateWidget;

    public FocusNode EffectiveFocusNode => Current.FocusNode ?? _focusNode!;

    public override void InitState()
    {
        base.InitState();
        AttachFocusNode(Current.FocusNode);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var old = (DropdownButtonFormField<T>)oldWidget;
        if (!ReferenceEquals(old.FocusNode, Current.FocusNode))
        {
            DetachFocusNode(old.FocusNode);
            AttachFocusNode(Current.FocusNode);
        }
        if (!EqualityComparer<T?>.Default.Equals(old.InitialValue, Current.InitialValue))
            SetValue(Current.InitialValue);
    }

    public override void DidChange(T? value)
    {
        base.DidChange(value);
        Current.OnChanged?.Invoke(value);
    }

    public override void Reset()
    {
        base.Reset();
        Current.OnChanged?.Invoke(Value);
    }

    public override void Dispose()
    {
        DetachFocusNode(Current.FocusNode);
        base.Dispose();
    }

    private void AttachFocusNode(FocusNode? external)
    {
        _focusNode = external ?? new FocusNode();
        _ownsFocusNode = external is null;
        _focusNode.AddListener(HandleFocusChanged);
    }

    private void DetachFocusNode(FocusNode? external)
    {
        var node = external ?? _focusNode;
        if (node is null) return;
        node.RemoveListener(HandleFocusChanged);
        if (_ownsFocusNode) node.Dispose();
        _focusNode = null;
        _ownsFocusNode = false;
    }

    private void HandleFocusChanged()
    {
        if (Mounted) InvokeSetState(() => { });
    }
}
