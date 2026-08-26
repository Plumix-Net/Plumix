using Avalonia;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/monodrag.dart (approximate)

namespace Plumix.Gestures;

public abstract class DragGestureRecognizer : GestureRecognizer, IGestureArenaMember
{
    // gestures/constants.dart: kMinFlingVelocity, kMaxFlingVelocity.
    public const double KMinFlingVelocity = 50.0;
    public const double KMaxFlingVelocity = 8000.0;

    private const double DefaultTouchSlop = 18.0;
    private readonly Dictionary<int, DragTracker> _trackers = [];

    public static GestureVelocityTrackerBuilder DefaultVelocityTrackerBuilder { get; } =
        @event => new VelocityTracker(@event.Kind);

    protected DragGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    public Action<DragDownDetails>? OnDown { get; set; }

    public Action<DragStartDetails>? OnStart { get; set; }

    public Action<DragUpdateDetails>? OnUpdate { get; set; }

    public Action<DragEndDetails>? OnEnd { get; set; }

    public Action? OnCancel { get; set; }

    public DragStartBehavior DragStartBehavior { get; set; } = DragStartBehavior.Start;

    /// <summary>
    /// Whether this recognizer waits for the drag threshold after winning the gesture arena.
    /// </summary>
    public bool OnlyAcceptDragOnThreshold { get; set; }

    public GestureVelocityTrackerBuilder VelocityTrackerBuilder { get; set; } =
        DefaultVelocityTrackerBuilder;

    /// <summary>
    /// The minimum distance an input pointer drag must have moved to be considered a fling gesture.
    /// Null falls back to the device's hit slop.
    /// </summary>
    public double? MinFlingDistance { get; set; }

    /// <summary>
    /// The minimum velocity for an input pointer drag to be considered a fling gesture. Null falls
    /// back to <see cref="KMinFlingVelocity"/>.
    /// </summary>
    public double? MinFlingVelocity { get; set; }

    /// <summary>
    /// Fling velocity magnitudes are clamped to this value. Null falls back to
    /// <see cref="KMaxFlingVelocity"/>.
    /// </summary>
    public double? MaxFlingVelocity { get; set; }

    protected double TouchSlop => GestureSettings?.TouchSlop ?? DefaultTouchSlop;

    public override void AddPointer(PointerDownEvent @event)
    {
        if (_trackers.ContainsKey(@event.Pointer) || !IsPointerAllowed(@event))
        {
            return;
        }

        GestureArenaEntry entry = AddPointerToArena(@event.Pointer, this);
        VelocityTracker velocityTracker = VelocityTrackerBuilder(@event);
        _trackers[@event.Pointer] = new DragTracker(@event, entry, velocityTracker);
        StartTrackingPointer(@event.Pointer);
        OnDown?.Invoke(new DragDownDetails(
            GlobalPosition: @event.Position,
            LocalPosition: @event.LocalPosition));
    }

    public void AcceptGesture(int pointer)
    {
        if (!_trackers.TryGetValue(pointer, out var tracker))
        {
            return;
        }

        tracker.Accepted = true;
        if (OnlyAcceptDragOnThreshold && !tracker.HasDragThresholdBeenMet)
        {
            return;
        }

        CheckDrag(tracker);
    }

    public void RejectGesture(int pointer)
    {
        if (!_trackers.TryGetValue(pointer, out var tracker))
        {
            return;
        }

        bool started = tracker.Started;
        Cleanup(pointer);
        if (!started)
        {
            // Every OnDown is followed by either a start/end pair or a cancel.
            OnCancel?.Invoke();
        }
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
                        tracker.PendingTimestampUtc = @event.TimestampUtc;
                        tracker.HasDragThresholdBeenMet = true;
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

                        tracker.PendingPosition = @event.Position;
                        tracker.PendingTimestampUtc = @event.TimestampUtc;
                        tracker.HasDragThresholdBeenMet = true;
                        CheckDrag(tracker);
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
                    ResolveWithoutDrag(tracker, @event.Pointer);
                    return;
                }

