using Avalonia;
using Avalonia.Media;
using Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/sliver.dart (approximate)

namespace Plumix.Rendering;

public readonly record struct SliverConstraints(
    Axis Axis,
    double ScrollOffset,
    double RemainingPaintExtent,
    double CrossAxisExtent,
    double ViewportMainAxisExtent,
    double CacheOrigin = 0,
    double RemainingCacheExtent = 0,
    AxisDirection AxisDirection = AxisDirection.Down,
    GrowthDirection GrowthDirection = GrowthDirection.Forward);

public readonly record struct SliverGeometry(
    double ScrollExtent = 0,
    double PaintExtent = 0,
    double LayoutExtent = 0,
    double MaxPaintExtent = 0,
    double CacheExtent = 0,
    double ScrollOffsetCorrection = 0,
    bool HasVisualOverflow = false);

public readonly record struct SliverGridGeometry(
    double ScrollOffset,
    double CrossAxisOffset,
    double MainAxisExtent,
    double CrossAxisExtent)
{
    public double TrailingScrollOffset => ScrollOffset + MainAxisExtent;

    public BoxConstraints GetBoxConstraints(SliverConstraints constraints)
    {
        if (constraints.Axis == Axis.Vertical)
        {
            return new BoxConstraints(
                MinWidth: CrossAxisExtent,
                MaxWidth: CrossAxisExtent,
                MinHeight: MainAxisExtent,
                MaxHeight: MainAxisExtent);
        }

        return new BoxConstraints(
            MinWidth: MainAxisExtent,
            MaxWidth: MainAxisExtent,
            MinHeight: CrossAxisExtent,
            MaxHeight: CrossAxisExtent);
    }
}

public abstract class SliverGridLayout
{
    public abstract int GetMinChildIndexForScrollOffset(double scrollOffset);

    public abstract int GetMaxChildIndexForScrollOffset(double scrollOffset);

    public abstract SliverGridGeometry GetGeometryForChildIndex(int index);

    public abstract double ComputeMaxScrollOffset(int childCount);
}

public sealed class SliverGridRegularTileLayout : SliverGridLayout
{
    public SliverGridRegularTileLayout(
        int crossAxisCount,
        double mainAxisStride,
        double crossAxisStride,
        double childMainAxisExtent,
        double childCrossAxisExtent,
        bool reverseCrossAxis)
    {
        if (crossAxisCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(crossAxisCount), "crossAxisCount must be greater than 0.");
        }

        if (mainAxisStride < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mainAxisStride), "mainAxisStride cannot be negative.");
        }

        if (crossAxisStride < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(crossAxisStride), "crossAxisStride cannot be negative.");
        }

        if (childMainAxisExtent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(childMainAxisExtent), "childMainAxisExtent cannot be negative.");
        }

        if (childCrossAxisExtent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(childCrossAxisExtent), "childCrossAxisExtent cannot be negative.");
        }

        CrossAxisCount = crossAxisCount;
        MainAxisStride = mainAxisStride;
        CrossAxisStride = crossAxisStride;
        ChildMainAxisExtent = childMainAxisExtent;
        ChildCrossAxisExtent = childCrossAxisExtent;
        ReverseCrossAxis = reverseCrossAxis;
    }

    public int CrossAxisCount { get; }

    public double MainAxisStride { get; }

    public double CrossAxisStride { get; }

    public double ChildMainAxisExtent { get; }

    public double ChildCrossAxisExtent { get; }

    public bool ReverseCrossAxis { get; }

    public override int GetMinChildIndexForScrollOffset(double scrollOffset)
    {
        return MainAxisStride > 0.0001
            ? CrossAxisCount * (int)Math.Floor(scrollOffset / MainAxisStride)
            : 0;
    }

    public override int GetMaxChildIndexForScrollOffset(double scrollOffset)
    {
        if (MainAxisStride > 0)
        {
            var mainAxisCount = (int)Math.Ceiling(scrollOffset / MainAxisStride);
            return Math.Max(0, CrossAxisCount * mainAxisCount - 1);
        }

        return 0;
    }

    public override SliverGridGeometry GetGeometryForChildIndex(int index)
    {
        var crossAxisStart = (index % CrossAxisCount) * CrossAxisStride;
        return new SliverGridGeometry(
            ScrollOffset: (index / CrossAxisCount) * MainAxisStride,
            CrossAxisOffset: OffsetFromStartInCrossAxis(crossAxisStart),
            MainAxisExtent: ChildMainAxisExtent,
            CrossAxisExtent: ChildCrossAxisExtent);
    }

    public override double ComputeMaxScrollOffset(int childCount)
    {
        if (childCount == 0)
        {
            return 0;
        }

        var mainAxisCount = ((childCount - 1) / CrossAxisCount) + 1;
        var mainAxisSpacing = MainAxisStride - ChildMainAxisExtent;
        return MainAxisStride * mainAxisCount - mainAxisSpacing;
    }

    private double OffsetFromStartInCrossAxis(double crossAxisStart)
    {
        if (!ReverseCrossAxis)
        {
            return crossAxisStart;
        }

        return CrossAxisCount * CrossAxisStride
               - crossAxisStart
               - ChildCrossAxisExtent
               - (CrossAxisStride - ChildCrossAxisExtent);
    }
}

public abstract class SliverGridDelegate
{
    public abstract SliverGridLayout GetLayout(SliverConstraints constraints);

    public abstract bool ShouldRelayout(SliverGridDelegate oldDelegate);
}

public sealed class SliverGridDelegateWithFixedCrossAxisCount : SliverGridDelegate
{
    public SliverGridDelegateWithFixedCrossAxisCount(
        int crossAxisCount,
        double mainAxisSpacing = 0,
        double crossAxisSpacing = 0,
        double childAspectRatio = 1,
        double? mainAxisExtent = null)
    {
        if (crossAxisCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(crossAxisCount), "crossAxisCount must be greater than 0.");
        }

        if (mainAxisSpacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mainAxisSpacing), "mainAxisSpacing cannot be negative.");
        }

        if (crossAxisSpacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(crossAxisSpacing), "crossAxisSpacing cannot be negative.");
        }

        if (childAspectRatio <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(childAspectRatio), "childAspectRatio must be greater than 0.");
        }

        if (mainAxisExtent.HasValue && mainAxisExtent.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mainAxisExtent), "mainAxisExtent cannot be negative.");
        }

        CrossAxisCount = crossAxisCount;
        MainAxisSpacing = mainAxisSpacing;
        CrossAxisSpacing = crossAxisSpacing;
        ChildAspectRatio = childAspectRatio;
        MainAxisExtent = mainAxisExtent;
    }

    public int CrossAxisCount { get; }

    public double MainAxisSpacing { get; }

    public double CrossAxisSpacing { get; }

    public double ChildAspectRatio { get; }

    public double? MainAxisExtent { get; }

    public override SliverGridLayout GetLayout(SliverConstraints constraints)
    {
        var usableCrossAxisExtent = Math.Max(
            0,
            constraints.CrossAxisExtent - CrossAxisSpacing * (CrossAxisCount - 1));
        var childCrossAxisExtent = usableCrossAxisExtent / CrossAxisCount;
        var childMainAxisExtent = MainAxisExtent ?? childCrossAxisExtent / ChildAspectRatio;
        return new SliverGridRegularTileLayout(
            crossAxisCount: CrossAxisCount,
            mainAxisStride: childMainAxisExtent + MainAxisSpacing,
            crossAxisStride: childCrossAxisExtent + CrossAxisSpacing,
            childMainAxisExtent: childMainAxisExtent,
            childCrossAxisExtent: childCrossAxisExtent,
            reverseCrossAxis: false);
    }

    public override bool ShouldRelayout(SliverGridDelegate oldDelegate)
    {
        if (oldDelegate is not SliverGridDelegateWithFixedCrossAxisCount old)
        {
            return true;
        }

        return old.CrossAxisCount != CrossAxisCount
               || Math.Abs(old.MainAxisSpacing - MainAxisSpacing) > 0.0001
               || Math.Abs(old.CrossAxisSpacing - CrossAxisSpacing) > 0.0001
               || Math.Abs(old.ChildAspectRatio - ChildAspectRatio) > 0.0001
               || NullableDoubleChanged(old.MainAxisExtent, MainAxisExtent);
    }

    private static bool NullableDoubleChanged(double? lhs, double? rhs)
    {
        if (!lhs.HasValue && !rhs.HasValue)
        {
            return false;
        }

        if (lhs.HasValue != rhs.HasValue)
        {
            return true;
        }

        return Math.Abs(lhs!.Value - rhs!.Value) > 0.0001;
    }
}

