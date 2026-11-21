using System.Diagnostics.CodeAnalysis;

namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkPathOpsLine.h and SkPathOpsLine.cpp at chrome/m119 (the Skia
// inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types
// are kept.

internal struct SkDLine
{
    public SkDPoint2 Pts;

    public SkDLine(SkDPoint p0, SkDPoint p1)
    {
        Pts[0] = p0;
        Pts[1] = p1;
    }

    [UnscopedRef]
    public ref SkDPoint this[int n] => ref Pts[n];

    public SkDLine Set(ReadOnlySpan<PathPoint> pts)
    {
        Pts[0].Set(pts[0]);
        Pts[1].Set(pts[1]);
        return this;
    }

    public readonly SkDPoint PtAtT(double t)
    {
        if (0 == t)
        {
            return Pts[0];
        }

        if (1 == t)
        {
            return Pts[1];
        }

        double one_t = 1 - t;
        SkDPoint result = new SkDPoint(one_t * Pts[0].X + t * Pts[1].X, one_t * Pts[0].Y + t * Pts[1].Y);
        return result;
    }

    public readonly double ExactPoint(in SkDPoint xy)
    {
        if (xy == Pts[0])
        {
            // do cheapest test first
            return 0;
        }

        if (xy == Pts[1])
        {
            return 1;
        }

        return -1;
    }

    // Skia's nearPoint(xy, nullptr).
    public readonly double NearPoint(in SkDPoint xy)
    {
        bool unequal = false;
        return NearPoint(xy, ref unequal);
    }

    public readonly double NearPoint(in SkDPoint xy, ref bool unequal)
    {
        if (!SkPathOps.AlmostBetweenUlps(Pts[0].X, xy.X, Pts[1].X)
            || !SkPathOps.AlmostBetweenUlps(Pts[0].Y, xy.Y, Pts[1].Y))
        {
            return -1;
        }

        // project a perpendicular ray from the point to the line; find the T on the line
        SkDVector len = Pts[1] - Pts[0]; // the x/y magnitudes of the line
        double denom = len.X * len.X + len.Y * len.Y; // see DLine intersectRay
        SkDVector ab0 = xy - Pts[0];
        double numer = len.X * ab0.X + ab0.Y * len.Y;
        if (!SkPathOps.Between(0, numer, denom))
        {
            return -1;
        }

        if (denom == 0)
        {
            return 0;
        }

        double t = numer / denom;
        SkDPoint realPt = PtAtT(t);
        double dist = realPt.Distance(xy); // OPTIMIZATION: can we compare against distSq instead ?
        // find the ordinal in the original line with the largest unsigned exponent
        double tiniest = SkPathOps.StdMin(
            SkPathOps.StdMin(SkPathOps.StdMin(Pts[0].X, Pts[0].Y), Pts[1].X),
            Pts[1].Y);
        double largest = SkPathOps.StdMax(
            SkPathOps.StdMax(SkPathOps.StdMax(Pts[0].X, Pts[0].Y), Pts[1].X),
            Pts[1].Y);
        largest = SkPathOps.StdMax(largest, -tiniest);
        if (!SkPathOps.AlmostEqualUlpsPin(largest, largest + dist))
        {
            // is the dist within ULPS tolerance?
            return -1;
        }

        unequal = (float)largest != (float)(largest + dist);
        t = SkPathOps.SkPinT(t); // a looser pin breaks skpwww_lptemp_com_3
        return t;
    }

    public readonly bool NearRay(in SkDPoint xy)
    {
        // project a perpendicular ray from the point to the line; find the T on the line
        SkDVector len = Pts[1] - Pts[0]; // the x/y magnitudes of the line
        double denom = len.X * len.X + len.Y * len.Y; // see DLine intersectRay
        SkDVector ab0 = xy - Pts[0];
        double numer = len.X * ab0.X + ab0.Y * len.Y;
        double t = numer / denom;
        SkDPoint realPt = PtAtT(t);
        double dist = realPt.Distance(xy); // OPTIMIZATION: can we compare against distSq instead ?
        // find the ordinal in the original line with the largest unsigned exponent
        double tiniest = SkPathOps.StdMin(
            SkPathOps.StdMin(SkPathOps.StdMin(Pts[0].X, Pts[0].Y), Pts[1].X),
            Pts[1].Y);
        double largest = SkPathOps.StdMax(
            SkPathOps.StdMax(SkPathOps.StdMax(Pts[0].X, Pts[0].Y), Pts[1].X),
            Pts[1].Y);
        largest = SkPathOps.StdMax(largest, -tiniest);
        return SkPathOps.RoughlyEqualUlps(largest, largest + dist); // is the dist within ULPS tolerance?
    }

    public static double ExactPointH(in SkDPoint xy, double left, double right, double y)
    {
        if (xy.Y == y)
        {
            if (xy.X == left)
            {
                return 0;
            }

            if (xy.X == right)
            {
                return 1;
            }
        }

        return -1;
    }

    public static double NearPointH(in SkDPoint xy, double left, double right, double y)
    {
        if (!SkPathOps.AlmostBequalUlps(xy.Y, y))
        {
            return -1;
        }

        if (!SkPathOps.AlmostBetweenUlps(left, xy.X, right))
        {
            return -1;
        }

        double t = (xy.X - left) / (right - left);
        t = SkPathOps.SkPinT(t);
        double realPtX = (1 - t) * left + t * right;
        SkDVector distU = new SkDVector(xy.Y - y, xy.X - realPtX);
        double distSq = distU.X * distU.X + distU.Y * distU.Y;
        double dist = Math.Sqrt(distSq); // OPTIMIZATION: can we compare against distSq instead ?
        double tiniest = SkPathOps.StdMin(SkPathOps.StdMin(y, left), right);
        double largest = SkPathOps.StdMax(SkPathOps.StdMax(y, left), right);
        largest = SkPathOps.StdMax(largest, -tiniest);
        if (!SkPathOps.AlmostEqualUlps(largest, largest + dist))
        {
            // is the dist within ULPS tolerance?
            return -1;
        }

        return t;
    }

    public static double ExactPointV(in SkDPoint xy, double top, double bottom, double x)
    {
        if (xy.X == x)
        {
            if (xy.Y == top)
            {
                return 0;
            }

            if (xy.Y == bottom)
            {
                return 1;
            }
        }

        return -1;
    }

    public static double NearPointV(in SkDPoint xy, double top, double bottom, double x)
    {
        if (!SkPathOps.AlmostBequalUlps(xy.X, x))
        {
            return -1;
        }

        if (!SkPathOps.AlmostBetweenUlps(top, xy.Y, bottom))
        {
            return -1;
        }

        double t = (xy.Y - top) / (bottom - top);
        t = SkPathOps.SkPinT(t);
        double realPtY = (1 - t) * top + t * bottom;
        SkDVector distU = new SkDVector(xy.X - x, xy.Y - realPtY);
        double distSq = distU.X * distU.X + distU.Y * distU.Y;
        double dist = Math.Sqrt(distSq); // OPTIMIZATION: can we compare against distSq instead ?
        double tiniest = SkPathOps.StdMin(SkPathOps.StdMin(x, top), bottom);
        double largest = SkPathOps.StdMax(SkPathOps.StdMax(x, top), bottom);
        largest = SkPathOps.StdMax(largest, -tiniest);
        if (!SkPathOps.AlmostEqualUlps(largest, largest + dist))
        {
            // is the dist within ULPS tolerance?
            return -1;
        }

        return t;
    }
}
