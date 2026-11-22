using System.Runtime.InteropServices;

namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkPathOpsCommon.h and SkPathOpsCommon.cpp at chrome/m119 (the
// Skia inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double
// types are kept. SkTDArray<SkOpSpanBase*>* chase is a List<SkOpSpanBase> (append/back/pop_back at its end;
// TRY_ROTATE is off in Skia). The SkOpSpanBase** start/end outputs are `ref SkOpSpanBase?`, since FindChase writes
// nullptr into endPtr before activeAngle fills it. SkOpContourHead** is `ref SkOpContourHead`. FixWinding is
// declared in SkPathOpsCommon.h but defined in SkPathOpsAsWinding.cpp, which Op and Simplify do not need, so it is
// not ported; OpDebug is ported as Op (SkPathOpsOp.cs). The DEBUG_COIN/DEBUG_PHASE parameters,
// SkPathOpsDebug::ShowActiveSpans and debug validation are omitted.

internal static partial class SkPathOps
{
    public static SkOpAngle? AngleWinding(
        SkOpSpanBase start,
        SkOpSpanBase end,
        out int windingPtr,
        out bool sortablePtr)
    {
        // find first angle, initialize winding to computed fWindSum
        SkOpSegment segment = start.Segment();
        SkOpAngle? angle = segment.SpanToAngle(start, end);
        if (angle == null)
        {
            windingPtr = SkMinS32;
            sortablePtr = false;
            return null;
        }

        bool computeWinding = false;
        SkOpAngle firstAngle = angle;
        bool loop = false;
        bool unorderable = false;
        int winding = SkMinS32;
        do
        {
            angle = angle.Next();
            if (angle == null)
            {
                windingPtr = 0;
                sortablePtr = false;
                return null;
            }

            unorderable |= angle.Unorderable();
            if (computeWinding = unorderable || (angle == firstAngle && loop))
            {
                break; // if we get here, there's no winding, loop is unorderable
            }

            loop |= angle == firstAngle;
            segment = angle.Segment();
            winding = segment.WindSum(angle);
        }
        while (winding == SkMinS32);

        // if the angle loop contains an unorderable span, the angle order may be useless
        // directly compute the winding in this case for each span
        if (computeWinding)
        {
            firstAngle = angle;
            winding = SkMinS32;
            do
            {
                SkOpSpanBase startSpan = angle.Start();
                SkOpSpanBase endSpan = angle.End();
                SkOpSpan lesser = startSpan.Starter(endSpan);
                int testWinding = lesser.WindSum();
                if (testWinding == SkMinS32)
                {
                    testWinding = lesser.ComputeWindSum();
                }

                if (testWinding != SkMinS32)
                {
                    segment = angle.Segment();
                    winding = testWinding;
                }

                angle = angle.Next()!;
            }
            while (angle != firstAngle);
        }

        sortablePtr = !unorderable;
        windingPtr = winding;
        return angle;
    }

    public static SkOpSpan? FindUndone(SkOpContourHead contourHead)
    {
        SkOpContour? contour = contourHead;
        do
        {
            if (contour.Done())
            {
                continue;
            }

            SkOpSpan? result = contour.UndoneSpan();
            if (result != null)
            {
                return result;
            }
        }
        while ((contour = contour.Next()) != null);

        return null;
    }

    public static SkOpSegment? FindChase(
        List<SkOpSpanBase> chase,
        ref SkOpSpanBase? startPtr,
        ref SkOpSpanBase? endPtr)
    {
        while (chase.Count != 0)
        {
            SkOpSpanBase span = chase[^1];
            chase.RemoveAt(chase.Count - 1);
            SkOpSegment segment = span.Segment();
            startPtr = span.PtT().Next().Span();
            bool done = true;
            endPtr = null;
            SkOpAngle? last = segment.ActiveAngle(startPtr!, ref startPtr, ref endPtr, ref done);
            if (last != null)
            {
                startPtr = last.Start();
                endPtr = last.End();
                chase.Add(span);
                return last.Segment();
            }

            if (done)
            {
                continue;
            }

            // find first angle, initialize winding to computed wind sum
            SkOpAngle? angle = AngleWinding(startPtr!, endPtr!, out int winding, out bool sortable);
            if (angle == null)
            {
                return null;
            }

            if (winding == SkMinS32)
            {
                continue;
            }

            int sumWinding = 0;
            if (sortable)
            {
                segment = angle.Segment();
                sumWinding = segment.UpdateWindingReverse(angle);
            }

            SkOpSegment? first = null;
            SkOpAngle firstAngle = angle;
            while ((angle = angle.Next()!) != firstAngle)
            {
                segment = angle.Segment();
                SkOpSpanBase start = angle.Start();
                SkOpSpanBase end = angle.End();
                int maxWinding = 0;
                if (sortable)
                {
                    segment.SetUpWinding(start, end, out maxWinding, ref sumWinding);
                }

                if (!segment.Done(angle))
                {
                    if (first == null && (sortable || start.Starter(end).WindSum() != SkMinS32))
                    {
                        first = segment;
                        startPtr = start;
                        endPtr = end;
                    }

                    // OPTIMIZATION: should this also add to the chase?
                    if (sortable)
                    {
                        // TODO: add error handling
                        _ = segment.MarkAngle(maxWinding, sumWinding, angle);
                    }
                }
            }

            if (first != null)
            {
                chase.Add(span);
                return first;
            }
        }

        return null;
    }

    public static bool SortContourList(ref SkOpContourHead contourList, bool evenOdd, bool oppEvenOdd)
    {
        List<SkOpContour> list = new List<SkOpContour>();
        SkOpContour? contour = contourList;
        do
        {
            if (contour.Count() != 0)
            {
                contour.SetOppXor(contour.Operand() ? evenOdd : oppEvenOdd);
                list.Add(contour);
            }
        }
        while ((contour = contour.Next()) != null);

        int count = list.Count;
        if (count == 0)
        {
            return false;
        }

        if (count > 1)
        {
            SkTQSort(CollectionsMarshal.AsSpan(list), SkOpContour.LessThan);
        }

        contour = list[0];
        SkOpContourHead contourHead = (SkOpContourHead)contour;
        contour.GlobalState().SetContourHead(contourHead);
        contourList = contourHead;
        for (int index = 1; index < count; ++index)
        {
            SkOpContour next = list[index];
            contour.SetNext(next);
            contour = next;
        }

        contour.SetNext(null);
        return true;
    }

    public static bool HandleCoincidence(SkOpContourHead contourList, SkOpCoincidence coincidence)
    {
        SkOpGlobalState globalState = contourList.GlobalState();
        // match up points within the coincident runs
        if (!coincidence.AddExpanded())
        {
            return false;
        }

        // combine t values when multiple intersections occur on some segments but not others
        if (!SkPathOpsCommonStatics.MoveMultiples(contourList))
        {
            return false;
        }

        // move t values and points together to eliminate small/tiny gaps
        if (!SkPathOpsCommonStatics.MoveNearby(contourList))
        {
            return false;
        }

        // add coincidence formed by pairing on curve points and endpoints
        coincidence.CorrectEnds();
        if (!coincidence.AddEndMovedSpans())
        {
            return false;
        }

        const int SafetyCount = 3;
        int safetyHatch = SafetyCount;
        // look for coincidence present in A-B and A-C but missing in B-C
        do
        {
            if (!coincidence.AddMissing(out bool added))
            {
                return false;
            }

            if (!added)
            {
                break;
            }

            if (--safetyHatch == 0)
            {
                return false;
            }

            _ = SkPathOpsCommonStatics.MoveNearby(contourList);
        }
        while (true);

        // check to see if, loosely, coincident ranges may be expanded
        if (coincidence.Expand())
        {
            if (!coincidence.AddMissing(out _))
            {
                return false;
            }

            if (!coincidence.AddExpanded())
            {
                return false;
            }

            if (!SkPathOpsCommonStatics.MoveMultiples(contourList))
            {
                return false;
            }

            _ = SkPathOpsCommonStatics.MoveNearby(contourList);
        }

        // the expanded ranges may not align -- add the missing spans
        if (!coincidence.AddExpanded())
        {
            return false;
        }

        // mark spans of coincident segments as coincident
        _ = coincidence.Mark();
        // look for coincidence lines and curves undetected by intersection
        if (SkPathOpsCommonStatics.MissingCoincidence(contourList))
        {
            _ = coincidence.Expand();
            if (!coincidence.AddExpanded())
            {
                return false;
            }

            if (!coincidence.Mark())
            {
                return false;
            }
        }
        else
        {
            _ = coincidence.Expand();
        }

        _ = coincidence.Expand();

        SkOpCoincidence overlaps = new SkOpCoincidence(globalState);
        safetyHatch = SafetyCount;
        do
        {
            SkOpCoincidence pairs = overlaps.IsEmpty() ? coincidence : overlaps;
            // adjust the winding value to account for coincident edges
            if (!pairs.Apply())
            {
                return false;
            }

            // For each coincident pair that overlaps another, when the receivers (the 1st of the pair)
            // are different, construct a new pair to resolve their mutual span
            if (!pairs.FindOverlaps(overlaps))
            {
                return false;
            }

            if (--safetyHatch == 0)
            {
                return false;
            }
        }
        while (!overlaps.IsEmpty());

        SkPathOpsCommonStatics.CalcAngles(contourList);
        if (!SkPathOpsCommonStatics.SortAngles(contourList))
        {
            return false;
        }

        return true;
    }
}

file static class SkPathOpsCommonStatics
{
    public static void CalcAngles(SkOpContourHead contourList)
    {
        SkOpContour? contour = contourList;
        do
        {
            contour.CalcAngles();
        }
        while ((contour = contour.Next()) != null);
    }

    public static bool MissingCoincidence(SkOpContourHead contourList)
    {
        SkOpContour? contour = contourList;
        bool result = false;
        do
        {
            result |= contour.MissingCoincidence();
        }
        while ((contour = contour.Next()) != null);

        return result;
    }

    public static bool MoveMultiples(SkOpContourHead contourList)
    {
        SkOpContour? contour = contourList;
        do
        {
            if (!contour.MoveMultiples())
            {
                return false;
            }
        }
        while ((contour = contour.Next()) != null);

        return true;
    }

    public static bool MoveNearby(SkOpContourHead contourList)
    {
        SkOpContour? contour = contourList;
        do
        {
            if (!contour.MoveNearby())
            {
                return false;
            }
        }
        while ((contour = contour.Next()) != null);

        return true;
    }

    public static bool SortAngles(SkOpContourHead contourList)
    {
        SkOpContour? contour = contourList;
        do
        {
            if (!contour.SortAngles())
            {
                return false;
            }
        }
        while ((contour = contour.Next()) != null);

        return true;
    }
}
