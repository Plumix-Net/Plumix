using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// material_ui/lib/src/chip.dart
// material_ui/lib/src/action_chip.dart
// material_ui/lib/src/choice_chip.dart

public sealed record ChipAnimationStyle(
    AnimationStyle? EnableAnimation = null,
    AnimationStyle? SelectAnimation = null,
    AnimationStyle? AvatarDrawerAnimation = null,
    AnimationStyle? DeleteDrawerAnimation = null);

internal enum ChipVariant
{
    Flat,
    Elevated,
}

internal enum ChipDefaultsKind
{
    Raw,
    Action,
    Choice,
    Filter,
    Input,
}

public sealed class ActionChip : StatelessWidget
{
    public ActionChip(
        Widget label,
        Action? onPressed,
        Widget? avatar = null,
        TextStyle? labelStyle = null,
        Thickness? labelPadding = null,
        double? pressElevation = null,
        string? tooltip = null,
        MaterialStateProperty<BorderSide?>? side = null,
        MaterialStateProperty<ShapeBorder?>? shape = null,
        Clip clipBehavior = Clip.None,
        FocusNode? focusNode = null,
        bool autofocus = false,
        MaterialStateProperty<Color?>? color = null,
        Color? backgroundColor = null,
        Color? disabledColor = null,
        Thickness? padding = null,
        VisualDensity? visualDensity = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        IconThemeData? iconTheme = null,
        BoxConstraints? avatarBoxConstraints = null,
        ChipAnimationStyle? chipAnimationStyle = null,
        MouseCursor? mouseCursor = null,
        Key? key = null) : this(
            label, onPressed, avatar, labelStyle, labelPadding, pressElevation, tooltip, side, shape,
            clipBehavior, focusNode, autofocus, color, backgroundColor, disabledColor, padding,
            visualDensity, materialTapTargetSize, elevation, shadowColor, surfaceTintColor, iconTheme,
            avatarBoxConstraints, chipAnimationStyle, mouseCursor, ChipVariant.Flat, key)
    {
    }

    private ActionChip(
        Widget label,
        Action? onPressed,
        Widget? avatar,
        TextStyle? labelStyle,
        Thickness? labelPadding,
        double? pressElevation,
        string? tooltip,
        MaterialStateProperty<BorderSide?>? side,
        MaterialStateProperty<ShapeBorder?>? shape,
        Clip clipBehavior,
        FocusNode? focusNode,
        bool autofocus,
        MaterialStateProperty<Color?>? color,
        Color? backgroundColor,
        Color? disabledColor,
        Thickness? padding,
        VisualDensity? visualDensity,
        MaterialTapTargetSize? materialTapTargetSize,
        double? elevation,
        Color? shadowColor,
        Color? surfaceTintColor,
        IconThemeData? iconTheme,
        BoxConstraints? avatarBoxConstraints,
        ChipAnimationStyle? chipAnimationStyle,
        MouseCursor? mouseCursor,
        ChipVariant variant,
        Key? key) : base(key)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        ValidateElevation(pressElevation, nameof(pressElevation));
        ValidateElevation(elevation, nameof(elevation));
        Avatar = avatar;
        OnPressed = onPressed;
        LabelStyle = labelStyle;
        LabelPadding = labelPadding;
        PressElevation = pressElevation;
        Tooltip = tooltip;
        Side = side;
        Shape = shape;
        ClipBehavior = clipBehavior;
        FocusNode = focusNode;
        Autofocus = autofocus;
        Color = color;
        BackgroundColor = backgroundColor;
        DisabledColor = disabledColor;
        Padding = padding;
        VisualDensity = visualDensity;
        MaterialTapTargetSize = materialTapTargetSize;
        Elevation = elevation;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        IconTheme = iconTheme;
        AvatarBoxConstraints = avatarBoxConstraints;
        ChipAnimationStyle = chipAnimationStyle;
        MouseCursor = mouseCursor;
        Variant = variant;
    }

    public static ActionChip Elevated(
        Widget label,
        Action? onPressed,
        Widget? avatar = null,
        TextStyle? labelStyle = null,
        Thickness? labelPadding = null,
        double? pressElevation = null,
        string? tooltip = null,
        MaterialStateProperty<BorderSide?>? side = null,
        MaterialStateProperty<ShapeBorder?>? shape = null,
        Clip clipBehavior = Clip.None,
        FocusNode? focusNode = null,
        bool autofocus = false,
        MaterialStateProperty<Color?>? color = null,
        Color? backgroundColor = null,
        Color? disabledColor = null,
        Thickness? padding = null,
        VisualDensity? visualDensity = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        IconThemeData? iconTheme = null,
        BoxConstraints? avatarBoxConstraints = null,
        ChipAnimationStyle? chipAnimationStyle = null,
        MouseCursor? mouseCursor = null,
        Key? key = null)
    {
        return new ActionChip(
            label, onPressed, avatar, labelStyle, labelPadding, pressElevation, tooltip, side, shape,
            clipBehavior, focusNode, autofocus, color, backgroundColor, disabledColor, padding,
            visualDensity, materialTapTargetSize, elevation, shadowColor, surfaceTintColor, iconTheme,
            avatarBoxConstraints, chipAnimationStyle, mouseCursor, ChipVariant.Elevated, key);
    }

    public Widget? Avatar { get; }
    public Widget Label { get; }
    public TextStyle? LabelStyle { get; }
    public Thickness? LabelPadding { get; }
    public Action? OnPressed { get; }
    public double? PressElevation { get; }
    public string? Tooltip { get; }
    public MaterialStateProperty<BorderSide?>? Side { get; }
    public MaterialStateProperty<ShapeBorder?>? Shape { get; }
    public Clip ClipBehavior { get; }
    public FocusNode? FocusNode { get; }
    public bool Autofocus { get; }
    public MaterialStateProperty<Color?>? Color { get; }
    public Color? BackgroundColor { get; }
    public Color? DisabledColor { get; }
    public Thickness? Padding { get; }
    public VisualDensity? VisualDensity { get; }
    public MaterialTapTargetSize? MaterialTapTargetSize { get; }
    public double? Elevation { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public IconThemeData? IconTheme { get; }
    public BoxConstraints? AvatarBoxConstraints { get; }
    public ChipAnimationStyle? ChipAnimationStyle { get; }
    public MouseCursor? MouseCursor { get; }
    internal ChipVariant Variant { get; }

    public bool IsEnabled => OnPressed is not null;

    public override Widget Build(BuildContext context)
    {
        return new RawChip(
            avatar: Avatar,
            label: Label,
            labelStyle: LabelStyle,
            labelPadding: LabelPadding,
            onPressed: OnPressed,
            onSelected: null,
            pressElevation: PressElevation,
            selected: false,
            showCheckmark: null,
            checkmarkColor: null,
            tooltip: Tooltip,
            side: Side,
            shape: Shape,
            clipBehavior: ClipBehavior,
            focusNode: FocusNode,
            autofocus: Autofocus,
            color: Color,
            backgroundColor: BackgroundColor,
            disabledColor: DisabledColor,
            selectedColor: null,
            padding: Padding,
            visualDensity: VisualDensity,
            isEnabled: IsEnabled,
            materialTapTargetSize: MaterialTapTargetSize,
            elevation: Elevation,
            shadowColor: ShadowColor,
            surfaceTintColor: SurfaceTintColor,
            selectedShadowColor: null,
            avatarBorder: null,
            iconTheme: IconTheme,
            avatarBoxConstraints: AvatarBoxConstraints,
            chipAnimationStyle: ChipAnimationStyle,
            mouseCursor: MouseCursor,
            defaultsKind: ChipDefaultsKind.Action,
            variant: Variant);
    }

    private static void ValidateElevation(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(name, "Chip elevation must be non-negative and finite.");
        }
    }
}

