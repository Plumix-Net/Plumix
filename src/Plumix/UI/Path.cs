using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/painting.dart (Path)
// Engine adapter: engine/src/flutter/lib/ui/painting/path.cc over skia/src/core/SkPath.cpp. The path keeps
// SkPath's own data — verbs, float points, conic weights, the last-move index and the oval/rrect shape
// it was built as — and every builder appends exactly what SkPath's does (PathSkiaParityTests checks
// this against the Skia the engine ships); containment ports SkPath::contains (PathContainment) and
// measurement SkContourMeasure (PathMetrics). Boolean operations and perspective transforms still run on
// a native copy (SkiaPathInterop).
public enum PathFillType
{
    NonZero,
    EvenOdd,
}

public enum PathOperation
{
    Difference,
    Intersect,
    Union,
    Xor,
    ReverseDifference,
}

/// <summary>The kinds of segment a <see cref="Path"/> holds: Skia's <c>SkPath::Verb</c>.</summary>
internal enum PathVerb : byte
{
    Move,
    Line,
    Quad,
    Conic,
    Cubic,
    Close,
}

/// <summary>
/// Skia's <c>fIsOval</c>/<c>fIsRRect</c>: the path is exactly one oval or rounded rect as
/// <c>addOval</c>/<c>addRRect</c> wrote it (after any leading moves), then any transforms that keep
/// rects rects. Skia draws and strokes such a path through its oval and rrect fast paths.
/// </summary>
internal sealed class PathShape
{
    internal required bool IsRRect { get; init; }

    internal required float Left { get; init; }

    internal required float Top { get; init; }

    internal required float Right { get; init; }

    internal required float Bottom { get; init; }

    /// <summary>The scaled radii of a rounded rect: x, y per corner from the top left, clockwise.</summary>
    internal float[]? Radii { get; init; }

    internal required bool CounterClockwise { get; init; }

    internal required int StartIndex { get; init; }

    /// <summary>The moves the path held before the shape was added.</summary>
    internal required PathPoint[] LeadingMoves { get; init; }

    /// <summary>The transforms applied since, in order.</summary>
    internal PathMatrix[] Transforms { get; init; } = [];

    internal PathShape WithTransform(PathMatrix matrix) => new()
    {
        IsRRect = IsRRect,
        Left = Left,
        Top = Top,
        Right = Right,
        Bottom = Bottom,
        Radii = Radii,
        CounterClockwise = CounterClockwise,
        StartIndex = StartIndex,
        LeadingMoves = LeadingMoves,
        Transforms = [.. Transforms, matrix],
    };
}

/// <summary>A mutable collection of line, quadratic, conic and cubic contours.</summary>
public sealed class Path
{
    private const int InitialLastMoveToIndex = ~0;

    private readonly List<PathVerb> _verbs;
    private readonly List<PathPoint> _points;
    private readonly List<float> _weights;
    private PathFillType _fillType;
    private int _lastMoveToIndex = InitialLastMoveToIndex;
    private PathShape? _shape;
    private int _version;

    public Path()
    {
        _verbs = [];
        _points = [];
        _weights = [];
    }

    public Path(Path source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _verbs = [.. source._verbs];
        _points = [.. source._points];
        _weights = [.. source._weights];
        _fillType = source._fillType;
        _lastMoveToIndex = source._lastMoveToIndex;
        _shape = source._shape;
    }

    /// <summary>Skia's <c>SkPath::operator=</c>: this path becomes a copy of <paramref name="source"/>.</summary>
    internal void CopyFrom(Path source)
    {
        _verbs.Clear();
        _verbs.AddRange(source._verbs);
        _points.Clear();
        _points.AddRange(source._points);
        _weights.Clear();
        _weights.AddRange(source._weights);
        _fillType = source._fillType;
        _lastMoveToIndex = source._lastMoveToIndex;
        _shape = source._shape;
        _version++;
    }

    public PathFillType FillType
    {
        get => _fillType;
        set
        {
            _fillType = value;
            _version++;
        }
    }

    internal IReadOnlyList<PathVerb> Verbs => _verbs;

    internal IReadOnlyList<PathPoint> Points => _points;

    internal IReadOnlyList<float> ConicWeights => _weights;

    internal PathShape? Shape => _shape;

    /// <summary>Changes on every edit, so a backend can tell whether its native copy is current.</summary>
    internal int Version => _version;

    /// <summary>The backend's native copy of this path and the <see cref="Version"/> it was built at.</summary>
    internal (int Version, object Native)? NativeCache { get; set; }

    public void MoveTo(double x, double y) => MoveTo(new PathPoint(Narrow(x), Narrow(y)));

    public void RelativeMoveTo(double dx, double dy)
    {
        PathPoint point = default;
        int count = _points.Count;
        if (count > 0)
        {
            point = _lastMoveToIndex >= 0 ? _points[count - 1] : _points[~_lastMoveToIndex];
        }

        MoveTo(new PathPoint(point.X + Narrow(dx), point.Y + Narrow(dy)));
    }

    public void LineTo(double x, double y) => LineTo(new PathPoint(Narrow(x), Narrow(y)));

    public void RelativeLineTo(double dx, double dy)
    {
        InjectMoveToIfNeeded();
        PathPoint last = LastPoint;
        LineTo(new PathPoint(last.X + Narrow(dx), last.Y + Narrow(dy)));
    }

    public void QuadraticBezierTo(double x1, double y1, double x2, double y2) =>
        QuadTo(new PathPoint(Narrow(x1), Narrow(y1)), new PathPoint(Narrow(x2), Narrow(y2)));

    public void RelativeQuadraticBezierTo(double x1, double y1, double x2, double y2)
    {
        InjectMoveToIfNeeded();
        PathPoint last = LastPoint;
        QuadTo(
            new PathPoint(last.X + Narrow(x1), last.Y + Narrow(y1)),
            new PathPoint(last.X + Narrow(x2), last.Y + Narrow(y2)));
    }

    public void CubicTo(double x1, double y1, double x2, double y2, double x3, double y3) =>
        CubicTo(
            new PathPoint(Narrow(x1), Narrow(y1)),
            new PathPoint(Narrow(x2), Narrow(y2)),
            new PathPoint(Narrow(x3), Narrow(y3)));

