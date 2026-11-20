namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/text.dart (FontStyle, FontWeight)

/// Whether to use the italic type variation of glyphs in the font.
public enum FontStyle
{
    /// Use the upright ("Roman") glyphs.
    Normal,

    /// Use glyphs that have a more pronounced angle and typically a cursive style ("italic type").
    Italic,
}

/// <summary>The thickness of the glyphs used to draw the text.</summary>
/// <remarks>
/// A value type: <c>default(FontWeight)</c> is <see cref="Normal"/>, the weight Dart uses when none is
/// given.
/// </remarks>
public readonly struct FontWeight : IEquatable<FontWeight>
{
    // Stored relative to w400 so that `default` is the normal weight rather than an invalid zero.
    private readonly int _offsetFromNormal;

    /// <summary>Creates a font weight; <paramref name="value"/> must be between 1 and 1000.</summary>
    public FontWeight(int value)
    {
        if (Foundation.Constants.KDebugMode && (value < 1 || value > 1000))
        {
            throw new Foundation.AssertionError("Font weight must be between 1 and 1000");
        }

        _offsetFromNormal = value - 400;
    }

    /// <summary>The thickness value of this font weight, usually a multiple of 100.</summary>
    public int Value => _offsetFromNormal + 400;

    /// <summary>The encoded integer value of this font weight (0 for w100 through 8 for w900).</summary>
    [Obsolete("Use Value, which is more precise.")]
    public int Index => Math.Clamp(Value / 100 - 1, 0, 8);

    /// <summary>Thin, the least thick.</summary>
    public static FontWeight W100 => new(100);

    /// <summary>Extra-light.</summary>
    public static FontWeight W200 => new(200);

    /// <summary>Light.</summary>
    public static FontWeight W300 => new(300);

    /// <summary>Normal / regular / plain.</summary>
    public static FontWeight W400 => new(400);

    /// <summary>Medium.</summary>
    public static FontWeight W500 => new(500);

    /// <summary>Semi-bold.</summary>
    public static FontWeight W600 => new(600);

    /// <summary>Bold.</summary>
    public static FontWeight W700 => new(700);

    /// <summary>Extra-bold.</summary>
    public static FontWeight W800 => new(800);

    /// <summary>Black, the most thick.</summary>
    public static FontWeight W900 => new(900);

    /// <summary>The default font weight.</summary>
    public static FontWeight Normal => W400;

    /// <summary>A commonly used font weight that is heavier than normal.</summary>
    public static FontWeight Bold => W700;

    /// <summary>A list of all the font weights.</summary>
    public static IReadOnlyList<FontWeight> Values { get; } =
        [W100, W200, W300, W400, W500, W600, W700, W800, W900];

    /// <summary>
    /// Linearly interpolates between two font weights, rounding to an integer value clamped to
    /// w100..w900. A null end reads as <see cref="Normal"/>; two null ends give null.
    /// </summary>
    public static FontWeight? Lerp(FontWeight? a, FontWeight? b, double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        int start = (a ?? Normal).Value;
        int end = (b ?? Normal).Value;
        double value = start + (end - start) * t;
        return new FontWeight(Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 100, 900));
    }

    public static bool operator ==(FontWeight a, FontWeight b) => a.Value == b.Value;
    public static bool operator !=(FontWeight a, FontWeight b) => a.Value != b.Value;

    public bool Equals(FontWeight other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is FontWeight other && Equals(other);
    public override int GetHashCode() => Value;

    public override string ToString() => Value % 100 != 0
        ? $"FontWeight({Value})"
        : $"FontWeight.w{Math.Clamp(Value / 100, 1, 9) * 100}";
}
