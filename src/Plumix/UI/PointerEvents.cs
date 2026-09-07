using Avalonia;

// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/events.dart (approximate)

namespace Plumix.UI;

public enum PointerDeviceKind
{
    Touch,
    Mouse,
    Stylus,
    InvertedStylus,
    Trackpad,
    Unknown
}

[Flags]
public enum PointerButtons
{
    None = 0,
    Primary = 1 << 0,
    Secondary = 1 << 1,
    Middle = 1 << 2
}

public abstract class PointerEvent
{
    private readonly int? _device;

    protected PointerEvent(
        int pointer,
        PointerDeviceKind kind,
        Point position,
        PointerButtons buttons,
        bool down,
        DateTime timestampUtc)
    {
        Pointer = pointer;
        Kind = kind;
        Position = position;
        LocalPosition = position;
        Delta = default;
        LocalDelta = default;
        Buttons = buttons;
        Down = down;
        TimestampUtc = timestampUtc;
    }

    public int Pointer { get; }

    /// <summary>
    /// Unique identifier for the pointing device that produced this event. Dart's
    /// `PointerEvent.device`; <see cref="Plumix.Rendering.MouseTracker"/> keys its per-device state
    /// on it, so two mice hovering at once keep separate enter/exit and cursor state.
    /// </summary>
    /// <remarks>
    /// Dart defaults it to 0 because its engine allocates pointer ids per gesture and device ids per
    /// device. Plumix's hosts get one identifier per device from the platform and pass it as
    /// <see cref="Pointer"/>, so an event that does not set this reports its pointer as its device.
    /// </remarks>
    public int Device
    {
        get => _device ?? Pointer;
        init => _device = value;
    }

    /// <summary>
    /// The identifier of the view this event came from. Dart's `PointerEvent.viewId`; the mouse
    /// tracker stores it so a frame-driven re-hit-test goes to the same view the pointer is over.
    /// </summary>
    public int ViewId { get; init; }

    public PointerDeviceKind Kind { get; }

    public PointerButtons Buttons { get; }

    public bool Down { get; }

    public DateTime TimestampUtc { get; }

    public Point Position { get; }

    public Point LocalPosition { get; private set; }

    public Point Delta { get; private set; }

    public Point LocalDelta { get; private set; }

    /// <summary>
    /// The pressure of the touch, normalised against <see cref="PressureMin"/> and
    /// <see cref="PressureMax"/>. Dart's `PointerEvent.pressure`; devices without a force sensor
    /// report the default 1.0.
    /// </summary>
    public double Pressure { get; init; } = 1.0;

    /// <summary>
    /// The lowest value <see cref="Pressure"/> can report on this pointer's device. Dart's
    /// `PointerEvent.pressureMin`.
    /// </summary>
    public double PressureMin { get; init; } = 1.0;

    /// <summary>
    /// The highest value <see cref="Pressure"/> can report on this pointer's device. A device
    /// without pressure sensing reports 1.0, which is how
    /// <see cref="Plumix.Gestures.ForcePressGestureRecognizer"/> detects that it must not compete.
    /// Dart's `PointerEvent.pressureMax`.
    /// </summary>
    public double PressureMax { get; init; } = 1.0;

    /// <summary>
    /// The untransformed event this event was derived from, or null when this is the original
    /// event. Dart's `PointerEvent.original`; the <see cref="Plumix.Gestures.PointerSignalResolver"/>
    /// uses it to identify transformed copies of one signal event.
    /// </summary>
    public PointerEvent? Original { get; private set; }

    /// <summary>
    /// Whether the framework generated this event rather than the platform reporting it. Dart's
    /// `PointerEvent.synthesized`; synthesized moves are excluded from velocity tracking.
    /// </summary>
    public bool Synthesized { get; init; }

    /// <summary>
    /// The transform that maps this event's global coordinates into the local space of the object
    /// that received it, or null when no transform has been applied. Dart's `PointerEvent.transform`.
    /// </summary>
    public Matrix4? Transform { get; private set; }

    /// <summary>
    /// Returns a copy of this event whose local coordinates are this event's global coordinates
    /// mapped through <paramref name="transform"/>. Ports Dart's `PointerEvent.transformed`: a null
    /// transform, or the transform this event already carries, returns the event untouched.
    /// </summary>
    public PointerEvent Transformed(Matrix4? transform)
    {
        if (transform is null || ReferenceEquals(transform, Transform))
        {
            return this;
        }

        var clone = (PointerEvent)MemberwiseClone();
        clone.Original = Original ?? this;
        clone.Transform = transform;
        clone.LocalPosition = TransformPosition(transform, Position);
        clone.LocalDelta = Plumix.Gestures.PointerEventUtils.TransformDeltaViaPositions(
            untransformedEndPosition: Position,
            untransformedDelta: Delta,
            transform: transform,
            transformedEndPosition: clone.LocalPosition);
        clone.ApplyLocalTransform(transform);
        return clone;
    }

