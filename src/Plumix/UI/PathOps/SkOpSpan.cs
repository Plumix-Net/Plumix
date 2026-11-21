namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkOpSpan.h and SkOpSpan.cpp at chrome/m119 (the Skia inside
// SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types are
// kept. SkOpPtT, SkOpSpanBase and SkOpSpan are classes (convention 6); the circular ptT lists, the coincidence
// loops and the span links are class references and pointer comparisons are reference comparisons. A span base
// owns its SkOpPtT (Skia embeds fPtT by value and hands out &fPtT), so a span and its ptT keep a one-to-one
// identity. upCast() is a C# cast: Skia's segments hold one SkOpSpanBase (the tail, t == 1) and SkOpSpan objects
// for every other span, so final() (fPtT.fT == 1) is exactly "is not an SkOpSpan"; a cast Skia would perform on the
// tail is undefined behaviour there and throws here. SkOpSpanBase::Collapsed is the top-level enum
// SkOpSpanBaseCollapsed, because C# cannot nest a type named like the collapsed() method. SkOpSpan is `partial`:
// sortableTop is defined in SkPathOpsWinding.cpp (its part is in SkPathOpsWinding.cs). setCoinStart and
// alignInner are declared but never defined in Skia, so they are not ported. FAIL_IF is `if (cond) return false;`.

internal enum SkOpSpanBaseCollapsed
{
    No,
    Yes,
    Error,
}

// subset of op span used by terminal span (when t is equal to one)
internal sealed class SkOpPtT
{
    public const int KIsAlias = 1;
    public const int KIsDuplicate = 1;

    public double T;
    public PathPoint Pt; // cache of point value at this t
    private SkOpSpanBase _span = null!; // contains winding data
    private SkOpPtT _next = null!; // intersection on opposite curve or alias on this curve
    private bool _deleted; // set if removed from span list
    private bool _duplicatePt; // set if identical pt is somewhere in the next loop
    // below mutable since referrer is otherwise always const
    private bool _coincident; // set if at some point a coincident span pointed here

    // please keep in sync with debugAddOpp()
    public void AddOpp(SkOpPtT opp, SkOpPtT oppPrev)
    {
        SkOpPtT oldNext = _next;
        _next = opp;
        oppPrev._next = oldNext;
    }

    public bool Coincident()
    {
        return _coincident;
    }

    public bool Deleted()
    {
        return _deleted;
    }

    public bool Duplicate()
    {
        return _duplicatePt;
    }

    public void Insert(SkOpPtT span)
    {
        span._next = _next;
        _next = span;
    }

    public SkOpPtT Next()
    {
        return _next;
    }

    // returns nullptr if this is already in the opp ptT loop
    public SkOpPtT? OppPrev(SkOpPtT opp)
    {
        // find the fOpp ptr to opp
        SkOpPtT oppPrev = opp._next;
        if (oppPrev == this)
        {
            return null;
        }

        while (oppPrev._next != opp)
        {
            oppPrev = oppPrev._next;
            if (oppPrev == this)
            {
                return null;
            }
        }

        return oppPrev;
    }

    public static bool Overlaps(
        SkOpPtT s1,
        SkOpPtT e1,
        SkOpPtT s2,
        SkOpPtT e2,
        out SkOpPtT? sOut,
        out SkOpPtT? eOut)
    {
        SkOpPtT start1 = s1.T < e1.T ? s1 : e1;
        SkOpPtT start2 = s2.T < e2.T ? s2 : e2;
        sOut = SkPathOps.Between(s1.T, start2.T, e1.T)
            ? start2
            : SkPathOps.Between(s2.T, start1.T, e2.T) ? start1 : null;
        SkOpPtT end1 = s1.T < e1.T ? e1 : s1;
        SkOpPtT end2 = s2.T < e2.T ? e2 : s2;
        eOut = SkPathOps.Between(s1.T, end2.T, e1.T)
            ? end2
            : SkPathOps.Between(s2.T, end1.T, e2.T) ? end1 : null;
        if (sOut == eOut)
        {
            return false;
        }

        return sOut != null && eOut != null;
    }

    public void SetCoincident()
    {
        _coincident = true;
    }

    public void SetSpan(SkOpSpanBase span)
    {
        _span = span;
    }

    public SkOpSpanBase Span()
    {
        return _span;
    }

    public SkOpPtT Starter(SkOpPtT end)
    {
        return T < end.T ? this : end;
    }

    // SkOpSpan.cpp

    public bool Alias()
    {
        return Span().PtT() != this;
    }

    public SkOpPtT? Active()
    {
        if (!_deleted)
        {
            return this;
        }

        SkOpPtT ptT = this;
        SkOpPtT stopPtT = ptT;
        while ((ptT = ptT.Next()) != stopPtT)
        {
            if (ptT._span == _span && !ptT._deleted)
            {
                return ptT;
            }
        }

        return null; // should never return deleted; caller must abort
    }

    public bool Contains(SkOpPtT check)
    {
        SkOpPtT ptT = this;
        SkOpPtT stopPtT = ptT;
        while ((ptT = ptT.Next()) != stopPtT)
        {
            if (ptT == check)
            {
                return true;
            }
        }

        return false;
    }

    public bool Contains(SkOpSegment segment, PathPoint pt)
    {
        SkOpPtT ptT = this;
        SkOpPtT stopPtT = ptT;
        while ((ptT = ptT.Next()) != stopPtT)
        {
            if (ptT.Pt == pt && ptT.Segment() == segment)
            {
                return true;
            }
        }

        return false;
    }

    public bool Contains(SkOpSegment segment, double t)
    {
        SkOpPtT ptT = this;
        SkOpPtT stopPtT = ptT;
        while ((ptT = ptT.Next()) != stopPtT)
        {
            if (ptT.T == t && ptT.Segment() == segment)
            {
                return true;
            }
        }

        return false;
    }

    public SkOpPtT? Contains(SkOpSegment check)
    {
        SkOpPtT ptT = this;
        SkOpPtT stopPtT = ptT;
        while ((ptT = ptT.Next()) != stopPtT)
        {
            if (ptT.Segment() == check && !ptT.Deleted())
            {
                return ptT;
            }
        }

        return null;
    }

    public SkOpContour Contour()
    {
        return Segment().Contour();
    }

    public SkOpPtT? Find(SkOpSegment segment)
    {
        SkOpPtT ptT = this;
        SkOpPtT stopPtT = ptT;
        do
        {
            if (ptT.Segment() == segment && !ptT.Deleted())
            {
                return ptT;
            }

            ptT = ptT._next;
        }
        while (stopPtT != ptT);

        //    SkASSERT(0);
        return null;
    }

    public SkOpGlobalState GlobalState()
    {
        return Contour().GlobalState();
    }

    public void Init(SkOpSpanBase span, double t, PathPoint pt, bool duplicate)
    {
        T = t;
        Pt = pt;
        _span = span;
        _next = this;
        _duplicatePt = duplicate;
        _deleted = false;
        _coincident = false;
    }

    public bool OnEnd()
    {
        SkOpSpanBase span = Span();
        if (span.PtT() != this)
        {
            return false;
        }

        SkOpSegment segment = Segment();
        return span == segment.Head() || span == segment.Tail();
    }

    public bool PtAlreadySeen(SkOpPtT check)
    {
        while (this != check)
        {
            if (Pt == check.Pt)
            {
                return true;
            }

            check = check._next;
        }

        return false;
    }

    public SkOpPtT Prev()
    {
        SkOpPtT result = this;
        SkOpPtT next = this;
        while ((next = next._next) != this)
        {
            result = next;
        }

        return result;
    }

    public SkOpSegment Segment()
    {
        return Span().Segment();
    }

    public void SetDeleted()
    {
        _deleted = true;
    }
}

internal class SkOpSpanBase
{
    // no direct access to internals to avoid treating a span base as a span
    protected readonly SkOpPtT _ptT = new SkOpPtT(); // list of points and t values associated with the start
    protected SkOpSegment _segment = null!; // segment that contains this span
    protected SkOpSpanBase _coinEnd = null!; // linked list of coincident spans that end here (may point to itself)
    protected SkOpAngle? _fromAngle; // points to next angle from span start to end
    protected SkOpSpan? _prev; // previous intersection point
    protected int _spanAdds; // number of times intersections have been added to span
    protected bool _aligned;
    protected bool _chased; // set after span has been added to chase array

    public void BumpSpanAdds()
    {
        ++_spanAdds;
    }

    public bool Chased()
    {
        return _chased;
    }

    public SkOpSpanBase CoinEnd()
    {
        return _coinEnd;
    }

    public bool ContainsCoinEnd(SkOpSpanBase coin)
    {
        SkOpSpanBase next = this;
        while ((next = next._coinEnd) != this)
        {
            if (next == coin)
            {
                return true;
            }
        }

        return false;
    }

    public bool Deleted()
    {
        return _ptT.Deleted();
    }

    public bool Final()
    {
        return _ptT.T == 1;
    }

    public SkOpAngle? FromAngle()
    {
        return _fromAngle;
    }

    // Please keep this in sync with debugInsertCoinEnd()
    public void InsertCoinEnd(SkOpSpanBase coin)
    {
        if (ContainsCoinEnd(coin))
        {
            return;
        }

        SkOpSpanBase coinNext = coin._coinEnd;
        coin._coinEnd = _coinEnd;
        _coinEnd = coinNext;
    }

    public SkOpSpan? Prev()
    {
        return _prev;
    }

    public PathPoint Pt()
    {
        return _ptT.Pt;
    }

    public SkOpPtT PtT()
    {
        return _ptT;
    }

    public SkOpSegment Segment()
    {
        return _segment;
    }

    public void SetAligned()
    {
        _aligned = true;
    }

    public void SetChased(bool chased)
    {
        _chased = chased;
    }

    public void SetFromAngle(SkOpAngle? angle)
    {
        _fromAngle = angle;
    }

    public void SetPrev(SkOpSpan? prev)
    {
        _prev = prev;
    }

    public bool Simple()
    {
        return _ptT.Next().Next() == _ptT;
    }

    public int SpanAddsCount()
    {
        return _spanAdds;
    }

    public SkOpSpan Starter(SkOpSpanBase end)
    {
        SkOpSpanBase result = T() < end.T() ? this : end;
        return result.UpCast();
    }

    public SkOpSpan Starter(ref SkOpSpanBase endPtr)
    {
        SkOpSpanBase end = endPtr;
        SkOpSpanBase result;
        if (T() < end.T())
        {
            result = this;
        }
        else
        {
            result = end;
            endPtr = this;
        }

        return result.UpCast();
    }

    public int Step(SkOpSpanBase end)
    {
        return T() < end.T() ? 1 : -1;
    }

    public double T()
    {
        return _ptT.T;
    }

    public void Unaligned()
    {
        _aligned = false;
    }

    public SkOpSpan UpCast()
    {
        return (SkOpSpan)this;
    }

    public SkOpSpan? UpCastable()
    {
        return Final() ? null : UpCast();
    }

    // SkOpSpan.cpp

    public bool AddOpp(SkOpSpanBase opp)
    {
        SkOpPtT? oppPrev = PtT().OppPrev(opp.PtT());
        if (oppPrev == null)
        {
            return true;
        }

        if (!MergeMatches(opp))
        {
            return false;
        }

        PtT().AddOpp(opp.PtT(), oppPrev);
        CheckForCollapsedCoincidence();
        return true;
    }

    public SkOpSpanBaseCollapsed Collapsed(double s, double e)
    {
        SkOpPtT start = _ptT;
        SkOpPtT? startNext = null;
        SkOpPtT walk = start;
        double min = walk.T;
        double max = min;
        SkOpSegment segment = Segment();
        int safetyNet = 100000;
        while ((walk = walk.Next()) != start)
        {
            if (--safetyNet == 0)
            {
                return SkOpSpanBaseCollapsed.Error;
            }

            if (walk == startNext)
            {
                return SkOpSpanBaseCollapsed.Error;
            }

            if (walk.Segment() != segment)
            {
                continue;
            }

            min = SkPathOps.StdMin(min, walk.T);
            max = SkPathOps.StdMax(max, walk.T);
            if (SkPathOps.Between(min, s, max) && SkPathOps.Between(min, e, max))
            {
                return SkOpSpanBaseCollapsed.Yes;
            }

            startNext = start.Next();
        }

        return SkOpSpanBaseCollapsed.No;
    }

    public bool Contains(SkOpSpanBase span)
    {
        SkOpPtT start = _ptT;
        SkOpPtT check = span._ptT;
        SkOpPtT walk = start;
        while ((walk = walk.Next()) != start)
        {
            if (walk == check)
            {
                return true;
            }
        }

        return false;
    }

    public SkOpPtT? Contains(SkOpSegment segment)
    {
        SkOpPtT start = _ptT;
        SkOpPtT walk = start;
        while ((walk = walk.Next()) != start)
        {
            if (walk.Deleted())
            {
                continue;
            }

            if (walk.Segment() == segment && walk.Span().PtT() == walk)
            {
                return walk;
            }
        }

        return null;
    }

    public bool ContainsCoinEnd(SkOpSegment segment)
    {
        SkOpSpanBase next = this;
        while ((next = next._coinEnd) != this)
        {
            if (next.Segment() == segment)
            {
                return true;
            }
        }

        return false;
    }

    public SkOpContour Contour()
    {
        return Segment().Contour();
    }

    public SkOpGlobalState GlobalState()
    {
        return Contour().GlobalState();
    }

    public void InitBase(SkOpSegment segment, SkOpSpan? prev, double t, PathPoint pt)
    {
        _segment = segment;
        _ptT.Init(this, t, pt, false);
        _coinEnd = this;
        _fromAngle = null;
        _prev = prev;
        _spanAdds = 0;
        _aligned = true;
        _chased = false;
    }

    // this pair of spans share a common t value or point; merge them and eliminate duplicates
    // this does not compute the best t or pt value; this merely moves all data into a single list
    public void Merge(SkOpSpan span)
    {
        SkOpPtT spanPtT = span.PtT();
        span.Release(PtT());
        if (Contains(span))
        {
            return; // merge is already in the ptT loop
        }

        SkOpPtT remainder = spanPtT.Next();
        PtT().Insert(spanPtT);
        while (remainder != spanPtT)
        {
            SkOpPtT next = remainder.Next();
            SkOpPtT compare = spanPtT.Next();
            while (compare != spanPtT)
            {
                SkOpPtT nextC = compare.Next();
                if (nextC.Span() == remainder.Span() && nextC.T == remainder.T)
                {
                    goto tryNextRemainder;
                }

                compare = nextC;
            }

            spanPtT.Insert(remainder);
        tryNextRemainder:
            remainder = next;
        }

        _spanAdds += span._spanAdds;
    }

    // please keep in sync with debugCheckForCollapsedCoincidence()
    public void CheckForCollapsedCoincidence()
    {
        SkOpCoincidence coins = GlobalState().Coincidence()!;
        if (coins.IsEmpty())
        {
            return;
        }

        // the insert above may have put both ends of a coincident run in the same span
        // for each coincident ptT in loop; see if its opposite in is also in the loop
        // this implementation is the motivation for marking that a ptT is referenced by a coincident span
        SkOpPtT head = PtT();
        SkOpPtT test = head;
        do
        {
            if (!test.Coincident())
            {
                continue;
            }

            coins.MarkCollapsed(test);
        }
        while ((test = test.Next()) != head);

        coins.ReleaseDeleted();
    }

    // please keep in sync with debugMergeMatches()
    // Look to see if pt-t linked list contains same segment more than once
    // if so, and if each pt-t is directly pointed to by spans in that segment,
    // merge them
    // keep the points, but remove spans so that the segment doesn't have 2 or more
    // spans pointing to the same pt-t loop at different loop elements
    public bool MergeMatches(SkOpSpanBase opp)
    {
        SkOpPtT test = _ptT;
        SkOpPtT testNext;
        SkOpPtT stop = test;
        int safetyHatch = 1000000;
        do
        {
            if (--safetyHatch == 0)
            {
                return false;
            }

            testNext = test.Next();
            if (test.Deleted())
            {
                continue;
            }

            SkOpSpanBase testBase = test.Span();
            SkOpSegment segment = test.Segment();
            if (segment.Done())
            {
                continue;
            }

            SkOpPtT inner = opp.PtT();
            SkOpPtT innerStop = inner;
            do
            {
                if (inner.Segment() != segment)
                {
                    continue;
                }

                if (inner.Deleted())
                {
                    continue;
                }

                SkOpSpanBase innerBase = inner.Span();
                // when the intersection is first detected, the span base is marked if there are
                // more than one point in the intersection.
                if (!SkPathOps.ZeroOrOne(inner.T))
                {
                    innerBase.UpCast().Release(test);
                }
                else
                {
                    if (!SkPathOps.ZeroOrOne(test.T))
                    {
                        testBase.UpCast().Release(inner);
                    }
                    else
                    {
                        segment.MarkAllDone(); // mark segment as collapsed
                        test.SetDeleted();
                        inner.SetDeleted();
                    }
                }

                break;
            }
            while ((inner = inner.Next()) != innerStop);
        }
        while ((test = testNext) != stop);

        CheckForCollapsedCoincidence();
        return true;
    }
}

internal sealed partial class SkOpSpan : SkOpSpanBase
{
    // no direct access to internals to avoid treating a span base as a span
    private SkOpSpan _coincident = null!; // linked list of spans coincident with this one (may point to itself)
    private SkOpAngle? _toAngle; // points to next angle from span start to end
    private SkOpSpanBase _next = null!; // next intersection point
    private int _windSum; // accumulated from contours surrounding this one.
    private int _oppSum; // for binary operators: the opposite winding sum
    private int _windValue; // 0 == canceled; 1 == normal; >1 == coincident
    private int _oppValue; // normally 0 -- when binary coincident edges combine, opp value goes here
    // Read by sortableTop (SkPathOpsWinding.cpp), which a later layer ports, so it is not private yet.
    internal int _topTTry; // specifies direction and t value to try next
    private bool _done; // if set, this span to next higher T has been processed
    private bool _alreadyAdded;

    public bool AlreadyAdded()
    {
        if (_alreadyAdded)
        {
            return true;
        }

        return false;
    }

    public bool ClearCoincident()
    {
        if (_coincident == this)
        {
            return false;
        }

        _coincident = this;
        return true;
    }

    public bool ContainsCoincidence(SkOpSpan coin)
    {
        SkOpSpan next = this;
        while ((next = next._coincident) != this)
        {
            if (next == coin)
            {
                return true;
            }
        }

        return false;
    }

    public bool Done()
    {
        return _done;
    }

    // Please keep this in sync with debugInsertCoincidence()
    public void InsertCoincidence(SkOpSpan coin)
    {
        if (ContainsCoincidence(coin))
        {
            return;
        }

        SkOpSpan coinNext = coin._coincident;
        coin._coincident = _coincident;
        _coincident = coinNext;
    }

    public bool IsCanceled()
    {
        return _windValue == 0 && _oppValue == 0;
    }

    public bool IsCoincident()
    {
        return _coincident != this;
    }

    public void MarkAdded()
    {
        _alreadyAdded = true;
    }

    public SkOpSpanBase Next()
    {
        return _next;
    }

    public int OppSum()
    {
        return _oppSum;
    }

    public int OppValue()
    {
        return _oppValue;
    }

    public void SetDone(bool done)
    {
        _done = done;
    }

    public void SetNext(SkOpSpanBase nextT)
    {
        _next = nextT;
    }

    public void SetOppValue(int oppValue)
    {
        _oppValue = oppValue;
    }

    public void SetToAngle(SkOpAngle? angle)
    {
        _toAngle = angle;
    }

    public void SetWindValue(int windValue)
    {
        _windValue = windValue;
    }

    public SkOpAngle? ToAngle()
    {
        return _toAngle;
    }

    public int WindSum()
    {
        return _windSum;
    }

    public int WindValue()
    {
        return _windValue;
    }

    // SkOpSpan.cpp

    public int ComputeWindSum()
    {
        SkOpGlobalState globals = GlobalState();
        SkOpContour contourHead = globals.ContourHead();
        int windTry = 0;
        while (!SortableTop(contourHead) && ++windTry < SkOpGlobalState.KMaxWindingTries)
        {
        }

        return WindSum();
    }

    public bool ContainsCoincidence(SkOpSegment segment)
    {
        SkOpSpan next = _coincident;
        do
        {
            if (next.Segment() == segment)
            {
                return true;
            }
        }
        while ((next = next._coincident) != this);

        return false;
    }

    public void Init(SkOpSegment segment, SkOpSpan? prev, double t, PathPoint pt)
    {
        InitBase(segment, prev, t, pt);
        _coincident = this;
        _toAngle = null;
        _windSum = _oppSum = SkPathOps.SkMinS32;
        _windValue = 1;
        _oppValue = 0;
        _topTTry = 0;
        _chased = _done = false;
        segment.BumpCount();
        _alreadyAdded = false;
    }

    // Please keep this in sync with debugInsertCoincidence()
    public bool InsertCoincidence(SkOpSegment segment, bool flipped, bool ordered)
    {
        if (ContainsCoincidence(segment))
        {
            return true;
        }

        SkOpPtT next = _ptT;
        while ((next = next.Next()) != _ptT)
        {
            if (next.Segment() == segment)
            {
                SkOpSpan? span;
                SkOpSpanBase @base = next.Span();
                if (!ordered)
                {
                    SkOpPtT? spanEndPtT = _next.Contains(segment);
                    if (spanEndPtT == null)
                    {
                        return false;
                    }

                    SkOpSpanBase spanEnd = spanEndPtT.Span();
                    SkOpPtT start = @base.PtT().Starter(spanEnd.PtT());
                    if (start.Span().UpCastable() == null)
                    {
                        return false;
                    }

                    span = start.Span().UpCast();
                }
                else if (flipped)
                {
                    span = @base.Prev();
                    if (span == null)
                    {
                        return false;
                    }
                }
                else
                {
                    if (@base.UpCastable() == null)
                    {
                        return false;
                    }

                    span = @base.UpCast();
                }

                InsertCoincidence(span);
                return true;
            }
        }

        return true;
    }

    public void Release(SkOpPtT kept)
    {
        SkOpSpan prev = Prev()!;
        SkOpSpanBase next = Next();
        prev.SetNext(next);
        next.SetPrev(prev);
        Segment().Release(this);
        SkOpCoincidence? coincidence = GlobalState().Coincidence();
        if (coincidence != null)
        {
            coincidence.FixUp(PtT(), kept);
        }

        PtT().SetDeleted();
        SkOpPtT stopPtT = PtT();
        SkOpPtT testPtT = stopPtT;
        SkOpSpanBase keptSpan = kept.Span();
        do
        {
            if (this == testPtT.Span())
            {
                testPtT.SetSpan(keptSpan);
            }
        }
        while ((testPtT = testPtT.Next()) != stopPtT);
    }

    public void SetOppSum(int oppSum)
    {
        if (_oppSum != SkPathOps.SkMinS32 && _oppSum != oppSum)
        {
            GlobalState().SetWindingFailed();
            return;
        }

        _oppSum = oppSum;
    }

    public void SetWindSum(int windSum)
    {
        if (_windSum != SkPathOps.SkMinS32 && _windSum != windSum)
        {
            GlobalState().SetWindingFailed();
            return;
        }

        _windSum = windSum;
    }
}
