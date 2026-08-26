using Avalonia;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/multitap.dart

namespace Plumix.Gestures;

/// <summary>
/// Dart's `_CountdownZoned`: a fire-and-forget countdown whose only observable is whether it has
/// elapsed.
/// </summary>
internal sealed class CountdownZoned
{
    public CountdownZoned(TimeSpan duration)
    {
        GestureTimer.Start(duration, () => Timeout = true);
    }

    public bool Timeout { get; private set; }
}

/// <summary>
/// Dart's `_TapTracker`: tracks a single tap sequence for the multitap recognizers.
/// </summary>
internal class TapTracker
{
    private readonly Point _initialGlobalPosition;
    private readonly CountdownZoned _doubleTapMinTimeCountdown;
    private bool _isTrackingPointer;

    public TapTracker(
        PointerDownEvent @event,
        GestureArenaEntry entry,
        TimeSpan doubleTapMinTime,
        DeviceGestureSettings? gestureSettings)
    {
        GestureSettings = gestureSettings;
        Pointer = @event.Pointer;
        Entry = entry;
        _initialGlobalPosition = @event.Position;
        InitialButtons = @event.Buttons;
        _doubleTapMinTimeCountdown = new CountdownZoned(doubleTapMinTime);
    }

    public DeviceGestureSettings? GestureSettings { get; }

    public int Pointer { get; }

    public GestureArenaEntry Entry { get; }

    public PointerButtons InitialButtons { get; }

    public void StartTrackingPointer(PointerRouter router, PointerRoute route)
    {
        if (!_isTrackingPointer)
        {
            _isTrackingPointer = true;
            router.AddRoute(Pointer, route);
        }
    }

    public virtual void StopTrackingPointer(PointerRouter router, PointerRoute route)
    {
        if (_isTrackingPointer)
        {
            _isTrackingPointer = false;
            router.RemoveRoute(Pointer, route);
        }
    }

    public bool IsWithinGlobalTolerance(PointerEvent @event, double tolerance)
    {
        return (@event.Position - _initialGlobalPosition).Distance() <= tolerance;
    }

    public bool HasElapsedMinTime()
    {
        return _doubleTapMinTimeCountdown.Timeout;
    }

    public bool HasSameButton(PointerDownEvent @event)
    {
        return @event.Buttons == InitialButtons;
    }
}

/// <summary>
/// Recognizes when the user has tapped the screen at the same location twice in quick succession.
/// Ports Dart's `DoubleTapGestureRecognizer`.
/// </summary>
public class DoubleTapGestureRecognizer : GestureRecognizer, IGestureArenaMember
{
    // The recognizer has four implicit states, exactly like Dart:
    // - Waiting on first tap: no trackers, `_firstTap` null.
    // - First tap in progress: trackers present, `_firstTap` null.
    // - Waiting on second tap: no trackers, `_firstTap` non-null (arena held).
    // - Second tap in progress: trackers present, `_firstTap` non-null.
    private readonly Dictionary<int, TapTracker> _trackers = [];
    private GestureTimer? _doubleTapTimer;
    private TapTracker? _firstTap;

    public DoubleTapGestureRecognizer(
        AllowedButtonsFilter? allowedButtonsFilter = null,
        GestureBinding? binding = null) : base(binding)
    {
        AllowedButtonsFilter = allowedButtonsFilter ?? DefaultDoubleTapButtonAcceptBehavior;
    }

    /// <summary>Dart's `_defaultButtonAcceptBehavior`: double taps accept only the primary button.</summary>
    private static bool DefaultDoubleTapButtonAcceptBehavior(PointerButtons buttons)
    {
        return buttons == PointerButtons.Primary;
    }

    /// <summary>A pointer that might cause a double tap has contacted the screen at a particular location.</summary>
    public Action<TapDownDetails>? OnDoubleTapDown { get; set; }

    /// <summary>The user has tapped the screen at the same location twice in quick succession.</summary>
    public Action? OnDoubleTap { get; set; }

    /// <summary>The pointer that previously triggered <see cref="OnDoubleTapDown"/> will not end up causing a double tap.</summary>
    public Action? OnDoubleTapCancel { get; set; }

