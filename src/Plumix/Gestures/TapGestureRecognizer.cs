using Avalonia;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/tap.dart

namespace Plumix.Gestures;

/// <summary>Details for `GestureTapDownCallback`, such as the position of the tap.</summary>
public sealed class TapDownDetails : IPositionedGestureDetails
{
    public TapDownDetails(
        Point globalPosition = default,
        Point? localPosition = null,
        PointerDeviceKind? kind = null)
    {
        GlobalPosition = globalPosition;
        LocalPosition = localPosition ?? globalPosition;
        Kind = kind;
    }

    public Point GlobalPosition { get; }

    public Point LocalPosition { get; }

    /// <summary>The kind of the device that initiated the event.</summary>
    public PointerDeviceKind? Kind { get; }
}

/// <summary>Details for `GestureTapUpCallback`, such as the position of the tap.</summary>
public sealed class TapUpDetails : IPositionedGestureDetails
{
    public TapUpDetails(
        PointerDeviceKind kind,
        Point globalPosition = default,
        Point? localPosition = null)
    {
        Kind = kind;
        GlobalPosition = globalPosition;
        LocalPosition = localPosition ?? globalPosition;
    }

    public Point GlobalPosition { get; }

    public Point LocalPosition { get; }

    /// <summary>The kind of the device that initiated the event. Required and non-null in Dart.</summary>
    public PointerDeviceKind Kind { get; }
}

/// <summary>Details for `GestureTapMoveCallback`, such as the new position of the pointer.</summary>
public sealed class TapMoveDetails
{
    public TapMoveDetails(
        PointerDeviceKind kind,
        Point globalPosition = default,
        Point delta = default,
        Point? localPosition = null)
    {
        Kind = kind;
        GlobalPosition = globalPosition;
        Delta = delta;
        LocalPosition = localPosition ?? globalPosition;
    }

    public Point GlobalPosition { get; }

    public Point LocalPosition { get; }

    /// <summary>The kind of the device that initiated the event.</summary>
    public PointerDeviceKind Kind { get; }

    /// <summary>
    /// The amount the pointer has moved in the coordinate space of the event receiver since the
    /// previous update.
    /// </summary>
    public Point Delta { get; }
}

/// <summary>
/// A base class for gesture recognizers that recognize taps.
/// Ports Dart's `BaseTapGestureRecognizer`.
/// </summary>
public abstract class BaseTapGestureRecognizer : PrimaryPointerGestureRecognizer
{
    private bool _sentTapDown;
    private bool _wonArenaForPrimaryPointer;
    private PointerDownEvent? _down;
    private PointerUpEvent? _up;

    protected BaseTapGestureRecognizer(
        double? preAcceptSlopTolerance = UnsetTouchSlop,
        double? postAcceptSlopTolerance = UnsetTouchSlop,
        GestureBinding? binding = null) : base(
        deadline: GestureConstants.PressTimeout,
        preAcceptSlopTolerance: preAcceptSlopTolerance,
        postAcceptSlopTolerance: postAcceptSlopTolerance,
        binding: binding)
    {
    }

    /// <summary>A pointer has contacted the screen, which might be the start of a tap.</summary>
    protected abstract void HandleTapDown(PointerDownEvent down);

    /// <summary>A pointer has stopped contacting the screen, ending a tap.</summary>
    protected abstract void HandleTapUp(PointerDownEvent down, PointerUpEvent up);

    /// <summary>A pointer in a tap sequence has moved. Empty by default, exactly like Dart.</summary>
    protected virtual void HandleTapMove(PointerMoveEvent move)
    {
    }

    /// <summary>
    /// A pointer that previously triggered <see cref="HandleTapDown"/> will not complete a tap.
    /// <paramref name="reason"/> is Dart's `reason`: <c>""</c> for a pointer cancel event,
    /// <c>"spontaneous"</c> for a self-rejection after winning, <c>"forced"</c> when another
    /// arena member won.
    /// </summary>
    protected abstract void HandleTapCancel(PointerDownEvent down, PointerCancelEvent? cancel, string reason);

    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        if (State == GestureRecognizerState.Ready)
        {
            // If the recognizer is ready but a down and an up are still stored, the previous arena
            // was never resolved: a new pointer restarts the recognizer.
            if (_down is not null && _up is not null)
            {
                Reset();
            }

            // `_down` must be assigned here instead of `HandlePrimaryPointer`, because
            // `AcceptGesture` can be called before any events are routed and needs the down event.
            _down = @event;
        }

