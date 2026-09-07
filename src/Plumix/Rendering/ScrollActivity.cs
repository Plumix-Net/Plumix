using System.Diagnostics;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Physics;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_activity.dart

namespace Plumix.Rendering;

/// <summary>
/// A backend for a <see cref="ScrollActivity"/>.
/// </summary>
/// <remarks>
/// Used by subclasses of <see cref="ScrollActivity"/> to manipulate the scroll view that they are
/// acting upon.
/// <para>
/// Dart declares this as an abstract class that <c>ScrollPositionWithSingleContext</c> implements;
/// C# states and positions already have a base class, so the contract is an interface.
/// </para>
/// </remarks>
public interface IScrollActivityDelegate
{
    /// <summary>The direction in which the scroll view scrolls.</summary>
    AxisDirection AxisDirection { get; }

    /// <summary>
    /// Update the scroll position to the given pixel value.
    /// </summary>
    /// <returns>The overscroll, as described by <see cref="ScrollPosition.SetPixels"/>.</returns>
    double SetPixels(double pixels);

    /// <summary>
    /// Updates the scroll position by the given amount.
    /// </summary>
    /// <remarks>
    /// Appropriate for when the user is directly manipulating the scroll position, which is why the
    /// implementations apply <see cref="ScrollPhysics.ApplyPhysicsToUserOffset"/>.
    /// </remarks>
    void ApplyUserOffset(double delta);

    /// <summary>Terminate the current activity and start an idle activity.</summary>
    void GoIdle();

    /// <summary>Terminate the current activity and start a ballistic activity with the given velocity.</summary>
    void GoBallistic(double velocity);
}

/// <summary>
/// Base class for scrolling activities like dragging and flinging.
/// </summary>
/// <remarks>
/// See also <see cref="ScrollPosition"/>, which uses <see cref="ScrollActivity"/> objects to manage
/// the <see cref="ScrollPosition"/> of a <see cref="Scrollable"/>.
/// </remarks>
public abstract class ScrollActivity : IDisposable
{
    private IScrollActivityDelegate _delegate;

    /// <summary>Initializes <see cref="Delegate"/> for subclasses.</summary>
    protected ScrollActivity(IScrollActivityDelegate @delegate)
    {
        _delegate = @delegate;
    }

    /// <summary>The delegate that this activity will use to actuate the scroll view.</summary>
    public IScrollActivityDelegate Delegate => _delegate;

    /// <summary>
    /// Whether <see cref="Dispose"/> has run, so an animation callback that outlives the activity
    /// can tell that it has nothing left to drive (Dart's <c>_isDisposed</c>).
    /// </summary>
    private protected bool IsDisposed { get; private set; }

    /// <summary>
    /// Updates the activity's link to the <see cref="IScrollActivityDelegate"/>.
    /// </summary>
    /// <remarks>
    /// This should only be called when an activity is being moved from a defunct (or about-to-be
    /// defunct) <see cref="IScrollActivityDelegate"/> object to a new one.
    /// </remarks>
    public virtual void UpdateDelegate(IScrollActivityDelegate value)
    {
        Debug.Assert(!ReferenceEquals(_delegate, value));
        _delegate = value;
    }

    /// <summary>
    /// Called by the <see cref="ScrollPosition"/> when it has changed type (for example, when
    /// changing from an Android-style scroll position to an iOS-style scroll position). If this
    /// activity can differ between the two types of scroll positions, then it should tell the
    /// position to restart that activity appropriately.
    /// </summary>
    /// <remarks>
    /// For example, <see cref="BallisticScrollActivity"/>'s implementation calls
    /// <see cref="IScrollActivityDelegate.GoBallistic"/>.
    /// </remarks>
    public virtual void ResetActivity()
    {
    }

    /// <summary>Dispatch a <see cref="ScrollStartNotification"/> with the given metrics.</summary>
    public virtual void DispatchScrollStartNotification(IScrollMetrics metrics, BuildContext? context)
    {
        new ScrollStartNotification(metrics, sourceContext: context).Dispatch(context);
    }

