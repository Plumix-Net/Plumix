using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/dropdown_menu_theme.dart

/// <summary>
/// Overrides the defaults of <see cref="DropdownMenu{T}"/> descendants.
/// </summary>
public sealed partial record DropdownMenuThemeData(
    TextStyle? TextStyle = null,
    InputDecorationThemeData? InputDecorationTheme = null,
    MenuStyle? MenuStyle = null,
    Color? DisabledColor = null);

/// <summary>
/// An <see cref="InheritedTheme"/> that overrides the default properties of the
/// <see cref="DropdownMenu{T}"/> widgets below it.
/// </summary>
public sealed class DropdownMenuTheme : InheritedTheme
{
    public DropdownMenuTheme(DropdownMenuThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public DropdownMenuThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child) => new DropdownMenuTheme(Data, child);

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(Data, ((DropdownMenuTheme)oldWidget).Data);

    public static DropdownMenuThemeData? MaybeOf(BuildContext context) =>
        context.DependOnInherited<DropdownMenuTheme>()?.Data;

    public static DropdownMenuThemeData Of(BuildContext context) =>
        MaybeOf(context) ?? Theme.Of(context).DropdownMenuTheme;

    /// <summary>
    /// Flutter's `_DropdownMenuDefaultsM3`. It deliberately leaves the menu surface properties
    /// (background/shadow/surface tint/elevation/shape/padding) unset so they resolve through
    /// <see cref="MenuAnchor"/>'s own `_MenuDefaultsM3`, exactly as in Dart.
    /// </summary>
    internal static DropdownMenuThemeData Defaults(BuildContext context)
    {
        var theme = Theme.Of(context);
        return new DropdownMenuThemeData(
            TextStyle: theme.TextTheme.BodyLarge,
            InputDecorationTheme: new InputDecorationThemeData(border: new OutlineInputBorder()),
            MenuStyle: new MenuStyle(
                minimumSize: MaterialStateProperty<Size?>.All(new Size(112.0, 0.0)),
                maximumSize: MaterialStateProperty<Size?>.All(
                    new Size(double.PositiveInfinity, double.PositiveInfinity)),
                visualDensity: VisualDensity.Standard),
            DisabledColor: MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnSurface, 0.38));
    }
}
