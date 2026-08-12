using Avalonia;
using Avalonia.Media;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/notched_shapes.dart

public abstract class NotchedShape
{
    public abstract Geometry GetOuterPath(Rect host, Rect? guest);
}

public sealed class CircularNotchedRectangle : NotchedShape
{
    public CircularNotchedRectangle(bool inverted = false)
    {
        Inverted = inverted;
    }

    public bool Inverted { get; }

    public override Geometry GetOuterPath(Rect host, Rect? guest)
    {
        if (!guest.HasValue || !host.Intersects(guest.Value))
        {
            return new RectangleGeometry(host);
        }

        var guestRect = guest.Value;
        double radius = guestRect.Width / 2.0;
        double invertMultiplier = Inverted ? -1.0 : 1.0;
        const double s1 = 15.0;
        const double s2 = 1.0;
        double a = -radius - s2;
        double b = (Inverted ? host.Bottom : host.Top) - guestRect.Center.Y;
        double radicand = b * b * radius * radius * (a * a + b * b - radius * radius);
        double n2 = Math.Sqrt(Math.Max(0, radicand));
        double denominator = a * a + b * b;
        if (denominator <= 0)
        {
            return new RectangleGeometry(host);
        }

        double p2xA = ((a * radius * radius) - n2) / denominator;
        double p2xB = ((a * radius * radius) + n2) / denominator;
        double p2yA = Math.Sqrt(Math.Max(0, radius * radius - p2xA * p2xA)) * invertMultiplier;
        double p2yB = Math.Sqrt(Math.Max(0, radius * radius - p2xB * p2xB)) * invertMultiplier;
        double compare = b < 0 ? -1.0 : 1.0;
        var p2 = compare * p2yA > compare * p2yB
            ? new Point(p2xA, p2yA)
            : new Point(p2xB, p2yB);
        var center = guestRect.Center;
        var points = new[]
        {
            new Point(a - s1, b) + center,
            new Point(a, b) + center,
            p2 + center,
            new Point(-p2.X, p2.Y) + center,
            new Point(-a, b) + center,
            new Point(-(a - s1), b) + center,
        };

        var geometry = new StreamGeometry();
        using var path = geometry.Open();
        path.BeginFigure(host.TopLeft, isFilled: true);
        if (!Inverted)
        {
            path.LineTo(points[0]);
            path.QuadraticBezierTo(points[1], points[2]);
            path.ArcTo(
                points[3],
                new Size(radius, radius),
                rotationAngle: 0,
                isLargeArc: false,
                sweepDirection: SweepDirection.CounterClockwise);
            path.QuadraticBezierTo(points[4], points[5]);
            path.LineTo(host.TopRight);
            path.LineTo(host.BottomRight);
            path.LineTo(host.BottomLeft);
        }
        else
        {
            path.LineTo(host.TopRight);
            path.LineTo(host.BottomRight);
            path.LineTo(points[5]);
            path.QuadraticBezierTo(points[4], points[3]);
            path.ArcTo(
                points[2],
                new Size(radius, radius),
                rotationAngle: 0,
                isLargeArc: false,
                sweepDirection: SweepDirection.CounterClockwise);
            path.QuadraticBezierTo(points[1], points[0]);
            path.LineTo(host.BottomLeft);
        }
        path.EndFigure(isClosed: true);
        return geometry;
    }
}

public sealed class AutomaticNotchedShape : NotchedShape
{
    public AutomaticNotchedShape(ShapeBorder host, ShapeBorder? guest = null)
    {
        Host = host ?? throw new ArgumentNullException(nameof(host));
        Guest = guest;
    }

    public ShapeBorder Host { get; }

    public ShapeBorder? Guest { get; }

    public override Geometry GetOuterPath(Rect host, Rect? guest)
    {
        var hostGeometry = BuildShapeGeometry(Host, host);
        if (Guest is null || !guest.HasValue)
        {
            return hostGeometry;
        }

        var guestGeometry = BuildShapeGeometry(Guest, guest.Value);
        return new CombinedGeometry(
            GeometryCombineMode.Exclude,
            hostGeometry,
            guestGeometry);
    }

    private static Geometry BuildShapeGeometry(ShapeBorder shape, Rect rect)
    {
        return shape.GetOuterPath(rect).ToGeometry();
    }
}