public sealed class SliverGridDelegateWithMaxCrossAxisExtent : SliverGridDelegate
{
    public SliverGridDelegateWithMaxCrossAxisExtent(
        double maxCrossAxisExtent,
        double mainAxisSpacing = 0,
        double crossAxisSpacing = 0,
        double childAspectRatio = 1,
        double? mainAxisExtent = null)
    {
        if (maxCrossAxisExtent <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCrossAxisExtent), "maxCrossAxisExtent must be greater than 0.");
        }

        if (mainAxisSpacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mainAxisSpacing), "mainAxisSpacing cannot be negative.");
        }

        if (crossAxisSpacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(crossAxisSpacing), "crossAxisSpacing cannot be negative.");
        }

        if (childAspectRatio <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(childAspectRatio), "childAspectRatio must be greater than 0.");
        }

        if (mainAxisExtent.HasValue && mainAxisExtent.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mainAxisExtent), "mainAxisExtent cannot be negative.");
        }

        MaxCrossAxisExtent = maxCrossAxisExtent;
        MainAxisSpacing = mainAxisSpacing;
        CrossAxisSpacing = crossAxisSpacing;
        ChildAspectRatio = childAspectRatio;
        MainAxisExtent = mainAxisExtent;
    }

    public double MaxCrossAxisExtent { get; }

    public double MainAxisSpacing { get; }

    public double CrossAxisSpacing { get; }

    public double ChildAspectRatio { get; }

    public double? MainAxisExtent { get; }

    public override SliverGridLayout GetLayout(SliverConstraints constraints)
    {
        var crossAxisCount = (int)Math.Ceiling(
            constraints.CrossAxisExtent / (MaxCrossAxisExtent + CrossAxisSpacing));
        crossAxisCount = Math.Max(1, crossAxisCount);

        var usableCrossAxisExtent = Math.Max(
            0,
            constraints.CrossAxisExtent - CrossAxisSpacing * (crossAxisCount - 1));
        var childCrossAxisExtent = usableCrossAxisExtent / crossAxisCount;
        var childMainAxisExtent = MainAxisExtent ?? childCrossAxisExtent / ChildAspectRatio;
        return new SliverGridRegularTileLayout(
            crossAxisCount: crossAxisCount,
            mainAxisStride: childMainAxisExtent + MainAxisSpacing,
            crossAxisStride: childCrossAxisExtent + CrossAxisSpacing,
            childMainAxisExtent: childMainAxisExtent,
            childCrossAxisExtent: childCrossAxisExtent,
            reverseCrossAxis: false);
    }

    public override bool ShouldRelayout(SliverGridDelegate oldDelegate)
    {
        if (oldDelegate is not SliverGridDelegateWithMaxCrossAxisExtent old)
        {
            return true;
        }

        return Math.Abs(old.MaxCrossAxisExtent - MaxCrossAxisExtent) > 0.0001
               || Math.Abs(old.MainAxisSpacing - MainAxisSpacing) > 0.0001
               || Math.Abs(old.CrossAxisSpacing - CrossAxisSpacing) > 0.0001
               || Math.Abs(old.ChildAspectRatio - ChildAspectRatio) > 0.0001
               || NullableDoubleChanged(old.MainAxisExtent, MainAxisExtent);
    }

    private static bool NullableDoubleChanged(double? lhs, double? rhs)
    {
        if (!lhs.HasValue && !rhs.HasValue)
        {
            return false;
        }

        if (lhs.HasValue != rhs.HasValue)
        {
            return true;
        }

        return Math.Abs(lhs!.Value - rhs!.Value) > 0.0001;
    }
}

public interface IRenderSliverBoxChildManager
{
    int? ChildCount { get; }
    bool CreateChild(int index, RenderBox? after);
    void RemoveChild(RenderBox child);
    void DidAdoptChild(RenderBox child);
    void SetDidUnderflow(bool value);
}

public sealed class SliverPhysicalParentData : ContainerBoxParentData<RenderSliver>
{
}

public class SliverMultiBoxAdaptorParentData : ContainerBoxParentData<RenderBox>
{
    public int Index { get; set; }
    public double LayoutOffset { get; set; }
    public bool KeepAlive { get; set; }
    public bool KeptAlive { get; set; }
}

public sealed class SliverGridParentData : SliverMultiBoxAdaptorParentData
{
    public double CrossAxisOffset { get; set; }
}

public abstract class RenderSliver : RenderBox
{
    private SliverConstraints? _sliverConstraints;

    public SliverConstraints ConstraintsForSliver =>
        _sliverConstraints ?? throw new InvalidOperationException("RenderSliver is not laid out.");

    public SliverGeometry Geometry { get; protected set; }

    public void LayoutWithSliverConstraints(SliverConstraints constraints)
    {
        _sliverConstraints = constraints;
        var remainingCacheExtent = constraints.RemainingCacheExtent > 0
            ? constraints.RemainingCacheExtent
            : constraints.RemainingPaintExtent;
        var scrollAwareMainAxisExtent = constraints.ViewportMainAxisExtent
                                        + Math.Max(0, constraints.ScrollOffset)
                                        + Math.Max(0, remainingCacheExtent);

        BoxConstraints layoutConstraints;
        if (constraints.Axis == Axis.Vertical)
        {
            layoutConstraints = new BoxConstraints(
                MinWidth: constraints.CrossAxisExtent,
                MaxWidth: constraints.CrossAxisExtent,
                MinHeight: 0,
                MaxHeight: scrollAwareMainAxisExtent);
        }
        else
        {
            layoutConstraints = new BoxConstraints(
                MinWidth: 0,
                MaxWidth: scrollAwareMainAxisExtent,
                MinHeight: constraints.CrossAxisExtent,
                MaxHeight: constraints.CrossAxisExtent);
        }

        Layout(layoutConstraints);
    }

    protected override void PerformLayout()
    {
        var constraints = ConstraintsForSliver;
        PerformSliverLayout(constraints);

        var mainExtent = Math.Max(0, Geometry.PaintExtent);
        Size = constraints.Axis == Axis.Vertical
            ? new Size(constraints.CrossAxisExtent, mainExtent)
            : new Size(mainExtent, constraints.CrossAxisExtent);
    }

    protected abstract void PerformSliverLayout(SliverConstraints constraints);
}

