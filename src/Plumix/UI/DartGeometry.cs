using System.Globalization;
using Avalonia;
using Plumix.Foundation;

namespace Plumix;

// Dart parity source: flutter/engine/src/flutter/lib/ui/geometry.dart (Offset, Size, Rect)
// Public Avalonia carriers are retained. Factories and operations whose names conflict with
// native members live on this companion; see DIVERGENCES.md for the edge-storage limitation.
public static class DartGeometry
{
    public static Offset OffsetZero => default;
    public static Offset OffsetInfinite => new(double.PositiveInfinity, double.PositiveInfinity);
    public static Size SizeZero => default;
    public static Size SizeInfinite => new(double.PositiveInfinity, double.PositiveInfinity);
    public static Rect RectZero => default;
    public static Rect RectLargest => RectFromLTRB(-1.0E+9, -1.0E+9, 1.0E+9, 1.0E+9);

    public static Offset OffsetFromDirection(double direction, double distance = 1.0) =>
        new(distance * Math.Cos(direction), distance * Math.Sin(direction));

    public static Offset? OffsetLerp(Offset? a, Offset? b, double t) => b is null
        ? a * (1.0 - t)
        : a is null ? b * t : new Offset(LerpDouble(a.Value.Dx, b.Value.Dx, t), LerpDouble(a.Value.Dy, b.Value.Dy, t));

    public static Size SizeCopy(Size source) => source;
    public static Size SizeSquare(double dimension) => new(dimension, dimension);
    public static Size SizeFromWidth(double width) => new(width, double.PositiveInfinity);
    public static Size SizeFromHeight(double height) => new(double.PositiveInfinity, height);
    public static Size SizeFromRadius(double radius) => new(radius * 2.0, radius * 2.0);
    public static Size? SizeLerp(Size? a, Size? b, double t) => b is null
        ? a * (1.0 - t)
        : a is null ? b * t : new Size(
            LerpDouble(a.Value.Width, b.Value.Width, t), LerpDouble(a.Value.Height, b.Value.Height, t));

    public static double SizeAspectRatio(Size size) => size.Height != 0.0
        ? size.Width / size.Height
        : size.Width > 0.0 ? double.PositiveInfinity : size.Width < 0.0 ? double.NegativeInfinity : 0.0;

    public static Rect RectFromLTRB(double left, double top, double right, double bottom) =>
        DartGeometry.RectFromLTWH(left, top, right - left, bottom - top);
    public static Rect RectFromLTWH(double left, double top, double width, double height) =>
        new Rect(left, top, width, height);
    // Migration helpers: Avalonia constructor shapes, removed with DartGeometry.
    public static Rect RectFromOffsetAndSize(Offset offset, Size size) =>
        DartGeometry.RectFromLTWH(offset.Dx, offset.Dy, size.Width, size.Height);
    public static Rect RectFromCorners(Offset topLeft, Offset bottomRight) =>
        RectFromLTRB(topLeft.Dx, topLeft.Dy, bottomRight.Dx, bottomRight.Dy);
    public static Rect RectFromSize(Size size) => DartGeometry.RectFromLTWH(0.0, 0.0, size.Width, size.Height);
    public static Rect RectFromCenter(Offset center, double width, double height) => RectFromLTRB(
        center.Dx - width / 2.0, center.Dy - height / 2.0, center.Dx + width / 2.0, center.Dy + height / 2.0);
    public static Rect RectFromCircle(Offset center, double radius) =>
        RectFromCenter(center, radius * 2.0, radius * 2.0);
    public static Rect RectFromPoints(Offset a, Offset b) => RectFromLTRB(
        Math.Min(a.Dx, b.Dx), Math.Min(a.Dy, b.Dy), Math.Max(a.Dx, b.Dx), Math.Max(a.Dy, b.Dy));
    public static bool RectIsEmpty(Rect rect) => rect.Left >= rect.Right || rect.Top >= rect.Bottom;
    public static Rect RectIntersect(Rect a, Rect b) => RectFromLTRB(
        Math.Max(a.Left, b.Left), Math.Max(a.Top, b.Top), Math.Min(a.Right, b.Right), Math.Min(a.Bottom, b.Bottom));
    public static Rect RectExpandToInclude(Rect a, Rect b) => RectFromLTRB(
        Math.Min(a.Left, b.Left), Math.Min(a.Top, b.Top), Math.Max(a.Right, b.Right), Math.Max(a.Bottom, b.Bottom));
    public static Rect RectInflate(Rect rect, double delta) => RectFromLTRB(
        rect.Left - delta, rect.Top - delta, rect.Right + delta, rect.Bottom + delta);
    public static Rect RectDeflate(Rect rect, double delta) => RectInflate(rect, -delta);
    public static Rect? RectLerp(Rect? a, Rect? b, double t)
    {
        if (b is null)
        {
            return a is { } start ? ScaleRect(start, 1.0 - t) : null;
        }

        if (a is not { } from)
        {
            return ScaleRect(b.Value, t);
        }

        Rect to = b.Value;
        return RectFromLTRB(LerpDouble(from.Left, to.Left, t), LerpDouble(from.Top, to.Top, t),
            LerpDouble(from.Right, to.Right, t), LerpDouble(from.Bottom, to.Bottom, t));
    }

