using Avalonia;
using Plumix.Foundation;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/sliver_tree.dart

namespace Plumix.Rendering;

/// <summary>
/// Represents the animation of the children of a parent <see cref="TreeSliverNode"/> that are
/// animating into or out of view.
/// </summary>
/// <remarks>
/// Dart's <c>TreeSliverNodesAnimation</c> record. <see cref="ToIndex"/> is inclusive; <see cref="Value"/>
/// is the (curved) value of the animation.
/// </remarks>
public readonly record struct TreeSliverNodesAnimation(int FromIndex, int ToIndex, double Value);

/// <summary>
/// Used to pass information down to <see cref="RenderTreeSliver"/>.
/// </summary>
/// <remarks>Dart's <c>TreeSliverNodeParentData</c>.</remarks>
public class TreeSliverNodeParentData : SliverMultiBoxAdaptorParentData
{
    /// <summary>The depth of the node, used by <see cref="RenderTreeSliver"/> to offset children by
    /// the <see cref="TreeSliverIndentationType"/>.</summary>
    public int Depth { get; set; }
}

/// <summary>
/// The style of indentation for <see cref="TreeSliverNode"/>s in a <see cref="TreeSliver{T}"/>, as
/// handled by <see cref="RenderTreeSliver"/>.
/// </summary>
/// <remarks>Dart's <c>TreeSliverIndentationType</c>.</remarks>
public sealed class TreeSliverIndentationType
{
    private TreeSliverIndentationType(double value)
    {
        Value = value;
    }

    /// <summary>The number of pixels by which a <see cref="TreeSliverNode"/> is offset per depth.</summary>
    public double Value { get; }

    /// <summary>The default indentation of child <see cref="TreeSliverNode"/>s: 10 pixels per depth.</summary>
    public static TreeSliverIndentationType Standard { get; } = new(10.0);

    /// <summary>Configures no offsetting of child nodes.</summary>
    public static TreeSliverIndentationType None { get; } = new(0.0);

    /// <summary>Configures a custom offset for indenting child nodes.</summary>
    public static TreeSliverIndentationType Custom(double value)
    {
        DebugAssertions.Assert(value >= 0.0);
        return new TreeSliverIndentationType(value);
    }
}

/// <summary>
/// A sliver that places multiple <see cref="TreeSliverNode"/>s in a linear array along the main axis,
/// while staggering nodes that are animating into and out of view.
/// </summary>
/// <remarks>
/// Dart's <c>RenderTreeSliver</c>. The layout offsets of the rows after an animating parent are
/// pulled back by the animation, and the rows that follow it paint inside a clip, so children appear
/// to slide out from under their parent.
/// </remarks>
public class RenderTreeSliver : RenderSliverVariedExtentList
{
    // TreeSliverNodesAnimation.fromIndex - 1 (the parent's index) -> animation key.
    private readonly Dictionary<int, UniqueKey> _animationLeadingIndices = [];

    // Animation key -> the fixed distance the animating rows travel.
    private readonly Dictionary<UniqueKey, double> _animationOffsets = [];

    private readonly Dictionary<UniqueKey, LayerHandle<ClipRectLayer>> _clipHandles = [];

    private Dictionary<UniqueKey, TreeSliverNodesAnimation> _activeAnimations;
    private double _indentation;

    /// <summary>Creates the sliver that lays out the rows of a <see cref="TreeSliver{T}"/>.</summary>
    public RenderTreeSliver(
        IRenderSliverBoxChildManager? childManager,
        ItemExtentBuilder itemExtentBuilder,
        Dictionary<UniqueKey, TreeSliverNodesAnimation> activeAnimations,
        double indentation)
        : base(itemExtentBuilder, childManager)
    {
        _activeAnimations = activeAnimations;
        _indentation = indentation;
    }

    /// <summary>The currently active <see cref="TreeSliverNode"/> animations.</summary>
    /// <remarks>
    /// Since the index of animating nodes can change at any time, the unique key is used to track an
    /// animation of nodes across frames.
    /// </remarks>
    public Dictionary<UniqueKey, TreeSliverNodesAnimation> ActiveAnimations
    {
        get => _activeAnimations;
        set
        {
            if (ReferenceEquals(_activeAnimations, value))
            {
                return;
            }

            _activeAnimations = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>The number of pixels by which child nodes are offset in the cross axis per depth.</summary>
    public double Indentation
    {
        get => _indentation;
        set
        {
            if (_indentation == value)
            {
                return;
            }

            // Dart asserts the getter here, which still reads the old value.
            DebugAssertions.Assert(Indentation >= 0.0);
            _indentation = value;
            MarkNeedsLayout();
        }
    }

    /// <inheritdoc />
    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not TreeSliverNodeParentData)
        {
            child.parentData = new TreeSliverNodeParentData();
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        foreach (LayerHandle<ClipRectLayer> handle in _clipHandles.Values)
        {
            handle.Layer = null;
        }

        _clipHandles.Clear();
        base.Dispose();
    }

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        DebugAssertions.Assert(
            Constraints.AxisDirection == AxisDirection.Down,
            "TreeSliver is only supported in Viewports with an AxisDirection.down. "
            + $"The current axis direction is: AxisDirection.{Diagnostics.EnumName(Constraints.AxisDirection)}.");
        UpdateAnimationCache();
        base.PerformLayout();
    }

