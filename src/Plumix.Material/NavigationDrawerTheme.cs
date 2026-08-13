using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/navigation_drawer_theme.dart

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
    MaterialStateProperty<IconThemeData?>? IconTheme = null)
{
    public NavigationDrawerThemeData CopyWith(
        double? tileHeight = null,
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        Color? indicatorColor = null,
        ShapeBorder? indicatorShape = null,
        Size? indicatorSize = null,
        MaterialStateProperty<TextStyle?>? labelTextStyle = null,
        MaterialStateProperty<IconThemeData?>? iconTheme = null)
    {
        return new NavigationDrawerThemeData(
            TileHeight: tileHeight ?? TileHeight,
            BackgroundColor: backgroundColor ?? BackgroundColor,
            Elevation: elevation ?? Elevation,
            ShadowColor: shadowColor ?? ShadowColor,
            SurfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            IndicatorColor: indicatorColor ?? IndicatorColor,
            IndicatorShape: indicatorShape ?? IndicatorShape,
            IndicatorSize: indicatorSize ?? IndicatorSize,
            LabelTextStyle: labelTextStyle ?? LabelTextStyle,
            IconTheme: iconTheme ?? IconTheme);
    }

    public static NavigationDrawerThemeData? Lerp(
        NavigationDrawerThemeData? a,
        NavigationDrawerThemeData? b,
        double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        double clampedT = Math.Clamp(t, 0.0, 1.0);
        return new NavigationDrawerThemeData(
            TileHeight: MaterialThemeLerp.Double(a?.TileHeight, b?.TileHeight, clampedT),
            BackgroundColor: MaterialThemeLerp.Color(
                a?.BackgroundColor,
                b?.BackgroundColor,
                clampedT),
            Elevation: MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, clampedT),
            ShadowColor: MaterialThemeLerp.Color(a?.ShadowColor, b?.ShadowColor, clampedT),
            SurfaceTintColor: MaterialThemeLerp.Color(
                a?.SurfaceTintColor,
                b?.SurfaceTintColor,
                clampedT),
            IndicatorColor: MaterialThemeLerp.Color(
                a?.IndicatorColor,
                b?.IndicatorColor,
                clampedT),
            IndicatorShape: MaterialThemeLerp.Shape(
                a?.IndicatorShape,
                b?.IndicatorShape,
                clampedT),
            // Flutter currently lerps indicatorSize from the first value to itself.
            IndicatorSize: MaterialThemeLerp.Size(a?.IndicatorSize, a?.IndicatorSize, clampedT),
            LabelTextStyle: MaterialThemeLerp.StateProperty(
                a?.LabelTextStyle,
                b?.LabelTextStyle,
                clampedT,
                MaterialThemeLerp.TextStyle),
            IconTheme: MaterialThemeLerp.StateProperty(
                a?.IconTheme,
                b?.IconTheme,
                clampedT,
                MaterialThemeLerp.IconTheme));
    }
}

public sealed class NavigationDrawerTheme : InheritedTheme
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

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new NavigationDrawerTheme(Data, child);
    }

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
