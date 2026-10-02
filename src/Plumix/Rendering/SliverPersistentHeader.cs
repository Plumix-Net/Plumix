using Avalonia;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/sliver_persistent_header.dart

namespace Plumix.Rendering;

/// <summary>
/// Specifies how a stretched header is to trigger an <c>AsyncCallback</c>.
/// </summary>
public class OverScrollHeaderStretchConfiguration
{
    /// <summary>
    /// Creates an object that specifies how a stretched header may activate an <c>AsyncCallback</c>.
    /// </summary>
    public OverScrollHeaderStretchConfiguration(
        double stretchTriggerOffset = 100.0,
        Func<Task>? onStretchTrigger = null)
    {
        StretchTriggerOffset = stretchTriggerOffset;
        OnStretchTrigger = onStretchTrigger;
    }

    /// <summary>The offset of overscroll required to trigger the <see cref="OnStretchTrigger"/>.</summary>
    public double StretchTriggerOffset { get; }

    /// <summary>
    /// The callback function to be executed when a user over-scrolls to the offset specified by
    /// <see cref="StretchTriggerOffset"/>.
    /// </summary>
    public Func<Task>? OnStretchTrigger { get; }
}

/// <summary>
/// <see cref="RenderObject.ShowOnScreen"/> configuration for a floating or pinned persistent header.
/// </summary>
/// <remarks>
/// When a <c>showOnScreen</c> request targets a floating header, the header expands (and the
/// enclosing viewport scrolls) so its extent falls in
/// [<see cref="MinShowOnScreenExtent"/>, <see cref="MaxShowOnScreenExtent"/>].
/// </remarks>
public sealed class PersistentHeaderShowOnScreenConfiguration
{
    /// <summary>
    /// Creates an object that specifies how a pinned or floating persistent header should behave in response to
    /// <c>showOnScreen</c> requests.
    /// </summary>
    public PersistentHeaderShowOnScreenConfiguration(
        double minShowOnScreenExtent = double.NegativeInfinity,
        double maxShowOnScreenExtent = double.PositiveInfinity)
    {
        DebugAssertions.Assert(minShowOnScreenExtent <= maxShowOnScreenExtent);
        MinShowOnScreenExtent = minShowOnScreenExtent;
        MaxShowOnScreenExtent = maxShowOnScreenExtent;
    }

    /// <summary>
    /// The smallest the floating header can expand to in the main axis direction, in response to a
    /// <c>showOnScreen</c> request, regardless of the persistent header's current extent.
    /// </summary>
    public double MinShowOnScreenExtent { get; }

    /// <summary>
    /// The maximum extent above which a floating persistent header will not expand to in response to
    /// a <c>showOnScreen</c> request.
    /// </summary>
    public double MaxShowOnScreenExtent { get; }
}

/// <summary>
/// Specifies how a floating header is to be "snapped" (animated) into or out of view.
/// </summary>
public class FloatingHeaderSnapConfiguration
{
    /// <summary>
    /// Creates an object that specifies how a floating header is to be "snapped" (animated) into or out of view.
    /// </summary>
    public FloatingHeaderSnapConfiguration(Curve? curve = null, TimeSpan? duration = null)
    {
        Curve = curve ?? Curves.Ease;
        Duration = duration ?? TimeSpan.FromMilliseconds(300);
    }

    /// <summary>The snap animation curve.</summary>
    public Curve Curve { get; }

    /// <summary>The snap animation's duration.</summary>
    public TimeSpan Duration { get; }
}

