using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/notched_shapes.dart

public abstract class NotchedShape
{
    public abstract Path GetOuterPath(Rect host, Rect? guest);
}

public sealed class CircularNotchedRectangle : NotchedShape
{
    public CircularNotchedRectangle(bool inverted = false)
    {
        Inverted = inverted;
    }

    public bool Inverted { get; }

    public override Path GetOuterPath(Rect host, Rect? guest)
    {
        if (!guest.HasValue || !host.Overlaps(guest.Value))
        {
            return RectPath(host);
        }

        var guestRect = guest.Value;
        double radius = guestRect.Width / 2.0;
        double invertMultiplier = Inverted ? -1.0 : 1.0;
        const double s1 = 15.0;
        const double s2 = 1.0;
        double a = -radius - s2;
        double b = (Inverted ? host.Bottom : host.Top) - guestRect.Center.Dy;
        double radicand = b * b * radius * radius * (a * a + b * b - radius * radius);
        double n2 = Math.Sqrt(Math.Max(0, radicand));
        double denominator = a * a + b * b;
        if (denominator <= 0)
        {
            return RectPath(host);
        }

        double p2xA = ((a * radius * radius) - n2) / denominator;
        double p2xB = ((a * radius * radius) + n2) / denominator;
        double p2yA = Math.Sqrt(Math.Max(0, radius * radius - p2xA * p2xA)) * invertMultiplier;
        double p2yB = Math.Sqrt(Math.Max(0, radius * radius - p2xB * p2xB)) * invertMultiplier;
        double compare = b < 0 ? -1.0 : 1.0;
        var p2 = compare * p2yA > compare * p2yB
            ? new Offset(p2xA, p2yA)
            : new Offset(p2xB, p2yB);
        var center = guestRect.Center;
        var points = new[]
        {
            new Offset(a - s1, b) + center,
            new Offset(a, b) + center,
            p2 + center,
            new Offset(-p2.Dx, p2.Dy) + center,
            new Offset(-a, b) + center,
            new Offset(-(a - s1), b) + center,
        };

        var path = new Path();
        path.MoveTo(host.Left, host.Top);
        if (!Inverted)
        {
            path.LineTo(points[0].Dx, points[0].Dy);
            path.QuadraticBezierTo(points[1].Dx, points[1].Dy, points[2].Dx, points[2].Dy);
            path.ArcToPoint(points[3], radius: Radius.Circular(radius), clockwise: false);
            path.QuadraticBezierTo(points[4].Dx, points[4].Dy, points[5].Dx, points[5].Dy);
            path.LineTo(host.Right, host.Top);
            path.LineTo(host.Right, host.Bottom);
            path.LineTo(host.Left, host.Bottom);
        }
        else
        {
            path.LineTo(host.Right, host.Top);
            path.LineTo(host.Right, host.Bottom);
            path.LineTo(points[5].Dx, points[5].Dy);
            path.QuadraticBezierTo(points[4].Dx, points[4].Dy, points[3].Dx, points[3].Dy);
            path.ArcToPoint(points[2], radius: Radius.Circular(radius), clockwise: false);
            path.QuadraticBezierTo(points[1].Dx, points[1].Dy, points[0].Dx, points[0].Dy);
            path.LineTo(host.Left, host.Bottom);
        }

        path.Close();
        return path;
    }

    private static Path RectPath(Rect rect)
    {
        var path = new Path();
        path.AddRect(rect);
        return path;
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

    public override Path GetOuterPath(Rect host, Rect? guest)
    {
        Path hostPath = Host.GetOuterPath(host);
        if (Guest is null || guest is null)
        {
            return hostPath;
        }

        Path guestPath = Guest.GetOuterPath(guest.Value);
        return Path.Combine(PathOperation.Difference, hostPath, guestPath);
    }
}
