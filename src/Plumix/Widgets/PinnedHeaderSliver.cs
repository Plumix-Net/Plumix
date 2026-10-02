using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/pinned_header_sliver.dart

namespace Plumix.Widgets;

/// <summary>A sliver that keeps its widget child at the top of a <see cref="CustomScrollView"/>.</summary>
/// <remarks>
/// This sliver is preferable to the general purpose <see cref="SliverPersistentHeader"/> for its
/// relatively narrow use case because there's no need to create a
/// <see cref="SliverPersistentHeaderDelegate"/> or to predict the header's size.
/// </remarks>
public sealed class PinnedHeaderSliver : StatelessWidget
{
    /// <summary>
    /// Creates a sliver whose <see cref="Widget"/> child appears at the top of a <see cref="CustomScrollView"/>.
    /// </summary>
    public PinnedHeaderSliver(Widget? child = null, Key? key = null) : base(key)
    {
        Child = child;
    }

    /// <summary>The widget contained by this sliver.</summary>
    public Widget? Child { get; }

    public override Widget Build(BuildContext context) => new PinnedHeaderSliverRenderObjectWidget(
        child: new Semantics(container: true, explicitChildNodes: true, child: Child));
}

/// <summary>Dart's private <c>_PinnedHeaderSliver</c>.</summary>
internal sealed class PinnedHeaderSliverRenderObjectWidget : SingleChildRenderObjectWidget
{
    public PinnedHeaderSliverRenderObjectWidget(Widget? child = null) : base(child)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderPinnedHeaderSliver();
    }
}

/// <summary>Dart's private <c>_RenderPinnedHeaderSliver</c>.</summary>
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

            DebugAssertions.Assert(Child.HasSize);
            return Constraints.Axis switch
            {
                Axis.Vertical => Child.Size.Height,
                _ => Child.Size.Width,
            };
        }
    }

    public override double ChildMainAxisPosition(RenderObject child) => 0;

    protected override void PerformLayout()
    {
        SliverConstraints constraints = Constraints;
        Child?.Layout(constraints.AsBoxConstraints(), parentUsesSize: true);

        double layoutExtent = Math.Clamp(
            ChildExtent - constraints.ScrollOffset,
            0,
            constraints.RemainingPaintExtent);
        double paintExtent = Math.Min(
            ChildExtent,
            constraints.RemainingPaintExtent - constraints.Overlap);
        Geometry = new SliverGeometry(
            ScrollExtent: ChildExtent,
            PaintOrigin: constraints.Overlap,
            PaintExtent: paintExtent,
            LayoutExtent: layoutExtent,
            MaxPaintExtent: ChildExtent,
            MaxScrollObstructionExtent: ChildExtent,
            CacheExtent: CalculateCacheOffset(constraints, from: 0.0, to: ChildExtent),
            // Conservatively say we do have overflow to avoid complexity.
            HasVisualOverflow: true);
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        if (Geometry != null && Geometry.LayoutExtent < ChildExtent)
        {
            configuration.AddTagForChildren(RenderViewport.ExcludeFromScrolling);
        }
    }
}
