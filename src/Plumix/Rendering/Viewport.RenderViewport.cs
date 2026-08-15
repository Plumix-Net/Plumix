using Avalonia;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/viewport.dart

namespace Plumix.Rendering;

/// <summary>The order in which a viewport paints its slivers.</summary>
public enum SliverPaintOrder
{
    /// <summary>The first sliver in the child list paints on top of the following ones.</summary>
    FirstIsTop,

    /// <summary>The last sliver in the child list paints on top of the preceding ones.</summary>
    LastIsTop,
}

/// <summary>
/// A render object that is bigger on the inside: it displays a subset of its children, selected by a
/// <see cref="ViewportOffset"/>.
/// </summary>
/// <remarks>
/// Flutter declares this as <c>RenderViewportBase&lt;ParentDataClass&gt;</c> mixing in
/// <c>ContainerRenderObjectMixin</c>. Plumix composes the container mixin instead of mixing it in,
/// and its <see cref="RenderSliver"/> is a <see cref="RenderBox"/>, so hit testing goes through the
/// box protocol rather than <c>SliverHitTestResult</c>.
/// </remarks>
public abstract class RenderViewportBase<TParentData> : RenderBox, IRenderObjectContainer, IRenderAbstractViewport
    where TParentData : ContainerBoxParentData<RenderSliver>, new()
{
    private readonly RenderBoxContainerDefaultsMixin<RenderSliver, TParentData> _container;
    private AxisDirection _axisDirection;
    private AxisDirection _crossAxisDirection;
    private ViewportOffset _offset;
    private ScrollCacheExtent _scrollCacheExtent;
    private SliverPaintOrder _paintOrder;
    private Clip _clipBehavior;

    protected RenderViewportBase(
        ViewportOffset offset,
        AxisDirection? crossAxisDirection,
        AxisDirection axisDirection = AxisDirection.Down,
        ScrollCacheExtent? scrollCacheExtent = null,
        SliverPaintOrder paintOrder = SliverPaintOrder.FirstIsTop,
        Clip clipBehavior = Clip.HardEdge)
    {
        ArgumentNullException.ThrowIfNull(offset);
        // Flutter requires the cross axis direction; Plumix defaults it to the perpendicular
        // reading-order direction so a render object can be built without a BuildContext.
        AxisDirection resolvedCrossAxisDirection = crossAxisDirection
            ?? (ScrollDirectionUtils.AxisDirectionToAxis(axisDirection) == Axis.Vertical
                ? AxisDirection.Right
                : AxisDirection.Down);
        if (ScrollDirectionUtils.AxisDirectionToAxis(axisDirection)
            == ScrollDirectionUtils.AxisDirectionToAxis(resolvedCrossAxisDirection))
        {
            throw new ArgumentException(
                "The cross axis direction must be perpendicular to the main axis direction.",
                nameof(crossAxisDirection));
        }

        _container = new RenderBoxContainerDefaultsMixin<RenderSliver, TParentData>(this);
        _axisDirection = axisDirection;
        _crossAxisDirection = resolvedCrossAxisDirection;
        _offset = offset;
        _scrollCacheExtent = scrollCacheExtent
                             ?? ScrollCacheExtent.Pixels(RenderAbstractViewport.DefaultCacheExtent);
        _paintOrder = paintOrder;
        _clipBehavior = clipBehavior;
    }

    /// <summary>The direction in which the scroll offset increases.</summary>
    public AxisDirection AxisDirection
    {
        get => _axisDirection;
        set
        {
            if (_axisDirection == value)
            {
                return;
            }

            _axisDirection = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>The direction in which child should be laid out in the cross axis.</summary>
    public AxisDirection CrossAxisDirection
    {
        get => _crossAxisDirection;
        set
        {
            if (_crossAxisDirection == value)
            {
                return;
            }

            _crossAxisDirection = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>The axis along which the scroll offset increases.</summary>
    public Axis Axis => ScrollDirectionUtils.AxisDirectionToAxis(_axisDirection);

    /// <inheritdoc />
    public ViewportOffset Offset
    {
        get => _offset;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_offset, value))
            {
                return;
            }

            if (Owner != null)
            {
                _offset.RemoveListener(MarkNeedsLayout);
            }

            _offset = value;
            if (Owner != null)
            {
                _offset.AddListener(MarkNeedsLayout);
            }

            // We need to go through layout even if the new offset has the same pixels value as the
            // old offset so that we will apply our viewport and content dimensions.
            MarkNeedsLayout();
        }
    }

    /// <summary>The extent, in pixels or viewport fractions, laid out beyond the visible area.</summary>
    public ScrollCacheExtent ScrollCacheExtent
    {
        get => _scrollCacheExtent;
        set
        {
            ScrollCacheExtent effectiveValue = value
                                               ?? ScrollCacheExtent.Pixels(
                                                   RenderAbstractViewport.DefaultCacheExtent);
            if (_scrollCacheExtent == effectiveValue)
            {
                return;
            }

            _scrollCacheExtent = effectiveValue;
            MarkNeedsLayout();
        }
    }

    /// <summary>The raw value of <see cref="ScrollCacheExtent"/>, in its own <see cref="CacheExtentStyle"/>.</summary>
    /// <remarks>Flutter's deprecated <c>cacheExtent</c>; the style is preserved across assignments.</remarks>
    public double CacheExtent
    {
        get => _scrollCacheExtent.Value;
        set
        {
            ScrollCacheExtent = _scrollCacheExtent.Style == CacheExtentStyle.Viewport
                ? ScrollCacheExtent.Viewport(value)
                : ScrollCacheExtent.Pixels(value);
        }
    }

    /// <summary>Whether <see cref="CacheExtent"/> counts pixels or viewport fractions.</summary>
    /// <remarks>Flutter's deprecated <c>cacheExtentStyle</c>.</remarks>
    public CacheExtentStyle CacheExtentStyle
    {
        get => _scrollCacheExtent.Style;
        set
        {
            if (_scrollCacheExtent.Style == value)
            {
                return;
            }

            ScrollCacheExtent = value == CacheExtentStyle.Viewport
                ? ScrollCacheExtent.Viewport(_scrollCacheExtent.Value)
                : ScrollCacheExtent.Pixels(_scrollCacheExtent.Value);
        }
    }

    /// <summary>Which sliver paints on top when slivers overlap.</summary>
    public SliverPaintOrder PaintOrder
    {
        get => _paintOrder;
        set
        {
            if (_paintOrder == value)
            {
                return;
            }

            _paintOrder = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (_clipBehavior == value)
            {
                return;
            }

            _clipBehavior = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    public override bool IsRepaintBoundary => true;

    /// <summary>The resolved cache extent in pixels, established during layout.</summary>
    protected double? CalculatedCacheExtent { get; set; }

    /// <summary>Whether any child reported that it painted beyond the viewport bounds.</summary>
    protected abstract bool HasVisualOverflow { get; }

    protected override void OnAttach()
    {
        base.OnAttach();
        _offset.AddListener(MarkNeedsLayout);
    }

    protected override void OnDetach()
    {
        _offset.RemoveListener(MarkNeedsLayout);
        base.OnDetach();
    }

    public int ChildCount => _container.ChildCount;

    public RenderSliver? FirstChild => _container.FirstChild;

    public RenderSliver? LastChild => _container.LastChild;

    public RenderSliver? ChildAfter(RenderSliver child) => _container.ChildAfter(child);

    public RenderSliver? ChildBefore(RenderSliver child) => _container.ChildBefore(child);

    public virtual void Insert(RenderSliver child, RenderSliver? after = null)
    {
        _container.Insert(child, after);
    }

    public void Move(RenderSliver child, RenderSliver? after = null)
    {
        _container.Move(child, after);
    }

    public virtual void Remove(RenderSliver child)
    {
        _container.Remove(child);
    }

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after)
    {
        Insert((RenderSliver)child, (RenderSliver?)after);
    }

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after)
    {
        Move((RenderSliver)child, (RenderSliver?)after);
    }

    void IRenderObjectContainer.Remove(RenderObject child)
    {
        Remove((RenderSliver)child);
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not TParentData)
        {
            child.parentData = new TParentData();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (RenderSliver? child = FirstChild; child != null; child = _container.ChildAfter(child))
        {
            visitor(child);
        }
    }

    /// <summary>
    /// Walks the children in the order they are painted, which is the reverse of
    /// <see cref="ChildrenInHitTestOrder"/>.
    /// </summary>
    public IEnumerable<RenderSliver> ChildrenInPaintOrder => _paintOrder == SliverPaintOrder.FirstIsTop
        ? ChildrenLastToFirst()
        : ChildrenFirstToLast();

    /// <summary>Walks the children in the order they are hit tested.</summary>
    public IEnumerable<RenderSliver> ChildrenInHitTestOrder => _paintOrder == SliverPaintOrder.FirstIsTop
        ? ChildrenFirstToLast()
        : ChildrenLastToFirst();

    private List<RenderSliver> ChildrenFirstToLast()
    {
        var children = new List<RenderSliver>(ChildCount);
        for (RenderSliver? child = FirstChild; child != null; child = _container.ChildAfter(child))
        {
            children.Add(child);
        }

        return children;
    }

    private List<RenderSliver> ChildrenLastToFirst()
    {
        var children = new List<RenderSliver>(ChildCount);
        for (RenderSliver? child = LastChild; child != null; child = _container.ChildBefore(child))
        {
            children.Add(child);
        }

        return children;
    }

    protected override double ComputeMinIntrinsicWidth(double height) => ThrowIntrinsicsUnsupported();

    protected override double ComputeMaxIntrinsicWidth(double height) => ThrowIntrinsicsUnsupported();

    protected override double ComputeMinIntrinsicHeight(double width) => ThrowIntrinsicsUnsupported();

    protected override double ComputeMaxIntrinsicHeight(double width) => ThrowIntrinsicsUnsupported();

    /// <remarks>Flutter's <c>debugThrowIfNotCheckingIntrinsics</c>.</remarks>
    private double ThrowIntrinsicsUnsupported()
    {
        throw new InvalidOperationException(
            $"{GetType().Name} does not support returning intrinsic dimensions. Calculating the "
            + "intrinsic dimensions would require instantiating every child of the viewport, which "
            + $"defeats the point of viewports being lazy. {IntrinsicsHint}");
    }

    /// <summary>The closing hint of the intrinsics error message.</summary>
    protected virtual string IntrinsicsHint =>
        "If you are merely trying to shrink-wrap the viewport in the main axis direction, consider a "
        + "RenderShrinkWrappingViewport render object (ShrinkWrappingViewport widget), which achieves "
        + "that effect without implementing the intrinsic dimension API.";

    /// <summary>
    /// Lays out a contiguous run of slivers, starting with <paramref name="child"/> and walking with
    /// <paramref name="advance"/>, and returns the scroll offset correction one of them asked for
    /// (zero when the whole sequence was laid out).
    /// </summary>
    /// <remarks>Flutter's <c>RenderViewportBase.layoutChildSequence</c>.</remarks>
    protected double LayoutChildSequence(
        RenderSliver? child,
        double scrollOffset,
        double overlap,
        double layoutOffset,
        double remainingPaintExtent,
        double mainAxisExtent,
        double crossAxisExtent,
        GrowthDirection growthDirection,
        Func<RenderSliver, RenderSliver?> advance,
        double remainingCacheExtent,
        double cacheOrigin)
    {
        if (!double.IsFinite(scrollOffset) || scrollOffset < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(scrollOffset));
        }

        double initialLayoutOffset = layoutOffset;
        ScrollDirection adjustedUserScrollDirection = ApplyGrowthDirectionToScrollDirection(
            _offset.UserScrollDirection,
            growthDirection);
        double maxPaintOffset = layoutOffset + overlap;
        double precedingScrollExtent = 0.0;

        while (child != null)
        {
            double sliverScrollOffset = scrollOffset <= 0.0 ? 0.0 : scrollOffset;
            // If the scrollOffset is 0.0 the child may not be laid out yet; give it a cache origin
            // it can start painting from.
            double correctedCacheOrigin = Math.Max(cacheOrigin, -sliverScrollOffset);
            double cacheExtentCorrection = cacheOrigin - correctedCacheOrigin;

            child.LayoutWithSliverConstraints(new SliverConstraints(
                Axis: Axis,
                ScrollOffset: sliverScrollOffset,
                RemainingPaintExtent: Math.Max(0.0, remainingPaintExtent - layoutOffset + initialLayoutOffset),
                CrossAxisExtent: crossAxisExtent,
                ViewportMainAxisExtent: mainAxisExtent,
                CacheOrigin: correctedCacheOrigin,
                RemainingCacheExtent: Math.Max(0.0, remainingCacheExtent + cacheExtentCorrection),
                AxisDirection: _axisDirection,
                GrowthDirection: growthDirection,
                Overlap: maxPaintOffset - layoutOffset,
                PrecedingScrollExtent: precedingScrollExtent,
                UserScrollDirection: adjustedUserScrollDirection,
                CrossAxisDirection: _crossAxisDirection));

            SliverGeometry childLayoutGeometry = child.Geometry;

            // If the child overflowed, ask the viewport to relayout at a corrected scroll offset.
            if (childLayoutGeometry.ScrollOffsetCorrection != 0.0)
            {
                return childLayoutGeometry.ScrollOffsetCorrection;
            }

            double effectiveLayoutOffset = layoutOffset + childLayoutGeometry.PaintOrigin;

            // `effectiveLayoutOffset` is not the layout offset of the child after the trailing edge
            // of the viewport, because the child is not visible there; the scroll offset, which keeps
            // increasing, roughly orders the invisible children instead.
            if (childLayoutGeometry.Visible || scrollOffset > 0)
            {
                UpdateChildLayoutOffset(child, effectiveLayoutOffset, growthDirection);
            }
            else
            {
                UpdateChildLayoutOffset(child, -scrollOffset + initialLayoutOffset, growthDirection);
            }

            maxPaintOffset = Math.Max(effectiveLayoutOffset + childLayoutGeometry.PaintExtent, maxPaintOffset);
            scrollOffset -= childLayoutGeometry.ScrollExtent;
            precedingScrollExtent += childLayoutGeometry.ScrollExtent;
            layoutOffset += childLayoutGeometry.LayoutExtent;
            if (childLayoutGeometry.CacheExtent != 0.0)
            {
                remainingCacheExtent -= childLayoutGeometry.CacheExtent - cacheExtentCorrection;
                cacheOrigin = Math.Min(correctedCacheOrigin + childLayoutGeometry.CacheExtent, 0.0);
            }

            UpdateOutOfBandData(growthDirection, childLayoutGeometry);

            child = advance(child);
        }

        // We made it without a correction, whee!
        return 0.0;
    }

    /// <remarks>Flutter's <c>applyGrowthDirectionToScrollDirection</c>.</remarks>
    private static ScrollDirection ApplyGrowthDirectionToScrollDirection(
        ScrollDirection scrollDirection,
        GrowthDirection growthDirection)
    {
        return growthDirection == GrowthDirection.Forward
            ? scrollDirection
            : ScrollDirectionUtils.FlipScrollDirection(scrollDirection);
    }

    /// <summary>Records a child's geometry into the viewport's own out-of-band totals.</summary>
    protected abstract void UpdateOutOfBandData(
        GrowthDirection growthDirection,
        SliverGeometry childLayoutGeometry);

    /// <summary>Stores a child's layout offset in whatever form this viewport's parent data takes.</summary>
    protected abstract void UpdateChildLayoutOffset(
        RenderSliver child,
        double layoutOffset,
        GrowthDirection growthDirection);

    /// <summary>The offset at which the given child should be painted.</summary>
    public abstract Point PaintOffsetOf(RenderSliver child);

    /// <summary>
    /// The scroll offset within the viewport at which the given point in the given child is located.
    /// </summary>
    public abstract double ScrollOffsetOf(RenderSliver child, double scrollOffsetWithinChild);

    /// <summary>The total extent pinned by the slivers laid out before <paramref name="child"/>.</summary>
    public abstract double MaxScrollObstructionExtentBefore(RenderSliver child);

    /// <summary>Converts a main-axis position in this viewport into one inside the given child.</summary>
    public abstract double ComputeChildMainAxisPosition(RenderSliver child, double parentMainAxisPosition);

    /// <summary>Converts a layout offset in the given growth direction into a paint offset.</summary>
    protected Point ComputeAbsolutePaintOffset(
        RenderSliver child,
        double layoutOffset,
        GrowthDirection growthDirection)
    {
        return ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(_axisDirection, growthDirection) switch
        {
            AxisDirection.Up => new Point(0.0, Size.Height - layoutOffset - child.Geometry.PaintExtent),
            AxisDirection.Left => new Point(Size.Width - layoutOffset - child.Geometry.PaintExtent, 0.0),
            AxisDirection.Right => new Point(layoutOffset, 0.0),
            _ => new Point(0.0, layoutOffset),
        };
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (FirstChild is null)
        {
            return;
        }

        if (HasVisualOverflow && ClipBehavior != Clip.None)
        {
            context.PushClipRect(
                new Rect(offset, Size),
                clippedContext => PaintContents(clippedContext, offset),
                ClipBehavior);
            return;
        }

        PaintContents(context, offset);
    }

    private void PaintContents(PaintingContext context, Point offset)
    {
        foreach (RenderSliver child in ChildrenInPaintOrder)
        {
            if (child.Geometry.Visible)
            {
                context.PaintChild(child, offset + PaintOffsetOf(child));
            }
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        foreach (RenderSliver child in ChildrenInHitTestOrder)
        {
            if (!child.Geometry.Visible)
            {
                continue;
            }

            if (child.HitTest(result, position - PaintOffsetOf(child)))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public RevealedOffset GetOffsetToReveal(
        RenderObject target,
        double alignment,
        Rect? rect = null,
        Axis? axis = null)
    {
        ArgumentNullException.ThrowIfNull(target);

        // A one-dimensional viewport uses its own axis; a mismatched request is not an error.
        Axis effectiveAxis = Axis;

        double leadingScrollOffset = 0.0;
        RenderObject current = target;
        RenderBox? pivot = null;
        bool onlySlivers = target is RenderSliver;
        while (!ReferenceEquals(current.Parent, this))
        {
            if (current.Parent is not { } parent)
            {
                // Not a descendant of this viewport: nothing to reveal.
                return new RevealedOffset(_offset.Pixels, rect ?? target.PaintBounds);
            }

            // Flutter's `RenderSliver` is not a `RenderBox`, so its walk can test the type directly.
            // Plumix's is (see the sliver hit-testing divergence), so a nested sliver would otherwise
            // become the pivot and the descendant's paint offset inside it would be counted twice.
            if (current is RenderBox box && current is not RenderSliver)
            {
                pivot = box;
            }

            if (parent is RenderSliver parentSliver)
            {
                leadingScrollOffset += parentSliver.ChildScrollOffset(current) ?? 0.0;
            }
            else
            {
                // A non-sliver ancestor moves its child non-linearly with the scroll offset, so
                // everything accumulated so far is meaningless.
                onlySlivers = false;
                leadingScrollOffset = 0.0;
            }

            current = parent;
        }

        double pivotExtent;
        Rect rectLocal;
        GrowthDirection growthDirection;
        if (pivot != null)
        {
            var pivotParent = (RenderSliver)pivot.Parent!;
            growthDirection = pivotParent.ConstraintsForSliver.GrowthDirection;
            pivotExtent = effectiveAxis == Axis.Horizontal ? pivot.Size.Width : pivot.Size.Height;
            rect ??= target.PaintBounds;
            rectLocal = RenderObject.TransformRect(target.GetTransformTo(pivot), rect.Value);
        }
        else if (onlySlivers)
        {
            // The target is a sliver and there is no box between it and this viewport, so a rect is
            // made up from the sliver's own geometry.
            var targetSliver = (RenderSliver)target;
            growthDirection = targetSliver.ConstraintsForSliver.GrowthDirection;
            pivotExtent = targetSliver.Geometry.ScrollExtent;
            double crossAxisExtent = targetSliver.ConstraintsForSliver.CrossAxisExtent;
            rect ??= effectiveAxis == Axis.Horizontal
                ? new Rect(0.0, 0.0, targetSliver.Geometry.ScrollExtent, crossAxisExtent)
                : new Rect(0.0, 0.0, crossAxisExtent, targetSliver.Geometry.ScrollExtent);
            rectLocal = rect.Value;
        }
        else
        {
            return new RevealedOffset(_offset.Pixels, rect ?? target.PaintBounds);
        }

        var sliver = (RenderSliver)current;
        leadingScrollOffset += ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
            _axisDirection,
            growthDirection) switch
        {
            AxisDirection.Up => pivotExtent - rectLocal.Bottom,
            AxisDirection.Left => pivotExtent - rectLocal.Right,
            AxisDirection.Right => rectLocal.Left,
            _ => rectLocal.Top,
        };

        // The scroll offset at which the leading edge of the sliver would already be pinned in place.
        bool isPinned = sliver.Geometry.MaxScrollObstructionExtent > 0.0 && leadingScrollOffset >= 0.0;
        leadingScrollOffset = ScrollOffsetOf(sliver, leadingScrollOffset);

        Rect targetRect = RenderObject.TransformRect(target.GetTransformTo(this), rect.Value);
        double extentOfPinnedSlivers = MaxScrollObstructionExtentBefore(sliver);
        switch (sliver.ConstraintsForSliver.GrowthDirection)
        {
            case GrowthDirection.Forward:
                if (isPinned && alignment <= 0)
                {
                    // Aligning a pinned sliver's leading edge is already satisfied at every offset;
                    // the caller clamps this to the maximum scroll extent.
                    return new RevealedOffset(double.PositiveInfinity, targetRect);
                }

                leadingScrollOffset -= extentOfPinnedSlivers;
                break;
            case GrowthDirection.Reverse:
                if (isPinned && alignment >= 1)
                {
                    return new RevealedOffset(double.NegativeInfinity, targetRect);
                }

                // If child's growth direction is reverse, when viewport.offset is
                // `leadingScrollOffset`, it is positioned just outside of the leading edge of the
                // viewport.
                leadingScrollOffset -= effectiveAxis == Axis.Vertical
                    ? targetRect.Height
                    : targetRect.Width;
                break;
        }

        double mainAxisExtentDifference = effectiveAxis == Axis.Horizontal
            ? Size.Width - extentOfPinnedSlivers - rectLocal.Width
            : Size.Height - extentOfPinnedSlivers - rectLocal.Height;
        double targetOffset = leadingScrollOffset - mainAxisExtentDifference * alignment;
        double offsetDifference = _offset.Pixels - targetOffset;
        targetRect = _axisDirection switch
        {
            AxisDirection.Up => TranslateRect(targetRect, 0.0, -offsetDifference),
            AxisDirection.Down => TranslateRect(targetRect, 0.0, offsetDifference),
            AxisDirection.Left => TranslateRect(targetRect, -offsetDifference, 0.0),
            _ => TranslateRect(targetRect, offsetDifference, 0.0),
        };

        return new RevealedOffset(targetOffset, targetRect);
    }

    private static Rect TranslateRect(Rect rect, double dx, double dy)
    {
        return new Rect(rect.X + dx, rect.Y + dy, rect.Width, rect.Height);
    }

    public override void ShowOnScreen(
        RenderObject? descendant = null,
        Rect? rect = null,
        TimeSpan duration = default,
        Curve? curve = null)
    {
        if (!_offset.AllowImplicitScrolling)
        {
            base.ShowOnScreen(descendant, rect, duration, curve);
            return;
        }

        Rect? newRect = RenderAbstractViewport.ShowInViewport(
            this,
            _offset,
            descendant,
            rect,
            duration,
            curve);
        base.ShowOnScreen(rect: newRect, duration: duration, curve: curve);
    }

    /// <summary>
    /// Tags every semantics node below a viewport, so the enclosing scroll semantics boundary can tell
    /// that it is looking at viewport children and split them into a scrolling and a non-scrolling pane.
    /// </summary>
    public static readonly SemanticsTag UseTwoPaneSemantics = new("RenderViewport.twoPane");

    /// <summary>
    /// Tags the semantics nodes of viewport children that must not scroll with the viewport's content,
    /// such as a pinned header. They become siblings of the scrolling node instead of its children.
    /// </summary>
    public static readonly SemanticsTag ExcludeFromScrolling = new("RenderViewport.excludeFromScrolling");

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.AddTagForChildren(UseTwoPaneSemantics);
    }

    /// <summary>
    /// Whether <paramref name="child"/> still occupies paint or cache extent. A sliver that does not,
    /// and does not opt into <see cref="RenderSliver.EnsureSemantics"/>, contributes no semantics.
    /// </summary>
    private static bool IsSemanticallyRelevant(RenderSliver child)
    {
        return child.Geometry.Visible || child.Geometry.CacheExtent > 0.0 || child.EnsureSemantics;
    }

    protected override Rect? DescribeApproximatePaintClip(RenderObject? child)
    {
        if (child is RenderSliver sliver && sliver.EnsureSemantics && !IsSemanticallyRelevant(sliver))
        {
            return null;
        }

        if (ClipBehavior == Clip.None)
        {
            return null;
        }

        var viewportClip = new Rect(new Point(0, 0), Size);
        if (child is not RenderSliver clippedSliver)
        {
            return viewportClip;
        }

        SliverConstraints constraints = clippedSliver.ConstraintsForSliver;
        // The viewport's main axis extent is infinite for a shrink-wrapping viewport inside a flex,
        // which makes the overlap start meaningless.
        if (constraints.Overlap == 0 || double.IsInfinity(constraints.ViewportMainAxisExtent))
        {
            return viewportClip;
        }

        double left = viewportClip.Left;
        double right = viewportClip.Right;
        double top = viewportClip.Top;
        double bottom = viewportClip.Bottom;
        double startOfOverlap = constraints.ViewportMainAxisExtent - constraints.RemainingPaintExtent;
        double overlapCorrection = startOfOverlap + constraints.Overlap;
        switch (ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
                    _axisDirection,
                    constraints.GrowthDirection))
        {
            case AxisDirection.Down:
                top += overlapCorrection;
                break;
            case AxisDirection.Up:
                bottom -= overlapCorrection;
                break;
            case AxisDirection.Right:
                left += overlapCorrection;
                break;
            case AxisDirection.Left:
                right -= overlapCorrection;
                break;
        }

        return new Rect(new Point(left, top), new Point(right, bottom));
    }

    /// <summary>
    /// The viewport's semantics clip is its paint clip grown by the cache extent, so children that are
    /// laid out but scrolled off screen stay in the tree and are reported as hidden instead of dropped.
    /// </summary>
    protected override Rect? DescribeSemanticsClip(RenderObject? child)
    {
        if (child is RenderSliver sliver && sliver.EnsureSemantics && !IsSemanticallyRelevant(sliver))
        {
            return null;
        }

        var semanticBounds = new Rect(new Point(0, 0), Size);
        if (CalculatedCacheExtent is not { } cacheExtent)
        {
            return semanticBounds;
        }

        return Axis == Axis.Vertical
            ? new Rect(
                new Point(semanticBounds.Left, semanticBounds.Top - cacheExtent),
                new Point(semanticBounds.Right, semanticBounds.Bottom + cacheExtent))
            : new Rect(
                new Point(semanticBounds.Left - cacheExtent, semanticBounds.Top),
                new Point(semanticBounds.Right + cacheExtent, semanticBounds.Bottom));
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        // Flutter walks `childrenInPaintOrder` here; Plumix has no geometry-driven traversal sort, so
        // it keeps first-to-last order and applies only the visible-or-cached filter.
        foreach (RenderSliver child in ChildrenFirstToLast())
        {
            if (IsSemanticallyRelevant(child))
            {
                visitor(child);
            }
        }
    }
}

