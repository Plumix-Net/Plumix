using Avalonia.Media;

namespace Plumix.Painting;

// Dart parity source: flutter/packages/flutter/lib/src/painting/colors.dart

/// A color represented using alpha, hue, saturation, and lightness.
public sealed record HSLColor
{
    public HSLColor(double alpha, double hue, double saturation, double lightness)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(alpha, 0.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(alpha, 1.0);
        ArgumentOutOfRangeException.ThrowIfLessThan(hue, 0.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hue, 360.0);
        ArgumentOutOfRangeException.ThrowIfLessThan(saturation, 0.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(saturation, 1.0);
        ArgumentOutOfRangeException.ThrowIfLessThan(lightness, 0.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(lightness, 1.0);

        Alpha = alpha;
        Hue = hue;
        Saturation = saturation;
        Lightness = lightness;
    }

    public double Alpha { get; }

    public double Hue { get; }

    public double Saturation { get; }

    public double Lightness { get; }

    public static HSLColor FromAHSL(double alpha, double hue, double saturation, double lightness)
    {
        return new HSLColor(alpha, hue, saturation, lightness);
    }

    public static HSLColor FromColor(Color color)
    {
        double red = color.R / 255.0;
        double green = color.G / 255.0;
        double blue = color.B / 255.0;
        double max = Math.Max(red, Math.Max(green, blue));
        double min = Math.Min(red, Math.Min(green, blue));
        double delta = max - min;
        double lightness = (max + min) / 2.0;
        double saturation = min == max
            ? 0.0
            : Math.Clamp(delta / (1.0 - Math.Abs((2.0 * lightness) - 1.0)), 0.0, 1.0);

        return new HSLColor(
            color.A / 255.0,
            GetHue(red, green, blue, max, delta),
            saturation,
            lightness);
    }

    public HSLColor WithAlpha(double alpha) => new(alpha, Hue, Saturation, Lightness);

    public HSLColor WithHue(double hue) => new(Alpha, hue, Saturation, Lightness);

    public HSLColor WithSaturation(double saturation) => new(Alpha, Hue, saturation, Lightness);

    public HSLColor WithLightness(double lightness) => new(Alpha, Hue, Saturation, lightness);

    public Color ToColor()
    {
        double chroma = (1.0 - Math.Abs((2.0 * Lightness) - 1.0)) * Saturation;
        double secondary = chroma * (1.0 - Math.Abs(((Hue / 60.0) % 2.0) - 1.0));
        double match = Lightness - (chroma / 2.0);
        (double red, double green, double blue) = Hue switch
        {
            < 60.0 => (chroma, secondary, 0.0),
            < 120.0 => (secondary, chroma, 0.0),
            < 180.0 => (0.0, chroma, secondary),
            < 240.0 => (0.0, secondary, chroma),
            < 300.0 => (secondary, 0.0, chroma),
            _ => (chroma, 0.0, secondary),
        };

        return Color.FromArgb(
            RoundChannel(Alpha),
            RoundChannel(red + match),
            RoundChannel(green + match),
            RoundChannel(blue + match));
    }

    public static HSLColor? Lerp(HSLColor? a, HSLColor? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        if (a is null)
        {
            return b!.WithAlpha(Math.Clamp(b.Alpha * t, 0.0, 1.0));
        }

        if (b is null)
        {
            return a.WithAlpha(Math.Clamp(a.Alpha * (1.0 - t), 0.0, 1.0));
        }

        return new HSLColor(
            Math.Clamp(LerpDouble(a.Alpha, b.Alpha, t), 0.0, 1.0),
            PositiveModulo(LerpDouble(a.Hue, b.Hue, t), 360.0),
            Math.Clamp(LerpDouble(a.Saturation, b.Saturation, t), 0.0, 1.0),
            Math.Clamp(LerpDouble(a.Lightness, b.Lightness, t), 0.0, 1.0));
    }

    private static double GetHue(double red, double green, double blue, double max, double delta)
    {
        if (delta == 0.0)
        {
            return 0.0;
        }

        double hue = max switch
        {
            _ when max == red => 60.0 * PositiveModulo((green - blue) / delta, 6.0),
            _ when max == green => 60.0 * (((blue - red) / delta) + 2.0),
            _ => 60.0 * (((red - green) / delta) + 4.0),
        };
        return double.IsNaN(hue) ? 0.0 : hue;
    }

    private static byte RoundChannel(double channel)
    {
        double rounded = Math.Round(channel * 255.0, MidpointRounding.AwayFromZero);
        return (byte)Math.Clamp(rounded, 0.0, 255.0);
    }

    private static double LerpDouble(double a, double b, double t) => a + ((b - a) * t);

    private static double PositiveModulo(double value, double modulus) => ((value % modulus) + modulus) % modulus;
}
