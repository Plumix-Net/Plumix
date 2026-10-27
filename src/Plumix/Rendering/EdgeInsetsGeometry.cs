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

    public EdgeInsets Resolve(TextDirection direction)
    {
        return direction == TextDirection.Ltr
            ? new EdgeInsets(Left + Start, Top, Right + End, Bottom)
            : new EdgeInsets(Left + End, Top, Right + Start, Bottom);
    }

    /// <summary>
    /// Whether resolving these insets needs a text direction, mirroring the assert Dart's
    /// `EdgeInsetsDirectional.resolve` and `_MixedEdgeInsets.resolve` share.
    /// </summary>
    public bool RequiresTextDirection => Start != 0.0 || End != 0.0;

    /// <summary>Whether every dimension is non-negative. Dart's `EdgeInsetsGeometry.isNonNegative`.</summary>
    public bool IsNonNegative =>
        Left >= 0.0 && Top >= 0.0 && Right >= 0.0 && Bottom >= 0.0 && Start >= 0.0 && End >= 0.0;

    /// <summary>
    /// Resolves against an optional direction: purely physical insets ignore it, while directional
    /// (or mixed) ones require it. Dart's `EdgeInsetsGeometry.resolve(TextDirection?)`.
    /// </summary>
    public EdgeInsets Resolve(TextDirection? direction)
    {
        if (direction is { } value)
        {
            return Resolve(value);
        }

        if (RequiresTextDirection)
        {
            throw new ArgumentNullException(
                nameof(direction),
                "Directional insets cannot be resolved without a text direction.");
        }

        return new EdgeInsets(Left, Top, Right, Bottom);
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

    /// <summary>Returns the difference between two insets. Dart's <c>EdgeInsetsGeometry.subtract</c>.</summary>
    public EdgeInsetsGeometry Subtract(EdgeInsetsGeometry other)
    {
        return new EdgeInsetsGeometry(
            left: Left - other.Left,
            top: Top - other.Top,
            right: Right - other.Right,
            bottom: Bottom - other.Bottom,
            start: Start - other.Start,
            end: End - other.End);
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

    /// <summary>Dart's <c>EdgeInsetsGeometry.toString</c>.</summary>
    public override string ToString()
    {
        if (Start == 0.0 && End == 0.0)
        {
            if (Left == 0.0 && Right == 0.0 && Top == 0.0 && Bottom == 0.0)
            {
                return "EdgeInsets.zero";
            }

            if (Left == Right && Right == Top && Top == Bottom)
            {
                return $"EdgeInsets.all({Fixed1(Left)})";
            }

            return $"EdgeInsets({Fixed1(Left)}, {Fixed1(Top)}, {Fixed1(Right)}, {Fixed1(Bottom)})";
        }

        if (Left == 0.0 && Right == 0.0)
        {
            return $"EdgeInsetsDirectional({Fixed1(Start)}, {Fixed1(Top)}, {Fixed1(End)}, {Fixed1(Bottom)})";
        }

        return $"EdgeInsets({Fixed1(Left)}, {Fixed1(Top)}, {Fixed1(Right)}, {Fixed1(Bottom)})"
               + $" + EdgeInsetsDirectional({Fixed1(Start)}, 0.0, {Fixed1(End)}, 0.0)";
    }

    private static string Fixed1(double value) =>
        value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

    private static double LerpDouble(double a, double b, double t)
    {
        return a + ((b - a) * t);
    }
}

/// <summary>
/// An immutable set of offsets in each of the four cardinal directions.
/// </summary>
/// <remarks>
/// Dart's <c>EdgeInsets</c>. Dart's <c>operator ~/</c> is <see cref="TruncatingDivide"/>, since C#
/// cannot declare it. Avalonia's <see cref="Thickness"/> converts implicitly to this type, so host code
/// can pass it where the framework takes insets; <see cref="ToThickness"/> converts back at the
/// Avalonia boundary.
/// </remarks>
public readonly record struct EdgeInsets(double Left, double Top, double Right, double Bottom)
{
    /// <summary>An <see cref="EdgeInsets"/> with zero offsets in each direction.</summary>
    public static EdgeInsets Zero => default;

    /// <summary>Creates insets from offsets from the left, top, right, and bottom.</summary>
    public static EdgeInsets FromLTRB(double left, double top, double right, double bottom)
    {
        return new EdgeInsets(left, top, right, bottom);
    }

    /// <summary>Creates insets where all the offsets are <paramref name="value"/>.</summary>
    public static EdgeInsets All(double value)
    {
        return new EdgeInsets(value, value, value, value);
    }

    /// <summary>Creates insets with symmetrical vertical and horizontal offsets.</summary>
    public static EdgeInsets Symmetric(double horizontal = 0.0, double vertical = 0.0)
    {
        return new EdgeInsets(horizontal, vertical, horizontal, vertical);
    }

    /// <summary>Creates insets with only the given values non-zero.</summary>
    public static EdgeInsets Only(
        double left = 0.0,
        double top = 0.0,
        double right = 0.0,
        double bottom = 0.0)
    {
        return new EdgeInsets(left, top, right, bottom);
    }

    /// <summary>
    /// Creates insets that match the given view padding, converted from physical to logical pixels.
    /// </summary>
    public static EdgeInsets FromViewPadding(ViewPadding padding, double devicePixelRatio)
    {
        return new EdgeInsets(
            padding.Left / devicePixelRatio,
            padding.Top / devicePixelRatio,
            padding.Right / devicePixelRatio,
            padding.Bottom / devicePixelRatio);
    }

    /// <summary>The total offset in the horizontal direction (Dart's <c>EdgeInsetsGeometry.horizontal</c>).</summary>
    public double Horizontal => Left + Right;

    /// <summary>The total offset in the vertical direction (Dart's <c>EdgeInsetsGeometry.vertical</c>).</summary>
    public double Vertical => Top + Bottom;

    /// <summary>Whether every dimension is non-negative.</summary>
    public bool IsNonNegative => Left >= 0.0 && Right >= 0.0 && Top >= 0.0 && Bottom >= 0.0;

    /// <summary>The total offset in the given direction.</summary>
    public double Along(Axis axis)
    {
        return axis switch
        {
            Axis.Horizontal => Horizontal,
            _ => Vertical,
        };
    }

    /// <summary>The size that these insets would occupy with an empty interior.</summary>
    public Size CollapsedSize => new(Horizontal, Vertical);

    /// <summary>An offset describing the vector from the top left of a rectangle to its inset top left.</summary>
    public Point TopLeft => new(Left, Top);

    /// <summary>An offset describing the vector from the top right of a rectangle to its inset top right.</summary>
    public Point TopRight => new(-Right, Top);

    /// <summary>An offset describing the vector from the bottom left of a rectangle to its inset bottom left.</summary>
    public Point BottomLeft => new(Left, -Bottom);

    /// <summary>
    /// An offset describing the vector from the bottom right of a rectangle to its inset bottom right.
    /// </summary>
    public Point BottomRight => new(-Right, -Bottom);

    /// <summary>Insets with top and bottom as well as left and right flipped.</summary>
    public EdgeInsets Flipped => new(Right, Bottom, Left, Top);

    /// <summary>
    /// Returns a new rect that is bigger than the given rect in each direction by the amount of
    /// inset in each direction.
    /// </summary>
    public Rect InflateRect(Rect rect)
    {
        return DartGeometry.RectFromLTRB(
            rect.Left - Left,
            rect.Top - Top,
            rect.Right + Right,
            rect.Bottom + Bottom);
    }

    /// <summary>
    /// Returns a new rect that is smaller than the given rect in each direction by the amount of
    /// inset in each direction. If the rect is smaller than the insets, the result has a negative size.
    /// </summary>
    public Rect DeflateRect(Rect rect)
    {
        return DartGeometry.RectFromLTRB(
            rect.Left + Left,
            rect.Top + Top,
            rect.Right - Right,
            rect.Bottom - Bottom);
    }

    /// <summary>
    /// Returns a new size that is bigger than the given size by the amount of inset in the
    /// horizontal and vertical directions.
    /// </summary>
    public Size InflateSize(Size size)
    {
        return new Size(size.Width + Horizontal, size.Height + Vertical);
    }

    /// <summary>
    /// Returns a new size that is smaller than the given size by the amount of inset in the
    /// horizontal and vertical directions; it has negative dimensions when the size is smaller than
    /// <see cref="CollapsedSize"/>.
    /// </summary>
    public Size DeflateSize(Size size)
    {
        return new Size(size.Width - Horizontal, size.Height - Vertical);
    }

    /// <summary>
    /// Returns a new <see cref="RRect"/> that is bigger than the given one in each direction by the
    /// amount of inset in each direction; each corner radius grows by the adjacent insets.
    /// </summary>
    public RRect InflateRRect(RRect rect)
    {
        return RRect.FromLTRBAndCorners(
            rect.Left - Left,
            rect.Top - Top,
            rect.Right + Right,
            rect.Bottom + Bottom,
            (rect.TopLeft + new Radius(Left, Top)).Clamp(minimum: Radius.Zero),
            (rect.TopRight + new Radius(Right, Top)).Clamp(minimum: Radius.Zero),
            (rect.BottomRight + new Radius(Right, Bottom)).Clamp(minimum: Radius.Zero),
            (rect.BottomLeft + new Radius(Left, Bottom)).Clamp(minimum: Radius.Zero));
    }

    /// <summary>
    /// Returns a new <see cref="RRect"/> that is smaller than the given one in each direction by the
    /// amount of inset in each direction; each corner radius shrinks by the adjacent insets, never
    /// below zero.
    /// </summary>
    public RRect DeflateRRect(RRect rect)
    {
        return RRect.FromLTRBAndCorners(
            rect.Left + Left,
            rect.Top + Top,
            rect.Right - Right,
            rect.Bottom - Bottom,
            (rect.TopLeft - new Radius(Left, Top)).Clamp(minimum: Radius.Zero),
            (rect.TopRight - new Radius(Right, Top)).Clamp(minimum: Radius.Zero),
            (rect.BottomRight - new Radius(Right, Bottom)).Clamp(minimum: Radius.Zero),
            (rect.BottomLeft - new Radius(Left, Bottom)).Clamp(minimum: Radius.Zero));
    }

    /// <summary>Returns the difference between these insets and <paramref name="other"/>.</summary>
    public EdgeInsetsGeometry Subtract(EdgeInsetsGeometry other)
    {
        return ((EdgeInsetsGeometry)this).Subtract(other);
    }

    /// <summary>Returns the sum of these insets and <paramref name="other"/>.</summary>
    public EdgeInsetsGeometry Add(EdgeInsetsGeometry other)
    {
        return ((EdgeInsetsGeometry)this).Add(other);
    }

    /// <summary>
    /// Returns new insets whose every edge is clamped between the matching edges of
    /// <paramref name="min"/> and <paramref name="max"/>.
    /// </summary>
    public EdgeInsetsGeometry Clamp(EdgeInsetsGeometry min, EdgeInsetsGeometry max)
    {
        return FromLTRB(
            Math.Clamp(Left, min.Left, max.Left),
            Math.Clamp(Top, min.Top, max.Top),
            Math.Clamp(Right, min.Right, max.Right),
            Math.Clamp(Bottom, min.Bottom, max.Bottom));
    }

    /// <summary>Returns the difference between two <see cref="EdgeInsets"/>.</summary>
    public static EdgeInsets operator -(EdgeInsets a, EdgeInsets b)
    {
        return new EdgeInsets(a.Left - b.Left, a.Top - b.Top, a.Right - b.Right, a.Bottom - b.Bottom);
    }

    /// <summary>Returns the sum of two <see cref="EdgeInsets"/>.</summary>
    public static EdgeInsets operator +(EdgeInsets a, EdgeInsets b)
    {
        return new EdgeInsets(a.Left + b.Left, a.Top + b.Top, a.Right + b.Right, a.Bottom + b.Bottom);
    }

    /// <summary>Returns the insets negated: each edge is the negation of the original.</summary>
    public static EdgeInsets operator -(EdgeInsets value)
    {
        return new EdgeInsets(-value.Left, -value.Top, -value.Right, -value.Bottom);
    }

    /// <summary>Scales the insets in each dimension by the given factor.</summary>
    public static EdgeInsets operator *(EdgeInsets value, double factor)
    {
        return new EdgeInsets(
            value.Left * factor,
            value.Top * factor,
            value.Right * factor,
            value.Bottom * factor);
    }

    /// <summary>Divides the insets in each dimension by the given factor.</summary>
    public static EdgeInsets operator /(EdgeInsets value, double factor)
    {
        return new EdgeInsets(
            value.Left / factor,
            value.Top / factor,
            value.Right / factor,
            value.Bottom / factor);
    }

    /// <summary>Computes the remainder of each dimension of the insets by the given value.</summary>
    /// <remarks>Dart's <c>%</c> is Euclidean: the result is never negative.</remarks>
    public static EdgeInsets operator %(EdgeInsets value, double divisor)
    {
        return new EdgeInsets(
            DartGeometry.Modulo(value.Left, divisor),
            DartGeometry.Modulo(value.Top, divisor),
            DartGeometry.Modulo(value.Right, divisor),
            DartGeometry.Modulo(value.Bottom, divisor));
    }

    /// <summary>
    /// Integer-divides the insets in each dimension by the given factor, truncating toward zero.
    /// </summary>
    /// <remarks>Dart's <c>operator ~/</c>, which C# cannot declare.</remarks>
    public EdgeInsets TruncatingDivide(double factor)
    {
        return new EdgeInsets(
            DartGeometry.TruncateQuotient(Left, factor),
            DartGeometry.TruncateQuotient(Top, factor),
            DartGeometry.TruncateQuotient(Right, factor),
            DartGeometry.TruncateQuotient(Bottom, factor));
    }

    /// <summary>Linearly interpolate between two <see cref="EdgeInsets"/>. Dart's <c>EdgeInsets.lerp</c>.</summary>
    /// <remarks>If either is null, this function interpolates from <see cref="Zero"/>.</remarks>
    public static EdgeInsets? Lerp(EdgeInsets? a, EdgeInsets? b, double t)
    {
        if (a == b)
        {
            return a;
        }

        if (a is not { } from)
        {
            return b!.Value * t;
        }

        if (b is not { } target)
        {
            return from * (1.0 - t);
        }

        return new EdgeInsets(
            LerpDouble(from.Left, target.Left, t),
            LerpDouble(from.Top, target.Top, t),
            LerpDouble(from.Right, target.Right, t),
            LerpDouble(from.Bottom, target.Bottom, t));
    }

    /// <summary>
    /// Convert these insets to physical insets: <see cref="EdgeInsets"/> are already physical, so this
    /// returns them unchanged.
    /// </summary>
    public EdgeInsets Resolve(TextDirection? direction) => this;

    /// <summary>Creates a copy of these insets but with the given fields replaced with the new values.</summary>
    public EdgeInsets CopyWith(
        double? left = null,
        double? top = null,
        double? right = null,
        double? bottom = null)
    {
        return new EdgeInsets(left ?? Left, top ?? Top, right ?? Right, bottom ?? Bottom);
    }

    /// <summary>Dart's <c>EdgeInsetsGeometry.toString</c>.</summary>
    public override string ToString() => ((EdgeInsetsGeometry)this).ToString();

    /// <summary>Converts Avalonia insets to framework insets.</summary>
    public static implicit operator EdgeInsets(Thickness value)
    {
        return new EdgeInsets(value.Left, value.Top, value.Right, value.Bottom);
    }

    /// <summary>Converts these insets to Avalonia's <see cref="Thickness"/>, for host code.</summary>
    public Thickness ToThickness() => new(Left, Top, Right, Bottom);

    // dart:ui `lerpDouble`.
    private static double LerpDouble(double a, double b, double t) => (a * (1.0 - t)) + (b * t);
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

    public EdgeInsets Resolve(TextDirection direction)
    {
        return direction == TextDirection.Ltr
            ? new EdgeInsets(Start, Top, End, Bottom)
            : new EdgeInsets(End, Top, Start, Bottom);
    }
}
