using System.Collections;
using Plumix.Foundation;

namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/painting.dart (Tangent, PathMetrics, PathMetric)
// Engine adapter: engine/src/flutter/lib/ui/painting/path_measure.cc over
// skia/src/core/SkContourMeasure.cpp, ported with Skia's float math (PathSkiaParityTests).
public sealed class Tangent(Offset position, Offset vector)
{
    public Offset Position { get; } = position;
    public Offset Vector { get; } = vector;
    public double Angle => -Math.Atan2(Vector.Dy, Vector.Dx);
    public static Tangent FromAngle(Offset position, double angle) =>
        new(position, new Offset(Math.Cos(angle), Math.Sin(angle)));
}

/// <summary>A single-use iterable over a snapshot of a path's nonzero-length contours.</summary>
public sealed class PathMetrics : IEnumerable<PathMetric>
{
    private readonly PathMetricIterator _iterator;
    internal PathMetrics(Path path, bool forceClosed) => _iterator = new PathMetricIterator(path, forceClosed);
    public PathMetricIterator GetEnumerator() => _iterator;
    IEnumerator<PathMetric> IEnumerable<PathMetric>.GetEnumerator() => _iterator;
    IEnumerator IEnumerable.GetEnumerator() => _iterator;
}

public sealed class PathMetricIterator : IEnumerator<PathMetric>
{
    private readonly ContourMeasureIterator _contours;
    private PathMetric? _current;
    private int _contourIndex = -1;
    private bool _finished;

    internal PathMetricIterator(Path path, bool forceClosed) =>
        _contours = new ContourMeasureIterator(new Path(path), forceClosed);

    public PathMetric Current => _current ?? throw new InvalidOperationException(
        "PathMetricIterator is not pointing to a PathMetric. This can happen in two situations:\n"
        + "- The iteration has not started yet. If so, call \"moveNext\" to start iteration.\n"
        + "- The iterator ran out of elements. If so, check that \"moveNext\" returns true "
        + "prior to calling \"current\".");
    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (_finished)
        {
            return false;
        }

        if (_contours.Next() is not { } contour)
        {
            _current = null;
            _finished = true;
            return false;
        }

        _contourIndex++;
        _current = new PathMetric(contour, _contourIndex);
        return true;
    }

    public void Reset() => throw new NotSupportedException("PathMetrics does not restart iteration.");

    // Dart returns the same iterator on subsequent enumerations, including after First/Take, so the
    // shared stream survives foreach disposal.
    public void Dispose() { }
}

public sealed class PathMetric
{
    private readonly ContourMeasure _measure;

    internal PathMetric(ContourMeasure measure, int contourIndex)
    {
        _measure = measure;
        ContourIndex = contourIndex;
    }

    public double Length => _measure.Length;
    public bool IsClosed => _measure.IsClosed;
    public int ContourIndex { get; }

    public Tangent? GetTangentForOffset(double distance) =>
        _measure.GetPositionAndTangent(Path.Narrow(distance)) is ({ } position, { } tangent)
            ? new Tangent(new Offset(position.X, position.Y), new Offset(tangent.X, tangent.Y))
            : null;

    public Path ExtractPath(double start, double end, bool startWithMoveTo = true)
    {
        var result = new Path();
        _measure.GetSegment(Path.Narrow(start), Path.Narrow(end), result, startWithMoveTo);
        return result;
    }

    public override string ToString()
    {
        string length = BindingBase.DartDoubleToString(Length);
        return $"PathMetric(length: {length}, isClosed: {IsClosed.ToString().ToLowerInvariant()}, "
               + $"contourIndex: {ContourIndex})";
    }
}

/// <summary>Skia's <c>SkContourMeasure::Segment</c>: one flattened piece of a contour.</summary>
internal readonly record struct ContourSegment(float Distance, int PointIndex, int TValue, PathVerb Type)
{
    // kMaxTValue: t is stored as a 30-bit fixed-point value.
    internal const int MaxTValue = 0x3FFFFFFF;

    internal float ScalarT => TValue * (1.0f / MaxTValue);
}

