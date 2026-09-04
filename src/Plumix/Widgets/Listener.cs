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
        Action<PointerDownEvent>? onPointerDown = null,
        Action<PointerMoveEvent>? onPointerMove = null,
        Action<PointerEnterEvent>? onPointerEnter = null,
        Action<PointerExitEvent>? onPointerExit = null,
        Action<PointerHoverEvent>? onPointerHover = null,
        Action<PointerUpEvent>? onPointerUp = null,
        Action<PointerCancelEvent>? onPointerCancel = null,
        Action<PointerPanZoomStartEvent>? onPointerPanZoomStart = null,
        Action<PointerPanZoomUpdateEvent>? onPointerPanZoomUpdate = null,
        Action<PointerPanZoomEndEvent>? onPointerPanZoomEnd = null,
        Action<PointerSignalEvent>? onPointerSignal = null,
        HitTestBehavior behavior = HitTestBehavior.DeferToChild,
        Key? key = null) : base(child, key)
    {
        OnPointerDown = onPointerDown;
        OnPointerMove = onPointerMove;
        OnPointerEnter = onPointerEnter;
        OnPointerExit = onPointerExit;
        OnPointerHover = onPointerHover;
        OnPointerUp = onPointerUp;
        OnPointerCancel = onPointerCancel;
        OnPointerPanZoomStart = onPointerPanZoomStart;
        OnPointerPanZoomUpdate = onPointerPanZoomUpdate;
        OnPointerPanZoomEnd = onPointerPanZoomEnd;
        OnPointerSignal = onPointerSignal;
        Behavior = behavior;
    }

    public Action<PointerDownEvent>? OnPointerDown { get; }

    public Action<PointerMoveEvent>? OnPointerMove { get; }

    public Action<PointerEnterEvent>? OnPointerEnter { get; }

    public Action<PointerExitEvent>? OnPointerExit { get; }

    public Action<PointerHoverEvent>? OnPointerHover { get; }

    public Action<PointerUpEvent>? OnPointerUp { get; }

    public Action<PointerCancelEvent>? OnPointerCancel { get; }

    /// <summary>Called when a trackpad pan/zoom gesture starts over this widget.</summary>
    public Action<PointerPanZoomStartEvent>? OnPointerPanZoomStart { get; }

    /// <summary>Called when the trackpad pan/zoom gesture in progress reports new values.</summary>
    public Action<PointerPanZoomUpdateEvent>? OnPointerPanZoomUpdate { get; }

    /// <summary>Called when the trackpad pan/zoom gesture in progress ends.</summary>
    public Action<PointerPanZoomEndEvent>? OnPointerPanZoomEnd { get; }

    public Action<PointerSignalEvent>? OnPointerSignal { get; }

    public HitTestBehavior Behavior { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderPointerListener(
            onPointerDown: OnPointerDown,
            onPointerMove: OnPointerMove,
            onPointerEnter: OnPointerEnter,
            onPointerExit: OnPointerExit,
            onPointerHover: OnPointerHover,
            onPointerUp: OnPointerUp,
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
        listener.OnPointerEnter = OnPointerEnter;
        listener.OnPointerExit = OnPointerExit;
        listener.OnPointerHover = OnPointerHover;
        listener.OnPointerUp = OnPointerUp;
        listener.OnPointerCancel = OnPointerCancel;
        listener.OnPointerPanZoomStart = OnPointerPanZoomStart;
        listener.OnPointerPanZoomUpdate = OnPointerPanZoomUpdate;
        listener.OnPointerPanZoomEnd = OnPointerPanZoomEnd;
        listener.OnPointerSignal = OnPointerSignal;
        listener.Behavior = Behavior;
    }
}