/// <summary>
/// A render object that is bigger on the inside, laying its slivers out in both growth directions
/// from a <see cref="Center"/> child.
/// </summary>
public class RenderViewport : RenderViewportBase<SliverPhysicalParentData>
{
    /// <summary>The maximum number of layout passes each child may force through a correction.</summary>
    private const int MaxLayoutCyclesPerChild = 10;

    private double _anchor;
    private RenderSliver? _center;
    private double _minScrollExtent;
    private double _maxScrollExtent;
    private bool _hasVisualOverflow;

    public RenderViewport(
        ViewportOffset offset,
        AxisDirection? crossAxisDirection = null,
        AxisDirection axisDirection = AxisDirection.Down,
        double anchor = 0.0,
        IReadOnlyList<RenderSliver>? children = null,
        RenderSliver? center = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        SliverPaintOrder paintOrder = SliverPaintOrder.FirstIsTop,
        Clip clipBehavior = Clip.HardEdge)
        : base(offset, crossAxisDirection, axisDirection, scrollCacheExtent, paintOrder, clipBehavior)
    {
        if (!double.IsFinite(anchor) || anchor < 0.0 || anchor > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(anchor));
        }

        _anchor = anchor;
        _center = center;
        if (children != null)
        {
            foreach (RenderSliver child in children)
            {
                Insert(child, after: LastChild);
            }
        }

