namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of SkClassifyCubic and SkCubicType (for SkDCubic::ComplexBreak) and of
// SkFindQuadMaxCurvature, SkChopQuadAtMaxCurvature and SkConic::chopAt (for SkOpEdgeBuilder) from
// skia/src/core/SkGeometry.cpp/.h at chrome/m119 (the Skia inside SkiaSharp 3.119), behind Path.Combine;
// Plumix.UI.PathGeometry (the rest of SkGeometry) does not port them, and SkChopQuadAt and chopAt reuse its
// ChopQuadAt and ChopConicAt. Line for line: Skia's names, operation order and float/double types are kept.

internal enum SkCubicType
{
    Serpentine,
    Loop,
    LocalCusp, // Cusp at a non-infinite parameter value with an inflection at t=infinity.
    CuspAtInfinity, // Cusp with a cusp at t=infinity and a local inflection.
    Quadratic,
    LineOrPoint,
}

internal static partial class SkPathOps
{
    /** Returns the cubic classification.

        t[],s[] are set to the two homogeneous parameter values at which points the lines L & M
        intersect with K, sorted from smallest to largest and oriented so positive values of the
        implicit are on the "left" side. For a serpentine curve they are the inflection points. For a
        loop they are the double point. For a local cusp, they are both equal and denote the cusp point.
        For a cusp at an infinite parameter value, one will be the local inflection point and the other
        +inf (t,s = 1,0). If the curve is degenerate (i.e. quadratic or linear) they are both set to a
        parameter value of +inf (t,s = 1,0).

        d[] is filled with the cubic inflection function coefficients. Empty spans stand for Skia's nullptr.
    */
    public static SkCubicType SkClassifyCubic(
        ReadOnlySpan<PathPoint> P,
        Span<double> t,
        Span<double> s,
        Span<double> d)
    {
        // Find the cubic's inflection function, I = [T^3  -3T^2  3T  -1] dot D. (D0 will always be 0
        // for integral cubics.)
        //
        // See "Resolution Independent Curve Rendering using Programmable Graphics Hardware",
        // 4.2 Curve Categorization:
        //
        // https://www.microsoft.com/en-us/research/wp-content/uploads/2005/01/p1000-loop.pdf
        double A1 = SkGeometryStatics.CalcDotCrossCubic(P[0], P[3], P[2]);
        double A2 = SkGeometryStatics.CalcDotCrossCubic(P[1], P[0], P[3]);
        double A3 = SkGeometryStatics.CalcDotCrossCubic(P[2], P[1], P[0]);

        double D3 = 3 * A3;
        double D2 = D3 - A2;
        double D1 = D2 - A2 + A1;

        // Shift the exponents in D so the largest magnitude falls somewhere in 1..2. This protects us
        // from overflow down the road while solving for roots and KLM functionals.
        double Dmax = StdMax(StdMax(Math.Abs(D1), Math.Abs(D2)), Math.Abs(D3));
        double norm = SkGeometryStatics.PreviousInversePow2(Dmax);
        D1 *= norm;
        D2 *= norm;
        D3 *= norm;

        if (!d.IsEmpty)
        {
            d[3] = D3;
            d[2] = D2;
            d[1] = D1;
            d[0] = 0;
        }

        bool hasTs = !t.IsEmpty && !s.IsEmpty;

        // Now use the inflection function to classify the cubic.
        //
        // See "Resolution Independent Curve Rendering using Programmable Graphics Hardware",
        // 4.4 Integral Cubics:
        //
        // https://www.microsoft.com/en-us/research/wp-content/uploads/2005/01/p1000-loop.pdf
        if (0 != D1)
        {
            double discr = 3 * D2 * D2 - 4 * D1 * D3;
            if (discr > 0)
            {
                // Serpentine.
                if (hasTs)
                {
                    double q = 3 * D2 + Math.CopySign(Math.Sqrt(3 * discr), D2);
                    SkGeometryStatics.WriteCubicInflectionRoots(q, 6 * D1, 2 * D3, q, t, s);
                }

                return SkCubicType.Serpentine;
            }
            else if (discr < 0)
            {
                // Loop.
                if (hasTs)
                {
                    double q = D2 + Math.CopySign(Math.Sqrt(-discr), D2);
                    SkGeometryStatics.WriteCubicInflectionRoots(q, 2 * D1, 2 * (D2 * D2 - D3 * D1), D1 * q, t, s);
                }

                return SkCubicType.Loop;
            }
            else
            {
                // Cusp.
                if (hasTs)
                {
                    SkGeometryStatics.WriteCubicInflectionRoots(D2, 2 * D1, D2, 2 * D1, t, s);
                }

                return SkCubicType.LocalCusp;
            }
        }
        else
        {
            if (0 != D2)
            {
                // Cusp at T=infinity.
                if (hasTs)
                {
                    SkGeometryStatics.WriteCubicInflectionRoots(D3, 3 * D2, 1, 0, t, s); // T1=infinity.
                }

                return SkCubicType.CuspAtInfinity;
            }
            else
            {
                // Degenerate.
                if (hasTs)
                {
                    SkGeometryStatics.WriteCubicInflectionRoots(1, 0, 1, 0, t, s); // T0=T1=infinity.
                }

                return 0 != D3 ? SkCubicType.Quadratic : SkCubicType.LineOrPoint;
            }
        }
    }

