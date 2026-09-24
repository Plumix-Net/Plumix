using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/binding.dart
// The host supplies logical pointer events directly; engine packet conversion remains platform glue.

namespace Plumix.Gestures;

/// <summary>Clock used to place touch samples between input and presentation frames.</summary>
public class SamplingClock
{
    public virtual DateTime Now() => DateTime.UtcNow;

    public virtual Stopwatch Stopwatch() => new();
}

public sealed class GestureBinding : IHitTestTarget
{
    private static readonly TimeSpan SamplingInterval = TimeSpan.FromTicks(166670);
    private static readonly TimeSpan DefaultSamplingOffset = TimeSpan.FromMilliseconds(-38);

    internal static event Action<PointerEvent>? PointerEventReceived;

    public static GestureBinding Instance { get; } = new();

    private readonly Dictionary<int, HitTestResult> _hitTests = [];
    private readonly Dictionary<int, Point> _lastPositions = [];
    private readonly Dictionary<int, RenderView> _pointerRoots = [];
    private readonly LinkedList<(RenderView Root, PointerEvent Event)> _pendingPointerEvents = [];
    private readonly Dictionary<int, PointerEventResampler> _resamplers = [];
    private readonly Dictionary<int, RenderView> _resamplerRoots = [];
    private Timer? _resamplingTimer;
    private DateTime _frameTime;
    private Stopwatch? _frameTimeAge;
    private bool _frameCallbackScheduled;
    private int _resamplingGeneration;
    private bool _samplingResampledEvents;
    private bool _flushingPointerEvents;

    private GestureBinding()
    {
        PlatformDispatcher.Instance.OnHitTest = HandleHitTest;
    }

    /// <summary>Compatibility forward to Dart's <c>RendererBinding.initMouseTracker</c>.</summary>
    public void InitMouseTracker(MouseTracker? tracker = null)
    {
        RendererBinding.Instance.InitMouseTracker(tracker);
    }

    public PointerRouter PointerRouter { get; } = new();

    public GestureArenaManager GestureArena { get; } = new();

    public PointerSignalResolver PointerSignalResolver { get; } = new();

    public MouseTracker MouseTracker => RendererBinding.Instance.MouseTracker;

    public bool ResamplingEnabled { get; set; }

    public TimeSpan SamplingOffset { get; set; } = DefaultSamplingOffset;

    public SamplingClock SamplingClock { get; set; } = new();

    /// <summary>
    /// Receives a logical pointer event from the host. A reentrant event waits in the same queue
    /// as Dart's engine packet events, so a cancellation requested during a down event goes first.
    /// </summary>
    public void HandlePointerEvent(RenderView root, PointerEvent @event)
    {
        using Scheduler.FrameworkThreadScope scope = Scheduler.EnterFrameworkThread();
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(@event);
        if (ResamplingEnabled)
        {
            if (@event.Kind == PointerDeviceKind.Touch)
            {
                if (!_resamplers.TryGetValue(@event.Device, out PointerEventResampler? resampler))
                {
                    resampler = new PointerEventResampler();
                    _resamplers[@event.Device] = resampler;
                }

                _resamplerRoots[@event.Device] = root;
                resampler.AddEvent(@event);
            }
            else
            {
                _pendingPointerEvents.AddLast((root, @event));
                FlushPointerEventQueue();
            }

            SampleResampledEvents();
            return;
        }

        StopResampling();
        _pendingPointerEvents.AddLast((root, @event));
        FlushPointerEventQueue();
    }

    private void SampleResampledEvents()
    {
        if (_samplingResampledEvents)
        {
            return;
        }

        _samplingResampledEvents = true;
        try
        {
            SampleResampledEventsCore();
        }
        finally
        {
            _samplingResampledEvents = false;
        }
    }

