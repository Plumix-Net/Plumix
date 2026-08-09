using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// flutter/packages/flutter/lib/src/material/filter_chip.dart
// flutter/packages/flutter/lib/src/material/input_chip.dart

public sealed class FilterChip : StatelessWidget
{
    public FilterChip(
        Widget label,
        Action<bool>? onSelected,
        bool selected = false,
        Widget? avatar = null,
        TextStyle? labelStyle = null,
        Thickness? labelPadding = null,
        Widget? deleteIcon = null,
        Action? onDeleted = null,
        Color? deleteIconColor = null,
        string? deleteButtonTooltipMessage = null,
        double? pressElevation = null,
        Color? disabledColor = null,
        Color? selectedColor = null,
        string? tooltip = null,
        MaterialStateProperty<BorderSide?>? side = null,
        MaterialStateProperty<ShapeBorder?>? shape = null,
        Clip clipBehavior = Clip.None,
        FocusNode? focusNode = null,
        bool autofocus = false,
        MaterialStateProperty<Color?>? color = null,
        Color? backgroundColor = null,
        Thickness? padding = null,
        VisualDensity? visualDensity = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        IconThemeData? iconTheme = null,
        Color? selectedShadowColor = null,
        bool? showCheckmark = null,
        Color? checkmarkColor = null,
        ShapeBorder? avatarBorder = null,
        BoxConstraints? avatarBoxConstraints = null,
        BoxConstraints? deleteIconBoxConstraints = null,
        ChipAnimationStyle? chipAnimationStyle = null,
        MouseCursor? mouseCursor = null,
        Key? key = null) : this(
            label, onSelected, selected, avatar, labelStyle, labelPadding, deleteIcon, onDeleted,
            deleteIconColor, deleteButtonTooltipMessage, pressElevation, disabledColor, selectedColor,
            tooltip, side, shape, clipBehavior, focusNode, autofocus, color, backgroundColor, padding,
            visualDensity, materialTapTargetSize, elevation, shadowColor, surfaceTintColor, iconTheme,
            selectedShadowColor, showCheckmark, checkmarkColor, avatarBorder, avatarBoxConstraints,
            deleteIconBoxConstraints, chipAnimationStyle, mouseCursor, ChipVariant.Flat, key)
    {
    }

