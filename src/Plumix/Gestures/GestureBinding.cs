using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/binding.dart (approximate)

namespace Plumix.Gestures;

public sealed class GestureBinding
{
    internal static event Action<PointerEvent>? PointerEventReceived;

    public static GestureBinding Instance { get; } = new();

    private readonly Dictionary<int, HitTestResult> _hitTests = [];
    private readonly Dictionary<int, Point> _lastPositions = [];
    private RenderView? _hitTestRoot;
    private bool _mouseTrackerUpdateScheduled;

    private GestureBinding()
    {
        InitMouseTracker();
    }

    /// <summary>
    /// Creates the mouse tracker over this binding's hit-test entry point. Dart's
    /// `RendererBinding.initMouseTracker`; a test may pass its own tracker.
    /// </summary>
    public void InitMouseTracker(MouseTracker? tracker = null)
    {
        MouseTracker?.Dispose();
        MouseTracker = tracker ?? new MouseTracker(HitTestInView);
    }

    public PointerRouter PointerRouter { get; } = new();

    public GestureArenaManager GestureArena { get; } = new();

    /// <summary>
    /// Dart's `pointerSignalResolver`: the resolver used for determining which widget handles a
    /// pointer signal event.
    /// </summary>
    public PointerSignalResolver PointerSignalResolver { get; } = new();

    /// <summary>
    /// Tracks which annotated regions each mouse is over. Dart's `RendererBinding.mouseTracker`;
    /// Plumix has no renderer binding, so the single hit-testing entry point owns it.
    /// </summary>
    public MouseTracker MouseTracker { get; private set; } = null!;

    public void HandlePointerEvent(RenderView root, PointerEvent @event)
    {
        using Scheduler.FrameworkThreadScope scope = Scheduler.EnterFrameworkThread();
        ArgumentNullException.ThrowIfNull(root);
        // Dart resolves the view from `PointerEvent.viewId`; Plumix's hosts pass the root they own,
        // so the tracker's own hit tests (which run outside an event) reuse the last one seen.
        _hitTestRoot = root;
        PointerEventReceived?.Invoke(@event);
        var eventWithDelta = AttachDelta(@event);
        HitTestResult? hitTestResult = null;

        switch (@event)
        {
            case PointerDownEvent or PointerPanZoomStartEvent:
            {
                var result = new BoxHitTestResult();
                root.HitTest(result, @event.Position);
                _hitTests[@event.Pointer] = result;
                hitTestResult = result;
                break;
            }
            // A pan/zoom update carries `Down == false`, so Dart gives it its own arm alongside the
            // moves; it reuses the path cached when the gesture started.
            case PointerMoveEvent or PointerUpEvent or PointerCancelEvent
                or PointerPanZoomUpdateEvent or PointerPanZoomEndEvent:
            {
                _hitTests.TryGetValue(@event.Pointer, out hitTestResult);
                break;
            }
            case PointerHoverEvent:
            {
                var result = new BoxHitTestResult();
                root.HitTest(result, @event.Position);
                hitTestResult = result;
                break;
            }
            case PointerSignalEvent:
            {
                var result = new BoxHitTestResult();
                root.HitTest(result, @event.Position);
                hitTestResult = result;
                break;
            }
        }

        DispatchEvent(eventWithDelta, hitTestResult);

        if (eventWithDelta is PointerSignalEvent signalEvent)
        {
            // Dart's GestureBinding.handleEvent: signals resolve to their first registered
            // handler once the framework has finished dispatching the event.
            PointerSignalResolver.Resolve(signalEvent);
        }

        if (@event is PointerDownEvent or PointerPanZoomStartEvent)
        {
            GestureArena.Close(@event.Pointer);
        }

        if (@event is PointerUpEvent or PointerCancelEvent or PointerPanZoomEndEvent)
        {
            GestureArena.Sweep(@event.Pointer);
            _hitTests.Remove(@event.Pointer);
            _lastPositions.Remove(@event.Pointer);
        }

        // Dart's `_resolveByDefault` runs in a microtask, i.e. after the whole event has been
        // dispatched; draining here reproduces that ordering.
        GestureArena.FlushDefaultResolutions();
    }

    public void DispatchEvent(PointerEvent @event, HitTestResult? hitTestResult)
    {
        // Dart's `RendererBinding.dispatchEvent` updates the tracker before the path dispatch, so a
        // nested region's enter (back to front) precedes its hover (front to back). A move reuses
        // the cached down-path, which is not a valid hover hit test, so the tracker re-runs its own.
        MouseTracker.UpdateWithEvent(@event, @event is PointerMoveEvent ? null : hitTestResult);
        if (hitTestResult != null)
        {
            foreach (var entry in hitTestResult.Path)
            {
                entry.Target.HandleEvent(@event.Transformed(entry.Transform), entry);
            }
        }

        PointerRouter.Route(@event);
    }

    /// <summary>
    /// Schedules the once-per-frame device update. Dart's
    /// `RendererBinding._scheduleMouseTrackerUpdate`, called after every produced frame so a region
    /// that moved, appeared or disappeared during it still produces its enter and exit events.
    /// </summary>
    public void ScheduleMouseTrackerUpdate()
    {
        // Dart asserts that no update is pending, because one `RendererBinding` produces one frame.
        // Plumix's hosts share this binding, so several of them may report the same frame; the first
        // one schedules the single update and the rest are no-ops.
        if (_mouseTrackerUpdateScheduled)
        {
            return;
        }

        _mouseTrackerUpdateScheduled = true;
        Scheduler.AddPostFrameCallback(
            _ =>
            {
                _mouseTrackerUpdateScheduled = false;
                MouseTracker.UpdateAllDevices();
            });
    }

    /// <summary>
    /// Hit-tests at <paramref name="position"/> for the view the last pointer event came from.
    /// Dart's `RendererBinding.hitTestInView`.
    /// </summary>
    public HitTestResult HitTestInView(Point position, int viewId)
    {
        var result = new BoxHitTestResult();
        _hitTestRoot?.HitTest(result, position);
        return result;
    }

    internal void ResetForTests()
    {
        _hitTests.Clear();
        _lastPositions.Clear();
        _hitTestRoot = null;
        _mouseTrackerUpdateScheduled = false;
        PointerRouter.Reset();
        GestureArena.Reset();
        InitMouseTracker();
    }

    private PointerEvent AttachDelta(PointerEvent @event)
    {
        // Signals carry their own scroll delta, and a pan/zoom gesture reports movement through
        // `PanDelta`; Dart leaves `delta` at zero for both.
        // An added or removed pointer reports no movement either, and Dart's synthesized exit on
        // disconnect is asserted to carry a zero delta.
        if (@event is PointerSignalEvent or PointerAddedEvent or PointerRemovedEvent
            or PointerPanZoomStartEvent or PointerPanZoomUpdateEvent or PointerPanZoomEndEvent)
        {
            return @event.WithDelta(default);
        }

        int pointer = @event.Pointer;
        if (!_lastPositions.TryGetValue(pointer, out var previousPosition))
        {
            _lastPositions[pointer] = @event.Position;
            return @event.WithDelta(default);
        }

        var delta = @event.Position - previousPosition;
        _lastPositions[pointer] = @event.Position;
        return @event.WithDelta(delta);
    }
}
