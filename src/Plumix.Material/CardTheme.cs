using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/card_theme.dart

public sealed record CardThemeData
{
    public CardThemeData(
        Clip? ClipBehavior = null,
        Color? Color = null,
        Color? ShadowColor = null,
        Color? SurfaceTintColor = null,
        double? Elevation = null,
        Thickness? Margin = null,
        ShapeBorder? Shape = null)
    {
        if (Elevation.HasValue
            && (double.IsNaN(Elevation.Value)
                || double.IsInfinity(Elevation.Value)
                || Elevation.Value < 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(Elevation),
                "Card theme elevation must be non-negative and finite.");
        }

        this.ClipBehavior = ClipBehavior;
        this.Color = Color;
        this.ShadowColor = ShadowColor;
        this.SurfaceTintColor = SurfaceTintColor;
        this.Elevation = Elevation;
        this.Margin = Margin;
        this.Shape = Shape;
    }

    public Clip? ClipBehavior { get; init; }

    public Color? Color { get; init; }

    public Color? ShadowColor { get; init; }

    public Color? SurfaceTintColor { get; init; }

    public double? Elevation { get; init; }

    public Thickness? Margin { get; init; }

    public ShapeBorder? Shape { get; init; }

    public CardThemeData CopyWith(
        Clip? clipBehavior = null,
        Color? color = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        double? elevation = null,
        Thickness? margin = null,
        ShapeBorder? shape = null)
    {
        return new CardThemeData(
            ClipBehavior: clipBehavior ?? ClipBehavior,
            Color: color ?? Color,
            ShadowColor: shadowColor ?? ShadowColor,
            SurfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            Elevation: elevation ?? Elevation,
            Margin: margin ?? Margin,
            Shape: shape ?? Shape);
    }

    public static CardThemeData Lerp(CardThemeData? a, CardThemeData? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        double clampedT = Math.Clamp(t, 0.0, 1.0);
        return new CardThemeData(
            ClipBehavior: clampedT < 0.5 ? a?.ClipBehavior : b?.ClipBehavior,
            Color: MaterialThemeLerp.Color(a?.Color, b?.Color, clampedT),
            ShadowColor: MaterialThemeLerp.Color(a?.ShadowColor, b?.ShadowColor, clampedT),
            SurfaceTintColor: MaterialThemeLerp.Color(
                a?.SurfaceTintColor,
                b?.SurfaceTintColor,
                clampedT),
            Elevation: MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, clampedT),
            Margin: MaterialThemeLerp.Thickness(a?.Margin, b?.Margin, clampedT),
            Shape: MaterialThemeLerp.Shape(a?.Shape, b?.Shape, clampedT));
    }
}

public sealed class CardTheme : InheritedWidget
{
    public CardTheme(
        Widget? child = null,
        Clip? clipBehavior = null,
        Color? color = null,
        Color? surfaceTintColor = null,
        Color? shadowColor = null,
        double? elevation = null,
        Thickness? margin = null,
        ShapeBorder? shape = null,
        CardThemeData? data = null,
        Key? key = null) : base(key)
    {
        bool hasLegacyProperties = clipBehavior.HasValue
                                   || color.HasValue
                                   || surfaceTintColor.HasValue
                                   || shadowColor.HasValue
                                   || elevation.HasValue
                                   || margin.HasValue
                                   || shape is not null;
        if (data is not null && hasLegacyProperties)
        {
            throw new ArgumentException(
                "data cannot be combined with individual CardTheme properties.",
                nameof(data));
        }

        Data = data ?? new CardThemeData(
            ClipBehavior: clipBehavior,
            Color: color,
            ShadowColor: shadowColor,
            SurfaceTintColor: surfaceTintColor,
            Elevation: elevation,
            Margin: margin,
            Shape: shape);
        Child = child ?? new SizedBox();
    }

    public CardTheme(
        CardThemeData themeData,
        Widget content,
        Key? key = null) : this(
        child: content,
        data: themeData,
        key: key)
    {
    }

    public CardThemeData Data { get; }

    public Widget Child { get; }

    public Clip? ClipBehavior => Data.ClipBehavior;

    public Color? Color => Data.Color;

    public Color? ShadowColor => Data.ShadowColor;

    public Color? SurfaceTintColor => Data.SurfaceTintColor;

    public double? Elevation => Data.Elevation;

    public Thickness? Margin => Data.Margin;

    public ShapeBorder? Shape => Data.Shape;

    public CardTheme CopyWith(
        Clip? clipBehavior = null,
        Color? color = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        double? elevation = null,
        Thickness? margin = null,
        ShapeBorder? shape = null)
    {
        return new CardTheme(
            clipBehavior: clipBehavior ?? ClipBehavior,
            color: color ?? Color,
            shadowColor: shadowColor ?? ShadowColor,
            surfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            elevation: elevation ?? Elevation,
            margin: margin ?? Margin,
            shape: shape ?? Shape);
    }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((CardTheme)oldWidget).Data, Data);
    }

    public static CardThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<CardTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).CardTheme;
    }

    public static CardTheme Lerp(CardTheme? a, CardTheme? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        return new CardTheme(data: CardThemeData.Lerp(a?.Data, b?.Data, t));
    }
}
