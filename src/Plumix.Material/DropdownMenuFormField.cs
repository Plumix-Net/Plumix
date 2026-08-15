using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/dropdown_menu_form_field.dart

/// <summary>
/// A <see cref="FormField{T}"/> that wraps a <see cref="DropdownMenu{T}"/> so the selection takes
/// part in form validation, saving, resetting and state restoration.
/// </summary>
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
        TextInputType? keyboardType = null,
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
        EdgeInsetsGeometry? expandedInsets = null,
        Vector? alignmentOffset = null,
        DropdownMenuFilterCallback<T>? filterCallback = null,
        DropdownMenuSearchCallback<T>? searchCallback = null,
        IReadOnlyList<TextInputFormatter>? inputFormatters = null,
        DropdownMenuCloseBehavior closeBehavior = DropdownMenuCloseBehavior.All,
        int maxLines = 1,
        TextInputAction? textInputAction = null,
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
                new DropdownMenuFormFieldConfiguration<T>(
                    dropdownMenuEntries,
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
                    keyboardType,
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
                    inputFormatters,
                    closeBehavior,
                    maxLines,
                    textInputAction,
                    cursorHeight,
                    menuController,
                    restorationId,
                    errorBuilder)),
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
        if (dropdownMenuEntries is null) throw new ArgumentNullException(nameof(dropdownMenuEntries));
        if (filterCallback is not null && !enableFilter)
            throw new ArgumentException("filterCallback requires enableFilter=true.", nameof(filterCallback));
        if (trailingIconFocusNode is not null && !showTrailingIcon)
        {
            throw new ArgumentException(
                "trailingIconFocusNode requires showTrailingIcon=true.",
                nameof(trailingIconFocusNode));
        }

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
        DropdownMenuFormFieldConfiguration<T> config)
    {
        // Dart's `effectiveDecorationBuilder`: labels always arrive through the decoration so the
        // wrapped `DropdownMenu`'s own label/hint/helper/error stay null and its assert holds.
        InputDecoration EffectiveDecoration(BuildContext context, MenuController controller)
        {
            var decoration = config.DecorationBuilder?.Invoke(context, controller) ?? new InputDecoration();
            var decorationWithLabels = decoration.WithLabels(config.Label, config.HintText, config.HelperText);
            if (state.ErrorText is not { } errorText) return decorationWithLabels;
            return config.ErrorBuilder is { } errorBuilder
                ? decorationWithLabels.WithFormError(null, errorBuilder(state.Context, errorText))
                : decorationWithLabels.WithFormError(errorText);
        }

        return new UnmanagedRestorationScope(
            bucket: state.Bucket,
            child: new DropdownMenu<T>(
                dropdownMenuEntries: config.DropdownMenuEntries,
                restorationId: config.RestorationId,
                enabled: config.Enabled,
                width: config.Width,
                menuHeight: config.MenuHeight,
                leadingIcon: config.LeadingIcon,
                trailingIcon: config.TrailingIcon,
                showTrailingIcon: config.ShowTrailingIcon,
                trailingIconFocusNode: config.TrailingIconFocusNode,
                selectedTrailingIcon: config.SelectedTrailingIcon,
                enableFilter: config.EnableFilter,
                enableSearch: config.EnableSearch,
                keyboardType: config.KeyboardType,
                textStyle: config.TextStyle,
                textAlign: config.TextAlign,
                inputDecorationTheme: config.InputDecorationTheme,
                decorationBuilder: EffectiveDecoration,
                menuStyle: config.MenuStyle,
                controller: state.TextFieldController,
                initialSelection: state.Value,
                onSelected: state.DidChange,
                focusNode: config.FocusNode,
                requestFocusOnTap: config.RequestFocusOnTap,
                selectOnly: config.SelectOnly,
                expandedInsets: config.ExpandedInsets,
                alignmentOffset: config.AlignmentOffset,
                filterCallback: config.FilterCallback,
                searchCallback: config.SearchCallback,
                inputFormatters: config.InputFormatters,
                closeBehavior: config.CloseBehavior,
                maxLines: config.MaxLines,
                textInputAction: config.TextInputAction,
                cursorHeight: config.CursorHeight,
                menuController: config.MenuController));
    }
}

