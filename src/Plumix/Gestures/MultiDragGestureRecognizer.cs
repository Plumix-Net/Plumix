using Avalonia;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/multidrag.dart

namespace Plumix.Gestures;

/// <summary>The client owned by one pointer accepted by a multi-drag recognizer.</summary>
public abstract class Drag
{
    public abstract void Update(DragUpdateDetails details);

    public abstract void End(DragEndDetails details);

    public abstract void Cancel();
}

/// <summary>
/// Signature for when <see cref="MultiDragGestureRecognizer"/> recognizes the start of a drag gesture.
/// </summary>
public delegate Drag? GestureMultiDragStartCallback(Point position);

/// <summary>Per-pointer state for a <see cref="MultiDragGestureRecognizer"/>.</summary>
public abstract class MultiDragPointerState : IDisposable
{
    private readonly VelocityTracker _velocityTracker;
    private Point _lastPosition;
    private Drag? _client;
    private DateTime? _lastPendingEventTimestampUtc;
    private GestureArenaEntry? _arenaEntry;

    protected MultiDragPointerState(
        Point initialPosition,
        PointerDeviceKind kind,
        DeviceGestureSettings? gestureSettings)
    {
        InitialPosition = initialPosition;
        Kind = kind;
        GestureSettings = gestureSettings;
        _velocityTracker = new VelocityTracker(kind);
        _lastPosition = initialPosition;
    }

    /// <summary>Device settings used to resolve hit slop for this pointer.</summary>
    public DeviceGestureSettings? GestureSettings { get; }

    /// <summary>The global coordinates of the pointer when the pointer contacted the screen.</summary>
    public Point InitialPosition { get; }

    /// <summary>The kind of pointer performing the multi-drag gesture.</summary>
    public PointerDeviceKind Kind { get; }

    /// <summary>
    /// The offset the pointer has moved since it contacted the screen, while this state has neither
    /// been accepted nor rejected; null once a client is attached or the pointer sequence ends.
    /// </summary>
    public Point? PendingDelta { get; private set; } = default(Point);

    internal void SetArenaEntry(GestureArenaEntry entry) => _arenaEntry = entry;

    /// <summary>Resolves this pointer's arena entry.</summary>
    protected void Resolve(GestureDisposition disposition) => _arenaEntry?.Resolve(disposition);

    internal void Move(PointerMoveEvent @event)
    {
        if (_arenaEntry is null)
        {
            return;
        }

        _velocityTracker.AddPosition(@event.TimestampUtc, @event.Position);
        Point delta = new(@event.Position.X - _lastPosition.X, @event.Position.Y - _lastPosition.Y);
        _lastPosition = @event.Position;
        if (_client is not null)
        {
            _client.Update(new DragUpdateDetails(
                GlobalPosition: @event.Position,
                LocalPosition: @event.LocalPosition,
                Delta: delta,
                PrimaryDelta: 0.0,
                SourceTimeStampUtc: @event.TimestampUtc,
                Kind: Kind));
        }
        else
        {
            PendingDelta = new Point(PendingDelta!.Value.X + delta.X, PendingDelta.Value.Y + delta.Y);
            _lastPendingEventTimestampUtc = @event.TimestampUtc;
            CheckForResolutionAfterMove();
        }
    }

    /// <summary>
    /// Override this to call <see cref="Resolve"/> once the pointer has moved far enough that the
    /// gesture should be recognized or rejected; called after each pending-delta update.
    /// </summary>
    protected virtual void CheckForResolutionAfterMove()
    {
    }

    /// <summary>
    /// Called when the gesture was accepted in the arena. Call <paramref name="starter"/>, with this
    /// pointer's initial position, when the drag should actually start (possibly later, as
    /// <see cref="DelayedMultiDragGestureRecognizer"/> does).
    /// </summary>
    protected internal abstract void Accepted(GestureMultiDragStartCallback starter);

    /// <summary>Called when the gesture was rejected in the arena.</summary>
    protected internal virtual void Rejected()
    {
        PendingDelta = null;
        _lastPendingEventTimestampUtc = null;
        _arenaEntry = null;
    }

    internal void StartDrag(Drag client)
    {
        _client = client;
        var details = new DragUpdateDetails(
            GlobalPosition: InitialPosition,
            LocalPosition: InitialPosition,
            Delta: PendingDelta ?? default,
            PrimaryDelta: 0.0,
            SourceTimeStampUtc: _lastPendingEventTimestampUtc,
            Kind: Kind);
        PendingDelta = null;
        _lastPendingEventTimestampUtc = null;
        _client.Update(details);
    }

    internal void Up()
    {
        if (_client is not null)
        {
            var details = new DragEndDetails(velocity: _velocityTracker.GetVelocity(), primaryVelocity: 0.0);
            Drag client = _client;
            _client = null;
            client.End(details);
        }
        else
        {
            PendingDelta = null;
            _lastPendingEventTimestampUtc = null;
        }
    }