        if (_down is not null)
        {
            // A pointer that arrives while the recognizer is rejected-but-tracking is ignored.
            base.AddAllowedPointer(@event);
        }
    }

    protected override void StartTrackingPointer(int pointer, Matrix4? transform = null)
    {
        // The recognizer should never track any pointers when `_down` is null, because calling
        // `_checkDown` in that situation will throw.
        if (_down is null)
        {
            throw new InvalidOperationException(
                "A tap recognizer cannot start tracking a pointer before it stores the down event.");
        }

        base.StartTrackingPointer(pointer, transform);
    }

    protected override void HandlePrimaryPointer(PointerEvent @event)
    {
        switch (@event)
        {
            case PointerUpEvent up:
                _up = up;
                CheckUp();
                break;
            case PointerCancelEvent cancel:
                Resolve(GestureDisposition.Rejected);
                if (_sentTapDown)
                {
                    CheckCancel(cancel, "");
                }

                Reset();
                break;
            default:
                if (@event.Buttons != _down!.Buttons)
                {
                    Resolve(GestureDisposition.Rejected);
                    StopTrackingPointer(PrimaryPointer!.Value);
                }
                else if (@event is PointerMoveEvent move)
                {
                    CheckMove(move);
                }

                break;
        }
    }

    public override void Resolve(GestureDisposition disposition)
    {
        if (_wonArenaForPrimaryPointer && disposition == GestureDisposition.Rejected)
        {
            // This can happen when the gesture has been canceled. For example, when the pointer
            // has exceeded the touch slop, the buttons have been changed, or if the recognizer is
            // disposed.
            CheckCancel(null, "spontaneous");
            Reset();
        }

        base.Resolve(disposition);
    }

    protected override void DidExceedDeadline()
    {
        CheckDown();
    }

    public override void AcceptGesture(int pointer)
    {
        base.AcceptGesture(pointer);
        if (pointer == PrimaryPointer)
        {
            CheckDown();
            _wonArenaForPrimaryPointer = true;
            CheckUp();
        }
    }

    public override void RejectGesture(int pointer)
    {
        base.RejectGesture(pointer);
        if (pointer == PrimaryPointer)
        {
            if (_sentTapDown)
            {
                CheckCancel(null, "forced");
            }

            Reset();
        }
    }

    private void CheckDown()
    {
        if (_sentTapDown)
        {
            return;
        }

        HandleTapDown(down: _down!);
        _sentTapDown = true;
    }

    private void CheckUp()
    {
        if (!_wonArenaForPrimaryPointer || _up is null)
        {
            return;
        }

        HandleTapUp(down: _down!, up: _up);
        Reset();
    }

    private void CheckCancel(PointerCancelEvent? @event, string note)
    {
        HandleTapCancel(down: _down!, cancel: @event, reason: note);
    }

    private void CheckMove(PointerMoveEvent @event)
    {
        HandleTapMove(move: @event);
    }

    private void Reset()
    {
        _sentTapDown = false;
        _wonArenaForPrimaryPointer = false;
        _up = null;
        _down = null;
    }

    public override string DebugDescription => "base tap";
}

/// <summary>
/// Recognizes taps: pointer events that come into and go out of contact with the screen without
/// moving. Ports Dart's `TapGestureRecognizer`.
/// </summary>
public class TapGestureRecognizer : BaseTapGestureRecognizer
{
    public TapGestureRecognizer(
        double? preAcceptSlopTolerance = UnsetTouchSlop,
        double? postAcceptSlopTolerance = UnsetTouchSlop,
        GestureBinding? binding = null) : base(
        preAcceptSlopTolerance: preAcceptSlopTolerance,
        postAcceptSlopTolerance: postAcceptSlopTolerance,
        binding: binding)
    {
    }

    /// <summary>A pointer that might cause a tap with a primary button has contacted the screen.</summary>
    public Action<TapDownDetails>? OnTapDown { get; set; }

    /// <summary>A pointer that will trigger a tap with a primary button has stopped contacting the screen.</summary>
    public Action<TapUpDetails>? OnTapUp { get; set; }

    /// <summary>A tap with a primary button has occurred.</summary>
    public Action? OnTap { get; set; }

    /// <summary>A pointer that triggered a tap with a primary button has moved.</summary>
    public Action<TapMoveDetails>? OnTapMove { get; set; }

    /// <summary>The pointer that previously triggered <see cref="OnTapDown"/> will not end up causing a tap.</summary>
    public Action? OnTapCancel { get; set; }

    /// <summary>A tap with a secondary button has occurred.</summary>
    public Action? OnSecondaryTap { get; set; }

    /// <summary>A pointer that might cause a tap with a secondary button has contacted the screen.</summary>
    public Action<TapDownDetails>? OnSecondaryTapDown { get; set; }

    /// <summary>A pointer that will trigger a tap with a secondary button has stopped contacting the screen.</summary>
    public Action<TapUpDetails>? OnSecondaryTapUp { get; set; }

    /// <summary>The pointer that previously triggered <see cref="OnSecondaryTapDown"/> will not cause a tap.</summary>
    public Action? OnSecondaryTapCancel { get; set; }

