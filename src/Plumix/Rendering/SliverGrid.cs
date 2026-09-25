using System.Diagnostics;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/sliver_grid.dart

namespace Plumix.Rendering;

/// <summary>Describes the placement of a child in a <see cref="RenderSliverGrid"/>.</summary>
/// <remarks>Flutter's <c>SliverGridGeometry</c>.</remarks>
public class SliverGridGeometry
{
    /// <summary>Creates an object that describes the placement of a child in a grid.</summary>
    public SliverGridGeometry(
        double scrollOffset,
        double crossAxisOffset,
        double mainAxisExtent,
        double crossAxisExtent)
    {
        ScrollOffset = scrollOffset;
        CrossAxisOffset = crossAxisOffset;
        MainAxisExtent = mainAxisExtent;
        CrossAxisExtent = crossAxisExtent;
    }

    /// <summary>The scroll offset of the leading edge of the child relative to the leading edge of
    /// the parent.</summary>
    public double ScrollOffset { get; }

    /// <summary>The offset of the child in the non-scrolling axis.</summary>
    public double CrossAxisOffset { get; }

    /// <summary>The extent of the child in the scrolling axis.</summary>
    public double MainAxisExtent { get; }

    /// <summary>The extent of the child in the non-scrolling axis.</summary>
    public double CrossAxisExtent { get; }

    /// <summary>The scroll offset of the trailing edge of the child relative to the leading edge of
    /// the parent.</summary>
    public double TrailingScrollOffset => ScrollOffset + MainAxisExtent;

    /// <summary>Returns a tight <see cref="BoxConstraints"/> that forces the child to have the
    /// required size, given a <see cref="SliverConstraints"/>.</summary>
    public BoxConstraints GetBoxConstraints(SliverConstraints constraints)
    {
        return constraints.AsBoxConstraints(
            minExtent: MainAxisExtent,
            maxExtent: MainAxisExtent,
            crossAxisExtent: CrossAxisExtent);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        string[] properties =
        [
            $"scrollOffset: {BindingBase.DartDoubleToString(ScrollOffset)}",
            $"crossAxisOffset: {BindingBase.DartDoubleToString(CrossAxisOffset)}",
            $"mainAxisExtent: {BindingBase.DartDoubleToString(MainAxisExtent)}",
            $"crossAxisExtent: {BindingBase.DartDoubleToString(CrossAxisExtent)}",
        ];
        return $"SliverGridGeometry({string.Join(", ", properties)})";
    }
}

/// <summary>The size and position of all the tiles in a <see cref="RenderSliverGrid"/>.</summary>
/// <remarks>Flutter's <c>SliverGridLayout</c>.</remarks>
public abstract class SliverGridLayout
{
    /// <summary>Abstract const constructor. This constructor enables subclasses to provide const
    /// constructors so that they can be used in const expressions.</summary>
    protected SliverGridLayout()
    {
    }

    /// <summary>The minimum child index that intersects with (or is after) this scroll
    /// offset.</summary>
    public abstract int GetMinChildIndexForScrollOffset(double scrollOffset);

    /// <summary>The maximum child index that intersects with (or is before) this scroll
    /// offset.</summary>
    public abstract int GetMaxChildIndexForScrollOffset(double scrollOffset);

    /// <summary>The size and position of the child with the given index.</summary>
    public abstract SliverGridGeometry GetGeometryForChildIndex(int index);

    /// <summary>The scroll extent needed to fully display all the tiles if there are
    /// <paramref name="childCount"/> children in total.</summary>
    /// <remarks>The child count will never be null.</remarks>
    public abstract double ComputeMaxScrollOffset(int childCount);
}