    public void RelativeCubicTo(double x1, double y1, double x2, double y2, double x3, double y3)
    {
        InjectMoveToIfNeeded();
        PathPoint last = LastPoint;
        CubicTo(
            new PathPoint(last.X + Narrow(x1), last.Y + Narrow(y1)),
            new PathPoint(last.X + Narrow(x2), last.Y + Narrow(y2)),
            new PathPoint(last.X + Narrow(x3), last.Y + Narrow(y3)));
    }

    public void ConicTo(double x1, double y1, double x2, double y2, double weight) =>
        ConicTo(new PathPoint(Narrow(x1), Narrow(y1)), new PathPoint(Narrow(x2), Narrow(y2)), Narrow(weight));

    public void RelativeConicTo(double x1, double y1, double x2, double y2, double weight)
    {
        InjectMoveToIfNeeded();
        PathPoint last = LastPoint;
        ConicTo(
            new PathPoint(last.X + Narrow(x1), last.Y + Narrow(y1)),
            new PathPoint(last.X + Narrow(x2), last.Y + Narrow(y2)),
            Narrow(weight));
    }

    public void ArcTo(Rect oval, double startAngleRadians, double sweepAngleRadians, bool forceMoveTo)
    {
        CheckRect(oval);
        ArcTo(ToSkRect(oval), Degrees(startAngleRadians), Degrees(sweepAngleRadians), forceMoveTo);
    }

    public void ArcToPoint(Offset arcEnd, Radius radius = default,
        double rotation = 0.0, bool largeArc = false, bool clockwise = true)
    {
        CheckPoint(arcEnd);
        CheckRadius(radius);
        SvgArcTo(Narrow(radius.X), Narrow(radius.Y), Narrow(rotation), largeArc, clockwise,
            Narrow(arcEnd.Dx), Narrow(arcEnd.Dy));
    }

    public void RelativeArcToPoint(Offset arcEndDelta, Radius radius = default,
        double rotation = 0.0, bool largeArc = false, bool clockwise = true)
    {
        CheckPoint(arcEndDelta);
        CheckRadius(radius);
        // SkPath::rArcTo.
        PathPoint current = LastPoint;
        SvgArcTo(Narrow(radius.X), Narrow(radius.Y), Narrow(rotation), largeArc, clockwise,
            current.X + Narrow(arcEndDelta.Dx), current.Y + Narrow(arcEndDelta.Dy));
    }

    public void AddRect(Rect rect)
    {
        CheckRect(rect);
        AddRect(ToSkRect(rect), counterClockwise: false, startIndex: 0);
    }

    public void AddOval(Rect oval)
    {
        CheckRect(oval);
        AddOval(ToSkRect(oval), counterClockwise: false, startIndex: 1);
    }

    // SkPath::addArc.
    public void AddArc(Rect oval, double startAngleRadians, double sweepAngleRadians)
    {
        CheckRect(oval);
        SkRect rect = ToSkRect(oval);
        float startAngle = Degrees(startAngleRadians);
        float sweepAngle = Degrees(sweepAngleRadians);
        if (rect.IsEmpty || sweepAngle == 0.0f)
        {
            return;
        }

        const float fullCircleAngle = 360.0f;
        if (sweepAngle >= fullCircleAngle || sweepAngle <= -fullCircleAngle)
        {
            // An arc that begins at one of addOval's legal starting positions is an oval.
            float startOver90 = startAngle / 90.0f;
            float startOver90I = PathMath.Round(startOver90);
            float error = startOver90 - startOver90I;
            if (PathMath.NearlyEqual(error, 0.0f))
            {
                // Index 1 is at startAngle == 0.
                float startIndex = (startOver90I + 1.0f) % 4.0f;
                startIndex = startIndex < 0.0f ? startIndex + 4.0f : startIndex;
                AddOval(rect, counterClockwise: sweepAngle <= 0.0f, (int)startIndex);
                return;
            }
        }

        ArcTo(rect, startAngle, sweepAngle, forceMoveTo: true);
    }

