using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source (reference): flutter/packages/flutter/lib/src/cupertino/checkbox.dart (adapted primitives)

namespace Plumix.Widgets;

public enum StrokeGlyphKind
{
    Check,
    Dash
}

public sealed class StrokeGlyph : LeafRenderObjectWidget
{
    public StrokeGlyph(
        StrokeGlyphKind kind,
        Color color,
        double size,
        Key? key = null) : base(key)
    {
        Kind = kind;
        Color = color;
        Size = Math.Max(0, size);
    }

    public StrokeGlyphKind Kind { get; }

    public Color Color { get; }

    public double Size { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderStrokeGlyph(Kind, Color, Size);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var glyph = (RenderStrokeGlyph)renderObject;
        glyph.Kind = Kind;
        glyph.Color = Color;
        glyph.GlyphSize = Size;
    }
}
