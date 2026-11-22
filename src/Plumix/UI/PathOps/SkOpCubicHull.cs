namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkOpCubicHull.cpp at chrome/m119 (the Skia inside SkiaSharp
// 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types are kept.
// Skia's size_t indices (all in 0..3) are int; char order[4] is Span<sbyte>.

internal partial struct SkDCubic
{
    /* Given a cubic, find the convex hull described by the end and control points.
       The hull may have 3 or 4 points. Cubics that degenerate into a point or line
       are not considered.

       The hull is computed by assuming that three points, if unique and non-linear,
       form a triangle. The fourth point may replace one of the first three, may be
       discarded if in the triangle or on an edge, or may be inserted between any of
       the three to form a convex quadralateral.

       The indices returned in order describe the convex hull.
    */
    public readonly int ConvexHull(Span<sbyte> order)
    {
        int index;
        // find top point
        int yMin = 0;
        for (index = 1; index < 4; ++index)
        {
            if (Pts[yMin].Y > Pts[index].Y || (Pts[yMin].Y == Pts[index].Y
                && Pts[yMin].X > Pts[index].X))
            {
                yMin = index;
            }
        }

        order[0] = (sbyte)yMin;
        int midX = -1;
        int backupYMin = -1;
        for (int pass = 0; pass < 2; ++pass)
        {
            for (index = 0; index < 4; ++index)
            {
                if (index == yMin)
                {
                    continue;
                }

                // rotate line from (yMin, index) to axis
                // see if remaining two points are both above or below
                // use this to find mid
                int mask = SkPathOps.OtherTwo(yMin, index);
                int side1 = yMin ^ mask;
                int side2 = index ^ mask;
                SkDCubic rotPath = default;
                if (!SkOpCubicHullStatics.Rotate(this, yMin, index, ref rotPath))
                {
                    // ! if cbc[yMin]==cbc[idx]
                    order[1] = (sbyte)side1;
                    order[2] = (sbyte)side2;
                    return 3;
                }

                int sides = SkOpCubicHullStatics.Side(rotPath.Pts[side1].Y - rotPath.Pts[yMin].Y);
                sides ^= SkOpCubicHullStatics.Side(rotPath.Pts[side2].Y - rotPath.Pts[yMin].Y);
                if (sides == 2)
                {
                    // '2' means one remaining point <0, one >0
                    if (midX >= 0)
                    {
                        // one of the control points is equal to an end point
                        order[0] = 0;
                        order[1] = 3;
                        if (Pts[1] == Pts[0] || Pts[1] == Pts[3])
                        {
                            order[2] = 2;
                            return 3;
                        }

                        if (Pts[2] == Pts[0] || Pts[2] == Pts[3])
                        {
                            order[2] = 1;
                            return 3;
                        }

                        // one of the control points may be very nearly but not exactly equal --
                        double dist1_0 = Pts[1].DistanceSquared(Pts[0]);
                        double dist1_3 = Pts[1].DistanceSquared(Pts[3]);
                        double dist2_0 = Pts[2].DistanceSquared(Pts[0]);
                        double dist2_3 = Pts[2].DistanceSquared(Pts[3]);
                        double smallest1distSq = SkPathOps.StdMin(dist1_0, dist1_3);
                        double smallest2distSq = SkPathOps.StdMin(dist2_0, dist2_3);
                        if (SkPathOps.ApproximatelyZero(SkPathOps.StdMin(smallest1distSq, smallest2distSq)))
                        {
                            order[2] = (sbyte)(smallest1distSq < smallest2distSq ? 2 : 1);
                            return 3;
                        }
                    }

                    midX = index;
                }
                else if (sides == 0)
                {
                    // '0' means both to one side or the other
                    backupYMin = index;
                }
            }

            if (midX >= 0)
            {
                break;
            }

            if (backupYMin < 0)
            {
                break;
            }

            yMin = backupYMin;
            backupYMin = -1;
        }

        if (midX < 0)
        {
            midX = yMin ^ 3; // choose any other point
        }

        int mask2 = SkPathOps.OtherTwo(yMin, midX); // Skia's second `mask`
        int least = yMin ^ mask2;
        int most = midX ^ mask2;
        order[0] = (sbyte)yMin;
        order[1] = (sbyte)least;

        // see if mid value is on same side of line (least, most) as yMin
        SkDCubic midPath = default;
        if (!SkOpCubicHullStatics.Rotate(this, least, most, ref midPath))
        {
            // ! if cbc[least]==cbc[most]
            order[2] = (sbyte)midX;
            return 3;
        }

        int midSides = SkOpCubicHullStatics.Side(midPath.Pts[yMin].Y - midPath.Pts[least].Y);
        midSides ^= SkOpCubicHullStatics.Side(midPath.Pts[midX].Y - midPath.Pts[least].Y);
        if (midSides != 2)
        {
            // if mid point is not between
            order[2] = (sbyte)most;
            return 3; // result is a triangle
        }

        order[2] = (sbyte)midX;
        order[3] = (sbyte)most;
        return 4; // result is a quadralateral
    }
}

file static class SkOpCubicHullStatics
{
    public static bool Rotate(in SkDCubic cubic, int zero, int index, ref SkDCubic rotPath)
    {
        double dy = cubic.Pts[index].Y - cubic.Pts[zero].Y;
        double dx = cubic.Pts[index].X - cubic.Pts[zero].X;
        if (SkPathOps.ApproximatelyZero(dy))
        {
            if (SkPathOps.ApproximatelyZero(dx))
            {
                return false;
            }

            rotPath = cubic;
            if (dy != 0)
            {
                rotPath[index].Y = cubic.Pts[zero].Y;
                int mask = SkPathOps.OtherTwo(index, zero);
                int side1 = index ^ mask;
                int side2 = zero ^ mask;
                if (SkPathOps.ApproximatelyEqual(cubic.Pts[side1].Y, cubic.Pts[zero].Y))
                {
                    rotPath[side1].Y = cubic.Pts[zero].Y;
                }

                if (SkPathOps.ApproximatelyEqual(cubic.Pts[side2].Y, cubic.Pts[zero].Y))
                {
                    rotPath[side2].Y = cubic.Pts[zero].Y;
                }
            }

            return true;
        }

        for (int i = 0; i < 4; ++i)
        {
            rotPath[i].X = cubic.Pts[i].X * dx + cubic.Pts[i].Y * dy;
            rotPath[i].Y = cubic.Pts[i].Y * dx - cubic.Pts[i].X * dy;
        }

        return true;
    }

    // Returns 0 if negative, 1 if zero, 2 if positive
    public static int Side(double x)
    {
        return (x > 0 ? 1 : 0) + (x >= 0 ? 1 : 0);
    }
}
