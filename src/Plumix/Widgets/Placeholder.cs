using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/placeholder.dart

internal sealed class PlaceholderPainter : CustomPainter
{
    public PlaceholderPainter(Color color, double strokeWidth)
    {
        Color = color;
        StrokeWidth = strokeWidth;
    }

    internal Color Color { get; }

    internal double StrokeWidth { get; }

    public override void Paint(PaintingContext context, Size size)
    {
        var pen = new Pen(new SolidColorBrush(Color), StrokeWidth);
        var rect = new Rect(size);
        context.DrawRectangle(Brushes.Transparent, pen, rect);
        context.DrawLine(pen, rect.TopRight, rect.BottomLeft);
        context.DrawLine(pen, rect.TopLeft, rect.BottomRight);
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        return oldDelegate is not PlaceholderPainter oldPainter
               || oldPainter.Color != Color
               || oldPainter.StrokeWidth != StrokeWidth;
    }

    public override bool? HitTest(Point position) => false;
}

public sealed class Placeholder : StatelessWidget
{
    public static Color DefaultColor { get; } = Color.FromRgb(0x45, 0x5A, 0x64);

    public Placeholder(
        Color? color = null,
        double strokeWidth = 2.0,
        double fallbackWidth = 400.0,
        double fallbackHeight = 400.0,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        Color = color ?? DefaultColor;
        StrokeWidth = strokeWidth;
        FallbackWidth = fallbackWidth;
        FallbackHeight = fallbackHeight;
        Child = child;
    }

    public Color Color { get; }

    public double StrokeWidth { get; }

    public double FallbackWidth { get; }

    public double FallbackHeight { get; }

    public Widget? Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new LimitedBox(
            maxWidth: FallbackWidth,
            maxHeight: FallbackHeight,
            child: new CustomPaint(
                size: new Size(double.PositiveInfinity, double.PositiveInfinity),
                painter: new PlaceholderPainter(Color, StrokeWidth),
                child: Child));
    }
}
