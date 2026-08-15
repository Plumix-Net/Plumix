using Avalonia;
using Plumix.UI;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/edge_insets.dart

public readonly record struct EdgeInsetsGeometry
{
    private EdgeInsetsGeometry(
        double left,
        double top,
        double right,
        double bottom,
        double start,
        double end)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        Start = start;
        End = end;
    }

    public double Left { get; }

    public double Top { get; }

    public double Right { get; }

    public double Bottom { get; }

    public double Start { get; }

    public double End { get; }

    public double Horizontal => Left + Right + Start + End;

    public double Vertical => Top + Bottom;

    public static EdgeInsetsGeometry Zero => default;

    public static EdgeInsetsGeometry All(double value)
    {
        return FromLTRB(value, value, value, value);
    }

    public static EdgeInsetsGeometry Symmetric(double horizontal = 0.0, double vertical = 0.0)
    {
        return FromLTRB(horizontal, vertical, horizontal, vertical);
    }

    public static EdgeInsetsGeometry Only(
        double left = 0.0,
        double top = 0.0,
        double right = 0.0,
        double bottom = 0.0)
    {
        return FromLTRB(left, top, right, bottom);
    }

    public static EdgeInsetsGeometry FromLTRB(double left, double top, double right, double bottom)
    {
        return new EdgeInsetsGeometry(left, top, right, bottom, 0.0, 0.0);
    }

    public static EdgeInsetsGeometry DirectionalOnly(
        double start = 0.0,
        double top = 0.0,
        double end = 0.0,
        double bottom = 0.0)
    {
        return new EdgeInsetsGeometry(0.0, top, 0.0, bottom, start, end);
    }

    /// <summary>Dart's `EdgeInsetsDirectional.symmetric`.</summary>
    public static EdgeInsetsGeometry DirectionalSymmetric(
        double horizontal = 0.0,
        double vertical = 0.0)
    {
        return new EdgeInsetsGeometry(0.0, vertical, 0.0, vertical, horizontal, horizontal);
    }

    public Thickness Resolve(TextDirection direction)
    {
        return direction == TextDirection.Ltr
            ? new Thickness(Left + Start, Top, Right + End, Bottom)
            : new Thickness(Left + End, Top, Right + Start, Bottom);
    }

    public EdgeInsetsGeometry Add(EdgeInsetsGeometry other)
    {
        return new EdgeInsetsGeometry(
            left: Left + other.Left,
            top: Top + other.Top,
            right: Right + other.Right,
            bottom: Bottom + other.Bottom,
            start: Start + other.Start,
            end: End + other.End);
    }

    public static EdgeInsetsGeometry operator +(EdgeInsetsGeometry a, EdgeInsetsGeometry b)
    {
        return a.Add(b);
    }

    /// <summary>Insets with every edge set to positive infinity.</summary>
    public static EdgeInsetsGeometry Infinity => new(
        double.PositiveInfinity,
        double.PositiveInfinity,
        double.PositiveInfinity,
        double.PositiveInfinity,
        double.PositiveInfinity,
        double.PositiveInfinity);

    /// <summary>Clamps every edge of these insets between the matching edges of the bounds.</summary>
    public EdgeInsetsGeometry Clamp(EdgeInsetsGeometry min, EdgeInsetsGeometry max)
    {
        return new EdgeInsetsGeometry(
            left: Math.Clamp(Left, min.Left, max.Left),
            top: Math.Clamp(Top, min.Top, max.Top),
            right: Math.Clamp(Right, min.Right, max.Right),
            bottom: Math.Clamp(Bottom, min.Bottom, max.Bottom),
            start: Math.Clamp(Start, min.Start, max.Start),
            end: Math.Clamp(End, min.End, max.End));
    }

    public static EdgeInsetsGeometry? Lerp(
        EdgeInsetsGeometry? a,
        EdgeInsetsGeometry? b,
        double t)
    {
        if (a == b)
        {
            return a;
        }

        EdgeInsetsGeometry from = a ?? default;
        EdgeInsetsGeometry to = b ?? default;
        return new EdgeInsetsGeometry(
            left: LerpDouble(from.Left, to.Left, t),
            top: LerpDouble(from.Top, to.Top, t),
            right: LerpDouble(from.Right, to.Right, t),
            bottom: LerpDouble(from.Bottom, to.Bottom, t),
            start: LerpDouble(from.Start, to.Start, t),
            end: LerpDouble(from.End, to.End, t));
    }

    public static implicit operator EdgeInsetsGeometry(Thickness value)
    {
        return FromLTRB(value.Left, value.Top, value.Right, value.Bottom);
    }

    public static implicit operator EdgeInsetsGeometry(EdgeInsets value)
    {
        return FromLTRB(value.Left, value.Top, value.Right, value.Bottom);
    }

    public static implicit operator EdgeInsetsGeometry(EdgeInsetsDirectional value)
    {
        return DirectionalOnly(value.Start, value.Top, value.End, value.Bottom);
    }

    private static double LerpDouble(double a, double b, double t)
    {
        return a + ((b - a) * t);
    }
}

/// <summary>Dart's `EdgeInsets` helpers that operate on already resolved insets.</summary>
public static class ResolvedEdgeInsetsExtensions
{
    /// <summary>
    /// Dart's `EdgeInsets.inflateRect`: a rectangle with the edges of <paramref name="rect"/> moved
    /// outwards by the corresponding edge of <paramref name="insets"/>.
    /// </summary>
    public static Rect InflateRect(this Thickness insets, Rect rect)
    {
        double left = rect.Left - insets.Left;
        double top = rect.Top - insets.Top;
        double right = rect.Right + insets.Right;
        double bottom = rect.Bottom + insets.Bottom;
        return new Rect(left, top, Math.Max(0.0, right - left), Math.Max(0.0, bottom - top));
    }
}

public readonly record struct EdgeInsets(double Left, double Top, double Right, double Bottom)
{
    public static EdgeInsets Zero => default;

    public static EdgeInsets All(double value)
    {
        return new EdgeInsets(value, value, value, value);
    }

    public static EdgeInsets Symmetric(double horizontal = 0.0, double vertical = 0.0)
    {
        return new EdgeInsets(horizontal, vertical, horizontal, vertical);
    }

    public static EdgeInsets Only(
        double left = 0.0,
        double top = 0.0,
        double right = 0.0,
        double bottom = 0.0)
    {
        return new EdgeInsets(left, top, right, bottom);
    }

    public Thickness Resolve(TextDirection direction)
    {
        return new Thickness(Left, Top, Right, Bottom);
    }
}

public readonly record struct EdgeInsetsDirectional(double Start, double Top, double End, double Bottom)
{
    public static EdgeInsetsDirectional Zero => default;

    public static EdgeInsetsDirectional All(double value)
    {
        return new EdgeInsetsDirectional(value, value, value, value);
    }

    public static EdgeInsetsDirectional Only(
        double start = 0.0,
        double top = 0.0,
        double end = 0.0,
        double bottom = 0.0)
    {
        return new EdgeInsetsDirectional(start, top, end, bottom);
    }

    public Thickness Resolve(TextDirection direction)
    {
        return direction == TextDirection.Ltr
            ? new Thickness(Start, Top, End, Bottom)
            : new Thickness(End, Top, Start, Bottom);
    }
}