/// <summary>
/// A base class for slivers that have a <see cref="RenderBox"/> child which scrolls normally, except
/// that when it hits the leading edge (typically the top) of the viewport, it shrinks to a minimum
/// size (<see cref="MinExtent"/>).
/// </summary>
/// <remarks>
/// <para>This class primarily provides helpers for managing the child, in particular:</para>
/// <list type="bullet">
/// <item><see cref="LayoutChild"/>, which applies min and max extents and a scroll offset to lay out
/// the child. This is normally called from <see cref="RenderObject.PerformLayout"/>.</item>
/// <item><see cref="ChildExtent"/>, to convert the child's box layout dimensions to the sliver
/// geometry model.</item>
/// <item>Hit testing, painting, and other details of the sliver protocol.</item>
/// </list>
/// <para>
/// Subclasses must implement <see cref="RenderObject.PerformLayout"/>, <see cref="MinExtent"/>, and
/// <see cref="MaxExtent"/>, and typically also will implement <see cref="UpdateChild"/>.
/// </para>
/// <para>
/// Dart mixes in <c>RenderObjectWithChildMixin&lt;RenderBox&gt;</c> and <c>RenderSliverHelpers</c>; C#
/// has no mixins, so the child slot is spelled out here and the helpers are the static
/// <see cref="RenderSliverHelpers"/>.
/// </para>
/// </remarks>
public abstract class RenderSliverPersistentHeader : RenderSliver, IRenderObjectSingleChildContainer
{
    private RenderBox? _child;
    private double? _lastStretchOffset;
    private bool _needsUpdateChild = true;
    private double _lastShrinkOffset;
    private bool _lastOverlapsContent;

    /// <summary>Creates a sliver that changes its size when scrolled to the start of the viewport.</summary>
    protected RenderSliverPersistentHeader(
        RenderBox? child = null,
        OverScrollHeaderStretchConfiguration? stretchConfiguration = null)
    {
        StretchConfiguration = stretchConfiguration;
        Child = child;
    }

    /// <summary>Dart's <c>RenderObjectWithChildMixin.child</c>.</summary>
    public RenderBox? Child
    {
        get => _child;
        set
        {
            if (_child != null)
            {
                DropChild(_child);
            }

            _child = value;
            if (_child != null)
            {
                AdoptChild(_child);
            }
        }
    }

