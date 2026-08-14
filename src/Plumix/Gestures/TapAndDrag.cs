using Avalonia;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/tap_and_drag.dart
// C# has no mixins, so Dart's private `_TapStatusTrackerMixin` is folded into
// `BaseTapAndDragGestureRecognizer`; every member the mixin contributes keeps its source name.

namespace Plumix.Gestures;

/// <summary>Dart's private `_DragState`.</summary>
internal enum TapDragState
{
    /// A pointer has not yet been tracked.
    Ready,

    /// A pointer has been tracked, but the drag distance has not been reached.
    Possible,

    /// The drag has been recognized.
    Accepted,
}

/// <summary>Details for [GestureTapDragDownCallback], such as the position of the pointer.</summary>
public sealed class TapDragDownDetails
{
    public TapDragDownDetails(
        Point globalPosition,
        Point localPosition,
        int consecutiveTapCount,
        PointerDeviceKind? kind = null)
    {
        GlobalPosition = globalPosition;
        LocalPosition = localPosition;
        ConsecutiveTapCount = consecutiveTapCount;
        Kind = kind;
    }

    public Point GlobalPosition { get; }

    public Point LocalPosition { get; }

    public PointerDeviceKind? Kind { get; }

    /// The number of consecutive taps before this drag began.
    public int ConsecutiveTapCount { get; }
}

/// <summary>Details for [GestureTapDragUpCallback].</summary>
public sealed class TapDragUpDetails
{
    public TapDragUpDetails(
        Point globalPosition,
        Point localPosition,
        PointerDeviceKind kind,
        int consecutiveTapCount)
    {
        GlobalPosition = globalPosition;
        LocalPosition = localPosition;
        Kind = kind;
        ConsecutiveTapCount = consecutiveTapCount;
    }

    public Point GlobalPosition { get; }

    public Point LocalPosition { get; }

    public PointerDeviceKind Kind { get; }

    public int ConsecutiveTapCount { get; }
}

/// <summary>Details for [GestureTapDragStartCallback].</summary>
public sealed class TapDragStartDetails
{
    public TapDragStartDetails(
        Point globalPosition,
        Point localPosition,
        int consecutiveTapCount,
        DateTime? sourceTimeStampUtc = null,
        PointerDeviceKind? kind = null)
    {
        GlobalPosition = globalPosition;
        LocalPosition = localPosition;
        ConsecutiveTapCount = consecutiveTapCount;
        SourceTimeStampUtc = sourceTimeStampUtc;
        Kind = kind;
    }

    public Point GlobalPosition { get; }

    public Point LocalPosition { get; }

    /// Recorded timestamp of the source pointer event that triggered the drag.
    public DateTime? SourceTimeStampUtc { get; }

    public PointerDeviceKind? Kind { get; }

    public int ConsecutiveTapCount { get; }
}

/// <summary>Details for [GestureTapDragUpdateCallback].</summary>
public sealed class TapDragUpdateDetails
{
    public TapDragUpdateDetails(
        Point globalPosition,
        Point localPosition,
        Point offsetFromOrigin,
        Point localOffsetFromOrigin,
        int consecutiveTapCount,
        DateTime? sourceTimeStampUtc = null,
        Point delta = default,
        double? primaryDelta = null,
        PointerDeviceKind? kind = null)
    {
        if (primaryDelta is { } primary
            && !(primary == delta.X && delta.Y == 0.0)
            && !(primary == delta.Y && delta.X == 0.0))
        {
            throw new ArgumentException(
                "PrimaryDelta must match one axis of Delta while the other axis is zero.",
                nameof(primaryDelta));
        }

        GlobalPosition = globalPosition;
        LocalPosition = localPosition;
        OffsetFromOrigin = offsetFromOrigin;
        LocalOffsetFromOrigin = localOffsetFromOrigin;
        ConsecutiveTapCount = consecutiveTapCount;
        SourceTimeStampUtc = sourceTimeStampUtc;
        Delta = delta;
        PrimaryDelta = primaryDelta;
        Kind = kind;
    }

    public Point GlobalPosition { get; }

    public Point LocalPosition { get; }

