using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/events.dart
// The top-level functions of `events.dart` (`computeHitSlop`, `smallestButton`, ...) live on
// `Plumix.Gestures.PointerEventUtils`, because C# needs a containing type for them.

namespace Plumix.UI;

/// <summary>
/// The kind of pointer device. Dart's <c>dart:ui</c> <c>PointerDeviceKind</c>, which
/// <c>events.dart</c> re-exports.
/// </summary>
public enum PointerDeviceKind
{
    Touch,
    Mouse,
    Stylus,
    InvertedStylus,
    Trackpad,
    Unknown
}

/// <summary>
/// The bit field of <see cref="PointerEvent.Buttons"/>. Dart types <c>buttons</c> as a plain
/// 64-bit <c>int</c> and names its bits with the <c>k*Button</c> constants; each constant is a
/// member here (the aliases share a bit exactly as the Dart constants do), and any other bit —
/// <see cref="Gestures.PointerEventUtils.NthMouseButton"/> reaches up to the 62nd — is a value of
/// this type through a cast.
/// </summary>
[Flags]
public enum PointerButtons : long
{
    None = 0,

    /// <summary>Dart's <c>kPrimaryButton</c>: the primary button of a mouse, stylus or touch.</summary>
    Primary = 0x01,

    /// <summary>Dart's <c>kSecondaryButton</c>.</summary>
    Secondary = 0x02,

    /// <summary>Dart's <c>kPrimaryMouseButton</c>, usually the left mouse button.</summary>
    PrimaryMouse = Primary,

    /// <summary>Dart's <c>kSecondaryMouseButton</c>, usually the right mouse button.</summary>
    SecondaryMouse = Secondary,

    /// <summary>Dart's <c>kStylusContact</c>: the stylus touches the screen.</summary>
    StylusContact = Primary,

    /// <summary>Dart's <c>kPrimaryStylusButton</c>, the button closest to the stylus tip.</summary>
    PrimaryStylus = Secondary,

    /// <summary>Dart's <c>kTertiaryButton</c>.</summary>
    Tertiary = 0x04,

    /// <summary>Dart's <c>kMiddleMouseButton</c>, usually the scroll-wheel click.</summary>
    MiddleMouse = Tertiary,

    /// <summary>Dart's <c>kSecondaryStylusButton</c>, the stylus button second closest to the tip.</summary>
    SecondaryStylus = Tertiary,

    /// <summary>Dart's <c>kBackMouseButton</c>.</summary>
    BackMouse = 0x08,

    /// <summary>Dart's <c>kForwardMouseButton</c>.</summary>
    ForwardMouse = 0x10,

    /// <summary>Dart's <c>kTouchContact</c>: the pointer contacts the touch screen.</summary>
    TouchContact = Primary,
}

/// <summary>
/// Dart's <c>RespondPointerEventCallback</c>: how a <see cref="PointerScrollEvent"/> reports back to
/// the embedder whether the platform should still run its default action.
/// </summary>
public delegate void RespondPointerEventCallback(bool allowPlatformDefault);

/// <summary>
/// Base class for touch, stylus, or mouse events. Ports Dart's <c>PointerEvent</c> together with the
/// <c>_PointerEventDescription</c> mixin every concrete event mixes in.
/// </summary>
/// <remarks>
/// <para>
/// Dart's <c>timeStamp</c> is a <c>Duration</c> on the engine's monotonic timeline; Plumix hosts
/// stamp events with the UTC wall clock, so the field is <see cref="TimestampUtc"/> and
/// <c>Duration.zero</c> is <c>default(DateTime)</c> (see <c>docs/ai/DIVERGENCES.md</c>).
/// </para>
/// <para>
/// Dart models a transformed event as a private <c>_Transformed*Event</c> wrapper that delegates to
/// <see cref="Original"/>. Plumix copies the original instead and fills the transformed local
/// fields eagerly: the observable fields, the identity of <see cref="Original"/> and the rule that
/// transforms always apply to the original match, and <see cref="ToStringShort"/> reports the
/// wrapper's <c>_Transformed</c> type name.
/// </para>
/// </remarks>
public abstract class PointerEvent : Diagnosticable
{
    protected PointerEvent(
        int viewId = 0,
        int embedderId = 0,
        DateTime timestampUtc = default,
        int pointer = 0,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        int? device = null,
        Point position = default,
        Point delta = default,
        PointerButtons buttons = PointerButtons.None,
        bool down = false,
        bool obscured = false,
        double pressure = 1.0,
        double pressureMin = 1.0,
        double pressureMax = 1.0,
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
        bool synthesized = false,
        Matrix4? transform = null,
        PointerEvent? original = null)
    {
        ViewId = viewId;
        EmbedderId = embedderId;
        TimestampUtc = timestampUtc;
        Pointer = pointer;
        Kind = kind;
        Device = device ?? pointer;
        Position = position;
        LocalPosition = position;
        Delta = delta;
        LocalDelta = delta;
        Buttons = buttons;
        Down = down;
        Obscured = obscured;
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
        Synthesized = synthesized;
        Transform = transform;
        Original = original;
    }

    /// <summary>The ID of the view this event came from. Dart's <c>viewId</c>.</summary>
    public int ViewId { get; }

    /// <summary>
    /// Unique identifier that ties the event to the embedder event that created it. Dart's
    /// <c>embedderId</c>; 0 when the platform does not provide one.
    /// </summary>
    public int EmbedderId { get; }

    /// <summary>
    /// Time of event dispatch. Dart's <c>timeStamp</c>, stamped on the UTC wall clock instead of an
    /// engine-relative <c>Duration</c>.
    /// </summary>
    public DateTime TimestampUtc { get; }

    /// <summary>
    /// Unique identifier for the pointer, not reused; changes for each new pointer down event.
    /// Dart's <c>pointer</c>.
    /// </summary>
    public int Pointer { get; }

    /// <summary>The kind of input device for which the event was generated. Dart's <c>kind</c>.</summary>
    public PointerDeviceKind Kind { get; }

    /// <summary>
    /// Unique identifier for the pointing device, reused across interactions. Dart's <c>device</c>;
    /// <see cref="Plumix.Rendering.MouseTracker"/> keys its per-device state on it.
    /// </summary>
    /// <remarks>
    /// Dart defaults it to 0 because its engine allocates pointer ids per gesture and device ids per
    /// device. Plumix's hosts get one identifier per device from the platform and pass it as
    /// <see cref="Pointer"/>, so an event constructed without a device reports its pointer.
    /// </remarks>
    public int Device { get; }

    /// <summary>Coordinate of the position of the pointer, in logical pixels in the global space.</summary>
    public Point Position { get; }

    /// <summary>
    /// <see cref="Position"/> transformed into the event receiver's local coordinate system
    /// according to <see cref="Transform"/>; equal to <see cref="Position"/> when untransformed.
    /// </summary>
    public Point LocalPosition { get; private set; }

    /// <summary>
    /// Distance in logical pixels that the pointer moved since the last move or hover event. Always
    /// zero for down, up and cancel events.
    /// </summary>
    public Point Delta { get; private set; }

    /// <summary>
    /// <see cref="Delta"/> transformed into the event receiver's local coordinate system according
    /// to <see cref="Transform"/>; equal to <see cref="Delta"/> when untransformed.
    /// </summary>
    public Point LocalDelta { get; private set; }

    /// <summary>Bit field using the <see cref="PointerButtons"/> constants. Dart's <c>buttons</c>.</summary>
    public PointerButtons Buttons { get; }

    /// <summary>
    /// Set if the pointer is currently down: a touch or stylus in contact, or a mouse button
    /// pressed. Dart's <c>down</c>.
    /// </summary>
    public bool Down { get; }

