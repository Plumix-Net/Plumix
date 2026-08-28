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

    public PointerDeviceKind Kind { get; }

    public PointerButtons Buttons { get; }

    public bool Down { get; }

    public DateTime TimestampUtc { get; }

    public Point Position { get; }

    public Point LocalPosition { get; private set; }

    public Point Delta { get; private set; }

    public Point LocalDelta { get; private set; }

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
        return clone;
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
        : base(pointer, kind, position, buttons, down: true, timestampUtc)
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
        : base(pointer, kind, position, buttons, down, timestampUtc)
    {
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
        DateTime timestampUtc)
        : base(pointer, kind, position, buttons, down: false, timestampUtc)
    {
    }
}

public sealed class PointerExitEvent : PointerEvent
{
    public PointerExitEvent(
        int pointer,
        PointerDeviceKind kind,
        Point position,
        PointerButtons buttons,
        DateTime timestampUtc)
        : base(pointer, kind, position, buttons, down: false, timestampUtc)
    {
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
        : base(pointer, kind, position, buttons, down: false, timestampUtc)
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
        : base(pointer, kind, position, buttons, down: false, timestampUtc)
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
