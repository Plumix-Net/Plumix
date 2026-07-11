using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/chip.dart

public sealed class Chip : StatelessWidget
{
    public Chip(
        Widget label,
        Widget? avatar = null,
        TextStyle? labelStyle = null,
        Thickness? labelPadding = null,
        Widget? deleteIcon = null,
        Action? onDeleted = null,
        Color? deleteIconColor = null,
        string? deleteButtonTooltipMessage = null,
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
        BoxConstraints? avatarBoxConstraints = null,
        BoxConstraints? deleteIconBoxConstraints = null,
        ChipAnimationStyle? chipAnimationStyle = null,
        MouseCursor? mouseCursor = null,
        Key? key = null) : base(key)
    {
        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(elevation));
        }

        Label = label ?? throw new ArgumentNullException(nameof(label));
        Avatar = avatar;
        LabelStyle = labelStyle;
        LabelPadding = labelPadding;
        DeleteIcon = deleteIcon;
        OnDeleted = onDeleted;
        DeleteIconColor = deleteIconColor;
        DeleteButtonTooltipMessage = deleteButtonTooltipMessage;
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
        AvatarBoxConstraints = avatarBoxConstraints;
        DeleteIconBoxConstraints = deleteIconBoxConstraints;
        ChipAnimationStyle = chipAnimationStyle;
        MouseCursor = mouseCursor;
    }

    public Widget? Avatar { get; }
    public Widget Label { get; }
    public TextStyle? LabelStyle { get; }
    public Thickness? LabelPadding { get; }
    public Widget? DeleteIcon { get; }
    public Action? OnDeleted { get; }
    public Color? DeleteIconColor { get; }
    public string? DeleteButtonTooltipMessage { get; }
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
    public IconThemeData? IconTheme { get; }
    public BoxConstraints? AvatarBoxConstraints { get; }
    public BoxConstraints? DeleteIconBoxConstraints { get; }
    public ChipAnimationStyle? ChipAnimationStyle { get; }
    public MouseCursor? MouseCursor { get; }

    public override Widget Build(BuildContext context)
    {
        return new RawChip(
            avatar: Avatar,
            label: Label,
            labelStyle: LabelStyle,
            labelPadding: LabelPadding,
            deleteIcon: DeleteIcon,
            onDeleted: OnDeleted,
            deleteIconColor: DeleteIconColor,
            deleteButtonTooltipMessage: DeleteButtonTooltipMessage,
            tapEnabled: false,
            side: Side,
            shape: Shape,
            clipBehavior: ClipBehavior,
            focusNode: FocusNode,
            autofocus: Autofocus,
            color: Color,
            backgroundColor: BackgroundColor,
            padding: Padding,
            visualDensity: VisualDensity,
            materialTapTargetSize: MaterialTapTargetSize,
            elevation: Elevation,
            shadowColor: ShadowColor,
            surfaceTintColor: SurfaceTintColor,
            iconTheme: IconTheme,
            avatarBoxConstraints: AvatarBoxConstraints,
            deleteIconBoxConstraints: DeleteIconBoxConstraints,
            chipAnimationStyle: ChipAnimationStyle,
            mouseCursor: MouseCursor);
    }
}
