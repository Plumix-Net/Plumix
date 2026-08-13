using Avalonia.Media;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/elevation_overlay.dart

/// <summary>
/// Applies the Material 2 elevation overlay and Material 3 surface tint used to
/// communicate the elevation of a surface.
/// </summary>
public static class ElevationOverlay
{
    private static readonly (double Elevation, double Opacity)[] SurfaceTintElevationOpacities =
    [
        (0.0, 0.0),
        (1.0, 0.05),
        (3.0, 0.08),
        (6.0, 0.11),
        (8.0, 0.12),
        (12.0, 0.14),
    ];

    public static Color ApplySurfaceTint(Color color, Color? surfaceTint, double elevation)
    {
        if (!surfaceTint.HasValue || surfaceTint.Value == Colors.Transparent)
        {
            return color;
        }

        double opacity = SurfaceTintOpacityForElevation(elevation);
        return AlphaBlend(WithOpacity(surfaceTint.Value, opacity), color);
    }

    public static Color ApplyOverlay(BuildContext context, Color color, double elevation)
    {
        return ApplyOverlay(Theme.Of(context), color, elevation);
    }

    public static Color OverlayColor(BuildContext context, double elevation)
    {
        return WithOpacity(Theme.Of(context).OnSurfaceColor, OverlayOpacity(elevation));
    }

    public static Color ColorWithOverlay(Color surface, Color overlay, double elevation)
    {
        return AlphaBlend(WithOpacity(overlay, OverlayOpacity(elevation)), surface);
    }

    internal static Color ApplyOverlay(ThemeData theme, Color color, double elevation)
    {
        ArgumentNullException.ThrowIfNull(theme);
        if (elevation > 0.0
            && theme.ApplyElevationOverlayColor
            && theme.Brightness == Brightness.Dark
            && HasSameOpaqueColor(color, theme.SurfaceColor))
        {
            return ColorWithOverlay(color, theme.OnSurfaceColor, elevation);
        }

        return color;
    }

    private static double SurfaceTintOpacityForElevation(double elevation)
    {
        if (elevation < SurfaceTintElevationOpacities[0].Elevation)
        {
            return SurfaceTintElevationOpacities[0].Opacity;
        }

        int index = 0;
        while (elevation >= SurfaceTintElevationOpacities[index].Elevation)
        {
            if (elevation == SurfaceTintElevationOpacities[index].Elevation
                || index + 1 == SurfaceTintElevationOpacities.Length)
            {
                return SurfaceTintElevationOpacities[index].Opacity;
            }

            index++;
        }

        (double lowerElevation, double lowerOpacity) = SurfaceTintElevationOpacities[index - 1];
        (double upperElevation, double upperOpacity) = SurfaceTintElevationOpacities[index];
        double t = (elevation - lowerElevation) / (upperElevation - lowerElevation);
        return lowerOpacity + (t * (upperOpacity - lowerOpacity));
    }

    private static double OverlayOpacity(double elevation)
    {
        return ((4.5 * Math.Log(elevation + 1.0)) + 2.0) / 100.0;
    }

    private static bool HasSameOpaqueColor(Color first, Color second)
    {
        return first.R == second.R && first.G == second.G && first.B == second.B;
    }

    private static Color WithOpacity(Color color, double opacity)
    {
        return Color.FromArgb(ToByte(opacity * 255.0), color.R, color.G, color.B);
    }

    private static Color AlphaBlend(Color foreground, Color background)
    {
        double alpha = foreground.A / 255.0;
        if (alpha == 0.0)
        {
            return background;
        }

        double inverseAlpha = 1.0 - alpha;
        double backgroundAlpha = background.A / 255.0;
        if (backgroundAlpha == 1.0)
        {
            return Color.FromArgb(
                255,
                ToByte(((foreground.R * alpha) + (background.R * inverseAlpha))),
                ToByte(((foreground.G * alpha) + (background.G * inverseAlpha))),
                ToByte(((foreground.B * alpha) + (background.B * inverseAlpha))));
        }

        backgroundAlpha *= inverseAlpha;
        double outputAlpha = alpha + backgroundAlpha;
        if (outputAlpha == 0.0)
        {
            return Colors.Transparent;
        }

        return Color.FromArgb(
            ToByte(outputAlpha * 255.0),
            ToByte(((foreground.R * alpha) + (background.R * backgroundAlpha)) / outputAlpha),
            ToByte(((foreground.G * alpha) + (background.G * backgroundAlpha)) / outputAlpha),
            ToByte(((foreground.B * alpha) + (background.B * backgroundAlpha)) / outputAlpha));
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Clamp(
            (int)Math.Round(value, MidpointRounding.AwayFromZero),
            byte.MinValue,
            byte.MaxValue);
    }
}