    protected override bool IsPointerAllowed(PointerDownEvent @event)
    {
        if (_firstTap is null
            && OnDoubleTapDown is null
            && OnDoubleTap is null
            && OnDoubleTapCancel is null)
        {
            return false;
        }

        // If second tap is not allowed, reset the state.
        bool isPointerAllowed = base.IsPointerAllowed(@event);
        if (!isPointerAllowed)
        {
            Reset();
        }

        return isPointerAllowed;
    }

    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        if (_firstTap is not null)
        {
            if (!_firstTap.IsWithinGlobalTolerance(@event, GestureConstants.DoubleTapSlop))
            {
                // Ignore out-of-bounds second taps.
                return;
            }

            if (!_firstTap.HasElapsedMinTime() || !_firstTap.HasSameButton(@event))
            {
                // Restart when the second tap is too close to the first (touch screens often
                // detect touches intermittently), or when buttons mismatch.
                Reset();
                TrackTap(@event);
                return;
            }

            if (OnDoubleTapDown is { } onDoubleTapDown)
            {
                var details = new TapDownDetails(
                    globalPosition: @event.Position,
                    localPosition: @event.LocalPosition,
                    kind: GetKindForPointer(@event.Pointer));
                InvokeCallback("onDoubleTapDown", () => onDoubleTapDown(details));
            }
        }

        TrackTap(@event);
    }

    protected override void HandleEvent(PointerEvent @event)
    {
        if (!_trackers.TryGetValue(@event.Pointer, out TapTracker? tracker))
        {
            return;
        }

        switch (@event)
        {
            case PointerUpEvent:
                if (_firstTap is null)
                {
                    RegisterFirstTap(tracker);
                }
                else
                {
                    RegisterSecondTap(tracker);
                }

                break;
            case PointerMoveEvent move:
                if (!tracker.IsWithinGlobalTolerance(move, GestureConstants.DoubleTapTouchSlop))
                {
                    Reject(tracker);
                }

                break;
            case PointerCancelEvent:
                Reject(tracker);
                break;
        }
    }

    public void AcceptGesture(int pointer)
    {
    }

    public void RejectGesture(int pointer)
    {
        if (!_trackers.TryGetValue(pointer, out TapTracker? tracker))
        {
            // If tracker isn't in the list, check if this is the first tap tracker.
            if (_firstTap is not null && _firstTap.Pointer == pointer)
            {
                tracker = _firstTap;
            }
        }

        // If tracker is still null, the pointer's gesture was already rejected by this recognizer.
        if (tracker is not null)
        {
            Reject(tracker);
        }
    }

    private void Reject(TapTracker tracker)
    {
        _trackers.Remove(tracker.Pointer);
        tracker.Entry.Resolve(GestureDisposition.Rejected);
        FreezeTracker(tracker);
        if (_firstTap is not null)
        {
            if (ReferenceEquals(tracker, _firstTap))
            {
                Reset();
            }
            else
            {
                CheckCancel();
                if (_trackers.Count == 0)
                {
                    Reset();
                }
            }
        }
    }

    public override void Dispose()
    {
        Reset();
        base.Dispose();
    }

    private void Reset()
    {
        StopDoubleTapTimer();
        if (_firstTap is not null)
        {
            if (_trackers.Count > 0)
            {
                CheckCancel();
            }

            // Note, order is important below in order for the resolve -> reject flow to work
            // properly, exactly like Dart.
            TapTracker tracker = _firstTap;
            _firstTap = null;
            Reject(tracker);
            GestureArena.Release(tracker.Pointer);
        }

        ClearTrackers();
    }

    private void TrackTap(PointerDownEvent @event)
    {
        StopDoubleTapTimer();
        var tracker = new TapTracker(
            @event: @event,
            entry: GestureArena.Add(@event.Pointer, this),
            doubleTapMinTime: GestureConstants.DoubleTapMinTime,
            gestureSettings: GestureSettings);
        _trackers[@event.Pointer] = tracker;
        tracker.StartTrackingPointer(PointerRouter, HandleEvent);
    }

    private void RegisterFirstTap(TapTracker tracker)
    {
        StartDoubleTapTimer();
        GestureArena.Hold(tracker.Pointer);
        // Note, order is important below in order for the clear -> reject flow to work properly.
        FreezeTracker(tracker);
        _trackers.Remove(tracker.Pointer);
        ClearTrackers();
        _firstTap = tracker;
    }

    private void RegisterSecondTap(TapTracker tracker)
    {
        _firstTap!.Entry.Resolve(GestureDisposition.Accepted);
        tracker.Entry.Resolve(GestureDisposition.Accepted);
        FreezeTracker(tracker);
        _trackers.Remove(tracker.Pointer);
        CheckUp(tracker.InitialButtons);
        Reset();
    }

    private void ClearTrackers()
    {
        foreach (TapTracker tracker in _trackers.Values.ToArray())
        {
            Reject(tracker);
        }
    }

    private void FreezeTracker(TapTracker tracker)
    {
        tracker.StopTrackingPointer(PointerRouter, HandleEvent);
    }

    private void StartDoubleTapTimer()
    {
        _doubleTapTimer ??= GestureTimer.Start(GestureConstants.DoubleTapTimeout, Reset);
    }

    private void StopDoubleTapTimer()
    {
        _doubleTapTimer?.Cancel();
        _doubleTapTimer = null;
    }

    private void CheckUp(PointerButtons buttons)
    {
        if (OnDoubleTap is { } onDoubleTap)
        {
            InvokeCallback("onDoubleTap", onDoubleTap);
        }
    }

    private void CheckCancel()
    {
        if (OnDoubleTapCancel is { } onDoubleTapCancel)
        {
            InvokeCallback("onDoubleTapCancel", onDoubleTapCancel);
        }
    }

    public override string DebugDescription => "double tap";
}

