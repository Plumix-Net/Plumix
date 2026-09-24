using System.Globalization;

namespace Plumix.UI;

// Dart parity source: dart:ui FontFeature and FontVariation (engine/src/flutter/lib/ui/text.dart).

/// A feature tag and value that affect the selection of glyphs in a font.
public sealed class FontFeature : IEquatable<FontFeature>
{
    /// Creates a <see cref="FontFeature"/> object, which can be added to a text style to control
    /// the selection of glyphs in a font.
    public FontFeature(string feature, int value = 1)
    {
        ArgumentNullException.ThrowIfNull(feature);
        if (feature.Length != 4)
        {
            throw new ArgumentException("Feature tag must be exactly four characters long.", nameof(feature));
        }

        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Feature value must be zero or a positive integer.");
        }

        Feature = feature;
        Value = value;
    }

    /// Create a <see cref="FontFeature"/> object that enables the feature with the given tag.
    public static FontFeature Enable(string feature) => new(feature, 1);

    /// Create a <see cref="FontFeature"/> object that disables the feature with the given tag.
    public static FontFeature Disable(string feature) => new(feature, 0);

    /// Access alternative glyphs (`aalt`).
    public static FontFeature Alternative(int value) => new("aalt", value);

    /// Use alternative ligatures to represent fractions (`afrc`).
    public static FontFeature AlternativeFractions() => new("afrc");

    /// Enable contextual alternates (`calt`).
    public static FontFeature ContextualAlternates() => new("calt");

    /// Enable case-sensitive forms (`case`).
    public static FontFeature CaseSensitiveForms() => new("case");

    /// Select a character variant (`cv01` through `cv99`).
    public static FontFeature CharacterVariant(int value)
    {
        if (value is < 1 or > 99)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "value >= 1 && value <= 99");
        }

        return new FontFeature($"cv{value.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}");
    }

    /// Display digits as denominators (`dnom`).
    public static FontFeature Denominator() => new("dnom");

    /// Use ligatures to represent fractions (`frac`).
    public static FontFeature Fractions() => new("frac");

    /// Use historical forms (`hist`).
    public static FontFeature HistoricalForms() => new("hist");

    /// Use historical ligatures (`hlig`).
    public static FontFeature HistoricalLigatures() => new("hlig");

    /// Use lining figures (`lnum`).
    public static FontFeature LiningFigures() => new("lnum");

    /// Use locale-specific glyphs (`locl`).
    public static FontFeature LocaleAware(bool enable = true) => new("locl", enable ? 1 : 0);

    /// Display alternative glyphs for numerals (`nalt`).
    public static FontFeature NotationalForms(int value = 1) => new("nalt", value);

    /// Display digits as numerators (`numr`).
    public static FontFeature Numerators() => new("numr");

    /// Use old style figures (`onum`).
    public static FontFeature OldstyleFigures() => new("onum");

    /// Use ordinal forms for alphabetic glyphs (`ordn`).
    public static FontFeature OrdinalForms() => new("ordn");

    /// Use proportional (varying width) figures (`pnum`).
    public static FontFeature ProportionalFigures() => new("pnum");

    /// Randomize the alternate forms used in text (`rand`).
    public static FontFeature Randomize() => new("rand");

    /// Enable stylistic alternates (`salt`).
    public static FontFeature StylisticAlternates() => new("salt");

    /// Use scientific inferiors (`sinf`).
    public static FontFeature ScientificInferiors() => new("sinf");

    /// Select a stylistic set (`ss01` through `ss20`).
    public static FontFeature StylisticSet(int value)
    {
        if (value is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "value >= 1 && value <= 20");
        }

        return new FontFeature($"ss{value.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}");
    }

    /// Enable subscripts (`subs`).
    public static FontFeature Subscripts() => new("subs");

    /// Enable superscripts (`sups`).
    public static FontFeature Superscripts() => new("sups");

    /// Enable swash glyphs (`swsh`).
    public static FontFeature Swash(int value = 1) => new("swsh", value);

    /// Use tabular (monospace) figures (`tnum`).
    public static FontFeature TabularFigures() => new("tnum");

    /// Use the slashed zero (`zero`).
    public static FontFeature SlashedZero() => new("zero");

    /// The tag that identifies the effect of this feature. Must consist of 4 ASCII characters.
    public string Feature { get; }

    /// The value assigned to this feature. Must be a positive integer.
    public int Value { get; }

    public bool Equals(FontFeature? other)
    {
        return other is not null
               && other.GetType() == GetType()
               && string.Equals(other.Feature, Feature, StringComparison.Ordinal)
               && other.Value == Value;
    }

    public override bool Equals(object? obj) => Equals(obj as FontFeature);

    public override int GetHashCode() => HashCode.Combine(Feature, Value);

    public override string ToString() => $"FontFeature('{Feature}', {Value.ToString(CultureInfo.InvariantCulture)})";
}

