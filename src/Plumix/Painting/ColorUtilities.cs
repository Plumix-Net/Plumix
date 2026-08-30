using System.Globalization;
using Avalonia.Media;

namespace Plumix.Painting;

// C#-only infrastructure: shared equivalents of dart:ui Color.alphaBlend, Color.lerp and
// Color.computeLuminance.

public static class ColorUtilities
{
    /// <summary>Dart's <c>Color.computeLuminance</c>: the WCAG relative luminance of the color.</summary>
    public static double ComputeLuminance(this Color color)
    {
        static double Linearize(byte component)
        {
            double value = component / 255.0;
            return value <= 0.03928
                ? value / 12.92
                : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Linearize(color.R))
               + (0.7152 * Linearize(color.G))
               + (0.0722 * Linearize(color.B));
    }

    /// <summary>
    /// Dart's <c>Color.withValues(alpha:)</c> (spelled <c>Color.withOpacity</c> before 3.27): the
    /// same color at the given alpha.
    /// </summary>
    public static Color WithOpacity(this Color color, double opacity)
    {
        byte alpha = (byte)Math.Clamp((int)Math.Round(255 * opacity), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    /// <summary>
    /// Dart's <c>Color.toString</c>:
    /// <c>Color(alpha: 1.0000, red: 0.1255, green: 0.6980, blue: 0.6667, colorSpace: ColorSpace.sRGB)</c>.
    /// </summary>
    /// <remarks>
    /// The framework's colour type is Avalonia's <see cref="Color"/>, whose own <c>ToString</c>
    /// renders <c>#AARRGGBB</c>. Diagnostics dumps are compared against Flutter's, so
    /// <see cref="ColorSwatch"/>'s <c>ColorProperty</c> spells colours through this formatter
    /// instead. Avalonia models only sRGB, which is dart:ui's default colour space.
    /// </remarks>
    public static string ToDartString(this Color color)
    {
        static string Component(byte value) =>
            (value / 255.0).ToString("F4", CultureInfo.InvariantCulture);

        return $"Color(alpha: {Component(color.A)}, red: {Component(color.R)}, "
               + $"green: {Component(color.G)}, blue: {Component(color.B)}, "
               + "colorSpace: ColorSpace.sRGB)";
    }

    public static Color AlphaBlend(Color foreground, Color background)
    {
        double foregroundAlpha = foreground.A / 255.0;
        double backgroundAlpha = background.A / 255.0;
        double outputAlpha = foregroundAlpha + (backgroundAlpha * (1.0 - foregroundAlpha));
        if (outputAlpha <= 0.0)
        {
            return Colors.Transparent;
        }

        byte Blend(byte foregroundChannel, byte backgroundChannel)
        {
            double channel = ((foregroundChannel * foregroundAlpha)
                              + (backgroundChannel * backgroundAlpha * (1.0 - foregroundAlpha)))
                             / outputAlpha;
            return (byte)Math.Clamp(
                (int)Math.Round(channel, MidpointRounding.AwayFromZero),
                byte.MinValue,
                byte.MaxValue);
        }

        return Color.FromArgb(
            (byte)Math.Clamp(
                (int)Math.Round(outputAlpha * 255.0, MidpointRounding.AwayFromZero),
                byte.MinValue,
                byte.MaxValue),
            Blend(foreground.R, background.R),
            Blend(foreground.G, background.G),
            Blend(foreground.B, background.B));
    }

    public static Color Lerp(Color from, Color to, double t)
    {
        byte LerpChannel(byte start, byte end) =>
            (byte)Math.Clamp(
                (int)Math.Round(start + ((end - start) * t), MidpointRounding.AwayFromZero),
                byte.MinValue,
                byte.MaxValue);

        return Color.FromArgb(
            LerpChannel(from.A, to.A),
            LerpChannel(from.R, to.R),
            LerpChannel(from.G, to.G),
            LerpChannel(from.B, to.B));
    }

    public static Color? Lerp(Color? from, Color? to, double t)
    {
        Color start = from ?? Colors.Transparent;
        Color end = to ?? Colors.Transparent;
        if (!from.HasValue)
        {
            start = Color.FromArgb(0, end.R, end.G, end.B);
        }
        if (!to.HasValue)
        {
            end = Color.FromArgb(0, start.R, start.G, start.B);
        }
        return from.HasValue || to.HasValue ? Lerp(start, end, t) : null;
    }

    public static double? LerpDouble(double? from, double? to, double t)
    {
        if (!from.HasValue && !to.HasValue)
        {
            return null;
        }

        double start = from ?? 0.0;
        double end = to ?? 0.0;
        return start + ((end - start) * t);
    }
}
