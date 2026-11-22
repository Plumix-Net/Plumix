namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkDConicLineIntersection.cpp at chrome/m119 (the Skia inside
// SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types are
// kept. LineConicIntersections holds copies of the conic and line Skia references (neither changes while it
// lives). The SkDCurve cast in addLineNearEndPoints copies the conic into an SkDCurve.

file struct LineConicIntersections
{
    public enum PinTPoint
    {
        PointUninitialized,
        PointInitialized,
    }

    private readonly SkDConic _conic;
    private readonly SkDLine _line;
    private readonly SkIntersections _intersections;
    private bool _allowNear;

    public LineConicIntersections(in SkDConic c, in SkDLine l, SkIntersections i)
    {
        _conic = c;
        _line = l;
        _intersections = i;
        _allowNear = true;
        i.SetMax(4); // allow short partial coincidence plus discrete intersection
    }

    // Skia leaves fLine and fIntersections unset here (nullptr in debug builds); only the intercepts use this.
    public LineConicIntersections(in SkDConic c)
    {
        _conic = c;
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
            double conicMidT = (_intersections[0][index] + _intersections[0][index + 1]) / 2;
            SkDPoint conicMidPt = _conic.PtAtT(conicMidT);
            double t = _line.NearPoint(conicMidPt);
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

    public readonly int HorizontalIntersect(double axisIntercept, Span<double> roots)
    {
        Span<double> conicVals = stackalloc double[3]
        {
            _conic.Pts.Pts[0].Y, _conic.Pts.Pts[1].Y, _conic.Pts.Pts[2].Y,
        };
        return ValidT(conicVals, axisIntercept, roots);
    }

    public readonly int HorizontalIntersect(double axisIntercept, double left, double right, bool flipped)
    {
        AddExactHorizontalEndPoints(left, right, axisIntercept);
        if (_allowNear)
        {
            AddNearHorizontalEndPoints(left, right, axisIntercept);
        }

        Span<double> roots = stackalloc double[2];
        int count = HorizontalIntersect(axisIntercept, roots);
        for (int index = 0; index < count; ++index)
        {
            double conicT = roots[index];
            SkDPoint pt = _conic.PtAtT(conicT);
            double lineT = (pt.X - left) / (right - left);
            if (PinTs(ref conicT, ref lineT, ref pt, PinTPoint.PointInitialized) && UniqueAnswer(conicT, pt))
            {
                _intersections.Insert(conicT, lineT, pt);
            }
        }

        if (flipped)
        {
            _intersections.Flip();
        }

        CheckCoincident();
        return _intersections.Used();
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
            double conicT = rootVals[index];
            double lineT = FindLineT(conicT);
            SkDPoint pt = default;
            if (PinTs(ref conicT, ref lineT, ref pt, PinTPoint.PointUninitialized) && UniqueAnswer(conicT, pt))
            {
                _intersections.Insert(conicT, lineT, pt);
            }
        }

        CheckCoincident();
        return _intersections.Used();
    }

    public readonly int IntersectRay(Span<double> roots)
    {
        double adj = _line.Pts[1].X - _line.Pts[0].X;
        double opp = _line.Pts[1].Y - _line.Pts[0].Y;
        Span<double> r = stackalloc double[3];
        for (int n = 0; n < 3; ++n)
        {
            r[n] = (_conic.Pts.Pts[n].Y - _line.Pts[0].Y) * adj - (_conic.Pts.Pts[n].X - _line.Pts[0].X) * opp;
        }

        return ValidT(r, 0, roots);
    }

    public readonly int ValidT(ReadOnlySpan<double> r, double axisIntercept, Span<double> roots)
    {
        double A = r[2];
        double B = r[1] * _conic.Weight - axisIntercept * _conic.Weight + axisIntercept;
        double C = r[0];
        A += C - 2 * B; // A = a + c - 2*(b*w - xCept*w + xCept)
        B -= C; // B = b*w - w * xCept + xCept - a
        C -= axisIntercept;
        return SkDQuad.RootsValidT(A, 2 * B, C, roots);
    }

    public readonly int VerticalIntersect(double axisIntercept, Span<double> roots)
    {
        Span<double> conicVals = stackalloc double[3]
        {
            _conic.Pts.Pts[0].X, _conic.Pts.Pts[1].X, _conic.Pts.Pts[2].X,
        };
        return ValidT(conicVals, axisIntercept, roots);
    }

    public readonly int VerticalIntersect(double axisIntercept, double top, double bottom, bool flipped)
    {
        AddExactVerticalEndPoints(top, bottom, axisIntercept);
        if (_allowNear)
        {
            AddNearVerticalEndPoints(top, bottom, axisIntercept);
        }

        Span<double> roots = stackalloc double[2];
        int count = VerticalIntersect(axisIntercept, roots);
        for (int index = 0; index < count; ++index)
        {
            double conicT = roots[index];
            SkDPoint pt = _conic.PtAtT(conicT);
            double lineT = (pt.Y - top) / (bottom - top);
            if (PinTs(ref conicT, ref lineT, ref pt, PinTPoint.PointInitialized) && UniqueAnswer(conicT, pt))
            {
                _intersections.Insert(conicT, lineT, pt);
            }
        }

        if (flipped)
        {
            _intersections.Flip();
        }

        CheckCoincident();
        return _intersections.Used();
    }

    // OPTIMIZE: Functions of the form add .. points are indentical to the conic routines.
    // add endpoints first to get zero and one t values exactly
    private readonly void AddExactEndPoints()
    {
        for (int cIndex = 0; cIndex < SkDConic.KPointCount; cIndex += SkDConic.KPointLast)
        {
            double lineT = _line.ExactPoint(_conic.Pts.Pts[cIndex]);
            if (lineT < 0)
            {
                continue;
            }

            double conicT = cIndex >> 1;
            _intersections.Insert(conicT, lineT, _conic.Pts.Pts[cIndex]);
        }
    }

    private readonly void AddNearEndPoints()
    {
        for (int cIndex = 0; cIndex < SkDConic.KPointCount; cIndex += SkDConic.KPointLast)
        {
            double conicT = cIndex >> 1;
            if (_intersections.HasT(conicT))
            {
                continue;
            }

            double lineT = _line.NearPoint(_conic.Pts.Pts[cIndex]);
            if (lineT < 0)
            {
                continue;
            }

            _intersections.Insert(conicT, lineT, _conic.Pts.Pts[cIndex]);
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
            curve.Conic = _conic;
            double conicT = curve.NearPoint(PathVerb.Conic, _line.Pts[lIndex], _line.Pts[lIndex == 0 ? 1 : 0]);
            if (conicT < 0)
            {
                continue;
            }

            _intersections.Insert(conicT, lineT, _line.Pts[lIndex]);
        }
    }

    private readonly void AddExactHorizontalEndPoints(double left, double right, double y)
    {
        for (int cIndex = 0; cIndex < SkDConic.KPointCount; cIndex += SkDConic.KPointLast)
        {
            double lineT = SkDLine.ExactPointH(_conic.Pts.Pts[cIndex], left, right, y);
            if (lineT < 0)
            {
                continue;
            }

            double conicT = cIndex >> 1;
            _intersections.Insert(conicT, lineT, _conic.Pts.Pts[cIndex]);
        }
    }

    private readonly void AddNearHorizontalEndPoints(double left, double right, double y)
    {
        for (int cIndex = 0; cIndex < SkDConic.KPointCount; cIndex += SkDConic.KPointLast)
        {
            double conicT = cIndex >> 1;
            if (_intersections.HasT(conicT))
            {
                continue;
            }

            double lineT = SkDLine.NearPointH(_conic.Pts.Pts[cIndex], left, right, y);
            if (lineT < 0)
            {
                continue;
            }

            _intersections.Insert(conicT, lineT, _conic.Pts.Pts[cIndex]);
        }

        AddLineNearEndPoints();
    }

    private readonly void AddExactVerticalEndPoints(double top, double bottom, double x)
    {
        for (int cIndex = 0; cIndex < SkDConic.KPointCount; cIndex += SkDConic.KPointLast)
        {
            double lineT = SkDLine.ExactPointV(_conic.Pts.Pts[cIndex], top, bottom, x);
            if (lineT < 0)
            {
                continue;
            }

            double conicT = cIndex >> 1;
            _intersections.Insert(conicT, lineT, _conic.Pts.Pts[cIndex]);
        }
    }

    private readonly void AddNearVerticalEndPoints(double top, double bottom, double x)
    {
        for (int cIndex = 0; cIndex < SkDConic.KPointCount; cIndex += SkDConic.KPointLast)
        {
            double conicT = cIndex >> 1;
            if (_intersections.HasT(conicT))
            {
                continue;
            }

            double lineT = SkDLine.NearPointV(_conic.Pts.Pts[cIndex], top, bottom, x);
            if (lineT < 0)
            {
                continue;
            }

            _intersections.Insert(conicT, lineT, _conic.Pts.Pts[cIndex]);
        }

        AddLineNearEndPoints();
    }

    private readonly double FindLineT(double t)
    {
        SkDPoint xy = _conic.PtAtT(t);
        double dx = _line.Pts[1].X - _line.Pts[0].X;
        double dy = _line.Pts[1].Y - _line.Pts[0].Y;
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            return (xy.X - _line.Pts[0].X) / dx;
        }

        return (xy.Y - _line.Pts[0].Y) / dy;
    }

    private readonly bool PinTs(ref double conicT, ref double lineT, ref SkDPoint pt, PinTPoint ptSet)
    {
        if (!SkPathOps.ApproximatelyOneOrLessDouble(lineT))
        {
            return false;
        }

        if (!SkPathOps.ApproximatelyZeroOrMoreDouble(lineT))
        {
            return false;
        }

        double qT = conicT = SkPathOps.SkPinT(conicT);
        double lT = lineT = SkPathOps.SkPinT(lineT);
        if (lT == 0 || lT == 1 || (ptSet == PinTPoint.PointUninitialized && qT != 0 && qT != 1))
        {
            pt = _line.PtAtT(lT);
        }
        else if (ptSet == PinTPoint.PointUninitialized)
        {
            pt = _conic.PtAtT(qT);
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

        if (SkPathOps.SkPointEquals(gridPt, _conic.Pts.Pts[0].AsSkPoint()))
        {
            pt = _conic.Pts.Pts[0];
            conicT = 0;
        }
        else if (SkPathOps.SkPointEquals(gridPt, _conic.Pts.Pts[2].AsSkPoint()))
        {
            pt = _conic.Pts.Pts[2];
            conicT = 1;
        }

        return true;
    }

    private readonly bool UniqueAnswer(double conicT, in SkDPoint pt)
    {
        for (int inner = 0; inner < _intersections.Used(); ++inner)
        {
            if (_intersections.Pt(inner) != pt)
            {
                continue;
            }

            double existingConicT = _intersections[0][inner];
            if (conicT == existingConicT)
            {
                return false;
            }

            // check if midway on conic is also same point. If so, discard this
            double conicMidT = (existingConicT + conicT) / 2;
            SkDPoint conicMidPt = _conic.PtAtT(conicMidT);
            if (conicMidPt.ApproximatelyEqual(pt))
            {
                return false;
            }
        }

        return true;
    }
}

internal sealed partial class SkIntersections
{
    public int Horizontal(in SkDConic conic, double left, double right, double y, bool flipped)
    {
        SkDLine line = new SkDLine(new SkDPoint(left, y), new SkDPoint(right, y));
        LineConicIntersections c = new LineConicIntersections(conic, line, this);
        return c.HorizontalIntersect(y, left, right, flipped);
    }

    public int Vertical(in SkDConic conic, double top, double bottom, double x, bool flipped)
    {
        SkDLine line = new SkDLine(new SkDPoint(x, top), new SkDPoint(x, bottom));
        LineConicIntersections c = new LineConicIntersections(conic, line, this);
        return c.VerticalIntersect(x, top, bottom, flipped);
    }

    public int Intersect(in SkDConic conic, in SkDLine line)
    {
        LineConicIntersections c = new LineConicIntersections(conic, line, this);
        c.AllowNear(_allowNear);
        return c.Intersect();
    }

    public int IntersectRay(in SkDConic conic, in SkDLine line)
    {
        LineConicIntersections c = new LineConicIntersections(conic, line, this);
        _used = (byte)c.IntersectRay(_t[0]);
        for (int index = 0; index < _used; ++index)
        {
            _pt[index] = conic.PtAtT(_t[0][index]);
        }

        return _used;
    }

    public static int HorizontalIntercept(in SkDConic conic, float y, Span<double> roots)
    {
        LineConicIntersections c = new LineConicIntersections(conic);
        return c.HorizontalIntersect(y, roots);
    }

    public static int VerticalIntercept(in SkDConic conic, float x, Span<double> roots)
    {
        LineConicIntersections c = new LineConicIntersections(conic);
        return c.VerticalIntersect(x, roots);
    }
}
