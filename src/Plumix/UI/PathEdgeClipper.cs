namespace Plumix.UI;

// Engine adapter: skia/src/core/SkEdgeClipper.cpp, SkLineClipper.cpp, SkPathPriv.h (SkPathEdgeIter), and the
// SkChopMonoCubicAtX/Y root finding of SkGeometry.cpp with src/base/SkCubics.cpp, SkQuads.cpp,
// SkBezierCurves.cpp and SkFloatingPoint.cpp at chrome/m119. SkPath::transform's perspective clip
// (PathPerspective) clips the path's edges against a half plane through these, with Skia's float
// operations in Skia's order (PathSkiaParityTests).

/// <summary>
/// Skia's <c>SkEdgeClipper</c>: clips one edge (line, quad or cubic) to a rectangle, producing up to
/// <see cref="MaxVerbs"/> lines and curves that are read back with <see cref="Next"/>.
/// </summary>
internal sealed class PathEdgeClipper(bool canCullToTheRight)
{
    // Max curvature in X and Y splits a cubic into 9 pieces, times (line + cubic); 2 lines + 1 cubic need
    // 6 points.
    private const int MaxVerbs = 18;
    private const int MaxPoints = 54;

    private readonly PathPoint[] _points = new PathPoint[MaxPoints];
    private readonly PathVerb?[] _verbs = new PathVerb?[MaxVerbs + 1];
    private int _currentPoint;
    private int _currentVerb;

    private bool CanCullToTheRight { get; } = canCullToTheRight;

    // SkEdgeClipper::ClipPath: clips each edge of every contour (each closed by a line), conics as quads,
    // and hands each clipper result with whether it starts a new contour to consume.
    internal static void ClipPath(
        IReadOnlyList<PathVerb> verbs,
        IReadOnlyList<PathPoint> points,
        IReadOnlyList<float> weights,
        SkRect clip,
        bool canCullToTheRight,
        Action<PathEdgeClipper, bool> consume)
    {
        const float conicTolerance = 1.0f / 4;
        var iterator = new PathEdgeIterator(verbs, points, weights);
        var clipper = new PathEdgeClipper(canCullToTheRight);
        Span<PathPoint> edge = stackalloc PathPoint[4];
        while (iterator.Next(edge, out bool isNewContour) is { } verb)
        {
            switch (verb)
            {
                case PathVerb.Line:
                    if (clipper.ClipLine(edge[0], edge[1], clip))
                    {
                        consume(clipper, isNewContour);
                    }

                    break;
                case PathVerb.Quad:
                    if (clipper.ClipQuad(edge, clip))
                    {
                        consume(clipper, isNewContour);
                    }

                    break;
                case PathVerb.Conic:
                    int quadCount = PathGeometry.ConicToQuads(edge, iterator.ConicWeight, conicTolerance,
                        out PathPoint[] quads);
                    for (int index = 0; index < quadCount; ++index)
                    {
                        if (clipper.ClipQuad(quads.AsSpan(index * 2, 3), clip))
                        {
                            consume(clipper, isNewContour);
                        }
                    }

                    break;
                default:
                    if (clipper.ClipCubic(edge, clip))
                    {
                        consume(clipper, isNewContour);
                    }

                    break;
            }
        }
    }

    // SkEdgeClipper::next: the next clipped segment's points (2, 3 or 4), or null when done.
    internal PathVerb? Next(Span<PathPoint> points)
    {
        PathVerb? verb = _verbs[_currentVerb];
        int count = verb switch
        {
            PathVerb.Line => 2,
            PathVerb.Quad => 3,
            PathVerb.Cubic => 4,
            _ => 0,
        };
        if (count > 0)
        {
            _points.AsSpan(_currentPoint, count).CopyTo(points);
            _currentPoint += count;
            _currentVerb += 1;
        }

        return verb;
    }

    // SkEdgeClipper::clipLine.
    private bool ClipLine(PathPoint p0, PathPoint p1, SkRect clip)
    {
        Begin();
        Span<PathPoint> lines = stackalloc PathPoint[PathLineClipper.MaxPoints];
        int lineCount = PathLineClipper.ClipLine(p0, p1, clip, lines, CanCullToTheRight);
        for (int index = 0; index < lineCount; index++)
        {
            AppendLine(lines[index], lines[index + 1]);
        }

        return End();
    }

    private void Begin()
    {
        _currentPoint = 0;
        _currentVerb = 0;
    }

    private bool End()
    {
        _verbs[_currentVerb] = null;
        _currentPoint = 0;
        _currentVerb = 0;
        return _verbs[0] is not null;
    }

    // SkEdgeClipper::clipQuad.
    private bool ClipQuad(ReadOnlySpan<PathPoint> source, SkRect clip)
    {
        Begin();
        SkRect bounds = PathPerspective.BoundsCheck(source[..3]);
        if (!QuickReject(bounds, clip))
        {
            Span<PathPoint> monoY = stackalloc PathPoint[5];
            Span<PathPoint> monoX = stackalloc PathPoint[5];
            int countY = PathGeometry.ChopQuadAtYExtrema(source, monoY);
            for (int y = 0; y <= countY; y++)
            {
                int countX = PathGeometry.ChopQuadAtXExtrema(monoY.Slice(y * 2, 3), monoX);
                for (int x = 0; x <= countX; x++)
                {
                    ClipMonoQuad(monoX.Slice(x * 2, 3), clip);
                }
            }
        }

        return End();
    }

    private static bool QuickReject(SkRect bounds, SkRect clip) =>
        bounds.Top >= clip.Bottom || bounds.Bottom <= clip.Top;

