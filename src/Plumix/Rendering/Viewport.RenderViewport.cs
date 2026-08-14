using Avalonia;
using Avalonia.Media;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/viewport.dart (approximate)

namespace Plumix.Rendering;

/// <summary>
/// Reports the measured viewport extent and content extents to the owning scrollable.
/// </summary>
/// <returns>
/// The offset the viewport must lay out at, when applying the dimensions moved the scroll position
/// (Flutter's <c>applyViewportDimension</c> returning false); null when nothing was corrected.
/// </returns>
public delegate double? ViewportMetricsChangedCallback(
    double viewportExtent,
    double minScrollExtent,
    double maxScrollExtent);

public sealed class RenderViewport : RenderBox, IRenderObjectContainer, IRenderAbstractViewport
{
    private readonly RenderBoxContainerDefaultsMixin<RenderSliver, SliverPhysicalParentData> _container;
    private Axis _axis;
    private AxisDirection _axisDirection;
    private GrowthDirection _growthDirection;
    private ScrollDirection _userScrollDirection;
    private double _offsetPixels;
    private double _cacheExtent;
    private CacheExtentStyle _cacheExtentStyle;
    private bool _shrinkWrap;
    private double _anchor;
    private Clip _clipBehavior;
    private double _maxScrollExtent;
    private double? _calculatedCacheExtent;
    private double? _reportedPixels;
    private RenderSliverToBoxAdapter? _legacyChildSliver;

    public RenderViewport(
        Axis axis = Axis.Vertical,
        AxisDirection? axisDirection = null,
        GrowthDirection growthDirection = GrowthDirection.Forward,
        double offsetPixels = 0.0,
        double cacheExtent = 0.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        bool shrinkWrap = false,
        double anchor = 0.0,
        ViewportMetricsChangedCallback? onViewportMetricsChanged = null,
        RenderBox? child = null,
        Clip clipBehavior = Clip.HardEdge,
        ScrollDirection userScrollDirection = ScrollDirection.Idle)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderSliver, SliverPhysicalParentData>(this);
        _axisDirection = axisDirection ?? ScrollDirectionUtils.DefaultAxisDirection(axis);
        _axis = ScrollDirectionUtils.AxisDirectionToAxis(_axisDirection);
        _growthDirection = growthDirection;
        _userScrollDirection = userScrollDirection;
        _offsetPixels = offsetPixels;
        _cacheExtent = Math.Max(0, cacheExtent);
        _cacheExtentStyle = cacheExtentStyle;
        _shrinkWrap = shrinkWrap;
        Anchor = anchor;
        _clipBehavior = clipBehavior;
        OnViewportMetricsChanged = onViewportMetricsChanged;