/// <summary>A <see cref="SliverGridLayout"/> that uses equally sized and spaced tiles.</summary>
/// <remarks>Flutter's <c>SliverGridRegularTileLayout</c>.</remarks>
public class SliverGridRegularTileLayout : SliverGridLayout
{
    /// <summary>Creates a layout that uses equally sized and spaced tiles.</summary>
    public SliverGridRegularTileLayout(
        int crossAxisCount,
        double mainAxisStride,
        double crossAxisStride,
        double childMainAxisExtent,
        double childCrossAxisExtent,
        bool reverseCrossAxis)
    {
        if (Constants.KDebugMode)
        {
            if (!(crossAxisCount > 0))
            {
                throw new AssertionError("crossAxisCount > 0");
            }

            if (!(mainAxisStride >= 0))
            {
                throw new AssertionError("mainAxisStride >= 0");
            }

            if (!(crossAxisStride >= 0))
            {
                throw new AssertionError("crossAxisStride >= 0");
            }

            if (!(childMainAxisExtent >= 0))
            {
                throw new AssertionError("childMainAxisExtent >= 0");
            }

            if (!(childCrossAxisExtent >= 0))
            {
                throw new AssertionError("childCrossAxisExtent >= 0");
            }
        }

        CrossAxisCount = crossAxisCount;
        MainAxisStride = mainAxisStride;
        CrossAxisStride = crossAxisStride;
        ChildMainAxisExtent = childMainAxisExtent;
        ChildCrossAxisExtent = childCrossAxisExtent;
        ReverseCrossAxis = reverseCrossAxis;
    }

    /// <summary>The number of children in the cross axis.</summary>
    public int CrossAxisCount { get; }

    /// <summary>The number of pixels from the leading edge of one tile to the leading edge of the
    /// next tile in the main axis.</summary>
    public double MainAxisStride { get; }

    /// <summary>The number of pixels from the leading edge of one tile to the leading edge of the
    /// next tile in the cross axis.</summary>
    public double CrossAxisStride { get; }

    /// <summary>The number of pixels from the leading edge of one tile to the trailing edge of the
    /// same tile in the main axis.</summary>
    public double ChildMainAxisExtent { get; }

    /// <summary>The number of pixels from the leading edge of one tile to the trailing edge of the
    /// same tile in the cross axis.</summary>
    public double ChildCrossAxisExtent { get; }

    /// <summary>Whether the children should be placed in the opposite order of increasing
    /// coordinates in the cross axis.</summary>
    public bool ReverseCrossAxis { get; }

    /// <inheritdoc />
    public override int GetMinChildIndexForScrollOffset(double scrollOffset)
    {
        return MainAxisStride > Constants.PrecisionErrorTolerance
            ? CrossAxisCount * TruncatingDivide(scrollOffset, MainAxisStride)
            : 0;
    }

    /// <inheritdoc />
    public override int GetMaxChildIndexForScrollOffset(double scrollOffset)
    {
        if (MainAxisStride > 0.0)
        {
            int mainAxisCount = (int)Math.Ceiling(scrollOffset / MainAxisStride);
            return Math.Max(0, CrossAxisCount * mainAxisCount - 1);
        }

        return 0;
    }

    private double GetOffsetFromStartInCrossAxis(double crossAxisStart)
    {
        if (ReverseCrossAxis)
        {
            return CrossAxisCount * CrossAxisStride
                   - crossAxisStart
                   - ChildCrossAxisExtent
                   - (CrossAxisStride - ChildCrossAxisExtent);
        }

        return crossAxisStart;
    }

    /// <inheritdoc />
    public override SliverGridGeometry GetGeometryForChildIndex(int index)
    {
        double crossAxisStart = (index % CrossAxisCount) * CrossAxisStride;
        return new SliverGridGeometry(
            scrollOffset: (index / CrossAxisCount) * MainAxisStride,
            crossAxisOffset: GetOffsetFromStartInCrossAxis(crossAxisStart),
            mainAxisExtent: ChildMainAxisExtent,
            crossAxisExtent: ChildCrossAxisExtent);
    }