public sealed class ChoiceChip : StatelessWidget
{
    public ChoiceChip(
        Widget label,
        bool selected,
        Action<bool>? onSelected,
        Widget? avatar = null,
        TextStyle? labelStyle = null,
        Thickness? labelPadding = null,
        double? pressElevation = null,
        Color? selectedColor = null,
        Color? disabledColor = null,
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
        ChipAnimationStyle? chipAnimationStyle = null,
        MouseCursor? mouseCursor = null,
        Key? key = null) : this(
            label, selected, onSelected, avatar, labelStyle, labelPadding, pressElevation,
            selectedColor, disabledColor, tooltip, side, shape, clipBehavior, focusNode, autofocus,
            color, backgroundColor, padding, visualDensity, materialTapTargetSize, elevation,
            shadowColor, surfaceTintColor, iconTheme, selectedShadowColor, showCheckmark,
            checkmarkColor, avatarBorder, avatarBoxConstraints, chipAnimationStyle, mouseCursor,
            ChipVariant.Flat, key)
    {
    }

    private ChoiceChip(
        Widget label,
        bool selected,
        Action<bool>? onSelected,
        Widget? avatar,
        TextStyle? labelStyle,
        Thickness? labelPadding,
        double? pressElevation,
        Color? selectedColor,
        Color? disabledColor,
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
        ChipAnimationStyle? chipAnimationStyle,
        MouseCursor? mouseCursor,
        ChipVariant variant,
        Key? key) : base(key)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        if (pressElevation.HasValue && (!double.IsFinite(pressElevation.Value) || pressElevation.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(pressElevation));
        }
        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(elevation));
        }

        Selected = selected;
        OnSelected = onSelected;
        Avatar = avatar;
        LabelStyle = labelStyle;
        LabelPadding = labelPadding;
        PressElevation = pressElevation;
        SelectedColor = selectedColor;
        DisabledColor = disabledColor;
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
        AvatarBorder = avatarBorder ?? new RoundedRectangleBorder(borderRadius:
            Plumix.Rendering.BorderRadius.Circular(10_000));
        AvatarBoxConstraints = avatarBoxConstraints;
        ChipAnimationStyle = chipAnimationStyle;
        MouseCursor = mouseCursor;
        Variant = variant;
    }

    public static ChoiceChip Elevated(
        Widget label,
        bool selected,
        Action<bool>? onSelected,
        Widget? avatar = null,
        TextStyle? labelStyle = null,
        Thickness? labelPadding = null,
        double? pressElevation = null,
        Color? selectedColor = null,
        Color? disabledColor = null,
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
        ChipAnimationStyle? chipAnimationStyle = null,
        MouseCursor? mouseCursor = null,
        Key? key = null)
    {
        return new ChoiceChip(
            label, selected, onSelected, avatar, labelStyle, labelPadding, pressElevation,
            selectedColor, disabledColor, tooltip, side, shape, clipBehavior, focusNode, autofocus,
            color, backgroundColor, padding, visualDensity, materialTapTargetSize, elevation,
            shadowColor, surfaceTintColor, iconTheme, selectedShadowColor, showCheckmark,
            checkmarkColor, avatarBorder, avatarBoxConstraints, chipAnimationStyle, mouseCursor,
            ChipVariant.Elevated, key);
    }

    public Widget? Avatar { get; }
    public Widget Label { get; }
    public TextStyle? LabelStyle { get; }
    public Thickness? LabelPadding { get; }
    public Action<bool>? OnSelected { get; }
    public double? PressElevation { get; }
    public bool Selected { get; }
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
    public Color? SelectedShadowColor { get; }
    public bool? ShowCheckmark { get; }
    public Color? CheckmarkColor { get; }
    public ShapeBorder AvatarBorder { get; }
    public IconThemeData? IconTheme { get; }
    public BoxConstraints? AvatarBoxConstraints { get; }
    public ChipAnimationStyle? ChipAnimationStyle { get; }
    public MouseCursor? MouseCursor { get; }
    internal ChipVariant Variant { get; }

    public bool IsEnabled => OnSelected is not null;

    public override Widget Build(BuildContext context)
    {
        var chipTheme = ChipTheme.Of(context);
        return new RawChip(
            avatar: Avatar,
            label: Label,
            labelStyle: LabelStyle ?? (Selected ? chipTheme.SecondaryLabelStyle : null),
            labelPadding: LabelPadding,
            onPressed: null,
            onSelected: OnSelected,
            pressElevation: PressElevation,
            selected: Selected,
            showCheckmark: ShowCheckmark ?? chipTheme.ShowCheckmark ?? Theme.Of(context).UseMaterial3,
            checkmarkColor: CheckmarkColor,
            tooltip: Tooltip,
            side: Side,
            shape: Shape,
            clipBehavior: ClipBehavior,
            focusNode: FocusNode,
            autofocus: Autofocus,
            disabledColor: DisabledColor,
            selectedColor: SelectedColor ?? chipTheme.SecondarySelectedColor,
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
            defaultsKind: ChipDefaultsKind.Choice,
            variant: Variant);
    }
}