/// <summary>Skia's <c>SkContourMeasure</c>: one nonzero-length contour, flattened for measuring.</summary>
internal sealed class ContourMeasure(ContourSegment[] segments, PathPoint[] points, float length, bool isClosed)
{
    internal float Length { get; } = length;

    internal bool IsClosed { get; } = isClosed;

    // SkContourMeasure::distanceToSegment.
    private int DistanceToSegment(float distance, out float t)
    {
        // SkTSearch: the first segment whose distance is not below the key.
        int low = 0;
        int high = segments.Length - 1;
        while (low < high)
        {
            int middle = low + ((high - low) >> 1);
            if (segments[middle].Distance < distance)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        int index = high;
        ContourSegment segment = segments[index];
        // Interpolate t with the previous segment when it covers the same points.
        float startT = 0.0f;
        float startD = 0.0f;
        if (index > 0)
        {
            startD = segments[index - 1].Distance;
            if (segments[index - 1].PointIndex == segment.PointIndex)
            {
                startT = segments[index - 1].ScalarT;
            }
        }

        t = startT + ((segment.ScalarT - startT) * (distance - startD) / (segment.Distance - startD));
        return index;
    }

    // SkContourMeasure::getPosTan.
    internal (PathPoint? Position, PathPoint? Tangent) GetPositionAndTangent(float distance)
    {
        if (float.IsNaN(distance))
        {
            return (null, null);
        }

        distance = Math.Clamp(distance, 0.0f, Length);
        int index = DistanceToSegment(distance, out float t);
        if (float.IsNaN(t))
        {
            return (null, null);
        }

        ContourSegment segment = segments[index];
        (PathPoint position, PathPoint tangent) = PositionAndTangent(segment, t);
        return (position, tangent);
    }

    private ReadOnlySpan<PathPoint> PointsOf(ContourSegment segment) =>
        points.AsSpan(segment.PointIndex, Math.Min(4, points.Length - segment.PointIndex));

    // compute_pos_tan.
    private (PathPoint Position, PathPoint Tangent) PositionAndTangent(ContourSegment segment, float t)
    {
        ReadOnlySpan<PathPoint> p = PointsOf(segment);
        switch (segment.Type)
        {
            case PathVerb.Line:
                return (
                    new PathPoint(PathGeometry.Interp(p[0].X, p[1].X, t), PathGeometry.Interp(p[0].Y, p[1].Y, t)),
                    new PathPoint(p[1].X - p[0].X, p[1].Y - p[0].Y).WithLength(1.0f));
            case PathVerb.Quad:
                return (QuadAt(p, t), PathGeometry.QuadTangentAt(p, t).WithLength(1.0f));
            case PathVerb.Conic:
            {
                // The weight is stored in the point after the start: (start, (w, 0), control, end).
                ReadOnlySpan<PathPoint> conic = [p[0], p[2], p[3]];
                float weight = p[1].X;
                return (ConicAt(conic, weight, t), PathGeometry.ConicTangentAt(conic, weight, t).WithLength(1.0f));
            }

            default:
                return (CubicAt(p, t), PathGeometry.CubicTangentAt(p, t).WithLength(1.0f));
        }
    }

    // SkQuadCoeff(src).eval(t).
    internal static PathPoint QuadAt(ReadOnlySpan<PathPoint> p, float t)
    {
        PathPoint c = p[0];
        PathPoint b = (p[1] - c) + (p[1] - c);
        PathPoint a = p[2] - (p[1] + p[1]) + c;
        return new PathPoint((((a.X * t) + b.X) * t) + c.X, (((a.Y * t) + b.Y) * t) + c.Y);
    }

    // SkCubicCoeff(src).eval(t).
    internal static PathPoint CubicAt(ReadOnlySpan<PathPoint> p, float t)
    {
        PathPoint p0 = p[0];
        PathPoint p1 = p[1];
        PathPoint p2 = p[2];
        PathPoint p3 = p[3];
        var a = new PathPoint(p3.X + (3 * (p1.X - p2.X)) - p0.X, p3.Y + (3 * (p1.Y - p2.Y)) - p0.Y);
        PathPoint inner = p2 - (p1 + p1) + p0;
        var b = new PathPoint(3 * inner.X, 3 * inner.Y);
        var c = new PathPoint(3 * (p1.X - p0.X), 3 * (p1.Y - p0.Y));
        return new PathPoint(
            (((((a.X * t) + b.X) * t) + c.X) * t) + p0.X,
            (((((a.Y * t) + b.Y) * t) + c.Y) * t) + p0.Y);
    }

    // SkConic::evalAt (SkConicCoeff).
    internal static PathPoint ConicAt(ReadOnlySpan<PathPoint> p, float weight, float t)
    {
        (PathPoint numeratorA, PathPoint numeratorB, PathPoint numeratorC, float denominatorA, float denominatorB) =
            ConicCoefficients(p, weight);
        var numerator = new PathPoint(
            (((numeratorA.X * t) + numeratorB.X) * t) + numeratorC.X,
            (((numeratorA.Y * t) + numeratorB.Y) * t) + numeratorC.Y);
        float denominator = (((denominatorA * t) + denominatorB) * t) + 1.0f;
        return new PathPoint(numerator.X / denominator, numerator.Y / denominator);
    }

    private static (PathPoint NumeratorA, PathPoint NumeratorB, PathPoint NumeratorC, float DenominatorA,
        float DenominatorB) ConicCoefficients(ReadOnlySpan<PathPoint> p, float weight)
    {
        PathPoint p1W = p[1].Scale(weight);
        PathPoint c = p[0];
        PathPoint a = p[2] - (p1W + p1W) + c;
        PathPoint b = (p1W - c) + (p1W - c);
        float denominatorB = 2 * (weight - 1.0f);
        return (a, b, c, 0.0f - denominatorB, denominatorB);
    }

    // SkConic::chopAt(t1, t2, dst).
    private static (PathPoint P0, PathPoint P1, PathPoint P2, float Weight)? ChopConicBetween(
        ReadOnlySpan<PathPoint> p, float weight, float t1, float t2)
    {
        Span<PathPoint> pair = stackalloc PathPoint[5];
        if (t1 == 0.0f || t2 == 1.0f)
        {
            if (t1 == 0.0f && t2 == 1.0f)
            {
                return (p[0], p[1], p[2], weight);
            }

            if (PathGeometry.ChopConicAt(p, weight, t1 != 0.0f ? t1 : t2, pair, out float w0, out float w1))
            {
                return t1 != 0.0f ? (pair[2], pair[3], pair[4], w1) : (pair[0], pair[1], pair[2], w0);
            }
        }

        (PathPoint numeratorA, PathPoint numeratorB, PathPoint numeratorC, float denominatorA, float denominatorB) =
            ConicCoefficients(p, weight);
        PathPoint Numerator(float t) => new(
            (((numeratorA.X * t) + numeratorB.X) * t) + numeratorC.X,
            (((numeratorA.Y * t) + numeratorB.Y) * t) + numeratorC.Y);
        float Denominator(float t) => (((denominatorA * t) + denominatorB) * t) + 1.0f;
        PathPoint aXY = Numerator(t1);
        float aZ = Denominator(t1);
        float middleT = (t1 + t2) / 2;
        PathPoint dXY = Numerator(middleT);
        float dZ = Denominator(middleT);
        PathPoint cXY = Numerator(t2);
        float cZ = Denominator(t2);
        PathPoint bXY = (dXY + dXY) - (aXY + cXY).Scale(0.5f);
        float bZ = (dZ + dZ) - ((aZ + cZ) * 0.5f);
        return (
            new PathPoint(aXY.X / aZ, aXY.Y / aZ),
            new PathPoint(bXY.X / bZ, bXY.Y / bZ),
            new PathPoint(cXY.X / cZ, cXY.Y / cZ),
            bZ / MathF.Sqrt(aZ * cZ));
    }

    // SkContourMeasure::getSegment.
    internal bool GetSegment(float startD, float stopD, Path destination, bool startWithMoveTo)
    {
        if (startD < 0.0f)
        {
            startD = 0.0f;
        }

        if (stopD > Length)
        {
            stopD = Length;
        }

        // Also catches NaN.
        if (!(startD <= stopD) || segments.Length == 0)
        {
            return false;
        }

        int index = DistanceToSegment(startD, out float startT);
        if (!float.IsFinite(startT))
        {
            return false;
        }

        int stopIndex = DistanceToSegment(stopD, out float stopT);
        if (!float.IsFinite(stopT))
        {
            return false;
        }

        if (startWithMoveTo)
        {
            PathPoint position = PositionAndTangent(segments[index], startT).Position;
            destination.MoveTo(position.X, position.Y);
        }

        if (segments[index].PointIndex == segments[stopIndex].PointIndex)
        {
            SegmentTo(segments[index], startT, stopT, destination);
            return true;
        }

        do
        {
            SegmentTo(segments[index], startT, 1.0f, destination);
            int pointIndex = segments[index].PointIndex;
            do
            {
                index++;
            }
            while (segments[index].PointIndex == pointIndex);

            startT = 0.0f;
        }
        while (segments[index].PointIndex < segments[stopIndex].PointIndex);

        SegmentTo(segments[index], 0.0f, stopT, destination);
        return true;
    }

    // SkContourMeasure_segTo.
    private void SegmentTo(ContourSegment segment, float startT, float stopT, Path destination)
    {
        if (startT == stopT)
        {
            // A zero-length dash: a zero-length line, which the stroker caps.
            if (destination.Verbs.Count != 0)
            {
                PathPoint last = destination.Points[^1];
                destination.LineTo(last.X, last.Y);
            }

            return;
        }

        ReadOnlySpan<PathPoint> p = PointsOf(segment);
        Span<PathPoint> first = stackalloc PathPoint[7];
        Span<PathPoint> second = stackalloc PathPoint[7];
        switch (segment.Type)
        {
            case PathVerb.Line:
                if (stopT == 1.0f)
                {
                    destination.LineTo(p[1].X, p[1].Y);
                }
                else
                {
                    destination.LineTo(PathGeometry.Interp(p[0].X, p[1].X, stopT),
                        PathGeometry.Interp(p[0].Y, p[1].Y, stopT));
                }

                break;
            case PathVerb.Quad:
                if (startT == 0.0f)
                {
                    if (stopT == 1.0f)
                    {
                        QuadTo(destination, p[1], p[2]);
                    }
                    else
                    {
                        PathGeometry.ChopQuadAt(p, first, stopT);
                        QuadTo(destination, first[1], first[2]);
                    }
                }
                else
                {
                    PathGeometry.ChopQuadAt(p, first, startT);
                    if (stopT == 1.0f)
                    {
                        QuadTo(destination, first[3], first[4]);
                    }
                    else
                    {
                        PathGeometry.ChopQuadAt(first[2..], second, (stopT - startT) / (1 - startT));
                        QuadTo(destination, second[1], second[2]);
                    }
                }

                break;
            case PathVerb.Conic:
            {
                ReadOnlySpan<PathPoint> conic = [p[0], p[2], p[3]];
                float weight = p[1].X;
                if (startT == 0.0f && stopT == 1.0f)
                {
                    ConicTo(destination, conic[1], conic[2], weight);
                }
                else if (startT == 0.0f || stopT == 1.0f)
                {
                    float t = startT == 0.0f ? stopT : startT;
                    if (PathGeometry.ChopConicAt(conic, weight, t, first, out float w0, out float w1))
                    {
                        if (startT == 0.0f)
                        {
                            ConicTo(destination, first[1], first[2], w0);
                        }
                        else
                        {
                            ConicTo(destination, first[3], first[4], w1);
                        }
                    }
                }
                else if (ChopConicBetween(conic, weight, startT, stopT) is { } chopped)
                {
                    ConicTo(destination, chopped.P1, chopped.P2, chopped.Weight);
                }

                break;
            }

            default:
                if (startT == 0.0f)
                {
                    if (stopT == 1.0f)
                    {
                        CubicTo(destination, p[1], p[2], p[3]);
                    }
                    else
                    {
                        PathGeometry.ChopCubicAt(p, first, stopT);
                        CubicTo(destination, first[1], first[2], first[3]);
                    }
                }
                else
                {
                    PathGeometry.ChopCubicAt(p, first, startT);
                    if (stopT == 1.0f)
                    {
                        CubicTo(destination, first[4], first[5], first[6]);
                    }
                    else
                    {
                        PathGeometry.ChopCubicAt(first[3..], second, (stopT - startT) / (1 - startT));
                        CubicTo(destination, second[1], second[2], second[3]);
                    }
                }

                break;
        }
    }

    private static void QuadTo(Path path, PathPoint control, PathPoint end) =>
        path.QuadraticBezierTo(control.X, control.Y, end.X, end.Y);

    private static void ConicTo(Path path, PathPoint control, PathPoint end, float weight) =>
        path.ConicTo(control.X, control.Y, end.X, end.Y, weight);

    private static void CubicTo(Path path, PathPoint control1, PathPoint control2, PathPoint end) =>
        path.CubicTo(control1.X, control1.Y, control2.X, control2.Y, end.X, end.Y);
}

/// <summary>Skia's <c>SkContourMeasureIter</c>: a path's nonzero-length contours, one at a time.</summary>
internal sealed class ContourMeasureIterator
{
    // CHEAP_DIST_LIMIT at a resolution scale of 1.
    private const float Tolerance = 0.5f;