public abstract class RenderSliverSingleBoxAdapter : RenderSliver, IRenderObjectSingleChildContainer
{
    private RenderBox? _child;

    public RenderBox? Child
    {
        get => _child;
        set
        {
            if (ReferenceEquals(_child, value))
            {
                return;
            }

            if (_child != null)
            {
                DropChild(_child);
            }

            _child = value;
            if (_child != null)
            {
                AdoptChild(_child);
            }

            MarkNeedsLayout();
        }
    }

    RenderObject? IRenderObjectSingleChildContainer.Child
    {
        get => Child;
        set => Child = (RenderBox?)value;
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not BoxParentData)
        {
            child.parentData = new BoxParentData();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    protected static double ChildExtentForAxis(Size size, Axis axis)
    {
        return axis == Axis.Vertical ? size.Height : size.Width;
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Child == null || Geometry.PaintExtent <= 0)
        {
            return;
        }

        var childParentData = (BoxParentData)Child.parentData!;
        ctx.PaintChild(Child, offset + childParentData.offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child == null || Geometry.PaintExtent <= 0)
        {
            return false;
        }

        var childParentData = (BoxParentData)Child.parentData!;
        return Child.HitTest(result, position - childParentData.offset);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (Child == null)
        {
            return;
        }

        var childParentData = (BoxParentData)Child.parentData!;
        visitor(Child, childParentData.offset, Matrix.Identity);
    }
}

public class RenderSliverToBoxAdapter : RenderSliverSingleBoxAdapter
{
    public RenderSliverToBoxAdapter(RenderBox? child = null)
    {
        Child = child;
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        if (Child == null)
        {
            Geometry = default;
            return;
        }

        BoxConstraints childConstraints;
        if (constraints.Axis == Axis.Vertical)
        {
            childConstraints = new BoxConstraints(
                MinWidth: constraints.CrossAxisExtent,
                MaxWidth: constraints.CrossAxisExtent,
                MinHeight: 0,
                MaxHeight: double.PositiveInfinity);
        }
        else
        {
            childConstraints = new BoxConstraints(
                MinWidth: 0,
                MaxWidth: double.PositiveInfinity,
                MinHeight: constraints.CrossAxisExtent,
                MaxHeight: constraints.CrossAxisExtent);
        }

        Child.Layout(childConstraints, parentUsesSize: true);

        var childExtent = ChildExtentForAxis(Child.Size, constraints.Axis);
        var effectiveScrollOffset = Math.Clamp(constraints.ScrollOffset, 0, childExtent);
        var remaining = Math.Max(0, childExtent - effectiveScrollOffset);

        var paintedExtent = Math.Min(remaining, constraints.RemainingPaintExtent);
        var layoutExtent = Math.Min(remaining, constraints.ViewportMainAxisExtent);
        var cacheStart = constraints.ScrollOffset + constraints.CacheOrigin;
        var cacheEnd = cacheStart + Math.Max(0, constraints.RemainingCacheExtent);
        var cacheExtent = Math.Max(0, Math.Min(childExtent, cacheEnd) - Math.Max(0, cacheStart));

        var childParentData = (BoxParentData)Child.parentData!;
        childParentData.offset = constraints.Axis == Axis.Vertical
            ? new Point(0, -effectiveScrollOffset)
            : new Point(-effectiveScrollOffset, 0);

        Geometry = new SliverGeometry(
            ScrollExtent: childExtent,
            PaintExtent: paintedExtent,
            LayoutExtent: layoutExtent,
            MaxPaintExtent: childExtent,
            CacheExtent: cacheExtent,
            HasVisualOverflow: remaining > constraints.RemainingPaintExtent);
    }
}

public sealed class RenderSliverPadding : RenderSliver, IRenderObjectSingleChildContainer
{
    private RenderSliver? _child;
    private Thickness _padding;

    public RenderSliverPadding(Thickness padding, RenderSliver? child = null)
    {
        _padding = padding;
        Child = child;
    }

    public Thickness Padding
    {
        get => _padding;
        set
        {
            if (_padding.Equals(value))
            {
                return;
            }

            _padding = value;
            MarkNeedsLayout();
        }
    }

    public RenderSliver? Child
    {
        get => _child;
        set
        {
            if (ReferenceEquals(_child, value))
            {
                return;
            }

            if (_child != null)
            {
                DropChild(_child);
            }

            _child = value;
            if (_child != null)
            {
                AdoptChild(_child);
            }

            MarkNeedsLayout();
        }
    }

