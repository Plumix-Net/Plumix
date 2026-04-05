using Avalonia;
using Avalonia.Media;
using Flutter.Rendering;
using Flutter.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/cupertino/checkbox.dart (adapted primitives)

namespace Flutter;

public sealed class RenderStrokeGlyph : RenderBox
{
    private StrokeGlyphKind _kind;
    private Color _color;
    private double _glyphSize;

    public RenderStrokeGlyph(StrokeGlyphKind kind, Color color, double glyphSize)
    {
        _kind = kind;
        _color = color;
        _glyphSize = Math.Max(0, glyphSize);
    }

    public StrokeGlyphKind Kind
    {
        get => _kind;
        set
        {
            if (_kind == value)
            {
                return;
            }

            _kind = value;
            MarkNeedsPaint();
        }
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (_color == value)
            {
                return;
            }

            _color = value;
            MarkNeedsPaint();
        }
    }

    public double GlyphSize
    {
        get => _glyphSize;
        set
        {
            var normalized = Math.Max(0, value);
            if (Math.Abs(_glyphSize - normalized) < 0.0001)
            {
                return;
            }

            _glyphSize = normalized;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        Size = Constraints.Constrain(new Size(GlyphSize, GlyphSize));
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        var glyphExtent = Math.Min(Size.Width, Size.Height);
        if (glyphExtent <= 0.001)
        {
            return;
        }

        var dx = offset.X + ((Size.Width - glyphExtent) / 2.0);
        var dy = offset.Y + ((Size.Height - glyphExtent) / 2.0);
        var pen = new Pen(new SolidColorBrush(Color), 2.0, lineCap: PenLineCap.Round);

        switch (Kind)
        {
            case StrokeGlyphKind.Check:
                DrawCheck(ctx, dx, dy, glyphExtent, pen);
                break;
            case StrokeGlyphKind.Dash:
                DrawDash(ctx, dx, dy, glyphExtent, pen);
                break;
        }
    }

    private static void DrawCheck(PaintingContext ctx, double dx, double dy, double extent, Pen pen)
    {
        var start = new Point(dx + (extent * 0.22), dy + (extent * 0.54));
        var mid = new Point(dx + (extent * 0.40), dy + (extent * 0.75));
        var end = new Point(dx + (extent * 0.78), dy + (extent * 0.25));

        ctx.DrawLine(pen, start, mid);
        ctx.DrawLine(pen, mid, end);
    }

    private static void DrawDash(PaintingContext ctx, double dx, double dy, double extent, Pen pen)
    {
        var start = new Point(dx + (extent * 0.25), dy + (extent * 0.50));
        var end = new Point(dx + (extent * 0.75), dy + (extent * 0.50));
        ctx.DrawLine(pen, start, end);
    }
}
