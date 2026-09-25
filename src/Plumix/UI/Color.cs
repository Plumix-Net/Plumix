using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity source: dart:ui Color, ColorSpace and the colour-space transforms
// (engine/src/flutter/lib/ui/painting.dart).

/// <summary>The color space describes the colors that are available to an <see cref="Color"/>.</summary>
public enum ColorSpace
{
    /// The sRGB color space.
    SRGB,

    /// A color space that is backwards compatible with sRGB but can represent colors outside of that
    /// gamut with values outside of [0..1].
    ExtendedSRGB,

    /// The Display P3 color space.
    DisplayP3,
}

/// <summary>
/// An immutable color value in ARGB format, with each component stored as a double in the
/// <see cref="ColorSpace"/> the color belongs to.
/// </summary>
/// <remarks>
/// Like Dart's <c>Color</c>, this is a non-sealed class: <c>ColorSwatch</c>, <c>WidgetStateColor</c>
/// and <c>CupertinoDynamicColor</c> derive from it, so any <see cref="Color"/> slot can hold them.
/// Every member Dart's implementers override is virtual. Equality follows Dart's <c>==</c>: two
/// colors are equal only when they have the same runtime type.
/// C#-only: the colour converts implicitly to <see cref="Avalonia.Media.Color"/> (the drawing
/// backend's 8-bit sRGB value, quantised like <see cref="ToARGB32"/>) and explicitly back.
/// </remarks>
public class Color : IEquatable<Color>
{
    private readonly double _a;
    private readonly double _r;
    private readonly double _g;
    private readonly double _b;
    private readonly ColorSpace _colorSpace;

    /// <summary>
    /// Construct an sRGB color from the lower 32 bits of an int: alpha in bits 24-31, red in bits
    /// 16-23, green in bits 8-15 and blue in bits 0-7.
    /// </summary>
    public Color(uint value)
        : this(
            (int)(value >> 24),
            (int)(value >> 16),
            (int)(value >> 8),
            (int)value,
            ColorSpace.SRGB,
            fromArgbc: true)
    {
    }

    /// <summary>Dart's <c>Color.from</c>, for subclasses.</summary>
    protected Color(double alpha, double red, double green, double blue, ColorSpace colorSpace = ColorSpace.SRGB)
    {
        _a = alpha;
        _r = red;
        _g = green;
        _b = blue;
        _colorSpace = colorSpace;
    }

    // Dart's `Color._fromARGBC`.
    private Color(int alpha, int red, int green, int blue, ColorSpace colorSpace, bool fromArgbc)
        : this(red, green, blue, (alpha & 0xff) / 255.0, colorSpace)
    {
        _ = fromArgbc;
    }

    // Dart's `Color._fromRGBOC`.
    private Color(int r, int g, int b, double opacity, ColorSpace colorSpace)
    {
        _a = opacity;
        _r = (r & 0xff) / 255.0;
        _g = (g & 0xff) / 255.0;
        _b = (b & 0xff) / 255.0;
        _colorSpace = colorSpace;
    }

    /// <summary>Construct a color with normalized color components.</summary>
    public static Color From(
        double alpha,
        double red,
        double green,
        double blue,
        ColorSpace colorSpace = ColorSpace.SRGB) =>
        new(alpha, red, green, blue, colorSpace);

    /// <summary>Construct an sRGB color from the lower 8 bits of four integers.</summary>
    public static Color FromARGB(int a, int r, int g, int b) => new(a, r, g, b, ColorSpace.SRGB, fromArgbc: true);

    /// <summary>
    /// Create an sRGB color from red, green, blue, and opacity, similar to <c>rgba()</c> in CSS.
    /// </summary>
    public static Color FromRGBO(int r, int g, int b, double opacity) => new(r, g, b, opacity, ColorSpace.SRGB);

    /// <summary>The alpha channel of this color, from 0.0 to 1.0.</summary>
    public virtual double A => _a;