    /// <summary>
    /// Set if an application from a different security domain is in any way obscuring this
    /// application's window. Dart notes this is not currently implemented.
    /// </summary>
    public bool Obscured { get; }

    /// <summary>
    /// The pressure of the touch, from 0.0 (no pressure) to 1.0 (normal pressure) and beyond.
    /// Devices without a force sensor report 1.0. Dart's <c>pressure</c>.
    /// </summary>
    public double Pressure { get; }

    /// <summary>
    /// The minimum value that <see cref="Pressure"/> can return for this pointer; 1.0 for devices
    /// that do not detect pressure. Dart's <c>pressureMin</c>.
    /// </summary>
    public double PressureMin { get; }

    /// <summary>
    /// The maximum value that <see cref="Pressure"/> can return for this pointer; 1.0 for devices
    /// that do not detect pressure, which is how
    /// <see cref="Plumix.Gestures.ForcePressGestureRecognizer"/> detects that it must not compete.
    /// </summary>
    public double PressureMax { get; }

    /// <summary>
    /// The distance of the detected object from the input surface, 0.0 by definition while down.
    /// Dart's <c>distance</c>.
    /// </summary>
    public double Distance { get; }

    /// <summary>The minimum value that <see cref="Distance"/> can return; always 0.0.</summary>
    public double DistanceMin => 0.0;

    /// <summary>
    /// The maximum value that <see cref="Distance"/> can return; 0.0 if the device cannot detect
    /// hover. Dart's <c>distanceMax</c>.
    /// </summary>
    public double DistanceMax { get; }

    /// <summary>
    /// The area of the screen being pressed, scaled to a value between 0 and 1. Only reported on
    /// Android. Dart's <c>size</c>.
    /// </summary>
    public double Size { get; }

    /// <summary>The radius of the contact ellipse along the major axis, in logical pixels.</summary>
    public double RadiusMajor { get; }

    /// <summary>The radius of the contact ellipse along the minor axis, in logical pixels.</summary>
    public double RadiusMinor { get; }

    /// <summary>
    /// The minimum value that could be reported for <see cref="RadiusMajor"/> and
    /// <see cref="RadiusMinor"/>.
    /// </summary>
    public double RadiusMin { get; }

    /// <summary>
    /// The maximum value that could be reported for <see cref="RadiusMajor"/> and
    /// <see cref="RadiusMinor"/>.
    /// </summary>
    public double RadiusMax { get; }

    /// <summary>
    /// The orientation angle of the detected object, in radians. Dart's <c>orientation</c>.
    /// </summary>
    public double Orientation { get; }

    /// <summary>
    /// The tilt angle of the detected object, in radians; 0.0 means perpendicular to the input
    /// surface. Dart's <c>tilt</c>.
    /// </summary>
    public double Tilt { get; }

    /// <summary>Opaque platform-specific data associated with the event. Dart's <c>platformData</c>.</summary>
    public int PlatformData { get; }

    /// <summary>
    /// Set if the event was synthesized by the framework rather than reported by the platform.
    /// Dart's <c>synthesized</c>; synthesized moves are excluded from velocity tracking.
    /// </summary>
    public bool Synthesized { get; }

    /// <summary>
    /// The transformation used to transform this event from the global coordinate space into the
    /// coordinate space of the event receiver; null means identity. Dart's <c>transform</c>.
    /// </summary>
    public Matrix4? Transform { get; private set; }

    /// <summary>
    /// The original un-transformed event this event was derived from, or null. Dart's
    /// <c>original</c>; the <see cref="Plumix.Gestures.PointerSignalResolver"/> uses it to identify
    /// transformed copies of one signal event.
    /// </summary>
    public PointerEvent? Original { get; private set; }

    /// <summary>
    /// Set on the move and hover events <see cref="Plumix.Gestures.PointerEventResampler"/> emits,
    /// whose deltas it already computed; the binding then keeps their deltas as they are.
    /// </summary>
    internal bool IsResampled { get; private set; }

    /// <summary>
    /// Transforms the event from the global coordinate space into the coordinate space of an event
    /// receiver. Ports Dart's <c>PointerEvent.transformed</c>: a null transform returns the
    /// un-transformed event, and a transform always applies to <see cref="Original"/>, so it
    /// replaces rather than composes with a transform this event already carries.
    /// </summary>
    public PointerEvent Transformed(Matrix4? transform)
    {
        if (Transform is not null)
        {
            // `_Transformed*Event.transformed` => `original.transformed(transform)`.
            return Original!.Transformed(transform);
        }

        if (transform is null)
        {
            return this;
        }

        PointerEvent source = Original ?? this;
        var result = (PointerEvent)source.MemberwiseClone();
        result.Original = source;
        result.Transform = transform;
        result.LocalPosition = TransformPosition(transform, source.Position);
        result.LocalDelta = TransformDeltaViaPositions(
            untransformedEndPosition: source.Position,
            untransformedDelta: source.Delta,
            transform: transform,
            transformedEndPosition: result.LocalPosition);
        result.ApplyLocalTransform(transform);
        return result;
    }

