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
            Height: LerpDouble(a?.Height, b?.Height, clampedT),
            BackgroundColor: LerpColor(a?.BackgroundColor, b?.BackgroundColor, clampedT),
            Elevation: LerpDouble(a?.Elevation, b?.Elevation, clampedT),
            ShadowColor: LerpColor(a?.ShadowColor, b?.ShadowColor, clampedT),
            SurfaceTintColor: LerpColor(a?.SurfaceTintColor, b?.SurfaceTintColor, clampedT),
            IndicatorColor: LerpColor(a?.IndicatorColor, b?.IndicatorColor, clampedT),
            IndicatorShape: LerpShape(a?.IndicatorShape, b?.IndicatorShape, clampedT),
            LabelTextStyle: LerpStateProperty(
                a?.LabelTextStyle,
                b?.LabelTextStyle,
                clampedT,
                LerpTextStyle),
            IconTheme: LerpStateProperty(
                a?.IconTheme,
                b?.IconTheme,
                clampedT,
                IconThemeData.Lerp),
            LabelBehavior: clampedT < 0.5 ? a?.LabelBehavior : b?.LabelBehavior,
            OverlayColor: LerpColorStateProperty(
                a?.OverlayColor,
                b?.OverlayColor,
                clampedT),
            LabelPadding: LerpThickness(a?.LabelPadding, b?.LabelPadding, clampedT));
    }

    private static MaterialStateProperty<T?>? LerpStateProperty<T>(
        MaterialStateProperty<T?>? a,
        MaterialStateProperty<T?>? b,
        double t,
        Func<T?, T?, double, T?> lerp) where T : class
    {
        if (a is null && b is null)
        {
            return null;
        }

        return MaterialStateProperty<T?>.ResolveWith(
            states => lerp(a?.Resolve(states), b?.Resolve(states), t));
    }

    private static MaterialStateProperty<Color?>? LerpColorStateProperty(
        MaterialStateProperty<Color?>? a,
        MaterialStateProperty<Color?>? b,
        double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return MaterialStateProperty<Color?>.ResolveWith(
            states => LerpColor(a?.Resolve(states), b?.Resolve(states), t));
    }

    private static double? LerpDouble(double? a, double? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        double from = a ?? 0.0;
        double to = b ?? 0.0;
        return from + ((to - from) * t);
    }

    private static Color? LerpColor(Color? a, Color? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        Color from = a ?? Color.FromArgb(0, b!.Value.R, b.Value.G, b.Value.B);
        Color to = b ?? Color.FromArgb(0, a!.Value.R, a.Value.G, a.Value.B);
        return new ColorTween().Evaluate(t, from, to);
    }

    private static TextStyle? LerpTextStyle(TextStyle? a, TextStyle? b, double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return TextStyle.Lerp(a ?? new TextStyle(), b ?? new TextStyle(), t);
    }

    private static ShapeBorder? LerpShape(ShapeBorder? a, ShapeBorder? b, double t)
    {
        if (a is null || b is null)
        {
            return t < 0.5 ? a : b;
        }

        BorderSide? side = LerpBorderSide(a.Side, b.Side, t);
        double radius = a.BorderRadius.Radius
                        + ((b.BorderRadius.Radius - a.BorderRadius.Radius) * t);
        return new ShapeBorder(BorderRadius.Circular(radius), side)
        {
            Shape = t < 0.5 ? a.Shape : b.Shape,
        };
    }

    private static BorderSide? LerpBorderSide(BorderSide? a, BorderSide? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        BorderSide from = a ?? new BorderSide(
            Color.FromArgb(0, b!.Value.Color.R, b.Value.Color.G, b.Value.Color.B),
            0.0,
            b.Value.Style);
        BorderSide to = b ?? new BorderSide(
            Color.FromArgb(0, a!.Value.Color.R, a.Value.Color.G, a.Value.Color.B),
            0.0,
            a.Value.Style);
        return new BorderSide(
            LerpColor(from.Color, to.Color, t)!.Value,
            from.Width + ((to.Width - from.Width) * t),
            t < 0.5 ? from.Style : to.Style);
    }

    private static Thickness? LerpThickness(Thickness? a, Thickness? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        Thickness from = a ?? default;
        Thickness to = b ?? default;
        return new Thickness(
            from.Left + ((to.Left - from.Left) * t),
            from.Top + ((to.Top - from.Top) * t),
            from.Right + ((to.Right - from.Right) * t),
            from.Bottom + ((to.Bottom - from.Bottom) * t));
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
