using System.Globalization;
using Plumix.Foundation;

namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/geometry.dart (OffsetBase, Offset, Size, Rect)
//
// Value types, not classes: layout and paint create them on every frame. `==` compares components
// the way Dart does (a NaN component is unequal to itself); `Equals`/`GetHashCode` are reflexive,
// like `double`'s, so dictionaries and sets keep working.

/// <summary>An immutable 2D floating-point offset (dart:ui <c>Offset</c>).</summary>
public readonly struct Offset : IEquatable<Offset>
{
    public Offset(double dx, double dy)
    {
        Dx = dx;
        Dy = dy;
    }

    public double Dx { get; }
    public double Dy { get; }

    public static Offset Zero => default;
    public static Offset Infinite => new(double.PositiveInfinity, double.PositiveInfinity);

    public static Offset FromDirection(double direction, double distance = 1.0) =>
        new(distance * Math.Cos(direction), distance * Math.Sin(direction));

    public double Distance => Math.Sqrt(Dx * Dx + Dy * Dy);
    public double DistanceSquared => Dx * Dx + Dy * Dy;
    public double Direction => Math.Atan2(Dy, Dx);
    public bool IsInfinite => double.IsPositiveInfinity(Dx) || double.IsPositiveInfinity(Dy);
    public bool IsFinite => double.IsFinite(Dx) && double.IsFinite(Dy);

    public Offset Scale(double scaleX, double scaleY) => new(Dx * scaleX, Dy * scaleY);
    public Offset Translate(double translateX, double translateY) => new(Dx + translateX, Dy + translateY);
    public Offset TruncateDivide(double operand) => new(
        GeometryMath.TruncateQuotient(Dx, operand), GeometryMath.TruncateQuotient(Dy, operand));

    public static Offset operator -(Offset value) => new(-value.Dx, -value.Dy);
    public static Offset operator -(Offset a, Offset b) => new(a.Dx - b.Dx, a.Dy - b.Dy);
    public static Offset operator +(Offset a, Offset b) => new(a.Dx + b.Dx, a.Dy + b.Dy);
    public static Offset operator *(Offset a, double operand) => new(a.Dx * operand, a.Dy * operand);
    public static Offset operator /(Offset a, double operand) => new(a.Dx / operand, a.Dy / operand);
    public static Offset operator %(Offset a, double operand) => new(
        GeometryMath.Modulo(a.Dx, operand), GeometryMath.Modulo(a.Dy, operand));

    /// <summary>A rectangle from this offset (top-left) and <paramref name="other"/> (Dart's <c>&amp;</c>).</summary>
    public static Rect operator &(Offset a, Size other) =>
        Rect.FromLTWH(a.Dx, a.Dy, other.Width, other.Height);

    // OffsetBase comparisons: true only when the relation holds for both components.
    public static bool operator <(Offset a, Offset b) => a.Dx < b.Dx && a.Dy < b.Dy;
    public static bool operator <=(Offset a, Offset b) => a.Dx <= b.Dx && a.Dy <= b.Dy;
    public static bool operator >(Offset a, Offset b) => a.Dx > b.Dx && a.Dy > b.Dy;
    public static bool operator >=(Offset a, Offset b) => a.Dx >= b.Dx && a.Dy >= b.Dy;

    public static bool operator ==(Offset a, Offset b) => a.Dx == b.Dx && a.Dy == b.Dy;
    public static bool operator !=(Offset a, Offset b) => !(a == b);

    public static Offset? Lerp(Offset? a, Offset? b, double t)
    {
        if (b is not { } end)
        {
            return a is { } start ? start * (1.0 - t) : null;
        }

        return a is not { } from
            ? end * t
            : new Offset(GeometryMath.LerpDouble(from.Dx, end.Dx, t), GeometryMath.LerpDouble(from.Dy, end.Dy, t));
    }

    public static Offset Lerp(Offset a, Offset b, double t) =>
        new(GeometryMath.LerpDouble(a.Dx, b.Dx, t), GeometryMath.LerpDouble(a.Dy, b.Dy, t));

    public bool Equals(Offset other) => Dx.Equals(other.Dx) && Dy.Equals(other.Dy);
    public override bool Equals(object? obj) => obj is Offset other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Dx, Dy);
    public override string ToString() => $"Offset({GeometryMath.Fixed(Dx)}, {GeometryMath.Fixed(Dy)})";

    public void Deconstruct(out double dx, out double dy)
    {
        dx = Dx;
        dy = Dy;
    }
}

