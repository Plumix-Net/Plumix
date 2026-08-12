using Avalonia;
using Avalonia.Media;

namespace Plumix.UI;

// Dart parity source: dart:ui Path (control-port subset used by ClipPath)

public enum PathFillType
{
    NonZero,
    EvenOdd,
}

public sealed class Path
{
    private readonly List<PathContour> _contours = [];
    private List<Point>? _currentPoints;
    private bool _currentClosed;

    public PathFillType FillType { get; set; } = PathFillType.NonZero;

    public void MoveTo(double x, double y)
    {
        FinishCurrentContour();
        _currentPoints = [new Point(x, y)];
        _currentClosed = false;
    }

    public void LineTo(double x, double y)
    {
        EnsureCurrentContour();
        _currentPoints!.Add(new Point(x, y));
    }

    public void QuadraticBezierTo(double x1, double y1, double x2, double y2)
    {
        EnsureCurrentContour();
        Point start = _currentPoints![^1];
        const int segmentCount = 16;
        for (int segment = 1; segment <= segmentCount; segment++)
        {
            double t = segment / (double)segmentCount;
            double inverse = 1.0 - t;
            _currentPoints.Add(new Point(
                (inverse * inverse * start.X) + (2.0 * inverse * t * x1) + (t * t * x2),
                (inverse * inverse * start.Y) + (2.0 * inverse * t * y1) + (t * t * y2)));
        }
    }

    public void CubicTo(double x1, double y1, double x2, double y2, double x3, double y3)
    {
        EnsureCurrentContour();
        Point start = _currentPoints![^1];
        const int segmentCount = 24;
        for (int segment = 1; segment <= segmentCount; segment++)
        {
            double t = segment / (double)segmentCount;
            double inverse = 1.0 - t;
            _currentPoints.Add(new Point(
                (inverse * inverse * inverse * start.X)
                + (3.0 * inverse * inverse * t * x1)
                + (3.0 * inverse * t * t * x2)
                + (t * t * t * x3),
                (inverse * inverse * inverse * start.Y)
                + (3.0 * inverse * inverse * t * y1)
                + (3.0 * inverse * t * t * y2)
                + (t * t * t * y3)));
        }
    }

    // Dart parity source: dart:ui Path.addArc / Path.arcTo (oval-inscribed sweep).
    public void AddArc(Rect oval, double startAngleRadians, double sweepAngleRadians)
    {
        FinishCurrentContour();
        _currentPoints = [];
        _currentClosed = false;
        AppendArcPoints(oval, startAngleRadians, sweepAngleRadians, forceMoveTo: true);
    }

    public void ArcTo(Rect oval, double startAngleRadians, double sweepAngleRadians, bool forceMoveTo)
    {
        EnsureCurrentContour();
        AppendArcPoints(oval, startAngleRadians, sweepAngleRadians, forceMoveTo);
    }

    private void AppendArcPoints(
        Rect oval,
        double startAngleRadians,
        double sweepAngleRadians,
        bool forceMoveTo)
    {
        double radiusX = oval.Width / 2.0;
        double radiusY = oval.Height / 2.0;
        Point center = oval.Center;
        int segmentCount = Math.Max(2, (int)Math.Ceiling(Math.Abs(sweepAngleRadians) / (Math.PI / 16.0)));
        for (int segment = 0; segment <= segmentCount; segment++)
        {
            double angle = startAngleRadians + (sweepAngleRadians * segment / segmentCount);
            var point = new Point(
                center.X + (Math.Cos(angle) * radiusX),
                center.Y + (Math.Sin(angle) * radiusY));
            if (segment == 0 && !forceMoveTo && _currentPoints!.Count > 0)
            {
                continue;
            }

            _currentPoints!.Add(point);
        }
    }

    public void Close()
    {
        if (_currentPoints is null)
        {
            return;
        }

        _currentClosed = true;
        FinishCurrentContour();
    }

    public void AddRect(Rect rect)
    {
        FinishCurrentContour();
        _contours.Add(PathContour.Rectangle(rect));
    }

    public void AddOval(Rect oval)
    {
        FinishCurrentContour();
        _contours.Add(PathContour.Ellipse(oval));
    }

    public void AddRoundedRect(Rect rect, double radius)
    {
        FinishCurrentContour();
        double clampedRadius = Math.Clamp(radius, 0.0, Math.Min(rect.Width, rect.Height) / 2.0);
        _contours.Add(PathContour.RoundedRectangle(rect, clampedRadius));
    }

    public void AddPath(Path other)
    {
        ArgumentNullException.ThrowIfNull(other);
        FinishCurrentContour();
        _contours.AddRange(other.SnapshotContours());
    }