public sealed class RawChip : StatefulWidget
{
    public RawChip(
        Widget label,
        Widget? avatar = null,
        TextStyle? labelStyle = null,
        Thickness? labelPadding = null,
        Action? onPressed = null,
        Action<bool>? onSelected = null,
        double? pressElevation = null,
        bool selected = false,
        bool? showCheckmark = null,
        Color? checkmarkColor = null,
        string? tooltip = null,
        MaterialStateProperty<BorderSide?>? side = null,
        MaterialStateProperty<ShapeBorder?>? shape = null,
        Clip clipBehavior = Clip.None,
        FocusNode? focusNode = null,
        bool autofocus = false,
        Color? disabledColor = null,
        Color? selectedColor = null,
        MaterialStateProperty<Color?>? color = null,
        Color? backgroundColor = null,
        Thickness? padding = null,
        VisualDensity? visualDensity = null,
        bool isEnabled = true,
        bool tapEnabled = true,
        MaterialTapTargetSize? materialTapTargetSize = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        Color? selectedShadowColor = null,
        ShapeBorder? avatarBorder = null,
        IconThemeData? iconTheme = null,
        BoxConstraints? avatarBoxConstraints = null,
        ChipAnimationStyle? chipAnimationStyle = null,
        MouseCursor? mouseCursor = null,
        Widget? deleteIcon = null,
        Action? onDeleted = null,
        Color? deleteIconColor = null,
        string? deleteButtonTooltipMessage = null,
        BoxConstraints? deleteIconBoxConstraints = null,
        Key? key = null) : this(
            label, avatar, labelStyle, labelPadding, onPressed, onSelected, pressElevation, selected,
            showCheckmark, checkmarkColor, tooltip, side, shape, clipBehavior, focusNode, autofocus,
            disabledColor, selectedColor, color, backgroundColor, padding, visualDensity, isEnabled,
            materialTapTargetSize, elevation, shadowColor, surfaceTintColor, selectedShadowColor,
            avatarBorder, iconTheme, avatarBoxConstraints, chipAnimationStyle, mouseCursor,
            ChipDefaultsKind.Raw, ChipVariant.Flat,
            deleteIcon, onDeleted, deleteIconColor, deleteButtonTooltipMessage,
            deleteIconBoxConstraints, tapEnabled, key)
    {
    }

