using Avalonia;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/recognizer.dart (approximate)

namespace Plumix.Gestures;

/// <summary>Dart's `AllowedButtonsFilter`: decides whether a button combination competes.</summary>
public delegate bool AllowedButtonsFilter(PointerButtons buttons);

public abstract class GestureRecognizer : IDisposable
{
    private readonly HashSet<int> _trackedPointers = [];
    private readonly Dictionary<int, (PointerDeviceKind Kind, PointerButtons Buttons)> _pointerToEventData = [];
    private readonly PointerRoute _route;
    private GestureArenaTeam? _team;

    protected GestureRecognizer(GestureBinding? binding = null)
    {
        Binding = binding ?? GestureBinding.Instance;
        _route = HandleRoutedEvent;
    }

    protected GestureBinding Binding { get; }

    protected PointerRouter PointerRouter => Binding.PointerRouter;

    protected GestureArenaManager GestureArena => Binding.GestureArena;

    /// <summary>The recognizer's owner, used only for diagnostics.</summary>
    public object? DebugOwner { get; set; }

    /// <summary>Device kinds this recognizer accepts; null accepts every kind.</summary>
    public IReadOnlySet<PointerDeviceKind>? SupportedDevices { get; set; }

    /// <summary>Host-supplied gesture tuning that overrides framework defaults such as touch slop.</summary>
    public DeviceGestureSettings? GestureSettings { get; set; }

    /// <summary>The arena team this recognizer competes through, when one is assigned.</summary>
    public GestureArenaTeam? Team
    {
        get => _team;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_team is not null || HasTrackedPointers)
            {
                throw new InvalidOperationException(
                    "A gesture recognizer's team can only be assigned once before it tracks pointers.");
            }

            _team = value;
        }
    }

    /// <summary>Dart's `allowedButtonsFilter`; the default accepts every button combination.</summary>
    public AllowedButtonsFilter AllowedButtonsFilter { get; set; } = DefaultButtonAcceptBehavior;

    /// <summary>A short description used by diagnostics.</summary>
    public virtual string DebugDescription => GetType().Name;

    public virtual void AddPointer(PointerDownEvent @event)
    {
        _pointerToEventData[@event.Pointer] = (@event.Kind, @event.Buttons);
        if (IsPointerAllowed(@event))
        {
            AddAllowedPointer(@event);
            return;
        }

        HandleNonAllowedPointer(@event);
    }

    /// <summary>Registers a pointer that passed <see cref="IsPointerAllowed"/>.</summary>
    protected virtual void AddAllowedPointer(PointerDownEvent @event)
    {
    }

    /// <summary>Called for a pointer this recognizer refuses to compete for.</summary>
    protected virtual void HandleNonAllowedPointer(PointerDownEvent @event)
    {
    }

    protected virtual bool IsPointerAllowed(PointerDownEvent @event)
    {
        return (SupportedDevices is null || SupportedDevices.Contains(@event.Kind))
               && AllowedButtonsFilter(@event.Buttons);
    }

    /// <summary>The device kind recorded when the given pointer went down.</summary>
    protected PointerDeviceKind GetKindForPointer(int pointer)
    {
        return _pointerToEventData.TryGetValue(pointer, out var data) ? data.Kind : PointerDeviceKind.Unknown;
    }

    /// <summary>The buttons recorded when the given pointer went down.</summary>
    protected PointerButtons GetButtonsForPointer(int pointer)
    {
        return _pointerToEventData.TryGetValue(pointer, out var data) ? data.Buttons : PointerButtons.None;
    }

    /// <summary>Dart's `invokeCallback`: runs a user callback, naming it if it throws.</summary>
    protected T? InvokeCallback<T>(string name, Func<T> callback)
    {
        try
        {
            return callback();
        }
        catch (Exception error)
        {
            throw new InvalidOperationException(
                $"Error while routing a pointer event to '{name}' of {DebugDescription}.",
                error);
        }
    }

    /// <summary>Dart's `invokeCallback` for callbacks with no return value.</summary>
    protected void InvokeCallback(string name, Action callback)
    {
        InvokeCallback<object?>(name, () =>
        {
            callback();
            return null;
        });
    }

    public virtual void Dispose()
    {
        foreach (int pointer in _trackedPointers.ToArray())
        {
            PointerRouter.RemoveRoute(pointer, _route);
        }

        _trackedPointers.Clear();
        _pointerToEventData.Clear();
    }

    protected virtual void StartTrackingPointer(int pointer)
    {
        if (_trackedPointers.Add(pointer))
        {
            PointerRouter.AddRoute(pointer, _route);
        }
    }

    protected virtual void StopTrackingPointer(int pointer)
    {
        if (_trackedPointers.Remove(pointer))
        {
            PointerRouter.RemoveRoute(pointer, _route);
        }
    }

    protected bool IsTrackingPointer(int pointer)
    {
        return _trackedPointers.Contains(pointer);
    }

    /// <summary>Whether any pointer is currently routed to this recognizer.</summary>
    protected bool HasTrackedPointers => _trackedPointers.Count > 0;

    /// <summary>Adds this recognizer, or its team, to the arena for <paramref name="pointer"/>.</summary>
    protected GestureArenaEntry AddPointerToArena(int pointer, IGestureArenaMember member)
    {
        return _team?.Add(pointer, member) ?? GestureArena.Add(pointer, member);
    }

    protected abstract void HandleEvent(PointerEvent @event);

    private static bool DefaultButtonAcceptBehavior(PointerButtons buttons) => true;

    private void HandleRoutedEvent(PointerEvent @event)
    {
        if (!IsTrackingPointer(@event.Pointer))
        {
            return;
        }

        HandleEvent(@event);
    }
}

