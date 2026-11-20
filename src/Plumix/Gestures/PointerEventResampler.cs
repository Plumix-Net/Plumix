using Avalonia;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/resampler.dart

namespace Plumix.Gestures;

/// <summary>Resamples one touch-device pointer sequence at frame-aligned times.</summary>
public sealed class PointerEventResampler
{
    private readonly Queue<PointerEvent> _queuedEvents = [];
    private PointerEvent? _last;
    private PointerEvent? _next;
    private Offset _position;
    private bool _isTracked;
    private bool _isDown;
    private int _pointerIdentifier;
    private PointerButtons _hasButtons;

    public bool HasPendingEvents => _queuedEvents.Count > 0;

    public bool IsTracked => _isTracked;

    public bool IsDown => _isDown;

    public void AddEvent(PointerEvent @event)
    {
        _queuedEvents.Enqueue(@event);
    }

    public void Sample(DateTime sampleTime, DateTime nextSampleTime, Action<PointerEvent> callback)
    {
        ProcessPointerEvents(sampleTime);
        DequeueAndSampleNonHoverOrMovePointerEventsUntil(sampleTime, nextSampleTime, callback);
        if (_isTracked)
        {
            SamplePointerPosition(sampleTime, callback);
        }
    }

    public void Stop(Action<PointerEvent> callback)
    {
        while (_queuedEvents.TryDequeue(out PointerEvent? @event))
        {
            callback(@event);
        }

        _pointerIdentifier = 0;
        _isDown = false;
        _isTracked = false;
        _position = default;
        _next = null;
        _last = null;
    }

    private Offset PositionAt(DateTime sampleTime)
    {
        Offset position = _next?.Position ?? default;
        if (_next is not null && _last is not null
            && _next.TimestampUtc > sampleTime && _next.TimestampUtc > _last.TimestampUtc)
        {
            double interval = (_next.TimestampUtc - _last.TimestampUtc).TotalMilliseconds;
            double scalar = (sampleTime - _last.TimestampUtc).TotalMilliseconds / interval;
            position = new Offset(
                _last.Position.Dx + (position.Dx - _last.Position.Dx) * scalar,
                _last.Position.Dy + (position.Dy - _last.Position.Dy) * scalar);
        }

        return position;
    }

    private void ProcessPointerEvents(DateTime sampleTime)
    {
        foreach (PointerEvent @event in _queuedEvents)
        {
            if (@event.TimestampUtc <= sampleTime || _last is null)
            {
                _last = @event;
                _next = @event;
                continue;
            }

            if (_next is null || _next.TimestampUtc < sampleTime)
            {
                _next = @event;
                break;
            }
        }
    }

    private void DequeueAndSampleNonHoverOrMovePointerEventsUntil(
        DateTime sampleTime,
        DateTime nextSampleTime,
        Action<PointerEvent> callback)
    {
        DateTime endTime = sampleTime;
        foreach (PointerEvent @event in _queuedEvents)
        {
            if (@event.TimestampUtc <= sampleTime)
            {
                continue;
            }

            if (@event.TimestampUtc >= nextSampleTime)
            {
                break;
            }

            if (@event is PointerUpEvent or PointerRemovedEvent)
            {
                endTime = @event.TimestampUtc;
                continue;
            }

            if (@event is not PointerMoveEvent and not PointerHoverEvent)
            {
                break;
            }
        }

        while (_queuedEvents.TryPeek(out PointerEvent? @event) && @event.TimestampUtc <= endTime)
        {
            bool wasTracked = _isTracked;
            bool wasDown = _isDown;
            PointerButtons hadButtons = _hasButtons;

            _isTracked = @event is not PointerRemovedEvent;
            _isDown = @event.Down;
            _hasButtons = @event.Buttons;
            Offset position = PositionAt(sampleTime);
            if (_isTracked && !wasTracked)
            {
                _position = position;
            }

            int pointerIdentifier = @event.Pointer;
            if (wasDown && _pointerIdentifier != pointerIdentifier)
            {
                throw new InvalidOperationException("The pointer identifier changed while down.");
            }

            _pointerIdentifier = pointerIdentifier;
            if (@event is not PointerMoveEvent and not PointerHoverEvent)
            {
                if (position != _position)
                {
                    Offset delta = position - _position;
                    callback(ToMoveOrHoverEvent(
                        @event, position, delta, sampleTime, wasDown, hadButtons));
                    _position = position;
                }

                callback(@event.CopyWith(
                    position: position,
                    delta: default(Offset),
                    pointer: pointerIdentifier,
                    timestampUtc: sampleTime).MarkResampled());
            }

            _queuedEvents.Dequeue();
        }
    }

    private void SamplePointerPosition(DateTime sampleTime, Action<PointerEvent> callback)
    {
        Offset position = PositionAt(sampleTime);
        if (position != _position && _next is not null)
        {
            Offset delta = position - _position;
            callback(ToMoveOrHoverEvent(
                _next, position, delta, sampleTime, _isDown, _hasButtons));
            _position = position;
        }
    }

    private static PointerEvent ToHoverEvent(PointerEvent @event, Offset position, Offset delta, DateTime timeStamp)
    {
        return new PointerHoverEvent(
            viewId: @event.ViewId,
            timestampUtc: timeStamp,
            kind: @event.Kind,
            device: @event.Device,
            position: position,
            delta: delta,
            buttons: @event.Buttons,
            obscured: @event.Obscured,
            pressureMin: @event.PressureMin,
            pressureMax: @event.PressureMax,
            distance: @event.Distance,
            distanceMax: @event.DistanceMax,
            size: @event.Size,
            radiusMajor: @event.RadiusMajor,
            radiusMinor: @event.RadiusMinor,
            radiusMin: @event.RadiusMin,
            radiusMax: @event.RadiusMax,
            orientation: @event.Orientation,
            tilt: @event.Tilt,
            synthesized: @event.Synthesized,
            embedderId: @event.EmbedderId);
    }

    private static PointerEvent ToMoveEvent(
        PointerEvent @event,
        Offset position,
        Offset delta,
        int pointerIdentifier,
        DateTime timeStamp,
        PointerButtons buttons)
    {
        return new PointerMoveEvent(
            viewId: @event.ViewId,
            timestampUtc: timeStamp,
            pointer: pointerIdentifier,
            kind: @event.Kind,
            device: @event.Device,
            position: position,
            delta: delta,
            buttons: buttons,
            obscured: @event.Obscured,
            pressure: @event.Pressure,
            pressureMin: @event.PressureMin,
            pressureMax: @event.PressureMax,
            distanceMax: @event.DistanceMax,
            size: @event.Size,
            radiusMajor: @event.RadiusMajor,
            radiusMinor: @event.RadiusMinor,
            radiusMin: @event.RadiusMin,
            radiusMax: @event.RadiusMax,
            orientation: @event.Orientation,
            tilt: @event.Tilt,
            platformData: @event.PlatformData,
            synthesized: @event.Synthesized,
            embedderId: @event.EmbedderId);
    }

    private PointerEvent ToMoveOrHoverEvent(
        PointerEvent @event,
        Offset position,
        Offset delta,
        DateTime timeStamp,
        bool isDown,
        PointerButtons buttons)
    {
        PointerEvent result = isDown
            ? ToMoveEvent(@event, position, delta, _pointerIdentifier, timeStamp, buttons)
            : ToHoverEvent(@event, position, delta, timeStamp);
        return result.MarkResampled();
    }
}
