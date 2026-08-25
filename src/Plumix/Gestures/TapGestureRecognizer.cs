using Avalonia;
using Avalonia.Threading;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/tap.dart (approximate)

namespace Plumix.Gestures;

public sealed class TapGestureRecognizer : GestureRecognizer, IGestureArenaMember
{
    /// <summary>
    /// Dart's `_unsetTouchSlop` (`gestures/recognizer.dart`): the sentinel that distinguishes "not
    /// specified" (fall back to the device touch slop) from an explicit null (never reject on move).
    /// </summary>
    private const double UnsetTouchSlop = -1.0;

    private static readonly TimeSpan DoubleTapTimeout = TimeSpan.FromMilliseconds(300);
    private readonly Dictionary<int, TapTracker> _trackers = [];
    private readonly object _doubleTapGate = new();
    private readonly double? _preAcceptSlopTolerance;
    private readonly double? _postAcceptSlopTolerance;
    private Timer? _singleTapTimer;
    private DateTime? _lastTapAt;
    private Point _lastTapPosition;

    public TapGestureRecognizer(
        GestureBinding? binding = null,
        double? preAcceptSlopTolerance = UnsetTouchSlop,
        double? postAcceptSlopTolerance = UnsetTouchSlop) : base(binding)
    {
        if (preAcceptSlopTolerance is { } pre && pre != UnsetTouchSlop && pre < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(preAcceptSlopTolerance),
                "The preAcceptSlopTolerance must be unspecified, positive, or null.");
        }