    /// <summary>Dispatch a <see cref="ScrollUpdateNotification"/> with the given metrics and scroll delta.</summary>
    public virtual void DispatchScrollUpdateNotification(
        IScrollMetrics metrics,
        BuildContext context,
        double scrollDelta)
    {
        new ScrollUpdateNotification(metrics, scrollDelta: scrollDelta, sourceContext: context).Dispatch(context);
    }

    /// <summary>Dispatch an <see cref="OverscrollNotification"/> with the given metrics and overscroll.</summary>
    public virtual void DispatchOverscrollNotification(
        IScrollMetrics metrics,
        BuildContext context,
        double overscroll)
    {
        new OverscrollNotification(metrics, overscroll: overscroll, sourceContext: context).Dispatch(context);
    }

    /// <summary>Dispatch a <see cref="ScrollEndNotification"/> with the given metrics and overscroll.</summary>
    public virtual void DispatchScrollEndNotification(IScrollMetrics metrics, BuildContext context)
    {
        new ScrollEndNotification(metrics, sourceContext: context).Dispatch(context);
    }

    /// <summary>Called when the scroll view that is performing this activity changes its metrics.</summary>
    public virtual void ApplyNewDimensions()
    {
    }

    /// <summary>
    /// Whether the scroll view should ignore pointer events while performing this activity.
    /// </summary>
    /// <remarks>See also <see cref="IsScrolling"/>, and <see cref="Velocity"/>.</remarks>
    public abstract bool ShouldIgnorePointer { get; }

    /// <summary>Whether performing this activity constitutes scrolling.</summary>
    /// <remarks>
    /// Used, for example, to determine whether the user scroll direction (see
    /// <see cref="ScrollPosition.UserScrollDirection"/>) is appropriate to apply.
    /// </remarks>
    public abstract bool IsScrolling { get; }

    /// <summary>
    /// If applicable, the velocity at which the scroll offset is currently independently changing
    /// (i.e. without external stimuli such as a dragging gestures) in logical pixels per second.
    /// </summary>
    public abstract double Velocity { get; }

    /// <summary>Called when the scroll view stops performing this activity.</summary>
    public virtual void Dispose()
    {
        IsDisposed = true;
    }

    public override string ToString() => Diagnostics.DescribeIdentity(this);
}

/// <summary>
/// A scroll activity that does nothing.
/// </summary>
/// <remarks>
/// When a scroll widget stops scrolling, it is instructed to begin performing the
/// <see cref="IdleScrollActivity"/>.
/// </remarks>
public class IdleScrollActivity(IScrollActivityDelegate @delegate) : ScrollActivity(@delegate)
{
    public override void ApplyNewDimensions() => Delegate.GoBallistic(0.0);

    public override bool ShouldIgnorePointer => false;

    public override bool IsScrolling => false;

    public override double Velocity => 0.0;
}

/// <summary>
/// A scroll activity that does nothing but can be released to resume normal idle behavior.
/// </summary>
/// <remarks>
/// This is used while the user is touching the <see cref="Scrollable"/> but before the touch has
/// become a <see cref="Drag"/>.
/// <para>
/// For the purposes of <see cref="ScrollNotification"/>s, this activity does not constitute
/// scrolling, and does not prevent the user from interacting with the contents of the
/// <see cref="Scrollable"/> (unlike when a drag has begun or there is a scroll animation underway).
/// </para>
/// </remarks>
public class HoldScrollActivity : ScrollActivity, IScrollHoldController
{
    public HoldScrollActivity(IScrollActivityDelegate @delegate, Action? onHoldCanceled = null)
        : base(@delegate)
    {
        OnHoldCanceled = onHoldCanceled;
    }

    /// <summary>Called when <see cref="ScrollActivity.Dispose"/> is called.</summary>
    public Action? OnHoldCanceled { get; }

    public override bool ShouldIgnorePointer => false;

    public override bool IsScrolling => false;

    public override double Velocity => 0.0;

    public void Cancel()
    {
        Delegate.GoBallistic(0.0);
    }

    public override void Dispose()
    {
        OnHoldCanceled?.Invoke();
        base.Dispose();
    }
}