    /// <summary>
    /// Maps the local copies of any coordinates a subclass adds on top of position and delta.
    /// Called on the freshly transformed clone after its <see cref="LocalPosition"/> and
    /// <see cref="LocalDelta"/> have been computed. Dart gets this for free from its
    /// `_TransformedPointer*Event` classes, which recompute every local field themselves.
    /// </summary>
    protected virtual void ApplyLocalTransform(Matrix4 transform)
    {
    }

    /// <summary>
    /// Maps <paramref name="position"/> through <paramref name="transform"/> after removing the
    /// perspective row and column. Dart's static `PointerEvent.transformPosition`.
    /// </summary>
    public static Point TransformPosition(Matrix4? transform, Point position)
    {
        if (transform is null)
        {
            return position;
        }

        Matrix4 flattened = Plumix.Gestures.PointerEventUtils.RemovePerspectiveTransform(transform);
        return Plumix.Rendering.MatrixUtils.TransformPoint(flattened, position);
    }

    /// <summary>
    /// Throws when a pointer event type that the platform never reports for a trackpad is built
    /// with <see cref="PointerDeviceKind.Trackpad"/>. Ports the
    /// `assert(!identical(kind, PointerDeviceKind.trackpad))` that Dart's `PointerDownEvent`,
    /// `PointerMoveEvent`, `PointerUpEvent` and `PointerCancelEvent` constructors carry: a trackpad
    /// reports its gestures as the `PointerPanZoom*` events instead.
    /// </summary>
    protected static PointerDeviceKind AssertNotTrackpad(PointerDeviceKind kind)
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
    /// Copies <paramref name="source"/>'s delta onto this freshly built event. The synthesized
    /// enter/exit events Dart builds with `fromMouseEvent` carry the triggering event's delta.
    /// </summary>
    private protected void CopyDeltaFrom(PointerEvent source)
    {
        Delta = source.Delta;
        LocalDelta = source.Delta;
    }

    internal PointerEvent WithDelta(Point delta)
    {
        var clone = (PointerEvent)MemberwiseClone();
        clone.Delta = delta;
        clone.LocalDelta = delta;
        clone.Original = Original ?? this;
        return clone;
    }

    internal PointerEvent WithLocalCoordinates(Point localPosition, Point localDelta)
    {
        var clone = (PointerEvent)MemberwiseClone();
        clone.LocalPosition = localPosition;
        clone.LocalDelta = localDelta;
        clone.Original = Original ?? this;
        return clone;
    }
}

public sealed class PointerDownEvent : PointerEvent
{
    public PointerDownEvent(
        int pointer,
        PointerDeviceKind kind,
        Point position,
        PointerButtons buttons,
        DateTime timestampUtc)
        : base(pointer, AssertNotTrackpad(kind), position, buttons, down: true, timestampUtc)
    {
    }
}

public sealed class PointerMoveEvent : PointerEvent
{
    public PointerMoveEvent(
        int pointer,
        PointerDeviceKind kind,
        Point position,
        PointerButtons buttons,
        bool down,
        DateTime timestampUtc)
        : base(pointer, AssertNotTrackpad(kind), position, buttons, down, timestampUtc)
    {
    }
}

/// <summary>
/// The device has started tracking the pointer. Dart's `PointerAddedEvent`: for a mouse this is the
/// moment the pointer becomes able to produce hover events, and it is what makes
/// <see cref="Plumix.Rendering.MouseTracker.MouseIsConnected"/> true.
/// </summary>
public sealed class PointerAddedEvent : PointerEvent
{
    public PointerAddedEvent(
        int pointer,
        PointerDeviceKind kind,
        Point position,
        PointerButtons buttons = PointerButtons.None,
        DateTime timestampUtc = default)
        : base(pointer, kind, position, buttons, down: false, timestampUtc)
    {
        Pressure = 0.0;
    }
}

/// <summary>
/// The device is no longer tracking the pointer. Dart's `PointerRemovedEvent`: the mouse tracker
/// treats it as an exit from every annotation and drops the device's cursor session.
/// </summary>
public sealed class PointerRemovedEvent : PointerEvent
{
    public PointerRemovedEvent(
        int pointer,
        PointerDeviceKind kind,
        Point position,
        PointerButtons buttons = PointerButtons.None,
        DateTime timestampUtc = default)
        : base(pointer, kind, position, buttons, down: false, timestampUtc)
    {
        Pressure = 0.0;
    }
}

