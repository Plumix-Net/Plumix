using Avalonia;

// Dart parity source: dart:ui Rect.fromLTRB/fromCenter/fromCircle/isEmpty/shortestSide and
// Size.fromRadius/shortestSide (engine/src/flutter/lib/ui/geometry.dart). Avalonia's `Rect` and `Size`
// are sealed value types, so the named constructors and getters Dart ports need live here.

namespace Plumix.UI;

/// <summary>dart:ui's <c>Rect</c>/<c>Size</c> named constructors and getters over Avalonia's types.</summary>
internal static class DartGeometry
{
    /// <summary>
    /// Dart's <c>Rect.fromLTRB</c>: the coordinates are kept as given, so <c>right &lt; left</c> yields a
    /// negative width (an empty rect), like dart:ui.
    /// </summary>
    public static Rect RectFromLTRB(double left, double top, double right, double bottom) =>
        new(left, top, right - left, bottom - top);

    /// <summary>Dart's <c>Rect.fromCenter</c>.</summary>
    public static Rect RectFromCenter(Point center, double width, double height) =>
        new(center.X - (width / 2), center.Y - (height / 2), width, height);

    /// <summary>Dart's <c>Rect.fromCircle</c>.</summary>
    public static Rect RectFromCircle(Point center, double radius) =>
        RectFromCenter(center, radius * 2, radius * 2);

    /// <summary>Dart's <c>Rect.isEmpty</c>.</summary>
    public static bool RectIsEmpty(Rect rect) => rect.Left >= rect.Right || rect.Top >= rect.Bottom;

    /// <summary>Dart's <c>Rect.shortestSide</c>.</summary>
    public static double ShortestSide(Rect rect) => Math.Min(Math.Abs(rect.Width), Math.Abs(rect.Height));

    /// <summary>Dart's <c>Size.shortestSide</c>.</summary>
    public static double ShortestSide(Size size) => Math.Min(Math.Abs(size.Width), Math.Abs(size.Height));

    /// <summary>Dart's <c>Size.fromRadius</c>.</summary>
    public static Size SizeFromRadius(double radius) => new(radius * 2.0, radius * 2.0);
}
