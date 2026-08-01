using Avalonia;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/monodrag.dart (approximate)

namespace Plumix.Gestures;

public abstract class DragGestureRecognizer : GestureRecognizer, IGestureArenaMember
{
    private const double DefaultTouchSlop = 18.0;
    private readonly Dictionary<int, DragTracker> _trackers = [];

    protected DragGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    public Action<DragStartDetails>? OnStart { get; set; }

    public Action<DragUpdateDetails>? OnUpdate { get; set; }

    public Action<DragEndDetails>? OnEnd { get; set; }

    public Action? OnCancel { get; set; }

    public DragStartBehavior DragStartBehavior { get; set; } = DragStartBehavior.Start;

    protected double TouchSlop => GestureSettings?.TouchSlop ?? DefaultTouchSlop;

    public override void AddPointer(PointerDownEvent @event)
    {
        if (_trackers.ContainsKey(@event.Pointer) || !IsPointerAllowed(@event))
        {
            return;
        }

        var entry = GestureArena.Add(@event.Pointer, this);
        _trackers[@event.Pointer] = new DragTracker(
            @event.Position,
            @event.LocalPosition,
            @event.Kind,
            @event.TimestampUtc,
            entry);
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
        OnStart?.Invoke(CreateStartDetails(tracker, startPosition));
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
                    else if (RejectsCrossAxisDrags && cross > TouchSlop && cross > primary)
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
                        OnStart?.Invoke(CreateStartDetails(tracker, startPosition));
                    }

                    var delta = @event.Position - tracker.LastPosition;
                    double primaryDelta = GetPrimaryValue(delta);
                    if (Math.Abs(primaryDelta) > double.Epsilon)
                    {
                        OnUpdate?.Invoke(new DragUpdateDetails(
                            GlobalPosition: @event.Position,
                            LocalPosition: @event.LocalPosition,
                            Delta: delta,
                            PrimaryDelta: GetReportedPrimaryDelta(delta),
                            SourceTimeStampUtc: @event.TimestampUtc,
                            Kind: tracker.Kind));
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

    /// <summary>Whether a drag that is dominated by the cross axis rejects this recognizer.</summary>
    protected virtual bool RejectsCrossAxisDrags => true;

    /// <summary>The primary delta reported to listeners; free-axis recognizers report no primary axis.</summary>
    protected virtual double GetReportedPrimaryDelta(Point delta) => GetPrimaryValue(delta);

    protected virtual Point ResolveStartPosition(Point initialPosition, Point currentPosition)
    {
        Point delta = currentPosition - initialPosition;
        double primaryDelta = GetPrimaryValue(delta);
        return initialPosition + GetPrimaryOffset(Math.Sign(primaryDelta) * TouchSlop);
    }

    private DragStartDetails CreateStartDetails(DragTracker tracker, Point globalPosition)
    {
        Point localPosition = tracker.InitialLocalPosition + (globalPosition - tracker.InitialPosition);
        return new DragStartDetails(
            GlobalPosition: globalPosition,
            LocalPosition: localPosition,
            SourceTimeStampUtc: tracker.LastTimestampUtc,
            Kind: tracker.Kind);
    }

    private sealed class DragTracker
    {
        private readonly List<VelocitySample> _samples = [];

        public DragTracker(
            Point initialPosition,
            Point initialLocalPosition,
            PointerDeviceKind kind,
            DateTime timestampUtc,
            GestureArenaEntry entry)
        {
            InitialPosition = initialPosition;
            InitialLocalPosition = initialLocalPosition;
            Kind = kind;
            LastPosition = initialPosition;
            LastTimestampUtc = timestampUtc;
            Entry = entry;
            _samples.Add(new VelocitySample(initialPosition, timestampUtc));
        }

        public Point InitialPosition { get; }

        public Point InitialLocalPosition { get; }

        public PointerDeviceKind Kind { get; }

        public DateTime LastTimestampUtc { get; private set; }

        public Point LastPosition { get; set; }

        public GestureArenaEntry Entry { get; }

        public bool Accepted { get; set; }

        public Point? PendingPosition { get; set; }

        public bool Started { get; set; }

        public void RecordPosition(Point position, DateTime timestampUtc)
        {
            LastPosition = position;
            LastTimestampUtc = timestampUtc;

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

/// <summary>Recognizes drags in any direction; it never yields to a competing axis.</summary>
public sealed class PanGestureRecognizer : DragGestureRecognizer
{
    public PanGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    protected override bool RejectsCrossAxisDrags => false;

    protected override double GetPrimaryValue(Point offset)
    {
        return Math.Sqrt((offset.X * offset.X) + (offset.Y * offset.Y));
    }

    protected override double GetCrossValue(Point offset)
    {
        return 0.0;
    }

    protected override Point GetPrimaryOffset(double value) => default;

    protected override double GetReportedPrimaryDelta(Point delta) => 0.0;

    protected override Point ResolveStartPosition(Point initialPosition, Point currentPosition)
    {
        return currentPosition;
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

    protected override Point GetPrimaryOffset(double value) => new(0.0, value);
}
