using Avalonia;
using Plumix.Rendering;

namespace Plumix.UI;

// Dart parity source: dart:ui RRect (control-port subset used by input borders)

public readonly record struct RRect
{
    public RRect(
        Rect rect,
        Radius topLeft,
        Radius topRight,
        Radius bottomRight,
        Radius bottomLeft)
    {
        Rect = rect;
        TopLeft = topLeft;
        TopRight = topRight;
        BottomRight = bottomRight;
        BottomLeft = bottomLeft;
    }

    public Rect Rect { get; }

    public Radius TopLeft { get; }

    public Radius TopRight { get; }

    public Radius BottomRight { get; }

    public Radius BottomLeft { get; }

    public double Left => Rect.Left;

    public double Top => Rect.Top;

    public double Right => Rect.Right;

    public double Bottom => Rect.Bottom;

    public double Width => Rect.Width;

    public double Height => Rect.Height;

    public BorderRadius Radii => new(TopLeft, TopRight, BottomRight, BottomLeft);

    public static RRect FromRectAndRadius(Rect rect, Radius radius) =>
        new(rect, radius, radius, radius, radius);

    public static RRect FromRectAndCorners(Rect rect, BorderRadius borderRadius) => new(
        rect,
        borderRadius.TopLeftRadius,
        borderRadius.TopRightRadius,
        borderRadius.BottomRightRadius,
        borderRadius.BottomLeftRadius);

    public double ShortestSide => Math.Min(Math.Abs(Width), Math.Abs(Height));

    public Point Center => Rect.Center;

    public static RRect FromRectAndRadius(Rect rect, double radius) =>
        FromRectAndRadius(rect, Plumix.Rendering.Radius.Circular(radius));

    // Dart parity source: dart:ui RRect.fromLTRBXY. The rect is stored sorted: dart:ui keeps the
    // coordinates as given, but Skia sorts an unsorted rect at draw time, so this is render-equivalent.
    public static RRect FromLTRBXY(
        double left,
        double top,
        double right,
        double bottom,
        double radiusX,
        double radiusY)
    {
        var radius = Plumix.Rendering.Radius.Elliptical(radiusX, radiusY);
        var rect = new Rect(
            Math.Min(left, right),
            Math.Min(top, bottom),
            Math.Abs(right - left),
            Math.Abs(bottom - top));
        return new RRect(rect, radius, radius, radius, radius);
    }

    /// Moves each edge out by the matching inset and grows every radius by the same amounts.
    // Dart parity source: flutter/packages/flutter/lib/src/painting/edge_insets.dart EdgeInsets.inflateRRect.
    public RRect InflateEdges(Thickness insets) => new(
        new Rect(
            Rect.Left - insets.Left,
            Rect.Top - insets.Top,
            Rect.Width + insets.Left + insets.Right,
            Rect.Height + insets.Top + insets.Bottom),
        Plumix.Rendering.Radius.Elliptical(TopLeft.X + insets.Left, TopLeft.Y + insets.Top),
        Plumix.Rendering.Radius.Elliptical(TopRight.X + insets.Right, TopRight.Y + insets.Top),
        Plumix.Rendering.Radius.Elliptical(BottomRight.X + insets.Right, BottomRight.Y + insets.Bottom),
        Plumix.Rendering.Radius.Elliptical(BottomLeft.X + insets.Left, BottomLeft.Y + insets.Bottom));

    /// Moves each edge in by the matching inset and shrinks every radius by the same amounts.
    // Dart parity source: flutter/packages/flutter/lib/src/painting/edge_insets.dart EdgeInsets.deflateRRect.
    public RRect DeflateEdges(Thickness insets) =>
        InflateEdges(new Thickness(-insets.Left, -insets.Top, -insets.Right, -insets.Bottom));

    public RRect Inflate(double delta) => new(
        new Rect(Rect.Left - delta, Rect.Top - delta, Rect.Width + (delta * 2.0), Rect.Height + (delta * 2.0)),
        InflateRadius(TopLeft, delta),
        InflateRadius(TopRight, delta),
        InflateRadius(BottomRight, delta),
        InflateRadius(BottomLeft, delta));

    public RRect Deflate(double delta) => Inflate(-delta);

    /// Proportionally shrinks every radius when the radii on a side exceed that side's length.
    public RRect ScaleRadii()
    {
        double scale = 1.0;
        scale = MinScale(scale, BottomLeft.Y + TopLeft.Y, Height);
        scale = MinScale(scale, TopLeft.X + TopRight.X, Width);
        scale = MinScale(scale, TopRight.Y + BottomRight.Y, Height);
        scale = MinScale(scale, BottomRight.X + BottomLeft.X, Width);

        if (scale >= 1.0)
        {
            return this;
        }

        return new RRect(
            Rect,
            Radius.Elliptical(TopLeft.X * scale, TopLeft.Y * scale),
            Radius.Elliptical(TopRight.X * scale, TopRight.Y * scale),
            Radius.Elliptical(BottomRight.X * scale, BottomRight.Y * scale),
            Radius.Elliptical(BottomLeft.X * scale, BottomLeft.Y * scale));
    }

    /// Builds the closed outline of this rounded rectangle, corner arcs included.
    public Path ToPath()
    {
        RRect scaled = ScaleRadii();
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

    private static Radius InflateRadius(Radius radius, double delta) =>
        Radius.Elliptical(Math.Max(0.0, radius.X + delta), Math.Max(0.0, radius.Y + delta));

    private static double MinScale(double scale, double sum, double limit)
    {
        if (sum <= 0.0 || limit <= 0.0)
        {
            return scale;
        }

        return Math.Min(scale, limit / sum);
    }
}