/// <summary>
/// A pair of positions for the same point: one in the global (root) coordinate space and one in the
/// receiving render object's local space.
/// </summary>
public readonly record struct OffsetPair(Point Local, Point Global)
{
    public static OffsetPair Zero { get; } = new(default, default);

    /// <summary>The event's position pair.</summary>
    public static OffsetPair FromEventPosition(PointerEvent @event)
    {
        return new OffsetPair(Local: @event.LocalPosition, Global: @event.Position);
    }

    /// <summary>The event's delta pair.</summary>
    public static OffsetPair FromEventDelta(PointerEvent @event)
    {
        return new OffsetPair(Local: @event.LocalDelta, Global: @event.Delta);
    }

    public static OffsetPair operator +(OffsetPair left, OffsetPair right)
    {
        return new OffsetPair(Local: left.Local + right.Local, Global: left.Global + right.Global);
    }

    public static OffsetPair operator -(OffsetPair left, OffsetPair right)
    {
        return new OffsetPair(Local: left.Local - right.Local, Global: left.Global - right.Global);
    }
}

/// <summary>
/// A recognizer that tracks a single sequence of pointer events: it owns one arena entry per pointer
/// and learns when the last of them stops being tracked.
/// </summary>
public abstract class OneSequenceGestureRecognizer : GestureRecognizer, IGestureArenaMember
{
    private readonly Dictionary<int, GestureArenaEntry> _entries = [];

    protected OneSequenceGestureRecognizer(GestureBinding? binding = null) : base(binding)
    {
    }

    public abstract void AcceptGesture(int pointer);

