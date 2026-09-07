using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/mouse_tracker.dart

namespace Plumix.Rendering;

/// <summary>
/// Signature for hit testing at <paramref name="position"/> in the view identified by
/// <paramref name="viewId"/>. Dart's `MouseTrackerHitTest`.
/// </summary>
public delegate HitTestResult MouseTrackerHitTest(Point position, int viewId);

/// <summary>
/// Tracks the relationship between mouse devices and annotated regions, and dispatches mouse
/// enter/exit events and mouse cursor changes. Dart's `MouseTracker`.
/// </summary>
/// <remarks>
/// State is updated from two places: <see cref="UpdateWithEvent"/> during pointer dispatch, and
/// <see cref="UpdateAllDevices"/> once per frame, so a region that appears, disappears or moves
/// under a motionless pointer still produces the right enter and exit events.
/// </remarks>
public sealed class MouseTracker : ChangeNotifier
{
    private readonly MouseTrackerHitTest _hitTestInView;
    private readonly MouseCursorManager _mouseCursorMixin = new(SystemMouseCursors.Basic);
    private readonly Dictionary<int, MouseState> _mouseStates = [];
    private bool _debugDuringDeviceUpdate;

    /// <summary>Creates a tracker that hit-tests through <paramref name="hitTestInView"/>.</summary>
    public MouseTracker(MouseTrackerHitTest hitTestInView)
    {
        ArgumentNullException.ThrowIfNull(hitTestInView);
        _hitTestInView = hitTestInView;
    }

    /// <summary>
    /// Whether or not at least one mouse is connected and has produced events. Dart's
    /// `MouseTracker.mouseIsConnected`; listeners of this <see cref="ChangeNotifier"/> are notified
    /// when it changes.
    /// </summary>
    public bool MouseIsConnected => _mouseStates.Count > 0;

    /// <summary>
    /// The cursor <paramref name="device"/> is currently displaying, or null in release mode.
    /// Dart's `MouseTracker.debugDeviceActiveCursor`.
    /// </summary>
    public MouseCursor? DebugDeviceActiveCursor(int device) => _mouseCursorMixin.DebugDeviceActiveCursor(device);

    /// <summary>
    /// Updates the state for a single device from <paramref name="event"/>. Dart's
    /// `MouseTracker.updateWithEvent`; <paramref name="hitTestResult"/> is the result the binding
    /// already computed, or null when the tracker must run its own hit test.
    /// </summary>
    public void UpdateWithEvent(PointerEvent @event, HitTestResult? hitTestResult)
    {
        ArgumentNullException.ThrowIfNull(@event);
        if (@event.Kind != PointerDeviceKind.Mouse && @event.Kind != PointerDeviceKind.Stylus)
        {
            return;
        }

        if (@event is PointerSignalEvent)
        {
            return;
        }

        HitTestResult result = @event is PointerRemovedEvent
            ? new HitTestResult()
            : hitTestResult ?? _hitTestInView(@event.Position, @event.ViewId);
        _mouseStates.TryGetValue(@event.Device, out MouseState? existingState);
        if (!ShouldMarkStateDirty(existingState, @event))
        {
            return;
        }

        MonitorMouseConnection(() => DeviceUpdatePhase(() =>
        {
            // Update mouseState to the latest devices that have not been removed, so that
            // updateDetails will See the up-to-date "before" states.
            if (existingState is null)
            {
                if (@event is PointerRemovedEvent)
                {
                    return;
                }

                _mouseStates[@event.Device] = new MouseState(@event);
            }
            else
            {
                if (@event is PointerRemovedEvent)
                {
                    _mouseStates.Remove(@event.Device);
                }
            }

            MouseState targetState = _mouseStates.TryGetValue(@event.Device, out MouseState? current)
                ? current
                : existingState!;

            PointerEvent lastEvent = targetState.ReplaceLatestEvent(@event);
            List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> nextAnnotations = @event is PointerRemovedEvent
                ? []
                : HitTestInViewResultToAnnotations(result);
            List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> lastAnnotations =
                targetState.ReplaceAnnotations(nextAnnotations);

            HandleDeviceUpdate(MouseTrackerUpdateDetails.ByPointerEvent(
                lastAnnotations: lastAnnotations,
                nextAnnotations: nextAnnotations,
                previousEvent: lastEvent,
                triggeringEvent: @event));
        }));
    }