    RenderObject? IRenderObjectSingleChildContainer.Child
    {
        get => Child;
        set => Child = (RenderSliver?)value;
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not SliverPhysicalParentData)
        {
            child.parentData = new SliverPhysicalParentData();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_child == null || Geometry.PaintExtent <= 0)
        {
            return;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        ctx.PaintChild(_child, offset + childParentData.offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (_child == null || Geometry.PaintExtent <= 0)
        {
            return false;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        return _child.HitTest(result, position - childParentData.offset);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (_child == null)
        {
            return;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        visitor(_child, childParentData.offset, Matrix.Identity);
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        var (mainStartPadding, mainEndPadding, crossStartPadding, crossEndPadding) = ResolvePadding(_padding, constraints);
        var mainAxisPadding = mainStartPadding + mainEndPadding;
        var crossAxisPadding = crossStartPadding + crossEndPadding;
        var remainingCacheExtent = constraints.RemainingCacheExtent > 0
            ? constraints.RemainingCacheExtent
            : constraints.RemainingPaintExtent;

        if (_child == null)
        {
            var paddedPaintExtent = CalculatePaintExtent(
                from: 0,
                to: mainAxisPadding,
                scrollOffset: constraints.ScrollOffset,
                remainingPaintExtent: constraints.RemainingPaintExtent);
            var paddedLayoutExtent = Math.Min(paddedPaintExtent, constraints.ViewportMainAxisExtent);
            var paddedCacheExtent = CalculatePaintExtent(
                from: 0,
                to: mainAxisPadding,
                scrollOffset: constraints.ScrollOffset + constraints.CacheOrigin,
                remainingPaintExtent: remainingCacheExtent);
            var paddedTargetEndScrollOffsetForPaint = constraints.ScrollOffset + constraints.RemainingPaintExtent;

            Geometry = new SliverGeometry(
                ScrollExtent: mainAxisPadding,
                PaintExtent: paddedPaintExtent,
                LayoutExtent: paddedLayoutExtent,
                MaxPaintExtent: mainAxisPadding,
                CacheExtent: paddedCacheExtent,
                HasVisualOverflow: mainAxisPadding > paddedTargetEndScrollOffsetForPaint || constraints.ScrollOffset > 0);
            return;
        }

        var cacheStart = constraints.ScrollOffset + constraints.CacheOrigin;
        var cacheEnd = cacheStart + Math.Max(0, remainingCacheExtent);
        var childScrollOffset = Math.Max(0, constraints.ScrollOffset - mainStartPadding);
        var childCacheStart = Math.Max(0, cacheStart - mainStartPadding);
        var childCacheEnd = Math.Max(childCacheStart, cacheEnd - mainStartPadding);
        var childRemainingCacheExtent = Math.Max(0, childCacheEnd - childCacheStart);
        var childCacheOrigin = childCacheStart - childScrollOffset;
        var beforePaddingPaintExtent = CalculatePaintExtent(
            from: 0,
            to: mainStartPadding,
            scrollOffset: constraints.ScrollOffset,
            remainingPaintExtent: constraints.RemainingPaintExtent);
        var childRemainingPaintExtent = Math.Max(0, constraints.RemainingPaintExtent - beforePaddingPaintExtent);
        var childCrossAxisExtent = Math.Max(0, constraints.CrossAxisExtent - crossAxisPadding);

        _child.LayoutWithSliverConstraints(new SliverConstraints(
            constraints.Axis,
            childScrollOffset,
            childRemainingPaintExtent,
            childCrossAxisExtent,
            constraints.ViewportMainAxisExtent,
            CacheOrigin: childCacheOrigin,
            RemainingCacheExtent: childRemainingCacheExtent,
            AxisDirection: constraints.AxisDirection,
            GrowthDirection: constraints.GrowthDirection));

        if (Math.Abs(_child.Geometry.ScrollOffsetCorrection) > 0.0001)
        {
            Geometry = new SliverGeometry(ScrollOffsetCorrection: _child.Geometry.ScrollOffsetCorrection);
            return;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        // Child paint origin is the visible portion of leading padding; the child sliver
        // applies its own scroll offset internally and must not be shifted by full scroll offset again.
        var childMainAxisOffset = beforePaddingPaintExtent;
        childParentData.offset = constraints.Axis == Axis.Vertical
            ? new Point(crossStartPadding, childMainAxisOffset)
            : new Point(childMainAxisOffset, crossStartPadding);

        var totalScrollExtent = mainStartPadding + _child.Geometry.ScrollExtent + mainEndPadding;
        var maxPaintExtent = mainStartPadding + _child.Geometry.MaxPaintExtent + mainEndPadding;
        var paintExtent = CalculatePaintExtent(
            from: 0,
            to: totalScrollExtent,
            scrollOffset: constraints.ScrollOffset,
            remainingPaintExtent: constraints.RemainingPaintExtent);
        var layoutExtent = Math.Min(paintExtent, constraints.ViewportMainAxisExtent);
        var cacheExtent = CalculatePaintExtent(
            from: 0,
            to: totalScrollExtent,
            scrollOffset: constraints.ScrollOffset + constraints.CacheOrigin,
            remainingPaintExtent: remainingCacheExtent);
        var targetEndScrollOffsetForPaint = constraints.ScrollOffset + constraints.RemainingPaintExtent;

        Geometry = new SliverGeometry(
            ScrollExtent: totalScrollExtent,
            PaintExtent: paintExtent,
            LayoutExtent: layoutExtent,
            MaxPaintExtent: maxPaintExtent,
            CacheExtent: cacheExtent,
            HasVisualOverflow:
            _child.Geometry.HasVisualOverflow
            || totalScrollExtent > targetEndScrollOffsetForPaint
            || constraints.ScrollOffset > 0);
    }

    private static (double mainStart, double mainEnd, double crossStart, double crossEnd) ResolvePadding(
        Thickness padding,
        SliverConstraints constraints)
    {
        double mainStart;
        double mainEnd;
        double crossStart;
        double crossEnd;

        if (constraints.Axis == Axis.Vertical)
        {
            mainStart = constraints.AxisDirection == AxisDirection.Up ? padding.Bottom : padding.Top;
            mainEnd = constraints.AxisDirection == AxisDirection.Up ? padding.Top : padding.Bottom;
            crossStart = padding.Left;
            crossEnd = padding.Right;
        }
        else
        {
            mainStart = constraints.AxisDirection == AxisDirection.Left ? padding.Right : padding.Left;
            mainEnd = constraints.AxisDirection == AxisDirection.Left ? padding.Left : padding.Right;
            crossStart = padding.Top;
            crossEnd = padding.Bottom;
        }

        if (constraints.GrowthDirection == GrowthDirection.Reverse)
        {
            (mainStart, mainEnd) = (mainEnd, mainStart);
        }

        return (mainStart, mainEnd, crossStart, crossEnd);
    }

    private static double CalculatePaintExtent(
        double from,
        double to,
        double scrollOffset,
        double remainingPaintExtent)
    {
        var visibleStart = Math.Max(from, scrollOffset);
        var visibleEnd = Math.Min(to, scrollOffset + remainingPaintExtent);
        return Math.Max(0, visibleEnd - visibleStart);
    }
}

public abstract class RenderSliverMultiBoxAdaptor : RenderSliver,
    IRenderBoxContainerDefaultsMixin<RenderBox, SliverMultiBoxAdaptorParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, SliverMultiBoxAdaptorParentData> _container;
    private readonly Dictionary<int, RenderBox> _keepAliveBucket = [];
    private IRenderSliverBoxChildManager? _childManager;

    protected RenderSliverMultiBoxAdaptor(IRenderSliverBoxChildManager? childManager = null)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, SliverMultiBoxAdaptorParentData>(this);
        _childManager = childManager;
    }

    public IRenderSliverBoxChildManager? ChildManager
    {
        get => _childManager;
        set
        {
            if (ReferenceEquals(_childManager, value))
            {
                return;
            }

            _childManager = value;
            MarkNeedsLayout();
        }
    }

    public int ChildCount => _container.ChildCount;

    public RenderBox? FirstChild => _container.FirstChild;

    public RenderBox? LastChild => _container.LastChild;

    public void Insert(RenderBox child, RenderBox? after = null)
    {
        SetupParentData(child);
        var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
        childParentData.KeptAlive = false;
        _container.Insert(child, after);
        _childManager?.DidAdoptChild(child);
    }

    public void Move(RenderBox child, RenderBox? after = null)
    {
        var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
        if (!childParentData.KeptAlive)
        {
            _container.Move(child, after);
            _childManager?.DidAdoptChild(child);
            MarkNeedsLayout();
            return;
        }

        if (_keepAliveBucket.TryGetValue(childParentData.Index, out var cachedChild) && ReferenceEquals(cachedChild, child))
        {
            _keepAliveBucket.Remove(childParentData.Index);
        }

        _childManager?.DidAdoptChild(child);
        _keepAliveBucket[childParentData.Index] = child;
        MarkNeedsLayout();
    }

    public void Remove(RenderBox child)
    {
        var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
        if (childParentData.KeptAlive)
        {
            if (_keepAliveBucket.TryGetValue(childParentData.Index, out var cachedChild) && ReferenceEquals(cachedChild, child))
            {
                _keepAliveBucket.Remove(childParentData.Index);
            }

            DropChild(child);
            childParentData.KeptAlive = false;
            return;
        }

        _container.Remove(child);
    }

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after)
    {
        Insert((RenderBox)child, (RenderBox?)after);
    }

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after)
    {
        Move((RenderBox)child, (RenderBox?)after);
    }

    void IRenderObjectContainer.Remove(RenderObject child)
    {
        Remove((RenderBox)child);
    }

    public RenderBox? ChildAfter(RenderBox child)
    {
        return _container.ChildAfter(child);
    }

    public RenderBox? ChildBefore(RenderBox child)
    {
        return _container.ChildBefore(child);
    }

