namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkOpAngle.h and SkOpAngle.cpp at chrome/m119 (the Skia inside
// SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types are
// kept. SkOpAngle is a class (arena-allocated, linked through fNext). C++ int/bool conversions are spelled out:
// a function returning bool that returns an int returns `value != 0`, and `return x < 0;` from an int function
// returns 1 or 0. The local SkDVector pointers of checkParallel (sweep/tweep) only ever read element 0, so they are
// copies. COMPARE_RESULT(n, compare) is `compare` in release builds. debug*, dump*, fID and fCheckCoincidence (read
// only by debugCheckCoincidence) are omitted.

internal sealed class SkOpAngle
{
    public enum IncludeType
    {
        UnaryWinding,
        UnaryXor,
        BinarySingle,
        BinaryOpp,
    }

    private SkDCurve _originalCurvePart; // the curve from start to end
    private SkDCurveSweep _part; // the curve from start to end offset as needed
    private double _side;
    private SkLineParameters _tangentHalf; // used only to sort a pair of lines or line-like sections
    private SkOpAngle? _next;
    private SkOpSpanBase? _lastMarked;
    private SkOpSpanBase _start = null!;
    private SkOpSpanBase _end = null!;
    private SkOpSpanBase _computedEnd = null!;
    private int _sectorMask;
    private sbyte _sectorStart; // in 32nds of a circle
    private sbyte _sectorEnd;
    private bool _unorderable;
    private bool _computeSector;
    private bool _computedSector;
    private bool _tangentsAmbiguous;

    public SkOpSpanBase End()
    {
        return _end;
    }

    public SkOpAngle? Next()
    {
        return _next;
    }

    public void SetLastMarked(SkOpSpanBase? marked)
    {
        _lastMarked = marked;
    }

    public SkOpSpanBase Start()
    {
        return _start;
    }

    public bool TangentsAmbiguous()
    {
        return _tangentsAmbiguous;
    }

    public bool Unorderable()
    {
        return _unorderable;
    }

    // SkOpAngle.cpp

    /* Angles are sorted counterclockwise. The smallest angle has a positive x and the smallest
       positive y. The largest angle has a positive x and a zero y. */

    /*             quarter angle values for sector

    31   x > 0, y == 0              horizontal line (to the right)
    0    x > 0, y == epsilon        quad/cubic horizontal tangent eventually going +y
    1    x > 0, y > 0, x > y        nearer horizontal angle
    2                  x + e == y   quad/cubic 45 going horiz
    3    x > 0, y > 0, x == y       45 angle
    4                  x == y + e   quad/cubic 45 going vert
    5    x > 0, y > 0, x < y        nearer vertical angle
    6    x == epsilon, y > 0        quad/cubic vertical tangent eventually going +x
    7    x == 0, y > 0              vertical line (to the top)

                                          8  7  6
                                     9       |       5
                                  10         |          4
                                11           |            3
                              12  \          |           / 2
                             13              |              1
                            14               |               0
                            15 --------------+------------- 31
                            16               |              30
                             17              |             29
                              18  /          |          \ 28
                                19           |           27
                                  20         |         26
                                     21      |      25
                                         22 23 24
    */

    // return true if lh < this < rh
    private bool After(SkOpAngle test)
    {
        SkOpAngle lh = test;
        SkOpAngle rh = lh._next!;
        _part.Curve = _originalCurvePart;
        // Adjust lh and rh to share the same origin (floating point error in intersections can mean
        // they aren't exactly the same).
        lh._part.Curve = lh._originalCurvePart;
        lh._part.Curve[0] = _part.Curve[0];
        rh._part.Curve = rh._originalCurvePart;
        rh._part.Curve[0] = _part.Curve[0];

        if (lh._computeSector && !lh.ComputeSector())
        {
            return true;
        }

        if (_computeSector && !ComputeSector())
        {
            return true;
        }

        if (rh._computeSector && !rh.ComputeSector())
        {
            return true;
        }

        bool ltrOverlap = ((lh._sectorMask | rh._sectorMask) & _sectorMask) != 0;
        bool lrOverlap = (lh._sectorMask & rh._sectorMask) != 0;
        int lrOrder; // set to -1 if either order works
        if (!lrOverlap)
        {
            // no lh/rh sector overlap
            if (!ltrOverlap)
            {
                // no lh/this/rh sector overlap
                return (lh._sectorEnd > rh._sectorStart)
                    ^ (_sectorStart > lh._sectorEnd)
                    ^ (_sectorStart > rh._sectorStart);
            }

            int lrGap = (rh._sectorStart - lh._sectorStart + 32) & 0x1f;
            /* A tiny change can move the start +/- 4. The order can only be determined if
               lr gap is not 12 to 20 or -12 to -20.
                   -31 ..-21      1
                   -20 ..-12     -1
                   -11 .. -1      0
                     0          shouldn't get here
                    11 ..  1      1
                    12 .. 20     -1
                    21 .. 31      0
             */
            lrOrder = lrGap > 20 ? 0 : lrGap > 11 ? -1 : 1;
        }
        else
        {
            lrOrder = lh.Orderable(rh);
            if (!ltrOverlap && lrOrder >= 0)
            {
                return lrOrder == 0;
            }
        }

        int ltOrder;
        if ((lh._sectorMask & _sectorMask) != 0)
        {
            ltOrder = lh.Orderable(this);
        }
        else
        {
            int ltGap = (_sectorStart - lh._sectorStart + 32) & 0x1f;
            ltOrder = ltGap > 20 ? 0 : ltGap > 11 ? -1 : 1;
        }

        int trOrder;
        if ((rh._sectorMask & _sectorMask) != 0)
        {
            trOrder = Orderable(rh);
        }
        else
        {
            int trGap = (rh._sectorStart - _sectorStart + 32) & 0x1f;
            trOrder = trGap > 20 ? 0 : trGap > 11 ? -1 : 1;
        }

        AlignmentSameSide(lh, ref ltOrder);
        AlignmentSameSide(rh, ref trOrder);
        if (lrOrder >= 0 && ltOrder >= 0 && trOrder >= 0)
        {
            return (lrOrder != 0 ? (ltOrder & trOrder) : (ltOrder | trOrder)) != 0;
        }

        //    SkASSERT(lrOrder >= 0 || ltOrder >= 0 || trOrder >= 0);
        // There's not enough information to sort. Get the pairs of angles in opposite planes.
        // If an order is < 0, the pair is already in an opposite plane. Check the remaining pairs.
        // FIXME : once all variants are understood, rewrite this more simply
        if (ltOrder == 0 && lrOrder == 0)
        {
            // FIXME : once this is verified to work, remove one opposite angle call
            bool ltOpposite = lh.OppositePlanes(this);
            return ltOpposite;
        }
        else if (ltOrder == 1 && trOrder == 0)
        {
            bool trOpposite = OppositePlanes(rh);
            return trOpposite;
        }
        else if (lrOrder == 1 && trOrder == 1)
        {
            //        SkDEBUGCODE(bool trOpposite = oppositePlanes(rh));
            bool lrOpposite = lh.OppositePlanes(rh);
            //        SkASSERT(lrOpposite != trOpposite);
            return lrOpposite;
        }

        // If a pair couldn't be ordered, there's not enough information to determine the sort.
        // Refer to:  https://docs.google.com/drawings/d/1KV-8SJTedku9fj4K6fd1SB-8divuV_uivHVsSgwXICQ
        if (_unorderable || lh._unorderable || rh._unorderable)
        {
            // limit to lines; should work with curves, but wait for a failing test to verify
            if (!_part.IsCurve() && !lh._part.IsCurve() && !rh._part.IsCurve())
            {
                // see if original raw data is orderable
                // if two share a point, check if third has both points in same half plane
                int ltShare = lh._originalCurvePart[0] == _originalCurvePart[0] ? 1 : 0;
                int lrShare = lh._originalCurvePart[0] == rh._originalCurvePart[0] ? 1 : 0;
                int trShare = _originalCurvePart[0] == rh._originalCurvePart[0] ? 1 : 0;
                // if only one pair are the same, the third point touches neither of the pair
                if (ltShare + lrShare + trShare == 1)
                {
                    if (lrShare != 0)
                    {
                        int ltOOrder = lh.LinesOnOriginalSide(this);
                        int rtOOrder = rh.LinesOnOriginalSide(this);
                        if ((rtOOrder ^ ltOOrder) == 1)
                        {
                            return ltOOrder != 0;
                        }
                    }
                    else if (trShare != 0)
                    {
                        int tlOOrder = LinesOnOriginalSide(lh);
                        int rlOOrder = rh.LinesOnOriginalSide(lh);
                        if ((tlOOrder ^ rlOOrder) == 1)
                        {
                            return rlOOrder != 0;
                        }
                    }
                    else
                    {
                        int trOOrder = rh.LinesOnOriginalSide(this);
                        int lrOOrder = lh.LinesOnOriginalSide(rh);
                        // result must be 0 and 1 or 1 and 0 to be valid
                        if ((lrOOrder ^ trOOrder) == 1)
                        {
                            return trOOrder != 0;
                        }
                    }
                }
            }
        }

        if (lrOrder < 0)
        {
            if (ltOrder < 0)
            {
                return trOrder != 0;
            }

            return ltOrder != 0;
        }

        return lrOrder == 0;
    }

    private int LineOnOneSide(in SkDPoint origin, in SkDVector line, SkOpAngle test, bool useOriginal)
    {
        Span<double> crosses = stackalloc double[3];
        PathVerb testVerb = test.Segment().Verb();
        int iMax = SkPathOps.SkPathOpsVerbToPoints(testVerb);
        //    SkASSERT(origin == test.fCurveHalf[0]);
        ref readonly SkDCurve testCurve = ref useOriginal ? ref test._originalCurvePart : ref test._part.Curve;
        for (int index = 1; index <= iMax; ++index)
        {
            double xy1 = line.X * (testCurve.Cubic.Pts[index].Y - origin.Y);
            double xy2 = line.Y * (testCurve.Cubic.Pts[index].X - origin.X);
            crosses[index - 1] = SkPathOps.AlmostBequalUlps(xy1, xy2) ? 0 : xy1 - xy2;
        }

        if (crosses[0] * crosses[1] < 0)
        {
            return -1;
        }

        if (PathVerb.Cubic == testVerb)
        {
            if (crosses[0] * crosses[2] < 0 || crosses[1] * crosses[2] < 0)
            {
                return -1;
            }
        }

        if (crosses[0] != 0)
        {
            return crosses[0] < 0 ? 1 : 0;
        }

        if (crosses[1] != 0)
        {
            return crosses[1] < 0 ? 1 : 0;
        }

        if (PathVerb.Cubic == testVerb && crosses[2] != 0)
        {
            return crosses[2] < 0 ? 1 : 0;
        }

        return -2;
    }

    // given a line, see if the opposite curve's convex hull is all on one side
    // returns -1=not on one side    0=this CW of test   1=this CCW of test
    private int LineOnOneSide(SkOpAngle test, bool useOriginal)
    {
        SkDPoint origin = _part.Curve[0];
        SkDVector line = _part.Curve[1] - origin;
        int result = LineOnOneSide(origin, line, test, useOriginal);
        if (-2 == result)
        {
            _unorderable = true;
            result = -1;
        }

        return result;
    }

    // experiment works only with lines for now
    private int LinesOnOriginalSide(SkOpAngle test)
    {
        SkDPoint origin = _originalCurvePart[0];
        SkDVector line = _originalCurvePart[1] - origin;
        Span<double> dots = stackalloc double[2];
        Span<double> crosses = stackalloc double[2];
        ref readonly SkDCurve testCurve = ref test._originalCurvePart;
        for (int index = 0; index < 2; ++index)
        {
            SkDVector testLine = testCurve.Cubic.Pts[index] - origin;
            double xy1 = line.X * testLine.Y;
            double xy2 = line.Y * testLine.X;
            dots[index] = line.X * testLine.X + line.Y * testLine.Y;
            crosses[index] = SkPathOps.AlmostBequalUlps(xy1, xy2) ? 0 : xy1 - xy2;
        }

        if (crosses[0] * crosses[1] < 0)
        {
            return -1;
        }

        if (crosses[0] != 0)
        {
            return crosses[0] < 0 ? 1 : 0;
        }

        if (crosses[1] != 0)
        {
            return crosses[1] < 0 ? 1 : 0;
        }

        if ((dots[0] == 0 && dots[1] < 0) || (dots[0] < 0 && dots[1] == 0))
        {
            return 2; // 180 degrees apart
        }

        _unorderable = true;
        return -1;
    }

    // To sort the angles, all curves are translated to have the same starting point.
    // If the curve's control point in its original position is on one side of a compared line,
    // and translated is on the opposite side, reverse the previously computed order.
    private void AlignmentSameSide(SkOpAngle test, ref int order)
    {
        if (order < 0)
        {
            return;
        }

        if (_part.IsCurve())
        {
            // This should support all curve types, but only bug that requires this has lines
            // Turning on for curves causes existing tests to fail
            return;
        }

        if (test._part.IsCurve())
        {
            return;
        }

        SkDPoint xOrigin = test._part.Curve.Line[0];
        SkDPoint oOrigin = test._originalCurvePart.Line[0];
        if (xOrigin == oOrigin)
        {
            return;
        }

        int iMax = SkPathOps.SkPathOpsVerbToPoints(Segment().Verb());
        SkDVector xLine = test._part.Curve.Line[1] - xOrigin;
        SkDVector oLine = test._originalCurvePart.Line[1] - oOrigin;
        for (int index = 1; index <= iMax; ++index)
        {
            SkDPoint testPt = _part.Curve[index];
            double xCross = oLine.CrossCheck(testPt - xOrigin);
            double oCross = xLine.CrossCheck(testPt - oOrigin);
            if (oCross * xCross < 0)
            {
                order ^= 1;
                break;
            }
        }
    }

    private bool CheckCrossesZero()
    {
        int start = SkPathOps.StdMin((int)_sectorStart, (int)_sectorEnd);
        int end = SkPathOps.StdMax((int)_sectorStart, (int)_sectorEnd);
        bool crossesZero = end - start > 16;
        return crossesZero;
    }

    private bool CheckParallel(SkOpAngle rh)
    {
        SkDVector sweep;
        SkDVector tweep;
        if (_part.IsOrdered())
        {
            sweep = _part.Sweep[0];
        }
        else
        {
            sweep = _part.Curve[1] - _part.Curve[0];
        }

        if (rh._part.IsOrdered())
        {
            tweep = rh._part.Sweep[0];
        }
        else
        {
            tweep = rh._part.Curve[1] - rh._part.Curve[0];
        }

        double s0xt0 = sweep.CrossCheck(tweep);
        if (TangentsDiverge(rh, s0xt0))
        {
            return s0xt0 < 0;
        }

        // compute the perpendicular to the endpoints and see where it intersects the opposite curve
        // if the intersections within the t range, do a cross check on those
        bool inside = false;
        if (!_end.Contains(rh._end))
        {
            if (EndToSide(rh, ref inside))
            {
                return inside;
            }

            if (rh.EndToSide(this, ref inside))
            {
                return !inside;
            }
        }

        if (MidToSide(rh, ref inside))
        {
            return inside;
        }

        if (rh.MidToSide(this, ref inside))
        {
            return !inside;
        }

        // compute the cross check from the mid T values (last resort)
        SkDVector m0 = Segment().DPtAtT(MidT()) - _part.Curve[0];
        SkDVector m1 = rh.Segment().DPtAtT(rh.MidT()) - rh._part.Curve[0];
        double m0xm1 = m0.CrossCheck(m1);
        if (m0xm1 == 0)
        {
            _unorderable = true;
            rh._unorderable = true;
            return true;
        }

        return m0xm1 < 0;
    }

    // the original angle is too short to get meaningful sector information
    // lengthen it until it is long enough to be meaningful or leave it unset if lengthening it
    // would cause it to intersect one of the adjacent angles
    private bool ComputeSector()
    {
        if (_computedSector)
        {
            return !_unorderable;
        }

        _computedSector = true;
        bool stepUp = _start.T() < _end.T();
        SkOpSpanBase? checkEnd = _end;
        if (checkEnd.Final() && stepUp)
        {
            _unorderable = true;
            return false;
        }

        do
        {
            // advance end
            SkOpSegment other = checkEnd.Segment();
            SkOpSpanBase oSpan = other.Head();
            do
            {
                if (oSpan.Segment() != Segment())
                {
                    continue;
                }

                if (oSpan == checkEnd)
                {
                    continue;
                }

                if (!SkPathOps.ApproximatelyEqual(oSpan.T(), checkEnd.T()))
                {
                    continue;
                }

                goto recomputeSector;
            }
            while (!oSpan.Final() && (oSpan = oSpan.UpCast().Next()) != null);

            checkEnd = stepUp
                ? !checkEnd.Final() ? checkEnd.UpCast().Next() : null
                : checkEnd.Prev();
        }
        while (checkEnd != null);

    recomputeSector:
        SkOpSpanBase computedEnd = stepUp
            ? checkEnd != null ? checkEnd.Prev()! : _end.Segment().Head()
            : checkEnd != null ? checkEnd.UpCast().Next() : _end.Segment().Tail();
        if (checkEnd == _end || computedEnd == _end || computedEnd == _start)
        {
            _unorderable = true;
            return false;
        }

        if (stepUp != (_start.T() < computedEnd.T()))
        {
            _unorderable = true;
            return false;
        }

        SkOpSpanBase saveEnd = _end;
        _computedEnd = _end = computedEnd;
        SetSpans();
        SetSector();
        _end = saveEnd;
        return !_unorderable;
    }

    private int ConvexHullOverlaps(SkOpAngle rh)
    {
        ref readonly SkDVector2 sweep = ref _part.Sweep;
        ref readonly SkDVector2 tweep = ref rh._part.Sweep;
        double s0xs1 = sweep[0].CrossCheck(sweep[1]);
        double s0xt0 = sweep[0].CrossCheck(tweep[0]);
        double s1xt0 = sweep[1].CrossCheck(tweep[0]);
        bool tBetweenS = s0xs1 > 0 ? s0xt0 > 0 && s1xt0 < 0 : s0xt0 < 0 && s1xt0 > 0;
        double s0xt1 = sweep[0].CrossCheck(tweep[1]);
        double s1xt1 = sweep[1].CrossCheck(tweep[1]);
        tBetweenS |= s0xs1 > 0 ? s0xt1 > 0 && s1xt1 < 0 : s0xt1 < 0 && s1xt1 > 0;
        double t0xt1 = tweep[0].CrossCheck(tweep[1]);
        if (tBetweenS)
        {
            return -1;
        }

        if ((s0xt0 == 0 && s1xt1 == 0) || (s1xt0 == 0 && s0xt1 == 0))
        {
            // s0 to s1 equals t0 to t1
            return -1;
        }

        bool sBetweenT = t0xt1 > 0 ? s0xt0 < 0 && s0xt1 > 0 : s0xt0 > 0 && s0xt1 < 0;
        sBetweenT |= t0xt1 > 0 ? s1xt0 < 0 && s1xt1 > 0 : s1xt0 > 0 && s1xt1 < 0;
        if (sBetweenT)
        {
            return -1;
        }

        // if all of the sweeps are in the same half plane, then the order of any pair is enough
        if (s0xt0 >= 0 && s0xt1 >= 0 && s1xt0 >= 0 && s1xt1 >= 0)
        {
            return 0;
        }

        if (s0xt0 <= 0 && s0xt1 <= 0 && s1xt0 <= 0 && s1xt1 <= 0)
        {
            return 1;
        }

        // if the outside sweeps are greater than 180 degress:
        // first assume the inital tangents are the ordering
        // if the midpoint direction matches the inital order, that is enough
        SkDVector m0 = Segment().DPtAtT(MidT()) - _part.Curve[0];
        SkDVector m1 = rh.Segment().DPtAtT(rh.MidT()) - rh._part.Curve[0];
        double m0xm1 = m0.CrossCheck(m1);
        if (s0xt0 > 0 && m0xm1 > 0)
        {
            return 0;
        }

        if (s0xt0 < 0 && m0xm1 < 0)
        {
            return 1;
        }

        if (TangentsDiverge(rh, s0xt0))
        {
            return s0xt0 < 0 ? 1 : 0;
        }

        return m0xm1 < 0 ? 1 : 0;
    }

    // OPTIMIZATION: longest can all be either lazily computed here or precomputed in setup
    public double DistEndRatio(double dist)
    {
        double longest = 0;
        SkOpSegment segment = Segment();
        int ptCount = SkPathOps.SkPathOpsVerbToPoints(segment.Verb());
        ReadOnlySpan<PathPoint> pts = segment.Pts();
        for (int idx1 = 0; idx1 <= ptCount - 1; ++idx1)
        {
            for (int idx2 = idx1 + 1; idx2 <= ptCount; ++idx2)
            {
                if (idx1 == idx2)
                {
                    continue;
                }

                SkDVector v = default;
                v.Set(pts[idx2] - pts[idx1]);
                double lenSq = v.LengthSquared();
                longest = SkPathOps.StdMax(longest, lenSq);
            }
        }

        return Math.Sqrt(longest) / dist;
    }

    private bool EndsIntersect(SkOpAngle rh)
    {
        PathVerb lVerb = Segment().Verb();
        PathVerb rVerb = rh.Segment().Verb();
        int lPts = SkPathOps.SkPathOpsVerbToPoints(lVerb);
        int rPts = SkPathOps.SkPathOpsVerbToPoints(rVerb);
        Span<SkDLine> rays = stackalloc SkDLine[2];
        rays[0] = new SkDLine(_part.Curve[0], rh._part.Curve[rPts]);
        rays[1] = new SkDLine(_part.Curve[0], _part.Curve[lPts]);
        if (_end.Contains(rh._end))
        {
            return CheckParallel(rh);
        }

        Span<double> smallTs = stackalloc double[2] { -1, -1 };
        Span<bool> limited = stackalloc bool[2] { false, false };
        for (int index = 0; index < 2; ++index)
        {
            PathVerb cVerb = index != 0 ? rVerb : lVerb;
            // if the curve is a line, then the line and the ray intersect only at their crossing
            if (cVerb == PathVerb.Line)
            {
                continue;
            }

            SkOpSegment segment = index != 0 ? rh.Segment() : Segment();
            SkIntersections i = new SkIntersections();
            SkPathOps.CurveIntersectRay[(int)cVerb]!(segment.Pts(), segment.Weight(), rays[index], i);
            double tStart = index != 0 ? rh._start.T() : _start.T();
            double tEnd = index != 0 ? rh._computedEnd.T() : _computedEnd.T();
            bool testAscends = tStart < (index != 0 ? rh._computedEnd.T() : _computedEnd.T());
            double t = testAscends ? 0 : 1;
            for (int idx2 = 0; idx2 < i.Used(); ++idx2)
            {
                double testT = i[0][idx2];
                if (!SkPathOps.ApproximatelyBetweenOrderable(tStart, testT, tEnd))
                {
                    continue;
                }

                if (SkPathOps.ApproximatelyEqualOrderable(tStart, testT))
                {
                    continue;
                }

                smallTs[index] = t = testAscends ? SkPathOps.StdMax(t, testT) : SkPathOps.StdMin(t, testT);
                limited[index] = SkPathOps.ApproximatelyEqualOrderable(t, tEnd);
            }
        }

        bool sRayLonger = false;
        SkDVector sCept = new SkDVector(0, 0);
        double sCeptT = -1;
        int sIndex = -1;
        bool useIntersect = false;
        for (int index = 0; index < 2; ++index)
        {
            if (smallTs[index] < 0)
            {
                continue;
            }

            SkOpSegment segment = index != 0 ? rh.Segment() : Segment();
            SkDPoint dPt = segment.DPtAtT(smallTs[index]);
            SkDVector cept = dPt - rays[index][0];
            // If this point is on the curve, it should have been detected earlier by ordinary
            // curve intersection. This may be hard to determine in general, but for lines,
            // the point could be close to or equal to its end, but shouldn't be near the start.
            if ((index != 0 ? lPts : rPts) == 1)
            {
                SkDVector total = rays[index][1] - rays[index][0];
                if (cept.LengthSquared() * 2 < total.LengthSquared())
                {
                    continue;
                }
            }

            SkDVector end = rays[index][1] - rays[index][0];
            if (cept.X * end.X < 0 || cept.Y * end.Y < 0)
            {
                continue;
            }

            double rayDist = cept.Length();
            double endDist = end.Length();
            bool rayLonger = rayDist > endDist;
            if (limited[0] && limited[1] && rayLonger)
            {
                useIntersect = true;
                sRayLonger = rayLonger;
                sCept = cept;
                sCeptT = smallTs[index];
                sIndex = index;
                break;
            }

            double delta = Math.Abs(rayDist - endDist);
            double minX, minY, maxX, maxY;
            minX = minY = SkPathOps.SkScalarInfinity;
            maxX = maxY = -SkPathOps.SkScalarInfinity;
            ref readonly SkDCurve curve = ref index != 0 ? ref rh._part.Curve : ref _part.Curve;
            int ptCount = index != 0 ? rPts : lPts;
            for (int idx2 = 0; idx2 <= ptCount; ++idx2)
            {
                minX = SkPathOps.StdMin(minX, curve.Cubic.Pts[idx2].X);
                minY = SkPathOps.StdMin(minY, curve.Cubic.Pts[idx2].Y);
                maxX = SkPathOps.StdMax(maxX, curve.Cubic.Pts[idx2].X);
                maxY = SkPathOps.StdMax(maxY, curve.Cubic.Pts[idx2].Y);
            }

            double maxWidth = SkPathOps.StdMax(maxX - minX, maxY - minY);
            delta = SkPathOps.SkIeeeDoubleDivide(delta, maxWidth);
            // FIXME: move these magic numbers
            // This fixes skbug.com/8380
            // Larger changes (like changing the constant in the next block) cause other
            // tests to fail as documented in the bug.
            // This could probably become a more general test: e.g., if translating the
            // curve causes the cross product of any control point or end point to change
            // sign with regard to the opposite curve's hull, treat the curves as parallel.

            // Moreso, this points to the general fragility of this approach of assigning
            // winding by sorting the angles of curves sharing a common point, as mentioned
            // in the bug.
            if (delta < 4e-3 && delta > 1e-3 && !useIntersect && _part.IsCurve()
                && rh._part.IsCurve() && _originalCurvePart[0] != _part.Curve.Line[0])
            {
                // see if original curve is on one side of hull; translated is on the other
                SkDPoint origin = rh._originalCurvePart[0];
                int count = SkPathOps.SkPathOpsVerbToPoints(rh.Segment().Verb());
                SkDVector line = rh._originalCurvePart[count] - origin;
                int originalSide = rh.LineOnOneSide(origin, line, this, true);
                if (originalSide >= 0)
                {
                    int translatedSide = rh.LineOnOneSide(origin, line, this, false);
                    if (originalSide != translatedSide)
                    {
                        continue;
                    }
                }
            }

            if (delta > 1e-3 && (useIntersect ^= true))
            {
                sRayLonger = rayLonger;
                sCept = cept;
                sCeptT = smallTs[index];
                sIndex = index;
            }
        }

        if (useIntersect)
        {
            ref readonly SkDCurve curve = ref sIndex != 0 ? ref rh._part.Curve : ref _part.Curve;
            SkOpSegment segment = sIndex != 0 ? rh.Segment() : Segment();
            double tStart = sIndex != 0 ? rh._start.T() : _start.T();
            SkDVector mid = segment.DPtAtT(tStart + (sCeptT - tStart) / 2) - curve.Cubic.Pts[0];
            double septDir = mid.CrossCheck(sCept);
            if (septDir == 0)
            {
                return CheckParallel(rh);
            }

            return sRayLonger ^ (sIndex == 0) ^ (septDir < 0);
        }
        else
        {
            return CheckParallel(rh);
        }
    }

    private bool EndToSide(SkOpAngle rh, ref bool inside)
    {
        SkOpSegment segment = Segment();
        PathVerb verb = segment.Verb();
        SkDLine rayEnd = default;
        rayEnd[0].Set(_end.Pt());
        rayEnd[1] = rayEnd[0];
        SkDVector slopeAtEnd = SkPathOps.CurveDSlopeAtT[(int)verb]!(segment.Pts(), segment.Weight(), _end.T());
        rayEnd[1].X += slopeAtEnd.Y;
        rayEnd[1].Y -= slopeAtEnd.X;
        SkIntersections iEnd = new SkIntersections();
        SkOpSegment oppSegment = rh.Segment();
        PathVerb oppVerb = oppSegment.Verb();
        SkPathOps.CurveIntersectRay[(int)oppVerb]!(oppSegment.Pts(), oppSegment.Weight(), rayEnd, iEnd);
        int closestEnd = iEnd.ClosestTo(rh._start.T(), rh._end.T(), rayEnd[0], out double endDist);
        if (closestEnd < 0)
        {
            return false;
        }

        if (endDist == 0)
        {
            return false;
        }

        SkDPoint start = default;
        start.Set(_start.Pt());
        // OPTIMIZATION: multiple times in the code we find the max scalar
        double minX, minY, maxX, maxY;
        minX = minY = SkPathOps.SkScalarInfinity;
        maxX = maxY = -SkPathOps.SkScalarInfinity;
        ref readonly SkDCurve curve = ref rh._part.Curve;
        int oppPts = SkPathOps.SkPathOpsVerbToPoints(oppVerb);
        for (int idx2 = 0; idx2 <= oppPts; ++idx2)
        {
            minX = SkPathOps.StdMin(minX, curve.Cubic.Pts[idx2].X);
            minY = SkPathOps.StdMin(minY, curve.Cubic.Pts[idx2].Y);
            maxX = SkPathOps.StdMax(maxX, curve.Cubic.Pts[idx2].X);
            maxY = SkPathOps.StdMax(maxY, curve.Cubic.Pts[idx2].Y);
        }

        double maxWidth = SkPathOps.StdMax(maxX - minX, maxY - minY);
        endDist = SkPathOps.SkIeeeDoubleDivide(endDist, maxWidth);
        if (!(endDist >= 5e-12))
        {
            // empirically found
            return false; // ! above catches NaN
        }

        SkDPoint endPt = rayEnd[0];
        SkDPoint oppPt = iEnd.Pt(closestEnd);
        SkDVector vLeft = endPt - start;
        SkDVector vRight = oppPt - start;
        double dir = vLeft.CrossNoNormalCheck(vRight);
        if (dir == 0)
        {
            return false;
        }

        inside = dir < 0;
        return true;
    }

    /*      y<0 y==0 y>0  x<0 x==0 x>0 xy<0 xy==0 xy>0
        0    x                      x               x
        1    x                      x          x
        2    x                      x    x
        3    x                  x        x
        4    x             x             x
        5    x             x                   x
        6    x             x                        x
        7         x        x                        x
        8             x    x                        x
        9             x    x                   x
        10            x    x             x
        11            x         x        x
        12            x             x    x
        13            x             x          x
        14            x             x               x
        15        x                 x               x
    */
    private int FindSector(PathVerb verb, double x, double y)
    {
        double absX = Math.Abs(x);
        double absY = Math.Abs(y);
        double xy = PathVerb.Line == verb || !SkPathOps.AlmostEqualUlps(absX, absY) ? absX - absY : 0;
        // If there are four quadrants and eight octants, and since the Latin for sixteen is sedecim,
        // one could coin the term sedecimant for a space divided into 16 sections.
        // http://english.stackexchange.com/questions/133688/word-for-something-partitioned-into-16-parts
        int sector = SkOpAngleStatics.Sedecimant[
            (xy >= 0 ? 1 : 0) + (xy > 0 ? 1 : 0),
            (y >= 0 ? 1 : 0) + (y > 0 ? 1 : 0),
            (x >= 0 ? 1 : 0) + (x > 0 ? 1 : 0)] * 2 + 1;
        //    SkASSERT(SkPath::kLine_Verb == verb || sector >= 0);
        return sector;
    }

    private SkOpGlobalState GlobalState()
    {
        return Segment().GlobalState();
    }

    // OPTIMIZE: if this loops to only one other angle, after first compare fails, insert on other side
    // OPTIMIZE: return where insertion succeeded. Then, start next insertion on opposite side
    public bool Insert(SkOpAngle angle)
    {
        if (angle._next != null)
        {
            if (LoopCount() >= angle.LoopCount())
            {
                if (!Merge(angle))
                {
                    return true;
                }
            }
            else if (_next != null)
            {
                if (!angle.Merge(this))
                {
                    return true;
                }
            }
            else
            {
                angle.Insert(this);
            }

            return true;
        }

        bool singleton = null == _next;
        if (singleton)
        {
            _next = this;
        }

        SkOpAngle next = _next!;
        if (next._next == this)
        {
            if (singleton || angle.After(this))
            {
                _next = angle;
                angle._next = next;
            }
            else
            {
                next._next = angle;
                angle._next = this;
            }

            return true;
        }

        SkOpAngle last = this;
        bool flipAmbiguity = false;
        do
        {
            if (angle.After(last) ^ (angle.TangentsAmbiguous() & flipAmbiguity))
            {
                last._next = angle;
                angle._next = next;
                break;
            }

            last = next;
            if (last == this)
            {
                if (flipAmbiguity)
                {
                    return false;
                }

                // We're in a loop. If a sort was ambiguous, flip it to end the loop.
                flipAmbiguity = true;
            }

            next = next._next!;
        }
        while (true);

        return true;
    }

    public SkOpSpanBase? LastMarked()
    {
        if (_lastMarked != null)
        {
            if (_lastMarked.Chased())
            {
                return null;
            }

            _lastMarked.SetChased(true);
        }

        return _lastMarked;
    }

    public bool LoopContains(SkOpAngle angle)
    {
        if (_next == null)
        {
            return false;
        }

        SkOpAngle first = this;
        SkOpAngle loop = this;
        SkOpSegment tSegment = angle._start.Segment();
        double tStart = angle._start.T();
        double tEnd = angle._end.T();
        do
        {
            SkOpSegment lSegment = loop._start.Segment();
            if (lSegment != tSegment)
            {
                continue;
            }

            double lStart = loop._start.T();
            if (lStart != tEnd)
            {
                continue;
            }

            double lEnd = loop._end.T();
            if (lEnd == tStart)
            {
                return true;
            }
        }
        while ((loop = loop._next!) != first);

        return false;
    }

    public int LoopCount()
    {
        int count = 0;
        SkOpAngle first = this;
        SkOpAngle? next = this;
        do
        {
            next = next._next;
            ++count;
        }
        while (next != null && next != first);

        return count;
    }

    private bool Merge(SkOpAngle angle)
    {
        SkOpAngle working = angle;
        do
        {
            if (this == working)
            {
                return false;
            }

            working = working._next!;
        }
        while (working != angle);

        do
        {
            SkOpAngle next = working._next!;
            working._next = null;
            Insert(working);
            working = next;
        }
        while (working != angle);

        // it's likely that a pair of the angles are unorderable
        return true;
    }

    private double MidT()
    {
        return (_start.T() + _end.T()) / 2;
    }

    private bool MidToSide(SkOpAngle rh, ref bool inside)
    {
        SkOpSegment segment = Segment();
        PathVerb verb = segment.Verb();
        PathPoint startPt = _start.Pt();
        PathPoint endPt = _end.Pt();
        SkDPoint dStartPt = default;
        dStartPt.Set(startPt);
        SkDLine rayMid = default;
        rayMid[0].X = (startPt.X + endPt.X) / 2;
        rayMid[0].Y = (startPt.Y + endPt.Y) / 2;
        rayMid[1].X = rayMid[0].X + (endPt.Y - startPt.Y);
        rayMid[1].Y = rayMid[0].Y - (endPt.X - startPt.X);
        SkIntersections iMid = new SkIntersections();
        SkPathOps.CurveIntersectRay[(int)verb]!(segment.Pts(), segment.Weight(), rayMid, iMid);
        int iOutside = iMid.MostOutside(_start.T(), _end.T(), dStartPt);
        if (iOutside < 0)
        {
            return false;
        }

        SkOpSegment oppSegment = rh.Segment();
        PathVerb oppVerb = oppSegment.Verb();
        SkIntersections oppMid = new SkIntersections();
        SkPathOps.CurveIntersectRay[(int)oppVerb]!(oppSegment.Pts(), oppSegment.Weight(), rayMid, oppMid);
        int oppOutside = oppMid.MostOutside(rh._start.T(), rh._end.T(), dStartPt);
        if (oppOutside < 0)
        {
            return false;
        }

        SkDVector iSide = iMid.Pt(iOutside) - dStartPt;
        SkDVector oppSide = oppMid.Pt(oppOutside) - dStartPt;
        double dir = iSide.CrossCheck(oppSide);
        if (dir == 0)
        {
            return false;
        }

        inside = dir < 0;
        return true;
    }

    private bool OppositePlanes(SkOpAngle rh)
    {
        int startSpan = SkPathOps.SkTAbs(rh._sectorStart - _sectorStart);
        return startSpan >= 8;
    }

    // false == this < rh ; true == this > rh; -1 == unorderable
    private int Orderable(SkOpAngle rh)
    {
        int result;
        if (!_part.IsCurve())
        {
            if (!rh._part.IsCurve())
            {
                double leftX = _tangentHalf.Dx();
                double leftY = _tangentHalf.Dy();
                double rightX = rh._tangentHalf.Dx();
                double rightY = rh._tangentHalf.Dy();
                double x_ry = leftX * rightY;
                double rx_y = rightX * leftY;
                if (x_ry == rx_y)
                {
                    if (leftX * rightX < 0 || leftY * rightY < 0)
                    {
                        return 1; // exactly 180 degrees apart
                    }

                    goto unorderable;
                }

                return x_ry < rx_y ? 1 : 0;
            }

            if ((result = LineOnOneSide(rh, false)) >= 0)
            {
                return result;
            }

            if (_unorderable || SkPathOps.ApproximatelyZero(rh._side))
            {
                goto unorderable;
            }
        }
        else if (!rh._part.IsCurve())
        {
            if ((result = rh.LineOnOneSide(this, false)) >= 0)
            {
                return result != 0 ? 0 : 1;
            }

            if (rh._unorderable || SkPathOps.ApproximatelyZero(_side))
            {
                goto unorderable;
            }
        }
        else if ((result = ConvexHullOverlaps(rh)) >= 0)
        {
            return result;
        }

        return EndsIntersect(rh) ? 1 : 0;
    unorderable:
        _unorderable = true;
        rh._unorderable = true;
        return -1;
    }

    // OPTIMIZE: if this shows up in a profile, add a previous pointer
    // as is, this should be rarely called
    public SkOpAngle Previous()
    {
        SkOpAngle last = _next!;
        do
        {
            SkOpAngle next = last._next!;
            if (next == this)
            {
                return last;
            }

            last = next;
        }
        while (true);
    }

    public SkOpSegment Segment()
    {
        return _start.Segment();
    }

    public void Set(SkOpSpanBase start, SkOpSpanBase end)
    {
        _start = start;
        _computedEnd = _end = end;
        _next = null;
        _computeSector = _computedSector = _tangentsAmbiguous = false;
        SetSpans();
        SetSector();
    }

    private void SetSpans()
    {
        _unorderable = false;
        _lastMarked = null;
        if (_start == null)
        {
            _unorderable = true;
            return;
        }

        SkOpSegment segment = _start.Segment();
        ReadOnlySpan<PathPoint> pts = segment.Pts();
        segment.SubDivide(_start, _end, ref _part.Curve); //  set at least the line part if not more
        _originalCurvePart = _part.Curve;
        PathVerb verb = segment.Verb();
        _part.SetCurveHullSweep(verb);
        if (PathVerb.Line != verb && !_part.IsCurve())
        {
            SkDLine lineHalf = default;
            _part.Curve[1] = _part.Curve[SkPathOps.SkPathOpsVerbToPoints(verb)];
            _originalCurvePart[1] = _part.Curve[1];
            lineHalf[0].Set(_part.Curve[0].AsSkPoint());
            lineHalf[1].Set(_part.Curve[1].AsSkPoint());
            _tangentHalf.LineEndPoints(lineHalf);
            _side = 0;
        }

        switch (verb)
        {
            case PathVerb.Line:
            {
                PathPoint cP1 = pts[_start.T() < _end.T() ? 1 : 0];
                SkDLine lineHalf = default;
                lineHalf[0].Set(_start.Pt());
                lineHalf[1].Set(cP1);
                _tangentHalf.LineEndPoints(lineHalf);
                _side = 0;
                return;
            }

            case PathVerb.Quad:
            case PathVerb.Conic:
            {
                SkLineParameters tangentPart = default;
                _ = tangentPart.QuadEndPoints(_part.Curve.Quad);
                _side = -tangentPart.PointDistance(_part.Curve[2]); // not normalized -- compare sign only
                break;
            }

            case PathVerb.Cubic:
            {
                SkLineParameters tangentPart = default;
                _ = tangentPart.CubicPart(_part.Curve.Cubic);
                _side = -tangentPart.PointDistance(_part.Curve[3]);
                Span<double> testTs = stackalloc double[4];
                // OPTIMIZATION: keep inflections precomputed with cubic segment?
                int testCount = SkDCubic.FindInflections(pts, testTs);
                double startT = _start.T();
                double endT = _end.T();
                double limitT = endT;
                int index;
                for (index = 0; index < testCount; ++index)
                {
                    if (!SkPathOps.Between(startT, testTs[index], limitT))
                    {
                        testTs[index] = -1;
                    }
                }

                testTs[testCount++] = startT;
                testTs[testCount++] = endT;
                SkPathOps.SkTQSort(testTs[..testCount]);
                double bestSide = 0;
                int testCases = (testCount << 1) - 1;
                index = 0;
                while (testTs[index] < 0)
                {
                    ++index;
                }

                index <<= 1;
                for (; index < testCases; ++index)
                {
                    int testIndex = index >> 1;
                    double testT = testTs[testIndex];
                    if ((index & 1) != 0)
                    {
                        testT = (testT + testTs[testIndex + 1]) / 2;
                    }

                    // OPTIMIZE: could avoid call for t == startT, endT
                    SkDPoint pt = SkPathOps.DcubicXyAtT(pts, segment.Weight(), testT);
                    SkLineParameters testPart = default;
                    testPart.CubicEndPoints(_part.Curve.Cubic);
                    double testSide = testPart.PointDistance(pt);
                    if (Math.Abs(bestSide) < Math.Abs(testSide))
                    {
                        bestSide = testSide;
                    }
                }

                _side = -bestSide; // compare sign only
                break;
            }

            default:
                break;
        }
    }

    private void SetSector()
    {
        if (_start == null)
        {
            _unorderable = true;
            return;
        }

        SkOpSegment segment = _start.Segment();
        PathVerb verb = segment.Verb();
        _sectorStart = (sbyte)FindSector(verb, _part.Sweep[0].X, _part.Sweep[0].Y);
        if (_sectorStart < 0)
        {
            goto deferTilLater;
        }

        if (!_part.IsCurve())
        {
            // if it's a line or line-like, note that both sectors are the same
            _sectorEnd = _sectorStart;
            _sectorMask = 1 << _sectorStart;
            return;
        }

        _sectorEnd = (sbyte)FindSector(verb, _part.Sweep[1].X, _part.Sweep[1].Y);
        if (_sectorEnd < 0)
        {
            goto deferTilLater;
        }

        if (_sectorEnd == _sectorStart
            && (_sectorStart & 3) != 3)
        {
            // if the sector has no span, it can't be an exact angle
            _sectorMask = 1 << _sectorStart;
            return;
        }

        bool crossesZero = CheckCrossesZero();
        int start = SkPathOps.StdMin((int)_sectorStart, (int)_sectorEnd);
        bool curveBendsCCW = (_sectorStart == start) ^ crossesZero;
        // bump the start and end of the sector span if they are on exact compass points
        if ((_sectorStart & 3) == 3)
        {
            _sectorStart = (sbyte)((_sectorStart + (curveBendsCCW ? 1 : 31)) & 0x1f);
        }

        if ((_sectorEnd & 3) == 3)
        {
            _sectorEnd = (sbyte)((_sectorEnd + (curveBendsCCW ? 31 : 1)) & 0x1f);
        }

        crossesZero = CheckCrossesZero();
        start = SkPathOps.StdMin((int)_sectorStart, (int)_sectorEnd);
        int end = SkPathOps.StdMax((int)_sectorStart, (int)_sectorEnd);
        if (!crossesZero)
        {
            _sectorMask = (int)(uint.MaxValue >> (31 - end + start) << start);
        }
        else
        {
            _sectorMask = (int)((uint.MaxValue >> (31 - start)) | (uint.MaxValue << end));
        }

        return;

    deferTilLater:
        _sectorStart = _sectorEnd = -1;
        _sectorMask = 0;
        _computeSector = true; // can't determine sector until segment length can be found
    }

    public SkOpSpan Starter()
    {
        return _start.Starter(_end);
    }

    private bool TangentsDiverge(SkOpAngle rh, double s0xt0)
    {
        if (s0xt0 == 0)
        {
            return false;
        }

        // if the ctrl tangents are not nearly parallel, use them
        // solve for opposite direction displacement scale factor == m
        // initial dir = v1.cross(v2) == v2.x * v1.y - v2.y * v1.x
        // displacement of q1[1] : dq1 = { -m * v1.y, m * v1.x } + q1[1]
        // straight angle when : v2.x * (dq1.y - q1[0].y) == v2.y * (dq1.x - q1[0].x)
        //                       v2.x * (m * v1.x + v1.y) == v2.y * (-m * v1.y + v1.x)
        // - m * (v2.x * v1.x + v2.y * v1.y) == v2.x * v1.y - v2.y * v1.x
        // m = (v2.y * v1.x - v2.x * v1.y) / (v2.x * v1.x + v2.y * v1.y)
        // m = v1.cross(v2) / v1.dot(v2)
        ref readonly SkDVector2 sweep = ref _part.Sweep;
        ref readonly SkDVector2 tweep = ref rh._part.Sweep;
        double s0dt0 = sweep[0].Dot(tweep[0]);
        if (s0dt0 == 0)
        {
            return true;
        }

        double m = s0xt0 / s0dt0;
        double sDist = sweep[0].Length() * m;
        double tDist = tweep[0].Length() * m;
        bool useS = Math.Abs(sDist) < Math.Abs(tDist);
        double mFactor = Math.Abs(useS ? DistEndRatio(sDist) : rh.DistEndRatio(tDist));
        _tangentsAmbiguous = mFactor >= 50 && mFactor < 200;
        return mFactor < 50; // empirically found limit
    }
}

file static class SkOpAngleStatics
{
    public static readonly int[,,] Sedecimant =
    {
        //       y<0           y==0           y>0
        //   x<0 x==0 x>0  x<0 x==0 x>0  x<0 x==0 x>0
        { { 4, 3, 2 }, { 7, -1, 15 }, { 10, 11, 12 } }, // abs(x) <  abs(y)
        { { 5, -1, 1 }, { -1, -1, -1 }, { 9, -1, 13 } }, // abs(x) == abs(y)
        { { 6, 3, 0 }, { 7, -1, 15 }, { 8, 11, 14 } }, // abs(x) >  abs(y)
    };
}