    /// <inheritdoc />
    public override double ComputeMaxScrollOffset(int childCount)
    {
        if (childCount == 0)
        {
            // There are no children in the grid. The max scroll offset should be zero.
            return 0.0;
        }

        int mainAxisCount = ((childCount - 1) / CrossAxisCount) + 1;
        double mainAxisSpacing = MainAxisStride - ChildMainAxisExtent;
        return MainAxisStride * mainAxisCount - mainAxisSpacing;
    }

    /// <summary>Dart's <c>double ~/ double</c>: the quotient truncated toward zero.</summary>
    private static int TruncatingDivide(double dividend, double divisor) =>
        (int)Math.Truncate(dividend / divisor);
}

/// <summary>Controls the layout of tiles in a grid.</summary>
/// <remarks>
/// Flutter's <c>SliverGridDelegate</c>. Dart declares <c>shouldRelayout</c>'s parameter
/// <c>covariant</c>; <see cref="RenderSliverGrid.GridDelegate"/> only calls it when both delegates
/// have the same runtime type, so the concrete overrides cast.
/// </remarks>
public abstract class SliverGridDelegate
{
    /// <summary>Abstract const constructor. This constructor enables subclasses to provide const
    /// constructors so that they can be used in const expressions.</summary>
    protected SliverGridDelegate()
    {
    }

    /// <summary>Returns information about the size and position of the tiles in the grid.</summary>
    public abstract SliverGridLayout GetLayout(SliverConstraints constraints);

    /// <summary>Override this method to return true when the children need to be laid out.</summary>
    /// <remarks>This should compare the fields of the current delegate and the given
    /// <paramref name="oldDelegate"/> and return true if the fields are such that the layout would
    /// be different.</remarks>
    public abstract bool ShouldRelayout(SliverGridDelegate oldDelegate);
}

/// <summary>Creates grid layouts with a fixed number of tiles in the cross axis.</summary>
/// <remarks>Flutter's <c>SliverGridDelegateWithFixedCrossAxisCount</c>.</remarks>
public class SliverGridDelegateWithFixedCrossAxisCount : SliverGridDelegate
{
    /// <summary>Creates a delegate that makes grid layouts with a fixed number of tiles in the cross
    /// axis.</summary>
    public SliverGridDelegateWithFixedCrossAxisCount(
        int crossAxisCount,
        double mainAxisSpacing = 0.0,
        double crossAxisSpacing = 0.0,
        double childAspectRatio = 1.0,
        double? mainAxisExtent = null)
    {
        if (Constants.KDebugMode)
        {
            if (!(crossAxisCount > 0))
            {
                throw new AssertionError("crossAxisCount > 0");
            }

            if (!(mainAxisSpacing >= 0))
            {
                throw new AssertionError("mainAxisSpacing >= 0");
            }

            if (!(crossAxisSpacing >= 0))
            {
                throw new AssertionError("crossAxisSpacing >= 0");
            }

            if (!(childAspectRatio > 0))
            {
                throw new AssertionError("childAspectRatio > 0");
            }

            if (!(mainAxisExtent is null || mainAxisExtent >= 0))
            {
                throw new AssertionError("mainAxisExtent == null || mainAxisExtent >= 0");
            }
        }

        CrossAxisCount = crossAxisCount;
        MainAxisSpacing = mainAxisSpacing;
        CrossAxisSpacing = crossAxisSpacing;
        ChildAspectRatio = childAspectRatio;
        MainAxisExtent = mainAxisExtent;
    }

    /// <summary>The number of children in the cross axis.</summary>
    public int CrossAxisCount { get; }

    /// <summary>The number of logical pixels between each child along the main axis.</summary>
    public double MainAxisSpacing { get; }

    /// <summary>The number of logical pixels between each child along the cross axis.</summary>
    public double CrossAxisSpacing { get; }

    /// <summary>The ratio of the cross-axis to the main-axis extent of each child.</summary>
    public double ChildAspectRatio { get; }

