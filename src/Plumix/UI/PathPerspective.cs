namespace Plumix.UI;

// Engine adapter: skia/src/core/SkPath.cpp at chrome/m119 — SkPath::transform's perspective branch with
// SkApplyPerspectiveClip::kYes (SkPathPriv::PerspectiveClip, SkHalfPlane, clip, subdivide_cubic_to) and
// SkConic::TransformW (src/core/SkGeometry.cpp). Points behind the camera's w = kW0PlaneDistance plane are
// clipped away (PathEdgeClipper), quads and conics become conics with the projected weight, cubics are
// subdivided, and every point is projected, with Skia's float operations in Skia's order
// (PathSkiaParityTests).

/// <summary>Skia's <c>SkPath::transform</c> for a matrix with perspective.</summary>
internal static class PathPerspective
{
    // SkPathPriv::kW0PlaneDistance: the distance in front of w = 0 that the path is clipped at.
    private const float W0PlaneDistance = 1.0f / (1 << 14);

    // SkPath::transform(matrix, dst, SkApplyPerspectiveClip::kYes) when matrix has perspective. The result
    // keeps the fill type and drops any oval/rrect shape.
    internal static Path Transform(Path path, PathMatrix matrix)
    {
        Path source = PerspectiveClip(path, matrix) ?? path;
        var segments = new List<(PathVerb Verb, PathPoint[] Points, float Weight)>();
        var iterator = new ClosingPathIterator(source, forceClose: false);
        Span<PathPoint> points = stackalloc PathPoint[4];
        while (iterator.Next(points) is { } verb)
        {
            switch (verb)
            {
                case PathVerb.Move:
                    segments.Add((verb, [matrix.Map(points[0])], 0.0f));
                    break;
                case PathVerb.Line:
                    segments.Add((verb, [matrix.Map(points[1])], 0.0f));
                    break;
                case PathVerb.Quad:
                case PathVerb.Conic:
                    // A quad is promoted to a conic.
                    float weight = verb == PathVerb.Quad ? 1.0f : iterator.ConicWeight;
                    segments.Add((PathVerb.Conic, [matrix.Map(points[1]), matrix.Map(points[2])],
                        TransformW(points, weight, matrix)));
                    break;
                case PathVerb.Cubic:
                    SubdivideCubicTo(segments, points, matrix, 2);
                    break;
                default:
                    segments.Add((PathVerb.Close, [], 0.0f));
                    break;
            }
        }

        // Skia builds the path through SkPath's builders, then projects every point; projecting each point
        // as it is added is the same, since the builders only copy points.
        return Path.FromSegments(path.FillType, segments);
    }

    // subdivide_cubic_to: a cubic as 2^level cubics, halving each.
    private static void SubdivideCubicTo(
        List<(PathVerb Verb, PathPoint[] Points, float Weight)> segments,
        ReadOnlySpan<PathPoint> points,
        PathMatrix matrix,
        int level)
    {
        if (--level >= 0)
        {
            Span<PathPoint> halves = stackalloc PathPoint[7];
            PathGeometry.ChopCubicAt(points, halves, 0.5f);
            SubdivideCubicTo(segments, halves[..4], matrix, level);
            SubdivideCubicTo(segments, halves[3..], matrix, level);
        }
        else
        {
            segments.Add((PathVerb.Cubic, [matrix.Map(points[1]), matrix.Map(points[2]), matrix.Map(points[3])],
                0.0f));
        }
    }

