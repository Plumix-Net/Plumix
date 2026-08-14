using Avalonia;
using Plumix;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Physics;
using Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/scroll_position.dart; flutter/packages/flutter/lib/src/widgets/scroll_physics.dart; flutter/packages/flutter/lib/src/widgets/scroll_activity.dart (adapted)

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_metrics.dart
public interface IScrollMetrics
{
    double Pixels { get; }
    double MinScrollExtent { get; }
    double MaxScrollExtent { get; }
    double ViewportDimension { get; }

    /// <summary>Whether the <see cref="Pixels"/> value is outside the <c>Min</c>/<c>Max</c> extents.</summary>
    bool OutOfRange => Pixels < MinScrollExtent || Pixels > MaxScrollExtent;

    /// <summary>The number of device pixels for each logical pixel of the view the scrollable is in.</summary>
    double DevicePixelRatio => 1.0;
}

/// <summary>An immutable snapshot of values associated with a <see cref="ScrollPosition"/>.</summary>
public sealed record FixedScrollMetrics(
    double Pixels,
    double MinScrollExtent,
    double MaxScrollExtent,
    double ViewportDimension,
    double DevicePixelRatio = 1.0) : IScrollMetrics
{
    /// <summary>Whether the <see cref="Pixels"/> value is outside the min/max extents.</summary>
    public bool OutOfRange => Pixels < MinScrollExtent || Pixels > MaxScrollExtent;

    public static FixedScrollMetrics From(IScrollMetrics metrics) => new(
        metrics.Pixels,
        metrics.MinScrollExtent,
        metrics.MaxScrollExtent,
        metrics.ViewportDimension,
        metrics.DevicePixelRatio);
}

public enum CacheExtentStyle
{
    Pixel,
    Viewport
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/viewport.dart
public sealed record ScrollCacheExtent
{
    private ScrollCacheExtent(double value, CacheExtentStyle style)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        Value = value;
        Style = style;
    }

    public double Value { get; }

    public CacheExtentStyle Style { get; }

    public static ScrollCacheExtent Pixels(double pixels) => new(pixels, CacheExtentStyle.Pixel);

    public static ScrollCacheExtent Viewport(double value) => new(value, CacheExtentStyle.Viewport);

    internal double CalculateCacheOffset(double mainAxisExtent)
    {
        return Style == CacheExtentStyle.Viewport ? Value * mainAxisExtent : Value;
    }
}

public enum AxisDirection
{
    Up,
    Right,
    Down,
    Left
}

