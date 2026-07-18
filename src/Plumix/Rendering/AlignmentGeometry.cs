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
    private AlignmentGeometry(double x, double y, bool directional)
    {
        X = x;
        Y = y;
        IsDirectional = directional;
    }

    public double X { get; }

    public double Y { get; }

    internal bool IsDirectional { get; }

    public Alignment Resolve(TextDirection direction)
    {
        double x = IsDirectional && direction == TextDirection.Rtl ? -X : X;
        return new Alignment(x, Y);
    }

    public static implicit operator AlignmentGeometry(Alignment alignment)
    {
        return new AlignmentGeometry(alignment.X, alignment.Y, directional: false);
    }

    public static implicit operator AlignmentGeometry(AlignmentDirectional alignment)
    {
        return new AlignmentGeometry(alignment.Start, alignment.Y, directional: true);
    }
}
