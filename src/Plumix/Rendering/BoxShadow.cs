using Avalonia;
using Avalonia.Media;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/box_shadow.dart

/// <summary>dart:ui `BlurStyle`, the mask-filter style a shadow's blur is drawn with.</summary>
public enum BlurStyle
{
    /// Fuzzy inside and outside.
    Normal,

    /// Solid inside, fuzzy outside.
    Solid,

    /// Nothing inside, fuzzy outside.
    Outer,

    /// Fuzzy inside, nothing outside.
    Inner,
}

/// <summary>
/// dart:ui `Shadow`: a shadow cast by a shape, described by a color, an offset and a blur radius.
/// </summary>
public record Shadow
{
    /// dart:ui `Shadow._kColorDefault`.
    public static Color DefaultColor { get; } = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);

    public Shadow(Color? color = null, Point offset = default, double blurRadius = 0.0)
    {
        if (blurRadius < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(blurRadius),
                blurRadius,
                "Text shadow blur radius should be non-negative.");
        }

        Color = color ?? DefaultColor;
        Offset = offset;
        BlurRadius = blurRadius;
    }

    /// <summary>Color that the shadow will be drawn with.</summary>
    public Color Color { get; }

    /// <summary>The displacement of the shadow from the casting element.</summary>
    public Point Offset { get; }

    /// <summary>The standard deviation of the Gaussian to convolve with the shadow's shape.</summary>
    public double BlurRadius { get; }

    /// <summary>Converts a blur radius in pixels to sigmas, matching `SkBlurMask::ConvertRadiusToSigma`.</summary>
    public static double ConvertRadiusToSigma(double radius)
    {
        return radius > 0 ? (radius * 0.57735) + 0.5 : 0;
    }

    /// <summary>The <see cref="BlurRadius"/> in sigmas instead of logical pixels.</summary>
    public double BlurSigma => ConvertRadiusToSigma(BlurRadius);

    /// <summary>Returns a new shadow with its offset and blur radius scaled by the given factor.</summary>
    public virtual Shadow Scale(double factor)
    {
        return new Shadow(Color, ScaleOffset(Offset, factor), BlurRadius * factor);
    }

    /// <summary>Linearly interpolates between two shadows.</summary>
    public static Shadow? Lerp(Shadow? a, Shadow? b, double t)
    {
        if (b is null)
        {
            return a?.Scale(1.0 - t);
        }

        if (a is null)
        {
            return b.Scale(t);
        }

        return new Shadow(
            LerpColor(a.Color, b.Color, t),
            LerpOffset(a.Offset, b.Offset, t),
            LerpDouble(a.BlurRadius, b.BlurRadius, t));
    }

    /// <summary>Linearly interpolates between two lists of shadows; excess items are lerped with null.</summary>
    public static IReadOnlyList<Shadow>? LerpList(IReadOnlyList<Shadow>? a, IReadOnlyList<Shadow>? b, double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return LerpShadowList(a, b, t, static (x, y, factor) => Lerp(x, y, factor)!);
    }

    public override string ToString() =>
        $"TextShadow({DartFormat.Color(Color)}, {DartFormat.Offset(Offset)}, {DartFormat.Number(BlurRadius)})";

    internal static IReadOnlyList<TShadow> LerpShadowList<TShadow>(
        IReadOnlyList<TShadow>? a,
        IReadOnlyList<TShadow>? b,
        double t,
        Func<TShadow?, TShadow?, double, TShadow> lerp)
        where TShadow : Shadow
    {
        IReadOnlyList<TShadow> from = a ?? [];
        IReadOnlyList<TShadow> to = b ?? [];
        int commonLength = Math.Min(from.Count, to.Count);
        var result = new List<TShadow>(Math.Max(from.Count, to.Count));
        for (int index = 0; index < commonLength; index++)
        {
            result.Add(lerp(from[index], to[index], t));
        }

        for (int index = commonLength; index < from.Count; index++)
        {
            result.Add(lerp(from[index], null, t));
        }

        for (int index = commonLength; index < to.Count; index++)
        {
            result.Add(lerp(null, to[index], t));
        }

        return result;
    }

    internal static double LerpDouble(double a, double b, double t) => a + ((b - a) * t);

    internal static Point LerpOffset(Point a, Point b, double t)
    {
        return new Point(LerpDouble(a.X, b.X, t), LerpDouble(a.Y, b.Y, t));
    }

    internal static Point ScaleOffset(Point offset, double factor)
    {
        return new Point(offset.X * factor, offset.Y * factor);
    }

    internal static Color LerpColor(Color a, Color b, double t)
    {
        return Color.FromArgb(
            LerpChannel(a.A, b.A, t),
            LerpChannel(a.R, b.R, t),
            LerpChannel(a.G, b.G, t),
            LerpChannel(a.B, b.B, t));
    }

    private static byte LerpChannel(byte a, byte b, double t)
    {
        return (byte)Math.Clamp((int)Math.Round(a + ((b - a) * t)), byte.MinValue, byte.MaxValue);
    }
}

/// <summary>
/// A shadow cast by a box. Adds <see cref="SpreadRadius"/> and <see cref="BlurStyle"/> to <see cref="Shadow"/>.
/// </summary>
public sealed record BoxShadow : Shadow
{
    public BoxShadow(
        Color? color = null,
        Point offset = default,
        double blurRadius = 0.0,
        double spreadRadius = 0.0,
        BlurStyle blurStyle = BlurStyle.Normal)
        : base(color, offset, blurRadius)
    {
        SpreadRadius = spreadRadius;
        BlurStyle = blurStyle;
    }

    /// <summary>The amount the box should be inflated prior to applying the blur.</summary>
    public double SpreadRadius { get; }