    private void SampleResampledEventsCore()
    {
        if (_resamplers.Count == 0)
        {
            StopResamplingTimer();
            _frameTime = default;
            _frameTimeAge = null;
            _frameCallbackScheduled = false;
            _resamplingGeneration++;
            return;
        }

        if (_frameTime == default)
        {
            _frameTime = SamplingClock.Now();
            _frameTimeAge = SamplingClock.Stopwatch();
            _frameTimeAge.Start();
        }

        _resamplingTimer ??= new Timer(
            _ => Scheduler.ScheduleMicrotask(HandleSampleTimeChanged),
            null,
            SamplingInterval,
            SamplingInterval);

        long intervals = _frameTimeAge!.Elapsed.Ticks / SamplingInterval.Ticks;
        DateTime sampleTime = _frameTime
            + TimeSpan.FromTicks(intervals * SamplingInterval.Ticks)
            + SamplingOffset;
        DateTime nextSampleTime = sampleTime + SamplingInterval;
        foreach ((int device, PointerEventResampler resampler) in _resamplers.ToArray())
        {
            RenderView root = _resamplerRoots[device];
            resampler.Sample(sampleTime, nextSampleTime, @event => QueueResampledEvent(root, @event));
            if (!resampler.HasPendingEvents && !resampler.IsDown)
            {
                _resamplers.Remove(device);
                _resamplerRoots.Remove(device);
            }
        }

        if (_resamplers.Count == 0)
        {
            StopResamplingTimer();
            _frameTime = default;
            _frameTimeAge = null;
            _frameCallbackScheduled = false;
            _resamplingGeneration++;
        }
        else if (!_frameCallbackScheduled)
        {
            _frameCallbackScheduled = true;
            int generation = _resamplingGeneration;
            Scheduler.AddPostFrameCallback(_ =>
            {
                if (generation != _resamplingGeneration)
                {
                    return;
                }

                _frameCallbackScheduled = false;
                _frameTime = SamplingClock.Now();
                _frameTimeAge?.Restart();
                if (ResamplingEnabled)
                {
                    SampleResampledEvents();
                }
            }, debugLabel: "Resampler.startTimer");
        }
    }

    private void HandleSampleTimeChanged()
    {
        using Scheduler.FrameworkThreadScope scope = Scheduler.EnterFrameworkThread();
        if (ResamplingEnabled)
        {
            SampleResampledEvents();
        }
        else
        {
            StopResampling();
        }
    }

    private void QueueResampledEvent(RenderView root, PointerEvent @event)
    {
        _pendingPointerEvents.AddLast((root, @event));
        FlushPointerEventQueue();
    }

    private void StopResampling()
    {
        if (_resamplers.Count == 0 && _resamplingTimer is null)
        {
            return;
        }

        _resamplingGeneration++;
        _frameCallbackScheduled = false;
        (RenderView Root, PointerEventResampler Resampler)[] pending = _resamplers
            .Select(pair => (_resamplerRoots[pair.Key], pair.Value))
            .ToArray();
        _resamplers.Clear();
        _resamplerRoots.Clear();
        foreach ((RenderView root, PointerEventResampler resampler) in pending)
        {
            resampler.Stop(@event => QueueResampledEvent(root, @event));
        }

        _frameTime = default;
        _frameTimeAge = null;
        StopResamplingTimer();
    }

    private void StopResamplingTimer()
    {
        _resamplingTimer?.Dispose();
        _resamplingTimer = null;
    }

    /// <summary>
    /// Schedules Dart's <c>cancelPointer</c> before the next queued event. When no event is being
    /// dispatched, the cancellation is delivered in a microtask.
    /// </summary>
    public void CancelPointer(int pointer)
    {
        if (!_pointerRoots.TryGetValue(pointer, out RenderView? root))
        {
            return;
        }

        var cancel = new PointerCancelEvent(
            pointer: pointer,
            timestampUtc: DateTime.UnixEpoch,
            viewId: root.FlutterView.ViewId);
        bool wasEmpty = _pendingPointerEvents.Count == 0;
        _pendingPointerEvents.AddFirst((root, cancel));
        if (wasEmpty && !_flushingPointerEvents)
        {
            Scheduler.ScheduleMicrotask(FlushPointerEventQueue);
        }
    }