/// <summary>
/// Carries the <see cref="DropdownMenuFormField{T}"/> constructor arguments into the field builder.
/// Dart closes over them directly; C# cannot capture constructor parameters in a base-call lambda
/// without one, so they travel as a record.
/// </summary>
internal sealed record DropdownMenuFormFieldConfiguration<T>(
    IReadOnlyList<DropdownMenuEntry<T>> DropdownMenuEntries,
    bool Enabled,
    double? Width,
    double? MenuHeight,
    Widget? LeadingIcon,
    Widget? TrailingIcon,
    bool ShowTrailingIcon,
    FocusNode? TrailingIconFocusNode,
    Widget? Label,
    string? HintText,
    string? HelperText,
    Widget? SelectedTrailingIcon,
    bool EnableFilter,
    bool EnableSearch,
    TextInputType? KeyboardType,
    TextStyle? TextStyle,
    TextAlign TextAlign,
    InputDecorationThemeData? InputDecorationTheme,
    DropdownMenuDecorationBuilder? DecorationBuilder,
    MenuStyle? MenuStyle,
    FocusNode? FocusNode,
    bool? RequestFocusOnTap,
    bool SelectOnly,
    EdgeInsetsGeometry? ExpandedInsets,
    Vector? AlignmentOffset,
    DropdownMenuFilterCallback<T>? FilterCallback,
    DropdownMenuSearchCallback<T>? SearchCallback,
    IReadOnlyList<TextInputFormatter>? InputFormatters,
    DropdownMenuCloseBehavior CloseBehavior,
    int MaxLines,
    TextInputAction? TextInputAction,
    double? CursorHeight,
    MenuController? MenuController,
    string? RestorationId,
    FormFieldErrorBuilder? ErrorBuilder);

public sealed class DropdownMenuFormFieldState<T> : FormFieldState<T>
{
    private RestorableTextEditingController? _restorableController;
    private TextEditingController? _localTextFieldController;

    private DropdownMenuFormField<T> Current => (DropdownMenuFormField<T>)StateWidget;

    /// <summary>Dart's `_DropdownMenuFormFieldState.textFieldController`.</summary>
    public TextEditingController TextFieldController =>
        Current.Controller ?? (_localTextFieldController ??= new TextEditingController());

    public override void InitState()
    {
        base.InitState();
        CreateRestorableController(CurrentField.InitialValue);
    }

    protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
    {
        base.RestoreState(oldBucket, initialRestore);
        if (_restorableController is null) return;
        RegisterRestorableController();
        if (FindValueByLabel(_restorableController.Value.Text, out T? matchingValue))
        {
            SetValue(matchingValue);
        }
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var old = (DropdownMenuFormField<T>)oldWidget;
        if (!EqualityComparer<T?>.Default.Equals(old.InitialValue, CurrentField.InitialValue)
            && !HasInteractedByUser)
        {
            SetValue(CurrentField.InitialValue);
        }

        if (!ReferenceEquals(old.Controller, Current.Controller))
        {
            _localTextFieldController?.Dispose();
            _localTextFieldController = null;
        }
    }

    public override void Dispose()
    {
        _restorableController?.Dispose();
        _restorableController = null;
        _localTextFieldController?.Dispose();
        _localTextFieldController = null;
        base.Dispose();
    }

    public override void DidChange(T? value)
    {
        base.DidChange(value);
        Current.OnSelected?.Invoke(value);
        UpdateRestorableController(value);
    }

    public override void Reset()
    {
        base.Reset();
        Current.OnSelected?.Invoke(Value);
        UpdateRestorableController(CurrentField.InitialValue);
        if (CurrentField.InitialValue is null) TextFieldController.Clear();
    }

    private void CreateRestorableController(T? initialValue)
    {
        if (_restorableController is not null)
            throw new InvalidOperationException("The restorable controller was already created.");
        _restorableController = RestorableTextEditingController.FromValue(
            new TextEditingValue(FindLabelByValue(initialValue)));
        if (!RestorePending) RegisterRestorableController();
    }

    private void RegisterRestorableController() => RegisterForRestoration(_restorableController!, "controller");

    private void UpdateRestorableController(T? value)
    {
        if (_restorableController is null) return;
        _restorableController.Value.Value = new TextEditingValue(FindLabelByValue(value));
    }

    private bool FindValueByLabel(string label, out T? value)
    {
        foreach (var entry in Current.DropdownMenuEntries)
        {
            if (string.Equals(entry.Label, label, StringComparison.Ordinal))
            {
                value = entry.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private string FindLabelByValue(T? value)
    {
        foreach (var entry in Current.DropdownMenuEntries)
        {
            if (EqualityComparer<T?>.Default.Equals(entry.Value, value)) return entry.Label;
        }

        return string.Empty;
    }
}
