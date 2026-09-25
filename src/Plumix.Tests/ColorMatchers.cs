// Dart parity source: flutter/packages/flutter_test/lib/src/matchers.dart (colorEpsilon,
// isSameColorAs, _ColorMatcher)

using Xunit;

namespace Plumix.Tests;

/// <summary>
/// flutter_test's colour matchers: <c>isSameColorAs</c> compares each normalized component within a
/// threshold, because dart:ui colours carry double components and interpolated values rarely land
/// exactly on an 8-bit step.
/// </summary>
internal static class ColorMatchers
{
    /// <summary>flutter_test's <c>colorEpsilon</c>: the default <see cref="AssertSameColorAs"/> threshold.</summary>
    public const double ColorEpsilon = 0.004;

    /// <summary>flutter_test's <c>_ColorMatcher.matches</c>.</summary>
    public static bool IsSameColorAs(Color? item, Color target, double threshold = ColorEpsilon) =>
        item is not null
        && item.ColorSpace == target.ColorSpace
        && Math.Abs(item.A - target.A) <= threshold
        && Math.Abs(item.R - target.R) <= threshold
        && Math.Abs(item.G - target.G) <= threshold
        && Math.Abs(item.B - target.B) <= threshold;

    /// <summary><c>expect(item, isSameColorAs(target, threshold: threshold))</c>.</summary>
    public static void AssertSameColorAs(Color target, Color? item, double threshold = ColorEpsilon)
    {
        if (!IsSameColorAs(item, target, threshold))
        {
            Assert.Fail(
                $"Expected a color matching \"{Describe(target)}\" with threshold \"{threshold}\", "
                + $"but got \"{(item is null ? "null" : Describe(item))}\".");
        }
    }

    // The components alone: a resolved CupertinoDynamicColor's own toString reads its resolving
    // element's widget, which throws once the test harness has unmounted it.
    private static string Describe(Color color) =>
        $"{color.GetType().Name}(a: {color.A:F4}, r: {color.R:F4}, g: {color.G:F4}, b: {color.B:F4})";

    /// <summary><c>AssertSameColorAs</c> over two lists of the same length.</summary>
    public static void AssertSameColorsAs(
        IReadOnlyList<Color> target,
        IReadOnlyList<Color>? items,
        double threshold = ColorEpsilon)
    {
        Assert.NotNull(items);
        Assert.Equal(target.Count, items!.Count);
        for (int index = 0; index < target.Count; index++)
        {
            AssertSameColorAs(target[index], items[index], threshold);
        }
    }
}
