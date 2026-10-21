using System.Buffers.Binary;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Painting;

namespace Plumix.UI;

// C#-only infrastructure: SkParagraph Decorations.cpp at Flutter's pinned Skia revision
// 8df24be66531469e576a806749a0202ae26b8d08. Flutter uses the default Through mode (no skip-ink).
internal static class ParagraphDecorations
{
    internal static void Paint(Canvas canvas, ShapedTextRun run, ResolvedTextStyle style)
    {
        if (style.Decoration == TextDecoration.None)
        {
            return;
        }

        TextMetrics metrics = run.TextMetrics;
        double thickness = metrics.UnderlineThickness > 0 ? metrics.UnderlineThickness : style.FontSize / 14.0;
        if (style.Decoration == TextDecoration.LineThrough && metrics.StrikethroughThickness > 0)
        {
            thickness = metrics.StrikethroughThickness;
        }

        thickness *= style.DecorationThickness ?? 1.0;
        if (thickness <= 0)
        {
            return;
        }

        Color color = style.DecorationColor ?? style.Color;
        double baseline = run.GlyphRun.BaselineOrigin.Y;
        if (style.Decoration.HasFlag(TextDecoration.Underline))
        {
            Draw(baseline + (metrics.UnderlinePosition > 0 ? metrics.UnderlinePosition : thickness));
        }

        if (style.Decoration.HasFlag(TextDecoration.Overline))
        {
            Draw(baseline + metrics.Ascent);
        }

        if (style.Decoration.HasFlag(TextDecoration.LineThrough))
        {
            GlyphTypeface face = run.ShapedBuffer.GlyphTypeface;
            double position = face.PlatformTypeface.TryGetTable(0x4F532F32, out _)
                ? metrics.StrikethroughPosition : -XHeight(face, style.FontSize) / 2.0;
            Draw(baseline + position);
        }

        void Draw(double y)
        {
            double width = run.Size.Width;
            var fill = new Paint { Color = color };
            switch (style.DecorationStyle)
            {
                case TextDecorationStyle.Double:
                    Rectangle(0, width, y);
                    Rectangle(0, width, y + 3.0);
                    break;
                case TextDecorationStyle.Dashed:
                case TextDecorationStyle.Dotted:
                    double scale = style.FontSize / 14.0;
                    double segment = (style.DecorationStyle == TextDecorationStyle.Dotted ? 1.0 : 4.0) * scale;
                    double gap = (style.DecorationStyle == TextDecorationStyle.Dotted ? 1.5 : 2.0) * scale;
                    for (double x = 0; x < width; x += segment + gap)
                    {
                        Rectangle(x, Math.Min(segment, width - x), y);
                    }

                    break;
                case TextDecorationStyle.Wavy:
                    var path = new Path();
                    path.MoveTo(0, y);
                    double step = thickness * 2.0;
                    double sign = -1.0;
                    double cursor = 0;
                    while (cursor + step <= width)
                    {
                        path.QuadraticBezierTo(cursor + thickness, y + sign * thickness, cursor + step, y);
                        cursor += step;
                        sign = -sign;
                    }

                    double remainder = width - cursor;
                    if (remainder > 0)
                    {
                        path.QuadraticBezierTo(cursor + remainder / 2, y + sign * remainder / 2,
                            width, y + sign * (remainder - remainder * remainder / step));
                    }

                    canvas.DrawPath(path, new Paint
                    {
                        Color = color, Style = PaintingStyle.Stroke, StrokeWidth = thickness,
                    });
                    break;
                default:
                    Rectangle(0, width, y);
                    break;
            }

            void Rectangle(double x, double length, double centerY) =>
                canvas.DrawRect(new Rect(x, centerY - thickness / 2.0, length, thickness), fill);
        }
    }

    private static double XHeight(GlyphTypeface face, double size)
    {
        if (face.PlatformTypeface.TryGetTable(0x4F532F32, out ReadOnlyMemory<byte> os2) && os2.Length >= 88)
        {
            double height = BinaryPrimitives.ReadInt16BigEndian(os2.Span.Slice(86, 2));
            if (height > 0)
            {
                return height * size / face.Metrics.DesignEmHeight;
            }
        }

        using var glyph = new GlyphRun(face, size, "x".AsMemory(), new ushort[] { face.CharacterToGlyphMap['x'] });
        return glyph.InkBounds.Height;
    }
}

internal sealed class ParagraphEllipsis(string ellipsis) : TextTrimming
{
    public override TextCollapsingProperties CreateCollapsingProperties(TextCollapsingCreateInfo createInfo) =>
        new TextTrailingCharacterEllipsis(
            ellipsis, createInfo.Width, createInfo.TextRunProperties, createInfo.FlowDirection);
}