    /// <summary>
    /// Creates a copy of the event with the specified properties replaced. Ports Dart's
    /// <c>PointerEvent.copyWith</c>: each event type forwards only the fields its own constructor
    /// accepts, and calling this on a transformed event returns a transformed event.
    /// </summary>
    public abstract PointerEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null);

    /// <summary>
    /// Maps the local copies of any coordinates a subclass adds on top of position and delta. Called
    /// on the freshly transformed copy; Dart's <c>_Transformed*Event</c> classes compute these as
    /// their own <c>late final</c> fields.
    /// </summary>
    private protected virtual void ApplyLocalTransform(Matrix4 transform)
    {
    }

    /// <summary>
    /// Returns the transformation of <paramref name="position"/> into the coordinate system
    /// described by <paramref name="transform"/>, with the perspective divide. Dart's static
    /// <c>PointerEvent.transformPosition</c>.
    /// </summary>
    public static Point TransformPosition(Matrix4? transform, Point position)
    {
        if (transform is null)
        {
            return position;
        }

        return MatrixUtils.TransformPoint(transform, position);
    }

    /// <summary>
    /// Transforms <paramref name="untransformedDelta"/> into the coordinate system described by
    /// <paramref name="transform"/> by transforming both of its end points and subtracting, which is
    /// more precise than applying the matrix to the delta. Dart's static
    /// <c>PointerEvent.transformDeltaViaPositions</c>.
    /// </summary>
    public static Point TransformDeltaViaPositions(
        Point untransformedEndPosition,
        Point untransformedDelta,
        Matrix4? transform,
        Point? transformedEndPosition = null)
    {
        if (transform is null)
        {
            return untransformedDelta;
        }

        Point end = transformedEndPosition ?? TransformPosition(transform, untransformedEndPosition);
        Point start = TransformPosition(transform, untransformedEndPosition - untransformedDelta);
        return end - start;
    }

    /// <summary>
    /// Removes the "perspective" component from <paramref name="transform"/>: a copy with the z row
    /// and column reset to <c>(0, 0, 1, 0)</c>, so a paint transform can be inverted for hit testing
    /// without the perspective divide flattening the plane. Dart's static
    /// <c>PointerEvent.removePerspectiveTransform</c>; the input is not mutated.
    /// </summary>
    public static Matrix4 RemovePerspectiveTransform(Matrix4 transform)
    {
        var vector = new Vector4(0.0, 0.0, 1.0, 0.0);
        Matrix4 result = transform.Clone();
        result.SetColumn(2, vector);
        result.SetRow(2, vector);
        return result;
    }

    /// <inheritdoc />
    /// <remarks>
    /// A transformed event reports Dart's wrapper type (<c>_TransformedPointerDownEvent</c>, ...).
    /// </remarks>
    public override string ToStringShort()
    {
        string description = base.ToStringShort();
        return Transform is not null && Constants.KDebugMode ? "_Transformed" + description : description;
    }

    /// <inheritdoc />
    /// <remarks>Dart's <c>_PointerEventDescription.debugFillProperties</c>.</remarks>
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Point>("position", Position));
        properties.Add(new DiagnosticsProperty<Point>(
            "localPosition",
            LocalPosition,
            defaultValue: Position,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<Point>(
            "delta",
            Delta,
            defaultValue: default(Point),
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<Point>(
            "localDelta",
            LocalDelta,
            defaultValue: Delta,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<DateTime>(
            "timeStamp",
            TimestampUtc,
            defaultValue: default(DateTime),
            level: DiagnosticLevel.Debug));
        properties.Add(new IntProperty("pointer", Pointer, level: DiagnosticLevel.Debug));
        properties.Add(new EnumProperty<PointerDeviceKind>("kind", Kind, level: DiagnosticLevel.Debug));
        properties.Add(new IntProperty("device", Device, defaultValue: 0, level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<long>(
            "buttons",
            (long)Buttons,
            defaultValue: 0L,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<bool>("down", Down, level: DiagnosticLevel.Debug));
        AddDouble(properties, "pressure", Pressure, 1.0);
        AddDouble(properties, "pressureMin", PressureMin, 1.0);
        AddDouble(properties, "pressureMax", PressureMax, 1.0);
        AddDouble(properties, "distance", Distance, 0.0);
        AddDouble(properties, "distanceMin", DistanceMin, 0.0);
        AddDouble(properties, "distanceMax", DistanceMax, 0.0);
        AddDouble(properties, "size", Size, 0.0);
        AddDouble(properties, "radiusMajor", RadiusMajor, 0.0);
        AddDouble(properties, "radiusMinor", RadiusMinor, 0.0);
        AddDouble(properties, "radiusMin", RadiusMin, 0.0);
        AddDouble(properties, "radiusMax", RadiusMax, 0.0);
        AddDouble(properties, "orientation", Orientation, 0.0);
        AddDouble(properties, "tilt", Tilt, 0.0);
        properties.Add(new IntProperty(
            "platformData",
            PlatformData,
            defaultValue: 0,
            level: DiagnosticLevel.Debug));
        properties.Add(new FlagProperty("obscured", Obscured, ifTrue: "obscured", level: DiagnosticLevel.Debug));
        properties.Add(new FlagProperty(
            "synthesized",
            Synthesized,
            ifTrue: "synthesized",
            level: DiagnosticLevel.Debug));
        properties.Add(new IntProperty("embedderId", EmbedderId, defaultValue: 0, level: DiagnosticLevel.Debug));
        properties.Add(new IntProperty("viewId", ViewId, defaultValue: 0, level: DiagnosticLevel.Debug));
    }

    /// <summary>
    /// Returns a complete textual description of this event, every property down to
    /// <see cref="DiagnosticLevel.Fine"/>. Dart's <c>toStringFull</c>.
    /// </summary>
    public string ToStringFull() => ToString(DiagnosticLevel.Fine);

    /// <summary>
    /// Throws when an event type that the platform never reports for a trackpad is built with
    /// <see cref="PointerDeviceKind.Trackpad"/>. Ports the
    /// <c>assert(!identical(kind, PointerDeviceKind.trackpad))</c> of Dart's enter, exit, down, move,
    /// up and cancel constructors: a trackpad reports its gestures as the pan/zoom events instead.
    /// </summary>
    private protected static PointerDeviceKind AssertNotTrackpad(PointerDeviceKind kind)
    {
        if (kind == PointerDeviceKind.Trackpad)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                "A trackpad reports its gestures as PointerPanZoom events, not as this event type.");
        }

        return kind;
    }

    /// <summary>
    /// Ports the <c>assert(kind == null || identical(kind, PointerDeviceKind.trackpad))</c> of the
    /// pan/zoom events' <c>copyWith</c>.
    /// </summary>
    private protected static void AssertTrackpadOrNull(PointerDeviceKind? kind)
    {
        if (kind is not null && kind != PointerDeviceKind.Trackpad)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                "A pan/zoom event can only be reported by a trackpad.");
        }
    }

    /// <summary>
    /// Returns a copy of this un-transformed event carrying <paramref name="delta"/>. Plumix hosts
    /// report logical events without deltas, so <see cref="Plumix.Gestures.GestureBinding"/> computes
    /// them the way Flutter's engine does before the event reaches <c>PointerEventConverter</c>.
    /// </summary>
    internal PointerEvent WithDelta(Point delta)
    {
        if (Transform is not null)
        {
            throw new InvalidOperationException("Only an un-transformed event can take a delta.");
        }

        var result = (PointerEvent)MemberwiseClone();
        result.Delta = delta;
        result.LocalDelta = delta;
        return result;
    }

    /// <summary>
    /// Marks an event the resampler has just created, so the binding keeps the delta the resampler
    /// computed for it.
    /// </summary>
    internal PointerEvent MarkResampled()
    {
        IsResampled = true;
        return this;
    }

    private static void AddDouble(
        DiagnosticPropertiesBuilder properties,
        string name,
        double value,
        double defaultValue)
    {
        properties.Add(new DoubleProperty(name, value, defaultValue: defaultValue, level: DiagnosticLevel.Debug));
    }
}

/// <summary>
/// The device has started tracking the pointer. Dart's <c>PointerAddedEvent</c>: for a mouse this is
/// the moment the pointer becomes able to produce hover events, and it is what makes
/// <see cref="Plumix.Rendering.MouseTracker.MouseIsConnected"/> true.
/// </summary>
/// <remarks>
/// The leading parameters keep the positional order earlier Plumix releases used; every other one
/// is named, as in Dart, where they are all named.
/// </remarks>
public sealed class PointerAddedEvent : PointerEvent
{
    public PointerAddedEvent(
        int pointer = 0,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        Point position = default,
        DateTime timestampUtc = default,
        int viewId = 0,
        int? device = null,
        bool obscured = false,
        double pressureMin = 1.0,
        double pressureMax = 1.0,
        double distance = 0.0,
        double distanceMax = 0.0,
        double radiusMin = 0.0,
        double radiusMax = 0.0,
        double orientation = 0.0,
        double tilt = 0.0,
        int embedderId = 0)
        : base(
            viewId: viewId,
            timestampUtc: timestampUtc,
            pointer: pointer,
            kind: kind,
            device: device,
            position: position,
            obscured: obscured,
            pressure: 0.0,
            pressureMin: pressureMin,
            pressureMax: pressureMax,
            distance: distance,
            distanceMax: distanceMax,
            radiusMin: radiusMin,
            radiusMax: radiusMax,
            orientation: orientation,
            tilt: tilt,
            embedderId: embedderId)
    {
    }

    /// <inheritdoc />
    /// <remarks>Dart's <c>_CopyPointerAddedEvent</c>: <c>pointer</c> is not forwarded and resets to 0.</remarks>
    public override PointerAddedEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null)
    {
        return (PointerAddedEvent)new PointerAddedEvent(
            viewId: viewId ?? ViewId,
            timestampUtc: timestampUtc ?? TimestampUtc,
            kind: kind ?? Kind,
            device: device ?? Device,
            position: position ?? Position,
            obscured: obscured ?? Obscured,
            pressureMin: pressureMin ?? PressureMin,
            pressureMax: pressureMax ?? PressureMax,
            distance: distance ?? Distance,
            distanceMax: distanceMax ?? DistanceMax,
            radiusMin: radiusMin ?? RadiusMin,
            radiusMax: radiusMax ?? RadiusMax,
            orientation: orientation ?? Orientation,
            tilt: tilt ?? Tilt,
            embedderId: embedderId ?? EmbedderId).Transformed(Transform);
    }
}