    // sort_increasing_Y: copies monotonic-in-Y source into destination increasing in Y; true when reversed.
    private static bool SortIncreasingY(ReadOnlySpan<PathPoint> source, Span<PathPoint> destination)
    {
        int count = source.Length;
        if (source[0].Y > source[count - 1].Y)
        {
            for (int index = 0; index < count; index++)
            {
                destination[index] = source[count - index - 1];
            }

            return true;
        }

        source.CopyTo(destination);
        return false;
    }

    // chopMonoQuadAt: solves F(t) = target for one coordinate's quad.
    private static bool ChopMonoQuadAt(float c0, float c1, float c2, float target, out float t)
    {
        float a = c0 - c1 - c1 + c2;
        float b = 2 * (c1 - c0);
        float c = c0 - target;
        // Only one root is expected, but there is room for two.
        Span<float> roots = stackalloc float[2];
        int count = PathGeometry.FindUnitQuadRoots(a, b, c, roots);
        t = count > 0 ? roots[0] : 0.0f;
        return count > 0;
    }

    // chop_quad_in_Y: clips points in place to the clip's top and bottom.
    private static void ChopQuadInY(Span<PathPoint> points, SkRect clip)
    {
        Span<PathPoint> chopped = stackalloc PathPoint[5];
        // Partially above.
        if (points[0].Y < clip.Top)
        {
            if (ChopMonoQuadAt(points[0].Y, points[1].Y, points[2].Y, clip.Top, out float t))
            {
                // Take the second quad, clamped to clean up imprecise numerics in the chop.
                PathGeometry.ChopQuadAt(points, chopped, t);
                chopped[2] = chopped[2] with { Y = clip.Top };
                chopped[3] = chopped[3] with { Y = ClampGe(chopped[3].Y, clip.Top) };
                points[0] = chopped[2];
                points[1] = chopped[3];
            }
            else
            {
                // Inexact numerics: clamp against the top.
                for (int index = 0; index < 3; index++)
                {
                    if (points[index].Y < clip.Top)
                    {
                        points[index] = points[index] with { Y = clip.Top };
                    }
                }
            }
        }

        // Partially below.
        if (points[2].Y > clip.Bottom)
        {
            if (ChopMonoQuadAt(points[0].Y, points[1].Y, points[2].Y, clip.Bottom, out float t))
            {
                PathGeometry.ChopQuadAt(points, chopped, t);
                chopped[1] = chopped[1] with { Y = ClampLe(chopped[1].Y, clip.Bottom) };
                chopped[2] = chopped[2] with { Y = clip.Bottom };
                points[1] = chopped[1];
                points[2] = chopped[2];
            }
            else
            {
                for (int index = 0; index < 3; index++)
                {
                    if (points[index].Y > clip.Bottom)
                    {
                        points[index] = points[index] with { Y = clip.Bottom };
                    }
                }
            }
        }
    }

    // clamp_le and clamp_ge.
    private static float ClampLe(float value, float max) => value > max ? max : value;

    private static float ClampGe(float value, float min) => value < min ? min : value;

    // SkEdgeClipper::clipMonoQuad: source is monotonic in X and Y.
    private void ClipMonoQuad(ReadOnlySpan<PathPoint> source, SkRect clip)
    {
        Span<PathPoint> points = stackalloc PathPoint[3];
        bool reverse = SortIncreasingY(source, points);

        // Completely above or below.
        if (points[2].Y <= clip.Top || points[0].Y >= clip.Bottom)
        {
            return;
        }

        // Chop so that the points are within the clip in Y.
        ChopQuadInY(points, clip);

        if (points[0].X > points[2].X)
        {
            (points[0], points[2]) = (points[2], points[0]);
            reverse = !reverse;
        }

        // Chop in X as needed, and record the segments.
        if (points[2].X <= clip.Left)
        {
            // Wholly to the left.
            AppendVLine(clip.Left, points[0].Y, points[2].Y, reverse);
            return;
        }

        if (points[0].X >= clip.Right)
        {
            // Wholly to the right.
            if (!CanCullToTheRight)
            {
                AppendVLine(clip.Right, points[0].Y, points[2].Y, reverse);
            }

            return;
        }

        Span<PathPoint> chopped = stackalloc PathPoint[5];
        // Partially to the left.
        if (points[0].X < clip.Left)
        {
            if (ChopMonoQuadAt(points[0].X, points[1].X, points[2].X, clip.Left, out float t))
            {
                PathGeometry.ChopQuadAt(points, chopped, t);
                AppendVLine(clip.Left, chopped[0].Y, chopped[2].Y, reverse);
                // Clamp to clean up imprecise numerics in the chop.
                chopped[2] = chopped[2] with { X = clip.Left };
                chopped[3] = chopped[3] with { X = ClampGe(chopped[3].X, clip.Left) };
                points[0] = chopped[2];
                points[1] = chopped[3];
            }
            else
            {
                // Inexact numerics: clamp against the left.
                AppendVLine(clip.Left, points[0].Y, points[2].Y, reverse);
                return;
            }
        }

        // Partially to the right.
        if (points[2].X > clip.Right)
        {
            if (ChopMonoQuadAt(points[0].X, points[1].X, points[2].X, clip.Right, out float t))
            {
                PathGeometry.ChopQuadAt(points, chopped, t);
                chopped[1] = chopped[1] with { X = ClampLe(chopped[1].X, clip.Right) };
                chopped[2] = chopped[2] with { X = clip.Right };
                AppendQuad(chopped, reverse);
                AppendVLine(clip.Right, chopped[2].Y, chopped[4].Y, reverse);
            }
            else
            {
                // Inexact numerics: clamp against the right.
                points[1] = points[1] with { X = Math.Min(points[1].X, clip.Right) };
                points[2] = points[2] with { X = Math.Min(points[2].X, clip.Right) };
                AppendQuad(points, reverse);
            }
        }
        else
        {
            // Wholly inside the clip.
            AppendQuad(points, reverse);
        }
    }

