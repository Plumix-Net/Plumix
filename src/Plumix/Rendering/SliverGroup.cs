using Avalonia;
using Plumix.Foundation;

// Dart parity sources:
// flutter/packages/flutter/lib/src/rendering/sliver_group.dart
// flutter/packages/flutter/lib/src/rendering/proxy_sliver.dart

namespace Plumix.Rendering;

public sealed class RenderSliverConstrainedCrossAxis : RenderProxySliver
{
    private double _maxExtent;

    public RenderSliverConstrainedCrossAxis(double maxExtent, RenderSliver? sliver = null) : base(sliver)
    {
        if (maxExtent < 0.0 || double.IsNaN(maxExtent))
        {
            throw new ArgumentOutOfRangeException(nameof(maxExtent), "maxExtent must be nonnegative.");
        }

        _maxExtent = maxExtent;
    }

    public double MaxExtent
    {
        get => _maxExtent;
        set
        {
            if (value < 0.0 || double.IsNaN(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "maxExtent must be nonnegative.");
            }

            if (_maxExtent == value)
            {
                return;
            }

            _maxExtent = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        if (Child == null)
        {
            throw new InvalidOperationException("RenderSliverConstrainedCrossAxis requires a child sliver.");
        }

        double crossAxisExtent = Math.Min(MaxExtent, constraints.CrossAxisExtent);
        Child.LayoutWithSliverConstraints(constraints with { CrossAxisExtent = crossAxisExtent });
        ((SliverPhysicalParentData)Child.parentData!).offset = default;
        Geometry = Child.Geometry with { CrossAxisExtent = crossAxisExtent };
    }
}

public sealed class RenderSliverCrossAxisGroup : RenderSliver, IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderSliver, SliverPhysicalParentData> _container;

