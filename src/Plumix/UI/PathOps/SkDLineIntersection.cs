namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkDLineIntersection.cpp at chrome/m119 (the Skia inside
// SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types are
// kept. The SkIntersections part for line/line, line/horizontal and line/vertical intersections.

internal sealed partial class SkIntersections
{
    private void CleanUpParallelLines(bool parallel)
    {
        while (_used > 2)
        {
            RemoveOne(1);
        }

        if (_used == 2 && !parallel)
        {
            bool startMatch = _t[0][0] == 0 || SkPathOps.ZeroOrOne(_t[1][0]);
            bool endMatch = _t[0][1] == 1 || SkPathOps.ZeroOrOne(_t[1][1]);
            if ((!startMatch && !endMatch) || SkPathOps.ApproximatelyEqual(_t[0][0], _t[0][1]))
            {
                if (startMatch && endMatch && (_t[0][0] != 0 || !SkPathOps.ZeroOrOne(_t[1][0]))
                    && _t[0][1] == 1 && SkPathOps.ZeroOrOne(_t[1][1]))
                {
                    RemoveOne(0);
                }
                else
                {
                    RemoveOne(endMatch ? 1 : 0);
                }
            }
        }

        if (_used == 2)
        {
            _isCoincident[0] = _isCoincident[1] = 0x03;
        }
    }

    private void ComputePoints(in SkDLine line, int used)
    {
        _pt[0] = line.PtAtT(_t[0][0]);
        if ((_used = (byte)used) == 2)
        {
            _pt[1] = line.PtAtT(_t[0][1]);
        }
    }

    public int IntersectRay(in SkDLine a, in SkDLine b)
    {
        _max = 2;
        SkDVector aLen = a.Pts[1] - a.Pts[0];
        SkDVector bLen = b.Pts[1] - b.Pts[0];
        /* Slopes match when denom goes to zero:
                          axLen / ayLen ==                   bxLen / byLen
        (ayLen * byLen) * axLen / ayLen == (ayLen * byLen) * bxLen / byLen
                 byLen  * axLen         ==  ayLen          * bxLen
                 byLen  * axLen         -   ayLen          * bxLen == 0 ( == denom )
         */
        double denom = bLen.Y * aLen.X - aLen.Y * bLen.X;
        int used;
        if (!SkPathOps.ApproximatelyZero(denom))
        {
            SkDVector ab0 = a.Pts[0] - b.Pts[0];
            double numerA = ab0.Y * bLen.X - bLen.Y * ab0.X;
            double numerB = ab0.Y * aLen.X - aLen.Y * ab0.X;
            numerA /= denom;
            numerB /= denom;
            _t[0][0] = numerA;
            _t[1][0] = numerB;
            used = 1;
        }
        else
        {
            /* See if the axis intercepts match:
                      ay - ax * ayLen / axLen  ==          by - bx * ayLen / axLen
             axLen * (ay - ax * ayLen / axLen) == axLen * (by - bx * ayLen / axLen)
             axLen *  ay - ax * ayLen          == axLen *  by - bx * ayLen
            */
            if (!SkPathOps.AlmostEqualUlps(
                    aLen.X * a.Pts[0].Y - aLen.Y * a.Pts[0].X,
                    aLen.X * b.Pts[0].Y - aLen.Y * b.Pts[0].X))
            {
                return _used = 0;
            }

            // there's no great answer for intersection points for coincident rays, but return something
            _t[0][0] = _t[1][0] = 0;
            _t[1][0] = _t[1][1] = 1;
            used = 2;
        }

        ComputePoints(a, used);
        return _used;
    }

    // note that this only works if both lines are neither horizontal nor vertical
    public int Intersect(in SkDLine a, in SkDLine b)
    {
        _max = 3; // note that we clean up so that there is no more than two in the end
        // see if end points intersect the opposite line
        double t;
        for (int iA = 0; iA < 2; ++iA)
        {
            if ((t = b.ExactPoint(a.Pts[iA])) >= 0)
            {
                Insert(iA, t, a.Pts[iA]);
            }
        }

        for (int iB = 0; iB < 2; ++iB)
        {
            if ((t = a.ExactPoint(b.Pts[iB])) >= 0)
            {
                Insert(t, iB, b.Pts[iB]);
            }
        }

        /* Determine the intersection point of two line segments
           Return FALSE if the lines don't intersect
           from: http://paulbourke.net/geometry/lineline2d/ */
        double axLen = a.Pts[1].X - a.Pts[0].X;
        double ayLen = a.Pts[1].Y - a.Pts[0].Y;
        double bxLen = b.Pts[1].X - b.Pts[0].X;
        double byLen = b.Pts[1].Y - b.Pts[0].Y;
        /* Slopes match when denom goes to zero:
                          axLen / ayLen ==                   bxLen / byLen
        (ayLen * byLen) * axLen / ayLen == (ayLen * byLen) * bxLen / byLen
                 byLen  * axLen         ==  ayLen          * bxLen
                 byLen  * axLen         -   ayLen          * bxLen == 0 ( == denom )
         */
        double axByLen = axLen * byLen;
        double ayBxLen = ayLen * bxLen;
        // detect parallel lines the same way here and in SkOpAngle operator <
        // so that non-parallel means they are also sortable
        bool unparallel = _allowNear
            ? SkPathOps.NotAlmostEqualUlpsPin(axByLen, ayBxLen)
            : SkPathOps.NotAlmostDequalUlps(axByLen, ayBxLen);
        if (unparallel && _used == 0)
        {
            double ab0y = a.Pts[0].Y - b.Pts[0].Y;
            double ab0x = a.Pts[0].X - b.Pts[0].X;
            double numerA = ab0y * bxLen - byLen * ab0x;
            double numerB = ab0y * axLen - ayLen * ab0x;
            double denom = axByLen - ayBxLen;
            if (SkPathOps.Between(0, numerA, denom) && SkPathOps.Between(0, numerB, denom))
            {
                _t[0][0] = numerA / denom;
                _t[1][0] = numerB / denom;
                ComputePoints(a, 1);
            }
        }

        /* Allow tracking that both sets of end points are near each other -- the lines are entirely
           coincident -- even when the end points are not exactly the same.
           Mark this as a 'wild card' for the end points, so that either point is considered totally
           coincident. Then, avoid folding the lines over each other, but allow either end to mate
           to the next set of lines.
         */
        if (_allowNear || !unparallel)
        {
            Span<double> aNearB = stackalloc double[2];
            Span<double> bNearA = stackalloc double[2];
            Span<bool> aNotB = stackalloc bool[2] { false, false };
            Span<bool> bNotA = stackalloc bool[2] { false, false };
            int nearCount = 0;
            for (int index = 0; index < 2; ++index)
            {
                aNearB[index] = t = b.NearPoint(a.Pts[index], ref aNotB[index]);
                nearCount += t >= 0 ? 1 : 0;
                bNearA[index] = t = a.NearPoint(b.Pts[index], ref bNotA[index]);
                nearCount += t >= 0 ? 1 : 0;
            }

            if (nearCount > 0)
            {
                // Skip if each segment contributes to one end point.
                if (nearCount != 2 || aNotB[0] == aNotB[1])
                {
                    for (int iA = 0; iA < 2; ++iA)
                    {
                        if (!aNotB[iA])
                        {
                            continue;
                        }

                        int nearer = aNearB[iA] > 0.5 ? 1 : 0;
                        if (!bNotA[nearer])
                        {
                            continue;
                        }

                        InsertNear(iA, nearer, a.Pts[iA], b.Pts[nearer]);
                        aNearB[iA] = -1;
                        bNearA[nearer] = -1;
                        nearCount -= 2;
                    }
                }

                if (nearCount > 0)
                {
                    for (int iA = 0; iA < 2; ++iA)
                    {
                        if (aNearB[iA] >= 0)
                        {
                            Insert(iA, aNearB[iA], a.Pts[iA]);
                        }
                    }

                    for (int iB = 0; iB < 2; ++iB)
                    {
                        if (bNearA[iB] >= 0)
                        {
                            Insert(bNearA[iB], iB, b.Pts[iB]);
                        }
                    }
                }
            }
        }

        CleanUpParallelLines(!unparallel);
        return _used;
    }

    public static double HorizontalIntercept(in SkDLine line, double y)
    {
        return SkPathOps.SkPinT((y - line.Pts[0].Y) / (line.Pts[1].Y - line.Pts[0].Y));
    }

    public int Horizontal(in SkDLine line, double left, double right, double y, bool flipped)
    {
        _max = 3; // clean up parallel at the end will limit the result to 2 at the most
        // see if end points intersect the opposite line
        double t;
        SkDPoint leftPt = new SkDPoint(left, y);
        if ((t = line.ExactPoint(leftPt)) >= 0)
        {
            Insert(t, flipped ? 1 : 0, leftPt);
        }

        if (left != right)
        {
            SkDPoint rightPt = new SkDPoint(right, y);
            if ((t = line.ExactPoint(rightPt)) >= 0)
            {
                Insert(t, !flipped ? 1 : 0, rightPt);
            }

            for (int index = 0; index < 2; ++index)
            {
                if ((t = SkDLine.ExactPointH(line.Pts[index], left, right, y)) >= 0)
                {
                    Insert(index, flipped ? 1 - t : t, line.Pts[index]);
                }
            }
        }

        int result = SkDLineIntersectionStatics.HorizontalCoincident(line, y);
        if (result == 1 && _used == 0)
        {
            _t[0][0] = HorizontalIntercept(line, y);
            double xIntercept = line.Pts[0].X + _t[0][0] * (line.Pts[1].X - line.Pts[0].X);
            if (SkPathOps.Between(left, xIntercept, right))
            {
                _t[1][0] = (xIntercept - left) / (right - left);
                if (flipped)
                {
                    // OPTIMIZATION: ? instead of swapping, pass original line, use [1].fX - [0].fX
                    for (int index = 0; index < result; ++index)
                    {
                        _t[1][index] = 1 - _t[1][index];
                    }
                }

                _pt[0].X = xIntercept;
                _pt[0].Y = y;
                _used = 1;
            }
        }

        if (_allowNear || result == 2)
        {
            if ((t = line.NearPoint(leftPt)) >= 0)
            {
                Insert(t, flipped ? 1 : 0, leftPt);
            }

            if (left != right)
            {
                SkDPoint rightPt = new SkDPoint(right, y);
                if ((t = line.NearPoint(rightPt)) >= 0)
                {
                    Insert(t, !flipped ? 1 : 0, rightPt);
                }

                for (int index = 0; index < 2; ++index)
                {
                    if ((t = SkDLine.NearPointH(line.Pts[index], left, right, y)) >= 0)
                    {
                        Insert(index, flipped ? 1 - t : t, line.Pts[index]);
                    }
                }
            }
        }

        CleanUpParallelLines(result == 2);
        return _used;
    }

    public static double VerticalIntercept(in SkDLine line, double x)
    {
        return SkPathOps.SkPinT((x - line.Pts[0].X) / (line.Pts[1].X - line.Pts[0].X));
    }

    public int Vertical(in SkDLine line, double top, double bottom, double x, bool flipped)
    {
        _max = 3; // cleanup parallel lines will bring this back line
        // see if end points intersect the opposite line
        double t;
        SkDPoint topPt = new SkDPoint(x, top);
        if ((t = line.ExactPoint(topPt)) >= 0)
        {
            Insert(t, flipped ? 1 : 0, topPt);
        }

        if (top != bottom)
        {
            SkDPoint bottomPt = new SkDPoint(x, bottom);
            if ((t = line.ExactPoint(bottomPt)) >= 0)
            {
                Insert(t, !flipped ? 1 : 0, bottomPt);
            }

            for (int index = 0; index < 2; ++index)
            {
                if ((t = SkDLine.ExactPointV(line.Pts[index], top, bottom, x)) >= 0)
                {
                    Insert(index, flipped ? 1 - t : t, line.Pts[index]);
                }
            }
        }

        int result = SkDLineIntersectionStatics.VerticalCoincident(line, x);
        if (result == 1 && _used == 0)
        {
            _t[0][0] = VerticalIntercept(line, x);
            double yIntercept = line.Pts[0].Y + _t[0][0] * (line.Pts[1].Y - line.Pts[0].Y);
            if (SkPathOps.Between(top, yIntercept, bottom))
            {
                _t[1][0] = (yIntercept - top) / (bottom - top);
                if (flipped)
                {
                    // OPTIMIZATION: instead of swapping, pass original line, use [1].fY - [0].fY
                    for (int index = 0; index < result; ++index)
                    {
                        _t[1][index] = 1 - _t[1][index];
                    }
                }

                _pt[0].X = x;
                _pt[0].Y = yIntercept;
                _used = 1;
            }
        }

        if (_allowNear || result == 2)
        {
            if ((t = line.NearPoint(topPt)) >= 0)
            {
                Insert(t, flipped ? 1 : 0, topPt);
            }

            if (top != bottom)
            {
                SkDPoint bottomPt = new SkDPoint(x, bottom);
                if ((t = line.NearPoint(bottomPt)) >= 0)
                {
                    Insert(t, !flipped ? 1 : 0, bottomPt);
                }

                for (int index = 0; index < 2; ++index)
                {
                    if ((t = SkDLine.NearPointV(line.Pts[index], top, bottom, x)) >= 0)
                    {
                        Insert(index, flipped ? 1 - t : t, line.Pts[index]);
                    }
                }
            }
        }

        CleanUpParallelLines(result == 2);
        return _used;
    }
}

file static class SkDLineIntersectionStatics
{
    public static int HorizontalCoincident(in SkDLine line, double y)
    {
        double min = line.Pts[0].Y;
        double max = line.Pts[1].Y;
        if (min > max)
        {
            (min, max) = (max, min);
        }

        if (min > y || max < y)
        {
            return 0;
        }

        if (SkPathOps.AlmostEqualUlps(min, max) && max - min < Math.Abs(line.Pts[0].X - line.Pts[1].X))
        {
            return 2;
        }

        return 1;
    }

    public static int VerticalCoincident(in SkDLine line, double x)
    {
        double min = line.Pts[0].X;
        double max = line.Pts[1].X;
        if (min > max)
        {
            (min, max) = (max, min);
        }

        if (!SkPathOps.PreciselyBetween(min, x, max))
        {
            return 0;
        }

        if (SkPathOps.AlmostEqualUlps(min, max))
        {
            return 2;
        }

        return 1;
    }
}