    // Maps the index of parents to the animation key of their children.
    private void UpdateAnimationCache()
    {
        _animationLeadingIndices.Clear();
        foreach (KeyValuePair<UniqueKey, TreeSliverNodesAnimation> entry in _activeAnimations)
        {
            _animationLeadingIndices[entry.Value.FromIndex - 1] = entry.Key;
        }

        // Remove any stored offsets or clip layers that are no longer actively animating.
        foreach (UniqueKey key in _animationOffsets.Keys.ToList())
        {
            if (!_activeAnimations.ContainsKey(key))
            {
                _animationOffsets.Remove(key);
            }
        }

        foreach (KeyValuePair<UniqueKey, LayerHandle<ClipRectLayer>> entry in _clipHandles.ToList())
        {
            if (!_activeAnimations.ContainsKey(entry.Key))
            {
                entry.Value.Layer = null;
                _clipHandles.Remove(entry.Key);
            }
        }
    }

    // Computes how far the animating rows of `key` travel, starting at `position`.
    private void ComputeAnimationOffsetFor(UniqueKey key, double position)
    {
        DebugAssertions.Assert(_activeAnimations.ContainsKey(key));
        double targetPosition = Constraints.ScrollOffset + Constraints.RemainingCacheExtent;
        double currentPosition = position;
        int startingIndex = _activeAnimations[key].FromIndex;
        int lastIndex = _activeAnimations[key].ToIndex;
        int currentIndex = startingIndex;
        double totalAnimatingOffset = 0.0;
        // We animate only a portion of children that would be visible/in the cache extent, unless all
        // children would fit on the screen.
        while (currentIndex <= lastIndex && currentPosition < targetPosition)
        {
            double itemExtent = ItemExtentBuilder!(currentIndex, LayoutDimensions)!.Value;
            totalAnimatingOffset += itemExtent;
            currentPosition += itemExtent;
            currentIndex++;
        }

        // For the life of this animation, which affects all children following startingIndex (not
        // just those in this animation), this is the offset to use.
        _animationOffsets[key] = totalAnimatingOffset;
    }

    /// <inheritdoc />
    public override int GetMinChildIndexForScrollOffset(double scrollOffset, double itemExtent) =>
        GetChildIndexForScrollOffset(scrollOffset);

    /// <inheritdoc />
    public override int GetMaxChildIndexForScrollOffset(double scrollOffset, double itemExtent) =>
        GetChildIndexForScrollOffset(scrollOffset);

    private int GetChildIndexForScrollOffset(double scrollOffset)
    {
        if (scrollOffset == 0.0)
        {
            return 0;
        }

        double position = 0.0;
        int index = 0;
        double totalAnimationOffset = 0.0;
        int? childCount = ChildManager?.EstimatedChildCount;
        while (position < scrollOffset)
        {
            if (childCount is not null && index > childCount.Value - 1)
            {
                break;
            }

            double? itemExtent = ItemExtentBuilder!(index, LayoutDimensions);
            if (itemExtent is null)
            {
                break;
            }

            if (_animationLeadingIndices.TryGetValue(index, out UniqueKey? animationKey))
            {
                if (!_animationOffsets.ContainsKey(animationKey))
                {
                    // We have not computed the distance this block is traveling over the course of the
                    // animation, do so now.
                    ComputeAnimationOffsetFor(animationKey, position);
                }

                // We add the offset accounting for the animation value.
                totalAnimationOffset += _animationOffsets[animationKey]
                    * (1 - _activeAnimations[animationKey].Value);
            }

            position += itemExtent.Value - totalAnimationOffset;
            ++index;
        }

        return index - 1;
    }

    /// <inheritdoc />
    public override double ChildCrossAxisPosition(RenderObject child)
    {
        return ((TreeSliverNodeParentData)child.parentData!).Depth * Indentation;
    }

