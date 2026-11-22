using System.Diagnostics.CodeAnalysis;

namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkPathOpsQuad.h and SkPathOpsQuad.cpp at chrome/m119 (the Skia
// inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types
// are kept. SkDQuad is partial: horizontalIntersect/verticalIntersect come with SkDQuadLineIntersection.cpp.

internal struct SkDQuadPair
{
    public SkDPoint5 Pts;

    // Skia reinterprets pts[0..2] / pts[2..4] as an SkDQuad.
    public readonly SkDQuad First()
    {
        return new SkDQuad(Pts[0], Pts[1], Pts[2]);
    }

    public readonly SkDQuad Second()
    {
        return new SkDQuad(Pts[2], Pts[3], Pts[4]);
    }
}

internal partial struct SkDQuad
{
    public const int KPointCount = 3;
    public const int KPointLast = KPointCount - 1;
    public const int KMaxIntersections = 4;

    public SkDPoint3 Pts;

    public SkDQuad(SkDPoint p0, SkDPoint p1, SkDPoint p2)
    {
        Pts[0] = p0;
        Pts[1] = p1;
        Pts[2] = p2;
    }

    public readonly bool Collapsed()
    {
        return Pts[0].ApproximatelyEqual(Pts[1]) && Pts[0].ApproximatelyEqual(Pts[2]);
    }

    public readonly bool ControlsInside()
    {
        SkDVector v01 = Pts[0] - Pts[1];
        SkDVector v02 = Pts[0] - Pts[2];
        SkDVector v12 = Pts[1] - Pts[2];
        return v02.Dot(v01) > 0 && v02.Dot(v12) > 0;
    }

    public readonly SkDQuad Flip()
    {
        SkDQuad result = new SkDQuad(Pts[2], Pts[1], Pts[0]);
        return result;
    }

    public static bool IsConic()
    {
        return false;
    }

    public SkDQuad Set(ReadOnlySpan<PathPoint> pts)
    {
        Pts[0].Set(pts[0]);
        Pts[1].Set(pts[1]);
        Pts[2].Set(pts[2]);
        return this;
    }

    [UnscopedRef]
    public ref SkDPoint this[int n] => ref Pts[n];

    public static int MaxIntersections()
    {
        return KMaxIntersections;
    }

    public static int PointCount()
    {
        return KPointCount;
    }

    public static int PointLast()
    {
        return KPointLast;
    }

    public readonly void SubDivide(double t1, double t2, out SkDQuad quad)
    {
        quad = SubDivide(t1, t2);
    }

    public static SkDQuad SubDivide(ReadOnlySpan<PathPoint> a, double t1, double t2)
    {
        SkDQuad quad = default;
        quad.Set(a);
        return quad.SubDivide(t1, t2);
    }

    public static SkDPoint SubDivide(
        ReadOnlySpan<PathPoint> pts,
        in SkDPoint a,
        in SkDPoint c,
        double t1,
        double t2)
    {
        SkDQuad quad = default;
        quad.Set(pts);
        return quad.SubDivide(a, c, t1, t2);
    }

    // SkPathOpsQuad.cpp

    /* started with at_most_end_pts_in_common from SkDQuadIntersection.cpp */
    // Do a quick reject by rotating all points relative to a line formed by
    // a pair of one quad's points. If the 2nd quad's points
    // are on the line or on the opposite side from the 1st quad's 'odd man', the
    // curves at most intersect at the endpoints.
    /* if returning true, check contains true if quad's hull collapsed, making the cubic linear
       if returning false, check contains true if the the quad pair have only the end point in common
    */
    public readonly bool HullIntersects(in SkDQuad q2, ref bool isLinear)
    {
        bool linear = true;
        Span<SkDPoint> endPt = stackalloc SkDPoint[2];
        for (int oddMan = 0; oddMan < KPointCount; ++oddMan)
        {
            OtherPts(oddMan, endPt);
            double origX = endPt[0].X;
            double origY = endPt[0].Y;
            double adj = endPt[1].X - origX;
            double opp = endPt[1].Y - origY;
            double sign = (Pts[oddMan].Y - origY) * adj - (Pts[oddMan].X - origX) * opp;
            if (SkPathOps.ApproximatelyZero(sign))
            {
                continue;
            }

            linear = false;
            bool foundOutlier = false;
            for (int n = 0; n < KPointCount; ++n)
            {
                double test = (q2.Pts[n].Y - origY) * adj - (q2.Pts[n].X - origX) * opp;
                if (test * sign > 0 && !SkPathOps.PreciselyZero(test))
                {
                    foundOutlier = true;
                    break;
                }
            }

            if (!foundOutlier)
            {
                return false;
            }
        }

        if (linear
            && !SkPathOpsQuadStatics.MatchesEnd(Pts, q2.Pts[0])
            && !SkPathOpsQuadStatics.MatchesEnd(Pts, q2.Pts[2]))
        {
            // if the end point of the opposite quad is inside the hull that is nearly a line,
            // then representing the quad as a line may cause the intersection to be missed.
            // Check to see if the endpoint is in the triangle.
            if (SkPathOpsQuadStatics.PointInTriangle(Pts, q2.Pts[0])
                || SkPathOpsQuadStatics.PointInTriangle(Pts, q2.Pts[2]))
            {
                linear = false;
            }
        }

        isLinear = linear;
        return true;
    }

    public readonly bool HullIntersects(in SkDConic conic, ref bool isLinear)
    {
        return conic.HullIntersects(this, ref isLinear);
    }

    public readonly bool HullIntersects(in SkDCubic cubic, ref bool isLinear)
    {
        return cubic.HullIntersects(this, ref isLinear);
    }

    /* bit twiddling for finding the off curve index (x&~m is the pair in [0,1,2] excluding oddMan)
    oddMan    opp   x=oddMan^opp  x=x-oddMan  m=x>>2   x&~m
        0       1         1            1         0       1
                2         2            2         0       2
        1       1         0           -1        -1       0
                2         3            2         0       2
        2       1         3            1         0       1
                2         0           -2        -1       0
    */
    public readonly void OtherPts(int oddMan, Span<SkDPoint> endPt)
    {
        for (int opp = 1; opp < KPointCount; ++opp)
        {
            int end = (oddMan ^ opp) - oddMan; // choose a value not equal to oddMan
            end &= ~(end >> 2); // if the value went negative, set it to zero
            endPt[opp - 1] = Pts[end];
        }
    }

    public static int AddValidTs(ReadOnlySpan<double> s, int realRoots, Span<double> t)
    {
        int foundRoots = 0;
        for (int index = 0; index < realRoots; ++index)
        {
            double tValue = s[index];
            if (SkPathOps.ApproximatelyZeroOrMore(tValue) && SkPathOps.ApproximatelyOneOrLess(tValue))
            {
                if (SkPathOps.ApproximatelyLessThanZero(tValue))
                {
                    tValue = 0;
                }
                else if (SkPathOps.ApproximatelyGreaterThanOne(tValue))
                {
                    tValue = 1;
                }

                for (int idx2 = 0; idx2 < foundRoots; ++idx2)
                {
                    if (SkPathOps.ApproximatelyEqual(t[idx2], tValue))
                    {
                        goto nextRoot;
                    }
                }

                t[foundRoots++] = tValue;
            }

        nextRoot:
            ;
        }

        return foundRoots;
    }

    // note: caller expects multiple results to be sorted smaller first
    // note: http://en.wikipedia.org/wiki/Loss_of_significance has an interesting
    //  analysis of the quadratic equation, suggesting why the following looks at
    //  the sign of B -- and further suggesting that the greatest loss of precision
    //  is in b squared less two a c
    public static int RootsValidT(double A, double B, double C, Span<double> t)
    {
        Span<double> s = stackalloc double[2];
        int realRoots = RootsReal(A, B, C, s);
        int foundRoots = AddValidTs(s, realRoots, t);
        return foundRoots;
    }

    /*
    Numeric Solutions (5.6) suggests to solve the quadratic by computing
           Q = -1/2(B + sgn(B)Sqrt(B^2 - 4 A C))
    and using the roots
          t1 = Q / A
          t2 = C / Q
    */
    // this does not discard real roots <= 0 or >= 1
    // TODO(skbug.com/14063) Deduplicate with SkQuads::RootsReal
    public static int RootsReal(double A, double B, double C, Span<double> s)
    {
        if (A == 0)
        {
            return SkPathOpsQuadStatics.HandleZero(B, C, s);
        }

        double p = B / (2 * A);
        double q = C / A;
        if (SkPathOps.ApproximatelyZero(A)
            && (SkPathOps.ApproximatelyZeroInverse(p) || SkPathOps.ApproximatelyZeroInverse(q)))
        {
            return SkPathOpsQuadStatics.HandleZero(B, C, s);
        }

        /* normal form: x^2 + px + q = 0 */
        double p2 = p * p;
        if (!SkPathOps.AlmostDequalUlps(p2, q) && p2 < q)
        {
            return 0;
        }

        double sqrt_D = 0;
        if (p2 > q)
        {
            sqrt_D = Math.Sqrt(p2 - q);
        }

        s[0] = sqrt_D - p;
        s[1] = -sqrt_D - p;
        return 1 + (SkPathOps.AlmostDequalUlps(s[0], s[1]) ? 0 : 1);
    }

    public readonly bool IsLinear(int startIndex, int endIndex)
    {
        SkLineParameters lineParameters = default;
        lineParameters.QuadEndPoints(this, startIndex, endIndex);
        // FIXME: maybe it's possible to avoid this and compare non-normalized
        lineParameters.Normalize();
        double distance = lineParameters.ControlPtDistance(this);
        double tiniest = SkPathOps.StdMin(
            SkPathOps.StdMin(
                SkPathOps.StdMin(SkPathOps.StdMin(SkPathOps.StdMin(Pts[0].X, Pts[0].Y), Pts[1].X), Pts[1].Y),
                Pts[2].X),
            Pts[2].Y);
        double largest = SkPathOps.StdMax(
            SkPathOps.StdMax(
                SkPathOps.StdMax(SkPathOps.StdMax(SkPathOps.StdMax(Pts[0].X, Pts[0].Y), Pts[1].X), Pts[1].Y),
                Pts[2].X),
            Pts[2].Y);
        largest = SkPathOps.StdMax(largest, -tiniest);
        return SkPathOps.ApproximatelyZeroWhenComparedTo(distance, largest);
    }

    public readonly SkDVector DxdyAtT(double t)
    {
        double a = t - 1;
        double b = 1 - 2 * t;
        double c = t;
        SkDVector result = new SkDVector(
            a * Pts[0].X + b * Pts[1].X + c * Pts[2].X,
            a * Pts[0].Y + b * Pts[1].Y + c * Pts[2].Y);
        if (result.X == 0 && result.Y == 0)
        {
            if (SkPathOps.ZeroOrOne(t))
            {
                result = Pts[2] - Pts[0];
            }

            // else: incomplete (Skia only logs "!q")
        }

        return result;
    }

    // OPTIMIZE: assert if caller passes in t == 0 / t == 1 ?
    public readonly SkDPoint PtAtT(double t)
    {
        if (0 == t)
        {
            return Pts[0];
        }

        if (1 == t)
        {
            return Pts[2];
        }

        double one_t = 1 - t;
        double a = one_t * one_t;
        double b = 2 * one_t * t;
        double c = t * t;
        SkDPoint result = new SkDPoint(
            a * Pts[0].X + b * Pts[1].X + c * Pts[2].X,
            a * Pts[0].Y + b * Pts[1].Y + c * Pts[2].Y);
        return result;
    }

    public readonly bool MonotonicInX()
    {
        return SkPathOps.Between(Pts[0].X, Pts[1].X, Pts[2].X);
    }

    public readonly bool MonotonicInY()
    {
        return SkPathOps.Between(Pts[0].Y, Pts[1].Y, Pts[2].Y);
    }

    /*
    Given a quadratic q, t1, and t2, find a small quadratic segment.

    The new quadratic is defined by A, B, and C, where
     A = c[0]*(1 - t1)*(1 - t1) + 2*c[1]*t1*(1 - t1) + c[2]*t1*t1
     C = c[3]*(1 - t1)*(1 - t1) + 2*c[2]*t1*(1 - t1) + c[1]*t1*t1

    To find B, compute the point halfway between t1 and t2:

    q(at (t1 + t2)/2) == D

    Next, compute where D must be if we know the value of B:

    _12 = A/2 + B/2
    12_ = B/2 + C/2
    123 = A/4 + B/2 + C/4
        = D

    Group the known values on one side:

    B   = D*2 - A/2 - C/2
    */

    // OPTIMIZE? : special case  t1 = 1 && t2 = 0
    public readonly SkDQuad SubDivide(double t1, double t2)
    {
        if (0 == t1 && 1 == t2)
        {
            return this;
        }

        ReadOnlySpan<double> fPtsX = SkPathOps.Doubles(Pts);
        ReadOnlySpan<double> fPtsY = fPtsX[1..];
        SkDQuad dst = default;
        double ax = dst[0].X = SkPathOpsQuadStatics.InterpQuadCoords(fPtsX, t1);
        double ay = dst[0].Y = SkPathOpsQuadStatics.InterpQuadCoords(fPtsY, t1);
        double dx = SkPathOpsQuadStatics.InterpQuadCoords(fPtsX, (t1 + t2) / 2);
        double dy = SkPathOpsQuadStatics.InterpQuadCoords(fPtsY, (t1 + t2) / 2);
        double cx = dst[2].X = SkPathOpsQuadStatics.InterpQuadCoords(fPtsX, t2);
        double cy = dst[2].Y = SkPathOpsQuadStatics.InterpQuadCoords(fPtsY, t2);
        /* bx = */
        dst[1].X = 2 * dx - (ax + cx) / 2;
        /* by = */
        dst[1].Y = 2 * dy - (ay + cy) / 2;
        return dst;
    }

    public readonly void Align(int endIndex, ref SkDPoint dstPt)
    {
        if (Pts[endIndex].X == Pts[1].X)
        {
            dstPt.X = Pts[endIndex].X;
        }

        if (Pts[endIndex].Y == Pts[1].Y)
        {
            dstPt.Y = Pts[endIndex].Y;
        }
    }

    public readonly SkDPoint SubDivide(in SkDPoint a, in SkDPoint c, double t1, double t2)
    {
        SkDPoint b;
        SkDQuad sub = SubDivide(t1, t2);
        SkDLine b0 = new SkDLine(a, sub[1] + (a - sub[0]));
        SkDLine b1 = new SkDLine(c, sub[1] + (c - sub[2]));
        SkIntersections i = new SkIntersections();
        i.IntersectRay(b0, b1);
        if (i.Used() == 1 && i[0][0] >= 0 && i[1][0] >= 0)
        {
            b = i.Pt(0);
        }
        else
        {
            return SkDPoint.Mid(b0[1], b1[1]);
        }

        if (t1 == 0 || t2 == 0)
        {
            Align(0, ref b);
        }

        if (t1 == 1 || t2 == 1)
        {
            Align(2, ref b);
        }

        if (SkPathOps.AlmostBequalUlps(b.X, a.X))
        {
            b.X = a.X;
        }
        else if (SkPathOps.AlmostBequalUlps(b.X, c.X))
        {
            b.X = c.X;
        }

        if (SkPathOps.AlmostBequalUlps(b.Y, a.Y))
        {
            b.Y = a.Y;
        }
        else if (SkPathOps.AlmostBequalUlps(b.Y, c.Y))
        {
            b.Y = c.Y;
        }

        return b;
    }

    public readonly SkDQuadPair ChopAt(double t)
    {
        SkDQuadPair dst = default;
        Span<double> dstX = SkPathOps.MutableDoubles(dst.Pts);
        SkPathOpsQuadStatics.InterpQuadCoords(SkPathOps.Doubles(Pts), dstX, t);
        SkPathOpsQuadStatics.InterpQuadCoords(SkPathOps.Doubles(Pts)[1..], dstX[1..], t);
        return dst;
    }

    /** Quad'(t) = At + B, where
        A = 2(a - 2b + c)
        B = 2(b - a)
        Solve for t, only if it fits between 0 < t < 1
    */
    public static int FindExtrema(ReadOnlySpan<double> src, Span<double> tValue)
    {
        /*  At + B == 0
            t = -B / A
        */
        double a = src[0];
        double b = src[2];
        double c = src[4];
        return SkPathOpsQuadStatics.ValidUnitDivide(a - b, a - b - b + c, ref tValue[0]);
    }

    /* Parameterization form, given A*t*t + 2*B*t*(1-t) + C*(1-t)*(1-t)
     *
     * a = A - 2*B +   C
     * b =     2*B - 2*C
     * c =             C
     */
    public static void SetABC(ReadOnlySpan<double> quad, out double a, out double b, out double c)
    {
        a = quad[0]; // a = A
        b = 2 * quad[2]; // b =     2*B
        c = quad[4]; // c =             C
        b -= c; // b =     2*B -   C
        a -= b; // a = A - 2*B +   C
        b -= c; // b =     2*B - 2*C
    }
}

internal sealed class SkTQuad : SkTCurve
{
    public SkDQuad Quad;

    public SkTQuad()
    {
    }

    public SkTQuad(in SkDQuad q)
    {
        Quad = q;
    }

    public override ref SkDPoint this[int n] => ref Quad[n];

    public override bool Collapsed()
    {
        return Quad.Collapsed();
    }

    public override bool ControlsInside()
    {
        return Quad.ControlsInside();
    }

    public override SkDVector DxdyAtT(double t)
    {
        return Quad.DxdyAtT(t);
    }

    public override bool HullIntersects(in SkDQuad quad, ref bool isLinear)
    {
        return quad.HullIntersects(Quad, ref isLinear);
    }

    public override bool HullIntersects(in SkDConic conic, ref bool isLinear)
    {
        return conic.HullIntersects(Quad, ref isLinear);
    }

    public override bool HullIntersects(in SkDCubic cubic, ref bool isLinear)
    {
        return cubic.HullIntersects(Quad, ref isLinear);
    }

    public override bool HullIntersects(SkTCurve curve, ref bool isLinear)
    {
        return curve.HullIntersects(Quad, ref isLinear);
    }

    public override int IntersectRay(SkIntersections i, in SkDLine line)
    {
        return i.IntersectRay(Quad, line);
    }

    public override bool IsConic()
    {
        return false;
    }

    public override SkTCurve Make()
    {
        return new SkTQuad();
    }

    public override int MaxIntersections()
    {
        return SkDQuad.KMaxIntersections;
    }

    public override void OtherPts(int oddMan, Span<SkDPoint> endPt)
    {
        Quad.OtherPts(oddMan, endPt);
    }

    public override int PointCount()
    {
        return SkDQuad.KPointCount;
    }

    public override int PointLast()
    {
        return SkDQuad.KPointLast;
    }

    public override SkDPoint PtAtT(double t)
    {
        return Quad.PtAtT(t);
    }

    public override void SetBounds(ref SkDRect rect)
    {
        rect.SetBounds(Quad);
    }

    public override void SubDivide(double t1, double t2, SkTCurve curve)
    {
        ((SkTQuad)curve).Quad = Quad.SubDivide(t1, t2);
    }
}

file static class SkPathOpsQuadStatics
{
    // from blackpawn.com/texts/pointinpoly
    public static bool PointInTriangle(ReadOnlySpan<SkDPoint> fPts, in SkDPoint test)
    {
        SkDVector v0 = fPts[2] - fPts[0];
        SkDVector v1 = fPts[1] - fPts[0];
        SkDVector v2 = test - fPts[0];
        double dot00 = v0.Dot(v0);
        double dot01 = v0.Dot(v1);
        double dot02 = v0.Dot(v2);
        double dot11 = v1.Dot(v1);
        double dot12 = v1.Dot(v2);
        // Compute barycentric coordinates
        double denom = dot00 * dot11 - dot01 * dot01;
        double u = dot11 * dot02 - dot01 * dot12;
        double v = dot00 * dot12 - dot01 * dot02;
        // Check if point is in triangle
        if (denom >= 0)
        {
            return u >= 0 && v >= 0 && u + v < denom;
        }

        return u <= 0 && v <= 0 && u + v > denom;
    }

    public static bool MatchesEnd(ReadOnlySpan<SkDPoint> fPts, in SkDPoint test)
    {
        return fPts[0] == test || fPts[2] == test;
    }

    public static int HandleZero(double B, double C, Span<double> s)
    {
        if (SkPathOps.ApproximatelyZero(B))
        {
            s[0] = 0;
            return C == 0 ? 1 : 0;
        }

        s[0] = -C / B;
        return 1;
    }

    public static double InterpQuadCoords(ReadOnlySpan<double> src, double t)
    {
        if (0 == t)
        {
            return src[0];
        }

        if (1 == t)
        {
            return src[4];
        }

        double ab = SkPathOps.SkDInterp(src[0], src[2], t);
        double bc = SkPathOps.SkDInterp(src[2], src[4], t);
        double abc = SkPathOps.SkDInterp(ab, bc, t);
        return abc;
    }

    /* classic one t subdivision */
    public static void InterpQuadCoords(ReadOnlySpan<double> src, Span<double> dst, double t)
    {
        double ab = SkPathOps.SkDInterp(src[0], src[2], t);
        double bc = SkPathOps.SkDInterp(src[2], src[4], t);
        dst[0] = src[0];
        dst[2] = ab;
        dst[4] = SkPathOps.SkDInterp(ab, bc, t);
        dst[6] = bc;
        dst[8] = src[4];
    }

    public static int ValidUnitDivide(double numer, double denom, ref double ratio)
    {
        if (numer < 0)
        {
            numer = -numer;
            denom = -denom;
        }

        if (denom == 0 || numer == 0 || numer >= denom)
        {
            return 0;
        }

        double r = numer / denom;
        if (r == 0)
        {
            // catch underflow if numer <<<< denom
            return 0;
        }

        ratio = r;
        return 1;
    }
}
