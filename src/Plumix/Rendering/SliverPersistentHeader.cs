using Avalonia;
using Plumix.Widgets;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/sliver_persistent_header.dart

namespace Plumix.Rendering;

/// <summary>
/// Specifies how a stretched header is to trigger an <see cref="OnStretchTrigger"/>.
/// </summary>
public sealed class OverScrollHeaderStretchConfiguration
{
    public OverScrollHeaderStretchConfiguration(
        double stretchTriggerOffset = 100.0,
        Func<Task>? onStretchTrigger = null)
    {
        StretchTriggerOffset = stretchTriggerOffset;
        OnStretchTrigger = onStretchTrigger;
    }

    /// <summary>The offset of overscroll required to trigger <see cref="OnStretchTrigger"/>.</summary>
    public double StretchTriggerOffset { get; }

    /// <summary>The callback invoked when the header reaches <see cref="StretchTriggerOffset"/>.</summary>
    public Func<Task>? OnStretchTrigger { get; }
}

/// <summary>
/// Specifies how a floating header is to be "snapped" (animated) into or out of view.
/// </summary>
public sealed class FloatingHeaderSnapConfiguration
{
    public FloatingHeaderSnapConfiguration(Curve? curve = null, TimeSpan? duration = null)
    {
        Curve = curve ?? Curves.Ease;
        Duration = duration ?? TimeSpan.FromMilliseconds(300);
    }

    /// <summary>The curve to use for the snap animation.</summary>
    public Curve Curve { get; }

    /// <summary>The duration of the snap animation.</summary>
    public TimeSpan Duration { get; }
}

/// <summary>
/// A base class for slivers that have a <see cref="RenderBox"/> child which scrolls normally, but
/// that stays pinned when the sliver would otherwise start to scroll off the leading edge.
/// </summary>
public abstract class RenderSliverPersistentHeader : RenderSliverSingleBoxAdapter
{
    private double _minExtent;
    private double _maxExtent;
    private bool _needsUpdateChild = true;
    private double _lastShrinkOffset;
    private bool _lastOverlapsContent;
    private double _lastStretchOffset;

    protected RenderSliverPersistentHeader(
        double minExtent,
        double maxExtent,
        RenderBox? child = null,
        OverScrollHeaderStretchConfiguration? stretchConfiguration = null)
    {
        ValidateExtents(minExtent, maxExtent);
        _minExtent = minExtent;
        _maxExtent = maxExtent;
        StretchConfiguration = stretchConfiguration;
        Child = child;
    }