    RenderObject? IRenderObjectSingleChildContainer.Child
    {
        get => Child;
        set
        {
            DebugAssertions.Assert(value is null || DebugValidateChildType<RenderBox>(this, value));
            Child = (RenderBox?)value;
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    /// <summary>The biggest size that the child box is allowed to take.</summary>
    /// <remarks>
    /// The <see cref="LayoutChild"/> method uses this to determine the box constraints passed to the
    /// child. It is the largest the child can be in the main axis, before any overscroll stretch.
    /// </remarks>
    public abstract double MaxExtent { get; }

    /// <summary>The smallest size that the child box is allowed to take.</summary>
    /// <remarks>
    /// The <see cref="LayoutChild"/> method uses this to determine the box constraints passed to the
    /// child.
    /// </remarks>
    public abstract double MinExtent { get; }

    /// <summary>The dimension of the child in the main axis.</summary>
    protected double ChildExtent
    {
        get
        {
            if (Child == null)
            {
                return 0.0;
            }

            DebugAssertions.Assert(Child.HasSize);
            return Constraints.Axis switch
            {
                Axis.Vertical => Child.Size.Height,
                _ => Child.Size.Width,
            };
        }
    }

    /// <summary>The last value that <see cref="UpdateChild"/> was called with as its shrink offset.</summary>
    public double LastShrinkOffset => _lastShrinkOffset;

    /// <summary>The last value that <see cref="UpdateChild"/> was called with as its overlaps-content flag.</summary>
    public bool LastOverlapsContent => _lastOverlapsContent;

    /// <summary>
    /// Defines the parameters used to execute an <c>AsyncCallback</c> when a stretching header over-scrolls.
    /// </summary>
    /// <remarks>If this is null then the stretch trigger is not invoked.</remarks>
    public OverScrollHeaderStretchConfiguration? StretchConfiguration { get; set; }

    /// <summary>Update the child render object if necessary.</summary>
    /// <remarks>
    /// Called before the first layout, any time <see cref="MarkNeedsLayout"/> is called, and any time
    /// the scroll offset changes. The <paramref name="shrinkOffset"/> is the difference between the
    /// <see cref="MaxExtent"/> and the current size. Zero means the header is fully expanded, any
    /// greater number up to <see cref="MaxExtent"/> means that the header has been scrolled by that
    /// much. The <paramref name="overlapsContent"/> argument is true if the sliver's leading edge is
    /// beyond its normal place in the viewport contents, and false otherwise. It may still paint
    /// beyond its normal place if the <see cref="MinExtent"/> after this call is greater than the
    /// amount of space that would normally be left.
    /// </remarks>
    protected virtual void UpdateChild(double shrinkOffset, bool overlapsContent)
    {
    }

    public override void MarkNeedsLayout()
    {
        // This is automatically called whenever the child's intrinsic dimensions
        // change, at which point we should remeasure them during the next layout.
        _needsUpdateChild = true;
        base.MarkNeedsLayout();
    }

    /// <summary>Lays out the <see cref="Child"/>.</summary>
    /// <remarks>
    /// This is called by subclasses' <see cref="RenderObject.PerformLayout"/>. It should be passed
    /// the scroll offset, which is the distance from where this sliver would be if it were laid out
    /// normally to where it actually is (which is the leading edge of the viewport, when the header
    /// is pinned). The <paramref name="maxExtent"/> argument is the maximum extent the header can
    /// have before stretching; it is usually <see cref="MaxExtent"/>, but subclasses may pass a
    /// different value.
    /// </remarks>
    protected void LayoutChild(double scrollOffset, double maxExtent, bool overlapsContent = false)
    {
        double shrinkOffset = Math.Min(scrollOffset, maxExtent);
        if (_needsUpdateChild || _lastShrinkOffset != shrinkOffset || _lastOverlapsContent != overlapsContent)
        {
            InvokeLayoutCallback<SliverConstraints>(
                constraints =>
                {
                    DebugAssertions.Assert(constraints == Constraints);
                    UpdateChild(shrinkOffset, overlapsContent);
                },
                Constraints);
            _lastShrinkOffset = shrinkOffset;
            _lastOverlapsContent = overlapsContent;
            _needsUpdateChild = false;
        }

        if (Constants.KDebugMode && !(MinExtent <= maxExtent))
        {
            throw new FlutterError([
                new ErrorSummary($"The maxExtent for this {GetType().Name} is less than its minExtent."),
                new DoubleProperty("The specified maxExtent was", maxExtent),
                new DoubleProperty("The specified minExtent was", MinExtent),
            ]);
        }

        double stretchOffset = 0.0;
        if (StretchConfiguration != null && Constraints.ScrollOffset == 0.0)
        {
            stretchOffset += Math.Abs(Constraints.Overlap);
        }

        Child?.Layout(
            Constraints.AsBoxConstraints(maxExtent: Math.Max(MinExtent, maxExtent - shrinkOffset) + stretchOffset),
            parentUsesSize: true);

        // Dart's `_lastStretchOffset` is `late`: reading it before the first layout wrote it throws.
        if (StretchConfiguration != null
            && StretchConfiguration.OnStretchTrigger != null
            && stretchOffset >= StretchConfiguration.StretchTriggerOffset
            && _lastStretchOffset!.Value <= StretchConfiguration.StretchTriggerOffset)
        {
            _ = StretchConfiguration.OnStretchTrigger!();
        }

        _lastStretchOffset = stretchOffset;
    }

    /// <summary>
    /// Returns the distance from the leading <i>visible</i> edge of the sliver to the side of the
    /// child closest to that edge, in the scroll axis direction.
    /// </summary>
    /// <remarks>
    /// For example, if the <see cref="SliverConstraints.AxisDirection"/> is
    /// <see cref="AxisDirection.Down"/>, then this is the distance from the top of the visible portion
    /// of the sliver to the top of the child. If the child is scrolled partially off the top of the
    /// viewport, then this will be negative. On the other hand, if the child is in the middle of the
    /// viewport, then this will be zero; if it is aligned with the bottom edge it will be the extent
    /// of the sliver minus the extent of the child, and so forth.
    /// </remarks>
    public override double ChildMainAxisPosition(RenderObject child) => base.ChildMainAxisPosition(child);

    protected override bool HitTestChildren(
        SliverHitTestResult result,
        double mainAxisPosition,
        double crossAxisPosition)
    {
        DebugAssertions.Assert(Geometry!.HitTestExtent > 0.0);
        if (Child != null)
        {
            return this.HitTestBoxChild(
                BoxHitTestResult.Wrap(result),
                Child,
                mainAxisPosition: mainAxisPosition,
                crossAxisPosition: crossAxisPosition);
        }

        return false;
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        DebugAssertions.Assert(ReferenceEquals(child, Child));
        this.ApplyPaintTransformForBoxChild((RenderBox)child, transform);
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child != null && Geometry!.Visible)
        {
            offset += ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
                Constraints.AxisDirection,
                Constraints.GrowthDirection) switch
            {
                AxisDirection.Up => new Point(
                    0.0,
                    Geometry.PaintExtent - ChildMainAxisPosition(Child) - ChildExtent),
                AxisDirection.Left => new Point(
                    Geometry.PaintExtent - ChildMainAxisPosition(Child) - ChildExtent,
                    0.0),
                AxisDirection.Right => new Point(ChildMainAxisPosition(Child), 0.0),
                _ => new Point(0.0, ChildMainAxisPosition(Child)),
            };
            context.PaintChild(Child, offset);
        }
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.AddTagForChildren(RenderViewport.ExcludeFromScrolling);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(DoubleProperty.Lazy("maxExtent", () => MaxExtent));
        properties.Add(DoubleProperty.Lazy("child position", () => ChildMainAxisPosition(Child!)));
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => DebugDescribeSingleChild(Child);

    /// <summary>
    /// Dart's private <c>_trim</c>: <c>original?.intersect(Rect.fromLTRB(left, top, right, bottom))</c>.
    /// </summary>
    internal static Rect? Trim(
        Rect? original,
        double top = double.NegativeInfinity,
        double right = double.PositiveInfinity,
        double bottom = double.PositiveInfinity,
        double left = double.NegativeInfinity)
    {
        if (original is not { } rect)
        {
            return null;
        }

        // Dart's `Rect.intersect` keeps a negative extent where Avalonia's returns an empty rect.
        double newLeft = Math.Max(rect.Left, left);
        double newTop = Math.Max(rect.Top, top);
        double newRight = Math.Min(rect.Right, right);
        double newBottom = Math.Min(rect.Bottom, bottom);
        return new Rect(newLeft, newTop, newRight - newLeft, newBottom - newTop);
    }
}

/// <summary>
/// A sliver with a <see cref="RenderBox"/> child which scrolls normally, except that when it hits the leading edge
/// (typically the top) of the viewport, it shrinks to a minimum size before continuing to scroll.
/// </summary>
/// <remarks>
/// This sliver makes no effort to avoid overlapping other content.
/// </remarks>
public abstract class RenderSliverScrollingPersistentHeader : RenderSliverPersistentHeader
{
    private double? _childPosition;

