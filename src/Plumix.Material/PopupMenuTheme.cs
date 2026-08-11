using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/popup_menu_theme.dart

public enum PopupMenuPosition
{
    Over,
    Under,
}

public sealed partial record PopupMenuThemeData
{
    public PopupMenuThemeData(
        Color? Color = null,
        ShapeBorder? Shape = null,
        EdgeInsetsGeometry? MenuPadding = null,
        double? Elevation = null,
        Color? ShadowColor = null,
        Color? SurfaceTintColor = null,
        TextStyle? TextStyle = null,
        MaterialStateProperty<TextStyle?>? LabelTextStyle = null,
        bool? EnableFeedback = null,
        MaterialStateProperty<MouseCursor?>? MouseCursor = null,
        PopupMenuPosition? Position = null,
        Color? IconColor = null,
        double? IconSize = null)
    {
        this.Color = Color;
        this.Shape = Shape;
        this.MenuPadding = MenuPadding;
        this.Elevation = Elevation;
        this.ShadowColor = ShadowColor;
        this.SurfaceTintColor = SurfaceTintColor;
        this.TextStyle = TextStyle;
        this.LabelTextStyle = LabelTextStyle;
        this.EnableFeedback = EnableFeedback;
        this.MouseCursor = MouseCursor;
        this.Position = Position;
        this.IconColor = IconColor;
        this.IconSize = IconSize;
    }

    public Color? Color { get; init; }
    public ShapeBorder? Shape { get; init; }
    public EdgeInsetsGeometry? MenuPadding { get; init; }
    public double? Elevation { get; init; }
    public Color? ShadowColor { get; init; }
    public Color? SurfaceTintColor { get; init; }
    public TextStyle? TextStyle { get; init; }
    public MaterialStateProperty<TextStyle?>? LabelTextStyle { get; init; }
    public bool? EnableFeedback { get; init; }
    public MaterialStateProperty<MouseCursor?>? MouseCursor { get; init; }
    public PopupMenuPosition? Position { get; init; }
    public Color? IconColor { get; init; }
    public double? IconSize { get; init; }

    public PopupMenuThemeData CopyWith(
        Color? color = null,
        ShapeBorder? shape = null,
        EdgeInsetsGeometry? menuPadding = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        TextStyle? textStyle = null,
        MaterialStateProperty<TextStyle?>? labelTextStyle = null,
        bool? enableFeedback = null,
        MaterialStateProperty<MouseCursor?>? mouseCursor = null,
        PopupMenuPosition? position = null,
        Color? iconColor = null,
        double? iconSize = null)
    {
        return new PopupMenuThemeData(
            Color: color ?? Color,
            Shape: shape ?? Shape,
            MenuPadding: menuPadding ?? MenuPadding,
            Elevation: elevation ?? Elevation,
            ShadowColor: shadowColor ?? ShadowColor,
            SurfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            TextStyle: textStyle ?? TextStyle,
            LabelTextStyle: labelTextStyle ?? LabelTextStyle,
            EnableFeedback: enableFeedback ?? EnableFeedback,
            MouseCursor: mouseCursor ?? MouseCursor,
            Position: position ?? Position,
            IconColor: iconColor ?? IconColor,
            IconSize: iconSize ?? IconSize);
    }
}

public sealed class PopupMenuTheme : InheritedTheme
{
    public PopupMenuTheme(PopupMenuThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public PopupMenuThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child) => new PopupMenuTheme(Data, child);

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((PopupMenuTheme)oldWidget).Data, Data);

    public static PopupMenuThemeData Of(BuildContext context) =>
        context.DependOnInherited<PopupMenuTheme>()?.Data ?? Theme.Of(context).PopupMenuTheme;
}
