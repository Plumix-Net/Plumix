using Avalonia.Media;
using SkiaSharp;

namespace Plumix.UI;

// C#-only infrastructure: the bridge between the framework's Path (SkPath's data in managed code) and a
// native SKPath. Drawing backends get a native copy here; containment, boolean operations, perspective
// transforms and measurement still run on it until they are ported (docs/product avalonia-free-core.md,
// stage 2b).

/// <summary>Converts paths to and from native Skia paths.</summary>
internal static class SkiaPathInterop
{
    /// <summary>
    /// The path as a native SKPath, built once per version of the path and owned by it: callers must not
    /// mutate or dispose it.
    /// </summary>
    internal static SKPath ToNative(Path path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.NativeCache is { } cache && cache.Version == path.Version)
        {
            return (SKPath)cache.Native;
        }

        SKPath native = Build(path);
        path.NativeCache = (path.Version, native);
        return native;
    }

    /// <summary>A native copy of the path the caller owns.</summary>
    internal static SKPath CloneNativePath(this Path path) => new(ToNative(path));

    private static SKPath Build(Path path)
    {
        var native = new SKPath
        {
            FillType = path.FillType == PathFillType.EvenOdd ? SKPathFillType.EvenOdd : SKPathFillType.Winding,
        };
        if (path.Shape is { } shape)
        {
            // Rebuilt through Skia's own addOval/addRRect and transforms, so the native path carries the
            // oval/rrect flags Skia's fast paths look for.
            foreach (PathPoint move in shape.LeadingMoves)
            {
                native.MoveTo(move.X, move.Y);
            }

            var bounds = new SKRect(shape.Left, shape.Top, shape.Right, shape.Bottom);
            SKPathDirection direction = shape.CounterClockwise
                ? SKPathDirection.CounterClockwise
                : SKPathDirection.Clockwise;
            if (shape.IsRRect)
            {
                float[] radii = shape.Radii!;
                using var rrect = new SKRoundRect();
                rrect.SetRectRadii(bounds,
                [
                    new SKPoint(radii[0], radii[1]), new SKPoint(radii[2], radii[3]),
                    new SKPoint(radii[4], radii[5]), new SKPoint(radii[6], radii[7]),
                ]);
                native.AddRoundRect(rrect, direction, (uint)shape.StartIndex);
            }
            else if (shape.StartIndex == 1)
            {
                native.AddOval(bounds, direction);
            }
            else
            {
                // SkiaSharp binds addOval without a start index; a full-circle addArc starting on a
                // quadrant is addOval(oval, dir, startIndex), with index 1 at angle 0.
                native.AddArc(bounds, (shape.StartIndex - 1) * 90.0f, shape.CounterClockwise ? -360.0f : 360.0f);
            }

            foreach (PathMatrix matrix in shape.Transforms)
            {
                native.Transform(ToNative(matrix));
            }

            return native;
        }

        foreach ((PathVerb verb, PathPoint[] points, float weight) in path.Segments())
        {
            switch (verb)
            {
                case PathVerb.Move:
                    native.MoveTo(points[0].X, points[0].Y);
                    break;
                case PathVerb.Line:
                    native.LineTo(points[0].X, points[0].Y);
                    break;
                case PathVerb.Quad:
                    native.QuadTo(points[0].X, points[0].Y, points[1].X, points[1].Y);
                    break;
                case PathVerb.Conic:
                    native.ConicTo(points[0].X, points[0].Y, points[1].X, points[1].Y, weight);
                    break;
                case PathVerb.Cubic:
                    native.CubicTo(points[0].X, points[0].Y, points[1].X, points[1].Y, points[2].X, points[2].Y);
                    break;
                default:
                    native.Close();
                    break;
            }
        }

        return native;
    }

    /// <summary>Reads a native path back into a framework path, and disposes it.</summary>
    internal static Path FromNative(SKPath native)
    {
        using (native)
        {
            PathFillType fillType = native.FillType == SKPathFillType.EvenOdd
                ? PathFillType.EvenOdd
                : PathFillType.NonZero;
            return Path.FromSegments(fillType, ReadSegments(native));
        }
    }

    private static List<(PathVerb Verb, PathPoint[] Points, float Weight)> ReadSegments(SKPath native)
    {
        var segments = new List<(PathVerb, PathPoint[], float)>();
        using SKPath.RawIterator iterator = native.CreateRawIterator();
        var points = new SKPoint[4];
        SKPathVerb verb;
        while ((verb = iterator.Next(points)) != SKPathVerb.Done)
        {
            switch (verb)
            {
                case SKPathVerb.Move:
                    segments.Add((PathVerb.Move, [Point(points[0])], 0.0f));
                    break;
                case SKPathVerb.Line:
                    segments.Add((PathVerb.Line, [Point(points[1])], 0.0f));
                    break;
                case SKPathVerb.Quad:
                    segments.Add((PathVerb.Quad, [Point(points[1]), Point(points[2])], 0.0f));
                    break;
                case SKPathVerb.Conic:
                    segments.Add((PathVerb.Conic, [Point(points[1]), Point(points[2])], iterator.ConicWeight()));
                    break;
                case SKPathVerb.Cubic:
                    segments.Add((PathVerb.Cubic, [Point(points[1]), Point(points[2]), Point(points[3])], 0.0f));
                    break;
                case SKPathVerb.Close:
                    segments.Add((PathVerb.Close, [], 0.0f));
                    break;
            }
        }

        return segments;
    }

    internal static bool Contains(Path path, float x, float y) => ToNative(path).Contains(x, y);

    internal static Path? Combine(PathOperation operation, Path path1, Path path2)
    {
        var result = new SKPath();
        if (!ToNative(path1).Op(ToNative(path2), (SKPathOp)operation, result))
        {
            result.Dispose();
            return null;
        }

        return FromNative(result);
    }

    // SkPath::transform with perspective (curves promoted, perspective-clipped).
    internal static Path TransformWithPerspective(Path path, PathMatrix matrix)
    {
        var result = new SKPath();
        ToNative(path).Transform(ToNative(matrix), result);
        return FromNative(result);
    }

    internal static PathMetrics ComputeMetrics(Path path, bool forceClosed) => new(ToNative(path), forceClosed);

    private static SKMatrix ToNative(PathMatrix matrix) => new(
        matrix[0], matrix[1], matrix[2],
        matrix[3], matrix[4], matrix[5],
        matrix[6], matrix[7], matrix[8]);

    private static PathPoint Point(SKPoint point) => new(point.X, point.Y);

    private static Offset FromNative(SKPoint point) => new(point.X, point.Y);

    private static Rect FromNative(SKRect rect) => Rect.FromLTWH(rect.Left, rect.Top, rect.Width, rect.Height);

    /// <summary>The path as Avalonia geometry, for drawing and clipping through Avalonia's drawing context.</summary>
    internal static Geometry ToGeometry(this Path path)
    {
        SKPath native = ToNative(path);
        if (native.IsOval)
        {
            return new EllipseGeometry(FromNative(native.GetOvalBounds()).ToAvalonia());
        }

        if (native.IsRect)
        {
            return new RectangleGeometry(FromNative(native.GetRect()).ToAvalonia());
        }

        if (native.IsRoundRect)
        {
            using SKRoundRect rounded = native.GetRoundRect();
            SKPoint[] radii = rounded.Radii;
            if (radii.All(radius => radius == radii[0]))
            {
                return new RectangleGeometry(FromNative(rounded.Rect).ToAvalonia(), radii[0].X, radii[0].Y);
            }
        }

        var geometry = new StreamGeometry();
        using StreamGeometryContext context = geometry.Open();
        context.SetFillRule(path.FillType == PathFillType.EvenOdd ? FillRule.EvenOdd : FillRule.NonZero);
        using var iterator = native.CreateRawIterator();
        var points = new SKPoint[4];
        bool open = false;
        SKPathVerb verb;
        while ((verb = iterator.Next(points)) != SKPathVerb.Done)
        {
            switch (verb)
            {
                case SKPathVerb.Move:
                    if (open)
                    {
                        context.EndFigure(false);
                    }

                    context.BeginFigure(FromNative(points[0]).ToAvalonia(), true);
                    open = true;
                    break;
                case SKPathVerb.Line:
                    context.LineTo(FromNative(points[1]).ToAvalonia());
                    break;
                case SKPathVerb.Quad:
                    context.QuadraticBezierTo(FromNative(points[1]).ToAvalonia(), FromNative(points[2]).ToAvalonia());
                    break;
                case SKPathVerb.Conic:
                    WriteConic(context, points, iterator.ConicWeight());
                    break;
                case SKPathVerb.Cubic:
                    context.CubicBezierTo(
                        FromNative(points[1]).ToAvalonia(),
                        FromNative(points[2]).ToAvalonia(),
                        FromNative(points[3]).ToAvalonia());
                    break;
                case SKPathVerb.Close:
                    context.EndFigure(true);
                    open = false;
                    break;
            }
        }

        if (open)
        {
            context.EndFigure(false);
        }

        return geometry;
    }

    private static void WriteConic(StreamGeometryContext context, SKPoint[] points, float weight)
    {
        // Avalonia has no rational quadratic command, but its Skia backend turns ArcTo back into
        // conics, so circular and axis-aligned elliptical arcs keep their exact shape and the few
        // points Skia's analytic anti-aliasing needs. Other conics fall back to quads below.
        if (TryWriteArc(context, points, weight))
        {
            return;
        }

        // Only this bridge approximates conics; the path itself keeps exact conics.
        double factor = (weight - 1.0) / (4.0 * (2.0 + weight));
        double errorX = factor * (points[0].X - 2.0 * points[1].X + points[2].X);
        double errorY = factor * (points[0].Y - 2.0 * points[1].Y + points[2].Y);
        double error = Math.Sqrt(errorX * errorX + errorY * errorY);
        int subdivisions = 0;
        while (error > 0.01 && subdivisions < 5)
        {
            error *= 0.25;
            subdivisions++;
        }

        SKPoint[] quads = SKPath.ConvertConicToQuads(points[0], points[1], points[2], weight, subdivisions);
        for (int index = 1; index + 1 < quads.Length; index += 2)
        {
            context.QuadraticBezierTo(FromNative(quads[index]).ToAvalonia(), FromNative(quads[index + 1]).ToAvalonia());
        }
    }

    private static bool TryWriteArc(StreamGeometryContext context, SKPoint[] points, float weight)
    {
        if (!(weight > 0.0f && weight < 1.0f))
        {
            return false;
        }

        double d0X = points[1].X - points[0].X;
        double d0Y = points[1].Y - points[0].Y;
        double d1X = points[2].X - points[1].X;
        double d1Y = points[2].Y - points[1].Y;
        double cross = (d0X * d1Y) - (d0Y * d1X);
        if (cross == 0.0)
        {
            return false;
        }

        Size size;
        double length0 = Math.Sqrt((d0X * d0X) + (d0Y * d0Y));
        double length1 = Math.Sqrt((d1X * d1X) + (d1Y * d1Y));
        bool axisAligned = (d0X == 0.0 && d1Y == 0.0) || (d0Y == 0.0 && d1X == 0.0);
        if (axisAligned && Math.Abs(weight - (Math.Sqrt(2.0) / 2.0)) < 1e-6)
        {
            // A quarter of an axis-aligned ellipse, as SkPath::addRRect and addOval write corners.
            size = new Size(Math.Abs(points[2].X - points[0].X), Math.Abs(points[2].Y - points[0].Y));
        }
        else if (Math.Abs(length0 - length1) <= 1e-5 * Math.Max(length0, length1))
        {
            // A circular arc whose sweep is 2 * acos(weight).
            double radius = length0 / Math.Tan(Math.Acos(weight));
            size = new Size(radius, radius);
        }
        else
        {
            return false;
        }

        context.ArcTo(
            FromNative(points[2]).ToAvalonia(),
            size.ToAvalonia(),
            0.0,
            false,
            cross > 0.0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
        return true;
    }
}