public sealed class PointerHoverEvent : PointerEvent
{
    public PointerHoverEvent(
        int pointer,
        PointerDeviceKind kind,
        Point position,
        PointerButtons buttons,
        DateTime timestampUtc)
        : base(pointer, kind, position, buttons, down: false, timestampUtc)
    {
    }
}

public sealed class PointerEnterEvent : PointerEvent
{
    public PointerEnterEvent(
        int pointer,
        PointerDeviceKind kind,
        Point position,
        PointerButtons buttons,
        DateTime timestampUtc,
        bool down = false)
        : base(pointer, kind, position, buttons, down, timestampUtc)
    {
        Pressure = 0.0;
    }

    /// <summary>
    /// Creates an enter event from the mouse event that triggered the region change. Dart's
    /// `PointerEnterEvent.fromMouseEvent`: every field is copied except the pressure, which is
    /// forced to zero, and the result is transformed by the source event's own transform.
    /// </summary>
    public static PointerEnterEvent FromMouseEvent(PointerEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var result = new PointerEnterEvent(
            pointer: @event.Pointer,
            kind: @event.Kind,
            position: @event.Position,
            buttons: @event.Buttons,
            timestampUtc: @event.TimestampUtc,
            down: @event.Down)
        {
            Device = @event.Device,
            ViewId = @event.ViewId,
            PressureMin = @event.PressureMin,
            PressureMax = @event.PressureMax,
            Synthesized = @event.Synthesized,
        };
        result.CopyDeltaFrom(@event);
        return (PointerEnterEvent)result.Transformed(@event.Transform);
    }
}

public sealed class PointerExitEvent : PointerEvent
{
    public PointerExitEvent(
        int pointer,
        PointerDeviceKind kind,
        Point position,
        PointerButtons buttons,
        DateTime timestampUtc,
        bool down = false)
        : base(pointer, kind, position, buttons, down, timestampUtc)
    {
        Pressure = 0.0;
    }

    /// <summary>
    /// Creates an exit event from the mouse event that triggered the region change. Dart's
    /// `PointerExitEvent.fromMouseEvent`; see <see cref="PointerEnterEvent.FromMouseEvent"/>.
    /// </summary>
    public static PointerExitEvent FromMouseEvent(PointerEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var result = new PointerExitEvent(
            pointer: @event.Pointer,
            kind: @event.Kind,
            position: @event.Position,
            buttons: @event.Buttons,
            timestampUtc: @event.TimestampUtc,
            down: @event.Down)
        {
            Device = @event.Device,
            ViewId = @event.ViewId,
            PressureMin = @event.PressureMin,
            PressureMax = @event.PressureMax,
            Synthesized = @event.Synthesized,
        };
        result.CopyDeltaFrom(@event);
        return (PointerExitEvent)result.Transformed(@event.Transform);
    }
}

public sealed class PointerUpEvent : PointerEvent
{
    public PointerUpEvent(
        int pointer,
        PointerDeviceKind kind,
        Point position,
        PointerButtons buttons,
        DateTime timestampUtc)
        : base(pointer, AssertNotTrackpad(kind), position, buttons, down: false, timestampUtc)
    {
    }
}

public sealed class PointerCancelEvent : PointerEvent
{
    public PointerCancelEvent(
        int pointer,
        PointerDeviceKind kind,
        Point position,
        PointerButtons buttons,
        DateTime timestampUtc)
        : base(pointer, AssertNotTrackpad(kind), position, buttons, down: false, timestampUtc)
    {
    }
}

public abstract class PointerSignalEvent : PointerEvent
{
    protected PointerSignalEvent(
        int pointer,
        PointerDeviceKind kind,
        Point position,
        PointerButtons buttons,
        DateTime timestampUtc)
        : base(pointer, kind, position, buttons, down: false, timestampUtc)
    {
    }

    /// <summary>
    /// Dart's `respond`: lets the framework tell the platform whether its default action (for
    /// example native scrolling on the web) should still run for this signal.
    /// </summary>
    public virtual void Respond(bool allowPlatformDefault)
    {
    }
}

public sealed class PointerScrollEvent : PointerSignalEvent
{
    private readonly Action<bool>? _onRespond;

    public PointerScrollEvent(
        int pointer,
        PointerDeviceKind kind,
        Point position,
        PointerButtons buttons,
        Point scrollDelta,
        DateTime timestampUtc,
        Action<bool>? onRespond = null)
        : base(pointer, kind, position, buttons, timestampUtc)
    {
        ScrollDelta = scrollDelta;
        _onRespond = onRespond;
    }