    public abstract void RejectGesture(int pointer);

    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        StartTrackingPointer(@event.Pointer);
    }

    protected override void HandleNonAllowedPointer(PointerDownEvent @event)
    {
        Resolve(GestureDisposition.Rejected);
    }

    /// <summary>Called when the recognizer stops tracking its last pointer.</summary>
    protected abstract void DidStopTrackingLastPointer(int pointer);

    /// <summary>Resolves every pointer this recognizer is competing for.</summary>
    protected void Resolve(GestureDisposition disposition)
    {
        var localEntries = _entries.Values.ToList();
        _entries.Clear();
        foreach (GestureArenaEntry entry in localEntries)
        {
            entry.Resolve(disposition);
        }
    }

    /// <summary>Resolves a single pointer this recognizer is competing for.</summary>
    protected void ResolvePointer(int pointer, GestureDisposition disposition)
    {
        if (_entries.Remove(pointer, out GestureArenaEntry entry))
        {
            entry.Resolve(disposition);
        }
    }

    protected override void StartTrackingPointer(int pointer)
    {
        base.StartTrackingPointer(pointer);
        // A reused pointer id starts a fresh arena, so the entry is always replaced.
        _entries[pointer] = AddPointerToArena(pointer, this);
    }

    protected override void StopTrackingPointer(int pointer)
    {
        bool wasTracking = IsTrackingPointer(pointer);
        base.StopTrackingPointer(pointer);
        if (wasTracking && !HasTrackedPointers)
        {
            DidStopTrackingLastPointer(pointer);
        }
    }

    /// <summary>Stops tracking the pointer once its sequence has ended.</summary>
    protected void StopTrackingIfPointerNoLongerDown(PointerEvent @event)
    {
        if (@event is PointerUpEvent or PointerCancelEvent)
        {
            StopTrackingPointer(@event.Pointer);
        }
    }

    public override void Dispose()
    {
        Resolve(GestureDisposition.Rejected);
        base.Dispose();
        _entries.Clear();
    }
}

/// <summary>
/// Details for <see cref="TapGestureRecognizer.OnTapMove"/>: where a pointer that is still part of
/// a tap sequence has moved to. Ports Dart's `TapMoveDetails` (`gestures/tap.dart`).
/// </summary>
public readonly record struct TapMoveDetails(
    Point GlobalPosition,
    Point LocalPosition,
    Point Delta,
    PointerDeviceKind Kind);

public readonly record struct DragDownDetails(
    Point GlobalPosition,
    Point LocalPosition = default);

public readonly record struct DragStartDetails(
    Point GlobalPosition,
    Point LocalPosition = default,
    DateTime? SourceTimeStampUtc = null,
    PointerDeviceKind? Kind = null);

public readonly record struct DragUpdateDetails(
    Point GlobalPosition,
    Point LocalPosition,
    Point Delta,
    double PrimaryDelta,
    DateTime? SourceTimeStampUtc = null,
    PointerDeviceKind? Kind = null);

public readonly record struct Velocity(Vector PixelsPerSecond)
{
    public static Velocity Zero { get; } = new(default);

    public static Velocity operator -(Velocity value) => new(-value.PixelsPerSecond);

    public static Velocity operator -(Velocity left, Velocity right)
    {
        return new Velocity(left.PixelsPerSecond - right.PixelsPerSecond);
    }

    public static Velocity operator +(Velocity left, Velocity right)
    {
        return new Velocity(left.PixelsPerSecond + right.PixelsPerSecond);
    }

    public Velocity ClampMagnitude(double minimumValue, double maximumValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minimumValue);
        if (maximumValue < minimumValue)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumValue));
        }

        double magnitude = PixelsPerSecond.Length;
        if (magnitude > maximumValue)
        {
            return new Velocity(PixelsPerSecond / magnitude * maximumValue);
        }

        if (magnitude < minimumValue && magnitude > 0.0)
        {
            return new Velocity(PixelsPerSecond / magnitude * minimumValue);
        }

        return this;
    }
}

public readonly record struct DragEndDetails
{
    public DragEndDetails(double primaryVelocity) : this(
        velocity: new Velocity(new Vector(primaryVelocity, 0)),
        primaryVelocity: primaryVelocity)
    {
    }

    public DragEndDetails(
        Velocity velocity,
        double primaryVelocity,
        Point globalPosition = default,
        Point localPosition = default)
    {
        Velocity = velocity;
        PrimaryVelocity = primaryVelocity;
        GlobalPosition = globalPosition;
        LocalPosition = localPosition;
    }

    public Velocity Velocity { get; }

    public double PrimaryVelocity { get; }

    /// <summary>The global position the pointer was at when it stopped contacting the screen.</summary>
    public Point GlobalPosition { get; }

    /// <summary>The local position in the receiving object's coordinate system.</summary>
    public Point LocalPosition { get; }
}
