using Avalonia;

namespace Plumix;

// Dart parity source: flutter/engine/src/flutter/lib/ui/geometry.dart (Offset, Size, Rect)
// Member collisions with Avalonia use the DartGeometry companion or explicit Dart-prefixed names.
public static class OffsetExtensions
{
    extension(Offset offset)
    {
        public double Dx => offset.X;
        public double Dy => offset.Y;
        public double DistanceSquared => offset.Dx * offset.Dx + offset.Dy * offset.Dy;
        public double DartDistanceSquared => offset.X * offset.X + offset.Y * offset.Y;
        public double DartDistance => Math.Sqrt(offset.DistanceSquared);
        public double Direction => Math.Atan2(offset.Dy, offset.Dx);
        public bool IsInfinite => double.IsPositiveInfinity(offset.Dx) || double.IsPositiveInfinity(offset.Dy);
        public bool IsFinite => double.IsFinite(offset.Dx) && double.IsFinite(offset.Dy);
        public Offset Scale(double scaleX, double scaleY) => new(offset.Dx * scaleX, offset.Dy * scaleY);
        public Offset Translate(double translateX, double translateY) =>
            new(offset.Dx + translateX, offset.Dy + translateY);
        public Rect WithSize(Size size) => DartGeometry.RectFromOffsetAndSize(offset, size);
        public Offset TruncateDivide(double divisor) => new(
            DartGeometry.TruncateQuotient(offset.Dx, divisor), DartGeometry.TruncateQuotient(offset.Dy, divisor));
        public Offset Modulo(double divisor) => new(
            DartGeometry.Modulo(offset.Dx, divisor), DartGeometry.Modulo(offset.Dy, divisor));
    }
}

public static class SizeExtensions
{
    extension(Size size)
    {
        public Size Flipped => new(size.Height, size.Width);
        public bool IsEmpty => size.Width <= 0.0 || size.Height <= 0.0;
        public bool IsInfinite => double.IsPositiveInfinity(size.Width) || double.IsPositiveInfinity(size.Height);
        public bool IsFinite => double.IsFinite(size.Width) && double.IsFinite(size.Height);
        public double DartAspectRatio => DartGeometry.SizeAspectRatio(size);
        public double ShortestSide => DartGeometry.ShortestSide(size);
        public double LongestSide => DartGeometry.LongestSide(size);
        public bool Contains(Offset offset) =>
            offset.Dx >= 0.0 && offset.Dx < size.Width && offset.Dy >= 0.0 && offset.Dy < size.Height;
        public Offset TopLeft(Offset origin) => origin;
        public Offset TopCenter(Offset origin) => new(origin.Dx + size.Width / 2.0, origin.Dy);
        public Offset TopRight(Offset origin) => new(origin.Dx + size.Width, origin.Dy);
        public Offset CenterLeft(Offset origin) => new(origin.Dx, origin.Dy + size.Height / 2.0);
        public Offset Center(Offset origin) => new(origin.Dx + size.Width / 2.0, origin.Dy + size.Height / 2.0);
        public Offset CenterRight(Offset origin) => new(origin.Dx + size.Width, origin.Dy + size.Height / 2.0);
        public Offset BottomLeft(Offset origin) => new(origin.Dx, origin.Dy + size.Height);
        public Offset BottomCenter(Offset origin) => new(origin.Dx + size.Width / 2.0, origin.Dy + size.Height);
        public Offset BottomRight(Offset origin) => new(origin.Dx + size.Width, origin.Dy + size.Height);
        public Size Add(Offset offset) => new(size.Width + offset.Dx, size.Height + offset.Dy);
        public Size Subtract(Offset offset) => new(size.Width - offset.Dx, size.Height - offset.Dy);
        public Offset Difference(Size other) => new(size.Width - other.Width, size.Height - other.Height);
        public Size TruncateDivide(double divisor) => new(
            DartGeometry.TruncateQuotient(size.Width, divisor), DartGeometry.TruncateQuotient(size.Height, divisor));
        public Size Modulo(double divisor) => new(
            DartGeometry.Modulo(size.Width, divisor), DartGeometry.Modulo(size.Height, divisor));
    }
}

public static class RectExtensions
{
    extension(Rect rect)
    {
        public bool HasNaN => double.IsNaN(rect.Left) || double.IsNaN(rect.Top)
                              || double.IsNaN(rect.Right) || double.IsNaN(rect.Bottom);
        public bool IsInfinite => double.IsPositiveInfinity(rect.Left) || double.IsPositiveInfinity(rect.Top)
                                  || double.IsPositiveInfinity(rect.Right) || double.IsPositiveInfinity(rect.Bottom);
        public bool IsFinite => double.IsFinite(rect.Left) && double.IsFinite(rect.Top)
                                && double.IsFinite(rect.Right) && double.IsFinite(rect.Bottom);
        public bool IsEmpty => DartGeometry.RectIsEmpty(rect);
        public double ShortestSide => DartGeometry.ShortestSide(rect);
        public double LongestSide => DartGeometry.LongestSide(rect);
        public Offset TopCenter => new(rect.Left + rect.Width / 2.0, rect.Top);
        public Offset CenterLeft => new(rect.Left, rect.Top + rect.Height / 2.0);
        public Offset CenterRight => new(rect.Right, rect.Top + rect.Height / 2.0);
        public Offset BottomCenter => new(rect.Left + rect.Width / 2.0, rect.Bottom);
        public Rect Shift(Offset offset) => DartGeometry.RectFromLTRB(
            rect.Left + offset.Dx, rect.Top + offset.Dy, rect.Right + offset.Dx, rect.Bottom + offset.Dy);
        public Rect Translate(double x, double y) => rect.Shift(new Offset(x, y));
        public Rect IntersectUnclamped(Rect other) => DartGeometry.RectIntersect(rect, other);
        public Rect ExpandToInclude(Rect other) => DartGeometry.RectExpandToInclude(rect, other);
        public bool Overlaps(Rect other) => !(rect.Right <= other.Left || other.Right <= rect.Left
                                              || rect.Bottom <= other.Top || other.Bottom <= rect.Top);
        /// <summary>Dart's half-open containment; Avalonia's instance Contains includes all four edges.</summary>
        public bool ContainsHalfOpen(Offset offset) =>
            offset.Dx >= rect.Left && offset.Dx < rect.Right && offset.Dy >= rect.Top && offset.Dy < rect.Bottom;
    }
}
