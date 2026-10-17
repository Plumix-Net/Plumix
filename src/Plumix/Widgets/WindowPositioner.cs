using Avalonia;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/_window_positioner.dart

namespace Plumix.Widgets;

/// <summary>
/// Defines the anchor point for the anchor rectangle or child window when positioning a window.
/// </summary>
/// <remarks>
/// Dart's <c>WindowPositionerAnchor</c>. The specified anchor is used to derive an anchor point on the
/// anchor rectangle that the child window will be positioned relative to. If a corner anchor is set
/// (e.g. <see cref="TopLeft"/> or <see cref="BottomRight"/>), the anchor point will be at the specified
/// corner; otherwise, the derived anchor point will be centered on the specified edge, or in the center
/// of the anchor rectangle if no edge is specified.
/// </remarks>
public enum WindowPositionerAnchor
{
    /// <summary>If the window is the child window, it is centered on the anchor point.</summary>
    Center,

    /// <summary>The anchor point is centered on the top edge of the anchor rectangle.</summary>
    Top,

    /// <summary>The anchor point is centered on the bottom edge of the anchor rectangle.</summary>
    Bottom,

    /// <summary>The anchor point is centered on the left edge of the anchor rectangle.</summary>
    Left,

    /// <summary>The anchor point is centered on the right edge of the anchor rectangle.</summary>
    Right,

    /// <summary>The anchor point is at the top-left corner of the anchor rectangle.</summary>
    TopLeft,

    /// <summary>The anchor point is at the bottom-left corner of the anchor rectangle.</summary>
    BottomLeft,

    /// <summary>The anchor point is at the top-right corner of the anchor rectangle.</summary>
    TopRight,

    /// <summary>The anchor point is at the bottom-right corner of the anchor rectangle.</summary>
    BottomRight,
}

/// <summary>Dart's private <c>WindowPositionerAnchor</c> members, as extensions.</summary>
internal static class WindowPositionerAnchorExtensions
{
    /// <summary>Dart's <c>_flipX</c>: mirrors the anchor horizontally.</summary>
    public static WindowPositionerAnchor FlipX(this WindowPositionerAnchor anchor)
    {
        return anchor switch
        {
            WindowPositionerAnchor.Center => WindowPositionerAnchor.Center,
            WindowPositionerAnchor.Top => WindowPositionerAnchor.Top,
            WindowPositionerAnchor.Bottom => WindowPositionerAnchor.Bottom,
            WindowPositionerAnchor.Left => WindowPositionerAnchor.Right,
            WindowPositionerAnchor.Right => WindowPositionerAnchor.Left,
            WindowPositionerAnchor.TopLeft => WindowPositionerAnchor.TopRight,
            WindowPositionerAnchor.BottomLeft => WindowPositionerAnchor.BottomRight,
            WindowPositionerAnchor.TopRight => WindowPositionerAnchor.TopLeft,
            WindowPositionerAnchor.BottomRight => WindowPositionerAnchor.BottomLeft,
            _ => throw new ArgumentOutOfRangeException(nameof(anchor)),
        };
    }

    /// <summary>Dart's <c>_flipY</c>: mirrors the anchor vertically.</summary>
    public static WindowPositionerAnchor FlipY(this WindowPositionerAnchor anchor)
    {
        return anchor switch
        {
            WindowPositionerAnchor.Center => WindowPositionerAnchor.Center,
            WindowPositionerAnchor.Top => WindowPositionerAnchor.Bottom,
            WindowPositionerAnchor.Bottom => WindowPositionerAnchor.Top,
            WindowPositionerAnchor.Left => WindowPositionerAnchor.Left,
            WindowPositionerAnchor.Right => WindowPositionerAnchor.Right,
            WindowPositionerAnchor.TopLeft => WindowPositionerAnchor.BottomLeft,
            WindowPositionerAnchor.BottomLeft => WindowPositionerAnchor.TopLeft,
            WindowPositionerAnchor.TopRight => WindowPositionerAnchor.BottomRight,
            WindowPositionerAnchor.BottomRight => WindowPositionerAnchor.TopRight,
            _ => throw new ArgumentOutOfRangeException(nameof(anchor)),
        };
    }

