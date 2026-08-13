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
    private AlignmentGeometry(double x, double start, double y, bool isDirectional)
    {
        PhysicalX = x;
        Start = start;
        Y = y;
        IsDirectional = isDirectional;
    }

    private double PhysicalX { get; }

    private double Start { get; }

    public double X => PhysicalX + Start;

    public double Y { get; }

    /// <summary>
    /// Whether this value came from an <see cref="AlignmentDirectional"/>, mirroring Flutter's
    /// `alignment is AlignmentDirectional` check. A value lerped between the two kinds is mixed,
    /// and reports <see langword="false"/> just as Dart's `_MixedAlignment` does.
    /// </summary>
    public bool IsDirectional { get; }

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

        if (a is null)
        {
            return Scale(b!.Value, t);
        }

        if (b is null)
        {
            return Scale(a.Value, 1.0 - t);
        }

        AlignmentGeometry from = a.Value;
        AlignmentGeometry to = b.Value;
        return new AlignmentGeometry(
            x: LerpDouble(from.PhysicalX, to.PhysicalX, t),
            start: LerpDouble(from.Start, to.Start, t),
            y: LerpDouble(from.Y, to.Y, t),
            isDirectional: from.IsDirectional && to.IsDirectional);
    }

    public static implicit operator AlignmentGeometry(Alignment alignment)
    {
        return new AlignmentGeometry(alignment.X, 0.0, alignment.Y, isDirectional: false);
    }

    public static implicit operator AlignmentGeometry(AlignmentDirectional alignment)
    {
        return new AlignmentGeometry(0.0, alignment.Start, alignment.Y, isDirectional: true);
    }

    private static AlignmentGeometry Scale(AlignmentGeometry value, double factor)
    {
        return new AlignmentGeometry(
            x: value.PhysicalX * factor,
            start: value.Start * factor,
            y: value.Y * factor,
            isDirectional: value.IsDirectional);
    }

    private static double LerpDouble(double a, double b, double t) => a + ((b - a) * t);
}
