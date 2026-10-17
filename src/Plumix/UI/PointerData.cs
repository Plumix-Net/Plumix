using System.Globalization;

// Dart parity source: flutter/bin/cache/pkg/sky_engine/lib/ui/pointer.dart
// `PointerDeviceKind` lives next to the framework's pointer events (`Plumix.UI.PointerEvents`).

namespace Plumix.UI;

/// <summary>How the pointer has changed since the last report. Dart's <c>PointerChange</c>.</summary>
public enum PointerChange
{
    /// <summary>The input from the pointer is no longer directed towards this receiver.</summary>
    Cancel,

    /// <summary>The device has started tracking the pointer.</summary>
    Add,

    /// <summary>The device is no longer tracking the pointer.</summary>
    Remove,

    /// <summary>The pointer moved while down and no buttons are pressed.</summary>
    Hover,

    /// <summary>The pointer made contact with the device.</summary>
    Down,

    /// <summary>The pointer moved in contact with the device.</summary>
    Move,

    /// <summary>The pointer stopped making contact with the device.</summary>
    Up,

    /// <summary>A pan/zoom gesture started on a trackpad.</summary>
    PanZoomStart,

    /// <summary>The pan/zoom gesture changed.</summary>
    PanZoomUpdate,

    /// <summary>The pan/zoom gesture ended.</summary>
    PanZoomEnd,
}

/// <summary>The kind of signal a pointer data entry carries. Dart's <c>PointerSignalKind</c>.</summary>
public enum PointerSignalKind
{
    /// <summary>The event is not associated with a pointer signal.</summary>
    None,

    /// <summary>A pointer-generated scroll (for example, a mouse wheel or trackpad scroll).</summary>
    Scroll,

    /// <summary>A pointer-generated scroll-inertia cancel.</summary>
    ScrollInertiaCancel,

    /// <summary>A pointer-generated scale event (for example, a trackpad pinch).</summary>
    Scale,

    /// <summary>An unknown pointer signal kind; the converter drops such entries.</summary>
    Unknown,
}

/// <summary>
/// Dart's <c>PointerDataRespondCallback</c>: lets the framework answer the embedder about the event
/// that created a <see cref="PointerData"/>.
/// </summary>
public delegate void PointerDataRespondCallback(bool allowPlatformDefault);

/// <summary>
/// Information about the state of a pointer in physical pixels. Dart's <c>PointerData</c>, what the
/// engine reports through <see cref="PlatformDispatcher.OnPointerDataPacket"/> before
/// <see cref="Gestures.PointerEventConverter"/> turns it into logical pixels.
/// </summary>
/// <remarks>
/// Dart's <c>timeStamp</c> is a <c>Duration</c>; <see cref="TimeStamp"/> keeps it and the converter
/// places it on the timeline of <see cref="PointerEvent.TimestampUtc"/>, whose zero is
/// <c>default(DateTime)</c> (see <c>docs/ai/DIVERGENCES.md</c>).
/// </remarks>
public sealed class PointerData
{
    private readonly PointerDataRespondCallback? _onRespond;

