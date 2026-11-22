namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkPathOpsTSect.h and SkPathOpsTSect.cpp at chrome/m119 (the
// Skia inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double
// types are kept. SkTSect/SkTSpan work through SkTCurve (convention 9). SkTSpan, SkTSpanBounded, SkClosestRecord and
// SkClosestSect are classes (arena-allocated or used through pointers); SkTCoincident is a struct, since Skia embeds
// it by value and copies it (fCoinStart = prior->fCoinEnd). SkTSect is a friend of SkTSpan, so the SkTSpan fields
// SkTSect touches are internal but keep the private _name. The SkArenaAlloc parameters are dropped; spans SkTSect
// deletes still go on its fDeleted list and are reused by addOne, as in Skia. SkOpDebugBool is bool (release).
// Debug-only code (validate*, debug*, dump*, SkOPASSERT, fID, fDebug*) is omitted. extractCoincident keeps the
// release behaviour where oppEndT is only set from SK_INIT_TO_AVOID_WARNING (0) before it is recomputed.

internal static partial class SkPathOps
{
    // Skia's x != x.
    public static bool SkDoubleIsNaN(double x)
    {
        return double.IsNaN(x);
    }
}

internal struct SkTCoincident
{
    private SkDPoint _perpPt;
    private double _perpT; // perpendicular intersection on opposite curve
    private bool _match;

    public SkTCoincident()
    {
        Init();
    }

    public readonly bool IsMatch()
    {
        return _match;
    }

    public void Init()
    {
        _perpT = -1;
        _match = false;
        _perpPt.X = _perpPt.Y = double.NaN; // SK_ScalarNaN
    }

    public void MarkCoincident()
    {
        if (!_match)
        {
            _perpT = -1;
        }

        _match = true;
    }

    public readonly SkDPoint PerpPt()
    {
        return _perpPt;
    }

    public readonly double PerpT()
    {
        return _perpT;
    }

    // SkPathOpsTSect.cpp

    public void SetPerp(SkTCurve c1, double t, in SkDPoint cPt, SkTCurve c2)
    {
        SkDVector dxdy = c1.DxdyAtT(t);
        SkDLine perp = new SkDLine(cPt, new SkDPoint(cPt.X + dxdy.Y, cPt.Y - dxdy.X));
        SkIntersections i = new SkIntersections();
        int used = i.IntersectRay(c2, perp);
        // only keep closest
        if (used == 0 || used == 3)
        {
            Init();
            return;
        }

        _perpT = i[0][0];
        _perpPt = i.Pt(0);
        if (used == 2)
        {
            double distSq = (_perpPt - cPt).LengthSquared();
            double dist2Sq = (i.Pt(1) - cPt).LengthSquared();
            if (dist2Sq < distSq)
            {
                _perpT = i[0][1];
                _perpPt = i.Pt(1);
            }
        }

        _match = cPt.ApproximatelyEqual(_perpPt);
    }
}

internal sealed class SkTSpanBounded
{
    public SkTSpan? Bounded;
    public SkTSpanBounded? Next;
}

internal sealed class SkTSpan
{
    // friend class SkTSect: the fields SkTSect reads and writes are internal.
    internal SkTCurve _part;
    internal SkTCoincident _coinStart = new SkTCoincident();
    internal SkTCoincident _coinEnd = new SkTCoincident();
    internal SkTSpanBounded? _bounded;
    internal SkTSpan? _prev;
    internal SkTSpan? _next;
    internal SkDRect _bounds;
    internal double _startT;
    internal double _endT;
    internal double _boundsMax;
    internal bool _collapsed;
    internal bool _hasPerp;
    internal bool _isLinear;
    internal bool _isLine;
    internal bool _deleted;

    public SkTSpan(SkTCurve curve)
    {
        _part = curve.Make();
    }

    public double EndT()
    {
        return _endT;
    }

    public SkTSpan? FindOppT(double t)
    {
        SkTSpan? result = OppT(t);
        return result;
    }

    public bool HasOppT(double t)
    {
        return OppT(t) != null;
    }

    public bool IsBounded()
    {
        return _bounded != null;
    }

    public void MarkCoincident()
    {
        _coinStart.MarkCoincident();
        _coinEnd.MarkCoincident();
    }

    public SkTSpan? Next()
    {
        return _next;
    }

    public SkTCurve Part()
    {
        return _part;
    }

    public int PointCount()
    {
        return _part.PointCount();
    }

    public ref readonly SkDPoint PointFirst()
    {
        return ref _part[0];
    }

    public ref readonly SkDPoint PointLast()
    {
        return ref _part[_part.PointLast()];
    }

    public void Reset()
    {
        _bounded = null;
    }

    public void ResetBounds(SkTCurve curve)
    {
        _isLinear = _isLine = false;
        InitBounds(curve);
    }

    public bool Split(SkTSpan work)
    {
        return SplitAt(work, (work._startT + work._endT) * 0.5);
    }

    public double StartT()
    {
        return _startT;
    }

    // SkPathOpsTSect.cpp

    public void AddBounded(SkTSpan span)
    {
        SkTSpanBounded bounded = new SkTSpanBounded();
        bounded.Bounded = span;
        bounded.Next = _bounded;
        _bounded = bounded;
    }

    public double ClosestBoundedT(in SkDPoint pt)
    {
        double result = -1;
        double closest = double.MaxValue; // DBL_MAX
        SkTSpanBounded? testBounded = _bounded;
        while (testBounded != null)
        {
            SkTSpan test = testBounded.Bounded!;
            double startDist = test.PointFirst().DistanceSquared(pt);
            if (closest > startDist)
            {
                closest = startDist;
                result = test._startT;
            }

            double endDist = test.PointLast().DistanceSquared(pt);
            if (closest > endDist)
            {
                closest = endDist;
                result = test._endT;
            }

            testBounded = testBounded.Next;
        }

        return result;
    }

    public bool Contains(double t)
    {
        SkTSpan? work = this;
        do
        {
            if (SkPathOps.Between(work._startT, t, work._endT))
            {
                return true;
            }
        }
        while ((work = work._next) != null);

        return false;
    }

    public SkTSpan? FindOppSpan(SkTSpan opp)
    {
        SkTSpanBounded? bounded = _bounded;
        while (bounded != null)
        {
            SkTSpan? test = bounded.Bounded;
            if (opp == test)
            {
                return test;
            }

            bounded = bounded.Next;
        }

        return null;
    }

    // returns 0 if no hull intersection
    //         1 if hulls intersect
    //         2 if hulls only share a common endpoint
    //        -1 if linear and further checking is required
    internal int HullCheck(SkTSpan opp, ref bool start, ref bool oppStart)
    {
        if (_isLinear)
        {
            return -1;
        }

        if (OnlyEndPointsInCommon(opp, ref start, ref oppStart, out bool ptsInCommon))
        {
            return 2;
        }

        bool linear = false;
        if (_part.HullIntersects(opp._part, ref linear))
        {
            if (!linear)
            {
                // check set true if linear
                return 1;
            }

            _isLinear = true;
            _isLine = _part.ControlsInside();
            return ptsInCommon ? 1 : -1;
        }

        // hull is not linear; check set true if intersected at the end points
        return (ptsInCommon ? 1 : 0) << 1; // 0 or 2
    }

    // OPTIMIZE ? If at_most_end_pts_in_common detects that one quad is near linear,
    // use line intersection to guess a better split than 0.5
    // OPTIMIZE Once at_most_end_pts_in_common detects linear, mark span so all future splits are linear
    public int HullsIntersect(SkTSpan opp, ref bool start, ref bool oppStart)
    {
        if (!_bounds.Intersects(opp._bounds))
        {
            return 0;
        }

        int hullSect = HullCheck(opp, ref start, ref oppStart);
        if (hullSect >= 0)
        {
            return hullSect;
        }

        hullSect = opp.HullCheck(this, ref oppStart, ref start);
        if (hullSect >= 0)
        {
            return hullSect;
        }

        return -1;
    }

    public void Init(SkTCurve c)
    {
        _prev = _next = null;
        _startT = 0;
        _endT = 1;
        _bounded = null;
        ResetBounds(c);
    }

    public bool InitBounds(SkTCurve c)
    {
        if (SkPathOps.SkDoubleIsNaN(_startT) || SkPathOps.SkDoubleIsNaN(_endT))
        {
            return false;
        }

        c.SubDivide(_startT, _endT, _part);
        _bounds.SetBounds(_part);
        _coinStart.Init();
        _coinEnd.Init();
        _boundsMax = SkPathOps.StdMax(_bounds.Width(), _bounds.Height());
        _collapsed = _part.Collapsed();
        _hasPerp = false;
        _deleted = false;
        return _bounds.Valid();
    }

    public bool LinearsIntersect(SkTSpan span)
    {
        int result = LinearIntersects(span._part);
        if (result <= 1)
        {
            return result != 0;
        }

        result = span.LinearIntersects(_part);
        return result != 0;
    }

    public double LinearT(in SkDPoint pt)
    {
        SkDVector len = PointLast() - PointFirst();
        return Math.Abs(len.X) > Math.Abs(len.Y)
            ? (pt.X - PointFirst().X) / len.X
            : (pt.Y - PointFirst().Y) / len.Y;
    }

    private int LinearIntersects(SkTCurve q2)
    {
        // looks like q1 is near-linear
        int start = 0, end = _part.PointLast(); // the outside points are usually the extremes
        if (!_part.ControlsInside())
        {
            double dist = 0; // if there's any question, compute distance to find best outsiders
            for (int outer = 0; outer < PointCount() - 1; ++outer)
            {
                for (int inner = outer + 1; inner < PointCount(); ++inner)
                {
                    double test = (_part[outer] - _part[inner]).LengthSquared();
                    if (dist > test)
                    {
                        continue;
                    }

                    dist = test;
                    start = outer;
                    end = inner;
                }
            }
        }

        // see if q2 is on one side of the line formed by the extreme points
        double origX = _part[start].X;
        double origY = _part[start].Y;
        double adj = _part[end].X - origX;
        double opp = _part[end].Y - origY;
        double maxPart = SkPathOps.StdMax(Math.Abs(adj), Math.Abs(opp));
        double sign = 0; // initialization to shut up warning in release build
        for (int n = 0; n < q2.PointCount(); ++n)
        {
            double dx = q2[n].Y - origY;
            double dy = q2[n].X - origX;
            double maxVal = SkPathOps.StdMax(maxPart, SkPathOps.StdMax(Math.Abs(dx), Math.Abs(dy)));
            double test = (q2[n].Y - origY) * adj - (q2[n].X - origX) * opp;
            if (SkPathOps.PreciselyZeroWhenComparedTo(test, maxVal))
            {
                return 1;
            }

            if (SkPathOps.ApproximatelyZeroWhenComparedTo(test, maxVal))
            {
                return 3;
            }

            if (n == 0)
            {
                sign = test;
                continue;
            }

            if (test * sign < 0)
            {
                return 1;
            }
        }

        return 0;
    }

    public bool OnlyEndPointsInCommon(SkTSpan opp, ref bool start, ref bool oppStart, out bool ptsInCommon)
    {
        if (opp.PointFirst() == PointFirst())
        {
            start = oppStart = true;
        }
        else if (opp.PointFirst() == PointLast())
        {
            start = false;
            oppStart = true;
        }
        else if (opp.PointLast() == PointFirst())
        {
            start = true;
            oppStart = false;
        }
        else if (opp.PointLast() == PointLast())
        {
            start = oppStart = false;
        }
        else
        {
            ptsInCommon = false;
            return false;
        }

        ptsInCommon = true;
        Span<SkDPoint> otherPts = stackalloc SkDPoint[4];
        Span<SkDPoint> oppOtherPts = stackalloc SkDPoint[4];
        int baseIndex = start ? 0 : _part.PointLast();
        _part.OtherPts(baseIndex, otherPts);
        opp._part.OtherPts(oppStart ? 0 : opp._part.PointLast(), oppOtherPts);
        SkDPoint @base = _part[baseIndex];
        for (int o1 = 0; o1 < PointCount() - 1; ++o1)
        {
            SkDVector v1 = otherPts[o1] - @base;
            for (int o2 = 0; o2 < opp.PointCount() - 1; ++o2)
            {
                SkDVector v2 = oppOtherPts[o2] - @base;
                if (v2.Dot(v1) >= 0)
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal SkTSpan? OppT(double t)
    {
        SkTSpanBounded? bounded = _bounded;
        while (bounded != null)
        {
            SkTSpan test = bounded.Bounded!;
            if (SkPathOps.Between(test._startT, t, test._endT))
            {
                return test;
            }

            bounded = bounded.Next;
        }

        return null;
    }

    public bool RemoveAllBounded()
    {
        bool deleteSpan = false;
        SkTSpanBounded? bounded = _bounded;
        while (bounded != null)
        {
            SkTSpan opp = bounded.Bounded!;
            deleteSpan |= opp.RemoveBounded(this);
            bounded = bounded.Next;
        }

        return deleteSpan;
    }

    public bool RemoveBounded(SkTSpan opp)
    {
        if (_hasPerp)
        {
            bool foundStart = false;
            bool foundEnd = false;
            SkTSpanBounded? testBounded = _bounded; // Skia's inner `bounded`
            while (testBounded != null)
            {
                SkTSpan test = testBounded.Bounded!;
                if (opp != test)
                {
                    foundStart |= SkPathOps.Between(test._startT, _coinStart.PerpT(), test._endT);
                    foundEnd |= SkPathOps.Between(test._startT, _coinEnd.PerpT(), test._endT);
                }

                testBounded = testBounded.Next;
            }

            if (!foundStart || !foundEnd)
            {
                _hasPerp = false;
                _coinStart.Init();
                _coinEnd.Init();
            }
        }

        SkTSpanBounded? bounded = _bounded;
        SkTSpanBounded? prev = null;
        while (bounded != null)
        {
            SkTSpanBounded? boundedNext = bounded.Next;
            if (opp == bounded.Bounded)
            {
                if (prev != null)
                {
                    prev.Next = boundedNext;
                    return false;
                }
                else
                {
                    _bounded = boundedNext;
                    return _bounded == null;
                }
            }

            prev = bounded;
            bounded = boundedNext;
        }

        return false;
    }

    public bool SplitAt(SkTSpan work, double t)
    {
        _startT = t;
        _endT = work._endT;
        if (_startT == _endT)
        {
            _collapsed = true;
            return false;
        }

        work._endT = t;
        if (work._startT == work._endT)
        {
            work._collapsed = true;
            return false;
        }

        _prev = work;
        _next = work._next;
        _isLinear = work._isLinear;
        _isLine = work._isLine;

        work._next = this;
        if (_next != null)
        {
            _next._prev = this;
        }

        SkTSpanBounded? bounded = work._bounded;
        _bounded = null;
        while (bounded != null)
        {
            AddBounded(bounded.Bounded!);
            bounded = bounded.Next;
        }

        bounded = _bounded;
        while (bounded != null)
        {
            bounded.Bounded!.AddBounded(this);
            bounded = bounded.Next;
        }

        return true;
    }
}

internal sealed class SkTSect
{
    private const int KZeroS1Set = 1;
    private const int KOneS1Set = 2;
    private const int KZeroS2Set = 4;
    private const int KOneS2Set = 8;

    private readonly SkTCurve _curve;
    private SkTSpan? _head;
    private SkTSpan? _coincident;
    private SkTSpan? _deleted;
    private int _activeCount;
    private bool _removedStartT;
    private bool _removedEndT;
    private bool _hung;

    public SkTSect(SkTCurve c)
    {
        _curve = c;
        _coincident = null;
        _deleted = null;
        _activeCount = 0;
        _hung = false;
        ResetRemovedEnds();
        _head = AddOne();
        _head.Init(c);
    }

    private SkTSpan AddSplitAt(SkTSpan span, double t)
    {
        SkTSpan result = AddOne();
        result.SplitAt(span, t);
        result.InitBounds(_curve);
        span.InitBounds(_curve);
        return result;
    }

    private ref readonly SkDPoint PointLast()
    {
        return ref _curve[_curve.PointLast()];
    }

    private void ResetRemovedEnds()
    {
        _removedStartT = _removedEndT = false;
    }

    // SkPathOpsTSect.cpp

    private SkTSpan AddFollowing(SkTSpan? prior)
    {
        SkTSpan result = AddOne();
        result._startT = prior != null ? prior._endT : 0;
        SkTSpan? next = prior != null ? prior._next : _head;
        result._endT = next != null ? next._startT : 1;
        result._prev = prior;
        result._next = next;
        if (prior != null)
        {
            prior._next = result;
        }
        else
        {
            _head = result;
        }

        if (next != null)
        {
            next._prev = result;
        }

        result.ResetBounds(_curve);
        // world may not be consistent to call validate here
        return result;
    }

    private void AddForPerp(SkTSpan span, double t)
    {
        if (!span.HasOppT(t))
        {
            SkTSpan? opp = SpanAtT(t, out SkTSpan? priorSpan);
            if (opp == null)
            {
                opp = AddFollowing(priorSpan);
            }

            opp.AddBounded(span);
            span.AddBounded(opp);
        }
    }

    private SkTSpan AddOne()
    {
        SkTSpan result;
        if (_deleted != null)
        {
            result = _deleted;
            _deleted = result._next;
        }
        else
        {
            result = new SkTSpan(_curve);
        }

        result.Reset();
        result._hasPerp = false;
        result._deleted = false;
        ++_activeCount;
        return result;
    }

    private bool BinarySearchCoin(
        SkTSect sect2,
        double tStart,
        double tStep,
        ref double resultT,
        ref double oppT,
        ref SkTSpan? oppFirst)
    {
        SkTSpan work = new SkTSpan(_curve);
        double result = work._startT = work._endT = tStart;
        SkDPoint last = _curve.PtAtT(tStart);
        SkDPoint oppPt = default;
        bool flip = false;
        bool contained = false;
        bool down = tStep < 0;
        SkTCurve opp = sect2._curve;
        do
        {
            tStep *= 0.5;
            work._startT += tStep;
            if (flip)
            {
                tStep = -tStep;
                flip = false;
            }

            work.InitBounds(_curve);
            if (work._collapsed)
            {
                return false;
            }

            if (last.ApproximatelyEqual(work.PointFirst()))
            {
                break;
            }

            last = work.PointFirst();
            work._coinStart.SetPerp(_curve, work._startT, last, opp);
            if (work._coinStart.IsMatch())
            {
                double oppTTest = work._coinStart.PerpT();
                if (sect2._head!.Contains(oppTTest))
                {
                    oppT = oppTTest;
                    oppPt = work._coinStart.PerpPt();
                    contained = true;
                    if (down ? result <= work._startT : result >= work._startT)
                    {
                        oppFirst = null; // signal caller to fail
                        return false;
                    }

                    result = work._startT;
                    continue;
                }
            }

            tStep = -tStep;
            flip = true;
        }
        while (true);

        if (!contained)
        {
            return false;
        }

        if (last.ApproximatelyEqual(_curve[0]))
        {
            result = 0;
        }
        else if (last.ApproximatelyEqual(PointLast()))
        {
            result = 1;
        }

        if (oppPt.ApproximatelyEqual(opp[0]))
        {
            oppT = 0;
        }
        else if (oppPt.ApproximatelyEqual(sect2.PointLast()))
        {
            oppT = 1;
        }

        resultT = result;
        return true;
    }

    // OPTIMIZE ? keep a sorted list of sizes in the form of a doubly-linked list in quad span
    //            so that each quad sect has a pointer to the largest, and can update it as spans
    //            are split
    private SkTSpan? BoundsMax()
    {
        SkTSpan? test = _head!;
        SkTSpan largest = _head!;
        bool lCollapsed = largest._collapsed;
        int safetyNet = 10000;
        while ((test = test._next) != null)
        {
            if (--safetyNet == 0)
            {
                _hung = true;
                return null;
            }

            bool tCollapsed = test._collapsed;
            if ((lCollapsed && !tCollapsed)
                || (lCollapsed == tCollapsed && largest._boundsMax < test._boundsMax))
            {
                largest = test;
                lCollapsed = test._collapsed;
            }
        }

        return largest;
    }

    private bool CoincidentCheck(SkTSect sect2)
    {
        SkTSpan? first = _head;
        if (first == null)
        {
            return false;
        }

        SkTSpan? last = null;
        SkTSpan? next;
        do
        {
            int consecutive = CountConsecutiveSpans(first, out last);
            next = last._next;
            if (consecutive < SkPathOpsTSectStatics.CoincidentSpanCount)
            {
                continue;
            }

            ComputePerpendiculars(sect2, first, last);
            // check to see if a range of points are on the curve
            SkTSpan? coinStart = first;
            do
            {
                bool success = ExtractCoincident(sect2, coinStart, last, ref coinStart);
                if (!success)
                {
                    return false;
                }
            }
            while (coinStart != null && !last._deleted);

            if (_head == null || sect2._head == null)
            {
                break;
            }

            if (next == null || next._deleted)
            {
                break;
            }
        }
        while ((first = next) != null);

        return true;
    }

    private void CoincidentForce(SkTSect sect2, double start1s, double start1e)
    {
        SkTSpan first = _head!;
        SkTSpan? last = Tail();
        SkTSpan oppFirst = sect2._head!;
        SkTSpan? oppLast = sect2.Tail();
        if (last == null || oppLast == null)
        {
            return;
        }

        bool deleteEmptySpans = UpdateBounded(first, last, oppFirst);
        deleteEmptySpans |= sect2.UpdateBounded(oppFirst, oppLast, first);
        RemoveSpanRange(first, last);
        sect2.RemoveSpanRange(oppFirst, oppLast);
        first._startT = start1s;
        first._endT = start1e;
        first.ResetBounds(_curve);
        first._coinStart.SetPerp(_curve, start1s, _curve[0], sect2._curve);
        first._coinEnd.SetPerp(_curve, start1e, PointLast(), sect2._curve);
        bool oppMatched = first._coinStart.PerpT() < first._coinEnd.PerpT();
        double oppStartT = first._coinStart.PerpT() == -1 ? 0 : SkPathOps.StdMax(0.0, first._coinStart.PerpT());
        double oppEndT = first._coinEnd.PerpT() == -1 ? 1 : SkPathOps.StdMin(1.0, first._coinEnd.PerpT());
        if (!oppMatched)
        {
            (oppStartT, oppEndT) = (oppEndT, oppStartT);
        }

        oppFirst._startT = oppStartT;
        oppFirst._endT = oppEndT;
        oppFirst.ResetBounds(sect2._curve);
        RemoveCoincident(first, false);
        sect2.RemoveCoincident(oppFirst, true);
        if (deleteEmptySpans)
        {
            DeleteEmptySpans();
            sect2.DeleteEmptySpans();
        }
    }

    private bool CoincidentHasT(double t)
    {
        SkTSpan? test = _coincident;
        while (test != null)
        {
            if (SkPathOps.Between(test._startT, t, test._endT))
            {
                return true;
            }

            test = test._next;
        }

        return false;
    }

    private int Collapsed()
    {
        int result = 0;
        SkTSpan? test = _head;
        while (test != null)
        {
            if (test._collapsed)
            {
                ++result;
            }

            test = test.Next();
        }

        return result;
    }

    private void ComputePerpendiculars(SkTSect sect2, SkTSpan first, SkTSpan? last)
    {
        if (last == null)
        {
            return;
        }

        SkTCurve opp = sect2._curve;
        SkTSpan work = first;
        SkTSpan? prior = null;
        do
        {
            if (!work._hasPerp && !work._collapsed)
            {
                if (prior != null)
                {
                    work._coinStart = prior._coinEnd;
                }
                else
                {
                    work._coinStart.SetPerp(_curve, work._startT, work.PointFirst(), opp);
                }

                if (work._coinStart.IsMatch())
                {
                    double perpT = work._coinStart.PerpT();
                    if (sect2.CoincidentHasT(perpT))
                    {
                        work._coinStart.Init();
                    }
                    else
                    {
                        sect2.AddForPerp(work, perpT);
                    }
                }

                work._coinEnd.SetPerp(_curve, work._endT, work.PointLast(), opp);
                if (work._coinEnd.IsMatch())
                {
                    double perpT = work._coinEnd.PerpT();
                    if (sect2.CoincidentHasT(perpT))
                    {
                        work._coinEnd.Init();
                    }
                    else
                    {
                        sect2.AddForPerp(work, perpT);
                    }
                }

                work._hasPerp = true;
            }

            if (work == last)
            {
                break;
            }

            prior = work;
            work = work._next!;
        }
        while (true);
    }

    private int CountConsecutiveSpans(SkTSpan first, out SkTSpan lastPtr)
    {
        int consecutive = 1;
        SkTSpan last = first;
        do
        {
            SkTSpan? next = last._next;
            if (next == null)
            {
                break;
            }

            if (next._startT > last._endT)
            {
                break;
            }

            ++consecutive;
            last = next;
        }
        while (true);

        lastPtr = last;
        return consecutive;
    }

    public bool HasBounded(SkTSpan span)
    {
        SkTSpan? test = _head;
        if (test == null)
        {
            return false;
        }

        do
        {
            if (test.FindOppSpan(span) != null)
            {
                return true;
            }
        }
        while ((test = test.Next()) != null);

        return false;
    }

    private bool DeleteEmptySpans()
    {
        SkTSpan? test;
        SkTSpan? next = _head;
        int safetyHatch = 1000;
        while ((test = next) != null)
        {
            next = test._next;
            if (test._bounded == null)
            {
                if (!RemoveSpan(test))
                {
                    return false;
                }
            }

            if (--safetyHatch < 0)
            {
                return false;
            }
        }

        return true;
    }

    private bool ExtractCoincident(SkTSect sect2, SkTSpan? first, SkTSpan? last, ref SkTSpan? result)
    {
        first = FindCoincidentRun(first!, ref last);
        if (first == null || last == null)
        {
            result = null;
            return true;
        }

        // march outwards to find limit of coincidence from here to previous and next spans
        double startT = first._startT;
        double oppStartT = 0; // SK_INIT_TO_AVOID_WARNING
        double oppEndT = 0; // SK_INIT_TO_AVOID_WARNING
        SkTSpan? prev = first._prev;
        SkTSpan? oppFirst = first.FindOppT(first._coinStart.PerpT());
        bool oppMatched = first._coinStart.PerpT() < first._coinEnd.PerpT();
        double coinStart = 0; // uninitialized in Skia; only read after binarySearchCoin sets it
        SkTSpan? cutFirst = null;
        if (prev != null && prev._endT == startT
            && BinarySearchCoin(
                sect2,
                startT,
                prev._startT - startT,
                ref coinStart,
                ref oppStartT,
                ref oppFirst)
            && prev._startT < coinStart && coinStart < startT
            && (cutFirst = prev.OppT(oppStartT)) != null)
        {
            oppFirst = cutFirst;
            first = AddSplitAt(prev, coinStart);
            first.MarkCoincident();
            prev._coinEnd.MarkCoincident();
            if (oppFirst._startT < oppStartT && oppStartT < oppFirst._endT)
            {
                SkTSpan oppHalf = sect2.AddSplitAt(oppFirst, oppStartT);
                if (oppMatched)
                {
                    oppFirst._coinEnd.MarkCoincident();
                    oppHalf.MarkCoincident();
                    oppFirst = oppHalf;
                }
                else
                {
                    oppFirst.MarkCoincident();
                    oppHalf._coinStart.MarkCoincident();
                }
            }
        }
        else
        {
            if (oppFirst == null)
            {
                return false;
            }
        }

        // FIXME: incomplete : if we're not at the end, find end of coin
        SkTSpan? oppLast;
        oppLast = last.FindOppT(last._coinEnd.PerpT());
        if (!oppMatched)
        {
            (oppFirst, oppLast) = (oppLast, oppFirst);
            (oppStartT, oppEndT) = (oppEndT, oppStartT);
        }

        if (oppFirst == null)
        {
            result = null;
            return true;
        }

        if (oppLast == null)
        {
            result = null;
            return true;
        }

        // reduce coincident runs to single entries
        bool deleteEmptySpans = UpdateBounded(first, last, oppFirst);
        deleteEmptySpans |= sect2.UpdateBounded(oppFirst, oppLast, first);
        RemoveSpanRange(first, last);
        sect2.RemoveSpanRange(oppFirst, oppLast);
        first._endT = last._endT;
        first.ResetBounds(_curve);
        first._coinStart.SetPerp(_curve, first._startT, first.PointFirst(), sect2._curve);
        first._coinEnd.SetPerp(_curve, first._endT, first.PointLast(), sect2._curve);
        oppStartT = first._coinStart.PerpT();
        oppEndT = first._coinEnd.PerpT();
        if (SkPathOps.Between(0, oppStartT, 1) && SkPathOps.Between(0, oppEndT, 1))
        {
            if (!oppMatched)
            {
                (oppStartT, oppEndT) = (oppEndT, oppStartT);
            }

            oppFirst._startT = oppStartT;
            oppFirst._endT = oppEndT;
            oppFirst.ResetBounds(sect2._curve);
        }

        last = first._next;
        if (!RemoveCoincident(first, false))
        {
            return false;
        }

        if (!sect2.RemoveCoincident(oppFirst, true))
        {
            return false;
        }

        if (deleteEmptySpans)
        {
            if (!DeleteEmptySpans() || !sect2.DeleteEmptySpans())
            {
                result = null;
                return false;
            }
        }

        result = last != null && !last._deleted && _head != null && sect2._head != null ? last : null;
        return true;
    }

    private SkTSpan? FindCoincidentRun(SkTSpan first, ref SkTSpan? lastPtr)
    {
        SkTSpan? work = first;
        SkTSpan? lastCandidate = null;
        SkTSpan? firstResult = null; // Skia reuses `first`
        // find the first fully coincident span
        do
        {
            if (work._coinStart.IsMatch())
            {
                if (!work._coinEnd.IsMatch())
                {
                    break;
                }

                lastCandidate = work;
                if (firstResult == null)
                {
                    firstResult = work;
                }
            }
            else if (firstResult != null && work._collapsed)
            {
                lastPtr = lastCandidate;
                return firstResult;
            }
            else
            {
                lastCandidate = null;
            }

            if (work == lastPtr)
            {
                return firstResult;
            }

            work = work._next;
            if (work == null)
            {
                return null;
            }
        }
        while (true);

        if (lastCandidate != null)
        {
            lastPtr = lastCandidate;
        }

        return firstResult;
    }

    private int Intersects(SkTSpan span, SkTSect opp, SkTSpan oppSpan, ref int oppResult)
    {
        bool spanStart = false, oppStart = false;
        int hullResult = span.HullsIntersect(oppSpan, ref spanStart, ref oppStart);
        if (hullResult >= 0)
        {
            if (hullResult == 2)
            {
                // hulls have one point in common
                if (span._bounded == null || span._bounded.Next == null)
                {
                    if (spanStart)
                    {
                        span._endT = span._startT;
                    }
                    else
                    {
                        span._startT = span._endT;
                    }
                }
                else
                {
                    hullResult = 1;
                }

                if (oppSpan._bounded == null || oppSpan._bounded.Next == null)
                {
                    if (oppSpan._bounded != null && oppSpan._bounded.Bounded != span)
                    {
                        return 0;
                    }

                    if (oppStart)
                    {
                        oppSpan._endT = oppSpan._startT;
                    }
                    else
                    {
                        oppSpan._startT = oppSpan._endT;
                    }

                    oppResult = 2;
                }
                else
                {
                    oppResult = 1;
                }
            }
            else
            {
                oppResult = 1;
            }

            return hullResult;
        }

        if (span._isLine && oppSpan._isLine)
        {
            SkIntersections i = new SkIntersections();
            int sects = LinesIntersect(span, opp, oppSpan, i);
            if (sects == 2)
            {
                return oppResult = 1;
            }

            if (sects == 0)
            {
                return -1;
            }

            RemovedEndCheck(span);
            span._startT = span._endT = i[0][0];
            opp.RemovedEndCheck(oppSpan);
            oppSpan._startT = oppSpan._endT = i[1][0];
            return oppResult = 2;
        }

        if (span._isLinear || oppSpan._isLinear)
        {
            return oppResult = span.LinearsIntersect(oppSpan) ? 1 : 0;
        }

        return oppResult = 1;
    }

    // while the intersection points are sufficiently far apart:
    // construct the tangent lines from the intersections
    // find the point where the tangent line intersects the opposite curve
    private int LinesIntersect(SkTSpan span, SkTSect opp, SkTSpan oppSpan, SkIntersections i)
    {
        SkIntersections thisRayI = new SkIntersections();
        SkIntersections oppRayI = new SkIntersections();
        SkDLine thisLine = new SkDLine(span.PointFirst(), span.PointLast());
        SkDLine oppLine = new SkDLine(oppSpan.PointFirst(), oppSpan.PointLast());
        int loopCount = 0;
        double bestDistSq = double.MaxValue; // DBL_MAX
        if (thisRayI.IntersectRay(opp._curve, thisLine) == 0)
        {
            return 0;
        }

        if (oppRayI.IntersectRay(_curve, oppLine) == 0)
        {
            return 0;
        }

        // if the ends of each line intersect the opposite curve, the lines are coincident
        if (thisRayI.Used() > 1)
        {
            int ptMatches = 0;
            for (int tIndex = 0; tIndex < thisRayI.Used(); ++tIndex)
            {
                for (int lIndex = 0; lIndex < 2; ++lIndex)
                {
                    ptMatches += thisRayI.Pt(tIndex).ApproximatelyEqual(thisLine.Pts[lIndex]) ? 1 : 0;
                }
            }

            if (ptMatches == 2 || SkPathOpsTSectStatics.IsParallel(thisLine, opp._curve))
            {
                return 2;
            }
        }

        if (oppRayI.Used() > 1)
        {
            int ptMatches = 0;
            for (int oIndex = 0; oIndex < oppRayI.Used(); ++oIndex)
            {
                for (int lIndex = 0; lIndex < 2; ++lIndex)
                {
                    ptMatches += oppRayI.Pt(oIndex).ApproximatelyEqual(oppLine.Pts[lIndex]) ? 1 : 0;
                }
            }

            if (ptMatches == 2 || SkPathOpsTSectStatics.IsParallel(oppLine, _curve))
            {
                return 2;
            }
        }

        do
        {
            // pick the closest pair of points
            double closest = double.MaxValue; // DBL_MAX
            int closeIndex = 0; // SK_INIT_TO_AVOID_WARNING
            int oppCloseIndex = 0; // SK_INIT_TO_AVOID_WARNING
            for (int index = 0; index < oppRayI.Used(); ++index)
            {
                if (!SkPathOps.RoughlyBetween(span._startT, oppRayI[0][index], span._endT))
                {
                    continue;
                }

                for (int oIndex = 0; oIndex < thisRayI.Used(); ++oIndex)
                {
                    if (!SkPathOps.RoughlyBetween(oppSpan._startT, thisRayI[0][oIndex], oppSpan._endT))
                    {
                        continue;
                    }

                    double distSq = thisRayI.Pt(index).DistanceSquared(oppRayI.Pt(oIndex));
                    if (closest > distSq)
                    {
                        closest = distSq;
                        closeIndex = index;
                        oppCloseIndex = oIndex;
                    }
                }
            }

            if (closest == double.MaxValue)
            {
                break;
            }

            ref readonly SkDPoint oppIPt = ref thisRayI.Pt(oppCloseIndex);
            ref readonly SkDPoint iPt = ref oppRayI.Pt(closeIndex);
            if (SkPathOps.Between(span._startT, oppRayI[0][closeIndex], span._endT)
                && SkPathOps.Between(oppSpan._startT, thisRayI[0][oppCloseIndex], oppSpan._endT)
                && oppIPt.ApproximatelyEqual(iPt))
            {
                i.Merge(oppRayI, closeIndex, thisRayI, oppCloseIndex);
                return i.Used();
            }

            double distSqI = oppIPt.DistanceSquared(iPt); // Skia's loop-level `distSq`
            if (bestDistSq < distSqI || ++loopCount > 5)
            {
                return 0;
            }

            bestDistSq = distSqI;
            double oppStart = oppRayI[0][closeIndex];
            thisLine[0] = _curve.PtAtT(oppStart);
            thisLine[1] = thisLine[0] + _curve.DxdyAtT(oppStart);
            if (thisRayI.IntersectRay(opp._curve, thisLine) == 0)
            {
                break;
            }

            double start = thisRayI[0][oppCloseIndex];
            oppLine[0] = opp._curve.PtAtT(start);
            oppLine[1] = oppLine[0] + opp._curve.DxdyAtT(start);
            if (oppRayI.IntersectRay(_curve, oppLine) == 0)
            {
                break;
            }
        }
        while (true);

        // convergence may fail if the curves are nearly coincident
        SkTCoincident oCoinS = new SkTCoincident();
        SkTCoincident oCoinE = new SkTCoincident();
        oCoinS.SetPerp(opp._curve, oppSpan._startT, oppSpan.PointFirst(), _curve);
        oCoinE.SetPerp(opp._curve, oppSpan._endT, oppSpan.PointLast(), _curve);
        double tStart = oCoinS.PerpT();
        double tEnd = oCoinE.PerpT();
        bool swap = tStart > tEnd;
        if (swap)
        {
            (tStart, tEnd) = (tEnd, tStart);
        }

        tStart = SkPathOps.StdMax(tStart, span._startT);
        tEnd = SkPathOps.StdMin(tEnd, span._endT);
        if (tStart > tEnd)
        {
            return 0;
        }

        SkDVector perpS, perpE;
        if (tStart == span._startT)
        {
            SkTCoincident coinS = new SkTCoincident();
            coinS.SetPerp(_curve, span._startT, span.PointFirst(), opp._curve);
            perpS = span.PointFirst() - coinS.PerpPt();
        }
        else if (swap)
        {
            perpS = oCoinE.PerpPt() - oppSpan.PointLast();
        }
        else
        {
            perpS = oCoinS.PerpPt() - oppSpan.PointFirst();
        }

        if (tEnd == span._endT)
        {
            SkTCoincident coinE = new SkTCoincident();
            coinE.SetPerp(_curve, span._endT, span.PointLast(), opp._curve);
            perpE = span.PointLast() - coinE.PerpPt();
        }
        else if (swap)
        {
            perpE = oCoinS.PerpPt() - oppSpan.PointFirst();
        }
        else
        {
            perpE = oCoinE.PerpPt() - oppSpan.PointLast();
        }

        if (perpS.Dot(perpE) >= 0)
        {
            return 0;
        }

        SkTCoincident coinW = new SkTCoincident();
        double workT = tStart;
        double tStep = tEnd - tStart;
        SkDPoint workPt;
        do
        {
            tStep *= 0.5;
            if (SkPathOps.PreciselyZero(tStep))
            {
                return 0;
            }

            workT += tStep;
            workPt = _curve.PtAtT(workT);
            coinW.SetPerp(_curve, workT, workPt, opp._curve);
            double perpT = coinW.PerpT();
            if (coinW.IsMatch() ? !SkPathOps.Between(oppSpan._startT, perpT, oppSpan._endT) : perpT < 0)
            {
                continue;
            }

            SkDVector perpW = workPt - coinW.PerpPt();
            if ((perpS.Dot(perpW) >= 0) == (tStep < 0))
            {
                tStep = -tStep;
            }

            if (workPt.ApproximatelyEqual(coinW.PerpPt()))
            {
                break;
            }
        }
        while (true);

        double oppTTest = coinW.PerpT();
        if (!opp._head!.Contains(oppTTest))
        {
            return 0;
        }

        i.SetMax(1);
        i.Insert(workT, oppTTest, workPt);
        return 1;
    }

    private bool MarkSpanGone(SkTSpan span)
    {
        if (--_activeCount < 0)
        {
            return false;
        }

        span._next = _deleted;
        _deleted = span;
        span._deleted = true;
        return true;
    }

    private bool MatchedDirection(double t, SkTSect sect2, double t2)
    {
        SkDVector dxdy = _curve.DxdyAtT(t);
        SkDVector dxdy2 = sect2._curve.DxdyAtT(t2);
        return dxdy.Dot(dxdy2) >= 0;
    }

    private void MatchedDirCheck(double t, SkTSect sect2, double t2, ref bool calcMatched, ref bool oppMatched)
    {
        if (calcMatched)
        {
            // Skia only asserts oppMatched == matchedDirection(t, sect2, t2) here.
        }
        else
        {
            oppMatched = MatchedDirection(t, sect2, t2);
            calcMatched = true;
        }
    }

    private void MergeCoincidence(SkTSect sect2)
    {
        double smallLimit = 0;
        do
        {
            // find the smallest unprocessed span
            SkTSpan? smaller = null;
            SkTSpan? test = _coincident;
            do
            {
                if (test == null)
                {
                    return;
                }

                if (test._startT < smallLimit)
                {
                    continue;
                }

                if (smaller != null && smaller._endT < test._startT)
                {
                    continue;
                }

                smaller = test;
            }
            while ((test = test._next) != null);

            if (smaller == null)
            {
                return;
            }

            smallLimit = smaller._endT;
            // find next larger span
            SkTSpan? prior = null;
            SkTSpan? larger = null;
            SkTSpan? largerPrior = null;
            test = _coincident!;
            do
            {
                if (test._startT < smaller._endT)
                {
                    continue;
                }

                if (larger != null && larger._startT < test._startT)
                {
                    continue;
                }

                largerPrior = prior;
                larger = test;
            }
            while ((prior = test) != null && (test = test._next) != null); // (void) (prior = test), (test = ...)

            if (larger == null)
            {
                continue;
            }

            // check middle t value to see if it is coincident as well
            double midT = (smaller._endT + larger._startT) / 2;
            SkDPoint midPt = _curve.PtAtT(midT);
            SkTCoincident coin = new SkTCoincident();
            coin.SetPerp(_curve, midT, midPt, sect2._curve);
            if (coin.IsMatch())
            {
                smaller._endT = larger._endT;
                smaller._coinEnd = larger._coinEnd;
                if (largerPrior != null)
                {
                    largerPrior._next = larger._next;
                }
                else
                {
                    _coincident = larger._next;
                }
            }
        }
        while (true);
    }

    private SkTSpan? Prev(SkTSpan span)
    {
        SkTSpan? result = null;
        SkTSpan? test = _head;
        while (span != test)
        {
            result = test;
            test = test!._next;
        }

        return result;
    }

    private void RecoverCollapsed()
    {
        SkTSpan? deleted = _deleted;
        while (deleted != null)
        {
            SkTSpan? delNext = deleted._next;
            if (deleted._collapsed)
            {
                // Skia walks SkTSpan** spanPtr from &fHead; spanOwner is the span whose fNext spanPtr points at
                // (null while spanPtr is &fHead).
                SkTSpan? spanOwner = null;
                SkTSpan? spanAt = _head;
                while (spanAt != null && spanAt._endT <= deleted._startT)
                {
                    spanOwner = spanAt;
                    spanAt = spanAt._next;
                }

                deleted._next = spanAt;
                if (spanOwner == null)
                {
                    _head = deleted;
                }
                else
                {
                    spanOwner._next = deleted;
                }
            }

            deleted = delNext;
        }
    }

    private void RemoveAllBut(SkTSpan keep, SkTSpan span, SkTSect opp)
    {
        SkTSpanBounded? testBounded = span._bounded;
        while (testBounded != null)
        {
            SkTSpan bounded = testBounded.Bounded!;
            SkTSpanBounded? next = testBounded.Next;
            // may have been deleted when opp did 'remove all but'
            if (bounded != keep && !bounded._deleted)
            {
                span.RemoveBounded(bounded); // SkAssertResult: evaluated, result unused in release
                if (bounded.RemoveBounded(span))
                {
                    opp.RemoveSpan(bounded);
                }
            }

            testBounded = next;
        }
    }

    private bool RemoveByPerpendicular(SkTSect opp)
    {
        SkTSpan? test = _head!;
        SkTSpan? next;
        do
        {
            next = test._next;
            if (test._coinStart.PerpT() < 0 || test._coinEnd.PerpT() < 0)
            {
                continue;
            }

            SkDVector startV = test._coinStart.PerpPt() - test.PointFirst();
            SkDVector endV = test._coinEnd.PerpPt() - test.PointLast();
            if (startV.Dot(endV) <= 0)
            {
                continue;
            }

            if (!RemoveSpans(test, opp))
            {
                return false;
            }
        }
        while ((test = next) != null);

        return true;
    }

    private bool RemoveCoincident(SkTSpan span, bool isBetween)
    {
        if (!UnlinkSpan(span))
        {
            return false;
        }

        if (isBetween || SkPathOps.Between(0, span._coinStart.PerpT(), 1))
        {
            --_activeCount;
            span._next = _coincident;
            _coincident = span;
        }
        else
        {
            MarkSpanGone(span);
        }

        return true;
    }

    private void RemovedEndCheck(SkTSpan span)
    {
        if (span._startT == 0)
        {
            _removedStartT = true;
        }

        if (1 == span._endT)
        {
            _removedEndT = true;
        }
    }

    private bool RemoveSpan(SkTSpan span)
    {
        RemovedEndCheck(span);
        if (!UnlinkSpan(span))
        {
            return false;
        }

        return MarkSpanGone(span);
    }

    private void RemoveSpanRange(SkTSpan first, SkTSpan last)
    {
        if (first == last)
        {
            return;
        }

        SkTSpan? span = first;
        SkTSpan? final = last._next;
        SkTSpan? next = span._next;
        while ((span = next) != null && span != final)
        {
            next = span._next;
            MarkSpanGone(span);
        }

        if (final != null)
        {
            final._prev = first;
        }

        first._next = final;
        // world may not be ready for validation here
    }

    private bool RemoveSpans(SkTSpan span, SkTSect opp)
    {
        SkTSpanBounded? bounded = span._bounded;
        while (bounded != null)
        {
            SkTSpan spanBounded = bounded.Bounded!;
            SkTSpanBounded? next = bounded.Next;
            if (span.RemoveBounded(spanBounded))
            {
                // shuffles last into position 0
                RemoveSpan(span);
            }

            if (spanBounded.RemoveBounded(span))
            {
                opp.RemoveSpan(spanBounded);
            }

            if (span._deleted && opp.HasBounded(span))
            {
                return false;
            }

            bounded = next;
        }

        return true;
    }

    private SkTSpan? SpanAtT(double t, out SkTSpan? priorSpan)
    {
        SkTSpan? test = _head;
        SkTSpan? prev = null;
        while (test != null && test._endT < t)
        {
            prev = test;
            test = test._next;
        }

        priorSpan = prev;
        return test != null && test._startT <= t ? test : null;
    }

    private SkTSpan? Tail()
    {
        SkTSpan result = _head!;
        SkTSpan? next = _head!;
        int safetyNet = 100000;
        while ((next = next._next) != null)
        {
            if (--safetyNet == 0)
            {
                return null;
            }

            if (next._endT > result._endT)
            {
                result = next;
            }
        }

        return result;
    }

    /* Each span has a range of opposite spans it intersects. After the span is split in two,
        adjust the range to its new size */
    private bool Trim(SkTSpan span, SkTSect opp)
    {
        if (!span.InitBounds(_curve))
        {
            return false; // FAIL_IF
        }

        SkTSpanBounded? testBounded = span._bounded;
        while (testBounded != null)
        {
            SkTSpan test = testBounded.Bounded!;
            SkTSpanBounded? next = testBounded.Next;
            int oppSects = 0;
            int sects = Intersects(span, opp, test, ref oppSects);
            if (sects >= 1)
            {
                if (oppSects == 2)
                {
                    test.InitBounds(opp._curve);
                    opp.RemoveAllBut(span, test, this);
                }

                if (sects == 2)
                {
                    span.InitBounds(_curve);
                    RemoveAllBut(test, span, opp);
                    return true;
                }
            }
            else
            {
                if (span.RemoveBounded(test))
                {
                    RemoveSpan(span);
                }

                if (test.RemoveBounded(span))
                {
                    opp.RemoveSpan(test);
                }
            }

            testBounded = next;
        }

        return true;
    }

    private bool UnlinkSpan(SkTSpan span)
    {
        SkTSpan? prev = span._prev;
        SkTSpan? next = span._next;
        if (prev != null)
        {
            prev._next = next;
            if (next != null)
            {
                next._prev = prev;
                if (next._startT > next._endT)
                {
                    return false;
                }

                // world may not be ready for validate here
            }
        }
        else
        {
            _head = next;
            if (next != null)
            {
                next._prev = null;
            }
        }

        return true;
    }

    private bool UpdateBounded(SkTSpan first, SkTSpan last, SkTSpan oppFirst)
    {
        SkTSpan? test = first;
        SkTSpan? final = last.Next();
        bool deleteSpan = false;
        do
        {
            deleteSpan |= test.RemoveAllBounded();
        }
        while ((test = test._next) != final && test != null);

        first._bounded = null;
        first.AddBounded(oppFirst);
        // cannot call validate until remove span range is called
        return deleteSpan;
    }

    private static int EndsEqual(SkTSect sect1, SkTSect sect2, SkIntersections intersections)
    {
        int zeroOneSet = 0;
        if (sect1._curve[0] == sect2._curve[0])
        {
            zeroOneSet |= KZeroS1Set | KZeroS2Set;
            intersections.Insert(0, 0, sect1._curve[0]);
        }

        if (sect1._curve[0] == sect2.PointLast())
        {
            zeroOneSet |= KZeroS1Set | KOneS2Set;
            intersections.Insert(0, 1, sect1._curve[0]);
        }

        if (sect1.PointLast() == sect2._curve[0])
        {
            zeroOneSet |= KOneS1Set | KZeroS2Set;
            intersections.Insert(1, 0, sect1.PointLast());
        }

        if (sect1.PointLast() == sect2.PointLast())
        {
            zeroOneSet |= KOneS1Set | KOneS2Set;
            intersections.Insert(1, 1, sect1.PointLast());
        }

        // check for zero
        if ((zeroOneSet & (KZeroS1Set | KZeroS2Set)) == 0
            && sect1._curve[0].ApproximatelyEqual(sect2._curve[0]))
        {
            zeroOneSet |= KZeroS1Set | KZeroS2Set;
            intersections.InsertNear(0, 0, sect1._curve[0], sect2._curve[0]);
        }

        if ((zeroOneSet & (KZeroS1Set | KOneS2Set)) == 0
            && sect1._curve[0].ApproximatelyEqual(sect2.PointLast()))
        {
            zeroOneSet |= KZeroS1Set | KOneS2Set;
            intersections.InsertNear(0, 1, sect1._curve[0], sect2.PointLast());
        }

        // check for one
        if ((zeroOneSet & (KOneS1Set | KZeroS2Set)) == 0
            && sect1.PointLast().ApproximatelyEqual(sect2._curve[0]))
        {
            zeroOneSet |= KOneS1Set | KZeroS2Set;
            intersections.InsertNear(1, 0, sect1.PointLast(), sect2._curve[0]);
        }

        if ((zeroOneSet & (KOneS1Set | KOneS2Set)) == 0
            && sect1.PointLast().ApproximatelyEqual(sect2.PointLast()))
        {
            zeroOneSet |= KOneS1Set | KOneS2Set;
            intersections.InsertNear(1, 1, sect1.PointLast(), sect2.PointLast());
        }

        return zeroOneSet;
    }

    // returns true if the rect is too small to consider
    public static void BinarySearch(SkTSect sect1, SkTSect sect2, SkIntersections intersections)
    {
        intersections.Reset();
        intersections.SetMax(sect1._curve.MaxIntersections() + 4); // give extra for slop
        SkTSpan span1 = sect1._head!;
        SkTSpan span2 = sect2._head!;
        int oppSect = 0;
        int sect = sect1.Intersects(span1, sect2, span2, ref oppSect);
        if (sect == 0)
        {
            return;
        }

        if (sect == 2 && oppSect == 2)
        {
            _ = EndsEqual(sect1, sect2, intersections);
            return;
        }

        span1.AddBounded(span2);
        span2.AddBounded(span1);
        const int KMaxCoinLoopCount = 8;
        int coinLoopCount = KMaxCoinLoopCount;
        double start1s = 0; // SK_INIT_TO_AVOID_WARNING
        double start1e = 0; // SK_INIT_TO_AVOID_WARNING
        do
        {
            // find the largest bounds
            SkTSpan? largest1 = sect1.BoundsMax();
            if (largest1 == null)
            {
                if (sect1._hung)
                {
                    return;
                }

                break;
            }

            SkTSpan? largest2 = sect2.BoundsMax();
            // split it
            if (largest2 == null
                || (largest1 != null
                    && (largest1._boundsMax > largest2._boundsMax
                        || (!largest1._collapsed && largest2._collapsed))))
            {
                if (sect2._hung)
                {
                    return;
                }

                if (largest1._collapsed)
                {
                    break;
                }

                sect1.ResetRemovedEnds();
                sect2.ResetRemovedEnds();
                // trim parts that don't intersect the opposite
                SkTSpan half1 = sect1.AddOne();
                if (!half1.Split(largest1))
                {
                    break;
                }

                if (!sect1.Trim(largest1, sect2))
                {
                    return;
                }

                if (!sect1.Trim(half1, sect2))
                {
                    return;
                }
            }
            else
            {
                if (largest2._collapsed)
                {
                    break;
                }

                sect1.ResetRemovedEnds();
                sect2.ResetRemovedEnds();
                // trim parts that don't intersect the opposite
                SkTSpan half2 = sect2.AddOne();
                if (!half2.Split(largest2))
                {
                    break;
                }

                if (!sect2.Trim(largest2, sect1))
                {
                    return;
                }

                if (!sect2.Trim(half2, sect1))
                {
                    return;
                }
            }

            // if there are 9 or more continuous spans on both sects, suspect coincidence
            if (sect1._activeCount >= SkPathOpsTSectStatics.CoincidentSpanCount
                && sect2._activeCount >= SkPathOpsTSectStatics.CoincidentSpanCount)
            {
                if (coinLoopCount == KMaxCoinLoopCount)
                {
                    start1s = sect1._head!._startT;
                    start1e = sect1.Tail()!._endT;
                }

                if (!sect1.CoincidentCheck(sect2))
                {
                    return;
                }

                if (--coinLoopCount == 0 && sect1._head != null && sect2._head != null)
                {
                    /* All known working cases resolve in two tries. Sadly, cubicConicTests[0]
                       gets stuck in a loop. It adds an extension to allow a coincident end
                       perpendicular to track its intersection in the opposite curve. However,
                       the bounding box of the extension does not intersect the original curve,
                       so the extension is discarded, only to be added again the next time around. */
                    sect1.CoincidentForce(sect2, start1s, start1e);
                }
            }

            if (sect1._activeCount >= SkPathOpsTSectStatics.CoincidentSpanCount
                && sect2._activeCount >= SkPathOpsTSectStatics.CoincidentSpanCount)
            {
                if (sect1._head == null)
                {
                    return;
                }

                sect1.ComputePerpendiculars(sect2, sect1._head, sect1.Tail());
                if (sect2._head == null)
                {
                    return;
                }

                sect2.ComputePerpendiculars(sect1, sect2._head, sect2.Tail());
                if (!sect1.RemoveByPerpendicular(sect2))
                {
                    return;
                }

                if (sect1.Collapsed() > sect1._curve.MaxIntersections())
                {
                    break;
                }
            }

            if (sect1._head == null || sect2._head == null)
            {
                break;
            }
        }
        while (true);

        SkTSpan? coincident = sect1._coincident;
        if (coincident != null)
        {
            // if there is more than one coincident span, check loosely to see if they should be joined
            if (coincident._next != null)
            {
                sect1.MergeCoincidence(sect2);
                coincident = sect1._coincident;
            }

            do
            {
                if (coincident == null)
                {
                    return;
                }

                if (!coincident._coinStart.IsMatch())
                {
                    continue;
                }

                if (!coincident._coinEnd.IsMatch())
                {
                    continue;
                }

                double perpT = coincident._coinStart.PerpT();
                if (perpT < 0)
                {
                    return;
                }

                int index = intersections.InsertCoincident(coincident._startT, perpT, coincident.PointFirst());
                if ((intersections.InsertCoincident(
                        coincident._endT,
                        coincident._coinEnd.PerpT(),
                        coincident.PointLast()) < 0) && index >= 0)
                {
                    intersections.ClearCoincidence(index);
                }
            }
            while ((coincident = coincident._next) != null);
        }

        int zeroOneSet = EndsEqual(sect1, sect2, intersections);
        // if the final iteration contains an end (0 or 1),
        if (sect1._removedStartT && (zeroOneSet & KZeroS1Set) == 0)
        {
            SkTCoincident perp = new SkTCoincident(); // intersect perpendicular with opposite curve
            perp.SetPerp(sect1._curve, 0, sect1._curve[0], sect2._curve);
            if (perp.IsMatch())
            {
                intersections.Insert(0, perp.PerpT(), perp.PerpPt());
            }
        }

        if (sect1._removedEndT && (zeroOneSet & KOneS1Set) == 0)
        {
            SkTCoincident perp = new SkTCoincident();
            perp.SetPerp(sect1._curve, 1, sect1.PointLast(), sect2._curve);
            if (perp.IsMatch())
            {
                intersections.Insert(1, perp.PerpT(), perp.PerpPt());
            }
        }

        if (sect2._removedStartT && (zeroOneSet & KZeroS2Set) == 0)
        {
            SkTCoincident perp = new SkTCoincident();
            perp.SetPerp(sect2._curve, 0, sect2._curve[0], sect1._curve);
            if (perp.IsMatch())
            {
                intersections.Insert(perp.PerpT(), 0, perp.PerpPt());
            }
        }

        if (sect2._removedEndT && (zeroOneSet & KOneS2Set) == 0)
        {
            SkTCoincident perp = new SkTCoincident();
            perp.SetPerp(sect2._curve, 1, sect2.PointLast(), sect1._curve);
            if (perp.IsMatch())
            {
                intersections.Insert(perp.PerpT(), 1, perp.PerpPt());
            }
        }

        if (sect1._head == null || sect2._head == null)
        {
            return;
        }

        sect1.RecoverCollapsed();
        sect2.RecoverCollapsed();
        SkTSpan? result1 = sect1._head;
        // check heads and tails for zero and ones and insert them if we haven't already done so
        SkTSpan head1 = result1!;
        if ((zeroOneSet & KZeroS1Set) == 0 && SkPathOps.ApproximatelyLessThanZero(head1._startT))
        {
            ref readonly SkDPoint start1 = ref sect1._curve[0];
            if (head1.IsBounded())
            {
                double t = head1.ClosestBoundedT(start1);
                if (sect2._curve.PtAtT(t).ApproximatelyEqual(start1))
                {
                    intersections.Insert(0, t, start1);
                }
            }
        }

        SkTSpan head2 = sect2._head!;
        if ((zeroOneSet & KZeroS2Set) == 0 && SkPathOps.ApproximatelyLessThanZero(head2._startT))
        {
            ref readonly SkDPoint start2 = ref sect2._curve[0];
            if (head2.IsBounded())
            {
                double t = head2.ClosestBoundedT(start2);
                if (sect1._curve.PtAtT(t).ApproximatelyEqual(start2))
                {
                    intersections.Insert(t, 0, start2);
                }
            }
        }

        if ((zeroOneSet & KOneS1Set) == 0)
        {
            SkTSpan? tail1 = sect1.Tail();
            if (tail1 == null)
            {
                return;
            }

            if (SkPathOps.ApproximatelyGreaterThanOne(tail1._endT))
            {
                ref readonly SkDPoint end1 = ref sect1.PointLast();
                if (tail1.IsBounded())
                {
                    double t = tail1.ClosestBoundedT(end1);
                    if (sect2._curve.PtAtT(t).ApproximatelyEqual(end1))
                    {
                        intersections.Insert(1, t, end1);
                    }
                }
            }
        }

        if ((zeroOneSet & KOneS2Set) == 0)
        {
            SkTSpan? tail2 = sect2.Tail();
            if (tail2 == null)
            {
                return;
            }

            if (SkPathOps.ApproximatelyGreaterThanOne(tail2._endT))
            {
                ref readonly SkDPoint end2 = ref sect2.PointLast();
                if (tail2.IsBounded())
                {
                    double t = tail2.ClosestBoundedT(end2);
                    if (sect1._curve.PtAtT(t).ApproximatelyEqual(end2))
                    {
                        intersections.Insert(t, 1, end2);
                    }
                }
            }
        }

        SkClosestSect closest = new SkClosestSect();
        do
        {
            while (result1 != null && result1._coinStart.IsMatch() && result1._coinEnd.IsMatch())
            {
                result1 = result1._next;
            }

            if (result1 == null)
            {
                break;
            }

            SkTSpan? result2 = sect2._head;
            while (result2 != null)
            {
                closest.Find(result1, result2);
                result2 = result2._next;
            }
        }
        while ((result1 = result1._next) != null);

        closest.Finish(intersections);
        // if there is more than one intersection and it isn't already coincident, check
        int last = intersections.Used() - 1;
        for (int index = 0; index < last;)
        {
            if (intersections.IsCoincident(index) && intersections.IsCoincident(index + 1))
            {
                ++index;
                continue;
            }

            double midT = (intersections[0][index] + intersections[0][index + 1]) / 2;
            SkDPoint midPt = sect1._curve.PtAtT(midT);
            // intersect perpendicular with opposite curve
            SkTCoincident perp = new SkTCoincident();
            perp.SetPerp(sect1._curve, midT, midPt, sect2._curve);
            if (!perp.IsMatch())
            {
                ++index;
                continue;
            }

            if (intersections.IsCoincident(index))
            {
                intersections.RemoveOne(index);
                --last;
            }
            else if (intersections.IsCoincident(index + 1))
            {
                intersections.RemoveOne(index + 1);
                --last;
            }
            else
            {
                intersections.SetCoincident(index++);
            }

            intersections.SetCoincident(index);
        }
    }
}

file sealed class SkClosestRecord
{
    public SkTSpan? C1Span;
    public SkTSpan? C2Span;
    public double C1StartT;
    public double C1EndT;
    public double C2StartT;
    public double C2EndT;
    public double Closest;
    public int C1Index;
    public int C2Index;

    // Skia's operator<.
    public static bool LessThan(SkClosestRecord lh, SkClosestRecord rh)
    {
        return lh.Closest < rh.Closest;
    }

    public void AddIntersection(SkIntersections intersections)
    {
        double r1t = C1Index != 0 ? C1Span!.EndT() : C1Span!.StartT();
        double r2t = C2Index != 0 ? C2Span!.EndT() : C2Span!.StartT();
        intersections.Insert(r1t, r2t, C1Span.Part()[C1Index]);
    }

    public void FindEnd(SkTSpan span1, SkTSpan span2, int c1Index, int c2Index)
    {
        SkTCurve c1 = span1.Part();
        SkTCurve c2 = span2.Part();
        if (!c1[c1Index].ApproximatelyEqual(c2[c2Index]))
        {
            return;
        }

        double dist = c1[c1Index].DistanceSquared(c2[c2Index]);
        if (Closest < dist)
        {
            return;
        }

        C1Span = span1;
        C2Span = span2;
        C1StartT = span1.StartT();
        C1EndT = span1.EndT();
        C2StartT = span2.StartT();
        C2EndT = span2.EndT();
        C1Index = c1Index;
        C2Index = c2Index;
        Closest = dist;
    }

    public bool MatesWith(SkClosestRecord mate)
    {
        return C1Span == mate.C1Span || C1Span!.EndT() == mate.C1Span!.StartT()
            || C1Span.StartT() == mate.C1Span.EndT()
            || C2Span == mate.C2Span
            || C2Span!.EndT() == mate.C2Span!.StartT()
            || C2Span.StartT() == mate.C2Span.EndT();
    }

    public void Merge(SkClosestRecord mate)
    {
        C1Span = mate.C1Span;
        C2Span = mate.C2Span;
        Closest = mate.Closest;
        C1Index = mate.C1Index;
        C2Index = mate.C2Index;
    }

    public void Reset()
    {
        Closest = SkPathOps.SkScalarMax; // FLT_MAX
    }

    public void Update(SkClosestRecord mate)
    {
        C1StartT = SkPathOps.StdMin(C1StartT, mate.C1StartT);
        C1EndT = SkPathOps.StdMax(C1EndT, mate.C1EndT);
        C2StartT = SkPathOps.StdMin(C2StartT, mate.C2StartT);
        C2EndT = SkPathOps.StdMax(C2EndT, mate.C2EndT);
    }
}

file sealed class SkClosestSect
{
    // this is oversized so that an extra records can merge into final one
    private readonly List<SkClosestRecord> _closest = new List<SkClosestRecord>(SkDCubic.KMaxIntersections * 2);
    private int _used;

    public SkClosestSect()
    {
        _used = 0;
        SkClosestRecord record = new SkClosestRecord();
        _closest.Add(record);
        record.Reset();
    }

    public bool Find(SkTSpan span1, SkTSpan span2)
    {
        SkClosestRecord record = _closest[_used];
        record.FindEnd(span1, span2, 0, 0);
        record.FindEnd(span1, span2, 0, span2.Part().PointLast());
        record.FindEnd(span1, span2, span1.Part().PointLast(), 0);
        record.FindEnd(span1, span2, span1.Part().PointLast(), span2.Part().PointLast());
        if (record.Closest == SkPathOps.SkScalarMax)
        {
            return false;
        }

        for (int index = 0; index < _used; ++index)
        {
            SkClosestRecord test = _closest[index];
            if (test.MatesWith(record))
            {
                if (test.Closest > record.Closest)
                {
                    test.Merge(record);
                }

                test.Update(record);
                record.Reset();
                return false;
            }
        }

        ++_used;
        SkClosestRecord next = new SkClosestRecord();
        _closest.Add(next);
        next.Reset();
        return true;
    }

    public void Finish(SkIntersections intersections)
    {
        SkClosestRecord[] closestPtrs = new SkClosestRecord[_used];
        for (int index = 0; index < _used; ++index)
        {
            closestPtrs[index] = _closest[index];
        }

        SkPathOps.SkTQSort<SkClosestRecord>(closestPtrs, SkClosestRecord.LessThan);
        for (int index = 0; index < _used; ++index)
        {
            SkClosestRecord test = closestPtrs[index];
            test.AddIntersection(intersections);
        }
    }
}

file static class SkPathOpsTSectStatics
{
    public const int CoincidentSpanCount = 9; // COINCIDENT_SPAN_COUNT

    public static bool IsParallel(in SkDLine thisLine, SkTCurve opp)
    {
        if (!opp.IsConic())
        {
            return false; // FIXME : breaks a lot of stuff now
        }

        int finds = 0;
        SkDLine thisPerp = default;
        thisPerp.Pts[0].X = thisLine.Pts[1].X + (thisLine.Pts[1].Y - thisLine.Pts[0].Y);
        thisPerp.Pts[0].Y = thisLine.Pts[1].Y + (thisLine.Pts[0].X - thisLine.Pts[1].X);
        thisPerp.Pts[1] = thisLine.Pts[1];
        SkIntersections perpRayI = new SkIntersections();
        perpRayI.IntersectRay(opp, thisPerp);
        for (int pIndex = 0; pIndex < perpRayI.Used(); ++pIndex)
        {
            finds += perpRayI.Pt(pIndex).ApproximatelyEqual(thisPerp.Pts[1]) ? 1 : 0;
        }

        thisPerp.Pts[1].X = thisLine.Pts[0].X + (thisLine.Pts[1].Y - thisLine.Pts[0].Y);
        thisPerp.Pts[1].Y = thisLine.Pts[0].Y + (thisLine.Pts[0].X - thisLine.Pts[1].X);
        thisPerp.Pts[0] = thisLine.Pts[0];
        perpRayI.IntersectRay(opp, thisPerp);
        for (int pIndex = 0; pIndex < perpRayI.Used(); ++pIndex)
        {
            finds += perpRayI.Pt(pIndex).ApproximatelyEqual(thisPerp.Pts[0]) ? 1 : 0;
        }

        return finds >= 2;
    }
}

internal sealed partial class SkIntersections
{
    public int Intersect(in SkDQuad q1, in SkDQuad q2)
    {
        SkTQuad quad1 = new SkTQuad(q1);
        SkTQuad quad2 = new SkTQuad(q2);
        SkTSect sect1 = new SkTSect(quad1);
        SkTSect sect2 = new SkTSect(quad2);
        SkTSect.BinarySearch(sect1, sect2, this);
        return Used();
    }

    public int Intersect(in SkDConic c, in SkDQuad q)
    {
        SkTConic conic = new SkTConic(c);
        SkTQuad quad = new SkTQuad(q);
        SkTSect sect1 = new SkTSect(conic);
        SkTSect sect2 = new SkTSect(quad);
        SkTSect.BinarySearch(sect1, sect2, this);
        return Used();
    }

    public int Intersect(in SkDConic c1, in SkDConic c2)
    {
        SkTConic conic1 = new SkTConic(c1);
        SkTConic conic2 = new SkTConic(c2);
        SkTSect sect1 = new SkTSect(conic1);
        SkTSect sect2 = new SkTSect(conic2);
        SkTSect.BinarySearch(sect1, sect2, this);
        return Used();
    }

    public int Intersect(in SkDCubic c, in SkDQuad q)
    {
        SkTCubic cubic = new SkTCubic(c);
        SkTQuad quad = new SkTQuad(q);
        SkTSect sect1 = new SkTSect(cubic);
        SkTSect sect2 = new SkTSect(quad);
        SkTSect.BinarySearch(sect1, sect2, this);
        return Used();
    }

    public int Intersect(in SkDCubic cu, in SkDConic co)
    {
        SkTCubic cubic = new SkTCubic(cu);
        SkTConic conic = new SkTConic(co);
        SkTSect sect1 = new SkTSect(cubic);
        SkTSect sect2 = new SkTSect(conic);
        SkTSect.BinarySearch(sect1, sect2, this);
        return Used();
    }

    public int Intersect(in SkDCubic c1, in SkDCubic c2)
    {
        SkTCubic cubic1 = new SkTCubic(c1);
        SkTCubic cubic2 = new SkTCubic(c2);
        SkTSect sect1 = new SkTSect(cubic1);
        SkTSect sect2 = new SkTSect(cubic2);
        SkTSect.BinarySearch(sect1, sect2, this);
        return Used();
    }
}