    public RenderSliverCrossAxisGroup()
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderSliver, SliverPhysicalParentData>(this);
    }

    public int ChildCount => _container.ChildCount;

    public RenderSliver? FirstChild => _container.FirstChild;

    public RenderSliver? LastChild => _container.LastChild;

    public void Insert(RenderSliver child, RenderSliver? after = null) => _container.Insert(child, after);

    public void Move(RenderSliver child, RenderSliver? after = null) => _container.Move(child, after);

    public void Remove(RenderSliver child) => _container.Remove(child);

    public RenderSliver? ChildAfter(RenderSliver child) => _container.ChildAfter(child);

    public RenderSliver? ChildBefore(RenderSliver child) => _container.ChildBefore(child);

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) =>
        Insert((RenderSliver)child, (RenderSliver?)after);

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) =>
        Move((RenderSliver)child, (RenderSliver?)after);

    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderSliver)child);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not SliverPhysicalParentData parentData)
        {
            parentData = new SliverPhysicalParentData();
            child.parentData = parentData;
        }

        parentData.CrossAxisFlex ??= 1;
    }

    public override double ChildMainAxisPosition(RenderObject child)
    {
        return 0.0;
    }

    public override double ChildCrossAxisPosition(RenderObject child)
    {
        Point paintOffset = ((SliverPhysicalParentData)child.parentData!).offset;
        return ConstraintsForSliver.Axis == Axis.Vertical ? paintOffset.X : paintOffset.Y;
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (RenderSliver? child = FirstChild; child != null; child = ChildAfter(child))
        {
            visitor(child);
        }
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        double crossAxisExtent = constraints.CrossAxisExtent;
        if (!double.IsFinite(crossAxisExtent))
        {
            throw new InvalidOperationException("SliverCrossAxisGroup requires a finite cross-axis extent.");
        }

        int totalFlex = 0;
        double remainingExtent = crossAxisExtent;
        for (RenderSliver? child = FirstChild; child != null; child = ChildAfter(child))
        {
            var parentData = (SliverPhysicalParentData)child.parentData!;
            int flex = parentData.CrossAxisFlex ?? 0;
            if (flex == 0)
            {
                EnsureExtentAvailable(remainingExtent);
                child.LayoutWithSliverConstraints(constraints with { CrossAxisExtent = remainingExtent });
                double childCrossAxisExtent = child.Geometry.CrossAxisExtent
                    ?? throw new InvalidOperationException(
                        "A non-flex SliverCrossAxisGroup child must provide CrossAxisExtent geometry.");
                remainingExtent = Math.Max(0.0, remainingExtent - childCrossAxisExtent);
            }
            else
            {
                totalFlex += flex;
            }
        }

        double extentPerFlexValue = totalFlex == 0 ? 0.0 : remainingExtent / totalFlex;
        Geometry = default;
        for (RenderSliver? child = FirstChild; child != null; child = ChildAfter(child))
        {
            var parentData = (SliverPhysicalParentData)child.parentData!;
            int flex = parentData.CrossAxisFlex ?? 0;
            if (flex != 0)
            {
                double childExtent = extentPerFlexValue * flex;
                EnsureExtentAvailable(childExtent);
                child.LayoutWithSliverConstraints(constraints with { CrossAxisExtent = childExtent });
            }

            if (Geometry.ScrollExtent < child.Geometry.ScrollExtent)
            {
                Geometry = child.Geometry;
            }
        }

        double crossAxisOffset = 0.0;
        for (RenderSliver? child = FirstChild; child != null; child = ChildAfter(child))
        {
            var parentData = (SliverPhysicalParentData)child.parentData!;
            SliverGeometry childGeometry = child.Geometry;
            double remainingScrollExtent = Geometry.ScrollExtent - constraints.ScrollOffset;
            double paintCorrection = childGeometry.PaintExtent > remainingScrollExtent
                ? childGeometry.PaintExtent - remainingScrollExtent
                : 0.0;
            double childExtent = childGeometry.CrossAxisExtent
                ?? extentPerFlexValue * (parentData.CrossAxisFlex ?? 0);
            parentData.offset = constraints.Axis == Axis.Vertical
                ? new Point(crossAxisOffset, -paintCorrection)
                : new Point(-paintCorrection, crossAxisOffset);
            crossAxisOffset += childExtent;
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        for (RenderSliver? child = FirstChild; child != null; child = ChildAfter(child))
        {
            if (!child.Geometry.Visible)
            {
                continue;
            }

            var parentData = (SliverPhysicalParentData)child.parentData!;
            context.PaintChild(child, offset + parentData.offset);
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        for (RenderSliver? child = LastChild; child != null; child = ChildBefore(child))
        {
            var parentData = (SliverPhysicalParentData)child.parentData!;
            RenderSliver localChild = child;
            bool isHit = result.AddWithPaintOffset(
                parentData.offset,
                position,
                (hitResult, transformed) => localChild.HitTest(hitResult, transformed));
            if (isHit)
            {
                return true;
            }
        }

        return false;
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        for (RenderSliver? child = FirstChild; child != null; child = ChildAfter(child))
        {
            var parentData = (SliverPhysicalParentData)child.parentData!;
            visitor(child);
        }
    }

    private static void EnsureExtentAvailable(double extent)
    {
        if (extent <= 0.0)
        {
            throw new InvalidOperationException(
                "SliverCrossAxisGroup ran out of extent before a child could be laid out.");
        }
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => _container.DebugDescribeChildren();
}

public sealed class RenderSliverMainAxisGroup : RenderSliver, IRenderObjectContainer
{
    private const double PrecisionErrorTolerance = 0.0001;
    private readonly RenderBoxContainerDefaultsMixin<RenderSliver, SliverPhysicalParentData> _container;

    public RenderSliverMainAxisGroup()
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderSliver, SliverPhysicalParentData>(this);
    }

    public int ChildCount => _container.ChildCount;

    public RenderSliver? FirstChild => _container.FirstChild;

    public RenderSliver? LastChild => _container.LastChild;

    public void Insert(RenderSliver child, RenderSliver? after = null) => _container.Insert(child, after);

    public void Move(RenderSliver child, RenderSliver? after = null) => _container.Move(child, after);

    public void Remove(RenderSliver child) => _container.Remove(child);

    public RenderSliver? ChildAfter(RenderSliver child) => _container.ChildAfter(child);

    public RenderSliver? ChildBefore(RenderSliver child) => _container.ChildBefore(child);

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) =>
        Insert((RenderSliver)child, (RenderSliver?)after);

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) =>
        Move((RenderSliver)child, (RenderSliver?)after);

    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderSliver)child);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not SliverPhysicalParentData)
        {
            child.parentData = new SliverPhysicalParentData();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (RenderSliver? child = FirstChild; child != null; child = ChildAfter(child))
        {
            visitor(child);
        }
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        double scrollOffset = 0.0;
        double layoutOffset = 0.0;
        double maxPaintExtent = 0.0;
        double paintOffset = constraints.Overlap;
        double maxScrollObstructionExtent = 0.0;
        double cacheOrigin = constraints.CacheOrigin;
        double remainingCacheExtent = constraints.RemainingCacheExtent;

        RenderSliver? leadingChild = constraints.GrowthDirection == GrowthDirection.Forward
            ? FirstChild
            : LastChild;
        Func<RenderSliver, RenderSliver?> advance = constraints.GrowthDirection == GrowthDirection.Forward
            ? ChildAfter
            : ChildBefore;

        for (RenderSliver? child = leadingChild; child != null; child = advance(child))
        {
            double beforeOffsetPaintExtent = CalculatePaintOffset(constraints, from: 0.0, to: scrollOffset);
            double childScrollOffset = Math.Max(0.0, constraints.ScrollOffset - scrollOffset);
            double correctedCacheOrigin = Math.Max(cacheOrigin, -childScrollOffset);
            double cacheExtentCorrection = cacheOrigin - correctedCacheOrigin;
            child.LayoutWithSliverConstraints(constraints with
            {
                ScrollOffset = childScrollOffset,
                CacheOrigin = correctedCacheOrigin,
                Overlap = Math.Max(0.0, FixPrecisionError(paintOffset - beforeOffsetPaintExtent)),
                RemainingPaintExtent = FixPrecisionError(
                    constraints.RemainingPaintExtent - beforeOffsetPaintExtent),
                RemainingCacheExtent = Math.Max(
                    0.0,
                    FixPrecisionError(remainingCacheExtent + cacheExtentCorrection)),
                PrecedingScrollExtent = scrollOffset + constraints.PrecedingScrollExtent,
            });

            SliverGeometry childGeometry = child.Geometry;
            if (childGeometry.ScrollOffsetCorrection is double correction)
            {
                Geometry = new SliverGeometry(ScrollOffsetCorrection: correction);
                return;
            }

            double childPaintOffset = layoutOffset + childGeometry.PaintOrigin;
            var parentData = (SliverPhysicalParentData)child.parentData!;
            parentData.offset = constraints.Axis == Axis.Vertical
                ? new Point(0.0, childPaintOffset)
                : new Point(childPaintOffset, 0.0);
            scrollOffset += childGeometry.ScrollExtent;
            layoutOffset += childGeometry.LayoutExtent;
            maxPaintExtent += childGeometry.MaxPaintExtent;
            maxScrollObstructionExtent += childGeometry.MaxScrollObstructionExtent;
            paintOffset = Math.Max(childPaintOffset + childGeometry.PaintExtent, paintOffset);
            if (childGeometry.CacheExtent != 0.0)
            {
                remainingCacheExtent = FixPrecisionError(
                    remainingCacheExtent - childGeometry.CacheExtent - cacheExtentCorrection);
                cacheOrigin = Math.Min(correctedCacheOrigin + childGeometry.CacheExtent, 0.0);
            }

            if (advance(child) != null && double.IsPositiveInfinity(maxPaintExtent))
            {
                throw new InvalidOperationException("An unreachable sliver follows a sliver with infinite extent.");
            }
        }

        double remainingExtent = Math.Max(0.0, scrollOffset - constraints.ScrollOffset);
        if (paintOffset > remainingExtent)
        {
            bool pinnedChildrenOverflow =
                maxScrollObstructionExtent > remainingExtent - constraints.Overlap;
            double paintCorrection = paintOffset - remainingExtent;
            paintOffset = remainingExtent;
            for (RenderSliver? child = FirstChild; child != null; child = ChildAfter(child))
            {
                SliverGeometry childGeometry = child.Geometry;
                var parentData = (SliverPhysicalParentData)child.parentData!;
                double childMainAxisPaintOffset = constraints.Axis == Axis.Vertical
                    ? parentData.offset.Y
                    : parentData.offset.X;
                double childPaintEnd = childMainAxisPaintOffset + childGeometry.PaintExtent;
                bool childIsPinned = childGeometry.MaxScrollObstructionExtent > 0.0;
                if (childPaintEnd > remainingExtent || pinnedChildrenOverflow && childIsPinned)
                {
                    parentData.offset = constraints.Axis == Axis.Vertical
                        ? new Point(0.0, parentData.offset.Y - paintCorrection)
                        : new Point(parentData.offset.X - paintCorrection, 0.0);
                }
            }
        }

        double cacheExtent = CalculateCacheOffset(
            constraints,
            from: Math.Min(constraints.ScrollOffset, 0.0),
            to: scrollOffset);
        double paintExtent = Math.Clamp(paintOffset, 0.0, constraints.RemainingPaintExtent);
        Geometry = new SliverGeometry(
            ScrollExtent: scrollOffset,
            PaintExtent: paintExtent,
            LayoutExtent: paintExtent,
            CacheExtent: cacheExtent,
            MaxPaintExtent: maxPaintExtent,
            HasVisualOverflow: scrollOffset > constraints.RemainingPaintExtent
                || constraints.ScrollOffset > 0.0);

        for (RenderSliver? child = leadingChild; child != null; child = advance(child))
        {
            var parentData = (SliverPhysicalParentData)child.parentData!;
            AxisDirection effectiveDirection = ApplyGrowthDirectionToAxisDirection(
                constraints.AxisDirection,
                constraints.GrowthDirection);
            parentData.offset = effectiveDirection switch
            {
                AxisDirection.Up => new Point(
                    0.0,
                    paintExtent - parentData.offset.Y - child.Geometry.PaintExtent),
                AxisDirection.Left => new Point(
                    paintExtent - parentData.offset.X - child.Geometry.PaintExtent,
                    0.0),
                _ => parentData.offset,
            };
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        for (RenderSliver? child = LastChild; child != null; child = ChildBefore(child))
        {
            if (!child.Geometry.Visible)
            {
                continue;
            }

            var parentData = (SliverPhysicalParentData)child.parentData!;
            context.PaintChild(child, offset + parentData.offset);
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        for (RenderSliver? child = FirstChild; child != null; child = ChildAfter(child))
        {
            var parentData = (SliverPhysicalParentData)child.parentData!;
            RenderSliver localChild = child;
            bool isHit = result.AddWithPaintOffset(
                parentData.offset,
                position,
                (hitResult, transformed) => localChild.HitTest(hitResult, transformed));
            if (isHit)
            {
                return true;
            }
        }

        return false;
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        for (RenderSliver? child = FirstChild; child != null; child = ChildAfter(child))
        {
            if (!child.Geometry.Visible && child.Geometry.CacheExtent <= 0.0)
            {
                continue;
            }

            var parentData = (SliverPhysicalParentData)child.parentData!;
            visitor(child);
        }
    }

    public override double ChildMainAxisPosition(RenderObject child)
    {
        var sliver = (RenderSliver)child;
        Point paintOffset = ((SliverPhysicalParentData)child.parentData!).offset;
        AxisDirection effectiveDirection = ApplyGrowthDirectionToAxisDirection(
            sliver.ConstraintsForSliver.AxisDirection,
            sliver.ConstraintsForSliver.GrowthDirection);
        return effectiveDirection switch
        {
            AxisDirection.Down => paintOffset.Y,
            AxisDirection.Right => paintOffset.X,
            AxisDirection.Up => Geometry.PaintExtent - sliver.Geometry.PaintExtent - paintOffset.Y,
            AxisDirection.Left => Geometry.PaintExtent - sliver.Geometry.PaintExtent - paintOffset.X,
            _ => paintOffset.Y,
        };
    }

    public override double ChildCrossAxisPosition(RenderObject child)
    {
        return 0.0;
    }

    public override double? ChildScrollOffset(RenderObject renderChild)
    {
        var child = (RenderSliver)renderChild;
        if (!ReferenceEquals(child.Parent, this))
        {
            throw new ArgumentException("The child does not belong to this group.", nameof(renderChild));
        }

        double obstructionExtent = MaxScrollObstructionExtentBefore(child);
        double offset = 0.0;
        if (ConstraintsForSliver.GrowthDirection == GrowthDirection.Forward)
        {
            for (RenderSliver? current = ChildBefore(child); current != null; current = ChildBefore(current))
            {
                offset += current.Geometry.ScrollExtent;
            }
        }
        else
        {
            for (RenderSliver? current = ChildAfter(child); current != null; current = ChildAfter(current))
            {
                offset -= current.Geometry.ScrollExtent;
            }
        }

        return offset - obstructionExtent;
    }

    private double MaxScrollObstructionExtentBefore(RenderSliver child)
    {
        double pinnedExtent = 0.0;
        if (child.ConstraintsForSliver.GrowthDirection == GrowthDirection.Forward)
        {
            for (RenderSliver? current = FirstChild;
                 current != null && !ReferenceEquals(current, child);
                 current = ChildAfter(current))
            {
                pinnedExtent += current.Geometry.MaxScrollObstructionExtent;
            }
        }
        else
        {
            for (RenderSliver? current = LastChild;
                 current != null && !ReferenceEquals(current, child);
                 current = ChildBefore(current))
            {
                pinnedExtent += current.Geometry.MaxScrollObstructionExtent;
            }
        }

        return pinnedExtent;
    }

    private static AxisDirection ApplyGrowthDirectionToAxisDirection(
        AxisDirection axisDirection,
        GrowthDirection growthDirection)
    {
        if (growthDirection == GrowthDirection.Forward)
        {
            return axisDirection;
        }

        return axisDirection switch
        {
            AxisDirection.Up => AxisDirection.Down,
            AxisDirection.Right => AxisDirection.Left,
            AxisDirection.Down => AxisDirection.Up,
            AxisDirection.Left => AxisDirection.Right,
            _ => axisDirection,
        };
    }

    private static double FixPrecisionError(double number)
    {
        return Math.Abs(number) < PrecisionErrorTolerance ? 0.0 : number;
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => _container.DebugDescribeChildren();
}
