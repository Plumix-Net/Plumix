using System.Runtime.InteropServices;

namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkPathOpsWinding.cpp at chrome/m119 (the Skia inside SkiaSharp
// 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types are kept. The
// members it defines for SkOpContour (rayCheck, findSortableTop), SkOpSegment (rayCheck, windingSpanAtT,
// findSortableTop) and SkOpSpan (sortableTop) are partial parts of those classes; FindSortableTop is in SkPathOps.
// SkOpRayHit is a class (its records link through fNext); the SkArenaAlloc that sortableTop passes down is dropped
// (make<SkOpRayHit>() is `new SkOpRayHit()`) and SkOpRayHit** hits is `ref SkOpRayHit? hits`. The (&pt.fX)[i] and
// (&rect.fLeft)[i] lane reads are spelled out as index switches. gDebugRayDirName and DEBUG_WINDING output are
// omitted.

internal enum SkOpRayDir
{
    Left,
    Top,
    Right,
    Bottom,
}

internal sealed class SkOpRayHit
{
    public SkOpRayHit? Next;
    public SkOpSpan? Span;
    public PathPoint Pt;
    public double T;
    public SkDVector Slope;
    public bool Valid;

    public SkOpRayDir MakeTestBase(SkOpSpan span, double t)
    {
        Next = null;
        Span = span;
        T = span.T() * (1 - t) + span.Next().T() * t;
        SkOpSegment segment = span.Segment();
        Slope = segment.DSlopeAtT(T);
        Pt = segment.PtAtT(T);
        Valid = true;
        return Math.Abs(Slope.X) < Math.Abs(Slope.Y) ? SkOpRayDir.Left : SkOpRayDir.Top;
    }
}

internal partial class SkOpContour
{
    public void RayCheck(SkOpRayHit @base, SkOpRayDir dir, ref SkOpRayHit? hits)
    {
        // if the bounds extreme is outside the best, we're done
        float baseXY = SkPathOpsWindingStatics.PtXy(@base.Pt, dir);
        float boundsXY = SkPathOpsWindingStatics.RectSide(_bounds, dir);
        bool checkLessThan = SkPathOpsWindingStatics.LessThan(dir);
        if (!SkPathOps.ApproximatelyEqual(baseXY, boundsXY) && (baseXY < boundsXY) == checkLessThan)
        {
            return;
        }

        SkOpSegment? testSegment = _head;
        do
        {
            testSegment.RayCheck(@base, dir, ref hits);
        }
        while ((testSegment = testSegment.Next()) != null);
    }

    public SkOpSpan? FindSortableTop(SkOpContour contourHead)
    {
        bool allDone = true;
        if (_count != 0)
        {
            SkOpSegment? testSegment = _head;
            do
            {
                if (testSegment.Done())
                {
                    continue;
                }

                allDone = false;
                SkOpSpan? result = testSegment.FindSortableTop(contourHead);
                if (result != null)
                {
                    return result;
                }
            }
            while ((testSegment = testSegment.Next()) != null);
        }

        if (allDone)
        {
            _done = true;
        }

        return null;
    }
}

internal sealed partial class SkOpSegment
{
    public void RayCheck(SkOpRayHit @base, SkOpRayDir dir, ref SkOpRayHit? hits)
    {
        if (!SkPathOpsWindingStatics.SidewaysOverlap(_bounds, @base.Pt, dir))
        {
            return;
        }

        float baseXY = SkPathOpsWindingStatics.PtXy(@base.Pt, dir);
        float boundsXY = SkPathOpsWindingStatics.RectSide(_bounds, dir);
        bool checkLessThan = SkPathOpsWindingStatics.LessThan(dir);
        if (!SkPathOps.ApproximatelyEqual(baseXY, boundsXY) && (baseXY < boundsXY) == checkLessThan)
        {
            return;
        }

        Span<double> tVals = stackalloc double[3];
        float baseYX = SkPathOpsWindingStatics.PtYx(@base.Pt, dir);
        int roots = SkPathOps.CurveIntercept[(int)_verb * 2 + SkPathOpsWindingStatics.XyIndex(dir)]!(
            _pts, _weight, baseYX, tVals);
        for (int index = 0; index < roots; ++index)
        {
            double t = tVals[index];
            if (@base.Span!.Segment() == this && SkPathOps.ApproximatelyEqual(@base.T, t))
            {
                continue;
            }

            SkDVector slope = default;
            PathPoint pt;
            bool valid = false;
            if (SkPathOps.ApproximatelyZero(t))
            {
                pt = _pts[0];
            }
            else if (SkPathOps.ApproximatelyEqual(t, 1))
            {
                pt = _pts[SkPathOps.SkPathOpsVerbToPoints(_verb)];
            }
            else
            {
                pt = PtAtT(t);
                if (SkDPoint.ApproximatelyEqual(pt, @base.Pt))
                {
                    if (@base.Span.Segment() == this)
                    {
                        continue;
                    }
                }
                else
                {
                    float ptXY = SkPathOpsWindingStatics.PtXy(pt, dir);
                    if (!SkPathOps.ApproximatelyEqual(baseXY, ptXY) && (baseXY < ptXY) == checkLessThan)
                    {
                        continue;
                    }

                    slope = DSlopeAtT(t);
                    if (_verb == PathVerb.Cubic && @base.Span.Segment() == this
                        && SkPathOps.RoughlyEqual(@base.T, t)
                        && SkDPoint.RoughlyEqual(pt, @base.Pt))
                    {
                        continue;
                    }

                    if (Math.Abs(SkPathOpsWindingStatics.PtDydx(slope, dir) * 10000)
                        > Math.Abs(SkPathOpsWindingStatics.PtDxdy(slope, dir)))
                    {
                        valid = true;
                    }
                }
            }

            SkOpSpan? span = WindingSpanAtT(t);
            if (span == null)
            {
                valid = false;
            }
            else if (span.WindValue() == 0 && span.OppValue() == 0)
            {
                continue;
            }

            SkOpRayHit newHit = new SkOpRayHit();
            newHit.Next = hits;
            newHit.Pt = pt;
            newHit.Slope = slope;
            newHit.Span = span;
            newHit.T = t;
            newHit.Valid = valid;
            hits = newHit;
        }
    }

    public SkOpSpan? WindingSpanAtT(double tHit)
    {
        SkOpSpan span = _head;
        SkOpSpanBase next;
        do
        {
            next = span.Next();
            if (SkPathOps.ApproximatelyEqual(tHit, next.T()))
            {
                return null;
            }

            if (tHit < next.T())
            {
                return span;
            }
        }
        while (!next.Final() && (span = next.UpCast()) != null);

        return null;
    }

    public SkOpSpan? FindSortableTop(SkOpContour contourHead)
    {
        SkOpSpan span = _head;
        SkOpSpanBase next;
        do
        {
            next = span.Next();
            if (span.Done())
            {
                continue;
            }

            if (span.WindSum() != SkPathOps.SkMinS32)
            {
                return span;
            }

            if (span.SortableTop(contourHead))
            {
                return span;
            }
        }
        while (!next.Final() && (span = next.UpCast()) != null);

        return null;
    }
}

internal sealed partial class SkOpSpan
{
    public bool SortableTop(SkOpContour contourHead)
    {
        double t = SkPathOpsWindingStatics.GetTGuess(_topTTry++, out int dirOffset);
        SkOpRayHit hitBase = new SkOpRayHit();
        SkOpRayDir dir = hitBase.MakeTestBase(this, t);
        if (hitBase.Slope.X == 0 && hitBase.Slope.Y == 0)
        {
            return false;
        }

        SkOpRayHit? hitHead = hitBase;
        dir = (SkOpRayDir)((int)dir + dirOffset);
        if (hitBase.Span != null && hitBase.Span.Segment().Verb() > PathVerb.Line
            && SkPathOpsWindingStatics.PtDydx(hitBase.Slope, dir) == 0)
        {
            return false;
        }

        SkOpContour? contour = contourHead;
        do
        {
            if (contour.Count() == 0)
            {
                continue;
            }

            contour.RayCheck(hitBase, dir, ref hitHead);
        }
        while ((contour = contour.Next()) != null);

        // sort hits
        List<SkOpRayHit> sorted = new List<SkOpRayHit>();
        SkOpRayHit? hit = hitHead;
        while (hit != null)
        {
            sorted.Add(hit);
            hit = hit.Next;
        }

        int count = sorted.Count;
        SkPathOps.SkTQSort(CollectionsMarshal.AsSpan(sorted),
            SkPathOpsWindingStatics.XyIndex(dir) != 0
                ? SkPathOpsWindingStatics.LessThan(dir)
                    ? SkPathOpsWindingStatics.HitCompareY
                    : SkPathOpsWindingStatics.ReverseHitCompareY
                : SkPathOpsWindingStatics.LessThan(dir)
                    ? SkPathOpsWindingStatics.HitCompareX
                    : SkPathOpsWindingStatics.ReverseHitCompareX);
        // verify windings
        PathPoint? last = null;
        int wind = 0;
        int oppWind = 0;
        for (int index = 0; index < count; ++index)
        {
            hit = sorted[index];
            if (!hit.Valid)
            {
                return false;
            }

            bool ccw = SkPathOpsWindingStatics.CcwDxdy(hit.Slope, dir);
            //        SkASSERT(!approximately_zero(hit->fT) || !hit->fValid);
            SkOpSpan? span = hit.Span;
            if (span == null)
            {
                return false;
            }

            SkOpSegment hitSegment = span.Segment();
            if (span.WindValue() == 0 && span.OppValue() == 0)
            {
                continue;
            }

            if (last != null && SkDPoint.ApproximatelyEqual(last.Value, hit.Pt))
            {
                return false;
            }

            if (index < count - 1)
            {
                PathPoint next = sorted[index + 1].Pt;
                if (SkDPoint.ApproximatelyEqual(next, hit.Pt))
                {
                    return false;
                }
            }

            bool operand = hitSegment.Operand();
            if (operand)
            {
                (wind, oppWind) = (oppWind, wind);
            }

            int lastWind = wind;
            int lastOpp = oppWind;
            int windValue = ccw ? -span.WindValue() : span.WindValue();
            int oppValue = ccw ? -span.OppValue() : span.OppValue();
            wind += windValue;
            oppWind += oppValue;
            bool sumSet = false;
            int spanSum = span.WindSum();
            int windSum = SkOpSegment.UseInnerWinding(lastWind, wind) ? wind : lastWind;
            if (spanSum == SkPathOps.SkMinS32)
            {
                span.SetWindSum(windSum);
                sumSet = true;
            }
            else
            {
                // the need for this condition suggests that UseInnerWinding is flawed
                // happened when last = 1 wind = -1
            }

            int oSpanSum = span.OppSum();
            int oppSum = SkOpSegment.UseInnerWinding(lastOpp, oppWind) ? oppWind : lastOpp;
            if (oSpanSum == SkPathOps.SkMinS32)
            {
                span.SetOppSum(oppSum);
            }

            if (sumSet)
            {
                if (GlobalState().Phase() == SkOpPhase.FixWinding)
                {
                    hitSegment.Contour().SetCcw(ccw ? 1 : 0);
                }
                else
                {
                    _ = hitSegment.MarkAndChaseWinding(span, span.Next(), windSum, oppSum);
                    _ = hitSegment.MarkAndChaseWinding(span.Next(), span, windSum, oppSum);
                }
            }

            if (operand)
            {
                (wind, oppWind) = (oppWind, wind);
            }

            last = hit.Pt;
            GlobalState().BumpNested();
        }

        return true;
    }
}

internal static partial class SkPathOps
{
    public static SkOpSpan? FindSortableTop(SkOpContourHead contourHead)
    {
        for (int index = 0; index < SkOpGlobalState.KMaxWindingTries; ++index)
        {
            SkOpContour? contour = contourHead;
            do
            {
                if (contour.Done())
                {
                    continue;
                }

                SkOpSpan? result = contour.FindSortableTop(contourHead);
                if (result != null)
                {
                    return result;
                }
            }
            while ((contour = contour.Next()) != null);
        }

        return null;
    }
}

file static class SkPathOpsWindingStatics
{
    public static int XyIndex(SkOpRayDir dir)
    {
        return (int)dir & 1;
    }

    public static float PtXy(PathPoint pt, SkOpRayDir dir)
    {
        return XyIndex(dir) == 0 ? pt.X : pt.Y;
    }

    public static float PtYx(PathPoint pt, SkOpRayDir dir)
    {
        return XyIndex(dir) == 0 ? pt.Y : pt.X;
    }

    public static double PtDxdy(in SkDVector v, SkOpRayDir dir)
    {
        return XyIndex(dir) == 0 ? v.X : v.Y;
    }

    public static double PtDydx(in SkDVector v, SkOpRayDir dir)
    {
        return XyIndex(dir) == 0 ? v.Y : v.X;
    }

    public static float RectSide(in SkPathOpsBounds r, SkOpRayDir dir)
    {
        switch (dir)
        {
            case SkOpRayDir.Left:
                return r.Left;
            case SkOpRayDir.Top:
                return r.Top;
            case SkOpRayDir.Right:
                return r.Right;
            default:
                return r.Bottom;
        }
    }

    public static bool SidewaysOverlap(in SkPathOpsBounds rect, PathPoint pt, SkOpRayDir dir)
    {
        int i = XyIndex(dir) == 0 ? 1 : 0;
        return i == 0
            ? SkPathOps.ApproximatelyBetween(rect.Left, pt.X, rect.Right)
            : SkPathOps.ApproximatelyBetween(rect.Top, pt.Y, rect.Bottom);
    }

    public static bool LessThan(SkOpRayDir dir)
    {
        return ((int)dir & 2) == 0;
    }

    public static bool CcwDxdy(in SkDVector v, SkOpRayDir dir)
    {
        bool vPartPos = PtDydx(v, dir) > 0;
        bool leftBottom = (((int)dir + 1) & 2) != 0;
        return vPartPos == leftBottom;
    }

    public static bool HitCompareX(SkOpRayHit a, SkOpRayHit b)
    {
        return a.Pt.X < b.Pt.X;
    }

    public static bool ReverseHitCompareX(SkOpRayHit a, SkOpRayHit b)
    {
        return b.Pt.X < a.Pt.X;
    }

    public static bool HitCompareY(SkOpRayHit a, SkOpRayHit b)
    {
        return a.Pt.Y < b.Pt.Y;
    }

    public static bool ReverseHitCompareY(SkOpRayHit a, SkOpRayHit b)
    {
        return b.Pt.Y < a.Pt.Y;
    }

    public static double GetTGuess(int tTry, out int dirOffset)
    {
        double t = 0.5;
        dirOffset = tTry & 1;
        int tBase = tTry >> 1;
        int tBits = 0;
        while ((tTry >>= 1) != 0)
        {
            t /= 2;
            ++tBits;
        }

        if (tBits != 0)
        {
            int tIndex = (tBase - 1) & ((1 << tBits) - 1);
            t += t * 2 * tIndex;
        }

        return t;
    }
}