    /// <summary>The red channel of this color.</summary>
    public virtual double R => _r;

    /// <summary>The green channel of this color.</summary>
    public virtual double G => _g;

    /// <summary>The blue channel of this color.</summary>
    public virtual double B => _b;

    /// <summary>The color space of this color.</summary>
    public virtual ColorSpace ColorSpace => _colorSpace;

    private static int FloatToInt8(double x) =>
        Math.Clamp((int)Math.Round(x * 255.0, MidpointRounding.AwayFromZero), 0, 255);

    /// <summary>A 32 bit value representing this color (deprecated in Dart; use <see cref="ToARGB32"/>).</summary>
    public virtual uint Value => ToARGB32();

    /// <summary>Returns a 32-bit value representing this color.</summary>
    public virtual uint ToARGB32() =>
        (uint)((FloatToInt8(A) << 24)
               | (FloatToInt8(R) << 16)
               | (FloatToInt8(G) << 8)
               | FloatToInt8(B));

    /// <summary>The alpha channel of this color in an 8 bit value (deprecated in Dart).</summary>
    public virtual int Alpha => (int)((0xff000000 & Value) >> 24);

    /// <summary>The alpha channel of this color as a double (deprecated in Dart; use <see cref="A"/>).</summary>
    public virtual double Opacity => Alpha / (double)0xFF;

    /// <summary>The red channel of this color in an 8 bit value (deprecated in Dart).</summary>
    public virtual int Red => (int)((0x00ff0000 & Value) >> 16);

    /// <summary>The green channel of this color in an 8 bit value (deprecated in Dart).</summary>
    public virtual int Green => (int)((0x0000ff00 & Value) >> 8);

    /// <summary>The blue channel of this color in an 8 bit value (deprecated in Dart).</summary>
    public virtual int Blue => (int)(0x000000ff & Value);

    /// <summary>
    /// Returns a new color with the provided components updated, converted to
    /// <paramref name="colorSpace"/> when it differs from this color's.
    /// </summary>
    public virtual Color WithValues(
        double? alpha = null,
        double? red = null,
        double? green = null,
        double? blue = null,
        ColorSpace? colorSpace = null)
    {
        Color? updatedComponents = null;
        if (alpha != null || red != null || green != null || blue != null)
        {
            updatedComponents = From(
                alpha: alpha ?? A,
                red: red ?? R,
                green: green ?? G,
                blue: blue ?? B,
                colorSpace: ColorSpace);
        }

        if (colorSpace != null && colorSpace != ColorSpace)
        {
            IColorTransform transform = GetColorTransform(ColorSpace, (ColorSpace)colorSpace);
            return transform.Transform(updatedComponents ?? this, (ColorSpace)colorSpace);
        }

        return updatedComponents ?? this;
    }

    /// <summary>Returns a new color that matches this color with the alpha channel replaced.</summary>
    public virtual Color WithAlpha(int a) => FromARGB(a, Red, Green, Blue);

    /// <summary>
    /// Returns a new color that matches this color with the alpha channel replaced with the given
    /// opacity (deprecated in Dart; use <see cref="WithValues"/> to avoid precision loss).
    /// </summary>
    public virtual Color WithOpacity(double opacity)
    {
        DebugAssertions.Assert(opacity >= 0.0 && opacity <= 1.0);
        return WithAlpha((int)Math.Round(255.0 * opacity, MidpointRounding.AwayFromZero));
    }

    /// <summary>Returns a new color that matches this color with the red channel replaced.</summary>
    public virtual Color WithRed(int r) => FromARGB(Alpha, r, Green, Blue);

    /// <summary>Returns a new color that matches this color with the green channel replaced.</summary>
    public virtual Color WithGreen(int g) => FromARGB(Alpha, Red, g, Blue);

    /// <summary>Returns a new color that matches this color with the blue channel replaced.</summary>
    public virtual Color WithBlue(int b) => FromARGB(Alpha, Red, Green, b);

