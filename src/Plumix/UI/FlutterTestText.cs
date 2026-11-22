// C#-only infrastructure; no Dart parity source. What HeadlessParagraph records for its lines, as framework
// data: the paint steps of SkParagraph's `TextLine::paint` (modules/skparagraph/src/TextLine.cpp) —
// backgrounds, shadows, glyphs, then decorations (`Decorations.cpp`) — over the engine's own `FlutterTest`
// font (`kFlutterTestFont` in flutter/runtime/test_font_data.cc, embedded as Assets/FlutterTest.ttf). The
// backend draws them the way Flutter's `DisplayListParagraphPainter` (txt/src/skia/paragraph_skia.cc) hands
// them to Skia; Plumix.Avalonia's is FlutterTestGlyphDraw, played by CanvasPlayback
// (docs/product avalonia-free-core.md, stage 3).

namespace Plumix.UI;

/// <summary>The paint steps of a headless paragraph and the `FlutterTest` font's glyph shapes.</summary>
internal static class FlutterTestText
{
    /// <summary>The `FlutterTest` font's ascent, as a fraction of the font size (768 of 1024 units).</summary>
    internal const double AscentRatio = 0.75;

    /// <summary>The `FlutterTest` font's descent, as a fraction of the font size (256 of 1024 units).</summary>
    internal const double DescentRatio = 0.25;

    /// <summary>The outline a code point has in the `FlutterTest` font.</summary>
    internal enum GlyphShape
    {
        /// <summary>A solid box one em wide from the ascent to the descent.</summary>
        Box,

        /// <summary>Only the box's descent part (`p`).</summary>
        DescentOnly,

        /// <summary>Only the box's ascent part (`É`).</summary>
        AscentOnly,

        /// <summary>The `.notdef` frame of a code point the font does not cover.</summary>
        NotDef,
    }

    /// <summary>
    /// One code point at its pen position on the baseline. <paramref name="FakeBold"/> and
    /// <paramref name="FakeItalic"/> are SkParagraph's synthetic styles for a typeface (here the regular,
    /// upright `FlutterTest`) that lacks the requested weight or slant.
    /// </summary>
    internal readonly record struct Glyph(
        int CodePoint,
        double X,
        double Baseline,
        double FontSize,
        Color Color,
        bool FakeBold = false,
        bool FakeItalic = false);

    /// <summary>One paint step of a paragraph line.</summary>
    internal union Operation(BackgroundOperation, GlyphsOperation, DecorationOperation);

    /// <summary>`TextLine::paintBackground`: the run's clip rectangle filled with the background paint.</summary>
    internal sealed record BackgroundOperation(Rect Rect, Color Color);

    /// <summary>
    /// Glyph runs (`TextBlobRecord::paint`), or a text shadow (`paintShadow`) when <see cref="Shadow"/> is set:
    /// the same glyphs offset, recolored and blurred with a normal blur mask filter of the shadow's sigma.
    /// </summary>
    internal sealed record GlyphsOperation(IReadOnlyList<Glyph> Glyphs, TextShadowDraw? Shadow = null);

    /// <summary>A text shadow's offset, color and blur sigma.</summary>
    internal readonly record struct TextShadowDraw(Offset Offset, Color Color, double BlurSigma);

    /// <summary>
    /// `Decorations::paint` for one style run. <see cref="Clip"/> is the run's clip rectangle (`ClipContext.clip`
    /// in paragraph coordinates); <see cref="CorrectAscent"/> and <see cref="RawAscent"/> are the run's
    /// height-adjusted and font ascents as positive distances above the baseline.
    /// </summary>
    internal sealed record DecorationOperation(
        Rect Clip,
        double CorrectAscent,
        double RawAscent,
        double FontSize,
        TextDecoration Decoration,
        TextDecorationStyle Style,
        double ThicknessMultiplier,
        Color Color);

    /// <summary>Consecutive operations the backend draws as one batch.</summary>
    internal sealed record GlyphBatch(IReadOnlyList<Operation> Operations);

    /// <summary>
    /// A glyph painted with a shader: the glyph's <paramref name="Shape"/> at <paramref name="X"/> on
    /// <paramref name="Baseline"/>, <paramref name="Width"/> wide, at <paramref name="FontSize"/>, filled with
    /// <paramref name="Shader"/>.
    /// </summary>
    internal sealed record ShaderGlyph(
        GlyphShape Shape,
        double X,
        double Width,
        double Baseline,
        double FontSize,
        Shader Shader);

    /// <summary>One step of a <see cref="FlutterTestTextOp"/>, drawn in order.</summary>
    internal union Step(GlyphBatch, ShaderGlyph);
}

/// <summary>
/// Plumix-only: the lines of a laid-out HeadlessParagraph, as the ordered <see cref="Steps"/> its
/// <c>TextLine::paint</c> produced, with the <see cref="Bounds"/> they draw within.
/// </summary>
internal sealed class FlutterTestTextOp(IReadOnlyList<FlutterTestText.Step> steps, Rect bounds) : CanvasOp
{
    internal IReadOnlyList<FlutterTestText.Step> Steps { get; } = steps;

    /// <summary>The steps' paint bounds, inflated by a pixel.</summary>
    internal Rect Bounds { get; } = bounds;
}
