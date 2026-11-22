namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkPathOpsSimplify.cpp at chrome/m119 (the Skia inside SkiaSharp
// 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types are kept.
// Simplify is SimplifyDebug without its debug parameters (Skia's Simplify only forwards to it); SkPath is
// Plumix.UI.Path, isInverseFillType() is always false (see SkPathOpsOp.cs) and isConvex() computes the convexity
// (SkPath.cs). `*result = path` is Path.CopyFrom, which keeps the oval/rrect shape as SkPath's copy does. The arena
// allocator is dropped and the stack SkOpContour that Skia static_casts to SkOpContourHead is an SkOpContourHead.
// DEBUG_* blocks and SkPathOpsDebug calls are omitted.

internal static partial class SkPathOps
{
    // FIXME : add this as a member of SkPath
    public static bool Simplify(Path path, Path result)
    {
        // returns 1 for evenodd, -1 for winding, regardless of inverse-ness
        // (SkPathFillType::kInverseEvenOdd has no Plumix counterpart; see SkPathOpsOp.cs)
        PathFillType fillType = SkPathIsInverseFillType(path) ? PathFillType.EvenOdd : PathFillType.EvenOdd;
        if (SkPathIsConvex(path))
        {
            if (result != path)
            {
                result.CopyFrom(path);
            }

            result.FillType = fillType;
            return true;
        }

        // turn path into list of segments
        SkOpContourHead contourList = new SkOpContourHead();
        SkOpGlobalState globalState = new SkOpGlobalState(contourList);
        SkOpCoincidence coincidence = new SkOpCoincidence(globalState);
        SkOpEdgeBuilder builder = new SkOpEdgeBuilder(path, contourList, globalState);
        if (!builder.Finish())
        {
            return false;
        }

        if (!SortContourList(ref contourList, false, false))
        {
            result.Reset();
            result.FillType = fillType;
            return true;
        }

        // find all intersections between segments
        SkOpContour? current = contourList;
        do
        {
            SkOpContour? next = current;
            while (AddIntersectTs(current, next!, coincidence)
                && (next = next!.Next()) != null)
            {
            }
        }
        while ((current = current.Next()) != null);

        bool success = HandleCoincidence(contourList, coincidence);
        if (!success)
        {
            return false;
        }

        // construct closed contours
        result.Reset();
        result.FillType = fillType;
        SkPathWriter wrapper = new SkPathWriter(result);
        if (builder.XorMask() == SkPathOpsMask.Winding
                ? !SkPathOpsSimplifyStatics.BridgeWinding(contourList, wrapper)
                : !SkPathOpsSimplifyStatics.BridgeXor(contourList, wrapper))
        {
            return false;
        }

        wrapper.Assemble(); // if some edges could not be resolved, assemble remaining
        return true;
    }
}

file static class SkPathOpsSimplifyStatics
{
    public static bool BridgeWinding(SkOpContourHead contourList, SkPathWriter writer)
    {
        bool unsortable = false;
        do
        {
            SkOpSpan? span = SkPathOps.FindSortableTop(contourList);
            if (span == null)
            {
                break;
            }

            SkOpSegment? current = span.Segment();
            SkOpSpanBase? start = span.Next();
            SkOpSpanBase? end = span;
            List<SkOpSpanBase> chase = new List<SkOpSpanBase>();
            do
            {
                if (current!.ActiveWinding(start!, end!))
                {
                    do
                    {
                        if (!unsortable && current.Done())
                        {
                            break;
                        }

                        SkOpSpanBase nextStart = start!;
                        SkOpSpanBase nextEnd = end!;
                        SkOpSegment? next = current.FindNextWinding(chase, ref nextStart, ref nextEnd,
                            ref unsortable);
                        if (next == null)
                        {
                            break;
                        }

                        if (!current.AddCurveTo(start!, end!, writer))
                        {
                            return false;
                        }

                        current = next;
                        start = nextStart;
                        end = nextEnd;
                    }
                    while (!writer.IsClosed() && (!unsortable || !start.Starter(end).Done()));

                    if (current.ActiveWinding(start!, end!) && !writer.IsClosed())
                    {
                        SkOpSpan spanStart = start!.Starter(end!);
                        if (!spanStart.Done())
                        {
                            if (!current.AddCurveTo(start, end!, writer))
                            {
                                return false;
                            }

                            current.MarkDone(spanStart);
                        }
                    }

                    writer.FinishContour();
                }
                else
                {
                    SkOpSpanBase? last = null;
                    if (!current.MarkAndChaseDone(start!, end!, ref last))
                    {
                        return false;
                    }

                    if (last != null && !last.Chased())
                    {
                        last.SetChased(true);
                        chase.Add(last);
                    }
                }

                current = SkPathOps.FindChase(chase, ref start, ref end);
                if (current == null)
                {
                    break;
                }
            }
            while (true);
        }
        while (true);

        return true;
    }

    // returns true if all edges were processed
    public static bool BridgeXor(SkOpContourHead contourList, SkPathWriter writer)
    {
        bool unsortable = false;
        int safetyNet = 1000000;
        do
        {
            SkOpSpan? span = SkPathOps.FindUndone(contourList);
            if (span == null)
            {
                break;
            }

            SkOpSegment current = span.Segment();
            SkOpSpanBase start = span.Next();
            SkOpSpanBase end = span;
            do
            {
                if (--safetyNet < 0)
                {
                    return false;
                }

                if (!unsortable && current.Done())
                {
                    break;
                }

                SkOpSpanBase nextStart = start;
                SkOpSpanBase nextEnd = end;
                SkOpSegment? next = current.FindNextXor(ref nextStart, ref nextEnd, ref unsortable);
                if (next == null)
                {
                    break;
                }

                if (!current.AddCurveTo(start, end, writer))
                {
                    return false;
                }

                current = next;
                start = nextStart;
                end = nextEnd;
            }
            while (!writer.IsClosed() && (!unsortable || !start.Starter(end).Done()));

            if (!writer.IsClosed())
            {
                SkOpSpan spanStart = start.Starter(end);
                if (!spanStart.Done())
                {
                    return false;
                }
            }

            writer.FinishContour();
        }
        while (true);

        return true;
    }
}