    /// <summary>The extent of each tile in the main axis. If provided it would define the logical
    /// pixels taken by each tile in the main axis.</summary>
    /// <remarks>If null, <see cref="ChildAspectRatio"/> is used instead.</remarks>
    public double? MainAxisExtent { get; }

    private bool DebugAssertIsValid()
    {
        Debug.Assert(CrossAxisCount > 0);
        Debug.Assert(MainAxisSpacing >= 0.0);
        Debug.Assert(CrossAxisSpacing >= 0.0);
        Debug.Assert(ChildAspectRatio > 0.0);
        return true;
    }

    /// <inheritdoc />
    public override SliverGridLayout GetLayout(SliverConstraints constraints)
    {
        Debug.Assert(DebugAssertIsValid());
        double usableCrossAxisExtent = Math.Max(
            0.0,
            constraints.CrossAxisExtent - CrossAxisSpacing * (CrossAxisCount - 1));
        double childCrossAxisExtent = usableCrossAxisExtent / CrossAxisCount;
        double childMainAxisExtent = MainAxisExtent ?? childCrossAxisExtent / ChildAspectRatio;
        return new SliverGridRegularTileLayout(
            crossAxisCount: CrossAxisCount,
            mainAxisStride: childMainAxisExtent + MainAxisSpacing,
            crossAxisStride: childCrossAxisExtent + CrossAxisSpacing,
            childMainAxisExtent: childMainAxisExtent,
            childCrossAxisExtent: childCrossAxisExtent,
            reverseCrossAxis: ScrollDirectionUtils.AxisDirectionIsReversed(constraints.CrossAxisDirection));
    }

    /// <inheritdoc />
    public override bool ShouldRelayout(SliverGridDelegate oldDelegate)
    {
        var old = (SliverGridDelegateWithFixedCrossAxisCount)oldDelegate;
        return old.CrossAxisCount != CrossAxisCount
               || old.MainAxisSpacing != MainAxisSpacing
               || old.CrossAxisSpacing != CrossAxisSpacing
               || old.ChildAspectRatio != ChildAspectRatio
               || old.MainAxisExtent != MainAxisExtent;
    }
}

/// <summary>Creates grid layouts with tiles that each have a maximum cross-axis extent.</summary>
/// <remarks>Flutter's <c>SliverGridDelegateWithMaxCrossAxisExtent</c>.</remarks>
public class SliverGridDelegateWithMaxCrossAxisExtent : SliverGridDelegate
{
    /// <summary>Creates a delegate that makes grid layouts with tiles that have a maximum
    /// cross-axis extent.</summary>
    public SliverGridDelegateWithMaxCrossAxisExtent(
        double maxCrossAxisExtent,
        double mainAxisSpacing = 0.0,
        double crossAxisSpacing = 0.0,
        double childAspectRatio = 1.0,
        double? mainAxisExtent = null)
    {
        if (Constants.KDebugMode)
        {
            if (!(maxCrossAxisExtent > 0))
            {
                throw new AssertionError("maxCrossAxisExtent > 0");
            }

            if (!(mainAxisSpacing >= 0))
            {
                throw new AssertionError("mainAxisSpacing >= 0");
            }

            if (!(crossAxisSpacing >= 0))
            {
                throw new AssertionError("crossAxisSpacing >= 0");
            }

            if (!(childAspectRatio > 0))
            {
                throw new AssertionError("childAspectRatio > 0");
            }

            if (!(mainAxisExtent is null || mainAxisExtent >= 0))
            {
                throw new AssertionError("mainAxisExtent == null || mainAxisExtent >= 0");
            }
        }

        MaxCrossAxisExtent = maxCrossAxisExtent;
        MainAxisSpacing = mainAxisSpacing;
        CrossAxisSpacing = crossAxisSpacing;
        ChildAspectRatio = childAspectRatio;
        MainAxisExtent = mainAxisExtent;
    }