    // mono_cubic_closestT: a binary search for the t whose coordinate is nearest value.
    private static float MonoCubicClosestT(float c0, float c1, float c2, float c3, float value)
    {
        float t = 0.5f;
        float lastT;
        float bestT = 0.0f;
        float step = 0.25f;
        float d = c0;
        float a = c3 + (3 * (c1 - c2)) - d;
        float b = 3 * (c2 - c1 - c1 + d);
        float c = 3 * (c1 - d);
        value -= d;
        float closest = float.MaxValue;
        do
        {
            float location = ((((a * t) + b) * t) + c) * t;
            float distance = Math.Abs(location - value);
            if (closest > distance)
            {
                closest = distance;
                bestT = t;
            }

            lastT = t;
            t += location < value ? step : -step;
            step *= 0.5f;
        }
        while (closest > 0.25f && lastT != t);

        return bestT;
    }

    // chop_mono_cubic_at_y / chop_mono_cubic_at_x.
    private static void ChopMonoCubicAt(ReadOnlySpan<PathPoint> source, float value, bool yDirection,
        Span<PathPoint> destination)
    {
        if (PathCubicRoots.ChopMonoCubicAt(source, value, yDirection, destination))
        {
            return;
        }

        float t = yDirection
            ? MonoCubicClosestT(source[0].Y, source[1].Y, source[2].Y, source[3].Y, value)
            : MonoCubicClosestT(source[0].X, source[1].X, source[2].X, source[3].X, value);
        PathGeometry.ChopCubicAt(source, destination, t);
    }

    // chop_cubic_in_Y: clips points in place to the clip's top and bottom.
    private static void ChopCubicInY(Span<PathPoint> points, SkRect clip)
    {
        Span<PathPoint> chopped = stackalloc PathPoint[7];
        // Partially above.
        if (points[0].Y < clip.Top)
        {
            ChopMonoCubicAt(points, clip.Top, true, chopped);

            // A large range in the points can chop poorly, leaving the lower cubic partly above the
            // clip. The first one or two Y values can be smashed down, but not three: re-chop the lower
            // cubic as a guess, then fall through to smashing.
            if (chopped[3].Y < clip.Top && chopped[4].Y < clip.Top && chopped[5].Y < clip.Top)
            {
                Span<PathPoint> guess = stackalloc PathPoint[4];
                chopped.Slice(3, 4).CopyTo(guess);
                ChopMonoCubicAt(guess, clip.Top, true, chopped);
            }

            // The chopper's numerics are not trusted: force chopped[3, 4] below the top.
            chopped[3] = chopped[3] with { Y = clip.Top };
            chopped[4] = chopped[4] with { Y = ClampGe(chopped[4].Y, clip.Top) };
            points[0] = chopped[3];
            points[1] = chopped[4];
            points[2] = chopped[5];
        }

        // Partially below.
        if (points[3].Y > clip.Bottom)
        {
            ChopMonoCubicAt(points, clip.Bottom, true, chopped);
            chopped[3] = chopped[3] with { Y = clip.Bottom };
            chopped[2] = chopped[2] with { Y = ClampLe(chopped[2].Y, clip.Bottom) };
            points[1] = chopped[1];
            points[2] = chopped[2];
            points[3] = chopped[3];
        }
    }

    // SkEdgeClipper::clipMonoCubic: source is monotonic in X and Y.
    private void ClipMonoCubic(ReadOnlySpan<PathPoint> source, SkRect clip)
    {
        Span<PathPoint> points = stackalloc PathPoint[4];
        bool reverse = SortIncreasingY(source, points);

        // Completely above or below.
        if (points[3].Y <= clip.Top || points[0].Y >= clip.Bottom)
        {
            return;
        }

        // Chop so that the points are within the clip in Y.
        ChopCubicInY(points, clip);

        if (points[0].X > points[3].X)
        {
            (points[0], points[3]) = (points[3], points[0]);
            (points[1], points[2]) = (points[2], points[1]);
            reverse = !reverse;
        }

        // Chop in X as needed, and record the segments.
        if (points[3].X <= clip.Left)
        {
            // Wholly to the left.
            AppendVLine(clip.Left, points[0].Y, points[3].Y, reverse);
            return;
        }

        if (points[0].X >= clip.Right)
        {
            // Wholly to the right.
            if (!CanCullToTheRight)
            {
                AppendVLine(clip.Right, points[0].Y, points[3].Y, reverse);
            }

            return;
        }

        Span<PathPoint> chopped = stackalloc PathPoint[7];
        // Partially to the left.
        if (points[0].X < clip.Left)
        {
            ChopMonoCubicAt(points, clip.Left, false, chopped);
            AppendVLine(clip.Left, chopped[0].Y, chopped[3].Y, reverse);

            // The chopper's numerics are not trusted: force chopped[3, 4] right of the left edge.
            chopped[3] = chopped[3] with { X = clip.Left };
            chopped[4] = chopped[4] with { X = ClampGe(chopped[4].X, clip.Left) };
            points[0] = chopped[3];
            points[1] = chopped[4];
            points[2] = chopped[5];
        }

        // Partially to the right.
        if (points[3].X > clip.Right)
        {
            ChopMonoCubicAt(points, clip.Right, false, chopped);
            chopped[3] = chopped[3] with { X = clip.Right };
            chopped[2] = chopped[2] with { X = ClampLe(chopped[2].X, clip.Right) };
            AppendCubic(chopped, reverse);
            AppendVLine(clip.Right, chopped[3].Y, chopped[6].Y, reverse);
        }
        else
        {
            // Wholly inside the clip.
            AppendCubic(points, reverse);
        }
    }

