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
        if (surfaceTint == null || surfaceTint! == Colors.Transparent)
        {
            return color;
        }

        double opacity = SurfaceTintOpacityForElevation(elevation);
        return AlphaBlend(WithOpacity(surfaceTint!, opacity), color);
    }

    public static Color ApplyOverlay(BuildContext context, Color color, double elevation)
    {
        return ApplyOverlay(Theme.Of(context), color, elevation);
    }

    public static Color OverlayColor(BuildContext context, double elevation)
    {
        return WithOpacity(Theme.Of(context).ColorScheme.OnSurface, OverlayOpacity(elevation));
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
            && HasSameOpaqueColor(color, theme.ColorScheme.Surface))
        {
            return ColorWithOverlay(color, theme.ColorScheme.OnSurface, elevation);
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
        return first.Red == second.Red && first.Green == second.Green && first.Blue == second.Blue;
    }

    private static Color WithOpacity(Color color, double opacity) => color.WithOpacity(opacity);

    internal static Color AlphaBlend(Color foreground, Color background) => Color.AlphaBlend(foreground, background);

    private static byte ToByte(double value)
    {
        return (byte)Math.Clamp(
            (int)Math.Round(value, MidpointRounding.AwayFromZero),
            byte.MinValue,
            byte.MaxValue);
    }
}
