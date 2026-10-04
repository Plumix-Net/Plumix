using Avalonia;

// Dart parity source: flutter/packages/flutter/lib/src/painting/geometry.dart

namespace Plumix.Painting;

/// <summary>The top-level functions of Dart's <c>painting/geometry.dart</c>.</summary>
public static class PaintingGeometry
{
    /// <summary>
    /// Position a child box within a container box, either above or below a target point.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dart's <c>positionDependentBox</c>. The container's size is described by <paramref name="size"/>.
    /// The target point is specified by <paramref name="target"/>, as an offset from the top left of
    /// the container. The child box's size is given by <paramref name="childSize"/>.
    /// </para>
    /// <para>
    /// The <paramref name="preferBelow"/> argument is used when there is room both above and below the
    /// target point; <paramref name="verticalOffset"/> is the amount of vertical distance between the
    /// target and the displayed child, and <paramref name="margin"/> is the amount of horizontal
    /// distance between the child and the edges of the container.
    /// </para>
    /// </remarks>
    public static Point PositionDependentBox(
        Size size,
        Size childSize,
        Point target,
        bool preferBelow,
        double verticalOffset = 0.0,
        double margin = 10.0)
    {
        // VERTICAL DIRECTION
        bool fitsBelow = target.Y + verticalOffset + childSize.Height <= size.Height - margin;
        bool fitsAbove = target.Y - verticalOffset - childSize.Height >= margin;
        bool tooltipBelow = fitsAbove == fitsBelow ? preferBelow : fitsBelow;
        double y;
        if (tooltipBelow)
        {
            y = Math.Min(target.Y + verticalOffset, size.Height - margin);
        }
        else
        {
            y = Math.Max(target.Y - verticalOffset - childSize.Height, margin);
        }

        // HORIZONTAL DIRECTION
        double flexibleSpace = size.Width - childSize.Width;
        double x = flexibleSpace <= 2 * margin
            // If there's not enough horizontal space for margin + child, center the
            // child.
            ? flexibleSpace / 2.0
            : Math.Clamp(target.X - childSize.Width / 2, margin, flexibleSpace - margin);
        return new Point(x, y);
    }
}
