namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkPathOpsRect.h and SkPathOpsRect.cpp at chrome/m119 (the Skia
// inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types
// are kept.

internal struct SkDRect
{
    public double Left;
    public double Top;
    public double Right;
    public double Bottom;

    public void Add(in SkDPoint pt)
    {
        Left = SkPathOps.StdMin(Left, pt.X);
        Top = SkPathOps.StdMin(Top, pt.Y);
        Right = SkPathOps.StdMax(Right, pt.X);
        Bottom = SkPathOps.StdMax(Bottom, pt.Y);
    }

    public readonly bool Contains(in SkDPoint pt)
    {
        return SkPathOps.ApproximatelyBetween(Left, pt.X, Right)
            && SkPathOps.ApproximatelyBetween(Top, pt.Y, Bottom);
    }

    public readonly bool Intersects(in SkDRect r)
    {
        return r.Left <= Right && Left <= r.Right && r.Top <= Bottom && Top <= r.Bottom;
    }

    public void Set(in SkDPoint pt)
    {
        Left = Right = pt.X;
        Top = Bottom = pt.Y;
    }

    public readonly double Width()
    {
        return Right - Left;
    }

    public readonly double Height()
    {
        return Bottom - Top;
    }

    public void SetBounds(in SkDConic curve)
    {
        SetBounds(curve, curve, 0, 1);
    }

    public void SetBounds(in SkDCubic curve)
    {
        SetBounds(curve, curve, 0, 1);
    }

    public void SetBounds(in SkDQuad curve)
    {
        SetBounds(curve, curve, 0, 1);
    }

    public readonly bool Valid()
    {
        return Left <= Right && Top <= Bottom;
    }

    // SkPathOpsRect.cpp

    public void SetBounds(in SkDQuad curve, in SkDQuad sub, double startT, double endT)
    {
        Set(sub.Pts[0]);
        Add(sub.Pts[2]);
        Span<double> tValues = stackalloc double[2];
        int roots = 0;
        if (!sub.MonotonicInX())
        {
            roots = SkDQuad.FindExtrema(SkPathOps.Doubles(sub.Pts), tValues);
        }

        if (!sub.MonotonicInY())
        {
            roots += SkDQuad.FindExtrema(SkPathOps.Doubles(sub.Pts)[1..], tValues[roots..]);
        }

        for (int index = 0; index < roots; ++index)
        {
            double t = startT + (endT - startT) * tValues[index];
            Add(curve.PtAtT(t));
        }
    }

    public void SetBounds(in SkDConic curve, in SkDConic sub, double startT, double endT)
    {
        Set(sub.Pts.Pts[0]);
        Add(sub.Pts.Pts[2]);
        Span<double> tValues = stackalloc double[2];
        int roots = 0;
        if (!sub.MonotonicInX())
        {
            roots = SkDConic.FindExtrema(SkPathOps.Doubles(sub.Pts.Pts), sub.Weight, tValues);
        }

        if (!sub.MonotonicInY())
        {
            roots += SkDConic.FindExtrema(SkPathOps.Doubles(sub.Pts.Pts)[1..], sub.Weight, tValues[roots..]);
        }

        for (int index = 0; index < roots; ++index)
        {
            double t = startT + (endT - startT) * tValues[index];
            Add(curve.PtAtT(t));
        }
    }

    public void SetBounds(in SkDCubic curve, in SkDCubic sub, double startT, double endT)
    {
        Set(sub.Pts[0]);
        Add(sub.Pts[3]);
        Span<double> tValues = stackalloc double[4];
        int roots = 0;
        if (!sub.MonotonicInX())
        {
            roots = SkDCubic.FindExtrema(SkPathOps.Doubles(sub.Pts), tValues);
        }

        if (!sub.MonotonicInY())
        {
            roots += SkDCubic.FindExtrema(SkPathOps.Doubles(sub.Pts)[1..], tValues[roots..]);
        }

        for (int index = 0; index < roots; ++index)
        {
            double t = startT + (endT - startT) * tValues[index];
            Add(curve.PtAtT(t));
        }
    }

    public void SetBounds(SkTCurve curve)
    {
        curve.SetBounds(ref this);
    }
}