        if (center == null && FirstChild != null)
        {
            _center = FirstChild;
        }
    }

    /// <summary>
    /// The relative position of the zero scroll offset, as a fraction of the viewport's main axis
    /// extent measured from the leading edge.
    /// </summary>
    public double Anchor
    {
        get => _anchor;
        set
        {
            if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (_anchor == value)
            {
                return;
            }

            _anchor = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>
    /// The first child in the <see cref="GrowthDirection.Forward"/> growth direction; every child
    /// before it grows in the reverse direction and occupies negative scroll offsets.
    /// </summary>
    public RenderSliver? Center
    {
        get => _center;
        set
        {
            if (ReferenceEquals(_center, value))
            {
                return;
            }

            _center = value;
            MarkNeedsLayout();
        }
    }

    /// <inheritdoc />
    protected override bool HasVisualOverflow => _hasVisualOverflow;

    public override void Insert(RenderSliver child, RenderSliver? after = null)
    {
        base.Insert(child, after);
        // Flutter's constructor falls back to `firstChild` when no center was supplied, and its
        // `_ViewportElement` assigns one on every mount/update. A render object driven directly
        // keeps that fallback alive as children arrive, so the first sliver anchors the viewport.
        _center ??= FirstChild;
    }

    public override void Remove(RenderSliver child)
    {
        if (ReferenceEquals(_center, child))
        {
            _center = null;
        }

        base.Remove(child);
    }

    protected override void PerformLayout()
    {
        Size = Constraints.Biggest;
        switch (Axis)
        {
            case Axis.Vertical:
                Offset.ApplyViewportDimension(Size.Height);
                break;
            default:
                Offset.ApplyViewportDimension(Size.Width);
                break;
        }

        if (Center is null)
        {
            _minScrollExtent = 0.0;
            _maxScrollExtent = 0.0;
            _hasVisualOverflow = false;
            Offset.ApplyContentDimensions(0.0, 0.0);
            return;
        }

        double mainAxisExtent = Axis == Axis.Vertical ? Size.Height : Size.Width;
        double crossAxisExtent = Axis == Axis.Vertical ? Size.Width : Size.Height;
        double centerOffsetAdjustment = Center.CenterOffsetAdjustment;
        int maxLayoutCycles = MaxLayoutCyclesPerChild * ChildCount;

        int count = 0;
        do
        {
            double correction = AttemptLayout(
                mainAxisExtent,
                crossAxisExtent,
                Offset.Pixels + centerOffsetAdjustment);
            if (correction != 0.0)
            {
                Offset.CorrectBy(correction);
            }
            else if (Offset.ApplyContentDimensions(
                         Math.Min(0.0, _minScrollExtent + mainAxisExtent * Anchor),
                         Math.Max(0.0, _maxScrollExtent - mainAxisExtent * (1.0 - Anchor))))
            {
                break;
            }

            count += 1;
        }
        while (count < maxLayoutCycles);
    }

    /// <remarks>Flutter's <c>RenderViewport._attemptLayout</c>.</remarks>
    private double AttemptLayout(double mainAxisExtent, double crossAxisExtent, double correctedOffset)
    {
        _minScrollExtent = 0.0;
        _maxScrollExtent = 0.0;
        _hasVisualOverflow = false;

        // Center offset: the distance from the leading edge to the zero scroll offset (the line
        // between the forward slivers and the reverse slivers).
        double centerOffset = mainAxisExtent * Anchor - correctedOffset;
        double reverseDirectionRemainingPaintExtent = Math.Clamp(centerOffset, 0.0, mainAxisExtent);
        double forwardDirectionRemainingPaintExtent =
            Math.Clamp(mainAxisExtent - centerOffset, 0.0, mainAxisExtent);

        double calculatedCacheExtent = ScrollCacheExtent.CalculateCacheOffset(mainAxisExtent);
        CalculatedCacheExtent = calculatedCacheExtent;

        double fullCacheExtent = mainAxisExtent + 2 * calculatedCacheExtent;
        double centerCacheOffset = centerOffset + calculatedCacheExtent;
        double reverseDirectionRemainingCacheExtent = Math.Clamp(centerCacheOffset, 0.0, fullCacheExtent);
        double forwardDirectionRemainingCacheExtent =
            Math.Clamp(fullCacheExtent - centerCacheOffset, 0.0, fullCacheExtent);

        RenderSliver? leadingNegativeChild = ChildBefore(Center!);
        if (leadingNegativeChild != null)
        {
            // The negative scroll offsets.
            double result = LayoutChildSequence(
                child: leadingNegativeChild,
                scrollOffset: Math.Max(mainAxisExtent, centerOffset) - mainAxisExtent,
                overlap: 0.0,
                layoutOffset: forwardDirectionRemainingPaintExtent,
                remainingPaintExtent: reverseDirectionRemainingPaintExtent,
                mainAxisExtent: mainAxisExtent,
                crossAxisExtent: crossAxisExtent,
                growthDirection: GrowthDirection.Reverse,
                advance: ChildBefore,
                remainingCacheExtent: reverseDirectionRemainingCacheExtent,
                cacheOrigin: Math.Clamp(mainAxisExtent - centerOffset, -calculatedCacheExtent, 0.0));
            if (result != 0.0)
            {
                return -result;
            }
        }

        // The positive scroll offsets.
        return LayoutChildSequence(
            child: Center,
            scrollOffset: Math.Max(0.0, -centerOffset),
            overlap: leadingNegativeChild == null ? Math.Min(0.0, -centerOffset) : 0.0,
            layoutOffset: centerOffset >= mainAxisExtent ? centerOffset : reverseDirectionRemainingPaintExtent,
            remainingPaintExtent: forwardDirectionRemainingPaintExtent,
            mainAxisExtent: mainAxisExtent,
            crossAxisExtent: crossAxisExtent,
            growthDirection: GrowthDirection.Forward,
            advance: ChildAfter,
            remainingCacheExtent: forwardDirectionRemainingCacheExtent,
            cacheOrigin: Math.Clamp(centerOffset, -calculatedCacheExtent, 0.0));
    }

    protected override void UpdateOutOfBandData(
        GrowthDirection growthDirection,
        SliverGeometry childLayoutGeometry)
    {
        switch (growthDirection)
        {
            case GrowthDirection.Forward:
                _maxScrollExtent += childLayoutGeometry.ScrollExtent;
                break;
            case GrowthDirection.Reverse:
                _minScrollExtent -= childLayoutGeometry.ScrollExtent;
                break;
        }

        if (childLayoutGeometry.HasVisualOverflow)
        {
            _hasVisualOverflow = true;
        }
    }

    protected override void UpdateChildLayoutOffset(
        RenderSliver child,
        double layoutOffset,
        GrowthDirection growthDirection)
    {
        var childParentData = (SliverPhysicalParentData)child.parentData!;
        childParentData.offset = ComputeAbsolutePaintOffset(child, layoutOffset, growthDirection);
    }

    public override Point PaintOffsetOf(RenderSliver child)
    {
        return ((SliverPhysicalParentData)child.parentData!).offset;
    }

    public override double ScrollOffsetOf(RenderSliver child, double scrollOffsetWithinChild)
    {
        switch (child.ConstraintsForSliver.GrowthDirection)
        {
            case GrowthDirection.Forward:
            {
                double scrollOffsetToChild = 0.0;
                RenderSliver? current = Center;
                while (current != null && !ReferenceEquals(current, child))
                {
                    scrollOffsetToChild += current.Geometry.ScrollExtent;
                    current = ChildAfter(current);
                }

                return scrollOffsetToChild + scrollOffsetWithinChild;
            }

            default:
            {
                double scrollOffsetToChild = 0.0;
                RenderSliver? current = ChildBefore(Center!);
                while (current != null && !ReferenceEquals(current, child))
                {
                    scrollOffsetToChild -= current.Geometry.ScrollExtent;
                    current = ChildBefore(current);
                }

                return scrollOffsetToChild - scrollOffsetWithinChild;
            }
        }
    }

    public override double MaxScrollObstructionExtentBefore(RenderSliver child)
    {
        double pinnedExtent = 0.0;
        switch (child.ConstraintsForSliver.GrowthDirection)
        {
            case GrowthDirection.Forward:
            {
                RenderSliver? current = Center;
                while (current != null && !ReferenceEquals(current, child))
                {
                    pinnedExtent += current.Geometry.MaxScrollObstructionExtent;
                    current = ChildAfter(current);
                }

                return pinnedExtent;
            }

            default:
            {
                RenderSliver? current = ChildBefore(Center!);
                while (current != null && !ReferenceEquals(current, child))
                {
                    pinnedExtent += current.Geometry.MaxScrollObstructionExtent;
                    current = ChildBefore(current);
                }

                return pinnedExtent;
            }
        }
    }

    public override double ComputeChildMainAxisPosition(RenderSliver child, double parentMainAxisPosition)
    {
        Point paintOffset = ((SliverPhysicalParentData)child.parentData!).offset;
        return ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
            child.ConstraintsForSliver.AxisDirection,
            child.ConstraintsForSliver.GrowthDirection) switch
        {
            AxisDirection.Down => parentMainAxisPosition - paintOffset.Y,
            AxisDirection.Right => parentMainAxisPosition - paintOffset.X,
            AxisDirection.Up => child.Geometry.PaintExtent - (parentMainAxisPosition - paintOffset.Y),
            _ => child.Geometry.PaintExtent - (parentMainAxisPosition - paintOffset.X),
        };
    }

    /// <summary>The index of the first child relative to <see cref="Center"/>, which is index zero.</summary>
    public int IndexOfFirstChild
    {
        get
        {
            int count = 0;
            RenderSliver? child = Center;
            while (child != null && !ReferenceEquals(child, FirstChild))
            {
                count -= 1;
                child = ChildBefore(child);
            }

            return count;
        }
    }

    /// <summary>The debug label of the child at the given index relative to <see cref="Center"/>.</summary>
    public static string LabelForChild(int index) => index == 0 ? "center child" : $"child {index}";
}

