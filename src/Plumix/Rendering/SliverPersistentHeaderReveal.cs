using Avalonia;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/sliver_persistent_header.dart

namespace Plumix.Rendering;

/// <summary>
/// How much of a persistent header a <see cref="RenderObject.ShowOnScreen"/> request is allowed to
/// expand, when the header wants to reveal itself rather than let the viewport scroll it into view.
/// </summary>
public sealed class PersistentHeaderShowOnScreenConfiguration : IEquatable<PersistentHeaderShowOnScreenConfiguration>
{
    public PersistentHeaderShowOnScreenConfiguration(
        double minShowOnScreenExtent = double.NegativeInfinity,
        double maxShowOnScreenExtent = double.PositiveInfinity)
    {
        if (minShowOnScreenExtent > maxShowOnScreenExtent)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minShowOnScreenExtent),
                "minShowOnScreenExtent must be less than or equal to maxShowOnScreenExtent.");
        }

        MinShowOnScreenExtent = minShowOnScreenExtent;
        MaxShowOnScreenExtent = maxShowOnScreenExtent;
    }

    /// <summary>The smallest extent the header expands to when it reveals itself.</summary>
    public double MinShowOnScreenExtent { get; }

    /// <summary>The largest extent the header expands to when it reveals itself.</summary>
    public double MaxShowOnScreenExtent { get; }

    public bool Equals(PersistentHeaderShowOnScreenConfiguration? other)
    {
        return other is not null
               && MinShowOnScreenExtent.Equals(other.MinShowOnScreenExtent)
               && MaxShowOnScreenExtent.Equals(other.MaxShowOnScreenExtent);
    }

    public override bool Equals(object? obj) => Equals(obj as PersistentHeaderShowOnScreenConfiguration);

    public override int GetHashCode() => HashCode.Combine(MinShowOnScreenExtent, MaxShowOnScreenExtent);
}

/// <summary>Shared reveal helpers for the persistent-header render objects.</summary>
internal static class PersistentHeaderReveal
{
    /// <summary>
    /// Clips <paramref name="original"/> to the supplied edges, which is how Flutter drops the part
    /// of a reveal request that sits behind a pinned header's leading edge.
    /// </summary>
    public static Rect? Trim(
        Rect? original,
        double top = double.NegativeInfinity,
        double right = double.PositiveInfinity,
        double bottom = double.PositiveInfinity,
        double left = double.NegativeInfinity)
    {
        if (original is not { } rect)
        {
            return null;
        }

        double newLeft = Math.Max(rect.Left, left);
        double newTop = Math.Max(rect.Top, top);
        double newRight = Math.Min(rect.Right, right);
        double newBottom = Math.Min(rect.Bottom, bottom);
        return new Rect(newLeft, newTop, newRight - newLeft, newBottom - newTop);
    }

    /// <summary>The axis direction the header's content actually grows in.</summary>
    public static AxisDirection EffectiveAxisDirection(SliverConstraints constraints)
    {
        if (constraints.GrowthDirection == GrowthDirection.Forward)
        {
            return constraints.AxisDirection;
        }

        return constraints.AxisDirection switch
        {
            AxisDirection.Up => AxisDirection.Down,
            AxisDirection.Down => AxisDirection.Up,
            AxisDirection.Left => AxisDirection.Right,
            _ => AxisDirection.Left,
        };
    }

    /// <summary>
    /// Trims a reveal request against a pinned header of <paramref name="childExtent"/>, so the
    /// enclosing viewport is only asked to reveal the part that is not already pinned in place.
    /// </summary>
    public static Rect? TrimForPinnedHeader(
        Rect? localBounds,
        AxisDirection effectiveAxisDirection,
        double childExtent)
    {
        return effectiveAxisDirection switch
        {
            AxisDirection.Up => Trim(localBounds, bottom: childExtent),
            AxisDirection.Left => Trim(localBounds, right: childExtent),
            AxisDirection.Right => Trim(localBounds, left: 0.0),
            _ => Trim(localBounds, top: 0.0),
        };
    }
}
