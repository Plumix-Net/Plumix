using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/navigation_bar_theme.dart

public sealed record NavigationBarThemeData(
    double? Height = null,
    Color? BackgroundColor = null,
    double? Elevation = null,
    Color? ShadowColor = null,
    Color? SurfaceTintColor = null,
    Color? IndicatorColor = null,
    ShapeBorder? IndicatorShape = null,
    MaterialStateProperty<TextStyle?>? LabelTextStyle = null,
    MaterialStateProperty<IconThemeData?>? IconTheme = null,
    NavigationDestinationLabelBehavior? LabelBehavior = null,
    MaterialStateProperty<Color?>? OverlayColor = null,
    Thickness? LabelPadding = null)
{
    public NavigationBarThemeData CopyWith(
        double? height = null,
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        Color? indicatorColor = null,
        ShapeBorder? indicatorShape = null,
        MaterialStateProperty<TextStyle?>? labelTextStyle = null,
        MaterialStateProperty<IconThemeData?>? iconTheme = null,
        NavigationDestinationLabelBehavior? labelBehavior = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        Thickness? labelPadding = null)
    {
        return new NavigationBarThemeData(
            Height: height ?? Height,
            BackgroundColor: backgroundColor ?? BackgroundColor,
            Elevation: elevation ?? Elevation,
            ShadowColor: shadowColor ?? ShadowColor,
            SurfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            IndicatorColor: indicatorColor ?? IndicatorColor,
            IndicatorShape: indicatorShape ?? IndicatorShape,
            LabelTextStyle: labelTextStyle ?? LabelTextStyle,
            IconTheme: iconTheme ?? IconTheme,
            LabelBehavior: labelBehavior ?? LabelBehavior,
            OverlayColor: overlayColor ?? OverlayColor,
            LabelPadding: labelPadding ?? LabelPadding);
    }

    public static NavigationBarThemeData? Lerp(
        NavigationBarThemeData? a,
        NavigationBarThemeData? b,
        double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        double clampedT = Math.Clamp(t, 0.0, 1.0);
        return new NavigationBarThemeData(
            Height: MaterialThemeLerp.Double(a?.Height, b?.Height, clampedT),
            BackgroundColor: MaterialThemeLerp.Color(a?.BackgroundColor, b?.BackgroundColor, clampedT),
            Elevation: MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, clampedT),
            ShadowColor: MaterialThemeLerp.Color(a?.ShadowColor, b?.ShadowColor, clampedT),
            SurfaceTintColor: MaterialThemeLerp.Color(
                a?.SurfaceTintColor,
                b?.SurfaceTintColor,
                clampedT),
            IndicatorColor: MaterialThemeLerp.Color(a?.IndicatorColor, b?.IndicatorColor, clampedT),
            IndicatorShape: MaterialThemeLerp.Shape(a?.IndicatorShape, b?.IndicatorShape, clampedT),
            LabelTextStyle: MaterialThemeLerp.StateProperty(
                a?.LabelTextStyle,
                b?.LabelTextStyle,
                clampedT,
                MaterialThemeLerp.TextStyle),
            IconTheme: MaterialThemeLerp.StateProperty(
                a?.IconTheme,
                b?.IconTheme,
                clampedT,
                MaterialThemeLerp.IconTheme),
            LabelBehavior: clampedT < 0.5 ? a?.LabelBehavior : b?.LabelBehavior,
            OverlayColor: MaterialThemeLerp.ColorStateProperty(
                a?.OverlayColor,
                b?.OverlayColor,
                clampedT),
            LabelPadding: MaterialThemeLerp.Thickness(a?.LabelPadding, b?.LabelPadding, clampedT));
    }
}

public sealed class NavigationBarTheme : InheritedTheme
{
    public NavigationBarTheme(NavigationBarThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public NavigationBarThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new NavigationBarTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((NavigationBarTheme)oldWidget).Data, Data);
    }

    public static NavigationBarThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<NavigationBarTheme>()?.Data
               ?? Theme.Of(context).NavigationBarTheme;
    }
}
