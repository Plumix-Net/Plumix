using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver_resizing_header.dart

namespace Plumix.Widgets;

/// <summary>
/// A sliver that is pinned to the start of its <see cref="CustomScrollView"/> and reacts to
/// scrolling by resizing between the intrinsic sizes of its min and max extent prototypes.
/// </summary>
/// <remarks>
/// The minimum and maximum sizes of this sliver's child are defined by
/// <see cref="MinExtentPrototype"/> and <see cref="MaxExtentPrototype"/>, two widgets that are built
/// but not displayed. If a prototype is not specified, the minimum extent is 0 and the maximum
/// extent is the child's intrinsic size.
/// </remarks>
public sealed class SliverResizingHeader : StatelessWidget
{
    /// <summary>Create a pinned header sliver that reacts to scrolling by resizing.</summary>
    public SliverResizingHeader(
        Widget? minExtentPrototype = null,
        Widget? maxExtentPrototype = null,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        MinExtentPrototype = minExtentPrototype;
        MaxExtentPrototype = maxExtentPrototype;
        Child = child;
    }

    /// <summary>
    /// Laid out once to define the minimum size of this sliver along the
    /// <see cref="CustomScrollView.ScrollDirection"/> axis. If null, the minimum size is 0.
    /// </summary>
    public Widget? MinExtentPrototype { get; }

    /// <summary>
    /// Laid out once to define the maximum size of this sliver along the
    /// <see cref="CustomScrollView.ScrollDirection"/> axis. If null, the maximum extent is the
    /// child's intrinsic size.
    /// </summary>
    public Widget? MaxExtentPrototype { get; }

    /// <summary>The widget contained by this sliver.</summary>
    public Widget? Child { get; }

    private static Widget? ExcludeFocus(Widget? extentPrototype)
    {
        return extentPrototype != null ? new ExcludeFocus(child: extentPrototype) : null;
    }

    public override Widget Build(BuildContext context)
    {
        return new SliverResizingHeaderRenderObjectWidget(
            minExtentPrototype: ExcludeFocus(MinExtentPrototype),
            maxExtentPrototype: ExcludeFocus(MaxExtentPrototype),
            child: new Semantics(
                container: true,
                explicitChildNodes: true,
                child: Child ?? SizedBox.Shrink()));
    }
}

/// <summary>Dart's private <c>_Slot</c>.</summary>
internal enum SliverResizingHeaderSlot
{
    MinExtent,
    MaxExtent,
    Child,
}

/// <summary>Dart's private <c>_SliverResizingHeader</c>.</summary>
internal sealed class SliverResizingHeaderRenderObjectWidget
    : SlottedMultiChildRenderObjectWidget<SliverResizingHeaderSlot>
{
    private static readonly IReadOnlyList<SliverResizingHeaderSlot> AllSlots =
    [
        SliverResizingHeaderSlot.MinExtent,
        SliverResizingHeaderSlot.MaxExtent,
        SliverResizingHeaderSlot.Child,
    ];

    public SliverResizingHeaderRenderObjectWidget(
        Widget? minExtentPrototype,
        Widget? maxExtentPrototype,
        Widget child)
    {
        MinExtentPrototype = minExtentPrototype;
        MaxExtentPrototype = maxExtentPrototype;
        Child = child;
    }

    public Widget? MinExtentPrototype { get; }

    public Widget? MaxExtentPrototype { get; }

    public Widget Child { get; }

    public override IReadOnlyList<SliverResizingHeaderSlot> Slots => AllSlots;

    public override Widget? ChildForSlot(SliverResizingHeaderSlot slot)
    {
        return slot switch
        {
            SliverResizingHeaderSlot.MinExtent => MinExtentPrototype,
            SliverResizingHeaderSlot.MaxExtent => MaxExtentPrototype,
            _ => Child,
        };
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverResizingHeader();
    }
}