    // too_big_for_reliable_float_math: the largest values (found by experiment) for which chopping at
    // extrema and at clip edges is still reliable.
    private static bool TooBigForReliableFloatMath(SkRect rect)
    {
        const float limit = 1 << 22;
        return rect.Left < -limit || rect.Top < -limit || rect.Right > limit || rect.Bottom > limit;
    }

    // SkEdgeClipper::clipCubic.
    private bool ClipCubic(ReadOnlySpan<PathPoint> source, SkRect clip)
    {
        Begin();
        SkRect bounds = PathPerspective.BoundsCheck(source[..4]);
        // Clipped out vertically?
        if (bounds.Bottom > clip.Top && bounds.Top < clip.Bottom)
        {
            if (TooBigForReliableFloatMath(bounds))
            {
                // The cubic cannot be clipped safely: draw a line, which can be.
                return ClipLine(source[0], source[3], clip);
            }

            Span<PathPoint> monoY = stackalloc PathPoint[10];
            Span<PathPoint> monoX = stackalloc PathPoint[10];
            int countY = PathGeometry.ChopCubicAtYExtrema(source, monoY);
            for (int y = 0; y <= countY; y++)
            {
                int countX = PathGeometry.ChopCubicAtXExtrema(monoY.Slice(y * 3, 4), monoX);
                for (int x = 0; x <= countX; x++)
                {
                    ClipMonoCubic(monoX.Slice(x * 3, 4), clip);
                }
            }
        }

        return End();
    }

    private void AppendLine(PathPoint p0, PathPoint p1)
    {
        _verbs[_currentVerb++] = PathVerb.Line;
        _points[_currentPoint] = p0;
        _points[_currentPoint + 1] = p1;
        _currentPoint += 2;
    }

    private void AppendVLine(float x, float y0, float y1, bool reverse)
    {
        _verbs[_currentVerb++] = PathVerb.Line;
        if (reverse)
        {
            (y0, y1) = (y1, y0);
        }

        _points[_currentPoint] = new PathPoint(x, y0);
        _points[_currentPoint + 1] = new PathPoint(x, y1);
        _currentPoint += 2;
    }

    private void AppendQuad(ReadOnlySpan<PathPoint> points, bool reverse)
    {
        _verbs[_currentVerb++] = PathVerb.Quad;
        _points[_currentPoint] = reverse ? points[2] : points[0];
        _points[_currentPoint + 1] = points[1];
        _points[_currentPoint + 2] = reverse ? points[0] : points[2];
        _currentPoint += 3;
    }

    private void AppendCubic(ReadOnlySpan<PathPoint> points, bool reverse)
    {
        _verbs[_currentVerb++] = PathVerb.Cubic;
        for (int index = 0; index < 4; index++)
        {
            _points[_currentPoint + index] = reverse ? points[3 - index] : points[index];
        }

        _currentPoint += 4;
    }
}

/// <summary>
/// Skia's <c>SkPathEdgeIter</c>: a path's edges (lines, quads, conics, cubics) with the point they start
/// from, every contour that has an edge closed by a line, and whether an edge starts a new contour.
/// </summary>
internal struct PathEdgeIterator(
    IReadOnlyList<PathVerb> verbs,
    IReadOnlyList<PathPoint> points,
    IReadOnlyList<float> weights)
{
    private int _verb;
    private int _point;
    private int _weight = -1;
    private int _moveTo;
    private bool _needsCloseLine;
    private bool _nextIsNewContour;

    internal readonly float ConicWeight => weights[_weight];

    internal PathVerb? Next(Span<PathPoint> edge, out bool isNewContour)
    {
        while (true)
        {
            if (_verb == verbs.Count)
            {
                isNewContour = false;
                return _needsCloseLine ? CloseLine(edge) : null;
            }

            PathVerb verb = verbs[_verb++];
            switch (verb)
            {
                case PathVerb.Move:
                    if (_needsCloseLine)
                    {
                        CloseLine(edge);
                        _moveTo = _point++;
                        isNewContour = false;
                        return PathVerb.Line;
                    }

                    _moveTo = _point++;
                    _nextIsNewContour = true;
                    break;
                case PathVerb.Close:
                    if (_needsCloseLine)
                    {
                        isNewContour = false;
                        return CloseLine(edge);
                    }

                    break;
                default:
                    // An actual edge.
                    int count = verb switch
                    {
                        PathVerb.Line => 1,
                        PathVerb.Cubic => 3,
                        _ => 2,
                    };
                    if (verb == PathVerb.Conic)
                    {
                        _weight++;
                    }

                    _needsCloseLine = true;
                    _point += count;
                    for (int index = 0; index <= count; index++)
                    {
                        edge[index] = points[_point - count - 1 + index];
                    }

                    isNewContour = _nextIsNewContour;
                    _nextIsNewContour = false;
                    return verb;
            }
        }
    }

    // The closing line from the last point back to the contour's move.
    private PathVerb CloseLine(Span<PathPoint> edge)
    {
        edge[0] = points[_point - 1];
        edge[1] = points[_moveTo];
        _needsCloseLine = false;
        _nextIsNewContour = true;
        return PathVerb.Line;
    }
}

