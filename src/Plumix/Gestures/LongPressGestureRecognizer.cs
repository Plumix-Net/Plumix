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

    public Action? OnLongPressUp { get; set; }

    /// <summary>Called when a long press is recognized, with the position it started at.</summary>
    public Action<LongPressStartDetails>? OnLongPressStart { get; set; }

    /// <summary>Called when the pointer moves after a long press was recognized.</summary>
    public Action<LongPressMoveUpdateDetails>? OnLongPressMoveUpdate { get; set; }

    /// <summary>Called when the pointer stops contacting the screen after a long press.</summary>
    public Action<LongPressEndDetails>? OnLongPressEnd { get; set; }

    public override void AddPointer(PointerDownEvent @event)
    {
        if (_trackers.ContainsKey(@event.Pointer) || !IsPointerAllowed(@event))
        {
            return;
        }

        var arenaEntry = GestureArena.Add(@event.Pointer, this);
        var tracker = new LongPressTracker(@event.Position, @event.LocalPosition, arenaEntry);
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
                if (tracker.Fired)
                {
                    // Once the press has been recognized, movement extends it instead of
                    // rejecting it, matching Flutter's `onLongPressMoveUpdate`.
                    OnLongPressMoveUpdate?.Invoke(new LongPressMoveUpdateDetails(
                        GlobalPosition: @event.Position,
                        LocalPosition: @event.LocalPosition,
                        OffsetFromOrigin: @event.Position - tracker.InitialPosition,
                        LocalOffsetFromOrigin: @event.LocalPosition - tracker.InitialLocalPosition));
                    break;
                }

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
                    if (tracker.Fired)
                    {
                        OnLongPressEnd?.Invoke(new LongPressEndDetails(
                            GlobalPosition: @event.Position,
                            LocalPosition: @event.LocalPosition));
                        OnLongPressUp?.Invoke();
                    }
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
        if (Deadline <= TimeSpan.Zero)
        {
            HandleDeadline(pointer, tracker);
            return;
        }

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
                HandleDeadline(pointer, tracker));
        });
    }

    private void HandleDeadline(int pointer, LongPressTracker tracker)
    {
        if (!_trackers.TryGetValue(pointer, out var activeTracker) || !ReferenceEquals(activeTracker, tracker))
        {
            return;
        }

        activeTracker.DeadlineExceeded = true;
        activeTracker.Entry.Resolve(GestureDisposition.Accepted);
        TryFire(pointer, activeTracker);
    }

    private void TryFire(int pointer, LongPressTracker tracker)
    {
        if (!tracker.Accepted || !tracker.DeadlineExceeded || tracker.Fired)
        {
            return;
        }

        tracker.Fired = true;
        OnLongPressStart?.Invoke(new LongPressStartDetails(
            GlobalPosition: tracker.InitialPosition,
            LocalPosition: tracker.InitialLocalPosition));
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
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private sealed class LongPressTracker
    {
        public LongPressTracker(Point initialPosition, Point initialLocalPosition, GestureArenaEntry entry)
        {
            InitialPosition = initialPosition;
            InitialLocalPosition = initialLocalPosition;
            Entry = entry;
            Cancellation = new CancellationTokenSource();
        }

        public Point InitialPosition { get; }

        public Point InitialLocalPosition { get; }

        public GestureArenaEntry Entry { get; }

        public CancellationTokenSource Cancellation { get; }

        public bool Accepted { get; set; }

        public bool DeadlineExceeded { get; set; }

        public bool Fired { get; set; }
    }
}

/// <summary>Details for [GestureLongPressStartCallback].</summary>
public readonly record struct LongPressStartDetails(Point GlobalPosition, Point LocalPosition = default);

/// <summary>Details for [GestureLongPressMoveUpdateCallback].</summary>
public readonly record struct LongPressMoveUpdateDetails(
    Point GlobalPosition,
    Point LocalPosition = default,
    Point OffsetFromOrigin = default,
    Point LocalOffsetFromOrigin = default);

/// <summary>Details for [GestureLongPressEndCallback].</summary>
public readonly record struct LongPressEndDetails(
    Point GlobalPosition,
    Point LocalPosition = default,
    Velocity Velocity = default);
