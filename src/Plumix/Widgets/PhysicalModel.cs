using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (PhysicalModel)

public sealed class PhysicalModel : SingleChildRenderObjectWidget
{
    public PhysicalModel(
        Color color,
        Widget? child = null,
        BoxShape shape = BoxShape.Rectangle,
        Clip clipBehavior = Clip.None,
        BorderRadius? borderRadius = null,
        double elevation = 0.0,
        Color? shadowColor = null,
        Key? key = null) : base(child, key)
    {
        if (!double.IsFinite(elevation) || elevation < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Elevation must be finite and non-negative.");
        }

        Color = color;
        Shape = shape;
        ClipBehavior = clipBehavior;
        BorderRadius = borderRadius;
        Elevation = elevation;
        ShadowColor = shadowColor ?? Colors.Black;
    }

    public BoxShape Shape { get; }

    public Clip ClipBehavior { get; }

    public BorderRadius? BorderRadius { get; }

    public double Elevation { get; }

    public Color Color { get; }

    public Color ShadowColor { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderPhysicalModel(
            color: Color,
            shape: Shape,
            clipBehavior: ClipBehavior,
            borderRadius: BorderRadius,
            elevation: Elevation,
            shadowColor: ShadowColor);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var physicalModel = (RenderPhysicalModel)renderObject;
        physicalModel.Shape = Shape;
        physicalModel.ClipBehavior = ClipBehavior;
        physicalModel.BorderRadius = BorderRadius;
        physicalModel.Elevation = Elevation;
        physicalModel.Color = Color;
        physicalModel.ShadowColor = ShadowColor;
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (PhysicalShape)
public sealed class PhysicalShape : SingleChildRenderObjectWidget
{
    public PhysicalShape(
        CustomClipper<Path> clipper,
        Color color,
        Widget? child = null,
        Clip clipBehavior = Clip.None,
        double elevation = 0.0,
        Color? shadowColor = null,
        Key? key = null) : base(child, key)
    {
        if (!double.IsFinite(elevation) || elevation < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Elevation must be finite and non-negative.");
        }

        Clipper = clipper ?? throw new ArgumentNullException(nameof(clipper));
        ClipBehavior = clipBehavior;
        Elevation = elevation;
        Color = color;
        ShadowColor = shadowColor ?? Colors.Black;
    }

    public CustomClipper<Path> Clipper { get; }

    public Clip ClipBehavior { get; }

    public double Elevation { get; }

    public Color Color { get; }

    public Color ShadowColor { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderPhysicalShape(
            clipper: Clipper,
            clipBehavior: ClipBehavior,
            elevation: Elevation,
            color: Color,
            shadowColor: ShadowColor);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var physicalShape = (RenderPhysicalShape)renderObject;
        physicalShape.Clipper = Clipper;
        physicalShape.ClipBehavior = ClipBehavior;
        physicalShape.Elevation = Elevation;
        physicalShape.Color = Color;
        physicalShape.ShadowColor = ShadowColor;
    }
}