    // See <https://www.w3.org/TR/WCAG20/#relativeluminancedef>
    private static double LinearizeColorComponent(double component)
    {
        if (component <= 0.03928)
        {
            return component / 12.92;
        }

        return Math.Pow((component + 0.055) / 1.055, 2.4);
    }

    /// <summary>
    /// Returns a brightness value between 0 for darkest and 1 for lightest: the relative luminance
    /// of the color.
    /// </summary>
    public virtual double ComputeLuminance()
    {
        DebugAssertions.Assert(ColorSpace != ColorSpace.ExtendedSRGB);
        // See <https://www.w3.org/TR/WCAG20/#relativeluminancedef>
        double rLinear = LinearizeColorComponent(R);
        double gLinear = LinearizeColorComponent(G);
        double bLinear = LinearizeColorComponent(B);
        return (0.2126 * rLinear) + (0.7152 * gLinear) + (0.0722 * bLinear);
    }

    // Dart's top-level `_scaleAlpha`.
    private static Color ScaleAlpha(Color x, double factor) =>
        x.WithValues(alpha: ClampDouble(x.A * factor, 0, 1));

    // Dart's top-level `_widerColorSpace`.
    private static ColorSpace WiderColorSpace(ColorSpace a, ColorSpace b) =>
        a == ColorSpace.DisplayP3 || b == ColorSpace.DisplayP3 ? ColorSpace.DisplayP3 : a;

    // dart:ui `_lerpDouble`.
    private static double LerpDouble(double a, double b, double t) => (a * (1.0 - t)) + (b * t);

    // package:flutter `clampDouble`, which dart:ui uses through its own copy.
    private static double ClampDouble(double x, double min, double max)
    {
        if (x < min)
        {
            return min;
        }

        if (x > max)
        {
            return max;
        }

        return double.IsNaN(x) ? max : x;
    }

    /// <summary>Linearly interpolate between two colors.</summary>
    /// <remarks>
    /// If either color is null, this function linearly interpolates from a transparent instance of
    /// the other color. The result is null only when both are null.
    /// </remarks>
    [return: NotNullIfNotNull(nameof(x))]
    [return: NotNullIfNotNull(nameof(y))]
    public static Color? Lerp(Color? x, Color? y, double t)
    {
        DebugAssertions.Assert(x?.ColorSpace != ColorSpace.ExtendedSRGB);
        DebugAssertions.Assert(y?.ColorSpace != ColorSpace.ExtendedSRGB);
        if (y == null)
        {
            return x == null ? null : ScaleAlpha(x, 1.0 - t);
        }

        if (x == null)
        {
            return ScaleAlpha(y, t);
        }

        Color a;
        Color b;
        ColorSpace resultColorSpace;
        if (x.ColorSpace == y.ColorSpace)
        {
            a = x;
            b = y;
            resultColorSpace = x.ColorSpace;
        }
        else
        {
            resultColorSpace = WiderColorSpace(x.ColorSpace, y.ColorSpace);
            a = x.WithValues(colorSpace: resultColorSpace);
            b = y.WithValues(colorSpace: resultColorSpace);
        }

        return From(
            alpha: ClampDouble(LerpDouble(a.A, b.A, t), 0, 1),
            red: ClampDouble(LerpDouble(a.R, b.R, t), 0, 1),
            green: ClampDouble(LerpDouble(a.G, b.G, t), 0, 1),
            blue: ClampDouble(LerpDouble(a.B, b.B, t), 0, 1),
            colorSpace: resultColorSpace);
    }