    private void FlushPointerEventQueue()
    {
        if (_flushingPointerEvents)
        {
            return;
        }

        _flushingPointerEvents = true;
        try
        {
            while (_pendingPointerEvents.First is { } first)
            {
                _pendingPointerEvents.RemoveFirst();
                HandlePointerEventImmediately(first.Value.Root, first.Value.Event);
            }
        }
        finally
        {
            _flushingPointerEvents = false;
        }
    }

    private void HandlePointerEventImmediately(RenderView root, PointerEvent @event)
    {
        PointerEventReceived?.Invoke(@event);
        PointerEvent eventWithDelta = AttachDelta(@event);
        HitTestResult? result = null;

        if (@event is PointerDownEvent or PointerSignalEvent or PointerHoverEvent or PointerPanZoomStartEvent)
        {
            result = HitTestInView(root, @event.Position);
            if (@event is PointerDownEvent or PointerPanZoomStartEvent)
            {
                if (_hitTests.ContainsKey(@event.Pointer))
                {
                    throw new AssertionError("A pointer down unexpectedly already has a hit test result.");
                }

                _hitTests[@event.Pointer] = result;
                _pointerRoots[@event.Pointer] = root;
            }
        }
        else if (@event is PointerUpEvent or PointerCancelEvent or PointerPanZoomEndEvent)
        {
            _hitTests.Remove(@event.Pointer, out result);
            _pointerRoots.Remove(@event.Pointer);
            _lastPositions.Remove(@event.Pointer);
        }
        else if (@event.Down || @event is PointerMoveEvent or PointerPanZoomUpdateEvent)
        {
            _hitTests.TryGetValue(@event.Pointer, out result);
        }

        if (result is not null || @event is PointerAddedEvent or PointerRemovedEvent)
        {
            DispatchEvent(eventWithDelta, result);
        }

        // A default arena win is a microtask in Dart, after every target and router saw the event.
        GestureArena.FlushDefaultResolutions();
    }

    private HitTestResult HitTestInView(RenderView root, Point position)
    {
        var result = new BoxHitTestResult();
        root.HitTest(result, position);
        result.Add(new HitTestEntry(this));
        return result;
    }

    /// <summary>Delivers events to hit-test targets, reporting one target's failure and continuing.</summary>
    public void DispatchEvent(PointerEvent @event, HitTestResult? hitTestResult)
    {
        MouseTracker.UpdateWithEvent(@event, @event is PointerMoveEvent ? null : hitTestResult);
        if (hitTestResult is null)
        {
            try
            {
                PointerRouter.Route(@event);
            }
            catch (Exception exception)
            {
                ReportDispatchError(exception, @event, null);
            }

            return;
        }

        foreach (HitTestEntry entry in hitTestResult.Path)
        {
            try
            {
                entry.Target.HandleEvent(@event.Transformed(entry.Transform), entry);
            }
            catch (Exception exception)
            {
                ReportDispatchError(exception, @event, entry);
            }
        }
    }