    public void AddAll(List<RenderBox> children)
    {
        _container.AddAll(children);
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not SliverMultiBoxAdaptorParentData)
        {
            child.parentData = new SliverMultiBoxAdaptorParentData();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (var child = FirstChild; child != null; child = ChildAfter(child))
        {
            visitor(child);
        }

        foreach (var child in _keepAliveBucket.Values)
        {
            visitor(child);
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        _container.DefaultPaint(ctx, offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        return _container.DefaultHitTestChildren(result, position);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        for (var child = FirstChild; child != null; child = ChildAfter(child))
        {
            var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
            visitor(child, childParentData.offset, Matrix.Identity);
        }
    }

    protected BoxConstraints ChildConstraintsForSliver(SliverConstraints constraints)
    {
        if (constraints.Axis == Axis.Vertical)
        {
            return new BoxConstraints(
                MinWidth: constraints.CrossAxisExtent,
                MaxWidth: constraints.CrossAxisExtent,
                MinHeight: 0,
                MaxHeight: double.PositiveInfinity);
        }

        return new BoxConstraints(
            MinWidth: 0,
            MaxWidth: double.PositiveInfinity,
            MinHeight: constraints.CrossAxisExtent,
            MaxHeight: constraints.CrossAxisExtent);
    }

    protected static double ChildMainAxisExtent(RenderBox child, Axis axis)
    {
        return axis == Axis.Vertical ? child.Size.Height : child.Size.Width;
    }

    protected int IndexOf(RenderBox child)
    {
        return ((SliverMultiBoxAdaptorParentData)child.parentData!).Index;
    }

    protected double ChildScrollOffset(RenderBox child)
    {
        return ((SliverMultiBoxAdaptorParentData)child.parentData!).LayoutOffset;
    }

    protected bool AddInitialChild(int index = 0, double layoutOffset = 0)
    {
        if (FirstChild != null)
        {
            return true;
        }

        if (!CreateOrObtainChild(index, after: null) || FirstChild == null)
        {
            _childManager?.SetDidUnderflow(true);
            return false;
        }

        var firstChildParentData = (SliverMultiBoxAdaptorParentData)FirstChild.parentData!;
        firstChildParentData.LayoutOffset = layoutOffset;
        return true;
    }

    protected RenderBox? InsertAndLayoutLeadingChild(BoxConstraints childConstraints)
    {
        if (FirstChild == null)
        {
            return null;
        }

        var index = IndexOf(FirstChild) - 1;
        if (index < 0)
        {
            _childManager?.SetDidUnderflow(true);
            return null;
        }

        if (!CreateOrObtainChild(index, after: null) || FirstChild == null || IndexOf(FirstChild) != index)
        {
            _childManager?.SetDidUnderflow(true);
            return null;
        }

        FirstChild.Layout(childConstraints, parentUsesSize: true);
        return FirstChild;
    }

    protected RenderBox? InsertAndLayoutChild(BoxConstraints childConstraints, RenderBox after)
    {
        var index = IndexOf(after) + 1;
        if (!CreateOrObtainChild(index, after))
        {
            _childManager?.SetDidUnderflow(true);
            return null;
        }

        var child = ChildAfter(after);
        if (child == null || IndexOf(child) != index)
        {
            _childManager?.SetDidUnderflow(true);
            return null;
        }

        child.Layout(childConstraints, parentUsesSize: true);
        return child;
    }

    protected void CollectGarbage(int leadingGarbage, int trailingGarbage)
    {
        while (leadingGarbage > 0 && FirstChild != null)
        {
            DestroyOrCacheChild(FirstChild);
            leadingGarbage -= 1;
        }

        while (trailingGarbage > 0 && LastChild != null)
        {
            DestroyOrCacheChild(LastChild);
            trailingGarbage -= 1;
        }

        if (_childManager == null || _keepAliveBucket.Count == 0)
        {
            return;
        }

        foreach (var keepAliveChild in _keepAliveBucket.Values
                     .Where(child => !((SliverMultiBoxAdaptorParentData)child.parentData!).KeepAlive)
                     .ToArray())
        {
            _childManager.RemoveChild(keepAliveChild);
        }
    }

    private bool CreateOrObtainChild(int index, RenderBox? after)
    {
        if (index < 0)
        {
            return false;
        }

        if (_keepAliveBucket.TryGetValue(index, out var keptAliveChild))
        {
            _keepAliveBucket.Remove(index);
            var parentData = (SliverMultiBoxAdaptorParentData)keptAliveChild.parentData!;
            parentData.KeptAlive = false;
            Insert(keptAliveChild, after);
            return true;
        }

        return _childManager?.CreateChild(index, after) ?? false;
    }

    private void DestroyOrCacheChild(RenderBox child)
    {
        var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
        if (childParentData.KeepAlive)
        {
            Remove(child);
            _keepAliveBucket[childParentData.Index] = child;
            AdoptChild(child);
            childParentData.KeptAlive = true;
            return;
        }

        _childManager?.RemoveChild(child);
    }

    public void DefaultPaint(PaintingContext ctx, Point offset)
    {
        _container.DefaultPaint(ctx, offset);
    }

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position)
    {
        return _container.DefaultHitTestChildren(result, position);
    }
}

public sealed class RenderSliverList : RenderSliverMultiBoxAdaptor
{
    public RenderSliverList(IRenderSliverBoxChildManager? childManager = null) : base(childManager)
    {
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        var childManager = ChildManager;
        if (childManager == null)
        {
            Geometry = default;
            return;
        }

        childManager.SetDidUnderflow(false);

        if (FirstChild == null && !AddInitialChild())
        {
            Geometry = default;
            return;
        }

        var firstChild = FirstChild;
        if (firstChild == null)
        {
            Geometry = default;
            childManager.SetDidUnderflow(true);
            return;
        }

        var childConstraints = ChildConstraintsForSliver(constraints);
        var remainingCacheExtent = constraints.RemainingCacheExtent > 0
            ? constraints.RemainingCacheExtent
            : constraints.RemainingPaintExtent;
        var scrollOffset = Math.Max(0, constraints.ScrollOffset + constraints.CacheOrigin);
        var targetEndScrollOffset = scrollOffset + Math.Max(0, remainingCacheExtent);

        var earliestUsefulChild = firstChild;
        while (ChildScrollOffset(earliestUsefulChild) > scrollOffset)
        {
            var oldFirstChild = earliestUsefulChild;
            var oldFirstOffset = ChildScrollOffset(oldFirstChild);

            var newLeadingChild = InsertAndLayoutLeadingChild(childConstraints);
            if (newLeadingChild == null)
            {
                var anchorChild = FirstChild ?? earliestUsefulChild;
                if (IndexOf(anchorChild) == 0)
                {
                    var correction = -ChildScrollOffset(anchorChild);
                    if (Math.Abs(correction) > 0.0001)
                    {
                        Geometry = new SliverGeometry(ScrollOffsetCorrection: correction);
                        return;
                    }
                }

                break;
            }

            var newLeadingParentData = (SliverMultiBoxAdaptorParentData)newLeadingChild.parentData!;
            newLeadingParentData.LayoutOffset = oldFirstOffset - ChildMainAxisExtent(newLeadingChild, constraints.Axis);
            earliestUsefulChild = newLeadingChild;
        }

        earliestUsefulChild = FirstChild ?? earliestUsefulChild;
        earliestUsefulChild.Layout(childConstraints, parentUsesSize: true);
        var earliestUsefulParentData = (SliverMultiBoxAdaptorParentData)earliestUsefulChild.parentData!;
        earliestUsefulParentData.offset = constraints.Axis == Axis.Vertical
            ? new Point(0, earliestUsefulParentData.LayoutOffset - constraints.ScrollOffset)
            : new Point(earliestUsefulParentData.LayoutOffset - constraints.ScrollOffset, 0);

        var leadingGarbage = 0;
        var trailingGarbage = 0;
        var reachedEnd = false;

        RenderBox? child = earliestUsefulChild;
        var index = IndexOf(child);
        var endScrollOffset = ChildScrollOffset(child) + ChildMainAxisExtent(child, constraints.Axis);

        bool Advance()
        {
            if (child == null)
            {
                return false;
            }

            var nextChild = ChildAfter(child);
            var nextIndex = index + 1;
            if (nextChild == null || IndexOf(nextChild) != nextIndex)
            {
                nextChild = InsertAndLayoutChild(childConstraints, child);
                if (nextChild == null)
                {
                    return false;
                }
            }
            else
            {
                nextChild.Layout(childConstraints, parentUsesSize: true);
            }

            var nextChildParentData = (SliverMultiBoxAdaptorParentData)nextChild.parentData!;
            nextChildParentData.Index = nextIndex;
            nextChildParentData.LayoutOffset = endScrollOffset;
            nextChildParentData.offset = constraints.Axis == Axis.Vertical
                ? new Point(0, nextChildParentData.LayoutOffset - constraints.ScrollOffset)
                : new Point(nextChildParentData.LayoutOffset - constraints.ScrollOffset, 0);

            child = nextChild;
            index = nextIndex;
            endScrollOffset = nextChildParentData.LayoutOffset + ChildMainAxisExtent(nextChild, constraints.Axis);
            return true;
        }

        while (endScrollOffset < scrollOffset)
        {
            leadingGarbage += 1;
            if (!Advance())
            {
                reachedEnd = true;
                if (leadingGarbage > 0)
                {
                    leadingGarbage -= 1;
                }

                break;
            }
        }

        if (!reachedEnd)
        {
            while (endScrollOffset < targetEndScrollOffset)
            {
                if (!Advance())
                {
                    reachedEnd = true;
                    break;
                }
            }
        }

        if (child != null)
        {
            for (var trailingChild = ChildAfter(child); trailingChild != null; trailingChild = ChildAfter(trailingChild))
            {
                trailingGarbage += 1;
            }
        }

        CollectGarbage(leadingGarbage, trailingGarbage);

        firstChild = FirstChild;
        if (firstChild == null)
        {
            Geometry = default;
            return;
        }

        var firstIndex = IndexOf(firstChild);
        var leadingScrollOffset = ChildScrollOffset(firstChild);
        var estimatedMaxScrollOffset = reachedEnd
            ? endScrollOffset
            : EstimateMaxScrollOffset(
                firstIndex,
                index,
                leadingScrollOffset,
                endScrollOffset,
                childManager.ChildCount);

        var paintExtent = CalculatePaintExtent(
            from: leadingScrollOffset,
            to: endScrollOffset,
            scrollOffset: constraints.ScrollOffset,
            remainingPaintExtent: constraints.RemainingPaintExtent);
        var layoutExtent = Math.Min(paintExtent, constraints.ViewportMainAxisExtent);
        var cacheExtent = CalculatePaintExtent(
            from: leadingScrollOffset,
            to: endScrollOffset,
            scrollOffset: constraints.ScrollOffset + constraints.CacheOrigin,
            remainingPaintExtent: remainingCacheExtent);
        var targetEndScrollOffsetForPaint = constraints.ScrollOffset + constraints.RemainingPaintExtent;

        Geometry = new SliverGeometry(
            ScrollExtent: estimatedMaxScrollOffset,
            PaintExtent: paintExtent,
            LayoutExtent: layoutExtent,
            MaxPaintExtent: estimatedMaxScrollOffset,
            CacheExtent: cacheExtent,
            HasVisualOverflow: endScrollOffset > targetEndScrollOffsetForPaint || constraints.ScrollOffset > 0);
    }