    /// <summary>
    /// Dart's <c>_offsetFor</c>: the offset from a child rectangle of <paramref name="size"/>'s top-left
    /// corner to the anchor's point on it, negated.
    /// </summary>
    public static Point OffsetFor(this WindowPositionerAnchor anchor, Size size)
    {
        return anchor switch
        {
            WindowPositionerAnchor.Center => new Point(-size.Width / 2.0, -size.Height / 2.0),
            WindowPositionerAnchor.Top => new Point(-size.Width / 2.0, 0.0),
            WindowPositionerAnchor.Bottom => new Point(-size.Width / 2.0, -size.Height),
            WindowPositionerAnchor.Left => new Point(0.0, -size.Height / 2.0),
            WindowPositionerAnchor.Right => new Point(-size.Width, -size.Height / 2.0),
            WindowPositionerAnchor.TopLeft => new Point(0.0, 0.0),
            WindowPositionerAnchor.BottomLeft => new Point(0.0, -size.Height),
            WindowPositionerAnchor.TopRight => new Point(-size.Width, 0.0),
            WindowPositionerAnchor.BottomRight => new Point(-size.Width, -size.Height),
            _ => throw new ArgumentOutOfRangeException(nameof(anchor)),
        };
    }

    /// <summary>Dart's <c>_anchorPositionFor</c>: the anchor's point on <paramref name="rect"/>.</summary>
    public static Point AnchorPositionFor(this WindowPositionerAnchor anchor, Rect rect)
    {
        return anchor switch
        {
            WindowPositionerAnchor.Center => rect.Center,
            WindowPositionerAnchor.Top => new Point(rect.Left + rect.Width / 2.0, rect.Top),
            WindowPositionerAnchor.Bottom => new Point(rect.Left + rect.Width / 2.0, rect.Bottom),
            WindowPositionerAnchor.Left => new Point(rect.Left, rect.Top + rect.Height / 2.0),
            WindowPositionerAnchor.Right => new Point(rect.Right, rect.Top + rect.Height / 2.0),
            WindowPositionerAnchor.TopLeft => rect.TopLeft,
            WindowPositionerAnchor.BottomLeft => rect.BottomLeft,
            WindowPositionerAnchor.TopRight => rect.TopRight,
            WindowPositionerAnchor.BottomRight => rect.BottomRight,
            _ => throw new ArgumentOutOfRangeException(nameof(anchor)),
        };
    }
}

/// <summary>
/// The constraint adjustment value: how the position of the window should be adjusted if the unadjusted
/// position would result in the surface being partly constrained.
/// </summary>
/// <remarks>
/// Dart's <c>WindowPositionerConstraintAdjustment</c>. Whether a surface is considered "constrained" is left
/// to the platform to determine; for example, the surface may be partly outside the output's "work area",
/// thus necessitating the child surface's position be adjusted until it is entirely inside the work area.
/// </remarks>
public sealed class WindowPositionerConstraintAdjustment
{
    /// <summary>Creates a new <see cref="WindowPositionerConstraintAdjustment"/>.</summary>
    public WindowPositionerConstraintAdjustment(
        bool flipX = false,
        bool flipY = false,
        bool slideX = false,
        bool slideY = false,
        bool resizeX = false,
        bool resizeY = false)
    {
        FlipX = flipX;
        FlipY = flipY;
        SlideX = slideX;
        SlideY = slideY;
        ResizeX = resizeX;
        ResizeY = resizeY;
    }

    /// <summary>Whether to flip the window along the x-axis if it would be constrained.</summary>
    public bool FlipX { get; }

    /// <summary>Whether to flip the window along the y-axis if it would be constrained.</summary>
    public bool FlipY { get; }

    /// <summary>Whether to slide the window along the x-axis if it would be constrained.</summary>
    public bool SlideX { get; }

    /// <summary>Whether to slide the window along the y-axis if it would be constrained.</summary>
    public bool SlideY { get; }

    /// <summary>Whether to resize the window along the x-axis if it would be constrained.</summary>
    public bool ResizeX { get; }

    /// <summary>Whether to resize the window along the y-axis if it would be constrained.</summary>
    public bool ResizeY { get; }

    public override string ToString()
    {
        return $"WindowPositionerConstraintAdjustment(flipX: {Bool(FlipX)}, flipY: {Bool(FlipY)}, "
               + $"slideX: {Bool(SlideX)}, slideY: {Bool(SlideY)}, "
               + $"resizeX: {Bool(ResizeX)}, resizeY: {Bool(ResizeY)})";
    }

