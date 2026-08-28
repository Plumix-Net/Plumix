using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/long_press.dart

namespace Plumix.Gestures;

/// <summary>Details for `GestureLongPressDownCallback`.</summary>
public readonly record struct LongPressDownDetails(
    Point GlobalPosition = default,
    Point LocalPosition = default,
    PointerDeviceKind? Kind = null) : IPositionedGestureDetails;

/// <summary>Details for `GestureLongPressStartCallback`.</summary>
public readonly record struct LongPressStartDetails(
    Point GlobalPosition = default,
    Point LocalPosition = default) : IPositionedGestureDetails;

/// <summary>Details for `GestureLongPressMoveUpdateCallback`.</summary>
public readonly record struct LongPressMoveUpdateDetails(
    Point GlobalPosition = default,
    Point LocalPosition = default,
    Point OffsetFromOrigin = default,
    Point LocalOffsetFromOrigin = default) : IPositionedGestureDetails;

/// <summary>Details for `GestureLongPressEndCallback`.</summary>
public readonly record struct LongPressEndDetails(
    Point GlobalPosition = default,
    Point LocalPosition = default,
    Velocity Velocity = default) : IPositionedGestureDetails;

/// <summary>
/// Recognizes when the user has pressed down at the same location for a long period of time.
/// Ports Dart's `LongPressGestureRecognizer`.
/// </summary>
public class LongPressGestureRecognizer : PrimaryPointerGestureRecognizer
{
    private bool _longPressAccepted;
    private OffsetPair? _longPressOrigin;
    private PointerButtons? _initialButtons;
    private VelocityTracker? _velocityTracker;

    /// <summary>
    /// Creates a long-press recognizer. <paramref name="duration"/> defaults to
    /// <see cref="GestureConstants.LongPressTimeout"/> (500 ms) and
    /// <paramref name="postAcceptSlopTolerance"/> defaults to null, so the pointer may drift any
    /// distance once the press has been recognized.
    /// </summary>
    public LongPressGestureRecognizer(
        TimeSpan? duration = null,
        double? postAcceptSlopTolerance = null,
        GestureBinding? binding = null)
        : base(
            deadline: duration ?? GestureConstants.LongPressTimeout,
            postAcceptSlopTolerance: postAcceptSlopTolerance,
            binding: binding)
    {
        AllowedButtonsFilter = DefaultButtonAcceptBehavior;
    }

    /// <summary>Called when a pointer that might cause a long press has contacted the screen.</summary>
    public Action<LongPressDownDetails>? OnLongPressDown { get; set; }

    /// <summary>
    /// Called when a pointer that previously triggered <see cref="OnLongPressDown"/> will not end
    /// up causing a long press.
    /// </summary>
    public Action? OnLongPressCancel { get; set; }

    /// <summary>Called when a long press gesture with a primary button has been recognized.</summary>
    public Action? OnLongPress { get; set; }

    /// <summary>Called when a long press gesture with a primary button has been recognized, with position.</summary>
    public Action<LongPressStartDetails>? OnLongPressStart { get; set; }

    /// <summary>Called when moving after the long press with a primary button is recognized.</summary>
    public Action<LongPressMoveUpdateDetails>? OnLongPressMoveUpdate { get; set; }

    /// <summary>Called when the pointer stops contacting the screen after a primary-button long press.</summary>
    public Action? OnLongPressUp { get; set; }

    /// <summary>
    /// Called when the pointer stops contacting the screen after a primary-button long press, with
    /// position and velocity.
    /// </summary>
    public Action<LongPressEndDetails>? OnLongPressEnd { get; set; }

    /// <summary>Called when a pointer that might cause a secondary long press has contacted the screen.</summary>
    public Action<LongPressDownDetails>? OnSecondaryLongPressDown { get; set; }

    /// <summary>Called when a secondary-button press will not end up causing a long press.</summary>
    public Action? OnSecondaryLongPressCancel { get; set; }

    /// <summary>Called when a long press gesture with a secondary button has been recognized.</summary>
    public Action? OnSecondaryLongPress { get; set; }

    /// <summary>Called when a secondary-button long press is recognized, with position.</summary>
    public Action<LongPressStartDetails>? OnSecondaryLongPressStart { get; set; }