    private static double EstimateMaxScrollOffset(
        int firstIndex,
        int lastIndex,
        double leadingScrollOffset,
        double trailingScrollOffset,
        int? childCount)
    {
        if (!childCount.HasValue)
        {
            return double.PositiveInfinity;
        }

        if (lastIndex >= childCount.Value - 1)
        {
            return trailingScrollOffset;
        }

        var reifiedCount = Math.Max(1, lastIndex - firstIndex + 1);
        var averageExtent = (trailingScrollOffset - leadingScrollOffset) / reifiedCount;
        var remainingCount = Math.Max(0, childCount.Value - lastIndex - 1);
        return trailingScrollOffset + averageExtent * remainingCount;
    }

    private static double CalculatePaintExtent(
        double from,
        double to,
        double scrollOffset,
        double remainingPaintExtent)
    {
        var visibleStart = Math.Max(from, scrollOffset);
        var visibleEnd = Math.Min(to, scrollOffset + remainingPaintExtent);
        return Math.Max(0, visibleEnd - visibleStart);
    }
}

public sealed class RenderSliverGrid : RenderSliverMultiBoxAdaptor
{
    private SliverGridDelegate _gridDelegate;

    public RenderSliverGrid(SliverGridDelegate gridDelegate, IRenderSliverBoxChildManager? childManager = null) : base(childManager)
    {
        _gridDelegate = gridDelegate ?? throw new ArgumentNullException(nameof(gridDelegate));
    }

    public SliverGridDelegate GridDelegate
    {
        get => _gridDelegate;
        set
        {
            if (ReferenceEquals(_gridDelegate, value))
            {
                return;
            }

            var shouldRelayout = value.GetType() != _gridDelegate.GetType() || value.ShouldRelayout(_gridDelegate);
            _gridDelegate = value;
            if (shouldRelayout)
            {
                MarkNeedsLayout();
            }
        }
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not SliverGridParentData)
        {
            child.parentData = new SliverGridParentData();
        }
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        var childManager = ChildManager;
        if (childManager == null)
        {
            Geometry = default;
            return;
        }

        childManager.SetDidUnderflow(false);
        var childCount = childManager.ChildCount;
        if (childCount == 0)
        {
            var activeChildCount = CountActiveChildren();
            if (activeChildCount > 0)
            {
                CollectGarbage(activeChildCount, 0);
            }

            Geometry = default;
            childManager.SetDidUnderflow(true);
            return;
        }

        var remainingCacheExtent = constraints.RemainingCacheExtent > 0
            ? constraints.RemainingCacheExtent
            : constraints.RemainingPaintExtent;
        var scrollOffset = Math.Max(0, constraints.ScrollOffset + constraints.CacheOrigin);
        var targetEndScrollOffset = scrollOffset + Math.Max(0, remainingCacheExtent);
        var layout = _gridDelegate.GetLayout(constraints);

        var firstIndex = layout.GetMinChildIndexForScrollOffset(scrollOffset);
        var hasFiniteTarget = !double.IsInfinity(targetEndScrollOffset);
        var targetLastIndex = hasFiniteTarget
            ? layout.GetMaxChildIndexForScrollOffset(targetEndScrollOffset)
            : int.MaxValue;

        if (childCount.HasValue)
        {
            if (childCount.Value <= 0)
            {
                Geometry = default;
                childManager.SetDidUnderflow(true);
                return;
            }

            var maxIndex = childCount.Value - 1;
            firstIndex = Math.Clamp(firstIndex, 0, maxIndex);
            if (hasFiniteTarget)
            {
                targetLastIndex = Math.Clamp(targetLastIndex, 0, maxIndex);
                if (targetLastIndex < firstIndex)
                {
                    targetLastIndex = firstIndex;
                }
            }
        }

        var firstChildGeometry = layout.GetGeometryForChildIndex(firstIndex);
        if (FirstChild == null && !AddInitialChild(firstIndex, firstChildGeometry.ScrollOffset))
        {
            var max = childCount.HasValue
                ? layout.ComputeMaxScrollOffset(childCount.Value)
                : 0;
            Geometry = new SliverGeometry(
                ScrollExtent: max,
                MaxPaintExtent: max);
            childManager.SetDidUnderflow(true);
            return;
        }

