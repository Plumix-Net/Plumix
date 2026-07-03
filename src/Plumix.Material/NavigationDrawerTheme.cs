using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/navigation_drawer_theme.dart

public sealed record NavigationDrawerThemeData(
    double? TileHeight = null,
    Color? BackgroundColor = null,
    double? Elevation = null,
    Color? ShadowColor = null,
    Color? SurfaceTintColor = null,
    Color? IndicatorColor = null,
    ShapeBorder? IndicatorShape = null,
    Size? IndicatorSize = null,
    MaterialStateProperty<TextStyle?>? LabelTextStyle = null,
    MaterialStateProperty<IconThemeData?>? IconTheme = null);

public sealed class NavigationDrawerTheme : InheritedWidget
{
    public NavigationDrawerTheme(
        NavigationDrawerThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public NavigationDrawerThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((NavigationDrawerTheme)oldWidget).Data, Data);
    }

    public static NavigationDrawerThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<NavigationDrawerTheme>()?.Data
               ?? Theme.Of(context).NavigationDrawerTheme;
    }
}