    // SkPath::addPoly.
    public void AddPolygon(IReadOnlyList<Offset> points, bool close)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            return;
        }

        _lastMoveToIndex = _points.Count;
        Append(PathVerb.Move, new PathPoint(Narrow(points[0].Dx), Narrow(points[0].Dy)));
        for (int index = 1; index < points.Count; index++)
        {
            Append(PathVerb.Line, new PathPoint(Narrow(points[index].Dx), Narrow(points[index].Dy)));
        }

        if (close)
        {
            AppendVerb(PathVerb.Close);
            MarkContourClosed();
        }
    }

    public void AddRoundedRect(Rect rect, double radius) => AddRRect(RRect.FromRectXY(rect, radius, radius));

    public void AddRRect(RRect rrect)
    {
        RRect normalized = rrect.NormalizeForPaint();
        CheckRect(normalized.Rect);
        CheckRadius(normalized.TopLeft);
        CheckRadius(normalized.TopRight);
        CheckRadius(normalized.BottomRight);
        CheckRadius(normalized.BottomLeft);
        var skRRect = SkRRect.Create(
            ToSkRect(normalized.Rect),
            [
                Narrow(normalized.TopLeft.X), Narrow(normalized.TopLeft.Y),
                Narrow(normalized.TopRight.X), Narrow(normalized.TopRight.Y),
                Narrow(normalized.BottomRight.X), Narrow(normalized.BottomRight.Y),
                Narrow(normalized.BottomLeft.X), Narrow(normalized.BottomLeft.Y),
            ]);
        AddRRect(skRRect, counterClockwise: false, startIndex: 6);
    }

    public void AddRSuperellipse(RSuperellipse rsuperellipse) => AddPath(rsuperellipse.ToPath());

    public void AddPath(Path other) => AddPath(other, default);

    public void AddPath(Path path, Offset offset, IReadOnlyList<double>? matrix4 = null) =>
        Append(path, offset, matrix4, extend: false);

    public void ExtendWithPath(Path path, Offset offset, IReadOnlyList<double>? matrix4 = null) =>
        Append(path, offset, matrix4, extend: true);

    // SkPath::close.
    public void Close()
    {
        if (_verbs.Count > 0 && _verbs[^1] != PathVerb.Close)
        {
            AppendVerb(PathVerb.Close);
        }

        // Signal that a moveTo must follow, unless the last contour is already closed.
        MarkContourClosed();
    }

    // SkPath::reset.
    public void Reset()
    {
        _verbs.Clear();
        _points.Clear();
        _weights.Clear();
        _lastMoveToIndex = InitialLastMoveToIndex;
        _fillType = PathFillType.NonZero;
        _shape = null;
        _version++;
    }

    public bool Contains(Offset point)
    {
        CheckPoint(point);
        return PathContainment.Contains(this, Narrow(point.Dx), Narrow(point.Dy));
    }

    // SkPath::offset.
    public Path Shift(Offset offset)
    {
        CheckPoint(offset);
        return TransformAffine(PathMatrix.Translate(Narrow(offset.Dx), Narrow(offset.Dy)));
    }

    /// <summary>Plumix-only: maps the path by an affine matrix given as Avalonia-style components.</summary>
    internal Path Transform(double m11, double m12, double m21, double m22, double m31, double m32) =>
        TransformAffine(PathMatrix.FromValues(
            Narrow(m11), Narrow(m21), Narrow(m31),
            Narrow(m12), Narrow(m22), Narrow(m32),
            0.0f, 0.0f, 1.0f));

    public Path Transform(IReadOnlyList<double> matrix4)
    {
        PathMatrix matrix = ToPathMatrix(matrix4);
        return matrix.HasPerspective ? SkiaPathInterop.TransformWithPerspective(this, matrix) : TransformAffine(matrix);
    }

    // SkPath::transform without perspective (SkPathRef::CreateTransformedCopy).
    private Path TransformAffine(PathMatrix matrix)
    {
        var result = new Path(this);
        if (matrix.IsIdentity)
        {
            return result;
        }

        for (int index = 0; index < result._points.Count; index++)
        {
            result._points[index] = matrix.Map(result._points[index]);
        }

        result._shape = _shape is not null && matrix.RectStaysRect ? _shape.WithTransform(matrix) : null;
        return result;
    }

    public Rect GetBounds()
    {
        // Pinned SkPathBuilder.computeFiniteBounds ignores a trailing move, but checks all points
        // for finiteness.
        int pointCount = _points.Count;
        if (pointCount == 0 || _points.Any(point => !float.IsFinite(point.X) || !float.IsFinite(point.Y)))
        {
            return default;
        }

        int count = pointCount - (_verbs[^1] == PathVerb.Move && _verbs.Count > 1 ? 1 : 0);
        double left = _points[0].X;
        double top = _points[0].Y;
        double right = left;
        double bottom = top;
        for (int index = 1; index < count; index++)
        {
            left = Math.Min(left, _points[index].X);
            top = Math.Min(top, _points[index].Y);
            right = Math.Max(right, _points[index].X);
            bottom = Math.Max(bottom, _points[index].Y);
        }

        return Rect.FromLTWH(left, top, right - left, bottom - top);
    }

    public static Path Combine(PathOperation operation, Path path1, Path path2)
    {
        ArgumentNullException.ThrowIfNull(path1);
        ArgumentNullException.ThrowIfNull(path2);
        var result = new Path();
        if (!PathOps.SkPathOps.Op(path1, path2, (PathOps.SkPathOp)operation, result))
        {
            throw new InvalidOperationException(
                "Path.combine() failed.  This may be due an invalid path; in particular, check for NaN values.");
        }

        return result;
    }

    public PathMetrics ComputeMetrics(bool forceClosed = false) => new(this, forceClosed);

    public override string ToString() => "Path";

    /// <summary>Plumix-only: rebuilds a path from Skia-shaped data a backend produced.</summary>
    internal static Path FromSegments(
        PathFillType fillType,
        IEnumerable<(PathVerb Verb, PathPoint[] Points, float Weight)> segments)
    {
        var path = new Path { _fillType = fillType };
        foreach ((PathVerb verb, PathPoint[] points, float weight) in segments)
        {
            switch (verb)
            {
                case PathVerb.Move:
                    path.MoveTo(points[0]);
                    break;
                case PathVerb.Line:
                    path.LineTo(points[0]);
                    break;
                case PathVerb.Quad:
                    path.QuadTo(points[0], points[1]);
                    break;
                case PathVerb.Conic:
                    path.ConicTo(points[0], points[1], weight);
                    break;
                case PathVerb.Cubic:
                    path.CubicTo(points[0], points[1], points[2]);
                    break;
                default:
                    path.Close();
                    break;
            }
        }

        return path;
    }

    private PathPoint LastPoint => _points.Count > 0 ? _points[^1] : default;

    private void AppendVerb(PathVerb verb)
    {
        _verbs.Add(verb);
        _shape = null;
        _version++;
    }

    private void Append(PathVerb verb, PathPoint point)
    {
        _points.Add(point);
        AppendVerb(verb);
    }

    private void MarkContourClosed()
    {
        if (_lastMoveToIndex >= 0)
        {
            _lastMoveToIndex = ~_lastMoveToIndex;
        }
    }

    // SkPath::moveTo.
    private void MoveTo(PathPoint point)
    {
        _lastMoveToIndex = _points.Count;
        Append(PathVerb.Move, point);
    }

    // SkPath::injectMoveToIfNeeded.
    private void InjectMoveToIfNeeded()
    {
        if (_lastMoveToIndex < 0)
        {
            MoveTo(_verbs.Count == 0 ? default : _points[~_lastMoveToIndex]);
        }
    }

    private void LineTo(PathPoint point)
    {
        InjectMoveToIfNeeded();
        Append(PathVerb.Line, point);
    }

    private void QuadTo(PathPoint control, PathPoint end)
    {
        InjectMoveToIfNeeded();
        _points.Add(control);
        Append(PathVerb.Quad, end);
    }

    // SkPath::conicTo.
    private void ConicTo(PathPoint control, PathPoint end, float weight)
    {
        if (!(weight > 0.0f))
        {
            // <= 0 or NaN.
            LineTo(end);
        }
        else if (!float.IsFinite(weight))
        {
            LineTo(control);
            LineTo(end);
        }
        else if (weight == 1.0f)
        {
            QuadTo(control, end);
        }
        else
        {
            InjectMoveToIfNeeded();
            _points.Add(control);
            _weights.Add(weight);
            Append(PathVerb.Conic, end);
        }
    }

    private void CubicTo(PathPoint control1, PathPoint control2, PathPoint end)
    {
        InjectMoveToIfNeeded();
        _points.Add(control1);
        _points.Add(control2);
        Append(PathVerb.Cubic, end);
    }

    // SkPath::setLastPt.
    private void SetLastPoint(PathPoint point)
    {
        if (_points.Count == 0)
        {
            MoveTo(point);
            return;
        }

        _points[^1] = point;
        _version++;
    }

    // SkPath::hasOnlyMoveTos.
    private bool HasOnlyMoveTos() =>
        _verbs.All(verb => verb is PathVerb.Move or PathVerb.Close);

    private PathPoint[] LeadingMoves() => [.. _points];

    // SkPath::addRect(rect, dir, startIndex).
    private void AddRect(SkRect rect, bool counterClockwise, int startIndex)
    {
        PathPoint[] corners = rect.Corners;
        int advance = counterClockwise ? 3 : 1;
        int current = startIndex % 4;
        MoveTo(corners[current]);
        for (int index = 0; index < 3; index++)
        {
            current = (current + advance) % 4;
            LineTo(corners[current]);
        }

        Close();
    }

    // SkPath::addOval(oval, dir, startPointIndex).
    private void AddOval(SkRect oval, bool counterClockwise, int startIndex)
    {
        bool isOval = HasOnlyMoveTos();
        PathPoint[] leadingMoves = isOval ? LeadingMoves() : [];
        float centerX = PathMath.Midpoint(oval.Left, oval.Right);
        float centerY = PathMath.Midpoint(oval.Top, oval.Bottom);
        PathPoint[] ovalPoints =
        [
            new(centerX, oval.Top),
            new(oval.Right, centerY),
            new(centerX, oval.Bottom),
            new(oval.Left, centerY),
        ];
        PathPoint[] corners = oval.Corners;
        int advance = counterClockwise ? 3 : 1;
        int ovalIndex = startIndex % 4;
        // The corner iterator tracks "behind" the oval points.
        int cornerIndex = (startIndex + (counterClockwise ? 1 : 0)) % 4;
        MoveTo(ovalPoints[ovalIndex]);
        for (int index = 0; index < 4; index++)
        {
            cornerIndex = (cornerIndex + advance) % 4;
            ovalIndex = (ovalIndex + advance) % 4;
            ConicTo(corners[cornerIndex], ovalPoints[ovalIndex], PathMath.Root2Over2);
        }

        Close();
        if (isOval)
        {
            _shape = new PathShape
            {
                IsRRect = false,
                Left = oval.Left,
                Top = oval.Top,
                Right = oval.Right,
                Bottom = oval.Bottom,
                CounterClockwise = counterClockwise,
                StartIndex = startIndex % 4,
                LeadingMoves = leadingMoves,
            };
        }
    }

    // SkPath::addRRect(rrect, dir, startIndex).
    private void AddRRect(SkRRect rrect, bool counterClockwise, int startIndex)
    {
        SkRect bounds = rrect.Bounds;
        if (rrect.Type is SkRRectType.Rect or SkRRectType.Empty)
        {
            // Degenerate (rect): the radii points collapse.
            AddRect(bounds, counterClockwise, (startIndex + 1) / 2);
            return;
        }

        if (rrect.Type == SkRRectType.Oval)
        {
            // Degenerate (oval): the line points collapse.
            AddOval(bounds, counterClockwise, startIndex / 2);
            return;
        }

        bool isRRect = HasOnlyMoveTos();
        PathPoint[] leadingMoves = isRRect ? LeadingMoves() : [];
        float[] radii = rrect.Radii;
        // A conic starts odd indices when moving clockwise, even ones counter-clockwise.
        bool startsWithConic = (startIndex & 1) == (counterClockwise ? 0 : 1);
        float left = bounds.Left;
        float top = bounds.Top;
        float right = bounds.Right;
        float bottom = bounds.Bottom;
        PathPoint[] rrectPoints =
        [
            new(left + radii[0], top),
            new(right - radii[2], top),
            new(right, top + radii[3]),
            new(right, bottom - radii[5]),
            new(right - radii[4], bottom),
            new(left + radii[6], bottom),
            new(left, bottom - radii[7]),
            new(left, top + radii[1]),
        ];
        PathPoint[] corners = bounds.Corners;
        int rrectAdvance = counterClockwise ? 7 : 1;
        int cornerAdvance = counterClockwise ? 3 : 1;
        int rrectIndex = startIndex % 8;
        // Corner indices follow the collapsed radii model, adjusted so the start point is "behind".
        int cornerIndex = ((startIndex / 2) + (counterClockwise ? 1 : 0)) % 4;
        PathPoint NextRRect() => rrectPoints[rrectIndex = (rrectIndex + rrectAdvance) % 8];
        PathPoint NextCorner() => corners[cornerIndex = (cornerIndex + cornerAdvance) % 4];
        MoveTo(rrectPoints[rrectIndex]);
        if (startsWithConic)
        {
            for (int index = 0; index < 3; index++)
            {
                PathPoint corner = NextCorner();
                ConicTo(corner, NextRRect(), PathMath.Root2Over2);
                LineTo(NextRRect());
            }

            PathPoint last = NextCorner();
            ConicTo(last, NextRRect(), PathMath.Root2Over2);
            // The final lineTo is handled by close().
        }
        else
        {
            for (int index = 0; index < 4; index++)
            {
                LineTo(NextRRect());
                PathPoint corner = NextCorner();
                ConicTo(corner, NextRRect(), PathMath.Root2Over2);
            }
        }

        Close();
        if (isRRect)
        {
            _shape = new PathShape
            {
                IsRRect = true,
                Left = left,
                Top = top,
                Right = right,
                Bottom = bottom,
                Radii = radii,
                CounterClockwise = counterClockwise,
                StartIndex = startIndex % 8,
                LeadingMoves = leadingMoves,
            };
        }
    }

    // SkPath::arcTo(oval, startAngle, sweepAngle, forceMoveTo), angles in degrees.
    private void ArcTo(SkRect oval, float startAngle, float sweepAngle, bool forceMoveTo)
    {
        if (oval.Width < 0.0f || oval.Height < 0.0f)
        {
            return;
        }

        startAngle %= 360.0f;
        if (_verbs.Count == 0)
        {
            forceMoveTo = true;
        }

        void AddPoint(PathPoint point)
        {
            if (forceMoveTo)
            {
                MoveTo(point);
            }
            else if (_points.Count == 0
                     || !PathMath.NearlyEqual(_points[^1].X, point.X)
                     || !PathMath.NearlyEqual(_points[^1].Y, point.Y))
            {
                LineTo(point);
            }
        }

        if (ArcIsLonePoint(oval, startAngle, sweepAngle) is { } lonePoint)
        {
            if (forceMoveTo)
            {
                MoveTo(lonePoint);
            }
            else
            {
                LineTo(lonePoint);
            }

            return;
        }

        (PathPoint startVector, PathPoint stopVector, bool clockwise) = AnglesToUnitVectors(startAngle, sweepAngle);
        if (startVector == stopVector)
        {
            // The sweep is too small for AnglesToUnitVectors to resolve.
            float endAngle = PathMath.DegreesToRadians(startAngle + sweepAngle);
            float radiusX = oval.Width / 2;
            float radiusY = oval.Height / 2;
            AddPoint(new PathPoint(
                PathMath.Midpoint(oval.Left, oval.Right) + (radiusX * MathF.Cos(endAngle)),
                PathMath.Midpoint(oval.Top, oval.Bottom) + (radiusY * MathF.Sin(endAngle))));
            return;
        }

        PathMatrix matrix = PathMatrix.Scale(oval.Width * 0.5f, oval.Height * 0.5f)
            .PostTranslate(PathMath.Midpoint(oval.Left, oval.Right), PathMath.Midpoint(oval.Top, oval.Bottom));
        List<(PathPoint P0, PathPoint P1, PathPoint P2, float Weight)> conics =
            BuildUnitArc(startVector, stopVector, clockwise, matrix);
        if (conics.Count == 0)
        {
            AddPoint(matrix.Map(stopVector));
            return;
        }

        AddPoint(conics[0].P0);
        foreach ((_, PathPoint p1, PathPoint p2, float weight) in conics)
        {
            ConicTo(p1, p2, weight);
        }
    }

    private static PathPoint? ArcIsLonePoint(SkRect oval, float startAngle, float sweepAngle)
    {
        if (sweepAngle == 0.0f && (startAngle == 0.0f || startAngle == 360.0f))
        {
            // Moves into and out of ovals must not distort the oval's bounds.
            return new PathPoint(oval.Right, PathMath.Midpoint(oval.Top, oval.Bottom));
        }

        if (oval.Width == 0.0f && oval.Height == 0.0f)
        {
            return new PathPoint(oval.Right, oval.Top);
        }

        return null;
    }

    // SkPath's angles_to_unit_vectors.
    private static (PathPoint Start, PathPoint Stop, bool Clockwise) AnglesToUnitVectors(
        float startAngle,
        float sweepAngle)
    {
        float startRadians = PathMath.DegreesToRadians(startAngle);
        float stopRadians = PathMath.DegreesToRadians(startAngle + sweepAngle);
        var start = new PathPoint(PathMath.CosSnapToZero(startRadians), PathMath.SinSnapToZero(startRadians));
        var stop = new PathPoint(PathMath.CosSnapToZero(stopRadians), PathMath.SinSnapToZero(stopRadians));
        // A sweep nearly (but not quite) 360 may land the stop vector on the start vector; nudge it back.
        if (start == stop)
        {
            float sweep = Math.Abs(sweepAngle);
            if (sweep < 360.0f && sweep > 359.0f)
            {
                float deltaRadians = MathF.CopySign(1.0f / 512, sweepAngle);
                do
                {
                    stopRadians -= deltaRadians;
                    stop = new PathPoint(PathMath.CosSnapToZero(stopRadians), PathMath.SinSnapToZero(stopRadians));
                }
                while (start == stop);
            }
        }

        return (start, stop, sweepAngle > 0.0f);
    }

    // SkConic::BuildUnitArc.
    private static List<(PathPoint P0, PathPoint P1, PathPoint P2, float Weight)> BuildUnitArc(
        PathPoint unitStart,
        PathPoint unitStop,
        bool clockwise,
        PathMatrix userMatrix)
    {
        var result = new List<(PathPoint, PathPoint, PathPoint, float)>(5);
        // Rotate by x, y so that unitStart is (1, 0).
        float x = (unitStart.X * unitStop.X) + (unitStart.Y * unitStop.Y);
        float y = (unitStart.X * unitStop.Y) - (unitStart.Y * unitStop.X);
        float absY = Math.Abs(y);
        // Effectively coincident vectors: an angle of nearly 0 (x > 0) in the sweep's direction.
        if (absY <= PathMath.NearlyZero && x > 0.0f && ((y >= 0.0f && clockwise) || (y <= 0.0f && !clockwise)))
        {
            return result;
        }

        if (!clockwise)
        {
            y = -y;
        }

        // One conic per quadrant of a circle: which quadrant does (x, y) lie in?
        int quadrant = 0;
        if (y == 0.0f)
        {
            quadrant = 2;
        }
        else if (x == 0.0f)
        {
            quadrant = y > 0.0f ? 1 : 3;
        }
        else
        {
            if (y < 0.0f)
            {
                quadrant += 2;
            }

            if ((x < 0.0f) != (y < 0.0f))
            {
                quadrant += 1;
            }
        }

        PathPoint[] quadrantPoints =
        [
            new(1, 0), new(1, 1), new(0, 1), new(-1, 1), new(-1, 0), new(-1, -1), new(0, -1), new(1, -1),
        ];
        for (int index = 0; index < quadrant; index++)
        {
            result.Add((quadrantPoints[index * 2], quadrantPoints[(index * 2) + 1], quadrantPoints[(index * 2) + 2],
                PathMath.Root2Over2));
        }

        // The remaining sub-90-degree arc.
        var finalPoint = new PathPoint(x, y);
        PathPoint lastQuadrant = quadrantPoints[quadrant * 2];
        float dot = (lastQuadrant.X * finalPoint.X) + (lastQuadrant.Y * finalPoint.Y);
        if (dot < 1.0f)
        {
            var offCurve = new PathPoint(lastQuadrant.X + x, lastQuadrant.Y + y);
            // The bisector rescaled to the off-curve point: its length is 1 / cos(theta / 2), and
            // cos(theta / 2) is the conic's weight.
            float cosThetaOver2 = MathF.Sqrt((1 + dot) / 2);
            offCurve = offCurve.WithLength(1.0f / cosThetaOver2);
            if (lastQuadrant != offCurve)
            {
                result.Add((lastQuadrant, offCurve, finalPoint, cosThetaOver2));
            }
        }

        // Flip for counter-clockwise, rotate into unitStart, then apply the user matrix.
        PathMatrix matrix = PathMatrix.SinCos(unitStart.Y, unitStart.X)
            .PreScale(1.0f, clockwise ? 1.0f : -1.0f)
            .PostConcat(userMatrix);
        for (int index = 0; index < result.Count; index++)
        {
            (PathPoint p0, PathPoint p1, PathPoint p2, float weight) = result[index];
            result[index] = (matrix.Map(p0), matrix.Map(p1), matrix.Map(p2), weight);
        }

        return result;
    }

    // SkPath::arcTo(rx, ry, xAxisRotate, largeArc, sweep, x, y): the SVG endpoint arc.
    private void SvgArcTo(float radiusX, float radiusY, float angle, bool largeArc, bool clockwise, float x, float y)
    {
        InjectMoveToIfNeeded();
        PathPoint source = LastPoint;
        // A zero radius makes the arc a straight line (SVG implementation notes, out-of-range parameters).
        if (radiusX == 0.0f || radiusY == 0.0f)
        {
            LineTo(new PathPoint(x, y));
            return;
        }

        var target = new PathPoint(x, y);
        // Coincident end points make a zero-length path, for continuity in animations.
        if (source == target)
        {
            LineTo(target);
            return;
        }

        radiusX = Math.Abs(radiusX);
        radiusY = Math.Abs(radiusY);
        PathPoint midPointDistance = (source - target).Scale(0.5f);
        PathPoint transformedMidPoint = PathMatrix.Rotate(-angle).Map(midPointDistance);
        float squareRadiusX = radiusX * radiusX;
        float squareRadiusY = radiusY * radiusY;
        float squareX = transformedMidPoint.X * transformedMidPoint.X;
        float squareY = transformedMidPoint.Y * transformedMidPoint.Y;
        // Scale the radii up if they are too small to reach (out-of-range radii correction).
        float radiiScale = (squareX / squareRadiusX) + (squareY / squareRadiusY);
        if (radiiScale > 1.0f)
        {
            radiiScale = MathF.Sqrt(radiiScale);
            radiusX *= radiiScale;
            radiusY *= radiiScale;
        }

        PathMatrix pointTransform = PathMatrix.Scale(1 / radiusX, 1 / radiusY).PreRotate(-angle);
        PathPoint unit0 = pointTransform.Map(source);
        PathPoint unit1 = pointTransform.Map(target);
        PathPoint delta = unit1 - unit0;
        float d = (delta.X * delta.X) + (delta.Y * delta.Y);
        float scaleFactorSquared = Math.Max((1 / d) - 0.25f, 0.0f);
        float scaleFactor = MathF.Sqrt(scaleFactorSquared);
        if (!clockwise != largeArc)
        {
            scaleFactor = -scaleFactor;
        }

        delta = delta.Scale(scaleFactor);
        PathPoint centerPoint = (unit0 + unit1).Scale(0.5f);
        centerPoint = new PathPoint(centerPoint.X - delta.Y, centerPoint.Y + delta.X);
        unit0 -= centerPoint;
        unit1 -= centerPoint;
        float theta1 = MathF.Atan2(unit0.Y, unit0.X);
        float theta2 = MathF.Atan2(unit1.Y, unit1.X);
        float thetaArc = theta2 - theta1;
        if (thetaArc < 0.0f && clockwise)
        {
            thetaArc += PathMath.Pi * 2;
        }
        else if (thetaArc > 0.0f && !clockwise)
        {
            thetaArc -= PathMath.Pi * 2;
        }

        // Very tiny angles make the following math go wonky (skbug.com/9272).
        if (Math.Abs(thetaArc) < PathMath.Pi / (1000 * 1000))
        {
            LineTo(target);
            return;
        }

        pointTransform = PathMatrix.Rotate(angle).PreScale(radiusX, radiusY);
        // The arc may be slightly bigger than a quarter circle, so allow up to a third.
        int segments = (int)MathF.Ceiling(Math.Abs(thetaArc / (2 * PathMath.Pi / 3)));
        float thetaWidth = thetaArc / segments;
        float t = MathF.Tan(0.5f * thetaWidth);
        if (!float.IsFinite(t))
        {
            return;
        }

        float startTheta = theta1;
        float weight = MathF.Sqrt(0.5f + (MathF.Cos(thetaWidth) * 0.5f));
        static bool IsInteger(float value) => value == MathF.Floor(value);
        // On integer input a quarter arc's conics land on integers too, so a round rect stays convex.
        bool expectIntegers = PathMath.IsNearlyZero((PathMath.Pi / 2) - Math.Abs(thetaWidth))
                              && IsInteger(radiusX) && IsInteger(radiusY) && IsInteger(x) && IsInteger(y);
        for (int index = 0; index < segments; index++)
        {
            float endTheta = startTheta + thetaWidth;
            float sinEndTheta = PathMath.SinSnapToZero(endTheta);
            float cosEndTheta = PathMath.CosSnapToZero(endTheta);
            unit1 = new PathPoint(cosEndTheta, sinEndTheta) + centerPoint;
            unit0 = new PathPoint(unit1.X + (t * sinEndTheta), unit1.Y + (-t * cosEndTheta));
            PathPoint mapped0 = pointTransform.Map(unit0);
            PathPoint mapped1 = pointTransform.Map(unit1);
            if (expectIntegers)
            {
                mapped0 = new PathPoint(PathMath.Round(mapped0.X), PathMath.Round(mapped0.Y));
                mapped1 = new PathPoint(PathMath.Round(mapped1.X), PathMath.Round(mapped1.Y));
            }

            ConicTo(mapped0, mapped1, weight);
            startTheta = endTheta;
        }

        // The final point is the input point by definition; rounding must not move it.
        SetLastPoint(target);
    }

    // SkPath::addPath(src, matrix, mode).
    private void Append(Path path, Offset offset, IReadOnlyList<double>? matrix4, bool extend)
    {
        ArgumentNullException.ThrowIfNull(path);
        CheckPoint(offset);
        PathMatrix matrix;
        if (matrix4 is null)
        {
            matrix = PathMatrix.Translate(Narrow(offset.Dx), Narrow(offset.Dy));
        }
        else
        {
            PathMatrix values = ToPathMatrix(matrix4);
            matrix = PathMatrix.FromValues(
                values[0], values[1], values[2] + Narrow(offset.Dx),
                values[3], values[4], values[5] + Narrow(offset.Dy),
                values[6], values[7], values[8]);
        }

        if (path._verbs.Count == 0)
        {
            return;
        }

        // Adding a path to itself reads a copy.
        Path source = ReferenceEquals(path, this) ? new Path(path) : path;
        if (!extend && !matrix.HasPerspective)
        {
            _lastMoveToIndex = source._lastMoveToIndex >= 0
                ? source._lastMoveToIndex + _points.Count
                : source._lastMoveToIndex - _points.Count;
            foreach (PathPoint point in source._points)
            {
                _points.Add(matrix.Map(point));
            }

            _weights.AddRange(source._weights);
            _verbs.AddRange(source._verbs);
            _shape = null;
            _version++;
            return;
        }

        bool firstVerb = true;
        foreach ((PathVerb verb, PathPoint[] points, float weight) in source.Segments())
        {
            switch (verb)
            {
                case PathVerb.Move:
                    PathPoint moved = matrix.Map(points[0]);
                    if (firstVerb && extend && _verbs.Count > 0)
                    {
                        // In case the last contour is closed.
                        InjectMoveToIfNeeded();
                        // A degenerate join adds no line.
                        if (_points.Count == 0 || _points[^1] != moved)
                        {
                            LineTo(moved);
                        }
                    }
                    else
                    {
                        MoveTo(moved);
                    }

                    break;
                case PathVerb.Line:
                    LineTo(matrix.Map(points[0]));
                    break;
                case PathVerb.Quad:
                    QuadTo(matrix.Map(points[0]), matrix.Map(points[1]));
                    break;
                case PathVerb.Conic:
                    ConicTo(matrix.Map(points[0]), matrix.Map(points[1]), weight);
                    break;
                case PathVerb.Cubic:
                    CubicTo(matrix.Map(points[0]), matrix.Map(points[1]), matrix.Map(points[2]));
                    break;
                default:
                    Close();
                    break;
            }

            firstVerb = false;
        }
    }

    /// <summary>
    /// The path's segments: each verb with the points it adds (not the start point it continues from)
    /// and its conic weight.
    /// </summary>
    internal IEnumerable<(PathVerb Verb, PathPoint[] Points, float Weight)> Segments()
    {
        int point = 0;
        int weight = 0;
        foreach (PathVerb verb in _verbs)
        {
            switch (verb)
            {
                case PathVerb.Move:
                case PathVerb.Line:
                    yield return (verb, [_points[point]], 0.0f);
                    point += 1;
                    break;
                case PathVerb.Quad:
                    yield return (verb, [_points[point], _points[point + 1]], 0.0f);
                    point += 2;
                    break;
                case PathVerb.Conic:
                    yield return (verb, [_points[point], _points[point + 1]], _weights[weight++]);
                    point += 2;
                    break;
                case PathVerb.Cubic:
                    yield return (verb, [_points[point], _points[point + 1], _points[point + 2]], 0.0f);
                    point += 3;
                    break;
                default:
                    yield return (verb, [], 0.0f);
                    break;
            }
        }
    }

    internal static float Narrow(double value) => double.IsFinite(value)
        ? (float)Math.Clamp(value, -float.MaxValue, float.MaxValue) : (float)value;

    private static float Degrees(double radians) => Narrow(radians) * 180.0f / (float)Math.PI;

    private static SkRect ToSkRect(Rect rect) =>
        new(Narrow(rect.Left), Narrow(rect.Top), Narrow(rect.Right), Narrow(rect.Bottom));

    private static PathMatrix ToPathMatrix(IReadOnlyList<double> matrix4)
    {
        ArgumentNullException.ThrowIfNull(matrix4);
        DebugAssertions.Assert(matrix4.Count == 16 && matrix4.All(double.IsFinite),
            "'matrix4' must have 16 finite entries.");
        return PathMatrix.FromValues(
            Narrow(matrix4[0]), Narrow(matrix4[4]), Narrow(matrix4[12]),
            Narrow(matrix4[1]), Narrow(matrix4[5]), Narrow(matrix4[13]),
            Narrow(matrix4[3]), Narrow(matrix4[7]), Narrow(matrix4[15]));
    }

    private static void CheckPoint(Offset point) => DebugAssertions.Assert(
        !double.IsNaN(point.Dx) && !double.IsNaN(point.Dy), "Offset argument contained a NaN value.");

    private static void CheckRadius(Radius radius) => DebugAssertions.Assert(
        !double.IsNaN(radius.X) && !double.IsNaN(radius.Y), "Radius argument contained a NaN value.");

    private static void CheckRect(Rect rect) => DebugAssertions.Assert(
        !double.IsNaN(rect.Left) && !double.IsNaN(rect.Top)
        && !double.IsNaN(rect.Right) && !double.IsNaN(rect.Bottom), "Rect argument contained a NaN value.");
}

