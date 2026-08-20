using Plumix.UI;

namespace Plumix.Rendering;

// C#-only infrastructure. Narrows a ported ShapeBorder back to the radius/side pair still consumed by
// framework paint and clip paths that have not migrated to path-based composition. New code should
// paint through ShapeDecoration and clip through ShapeBorderClipper instead of using these helpers.

public static class ShapeBorderGeometry
{
    /// The outline of an [OutlinedBorder], or [BorderSide.None] for shapes that have no single side.
    public static BorderSide SideOrNone(ShapeBorder? shape)
    {
        return shape is OutlinedBorder outlined ? outlined.Side : BorderSide.None;
    }

    /// The outline of an [OutlinedBorder], or null for shapes that have no single side.
    public static BorderSide? SideOrNull(ShapeBorder? shape)
    {
        return shape is OutlinedBorder outlined ? outlined.Side : null;
    }

    /// Whether the shape paints as a circle rather than a rectangle.
    public static BoxShape BoxShapeOf(ShapeBorder? shape)
    {
        return shape is CircleBorder ? BoxShape.Circle : BoxShape.Rectangle;
    }

    /// The corner radius the shape paints with, or [BorderRadius.Zero] when it has none.
    public static BorderRadius ResolveRadius(ShapeBorder? shape, TextDirection textDirection = TextDirection.Ltr)
    {
        return ResolveRadiusOrNull(shape, textDirection) ?? BorderRadius.Zero;
    }

    /// The corner radius the shape paints with, or null when it has none.
    public static BorderRadius? ResolveRadiusOrNull(
        ShapeBorder? shape,
        TextDirection textDirection = TextDirection.Ltr)
    {
        return shape switch
        {
            RoundedRectangleBorder rounded => rounded.BorderRadius.Resolve(textDirection),
            RoundedSuperellipseBorder rounded => rounded.BorderRadius.Resolve(textDirection),
            BeveledRectangleBorder beveled => beveled.BorderRadius.Resolve(textDirection),
            ContinuousRectangleBorder continuous => continuous.BorderRadius.Resolve(textDirection),
            StadiumBorder or CircleBorder => BorderRadius.Circular(9999.0),
            _ => null,
        };
    }
}