    private readonly List<(PathVerb Verb, PathPoint[] Points, float Weight)> _segments;
    private readonly bool _forceClosed;
    private readonly List<ContourSegment> _contourSegments = [];
    private readonly List<PathPoint> _points = [];
    private int _next;

    internal ContourMeasureIterator(Path path, bool forceClosed)
    {
        _forceClosed = forceClosed;
        // A non-finite path measures as empty.
        _segments = path.Points.All(point => float.IsFinite(point.X) && float.IsFinite(point.Y))
            ? WithStartPoints(path)
            : [];
    }

    // SkPathPriv::Iterate: each segment with its start point first.
    private static List<(PathVerb, PathPoint[], float)> WithStartPoints(Path path)
    {
        var result = new List<(PathVerb, PathPoint[], float)>();
        PathPoint last = default;
        PathPoint moveTo = default;
        foreach ((PathVerb verb, PathPoint[] points, float weight) in path.Segments())
        {
            switch (verb)
            {
                case PathVerb.Move:
                    last = moveTo = points[0];
                    result.Add((verb, points, weight));
                    break;
                case PathVerb.Close:
                    result.Add((verb, [last], weight));
                    last = moveTo;
                    break;
                default:
                    result.Add((verb, [last, .. points], weight));
                    last = points[^1];
                    break;
            }
        }

        return result;
    }

