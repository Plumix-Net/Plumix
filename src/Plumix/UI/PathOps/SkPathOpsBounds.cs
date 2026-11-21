namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkPathOpsBounds.h at chrome/m119 (the Skia inside SkiaSharp
// 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types are kept.
// SkPathOpsBounds derives from SkRect in Skia; a struct cannot, so it carries SkRect's float fields itself and is
// `partial`: a later layer adds the SkRect members it calls (setEmpty, setBounds, ...) as further parts.

// SkPathOpsBounds, unlike SkRect, does not consider a line to be empty.
internal partial struct SkPathOpsBounds
{
    public float Left;
    public float Top;
    public float Right;
    public float Bottom;

    public static bool Intersects(in SkPathOpsBounds a, in SkPathOpsBounds b)
    {
        return SkPathOps.AlmostLessOrEqualUlps(a.Left, b.Right)
            && SkPathOps.AlmostLessOrEqualUlps(b.Left, a.Right)
            && SkPathOps.AlmostLessOrEqualUlps(a.Top, b.Bottom)
            && SkPathOps.AlmostLessOrEqualUlps(b.Top, a.Bottom);
    }

    // Note that add(), unlike SkRect::join() or SkRect::growToInclude()
    // does not treat the bounds of horizontal and vertical lines as
    // empty rectangles.
    public void Add(float left, float top, float right, float bottom)
    {
        if (left < Left)
        {
            Left = left;
        }

        if (top < Top)
        {
            Top = top;
        }

        if (right > Right)
        {
            Right = right;
        }

        if (bottom > Bottom)
        {
            Bottom = bottom;
        }
    }

    public void Add(in SkPathOpsBounds toAdd)
    {
        Add(toAdd.Left, toAdd.Top, toAdd.Right, toAdd.Bottom);
    }

    public void Add(PathPoint pt)
    {
        if (pt.X < Left)
        {
            Left = pt.X;
        }

        if (pt.Y < Top)
        {
            Top = pt.Y;
        }

        if (pt.X > Right)
        {
            Right = pt.X;
        }

        if (pt.Y > Bottom)
        {
            Bottom = pt.Y;
        }
    }

    public void Add(in SkDPoint pt)
    {
        if (pt.X < Left)
        {
            Left = SkPathOps.SkDoubleToScalar(pt.X);
        }

        if (pt.Y < Top)
        {
            Top = SkPathOps.SkDoubleToScalar(pt.Y);
        }

        if (pt.X > Right)
        {
            Right = SkPathOps.SkDoubleToScalar(pt.X);
        }

        if (pt.Y > Bottom)
        {
            Bottom = SkPathOps.SkDoubleToScalar(pt.Y);
        }
    }

    public readonly bool AlmostContains(PathPoint pt)
    {
        return SkPathOps.AlmostLessOrEqualUlps(Left, pt.X)
            && SkPathOps.AlmostLessOrEqualUlps(pt.X, Right)
            && SkPathOps.AlmostLessOrEqualUlps(Top, pt.Y)
            && SkPathOps.AlmostLessOrEqualUlps(pt.Y, Bottom);
    }

    public readonly bool Contains(PathPoint pt)
    {
        return Left <= pt.X && Top <= pt.Y && Right >= pt.X && Bottom >= pt.Y;
    }

    // SkRect::setLTRB (include/core/SkRect.h), which SkDCurve's set*Bounds call.
    public void SetLTRB(float left, float top, float right, float bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
}