/// <summary>
/// A viewport that sizes itself to the total extent of its slivers along the main axis.
/// </summary>
public class RenderShrinkWrappingViewport : RenderViewportBase<SliverLogicalParentData>
{
    private double _maxScrollExtent;
    private double _shrinkWrapExtent;
    private bool _hasVisualOverflow;

    public RenderShrinkWrappingViewport(
        ViewportOffset offset,
        AxisDirection? crossAxisDirection = null,
        AxisDirection axisDirection = AxisDirection.Down,
        ScrollCacheExtent? scrollCacheExtent = null,
        SliverPaintOrder paintOrder = SliverPaintOrder.FirstIsTop,
        Clip clipBehavior = Clip.HardEdge,
        IReadOnlyList<RenderSliver>? children = null)
        : base(offset, crossAxisDirection, axisDirection, scrollCacheExtent, paintOrder, clipBehavior)
    {
        if (children != null)
        {
            foreach (RenderSliver child in children)
            {
                Insert(child, after: LastChild);
            }
        }
    }

    /// <inheritdoc />
    protected override bool HasVisualOverflow => _hasVisualOverflow;

    /// <inheritdoc />
    protected override string IntrinsicsHint =>
        "If you are merely trying to shrink-wrap the viewport in the main axis direction, you should "
        + "be able to achieve that effect by just giving the viewport loose constraints, without "
        + "needing to measure its intrinsic dimensions.";