    /// <summary>Called when moving after a secondary-button long press is recognized.</summary>
    public Action<LongPressMoveUpdateDetails>? OnSecondaryLongPressMoveUpdate { get; set; }

    /// <summary>Called when the pointer lifts after a secondary-button long press.</summary>
    public Action? OnSecondaryLongPressUp { get; set; }

    /// <summary>
    /// Called when the pointer lifts after a secondary-button long press, with position and velocity.
    /// </summary>
    public Action<LongPressEndDetails>? OnSecondaryLongPressEnd { get; set; }

    /// <summary>Called when a pointer that might cause a tertiary long press has contacted the screen.</summary>
    public Action<LongPressDownDetails>? OnTertiaryLongPressDown { get; set; }

    /// <summary>Called when a tertiary-button press will not end up causing a long press.</summary>
    public Action? OnTertiaryLongPressCancel { get; set; }

    /// <summary>Called when a long press gesture with a tertiary button has been recognized.</summary>
    public Action? OnTertiaryLongPress { get; set; }

    /// <summary>Called when a tertiary-button long press is recognized, with position.</summary>
    public Action<LongPressStartDetails>? OnTertiaryLongPressStart { get; set; }

    /// <summary>Called when moving after a tertiary-button long press is recognized.</summary>
    public Action<LongPressMoveUpdateDetails>? OnTertiaryLongPressMoveUpdate { get; set; }

    /// <summary>Called when the pointer lifts after a tertiary-button long press.</summary>
    public Action? OnTertiaryLongPressUp { get; set; }

    /// <summary>
    /// Called when the pointer lifts after a tertiary-button long press, with position and velocity.
    /// </summary>
    public Action<LongPressEndDetails>? OnTertiaryLongPressEnd { get; set; }

    public override string DebugDescription => "long press";

    protected override bool IsPointerAllowed(PointerDownEvent @event)
    {
        switch (@event.Buttons)
        {
            case PointerButtons.Primary:
                if (OnLongPressDown is null
                    && OnLongPressCancel is null
                    && OnLongPressStart is null
                    && OnLongPress is null
                    && OnLongPressMoveUpdate is null
                    && OnLongPressEnd is null
                    && OnLongPressUp is null)
                {
                    return false;
                }

                break;
            case PointerButtons.Secondary:
                if (OnSecondaryLongPressDown is null
                    && OnSecondaryLongPressCancel is null
                    && OnSecondaryLongPressStart is null
                    && OnSecondaryLongPress is null
                    && OnSecondaryLongPressMoveUpdate is null
                    && OnSecondaryLongPressEnd is null
                    && OnSecondaryLongPressUp is null)
                {
                    return false;
                }

                break;
            case PointerButtons.Middle:
                if (OnTertiaryLongPressDown is null
                    && OnTertiaryLongPressCancel is null
                    && OnTertiaryLongPressStart is null
                    && OnTertiaryLongPress is null
                    && OnTertiaryLongPressMoveUpdate is null
                    && OnTertiaryLongPressEnd is null
                    && OnTertiaryLongPressUp is null)
                {
                    return false;
                }

                break;
            default:
                return false;
        }

        return base.IsPointerAllowed(@event);
    }

    protected override void DidExceedDeadline()
    {
        // Exceeding the deadline puts the gesture in the accepted state.
        Resolve(GestureDisposition.Accepted);
        _longPressAccepted = true;
        base.AcceptGesture(PrimaryPointer!.Value);
        CheckLongPressStart();
    }

    protected override void HandlePrimaryPointer(PointerEvent @event)
    {
        if (!@event.Synthesized)
        {
            if (@event is PointerDownEvent)
            {
                _velocityTracker = new VelocityTracker(@event.Kind);
                _velocityTracker.AddPosition(@event.TimestampUtc, @event.LocalPosition);
            }

            if (@event is PointerMoveEvent)
            {
                _velocityTracker?.AddPosition(@event.TimestampUtc, @event.LocalPosition);
            }
        }

        if (@event is PointerUpEvent)
        {
            if (_longPressAccepted)
            {
                CheckLongPressEnd(@event);
            }
            else
            {
                // Pointer is lifted before the timeout.
                Resolve(GestureDisposition.Rejected);
            }

            Reset();
        }
        else if (@event is PointerCancelEvent)
        {
            CheckLongPressCancel();
            Reset();
        }
        else if (@event is PointerDownEvent)
        {
            // The first touch.
            _longPressOrigin = OffsetPair.FromEventPosition(@event);
            _initialButtons = @event.Buttons;
            CheckLongPressDown(@event);
        }
        else if (@event is PointerMoveEvent)
        {
            if (@event.Buttons != _initialButtons && !_longPressAccepted)
            {
                Resolve(GestureDisposition.Rejected);
                StopTrackingPointer(PrimaryPointer!.Value);
            }
            else if (_longPressAccepted)
            {
                CheckLongPressMoveUpdate(@event);
            }
        }
    }