    public DateTime? SourceTimeStampUtc { get; }

    /// The amount the pointer has moved in the coordinate space of the event receiver since the
    /// previous update.
    public Point Delta { get; }

    /// The amount the pointer has moved along the primary axis, or null for a free-axis drag.
    public double? PrimaryDelta { get; }

    public PointerDeviceKind? Kind { get; }

    /// A delta offset from the point where the drag initially contacted the screen.
    public Point OffsetFromOrigin { get; }

    /// [OffsetFromOrigin] in the coordinate space of the event receiver.
    public Point LocalOffsetFromOrigin { get; }

    public int ConsecutiveTapCount { get; }
}

/// <summary>Details for [GestureTapDragEndCallback].</summary>
public sealed class TapDragEndDetails
{
    public TapDragEndDetails(
        int consecutiveTapCount,
        Point globalPosition = default,
        Point? localPosition = null,
        Velocity velocity = default,
        double? primaryVelocity = null)
    {
        if (primaryVelocity is { } primary
            && primary != velocity.PixelsPerSecond.X
            && primary != velocity.PixelsPerSecond.Y)
        {
            throw new ArgumentException(
                "PrimaryVelocity must match one axis of Velocity.",
                nameof(primaryVelocity));
        }

        GlobalPosition = globalPosition;
        LocalPosition = localPosition ?? globalPosition;
        Velocity = velocity;
        PrimaryVelocity = primaryVelocity;
        ConsecutiveTapCount = consecutiveTapCount;
    }

    public Point GlobalPosition { get; }

    public Point LocalPosition { get; }

    /// The velocity the pointer was moving when it stopped contacting the screen.
    public Velocity Velocity { get; }

    /// The velocity the pointer was moving along the primary axis when it stopped contacting the
    /// screen, or null for a free-axis drag.
    public double? PrimaryVelocity { get; }

    public int ConsecutiveTapCount { get; }
}

/// <summary>
/// A base class for gesture recognizers that recognize taps and drags, and that keep track of the
/// number of consecutive taps that started a gesture series.
/// </summary>
public abstract class BaseTapAndDragGestureRecognizer : OneSequenceGestureRecognizer
{
    // -- Tap-series tracking (Dart's private `_TapStatusTrackerMixin`) -------------------------
    private PointerDownEvent? _down;
    private PointerUpEvent? _up;
    private int _consecutiveTapCount;
    private OffsetPair? _originPosition;
    private PointerButtons? _previousButtons;
    private GestureTimer? _consecutiveTapTimer;
    private Point? _lastTapOffset;

    // -- Tap-and-drag state --------------------------------------------------------------------
    private readonly HashSet<int> _acceptedActivePointers = [];
    private readonly TimeSpan _deadline = GestureConstants.PressTimeout;
    private bool _pastSlopTolerance;
    private bool _sentTapDown;
    private bool _wonArenaForPrimaryPointer;
    private int? _primaryPointer;
    private GestureTimer? _deadlineTimer;
    private TapDragState _dragState = TapDragState.Ready;
    private PointerEvent? _start;
    private OffsetPair _initialPosition;
    private OffsetPair _currentPosition;
    private double _globalDistanceMoved;
    private double _globalDistanceMovedAllAxes;
    private TapDragUpdateDetails? _lastDragUpdateDetails;
    private GestureTimer? _dragUpdateThrottleTimer;

    protected BaseTapAndDragGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    /// The most recent pointer-down event this recognizer is tracking.
    public PointerDownEvent? CurrentDown => _down;

    /// The most recent pointer-up event this recognizer is tracking.
    public PointerUpEvent? CurrentUp => _up;

    /// The number of consecutive taps that the most recent gesture is part of.
    public int ConsecutiveTapCount => _consecutiveTapCount;

    /// The maximum number of consecutive taps before the counter restarts; null means unbounded.
    public int? MaxConsecutiveTap { get; set; }

    /// Called when a new tap series begins.
    public Action? OnTapTrackStart { get; set; }

    /// Called when the tap series is reset.
    public Action? OnTapTrackReset { get; set; }