public enum GrowthDirection
{
    Forward,
    Reverse
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/viewport_offset.dart
public enum ScrollDirection
{
    Idle,
    Forward,
    Reverse
}

public static class ScrollDirectionUtils
{
    public static Axis AxisDirectionToAxis(AxisDirection direction)
    {
        return direction switch
        {
            AxisDirection.Up => Axis.Vertical,
            AxisDirection.Down => Axis.Vertical,
            AxisDirection.Left => Axis.Horizontal,
            AxisDirection.Right => Axis.Horizontal,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    public static bool AxisDirectionIsReversed(AxisDirection direction)
    {
        return direction == AxisDirection.Up || direction == AxisDirection.Left;
    }

    public static AxisDirection DefaultAxisDirection(Axis axis)
    {
        return axis == Axis.Vertical ? AxisDirection.Down : AxisDirection.Right;
    }

    /// <remarks>Flutter's <c>applyGrowthDirectionToAxisDirection</c>.</remarks>
    public static AxisDirection ApplyGrowthDirectionToAxisDirection(
        AxisDirection axisDirection,
        GrowthDirection growthDirection)
    {
        if (growthDirection == GrowthDirection.Forward)
        {
            return axisDirection;
        }

        return axisDirection switch
        {
            AxisDirection.Up => AxisDirection.Down,
            AxisDirection.Right => AxisDirection.Left,
            AxisDirection.Down => AxisDirection.Up,
            AxisDirection.Left => AxisDirection.Right,
            _ => axisDirection,
        };
    }
}

public abstract class ScrollActivity : IDisposable
{
    protected ScrollActivity(ScrollPosition position)
    {
        Position = position;
    }

    protected ScrollPosition Position { get; private set; }

    /// <summary>
    /// Re-points this activity at the position that absorbed it, so an in-flight drag or ballistic
    /// run survives a scrollable replacing its <see cref="ScrollPosition"/>.
    /// </summary>
    public virtual void UpdateDelegate(ScrollPosition value)
    {
        Position = value;
    }

    /// <summary>Whether performing this activity constitutes scrolling.</summary>
    public virtual bool IsScrolling => true;

    /// <summary>The velocity at which the scroll offset is currently independently changing.</summary>
    public virtual double Velocity => 0.0;

    /// <summary>
    /// Called when the viewport or content dimensions change, so the activity can react to a
    /// position that the new dimensions may have put out of range.
    /// </summary>
    public virtual void ApplyNewDimensions()
    {
    }

    public virtual void Dispose()
    {
    }
}

public sealed class IdleScrollActivity(ScrollPosition position) : ScrollActivity(position)
{
    public override bool IsScrolling => false;

    public override void ApplyNewDimensions() => Position.GoBallistic(0.0);
}

/// <summary>
/// A scroll activity that holds the position still, cancelling any ballistic motion, while the
/// pointer that will drive the drag is still down.
/// </summary>
public sealed class HoldScrollActivity : ScrollActivity, IScrollHoldController
{
    private readonly Action? _onHoldCanceled;

    public HoldScrollActivity(ScrollPosition position, Action? onHoldCanceled = null) : base(position)
    {
        _onHoldCanceled = onHoldCanceled;
    }

    public override bool IsScrolling => false;

    public override double Velocity => 0.0;

    public void Cancel()
    {
        Position.GoBallistic(0.0);
    }

    public override void Dispose()
    {
        _onHoldCanceled?.Invoke();
        base.Dispose();
    }
}

/// <summary>The activity a scroll position runs while the user drags it.</summary>
public sealed class DragScrollActivity : ScrollActivity
{
    private ScrollDragController? _controller;

    public DragScrollActivity(ScrollPosition position, ScrollDragController? controller = null)
        : base(position)
    {
        _controller = controller;
    }

    public override double Velocity => 0.0;

    public override void Dispose()
    {
        _controller = null;
        base.Dispose();
    }
}

/// <summary>
/// Scrolls a scroll position from the deltas reported by a drag gesture recognizer.
/// </summary>
/// <remarks>
/// The controller owns the two pieces of drag behavior the physics parameterize: the momentum
/// carried over from a previous fling (<see cref="CarriedVelocity"/>, from
/// <see cref="ScrollPhysics.CarriedMomentum"/>) and the distance a resting finger must travel
/// before the position starts moving again (<see cref="MotionStartDistanceThreshold"/>, from
/// <see cref="ScrollPhysics.DragStartDistanceMotionThreshold"/>).
/// </remarks>
public sealed class ScrollDragController : IDrag, IDisposable
{
    /// <summary>How long a drag must be stationary before its carried momentum is dropped.</summary>
    public static readonly TimeSpan MomentumRetainStationaryDurationThreshold =
        TimeSpan.FromMilliseconds(20);

    /// <summary>How long a drag must be stationary before the motion-start threshold re-arms.</summary>
    public static readonly TimeSpan MotionStoppedDurationThreshold = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// The fraction of the carried velocity a new fling must reach to keep the carried momentum.
    /// </summary>
    public const double MomentumRetainVelocityThresholdFactor = 0.5;

    // A single update this large is a deliberate motion and passes the threshold unmodified.
    private const double BigThresholdBreakDistance = 24.0;

    private readonly Action? _onDragCanceled;
    private DateTime? _lastNonStationaryTimestampUtc;
    private double? _offsetSinceLastStop;
    private bool _retainMomentum;

    public ScrollDragController(
        ScrollPosition position,
        DragStartDetails details,
        Action? onDragCanceled = null,
        double? carriedVelocity = null,
        double? motionStartDistanceThreshold = null)
    {
        if (motionStartDistanceThreshold is <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(motionStartDistanceThreshold),
                "motionStartDistanceThreshold must be a positive number or null");
        }

        Position = position;
        CarriedVelocity = carriedVelocity;
        MotionStartDistanceThreshold = motionStartDistanceThreshold;
        _onDragCanceled = onDragCanceled;
        _retainMomentum = carriedVelocity is not null and not 0.0;
        _lastNonStationaryTimestampUtc = details.SourceTimeStampUtc;
        _offsetSinceLastStop = motionStartDistanceThreshold == null ? null : 0.0;
    }

    /// <summary>The position this drag scrolls.</summary>
    public ScrollPosition Position { get; private set; }

    /// <summary>
    /// Re-points this drag at the position that absorbed it (Flutter's
    /// <c>ScrollDragController.updatePosition</c>).
    /// </summary>
    public void UpdatePosition(ScrollPosition value)
    {
        Position = value;
    }

    /// <summary>Velocity carried over from a previous ballistic activity, if any.</summary>
    public double? CarriedVelocity { get; }

    /// <summary>The distance a resting drag must travel before the position starts moving.</summary>
    public double? MotionStartDistanceThreshold { get; }

    private bool Reversed => ScrollDirectionUtils.AxisDirectionIsReversed(Position.AxisDirection);

    /// <summary>
    /// Applies a drag update, returning the offset actually handed to the position (zero when the
    /// motion-start threshold swallowed it).
    /// </summary>
    public double Update(DragUpdateDetails details)
    {
        double offset = details.PrimaryDelta;
        if (offset != 0.0)
        {
            _lastNonStationaryTimestampUtc = details.SourceTimeStampUtc;
        }

        // By default, iOS platforms carries momentum and has a start threshold (configured in
        // BouncingScrollPhysics). The rest of the platforms does not.
        MaybeLoseMomentum(offset, details.SourceTimeStampUtc);
        offset = AdjustForScrollStartThreshold(offset, details.SourceTimeStampUtc);
        if (offset == 0.0)
        {
            return 0.0;
        }

        if (Reversed)
        {
            offset = -offset;
        }

        Position.ApplyUserOffset(offset);
        return offset;
    }

    void IDrag.Update(DragUpdateDetails details) => Update(details);

    public void End(DragEndDetails details)
    {
        // We negate the velocity here because if the touch is moving downwards, the scroll has to
        // move upwards. It's the same reason that update is negated.
        double velocity = -details.PrimaryVelocity;
        if (Reversed)
        {
            velocity = -velocity;
        }

        if (_retainMomentum)
        {
            // Build momentum only if the velocity of the last drag was in the same direction and if
            // the velocity is not substantially lower than the carried momentum.
            bool isFlingingInSameDirection = Math.Sign(velocity) == Math.Sign(CarriedVelocity!.Value);
            bool isVelocityNotSubstantiallyLessThanCarriedMomentum =
                Math.Abs(velocity)
                > Math.Abs(CarriedVelocity.Value) * MomentumRetainVelocityThresholdFactor;
            if (isFlingingInSameDirection && isVelocityNotSubstantiallyLessThanCarriedMomentum)
            {
                velocity += CarriedVelocity.Value;
            }
        }

        Position.GoBallistic(velocity);
    }

    public void Cancel()
    {
        Position.GoBallistic(0.0);
    }

    public void Dispose()
    {
        _onDragCanceled?.Invoke();
    }

    /// <summary>Drops the carried momentum once the drag has been stationary for long enough.</summary>
    private void MaybeLoseMomentum(double offset, DateTime? timestampUtc)
    {
        if (_retainMomentum
            && offset == 0.0
            && (timestampUtc == null || Elapsed(timestampUtc) > MomentumRetainStationaryDurationThreshold))
        {
            // If pointer is stationary for too long, we lose momentum.
            _retainMomentum = false;
        }
    }

    /// <summary>
    /// Swallows offsets until the drag has moved past the motion-start threshold, then releases a
    /// damped first offset so the position does not jump.
    /// </summary>
    private double AdjustForScrollStartThreshold(double offset, DateTime? timestampUtc)
    {
        if (timestampUtc == null)
        {
            // If we can't track time, we can't apply thresholds. Semantics-driven drags land here.
            return offset;
        }

        if (offset == 0.0)
        {
            if (MotionStartDistanceThreshold != null
                && _offsetSinceLastStop == null
                && Elapsed(timestampUtc) > MotionStoppedDurationThreshold)
            {
                // Enforce a new threshold.
                _offsetSinceLastStop = 0.0;
            }

            // Not moving can't break threshold.
            return 0.0;
        }

        if (_offsetSinceLastStop == null)
        {
            // Already in motion or no threshold behavior configured such as for desktop.
            return offset;
        }

        _offsetSinceLastStop += offset;
        if (Math.Abs(_offsetSinceLastStop.Value) > MotionStartDistanceThreshold!.Value)
        {
            // Threshold broken.
            _offsetSinceLastStop = null;
            if (Math.Abs(offset) > BigThresholdBreakDistance)
            {
                // This is heuristically a very deliberate fling. Leave the drag alone.
                return offset;
            }

            // This is a normal speed threshold break.
            return Math.Min(
                       // Ease into the motion when the threshold is initially broken to avoid a
                       // visible jump.
                       MotionStartDistanceThreshold.Value / 3.0,
                       Math.Abs(offset))
                   * Math.Sign(offset);
        }

        return 0.0;
    }

    private TimeSpan Elapsed(DateTime? timestampUtc)
    {
        if (timestampUtc == null || _lastNonStationaryTimestampUtc == null)
        {
            return TimeSpan.Zero;
        }

        return timestampUtc.Value - _lastNonStationaryTimestampUtc.Value;
    }
}

public sealed class PointerScrollActivity(ScrollPosition position) : ScrollActivity(position)
{
}

public sealed class DrivenScrollActivity : ScrollActivity
{
    private readonly Curve _curve;
    private readonly TimeSpan _duration;
    private readonly double _from;
    private readonly Ticker _ticker;
    private readonly double _to;
    private readonly TaskCompletionSource _completer =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TimeSpan _elapsed;
    private bool _disposed;