    private FilterChip(
        Widget label,
        Action<bool>? onSelected,
        bool selected,
        Widget? avatar,
        TextStyle? labelStyle,
        Thickness? labelPadding,
        Widget? deleteIcon,
        Action? onDeleted,
        Color? deleteIconColor,
        string? deleteButtonTooltipMessage,
        double? pressElevation,
        Color? disabledColor,
        Color? selectedColor,
        string? tooltip,
        MaterialStateProperty<BorderSide?>? side,
        MaterialStateProperty<ShapeBorder?>? shape,
        Clip clipBehavior,
        FocusNode? focusNode,
        bool autofocus,
        MaterialStateProperty<Color?>? color,
        Color? backgroundColor,
        Thickness? padding,
        VisualDensity? visualDensity,
        MaterialTapTargetSize? materialTapTargetSize,
        double? elevation,
        Color? shadowColor,
        Color? surfaceTintColor,
        IconThemeData? iconTheme,
        Color? selectedShadowColor,
        bool? showCheckmark,
        Color? checkmarkColor,
        ShapeBorder? avatarBorder,
        BoxConstraints? avatarBoxConstraints,
        BoxConstraints? deleteIconBoxConstraints,
        ChipAnimationStyle? chipAnimationStyle,
        MouseCursor? mouseCursor,
        ChipVariant variant,
        Key? key) : base(key)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        ValidateElevation(pressElevation, nameof(pressElevation));
        ValidateElevation(elevation, nameof(elevation));
        OnSelected = onSelected;
        Selected = selected;
        Avatar = avatar;
        LabelStyle = labelStyle;
        LabelPadding = labelPadding;
        DeleteIcon = deleteIcon;
        OnDeleted = onDeleted;
        DeleteIconColor = deleteIconColor;
        DeleteButtonTooltipMessage = deleteButtonTooltipMessage;
        PressElevation = pressElevation;
        DisabledColor = disabledColor;
        SelectedColor = selectedColor;
        Tooltip = tooltip;
        Side = side;
        Shape = shape;
        ClipBehavior = clipBehavior;
        FocusNode = focusNode;
        Autofocus = autofocus;
        Color = color;
        BackgroundColor = backgroundColor;
        Padding = padding;
        VisualDensity = visualDensity;
        MaterialTapTargetSize = materialTapTargetSize;
        Elevation = elevation;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        IconTheme = iconTheme;
        SelectedShadowColor = selectedShadowColor;
        ShowCheckmark = showCheckmark;
        CheckmarkColor = checkmarkColor;
        AvatarBorder = avatarBorder ?? ShapeBorder.RoundedRectangle(10_000);
        AvatarBoxConstraints = avatarBoxConstraints;
        DeleteIconBoxConstraints = deleteIconBoxConstraints;
        ChipAnimationStyle = chipAnimationStyle;
        MouseCursor = mouseCursor;
        Variant = variant;
    }

    public static FilterChip Elevated(
        Widget label,
        Action<bool>? onSelected,
        bool selected = false,
        Widget? avatar = null,
        TextStyle? labelStyle = null,
        Thickness? labelPadding = null,
        Widget? deleteIcon = null,
        Action? onDeleted = null,
        Color? deleteIconColor = null,
        string? deleteButtonTooltipMessage = null,
        double? pressElevation = null,
        Color? disabledColor = null,
        Color? selectedColor = null,
        string? tooltip = null,
        MaterialStateProperty<BorderSide?>? side = null,
        MaterialStateProperty<ShapeBorder?>? shape = null,
        Clip clipBehavior = Clip.None,
        FocusNode? focusNode = null,
        bool autofocus = false,
        MaterialStateProperty<Color?>? color = null,
        Color? backgroundColor = null,
        Thickness? padding = null,
        VisualDensity? visualDensity = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        IconThemeData? iconTheme = null,
        Color? selectedShadowColor = null,
        bool? showCheckmark = null,
        Color? checkmarkColor = null,
        ShapeBorder? avatarBorder = null,
        BoxConstraints? avatarBoxConstraints = null,
        BoxConstraints? deleteIconBoxConstraints = null,
        ChipAnimationStyle? chipAnimationStyle = null,
        MouseCursor? mouseCursor = null,
        Key? key = null)
    {
        return new FilterChip(
            label, onSelected, selected, avatar, labelStyle, labelPadding, deleteIcon, onDeleted,
            deleteIconColor, deleteButtonTooltipMessage, pressElevation, disabledColor, selectedColor,
            tooltip, side, shape, clipBehavior, focusNode, autofocus, color, backgroundColor, padding,
            visualDensity, materialTapTargetSize, elevation, shadowColor, surfaceTintColor, iconTheme,
            selectedShadowColor, showCheckmark, checkmarkColor, avatarBorder, avatarBoxConstraints,
            deleteIconBoxConstraints, chipAnimationStyle, mouseCursor, ChipVariant.Elevated, key);
    }

    public Widget? Avatar { get; }
    public Widget Label { get; }
    public TextStyle? LabelStyle { get; }
    public Thickness? LabelPadding { get; }
    public bool Selected { get; }
    public Action<bool>? OnSelected { get; }
    public Widget? DeleteIcon { get; }
    public Action? OnDeleted { get; }
    public Color? DeleteIconColor { get; }
    public string? DeleteButtonTooltipMessage { get; }
    public double? PressElevation { get; }
    public Color? DisabledColor { get; }
    public Color? SelectedColor { get; }
    public string? Tooltip { get; }
    public MaterialStateProperty<BorderSide?>? Side { get; }
    public MaterialStateProperty<ShapeBorder?>? Shape { get; }
    public Clip ClipBehavior { get; }
    public FocusNode? FocusNode { get; }
    public bool Autofocus { get; }
    public MaterialStateProperty<Color?>? Color { get; }
    public Color? BackgroundColor { get; }
    public Thickness? Padding { get; }
    public VisualDensity? VisualDensity { get; }
    public MaterialTapTargetSize? MaterialTapTargetSize { get; }
    public double? Elevation { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public IconThemeData? IconTheme { get; }
    public Color? SelectedShadowColor { get; }
    public bool? ShowCheckmark { get; }
    public Color? CheckmarkColor { get; }
    public ShapeBorder AvatarBorder { get; }
    public BoxConstraints? AvatarBoxConstraints { get; }
    public BoxConstraints? DeleteIconBoxConstraints { get; }
    public ChipAnimationStyle? ChipAnimationStyle { get; }
    public MouseCursor? MouseCursor { get; }
    internal ChipVariant Variant { get; }

    public bool IsEnabled => OnSelected is not null;

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var resolvedDeleteIcon = DeleteIcon ?? (theme.UseMaterial3 ? new Icon(Icons.Clear, size: 18) : null);
        return new RawChip(
            label: Label,
            avatar: Avatar,
            labelStyle: LabelStyle,
            labelPadding: LabelPadding,
            onPressed: null,
            onSelected: OnSelected,
            pressElevation: PressElevation,
            selected: Selected,
            showCheckmark: ShowCheckmark,
            checkmarkColor: CheckmarkColor,
            tooltip: Tooltip,
            side: Side,
            shape: Shape,
            clipBehavior: ClipBehavior,
            focusNode: FocusNode,
            autofocus: Autofocus,
            disabledColor: DisabledColor,
            selectedColor: SelectedColor,
            color: Color,
            backgroundColor: BackgroundColor,
            padding: Padding,
            visualDensity: VisualDensity,
            isEnabled: IsEnabled,
            materialTapTargetSize: MaterialTapTargetSize,
            elevation: Elevation,
            shadowColor: ShadowColor,
            surfaceTintColor: SurfaceTintColor,
            selectedShadowColor: SelectedShadowColor,
            avatarBorder: AvatarBorder,
            iconTheme: IconTheme,
            avatarBoxConstraints: AvatarBoxConstraints,
            chipAnimationStyle: ChipAnimationStyle,
            mouseCursor: MouseCursor,
            defaultsKind: ChipDefaultsKind.Filter,
            variant: Variant,
            deleteIcon: resolvedDeleteIcon,
            onDeleted: OnDeleted,
            deleteIconColor: DeleteIconColor,
            deleteButtonTooltipMessage: DeleteButtonTooltipMessage,
            deleteIconBoxConstraints: DeleteIconBoxConstraints);
    }

    private static void ValidateElevation(double? value, string parameterName)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Chip elevation must be non-negative and finite.");
        }
    }
}

