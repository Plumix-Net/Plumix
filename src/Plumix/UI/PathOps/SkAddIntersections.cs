namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkAddIntersections.h and SkAddIntersections.cpp at chrome/m119
// (the Skia inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and
// float/double types are kept. The debugShow*Intersection helpers are empty in release builds and are omitted, as
// are debugValidate and debugAddLoopCount.

internal static partial class SkPathOps
{
    public static bool AddIntersectTs(SkOpContour test, SkOpContour next, SkOpCoincidence coincidence)
    {
        if (test != next)
        {
            if (AlmostLessUlps(test.Bounds().Bottom, next.Bounds().Top))
            {
                return false;
            }

            // OPTIMIZATION: outset contour bounds a smidgen instead?
            if (!SkPathOpsBounds.Intersects(test.Bounds(), next.Bounds()))
            {
                return true;
            }
        }

        SkOpPtT?[] coinPtT = new SkOpPtT?[2];
        SkIntersectionHelper wt = default;
        wt.Init(test);
        do
        {
            SkIntersectionHelper wn = default;
            wn.Init(next);
            if (test == next && !wn.StartAfter(wt))
            {
                continue;
            }

            do
            {
                if (!SkPathOpsBounds.Intersects(wt.Bounds(), wn.Bounds()))
                {
                    continue;
                }

                int pts = 0;
                SkIntersections ts = new SkIntersections();
                bool swap = false;
                SkDQuad quad1 = default, quad2 = default;
                SkDConic conic1 = default, conic2 = default;
                SkDCubic cubic1 = default, cubic2 = default;
                switch (wt.SegmentType())
                {
                    case SkIntersectionHelperSegmentType.HorizontalLine:
                        swap = true;
                        switch (wn.SegmentType())
                        {
                            case SkIntersectionHelperSegmentType.HorizontalLine:
                            case SkIntersectionHelperSegmentType.VerticalLine:
                            case SkIntersectionHelperSegmentType.Line:
                                pts = ts.LineHorizontal(wn.Pts(), wt.Left(),
                                    wt.Right(), wt.Y(), wt.XFlipped());
                                break;
                            case SkIntersectionHelperSegmentType.Quad:
                                pts = ts.QuadHorizontal(wn.Pts(), wt.Left(),
                                    wt.Right(), wt.Y(), wt.XFlipped());
                                break;
                            case SkIntersectionHelperSegmentType.Conic:
                                pts = ts.ConicHorizontal(wn.Pts(), wn.Weight(), wt.Left(),
                                    wt.Right(), wt.Y(), wt.XFlipped());
                                break;
                            case SkIntersectionHelperSegmentType.Cubic:
                                pts = ts.CubicHorizontal(wn.Pts(), wt.Left(),
                                    wt.Right(), wt.Y(), wt.XFlipped());
                                break;
                            default:
                                break;
                        }

                        break;
                    case SkIntersectionHelperSegmentType.VerticalLine:
                        swap = true;
                        switch (wn.SegmentType())
                        {
                            case SkIntersectionHelperSegmentType.HorizontalLine:
                            case SkIntersectionHelperSegmentType.VerticalLine:
                            case SkIntersectionHelperSegmentType.Line:
                            {
                                pts = ts.LineVertical(wn.Pts(), wt.Top(),
                                    wt.Bottom(), wt.X(), wt.YFlipped());
                                break;
                            }

                            case SkIntersectionHelperSegmentType.Quad:
                            {
                                pts = ts.QuadVertical(wn.Pts(), wt.Top(),
                                    wt.Bottom(), wt.X(), wt.YFlipped());
                                break;
                            }

                            case SkIntersectionHelperSegmentType.Conic:
                            {
                                pts = ts.ConicVertical(wn.Pts(), wn.Weight(), wt.Top(),
                                    wt.Bottom(), wt.X(), wt.YFlipped());
                                break;
                            }

                            case SkIntersectionHelperSegmentType.Cubic:
                            {
                                pts = ts.CubicVertical(wn.Pts(), wt.Top(),
                                    wt.Bottom(), wt.X(), wt.YFlipped());
                                break;
                            }

                            default:
                                break;
                        }

                        break;
                    case SkIntersectionHelperSegmentType.Line:
                        switch (wn.SegmentType())
                        {
                            case SkIntersectionHelperSegmentType.HorizontalLine:
                                pts = ts.LineHorizontal(wt.Pts(), wn.Left(),
                                    wn.Right(), wn.Y(), wn.XFlipped());
                                break;
                            case SkIntersectionHelperSegmentType.VerticalLine:
                                pts = ts.LineVertical(wt.Pts(), wn.Top(),
                                    wn.Bottom(), wn.X(), wn.YFlipped());
                                break;
                            case SkIntersectionHelperSegmentType.Line:
                                pts = ts.LineLine(wt.Pts(), wn.Pts());
                                break;
                            case SkIntersectionHelperSegmentType.Quad:
                                swap = true;
                                pts = ts.QuadLine(wn.Pts(), wt.Pts());
                                break;
                            case SkIntersectionHelperSegmentType.Conic:
                                swap = true;
                                pts = ts.ConicLine(wn.Pts(), wn.Weight(), wt.Pts());
                                break;
                            case SkIntersectionHelperSegmentType.Cubic:
                                swap = true;
                                pts = ts.CubicLine(wn.Pts(), wt.Pts());
                                break;
                            default:
                                break;
                        }

                        break;
                    case SkIntersectionHelperSegmentType.Quad:
                        switch (wn.SegmentType())
                        {
                            case SkIntersectionHelperSegmentType.HorizontalLine:
                                pts = ts.QuadHorizontal(wt.Pts(), wn.Left(),
                                    wn.Right(), wn.Y(), wn.XFlipped());
                                break;
                            case SkIntersectionHelperSegmentType.VerticalLine:
                                pts = ts.QuadVertical(wt.Pts(), wn.Top(),
                                    wn.Bottom(), wn.X(), wn.YFlipped());
                                break;
                            case SkIntersectionHelperSegmentType.Line:
                                pts = ts.QuadLine(wt.Pts(), wn.Pts());
                                break;
                            case SkIntersectionHelperSegmentType.Quad:
                            {
                                pts = ts.Intersect(quad1.Set(wt.Pts()), quad2.Set(wn.Pts()));
                                break;
                            }

                            case SkIntersectionHelperSegmentType.Conic:
                            {
                                swap = true;
                                pts = ts.Intersect(conic2.Set(wn.Pts(), wn.Weight()),
                                    quad1.Set(wt.Pts()));
                                break;
                            }

                            case SkIntersectionHelperSegmentType.Cubic:
                            {
                                swap = true;
                                pts = ts.Intersect(cubic2.Set(wn.Pts()), quad1.Set(wt.Pts()));
                                break;
                            }

                            default:
                                break;
                        }

                        break;
                    case SkIntersectionHelperSegmentType.Conic:
                        switch (wn.SegmentType())
                        {
                            case SkIntersectionHelperSegmentType.HorizontalLine:
                                pts = ts.ConicHorizontal(wt.Pts(), wt.Weight(), wn.Left(),
                                    wn.Right(), wn.Y(), wn.XFlipped());
                                break;
                            case SkIntersectionHelperSegmentType.VerticalLine:
                                pts = ts.ConicVertical(wt.Pts(), wt.Weight(), wn.Top(),
                                    wn.Bottom(), wn.X(), wn.YFlipped());
                                break;
                            case SkIntersectionHelperSegmentType.Line:
                                pts = ts.ConicLine(wt.Pts(), wt.Weight(), wn.Pts());
                                break;
                            case SkIntersectionHelperSegmentType.Quad:
                            {
                                pts = ts.Intersect(conic1.Set(wt.Pts(), wt.Weight()),
                                    quad2.Set(wn.Pts()));
                                break;
                            }

                            case SkIntersectionHelperSegmentType.Conic:
                            {
                                pts = ts.Intersect(conic1.Set(wt.Pts(), wt.Weight()),
                                    conic2.Set(wn.Pts(), wn.Weight()));
                                break;
                            }

                            case SkIntersectionHelperSegmentType.Cubic:
                            {
                                swap = true;
                                pts = ts.Intersect(cubic2.Set(wn.Pts()),
                                    conic1.Set(wt.Pts(), wt.Weight()));
                                break;
                            }
                        }

                        break;
                    case SkIntersectionHelperSegmentType.Cubic:
                        switch (wn.SegmentType())
                        {
                            case SkIntersectionHelperSegmentType.HorizontalLine:
                                pts = ts.CubicHorizontal(wt.Pts(), wn.Left(),
                                    wn.Right(), wn.Y(), wn.XFlipped());
                                break;
                            case SkIntersectionHelperSegmentType.VerticalLine:
                                pts = ts.CubicVertical(wt.Pts(), wn.Top(),
                                    wn.Bottom(), wn.X(), wn.YFlipped());
                                break;
                            case SkIntersectionHelperSegmentType.Line:
                                pts = ts.CubicLine(wt.Pts(), wn.Pts());
                                break;
                            case SkIntersectionHelperSegmentType.Quad:
                            {
                                pts = ts.Intersect(cubic1.Set(wt.Pts()), quad2.Set(wn.Pts()));
                                break;
                            }

                            case SkIntersectionHelperSegmentType.Conic:
                            {
                                pts = ts.Intersect(cubic1.Set(wt.Pts()),
                                    conic2.Set(wn.Pts(), wn.Weight()));
                                break;
                            }

                            case SkIntersectionHelperSegmentType.Cubic:
                            {
                                pts = ts.Intersect(cubic1.Set(wt.Pts()), cubic2.Set(wn.Pts()));
                                break;
                            }

                            default:
                                break;
                        }

                        break;
                    default:
                        break;
                }

                int coinIndex = -1;
                for (int pt = 0; pt < pts; ++pt)
                {
                    // if t value is used to compute pt in addT, error may creep in and
                    // rect intersections may result in non-rects. if pt value from intersection
                    // is passed in, current tests break. As a workaround, pass in pt
                    // value from intersection only if pt.x and pt.y is integral
                    PathPoint iPt = ts.Pt(pt).AsSkPoint();
                    bool iPtIsIntegral = iPt.X == MathF.Floor(iPt.X) && iPt.Y == MathF.Floor(iPt.Y);
                    SkOpPtT testTAt = iPtIsIntegral
                        ? wt.Segment().AddT(ts[swap ? 1 : 0][pt], iPt)!
                        : wt.Segment().AddT(ts[swap ? 1 : 0][pt])!;
                    SkOpPtT nextTAt = iPtIsIntegral
                        ? wn.Segment().AddT(ts[!swap ? 1 : 0][pt], iPt)!
                        : wn.Segment().AddT(ts[!swap ? 1 : 0][pt])!;
                    if (!testTAt.Contains(nextTAt))
                    {
                        SkOpPtT? oppPrev = testTAt.OppPrev(nextTAt); //  Returns nullptr if pair
                        if (oppPrev != null)
                        {
                            //  already share a pt-t loop.
                            _ = testTAt.Span().MergeMatches(nextTAt.Span());
                            testTAt.AddOpp(nextTAt, oppPrev);
                        }

                        if (testTAt.Pt != nextTAt.Pt)
                        {
                            testTAt.Span().Unaligned();
                            nextTAt.Span().Unaligned();
                        }
                    }

                    if (!ts.IsCoincident(pt))
                    {
                        continue;
                    }

                    if (coinIndex < 0)
                    {
                        coinPtT[0] = testTAt;
                        coinPtT[1] = nextTAt;
                        coinIndex = pt;
                        continue;
                    }

                    if (coinPtT[0]!.Span() == testTAt.Span())
                    {
                        coinIndex = -1;
                        continue;
                    }

                    if (coinPtT[1]!.Span() == nextTAt.Span())
                    {
                        coinIndex = -1; // coincidence span collapsed
                        continue;
                    }

                    if (swap)
                    {
                        (coinPtT[0], coinPtT[1]) = (coinPtT[1], coinPtT[0]);
                        (testTAt, nextTAt) = (nextTAt, testTAt);
                    }

                    if (coinPtT[0]!.Span().Deleted())
                    {
                        coinIndex = -1;
                        continue;
                    }

                    if (testTAt.Span().Deleted())
                    {
                        coinIndex = -1;
                        continue;
                    }

                    coincidence.Add(coinPtT[0]!, testTAt, coinPtT[1]!, nextTAt);
                    coinIndex = -1;
                }
            }
            while (wn.Advance());
        }
        while (wt.Advance());

        return true;
    }
}
