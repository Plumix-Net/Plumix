using System.Runtime.InteropServices;

namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkReduceOrder.h and SkReduceOrder.cpp at chrome/m119 (the Skia
// inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types
// are kept. SkReduceOrder is a C++ union and Skia relies on it (Quad() reduces into fQuad and reads fLine; Cubic()
// reduces into fCubic and reads fQuad), so its members overlap here too. The fields are FLine/FQuad/FCubic because
// the static Quad()/Cubic()/Conic() take the plain names. SkConic arguments are (points, weight).

[StructLayout(LayoutKind.Explicit)]
internal struct SkReduceOrder
{
    public enum Quadratics
    {
        No,
        Allow,
    }

    [FieldOffset(0)]
    public SkDLine FLine;

    [FieldOffset(0)]
    public SkDQuad FQuad;

    [FieldOffset(0)]
    public SkDCubic FCubic;

    public int Reduce(in SkDLine line)
    {
        FLine[0] = line.Pts[0];
        int different = line.Pts[0] != line.Pts[1] ? 1 : 0;
        FLine[1] = line.Pts[different];
        return 1 + different;
    }

    // reduce to a quadratic or smaller
    // look for identical points
    // look for all four points in a line
    // note that three points in a line doesn't simplify a cubic
    // look for approximation with single quadratic
    // save approximation with multiple quadratics for later
    public int Reduce(in SkDQuad quad)
    {
        int index, minX, maxX, minY, maxY;
        int minXSet, minYSet;
        minX = maxX = minY = maxY = 0;
        minXSet = minYSet = 0;
        for (index = 1; index < 3; ++index)
        {
            if (quad.Pts[minX].X > quad.Pts[index].X)
            {
                minX = index;
            }

            if (quad.Pts[minY].Y > quad.Pts[index].Y)
            {
                minY = index;
            }

            if (quad.Pts[maxX].X < quad.Pts[index].X)
            {
                maxX = index;
            }

            if (quad.Pts[maxY].Y < quad.Pts[index].Y)
            {
                maxY = index;
            }
        }

        for (index = 0; index < 3; ++index)
        {
            if (SkPathOps.AlmostEqualUlps(quad.Pts[index].X, quad.Pts[minX].X))
            {
                minXSet |= 1 << index;
            }

            if (SkPathOps.AlmostEqualUlps(quad.Pts[index].Y, quad.Pts[minY].Y))
            {
                minYSet |= 1 << index;
            }
        }

        if ((minXSet & 0x05) == 0x5 && (minYSet & 0x05) == 0x5)
        {
            // test for degenerate
            // this quad starts and ends at the same place, so never contributes
            // to the fill
            return SkReduceOrderStatics.CoincidentLine(quad, ref FQuad);
        }

        if (minXSet == 0x7)
        {
            // test for vertical line
            return SkReduceOrderStatics.VerticalLine(quad, ref FQuad);
        }

        if (minYSet == 0x7)
        {
            // test for horizontal line
            return SkReduceOrderStatics.HorizontalLine(quad, ref FQuad);
        }

        int result = SkReduceOrderStatics.CheckLinear(quad, minX, maxX, minY, maxY, ref FQuad);
        if (result != 0)
        {
            return result;
        }

        FQuad = quad;
        return 3;
    }

    /* food for thought:
    http://objectmix.com/graphics/132906-fast-precision-driven-cubic-quadratic-piecewise-degree-reduction-algos-2-a.html

    Given points c1, c2, c3 and c4 of a cubic Bezier, the points of the
    corresponding quadratic Bezier are (given in convex combinations of
    points):

    q1 = (11/13)c1 + (3/13)c2 -(3/13)c3 + (2/13)c4
    q2 = -c1 + (3/2)c2 + (3/2)c3 - c4
    q3 = (2/13)c1 - (3/13)c2 + (3/13)c3 + (11/13)c4

    Of course, this curve does not interpolate the end-points, but it would
    be interesting to see the behaviour of such a curve in an applet.

    --
    Kalle Rutanen
    http://kaba.hilvi.org

    */

    // reduce to a quadratic or smaller
    // look for identical points
    // look for all four points in a line
    // note that three points in a line doesn't simplify a cubic
    // look for approximation with single quadratic
    // save approximation with multiple quadratics for later
    public int Reduce(in SkDCubic cubic, Quadratics allowQuadratics)
    {
        int index, minX, maxX, minY, maxY;
        int minXSet, minYSet;
        minX = maxX = minY = maxY = 0;
        minXSet = minYSet = 0;
        for (index = 1; index < 4; ++index)
        {
            if (cubic.Pts[minX].X > cubic.Pts[index].X)
            {
                minX = index;
            }

            if (cubic.Pts[minY].Y > cubic.Pts[index].Y)
            {
                minY = index;
            }

            if (cubic.Pts[maxX].X < cubic.Pts[index].X)
            {
                maxX = index;
            }

            if (cubic.Pts[maxY].Y < cubic.Pts[index].Y)
            {
                maxY = index;
            }
        }

        for (index = 0; index < 4; ++index)
        {
            double cx = cubic.Pts[index].X;
            double cy = cubic.Pts[index].Y;
            double denom = SkPathOps.StdMax(
                Math.Abs(cx),
                SkPathOps.StdMax(
                    Math.Abs(cy),
                    SkPathOps.StdMax(Math.Abs(cubic.Pts[minX].X), Math.Abs(cubic.Pts[minY].Y))));
            if (denom == 0)
            {
                minXSet |= 1 << index;
                minYSet |= 1 << index;
                continue;
            }

            double inv = 1 / denom;
            if (SkPathOps.ApproximatelyEqualHalf(cx * inv, cubic.Pts[minX].X * inv))
            {
                minXSet |= 1 << index;
            }

            if (SkPathOps.ApproximatelyEqualHalf(cy * inv, cubic.Pts[minY].Y * inv))
            {
                minYSet |= 1 << index;
            }
        }

        if (minXSet == 0xF)
        {
            // test for vertical line
            if (minYSet == 0xF)
            {
                // return 1 if all four are coincident
                return SkReduceOrderStatics.CoincidentLine(cubic, ref FCubic);
            }

            return SkReduceOrderStatics.VerticalLine(cubic, ref FCubic);
        }

        if (minYSet == 0xF)
        {
            // test for horizontal line
            return SkReduceOrderStatics.HorizontalLine(cubic, ref FCubic);
        }

        int result = SkReduceOrderStatics.CheckLinear(cubic, minX, maxX, minY, maxY, ref FCubic);
        if (result != 0)
        {
            return result;
        }

        if (allowQuadratics == Quadratics.Allow
            && (result = SkReduceOrderStatics.CheckQuadratic(cubic, ref FCubic)) != 0)
        {
            return result;
        }

        FCubic = cubic;
        return 4;
    }

    public static PathVerb Quad(ReadOnlySpan<PathPoint> a, Span<PathPoint> reducePts)
    {
        SkDQuad quad = default;
        quad.Set(a);
        SkReduceOrder reducer = default;
        int order = reducer.Reduce(quad);
        if (order == 2)
        {
            // quad became line
            for (int index = 0; index < order; ++index)
            {
                reducePts[index] = reducer.FLine[index].AsSkPoint();
            }
        }

        return SkPathOps.SkPathOpsPointsToVerb(order - 1);
    }

    public static PathVerb Conic(ReadOnlySpan<PathPoint> pts, float w, Span<PathPoint> reducePts)
    {
        PathVerb verb = SkReduceOrder.Quad(pts, reducePts);
        if (verb > PathVerb.Line && w == 1)
        {
            return PathVerb.Quad;
        }

        return verb == PathVerb.Quad ? PathVerb.Conic : verb;
    }

    public static PathVerb Cubic(ReadOnlySpan<PathPoint> a, Span<PathPoint> reducePts)
    {
        if (SkDPoint.ApproximatelyEqual(a[0], a[1]) && SkDPoint.ApproximatelyEqual(a[0], a[2])
            && SkDPoint.ApproximatelyEqual(a[0], a[3]))
        {
            reducePts[0] = a[0];
            return PathVerb.Move;
        }

        SkDCubic cubic = default;
        cubic.Set(a);
        SkReduceOrder reducer = default;
        int order = reducer.Reduce(cubic, Quadratics.Allow);
        if (order == 2 || order == 3)
        {
            // cubic became line or quad
            for (int index = 0; index < order; ++index)
            {
                reducePts[index] = reducer.FQuad[index].AsSkPoint();
            }
        }

        return SkPathOps.SkPathOpsPointsToVerb(order - 1);
    }
}

file static class SkReduceOrderStatics
{
    public static int CoincidentLine(in SkDQuad quad, ref SkDQuad reduction)
    {
        reduction[0] = reduction[1] = quad.Pts[0];
        return 1;
    }

    public static int ReductionLineCount(in SkDQuad reduction)
    {
        return 1 + (reduction.Pts[0].ApproximatelyEqual(reduction.Pts[1]) ? 0 : 1);
    }

    public static int VerticalLine(in SkDQuad quad, ref SkDQuad reduction)
    {
        reduction[0] = quad.Pts[0];
        reduction[1] = quad.Pts[2];
        return ReductionLineCount(reduction);
    }

    public static int HorizontalLine(in SkDQuad quad, ref SkDQuad reduction)
    {
        reduction[0] = quad.Pts[0];
        reduction[1] = quad.Pts[2];
        return ReductionLineCount(reduction);
    }

    public static int CheckLinear(in SkDQuad quad, int minX, int maxX, int minY, int maxY, ref SkDQuad reduction)
    {
        if (!quad.IsLinear(0, 2))
        {
            return 0;
        }

        // four are colinear: return line formed by outside
        reduction[0] = quad.Pts[0];
        reduction[1] = quad.Pts[2];
        return ReductionLineCount(reduction);
    }

    public static int CoincidentLine(in SkDCubic cubic, ref SkDCubic reduction)
    {
        reduction[0] = reduction[1] = cubic.Pts[0];
        return 1;
    }

    public static int ReductionLineCount(in SkDCubic reduction)
    {
        return 1 + (reduction.Pts[0].ApproximatelyEqual(reduction.Pts[1]) ? 0 : 1);
    }

    public static int VerticalLine(in SkDCubic cubic, ref SkDCubic reduction)
    {
        reduction[0] = cubic.Pts[0];
        reduction[1] = cubic.Pts[3];
        return ReductionLineCount(reduction);
    }

    public static int HorizontalLine(in SkDCubic cubic, ref SkDCubic reduction)
    {
        reduction[0] = cubic.Pts[0];
        reduction[1] = cubic.Pts[3];
        return ReductionLineCount(reduction);
    }

    // check to see if it is a quadratic or a line
    public static int CheckQuadratic(in SkDCubic cubic, ref SkDCubic reduction)
    {
        double dx10 = cubic.Pts[1].X - cubic.Pts[0].X;
        double dx23 = cubic.Pts[2].X - cubic.Pts[3].X;
        double midX = cubic.Pts[0].X + dx10 * 3 / 2;
        double sideAx = midX - cubic.Pts[3].X;
        double sideBx = dx23 * 3 / 2;
        if (SkPathOps.ApproximatelyZero(sideAx)
            ? !SkPathOps.ApproximatelyEqual(sideAx, sideBx)
            : !SkPathOps.AlmostEqualUlpsPin(sideAx, sideBx))
        {
            return 0;
        }

        double dy10 = cubic.Pts[1].Y - cubic.Pts[0].Y;
        double dy23 = cubic.Pts[2].Y - cubic.Pts[3].Y;
        double midY = cubic.Pts[0].Y + dy10 * 3 / 2;
        double sideAy = midY - cubic.Pts[3].Y;
        double sideBy = dy23 * 3 / 2;
        if (SkPathOps.ApproximatelyZero(sideAy)
            ? !SkPathOps.ApproximatelyEqual(sideAy, sideBy)
            : !SkPathOps.AlmostEqualUlpsPin(sideAy, sideBy))
        {
            return 0;
        }

        reduction[0] = cubic.Pts[0];
        reduction[1].X = midX;
        reduction[1].Y = midY;
        reduction[2] = cubic.Pts[3];
        return 3;
    }

    public static int CheckLinear(
        in SkDCubic cubic,
        int minX,
        int maxX,
        int minY,
        int maxY,
        ref SkDCubic reduction)
    {
        if (!cubic.IsLinear(0, 3))
        {
            return 0;
        }

        // four are colinear: return line formed by outside
        reduction[0] = cubic.Pts[0];
        reduction[1] = cubic.Pts[3];
        return ReductionLineCount(reduction);
    }
}
