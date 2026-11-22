using System.Diagnostics.CodeAnalysis;

namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkPathOpsCubic.h and SkPathOpsCubic.cpp at chrome/m119 (the
// Skia inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double
// types are kept. SkDCubic is partial: convexHull is in SkOpCubicHull.cs, horizontalIntersect/verticalIntersect
// come with SkDCubicLineIntersection.cpp. toQuad (SkDCubicToQuads.cpp) is not ported: nothing in pathops calls it.

internal partial struct SkDCubic
{
    public const int KPointCount = 4;
    public const int KPointLast = KPointCount - 1;
    public const int KMaxIntersections = 9;

    public enum SearchAxis
    {
        XAxis,
        YAxis,
    }

    public const int GPrecisionUnit = 256; // FIXME: test different values in test framework

    public SkDPoint4 Pts;

    public SkDCubic(SkDPoint p0, SkDPoint p1, SkDPoint p2, SkDPoint p3)
    {
        Pts[0] = p0;
        Pts[1] = p1;
        Pts[2] = p2;
        Pts[3] = p3;
    }

    public readonly bool Collapsed()
    {
        return Pts[0].ApproximatelyEqual(Pts[1]) && Pts[0].ApproximatelyEqual(Pts[2])
            && Pts[0].ApproximatelyEqual(Pts[3]);
    }

    public readonly bool ControlsInside()
    {
        SkDVector v01 = Pts[0] - Pts[1];
        SkDVector v02 = Pts[0] - Pts[2];
        SkDVector v03 = Pts[0] - Pts[3];
        SkDVector v13 = Pts[1] - Pts[3];
        SkDVector v23 = Pts[2] - Pts[3];
        return v03.Dot(v01) > 0 && v03.Dot(v02) > 0 && v03.Dot(v13) > 0 && v03.Dot(v23) > 0;
    }

    public static bool IsConic()
    {
        return false;
    }

    [UnscopedRef]
    public ref SkDPoint this[int n] => ref Pts[n];

    public static int FindInflections(ReadOnlySpan<PathPoint> a, Span<double> tValues)
    {
        SkDCubic cubic = default;
        return cubic.Set(a).FindInflections(tValues);
    }

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

    public SkDCubic Set(ReadOnlySpan<PathPoint> pts)
    {
        Pts[0].Set(pts[0]);
        Pts[1].Set(pts[1]);
        Pts[2].Set(pts[2]);
        Pts[3].Set(pts[3]);
        return this;
    }

    public readonly void SubDivide(double t1, double t2, out SkDCubic c)
    {
        c = SubDivide(t1, t2);
    }

    public static SkDCubic SubDivide(ReadOnlySpan<PathPoint> a, double t1, double t2)
    {
        SkDCubic cubic = default;
        return cubic.Set(a).SubDivide(t1, t2);
    }

    public static void SubDivide(
        ReadOnlySpan<PathPoint> pts,
        in SkDPoint a,
        in SkDPoint d,
        double t1,
        double t2,
        Span<SkDPoint> p)
    {
        SkDCubic cubic = default;
        cubic.Set(pts).SubDivide(a, d, t1, t2, p);
    }

    // SkPathOpsCubic.cpp

    public readonly void Align(int endIndex, int ctrlIndex, ref SkDPoint dstPt)
    {
        if (Pts[endIndex].X == Pts[ctrlIndex].X)
        {
            dstPt.X = Pts[endIndex].X;
        }

        if (Pts[endIndex].Y == Pts[ctrlIndex].Y)
        {
            dstPt.Y = Pts[endIndex].Y;
        }
    }

    // give up when changing t no longer moves point
    // also, copy point rather than recompute it when it does change
    public readonly double BinarySearch(double min, double max, double axisIntercept, SearchAxis xAxis)
    {
        double t = (min + max) / 2;
        double step = (t - min) / 2;
        SkDPoint cubicAtT = PtAtT(t);
        double calcPos = SkPathOpsCubicStatics.Axis(cubicAtT, xAxis);
        double calcDist = calcPos - axisIntercept;
        do
        {
            double priorT = SkPathOps.StdMax(min, t - step);
            SkDPoint lessPt = PtAtT(priorT);
            if (SkPathOps.ApproximatelyEqualHalf(lessPt.X, cubicAtT.X)
                && SkPathOps.ApproximatelyEqualHalf(lessPt.Y, cubicAtT.Y))
            {
                return -1; // binary search found no point at this axis intercept
            }

            double lessDist = SkPathOpsCubicStatics.Axis(lessPt, xAxis) - axisIntercept;
            double lastStep = step;
            step /= 2;
            if (calcDist > 0 ? calcDist > lessDist : calcDist < lessDist)
            {
                t = priorT;
            }
            else
            {
                double nextT = t + lastStep;
                if (nextT > max)
                {
                    return -1;
                }

                SkDPoint morePt = PtAtT(nextT);
                if (SkPathOps.ApproximatelyEqualHalf(morePt.X, cubicAtT.X)
                    && SkPathOps.ApproximatelyEqualHalf(morePt.Y, cubicAtT.Y))
                {
                    return -1; // binary search found no point at this axis intercept
                }

                double moreDist = SkPathOpsCubicStatics.Axis(morePt, xAxis) - axisIntercept;
                if (calcDist > 0 ? calcDist <= moreDist : calcDist >= moreDist)
                {
                    continue;
                }

                t = nextT;
            }

            SkDPoint testAtT = PtAtT(t);
            cubicAtT = testAtT;
            calcPos = SkPathOpsCubicStatics.Axis(cubicAtT, xAxis);
            calcDist = calcPos - axisIntercept;
        } while (!SkPathOps.ApproximatelyEqual(calcPos, axisIntercept));

        return t;
    }

    // get the rough scale of the cubic; used to determine if curvature is extreme
    public readonly double CalcPrecision()
    {
        return ((Pts[1] - Pts[0]).Length()
            + (Pts[2] - Pts[1]).Length()
            + (Pts[3] - Pts[2]).Length()) / GPrecisionUnit;
    }

    public readonly SkDCubicPair ChopAt(double t)
    {
        SkDCubicPair dst = default;
        if (t == 0.5)
        {
            dst.Pts[0] = Pts[0];
            dst.Pts[1].X = (Pts[0].X + Pts[1].X) / 2;
            dst.Pts[1].Y = (Pts[0].Y + Pts[1].Y) / 2;
            dst.Pts[2].X = (Pts[0].X + 2 * Pts[1].X + Pts[2].X) / 4;
            dst.Pts[2].Y = (Pts[0].Y + 2 * Pts[1].Y + Pts[2].Y) / 4;
            dst.Pts[3].X = (Pts[0].X + 3 * (Pts[1].X + Pts[2].X) + Pts[3].X) / 8;
            dst.Pts[3].Y = (Pts[0].Y + 3 * (Pts[1].Y + Pts[2].Y) + Pts[3].Y) / 8;
            dst.Pts[4].X = (Pts[1].X + 2 * Pts[2].X + Pts[3].X) / 4;
            dst.Pts[4].Y = (Pts[1].Y + 2 * Pts[2].Y + Pts[3].Y) / 4;
            dst.Pts[5].X = (Pts[2].X + Pts[3].X) / 2;
            dst.Pts[5].Y = (Pts[2].Y + Pts[3].Y) / 2;
            dst.Pts[6] = Pts[3];
            return dst;
        }

        Span<double> dstX = SkPathOps.MutableDoubles(dst.Pts);
        SkPathOpsCubicStatics.InterpCubicCoords(SkPathOps.Doubles(Pts), dstX, t);
        SkPathOpsCubicStatics.InterpCubicCoords(SkPathOps.Doubles(Pts)[1..], dstX[1..], t);
        return dst;
    }

    // TODO(skbug.com/14063) deduplicate this with SkBezierCubic::ConvertToPolynomial
    public static void Coefficients(ReadOnlySpan<double> src, out double A, out double B, out double C, out double D)
    {
        A = src[6]; // d
        B = src[4] * 3; // 3*c
        C = src[2] * 3; // 3*b
        D = src[0]; // a
        A -= D - C + B; // A =   -a + 3*b - 3*c + d
        B += 3 * D - 2 * C; // B =  3*a - 6*b + 3*c
        C -= 3 * D; // C = -3*a + 3*b
    }

    public readonly bool EndsAreExtremaInXOrY()
    {
        return (SkPathOps.Between(Pts[0].X, Pts[1].X, Pts[3].X)
                && SkPathOps.Between(Pts[0].X, Pts[2].X, Pts[3].X))
            || (SkPathOps.Between(Pts[0].Y, Pts[1].Y, Pts[3].Y)
                && SkPathOps.Between(Pts[0].Y, Pts[2].Y, Pts[3].Y));
    }

    // Do a quick reject by rotating all points relative to a line formed by
    // a pair of one cubic's points. If the 2nd cubic's points
    // are on the line or on the opposite side from the 1st cubic's 'odd man', the
    // curves at most intersect at the endpoints.
    /* if returning true, check contains true if cubic's hull collapsed, making the cubic linear
       if returning false, check contains true if the the cubic pair have only the end point in common
    */
    public readonly bool HullIntersects(ReadOnlySpan<SkDPoint> pts, int ptCount, ref bool isLinear)
    {
        bool linear = true;
        Span<sbyte> hullOrder = stackalloc sbyte[4];
        int hullCount = ConvexHull(hullOrder);
        int end1 = hullOrder[0];
        int hullIndex = 0;
        Span<SkDPoint> endPt = stackalloc SkDPoint[2];
        endPt[0] = Pts[end1];
        do
        {
            hullIndex = (hullIndex + 1) % hullCount;
            int end2 = hullOrder[hullIndex];
            endPt[1] = Pts[end2];
            double origX = endPt[0].X;
            double origY = endPt[0].Y;
            double adj = endPt[1].X - origX;
            double opp = endPt[1].Y - origY;
            int oddManMask = SkPathOps.OtherTwo(end1, end2);
            int oddMan = end1 ^ oddManMask;
            double sign = (Pts[oddMan].Y - origY) * adj - (Pts[oddMan].X - origX) * opp;
            int oddMan2 = end2 ^ oddManMask;
            double sign2 = (Pts[oddMan2].Y - origY) * adj - (Pts[oddMan2].X - origX) * opp;
            if (sign * sign2 < 0)
            {
                continue;
            }

            if (SkPathOps.ApproximatelyZero(sign))
            {
                sign = sign2;
                if (SkPathOps.ApproximatelyZero(sign))
                {
                    continue;
                }
            }

            linear = false;
            bool foundOutlier = false;
            for (int n = 0; n < ptCount; ++n)
            {
                double test = (pts[n].Y - origY) * adj - (pts[n].X - origX) * opp;
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

            endPt[0] = endPt[1];
            end1 = end2;
        } while (hullIndex != 0);

        isLinear = linear;
        return true;
    }

    public readonly bool HullIntersects(in SkDCubic c2, ref bool isLinear)
    {
        return HullIntersects(c2.Pts, SkDCubic.KPointCount, ref isLinear);
    }

    public readonly bool HullIntersects(in SkDQuad quad, ref bool isLinear)
    {
        return HullIntersects(quad.Pts, SkDQuad.KPointCount, ref isLinear);
    }

    public readonly bool HullIntersects(in SkDConic conic, ref bool isLinear)
    {
        return HullIntersects(conic.Pts, ref isLinear);
    }

    public readonly bool IsLinear(int startIndex, int endIndex)
    {
        if (Pts[0].ApproximatelyDEqual(Pts[3]))
        {
            // Skia reads the cubic's first three points as a quad.
            return new SkDQuad(Pts[0], Pts[1], Pts[2]).IsLinear(0, 2);
        }

        SkLineParameters lineParameters = default;
        lineParameters.CubicEndPoints(this, startIndex, endIndex);
        // FIXME: maybe it's possible to avoid this and compare non-normalized
        lineParameters.Normalize();
        double tiniest = SkPathOps.StdMin(
            SkPathOps.StdMin(
                SkPathOps.StdMin(
                    SkPathOps.StdMin(
                        SkPathOps.StdMin(SkPathOps.StdMin(SkPathOps.StdMin(Pts[0].X, Pts[0].Y), Pts[1].X), Pts[1].Y),
                        Pts[2].X),
                    Pts[2].Y),
                Pts[3].X),
            Pts[3].Y);
        double largest = SkPathOps.StdMax(
            SkPathOps.StdMax(
                SkPathOps.StdMax(
                    SkPathOps.StdMax(
                        SkPathOps.StdMax(SkPathOps.StdMax(SkPathOps.StdMax(Pts[0].X, Pts[0].Y), Pts[1].X), Pts[1].Y),
                        Pts[2].X),
                    Pts[2].Y),
                Pts[3].X),
            Pts[3].Y);
        largest = SkPathOps.StdMax(largest, -tiniest);
        double distance = lineParameters.ControlPtDistance(this, 1);
        if (!SkPathOps.ApproximatelyZeroWhenComparedTo(distance, largest))
        {
            return false;
        }

        distance = lineParameters.ControlPtDistance(this, 2);
        return SkPathOps.ApproximatelyZeroWhenComparedTo(distance, largest);
    }

    public static int ComplexBreak(ReadOnlySpan<PathPoint> pointsPtr, Span<float> t)
    {
        SkDCubic cubic = default;
        cubic.Set(pointsPtr);
        if (cubic.MonotonicInX() && cubic.MonotonicInY())
        {
            return 0;
        }

        Span<double> tt = stackalloc double[2];
        Span<double> ss = stackalloc double[2];
        SkCubicType cubicType = SkPathOps.SkClassifyCubic(pointsPtr, tt, ss, Span<double>.Empty);
        switch (cubicType)
        {
            case SkCubicType.Loop:
            {
                double td = tt[0], te = tt[1], sd = ss[0], se = ss[1];
                if (SkPathOps.RoughlyBetween(0, td, sd) && SkPathOps.RoughlyBetween(0, te, se))
                {
                    t[0] = (float)((td * se + te * sd) / (2 * sd * se));
                    return (t[0] > 0 && t[0] < 1) ? 1 : 0;
                }

                goto case SkCubicType.Serpentine; // fall through if no t value found
            }

            case SkCubicType.Serpentine:
            case SkCubicType.LocalCusp:
            case SkCubicType.CuspAtInfinity:
            {
                Span<double> inflectionTs = stackalloc double[2];
                int infTCount = cubic.FindInflections(inflectionTs);
                Span<double> maxCurvature = stackalloc double[3];
                int roots = cubic.FindMaxCurvature(maxCurvature);
                if (infTCount == 2)
                {
                    for (int index = 0; index < roots; ++index)
                    {
                        if (SkPathOps.Between(inflectionTs[0], maxCurvature[index], inflectionTs[1]))
                        {
                            t[0] = (float)maxCurvature[index];
                            return (t[0] > 0 && t[0] < 1) ? 1 : 0;
                        }
                    }
                }
                else
                {
                    int resultCount = 0;
                    // FIXME: constant found through experimentation -- maybe there's a better way....
                    double precision = cubic.CalcPrecision() * 2;
                    ReadOnlySpan<double> cubicPts = SkPathOps.Doubles(cubic.Pts);
                    for (int index = 0; index < roots; ++index)
                    {
                        double testT = maxCurvature[index];
                        if (0 >= testT || testT >= 1)
                        {
                            continue;
                        }

                        // don't call dxdyAtT since we want (0,0) results
                        SkDVector dPt = new SkDVector(
                            SkPathOpsCubicStatics.DerivativeAtT(cubicPts, testT),
                            SkPathOpsCubicStatics.DerivativeAtT(cubicPts[1..], testT));
                        double dPtLen = dPt.Length();
                        if (dPtLen < precision)
                        {
                            t[resultCount++] = (float)testT;
                        }
                    }

                    if (resultCount == 0 && infTCount == 1)
                    {
                        t[0] = (float)inflectionTs[0];
                        resultCount = (t[0] > 0 && t[0] < 1) ? 1 : 0;
                    }

                    return resultCount;
                }

                break;
            }

            default:
                break;
        }

        return 0;
    }

    public readonly bool MonotonicInX()
    {
        return SkPathOps.PreciselyBetween(Pts[0].X, Pts[1].X, Pts[3].X)
            && SkPathOps.PreciselyBetween(Pts[0].X, Pts[2].X, Pts[3].X);
    }

    public readonly bool MonotonicInY()
    {
        return SkPathOps.PreciselyBetween(Pts[0].Y, Pts[1].Y, Pts[3].Y)
            && SkPathOps.PreciselyBetween(Pts[0].Y, Pts[2].Y, Pts[3].Y);
    }

    public readonly void OtherPts(int index, Span<SkDPoint> o1Pts)
    {
        int offset = index == 0 ? 1 : 0;
        o1Pts[0] = Pts[offset];
        o1Pts[1] = Pts[++offset];
        o1Pts[2] = Pts[++offset];
    }

    public readonly int SearchRoots(
        Span<double> extremeTs,
        int extrema,
        double axisIntercept,
        SearchAxis xAxis,
        Span<double> validRoots)
    {
        extrema += FindInflections(extremeTs[extrema..]);
        extremeTs[extrema++] = 0;
        extremeTs[extrema] = 1;
        SkPathOps.SkTQSort(extremeTs[..(extrema + 1)]);
        int validCount = 0;
        for (int index = 0; index < extrema;)
        {
            double min = extremeTs[index];
            double max = extremeTs[++index];
            if (min == max)
            {
                continue;
            }

            double newT = BinarySearch(min, max, axisIntercept, xAxis);
            if (newT >= 0)
            {
                if (validCount >= 3)
                {
                    return 0;
                }

                validRoots[validCount++] = newT;
            }
        }

        return validCount;
    }

    // cubic roots

    // from SkGeometry.cpp (and Numeric Solutions, 5.6)
    // // TODO(skbug.com/14063) Deduplicate with SkCubics::RootsValidT
    public static int RootsValidT(double A, double B, double C, double D, Span<double> t)
    {
        Span<double> s = stackalloc double[3];
        int realRoots = RootsReal(A, B, C, D, s);
        int foundRoots = SkDQuad.AddValidTs(s, realRoots, t);
        for (int index = 0; index < realRoots; ++index)
        {
            double tValue = s[index];
            if (!SkPathOps.ApproximatelyOneOrLess(tValue) && SkPathOps.Between(1, tValue, 1.00005))
            {
                for (int idx2 = 0; idx2 < foundRoots; ++idx2)
                {
                    if (SkPathOps.ApproximatelyEqual(t[idx2], 1))
                    {
                        goto nextRoot;
                    }
                }

                t[foundRoots++] = 1;
            }
            else if (!SkPathOps.ApproximatelyZeroOrMore(tValue) && SkPathOps.Between(-0.00005, tValue, 0))
            {
                for (int idx2 = 0; idx2 < foundRoots; ++idx2)
                {
                    if (SkPathOps.ApproximatelyEqual(t[idx2], 0))
                    {
                        goto nextRoot;
                    }
                }

                t[foundRoots++] = 0;
            }

        nextRoot:
            ;
        }

        return foundRoots;
    }

    // TODO(skbug.com/14063) Deduplicate with SkCubics::RootsReal
    public static int RootsReal(double A, double B, double C, double D, Span<double> s)
    {
        if (SkPathOps.ApproximatelyZero(A)
            && SkPathOps.ApproximatelyZeroWhenComparedTo(A, B)
            && SkPathOps.ApproximatelyZeroWhenComparedTo(A, C)
            && SkPathOps.ApproximatelyZeroWhenComparedTo(A, D))
        {
            // we're just a quadratic
            return SkDQuad.RootsReal(B, C, D, s);
        }

        if (SkPathOps.ApproximatelyZeroWhenComparedTo(D, A)
            && SkPathOps.ApproximatelyZeroWhenComparedTo(D, B)
            && SkPathOps.ApproximatelyZeroWhenComparedTo(D, C))
        {
            // 0 is one root
            int num = SkDQuad.RootsReal(A, B, C, s);
            for (int i = 0; i < num; ++i)
            {
                if (SkPathOps.ApproximatelyZero(s[i]))
                {
                    return num;
                }
            }

            s[num++] = 0;
            return num;
        }

        if (SkPathOps.ApproximatelyZero(A + B + C + D))
        {
            // 1 is one root
            int num = SkDQuad.RootsReal(A, A + B, -D, s);
            for (int i = 0; i < num; ++i)
            {
                if (SkPathOps.AlmostDequalUlps(s[i], 1))
                {
                    return num;
                }
            }

            s[num++] = 1;
            return num;
        }

        double a, b, c;
        {
            double invA = 1 / A;
            a = B * invA;
            b = C * invA;
            c = D * invA;
        }
        double a2 = a * a;
        double Q = (a2 - b * 3) / 9;
        double R = (2 * a2 * a - 9 * a * b + 27 * c) / 54;
        double R2 = R * R;
        double Q3 = Q * Q * Q;
        double R2MinusQ3 = R2 - Q3;
        double adiv3 = a / 3;
        double r;
        int roots = 0; // Skia's double* roots = s, as an index
        if (R2MinusQ3 < 0)
        {
            // we have 3 real roots
            // the divide/root can, due to finite precisions, be slightly outside of -1...1
            double theta = Math.Acos(SkPathOps.SkTPin(R / Math.Sqrt(Q3), -1.0, 1.0));
            double neg2RootQ = -2 * Math.Sqrt(Q);

            r = neg2RootQ * Math.Cos(theta / 3) - adiv3;
            s[roots++] = r;

            r = neg2RootQ * Math.Cos((theta + 2 * SkPathOpsCubicStatics.PI) / 3) - adiv3;
            if (!SkPathOps.AlmostDequalUlps(s[0], r))
            {
                s[roots++] = r;
            }

            r = neg2RootQ * Math.Cos((theta - 2 * SkPathOpsCubicStatics.PI) / 3) - adiv3;
            if (!SkPathOps.AlmostDequalUlps(s[0], r) && (roots == 1 || !SkPathOps.AlmostDequalUlps(s[1], r)))
            {
                s[roots++] = r;
            }
        }
        else
        {
            // we have 1 real root
            double sqrtR2MinusQ3 = Math.Sqrt(R2MinusQ3);
            A = Math.Abs(R) + sqrtR2MinusQ3;
            A = Math.Cbrt(A); // cube root
            if (R > 0)
            {
                A = -A;
            }

            if (A != 0)
            {
                A += Q / A;
            }

            r = A - adiv3;
            s[roots++] = r;
            if (SkPathOps.AlmostDequalUlps((double)R2, (double)Q3))
            {
                r = -A / 2 - adiv3;
                if (!SkPathOps.AlmostDequalUlps(s[0], r))
                {
                    s[roots++] = r;
                }
            }
        }

        return roots;
    }

    // OPTIMIZE? compute t^2, t(1-t), and (1-t)^2 and pass them to another version of derivative at t?
    public readonly SkDVector DxdyAtT(double t)
    {
        ReadOnlySpan<double> fPts = SkPathOps.Doubles(Pts);
        SkDVector result = new SkDVector(
            SkPathOpsCubicStatics.DerivativeAtT(fPts, t),
            SkPathOpsCubicStatics.DerivativeAtT(fPts[1..], t));
        if (result.X == 0 && result.Y == 0)
        {
            if (t == 0)
            {
                result = Pts[2] - Pts[0];
            }
            else if (t == 1)
            {
                result = Pts[3] - Pts[1];
            }

            // else: incomplete (Skia only logs "!c")
            if (result.X == 0 && result.Y == 0 && SkPathOps.ZeroOrOne(t))
            {
                result = Pts[3] - Pts[0];
            }
        }

        return result;
    }

    // OPTIMIZE? share code with formulate_F1DotF2
    // e.g. https://stackoverflow.com/a/35927917
    public readonly int FindInflections(Span<double> tValues)
    {
        double Ax = Pts[1].X - Pts[0].X;
        double Ay = Pts[1].Y - Pts[0].Y;
        double Bx = Pts[2].X - 2 * Pts[1].X + Pts[0].X;
        double By = Pts[2].Y - 2 * Pts[1].Y + Pts[0].Y;
        double Cx = Pts[3].X + 3 * (Pts[1].X - Pts[2].X) - Pts[0].X;
        double Cy = Pts[3].Y + 3 * (Pts[1].Y - Pts[2].Y) - Pts[0].Y;
        return SkDQuad.RootsValidT(Bx * Cy - By * Cx, Ax * Cy - Ay * Cx, Ax * By - Ay * Bx, tValues);
    }

    /** SkDCubic'(t) = At^2 + Bt + C, where
        A = 3(-a + 3(b - c) + d)
        B = 6(a - 2b + c)
        C = 3(b - a)
        Solve for t, keeping only those that fit between 0 < t < 1
    */
    public static int FindExtrema(ReadOnlySpan<double> src, Span<double> tValues)
    {
        // we divide A,B,C by 3 to simplify
        double a = src[0];
        double b = src[2];
        double c = src[4];
        double d = src[6];
        double A = d - a + 3 * (b - c);
        double B = 2 * (a - b - b + c);
        double C = b - a;

        return SkDQuad.RootsValidT(A, B, C, tValues);
    }

    /*  from SkGeometry.cpp
        Looking for F' dot F'' == 0

        A = b - a
        B = c - 2b + a
        C = d - 3c + 3b - a

        F' = 3Ct^2 + 6Bt + 3A
        F'' = 6Ct + 6B

        F' dot F'' -> CCt^3 + 3BCt^2 + (2BB + CA)t + AB
    */
    public readonly int FindMaxCurvature(Span<double> tValues)
    {
        Span<double> coeffX = stackalloc double[4];
        Span<double> coeffY = stackalloc double[4];
        int i;
        SkPathOpsCubicStatics.FormulateF1DotF2(SkPathOps.Doubles(Pts), coeffX);
        SkPathOpsCubicStatics.FormulateF1DotF2(SkPathOps.Doubles(Pts)[1..], coeffY);
        for (i = 0; i < 4; i++)
        {
            coeffX[i] = coeffX[i] + coeffY[i];
        }

        return RootsValidT(coeffX[0], coeffX[1], coeffX[2], coeffX[3], tValues);
    }

    public readonly SkDPoint PtAtT(double t)
    {
        if (0 == t)
        {
            return Pts[0];
        }

        if (1 == t)
        {
            return Pts[3];
        }

        double one_t = 1 - t;
        double one_t2 = one_t * one_t;
        double a = one_t2 * one_t;
        double b = 3 * one_t2 * t;
        double t2 = t * t;
        double c = 3 * one_t * t2;
        double d = t2 * t;
        SkDPoint result = new SkDPoint(
            a * Pts[0].X + b * Pts[1].X + c * Pts[2].X + d * Pts[3].X,
            a * Pts[0].Y + b * Pts[1].Y + c * Pts[2].Y + d * Pts[3].Y);
        return result;
    }

    /*
     Given a cubic c, t1, and t2, find a small cubic segment.

     The new cubic is defined as points A, B, C, and D, where
     s1 = 1 - t1
     s2 = 1 - t2
     A = c[0]*s1*s1*s1 + 3*c[1]*s1*s1*t1 + 3*c[2]*s1*t1*t1 + c[3]*t1*t1*t1
     D = c[0]*s2*s2*s2 + 3*c[1]*s2*s2*t2 + 3*c[2]*s2*t2*t2 + c[3]*t2*t2*t2

     We don't have B or C. So We define two equations to isolate them.
     First, compute two reference T values 1/3 and 2/3 from t1 to t2:

     c(at (2*t1 + t2)/3) == E
     c(at (t1 + 2*t2)/3) == F

     Next, compute where those values must be if we know the values of B and C:

     _12   =  A*2/3 + B*1/3
     12_   =  A*1/3 + B*2/3
     _23   =  B*2/3 + C*1/3
     23_   =  B*1/3 + C*2/3
     _34   =  C*2/3 + D*1/3
     34_   =  C*1/3 + D*2/3
     _123  = (A*2/3 + B*1/3)*2/3 + (B*2/3 + C*1/3)*1/3 = A*4/9 + B*4/9 + C*1/9
     123_  = (A*1/3 + B*2/3)*1/3 + (B*1/3 + C*2/3)*2/3 = A*1/9 + B*4/9 + C*4/9
     _234  = (B*2/3 + C*1/3)*2/3 + (C*2/3 + D*1/3)*1/3 = B*4/9 + C*4/9 + D*1/9
     234_  = (B*1/3 + C*2/3)*1/3 + (C*1/3 + D*2/3)*2/3 = B*1/9 + C*4/9 + D*4/9
     _1234 = (A*4/9 + B*4/9 + C*1/9)*2/3 + (B*4/9 + C*4/9 + D*1/9)*1/3
           =  A*8/27 + B*12/27 + C*6/27 + D*1/27
           =  E
     1234_ = (A*1/9 + B*4/9 + C*4/9)*1/3 + (B*1/9 + C*4/9 + D*4/9)*2/3
           =  A*1/27 + B*6/27 + C*12/27 + D*8/27
           =  F
     E*27  =  A*8    + B*12   + C*6     + D
     F*27  =  A      + B*6    + C*12    + D*8

    Group the known values on one side:

     M       = E*27 - A*8 - D     = B*12 + C* 6
     N       = F*27 - A   - D*8   = B* 6 + C*12
     M*2 - N = B*18
     N*2 - M = C*18
     B       = (M*2 - N)/18
     C       = (N*2 - M)/18
     */
    public readonly SkDCubic SubDivide(double t1, double t2)
    {
        if (t1 == 0 || t2 == 1)
        {
            if (t1 == 0 && t2 == 1)
            {
                return this;
            }

            SkDCubicPair pair = ChopAt(t1 == 0 ? t2 : t1);
            SkDCubic half = t1 == 0 ? pair.First() : pair.Second(); // Skia's inner dst
            return half;
        }

        ReadOnlySpan<double> fPtsX = SkPathOps.Doubles(Pts);
        ReadOnlySpan<double> fPtsY = fPtsX[1..];
        SkDCubic dst = default;
        double ax = dst[0].X = SkPathOpsCubicStatics.InterpCubicCoords(fPtsX, t1);
        double ay = dst[0].Y = SkPathOpsCubicStatics.InterpCubicCoords(fPtsY, t1);
        double ex = SkPathOpsCubicStatics.InterpCubicCoords(fPtsX, (t1 * 2 + t2) / 3);
        double ey = SkPathOpsCubicStatics.InterpCubicCoords(fPtsY, (t1 * 2 + t2) / 3);
        double fx = SkPathOpsCubicStatics.InterpCubicCoords(fPtsX, (t1 + t2 * 2) / 3);
        double fy = SkPathOpsCubicStatics.InterpCubicCoords(fPtsY, (t1 + t2 * 2) / 3);
        double dx = dst[3].X = SkPathOpsCubicStatics.InterpCubicCoords(fPtsX, t2);
        double dy = dst[3].Y = SkPathOpsCubicStatics.InterpCubicCoords(fPtsY, t2);
        double mx = ex * 27 - ax * 8 - dx;
        double my = ey * 27 - ay * 8 - dy;
        double nx = fx * 27 - ax - dx * 8;
        double ny = fy * 27 - ay - dy * 8;
        /* bx = */
        dst[1].X = (mx * 2 - nx) / 18;
        /* by = */
        dst[1].Y = (my * 2 - ny) / 18;
        /* cx = */
        dst[2].X = (nx * 2 - mx) / 18;
        /* cy = */
        dst[2].Y = (ny * 2 - my) / 18;
        // FIXME: call align() ?
        return dst;
    }

    public readonly void SubDivide(in SkDPoint a, in SkDPoint d, double t1, double t2, Span<SkDPoint> dst)
    {
        // this approach assumes that the control points computed directly are accurate enough
        SkDCubic sub = SubDivide(t1, t2);
        dst[0] = sub[1] + (a - sub[0]);
        dst[1] = sub[2] + (d - sub[3]);
        if (t1 == 0 || t2 == 0)
        {
            Align(0, 1, ref dst[t1 == 0 ? 0 : 1]);
        }

        if (t1 == 1 || t2 == 1)
        {
            Align(3, 2, ref dst[t1 == 1 ? 0 : 1]);
        }

        if (SkPathOps.AlmostBequalUlps(dst[0].X, a.X))
        {
            dst[0].X = a.X;
        }

        if (SkPathOps.AlmostBequalUlps(dst[0].Y, a.Y))
        {
            dst[0].Y = a.Y;
        }

        if (SkPathOps.AlmostBequalUlps(dst[1].X, d.X))
        {
            dst[1].X = d.X;
        }

        if (SkPathOps.AlmostBequalUlps(dst[1].Y, d.Y))
        {
            dst[1].Y = d.Y;
        }
    }

    public readonly bool ToFloatPoints(Span<PathPoint> pts)
    {
        ReadOnlySpan<double> dCubic = SkPathOps.Doubles(Pts);
        Span<float> cubic = stackalloc float[KPointCount * 2];
        for (int index = 0; index < KPointCount * 2; ++index)
        {
            cubic[index] = SkPathOps.SkDoubleToScalar(dCubic[index]);
            if (SkPathOps.SkScalarAbs(cubic[index]) < SkPathOps.FltEpsilonOrderableErr)
            {
                cubic[index] = 0;
            }
        }

        // Skia writes the floats straight into pts; PathPoint is immutable, so they are stored per point.
        for (int index = 0; index < KPointCount; ++index)
        {
            pts[index] = new PathPoint(cubic[index * 2], cubic[index * 2 + 1]);
        }

        return SkPathOps.SkScalarsAreFinite(cubic, KPointCount * 2);
    }

    public readonly double Top(in SkDCubic dCurve, double startT, double endT, ref SkDPoint topPt)
    {
        Span<double> extremeTs = stackalloc double[2];
        double topT = -1;
        int roots = SkDCubic.FindExtrema(SkPathOps.Doubles(Pts)[1..], extremeTs);
        for (int index = 0; index < roots; ++index)
        {
            double t = startT + (endT - startT) * extremeTs[index];
            SkDPoint mid = dCurve.PtAtT(t);
            if (topPt.Y > mid.Y || (topPt.Y == mid.Y && topPt.X > mid.X))
            {
                topT = t;
                topPt = mid;
            }
        }

        return topT;
    }
}

internal static partial class SkPathOps
{
    /* Given the set [0, 1, 2, 3], and two of the four members, compute an XOR mask
       that computes the other two. Note that:

       one ^ two == 3 for (0, 3), (1, 2)
       one ^ two <  3 for (0, 1), (0, 2), (1, 3), (2, 3)
       3 - (one ^ two) is either 0, 1, or 2
       1 >> (3 - (one ^ two)) is either 0 or 1
    thus:
       returned == 2 for (0, 3), (1, 2)
       returned == 3 for (0, 1), (0, 2), (1, 3), (2, 3)
    given that:
       (0, 3) ^ 2 -> (2, 1)  (1, 2) ^ 2 -> (3, 0)
       (0, 1) ^ 3 -> (3, 2)  (0, 2) ^ 3 -> (3, 1)  (1, 3) ^ 3 -> (2, 0)  (2, 3) ^ 3 -> (1, 0)
    */
    public static int OtherTwo(int one, int two)
    {
        return (1 >> (3 - (one ^ two))) ^ 3;
    }
}

internal struct SkDCubicPair
{
    public SkDPoint7 Pts;

    // Skia reinterprets pts[0..3] / pts[3..6] as an SkDCubic.
    public readonly SkDCubic First()
    {
        return new SkDCubic(Pts[0], Pts[1], Pts[2], Pts[3]);
    }

    public readonly SkDCubic Second()
    {
        return new SkDCubic(Pts[3], Pts[4], Pts[5], Pts[6]);
    }
}

internal sealed class SkTCubic : SkTCurve
{
    public SkDCubic Cubic;

    public SkTCubic()
    {
    }

    public SkTCubic(in SkDCubic c)
    {
        Cubic = c;
    }

    public override ref SkDPoint this[int n] => ref Cubic[n];

    public override bool Collapsed()
    {
        return Cubic.Collapsed();
    }

    public override bool ControlsInside()
    {
        return Cubic.ControlsInside();
    }

    public override SkDVector DxdyAtT(double t)
    {
        return Cubic.DxdyAtT(t);
    }

    public override bool HullIntersects(in SkDQuad quad, ref bool isLinear)
    {
        return quad.HullIntersects(Cubic, ref isLinear);
    }

    public override bool HullIntersects(in SkDConic conic, ref bool isLinear)
    {
        return conic.HullIntersects(Cubic, ref isLinear);
    }

    public override bool HullIntersects(in SkDCubic cubic, ref bool isLinear)
    {
        return cubic.HullIntersects(Cubic, ref isLinear);
    }

    public override bool HullIntersects(SkTCurve curve, ref bool isLinear)
    {
        return curve.HullIntersects(Cubic, ref isLinear);
    }

    public override int IntersectRay(SkIntersections i, in SkDLine line)
    {
        return i.IntersectRay(Cubic, line);
    }

    public override bool IsConic()
    {
        return false;
    }

    public override SkTCurve Make()
    {
        return new SkTCubic();
    }

    public override int MaxIntersections()
    {
        return SkDCubic.KMaxIntersections;
    }

    public override void OtherPts(int oddMan, Span<SkDPoint> endPt)
    {
        Cubic.OtherPts(oddMan, endPt);
    }

    public override int PointCount()
    {
        return SkDCubic.KPointCount;
    }

    public override int PointLast()
    {
        return SkDCubic.KPointLast;
    }

    public override SkDPoint PtAtT(double t)
    {
        return Cubic.PtAtT(t);
    }

    public override void SetBounds(ref SkDRect rect)
    {
        rect.SetBounds(Cubic);
    }

    public override void SubDivide(double t1, double t2, SkTCurve curve)
    {
        ((SkTCubic)curve).Cubic = Cubic.SubDivide(t1, t2);
    }
}

file static class SkPathOpsCubicStatics
{
    public const double PI = 3.141592653589793;

    // Skia's (&pt.fX)[xAxis].
    public static double Axis(in SkDPoint pt, SkDCubic.SearchAxis xAxis)
    {
        return xAxis == SkDCubic.SearchAxis.XAxis ? pt.X : pt.Y;
    }

    /* classic one t subdivision */
    public static void InterpCubicCoords(ReadOnlySpan<double> src, Span<double> dst, double t)
    {
        double ab = SkPathOps.SkDInterp(src[0], src[2], t);
        double bc = SkPathOps.SkDInterp(src[2], src[4], t);
        double cd = SkPathOps.SkDInterp(src[4], src[6], t);
        double abc = SkPathOps.SkDInterp(ab, bc, t);
        double bcd = SkPathOps.SkDInterp(bc, cd, t);
        double abcd = SkPathOps.SkDInterp(abc, bcd, t);

        dst[0] = src[0];
        dst[2] = ab;
        dst[4] = abc;
        dst[6] = abcd;
        dst[8] = bcd;
        dst[10] = cd;
        dst[12] = src[6];
    }

    public static double InterpCubicCoords(ReadOnlySpan<double> src, double t)
    {
        double ab = SkPathOps.SkDInterp(src[0], src[2], t);
        double bc = SkPathOps.SkDInterp(src[2], src[4], t);
        double cd = SkPathOps.SkDInterp(src[4], src[6], t);
        double abc = SkPathOps.SkDInterp(ab, bc, t);
        double bcd = SkPathOps.SkDInterp(bc, cd, t);
        double abcd = SkPathOps.SkDInterp(abc, bcd, t);
        return abcd;
    }

    // from http://www.cs.sunysb.edu/~qin/courses/geometry/4.pdf
    // c(t)  = a(1-t)^3 + 3bt(1-t)^2 + 3c(1-t)t^2 + dt^3
    // c'(t) = -3a(1-t)^2 + 3b((1-t)^2 - 2t(1-t)) + 3c(2t(1-t) - t^2) + 3dt^2
    //       = 3(b-a)(1-t)^2 + 6(c-b)t(1-t) + 3(d-c)t^2
    public static double DerivativeAtT(ReadOnlySpan<double> src, double t)
    {
        double one_t = 1 - t;
        double a = src[0];
        double b = src[2];
        double c = src[4];
        double d = src[6];
        return 3 * ((b - a) * one_t * one_t + 2 * (c - b) * t * one_t + (d - c) * t * t);
    }

    public static void FormulateF1DotF2(ReadOnlySpan<double> src, Span<double> coeff)
    {
        double a = src[2] - src[0];
        double b = src[4] - 2 * src[2] + src[0];
        double c = src[6] + 3 * (src[2] - src[4]) - src[0];
        coeff[0] = c * c;
        coeff[1] = 3 * b * c;
        coeff[2] = 2 * b * b + c * a;
        coeff[3] = a * b;
    }
}