    /// <summary>The smallest size to allow the header to reach when it shrinks at the start of the viewport.</summary>
    /// <remarks>
    /// Flutter reads this from the header's delegate on every access; C# has no mixins, so the widget
    /// layer pushes the delegate's value here instead. Both extents are contractually constant for
    /// the lifetime of one delegate, so the observable behavior is the same.
    /// </remarks>
    public double MinExtent
    {
        get => _minExtent;
        set
        {
            ValidateExtents(value, _maxExtent);
            if (Close(_minExtent, value))
            {
                return;
            }

            _minExtent = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>The biggest size the header can have.</summary>
    public double MaxExtent
    {
        get => _maxExtent;
        set
        {
            ValidateExtents(_minExtent, value);
            if (Close(_maxExtent, value))
            {
                return;
            }

            _maxExtent = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>Configuration for the stretch behavior of the header, or null to disable stretching.</summary>
    public OverScrollHeaderStretchConfiguration? StretchConfiguration { get; set; }

    /// <summary>The shrink offset the child was last built with.</summary>
    public double LastShrinkOffset => _lastShrinkOffset;

    /// <summary>Whether the child was last built for a header that overlaps following content.</summary>
    public bool LastOverlapsContent => _lastOverlapsContent;

    /// <summary>
    /// The element hook that rebuilds the child during layout. Flutter reaches its element through
    /// <c>_RenderSliverPersistentHeaderForWidgetsMixin</c>; C# has no mixins, so the element installs
    /// this callback instead.
    /// </summary>
    internal Action<double, bool>? ChildBuilder { get; set; }

    /// <summary>The main-axis extent of the child, or zero when there is none.</summary>
    protected double ChildExtent
    {
        get
        {
            if (Child == null)
            {
                return 0.0;
            }

            return ConstraintsForSliver.Axis == Axis.Vertical ? Child.Size.Height : Child.Size.Width;
        }
    }

    /// <summary>
    /// A persistent header never scrolls with the viewport's content, so its semantics nodes are tagged
    /// out of the scrolling pane and become siblings of the scrolling node.
    /// </summary>
    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.AddTagForChildren(RenderViewport.ExcludeFromScrolling);
    }

    public override void MarkNeedsLayout()
    {
        // This is automatically called whenever the child's intrinsic dimensions change, at which
        // point we should remeasure the child.
        _needsUpdateChild = true;
        base.MarkNeedsLayout();
    }

    /// <summary>Rebuilds the child for the given shrink offset. The widget layer overrides this.</summary>
    protected virtual void UpdateChild(double shrinkOffset, bool overlapsContent)
    {
        ChildBuilder?.Invoke(shrinkOffset, overlapsContent);
    }

    /// <summary>Lays the child out, applying any overscroll stretch and firing the stretch trigger.</summary>
    protected void LayoutChild(double scrollOffset, double maxExtent, bool overlapsContent = false)
    {
        double shrinkOffset = Math.Min(scrollOffset, maxExtent);
        if (_needsUpdateChild || _lastShrinkOffset != shrinkOffset || _lastOverlapsContent != overlapsContent)
        {
            InvokeLayoutCallback<SliverConstraints>(
                _ => UpdateChild(shrinkOffset, overlapsContent),
                ConstraintsForSliver);
            _lastShrinkOffset = shrinkOffset;
            _lastOverlapsContent = overlapsContent;
            _needsUpdateChild = false;
        }

        if (MinExtent > maxExtent)
        {
            throw new InvalidOperationException(
                $"The maxExtent for this {GetType().Name} is less than its minExtent. "
                + $"The specified maxExtent was {maxExtent}. The specified minExtent was {MinExtent}.");
        }

        SliverConstraints constraints = ConstraintsForSliver;
        double stretchOffset = 0.0;
        if (StretchConfiguration != null && constraints.ScrollOffset == 0.0)
        {
            stretchOffset += Math.Abs(constraints.Overlap);
        }

        Child?.Layout(
            constraints.AsBoxConstraints(maxExtent: Math.Max(MinExtent, maxExtent - shrinkOffset) + stretchOffset),
            parentUsesSize: true);

        if (StretchConfiguration is { OnStretchTrigger: not null } stretch
            && stretchOffset >= stretch.StretchTriggerOffset
            && _lastStretchOffset <= stretch.StretchTriggerOffset)
        {
            _ = stretch.OnStretchTrigger!();
        }

        _lastStretchOffset = stretchOffset;
    }

    /// <summary>
    /// The overscroll extent this header's geometry reports, which — unlike the extent
    /// <see cref="LayoutChild"/> stretches into — is not gated on a zero scroll offset.
    /// </summary>
    protected double GeometryStretchOffset =>
        StretchConfiguration != null ? Math.Abs(ConstraintsForSliver.Overlap) : 0.0;

    /// <summary>
    /// Places the child at the offset implied by <see cref="RenderSliver.ChildMainAxisPosition"/>.
    /// Flutter applies the same mapping inside <c>paint</c>; Plumix stores it in the child's parent
    /// data so painting, hit testing and semantics all read one offset.
    /// </summary>
    protected void UpdateChildPaintOffset()
    {
        if (Child == null)
        {
            return;
        }

        double position = ChildMainAxisPosition(Child);
        double childExtent = ChildExtent;
        double paintExtent = Geometry.PaintExtent;
        ((BoxParentData)Child.parentData!).offset =
            PersistentHeaderReveal.EffectiveAxisDirection(ConstraintsForSliver) switch
            {
                AxisDirection.Up => new Point(0.0, paintExtent - position - childExtent),
                AxisDirection.Left => new Point(paintExtent - position - childExtent, 0.0),
                AxisDirection.Right => new Point(position, 0.0),
                _ => new Point(0.0, position),
            };
    }

    private static void ValidateExtents(double minExtent, double maxExtent)
    {
        if (!double.IsFinite(minExtent) || minExtent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minExtent));
        }

        if (!double.IsFinite(maxExtent) || maxExtent < minExtent)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExtent));
        }
    }

    private static bool Close(double a, double b) => Math.Abs(a - b) <= 0.0001;

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(DoubleProperty.Lazy("maxExtent", () => MaxExtent));
        properties.Add(DoubleProperty.Lazy(
            "child position",
            () => Child is null ? null : ChildMainAxisPosition(Child)));
    }
}

