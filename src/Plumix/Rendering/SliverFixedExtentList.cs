using Avalonia;
using Plumix.Foundation;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/sliver_fixed_extent_list.dart

namespace Plumix.Rendering;

/// <summary>
/// A sliver that places multiple box children with the same main-axis extent in a linear array,
/// or with an extent supplied per index by <see cref="ItemExtentBuilder"/>.
/// </summary>
/// <remarks>
/// Flutter's <c>RenderSliverFixedExtentBoxAdaptor</c>. Because the item geometry is derived from
/// overridable methods rather than measured children, subclasses can resize items as a function of
/// the current scroll offset (this is what <c>CarouselView</c> is built on).
/// </remarks>
public abstract class RenderSliverFixedExtentBoxAdaptor : RenderSliverMultiBoxAdaptor
{
    /// <summary>
    /// The sentinel Flutter passes to the deprecated <c>itemExtent</c> parameter of the layout
    /// hooks. No override reads it; the extent is available through <see cref="ItemExtent"/>.
    /// </summary>
    protected const double DeprecatedExtraItemExtent = -1;

    private SliverLayoutDimensions _currentLayoutDimensions;

    protected RenderSliverFixedExtentBoxAdaptor(IRenderSliverBoxChildManager? childManager = null)
        : base(childManager)
    {
    }

    /// <summary>The extent of every child, or null when <see cref="ItemExtentBuilder"/> supplies it.</summary>
    public abstract double? ItemExtent { get; }

    /// <summary>Supplies the main-axis extent of the child at a given index, or null for a uniform extent.</summary>
    public virtual ItemExtentBuilder? ItemExtentBuilder => null;

    /// <summary>The viewport geometry the current layout pass is running against.</summary>
    protected SliverLayoutDimensions LayoutDimensions => _currentLayoutDimensions;

    /// <summary>The scroll offset of the child at <paramref name="index"/>.</summary>
    protected virtual double IndexToLayoutOffset(double itemExtent, int index)
    {
        ItemExtentBuilder? builder = ItemExtentBuilder;
        if (builder is null)
        {
            return itemExtent * index;
        }

        double offset = 0;
        for (int i = 0; i < index; i += 1)
        {
            int? childCount = ChildManager?.ChildCount;
            if (childCount is not null && i > childCount.Value - 1)
            {
                break;
            }

            double? extent = builder(i, _currentLayoutDimensions);
            if (extent is null)
            {
                break;
            }

            offset += extent.Value;
        }

        return offset;
    }

    /// <summary>The index of the first child that paints at or after <paramref name="scrollOffset"/>.</summary>
    protected virtual int GetMinChildIndexForScrollOffset(double scrollOffset, double itemExtent)
    {
        ItemExtentBuilder? builder = ItemExtentBuilder;
        if (builder is not null)
        {
            return GetChildIndexForScrollOffset(scrollOffset, builder);
        }

        if (itemExtent > 0.0)
        {
            double actual = scrollOffset / itemExtent;
            int round = RoundHalfAwayFromZero(actual);
            return Math.Abs((actual * itemExtent) - (round * itemExtent)) < Constants.PrecisionErrorTolerance
                ? round
                : (int)Math.Floor(actual);
        }

        return 0;
    }

    /// <summary>The index of the last child that paints at or before <paramref name="scrollOffset"/>.</summary>
    protected virtual int GetMaxChildIndexForScrollOffset(double scrollOffset, double itemExtent)
    {
        ItemExtentBuilder? builder = ItemExtentBuilder;
        if (builder is not null)
        {
            return GetChildIndexForScrollOffset(scrollOffset, builder);
        }

        if (itemExtent > 0.0)
        {
            double actual = (scrollOffset / itemExtent) - 1;
            int round = RoundHalfAwayFromZero(actual);
            return Math.Abs((actual * itemExtent) - (round * itemExtent)) < Constants.PrecisionErrorTolerance
                ? Math.Max(0, round)
                : Math.Max(0, (int)Math.Ceiling(actual));
        }

        return 0;
    }

    /// <summary>The scroll extent of the whole child list, when the child count is known.</summary>
    protected virtual double ComputeMaxScrollOffset(SliverConstraints constraints, double itemExtent)
    {
        int childCount = ChildManager?.ChildCount ?? 0;
        ItemExtentBuilder? builder = ItemExtentBuilder;
        if (builder is null)
        {
            return childCount * itemExtent;
        }

        double offset = 0;
        for (int i = 0; i < childCount; i += 1)
        {
            double? extent = builder(i, _currentLayoutDimensions);
            if (extent is null)
            {
                break;
            }

            offset += extent.Value;
        }

        return offset;
    }

