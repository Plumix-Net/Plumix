using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources: flutter/packages/flutter/lib/src/material/dropdown_menu_theme.dart;
// flutter/packages/flutter/lib/src/material/menu_style.dart

public sealed partial record MenuStyle(
    MaterialStateProperty<Color?>? BackgroundColor = null,
    MaterialStateProperty<Color?>? ShadowColor = null,
    MaterialStateProperty<Color?>? SurfaceTintColor = null,
    MaterialStateProperty<double?>? Elevation = null,
    MaterialStateProperty<Thickness?>? Padding = null,
    MaterialStateProperty<Size?>? MinimumSize = null,
    MaterialStateProperty<Size?>? FixedSize = null,
    MaterialStateProperty<Size?>? MaximumSize = null,
    MaterialStateProperty<BorderSide?>? Side = null,
    MaterialStateProperty<ShapeBorder?>? Shape = null,
    MaterialStateProperty<MouseCursor?>? MouseCursor = null,
    Alignment? Alignment = null,
    VisualDensity? VisualDensity = null)
{
    public MenuStyle Merge(MenuStyle? style) => style is null ? this : this with
    {
        BackgroundColor = BackgroundColor ?? style.BackgroundColor,
        ShadowColor = ShadowColor ?? style.ShadowColor,
        SurfaceTintColor = SurfaceTintColor ?? style.SurfaceTintColor,
        Elevation = Elevation ?? style.Elevation,
        Padding = Padding ?? style.Padding,
        MinimumSize = MinimumSize ?? style.MinimumSize,
        FixedSize = FixedSize ?? style.FixedSize,
        MaximumSize = MaximumSize ?? style.MaximumSize,
        Side = Side ?? style.Side,
        Shape = Shape ?? style.Shape,
        MouseCursor = MouseCursor ?? style.MouseCursor,
        Alignment = Alignment ?? style.Alignment,
        VisualDensity = VisualDensity ?? style.VisualDensity,
    };
}

public sealed partial record DropdownMenuThemeData(
    TextStyle? TextStyle = null,
    InputDecorationThemeData? InputDecorationTheme = null,
    MenuStyle? MenuStyle = null,
    Color? DisabledColor = null);

public sealed class DropdownMenuTheme : InheritedWidget
{
    public DropdownMenuTheme(DropdownMenuThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public DropdownMenuThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(Data, ((DropdownMenuTheme)oldWidget).Data);

    public static DropdownMenuThemeData? MaybeOf(BuildContext context) =>
        context.DependOnInherited<DropdownMenuTheme>()?.Data;

    public static DropdownMenuThemeData Of(BuildContext context) =>
        MaybeOf(context) ?? Theme.Of(context).DropdownMenuTheme;

    internal static DropdownMenuThemeData Defaults(BuildContext context)
    {
        var theme = Theme.Of(context);
        return new DropdownMenuThemeData(
            TextStyle: theme.TextTheme.BodyLarge,
            InputDecorationTheme: new InputDecorationThemeData(
                Border: new OutlineInputBorder()),
            MenuStyle: new MenuStyle(
                BackgroundColor: MaterialStateProperty<Color?>.All(theme.SurfaceContainerColor),
                ShadowColor: MaterialStateProperty<Color?>.All(theme.ShadowColor),
                SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                Elevation: MaterialStateProperty<double?>.All(3),
                Padding: MaterialStateProperty<Thickness?>.All(new Thickness(0, 8)),
                MinimumSize: MaterialStateProperty<Size?>.All(new Size(112, 0)),
                MaximumSize: MaterialStateProperty<Size?>.All(new Size(double.PositiveInfinity, double.PositiveInfinity)),
                Shape: MaterialStateProperty<ShapeBorder?>.All(ShapeBorder.RoundedRectangle(4)),
                VisualDensity: Plumix.Material.VisualDensity.Standard),
            DisabledColor: MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38));
    }
}
