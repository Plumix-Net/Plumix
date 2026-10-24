using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Plumix.UI;

// C#-only backend bridge: draws HeadlessParagraph glyphs with Skia and the engine's own `FlutterTest`
// font (`kFlutterTestFont` in flutter/runtime/test_font_data.cc, embedded as Assets/FlutterTest.ttf), so
// glyph positioning (quarter-pixel x, whole-pixel baselines) and coverage match what flutter_tester paints.
internal sealed class FlutterTestGlyphDraw(IReadOnlyList<FlutterTestGlyphDraw.Glyph> glyphs, Rect bounds)
    : ICustomDrawOperation
{
    /// <summary>One code point at its pen position on the baseline.</summary>
    internal readonly record struct Glyph(int CodePoint, double X, double Baseline, double FontSize, Color Color);

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
            foreach (Glyph glyph in glyphs)
            {
                context.FillRectangle(
                    new Avalonia.Media.Immutable.ImmutableSolidColorBrush(glyph.Color),
                    new Rect(glyph.X, glyph.Baseline - (glyph.FontSize * 0.75), glyph.FontSize, glyph.FontSize));
            }

            return;
        }

        using ISkiaSharpApiLease lease = feature.Lease();
        // SkParagraph's TextStyle font settings.
        using var font = new SKFont(typeface)
        {
            Subpixel = true,
            Edging = SKFontEdging.Antialias,
            Hinting = SKFontHinting.Slight,
        };
        using var paint = new SKPaint { IsAntialias = true };
        int start = 0;
        while (start < glyphs.Count)
        {
            Glyph first = glyphs[start];
            int end = start + 1;
            while (end < glyphs.Count
                   && glyphs[end].FontSize == first.FontSize
                   && glyphs[end].Color == first.Color)
            {
                end++;
            }

            font.Size = (float)first.FontSize;
            paint.Color = new SKColor(first.Color.ToARGB32());
            using var builder = new SKTextBlobBuilder();
            SKPositionedRunBuffer run = builder.AllocatePositionedRun(font, end - start);
            Span<ushort> ids = run.GetGlyphSpan();
            Span<SKPoint> positions = run.GetPositionSpan();
            for (int index = start; index < end; index++)
            {
                Glyph glyph = glyphs[index];
                ids[index - start] = font.GetGlyph(glyph.CodePoint);
                positions[index - start] = new SKPoint((float)glyph.X, (float)glyph.Baseline);
            }

            using SKTextBlob? blob = builder.Build();
            if (blob is not null)
            {
                lease.SkCanvas.DrawText(blob, 0, 0, paint);
            }

            start = end;
        }
    }

    private static SKTypeface? LoadTypeface()
    {
        using Stream? stream = typeof(FlutterTestGlyphDraw).Assembly
            .GetManifestResourceStream("Plumix.UI.Assets.FlutterTest.ttf");
        return stream is null ? null : SKTypeface.FromStream(stream);
    }
}
