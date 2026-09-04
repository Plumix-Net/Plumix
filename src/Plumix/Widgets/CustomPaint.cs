using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (CustomPaint)

public sealed class CustomPaint : SingleChildRenderObjectWidget
{
    public CustomPaint(
        CustomPainter? painter = null,
        CustomPainter? foregroundPainter = null,
        Size size = default,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        Painter = painter;
        ForegroundPainter = foregroundPainter;
        Size = size;
    }

    public CustomPainter? Painter { get; }
    public CustomPainter? ForegroundPainter { get; }
    public Size Size { get; }

    public override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderCustomPaint(Painter, ForegroundPainter, Size);

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var customPaint = (RenderCustomPaint)renderObject;
        customPaint.Painter = Painter;
        customPaint.ForegroundPainter = ForegroundPainter;
        customPaint.PreferredSize = Size;
    }
}