    /// <summary>Dart's final binding-level hit-test target.</summary>
    public void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        PointerRouter.Route(@event);
        if (@event is PointerDownEvent or PointerPanZoomStartEvent)
        {
            GestureArena.Close(@event.Pointer);
        }
        else if (@event is PointerUpEvent or PointerPanZoomEndEvent)
        {
            GestureArena.Sweep(@event.Pointer);
        }
        else if (@event is PointerSignalEvent signal)
        {
            PointerSignalResolver.Resolve(signal);
        }
    }

    private static void ReportDispatchError(Exception exception, PointerEvent @event, HitTestEntry? entry)
    {
        FlutterError.ReportError(new FlutterErrorDetailsForPointerEventDispatcher(
            exception,
            stack: exception.StackTrace,
            context: new ErrorDescription(
                entry is null
                    ? "while dispatching a non-hit-tested pointer event"
                    : "while dispatching a pointer event"),
            @event: @event,
            hitTestEntry: entry,
            informationCollector: () =>
            {
                List<DiagnosticsNode> properties =
                [
                    new DiagnosticsProperty<PointerEvent>(
                        "Event",
                        @event,
                        style: DiagnosticsTreeStyle.ErrorProperty),
                ];
                if (entry is not null)
                {
                    properties.Add(new DiagnosticsProperty<IHitTestTarget>(
                        "Target",
                        entry.Target,
                        style: DiagnosticsTreeStyle.ErrorProperty));
                }

                return properties;
            }));
    }

    public void ScheduleMouseTrackerUpdate()
    {
        RendererBinding.Instance.ScheduleMouseTrackerUpdate();
    }

    public HitTestResult HitTestInView(Point position, int viewId)
    {
        return RendererBinding.Instance.HitTestInView(position, viewId);
    }

    private HitTestResponse HandleHitTest(HitTestRequest request)
    {
        HitTestResult result = HitTestInView(request.Offset, request.ViewId);
        return new HitTestResponse(result.Path.Any(entry => entry.Target is INativeHitTestTarget));
    }

    internal void ResetForTests()
    {
        _resamplingGeneration++;
        StopResamplingTimer();
        _resamplers.Clear();
        _resamplerRoots.Clear();
        _frameTime = default;
        _frameTimeAge = null;
        _frameCallbackScheduled = false;
        _samplingResampledEvents = false;
        ResamplingEnabled = false;
        SamplingOffset = DefaultSamplingOffset;
        SamplingClock = new SamplingClock();
        _hitTests.Clear();
        _lastPositions.Clear();
        _pointerRoots.Clear();
        _pendingPointerEvents.Clear();
        _flushingPointerEvents = false;
        PointerRouter.Reset();
        GestureArena.Reset();
        RendererBinding.Instance.ResetMouseTrackerForTests();
    }

    /// <summary>
    /// Gives a host-reported move or hover event the delta Flutter's engine would have computed: the
    /// distance from the pointer's previous position. Every other event type has no settable delta
    /// in Dart, so it is dispatched as reported; a down and a hover still record the position the
    /// next move measures from.
    /// </summary>
    private PointerEvent AttachDelta(PointerEvent @event)
    {
        int pointer = @event.Pointer;
        if (@event.IsResampled)
        {
            _lastPositions[pointer] = @event.Position;
            return @event;
        }

        if (@event is not PointerMoveEvent and not PointerHoverEvent)
        {
            if (@event is PointerDownEvent)
            {
                _lastPositions[pointer] = @event.Position;
            }

            return @event;
        }

        Point delta = _lastPositions.TryGetValue(pointer, out Point previousPosition)
            ? @event.Position - previousPosition
            : default;
        _lastPositions[pointer] = @event.Position;
        return delta == @event.Delta ? @event : @event.WithDelta(delta);
    }
}

/// <summary>Dart's error details for one failing pointer event target.</summary>
public sealed class FlutterErrorDetailsForPointerEventDispatcher : FlutterErrorDetails
{
    public FlutterErrorDetailsForPointerEventDispatcher(
        object exception,
        string? stack = null,
        string? library = "gesture library",
        DiagnosticsNode? context = null,
        PointerEvent? @event = null,
        HitTestEntry? hitTestEntry = null,
        InformationCollector? informationCollector = null,
        bool silent = false)
        : base(
            exception,
            stack,
            library,
            context,
            informationCollector: informationCollector,
            silent: silent)
    {
        Event = @event;
        HitTestEntry = hitTestEntry;
    }

    public PointerEvent? Event { get; }

    public HitTestEntry? HitTestEntry { get; }
}
