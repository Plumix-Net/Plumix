using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/geometry.dart (_RRectLike)
// C# structs cannot inherit the private generic base; both public shapes delegate to this value.
internal readonly record struct RoundRectGeometry
{
    public RoundRectGeometry(
        double left, double top, double right, double bottom,
        Radius topLeft, Radius topRight, Radius bottomRight, Radius bottomLeft)
    {
        DebugAssertions.Assert(topLeft.X >= 0 && topLeft.Y >= 0);
        DebugAssertions.Assert(topRight.X >= 0 && topRight.Y >= 0);
        DebugAssertions.Assert(bottomRight.X >= 0 && bottomRight.Y >= 0);
        DebugAssertions.Assert(bottomLeft.X >= 0 && bottomLeft.Y >= 0);
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        TopLeft = topLeft;
        TopRight = topRight;
        BottomRight = bottomRight;
        BottomLeft = bottomLeft;
    }

    public double Left { get; }
    public double Top { get; }
    public double Right { get; }
    public double Bottom { get; }
    public Radius TopLeft { get; }
    public Radius TopRight { get; }
    public Radius BottomRight { get; }
    public Radius BottomLeft { get; }
    public double Width => Right - Left;
    public double Height => Bottom - Top;
    public Rect OuterRect => DartGeometry.RectFromLTRB(Left, Top, Right, Bottom);
    public Point Center => new(Left + Width / 2.0, Top + Height / 2.0);
    public double ShortestSide => Math.Min(Math.Abs(Width), Math.Abs(Height));
    public double LongestSide => Math.Max(Math.Abs(Width), Math.Abs(Height));
    public bool IsEmpty => Left >= Right || Top >= Bottom;
    public bool IsFinite => double.IsFinite(Left) && double.IsFinite(Top)
                            && double.IsFinite(Right) && double.IsFinite(Bottom);
    public bool IsRect => (TopLeft.X == 0 || TopLeft.Y == 0) && (TopRight.X == 0 || TopRight.Y == 0)
                          && (BottomRight.X == 0 || BottomRight.Y == 0) && (BottomLeft.X == 0 || BottomLeft.Y == 0);
    private bool Uniform => TopLeft == TopRight && TopRight == BottomRight && BottomRight == BottomLeft;
    public bool IsStadium => Uniform && (Width <= 2 * TopLeft.X || Height <= 2 * TopLeft.Y);
    public bool IsEllipse => Uniform && Width <= 2 * TopLeft.X && Height <= 2 * TopLeft.Y;
    public bool IsCircle => Width == Height && IsEllipse;
    public bool HasNaN => double.IsNaN(Left) || double.IsNaN(Top) || double.IsNaN(Right) || double.IsNaN(Bottom)
                          || HasNaNRadius(TopLeft) || HasNaNRadius(TopRight)
                          || HasNaNRadius(BottomRight) || HasNaNRadius(BottomLeft);

    public Rect SafeInnerRect => InnerRect(0.29289321881);
    public Rect MiddleRect => InnerRect(1.0);
    public Rect WideMiddleRect => DartGeometry.RectFromLTRB(
        Left, Top + Math.Max(TopLeft.Y, TopRight.Y), Right, Bottom - Math.Max(BottomRight.Y, BottomLeft.Y));
    public Rect TallMiddleRect => DartGeometry.RectFromLTRB(
        Left + Math.Max(BottomLeft.X, TopLeft.X), Top, Right - Math.Max(TopRight.X, BottomRight.X), Bottom);

    private Rect InnerRect(double factor) => DartGeometry.RectFromLTRB(
        Left + Math.Max(BottomLeft.X, TopLeft.X) * factor,
        Top + Math.Max(TopLeft.Y, TopRight.Y) * factor,
        Right - Math.Max(TopRight.X, BottomRight.X) * factor,
        Bottom - Math.Max(BottomRight.Y, BottomLeft.Y) * factor);

    public RoundRectGeometry Shift(Point offset) => new(
        Left + offset.X, Top + offset.Y, Right + offset.X, Bottom + offset.Y,
        TopLeft, TopRight, BottomRight, BottomLeft);

    public RoundRectGeometry Inflate(double delta) => new(
        Left - delta, Top - delta, Right + delta, Bottom + delta,
        InflateRadius(TopLeft, delta), InflateRadius(TopRight, delta),
        InflateRadius(BottomRight, delta), InflateRadius(BottomLeft, delta));

    // Drawing backends sort bounds and normalize flat/nonfinite radii before constructing a contour.
    // This intentionally differs from the public Dart scaleRadii method and native contains path.
    internal RoundRectGeometry NormalizeForPaint()
    {
        if (!IsFinite) { return default; }
        bool finiteRadii = double.IsFinite(TopLeft.X) && double.IsFinite(TopLeft.Y)
            && double.IsFinite(TopRight.X) && double.IsFinite(TopRight.Y)
            && double.IsFinite(BottomRight.X) && double.IsFinite(BottomRight.Y)
            && double.IsFinite(BottomLeft.X) && double.IsFinite(BottomLeft.Y);
        return new RoundRectGeometry(Math.Min(Left, Right), Math.Min(Top, Bottom),
            Math.Max(Left, Right), Math.Max(Top, Bottom),
            finiteRadii ? NormalizeRadius(TopLeft) : default, finiteRadii ? NormalizeRadius(TopRight) : default,
            finiteRadii ? NormalizeRadius(BottomRight) : default, finiteRadii ? NormalizeRadius(BottomLeft) : default)
            .ScaleRadii();
    }

    private static Radius NormalizeRadius(Radius radius) => radius.X <= 0 || radius.Y <= 0 ? default : radius;

    public RoundRectGeometry ScaleRadii()
    {
        double scale = 1.0;
        scale = MinScale(scale, BottomLeft.Y + TopLeft.Y, Height);
        scale = MinScale(scale, TopLeft.X + TopRight.X, Width);
        scale = MinScale(scale, TopRight.Y + BottomRight.Y, Height);
        scale = MinScale(scale, BottomRight.X + BottomLeft.X, Width);
        DebugAssertions.Assert(scale >= 0);
        return scale < 1.0
            ? new(Left, Top, Right, Bottom, TopLeft * scale, TopRight * scale, BottomRight * scale, BottomLeft * scale)
            : this;
    }

    public RoundRectGeometry LerpTo(RoundRectGeometry? b, double t)
    {
        if (b is not { } end)
        {
            double k = 1.0 - t;
            return new(Left * k, Top * k, Right * k, Bottom * k,
                ClampRadius(TopLeft * k), ClampRadius(TopRight * k),
                ClampRadius(BottomRight * k), ClampRadius(BottomLeft * k));
        }

        return new(
            DartGeometry.LerpDouble(Left, end.Left, t), DartGeometry.LerpDouble(Top, end.Top, t),
            DartGeometry.LerpDouble(Right, end.Right, t), DartGeometry.LerpDouble(Bottom, end.Bottom, t),
            ClampRadius(Radius.Lerp(TopLeft, end.TopLeft, t)), ClampRadius(Radius.Lerp(TopRight, end.TopRight, t)),
            ClampRadius(Radius.Lerp(BottomRight, end.BottomRight, t)),
            ClampRadius(Radius.Lerp(BottomLeft, end.BottomLeft, t)));
    }

    public bool Equals(RoundRectGeometry other) => Left == other.Left && Top == other.Top
        && Right == other.Right && Bottom == other.Bottom && TopLeft == other.TopLeft
        && TopRight == other.TopRight && BottomRight == other.BottomRight && BottomLeft == other.BottomLeft;
    public override int GetHashCode() =>
        HashCode.Combine(Left, Top, Right, Bottom, TopLeft, TopRight, BottomRight, BottomLeft);

    public string Format(string className)
    {
        string rect = $"{DartGeometry.Fixed(Left)}, {DartGeometry.Fixed(Top)}, "
                      + $"{DartGeometry.Fixed(Right)}, {DartGeometry.Fixed(Bottom)}";
        if (Uniform)
        {
            return TopLeft.X == TopLeft.Y
                ? $"{className}.fromLTRBR({rect}, {DartGeometry.Fixed(TopLeft.X)})"
                : $"{className}.fromLTRBXY({rect}, {DartGeometry.Fixed(TopLeft.X)}, {DartGeometry.Fixed(TopLeft.Y)})";
        }

        return $"{className}.fromLTRBAndCorners({rect}, topLeft: {TopLeft}, topRight: {TopRight}, "
               + $"bottomRight: {BottomRight}, bottomLeft: {BottomLeft})";
    }

    private static bool HasNaNRadius(Radius radius) => double.IsNaN(radius.X) || double.IsNaN(radius.Y);
    private static Radius ClampRadius(Radius radius) => new(Math.Max(0, radius.X), Math.Max(0, radius.Y));
    private static Radius InflateRadius(Radius radius, double delta) =>
        ClampRadius(new(radius.X + delta, radius.Y + delta));
    private static double MinScale(double scale, double sum, double limit) =>
        sum > limit && sum != 0.0 ? Math.Min(scale, limit / sum) : scale;
}
