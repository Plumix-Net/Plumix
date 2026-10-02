using Avalonia;
using Plumix.UI;
using Canvas = Plumix.UI.Canvas;

namespace Plumix.Rendering;

// C#-only infrastructure: no Dart parity source. Dart's `BoxPainter.paint`, `ShapeBorder.paint` and
// `CustomPainter.paint` take a `Canvas`; Plumix's take a `PaintingContext` (docs/ai/DIVERGENCES.md,
// the `Canvas`/`PaintingContext` row). Code that Dart hands only a canvas (`InkFeature.paintFeature`)
// wraps it in this context to call those painters: everything paints inline into the given canvas.

/// <summary>A <see cref="PaintingContext"/> whose drawing goes straight into an existing canvas.</summary>
internal sealed class CanvasPaintingContext(Canvas canvas) : PaintingContext(new OffsetLayer(), default)
{
    public override Canvas Canvas => canvas;

    public override void PaintChild(RenderObject child, Point offset) => child.Paint(this, offset);

    public override void PushLayer(
        ContainerLayer childLayer,
        PaintingContextCallback painter,
        Point offset,
        Rect? childPaintBounds = null)
    {
        painter(this, offset);
    }

    public override ClipRectLayer? PushClipRect(
        bool needsCompositing,
        Point offset,
        Rect clipRect,
        PaintingContextCallback painter,
        Clip clipBehavior = Clip.HardEdge,
        ClipRectLayer? oldLayer = null)
    {
        Rect shifted = clipRect.Translate(new Vector(offset.X, offset.Y));
        ClipRectAndPaint(shifted, clipBehavior, shifted, () => painter(this, offset));
        return null;
    }
}
