namespace Plumix.UI;

// Engine adapter: skia/src/core/SkPath.cpp (SkPath::contains and its winding_* / tangent_* helpers, over
// SkPath::Iter with forceClose). Path.Contains answers exactly as the engine's Skia does, boundary points
// included (PathSkiaParityTests).

/// <summary>
/// Skia's <c>SkPath::Iter</c>: segments with their start point; every contour is closed by a line when
/// <c>forceClose</c> is set, and closed contours get their closing line either way.
/// </summary>
internal struct ClosingPathIterator
{
    private readonly IReadOnlyList<PathVerb> _verbs;
    private readonly IReadOnlyList<PathPoint> _points;
    private readonly IReadOnlyList<float> _weights;
    private int _verb;
    private int _point;
    private int _weight;
    private PathPoint _moveTo;
    private PathPoint _lastPoint;
    private readonly bool _forceClose;
    private bool _needClose;

    internal ClosingPathIterator(Path path, bool forceClose = true)
    {
        _verbs = path.Verbs;
        _points = path.Points;
        _weights = path.ConicWeights;
        _forceClose = forceClose;
    }

    /// <summary>The weight of the conic <see cref="Next"/> last returned.</summary>
    internal float ConicWeight { get; private set; }

    /// <summary>
    /// The next segment: <paramref name="points"/> holds its start point and the points it adds. Null at
    /// the end.
    /// </summary>
    internal PathVerb? Next(Span<PathPoint> points)
    {
        if (_verb == _verbs.Count)
        {
            if (_needClose)
            {
                if (AutoClose(points) == PathVerb.Line)
                {
                    return PathVerb.Line;
                }

                _needClose = false;
                return PathVerb.Close;
            }

            return null;
        }

        PathVerb verb = _verbs[_verb++];
        switch (verb)
        {
            case PathVerb.Move:
                if (_needClose)
                {
                    // Close the previous contour before moving on.
                    _verb--;
                    PathVerb closing = AutoClose(points);
                    if (closing == PathVerb.Close)
                    {
                        _needClose = false;
                    }

                    return closing;
                }

                if (_verb == _verbs.Count)
                {
                    // A trailing move.
                    return null;
                }

                _moveTo = _points[_point++];
                points[0] = _moveTo;
                _lastPoint = _moveTo;
                _needClose = _forceClose;
                return verb;
            case PathVerb.Line:
                points[0] = _lastPoint;
                points[1] = _points[_point++];
                _lastPoint = points[1];
                return verb;
            case PathVerb.Conic:
            case PathVerb.Quad:
                if (verb == PathVerb.Conic)
                {
                    ConicWeight = _weights[_weight++];
                }

                points[0] = _lastPoint;
                points[1] = _points[_point++];
                points[2] = _points[_point++];
                _lastPoint = points[2];
                return verb;
            case PathVerb.Cubic:
                points[0] = _lastPoint;
                points[1] = _points[_point++];
                points[2] = _points[_point++];
                points[3] = _points[_point++];
                _lastPoint = points[3];
                return verb;
            default:
                PathVerb result = AutoClose(points);
                if (result == PathVerb.Line)
                {
                    // Revisit the close after the closing line.
                    _verb--;
                }
                else
                {
                    _needClose = false;
                }

                _lastPoint = _moveTo;
                return result;
        }
    }

    // SkPath::Iter::autoClose.
    private PathVerb AutoClose(Span<PathPoint> points)
    {
        if (_lastPoint != _moveTo)
        {
            // NaN points compare unequal, but the iterator treats them as the same.
            if (float.IsNaN(_lastPoint.X) || float.IsNaN(_lastPoint.Y)
                                           || float.IsNaN(_moveTo.X) || float.IsNaN(_moveTo.Y))
            {
                return PathVerb.Close;
            }

            points[0] = _lastPoint;
            points[1] = _moveTo;
            _lastPoint = _moveTo;
            return PathVerb.Line;
        }

        points[0] = _moveTo;
        return PathVerb.Close;
    }
}

