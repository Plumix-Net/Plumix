using System.Runtime.CompilerServices;
using NImpeller;
using Plumix.Rendering;
using Plumix.UI;
using Path = Plumix.UI.Path;
using static Plumix.UI.FlutterTestText;

namespace Plumix.Impeller;

// C#-only infrastructure: no Dart parity source. Draws HeadlessParagraph's recorded FlutterTest lines
// (FlutterTestText.cs): glyphs a host shaper placed (ImpellerFonts.cs) as Impeller glyph runs, FlutterTest
// glyphs as the font's box outlines, plus backgrounds, shadows and solid decorations. Stage 4 prototype:
// shader-filled glyphs are not drawn yet.

/// <summary>Draws <see cref="FlutterTestTextOp"/> entries with Impeller rectangles.</summary>
internal static class ImpellerFlutterTestText
{
    internal static void Draw(ImpellerDisplayListBuilder builder, FlutterTestTextOp text)
    {
        foreach (Step step in text.Steps)
        {
            switch (step)
            {
                case GlyphBatch batch:
                    foreach (Operation operation in batch.Operations)
                    {
                        DrawOperation(builder, operation);
                    }

                    break;
                default:
                    ImpellerPaints.Unsupported("shader-filled FlutterTest glyphs");
                    break;
            }
        }
    }

    private static void DrawOperation(ImpellerDisplayListBuilder builder, Operation operation)
    {
        switch (operation)
        {
            case BackgroundOperation background:
                using (ImpellerPaint paint = ImpellerPaints.Solid(background.Color))
                {
                    builder.DrawRect(background.Rect.ToImpeller(), paint);
                }

                break;
            case GlyphsOperation { Shadow: null } glyphs:
                foreach ((ImpellerGlyphRun run, Color color) in RunsOf(glyphs))
                {
                    using ImpellerPaint paint = ImpellerPaints.Solid(color);
                    builder.DrawGlyphRun(run, default, paint);
                }

                foreach (Glyph glyph in glyphs.Glyphs)
                {
                    if (glyph.Typeface is null)
                    {
                        using ImpellerPaint paint = ImpellerPaints.Solid(glyph.Color);
                        DrawGlyph(builder, HeadlessParagraph.ShapeOf(glyph.CodePoint), glyph, paint);
                    }
                }

                break;
            case GlyphsOperation { Shadow: { } shadow } glyphs:
                using (ImpellerPaint paint = ImpellerPaints.Solid(shadow.Color))
                {
                    if (shadow.BlurSigma > 0
                        && ImpellerMaskFilter.CreateBlurNew(ImpellerBlurStyle.kImpellerBlurStyleNormal,
                            (float)shadow.BlurSigma) is { } blur)
                    {
                        paint.SetMaskFilter(blur);
                    }

                    builder.Save();
                    builder.Translate((float)shadow.Offset.Dx, (float)shadow.Offset.Dy);
                    foreach ((ImpellerGlyphRun run, Color _) in RunsOf(glyphs))
                    {
                        builder.DrawGlyphRun(run, default, paint);
                    }

                    builder.Restore();
                }

                break;
            case DecorationOperation decoration:
                DrawDecoration(builder, decoration);
                break;
        }
    }

    // Glyph runs are built once per recorded operation: retained pictures replay them every frame.
    private static readonly ConditionalWeakTable<GlyphsOperation, List<(ImpellerGlyphRun, Color)>> s_runs = new();

