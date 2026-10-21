using Avalonia;

namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/geometry.dart (Offset, Size, Rect)
// Member collisions with Avalonia use the DartGeometry companion or explicit Dart-prefixed names.
public static class OffsetExtensions
{
    extension(Point offset)
    {
        public double Dx => offset.X;
        public double Dy => offset.Y;
        public double DistanceSquared => offset.X * offset.X + offset.Y * offset.Y;
        public double DartDistance => Math.Sqrt(offset.DistanceSquared);
        public double Direction => Math.Atan2(offset.Y, offset.X);
        public bool IsInfinite => double.IsPositiveInfinity(offset.X) || double.IsPositiveInfinity(offset.Y);
        public bool IsFinite => double.IsFinite(offset.X) && double.IsFinite(offset.Y);
        public Point Scale(double scaleX, double scaleY) => new(offset.X * scaleX, offset.Y * scaleY);
        public Point Translate(double translateX, double translateY) =>
            new(offset.X + translateX, offset.Y + translateY);
        public Rect WithSize(Size size) => new(offset, size);
        public Point TruncateDivide(double divisor) => new(
            DartGeometry.TruncateQuotient(offset.X, divisor), DartGeometry.TruncateQuotient(offset.Y, divisor));
        public Point Modulo(double divisor) => new(
            DartGeometry.Modulo(offset.X, divisor), DartGeometry.Modulo(offset.Y, divisor));
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
        public bool Contains(Point offset) =>
            offset.X >= 0.0 && offset.X < size.Width && offset.Y >= 0.0 && offset.Y < size.Height;
        public Point TopLeft(Point origin) => origin;
        public Point TopCenter(Point origin) => new(origin.X + size.Width / 2.0, origin.Y);
        public Point TopRight(Point origin) => new(origin.X + size.Width, origin.Y);
        public Point CenterLeft(Point origin) => new(origin.X, origin.Y + size.Height / 2.0);
        public Point Center(Point origin) => new(origin.X + size.Width / 2.0, origin.Y + size.Height / 2.0);
        public Point CenterRight(Point origin) => new(origin.X + size.Width, origin.Y + size.Height / 2.0);
        public Point BottomLeft(Point origin) => new(origin.X, origin.Y + size.Height);
        public Point BottomCenter(Point origin) => new(origin.X + size.Width / 2.0, origin.Y + size.Height);
        public Point BottomRight(Point origin) => new(origin.X + size.Width, origin.Y + size.Height);
        public Size Add(Point offset) => new(size.Width + offset.X, size.Height + offset.Y);
        public Size Subtract(Point offset) => new(size.Width - offset.X, size.Height - offset.Y);
        public Point Difference(Size other) => new(size.Width - other.Width, size.Height - other.Height);
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
        public Point TopCenter => new(rect.Left + rect.Width / 2.0, rect.Top);
        public Point CenterLeft => new(rect.Left, rect.Top + rect.Height / 2.0);
        public Point CenterRight => new(rect.Right, rect.Top + rect.Height / 2.0);
        public Point BottomCenter => new(rect.Left + rect.Width / 2.0, rect.Bottom);
        public Rect Shift(Point offset) => DartGeometry.RectFromLTRB(
            rect.Left + offset.X, rect.Top + offset.Y, rect.Right + offset.X, rect.Bottom + offset.Y);
        public Rect Translate(double x, double y) => rect.Shift(new Point(x, y));
        public Rect IntersectUnclamped(Rect other) => DartGeometry.RectIntersect(rect, other);
        public Rect ExpandToInclude(Rect other) => DartGeometry.RectExpandToInclude(rect, other);
        public bool Overlaps(Rect other) => !(rect.Right <= other.Left || other.Right <= rect.Left
                                              || rect.Bottom <= other.Top || other.Bottom <= rect.Top);
        /// <summary>Dart's half-open containment; Avalonia's instance Contains includes all four edges.</summary>
        public bool ContainsHalfOpen(Point offset) =>
            offset.X >= rect.Left && offset.X < rect.Right && offset.Y >= rect.Top && offset.Y < rect.Bottom;
    }
}
