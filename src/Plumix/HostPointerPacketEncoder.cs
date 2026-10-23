using Avalonia;
using Plumix.UI;

// C#-only infrastructure; no Dart parity source. Flutter's embedders report pointer input to the
// framework as physical-pixel `PointerData` that `PointerEventConverter` widens into logical events
// (`gestures/converter.dart`). An Avalonia host reads logical positions and already builds
// `PointerEvent`s, so this class does the engine side in reverse: it turns each event into the
// `PointerData` an engine would have reported, including the move/hover delta the engine computes.

namespace Plumix;

/// <summary>
/// Encodes the logical <see cref="PointerEvent"/>s a host builds from platform input as the physical
/// <see cref="PointerData"/> entries an engine reports through
/// <see cref="PlatformDispatcher.OnPointerDataPacket"/>.
/// </summary>
/// <remarks>
/// One encoder serves one host: it remembers each pointer's last position so move and hover entries
/// carry the delta from the previous report, as the engine's do.
/// </remarks>
public sealed class HostPointerPacketEncoder
{
    private readonly Dictionary<int, Point> _lastPositions = [];
    private readonly Dictionary<int, int> _pointerIdentifiers = [];
    private static int _nextPointerIdentifier;

    /// <summary>Encodes <paramref name="event"/> for a view of the given id and device pixel ratio.</summary>
    public PointerData Encode(PointerEvent @event, int viewId, double devicePixelRatio)
    {
        ArgumentNullException.ThrowIfNull(@event);
        (PointerChange change, PointerSignalKind? signal) = Classify(@event);

        // Flutter's engine starts a new gesture identity on every down/pan-zoom start. Avalonia's
        // pointer id identifies the physical mouse for its entire lifetime, including later clicks.
        // Reusing it here would join a second tap to the first tap's already-closed gesture arena.
        int identifier = 0;
        if (signal is null)
        {
            if (change is PointerChange.Down or PointerChange.PanZoomStart)
            {
                identifier = Interlocked.Increment(ref _nextPointerIdentifier);
                _pointerIdentifiers[@event.Pointer] = identifier;
            }
            else if (change is PointerChange.Move or PointerChange.Up or PointerChange.Cancel
                or PointerChange.PanZoomUpdate or PointerChange.PanZoomEnd)
            {
                _pointerIdentifiers.TryGetValue(@event.Pointer, out identifier);
            }

            if (change is PointerChange.Up or PointerChange.Cancel or PointerChange.Remove or PointerChange.PanZoomEnd)
            {
                _pointerIdentifiers.Remove(@event.Pointer);
            }
        }

        Point delta = default;
        if (change is PointerChange.Hover or PointerChange.Move)
        {
            delta = _lastPositions.TryGetValue(@event.Pointer, out Point previous)
                ? @event.Position - previous
                : default;
        }

        switch (change)
        {
            case PointerChange.Add or PointerChange.Down or PointerChange.Hover or PointerChange.Move
                when signal is null:
                _lastPositions[@event.Pointer] = @event.Position;
                break;
            case PointerChange.Up or PointerChange.Cancel or PointerChange.Remove when signal is null:
                _lastPositions.Remove(@event.Pointer);
                break;
        }

        double ratio = devicePixelRatio;
        var scroll = @event as PointerScrollEvent;
        var panZoom = @event as PointerPanZoomUpdateEvent;
        return new PointerData(
            viewId: viewId,
            embedderId: @event.EmbedderId,
            timeStamp: @event.TimestampUtc - default(DateTime),
            change: change,
            kind: @event.Kind,
            signalKind: signal,
            device: @event.Device,
            pointerIdentifier: identifier,
            physicalX: @event.Position.X * ratio,
            physicalY: @event.Position.Y * ratio,
            physicalDeltaX: delta.X * ratio,
            physicalDeltaY: delta.Y * ratio,
            buttons: @event.Buttons,
            obscured: @event.Obscured,
            synthesized: @event.Synthesized,
            pressure: @event.Pressure,
            pressureMin: @event.PressureMin,
            pressureMax: @event.PressureMax,
            distance: @event.Distance,
            distanceMax: @event.DistanceMax,
            size: @event.Size,
            radiusMajor: @event.RadiusMajor * ratio,
            radiusMinor: @event.RadiusMinor * ratio,
            radiusMin: @event.RadiusMin * ratio,
            radiusMax: @event.RadiusMax * ratio,
            orientation: @event.Orientation,
            tilt: @event.Tilt,
            platformData: @event.PlatformData,
            scrollDeltaX: (scroll?.ScrollDelta.X ?? 0.0) * ratio,
            scrollDeltaY: (scroll?.ScrollDelta.Y ?? 0.0) * ratio,
            panX: (panZoom?.Pan.X ?? 0.0) * ratio,
            panY: (panZoom?.Pan.Y ?? 0.0) * ratio,
            panDeltaX: (panZoom?.PanDelta.X ?? 0.0) * ratio,
            panDeltaY: (panZoom?.PanDelta.Y ?? 0.0) * ratio,
            scale: @event switch
            {
                PointerPanZoomUpdateEvent update => update.Scale,
                PointerScaleEvent scale => scale.Scale,
                _ => 0.0,
            },
            rotation: panZoom?.Rotation ?? 0.0,
            onRespond: scroll is null ? null : allow => scroll.Respond(allow));
    }

    /// <summary>Encodes <paramref name="event"/> and reports it as a one-entry packet.</summary>
    public void Report(PointerEvent @event, int viewId, double devicePixelRatio)
    {
        PointerData data = Encode(@event, viewId, devicePixelRatio);
        PlatformDispatcher.Instance.OnPointerDataPacket?.Invoke(new PointerDataPacket([data]));
    }

    private static (PointerChange Change, PointerSignalKind? Signal) Classify(PointerEvent @event)
    {
        return @event switch
        {
            PointerAddedEvent => (PointerChange.Add, null),
            PointerRemovedEvent => (PointerChange.Remove, null),
            PointerHoverEvent => (PointerChange.Hover, null),
            PointerDownEvent => (PointerChange.Down, null),
            PointerMoveEvent => (PointerChange.Move, null),
            PointerUpEvent => (PointerChange.Up, null),
            PointerCancelEvent => (PointerChange.Cancel, null),
            PointerPanZoomStartEvent => (PointerChange.PanZoomStart, null),
            PointerPanZoomUpdateEvent => (PointerChange.PanZoomUpdate, null),
            PointerPanZoomEndEvent => (PointerChange.PanZoomEnd, null),
            PointerScrollEvent => (PointerChange.Hover, PointerSignalKind.Scroll),
            PointerScrollInertiaCancelEvent => (PointerChange.Hover, PointerSignalKind.ScrollInertiaCancel),
            PointerScaleEvent => (PointerChange.Hover, PointerSignalKind.Scale),
            _ => throw new ArgumentException($"Unsupported pointer event {@event.GetType().Name}.", nameof(@event)),
        };
    }
}