    /// <summary>Creates a sliver that shrinks when it hits the start of the viewport, then scrolls off.</summary>
    protected RenderSliverScrollingPersistentHeader(
        RenderBox? child = null,
        OverScrollHeaderStretchConfiguration? stretchConfiguration = null)
        : base(child, stretchConfiguration)
    {
    }

    /// <summary>
    /// Updates <see cref="RenderSliver.Geometry"/>, and returns the new value for <see cref="ChildMainAxisPosition"/>.
    /// </summary>
    /// <remarks>This is used by <see cref="RenderObject.PerformLayout"/>.</remarks>
    protected double UpdateGeometry()
    {
        double stretchOffset = 0.0;
        if (StretchConfiguration != null)
        {
            stretchOffset += Math.Abs(Constraints.Overlap);
        }

        double maxExtent = MaxExtent;
        double paintExtent = maxExtent - Constraints.ScrollOffset;
        double cacheExtent = CalculateCacheOffset(Constraints, from: 0.0, to: maxExtent);

        Geometry = new SliverGeometry(
            CacheExtent: cacheExtent,
            ScrollExtent: maxExtent,
            PaintOrigin: Math.Min(Constraints.Overlap, 0.0),
            PaintExtent: Math.Clamp(paintExtent, 0.0, Constraints.RemainingPaintExtent),
            MaxPaintExtent: maxExtent + stretchOffset,
            // Conservatively say we do have overflow to avoid complexity.
            HasVisualOverflow: true);
        return stretchOffset > 0 ? 0.0 : Math.Min(0.0, paintExtent - ChildExtent);
    }

    protected override void PerformLayout()
    {
        SliverConstraints constraints = Constraints;
        double maxExtent = MaxExtent;
        LayoutChild(constraints.ScrollOffset, maxExtent);
        _childPosition = UpdateGeometry();
    }

