using Plumix.UI;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/alignment.dart

public readonly record struct AlignmentDirectional(double Start, double Y)
{
    public static AlignmentDirectional TopStart => new(-1, -1);

    public static AlignmentDirectional TopCenter => new(0, -1);

    public static AlignmentDirectional TopEnd => new(1, -1);

    public static AlignmentDirectional CenterStart => new(-1, 0);

    public static AlignmentDirectional Center => new(0, 0);

    public static AlignmentDirectional CenterEnd => new(1, 0);

    public static AlignmentDirectional BottomStart => new(-1, 1);

    public static AlignmentDirectional BottomCenter => new(0, 1);

    public static AlignmentDirectional BottomEnd => new(1, 1);

    public Alignment Resolve(TextDirection direction)
    {
        double x = direction == TextDirection.Rtl ? -Start : Start;
        return new Alignment(x, Y);
    }
}

public readonly record struct AlignmentGeometry
{
    private AlignmentGeometry(double x, double start, double y)
    {
        PhysicalX = x;
        Start = start;
        Y = y;
    }

    private double PhysicalX { get; }

    private double Start { get; }

    public double X => PhysicalX + Start;

    public double Y { get; }

    internal bool IsDirectional => Start != 0.0;

    public Alignment Resolve(TextDirection direction)
    {
        double x = PhysicalX + (direction == TextDirection.Rtl ? -Start : Start);
        return new Alignment(x, Y);
    }

    public static AlignmentGeometry? Lerp(
        AlignmentGeometry? a,
        AlignmentGeometry? b,
        double t)
    {
        if (a == b)
        {
            return a;
        }

        AlignmentGeometry from = a ?? default;
        AlignmentGeometry to = b ?? default;
        return new AlignmentGeometry(
            x: LerpDouble(from.PhysicalX, to.PhysicalX, t),
            start: LerpDouble(from.Start, to.Start, t),
            y: LerpDouble(from.Y, to.Y, t));
    }

    public static implicit operator AlignmentGeometry(Alignment alignment)
    {
        return new AlignmentGeometry(alignment.X, 0.0, alignment.Y);
    }

    public static implicit operator AlignmentGeometry(AlignmentDirectional alignment)
    {
        return new AlignmentGeometry(0.0, alignment.Start, alignment.Y);
    }

    private static double LerpDouble(double a, double b, double t) => a + ((b - a) * t);
}
