using Avalonia;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/monodrag.dart (approximate)

namespace Plumix.Gestures;

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

    public DragStartBehavior DragStartBehavior { get; set; } = DragStartBehavior.Start;

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
        if (DragStartBehavior == DragStartBehavior.Start && tracker.PendingPosition is null)
        {
            return;
        }

        Point startPosition = DragStartBehavior == DragStartBehavior.Down
            ? tracker.InitialPosition
            : ResolveStartPosition(
                tracker.InitialPosition,
                tracker.PendingPosition ?? tracker.LastPosition);
        tracker.LastPosition = startPosition;
        tracker.Started = true;
        OnStart?.Invoke(new DragStartDetails(startPosition));
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
                    double primary = Math.Abs(GetPrimaryValue(totalDelta));
                    double cross = Math.Abs(GetCrossValue(totalDelta));

                    if (primary > TouchSlop && primary > cross)
                    {
                        tracker.PendingPosition = @event.Position;
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
                    if (!tracker.Started)
                    {
                        double primary = Math.Abs(GetPrimaryValue(totalDelta));
                        double cross = Math.Abs(GetCrossValue(totalDelta));
                        if (primary <= TouchSlop || primary <= cross)
                        {
                            tracker.RecordPosition(@event.Position, @event.TimestampUtc);
                            break;
                        }

                        tracker.Started = true;
                        Point startPosition = ResolveStartPosition(
                            tracker.InitialPosition,
                            @event.Position);
                        tracker.LastPosition = startPosition;
                        OnStart?.Invoke(new DragStartDetails(startPosition));
                    }

                    var delta = @event.Position - tracker.LastPosition;
                    double primaryDelta = GetPrimaryValue(delta);
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
                if (!tracker.Accepted || !tracker.Started)
                {
                    tracker.Entry.Resolve(GestureDisposition.Rejected);
                    Cleanup(@event.Pointer);
                    return;
                }

                tracker.RecordPosition(@event.Position, @event.TimestampUtc);
                Vector pixelsPerSecond = tracker.EstimateVelocity();
                double primaryVelocity = GetPrimaryValue(new Point(
                    pixelsPerSecond.X,
                    pixelsPerSecond.Y));
                OnEnd?.Invoke(new DragEndDetails(
                    velocity: new Velocity(pixelsPerSecond),
                    primaryVelocity: primaryVelocity));
                Cleanup(@event.Pointer);
                break;
            }
            case PointerCancelEvent:
            {
                if (tracker.Started)
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

    protected abstract Point GetPrimaryOffset(double value);

    private Point ResolveStartPosition(Point initialPosition, Point currentPosition)
    {
        Point delta = currentPosition - initialPosition;
        double primaryDelta = GetPrimaryValue(delta);
        return initialPosition + GetPrimaryOffset(Math.Sign(primaryDelta) * TouchSlop);
    }

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

        public Point? PendingPosition { get; set; }

        public bool Started { get; set; }

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

        public Vector EstimateVelocity()
        {
            if (_samples.Count < 2)
            {
                return default;
            }

            var newest = _samples[^1];
            for (int i = _samples.Count - 2; i >= 0; i--)
            {
                var older = _samples[i];
                double elapsedSeconds = (newest.TimestampUtc - older.TimestampUtc).TotalSeconds;
                if (elapsedSeconds <= 0)
                {
                    continue;
                }

                Point delta = newest.Position - older.Position;
                return new Vector(delta.X / elapsedSeconds, delta.Y / elapsedSeconds);
            }

            return default;
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

    protected override Point GetPrimaryOffset(double value) => new(value, 0.0);
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

    protected override Point GetPrimaryOffset(double value) => new(0.0, value);
}