                if (OnEnd != null)
                {
                    VelocityEstimate? estimate = tracker.VelocityTracker.GetVelocityEstimate();
                    DragEndDetails? details = estimate == null
                        ? null
                        : ConsiderFling(estimate, tracker.Kind);
                    DragEndDetails resolved = details ?? new DragEndDetails(
                        velocity: Velocity.Zero,
                        primaryVelocity: 0.0);
                    // Dart's `_checkEnd` reports the pointer's last position alongside the velocity.
                    OnEnd.Invoke(new DragEndDetails(
                        velocity: resolved.Velocity,
                        primaryVelocity: resolved.PrimaryVelocity,
                        globalPosition: @event.Position,
                        localPosition: @event.LocalPosition));
                }

                Cleanup(@event.Pointer);
                break;
            }
            case PointerCancelEvent:
            {
                ResolveWithoutDrag(tracker, @event.Pointer);
                break;
            }
        }
    }

    public override void Dispose()
    {
        foreach ((int pointer, DragTracker tracker) in _trackers.ToArray())
        {
            GestureArenaEntry entry = tracker.Entry;
            Cleanup(pointer);
            entry.Resolve(GestureDisposition.Rejected);
        }

        base.Dispose();
    }

    /// <summary>
    /// Ends a pointer that never produced a drag: the arena entry is rejected and a single cancel is
    /// reported. The tracker is removed first so the arena's own rejection callback is a no-op.
    /// </summary>
    private void ResolveWithoutDrag(DragTracker tracker, int pointer)
    {
        GestureArenaEntry entry = tracker.Entry;
        Cleanup(pointer);
        entry.Resolve(GestureDisposition.Rejected);
        OnCancel?.Invoke();
    }

    private void Cleanup(int pointer)
    {
        StopTrackingPointer(pointer);
        _trackers.Remove(pointer);
    }

    protected abstract double GetPrimaryValue(Point offset);

    protected abstract double GetCrossValue(Point offset);

    protected abstract Point GetPrimaryOffset(double value);

    /// <summary>Whether the given velocity estimate is fast and far enough to be a fling.</summary>
    protected abstract bool IsFlingGesture(VelocityEstimate estimate, PointerDeviceKind kind);

    /// <summary>
    /// The end details for a fling with the given estimate, or null when the gesture is not a fling.
    /// </summary>
    protected abstract DragEndDetails? ConsiderFling(VelocityEstimate estimate, PointerDeviceKind kind);

    /// <summary>The fling distance floor: the recognizer's own value, or the device's hit slop.</summary>
    protected double EffectiveMinFlingDistance => MinFlingDistance ?? TouchSlop;

    /// <summary>The fling velocity floor: the recognizer's own value, or the framework default.</summary>
    protected double EffectiveMinFlingVelocity => MinFlingVelocity ?? KMinFlingVelocity;

    /// <summary>The fling velocity ceiling: the recognizer's own value, or the framework default.</summary>
    protected double EffectiveMaxFlingVelocity => MaxFlingVelocity ?? KMaxFlingVelocity;

    /// <summary>Whether a drag that is dominated by the cross axis rejects this recognizer.</summary>
    protected virtual bool RejectsCrossAxisDrags => true;

    /// <summary>The primary delta reported to listeners; free-axis recognizers report no primary axis.</summary>
    protected virtual double GetReportedPrimaryDelta(Point delta) => GetPrimaryValue(delta);

    private void CheckDrag(DragTracker tracker)
    {
        if (tracker.Started)
        {
            return;
        }

        Point startPosition = DragStartBehavior == DragStartBehavior.Down
            ? tracker.InitialPosition
            : tracker.PendingPosition ?? tracker.LastPosition;
        tracker.LastPosition = startPosition;
        tracker.Started = true;
        OnStart?.Invoke(CreateStartDetails(tracker, startPosition));
    }

    private DragStartDetails CreateStartDetails(DragTracker tracker, Point globalPosition)
    {
        Point localPosition = tracker.InitialLocalPosition + (globalPosition - tracker.InitialPosition);
        return new DragStartDetails(
            GlobalPosition: globalPosition,
            LocalPosition: localPosition,
            SourceTimeStampUtc: tracker.PendingTimestampUtc ?? tracker.LastTimestampUtc,
            Kind: tracker.Kind);
    }

    private sealed class DragTracker
    {
        public DragTracker(PointerDownEvent @event, GestureArenaEntry entry, VelocityTracker velocityTracker)
        {
            InitialPosition = @event.Position;
            InitialLocalPosition = @event.LocalPosition;
            Kind = @event.Kind;
            LastPosition = @event.Position;
            LastTimestampUtc = @event.TimestampUtc;
            Entry = entry;
            VelocityTracker = velocityTracker;
            VelocityTracker.AddPosition(@event.TimestampUtc, @event.Position);
        }

        public Point InitialPosition { get; }

        public Point InitialLocalPosition { get; }

        public PointerDeviceKind Kind { get; }

        public DateTime LastTimestampUtc { get; private set; }

        public Point LastPosition { get; set; }

        public GestureArenaEntry Entry { get; }

        public VelocityTracker VelocityTracker { get; }

        public bool Accepted { get; set; }

        public Point? PendingPosition { get; set; }

        public DateTime? PendingTimestampUtc { get; set; }

        public bool HasDragThresholdBeenMet { get; set; }

        public bool Started { get; set; }

        public void RecordPosition(Point position, DateTime timestampUtc)
        {
            LastPosition = position;
            LastTimestampUtc = timestampUtc;
            VelocityTracker.AddPosition(timestampUtc, position);
        }
    }
}