    private static string Bool(bool value) => value ? "true" : "false";
}

/// <summary>
/// The <see cref="WindowPositioner"/> provides a collection of rules for the placement of a child window
/// relative to a parent window.
/// </summary>
/// <remarks>
/// Dart's <c>WindowPositioner</c>. Rules can be defined to ensure the child window remains within the
/// visible area's borders, and to specify how the child window changes its position, such as sliding
/// along an axis, or flipping around a rectangle.
/// </remarks>
public sealed class WindowPositioner
{
    private static readonly WindowPositionerConstraintAdjustment DefaultConstraintAdjustment = new();

    /// <summary>Creates a new <see cref="WindowPositioner"/>.</summary>
    public WindowPositioner(
        WindowPositionerAnchor parentAnchor = WindowPositionerAnchor.Center,
        WindowPositionerAnchor childAnchor = WindowPositionerAnchor.Center,
        Point offset = default,
        WindowPositionerConstraintAdjustment? constraintAdjustment = null)
    {
        ParentAnchor = parentAnchor;
        ChildAnchor = childAnchor;
        Offset = offset;
        ConstraintAdjustment = constraintAdjustment ?? DefaultConstraintAdjustment;
    }

    /// <summary>Copy a <see cref="WindowPositioner"/> with some fields replaced.</summary>
    public WindowPositioner CopyWith(
        WindowPositionerAnchor? parentAnchor = null,
        WindowPositionerAnchor? childAnchor = null,
        Point? offset = null,
        WindowPositionerConstraintAdjustment? constraintAdjustment = null)
    {
        return new WindowPositioner(
            parentAnchor: parentAnchor ?? ParentAnchor,
            childAnchor: childAnchor ?? ChildAnchor,
            offset: offset ?? Offset,
            constraintAdjustment: constraintAdjustment ?? ConstraintAdjustment);
    }

    /// <summary>Defines the anchor point for the anchor rectangle.</summary>
    /// <remarks>
    /// The specified anchor is used to derive an anchor point that the child window will be positioned
    /// relative to.
    /// </remarks>
    public WindowPositionerAnchor ParentAnchor { get; }

    /// <summary>Defines the anchor point for the child window.</summary>
    public WindowPositionerAnchor ChildAnchor { get; }

    /// <summary>Specify the window position offset relative to the position of the anchor.</summary>
    /// <remarks>
    /// The anchor point is defined by <see cref="ParentAnchor"/>; the offset is applied to it before it is
    /// constrained to the parent rectangle.
    /// </remarks>
    public Point Offset { get; }

    /// <summary>
    /// The adjustments to apply if the initial position causes the child window to be constrained.
    /// </summary>
    public WindowPositionerConstraintAdjustment ConstraintAdjustment { get; }