/// <summary>Dart's private <c>_RenderSliverResizingHeader</c>.</summary>
/// <remarks>
/// Dart mixes in <c>SlottedContainerRenderObjectMixin</c> and <c>RenderSliverHelpers</c>. C# has no
/// mixins: the slot map, <c>childForSlot</c>, <c>children</c>, <c>visitChildren</c> and
/// <c>debugDescribeChildren</c> of the first are spelled out here (attach, detach and redepth walk
/// <see cref="VisitChildren"/> in the base class), and the second is the static
/// <see cref="RenderSliverHelpers"/>.
/// </remarks>
internal sealed class RenderSliverResizingHeader : RenderSliver, ISlottedRenderObjectContainer
{
    private readonly Dictionary<SliverResizingHeaderSlot, RenderBox> _slotToChild = [];

    public RenderBox? MinExtentPrototype => ChildForSlot(SliverResizingHeaderSlot.MinExtent);

    public RenderBox? MaxExtentPrototype => ChildForSlot(SliverResizingHeaderSlot.MaxExtent);

    public RenderBox? Child => ChildForSlot(SliverResizingHeaderSlot.Child);

    /// <summary>
    /// Dart's <c>children</c> override: the prototypes first, then the child, skipping the empty
    /// slots.
    /// </summary>
    private IEnumerable<RenderBox> Children
    {
        get
        {
            if (MinExtentPrototype is { } minExtentPrototype)
            {
                yield return minExtentPrototype;
            }

            if (MaxExtentPrototype is { } maxExtentPrototype)
            {
                yield return maxExtentPrototype;
            }

            if (Child is { } child)
            {
                yield return child;
            }
        }
    }

    private RenderBox? ChildForSlot(SliverResizingHeaderSlot slot) =>
        _slotToChild.TryGetValue(slot, out RenderBox? child) ? child : null;

