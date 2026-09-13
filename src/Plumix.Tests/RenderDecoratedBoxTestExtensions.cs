using Plumix.Rendering;

// C#-only test infrastructure: projects a RenderDecoratedBox decoration onto a BoxDecoration so tests can read
// its color, border, radius and shadows whether the widget painted a BoxDecoration or a ShapeDecoration.

namespace Plumix.Tests;

internal static class RenderDecoratedBoxTestExtensions
{
    extension(RenderDecoratedBox box)
    {
        /// <summary>
        /// The decoration as a <see cref="BoxDecoration"/>. A <see cref="ShapeDecoration"/> whose shape has an
        /// exact box equivalent (rounded rectangle, stadium, circle or <see cref="BoxBorder"/>) is projected onto
        /// one; any other decoration throws.
        /// </summary>
        public BoxDecoration AsBoxDecoration => box.Decoration switch
        {
            BoxDecoration boxDecoration => boxDecoration,
            ShapeDecoration shape when TryProject(shape) is { } projected => projected,
            _ => throw new InvalidOperationException("The current decoration is not a BoxDecoration."),
        };
    }

    private static BoxDecoration? TryProject(ShapeDecoration decoration)
    {
        BorderSide side = ShapeBorderGeometry.SideOrNone(decoration.Shape);
        BoxBorder? border = decoration.Shape as BoxBorder
                            ?? (side == BorderSide.None ? null : Border.FromBorderSide(side));
        BoxShape shape = ShapeBorderGeometry.BoxShapeOf(decoration.Shape);
        BorderRadius? radius = decoration.Shape is BoxBorder
            ? null
            : ShapeBorderGeometry.ResolveRadiusOrNull(decoration.Shape);
        if (radius is null && decoration.Shape is not BoxBorder)
        {
            return null;
        }

        return new BoxDecoration(
            Color: decoration.Color,
            Gradient: decoration.Gradient,
            Border: border,
            BorderRadius: shape == BoxShape.Circle ? null : radius,
            BoxShadows: decoration.Shadows is { Count: > 0 } ? decoration.Shadows : null,
            Image: decoration.Image,
            Shape: shape);
    }
}
