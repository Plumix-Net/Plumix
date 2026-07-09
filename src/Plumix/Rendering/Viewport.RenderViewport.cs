using Avalonia;
using Avalonia.Media;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/viewport.dart (approximate)

namespace Plumix.Rendering;

public sealed class RenderViewport : RenderBox, IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderSliver, SliverPhysicalParentData> _container;
    private Axis _axis;
    private AxisDirection _axisDirection;
    private GrowthDirection _growthDirection;
    private double _offsetPixels;
    private double _cacheExtent;
    private CacheExtentStyle _cacheExtentStyle;
    private bool _shrinkWrap;
    private double _maxScrollExtent;
    private RenderSliverToBoxAdapter? _legacyChildSliver;

    public RenderViewport(
        Axis axis = Axis.Vertical,
        AxisDirection? axisDirection = null,
        GrowthDirection growthDirection = GrowthDirection.Forward,
        double offsetPixels = 0.0,
        double cacheExtent = 0.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        bool shrinkWrap = false,
        Action<double, double, double>? onViewportMetricsChanged = null,
        RenderBox? child = null)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderSliver, SliverPhysicalParentData>(this);
        _axisDirection = axisDirection ?? ScrollDirectionUtils.DefaultAxisDirection(axis);
        _axis = ScrollDirectionUtils.AxisDirectionToAxis(_axisDirection);
        _growthDirection = growthDirection;
        _offsetPixels = offsetPixels;
        _cacheExtent = Math.Max(0, cacheExtent);
        _cacheExtentStyle = cacheExtentStyle;
        _shrinkWrap = shrinkWrap;
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

    public Action<double, double, double>? OnViewportMetricsChanged { get; set; }

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

            double maxScrollExtent = Math.Max(0, layout.totalScrollExtent - viewportMainAxisExtent);
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
                _offsetPixels = currentOffset;
                _maxScrollExtent = maxScrollExtent;
                OnViewportMetricsChanged?.Invoke(viewportMainAxisExtent, 0, _maxScrollExtent);
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
        _maxScrollExtent = Math.Max(0, finalLayout.totalScrollExtent - viewportMainAxisExtent);
        _offsetPixels = UserOffsetFromEffective(finalLayout.scrollOffset, _maxScrollExtent);
        OnViewportMetricsChanged?.Invoke(viewportMainAxisExtent, 0, _maxScrollExtent);
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Size.Width <= 0 || Size.Height <= 0)
        {
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

    protected override Rect? DescribeApproximatePaintClip(RenderObject? child)
    {
        return new Rect(new Point(0, 0), Size);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        for (var child = FirstChild; child != null; child = _container.ChildAfter(child))
        {
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
        double currentScrollOffset = Math.Max(0, scrollOffset);

        for (int pass = 0; pass < 8; pass++)
        {
            var result = LayoutChildren(
                currentScrollOffset,
                viewportMainAxisExtent,
                crossAxisExtent);
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
            crossAxisExtent);
        return (currentScrollOffset, finalResult.totalScrollExtent, finalResult.paintedExtent);
    }

    private (double totalScrollExtent, double paintedExtent, double? scrollOffsetCorrection) LayoutChildren(
        double scrollOffset,
        double viewportMainAxisExtent,
        double crossAxisExtent)
    {
        double precedingScrollExtent = 0.0;
        double paintedExtent = 0.0;
        double cacheExtent = Math.Max(0, _cacheExtentStyle == CacheExtentStyle.Viewport
            ? _cacheExtent * viewportMainAxisExtent
            : _cacheExtent);
        double cacheStart = Math.Max(0, scrollOffset - cacheExtent);
        double cacheEnd = scrollOffset + viewportMainAxisExtent + cacheExtent;

        for (var child = FirstChild; child != null; child = _container.ChildAfter(child))
        {
            double localScrollOffset = Math.Max(0, scrollOffset - precedingScrollExtent);
            double remainingPaintExtent = Math.Max(0, viewportMainAxisExtent - paintedExtent);
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
                GrowthDirection: _growthDirection));

            if (Math.Abs(child.Geometry.ScrollOffsetCorrection) > 0.0001)
            {
                return (precedingScrollExtent, paintedExtent, child.Geometry.ScrollOffsetCorrection);
            }

            var parentData = (SliverPhysicalParentData)child.parentData!;
            parentData.offset = Axis == Axis.Vertical
                ? new Point(0, paintedExtent)
                : new Point(paintedExtent, 0);

            precedingScrollExtent += child.Geometry.ScrollExtent;
            paintedExtent += child.Geometry.PaintExtent;
        }

        return (precedingScrollExtent, paintedExtent, null);
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
