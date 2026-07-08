using Avalonia;
using Avalonia.Threading;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/tap.dart (approximate)

namespace Plumix.Gestures;

public sealed class TapGestureRecognizer : GestureRecognizer, IGestureArenaMember
{
    private const double TouchSlop = 18.0;
    private static readonly TimeSpan DoubleTapTimeout = TimeSpan.FromMilliseconds(300);
    private readonly Dictionary<int, TapTracker> _trackers = [];
    private readonly object _doubleTapGate = new();
    private Timer? _singleTapTimer;
    private DateTime? _lastTapAt;
    private Point _lastTapPosition;

    public TapGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    public Action? OnTap { get; set; }
    public Action? OnDoubleTap { get; set; }
    public Action<PointerDownEvent>? OnTapDown { get; set; }
    public Action<PointerUpEvent>? OnTapUp { get; set; }
    public Action? OnTapCancel { get; set; }
    public Action? OnSecondaryTap { get; set; }
    public Action<PointerDownEvent>? OnSecondaryTapDown { get; set; }
    public Action<PointerUpEvent>? OnSecondaryTapUp { get; set; }
    public Action? OnSecondaryTapCancel { get; set; }

    public override void AddPointer(PointerDownEvent @event)
    {
        if (_trackers.ContainsKey(@event.Pointer))
        {
            return;
        }

        var arenaEntry = GestureArena.Add(@event.Pointer, this);
        var isSecondary = (@event.Buttons & PointerButtons.Secondary) != 0;
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
            case PointerMoveEvent:
            {
                if (Distance(tracker.InitialPosition, @event.Position) > TouchSlop)
                {
                    tracker.Entry.Resolve(GestureDisposition.Rejected);
                    Cleanup(@event.Pointer);
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
                && Distance(_lastTapPosition, position) <= TouchSlop)
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
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
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