/// <summary>
/// The device is no longer tracking the pointer. Dart's <c>PointerRemovedEvent</c>: the mouse
/// tracker treats it as an exit from every annotation and drops the device's cursor session.
/// </summary>
public sealed class PointerRemovedEvent : PointerEvent
{
    public PointerRemovedEvent(
        int pointer = 0,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        Point position = default,
        DateTime timestampUtc = default,
        int viewId = 0,
        int? device = null,
        bool obscured = false,
        double pressureMin = 1.0,
        double pressureMax = 1.0,
        double distanceMax = 0.0,
        double radiusMin = 0.0,
        double radiusMax = 0.0,
        PointerRemovedEvent? original = null,
        int embedderId = 0)
        : base(
            viewId: viewId,
            timestampUtc: timestampUtc,
            pointer: pointer,
            kind: kind,
            device: device,
            position: position,
            obscured: obscured,
            pressure: 0.0,
            pressureMin: pressureMin,
            pressureMax: pressureMax,
            distanceMax: distanceMax,
            radiusMin: radiusMin,
            radiusMax: radiusMax,
            original: original,
            embedderId: embedderId)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dart's <c>_CopyPointerRemovedEvent</c>: <c>pointer</c> and <c>original</c> are not forwarded.
    /// </remarks>
    public override PointerRemovedEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null)
    {
        return (PointerRemovedEvent)new PointerRemovedEvent(
            viewId: viewId ?? ViewId,
            timestampUtc: timestampUtc ?? TimestampUtc,
            kind: kind ?? Kind,
            device: device ?? Device,
            position: position ?? Position,
            obscured: obscured ?? Obscured,
            pressureMin: pressureMin ?? PressureMin,
            pressureMax: pressureMax ?? PressureMax,
            distanceMax: distanceMax ?? DistanceMax,
            radiusMin: radiusMin ?? RadiusMin,
            radiusMax: radiusMax ?? RadiusMax,
            embedderId: embedderId ?? EmbedderId).Transformed(Transform);
    }
}

/// <summary>
/// The pointer has moved with respect to the device while the pointer is not in contact with the
/// device. Dart's <c>PointerHoverEvent</c>; unlike the contact events it may come from a trackpad.
/// </summary>
public sealed class PointerHoverEvent : PointerEvent
{
    public PointerHoverEvent(
        int pointer = 0,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        Point position = default,
        PointerButtons buttons = PointerButtons.None,
        DateTime timestampUtc = default,
        int viewId = 0,
        int? device = null,
        Point delta = default,
        bool obscured = false,
        double pressureMin = 1.0,
        double pressureMax = 1.0,
        double distance = 0.0,
        double distanceMax = 0.0,
        double size = 0.0,
        double radiusMajor = 0.0,
        double radiusMinor = 0.0,
        double radiusMin = 0.0,
        double radiusMax = 0.0,
        double orientation = 0.0,
        double tilt = 0.0,
        bool synthesized = false,
        int embedderId = 0)
        : base(
            viewId: viewId,
            timestampUtc: timestampUtc,
            kind: kind,
            pointer: pointer,
            device: device,
            position: position,
            delta: delta,
            buttons: buttons,
            down: false,
            obscured: obscured,
            pressure: 0.0,
            pressureMin: pressureMin,
            pressureMax: pressureMax,
            distance: distance,
            distanceMax: distanceMax,
            size: size,
            radiusMajor: radiusMajor,
            radiusMinor: radiusMinor,
            radiusMin: radiusMin,
            radiusMax: radiusMax,
            orientation: orientation,
            tilt: tilt,
            synthesized: synthesized,
            embedderId: embedderId)
    {
    }

    /// <inheritdoc />
    /// <remarks>Dart's <c>_CopyPointerHoverEvent</c>: <c>pointer</c> is not forwarded and resets to 0.</remarks>
    public override PointerHoverEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null)
    {
        return (PointerHoverEvent)new PointerHoverEvent(
            viewId: viewId ?? ViewId,
            timestampUtc: timestampUtc ?? TimestampUtc,
            kind: kind ?? Kind,
            device: device ?? Device,
            position: position ?? Position,
            delta: delta ?? Delta,
            buttons: buttons ?? Buttons,
            obscured: obscured ?? Obscured,
            pressureMin: pressureMin ?? PressureMin,
            pressureMax: pressureMax ?? PressureMax,
            distance: distance ?? Distance,
            distanceMax: distanceMax ?? DistanceMax,
            size: size ?? Size,
            radiusMajor: radiusMajor ?? RadiusMajor,
            radiusMinor: radiusMinor ?? RadiusMinor,
            radiusMin: radiusMin ?? RadiusMin,
            radiusMax: radiusMax ?? RadiusMax,
            orientation: orientation ?? Orientation,
            tilt: tilt ?? Tilt,
            synthesized: synthesized ?? Synthesized,
            embedderId: embedderId ?? EmbedderId).Transformed(Transform);
    }
}

/// <summary>
/// The pointer has moved with respect to the device while the pointer is or is not in contact with
/// the device, and it has entered a target object. Dart's <c>PointerEnterEvent</c>; the
/// <see cref="Plumix.Rendering.MouseTracker"/> synthesizes it, it is never dispatched by the binding.
/// </summary>
public sealed class PointerEnterEvent : PointerEvent
{
    public PointerEnterEvent(
        int pointer = 0,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        Point position = default,
        PointerButtons buttons = PointerButtons.None,
        DateTime timestampUtc = default,
        bool down = false,
        int viewId = 0,
        int? device = null,
        Point delta = default,
        bool obscured = false,
        double pressureMin = 1.0,
        double pressureMax = 1.0,
        double distance = 0.0,
        double distanceMax = 0.0,
        double size = 0.0,
        double radiusMajor = 0.0,
        double radiusMinor = 0.0,
        double radiusMin = 0.0,
        double radiusMax = 0.0,
        double orientation = 0.0,
        double tilt = 0.0,
        bool synthesized = false,
        int embedderId = 0)
        : base(
            viewId: viewId,
            timestampUtc: timestampUtc,
            pointer: pointer,
            kind: AssertNotTrackpad(kind),
            device: device,
            position: position,
            delta: delta,
            buttons: buttons,
            down: down,
            obscured: obscured,
            pressure: 0.0,
            pressureMin: pressureMin,
            pressureMax: pressureMax,
            distance: distance,
            distanceMax: distanceMax,
            size: size,
            radiusMajor: radiusMajor,
            radiusMinor: radiusMinor,
            radiusMin: radiusMin,
            radiusMax: radiusMax,
            orientation: orientation,
            tilt: tilt,
            synthesized: synthesized,
            embedderId: embedderId)
    {
    }

