using System.Runtime.InteropServices;

namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkOpCoincidence.h and SkOpCoincidence.cpp at chrome/m119 (the
// Skia inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double
// types are kept. SkCoincidentSpans and SkOpCoincidence are classes (convention 6); arena make<SkCoincidentSpans>()
// plus init() (which zeroes the record) is `new SkCoincidentSpans()`. The const_cast "Writable" accessors return the
// same references as their const twins. correctOneEnd's member-function pointers are delegates bound to the span
// pair. restoreHead's SkCoincidentSpans** walk is spelled with a trailing-node reference, which unlinks the same
// records in the same order. SkTDArray<SkCoincidentSpans*> is a List. The DEBUG_COIN parameters, SkDEBUGPARAMS
// (over1e/over2e), debug* and dump* are omitted, as are fContinue, fSpanDeleted, fPtAllocated, fCoinExtended and
// fSpanMerged, which the constructor clears and nothing reads. FAIL_IF is `if (cond) return false;`.

internal sealed class SkCoincidentSpans
{
    private SkCoincidentSpans? _next;
    private SkOpPtT _coinPtTStart = null!;
    private SkOpPtT _coinPtTEnd = null!;
    private SkOpPtT _oppPtTStart = null!;
    private SkOpPtT _oppPtTEnd = null!;

    // These return non-const pointers so that, as copies, they can be added
    // to a new span pair
    public SkOpPtT CoinPtTEndWritable()
    {
        return _coinPtTEnd;
    }

    public SkOpPtT CoinPtTStartWritable()
    {
        return _coinPtTStart;
    }

    public bool Flipped()
    {
        return _oppPtTStart.T > _oppPtTEnd.T;
    }

    public SkCoincidentSpans? Next()
    {
        return _next;
    }

    // These return non-const pointers so that, as copies, they can be added
    // to a new span pair
    public SkOpPtT OppPtTStartWritable()
    {
        return _oppPtTStart;
    }

    public SkOpPtT OppPtTEndWritable()
    {
        return _oppPtTEnd;
    }

    public void SetCoinPtTEnd(SkOpPtT ptT)
    {
        _coinPtTEnd = ptT;
        ptT.SetCoincident();
    }

    public void SetCoinPtTStart(SkOpPtT ptT)
    {
        _coinPtTStart = ptT;
        ptT.SetCoincident();
    }

    public void SetEnds(SkOpPtT coinPtTEnd, SkOpPtT oppPtTEnd)
    {
        SetCoinPtTEnd(coinPtTEnd);
        SetOppPtTEnd(oppPtTEnd);
    }

    public void SetOppPtTEnd(SkOpPtT ptT)
    {
        _oppPtTEnd = ptT;
        ptT.SetCoincident();
    }

    public void SetOppPtTStart(SkOpPtT ptT)
    {
        _oppPtTStart = ptT;
        ptT.SetCoincident();
    }

    public void SetStarts(SkOpPtT coinPtTStart, SkOpPtT oppPtTStart)
    {
        SetCoinPtTStart(coinPtTStart);
        SetOppPtTStart(oppPtTStart);
    }

    public void SetNext(SkCoincidentSpans? next)
    {
        _next = next;
    }

    // SkOpCoincidence.cpp

    // returns true if coincident span's start and end are the same
    public bool Collapsed(SkOpPtT test)
    {
        return (_coinPtTStart == test && _coinPtTEnd.Contains(test))
            || (_coinPtTEnd == test && _coinPtTStart.Contains(test))
            || (_oppPtTStart == test && _oppPtTEnd.Contains(test))
            || (_oppPtTEnd == test && _oppPtTStart.Contains(test));
    }

    // out of line since this function is referenced by address
    public SkOpPtT CoinPtTEnd()
    {
        return _coinPtTEnd;
    }

    // out of line since this function is referenced by address
    public SkOpPtT CoinPtTStart()
    {
        return _coinPtTStart;
    }

    // sets the span's end to the ptT referenced by the previous-next
    public void CorrectOneEnd(Func<SkOpPtT> getEnd, Action<SkOpPtT> setEnd)
    {
        SkOpPtT origPtT = getEnd();
        SkOpSpanBase origSpan = origPtT.Span();
        SkOpSpan? prev = origSpan.Prev();
        SkOpPtT testPtT = prev != null
            ? prev.Next().PtT()
            : origSpan.UpCast().Next().Prev()!.PtT();
        if (origPtT != testPtT)
        {
            setEnd(testPtT);
        }
    }

    /* Please keep this in sync with debugCorrectEnds */
    // FIXME: member pointers have fallen out of favor and can be replaced with
    // an alternative approach.
    // makes all span ends agree with the segment's spans that define them
    public void CorrectEnds()
    {
        CorrectOneEnd(CoinPtTStart, SetCoinPtTStart);
        CorrectOneEnd(CoinPtTEnd, SetCoinPtTEnd);
        CorrectOneEnd(OppPtTStart, SetOppPtTStart);
        CorrectOneEnd(OppPtTEnd, SetOppPtTEnd);
    }

    /* Please keep this in sync with debugExpand */
    // expand the range by checking adjacent spans for coincidence
    public bool Expand()
    {
        bool expanded = false;
        SkOpSegment segment = CoinPtTStart().Segment();
        SkOpSegment oppSegment = OppPtTStart().Segment();
        do
        {
            SkOpSpan start = CoinPtTStart().Span().UpCast();
            SkOpSpan? prev = start.Prev();
            SkOpPtT? oppPtT;
            if (prev == null || (oppPtT = prev.Contains(oppSegment)) == null)
            {
                break;
            }

            double midT = (prev.T() + start.T()) / 2;
            if (!segment.IsClose(midT, oppSegment))
            {
                break;
            }

            SetStarts(prev.PtT(), oppPtT);
            expanded = true;
        }
        while (true);

        do
        {
            SkOpSpanBase end = CoinPtTEnd().Span();
            SkOpSpanBase? next = end.Final() ? null : end.UpCast().Next();
            if (next != null && next.Deleted())
            {
                break;
            }

            SkOpPtT? oppPtT;
            if (next == null || (oppPtT = next.Contains(oppSegment)) == null)
            {
                break;
            }

            double midT = (end.T() + next.T()) / 2;
            if (!segment.IsClose(midT, oppSegment))
            {
                break;
            }

            SetEnds(next.PtT(), oppPtT);
            expanded = true;
        }
        while (true);

        return expanded;
    }

    // increase the range of this span
    public bool Extend(SkOpPtT coinPtTStart, SkOpPtT coinPtTEnd, SkOpPtT oppPtTStart, SkOpPtT oppPtTEnd)
    {
        bool result = false;
        if (_coinPtTStart.T > coinPtTStart.T || (Flipped()
                ? _oppPtTStart.T < oppPtTStart.T
                : _oppPtTStart.T > oppPtTStart.T))
        {
            SetStarts(coinPtTStart, oppPtTStart);
            result = true;
        }

        if (_coinPtTEnd.T < coinPtTEnd.T || (Flipped()
                ? _oppPtTEnd.T > oppPtTEnd.T
                : _oppPtTEnd.T < oppPtTEnd.T))
        {
            SetEnds(coinPtTEnd, oppPtTEnd);
            result = true;
        }

        return result;
    }

    // set the range of this span
    public void Set(
        SkCoincidentSpans? next,
        SkOpPtT coinPtTStart,
        SkOpPtT coinPtTEnd,
        SkOpPtT oppPtTStart,
        SkOpPtT oppPtTEnd)
    {
        _next = next;
        SetStarts(coinPtTStart, oppPtTStart);
        SetEnds(coinPtTEnd, oppPtTEnd);
    }

    // returns true if both points are inside this
    public bool Contains(SkOpPtT s, SkOpPtT e)
    {
        if (s.T > e.T)
        {
            (s, e) = (e, s);
        }

        if (s.Segment() == _coinPtTStart.Segment())
        {
            return _coinPtTStart.T <= s.T && e.T <= _coinPtTEnd.T;
        }
        else
        {
            double oppTs = _oppPtTStart.T;
            double oppTe = _oppPtTEnd.T;
            if (oppTs > oppTe)
            {
                (oppTs, oppTe) = (oppTe, oppTs);
            }

            return oppTs <= s.T && e.T <= oppTe;
        }
    }

    // out of line since this function is referenced by address
    public SkOpPtT OppPtTStart()
    {
        return _oppPtTStart;
    }

    // out of line since this function is referenced by address
    public SkOpPtT OppPtTEnd()
    {
        return _oppPtTEnd;
    }

    // A coincident span is unordered if the pairs of points in the main and opposite curves'
    // t values do not ascend or descend. For instance, if a tightly arced quadratic is
    // coincident with another curve, it may intersect it out of order.
    public bool Ordered(out bool result)
    {
        SkOpSpanBase start = CoinPtTStart().Span();
        SkOpSpanBase end = CoinPtTEnd().Span();
        SkOpSpanBase next = start.UpCast().Next();
        if (next == end)
        {
            result = true;
            return true;
        }

        bool flipped = Flipped();
        SkOpSegment oppSeg = OppPtTStart().Segment();
        double oppLastT = _oppPtTStart.T;
        do
        {
            SkOpPtT? opp = next.Contains(oppSeg);
            if (opp == null)
            {
                //            SkOPOBJASSERT(start, 0);  // may assert if coincident span isn't fully processed
                result = false;
                return false;
            }

            if ((oppLastT > opp.T) != flipped)
            {
                result = false;
                return true;
            }

            oppLastT = opp.T;
            if (next == end)
            {
                break;
            }

            if (next.UpCastable() == null)
            {
                result = false;
                return true;
            }

            next = next.UpCast().Next();
        }
        while (true);

        result = true;
        return true;
    }
}

internal sealed class SkOpCoincidence
{
    private SkCoincidentSpans? _head;
    private SkCoincidentSpans? _top;
    private readonly SkOpGlobalState _globalState;

    public SkOpCoincidence(SkOpGlobalState globalState)
    {
        _head = null;
        _top = null;
        _globalState = globalState;
        globalState.SetCoincidence(this);
    }

    public SkOpGlobalState GlobalState()
    {
        return _globalState;
    }

    public bool IsEmpty()
    {
        return _head == null && _top == null;
    }

    public static bool Ordered(SkOpPtT coinPtTStart, SkOpPtT oppPtTStart)
    {
        return Ordered(coinPtTStart.Segment(), oppPtTStart.Segment());
    }

    // SkOpCoincidence.cpp

    // if there is an existing pair that overlaps the addition, extend it
    public bool Extend(SkOpPtT coinPtTStart, SkOpPtT coinPtTEnd, SkOpPtT oppPtTStart, SkOpPtT oppPtTEnd)
    {
        SkCoincidentSpans? test = _head;
        if (test == null)
        {
            return false;
        }

        SkOpSegment coinSeg = coinPtTStart.Segment();
        SkOpSegment oppSeg = oppPtTStart.Segment();
        if (!Ordered(coinPtTStart, oppPtTStart))
        {
            (coinSeg, oppSeg) = (oppSeg, coinSeg);
            (coinPtTStart, oppPtTStart) = (oppPtTStart, coinPtTStart);
            (coinPtTEnd, oppPtTEnd) = (oppPtTEnd, coinPtTEnd);
            if (coinPtTStart.T > coinPtTEnd.T)
            {
                (coinPtTStart, coinPtTEnd) = (coinPtTEnd, coinPtTStart);
                (oppPtTStart, oppPtTEnd) = (oppPtTEnd, oppPtTStart);
            }
        }

        double oppMinT = SkPathOps.StdMin(oppPtTStart.T, oppPtTEnd.T);
        do
        {
            if (coinSeg != test.CoinPtTStart().Segment())
            {
                continue;
            }

            if (oppSeg != test.OppPtTStart().Segment())
            {
                continue;
            }

            double oTestMinT = SkPathOps.StdMin(test.OppPtTStart().T, test.OppPtTEnd().T);
            double oTestMaxT = SkPathOps.StdMax(test.OppPtTStart().T, test.OppPtTEnd().T);
            // if debug check triggers, caller failed to check if extended already exists
            if ((test.CoinPtTStart().T <= coinPtTEnd.T
                    && coinPtTStart.T <= test.CoinPtTEnd().T)
                || (oTestMinT <= oTestMaxT && oppMinT <= oTestMaxT))
            {
                _ = test.Extend(coinPtTStart, coinPtTEnd, oppPtTStart, oppPtTEnd);
                return true;
            }
        }
        while ((test = test.Next()) != null);

        return false;
    }

    // adds a new coincident pair
    public void Add(SkOpPtT coinPtTStart, SkOpPtT coinPtTEnd, SkOpPtT oppPtTStart, SkOpPtT oppPtTEnd)
    {
        // OPTIMIZE: caller should have already sorted
        if (!Ordered(coinPtTStart, oppPtTStart))
        {
            if (oppPtTStart.T < oppPtTEnd.T)
            {
                Add(oppPtTStart, oppPtTEnd, coinPtTStart, coinPtTEnd);
            }
            else
            {
                Add(oppPtTEnd, oppPtTStart, coinPtTEnd, coinPtTStart);
            }

            return;
        }

        // choose the ptT at the front of the list to track
        coinPtTStart = coinPtTStart.Span().PtT();
        coinPtTEnd = coinPtTEnd.Span().PtT();
        oppPtTStart = oppPtTStart.Span().PtT();
        oppPtTEnd = oppPtTEnd.Span().PtT();
        SkCoincidentSpans coinRec = new SkCoincidentSpans();
        coinRec.Set(_head, coinPtTStart, coinPtTEnd, oppPtTStart, oppPtTEnd);
        _head = coinRec;
    }

    // description below
    private bool AddEndMovedSpans(SkOpSpan @base, SkOpSpanBase testSpan)
    {
        SkOpPtT testPtT = testSpan.PtT();
        SkOpPtT stopPtT = testPtT;
        SkOpSegment baseSeg = @base.Segment();
        int escapeHatch = 100000; // this is 100 times larger than the debugLoopLimit test
        while ((testPtT = testPtT.Next()) != stopPtT)
        {
            if (--escapeHatch <= 0)
            {
                return false; // if triggered (likely by a fuzz-generated test) too complex to succeed
            }

            SkOpSegment testSeg = testPtT.Segment();
            if (testPtT.Deleted())
            {
                continue;
            }

            if (testSeg == baseSeg)
            {
                continue;
            }

            if (testPtT.Span().PtT() != testPtT)
            {
                continue;
            }

            if (Contains(baseSeg, testSeg, testPtT.T))
            {
                continue;
            }

            // intersect perp with base->ptT() with testPtT->segment()
            SkDVector dxdy = baseSeg.DSlopeAtT(@base.T());
            PathPoint pt = @base.Pt();
            SkDLine ray = new SkDLine(
                new SkDPoint(pt.X, pt.Y),
                new SkDPoint(pt.X + dxdy.Y, pt.Y - dxdy.X));
            SkIntersections i = new SkIntersections();
            SkPathOps.CurveIntersectRay[(int)testSeg.Verb()]!(testSeg.Pts(), testSeg.Weight(), ray, i);
            for (int index = 0; index < i.Used(); ++index)
            {
                double t = i[0][index];
                if (!SkPathOps.Between(0, t, 1))
                {
                    continue;
                }

                SkDPoint oppPt = i.Pt(index);
                if (!oppPt.ApproximatelyEqual(pt))
                {
                    continue;
                }

                SkOpSegment writableSeg = testSeg;
                SkOpPtT oppStart = writableSeg.AddT(t)!;
                if (oppStart == testPtT)
                {
                    continue;
                }

                SkOpSpan writableBase = @base;
                _ = oppStart.Span().AddOpp(writableBase);
                if (oppStart.Deleted())
                {
                    continue;
                }

                SkOpSegment coinSeg = @base.Segment();
                SkOpSegment oppSeg = oppStart.Segment();
                double coinTs, coinTe, oppTs, oppTe;
                if (Ordered(coinSeg, oppSeg))
                {
                    coinTs = @base.T();
                    coinTe = testSpan.T();
                    oppTs = oppStart.T;
                    oppTe = testPtT.T;
                }
                else
                {
                    (coinSeg, oppSeg) = (oppSeg, coinSeg);
                    coinTs = oppStart.T;
                    coinTe = testPtT.T;
                    oppTs = @base.T();
                    oppTe = testSpan.T();
                }

                if (coinTs > coinTe)
                {
                    (coinTs, coinTe) = (coinTe, coinTs);
                    (oppTs, oppTe) = (oppTe, oppTs);
                }

                bool added = false;
                if (!AddOrOverlap(coinSeg, oppSeg, coinTs, coinTe, oppTs, oppTe, ref added))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // description below
    private bool AddEndMovedSpans(SkOpPtT ptT)
    {
        if (ptT.Span().UpCastable() == null)
        {
            return false;
        }

        SkOpSpan @base = ptT.Span().UpCast();
        SkOpSpan? prev = @base.Prev();
        if (prev == null)
        {
            return false;
        }

        if (!prev.IsCanceled())
        {
            if (!AddEndMovedSpans(@base, @base.Prev()!))
            {
                return false;
            }
        }

        if (!@base.IsCanceled())
        {
            if (!AddEndMovedSpans(@base, @base.Next()))
            {
                return false;
            }
        }

        return true;
    }

    /*  If A is coincident with B and B includes an endpoint, and A's matching point
        is not the endpoint (i.e., there's an implied line connecting B-end and A)
        then assume that the same implied line may intersect another curve close to B.
        Since we only care about coincidence that was undetected, look at the
        ptT list on B-segment adjacent to the B-end/A ptT loop (not in the loop, but
        next door) and see if the A matching point is close enough to form another
        coincident pair. If so, check for a new coincident span between B-end/A ptT loop
        and the adjacent ptT loop.
    */
    public bool AddEndMovedSpans()
    {
        SkCoincidentSpans? span = _head;
        if (span == null)
        {
            return true;
        }

        _top = span;
        _head = null;
        do
        {
            if (span.CoinPtTStart().Pt != span.OppPtTStart().Pt)
            {
                if (1 == span.CoinPtTStart().T)
                {
                    return false;
                }

                bool onEnd = span.CoinPtTStart().T == 0;
                bool oOnEnd = SkPathOps.ZeroOrOne(span.OppPtTStart().T);
                if (onEnd)
                {
                    if (!oOnEnd)
                    {
                        // if both are on end, any nearby intersect was already found
                        if (!AddEndMovedSpans(span.OppPtTStart()))
                        {
                            return false;
                        }
                    }
                }
                else if (oOnEnd)
                {
                    if (!AddEndMovedSpans(span.CoinPtTStart()))
                    {
                        return false;
                    }
                }
            }

            if (span.CoinPtTEnd().Pt != span.OppPtTEnd().Pt)
            {
                bool onEnd = span.CoinPtTEnd().T == 1;
                bool oOnEnd = SkPathOps.ZeroOrOne(span.OppPtTEnd().T);
                if (onEnd)
                {
                    if (!oOnEnd)
                    {
                        if (!AddEndMovedSpans(span.OppPtTEnd()))
                        {
                            return false;
                        }
                    }
                }
                else if (oOnEnd)
                {
                    if (!AddEndMovedSpans(span.CoinPtTEnd()))
                    {
                        return false;
                    }
                }
            }
        }
        while ((span = span.Next()) != null);

        RestoreHead();
        return true;
    }

    /* Please keep this in sync with debugAddExpanded */
    // for each coincident pair, match the spans
    // if the spans don't match, add the missing pt to the segment and loop it in the opposite span
    public bool AddExpanded()
    {
        SkCoincidentSpans? coin = _head;
        if (coin == null)
        {
            return true;
        }

        do
        {
            SkOpPtT startPtT = coin.CoinPtTStart();
            SkOpPtT oStartPtT = coin.OppPtTStart();
            double priorT = startPtT.T;
            double oPriorT = oStartPtT.T;
            if (!startPtT.Contains(oStartPtT))
            {
                return false;
            }

            SkOpSpanBase start = startPtT.Span();
            SkOpSpanBase oStart = oStartPtT.Span();
            SkOpSpanBase end = coin.CoinPtTEnd().Span();
            SkOpSpanBase oEnd = coin.OppPtTEnd().Span();
            if (oEnd.Deleted())
            {
                return false;
            }

            if (start.UpCastable() == null)
            {
                return false;
            }

            SkOpSpanBase test = start.UpCast().Next();
            if (!coin.Flipped() && oStart.UpCastable() == null)
            {
                return false;
            }

            SkOpSpanBase? oTest = coin.Flipped() ? oStart.Prev() : oStart.UpCast().Next();
            if (oTest == null)
            {
                return false;
            }

            SkOpSegment seg = start.Segment();
            SkOpSegment oSeg = oStart.Segment();
            while (test != end || oTest != oEnd)
            {
                SkOpPtT? containedOpp = test.PtT().Contains(oSeg);
                SkOpPtT? containedThis = oTest.PtT().Contains(seg);
                if (containedOpp == null || containedThis == null)
                {
                    // choose the ends, or the first common pt-t list shared by both
                    double nextT, oNextT;
                    if (containedOpp != null)
                    {
                        nextT = test.T();
                        oNextT = containedOpp.T;
                    }
                    else if (containedThis != null)
                    {
                        nextT = containedThis.T;
                        oNextT = oTest.T();
                    }
                    else
                    {
                        // iterate through until a pt-t list found that contains the other
                        SkOpSpanBase walk = test;
                        SkOpPtT? walkOpp;
                        do
                        {
                            if (walk.UpCastable() == null)
                            {
                                return false;
                            }

                            walk = walk.UpCast().Next();
                        }
                        while ((walkOpp = walk.PtT().Contains(oSeg)) == null
                            && walk != coin.CoinPtTEnd().Span());

                        if (walkOpp == null)
                        {
                            return false;
                        }

                        nextT = walk.T();
                        oNextT = walkOpp.T;
                    }

                    // use t ranges to guess which one is missing
                    double startRange = nextT - priorT;
                    if (startRange == 0)
                    {
                        return false;
                    }

                    double startPart = (test.T() - priorT) / startRange;
                    double oStartRange = oNextT - oPriorT;
                    if (oStartRange == 0)
                    {
                        return false;
                    }

                    double oStartPart = (oTest.T() - oPriorT) / oStartRange;
                    if (startPart == oStartPart)
                    {
                        return false;
                    }

                    bool addToOpp = containedOpp == null && containedThis == null
                        ? startPart < oStartPart
                        : containedThis != null;
                    bool startOver = false;
                    bool success = addToOpp
                        ? oSeg.AddExpanded(oPriorT + oStartRange * startPart, test, ref startOver)
                        : seg.AddExpanded(priorT + startRange * oStartPart, oTest, ref startOver);
                    if (!success)
                    {
                        return false;
                    }

                    if (startOver)
                    {
                        test = start;
                        oTest = oStart;
                    }

                    end = coin.CoinPtTEnd().Span();
                    oEnd = coin.OppPtTEnd().Span();
                }

                if (test != end)
                {
                    if (test.UpCastable() == null)
                    {
                        return false;
                    }

                    priorT = test.T();
                    test = test.UpCast().Next();
                }

                if (oTest != oEnd)
                {
                    oPriorT = oTest.T();
                    if (coin.Flipped())
                    {
                        oTest = oTest.Prev();
                    }
                    else
                    {
                        if (oTest.UpCastable() == null)
                        {
                            return false;
                        }

                        oTest = oTest.UpCast().Next();
                    }

                    if (oTest == null)
                    {
                        return false;
                    }
                }
            }
        }
        while ((coin = coin.Next()) != null);

        return true;
    }

    // given a t span, map the same range on the coincident span
    /*
    the curves may not scale linearly, so interpolation may only happen within known points
    remap over1s, over1e, cointPtTStart, coinPtTEnd to smallest range that captures over1s
    then repeat to capture over1e
    */
    private static double TRange(SkOpPtT overS, double t, SkOpSegment coinSeg)
    {
        SkOpSpanBase work = overS.Span();
        SkOpPtT? foundStart = null;
        SkOpPtT? foundEnd = null;
        SkOpPtT? coinStart = null;
        SkOpPtT? coinEnd = null;
        do
        {
            SkOpPtT? contained = work.Contains(coinSeg);
            if (contained == null)
            {
                if (work.Final())
                {
                    break;
                }

                continue;
            }

            if (work.T() <= t)
            {
                coinStart = contained;
                foundStart = work.PtT();
            }

            if (work.T() >= t)
            {
                coinEnd = contained;
                foundEnd = work.PtT();
                break;
            }
        }
        while ((work = work.UpCast().Next()) != null);

        if (coinStart == null || coinEnd == null)
        {
            return 1;
        }

        // while overS->fT <=t and overS contains coinSeg
        double denom = foundEnd!.T - foundStart!.T;
        double sRatio = denom != 0 ? (t - foundStart.T) / denom : 1;
        return coinStart.T + (coinEnd.T - coinStart.T) * sRatio;
    }

    // return true if span overlaps existing and needs to adjust the coincident list
    private bool CheckOverlap(
        SkCoincidentSpans? check,
        SkOpSegment coinSeg,
        SkOpSegment oppSeg,
        double coinTs,
        double coinTe,
        double oppTs,
        double oppTe,
        List<SkCoincidentSpans> overlaps)
    {
        if (!Ordered(coinSeg, oppSeg))
        {
            if (oppTs < oppTe)
            {
                return CheckOverlap(check, oppSeg, coinSeg, oppTs, oppTe, coinTs, coinTe, overlaps);
            }

            return CheckOverlap(check, oppSeg, coinSeg, oppTe, oppTs, coinTe, coinTs, overlaps);
        }

        bool swapOpp = oppTs > oppTe;
        if (swapOpp)
        {
            (oppTs, oppTe) = (oppTe, oppTs);
        }

        do
        {
            if (check!.CoinPtTStart().Segment() != coinSeg)
            {
                continue;
            }

            if (check.OppPtTStart().Segment() != oppSeg)
            {
                continue;
            }

            double checkTs = check.CoinPtTStart().T;
            double checkTe = check.CoinPtTEnd().T;
            bool coinOutside = coinTe < checkTs || coinTs > checkTe;
            double oCheckTs = check.OppPtTStart().T;
            double oCheckTe = check.OppPtTEnd().T;
            if (swapOpp)
            {
                if (oCheckTs <= oCheckTe)
                {
                    return false;
                }

                (oCheckTs, oCheckTe) = (oCheckTe, oCheckTs);
            }

            bool oppOutside = oppTe < oCheckTs || oppTs > oCheckTe;
            if (coinOutside && oppOutside)
            {
                continue;
            }

            bool coinInside = coinTe <= checkTe && coinTs >= checkTs;
            bool oppInside = oppTe <= oCheckTe && oppTs >= oCheckTs;
            if (coinInside && oppInside)
            {
                // already included, do nothing
                return false;
            }

            overlaps.Add(check); // partial overlap, extend existing entry
        }
        while ((check = check.Next()) != null);

        return true;
    }

    /* Please keep this in sync with debugAddIfMissing() */
    // note that over1s, over1e, over2s, over2e are ordered
    private bool AddIfMissing(
        SkOpPtT over1s,
        SkOpPtT over2s,
        double tStart,
        double tEnd,
        SkOpSegment coinSeg,
        SkOpSegment oppSeg,
        ref bool added)
    {
        double coinTs, coinTe, oppTs, oppTe;
        coinTs = TRange(over1s, tStart, coinSeg);
        coinTe = TRange(over1s, tEnd, coinSeg);
        SkOpSpanBaseCollapsed result = coinSeg.Collapsed(coinTs, coinTe);
        if (SkOpSpanBaseCollapsed.No != result)
        {
            return SkOpSpanBaseCollapsed.Yes == result;
        }

        oppTs = TRange(over2s, tStart, oppSeg);
        oppTe = TRange(over2s, tEnd, oppSeg);
        result = oppSeg.Collapsed(oppTs, oppTe);
        if (SkOpSpanBaseCollapsed.No != result)
        {
            return SkOpSpanBaseCollapsed.Yes == result;
        }

        if (coinTs > coinTe)
        {
            (coinTs, coinTe) = (coinTe, coinTs);
            (oppTs, oppTe) = (oppTe, oppTs);
        }

        _ = AddOrOverlap(coinSeg, oppSeg, coinTs, coinTe, oppTs, oppTe, ref added);
        return true;
    }

    /* Please keep this in sync with debugAddOrOverlap() */
    // If this is called by addEndMovedSpans(), a returned false propogates out to an abort.
    // If this is called by AddIfMissing(), a returned false indicates there was nothing to add
    private bool AddOrOverlap(
        SkOpSegment coinSeg,
        SkOpSegment oppSeg,
        double coinTs,
        double coinTe,
        double oppTs,
        double oppTe,
        ref bool added)
    {
        List<SkCoincidentSpans> overlaps = new List<SkCoincidentSpans>();
        if (_top == null)
        {
            return false;
        }

        if (!CheckOverlap(_top, coinSeg, oppSeg, coinTs, coinTe, oppTs, oppTe, overlaps))
        {
            return true;
        }

        if (_head != null && !CheckOverlap(_head, coinSeg, oppSeg, coinTs,
                coinTe, oppTs, oppTe, overlaps))
        {
            return true;
        }

        SkCoincidentSpans? overlap = overlaps.Count != 0 ? overlaps[0] : null;
        for (int index = 1; index < overlaps.Count; ++index)
        {
            // combine overlaps before continuing
            SkCoincidentSpans test = overlaps[index];
            if (overlap!.CoinPtTStart().T > test.CoinPtTStart().T)
            {
                overlap.SetCoinPtTStart(test.CoinPtTStart());
            }

            if (overlap.CoinPtTEnd().T < test.CoinPtTEnd().T)
            {
                overlap.SetCoinPtTEnd(test.CoinPtTEnd());
            }

            if (overlap.Flipped()
                    ? overlap.OppPtTStart().T < test.OppPtTStart().T
                    : overlap.OppPtTStart().T > test.OppPtTStart().T)
            {
                overlap.SetOppPtTStart(test.OppPtTStart());
            }

            if (overlap.Flipped()
                    ? overlap.OppPtTEnd().T > test.OppPtTEnd().T
                    : overlap.OppPtTEnd().T < test.OppPtTEnd().T)
            {
                overlap.SetOppPtTEnd(test.OppPtTEnd());
            }

            if (_head == null || !Release(_head, test))
            {
                _ = Release(_top, test);
            }
        }

        SkOpPtT? cs = coinSeg.Existing(coinTs, oppSeg);
        SkOpPtT? ce = coinSeg.Existing(coinTe, oppSeg);
        if (overlap != null && cs != null && ce != null && overlap.Contains(cs, ce))
        {
            return true;
        }

        if (cs == ce && cs != null)
        {
            return false;
        }

        SkOpPtT? os = oppSeg.Existing(oppTs, coinSeg);
        SkOpPtT? oe = oppSeg.Existing(oppTe, coinSeg);
        if (overlap != null && os != null && oe != null && overlap.Contains(os, oe))
        {
            return true;
        }

        if (cs != null && cs.Deleted())
        {
            return false;
        }

        if (os != null && os.Deleted())
        {
            return false;
        }

        if (ce != null && ce.Deleted())
        {
            return false;
        }

        if (oe != null && oe.Deleted())
        {
            return false;
        }

        SkOpPtT? csExisting = cs == null ? coinSeg.Existing(coinTs, null) : null;
        SkOpPtT? ceExisting = ce == null ? coinSeg.Existing(coinTe, null) : null;
        if (csExisting != null && csExisting == ceExisting)
        {
            return false;
        }

        //    FAIL_IF(csExisting && (csExisting == ce ||
        //            csExisting->contains(ceExisting ? ceExisting : ce)));
        if (ceExisting != null && (ceExisting == cs
                || ceExisting.Contains(csExisting != null ? csExisting : cs!)))
        {
            return false;
        }

        SkOpPtT? osExisting = os == null ? oppSeg.Existing(oppTs, null) : null;
        SkOpPtT? oeExisting = oe == null ? oppSeg.Existing(oppTe, null) : null;
        if (osExisting != null && osExisting == oeExisting)
        {
            return false;
        }

        if (osExisting != null && (osExisting == oe
                || osExisting.Contains(oeExisting != null ? oeExisting : oe!)))
        {
            return false;
        }

        if (oeExisting != null && (oeExisting == os
                || oeExisting.Contains(osExisting != null ? osExisting : os!)))
        {
            return false;
        }

        // extra line in debug code
        if (cs == null || os == null)
        {
            SkOpPtT? csWritable = cs != null ? cs : coinSeg.AddT(coinTs);
            if (csWritable == ce)
            {
                return true;
            }

            SkOpPtT? osWritable = os != null ? os : oppSeg.AddT(oppTs);
            if (csWritable == null || osWritable == null)
            {
                return false;
            }

            _ = csWritable.Span().AddOpp(osWritable.Span());
            cs = csWritable;
            os = osWritable.Active();
            if (os == null)
            {
                return false;
            }

            if ((ce != null && ce.Deleted()) || (oe != null && oe.Deleted()))
            {
                return false;
            }
        }

        if (ce == null || oe == null)
        {
            SkOpPtT ceWritable = ce != null ? ce : coinSeg.AddT(coinTe)!;
            SkOpPtT oeWritable = oe != null ? oe : oppSeg.AddT(oppTe)!;
            if (!ceWritable.Span().AddOpp(oeWritable.Span()))
            {
                return false;
            }

            ce = ceWritable;
            oe = oeWritable;
        }

        if (cs.Deleted())
        {
            return false;
        }

        if (os.Deleted())
        {
            return false;
        }

        if (ce.Deleted())
        {
            return false;
        }

        if (oe.Deleted())
        {
            return false;
        }

        if (cs.Contains(ce) || os.Contains(oe))
        {
            return false;
        }

        bool result = true;
        if (overlap != null)
        {
            if (overlap.CoinPtTStart().Segment() == coinSeg)
            {
                result = overlap.Extend(cs, ce, os, oe);
            }
            else
            {
                if (os.T > oe.T)
                {
                    (cs, ce) = (ce, cs);
                    (os, oe) = (oe, os);
                }

                result = overlap.Extend(os, oe, cs, ce);
            }
        }
        else
        {
            Add(cs, ce, os, oe);
        }

        if (result)
        {
            added = true;
        }

        return true;
    }

    // Please keep this in sync with debugAddMissing()
    /* detects overlaps of different coincident runs on same segment */
    /* does not detect overlaps for pairs without any segments in common */
    // returns true if caller should loop again
    public bool AddMissing(out bool added)
    {
        SkCoincidentSpans? outer = _head;
        added = false;
        if (outer == null)
        {
            return true;
        }

        _top = outer;
        _head = null;
        do
        {
            // addifmissing can modify the list that this is walking
            // save head so that walker can iterate over old data unperturbed
            // addifmissing adds to head freely then add saved head in the end
            SkOpPtT ocs = outer.CoinPtTStart();
            if (ocs.Deleted())
            {
                return false;
            }

            SkOpSegment outerCoin = ocs.Segment();
            if (outerCoin.Done())
            {
                return false;
            }

            SkOpPtT oos = outer.OppPtTStart();
            if (oos.Deleted())
            {
                return true;
            }

            SkOpSegment outerOpp = oos.Segment();
            SkOpSegment outerCoinWritable = outerCoin;
            SkOpSegment outerOppWritable = outerOpp;
            SkCoincidentSpans? inner = outer;
            while ((inner = inner.Next()) != null)
            {
                double overS, overE;
                SkOpPtT ics = inner.CoinPtTStart();
                if (ics.Deleted())
                {
                    return false;
                }

                SkOpSegment innerCoin = ics.Segment();
                if (innerCoin.Done())
                {
                    return false;
                }

                SkOpPtT ios = inner.OppPtTStart();
                if (ios.Deleted())
                {
                    return false;
                }

                SkOpSegment innerOpp = ios.Segment();
                SkOpSegment innerCoinWritable = innerCoin;
                SkOpSegment innerOppWritable = innerOpp;
                if (outerCoin == innerCoin)
                {
                    SkOpPtT oce = outer.CoinPtTEnd();
                    if (oce.Deleted())
                    {
                        return true;
                    }

                    SkOpPtT ice = inner.CoinPtTEnd();
                    if (ice.Deleted())
                    {
                        return false;
                    }

                    if (outerOpp != innerOpp && Overlap(ocs, oce, ics, ice, out overS, out overE))
                    {
                        if (!AddIfMissing(ocs.Starter(oce), ics.Starter(ice),
                                overS, overE, outerOppWritable, innerOppWritable, ref added))
                        {
                            return false;
                        }
                    }
                }
                else if (outerCoin == innerOpp)
                {
                    SkOpPtT oce = outer.CoinPtTEnd();
                    if (oce.Deleted())
                    {
                        return false;
                    }

                    SkOpPtT ioe = inner.OppPtTEnd();
                    if (ioe.Deleted())
                    {
                        return false;
                    }

                    if (outerOpp != innerCoin && Overlap(ocs, oce, ios, ioe, out overS, out overE))
                    {
                        if (!AddIfMissing(ocs.Starter(oce), ios.Starter(ioe),
                                overS, overE, outerOppWritable, innerCoinWritable, ref added))
                        {
                            return false;
                        }
                    }
                }
                else if (outerOpp == innerCoin)
                {
                    SkOpPtT ooe = outer.OppPtTEnd();
                    if (ooe.Deleted())
                    {
                        return false;
                    }

                    SkOpPtT ice = inner.CoinPtTEnd();
                    if (ice.Deleted())
                    {
                        return false;
                    }

                    if (Overlap(oos, ooe, ics, ice, out overS, out overE))
                    {
                        if (!AddIfMissing(oos.Starter(ooe), ics.Starter(ice),
                                overS, overE, outerCoinWritable, innerOppWritable, ref added))
                        {
                            return false;
                        }
                    }
                }
                else if (outerOpp == innerOpp)
                {
                    SkOpPtT ooe = outer.OppPtTEnd();
                    if (ooe.Deleted())
                    {
                        return false;
                    }

                    SkOpPtT ioe = inner.OppPtTEnd();
                    if (ioe.Deleted())
                    {
                        return true;
                    }

                    if (Overlap(oos, ooe, ios, ioe, out overS, out overE))
                    {
                        if (!AddIfMissing(oos.Starter(ooe), ios.Starter(ioe),
                                overS, overE, outerCoinWritable, innerCoinWritable, ref added))
                        {
                            return false;
                        }
                    }
                }
            }
        }
        while ((outer = outer.Next()) != null);

        RestoreHead();
        return true;
    }

    private bool AddOverlap(
        SkOpSegment seg1,
        SkOpSegment seg1o,
        SkOpSegment seg2,
        SkOpSegment seg2o,
        SkOpPtT overS,
        SkOpPtT overE)
    {
        SkOpPtT? s1 = overS.Find(seg1);
        SkOpPtT? e1 = overE.Find(seg1);
        if (s1 == null)
        {
            return false;
        }

        if (e1 == null)
        {
            return false;
        }

        if (s1.Starter(e1).Span().UpCast().WindValue() == 0)
        {
            s1 = overS.Find(seg1o);
            e1 = overE.Find(seg1o);
            if (s1 == null)
            {
                return false;
            }

            if (e1 == null)
            {
                return false;
            }

            if (s1.Starter(e1).Span().UpCast().WindValue() == 0)
            {
                return true;
            }
        }

        SkOpPtT? s2 = overS.Find(seg2);
        SkOpPtT? e2 = overE.Find(seg2);
        if (s2 == null)
        {
            return false;
        }

        if (e2 == null)
        {
            return false;
        }

        if (s2.Starter(e2).Span().UpCast().WindValue() == 0)
        {
            s2 = overS.Find(seg2o);
            e2 = overE.Find(seg2o);
            if (s2 == null)
            {
                return false;
            }

            if (e2 == null)
            {
                return false;
            }

            if (s2.Starter(e2).Span().UpCast().WindValue() == 0)
            {
                return true;
            }
        }

        if (s1.Segment() == s2.Segment())
        {
            return true;
        }

        if (s1.T > e1.T)
        {
            (s1, e1) = (e1, s1);
            (s2, e2) = (e2, s2);
        }

        Add(s1, e1, s2, e2);
        return true;
    }

    private bool Contains(SkOpSegment seg, SkOpSegment opp, double oppT)
    {
        if (Contains(_head, seg, opp, oppT))
        {
            return true;
        }

        if (Contains(_top, seg, opp, oppT))
        {
            return true;
        }

        return false;
    }

    private bool Contains(SkCoincidentSpans? coin, SkOpSegment seg, SkOpSegment opp, double oppT)
    {
        if (coin == null)
        {
            return false;
        }

        do
        {
            if (coin.CoinPtTStart().Segment() == seg && coin.OppPtTStart().Segment() == opp
                && SkPathOps.Between(coin.OppPtTStart().T, oppT, coin.OppPtTEnd().T))
            {
                return true;
            }

            if (coin.OppPtTStart().Segment() == seg && coin.CoinPtTStart().Segment() == opp
                && SkPathOps.Between(coin.CoinPtTStart().T, oppT, coin.CoinPtTEnd().T))
            {
                return true;
            }
        }
        while ((coin = coin.Next()) != null);

        return false;
    }

    public bool Contains(SkOpPtT coinPtTStart, SkOpPtT coinPtTEnd, SkOpPtT oppPtTStart, SkOpPtT oppPtTEnd)
    {
        SkCoincidentSpans? test = _head;
        if (test == null)
        {
            return false;
        }

        SkOpSegment coinSeg = coinPtTStart.Segment();
        SkOpSegment oppSeg = oppPtTStart.Segment();
        if (!Ordered(coinPtTStart, oppPtTStart))
        {
            (coinSeg, oppSeg) = (oppSeg, coinSeg);
            (coinPtTStart, oppPtTStart) = (oppPtTStart, coinPtTStart);
            (coinPtTEnd, oppPtTEnd) = (oppPtTEnd, coinPtTEnd);
            if (coinPtTStart.T > coinPtTEnd.T)
            {
                (coinPtTStart, coinPtTEnd) = (coinPtTEnd, coinPtTStart);
                (oppPtTStart, oppPtTEnd) = (oppPtTEnd, oppPtTStart);
            }
        }

        double oppMinT = SkPathOps.StdMin(oppPtTStart.T, oppPtTEnd.T);
        double oppMaxT = SkPathOps.StdMax(oppPtTStart.T, oppPtTEnd.T);
        do
        {
            if (coinSeg != test.CoinPtTStart().Segment())
            {
                continue;
            }

            if (coinPtTStart.T < test.CoinPtTStart().T)
            {
                continue;
            }

            if (coinPtTEnd.T > test.CoinPtTEnd().T)
            {
                continue;
            }

            if (oppSeg != test.OppPtTStart().Segment())
            {
                continue;
            }

            if (oppMinT < SkPathOps.StdMin(test.OppPtTStart().T, test.OppPtTEnd().T))
            {
                continue;
            }

            if (oppMaxT > SkPathOps.StdMax(test.OppPtTStart().T, test.OppPtTEnd().T))
            {
                continue;
            }

            return true;
        }
        while ((test = test.Next()) != null);

        return false;
    }

    public void CorrectEnds()
    {
        SkCoincidentSpans? coin = _head;
        if (coin == null)
        {
            return;
        }

        do
        {
            coin.CorrectEnds();
        }
        while ((coin = coin.Next()) != null);
    }

    // walk span sets in parallel, moving winding from one to the other
    public bool Apply()
    {
        SkCoincidentSpans? coin = _head;
        if (coin == null)
        {
            return true;
        }

        do
        {
            SkOpSpanBase startSpan = coin.CoinPtTStartWritable().Span();
            if (startSpan.UpCastable() == null)
            {
                return false;
            }

            SkOpSpan start = startSpan.UpCast();
            if (start.Deleted())
            {
                continue;
            }

            SkOpSpanBase end = coin.CoinPtTEnd().Span();
            if (start != start.Starter(end))
            {
                return false;
            }

            bool flipped = coin.Flipped();
            SkOpSpanBase oStartBase = (flipped ? coin.OppPtTEndWritable() : coin.OppPtTStartWritable()).Span();
            if (oStartBase.UpCastable() == null)
            {
                return false;
            }

            SkOpSpan oStart = oStartBase.UpCast();
            if (oStart.Deleted())
            {
                continue;
            }

            SkOpSpanBase oEnd = (flipped ? coin.OppPtTStart() : coin.OppPtTEnd()).Span();
            SkOpSegment segment = start.Segment();
            SkOpSegment oSegment = oStart.Segment();
            bool operandSwap = segment.Operand() != oSegment.Operand();
            if (flipped)
            {
                if (oEnd.Deleted())
                {
                    continue;
                }

                do
                {
                    SkOpSpanBase oNext = oStart.Next();
                    if (oNext == oEnd)
                    {
                        break;
                    }

                    if (oNext.UpCastable() == null)
                    {
                        return false;
                    }

                    oStart = oNext.UpCast();
                }
                while (true);
            }

            do
            {
                int windValue = start.WindValue();
                int oppValue = start.OppValue();
                int oWindValue = oStart.WindValue();
                int oOppValue = oStart.OppValue();
                // winding values are added or subtracted depending on direction and wind type
                // same or opposite values are summed depending on the operand value
                int windDiff = operandSwap ? oOppValue : oWindValue;
                int oWindDiff = operandSwap ? oppValue : windValue;
                if (!flipped)
                {
                    windDiff = -windDiff;
                    oWindDiff = -oWindDiff;
                }

                bool addToStart = windValue != 0 && (windValue > windDiff || (windValue == windDiff
                    && oWindValue <= oWindDiff));
                if (addToStart ? start.Done() : oStart.Done())
                {
                    addToStart ^= true;
                }

                if (addToStart)
                {
                    if (operandSwap)
                    {
                        (oWindValue, oOppValue) = (oOppValue, oWindValue);
                    }

                    if (flipped)
                    {
                        windValue -= oWindValue;
                        oppValue -= oOppValue;
                    }
                    else
                    {
                        windValue += oWindValue;
                        oppValue += oOppValue;
                    }

                    if (segment.IsXor())
                    {
                        windValue &= 1;
                    }

                    if (segment.OppXor())
                    {
                        oppValue &= 1;
                    }

                    oWindValue = oOppValue = 0;
                }
                else
                {
                    if (operandSwap)
                    {
                        (windValue, oppValue) = (oppValue, windValue);
                    }

                    if (flipped)
                    {
                        oWindValue -= windValue;
                        oOppValue -= oppValue;
                    }
                    else
                    {
                        oWindValue += windValue;
                        oOppValue += oppValue;
                    }

                    if (oSegment.IsXor())
                    {
                        oWindValue &= 1;
                    }

                    if (oSegment.OppXor())
                    {
                        oOppValue &= 1;
                    }

                    windValue = oppValue = 0;
                }

                if (windValue <= -1)
                {
                    return false;
                }

                start.SetWindValue(windValue);
                start.SetOppValue(oppValue);
                if (oWindValue <= -1)
                {
                    return false;
                }

                oStart.SetWindValue(oWindValue);
                oStart.SetOppValue(oOppValue);
                if (windValue == 0 && oppValue == 0)
                {
                    segment.MarkDone(start);
                }

                if (oWindValue == 0 && oOppValue == 0)
                {
                    oSegment.MarkDone(oStart);
                }

                SkOpSpanBase next = start.Next();
                SkOpSpanBase? oNext = flipped ? oStart.Prev() : oStart.Next();
                if (next == end)
                {
                    break;
                }

                if (next.UpCastable() == null)
                {
                    return false;
                }

                start = next.UpCast();
                // if the opposite ran out too soon, just reuse the last span
                if (oNext == null || oNext.UpCastable() == null)
                {
                    oNext = oStart;
                }

                oStart = oNext.UpCast();
            }
            while (true);
        }
        while ((coin = coin.Next()) != null);

        return true;
    }

    // Please keep this in sync with debugRelease()
    private bool Release(SkCoincidentSpans coin, SkCoincidentSpans remove)
    {
        SkCoincidentSpans head = coin;
        SkCoincidentSpans? prev = null;
        SkCoincidentSpans? next;
        SkCoincidentSpans? walk = coin;
        do
        {
            next = walk.Next();
            if (walk == remove)
            {
                if (prev != null)
                {
                    prev.SetNext(next);
                }
                else if (head == _head)
                {
                    _head = next;
                }
                else
                {
                    _top = next;
                }

                break;
            }

            prev = walk;
        }
        while ((walk = next) != null);

        return walk != null;
    }

    private void ReleaseDeleted(SkCoincidentSpans? coin)
    {
        if (coin == null)
        {
            return;
        }

        SkCoincidentSpans head = coin;
        SkCoincidentSpans? prev = null;
        SkCoincidentSpans? next;
        do
        {
            next = coin.Next();
            if (coin.CoinPtTStart().Deleted())
            {
                if (prev != null)
                {
                    prev.SetNext(next);
                }
                else if (head == _head)
                {
                    _head = next;
                }
                else
                {
                    _top = next;
                }
            }
            else
            {
                prev = coin;
            }
        }
        while ((coin = next) != null);
    }

    public void ReleaseDeleted()
    {
        ReleaseDeleted(_head);
        ReleaseDeleted(_top);
    }

    private void RestoreHead()
    {
        // SkCoincidentSpans** headPtr = &fHead; while (*headPtr) headPtr = (*headPtr)->nextPtr(); *headPtr = fTop;
        if (_head == null)
        {
            _head = _top;
        }
        else
        {
            SkCoincidentSpans last = _head;
            while (last.Next() != null)
            {
                last = last.Next()!;
            }

            last.SetNext(_top);
        }

        _top = null;
        // segments may have collapsed in the meantime; remove empty referenced segments
        // (headPtr = &fHead; the trailing node `prev` stands for headPtr == &prev->fNext)
        SkCoincidentSpans? prev = null;
        SkCoincidentSpans? test = _head;
        while (test != null)
        {
            if (test.CoinPtTStart().Segment().Done() || test.OppPtTStart().Segment().Done())
            {
                if (prev == null)
                {
                    _head = test.Next();
                }
                else
                {
                    prev.SetNext(test.Next());
                }

                test = test.Next();
                continue;
            }

            prev = test;
            test = test.Next();
        }
    }

    // Please keep this in sync with debugExpand()
    // expand the range by checking adjacent spans for coincidence
    public bool Expand()
    {
        SkCoincidentSpans? coin = _head;
        if (coin == null)
        {
            return false;
        }

        bool expanded = false;
        do
        {
            if (coin.Expand())
            {
                // check to see if multiple spans expanded so they are now identical
                SkCoincidentSpans? test = _head;
                do
                {
                    if (coin == test)
                    {
                        continue;
                    }

                    if (coin.CoinPtTStart() == test!.CoinPtTStart()
                        && coin.OppPtTStart() == test.OppPtTStart())
                    {
                        _ = Release(_head!, test);
                        break;
                    }
                }
                while ((test = test!.Next()) != null);

                expanded = true;
            }
        }
        while ((coin = coin.Next()) != null);

        return expanded;
    }

    public bool FindOverlaps(SkOpCoincidence overlaps)
    {
        overlaps._head = overlaps._top = null;
        SkCoincidentSpans? outer = _head;
        while (outer != null)
        {
            SkOpSegment outerCoin = outer.CoinPtTStart().Segment();
            SkOpSegment outerOpp = outer.OppPtTStart().Segment();
            SkCoincidentSpans? inner = outer;
            while ((inner = inner.Next()) != null)
            {
                SkOpSegment innerCoin = inner.CoinPtTStart().Segment();
                if (outerCoin == innerCoin)
                {
                    continue; // both winners are the same segment, so there's no additional overlap
                }

                SkOpSegment innerOpp = inner.OppPtTStart().Segment();
                SkOpPtT? overlapS = null;
                SkOpPtT? overlapE = null;
                if ((outerOpp == innerCoin && SkOpPtT.Overlaps(outer.OppPtTStart(),
                        outer.OppPtTEnd(), inner.CoinPtTStart(), inner.CoinPtTEnd(), out overlapS,
                        out overlapE))
                    || (outerCoin == innerOpp && SkOpPtT.Overlaps(outer.CoinPtTStart(),
                        outer.CoinPtTEnd(), inner.OppPtTStart(), inner.OppPtTEnd(),
                        out overlapS, out overlapE))
                    || (outerOpp == innerOpp && SkOpPtT.Overlaps(outer.OppPtTStart(),
                        outer.OppPtTEnd(), inner.OppPtTStart(), inner.OppPtTEnd(),
                        out overlapS, out overlapE)))
                {
                    if (!overlaps.AddOverlap(outerCoin, outerOpp, innerCoin, innerOpp,
                            overlapS!, overlapE!))
                    {
                        return false;
                    }
                }
            }

            outer = outer.Next();
        }

        return true;
    }

    public void FixUp(SkOpPtT deleted, SkOpPtT kept)
    {
        if (_head != null)
        {
            FixUp(_head, deleted, kept);
        }

        if (_top != null)
        {
            FixUp(_top, deleted, kept);
        }
    }

    private void FixUp(SkCoincidentSpans coin, SkOpPtT deleted, SkOpPtT kept)
    {
        SkCoincidentSpans head = coin;
        SkCoincidentSpans? walk = coin;
        do
        {
            if (walk.CoinPtTStart() == deleted)
            {
                if (walk.CoinPtTEnd().Span() == kept.Span())
                {
                    _ = Release(head, walk);
                    continue;
                }

                walk.SetCoinPtTStart(kept);
            }

            if (walk.CoinPtTEnd() == deleted)
            {
                if (walk.CoinPtTStart().Span() == kept.Span())
                {
                    _ = Release(head, walk);
                    continue;
                }

                walk.SetCoinPtTEnd(kept);
            }

            if (walk.OppPtTStart() == deleted)
            {
                if (walk.OppPtTEnd().Span() == kept.Span())
                {
                    _ = Release(head, walk);
                    continue;
                }

                walk.SetOppPtTStart(kept);
            }

            if (walk.OppPtTEnd() == deleted)
            {
                if (walk.OppPtTStart().Span() == kept.Span())
                {
                    _ = Release(head, walk);
                    continue;
                }

                walk.SetOppPtTEnd(kept);
            }
        }
        while ((walk = walk.Next()) != null);
    }

    // Please keep this in sync with debugMark()
    /* this sets up the coincidence links in the segments when the coincidence crosses multiple spans */
    public bool Mark()
    {
        SkCoincidentSpans? coin = _head;
        if (coin == null)
        {
            return true;
        }

        do
        {
            SkOpSpanBase startBase = coin.CoinPtTStartWritable().Span();
            if (startBase.UpCastable() == null)
            {
                return false;
            }

            SkOpSpan start = startBase.UpCast();
            if (start.Deleted())
            {
                return false;
            }

            SkOpSpanBase end = coin.CoinPtTEndWritable().Span();
            SkOpSpanBase oStart = coin.OppPtTStartWritable().Span();
            SkOpSpanBase oEnd = coin.OppPtTEndWritable().Span();
            if (oEnd.Deleted())
            {
                return false;
            }

            bool flipped = coin.Flipped();
            if (flipped)
            {
                (oStart, oEnd) = (oEnd, oStart);
            }

            /* coin and opp spans may not match up. Mark the ends, and then let the interior
               get marked as many times as the spans allow */
            if (oStart.UpCastable() == null)
            {
                return false;
            }

            start.InsertCoincidence(oStart.UpCast());
            end.InsertCoinEnd(oEnd);
            SkOpSegment segment = start.Segment();
            SkOpSegment oSegment = oStart.Segment();
            SkOpSpanBase next = start;
            SkOpSpanBase oNext = oStart;
            if (!coin.Ordered(out bool ordered))
            {
                return false;
            }

            while ((next = next.UpCast().Next()) != end)
            {
                if (next.UpCastable() == null)
                {
                    return false;
                }

                if (!next.UpCast().InsertCoincidence(oSegment, flipped, ordered))
                {
                    return false;
                }
            }

            while ((oNext = oNext.UpCast().Next()) != oEnd)
            {
                if (oNext.UpCastable() == null)
                {
                    return false;
                }

                if (!oNext.UpCast().InsertCoincidence(segment, flipped, ordered))
                {
                    return false;
                }
            }
        }
        while ((coin = coin.Next()) != null);

        return true;
    }

    // Please keep in sync with debugMarkCollapsed()
    private void MarkCollapsed(SkCoincidentSpans? coin, SkOpPtT test)
    {
        SkCoincidentSpans? head = coin;
        while (coin != null)
        {
            if (coin.Collapsed(test))
            {
                if (SkPathOps.ZeroOrOne(coin.CoinPtTStart().T) && SkPathOps.ZeroOrOne(coin.CoinPtTEnd().T))
                {
                    coin.CoinPtTStartWritable().Segment().MarkAllDone();
                }

                if (SkPathOps.ZeroOrOne(coin.OppPtTStart().T) && SkPathOps.ZeroOrOne(coin.OppPtTEnd().T))
                {
                    coin.OppPtTStartWritable().Segment().MarkAllDone();
                }

                _ = Release(head!, coin);
            }

            coin = coin.Next();
        }
    }

    // Please keep in sync with debugMarkCollapsed()
    public void MarkCollapsed(SkOpPtT test)
    {
        MarkCollapsed(_head, test);
        MarkCollapsed(_top, test);
    }

    public static bool Ordered(SkOpSegment coinSeg, SkOpSegment oppSeg)
    {
        if (coinSeg.Verb() < oppSeg.Verb())
        {
            return true;
        }

        if (coinSeg.Verb() > oppSeg.Verb())
        {
            return false;
        }

        int count = (SkPathOps.SkPathOpsVerbToPoints(coinSeg.Verb()) + 1) * 2;
        ReadOnlySpan<float> cPt = MemoryMarshal.Cast<PathPoint, float>(coinSeg.Pts());
        ReadOnlySpan<float> oPt = MemoryMarshal.Cast<PathPoint, float>(oppSeg.Pts());
        for (int index = 0; index < count; ++index)
        {
            if (cPt[index] < oPt[index])
            {
                return true;
            }

            if (cPt[index] > oPt[index])
            {
                return false;
            }
        }

        return true;
    }

    private bool Overlap(
        SkOpPtT coin1s,
        SkOpPtT coin1e,
        SkOpPtT coin2s,
        SkOpPtT coin2e,
        out double overS,
        out double overE)
    {
        overS = SkPathOps.StdMax(SkPathOps.StdMin(coin1s.T, coin1e.T), SkPathOps.StdMin(coin2s.T, coin2e.T));
        overE = SkPathOps.StdMin(SkPathOps.StdMax(coin1s.T, coin1e.T), SkPathOps.StdMax(coin2s.T, coin2e.T));
        return overS < overE;
    }

    // Commented-out lines keep this in sync with debugRelease()
    public void Release(SkOpSegment deleted)
    {
        SkCoincidentSpans? coin = _head;
        if (coin == null)
        {
            return;
        }

        do
        {
            if (coin.CoinPtTStart().Segment() == deleted
                || coin.CoinPtTEnd().Segment() == deleted
                || coin.OppPtTStart().Segment() == deleted
                || coin.OppPtTEnd().Segment() == deleted)
            {
                _ = Release(_head!, coin);
            }
        }
        while ((coin = coin.Next()) != null);
    }
}