/// <summary>
/// Dart's `_TapGesture`: the tap tracked by <see cref="MultiTapGestureRecognizer"/>.
/// </summary>
internal sealed class TapGesture : TapTracker
{
    private readonly MultiTapGestureRecognizer _gestureRecognizer;
    private bool _wonArena;
    private GestureTimer? _timer;
    private OffsetPair _lastPosition;
    private OffsetPair? _finalPosition;

    public TapGesture(
        MultiTapGestureRecognizer gestureRecognizer,
        PointerDownEvent @event,
        TimeSpan longTapDelay,
        GestureArenaEntry entry,
        DeviceGestureSettings? gestureSettings) : base(
        @event: @event,
        entry: entry,
        doubleTapMinTime: GestureConstants.DoubleTapMinTime,
        gestureSettings: gestureSettings)
    {
        _gestureRecognizer = gestureRecognizer;
        _lastPosition = OffsetPair.FromEventPosition(@event);
        StartTrackingPointer(gestureRecognizer.Router, HandleEvent);
        if (longTapDelay > TimeSpan.Zero)
        {
            _timer = GestureTimer.Start(longTapDelay, () =>
            {
                _timer = null;
                _gestureRecognizer.DispatchLongTap(@event.Pointer, _lastPosition);
            });
        }
    }

    public void HandleEvent(PointerEvent @event)
    {
        if (@event.Pointer != Pointer)
        {
            return;
        }

        switch (@event)
        {
            case PointerMoveEvent move:
                if (!IsWithinGlobalTolerance(move, PointerEventUtils.ComputeHitSlop(move.Kind, GestureSettings)))
                {
                    Cancel();
                }
                else
                {
                    _lastPosition = OffsetPair.FromEventPosition(move);
                }

                break;
            case PointerCancelEvent:
                Cancel();
                break;
            case PointerUpEvent up:
                StopTrackingPointer(_gestureRecognizer.Router, HandleEvent);
                _finalPosition = OffsetPair.FromEventPosition(up);
                Check();
                break;
        }
    }

    public override void StopTrackingPointer(PointerRouter router, PointerRoute route)
    {
        _timer?.Cancel();
        _timer = null;
        base.StopTrackingPointer(router, route);
    }

    public void Accept()
    {
        _wonArena = true;
        Check();
    }

    public void Reject()
    {
        StopTrackingPointer(_gestureRecognizer.Router, HandleEvent);
        _gestureRecognizer.DispatchCancel(Pointer);
    }

    public void Cancel()
    {
        // If we won the arena already, then entry is resolved, so we need to clean up ourselves;
        // otherwise, we are rejected by the arena, which then calls reject() for us.
        if (_wonArena)
        {
            Reject();
        }
        else
        {
            Entry.Resolve(GestureDisposition.Rejected);
        }
    }