/// <summary>
/// Scrolls a scroll view as the user drags their finger across the screen.
/// </summary>
/// <remarks>
/// See also <see cref="DragScrollActivity"/>, which is the activity the scroll view performs while
/// a drag is underway.
/// </remarks>
public class ScrollDragController : IDrag
{
    /// <summary>
    /// Maximum amount of time interval the drag can have consecutive stationary pointer update
    /// events before losing the momentum carried from a previous scroll activity.
    /// </summary>
    public static readonly TimeSpan MomentumRetainStationaryDurationThreshold =
        TimeSpan.FromMilliseconds(20);

    /// <summary>
    /// Maximum amount of time interval the drag can have consecutive stationary pointer update
    /// events before needing to break the <see cref="MotionStartDistanceThreshold"/> to start
    /// motion again.
    /// </summary>
    public static readonly TimeSpan MotionStoppedDurationThreshold = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// The drag distance past which, a [motionStartDistanceThreshold] breaking drag is considered a
    /// deliberate fling.
    /// </summary>
    private const double BigThresholdBreakDistance = 24.0;

    /// <summary>
    /// The minimum amount of velocity needed to apply the momentum retained from a previous scroll
    /// activity, expressed as a factor of <see cref="CarriedVelocity"/>.
    /// </summary>
    public const double MomentumRetainVelocityThresholdFactor = 0.5;

    private readonly PointerDeviceKind? _kind;
    private IScrollActivityDelegate _delegate;
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