    public override double ChildMainAxisPosition(RenderObject child)
    {
        DebugAssertions.Assert(ReferenceEquals(child, Child));
        DebugAssertions.Assert(_childPosition != null);
        return _childPosition!.Value;
    }
}

/// <summary>
/// A sliver with a <see cref="RenderBox"/> child which never scrolls off the viewport in the positive scroll direction,
/// and which first scrolls on at a full size but then shrinks as the viewport continues to scroll.
/// </summary>
/// <remarks>
/// This sliver avoids overlapping other earlier slivers where possible.
/// </remarks>
public abstract class RenderSliverPinnedPersistentHeader : RenderSliverPersistentHeader
{
    /// <summary>
    /// Creates a sliver that shrinks when it hits the start of the viewport, then stays pinned there.
    /// </summary>
    protected RenderSliverPinnedPersistentHeader(
        RenderBox? child = null,
        OverScrollHeaderStretchConfiguration? stretchConfiguration = null,
        PersistentHeaderShowOnScreenConfiguration? showOnScreenConfiguration = null)
        : base(child, stretchConfiguration)
    {
        ShowOnScreenConfiguration = showOnScreenConfiguration ?? new PersistentHeaderShowOnScreenConfiguration();
    }

    /// <summary>Specifies the persistent header's behavior when <c>showOnScreen</c> is called.</summary>
    /// <remarks>If set to null, the persistent header will delegate the <c>showOnScreen</c> call to its
    /// parent.</remarks>
    public PersistentHeaderShowOnScreenConfiguration? ShowOnScreenConfiguration { get; set; }

    protected override void PerformLayout()
    {
        SliverConstraints constraints = Constraints;
        double maxExtent = MaxExtent;
        bool overlapsContent = constraints.Overlap > 0.0;
        LayoutChild(constraints.ScrollOffset, maxExtent, overlapsContent: overlapsContent);
        double effectiveRemainingPaintExtent = Math.Max(0, constraints.RemainingPaintExtent - constraints.Overlap);
        double layoutExtent = Math.Clamp(
            maxExtent - constraints.ScrollOffset,
            0.0,
            effectiveRemainingPaintExtent);
        double stretchOffset = StretchConfiguration != null ? Math.Abs(constraints.Overlap) : 0.0;
        Geometry = new SliverGeometry(
            ScrollExtent: maxExtent,
            PaintOrigin: constraints.Overlap,
            PaintExtent: Math.Min(ChildExtent, effectiveRemainingPaintExtent),
            LayoutExtent: layoutExtent,
            MaxPaintExtent: maxExtent + stretchOffset,
            MaxScrollObstructionExtent: MinExtent,
            CacheExtent: layoutExtent > 0.0 ? -constraints.CacheOrigin + layoutExtent : layoutExtent,
            // Conservatively say we do have overflow to avoid complexity.
            HasVisualOverflow: true);
    }

    public override double ChildMainAxisPosition(RenderObject child) => 0.0;

    public override void ShowOnScreen(
        RenderObject? descendant = null,
        Rect? rect = null,
        TimeSpan duration = default,
        Curve? curve = null)
    {
        Rect? localBounds = descendant != null
            ? MatrixUtils.TransformRect(descendant.GetTransformTo(this), rect ?? descendant.PaintBounds)
            : rect;

        Rect? newRect = ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
            Constraints.AxisDirection,
            Constraints.GrowthDirection) switch
        {
            AxisDirection.Up => Trim(localBounds, bottom: ChildExtent),
            AxisDirection.Left => Trim(localBounds, right: ChildExtent),
            AxisDirection.Right => Trim(localBounds, left: 0),
            _ => Trim(localBounds, top: 0),
        };

        base.ShowOnScreen(descendant: this, rect: newRect, duration: duration, curve: curve ?? Curves.Ease);
    }
}

/// <summary>
/// A sliver with a <see cref="RenderBox"/> child which shrinks and scrolls like a <see
/// cref="RenderSliverScrollingPersistentHeader"/>, but immediately comes back when the user scrolls in the reverse
/// direction.
/// </summary>
public abstract class RenderSliverFloatingPersistentHeader : RenderSliverPersistentHeader
{
    private AnimationController? _controller;
    private Animation<double>? _animation;
    private double? _lastActualScrollOffset;
    private double? _effectiveScrollOffset;

    // Important for pointer scrolling, which does not have the same concept of a hold and release
    // scroll movement, like dragging. This keeps track of the last ScrollDirection when scrolling
    // started.
    private ScrollDirection? _lastStartedScrollDirection;

    // Distance from our leading edge to the child's leading edge, in the axis direction. Negative if
    // we're scrolled off the top.
    private double? _childPosition;