    //  F(t)    = a (1 - t) ^ 2 + 2 b t (1 - t) + c t ^ 2
    //  F'(t)   = 2 (b - a) + 2 (a - 2b + c) t
    //  F''(t)  = 2 (a - 2b + c)
    //
    //  A = 2 (b - a)
    //  B = 2 (a - 2b + c)
    //
    //  Maximum curvature for a quadratic means solving
    //  Fx' Fx'' + Fy' Fy'' = 0
    //
    //  t = - (Ax Bx + Ay By) / (Bx ^ 2 + By ^ 2)
    //
    public static float SkFindQuadMaxCurvature(ReadOnlySpan<PathPoint> src)
    {
        float ax = src[1].X - src[0].X;
        float ay = src[1].Y - src[0].Y;
        float bx = src[0].X - src[1].X - src[1].X + src[2].X;
        float by = src[0].Y - src[1].Y - src[1].Y + src[2].Y;

        float numer = -(ax * bx + ay * by);
        float denom = bx * bx + by * by;
        if (denom < 0)
        {
            numer = -numer;
            denom = -denom;
        }

        if (numer <= 0)
        {
            return 0;
        }

        if (numer >= denom)
        {
            // Also catches denom=0.
            return 1;
        }

        float t = numer / denom;
        return t;
    }

    public static int SkChopQuadAtMaxCurvature(ReadOnlySpan<PathPoint> src, Span<PathPoint> dst)
    {
        float t = SkFindQuadMaxCurvature(src);
        if (t > 0 && t < 1)
        {
            PathGeometry.ChopQuadAt(src, dst, t);
            return 2;
        }
        else
        {
            src[..3].CopyTo(dst);
            return 1;
        }
    }

    // SkConic::chopAt(t, dst): dst[0] is pts[0..3) with weight0, dst[1] is pts[2..5) with weight1; false if
    // infinity or NaN is generated.
    public static bool SkConicChopAt(
        ReadOnlySpan<PathPoint> pts,
        float w,
        float t,
        Span<PathPoint> dst,
        out float weight0,
        out float weight1)
    {
        return PathGeometry.ChopConicAt(pts, w, t, dst, out weight0, out weight1);
    }
}

file static class SkGeometryStatics
{
    // Assumes the third component of points is 1.
    // Calcs p0 . (p1 x p2)
    public static double CalcDotCrossCubic(PathPoint p0, PathPoint p1, PathPoint p2)
    {
        double xComp = (double)p0.X * ((double)p1.Y - (double)p2.Y);
        double yComp = (double)p0.Y * ((double)p2.X - (double)p1.X);
        double wComp = (double)p1.X * (double)p2.Y - (double)p1.Y * (double)p2.X;
        return (xComp + yComp + wComp);
    }

    // Returns a positive power of 2 that, when multiplied by n, and excepting the two edge cases listed
    // below, shifts the exponent of n to yield a magnitude somewhere inside [1..2).
    // Returns 2^1023 if abs(n) < 2^-1022 (including 0).
    // Returns NaN if n is Inf or NaN.
    public static double PreviousInversePow2(double n)
    {
        ulong bits = (ulong)BitConverter.DoubleToInt64Bits(n);
        bits = ((1023UL * 2 << 52) + ((1UL << 52) - 1)) - bits; // exp=-exp
        bits &= 0x7ffUL << 52; // mantissa=1.0, sign=0
        n = BitConverter.Int64BitsToDouble((long)bits);
        return n;
    }

    public static void WriteCubicInflectionRoots(
        double t0,
        double s0,
        double t1,
        double s1,
        Span<double> t,
        Span<double> s)
    {
        t[0] = t0;
        s[0] = s0;

        // This copysign/abs business orients the implicit function so positive values are always on the
        // "left" side of the curve.
        t[1] = -Math.CopySign(t1, t1 * s1);
        s[1] = -Math.Abs(s1);

        // Ensure t[0]/s[0] <= t[1]/s[1] (s[1] is negative from above).
        if (Math.CopySign(s[1], s[0]) * t[0] > -Math.Abs(s[0]) * t[1])
        {
            (t[0], t[1]) = (t[1], t[0]);
            (s[0], s[1]) = (s[1], s[0]);
        }
    }
}