/// <summary>Skia's <c>SkLineClipper::ClipLine</c>: a line clipped to a rectangle, as 0 to 3 lines.</summary>
internal static class PathLineClipper
{
    internal const int MaxPoints = 4;

    // pin_unsorted.
    private static double PinUnsorted(double value, double limit0, double limit1)
    {
        if (limit1 < limit0)
        {
            (limit0, limit1) = (limit1, limit0);
        }

        if (value < limit0)
        {
            value = limit0;
        }
        else if (value > limit1)
        {
            value = limit1;
        }

        return value;
    }

    private static float PinUnsorted(float value, float limit0, float limit1)
    {
        if (limit1 < limit0)
        {
            (limit0, limit1) = (limit1, limit0);
        }

        if (value < limit0)
        {
            value = limit0;
        }
        else if (value > limit1)
        {
            value = limit1;
        }

        return value;
    }

    // sect_with_horizontal: the X of the line at Y, in double so it stays within the original limits.
    private static float SectWithHorizontal(PathPoint p0, PathPoint p1, float y)
    {
        float dy = p1.Y - p0.Y;
        if (PathMath.IsNearlyZero(dy))
        {
            return (p0.X + p1.X) * 0.5f;
        }

        double x0 = p0.X;
        double y0 = p0.Y;
        double x1 = p1.X;
        double y1 = p1.Y;
        double result = x0 + ((((double)y - y0) * (x1 - x0)) / (y1 - y0));
        // The result may still exceed [x0, x1] from the doubles' rounding.
        return (float)PinUnsorted(result, x0, x1);
    }

    // sect_with_vertical: the Y of the line at X.
    private static float SectWithVertical(PathPoint p0, PathPoint p1, float x)
    {
        float dx = p1.X - p0.X;
        if (PathMath.IsNearlyZero(dx))
        {
            return (p0.Y + p1.Y) * 0.5f;
        }

        double x0 = p0.X;
        double y0 = p0.Y;
        double x1 = p1.X;
        double y1 = p1.Y;
        double result = y0 + ((((double)x - x0) * (y1 - y0)) / (x1 - x0));
        return (float)result;
    }

    // sect_clamp_with_vertical: clamped to the line's Y range, which rounding can leave (skbug.com/7491).
    private static float SectClampWithVertical(PathPoint p0, PathPoint p1, float x) =>
        PinUnsorted(SectWithVertical(p0, p1, x), p0.Y, p1.Y);

    // SkLineClipper::ClipLine: lines holds lineCount + 1 points.
    internal static int ClipLine(PathPoint p0, PathPoint p1, SkRect clip, Span<PathPoint> lines,
        bool canCullToTheRight)
    {
        Span<PathPoint> pts = [p0, p1];
        int index0;
        int index1;
        if (pts[0].Y < pts[1].Y)
        {
            index0 = 0;
            index1 = 1;
        }
        else
        {
            index0 = 1;
            index1 = 0;
        }

        // Completely clipped out in Y (above or below)?
        if (pts[index1].Y <= clip.Top || pts[index0].Y >= clip.Bottom)
        {
            return 0;
        }

        // Chop in Y to one segment.
        Span<PathPoint> tmp = [p0, p1];
        if (pts[index0].Y < clip.Top)
        {
            tmp[index0] = new PathPoint(SectWithHorizontal(p0, p1, clip.Top), clip.Top);
        }

        if (tmp[index1].Y > clip.Bottom)
        {
            tmp[index1] = new PathPoint(SectWithHorizontal(p0, p1, clip.Bottom), clip.Bottom);
        }

        // Chop into 1..3 segments wholly within the clip in X.
        Span<PathPoint> resultStorage = stackalloc PathPoint[MaxPoints];
        scoped Span<PathPoint> result;
        int lineCount = 1;
        bool reverse;
        if (pts[0].X < pts[1].X)
        {
            index0 = 0;
            index1 = 1;
            reverse = false;
        }
        else
        {
            index0 = 1;
            index1 = 0;
            reverse = true;
        }

        if (tmp[index1].X <= clip.Left)
        {
            // Wholly to the left.
            tmp[0] = tmp[0] with { X = clip.Left };
            tmp[1] = tmp[1] with { X = clip.Left };
            result = tmp;
            reverse = false;
        }
        else if (tmp[index0].X >= clip.Right)
        {
            // Wholly to the right.
            if (canCullToTheRight)
            {
                return 0;
            }

            tmp[0] = tmp[0] with { X = clip.Right };
            tmp[1] = tmp[1] with { X = clip.Right };
            result = tmp;
            reverse = false;
        }
        else
        {
            result = resultStorage;
            int r = 0;
            if (tmp[index0].X < clip.Left)
            {
                result[r++] = new PathPoint(clip.Left, tmp[index0].Y);
                result[r] = new PathPoint(clip.Left, SectClampWithVertical(tmp[0], tmp[1], clip.Left));
            }
            else
            {
                result[r] = tmp[index0];
            }

            r += 1;
            if (tmp[index1].X > clip.Right)
            {
                result[r++] = new PathPoint(clip.Right, SectClampWithVertical(tmp[0], tmp[1], clip.Right));
                result[r] = new PathPoint(clip.Right, tmp[index1].Y);
            }
            else
            {
                result[r] = tmp[index1];
            }

            lineCount = r;
        }

        // Copy the points out, reversed to keep the winding.
        if (reverse)
        {
            for (int index = 0; index <= lineCount; index++)
            {
                lines[lineCount - index] = result[index];
            }
        }
        else
        {
            result[..(lineCount + 1)].CopyTo(lines);
        }

        return lineCount;
    }
}