    internal ContourMeasure? Next()
    {
        while (_next < _segments.Count)
        {
            if (BuildSegments() is { } contour)
            {
                return contour;
            }
        }

        return null;
    }

    // SkContourMeasureIter::Impl::buildSegments.
    private ContourMeasure? BuildSegments()
    {
        int pointIndex = -1;
        float distance = 0.0f;
        bool haveSeenClose = _forceClosed;
        bool haveSeenMoveTo = false;
        // As distance accumulates, each += must actually grow it: a tiny positive delta can vanish
        // against a large distance.
        _contourSegments.Clear();
        _points.Clear();
        for (; _next < _segments.Count; _next++)
        {
            (PathVerb verb, PathPoint[] points, float weight) = _segments[_next];
            if (haveSeenMoveTo && verb == PathVerb.Move)
            {
                break;
            }

            float previous = distance;
            switch (verb)
            {
                case PathVerb.Move:
                    pointIndex++;
                    _points.Add(points[0]);
                    haveSeenMoveTo = true;
                    break;
                case PathVerb.Line:
                    distance = LineSegment(points[0], points[1], distance, pointIndex);
                    if (distance > previous)
                    {
                        _points.Add(points[1]);
                        pointIndex++;
                    }

                    break;
                case PathVerb.Quad:
                    distance = QuadSegments(points, distance, 0, ContourSegment.MaxTValue, pointIndex);
                    if (distance > previous)
                    {
                        _points.Add(points[1]);
                        _points.Add(points[2]);
                        pointIndex += 2;
                    }

                    break;
                case PathVerb.Conic:
                    distance = ConicSegments(points, weight, distance, 0, points[0], ContourSegment.MaxTValue,
                        points[2], pointIndex);
                    if (distance > previous)
                    {
                        // The weight goes in the next point, followed by the last two points.
                        _points.Add(new PathPoint(weight, 0.0f));
                        _points.Add(points[1]);
                        _points.Add(points[2]);
                        pointIndex += 3;
                    }

                    break;
                case PathVerb.Cubic:
                    distance = CubicSegments(points, distance, 0, ContourSegment.MaxTValue, pointIndex);
                    if (distance > previous)
                    {
                        _points.Add(points[1]);
                        _points.Add(points[2]);
                        _points.Add(points[3]);
                        pointIndex += 3;
                    }

                    break;
                default:
                    haveSeenClose = true;
                    break;
            }
        }

        if (!float.IsFinite(distance) || _contourSegments.Count == 0)
        {
            return null;
        }

        if (haveSeenClose)
        {
            float previous = distance;
            PathPoint firstPoint = _points[0];
            distance = LineSegment(_points[pointIndex], firstPoint, distance, pointIndex);
            if (distance > previous)
            {
                _points.Add(firstPoint);
            }
        }

        return new ContourMeasure([.. _contourSegments], [.. _points], distance, haveSeenClose);
    }

