using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart

namespace Plumix.Rendering;

/// <summary>Signature for listening to <see cref="PointerDownEvent"/> events.</summary>
public delegate void PointerDownEventListener(PointerDownEvent @event);

/// <summary>Signature for listening to <see cref="PointerMoveEvent"/> events.</summary>
public delegate void PointerMoveEventListener(PointerMoveEvent @event);

/// <summary>Signature for listening to <see cref="PointerUpEvent"/> events.</summary>
public delegate void PointerUpEventListener(PointerUpEvent @event);

/// <summary>Signature for listening to <see cref="PointerCancelEvent"/> events.</summary>
public delegate void PointerCancelEventListener(PointerCancelEvent @event);

/// <summary>Signature for listening to <see cref="PointerPanZoomStartEvent"/> events.</summary>
public delegate void PointerPanZoomStartEventListener(PointerPanZoomStartEvent @event);

/// <summary>Signature for listening to <see cref="PointerPanZoomUpdateEvent"/> events.</summary>
public delegate void PointerPanZoomUpdateEventListener(PointerPanZoomUpdateEvent @event);

/// <summary>Signature for listening to <see cref="PointerPanZoomEndEvent"/> events.</summary>
public delegate void PointerPanZoomEndEventListener(PointerPanZoomEndEvent @event);

/// <summary>Signature for listening to <see cref="PointerSignalEvent"/> events.</summary>
public delegate void PointerSignalEventListener(PointerSignalEvent @event);

/// <summary>Calls callbacks in response to common pointer events.</summary>
public class RenderPointerListener : RenderProxyBoxWithHitTestBehavior
{
    /// <summary>Creates a render object that forwards pointer events to callbacks.</summary>
    public RenderPointerListener(
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
        RenderBox? child = null) : base(behavior, child)
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
    }

    /// <summary>Called when a pointer comes into contact with the screen at this object.</summary>
    public PointerDownEventListener? OnPointerDown { get; set; }

    /// <summary>Called when a pointer that triggered an OnPointerDown changes position.</summary>
    public PointerMoveEventListener? OnPointerMove { get; set; }

    /// <summary>Called when a pointer that triggered an OnPointerDown is no longer in contact.</summary>
    public PointerUpEventListener? OnPointerUp { get; set; }

    /// <summary>Called when a pointer that has not an OnPointerDown changes position.</summary>
    public PointerHoverEventListener? OnPointerHover { get; set; }

    /// <summary>Called when the input from a pointer that triggered an OnPointerDown is no longer
    /// directed towards this receiver.</summary>
    public PointerCancelEventListener? OnPointerCancel { get; set; }

    /// <summary>Called when a pan/zoom begins such as from a trackpad gesture.</summary>
    public PointerPanZoomStartEventListener? OnPointerPanZoomStart { get; set; }

    /// <summary>Called when a pan/zoom is updated.</summary>
    public PointerPanZoomUpdateEventListener? OnPointerPanZoomUpdate { get; set; }

    /// <summary>Called when a pan/zoom finishes.</summary>
    public PointerPanZoomEndEventListener? OnPointerPanZoomEnd { get; set; }

    /// <summary>Called when a pointer signal occurs over this object.</summary>
    public PointerSignalEventListener? OnPointerSignal { get; set; }

    /// <inheritdoc />
    public override Size ComputeSizeForNoChild(BoxConstraints constraints) => constraints.Biggest;

    /// <inheritdoc />
    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        Debug.Assert(DebugHandleEvent(@event, entry));
        switch (@event)
        {
            case PointerDownEvent downEvent:
                OnPointerDown?.Invoke(downEvent);
                break;
            case PointerMoveEvent moveEvent:
                OnPointerMove?.Invoke(moveEvent);
                break;
            case PointerUpEvent upEvent:
                OnPointerUp?.Invoke(upEvent);
                break;
            case PointerHoverEvent hoverEvent:
                OnPointerHover?.Invoke(hoverEvent);
                break;
            case PointerCancelEvent cancelEvent:
                OnPointerCancel?.Invoke(cancelEvent);
                break;
            case PointerPanZoomStartEvent panZoomStartEvent:
                OnPointerPanZoomStart?.Invoke(panZoomStartEvent);
                break;
            case PointerPanZoomUpdateEvent panZoomUpdateEvent:
                OnPointerPanZoomUpdate?.Invoke(panZoomUpdateEvent);
                break;
            case PointerPanZoomEndEvent panZoomEndEvent:
                OnPointerPanZoomEnd?.Invoke(panZoomEndEvent);
                break;
            case PointerSignalEvent signalEvent:
                OnPointerSignal?.Invoke(signalEvent);
                break;
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new FlagsSummary<Delegate>(
            "listeners",
            [
                new KeyValuePair<string, Delegate?>("down", OnPointerDown),
                new KeyValuePair<string, Delegate?>("move", OnPointerMove),
                new KeyValuePair<string, Delegate?>("up", OnPointerUp),
                new KeyValuePair<string, Delegate?>("hover", OnPointerHover),
                new KeyValuePair<string, Delegate?>("cancel", OnPointerCancel),
                new KeyValuePair<string, Delegate?>("panZoomStart", OnPointerPanZoomStart),
                new KeyValuePair<string, Delegate?>("panZoomUpdate", OnPointerPanZoomUpdate),
                new KeyValuePair<string, Delegate?>("panZoomEnd", OnPointerPanZoomEnd),
                new KeyValuePair<string, Delegate?>("signal", OnPointerSignal),
            ],
            ifEmpty: "<none>"));
    }
}