public class HorizontalDragGestureRecognizer : DragGestureRecognizer
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

    protected override bool IsFlingGesture(VelocityEstimate estimate, PointerDeviceKind kind)
    {
        return Math.Abs(estimate.PixelsPerSecond.X) > EffectiveMinFlingVelocity
               && Math.Abs(estimate.Offset.X) > EffectiveMinFlingDistance;
    }

    protected override DragEndDetails? ConsiderFling(VelocityEstimate estimate, PointerDeviceKind kind)
    {
        if (!IsFlingGesture(estimate, kind))
        {
            return null;
        }

        double maxVelocity = EffectiveMaxFlingVelocity;
        double dx = Math.Clamp(estimate.PixelsPerSecond.X, -maxVelocity, maxVelocity);
        return new DragEndDetails(velocity: new Velocity(new Vector(dx, 0.0)), primaryVelocity: dx);
    }
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

    protected override bool IsFlingGesture(VelocityEstimate estimate, PointerDeviceKind kind)
    {
        double minVelocity = EffectiveMinFlingVelocity;
        double minDistance = EffectiveMinFlingDistance;
        return estimate.PixelsPerSecond.SquaredLength > minVelocity * minVelocity
               && estimate.Offset.SquaredLength > minDistance * minDistance;
    }

    protected override DragEndDetails? ConsiderFling(VelocityEstimate estimate, PointerDeviceKind kind)
    {
        if (!IsFlingGesture(estimate, kind))
        {
            return null;
        }

        Velocity velocity = new Velocity(estimate.PixelsPerSecond)
            .ClampMagnitude(EffectiveMinFlingVelocity, EffectiveMaxFlingVelocity);
        return new DragEndDetails(velocity: velocity, primaryVelocity: 0.0);
    }
}

public class VerticalDragGestureRecognizer : DragGestureRecognizer
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

    protected override bool IsFlingGesture(VelocityEstimate estimate, PointerDeviceKind kind)
    {
        return Math.Abs(estimate.PixelsPerSecond.Y) > EffectiveMinFlingVelocity
               && Math.Abs(estimate.Offset.Y) > EffectiveMinFlingDistance;
    }

    protected override DragEndDetails? ConsiderFling(VelocityEstimate estimate, PointerDeviceKind kind)
    {
        if (!IsFlingGesture(estimate, kind))
        {
            return null;
        }

        double maxVelocity = EffectiveMaxFlingVelocity;
        double dy = Math.Clamp(estimate.PixelsPerSecond.Y, -maxVelocity, maxVelocity);
        return new DragEndDetails(velocity: new Velocity(new Vector(0.0, dy)), primaryVelocity: dy);
    }
}
