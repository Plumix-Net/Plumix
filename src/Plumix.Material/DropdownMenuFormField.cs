using Avalonia;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/dropdown_menu_form_field.dart

public sealed class DropdownMenuFormField<T> : FormField<T>
{
    public DropdownMenuFormField(
        IReadOnlyList<DropdownMenuEntry<T>> dropdownMenuEntries,
        bool enabled = true,
        double? width = null,
        double? menuHeight = null,
        Widget? leadingIcon = null,
        Widget? trailingIcon = null,
        bool showTrailingIcon = true,
        FocusNode? trailingIconFocusNode = null,
        Widget? label = null,
        string? hintText = null,
        string? helperText = null,
        Widget? selectedTrailingIcon = null,
        bool enableFilter = false,
        bool enableSearch = true,
        TextStyle? textStyle = null,
        TextAlign textAlign = TextAlign.Start,
        InputDecorationThemeData? inputDecorationTheme = null,
        DropdownMenuDecorationBuilder? decorationBuilder = null,
        MenuStyle? menuStyle = null,
        TextEditingController? controller = null,
        T? initialSelection = default,
        Action<T?>? onSelected = null,
        FocusNode? focusNode = null,
        bool? requestFocusOnTap = null,
        bool selectOnly = false,
        Thickness? expandedInsets = null,
        Vector? alignmentOffset = null,
        DropdownMenuFilterCallback<T>? filterCallback = null,
        DropdownMenuSearchCallback<T>? searchCallback = null,
        DropdownMenuCloseBehavior closeBehavior = DropdownMenuCloseBehavior.All,
        int? maxLines = 1,
        double? cursorHeight = null,
        MenuController? menuController = null,
        string? restorationId = null,
        FormFieldSetter<T>? onSaved = null,
        FormFieldValidator<T>? validator = null,
        string? forceErrorText = null,
        FormFieldErrorBuilder? errorBuilder = null,
        AutovalidateMode autovalidateMode = AutovalidateMode.Disabled,
        Key? key = null)
        : base(
            builder: field => BuildField(
                (DropdownMenuFormFieldState<T>)field,
                enabled,
                width,
                menuHeight,
                leadingIcon,
                trailingIcon,
                showTrailingIcon,
                trailingIconFocusNode,
                label,
                hintText,
                helperText,
                selectedTrailingIcon,
                enableFilter,
                enableSearch,
                textStyle,
                textAlign,
                inputDecorationTheme,
                decorationBuilder,
                menuStyle,
                focusNode,
                requestFocusOnTap,
                selectOnly,
                expandedInsets,
                alignmentOffset,
                filterCallback,
                searchCallback,
                dropdownMenuEntries,
                closeBehavior,
                maxLines,
                cursorHeight,
                menuController,
                restorationId,
                errorBuilder),
            onSaved: onSaved,
            forceErrorText: forceErrorText,
            validator: validator,
            errorBuilder: errorBuilder,
            initialValue: initialSelection,
            enabled: enabled,
            autovalidateMode: autovalidateMode,
            restorationId: restorationId,
            key: key)
    {
        // Reuse DropdownMenu's constructor validation so both surfaces reject the same inputs.
        _ = new DropdownMenu<T>(
            dropdownMenuEntries: dropdownMenuEntries,
            enabled: enabled,
            width: width,
            menuHeight: menuHeight,
            showTrailingIcon: showTrailingIcon,
            trailingIconFocusNode: trailingIconFocusNode,
            label: decorationBuilder is null ? label : null,
            hintText: decorationBuilder is null ? hintText : null,
            helperText: decorationBuilder is null ? helperText : null,
            enableFilter: enableFilter,
            filterCallback: filterCallback,
            maxLines: maxLines,
            cursorHeight: cursorHeight,
            expandedInsets: expandedInsets,
            decorationBuilder: decorationBuilder);
        DropdownMenuEntries = dropdownMenuEntries;
        Controller = controller;
        OnSelected = onSelected;
    }

    public IReadOnlyList<DropdownMenuEntry<T>> DropdownMenuEntries { get; }
    public TextEditingController? Controller { get; }
    public Action<T?>? OnSelected { get; }

    public override State CreateState() => new DropdownMenuFormFieldState<T>();