    // SkConic::TransformW: the weight of the conic once projected, w' = sqrt(w1 * w1 / (w0 * w2)) over the
    // projected homogeneous points' z.
    private static float TransformW(ReadOnlySpan<PathPoint> points, float weight, PathMatrix matrix)
    {
        // ratquad_mapTo3D, then SkMatrix::mapHomogeneousPoints's z (sdot).
        float z0 = (points[0].X * 1 * matrix[6]) + (points[0].Y * 1 * matrix[7]) + (1 * matrix[8]);
        float z1 = (points[1].X * weight * matrix[6]) + (points[1].Y * weight * matrix[7]) + (weight * matrix[8]);
        float z2 = (points[2].X * 1 * matrix[6]) + (points[2].Y * 1 * matrix[7]) + (1 * matrix[8]);
        // Doubles, to handle small numerators and denominators.
        double w0 = z0;
        double w1 = z1;
        double w2 = z2;
        return (float)Math.Sqrt((w1 * w1) / (w0 * w2));
    }

    // SkPathPriv::PerspectiveClip: null when nothing is clipped (the path is wholly in front of the
    // plane), otherwise the clipped path, empty when clipped out or when the calculations fail.
    private static Path? PerspectiveClip(Path path, PathMatrix matrix)
    {
        var plane = new HalfPlane(matrix[6], matrix[7], matrix[8] - W0PlaneDistance);
        if (plane.Normalize())
        {
            switch (plane.Test(BoundsCheck([.. path.Points])))
            {
                case HalfPlaneResult.AllPositive:
                    return null;
                case HalfPlaneResult.Mixed:
                    return Clip(path, plane);
            }
        }

        // Clipped out (or failed).
        return new Path();
    }

    // SkPath.cpp's clip: rotates the path so the (normalized) plane is y = 0, clips it to y >= 0 with
    // SkEdgeClipper, rebuilds the path from the clipped edges and rotates it back. Empty on failure.
    private static Path Clip(Path path, HalfPlane plane)
    {
        var p0 = new PathPoint(-plane.A * plane.C, -plane.B * plane.C);
        PathMatrix mx = PathMatrix.FromValues(
            plane.B, plane.A, p0.X,
            -plane.A, plane.B, p0.Y,
            0.0f, 0.0f, 1.0f);
        if (!mx.TryInvert(out PathMatrix inverse))
        {
            return new Path();
        }

        var rotated = new PathPoint[path.Points.Count];
        for (int index = 0; index < rotated.Length; index++)
        {
            rotated[index] = inverse.Map(path.Points[index]);
        }

        if (!PathGeometry.AreFinite(rotated))
        {
            return new Path();
        }

        const float big = float.MaxValue;
        var clip = new SkRect(-big, 0, big, big);
        var builder = new ClipPathBuilder();
        PathEdgeClipper.ClipPath(path.Verbs, rotated, path.ConicWeights, clip, false, builder.Consume);

        var segments = new List<(PathVerb Verb, PathPoint[] Points, float Weight)>(builder.Segments.Count);
        foreach ((PathVerb verb, PathPoint[] points) in builder.Segments)
        {
            var mapped = new PathPoint[points.Length];
            for (int index = 0; index < points.Length; index++)
            {
                mapped[index] = mx.Map(points[index]);
            }

            segments.Add((verb, mapped, 0.0f));
        }

        Path result = Path.FromSegments(path.FillType, segments);
        return PathGeometry.AreFinite([.. result.Points]) ? result : new Path();
    }

    // SkRect::setBoundsCheck: the points' bounds, or an empty rect when there are none or one is not finite.
    internal static SkRect BoundsCheck(ReadOnlySpan<PathPoint> points)
    {
        if (points.Length == 0)
        {
            return default;
        }

        float left = points[0].X;
        float top = points[0].Y;
        float right = left;
        float bottom = top;
        foreach (PathPoint point in points)
        {
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
            {
                return default;
            }

            left = point.X < left ? point.X : left;
            top = point.Y < top ? point.Y : top;
            right = point.X > right ? point.X : right;
            bottom = point.Y > bottom ? point.Y : bottom;
        }

        return new SkRect(left, top, right, bottom);
    }

    private enum HalfPlaneResult
    {
        AllNegative,
        AllPositive,
        Mixed,
    }

