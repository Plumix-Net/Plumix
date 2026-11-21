namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkOpSegment.h and SkOpSegment.cpp at chrome/m119 (the Skia
// inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types
// are kept. SkOpSegment is a `partial` class: rayCheck, windingSpanAtT and findSortableTop are defined in
// SkPathOpsWinding.cpp, whose part is in SkPathOpsWinding.cs. The segment owns its head SkOpSpan and tail
// SkOpSpanBase (Skia embeds them by value; head() and tail() return their addresses). fPts points into the edge
// builder's point array in Skia; nothing writes that array once segments exist, so the segment keeps its own copy
// of its verb's points. Optional pointer outputs that callers pass as nullptr (markAngle's result,
// markAndChaseWinding's lastPtr, markAndChaseDone's found, nextChase's minPtr/last) are overloads without the
// `ref` parameter (convention 5). SkTDArray<SkOpSpanBase*>* chase is a List<SkOpSpanBase>. addMissing is declared
// but never defined in Skia, so it is not ported. debug*, dump* and fID are omitted.

internal sealed partial class SkOpSegment
{
    private readonly SkOpSpan _head = new SkOpSpan(); // the head span always has its t set to zero
    private readonly SkOpSpanBase _tail = new SkOpSpanBase(); // the tail span always has its t set to one
    private SkOpContour _contour = null!;
    private SkOpSegment? _next; // forward-only linked list used by contour to walk the segments
    private SkOpSegment? _prev;
    private PathPoint[] _pts = null!; // pointer into array of points owned by edge builder that may be tweaked
    private SkPathOpsBounds _bounds; // tight bounds
    private float _weight;
    private int _count; // number of spans (one for a non-intersecting segment)
    private int _doneCount; // number of processed spans (zero initially)
    private PathVerb _verb;
    private bool _visited; // used by missing coincidence check

    // Skia's operator<.
    public static bool LessThan(SkOpSegment lh, SkOpSegment rh)
    {
        return lh._bounds.Top < rh._bounds.Top;
    }

    public SkOpSegment AddConic(ReadOnlySpan<PathPoint> pts, float weight, SkOpContour parent)
    {
        Init(pts, weight, parent, PathVerb.Conic);
        SkDCurve curve = default;
        curve.Conic.Set(pts, weight);
        curve.SetConicBounds(pts, weight, 0, 1, out _bounds);
        return this;
    }

    public SkOpSegment AddCubic(ReadOnlySpan<PathPoint> pts, SkOpContour parent)
    {
        Init(pts, 1, parent, PathVerb.Cubic);
        SkDCurve curve = default;
        curve.Cubic.Set(pts);
        curve.SetCubicBounds(pts, 1, 0, 1, out _bounds);
        return this;
    }

    public SkOpAngle AddEndSpan()
    {
        SkOpAngle angle = new SkOpAngle();
        angle.Set(_tail, _tail.Prev()!);
        _tail.SetFromAngle(angle);
        return angle;
    }

    public SkOpSegment AddLine(ReadOnlySpan<PathPoint> pts, SkOpContour parent)
    {
        Init(pts, 1, parent, PathVerb.Line);
        _bounds.SetBounds(pts, 2);
        return this;
    }

    public SkOpAngle AddStartSpan()
    {
        SkOpAngle angle = new SkOpAngle();
        angle.Set(_head, _head.Next());
        _head.SetToAngle(angle);
        return angle;
    }

    public SkOpSegment AddQuad(ReadOnlySpan<PathPoint> pts, SkOpContour parent)
    {
        Init(pts, 1, parent, PathVerb.Quad);
        SkDCurve curve = default;
        curve.Quad.Set(pts);
        curve.SetQuadBounds(pts, 1, 0, 1, out _bounds);
        return this;
    }

    public ref readonly SkPathOpsBounds Bounds()
    {
        return ref _bounds;
    }

    public void BumpCount()
    {
        ++_count;
    }

    public SkOpContour Contour()
    {
        return _contour;
    }

    public int Count()
    {
        return _count;
    }

    public bool Done()
    {
        return _doneCount == _count;
    }

    public bool Done(SkOpAngle angle)
    {
        return angle.Start().Starter(angle.End()).Done();
    }

    public SkDPoint DPtAtT(double mid)
    {
        return SkPathOps.CurveDPointAtT[(int)_verb]!(_pts, _weight, mid);
    }

    public SkDVector DSlopeAtT(double mid)
    {
        return SkPathOps.CurveDSlopeAtT[(int)_verb]!(_pts, _weight, mid);
    }

    public SkOpSpan Head()
    {
        return _head;
    }

    public SkOpSpan Insert(SkOpSpan prev)
    {
        SkOpGlobalState globalState = GlobalState();
        globalState.SetAllocatedOpSpan();
        SkOpSpan result = new SkOpSpan();
        SkOpSpanBase next = prev.Next();
        result.SetPrev(prev);
        prev.SetNext(result);
        result.SetNext(next);
        if (next != null)
        {
            next.SetPrev(result);
        }

        return result;
    }

    public bool IsHorizontal()
    {
        return _bounds.Top == _bounds.Bottom;
    }

    public SkOpSegment? IsSimple(ref SkOpSpanBase end, ref int step)
    {
        return NextChase(ref end, ref step);
    }

    public bool IsVertical()
    {
        return _bounds.Left == _bounds.Right;
    }

    public bool IsVertical(SkOpSpanBase start, SkOpSpanBase end)
    {
        return SkPathOps.CurveIsVertical[(int)_verb]!(_pts, _weight, start.T(), end.T());
    }

    public void JoinEnds(SkOpSegment start)
    {
        _tail.PtT().AddOpp(start._head.PtT(), start._head.PtT());
    }

    public PathPoint LastPt()
    {
        return _pts[SkPathOps.SkPathOpsVerbToPoints(_verb)];
    }

    public SkOpSegment? Next()
    {
        return _next;
    }

    public static int OppSign(SkOpSpanBase start, SkOpSpanBase end)
    {
        int result = start.T() < end.T() ? -start.UpCast().OppValue() : end.UpCast().OppValue();
        return result;
    }

    public SkOpSegment? Prev()
    {
        return _prev;
    }

    public PathPoint PtAtT(double mid)
    {
        return SkPathOps.CurvePointAtT[(int)_verb]!(_pts, _weight, mid);
    }

    public ReadOnlySpan<PathPoint> Pts()
    {
        return _pts;
    }

    public bool PtsDisjoint(SkOpPtT span, SkOpPtT test)
    {
        return PtsDisjoint(span.T, span.Pt, test.T, test.Pt);
    }

    public bool PtsDisjoint(SkOpPtT span, double t, PathPoint pt)
    {
        return PtsDisjoint(span.T, span.Pt, t, pt);
    }

    public void ResetVisited()
    {
        _visited = false;
    }

    public void SetContour(SkOpContour contour)
    {
        _contour = contour;
    }

    public void SetNext(SkOpSegment? next)
    {
        _next = next;
    }

    public void SetPrev(SkOpSegment? prev)
    {
        _prev = prev;
    }

    public void SetUpWinding(SkOpSpanBase start, SkOpSpanBase end, out int maxWinding, ref int sumWinding)
    {
        int deltaSum = SpanSign(start, end);
        maxWinding = sumWinding;
        if (sumWinding == SkPathOps.SkMinS32)
        {
            return;
        }

        sumWinding -= deltaSum;
    }

    public static int SpanSign(SkOpSpanBase start, SkOpSpanBase end)
    {
        int result = start.T() < end.T() ? -start.UpCast().WindValue() : end.UpCast().WindValue();
        return result;
    }

    public SkOpAngle? SpanToAngle(SkOpSpanBase start, SkOpSpanBase end)
    {
        return start.T() < end.T() ? start.UpCast().ToAngle() : start.FromAngle();
    }

    public SkOpSpanBase Tail()
    {
        return _tail;
    }

    public PathVerb Verb()
    {
        return _verb;
    }

    // look for two different spans that point to the same opposite segment
    public bool Visited()
    {
        if (!_visited)
        {
            _visited = true;
            return false;
        }

        return true;
    }

    public float Weight()
    {
        return _weight;
    }

    // SkOpSegment.cpp

    /*
    After computing raw intersections, post process all segments to:
    - find small collections of points that can be collapsed to a single point
    - find missing intersections to resolve differences caused by different algorithms

    Consider segments containing tiny or small intervals. Consider coincident segments
    because coincidence finds intersections through distance measurement that non-coincident
    intersection tests cannot.
     */

    public SkOpAngle? ActiveAngle(
        SkOpSpanBase start,
        ref SkOpSpanBase? startPtr,
        ref SkOpSpanBase? endPtr,
        ref bool done)
    {
        SkOpAngle? result = ActiveAngleInner(start, ref startPtr, ref endPtr, ref done);
        if (result != null)
        {
            return result;
        }

        result = ActiveAngleOther(start, ref startPtr, ref endPtr, ref done);
        if (result != null)
        {
            return result;
        }

        return null;
    }

    public SkOpAngle? ActiveAngleInner(
        SkOpSpanBase start,
        ref SkOpSpanBase? startPtr,
        ref SkOpSpanBase? endPtr,
        ref bool done)
    {
        SkOpSpan? upSpan = start.UpCastable();
        if (upSpan != null)
        {
            if (upSpan.WindValue() != 0 || upSpan.OppValue() != 0)
            {
                SkOpSpanBase next = upSpan.Next();
                if (endPtr == null)
                {
                    startPtr = start;
                    endPtr = next;
                }

                if (!upSpan.Done())
                {
                    if (upSpan.WindSum() != SkPathOps.SkMinS32)
                    {
                        return SpanToAngle(start, next);
                    }

                    done = false;
                }
            }
        }

        SkOpSpan? downSpan = start.Prev();
        // edge leading into junction
        if (downSpan != null)
        {
            if (downSpan.WindValue() != 0 || downSpan.OppValue() != 0)
            {
                if (endPtr == null)
                {
                    startPtr = start;
                    endPtr = downSpan;
                }

                if (!downSpan.Done())
                {
                    if (downSpan.WindSum() != SkPathOps.SkMinS32)
                    {
                        return SpanToAngle(start, downSpan);
                    }

                    done = false;
                }
            }
        }

        return null;
    }

    public SkOpAngle? ActiveAngleOther(
        SkOpSpanBase start,
        ref SkOpSpanBase? startPtr,
        ref SkOpSpanBase? endPtr,
        ref bool done)
    {
        SkOpPtT oPtT = start.PtT().Next();
        SkOpSegment other = oPtT.Segment();
        SkOpSpanBase oSpan = oPtT.Span();
        return other.ActiveAngleInner(oSpan, ref startPtr, ref endPtr, ref done);
    }

    public bool ActiveOp(SkOpSpanBase start, SkOpSpanBase end, int xorMiMask, int xorSuMask, SkPathOp op)
    {
        int sumMiWinding = UpdateWinding(end, start);
        int sumSuWinding = UpdateOppWinding(end, start);
        if (Operand())
        {
            (sumMiWinding, sumSuWinding) = (sumSuWinding, sumMiWinding);
        }

        return ActiveOp(xorMiMask, xorSuMask, start, end, op, ref sumMiWinding, ref sumSuWinding);
    }

    public bool ActiveOp(
        int xorMiMask,
        int xorSuMask,
        SkOpSpanBase start,
        SkOpSpanBase end,
        SkPathOp op,
        ref int sumMiWinding,
        ref int sumSuWinding)
    {
        SetUpWindings(
            start,
            end,
            ref sumMiWinding,
            ref sumSuWinding,
            out int maxWinding,
            out int sumWinding,
            out int oppMaxWinding,
            out int oppSumWinding);
        bool miFrom;
        bool miTo;
        bool suFrom;
        bool suTo;
        if (Operand())
        {
            miFrom = (oppMaxWinding & xorMiMask) != 0;
            miTo = (oppSumWinding & xorMiMask) != 0;
            suFrom = (maxWinding & xorSuMask) != 0;
            suTo = (sumWinding & xorSuMask) != 0;
        }
        else
        {
            miFrom = (maxWinding & xorMiMask) != 0;
            miTo = (sumWinding & xorMiMask) != 0;
            suFrom = (oppMaxWinding & xorSuMask) != 0;
            suTo = (oppSumWinding & xorSuMask) != 0;
        }

        bool result = SkOpSegmentStatics.GActiveEdge[
            (int)op,
            miFrom ? 1 : 0,
            miTo ? 1 : 0,
            suFrom ? 1 : 0,
            suTo ? 1 : 0];
        return result;
    }

    public bool ActiveWinding(SkOpSpanBase start, SkOpSpanBase end)
    {
        int sumWinding = UpdateWinding(end, start);
        return ActiveWinding(start, end, ref sumWinding);
    }

    public bool ActiveWinding(SkOpSpanBase start, SkOpSpanBase end, ref int sumWinding)
    {
        SetUpWinding(start, end, out int maxWinding, ref sumWinding);
        bool from = maxWinding != 0;
        bool to = sumWinding != 0;
        bool result = SkOpSegmentStatics.GUnaryActiveEdge[from ? 1 : 0, to ? 1 : 0];
        return result;
    }

    public bool AddCurveTo(SkOpSpanBase start, SkOpSpanBase end, SkPathWriter path)
    {
        SkOpSpan spanStart = start.Starter(end);
        if (spanStart.AlreadyAdded())
        {
            return false;
        }

        spanStart.MarkAdded();
        SkDCurveSweep curvePart = default;
        start.Segment().SubDivide(start, end, ref curvePart.Curve);
        curvePart.SetCurveHullSweep(_verb);
        PathVerb verb = curvePart.IsCurve() ? _verb : PathVerb.Line;
        path.DeferredMove(start.PtT());
        switch (verb)
        {
            case PathVerb.Line:
                if (!path.DeferredLine(end.PtT()))
                {
                    return false;
                }

                break;
            case PathVerb.Quad:
                path.QuadTo(curvePart.Curve.Quad[1].AsSkPoint(), end.PtT());
                break;
            case PathVerb.Conic:
                path.ConicTo(curvePart.Curve.Conic[1].AsSkPoint(), end.PtT(), curvePart.Curve.Conic.Weight);
                break;
            case PathVerb.Cubic:
                path.CubicTo(
                    curvePart.Curve.Cubic[1].AsSkPoint(),
                    curvePart.Curve.Cubic[2].AsSkPoint(),
                    end.PtT());
                break;
            default:
                break;
        }

        return true;
    }

    public SkOpPtT? Existing(double t, SkOpSegment? opp)
    {
        SkOpSpanBase test = _head;
        SkOpPtT testPtT;
        PathPoint pt = PtAtT(t);
        do
        {
            testPtT = test.PtT();
            if (testPtT.T == t)
            {
                break;
            }

            if (!Match(testPtT, this, t, pt))
            {
                if (t < testPtT.T)
                {
                    return null;
                }

                continue;
            }

            if (opp == null)
            {
                return testPtT;
            }

            SkOpPtT loop = testPtT.Next();
            while (loop != testPtT)
            {
                if (loop.Segment() == this && loop.T == t && loop.Pt == pt)
                {
                    goto foundMatch;
                }

                loop = loop.Next();
            }

            return null;
        }
        while ((test = test.UpCast().Next()) != null);

    foundMatch:
        return opp != null && test!.Contains(opp) == null ? null : testPtT;
    }

    // break the span so that the coincident part does not change the angle of the remainder
    public bool AddExpanded(double newT, SkOpSpanBase test, ref bool startOver)
    {
        if (Contains(newT))
        {
            return true;
        }

        GlobalState().ResetAllocatedOpSpan();
        if (!SkPathOps.Between(0, newT, 1))
        {
            return false;
        }

        SkOpPtT? newPtT = AddT(newT);
        startOver |= GlobalState().AllocatedOpSpan();
        if (newPtT == null)
        {
            return false;
        }

        newPtT.Pt = PtAtT(newT);
        SkOpPtT? oppPrev = test.PtT().OppPrev(newPtT);
        if (oppPrev != null)
        {
            // const cast away to change linked list; pt/t values stays unchanged
            SkOpSpanBase writableTest = test;
            writableTest.MergeMatches(newPtT.Span());
            writableTest.PtT().AddOpp(newPtT, oppPrev);
            writableTest.CheckForCollapsedCoincidence();
        }

        return true;
    }

    // Please keep this in sync with debugAddT()
    public SkOpPtT? AddT(double t, PathPoint pt)
    {
        SkOpSpanBase spanBase = _head;
        do
        {
            SkOpPtT result = spanBase.PtT();
            if (t == result.T || (!SkPathOps.ZeroOrOne(t) && Match(result, this, t, pt)))
            {
                spanBase.BumpSpanAdds();
                return result;
            }

            if (t < result.T)
            {
                SkOpSpan? prev = result.Span().Prev();
                if (prev == null)
                {
                    return null;
                }

                // marks in global state that new op span has been allocated
                SkOpSpan span = Insert(prev);
                span.Init(this, prev, t, pt);
                span.BumpSpanAdds();
                return span.PtT();
            }

            if (spanBase == _tail)
            {
                return null;
            }
        }
        while ((spanBase = spanBase.UpCast().Next()) != null);

        return null; // we never get here, but need this to satisfy compiler
    }

    public SkOpPtT? AddT(double t)
    {
        return AddT(t, PtAtT(t));
    }

    public void CalcAngles()
    {
        bool activePrior = !_head.IsCanceled();
        if (activePrior && !_head.Simple())
        {
            AddStartSpan();
        }

        SkOpSpan prior = _head;
        SkOpSpanBase spanBase = _head.Next();
        while (spanBase != _tail)
        {
            if (activePrior)
            {
                SkOpAngle priorAngle = new SkOpAngle();
                priorAngle.Set(spanBase, prior);
                spanBase.SetFromAngle(priorAngle);
            }

            SkOpSpan span = spanBase.UpCast();
            bool active = !span.IsCanceled();
            SkOpSpanBase next = span.Next();
            if (active)
            {
                SkOpAngle angle = new SkOpAngle();
                angle.Set(span, next);
                span.SetToAngle(angle);
            }

            activePrior = active;
            prior = span;
            spanBase = next;
        }

        if (activePrior && !_tail.Simple())
        {
            AddEndSpan();
        }
    }

    // Please keep this in sync with debugClearAll()
    public void ClearAll()
    {
        SkOpSpan? span = _head;
        do
        {
            ClearOne(span);
        }
        while ((span = span.Next().UpCastable()) != null);

        GlobalState().Coincidence()!.Release(this);
    }

    // Please keep this in sync with debugClearOne()
    public void ClearOne(SkOpSpan span)
    {
        span.SetWindValue(0);
        span.SetOppValue(0);
        MarkDone(span);
    }

    public SkOpSpanBaseCollapsed Collapsed(double s, double e)
    {
        SkOpSpanBase span = _head;
        do
        {
            SkOpSpanBaseCollapsed result = span.Collapsed(s, e);
            if (SkOpSpanBaseCollapsed.No != result)
            {
                return result;
            }
        }
        while (span.UpCastable() != null && (span = span.UpCast().Next()) != null);

        return SkOpSpanBaseCollapsed.No;
    }

    public static bool ComputeOneSum(SkOpAngle baseAngle, SkOpAngle nextAngle, SkOpAngle.IncludeType includeType)
    {
        SkOpSegment baseSegment = baseAngle.Segment();
        int sumMiWinding = baseSegment.UpdateWindingReverse(baseAngle);
        int sumSuWinding = 0; // uninitialized in Skia; read only when binary
        bool binary = includeType >= SkOpAngle.IncludeType.BinarySingle;
        if (binary)
        {
            sumSuWinding = baseSegment.UpdateOppWindingReverse(baseAngle);
            if (baseSegment.Operand())
            {
                (sumMiWinding, sumSuWinding) = (sumSuWinding, sumMiWinding);
            }
        }

        SkOpSegment nextSegment = nextAngle.Segment();
        int maxWinding, sumWinding;
        SkOpSpanBase? last = null;
        if (binary)
        {
            nextSegment.SetUpWindings(
                nextAngle.Start(),
                nextAngle.End(),
                ref sumMiWinding,
                ref sumSuWinding,
                out maxWinding,
                out sumWinding,
                out int oppMaxWinding,
                out int oppSumWinding);
            if (!nextSegment.MarkAngle(maxWinding, sumWinding, oppMaxWinding, oppSumWinding, nextAngle, ref last))
            {
                return false;
            }
        }
        else
        {
            nextSegment.SetUpWindings(
                nextAngle.Start(),
                nextAngle.End(),
                ref sumMiWinding,
                out maxWinding,
                out sumWinding);
            if (!nextSegment.MarkAngle(maxWinding, sumWinding, nextAngle, ref last))
            {
                return false;
            }
        }

        nextAngle.SetLastMarked(last);
        return true;
    }

    public static bool ComputeOneSumReverse(
        SkOpAngle baseAngle,
        SkOpAngle nextAngle,
        SkOpAngle.IncludeType includeType)
    {
        SkOpSegment baseSegment = baseAngle.Segment();
        int sumMiWinding = baseSegment.UpdateWinding(baseAngle);
        int sumSuWinding = 0; // uninitialized in Skia; read only when binary
        bool binary = includeType >= SkOpAngle.IncludeType.BinarySingle;
        if (binary)
        {
            sumSuWinding = baseSegment.UpdateOppWinding(baseAngle);
            if (baseSegment.Operand())
            {
                (sumMiWinding, sumSuWinding) = (sumSuWinding, sumMiWinding);
            }
        }

        SkOpSegment nextSegment = nextAngle.Segment();
        int maxWinding, sumWinding;
        SkOpSpanBase? last = null;
        if (binary)
        {
            nextSegment.SetUpWindings(
                nextAngle.End(),
                nextAngle.Start(),
                ref sumMiWinding,
                ref sumSuWinding,
                out maxWinding,
                out sumWinding,
                out int oppMaxWinding,
                out int oppSumWinding);
            if (!nextSegment.MarkAngle(maxWinding, sumWinding, oppMaxWinding, oppSumWinding, nextAngle, ref last))
            {
                return false;
            }
        }
        else
        {
            nextSegment.SetUpWindings(
                nextAngle.End(),
                nextAngle.Start(),
                ref sumMiWinding,
                out maxWinding,
                out sumWinding);
            if (!nextSegment.MarkAngle(maxWinding, sumWinding, nextAngle, ref last))
            {
                return false;
            }
        }

        nextAngle.SetLastMarked(last);
        return true;
    }

    // at this point, the span is already ordered, or unorderable
    public int ComputeSum(SkOpSpanBase start, SkOpSpanBase end, SkOpAngle.IncludeType includeType)
    {
        SkOpAngle? firstAngle = SpanToAngle(end, start);
        if (null == firstAngle || null == firstAngle.Next())
        {
            return SkPathOps.SkNaN32;
        }

        // if all angles have a computed winding,
        //  or if no adjacent angles are orderable,
        //  or if adjacent orderable angles have no computed winding,
        //  there's nothing to do
        // if two orderable angles are adjacent, and both are next to orderable angles,
        //  and one has winding computed, transfer to the other
        SkOpAngle? baseAngle = null;
        bool tryReverse = false;
        // look for counterclockwise transfers
        SkOpAngle angle = firstAngle.Previous();
        SkOpAngle next = angle.Next()!;
        firstAngle = next;
        do
        {
            SkOpAngle prior = angle;
            angle = next;
            next = angle.Next()!;
            if (prior.Unorderable() || angle.Unorderable() || next.Unorderable())
            {
                baseAngle = null;
                continue;
            }

            int testWinding = angle.Starter().WindSum();
            if (SkPathOps.SkMinS32 != testWinding)
            {
                baseAngle = angle;
                tryReverse = true;
                continue;
            }

            if (baseAngle != null)
            {
                ComputeOneSum(baseAngle, angle, includeType);
                baseAngle = SkPathOps.SkMinS32 != angle.Starter().WindSum() ? angle : null;
            }
        }
        while (next != firstAngle);

        if (baseAngle != null && SkPathOps.SkMinS32 == firstAngle.Starter().WindSum())
        {
            firstAngle = baseAngle;
            tryReverse = true;
        }

        if (tryReverse)
        {
            baseAngle = null;
            SkOpAngle prior = firstAngle;
            do
            {
                angle = prior;
                prior = angle.Previous();
                next = angle.Next()!;
                if (prior.Unorderable() || angle.Unorderable() || next.Unorderable())
                {
                    baseAngle = null;
                    continue;
                }

                int testWinding = angle.Starter().WindSum();
                if (SkPathOps.SkMinS32 != testWinding)
                {
                    baseAngle = angle;
                    continue;
                }

                if (baseAngle != null)
                {
                    ComputeOneSumReverse(baseAngle, angle, includeType);
                    baseAngle = SkPathOps.SkMinS32 != angle.Starter().WindSum() ? angle : null;
                }
            }
            while (prior != firstAngle);
        }

        return start.Starter(end).WindSum();
    }

    public bool Contains(double newT)
    {
        SkOpSpanBase spanBase = _head;
        do
        {
            if (spanBase.PtT().Contains(this, newT))
            {
                return true;
            }

            if (spanBase == _tail)
            {
                break;
            }

            spanBase = spanBase.UpCast().Next();
        }
        while (true);

        return false;
    }

    public void Release(SkOpSpan span)
    {
        if (span.Done())
        {
            --_doneCount;
        }

        --_count;
    }

    /*
     The M and S variable name parts stand for the operators.
       Mi stands for Minuend (see wiki subtraction, analogous to difference)
       Su stands for Subtrahend
     The Opp variable name part designates that the value is for the Opposite operator.
     Opposite values result from combining coincident spans.
     */
    public SkOpSegment? FindNextOp(
        List<SkOpSpanBase> chase,
        ref SkOpSpanBase nextStart,
        ref SkOpSpanBase nextEnd,
        ref bool unsortable,
        out bool simple,
        SkPathOp op,
        int xorMiMask,
        int xorSuMask)
    {
        SkOpSpanBase start = nextStart;
        SkOpSpanBase end = nextEnd;
        int step = start.Step(end);
        SkOpSegment? other = IsSimple(ref nextStart, ref step); // advances nextStart
        if (simple = other != null)
        {
            // mark the smaller of startIndex, endIndex done, and all adjacent
            // spans with the same T value (but not 'other' spans)
            SkOpSpan startSpan = start.Starter(end);
            if (startSpan.Done())
            {
                return null;
            }

            MarkDone(startSpan);
            nextEnd = step > 0 ? nextStart.UpCast().Next() : nextStart.Prev()!;
            return other;
        }

        SkOpSpanBase endNear = step > 0 ? nextStart.UpCast().Next() : nextStart.Prev()!;
        // more than one viable candidate -- measure angles to find best
        int calcWinding = ComputeSum(start, endNear, SkOpAngle.IncludeType.BinaryOpp);
        bool sortable = calcWinding != SkPathOps.SkNaN32;
        if (!sortable)
        {
            unsortable = true;
            MarkDone(start.Starter(end));
            return null;
        }

        SkOpAngle angle = SpanToAngle(end, start)!;
        if (angle.Unorderable())
        {
            unsortable = true;
            MarkDone(start.Starter(end));
            return null;
        }

        int sumMiWinding = UpdateWinding(end, start);
        if (sumMiWinding == SkPathOps.SkMinS32)
        {
            unsortable = true;
            MarkDone(start.Starter(end));
            return null;
        }

        int sumSuWinding = UpdateOppWinding(end, start);
        if (Operand())
        {
            (sumMiWinding, sumSuWinding) = (sumSuWinding, sumMiWinding);
        }

        SkOpAngle nextAngle = angle.Next()!;
        SkOpAngle? foundAngle = null;
        bool foundDone = false;
        // iterate through the angle, and compute everyone's winding
        SkOpSegment nextSegment;
        int activeCount = 0;
        do
        {
            nextSegment = nextAngle.Segment();
            bool activeAngle = nextSegment.ActiveOp(
                xorMiMask,
                xorSuMask,
                nextAngle.Start(),
                nextAngle.End(),
                op,
                ref sumMiWinding,
                ref sumSuWinding);
            if (activeAngle)
            {
                ++activeCount;
                if (foundAngle == null || (foundDone && (activeCount & 1) != 0))
                {
                    foundAngle = nextAngle;
                    foundDone = nextSegment.Done(nextAngle);
                }
            }

            if (nextSegment.Done())
            {
                continue;
            }

            if (!activeAngle)
            {
                _ = nextSegment.MarkAndChaseDone(nextAngle.Start(), nextAngle.End());
            }

            SkOpSpanBase? last = nextAngle.LastMarked();
            if (last != null)
            {
                chase.Add(last);
            }
        }
        while ((nextAngle = nextAngle.Next()!) != angle);

        start.Segment().MarkDone(start.Starter(end));
        if (foundAngle == null)
        {
            return null;
        }

        nextStart = foundAngle.Start();
        nextEnd = foundAngle.End();
        nextSegment = foundAngle.Segment();
        return nextSegment;
    }

    public SkOpSegment? FindNextWinding(
        List<SkOpSpanBase> chase,
        ref SkOpSpanBase nextStart,
        ref SkOpSpanBase nextEnd,
        ref bool unsortable)
    {
        SkOpSpanBase start = nextStart;
        SkOpSpanBase end = nextEnd;
        int step = start.Step(end);
        SkOpSegment? other = IsSimple(ref nextStart, ref step); // advances nextStart
        if (other != null)
        {
            // mark the smaller of startIndex, endIndex done, and all adjacent
            // spans with the same T value (but not 'other' spans)
            SkOpSpan startSpan = start.Starter(end);
            if (startSpan.Done())
            {
                return null;
            }

            MarkDone(startSpan);
            nextEnd = step > 0 ? nextStart.UpCast().Next() : nextStart.Prev()!;
            return other;
        }

        SkOpSpanBase endNear = step > 0 ? nextStart.UpCast().Next() : nextStart.Prev()!;
        // more than one viable candidate -- measure angles to find best
        int calcWinding = ComputeSum(start, endNear, SkOpAngle.IncludeType.UnaryWinding);
        bool sortable = calcWinding != SkPathOps.SkNaN32;
        if (!sortable)
        {
            unsortable = true;
            MarkDone(start.Starter(end));
            return null;
        }

        SkOpAngle angle = SpanToAngle(end, start)!;
        if (angle.Unorderable())
        {
            unsortable = true;
            MarkDone(start.Starter(end));
            return null;
        }

        int sumWinding = UpdateWinding(end, start);
        SkOpAngle nextAngle = angle.Next()!;
        SkOpAngle? foundAngle = null;
        bool foundDone = false;
        // iterate through the angle, and compute everyone's winding
        SkOpSegment nextSegment;
        int activeCount = 0;
        do
        {
            nextSegment = nextAngle.Segment();
            bool activeAngle = nextSegment.ActiveWinding(nextAngle.Start(), nextAngle.End(), ref sumWinding);
            if (activeAngle)
            {
                ++activeCount;
                if (foundAngle == null || (foundDone && (activeCount & 1) != 0))
                {
                    foundAngle = nextAngle;
                    foundDone = nextSegment.Done(nextAngle);
                }
            }

            if (nextSegment.Done())
            {
                continue;
            }

            if (!activeAngle)
            {
                _ = nextSegment.MarkAndChaseDone(nextAngle.Start(), nextAngle.End());
            }

            SkOpSpanBase? last = nextAngle.LastMarked();
            if (last != null)
            {
                chase.Add(last);
            }
        }
        while ((nextAngle = nextAngle.Next()!) != angle);

        start.Segment().MarkDone(start.Starter(end));
        if (foundAngle == null)
        {
            return null;
        }

        nextStart = foundAngle.Start();
        nextEnd = foundAngle.End();
        nextSegment = foundAngle.Segment();
        return nextSegment;
    }

    public SkOpSegment? FindNextXor(ref SkOpSpanBase nextStart, ref SkOpSpanBase nextEnd, ref bool unsortable)
    {
        SkOpSpanBase start = nextStart;
        SkOpSpanBase end = nextEnd;
        int step = start.Step(end);
        SkOpSegment? other = IsSimple(ref nextStart, ref step); // advances nextStart
        if (other != null)
        {
            // mark the smaller of startIndex, endIndex done, and all adjacent
            // spans with the same T value (but not 'other' spans)
            SkOpSpan startSpan = start.Starter(end);
            if (startSpan.Done())
            {
                return null;
            }

            MarkDone(startSpan);
            nextEnd = step > 0 ? nextStart.UpCast().Next() : nextStart.Prev()!;
            return other;
        }

        SkOpAngle? angle = SpanToAngle(end, start);
        if (angle == null || angle.Unorderable())
        {
            unsortable = true;
            MarkDone(start.Starter(end));
            return null;
        }

        SkOpAngle? nextAngle = angle.Next();
        SkOpAngle? foundAngle = null;
        bool foundDone = false;
        // iterate through the angle, and compute everyone's winding
        SkOpSegment nextSegment;
        int activeCount = 0;
        do
        {
            if (nextAngle == null)
            {
                return null;
            }

            nextSegment = nextAngle.Segment();
            ++activeCount;
            if (foundAngle == null || (foundDone && (activeCount & 1) != 0))
            {
                foundAngle = nextAngle;
                if (!(foundDone = nextSegment.Done(nextAngle)))
                {
                    break;
                }
            }

            nextAngle = nextAngle.Next();
        }
        while (nextAngle != angle);

        start.Segment().MarkDone(start.Starter(end));
        if (foundAngle == null)
        {
            return null;
        }

        nextStart = foundAngle.Start();
        nextEnd = foundAngle.End();
        nextSegment = foundAngle.Segment();
        return nextSegment;
    }

    public SkOpGlobalState GlobalState()
    {
        return Contour().GlobalState();
    }

    public void Init(ReadOnlySpan<PathPoint> pts, float weight, SkOpContour contour, PathVerb verb)
    {
        _contour = contour;
        _next = null;
        _pts = pts[..(SkPathOps.SkPathOpsVerbToPoints(verb) + 1)].ToArray();
        _weight = weight;
        _verb = verb;
        _count = 0;
        _doneCount = 0;
        _visited = false;
        SkOpSpan zeroSpan = _head;
        zeroSpan.Init(this, null, 0, _pts[0]);
        SkOpSpanBase oneSpan = _tail;
        zeroSpan.SetNext(oneSpan);
        oneSpan.InitBase(this, zeroSpan, 1, _pts[SkPathOps.SkPathOpsVerbToPoints(_verb)]);
    }

    public bool IsClose(double t, SkOpSegment opp)
    {
        SkDPoint cPt = DPtAtT(t);
        SkDVector dxdy = SkPathOps.CurveDSlopeAtT[(int)Verb()]!(Pts(), Weight(), t);
        SkDLine perp = new SkDLine(cPt, new SkDPoint(cPt.X + dxdy.Y, cPt.Y - dxdy.X));
        SkIntersections i = new SkIntersections();
        SkPathOps.CurveIntersectRay[(int)opp.Verb()]!(opp.Pts(), opp.Weight(), perp, i);
        int used = i.Used();
        for (int index = 0; index < used; ++index)
        {
            if (cPt.RoughlyEqual(i.Pt(index)))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsXor()
    {
        return _contour.IsXor();
    }

    public void MarkAllDone()
    {
        SkOpSpan? span = Head();
        do
        {
            MarkDone(span);
        }
        while ((span = span.Next().UpCastable()) != null);
    }

    // Skia's markAndChaseDone(start, end, nullptr).
    public bool MarkAndChaseDone(SkOpSpanBase start, SkOpSpanBase end)
    {
        SkOpSpanBase? found = null;
        return MarkAndChaseDone(start, end, ref found);
    }

    public bool MarkAndChaseDone(SkOpSpanBase start, SkOpSpanBase end, ref SkOpSpanBase? found)
    {
        int step = start.Step(end);
        SkOpSpan? minSpan = start.Starter(end);
        MarkDone(minSpan);
        SkOpSpanBase? last = null;
        SkOpSegment? other = this;
        SkOpSpan? priorDone = null;
        SkOpSpan? lastDone = null;
        int safetyNet = 100000;
        while ((other = other.NextChase(ref start, ref step, ref minSpan, ref last)) != null)
        {
            if (--safetyNet == 0)
            {
                return false;
            }

            if (other.Done())
            {
                break;
            }

            if (lastDone == minSpan || priorDone == minSpan)
            {
                found = null;
                return true;
            }

            other.MarkDone(minSpan!);
            priorDone = lastDone;
            lastDone = minSpan;
        }

        found = last;
        return true;
    }

    // Skia's markAndChaseWinding(start, end, winding, nullptr).
    public bool MarkAndChaseWinding(SkOpSpanBase start, SkOpSpanBase end, int winding)
    {
        SkOpSpanBase? lastPtr = null;
        return MarkAndChaseWinding(start, end, winding, ref lastPtr);
    }

    public bool MarkAndChaseWinding(SkOpSpanBase start, SkOpSpanBase end, int winding, ref SkOpSpanBase? lastPtr)
    {
        SkOpSpan? spanStart = start.Starter(end);
        int step = start.Step(end);
        bool success = MarkWinding(spanStart, winding);
        SkOpSpanBase? last = null;
        SkOpSegment? other = this;
        int safetyNet = 100000;
        while ((other = other.NextChase(ref start, ref step, ref spanStart, ref last)) != null)
        {
            if (--safetyNet == 0)
            {
                return false;
            }

            if (spanStart!.WindSum() != SkPathOps.SkMinS32)
            {
                //            SkASSERT(spanStart->windSum() == winding);   // FIXME: is this assert too aggressive?
                break;
            }

            _ = other.MarkWinding(spanStart, winding);
        }

        lastPtr = last;
        return success;
    }

    // Skia's markAndChaseWinding(start, end, winding, oppWinding, nullptr).
    public bool MarkAndChaseWinding(SkOpSpanBase start, SkOpSpanBase end, int winding, int oppWinding)
    {
        SkOpSpanBase? lastPtr = null;
        return MarkAndChaseWinding(start, end, winding, oppWinding, ref lastPtr);
    }

    public bool MarkAndChaseWinding(
        SkOpSpanBase start,
        SkOpSpanBase end,
        int winding,
        int oppWinding,
        ref SkOpSpanBase? lastPtr)
    {
        SkOpSpan? spanStart = start.Starter(end);
        int step = start.Step(end);
        bool success = MarkWinding(spanStart, winding, oppWinding);
        SkOpSpanBase? last = null;
        SkOpSegment? other = this;
        int safetyNet = 100000;
        while ((other = other.NextChase(ref start, ref step, ref spanStart, ref last)) != null)
        {
            if (--safetyNet == 0)
            {
                return false;
            }

            if (spanStart!.WindSum() != SkPathOps.SkMinS32)
            {
                if (Operand() == other.Operand())
                {
                    if (spanStart.WindSum() != winding || spanStart.OppSum() != oppWinding)
                    {
                        GlobalState().SetWindingFailed();
                        return true; // ... but let it succeed anyway
                    }
                }
                else
                {
                    if (spanStart.WindSum() != oppWinding)
                    {
                        return false;
                    }

                    if (spanStart.OppSum() != winding)
                    {
                        return false;
                    }
                }

                break;
            }

            if (Operand() == other.Operand())
            {
                _ = other.MarkWinding(spanStart, winding, oppWinding);
            }
            else
            {
                _ = other.MarkWinding(spanStart, oppWinding, winding);
            }
        }

        lastPtr = last;
        return success;
    }

    // Skia's markAngle(maxWinding, sumWinding, angle, nullptr).
    public bool MarkAngle(int maxWinding, int sumWinding, SkOpAngle angle)
    {
        SkOpSpanBase? result = null;
        return MarkAngle(maxWinding, sumWinding, angle, ref result);
    }

    public bool MarkAngle(int maxWinding, int sumWinding, SkOpAngle angle, ref SkOpSpanBase? result)
    {
        if (UseInnerWinding(maxWinding, sumWinding))
        {
            maxWinding = sumWinding;
        }

        if (!MarkAndChaseWinding(angle.Start(), angle.End(), maxWinding, ref result))
        {
            return false;
        }

        return true;
    }

    // Skia's markAngle(maxWinding, sumWinding, oppMaxWinding, oppSumWinding, angle, nullptr).
    public bool MarkAngle(int maxWinding, int sumWinding, int oppMaxWinding, int oppSumWinding, SkOpAngle angle)
    {
        SkOpSpanBase? result = null;
        return MarkAngle(maxWinding, sumWinding, oppMaxWinding, oppSumWinding, angle, ref result);
    }

    public bool MarkAngle(
        int maxWinding,
        int sumWinding,
        int oppMaxWinding,
        int oppSumWinding,
        SkOpAngle angle,
        ref SkOpSpanBase? result)
    {
        if (UseInnerWinding(maxWinding, sumWinding))
        {
            maxWinding = sumWinding;
        }

        if (oppMaxWinding != oppSumWinding && UseInnerWinding(oppMaxWinding, oppSumWinding))
        {
            oppMaxWinding = oppSumWinding;
        }

        // caller doesn't require that this marks anything
        if (!MarkAndChaseWinding(angle.Start(), angle.End(), maxWinding, oppMaxWinding, ref result))
        {
            return false;
        }

        return true;
    }

    public void MarkDone(SkOpSpan span)
    {
        if (span.Done())
        {
            return;
        }

        span.SetDone(true);
        ++_doneCount;
    }

    public bool MarkWinding(SkOpSpan span, int winding)
    {
        if (span.Done())
        {
            return false;
        }

        span.SetWindSum(winding);
        return true;
    }

    public bool MarkWinding(SkOpSpan span, int winding, int oppWinding)
    {
        if (span.Done())
        {
            return false;
        }

        span.SetWindSum(winding);
        span.SetOppSum(oppWinding);
        return true;
    }

    public bool Match(SkOpPtT @base, SkOpSegment testParent, double testT, PathPoint testPt)
    {
        if (this == testParent)
        {
            if (SkPathOps.PreciselyEqual(@base.T, testT))
            {
                return true;
            }
        }

        if (!SkDPoint.ApproximatelyEqual(testPt, @base.Pt))
        {
            return false;
        }

        return this != testParent || !PtsDisjoint(@base.T, @base.Pt, testT, testPt);
    }

    // Skia's nextChase(startPtr, stepPtr, nullptr, nullptr).
    public SkOpSegment? NextChase(ref SkOpSpanBase startPtr, ref int stepPtr)
    {
        SkOpSpan? minPtr = null;
        SkOpSpanBase? last = null;
        return NextChase(ref startPtr, ref stepPtr, ref minPtr, ref last);
    }

    public SkOpSegment? NextChase(
        ref SkOpSpanBase startPtr,
        ref int stepPtr,
        ref SkOpSpan? minPtr,
        ref SkOpSpanBase? last)
    {
        SkOpSpanBase origStart = startPtr;
        int step = stepPtr;
        SkOpSpanBase endSpan = step > 0 ? origStart.UpCast().Next() : origStart.Prev()!;
        SkOpAngle? angle = step > 0 ? endSpan.FromAngle() : endSpan.UpCast().ToAngle();
        SkOpSpanBase foundSpan;
        SkOpSpanBase? otherEnd;
        SkOpSegment other;
        if (angle == null)
        {
            if (endSpan.T() != 0 && endSpan.T() != 1)
            {
                return null;
            }

            SkOpPtT otherPtT = endSpan.PtT().Next();
            other = otherPtT.Segment();
            foundSpan = otherPtT.Span();
            otherEnd = step > 0
                ? foundSpan.UpCastable() != null ? foundSpan.UpCast().Next() : null
                : foundSpan.Prev();
        }
        else
        {
            int loopCount = angle.LoopCount();
            if (loopCount > 2)
            {
                return SkOpSegmentStatics.SetLast(ref last, endSpan);
            }

            SkOpAngle? next = angle.Next();
            if (null == next)
            {
                return null;
            }

            other = next.Segment();
            foundSpan = endSpan = next.Start();
            otherEnd = next.End();
        }

        if (otherEnd == null)
        {
            return null;
        }

        int foundStep = foundSpan.Step(otherEnd);
        if (stepPtr != foundStep)
        {
            return SkOpSegmentStatics.SetLast(ref last, endSpan);
        }

        //    SkASSERT(otherEnd >= 0);
        SkOpSpan origMin = step < 0 ? origStart.Prev()! : origStart.UpCast();
        SkOpSpan foundMin = foundSpan.Starter(otherEnd);
        if (foundMin.WindValue() != origMin.WindValue()
            || foundMin.OppValue() != origMin.OppValue())
        {
            return SkOpSegmentStatics.SetLast(ref last, endSpan);
        }

        startPtr = foundSpan;
        stepPtr = foundStep;
        minPtr = foundMin;
        return other;
    }

    // Please keep this in sync with DebugClearVisited()
    public static void ClearVisited(SkOpSpanBase span)
    {
        // reset visited flag back to false
        do
        {
            SkOpPtT ptT = span.PtT(), stopPtT = ptT;
            while ((ptT = ptT.Next()) != stopPtT)
            {
                SkOpSegment opp = ptT.Segment();
                opp.ResetVisited();
            }
        }
        while (!span.Final() && (span = span.UpCast().Next()) != null);
    }

    // Please keep this in sync with debugMissingCoincidence()
    // look for pairs of undetected coincident curves
    // assumes that segments going in have visited flag clear
    // Even though pairs of curves correct detect coincident runs, a run may be missed
    // if the coincidence is a product of multiple intersections. For instance, given
    // curves A, B, and C:
    // A-B intersect at a point 1; A-C and B-C intersect at point 2, so near
    // the end of C that the intersection is replaced with the end of C.
    // Even though A-B correctly do not detect an intersection at point 2,
    // the resulting run from point 1 to point 2 is coincident on A and B.
    public bool MissingCoincidence()
    {
        if (Done())
        {
            return false;
        }

        SkOpSpan? prior = null;
        SkOpSpanBase? spanBase = _head;
        bool result = false;
        int safetyNet = 100000;
        do
        {
            SkOpPtT ptT = spanBase.PtT(), spanStopPtT = ptT;
            while ((ptT = ptT.Next()) != spanStopPtT)
            {
                if (--safetyNet == 0)
                {
                    return false;
                }

                if (ptT.Deleted())
                {
                    continue;
                }

                SkOpSegment opp = ptT.Span().Segment();
                if (opp.Done())
                {
                    continue;
                }

                // when opp is encounted the 1st time, continue; on 2nd encounter, look for coincidence
                if (!opp.Visited())
                {
                    continue;
                }

                if (spanBase == _head)
                {
                    continue;
                }

                if (ptT.Segment() == this)
                {
                    continue;
                }

                SkOpSpan? span = spanBase.UpCastable();
                // FIXME?: this assumes that if the opposite segment is coincident then no more
                // coincidence needs to be detected. This may not be true.
                if (span != null && span.ContainsCoincidence(opp))
                {
                    continue;
                }

                if (spanBase.ContainsCoinEnd(opp))
                {
                    continue;
                }

                SkOpPtT priorPtT = null!, priorStopPtT;
                // find prior span containing opp segment
                SkOpSegment? priorOpp = null;
                SkOpSpan? priorTest = spanBase.Prev();
                while (priorOpp == null && priorTest != null)
                {
                    priorStopPtT = priorPtT = priorTest.PtT();
                    while ((priorPtT = priorPtT.Next()) != priorStopPtT)
                    {
                        if (priorPtT.Deleted())
                        {
                            continue;
                        }

                        SkOpSegment segment = priorPtT.Span().Segment();
                        if (segment == opp)
                        {
                            prior = priorTest;
                            priorOpp = opp;
                            break;
                        }
                    }

                    priorTest = priorTest.Prev();
                }

                if (priorOpp == null)
                {
                    continue;
                }

                if (priorPtT == ptT)
                {
                    continue;
                }

                SkOpPtT oppStart = prior!.PtT();
                SkOpPtT oppEnd = spanBase.PtT();
                bool swapped = priorPtT.T > ptT.T;
                if (swapped)
                {
                    (priorPtT, ptT) = (ptT, priorPtT);
                    (oppStart, oppEnd) = (oppEnd, oppStart);
                }

                SkOpCoincidence coincidences = GlobalState().Coincidence()!;
                SkOpPtT rootPriorPtT = priorPtT.Span().PtT();
                SkOpPtT rootPtT = ptT.Span().PtT();
                SkOpPtT rootOppStart = oppStart.Span().PtT();
                SkOpPtT rootOppEnd = oppEnd.Span().PtT();
                if (coincidences.Contains(rootPriorPtT, rootPtT, rootOppStart, rootOppEnd))
                {
                    goto swapBack;
                }

                if (TestForCoincidence(rootPriorPtT, rootPtT, prior, spanBase, opp))
                {
                    // mark coincidence
                    if (!coincidences.Extend(rootPriorPtT, rootPtT, rootOppStart, rootOppEnd))
                    {
                        coincidences.Add(rootPriorPtT, rootPtT, rootOppStart, rootOppEnd);
                    }

                    result = true;
                }

            swapBack:
                if (swapped)
                {
                    (priorPtT, ptT) = (ptT, priorPtT);
                }
            }
        }
        while ((spanBase = spanBase.Final() ? null : spanBase.UpCast().Next()) != null);

        ClearVisited(_head);
        return result;
    }

    // please keep this in sync with debugMoveMultiples()
    // if a span has more than one intersection, merge the other segments' span as needed
    public bool MoveMultiples()
    {
        SkOpSpanBase? test = _head;
        do
        {
            int addCount = test.SpanAddsCount();
            //        FAIL_IF(addCount < 1);
            if (addCount <= 1)
            {
                continue;
            }

            SkOpPtT startPtT = test.PtT();
            SkOpPtT testPtT = startPtT;
            int safetyHatch = 1000000;
            do
            {
                // iterate through all spans associated with start
                if (--safetyHatch == 0)
                {
                    return false;
                }

                SkOpSpanBase oppSpan = testPtT.Span();
                if (oppSpan.SpanAddsCount() == addCount)
                {
                    continue;
                }

                if (oppSpan.Deleted())
                {
                    continue;
                }

                SkOpSegment oppSegment = oppSpan.Segment();
                if (oppSegment == this)
                {
                    continue;
                }

                // find range of spans to consider merging
                SkOpSpanBase? oppPrev = oppSpan;
                SkOpSpanBase oppFirst = oppSpan;
                while ((oppPrev = oppPrev.Prev()) != null)
                {
                    if (!SkPathOps.RoughlyEqual(oppPrev.T(), oppSpan.T()))
                    {
                        break;
                    }

                    if (oppPrev.SpanAddsCount() == addCount)
                    {
                        continue;
                    }

                    if (oppPrev.Deleted())
                    {
                        continue;
                    }

                    oppFirst = oppPrev;
                }

                SkOpSpanBase? oppNext = oppSpan;
                SkOpSpanBase oppLast = oppSpan;
                while ((oppNext = oppNext.Final() ? null : oppNext.UpCast().Next()) != null)
                {
                    if (!SkPathOps.RoughlyEqual(oppNext.T(), oppSpan.T()))
                    {
                        break;
                    }

                    if (oppNext.SpanAddsCount() == addCount)
                    {
                        continue;
                    }

                    if (oppNext.Deleted())
                    {
                        continue;
                    }

                    oppLast = oppNext;
                }

                if (oppFirst == oppLast)
                {
                    continue;
                }

                SkOpSpanBase? oppTest = oppFirst;
                do
                {
                    if (oppTest == oppSpan)
                    {
                        continue;
                    }

                    // check to see if the candidate meets specific criteria:
                    // it contains spans of segments in test's loop but not including 'this'
                    SkOpPtT oppStartPtT = oppTest.PtT();
                    SkOpPtT oppPtT = oppStartPtT;
                    while ((oppPtT = oppPtT.Next()) != oppStartPtT)
                    {
                        SkOpSegment oppPtTSegment = oppPtT.Segment();
                        if (oppPtTSegment == this)
                        {
                            goto tryNextSpan;
                        }

                        SkOpPtT matchPtT = startPtT;
                        do
                        {
                            if (matchPtT.Segment() == oppPtTSegment)
                            {
                                goto foundMatch;
                            }
                        }
                        while ((matchPtT = matchPtT.Next()) != startPtT);

                        goto tryNextSpan;
                    foundMatch: // merge oppTest and oppSpan
                        oppTest.MergeMatches(oppSpan);
                        oppTest.AddOpp(oppSpan);
                        goto checkNextSpan;
                    }

                tryNextSpan:
                    ;
                }
                while (oppTest != oppLast && (oppTest = oppTest.UpCast().Next()) != null);
            }
            while ((testPtT = testPtT.Next()) != startPtT);

        checkNextSpan:
            ;
        }
        while ((test = test.Final() ? null : test.UpCast().Next()) != null);

        return true;
    }

    // adjacent spans may have points close by
    public bool SpansNearby(SkOpSpanBase refSpan, SkOpSpanBase checkSpan, ref bool found)
    {
        SkOpPtT refHead = refSpan.PtT();
        SkOpPtT checkHead = checkSpan.PtT();
        // if the first pt pair from adjacent spans are far apart, assume that all are far enough apart
        if (!SkDPoint.WayRoughlyEqual(refHead.Pt, checkHead.Pt))
        {
            found = false;
            return true;
        }

        // check only unique points
        float distSqBest = SkPathOps.SkScalarMax;
        SkOpPtT? refBest = null;
        SkOpPtT? checkBest = null;
        SkOpPtT @ref = refHead;
        do
        {
            if (@ref.Deleted())
            {
                continue;
            }

            while (@ref.PtAlreadySeen(refHead))
            {
                @ref = @ref.Next();
                if (@ref == refHead)
                {
                    goto doneCheckingDistance;
                }
            }

            SkOpPtT check = checkHead;
            SkOpSegment refSeg = @ref.Segment();
            int escapeHatch = 100000; // defend against infinite loops
            do
            {
                if (check.Deleted())
                {
                    continue;
                }

                while (check.PtAlreadySeen(checkHead))
                {
                    check = check.Next();
                    if (check == checkHead)
                    {
                        goto nextRef;
                    }
                }

                float distSq = SkPathOps.SkPointPrivDistanceToSqd(@ref.Pt, check.Pt);
                if (distSqBest > distSq && (refSeg != check.Segment()
                    || !refSeg.PtsDisjoint(@ref, check)))
                {
                    distSqBest = distSq;
                    refBest = @ref;
                    checkBest = check;
                }

                if (--escapeHatch <= 0)
                {
                    return false;
                }
            }
            while ((check = check.Next()) != checkHead);

        nextRef:
            ;
        }
        while ((@ref = @ref.Next()) != refHead);

    doneCheckingDistance:
        found = checkBest != null
            && refBest!.Segment().Match(refBest, checkBest.Segment(), checkBest.T, checkBest.Pt);
        return true;
    }

    // Please keep this function in sync with debugMoveNearby()
    // Move nearby t values and pts so they all hang off the same span. Alignment happens later.
    public bool MoveNearby()
    {
        // release undeleted spans pointing to this seg that are linked to the primary span
        SkOpSpanBase spanBase = _head;
        int escapeHatch = 9999; // the largest count for a regular test is 50; for a fuzzer, 500
        do
        {
            SkOpPtT ptT = spanBase.PtT();
            SkOpPtT headPtT = ptT;
            while ((ptT = ptT.Next()) != headPtT)
            {
                if (--escapeHatch == 0)
                {
                    return false;
                }

                SkOpSpanBase test = ptT.Span();
                if (ptT.Segment() == this && !ptT.Deleted() && test != spanBase
                    && test.PtT() == ptT)
                {
                    if (test.Final())
                    {
                        if (spanBase == _head)
                        {
                            ClearAll();
                            return true;
                        }

                        spanBase.UpCast().Release(ptT);
                    }
                    else if (test.Prev() != null)
                    {
                        test.UpCast().Release(headPtT);
                    }

                    break;
                }
            }

            spanBase = spanBase.UpCast().Next();
        }
        while (!spanBase.Final());

        // This loop looks for adjacent spans which are near by
        spanBase = _head;
        do
        {
            // iterate through all spans associated with start
            SkOpSpanBase test = spanBase.UpCast().Next();
            bool found = false;
            if (!SpansNearby(spanBase, test, ref found))
            {
                return false;
            }

            if (found)
            {
                if (test.Final())
                {
                    if (spanBase.Prev() != null)
                    {
                        test.Merge(spanBase.UpCast());
                    }
                    else
                    {
                        ClearAll();
                        return true;
                    }
                }
                else
                {
                    spanBase.Merge(test.UpCast());
                }
            }

            spanBase = test;
        }
        while (!spanBase.Final());

        return true;
    }

    public bool Operand()
    {
        return _contour.Operand();
    }

    public bool OppXor()
    {
        return _contour.OppXor();
    }

    public bool PtsDisjoint(double t1, PathPoint pt1, double t2, PathPoint pt2)
    {
        if (_verb == PathVerb.Line)
        {
            return false;
        }

        // quads (and cubics) can loop back to nearly a line so that an opposite curve
        // hits in two places with very different t values.
        // OPTIMIZATION: curves could be preflighted so that, for example, something like
        // 'controls contained by ends' could avoid this check for common curves
        // 'ends are extremes in x or y' is cheaper to compute and real-world common
        // on the other hand, the below check is relatively inexpensive
        double midT = (t1 + t2) / 2;
        PathPoint midPt = PtAtT(midT);
        double seDistSq = SkPathOps.StdMax(
            SkPathOps.SkPointPrivDistanceToSqd(pt1, pt2) * 2,
            SkPathOps.FltEpsilon * 2);
        return SkPathOps.SkPointPrivDistanceToSqd(midPt, pt1) > seDistSq
            || SkPathOps.SkPointPrivDistanceToSqd(midPt, pt2) > seDistSq;
    }

    public void SetUpWindings(
        SkOpSpanBase start,
        SkOpSpanBase end,
        ref int sumMiWinding,
        out int maxWinding,
        out int sumWinding)
    {
        int deltaSum = SpanSign(start, end);
        maxWinding = sumMiWinding;
        sumWinding = sumMiWinding -= deltaSum;
    }

    public void SetUpWindings(
        SkOpSpanBase start,
        SkOpSpanBase end,
        ref int sumMiWinding,
        ref int sumSuWinding,
        out int maxWinding,
        out int sumWinding,
        out int oppMaxWinding,
        out int oppSumWinding)
    {
        int deltaSum = SpanSign(start, end);
        int oppDeltaSum = OppSign(start, end);
        if (Operand())
        {
            maxWinding = sumSuWinding;
            sumWinding = sumSuWinding -= deltaSum;
            oppMaxWinding = sumMiWinding;
            oppSumWinding = sumMiWinding -= oppDeltaSum;
        }
        else
        {
            maxWinding = sumMiWinding;
            sumWinding = sumMiWinding -= deltaSum;
            oppMaxWinding = sumSuWinding;
            oppSumWinding = sumSuWinding -= oppDeltaSum;
        }
    }

    public bool SortAngles()
    {
        SkOpSpanBase span = _head;
        do
        {
            SkOpAngle? fromAngle = span.FromAngle();
            SkOpAngle? toAngle = span.Final() ? null : span.UpCast().ToAngle();
            if (fromAngle == null && toAngle == null)
            {
                continue;
            }

            SkOpAngle? baseAngle = fromAngle;
            if (fromAngle != null && toAngle != null)
            {
                if (!fromAngle.Insert(toAngle))
                {
                    return false;
                }
            }
            else if (fromAngle == null)
            {
                baseAngle = toAngle;
            }

            SkOpPtT ptT = span.PtT(), stopPtT = ptT;
            int safetyNet = 1000000;
            do
            {
                if (--safetyNet == 0)
                {
                    return false;
                }

                SkOpSpanBase oSpan = ptT.Span();
                if (oSpan == span)
                {
                    continue;
                }

                SkOpAngle? oAngle = oSpan.FromAngle();
                if (oAngle != null)
                {
                    if (!oAngle.LoopContains(baseAngle!))
                    {
                        baseAngle!.Insert(oAngle);
                    }
                }

                if (!oSpan.Final())
                {
                    oAngle = oSpan.UpCast().ToAngle();
                    if (oAngle != null)
                    {
                        if (!oAngle.LoopContains(baseAngle!))
                        {
                            baseAngle!.Insert(oAngle);
                        }
                    }
                }
            }
            while ((ptT = ptT.Next()) != stopPtT);

            if (baseAngle!.LoopCount() == 1)
            {
                span.SetFromAngle(null);
                if (toAngle != null)
                {
                    span.UpCast().SetToAngle(null);
                }

                baseAngle = null;
            }
        }
        while (!span.Final() && (span = span.UpCast().Next()) != null);

        return true;
    }

    public bool SubDivide(SkOpSpanBase start, SkOpSpanBase end, ref SkDCurve edge)
    {
        SkOpPtT startPtT = start.PtT();
        SkOpPtT endPtT = end.PtT();
        edge.Cubic[0].Set(startPtT.Pt);
        int points = SkPathOps.SkPathOpsVerbToPoints(_verb);
        edge.Cubic[points].Set(endPtT.Pt);
        if (_verb == PathVerb.Line)
        {
            return false;
        }

        double startT = startPtT.T;
        double endT = endPtT.T;
        if ((startT == 0 || endT == 0) && (startT == 1 || endT == 1))
        {
            // don't compute midpoints if we already have them
            if (_verb == PathVerb.Quad)
            {
                edge.Line[1].Set(_pts[1]);
                return false;
            }

            if (_verb == PathVerb.Conic)
            {
                edge.Conic[1].Set(_pts[1]);
                edge.Conic.Weight = _weight;
                return false;
            }

            if (startT == 0)
            {
                edge.Cubic[1].Set(_pts[1]);
                edge.Cubic[2].Set(_pts[2]);
                return false;
            }

            edge.Cubic[1].Set(_pts[2]);
            edge.Cubic[2].Set(_pts[1]);
            return false;
        }

        if (_verb == PathVerb.Quad)
        {
            edge.Quad[1] = SkDQuad.SubDivide(_pts, edge.Quad[0], edge.Quad[2], startT, endT);
        }
        else if (_verb == PathVerb.Conic)
        {
            edge.Conic[1] = SkDConic.SubDivide(
                _pts,
                _weight,
                edge.Quad[0],
                edge.Quad[2],
                startT,
                endT,
                out edge.Conic.Weight);
        }
        else
        {
            SkDCubic.SubDivide(_pts, edge.Cubic[0], edge.Cubic[3], startT, endT, edge.Cubic.Pts[1..]);
        }

        return true;
    }

    public bool TestForCoincidence(
        SkOpPtT priorPtT,
        SkOpPtT ptT,
        SkOpSpanBase prior,
        SkOpSpanBase spanBase,
        SkOpSegment opp)
    {
        // average t, find mid pt
        double midT = (prior.T() + spanBase.T()) / 2;
        PathPoint midPt = PtAtT(midT);
        bool coincident = true;
        // if the mid pt is not near either end pt, project perpendicular through opp seg
        if (!SkDPoint.ApproximatelyEqual(priorPtT.Pt, midPt)
            && !SkDPoint.ApproximatelyEqual(ptT.Pt, midPt))
        {
            if (priorPtT.Span() == ptT.Span())
            {
                return false;
            }

            coincident = false;
            SkIntersections i = new SkIntersections();
            SkDCurve curvePart = default;
            SubDivide(prior, spanBase, ref curvePart);
            SkDVector dxdy = SkPathOps.CurveDDSlopeAtT[(int)_verb]!(curvePart, 0.5f);
            SkDPoint partMidPt = SkPathOps.CurveDDPointAtT[(int)_verb]!(curvePart, 0.5f);
            SkDLine ray = new SkDLine(
                new SkDPoint(midPt.X, midPt.Y),
                new SkDPoint(partMidPt.X + dxdy.Y, partMidPt.Y - dxdy.X));
            SkDCurve oppPart = default;
            opp.SubDivide(priorPtT.Span(), ptT.Span(), ref oppPart);
            SkPathOps.CurveDIntersectRay[(int)opp.Verb()]!(oppPart, ray, i);
            // measure distance and see if it's small enough to denote coincidence
            for (int index = 0; index < i.Used(); ++index)
            {
                if (!SkPathOps.Between(0, i[0][index], 1))
                {
                    continue;
                }

                SkDPoint oppPt = i.Pt(index);
                if (oppPt.ApproximatelyDEqual(midPt))
                {
                    // the coincidence can occur at almost any angle
                    coincident = true;
                }
            }
        }

        return coincident;
    }

    public SkOpSpan? UndoneSpan()
    {
        SkOpSpan span = _head;
        SkOpSpanBase next;
        do
        {
            next = span.Next();
            if (!span.Done())
            {
                return span;
            }
        }
        while (!next.Final() && (span = next.UpCast()) != null);

        return null;
    }

    public int UpdateOppWinding(SkOpSpanBase start, SkOpSpanBase end)
    {
        SkOpSpan lesser = start.Starter(end);
        int oppWinding = lesser.OppSum();
        int oppSpanWinding = OppSign(start, end);
        if (oppSpanWinding != 0 && UseInnerWinding(oppWinding - oppSpanWinding, oppWinding)
            && oppWinding != SkPathOps.SkMaxS32)
        {
            oppWinding -= oppSpanWinding;
        }

        return oppWinding;
    }

    public int UpdateOppWinding(SkOpAngle angle)
    {
        SkOpSpanBase startSpan = angle.Start();
        SkOpSpanBase endSpan = angle.End();
        return UpdateOppWinding(endSpan, startSpan);
    }

    public int UpdateOppWindingReverse(SkOpAngle angle)
    {
        SkOpSpanBase startSpan = angle.Start();
        SkOpSpanBase endSpan = angle.End();
        return UpdateOppWinding(startSpan, endSpan);
    }

    public int UpdateWinding(SkOpSpanBase start, SkOpSpanBase end)
    {
        SkOpSpan lesser = start.Starter(end);
        int winding = lesser.WindSum();
        if (winding == SkPathOps.SkMinS32)
        {
            winding = lesser.ComputeWindSum();
        }

        if (winding == SkPathOps.SkMinS32)
        {
            return winding;
        }

        int spanWinding = SpanSign(start, end);
        if (winding != 0 && UseInnerWinding(winding - spanWinding, winding)
            && winding != SkPathOps.SkMaxS32)
        {
            winding -= spanWinding;
        }

        return winding;
    }

    public int UpdateWinding(SkOpAngle angle)
    {
        SkOpSpanBase startSpan = angle.Start();
        SkOpSpanBase endSpan = angle.End();
        return UpdateWinding(endSpan, startSpan);
    }

    public int UpdateWindingReverse(SkOpAngle angle)
    {
        SkOpSpanBase startSpan = angle.Start();
        SkOpSpanBase endSpan = angle.End();
        return UpdateWinding(startSpan, endSpan);
    }

    // OPTIMIZATION: does the following also work, and is it any faster?
    // return outerWinding * innerWinding > 0
    //      || ((outerWinding + innerWinding < 0) ^ ((outerWinding - innerWinding) < 0)))
    public static bool UseInnerWinding(int outerWinding, int innerWinding)
    {
        int absOut = SkPathOps.SkTAbs(outerWinding);
        int absIn = SkPathOps.SkTAbs(innerWinding);
        bool result = absOut == absIn ? outerWinding < 0 : absOut < absIn;
        return result;
    }

    public int WindSum(SkOpAngle angle)
    {
        SkOpSpan minSpan = angle.Start().Starter(angle.End());
        return minSpan.WindSum();
    }
}

file static class SkOpSegmentStatics
{
    public static readonly bool[,] GUnaryActiveEdge =
    {
        //  from=0  from=1
        //  to=0,1  to=0,1
        { false, true }, { true, false },
    };

    // [kXOR_SkPathOp + 1][2][2][2][2], indexed [op][miFrom][miTo][suFrom][suTo]
    public static readonly bool[,,,,] GActiveEdge = Build();

    public static SkOpSegment? SetLast(ref SkOpSpanBase? last, SkOpSpanBase endSpan)
    {
        last = endSpan;
        return null;
    }

    private static bool[,,,,] Build()
    {
        const bool F = false; // discard the edge
        const bool T = true; // keep the edge
        //                 miFrom=0                              miFrom=1
        //         miTo=0             miTo=1             miTo=0             miTo=1
        //     suFrom=0    1      suFrom=0    1      suFrom=0    1      suFrom=0    1
        //   suTo=0,1 suTo=0,1  suTo=0,1 suTo=0,1  suTo=0,1 suTo=0,1  suTo=0,1 suTo=0,1
        bool[,,,,] table =
        {
            { { { { F, F }, { F, F } }, { { T, F }, { T, F } } }, { { { T, T }, { F, F } }, { { F, T }, { T, F } } } },
            { { { { F, F }, { F, F } }, { { F, T }, { F, T } } }, { { { F, F }, { T, T } }, { { F, T }, { T, F } } } },
            { { { { F, T }, { T, F } }, { { T, T }, { F, F } } }, { { { T, F }, { T, F } }, { { F, F }, { F, F } } } },
            { { { { F, T }, { T, F } }, { { T, F }, { F, T } } }, { { { T, F }, { F, T } }, { { F, T }, { T, F } } } },
        };
        return table;
    }
}