    /// <summary>
    /// Creates an enter event from another event. Dart's <c>PointerEnterEvent.fromMouseEvent</c>:
    /// every field is copied except the embedder id, the platform data and the pressure (which the
    /// constructor forces to zero), and the result is transformed by the source event's transform.
    /// </summary>
    public static PointerEnterEvent FromMouseEvent(PointerEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (PointerEnterEvent)new PointerEnterEvent(
            viewId: @event.ViewId,
            timestampUtc: @event.TimestampUtc,
            pointer: @event.Pointer,
            kind: @event.Kind,
            device: @event.Device,
            position: @event.Position,
            delta: @event.Delta,
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
            down: @event.Down,
            synthesized: @event.Synthesized).Transformed(@event.Transform);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dart's <c>_CopyPointerEnterEvent</c>: <c>pointer</c> and <c>down</c> are not forwarded, so
    /// they reset to 0 and false.
    /// </remarks>
    public override PointerEnterEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null)
    {
        return (PointerEnterEvent)new PointerEnterEvent(
            viewId: viewId ?? ViewId,
            timestampUtc: timestampUtc ?? TimestampUtc,
            kind: kind ?? Kind,
            device: device ?? Device,
            position: position ?? Position,
            delta: delta ?? Delta,
            buttons: buttons ?? Buttons,
            obscured: obscured ?? Obscured,
            pressureMin: pressureMin ?? PressureMin,
            pressureMax: pressureMax ?? PressureMax,
            distance: distance ?? Distance,
            distanceMax: distanceMax ?? DistanceMax,
            size: size ?? Size,
            radiusMajor: radiusMajor ?? RadiusMajor,
            radiusMinor: radiusMinor ?? RadiusMinor,
            radiusMin: radiusMin ?? RadiusMin,
            radiusMax: radiusMax ?? RadiusMax,
            orientation: orientation ?? Orientation,
            tilt: tilt ?? Tilt,
            synthesized: synthesized ?? Synthesized,
            embedderId: embedderId ?? EmbedderId).Transformed(Transform);
    }
}

/// <summary>
/// The pointer has moved with respect to the device while the pointer is or is not in contact with
/// the device, and exited a target object. Dart's <c>PointerExitEvent</c>.
/// </summary>
public sealed class PointerExitEvent : PointerEvent
{
    public PointerExitEvent(
        int pointer = 0,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        Point position = default,
        PointerButtons buttons = PointerButtons.None,
        DateTime timestampUtc = default,
        bool down = false,
        int viewId = 0,
        int? device = null,
        Point delta = default,
        bool obscured = false,
        double pressureMin = 1.0,
        double pressureMax = 1.0,
        double distance = 0.0,
        double distanceMax = 0.0,
        double size = 0.0,
        double radiusMajor = 0.0,
        double radiusMinor = 0.0,
        double radiusMin = 0.0,
        double radiusMax = 0.0,
        double orientation = 0.0,
        double tilt = 0.0,
        bool synthesized = false,
        int embedderId = 0)
        : base(
            viewId: viewId,
            timestampUtc: timestampUtc,
            kind: AssertNotTrackpad(kind),
            pointer: pointer,
            device: device,
            position: position,
            delta: delta,
            buttons: buttons,
            down: down,
            obscured: obscured,
            pressure: 0.0,
            pressureMin: pressureMin,
            pressureMax: pressureMax,
            distance: distance,
            distanceMax: distanceMax,
            size: size,
            radiusMajor: radiusMajor,
            radiusMinor: radiusMinor,
            radiusMin: radiusMin,
            radiusMax: radiusMax,
            orientation: orientation,
            tilt: tilt,
            synthesized: synthesized,
            embedderId: embedderId)
    {
    }

    /// <summary>
    /// Creates an exit event from another event. Dart's <c>PointerExitEvent.fromMouseEvent</c>; see
    /// <see cref="PointerEnterEvent.FromMouseEvent"/>.
    /// </summary>
    public static PointerExitEvent FromMouseEvent(PointerEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (PointerExitEvent)new PointerExitEvent(
            viewId: @event.ViewId,
            timestampUtc: @event.TimestampUtc,
            pointer: @event.Pointer,
            kind: @event.Kind,
            device: @event.Device,
            position: @event.Position,
            delta: @event.Delta,
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
            down: @event.Down,
            synthesized: @event.Synthesized).Transformed(@event.Transform);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dart's <c>_CopyPointerExitEvent</c>: <c>pointer</c> and <c>down</c> are not forwarded, so they
    /// reset to 0 and false.
    /// </remarks>
    public override PointerExitEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null)
    {
        return (PointerExitEvent)new PointerExitEvent(
            viewId: viewId ?? ViewId,
            timestampUtc: timestampUtc ?? TimestampUtc,
            kind: kind ?? Kind,
            device: device ?? Device,
            position: position ?? Position,
            delta: delta ?? Delta,
            buttons: buttons ?? Buttons,
            obscured: obscured ?? Obscured,
            pressureMin: pressureMin ?? PressureMin,
            pressureMax: pressureMax ?? PressureMax,
            distance: distance ?? Distance,
            distanceMax: distanceMax ?? DistanceMax,
            size: size ?? Size,
            radiusMajor: radiusMajor ?? RadiusMajor,
            radiusMinor: radiusMinor ?? RadiusMinor,
            radiusMin: radiusMin ?? RadiusMin,
            radiusMax: radiusMax ?? RadiusMax,
            orientation: orientation ?? Orientation,
            tilt: tilt ?? Tilt,
            synthesized: synthesized ?? Synthesized,
            embedderId: embedderId ?? EmbedderId).Transformed(Transform);
    }
}

/// <summary>
/// The pointer has made contact with the device. Dart's <c>PointerDownEvent</c>: <c>down</c> is
/// always true, <c>distance</c> always 0.0 and <c>delta</c> always zero.
/// </summary>
public sealed class PointerDownEvent : PointerEvent
{
    public PointerDownEvent(
        int pointer = 0,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        Point position = default,
        PointerButtons buttons = PointerButtons.Primary,
        DateTime timestampUtc = default,
        int viewId = 0,
        int? device = null,
        bool obscured = false,
        double pressure = 1.0,
        double pressureMin = 1.0,
        double pressureMax = 1.0,
        double distanceMax = 0.0,
        double size = 0.0,
        double radiusMajor = 0.0,
        double radiusMinor = 0.0,
        double radiusMin = 0.0,
        double radiusMax = 0.0,
        double orientation = 0.0,
        double tilt = 0.0,
        int embedderId = 0)
        : base(
            viewId: viewId,
            timestampUtc: timestampUtc,
            pointer: pointer,
            kind: AssertNotTrackpad(kind),
            device: device,
            position: position,
            buttons: buttons,
            down: true,
            obscured: obscured,
            pressure: pressure,
            pressureMin: pressureMin,
            pressureMax: pressureMax,
            distance: 0.0,
            distanceMax: distanceMax,
            size: size,
            radiusMajor: radiusMajor,
            radiusMinor: radiusMinor,
            radiusMin: radiusMin,
            radiusMax: radiusMax,
            orientation: orientation,
            tilt: tilt,
            embedderId: embedderId)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dart's <c>_CopyPointerDownEvent</c>: <c>delta</c>, <c>distance</c> and <c>synthesized</c> are
    /// ignored.
    /// </remarks>
    public override PointerDownEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null)
    {
        return (PointerDownEvent)new PointerDownEvent(
            viewId: viewId ?? ViewId,
            timestampUtc: timestampUtc ?? TimestampUtc,
            pointer: pointer ?? Pointer,
            kind: kind ?? Kind,
            device: device ?? Device,
            position: position ?? Position,
            buttons: buttons ?? Buttons,
            obscured: obscured ?? Obscured,
            pressure: pressure ?? Pressure,
            pressureMin: pressureMin ?? PressureMin,
            pressureMax: pressureMax ?? PressureMax,
            distanceMax: distanceMax ?? DistanceMax,
            size: size ?? Size,
            radiusMajor: radiusMajor ?? RadiusMajor,
            radiusMinor: radiusMinor ?? RadiusMinor,
            radiusMin: radiusMin ?? RadiusMin,
            radiusMax: radiusMax ?? RadiusMax,
            orientation: orientation ?? Orientation,
            tilt: tilt ?? Tilt,
            embedderId: embedderId ?? EmbedderId).Transformed(Transform);
    }
}

