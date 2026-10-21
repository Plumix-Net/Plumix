using Avalonia;
using Plumix.Rendering;

namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/geometry.dart (RRect)

public readonly record struct RRect
{
    private readonly RoundRectGeometry _geometry;

    public RRect(Rect rect, Radius topLeft, Radius topRight, Radius bottomRight, Radius bottomLeft)
        : this(new RoundRectGeometry(rect.Left, rect.Top, rect.Right, rect.Bottom,
            topLeft, topRight, bottomRight, bottomLeft)) { }

    private RRect(RoundRectGeometry geometry) => _geometry = geometry;

    public Rect Rect => _geometry.OuterRect;
    public Rect OuterRect => _geometry.OuterRect;
    public Radius TopLeft => _geometry.TopLeft;
    public Radius TopRight => _geometry.TopRight;
    public Radius BottomRight => _geometry.BottomRight;
    public Radius BottomLeft => _geometry.BottomLeft;
    public double Left => _geometry.Left;
    public double Top => _geometry.Top;
    public double Right => _geometry.Right;
    public double Bottom => _geometry.Bottom;
    public double Width => _geometry.Width;
    public double Height => _geometry.Height;
    public Point Center => _geometry.Center;
    public double ShortestSide => _geometry.ShortestSide;
    public double LongestSide => _geometry.LongestSide;
    public Rect SafeInnerRect => _geometry.SafeInnerRect;
    public Rect MiddleRect => _geometry.MiddleRect;
    public Rect WideMiddleRect => _geometry.WideMiddleRect;
    public Rect TallMiddleRect => _geometry.TallMiddleRect;
    public bool IsEmpty => _geometry.IsEmpty;
    public bool IsFinite => _geometry.IsFinite;
    public bool IsRect => _geometry.IsRect;
    public bool IsStadium => _geometry.IsStadium;
    public bool IsEllipse => _geometry.IsEllipse;
    public bool IsCircle => _geometry.IsCircle;
    public bool HasNaN => _geometry.HasNaN;
    public Radius TlRadius => TopLeft;
    public double TlRadiusX => TopLeft.X;
    public double TlRadiusY => TopLeft.Y;
    public Radius TrRadius => TopRight;
    public double TrRadiusX => TopRight.X;
    public double TrRadiusY => TopRight.Y;
    public Radius BrRadius => BottomRight;
    public double BrRadiusX => BottomRight.X;
    public double BrRadiusY => BottomRight.Y;
    public Radius BlRadius => BottomLeft;
    public double BlRadiusX => BottomLeft.X;
    public double BlRadiusY => BottomLeft.Y;

    public static RRect Zero => default;
    public static RRect FromRectAndRadius(Rect rect, Radius radius) => new(rect, radius, radius, radius, radius);
    public static RRect FromRectAndRadius(Rect rect, double radius) => FromRectAndRadius(rect, Radius.Circular(radius));
    public static RRect FromRectXY(Rect rect, double radiusX, double radiusY) =>
        FromRectAndRadius(rect, new Radius(radiusX, radiusY));
    public static RRect FromLTRBXY(
        double left, double top, double right, double bottom, double radiusX, double radiusY) =>
        FromLTRBR(left, top, right, bottom, new Radius(radiusX, radiusY));
    public static RRect FromLTRBR(double left, double top, double right, double bottom, Radius radius) =>
        FromLTRBAndCorners(left, top, right, bottom, radius, radius, radius, radius);
    public static RRect FromLTRBAndCorners(
        double left, double top, double right, double bottom,
        Radius topLeft = default, Radius topRight = default,
        Radius bottomRight = default, Radius bottomLeft = default) =>
        new(new RoundRectGeometry(left, top, right, bottom, topLeft, topRight, bottomRight, bottomLeft));
    public static RRect FromRectAndCorners(
        Rect rect, Radius? topLeft = null, Radius? topRight = null,
        Radius? bottomRight = null, Radius? bottomLeft = null) =>
        new(rect, topLeft ?? default, topRight ?? default, bottomRight ?? default, bottomLeft ?? default);

    public RRect Shift(Point offset) => new(_geometry.Shift(offset));
    public RRect Inflate(double delta) => new(_geometry.Inflate(delta));
    public RRect Deflate(double delta) => Inflate(-delta);
    public RRect ScaleRadii() => new(_geometry.ScaleRadii());
    public static RRect? Lerp(RRect? a, RRect? b, double t) => a is { } start
        ? new RRect(start._geometry.LerpTo(b?._geometry, t))
        : b is { } end ? new RRect(end._geometry.LerpTo(null, 1 - t)) : null;
    public override string ToString() => _geometry.Format(nameof(RRect));

    public BorderRadius Radii => new(TopLeft, TopRight, BottomRight, BottomLeft);
    public static RRect FromRectAndCorners(Rect rect, BorderRadius borderRadius) => new(
        rect, borderRadius.TopLeftRadius.Clamp(minimum: Radius.Zero),
        borderRadius.TopRightRadius.Clamp(minimum: Radius.Zero),
        borderRadius.BottomRightRadius.Clamp(minimum: Radius.Zero),
        borderRadius.BottomLeftRadius.Clamp(minimum: Radius.Zero));

    // Dart parity source: flutter/packages/flutter/lib/src/painting/edge_insets.dart (inflateRRect/deflateRRect)
    public RRect InflateEdges(Thickness insets) => FromLTRBAndCorners(
        Left - insets.Left, Top - insets.Top, Right + insets.Right, Bottom + insets.Bottom,
        new Radius(TopLeft.X + insets.Left, TopLeft.Y + insets.Top).Clamp(minimum: Radius.Zero),
        new Radius(TopRight.X + insets.Right, TopRight.Y + insets.Top).Clamp(minimum: Radius.Zero),
        new Radius(BottomRight.X + insets.Right, BottomRight.Y + insets.Bottom).Clamp(minimum: Radius.Zero),
        new Radius(BottomLeft.X + insets.Left, BottomLeft.Y + insets.Bottom).Clamp(minimum: Radius.Zero));
    public RRect DeflateEdges(Thickness insets) =>
        InflateEdges(new Thickness(-insets.Left, -insets.Top, -insets.Right, -insets.Bottom));

    /// <summary>Whether the point specified by the given offset lies inside the rounded rectangle.</summary>
    /// <remarks>
    /// Dart's <c>RRect.contains</c>: the bounding box is half-open (the right and bottom edges are outside),
    /// and a point in a corner area must lie inside the corner's ellipse after <see cref="ScaleRadii"/>.
    /// </remarks>
    public bool Contains(Point point)
    {
        if (point.X < Left || point.X >= Right || point.Y < Top || point.Y >= Bottom)
        {
            return false;
        } // outside bounding box

        RRect scaled = ScaleRadii();

        double x;
        double y;
        double radiusX;
        double radiusY;
        // check whether point is in one of the rounded corner areas
        // x, y -> translate to ellipse center
        if (point.X < Left + scaled.TopLeft.X && point.Y < Top + scaled.TopLeft.Y)
        {
            x = point.X - Left - scaled.TopLeft.X;
            y = point.Y - Top - scaled.TopLeft.Y;
            radiusX = scaled.TopLeft.X;
            radiusY = scaled.TopLeft.Y;
        }
        else if (point.X > Right - scaled.TopRight.X && point.Y < Top + scaled.TopRight.Y)
        {
            x = point.X - Right + scaled.TopRight.X;
            y = point.Y - Top - scaled.TopRight.Y;
            radiusX = scaled.TopRight.X;
            radiusY = scaled.TopRight.Y;
        }
        else if (point.X > Right - scaled.BottomRight.X && point.Y > Bottom - scaled.BottomRight.Y)
        {
            x = point.X - Right + scaled.BottomRight.X;
            y = point.Y - Bottom + scaled.BottomRight.Y;
            radiusX = scaled.BottomRight.X;
            radiusY = scaled.BottomRight.Y;
        }
        else if (point.X < Left + scaled.BottomLeft.X && point.Y > Bottom - scaled.BottomLeft.Y)
        {
            x = point.X - Left - scaled.BottomLeft.X;
            y = point.Y - Bottom + scaled.BottomLeft.Y;
            radiusX = scaled.BottomLeft.X;
            radiusY = scaled.BottomLeft.Y;
        }
        else
        {
            return true; // inside and not within the rounded corner area
        }

        x = x / radiusX;
        y = y / radiusY;
        // check if the point is outside the unit circle
        if ((x * x) + (y * y) > 1.0)
        {
            return false;
        }

        return true;
    }

    internal RRect NormalizeForPaint() => new(_geometry.NormalizeForPaint());

    /// Builds the closed outline of this rounded rectangle, corner arcs included.
    public Path ToPath()
    {
        RRect scaled = new(_geometry.NormalizeForPaint());
        var path = new Path();
        path.MoveTo(scaled.Left + scaled.TopLeft.X, scaled.Top);
        path.LineTo(scaled.Right - scaled.TopRight.X, scaled.Top);
        AddCorner(path, scaled.TopRightCorner(), -Math.PI / 2.0);
        path.LineTo(scaled.Right, scaled.Bottom - scaled.BottomRight.Y);
        AddCorner(path, scaled.BottomRightCorner(), 0.0);
        path.LineTo(scaled.Left + scaled.BottomLeft.X, scaled.Bottom);
        AddCorner(path, scaled.BottomLeftCorner(), Math.PI / 2.0);
        path.LineTo(scaled.Left, scaled.Top + scaled.TopLeft.Y);
        AddCorner(path, scaled.TopLeftCorner(), Math.PI);
        path.Close();
        return path;
    }

    internal Rect TopLeftCorner() => new(Left, Top, TopLeft.X * 2.0, TopLeft.Y * 2.0);

    internal Rect TopRightCorner() =>
        new(Right - (TopRight.X * 2.0), Top, TopRight.X * 2.0, TopRight.Y * 2.0);

    internal Rect BottomRightCorner() => new(
        Right - (BottomRight.X * 2.0),
        Bottom - (BottomRight.Y * 2.0),
        BottomRight.X * 2.0,
        BottomRight.Y * 2.0);

    internal Rect BottomLeftCorner() =>
        new(Left, Bottom - (BottomLeft.Y * 2.0), BottomLeft.X * 2.0, BottomLeft.Y * 2.0);

    private static void AddCorner(Path path, Rect corner, double startAngle)
    {
        if (corner.Width <= 0.0 || corner.Height <= 0.0)
        {
            return;
        }

        path.ArcTo(corner, startAngle, Math.PI / 2.0, forceMoveTo: false);
    }

}