    private void Check()
    {
        if (_wonArena && _finalPosition is { } finalPosition)
        {
            _gestureRecognizer.DispatchTap(Pointer, finalPosition);
        }
    }
}

/// <summary>
/// Recognizes taps on a per-pointer basis: each pointer is a potential tap independently of other
/// pointers. Ports Dart's `MultiTapGestureRecognizer`.
/// </summary>
public class MultiTapGestureRecognizer : GestureRecognizer, IGestureArenaMember
{
    private readonly Dictionary<int, TapGesture> _gestureMap = [];

    public MultiTapGestureRecognizer(
        TimeSpan longTapDelay = default,
        GestureBinding? binding = null) : base(binding)
    {
        LongTapDelay = longTapDelay;
    }

    /// <summary>A pointer that might cause a tap has contacted the screen.</summary>
    public Action<int, TapDownDetails>? OnTapDown { get; set; }

    /// <summary>A pointer that will trigger a tap has stopped contacting the screen.</summary>
    public Action<int, TapUpDetails>? OnTapUp { get; set; }

    /// <summary>A tap has occurred.</summary>
    public Action<int>? OnTap { get; set; }

    /// <summary>The pointer that previously triggered <see cref="OnTapDown"/> will not cause a tap.</summary>
    public Action<int>? OnTapCancel { get; set; }

    /// <summary>The amount of time between <see cref="OnTapDown"/> and <see cref="OnLongTapDown"/>.</summary>
    public TimeSpan LongTapDelay { get; set; }

    /// <summary>A pointer that might cause a tap is still in contact after <see cref="LongTapDelay"/>.</summary>
    public Action<int, TapDownDetails>? OnLongTapDown { get; set; }

    internal PointerRouter Router => PointerRouter;

    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        _gestureMap[@event.Pointer] = new TapGesture(
            gestureRecognizer: this,
            @event: @event,
            longTapDelay: LongTapDelay,
            entry: GestureArena.Add(@event.Pointer, this),
            gestureSettings: GestureSettings);
        if (OnTapDown is { } onTapDown)
        {
            var details = new TapDownDetails(
                globalPosition: @event.Position,
                localPosition: @event.LocalPosition,
                kind: @event.Kind);
            InvokeCallback("onTapDown", () => onTapDown(@event.Pointer, details));
        }
    }

    protected override void HandleEvent(PointerEvent @event)
    {
        // Each TapGesture routes its own pointer events; nothing arrives through base tracking.
    }

    public void AcceptGesture(int pointer)
    {
        if (_gestureMap.TryGetValue(pointer, out TapGesture? gesture))
        {
            gesture.Accept();
        }
    }

    public void RejectGesture(int pointer)
    {
        if (_gestureMap.TryGetValue(pointer, out TapGesture? gesture))
        {
            gesture.Reject();
        }
    }

    internal void DispatchCancel(int pointer)
    {
        _gestureMap.Remove(pointer);
        if (OnTapCancel is { } onTapCancel)
        {
            InvokeCallback("onTapCancel", () => onTapCancel(pointer));
        }
    }

    internal void DispatchTap(int pointer, OffsetPair position)
    {
        _gestureMap.Remove(pointer);
        if (OnTapUp is { } onTapUp)
        {
            var details = new TapUpDetails(
                kind: GetKindForPointer(pointer),
                localPosition: position.Local,
                globalPosition: position.Global);
            InvokeCallback("onTapUp", () => onTapUp(pointer, details));
        }

        if (OnTap is { } onTap)
        {
            InvokeCallback("onTap", () => onTap(pointer));
        }
    }

    internal void DispatchLongTap(int pointer, OffsetPair lastPosition)
    {
        if (!_gestureMap.ContainsKey(pointer))
        {
            return;
        }

        if (OnLongTapDown is { } onLongTapDown)
        {
            var details = new TapDownDetails(
                globalPosition: lastPosition.Global,
                localPosition: lastPosition.Local,
                kind: GetKindForPointer(pointer));
            InvokeCallback("onLongTapDown", () => onLongTapDown(pointer, details));
        }
    }

    public override void Dispose()
    {
        foreach (TapGesture gesture in _gestureMap.Values.ToArray())
        {
            gesture.Cancel();
        }

        base.Dispose();
    }

    public override string DebugDescription => "multitap";
}

