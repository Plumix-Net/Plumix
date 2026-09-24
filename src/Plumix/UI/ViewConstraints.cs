using System.Globalization;
using Avalonia;

// Dart parity source: flutter/bin/cache/pkg/sky_engine/lib/ui/platform_dispatcher.dart (ViewConstraints)

namespace Plumix.UI;

/// <summary>Immutable layout constraints for a view.</summary>
/// <remarks>
/// dart:ui's <c>ViewConstraints</c>: the constraints a platform view may be sized within. A value type
/// here, so Dart's <c>identical</c> short-cut in <c>==</c> has no counterpart; the field comparison is
/// the same.
/// </remarks>
/// <param name="MinWidth">The minimum width that satisfies the constraints.</param>
/// <param name="MaxWidth">The maximum width that satisfies the constraints; may be infinite.</param>
/// <param name="MinHeight">The minimum height that satisfies the constraints.</param>
/// <param name="MaxHeight">The maximum height that satisfies the constraints; may be infinite.</param>
public readonly record struct ViewConstraints(
    double MinWidth = 0.0,
    double MaxWidth = double.PositiveInfinity,
    double MinHeight = 0.0,
    double MaxHeight = double.PositiveInfinity)
{
    /// <summary>
    /// The constraints Dart writes as <c>const ViewConstraints()</c>; C#'s parameterless
    /// <c>new ViewConstraints()</c> would bypass the defaults and yield a tight zero constraint.
    /// </summary>
    public static ViewConstraints Unbounded => new(0.0, double.PositiveInfinity, 0.0, double.PositiveInfinity);

    /// <summary>Creates view constraints that are respected only by the given size.</summary>
    public static ViewConstraints Tight(Size size) => new(size.Width, size.Width, size.Height, size.Height);

    /// <summary>Whether the given size satisfies the constraints.</summary>
    public bool IsSatisfiedBy(Size size)
    {
        return (MinWidth <= size.Width)
               && (size.Width <= MaxWidth)
               && (MinHeight <= size.Height)
               && (size.Height <= MaxHeight);
    }

    /// <summary>Whether there is exactly one size that satisfies the constraints.</summary>
    public bool IsTight => MinWidth >= MaxWidth && MinHeight >= MaxHeight;

    /// <summary>Scales each constraint parameter by the inverse of the given factor.</summary>
    public static ViewConstraints operator /(ViewConstraints constraints, double factor) => new(
        constraints.MinWidth / factor,
        constraints.MaxWidth / factor,
        constraints.MinHeight / factor,
        constraints.MaxHeight / factor);

    /// <inheritdoc />
    public override string ToString()
    {
        if (MinWidth == double.PositiveInfinity && MinHeight == double.PositiveInfinity)
        {
            return "ViewConstraints(biggest)";
        }

        if (MinWidth == 0
            && MaxWidth == double.PositiveInfinity
            && MinHeight == 0
            && MaxHeight == double.PositiveInfinity)
        {
            return "ViewConstraints(unconstrained)";
        }

        static string Describe(double min, double max, string dim)
        {
            if (min == max)
            {
                return $"{dim}={min.ToString("F1", CultureInfo.InvariantCulture)}";
            }

            return $"{min.ToString("F1", CultureInfo.InvariantCulture)}<={dim}"
                   + $"<={max.ToString("F1", CultureInfo.InvariantCulture)}";
        }

        string width = Describe(MinWidth, MaxWidth, "w");
        string height = Describe(MinHeight, MaxHeight, "h");
        return $"ViewConstraints({width}, {height})";
    }
}