    /// <summary>
    /// Combine the foreground color as a transparent color over top of a background color, and
    /// return the resulting combined color.
    /// </summary>
    public static Color AlphaBlend(Color foreground, Color background)
    {
        DebugAssertions.Assert(foreground.ColorSpace == background.ColorSpace);
        DebugAssertions.Assert(foreground.ColorSpace != ColorSpace.ExtendedSRGB);
        double alpha = foreground.A;
        if (alpha == 0)
        {
            // Foreground completely transparent.
            return background;
        }

        double invAlpha = 1 - alpha;
        double backAlpha = background.A;
        if (backAlpha == 1)
        {
            // Opaque background case
            return From(
                alpha: 1,
                red: (alpha * foreground.R) + (invAlpha * background.R),
                green: (alpha * foreground.G) + (invAlpha * background.G),
                blue: (alpha * foreground.B) + (invAlpha * background.B),
                colorSpace: foreground.ColorSpace);
        }

        // General case
        backAlpha *= invAlpha;
        double outAlpha = alpha + backAlpha;
        DebugAssertions.Assert(outAlpha != 0);
        return From(
            alpha: outAlpha,
            red: ((foreground.R * alpha) + (background.R * backAlpha)) / outAlpha,
            green: ((foreground.G * alpha) + (background.G * backAlpha)) / outAlpha,
            blue: ((foreground.B * alpha) + (background.B * backAlpha)) / outAlpha,
            colorSpace: foreground.ColorSpace);
    }

    /// <summary>
    /// Returns an alpha value representative of the provided opacity value, clamped to 0.0..1.0.
    /// </summary>
    public static int GetAlphaFromOpacity(double opacity) =>
        (int)Math.Round(ClampDouble(opacity, 0.0, 1.0) * 255, MidpointRounding.AwayFromZero);

    public static bool operator ==(Color? left, Color? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Color? left, Color? right) => !(left == right);

