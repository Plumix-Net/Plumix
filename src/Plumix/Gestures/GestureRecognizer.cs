using Avalonia;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/recognizer.dart (approximate)

namespace Plumix.Gestures;

public abstract class GestureRecognizer : IDisposable
{
    private readonly HashSet<int> _trackedPointers = [];
    private readonly PointerRoute _route;

    protected GestureRecognizer(GestureBinding? binding = null)
    {
        Binding = binding ?? GestureBinding.Instance;
        _route = HandleRoutedEvent;
    }

    protected GestureBinding Binding { get; }

    protected PointerRouter PointerRouter => Binding.PointerRouter;

    protected GestureArenaManager GestureArena => Binding.GestureArena;

    /// <summary>Device kinds this recognizer accepts; null accepts every kind.</summary>
    public IReadOnlySet<PointerDeviceKind>? SupportedDevices { get; set; }

    /// <summary>Host-supplied gesture tuning that overrides framework defaults such as touch slop.</summary>
    public DeviceGestureSettings? GestureSettings { get; set; }

    public abstract void AddPointer(PointerDownEvent @event);

    protected virtual bool IsPointerAllowed(PointerDownEvent @event)
    {
        return SupportedDevices is null || SupportedDevices.Contains(@event.Kind);
    }

    public virtual void Dispose()
    {
        foreach (int pointer in _trackedPointers.ToArray())
        {
            PointerRouter.RemoveRoute(pointer, _route);
        }

        _trackedPointers.Clear();
    }

    protected void StartTrackingPointer(int pointer)
    {
        if (_trackedPointers.Add(pointer))
        {
            PointerRouter.AddRoute(pointer, _route);
        }
    }

    protected void StopTrackingPointer(int pointer)
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

    protected abstract void HandleEvent(PointerEvent @event);

    private void HandleRoutedEvent(PointerEvent @event)
    {
        if (!IsTrackingPointer(@event.Pointer))
        {
            return;
        }

        HandleEvent(@event);
    }
}

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
}

public readonly record struct DragEndDetails
{
    public DragEndDetails(double primaryVelocity) : this(
        velocity: new Velocity(new Vector(primaryVelocity, 0)),
        primaryVelocity: primaryVelocity)
    {
    }

    public DragEndDetails(Velocity velocity, double primaryVelocity)
    {
        Velocity = velocity;
        PrimaryVelocity = primaryVelocity;
    }

    public Velocity Velocity { get; }

    public double PrimaryVelocity { get; }
}