/// <summary>Skia's point-in-path test.</summary>
internal static class PathContainment
{
    // SkPath::contains.
    internal static bool Contains(Path path, float x, float y)
    {
        if (path.Verbs.Count == 0)
        {
            return false;
        }

        // The path's bounds over every point; a non-finite path's are empty.
        (float left, float top, float right, float bottom) = Bounds(path);
        if (!(left <= x && x <= right && top <= y && y <= bottom))
        {
            return false;
        }

        bool evenOdd = path.FillType == PathFillType.EvenOdd;
        Span<PathPoint> points = stackalloc PathPoint[4];
        int winding = 0;
        int onCurveCount = 0;
        var iterator = new ClosingPathIterator(path);
        while (iterator.Next(points) is { } verb)
        {
            winding += verb switch
            {
                PathVerb.Line => WindingLine(points, x, y, ref onCurveCount),
                PathVerb.Quad => WindingQuad(points, x, y, ref onCurveCount),
                PathVerb.Conic => WindingConic(points, x, y, iterator.ConicWeight, ref onCurveCount),
                PathVerb.Cubic => WindingCubic(points, x, y, ref onCurveCount),
                _ => 0,
            };
        }

        if (evenOdd)
        {
            winding &= 1;
        }

        if (winding != 0)
        {
            return true;
        }

        if (onCurveCount <= 1)
        {
            return onCurveCount != 0;
        }

        if ((onCurveCount & 1) != 0 || evenOdd)
        {
            return (onCurveCount & 1) != 0;
        }

        // An even number of on-curve hits under the non-zero rule: pair up coincident hits, the ones
        // with opposite tangents, and count what is left.
        var tangents = new List<PathPoint>();
        iterator = new ClosingPathIterator(path);
        while (iterator.Next(points) is { } verb)
        {
            int oldCount = tangents.Count;
            switch (verb)
            {
                case PathVerb.Line:
                    TangentLine(points, x, y, tangents);
                    break;
                case PathVerb.Quad:
                    TangentQuad(points, x, y, tangents);
                    break;
                case PathVerb.Conic:
                    TangentConic(points, x, y, iterator.ConicWeight, tangents);
                    break;
                case PathVerb.Cubic:
                    TangentCubic(points, x, y, tangents);
                    break;
            }

            if (tangents.Count <= oldCount)
            {
                continue;
            }

            int last = tangents.Count - 1;
            PathPoint tangent = tangents[last];
            if (PathMath.IsNearlyZero((tangent.X * tangent.X) + (tangent.Y * tangent.Y)))
            {
                tangents.RemoveAt(last);
                continue;
            }

            for (int index = 0; index < last; index++)
            {
                PathPoint test = tangents[index];
                if (PathMath.IsNearlyZero((test.X * tangent.Y) - (test.Y * tangent.X))
                    && SignAsInt(tangent.X * test.X) <= 0
                    && SignAsInt(tangent.Y * test.Y) <= 0)
                {
                    tangents.RemoveAt(last);
                    // SkTDArray::removeShuffle: the last entry takes the removed one's place.
                    tangents[index] = tangents[^1];
                    tangents.RemoveAt(tangents.Count - 1);
                    break;
                }
            }
        }

        return tangents.Count != 0;
    }

    private static (float Left, float Top, float Right, float Bottom) Bounds(Path path)
    {
        IReadOnlyList<PathPoint> points = path.Points;
        float left = points[0].X;
        float top = points[0].Y;
        float right = left;
        float bottom = top;
        foreach (PathPoint point in points)
        {
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
            {
                return (0.0f, 0.0f, 0.0f, 0.0f);
            }

            left = Math.Min(left, point.X);
            top = Math.Min(top, point.Y);
            right = Math.Max(right, point.X);
            bottom = Math.Max(bottom, point.Y);
        }

        return (left, top, right, bottom);
    }

    private static bool Between(float a, float b, float c) => (a - b) * (c - b) <= 0.0f;