    /// <summary>
    /// Computes the screen-space rectangle for a child window placed according to this positioner.
    /// </summary>
    /// <remarks>
    /// Dart's <c>placeWindow</c>. <paramref name="childSize"/> is the frame size of the child window,
    /// <paramref name="anchorRect"/> the rectangle relative to which the child window is placed,
    /// <paramref name="parentRect"/> the parent window's rectangle and <paramref name="displayRect"/> the
    /// display's usable area, all in the same coordinate space.
    /// </remarks>
    public Rect PlaceWindow(Size childSize, Rect anchorRect, Rect parentRect, Rect displayRect)
    {
        Rect defaultResult;
        {
            Point result = ConstrainTo(parentRect, ParentAnchor.AnchorPositionFor(anchorRect) + Offset)
                           + ChildAnchor.OffsetFor(childSize);
            defaultResult = RectFrom(result, childSize);
            if (RectContains(displayRect, defaultResult))
            {
                return defaultResult;
            }
        }

        if (ConstraintAdjustment.FlipX)
        {
            Point result = ConstrainTo(
                               parentRect,
                               ParentAnchor.FlipX().AnchorPositionFor(anchorRect) + FlipX(Offset))
                           + ChildAnchor.FlipX().OffsetFor(childSize);
            if (RectContains(displayRect, RectFrom(result, childSize)))
            {
                return RectFrom(result, childSize);
            }
        }

        if (ConstraintAdjustment.FlipY)
        {
            Point result = ConstrainTo(
                               parentRect,
                               ParentAnchor.FlipY().AnchorPositionFor(anchorRect) + FlipY(Offset))
                           + ChildAnchor.FlipY().OffsetFor(childSize);
            if (RectContains(displayRect, RectFrom(result, childSize)))
            {
                return RectFrom(result, childSize);
            }
        }

        if (ConstraintAdjustment.FlipX && ConstraintAdjustment.FlipY)
        {
            Point result = ConstrainTo(
                               parentRect,
                               ParentAnchor.FlipY().FlipX().AnchorPositionFor(anchorRect) + FlipX(FlipY(Offset)))
                           + ChildAnchor.FlipY().FlipX().OffsetFor(childSize);
            if (RectContains(displayRect, RectFrom(result, childSize)))
            {
                return RectFrom(result, childSize);
            }
        }

        {
            Point result = ConstrainTo(parentRect, ParentAnchor.AnchorPositionFor(anchorRect) + Offset)
                           + ChildAnchor.OffsetFor(childSize);

            if (ConstraintAdjustment.SlideX)
            {
                double leftOverhang = result.X - displayRect.Left;
                double rightOverhang = result.X + childSize.Width - displayRect.Right;

                if (leftOverhang < 0.0)
                {
                    result += new Point(-leftOverhang, 0.0);
                }
                else if (rightOverhang > 0.0)
                {
                    result += new Point(-rightOverhang, 0.0);
                }
            }

            if (ConstraintAdjustment.SlideY)
            {
                double topOverhang = result.Y - displayRect.Top;
                double botOverhang = result.Y + childSize.Height - displayRect.Bottom;

                if (topOverhang < 0.0)
                {
                    result += new Point(0.0, -topOverhang);
                }
                else if (botOverhang > 0.0)
                {
                    result += new Point(0.0, -botOverhang);
                }
            }

            if (RectContains(displayRect, RectFrom(result, childSize)))
            {
                return RectFrom(result, childSize);
            }
        }

        {
            Point result = ConstrainTo(parentRect, ParentAnchor.AnchorPositionFor(anchorRect) + Offset)
                           + ChildAnchor.OffsetFor(childSize);

            if (ConstraintAdjustment.ResizeX)
            {
                double leftOverhang = result.X - displayRect.Left;
                double rightOverhang = result.X + childSize.Width - displayRect.Right;

                if (leftOverhang < 0.0)
                {
                    result += new Point(-leftOverhang, 0.0);
                    childSize = new Size(childSize.Width + leftOverhang, childSize.Height);
                }

                if (rightOverhang > 0.0)
                {
                    childSize = new Size(childSize.Width - rightOverhang, childSize.Height);
                }
            }

            if (ConstraintAdjustment.ResizeY)
            {
                double topOverhang = result.Y - displayRect.Top;
                double botOverhang = result.Y + childSize.Height - displayRect.Bottom;

                if (topOverhang < 0.0)
                {
                    result += new Point(0.0, -topOverhang);
                    childSize = new Size(childSize.Width, childSize.Height + topOverhang);
                }

                if (botOverhang > 0.0)
                {
                    childSize = new Size(childSize.Width, childSize.Height - botOverhang);
                }
            }

            if (RectContains(displayRect, RectFrom(result, childSize)))
            {
                return RectFrom(result, childSize);
            }
        }

        return defaultResult;
    }

    public override string ToString()
    {
        return $"WindowPositioner(parentAnchor: {Diagnostics.DescribeValue(ParentAnchor)}, "
               + $"childAnchor: {Diagnostics.DescribeValue(ChildAnchor)}, "
               + $"offset: {Diagnostics.DescribeValue(Offset)}, constraintAdjustment: {ConstraintAdjustment})";
    }

    /// <summary>Dart's <c>Offset &amp; Size</c>.</summary>
    private static Rect RectFrom(Point offset, Size size) =>
        new(offset.X, offset.Y, size.Width, size.Height);

    private static bool RectContains(Rect r1, Rect r2)
    {
        return r1.Left <= r2.Left && r1.Right >= r2.Right && r1.Top <= r2.Top && r1.Bottom >= r2.Bottom;
    }

    private static Point ConstrainTo(Rect r, Point p)
    {
        return new Point(Math.Clamp(p.X, r.Left, r.Right), Math.Clamp(p.Y, r.Top, r.Bottom));
    }

    private static Point FlipX(Point offset) => new(-offset.X, offset.Y);

    private static Point FlipY(Point offset) => new(offset.X, -offset.Y);
}