/// <summary>
/// The pointer has moved with respect to the device while the pointer is in contact with the
/// device. Dart's <c>PointerMoveEvent</c>: <c>down</c> is always true and <c>distance</c> always 0.0.
/// </summary>
public sealed class PointerMoveEvent : PointerEvent
{
    public PointerMoveEvent(
        int pointer = 0,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        Point position = default,
        PointerButtons buttons = PointerButtons.Primary,
        DateTime timestampUtc = default,
        int viewId = 0,
        int? device = null,
        Point delta = default,
        bool obscured = false,
        double pressure = 1.0,
        double pressureMin = 1.0,
        double pressureMax = 1.0,
        double distanceMax = 0.0,
        double size = 0.0,
        double radiusMajor = 0.0,
        double radiusMinor = 0.0,
        double radiusMin = 0.0,
        double radiusMax = 0.0,
        double orientation = 0.0,
        double tilt = 0.0,
        int platformData = 0,
        bool synthesized = false,
        int embedderId = 0)
        : base(
            viewId: viewId,
            timestampUtc: timestampUtc,
            pointer: pointer,
            kind: AssertNotTrackpad(kind),
            device: device,
            position: position,
            delta: delta,
            buttons: buttons,
            down: true,
            obscured: obscured,
            pressure: pressure,
            pressureMin: pressureMin,
            pressureMax: pressureMax,
            distance: 0.0,
            distanceMax: distanceMax,
            size: size,
            radiusMajor: radiusMajor,
            radiusMinor: radiusMinor,
            radiusMin: radiusMin,
            radiusMax: radiusMax,
            orientation: orientation,
            tilt: tilt,
            platformData: platformData,
            synthesized: synthesized,
            embedderId: embedderId)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dart's <c>_CopyPointerMoveEvent</c>: <c>platformData</c> is not forwarded and resets to 0;
    /// <c>distance</c> is ignored.
    /// </remarks>
    public override PointerMoveEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null)
    {
        return (PointerMoveEvent)new PointerMoveEvent(
            viewId: viewId ?? ViewId,
            timestampUtc: timestampUtc ?? TimestampUtc,
            pointer: pointer ?? Pointer,
            kind: kind ?? Kind,
            device: device ?? Device,
            position: position ?? Position,
            delta: delta ?? Delta,
            buttons: buttons ?? Buttons,
            obscured: obscured ?? Obscured,
            pressure: pressure ?? Pressure,
            pressureMin: pressureMin ?? PressureMin,
            pressureMax: pressureMax ?? PressureMax,
            distanceMax: distanceMax ?? DistanceMax,
            size: size ?? Size,
            radiusMajor: radiusMajor ?? RadiusMajor,
            radiusMinor: radiusMinor ?? RadiusMinor,
            radiusMin: radiusMin ?? RadiusMin,
            radiusMax: radiusMax ?? RadiusMax,
            orientation: orientation ?? Orientation,
            tilt: tilt ?? Tilt,
            synthesized: synthesized ?? Synthesized,
            embedderId: embedderId ?? EmbedderId).Transformed(Transform);
    }
}

/// <summary>
/// The pointer has stopped making contact with the device. Dart's <c>PointerUpEvent</c>: pressure
/// defaults to 0.0 but may be reported (flutter/flutter#31340); <c>down</c> is always false.
/// </summary>
public sealed class PointerUpEvent : PointerEvent
{
    public PointerUpEvent(
        int pointer = 0,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        Point position = default,
        PointerButtons buttons = PointerButtons.None,
        DateTime timestampUtc = default,
        int viewId = 0,
        int? device = null,
        bool obscured = false,
        double pressure = 0.0,
        double pressureMin = 1.0,
        double pressureMax = 1.0,
        double distance = 0.0,
        double distanceMax = 0.0,
        double size = 0.0,
        double radiusMajor = 0.0,
        double radiusMinor = 0.0,
        double radiusMin = 0.0,
        double radiusMax = 0.0,
        double orientation = 0.0,
        double tilt = 0.0,
        int embedderId = 0)
        : base(
            viewId: viewId,
            timestampUtc: timestampUtc,
            pointer: pointer,
            kind: AssertNotTrackpad(kind),
            device: device,
            position: position,
            buttons: buttons,
            down: false,
            obscured: obscured,
            pressure: pressure,
            pressureMin: pressureMin,
            pressureMax: pressureMax,
            distance: distance,
            distanceMax: distanceMax,
            size: size,
            radiusMajor: radiusMajor,
            radiusMinor: radiusMinor,
            radiusMin: radiusMin,
            radiusMax: radiusMax,
            orientation: orientation,
            tilt: tilt,
            embedderId: embedderId)
    {
    }

    /// <inheritdoc />
    public override PointerUpEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null)
    {
        return CopyWith(
            viewId: viewId,
            timestampUtc: timestampUtc,
            pointer: pointer,
            kind: kind,
            device: device,
            position: position,
            localPosition: null,
            delta: delta,
            buttons: buttons,
            obscured: obscured,
            pressure: pressure,
            pressureMin: pressureMin,
            pressureMax: pressureMax,
            distance: distance,
            distanceMax: distanceMax,
            size: size,
            radiusMajor: radiusMajor,
            radiusMinor: radiusMinor,
            radiusMin: radiusMin,
            radiusMax: radiusMax,
            orientation: orientation,
            tilt: tilt,
            synthesized: synthesized,
            embedderId: embedderId);
    }

    /// <summary>
    /// Dart's <c>_CopyPointerUpEvent.copyWith</c>, which also takes a <c>localPosition</c> that it
    /// ignores; <c>delta</c> and <c>synthesized</c> are ignored too.
    /// </summary>
    public PointerUpEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? localPosition = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null)
    {
        _ = localPosition;
        return (PointerUpEvent)new PointerUpEvent(
            viewId: viewId ?? ViewId,
            timestampUtc: timestampUtc ?? TimestampUtc,
            pointer: pointer ?? Pointer,
            kind: kind ?? Kind,
            device: device ?? Device,
            position: position ?? Position,
            buttons: buttons ?? Buttons,
            obscured: obscured ?? Obscured,
            pressure: pressure ?? Pressure,
            pressureMin: pressureMin ?? PressureMin,
            pressureMax: pressureMax ?? PressureMax,
            distance: distance ?? Distance,
            distanceMax: distanceMax ?? DistanceMax,
            size: size ?? Size,
            radiusMajor: radiusMajor ?? RadiusMajor,
            radiusMinor: radiusMinor ?? RadiusMinor,
            radiusMin: radiusMin ?? RadiusMin,
            radiusMax: radiusMax ?? RadiusMax,
            orientation: orientation ?? Orientation,
            tilt: tilt ?? Tilt,
            embedderId: embedderId ?? EmbedderId).Transformed(Transform);
    }
}

/// <summary>
/// An event that corresponds to a discrete pointer signal, such as a scroll wheel tick. Dart's
/// <c>PointerSignalEvent</c> with the <c>_RespondablePointerEvent</c> mixin; the kind defaults to
/// <see cref="PointerDeviceKind.Mouse"/>.
/// </summary>
public abstract class PointerSignalEvent : PointerEvent
{
    protected PointerSignalEvent(
        int viewId = 0,
        DateTime timestampUtc = default,
        int pointer = 0,
        PointerDeviceKind kind = PointerDeviceKind.Mouse,
        int? device = null,
        Point position = default,
        int embedderId = 0)
        : base(
            viewId: viewId,
            timestampUtc: timestampUtc,
            pointer: pointer,
            kind: kind,
            device: device,
            position: position,
            embedderId: embedderId)
    {
    }

    /// <summary>
    /// Sends a response to the native embedder for this event. Dart's <c>respond</c>: with
    /// <paramref name="allowPlatformDefault"/> true the platform may run its default action (for
    /// example native scrolling on the web). May be called any number of times, but once true has
    /// been sent it cannot be taken back. The default does nothing.
    /// </summary>
    public virtual void Respond(bool allowPlatformDefault)
    {
    }
}