    private static bool CheckOnCurve(float x, float y, PathPoint start, PathPoint end) =>
        start.Y == end.Y ? Between(start.X, x, end.X) && x != end.X : x == start.X && y == start.Y;

    private static int SignAsInt(float value) => value < 0.0f ? -1 : value > 0.0f ? 1 : 0;

    private static int WindingLine(ReadOnlySpan<PathPoint> points, float x, float y, ref int onCurveCount)
    {
        float x0 = points[0].X;
        float y0 = points[0].Y;
        float x1 = points[1].X;
        float y1 = points[1].Y;
        float dy = y1 - y0;
        int direction = 1;
        if (y0 > y1)
        {
            (y0, y1) = (y1, y0);
            direction = -1;
        }

        if (y < y0 || y > y1)
        {
            return 0;
        }

        if (CheckOnCurve(x, y, points[0], points[1]))
        {
            onCurveCount++;
            return 0;
        }

        if (y == y1)
        {
            return 0;
        }

        float cross = ((x1 - x0) * (y - points[0].Y)) - (dy * (x - x0));
        if (cross == 0.0f)
        {
            // On the line, away from its end points (handled above).
            if (x != x1 || y != points[1].Y)
            {
                onCurveCount++;
            }

            direction = 0;
        }
        else if (SignAsInt(cross) == direction)
        {
            direction = 0;
        }

        return direction;
    }

    private static bool IsMonoQuad(float y0, float y1, float y2)
    {
        if (y0 == y1)
        {
            return true;
        }

        return y0 < y1 ? y1 <= y2 : y1 >= y2;
    }

    private static int WindingQuad(ReadOnlySpan<PathPoint> points, float x, float y, ref int onCurveCount)
    {
        if (IsMonoQuad(points[0].Y, points[1].Y, points[2].Y))
        {
            return WindingMonoQuad(points, x, y, ref onCurveCount);
        }

        Span<PathPoint> chopped = stackalloc PathPoint[5];
        int count = PathGeometry.ChopQuadAtYExtrema(points, chopped);
        int winding = WindingMonoQuad(chopped, x, y, ref onCurveCount);
        if (count > 0)
        {
            winding += WindingMonoQuad(chopped[2..], x, y, ref onCurveCount);
        }

        return winding;
    }

    private static int WindingMonoQuad(ReadOnlySpan<PathPoint> points, float x, float y, ref int onCurveCount)
    {
        float y0 = points[0].Y;
        float y2 = points[2].Y;
        int direction = 1;
        if (y0 > y2)
        {
            (y0, y2) = (y2, y0);
            direction = -1;
        }

        if (y < y0 || y > y2)
        {
            return 0;
        }

        if (CheckOnCurve(x, y, points[0], points[2]))
        {
            onCurveCount++;
            return 0;
        }

        if (y == y2)
        {
            return 0;
        }

        Span<float> roots = stackalloc float[2];
        int count = PathGeometry.FindUnitQuadRoots(
            points[0].Y - (2 * points[1].Y) + points[2].Y,
            2 * (points[1].Y - points[0].Y),
            points[0].Y - y,
            roots);
        float xt;
        if (count == 0)
        {
            // No roots only when y0 == y: point 0 going up, point 2 going down.
            xt = points[1 - direction].X;
        }
        else
        {
            float c = points[0].X;
            float a = points[2].X - (2 * points[1].X) + c;
            float b = 2 * (points[1].X - c);
            xt = (float)PathGeometry.PolyEval(a, b, c, roots[0]);
        }

        return EndWinding(points[2], xt, x, y, direction, ref onCurveCount);
    }

    // The shared tail of the curve windings: an on-curve hit away from the end point (which is the next
    // segment's start point) counts as on the curve.
    private static int EndWinding(PathPoint end, float xt, float x, float y, int direction, ref int onCurveCount)
    {
        if (PathMath.NearlyEqual(xt, x) && (x != end.X || y != end.Y))
        {
            onCurveCount++;
            return 0;
        }

        return xt < x ? direction : 0;
    }