    /// <summary>
    /// Rebuilds the annotations of every tracked device from a fresh hit test. Dart's
    /// `MouseTracker.updateAllDevices`, scheduled as a post-frame callback so a region that moved,
    /// appeared or disappeared during the frame produces its enter and exit events.
    /// </summary>
    public void UpdateAllDevices()
    {
        DeviceUpdatePhase(() =>
        {
            foreach (MouseState dirtyState in _mouseStates.Values.ToList())
            {
                PointerEvent lastEvent = dirtyState.LatestEvent;
                List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> nextAnnotations = FindAnnotations(dirtyState);
                List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> lastAnnotations =
                    dirtyState.ReplaceAnnotations(nextAnnotations);

                HandleDeviceUpdate(MouseTrackerUpdateDetails.ByNewFrame(
                    lastAnnotations: lastAnnotations,
                    nextAnnotations: nextAnnotations,
                    previousEvent: lastEvent));
            }
        });
    }

    /// <summary>
    /// Whether <paramref name="event"/> can change the state <paramref name="state"/> describes.
    /// Dart's static `MouseTracker._shouldMarkStateDirty`.
    /// </summary>
    private static bool ShouldMarkStateDirty(MouseState? state, PointerEvent @event)
    {
        if (state is null)
        {
            return true;
        }

        PointerEvent lastEvent = state.LatestEvent;
        if (@event is PointerSignalEvent)
        {
            return false;
        }

        return lastEvent is PointerAddedEvent
               || @event is PointerRemovedEvent
               || lastEvent.Position != @event.Position;
    }

    /// <summary>
    /// Collects the annotations of <paramref name="result"/> in hit-test order together with the
    /// transform each of them was hit through. Dart's `_hitTestInViewResultToAnnotations`.
    /// </summary>
    private static List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> HitTestInViewResultToAnnotations(
        HitTestResult result)
    {
        var annotations = new List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>>();
        foreach (HitTestEntry entry in result.Path)
        {
            if (entry.Target is not IMouseTrackerAnnotation annotation)
            {
                continue;
            }

            // Dart keeps a `LinkedHashMap` keyed by the annotation, so a target that appears twice
            // holds one entry carrying the last transform seen.
            int existing = annotations.FindIndex(pair => ReferenceEquals(pair.Key, annotation));
            var pair = new KeyValuePair<IMouseTrackerAnnotation, Matrix4>(
                annotation,
                entry.Transform ?? Matrix4.Identity());
            if (existing >= 0)
            {
                annotations[existing] = pair;
            }
            else
            {
                annotations.Add(pair);
            }
        }

        return annotations;
    }

    /// <summary>
    /// Hit-tests <paramref name="state"/>'s last known position again. Dart's
    /// `MouseTracker._findAnnotations`; a device that is no longer tracked has no annotations and is
    /// not hit-tested at all.
    /// </summary>
    private List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> FindAnnotations(MouseState state)
    {
        Point globalPosition = state.LatestEvent.Position;
        int device = state.Device;
        int viewId = state.LatestEvent.ViewId;
        return _mouseStates.ContainsKey(device)
            ? HitTestInViewResultToAnnotations(_hitTestInView(globalPosition, viewId))
            : [];
    }

    /// <summary>
    /// Runs <paramref name="task"/> and notifies listeners when it changed
    /// <see cref="MouseIsConnected"/>. Dart's `_monitorMouseConnection`.
    /// </summary>
    private void MonitorMouseConnection(Action task)
    {
        bool mouseWasConnected = MouseIsConnected;
        task();
        if (mouseWasConnected != MouseIsConnected)
        {
            NotifyListeners();
        }
    }

    /// <summary>
    /// Runs <paramref name="task"/> inside a device update, rejecting a re-entrant one. Dart's
    /// `_deviceUpdatePhase`, whose guard is assert-only.
    /// </summary>
    private void DeviceUpdatePhase(Action task)
    {
        if (Constants.KDebugMode && _debugDuringDeviceUpdate)
        {
            throw new InvalidOperationException(
                "A mouse device update triggered another one; enter and exit callbacks must not "
                + "synchronously dispatch pointer events.");
        }

        _debugDuringDeviceUpdate = true;
        try
        {
            task();
        }
        finally
        {
            _debugDuringDeviceUpdate = false;
        }
    }

    /// <summary>
    /// Dispatches the enter and exit events for one device update and then updates its cursor.
    /// Dart's `_handleDeviceUpdate`.
    /// </summary>
    private void HandleDeviceUpdate(MouseTrackerUpdateDetails details)
    {
        HandleDeviceUpdateMouseEvents(details);
        _mouseCursorMixin.HandleDeviceCursorUpdate(
            details.Device,
            details.TriggeringEvent,
            details.NextAnnotations.Select(pair => pair.Key.Cursor));
    }

