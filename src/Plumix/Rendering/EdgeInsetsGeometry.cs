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

    public Thickness Resolve(TextDirection direction)
    {
        return direction == TextDirection.Ltr
            ? new Thickness(Left + Start, Top, Right + End, Bottom)
            : new Thickness(Left + End, Top, Right + Start, Bottom);
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