    internal void Cancel()
    {
        if (_client is not null)
        {
            Drag client = _client;
            _client = null;
            client.Cancel();
        }
        else
        {
            PendingDelta = null;
            _lastPendingEventTimestampUtc = null;
        }
    }

    public virtual void Dispose()
    {
        _arenaEntry?.Resolve(GestureDisposition.Rejected);
        _arenaEntry = null;
    }

    /// <summary>The Euclidean magnitude of a delta, like Dart's `Offset.distance`.</summary>
    protected static double DeltaDistance(Point delta) =>
        Math.Sqrt((delta.X * delta.X) + (delta.Y * delta.Y));
}

/// <summary>Recognizes movement on a per-pointer basis, so multiple drags can run at the same time.</summary>
public abstract class MultiDragGestureRecognizer : GestureRecognizer, IGestureArenaMember
{
    private readonly Dictionary<int, MultiDragPointerState> _pointers = [];

    protected MultiDragGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
        AllowedButtonsFilter = DefaultButtonAcceptBehavior;
    }

    /// <summary>Called when this class recognizes the start of a drag gesture for a pointer.</summary>
    public GestureMultiDragStartCallback? OnStart { get; set; }

    private static bool DefaultButtonAcceptBehavior(PointerButtons buttons) =>
        buttons == PointerButtons.Primary;

    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        if (_pointers.ContainsKey(@event.Pointer))
        {
            return;
        }

        MultiDragPointerState state = CreateNewPointerState(@event);
        _pointers[@event.Pointer] = state;
        StartTrackingPointer(@event.Pointer);
        state.SetArenaEntry(AddPointerToArena(@event.Pointer, this));
    }

    /// <summary>Creates the specific state object tracking one new pointer.</summary>
    protected abstract MultiDragPointerState CreateNewPointerState(PointerDownEvent @event);

    protected override void HandleEvent(PointerEvent @event)
    {
        if (!_pointers.TryGetValue(@event.Pointer, out MultiDragPointerState? state))
        {
            return;
        }

        switch (@event)
        {
            case PointerMoveEvent move:
                state.Move(move);
                break;
            case PointerUpEvent:
                state.Up();
                RemoveState(@event.Pointer);
                break;
            case PointerCancelEvent:
                state.Cancel();
                RemoveState(@event.Pointer);
                break;
        }
    }

    public void AcceptGesture(int pointer)
    {
        if (!_pointers.TryGetValue(pointer, out MultiDragPointerState? state))
        {
            // We might already have canceled this drag if the up comes before the accept.
            return;
        }

        state.Accepted(initialPosition => StartDrag(initialPosition, pointer));
    }

    private Drag? StartDrag(Point initialPosition, int pointer)
    {
        MultiDragPointerState state = _pointers[pointer];
        Drag? drag = null;
        if (OnStart is not null)
        {
            drag = InvokeCallback("onStart", () => OnStart(initialPosition));
        }

        if (drag is not null)
        {
            state.StartDrag(drag);
        }
        else
        {
            RemoveState(pointer);
        }

        return drag;
    }

    public void RejectGesture(int pointer)
    {
        if (_pointers.TryGetValue(pointer, out MultiDragPointerState? state))
        {
            state.Rejected();
            RemoveState(pointer);
        }
    }

    private void RemoveState(int pointer)
    {
        if (!_pointers.Remove(pointer, out MultiDragPointerState? state))
        {
            return;
        }

        StopTrackingPointer(pointer);
        state.Dispose();
    }

    public override void Dispose()
    {
        foreach (int pointer in _pointers.Keys.ToList())
        {
            RemoveState(pointer);
        }

        base.Dispose();
    }
}

internal sealed class ImmediatePointerState : MultiDragPointerState
{
    public ImmediatePointerState(
        Point initialPosition,
        PointerDeviceKind kind,
        DeviceGestureSettings? gestureSettings) : base(initialPosition, kind, gestureSettings)
    {
    }

    protected override void CheckForResolutionAfterMove()
    {
        if (DeltaDistance(PendingDelta!.Value) > PointerEventUtils.ComputeHitSlop(Kind, GestureSettings))
        {
            Resolve(GestureDisposition.Accepted);
        }
    }

    protected internal override void Accepted(GestureMultiDragStartCallback starter) =>
        starter(InitialPosition);
}

/// <summary>Recognizes movement both horizontally and vertically on a per-pointer basis.</summary>
public sealed class ImmediateMultiDragGestureRecognizer : MultiDragGestureRecognizer
{
    public ImmediateMultiDragGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    protected override MultiDragPointerState CreateNewPointerState(PointerDownEvent @event) =>
        new ImmediatePointerState(@event.Position, @event.Kind, GestureSettings);

    public override string DebugDescription => "multidrag";
}