/// <summary>Skia's <c>SkRect</c>: four float edges.</summary>
internal readonly record struct SkRect(float Left, float Top, float Right, float Bottom)
{
    internal float Width => Right - Left;

    internal float Height => Bottom - Top;

    // SkRect::isEmpty: NaN edges count as empty.
    internal bool IsEmpty => !(Left < Right && Top < Bottom);

    internal bool IsFinite => float.IsFinite(Left) && float.IsFinite(Top)
                                                    && float.IsFinite(Right) && float.IsFinite(Bottom);

    /// <summary>SkPath_RectPointIterator's corners: top left, top right, bottom right, bottom left.</summary>
    internal PathPoint[] Corners => [new(Left, Top), new(Right, Top), new(Right, Bottom), new(Left, Bottom)];

    internal SkRect Sorted() => new(
        Math.Min(Left, Right), Math.Min(Top, Bottom), Math.Max(Left, Right), Math.Max(Top, Bottom));
}

internal enum SkRRectType
{
    Empty,
    Rect,
    Oval,
    Simple,
    NinePatch,
    Complex,
}

/// <summary>Skia's <c>SkRRect</c>: the radii validation and scaling of <c>setRectRadii</c>.</summary>
internal sealed class SkRRect
{
    private SkRRect(SkRect bounds, float[] radii, SkRRectType type)
    {
        Bounds = bounds;
        Radii = radii;
        Type = type;
    }

