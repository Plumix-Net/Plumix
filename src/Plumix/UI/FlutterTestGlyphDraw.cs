using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Plumix.UI;

// C#-only backend bridge: draws HeadlessParagraph lines with Skia and the engine's own `FlutterTest`
// font (`kFlutterTestFont` in flutter/runtime/test_font_data.cc, embedded as Assets/FlutterTest.ttf), so
// glyph positioning (quarter-pixel x, whole-pixel baselines) and coverage match what flutter_tester paints.
// The operations follow SkParagraph's `TextLine::paint` (modules/skparagraph/src/TextLine.cpp): backgrounds,
// shadows, glyphs, then decorations (`Decorations.cpp`), each drawn the way Flutter's
// `DisplayListParagraphPainter` (txt/src/skia/paragraph_skia.cc) hands them to Skia.
internal sealed class FlutterTestGlyphDraw(IReadOnlyList<FlutterTestGlyphDraw.Operation> operations, Rect bounds)
    : ICustomDrawOperation
{
    // Decorations.cpp: kDoubleDecorationSpacing.
    private const float DoubleDecorationSpacing = 3.0f;

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
    internal abstract record Operation;

    /// <summary>`TextLine::paintBackground`: the run's clip rectangle filled with the background paint.</summary>
    internal sealed record BackgroundOperation(Rect Rect, Color Color) : Operation;

    /// <summary>
    /// Glyph runs (`TextBlobRecord::paint`), or a text shadow (`paintShadow`) when <see cref="Shadow"/> is set:
    /// the same glyphs offset, recolored and blurred with a normal blur mask filter of the shadow's sigma.
    /// </summary>
    internal sealed record GlyphsOperation(IReadOnlyList<Glyph> Glyphs, TextShadowDraw? Shadow = null) : Operation;

    /// <summary>A text shadow's offset, color and blur sigma.</summary>
    internal readonly record struct TextShadowDraw(Point Offset, Color Color, double BlurSigma);

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
        Color Color) : Operation;

    private static readonly Lazy<SKTypeface?> Typeface = new(LoadTypeface);

    public Rect Bounds => bounds;

    public bool HitTest(Point point) => false;

    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);

    public void Dispose()
    {
    }

    public void Render(ImmediateDrawingContext context)
    {
        ISkiaSharpApiLeaseFeature? feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        SKTypeface? typeface = Typeface.Value;
        if (feature is null || typeface is null)
        {
            RenderWithoutSkia(context);
            return;
        }

        using ISkiaSharpApiLease lease = feature.Lease();
        SKCanvas canvas = lease.SkCanvas;
        foreach (Operation operation in operations)
        {
            switch (operation)
            {
                case BackgroundOperation background:
                    using (var paint = new SKPaint { IsAntialias = true, Color = ToSk(background.Color) })
                    {
                        canvas.DrawRect(ToSk(background.Rect), paint);
                    }

                    break;
                case GlyphsOperation run:
                    DrawGlyphs(canvas, typeface, run);
                    break;
                case DecorationOperation decoration:
                    DrawDecoration(canvas, typeface, decoration);
                    break;
            }
        }
    }

    private void RenderWithoutSkia(ImmediateDrawingContext context)
    {
        foreach (Operation operation in operations)
        {
            switch (operation)
            {
                case BackgroundOperation background:
                    context.FillRectangle(new ImmutableSolidColorBrush(background.Color), background.Rect);
                    break;
                case GlyphsOperation { Shadow: null } run:
                    foreach (Glyph glyph in run.Glyphs)
                    {
                        context.FillRectangle(
                            new ImmutableSolidColorBrush(glyph.Color),
                            new Rect(
                                glyph.X,
                                glyph.Baseline - (glyph.FontSize * 0.75),
                                glyph.FontSize,
                                glyph.FontSize));
                    }

                    break;
            }
        }
    }

    private static void DrawGlyphs(SKCanvas canvas, SKTypeface typeface, GlyphsOperation run)
    {
        IReadOnlyList<Glyph> glyphs = run.Glyphs;
        // SkParagraph's TextStyle font settings.
        using var font = new SKFont(typeface)
        {
            Subpixel = true,
            Edging = SKFontEdging.Antialias,
            Hinting = SKFontHinting.Slight,
        };
        using var paint = new SKPaint { IsAntialias = true };
        using SKMaskFilter? blur = run.Shadow is { BlurSigma: > 0.0 } blurred
            ? SKMaskFilter.CreateBlur(SKBlurStyle.Normal, (float)blurred.BlurSigma, false)
            : null;
        paint.MaskFilter = blur;
        float dx = (float)(run.Shadow?.Offset.X ?? 0.0);
        float dy = (float)(run.Shadow?.Offset.Y ?? 0.0);
        int start = 0;
        while (start < glyphs.Count)
        {
            Glyph first = glyphs[start];
            int end = start + 1;
            while (end < glyphs.Count
                   && glyphs[end].FontSize == first.FontSize
                   && glyphs[end].Color == first.Color
                   && glyphs[end].FakeBold == first.FakeBold
                   && glyphs[end].FakeItalic == first.FakeItalic)
            {
                end++;
            }

            font.Size = (float)first.FontSize;
            // OneLineShaper::shape: font.setEmbolden(fakeBold); font.setSkewX(fakeItalic ? -SK_Scalar1 / 4 : 0).
            font.Embolden = first.FakeBold;
            font.SkewX = first.FakeItalic ? -0.25f : 0.0f;
            paint.Color = ToSk(run.Shadow?.Color ?? first.Color);
            using var builder = new SKTextBlobBuilder();
            SKPositionedRunBuffer buffer = builder.AllocatePositionedRun(font, end - start);
            Span<ushort> ids = buffer.GetGlyphSpan();
            Span<SKPoint> positions = buffer.GetPositionSpan();
            for (int index = start; index < end; index++)
            {
                Glyph glyph = glyphs[index];
                ids[index - start] = font.GetGlyph(glyph.CodePoint);
                positions[index - start] = new SKPoint((float)glyph.X, (float)glyph.Baseline);
            }

            using SKTextBlob? blob = builder.Build();
            if (blob is not null)
            {
                canvas.DrawText(blob, dx, dy, paint);
            }

            start = end;
        }
    }

    private static void DrawDecoration(SKCanvas canvas, SKTypeface typeface, DecorationOperation decoration)
    {
        using var font = new SKFont(typeface, (float)decoration.FontSize);
        SKFontMetrics metrics = font.Metrics;
        float fontSize = (float)decoration.FontSize;
        float correctAscent = (float)-decoration.CorrectAscent;
        float rawAscent = (float)-decoration.RawAscent;
        SKRect clip = ToSk(decoration.Clip);

        // Decorations::calculateThickness.
        float thickness = fontSize / 14.0f;
        if (metrics.UnderlineThickness is > 0.0f and float underlineThickness)
        {
            thickness = underlineThickness;
        }

        if (decoration.Decoration == TextDecoration.LineThrough
            && metrics.StrikeoutThickness is > 0.0f and float strikeoutThickness)
        {
            thickness = strikeoutThickness;
        }

        thickness *= (float)decoration.ThicknessMultiplier;
        foreach (TextDecoration kind in (ReadOnlySpan<TextDecoration>)
                 [TextDecoration.Underline, TextDecoration.Overline, TextDecoration.LineThrough])
        {
            if ((decoration.Decoration & kind) == 0)
            {
                continue;
            }

            // Decorations::calculatePosition.
            float ascent = kind == TextDecoration.Overline ? correctAscent - rawAscent : correctAscent;
            float position = kind switch
            {
                TextDecoration.Underline => metrics.UnderlinePosition is > 0.0f and float underline
                    ? underline
                    : thickness,
                TextDecoration.Overline => 0.0f,
                _ => metrics.StrikeoutPosition ?? metrics.XHeight / -2.0f,
            };
            position -= ascent;
            float x = clip.Left;
            float y = clip.Top + position;
            float width = clip.Width;

            // Decorations::calculatePaint; DisplayListParagraphPainter::toDlPaint.
            float scaleFactor = fontSize / 14.0f;
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = ToSk(decoration.Color),
                StrokeWidth = thickness,
                Style = SKPaintStyle.Stroke,
            };
            switch (decoration.Style)
            {
                case TextDecorationStyle.Wavy:
                    using (SKPath waves = CalculateWaves(thickness, width))
                    {
                        waves.Offset(x, y);
                        canvas.DrawPath(waves, paint);
                    }

                    break;
                case TextDecorationStyle.Double:
                    paint.Style = SKPaintStyle.Fill;
                    DrawLineAsRect(canvas, x, y, width, thickness, paint);
                    DrawLineAsRect(canvas, x, y + DoubleDecorationSpacing, width, thickness, paint);
                    break;
                case TextDecorationStyle.Dashed:
                case TextDecorationStyle.Dotted:
                    // DlSkCanvasDispatcher::drawDashedLine: a dash path effect with phase 0.
                    float[] intervals = decoration.Style == TextDecorationStyle.Dotted
                        ? [1.0f * scaleFactor, 1.5f * scaleFactor]
                        : [4.0f * scaleFactor, 2.0f * scaleFactor];
                    using (var dash = SKPathEffect.CreateDash(intervals, 0.0f))
                    {
                        paint.PathEffect = dash;
                        canvas.DrawLine(x, y, x + width, y, paint);
                        paint.PathEffect = null;
                    }

                    break;
                default:
                    paint.Style = SKPaintStyle.Fill;
                    DrawLineAsRect(canvas, x, y, width, thickness, paint);
                    break;
            }
        }
    }

    // Decorations.cpp: draw_line_as_rect.
    private static void DrawLineAsRect(SKCanvas canvas, float x, float y, float width, float thickness, SKPaint paint)
    {
        float radius = thickness * 0.5f;
        canvas.DrawRect(new SKRect(x, y - radius, x + width, y + radius), paint);
    }

    // Decorations::calculateWaves.
    private static SKPath CalculateWaves(float thickness, float width)
    {
        var path = new SKPath();
        int waveCount = 0;
        float xStart = 0.0f;
        float quarterWave = thickness;
        path.MoveTo(0.0f, 0.0f);
        while (xStart + (quarterWave * 2.0f) < width)
        {
            path.RQuadTo(quarterWave, waveCount % 2 != 0 ? quarterWave : -quarterWave, quarterWave * 2.0f, 0.0f);
            xStart += quarterWave * 2.0f;
            ++waveCount;
        }

        float remaining = width - xStart;
        if (remaining > 0.0f)
        {
            double sign = waveCount % 2 == 0 ? -1.0 : 1.0;
            double x1 = remaining / 2.0;
            double y1 = remaining / 2.0 * sign;
            double x2 = remaining;
            double y2 = (remaining - (remaining * remaining / (quarterWave * 2.0))) * sign;
            path.RQuadTo((float)x1, (float)y1, (float)x2, (float)y2);
        }

        return path;
    }

    private static SKColor ToSk(Color color) => new(color.ToARGB32());

    private static SKRect ToSk(Rect rect) =>
        new((float)rect.Left, (float)rect.Top, (float)rect.Right, (float)rect.Bottom);

    private static SKTypeface? LoadTypeface()
    {
        using Stream? stream = typeof(FlutterTestGlyphDraw).Assembly
            .GetManifestResourceStream("Plumix.UI.Assets.FlutterTest.ttf");
        return stream is null ? null : SKTypeface.FromStream(stream);
    }
}
