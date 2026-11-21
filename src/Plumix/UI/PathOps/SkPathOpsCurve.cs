using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkPathOpsCurve.h and SkPathOpsCurve.cpp at chrome/m119 (the
// Skia inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double
// types are kept. SkDCurve is a C++ union, so its members overlap here too (a conic's weight shares bytes with the
// cubic's last point, as in Skia). The header's function-pointer tables (CurvePointAtT, CurveDSlopeAtT, ...) are
// static arrays of delegates in SkPathOps, indexed by PathVerb as Skia indexes them by SkPath::Verb; the kMove_Verb
// slot is null as Skia's nullptr is. SkDCurve's conicTop/cubicTop/lineTop/quadTop and the Top[] table are declared
// in Skia but never defined, so they are not ported.

internal struct SkOpCurve
{
    public PathPoint4 Pts;
    public float Weight;

    [UnscopedRef]
    public ref PathPoint this[int n] => ref Pts[n];

    public void Set(in SkDQuad quad)
    {
        for (int index = 0; index < SkDQuad.KPointCount; ++index)
        {
            Pts[index] = quad.Pts[index].AsSkPoint();
        }
    }

    public void Set(in SkDCubic cubic)
    {
        for (int index = 0; index < SkDCubic.KPointCount; ++index)
        {
            Pts[index] = cubic.Pts[index].AsSkPoint();
        }
    }
}

[StructLayout(LayoutKind.Explicit)]
internal struct SkDCurve
{
    [FieldOffset(0)]
    public SkDLine Line;

    [FieldOffset(0)]
    public SkDQuad Quad;

    [FieldOffset(0)]
    public SkDConic Conic;

    [FieldOffset(0)]
    public SkDCubic Cubic;

    [UnscopedRef]
    public ref SkDPoint this[int n] => ref Cubic[n];

    // SkPathOpsCurve.cpp

    // this cheats and assumes that the perpendicular to the point is the closest ray to the curve
    // this case (where the line and the curve are nearly coincident) may be the only case that counts
    public readonly double NearPoint(PathVerb verb, in SkDPoint xy, in SkDPoint opp)
    {
        int count = SkPathOps.SkPathOpsVerbToPoints(verb);
        double minX = Cubic.Pts[0].X;
        double maxX = minX;
        for (int index = 1; index <= count; ++index)
        {
            minX = SkPathOps.StdMin(minX, Cubic.Pts[index].X);
            maxX = SkPathOps.StdMax(maxX, Cubic.Pts[index].X);
        }

        if (!SkPathOps.AlmostBetweenUlps(minX, xy.X, maxX))
        {
            return -1;
        }

        double minY = Cubic.Pts[0].Y;
        double maxY = minY;
        for (int index = 1; index <= count; ++index)
        {
            minY = SkPathOps.StdMin(minY, Cubic.Pts[index].Y);
            maxY = SkPathOps.StdMax(maxY, Cubic.Pts[index].Y);
        }

        if (!SkPathOps.AlmostBetweenUlps(minY, xy.Y, maxY))
        {
            return -1;
        }

        SkIntersections i = new SkIntersections();
        SkDLine perp = new SkDLine(xy, new SkDPoint(xy.X + opp.Y - xy.Y, xy.Y + xy.X - opp.X));
        SkPathOps.CurveDIntersectRay[(int)verb]!(this, perp, i);
        int minIndex = -1;
        double minDist = SkPathOps.SkScalarMax; // FLT_MAX
        for (int index = 0; index < i.Used(); ++index)
        {
            double dist = xy.Distance(i.Pt(index));
            if (minDist > dist)
            {
                minDist = dist;
                minIndex = index;
            }
        }

        if (minIndex < 0)
        {
            return -1;
        }

        double largest = SkPathOps.StdMax(SkPathOps.StdMax(maxX, maxY), -SkPathOps.StdMin(minX, minY));
        if (!SkPathOps.AlmostEqualUlpsPin(largest, largest + minDist))
        {
            // is distance within ULPS tolerance?
            return -1;
        }

        return SkPathOps.SkPinT(i[0][minIndex]);
    }

    public readonly void SetConicBounds(
        ReadOnlySpan<PathPoint> curve,
        float curveWeight,
        double tStart,
        double tEnd,
        out SkPathOpsBounds bounds)
    {
        SkDConic dCurve = default;
        dCurve.Set(curve, curveWeight);
        SkDRect dRect = default;
        dRect.SetBounds(dCurve, Conic, tStart, tEnd);
        bounds = default;
        bounds.SetLTRB(
            SkPathOps.SkDoubleToScalar(dRect.Left),
            SkPathOps.SkDoubleToScalar(dRect.Top),
            SkPathOps.SkDoubleToScalar(dRect.Right),
            SkPathOps.SkDoubleToScalar(dRect.Bottom));
    }

    public readonly void SetCubicBounds(
        ReadOnlySpan<PathPoint> curve,
        float weight,
        double tStart,
        double tEnd,
        out SkPathOpsBounds bounds)
    {
        SkDCubic dCurve = default;
        dCurve.Set(curve);
        SkDRect dRect = default;
        dRect.SetBounds(dCurve, Cubic, tStart, tEnd);
        bounds = default;
        bounds.SetLTRB(
            SkPathOps.SkDoubleToScalar(dRect.Left),
            SkPathOps.SkDoubleToScalar(dRect.Top),
            SkPathOps.SkDoubleToScalar(dRect.Right),
            SkPathOps.SkDoubleToScalar(dRect.Bottom));
    }

    public readonly void SetQuadBounds(
        ReadOnlySpan<PathPoint> curve,
        float weight,
        double tStart,
        double tEnd,
        out SkPathOpsBounds bounds)
    {
        SkDQuad dCurve = default;
        dCurve.Set(curve);
        SkDRect dRect = default;
        dRect.SetBounds(dCurve, Quad, tStart, tEnd);
        bounds = default;
        bounds.SetLTRB(
            SkPathOps.SkDoubleToScalar(dRect.Left),
            SkPathOps.SkDoubleToScalar(dRect.Top),
            SkPathOps.SkDoubleToScalar(dRect.Right),
            SkPathOps.SkDoubleToScalar(dRect.Bottom));
    }
}

internal struct SkDCurveSweep
{
    public SkDCurve Curve;
    public SkDVector2 Sweep;
    private bool _isCurve;
    private bool _ordered; // cleared when a cubic's control point isn't between the sweep vectors

    public readonly bool IsCurve()
    {
        return _isCurve;
    }

    public readonly bool IsOrdered()
    {
        return _ordered;
    }

    // SkPathOpsCurve.cpp

    public void SetCurveHullSweep(PathVerb verb)
    {
        _ordered = true;
        Sweep[0] = Curve[1] - Curve[0];
        if (PathVerb.Line == verb)
        {
            Sweep[1] = Sweep[0];
            _isCurve = false;
            return;
        }

        Sweep[1] = Curve[2] - Curve[0];
        // OPTIMIZE: I do the following float check a lot -- probably need a
        // central place for this val-is-small-compared-to-curve check
        double maxVal = 0;
        for (int index = 0; index <= SkPathOps.SkPathOpsVerbToPoints(verb); ++index)
        {
            maxVal = SkPathOps.StdMax(
                maxVal,
                SkPathOps.StdMax(SkPathOps.SkTAbs(Curve[index].X), SkPathOps.SkTAbs(Curve[index].Y)));
        }

        {
            if (PathVerb.Cubic != verb)
            {
                if (SkPathOps.RoughlyZeroWhenComparedTo(Sweep[0].X, maxVal)
                    && SkPathOps.RoughlyZeroWhenComparedTo(Sweep[0].Y, maxVal))
                {
                    Sweep[0] = Sweep[1];
                }

                goto setIsCurve;
            }

            SkDVector thirdSweep = Curve[3] - Curve[0];
            if (Sweep[0].X == 0 && Sweep[0].Y == 0)
            {
                Sweep[0] = Sweep[1];
                Sweep[1] = thirdSweep;
                if (SkPathOps.RoughlyZeroWhenComparedTo(Sweep[0].X, maxVal)
                    && SkPathOps.RoughlyZeroWhenComparedTo(Sweep[0].Y, maxVal))
                {
                    Sweep[0] = Sweep[1];
                    Curve[1] = Curve[3];
                }

                goto setIsCurve;
            }

            double s1x3 = Sweep[0].CrossCheck(thirdSweep);
            double s3x2 = thirdSweep.CrossCheck(Sweep[1]);
            if (s1x3 * s3x2 >= 0)
            {
                // if third vector is on or between first two vectors
                goto setIsCurve;
            }

            double s2x1 = Sweep[1].CrossCheck(Sweep[0]);
            // FIXME: If the sweep of the cubic is greater than 180 degrees, we're in trouble
            // probably such wide sweeps should be artificially subdivided earlier so that never happens
            if (s3x2 * s2x1 < 0)
            {
                Sweep[0] = Sweep[1];
                _ordered = false;
            }

            Sweep[1] = thirdSweep;
        }

    setIsCurve:
        _isCurve = Sweep[0].CrossCheck(Sweep[1]) != 0;
    }
}

// Skia's SkPoint fPts[4] (SkOpCurve).
[InlineArray(4)]
internal struct PathPoint4
{
    private PathPoint _element0;
}

// Skia's SkDVector fSweep[2] (SkDCurveSweep).
[InlineArray(2)]
internal struct SkDVector2
{
    private SkDVector _element0;
}

// The signatures of the header's function-pointer tables.
internal delegate SkDPoint CurveDPointAtTFn(ReadOnlySpan<PathPoint> a, float weight, double t);

internal delegate SkDPoint CurveDDPointAtTFn(in SkDCurve c, double t);

internal delegate PathPoint CurvePointAtTFn(ReadOnlySpan<PathPoint> a, float weight, double t);

internal delegate SkDVector CurveDSlopeAtTFn(ReadOnlySpan<PathPoint> a, float weight, double t);

internal delegate SkDVector CurveDDSlopeAtTFn(in SkDCurve c, double t);

internal delegate PathPoint CurveSlopeAtTFn(ReadOnlySpan<PathPoint> a, float weight, double t);

internal delegate bool CurveIsVerticalFn(ReadOnlySpan<PathPoint> a, float weight, double startT, double endT);

internal delegate void CurveIntersectRayFn(ReadOnlySpan<PathPoint> a, float weight, in SkDLine ray, SkIntersections i);

internal delegate void CurveDIntersectRayFn(in SkDCurve c, in SkDLine ray, SkIntersections i);

internal delegate int CurveInterceptFn(ReadOnlySpan<PathPoint> a, float weight, float axis, Span<double> roots);

internal static partial class SkPathOps
{
    public static SkDPoint DlineXyAtT(ReadOnlySpan<PathPoint> a, float weight, double t)
    {
        SkDLine line = default;
        line.Set(a);
        return line.PtAtT(t);
    }

    public static SkDPoint DquadXyAtT(ReadOnlySpan<PathPoint> a, float weight, double t)
    {
        SkDQuad quad = default;
        quad.Set(a);
        return quad.PtAtT(t);
    }

    public static SkDPoint DconicXyAtT(ReadOnlySpan<PathPoint> a, float weight, double t)
    {
        SkDConic conic = default;
        conic.Set(a, weight);
        return conic.PtAtT(t);
    }

    public static SkDPoint DcubicXyAtT(ReadOnlySpan<PathPoint> a, float weight, double t)
    {
        SkDCubic cubic = default;
        cubic.Set(a);
        return cubic.PtAtT(t);
    }

    public static readonly CurveDPointAtTFn?[] CurveDPointAtT =
    {
        null,
        DlineXyAtT,
        DquadXyAtT,
        DconicXyAtT,
        DcubicXyAtT,
    };

    public static SkDPoint DdlineXyAtT(in SkDCurve c, double t)
    {
        return c.Line.PtAtT(t);
    }

    public static SkDPoint DdquadXyAtT(in SkDCurve c, double t)
    {
        return c.Quad.PtAtT(t);
    }

    public static SkDPoint DdconicXyAtT(in SkDCurve c, double t)
    {
        return c.Conic.PtAtT(t);
    }

    public static SkDPoint DdcubicXyAtT(in SkDCurve c, double t)
    {
        return c.Cubic.PtAtT(t);
    }

    public static readonly CurveDDPointAtTFn?[] CurveDDPointAtT =
    {
        null,
        DdlineXyAtT,
        DdquadXyAtT,
        DdconicXyAtT,
        DdcubicXyAtT,
    };

    public static PathPoint FlineXyAtT(ReadOnlySpan<PathPoint> a, float weight, double t)
    {
        return DlineXyAtT(a, weight, t).AsSkPoint();
    }

    public static PathPoint FquadXyAtT(ReadOnlySpan<PathPoint> a, float weight, double t)
    {
        return DquadXyAtT(a, weight, t).AsSkPoint();
    }

    public static PathPoint FconicXyAtT(ReadOnlySpan<PathPoint> a, float weight, double t)
    {
        return DconicXyAtT(a, weight, t).AsSkPoint();
    }

    public static PathPoint FcubicXyAtT(ReadOnlySpan<PathPoint> a, float weight, double t)
    {
        return DcubicXyAtT(a, weight, t).AsSkPoint();
    }

    public static readonly CurvePointAtTFn?[] CurvePointAtT =
    {
        null,
        FlineXyAtT,
        FquadXyAtT,
        FconicXyAtT,
        FcubicXyAtT,
    };

    public static SkDVector DlineDxdyAtT(ReadOnlySpan<PathPoint> a, float weight, double t)
    {
        SkDLine line = default;
        line.Set(a);
        return line[1] - line[0];
    }

    public static SkDVector DquadDxdyAtT(ReadOnlySpan<PathPoint> a, float weight, double t)
    {
        SkDQuad quad = default;
        quad.Set(a);
        return quad.DxdyAtT(t);
    }

    public static SkDVector DconicDxdyAtT(ReadOnlySpan<PathPoint> a, float weight, double t)
    {
        SkDConic conic = default;
        conic.Set(a, weight);
        return conic.DxdyAtT(t);
    }

    public static SkDVector DcubicDxdyAtT(ReadOnlySpan<PathPoint> a, float weight, double t)
    {
        SkDCubic cubic = default;
        cubic.Set(a);
        return cubic.DxdyAtT(t);
    }

    public static readonly CurveDSlopeAtTFn?[] CurveDSlopeAtT =
    {
        null,
        DlineDxdyAtT,
        DquadDxdyAtT,
        DconicDxdyAtT,
        DcubicDxdyAtT,
    };

    public static SkDVector DdlineDxdyAtT(in SkDCurve c, double t)
    {
        return c.Line.Pts[1] - c.Line.Pts[0];
    }

    public static SkDVector DdquadDxdyAtT(in SkDCurve c, double t)
    {
        return c.Quad.DxdyAtT(t);
    }

    public static SkDVector DdconicDxdyAtT(in SkDCurve c, double t)
    {
        return c.Conic.DxdyAtT(t);
    }

    public static SkDVector DdcubicDxdyAtT(in SkDCurve c, double t)
    {
        return c.Cubic.DxdyAtT(t);
    }

    public static readonly CurveDDSlopeAtTFn?[] CurveDDSlopeAtT =
    {
        null,
        DdlineDxdyAtT,
        DdquadDxdyAtT,
        DdconicDxdyAtT,
        DdcubicDxdyAtT,
    };

    public static PathPoint FlineDxdyAtT(ReadOnlySpan<PathPoint> a, float weight, double t)
    {
        return a[1] - a[0];
    }

    public static PathPoint FquadDxdyAtT(ReadOnlySpan<PathPoint> a, float weight, double t)
    {
        return DquadDxdyAtT(a, weight, t).AsSkVector();
    }

    public static PathPoint FconicDxdyAtT(ReadOnlySpan<PathPoint> a, float weight, double t)
    {
        return DconicDxdyAtT(a, weight, t).AsSkVector();
    }

    public static PathPoint FcubicDxdyAtT(ReadOnlySpan<PathPoint> a, float weight, double t)
    {
        return DcubicDxdyAtT(a, weight, t).AsSkVector();
    }

    public static readonly CurveSlopeAtTFn?[] CurveSlopeAtT =
    {
        null,
        FlineDxdyAtT,
        FquadDxdyAtT,
        FconicDxdyAtT,
        FcubicDxdyAtT,
    };

    public static bool LineIsVertical(ReadOnlySpan<PathPoint> a, float weight, double startT, double endT)
    {
        SkDLine line = default;
        line.Set(a);
        Span<SkDPoint> dst = stackalloc SkDPoint[2] { line.PtAtT(startT), line.PtAtT(endT) };
        return AlmostEqualUlps(dst[0].X, dst[1].X);
    }

    public static bool QuadIsVertical(ReadOnlySpan<PathPoint> a, float weight, double startT, double endT)
    {
        SkDQuad quad = default;
        quad.Set(a);
        SkDQuad dst = quad.SubDivide(startT, endT);
        return AlmostEqualUlps(dst[0].X, dst[1].X) && AlmostEqualUlps(dst[1].X, dst[2].X);
    }

    public static bool ConicIsVertical(ReadOnlySpan<PathPoint> a, float weight, double startT, double endT)
    {
        SkDConic conic = default;
        conic.Set(a, weight);
        SkDConic dst = conic.SubDivide(startT, endT);
        return AlmostEqualUlps(dst[0].X, dst[1].X) && AlmostEqualUlps(dst[1].X, dst[2].X);
    }

    public static bool CubicIsVertical(ReadOnlySpan<PathPoint> a, float weight, double startT, double endT)
    {
        SkDCubic cubic = default;
        cubic.Set(a);
        SkDCubic dst = cubic.SubDivide(startT, endT);
        return AlmostEqualUlps(dst[0].X, dst[1].X) && AlmostEqualUlps(dst[1].X, dst[2].X)
            && AlmostEqualUlps(dst[2].X, dst[3].X);
    }

    public static readonly CurveIsVerticalFn?[] CurveIsVertical =
    {
        null,
        LineIsVertical,
        QuadIsVertical,
        ConicIsVertical,
        CubicIsVertical,
    };

    public static void LineIntersectRay(ReadOnlySpan<PathPoint> a, float weight, in SkDLine ray, SkIntersections i)
    {
        SkDLine line = default;
        line.Set(a);
        i.IntersectRay(line, ray);
    }

    public static void QuadIntersectRay(ReadOnlySpan<PathPoint> a, float weight, in SkDLine ray, SkIntersections i)
    {
        SkDQuad quad = default;
        quad.Set(a);
        i.IntersectRay(quad, ray);
    }

    public static void ConicIntersectRay(ReadOnlySpan<PathPoint> a, float weight, in SkDLine ray, SkIntersections i)
    {
        SkDConic conic = default;
        conic.Set(a, weight);
        i.IntersectRay(conic, ray);
    }

    public static void CubicIntersectRay(ReadOnlySpan<PathPoint> a, float weight, in SkDLine ray, SkIntersections i)
    {
        SkDCubic cubic = default;
        cubic.Set(a);
        i.IntersectRay(cubic, ray);
    }

    public static readonly CurveIntersectRayFn?[] CurveIntersectRay =
    {
        null,
        LineIntersectRay,
        QuadIntersectRay,
        ConicIntersectRay,
        CubicIntersectRay,
    };

    public static void DlineIntersectRay(in SkDCurve c, in SkDLine ray, SkIntersections i)
    {
        i.IntersectRay(c.Line, ray);
    }

    public static void DquadIntersectRay(in SkDCurve c, in SkDLine ray, SkIntersections i)
    {
        i.IntersectRay(c.Quad, ray);
    }

    public static void DconicIntersectRay(in SkDCurve c, in SkDLine ray, SkIntersections i)
    {
        i.IntersectRay(c.Conic, ray);
    }

    public static void DcubicIntersectRay(in SkDCurve c, in SkDLine ray, SkIntersections i)
    {
        i.IntersectRay(c.Cubic, ray);
    }

    public static readonly CurveDIntersectRayFn?[] CurveDIntersectRay =
    {
        null,
        DlineIntersectRay,
        DquadIntersectRay,
        DconicIntersectRay,
        DcubicIntersectRay,
    };

    public static int LineInterceptH(ReadOnlySpan<PathPoint> a, float weight, float y, Span<double> roots)
    {
        if (a[0].Y == a[1].Y)
        {
            return 0; // false
        }

        SkDLine line = default;
        roots[0] = SkIntersections.HorizontalIntercept(line.Set(a), y);
        return Between(0, roots[0], 1) ? 1 : 0;
    }

    public static int LineInterceptV(ReadOnlySpan<PathPoint> a, float weight, float x, Span<double> roots)
    {
        if (a[0].X == a[1].X)
        {
            return 0; // false
        }

        SkDLine line = default;
        roots[0] = SkIntersections.VerticalIntercept(line.Set(a), x);
        return Between(0, roots[0], 1) ? 1 : 0;
    }

    public static int QuadInterceptH(ReadOnlySpan<PathPoint> a, float weight, float y, Span<double> roots)
    {
        SkDQuad quad = default;
        return SkIntersections.HorizontalIntercept(quad.Set(a), y, roots);
    }

    public static int QuadInterceptV(ReadOnlySpan<PathPoint> a, float weight, float x, Span<double> roots)
    {
        SkDQuad quad = default;
        return SkIntersections.VerticalIntercept(quad.Set(a), x, roots);
    }

    public static int ConicInterceptH(ReadOnlySpan<PathPoint> a, float w, float y, Span<double> roots)
    {
        SkDConic conic = default;
        return SkIntersections.HorizontalIntercept(conic.Set(a, w), y, roots);
    }

    public static int ConicInterceptV(ReadOnlySpan<PathPoint> a, float w, float x, Span<double> roots)
    {
        SkDConic conic = default;
        return SkIntersections.VerticalIntercept(conic.Set(a, w), x, roots);
    }

    public static int CubicInterceptH(ReadOnlySpan<PathPoint> a, float weight, float y, Span<double> roots)
    {
        SkDCubic cubic = default;
        return cubic.Set(a).HorizontalIntersect(y, roots);
    }

    public static int CubicInterceptV(ReadOnlySpan<PathPoint> a, float weight, float x, Span<double> roots)
    {
        SkDCubic cubic = default;
        return cubic.Set(a).VerticalIntersect(x, roots);
    }

    // Indexed by verb * 2 + (0 for horizontal, 1 for vertical).
    public static readonly CurveInterceptFn?[] CurveIntercept =
    {
        null,
        null,
        LineInterceptH,
        LineInterceptV,
        QuadInterceptH,
        QuadInterceptV,
        ConicInterceptH,
        ConicInterceptV,
        CubicInterceptH,
        CubicInterceptV,
    };
}