    internal SkRect Bounds { get; }

    /// <summary>x, y per corner: upper left, upper right, lower right, lower left.</summary>
    internal float[] Radii { get; }

    internal SkRRectType Type { get; }

    // SkRRect::setRectRadii.
    internal static SkRRect Create(SkRect rect, float[] radii)
    {
        // Checked before sorting, since sorting can hide NaNs.
        if (!rect.IsFinite)
        {
            return new SkRRect(default, new float[8], SkRRectType.Empty);
        }

        SkRect sorted = rect.Sorted();
        if (sorted.IsEmpty)
        {
            return new SkRRect(sorted, new float[8], SkRRectType.Empty);
        }

        if (radii.Any(value => !float.IsFinite(value)))
        {
            return AsRect(sorted);
        }

        float[] scaled = [.. radii];
        if (ClampToZero(scaled))
        {
            return AsRect(sorted);
        }

        // Scale all radii down proportionally to fit (CSS backgrounds, section 5.5).
        double scale = 1.0;
        double width = (double)sorted.Right - sorted.Left;
        double height = (double)sorted.Bottom - sorted.Top;
        scale = MinScale(scaled[0], scaled[2], width, scale);
        scale = MinScale(scaled[3], scaled[5], height, scale);
        scale = MinScale(scaled[4], scaled[6], width, scale);
        scale = MinScale(scaled[7], scaled[1], height, scale);
        FlushToZero(scaled, 0, 2);
        FlushToZero(scaled, 3, 5);
        FlushToZero(scaled, 4, 6);
        FlushToZero(scaled, 7, 1);
        if (scale < 1.0)
        {
            AdjustRadii(width, scale, scaled, 0, 2);
            AdjustRadii(height, scale, scaled, 3, 5);
            AdjustRadii(width, scale, scaled, 4, 6);
            AdjustRadii(height, scale, scaled, 7, 1);
        }

        // Adjusting may zero one radius of a corner; zero its companion too.
        ClampToZero(scaled);
        return new SkRRect(sorted, scaled, ComputeType(sorted, scaled));
    }