    public virtual bool Equals(Color? other) => Equals((object?)other);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        return obj is Color other
               && other.A == A
               && other.R == R
               && other.G == G
               && other.B == B
               && other.ColorSpace == ColorSpace;
    }

    public override int GetHashCode() => HashCode.Combine(A, R, G, B, ColorSpace);

    public override string ToString() =>
        $"Color(alpha: {Fixed4(A)}, red: {Fixed4(R)}, green: {Fixed4(G)}, blue: {Fixed4(B)}, "
        + $"colorSpace: {ColorSpaceName(ColorSpace)})";

    private static string Fixed4(double value) => value.ToString("F4", CultureInfo.InvariantCulture);

    private static string ColorSpaceName(ColorSpace colorSpace) => colorSpace switch
    {
        ColorSpace.SRGB => "ColorSpace.sRGB",
        ColorSpace.ExtendedSRGB => "ColorSpace.extendedSRGB",
        _ => "ColorSpace.displayP3",
    };

    /// <summary>C#-only: the drawing backend's 8-bit sRGB value of this color.</summary>
    public static implicit operator Avalonia.Media.Color(Color color)
    {
        ArgumentNullException.ThrowIfNull(color);
        return Avalonia.Media.Color.FromUInt32(color.ToARGB32());
    }

    /// <summary>C#-only: an sRGB color from the drawing backend's 8-bit value.</summary>
    public static explicit operator Color(Avalonia.Media.Color color) => new(color.ToUInt32());

    // Dart's `_ColorTransform`.
    private interface IColorTransform
    {
        Color Transform(Color color, ColorSpace resultColorSpace);
    }

    // Dart's `_IdentityColorTransform`.
    private sealed class IdentityColorTransform : IColorTransform
    {
        public static readonly IdentityColorTransform Instance = new();

        public Color Transform(Color color, ColorSpace resultColorSpace) => color;
    }

    // Dart's `_ClampTransform`. Like Dart's, it clamps the input and never runs its child.
    private sealed class ClampTransform(IColorTransform child) : IColorTransform
    {
        public IColorTransform Child { get; } = child;

        public Color Transform(Color color, ColorSpace resultColorSpace) =>
            From(
                alpha: ClampDouble(color.A, 0, 1),
                red: ClampDouble(color.R, 0, 1),
                green: ClampDouble(color.G, 0, 1),
                blue: ClampDouble(color.B, 0, 1),
                colorSpace: resultColorSpace);
    }

    // sRGB standard constants for transfer functions.
    // See https://en.wikipedia.org/wiki/SRGB.
    private const double SrgbGamma = 2.4;
    private const double SrgbLinearThreshold = 0.04045;
    private const double SrgbLinearSlope = 12.92;
    private const double SrgbEncodedOffset = 0.055;
    private const double SrgbEncodedDivisor = 1.055;
    private const double SrgbLinearToEncodedThreshold = 0.0031308;

    private static double SrgbEotf(double v)
    {
        if (v <= SrgbLinearThreshold)
        {
            return v / SrgbLinearSlope;
        }

        return Math.Pow((v + SrgbEncodedOffset) / SrgbEncodedDivisor, SrgbGamma);
    }

    private static double SrgbOetf(double v)
    {
        if (v <= SrgbLinearToEncodedThreshold)
        {
            return v * SrgbLinearSlope;
        }

        return (SrgbEncodedDivisor * Math.Pow(v, 1.0 / SrgbGamma)) - SrgbEncodedOffset;
    }

    private static double SrgbEotfExtended(double v) => v < 0.0 ? -SrgbEotf(-v) : SrgbEotf(v);

    private static double SrgbOetfExtended(double v) => v < 0.0 ? -SrgbOetf(-v) : SrgbOetf(v);

    private static readonly double[] P3ToSrgbLinear =
    [
        1.2249401, -0.2249402, 0.0,
        -0.0420569, 1.0420571, 0.0,
        -0.0196376, -0.0786507, 1.0982884,
    ];

    private static readonly double[] SrgbToP3Linear =
    [
        0.8224622, 0.1775380, 0.0,
        0.0331942, 0.9668058, 0.0,
        0.0170806, 0.0723974, 0.9105220,
    ];

    // Dart's `_P3ToSrgbTransform` and `_SrgbToP3Transform`: the same linear-space matrix product.
    private sealed class MatrixColorTransform(double[] matrix) : IColorTransform
    {
        public static readonly MatrixColorTransform P3ToSrgb = new(P3ToSrgbLinear);

        public static readonly MatrixColorTransform SrgbToP3 = new(SrgbToP3Linear);

        public Color Transform(Color color, ColorSpace resultColorSpace)
        {
            double rLin = SrgbEotfExtended(color.R);
            double gLin = SrgbEotfExtended(color.G);
            double bLin = SrgbEotfExtended(color.B);

            double rOut = (matrix[0] * rLin) + (matrix[1] * gLin) + (matrix[2] * bLin);
            double gOut = (matrix[3] * rLin) + (matrix[4] * gLin) + (matrix[5] * bLin);
            double bOut = (matrix[6] * rLin) + (matrix[7] * gLin) + (matrix[8] * bLin);

            return From(
                alpha: color.A,
                red: SrgbOetfExtended(rOut),
                green: SrgbOetfExtended(gOut),
                blue: SrgbOetfExtended(bOut),
                colorSpace: resultColorSpace);
        }
    }

    // Dart's `_getColorTransform`.
    private static IColorTransform GetColorTransform(ColorSpace source, ColorSpace destination) =>
        (source, destination) switch
        {
            (ColorSpace.SRGB, ColorSpace.DisplayP3) => MatrixColorTransform.SrgbToP3,
            (ColorSpace.SRGB, _) => IdentityColorTransform.Instance,
            (ColorSpace.ExtendedSRGB, ColorSpace.SRGB) => new ClampTransform(IdentityColorTransform.Instance),
            (ColorSpace.ExtendedSRGB, ColorSpace.ExtendedSRGB) => IdentityColorTransform.Instance,
            (ColorSpace.ExtendedSRGB, _) => new ClampTransform(MatrixColorTransform.SrgbToP3),
            (_, ColorSpace.SRGB) => new ClampTransform(MatrixColorTransform.P3ToSrgb),
            (_, ColorSpace.ExtendedSRGB) => MatrixColorTransform.P3ToSrgb,
            _ => IdentityColorTransform.Instance,
        };
}
