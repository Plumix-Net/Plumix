using Avalonia;
using Plumix.Rendering;
using Plumix.UI;

// C#-only test infrastructure: reads a Material surface off the render tree. Dart's `Material` paints
// through a RenderPhysicalModel (canvas type, no shape) or a RenderPhysicalShape (every other opaque
// type), so its color, shadow, elevation and shape are the physical model's properties.

namespace Plumix.Tests;

/// <summary>The paint-relevant properties of one Material surface.</summary>
internal sealed record MaterialSurfaceProbe(
    RenderBox RenderObject,
    Color Color,
    Color ShadowColor,
    double Elevation,
    ShapeBorder Shape,
    Clip ClipBehavior)
{
    /// <summary>The outline side of <see cref="Shape"/>, or <see cref="BorderSide.None"/>.</summary>
    public BorderSide Side => ShapeBorderGeometry.SideOrNone(Shape);

    /// <summary>The outline as a <see cref="Border"/>, or null when the shape has no visible side.</summary>
    public BoxBorder? Border =>
        Shape as BoxBorder ?? (Side == BorderSide.None ? null : Plumix.Rendering.Border.FromBorderSide(Side));

    /// <summary>The corner radius of <see cref="Shape"/> resolved left-to-right.</summary>
    public BorderRadius BorderRadius => ShapeBorderGeometry.ResolveRadius(Shape);

    /// <summary>
    /// Whether the physical model casts a visible shadow: it calls <c>drawShadow</c> for a non-zero
    /// elevation, and a transparent shadow color draws nothing.
    /// </summary>
    public bool HasShadow => Elevation != 0.0 && ShadowColor.Alpha != 0;

    /// <summary>The size of the physical model.</summary>
    public Size Size => RenderObject.Size;

    /// <summary>Every Material surface under <paramref name="root"/>, in depth-first paint order.</summary>
    public static List<MaterialSurfaceProbe> FindAll(RenderObject? root)
    {
        var result = new List<MaterialSurfaceProbe>();
        Visit(root, result);
        return result;
    }

    /// <summary>The first Material surface under <paramref name="root"/>, or null.</summary>
    public static MaterialSurfaceProbe? Find(RenderObject? root) => FindAll(root).FirstOrDefault();

    /// <summary>The surface for one physical model render object.</summary>
    public static MaterialSurfaceProbe? From(RenderObject renderObject)
    {
        return renderObject switch
        {
            RenderPhysicalShape shape => new MaterialSurfaceProbe(
                shape,
                shape.Color,
                shape.ShadowColor,
                shape.Elevation,
                shape.Clipper is ShapeBorderClipper clipper ? clipper.Shape : new RoundedRectangleBorder(),
                shape.ClipBehavior),
            RenderPhysicalModel model => new MaterialSurfaceProbe(
                model,
                model.Color,
                model.ShadowColor,
                model.Elevation,
                model.Shape == BoxShape.Circle
                    ? new CircleBorder()
                    : new RoundedRectangleBorder(borderRadius: model.BorderRadius ?? BorderRadius.Zero),
                model.ClipBehavior),
            _ => null,
        };
    }

    private static void Visit(RenderObject? node, List<MaterialSurfaceProbe> result)
    {
        if (node is null)
        {
            return;
        }

        if (From(node) is { } surface)
        {
            result.Add(surface);
        }

        node.VisitChildren(child => Visit(child, result));
    }
}