    // SkPoint::Distance.
    private static float Distance(PathPoint a, PathPoint b)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;
        float squared = (dx * dx) + (dy * dy);
        return float.IsFinite(squared)
            ? MathF.Sqrt(squared)
            : (float)Math.Sqrt(((double)dx * dx) + ((double)dy * dy));
    }

    private static bool TSpanBigEnough(int tSpan) => (tSpan >> 10) != 0;

    private float AddSegment(float distance, float delta, int pointIndex, PathVerb type, int tValue)
    {
        float previous = distance;
        distance += delta;
        if (distance > previous)
        {
            _contourSegments.Add(new ContourSegment(distance, pointIndex, tValue, type));
        }

        return distance;
    }

    private float LineSegment(PathPoint p0, PathPoint p1, float distance, int pointIndex) =>
        AddSegment(distance, Distance(p0, p1), pointIndex, PathVerb.Line, ContourSegment.MaxTValue);

    // quad_too_curvy: the midpoint against the chord's midpoint.
    private static bool QuadTooCurvy(ReadOnlySpan<PathPoint> p)
    {
        float dx = (p[1].X * 0.5f) - ((p[0].X + p[2].X) * 0.5f * 0.5f);
        float dy = (p[1].Y * 0.5f) - ((p[0].Y + p[2].Y) * 0.5f * 0.5f);
        return Math.Max(Math.Abs(dx), Math.Abs(dy)) > Tolerance;
    }

