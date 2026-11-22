namespace Plumix.UI;

// C#-only infrastructure; no Dart parity source. SkParagraph lays text out over shaped runs of real fonts
// (OneLineShaper over HarfBuzz). HeadlessParagraph's line breaking, bidi and metrics work over any run
// shaping: without a shaper it shapes with the `FlutterTest` font's fixed metrics, and a host text engine
// that has its own fonts and shaper (the Impeller prototype's HarfBuzz, docs/product avalonia-free-core.md
// stage 4) hands one in through this seam. The faces are opaque to the core.

/// <summary>A font face a text shaper resolved a style to.</summary>
/// <param name="Typeface">The backend's typeface, drawn by its canvas player.</param>
/// <param name="Ascent">The face's ascent above the baseline, as a fraction of the font size.</param>
/// <param name="Descent">The face's descent below the baseline, as a fraction of the font size.</param>
/// <param name="FakeBold">Whether the requested weight is synthesized (SkParagraph's fake bold).</param>
/// <param name="FakeItalic">Whether the requested slant is synthesized (SkParagraph's fake italic).</param>
internal sealed record ShapingFace(object Typeface, double Ascent, double Descent, bool FakeBold, bool FakeItalic);

/// <summary>One shaped glyph: the code unit its cluster starts at, its glyph id, advance and offset.</summary>
/// <remarks>Advances and offsets are in logical pixels at the style's font size; y offsets grow upward.</remarks>
internal readonly record struct ShapedGlyphInfo(
    int Cluster,
    int GlyphId,
    double Advance,
    double OffsetX,
    double OffsetY,
    ShapingFace Face);

/// <summary>A host's font resolution and run shaping, for HeadlessParagraph.</summary>
internal interface ITextShaper
{
    /// <summary>The face <paramref name="style"/> resolves to (its primary family, weight and slant).</summary>
    ShapingFace Face(ResolvedTextStyle style);

    /// <summary>
    /// Shapes <paramref name="text"/>[<paramref name="start"/>, <paramref name="end"/>) in
    /// <paramref name="style"/>, with the whole text as shaping context; the glyphs come in ascending
    /// cluster order, falling back to other faces for code points the primary face lacks.
    /// </summary>
    IReadOnlyList<ShapedGlyphInfo> Shape(string text, int start, int end, ResolvedTextStyle style);
}