    /// <summary>Consecutive glyphs of one face, size, colour and synthetic style, as Impeller glyph runs.</summary>
    private static List<(ImpellerGlyphRun Run, Color Color)> RunsOf(GlyphsOperation operation) =>
        s_runs.GetValue(operation, static operation =>
        {
            var runs = new List<(ImpellerGlyphRun, Color)>();
            IReadOnlyList<Glyph> glyphs = operation.Glyphs;
            int index = 0;
            while (index < glyphs.Count)
            {
                if (glyphs[index].Typeface is not ImpellerFontFace face)
                {
                    index++;
                    continue;
                }

                Glyph first = glyphs[index];
                int end = index;
                while (end < glyphs.Count && ReferenceEquals(glyphs[end].Typeface, face)
                       && glyphs[end].FontSize == first.FontSize && glyphs[end].Color == first.Color
                       && glyphs[end].FakeBold == first.FakeBold && glyphs[end].FakeItalic == first.FakeItalic)
                {
                    end++;
                }

                ushort[] ids = new ushort[end - index];
                var positions = new ImpellerPoint[end - index];
                for (int i = index; i < end; i++)
                {
                    ids[i - index] = (ushort)glyphs[i].GlyphId;
                    positions[i - index] = new ImpellerPoint { X = (float)glyphs[i].X, Y = (float)glyphs[i].Baseline };
                }

                // SkParagraph's synthetic styles: emboldening and Skia's -1/4 skew.
                ImpellerGlyphRunOptions? options = first.FakeBold || first.FakeItalic
                    ? new ImpellerGlyphRunOptions
                    {
                        Edging = ImpellerFontEdging.kImpellerFontEdgingAntiAlias,
                        Hinting = ImpellerFontHinting.kImpellerFontHintingSlight,
                        Embolden = first.FakeBold ? 1 : 0,
                        Skew_x = first.FakeItalic ? -0.25f : 0f,
                    }
                    : null;
                if (face.Typeface.GlyphRunNew((float)first.FontSize, options, ids, positions) is { } run)
                {
                    runs.Add((run, first.Color));
                }

                index = end;
            }

            return runs;
        });

    // Decorations::paint, approximated: solid lines at fixed proportions of the font size (the real face's
    // underline metrics are not read yet); wavy, dashed, dotted and double lines draw solid.
    private static void DrawDecoration(ImpellerDisplayListBuilder builder, DecorationOperation decoration)
    {
        double baseline = decoration.Clip.Top + decoration.CorrectAscent;
        double thickness = Math.Max(1.0, decoration.FontSize / 14.0) * decoration.ThicknessMultiplier;
        using ImpellerPaint paint = ImpellerPaints.Solid(decoration.Color);
        void Line(double y) => builder.DrawRect(
            Rect.FromLTWH(decoration.Clip.Left, y - (thickness / 2), decoration.Clip.Width, thickness).ToImpeller(),
            paint);
        if (decoration.Decoration.HasFlag(TextDecoration.Underline))
        {
            Line(baseline + (decoration.FontSize * 0.1));
        }

        if (decoration.Decoration.HasFlag(TextDecoration.Overline))
        {
            Line(baseline - decoration.RawAscent);
        }

        if (decoration.Decoration.HasFlag(TextDecoration.LineThrough))
        {
            Line(baseline - (decoration.RawAscent * 0.35));
        }

        if (decoration.Style != TextDecorationStyle.Solid)
        {
            ImpellerPaints.Unsupported($"{decoration.Style} text decorations");
        }
    }

    private static void DrawGlyph(
        ImpellerDisplayListBuilder builder, GlyphShape shape, Glyph glyph, ImpellerPaint paint)
    {
        double size = glyph.FontSize;
        double ascent = size * AscentRatio;
        double descent = size * DescentRatio;
        switch (shape)
        {
            case GlyphShape.DescentOnly:
                builder.DrawRect(Rect.FromLTWH(glyph.X, glyph.Baseline, size, descent).ToImpeller(), paint);
                break;
            case GlyphShape.AscentOnly:
                builder.DrawRect(Rect.FromLTWH(glyph.X, glyph.Baseline - ascent, size, ascent).ToImpeller(), paint);
                break;
            case GlyphShape.NotDef:
                double unit = size / 1024.0;
                double x = glyph.X;
                double y = glyph.Baseline;
                var outer = Rect.FromLTWH(x + (128 * unit), y - (768 * unit), 768 * unit, 768 * unit);
                var inner = Rect.FromLTWH(x + (256 * unit), y - (640 * unit), 512 * unit, 512 * unit);
                builder.DrawRoundedRectDifference(outer.ToImpeller(), default, inner.ToImpeller(), default, paint);
                break;
            default:
                builder.DrawRect(
                    Rect.FromLTWH(glyph.X, glyph.Baseline - ascent, size, ascent + descent).ToImpeller(), paint);
                break;
        }
    }
}