    /// <summary>The maximum extent of tiles in the cross axis.</summary>
    public double MaxCrossAxisExtent { get; }

    /// <summary>The number of logical pixels between each child along the main axis.</summary>
    public double MainAxisSpacing { get; }

    /// <summary>The number of logical pixels between each child along the cross axis.</summary>
    public double CrossAxisSpacing { get; }

    /// <summary>The ratio of the cross-axis to the main-axis extent of each child.</summary>
    public double ChildAspectRatio { get; }

    /// <summary>The extent of each tile in the main axis. If provided it would define the logical
    /// pixels taken by each tile in the main axis.</summary>
    /// <remarks>If null, <see cref="ChildAspectRatio"/> is used instead.</remarks>
    public double? MainAxisExtent { get; }

    private bool DebugAssertIsValid(double crossAxisExtent)
    {
        Debug.Assert(crossAxisExtent > 0.0);
        Debug.Assert(MaxCrossAxisExtent > 0.0);
        Debug.Assert(MainAxisSpacing >= 0.0);
        Debug.Assert(CrossAxisSpacing >= 0.0);
        Debug.Assert(ChildAspectRatio > 0.0);
        return true;
    }

    /// <inheritdoc />
    public override SliverGridLayout GetLayout(SliverConstraints constraints)
    {
        Debug.Assert(DebugAssertIsValid(constraints.CrossAxisExtent));
        int crossAxisCount = (int)Math.Ceiling(
            constraints.CrossAxisExtent / (MaxCrossAxisExtent + CrossAxisSpacing));

        // Ensure a minimum count of 1, can be zero and result in an infinite extent below when the
        // window size is 0.
        crossAxisCount = Math.Max(1, crossAxisCount);
        double usableCrossAxisExtent = Math.Max(
            0.0,
            constraints.CrossAxisExtent - CrossAxisSpacing * (crossAxisCount - 1));
        double childCrossAxisExtent = usableCrossAxisExtent / crossAxisCount;
        double childMainAxisExtent = MainAxisExtent ?? childCrossAxisExtent / ChildAspectRatio;
        return new SliverGridRegularTileLayout(
            crossAxisCount: crossAxisCount,
            mainAxisStride: childMainAxisExtent + MainAxisSpacing,
            crossAxisStride: childCrossAxisExtent + CrossAxisSpacing,
            childMainAxisExtent: childMainAxisExtent,
            childCrossAxisExtent: childCrossAxisExtent,
            reverseCrossAxis: ScrollDirectionUtils.AxisDirectionIsReversed(constraints.CrossAxisDirection));
    }

    /// <inheritdoc />
    public override bool ShouldRelayout(SliverGridDelegate oldDelegate)
    {
        var old = (SliverGridDelegateWithMaxCrossAxisExtent)oldDelegate;
        return old.MaxCrossAxisExtent != MaxCrossAxisExtent
               || old.MainAxisSpacing != MainAxisSpacing
               || old.CrossAxisSpacing != CrossAxisSpacing
               || old.ChildAspectRatio != ChildAspectRatio
               || old.MainAxisExtent != MainAxisExtent;
    }
}

/// <summary>Parent data structure used by <see cref="RenderSliverGrid"/>.</summary>
/// <remarks>Flutter's <c>SliverGridParentData</c>.</remarks>
public class SliverGridParentData : SliverMultiBoxAdaptorParentData
{
    /// <summary>The offset of the child in the non-scrolling axis.</summary>
    /// <remarks>If the scroll axis is vertical, this offset is from the left-most edge of the
    /// parent to the left-most edge of the child. If the scroll axis is horizontal, this offset is
    /// from the top-most edge of the parent to the top-most edge of the child.</remarks>
    public double? CrossAxisOffset { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        string crossAxisOffset = CrossAxisOffset is null
            ? "null"
            : BindingBase.DartDoubleToString(CrossAxisOffset.Value);
        return $"crossAxisOffset={crossAxisOffset}; {base.ToString()}";
    }
}

