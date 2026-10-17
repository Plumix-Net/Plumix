using Avalonia;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/converter.dart

namespace Plumix.Gestures;

/// <summary>
/// Dart's <c>DevicePixelRatioGetter</c>: the device pixel ratio of the view identified by
/// <paramref name="viewId"/>, or <see langword="null"/> when no such view exists.
/// </summary>
public delegate double? DevicePixelRatioGetter(int viewId);

/// <summary>
/// Converts from engine pointer data to framework pointer events. Dart's
/// <c>PointerEventConverter</c>: it takes the <see cref="PointerData"/> of a
/// <see cref="PointerDataPacket"/> received through <see cref="PlatformDispatcher.OnPointerDataPacket"/>
/// and produces <see cref="PointerEvent"/> values in logical pixels.
/// </summary>
/// <remarks>
/// Dart's <c>timeStamp</c> is a <c>Duration</c>; the events carry it on the timeline of
/// <see cref="PointerEvent.TimestampUtc"/>, where <c>Duration.zero</c> is <c>default(DateTime)</c>.
/// </remarks>
public static class PointerEventConverter
{
    /// <summary>
    /// Expands the given packet of pointer data into a sequence of framework pointer events. The
    /// sequence is lazy, like Dart's <c>Iterable</c>.
    /// </summary>
    /// <param name="data">The pointer data of a packet.</param>
    /// <param name="devicePixelRatioForView">
    /// Looks up the device pixel ratio of the view a datum belongs to, to convert it from physical to
    /// logical pixels. An entry whose view no longer exists is dropped.
    /// </param>
    public static IEnumerable<PointerEvent> Expand(
        IEnumerable<PointerData> data,
        DevicePixelRatioGetter devicePixelRatioForView)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(devicePixelRatioForView);
        foreach (PointerData datum in data)
        {
            if (datum.SignalKind == PointerSignalKind.Unknown)
            {
                continue;
            }

            if (devicePixelRatioForView(datum.ViewId) is not { } devicePixelRatio)
            {
                // View doesn't exist anymore.
                continue;
            }

            PointerEvent? @event = Convert(datum, devicePixelRatio);
            if (@event is not null)
            {
                yield return @event;
            }
        }
    }

    private static PointerEvent? Convert(PointerData datum, double devicePixelRatio)
    {
        var position = new Point(datum.PhysicalX / devicePixelRatio, datum.PhysicalY / devicePixelRatio);
        var delta = new Point(datum.PhysicalDeltaX / devicePixelRatio, datum.PhysicalDeltaY / devicePixelRatio);
        double radiusMinor = ToLogicalPixels(datum.RadiusMinor, devicePixelRatio);
        double radiusMajor = ToLogicalPixels(datum.RadiusMajor, devicePixelRatio);
        double radiusMin = ToLogicalPixels(datum.RadiusMin, devicePixelRatio);
        double radiusMax = ToLogicalPixels(datum.RadiusMax, devicePixelRatio);
        DateTime timeStamp = default(DateTime) + datum.TimeStamp;
        PointerDeviceKind kind = datum.Kind;
        switch (datum.SignalKind ?? PointerSignalKind.None)
        {
            case PointerSignalKind.None:
                switch (datum.Change)
                {
                    case PointerChange.Add:
                        return new PointerAddedEvent(
                            viewId: datum.ViewId,
                            timestampUtc: timeStamp,
                            kind: kind,
                            device: datum.Device,
                            position: position,
                            obscured: datum.Obscured,
                            pressureMin: datum.PressureMin,
                            pressureMax: datum.PressureMax,
                            distance: datum.Distance,
                            distanceMax: datum.DistanceMax,
                            radiusMin: radiusMin,
                            radiusMax: radiusMax,
                            orientation: datum.Orientation,
                            tilt: datum.Tilt,
                            embedderId: datum.EmbedderId);
                    case PointerChange.Hover:
                        return new PointerHoverEvent(
                            viewId: datum.ViewId,
                            timestampUtc: timeStamp,
                            kind: kind,
                            device: datum.Device,
                            position: position,
                            delta: delta,
                            buttons: datum.Buttons,
                            obscured: datum.Obscured,
                            pressureMin: datum.PressureMin,
                            pressureMax: datum.PressureMax,
                            distance: datum.Distance,
                            distanceMax: datum.DistanceMax,
                            size: datum.Size,
                            radiusMajor: radiusMajor,
                            radiusMinor: radiusMinor,
                            radiusMin: radiusMin,
                            radiusMax: radiusMax,
                            orientation: datum.Orientation,
                            tilt: datum.Tilt,
                            synthesized: datum.Synthesized,
                            embedderId: datum.EmbedderId);
                    case PointerChange.Down:
                        return new PointerDownEvent(
                            viewId: datum.ViewId,
                            timestampUtc: timeStamp,
                            pointer: datum.PointerIdentifier,
                            kind: kind,
                            device: datum.Device,
                            position: position,
                            buttons: SynthesiseDownButtons(datum.Buttons, kind),
                            obscured: datum.Obscured,
                            pressure: datum.Pressure,
                            pressureMin: datum.PressureMin,
                            pressureMax: datum.PressureMax,
                            distanceMax: datum.DistanceMax,
                            size: datum.Size,
                            radiusMajor: radiusMajor,
                            radiusMinor: radiusMinor,
                            radiusMin: radiusMin,
                            radiusMax: radiusMax,
                            orientation: datum.Orientation,
                            tilt: datum.Tilt,
                            embedderId: datum.EmbedderId);
                    case PointerChange.Move:
                        return new PointerMoveEvent(
                            viewId: datum.ViewId,
                            timestampUtc: timeStamp,
                            pointer: datum.PointerIdentifier,
                            kind: kind,
                            device: datum.Device,
                            position: position,
                            delta: delta,
                            buttons: SynthesiseDownButtons(datum.Buttons, kind),
                            obscured: datum.Obscured,
                            pressure: datum.Pressure,
                            pressureMin: datum.PressureMin,
                            pressureMax: datum.PressureMax,
                            distanceMax: datum.DistanceMax,
                            size: datum.Size,
                            radiusMajor: radiusMajor,
                            radiusMinor: radiusMinor,
                            radiusMin: radiusMin,
                            radiusMax: radiusMax,
                            orientation: datum.Orientation,
                            tilt: datum.Tilt,
                            platformData: datum.PlatformData,
                            synthesized: datum.Synthesized,
                            embedderId: datum.EmbedderId);
                    case PointerChange.Up:
                        return new PointerUpEvent(
                            viewId: datum.ViewId,
                            timestampUtc: timeStamp,
                            pointer: datum.PointerIdentifier,
                            kind: kind,
                            device: datum.Device,
                            position: position,
                            buttons: datum.Buttons,
                            obscured: datum.Obscured,
                            pressure: datum.Pressure,
                            pressureMin: datum.PressureMin,
                            pressureMax: datum.PressureMax,
                            distance: datum.Distance,
                            distanceMax: datum.DistanceMax,
                            size: datum.Size,
                            radiusMajor: radiusMajor,
                            radiusMinor: radiusMinor,
                            radiusMin: radiusMin,
                            radiusMax: radiusMax,
                            orientation: datum.Orientation,
                            tilt: datum.Tilt,
                            embedderId: datum.EmbedderId);
                    case PointerChange.Cancel:
                        return new PointerCancelEvent(
                            viewId: datum.ViewId,
                            timestampUtc: timeStamp,
                            pointer: datum.PointerIdentifier,
                            kind: kind,
                            device: datum.Device,
                            position: position,
                            buttons: datum.Buttons,
                            obscured: datum.Obscured,
                            pressureMin: datum.PressureMin,
                            pressureMax: datum.PressureMax,
                            distance: datum.Distance,
                            distanceMax: datum.DistanceMax,
                            size: datum.Size,
                            radiusMajor: radiusMajor,
                            radiusMinor: radiusMinor,
                            radiusMin: radiusMin,
                            radiusMax: radiusMax,
                            orientation: datum.Orientation,
                            tilt: datum.Tilt,
                            embedderId: datum.EmbedderId);
                    case PointerChange.Remove:
                        return new PointerRemovedEvent(
                            viewId: datum.ViewId,
                            timestampUtc: timeStamp,
                            kind: kind,
                            device: datum.Device,
                            position: position,
                            obscured: datum.Obscured,
                            pressureMin: datum.PressureMin,
                            pressureMax: datum.PressureMax,
                            distanceMax: datum.DistanceMax,
                            radiusMin: radiusMin,
                            radiusMax: radiusMax,
                            embedderId: datum.EmbedderId);
                    case PointerChange.PanZoomStart:
                        return new PointerPanZoomStartEvent(
                            viewId: datum.ViewId,
                            timestampUtc: timeStamp,
                            pointer: datum.PointerIdentifier,
                            device: datum.Device,
                            position: position,
                            embedderId: datum.EmbedderId,
                            synthesized: datum.Synthesized);
                    case PointerChange.PanZoomUpdate:
                        var pan = new Point(datum.PanX / devicePixelRatio, datum.PanY / devicePixelRatio);
                        var panDelta = new Point(
                            datum.PanDeltaX / devicePixelRatio,
                            datum.PanDeltaY / devicePixelRatio);
                        return new PointerPanZoomUpdateEvent(
                            viewId: datum.ViewId,
                            timestampUtc: timeStamp,
                            pointer: datum.PointerIdentifier,
                            device: datum.Device,
                            position: position,
                            pan: pan,
                            panDelta: panDelta,
                            scale: datum.Scale,
                            rotation: datum.Rotation,
                            embedderId: datum.EmbedderId,
                            synthesized: datum.Synthesized);
                    case PointerChange.PanZoomEnd:
                        return new PointerPanZoomEndEvent(
                            viewId: datum.ViewId,
                            timestampUtc: timeStamp,
                            pointer: datum.PointerIdentifier,
                            device: datum.Device,
                            position: position,
                            embedderId: datum.EmbedderId,
                            synthesized: datum.Synthesized);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(datum), datum.Change, null);
                }

            case PointerSignalKind.Scroll:
                if (!double.IsFinite(datum.ScrollDeltaX)
                    || !double.IsFinite(datum.ScrollDeltaY)
                    || devicePixelRatio <= 0)
                {
                    return null;
                }

                var scrollDelta = new Point(
                    datum.ScrollDeltaX / devicePixelRatio,
                    datum.ScrollDeltaY / devicePixelRatio);
                return new PointerScrollEvent(
                    viewId: datum.ViewId,
                    timestampUtc: timeStamp,
                    kind: kind,
                    device: datum.Device,
                    position: position,
                    scrollDelta: scrollDelta,
                    embedderId: datum.EmbedderId,
                    onRespond: datum.Respond);
            case PointerSignalKind.ScrollInertiaCancel:
                return new PointerScrollInertiaCancelEvent(
                    viewId: datum.ViewId,
                    timestampUtc: timeStamp,
                    kind: kind,
                    device: datum.Device,
                    position: position,
                    embedderId: datum.EmbedderId);
            case PointerSignalKind.Scale:
                return new PointerScaleEvent(
                    viewId: datum.ViewId,
                    timestampUtc: timeStamp,
                    kind: kind,
                    device: datum.Device,
                    position: position,
                    embedderId: datum.EmbedderId,
                    scale: datum.Scale);
            default:
                throw new InvalidOperationException("Unreachable");
        }
    }

    // Add `kPrimaryButton` to [buttons] when a pointer of certain devices is down.
    //
    // Dart notes this patch is supposed to be done by embedders; the framework carries it until
    // PointerEventConverter moves to them (flutter/flutter#30454).
    private static PointerButtons SynthesiseDownButtons(PointerButtons buttons, PointerDeviceKind kind)
    {
        switch (kind)
        {
            case PointerDeviceKind.Mouse:
            case PointerDeviceKind.Trackpad:
                return buttons;
            case PointerDeviceKind.Touch:
            case PointerDeviceKind.Stylus:
            case PointerDeviceKind.InvertedStylus:
                return buttons == PointerButtons.None ? PointerButtons.Primary : buttons;
            default:
                // We have no information about the device but we know we never want
                // buttons to be 0 when the pointer is down.
                return buttons == PointerButtons.None ? PointerButtons.Primary : buttons;
        }
    }

    private static double ToLogicalPixels(double physicalPixels, double devicePixelRatio)
    {
        return physicalPixels / devicePixelRatio;
    }
}
