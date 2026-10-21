using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using RunGlyph = Avalonia.Media.TextFormatting.GlyphInfo;

namespace Plumix.UI;

// C#-only infrastructure: Avalonia resolves script/bidi/fallback runs first, then each resolved run
// is shaped with its own spacing, features and variable instance before wrapping. Vertical paragraph
// metrics belong to AvaloniaParagraph, not to the host formatter's paragraph-wide line height.
internal sealed class ParagraphShaping : ITextSource, IDisposable
{
    private readonly ParagraphContent _content;
    private readonly ParagraphTextSource _source;
    private readonly List<PreparedRun> _runs = [];
    private readonly Dictionary<(GlyphTypeface, ResolvedTextStyle), VariableFont> _variableFonts = [];

    internal ParagraphShaping(ParagraphContent content, ParagraphTextSource source)
    {
        _content = content;
        _source = source;
        using var natural = new TextLayout(source, source.CreateParagraphProperties(TextAlignment.Left));
        foreach (TextLine line in natural.TextLines)
        {
            foreach (ShapedTextRun run in line.TextRuns.OfType<ShapedTextRun>())
            {
                if (!MemoryMarshal.TryGetString(run.Text, out string? original, out int start, out int length)
                    || !ReferenceEquals(original, content.Text) || length == 0)
                {
                    continue;
                }

                ResolvedTextStyle style = content.StyleAt(start);
                GlyphTypeface face = run.ShapedBuffer.GlyphTypeface;
                VariableFont? variable = Variable(face, style);
                RunGlyph[] glyphs;
                bool reshape = variable is not null || style.FontFeatures is { Count: > 0 }
                    || content.Runs.Any(item => item.Style.FontFeatures is { Count: > 0 });
                if (reshape)
                {
                    using ShapedBuffer buffer = variable is null
                        ? TextShaper.Current.ShapeText(run.Text, new TextShaperOptions(
                            face, style.FontSize, run.BidiLevel, run.Properties.CultureInfo,
                            fontFeatures: run.Properties.FontFeatures))
                        : variable.Shape(content.Text, start, run.Length, face, style, run.BidiLevel);
                    glyphs = buffer.ToArray();
                }
                else
                {
                    // Preserve ligatures/kerning across paint-only style boundaries. The formatter
                    // already shaped these together; another shaping pass would break their context.
                    glyphs = run.ShapedBuffer.ToArray();
                    if (run.IsReversed)
                    {
                        Array.Reverse(glyphs);
                    }
                }

                for (int index = 0; index < glyphs.Length; index++)
                {
                    RunGlyph glyph = glyphs[index];
                    bool lastInCluster = index == glyphs.Length - 1
                                         || glyphs[index + 1].GlyphCluster != glyph.GlyphCluster;
                    if (glyph.GlyphCluster < content.Text.Length && content.Text[glyph.GlyphCluster] == '\t')
                    {
                        ushort space = face.CharacterToGlyphMap[' '];
                        face.TryGetHorizontalGlyphAdvance(space, out ushort advance);
                        glyph = new RunGlyph(space, glyph.GlyphCluster,
                            advance * style.FontSize / face.Metrics.DesignEmHeight, glyph.GlyphOffset);
                    }

                    double spacing = lastInCluster ? style.LetterSpacing : 0.0;
                    if (lastInCluster && glyph.GlyphCluster < content.Text.Length
                        && content.Text[glyph.GlyphCluster] is ' ' or '\t')
                    {
                        spacing += style.WordSpacing;
                    }

                    glyphs[index] = new RunGlyph(
                        glyph.GlyphIndex,
                        glyph.GlyphCluster,
                        glyph.GlyphAdvance + spacing,
                        glyph.GlyphOffset + new Vector(style.LetterSpacing / 2.0, 0));
                }

                _runs.Add(new PreparedRun(start, run.Length, glyphs, face, style.FontSize,
                    run.BidiLevel, run.Properties));
            }
        }

        _runs.Sort((left, right) => left.Start.CompareTo(right.Start));
    }

    internal static int SourceIndex(ReadOnlyMemory<char> text) =>
        MemoryMarshal.TryGetString(text, out _, out int start, out _) ? start : 0;

    internal ResolvedTextStyle Style(ShapedTextRun run) => _content.StyleAt(SourceIndex(run.Text));

    internal VariableFont? Variable(GlyphTypeface face, ResolvedTextStyle style)
    {
        if (!VariableFont.IsVariable(face))
        {
            return null;
        }

        var key = (face, style);
        if (!_variableFonts.TryGetValue(key, out VariableFont? font))
        {
            font = new VariableFont(face, style);
            _variableFonts.Add(key, font);
        }

        return font;
    }

    public TextRun? GetTextRun(int textSourceIndex)
    {
        foreach (PreparedRun run in _runs)
        {
            if (textSourceIndex < run.Start || textSourceIndex >= run.Start + run.Length)
            {
                continue;
            }

            RunGlyph[] glyphs = run.Glyphs.Where(glyph => glyph.GlyphCluster >= textSourceIndex).ToArray();
            var buffer = new ShapedBuffer(
                _content.Text.AsMemory(textSourceIndex, run.Start + run.Length - textSourceIndex),
                glyphs.Length, run.Face, run.FontSize, run.BidiLevel);
            for (int index = 0; index < glyphs.Length; index++)
            {
                buffer[index] = glyphs[index];
            }

            return new ShapedTextRun(buffer, run.Properties);
        }

        return _source.GetTextRun(textSourceIndex);
    }

    public void Dispose()
    {
        foreach (VariableFont font in _variableFonts.Values)
        {
            font.Dispose();
        }
    }

    private sealed record PreparedRun(
        int Start, int Length, RunGlyph[] Glyphs, GlyphTypeface Face, double FontSize,
        sbyte BidiLevel, TextRunProperties Properties);
}