        if (postAcceptSlopTolerance is { } post && post != UnsetTouchSlop && post < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(postAcceptSlopTolerance),
                "The postAcceptSlopTolerance must be unspecified, positive, or null.");
        }

        _preAcceptSlopTolerance = preAcceptSlopTolerance;
        _postAcceptSlopTolerance = postAcceptSlopTolerance;
    }

    /// <summary>
    /// The distance a pointer may travel before the gesture is accepted without the tap being
    /// rejected. Null means the tap is never rejected for moving before acceptance.
    /// </summary>
    public double? PreAcceptSlopTolerance =>
        _preAcceptSlopTolerance == UnsetTouchSlop ? DefaultTouchSlop : _preAcceptSlopTolerance;

    /// <summary>
    /// The distance a pointer may travel after the gesture is accepted before the tap is rejected.
    /// Null means the tap is never rejected for moving after acceptance, which is what
    /// <c>CupertinoButton</c> relies on to keep tracking a finger that leaves the button.
    /// </summary>
    public double? PostAcceptSlopTolerance =>
        _postAcceptSlopTolerance == UnsetTouchSlop ? DefaultTouchSlop : _postAcceptSlopTolerance;

    private double DefaultTouchSlop => GestureSettings?.TouchSlop ?? GestureConstants.TouchSlop;

    public Action? OnTap { get; set; }
    public Action? OnDoubleTap { get; set; }
    public Action<PointerDownEvent>? OnTapDown { get; set; }
    public Action<PointerUpEvent>? OnTapUp { get; set; }
    public Action? OnTapCancel { get; set; }

    /// <summary>A pointer that triggered a tap has moved without the tap being rejected.</summary>
    public Action<TapMoveDetails>? OnTapMove { get; set; }
    public Action? OnSecondaryTap { get; set; }
    public Action<PointerDownEvent>? OnSecondaryTapDown { get; set; }
    public Action<PointerUpEvent>? OnSecondaryTapUp { get; set; }
    public Action? OnSecondaryTapCancel { get; set; }

    /// <summary>
    /// Resolves every active pointer in the gesture arena, matching Flutter's public
    /// <c>GestureRecognizer.resolve</c> surface.
    /// </summary>
    public void Resolve(GestureDisposition disposition)
    {
        foreach (TapTracker tracker in _trackers.Values.ToArray())
        {
            tracker.Entry.Resolve(disposition);
        }
    }

    public override void AddPointer(PointerDownEvent @event)
    {
        if (_trackers.ContainsKey(@event.Pointer) || !IsPointerAllowed(@event))
        {
            return;
        }

        GestureArenaEntry arenaEntry = AddPointerToArena(@event.Pointer, this);
        bool isSecondary = (@event.Buttons & PointerButtons.Secondary) != 0;
        _trackers[@event.Pointer] = new TapTracker(@event.Position, arenaEntry, isSecondary);
        if (isSecondary)
        {
            OnSecondaryTapDown?.Invoke(@event);
        }
        else
        {
            OnTapDown?.Invoke(@event);
        }
        StartTrackingPointer(@event.Pointer);
    }

    /// <summary>
    /// Only competes for a button that has at least one callback, so a recognizer configured for
    /// secondary taps alone never claims a primary-button gesture.
    /// </summary>
    protected override bool IsPointerAllowed(PointerDownEvent @event)
    {
        if (!base.IsPointerAllowed(@event))
        {
            return false;
        }

        if ((@event.Buttons & PointerButtons.Secondary) != 0)
        {
            return OnSecondaryTap is not null
                   || OnSecondaryTapDown is not null
                   || OnSecondaryTapUp is not null
                   || OnSecondaryTapCancel is not null;
        }

        return OnTap is not null
               || OnDoubleTap is not null
               || OnTapDown is not null
               || OnTapUp is not null
               || OnTapCancel is not null
               || OnTapMove is not null;
    }

    public void AcceptGesture(int pointer)
    {
        if (!_trackers.TryGetValue(pointer, out var tracker))
        {
            return;
        }

        tracker.Accepted = true;
        TryFire(pointer, tracker);
    }

    public void RejectGesture(int pointer)
    {
        if (_trackers.TryGetValue(pointer, out var tracker))
        {
            if (tracker.IsSecondary)
            {
                OnSecondaryTapCancel?.Invoke();
            }
            else
            {
                OnTapCancel?.Invoke();
            }
        }
        Cleanup(pointer);
    }

    protected override void HandleEvent(PointerEvent @event)
    {
        if (!_trackers.TryGetValue(@event.Pointer, out var tracker))
        {
            return;
        }

        switch (@event)
        {
            case PointerMoveEvent move:
            {
                double? tolerance = tracker.Accepted ? PostAcceptSlopTolerance : PreAcceptSlopTolerance;
                if (tolerance is { } limit && Distance(tracker.InitialPosition, move.Position) > limit)
                {
                    tracker.Entry.Resolve(GestureDisposition.Rejected);
                    Cleanup(move.Pointer);
                    break;
                }

                if (OnTapMove is not null && move.Buttons == PointerButtons.Primary)
                {
                    OnTapMove(new TapMoveDetails(
                        move.Position,
                        move.LocalPosition,
                        move.Delta,
                        move.Kind));
                }

                break;
            }
            case PointerUpEvent:
            {
                tracker.UpEvent = (PointerUpEvent)@event;
                tracker.UpSeen = true;
                tracker.Entry.Resolve(GestureDisposition.Accepted);
                TryFire(@event.Pointer, tracker);
                break;
            }
            case PointerCancelEvent:
            {
                tracker.Entry.Resolve(GestureDisposition.Rejected);
                Cleanup(@event.Pointer);
                break;
            }
        }
    }

    private void TryFire(int pointer, TapTracker tracker)
    {
        if (!tracker.Accepted || !tracker.UpSeen || tracker.Fired)
        {
            return;
        }

        tracker.Fired = true;
        if (tracker.UpEvent is not null)
        {
            if (tracker.IsSecondary)
            {
                OnSecondaryTapUp?.Invoke(tracker.UpEvent);
            }
            else
            {
                OnTapUp?.Invoke(tracker.UpEvent);
            }
        }

        if (tracker.IsSecondary)
        {
            OnSecondaryTap?.Invoke();
        }
        else
        {
            FireTap(tracker.InitialPosition);
        }
        Cleanup(pointer);
    }

    private void FireTap(Point position)
    {
        if (OnDoubleTap is null)
        {
            OnTap?.Invoke();
            return;
        }

        Action? doubleTap = null;
        lock (_doubleTapGate)
        {
            var now = DateTime.UtcNow;
            if (_lastTapAt.HasValue
                && now - _lastTapAt.Value <= DoubleTapTimeout
                && Distance(_lastTapPosition, position) <= GestureConstants.DoubleTapTouchSlop)
            {
                _singleTapTimer?.Dispose();
                _singleTapTimer = null;
                _lastTapAt = null;
                doubleTap = OnDoubleTap;
            }
            else
            {
                _singleTapTimer?.Dispose();
                _lastTapAt = now;
                _lastTapPosition = position;
                _singleTapTimer = new Timer(_ =>
                {
                    Action? callback;
                    lock (_doubleTapGate)
                    {
                        callback = OnTap;
                        _lastTapAt = null;
                        _singleTapTimer?.Dispose();
                        _singleTapTimer = null;
                    }
                    if (callback is not null) Dispatcher.UIThread.Post(callback);
                }, null, DoubleTapTimeout, Timeout.InfiniteTimeSpan);
            }
        }
        doubleTap?.Invoke();
    }

    public override void Dispose()
    {
        foreach ((int pointer, TapTracker tracker) in _trackers.ToArray())
        {
            GestureArenaEntry entry = tracker.Entry;
            Cleanup(pointer);
            entry.Resolve(GestureDisposition.Rejected);
        }

        lock (_doubleTapGate)
        {
            _singleTapTimer?.Dispose();
            _singleTapTimer = null;
            _lastTapAt = null;
        }
        base.Dispose();
    }

    private void Cleanup(int pointer)
    {
        StopTrackingPointer(pointer);
        _trackers.Remove(pointer);
    }

    private static double Distance(Point a, Point b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private sealed class TapTracker
    {
        public TapTracker(Point initialPosition, GestureArenaEntry entry, bool isSecondary)
        {
            InitialPosition = initialPosition;
            Entry = entry;
            IsSecondary = isSecondary;
        }

        public Point InitialPosition { get; }

        public GestureArenaEntry Entry { get; }

        public bool IsSecondary { get; }

        public PointerUpEvent? UpEvent { get; set; }

        public bool Accepted { get; set; }

        public bool UpSeen { get; set; }

        public bool Fired { get; set; }
    }
}