    /// <summary>
    /// Estimates the scroll extent of the children that have not been laid out, by extrapolating
    /// from the average extent of the ones that have.
    /// </summary>
    protected virtual double EstimateMaxScrollOffset(
        SliverConstraints constraints,
        int firstIndex,
        int lastIndex,
        double leadingScrollOffset,
        double trailingScrollOffset)
    {
        int? childCount = ChildManager?.ChildCount;
        if (childCount is null)
        {
            return double.PositiveInfinity;
        }

        if (lastIndex == childCount.Value - 1)
        {
            return trailingScrollOffset;
        }

        int reifiedCount = lastIndex - firstIndex + 1;
        double averageExtent = reifiedCount <= 0
            ? 0
            : (trailingScrollOffset - leadingScrollOffset) / reifiedCount;
        int remainingCount = childCount.Value - lastIndex - 1;
        return trailingScrollOffset + (averageExtent * remainingCount);
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        IRenderSliverBoxChildManager? childManager = ChildManager;
        if (childManager is null)
        {
            Geometry = default;
            return;
        }

        childManager.SetDidUnderflow(false);
        SetLayoutDimensions(constraints);

        double itemExtent = ItemExtent ?? 0;
        double scrollOffset = Math.Max(0, constraints.ScrollOffset + constraints.CacheOrigin);
        double remainingExtent = Math.Max(0, constraints.RemainingCacheExtent);
        double targetEndScrollOffset = scrollOffset + remainingExtent;

        int firstIndex = GetMinChildIndexForScrollOffset(scrollOffset, itemExtent);
        int? targetLastIndex = double.IsFinite(targetEndScrollOffset)
            ? GetMaxChildIndexForScrollOffset(targetEndScrollOffset, itemExtent)
            : null;

        if (FirstChild is not null)
        {
            int leadingGarbage = CalculateLeadingGarbage(firstIndex);
            int trailingGarbage = targetLastIndex is null ? 0 : CalculateTrailingGarbage(targetLastIndex.Value);
            CollectGarbage(leadingGarbage, trailingGarbage);
        }
        else
        {
            CollectGarbage(0, 0);
        }

        if (FirstChild is null && !AddInitialChild(firstIndex, IndexToLayoutOffset(itemExtent, firstIndex)))
        {
            double max = childManager.ChildCount is null && firstIndex <= 0
                ? 0.0
                : ComputeMaxScrollOffset(constraints, itemExtent);
            Geometry = new SliverGeometry(ScrollExtent: max, MaxPaintExtent: max);
            return;
        }

        RenderBox? trailingChildWithLayout = null;
        for (int index = IndexOf(FirstChild!) - 1; index >= firstIndex; index -= 1)
        {
            RenderBox? leading = InsertAndLayoutLeadingChild(ChildConstraintsForIndex(constraints, index));
            if (leading is null)
            {
                // The children before the current first child are gone; let the viewport re-run
                // layout from the corrected offset instead of guessing their extent.
                Geometry = new SliverGeometry(ScrollOffsetCorrection: IndexToLayoutOffset(itemExtent, index));
                return;
            }

            SetChildGeometry(leading, constraints, IndexToLayoutOffset(itemExtent, index));
            trailingChildWithLayout ??= leading;
        }

        if (trailingChildWithLayout is null)
        {
            RenderBox first = FirstChild!;
            first.Layout(ChildConstraintsForIndex(constraints, IndexOf(first)), parentUsesSize: true);
            SetChildGeometry(first, constraints, IndexToLayoutOffset(itemExtent, firstIndex));
            trailingChildWithLayout = first;
        }

        double estimatedMaxScrollOffset = double.PositiveInfinity;
        for (int index = IndexOf(trailingChildWithLayout) + 1;
             targetLastIndex is null || index <= targetLastIndex.Value;
             index += 1)
        {
            RenderBox? child = ChildAfter(trailingChildWithLayout);
            if (child is null || IndexOf(child) != index)
            {
                child = InsertAndLayoutChild(ChildConstraintsForIndex(constraints, index), trailingChildWithLayout);
                if (child is null)
                {
                    estimatedMaxScrollOffset = IndexToLayoutOffset(itemExtent, index);
                    break;
                }
            }
            else
            {
                child.Layout(ChildConstraintsForIndex(constraints, index), parentUsesSize: true);
            }

            trailingChildWithLayout = child;
            SetChildGeometry(child, constraints, IndexToLayoutOffset(itemExtent, IndexOf(child)));
        }

        int lastIndex = IndexOf(LastChild!);
        double leadingScrollOffset = IndexToLayoutOffset(itemExtent, firstIndex);
        double trailingScrollOffset = IndexToLayoutOffset(itemExtent, lastIndex + 1);
        Geometry = BuildGeometry(
            constraints,
            itemExtent,
            firstIndex,
            lastIndex,
            leadingScrollOffset,
            trailingScrollOffset,
            estimatedMaxScrollOffset);
    }