/// <summary>
/// Calls callbacks in response to pointer events that are exclusive to mice. Dart's
/// `RenderMouseRegion`.
/// </summary>
/// <remarks>
/// Enter and exit are produced by <see cref="MouseTracker"/> from the per-device annotation diff, not
/// by ordinary event dispatch, so they also fire when the region itself appears, disappears or moves
/// under a still pointer.
/// </remarks>
public class RenderMouseRegion : RenderProxyBoxWithHitTestBehavior, IMouseTrackerAnnotation
{
    private bool _opaque;
    private MouseCursor _cursor;
    private bool _validForMouseTracker;

    /// <summary>Creates a render object that forwards pointer events to callbacks.</summary>
    /// <remarks>
    /// Dart's <c>cursor</c> defaults to <c>MouseCursor.defer</c>; <see cref="MouseCursor.Defer"/> is not
    /// a compile-time constant, so a null argument stands for it.
    /// </remarks>
    public RenderMouseRegion(
        PointerEnterEventListener? onEnter = null,
        PointerHoverEventListener? onHover = null,
        PointerExitEventListener? onExit = null,
        MouseCursor? cursor = null,
        bool validForMouseTracker = true,
        bool opaque = true,
        RenderBox? child = null,
        Plumix.Rendering.HitTestBehavior? hitTestBehavior = Plumix.Rendering.HitTestBehavior.Opaque)
        : base(hitTestBehavior ?? Plumix.Rendering.HitTestBehavior.Opaque, child)
    {
        OnEnter = onEnter;
        OnHover = onHover;
        OnExit = onExit;
        _cursor = cursor ?? MouseCursor.Defer;
        _validForMouseTracker = validForMouseTracker;
        _opaque = opaque;
    }

    /// <inheritdoc />
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        return base.HitTest(result, position) && _opaque;
    }

    /// <inheritdoc />
    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        Debug.Assert(DebugHandleEvent(@event, entry));
        if (@event is PointerHoverEvent hoverEvent)
        {
            OnHover?.Invoke(hoverEvent);
        }
    }

    /// <summary>Whether this object should prevent <see cref="RenderMouseRegion"/>s visually behind it
    /// from detecting the pointer, thus affecting how their OnHover, OnEnter, and OnExit behave.</summary>
    public bool Opaque
    {
        get => _opaque;
        set
        {
            if (_opaque != value)
            {
                _opaque = value;
                // Trigger [MouseTracker]'s device update to recalculate mouse states.
                MarkNeedsPaint();
            }
        }
    }

    /// <summary>How to behave during hit testing.</summary>
    public Plumix.Rendering.HitTestBehavior? HitTestBehavior
    {
        get => Behavior;
        set
        {
            Plumix.Rendering.HitTestBehavior newValue = value ?? Plumix.Rendering.HitTestBehavior.Opaque;
            if (Behavior != newValue)
            {
                Behavior = newValue;
                // Trigger [MouseTracker]'s device update to recalculate mouse states.
                MarkNeedsPaint();
            }
        }
    }

    /// <inheritdoc />
    public PointerEnterEventListener? OnEnter { get; set; }

    /// <summary>Triggered when a pointer has moved onto or within the region without buttons pressed.
    /// </summary>
    public PointerHoverEventListener? OnHover { get; set; }

    /// <inheritdoc />
    public PointerExitEventListener? OnExit { get; set; }

    /// <inheritdoc />
    public MouseCursor Cursor
    {
        get => _cursor;
        set
        {
            if (!_cursor.Equals(value))
            {
                _cursor = value;
                // A repaint is needed in order to trigger a device update of [MouseTracker] so that
                // this new value can be found.
                MarkNeedsPaint();
            }
        }
    }

    /// <inheritdoc />
    public bool ValidForMouseTracker => _validForMouseTracker;

    /// <inheritdoc />
    /// <remarks>Dart's <c>attach</c>: <c>super.attach(owner); _validForMouseTracker = true;</c>.</remarks>
    protected override void OnAttach()
    {
        base.OnAttach();
        _validForMouseTracker = true;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dart's <c>detach</c>: it is possible that the mouse tracker is still dispatching an update to
    /// this object when it is detached, so the flag is cleared first.
    /// </remarks>
    protected override void OnDetach()
    {
        _validForMouseTracker = false;
        base.OnDetach();
    }

    /// <inheritdoc />
    public override Size ComputeSizeForNoChild(BoxConstraints constraints) => constraints.Biggest;

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new FlagsSummary<Delegate>(
            "listeners",
            [
                new KeyValuePair<string, Delegate?>("enter", OnEnter),
                new KeyValuePair<string, Delegate?>("hover", OnHover),
                new KeyValuePair<string, Delegate?>("exit", OnExit),
            ],
            ifEmpty: "<none>"));
        properties.Add(new DiagnosticsProperty<MouseCursor>("cursor", Cursor, defaultValue: MouseCursor.Defer));
        properties.Add(new DiagnosticsProperty<bool>("opaque", Opaque, defaultValue: true));
        properties.Add(new FlagProperty(
            "validForMouseTracker",
            ValidForMouseTracker,
            defaultValue: true,
            ifFalse: "invalid for MouseTracker"));
    }
}