        _delegate = @delegate;
        LastDetails = details;
        OnDragCanceled = onDragCanceled;
        CarriedVelocity = carriedVelocity;
        MotionStartDistanceThreshold = motionStartDistanceThreshold;
        _retainMomentum = carriedVelocity is not null and not 0.0;
        _lastNonStationaryTimestampUtc = details.SourceTimeStampUtc;
        _kind = details.Kind;
        _offsetSinceLastStop = motionStartDistanceThreshold == null ? null : 0.0;
    }

    /// <summary>The object that will actuate the scroll view as the user drags.</summary>
    public IScrollActivityDelegate Delegate => _delegate;

    /// <summary>Called when <see cref="Dispose"/> is called.</summary>
    public Action? OnDragCanceled { get; }

    /// <summary>Velocity that was present from a previous <see cref="ScrollActivity"/> when this
    /// drag began.</summary>
    public double? CarriedVelocity { get; }

    /// <summary>Amount of pixels in either direction the drag has to move by to start scroll
    /// movement again after each time scrolling came to a stop.</summary>
    public double? MotionStartDistanceThreshold { get; }

    /// <summary>
    /// The most recently observed <see cref="DragStartDetails"/>, <see cref="DragUpdateDetails"/>,
    /// or <see cref="DragEndDetails"/> object.
    /// </summary>
    public object? LastDetails { get; private set; }

    /// <summary>The kind of pointer that is driving this drag (Flutter's <c>_kind</c>).</summary>
    internal PointerDeviceKind? Kind => _kind;

    private bool Reversed => ScrollDirectionUtils.AxisDirectionIsReversed(Delegate.AxisDirection);

    /// <summary>Updates the controller's link to the <see cref="IScrollActivityDelegate"/>.</summary>
    public void UpdateDelegate(IScrollActivityDelegate value)
    {
        Debug.Assert(!ReferenceEquals(_delegate, value));
        _delegate = value;
    }

    /// <summary>
    /// Determines whether to lose the existing incoming velocity when starting the drag.
    /// </summary>
    private void MaybeLoseMomentum(double offset, DateTime? timestampUtc)
    {
        if (_retainMomentum
            && offset == 0.0
            && (timestampUtc == null
                || timestampUtc.Value - _lastNonStationaryTimestampUtc!.Value
                > MomentumRetainStationaryDurationThreshold))
        {
            // If pointer is stationary for too long, we lose momentum.
            _retainMomentum = false;
        }
    }

    /// <summary>
    /// If a motion start threshold exists, determine whether the threshold needs to be broken to
    /// scroll. Also possibly apply an offset adjustment when the threshold is first broken.
    /// </summary>
    /// <returns>The <paramref name="offset"/> that should be used to scroll.</returns>
    private double AdjustForScrollStartThreshold(double offset, DateTime? timestampUtc)
    {
        if (timestampUtc == null)
        {
            // If we can't track time, we can't apply thresholds.
            // May be null for proxied drags like via accessibility.
            return offset;
        }

        if (offset == 0.0)
        {
            if (MotionStartDistanceThreshold != null
                && _offsetSinceLastStop == null
                && timestampUtc.Value - _lastNonStationaryTimestampUtc!.Value > MotionStoppedDurationThreshold)
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

    public void Update(DragUpdateDetails details)
    {
        Debug.Assert(details.PrimaryDelta != null);
        LastDetails = details;
        double offset = details.PrimaryDelta!.Value;
        if (offset != 0.0)
        {
            _lastNonStationaryTimestampUtc = details.SourceTimeStampUtc;
        }

        // By default, iOS platforms carries momentum and has a start threshold (configured in
        // [BouncingScrollPhysics]). The rest of the platforms do not.
        MaybeLoseMomentum(offset, details.SourceTimeStampUtc);
        offset = AdjustForScrollStartThreshold(offset, details.SourceTimeStampUtc);
        if (offset == 0.0)
        {
            return;
        }

        if (Reversed)
        {
            // Reverse the delta if the scroll is reversed.
            offset = -offset;
        }

        Delegate.ApplyUserOffset(offset);
    }

    public void End(DragEndDetails details)
    {
        Debug.Assert(details.PrimaryVelocity != null);
        // We negate the velocity here because if the touch is moving downwards,
        // the scroll has to move upwards. It's the same reason that update()
        // above negates the delta before applying it to the scroll offset.
        double velocity = -details.PrimaryVelocity!.Value;
        if (Reversed)
        {
            // Reverse the velocity if the scroll is reversed.
            velocity = -velocity;
        }

        LastDetails = details;

        // Build momentum only if dragging in the same direction.
        bool isFlingingInSameDirection = Math.Sign(velocity) == Math.Sign(CarriedVelocity ?? 0.0);
        // Build momentum only if the velocity of the last drag was not substantially lower than the
        // carried momentum.
        bool isVelocityNotSubstantiallyLessThanCarriedMomentum =
            Math.Abs(velocity) > Math.Abs(CarriedVelocity ?? 0.0) * MomentumRetainVelocityThresholdFactor;
        if (_retainMomentum && isFlingingInSameDirection && isVelocityNotSubstantiallyLessThanCarriedMomentum)
        {
            velocity += CarriedVelocity!.Value;
        }

        Delegate.GoBallistic(velocity);
    }

    public void Cancel()
    {
        Delegate.GoBallistic(0.0);
    }

    /// <summary>Called by the delegate when it is no longer sending events to this object.</summary>
    public virtual void Dispose()
    {
        LastDetails = null;
        OnDragCanceled?.Invoke();
    }

    public override string ToString() => Diagnostics.DescribeIdentity(this);
}

/// <summary>
/// The activity a scroll view performs when a user drags their finger across the screen.
/// </summary>
/// <remarks>
/// See also <see cref="ScrollDragController"/>, which listens to the <see cref="IDrag"/> and actually
/// scrolls the scroll view.
/// </remarks>
public class DragScrollActivity : ScrollActivity
{
    private ScrollDragController? _controller;

    public DragScrollActivity(IScrollActivityDelegate @delegate, ScrollDragController controller)
        : base(@delegate)
    {
        _controller = controller;
    }

    public override void DispatchScrollStartNotification(IScrollMetrics metrics, BuildContext? context)
    {
        object? lastDetails = _controller!.LastDetails;
        Debug.Assert(lastDetails is DragStartDetails);
        new ScrollStartNotification(metrics, dragDetails: (DragStartDetails)lastDetails!, sourceContext: context)
            .Dispatch(context);
    }

    public override void DispatchScrollUpdateNotification(
        IScrollMetrics metrics,
        BuildContext context,
        double scrollDelta)
    {
        object? lastDetails = _controller!.LastDetails;
        Debug.Assert(lastDetails is DragUpdateDetails);
        new ScrollUpdateNotification(
            metrics,
            dragDetails: (DragUpdateDetails)lastDetails!,
            scrollDelta: scrollDelta,
            sourceContext: context).Dispatch(context);
    }

    public override void DispatchOverscrollNotification(
        IScrollMetrics metrics,
        BuildContext context,
        double overscroll)
    {
        object? lastDetails = _controller!.LastDetails;
        Debug.Assert(lastDetails is DragUpdateDetails);
        new OverscrollNotification(
            metrics,
            overscroll: overscroll,
            dragDetails: (DragUpdateDetails)lastDetails!,
            sourceContext: context).Dispatch(context);
    }

    public override void DispatchScrollEndNotification(IScrollMetrics metrics, BuildContext context)
    {
        // We might not have DragEndDetails yet if we're being called from beginActivity.
        object? lastDetails = _controller!.LastDetails;
        new ScrollEndNotification(
            metrics,
            dragDetails: lastDetails as DragEndDetails?,
            sourceContext: context).Dispatch(context);
    }

    public override bool ShouldIgnorePointer => _controller?.Kind != PointerDeviceKind.Trackpad;

    public override bool IsScrolling => true;

    // DragScrollActivity is not independently changing velocity yet
    // until the drag is ended.
    public override double Velocity => 0.0;

    public override void Dispose()
    {
        _controller = null;
        base.Dispose();
    }

    public override string ToString() => $"{Diagnostics.DescribeIdentity(this)}({_controller})";
}

/// <summary>
/// An activity that animates a scroll view based on a physics <see cref="Simulation"/>.
/// </summary>
/// <remarks>
/// A <see cref="BallisticScrollActivity"/> is typically used when the user lifts their finger off
/// the screen to continue the scrolling gesture with the current velocity.
/// <para>
/// <see cref="BallisticScrollActivity"/> is also used to restore a scroll view to a valid scroll
/// offset when the geometry of the scroll view changes. In these situations, the
/// <see cref="Simulation"/> typically starts with a zero velocity.
/// </para>
/// </remarks>
public class BallisticScrollActivity : ScrollActivity
{
    private readonly AnimationController _controller;

    public BallisticScrollActivity(
        IScrollActivityDelegate @delegate,
        Simulation simulation,
        ITickerProvider vsync,
        bool shouldIgnorePointer) : base(@delegate)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(vsync);
        ShouldIgnorePointer = shouldIgnorePointer;
        _controller = AnimationController.Unbounded(
            debugLabel: Constants.KDebugMode
                ? Diagnostics.ObjectRuntimeType(this, "BallisticScrollActivity")
                : null,
            vsync: vsync);
        _controller.AddListener(Tick);
        // Won't trigger if we dispose _controller first.
        _controller.AnimateWith(simulation).WhenComplete(End);
    }

    public override void ResetActivity() => Delegate.GoBallistic(Velocity);

    public override void ApplyNewDimensions() => Delegate.GoBallistic(Velocity);

    private void Tick()
    {
        if (!ApplyMoveTo(_controller.Value))
        {
            Delegate.GoIdle();
        }
    }

    /// <summary>
    /// Move the position to the given location.
    /// </summary>
    /// <remarks>
    /// If the new position was fully applied, returns true. If there was any overflow, returns
    /// false.
    /// <para>The default implementation calls <see cref="IScrollActivityDelegate.SetPixels"/>.</para>
    /// </remarks>
    protected virtual bool ApplyMoveTo(double value)
    {
        return Math.Abs(Delegate.SetPixels(value)) < Constants.PrecisionErrorTolerance;
    }

    private void End()
    {
        // Check if the activity was disposed before going ballistic because _end might be called
        // after the activity is disposed.
        if (!IsDisposed)
        {
            Delegate.GoBallistic(0.0);
        }
    }

    public override void DispatchOverscrollNotification(
        IScrollMetrics metrics,
        BuildContext context,
        double overscroll)
    {
        new OverscrollNotification(
            metrics,
            overscroll: overscroll,
            velocity: Velocity,
            sourceContext: context).Dispatch(context);
    }

    public override bool ShouldIgnorePointer { get; }

    public override bool IsScrolling => true;

    public override double Velocity => _controller.Velocity;

    public override void Dispose()
    {
        _controller.Dispose();
        base.Dispose();
    }

    public override string ToString() => $"{Diagnostics.DescribeIdentity(this)}({_controller})";
}