/// An axis tag and value that can be used to customize variable fonts.
public sealed class FontVariation : IEquatable<FontVariation>
{
    // Dart's `32768.0 - 1.0 / 65536.0`: the largest signed 16.16 fixed-point value.
    private const double MaxValue = 32768.0 - (1.0 / 65536.0);

    /// Creates a <see cref="FontVariation"/> object, which can be added to a text style to select
    /// a font face along a variation axis.
    public FontVariation(string axis, double value)
    {
        ArgumentNullException.ThrowIfNull(axis);
        if (axis.Length != 4)
        {
            throw new ArgumentException("Axis tag must be exactly four characters long.", nameof(axis));
        }

        if (!(value >= -32768.0 && value < 32768.0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Value must be representable as a signed 16.16 fixed-point number, i.e. it must be in this "
                + "range: -32768.0 ≤ value < 32768.0");
        }

        Axis = axis;
        Value = value;
    }

    /// Variable font style (`ital`). The value must be between 0.0 and 1.0.
    public static FontVariation Italic(double value)
    {
        Require(value is >= 0.0 and <= 1.0, value, "value >= 0.0 && value <= 1.0");
        return new FontVariation("ital", value);
    }

    /// Optical size optimization (`opsz`). The value must be greater than zero.
    public static FontVariation OpticalSize(double value)
    {
        Require(value > 0.0, value, "value > 0.0");
        return new FontVariation("opsz", value);
    }

    /// Variable font slant (`slnt`), in degrees between -90 and 90 exclusive.
    public static FontVariation Slant(double value)
    {
        Require(value is > -90.0 and < 90.0, value, "value > -90.0 && value < 90.0");
        return new FontVariation("slnt", value);
    }

    /// Variable font width (`wdth`), as a percentage of normal width.
    public static FontVariation Width(double value)
    {
        Require(value >= 0.0, value, "value >= 0.0");
        return new FontVariation("wdth", value);
    }

    /// Variable font weight (`wght`), between 1 and 1000.
    public static FontVariation Weight(double value)
    {
        Require(value is >= 1 and <= 1000, value, "value >= 1 && value <= 1000");
        return new FontVariation("wght", value);
    }

    /// The tag that identifies the design axis. Must consist of 4 ASCII characters.
    public string Axis { get; }

    /// The value assigned to this design axis.
    public double Value { get; }

    /// Linearly interpolates between two font variations.
    ///
    /// When the two variations have different axes (or both are null) the result switches from
    /// `a` to `b` at `t == 0.5`; otherwise the value is interpolated and clamped to the 16.16
    /// fixed-point range.
    public static FontVariation? Lerp(FontVariation? a, FontVariation? b, double t)
    {
        if (!string.Equals(a?.Axis, b?.Axis, StringComparison.Ordinal) || (a is null && b is null))
        {
            return t < 0.5 ? a : b;
        }

        // Dart's `lerpDouble`.
        double value = a!.Value == b!.Value ? a.Value : (a.Value * (1.0 - t)) + (b.Value * t);
        return new FontVariation(a.Axis, Math.Clamp(value, -32768.0, MaxValue));
    }

    public bool Equals(FontVariation? other)
    {
        return other is not null
               && other.GetType() == GetType()
               && string.Equals(other.Axis, Axis, StringComparison.Ordinal)
               && other.Value == Value;
    }

    public override bool Equals(object? obj) => Equals(obj as FontVariation);

    public override int GetHashCode() => HashCode.Combine(Axis, Value);

    public override string ToString() => $"FontVariation('{Axis}', {Rendering.DartFormat.Number(Value)})";

    private static void Require(bool condition, double value, string message)
    {
        if (!condition)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, message);
        }
    }
}
