namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkLineParameters.h at chrome/m119 (the Skia inside SkiaSharp
// 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types are kept.
// Skia's class is only ever a stack value, so it is a struct here.

// Sources
// computer-aided design - volume 22 number 9 november 1990 pp 538 - 549
// online at http://cagd.cs.byu.edu/~tom/papers/bezclip.pdf

// This turns a line segment into a parameterized line, of the form
// ax + by + c = 0
// When a^2 + b^2 == 1, the line is normalized.
// The distance to the line for (x, y) is d(x,y) = ax + by + c
//
// Note that the distances below are not necessarily normalized. To get the true
// distance, it's necessary to either call normalize() after xxxEndPoints(), or
// divide the result of xxxDistance() by sqrt(normalSquared())
internal struct SkLineParameters
{
    private double _a;
    private double _b;
    private double _c;

    public bool CubicEndPoints(in SkDCubic pts)
    {
        int endIndex = 1;
        CubicEndPoints(pts, 0, endIndex);
        if (Dy() != 0)
        {
            return true;
        }

        if (Dx() == 0)
        {
            CubicEndPoints(pts, 0, ++endIndex);
            if (Dy() != 0)
            {
                return true;
            }

            if (Dx() == 0)
            {
                CubicEndPoints(pts, 0, ++endIndex); // line
                return false;
            }
        }

        // FIXME: after switching to round sort, remove bumping fA
        if (Dx() < 0)
        {
            // only worry about y bias when breaking cw/ccw tie
            return true;
        }

        // if cubic tangent is on x axis, look at next control point to break tie
        // control point may be approximate, so it must move significantly to account for error
        if (SkPathOps.NotAlmostEqualUlps(pts.Pts[0].Y, pts.Pts[++endIndex].Y))
        {
            if (pts.Pts[0].Y > pts.Pts[endIndex].Y)
            {
                _a = SkPathOps.DblEpsilon; // push it from 0 to slightly negative (y() returns -a)
            }

            return true;
        }

        if (endIndex == 3)
        {
            return true;
        }

        if (pts.Pts[0].Y > pts.Pts[3].Y)
        {
            _a = SkPathOps.DblEpsilon; // push it from 0 to slightly negative (y() returns -a)
        }

        return true;
    }

    public void CubicEndPoints(in SkDCubic pts, int s, int e)
    {
        _a = pts.Pts[s].Y - pts.Pts[e].Y;
        _b = pts.Pts[e].X - pts.Pts[s].X;
        _c = pts.Pts[s].X * pts.Pts[e].Y - pts.Pts[e].X * pts.Pts[s].Y;
    }

    public double CubicPart(in SkDCubic part)
    {
        CubicEndPoints(part);
        // Skia reads part[0..1] as an SkDLine.
        if (part.Pts[0] == part.Pts[1] || new SkDLine(part.Pts[0], part.Pts[1]).NearRay(part.Pts[2]))
        {
            return PointDistance(part.Pts[3]);
        }

        return PointDistance(part.Pts[2]);
    }

    public void LineEndPoints(in SkDLine pts)
    {
        _a = pts.Pts[0].Y - pts.Pts[1].Y;
        _b = pts.Pts[1].X - pts.Pts[0].X;
        _c = pts.Pts[0].X * pts.Pts[1].Y - pts.Pts[1].X * pts.Pts[0].Y;
    }

    public bool QuadEndPoints(in SkDQuad pts)
    {
        QuadEndPoints(pts, 0, 1);
        if (Dy() != 0)
        {
            return true;
        }

        if (Dx() == 0)
        {
            QuadEndPoints(pts, 0, 2);
            return false;
        }

        if (Dx() < 0)
        {
            // only worry about y bias when breaking cw/ccw tie
            return true;
        }

        // FIXME: after switching to round sort, remove this
        if (pts.Pts[0].Y > pts.Pts[2].Y)
        {
            _a = SkPathOps.DblEpsilon;
        }

        return true;
    }

    public void QuadEndPoints(in SkDQuad pts, int s, int e)
    {
        _a = pts.Pts[s].Y - pts.Pts[e].Y;
        _b = pts.Pts[e].X - pts.Pts[s].X;
        _c = pts.Pts[s].X * pts.Pts[e].Y - pts.Pts[e].X * pts.Pts[s].Y;
    }

    public double QuadPart(in SkDQuad part)
    {
        QuadEndPoints(part);
        return PointDistance(part.Pts[2]);
    }

    public readonly double NormalSquared()
    {
        return _a * _a + _b * _b;
    }

    public bool Normalize()
    {
        double normal = Math.Sqrt(NormalSquared());
        if (SkPathOps.ApproximatelyZero(normal))
        {
            _a = _b = _c = 0;
            return false;
        }

        double reciprocal = 1 / normal;
        _a *= reciprocal;
        _b *= reciprocal;
        _c *= reciprocal;
        return true;
    }

    public readonly void CubicDistanceY(in SkDCubic pts, ref SkDCubic distance)
    {
        double oneThird = 1 / 3.0;
        for (int index = 0; index < 4; ++index)
        {
            distance[index].X = index * oneThird;
            distance[index].Y = _a * pts.Pts[index].X + _b * pts.Pts[index].Y + _c;
        }
    }

    public readonly void QuadDistanceY(in SkDQuad pts, ref SkDQuad distance)
    {
        double oneHalf = 1 / 2.0;
        for (int index = 0; index < 3; ++index)
        {
            distance[index].X = index * oneHalf;
            distance[index].Y = _a * pts.Pts[index].X + _b * pts.Pts[index].Y + _c;
        }
    }

    public readonly double ControlPtDistance(in SkDCubic pts, int index)
    {
        return _a * pts.Pts[index].X + _b * pts.Pts[index].Y + _c;
    }

    public readonly double ControlPtDistance(in SkDQuad pts)
    {
        return _a * pts.Pts[1].X + _b * pts.Pts[1].Y + _c;
    }

    public readonly double PointDistance(in SkDPoint pt)
    {
        return _a * pt.X + _b * pt.Y + _c;
    }

    public readonly double Dx()
    {
        return _b;
    }

    public readonly double Dy()
    {
        return -_a;
    }
}