    /// <summary>A pointer that might cause a tap with a tertiary button has contacted the screen.</summary>
    public Action<TapDownDetails>? OnTertiaryTapDown { get; set; }

    /// <summary>A pointer that will trigger a tap with a tertiary button has stopped contacting the screen.</summary>
    public Action<TapUpDetails>? OnTertiaryTapUp { get; set; }

    /// <summary>The pointer that previously triggered <see cref="OnTertiaryTapDown"/> will not cause a tap.</summary>
    public Action? OnTertiaryTapCancel { get; set; }

    protected override bool IsPointerAllowed(PointerDownEvent @event)
    {
        switch (@event.Buttons)
        {
            case PointerButtons.Primary:
                if (OnTapDown is null
                    && OnTap is null
                    && OnTapUp is null
                    && OnTapCancel is null
                    && OnTapMove is null)
                {
                    return false;
                }

                break;
            case PointerButtons.Secondary:
                if (OnSecondaryTap is null
                    && OnSecondaryTapDown is null
                    && OnSecondaryTapUp is null
                    && OnSecondaryTapCancel is null)
                {
                    return false;
                }

                break;
            case PointerButtons.Middle:
                if (OnTertiaryTapDown is null
                    && OnTertiaryTapUp is null
                    && OnTertiaryTapCancel is null)
                {
                    return false;
                }

                break;
            default:
                return false;
        }

        return base.IsPointerAllowed(@event);
    }

    protected override void HandleTapDown(PointerDownEvent down)
    {
        var details = new TapDownDetails(
            globalPosition: down.Position,
            localPosition: down.LocalPosition,
            kind: GetKindForPointer(down.Pointer));
        switch (down.Buttons)
        {
            case PointerButtons.Primary:
                if (OnTapDown is { } onTapDown)
                {
                    InvokeCallback("onTapDown", () => onTapDown(details));
                }

                break;
            case PointerButtons.Secondary:
                if (OnSecondaryTapDown is { } onSecondaryTapDown)
                {
                    InvokeCallback("onSecondaryTapDown", () => onSecondaryTapDown(details));
                }

                break;
            case PointerButtons.Middle:
                if (OnTertiaryTapDown is { } onTertiaryTapDown)
                {
                    InvokeCallback("onTertiaryTapDown", () => onTertiaryTapDown(details));
                }

                break;
        }
    }

    protected override void HandleTapUp(PointerDownEvent down, PointerUpEvent up)
    {
        var details = new TapUpDetails(
            kind: up.Kind,
            globalPosition: up.Position,
            localPosition: up.LocalPosition);
        switch (down.Buttons)
        {
            case PointerButtons.Primary:
                if (OnTapUp is { } onTapUp)
                {
                    InvokeCallback("onTapUp", () => onTapUp(details));
                }

                if (OnTap is { } onTap)
                {
                    InvokeCallback("onTap", onTap);
                }

                break;
            case PointerButtons.Secondary:
                if (OnSecondaryTapUp is { } onSecondaryTapUp)
                {
                    InvokeCallback("onSecondaryTapUp", () => onSecondaryTapUp(details));
                }

                if (OnSecondaryTap is { } onSecondaryTap)
                {
                    InvokeCallback("onSecondaryTap", onSecondaryTap);
                }

                break;
            case PointerButtons.Middle:
                if (OnTertiaryTapUp is { } onTertiaryTapUp)
                {
                    InvokeCallback("onTertiaryTapUp", () => onTertiaryTapUp(details));
                }

                break;
        }
    }

    protected override void HandleTapMove(PointerMoveEvent move)
    {
        if (OnTapMove is { } onTapMove && move.Buttons == PointerButtons.Primary)
        {
            var details = new TapMoveDetails(
                globalPosition: move.Position,
                localPosition: move.LocalPosition,
                kind: GetKindForPointer(move.Pointer),
                delta: move.Delta);
            InvokeCallback("onTapMove", () => onTapMove(details));
        }
    }

    protected override void HandleTapCancel(PointerDownEvent down, PointerCancelEvent? cancel, string reason)
    {
        string note = reason == "" ? reason : reason + " ";
        switch (down.Buttons)
        {
            case PointerButtons.Primary:
                if (OnTapCancel is { } onTapCancel)
                {
                    InvokeCallback(note + "onTapCancel", onTapCancel);
                }

                break;
            case PointerButtons.Secondary:
                if (OnSecondaryTapCancel is { } onSecondaryTapCancel)
                {
                    InvokeCallback(note + "onSecondaryTapCancel", onSecondaryTapCancel);
                }

                break;
            case PointerButtons.Middle:
                if (OnTertiaryTapCancel is { } onTertiaryTapCancel)
                {
                    InvokeCallback(note + "onTertiaryTapCancel", onTertiaryTapCancel);
                }

                break;
        }
    }

    public override string DebugDescription => "tap";
}