    protected override void PerformLayout()
    {
        BoxConstraints constraints = Constraints;
        if (FirstChild is null)
        {
            CheckHasBoundedCrossAxis(constraints);
            Size = Axis == Axis.Vertical
                ? new Size(constraints.MaxWidth, constraints.MinHeight)
                : new Size(constraints.MinWidth, constraints.MaxHeight);
            Offset.ApplyViewportDimension(0.0);
            _maxScrollExtent = 0.0;
            _shrinkWrapExtent = 0.0;
            _hasVisualOverflow = false;
            Offset.ApplyContentDimensions(0.0, 0.0);
            return;
        }

        CheckHasBoundedCrossAxis(constraints);

        double mainAxisExtent;
        double crossAxisExtent;
        if (Axis == Axis.Vertical)
        {
            mainAxisExtent = constraints.MaxHeight;
            crossAxisExtent = constraints.MaxWidth;
        }
        else
        {
            mainAxisExtent = constraints.MaxWidth;
            crossAxisExtent = constraints.MaxHeight;
        }

        double effectiveExtent;
        while (true)
        {
            double correction = AttemptLayout(mainAxisExtent, crossAxisExtent, Offset.Pixels);
            if (correction != 0.0)
            {
                Offset.CorrectBy(correction);
                continue;
            }

            effectiveExtent = Axis == Axis.Vertical
                ? constraints.ConstrainHeight(_shrinkWrapExtent)
                : constraints.ConstrainWidth(_shrinkWrapExtent);
            bool didAcceptViewportDimension = Offset.ApplyViewportDimension(effectiveExtent);
            bool didAcceptContentDimension =
                Offset.ApplyContentDimensions(0.0, Math.Max(0.0, _maxScrollExtent - effectiveExtent));
            if (didAcceptViewportDimension && didAcceptContentDimension)
            {
                break;
            }
        }

        Size = Axis == Axis.Vertical
            ? new Size(constraints.ConstrainWidth(crossAxisExtent), constraints.ConstrainHeight(effectiveExtent))
            : new Size(constraints.ConstrainWidth(effectiveExtent), constraints.ConstrainHeight(crossAxisExtent));
    }

