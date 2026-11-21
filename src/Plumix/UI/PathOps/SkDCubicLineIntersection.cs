namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkDCubicLineIntersection.cpp at chrome/m119 (the Skia inside
// SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types are
// kept. LineCubicIntersections holds copies of the cubic and line Skia references (neither changes while it
// lives). The SkDCurve cast in addLineNearEndPoints copies the cubic into an SkDCurve.

/*
Find the intersection of a line and cubic by solving for valid t values.

Analogous to line-quadratic intersection, solve line-cubic intersection by
representing the cubic as:
  x = a(1-t)^3 + 2b(1-t)^2t + c(1-t)t^2 + dt^3
  y = e(1-t)^3 + 2f(1-t)^2t + g(1-t)t^2 + ht^3
and the line as:
  y = i*x + j  (if the line is more horizontal)
or:
  x = i*y + j  (if the line is more vertical)

Then using Mathematica, solve for the values of t where the cubic intersects the
line:

  (in) Resultant[
        a*(1 - t)^3 + 3*b*(1 - t)^2*t + 3*c*(1 - t)*t^2 + d*t^3 - x,
        e*(1 - t)^3 + 3*f*(1 - t)^2*t + 3*g*(1 - t)*t^2 + h*t^3 - i*x - j, x]
  (out) -e     +   j     +
       3 e t   - 3 f t   -
       3 e t^2 + 6 f t^2 - 3 g t^2 +
         e t^3 - 3 f t^3 + 3 g t^3 - h t^3 +
     i ( a     -
       3 a t + 3 b t +
       3 a t^2 - 6 b t^2 + 3 c t^2 -
         a t^3 + 3 b t^3 - 3 c t^3 + d t^3 )

if i goes to infinity, we can rewrite the line in terms of x. Mathematica:

  (in) Resultant[
        a*(1 - t)^3 + 3*b*(1 - t)^2*t + 3*c*(1 - t)*t^2 + d*t^3 - i*y - j,
        e*(1 - t)^3 + 3*f*(1 - t)^2*t + 3*g*(1 - t)*t^2 + h*t^3 - y,       y]
  (out)  a     -   j     -
       3 a t   + 3 b t   +
       3 a t^2 - 6 b t^2 + 3 c t^2 -
         a t^3 + 3 b t^3 - 3 c t^3 + d t^3 -
     i ( e     -
       3 e t   + 3 f t   +
       3 e t^2 - 6 f t^2 + 3 g t^2 -
         e t^3 + 3 f t^3 - 3 g t^3 + h t^3 )

Solving this with Mathematica produces an expression with hundreds of terms;
instead, use Numeric Solutions recipe to solve the cubic.

The near-horizontal case, in terms of:  Ax^3 + Bx^2 + Cx + D == 0
    A =   (-(-e + 3*f - 3*g + h) + i*(-a + 3*b - 3*c + d)     )
    B = 3*(-( e - 2*f +   g    ) + i*( a - 2*b +   c    )     )
    C = 3*(-(-e +   f          ) + i*(-a +   b          )     )
    D =   (-( e                ) + i*( a                ) + j )

The near-vertical case, in terms of:  Ax^3 + Bx^2 + Cx + D == 0
    A =   ( (-a + 3*b - 3*c + d) - i*(-e + 3*f - 3*g + h)     )
    B = 3*( ( a - 2*b +   c    ) - i*( e - 2*f +   g    )     )
    C = 3*( (-a +   b          ) - i*(-e +   f          )     )
    D =   ( ( a                ) - i*( e                ) - j )

For horizontal lines:
(in) Resultant[
      a*(1 - t)^3 + 3*b*(1 - t)^2*t + 3*c*(1 - t)*t^2 + d*t^3 - j,
      e*(1 - t)^3 + 3*f*(1 - t)^2*t + 3*g*(1 - t)*t^2 + h*t^3 - y, y]
(out)  e     -   j     -
     3 e t   + 3 f t   +
     3 e t^2 - 6 f t^2 + 3 g t^2 -
       e t^3 + 3 f t^3 - 3 g t^3 + h t^3
 */

file struct LineCubicIntersections
{
    public enum PinTPoint
    {
        PointUninitialized,
        PointInitialized,
    }

    private readonly SkDCubic _cubic;
    private readonly SkDLine _line;
    private readonly SkIntersections _intersections;
    private bool _allowNear;

    public LineCubicIntersections(in SkDCubic c, in SkDLine l, SkIntersections i)
    {
        _cubic = c;
        _line = l;
        _intersections = i;
        _allowNear = true;
        i.SetMax(4);
    }

    public void AllowNear(bool allow)
    {
        _allowNear = allow;
    }

    public readonly void CheckCoincident()
    {
        int last = _intersections.Used() - 1;
        for (int index = 0; index < last;)
        {
            double cubicMidT = (_intersections[0][index] + _intersections[0][index + 1]) / 2;
            SkDPoint cubicMidPt = _cubic.PtAtT(cubicMidT);
            double t = _line.NearPoint(cubicMidPt);
            if (t < 0)
            {
                ++index;
                continue;
            }

            if (_intersections.IsCoincident(index))
            {
                _intersections.RemoveOne(index);
                --last;
            }
            else if (_intersections.IsCoincident(index + 1))
            {
                _intersections.RemoveOne(index + 1);
                --last;
            }
            else
            {
                _intersections.SetCoincident(index++);
            }

            _intersections.SetCoincident(index);
        }
    }

    // see parallel routine in line quadratic intersections
    public readonly int IntersectRay(Span<double> roots)
    {
        double adj = _line.Pts[1].X - _line.Pts[0].X;
        double opp = _line.Pts[1].Y - _line.Pts[0].Y;
        SkDCubic c = default; // Skia leaves c uninitialized; only its x values are read before they are set
        for (int n = 0; n < 4; ++n)
        {
            c[n].X = (_cubic.Pts[n].Y - _line.Pts[0].Y) * adj - (_cubic.Pts[n].X - _line.Pts[0].X) * opp;
        }

        SkDCubic.Coefficients(SkPathOps.Doubles(c.Pts), out double A, out double B, out double C, out double D);
        int count = SkDCubic.RootsValidT(A, B, C, D, roots);
        for (int index = 0; index < count; ++index)
        {
            SkDPoint calcPt = c.PtAtT(roots[index]);
            if (!SkPathOps.ApproximatelyZero(calcPt.X))
            {
                for (int n = 0; n < 4; ++n)
                {
                    c[n].Y = (_cubic.Pts[n].Y - _line.Pts[0].Y) * opp
                        + (_cubic.Pts[n].X - _line.Pts[0].X) * adj;
                }

                Span<double> extremeTs = stackalloc double[6];
                int extrema = SkDCubic.FindExtrema(SkPathOps.Doubles(c.Pts), extremeTs);
                count = c.SearchRoots(extremeTs, extrema, 0, SkDCubic.SearchAxis.XAxis, roots);
                break;
            }
        }

        return count;
    }

    public readonly int Intersect()
    {
        AddExactEndPoints();
        if (_allowNear)
        {
            AddNearEndPoints();
        }

        Span<double> rootVals = stackalloc double[3];
        int roots = IntersectRay(rootVals);
        for (int index = 0; index < roots; ++index)
        {
            double cubicT = rootVals[index];
            double lineT = FindLineT(cubicT);
            SkDPoint pt = default;
            if (PinTs(ref cubicT, ref lineT, ref pt, PinTPoint.PointUninitialized) && UniqueAnswer(cubicT, pt))
            {
                _intersections.Insert(cubicT, lineT, pt);
            }
        }

        CheckCoincident();
        return _intersections.Used();
    }

    public static int HorizontalIntersect(in SkDCubic c, double axisIntercept, Span<double> roots)
    {
        SkDCubic.Coefficients(
            SkPathOps.Doubles(c.Pts)[1..],
            out double A,
            out double B,
            out double C,
            out double D);
        D -= axisIntercept;
        int count = SkDCubic.RootsValidT(A, B, C, D, roots);
        for (int index = 0; index < count; ++index)
        {
            SkDPoint calcPt = c.PtAtT(roots[index]);
            if (!SkPathOps.ApproximatelyEqual(calcPt.Y, axisIntercept))
            {
                Span<double> extremeTs = stackalloc double[6];
                int extrema = SkDCubic.FindExtrema(SkPathOps.Doubles(c.Pts)[1..], extremeTs);
                count = c.SearchRoots(extremeTs, extrema, axisIntercept, SkDCubic.SearchAxis.YAxis, roots);
                break;
            }
        }

        return count;
    }

    public readonly int HorizontalIntersect(double axisIntercept, double left, double right, bool flipped)
    {
        AddExactHorizontalEndPoints(left, right, axisIntercept);
        if (_allowNear)
        {
            AddNearHorizontalEndPoints(left, right, axisIntercept);
        }

        Span<double> roots = stackalloc double[3];
        int count = HorizontalIntersect(_cubic, axisIntercept, roots);
        for (int index = 0; index < count; ++index)
        {
            double cubicT = roots[index];
            SkDPoint pt = new SkDPoint(_cubic.PtAtT(cubicT).X, axisIntercept);
            double lineT = (pt.X - left) / (right - left);
            if (PinTs(ref cubicT, ref lineT, ref pt, PinTPoint.PointInitialized) && UniqueAnswer(cubicT, pt))
            {
                _intersections.Insert(cubicT, lineT, pt);
            }
        }

        if (flipped)
        {
            _intersections.Flip();
        }

        CheckCoincident();
        return _intersections.Used();
    }

    public readonly bool UniqueAnswer(double cubicT, in SkDPoint pt)
    {
        for (int inner = 0; inner < _intersections.Used(); ++inner)
        {
            if (_intersections.Pt(inner) != pt)
            {
                continue;
            }

            double existingCubicT = _intersections[0][inner];
            if (cubicT == existingCubicT)
            {
                return false;
            }

            // check if midway on cubic is also same point. If so, discard this
            double cubicMidT = (existingCubicT + cubicT) / 2;
            SkDPoint cubicMidPt = _cubic.PtAtT(cubicMidT);
            if (cubicMidPt.ApproximatelyEqual(pt))
            {
                return false;
            }
        }

        return true;
    }

    public static int VerticalIntersect(in SkDCubic c, double axisIntercept, Span<double> roots)
    {
        SkDCubic.Coefficients(SkPathOps.Doubles(c.Pts), out double A, out double B, out double C, out double D);
        D -= axisIntercept;
        int count = SkDCubic.RootsValidT(A, B, C, D, roots);
        for (int index = 0; index < count; ++index)
        {
            SkDPoint calcPt = c.PtAtT(roots[index]);
            if (!SkPathOps.ApproximatelyEqual(calcPt.X, axisIntercept))
            {
                Span<double> extremeTs = stackalloc double[6];
                int extrema = SkDCubic.FindExtrema(SkPathOps.Doubles(c.Pts), extremeTs);
                count = c.SearchRoots(extremeTs, extrema, axisIntercept, SkDCubic.SearchAxis.XAxis, roots);
                break;
            }
        }

        return count;
    }

    public readonly int VerticalIntersect(double axisIntercept, double top, double bottom, bool flipped)
    {
        AddExactVerticalEndPoints(top, bottom, axisIntercept);
        if (_allowNear)
        {
            AddNearVerticalEndPoints(top, bottom, axisIntercept);
        }

        Span<double> roots = stackalloc double[3];
        int count = VerticalIntersect(_cubic, axisIntercept, roots);
        for (int index = 0; index < count; ++index)
        {
            double cubicT = roots[index];
            SkDPoint pt = new SkDPoint(axisIntercept, _cubic.PtAtT(cubicT).Y);
            double lineT = (pt.Y - top) / (bottom - top);
            if (PinTs(ref cubicT, ref lineT, ref pt, PinTPoint.PointInitialized) && UniqueAnswer(cubicT, pt))
            {
                _intersections.Insert(cubicT, lineT, pt);
            }
        }

        if (flipped)
        {
            _intersections.Flip();
        }

        CheckCoincident();
        return _intersections.Used();
    }

    private readonly void AddExactEndPoints()
    {
        for (int cIndex = 0; cIndex < 4; cIndex += 3)
        {
            double lineT = _line.ExactPoint(_cubic.Pts[cIndex]);
            if (lineT < 0)
            {
                continue;
            }

            double cubicT = cIndex >> 1;
            _intersections.Insert(cubicT, lineT, _cubic.Pts[cIndex]);
        }
    }

    /* Note that this does not look for endpoints of the line that are near the cubic.
       These points are found later when check ends looks for missing points */
    private readonly void AddNearEndPoints()
    {
        for (int cIndex = 0; cIndex < 4; cIndex += 3)
        {
            double cubicT = cIndex >> 1;
            if (_intersections.HasT(cubicT))
            {
                continue;
            }

            double lineT = _line.NearPoint(_cubic.Pts[cIndex]);
            if (lineT < 0)
            {
                continue;
            }

            _intersections.Insert(cubicT, lineT, _cubic.Pts[cIndex]);
        }

        AddLineNearEndPoints();
    }

    private readonly void AddLineNearEndPoints()
    {
        for (int lIndex = 0; lIndex < 2; ++lIndex)
        {
            double lineT = lIndex;
            if (_intersections.HasOppT(lineT))
            {
                continue;
            }

            SkDCurve curve = default;
            curve.Cubic = _cubic;
            double cubicT = curve.NearPoint(PathVerb.Cubic, _line.Pts[lIndex], _line.Pts[lIndex == 0 ? 1 : 0]);
            if (cubicT < 0)
            {
                continue;
            }

            _intersections.Insert(cubicT, lineT, _line.Pts[lIndex]);
        }
    }

    private readonly void AddExactHorizontalEndPoints(double left, double right, double y)
    {
        for (int cIndex = 0; cIndex < 4; cIndex += 3)
        {
            double lineT = SkDLine.ExactPointH(_cubic.Pts[cIndex], left, right, y);
            if (lineT < 0)
            {
                continue;
            }

            double cubicT = cIndex >> 1;
            _intersections.Insert(cubicT, lineT, _cubic.Pts[cIndex]);
        }
    }

    private readonly void AddNearHorizontalEndPoints(double left, double right, double y)
    {
        for (int cIndex = 0; cIndex < 4; cIndex += 3)
        {
            double cubicT = cIndex >> 1;
            if (_intersections.HasT(cubicT))
            {
                continue;
            }

            double lineT = SkDLine.NearPointH(_cubic.Pts[cIndex], left, right, y);
            if (lineT < 0)
            {
                continue;
            }

            _intersections.Insert(cubicT, lineT, _cubic.Pts[cIndex]);
        }

        AddLineNearEndPoints();
    }

    private readonly void AddExactVerticalEndPoints(double top, double bottom, double x)
    {
        for (int cIndex = 0; cIndex < 4; cIndex += 3)
        {
            double lineT = SkDLine.ExactPointV(_cubic.Pts[cIndex], top, bottom, x);
            if (lineT < 0)
            {
                continue;
            }

            double cubicT = cIndex >> 1;
            _intersections.Insert(cubicT, lineT, _cubic.Pts[cIndex]);
        }
    }

    private readonly void AddNearVerticalEndPoints(double top, double bottom, double x)
    {
        for (int cIndex = 0; cIndex < 4; cIndex += 3)
        {
            double cubicT = cIndex >> 1;
            if (_intersections.HasT(cubicT))
            {
                continue;
            }

            double lineT = SkDLine.NearPointV(_cubic.Pts[cIndex], top, bottom, x);
            if (lineT < 0)
            {
                continue;
            }

            _intersections.Insert(cubicT, lineT, _cubic.Pts[cIndex]);
        }

        AddLineNearEndPoints();
    }

    private readonly double FindLineT(double t)
    {
        SkDPoint xy = _cubic.PtAtT(t);
        double dx = _line.Pts[1].X - _line.Pts[0].X;
        double dy = _line.Pts[1].Y - _line.Pts[0].Y;
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            return (xy.X - _line.Pts[0].X) / dx;
        }

        return (xy.Y - _line.Pts[0].Y) / dy;
    }

    private readonly bool PinTs(ref double cubicT, ref double lineT, ref SkDPoint pt, PinTPoint ptSet)
    {
        if (!SkPathOps.ApproximatelyOneOrLess(lineT))
        {
            return false;
        }

        if (!SkPathOps.ApproximatelyZeroOrMore(lineT))
        {
            return false;
        }

        double cT = cubicT = SkPathOps.SkPinT(cubicT);
        double lT = lineT = SkPathOps.SkPinT(lineT);
        SkDPoint lPt = _line.PtAtT(lT);
        SkDPoint cPt = _cubic.PtAtT(cT);
        if (!lPt.RoughlyEqual(cPt))
        {
            return false;
        }

        // FIXME: if points are roughly equal but not approximately equal, need to do
        // a binary search like quad/quad intersection to find more precise t values
        if (lT == 0 || lT == 1 || (ptSet == PinTPoint.PointUninitialized && cT != 0 && cT != 1))
        {
            pt = lPt;
        }
        else if (ptSet == PinTPoint.PointUninitialized)
        {
            pt = cPt;
        }

        PathPoint gridPt = pt.AsSkPoint();
        if (SkPathOps.SkPointEquals(gridPt, _line.Pts[0].AsSkPoint()))
        {
            lineT = 0;
        }
        else if (SkPathOps.SkPointEquals(gridPt, _line.Pts[1].AsSkPoint()))
        {
            lineT = 1;
        }

        if (SkPathOps.SkPointEquals(gridPt, _cubic.Pts[0].AsSkPoint()) && SkPathOps.ApproximatelyEqual(cubicT, 0))
        {
            cubicT = 0;
        }
        else if (SkPathOps.SkPointEquals(gridPt, _cubic.Pts[3].AsSkPoint())
            && SkPathOps.ApproximatelyEqual(cubicT, 1))
        {
            cubicT = 1;
        }

        return true;
    }
}

