using System.Diagnostics.CodeAnalysis;

namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkPathOpsConic.h and SkPathOpsConic.cpp at chrome/m119 (the
// Skia inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double
// types are kept. Skia's fPts is an SkDQuad, so the points themselves are Pts.Pts.

internal struct SkDConic
{
    public const int KPointCount = 3;
    public const int KPointLast = KPointCount - 1;
    public const int KMaxIntersections = 4;

    public SkDQuad Pts;
    public float Weight;

    public SkDConic(in SkDQuad pts, float weight)
    {
        Pts = pts;
        Weight = weight;
    }

    public readonly bool Collapsed()
    {
        return Pts.Collapsed();
    }

    public readonly bool ControlsInside()
    {
        return Pts.ControlsInside();
    }

    public readonly SkDConic Flip()
    {
        SkDConic result = new SkDConic(new SkDQuad(Pts.Pts[2], Pts.Pts[1], Pts.Pts[0]), Weight);
        return result;
    }

    public static bool IsConic()
    {
        return true;
    }

    public SkDConic Set(ReadOnlySpan<PathPoint> pts, float weight)
    {
        Pts.Set(pts);
        Weight = weight;
        return this;
    }

    [UnscopedRef]
    public ref SkDPoint this[int n] => ref Pts[n];

    public static int AddValidTs(ReadOnlySpan<double> s, int realRoots, Span<double> t)
    {
        return SkDQuad.AddValidTs(s, realRoots, t);
    }

    public readonly void Align(int endIndex, ref SkDPoint dstPt)
    {
        Pts.Align(endIndex, ref dstPt);
    }

    public readonly bool HullIntersects(in SkDQuad quad, ref bool isLinear)
    {
        return Pts.HullIntersects(quad, ref isLinear);
    }

    public readonly bool HullIntersects(in SkDConic conic, ref bool isLinear)
    {
        return Pts.HullIntersects(conic.Pts, ref isLinear);
    }

    public readonly bool IsLinear(int startIndex, int endIndex)
    {
        return Pts.IsLinear(startIndex, endIndex);
    }

    public static int MaxIntersections()
    {
        return KMaxIntersections;
    }

    public readonly bool MonotonicInX()
    {
        return Pts.MonotonicInX();
    }

    public readonly bool MonotonicInY()
    {
        return Pts.MonotonicInY();
    }

    public readonly void OtherPts(int oddMan, Span<SkDPoint> endPt)
    {
        Pts.OtherPts(oddMan, endPt);
    }

    public static int PointCount()
    {
        return KPointCount;
    }

    public static int PointLast()
    {
        return KPointLast;
    }

    public static int RootsReal(double A, double B, double C, Span<double> t)
    {
        return SkDQuad.RootsReal(A, B, C, t);
    }

    public static int RootsValidT(double A, double B, double C, Span<double> s)
    {
        return SkDQuad.RootsValidT(A, B, C, s);
    }

    public readonly void SubDivide(double t1, double t2, out SkDConic c)
    {
        c = SubDivide(t1, t2);
    }

    public static SkDConic SubDivide(ReadOnlySpan<PathPoint> a, float weight, double t1, double t2)
    {
        SkDConic conic = default;
        conic.Set(a, weight);
        return conic.SubDivide(t1, t2);
    }

    public static SkDPoint SubDivide(
        ReadOnlySpan<PathPoint> pts,
        float weight,
        in SkDPoint a,
        in SkDPoint c,
        double t1,
        double t2,
        out float newWeight)
    {
        SkDConic conic = default;
        conic.Set(pts, weight);
        return conic.SubDivide(a, c, t1, t2, out newWeight);
    }

    // SkPathOpsConic.cpp

    public static int FindExtrema(ReadOnlySpan<double> src, float w, Span<double> t)
    {
        Span<double> coeff = stackalloc double[3];
        SkPathOpsConicStatics.ConicDerivCoeff(src, w, coeff);

        Span<double> tValues = stackalloc double[2];
        int roots = SkDQuad.RootsValidT(coeff[0], coeff[1], coeff[2], tValues);
        // In extreme cases, the number of roots returned can be 2. Pathops
        // will fail later on, so there's no advantage to plumbing in an error
        // return here.
        // SkASSERT(0 == roots || 1 == roots);

        if (1 == roots)
        {
            t[0] = tValues[0];
            return 1;
        }

        return 0;
    }

    public readonly SkDVector DxdyAtT(double t)
    {
        ReadOnlySpan<double> fPts = SkPathOps.Doubles(Pts.Pts);
        SkDVector result = new SkDVector(
            SkPathOpsConicStatics.ConicEvalTan(fPts, Weight, t),
            SkPathOpsConicStatics.ConicEvalTan(fPts[1..], Weight, t));
        if (result.X == 0 && result.Y == 0)
        {
            if (SkPathOps.ZeroOrOne(t))
            {
                result = Pts.Pts[2] - Pts.Pts[0];
            }

            // else: incomplete (Skia only logs "!k")
        }

        return result;
    }

    public readonly bool HullIntersects(in SkDCubic cubic, ref bool isLinear)
    {
        return cubic.HullIntersects(this, ref isLinear);
    }

    public readonly SkDPoint PtAtT(double t)
    {
        if (t == 0)
        {
            return Pts.Pts[0];
        }

        if (t == 1)
        {
            return Pts.Pts[2];
        }

        ReadOnlySpan<double> fPts = SkPathOps.Doubles(Pts.Pts);
        double denominator = SkPathOpsConicStatics.ConicEvalDenominator(Weight, t);
        SkDPoint result = new SkDPoint(
            SkPathOps.SkIeeeDoubleDivide(SkPathOpsConicStatics.ConicEvalNumerator(fPts, Weight, t), denominator),
            SkPathOps.SkIeeeDoubleDivide(SkPathOpsConicStatics.ConicEvalNumerator(fPts[1..], Weight, t), denominator));
        return result;
    }

    /* see quad subdivide for point rationale */
    /* w rationale : the mid point between t1 and t2 could be determined from the computed a/b/c
       values if the computed w was known. Since we know the mid point at (t1+t2)/2, we'll assume
       that it is the same as the point on the new curve t==(0+1)/2.

        d / dz == conic_poly(dst, unknownW, .5) / conic_weight(unknownW, .5);

        conic_poly(dst, unknownW, .5)
                      =   a / 4 + (b * unknownW) / 2 + c / 4
                      =  (a + c) / 4 + (bx * unknownW) / 2

        conic_weight(unknownW, .5)
                      =   unknownW / 2 + 1 / 2

        d / dz                  == ((a + c) / 2 + b * unknownW) / (unknownW + 1)
        d / dz * (unknownW + 1) ==  (a + c) / 2 + b * unknownW
                  unknownW       = ((a + c) / 2 - d / dz) / (d / dz - b)

        Thus, w is the ratio of the distance from the mid of end points to the on-curve point, and the
        distance of the on-curve point to the control point.
     */
    public readonly SkDConic SubDivide(double t1, double t2)
    {
        ReadOnlySpan<double> fPtsX = SkPathOps.Doubles(Pts.Pts);
        ReadOnlySpan<double> fPtsY = fPtsX[1..];
        double ax, ay, az;
        if (t1 == 0)
        {
            ax = Pts.Pts[0].X;
            ay = Pts.Pts[0].Y;
            az = 1;
        }
        else if (t1 != 1)
        {
            ax = SkPathOpsConicStatics.ConicEvalNumerator(fPtsX, Weight, t1);
            ay = SkPathOpsConicStatics.ConicEvalNumerator(fPtsY, Weight, t1);
            az = SkPathOpsConicStatics.ConicEvalDenominator(Weight, t1);
        }
        else
        {
            ax = Pts.Pts[2].X;
            ay = Pts.Pts[2].Y;
            az = 1;
        }

        double midT = (t1 + t2) / 2;
        double dx = SkPathOpsConicStatics.ConicEvalNumerator(fPtsX, Weight, midT);
        double dy = SkPathOpsConicStatics.ConicEvalNumerator(fPtsY, Weight, midT);
        double dz = SkPathOpsConicStatics.ConicEvalDenominator(Weight, midT);
        double cx, cy, cz;
        if (t2 == 1)
        {
            cx = Pts.Pts[2].X;
            cy = Pts.Pts[2].Y;
            cz = 1;
        }
        else if (t2 != 0)
        {
            cx = SkPathOpsConicStatics.ConicEvalNumerator(fPtsX, Weight, t2);
            cy = SkPathOpsConicStatics.ConicEvalNumerator(fPtsY, Weight, t2);
            cz = SkPathOpsConicStatics.ConicEvalDenominator(Weight, t2);
        }
        else
        {
            cx = Pts.Pts[0].X;
            cy = Pts.Pts[0].Y;
            cz = 1;
        }

        double bx = 2 * dx - (ax + cx) / 2;
        double by = 2 * dy - (ay + cy) / 2;
        double bz = 2 * dz - (az + cz) / 2;
        if (bz == 0)
        {
            bz = 1; // if bz is 0, weight is 0, control point has no effect: any value will do
        }

        SkDConic dst = new SkDConic(
            new SkDQuad(
                new SkDPoint(ax / az, ay / az),
                new SkDPoint(bx / bz, by / bz),
                new SkDPoint(cx / cz, cy / cz)),
            SkPathOps.SkDoubleToScalar(bz / Math.Sqrt(az * cz)));
        return dst;
    }

    public readonly SkDPoint SubDivide(in SkDPoint a, in SkDPoint c, double t1, double t2, out float weight)
    {
        SkDConic chopped = SubDivide(t1, t2);
        weight = chopped.Weight;
        return chopped[1];
    }
}

internal sealed class SkTConic : SkTCurve
{
    public SkDConic Conic;

    public SkTConic()
    {
    }

    public SkTConic(in SkDConic c)
    {
        Conic = c;
    }

    public override ref SkDPoint this[int n] => ref Conic[n];

    public override bool Collapsed()
    {
        return Conic.Collapsed();
    }

    public override bool ControlsInside()
    {
        return Conic.ControlsInside();
    }

    public override SkDVector DxdyAtT(double t)
    {
        return Conic.DxdyAtT(t);
    }

    public override bool HullIntersects(in SkDQuad quad, ref bool isLinear)
    {
        return quad.HullIntersects(Conic, ref isLinear);
    }

    public override bool HullIntersects(in SkDConic conic, ref bool isLinear)
    {
        return conic.HullIntersects(Conic, ref isLinear);
    }

    public override bool HullIntersects(in SkDCubic cubic, ref bool isLinear)
    {
        return cubic.HullIntersects(Conic, ref isLinear);
    }

    public override bool HullIntersects(SkTCurve curve, ref bool isLinear)
    {
        return curve.HullIntersects(Conic, ref isLinear);
    }

    public override int IntersectRay(SkIntersections i, in SkDLine line)
    {
        return i.IntersectRay(Conic, line);
    }

    public override bool IsConic()
    {
        return true;
    }

    public override SkTCurve Make()
    {
        return new SkTConic();
    }

    public override int MaxIntersections()
    {
        return SkDConic.KMaxIntersections;
    }

    public override void OtherPts(int oddMan, Span<SkDPoint> endPt)
    {
        Conic.OtherPts(oddMan, endPt);
    }

    public override int PointCount()
    {
        return SkDConic.KPointCount;
    }

    public override int PointLast()
    {
        return SkDConic.KPointLast;
    }

    public override SkDPoint PtAtT(double t)
    {
        return Conic.PtAtT(t);
    }

    public override void SetBounds(ref SkDRect rect)
    {
        rect.SetBounds(Conic);
    }

    public override void SubDivide(double t1, double t2, SkTCurve curve)
    {
        ((SkTConic)curve).Conic = Conic.SubDivide(t1, t2);
    }
}

file static class SkPathOpsConicStatics
{
    // cribbed from the float version in SkGeometry.cpp
    public static void ConicDerivCoeff(ReadOnlySpan<double> src, float w, Span<double> coeff)
    {
        double P20 = src[4] - src[0];
        double P10 = src[2] - src[0];
        double wP10 = w * P10;
        coeff[0] = w * P20 - P20;
        coeff[1] = P20 - 2 * wP10;
        coeff[2] = wP10;
    }

    public static double ConicEvalTan(ReadOnlySpan<double> coord, float w, double t)
    {
        Span<double> coeff = stackalloc double[3];
        ConicDerivCoeff(coord, w, coeff);
        return t * (t * coeff[0] + coeff[1]) + coeff[2];
    }

    public static double ConicEvalNumerator(ReadOnlySpan<double> src, float w, double t)
    {
        double src2w = src[2] * w;
        double C = src[0];
        double A = src[4] - 2 * src2w + C;
        double B = 2 * (src2w - C);
        return (A * t + B) * t + C;
    }

    public static double ConicEvalDenominator(float w, double t)
    {
        double B = 2 * (w - 1); // float arithmetic, as in Skia
        double C = 1;
        double A = -B;
        return (A * t + B) * t + C;
    }
}
