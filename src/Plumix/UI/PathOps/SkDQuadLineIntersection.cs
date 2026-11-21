namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkDQuadLineIntersection.cpp at chrome/m119 (the Skia inside
// SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types are
// kept. LineQuadraticIntersections holds copies of the quad and line Skia references (neither changes while it
// lives). The SkDCurve cast in addLineNearEndPoints copies the quad into an SkDCurve.

/*
Find the intersection of a line and quadratic by solving for valid t values.

From http://stackoverflow.com/questions/1853637/how-to-find-the-mathematical-function-defining-a-bezier-curve

"A Bezier curve is a parametric function. A quadratic Bezier curve (i.e. three
control points) can be expressed as: F(t) = A(1 - t)^2 + B(1 - t)t + Ct^2 where
A, B and C are points and t goes from zero to one.

This will give you two equations:

  x = a(1 - t)^2 + b(1 - t)t + ct^2
  y = d(1 - t)^2 + e(1 - t)t + ft^2

If you add for instance the line equation (y = kx + m) to that, you'll end up
with three equations and three unknowns (x, y and t)."

Similar to above, the quadratic is represented as
  x = a(1-t)^2 + 2b(1-t)t + ct^2
  y = d(1-t)^2 + 2e(1-t)t + ft^2
and the line as
  y = g*x + h

Using Mathematica, solve for the values of t where the quadratic intersects the
line:

  (in)  t1 = Resultant[a*(1 - t)^2 + 2*b*(1 - t)*t + c*t^2 - x,
                       d*(1 - t)^2 + 2*e*(1 - t)*t  + f*t^2 - g*x - h, x]
  (out) -d + h + 2 d t - 2 e t - d t^2 + 2 e t^2 - f t^2 +
         g  (a - 2 a t + 2 b t + a t^2 - 2 b t^2 + c t^2)
  (in)  Solve[t1 == 0, t]
  (out) {
    {t -> (-2 d + 2 e +   2 a g - 2 b g    -
      Sqrt[(2 d - 2 e -   2 a g + 2 b g)^2 -
          4 (-d + 2 e - f + a g - 2 b g    + c g) (-d + a g + h)]) /
         (2 (-d + 2 e - f + a g - 2 b g    + c g))
         },
    {t -> (-2 d + 2 e +   2 a g - 2 b g    +
      Sqrt[(2 d - 2 e -   2 a g + 2 b g)^2 -
          4 (-d + 2 e - f + a g - 2 b g    + c g) (-d + a g + h)]) /
         (2 (-d + 2 e - f + a g - 2 b g    + c g))
         }
        }

Using the results above (when the line tends towards horizontal)
       A =   (-(d - 2*e + f) + g*(a - 2*b + c)     )
       B = 2*( (d -   e    ) - g*(a -   b    )     )
       C =   (-(d          ) + g*(a          ) + h )

If g goes to infinity, we can rewrite the line in terms of x.
  x = g'*y + h'

And solve accordingly in Mathematica:

  (in)  t2 = Resultant[a*(1 - t)^2 + 2*b*(1 - t)*t + c*t^2 - g'*y - h',
                       d*(1 - t)^2 + 2*e*(1 - t)*t  + f*t^2 - y, y]
  (out)  a - h' - 2 a t + 2 b t + a t^2 - 2 b t^2 + c t^2 -
         g'  (d - 2 d t + 2 e t + d t^2 - 2 e t^2 + f t^2)
  (in)  Solve[t2 == 0, t]
  (out) {
    {t -> (2 a - 2 b -   2 d g' + 2 e g'    -
    Sqrt[(-2 a + 2 b +   2 d g' - 2 e g')^2 -
          4 (a - 2 b + c - d g' + 2 e g' - f g') (a - d g' - h')]) /
         (2 (a - 2 b + c - d g' + 2 e g' - f g'))
         },
    {t -> (2 a - 2 b -   2 d g' + 2 e g'    +
    Sqrt[(-2 a + 2 b +   2 d g' - 2 e g')^2 -
          4 (a - 2 b + c - d g' + 2 e g' - f g') (a - d g' - h')])/
         (2 (a - 2 b + c - d g' + 2 e g' - f g'))
         }
        }

Thus, if the slope of the line tends towards vertical, we use:
       A =   ( (a - 2*b + c) - g'*(d  - 2*e + f)      )
       B = 2*(-(a -   b    ) + g'*(d  -   e    )      )
       C =   ( (a          ) - g'*(d           ) - h' )
 */

file struct LineQuadraticIntersections
{
    public enum PinTPoint
    {
        PointUninitialized,
        PointInitialized,
    }

    private readonly SkDQuad _quad;
    private readonly SkDLine _line;
    private readonly SkIntersections _intersections;
    private bool _allowNear;

    public LineQuadraticIntersections(in SkDQuad q, in SkDLine l, SkIntersections i)
    {
        _quad = q;
        _line = l;
        _intersections = i;
        _allowNear = true;
        i.SetMax(5); // allow short partial coincidence plus discrete intersections
    }

    // Skia leaves fLine and fIntersections unset here (nullptr in debug builds); only the intercepts use this.
    public LineQuadraticIntersections(in SkDQuad q)
    {
        _quad = q;
        _line = default;
        _intersections = null!;
        _allowNear = false;
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
            double quadMidT = (_intersections[0][index] + _intersections[0][index + 1]) / 2;
            SkDPoint quadMidPt = _quad.PtAtT(quadMidT);
            double t = _line.NearPoint(quadMidPt);
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

    public readonly int IntersectRay(Span<double> roots)
    {
        /*
            solve by rotating line+quad so line is horizontal, then finding the roots
            set up matrix to rotate quad to x-axis
            |cos(a) -sin(a)|
            |sin(a)  cos(a)|
            note that cos(a) = A(djacent) / Hypoteneuse
                      sin(a) = O(pposite) / Hypoteneuse
            since we are computing Ts, we can ignore hypoteneuse, the scale factor:
            |  A     -O    |
            |  O      A    |
            A = line[1].fX - line[0].fX (adjacent side of the right triangle)
            O = line[1].fY - line[0].fY (opposite side of the right triangle)
            for each of the three points (e.g. n = 0 to 2)
            quad[n].fY' = (quad[n].fY - line[0].fY) * A - (quad[n].fX - line[0].fX) * O
        */
        double adj = _line.Pts[1].X - _line.Pts[0].X;
        double opp = _line.Pts[1].Y - _line.Pts[0].Y;
        Span<double> r = stackalloc double[3];
        for (int n = 0; n < 3; ++n)
        {
            r[n] = (_quad.Pts[n].Y - _line.Pts[0].Y) * adj - (_quad.Pts[n].X - _line.Pts[0].X) * opp;
        }

        double A = r[2];
        double B = r[1];
        double C = r[0];
        A += C - 2 * B; // A = a - 2*b + c
        B -= C; // B = -(b - c)
        return SkDQuad.RootsValidT(A, 2 * B, C, roots);
    }

    public readonly int Intersect()
    {
        AddExactEndPoints();
        if (_allowNear)
        {
            AddNearEndPoints();
        }

        Span<double> rootVals = stackalloc double[2];
        int roots = IntersectRay(rootVals);
        for (int index = 0; index < roots; ++index)
        {
            double quadT = rootVals[index];
            double lineT = FindLineT(quadT);
            SkDPoint pt = default;
            if (PinTs(ref quadT, ref lineT, ref pt, PinTPoint.PointUninitialized) && UniqueAnswer(quadT, pt))
            {
                _intersections.Insert(quadT, lineT, pt);
            }
        }

        CheckCoincident();
        return _intersections.Used();
    }

    public readonly int HorizontalIntersect(double axisIntercept, Span<double> roots)
    {
        double D = _quad.Pts[2].Y; // f
        double E = _quad.Pts[1].Y; // e
        double F = _quad.Pts[0].Y; // d
        D += F - 2 * E; // D = d - 2*e + f
        E -= F; // E = -(d - e)
        F -= axisIntercept;
        return SkDQuad.RootsValidT(D, 2 * E, F, roots);
    }

    public readonly int HorizontalIntersect(double axisIntercept, double left, double right, bool flipped)
    {
        AddExactHorizontalEndPoints(left, right, axisIntercept);
        if (_allowNear)
        {
            AddNearHorizontalEndPoints(left, right, axisIntercept);
        }

        Span<double> rootVals = stackalloc double[2];
        int roots = HorizontalIntersect(axisIntercept, rootVals);
        for (int index = 0; index < roots; ++index)
        {
            double quadT = rootVals[index];
            SkDPoint pt = _quad.PtAtT(quadT);
            double lineT = (pt.X - left) / (right - left);
            if (PinTs(ref quadT, ref lineT, ref pt, PinTPoint.PointInitialized) && UniqueAnswer(quadT, pt))
            {
                _intersections.Insert(quadT, lineT, pt);
            }
        }

        if (flipped)
        {
            _intersections.Flip();
        }

        CheckCoincident();
        return _intersections.Used();
    }

    public readonly bool UniqueAnswer(double quadT, in SkDPoint pt)
    {
        for (int inner = 0; inner < _intersections.Used(); ++inner)
        {
            if (_intersections.Pt(inner) != pt)
            {
                continue;
            }

            double existingQuadT = _intersections[0][inner];
            if (quadT == existingQuadT)
            {
                return false;
            }

            // check if midway on quad is also same point. If so, discard this
            double quadMidT = (existingQuadT + quadT) / 2;
            SkDPoint quadMidPt = _quad.PtAtT(quadMidT);
            if (quadMidPt.ApproximatelyEqual(pt))
            {
                return false;
            }
        }

        return true;
    }

    public readonly int VerticalIntersect(double axisIntercept, Span<double> roots)
    {
        double D = _quad.Pts[2].X; // f
        double E = _quad.Pts[1].X; // e
        double F = _quad.Pts[0].X; // d
        D += F - 2 * E; // D = d - 2*e + f
        E -= F; // E = -(d - e)
        F -= axisIntercept;
        return SkDQuad.RootsValidT(D, 2 * E, F, roots);
    }

    public readonly int VerticalIntersect(double axisIntercept, double top, double bottom, bool flipped)
    {
        AddExactVerticalEndPoints(top, bottom, axisIntercept);
        if (_allowNear)
        {
            AddNearVerticalEndPoints(top, bottom, axisIntercept);
        }

        Span<double> rootVals = stackalloc double[2];
        int roots = VerticalIntersect(axisIntercept, rootVals);
        for (int index = 0; index < roots; ++index)
        {
            double quadT = rootVals[index];
            SkDPoint pt = _quad.PtAtT(quadT);
            double lineT = (pt.Y - top) / (bottom - top);
            if (PinTs(ref quadT, ref lineT, ref pt, PinTPoint.PointInitialized) && UniqueAnswer(quadT, pt))
            {
                _intersections.Insert(quadT, lineT, pt);
            }
        }

        if (flipped)
        {
            _intersections.Flip();
        }

        CheckCoincident();
        return _intersections.Used();
    }

    // add endpoints first to get zero and one t values exactly
    private readonly void AddExactEndPoints()
    {
        for (int qIndex = 0; qIndex < 3; qIndex += 2)
        {
            double lineT = _line.ExactPoint(_quad.Pts[qIndex]);
            if (lineT < 0)
            {
                continue;
            }

            double quadT = qIndex >> 1;
            _intersections.Insert(quadT, lineT, _quad.Pts[qIndex]);
        }
    }

    private readonly void AddNearEndPoints()
    {
        for (int qIndex = 0; qIndex < 3; qIndex += 2)
        {
            double quadT = qIndex >> 1;
            if (_intersections.HasT(quadT))
            {
                continue;
            }

            double lineT = _line.NearPoint(_quad.Pts[qIndex]);
            if (lineT < 0)
            {
                continue;
            }

            _intersections.Insert(quadT, lineT, _quad.Pts[qIndex]);
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
            curve.Quad = _quad;
            double quadT = curve.NearPoint(PathVerb.Quad, _line.Pts[lIndex], _line.Pts[lIndex == 0 ? 1 : 0]);
            if (quadT < 0)
            {
                continue;
            }

            _intersections.Insert(quadT, lineT, _line.Pts[lIndex]);
        }
    }

    private readonly void AddExactHorizontalEndPoints(double left, double right, double y)
    {
        for (int qIndex = 0; qIndex < 3; qIndex += 2)
        {
            double lineT = SkDLine.ExactPointH(_quad.Pts[qIndex], left, right, y);
            if (lineT < 0)
            {
                continue;
            }

            double quadT = qIndex >> 1;
            _intersections.Insert(quadT, lineT, _quad.Pts[qIndex]);
        }
    }

    private readonly void AddNearHorizontalEndPoints(double left, double right, double y)
    {
        for (int qIndex = 0; qIndex < 3; qIndex += 2)
        {
            double quadT = qIndex >> 1;
            if (_intersections.HasT(quadT))
            {
                continue;
            }

            double lineT = SkDLine.NearPointH(_quad.Pts[qIndex], left, right, y);
            if (lineT < 0)
            {
                continue;
            }

            _intersections.Insert(quadT, lineT, _quad.Pts[qIndex]);
        }

        AddLineNearEndPoints();
    }

    private readonly void AddExactVerticalEndPoints(double top, double bottom, double x)
    {
        for (int qIndex = 0; qIndex < 3; qIndex += 2)
        {
            double lineT = SkDLine.ExactPointV(_quad.Pts[qIndex], top, bottom, x);
            if (lineT < 0)
            {
                continue;
            }

            double quadT = qIndex >> 1;
            _intersections.Insert(quadT, lineT, _quad.Pts[qIndex]);
        }
    }

    private readonly void AddNearVerticalEndPoints(double top, double bottom, double x)
    {
        for (int qIndex = 0; qIndex < 3; qIndex += 2)
        {
            double quadT = qIndex >> 1;
            if (_intersections.HasT(quadT))
            {
                continue;
            }

            double lineT = SkDLine.NearPointV(_quad.Pts[qIndex], top, bottom, x);
            if (lineT < 0)
            {
                continue;
            }

            _intersections.Insert(quadT, lineT, _quad.Pts[qIndex]);
        }

        AddLineNearEndPoints();
    }

    private readonly double FindLineT(double t)
    {
        SkDPoint xy = _quad.PtAtT(t);
        double dx = _line.Pts[1].X - _line.Pts[0].X;
        double dy = _line.Pts[1].Y - _line.Pts[0].Y;
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            return (xy.X - _line.Pts[0].X) / dx;
        }

        return (xy.Y - _line.Pts[0].Y) / dy;
    }

    private readonly bool PinTs(ref double quadT, ref double lineT, ref SkDPoint pt, PinTPoint ptSet)
    {
        if (!SkPathOps.ApproximatelyOneOrLessDouble(lineT))
        {
            return false;
        }

        if (!SkPathOps.ApproximatelyZeroOrMoreDouble(lineT))
        {
            return false;
        }

        double qT = quadT = SkPathOps.SkPinT(quadT);
        double lT = lineT = SkPathOps.SkPinT(lineT);
        if (lT == 0 || lT == 1 || (ptSet == PinTPoint.PointUninitialized && qT != 0 && qT != 1))
        {
            pt = _line.PtAtT(lT);
        }
        else if (ptSet == PinTPoint.PointUninitialized)
        {
            pt = _quad.PtAtT(qT);
        }

        PathPoint gridPt = pt.AsSkPoint();
        if (SkDPoint.ApproximatelyEqual(gridPt, _line.Pts[0].AsSkPoint()))
        {
            pt = _line.Pts[0];
            lineT = 0;
        }
        else if (SkDPoint.ApproximatelyEqual(gridPt, _line.Pts[1].AsSkPoint()))
        {
            pt = _line.Pts[1];
            lineT = 1;
        }

        if (_intersections.Used() > 0 && SkPathOps.ApproximatelyEqual(_intersections[1][0], lineT))
        {
            return false;
        }

        if (SkPathOps.SkPointEquals(gridPt, _quad.Pts[0].AsSkPoint()))
        {
            pt = _quad.Pts[0];
            quadT = 0;
        }
        else if (SkPathOps.SkPointEquals(gridPt, _quad.Pts[2].AsSkPoint()))
        {
            pt = _quad.Pts[2];
            quadT = 1;
        }

        return true;
    }
}

internal sealed partial class SkIntersections
{
    public int Horizontal(in SkDQuad quad, double left, double right, double y, bool flipped)
    {
        SkDLine line = new SkDLine(new SkDPoint(left, y), new SkDPoint(right, y));
        LineQuadraticIntersections q = new LineQuadraticIntersections(quad, line, this);
        return q.HorizontalIntersect(y, left, right, flipped);
    }

    public int Vertical(in SkDQuad quad, double top, double bottom, double x, bool flipped)
    {
        SkDLine line = new SkDLine(new SkDPoint(x, top), new SkDPoint(x, bottom));
        LineQuadraticIntersections q = new LineQuadraticIntersections(quad, line, this);
        return q.VerticalIntersect(x, top, bottom, flipped);
    }

    public int Intersect(in SkDQuad quad, in SkDLine line)
    {
        LineQuadraticIntersections q = new LineQuadraticIntersections(quad, line, this);
        q.AllowNear(_allowNear);
        return q.Intersect();
    }

    public int IntersectRay(in SkDQuad quad, in SkDLine line)
    {
        LineQuadraticIntersections q = new LineQuadraticIntersections(quad, line, this);
        _used = (byte)q.IntersectRay(_t[0]);
        for (int index = 0; index < _used; ++index)
        {
            _pt[index] = quad.PtAtT(_t[0][index]);
        }

        return _used;
    }

    public static int HorizontalIntercept(in SkDQuad quad, float y, Span<double> roots)
    {
        LineQuadraticIntersections q = new LineQuadraticIntersections(quad);
        return q.HorizontalIntersect(y, roots);
    }

    public static int VerticalIntercept(in SkDQuad quad, float x, Span<double> roots)
    {
        LineQuadraticIntersections q = new LineQuadraticIntersections(quad);
        return q.VerticalIntersect(x, roots);
    }
}

// SkDQuad accessors to Intersection utilities
internal partial struct SkDQuad
{
    // Skia passes the double intercept to SkIntersections' SkScalar parameter, so it is narrowed to float.
    public readonly int HorizontalIntersect(double yIntercept, Span<double> roots)
    {
        return SkIntersections.HorizontalIntercept(this, (float)yIntercept, roots);
    }

    public readonly int VerticalIntersect(double xIntercept, Span<double> roots)
    {
        return SkIntersections.VerticalIntercept(this, (float)xIntercept, roots);
    }
}
