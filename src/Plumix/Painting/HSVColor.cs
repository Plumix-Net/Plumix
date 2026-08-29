using System.Globalization;
using Avalonia.Media;

namespace Plumix.Painting;

// Dart parity source: flutter/packages/flutter/lib/src/painting/colors.dart

/// A color represented using alpha, hue, saturation, and value.
///
/// An <see cref="HSVColor"/> is represented in a parameter space that is based on human perception
/// of color in pigments (e.g. paint and printer's ink). The hue describes which pigment is used,
/// the saturation which shade of the pigment, and the value resembles mixing the pigment with
/// different amounts of black or white pigment.
public sealed record HSVColor
{
    /// <summary>Creates a color. All arguments must be within their respective ranges.</summary>
    public HSVColor(double alpha, double hue, double saturation, double value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(alpha, 0.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(alpha, 1.0);
        ArgumentOutOfRangeException.ThrowIfLessThan(hue, 0.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hue, 360.0);
        ArgumentOutOfRangeException.ThrowIfLessThan(saturation, 0.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(saturation, 1.0);
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0);

        Alpha = alpha;
        Hue = hue;
        Saturation = saturation;
        Value = value;
    }

    /// Alpha, from 0.0 to 1.0. Describes the transparency of the color.
    public double Alpha { get; }

    /// Hue, from 0.0 to 360.0. Describes which color of the spectrum is represented.
    public double Hue { get; }

    /// Saturation, from 0.0 to 1.0. Describes how colorful the color is.
    public double Saturation { get; }

    /// Value, from 0.0 to 1.0. Describes how bright the color is.
    public double Value { get; }

    /// <remarks>Flutter's <c>HSVColor.fromAHSV</c>.</remarks>
    public static HSVColor FromAHSV(double alpha, double hue, double saturation, double value)
    {
        return new HSVColor(alpha, hue, saturation, value);
    }

    /// <remarks>Flutter's <c>HSVColor.fromColor</c>.</remarks>
    public static HSVColor FromColor(Color color)
    {
        double red = color.R / 255.0;
        double green = color.G / 255.0;
        double blue = color.B / 255.0;
        double max = Math.Max(red, Math.Max(green, blue));
        double min = Math.Min(red, Math.Min(green, blue));
        double delta = max - min;
        double saturation = max == 0.0 ? 0.0 : delta / max;

        return new HSVColor(color.A / 255.0, GetHue(red, green, blue, max, delta), saturation, max);
    }

    public HSVColor WithAlpha(double alpha) => new(alpha, Hue, Saturation, Value);

    public HSVColor WithHue(double hue) => new(Alpha, hue, Saturation, Value);

    public HSVColor WithSaturation(double saturation) => new(Alpha, Hue, saturation, Value);

    public HSVColor WithValue(double value) => new(Alpha, Hue, Saturation, value);

    /// <summary>Returns this color in RGB.</summary>
    public Color ToColor()
    {
        double chroma = Saturation * Value;
        double secondary = chroma * (1.0 - Math.Abs(((Hue / 60.0) % 2.0) - 1.0));
        double match = Value - chroma;
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

    /// <summary>Linearly interpolates between two <see cref="HSVColor"/>s.</summary>
    /// <remarks>
    /// Flutter's <c>HSVColor.lerp</c>: the channels are interpolated separately, and a null operand
    /// is treated as a transparent instance of the other color.
    /// </remarks>
    public static HSVColor? Lerp(HSVColor? a, HSVColor? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        if (a is null)
        {
            return b!.ScaleAlpha(t);
        }

        if (b is null)
        {
            return a.ScaleAlpha(1.0 - t);
        }

        return new HSVColor(
            Math.Clamp(LerpDouble(a.Alpha, b.Alpha, t), 0.0, 1.0),
            PositiveModulo(LerpDouble(a.Hue, b.Hue, t), 360.0),
            Math.Clamp(LerpDouble(a.Saturation, b.Saturation, t), 0.0, 1.0),
            Math.Clamp(LerpDouble(a.Value, b.Value, t), 0.0, 1.0));
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"HSVColor({Format(Alpha)}, {Format(Hue)}, {Format(Saturation)}, {Format(Value)})";

    /// <remarks>
    /// Dart's <c>double.toString</c> always writes a fraction part, so <c>60.0</c> prints as
    /// <c>60.0</c> where .NET's invariant formatting prints <c>60</c>.
    /// </remarks>
    private static string Format(double value) => double.IsFinite(value) && value == Math.Floor(value)
        ? value.ToString("F1", CultureInfo.InvariantCulture)
        : value.ToString(CultureInfo.InvariantCulture);

    /// <remarks>Flutter's <c>HSVColor._scaleAlpha</c>.</remarks>
    private HSVColor ScaleAlpha(double factor) => WithAlpha(Alpha * factor);

    /// <remarks>Flutter's <c>painting/colors.dart</c> top-level <c>_getHue</c>.</remarks>
    private static double GetHue(double red, double green, double blue, double max, double delta)
    {
        double hue = max switch
        {
            0.0 => 0.0,
            _ when max == red => 60.0 * PositiveModulo((green - blue) / delta, 6.0),
            _ when max == green => 60.0 * (((blue - red) / delta) + 2.0),
            _ when max == blue => 60.0 * (((red - green) / delta) + 4.0),
            _ => 0.0,
        };

        // Set hue to 0.0 when red == green == blue.
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