public sealed class InputChip : StatelessWidget
{
    public InputChip(
        Widget label,
        Widget? avatar = null,
        TextStyle? labelStyle = null,
        Thickness? labelPadding = null,
        bool selected = false,
        bool isEnabled = true,
        Action<bool>? onSelected = null,
        Widget? deleteIcon = null,
        Action? onDeleted = null,
        Color? deleteIconColor = null,
        string? deleteButtonTooltipMessage = null,
        Action? onPressed = null,
        double? pressElevation = null,
        Color? disabledColor = null,
        Color? selectedColor = null,
        string? tooltip = null,
        MaterialStateProperty<BorderSide?>? side = null,
        MaterialStateProperty<ShapeBorder?>? shape = null,
        Clip clipBehavior = Clip.None,
        FocusNode? focusNode = null,
        bool autofocus = false,
        MaterialStateProperty<Color?>? color = null,
        Color? backgroundColor = null,
        Thickness? padding = null,
        VisualDensity? visualDensity = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        IconThemeData? iconTheme = null,
        Color? selectedShadowColor = null,
        bool? showCheckmark = null,
        Color? checkmarkColor = null,
        ShapeBorder? avatarBorder = null,
        BoxConstraints? avatarBoxConstraints = null,
        BoxConstraints? deleteIconBoxConstraints = null,
        ChipAnimationStyle? chipAnimationStyle = null,
        MouseCursor? mouseCursor = null,
        Key? key = null) : base(key)
    {
        if (onPressed is not null && onSelected is not null)
        {
            throw new ArgumentException("InputChip accepts onPressed or onSelected, but not both.");
        }
        ValidateElevation(pressElevation, nameof(pressElevation));
        ValidateElevation(elevation, nameof(elevation));

        Label = label ?? throw new ArgumentNullException(nameof(label));
        Avatar = avatar;
        LabelStyle = labelStyle;
        LabelPadding = labelPadding;
        Selected = selected;
        IsEnabled = isEnabled;
        OnSelected = onSelected;
        DeleteIcon = deleteIcon;
        OnDeleted = onDeleted;
        DeleteIconColor = deleteIconColor;
        DeleteButtonTooltipMessage = deleteButtonTooltipMessage;
        OnPressed = onPressed;
        PressElevation = pressElevation;
        DisabledColor = disabledColor;
        SelectedColor = selectedColor;
        Tooltip = tooltip;
        Side = side;
        Shape = shape;
        ClipBehavior = clipBehavior;
        FocusNode = focusNode;
        Autofocus = autofocus;
        Color = color;
        BackgroundColor = backgroundColor;
        Padding = padding;
        VisualDensity = visualDensity;
        MaterialTapTargetSize = materialTapTargetSize;
        Elevation = elevation;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        IconTheme = iconTheme;
        SelectedShadowColor = selectedShadowColor;
        ShowCheckmark = showCheckmark;
        CheckmarkColor = checkmarkColor;
        AvatarBorder = avatarBorder ?? ShapeBorder.RoundedRectangle(10_000);
        AvatarBoxConstraints = avatarBoxConstraints;
        DeleteIconBoxConstraints = deleteIconBoxConstraints;
        ChipAnimationStyle = chipAnimationStyle;
        MouseCursor = mouseCursor;
    }

