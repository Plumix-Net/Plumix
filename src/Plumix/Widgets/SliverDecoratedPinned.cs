using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/decorated_sliver.dart
// flutter/packages/flutter/lib/src/widgets/pinned_header_sliver.dart

namespace Plumix.Widgets;

public sealed class DecoratedSliver : SingleChildRenderObjectWidget
{
    public DecoratedSliver(
        Decoration decoration,
        DecorationPosition position = DecorationPosition.Background,
        Widget? sliver = null,
        Key? key = null) : base(sliver, key)
    {
        Decoration = decoration ?? throw new ArgumentNullException(nameof(decoration));
        Position = position;
    }

    public Decoration Decoration { get; }

    public DecorationPosition Position { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderDecoratedSliver(
            decoration: Decoration,
            position: Position,
            configuration: ImageConfigurationUtils.CreateLocalImageConfiguration(context));
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var decoratedSliver = (RenderDecoratedSliver)renderObject;
        decoratedSliver.Decoration = Decoration;
        decoratedSliver.Position = Position;
        decoratedSliver.Configuration = ImageConfigurationUtils.CreateLocalImageConfiguration(context);
    }
}

public sealed class PinnedHeaderSliver : StatelessWidget
{
    public PinnedHeaderSliver(Widget? child = null, Key? key = null) : base(key)
    {
        Child = child;
    }

    public Widget? Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new PinnedHeaderSliverRenderWidget(
            child: new Semantics(
                child: Child,
                container: true,
                explicitChildNodes: true));
    }
}

internal sealed class PinnedHeaderSliverRenderWidget : SingleChildRenderObjectWidget
{
    public PinnedHeaderSliverRenderWidget(Widget? child = null) : base(child)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderPinnedHeaderSliver();
    }
}

internal sealed class RenderPinnedHeaderSliver : RenderSliverSingleBoxAdapter
{
    internal double ChildExtent
    {
        get
        {
            if (Child == null)
            {
                return 0.0;
            }

            return ConstraintsForSliver.Axis == Axis.Vertical
                ? Child.Size.Height
                : Child.Size.Width;
        }
    }

    public override double ChildMainAxisPosition(RenderObject child)
    {
        return 0.0;
    }

    public override void ShowOnScreen(
        RenderObject? descendant = null,
        Rect? rect = null,
        TimeSpan duration = default,
        Curve? curve = null)
    {
        Rect? localBounds = descendant != null
            ? RenderObject.TransformRect(descendant.GetTransformTo(this), rect ?? descendant.PaintBounds)
            : rect;
        Rect? trimmed = PersistentHeaderReveal.TrimForPinnedHeader(
            localBounds,
            PersistentHeaderReveal.EffectiveAxisDirection(ConstraintsForSliver),
            ChildExtent);
        base.ShowOnScreen(descendant: this, rect: trimmed, duration: duration, curve: curve);
    }

    /// <summary>
    /// While the header is scrolled far enough that it no longer occupies its full extent it is pinned
    /// against the leading edge, so its semantics leave the scrolling pane. At rest it is an ordinary
    /// scrolling child and carries no tag.
    /// </summary>
    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        if (HasSliverConstraints && Geometry.LayoutExtent < ChildExtent)
        {
            configuration.AddTagForChildren(RenderViewport.ExcludeFromScrolling);
        }
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        Child?.Layout(constraints.AsBoxConstraints(), parentUsesSize: true);
        if (Child != null)
        {
            ((BoxParentData)Child.parentData!).offset = default;
        }

        double childExtent = ChildExtent;
        double layoutExtent = Math.Clamp(
            childExtent - constraints.ScrollOffset,
            0.0,
            constraints.RemainingPaintExtent);
        double paintExtent = Math.Min(
            childExtent,
            Math.Max(0.0, constraints.RemainingPaintExtent - constraints.Overlap));
        Geometry = new SliverGeometry(
            ScrollExtent: childExtent,
            PaintOrigin: constraints.Overlap,
            PaintExtent: paintExtent,
            LayoutExtent: layoutExtent,
            MaxPaintExtent: childExtent,
            MaxScrollObstructionExtent: childExtent,
            CacheExtent: CalculateCacheOffset(constraints, from: 0.0, to: childExtent),
            HasVisualOverflow: true);
    }
}