    /// <summary>Skia's <c>SkHalfPlane</c>: a x + b y + c, positive in front of the plane.</summary>
    private struct HalfPlane(float a, float b, float c)
    {
        internal float A { get; private set; } = a;

        internal float B { get; private set; } = b;

        internal float C { get; private set; } = c;

        private readonly float Eval(float x, float y) => (A * x) + (B * y) + C;

        // SkHalfPlane::normalize: scales the normal to unit length; false when that fails.
        internal bool Normalize()
        {
            double a = A;
            double b = B;
            double c = C;
            double magnitude = Math.Sqrt((a * a) + (b * b));
            // A zero-length initial normal.
            if (magnitude == 0)
            {
                A = B = 0;
                C = 1;
                return true;
            }

            double scale = 1.0 / magnitude;
            a *= scale;
            b *= scale;
            c *= scale;
            // Not finite (as floats), or a zero-length normal.
            if (!float.IsFinite((float)a) || !float.IsFinite((float)b) || !float.IsFinite((float)c)
                || (a == 0 && b == 0))
            {
                A = B = 0;
                C = 1;
                return false;
            }

            A = (float)a;
            B = (float)b;
            C = (float)c;
            return true;
        }

        // SkHalfPlane::test: whether the bounds' diagonal aligned with the normal crosses the plane.
        internal readonly HalfPlaneResult Test(SkRect bounds)
        {
            float minX = A >= 0 ? bounds.Left : bounds.Right;
            float maxX = A >= 0 ? bounds.Right : bounds.Left;
            float minY = B >= 0 ? bounds.Top : bounds.Bottom;
            float maxY = B >= 0 ? bounds.Bottom : bounds.Top;
            float test = Eval(minX, minY);
            float sign = test * Eval(maxX, maxY);
            if (sign > 0)
            {
                // Wholly on one side.
                return test < 0 ? HalfPlaneResult.AllNegative : HalfPlaneResult.AllPositive;
            }

            return HalfPlaneResult.Mixed;
        }
    }

    /// <summary>
    /// clip()'s Rec over an <c>SkPathBuilder</c>: rebuilds a path from clipped edges, joining each to the
    /// previous one with a line when they do not meet.
    /// </summary>
    private sealed class ClipPathBuilder
    {
        private PathPoint _previous;
        private PathPoint _lastMovePoint;
        private bool _needsMoveVerb = true;

        internal List<(PathVerb Verb, PathPoint[] Points)> Segments { get; } = [];

        internal void Consume(PathEdgeClipper clipper, bool newContour)
        {
            bool addLineTo = false;
            Span<PathPoint> points = stackalloc PathPoint[4];
            while (clipper.Next(points) is { } verb)
            {
                if (newContour)
                {
                    MoveTo(points[0]);
                    _previous = points[0];
                    newContour = false;
                }

                if (addLineTo || points[0] != _previous)
                {
                    Add(PathVerb.Line, [points[0]]);
                }

                switch (verb)
                {
                    case PathVerb.Line:
                        Add(PathVerb.Line, [points[1]]);
                        _previous = points[1];
                        break;
                    case PathVerb.Quad:
                        Add(PathVerb.Quad, [points[1], points[2]]);
                        _previous = points[2];
                        break;
                    case PathVerb.Cubic:
                        Add(PathVerb.Cubic, [points[1], points[2], points[3]]);
                        _previous = points[3];
                        break;
                }

                addLineTo = true;
            }
        }

        // SkPathBuilder::moveTo.
        private void MoveTo(PathPoint point)
        {
            Segments.Add((PathVerb.Move, [point]));
            _lastMovePoint = point;
            _needsMoveVerb = false;
        }

        // SkPathBuilder::lineTo/quadTo/cubicTo, after ensureMove.
        private void Add(PathVerb verb, PathPoint[] points)
        {
            if (_needsMoveVerb)
            {
                MoveTo(_lastMovePoint);
            }

            Segments.Add((verb, points));
        }
    }
}