/// <summary>A sliver that places multiple box children in a two dimensional arrangement.</summary>
/// <remarks>
/// Flutter's <c>RenderSliverGrid</c>. The child manager is optional here and assigned by the
/// element after construction (see <see cref="RenderSliverMultiBoxAdaptor.ChildManager"/>).
/// </remarks>
public class RenderSliverGrid : RenderSliverMultiBoxAdaptor
{
    private SliverGridDelegate _gridDelegate;

    /// <summary>Creates a sliver that contains multiple box children whose size and position are
    /// determined by a delegate.</summary>
    public RenderSliverGrid(SliverGridDelegate gridDelegate, IRenderSliverBoxChildManager? childManager = null)
        : base(childManager)
    {
        _gridDelegate = gridDelegate;
    }

    /// <inheritdoc />
    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not SliverGridParentData)
        {
            child.parentData = new SliverGridParentData();
        }
    }

    /// <summary>The delegate that controls the size and position of the children.</summary>
    public SliverGridDelegate GridDelegate
    {
        get => _gridDelegate;
        set
        {
            if (ReferenceEquals(_gridDelegate, value))
            {
                return;
            }

            if (value.GetType() != _gridDelegate.GetType() || value.ShouldRelayout(_gridDelegate))
            {
                MarkNeedsLayout();
            }

            _gridDelegate = value;
        }
    }

    /// <inheritdoc />
    public override double ChildCrossAxisPosition(RenderObject child)
    {
        var childParentData = (SliverGridParentData)child.parentData!;
        return childParentData.CrossAxisOffset!.Value;
    }

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        SliverConstraints constraints = Constraints;
        IRenderSliverBoxChildManager? childManager = ChildManager;
        if (childManager is null)
        {
            Geometry = SliverGeometry.Zero;
            return;
        }

        childManager.DidStartLayout();
        childManager.SetDidUnderflow(false);

        double scrollOffset = constraints.ScrollOffset + constraints.CacheOrigin;
        Debug.Assert(scrollOffset >= 0.0);
        double remainingExtent = constraints.RemainingCacheExtent;
        Debug.Assert(remainingExtent >= 0.0);
        double targetEndScrollOffset = scrollOffset + remainingExtent;

        SliverGridLayout layout = _gridDelegate.GetLayout(constraints);

        int firstIndex = layout.GetMinChildIndexForScrollOffset(scrollOffset);
        int? targetLastIndex = double.IsFinite(targetEndScrollOffset)
            ? layout.GetMaxChildIndexForScrollOffset(targetEndScrollOffset)
            : null;

        if (FirstChild is not null)
        {
            int leadingGarbage = CalculateLeadingGarbage(firstIndex);
            int trailingGarbage = targetLastIndex is not null
                ? CalculateTrailingGarbage(targetLastIndex.Value)
                : 0;
            CollectGarbage(leadingGarbage, trailingGarbage);
        }
        else
        {
            CollectGarbage(0, 0);
        }

        SliverGridGeometry firstChildGridGeometry = layout.GetGeometryForChildIndex(firstIndex);

        if (FirstChild is null)
        {
            if (!AddInitialChild(firstIndex, firstChildGridGeometry.ScrollOffset))
            {
                // There are either no children, or we are past the end of all our children.
                double max = layout.ComputeMaxScrollOffset(childManager.ChildCount);
                Geometry = new SliverGeometry(ScrollExtent: max, MaxPaintExtent: max);
                childManager.DidFinishLayout();
                return;
            }
        }

        double leadingScrollOffset = firstChildGridGeometry.ScrollOffset;
        double trailingScrollOffset = firstChildGridGeometry.TrailingScrollOffset;
        RenderBox? trailingChildWithLayout = null;
        bool reachedEnd = false;

        for (int index = IndexOf(FirstChild!) - 1; index >= firstIndex; --index)
        {
            SliverGridGeometry gridGeometry = layout.GetGeometryForChildIndex(index);
            RenderBox child = InsertAndLayoutLeadingChild(gridGeometry.GetBoxConstraints(constraints))!;
            var childParentData = (SliverGridParentData)child.parentData!;
            childParentData.LayoutOffset = gridGeometry.ScrollOffset;
            childParentData.CrossAxisOffset = gridGeometry.CrossAxisOffset;
            Debug.Assert(childParentData.Index == index);
            trailingChildWithLayout ??= child;
            trailingScrollOffset = Math.Max(trailingScrollOffset, gridGeometry.TrailingScrollOffset);
        }

        if (trailingChildWithLayout is null)
        {
            FirstChild!.Layout(firstChildGridGeometry.GetBoxConstraints(constraints));
            var childParentData = (SliverGridParentData)FirstChild.parentData!;
            childParentData.LayoutOffset = firstChildGridGeometry.ScrollOffset;
            childParentData.CrossAxisOffset = firstChildGridGeometry.CrossAxisOffset;
            trailingChildWithLayout = FirstChild;
        }

        for (int index = IndexOf(trailingChildWithLayout) + 1;
             targetLastIndex is null || index <= targetLastIndex;
             ++index)
        {
            SliverGridGeometry gridGeometry = layout.GetGeometryForChildIndex(index);
            BoxConstraints childConstraints = gridGeometry.GetBoxConstraints(constraints);
            RenderBox? child = ChildAfter(trailingChildWithLayout!);
            if (child is null || IndexOf(child) != index)
            {
                child = InsertAndLayoutChild(childConstraints, after: trailingChildWithLayout);
                if (child is null)
                {
                    reachedEnd = true;

                    // We have run out of children.
                    break;
                }
            }
            else
            {
                child.Layout(childConstraints);
            }

            trailingChildWithLayout = child;
            var childParentData = (SliverGridParentData)child.parentData!;
            childParentData.LayoutOffset = gridGeometry.ScrollOffset;
            childParentData.CrossAxisOffset = gridGeometry.CrossAxisOffset;
            Debug.Assert(childParentData.Index == index);
            trailingScrollOffset = Math.Max(trailingScrollOffset, gridGeometry.TrailingScrollOffset);
        }

        int lastIndex = IndexOf(LastChild!);

        Debug.Assert(DebugAssertChildListIsNonEmptyAndContiguous());
        Debug.Assert(IndexOf(FirstChild!) == firstIndex);
        Debug.Assert(targetLastIndex is null || lastIndex <= targetLastIndex);

        double estimatedTotalExtent = reachedEnd
            ? trailingScrollOffset
            : childManager.EstimateMaxScrollOffset(
                constraints,
                firstIndex: firstIndex,
                lastIndex: lastIndex,
                leadingScrollOffset: leadingScrollOffset,
                trailingScrollOffset: trailingScrollOffset);
        double paintExtent = CalculatePaintOffset(
            constraints,
            from: Math.Min(constraints.ScrollOffset, leadingScrollOffset),
            to: trailingScrollOffset);
        double cacheExtent = CalculateCacheOffset(
            constraints,
            from: leadingScrollOffset,
            to: trailingScrollOffset);

        Geometry = new SliverGeometry(
            ScrollExtent: estimatedTotalExtent,
            PaintExtent: paintExtent,
            MaxPaintExtent: estimatedTotalExtent,
            CacheExtent: cacheExtent,
            HasVisualOverflow: estimatedTotalExtent > paintExtent
                               || constraints.ScrollOffset > 0.0
                               || constraints.Overlap != 0.0);

        // We may have started the layout while scrolled to the end, which would not expose a new
        // child.
        if (estimatedTotalExtent == trailingScrollOffset)
        {
            childManager.SetDidUnderflow(true);
        }

        childManager.DidFinishLayout();
    }
}
