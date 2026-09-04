using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver.dart

namespace Plumix.Widgets;

public sealed class SliverConstrainedCrossAxis : StatelessWidget
{
    public SliverConstrainedCrossAxis(double maxExtent, Widget sliver, Key? key = null) : base(key)
    {
        if (maxExtent < 0.0 || double.IsNaN(maxExtent))
        {
            throw new ArgumentOutOfRangeException(nameof(maxExtent), "maxExtent must be nonnegative.");
        }

        MaxExtent = maxExtent;
        Sliver = sliver ?? throw new ArgumentNullException(nameof(sliver));
    }

    public double MaxExtent { get; }

    public Widget Sliver { get; }

    public override Widget Build(BuildContext context)
    {
        return new SliverZeroFlexParentDataWidget(
            new SliverConstrainedCrossAxisRenderWidget(MaxExtent, Sliver));
    }
}

internal sealed class SliverZeroFlexParentDataWidget : ParentDataWidget<SliverPhysicalParentData>
{
    public SliverZeroFlexParentDataWidget(Widget sliver) : base(sliver)
    {
    }

    public override Type DebugTypicalAncestorWidgetType => typeof(SliverCrossAxisGroup);

    protected override void ApplyParentData(RenderObject renderObject)
    {
        var parentData = (SliverPhysicalParentData)renderObject.parentData!;
        if (parentData.CrossAxisFlex == 0)
        {
            return;
        }

        parentData.CrossAxisFlex = 0;
        renderObject.Parent?.MarkNeedsLayout();
    }
}

internal sealed class SliverConstrainedCrossAxisRenderWidget : SingleChildRenderObjectWidget
{
    public SliverConstrainedCrossAxisRenderWidget(double maxExtent, Widget sliver) : base(sliver)
    {
        MaxExtent = maxExtent;
    }

    public double MaxExtent { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverConstrainedCrossAxis(MaxExtent);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverConstrainedCrossAxis)renderObject).MaxExtent = MaxExtent;
    }
}

public sealed class SliverCrossAxisExpanded : ParentDataWidget<SliverPhysicalParentData>
{
    public SliverCrossAxisExpanded(int flex, Widget sliver, Key? key = null) : base(sliver, key)
    {
        if (flex <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(flex), "flex must be greater than zero.");
        }

        Flex = flex;
    }

    public int Flex { get; }

    public Widget Sliver => Child;

    public override Type DebugTypicalAncestorWidgetType => typeof(SliverCrossAxisGroup);

    protected override void ApplyParentData(RenderObject renderObject)
    {
        var parentData = (SliverPhysicalParentData)renderObject.parentData!;
        if (parentData.CrossAxisFlex == Flex)
        {
            return;
        }

        parentData.CrossAxisFlex = Flex;
        renderObject.Parent?.MarkNeedsLayout();
    }
}

public sealed class SliverCrossAxisGroup : MultiChildRenderObjectWidget
{
    public SliverCrossAxisGroup(IReadOnlyList<Widget> slivers, Key? key = null) : base(slivers, key)
    {
        Slivers = slivers ?? throw new ArgumentNullException(nameof(slivers));
    }

    public IReadOnlyList<Widget> Slivers { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverCrossAxisGroup();
    }
}

public sealed class SliverMainAxisGroup : MultiChildRenderObjectWidget
{
    public SliverMainAxisGroup(IReadOnlyList<Widget> slivers, Key? key = null) : base(slivers, key)
    {
        Slivers = slivers ?? throw new ArgumentNullException(nameof(slivers));
    }

    public IReadOnlyList<Widget> Slivers { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverMainAxisGroup();
    }
}