    internal RawChip(
        Widget label,
        Widget? avatar,
        TextStyle? labelStyle,
        Thickness? labelPadding,
        Action? onPressed,
        Action<bool>? onSelected,
        double? pressElevation,
        bool selected,
        bool? showCheckmark,
        Color? checkmarkColor,
        string? tooltip,
        MaterialStateProperty<BorderSide?>? side,
        MaterialStateProperty<ShapeBorder?>? shape,
        Clip clipBehavior,
        FocusNode? focusNode,
        bool autofocus,
        Color? disabledColor,
        Color? selectedColor,
        MaterialStateProperty<Color?>? color,
        Color? backgroundColor,
        Thickness? padding,
        VisualDensity? visualDensity,
        bool isEnabled,
        MaterialTapTargetSize? materialTapTargetSize,
        double? elevation,
        Color? shadowColor,
        Color? surfaceTintColor,
        Color? selectedShadowColor,
        ShapeBorder? avatarBorder,
        IconThemeData? iconTheme,
        BoxConstraints? avatarBoxConstraints,
        ChipAnimationStyle? chipAnimationStyle,
        MouseCursor? mouseCursor,
        ChipDefaultsKind defaultsKind,
        ChipVariant variant,
        Widget? deleteIcon = null,
        Action? onDeleted = null,
        Color? deleteIconColor = null,
        string? deleteButtonTooltipMessage = null,
        BoxConstraints? deleteIconBoxConstraints = null,
        bool tapEnabled = true,
        Key? key = null) : base(key)
    {
        if (onPressed is not null && onSelected is not null)
        {
            throw new ArgumentException("RawChip accepts onPressed or onSelected, but not both.");
        }
        if (pressElevation.HasValue && (!double.IsFinite(pressElevation.Value) || pressElevation.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(pressElevation));
        }
        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(elevation));
        }

        Label = label ?? throw new ArgumentNullException(nameof(label));
        Avatar = avatar;
        LabelStyle = labelStyle;
        LabelPadding = labelPadding;
        OnPressed = onPressed;
        OnSelected = onSelected;
        PressElevation = pressElevation;
        Selected = selected;
        ShowCheckmark = showCheckmark;
        CheckmarkColor = checkmarkColor;
        Tooltip = tooltip;
        Side = side;
        Shape = shape;
        ClipBehavior = clipBehavior;
        FocusNode = focusNode;
        Autofocus = autofocus;
        DisabledColor = disabledColor;
        SelectedColor = selectedColor;
        Color = color;
        BackgroundColor = backgroundColor;
        Padding = padding;
        VisualDensity = visualDensity;
        IsEnabled = isEnabled;
        MaterialTapTargetSize = materialTapTargetSize;
        Elevation = elevation;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        SelectedShadowColor = selectedShadowColor;
        AvatarBorder = avatarBorder ?? new RoundedRectangleBorder(borderRadius:
            Plumix.Rendering.BorderRadius.Circular(10_000));
        IconTheme = iconTheme;
        AvatarBoxConstraints = avatarBoxConstraints;
        DeleteIcon = deleteIcon ?? new Icon(Icons.Cancel);
        OnDeleted = onDeleted;
        DeleteIconColor = deleteIconColor;
        DeleteButtonTooltipMessage = deleteButtonTooltipMessage;
        DeleteIconBoxConstraints = deleteIconBoxConstraints;
        ChipAnimationStyle = chipAnimationStyle;
        MouseCursor = mouseCursor;
        TapEnabled = tapEnabled;
        DefaultsKind = defaultsKind;
        Variant = variant;
    }

    public Widget? Avatar { get; }
    public Widget Label { get; }
    public TextStyle? LabelStyle { get; }
    public Thickness? LabelPadding { get; }
    public Action? OnPressed { get; }
    public Action<bool>? OnSelected { get; }
    public double? PressElevation { get; }
    public bool Selected { get; }
    public bool? ShowCheckmark { get; }
    public Color? CheckmarkColor { get; }
    public string? Tooltip { get; }
    public MaterialStateProperty<BorderSide?>? Side { get; }
    public MaterialStateProperty<ShapeBorder?>? Shape { get; }
    public Clip ClipBehavior { get; }
    public FocusNode? FocusNode { get; }
    public bool Autofocus { get; }
    public Color? DisabledColor { get; }
    public Color? SelectedColor { get; }
    public MaterialStateProperty<Color?>? Color { get; }
    public Color? BackgroundColor { get; }
    public Thickness? Padding { get; }
    public VisualDensity? VisualDensity { get; }
    public bool IsEnabled { get; }
    public MaterialTapTargetSize? MaterialTapTargetSize { get; }
    public double? Elevation { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public Color? SelectedShadowColor { get; }
    public ShapeBorder AvatarBorder { get; }
    public IconThemeData? IconTheme { get; }
    public BoxConstraints? AvatarBoxConstraints { get; }
    public Widget DeleteIcon { get; }
    public Action? OnDeleted { get; }
    public Color? DeleteIconColor { get; }
    public string? DeleteButtonTooltipMessage { get; }
    public BoxConstraints? DeleteIconBoxConstraints { get; }
    public ChipAnimationStyle? ChipAnimationStyle { get; }
    public MouseCursor? MouseCursor { get; }
    public bool TapEnabled { get; }
    internal ChipDefaultsKind DefaultsKind { get; }
    internal ChipVariant Variant { get; }

    public bool CanTapBody => IsEnabled
                              && TapEnabled
                              && (OnPressed is not null || OnSelected is not null);

    public bool CanDelete => IsEnabled && OnDeleted is not null;

    public override State CreateState() => new RawChipState();

    private sealed class RawChipState : State
    {
        private AnimationController? _selectionController;
        private double _selectionProgress;
        private double _checkmarkProgress;
        private AnimationController? _avatarDrawerController;
        private double _avatarDrawerProgress;
        private AnimationController? _deleteController;
        private double _deleteProgress;
        private AnimationController? _enableController;
        private double _enableProgress;
        private readonly MaterialStatesController _statesController = new();
        private bool _isTapping;

        private RawChip CurrentWidget => (RawChip)StateWidget;

        /// Dart's `_RawChipState.canTap`.
        private bool CanTap => CurrentWidget.CanTapBody;

        /// Dart's `_RawChipState.isTapping`.
        private bool IsTapping => CanTap && _isTapping;

        public override void InitState()
        {
            _statesController.Update(MaterialState.Disabled, !CurrentWidget.IsEnabled);
            _statesController.Update(MaterialState.Selected, CurrentWidget.Selected);
            _statesController.AddListener(HandleStatesChanged);

            _selectionController = CreateController(
                CurrentWidget.ChipAnimationStyle?.SelectAnimation,
                TimeSpan.FromMilliseconds(195));
            _selectionController.SetValue(CurrentWidget.Selected ? 1.0 : 0.0);
            _selectionController.Changed += HandleSelectionTick;

            _avatarDrawerController = CreateController(
                CurrentWidget.ChipAnimationStyle?.AvatarDrawerAnimation,
                TimeSpan.FromMilliseconds(150));
            _avatarDrawerController.SetValue(CurrentWidget.Avatar is not null || CurrentWidget.Selected ? 1.0 : 0.0);
            _avatarDrawerController.Changed += HandleAvatarDrawerTick;

            _deleteController = CreateController(
                CurrentWidget.ChipAnimationStyle?.DeleteDrawerAnimation,
                TimeSpan.FromMilliseconds(150));
            _deleteController.SetValue(CurrentWidget.OnDeleted is null ? 0.0 : 1.0);
            _deleteController.Changed += HandleDeleteTick;

            _enableController = CreateController(
                CurrentWidget.ChipAnimationStyle?.EnableAnimation,
                TimeSpan.FromMilliseconds(75));
            _enableController.SetValue(CurrentWidget.IsEnabled ? 1.0 : 0.0);
            _enableController.Changed += HandleEnableTick;

            UpdateAnimationProgress();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldChip = (RawChip)oldWidget;
            _statesController.Update(MaterialState.Disabled, !CurrentWidget.IsEnabled);
            _statesController.Update(MaterialState.Selected, CurrentWidget.Selected);
            if (oldChip.IsEnabled != CurrentWidget.IsEnabled)
            {
                if (CurrentWidget.IsEnabled)
                {
                    _enableController!.Forward();
                }
                else
                {
                    _enableController!.Reverse();
                }
            }

            if (!ReferenceEquals(oldChip.Avatar, CurrentWidget.Avatar)
                || oldChip.Selected != CurrentWidget.Selected)
            {
                if (CurrentWidget.Avatar is not null || CurrentWidget.Selected)
                {
                    _avatarDrawerController!.Forward();
                }
                else
                {
                    _avatarDrawerController!.Reverse();
                }
            }

            if (oldChip.Selected != CurrentWidget.Selected)
            {
                if (CurrentWidget.Selected)
                {
                    _selectionController!.Forward();
                }
                else
                {
                    _selectionController!.Reverse();
                }
            }

            if (!Equals(oldChip.OnDeleted, CurrentWidget.OnDeleted))
            {
                if (CurrentWidget.OnDeleted is not null)
                {
                    _deleteController!.Forward();
                }
                else
                {
                    _deleteController!.Reverse();
                }
            }
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var theme = Theme.Of(context);
            var chipTheme = ChipTheme.Of(context);
            var defaults = ResolveDefaults(context, theme, widget);
            var padding = widget.Padding ?? chipTheme.Padding ?? defaults.Padding ?? new Thickness(4);
            var baseLabelStyle = chipTheme.LabelStyle ?? defaults.LabelStyle ?? theme.TextTheme.BodyLarge;
            var labelStyle = MergeTextStyles(baseLabelStyle, widget.LabelStyle);
            // Dart: the chip starts at 8px on each side and interpolates to 4px as text scaling
            // approaches 2, staying at 4px beyond that.
            double effectiveTextScale = ButtonStyleButton.EffectiveTextScale(context, labelStyle.FontSize ?? 14);
            EdgeInsetsGeometry defaultLabelPadding = EdgeInsetsGeometry.Lerp(
                EdgeInsetsGeometry.Symmetric(horizontal: 8.0),
                EdgeInsetsGeometry.Symmetric(horizontal: 4.0),
                Math.Clamp(effectiveTextScale - 1.0, 0.0, 1.0))!.Value;
            Thickness labelPadding = widget.LabelPadding
                                     ?? chipTheme.LabelPadding
                                     ?? defaults.LabelPadding
                                     ?? defaultLabelPadding.Resolve(Directionality.Of(context));
            var density = widget.VisualDensity ?? theme.VisualDensity;
            var tapTargetSize = widget.MaterialTapTargetSize ?? theme.MaterialTapTargetSize;
            var effectiveIconTheme = widget.IconTheme ?? chipTheme.IconTheme ?? defaults.IconTheme;
            bool showCheckmark = widget.ShowCheckmark ?? chipTheme.ShowCheckmark ?? defaults.ShowCheckmark ?? true;
            Color checkmarkColor = widget.CheckmarkColor
                                   ?? chipTheme.CheckmarkColor
                                   ?? defaults.CheckmarkColor
                                   ?? ResolveDefaultCheckmarkColor(theme.Brightness, widget.Avatar is not null);

            MaterialState states = _statesController.Value;
            ShapeBorder resolvedShape = ResolveShape(states, widget, chipTheme, defaults);
            double elevation = widget.Elevation ?? chipTheme.Elevation ?? defaults.Elevation ?? 0;
            double pressElevation = widget.PressElevation ?? chipTheme.PressElevation ?? defaults.PressElevation ?? 0;
            Color? shadowColor = widget.ShadowColor ?? chipTheme.ShadowColor ?? defaults.ShadowColor;
            Color? selectedShadowColor = widget.SelectedShadowColor
                                         ?? chipTheme.SelectedShadowColor
                                         ?? defaults.SelectedShadowColor;
            Color? surfaceTintColor = widget.SurfaceTintColor
                                      ?? chipTheme.SurfaceTintColor
                                      ?? defaults.SurfaceTintColor;
            TextStyle resolvedLabelStyle = labelStyle with
            {
                Color = ResolveLabelColor(states, widget, chipTheme, defaults, labelStyle, theme)
            };

            Widget label = new DefaultTextStyle(
                style: resolvedLabelStyle,
                overflow: TextOverflow.Fade,
                textAlign: TextAlign.Start,
                maxLines: 1,
                softWrap: false,
                child: widget.Label);
            Widget? leading = BuildAvatar(widget, effectiveIconTheme);
            var delete = BuildDelete(
                context,
                widget,
                chipTheme,
                defaults,
                effectiveIconTheme,
                density,
                tapTargetSize);
            Widget content = new ChipRenderWidget(
                label: label,
                avatar: leading,
                deleteIcon: delete,
                padding: padding,
                labelPadding: labelPadding,
                visualDensity: density,
                textDirection: Directionality.Of(context),
                isEnabled: widget.IsEnabled,
                canTap: widget.CanTapBody,
                showCheckmark: showCheckmark,
                checkmarkColor: checkmarkColor,
                avatarBorder: widget.AvatarBorder,
                avatarBoxConstraints: widget.AvatarBoxConstraints ?? chipTheme.AvatarBoxConstraints,
                deleteIconBoxConstraints: widget.DeleteIconBoxConstraints ?? chipTheme.DeleteIconBoxConstraints,
                checkmarkProgress: _checkmarkProgress,
                avatarDrawerProgress: _avatarDrawerProgress,
                deleteDrawerProgress: _deleteProgress,
                enableProgress: _enableProgress);

            if (!string.IsNullOrEmpty(widget.Tooltip) && widget.CanTapBody)
            {
                content = new Tooltip(message: widget.Tooltip!, child: content);
            }

            Widget result = new global::Plumix.Material.Material(
                elevation: IsTapping ? pressElevation : elevation,
                shadowColor: widget.Selected ? selectedShadowColor : shadowColor,
                surfaceTintColor: surfaceTintColor,
                animationDuration: PressedAnimationDuration,
                shape: resolvedShape,
                clipBehavior: widget.ClipBehavior,
                child: new InkWell(
                    onFocusChange: value => _statesController.Update(MaterialState.Focused, value),
                    focusNode: widget.FocusNode,
                    autofocus: widget.Autofocus,
                    canRequestFocus: widget.IsEnabled,
                    onTap: CanTap ? HandleTap : null,
                    onTapDown: CanTap ? _ => HandleTapDown() : null,
                    onTapCancel: CanTap ? HandleTapCancel : null,
                    onHover: CanTap
                        ? value => _statesController.Update(MaterialState.Hovered, value)
                        : null,
                    mouseCursor: widget.MouseCursor,
                    hoverColor: (widget.Color ?? chipTheme.Color) is null ? null : Colors.Transparent,
                    customBorder: resolvedShape,
                    child: new Ink(
                        decoration: new ShapeDecoration(
                            Shape: resolvedShape,
                            Color: ResolveBackground(states, widget, chipTheme, defaults)),
                        child: content)));

            Vector densityAdjustment = density.BaseSizeAdjustment;
            BoxConstraints constraints = tapTargetSize == Plumix.Material.MaterialTapTargetSize.Padded
                ? new BoxConstraints(
                    MinWidth: WidgetConstants.MinInteractiveDimension + densityAdjustment.X,
                    MinHeight: WidgetConstants.MinInteractiveDimension + densityAdjustment.Y)
                : new BoxConstraints();
            result = new ChipRedirectingHitDetectionWidget(
                constraints: constraints,
                child: new Center(widthFactor: 1.0, heightFactor: 1.0, child: result));

            return new Semantics(
                flags: widget.TapEnabled ? SemanticsFlags.IsButton : SemanticsFlags.None,
                container: true,
                selected: widget.Selected,
                enabled: widget.TapEnabled ? widget.CanTapBody : null,
                child: result);
        }

        /// Dart's `_kPressedAnimationDuration`.
        private static readonly TimeSpan PressedAnimationDuration = TimeSpan.FromMilliseconds(75);

        private void HandleStatesChanged() => SetState(() => { });

        /// Dart's `_RawChipState._handleTapDown`.
        private void HandleTapDown()
        {
            if (!CanTap)
            {
                return;
            }

            _statesController.Update(MaterialState.Pressed, true);
            SetState(() => _isTapping = true);
        }

        /// Dart's `_RawChipState._handleTapCancel`.
        private void HandleTapCancel()
        {
            if (!CanTap)
            {
                return;
            }

            _statesController.Update(MaterialState.Pressed, false);
            SetState(() => _isTapping = false);
        }

        /// Dart's `_RawChipState._handleTap`.
        private void HandleTap()
        {
            if (!CanTap)
            {
                return;
            }

            _statesController.Update(MaterialState.Pressed, false);
            SetState(() => _isTapping = false);
            RawChip widget = CurrentWidget;
            // Only one of these can be set, so only one will be called.
            widget.OnSelected?.Invoke(!widget.Selected);
            widget.OnPressed?.Invoke();
        }

        public override void Dispose()
        {
            _statesController.RemoveListener(HandleStatesChanged);
            _statesController.Dispose();
            DisposeSelectionController();
            DisposeAvatarDrawerController();
            DisposeDeleteController();
            DisposeEnableController();
        }

        private Widget? BuildDelete(
            BuildContext context,
            RawChip widget,
            ChipThemeData chipTheme,
            ChipThemeData defaults,
            IconThemeData? iconTheme,
            VisualDensity density,
            MaterialTapTargetSize tapTargetSize)
        {
            if (_deleteProgress <= 0) return null;

            var color = widget.DeleteIconColor
                        ?? chipTheme.DeleteIconColor
                        ?? widget.IconTheme?.Color
                        ?? chipTheme.IconTheme?.Color
                        ?? defaults.DeleteIconColor
                        ?? iconTheme?.Color
                        ?? Theme.Of(context).ColorScheme.OnSurfaceVariant;
            Widget icon = new IconTheme(
                new IconThemeData(Color: color, Size: iconTheme?.Size ?? 18),
                widget.DeleteIcon);
            icon = new Opacity(_deleteProgress, icon);

            Action? delete = widget.CanDelete ? widget.OnDeleted : null;
            if (delete is not null)
            {
                icon = new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onTap: delete,
                    child: icon);
            }

            string tooltip = widget.DeleteButtonTooltipMessage
                             ?? MaterialLocalizations.Of(context).DeleteButtonTooltip;
            if (delete is not null && !string.IsNullOrEmpty(tooltip))
            {
                icon = new Tooltip(message: tooltip, child: icon);
            }

            double semanticExtent = (tapTargetSize == Plumix.Material.MaterialTapTargetSize.Padded ? 48.0 : 32.0)
                                    + density.BaseSizeAdjustment.Y;
            semanticExtent = Math.Max(0.0, semanticExtent);
            return new EnsureMinSemanticsSize(
                minSemanticSize: new Size(semanticExtent, semanticExtent),
                label: tooltip,
                enabled: delete is not null,
                onTap: delete,
                child: icon);
        }

        private static Widget? BuildAvatar(RawChip widget, IconThemeData? iconTheme)
        {
            Widget? leading = widget.Avatar;
            if (leading is not null && iconTheme is not null)
            {
                leading = new IconTheme(iconTheme, leading);
            }
            return leading;
        }

        private static ChipThemeData ResolveDefaults(BuildContext context, ThemeData theme, RawChip chip)
        {
            if (!theme.UseMaterial3)
            {
                var primary = theme.Brightness == Brightness.Light ? Colors.Black : Colors.White;
                return new ChipThemeData(
                    BackgroundColor: WithAlpha(primary, 0x1f),
                    DisabledColor: WithAlpha(primary, 0x0c),
                    SelectedColor: WithAlpha(primary, 0x3d),
                    SecondarySelectedColor: WithAlpha(theme.PrimaryColor, 0x3d),
                    DeleteIconColor: WithAlpha(primary, 0xde),
                    ShowCheckmark: true,
                    CheckmarkColor: WithAlpha(primary, 0xde),
                    LabelStyle: theme.TextTheme.BodyLarge.CopyWith(color: WithAlpha(primary, 0xde)),
                    SecondaryLabelStyle: theme.TextTheme.BodyLarge.CopyWith(
                        color: WithAlpha(theme.PrimaryColor, 0xde)),
                    Padding: new Thickness(4),
                    Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(10_000)),
                    Elevation: 0,
                    PressElevation: 8,
                    IconTheme: new IconThemeData(Color: WithAlpha(primary, 0xde), Size: 18));
            }

            bool enabled = chip.IsEnabled;
            bool selected = chip.Selected;
            bool elevated = chip.Variant == ChipVariant.Elevated;
            var baseDefaults = new ChipThemeData(
                Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(8)),
                ShowCheckmark: true,
                SurfaceTintColor: Colors.Transparent,
                Padding: new Thickness(8),
                PressElevation: 1);

            if (chip.DefaultsKind is ChipDefaultsKind.Choice or ChipDefaultsKind.Filter)
            {
                return baseDefaults with
                {
                    Elevation = elevated && enabled ? 1 : 0,
                    ShadowColor = elevated ? theme.ShadowColor : Colors.Transparent,
                    LabelStyle = theme.TextTheme.LabelLarge.CopyWith(color:
                        enabled
                            ? selected ? theme.ColorScheme.OnSecondaryContainer : theme.ColorScheme.OnSurfaceVariant
                            : theme.ColorScheme.OnSurface),
                    Color = MaterialStateProperty<Color?>.ResolveWith(states =>
                    {
                        if (states.HasFlag(MaterialState.Selected) && states.HasFlag(MaterialState.Disabled))
                        {
                            return WithOpacity(theme.ColorScheme.OnSurface, 0.12);
                        }
                        if (states.HasFlag(MaterialState.Disabled))
                        {
                            return elevated ? WithOpacity(theme.ColorScheme.OnSurface, 0.12) : null;
                        }
                        if (states.HasFlag(MaterialState.Selected))
                        {
                            return theme.ColorScheme.SecondaryContainer;
                        }
                        return elevated ? theme.ColorScheme.SurfaceContainerLow : null;
                    }),
                    CheckmarkColor = enabled
                        ? selected ? theme.ColorScheme.OnSecondaryContainer : theme.PrimaryColor
                        : theme.ColorScheme.OnSurface,
                    DeleteIconColor = enabled
                        ? selected ? theme.ColorScheme.OnSecondaryContainer : theme.ColorScheme.OnSurfaceVariant
                        : theme.ColorScheme.OnSurface,
                    Side = !elevated && !selected
                        ? new BorderSide(enabled
                            ? theme.ColorScheme.OutlineVariant
                            : WithOpacity(theme.ColorScheme.OnSurface, 0.12))
                        : new BorderSide(Colors.Transparent, 0),
                    IconTheme = new IconThemeData(
                        Color: enabled
                            ? selected ? theme.ColorScheme.OnSecondaryContainer : theme.PrimaryColor
                            : theme.ColorScheme.OnSurface,
                        Size: 18),
                };
            }

            if (chip.DefaultsKind == ChipDefaultsKind.Input)
            {
                return baseDefaults with
                {
                    Elevation = 0,
                    ShadowColor = Colors.Transparent,
                    LabelStyle = theme.TextTheme.LabelLarge.CopyWith(color:
                        enabled
                            ? selected ? theme.ColorScheme.OnSecondaryContainer : theme.ColorScheme.OnSurfaceVariant
                            : theme.ColorScheme.OnSurface),
                    Color = MaterialStateProperty<Color?>.ResolveWith(states =>
                    {
                        if (states.HasFlag(MaterialState.Selected) && states.HasFlag(MaterialState.Disabled))
                        {
                            return WithOpacity(theme.ColorScheme.OnSurface, 0.12);
                        }
                        if (states.HasFlag(MaterialState.Disabled)) return null;
                        return states.HasFlag(MaterialState.Selected)
                            ? theme.ColorScheme.SecondaryContainer
                            : null;
                    }),
                    CheckmarkColor = enabled
                        ? selected ? theme.PrimaryColor : theme.ColorScheme.OnSurfaceVariant
                        : theme.ColorScheme.OnSurface,
                    DeleteIconColor = enabled
                        ? selected ? theme.ColorScheme.OnSecondaryContainer : theme.ColorScheme.OnSurfaceVariant
                        : theme.ColorScheme.OnSurface,
                    Side = !selected
                        ? new BorderSide(enabled
                            ? theme.ColorScheme.OutlineVariant
                            : WithOpacity(theme.ColorScheme.OnSurface, 0.12))
                        : new BorderSide(Colors.Transparent, 0),
                    IconTheme = new IconThemeData(
                        Color: enabled
                            ? selected ? theme.PrimaryColor : theme.ColorScheme.OnSurfaceVariant
                            : theme.ColorScheme.OnSurface,
                        Size: 18),
                };
            }

            if (chip.DefaultsKind == ChipDefaultsKind.Action)
            {
                return baseDefaults with
                {
                    Elevation = elevated && enabled ? 1 : 0,
                    ShadowColor = elevated ? theme.ShadowColor : Colors.Transparent,
                    LabelStyle = theme.TextTheme.LabelLarge.CopyWith(color: theme.ColorScheme.OnSurface),
                    Color = MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Disabled)
                            ? elevated ? WithOpacity(theme.ColorScheme.OnSurface, 0.12) : null
                            : elevated ? theme.ColorScheme.SurfaceContainerLow : null),
                    Side = !elevated
                        ? new BorderSide(enabled
                            ? theme.ColorScheme.OutlineVariant
                            : WithOpacity(theme.ColorScheme.OnSurface, 0.12))
                        : new BorderSide(Colors.Transparent, 0),
                    IconTheme = new IconThemeData(
                        Color: enabled ? theme.PrimaryColor : theme.ColorScheme.OnSurface,
                        Size: 18),
                };
            }

            return baseDefaults with
            {
                Elevation = 0,
                ShadowColor = Colors.Transparent,
                LabelStyle = theme.TextTheme.LabelLarge.CopyWith(
                    color: enabled ? theme.ColorScheme.OnSurfaceVariant : theme.ColorScheme.OnSurface),
                Side = new BorderSide(enabled
                    ? theme.ColorScheme.OutlineVariant
                    : WithOpacity(theme.ColorScheme.OnSurface, 0.12)),
                IconTheme = new IconThemeData(
                    Color: enabled ? theme.PrimaryColor : theme.ColorScheme.OnSurface,
                    Size: 18),
            };
        }

        private Color? ResolveBackground(
            MaterialState states,
            RawChip widget,
            ChipThemeData chipTheme,
            ChipThemeData defaults)
        {
            Color? resolved;
            if (widget.Color is not null && (resolved = widget.Color.Resolve(states)).HasValue)
            {
                return resolved;
            }
            if (chipTheme.Color is not null && (resolved = chipTheme.Color.Resolve(states)).HasValue)
            {
                return resolved;
            }

            var selectedColor = widget.SelectedColor
                                ?? chipTheme.SelectedColor
                                ?? (widget.DefaultsKind == ChipDefaultsKind.Choice
                                    ? defaults.SecondarySelectedColor
                                    : defaults.SelectedColor);
            var normalColor = widget.BackgroundColor ?? chipTheme.BackgroundColor ?? defaults.BackgroundColor;
            var disabledColor = widget.DisabledColor ?? chipTheme.DisabledColor ?? defaults.DisabledColor;
            var stateDefault = defaults.Color?.Resolve(states);

            var target = states.HasFlag(MaterialState.Selected)
                ? selectedColor ?? stateDefault
                : states.HasFlag(MaterialState.Disabled)
                    ? disabledColor ?? stateDefault
                    : normalColor ?? stateDefault;
            var unselected = states.HasFlag(MaterialState.Disabled)
                ? disabledColor ?? defaults.Color?.Resolve(MaterialState.Disabled)
                : normalColor ?? defaults.Color?.Resolve(MaterialState.None);

            if (selectedColor.HasValue
                && (_selectionController?.IsAnimating == true || _selectionProgress is > 0 and < 1))
            {
                var from = unselected ?? Avalonia.Media.Color.FromArgb(
                    0,
                    selectedColor.Value.R,
                    selectedColor.Value.G,
                    selectedColor.Value.B);
                return new ColorTween().Evaluate(_selectionProgress, from, selectedColor.Value);
            }

            return target ?? Colors.Transparent;
        }

        private static Color ResolveLabelColor(
            MaterialState states,
            RawChip widget,
            ChipThemeData chipTheme,
            ChipThemeData defaults,
            TextStyle labelStyle,
            ThemeData theme)
        {
            Color resolved;
            if (widget.LabelStyle?.Color is { } widgetColor)
            {
                resolved = widgetColor;
            }
            else if (states.HasFlag(MaterialState.Selected) && chipTheme.SecondaryLabelStyle?.Color is { } secondary)
            {
                resolved = secondary;
            }
            else
            {
                resolved = labelStyle.Color
                           ?? defaults.LabelStyle?.Color
                           ?? (states.HasFlag(MaterialState.Disabled)
                               ? theme.ColorScheme.OnSurface
                               : theme.ColorScheme.OnSurfaceVariant);
            }

            return resolved;
        }

        private static BorderSide? ResolveSide(
            MaterialState states,
            RawChip widget,
            ChipThemeData chipTheme,
            ChipThemeData defaults)
        {
            BorderSide? widgetSide = widget.Side?.Resolve(states);
            if (widgetSide.HasValue) return widgetSide;
            if (chipTheme.Side.HasValue) return chipTheme.Side;
            ShapeBorder shape = widget.Shape?.Resolve(states)
                                ?? chipTheme.Shape
                                ?? defaults.Shape
                                ?? new StadiumBorder();
            if (ShapeBorderGeometry.SideOrNull(shape) is { } shapeSide && shapeSide.Width > 0.0)
            {
                return shapeSide;
            }
            if (widget.DefaultsKind is ChipDefaultsKind.Choice or ChipDefaultsKind.Filter or ChipDefaultsKind.Input
                && widget.Variant == ChipVariant.Flat
                && states.HasFlag(MaterialState.Selected))
            {
                return new BorderSide(Colors.Transparent, 0);
            }
            return defaults.Side ?? ShapeBorderGeometry.SideOrNull(shape);
        }

        private static ShapeBorder ResolveShape(
            MaterialState states,
            RawChip widget,
            ChipThemeData chipTheme,
            ChipThemeData defaults)
        {
            ShapeBorder shape = widget.Shape?.Resolve(states)
                                ?? chipTheme.Shape
                                ?? defaults.Shape
                                ?? new StadiumBorder();
            BorderSide? resolvedSide = widget.Side?.Resolve(states) ?? chipTheme.Side;
            if (resolvedSide.HasValue)
            {
                return shape is OutlinedBorder outlinedShape
                    ? outlinedShape.CopyWith(resolvedSide)
                    : shape;
            }

            if (ShapeBorderGeometry.SideOrNull(shape) is { } shapeSide && shapeSide.Width > 0.0)
            {
                return shape;
            }

            return shape is OutlinedBorder outlinedDefault ? outlinedDefault.CopyWith(defaults.Side) : shape;
        }

        private static TextStyle MergeTextStyles(TextStyle baseStyle, TextStyle? overrideStyle)
        {
            if (overrideStyle is null) return baseStyle;
            return new TextStyle(
                FontFamily: overrideStyle.FontFamily ?? baseStyle.FontFamily,
                FontSize: overrideStyle.FontSize ?? baseStyle.FontSize,
                Color: overrideStyle.Color ?? baseStyle.Color,
                FontWeight: overrideStyle.FontWeight ?? baseStyle.FontWeight,
                FontStyle: overrideStyle.FontStyle ?? baseStyle.FontStyle,
                Height: overrideStyle.Height ?? baseStyle.Height,
                LetterSpacing: overrideStyle.LetterSpacing ?? baseStyle.LetterSpacing);
        }

        private void HandleSelectionTick()
        {
            SetState(UpdateSelectionProgress);
        }

        private void HandleAvatarDrawerTick()
        {
            SetState(UpdateAvatarDrawerProgress);
        }

        private void HandleDeleteTick()
        {
            SetState(UpdateDeleteProgress);
        }

        private void HandleEnableTick()
        {
            SetState(UpdateEnableProgress);
        }

        private AnimationController CreateController(AnimationStyle? style, TimeSpan defaultDuration)
        {
            return new AnimationController(duration: style?.Duration ?? defaultDuration, vsync: this)
            {
                ReverseDuration = style?.ReverseDuration,
                Curve = Curves.Linear,
            };
        }

        private void UpdateAnimationProgress()
        {
            UpdateSelectionProgress();
            UpdateAvatarDrawerProgress();
            UpdateDeleteProgress();
            UpdateEnableProgress();
        }

        private void UpdateSelectionProgress()
        {
            AnimationStyle? style = CurrentWidget.ChipAnimationStyle?.SelectAnimation;
            _selectionProgress = TransformController(
                _selectionController!,
                style,
                Curves.FastOutSlowIn,
                Curves.FastOutSlowIn);
            Curve checkmarkCurve = _selectionController!.Status == AnimationStatus.Reverse
                ? Curves.Interval(1.0 - (50.0 / 195.0), 1.0, Curves.FastOutSlowIn)
                : Curves.Interval(1.0 - (150.0 / 195.0), 1.0, Curves.FastOutSlowIn);
            _checkmarkProgress = checkmarkCurve(_selectionController.Value);
        }

        private void UpdateAvatarDrawerProgress()
        {
            AnimationStyle? style = CurrentWidget.ChipAnimationStyle?.AvatarDrawerAnimation;
            _avatarDrawerProgress = TransformController(
                _avatarDrawerController!,
                style,
                Curves.FastOutSlowIn,
                Curves.Interval(1.0 - (100.0 / 195.0), 1.0, Curves.FastOutSlowIn));
        }

        private void UpdateDeleteProgress()
        {
            AnimationStyle? style = CurrentWidget.ChipAnimationStyle?.DeleteDrawerAnimation;
            _deleteProgress = TransformController(
                _deleteController!,
                style,
                Curves.FastOutSlowIn,
                Curves.FastOutSlowIn);
        }

        private void UpdateEnableProgress()
        {
            AnimationStyle? style = CurrentWidget.ChipAnimationStyle?.EnableAnimation;
            _enableProgress = TransformController(
                _enableController!,
                style,
                Curves.FastOutSlowIn,
                Curves.FastOutSlowIn);
        }

        private static double TransformController(
            AnimationController controller,
            AnimationStyle? style,
            Curve defaultCurve,
            Curve defaultReverseCurve)
        {
            bool reversing = controller.Status == AnimationStatus.Reverse;
            Curve curve = reversing
                ? style?.ReverseCurve ?? style?.Curve ?? defaultReverseCurve
                : style?.Curve ?? defaultCurve;
            return curve(Math.Clamp(controller.Value, 0.0, 1.0));
        }

        private static Color ResolveDefaultCheckmarkColor(Brightness brightness, bool hasAvatar)
        {
            if (brightness == Brightness.Light)
            {
                return hasAvatar ? Colors.White : WithAlpha(Colors.Black, 0xde);
            }

            return hasAvatar ? Colors.Black : WithAlpha(Colors.White, 0xde);
        }

        private void DisposeSelectionController()
        {
            if (_selectionController is null) return;
            _selectionController.Changed -= HandleSelectionTick;
            _selectionController.Dispose();
            _selectionController = null;
        }

        private void DisposeAvatarDrawerController()
        {
            if (_avatarDrawerController is null) return;
            _avatarDrawerController.Changed -= HandleAvatarDrawerTick;
            _avatarDrawerController.Dispose();
            _avatarDrawerController = null;
        }

        private void DisposeDeleteController()
        {
            if (_deleteController is null) return;
            _deleteController.Changed -= HandleDeleteTick;
            _deleteController.Dispose();
            _deleteController = null;
        }

        private void DisposeEnableController()
        {
            if (_enableController is null) return;
            _enableController.Changed -= HandleEnableTick;
            _enableController.Dispose();
            _enableController = null;
        }

        private static Color WithOpacity(Color color, double opacity)
        {
            return Avalonia.Media.Color.FromArgb(
                (byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255),
                color.R,
                color.G,
                color.B);
        }

        private static Color WithAlpha(Color color, byte alpha)
        {
            return Avalonia.Media.Color.FromArgb(alpha, color.R, color.G, color.B);
        }
    }
}

