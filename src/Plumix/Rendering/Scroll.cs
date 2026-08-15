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

    /// <summary>The quantity of content conceptually "above" the viewport.</summary>
    double ExtentBefore => Math.Max(Pixels - MinScrollExtent, 0.0);

    /// <summary>The quantity of content conceptually "below" the viewport.</summary>
    double ExtentAfter => Math.Max(MaxScrollExtent - Pixels, 0.0);

    /// <summary>The quantity of content conceptually "inside" the viewport.</summary>
    double ExtentInside
    {
        get
        {
            double leadingOverscroll = Math.Clamp(MinScrollExtent - Pixels, 0.0, ViewportDimension);
            double trailingOverscroll = Math.Clamp(Pixels - MaxScrollExtent, 0.0, ViewportDimension);
            return Math.Max(0.0, ViewportDimension - leadingOverscroll - trailingOverscroll);
        }
    }
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

public static class ScrollDirectionUtils
{
    /// <summary>Returns the opposite of the given <see cref="ScrollDirection"/>.</summary>
    /// <remarks>Flutter's <c>flipScrollDirection</c> (<c>rendering/viewport_offset.dart</c>).</remarks>
    public static ScrollDirection FlipScrollDirection(ScrollDirection direction)
    {
        return direction switch
        {
            ScrollDirection.Idle => ScrollDirection.Idle,
            ScrollDirection.Forward => ScrollDirection.Reverse,
            _ => ScrollDirection.Forward,
        };
    }

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

