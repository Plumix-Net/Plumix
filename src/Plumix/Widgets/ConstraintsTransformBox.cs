using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (ConstraintsTransformBox)

public sealed class ConstraintsTransformBox : SingleChildRenderObjectWidget
{
    public ConstraintsTransformBox(
        BoxConstraintsTransform constraintsTransform,
        Widget? child = null,
        TextDirection? textDirection = null,
        AlignmentGeometry alignment = default,
        Clip clipBehavior = Clip.None,
        string debugTransformType = "",
        Key? key = null) : base(child, key)
    {
        ConstraintsTransform = constraintsTransform
            ?? throw new ArgumentNullException(nameof(constraintsTransform));
        TextDirection = textDirection;
        Alignment = alignment;
        ClipBehavior = clipBehavior;
        DebugTransformType = debugTransformType
            ?? throw new ArgumentNullException(nameof(debugTransformType));
    }

    public TextDirection? TextDirection { get; }

    public AlignmentGeometry Alignment { get; }

    public BoxConstraintsTransform ConstraintsTransform { get; }

    public Clip ClipBehavior { get; }

    public string DebugTransformType { get; }

    public static BoxConstraints Unmodified(BoxConstraints constraints)
    {
        return constraints;
    }

    public static BoxConstraints Unconstrained(BoxConstraints constraints)
    {
        return new BoxConstraints(
            MaxWidth: double.PositiveInfinity,
            MaxHeight: double.PositiveInfinity);
    }

    public static BoxConstraints WidthUnconstrained(BoxConstraints constraints)
    {
        return new BoxConstraints(
            MinWidth: 0,
            MaxWidth: double.PositiveInfinity,
            MinHeight: constraints.MinHeight,
            MaxHeight: constraints.MaxHeight);
    }

    public static BoxConstraints HeightUnconstrained(BoxConstraints constraints)
    {
        return new BoxConstraints(
            MinWidth: constraints.MinWidth,
            MaxWidth: constraints.MaxWidth,
            MinHeight: 0,
            MaxHeight: double.PositiveInfinity);
    }

    public static BoxConstraints MaxHeightUnconstrained(BoxConstraints constraints)
    {
        return constraints with { MaxHeight = double.PositiveInfinity };
    }

    public static BoxConstraints MaxWidthUnconstrained(BoxConstraints constraints)
    {
        return constraints with { MaxWidth = double.PositiveInfinity };
    }

    public static BoxConstraints MaxUnconstrained(BoxConstraints constraints)
    {
        return constraints with
        {
            MaxWidth = double.PositiveInfinity,
            MaxHeight = double.PositiveInfinity,
        };
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderConstraintsTransformBox(
            alignment: Alignment,
            textDirection: TextDirection ?? Directionality.MaybeOf(context),
            constraintsTransform: ConstraintsTransform,
            clipBehavior: ClipBehavior);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var transformBox = (RenderConstraintsTransformBox)renderObject;
        transformBox.TextDirection = TextDirection ?? Directionality.MaybeOf(context);
        transformBox.ConstraintsTransform = ConstraintsTransform;
        transformBox.Alignment = Alignment;
        transformBox.ClipBehavior = ClipBehavior;
    }
}