    /// Determines whether the drag reports its start position at the pointer-down location or where
    /// the drag was first recognized.
    public DragStartBehavior DragStartBehavior { get; set; } = DragStartBehavior.Start;

    /// When non-null, drag updates are coalesced and delivered at this interval.
    public TimeSpan? DragUpdateThrottleFrequency { get; set; }

    /// Whether the recognizer declares victory as soon as it recognizes a drag.
    public bool EagerVictoryOnDrag { get; set; } = true;

    public Action<TapDragDownDetails>? OnTapDown { get; set; }

    public Action<TapDragUpDetails>? OnTapUp { get; set; }

    public Action<TapDragStartDetails>? OnDragStart { get; set; }

    public Action<TapDragUpdateDetails>? OnDragUpdate { get; set; }

    public Action<TapDragEndDetails>? OnDragEnd { get; set; }

    public Action? OnCancel { get; set; }

    public override string DebugDescription => "tap_and_drag";

    /// The accumulated primary-axis drag distance, signed by the direction of travel.
    protected double AccumulatedGlobalDistanceMoved => _globalDistanceMoved;

    /// The delta reported for this recognizer's axis.
    protected abstract Point GetDeltaForDetails(Point delta);

    /// The primary-axis component of the offset, or null for a free-axis drag.
    protected abstract double? GetPrimaryValueFromOffset(Point value);

    /// Whether the accumulated movement is far enough for this recognizer to claim a drag.
    protected abstract bool HasSufficientGlobalDistanceToAccept(PointerDeviceKind pointerDeviceKind);

    protected override bool IsPointerAllowed(PointerDownEvent @event)
    {
        if (_primaryPointer is null)
        {
            if (@event.Buttons != PointerButtons.Primary)
            {
                return false;
            }

            if (OnTapDown is null
                && OnDragStart is null
                && OnDragUpdate is null
                && OnDragEnd is null
                && OnTapUp is null
                && OnCancel is null)
            {
                return false;
            }
        }
        else if (@event.Pointer != _primaryPointer)
        {
            return false;
        }

        return base.IsPointerAllowed(@event);
    }

    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        if (_dragState != TapDragState.Ready)
        {
            return;
        }