    /// <summary>The <see cref="Plumix.Rendering.BlurStyle"/> to use for this shadow.</summary>
    public BlurStyle BlurStyle { get; }

    public override BoxShadow Scale(double factor)
    {
        return new BoxShadow(
            Color,
            ScaleOffset(Offset, factor),
            BlurRadius * factor,
            SpreadRadius * factor,
            BlurStyle);
    }

    public BoxShadow CopyWith(
        Color? color = null,
        Point? offset = null,
        double? blurRadius = null,
        double? spreadRadius = null,
        BlurStyle? blurStyle = null)
    {
        return new BoxShadow(
            color ?? Color,
            offset ?? Offset,
            blurRadius ?? BlurRadius,
            spreadRadius ?? SpreadRadius,
            blurStyle ?? BlurStyle);
    }

    /// <summary>Linearly interpolates between two box shadows.</summary>
    public static BoxShadow? Lerp(BoxShadow? a, BoxShadow? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        if (a is null)
        {
            return b!.Scale(t);
        }

        if (b is null)
        {
            return a.Scale(1.0 - t);
        }

        return new BoxShadow(
            LerpColor(a.Color, b.Color, t),
            LerpOffset(a.Offset, b.Offset, t),
            LerpDouble(a.BlurRadius, b.BlurRadius, t),
            LerpDouble(a.SpreadRadius, b.SpreadRadius, t),
            a.BlurStyle == BlurStyle.Normal ? b.BlurStyle : a.BlurStyle);
    }

    /// <summary>Linearly interpolates between two lists of box shadows.</summary>
    public static IReadOnlyList<BoxShadow>? LerpList(
        IReadOnlyList<BoxShadow>? a,
        IReadOnlyList<BoxShadow>? b,
        double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return LerpShadowList(a, b, t, static (x, y, factor) => Lerp(x, y, factor)!);
    }

    public override string ToString()
    {
        return $"BoxShadow({DartFormat.Color(Color)}, {DartFormat.Offset(Offset)}, "
               + $"{DartFormat.Fixed(BlurRadius)}, {DartFormat.Fixed(SpreadRadius)}, {DartFormat.Enum(BlurStyle)})";
    }
}

/// <summary>
/// Formatting helpers that reproduce Dart's `toString` shapes for painting values.
/// </summary>
internal static class DartFormat
{
    /// dart:ui `Color.toString`.
    public static string Color(Color color) => $"Color(0x{color.ToUInt32():x8})";

    /// dart:ui `Offset.toString`.
    public static string Offset(Point offset) => $"Offset({Fixed(offset.X)}, {Fixed(offset.Y)})";

    /// `foundation.debugFormatDouble`, which prints one fractional digit.
    public static string Fixed(double value)
    {
        return value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// Dart's `double.toString`, which always keeps a fractional part.
    public static string Number(double value)
    {
        string text = value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        return text.Contains('.') || text.Contains('E') || text.Contains("Inf") || text.Contains("NaN")
            ? text
            : text + ".0";
    }

    /// Dart enum values print with a lower-camel name.
    public static string Enum<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        string name = value.ToString()!;
        return $"{typeof(TEnum).Name}.{char.ToLowerInvariant(name[0])}{name[1..]}";
    }
}

/// <summary>
/// Structural comparison for the `List&lt;BoxShadow&gt;` fields Dart compares with `listEquals`.
/// </summary>
internal static class ShadowList
{
    public static bool Equals<TShadow>(IReadOnlyList<TShadow>? a, IReadOnlyList<TShadow>? b)
        where TShadow : Shadow
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null || a.Count != b.Count)
        {
            return false;
        }

        for (int index = 0; index < a.Count; index++)
        {
            if (!a[index].Equals(b[index]))
            {
                return false;
            }
        }

        return true;
    }

    public static int GetHashCode<TShadow>(IReadOnlyList<TShadow>? shadows)
        where TShadow : Shadow
    {
        if (shadows is null)
        {
            return 0;
        }

        var hash = default(HashCode);
        foreach (TShadow shadow in shadows)
        {
            hash.Add(shadow);
        }

        return hash.ToHashCode();
    }
}

/// <summary>
/// Maps framework shadows onto the Avalonia drawing backend, which expresses a shadow as
/// `Avalonia.Media.BoxShadows` rather than as a mask-filtered paint.
/// </summary>
public static class BoxShadowExtensions
{
    /// <summary>Converts a framework shadow list into the backend value used by the drawing context.</summary>
    public static Avalonia.Media.BoxShadows ToAvalonia(this IReadOnlyList<BoxShadow>? shadows)
    {
        if (shadows is null || shadows.Count == 0)
        {
            return default;
        }

        Avalonia.Media.BoxShadow first = shadows[0].ToAvalonia();
        if (shadows.Count == 1)
        {
            return new Avalonia.Media.BoxShadows(first);
        }

        var rest = new Avalonia.Media.BoxShadow[shadows.Count - 1];
        for (int index = 1; index < shadows.Count; index++)
        {
            rest[index - 1] = shadows[index].ToAvalonia();
        }

        return new Avalonia.Media.BoxShadows(first, rest);
    }

    /// <summary>Converts a single framework shadow into the backend value.</summary>
    public static Avalonia.Media.BoxShadow ToAvalonia(this BoxShadow shadow)
    {
        ArgumentNullException.ThrowIfNull(shadow);
        return new Avalonia.Media.BoxShadow
        {
            OffsetX = shadow.Offset.X,
            OffsetY = shadow.Offset.Y,
            Blur = shadow.BlurRadius,
            Spread = shadow.SpreadRadius,
            Color = shadow.Color,
            IsInset = false,
        };
    }
}
