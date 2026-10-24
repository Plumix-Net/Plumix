using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using SkiaSharp;

namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/painting.dart (Path)
// Engine adapter: engine/src/flutter/lib/ui/painting/path.cc. Skia owns geometry, as in Flutter.
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

/// <summary>A mutable collection of native line, quadratic, conic and cubic contours.</summary>
public sealed class Path
{
    private readonly SKPath _path;

    public Path() : this(new SKPath()) { }
    public Path(Path source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _path = new SKPath(source._path);
    }
    internal Path(SKPath path) => _path = path;

    public PathFillType FillType
    {
        get => _path.FillType == SKPathFillType.EvenOdd ? PathFillType.EvenOdd : PathFillType.NonZero;
        set => _path.FillType = value == PathFillType.EvenOdd ? SKPathFillType.EvenOdd : SKPathFillType.Winding;
    }

    public void MoveTo(double x, double y) => _path.MoveTo(Narrow(x), Narrow(y));
    public void RelativeMoveTo(double dx, double dy) => _path.RMoveTo(Narrow(dx), Narrow(dy));
    public void LineTo(double x, double y) => _path.LineTo(Narrow(x), Narrow(y));
    public void RelativeLineTo(double dx, double dy) => _path.RLineTo(Narrow(dx), Narrow(dy));
    public void QuadraticBezierTo(double x1, double y1, double x2, double y2) =>
        _path.QuadTo(Narrow(x1), Narrow(y1), Narrow(x2), Narrow(y2));
    public void RelativeQuadraticBezierTo(double x1, double y1, double x2, double y2) =>
        _path.RQuadTo(Narrow(x1), Narrow(y1), Narrow(x2), Narrow(y2));
    public void CubicTo(double x1, double y1, double x2, double y2, double x3, double y3) =>
        _path.CubicTo(Narrow(x1), Narrow(y1), Narrow(x2), Narrow(y2), Narrow(x3), Narrow(y3));
    public void RelativeCubicTo(double x1, double y1, double x2, double y2, double x3, double y3) =>
        _path.RCubicTo(Narrow(x1), Narrow(y1), Narrow(x2), Narrow(y2), Narrow(x3), Narrow(y3));
    public void ConicTo(double x1, double y1, double x2, double y2, double weight) =>
        _path.ConicTo(Narrow(x1), Narrow(y1), Narrow(x2), Narrow(y2), Narrow(weight));
    public void RelativeConicTo(double x1, double y1, double x2, double y2, double weight) =>
        _path.RConicTo(Narrow(x1), Narrow(y1), Narrow(x2), Narrow(y2), Narrow(weight));

    public void ArcTo(Rect oval, double startAngleRadians, double sweepAngleRadians, bool forceMoveTo)
    {
        CheckRect(oval);
        _path.ArcTo(ToNative(oval), Degrees(startAngleRadians), Degrees(sweepAngleRadians), forceMoveTo);
    }

    public void ArcToPoint(Point arcEnd, Radius radius = default,
        double rotation = 0.0, bool largeArc = false, bool clockwise = true)
    {
        CheckPoint(arcEnd);
        CheckRadius(radius);
        _path.ArcTo(Narrow(radius.X), Narrow(radius.Y), Narrow(rotation),
            largeArc ? SKPathArcSize.Large : SKPathArcSize.Small,
            clockwise ? SKPathDirection.Clockwise : SKPathDirection.CounterClockwise,
            Narrow(arcEnd.X), Narrow(arcEnd.Y));
    }

    public void RelativeArcToPoint(Point arcEndDelta, Radius radius = default,
        double rotation = 0.0, bool largeArc = false, bool clockwise = true)
    {
        CheckPoint(arcEndDelta);
        CheckRadius(radius);
        _path.RArcTo(Narrow(radius.X), Narrow(radius.Y), Narrow(rotation),
            largeArc ? SKPathArcSize.Large : SKPathArcSize.Small,
            clockwise ? SKPathDirection.Clockwise : SKPathDirection.CounterClockwise,
            Narrow(arcEndDelta.X), Narrow(arcEndDelta.Y));
    }

    public void AddRect(Rect rect)
    {
        CheckRect(rect);
        _path.AddRect(ToNative(rect));
    }

    public void AddOval(Rect oval)
    {
        CheckRect(oval);
        _path.AddOval(ToNative(oval));
    }

    public void AddArc(Rect oval, double startAngleRadians, double sweepAngleRadians)
    {
        CheckRect(oval);
        _path.AddArc(ToNative(oval), Degrees(startAngleRadians), Degrees(sweepAngleRadians));
    }

