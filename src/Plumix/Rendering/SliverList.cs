using System.Diagnostics;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/sliver_list.dart

namespace Plumix.Rendering;

/// <summary>A sliver that places multiple box children in a linear array along the main
/// axis.</summary>
/// <remarks>
/// Flutter's <c>RenderSliverList</c>. Each child is forced to have the
/// <see cref="SliverConstraints.CrossAxisExtent"/> in the cross axis but determines its own main axis
/// extent. The child manager is optional here and assigned by the element after construction (see
/// <see cref="RenderSliverMultiBoxAdaptor.ChildManager"/>).
/// </remarks>
public class RenderSliverList : RenderSliverMultiBoxAdaptor
{
    /// <summary>Creates a sliver that places multiple box children in a linear array along the
    /// main axis.</summary>
    public RenderSliverList(IRenderSliverBoxChildManager? childManager = null) : base(childManager)
    {
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
        BoxConstraints childConstraints = constraints.AsBoxConstraints();
        int leadingGarbage = 0;
        int trailingGarbage = 0;
        bool reachedEnd = false;

        // This algorithm in principle is straight-forward: find the first child that overlaps the
        // given scrollOffset, creating more children at the top of the list if necessary, then walk
        // down the list updating and laying out each child and adding more at the end if necessary
        // until we have enough children to cover the entire viewport.
        //
        // It is complicated by one minor issue, which is that any time you update or create a child,
        // it's possible that some of the children that haven't yet been laid out will be removed,
        // leaving the list in an inconsistent state, and requiring that missing nodes be recreated.
        //
        // To keep this mess tractable, this algorithm starts from what is currently the first child,
        // if any, and then walks up and/or down from there, so that the nodes that might get removed
        // are always at the edges of what has already been laid out.

        // Make sure we have at least one child to start from.
        if (FirstChild is null && !AddInitialChild())
        {
            // There are no children.
            Geometry = SliverGeometry.Zero;
            childManager.DidFinishLayout();
            return;
        }

        // We have at least one child.

        // These variables track the range of children that we have laid out. Within this range, the
        // children have consecutive indices. Outside this range, it's possible for a child to get
        // removed without notice.
        RenderBox? leadingChildWithLayout = null;
        RenderBox? trailingChildWithLayout = null;

        RenderBox? earliestUsefulChild = FirstChild;

        // A firstChild with null layout offset is likely a result of children reordering.
        //
        // We rely on firstChild to have an accurate layout offset. In the case of a null layout
        // offset, we have to find the first child that has a valid one.
        if (ChildScrollOffset(FirstChild!) is null)
        {
            int leadingChildrenWithoutLayoutOffset = 0;
            while (earliestUsefulChild is not null && ChildScrollOffset(earliestUsefulChild) is null)
            {
                earliestUsefulChild = ChildAfter(earliestUsefulChild);
                leadingChildrenWithoutLayoutOffset += 1;
            }

            // We should be able to destroy children with a null layout offset safely, because they
            // are likely outside of the viewport.
            CollectGarbage(leadingChildrenWithoutLayoutOffset, 0);

            // If we cannot find a valid layout offset, start from the initial child.
            if (FirstChild is null && !AddInitialChild())
            {
                // There are no children.
                Geometry = SliverGeometry.Zero;
                childManager.DidFinishLayout();
                return;
            }
        }

        // Find the last child that is at or before the scrollOffset.
        earliestUsefulChild = FirstChild;
        for (double earliestScrollOffset = ChildScrollOffset(earliestUsefulChild!)!.Value;
             earliestScrollOffset > scrollOffset;
             earliestScrollOffset = ChildScrollOffset(earliestUsefulChild!)!.Value)
        {
            // We have to add children before the earliestUsefulChild.
            earliestUsefulChild = InsertAndLayoutLeadingChild(childConstraints, parentUsesSize: true);
            if (earliestUsefulChild is null)
            {
                var firstChildParentData = (SliverMultiBoxAdaptorParentData)FirstChild!.parentData!;
                firstChildParentData.LayoutOffset = 0.0;

                if (scrollOffset == 0.0)
                {
                    // InsertAndLayoutLeadingChild only lays out the children before firstChild. In
                    // this case, nothing has been laid out, so firstChild is laid out by hand.
                    FirstChild.Layout(childConstraints, parentUsesSize: true);
                    earliestUsefulChild = FirstChild;
                    leadingChildWithLayout = earliestUsefulChild;
                    trailingChildWithLayout ??= earliestUsefulChild;
                    break;
                }

                // We ran out of children before reaching the scroll offset. We must inform our
                // parent that this sliver cannot fulfill its contract and that we need a scroll
                // offset correction.
                Geometry = new SliverGeometry(ScrollOffsetCorrection: -scrollOffset);
                return;
            }

            double firstChildScrollOffset = earliestScrollOffset - PaintExtentOf(FirstChild!);

            // firstChildScrollOffset may contain a double precision error.
            if (firstChildScrollOffset < -Constants.PrecisionErrorTolerance)
            {
                // Let's assume there is no child before the first child. We will correct it on the
                // next layout if it is not.
                Geometry = new SliverGeometry(ScrollOffsetCorrection: -firstChildScrollOffset);
                var firstChildParentData = (SliverMultiBoxAdaptorParentData)FirstChild!.parentData!;
                firstChildParentData.LayoutOffset = 0.0;
                return;
            }

            var childParentData = (SliverMultiBoxAdaptorParentData)earliestUsefulChild.parentData!;
            childParentData.LayoutOffset = firstChildScrollOffset;
            Debug.Assert(ReferenceEquals(earliestUsefulChild, FirstChild));
            leadingChildWithLayout = earliestUsefulChild;
            trailingChildWithLayout ??= earliestUsefulChild;
        }

        Debug.Assert(ChildScrollOffset(FirstChild!)!.Value > -Constants.PrecisionErrorTolerance);

        // If the scroll offset is at zero, we should make sure we are actually at the beginning of
        // the list.
        if (scrollOffset < Constants.PrecisionErrorTolerance)
        {
            // We iterate from the firstChild in case the leading child has a 0 paint extent.
            while (IndexOf(FirstChild!) > 0)
            {
                double earliestScrollOffset = ChildScrollOffset(FirstChild!)!.Value;

                // We correct one child at a time. If there are more children before the
                // earliestUsefulChild, we will correct it once the scroll offset reaches zero again.
                earliestUsefulChild = InsertAndLayoutLeadingChild(childConstraints, parentUsesSize: true);
                Debug.Assert(earliestUsefulChild is not null);
                double firstChildScrollOffset = earliestScrollOffset - PaintExtentOf(FirstChild!);
                var childParentData = (SliverMultiBoxAdaptorParentData)FirstChild!.parentData!;
                childParentData.LayoutOffset = 0.0;

                // We only need to correct if the leading child actually has a paint extent.
                if (firstChildScrollOffset < -Constants.PrecisionErrorTolerance)
                {
                    Geometry = new SliverGeometry(ScrollOffsetCorrection: -firstChildScrollOffset);
                    return;
                }
            }
        }

        // At this point, earliestUsefulChild is the first child, and is a child whose scrollOffset is
        // at or before the scrollOffset, and leadingChildWithLayout and trailingChildWithLayout are
        // either null or cover a range of render boxes that we have laid out with the first being the
        // same as earliestUsefulChild and the last being either at or after the scroll offset.
        Debug.Assert(ReferenceEquals(earliestUsefulChild, FirstChild));
        Debug.Assert(ChildScrollOffset(earliestUsefulChild!)!.Value <= scrollOffset);

        // Make sure we've laid out at least one child.
        if (leadingChildWithLayout is null)
        {
            earliestUsefulChild!.Layout(childConstraints, parentUsesSize: true);
            leadingChildWithLayout = earliestUsefulChild;
            trailingChildWithLayout = earliestUsefulChild;
        }

        // Here, earliestUsefulChild is still the first child, it's got a scrollOffset that is at or
        // before our actual scrollOffset, and it has been laid out, and is in fact our
        // leadingChildWithLayout. It's possible that some children beyond that one have also been
        // laid out.
        bool inLayoutRange = true;
        RenderBox? child = earliestUsefulChild;
        int index = IndexOf(child!);
        double endScrollOffset = ChildScrollOffset(child!)!.Value + PaintExtentOf(child!);

        // Returns true if we advanced, false if we have no more children. Used in two different
        // places below, to avoid code duplication.
        bool Advance()
        {
            Debug.Assert(child is not null);
            if (ReferenceEquals(child, trailingChildWithLayout))
            {
                inLayoutRange = false;
            }

            child = ChildAfter(child!);
            if (child is null)
            {
                inLayoutRange = false;
            }

            index += 1;
            if (!inLayoutRange)
            {
                if (child is null || IndexOf(child) != index)
                {
                    // We are missing a child. Insert it (and lay it out) if possible.
                    child = InsertAndLayoutChild(
                        childConstraints,
                        after: trailingChildWithLayout,
                        parentUsesSize: true);
                    if (child is null)
                    {
                        // We have run out of children.
                        return false;
                    }
                }
                else
                {
                    // Lay out the child.
                    child.Layout(childConstraints, parentUsesSize: true);
                }

                trailingChildWithLayout = child;
            }

            Debug.Assert(child is not null);
            var childParentData = (SliverMultiBoxAdaptorParentData)child!.parentData!;
            childParentData.LayoutOffset = endScrollOffset;
            Debug.Assert(childParentData.Index == index);
            endScrollOffset = ChildScrollOffset(child)!.Value + PaintExtentOf(child);
            return true;
        }

        // Find the first child that ends after the scroll offset.
        while (endScrollOffset < scrollOffset)
        {
            leadingGarbage += 1;
            if (!Advance())
            {
                Debug.Assert(leadingGarbage == ChildCount);
                Debug.Assert(child is null);

                // We want to make sure we keep the last child around so we know the end scroll offset.
                CollectGarbage(leadingGarbage - 1, 0);
                Debug.Assert(ReferenceEquals(FirstChild, LastChild));
                double lastExtent = ChildScrollOffset(LastChild!)!.Value + PaintExtentOf(LastChild!);
                Geometry = new SliverGeometry(ScrollExtent: lastExtent, MaxPaintExtent: lastExtent);
                return;
            }
        }

        // Now find the first child that ends after our end.
        while (endScrollOffset < targetEndScrollOffset)
        {
            if (!Advance())
            {
                reachedEnd = true;
                break;
            }
        }

        // Finally count up all the remaining children and label them as garbage.
        if (child is not null)
        {
            child = ChildAfter(child);
            while (child is not null)
            {
                trailingGarbage += 1;
                child = ChildAfter(child);
            }
        }

        // At this point everything should be good to go, we just have to clean up the garbage and
        // report the geometry.
        CollectGarbage(leadingGarbage, trailingGarbage);

        Debug.Assert(DebugAssertChildListIsNonEmptyAndContiguous());
        double estimatedMaxScrollOffset;
        if (reachedEnd)
        {
            estimatedMaxScrollOffset = endScrollOffset;
        }
        else
        {
            estimatedMaxScrollOffset = childManager.EstimateMaxScrollOffset(
                constraints,
                firstIndex: IndexOf(FirstChild!),
                lastIndex: IndexOf(LastChild!),
                leadingScrollOffset: ChildScrollOffset(FirstChild!),
                trailingScrollOffset: endScrollOffset);
            Debug.Assert(estimatedMaxScrollOffset
                >= endScrollOffset - ChildScrollOffset(FirstChild!)!.Value);
        }

        double paintExtent = CalculatePaintOffset(
            constraints,
            from: ChildScrollOffset(FirstChild!)!.Value,
            to: endScrollOffset);
        double cacheExtent = CalculateCacheOffset(
            constraints,
            from: ChildScrollOffset(FirstChild!)!.Value,
            to: endScrollOffset);
        double targetEndScrollOffsetForPaint = constraints.ScrollOffset + constraints.RemainingPaintExtent;

        Geometry = new SliverGeometry(
            ScrollExtent: estimatedMaxScrollOffset,
            PaintExtent: paintExtent,
            MaxPaintExtent: estimatedMaxScrollOffset,
            CacheExtent: cacheExtent,

            // Conservative to avoid flickering away the clip during scroll.
            HasVisualOverflow: endScrollOffset > targetEndScrollOffsetForPaint || constraints.ScrollOffset > 0.0);

        // We may have started the layout while scrolled to the end, which would not expose a new
        // child.
        if (estimatedMaxScrollOffset == endScrollOffset)
        {
            childManager.SetDidUnderflow(true);
        }

        childManager.DidFinishLayout();
    }
}
