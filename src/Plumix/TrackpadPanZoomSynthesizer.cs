using Avalonia;
using Plumix.Gestures;
using Plumix.UI;

// C#-only infrastructure. Flutter receives `PointerChange.panZoomStart/panZoomUpdate/panZoomEnd`
// straight from the engine, with the gesture phase already resolved, and `PointerEventConverter`
// only has to widen the numbers (`gestures/converter.dart`). Avalonia reports trackpad gestures as
// bare per-delta events (`PointerTouchPadGestureMagnify`/`Rotate`/`Swipe`, raised from
// `AvnView.mm`'s `magnifyWithEvent:`/`rotateWithEvent:`) and drops `NSEvent.phase` on the way, so a
// host has no begin/end signal to convert. This class rebuilds the sequence a host cannot observe:
// the first delta opens it, later deltas accumulate into it, and an idle interval closes it.
// See docs/ai/DIVERGENCES.md.

namespace Plumix;

/// <summary>
/// Turns the phase-less trackpad deltas an Avalonia host receives into the
/// <see cref="PointerPanZoomStartEvent"/> / <see cref="PointerPanZoomUpdateEvent"/> /
/// <see cref="PointerPanZoomEndEvent"/> sequence the framework expects.
/// </summary>
public sealed class TrackpadPanZoomSynthesizer
{
    /// <summary>
    /// How long the synthesizer waits for another delta before ending the gesture. Long enough to
    /// bridge the gap between two frames of a slow pinch, short enough that a finished gesture ends
    /// before the user starts the next one.
    /// </summary>
    public static readonly TimeSpan DefaultIdleTimeout = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Pointer ids for synthesized gestures count down from -1. Avalonia's own pointer ids are
    /// non-negative, so a synthesized gesture can never collide with a real pointer in the
    /// binding's hit-test cache or in the gesture arena.
    /// </summary>
    private static int _nextPointer = -1;

    private readonly Action<PointerEvent> _dispatch;
    private readonly TimeSpan _idleTimeout;

    private GestureTimer? _idleTimer;
    private int? _pointer;
    private Point _position;
    private Point _pan;
    private double _scale = 1.0;
    private double _rotation;

    public TrackpadPanZoomSynthesizer(Action<PointerEvent> dispatch, TimeSpan? idleTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        _dispatch = dispatch;
        _idleTimeout = idleTimeout ?? DefaultIdleTimeout;
    }

    /// <summary>Whether a synthesized pan/zoom sequence is currently open.</summary>
    public bool IsActive => _pointer is not null;

    /// <summary>Reports a two-finger pan of <paramref name="panDelta"/> logical pixels.</summary>
    public void Pan(Point position, Point panDelta, DateTime timestampUtc)
    {
        Update(position, timestampUtc, panDelta: panDelta);
    }

    /// <summary>
    /// Reports a pinch that multiplies the gesture's zoom by <paramref name="scaleFactor"/>.
    /// macOS reports <c>NSEvent.magnification</c>, an additive increment, so a host passes
    /// <c>1 + magnification</c>.
    /// </summary>
    public void Zoom(Point position, double scaleFactor, DateTime timestampUtc)
    {
        Update(position, timestampUtc, scaleFactor: scaleFactor);
    }

    /// <summary>Reports a rotation of <paramref name="rotationDelta"/> radians, clockwise positive.</summary>
    public void Rotate(Point position, double rotationDelta, DateTime timestampUtc)
    {
        Update(position, timestampUtc, rotationDelta: rotationDelta);
    }

    /// <summary>
    /// Ends the open sequence now instead of waiting for the idle timeout. A host calls this when
    /// something else takes over the pointer — a button going down, or capture being lost.
    /// </summary>
    public void End(DateTime? timestampUtc = null)
    {
        if (_pointer is not { } pointer)
        {
            return;
        }

        _idleTimer?.Cancel();
        _idleTimer = null;
        _pointer = null;
        _pan = default;
        _scale = 1.0;
        _rotation = 0.0;
        _dispatch(new PointerPanZoomEndEvent(pointer, _position, timestampUtc ?? DateTime.UtcNow));
    }

    private void Update(
        Point position,
        DateTime timestampUtc,
        Point panDelta = default,
        double scaleFactor = 1.0,
        double rotationDelta = 0.0)
    {
        _position = position;
        if (_pointer is null)
        {
            _pointer = Interlocked.Decrement(ref _nextPointer) + 1;
            _pan = default;
            _scale = 1.0;
            _rotation = 0.0;
            _dispatch(new PointerPanZoomStartEvent(_pointer.Value, position, timestampUtc));
        }

        _pan += panDelta;
        _scale *= scaleFactor;
        _rotation += rotationDelta;
        _dispatch(new PointerPanZoomUpdateEvent(
            pointer: _pointer.Value,
            position: position,
            timestampUtc: timestampUtc,
            pan: _pan,
            panDelta: panDelta,
            scale: _scale,
            rotation: _rotation));

        _idleTimer?.Cancel();
        _idleTimer = GestureTimer.Start(_idleTimeout, () => End());
    }
}