    /// <summary>
    /// Dispatches the enter and exit events of one device update. Dart's static
    /// `_handleDeviceUpdateMouseEvents`: exits run in hit-test order (child before parent) and
    /// enters in reverse hit-test order (parent before child), and every exit precedes every enter.
    /// </summary>
    private static void HandleDeviceUpdateMouseEvents(MouseTrackerUpdateDetails details)
    {
        PointerEvent latestEvent = details.LatestEvent;
        List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> lastAnnotations = details.LastAnnotations;
        List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> nextAnnotations = details.NextAnnotations;

        // Send exit events to annotations that are in last but not in next, in hit-test order.
        PointerExitEvent baseExitEvent = PointerExitEvent.FromMouseEvent(latestEvent);
        foreach (KeyValuePair<IMouseTrackerAnnotation, Matrix4> pair in lastAnnotations)
        {
            if (pair.Key.ValidForMouseTracker && !ContainsAnnotation(nextAnnotations, pair.Key))
            {
                pair.Key.OnExit?.Invoke((PointerExitEvent)baseExitEvent.Transformed(pair.Value));
            }
        }

        // Send enter events to annotations that are not in last but in next, in reverse hit-test
        // order.
        var enteringAnnotations = nextAnnotations
            .Where(pair => !ContainsAnnotation(lastAnnotations, pair.Key))
            .ToList();
        PointerEnterEvent baseEnterEvent = PointerEnterEvent.FromMouseEvent(latestEvent);
        for (int i = enteringAnnotations.Count - 1; i >= 0; i--)
        {
            KeyValuePair<IMouseTrackerAnnotation, Matrix4> pair = enteringAnnotations[i];
            if (pair.Key.ValidForMouseTracker)
            {
                pair.Key.OnEnter?.Invoke((PointerEnterEvent)baseEnterEvent.Transformed(pair.Value));
            }
        }
    }

    private static bool ContainsAnnotation(
        List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> annotations,
        IMouseTrackerAnnotation annotation)
    {
        foreach (KeyValuePair<IMouseTrackerAnnotation, Matrix4> pair in annotations)
        {
            if (ReferenceEquals(pair.Key, annotation))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The state of one mouse device. Dart's private `_MouseState`.</summary>
    private sealed class MouseState
    {
        private List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> _annotations = [];
        private PointerEvent _latestEvent;

        public MouseState(PointerEvent initialEvent)
        {
            _latestEvent = initialEvent;
        }

        public PointerEvent LatestEvent => _latestEvent;

        public int Device => _latestEvent.Device;

        public List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> ReplaceAnnotations(
            List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> value)
        {
            List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> previous = _annotations;
            _annotations = value;
            return previous;
        }

        public PointerEvent ReplaceLatestEvent(PointerEvent value)
        {
            PointerEvent previous = _latestEvent;
            _latestEvent = value;
            return previous;
        }

        public override string ToString()
        {
            string describeEvent = Diagnostics.DescribeIdentity(_latestEvent);
            return $"{Diagnostics.DescribeIdentity(this)}(latestEvent: {describeEvent}, "
                   + $"annotations: [list of {_annotations.Count}])";
        }
    }

    /// <summary>Used to describe a device update. Dart's private `_MouseTrackerUpdateDetails`.</summary>
    private sealed class MouseTrackerUpdateDetails
    {
        private MouseTrackerUpdateDetails(
            List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> lastAnnotations,
            List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> nextAnnotations,
            PointerEvent? previousEvent,
            PointerEvent? triggeringEvent)
        {
            LastAnnotations = lastAnnotations;
            NextAnnotations = nextAnnotations;
            PreviousEvent = previousEvent;
            TriggeringEvent = triggeringEvent;
        }

        public List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> LastAnnotations { get; }

        public List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> NextAnnotations { get; }

        public PointerEvent? PreviousEvent { get; }

        public PointerEvent? TriggeringEvent { get; }

        public int Device => (PreviousEvent ?? TriggeringEvent)!.Device;

        public PointerEvent LatestEvent => TriggeringEvent ?? PreviousEvent!;

        /// <summary>Dart's `_MouseTrackerUpdateDetails.byNewFrame`.</summary>
        public static MouseTrackerUpdateDetails ByNewFrame(
            List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> lastAnnotations,
            List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> nextAnnotations,
            PointerEvent previousEvent)
            => new(lastAnnotations, nextAnnotations, previousEvent, triggeringEvent: null);

        /// <summary>Dart's `_MouseTrackerUpdateDetails.byPointerEvent`.</summary>
        public static MouseTrackerUpdateDetails ByPointerEvent(
            List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> lastAnnotations,
            List<KeyValuePair<IMouseTrackerAnnotation, Matrix4>> nextAnnotations,
            PointerEvent? previousEvent,
            PointerEvent triggeringEvent)
            => new(lastAnnotations, nextAnnotations, previousEvent, triggeringEvent);
    }
}