/// <summary>
/// Skia's <c>SkChopMonoCubicAtX/Y</c>: chops a monotonic cubic where it crosses an axis value, solving
/// the cubic in double (<c>SkCubics</c>, <c>SkQuads</c>, <c>SkBezierCubic</c>).
/// </summary>
internal static class PathCubicRoots
{
    // SkChopMonoCubicAtY (yDirection) / SkChopMonoCubicAtX.
    internal static bool ChopMonoCubicAt(ReadOnlySpan<PathPoint> source, float value, bool yDirection,
        Span<PathPoint> destination)
    {
        Span<double> coefficients =
        [
            source[0].X, source[0].Y, source[1].X, source[1].Y,
            source[2].X, source[2].Y, source[3].X, source[3].Y,
        ];
        if (!FirstAxisIntersection(coefficients, yDirection, value, out double solution))
        {
            return false;
        }

        Span<double> cubicPair = stackalloc double[14];
        Subdivide(coefficients, solution, cubicPair);
        for (int index = 0; index < 7; index++)
        {
            destination[index] = new PathPoint((float)cubicPair[index * 2], (float)cubicPair[(index * 2) + 1]);
        }

        return true;
    }

    private static bool CloseEnoughToZero(double x) => Math.Abs(x) < 0.00001;

    // first_axis_intersection.
    private static bool FirstAxisIntersection(ReadOnlySpan<double> coefficients, bool yDirection,
        double axisIntercept, out double solution)
    {
        solution = 0;
        (double a, double b, double c, double d) = ConvertToPolynomial(coefficients, yDirection);
        d -= axisIntercept;
        Span<double> roots = [0, 0, 0];
        int count = RootsValidT(a, b, c, d, roots);
        if (count == 0)
        {
            return false;
        }

        // At least one of the roots must be accurate.
        for (int index = 0; index < count; index++)
        {
            if (CloseEnoughToZero(EvalAt(a, b, c, d, roots[index])))
            {
                solution = roots[index];
                return true;
            }
        }

        // The normal solver was not accurate enough: fall back to a more accurate one.
        count = BinarySearchRootsValidT(a, b, c, d, roots);
        if (count == 0)
        {
            return false;
        }

        for (int index = 0; index < count; index++)
        {
            if (CloseEnoughToZero(EvalAt(a, b, c, d, roots[index])))
            {
                solution = roots[index];
                return true;
            }
        }

        return false;
    }

    // SkBezierCubic::ConvertToPolynomial.
    private static (double A, double B, double C, double D) ConvertToPolynomial(ReadOnlySpan<double> curve,
        bool yValues)
    {
        int offset = yValues ? 1 : 0;
        double p0 = curve[offset];
        double p1 = curve[offset + 2];
        double p2 = curve[offset + 4];
        double p3 = curve[offset + 6];
        return (-p0 + (3 * p1) - (3 * p2) + p3, (3 * p0) - (6 * p1) + (3 * p2), (-3 * p0) + (3 * p1), p0);
    }

    // SkBezierCubic::Subdivide: De Casteljau's algorithm.
    private static void Subdivide(ReadOnlySpan<double> curve, double t, Span<double> twoCurves)
    {
        twoCurves[0] = curve[0];
        twoCurves[1] = curve[1];
        twoCurves[12] = curve[6];
        twoCurves[13] = curve[7];
        double x01 = Interpolate(curve[0], curve[2], t);
        double y01 = Interpolate(curve[1], curve[3], t);
        double x12 = Interpolate(curve[2], curve[4], t);
        double y12 = Interpolate(curve[3], curve[5], t);
        double x23 = Interpolate(curve[4], curve[6], t);
        double y23 = Interpolate(curve[5], curve[7], t);
        twoCurves[2] = x01;
        twoCurves[3] = y01;
        twoCurves[10] = x23;
        twoCurves[11] = y23;
        twoCurves[4] = Interpolate(x01, x12, t);
        twoCurves[5] = Interpolate(y01, y12, t);
        twoCurves[8] = Interpolate(x12, x23, t);
        twoCurves[9] = Interpolate(y12, y23, t);
        twoCurves[6] = Interpolate(twoCurves[4], twoCurves[8], t);
        twoCurves[7] = Interpolate(twoCurves[5], twoCurves[9], t);
    }

    private static double Interpolate(double a, double b, double t) => a + ((b - a) * t);

    // SkCubics::EvalAt.
    private static double EvalAt(double a, double b, double c, double d, double t) =>
        Math.FusedMultiplyAdd(t, Math.FusedMultiplyAdd(t, Math.FusedMultiplyAdd(t, a, b), c), d);

    // sk_double_nearly_zero.
    private static bool NearlyZero(double a) => a == 0 || Math.Abs(a) < FloatEpsilon;

    // std::numeric_limits<float>::epsilon().
    private const double FloatEpsilon = 1.1920928955078125e-07;

    // sk_doubles_nearly_equal_ulps with the default 16 ulps.
    private static bool NearlyEqualUlps(double a, double b)
    {
        const int maxUlpsDiff = 16;
        double maxMagnitude = Math.Max(Math.Max(Magnitude(a), MinNormal), Magnitude(b));
        double tolerance = maxMagnitude * (DoubleEpsilon * (maxUlpsDiff + 1));
        return a == b || Math.Abs(b - a) < tolerance;
    }

    // std::numeric_limits<double>::min() and epsilon().
    private const double MinNormal = 2.2250738585072014e-308;
    private const double DoubleEpsilon = 2.220446049250313e-16;