    public DrivenScrollActivity(
        ScrollPosition position,
        double to,
        TimeSpan duration,
        Curve curve) : base(position)
    {
        if (!double.IsFinite(to))
        {
            throw new ArgumentOutOfRangeException(nameof(to));
        }
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        _from = position.Pixels;
        _to = to;
        _duration = duration;
        _curve = curve ?? throw new ArgumentNullException(nameof(curve));
        _ticker = position.TickerProvider?.CreateTicker(OnTick) ?? new Ticker(OnTick);
        _ticker.Start();
    }

    /// <summary>
    /// Completes when the animation finishes or is superseded, so <see cref="ScrollPosition.AnimateTo"/>
    /// can hand back Flutter's <c>Future&lt;void&gt;</c>.
    /// </summary>
    public Task Done => _completer.Task;

    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ticker.Dispose();
        _completer.TrySetResult();
    }

    private void OnTick(TimeSpan elapsed)
    {
        if (_disposed)
        {
            return;
        }

        _elapsed += elapsed;
        double progress = Math.Clamp(_elapsed.TotalSeconds / _duration.TotalSeconds, 0.0, 1.0);
        double value = _from + ((_to - _from) * _curve(progress));
        Position.SetPixelsFromActivity(value);
        if (progress >= 1.0)
        {
            Position.GoIdle();
        }
    }
}

public sealed class BallisticScrollActivity : ScrollActivity
{
    private readonly Simulation _simulation;
    private readonly Ticker _ticker;
    private double _elapsedSeconds;
    private bool _disposed;

