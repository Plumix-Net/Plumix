namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of the SkRect members of skia/include/core/SkRect.h and skia/src/core/SkRect.cpp that
// pathops calls on SkPathOpsBounds (which derives from SkRect in Skia), at chrome/m119 (the Skia inside SkiaSharp
// 3.119), behind Path.Combine. setBoundsCheck's skvx float4 lanes are spelled out one by one; skvx::min/max are
// std::min/std::max per lane. They differ from SSE's minps/maxps only on NaN, which empties the rect anyway, and on
// the sign of a zero, which pathops never observes. SkRect::intersect, which Op uses on the rects isRect finds, is
// ported for Plumix.UI.SkRect.

internal partial struct SkPathOpsBounds
{
    // SkRect::setEmpty: (0, 0, 0, 0).
    public void SetEmpty()
    {
        Left = 0;
        Top = 0;
        Right = 0;
        Bottom = 0;
    }

    // SkRect::setBounds.
    public void SetBounds(ReadOnlySpan<PathPoint> pts, int count)
    {
        _ = SetBoundsCheck(pts, count);
    }

    // SkRect::setBoundsCheck.
    public bool SetBoundsCheck(ReadOnlySpan<PathPoint> pts, int count)
    {
        if (count <= 0)
        {
            SetEmpty();
            return true;
        }

        float min0, min1, min2, min3;
        int index;
        if ((count & 1) != 0)
        {
            min0 = pts[0].X;
            min1 = pts[0].Y;
            min2 = pts[0].X;
            min3 = pts[0].Y;
            index = 1;
            count -= 1;
        }
        else
        {
            min0 = pts[0].X;
            min1 = pts[0].Y;
            min2 = pts[1].X;
            min3 = pts[1].Y;
            index = 2;
            count -= 2;
        }

        float max0 = min0, max1 = min1, max2 = min2, max3 = min3;
        float accum0 = min0 * 0, accum1 = min1 * 0, accum2 = min2 * 0, accum3 = min3 * 0;
        while (count != 0)
        {
            float xy0 = pts[index].X;
            float xy1 = pts[index].Y;
            float xy2 = pts[index + 1].X;
            float xy3 = pts[index + 1].Y;
            accum0 = accum0 * xy0;
            accum1 = accum1 * xy1;
            accum2 = accum2 * xy2;
            accum3 = accum3 * xy3;
            min0 = SkPathOps.StdMin(min0, xy0);
            min1 = SkPathOps.StdMin(min1, xy1);
            min2 = SkPathOps.StdMin(min2, xy2);
            min3 = SkPathOps.StdMin(min3, xy3);
            max0 = SkPathOps.StdMax(max0, xy0);
            max1 = SkPathOps.StdMax(max1, xy1);
            max2 = SkPathOps.StdMax(max2, xy2);
            max3 = SkPathOps.StdMax(max3, xy3);
            index += 2;
            count -= 2;
        }

        bool allFinite = accum0 * 0 == 0 && accum1 * 0 == 0 && accum2 * 0 == 0 && accum3 * 0 == 0;
        if (allFinite)
        {
            SetLTRB(
                SkPathOps.StdMin(min0, min2),
                SkPathOps.StdMin(min1, min3),
                SkPathOps.StdMax(max0, max2),
                SkPathOps.StdMax(max1, max3));
        }
        else
        {
            SetEmpty();
        }

        return allFinite;
    }
}

internal static partial class SkPathOps
{
    // SkRect::intersect(r) on a Plumix.UI.SkRect: CHECK_INTERSECT(r, this), then setLTRB. The !(L < R && T < B)
    // test returns false if either argument is NaN.
    public static bool SkRectIntersect(ref SkRect rect, in SkRect r)
    {
        float l = StdMax(r.Left, rect.Left);
        float rr = StdMin(r.Right, rect.Right);
        float t = StdMax(r.Top, rect.Top);
        float b = StdMin(r.Bottom, rect.Bottom);
        if (!(l < rr && t < b))
        {
            return false;
        }

        rect = new SkRect(l, t, rr, b);
        return true;
    }
}
