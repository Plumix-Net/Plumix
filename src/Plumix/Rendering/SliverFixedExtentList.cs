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

    private SliverLayoutDimensions? _currentLayoutDimensions;

    protected RenderSliverFixedExtentBoxAdaptor(IRenderSliverBoxChildManager? childManager = null)
        : base(childManager)
    {
    }

    /// <summary>The extent of every child, or null when <see cref="ItemExtentBuilder"/> supplies it.</summary>
    public abstract double? ItemExtent { get; }

    /// <summary>Supplies the main-axis extent of the child at a given index, or null for a uniform extent.</summary>
    public virtual ItemExtentBuilder? ItemExtentBuilder => null;

    /// <summary>The viewport geometry the current layout pass is running against.</summary>
    /// <remarks>
    /// Flutter's <c>layoutDimensions</c>: before the first layout pass it is derived from the
    /// current constraints rather than from the pass that has not run yet.
    /// </remarks>
    public SliverLayoutDimensions LayoutDimensions =>
        _currentLayoutDimensions ?? DimensionsFor(ConstraintsForSliver);

    /// <summary>The scroll offset of the child at <paramref name="index"/>.</summary>
    public virtual double IndexToLayoutOffset(double itemExtent, int index)
    {
        ItemExtentBuilder? builder = ItemExtentBuilder;
        if (builder is null)
        {
            return (ItemExtent ?? 0) * index;
        }

        double offset = 0;
        for (int i = 0; i < index; i += 1)
        {
            int? childCount = ChildManager?.EstimatedChildCount;
            if (childCount is not null && i > childCount.Value - 1)
            {
                break;
            }

            double? extent = builder(i, LayoutDimensions);
            if (extent is null)
            {
                break;
            }

            offset += extent.Value;
        }

        return offset;
    }

    /// <summary>The index of the first child that paints at or after <paramref name="scrollOffset"/>.</summary>
    public virtual int GetMinChildIndexForScrollOffset(double scrollOffset, double itemExtent)
    {
        ItemExtentBuilder? builder = ItemExtentBuilder;
        if (builder is not null)
        {
            return GetChildIndexForScrollOffset(scrollOffset, builder);
        }

        itemExtent = ItemExtent ?? 0;
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
    public virtual int GetMaxChildIndexForScrollOffset(double scrollOffset, double itemExtent)
    {
        ItemExtentBuilder? builder = ItemExtentBuilder;
        if (builder is not null)
        {
            return GetChildIndexForScrollOffset(scrollOffset, builder);
        }

        itemExtent = ItemExtent ?? 0;
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
    public virtual double ComputeMaxScrollOffset(SliverConstraints constraints, double itemExtent)
    {
        int childCount = ChildManager?.ChildCount ?? 0;
        ItemExtentBuilder? builder = ItemExtentBuilder;
        if (builder is null)
        {
            return childCount * (ItemExtent ?? 0);
        }

        double offset = 0;
        for (int i = 0; i < childCount; i += 1)
        {
            double? extent = builder(i, LayoutDimensions);
            if (extent is null)
            {
                break;
            }

            offset += extent.Value;
        }

        return offset;
    }

    /// <summary>
    /// Estimates the scroll extent of the whole child list, including the children that have not
    /// been laid out.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderSliverFixedExtentBoxAdaptor.estimateMaxScrollOffset</c>: a thin forward to
    /// <see cref="IRenderSliverBoxChildManager.EstimateMaxScrollOffset"/>, which extrapolates from
    /// the average extent of the reified children unless the delegate knows better.
    /// </remarks>
    protected virtual double EstimateMaxScrollOffset(
        SliverConstraints constraints,
        int firstIndex,
        int lastIndex,
        double leadingScrollOffset,
        double trailingScrollOffset)
    {
        return ChildManager?.EstimateMaxScrollOffset(
            constraints,
            firstIndex: firstIndex,
            lastIndex: lastIndex,
            leadingScrollOffset: leadingScrollOffset,
            trailingScrollOffset: trailingScrollOffset) ?? double.PositiveInfinity;
    }

    /// <inheritdoc />
    public override double PaintExtentOf(RenderBox child)
    {
        ItemExtentBuilder? builder = ItemExtentBuilder;
        return builder is null
            ? ItemExtent ?? 0
            : builder(IndexOf(child), LayoutDimensions) ?? 0;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderSliverFixedExtentBoxAdaptor.debugAssertDoesMeetConstraints</c>: a
    /// <see cref="ComputeMaxScrollOffset"/> override that does not return a multiple of the item
    /// extent leaves a partial item at the end of the list.
    /// </remarks>
    protected override void DebugAssertDoesMeetConstraints()
    {
        base.DebugAssertDoesMeetConstraints();
        if (!Constants.KDebugMode || ItemExtentBuilder is not null || !double.IsFinite(Geometry.ScrollExtent))
        {
            return;
        }

        double itemExtent = ItemExtent ?? 0;
        double scrollExtent = Geometry.ScrollExtent;
        double count = scrollExtent / itemExtent;
        double diff = Math.Abs(Math.Round(count, MidpointRounding.AwayFromZero) - count);
        if (diff * itemExtent <= Constants.PrecisionErrorTolerance || diff <= Constants.PrecisionErrorTolerance)
        {
            return;
        }

        throw new FlutterError(
        [
            new ErrorSummary(
                "RenderSliverFixedExtentBoxAdaptor.computeMaxScrollOffset() returned a value that is "
                + "not an even multiple of its itemExtent."),
            new ErrorDescription(
                $"The itemExtent was {itemExtent}, but the scrollExtent was {scrollExtent}."),
            new ErrorDescription(
                $"The difference was {diff}, which is greater than precisionErrorTolerance "
                + $"({Constants.PrecisionErrorTolerance})."),
            DescribeForError("The render object in question was"),
        ]);
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        IRenderSliverBoxChildManager? childManager = ChildManager;
        if (childManager is null)
        {
            Geometry = default;
            return;
        }

        childManager.DidStartLayout();
        childManager.SetDidUnderflow(false);
        SetLayoutDimensions(constraints);

        double scrollOffset = Math.Max(0, constraints.ScrollOffset + constraints.CacheOrigin);
        double remainingExtent = Math.Max(0, constraints.RemainingCacheExtent);
        double targetEndScrollOffset = scrollOffset + remainingExtent;

        int firstIndex = GetMinChildIndexForScrollOffset(scrollOffset, DeprecatedExtraItemExtent);
        int? targetLastIndex = double.IsFinite(targetEndScrollOffset)
            ? GetMaxChildIndexForScrollOffset(targetEndScrollOffset, DeprecatedExtraItemExtent)
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

        if (FirstChild is null
            && !AddInitialChild(firstIndex, IndexToLayoutOffset(DeprecatedExtraItemExtent, firstIndex)))
        {
            // There are either no children, or we are past the end of all our children.
            double max = firstIndex <= 0
                ? 0.0
                : ComputeMaxScrollOffset(constraints, DeprecatedExtraItemExtent);
            Geometry = new SliverGeometry(ScrollExtent: max, MaxPaintExtent: max);
            childManager.DidFinishLayout();
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
                Geometry = new SliverGeometry(
                    ScrollOffsetCorrection: IndexToLayoutOffset(DeprecatedExtraItemExtent, index));
                // Dart returns without didFinishLayout here: the viewport will re-run this layout.
                return;
            }

            SetChildGeometry(leading, constraints, IndexToLayoutOffset(DeprecatedExtraItemExtent, index));
            trailingChildWithLayout ??= leading;
        }

        if (trailingChildWithLayout is null)
        {
            RenderBox first = FirstChild!;
            first.Layout(ChildConstraintsForIndex(constraints, IndexOf(first)), parentUsesSize: true);
            SetChildGeometry(first, constraints, IndexToLayoutOffset(DeprecatedExtraItemExtent, firstIndex));
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
                    // We have run out of children.
                    estimatedMaxScrollOffset = IndexToLayoutOffset(DeprecatedExtraItemExtent, index);
                    break;
                }
            }
            else
            {
                child.Layout(ChildConstraintsForIndex(constraints, index), parentUsesSize: true);
            }

            trailingChildWithLayout = child;
            SetChildGeometry(
                child,
                constraints,
                IndexToLayoutOffset(DeprecatedExtraItemExtent, IndexOf(child)));
        }

        int lastIndex = IndexOf(LastChild!);
        double leadingScrollOffset = IndexToLayoutOffset(DeprecatedExtraItemExtent, firstIndex);
        double trailingScrollOffset = IndexToLayoutOffset(DeprecatedExtraItemExtent, lastIndex + 1);
        Geometry = BuildGeometry(
            constraints,
            DeprecatedExtraItemExtent,
            firstIndex,
            lastIndex,
            leadingScrollOffset,
            trailingScrollOffset,
            estimatedMaxScrollOffset);
        childManager.DidFinishLayout();
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

        // We may have started the layout while scrolled to the end, which would not expose a new child.
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
            // Conservative to avoid flickering away the clip during scroll.
            HasVisualOverflow: (targetLastIndexForPaint is not null && lastIndex >= targetLastIndexForPaint.Value)
                               || constraints.ScrollOffset > 0.0);
    }

    /// <summary>Records the viewport geometry the current layout pass runs against.</summary>
    protected void SetLayoutDimensions(SliverConstraints constraints)
    {
        _currentLayoutDimensions = DimensionsFor(constraints);
    }

    /// <summary>The tight box constraints the child at <paramref name="index"/> is laid out with.</summary>
    protected BoxConstraints ChildConstraintsForIndex(SliverConstraints constraints, int index)
    {
        double extent = ItemExtentBuilder is { } builder
            ? builder(index, LayoutDimensions) ?? 0
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

    /// <summary>Dart's <c>double.round()</c>, which rounds halves away from zero.</summary>
    protected static int RoundHalfAwayFromZero(double value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private static SliverLayoutDimensions DimensionsFor(SliverConstraints constraints)
    {
        return new SliverLayoutDimensions(
            ScrollOffset: constraints.ScrollOffset,
            PrecedingScrollExtent: constraints.PrecedingScrollExtent,
            ViewportMainAxisExtent: constraints.ViewportMainAxisExtent,
            CrossAxisExtent: constraints.CrossAxisExtent);
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
            int? childCount = ChildManager?.EstimatedChildCount;
            if (childCount is not null && index > childCount.Value - 1)
            {
                break;
            }

            double? extent = callback(index, LayoutDimensions);
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

/// <summary>
/// A sliver that places multiple box children with the same main-axis extent in a linear array.
/// </summary>
/// <remarks>Flutter's <c>RenderSliverFixedExtentList</c>.</remarks>
public class RenderSliverFixedExtentList : RenderSliverFixedExtentBoxAdaptor
{
    private double _itemExtent;

    public RenderSliverFixedExtentList(double itemExtent, IRenderSliverBoxChildManager? childManager = null)
        : base(childManager)
    {
        _itemExtent = itemExtent;
    }

    /// <inheritdoc />
    public override double? ItemExtent => _itemExtent;

    /// <summary>
    /// Sets the main-axis extent every child is forced to.
    /// </summary>
    /// <remarks>
    /// Dart spells this as the setter half of <c>itemExtent</c>. C# cannot add a setter to an
    /// overridden getter-only property, so the mutable half is a method (see
    /// <c>docs/ai/DIVERGENCES.md</c>).
    /// </remarks>
    public void SetItemExtent(double value)
    {
        if (_itemExtent == value)
        {
            return;
        }

        _itemExtent = value;
        MarkNeedsLayout();
    }
}

/// <summary>
/// A sliver that places multiple box children with the corresponding main-axis extent in a linear
/// array.
/// </summary>
/// <remarks>Flutter's <c>RenderSliverVariedExtentList</c>.</remarks>
public class RenderSliverVariedExtentList : RenderSliverFixedExtentBoxAdaptor
{
    private ItemExtentBuilder _itemExtentBuilder;

    public RenderSliverVariedExtentList(
        ItemExtentBuilder itemExtentBuilder,
        IRenderSliverBoxChildManager? childManager = null)
        : base(childManager)
    {
        _itemExtentBuilder = itemExtentBuilder ?? throw new ArgumentNullException(nameof(itemExtentBuilder));
    }

    /// <inheritdoc />
    public override ItemExtentBuilder? ItemExtentBuilder => _itemExtentBuilder;

    /// <inheritdoc />
    public override double? ItemExtent => null;

    /// <summary>Sets the callback that supplies each child's main-axis extent.</summary>
    /// <remarks>
    /// Dart's <c>itemExtentBuilder</c> setter; the method spelling is explained on
    /// <see cref="RenderSliverFixedExtentList.SetItemExtent"/>.
    /// </remarks>
    public void SetItemExtentBuilder(ItemExtentBuilder value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (ReferenceEquals(_itemExtentBuilder, value))
        {
            return;
        }

        _itemExtentBuilder = value;
        MarkNeedsLayout();
    }
}