        TrackTapStatusForNewPointer(@event);
        _primaryPointer = @event.Pointer;
        _globalDistanceMoved = 0.0;
        _globalDistanceMovedAllAxes = 0.0;
        _dragState = TapDragState.Possible;
        _initialPosition = new OffsetPair(Local: @event.LocalPosition, Global: @event.Position);
        _currentPosition = _initialPosition;
        _deadlineTimer = GestureTimer.Start(_deadline, DidExceedDeadline);
    }

    protected override void HandleNonAllowedPointer(PointerDownEvent @event)
    {
        // A secondary pointer with the primary button is silently dropped; any other button rejects
        // this recognizer, but only while it has not already won.
        if (@event.Buttons != PointerButtons.Primary && !_wonArenaForPrimaryPointer)
        {
            base.HandleNonAllowedPointer(@event);
        }
    }

    public override void AcceptGesture(int pointer)
    {
        if (pointer != _primaryPointer)
        {
            return;
        }

        StopDeadlineTimer();
        _acceptedActivePointers.Add(pointer);

        // Called when this recognizer is accepted by the arena, which can happen before or after the
        // pointer is released. `onTapDown` therefore always runs first.
        if (CurrentDown is { } down)
        {
            CheckTapDown(down);
        }

        _wonArenaForPrimaryPointer = true;

        if (_start is { } start && EagerVictoryOnDrag)
        {
            AcceptDrag(start);
        }

        if (_start is { } lateStart && !EagerVictoryOnDrag)
        {
            _dragState = TapDragState.Accepted;
            AcceptDrag(lateStart);
        }

        if (CurrentUp is { } up)
        {
            CheckTapUp(up);
        }
    }

    public override void RejectGesture(int pointer)
    {
        if (pointer != _primaryPointer)
        {
            return;
        }

        TapTrackerReset();
        StopDeadlineTimer();
        GiveUpPointer(pointer);
        ResetTaps();
        ResetDragUpdateThrottle();
    }

    protected override void DidStopTrackingLastPointer(int pointer)
    {
        switch (_dragState)
        {
            case TapDragState.Ready:
                CheckCancel();
                Resolve(GestureDisposition.Rejected);
                break;
            case TapDragState.Possible:
                if (_pastSlopTolerance)
                {
                    // A tap that drifted past the hit slop but never reached the drag distance is
                    // reported as a drag, so the gesture is not silently dropped.
                    if (_wonArenaForPrimaryPointer)
                    {
                        if (CurrentDown is { } down)
                        {
                            if (!_acceptedActivePointers.Remove(pointer))
                            {
                                ResolvePointer(pointer, GestureDisposition.Rejected);
                            }

                            _dragState = TapDragState.Accepted;
                            AcceptDrag(down);
                            CheckDragEnd();
                        }
                    }
                    else
                    {
                        CheckCancel();
                        Resolve(GestureDisposition.Rejected);
                    }
                }
                else if (CurrentUp is { } up)
                {
                    CheckTapUp(up);
                }

                break;
            case TapDragState.Accepted:
                CheckDragEnd();
                break;
        }

        StopDeadlineTimer();
        _start = null;
        _dragState = TapDragState.Ready;
        _pastSlopTolerance = false;
    }

    protected override void HandleEvent(PointerEvent @event)
    {
        if (@event.Pointer != _primaryPointer)
        {
            return;
        }

        TrackTapStatusForEvent(@event);

        switch (@event)
        {
            case PointerMoveEvent move:
            {
                double computedSlop = PointerEventUtils.ComputeHitSlop(move.Kind, GestureSettings);
                _pastSlopTolerance = _pastSlopTolerance
                                     || GetGlobalDistance(move, _initialPosition) > computedSlop;

                if (_dragState == TapDragState.Accepted)
                {
                    _currentPosition = OffsetPair.FromEventPosition(move);
                    CheckDragUpdate(move);
                }
                else if (_dragState == TapDragState.Possible)
                {
                    if (_start is null)
                    {
                        CheckDrag(move);
                    }

                    if (_start is { } start && _wonArenaForPrimaryPointer)
                    {
                        _dragState = TapDragState.Accepted;
                        AcceptDrag(start);
                    }
                }

                break;
            }
            case PointerUpEvent up:
            {
                if (_dragState == TapDragState.Possible)
                {
                    StopTrackingIfPointerNoLongerDown(up);
                }
                else if (_dragState == TapDragState.Accepted)
                {
                    GiveUpPointer(up.Pointer);
                }

                break;
            }
            case PointerCancelEvent cancel:
            {
                _dragState = TapDragState.Ready;
                GiveUpPointer(cancel.Pointer);
                break;
            }
        }
    }

    public override void Dispose()
    {
        StopDeadlineTimer();
        ResetDragUpdateThrottle();
        TapTrackerReset();
        base.Dispose();
    }

    // -- Tap-series tracking ---------------------------------------------------------------------

    private void TrackTapStatusForNewPointer(PointerDownEvent @event)
    {
        base.AddAllowedPointer(@event);

        // The timer's callback is deliberately empty in Dart: a series that timed out is only reset
        // once the next pointer goes down, so that `consecutiveTapCount` survives until then.
        if (_consecutiveTapTimer is { IsActive: false })
        {
            TapTrackerReset();
        }

        if (MaxConsecutiveTap == _consecutiveTapCount)
        {
            TapTrackerReset();
        }

        _up = null;
        if (_down is not null && !RepresentsSameSeries(@event))
        {
            _consecutiveTapCount = 1;
        }
        else
        {
            _consecutiveTapCount += 1;
        }

        ConsecutiveTapTimerStop();

        // `_down` must be assigned here rather than in `HandleEvent`, because the arena can accept
        // this recognizer before the down event is routed to it.
        _down = @event;
        _previousButtons = @event.Buttons;
        _lastTapOffset = @event.Position;
        _originPosition = new OffsetPair(Local: @event.LocalPosition, Global: @event.Position);
        OnTapTrackStart?.Invoke();
    }

    private void TrackTapStatusForEvent(PointerEvent @event)
    {
        switch (@event)
        {
            case PointerMoveEvent move:
            {
                double computedSlop = PointerEventUtils.ComputeHitSlop(move.Kind, GestureSettings);
                if (GetGlobalDistance(move, _originPosition) > computedSlop)
                {
                    ConsecutiveTapTimerStop();
                    _previousButtons = null;
                    _lastTapOffset = null;
                }

                break;
            }
            case PointerUpEvent up:
            {
                _up = up;
                if (_down is not null)
                {
                    ConsecutiveTapTimerStop();
                    ConsecutiveTapTimerStart();
                }

                break;
            }
            case PointerCancelEvent:
            {
                TapTrackerReset();
                break;
            }
        }
    }

    private bool RepresentsSameSeries(PointerDownEvent @event)
    {
        return _consecutiveTapTimer is not null
               && IsWithinConsecutiveTapTolerance(@event.Position)
               && HasSameButton(@event.Buttons);
    }

    private bool HasSameButton(PointerButtons buttons) => buttons == _previousButtons;

    private bool IsWithinConsecutiveTapTolerance(Point secondTapOffset)
    {
        return _lastTapOffset is { } lastTapOffset
               && (secondTapOffset - lastTapOffset).Distance() <= GestureConstants.DoubleTapSlop;
    }

    private void ConsecutiveTapTimerStart()
    {
        _consecutiveTapTimer ??= GestureTimer.Start(GestureConstants.DoubleTapTimeout, () => { });
    }

    private void ConsecutiveTapTimerStop()
    {
        _consecutiveTapTimer?.Cancel();
        _consecutiveTapTimer = null;
    }

    private void TapTrackerReset()
    {
        ConsecutiveTapTimerStop();
        _previousButtons = null;
        _originPosition = null;
        _lastTapOffset = null;
        _consecutiveTapCount = 0;
        _down = null;
        _up = null;
        OnTapTrackReset?.Invoke();
    }

    // -- Drag recognition ------------------------------------------------------------------------

    private void CheckDrag(PointerMoveEvent @event)
    {
        // Plumix pointer events carry no per-event transform, so the delta transform is the
        // identity; see `PointerEventUtils.TransformDeltaViaPositions`.
        Point movedLocally = GetDeltaForDetails(@event.LocalDelta);
        _globalDistanceMoved += PointerEventUtils.TransformDeltaViaPositions(
                untransformedEndPosition: @event.LocalPosition,
                untransformedDelta: movedLocally,
                transform: null)
            .Distance() * Math.Sign(GetPrimaryValueFromOffset(movedLocally) ?? 1);
        _globalDistanceMovedAllAxes += PointerEventUtils.TransformDeltaViaPositions(
                untransformedEndPosition: @event.LocalPosition,
                untransformedDelta: @event.LocalDelta,
                transform: null)
            .Distance();

        if (HasSufficientGlobalDistanceToAccept(@event.Kind)
            || (_wonArenaForPrimaryPointer
                && Math.Abs(_globalDistanceMovedAllAxes)
                > PointerEventUtils.ComputePanSlop(@event.Kind, GestureSettings)))
        {
            _start = @event;
            if (EagerVictoryOnDrag)
            {
                _dragState = TapDragState.Accepted;
                if (!_wonArenaForPrimaryPointer)
                {
                    Resolve(GestureDisposition.Accepted);
                }
            }
        }
    }

    private void AcceptDrag(PointerEvent @event)
    {
        if (!_wonArenaForPrimaryPointer)
        {
            return;
        }

        if (DragStartBehavior == DragStartBehavior.Start)
        {
            _initialPosition += new OffsetPair(Local: @event.LocalDelta, Global: @event.Delta);
            _currentPosition = _initialPosition;
        }

        CheckDragStart(@event);
        Point localDelta = @event.LocalDelta;
        if (localDelta != default)
        {
            _currentPosition = OffsetPair.FromEventPosition(@event);
            Point correctedLocalPosition = _initialPosition.Local + localDelta;
            Point globalUpdateDelta = PointerEventUtils.TransformDeltaViaPositions(
                untransformedEndPosition: correctedLocalPosition,
                untransformedDelta: localDelta,
                transform: null);
            var updateDelta = new OffsetPair(Local: localDelta, Global: globalUpdateDelta);
            CheckDragUpdate(@event, corrected: _initialPosition + updateDelta);
        }
    }

    private void CheckTapDown(PointerDownEvent @event)
    {
        if (_sentTapDown)
        {
            return;
        }

        if (OnTapDown is { } onTapDown)
        {
            var details = new TapDragDownDetails(
                globalPosition: @event.Position,
                localPosition: @event.LocalPosition,
                consecutiveTapCount: ConsecutiveTapCount,
                kind: GetKindForPointer(@event.Pointer));
            InvokeCallback(nameof(OnTapDown), () => onTapDown(details));
        }

        _sentTapDown = true;
    }

    private void CheckTapUp(PointerUpEvent @event)
    {
        if (!_wonArenaForPrimaryPointer)
        {
            return;
        }

        if (OnTapUp is { } onTapUp)
        {
            var details = new TapDragUpDetails(
                globalPosition: @event.Position,
                localPosition: @event.LocalPosition,
                kind: @event.Kind,
                consecutiveTapCount: ConsecutiveTapCount);
            InvokeCallback(nameof(OnTapUp), () => onTapUp(details));
        }

        ResetTaps();
        if (!_acceptedActivePointers.Remove(@event.Pointer))
        {
            ResolvePointer(@event.Pointer, GestureDisposition.Rejected);
        }
    }

    private void CheckDragStart(PointerEvent @event)
    {
        if (OnDragStart is { } onDragStart)
        {
            var details = new TapDragStartDetails(
                globalPosition: _initialPosition.Global,
                localPosition: _initialPosition.Local,
                consecutiveTapCount: ConsecutiveTapCount,
                sourceTimeStampUtc: @event.TimestampUtc,
                kind: GetKindForPointer(@event.Pointer));
            InvokeCallback(nameof(OnDragStart), () => onDragStart(details));
        }

        _start = null;
    }

    private void CheckDragUpdate(PointerEvent @event, OffsetPair? corrected = null)
    {
        Point globalPosition = corrected?.Global ?? @event.Position;
        Point localPosition = corrected?.Local ?? @event.LocalPosition;
        var details = new TapDragUpdateDetails(
            globalPosition: globalPosition,
            localPosition: localPosition,
            offsetFromOrigin: globalPosition - _initialPosition.Global,
            localOffsetFromOrigin: localPosition - _initialPosition.Local,
            consecutiveTapCount: ConsecutiveTapCount,
            sourceTimeStampUtc: @event.TimestampUtc,
            delta: @event.LocalDelta,
            kind: GetKindForPointer(@event.Pointer));

        if (DragUpdateThrottleFrequency is { } throttle)
        {
            _lastDragUpdateDetails = details;
            _dragUpdateThrottleTimer ??= GestureTimer.Start(throttle, HandleDragUpdateThrottled);
            return;
        }

        if (OnDragUpdate is { } onDragUpdate)
        {
            InvokeCallback(nameof(OnDragUpdate), () => onDragUpdate(details));
        }
    }

    private void HandleDragUpdateThrottled()
    {
        if (_lastDragUpdateDetails is { } details && OnDragUpdate is { } onDragUpdate)
        {
            InvokeCallback(nameof(OnDragUpdate), () => onDragUpdate(details));
        }

        _dragUpdateThrottleTimer = null;
        _lastDragUpdateDetails = null;
    }

    private void CheckDragEnd()
    {
        if (_dragUpdateThrottleTimer is not null)
        {
            // A pending drag update is delivered before the end so the sequence stays ordered.
            _dragUpdateThrottleTimer.Cancel();
            HandleDragUpdateThrottled();
        }

        if (OnDragEnd is { } onDragEnd)
        {
            var details = new TapDragEndDetails(
                consecutiveTapCount: ConsecutiveTapCount,
                globalPosition: _currentPosition.Global,
                localPosition: _currentPosition.Local,
                primaryVelocity: 0.0);
            InvokeCallback(nameof(OnDragEnd), () => onDragEnd(details));
        }

        ResetTaps();
        ResetDragUpdateThrottle();
    }

    private void CheckCancel()
    {
        if (!_sentTapDown)
        {
            // `onTapDown` never ran, so there is nothing to cancel.
            return;
        }

        if (OnCancel is { } onCancel)
        {
            InvokeCallback(nameof(OnCancel), onCancel);
        }

        ResetDragUpdateThrottle();
        ResetTaps();
    }

    private void DidExceedDeadline()
    {
        if (CurrentDown is not { } down)
        {
            return;
        }

        CheckTapDown(down);
        if (ConsecutiveTapCount > 1)
        {
            // A double tap or more claims the gesture immediately, so a competing long press cannot
            // win while the pointer is held.
            Resolve(GestureDisposition.Accepted);
        }
    }

    private void GiveUpPointer(int pointer)
    {
        StopTrackingPointer(pointer);
        if (!_acceptedActivePointers.Remove(pointer))
        {
            ResolvePointer(pointer, GestureDisposition.Rejected);
        }
    }

    private void ResetTaps()
    {
        _sentTapDown = false;
        _wonArenaForPrimaryPointer = false;
        _primaryPointer = null;
    }

    private void ResetDragUpdateThrottle()
    {
        if (DragUpdateThrottleFrequency is null)
        {
            return;
        }

        _lastDragUpdateDetails = null;
        _dragUpdateThrottleTimer?.Cancel();
        _dragUpdateThrottleTimer = null;
    }

    private void StopDeadlineTimer()
    {
        _deadlineTimer?.Cancel();
        _deadlineTimer = null;
    }

    private static double GetGlobalDistance(PointerEvent @event, OffsetPair? originPosition)
    {
        return originPosition is { } origin ? (@event.Position - origin.Global).Distance() : 0.0;
    }
}

