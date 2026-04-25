using Avalonia;
using Avalonia.Media;
using Flutter.Foundation;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/card.dart (approximate)

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
        var effectiveElevation = ResolveEffectiveElevation(cardTheme, defaults);
        var effectiveShape = Shape ?? cardTheme.Shape ?? defaults.Shape ?? ResolveDefaultShape(theme, Variant);
        var effectiveShadowColor = ShadowColor ?? cardTheme.ShadowColor ?? defaults.ShadowColor ?? theme.ShadowColor;
        var effectiveClipBehavior = ClipBehavior ?? cardTheme.ClipBehavior ?? defaults.ClipBehavior ?? Clip.None;
        var effectiveSurfaceTint = SurfaceTintColor ?? cardTheme.SurfaceTintColor ?? defaults.SurfaceTintColor;
        var effectiveColor = Color ?? cardTheme.Color ?? defaults.Color ?? theme.CardColor;
        if (effectiveSurfaceTint.HasValue)
        {
            effectiveColor = ApplySurfaceTint(effectiveColor, effectiveSurfaceTint.Value, effectiveElevation);
        }

        Widget content = Child ?? new SizedBox();
        var foregroundBorder = BorderOnForeground ? effectiveShape.Side : null;
        if (foregroundBorder.HasValue && foregroundBorder.Value.Width > 0)
        {
            content = new Stack(
                fit: StackFit.Passthrough,
                children:
                [
                    content,
                    new Positioned(
                        left: 0,
                        top: 0,
                        right: 0,
                        bottom: 0,
                        child: new DecoratedBox(
                            new BoxDecoration(
                                Border: foregroundBorder,
                                BorderRadius: effectiveShape.BorderRadius),
                            new SizedBox()))
                ]);
        }

        if (effectiveClipBehavior != Clip.None)
        {
            content = new ClipRRect(effectiveShape.BorderRadius, content);
        }

        var backgroundBorder = BorderOnForeground ? null : effectiveShape.Side;
        var material = new DecoratedBox(
            new BoxDecoration(
                Color: effectiveColor,
                Border: backgroundBorder,
                BorderRadius: effectiveShape.BorderRadius,
                BoxShadows: BuildBoxShadows(effectiveShadowColor, effectiveElevation)),
            content);

        return new Semantics(
            container: SemanticContainer,
            explicitChildNodes: !SemanticContainer,
            child: new Padding(effectiveMargin, material));
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
                Color: theme.SurfaceContainerHighestColor,
                ShadowColor: theme.ShadowColor,
                SurfaceTintColor: Colors.Transparent,
                Elevation: 0,
                Margin: DefaultMargin,
                Shape: ShapeBorder.RoundedRectangle(12)),
            CardVariant.Outlined => new CardThemeData(
                ClipBehavior: Clip.None,
                Color: theme.SurfaceColor,
                ShadowColor: theme.ShadowColor,
                SurfaceTintColor: Colors.Transparent,
                Elevation: 0,
                Margin: DefaultMargin,
                Shape: ShapeBorder.RoundedRectangle(
                    12,
                    new BorderSide(theme.OutlineVariantColor))),
            _ => new CardThemeData(
                ClipBehavior: Clip.None,
                Color: theme.SurfaceContainerLowColor,
                ShadowColor: theme.ShadowColor,
                SurfaceTintColor: Colors.Transparent,
                Elevation: 1,
                Margin: DefaultMargin,
                Shape: ShapeBorder.RoundedRectangle(12)),
        };
    }

    private double ResolveEffectiveElevation(CardThemeData cardTheme, CardThemeData defaults)
    {
        var effectiveElevation = Elevation ?? cardTheme.Elevation ?? defaults.Elevation ?? 1;
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
            return ShapeBorder.RoundedRectangle(12, new BorderSide(theme.OutlineVariantColor));
        }

        return ShapeBorder.RoundedRectangle(theme.UseMaterial3 ? 12 : 4);
    }

    private static BoxShadows? BuildBoxShadows(Color shadowColor, double elevation)
    {
        if (elevation <= 0 || shadowColor.A == 0)
        {
            return null;
        }

        var keyShadow = new BoxShadow
        {
            OffsetX = 0,
            OffsetY = Math.Max(1, Math.Round(elevation)),
            Blur = Math.Max(2, elevation * 2.4),
            Spread = 0,
            Color = ApplyOpacity(shadowColor, 0.20),
            IsInset = false
        };

        var ambientShadow = new BoxShadow
        {
            OffsetX = 0,
            OffsetY = Math.Max(1, Math.Round(elevation * 0.5)),
            Blur = Math.Max(3, elevation * 3.2),
            Spread = 0,
            Color = ApplyOpacity(shadowColor, 0.14),
            IsInset = false
        };

        return new BoxShadows(keyShadow, [ambientShadow]);
    }

    private static Color ApplySurfaceTint(Color color, Color surfaceTint, double elevation)
    {
        if (surfaceTint.A == 0 || elevation <= 0)
        {
            return color;
        }

        var opacity = ResolveSurfaceTintOpacityForElevation(elevation);
        if (opacity <= 0)
        {
            return color;
        }

        var tintOverlay = Avalonia.Media.Color.FromArgb(
            (byte)Math.Clamp((int)(opacity * 255), 0, 255),
            surfaceTint.R,
            surfaceTint.G,
            surfaceTint.B);

        return BlendColorOverlay(color, tintOverlay);
    }

    private static double ResolveSurfaceTintOpacityForElevation(double elevation)
    {
        ReadOnlySpan<(double Elevation, double Opacity)> stops =
        [
            (0.0, 0.0),
            (1.0, 0.05),
            (3.0, 0.08),
            (6.0, 0.11),
            (8.0, 0.12),
            (12.0, 0.14)
        ];

        if (elevation <= stops[0].Elevation)
        {
            return stops[0].Opacity;
        }

        for (var i = 1; i < stops.Length; i++)
        {
            var current = stops[i];
            if (Math.Abs(elevation - current.Elevation) < 0.0001)
            {
                return current.Opacity;
            }

            if (elevation < current.Elevation)
            {
                var lower = stops[i - 1];
                var t = (elevation - lower.Elevation) / (current.Elevation - lower.Elevation);
                return lower.Opacity + (t * (current.Opacity - lower.Opacity));
            }
        }

        return stops[^1].Opacity;
    }

    private static Color BlendColorOverlay(Color baseColor, Color overlayColor)
    {
        static byte Blend(byte from, byte to, double t)
        {
            return (byte)Math.Clamp((int)(from + ((to - from) * t)), 0, 255);
        }

        var clampedOpacity = Math.Clamp(overlayColor.A / 255.0, 0, 1);
        return Avalonia.Media.Color.FromArgb(
            baseColor.A,
            Blend(baseColor.R, overlayColor.R, clampedOpacity),
            Blend(baseColor.G, overlayColor.G, clampedOpacity),
            Blend(baseColor.B, overlayColor.B, clampedOpacity));
    }

    private static Color ApplyOpacity(Color color, double opacityMultiplier)
    {
        var baseOpacity = color.A / 255.0;
        var effectiveOpacity = Math.Clamp(baseOpacity * opacityMultiplier, 0, 1);
        var alpha = (byte)Math.Clamp((int)(effectiveOpacity * 255), 0, 255);
        return Avalonia.Media.Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}