    public Widget? Avatar { get; }
    public Widget Label { get; }
    public TextStyle? LabelStyle { get; }
    public Thickness? LabelPadding { get; }
    public bool Selected { get; }
    public bool IsEnabled { get; }
    public Action<bool>? OnSelected { get; }
    public Widget? DeleteIcon { get; }
    public Action? OnDeleted { get; }
    public Color? DeleteIconColor { get; }
    public string? DeleteButtonTooltipMessage { get; }
    public Action? OnPressed { get; }
    public double? PressElevation { get; }
    public Color? DisabledColor { get; }
    public Color? SelectedColor { get; }
    public string? Tooltip { get; }
    public MaterialStateProperty<BorderSide?>? Side { get; }
    public MaterialStateProperty<ShapeBorder?>? Shape { get; }
    public Clip ClipBehavior { get; }
    public FocusNode? FocusNode { get; }
    public bool Autofocus { get; }
    public MaterialStateProperty<Color?>? Color { get; }
    public Color? BackgroundColor { get; }
    public Thickness? Padding { get; }
    public VisualDensity? VisualDensity { get; }
    public MaterialTapTargetSize? MaterialTapTargetSize { get; }
    public double? Elevation { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public IconThemeData? IconTheme { get; }
    public Color? SelectedShadowColor { get; }
    public bool? ShowCheckmark { get; }
    public Color? CheckmarkColor { get; }
    public ShapeBorder AvatarBorder { get; }
    public BoxConstraints? AvatarBoxConstraints { get; }
    public BoxConstraints? DeleteIconBoxConstraints { get; }
    public ChipAnimationStyle? ChipAnimationStyle { get; }
    public MouseCursor? MouseCursor { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        bool enabled = IsEnabled && (OnSelected is not null || OnDeleted is not null || OnPressed is not null);
        var resolvedDeleteIcon = DeleteIcon ?? (theme.UseMaterial3 ? new Icon(Icons.Clear, size: 18) : null);
        return new RawChip(
            label: Label,
            avatar: Avatar,
            labelStyle: LabelStyle,
            labelPadding: LabelPadding,
            onPressed: OnPressed,
            onSelected: OnSelected,
            pressElevation: PressElevation,
            selected: Selected,
            showCheckmark: ShowCheckmark,
            checkmarkColor: CheckmarkColor,
            tooltip: Tooltip,
            side: Side,
            shape: Shape,
            clipBehavior: ClipBehavior,
            focusNode: FocusNode,
            autofocus: Autofocus,
            disabledColor: DisabledColor,
            selectedColor: SelectedColor,
            color: Color,
            backgroundColor: BackgroundColor,
            padding: Padding,
            visualDensity: VisualDensity,
            isEnabled: enabled,
            materialTapTargetSize: MaterialTapTargetSize,
            elevation: Elevation,
            shadowColor: ShadowColor,
            surfaceTintColor: SurfaceTintColor,
            selectedShadowColor: SelectedShadowColor,
            avatarBorder: AvatarBorder,
            iconTheme: IconTheme,
            avatarBoxConstraints: AvatarBoxConstraints,
            chipAnimationStyle: ChipAnimationStyle,
            mouseCursor: MouseCursor,
            defaultsKind: ChipDefaultsKind.Input,
            variant: ChipVariant.Flat,
            deleteIcon: resolvedDeleteIcon,
            onDeleted: OnDeleted,
            deleteIconColor: DeleteIconColor,
            deleteButtonTooltipMessage: DeleteButtonTooltipMessage,
            deleteIconBoxConstraints: DeleteIconBoxConstraints);
    }

    private static void ValidateElevation(double? value, string parameterName)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Chip elevation must be non-negative and finite.");
        }
    }
}