    private void CheckHasBoundedCrossAxis(BoxConstraints constraints)
    {
        if (Axis == Axis.Vertical)
        {
            if (!constraints.HasBoundedWidth)
            {
                throw new InvalidOperationException(
                    "Vertical viewport was given unbounded width. Viewports expand in the cross axis "
                    + "to fill their container and constrain their children to match their extent in "
                    + "the cross axis. In this case, a vertical shrinkwrapping viewport was given an "
                    + "unlimited amount of horizontal space in which to expand.");
            }

            return;
        }

        if (!constraints.HasBoundedHeight)
        {
            throw new InvalidOperationException(
                "Horizontal viewport was given unbounded height. Viewports expand in the cross axis "
                + "to fill their container and constrain their children to match their extent in the "
                + "cross axis. In this case, a horizontal shrinkwrapping viewport was given an "
                + "unlimited amount of vertical space in which to expand.");
        }
    }

    /// <remarks>Flutter's <c>RenderShrinkWrappingViewport._attemptLayout</c>.</remarks>
    private double AttemptLayout(double mainAxisExtent, double crossAxisExtent, double correctedOffset)
    {
        _maxScrollExtent = 0.0;
        _shrinkWrapExtent = 0.0;
        // Since the viewport is shrink wrapped, the content is always at the end of the viewport.
        _hasVisualOverflow = correctedOffset < 0.0;
        double calculatedCacheExtent = double.IsFinite(mainAxisExtent)
            ? ScrollCacheExtent.CalculateCacheOffset(mainAxisExtent)
            : 0.0;
        CalculatedCacheExtent = calculatedCacheExtent;

        return LayoutChildSequence(
            child: FirstChild,
            scrollOffset: Math.Max(0.0, correctedOffset),
            overlap: Math.Min(0.0, correctedOffset),
            layoutOffset: Math.Max(0.0, -correctedOffset),
            remainingPaintExtent: mainAxisExtent + Math.Min(0.0, correctedOffset),
            mainAxisExtent: mainAxisExtent,
            crossAxisExtent: crossAxisExtent,
            growthDirection: GrowthDirection.Forward,
            advance: ChildAfter,
            remainingCacheExtent: mainAxisExtent + 2 * calculatedCacheExtent,
            cacheOrigin: -calculatedCacheExtent);
    }