/// <summary>A 2D floating-point size (dart:ui <c>Size</c>).</summary>
public readonly struct Size : IEquatable<Size>
{
    public Size(double width, double height)
    {
        Width = width;
        Height = height;
    }

    public double Width { get; }
    public double Height { get; }

    public static Size Zero => default;
    public static Size Infinite => new(double.PositiveInfinity, double.PositiveInfinity);

    public static Size Copy(Size source) => source;
    public static Size Square(double dimension) => new(dimension, dimension);
    public static Size FromWidth(double width) => new(width, double.PositiveInfinity);
    public static Size FromHeight(double height) => new(double.PositiveInfinity, height);
    public static Size FromRadius(double radius) => new(radius * 2.0, radius * 2.0);

    public double AspectRatio => Height != 0.0
        ? Width / Height
        : Width > 0.0 ? double.PositiveInfinity : Width < 0.0 ? double.NegativeInfinity : 0.0;

    public bool IsEmpty => Width <= 0.0 || Height <= 0.0;
    public bool IsInfinite => double.IsPositiveInfinity(Width) || double.IsPositiveInfinity(Height);
    public bool IsFinite => double.IsFinite(Width) && double.IsFinite(Height);
    public double ShortestSide => Math.Min(Math.Abs(Width), Math.Abs(Height));
    public double LongestSide => Math.Max(Math.Abs(Width), Math.Abs(Height));
    public Size Flipped => new(Height, Width);

    public Offset TopLeft(Offset origin) => origin;
    public Offset TopCenter(Offset origin) => new(origin.Dx + Width / 2.0, origin.Dy);
    public Offset TopRight(Offset origin) => new(origin.Dx + Width, origin.Dy);
    public Offset CenterLeft(Offset origin) => new(origin.Dx, origin.Dy + Height / 2.0);
    public Offset Center(Offset origin) => new(origin.Dx + Width / 2.0, origin.Dy + Height / 2.0);
    public Offset CenterRight(Offset origin) => new(origin.Dx + Width, origin.Dy + Height / 2.0);
    public Offset BottomLeft(Offset origin) => new(origin.Dx, origin.Dy + Height);
    public Offset BottomCenter(Offset origin) => new(origin.Dx + Width / 2.0, origin.Dy + Height);
    public Offset BottomRight(Offset origin) => new(origin.Dx + Width, origin.Dy + Height);

    /// <summary>Whether <paramref name="offset"/> lies inside, top/left edges included, bottom/right excluded.</summary>
    public bool Contains(Offset offset) =>
        offset.Dx >= 0.0 && offset.Dx < Width && offset.Dy >= 0.0 && offset.Dy < Height;

    public Size TruncateDivide(double operand) => new(
        GeometryMath.TruncateQuotient(Width, operand), GeometryMath.TruncateQuotient(Height, operand));

    // Dart's `Size - OffsetBase`: subtracting a size gives the offset between them, subtracting an
    // offset gives a smaller size.
    public static Offset operator -(Size a, Size b) => new(a.Width - b.Width, a.Height - b.Height);
    public static Size operator -(Size a, Offset b) => new(a.Width - b.Dx, a.Height - b.Dy);
    public static Size operator +(Size a, Offset b) => new(a.Width + b.Dx, a.Height + b.Dy);
    public static Size operator *(Size a, double operand) => new(a.Width * operand, a.Height * operand);
    public static Size operator /(Size a, double operand) => new(a.Width / operand, a.Height / operand);
    public static Size operator %(Size a, double operand) => new(
        GeometryMath.Modulo(a.Width, operand), GeometryMath.Modulo(a.Height, operand));

    public static bool operator <(Size a, Size b) => a.Width < b.Width && a.Height < b.Height;
    public static bool operator <=(Size a, Size b) => a.Width <= b.Width && a.Height <= b.Height;
    public static bool operator >(Size a, Size b) => a.Width > b.Width && a.Height > b.Height;
    public static bool operator >=(Size a, Size b) => a.Width >= b.Width && a.Height >= b.Height;

    // Dart compares any two OffsetBase values component-wise, so an offset and a size compare too.
    public static bool operator <(Size a, Offset b) => a.Width < b.Dx && a.Height < b.Dy;
    public static bool operator <=(Size a, Offset b) => a.Width <= b.Dx && a.Height <= b.Dy;
    public static bool operator >(Size a, Offset b) => a.Width > b.Dx && a.Height > b.Dy;
    public static bool operator >=(Size a, Offset b) => a.Width >= b.Dx && a.Height >= b.Dy;
    public static bool operator <(Offset a, Size b) => a.Dx < b.Width && a.Dy < b.Height;
    public static bool operator <=(Offset a, Size b) => a.Dx <= b.Width && a.Dy <= b.Height;
    public static bool operator >(Offset a, Size b) => a.Dx > b.Width && a.Dy > b.Height;
    public static bool operator >=(Offset a, Size b) => a.Dx >= b.Width && a.Dy >= b.Height;

    public static bool operator ==(Size a, Size b) => a.Width == b.Width && a.Height == b.Height;
    public static bool operator !=(Size a, Size b) => !(a == b);

    public static Size? Lerp(Size? a, Size? b, double t)
    {
        if (b is not { } end)
        {
            return a is { } start ? start * (1.0 - t) : null;
        }

        return a is not { } from ? end * t : Lerp(from, end, t);
    }

    public static Size Lerp(Size a, Size b, double t) => new(
        GeometryMath.LerpDouble(a.Width, b.Width, t), GeometryMath.LerpDouble(a.Height, b.Height, t));

    public bool Equals(Size other) => Width.Equals(other.Width) && Height.Equals(other.Height);
    public override bool Equals(object? obj) => obj is Size other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Width, Height);
    public override string ToString() => $"Size({GeometryMath.Fixed(Width)}, {GeometryMath.Fixed(Height)})";

    public void Deconstruct(out double width, out double height)
    {
        width = Width;
        height = Height;
    }
}