    private void CheckLongPressDown(PointerEvent @event)
    {
        var details = new LongPressDownDetails(
            GlobalPosition: _longPressOrigin!.Value.Global,
            LocalPosition: _longPressOrigin!.Value.Local,
            Kind: GetKindForPointer(@event.Pointer));

        switch (_initialButtons)
        {
            case PointerButtons.Primary:
                if (OnLongPressDown is not null)
                {
                    InvokeCallback("onLongPressDown", () => OnLongPressDown!(details));
                }

                break;
            case PointerButtons.Secondary:
                if (OnSecondaryLongPressDown is not null)
                {
                    InvokeCallback("onSecondaryLongPressDown", () => OnSecondaryLongPressDown!(details));
                }

                break;
            case PointerButtons.Middle:
                if (OnTertiaryLongPressDown is not null)
                {
                    InvokeCallback("onTertiaryLongPressDown", () => OnTertiaryLongPressDown!(details));
                }

                break;
            default:
                throw new InvalidOperationException($"Unhandled button {_initialButtons}");
        }
    }

    private void CheckLongPressCancel()
    {
        if (State != GestureRecognizerState.Possible)
        {
            return;
        }

        switch (_initialButtons)
        {
            case PointerButtons.Primary:
                if (OnLongPressCancel is not null)
                {
                    InvokeCallback("onLongPressCancel", OnLongPressCancel);
                }

                break;
            case PointerButtons.Secondary:
                if (OnSecondaryLongPressCancel is not null)
                {
                    InvokeCallback("onSecondaryLongPressCancel", OnSecondaryLongPressCancel);
                }

                break;
            case PointerButtons.Middle:
                if (OnTertiaryLongPressCancel is not null)
                {
                    InvokeCallback("onTertiaryLongPressCancel", OnTertiaryLongPressCancel);
                }

                break;
            default:
                throw new InvalidOperationException($"Unhandled button {_initialButtons}");
        }
    }

    private void CheckLongPressStart()
    {
        switch (_initialButtons)
        {
            case PointerButtons.Primary:
                if (OnLongPressStart is not null)
                {
                    var details = new LongPressStartDetails(
                        GlobalPosition: _longPressOrigin!.Value.Global,
                        LocalPosition: _longPressOrigin!.Value.Local);
                    InvokeCallback("onLongPressStart", () => OnLongPressStart!(details));
                }

                if (OnLongPress is not null)
                {
                    InvokeCallback("onLongPress", OnLongPress);
                }

                break;
            case PointerButtons.Secondary:
                if (OnSecondaryLongPressStart is not null)
                {
                    var details = new LongPressStartDetails(
                        GlobalPosition: _longPressOrigin!.Value.Global,
                        LocalPosition: _longPressOrigin!.Value.Local);
                    InvokeCallback("onSecondaryLongPressStart", () => OnSecondaryLongPressStart!(details));
                }

                if (OnSecondaryLongPress is not null)
                {
                    InvokeCallback("onSecondaryLongPress", OnSecondaryLongPress);
                }

                break;
            case PointerButtons.Middle:
                if (OnTertiaryLongPressStart is not null)
                {
                    var details = new LongPressStartDetails(
                        GlobalPosition: _longPressOrigin!.Value.Global,
                        LocalPosition: _longPressOrigin!.Value.Local);
                    InvokeCallback("onTertiaryLongPressStart", () => OnTertiaryLongPressStart!(details));
                }

                if (OnTertiaryLongPress is not null)
                {
                    InvokeCallback("onTertiaryLongPress", OnTertiaryLongPress);
                }

                break;
            default:
                throw new InvalidOperationException($"Unhandled button {_initialButtons}");
        }
    }