/// <summary>Details for `GestureSerialTapDownCallback`, such as the tap count within the series.</summary>
public sealed class SerialTapDownDetails : IPositionedGestureDetails
{
    public SerialTapDownDetails(
        PointerDeviceKind kind,
        Point globalPosition = default,
        Point? localPosition = null,
        PointerButtons buttons = PointerButtons.None,
        int count = 1)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "The count must be greater than zero.");
        }

        Kind = kind;
        GlobalPosition = globalPosition;
        LocalPosition = localPosition ?? globalPosition;
        Buttons = buttons;
        Count = count;
    }

    public Point GlobalPosition { get; }

    public Point LocalPosition { get; }

    /// <summary>The kind of the device that initiated the event.</summary>
    public PointerDeviceKind Kind { get; }

    /// <summary>Which buttons were pressed when the pointer contacted the screen.</summary>
    public PointerButtons Buttons { get; }

    /// <summary>The number of consecutive taps this tap represents, starting at one.</summary>
    public int Count { get; }
}

/// <summary>Details for `GestureSerialTapCancelCallback`, namely the count of the canceled tap.</summary>
public sealed class SerialTapCancelDetails
{
    public SerialTapCancelDetails(int count = 1)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "The count must be greater than zero.");
        }

        Count = count;
    }

    /// <summary>The number of consecutive taps the canceled tap would have represented.</summary>
    public int Count { get; }
}

/// <summary>Details for `GestureSerialTapUpCallback`, such as the tap count within the series.</summary>
public sealed class SerialTapUpDetails : IPositionedGestureDetails
{
    public SerialTapUpDetails(
        Point globalPosition = default,
        Point? localPosition = null,
        PointerDeviceKind? kind = null,
        int count = 1)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "The count must be greater than zero.");
        }

        GlobalPosition = globalPosition;
        LocalPosition = localPosition ?? globalPosition;
        Kind = kind;
        Count = count;
    }

    public Point GlobalPosition { get; }

    public Point LocalPosition { get; }

    /// <summary>The kind of the device that initiated the event.</summary>
    public PointerDeviceKind? Kind { get; }

    /// <summary>The number of consecutive taps this tap represents, starting at one.</summary>
    public int Count { get; }
}

/// <summary>
/// Recognizes serial taps: taps in a series, whether one, two, or more.
/// Ports Dart's `SerialTapGestureRecognizer`.
/// </summary>
public class SerialTapGestureRecognizer : GestureRecognizer, IGestureArenaMember
{
    private readonly List<TapTracker> _completedTaps = [];
    private readonly Dictionary<int, GestureDisposition> _gestureResolutions = [];
    private GestureTimer? _serialTapTimer;
    private TapTracker? _pendingTap;

    public SerialTapGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    /// <summary>A pointer that might cause a serial tap has contacted the screen.</summary>
    public Action<SerialTapDownDetails>? OnSerialTapDown { get; set; }

    /// <summary>A pointer that previously triggered <see cref="OnSerialTapDown"/> will not cause a serial tap.</summary>
    public Action<SerialTapCancelDetails>? OnSerialTapCancel { get; set; }

    /// <summary>A pointer that will trigger a serial tap has stopped contacting the screen.</summary>
    public Action<SerialTapUpDetails>? OnSerialTapUp { get; set; }

    /// <summary>Whether this recognizer is currently tracking a pointer in contact with the screen.</summary>
    public bool IsTrackingPointerInSeries => _pendingTap is not null;

    protected override bool IsPointerAllowed(PointerDownEvent @event)
    {
        if (OnSerialTapDown is null && OnSerialTapCancel is null && OnSerialTapUp is null)
        {
            return false;
        }

        return base.IsPointerAllowed(@event);
    }

    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        if ((_completedTaps.Count > 0 && !RepresentsSameSeries(_completedTaps[^1], @event))
            || _pendingTap is not null)
        {
            Reset();
        }

