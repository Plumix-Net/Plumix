using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/dropdown_menu_theme.dart

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
                border: new OutlineInputBorder()),
            MenuStyle: new MenuStyle(
                backgroundColor: MaterialStateProperty<Color?>.All(theme.ColorScheme.SurfaceContainer),
                shadowColor: MaterialStateProperty<Color?>.All(theme.ShadowColor),
                surfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                elevation: MaterialStateProperty<double?>.All(3),
                padding: MaterialStateProperty<EdgeInsetsGeometry?>.All(
                    EdgeInsetsGeometry.Symmetric(vertical: 8)),
                minimumSize: MaterialStateProperty<Size?>.All(new Size(112, 0)),
                maximumSize: MaterialStateProperty<Size?>.All(
                    new Size(double.PositiveInfinity, double.PositiveInfinity)),
                shape: MaterialStateProperty<OutlinedBorder?>.All(new RoundedRectangleBorder(
                    borderRadius: Plumix.Rendering.BorderRadius.Circular(4))),
                visualDensity: Plumix.Material.VisualDensity.Standard),
            DisabledColor: MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38));
    }
}
