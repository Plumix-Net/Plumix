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

    public abstract void AddPointer(PointerDownEvent @event);

    public virtual void Dispose()
    {
        foreach (var pointer in _trackedPointers.ToArray())
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

public readonly record struct DragStartDetails(Point GlobalPosition);

public readonly record struct DragUpdateDetails(
    Point GlobalPosition,
    Point LocalPosition,
    Point Delta,
    double PrimaryDelta);

public readonly record struct DragEndDetails(double PrimaryVelocity);