        if (child != null)
        {
            Child = child;
        }
    }

    public Axis Axis
    {
        get => _axis;
        set
        {
            if (_axis == value)
            {
                return;
            }

            _axis = value;
            if (ScrollDirectionUtils.AxisDirectionToAxis(_axisDirection) != value)
            {
                _axisDirection = ScrollDirectionUtils.DefaultAxisDirection(value);
            }

            MarkNeedsLayout();
        }
    }

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
            _axis = ScrollDirectionUtils.AxisDirectionToAxis(value);
            MarkNeedsLayout();
        }
    }

    public GrowthDirection GrowthDirection
    {
        get => _growthDirection;
        set
        {
            if (_growthDirection == value)
            {
                return;
            }

            _growthDirection = value;
            MarkNeedsLayout();
        }
    }

    public ScrollDirection UserScrollDirection
    {
        get => _userScrollDirection;
        set
        {
            if (_userScrollDirection == value)
            {
                return;
            }

            _userScrollDirection = value;
            MarkNeedsLayout();
        }
    }

    public double OffsetPixels
    {
        get => _offsetPixels;
        set
        {
            if (Math.Abs(_offsetPixels - value) < 0.0001)
            {
                return;
            }

            _offsetPixels = value;
            MarkNeedsLayout();
        }
    }

    public ViewportMetricsChangedCallback? OnViewportMetricsChanged { get; set; }

    /// <inheritdoc />
    public ViewportMoveToCallback? OnMoveTo { get; set; }

    /// <inheritdoc />
    public bool AllowImplicitScrolling { get; set; } = true;

    double IRenderAbstractViewport.RevealOffsetPixels => _offsetPixels;

    public bool ShrinkWrap
    {
        get => _shrinkWrap;
        set
        {
            if (_shrinkWrap == value) return;
            _shrinkWrap = value;
            MarkNeedsLayout();
        }
    }

    public double Anchor
    {
        get => _anchor;
        set
        {
            if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (Math.Abs(_anchor - value) < 0.0001)
            {
                return;
            }

            _anchor = value;
            MarkNeedsLayout();
        }
    }

    public double CacheExtent
    {
        get => _cacheExtent;
        set
        {
            double normalized = Math.Max(0, value);
            if (Math.Abs(_cacheExtent - normalized) < 0.0001)
            {
                return;
            }

            _cacheExtent = normalized;
            MarkNeedsLayout();
        }
    }

    public CacheExtentStyle CacheExtentStyle
    {
        get => _cacheExtentStyle;
        set
        {
            if (_cacheExtentStyle == value)
            {
                return;
            }

            _cacheExtentStyle = value;
            MarkNeedsLayout();
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

    // Backward-compatible single child API used by existing tests/widgets.
    public RenderBox? Child
    {
        get => _legacyChildSliver?.Child;
        set
        {
            if (ReferenceEquals(_legacyChildSliver?.Child, value))
            {
                return;
            }

            if (_legacyChildSliver != null)
            {
                Remove(_legacyChildSliver);
                _legacyChildSliver = null;
            }

            if (value != null)
            {
                _legacyChildSliver = new RenderSliverToBoxAdapter(value);
                Insert(_legacyChildSliver, after: LastChild);
            }
        }
    }

    public int ChildCount => _container.ChildCount;

    public RenderSliver? FirstChild => _container.FirstChild;

    public RenderSliver? LastChild => _container.LastChild;

    public void Insert(RenderSliver child, RenderSliver? after = null)
    {
        _container.Insert(child, after);
    }

    public void Move(RenderSliver child, RenderSliver? after = null)
    {
        _container.Move(child, after);
    }

    public void Remove(RenderSliver child)
    {
        if (ReferenceEquals(child, _legacyChildSliver))
        {
            _legacyChildSliver = null;
        }

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
        if (child.parentData is not SliverPhysicalParentData)
        {
            child.parentData = new SliverPhysicalParentData();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (var child = FirstChild; child != null; child = _container.ChildAfter(child))
        {
            visitor(child);
        }
    }

    protected override void PerformLayout()
    {
        // A position that resolves its offset from the viewport extent (a page view's, say) only
        // learns its pixels once the dimensions reach it, so the reported correction is laid out
        // again in this same frame rather than surfacing as a one-frame flash.
        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (!LayoutOnce())
            {
                return;
            }
        }

        LayoutOnce();
    }

    /// <summary>Runs one layout pass; returns true when the scroll position corrected the offset.</summary>
    private bool LayoutOnce()
    {
        double laidOutOffset = _offsetPixels;
        PerformLayoutPass();
        if (_reportedPixels is not { } corrected
            || Math.Abs(corrected - laidOutOffset) <= 0.0001
            || Math.Abs(corrected - _offsetPixels) <= 0.0001)
        {
            return false;
        }

        _offsetPixels = corrected;
        return true;
    }

    private void PerformLayoutPass()
    {
        _reportedPixels = null;
        Size = Constraints.Constrain(Constraints.Biggest);

        double viewportMainAxisExtent = Axis == Axis.Vertical ? Size.Height : Size.Width;
        double crossAxisExtent = Axis == Axis.Vertical ? Size.Width : Size.Height;
        if (ShrinkWrap && double.IsFinite(viewportMainAxisExtent))
        {
            var probe = LayoutWithCorrections(0, viewportMainAxisExtent, crossAxisExtent);
            double desiredExtent = Math.Min(probe.totalScrollExtent, viewportMainAxisExtent);
            Size = Axis == Axis.Vertical
                ? Constraints.Constrain(new Size(Size.Width, desiredExtent))
                : Constraints.Constrain(new Size(desiredExtent, Size.Height));
            viewportMainAxisExtent = Axis == Axis.Vertical ? Size.Height : Size.Width;
        }
        double rawOffset = _offsetPixels;
        double currentOffset = Math.Max(0, _offsetPixels);
        double currentMaxScrollExtent = Math.Max(0, _maxScrollExtent);
        const double precisionErrorTolerance = 0.0001;

        for (int pass = 0; pass < 6; pass++)
        {
            double effectiveScrollOffset = EffectiveScrollOffsetForLayout(currentOffset, currentMaxScrollExtent);
            var layout = LayoutWithCorrections(
                scrollOffset: effectiveScrollOffset,
                viewportMainAxisExtent: viewportMainAxisExtent,
                crossAxisExtent: crossAxisExtent);

            double anchoredViewportExtent = viewportMainAxisExtent * (1.0 - Anchor);
            double maxScrollExtent = Math.Max(0, layout.totalScrollExtent - anchoredViewportExtent);
            double clampedOffset = Math.Clamp(currentOffset, 0, maxScrollExtent);
            if (Math.Abs(layout.scrollOffset - effectiveScrollOffset) > precisionErrorTolerance)
            {
                clampedOffset = UserOffsetFromEffective(layout.scrollOffset, maxScrollExtent);
            }

            double targetEffectiveScrollOffset = EffectiveScrollOffsetForLayout(clampedOffset, maxScrollExtent);
            bool offsetStable = Math.Abs(clampedOffset - currentOffset) <= precisionErrorTolerance;
            bool effectiveOffsetStable = Math.Abs(targetEffectiveScrollOffset - effectiveScrollOffset) <= precisionErrorTolerance;
            bool maxExtentStable = Math.Abs(maxScrollExtent - currentMaxScrollExtent) <= precisionErrorTolerance;

            if (offsetStable && effectiveOffsetStable && maxExtentStable)
            {
                currentOffset = clampedOffset;
                _maxScrollExtent = maxScrollExtent;

                // Offsets outside [0, maxScrollExtent] are legitimate under physics that allow
                // overscroll (iOS bouncing): keep them and lay the children out shifted by the
                // overscroll instead of clamping the position back into range.
                double inRangeOffset = Math.Clamp(rawOffset, 0, maxScrollExtent);
                bool correctedByLayout = Math.Abs(currentOffset - inRangeOffset) > precisionErrorTolerance;
                double overscroll = correctedByLayout ? 0.0 : rawOffset - inRangeOffset;
                if (Math.Abs(overscroll) > precisionErrorTolerance)
                {
                    LayoutWithCorrections(
                        scrollOffset: EffectiveScrollOffsetForOverscroll(rawOffset, maxScrollExtent),
                        viewportMainAxisExtent: viewportMainAxisExtent,
                        crossAxisExtent: crossAxisExtent);
                    _offsetPixels = rawOffset;
                }
                else
                {
                    _offsetPixels = currentOffset;
                }

                _reportedPixels = OnViewportMetricsChanged?.Invoke(viewportMainAxisExtent, 0, _maxScrollExtent);
                return;
            }

            currentOffset = clampedOffset;
            currentMaxScrollExtent = maxScrollExtent;
        }

        double finalEffectiveScrollOffset = EffectiveScrollOffsetForLayout(currentOffset, currentMaxScrollExtent);
        var finalLayout = LayoutWithCorrections(
            scrollOffset: finalEffectiveScrollOffset,
            viewportMainAxisExtent: viewportMainAxisExtent,
            crossAxisExtent: crossAxisExtent);
        double finalAnchoredViewportExtent = viewportMainAxisExtent * (1.0 - Anchor);
        _maxScrollExtent = Math.Max(0, finalLayout.totalScrollExtent - finalAnchoredViewportExtent);
        _offsetPixels = UserOffsetFromEffective(finalLayout.scrollOffset, _maxScrollExtent);
        _reportedPixels = OnViewportMetricsChanged?.Invoke(viewportMainAxisExtent, 0, _maxScrollExtent);
    }

    /// <summary>
    /// The scroll offset of the leading edge of <paramref name="child"/>, plus
    /// <paramref name="scrollOffsetWithinChild"/>.
    /// </summary>
    /// <remarks>
    /// Plumix's viewport has no <c>center</c> sliver, so every child grows forward from the first
    /// one; this is Flutter's <c>RenderShrinkWrappingViewport.scrollOffsetOf</c>.
    /// </remarks>
    public double ScrollOffsetOf(RenderSliver child, double scrollOffsetWithinChild)
    {
        double scrollOffsetToChild = 0.0;
        for (RenderSliver? current = FirstChild;
             current != null && !ReferenceEquals(current, child);
             current = _container.ChildAfter(current))
        {
            scrollOffsetToChild += current.Geometry.ScrollExtent;
        }

        return scrollOffsetToChild + scrollOffsetWithinChild;
    }

    /// <summary>The total extent pinned by the slivers laid out before <paramref name="child"/>.</summary>
    public double MaxScrollObstructionExtentBefore(RenderSliver child)
    {
        double pinnedExtent = 0.0;
        for (RenderSliver? current = FirstChild;
             current != null && !ReferenceEquals(current, child);
             current = _container.ChildAfter(current))
        {
            pinnedExtent += current.Geometry.MaxScrollObstructionExtent;
        }

        return pinnedExtent;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Plumix lays every sliver out in the physically forward direction and expresses a reversed
    /// axis purely through the offset mapping, so the leading edge is taken in layout space and the
    /// resulting scroll offset is mapped back to the position's pixels at the end. Flutter selects
    /// the edge from the effective axis direction instead and needs no final mapping.
    /// </remarks>
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
                return new RevealedOffset(_offsetPixels, rect ?? target.PaintBounds);
            }

            if (current is RenderBox box)
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
        if (pivot != null)
        {
            pivotExtent = effectiveAxis == Axis.Horizontal ? pivot.Size.Width : pivot.Size.Height;
            rect ??= target.PaintBounds;
            rectLocal = RenderObject.TransformRect(target.GetTransformTo(pivot), rect.Value);
        }
        else if (onlySlivers)
        {
            var targetSliver = (RenderSliver)target;
            pivotExtent = targetSliver.Geometry.ScrollExtent;
            double crossAxisExtent = targetSliver.ConstraintsForSliver.CrossAxisExtent;
            rect ??= effectiveAxis == Axis.Horizontal
                ? new Rect(0.0, 0.0, targetSliver.Geometry.ScrollExtent, crossAxisExtent)
                : new Rect(0.0, 0.0, crossAxisExtent, targetSliver.Geometry.ScrollExtent);
            rectLocal = rect.Value;
        }
        else
        {
            return new RevealedOffset(_offsetPixels, rect ?? target.PaintBounds);
        }

        var sliver = (RenderSliver)current;
        leadingScrollOffset += effectiveAxis == Axis.Horizontal ? rectLocal.Left : rectLocal.Top;
        bool isPinned = sliver.Geometry.MaxScrollObstructionExtent > 0.0 && leadingScrollOffset >= 0.0;
        leadingScrollOffset = ScrollOffsetOf(sliver, leadingScrollOffset);

        Rect targetRect = RenderObject.TransformRect(target.GetTransformTo(this), rect.Value);
        double extentOfPinnedSlivers = MaxScrollObstructionExtentBefore(sliver);
        if (isPinned && alignment <= 0.0)
        {
            // Aligning a pinned sliver's leading edge is already satisfied at every offset; the
            // caller clamps this to the maximum scroll extent.
            return new RevealedOffset(double.PositiveInfinity, targetRect);
        }

        leadingScrollOffset -= extentOfPinnedSlivers;
        double mainAxisExtentDifference = effectiveAxis == Axis.Horizontal
            ? Size.Width - extentOfPinnedSlivers - rectLocal.Width
            : Size.Height - extentOfPinnedSlivers - rectLocal.Height;
        double targetLayoutOffset = leadingScrollOffset - mainAxisExtentDifference * alignment;
        double targetOffset = UserOffsetFromEffectiveUnclamped(targetLayoutOffset);
        double offsetDifference = _offsetPixels - targetOffset;
        targetRect = _axisDirection switch
        {
            AxisDirection.Up => TranslateRect(targetRect, 0.0, -offsetDifference),
            AxisDirection.Down => TranslateRect(targetRect, 0.0, offsetDifference),
            AxisDirection.Left => TranslateRect(targetRect, -offsetDifference, 0.0),
            _ => TranslateRect(targetRect, offsetDifference, 0.0),
        };

        return new RevealedOffset(targetOffset, targetRect);
    }

    public override void ShowOnScreen(
        RenderObject? descendant = null,
        Rect? rect = null,
        TimeSpan duration = default,
        Curve? curve = null)
    {
        if (!AllowImplicitScrolling)
        {
            base.ShowOnScreen(descendant, rect, duration, curve);
            return;
        }

        Rect? revealed = RenderAbstractViewport.ShowInViewport(this, descendant, rect, duration, curve);
        base.ShowOnScreen(rect: revealed, duration: duration, curve: curve);
    }

    private static Rect TranslateRect(Rect rect, double dx, double dy)
    {
        return new Rect(rect.X + dx, rect.Y + dy, rect.Width, rect.Height);
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Size.Width <= 0 || Size.Height <= 0)
        {
            return;
        }

        if (ClipBehavior == Clip.None)
        {
            PaintChildrenFirstIsTop(ctx, offset);
            return;
        }

        var clipRect = new Rect(offset, Size);
        ctx.PushClipRect(clipRect, clippedContext => PaintChildrenFirstIsTop(clippedContext, offset));
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (position.X < 0 || position.Y < 0 || position.X > Size.Width || position.Y > Size.Height)
        {
            return false;
        }

        // Flutter viewports use SliverPaintOrder.firstIsTop by default. Since the
        // first sliver is painted last, hit testing must inspect it first.
        for (var child = FirstChild; child != null; child = _container.ChildAfter(child))
        {
            var parentData = (SliverPhysicalParentData)child.parentData!;
            if (child.HitTest(result, position - parentData.offset))
            {
                return true;
            }
        }

        return false;
    }

    private void PaintChildrenFirstIsTop(PaintingContext context, Point offset)
    {
        // Match RenderViewport's default SliverPaintOrder.firstIsTop. This is
        // essential for pinned headers: following list slivers may paint into
        // the header's area, but the leading header must remain above them.
        for (var child = LastChild; child != null; child = _container.ChildBefore(child))
        {
            var parentData = (SliverPhysicalParentData)child.parentData!;
            if (child.Geometry.PaintExtent > 0)
            {
                context.PaintChild(child, parentData.offset + offset);
            }
        }
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
        double cacheExtent = _calculatedCacheExtent ?? 0.0;
        if (_calculatedCacheExtent is null)
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

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        // Flutter walks `childrenInPaintOrder` here; Plumix has no geometry-driven traversal sort, so it
        // keeps first-to-last order and applies only the visible-or-cached filter.
        for (var child = FirstChild; child != null; child = _container.ChildAfter(child))
        {
            if (!IsSemanticallyRelevant(child))
            {
                continue;
            }

            var parentData = (SliverPhysicalParentData)child.parentData!;
            visitor(child, parentData.offset, Matrix.Identity);
        }
    }

    private (double scrollOffset, double totalScrollExtent, double paintedExtent) LayoutWithCorrections(
        double scrollOffset,
        double viewportMainAxisExtent,
        double crossAxisExtent)
    {
        const double precisionErrorTolerance = 0.0001;

        // A scroll offset before the leading edge is not passed to the slivers: they keep laying out
        // from zero and the whole sequence is shifted down by the overscroll instead.
        double leadingOverscroll = Math.Max(0.0, -scrollOffset);
        double currentScrollOffset = Math.Max(0, scrollOffset);

        for (int pass = 0; pass < 8; pass++)
        {
            var result = LayoutChildren(
                currentScrollOffset,
                viewportMainAxisExtent,
                crossAxisExtent,
                leadingOverscroll);
            if (!result.scrollOffsetCorrection.HasValue
                || Math.Abs(result.scrollOffsetCorrection.Value) <= precisionErrorTolerance)
            {
                return (currentScrollOffset, result.totalScrollExtent, result.paintedExtent);
            }

            currentScrollOffset = Math.Max(0, currentScrollOffset + result.scrollOffsetCorrection.Value);
        }

        var finalResult = LayoutChildren(
            currentScrollOffset,
            viewportMainAxisExtent,
            crossAxisExtent,
            leadingOverscroll);
        return (currentScrollOffset, finalResult.totalScrollExtent, finalResult.paintedExtent);
    }

    private (double totalScrollExtent, double paintedExtent, double? scrollOffsetCorrection) LayoutChildren(
        double scrollOffset,
        double viewportMainAxisExtent,
        double crossAxisExtent,
        double leadingOverscroll = 0.0)
    {
        double precedingScrollExtent = 0.0;
        double layoutOffset = viewportMainAxisExtent * Anchor + leadingOverscroll;
        double maxPaintOffset = leadingOverscroll;
        double cacheExtent = Math.Max(0, _cacheExtentStyle == CacheExtentStyle.Viewport
            ? _cacheExtent * viewportMainAxisExtent
            : _cacheExtent);
        _calculatedCacheExtent = cacheExtent;
        double cacheStart = Math.Max(0, scrollOffset - cacheExtent);
        double cacheEnd = scrollOffset + viewportMainAxisExtent + cacheExtent;

        for (var child = FirstChild; child != null; child = _container.ChildAfter(child))
        {
            double localScrollOffset = Math.Max(0, scrollOffset - precedingScrollExtent);
            double remainingPaintExtent = Math.Max(0, viewportMainAxisExtent - layoutOffset);
            double localCacheStart = Math.Max(0, cacheStart - precedingScrollExtent);
            double localCacheEnd = Math.Max(localCacheStart, cacheEnd - precedingScrollExtent);
            double remainingCacheExtent = Math.Max(0, localCacheEnd - localCacheStart);
            double cacheOrigin = localCacheStart - localScrollOffset;

            child.LayoutWithSliverConstraints(new SliverConstraints(
                Axis,
                localScrollOffset,
                remainingPaintExtent,
                crossAxisExtent,
                viewportMainAxisExtent,
                CacheOrigin: cacheOrigin,
                RemainingCacheExtent: remainingCacheExtent,
                AxisDirection: _axisDirection,
                GrowthDirection: _growthDirection,
                // The leading sliver receives the leading overscroll as a negative overlap, which is
                // what overscroll-aware slivers stretch into.
                Overlap: ReferenceEquals(child, FirstChild)
                    ? -leadingOverscroll
                    : maxPaintOffset - layoutOffset,
                PrecedingScrollExtent: precedingScrollExtent,
                UserScrollDirection: _userScrollDirection));

            if (Math.Abs(child.Geometry.ScrollOffsetCorrection) > 0.0001)
            {
                return (precedingScrollExtent, maxPaintOffset, child.Geometry.ScrollOffsetCorrection);
            }

            var parentData = (SliverPhysicalParentData)child.parentData!;
            double effectiveLayoutOffset = layoutOffset + child.Geometry.PaintOrigin;
            parentData.offset = Axis == Axis.Vertical
                ? new Point(0, effectiveLayoutOffset)
                : new Point(effectiveLayoutOffset, 0);

            maxPaintOffset = Math.Max(
                maxPaintOffset,
                effectiveLayoutOffset + child.Geometry.PaintExtent);
            precedingScrollExtent += child.Geometry.ScrollExtent;
            layoutOffset += child.Geometry.LayoutExtent;
        }

        return (precedingScrollExtent, maxPaintOffset, null);
    }

    private double EffectiveScrollOffsetForLayout(double userOffset, double maxScrollExtent)
    {
        double clampedOffset = Math.Clamp(userOffset, 0, Math.Max(0, maxScrollExtent));
        if (!ScrollDirectionUtils.AxisDirectionIsReversed(_axisDirection))
        {
            return clampedOffset;
        }

        return Math.Max(0, maxScrollExtent - clampedOffset);
    }

    /// <summary>
    /// Maps a user offset that may lie outside <c>[0, maxScrollExtent]</c> into layout space without
    /// clamping, so the overscroll survives into the sliver layout.
    /// </summary>
    private double EffectiveScrollOffsetForOverscroll(double userOffset, double maxScrollExtent)
    {
        if (!ScrollDirectionUtils.AxisDirectionIsReversed(_axisDirection))
        {
            return userOffset;
        }

        return Math.Max(0, maxScrollExtent) - userOffset;
    }

    /// <summary>
    /// Maps a layout-space scroll offset back to the position's pixels without clamping, so a reveal
    /// target outside the current extents survives for the caller to clamp.
    /// </summary>
    private double UserOffsetFromEffectiveUnclamped(double effectiveOffset)
    {
        if (!ScrollDirectionUtils.AxisDirectionIsReversed(_axisDirection))
        {
            return effectiveOffset;
        }

        return Math.Max(0, _maxScrollExtent) - effectiveOffset;
    }

    private double UserOffsetFromEffective(double effectiveOffset, double maxScrollExtent)
    {
        double clampedEffectiveOffset = Math.Clamp(effectiveOffset, 0, Math.Max(0, maxScrollExtent));
        if (!ScrollDirectionUtils.AxisDirectionIsReversed(_axisDirection))
        {
            return clampedEffectiveOffset;
        }

        return Math.Max(0, maxScrollExtent - clampedEffectiveOffset);
    }
}