    /// <summary>
    /// Resolves the direction in which the scroll offset increases from a scroll axis, whether the
    /// view is reversed, and the ambient reading direction.
    /// </summary>
    /// <remarks>Flutter's <c>getAxisDirectionFromAxisReverseAndDirectionality</c>.</remarks>
    public static AxisDirection GetAxisDirectionFromAxisReverseAndDirectionality(
        BuildContext context,
        Axis axis,
        bool reverse)
    {
        if (axis == Axis.Vertical)
        {
            return reverse ? AxisDirection.Up : AxisDirection.Down;
        }

        AxisDirection readingDirection = Directionality.Of(context) == UI.TextDirection.Rtl
            ? AxisDirection.Left
            : AxisDirection.Right;
        if (!reverse)
        {
            return readingDirection;
        }

        return readingDirection == AxisDirection.Left ? AxisDirection.Right : AxisDirection.Left;
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

/// <summary>
/// The interface a <see cref="ScrollActivity"/> drives. Implemented by <see cref="ScrollPosition"/>
/// and by coordinators that own several positions at once, such as the one behind
/// <see cref="Plumix.Widgets.NestedScrollView"/>.
/// </summary>
// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_activity.dart
public interface IScrollActivityDelegate
{
    /// <summary>The direction in which the scroll offset increases.</summary>
    AxisDirection AxisDirection { get; }

    /// <summary>
    /// Updates the scroll position to the given value, applying the physics' boundary conditions and
    /// returning the overscroll that could not be applied.
    /// </summary>
    double SetPixels(double pixels);

    /// <summary>Applies a user-driven delta, in the opposite sense to the scroll offset.</summary>
    void ApplyUserOffset(double delta);

    /// <summary>Terminates the current activity and goes idle.</summary>
    void GoIdle();

    /// <summary>Terminates the current activity and starts a ballistic one at the given velocity.</summary>
    void GoBallistic(double velocity);
}

public abstract class ScrollActivity : IDisposable
{
    protected ScrollActivity(IScrollActivityDelegate @delegate)
    {
        Delegate = @delegate;
    }

    /// <summary>The delegate this activity drives.</summary>
    public IScrollActivityDelegate Delegate { get; private set; }

    /// <summary>
    /// Re-points this activity at the delegate that absorbed it, so an in-flight drag or ballistic
    /// run survives a scrollable replacing its <see cref="ScrollPosition"/>.
    /// </summary>
    public virtual void UpdateDelegate(IScrollActivityDelegate value)
    {
        Delegate = value;
    }

    /// <summary>Whether performing this activity constitutes scrolling.</summary>
    public virtual bool IsScrolling => true;

    /// <summary>The velocity at which the scroll offset is currently independently changing.</summary>
    public virtual double Velocity => 0.0;

    /// <summary>
    /// Called when the position this activity drives is absorbed by a position of a different type,
    /// so an activity that captured type-specific state can restart itself.
    /// </summary>
    /// <remarks>Flutter's <c>ScrollActivity.resetActivity</c>.</remarks>
    public virtual void ResetActivity()
    {
    }

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

public sealed class IdleScrollActivity(IScrollActivityDelegate @delegate) : ScrollActivity(@delegate)
{
    public override bool IsScrolling => false;

    public override void ApplyNewDimensions() => Delegate.GoBallistic(0.0);
}

/// <summary>
/// A scroll activity that holds the position still, cancelling any ballistic motion, while the
/// pointer that will drive the drag is still down.
/// </summary>
public sealed class HoldScrollActivity : ScrollActivity, IScrollHoldController
{
    private readonly Action? _onHoldCanceled;

    public HoldScrollActivity(IScrollActivityDelegate @delegate, Action? onHoldCanceled = null)
        : base(@delegate)
    {
        _onHoldCanceled = onHoldCanceled;
    }

    public override bool IsScrolling => false;

    public override double Velocity => 0.0;

    public void Cancel()
    {
        Delegate.GoBallistic(0.0);
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

    public DragScrollActivity(IScrollActivityDelegate @delegate, ScrollDragController? controller = null)
        : base(@delegate)
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
        IScrollActivityDelegate @delegate,
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

        Delegate = @delegate;
        CarriedVelocity = carriedVelocity;
        MotionStartDistanceThreshold = motionStartDistanceThreshold;
        _onDragCanceled = onDragCanceled;
        _retainMomentum = carriedVelocity is not null and not 0.0;
        _lastNonStationaryTimestampUtc = details.SourceTimeStampUtc;
        _offsetSinceLastStop = motionStartDistanceThreshold == null ? null : 0.0;
    }

    /// <summary>The delegate this drag scrolls.</summary>
    public IScrollActivityDelegate Delegate { get; private set; }

    /// <summary>
    /// Re-points this drag at the delegate that absorbed it (Flutter's
    /// <c>ScrollDragController.updateDelegate</c>).
    /// </summary>
    public void UpdateDelegate(IScrollActivityDelegate value)
    {
        Delegate = value;
    }

    /// <summary>Velocity carried over from a previous ballistic activity, if any.</summary>
    public double? CarriedVelocity { get; }

    /// <summary>The distance a resting drag must travel before the position starts moving.</summary>
    public double? MotionStartDistanceThreshold { get; }

    private bool Reversed => ScrollDirectionUtils.AxisDirectionIsReversed(Delegate.AxisDirection);

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

        Delegate.ApplyUserOffset(offset);
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

        Delegate.GoBallistic(velocity);
    }

    public void Cancel()
    {
        Delegate.GoBallistic(0.0);
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

public sealed class PointerScrollActivity(IScrollActivityDelegate @delegate) : ScrollActivity(@delegate)
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
        IScrollActivityDelegate @delegate,
        double from,
        double to,
        TimeSpan duration,
        Curve curve,
        ITickerProvider? vsync) : base(@delegate)
    {
        if (!double.IsFinite(from))
        {
            throw new ArgumentOutOfRangeException(nameof(from));
        }
        if (!double.IsFinite(to))
        {
            throw new ArgumentOutOfRangeException(nameof(to));
        }
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        _from = from;
        _to = to;
        _duration = duration;
        _curve = curve ?? throw new ArgumentNullException(nameof(curve));
        _ticker = vsync?.CreateTicker(OnTick) ?? new Ticker(OnTick);
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

        _elapsed = elapsed;
        double progress = Math.Clamp(_elapsed.TotalSeconds / _duration.TotalSeconds, 0.0, 1.0);
        // Flutter drives this activity through an AnimationController, whose interpolation simulation
        // returns the exact end value at t == 1 rather than the curve's approximation of it.
        double value = progress >= 1.0 ? _to : _from + ((_to - _from) * _curve(progress));
        Delegate.SetPixels(value);
        if (progress >= 1.0)
        {
            Delegate.GoIdle();
        }
    }
}

public class BallisticScrollActivity : ScrollActivity
{
    private readonly Simulation _simulation;
    private readonly Ticker _ticker;
    private double _elapsedSeconds;
    private bool _disposed;

    public BallisticScrollActivity(
        IScrollActivityDelegate @delegate,
        Simulation simulation,
        ITickerProvider? vsync) : base(@delegate)
    {
        _simulation = simulation;
        _ticker = vsync?.CreateTicker(OnTick) ?? new Ticker(OnTick);
        _ticker.Start();
    }

    public override double Velocity => _disposed ? 0.0 : _simulation.DX(_elapsedSeconds);

    public override void ResetActivity() => Delegate.GoBallistic(Velocity);

    public override void ApplyNewDimensions() => Delegate.GoBallistic(Velocity);

    /// <summary>
    /// Moves the delegate to <paramref name="value"/>, returning whether the simulation can still be
    /// followed. A non-zero overscroll means the boundary conditions clipped the value.
    /// </summary>
    /// <remarks>Flutter's <c>BallisticScrollActivity.applyMoveTo</c>.</remarks>
    protected virtual bool ApplyMoveTo(double value)
    {
        return Math.Abs(Delegate.SetPixels(value)) < Constants.PrecisionErrorTolerance;
    }

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

        _elapsedSeconds = elapsed.TotalSeconds;

        // The simulation drives the position directly; the physics decide whether the proposed value
        // is reachable. A move that cannot be followed ends the activity.
        if (!ApplyMoveTo(_simulation.X(_elapsedSeconds)))
        {
            Delegate.GoIdle();
            return;
        }

        if (_simulation.IsDone(_elapsedSeconds))
        {
            // A completed ballistic run restarts ballistic with zero velocity, which springs the
            // position back when it is still out of range and goes idle otherwise.
            Delegate.GoBallistic(0.0);
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

public class ScrollPosition : ViewportOffset, IScrollMetrics, IScrollActivityDelegate
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
    private AxisDirection _lastMetricsAxisDirection = AxisDirection.Down;
    private Axis? _lastAxis;
    private bool _pendingDimensions;
    private bool _haveScheduledUpdateNotification;
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
    public override double Pixels => _pixels;

    /// <summary>Whether <see cref="Pixels"/> has been established by a layout or a correction.</summary>
    public override bool HasPixels => _hasPixels;

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

    /// <summary>The quantity of content conceptually "above" the viewport.</summary>
    public double ExtentBefore => Math.Max(_pixels - _minScrollExtent, 0.0);

    /// <summary>The quantity of content conceptually "below" the viewport.</summary>
    public double ExtentAfter => Math.Max(_maxScrollExtent - _pixels, 0.0);

    /// <summary>The quantity of content conceptually "inside" the viewport.</summary>
    public double ExtentInside => ((IScrollMetrics)this).ExtentInside;

    public ScrollPhysics Physics => _physics;

    public ScrollActivity Activity => _activity;

    public AxisDirection AxisDirection { get; internal set; } = AxisDirection.Down;

    /// <summary>The axis along which this position scrolls.</summary>
    public Axis Axis => ScrollDirectionUtils.AxisDirectionToAxis(AxisDirection);

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

    public override ScrollDirection UserScrollDirection => _userScrollDirection;

    public override void JumpTo(double pixels)
    {
        GoIdle();
        if (Pixels != pixels)
        {
            ForcePixels(pixels);
        }

        // Physics that allow out-of-range offsets settle the jump back into range.
        GoBallistic(0.0);
    }

    /// <summary>
    /// Animates the position to <paramref name="value"/>, returning a task that completes when the
    /// animation ends or is superseded by another activity (Flutter's <c>Future&lt;void&gt;</c>).
    /// </summary>
    public override Task AnimateTo(double to, TimeSpan duration, Curve? curve = null)
    {
        if (!double.IsFinite(to))
        {
            throw new ArgumentOutOfRangeException(nameof(to));
        }
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }
        if (duration == TimeSpan.Zero)
        {
            JumpTo(to);
            return Task.CompletedTask;
        }

        var activity = new DrivenScrollActivity(
            this,
            from: Pixels,
            to: to,
            duration: duration,
            curve: curve ?? Curves.Linear,
            vsync: TickerProvider);
        BeginActivity(activity);
        return activity.Done;
    }

    /// <summary>
    /// Jumps or animates to <paramref name="to"/> depending on whether a duration was supplied,
    /// clamping the target into the scroll extents unless <paramref name="clamp"/> is false.
    /// </summary>
    public override Task MoveTo(
        double to,
        TimeSpan? duration = null,
        Curve? curve = null,
        bool? clamp = null)
    {
        double target = clamp ?? true ? Math.Clamp(to, MinScrollExtent, MaxScrollExtent) : to;
        return base.MoveTo(target, duration, curve);
    }

    /// <summary>
    /// Applies a layout-time correction: the offset moves without notifying listeners, and the next
    /// <see cref="ApplyContentDimensions"/> is treated as a fresh set of dimensions.
    /// </summary>
    public override void CorrectBy(double correction)
    {
        if (!_hasPixels)
        {
            throw new InvalidOperationException(
                "An initial pixels value must exist by calling CorrectPixels on the ScrollPosition.");
        }

        _pixels += correction;
        _didChangeViewportDimensionOrReceiveCorrection = true;
    }

    /// <summary>
    /// Whether this position may be scrolled implicitly, for instance because an assistive
    /// technology asked a descendant to show itself on screen.
    /// </summary>
    public override bool AllowImplicitScrolling => Physics.AllowImplicitScrolling;

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
    public virtual IScrollHoldController Hold(Action? holdCancelCallback = null)
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
            @delegate: this,
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

        BeginActivity(new BallisticScrollActivity(this, simulation, TickerProvider));
    }

    public virtual void ApplyUserOffset(double delta)
    {
        double adjusted = Physics.ApplyPhysicsToUserOffset(this, delta);
        double targetPixels = Pixels - adjusted;
        UpdateUserScrollDirectionTowards(targetPixels);
        SetPixels(targetPixels);
    }

    /// <remarks>Flutter's <c>ScrollPositionWithSingleContext.pointerScroll</c>.</remarks>
    public virtual void ApplyPointerScrollDelta(double delta)
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
        UpdateUserScrollDirection(
            delta > 0.0 ? ScrollDirection.Reverse : ScrollDirection.Forward);
        double oldPixels = Pixels;
        IsScrollingNotifier.Value = true;
        ForcePixels(targetPixels);
        DidStartScroll();
        DidUpdateScrollPositionBy(Pixels - oldPixels);
        DidEndScroll();
        GoBallistic(0.0);
    }