        var firstChild = FirstChild;
        if (firstChild == null)
        {
            Geometry = default;
            childManager.SetDidUnderflow(true);
            return;
        }

        while (IndexOf(firstChild) > firstIndex)
        {
            var targetIndex = IndexOf(firstChild) - 1;
            var gridGeometry = layout.GetGeometryForChildIndex(targetIndex);
            var newLeadingChild = InsertAndLayoutLeadingChild(gridGeometry.GetBoxConstraints(constraints));
            if (newLeadingChild == null)
            {
                childManager.SetDidUnderflow(true);
                break;
            }

            var newLeadingParentData = (SliverGridParentData)newLeadingChild.parentData!;
            newLeadingParentData.Index = targetIndex;
            ApplyChildGeometry(newLeadingParentData, gridGeometry, constraints);
            firstChild = newLeadingChild;
        }

        var leadingGarbage = 0;
        var trailingGarbage = 0;
        var child = firstChild;
        var index = IndexOf(child);

        while (index < firstIndex)
        {
            leadingGarbage += 1;
            var nextChild = ChildAfter(child);
            if (nextChild == null || IndexOf(nextChild) != index + 1)
            {
                var nextGeometry = layout.GetGeometryForChildIndex(index + 1);
                nextChild = InsertAndLayoutChild(nextGeometry.GetBoxConstraints(constraints), child);
                if (nextChild == null)
                {
                    childManager.SetDidUnderflow(true);
                    break;
                }
            }

            child = nextChild;
            index += 1;
        }

        if (index != firstIndex)
        {
            firstIndex = index;
            if (hasFiniteTarget && targetLastIndex < firstIndex)
            {
                targetLastIndex = firstIndex;
            }
        }

        RenderBox? lastLaidOutChild = null;
        var reachedEnd = false;
        var leadingScrollOffset = layout.GetGeometryForChildIndex(firstIndex).ScrollOffset;
        var trailingScrollOffset = leadingScrollOffset;

        while (child != null && (!hasFiniteTarget || index <= targetLastIndex))
        {
            var gridGeometry = layout.GetGeometryForChildIndex(index);
            child.Layout(gridGeometry.GetBoxConstraints(constraints), parentUsesSize: true);
            var childParentData = (SliverGridParentData)child.parentData!;
            childParentData.Index = index;
            ApplyChildGeometry(childParentData, gridGeometry, constraints);
            lastLaidOutChild = child;
            trailingScrollOffset = Math.Max(trailingScrollOffset, gridGeometry.TrailingScrollOffset);

            if (hasFiniteTarget && index == targetLastIndex)
            {
                child = ChildAfter(child);
                break;
            }

            var nextChild = ChildAfter(child);
            if (nextChild == null || IndexOf(nextChild) != index + 1)
            {
                var nextGeometry = layout.GetGeometryForChildIndex(index + 1);
                nextChild = InsertAndLayoutChild(nextGeometry.GetBoxConstraints(constraints), child);
                if (nextChild == null)
                {
                    reachedEnd = true;
                    childManager.SetDidUnderflow(true);
                    child = null;
                    break;
                }
            }

            child = nextChild;
            index += 1;
        }

        if (lastLaidOutChild == null)
        {
            Geometry = default;
            return;
        }

        for (var trailingChild = child; trailingChild != null; trailingChild = ChildAfter(trailingChild))
        {
            trailingGarbage += 1;
        }

        CollectGarbage(leadingGarbage, trailingGarbage);

        var estimatedMaxScrollOffset = childCount.HasValue
            ? layout.ComputeMaxScrollOffset(childCount.Value)
            : reachedEnd
                ? trailingScrollOffset
                : double.PositiveInfinity;

        var paintExtent = CalculatePaintExtent(
            from: Math.Min(constraints.ScrollOffset, leadingScrollOffset),
            to: trailingScrollOffset,
            scrollOffset: constraints.ScrollOffset,
            remainingPaintExtent: constraints.RemainingPaintExtent);
        var layoutExtent = Math.Min(paintExtent, constraints.ViewportMainAxisExtent);
        var cacheExtent = CalculatePaintExtent(
            from: leadingScrollOffset,
            to: trailingScrollOffset,
            scrollOffset: constraints.ScrollOffset + constraints.CacheOrigin,
            remainingPaintExtent: remainingCacheExtent);
        var targetEndScrollOffsetForPaint = constraints.ScrollOffset + constraints.RemainingPaintExtent;

        Geometry = new SliverGeometry(
            ScrollExtent: estimatedMaxScrollOffset,
            PaintExtent: paintExtent,
            LayoutExtent: layoutExtent,
            MaxPaintExtent: estimatedMaxScrollOffset,
            CacheExtent: cacheExtent,
            HasVisualOverflow: estimatedMaxScrollOffset > targetEndScrollOffsetForPaint || constraints.ScrollOffset > 0);

        if (Math.Abs(estimatedMaxScrollOffset - trailingScrollOffset) < 0.0001)
        {
            childManager.SetDidUnderflow(true);
        }
    }

    private static void ApplyChildGeometry(
        SliverGridParentData parentData,
        SliverGridGeometry geometry,
        SliverConstraints constraints)
    {
        parentData.LayoutOffset = geometry.ScrollOffset;
        parentData.CrossAxisOffset = geometry.CrossAxisOffset;
        parentData.offset = constraints.Axis == Axis.Vertical
            ? new Point(geometry.CrossAxisOffset, geometry.ScrollOffset - constraints.ScrollOffset)
            : new Point(geometry.ScrollOffset - constraints.ScrollOffset, geometry.CrossAxisOffset);
    }

    private int CountActiveChildren()
    {
        var count = 0;
        for (var child = FirstChild; child != null; child = ChildAfter(child))
        {
            count += 1;
        }

        return count;
    }

    private static double CalculatePaintExtent(
        double from,
        double to,
        double scrollOffset,
        double remainingPaintExtent)
    {
        var visibleStart = Math.Max(from, scrollOffset);
        var visibleEnd = Math.Min(to, scrollOffset + remainingPaintExtent);
        return Math.Max(0, visibleEnd - visibleStart);
    }
}

public sealed class RenderSliverFixedExtentList : RenderSliverMultiBoxAdaptor
{
    private double _itemExtent;

    public RenderSliverFixedExtentList(double itemExtent, IRenderSliverBoxChildManager? childManager = null) : base(childManager)
    {
        _itemExtent = Math.Max(0, itemExtent);
    }