    /// <summary>
    /// Turns the laid-out child range into the sliver's geometry. Split out of
    /// <see cref="PerformSliverLayout"/> so that subclasses whose leading/trailing offsets differ
    /// (Flutter's <c>_RenderSliverWeightedCarousel</c>) can reuse the tail of the algorithm.
    /// </summary>
    protected SliverGeometry BuildGeometry(
        SliverConstraints constraints,
        double itemExtent,
        int firstIndex,
        int lastIndex,
        double leadingScrollOffset,
        double trailingScrollOffset,
        double estimatedMaxScrollOffset,
        double? paintFrom = null)
    {
        estimatedMaxScrollOffset = Math.Min(
            estimatedMaxScrollOffset,
            EstimateMaxScrollOffset(constraints, firstIndex, lastIndex, leadingScrollOffset, trailingScrollOffset));

        double from = paintFrom ?? leadingScrollOffset;
        double paintExtent = CalculatePaintOffset(constraints, from: from, to: trailingScrollOffset);
        double cacheExtent = CalculateCacheOffset(constraints, from: from, to: trailingScrollOffset);
        double targetEndScrollOffsetForPaint = constraints.ScrollOffset + constraints.RemainingPaintExtent;
        int? targetLastIndexForPaint = double.IsFinite(targetEndScrollOffsetForPaint)
            ? GetMaxChildIndexForScrollOffset(targetEndScrollOffsetForPaint, itemExtent)
            : null;

        if (Math.Abs(estimatedMaxScrollOffset - trailingScrollOffset) < Constants.PrecisionErrorTolerance)
        {
            ChildManager?.SetDidUnderflow(true);
        }

        return new SliverGeometry(
            ScrollExtent: estimatedMaxScrollOffset,
            PaintExtent: paintExtent,
            LayoutExtent: paintExtent,
            MaxPaintExtent: estimatedMaxScrollOffset,
            CacheExtent: cacheExtent,
            HasVisualOverflow: (targetLastIndexForPaint is not null && lastIndex >= targetLastIndexForPaint.Value)
                               || constraints.ScrollOffset > 0.0);
    }

    /// <summary>Records the viewport geometry the current layout pass runs against.</summary>
    protected void SetLayoutDimensions(SliverConstraints constraints)
    {
        _currentLayoutDimensions = new SliverLayoutDimensions(
            ScrollOffset: constraints.ScrollOffset,
            PrecedingScrollExtent: constraints.PrecedingScrollExtent,
            ViewportMainAxisExtent: constraints.ViewportMainAxisExtent,
            CrossAxisExtent: constraints.CrossAxisExtent);
    }

    /// <summary>The tight box constraints the child at <paramref name="index"/> is laid out with.</summary>
    protected BoxConstraints ChildConstraintsForIndex(SliverConstraints constraints, int index)
    {
        double extent = ItemExtentBuilder is { } builder
            ? builder(index, _currentLayoutDimensions) ?? 0
            : ItemExtent ?? 0;
        return constraints.AsBoxConstraints(minExtent: extent, maxExtent: extent);
    }

    /// <summary>Stores the child's scroll offset and derives its paint offset from it.</summary>
    protected void SetChildGeometry(RenderBox child, SliverConstraints constraints, double layoutOffset)
    {
        var data = (SliverMultiBoxAdaptorParentData)child.parentData!;
        data.LayoutOffset = layoutOffset;
        data.offset = constraints.Axis == Axis.Vertical
            ? new Point(0, layoutOffset - constraints.ScrollOffset)
            : new Point(layoutOffset - constraints.ScrollOffset, 0);
    }

    /// <summary>The number of laid-out children before <paramref name="firstIndex"/>.</summary>
    protected int CalculateLeadingGarbage(int firstIndex)
    {
        RenderBox? walker = FirstChild;
        int count = 0;
        while (walker is not null && IndexOf(walker) < firstIndex)
        {
            count += 1;
            walker = ChildAfter(walker);
        }

        return count;
    }

    /// <summary>The number of laid-out children after <paramref name="lastIndex"/>.</summary>
    protected int CalculateTrailingGarbage(int lastIndex)
    {
        RenderBox? walker = LastChild;
        int count = 0;
        while (walker is not null && IndexOf(walker) > lastIndex)
        {
            count += 1;
            walker = ChildBefore(walker);
        }

        return count;
    }

    /// <summary>Dart's <c>double.round()</c>, which rounds halves away from zero.</summary>
    protected static int RoundHalfAwayFromZero(double value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private int GetChildIndexForScrollOffset(double scrollOffset, ItemExtentBuilder callback)
    {
        if (scrollOffset == 0.0)
        {
            return 0;
        }

        double position = 0;
        int index = 0;
        while (position < scrollOffset)
        {
            int? childCount = ChildManager?.ChildCount;
            if (childCount is not null && index > childCount.Value - 1)
            {
                break;
            }

            double? extent = callback(index, _currentLayoutDimensions);
            if (extent is null)
            {
                break;
            }

            position += extent.Value;
            index += 1;
        }

        return index - 1;
    }
}