/// <summary>A sliver with a <see cref="RenderBox"/> child which scrolls normally.</summary>
public class RenderSliverScrollingPersistentHeader : RenderSliverPersistentHeader
{
    private double? _childPosition;

    public RenderSliverScrollingPersistentHeader(
        double minExtent,
        double maxExtent,
        RenderBox? child = null,
        OverScrollHeaderStretchConfiguration? stretchConfiguration = null)
        : base(minExtent, maxExtent, child, stretchConfiguration)
    {
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        LayoutChild(constraints.ScrollOffset, MaxExtent);
        _childPosition = UpdateGeometry();
        UpdateChildPaintOffset();
    }

    /// <summary>Updates <see cref="RenderSliver.Geometry"/> and returns the child's main axis position.</summary>
    protected virtual double UpdateGeometry()
    {
        SliverConstraints constraints = ConstraintsForSliver;
        double stretchOffset = GeometryStretchOffset;
        double maxExtent = MaxExtent;
        double paintExtent = maxExtent - constraints.ScrollOffset;
        double clampedPaintExtent = Math.Clamp(paintExtent, 0.0, constraints.RemainingPaintExtent);
        Geometry = new SliverGeometry(
            ScrollExtent: maxExtent,
            PaintExtent: clampedPaintExtent,
            LayoutExtent: clampedPaintExtent,
            MaxPaintExtent: maxExtent + stretchOffset,
            CacheExtent: CalculateCacheOffset(constraints, from: 0.0, to: maxExtent),
            HasVisualOverflow: true,
            PaintOrigin: Math.Min(constraints.Overlap, 0.0));
        return stretchOffset > 0 ? 0.0 : Math.Min(0.0, paintExtent - ChildExtent);
    }

    public override double ChildMainAxisPosition(RenderObject child) => _childPosition ?? 0.0;
}

/// <summary>A sliver with a <see cref="RenderBox"/> child which is pinned to the leading edge.</summary>
public class RenderSliverPinnedPersistentHeader : RenderSliverPersistentHeader
{
    public RenderSliverPinnedPersistentHeader(
        double minExtent,
        double maxExtent,
        RenderBox? child = null,
        OverScrollHeaderStretchConfiguration? stretchConfiguration = null,
        PersistentHeaderShowOnScreenConfiguration? showOnScreenConfiguration = null)
        : base(minExtent, maxExtent, child, stretchConfiguration)
    {
        ShowOnScreenConfiguration = showOnScreenConfiguration ?? new PersistentHeaderShowOnScreenConfiguration();
    }

    /// <summary>
    /// Specifies how a pinned header is to trim the rectangle a reveal request asks the viewport for.
    /// </summary>
    public PersistentHeaderShowOnScreenConfiguration? ShowOnScreenConfiguration { get; set; }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        double maxExtent = MaxExtent;
        bool overlapsContent = constraints.Overlap > 0.0;
        LayoutChild(constraints.ScrollOffset, maxExtent, overlapsContent);
        double effectiveRemainingPaintExtent = Math.Max(0.0, constraints.RemainingPaintExtent - constraints.Overlap);
        double layoutExtent = Math.Clamp(
            maxExtent - constraints.ScrollOffset,
            0.0,
            effectiveRemainingPaintExtent);
        double stretchOffset = GeometryStretchOffset;
        Geometry = new SliverGeometry(
            ScrollExtent: maxExtent,
            PaintExtent: Math.Min(ChildExtent, effectiveRemainingPaintExtent),
            LayoutExtent: layoutExtent,
            MaxPaintExtent: maxExtent + stretchOffset,
            CacheExtent: layoutExtent > 0.0 ? -constraints.CacheOrigin + layoutExtent : layoutExtent,
            HasVisualOverflow: true,
            PaintOrigin: constraints.Overlap,
            MaxScrollObstructionExtent: MinExtent);
        UpdateChildPaintOffset();
    }

    public override double ChildMainAxisPosition(RenderObject child) => 0.0;

    /// <summary>
    /// A pinned header stays put, so a reveal request only has to ask the viewport for the part of
    /// the rectangle that is not already held at the leading edge.
    /// </summary>
    public override void ShowOnScreen(
        RenderObject? descendant = null,
        Rect? rect = null,
        TimeSpan duration = default,
        Curve? curve = null)
    {
        Rect? localBounds = descendant != null
            ? RenderObject.TransformRect(descendant.GetTransformTo(this), rect ?? descendant.PaintBounds)
            : rect;
        Rect? newRect = PersistentHeaderReveal.TrimForPinnedHeader(
            localBounds,
            PersistentHeaderReveal.EffectiveAxisDirection(ConstraintsForSliver),
            ChildExtent);
        base.ShowOnScreen(descendant: this, rect: newRect, duration: duration, curve: curve);
    }
}