/// <summary>
/// An immutable, 2D, axis-aligned, floating-point rectangle whose coordinates are relative to a
/// given origin (dart:ui <c>Rect</c>). It stores its four edges independently.
/// </summary>
public readonly struct Rect : IEquatable<Rect>
{
    private Rect(double left, double top, double right, double bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public double Left { get; }
    public double Top { get; }
    public double Right { get; }
    public double Bottom { get; }

    public static Rect FromLTRB(double left, double top, double right, double bottom) =>
        new(left, top, right, bottom);

    public static Rect FromLTWH(double left, double top, double width, double height) =>
        new(left, top, left + width, top + height);

    public static Rect FromCircle(Offset center, double radius) => FromCenter(center, radius * 2.0, radius * 2.0);

    public static Rect FromCenter(Offset center, double width, double height) => new(
        center.Dx - width / 2.0, center.Dy - height / 2.0, center.Dx + width / 2.0, center.Dy + height / 2.0);

    public static Rect FromPoints(Offset a, Offset b) => new(
        Math.Min(a.Dx, b.Dx), Math.Min(a.Dy, b.Dy), Math.Max(a.Dx, b.Dx), Math.Max(a.Dy, b.Dy));

    public static Rect Zero => default;

    private const double GiantScalar = 1.0E+9;

    public static Rect Largest => new(-GiantScalar, -GiantScalar, GiantScalar, GiantScalar);

    public double Width => Right - Left;
    public double Height => Bottom - Top;
    public Size Size => new(Width, Height);

    public bool HasNaN => double.IsNaN(Left) || double.IsNaN(Top) || double.IsNaN(Right) || double.IsNaN(Bottom);

    public bool IsInfinite => double.IsPositiveInfinity(Left) || double.IsPositiveInfinity(Top)
                              || double.IsPositiveInfinity(Right) || double.IsPositiveInfinity(Bottom);

    public bool IsFinite => double.IsFinite(Left) && double.IsFinite(Top)
                            && double.IsFinite(Right) && double.IsFinite(Bottom);

    public bool IsEmpty => Left >= Right || Top >= Bottom;

    public Rect Shift(Offset offset) => new(Left + offset.Dx, Top + offset.Dy, Right + offset.Dx, Bottom + offset.Dy);

    public Rect Translate(double translateX, double translateY) =>
        new(Left + translateX, Top + translateY, Right + translateX, Bottom + translateY);

    public Rect Inflate(double delta) => new(Left - delta, Top - delta, Right + delta, Bottom + delta);
    public Rect Deflate(double delta) => Inflate(-delta);

    /// <summary>The intersection; a non-overlapping pair gives a rectangle with negative width or height.</summary>
    public Rect Intersect(Rect other) => new(
        Math.Max(Left, other.Left), Math.Max(Top, other.Top),
        Math.Min(Right, other.Right), Math.Min(Bottom, other.Bottom));

    public Rect ExpandToInclude(Rect other) => new(
        Math.Min(Left, other.Left), Math.Min(Top, other.Top),
        Math.Max(Right, other.Right), Math.Max(Bottom, other.Bottom));

    public bool Overlaps(Rect other) =>
        !(Right <= other.Left || other.Right <= Left || Bottom <= other.Top || other.Bottom <= Top);

    public double ShortestSide => Math.Min(Math.Abs(Width), Math.Abs(Height));
    public double LongestSide => Math.Max(Math.Abs(Width), Math.Abs(Height));

    public Offset TopLeft => new(Left, Top);
    public Offset TopCenter => new(Left + Width / 2.0, Top);
    public Offset TopRight => new(Right, Top);
    public Offset CenterLeft => new(Left, Top + Height / 2.0);
    public Offset Center => new(Left + Width / 2.0, Top + Height / 2.0);
    public Offset CenterRight => new(Right, Top + Height / 2.0);
    public Offset BottomLeft => new(Left, Bottom);
    public Offset BottomCenter => new(Left + Width / 2.0, Bottom);
    public Offset BottomRight => new(Right, Bottom);

    /// <summary>Whether <paramref name="offset"/> lies inside, top/left edges included, bottom/right excluded.</summary>
    public bool Contains(Offset offset) =>
        offset.Dx >= Left && offset.Dx < Right && offset.Dy >= Top && offset.Dy < Bottom;

    public static Rect? Lerp(Rect? a, Rect? b, double t)
    {
        if (b is not { } end)
        {
            return a is { } start ? Scaled(start, 1.0 - t) : null;
        }

        return a is not { } from ? Scaled(end, t) : Lerp(from, end, t);
    }

    public static Rect Lerp(Rect a, Rect b, double t) => new(
        GeometryMath.LerpDouble(a.Left, b.Left, t), GeometryMath.LerpDouble(a.Top, b.Top, t),
        GeometryMath.LerpDouble(a.Right, b.Right, t), GeometryMath.LerpDouble(a.Bottom, b.Bottom, t));

    private static Rect Scaled(Rect rect, double factor) =>
        new(rect.Left * factor, rect.Top * factor, rect.Right * factor, rect.Bottom * factor);

    public static bool operator ==(Rect a, Rect b) =>
        a.Left == b.Left && a.Top == b.Top && a.Right == b.Right && a.Bottom == b.Bottom;

    public static bool operator !=(Rect a, Rect b) => !(a == b);

    public bool Equals(Rect other) => Left.Equals(other.Left) && Top.Equals(other.Top)
                                      && Right.Equals(other.Right) && Bottom.Equals(other.Bottom);

    public override bool Equals(object? obj) => obj is Rect other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);

    public override string ToString() =>
        $"Rect.fromLTRB({GeometryMath.Fixed(Left)}, {GeometryMath.Fixed(Top)}, {GeometryMath.Fixed(Right)}, {GeometryMath.Fixed(Bottom)})";
}

/// <summary>Number helpers shared by the geometry types.</summary>
internal static class GeometryMath
{
    internal static double LerpDouble(double a, double b, double t) => a * (1.0 - t) + b * t;

    internal static string Fixed(double value) => Math.Abs(value) >= 1e21 && double.IsFinite(value)
        ? value.ToString("R", CultureInfo.InvariantCulture).ToLowerInvariant()
        : Diagnostics.ToStringAsFixed(value, 1);

    internal static double Modulo(double value, double divisor)
    {
        double remainder = value % divisor;
        if (remainder == 0.0)
        {
            return 0.0;
        }

        return remainder < 0.0 ? remainder + Math.Abs(divisor) : remainder;
    }

    internal static double TruncateQuotient(double value, double divisor)
    {
        double quotient = value / divisor;
        if (!double.IsFinite(quotient))
        {
            throw new NotSupportedException("Infinity or NaN toInt");
        }

        // Dart VM's double.~/ returns an int64 before converting it back to double.
        return Math.Clamp(Math.Truncate(quotient), (double)long.MinValue, (double)long.MaxValue);
    }
}
