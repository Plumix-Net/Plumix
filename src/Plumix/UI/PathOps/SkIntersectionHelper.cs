namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkIntersectionHelper.h at chrome/m119 (the Skia inside SkiaSharp
// 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float types are kept. The helper is
// a value type wrapping one segment reference, as in Skia. SkIntersectionHelper::SegmentType is the top-level enum
// SkIntersectionHelperSegmentType, because C# cannot nest a type named like the segmentType() method; it keeps
// Skia's numbering (the line kinds reuse SkPath::Verb values).

internal enum SkIntersectionHelperSegmentType
{
    HorizontalLine = -1,
    VerticalLine = 0,
    Line = (int)PathVerb.Line,
    Quad = (int)PathVerb.Quad,
    Conic = (int)PathVerb.Conic,
    Cubic = (int)PathVerb.Cubic,
}

internal struct SkIntersectionHelper
{
    private SkOpSegment _segment;

    public bool Advance()
    {
        SkOpSegment? next = _segment.Next();
        _segment = next!;
        return next != null;
    }

    public readonly float Bottom()
    {
        return Bounds().Bottom;
    }

    public readonly ref readonly SkPathOpsBounds Bounds()
    {
        return ref _segment.Bounds();
    }

    public readonly SkOpContour Contour()
    {
        return _segment.Contour();
    }

    public void Init(SkOpContour contour)
    {
        _segment = contour.First();
    }

    public readonly float Left()
    {
        return Bounds().Left;
    }

    public readonly ReadOnlySpan<PathPoint> Pts()
    {
        return _segment.Pts();
    }

    public readonly float Right()
    {
        return Bounds().Right;
    }

    public readonly SkOpSegment Segment()
    {
        return _segment;
    }

    public readonly SkIntersectionHelperSegmentType SegmentType()
    {
        SkIntersectionHelperSegmentType type = (SkIntersectionHelperSegmentType)_segment.Verb();
        if (type != SkIntersectionHelperSegmentType.Line)
        {
            return type;
        }

        if (_segment.IsHorizontal())
        {
            return SkIntersectionHelperSegmentType.HorizontalLine;
        }

        if (_segment.IsVertical())
        {
            return SkIntersectionHelperSegmentType.VerticalLine;
        }

        return SkIntersectionHelperSegmentType.Line;
    }

    public bool StartAfter(in SkIntersectionHelper after)
    {
        SkOpSegment? next = after._segment.Next();
        _segment = next!;
        return next != null;
    }

    public readonly float Top()
    {
        return Bounds().Top;
    }

    public readonly float Weight()
    {
        return _segment.Weight();
    }

    public readonly float X()
    {
        return Bounds().Left;
    }

    public readonly bool XFlipped()
    {
        return X() != Pts()[0].X;
    }

    public readonly float Y()
    {
        return Bounds().Top;
    }

    public readonly bool YFlipped()
    {
        return Y() != Pts()[0].Y;
    }
}
