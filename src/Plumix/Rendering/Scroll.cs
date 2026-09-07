using Avalonia;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/viewport.dart

namespace Plumix.Rendering;

public enum CacheExtentStyle
{
    Pixel,
    Viewport
}

public sealed record ScrollCacheExtent
{
    private ScrollCacheExtent(double value, CacheExtentStyle style)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        Value = value;
        Style = style;
    }

    public double Value { get; }

    public CacheExtentStyle Style { get; }

    public static ScrollCacheExtent Pixels(double pixels) => new(pixels, CacheExtentStyle.Pixel);

    public static ScrollCacheExtent Viewport(double value) => new(value, CacheExtentStyle.Viewport);

    internal double CalculateCacheOffset(double mainAxisExtent)
    {
        return Style == CacheExtentStyle.Viewport ? Value * mainAxisExtent : Value;
    }
}

public enum AxisDirection
{
    Up,
    Right,
    Down,
    Left
}

public enum GrowthDirection
{
    Forward,
    Reverse
}

public static class ScrollDirectionUtils
{
    /// <summary>Returns the opposite of the given <see cref="ScrollDirection"/>.</summary>
    /// <remarks>Flutter's <c>flipScrollDirection</c> (<c>rendering/viewport_offset.dart</c>).</remarks>
    public static ScrollDirection FlipScrollDirection(ScrollDirection direction)
    {
        return direction switch
        {
            ScrollDirection.Idle => ScrollDirection.Idle,
            ScrollDirection.Forward => ScrollDirection.Reverse,
            _ => ScrollDirection.Forward,
        };
    }

    public static Axis AxisDirectionToAxis(AxisDirection direction)
    {
        return direction switch
        {
            AxisDirection.Up => Axis.Vertical,
            AxisDirection.Down => Axis.Vertical,
            AxisDirection.Left => Axis.Horizontal,
            AxisDirection.Right => Axis.Horizontal,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    public static bool AxisDirectionIsReversed(AxisDirection direction)
    {
        return direction == AxisDirection.Up || direction == AxisDirection.Left;
    }

    public static AxisDirection DefaultAxisDirection(Axis axis)
    {
        return axis == Axis.Vertical ? AxisDirection.Down : AxisDirection.Right;
    }

    /// <summary>
    /// Resolves the direction in which the scroll offset increases from a scroll axis, whether the
    /// view is reversed, and the ambient reading direction.
    /// </summary>
    /// <remarks>Flutter's <c>getAxisDirectionFromAxisReverseAndDirectionality</c>.</remarks>
    public static AxisDirection GetAxisDirectionFromAxisReverseAndDirectionality(
        BuildContext context,
        Axis axis,
        bool reverse)
    {
        if (axis == Axis.Vertical)
        {
            return reverse ? AxisDirection.Up : AxisDirection.Down;
        }

        AxisDirection readingDirection = Directionality.Of(context) == UI.TextDirection.Rtl
            ? AxisDirection.Left
            : AxisDirection.Right;
        if (!reverse)
        {
            return readingDirection;
        }

        return readingDirection == AxisDirection.Left ? AxisDirection.Right : AxisDirection.Left;
    }

    /// <remarks>Flutter's <c>applyGrowthDirectionToAxisDirection</c>.</remarks>
    public static AxisDirection ApplyGrowthDirectionToAxisDirection(
        AxisDirection axisDirection,
        GrowthDirection growthDirection)
    {
        if (growthDirection == GrowthDirection.Forward)
        {
            return axisDirection;
        }

        return axisDirection switch
        {
            AxisDirection.Up => AxisDirection.Down,
            AxisDirection.Right => AxisDirection.Left,
            AxisDirection.Down => AxisDirection.Up,
            AxisDirection.Left => AxisDirection.Right,
            _ => axisDirection,
        };
    }
}