/// <summary>
/// The pointer issued a scroll event. Dart's <c>PointerScrollEvent</c>: it has no pointer of its
/// own (always 0) and no buttons; <see cref="ScrollDelta"/> is never transformed.
/// </summary>
public sealed class PointerScrollEvent : PointerSignalEvent
{
    private readonly RespondPointerEventCallback? _onRespond;

    public PointerScrollEvent(
        PointerDeviceKind kind = PointerDeviceKind.Mouse,
        Point position = default,
        Point scrollDelta = default,
        DateTime timestampUtc = default,
        RespondPointerEventCallback? onRespond = null,
        int viewId = 0,
        int? device = null,
        int embedderId = 0)
        : base(
            viewId: viewId,
            timestampUtc: timestampUtc,
            kind: kind,
            device: device,
            position: position,
            embedderId: embedderId)
    {
        ScrollDelta = scrollDelta;
        _onRespond = onRespond;
    }

    /// <summary>The amount to scroll, in logical pixels.</summary>
    public Point ScrollDelta { get; }

    /// <inheritdoc />
    public override void Respond(bool allowPlatformDefault)
    {
        _onRespond?.Invoke(allowPlatformDefault);
    }

    /// <inheritdoc />
    public override PointerScrollEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null)
    {
        return CopyWith(
            viewId: viewId,
            timestampUtc: timestampUtc,
            kind: kind,
            device: device,
            position: position,
            embedderId: embedderId,
            onRespond: null);
    }

    /// <summary>
    /// Dart's <c>_CopyPointerScrollEvent.copyWith</c>: <see cref="ScrollDelta"/> is always kept, and
    /// the response goes to <paramref name="onRespond"/> or, when that is null, to this event's
    /// <see cref="Respond"/>.
    /// </summary>
    public PointerScrollEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        int? embedderId = null,
        RespondPointerEventCallback? onRespond = null)
    {
        return (PointerScrollEvent)new PointerScrollEvent(
            viewId: viewId ?? ViewId,
            timestampUtc: timestampUtc ?? TimestampUtc,
            kind: kind ?? Kind,
            device: device ?? Device,
            position: position ?? Position,
            scrollDelta: ScrollDelta,
            embedderId: embedderId ?? EmbedderId,
            onRespond: onRespond ?? Respond).Transformed(Transform);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Point>("scrollDelta", ScrollDelta));
    }
}

/// <summary>
/// The pointer issued a scroll-inertia cancel event: the platform stopped the inertia of an earlier
/// scroll. Dart's <c>PointerScrollInertiaCancelEvent</c>; a trackpad may report it.
/// </summary>
public sealed class PointerScrollInertiaCancelEvent : PointerSignalEvent
{
    public PointerScrollInertiaCancelEvent(
        PointerDeviceKind kind = PointerDeviceKind.Mouse,
        Point position = default,
        DateTime timestampUtc = default,
        int viewId = 0,
        int? device = null,
        int embedderId = 0)
        : base(
            viewId: viewId,
            timestampUtc: timestampUtc,
            kind: kind,
            device: device,
            position: position,
            embedderId: embedderId)
    {
    }

    /// <inheritdoc />
    public override PointerScrollInertiaCancelEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null)
    {
        return (PointerScrollInertiaCancelEvent)new PointerScrollInertiaCancelEvent(
            viewId: viewId ?? ViewId,
            timestampUtc: timestampUtc ?? TimestampUtc,
            kind: kind ?? Kind,
            device: device ?? Device,
            position: position ?? Position,
            embedderId: embedderId ?? EmbedderId).Transformed(Transform);
    }
}

/// <summary>
/// The pointer issued a scale event: a discrete zoom request, such as a platform pinch reported
/// without a pan/zoom sequence. Dart's <c>PointerScaleEvent</c>.
/// </summary>
public sealed class PointerScaleEvent : PointerSignalEvent
{
    public PointerScaleEvent(
        PointerDeviceKind kind = PointerDeviceKind.Mouse,
        Point position = default,
        DateTime timestampUtc = default,
        int viewId = 0,
        int? device = null,
        int embedderId = 0,
        double scale = 1.0)
        : base(
            viewId: viewId,
            timestampUtc: timestampUtc,
            kind: kind,
            device: device,
            position: position,
            embedderId: embedderId)
    {
        Scale = scale;
    }

    /// <summary>The scale (zoom factor) of the event.</summary>
    public double Scale { get; }

    /// <inheritdoc />
    public override PointerScaleEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null)
    {
        return CopyWith(viewId, timestampUtc, kind, device, position, embedderId, scale: null);
    }

    /// <summary>Dart's <c>_CopyPointerScaleEvent.copyWith</c>, which also takes a <c>scale</c>.</summary>
    public PointerScaleEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        int? embedderId = null,
        double? scale = null)
    {
        return (PointerScaleEvent)new PointerScaleEvent(
            viewId: viewId ?? ViewId,
            timestampUtc: timestampUtc ?? TimestampUtc,
            kind: kind ?? Kind,
            device: device ?? Device,
            position: position ?? Position,
            embedderId: embedderId ?? EmbedderId,
            scale: scale ?? Scale).Transformed(Transform);
    }
}

/// <summary>
/// A pan/zoom gesture was started on a trackpad: the platform will report it as a stream of
/// <see cref="PointerPanZoomUpdateEvent"/>s until the matching <see cref="PointerPanZoomEndEvent"/>.
/// Dart's <c>PointerPanZoomStartEvent</c>.
/// </summary>
/// <remarks>
/// Like Dart's, the kind is hard-wired to <see cref="PointerDeviceKind.Trackpad"/>. The event is
/// deliberately not a <see cref="PointerSignalEvent"/>: it opens a pointer sequence with its own
/// arena and hit-test path, exactly like a pointer going down.
/// </remarks>
public sealed class PointerPanZoomStartEvent : PointerEvent
{
    public PointerPanZoomStartEvent(
        int pointer = 0,
        Point position = default,
        DateTime timestampUtc = default,
        int viewId = 0,
        int? device = null,
        int embedderId = 0,
        bool synthesized = false)
        : base(
            viewId: viewId,
            timestampUtc: timestampUtc,
            kind: PointerDeviceKind.Trackpad,
            device: device,
            pointer: pointer,
            position: position,
            embedderId: embedderId,
            synthesized: synthesized)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dart's <c>_CopyPointerPanZoomStartEvent</c>: the kind must stay a trackpad, and
    /// <c>pointer</c> and <c>synthesized</c> are not forwarded.
    /// </remarks>
    public override PointerPanZoomStartEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null)
    {
        AssertTrackpadOrNull(kind);
        return (PointerPanZoomStartEvent)new PointerPanZoomStartEvent(
            viewId: viewId ?? ViewId,
            timestampUtc: timestampUtc ?? TimestampUtc,
            device: device ?? Device,
            position: position ?? Position,
            embedderId: embedderId ?? EmbedderId).Transformed(Transform);
    }
}

/// <summary>
/// The trackpad reported new pan, zoom or rotation values for the pan/zoom gesture in progress.
/// Dart's <c>PointerPanZoomUpdateEvent</c>.
/// </summary>
public sealed class PointerPanZoomUpdateEvent : PointerEvent
{
    public PointerPanZoomUpdateEvent(
        int pointer = 0,
        Point position = default,
        DateTime timestampUtc = default,
        Point pan = default,
        Point panDelta = default,
        double scale = 1.0,
        double rotation = 0.0,
        int viewId = 0,
        int? device = null,
        int embedderId = 0,
        bool synthesized = false)
        : base(
            viewId: viewId,
            timestampUtc: timestampUtc,
            kind: PointerDeviceKind.Trackpad,
            device: device,
            pointer: pointer,
            position: position,
            embedderId: embedderId,
            synthesized: synthesized)
    {
        Pan = pan;
        LocalPan = pan;
        PanDelta = panDelta;
        LocalPanDelta = panDelta;
        Scale = scale;
        Rotation = rotation;
    }