/// <summary>
/// Recognizes taps along with movement in the horizontal direction.
/// </summary>
public class TapAndHorizontalDragGestureRecognizer : BaseTapAndDragGestureRecognizer
{
    public TapAndHorizontalDragGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    public override string DebugDescription => "tap and horizontal drag";

    protected override bool HasSufficientGlobalDistanceToAccept(PointerDeviceKind pointerDeviceKind)
    {
        return Math.Abs(AccumulatedGlobalDistanceMoved)
               > PointerEventUtils.ComputeHitSlop(pointerDeviceKind, GestureSettings);
    }

    protected override Point GetDeltaForDetails(Point delta) => new(delta.X, 0.0);

    protected override double? GetPrimaryValueFromOffset(Point value) => value.X;
}

/// <summary>
/// Recognizes taps along with both horizontal and vertical movement.
/// </summary>
public class TapAndPanGestureRecognizer : BaseTapAndDragGestureRecognizer
{
    public TapAndPanGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    public override string DebugDescription => "tap and pan";

    protected override bool HasSufficientGlobalDistanceToAccept(PointerDeviceKind pointerDeviceKind)
    {
        return Math.Abs(AccumulatedGlobalDistanceMoved)
               > PointerEventUtils.ComputePanSlop(pointerDeviceKind, GestureSettings);
    }

    protected override Point GetDeltaForDetails(Point delta) => delta;

    protected override double? GetPrimaryValueFromOffset(Point value) => null;
}

/// <summary>
/// Recognizes taps along with both horizontal and vertical movement.
/// </summary>
[Obsolete("Use TapAndPanGestureRecognizer instead. TapAndPanGestureRecognizer works exactly the same "
          + "but has a more disambiguated name from BaseTapAndDragGestureRecognizer. "
          + "This feature was deprecated after v3.9.0-19.0.pre.")]
public sealed class TapAndDragGestureRecognizer : BaseTapAndDragGestureRecognizer
{
    public TapAndDragGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    public override string DebugDescription => "tap and pan";

    protected override bool HasSufficientGlobalDistanceToAccept(PointerDeviceKind pointerDeviceKind)
    {
        return Math.Abs(AccumulatedGlobalDistanceMoved)
               > PointerEventUtils.ComputePanSlop(pointerDeviceKind, GestureSettings);
    }

    protected override Point GetDeltaForDetails(Point delta) => delta;

    protected override double? GetPrimaryValueFromOffset(Point value) => null;
}
