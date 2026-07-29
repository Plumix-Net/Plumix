using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/card.dart

public enum CardVariant
{
    Elevated,
    Filled,
    Outlined,
}

public sealed class Card : StatelessWidget
{
    private static readonly Thickness DefaultMargin = new(4);

    public Card(
        Color? color = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        bool borderOnForeground = true,
        Thickness? margin = null,
        Clip? clipBehavior = null,
        Widget? child = null,
        bool semanticContainer = true,
        Key? key = null) : this(
        variant: CardVariant.Elevated,
        color: color,
        shadowColor: shadowColor,
        surfaceTintColor: surfaceTintColor,
        elevation: elevation,
        shape: shape,
        borderOnForeground: borderOnForeground,
        margin: margin,
        clipBehavior: clipBehavior,
        child: child,
        semanticContainer: semanticContainer,
        key: key)
    {
    }

    private Card(
        CardVariant variant,
        Color? color = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        bool borderOnForeground = true,
        Thickness? margin = null,
        Clip? clipBehavior = null,
        Widget? child = null,
        bool semanticContainer = true,
        Key? key = null) : base(key)
    {
        if (elevation.HasValue
            && (double.IsNaN(elevation.Value)
                || double.IsInfinity(elevation.Value)
                || elevation.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Card elevation must be non-negative and finite.");
        }

        Variant = variant;
        Color = color;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        Elevation = elevation;
        Shape = shape;
        BorderOnForeground = borderOnForeground;
        Margin = margin;
        ClipBehavior = clipBehavior;
        Child = child;
        SemanticContainer = semanticContainer;
    }

    public CardVariant Variant { get; }

    public Color? Color { get; }

    public Color? ShadowColor { get; }

    public Color? SurfaceTintColor { get; }

    public double? Elevation { get; }

    public ShapeBorder? Shape { get; }

    public bool BorderOnForeground { get; }

    public Thickness? Margin { get; }

    public Clip? ClipBehavior { get; }

    public Widget? Child { get; }

    public bool SemanticContainer { get; }

    public static Card Filled(
        Color? color = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        bool borderOnForeground = true,
        Thickness? margin = null,
        Clip? clipBehavior = null,
        Widget? child = null,
        bool semanticContainer = true,
        Key? key = null)
    {
        return new Card(
            variant: CardVariant.Filled,
            color: color,
            shadowColor: shadowColor,
            surfaceTintColor: surfaceTintColor,
            elevation: elevation,
            shape: shape,
            borderOnForeground: borderOnForeground,
            margin: margin,
            clipBehavior: clipBehavior,
            child: child,
            semanticContainer: semanticContainer,
            key: key);
    }

    public static Card Outlined(
        Color? color = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        bool borderOnForeground = true,
        Thickness? margin = null,
        Clip? clipBehavior = null,
        Widget? child = null,
        bool semanticContainer = true,
        Key? key = null)
    {
        return new Card(
            variant: CardVariant.Outlined,
            color: color,
            shadowColor: shadowColor,
            surfaceTintColor: surfaceTintColor,
            elevation: elevation,
            shape: shape,
            borderOnForeground: borderOnForeground,
            margin: margin,
            clipBehavior: clipBehavior,
            child: child,
            semanticContainer: semanticContainer,
            key: key);
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var cardTheme = CardTheme.Of(context);
        var defaults = ResolveDefaults(theme);
        var effectiveMargin = Margin ?? cardTheme.Margin ?? defaults.Margin ?? DefaultMargin;
        double effectiveElevation = ResolveEffectiveElevation(cardTheme, defaults);
        var effectiveShape = Shape ?? cardTheme.Shape ?? defaults.Shape ?? ResolveDefaultShape(theme, Variant);
        var effectiveShadowColor = ShadowColor ?? cardTheme.ShadowColor ?? defaults.ShadowColor ?? theme.ShadowColor;
        var effectiveClipBehavior = ClipBehavior ?? cardTheme.ClipBehavior ?? defaults.ClipBehavior ?? Clip.None;
        var effectiveSurfaceTint = SurfaceTintColor ?? cardTheme.SurfaceTintColor ?? defaults.SurfaceTintColor;
        var effectiveColor = Color ?? cardTheme.Color ?? defaults.Color ?? theme.CardColor;

        return new Semantics(
            container: SemanticContainer,
            child: new Padding(
                effectiveMargin,
                new Material(
                    type: MaterialType.Card,
                    color: effectiveColor,
                    shadowColor: effectiveShadowColor,
                    surfaceTintColor: effectiveSurfaceTint,
                    elevation: effectiveElevation,
                    shape: effectiveShape,
                    borderOnForeground: BorderOnForeground,
                    clipBehavior: effectiveClipBehavior,
                    child: new Semantics(
                        explicitChildNodes: !SemanticContainer,
                        child: Child))));
    }

    private CardThemeData ResolveDefaults(ThemeData theme)
    {
        if (!theme.UseMaterial3)
        {
            return new CardThemeData(
                ClipBehavior: Clip.None,
                Color: theme.CardColor,
                ShadowColor: theme.ShadowColor,
                Elevation: 1,
                Margin: DefaultMargin,
                Shape: ShapeBorder.RoundedRectangle(4));
        }

        return Variant switch
        {
            CardVariant.Filled => new CardThemeData(
                ClipBehavior: Clip.None,
                Color: theme.ColorScheme.SurfaceContainerHighest,
                ShadowColor: theme.ColorScheme.Shadow,
                SurfaceTintColor: Colors.Transparent,
                Elevation: 0,
                Margin: DefaultMargin,
                Shape: ShapeBorder.RoundedRectangle(12)),
            CardVariant.Outlined => new CardThemeData(
                ClipBehavior: Clip.None,
                Color: theme.ColorScheme.Surface,
                ShadowColor: theme.ColorScheme.Shadow,
                SurfaceTintColor: Colors.Transparent,
                Elevation: 0,
                Margin: DefaultMargin,
                Shape: ShapeBorder.RoundedRectangle(
                    12,
                    new BorderSide(theme.ColorScheme.OutlineVariant))),
            _ => new CardThemeData(
                ClipBehavior: Clip.None,
                Color: theme.ColorScheme.SurfaceContainerLow,
                ShadowColor: theme.ColorScheme.Shadow,
                SurfaceTintColor: Colors.Transparent,
                Elevation: 1,
                Margin: DefaultMargin,
                Shape: ShapeBorder.RoundedRectangle(12)),
        };
    }

    private double ResolveEffectiveElevation(CardThemeData cardTheme, CardThemeData defaults)
    {
        double effectiveElevation = Elevation ?? cardTheme.Elevation ?? defaults.Elevation ?? 1;
        if (double.IsNaN(effectiveElevation)
            || double.IsInfinity(effectiveElevation)
            || effectiveElevation < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(CardThemeData.Elevation),
                "Card theme elevation must be non-negative and finite.");
        }

        return effectiveElevation;
    }

    private ShapeBorder ResolveDefaultShape(ThemeData theme, CardVariant variant)
    {
        if (theme.UseMaterial3 && variant == CardVariant.Outlined)
        {
            return ShapeBorder.RoundedRectangle(12, new BorderSide(theme.ColorScheme.OutlineVariant));
        }

        return ShapeBorder.RoundedRectangle(theme.UseMaterial3 ? 12 : 4);
    }
}