    private ITickerProvider? _vsync;

    /// <summary>
    /// Creates a sliver that shrinks when it hits the start of the viewport, then scrolls off, and comes back
    /// immediately when the user reverses the scroll direction.
    /// </summary>
    protected RenderSliverFloatingPersistentHeader(
        PersistentHeaderShowOnScreenConfiguration? showOnScreenConfiguration,
        RenderBox? child = null,
        ITickerProvider? vsync = null,
        FloatingHeaderSnapConfiguration? snapConfiguration = null,
        OverScrollHeaderStretchConfiguration? stretchConfiguration = null)
        : base(child, stretchConfiguration)
    {
        _vsync = vsync;
        SnapConfiguration = snapConfiguration;
        ShowOnScreenConfiguration = showOnScreenConfiguration;
    }

    protected override void OnDetach()
    {
        _controller?.Dispose();
        _controller = null; // lazily recreated if we're reattached.
        base.OnDetach();
    }

    /// <summary>
    /// The shrink offset the header is laid out with, which floating decouples from the scroll
    /// offset. Dart's library-private <c>_effectiveScrollOffset</c>, which the floating-pinned
    /// subclass reads.
    /// </summary>
    internal double? EffectiveScrollOffset => _effectiveScrollOffset;

    /// <summary>
    /// A <see cref="ITickerProvider"/> to use to vend <see cref="Ticker"/> objects, used for the snap
    /// and show-on-screen animations.
    /// </summary>
    public ITickerProvider? Vsync
    {
        get => _vsync;
        set
        {
            if (ReferenceEquals(value, _vsync))
            {
                return;
            }

            _vsync = value;
            if (value == null)
            {
                _controller?.Dispose();
                _controller = null;
            }
            else
            {
                _controller?.Resync(value);
            }
        }
    }

    /// <summary>Defines the parameters used to snap (animate) the floating header in and out of view.</summary>
    /// <remarks>If this is null, then the floating header does not snap.</remarks>
    public FloatingHeaderSnapConfiguration? SnapConfiguration { get; set; }

    /// <summary>Specifies the persistent header's behavior when <c>showOnScreen</c> is called.</summary>
    /// <remarks>If set to null, the persistent header will delegate the <c>showOnScreen</c> call to its
    /// parent.</remarks>
    public PersistentHeaderShowOnScreenConfiguration? ShowOnScreenConfiguration { get; set; }

    /// <summary>
    /// Updates <see cref="RenderSliver.Geometry"/>, and returns the new value for <see cref="ChildMainAxisPosition"/>.
    /// </summary>
    /// <remarks>This is used by <see cref="RenderObject.PerformLayout"/>.</remarks>
    protected virtual double UpdateGeometry()
    {
        double stretchOffset = 0.0;
        if (StretchConfiguration != null)
        {
            stretchOffset += Math.Abs(Constraints.Overlap);
        }

        double maxExtent = MaxExtent;
        double paintExtent = maxExtent - _effectiveScrollOffset!.Value;
        double layoutExtent = maxExtent - Constraints.ScrollOffset;
        Geometry = new SliverGeometry(
            ScrollExtent: maxExtent,
            PaintOrigin: Math.Min(Constraints.Overlap, 0.0),
            PaintExtent: Math.Clamp(paintExtent, 0.0, Constraints.RemainingPaintExtent),
            LayoutExtent: Math.Clamp(layoutExtent, 0.0, Constraints.RemainingPaintExtent),
            MaxPaintExtent: maxExtent + stretchOffset,
            // Conservatively say we do have overflow to avoid complexity.
            HasVisualOverflow: true);
        return stretchOffset > 0 ? 0.0 : Math.Min(0.0, paintExtent - ChildExtent);
    }

    private void UpdateAnimation(TimeSpan duration, double endValue, Curve curve)
    {
        DebugAssertions.Assert(
            Vsync != null,
            "vsync must not be null if the floating header changes size animatedly.");

        if (_controller == null)
        {
            _controller = new AnimationController(vsync: Vsync!, duration: duration);
            _controller.AddListener(() =>
            {
                if (_effectiveScrollOffset == _animation!.Value)
                {
                    return;
                }

                _effectiveScrollOffset = _animation.Value;
                MarkNeedsLayout();
            });
        }

        AnimationController effectiveController = _controller;
        _animation = effectiveController.Drive(
            new DoubleTween(begin: _effectiveScrollOffset, end: endValue).Chain(new CurveTween(curve: curve)));
    }