    // magnitude: 2^e of a normalized double, 0 for subnormals, infinity for NaN and infinity.
    private static double Magnitude(double a)
    {
        long bits = BitConverter.DoubleToInt64Bits(a) & 0x7FF0000000000000L;
        return BitConverter.Int64BitsToDouble(bits);
    }

    // SkCubics.cpp's nearly_equal.
    private static bool NearlyEqual(double x, double y) => NearlyZero(x) ? NearlyZero(y) : NearlyEqualUlps(x, y);

    // close_to_a_quadratic: a tiny A makes a huge, imprecise root; be precise about the other two.
    private static bool CloseToAQuadratic(double a, double b) =>
        NearlyZero(b) ? NearlyZero(a) : Math.Abs(a / b) < 1.0e-7;

    // SkCubics::RootsReal.
    private static int RootsReal(double a, double b, double c, double d, Span<double> solution)
    {
        if (CloseToAQuadratic(a, b))
        {
            return QuadRootsReal(b, c, d, solution);
        }

        if (NearlyZero(d))
        {
            // 0 is one root.
            int num = QuadRootsReal(a, b, c, solution);
            for (int index = 0; index < num; ++index)
            {
                if (NearlyZero(solution[index]))
                {
                    return num;
                }
            }

            solution[num++] = 0;
            return num;
        }

        if (NearlyZero(a + b + c + d))
        {
            // 1 is one root.
            int num = QuadRootsReal(a, a + b, -d, solution);
            for (int index = 0; index < num; ++index)
            {
                if (NearlyEqualUlps(solution[index], 1))
                {
                    return num;
                }
            }

            solution[num++] = 1;
            return num;
        }

        // A zero A (a NaN B fails close_to_a_quadratic) makes infinities here, caught by R2MinusQ3.
        double invA = 1 / a;
        double na = b * invA;
        double nb = c * invA;
        double nc = d * invA;
        double a2 = na * na;
        double q = (a2 - (nb * 3)) / 9;
        double r = ((2 * a2 * na) - (9 * na * nb) + (27 * nc)) / 54;
        double r2 = r * r;
        double q3 = q * q * q;
        double r2MinusQ3 = r2 - q3;
        // An infinite or NaN R2 or Q3 leaves no finite roots.
        if (!double.IsFinite(r2MinusQ3))
        {
            return 0;
        }

        double adiv3 = na / 3;
        double root;
        int count = 0;
        if (r2MinusQ3 < 0)
        {
            // Three real roots; the quotient can fall slightly outside [-1, 1].
            double theta = Math.Acos(Math.Max(-1.0, Math.Min(r / Math.Sqrt(q3), 1.0)));
            double neg2RootQ = -2 * Math.Sqrt(q);
            root = (neg2RootQ * Math.Cos(theta / 3)) - adiv3;
            solution[count++] = root;
            root = (neg2RootQ * Math.Cos((theta + (2 * Math.PI)) / 3)) - adiv3;
            if (!NearlyEqual(solution[0], root))
            {
                solution[count++] = root;
            }

            root = (neg2RootQ * Math.Cos((theta - (2 * Math.PI)) / 3)) - adiv3;
            if (!NearlyEqual(solution[0], root) && (count == 1 || !NearlyEqual(solution[1], root)))
            {
                solution[count++] = root;
            }
        }
        else
        {
            // One real root.
            double sqrtR2MinusQ3 = Math.Sqrt(r2MinusQ3);
            double big = Math.Abs(r) + sqrtR2MinusQ3;
            big = Math.Cbrt(big);
            if (r > 0)
            {
                big = -big;
            }

            if (!NearlyZero(big))
            {
                big += q / big;
            }

            root = big - adiv3;
            solution[count++] = root;
            if (!NearlyZero(r2) && NearlyEqualUlps(r2, q3))
            {
                root = (-big / 2) - adiv3;
                if (!NearlyEqual(solution[0], root))
                {
                    solution[count++] = root;
                }
            }
        }

        return count;
    }

    // SkCubics::RootsValidT.
    private static int RootsValidT(double a, double b, double c, double d, Span<double> solution)
    {
        Span<double> allRoots = [0, 0, 0];
        int realRoots = RootsReal(a, b, c, d, allRoots);
        int foundRoots = 0;
        for (int index = 0; index < realRoots; ++index)
        {
            double t = allRoots[index];
            if (t >= 1.0 && t <= 1.00005)
            {
                // Without 1 (or something very close) already in the list.
                if ((foundRoots < 1 || !NearlyEqualUlps(solution[0], 1))
                    && (foundRoots < 2 || !NearlyEqualUlps(solution[1], 1)))
                {
                    solution[foundRoots++] = 1;
                }
            }
            else if (t >= -0.00005 && (t <= 0.0 || NearlyZero(t)))
            {
                // Without 0 (or something very close) already in the list.
                if ((foundRoots < 1 || !NearlyZero(solution[0]))
                    && (foundRoots < 2 || !NearlyZero(solution[1])))
                {
                    solution[foundRoots++] = 0;
                }
            }
            else if (t > 0.0 && t < 1.0)
            {
                solution[foundRoots++] = t;
            }
        }

        return foundRoots;
    }

    // The binary search's cutoff, balancing performance and accuracy.
    private static bool ApproximatelyZero(double x) => Math.Abs(x) < 0.00000001;

    // find_extrema_valid_t: where the derivative 3At^2 + 2Bt + C is zero, in [0, 1].
    private static int FindExtremaValidT(double a, double b, double c, Span<double> t)
    {
        Span<double> roots = [0, 0];
        int numRoots = QuadRootsReal(3 * a, 2 * b, c, roots);
        int validRoots = 0;
        for (int index = 0; index < numRoots; index++)
        {
            double value = roots[index];
            if (value >= 0 && value <= 1.0)
            {
                t[validRoots++] = value;
            }
        }

        return validRoots;
    }

