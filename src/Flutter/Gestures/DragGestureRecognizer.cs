using Avalonia;
using Flutter.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/monodrag.dart (approximate)

namespace Flutter.Gestures;

public abstract class DragGestureRecognizer : GestureRecognizer, IGestureArenaMember
{
    private const double TouchSlop = 18.0;
    private readonly Dictionary<int, DragTracker> _trackers = [];

    protected DragGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    public Action<DragStartDetails>? OnStart { get; set; }

    public Action<DragUpdateDetails>? OnUpdate { get; set; }

    public Action<DragEndDetails>? OnEnd { get; set; }

    public Action? OnCancel { get; set; }

    public override void AddPointer(PointerDownEvent @event)
    {
        if (_trackers.ContainsKey(@event.Pointer))
        {
            return;
        }

        var entry = GestureArena.Add(@event.Pointer, this);
        _trackers[@event.Pointer] = new DragTracker(@event.Position, @event.TimestampUtc, entry);
        StartTrackingPointer(@event.Pointer);
    }

    public void AcceptGesture(int pointer)
    {
        if (!_trackers.TryGetValue(pointer, out var tracker))
        {
            return;
        }

        tracker.Accepted = true;
        tracker.LastPosition = tracker.InitialPosition;
        OnStart?.Invoke(new DragStartDetails(tracker.InitialPosition));
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
                var totalDelta = @event.Position - tracker.InitialPosition;
                if (!tracker.Accepted)
                {
                    var primary = Math.Abs(GetPrimaryValue(totalDelta));
                    var cross = Math.Abs(GetCrossValue(totalDelta));

                    if (primary > TouchSlop && primary > cross)
                    {
                        tracker.Entry.Resolve(GestureDisposition.Accepted);
                    }
                    else if (cross > TouchSlop && cross > primary)
                    {
                        tracker.Entry.Resolve(GestureDisposition.Rejected);
                        Cleanup(@event.Pointer);
                        return;
                    }
                }

                if (tracker.Accepted)
                {
                    var delta = @event.Position - tracker.LastPosition;
                    var primaryDelta = GetPrimaryValue(delta);
                    if (Math.Abs(primaryDelta) > double.Epsilon)
                    {
                        OnUpdate?.Invoke(new DragUpdateDetails(
                            GlobalPosition: @event.Position,
                            LocalPosition: @event.LocalPosition,
                            Delta: delta,
                            PrimaryDelta: primaryDelta));
                    }
                }

                tracker.RecordPosition(@event.Position, @event.TimestampUtc);
                break;
            }
            case PointerUpEvent:
            {
                if (!tracker.Accepted)
                {
                    tracker.Entry.Resolve(GestureDisposition.Rejected);
                    Cleanup(@event.Pointer);
                    return;
                }

                tracker.RecordPosition(@event.Position, @event.TimestampUtc);
                var endVelocity = tracker.EstimateVelocity(GetPrimaryValue);
                OnEnd?.Invoke(new DragEndDetails(endVelocity));
                Cleanup(@event.Pointer);
                break;
            }
            case PointerCancelEvent:
            {
                if (tracker.Accepted)
                {
                    OnCancel?.Invoke();
                }

                tracker.Entry.Resolve(GestureDisposition.Rejected);
                Cleanup(@event.Pointer);
                break;
            }
        }
    }

    private void Cleanup(int pointer)
    {
        StopTrackingPointer(pointer);
        _trackers.Remove(pointer);
    }

    protected abstract double GetPrimaryValue(Point offset);

    protected abstract double GetCrossValue(Point offset);

    private sealed class DragTracker
    {
        private readonly List<VelocitySample> _samples = [];

        public DragTracker(Point initialPosition, DateTime timestampUtc, GestureArenaEntry entry)
        {
            InitialPosition = initialPosition;
            LastPosition = initialPosition;
            Entry = entry;
            _samples.Add(new VelocitySample(initialPosition, timestampUtc));
        }

        public Point InitialPosition { get; }

        public Point LastPosition { get; set; }

        public GestureArenaEntry Entry { get; }

        public bool Accepted { get; set; }

        public void RecordPosition(Point position, DateTime timestampUtc)
        {
            LastPosition = position;

            if (_samples.Count > 0 && timestampUtc <= _samples[^1].TimestampUtc)
            {
                _samples[^1] = new VelocitySample(position, timestampUtc);
            }
            else
            {
                _samples.Add(new VelocitySample(position, timestampUtc));
            }

            const int maxVelocitySamples = 4;
            if (_samples.Count > maxVelocitySamples)
            {
                _samples.RemoveRange(0, _samples.Count - maxVelocitySamples);
            }
        }

        public double EstimateVelocity(Func<Point, double> primaryValue)
        {
            if (_samples.Count < 2)
            {
                return 0;
            }

            var newest = _samples[^1];
            for (var i = _samples.Count - 2; i >= 0; i--)
            {
                var older = _samples[i];
                var elapsedSeconds = (newest.TimestampUtc - older.TimestampUtc).TotalSeconds;
                if (elapsedSeconds <= 0)
                {
                    continue;
                }

                return primaryValue(newest.Position - older.Position) / elapsedSeconds;
            }

            return 0;
        }
    }

    private readonly record struct VelocitySample(Point Position, DateTime TimestampUtc);
}

public sealed class HorizontalDragGestureRecognizer : DragGestureRecognizer
{
    public HorizontalDragGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    protected override double GetPrimaryValue(Point offset)
    {
        return offset.X;
    }

    protected override double GetCrossValue(Point offset)
    {
        return offset.Y;
    }
}

public sealed class VerticalDragGestureRecognizer : DragGestureRecognizer
{
    public VerticalDragGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    protected override double GetPrimaryValue(Point offset)
    {
        return offset.Y;
    }

    protected override double GetCrossValue(Point offset)
    {
        return offset.X;
    }
}