/// <summary>
/// A sliver with a <see cref="RenderBox"/> child which shrinks and scrolls like a
/// <see cref="RenderSliverScrollingPersistentHeader"/>, but immediately comes back when the user
/// scrolls in the reverse direction.
/// </summary>
public class RenderSliverFloatingPersistentHeader : RenderSliverPersistentHeader
{
    private AnimationController? _controller;
    private Animation<double>? _animation;
    private double? _lastActualScrollOffset;
    private double? _effectiveScrollOffset;
    private ScrollDirection? _lastStartedScrollDirection;
    private double? _childPosition;
    private ITickerProvider? _vsync;

    public RenderSliverFloatingPersistentHeader(
        double minExtent,
        double maxExtent,
        RenderBox? child = null,
        ITickerProvider? vsync = null,
        FloatingHeaderSnapConfiguration? snapConfiguration = null,
        OverScrollHeaderStretchConfiguration? stretchConfiguration = null,
        PersistentHeaderShowOnScreenConfiguration? showOnScreenConfiguration = null)
        : base(minExtent, maxExtent, child, stretchConfiguration)
    {
        _vsync = vsync;
        SnapConfiguration = snapConfiguration;
        ShowOnScreenConfiguration = showOnScreenConfiguration;
    }

    /// <summary>Specifies how the header animates itself into or out of view, or null to disable snapping.</summary>
    public FloatingHeaderSnapConfiguration? SnapConfiguration { get; set; }

    /// <summary>How far a reveal request may expand this header, or null to defer to the viewport.</summary>
    public PersistentHeaderShowOnScreenConfiguration? ShowOnScreenConfiguration { get; set; }

    /// <summary>
    /// The ticker provider the snap animation runs on.
    /// </summary>
    /// <remarks>
    /// Plumix's <see cref="AnimationController"/> has no <c>resync</c>, so a new provider disposes the
    /// controller instead of re-hosting it; the controller is recreated lazily on the next snap.
    /// </remarks>
    public ITickerProvider? Vsync
    {
        get => _vsync;
        set
        {
            if (ReferenceEquals(_vsync, value))
            {
                return;
            }

            _vsync = value;
            DisposeController();
        }
    }

    /// <summary>The shrink offset the header is actually laid out with, which floating decouples from the scroll offset.</summary>
    public double? EffectiveScrollOffset => _effectiveScrollOffset;

    /// <summary>Records the direction of a scroll gesture as it starts.</summary>
    public void UpdateScrollStartDirection(ScrollDirection direction)
    {
        _lastStartedScrollDirection = direction;
    }

    /// <summary>Animates the header fully into or out of view when a scroll gesture ends.</summary>
    public void MaybeStartSnapAnimation(ScrollDirection direction)
    {
        if (SnapConfiguration is not { } snap)
        {
            return;
        }

        if (direction == ScrollDirection.Forward && _effectiveScrollOffset <= 0.0)
        {
            return;
        }

        if (direction == ScrollDirection.Reverse && _effectiveScrollOffset >= MaxExtent)
        {
            return;
        }

        UpdateAnimation(
            snap.Duration,
            direction == ScrollDirection.Forward ? 0.0 : MaxExtent,
            snap.Curve);
        _controller?.Forward(from: 0.0);
    }

    /// <summary>Stops an in-flight snap animation when a new scroll gesture starts.</summary>
    public void MaybeStopSnapAnimation(ScrollDirection direction)
    {
        _ = direction;
        _controller?.Stop();
    }