    private static int WindingConic(
        ReadOnlySpan<PathPoint> points, float x, float y, float weight, ref int onCurveCount)
    {
        // Very large points may make a conic non-monotonic yet fail to chop; then it stays whole.
        Span<PathPoint> chopped = stackalloc PathPoint[5];
        if (IsMonoQuad(points[0].Y, points[1].Y, points[2].Y)
            || !PathGeometry.ChopConicAtYExtrema(points, weight, chopped, out float weight0, out float weight1))
        {
            return WindingMonoConic(points, weight, x, y, ref onCurveCount);
        }

        return WindingMonoConic(chopped, weight0, x, y, ref onCurveCount)
               + WindingMonoConic(chopped[2..], weight1, x, y, ref onCurveCount);
    }

    private static int WindingMonoConic(
        ReadOnlySpan<PathPoint> points, float weight, float x, float y, ref int onCurveCount)
    {
        float y0 = points[0].Y;
        float y2 = points[2].Y;
        int direction = 1;
        if (y0 > y2)
        {
            (y0, y2) = (y2, y0);
            direction = -1;
        }

        if (y < y0 || y > y2)
        {
            return 0;
        }

        if (CheckOnCurve(x, y, points[0], points[2]))
        {
            onCurveCount++;
            return 0;
        }

        if (y == y2)
        {
            return 0;
        }

        Span<float> roots = stackalloc float[2];
        float a = points[2].Y;
        float b = (points[1].Y * weight) - (y * weight) + y;
        float c = points[0].Y;
        // A = a + c - 2 * (b * w - y * w + y), B = b * w - w * y + y - a.
        a += c - (2 * b);
        b -= c;
        c -= y;
        int count = PathGeometry.FindUnitQuadRoots(a, 2 * b, c, roots);
        float xt;
        if (count == 0)
        {
            xt = points[1 - direction].X;
        }
        else
        {
            float t = roots[0];
            xt = (float)(PathGeometry.ConicEvalNumerator(points[0].X, points[1].X, points[2].X, weight, t)
                         / PathGeometry.ConicEvalDenominator(weight, t));
        }

        return EndWinding(points[2], xt, x, y, direction, ref onCurveCount);
    }

    private static int WindingCubic(ReadOnlySpan<PathPoint> points, float x, float y, ref int onCurveCount)
    {
        Span<PathPoint> chopped = stackalloc PathPoint[10];
        int count = PathGeometry.ChopCubicAtYExtrema(points, chopped);
        int winding = 0;
        for (int index = 0; index <= count; index++)
        {
            winding += WindingMonoCubic(chopped.Slice(index * 3, 4), x, y, ref onCurveCount);
        }

        return winding;
    }

    private static int WindingMonoCubic(ReadOnlySpan<PathPoint> points, float x, float y, ref int onCurveCount)
    {
        float y0 = points[0].Y;
        float y3 = points[3].Y;
        int direction = 1;
        if (y0 > y3)
        {
            (y0, y3) = (y3, y0);
            direction = -1;
        }

        if (y < y0 || y > y3)
        {
            return 0;
        }

        if (CheckOnCurve(x, y, points[0], points[3]))
        {
            onCurveCount++;
            return 0;
        }

        if (y == y3)
        {
            return 0;
        }

        // Quick reject or accept on the x extent.
        float min = points[0].X;
        float max = min;
        for (int index = 1; index < 4; index++)
        {
            min = Math.Min(min, points[index].X);
            max = Math.Max(max, points[index].X);
        }

        if (x < min)
        {
            return 0;
        }

        if (x > max)
        {
            return direction;
        }

        if (!PathGeometry.ChopMonoCubicAtY(points, y, out float t))
        {
            return 0;
        }

        float xt = PathGeometry.EvalCubic(points[0].X, points[1].X, points[2].X, points[3].X, t);
        return EndWinding(points[3], xt, x, y, direction, ref onCurveCount);
    }

