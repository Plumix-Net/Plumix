using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/grid_paper.dart

internal sealed class GridPaperPainter : CustomPainter
{
    public GridPaperPainter(Color color, double interval, int divisions, int subdivisions)
    {
        Color = color;
        Interval = interval;
        Divisions = divisions;
        Subdivisions = subdivisions;
    }

    internal Color Color { get; }

    internal double Interval { get; }

    internal int Divisions { get; }

    internal int Subdivisions { get; }

    public override void Paint(PaintingContext context, Size size)
    {
        double allDivisions = Divisions * Subdivisions;
        double step = Interval / allDivisions;
        var brush = new SolidColorBrush(Color);

        for (double x = 0.0; x <= size.Width; x += step)
        {
            var pen = new Pen(brush, StrokeWidthAt(x));
            context.DrawLine(pen, new Point(x, 0.0), new Point(x, size.Height));
        }

        for (double y = 0.0; y <= size.Height; y += step)
        {
            var pen = new Pen(brush, StrokeWidthAt(y));
            context.DrawLine(pen, new Point(0.0, y), new Point(size.Width, y));
        }
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        return oldDelegate is not GridPaperPainter oldPainter
               || oldPainter.Color != Color
               || oldPainter.Interval != Interval
               || oldPainter.Divisions != Divisions
               || oldPainter.Subdivisions != Subdivisions;
    }

    public override bool? HitTest(Point position) => false;

    internal double StrokeWidthAt(double coordinate)
    {
        if (coordinate % Interval == 0.0)
        {
            return 1.0;
        }

        return coordinate % (Interval / Subdivisions) == 0.0 ? 0.5 : 0.25;
    }
}

public sealed class GridPaper : StatelessWidget
{
    public static Color DefaultColor { get; } = Color.FromArgb(0x7F, 0xC3, 0xE8, 0xF3);

    public GridPaper(
        Color? color = null,
        double interval = 100.0,
        int divisions = 2,
        int subdivisions = 5,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        if (divisions <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(divisions),
                "The divisions property must be greater than zero.");
        }

        if (subdivisions <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(subdivisions),
                "The subdivisions property must be greater than zero.");
        }

        Color = color ?? DefaultColor;
        Interval = interval;
        Divisions = divisions;
        Subdivisions = subdivisions;
        Child = child;
    }

    public Color Color { get; }

    public double Interval { get; }

    public int Divisions { get; }

    public int Subdivisions { get; }

    public Widget? Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new CustomPaint(
            foregroundPainter: new GridPaperPainter(Color, Interval, Divisions, Subdivisions),
            child: Child);
    }
}