    private static Rect ScaleRect(Rect rect, double factor) => RectFromLTRB(
        rect.Left * factor, rect.Top * factor, rect.Right * factor, rect.Bottom * factor);

    public static double ShortestSide(Rect rect) => Math.Min(Math.Abs(rect.Width), Math.Abs(rect.Height));
    public static double ShortestSide(Size size) => Math.Min(Math.Abs(size.Width), Math.Abs(size.Height));
    public static double LongestSide(Rect rect) => Math.Max(Math.Abs(rect.Width), Math.Abs(rect.Height));
    public static double LongestSide(Size size) => Math.Max(Math.Abs(size.Width), Math.Abs(size.Height));

    // OffsetBase's mixed Offset/Size comparisons, expressed with a shared pair of coordinates.
    public static bool LessThan(Offset a, Offset b) => a.Dx < b.Dx && a.Dy < b.Dy;
    public static bool LessThanOrEqual(Offset a, Offset b) => a.Dx <= b.Dx && a.Dy <= b.Dy;
    public static bool GreaterThan(Offset a, Offset b) => a.Dx > b.Dx && a.Dy > b.Dy;
    public static bool GreaterThanOrEqual(Offset a, Offset b) => a.Dx >= b.Dx && a.Dy >= b.Dy;
    public static bool LessThan(Size a, Size b) => LessThan(new Offset(a.Width, a.Height), new Offset(b.Width, b.Height));
    public static bool LessThanOrEqual(Size a, Size b) =>
        LessThanOrEqual(new Offset(a.Width, a.Height), new Offset(b.Width, b.Height));
    public static bool GreaterThan(Size a, Size b) =>
        GreaterThan(new Offset(a.Width, a.Height), new Offset(b.Width, b.Height));
    public static bool GreaterThanOrEqual(Size a, Size b) =>
        GreaterThanOrEqual(new Offset(a.Width, a.Height), new Offset(b.Width, b.Height));
    public static bool LessThan(Offset a, Size b) => LessThan(a, new Offset(b.Width, b.Height));
    public static bool LessThan(Size a, Offset b) => LessThan(new Offset(a.Width, a.Height), b);
    public static bool LessThanOrEqual(Offset a, Size b) => LessThanOrEqual(a, new Offset(b.Width, b.Height));
    public static bool LessThanOrEqual(Size a, Offset b) => LessThanOrEqual(new Offset(a.Width, a.Height), b);
    public static bool GreaterThan(Offset a, Size b) => GreaterThan(a, new Offset(b.Width, b.Height));
    public static bool GreaterThan(Size a, Offset b) => GreaterThan(new Offset(a.Width, a.Height), b);
    public static bool GreaterThanOrEqual(Offset a, Size b) => GreaterThanOrEqual(a, new Offset(b.Width, b.Height));
    public static bool GreaterThanOrEqual(Size a, Offset b) => GreaterThanOrEqual(new Offset(a.Width, a.Height), b);

    public static string OffsetToString(Offset point) => $"Offset({Fixed(point.Dx)}, {Fixed(point.Dy)})";
    public static string SizeToString(Size size) => $"Size({Fixed(size.Width)}, {Fixed(size.Height)})";
    public static string RectToString(Rect rect) =>
        $"Rect.fromLTRB({Fixed(rect.Left)}, {Fixed(rect.Top)}, {Fixed(rect.Right)}, {Fixed(rect.Bottom)})";

    internal static double LerpDouble(double a, double b, double t) => a * (1.0 - t) + b * t;
    internal static string Fixed(double value) => Math.Abs(value) >= 1e21 && double.IsFinite(value)
        ? value.ToString("R", CultureInfo.InvariantCulture).ToLowerInvariant()
        : Diagnostics.ToStringAsFixed(value, 1);

    internal static double Modulo(double value, double divisor)
    {
        double remainder = value % divisor;
        if (remainder == 0.0) { return 0.0; }
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