    /// <summary>
    /// Update the last known <see cref="ScrollDirection"/> when scrolling began.
    /// </summary>
    public void UpdateScrollStartDirection(ScrollDirection direction)
    {
        _lastStartedScrollDirection = direction;
    }

    /// <summary>
    /// If the header isn't already fully exposed, then scroll it into view.
    /// </summary>
    public void MaybeStartSnapAnimation(ScrollDirection direction)
    {
        FloatingHeaderSnapConfiguration? snap = SnapConfiguration;
        if (snap == null)
        {
            return;
        }

        if (direction == ScrollDirection.Forward && _effectiveScrollOffset!.Value <= 0.0)
        {
            return;
        }

        if (direction == ScrollDirection.Reverse && _effectiveScrollOffset!.Value >= MaxExtent)
        {
            return;
        }

        UpdateAnimation(
            snap.Duration,
            direction == ScrollDirection.Forward ? 0.0 : MaxExtent,
            snap.Curve);
        _controller?.Forward(from: 0.0);
    }

    /// <summary>
    /// If a header snap animation or a <see cref="ShowOnScreen"/> expand animation is underway then
    /// stop it.
    /// </summary>
    public void MaybeStopSnapAnimation(ScrollDirection direction)
    {
        _controller?.Stop();
    }