    public BallisticScrollActivity(ScrollPosition position, Simulation simulation) : base(position)
    {
        _simulation = simulation;
        _ticker = position.TickerProvider?.CreateTicker(OnTick) ?? new Ticker(OnTick);
        _ticker.Start();
    }

    public override double Velocity => _disposed ? 0.0 : _simulation.DX(_elapsedSeconds);

    public override void ApplyNewDimensions() => Position.GoBallistic(Velocity);

    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ticker.Dispose();
    }

    private void OnTick(TimeSpan elapsed)
    {
        if (_disposed)
        {
            return;
        }

        _elapsedSeconds += elapsed.TotalSeconds;

        // The simulation drives the position directly; the physics decide whether the proposed value
        // is reachable. A non-zero overscroll means the boundary conditions clipped the value, so the
        // simulation can no longer be followed and the activity ends.
        if (Math.Abs(Position.SetPixelsFromActivity(_simulation.X(_elapsedSeconds)))
            >= Constants.PrecisionErrorTolerance)
        {
            Position.GoIdle();
            return;
        }

        if (_simulation.IsDone(_elapsedSeconds))
        {
            // A completed ballistic run restarts ballistic with zero velocity, which springs the
            // position back when it is still out of range and goes idle otherwise.
            Position.GoBallistic(0.0);
        }
    }
}

/// <summary>
/// How <see cref="ScrollPosition.EnsureVisible"/> should treat the requested alignment.
/// </summary>
// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_position.dart
public enum ScrollPositionAlignmentPolicy
{
    /// <summary>Use the supplied alignment value as given.</summary>
    Explicit,

    /// <summary>
    /// Align to the trailing edge, but only when the target's trailing edge is past it; never scroll
    /// backwards.
    /// </summary>
    KeepVisibleAtEnd,

    /// <summary>
    /// Align to the leading edge, but only when the target's leading edge is before it; never scroll
    /// forwards.
    /// </summary>
    KeepVisibleAtStart,
}

public class ScrollPosition : ChangeNotifier, IScrollMetrics
{
    private readonly ScrollPhysics _physics;
    private double _pixels;
    private double _minScrollExtent;
    private double _maxScrollExtent;
    private double _viewportDimension;
    private bool _hasPixels;
    private bool _hasViewportDimension;
    private bool _hasContentDimensions;
    private bool _haveDimensions;
    private ScrollActivity _activity;
    private ScrollDirection _userScrollDirection = ScrollDirection.Idle;
    private FixedScrollMetrics? _lastMetrics;
    private bool _didChangeViewportDimensionOrReceiveCorrection = true;
    private ScrollDragController? _currentDrag;
    private double _heldPreviousVelocity;
    private double _impliedVelocity;

    public ScrollPosition(
        double initialPixels = 0.0,
        ScrollPhysics? physics = null,
        bool keepScrollOffset = true)
        : this((double?)initialPixels, physics, keepScrollOffset)
    {
    }

    /// <summary>
    /// Creates a position whose offset is not known yet. A null <paramref name="initialPixels"/>
    /// leaves <see cref="HasPixels"/> false until the first layout supplies a viewport dimension,
    /// which is what lets a subclass derive its offset from the viewport (Flutter's
    /// <c>ScrollPosition(initialPixels: null)</c>).
    /// </summary>
    protected ScrollPosition(
        double? initialPixels,
        ScrollPhysics? physics = null,
        bool keepScrollOffset = true)
    {
        _pixels = initialPixels ?? 0.0;
        _hasPixels = initialPixels.HasValue;
        KeepScrollOffset = keepScrollOffset;
        _physics = physics ?? new ClampingScrollPhysics();
        _activity = new IdleScrollActivity(this);
        IsScrollingNotifier = new ValueNotifier<bool>(false);
    }

