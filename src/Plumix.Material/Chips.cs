using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// flutter/packages/flutter/lib/src/material/chip.dart
// flutter/packages/flutter/lib/src/material/action_chip.dart
// flutter/packages/flutter/lib/src/material/choice_chip.dart

public sealed record ChipAnimationStyle(
    TimeSpan? EnableAnimation = null,
    TimeSpan? SelectAnimation = null,
    TimeSpan? AvatarDrawerAnimation = null,
    TimeSpan? DeleteDrawerAnimation = null);

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
        BorderSide? side = null,
        ShapeBorder? shape = null,
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
        BorderSide? side,
        ShapeBorder? shape,
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
        BorderSide? side = null,
        ShapeBorder? shape = null,
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
    public BorderSide? Side { get; }
    public ShapeBorder? Shape { get; }
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
        BorderSide? side = null,
        ShapeBorder? shape = null,
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
        BorderSide? side,
        ShapeBorder? shape,
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
        AvatarBorder = avatarBorder ?? ShapeBorder.RoundedRectangle(10_000);
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
        BorderSide? side = null,
        ShapeBorder? shape = null,
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
    public BorderSide? Side { get; }
    public ShapeBorder? Shape { get; }
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
        BorderSide? side = null,
        ShapeBorder? shape = null,
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
        BorderSide? side,
        ShapeBorder? shape,
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
        AvatarBorder = avatarBorder ?? ShapeBorder.RoundedRectangle(10_000);
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
    public BorderSide? Side { get; }
    public ShapeBorder? Shape { get; }
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
        private AnimationController? _deleteController;
        private double _deleteProgress;

        private RawChip CurrentWidget => (RawChip)StateWidget;

        public override void InitState()
        {
            _selectionProgress = CurrentWidget.Selected ? 1 : 0;
            _selectionController = new AnimationController(
                CurrentWidget.ChipAnimationStyle?.SelectAnimation ?? TimeSpan.FromMilliseconds(195))
            {
                Curve = Curves.EaseInOut,
            };
            _selectionController.Changed += HandleSelectionTick;
            _deleteProgress = CurrentWidget.OnDeleted is null ? 0 : 1;
            _deleteController = new AnimationController(
                CurrentWidget.ChipAnimationStyle?.DeleteDrawerAnimation ?? TimeSpan.FromMilliseconds(150))
            {
                Curve = Curves.EaseInOut,
            };
            _deleteController.Changed += HandleDeleteTick;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldChip = (RawChip)oldWidget;
            if (oldChip.ChipAnimationStyle?.SelectAnimation != CurrentWidget.ChipAnimationStyle?.SelectAnimation)
            {
                DisposeController();
                _selectionController = new AnimationController(
                    CurrentWidget.ChipAnimationStyle?.SelectAnimation ?? TimeSpan.FromMilliseconds(195))
                {
                    Curve = Curves.EaseInOut,
                };
                _selectionController.Changed += HandleSelectionTick;
            }

            if (oldChip.Selected != CurrentWidget.Selected)
            {
                if (CurrentWidget.Selected)
                {
                    _selectionController!.Forward(0);
                }
                else
                {
                    _selectionController!.Reverse(1);
                }
            }

            if (oldChip.ChipAnimationStyle?.DeleteDrawerAnimation
                != CurrentWidget.ChipAnimationStyle?.DeleteDrawerAnimation)
            {
                DisposeDeleteController();
                _deleteProgress = CurrentWidget.OnDeleted is null ? 0 : 1;
                _deleteController = new AnimationController(
                    CurrentWidget.ChipAnimationStyle?.DeleteDrawerAnimation ?? TimeSpan.FromMilliseconds(150))
                {
                    Curve = Curves.EaseInOut,
                };
                _deleteController.Changed += HandleDeleteTick;
            }
            else if ((oldChip.OnDeleted is null) != (CurrentWidget.OnDeleted is null))
            {
                if (CurrentWidget.OnDeleted is not null)
                {
                    _deleteController!.Forward(0);
                }
                else
                {
                    _deleteController!.Reverse(1);
                }
            }
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var theme = Theme.Of(context);
            var chipTheme = ChipTheme.Of(context);
            var defaults = ResolveDefaults(context, theme, widget);
            var shape = widget.Shape ?? chipTheme.Shape ?? defaults.Shape ?? ShapeBorder.RoundedRectangle(10_000);
            var side = widget.Side ?? chipTheme.Side ?? defaults.Side ?? shape.Side;
            var padding = widget.Padding ?? chipTheme.Padding ?? defaults.Padding ?? new Thickness(4);
            var baseLabelStyle = chipTheme.LabelStyle ?? defaults.LabelStyle ?? theme.TextTheme.BodyLarge;
            var labelStyle = MergeTextStyles(baseLabelStyle, widget.LabelStyle);
            double textScale = MaterialButtonCore.ResolvePaddingFontSizeMultiplier(
                context,
                labelStyle.FontSize ?? 14);
            var defaultLabelPadding = MaterialButtonCore.ScalePadding(
                new Thickness(8, 0),
                new Thickness(4, 0),
                new Thickness(4, 0),
                textScale);
            var labelPadding = widget.LabelPadding ?? chipTheme.LabelPadding ?? defaults.LabelPadding ?? defaultLabelPadding;
            var density = widget.VisualDensity ?? theme.VisualDensity;
            var tapTargetSize = widget.MaterialTapTargetSize ?? theme.MaterialTapTargetSize;
            var effectiveIconTheme = widget.IconTheme ?? chipTheme.IconTheme ?? defaults.IconTheme;

            var style = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    ResolveLabelColor(states, widget, chipTheme, defaults, labelStyle, theme)),
                BackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    ResolveBackground(states, widget, chipTheme, defaults)),
                ShadowColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Selected)
                        ? widget.SelectedShadowColor ?? chipTheme.SelectedShadowColor ?? defaults.SelectedShadowColor
                        : widget.ShadowColor ?? chipTheme.ShadowColor ?? defaults.ShadowColor ?? theme.ShadowColor),
                SurfaceTintColor: MaterialStateProperty<Color?>.All(
                    widget.SurfaceTintColor ?? chipTheme.SurfaceTintColor ?? defaults.SurfaceTintColor),
                OverlayColor: MaterialButtonCore.CreateDefaultOverlayResolver(theme.OnSurfaceColor),
                SplashColor: MaterialButtonCore.CreateDefaultSplashResolver(theme.OnSurfaceColor),
                Elevation: MaterialStateProperty<double?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Pressed)
                        ? widget.PressElevation ?? chipTheme.PressElevation ?? defaults.PressElevation ?? 0
                        : widget.Elevation ?? chipTheme.Elevation ?? defaults.Elevation ?? 0),
                IconColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                {
                    var iconColor = effectiveIconTheme?.Color
                                    ?? ResolveLabelColor(states, widget, chipTheme, defaults, labelStyle, theme);
                    return states.HasFlag(MaterialState.Disabled)
                        ? MaterialButtonCore.ApplyOpacity(iconColor, 0.38)
                        : iconColor;
                }),
                IconSize: MaterialStateProperty<double?>.All(effectiveIconTheme?.Size ?? 18),
                Side: MaterialStateProperty<BorderSide?>.ResolveWith(states =>
                    ResolveSide(states, widget, chipTheme, defaults, shape, theme)),
                Padding: MaterialStateProperty<Thickness?>.All(padding),
                Shape: MaterialStateProperty<BorderRadius?>.All(shape.BorderRadius),
                MinimumSize: MaterialStateProperty<Size?>.All(new Size(
                    0,
                    Math.Max(0, 32 + density.BaseSizeAdjustment.Y))),
                TapTargetSize: tapTargetSize,
                TextStyle: MaterialStateProperty<TextStyle?>.All(labelStyle));

            Widget label = new Padding(labelPadding, widget.Label);
            var leading = BuildLeading(widget, chipTheme, defaults, effectiveIconTheme);
            var delete = BuildDelete(
                context,
                widget,
                chipTheme,
                defaults,
                effectiveIconTheme);
            var contentChildren = new List<Widget>(3);
            if (leading is not null) contentChildren.Add(leading);
            contentChildren.Add(label);
            if (delete is not null) contentChildren.Add(delete);
            Widget content = contentChildren.Count == 1
                ? label
                : new Row(
                    mainAxisSize: MainAxisSize.Min,
                    children: contentChildren);

            Action? onTap = widget.CanTapBody
                ? () =>
                {
                    widget.OnSelected?.Invoke(!widget.Selected);
                    widget.OnPressed?.Invoke();
                }
                : null;
            Widget result = new MaterialButtonCore(
                child: content,
                onPressed: onTap,
                style: style,
                focusNode: widget.FocusNode,
                autofocus: widget.Autofocus,
                isSelected: widget.Selected,
                includeSemanticSelected: true,
                isSemanticButton: widget.TapEnabled,
                isSemanticChecked: widget.DefaultsKind is ChipDefaultsKind.Choice or ChipDefaultsKind.Filter
                                   || widget.OnSelected is not null
                    ? widget.Selected
                    : null,
                mouseCursor: widget.MouseCursor,
                clipBehavior: widget.ClipBehavior,
                enabled: widget.IsEnabled,
                semanticEnabled: widget.CanTapBody,
                tapTargetMinimumSize: tapTargetSize == Plumix.Material.MaterialTapTargetSize.Padded
                    ? new Size(
                        Math.Max(0, 48 + density.BaseSizeAdjustment.X),
                        Math.Max(0, 48 + density.BaseSizeAdjustment.Y))
                    : new Size(0, 0));

            if (!string.IsNullOrEmpty(widget.Tooltip) && widget.CanTapBody)
            {
                result = new Tooltip(message: widget.Tooltip!, child: result);
            }

            return result;
        }

        public override void Dispose()
        {
            DisposeController();
            DisposeDeleteController();
        }

        private Widget? BuildDelete(
            BuildContext context,
            RawChip widget,
            ChipThemeData chipTheme,
            ChipThemeData defaults,
            IconThemeData? iconTheme)
        {
            if (_deleteProgress <= 0) return null;

            var color = widget.DeleteIconColor
                        ?? chipTheme.DeleteIconColor
                        ?? widget.IconTheme?.Color
                        ?? chipTheme.IconTheme?.Color
                        ?? defaults.DeleteIconColor
                        ?? iconTheme?.Color
                        ?? Theme.Of(context).OnSurfaceVariantColor;
            if (!widget.IsEnabled)
            {
                color = MaterialButtonCore.ApplyOpacity(color, 0.38);
            }

            Widget icon = new IconTheme(
                new IconThemeData(Color: color, Size: iconTheme?.Size ?? 18),
                widget.DeleteIcon);
            icon = widget.DeleteIconBoxConstraints is { } constraints
                ? new ConstrainedBox(constraints, icon)
                : new SizedBox(width: 24, height: 24, child: icon);
            icon = new Align(
                alignment: Alignment.Center,
                widthFactor: _deleteProgress,
                heightFactor: 1,
                child: icon);
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

            return new Semantics(
                label: tooltip,
                flags: delete is null
                    ? SemanticsFlags.IsButton
                    : SemanticsFlags.IsButton | SemanticsFlags.IsEnabled,
                onTap: delete,
                child: icon);
        }

        private Widget? BuildLeading(
            RawChip widget,
            ChipThemeData chipTheme,
            ChipThemeData defaults,
            IconThemeData? iconTheme)
        {
            bool showCheckmark = widget.ShowCheckmark ?? chipTheme.ShowCheckmark ?? defaults.ShowCheckmark ?? false;
            bool checkmarkVisible = showCheckmark && _selectionProgress > 0;
            var checkmarkColor = widget.CheckmarkColor ?? chipTheme.CheckmarkColor ?? defaults.CheckmarkColor;
            if (!widget.IsEnabled && checkmarkColor.HasValue)
            {
                checkmarkColor = MaterialButtonCore.ApplyOpacity(checkmarkColor.Value, 0.38);
            }
            Widget? check = checkmarkVisible
                ? new Opacity(
                    opacity: _selectionProgress,
                    child: new IconTheme(
                        data: new IconThemeData(Color: checkmarkColor, Size: iconTheme?.Size ?? 18),
                        child: new Icon(Icons.Check)))
                : null;

            Widget? leading = widget.Avatar;
            if (leading is not null && iconTheme is not null)
            {
                leading = new IconTheme(iconTheme, leading);
            }

            if (leading is not null && check is not null)
            {
                leading = new Stack(
                    alignment: Alignment.Center,
                    fit: StackFit.Loose,
                    children: [leading, check]);
            }
            else
            {
                leading ??= check;
            }

            if (leading is not null && widget.AvatarBoxConstraints is { } constraints)
            {
                leading = new ConstrainedBox(constraints, leading);
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
                    ShowCheckmark: false,
                    CheckmarkColor: WithAlpha(primary, 0xde),
                    LabelStyle: theme.TextTheme.BodyLarge.CopyWith(color: WithAlpha(primary, 0xde)),
                    SecondaryLabelStyle: theme.TextTheme.BodyLarge.CopyWith(color: theme.PrimaryColor),
                    Padding: new Thickness(4),
                    Shape: ShapeBorder.RoundedRectangle(10_000),
                    Elevation: 0,
                    PressElevation: 8,
                    IconTheme: new IconThemeData(Color: WithAlpha(primary, 0xde), Size: 18));
            }

            bool enabled = chip.IsEnabled;
            bool selected = chip.Selected;
            bool elevated = chip.Variant == ChipVariant.Elevated;
            var baseDefaults = new ChipThemeData(
                Shape: ShapeBorder.RoundedRectangle(8),
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
                            ? selected ? theme.OnSecondaryContainerColor : theme.OnSurfaceVariantColor
                            : theme.OnSurfaceColor),
                    Color = MaterialStateProperty<Color?>.ResolveWith(states =>
                    {
                        if (states.HasFlag(MaterialState.Selected) && states.HasFlag(MaterialState.Disabled))
                        {
                            return WithOpacity(theme.OnSurfaceColor, 0.12);
                        }
                        if (states.HasFlag(MaterialState.Disabled))
                        {
                            return elevated ? WithOpacity(theme.OnSurfaceColor, 0.12) : null;
                        }
                        if (states.HasFlag(MaterialState.Selected))
                        {
                            return theme.SecondaryContainerColor;
                        }
                        return elevated ? theme.SurfaceContainerLowColor : null;
                    }),
                    CheckmarkColor = enabled
                        ? selected ? theme.OnSecondaryContainerColor : theme.PrimaryColor
                        : theme.OnSurfaceColor,
                    DeleteIconColor = enabled
                        ? selected ? theme.OnSecondaryContainerColor : theme.OnSurfaceVariantColor
                        : theme.OnSurfaceColor,
                    Side = !elevated && !selected
                        ? new BorderSide(enabled
                            ? theme.OutlineVariantColor
                            : WithOpacity(theme.OnSurfaceColor, 0.12))
                        : new BorderSide(Colors.Transparent, 0),
                    IconTheme = new IconThemeData(
                        Color: enabled
                            ? selected ? theme.OnSecondaryContainerColor : theme.PrimaryColor
                            : theme.OnSurfaceColor,
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
                            ? selected ? theme.OnSecondaryContainerColor : theme.OnSurfaceVariantColor
                            : theme.OnSurfaceColor),
                    Color = MaterialStateProperty<Color?>.ResolveWith(states =>
                    {
                        if (states.HasFlag(MaterialState.Selected) && states.HasFlag(MaterialState.Disabled))
                        {
                            return WithOpacity(theme.OnSurfaceColor, 0.12);
                        }
                        if (states.HasFlag(MaterialState.Disabled)) return null;
                        return states.HasFlag(MaterialState.Selected)
                            ? theme.SecondaryContainerColor
                            : null;
                    }),
                    CheckmarkColor = enabled
                        ? selected ? theme.PrimaryColor : theme.OnSurfaceVariantColor
                        : theme.OnSurfaceColor,
                    DeleteIconColor = enabled
                        ? selected ? theme.OnSecondaryContainerColor : theme.OnSurfaceVariantColor
                        : theme.OnSurfaceColor,
                    Side = !selected
                        ? new BorderSide(enabled
                            ? theme.OutlineVariantColor
                            : WithOpacity(theme.OnSurfaceColor, 0.12))
                        : new BorderSide(Colors.Transparent, 0),
                    IconTheme = new IconThemeData(
                        Color: enabled
                            ? selected ? theme.PrimaryColor : theme.OnSurfaceVariantColor
                            : theme.OnSurfaceColor,
                        Size: 18),
                };
            }

            if (chip.DefaultsKind == ChipDefaultsKind.Action)
            {
                return baseDefaults with
                {
                    Elevation = elevated && enabled ? 1 : 0,
                    ShadowColor = elevated ? theme.ShadowColor : Colors.Transparent,
                    LabelStyle = theme.TextTheme.LabelLarge.CopyWith(color: theme.OnSurfaceColor),
                    Color = MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Disabled)
                            ? elevated ? WithOpacity(theme.OnSurfaceColor, 0.12) : null
                            : elevated ? theme.SurfaceContainerLowColor : null),
                    Side = !elevated
                        ? new BorderSide(enabled
                            ? theme.OutlineVariantColor
                            : WithOpacity(theme.OnSurfaceColor, 0.12))
                        : new BorderSide(Colors.Transparent, 0),
                    IconTheme = new IconThemeData(
                        Color: enabled ? theme.PrimaryColor : theme.OnSurfaceColor,
                        Size: 18),
                };
            }

            return baseDefaults with
            {
                Elevation = 0,
                ShadowColor = Colors.Transparent,
                LabelStyle = theme.TextTheme.LabelLarge.CopyWith(
                    color: enabled ? theme.OnSurfaceVariantColor : theme.OnSurfaceColor),
                Side = new BorderSide(enabled
                    ? theme.OutlineVariantColor
                    : WithOpacity(theme.OnSurfaceColor, 0.12)),
                IconTheme = new IconThemeData(
                    Color: enabled ? theme.PrimaryColor : theme.OnSurfaceColor,
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
                           ?? (states.HasFlag(MaterialState.Disabled) ? theme.OnSurfaceColor : theme.OnSurfaceVariantColor);
            }

            return states.HasFlag(MaterialState.Disabled)
                ? MaterialButtonCore.ApplyOpacity(resolved, 0.38)
                : resolved;
        }

        private static BorderSide? ResolveSide(
            MaterialState states,
            RawChip widget,
            ChipThemeData chipTheme,
            ChipThemeData defaults,
            ShapeBorder shape,
            ThemeData theme)
        {
            if (widget.Side.HasValue) return widget.Side;
            if (chipTheme.Side.HasValue) return chipTheme.Side;
            if (widget.DefaultsKind is ChipDefaultsKind.Choice or ChipDefaultsKind.Filter or ChipDefaultsKind.Input
                && widget.Variant == ChipVariant.Flat
                && states.HasFlag(MaterialState.Selected))
            {
                return new BorderSide(Colors.Transparent, 0);
            }
            return defaults.Side ?? shape.Side;
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
            SetState(() => _selectionProgress = _selectionController!.Evaluate());
        }

        private void HandleDeleteTick()
        {
            SetState(() => _deleteProgress = _deleteController!.Evaluate());
        }

        private void DisposeController()
        {
            if (_selectionController is null) return;
            _selectionController.Changed -= HandleSelectionTick;
            _selectionController.Dispose();
            _selectionController = null;
        }

        private void DisposeDeleteController()
        {
            if (_deleteController is null) return;
            _deleteController.Changed -= HandleDeleteTick;
            _deleteController.Dispose();
            _deleteController = null;
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