    public PointerData(
        int viewId = 0,
        int embedderId = 0,
        TimeSpan timeStamp = default,
        PointerChange change = PointerChange.Cancel,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        PointerSignalKind? signalKind = null,
        int device = 0,
        int pointerIdentifier = 0,
        double physicalX = 0.0,
        double physicalY = 0.0,
        double physicalDeltaX = 0.0,
        double physicalDeltaY = 0.0,
        PointerButtons buttons = PointerButtons.None,
        bool obscured = false,
        bool synthesized = false,
        double pressure = 0.0,
        double pressureMin = 0.0,
        double pressureMax = 0.0,
        double distance = 0.0,
        double distanceMax = 0.0,
        double size = 0.0,
        double radiusMajor = 0.0,
        double radiusMinor = 0.0,
        double radiusMin = 0.0,
        double radiusMax = 0.0,
        double orientation = 0.0,
        double tilt = 0.0,
        int platformData = 0,
        double scrollDeltaX = 0.0,
        double scrollDeltaY = 0.0,
        double panX = 0.0,
        double panY = 0.0,
        double panDeltaX = 0.0,
        double panDeltaY = 0.0,
        double scale = 0.0,
        double rotation = 0.0,
        PointerDataRespondCallback? onRespond = null)
    {
        ViewId = viewId;
        EmbedderId = embedderId;
        TimeStamp = timeStamp;
        Change = change;
        Kind = kind;
        SignalKind = signalKind;
        Device = device;
        PointerIdentifier = pointerIdentifier;
        PhysicalX = physicalX;
        PhysicalY = physicalY;
        PhysicalDeltaX = physicalDeltaX;
        PhysicalDeltaY = physicalDeltaY;
        Buttons = buttons;
        Obscured = obscured;
        Synthesized = synthesized;
        Pressure = pressure;
        PressureMin = pressureMin;
        PressureMax = pressureMax;
        Distance = distance;
        DistanceMax = distanceMax;
        Size = size;
        RadiusMajor = radiusMajor;
        RadiusMinor = radiusMinor;
        RadiusMin = radiusMin;
        RadiusMax = radiusMax;
        Orientation = orientation;
        Tilt = tilt;
        PlatformData = platformData;
        ScrollDeltaX = scrollDeltaX;
        ScrollDeltaY = scrollDeltaY;
        PanX = panX;
        PanY = panY;
        PanDeltaX = panDeltaX;
        PanDeltaY = panDeltaY;
        Scale = scale;
        Rotation = rotation;
        _onRespond = onRespond;
    }

    /// <summary>The ID of the view this entry came from.</summary>
    public int ViewId { get; }

    /// <summary>Unique identifier that ties this entry to the embedder event that created it.</summary>
    public int EmbedderId { get; }

    /// <summary>Time of event dispatch, relative to an arbitrary timeline.</summary>
    public TimeSpan TimeStamp { get; }

    /// <summary>How the pointer has changed since the last report.</summary>
    public PointerChange Change { get; }

    /// <summary>The kind of input device for which the event was generated.</summary>
    public PointerDeviceKind Kind { get; }

    /// <summary>The kind of signal, or <see langword="null"/> for a plain pointer change.</summary>
    public PointerSignalKind? SignalKind { get; }

    /// <summary>Unique identifier for the pointing device, reused across interactions.</summary>
    public int Device { get; }

    /// <summary>Unique identifier for the pointer, not reused.</summary>
    public int PointerIdentifier { get; }

    /// <summary>X coordinate of the pointer, in physical pixels in the global coordinate space.</summary>
    public double PhysicalX { get; }

    /// <summary>Y coordinate of the pointer, in physical pixels in the global coordinate space.</summary>
    public double PhysicalY { get; }

    /// <summary>The distance the pointer moved along X since the last report, in physical pixels.</summary>
    public double PhysicalDeltaX { get; }

    /// <summary>The distance the pointer moved along Y since the last report, in physical pixels.</summary>
    public double PhysicalDeltaY { get; }

    /// <summary>Bit field using the <see cref="PointerButtons"/> constants.</summary>
    public PointerButtons Buttons { get; }

    /// <summary>Set if an application from a different security domain obscures the window.</summary>
    public bool Obscured { get; }

    /// <summary>Set if the entry was synthesized by the framework or the embedder.</summary>
    public bool Synthesized { get; }

    /// <summary>The pressure of the touch.</summary>
    public double Pressure { get; }

    /// <summary>The minimum value that <see cref="Pressure"/> can return.</summary>
    public double PressureMin { get; }

    /// <summary>The maximum value that <see cref="Pressure"/> can return.</summary>
    public double PressureMax { get; }

    /// <summary>The distance of the detected object from the input surface.</summary>
    public double Distance { get; }

    /// <summary>The maximum value that <see cref="Distance"/> can return.</summary>
    public double DistanceMax { get; }

    /// <summary>The area of the screen being pressed, scaled to a value between 0 and 1.</summary>
    public double Size { get; }

    /// <summary>The radius of the contact ellipse along the major axis, in physical pixels.</summary>
    public double RadiusMajor { get; }

    /// <summary>The radius of the contact ellipse along the minor axis, in physical pixels.</summary>
    public double RadiusMinor { get; }

    /// <summary>The minimum value that could be reported for the radii, in physical pixels.</summary>
    public double RadiusMin { get; }

