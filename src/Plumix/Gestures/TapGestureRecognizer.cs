using Avalonia;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/tap.dart (approximate)

namespace Plumix.Gestures;

public sealed class TapGestureRecognizer : GestureRecognizer, IGestureArenaMember
{
    private const double TouchSlop = 18.0;
    private readonly Dictionary<int, TapTracker> _trackers = [];

    public TapGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    public Action? OnTap { get; set; }

    public override void AddPointer(PointerDownEvent @event)
    {
        if (_trackers.ContainsKey(@event.Pointer))
        {
            return;
        }

        var arenaEntry = GestureArena.Add(@event.Pointer, this);
        _trackers[@event.Pointer] = new TapTracker(@event.Position, arenaEntry);
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
        OnTap?.Invoke();
        Cleanup(pointer);
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
        public TapTracker(Point initialPosition, GestureArenaEntry entry)
        {
            InitialPosition = initialPosition;
            Entry = entry;
        }

        public Point InitialPosition { get; }

        public GestureArenaEntry Entry { get; }

        public bool Accepted { get; set; }

        public bool UpSeen { get; set; }

        public bool Fired { get; set; }
    }
}
