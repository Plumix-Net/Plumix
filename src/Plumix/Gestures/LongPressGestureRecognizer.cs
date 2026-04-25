using Avalonia;
using Avalonia.Threading;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/long_press.dart (approximate)

namespace Plumix.Gestures;

public sealed class LongPressGestureRecognizer : GestureRecognizer, IGestureArenaMember
{
    private const double TouchSlop = 18.0;
    private readonly Dictionary<int, LongPressTracker> _trackers = [];

    public LongPressGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    public TimeSpan Deadline { get; set; } = TimeSpan.FromMilliseconds(500);

    public Action? OnLongPress { get; set; }

    public override void AddPointer(PointerDownEvent @event)
    {
        if (_trackers.ContainsKey(@event.Pointer))
        {
            return;
        }

        var arenaEntry = GestureArena.Add(@event.Pointer, this);
        var tracker = new LongPressTracker(@event.Position, arenaEntry);
        _trackers[@event.Pointer] = tracker;
        StartTrackingPointer(@event.Pointer);
        StartDeadlineTimer(@event.Pointer, tracker);
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
                if (!tracker.DeadlineExceeded)
                {
                    tracker.Entry.Resolve(GestureDisposition.Rejected);
                    Cleanup(@event.Pointer);
                }
                else
                {
                    Cleanup(@event.Pointer);
                }

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

    private void StartDeadlineTimer(int pointer, LongPressTracker tracker)
    {
        var cancellation = tracker.Cancellation;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(Deadline, cancellation.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!_trackers.TryGetValue(pointer, out var activeTracker) || !ReferenceEquals(activeTracker, tracker))
                {
                    return;
                }

                activeTracker.DeadlineExceeded = true;
                activeTracker.Entry.Resolve(GestureDisposition.Accepted);
                TryFire(pointer, activeTracker);
            });
        });
    }

    private void TryFire(int pointer, LongPressTracker tracker)
    {
        if (!tracker.Accepted || !tracker.DeadlineExceeded || tracker.Fired)
        {
            return;
        }

        tracker.Fired = true;
        OnLongPress?.Invoke();
    }

    private void Cleanup(int pointer)
    {
        if (_trackers.TryGetValue(pointer, out var tracker))
        {
            tracker.Cancellation.Cancel();
            tracker.Cancellation.Dispose();
        }

        StopTrackingPointer(pointer);
        _trackers.Remove(pointer);
    }

    private static double Distance(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private sealed class LongPressTracker
    {
        public LongPressTracker(Point initialPosition, GestureArenaEntry entry)
        {
            InitialPosition = initialPosition;
            Entry = entry;
            Cancellation = new CancellationTokenSource();
        }

        public Point InitialPosition { get; }

        public GestureArenaEntry Entry { get; }

        public CancellationTokenSource Cancellation { get; }

        public bool Accepted { get; set; }

        public bool DeadlineExceeded { get; set; }

        public bool Fired { get; set; }
    }
}