internal sealed partial class SkIntersections
{
    public int Horizontal(in SkDCubic cubic, double left, double right, double y, bool flipped)
    {
        SkDLine line = new SkDLine(new SkDPoint(left, y), new SkDPoint(right, y));
        LineCubicIntersections c = new LineCubicIntersections(cubic, line, this);
        return c.HorizontalIntersect(y, left, right, flipped);
    }

    public int Vertical(in SkDCubic cubic, double top, double bottom, double x, bool flipped)
    {
        SkDLine line = new SkDLine(new SkDPoint(x, top), new SkDPoint(x, bottom));
        LineCubicIntersections c = new LineCubicIntersections(cubic, line, this);
        return c.VerticalIntersect(x, top, bottom, flipped);
    }

    public int Intersect(in SkDCubic cubic, in SkDLine line)
    {
        LineCubicIntersections c = new LineCubicIntersections(cubic, line, this);
        c.AllowNear(_allowNear);
        return c.Intersect();
    }

    public int IntersectRay(in SkDCubic cubic, in SkDLine line)
    {
        LineCubicIntersections c = new LineCubicIntersections(cubic, line, this);
        _used = (byte)c.IntersectRay(_t[0]);
        for (int index = 0; index < _used; ++index)
        {
            _pt[index] = cubic.PtAtT(_t[0][index]);
        }

        return _used;
    }
}

// SkDCubic accessors to Intersection utilities
internal partial struct SkDCubic
{
    public readonly int HorizontalIntersect(double yIntercept, Span<double> roots)
    {
        return LineCubicIntersections.HorizontalIntersect(this, yIntercept, roots);
    }

    public readonly int VerticalIntersect(double xIntercept, Span<double> roots)
    {
        return LineCubicIntersections.VerticalIntersect(this, xIntercept, roots);
    }
}