/// <summary>
/// An activity that animates a scroll view based on animation parameters.
/// </summary>
/// <remarks>
/// For example, a <see cref="DrivenScrollActivity"/> is used to implement
/// <see cref="ScrollPosition.AnimateTo"/>.
/// <para>
/// See also <see cref="BallisticScrollActivity"/>, which animates a scroll view based on a physics
/// <see cref="Simulation"/>.
/// </para>
/// </remarks>
public class DrivenScrollActivity : ScrollActivity
{
    private readonly TaskCompletionSource _completer =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly AnimationController _controller;

    public DrivenScrollActivity(
        IScrollActivityDelegate @delegate,
        double from,
        double to,
        TimeSpan duration,
        Curve curve,
        ITickerProvider vsync) : base(@delegate)
    {
        ArgumentNullException.ThrowIfNull(curve);
        ArgumentNullException.ThrowIfNull(vsync);
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        _controller = AnimationController.Unbounded(
            value: from,
            debugLabel: Diagnostics.ObjectRuntimeType(this, "DrivenScrollActivity"),
            vsync: vsync);
        _controller.AddListener(Tick);
        _controller.AnimateTo(to, duration, curve).WhenComplete(End);
    }

    private DrivenScrollActivity(
        IScrollActivityDelegate @delegate,
        Simulation simulation,
        ITickerProvider vsync) : base(@delegate)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(vsync);
        _controller = AnimationController.Unbounded(
            debugLabel: Diagnostics.ObjectRuntimeType(this, "DrivenScrollActivity"),
            vsync: vsync);
        _controller.AddListener(Tick);
        _controller.AnimateWith(simulation).WhenComplete(End);
    }

    /// <summary>
    /// Creates an activity that animates a scroll view based on a <paramref name="simulation"/>.
    /// </summary>
    /// <remarks>
    /// Dart's <c>DrivenScrollActivity.simulation</c>, which C# expresses as a static factory.
    /// </remarks>
    public static DrivenScrollActivity FromSimulation(
        IScrollActivityDelegate @delegate,
        Simulation simulation,
        ITickerProvider vsync)
    {
        return new DrivenScrollActivity(@delegate, simulation, vsync);
    }

    /// <summary>
    /// A <see cref="Task"/> that completes when the activity stops.
    /// </summary>
    /// <remarks>
    /// For example, this <see cref="Task"/> completes if the animation reaches the end or if the
    /// user interacts with the scroll view in way that causes the animation to stop before it
    /// reaches the end.
    /// </remarks>
    public Task Done => _completer.Task;

    private void Tick()
    {
        if (!ApplyMoveTo(_controller.Value))
        {
            Delegate.GoIdle();
        }
    }

    /// <summary>
    /// Move the position to the given location.
    /// </summary>
    /// <remarks>
    /// If the new position was fully applied, returns true. If there was any overflow, returns
    /// false.
    /// </remarks>
    protected virtual bool ApplyMoveTo(double value)
    {
        return Math.Abs(Delegate.SetPixels(value)) < Constants.PrecisionErrorTolerance;
    }

    private void End()
    {
        // Check if the activity was disposed before going ballistic because _end might be called
        // after the activity is disposed.
        if (!IsDisposed)
        {
            Delegate.GoBallistic(Velocity);
        }
    }

    public override void DispatchOverscrollNotification(
        IScrollMetrics metrics,
        BuildContext context,
        double overscroll)
    {
        new OverscrollNotification(
            metrics,
            overscroll: overscroll,
            velocity: Velocity,
            sourceContext: context).Dispatch(context);
    }

    public override bool ShouldIgnorePointer => true;

    public override bool IsScrolling => true;

    public override double Velocity => _controller.Velocity;

    public override void Dispose()
    {
        _completer.TrySetResult();
        _controller.Dispose();
        base.Dispose();
    }

    public override string ToString() => $"{Diagnostics.DescribeIdentity(this)}({_controller})";
}