    protected override void OnDetach()
    {
        // The controller is lazily recreated if this render object is reattached.
        DisposeController();
        base.OnDetach();
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        double maxExtent = MaxExtent;
        if (_lastActualScrollOffset is { } lastActualScrollOffset
            && (constraints.ScrollOffset < lastActualScrollOffset || _effectiveScrollOffset < maxExtent))
        {
            double delta = lastActualScrollOffset - constraints.ScrollOffset;
            bool allowFloatingExpansion = constraints.UserScrollDirection == ScrollDirection.Forward
                                          || _lastStartedScrollDirection == ScrollDirection.Forward;
            if (allowFloatingExpansion)
            {
                if (_effectiveScrollOffset > maxExtent)
                {
                    // We're scrolled off-screen, but should reveal, so pretend we're just at the
                    // limit.
                    _effectiveScrollOffset = maxExtent;
                }
            }
            else
            {
                // We're not allowed to expand yet: pretend we did not scroll.
                if (delta > 0.0)
                {
                    delta = 0.0;
                }
            }

            _effectiveScrollOffset = Math.Clamp(
                _effectiveScrollOffset!.Value - delta,
                0.0,
                constraints.ScrollOffset);
        }
        else
        {
            _effectiveScrollOffset = constraints.ScrollOffset;
        }

        bool overlapsContent = _effectiveScrollOffset < constraints.ScrollOffset;
        LayoutChild(_effectiveScrollOffset!.Value, maxExtent, overlapsContent);
        _childPosition = UpdateGeometry();
        UpdateChildPaintOffset();
        _lastActualScrollOffset = constraints.ScrollOffset;
    }

    /// <summary>Updates <see cref="RenderSliver.Geometry"/> and returns the child's main axis position.</summary>
    protected virtual double UpdateGeometry()
    {
        SliverConstraints constraints = ConstraintsForSliver;
        double stretchOffset = GeometryStretchOffset;
        double maxExtent = MaxExtent;
        double paintExtent = maxExtent - _effectiveScrollOffset!.Value;
        double layoutExtent = maxExtent - constraints.ScrollOffset;
        Geometry = new SliverGeometry(
            ScrollExtent: maxExtent,
            PaintExtent: Math.Clamp(paintExtent, 0.0, constraints.RemainingPaintExtent),
            LayoutExtent: Math.Clamp(layoutExtent, 0.0, constraints.RemainingPaintExtent),
            MaxPaintExtent: maxExtent + stretchOffset,
            CacheExtent: Math.Clamp(layoutExtent, 0.0, constraints.RemainingPaintExtent),
            HasVisualOverflow: true,
            PaintOrigin: Math.Min(constraints.Overlap, 0.0));
        return stretchOffset > 0 ? 0.0 : Math.Min(0.0, paintExtent - ChildExtent);
    }

    public override double ChildMainAxisPosition(RenderObject child) => _childPosition ?? 0.0;

    /// <summary>
    /// A floating header expands itself to satisfy a reveal request rather than letting the viewport
    /// scroll it into view, as long as it was given a
    /// <see cref="PersistentHeaderShowOnScreenConfiguration"/>.
    /// </summary>
    public override void ShowOnScreen(
        RenderObject? descendant = null,
        Rect? rect = null,
        TimeSpan duration = default,
        Curve? curve = null)
    {
        if (ShowOnScreenConfiguration is not { } showOnScreen)
        {
            base.ShowOnScreen(descendant, rect, duration, curve);
            return;
        }

        // The reveal is computed in the child's coordinate space: when the header is scrolled above
        // the leading edge, the sliver's origin and the child's origin are not the same point.
        Rect? childBounds = descendant != null
            ? RenderObject.TransformRect(descendant.GetTransformTo(Child), rect ?? descendant.PaintBounds)
            : rect;

        double childExtent = ChildExtent;
        double targetExtent;
        Rect? targetRect;
        switch (PersistentHeaderReveal.EffectiveAxisDirection(ConstraintsForSliver))
        {
            case AxisDirection.Up:
                targetExtent = childExtent - (childBounds?.Top ?? 0.0);
                targetRect = PersistentHeaderReveal.Trim(childBounds, bottom: childExtent);
                break;
            case AxisDirection.Right:
                targetExtent = childBounds?.Right ?? childExtent;
                targetRect = PersistentHeaderReveal.Trim(childBounds, left: 0.0);
                break;
            case AxisDirection.Left:
                targetExtent = childExtent - (childBounds?.Left ?? 0.0);
                targetRect = PersistentHeaderReveal.Trim(childBounds, right: childExtent);
                break;
            default:
                targetExtent = childBounds?.Bottom ?? childExtent;
                targetRect = PersistentHeaderReveal.Trim(childBounds, top: 0.0);
                break;
        }

        // A stretch header can have a bigger childExtent than maxExtent.
        double effectiveMaxExtent = Math.Max(childExtent, MaxExtent);
        targetExtent = Math.Clamp(
            Math.Clamp(targetExtent, showOnScreen.MinShowOnScreenExtent, showOnScreen.MaxShowOnScreenExtent),
            childExtent,
            effectiveMaxExtent);

        // Expand the header, with animation. Contracting is not allowed.
        if (targetExtent > childExtent && _controller?.Status != AnimationStatus.Forward)
        {
            UpdateAnimation(duration, MaxExtent - targetExtent, curve ?? Curves.Ease);
            _controller?.Forward(from: 0.0);
        }

        base.ShowOnScreen(
            descendant: descendant == null ? this : Child,
            rect: targetRect,
            duration: duration,
            curve: curve);
    }