    private static Widget BuildField(
        DropdownMenuFormFieldState<T> state,
        bool enabled,
        double? width,
        double? menuHeight,
        Widget? leadingIcon,
        Widget? trailingIcon,
        bool showTrailingIcon,
        FocusNode? trailingIconFocusNode,
        Widget? label,
        string? hintText,
        string? helperText,
        Widget? selectedTrailingIcon,
        bool enableFilter,
        bool enableSearch,
        TextStyle? textStyle,
        TextAlign textAlign,
        InputDecorationThemeData? inputDecorationTheme,
        DropdownMenuDecorationBuilder? decorationBuilder,
        MenuStyle? menuStyle,
        FocusNode? focusNode,
        bool? requestFocusOnTap,
        bool selectOnly,
        Thickness? expandedInsets,
        Vector? alignmentOffset,
        DropdownMenuFilterCallback<T>? filterCallback,
        DropdownMenuSearchCallback<T>? searchCallback,
        IReadOnlyList<DropdownMenuEntry<T>> dropdownMenuEntries,
        DropdownMenuCloseBehavior closeBehavior,
        int? maxLines,
        double? cursorHeight,
        MenuController? menuController,
        string? restorationId,
        FormFieldErrorBuilder? errorBuilder)
    {
        InputDecoration EffectiveDecoration(BuildContext context, MenuController controller)
        {
            var decoration = decorationBuilder?.Invoke(context, controller) ?? new InputDecoration();
            decoration = decoration.WithLabels(label, hintText, helperText);
            if (state.ErrorText is not { } errorText) return decoration;
            var error = errorBuilder?.Invoke(state.Context, errorText);
            return decoration.WithFormError(errorText, error);
        }

        return new DropdownMenu<T>(
            dropdownMenuEntries: dropdownMenuEntries,
            enabled: enabled,
            width: width,
            menuHeight: menuHeight,
            leadingIcon: leadingIcon,
            trailingIcon: trailingIcon,
            showTrailingIcon: showTrailingIcon,
            trailingIconFocusNode: trailingIconFocusNode,
            selectedTrailingIcon: selectedTrailingIcon,
            enableFilter: enableFilter,
            enableSearch: enableSearch,
            textStyle: textStyle,
            textAlign: textAlign,
            inputDecorationTheme: inputDecorationTheme,
            decorationBuilder: EffectiveDecoration,
            menuStyle: menuStyle,
            controller: state.EffectiveController,
            initialSelection: state.Value,
            onSelected: state.DidChange,
            focusNode: focusNode,
            requestFocusOnTap: requestFocusOnTap,
            selectOnly: selectOnly,
            expandedInsets: expandedInsets,
            alignmentOffset: alignmentOffset,
            filterCallback: filterCallback,
            searchCallback: searchCallback,
            closeBehavior: closeBehavior,
            maxLines: maxLines,
            cursorHeight: cursorHeight,
            menuController: menuController,
            restorationId: restorationId);
    }
}

public sealed class DropdownMenuFormFieldState<T> : FormFieldState<T>
{
    private TextEditingController? _localController;
    private DropdownMenuFormField<T> Current => (DropdownMenuFormField<T>)StateWidget;

    public TextEditingController EffectiveController => Current.Controller ?? _localController!;

    public override void InitState()
    {
        base.InitState();
        _localController = Current.Controller is null
            ? new TextEditingController(FindLabel(Current.InitialValue))
            : null;
        if (Current.Controller is not null && string.IsNullOrEmpty(Current.Controller.Text))
            Current.Controller.Text = FindLabel(Current.InitialValue);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var old = (DropdownMenuFormField<T>)oldWidget;
        if (!ReferenceEquals(old.Controller, Current.Controller))
        {
            _localController?.Dispose();
            _localController = Current.Controller is null
                ? new TextEditingController()
                : null;
        }
        if (!EqualityComparer<T?>.Default.Equals(old.InitialValue, Current.InitialValue)
            && !HasInteractedByUser)
        {
            SetValue(Current.InitialValue);
            EffectiveController.Text = FindLabel(Current.InitialValue);
        }
    }

    public override void DidChange(T? value)
    {
        base.DidChange(value);
        EffectiveController.Text = FindLabel(value);
        Current.OnSelected?.Invoke(value);
    }

    public override void Reset()
    {
        base.Reset();
        EffectiveController.Text = FindLabel(Current.InitialValue);
        if (Current.InitialValue is null) EffectiveController.Clear();
        Current.OnSelected?.Invoke(Value);
    }

    public override void Dispose()
    {
        _localController?.Dispose();
        _localController = null;
        base.Dispose();
    }

    private string FindLabel(T? value)
    {
        foreach (var entry in Current.DropdownMenuEntries)
            if (EqualityComparer<T?>.Default.Equals(entry.Value, value)) return entry.Label;
        return string.Empty;
    }
}