/// <summary>Dart parity: `_ChipRedirectingHitDetectionWidget`.</summary>
internal sealed class ChipRedirectingHitDetectionWidget : SingleChildRenderObjectWidget
{
    public ChipRedirectingHitDetectionWidget(BoxConstraints constraints, Widget child, Key? key = null)
        : base(child, key)
    {
        Constraints = constraints;
    }

    public BoxConstraints Constraints { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderChipRedirectingHitDetection(Constraints);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderChipRedirectingHitDetection)renderObject).AdditionalConstraints = Constraints;
    }
}

/// <summary>Dart parity: `_RenderChipRedirectingHitDetection`.</summary>
internal sealed class RenderChipRedirectingHitDetection : RenderConstrainedBox
{
    public RenderChipRedirectingHitDetection(BoxConstraints additionalConstraints)
        : base(additionalConstraints)
    {
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (position.X < 0 || position.Y < 0 || position.X >= Size.Width || position.Y >= Size.Height)
        {
            return false;
        }

        // Only redirects hit detection which occurs above and below the render object. In order to
        // make this assumption true, the minimum width constraint is dropped, since any reasonable
        // chip would be at least that wide.
        var offset = new Point(position.X, Size.Height / 2.0);
        return result.AddWithRawTransform(
            MatrixUtils.ForceToPoint(offset),
            position,
            (BoxHitTestResult nested, Point _) => Child!.HitTest(nested, offset));
    }
}