    /// <summary>The total pan offset of the pan/zoom since it started.</summary>
    public Point Pan { get; }

    /// <summary><see cref="Pan"/> transformed into the event receiver's local coordinate system.</summary>
    public Point LocalPan { get; private set; }

    /// <summary>The amount the pan offset changed since the last event.</summary>
    public Point PanDelta { get; }

    /// <summary><see cref="PanDelta"/> transformed into the event receiver's local coordinate system.</summary>
    public Point LocalPanDelta { get; private set; }

    /// <summary>The scale (zoom factor) of the pan/zoom; 1.0 means no zoom.</summary>
    public double Scale { get; }

    /// <summary>The amount of rotation in radians since the pan/zoom started.</summary>
    public double Rotation { get; }

    /// <inheritdoc />
    public override PointerPanZoomUpdateEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null)
    {
        return CopyWith(
            viewId,
            timestampUtc,
            kind,
            device,
            position,
            embedderId,
            pan: null,
            localPan: null,
            panDelta: null,
            localPanDelta: null,
            scale: null,
            rotation: null);
    }

    /// <summary>
    /// Dart's <c>_CopyPointerPanZoomUpdateEvent.copyWith</c>: the kind must stay a trackpad,
    /// <paramref name="localPan"/> and <paramref name="localPanDelta"/> are accepted and ignored, and
    /// <c>pointer</c> and <c>synthesized</c> are not forwarded.
    /// </summary>
    public PointerPanZoomUpdateEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        int? embedderId = null,
        Point? pan = null,
        Point? localPan = null,
        Point? panDelta = null,
        Point? localPanDelta = null,
        double? scale = null,
        double? rotation = null)
    {
        AssertTrackpadOrNull(kind);
        _ = localPan;
        _ = localPanDelta;
        return (PointerPanZoomUpdateEvent)new PointerPanZoomUpdateEvent(
            viewId: viewId ?? ViewId,
            timestampUtc: timestampUtc ?? TimestampUtc,
            device: device ?? Device,
            position: position ?? Position,
            embedderId: embedderId ?? EmbedderId,
            pan: pan ?? Pan,
            panDelta: panDelta ?? PanDelta,
            scale: scale ?? Scale,
            rotation: rotation ?? Rotation).Transformed(Transform);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dart's <c>_TransformedPointerPanZoomUpdateEvent</c>: <c>localPan</c> is the point <c>pan</c>
    /// mapped through the transform, and <c>localPanDelta</c> is a delta anchored on <c>pan</c>.
    /// <see cref="Scale"/> and <see cref="Rotation"/> are not transformed.
    /// </remarks>
    private protected override void ApplyLocalTransform(Matrix4 transform)
    {
        LocalPan = TransformPosition(transform, Pan);
        LocalPanDelta = TransformDeltaViaPositions(
            untransformedEndPosition: Pan,
            untransformedDelta: PanDelta,
            transform: transform,
            transformedEndPosition: LocalPan);
    }
}

/// <summary>
/// The pan/zoom gesture in progress ended: the fingers left the trackpad. Dart's
/// <c>PointerPanZoomEndEvent</c>.
/// </summary>
public sealed class PointerPanZoomEndEvent : PointerEvent
{
    public PointerPanZoomEndEvent(
        int pointer = 0,
        Point position = default,
        DateTime timestampUtc = default,
        int viewId = 0,
        int? device = null,
        int embedderId = 0,
        bool synthesized = false)
        : base(
            viewId: viewId,
            timestampUtc: timestampUtc,
            kind: PointerDeviceKind.Trackpad,
            device: device,
            pointer: pointer,
            position: position,
            embedderId: embedderId,
            synthesized: synthesized)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dart's <c>_CopyPointerPanZoomEndEvent</c>: the kind must stay a trackpad, and <c>pointer</c>
    /// and <c>synthesized</c> are not forwarded.
    /// </remarks>
    public override PointerPanZoomEndEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null)
    {
        AssertTrackpadOrNull(kind);
        return (PointerPanZoomEndEvent)new PointerPanZoomEndEvent(
            viewId: viewId ?? ViewId,
            timestampUtc: timestampUtc ?? TimestampUtc,
            device: device ?? Device,
            position: position ?? Position,
            embedderId: embedderId ?? EmbedderId).Transformed(Transform);
    }
}

/// <summary>
/// The input from the pointer is no longer directed towards this receiver. Dart's
/// <c>PointerCancelEvent</c>: <c>down</c> is always false and <c>pressure</c> always 0.0.
/// </summary>
public sealed class PointerCancelEvent : PointerEvent
{
    public PointerCancelEvent(
        int pointer = 0,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        Point position = default,
        PointerButtons buttons = PointerButtons.None,
        DateTime timestampUtc = default,
        int viewId = 0,
        int? device = null,
        bool obscured = false,
        double pressureMin = 1.0,
        double pressureMax = 1.0,
        double distance = 0.0,
        double distanceMax = 0.0,
        double size = 0.0,
        double radiusMajor = 0.0,
        double radiusMinor = 0.0,
        double radiusMin = 0.0,
        double radiusMax = 0.0,
        double orientation = 0.0,
        double tilt = 0.0,
        int embedderId = 0)
        : base(
            viewId: viewId,
            timestampUtc: timestampUtc,
            pointer: pointer,
            kind: AssertNotTrackpad(kind),
            device: device,
            position: position,
            buttons: buttons,
            down: false,
            obscured: obscured,
            pressure: 0.0,
            pressureMin: pressureMin,
            pressureMax: pressureMax,
            distance: distance,
            distanceMax: distanceMax,
            size: size,
            radiusMajor: radiusMajor,
            radiusMinor: radiusMinor,
            radiusMin: radiusMin,
            radiusMax: radiusMax,
            orientation: orientation,
            tilt: tilt,
            embedderId: embedderId)
    {
    }

    /// <inheritdoc />
    public override PointerCancelEvent CopyWith(
        int? viewId = null,
        DateTime? timestampUtc = null,
        int? pointer = null,
        PointerDeviceKind? kind = null,
        int? device = null,
        Point? position = null,
        Point? delta = null,
        PointerButtons? buttons = null,
        bool? obscured = null,
        double? pressure = null,
        double? pressureMin = null,
        double? pressureMax = null,
        double? distance = null,
        double? distanceMax = null,
        double? size = null,
        double? radiusMajor = null,
        double? radiusMinor = null,
        double? radiusMin = null,
        double? radiusMax = null,
        double? orientation = null,
        double? tilt = null,
        bool? synthesized = null,
        int? embedderId = null)
    {
        return (PointerCancelEvent)new PointerCancelEvent(
            viewId: viewId ?? ViewId,
            timestampUtc: timestampUtc ?? TimestampUtc,
            pointer: pointer ?? Pointer,
            kind: kind ?? Kind,
            device: device ?? Device,
            position: position ?? Position,
            buttons: buttons ?? Buttons,
            obscured: obscured ?? Obscured,
            pressureMin: pressureMin ?? PressureMin,
            pressureMax: pressureMax ?? PressureMax,
            distance: distance ?? Distance,
            distanceMax: distanceMax ?? DistanceMax,
            size: size ?? Size,
            radiusMajor: radiusMajor ?? RadiusMajor,
            radiusMinor: radiusMinor ?? RadiusMinor,
            radiusMin: radiusMin ?? RadiusMin,
            radiusMax: radiusMax ?? RadiusMax,
            orientation: orientation ?? Orientation,
            tilt: tilt ?? Tilt,
            embedderId: embedderId ?? EmbedderId).Transformed(Transform);
    }
}