internal sealed class HorizontalPointerState : MultiDragPointerState
{
    public HorizontalPointerState(
        Point initialPosition,
        PointerDeviceKind kind,
        DeviceGestureSettings? gestureSettings) : base(initialPosition, kind, gestureSettings)
    {
    }

    protected override void CheckForResolutionAfterMove()
    {
        if (Math.Abs(PendingDelta!.Value.X) > PointerEventUtils.ComputeHitSlop(Kind, GestureSettings))
        {
            Resolve(GestureDisposition.Accepted);
        }
    }

    protected internal override void Accepted(GestureMultiDragStartCallback starter) =>
        starter(InitialPosition);
}

/// <summary>Recognizes movement in the horizontal direction on a per-pointer basis.</summary>
public sealed class HorizontalMultiDragGestureRecognizer : MultiDragGestureRecognizer
{
    public HorizontalMultiDragGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    protected override MultiDragPointerState CreateNewPointerState(PointerDownEvent @event) =>
        new HorizontalPointerState(@event.Position, @event.Kind, GestureSettings);

    public override string DebugDescription => "horizontal multidrag";
}

internal sealed class VerticalPointerState : MultiDragPointerState
{
    public VerticalPointerState(
        Point initialPosition,
        PointerDeviceKind kind,
        DeviceGestureSettings? gestureSettings) : base(initialPosition, kind, gestureSettings)
    {
    }

    protected override void CheckForResolutionAfterMove()
    {
        if (Math.Abs(PendingDelta!.Value.Y) > PointerEventUtils.ComputeHitSlop(Kind, GestureSettings))
        {
            Resolve(GestureDisposition.Accepted);
        }
    }

    protected internal override void Accepted(GestureMultiDragStartCallback starter) =>
        starter(InitialPosition);
}

/// <summary>Recognizes movement in the vertical direction on a per-pointer basis.</summary>
public sealed class VerticalMultiDragGestureRecognizer : MultiDragGestureRecognizer
{
    public VerticalMultiDragGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    protected override MultiDragPointerState CreateNewPointerState(PointerDownEvent @event) =>
        new VerticalPointerState(@event.Position, @event.Kind, GestureSettings);

    public override string DebugDescription => "vertical multidrag";
}

internal sealed class DelayedPointerState : MultiDragPointerState
{
    private GestureTimer? _timer;
    private GestureMultiDragStartCallback? _starter;

    public DelayedPointerState(
        Point initialPosition,
        TimeSpan delay,
        PointerDeviceKind kind,
        DeviceGestureSettings? gestureSettings) : base(initialPosition, kind, gestureSettings)
    {
        _timer = GestureTimer.Start(delay, DelayPassed);
    }

    private void DelayPassed()
    {
        _timer = null;
        if (_starter is not null)
        {
            GestureMultiDragStartCallback starter = _starter;
            _starter = null;
            starter(InitialPosition);
        }
        else
        {
            Resolve(GestureDisposition.Accepted);
        }
    }

    private void EnsureTimerStopped()
    {
        _timer?.Cancel();
        _timer = null;
    }

    protected internal override void Accepted(GestureMultiDragStartCallback starter)
    {
        if (_timer is null)
        {
            // If we've been accepted by the arena and the timer has expired, we can start the drag
            // right away.
            starter(InitialPosition);
        }
        else
        {
            // Wait for the drag to start. If a competing gesture would like to win the arena, it
            // has to wait.
            _starter = starter;
        }
    }

    protected override void CheckForResolutionAfterMove()
    {
        if (_timer is null)
        {
            // If we've been accepted by the arena and the timer has expired, the drag detail events
            // are dispatched to the drag client directly instead of accumulating in PendingDelta.
            return;
        }

        // The drag is rejected if the pointer moves past the hit slop before the delay expires.
        if (DeltaDistance(PendingDelta!.Value) > PointerEventUtils.ComputeHitSlop(Kind, GestureSettings))
        {
            Resolve(GestureDisposition.Rejected);
            EnsureTimerStopped();
        }
    }

    public override void Dispose()
    {
        EnsureTimerStopped();
        base.Dispose();
    }
}

/// <summary>
/// Recognizes movement on a per-pointer basis after a stationary long-press delay.
/// </summary>
public sealed class DelayedMultiDragGestureRecognizer : MultiDragGestureRecognizer
{
    public DelayedMultiDragGestureRecognizer(
        TimeSpan? delay = null,
        GestureBinding? binding = null) : base(binding)
    {
        Delay = delay ?? TimeSpan.FromMilliseconds(500);
        if (Delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay));
        }
    }

    /// <summary>The amount of time the pointer must remain in the same place for the drag to be recognized.</summary>
    public TimeSpan Delay { get; }

    protected override MultiDragPointerState CreateNewPointerState(PointerDownEvent @event) =>
        new DelayedPointerState(@event.Position, Delay, @event.Kind, GestureSettings);

    public override string DebugDescription => "long multidrag";
}