    public override bool ApplyViewportDimension(double viewportDimension)
    {
        if (!_hasViewportDimension || _viewportDimension != viewportDimension)
        {
            _viewportDimension = viewportDimension;
            _hasViewportDimension = true;
            // Everything that needs both values is done in ApplyContentDimensions, which the viewport
            // always calls soon afterwards in the same layout phase.
            _didChangeViewportDimensionOrReceiveCorrection = true;
        }

        return true;
    }

    public override bool ApplyContentDimensions(double minScrollExtent, double maxScrollExtent)
    {
        double tolerance = Tolerance.DefaultTolerance.Distance;
        if (!_hasContentDimensions
            || !NearEqual(_minScrollExtent, minScrollExtent, tolerance)
            || !NearEqual(_maxScrollExtent, maxScrollExtent, tolerance)
            || _didChangeViewportDimensionOrReceiveCorrection
            || _lastAxis != Axis)
        {
            if (minScrollExtent > maxScrollExtent)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minScrollExtent),
                    "minScrollExtent must be less than or equal to maxScrollExtent.");
            }

            _minScrollExtent = minScrollExtent;
            _maxScrollExtent = maxScrollExtent;
            _hasContentDimensions = true;
            _lastAxis = Axis;
            FixedScrollMetrics? currentMetrics = _haveDimensions ? FixedScrollMetrics.From(this) : null;
            _didChangeViewportDimensionOrReceiveCorrection = false;
            _pendingDimensions = true;
            if (_haveDimensions && !CorrectForNewDimensions(_lastMetrics!, currentMetrics!))
            {
                return false;
            }

            _haveDimensions = true;
        }

        if (_pendingDimensions)
        {
            // Lets the current activity settle a position the new dimensions put out of range.
            ApplyNewDimensions();
            _pendingDimensions = false;
        }

        if (IsMetricsChanged())
        {
            // It is too late to send a useful notification now: the potential listeners have already
            // been built this frame, so the dispatch waits until the frame is complete.
            if (!_haveScheduledUpdateNotification)
            {
                Scheduler.ScheduleMicrotask(DidUpdateScrollMetrics);
                _haveScheduledUpdateNotification = true;
            }

            _lastMetrics = FixedScrollMetrics.From(this);
            _lastMetricsAxisDirection = AxisDirection;
        }

        return true;
    }

    /// <summary>
    /// Verifies that the new content and viewport dimensions are acceptable, correcting the offset
    /// and returning false when they are not.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>ScrollPosition.correctForNewDimensions</c>. The default implementation defers to
    /// <see cref="ScrollPhysics.AdjustPositionForNewDimensions"/>.
    /// </remarks>
    protected virtual bool CorrectForNewDimensions(IScrollMetrics oldPosition, IScrollMetrics newPosition)
    {
        // The physics decide where the position lands after the dimensions change: clamping physics
        // reinforce the boundary, bouncing physics keep the relative overscroll.
        double newPixels = Physics.AdjustPositionForNewDimensions(
            oldPosition: oldPosition,
            newPosition: newPosition,
            isScrolling: Activity.IsScrolling,
            velocity: Activity.Velocity);
        if (newPixels != _pixels)
        {
            CorrectPixels(newPixels);
            return false;
        }

        return true;
    }

    /// <summary>Dispatches a notification that the scroll metrics changed.</summary>
    /// <remarks>Flutter's <c>ScrollPosition.didUpdateScrollMetrics</c>.</remarks>
    public void DidUpdateScrollMetrics()
    {
        _haveScheduledUpdateNotification = false;
        if (NotificationContext is { } context)
        {
            new ScrollMetricsNotification(CopyWith(), context).Dispatch(context);
        }
    }

    /// <summary>Whether the visible/hidden extents or the axis direction moved since the last dispatch.</summary>
    /// <remarks>Flutter's <c>ScrollPosition._isMetricsChanged</c>.</remarks>
    private bool IsMetricsChanged()
    {
        if (_lastMetrics is not { } last)
        {
            return true;
        }

        ScrollMetricsSnapshot current = CopyWith();
        var previous = new ScrollMetricsSnapshot(
            Pixels: last.Pixels,
            MinScrollExtent: last.MinScrollExtent,
            MaxScrollExtent: last.MaxScrollExtent,
            ViewportDimension: last.ViewportDimension,
            AxisDirection: _lastMetricsAxisDirection);
        return !(current.ExtentBefore == previous.ExtentBefore
                 && current.ExtentInside == previous.ExtentInside
                 && current.ExtentAfter == previous.ExtentAfter
                 && current.AxisDirection == previous.AxisDirection);
    }

    /// <summary>An immutable snapshot of this position's metrics.</summary>
    /// <remarks>
    /// Flutter's <c>ScrollMetrics.copyWith</c> with no overrides. Subclasses that carry extra metrics
    /// (a page view's viewport fraction, for instance) override this.
    /// </remarks>
    public virtual ScrollMetricsSnapshot CopyWith()
    {
        return new ScrollMetricsSnapshot(
            Pixels: _pixels,
            MinScrollExtent: _minScrollExtent,
            MaxScrollExtent: _maxScrollExtent,
            ViewportDimension: _viewportDimension,
            AxisDirection: AxisDirection);
    }

    /// <remarks>Flutter's <c>nearEqual</c>.</remarks>
    private static bool NearEqual(double a, double b, double epsilon)
    {
        return (a > b - epsilon && a < b + epsilon) || a == b;
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
        UpdateSemanticActions();
    }

    /// <summary>
    /// Raised whenever the set of scroll directions the position still accepts changes, so that the
    /// scrollable can filter the semantics actions its gesture handler exposes.
    /// </summary>
    /// <remarks>
    /// Flutter routes this through <c>ScrollContext.setSemanticsActions</c>; Plumix has no
    /// <c>ScrollContext</c>, so the scrollable subscribes to this event instead.
    /// </remarks>
    public event Action<SemanticsActions>? SemanticActionsChanged;

    private SemanticsActions? _semanticActions;

    /// <summary>
    /// Recomputes which directional scroll actions are still available. Scrolled to the top, the
    /// action that scrolls further up has to disappear.
    /// </summary>
    /// <remarks>Flutter's <c>ScrollPosition._updateSemanticActions</c>.</remarks>
    protected void UpdateSemanticActions()
    {
        (SemanticsActions forward, SemanticsActions backward) = AxisDirection switch
        {
            AxisDirection.Up => (SemanticsActions.ScrollDown, SemanticsActions.ScrollUp),
            AxisDirection.Down => (SemanticsActions.ScrollUp, SemanticsActions.ScrollDown),
            AxisDirection.Left => (SemanticsActions.ScrollRight, SemanticsActions.ScrollLeft),
            _ => (SemanticsActions.ScrollLeft, SemanticsActions.ScrollRight)
        };

        SemanticsActions actions = SemanticsActions.None;
        if (_hasPixels && _pixels > MinScrollExtent)
        {
            actions |= backward;
        }

        if (_hasPixels && _pixels < MaxScrollExtent)
        {
            actions |= forward;
        }

        if (_semanticActions == actions)
        {
            return;
        }

        _semanticActions = actions;
        SemanticActionsChanged?.Invoke(actions);
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
        if (other.GetType() != GetType())
        {
            _activity.ResetActivity();
        }

        IsScrollingNotifier.Value = _activity.IsScrolling;

        if (other._currentDrag is { } drag)
        {
            other._currentDrag = null;
            drag.UpdateDelegate(this);
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

    public virtual void GoIdle()
    {
        BeginActivity(new IdleScrollActivity(this));
    }

    /// <summary>
    /// Changes <see cref="Pixels"/> without notifying listeners, which is what makes it safe to call
    /// while the viewport is laying out (Flutter's <c>ScrollPosition.correctPixels</c>).
    /// </summary>
    protected bool CorrectPixels(double value)
    {
        if (_hasPixels && Math.Abs(value - _pixels) < 0.0001)
        {
            return false;
        }

        _pixels = value;
        _hasPixels = true;
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
        UpdateSemanticActions();
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
    public virtual double SetPixels(double value)
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
            UpdateSemanticActions();
        }

        return Math.Abs(overscroll) > Constants.PrecisionErrorTolerance ? overscroll : 0.0;
    }

    private void UpdateUserScrollDirectionTowards(double targetPixels)
    {
        if (targetPixels < Pixels)
        {
            UpdateUserScrollDirection(ScrollDirection.Forward);
        }
        else if (targetPixels > Pixels)
        {
            UpdateUserScrollDirection(ScrollDirection.Reverse);
        }
    }

    /// <summary>
    /// Records the direction the user is scrolling in and announces it through a
    /// <see cref="UserScrollNotification"/>.
    /// </summary>
    /// <remarks>Flutter's <c>ScrollPositionWithSingleContext.updateUserScrollDirection</c>.</remarks>
    protected internal virtual void UpdateUserScrollDirection(ScrollDirection value)
    {
        if (_userScrollDirection == value)
        {
            return;
        }

        _userScrollDirection = value;
        DidUpdateScrollDirection(value);
    }

    /// <summary>Dispatches a <see cref="ScrollStartNotification"/>.</summary>
    /// <remarks>Flutter's <c>ScrollPosition.didStartScroll</c>.</remarks>
    public void DidStartScroll()
    {
        if (NotificationContext is { } context)
        {
            new ScrollStartNotification(CopyWith()).Dispatch(context);
        }
    }

    /// <summary>Dispatches a <see cref="ScrollUpdateNotification"/>.</summary>
    /// <remarks>Flutter's <c>ScrollPosition.didUpdateScrollPositionBy</c>.</remarks>
    public void DidUpdateScrollPositionBy(double delta)
    {
        if (NotificationContext is { } context)
        {
            new ScrollUpdateNotification(CopyWith(), scrollDelta: delta).Dispatch(context);
        }
    }

    /// <summary>Dispatches a <see cref="ScrollEndNotification"/>.</summary>
    /// <remarks>Flutter's <c>ScrollPosition.didEndScroll</c>.</remarks>
    public void DidEndScroll()
    {
        if (NotificationContext is { } context)
        {
            new ScrollEndNotification(CopyWith()).Dispatch(context);
        }
    }

    /// <summary>Dispatches an <see cref="OverscrollNotification"/>.</summary>
    /// <remarks>Flutter's <c>ScrollPosition.didOverscrollBy</c>.</remarks>
    public void DidOverscrollBy(double value)
    {
        if (NotificationContext is { } context)
        {
            new OverscrollNotification(CopyWith(), overscroll: value).Dispatch(context);
        }
    }

    /// <summary>Dispatches a <see cref="UserScrollNotification"/>.</summary>
    /// <remarks>Flutter's <c>ScrollPosition.didUpdateScrollDirection</c>.</remarks>
    public void DidUpdateScrollDirection(ScrollDirection direction)
    {
        if (NotificationContext is { } context)
        {
            new UserScrollNotification(CopyWith(), direction).Dispatch(context);
        }
    }
}