    public Point ScrollDelta { get; }

    public override void Respond(bool allowPlatformDefault)
    {
        _onRespond?.Invoke(allowPlatformDefault);
    }
}

/// <summary>
/// The pointer issued a scroll-inertia cancel event: the platform stopped the inertia of an
/// earlier scroll. Ports Dart's `PointerScrollInertiaCancelEvent` (`gestures/events.dart`).
/// </summary>
public sealed class PointerScrollInertiaCancelEvent : PointerSignalEvent
{
    public PointerScrollInertiaCancelEvent(
        int pointer,
        PointerDeviceKind kind,
        Point position,
        PointerButtons buttons,
        DateTime timestampUtc)
        : base(pointer, kind, position, buttons, timestampUtc)
    {
    }
}

/// <summary>
/// A pan/zoom gesture started on a trackpad: the platform put two or more fingers down and will
/// report the gesture as a stream of <see cref="PointerPanZoomUpdateEvent"/>s until the matching
/// <see cref="PointerPanZoomEndEvent"/>. Ports Dart's `PointerPanZoomStartEvent`
/// (`gestures/events.dart`).
/// </summary>
/// <remarks>
/// Like Dart's, the kind is hard-wired to <see cref="PointerDeviceKind.Trackpad"/>: no other device
/// reports pan/zoom. The event is deliberately not a <see cref="PointerSignalEvent"/> — it opens a
/// pointer sequence with its own arena and hit-test path, exactly like a pointer going down.
/// </remarks>
public sealed class PointerPanZoomStartEvent : PointerEvent
{
    public PointerPanZoomStartEvent(int pointer, Point position, DateTime timestampUtc)
        : base(pointer, PointerDeviceKind.Trackpad, position, PointerButtons.None, down: false, timestampUtc)
    {
    }
}

/// <summary>
/// The trackpad reported new pan, zoom or rotation values for the pan/zoom gesture in progress.
/// Ports Dart's `PointerPanZoomUpdateEvent` (`gestures/events.dart`).
/// </summary>
public sealed class PointerPanZoomUpdateEvent : PointerEvent
{
    public PointerPanZoomUpdateEvent(
        int pointer,
        Point position,
        DateTime timestampUtc,
        Point pan = default,
        Point panDelta = default,
        double scale = 1.0,
        double rotation = 0.0)
        : base(pointer, PointerDeviceKind.Trackpad, position, PointerButtons.None, down: false, timestampUtc)
    {
        Pan = pan;
        LocalPan = pan;
        PanDelta = panDelta;
        LocalPanDelta = panDelta;
        Scale = scale;
        Rotation = rotation;
    }

    /// <summary>The total pan offset accumulated since the gesture started.</summary>
    public Point Pan { get; }

    /// <summary><see cref="Pan"/> in the coordinate space of the object that received the event.</summary>
    public Point LocalPan { get; private set; }

    /// <summary>How much <see cref="Pan"/> changed since the previous update.</summary>
    public Point PanDelta { get; }

    /// <summary><see cref="PanDelta"/> in the local coordinate space.</summary>
    public Point LocalPanDelta { get; private set; }

    /// <summary>The zoom factor of the gesture; <c>1.0</c> means no zoom.</summary>
    public double Scale { get; }

    /// <summary>How far the gesture has rotated, in radians, since it started.</summary>
    public double Rotation { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Dart's `_TransformedPointerPanZoomUpdateEvent`: `localPan` is the *point* `pan` mapped
    /// through the transform, while `localPanDelta` is a delta anchored on `pan` rather than on
    /// `position`. <see cref="Scale"/> and <see cref="Rotation"/> are not transformed.
    /// </remarks>
    protected override void ApplyLocalTransform(Matrix4 transform)
    {
        LocalPan = TransformPosition(transform, Pan);
        LocalPanDelta = Plumix.Gestures.PointerEventUtils.TransformDeltaViaPositions(
            untransformedEndPosition: Pan,
            untransformedDelta: PanDelta,
            transform: transform,
            transformedEndPosition: LocalPan);
    }
}

/// <summary>
/// The pan/zoom gesture in progress ended: the fingers left the trackpad. Ports Dart's
/// `PointerPanZoomEndEvent` (`gestures/events.dart`).
/// </summary>
public sealed class PointerPanZoomEndEvent : PointerEvent
{
    public PointerPanZoomEndEvent(int pointer, Point position, DateTime timestampUtc)
        : base(pointer, PointerDeviceKind.Trackpad, position, PointerButtons.None, down: false, timestampUtc)
    {
    }
}