    protected override void PerformLayout()
    {
        SliverConstraints constraints = Constraints;
        double maxExtent = MaxExtent;
        // We've laid out at least once to get an initial position, and either we are scrolling
        // back, so should reveal, or some part of it is visible, so should shrink or reveal as
        // appropriate.
        if (_lastActualScrollOffset != null
            && (constraints.ScrollOffset < _lastActualScrollOffset.Value
                || _effectiveScrollOffset!.Value < maxExtent))
        {
            double delta = _lastActualScrollOffset.Value - constraints.ScrollOffset;

            bool allowFloatingExpansion = constraints.UserScrollDirection == ScrollDirection.Forward
                                          || (_lastStartedScrollDirection != null
                                              && _lastStartedScrollDirection == ScrollDirection.Forward);
            if (allowFloatingExpansion)
            {
                if (_effectiveScrollOffset!.Value > maxExtent)
                {
                    // We're scrolled off-screen, but should reveal, so pretend we're just at the limit.
                    _effectiveScrollOffset = maxExtent;
                }
            }
            else
            {
                if (delta > 0.0)
                {
                    // Disallow the expansion. (But allow shrinking, i.e. delta < 0.0 is fine.)
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

        bool overlapsContent = _effectiveScrollOffset.Value < constraints.ScrollOffset;

        LayoutChild(_effectiveScrollOffset.Value, maxExtent, overlapsContent: overlapsContent);
        _childPosition = UpdateGeometry();
        _lastActualScrollOffset = constraints.ScrollOffset;
    }

    public override void ShowOnScreen(
        RenderObject? descendant = null,
        Rect? rect = null,
        TimeSpan duration = default,
        Curve? curve = null)
    {
        Curve effectiveCurve = curve ?? Curves.Ease;
        PersistentHeaderShowOnScreenConfiguration? showOnScreen = ShowOnScreenConfiguration;
        if (showOnScreen == null)
        {
            base.ShowOnScreen(descendant: descendant, rect: rect, duration: duration, curve: effectiveCurve);
            return;
        }

        DebugAssertions.Assert(Child != null || descendant == null);
        // We prefer the child's coordinate space (instead of the sliver's) because it's easier for
        // us to convert the target rect into target extents: when the sliver is sitting above the
        // leading edge and not being scrolled into view, the child's position on the viewport
        // overlaps with the sliver's, and the child's position doesn't change.
        Rect? childBounds = descendant != null
            ? MatrixUtils.TransformRect(descendant.GetTransformTo(Child), rect ?? descendant.PaintBounds)
            : rect;

        double targetExtent;
        Rect? targetRect;
        switch (ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
                    Constraints.AxisDirection,
                    Constraints.GrowthDirection))
        {
            case AxisDirection.Up:
                targetExtent = ChildExtent - (childBounds?.Top ?? 0);
                targetRect = Trim(childBounds, bottom: ChildExtent);
                break;
            case AxisDirection.Right:
                targetExtent = childBounds?.Right ?? ChildExtent;
                targetRect = Trim(childBounds, left: 0);
                break;
            case AxisDirection.Down:
                targetExtent = childBounds?.Bottom ?? ChildExtent;
                targetRect = Trim(childBounds, top: 0);
                break;
            default:
                targetExtent = ChildExtent - (childBounds?.Left ?? 0);
                targetRect = Trim(childBounds, right: ChildExtent);
                break;
        }

        // A stretch header can have a bigger childExtent than maxExtent.
        double effectiveMaxExtent = Math.Max(ChildExtent, MaxExtent);

        targetExtent = Math.Clamp(
            Math.Clamp(targetExtent, showOnScreen.MinShowOnScreenExtent, showOnScreen.MaxShowOnScreenExtent),
            // Clamp the value back to the valid range after applying additional constraints.
            // Contracting is not allowed.
            ChildExtent,
            effectiveMaxExtent);

        // Expands the header if needed, with animation.
        if (targetExtent > ChildExtent && _controller?.Status != AnimationStatus.Forward)
        {
            double targetScrollOffset = MaxExtent - targetExtent;
            DebugAssertions.Assert(
                Vsync != null,
                "vsync must not be null if the floating header changes size animatedly.");
            UpdateAnimation(duration, targetScrollOffset, effectiveCurve);
            _controller?.Forward(from: 0.0);
        }

        base.ShowOnScreen(
            descendant: descendant == null ? this : Child,
            rect: targetRect,
            duration: duration,
            curve: effectiveCurve);
    }

    public override double ChildMainAxisPosition(RenderObject child)
    {
        DebugAssertions.Assert(ReferenceEquals(child, Child));
        return _childPosition ?? 0.0;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("effective scroll offset", _effectiveScrollOffset));
    }
}

/// <summary>
/// A sliver with a <see cref="RenderBox"/> child which shrinks and then remains pinned to the start of the viewport
/// like a <see cref="RenderSliverPinnedPersistentHeader"/>, but immediately grows when the user scrolls in the reverse
/// direction.
/// </summary>
public abstract class RenderSliverFloatingPinnedPersistentHeader : RenderSliverFloatingPersistentHeader
{
    /// <summary>
    /// Creates a sliver that shrinks when it hits the start of the viewport, then stays pinned there, and grows
    /// immediately when the user reverses the scroll direction.
    /// </summary>
    protected RenderSliverFloatingPinnedPersistentHeader(
        RenderBox? child = null,
        ITickerProvider? vsync = null,
        FloatingHeaderSnapConfiguration? snapConfiguration = null,
        OverScrollHeaderStretchConfiguration? stretchConfiguration = null,
        PersistentHeaderShowOnScreenConfiguration? showOnScreenConfiguration = null)
        : base(
            showOnScreenConfiguration,
            child: child,
            vsync: vsync,
            snapConfiguration: snapConfiguration,
            stretchConfiguration: stretchConfiguration)
    {
    }

    protected override double UpdateGeometry()
    {
        double minExtent = MinExtent;
        double minAllowedExtent = Constraints.RemainingPaintExtent > minExtent
            ? minExtent
            : Constraints.RemainingPaintExtent;
        double maxExtent = MaxExtent;
        double paintExtent = maxExtent - EffectiveScrollOffset!.Value;
        double clampedPaintExtent = Math.Clamp(
            paintExtent,
            minAllowedExtent,
            Constraints.RemainingPaintExtent);
        double layoutExtent = maxExtent - Constraints.ScrollOffset;
        double stretchOffset = StretchConfiguration != null ? Math.Abs(Constraints.Overlap) : 0.0;
        Geometry = new SliverGeometry(
            ScrollExtent: maxExtent,
            PaintOrigin: Math.Min(Constraints.Overlap, 0.0),
            PaintExtent: clampedPaintExtent,
            LayoutExtent: Math.Clamp(layoutExtent, 0.0, clampedPaintExtent),
            MaxPaintExtent: maxExtent + stretchOffset,
            MaxScrollObstructionExtent: minExtent,
            // Conservatively say we do have overflow to avoid complexity.
            HasVisualOverflow: true);
        return 0.0;
    }
}