    protected override void UpdateOutOfBandData(
        GrowthDirection growthDirection,
        SliverGeometry childLayoutGeometry)
    {
        _maxScrollExtent += childLayoutGeometry.ScrollExtent;
        if (childLayoutGeometry.HasVisualOverflow)
        {
            _hasVisualOverflow = true;
        }

        _shrinkWrapExtent += childLayoutGeometry.MaxPaintExtent;
    }

    protected override void UpdateChildLayoutOffset(
        RenderSliver child,
        double layoutOffset,
        GrowthDirection growthDirection)
    {
        var childParentData = (SliverLogicalParentData)child.parentData!;
        childParentData.LayoutOffset = layoutOffset;
    }

    public override Point PaintOffsetOf(RenderSliver child)
    {
        var childParentData = (SliverLogicalParentData)child.parentData!;
        return ComputeAbsolutePaintOffset(
            child,
            childParentData.LayoutOffset ?? 0.0,
            GrowthDirection.Forward);
    }

    public override double ScrollOffsetOf(RenderSliver child, double scrollOffsetWithinChild)
    {
        double scrollOffsetToChild = 0.0;
        RenderSliver? current = FirstChild;
        while (current != null && !ReferenceEquals(current, child))
        {
            scrollOffsetToChild += current.Geometry.ScrollExtent;
            current = ChildAfter(current);
        }

        return scrollOffsetToChild + scrollOffsetWithinChild;
    }

    public override double MaxScrollObstructionExtentBefore(RenderSliver child)
    {
        double pinnedExtent = 0.0;
        RenderSliver? current = FirstChild;
        while (current != null && !ReferenceEquals(current, child))
        {
            pinnedExtent += current.Geometry.MaxScrollObstructionExtent;
            current = ChildAfter(current);
        }

        return pinnedExtent;
    }

    public override double ComputeChildMainAxisPosition(RenderSliver child, double parentMainAxisPosition)
    {
        double layoutOffset = ((SliverLogicalParentData)child.parentData!).LayoutOffset ?? 0.0;
        return ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
            child.ConstraintsForSliver.AxisDirection,
            child.ConstraintsForSliver.GrowthDirection) switch
        {
            AxisDirection.Down or AxisDirection.Right => parentMainAxisPosition - layoutOffset,
            AxisDirection.Up => Size.Height - parentMainAxisPosition - layoutOffset,
            _ => Size.Width - parentMainAxisPosition - layoutOffset,
        };
    }
}