    /// <summary>
    /// The current offset. Flutter asserts when the offset has not been established yet; Plumix
    /// reports zero and exposes <see cref="HasPixels"/> instead, because the offset is pushed into
    /// the viewport widget at build time rather than pulled during layout.
    /// </summary>
    public double Pixels => _pixels;

    /// <summary>Whether <see cref="Pixels"/> has been established by a layout or a correction.</summary>
    public bool HasPixels => _hasPixels;

    public double MinScrollExtent => _minScrollExtent;

    public double MaxScrollExtent => _maxScrollExtent;

    public double ViewportDimension => _viewportDimension;

    /// <summary>Whether <see cref="ViewportDimension"/> has been supplied by a layout.</summary>
    public bool HasViewportDimension => _hasViewportDimension;

    /// <summary>Whether the min/max scroll extents have been supplied by a layout.</summary>
    public bool HasContentDimensions => _hasContentDimensions;

    /// <summary>Whether <see cref="ApplyContentDimensions"/> has completed at least once.</summary>
    public bool HaveDimensions => _haveDimensions;

    /// <summary>Whether this position persists its offset through <see cref="PageStorage"/>.</summary>
    public bool KeepScrollOffset { get; }

    /// <summary>Whether the <see cref="Pixels"/> value is outside the min/max scroll extents.</summary>
    public bool OutOfRange => _pixels < _minScrollExtent || _pixels > _maxScrollExtent;

    public ScrollPhysics Physics => _physics;

    public ScrollActivity Activity => _activity;

    public AxisDirection AxisDirection { get; internal set; } = AxisDirection.Down;

    public double DevicePixelRatio { get; internal set; } = 1.0;

    public ValueNotifier<bool> IsScrollingNotifier { get; }

    internal ITickerProvider? TickerProvider { get; set; }

    /// <summary>
    /// The context a scroll position dispatches its notifications from. Flutter reaches this through
    /// <c>ScrollPosition.context.notificationContext</c>; Plumix has no separate `ScrollContext`, so the
    /// owning <see cref="Scrollable.ScrollableState"/> hands its own context to the position instead.
    /// </summary>
    public BuildContext? NotificationContext { get; internal set; }

    /// <summary>
    /// The key a <see cref="PageStorage"/> round-trip is filed under. Flutter reaches it through
    /// <c>ScrollPosition.context.storageContext</c> plus the scrollable's restoration id.
    /// </summary>
    internal string? RestorationId { get; set; }

    /// <summary>
    /// Invoked with <see cref="ScrollPhysics.ShouldAcceptUserOffset"/> whenever the dimensions
    /// change, so the owning scrollable can add or remove its drag gesture recognizers.
    /// </summary>
    internal Action<bool>? CanDragChanged { get; set; }

    public ScrollDirection UserScrollDirection => _userScrollDirection;

    public void JumpTo(double value)
    {
        GoIdle();
        if (Pixels != value)
        {
            ForcePixels(value);
        }

        // Physics that allow out-of-range offsets settle the jump back into range.
        GoBallistic(0.0);
    }