    /// <inheritdoc />
    public override double IndexToLayoutOffset(double itemExtent, int index)
    {
        double position = 0.0;
        int currentIndex = 0;
        double totalAnimationOffset = 0.0;
        int? childCount = ChildManager?.EstimatedChildCount;
        while (currentIndex < index)
        {
            if (childCount is not null && currentIndex > childCount.Value - 1)
            {
                break;
            }

            double? extent = ItemExtentBuilder!(currentIndex, LayoutDimensions);
            if (extent is null)
            {
                break;
            }

            if (_animationLeadingIndices.TryGetValue(currentIndex, out UniqueKey? animationKey))
            {
                DebugAssertions.Assert(_animationOffsets.ContainsKey(animationKey));
                totalAnimationOffset += _animationOffsets[animationKey]
                    * (1 - _activeAnimations[animationKey].Value);
            }

            position += extent.Value;
            currentIndex++;
        }

        return position - totalAnimationOffset;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dart's <c>RenderTreeSliver.paint</c>: the rows after each animating parent are painted in their
    /// own segment, clipped from the parent's trailing edge to the segment's last row.
    /// </remarks>
    public override void Paint(PaintingContext context, Point offset)
    {
        if (FirstChild is null)
        {
            return;
        }

        // TODO(Piinks): Clip based on actual visible area.
        RenderBox? nextChild = FirstChild;

        void PaintUpTo(int index, RenderBox? startWith, PaintingContext paintingContext, Point paintOffset)
        {
            RenderBox? child = startWith;
            while (child is not null && IndexOf(child) <= index)
            {
                double mainAxisDelta = ChildMainAxisPosition(child);
                var parentData = (TreeSliverNodeParentData)child.parentData!;
                var childOffset = new Point(
                    (parentData.Depth * Indentation) + paintOffset.X,
                    parentData.LayoutOffset!.Value - Constraints.ScrollOffset + paintOffset.Y);

                // If the child's visible interval (mainAxisDelta, mainAxisDelta + paintExtentOf(child))
                // does not intersect the paint extent interval (0, constraints.remainingPaintExtent),
                // it's hidden.
                if (mainAxisDelta < Constraints.RemainingPaintExtent && mainAxisDelta + PaintExtentOf(child) > 0)
                {
                    paintingContext.PaintChild(child, childOffset);
                }

                child = ChildAfter(child);
            }

            nextChild = child;
        }

        if (_animationLeadingIndices.Count == 0)
        {
            // There are no animations running.
            PaintUpTo(IndexOf(LastChild!), FirstChild, context, offset);
            return;
        }

        // We are animating.
        // Separate animating segments to clip for any overlap.
        int leadingIndex = IndexOf(FirstChild);
        List<int> animationIndices = [.. _animationLeadingIndices.Keys];
        animationIndices.Sort();
        var paintSegments = new List<(int LeadingIndex, int TrailingIndex)>();
        while (animationIndices.Count > 0)
        {
            int trailingIndex = animationIndices[0];
            animationIndices.RemoveAt(0);
            paintSegments.Add((leadingIndex, trailingIndex));
            leadingIndex = trailingIndex + 1;
        }

        paintSegments.Add((leadingIndex, IndexOf(LastChild!)));

        // Paint, clipping for all but the first segment.
        PaintUpTo(paintSegments[0].TrailingIndex, nextChild, context, offset);
        paintSegments.RemoveAt(0);
        // Paint the rest with clip layers.
        while (paintSegments.Count > 0)
        {
            (int LeadingIndex, int TrailingIndex) segment = paintSegments[0];
            paintSegments.RemoveAt(0);

            // Rect is calculated by the trailing edge of the parent (preceding leadingIndex), and the
            // trailing edge of the trailing index. We cannot rely on the leading edge of the leading
            // index, because it is currently moving.
            int parentIndex = Math.Max(segment.LeadingIndex - 1, 0);
            double leadingOffset = IndexToLayoutOffset(0.0, parentIndex)
                + (parentIndex == 0 ? 0.0 : ItemExtentBuilder!(parentIndex, LayoutDimensions)!.Value);
            double trailingOffset = IndexToLayoutOffset(0.0, segment.TrailingIndex)
                + ItemExtentBuilder!(segment.TrailingIndex, LayoutDimensions)!.Value;
            var rect = new Rect(
                new Point(0.0, leadingOffset),
                new Point(Constraints.CrossAxisExtent, trailingOffset));
            // We use the same animation key to keep track of the clip layer, unless this is the odd
            // man out segment.
            UniqueKey key = _animationLeadingIndices[parentIndex];
            if (!_clipHandles.TryGetValue(key, out LayerHandle<ClipRectLayer>? handle))
            {
                handle = new LayerHandle<ClipRectLayer>();
                _clipHandles[key] = handle;
            }

            handle.Layer = context.PushClipRect(
                NeedsCompositing,
                offset,
                rect,
                (PaintingContext clipContext, Point clipOffset) =>
                    PaintUpTo(segment.TrailingIndex, nextChild, clipContext, clipOffset),
                oldLayer: handle.Layer);
        }
    }
}