    private static SkRRect AsRect(SkRect rect) => new(rect, new float[8], SkRRectType.Rect);

    // Clamps non-positive radii (and their companions) to zero; true when every corner is square.
    private static bool ClampToZero(float[] radii)
    {
        bool allCornersSquare = true;
        for (int corner = 0; corner < 4; corner++)
        {
            if (radii[corner * 2] <= 0.0f || radii[(corner * 2) + 1] <= 0.0f)
            {
                radii[corner * 2] = 0.0f;
                radii[(corner * 2) + 1] = 0.0f;
            }
            else
            {
                allCornersSquare = false;
            }
        }

        return allCornersSquare;
    }

    private static double MinScale(double radius1, double radius2, double limit, double current) =>
        radius1 + radius2 > limit ? Math.Min(current, limit / (radius1 + radius2)) : current;

    private static void FlushToZero(float[] radii, int a, int b)
    {
        if (radii[a] + radii[b] == radii[a])
        {
            radii[b] = 0.0f;
        }
        else if (radii[a] + radii[b] == radii[b])
        {
            radii[a] = 0.0f;
        }
    }

    // SkScaleToSides::AdjustRadii.
    private static void AdjustRadii(double limit, double scale, float[] radii, int a, int b)
    {
        radii[a] = (float)(radii[a] * scale);
        radii[b] = (float)(radii[b] * scale);
        if (radii[a] + radii[b] > limit)
        {
            int minIndex = radii[a] > radii[b] ? b : a;
            int maxIndex = minIndex == a ? b : a;
            float newMinRadius = radii[minIndex];
            float newMaxRadius = (float)(limit - newMinRadius);
            // Reduce the larger radius an ulp at a time until the two fit.
            while (newMaxRadius + newMinRadius > limit)
            {
                newMaxRadius = MathF.BitDecrement(newMaxRadius);
            }

            radii[maxIndex] = newMaxRadius;
        }
    }

    // SkRRect::computeType.
    private static SkRRectType ComputeType(SkRect rect, float[] radii)
    {
        bool allRadiiEqual = true;
        bool allCornersSquare = radii[0] == 0.0f || radii[1] == 0.0f;
        for (int corner = 1; corner < 4; corner++)
        {
            if (radii[corner * 2] != 0.0f && radii[(corner * 2) + 1] != 0.0f)
            {
                allCornersSquare = false;
            }

            if (radii[corner * 2] != radii[(corner * 2) - 2] || radii[(corner * 2) + 1] != radii[(corner * 2) - 1])
            {
                allRadiiEqual = false;
            }
        }

        if (allCornersSquare)
        {
            return SkRRectType.Rect;
        }

        if (allRadiiEqual)
        {
            return radii[0] >= rect.Width * 0.5f && radii[1] >= rect.Height * 0.5f
                ? SkRRectType.Oval
                : SkRRectType.Simple;
        }

        // Nine-patch and complex rrects are built the same way.
        return SkRRectType.Complex;
    }
}