    // binary_search: -1 when there is no root between start and stop.
    private static double BinarySearch(double a, double b, double c, double d, double start, double stop)
    {
        double left = EvalAt(a, b, c, d, start);
        if (ApproximatelyZero(left))
        {
            return start;
        }

        double right = EvalAt(a, b, c, d, stop);
        if (!double.IsFinite(left) || !double.IsFinite(right))
        {
            return -1;
        }

        if ((left > 0 && right > 0) || (left < 0 && right < 0))
        {
            return -1;
        }

        // Bounded, against an infinite loop.
        const int maxIterations = 1000;
        for (int index = 0; index < maxIterations; index++)
        {
            double step = (start + stop) / 2;
            double current = EvalAt(a, b, c, d, step);
            if (ApproximatelyZero(current))
            {
                return step;
            }

            if ((current < 0 && left < 0) || (current > 0 && left > 0))
            {
                start = step;
            }
            else
            {
                stop = step;
            }
        }

        return -1;
    }

    // SkCubics::BinarySearchRootsValidT.
    private static int BinarySearchRootsValidT(double a, double b, double c, double d, Span<double> solution)
    {
        if (!double.IsFinite(a) || !double.IsFinite(b) || !double.IsFinite(c) || !double.IsFinite(d))
        {
            return 0;
        }

        Span<double> regions = [0, 0, 0, 1];
        // The local minima and maxima.
        Span<double> minMax = [0, 0];
        int extremaCount = FindExtremaValidT(a, b, c, minMax);
        int startIndex = 2 - extremaCount;
        if (extremaCount == 1)
        {
            regions[startIndex + 1] = minMax[0];
        }

        if (extremaCount == 2)
        {
            // In [0, 1], but maybe unsorted.
            regions[startIndex + 1] = Math.Min(minMax[0], minMax[1]);
            regions[startIndex + 2] = Math.Max(minMax[0], minMax[1]);
        }

        // regions[startIndex..3] ascend in [0, 1]; roots lie between them.
        int foundRoots = 0;
        for (; startIndex < 3; startIndex++)
        {
            double root = BinarySearch(a, b, c, d, regions[startIndex], regions[startIndex + 1]);
            if (root >= 0)
            {
                // Skip duplicates.
                if ((foundRoots < 1 || !ApproximatelyZero(solution[0] - root))
                    && (foundRoots < 2 || !ApproximatelyZero(solution[1] - root)))
                {
                    solution[foundRoots++] = root;
                }
            }
        }

        return foundRoots;
    }

    // SkQuads::RootsReal.
    private static int QuadRootsReal(double a, double b, double c, Span<double> solution)
    {
        if (CloseToLinear(a, b))
        {
            return SolveLinear(b, c, solution);
        }

        (double discriminant, double root0, double root1) = QuadRoots(a, -0.5 * b, c);
        if (!double.IsFinite(discriminant) || discriminant < 0)
        {
            return 0;
        }

        int roots = 0;
        double r0 = ZeroIfTiny(root0);
        if (double.IsFinite(r0))
        {
            solution[roots++] = r0;
        }

        double r1 = ZeroIfTiny(root1);
        if (double.IsFinite(r1))
        {
            solution[roots++] = r1;
        }

        if (roots == 2 && NearlyEqualUlps(solution[0], solution[1]))
        {
            roots = 1;
        }

        return roots;
    }

    private static double ZeroIfTiny(double x) => NearlyZero(x) ? 0 : x;

    // solve_linear: 0 = M x + B; a zero M has no solution unless B is zero too (then return one).
    private static int SolveLinear(double m, double b, Span<double> solution)
    {
        if (NearlyZero(m))
        {
            solution[0] = 0;
            return NearlyZero(b) ? 1 : 0;
        }

        solution[0] = -b / m;
        return double.IsFinite(solution[0]) ? 1 : 0;
    }

    // close_to_linear: B much bigger than A, or A zero.
    private static bool CloseToLinear(double a, double b) => a == 0 || Math.Abs(b / a) >= 1.0e+16;

    // SkQuads::Discriminant: B^2 - AC with at most 2 bits of rounding error, using FMA for the error terms.
    private static double Discriminant(double a, double b, double c)
    {
        double b2 = b * b;
        double ac = a * c;
        double roughDiscriminant = b2 - ac;
        if (3 * Math.Abs(roughDiscriminant) >= b2 + ac)
        {
            return roughDiscriminant;
        }

        double b2RoundingError = Math.FusedMultiplyAdd(b, b, -b2);
        double acRoundingError = Math.FusedMultiplyAdd(a, c, -ac);
        return (b2 - ac) + (b2RoundingError - acRoundingError);
    }

    // SkQuads::Roots: A x^2 - 2 B x + C = 0.
    private static (double Discriminant, double Root0, double Root1) QuadRoots(double a, double b, double c)
    {
        double discriminant = Discriminant(a, b, c);
        if (a == 0)
        {
            double root = b == 0 ? (c == 0 ? double.PositiveInfinity : double.NaN) : c / (2 * b);
            return (discriminant, root, root);
        }

        if (discriminant == 0)
        {
            return (discriminant, b / a, b / a);
        }

        if (discriminant > 0)
        {
            double d = Math.Sqrt(discriminant);
            double r = b > 0 ? b + d : b - d;
            return (discriminant, r / a, c / r);
        }

        // Negative or not finite.
        return (discriminant, double.NaN, double.NaN);
    }
}