    public void AddPolygon(IReadOnlyList<Point> points, bool close)
    {
        ArgumentNullException.ThrowIfNull(points);
        var native = new SKPoint[points.Count];
        for (int index = 0; index < points.Count; index++)
        {
            native[index] = ToNative(points[index]);
        }

        _path.AddPoly(native, close);
    }

    public void AddRoundedRect(Rect rect, double radius) => AddRRect(RRect.FromRectXY(rect, radius, radius));

    public void AddRRect(RRect rrect)
    {
        RRect normalized = rrect.NormalizeForPaint();
        CheckRect(normalized.Rect);
        using var roundRect = new SKRoundRect();
        roundRect.SetRectRadii(ToNative(normalized.Rect),
        [
            ToNativeRadius(normalized.TopLeft), ToNativeRadius(normalized.TopRight),
            ToNativeRadius(normalized.BottomRight), ToNativeRadius(normalized.BottomLeft),
        ]);
        _path.AddRoundRect(roundRect);
    }

    public void AddRSuperellipse(RSuperellipse rsuperellipse) => AddPath(rsuperellipse.ToPath());
    public void AddPath(Path other) => AddPath(other, default);
    public void AddPath(Path path, Point offset, IReadOnlyList<double>? matrix4 = null) =>
        Append(path, offset, matrix4, SKPathAddMode.Append);
    public void ExtendWithPath(Path path, Point offset, IReadOnlyList<double>? matrix4 = null) =>
        Append(path, offset, matrix4, SKPathAddMode.Extend);

    private void Append(Path path, Point offset, IReadOnlyList<double>? matrix4, SKPathAddMode mode)
    {
        ArgumentNullException.ThrowIfNull(path);
        CheckPoint(offset);
        if (matrix4 is null)
        {
            _path.AddPath(path._path, Narrow(offset.X), Narrow(offset.Y), mode);
        }
        else
        {
            SKMatrix matrix = ToNativeMatrix(matrix4);
            matrix.TransX += Narrow(offset.X);
            matrix.TransY += Narrow(offset.Y);
            _path.AddPath(path._path, in matrix, mode);
        }
    }

    public void Close() => _path.Close();
    public void Reset() => _path.Reset();

    public bool Contains(Point point)
    {
        CheckPoint(point);
        return _path.Contains(Narrow(point.X), Narrow(point.Y));
    }

    public Path Shift(Point offset)
    {
        CheckPoint(offset);
        var result = new Path(this);
        result._path.Offset(Narrow(offset.X), Narrow(offset.Y));
        return result;
    }

    public Path Transform(Matrix matrix) => TransformNative(new SKMatrix(
        Narrow(matrix.M11), Narrow(matrix.M21), Narrow(matrix.M31),
        Narrow(matrix.M12), Narrow(matrix.M22), Narrow(matrix.M32), 0, 0, 1));
    public Path Transform(IReadOnlyList<double> matrix4) => TransformNative(ToNativeMatrix(matrix4));

    private Path TransformNative(SKMatrix matrix)
    {
        var result = new SKPath();
        _path.Transform(matrix, result);
        return new Path(result);
    }

    public Rect GetBounds()
    {
        // Pinned SkPathBuilder.computeFiniteBounds ignores a trailing move, but checks all points
        // for finiteness. The bundled SKPath.Bounds predates that trimming rule.
        SKPoint[] points = _path.Points;
        if (points.Length == 0 || points.Any(point => !float.IsFinite(point.X) || !float.IsFinite(point.Y)))
        {
            return default;
        }

        using var iterator = _path.CreateRawIterator();
        var segment = new SKPoint[4];
        SKPathVerb last = SKPathVerb.Done;
        SKPathVerb verb;
        while ((verb = iterator.Next(segment)) != SKPathVerb.Done)
        {
            last = verb;
        }

        int count = points.Length - (last == SKPathVerb.Move && _path.VerbCount > 1 ? 1 : 0);
        double left = points[0].X;
        double top = points[0].Y;
        double right = left;
        double bottom = top;
        for (int index = 1; index < count; index++)
        {
            left = Math.Min(left, points[index].X);
            top = Math.Min(top, points[index].Y);
            right = Math.Max(right, points[index].X);
            bottom = Math.Max(bottom, points[index].Y);
        }

        return new Rect(left, top, right - left, bottom - top);
    }