    /// <summary>The maximum value that could be reported for the radii, in physical pixels.</summary>
    public double RadiusMax { get; }

    /// <summary>The orientation angle of the detected object, in radians.</summary>
    public double Orientation { get; }

    /// <summary>The tilt angle of the detected object, in radians.</summary>
    public double Tilt { get; }

    /// <summary>Opaque platform-specific data associated with the event.</summary>
    public int PlatformData { get; }

    /// <summary>For a scroll signal, the amount scrolled along X, in physical pixels.</summary>
    public double ScrollDeltaX { get; }

    /// <summary>For a scroll signal, the amount scrolled along Y, in physical pixels.</summary>
    public double ScrollDeltaY { get; }

    /// <summary>For a pan/zoom gesture, the total pan along X so far, in physical pixels.</summary>
    public double PanX { get; }

    /// <summary>For a pan/zoom gesture, the total pan along Y so far, in physical pixels.</summary>
    public double PanY { get; }

    /// <summary>For a pan/zoom gesture, the pan along X since the last report, in physical pixels.</summary>
    public double PanDeltaX { get; }

    /// <summary>For a pan/zoom gesture, the pan along Y since the last report, in physical pixels.</summary>
    public double PanDeltaY { get; }

    /// <summary>For a pan/zoom or scale signal, the scale so far.</summary>
    public double Scale { get; }

    /// <summary>For a pan/zoom gesture, the rotation so far, in radians.</summary>
    public double Rotation { get; }

    /// <summary>
    /// Answers the embedder about the event that created this entry. Dart's <c>respond</c>; does
    /// nothing when the embedder installed no callback.
    /// </summary>
    public void Respond(bool allowPlatformDefault)
    {
        _onRespond?.Invoke(allowPlatformDefault);
    }

    public override string ToString()
    {
        return FormattableString.Invariant($"PointerData(viewId: {ViewId}, x: {PhysicalX}, y: {PhysicalY})");
    }

    /// <summary>Every field of the entry, as Dart's <c>toStringFull</c> prints them.</summary>
    public string ToStringFull()
    {
        string[] fields =
        [
            Field("embedderId", EmbedderId),
            Field("timeStamp", TimeStamp),
            Field("change", Change),
            Field("kind", Kind),
            Field("signalKind", SignalKind),
            Field("device", Device),
            Field("pointerIdentifier", PointerIdentifier),
            Field("physicalX", PhysicalX),
            Field("physicalY", PhysicalY),
            Field("physicalDeltaX", PhysicalDeltaX),
            Field("physicalDeltaY", PhysicalDeltaY),
            Field("buttons", (long)Buttons),
            Field("synthesized", Synthesized),
            Field("pressure", Pressure),
            Field("pressureMin", PressureMin),
            Field("pressureMax", PressureMax),
            Field("distance", Distance),
            Field("distanceMax", DistanceMax),
            Field("size", Size),
            Field("radiusMajor", RadiusMajor),
            Field("radiusMinor", RadiusMinor),
            Field("radiusMin", RadiusMin),
            Field("radiusMax", RadiusMax),
            Field("orientation", Orientation),
            Field("tilt", Tilt),
            Field("platformData", PlatformData),
            Field("scrollDeltaX", ScrollDeltaX),
            Field("scrollDeltaY", ScrollDeltaY),
            Field("panX", PanX),
            Field("panY", PanY),
            Field("panDeltaX", PanDeltaX),
            Field("panDeltaY", PanDeltaY),
            Field("scale", Scale),
            Field("rotation", Rotation),
            Field("viewId", ViewId),
        ];
        return $"{nameof(PointerData)}({string.Join(", ", fields)})";
    }

    private static string Field(string name, object? value)
    {
        string text = value switch
        {
            null => "null",
            bool flag => flag ? "true" : "false",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        };
        return name + ": " + text;
    }
}

/// <summary>A sequence of reports about the state of pointers. Dart's <c>PointerDataPacket</c>.</summary>
public sealed class PointerDataPacket
{
    public PointerDataPacket(IReadOnlyList<PointerData>? data = null)
    {
        Data = data ?? [];
    }

    /// <summary>Data about the individual pointers in this packet.</summary>
    public IReadOnlyList<PointerData> Data { get; }
}