    private static void TangentLine(ReadOnlySpan<PathPoint> points, float x, float y, List<PathPoint> tangents)
    {
        float y0 = points[0].Y;
        float y1 = points[1].Y;
        if (!Between(y0, y, y1))
        {
            return;
        }

        float x0 = points[0].X;
        float x1 = points[1].X;
        if (!Between(x0, x, x1))
        {
            return;
        }

        float dx = x1 - x0;
        float dy = y1 - y0;
        if (!PathMath.NearlyEqual((x - x0) * dy, dx * (y - y0)))
        {
            return;
        }

        tangents.Add(new PathPoint(dx, dy));
    }

    private static bool OutsideQuadOrConicHull(ReadOnlySpan<PathPoint> points, float x, float y) =>
        (!Between(points[0].Y, y, points[1].Y) && !Between(points[1].Y, y, points[2].Y))
        || (!Between(points[0].X, x, points[1].X) && !Between(points[1].X, x, points[2].X));

    private static void TangentQuad(ReadOnlySpan<PathPoint> points, float x, float y, List<PathPoint> tangents)
    {
        if (OutsideQuadOrConicHull(points, x, y))
        {
            return;
        }

        Span<float> roots = stackalloc float[2];
        int count = PathGeometry.FindUnitQuadRoots(
            points[0].Y - (2 * points[1].Y) + points[2].Y,
            2 * (points[1].Y - points[0].Y),
            points[0].Y - y,
            roots);
        for (int index = 0; index < count; index++)
        {
            float t = roots[index];
            float c = points[0].X;
            float a = points[2].X - (2 * points[1].X) + c;
            float b = 2 * (points[1].X - c);
            float xt = (float)PathGeometry.PolyEval(a, b, c, t);
            if (PathMath.NearlyEqual(x, xt))
            {
                tangents.Add(PathGeometry.QuadTangentAt(points, t));
            }
        }
    }

    private static void TangentConic(
        ReadOnlySpan<PathPoint> points, float x, float y, float weight, List<PathPoint> tangents)
    {
        if (OutsideQuadOrConicHull(points, x, y))
        {
            return;
        }

        Span<float> roots = stackalloc float[2];
        float a = points[2].Y;
        float b = (points[1].Y * weight) - (y * weight) + y;
        float c = points[0].Y;
        a += c - (2 * b);
        b -= c;
        c -= y;
        int count = PathGeometry.FindUnitQuadRoots(a, 2 * b, c, roots);
        for (int index = 0; index < count; index++)
        {
            float t = roots[index];
            float xt = (float)(PathGeometry.ConicEvalNumerator(points[0].X, points[1].X, points[2].X, weight, t)
                               / PathGeometry.ConicEvalDenominator(weight, t));
            if (PathMath.NearlyEqual(x, xt))
            {
                tangents.Add(PathGeometry.ConicTangentAt(points, weight, t));
            }
        }
    }

    private static void TangentCubic(ReadOnlySpan<PathPoint> points, float x, float y, List<PathPoint> tangents)
    {
        if (!Between(points[0].Y, y, points[1].Y) && !Between(points[1].Y, y, points[2].Y)
                                                   && !Between(points[2].Y, y, points[3].Y))
        {
            return;
        }

        if (!Between(points[0].X, x, points[1].X) && !Between(points[1].X, x, points[2].X)
                                                   && !Between(points[2].X, x, points[3].X))
        {
            return;
        }

        Span<PathPoint> chopped = stackalloc PathPoint[10];
        int count = PathGeometry.ChopCubicAtYExtrema(points, chopped);
        for (int index = 0; index <= count; index++)
        {
            ReadOnlySpan<PathPoint> cubic = chopped.Slice(index * 3, 4);
            if (!PathGeometry.ChopMonoCubicAtY(cubic, y, out float t))
            {
                continue;
            }

            float xt = PathGeometry.EvalCubic(cubic[0].X, cubic[1].X, cubic[2].X, cubic[3].X, t);
            if (PathMath.NearlyEqual(x, xt))
            {
                tangents.Add(PathGeometry.CubicTangentAt(cubic, t));
            }
        }
    }
}