        TrackTap(@event);
    }

    private static bool RepresentsSameSeries(TapTracker tap, PointerDownEvent @event)
    {
        return tap.HasElapsedMinTime() // touch screens often detect touches intermittently
               && tap.HasSameButton(@event)
               && tap.IsWithinGlobalTolerance(@event, GestureConstants.DoubleTapSlop);
    }

    private void TrackTap(PointerDownEvent @event)
    {
        StopSerialTapTimer();
        if (OnSerialTapDown is { } onSerialTapDown)
        {
            var details = new SerialTapDownDetails(
                globalPosition: @event.Position,
                localPosition: @event.LocalPosition,
                kind: GetKindForPointer(@event.Pointer),
                buttons: @event.Buttons,
                count: _completedTaps.Count + 1);
            InvokeCallback("onSerialTapDown", () => onSerialTapDown(details));
        }

        var tracker = new TapTracker(
            @event: @event,
            entry: GestureArena.Add(@event.Pointer, this),
            doubleTapMinTime: GestureConstants.DoubleTapMinTime,
            gestureSettings: GestureSettings);
        _pendingTap = tracker;
        tracker.StartTrackingPointer(PointerRouter, HandleEvent);
    }

    protected override void HandleEvent(PointerEvent @event)
    {
        if (_pendingTap is not { } tracker || tracker.Pointer != @event.Pointer)
        {
            return;
        }

        switch (@event)
        {
            case PointerUpEvent up:
                RegisterTap(up, tracker);
                break;
            case PointerMoveEvent move:
                if (!tracker.IsWithinGlobalTolerance(move, GestureConstants.DoubleTapTouchSlop))
                {
                    Reset();
                }

                break;
            case PointerCancelEvent:
                Reset();
                break;
        }
    }

    public void AcceptGesture(int pointer)
    {
        _gestureResolutions[pointer] = GestureDisposition.Accepted;
    }

    public void RejectGesture(int pointer)
    {
        _gestureResolutions[pointer] = GestureDisposition.Rejected;
        Reset();
    }

    private void RejectPendingTap()
    {
        TapTracker tracker = _pendingTap!;
        _pendingTap = null;
        // Order is important here; the `resolve` call can recursively call `Reset()`, so the
        // cancel callback fires first, exactly like Dart.
        CheckCancel(_completedTaps.Count + 1);
        if (!_gestureResolutions.ContainsKey(tracker.Pointer))
        {
            tracker.Entry.Resolve(GestureDisposition.Rejected);
        }

        StopTrackingPointerTracker(tracker);
    }

    public override void Dispose()
    {
        Reset();
        base.Dispose();
    }

    private void Reset()
    {
        if (_pendingTap is not null)
        {
            RejectPendingTap();
        }

        _pendingTap = null;
        _completedTaps.Clear();
        _gestureResolutions.Clear();
        StopSerialTapTimer();
    }

    private void RegisterTap(PointerUpEvent @event, TapTracker tracker)
    {
        StartSerialTapTimer();
        if (!_gestureResolutions.ContainsKey(tracker.Pointer))
        {
            tracker.Entry.Resolve(GestureDisposition.Accepted);
        }

        StopTrackingPointerTracker(tracker);
        // Note, order is important below in order for the clear -> reject flow to work properly.
        _pendingTap = null;
        CheckUp(@event, tracker);
        _completedTaps.Add(tracker);
    }

    private void StopTrackingPointerTracker(TapTracker tracker)
    {
        tracker.StopTrackingPointer(PointerRouter, HandleEvent);
    }

    private void StartSerialTapTimer()
    {
        _serialTapTimer ??= GestureTimer.Start(GestureConstants.DoubleTapTimeout, Reset);
    }

    private void StopSerialTapTimer()
    {
        _serialTapTimer?.Cancel();
        _serialTapTimer = null;
    }

    private void CheckUp(PointerUpEvent @event, TapTracker tracker)
    {
        if (OnSerialTapUp is { } onSerialTapUp)
        {
            var details = new SerialTapUpDetails(
                globalPosition: @event.Position,
                localPosition: @event.LocalPosition,
                kind: GetKindForPointer(tracker.Pointer),
                count: _completedTaps.Count + 1);
            InvokeCallback("onSerialTapUp", () => onSerialTapUp(details));
        }
    }

    private void CheckCancel(int count)
    {
        if (OnSerialTapCancel is { } onSerialTapCancel)
        {
            var details = new SerialTapCancelDetails(count: count);
            InvokeCallback("onSerialTapCancel", () => onSerialTapCancel(details));
        }
    }

    public override string DebugDescription => "serial tap";
}