    public static Path Combine(PathOperation operation, Path path1, Path path2)
    {
        ArgumentNullException.ThrowIfNull(path1);
        ArgumentNullException.ThrowIfNull(path2);
        var result = new SKPath();
        if (!path1._path.Op(path2._path, (SKPathOp)operation, result))
        {
            result.Dispose();
            throw new InvalidOperationException(
                "Path.combine() failed.  This may be due an invalid path; in particular, check for NaN values.");
        }

        return new Path(result);
    }

    public PathMetrics ComputeMetrics(bool forceClosed = false) => new(_path, forceClosed);
    public override string ToString() => "Path";

    internal SKPath CloneNativePath() => new(_path);

    internal Geometry ToGeometry()
    {
        if (_path.IsOval)
        {
            return new EllipseGeometry(FromNative(_path.GetOvalBounds()));
        }

        if (_path.IsRect)
        {
            return new RectangleGeometry(FromNative(_path.GetRect()));
        }

        if (_path.IsRoundRect)
        {
            using SKRoundRect rounded = _path.GetRoundRect();
            SKPoint[] radii = rounded.Radii;
            if (radii.All(radius => radius == radii[0]))
            {
                return new RectangleGeometry(FromNative(rounded.Rect), radii[0].X, radii[0].Y);
            }
        }

        var geometry = new StreamGeometry();
        using StreamGeometryContext context = geometry.Open();
        context.SetFillRule(FillType == PathFillType.EvenOdd ? FillRule.EvenOdd : FillRule.NonZero);
        using var iterator = _path.CreateRawIterator();
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

                    context.BeginFigure(FromNative(points[0]), true);
                    open = true;
                    break;
                case SKPathVerb.Line:
                    context.LineTo(FromNative(points[1]));
                    break;
                case SKPathVerb.Quad:
                    context.QuadraticBezierTo(FromNative(points[1]), FromNative(points[2]));
                    break;
                case SKPathVerb.Conic:
                    WriteConic(context, points, iterator.ConicWeight());
                    break;
                case SKPathVerb.Cubic:
                    context.CubicBezierTo(FromNative(points[1]), FromNative(points[2]), FromNative(points[3]));
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

        // Only this bridge approximates conics; native geometry, containment, boolean operations,
        // transforms and metrics retain exact conics.
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
            context.QuadraticBezierTo(FromNative(quads[index]), FromNative(quads[index + 1]));
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
            FromNative(points[2]),
            size,
            0.0,
            false,
            cross > 0.0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
        return true;
    }

    internal static float Narrow(double value) => double.IsFinite(value)
        ? (float)Math.Clamp(value, -float.MaxValue, float.MaxValue) : (float)value;
    private static float Degrees(double radians) => Narrow(radians) * 180.0f / (float)Math.PI;
    private static SKPoint ToNative(Point point) => new(Narrow(point.X), Narrow(point.Y));
    private static Point FromNative(SKPoint point) => new(point.X, point.Y);
    private static SKRect ToNative(Rect rect) =>
        new(Narrow(rect.Left), Narrow(rect.Top), Narrow(rect.Right), Narrow(rect.Bottom));
    private static Rect FromNative(SKRect rect) => new(rect.Left, rect.Top, rect.Width, rect.Height);

    private static SKPoint ToNativeRadius(Radius radius)
    {
        CheckRadius(radius);
        return new SKPoint(Narrow(radius.X), Narrow(radius.Y));
    }

    private static SKMatrix ToNativeMatrix(IReadOnlyList<double> matrix4)
    {
        ArgumentNullException.ThrowIfNull(matrix4);
        DebugAssertions.Assert(matrix4.Count == 16 && matrix4.All(double.IsFinite),
            "'matrix4' must have 16 finite entries.");
        return new SKMatrix(
            Narrow(matrix4[0]), Narrow(matrix4[4]), Narrow(matrix4[12]),
            Narrow(matrix4[1]), Narrow(matrix4[5]), Narrow(matrix4[13]),
            Narrow(matrix4[3]), Narrow(matrix4[7]), Narrow(matrix4[15]));
    }

    private static void CheckPoint(Point point) => DebugAssertions.Assert(
        !double.IsNaN(point.X) && !double.IsNaN(point.Y), "Offset argument contained a NaN value.");
    private static void CheckRadius(Radius radius) => DebugAssertions.Assert(
        !double.IsNaN(radius.X) && !double.IsNaN(radius.Y), "Radius argument contained a NaN value.");
    private static void CheckRect(Rect rect) => DebugAssertions.Assert(
        !double.IsNaN(rect.Left) && !double.IsNaN(rect.Top)
        && !double.IsNaN(rect.Right) && !double.IsNaN(rect.Bottom), "Rect argument contained a NaN value.");
}