    public bool Contains(Point point)
    {
        IReadOnlyList<PathContour> contours = SnapshotContours();
        if (FillType == PathFillType.EvenOdd)
        {
            bool inside = false;
            foreach (PathContour contour in contours)
            {
                if (contour.Contains(point))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        int winding = 0;
        foreach (PathContour contour in contours)
        {
            winding += contour.WindingNumber(point);
        }

        return winding != 0;
    }

    internal Geometry ToGeometry()
    {
        IReadOnlyList<PathContour> contours = SnapshotContours();
        if (contours.Count == 0)
        {
            return new RectangleGeometry();
        }

        if (contours.Count == 1)
        {
            PathContour contour = contours[0];
            return contour.Kind switch
            {
                PathContourKind.Rectangle => new RectangleGeometry(contour.Bounds),
                PathContourKind.RoundedRectangle => new RectangleGeometry(
                    contour.Bounds,
                    contour.Radius,
                    contour.Radius),
                PathContourKind.Ellipse => new EllipseGeometry(contour.Bounds),
                _ => BuildStreamGeometry(contours, FillType),
            };
        }

        return BuildStreamGeometry(contours, FillType);
    }

    private static Geometry BuildStreamGeometry(
        IReadOnlyList<PathContour> contours,
        PathFillType fillType)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.SetFillRule(fillType == PathFillType.NonZero ? FillRule.NonZero : FillRule.EvenOdd);
        foreach (PathContour contour in contours)
        {
            IReadOnlyList<Point> points = contour.FlattenedPoints;
            if (points.Count == 0)
            {
                continue;
            }

            context.BeginFigure(points[0], isFilled: true);
            for (int index = 1; index < points.Count; index++)
            {
                context.LineTo(points[index]);
            }

            context.EndFigure(contour.IsClosed);
        }

        return geometry;
    }

    private void EnsureCurrentContour()
    {
        _currentPoints ??= [new Point(0, 0)];
    }

    private IReadOnlyList<PathContour> SnapshotContours()
    {
        if (_currentPoints is not { Count: > 0 } points)
        {
            return _contours;
        }

        var contours = new List<PathContour>(_contours.Count + 1);
        contours.AddRange(_contours);
        contours.Add(PathContour.Polygon(points, _currentClosed));
        return contours;
    }

    private void FinishCurrentContour()
    {
        if (_currentPoints is not { Count: > 0 } points)
        {
            _currentPoints = null;
            _currentClosed = false;
            return;
        }

        _contours.Add(PathContour.Polygon(points, _currentClosed));
        _currentPoints = null;
        _currentClosed = false;
    }

    private enum PathContourKind
    {
        Polygon,
        Rectangle,
        RoundedRectangle,
        Ellipse,
    }

    private sealed class PathContour
    {
        private PathContour(
            PathContourKind kind,
            Rect bounds,
            IReadOnlyList<Point> points,
            bool isClosed,
            double radius = 0.0)
        {
            Kind = kind;
            Bounds = bounds;
            FlattenedPoints = points;
            IsClosed = isClosed;
            Radius = radius;
        }

        public PathContourKind Kind { get; }
        public Rect Bounds { get; }
        public IReadOnlyList<Point> FlattenedPoints { get; }
        public bool IsClosed { get; }
        public double Radius { get; }

        public static PathContour Polygon(IReadOnlyList<Point> points, bool isClosed)
        {
            double minX = points.Min(static point => point.X);
            double minY = points.Min(static point => point.Y);
            double maxX = points.Max(static point => point.X);
            double maxY = points.Max(static point => point.Y);
            return new PathContour(
                PathContourKind.Polygon,
                new Rect(minX, minY, maxX - minX, maxY - minY),
                points.ToArray(),
                isClosed);
        }

        public static PathContour Rectangle(Rect rect)
        {
            return new PathContour(
                PathContourKind.Rectangle,
                rect,
                [rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft],
                isClosed: true);
        }

        public static PathContour RoundedRectangle(Rect rect, double radius)
        {
            IReadOnlyList<Point> points = radius <= 0.0
                ? [rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft]
                : BuildRoundedRectanglePoints(rect, radius);
            return new PathContour(
                PathContourKind.RoundedRectangle,
                rect,
                points,
                isClosed: true,
                radius);
        }

        public static PathContour Ellipse(Rect rect)
        {
            const int segmentCount = 48;
            var points = new List<Point>(segmentCount);
            for (int segment = 0; segment < segmentCount; segment++)
            {
                double angle = (Math.PI * 2.0 * segment) / segmentCount;
                points.Add(new Point(
                    rect.Center.X + (Math.Cos(angle) * rect.Width / 2.0),
                    rect.Center.Y + (Math.Sin(angle) * rect.Height / 2.0)));
            }

            return new PathContour(PathContourKind.Ellipse, rect, points, isClosed: true);
        }

        public bool Contains(Point point)
        {
            return Kind switch
            {
                PathContourKind.Rectangle => Bounds.Contains(point),
                PathContourKind.RoundedRectangle => ContainsRoundedRectangle(point),
                PathContourKind.Ellipse => ContainsEllipse(point),
                _ => WindingNumber(point) != 0,
            };
        }

        public int WindingNumber(Point point)
        {
            if (!ContainsBoundsInclusive(Bounds, point))
            {
                return 0;
            }

            if (Kind == PathContourKind.Ellipse)
            {
                return ContainsEllipse(point) ? 1 : 0;
            }

            if (Kind == PathContourKind.RoundedRectangle)
            {
                return ContainsRoundedRectangle(point) ? 1 : 0;
            }

            int winding = 0;
            IReadOnlyList<Point> points = FlattenedPoints;
            int edgeCount = points.Count;
            for (int index = 0; index < edgeCount; index++)
            {
                Point start = points[index];
                Point end = points[(index + 1) % points.Count];
                if (start.Y <= point.Y)
                {
                    if (end.Y > point.Y && Cross(start, end, point) > 0)
                    {
                        winding += 1;
                    }
                }
                else if (end.Y <= point.Y && Cross(start, end, point) < 0)
                {
                    winding -= 1;
                }
            }

            return winding;
        }

        private bool ContainsRoundedRectangle(Point point)
        {
            if (!Bounds.Contains(point))
            {
                return false;
            }

            if (Radius <= 0.0
                || (point.X >= Bounds.Left + Radius && point.X <= Bounds.Right - Radius)
                || (point.Y >= Bounds.Top + Radius && point.Y <= Bounds.Bottom - Radius))
            {
                return true;
            }

            double centerX = point.X < Bounds.Center.X ? Bounds.Left + Radius : Bounds.Right - Radius;
            double centerY = point.Y < Bounds.Center.Y ? Bounds.Top + Radius : Bounds.Bottom - Radius;
            double deltaX = point.X - centerX;
            double deltaY = point.Y - centerY;
            return (deltaX * deltaX) + (deltaY * deltaY) <= Radius * Radius;
        }

        private bool ContainsEllipse(Point point)
        {
            if (Bounds.Width <= 0.0 || Bounds.Height <= 0.0)
            {
                return false;
            }

            double normalizedX = (point.X - Bounds.Center.X) / Bounds.Width;
            double normalizedY = (point.Y - Bounds.Center.Y) / Bounds.Height;
            return (normalizedX * normalizedX) + (normalizedY * normalizedY) <= 0.25;
        }

        private static bool ContainsBoundsInclusive(Rect rect, Point point)
        {
            return point.X >= rect.Left
                   && point.X <= rect.Right
                   && point.Y >= rect.Top
                   && point.Y <= rect.Bottom;
        }

        private static double Cross(Point start, Point end, Point point)
        {
            return ((end.X - start.X) * (point.Y - start.Y))
                   - ((point.X - start.X) * (end.Y - start.Y));
        }

        private static IReadOnlyList<Point> BuildRoundedRectanglePoints(Rect rect, double radius)
        {
            const int segmentsPerCorner = 8;
            var points = new List<Point>((segmentsPerCorner + 1) * 4);
            AddArc(points, new Point(rect.Right - radius, rect.Top + radius), radius, -Math.PI / 2.0, 0.0);
            AddArc(points, new Point(rect.Right - radius, rect.Bottom - radius), radius, 0.0, Math.PI / 2.0);
            AddArc(points, new Point(rect.Left + radius, rect.Bottom - radius), radius, Math.PI / 2.0, Math.PI);
            AddArc(points, new Point(rect.Left + radius, rect.Top + radius), radius, Math.PI, Math.PI * 1.5);
            return points;

            static void AddArc(
                List<Point> target,
                Point center,
                double arcRadius,
                double startAngle,
                double endAngle)
            {
                for (int segment = 0; segment <= segmentsPerCorner; segment++)
                {
                    double t = segment / (double)segmentsPerCorner;
                    double angle = startAngle + ((endAngle - startAngle) * t);
                    target.Add(new Point(
                        center.X + (Math.Cos(angle) * arcRadius),
                        center.Y + (Math.Sin(angle) * arcRadius)));
                }
            }
        }
    }
}