    private void CheckLongPressMoveUpdate(PointerEvent @event)
    {
        var details = new LongPressMoveUpdateDetails(
            GlobalPosition: @event.Position,
            LocalPosition: @event.LocalPosition,
            OffsetFromOrigin: @event.Position - _longPressOrigin!.Value.Global,
            LocalOffsetFromOrigin: @event.LocalPosition - _longPressOrigin!.Value.Local);

        switch (_initialButtons)
        {
            case PointerButtons.Primary:
                if (OnLongPressMoveUpdate is not null)
                {
                    InvokeCallback("onLongPressMoveUpdate", () => OnLongPressMoveUpdate!(details));
                }

                break;
            case PointerButtons.Secondary:
                if (OnSecondaryLongPressMoveUpdate is not null)
                {
                    InvokeCallback(
                        "onSecondaryLongPressMoveUpdate",
                        () => OnSecondaryLongPressMoveUpdate!(details));
                }

                break;
            case PointerButtons.Middle:
                if (OnTertiaryLongPressMoveUpdate is not null)
                {
                    InvokeCallback(
                        "onTertiaryLongPressMoveUpdate",
                        () => OnTertiaryLongPressMoveUpdate!(details));
                }

                break;
            default:
                throw new InvalidOperationException($"Unhandled button {_initialButtons}");
        }
    }

    private void CheckLongPressEnd(PointerEvent @event)
    {
        VelocityEstimate? estimate = _velocityTracker!.GetVelocityEstimate();
        Velocity velocity = estimate is null ? Velocity.Zero : new Velocity(estimate.PixelsPerSecond);
        var details = new LongPressEndDetails(
            GlobalPosition: @event.Position,
            LocalPosition: @event.LocalPosition,
            Velocity: velocity);

        _velocityTracker = null;
        switch (_initialButtons)
        {
            case PointerButtons.Primary:
                if (OnLongPressEnd is not null)
                {
                    InvokeCallback("onLongPressEnd", () => OnLongPressEnd!(details));
                }

                if (OnLongPressUp is not null)
                {
                    InvokeCallback("onLongPressUp", OnLongPressUp);
                }

                break;
            case PointerButtons.Secondary:
                if (OnSecondaryLongPressEnd is not null)
                {
                    InvokeCallback("onSecondaryLongPressEnd", () => OnSecondaryLongPressEnd!(details));
                }

                if (OnSecondaryLongPressUp is not null)
                {
                    InvokeCallback("onSecondaryLongPressUp", OnSecondaryLongPressUp);
                }

                break;
            case PointerButtons.Middle:
                if (OnTertiaryLongPressEnd is not null)
                {
                    InvokeCallback("onTertiaryLongPressEnd", () => OnTertiaryLongPressEnd!(details));
                }

                if (OnTertiaryLongPressUp is not null)
                {
                    InvokeCallback("onTertiaryLongPressUp", OnTertiaryLongPressUp);
                }

                break;
            default:
                throw new InvalidOperationException($"Unhandled button {_initialButtons}");
        }
    }

    private void Reset()
    {
        _longPressAccepted = false;
        _longPressOrigin = null;
        _initialButtons = null;
        _velocityTracker = null;
    }

    public override void Resolve(GestureDisposition disposition)
    {
        if (disposition == GestureDisposition.Rejected)
        {
            if (_longPressAccepted)
            {
                // This can happen if the gesture has been canceled, for example when the buttons
                // have changed.
                Reset();
            }
            else
            {
                CheckLongPressCancel();
            }
        }

        base.Resolve(disposition);
    }

    public override void AcceptGesture(int pointer)
    {
        // Winning the arena isn't important here since it may happen from a sweep. Explicitly
        // exceeding the deadline puts the gesture in the accepted state.
    }

    /// <summary>
    /// Dart's `_defaultButtonAcceptBehavior`: exactly one of the primary, secondary or tertiary
    /// buttons, never a combination.
    /// </summary>
    private static bool DefaultButtonAcceptBehavior(PointerButtons buttons)
    {
        return buttons is PointerButtons.Primary or PointerButtons.Secondary or PointerButtons.Middle;
    }
}