    /// <summary>Dart's <c>SlottedContainerRenderObjectMixin._setChild</c>.</summary>
    void ISlottedRenderObjectContainer.SetChild(RenderObject? child, object slot)
    {
        var resolvedSlot = (SliverResizingHeaderSlot)slot;
        if (_slotToChild.TryGetValue(resolvedSlot, out RenderBox? oldChild))
        {
            DropChild(oldChild);
            _slotToChild.Remove(resolvedSlot);
        }

        if (child != null)
        {
            _slotToChild[resolvedSlot] = (RenderBox)child;
            AdoptChild(child);
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        foreach (RenderBox child in Children)
        {
            visitor(child);
        }
    }

    private double BoxExtent(RenderBox box)
    {
        DebugAssertions.Assert(box.HasSize);
        return Constraints.Axis switch
        {
            Axis.Vertical => box.Size.Height,
            _ => box.Size.Width,
        };
    }

    private double ChildExtent => Child == null ? 0 : BoxExtent(Child);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not SliverPhysicalParentData)
        {
            child.parentData = new SliverPhysicalParentData();
        }
    }

    /// <summary>Dart's <c>setChildParentData</c>; like Dart, nothing in this file calls it.</summary>
    internal void SetChildParentData(
        RenderObject child,
        SliverConstraints constraints,
        SliverGeometry geometry)
    {
        var childParentData = (SliverPhysicalParentData)child.parentData!;
        AxisDirection direction = ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
            constraints.AxisDirection,
            constraints.GrowthDirection);
        childParentData.PaintOffset = direction switch
        {
            AxisDirection.Up => new Point(
                0.0,
                -(geometry.ScrollExtent - (geometry.PaintExtent + constraints.ScrollOffset))),
            AxisDirection.Right => new Point(-constraints.ScrollOffset, 0.0),
            AxisDirection.Down => new Point(0.0, -constraints.ScrollOffset),
            _ => new Point(
                -(geometry.ScrollExtent - (geometry.PaintExtent + constraints.ScrollOffset)),
                0.0),
        };
    }

    public override double ChildMainAxisPosition(RenderObject child) => 0;

    protected override void PerformLayout()
    {
        SliverConstraints constraints = Constraints;
        BoxConstraints prototypeBoxConstraints = constraints.AsBoxConstraints();

        double minExtent = 0;
        if (MinExtentPrototype != null)
        {
            MinExtentPrototype.Layout(prototypeBoxConstraints, parentUsesSize: true);
            minExtent = BoxExtent(MinExtentPrototype);
        }

        double maxExtent;
        if (MaxExtentPrototype != null)
        {
            MaxExtentPrototype.Layout(prototypeBoxConstraints, parentUsesSize: true);
            maxExtent = BoxExtent(MaxExtentPrototype);
        }
        else
        {
            Size childSize = Child!.GetDryLayout(prototypeBoxConstraints);
            maxExtent = constraints.Axis switch
            {
                Axis.Vertical => childSize.Height,
                _ => childSize.Width,
            };
        }

        double scrollOffset = constraints.ScrollOffset;
        double shrinkOffset = Math.Min(scrollOffset, maxExtent);
        BoxConstraints boxConstraints = constraints.AsBoxConstraints(
            minExtent: minExtent,
            maxExtent: Math.Max(minExtent, maxExtent - shrinkOffset));
        Child?.Layout(boxConstraints, parentUsesSize: true);

        double remainingPaintExtent = constraints.RemainingPaintExtent;
        double layoutExtent = Math.Min(ChildExtent, maxExtent - scrollOffset);
        Geometry = new SliverGeometry(
            ScrollExtent: maxExtent,
            PaintOrigin: constraints.Overlap,
            PaintExtent: Math.Min(ChildExtent, remainingPaintExtent),
            LayoutExtent: Math.Clamp(layoutExtent, 0, remainingPaintExtent),
            MaxPaintExtent: ChildExtent,
            MaxScrollObstructionExtent: minExtent,
            CacheExtent: CalculateCacheOffset(constraints, from: 0.0, to: ChildExtent),
            // Conservatively say we do have overflow to avoid complexity.
            HasVisualOverflow: true);
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        var childParentData = (SliverPhysicalParentData)child.parentData!;
        childParentData.ApplyPaintTransform(transform);
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child != null && Geometry!.Visible)
        {
            var childParentData = (SliverPhysicalParentData)Child.parentData!;
            context.PaintChild(Child, offset + childParentData.PaintOffset);
        }
    }

    protected override bool HitTestChildren(
        SliverHitTestResult result,
        double mainAxisPosition,
        double crossAxisPosition)
    {
        DebugAssertions.Assert(Geometry!.HitTestExtent > 0.0);
        if (Child != null)
        {
            return this.HitTestBoxChild(
                BoxHitTestResult.Wrap(result),
                Child,
                mainAxisPosition: mainAxisPosition,
                crossAxisPosition: crossAxisPosition);
        }

        return false;
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        if (Geometry != null && Geometry.LayoutExtent < ChildExtent)
        {
            configuration.AddTagForChildren(RenderViewport.ExcludeFromScrolling);
        }
    }

    /// <summary>Dart's <c>SlottedContainerRenderObjectMixin.debugDescribeChildren</c>.</summary>
    public override List<DiagnosticsNode> DebugDescribeChildren()
    {
        var value = new List<DiagnosticsNode>();
        foreach (RenderBox child in Children)
        {
            SliverResizingHeaderSlot slot = _slotToChild.First(entry => ReferenceEquals(entry.Value, child)).Key;
            value.Add(child.ToDiagnosticsNode(name: DebugNameForSlot(slot)));
        }

        return value;
    }

    /// <summary>Dart's <c>debugNameForSlot</c>: an enum slot's <c>name</c>.</summary>
    private static string DebugNameForSlot(SliverResizingHeaderSlot slot) => slot switch
    {
        SliverResizingHeaderSlot.MinExtent => "minExtent",
        SliverResizingHeaderSlot.MaxExtent => "maxExtent",
        _ => "child",
    };
}