    private float QuadSegments(ReadOnlySpan<PathPoint> p, float distance, int minT, int maxT, int pointIndex)
    {
        if (TSpanBigEnough(maxT - minT) && QuadTooCurvy(p))
        {
            Span<PathPoint> halves = stackalloc PathPoint[5];
            int halfT = (minT + maxT) >> 1;
            PathGeometry.ChopQuadAt(p, halves, 0.5f);
            PathPoint[] second = [halves[2], halves[3], halves[4]];
            distance = QuadSegments(halves[..3], distance, minT, halfT, pointIndex);
            return QuadSegments(second, distance, halfT, maxT, pointIndex);
        }

        return AddSegment(distance, Distance(p[0], p[2]), pointIndex, PathVerb.Quad, maxT);
    }

    private static bool ConicTooCurvy(PathPoint first, PathPoint middle, PathPoint last)
    {
        PathPoint midEnds = (first + last).Scale(0.5f);
        PathPoint delta = middle - midEnds;
        return Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)) > Tolerance;
    }

    private float ConicSegments(
        PathPoint[] conic,
        float weight,
        float distance,
        int minT,
        PathPoint minPoint,
        int maxT,
        PathPoint maxPoint,
        int pointIndex)
    {
        int halfT = (minT + maxT) >> 1;
        PathPoint halfPoint = ContourMeasure.ConicAt(conic, weight, halfT * (1.0f / ContourSegment.MaxTValue));
        if (!float.IsFinite(halfPoint.X) || !float.IsFinite(halfPoint.Y))
        {
            return distance;
        }

        if (TSpanBigEnough(maxT - minT) && ConicTooCurvy(minPoint, halfPoint, maxPoint))
        {
            distance = ConicSegments(conic, weight, distance, minT, minPoint, halfT, halfPoint, pointIndex);
            return ConicSegments(conic, weight, distance, halfT, halfPoint, maxT, maxPoint, pointIndex);
        }

        return AddSegment(distance, Distance(minPoint, maxPoint), pointIndex, PathVerb.Conic, maxT);
    }

    private static bool CheapDistanceExceedsLimit(PathPoint point, float x, float y) =>
        Math.Max(Math.Abs(x - point.X), Math.Abs(y - point.Y)) > Tolerance;

    // cubic_too_curvy: the control points against the chord's third points.
    private static bool CubicTooCurvy(ReadOnlySpan<PathPoint> p) =>
        CheapDistanceExceedsLimit(p[1],
            PathGeometry.Interp(p[0].X, p[3].X, 1.0f / 3), PathGeometry.Interp(p[0].Y, p[3].Y, 1.0f / 3))
        || CheapDistanceExceedsLimit(p[2],
            PathGeometry.Interp(p[0].X, p[3].X, 1.0f * 2 / 3), PathGeometry.Interp(p[0].Y, p[3].Y, 1.0f * 2 / 3));

    private float CubicSegments(ReadOnlySpan<PathPoint> p, float distance, int minT, int maxT, int pointIndex)
    {
        if (TSpanBigEnough(maxT - minT) && CubicTooCurvy(p))
        {
            Span<PathPoint> halves = stackalloc PathPoint[7];
            int halfT = (minT + maxT) >> 1;
            PathGeometry.ChopCubicAt(p, halves, 0.5f);
            PathPoint[] second = [halves[3], halves[4], halves[5], halves[6]];
            distance = CubicSegments(halves[..4], distance, minT, halfT, pointIndex);
            return CubicSegments(second, distance, halfT, maxT, pointIndex);
        }

        return AddSegment(distance, Distance(p[0], p[3]), pointIndex, PathVerb.Cubic, maxT);
    }
}
