using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart

namespace Plumix.Widgets;

/// <summary>
/// A widget that calls callbacks in response to raw pointer events. Dart's `Listener`, which lives
/// in `basic.dart` next to the other proxy widgets even though `RawGestureDetector` is its main
/// consumer.
/// </summary>
public sealed class Listener : SingleChildRenderObjectWidget
{
    public Listener(
        Widget? child = null,
        PointerDownEventListener? onPointerDown = null,
        PointerMoveEventListener? onPointerMove = null,
        PointerUpEventListener? onPointerUp = null,
        PointerHoverEventListener? onPointerHover = null,
        PointerCancelEventListener? onPointerCancel = null,
        PointerPanZoomStartEventListener? onPointerPanZoomStart = null,
        PointerPanZoomUpdateEventListener? onPointerPanZoomUpdate = null,
        PointerPanZoomEndEventListener? onPointerPanZoomEnd = null,
        PointerSignalEventListener? onPointerSignal = null,
        HitTestBehavior behavior = HitTestBehavior.DeferToChild,
        Key? key = null) : base(child, key)
    {
        OnPointerDown = onPointerDown;
        OnPointerMove = onPointerMove;
        OnPointerUp = onPointerUp;
        OnPointerHover = onPointerHover;
        OnPointerCancel = onPointerCancel;
        OnPointerPanZoomStart = onPointerPanZoomStart;
        OnPointerPanZoomUpdate = onPointerPanZoomUpdate;
        OnPointerPanZoomEnd = onPointerPanZoomEnd;
        OnPointerSignal = onPointerSignal;
        Behavior = behavior;
    }

    public PointerDownEventListener? OnPointerDown { get; }

    public PointerMoveEventListener? OnPointerMove { get; }

    public PointerUpEventListener? OnPointerUp { get; }

    public PointerHoverEventListener? OnPointerHover { get; }

    public PointerCancelEventListener? OnPointerCancel { get; }

    /// <summary>Called when a trackpad pan/zoom gesture starts over this widget.</summary>
    public PointerPanZoomStartEventListener? OnPointerPanZoomStart { get; }

    /// <summary>Called when the trackpad pan/zoom gesture in progress reports new values.</summary>
    public PointerPanZoomUpdateEventListener? OnPointerPanZoomUpdate { get; }

    /// <summary>Called when the trackpad pan/zoom gesture in progress ends.</summary>
    public PointerPanZoomEndEventListener? OnPointerPanZoomEnd { get; }

    public PointerSignalEventListener? OnPointerSignal { get; }

    public HitTestBehavior Behavior { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderPointerListener(
            onPointerDown: OnPointerDown,
            onPointerMove: OnPointerMove,
            onPointerUp: OnPointerUp,
            onPointerHover: OnPointerHover,
            onPointerCancel: OnPointerCancel,
            onPointerPanZoomStart: OnPointerPanZoomStart,
            onPointerPanZoomUpdate: OnPointerPanZoomUpdate,
            onPointerPanZoomEnd: OnPointerPanZoomEnd,
            onPointerSignal: OnPointerSignal,
            behavior: Behavior);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var listener = (RenderPointerListener)renderObject;
        listener.OnPointerDown = OnPointerDown;
        listener.OnPointerMove = OnPointerMove;
        listener.OnPointerUp = OnPointerUp;
        listener.OnPointerHover = OnPointerHover;
        listener.OnPointerCancel = OnPointerCancel;
        listener.OnPointerPanZoomStart = OnPointerPanZoomStart;
        listener.OnPointerPanZoomUpdate = OnPointerPanZoomUpdate;
        listener.OnPointerPanZoomEnd = OnPointerPanZoomEnd;
        listener.OnPointerSignal = OnPointerSignal;
        listener.Behavior = Behavior;
    }
}
