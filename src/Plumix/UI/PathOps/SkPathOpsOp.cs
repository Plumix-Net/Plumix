namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkPathOpsOp.cpp at chrome/m119 (the Skia inside SkiaSharp
// 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types are kept. Op is
// OpDebug without its debug parameters (Skia's Op only forwards to it); SkPath is Plumix.UI.Path. Plumix has no
// inverse fill types (Dart's Path has only nonZero/evenOdd), so isInverseFillType() is always false here, but the
// gOpInverse/gOutInverse tables and the inverse branches stay so the code reads as Skia's; an inverse result fill
// could only come from an inverse input. The arena allocator is dropped (convention 6) and the stack SkOpContour
// that Skia static_casts to SkOpContourHead is an SkOpContourHead. `*result = original` is Path.CopyFrom.
// DEBUG_* blocks and SkPathOpsDebug calls are omitted.

internal static partial class SkPathOps
{
    // diagram of why this simplifcation is possible is here:
    // https://skia.org/dev/present/pathops link at bottom of the page
    // https://drive.google.com/file/d/0BwoLUwz9PYkHLWpsaXd0UDdaN00/view?usp=sharing
    private static readonly SkPathOp[,,] GOpInverse =
    {
        //                  inside minuend                               outside minuend
        //     inside subtrahend     outside subtrahend      inside subtrahend     outside subtrahend
        {
            { SkPathOp.Difference, SkPathOp.Intersect }, { SkPathOp.Union, SkPathOp.ReverseDifference },
        },
        {
            { SkPathOp.Intersect, SkPathOp.Difference }, { SkPathOp.ReverseDifference, SkPathOp.Union },
        },
        {
            { SkPathOp.Union, SkPathOp.ReverseDifference }, { SkPathOp.Difference, SkPathOp.Intersect },
        },
        {
            { SkPathOp.XOR, SkPathOp.XOR }, { SkPathOp.XOR, SkPathOp.XOR },
        },
        {
            { SkPathOp.ReverseDifference, SkPathOp.Union }, { SkPathOp.Intersect, SkPathOp.Difference },
        },
    };

    private static readonly bool[,,] GOutInverse =
    {
        { { false, false }, { true, false } }, // diff
        { { false, false }, { false, true } }, // sect
        { { false, true }, { true, true } }, // union
        { { false, true }, { true, false } }, // xor
        { { false, true }, { false, false } }, // rev diff
    };

    // SkPath::isInverseFillType: Plumix paths have no inverse fill types.
    private static bool SkPathIsInverseFillType(Path path)
    {
        return false;
    }

    public static bool Op(Path one, Path two, SkPathOp op, Path result)
    {
        bool oneInverse = SkPathIsInverseFillType(one);
        bool twoInverse = SkPathIsInverseFillType(two);
        op = GOpInverse[(int)op, oneInverse ? 1 : 0, twoInverse ? 1 : 0];
        bool inverseFill = GOutInverse[(int)op, oneInverse ? 1 : 0, twoInverse ? 1 : 0];
        // SkPathFillType::kInverseEvenOdd has no Plumix counterpart; inverseFill is false for non-inverse inputs.
        PathFillType fillType = inverseFill ? PathFillType.EvenOdd : PathFillType.EvenOdd;
        if (SkPathOp.Intersect == op && SkPathIsRect(one, out SkRect rect1) && SkPathIsRect(two, out SkRect rect2))
        {
            result.Reset();
            result.FillType = fillType;
            if (SkRectIntersect(ref rect1, rect2))
            {
                result.AddRect(Rect.FromLTRB(rect1.Left, rect1.Top, rect1.Right, rect1.Bottom));
            }

            return true;
        }

        if (SkPathIsEmpty(one) || SkPathIsEmpty(two))
        {
            Path work = new Path();
            switch (op)
            {
                case SkPathOp.Intersect:
                    break;
                case SkPathOp.Union:
                case SkPathOp.XOR:
                    work = SkPathIsEmpty(one) ? two : one;
                    break;
                case SkPathOp.Difference:
                    if (!SkPathIsEmpty(one))
                    {
                        work = one;
                    }

                    break;
                case SkPathOp.ReverseDifference:
                    if (!SkPathIsEmpty(two))
                    {
                        work = two;
                    }

                    break;
                default:
                    break;
            }

            if (inverseFill != SkPathIsInverseFillType(work))
            {
                // work.toggleInverseFillType(): unreachable without inverse fill types
            }

            return Simplify(work, result);
        }

        SkOpContourHead contourList = new SkOpContourHead();
        SkOpGlobalState globalState = new SkOpGlobalState(contourList);
        SkOpCoincidence coincidence = new SkOpCoincidence(globalState);
        Path minuend = one;
        Path subtrahend = two;
        if (op == SkPathOp.ReverseDifference)
        {
            (minuend, subtrahend) = (subtrahend, minuend);
            op = SkPathOp.Difference;
        }

        // turn path into list of segments
        SkOpEdgeBuilder builder = new SkOpEdgeBuilder(minuend, contourList, globalState);
        if (builder.Unparseable())
        {
            return false;
        }

        int xorMask = (int)builder.XorMask();
        builder.AddOperand(subtrahend);
        if (!builder.Finish())
        {
            return false;
        }

        int xorOpMask = (int)builder.XorMask();
        if (!SortContourList(ref contourList, xorMask == (int)SkPathOpsMask.EvenOdd,
                xorOpMask == (int)SkPathOpsMask.EvenOdd))
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
        Path original = new Path(result);
        result.Reset();
        result.FillType = fillType;
        SkPathWriter wrapper = new SkPathWriter(result);
        if (!SkPathOpsOpStatics.BridgeOp(contourList, op, xorMask, xorOpMask, wrapper))
        {
            result.CopyFrom(original);
            return false;
        }

        wrapper.Assemble(); // if some edges could not be resolved, assemble remaining
        return true;
    }
}

file static class SkPathOpsOpStatics
{
    public static bool FindChaseOp(
        List<SkOpSpanBase> chase,
        ref SkOpSpanBase? startPtr,
        ref SkOpSpanBase? endPtr,
        out SkOpSegment? result)
    {
        while (chase.Count != 0)
        {
            SkOpSpanBase span = chase[^1];
            chase.RemoveAt(chase.Count - 1);
            // OPTIMIZE: prev makes this compatible with old code -- but is it necessary?
            startPtr = span.PtT().Prev().Span();
            SkOpSegment segment = startPtr.Segment();
            bool done = true;
            endPtr = null;
            SkOpAngle? last = segment.ActiveAngle(startPtr, ref startPtr, ref endPtr, ref done);
            if (last != null)
            {
                startPtr = last.Start();
                endPtr = last.End();
                chase.Add(span);
                result = last.Segment();
                return true;
            }

            if (done)
            {
                continue;
            }

            SkOpAngle? angle = SkPathOps.AngleWinding(startPtr!, endPtr!, out int winding, out bool sortable);
            if (angle == null)
            {
                result = null;
                return true;
            }

            if (winding == SkPathOps.SkMinS32)
            {
                continue;
            }

            int sumMiWinding = 0, sumSuWinding = 0;
            if (sortable)
            {
                segment = angle.Segment();
                sumMiWinding = segment.UpdateWindingReverse(angle);
                if (sumMiWinding == SkPathOps.SkMinS32)
                {
                    result = null;
                    return true;
                }

                sumSuWinding = segment.UpdateOppWindingReverse(angle);
                if (sumSuWinding == SkPathOps.SkMinS32)
                {
                    result = null;
                    return true;
                }

                if (segment.Operand())
                {
                    (sumMiWinding, sumSuWinding) = (sumSuWinding, sumMiWinding);
                }
            }

            SkOpSegment? first = null;
            SkOpAngle firstAngle = angle;
            while ((angle = angle.Next()!) != firstAngle)
            {
                segment = angle.Segment();
                SkOpSpanBase start = angle.Start();
                SkOpSpanBase end = angle.End();
                int maxWinding = 0, sumWinding = 0, oppMaxWinding = 0, oppSumWinding = 0;
                if (sortable)
                {
                    segment.SetUpWindings(start, end, ref sumMiWinding, ref sumSuWinding,
                        out maxWinding, out sumWinding, out oppMaxWinding, out oppSumWinding);
                }

                if (!segment.Done(angle))
                {
                    if (first == null && (sortable || start.Starter(end).WindSum() != SkPathOps.SkMinS32))
                    {
                        first = segment;
                        startPtr = start;
                        endPtr = end;
                    }

                    // OPTIMIZATION: should this also add to the chase?
                    if (sortable)
                    {
                        if (!segment.MarkAngle(maxWinding, sumWinding, oppMaxWinding,
                                oppSumWinding, angle))
                        {
                            result = null;
                            return false;
                        }
                    }
                }
            }

            if (first != null)
            {
                chase.Add(span);
                result = first;
                return true;
            }
        }

        result = null;
        return true;
    }

    public static bool BridgeOp(
        SkOpContourHead contourList,
        SkPathOp op,
        int xorMask,
        int xorOpMask,
        SkPathWriter writer)
    {
        bool unsortable = false;
        bool lastSimple = false;
        bool simple = false;
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
                if (current!.ActiveOp(start!, end!, xorMask, xorOpMask, op))
                {
                    do
                    {
                        if (!unsortable && current.Done())
                        {
                            break;
                        }

                        SkOpSpanBase nextStart = start!;
                        SkOpSpanBase nextEnd = end!;
                        lastSimple = simple;
                        SkOpSegment? next = current.FindNextOp(chase, ref nextStart, ref nextEnd,
                            ref unsortable, out simple, op, xorMask, xorOpMask);
                        if (next == null)
                        {
                            if (!unsortable && writer.HasMove()
                                && current.Verb() != PathVerb.Line
                                && !writer.IsClosed())
                            {
                                if (!current.AddCurveTo(start!, end!, writer))
                                {
                                    return false;
                                }
                            }
                            else if (lastSimple)
                            {
                                if (!current.AddCurveTo(start!, end!, writer))
                                {
                                    return false;
                                }
                            }

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

                if (!FindChaseOp(chase, ref start, ref end, out current))
                {
                    return false;
                }

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
}