    /// <summary>
    /// Animates the position to <paramref name="value"/>, returning a task that completes when the
    /// animation ends or is superseded by another activity (Flutter's <c>Future&lt;void&gt;</c>).
    /// </summary>
    public Task AnimateTo(double value, TimeSpan duration, Curve? curve = null)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }
        if (duration == TimeSpan.Zero)
        {
            JumpTo(value);
            return Task.CompletedTask;
        }

        var activity = new DrivenScrollActivity(this, value, duration, curve ?? Curves.Linear);
        BeginActivity(activity);
        return activity.Done;
    }

    /// <summary>
    /// Jumps or animates to <paramref name="to"/> depending on whether a duration was supplied.
    /// </summary>
    public Task MoveTo(double to, TimeSpan? duration = null, Curve? curve = null)
    {
        if (duration is not { } animationDuration || animationDuration == TimeSpan.Zero)
        {
            JumpTo(to);
            return Task.CompletedTask;
        }

        return AnimateTo(to, animationDuration, curve ?? Curves.Ease);
    }

    /// <summary>
    /// Whether this position may be scrolled implicitly, for instance because an assistive
    /// technology asked a descendant to show itself on screen.
    /// </summary>
    public bool AllowImplicitScrolling => Physics.AllowImplicitScrolling;

    /// <summary>
    /// Scrolls this position so that <paramref name="target"/> becomes visible in the enclosing
    /// viewport.
    /// </summary>
    /// <param name="alignment">0.0 aligns the leading edge, 1.0 the trailing edge, 0.5 centers.</param>
    /// <param name="alignmentPolicy">
    /// Whether <paramref name="alignment"/> is used as given, or only far enough to keep the target
    /// visible at one edge without scrolling the other way.
    /// </param>
    /// <param name="targetRenderObject">
    /// The innermost object the caller actually wants revealed, when <paramref name="target"/> is an
    /// enclosing scrollable's render object rather than the original target.
    /// </param>
    public Task EnsureVisible(
        RenderObject target,
        double alignment = 0.0,
        TimeSpan duration = default,
        Curve? curve = null,
        ScrollPositionAlignmentPolicy alignmentPolicy = ScrollPositionAlignmentPolicy.Explicit,
        RenderObject? targetRenderObject = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        IRenderAbstractViewport? viewport = RenderAbstractViewport.MaybeOf(target);
        if (viewport is null)
        {
            return Task.CompletedTask;
        }

        Rect? targetRect = null;
        if (targetRenderObject != null && !ReferenceEquals(targetRenderObject, target))
        {
            targetRect = RenderObject.TransformRect(
                targetRenderObject.GetTransformTo(target),
                target.PaintBounds.Intersect(targetRenderObject.PaintBounds));
        }

        double resolved;
        switch (ApplyAxisDirectionToAlignmentPolicy(alignmentPolicy))
        {
            case ScrollPositionAlignmentPolicy.KeepVisibleAtEnd:
                resolved = Math.Clamp(
                    viewport.GetOffsetToReveal(target, 1.0, targetRect).Offset,
                    MinScrollExtent,
                    MaxScrollExtent);
                if (resolved < Pixels)
                {
                    resolved = Pixels;
                }

                break;
            case ScrollPositionAlignmentPolicy.KeepVisibleAtStart:
                resolved = Math.Clamp(
                    viewport.GetOffsetToReveal(target, 0.0, targetRect).Offset,
                    MinScrollExtent,
                    MaxScrollExtent);
                if (resolved > Pixels)
                {
                    resolved = Pixels;
                }

                break;
            default:
                resolved = Math.Clamp(
                    viewport.GetOffsetToReveal(target, alignment, targetRect).Offset,
                    MinScrollExtent,
                    MaxScrollExtent);
                break;
        }

        if (resolved == Pixels)
        {
            return Task.CompletedTask;
        }

        if (duration == TimeSpan.Zero)
        {
            JumpTo(resolved);
            return Task.CompletedTask;
        }

        return AnimateTo(resolved, duration, curve ?? Curves.Ease);
    }

    /// <summary>
    /// A reversed axis swaps which edge "start" and "end" mean; an explicit alignment is never
    /// flipped.
    /// </summary>
    private ScrollPositionAlignmentPolicy ApplyAxisDirectionToAlignmentPolicy(
        ScrollPositionAlignmentPolicy policy)
    {
        if (AxisDirection is not (AxisDirection.Up or AxisDirection.Left))
        {
            return policy;
        }

        return policy switch
        {
            ScrollPositionAlignmentPolicy.KeepVisibleAtEnd => ScrollPositionAlignmentPolicy.KeepVisibleAtStart,
            ScrollPositionAlignmentPolicy.KeepVisibleAtStart => ScrollPositionAlignmentPolicy.KeepVisibleAtEnd,
            _ => policy,
        };
    }

    public virtual void RestoreOffset(double offset, bool initialRestore = false)
    {
        if (!double.IsFinite(offset))
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "offset must be finite.");
        }

        if (initialRestore)
        {
            _pixels = offset;
            _hasPixels = true;
            return;
        }

        JumpTo(offset);
    }

    /// <summary>
    /// Writes the offset this position should be restored to into the enclosing
    /// <see cref="PageStorage"/>. Subclasses that track something other than pixels (a page index,
    /// for instance) override this together with <see cref="RestoreScrollOffset"/>.
    /// </summary>
    public virtual void SaveScrollOffset()
    {
        WriteStorageOffset(Pixels);
    }

    /// <summary>Reads back what <see cref="SaveScrollOffset"/> stored, if anything.</summary>
    public virtual void RestoreScrollOffset()
    {
        if (ReadStorageOffset() is { } offset)
        {
            RestoreOffset(offset, initialRestore: true);
        }
    }

    /// <summary>Stores <paramref name="value"/> under this position's restoration id.</summary>
    protected void WriteStorageOffset(double value)
    {
        if (!KeepScrollOffset || NotificationContext is not { } context)
        {
            return;
        }

        PageStorage.MaybeOf(context)?.WriteState(context, value, RestorationId);
    }

    /// <summary>Reads the value stored under this position's restoration id.</summary>
    protected double? ReadStorageOffset()
    {
        if (!KeepScrollOffset || NotificationContext is not { } context)
        {
            return null;
        }

        return PageStorage.MaybeOf(context)?.ReadState(context, RestorationId) is double offset
               && double.IsFinite(offset)
            ? offset
            : null;
    }

    public void BeginDrag()
    {
        BeginActivity(new DragScrollActivity(this));
    }

    /// <summary>
    /// Stops the current activity and holds the position still, remembering the velocity it was
    /// carrying so a drag started from this hold can restore it.
    /// </summary>
    public IScrollHoldController Hold(Action? holdCancelCallback = null)
    {
        double previousVelocity = Activity.Velocity;
        var holdActivity = new HoldScrollActivity(this, holdCancelCallback);
        BeginActivity(holdActivity);
        _heldPreviousVelocity = previousVelocity;
        return holdActivity;
    }

    /// <summary>
    /// Starts a drag, handing the physics' carried momentum and drag-start distance threshold to the
    /// returned controller.
    /// </summary>
    public virtual ScrollDragController Drag(DragStartDetails details, Action? dragCancelCallback = null)
    {
        var drag = new ScrollDragController(
            position: this,
            details: details,
            onDragCanceled: dragCancelCallback,
            carriedVelocity: Physics.CarriedMomentum(_heldPreviousVelocity),
            motionStartDistanceThreshold: Physics.DragStartDistanceMotionThreshold);
        BeginActivity(new DragScrollActivity(this, drag));
        _currentDrag = drag;
        return drag;
    }

    /// <summary>
    /// Whether the physics recommend deferring expensive frame-bound work because this position is
    /// changing quickly.
    /// </summary>
    public bool RecommendDeferredLoading(BuildContext context)
    {
        return Physics.RecommendDeferredLoading(
            Activity.Velocity + _impliedVelocity,
            FixedScrollMetrics.From(this),
            context);
    }

    internal void UpdateDragTo(double value)
    {
        if (Activity is not DragScrollActivity)
        {
            BeginDrag();
        }
        SetPixels(value);
    }

    public void EndDrag(double primaryPointerVelocity)
    {
        GoBallistic(-primaryPointerVelocity);
    }

    /// <summary>
    /// Starts a ballistic activity with the given velocity, or goes idle when the physics report
    /// that no simulation is needed.
    /// </summary>
    public virtual void GoBallistic(double velocity)
    {
        Simulation? simulation = Physics.CreateBallisticSimulation(this, velocity);
        if (simulation == null)
        {
            GoIdle();
            return;
        }

        BeginActivity(new BallisticScrollActivity(this, simulation));
    }

    public virtual void ApplyUserOffset(double delta)
    {
        double adjusted = Physics.ApplyPhysicsToUserOffset(this, delta);
        double targetPixels = Pixels - adjusted;
        UpdateUserScrollDirection(targetPixels);
        SetPixels(targetPixels);
    }

    public void ApplyPointerScrollDelta(double delta)
    {
        if (delta == 0.0)
        {
            GoBallistic(0.0);
            return;
        }

        // A pointer scroll never overscrolls: unlike a drag, the target is clamped into range and
        // written directly, bypassing the physics' boundary conditions.
        double targetPixels = Math.Min(Math.Max(Pixels + delta, MinScrollExtent), MaxScrollExtent);
        if (Math.Abs(targetPixels - Pixels) < Constants.PrecisionErrorTolerance)
        {
            return;
        }

        GoIdle();
        UpdateUserScrollDirection(targetPixels);
        IsScrollingNotifier.Value = true;
        ForcePixels(targetPixels);
        GoBallistic(0.0);
    }

    public virtual bool ApplyViewportDimension(double viewportDimension)
    {
        if (_hasViewportDimension && Math.Abs(_viewportDimension - viewportDimension) < 0.0001)
        {
            return false;
        }

        _viewportDimension = viewportDimension;
        _hasViewportDimension = true;
        _didChangeViewportDimensionOrReceiveCorrection = true;
        return true;
    }

    public virtual bool ApplyContentDimensions(double minScrollExtent, double maxScrollExtent)
    {
        bool minChanged = Math.Abs(_minScrollExtent - minScrollExtent) > 0.0001;
        bool maxChanged = Math.Abs(_maxScrollExtent - maxScrollExtent) > 0.0001;
        if (_hasContentDimensions
            && !minChanged
            && !maxChanged
            && !_didChangeViewportDimensionOrReceiveCorrection)
        {
            return false;
        }

        _minScrollExtent = minScrollExtent;
        _maxScrollExtent = maxScrollExtent;
        _hasContentDimensions = true;
        _didChangeViewportDimensionOrReceiveCorrection = false;

        var currentMetrics = FixedScrollMetrics.From(this);
        if (_lastMetrics != null)
        {
            // The physics decide where the position lands after the dimensions change: clamping
            // physics reinforce the boundary, bouncing physics keep the relative overscroll.
            double newPixels = Physics.AdjustPositionForNewDimensions(
                oldPosition: _lastMetrics,
                newPosition: currentMetrics,
                isScrolling: Activity.IsScrolling,
                velocity: Activity.Velocity);
            if (Math.Abs(newPixels - _pixels) > 0.0001)
            {
                CorrectPixels(newPixels);
            }
        }

        // Lets the current activity settle a position the new dimensions put out of range.
        ApplyNewDimensions();
        _lastMetrics = FixedScrollMetrics.From(this);
        _haveDimensions = true;
        return true;
    }

    /// <summary>
    /// Notifies the activity and the scrollable that the dimensions changed. Re-evaluating
    /// <see cref="ScrollPhysics.ShouldAcceptUserOffset"/> here is what registers or unregisters the
    /// scrollable's drag gesture recognizers.
    /// </summary>
    protected virtual void ApplyNewDimensions()
    {
        Activity.ApplyNewDimensions();
        CanDragChanged?.Invoke(Physics.ShouldAcceptUserOffset(this));
    }

    /// <summary>
    /// Takes over the scroll state of the position this one replaces, so that a drag or ballistic
    /// run in flight is not interrupted when a scrollable rebuilds its position (Flutter's
    /// <c>ScrollPosition.absorb</c> plus <c>ScrollPositionWithSingleContext.absorb</c>).
    /// </summary>
    public virtual void Absorb(ScrollPosition other)
    {
        ArgumentNullException.ThrowIfNull(other);

        _minScrollExtent = other._minScrollExtent;
        _maxScrollExtent = other._maxScrollExtent;
        _viewportDimension = other._viewportDimension;
        _pixels = other._pixels;
        _hasPixels = other._hasPixels;
        _hasViewportDimension = other._hasViewportDimension;
        _hasContentDimensions = other._hasContentDimensions;
        _haveDimensions = other._haveDimensions;
        _lastMetrics = other._lastMetrics;
        _userScrollDirection = other._userScrollDirection;
        _heldPreviousVelocity = other._heldPreviousVelocity;
        _didChangeViewportDimensionOrReceiveCorrection = true;

        // The activity moves over rather than being restarted: its ticker and simulation are still
        // valid, only the position they drive changes.
        ScrollActivity absorbed = other._activity;
        other._activity = new IdleScrollActivity(other);
        _activity.Dispose();
        _activity = absorbed;
        _activity.UpdateDelegate(this);
        IsScrollingNotifier.Value = _activity.IsScrolling;

        if (other._currentDrag is { } drag)
        {
            other._currentDrag = null;
            drag.UpdatePosition(this);
            _currentDrag = drag;
        }
    }

    public override void Dispose()
    {
        ScrollDragController? drag = _currentDrag;
        _currentDrag = null;
        drag?.Dispose();
        _activity.Dispose();
        IsScrollingNotifier.Dispose();
        base.Dispose();
    }

    internal virtual void BeginActivity(ScrollActivity activity)
    {
        _heldPreviousVelocity = 0.0;
        if (ReferenceEquals(_activity, activity))
        {
            return;
        }

        _activity.Dispose();
        _activity = activity;
        ScrollDragController? previousDrag = _currentDrag;
        _currentDrag = null;
        previousDrag?.Dispose();
        IsScrollingNotifier.Value = activity is not IdleScrollActivity;
    }

    internal virtual void GoIdle()
    {
        BeginActivity(new IdleScrollActivity(this));
    }

    internal double SetPixelsFromActivity(double value)
    {
        return SetPixels(value);
    }

    protected bool CorrectPixels(double value)
    {
        if (_hasPixels && Math.Abs(value - _pixels) < 0.0001)
        {
            return false;
        }

        _pixels = value;
        _hasPixels = true;
        NotifyListeners();
        return true;
    }

    /// <summary>
    /// Updates the offset without applying boundary conditions and contributes the displacement to
    /// deferred-loading velocity for the remainder of the current frame.
    /// </summary>
    protected void ForcePixels(double value)
    {
        _impliedVelocity = value - _pixels;
        _pixels = value;
        _hasPixels = true;
        NotifyListeners();
        Scheduler.AddPostFrameCallback(_ => _impliedVelocity = 0.0);
    }

    /// <summary>
    /// Updates the scroll position to the given value, applying the physics' boundary conditions.
    /// </summary>
    /// <returns>
    /// The overscroll: how far the value went beyond what the physics allow, or 0.0 when the whole
    /// change was applied. Physics that accept arbitrary offsets (such as
    /// <see cref="BouncingScrollPhysics"/>) always report 0.0, which is what lets the position travel
    /// outside the scroll extents.
    /// </returns>
    protected virtual double SetPixels(double value)
    {
        if (Math.Abs(value - _pixels) < Constants.PrecisionErrorTolerance)
        {
            return 0.0;
        }

        double overscroll = Physics.ApplyBoundaryConditions(this, value);
        double oldPixels = _pixels;
        _pixels = value - overscroll;
        _hasPixels = true;
        if (Math.Abs(_pixels - oldPixels) > Constants.PrecisionErrorTolerance)
        {
            NotifyListeners();
        }

        return Math.Abs(overscroll) > Constants.PrecisionErrorTolerance ? overscroll : 0.0;
    }

    private void UpdateUserScrollDirection(double targetPixels)
    {
        if (targetPixels < Pixels)
        {
            _userScrollDirection = ScrollDirection.Forward;
        }
        else if (targetPixels > Pixels)
        {
            _userScrollDirection = ScrollDirection.Reverse;
        }
    }
}
