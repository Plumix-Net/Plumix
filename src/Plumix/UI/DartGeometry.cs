using Plumix.UI;

namespace Plumix;

// C#-only infrastructure: no Dart parity source.
// Transitional companion from the Avalonia geometry carriers; removed by the migration.
public static class DartGeometry
{
    public static Offset OffsetZero => Offset.Zero;
    public static Offset OffsetInfinite => Offset.Infinite;
    public static Size SizeZero => Size.Zero;
    public static Size SizeInfinite => Size.Infinite;
    public static Rect RectZero => Rect.Zero;
    public static Rect RectLargest => Rect.Largest;
    public static Offset OffsetFromDirection(double direction, double distance = 1.0) =>
        Offset.FromDirection(direction, distance);
    public static Offset? OffsetLerp(Offset? a, Offset? b, double t) => Offset.Lerp(a, b, t);
    public static Size SizeCopy(Size source) => source;
    public static Size SizeSquare(double dimension) => Size.Square(dimension);
    public static Size SizeFromWidth(double width) => Size.FromWidth(width);
    public static Size SizeFromHeight(double height) => Size.FromHeight(height);
    public static Size SizeFromRadius(double radius) => Size.FromRadius(radius);
    public static Size? SizeLerp(Size? a, Size? b, double t) => Size.Lerp(a, b, t);
    public static double SizeAspectRatio(Size size) => size.AspectRatio;
    public static Rect RectFromLTRB(double left, double top, double right, double bottom) =>
        Rect.FromLTRB(left, top, right, bottom);
    public static Rect RectFromLTWH(double left, double top, double width, double height) =>
        Rect.FromLTWH(left, top, width, height);
    public static Rect RectFromOffsetAndSize(Offset offset, Size size) => offset & size;
    public static Rect RectFromCorners(Offset topLeft, Offset bottomRight) =>
        Rect.FromLTRB(topLeft.Dx, topLeft.Dy, bottomRight.Dx, bottomRight.Dy);
    public static Rect RectFromSize(Size size) => Offset.Zero & size;
    public static Rect RectFromCenter(Offset center, double width, double height) =>
        Rect.FromCenter(center, width, height);
    public static Rect RectFromCircle(Offset center, double radius) => Rect.FromCircle(center, radius);
    public static Rect RectFromPoints(Offset a, Offset b) => Rect.FromPoints(a, b);
    public static bool RectIsEmpty(Rect rect) => rect.IsEmpty;
    public static Rect RectIntersect(Rect a, Rect b) => a.Intersect(b);
    public static Rect RectExpandToInclude(Rect a, Rect b) => a.ExpandToInclude(b);
    public static Rect RectInflate(Rect rect, double delta) => rect.Inflate(delta);
    public static Rect RectDeflate(Rect rect, double delta) => rect.Deflate(delta);
    public static Rect? RectLerp(Rect? a, Rect? b, double t) => Rect.Lerp(a, b, t);
    public static double ShortestSide(Rect rect) => rect.ShortestSide;
    public static double ShortestSide(Size size) => size.ShortestSide;
    public static double LongestSide(Rect rect) => rect.LongestSide;
    public static double LongestSide(Size size) => size.LongestSide;
    public static bool LessThan(Offset a, Offset b) => a < b;
    public static bool LessThanOrEqual(Offset a, Offset b) => a <= b;
    public static bool GreaterThan(Offset a, Offset b) => a > b;
    public static bool GreaterThanOrEqual(Offset a, Offset b) => a >= b;
    public static bool LessThan(Size a, Size b) => a < b;
    public static bool LessThanOrEqual(Size a, Size b) => a <= b;
    public static bool GreaterThan(Size a, Size b) => a > b;
    public static bool GreaterThanOrEqual(Size a, Size b) => a >= b;
    public static bool LessThan(Offset a, Size b) => a < b;
    public static bool LessThan(Size a, Offset b) => a < b;
    public static bool LessThanOrEqual(Offset a, Size b) => a <= b;
    public static bool LessThanOrEqual(Size a, Offset b) => a <= b;
    public static bool GreaterThan(Offset a, Size b) => a > b;
    public static bool GreaterThan(Size a, Offset b) => a > b;
    public static bool GreaterThanOrEqual(Offset a, Size b) => a >= b;
    public static bool GreaterThanOrEqual(Size a, Offset b) => a >= b;
    public static string OffsetToString(Offset point) => point.ToString();
    public static string SizeToString(Size size) => size.ToString();
    public static string RectToString(Rect rect) => rect.ToString();
    internal static double LerpDouble(double a, double b, double t) => GeometryMath.LerpDouble(a, b, t);
    internal static string Fixed(double value) => GeometryMath.Fixed(value);
    internal static double Modulo(double value, double divisor) => GeometryMath.Modulo(value, divisor);
    internal static double TruncateQuotient(double value, double divisor) =>
        GeometryMath.TruncateQuotient(value, divisor);
}