    private void UpdateAnimation(TimeSpan duration, double endValue, Curve curve)
    {
        if (_vsync == null)
        {
            throw new InvalidOperationException(
                "vsync must not be null if the floating header changes size animatedly.");
        }

        if (_controller == null)
        {
            _controller = AnimationController.Unbounded(
                value: _effectiveScrollOffset ?? 0.0,
                vsync: _vsync,
                duration: duration <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : duration);
            _controller.AddListener(HandleAnimationTick);
        }

        _animation = _controller.Drive(
            new DoubleTween(begin: _effectiveScrollOffset ?? 0.0, end: endValue)
                .Chain(new CurveTween(curve)));
    }

    private void HandleAnimationTick()
    {
        if (_animation is not { } animation || _effectiveScrollOffset == animation.Value)
        {
            return;
        }

        _effectiveScrollOffset = animation.Value;
        MarkNeedsLayout();
    }

    private void DisposeController()
    {
        if (_controller == null)
        {
            return;
        }

        _controller.RemoveListener(HandleAnimationTick);
        _controller.Dispose();
        _controller = null;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("effective scroll offset", _effectiveScrollOffset));
    }
}

/// <summary>
/// A sliver with a <see cref="RenderBox"/> child which shrinks and then remains pinned to the
/// leading edge, and which immediately grows back when the user scrolls in the reverse direction.
/// </summary>
public class RenderSliverFloatingPinnedPersistentHeader : RenderSliverFloatingPersistentHeader
{
    public RenderSliverFloatingPinnedPersistentHeader(
        double minExtent,
        double maxExtent,
        RenderBox? child = null,
        ITickerProvider? vsync = null,
        FloatingHeaderSnapConfiguration? snapConfiguration = null,
        OverScrollHeaderStretchConfiguration? stretchConfiguration = null,
        PersistentHeaderShowOnScreenConfiguration? showOnScreenConfiguration = null)
        : base(
            minExtent,
            maxExtent,
            child,
            vsync,
            snapConfiguration,
            stretchConfiguration,
            showOnScreenConfiguration)
    {
    }

    protected override double UpdateGeometry()
    {
        SliverConstraints constraints = ConstraintsForSliver;
        double minExtent = MinExtent;
        double minAllowedExtent = constraints.RemainingPaintExtent > minExtent
            ? minExtent
            : constraints.RemainingPaintExtent;
        double maxExtent = MaxExtent;
        double paintExtent = maxExtent - EffectiveScrollOffset!.Value;
        double clampedPaintExtent = Math.Clamp(paintExtent, minAllowedExtent, constraints.RemainingPaintExtent);
        double layoutExtent = maxExtent - constraints.ScrollOffset;
        double stretchOffset = GeometryStretchOffset;
        Geometry = new SliverGeometry(
            ScrollExtent: maxExtent,
            PaintExtent: clampedPaintExtent,
            LayoutExtent: Math.Clamp(layoutExtent, 0.0, clampedPaintExtent),
            MaxPaintExtent: maxExtent + stretchOffset,
            CacheExtent: Math.Clamp(layoutExtent, 0.0, clampedPaintExtent),
            HasVisualOverflow: true,
            PaintOrigin: Math.Min(constraints.Overlap, 0.0),
            MaxScrollObstructionExtent: minExtent);
        return 0.0;
    }
}
