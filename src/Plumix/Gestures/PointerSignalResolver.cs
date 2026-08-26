using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/pointer_signal_resolver.dart

namespace Plumix.Gestures;

/// <summary>
/// The callback to register with a <see cref="PointerSignalResolver"/> to express interest in a
/// pointer signal event.
/// </summary>
public delegate void PointerSignalResolvedCallback(PointerSignalEvent @event);

/// <summary>
/// Mediates disputes over which listener should handle pointer signal events when multiple
/// listeners wish to handle those events. Signals are dispatched to the first registered handler,
/// which corresponds to the widget deepest in the hierarchy. Ports Dart's `PointerSignalResolver`.
/// </summary>
public sealed class PointerSignalResolver
{
    private PointerSignalResolvedCallback? _firstRegisteredCallback;
    private PointerSignalEvent? _currentEvent;

    private static bool IsSameEvent(PointerSignalEvent event1, PointerSignalEvent event2)
    {
        // Plumix events carry no `original`; the untransformed instance is the identity.
        return ReferenceEquals(event1.Original ?? event1, event2.Original ?? event2);
    }

    /// <summary>
    /// Registers interest in handling <paramref name="event"/>. Only the first registration for a
    /// given event wins; <see cref="Resolve"/> invokes it once dispatch completes.
    /// </summary>
    public void Register(PointerSignalEvent @event, PointerSignalResolvedCallback callback)
    {
        if (_currentEvent is not null && !IsSameEvent(_currentEvent, @event))
        {
            throw new InvalidOperationException(
                "Only one event disambiguation can be in flight at a time.");
        }

        if (_firstRegisteredCallback is not null)
        {
            return;
        }

        _currentEvent = @event;
        _firstRegisteredCallback = callback;
    }

    /// <summary>
    /// Resolves the event, calling the first registered callback if there was one. Called by
    /// <see cref="GestureBinding"/> after the framework finishes dispatching the signal event.
    /// </summary>
    public void Resolve(PointerSignalEvent @event)
    {
        if (_firstRegisteredCallback is null)
        {
            // Nothing in the framework/app wants to handle the event; allow the platform to
            // trigger any default native actions.
            @event.Respond(allowPlatformDefault: true);
            return;
        }

        try
        {
            _firstRegisteredCallback(_currentEvent!);
        }
        finally
        {
            _firstRegisteredCallback = null;
            _currentEvent = null;
        }
    }
}