    public double ItemExtent
    {
        get => _itemExtent;
        set
        {
            var normalized = Math.Max(0, value);
            if (Math.Abs(_itemExtent - normalized) < 0.0001)
            {
                return;
            }

            _itemExtent = normalized;
            MarkNeedsLayout();
        }
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        var childManager = ChildManager;
        if (childManager == null || _itemExtent <= 0)
        {
            Geometry = default;
            return;
        }

        childManager.SetDidUnderflow(false);

        var childCount = childManager.ChildCount;
        if (childCount == 0)
        {
            var activeChildCount = CountActiveChildren();
            if (activeChildCount > 0)
            {
                CollectGarbage(activeChildCount, 0);
            }

            Geometry = default;
            childManager.SetDidUnderflow(true);
            return;
        }

        var remainingCacheExtent = constraints.RemainingCacheExtent > 0
            ? constraints.RemainingCacheExtent
            : constraints.RemainingPaintExtent;
        var scrollOffset = Math.Max(0, constraints.ScrollOffset + constraints.CacheOrigin);
        var targetEndScrollOffset = scrollOffset + Math.Max(0, remainingCacheExtent);

        var firstIndex = GetMinChildIndexForScrollOffset(scrollOffset, _itemExtent);
        var targetLastIndex = GetMaxChildIndexForScrollOffset(targetEndScrollOffset, _itemExtent);
        if (childCount.HasValue)
        {
            var maxIndex = Math.Max(0, childCount.Value - 1);
            firstIndex = Math.Clamp(firstIndex, 0, maxIndex);
            targetLastIndex = Math.Clamp(targetLastIndex, 0, maxIndex);
            if (targetLastIndex < firstIndex)
            {
                targetLastIndex = firstIndex;
            }
        }

        var childConstraints = FixedExtentChildConstraints(constraints, _itemExtent);
        if (FirstChild == null && !AddInitialChild(firstIndex, firstIndex * _itemExtent))
        {
            Geometry = new SliverGeometry(
                ScrollExtent: childCount.HasValue ? childCount.Value * _itemExtent : 0,
                MaxPaintExtent: childCount.HasValue ? childCount.Value * _itemExtent : 0);
            childManager.SetDidUnderflow(true);
            return;
        }

        var firstChild = FirstChild;
        if (firstChild == null)
        {
            Geometry = default;
            childManager.SetDidUnderflow(true);
            return;
        }

        while (IndexOf(firstChild) > firstIndex)
        {
            var targetIndex = IndexOf(firstChild) - 1;
            var newLeadingChild = InsertAndLayoutLeadingChild(childConstraints);
            if (newLeadingChild == null)
            {
                childManager.SetDidUnderflow(true);
                break;
            }

            var newLeadingParentData = (SliverMultiBoxAdaptorParentData)newLeadingChild.parentData!;
            newLeadingParentData.Index = targetIndex;
            newLeadingParentData.LayoutOffset = targetIndex * _itemExtent;
            firstChild = newLeadingChild;
        }

        var leadingGarbage = 0;
        var trailingGarbage = 0;
        var child = firstChild;
        var index = IndexOf(child);

        while (index < firstIndex)
        {
            leadingGarbage += 1;
            var nextChild = ChildAfter(child);
            if (nextChild == null || IndexOf(nextChild) != index + 1)
            {
                nextChild = InsertAndLayoutChild(childConstraints, child);
                if (nextChild == null)
                {
                    childManager.SetDidUnderflow(true);
                    break;
                }
            }

            child = nextChild;
            index += 1;
        }

        if (index != firstIndex)
        {
            firstIndex = index;
            targetLastIndex = Math.Max(targetLastIndex, firstIndex);
        }

        RenderBox? lastLaidOutChild = null;
        var reachedEnd = false;

        while (child != null && index <= targetLastIndex)
        {
            var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
            childParentData.Index = index;
            childParentData.LayoutOffset = index * _itemExtent;
            child.Layout(childConstraints, parentUsesSize: true);
            childParentData.offset = constraints.Axis == Axis.Vertical
                ? new Point(0, childParentData.LayoutOffset - constraints.ScrollOffset)
                : new Point(childParentData.LayoutOffset - constraints.ScrollOffset, 0);

            lastLaidOutChild = child;

            if (index == targetLastIndex)
            {
                child = ChildAfter(child);
                break;
            }

            var nextChild = ChildAfter(child);
            if (nextChild == null || IndexOf(nextChild) != index + 1)
            {
                nextChild = InsertAndLayoutChild(childConstraints, child);
                if (nextChild == null)
                {
                    reachedEnd = true;
                    childManager.SetDidUnderflow(true);
                    child = null;
                    break;
                }
            }

            child = nextChild;
            index += 1;
        }

        if (lastLaidOutChild == null)
        {
            Geometry = default;
            return;
        }

        for (var trailingChild = child; trailingChild != null; trailingChild = ChildAfter(trailingChild))
        {
            trailingGarbage += 1;
        }

        CollectGarbage(leadingGarbage, trailingGarbage);

        var leadingScrollOffset = firstIndex * _itemExtent;
        var trailingScrollOffset = (index + 1) * _itemExtent;
        if (reachedEnd && childCount.HasValue)
        {
            trailingScrollOffset = Math.Min(trailingScrollOffset, childCount.Value * _itemExtent);
        }

        var estimatedMaxScrollOffset = childCount.HasValue
            ? childCount.Value * _itemExtent
            : reachedEnd
                ? trailingScrollOffset
                : double.PositiveInfinity;
        var paintExtent = CalculatePaintExtent(
            from: leadingScrollOffset,
            to: trailingScrollOffset,
            scrollOffset: constraints.ScrollOffset,
            remainingPaintExtent: constraints.RemainingPaintExtent);
        var layoutExtent = Math.Min(paintExtent, constraints.ViewportMainAxisExtent);
        var cacheExtent = CalculatePaintExtent(
            from: leadingScrollOffset,
            to: trailingScrollOffset,
            scrollOffset: constraints.ScrollOffset + constraints.CacheOrigin,
            remainingPaintExtent: remainingCacheExtent);
        var targetEndScrollOffsetForPaint = constraints.ScrollOffset + constraints.RemainingPaintExtent;

        Geometry = new SliverGeometry(
            ScrollExtent: estimatedMaxScrollOffset,
            PaintExtent: paintExtent,
            LayoutExtent: layoutExtent,
            MaxPaintExtent: estimatedMaxScrollOffset,
            CacheExtent: cacheExtent,
            HasVisualOverflow: trailingScrollOffset > targetEndScrollOffsetForPaint || constraints.ScrollOffset > 0);
    }

    private static int GetMinChildIndexForScrollOffset(double scrollOffset, double itemExtent)
    {
        if (scrollOffset <= 0)
        {
            return 0;
        }

        return Math.Max(0, (int)Math.Floor(scrollOffset / itemExtent));
    }

    private static int GetMaxChildIndexForScrollOffset(double scrollOffset, double itemExtent)
    {
        if (scrollOffset <= 0)
        {
            return 0;
        }

        return Math.Max(0, (int)Math.Ceiling(scrollOffset / itemExtent) - 1);
    }

    private static BoxConstraints FixedExtentChildConstraints(SliverConstraints constraints, double itemExtent)
    {
        if (constraints.Axis == Axis.Vertical)
        {
            return new BoxConstraints(
                MinWidth: constraints.CrossAxisExtent,
                MaxWidth: constraints.CrossAxisExtent,
                MinHeight: itemExtent,
                MaxHeight: itemExtent);
        }

        return new BoxConstraints(
            MinWidth: itemExtent,
            MaxWidth: itemExtent,
            MinHeight: constraints.CrossAxisExtent,
            MaxHeight: constraints.CrossAxisExtent);
    }

    private int CountActiveChildren()
    {
        var count = 0;
        for (var child = FirstChild; child != null; child = ChildAfter(child))
        {
            count += 1;
        }

        return count;
    }

    private static double CalculatePaintExtent(
        double from,
        double to,
        double scrollOffset,
        double remainingPaintExtent)
    {
        var visibleStart = Math.Max(from, scrollOffset);
        var visibleEnd = Math.Min(to, scrollOffset + remainingPaintExtent);
        return Math.Max(0, visibleEnd - visibleStart);
    }
}
