using System.Globalization;
using System.Text;
using Plumix.Developer;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Flow = Plumix.Developer.Flow;

// Dart parity source: flutter/packages/flutter/lib/src/scheduler/binding.dart

namespace Plumix;

/// <summary>
/// The library-level members of Dart's <c>scheduler/binding.dart</c>: <see cref="TimeDilation"/> and
/// <see cref="DefaultSchedulingStrategy"/>.
/// </summary>
/// <remarks>
/// C# has no top-level members, so they live on this static class, the way <see cref="SchedulerDebug"/>
/// carries <c>scheduler/debug.dart</c>'s.
/// </remarks>
public static class Scheduler
{
    private static double _timeDilation = 1.0;

    /// <summary>
    /// Slows down animations by this factor to help in development.
    /// </summary>
    /// <remarks>
    /// Dart's <c>timeDilation</c>. Setting it resets the epoch of the existing binding, if any,
    /// before storing the new value, so the timestamp of the frame that follows keeps the value the
    /// previous frame reported and only the frames after that advance at the new rate.
    /// </remarks>
    public static double TimeDilation
    {
        get => _timeDilation;
        set
        {
            DebugAssertions.Assert(value > 0.0);
            if (_timeDilation == value)
            {
                return;
            }

            // If the binding has been created, we need to resetEpoch first so that we capture start of
            // the epoch with the current time dilation.
            SchedulerBinding.InstanceOrNull?.ResetEpoch();
            _timeDilation = value;
        }
    }

    /// <summary>
    /// The default <see cref="SchedulingStrategy"/> for <see cref="SchedulerBinding.SchedulingStrategy"/>.
    /// </summary>
    /// <remarks>
    /// Dart's <c>defaultSchedulingStrategy</c>: if there are any frame callbacks registered, only runs
    /// tasks with a <see cref="Priority"/> of <see cref="Priority.Animation"/> or higher; otherwise
    /// runs all tasks.
    /// </remarks>
    public static bool DefaultSchedulingStrategy(int priority, SchedulerBinding scheduler)
    {
        ArgumentNullException.ThrowIfNull(scheduler);

        if (scheduler.TransientCallbackCount > 0)
        {
            return priority >= Priority.Animation.Value;
        }

        return true;
    }

    /// <summary>Restores the default <see cref="TimeDilation"/> without touching any binding.</summary>
    internal static void ResetTimeDilationForTests() => _timeDilation = 1.0;
}

/// <summary>
/// Signature for the <see cref="SchedulerBinding.SchedulingStrategy"/> callback. Called whenever the
/// system needs to decide whether a task at a given priority needs to be run.
/// </summary>
/// <remarks>Dart's <c>SchedulingStrategy</c>.</remarks>
public delegate bool SchedulingStrategy(int priority, SchedulerBinding scheduler);

/// <summary>The various phases that a <see cref="SchedulerBinding"/> goes through during
/// <see cref="SchedulerBinding.HandleBeginFrame"/>.</summary>
/// <remarks>
/// The values are ordered in the same order as the phases occur, so their relative index values can
/// be compared to each other.
/// </remarks>
public enum SchedulerPhase
{
    /// <summary>No frame is being processed.</summary>
    /// <remarks>
    /// Tasks (scheduled by <see cref="SchedulerBinding.ScheduleTask{T}(Func{T}, Priority, string?, Flow?)"/>),
    /// microtasks (scheduled by <see cref="EventLoop.ScheduleMicrotask"/>), timer callbacks, event
    /// handlers and other callbacks run during this phase.
    /// </remarks>
    Idle,

    /// <summary>The transient callbacks (<see cref="SchedulerBinding.ScheduleFrameCallback"/>) are running.</summary>
    /// <remarks>Typically, these callbacks handle updating objects to new animation states.</remarks>
    TransientCallbacks,

    /// <summary>Microtasks scheduled during the processing of transient callbacks are running.</summary>
    /// <remarks>This is the phase between <see cref="SchedulerBinding.HandleBeginFrame"/> and
    /// <see cref="SchedulerBinding.HandleDrawFrame"/>.</remarks>
    MidFrameMicrotasks,

    /// <summary>
    /// The persistent callbacks (<see cref="SchedulerBinding.AddPersistentFrameCallback"/>) are running.
    /// </summary>
    /// <remarks>Typically, this is the build/layout/paint pipeline.</remarks>
    PersistentCallbacks,

    /// <summary>The post-frame callbacks (<see cref="SchedulerBinding.AddPostFrameCallback"/>) are running.</summary>
    /// <remarks>
    /// Typically, these callbacks handle cleanup and scheduling of work for the next frame.
    /// </remarks>
    PostFrameCallbacks,
}

/// <summary>
/// An opaque handle that keeps a request for <see cref="DartPerformanceMode"/> active until disposed.
/// </summary>
/// <remarks>
/// Dart's <c>PerformanceModeRequestHandle</c>. To create a request, call
/// <see cref="SchedulerBinding.RequestPerformanceMode"/>; to remove it, call <see cref="Dispose"/>.
/// </remarks>
public sealed class PerformanceModeRequestHandle : IDisposable
{
    private Action? _cleanup;

    internal PerformanceModeRequestHandle(Action cleanup)
    {
        _cleanup = cleanup;
        DebugAssertions.Assert(FoundationDebug.DebugMaybeDispatchCreated(
            "scheduler",
            "PerformanceModeRequestHandle",
            this));
    }

    /// <summary>Call this method to signal to <see cref="SchedulerBinding"/> that a request for a
    /// <see cref="DartPerformanceMode"/> is no longer needed.</summary>
    /// <remarks>
    /// Dart asserts the handle is disposed once and then null-checks the cleanup callback, so a second
    /// call fails in every build mode.
    /// </remarks>
    public void Dispose()
    {
        DebugAssertions.Assert(_cleanup is not null);
        DebugAssertions.Assert(FoundationDebug.DebugMaybeDispatchDisposed(this));
        Action cleanup = _cleanup ?? throw new InvalidOperationException(
            "A PerformanceModeRequestHandle was disposed twice.");
        cleanup();
        _cleanup = null;
    }
}

/// <summary>
/// Scheduler for running transient, persistent and post-frame callbacks and non-rendering tasks.
/// </summary>
/// <remarks>
/// <para>
/// Dart's <c>SchedulerBinding</c>: transient callbacks (<see cref="ScheduleFrameCallback"/>),
/// persistent callbacks (<see cref="AddPersistentFrameCallback"/>), post-frame callbacks
/// (<see cref="AddPostFrameCallback"/>) and non-rendering tasks (<see cref="ScheduleTask{T}(Func{T},
/// Priority, string?, Flow?)"/>), between frames in priority order.
/// </para>
/// <para>
/// Dart mixes the bindings into one <c>WidgetsFlutterBinding</c> on top of <c>BindingBase</c>. Plumix's
/// bindings are separate singletons, so this is a standalone class whose <see cref="Instance"/> is
/// created on first use (Dart's <c>initInstances</c> runs in the constructor), and the
/// <c>BindingBase</c> members the scheduler overrides or calls — <see cref="Locked"/>,
/// <see cref="LockEvents"/>, <see cref="Unlocked"/> — are declared here; see
/// <c>docs/ai/DIVERGENCES.md</c>. The microtask queue and the vsync source Dart gets from its isolate and
/// engine are <see cref="EventLoop"/> and <see cref="FrameClock"/>.
/// </para>
/// </remarks>
public sealed class SchedulerBinding
{
    private static SchedulerBinding? _instance;

    private readonly List<Action<IReadOnlyList<FrameTiming>>> _timingsCallbacks = [];
    private readonly PriorityQueue<TaskEntry, (int Priority, int Sequence)> _taskQueue = new();
    private readonly HashSet<int> _removedIds = [];
    private readonly List<Action<TimeSpan>> _persistentCallbacks = [];
    private readonly List<Action<TimeSpan>> _postFrameCallbacks = [];

    private AppLifecycleState? _lifecycleState;
    private int _lockCount;
    private bool _hasRequestedAnEventLoopCallback;
    private int _taskSequence;
    private int _nextFrameCallbackId;
    private Dictionary<int, FrameCallbackEntry> _transientCallbacks = [];
    private TaskCompletionSource? _nextFrameCompleter;
    private bool _hasScheduledFrame;
    private SchedulerPhase _schedulerPhase = SchedulerPhase.Idle;
    private bool _framesEnabled = true;
    private bool _warmUpFrame;
    private TimeSpan? _firstRawTimeStampInEpoch;
    private TimeSpan _epochStart = TimeSpan.Zero;
    private TimeSpan _lastRawTimeStamp = TimeSpan.Zero;
    private TimeSpan? _currentFrameTimeStamp;
    private int _debugFrameNumber;
    private string? _debugBanner;
    private bool _rescheduleAfterWarmUpFrame;
    private TimelineTask? _frameTimelineTask = Constants.KReleaseMode ? null : new TimelineTask();
    private DartPerformanceMode? _performanceMode;
    private int _numPerformanceModeRequests;
    private bool _handlingFrame;

    private SchedulerBinding()
    {
        InitInstances();
        InitServiceExtensions();
    }

    /// <summary>The current <see cref="SchedulerBinding"/>, created on first use.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.instance</c>, which <c>BindingBase.checkInstance</c> guards until the
    /// application binding is initialized. Plumix has no application binding to initialize, so the
    /// first read constructs the binding, the way <c>PaintingBinding.Instance</c> does.
    /// </remarks>
    public static SchedulerBinding Instance => _instance ?? new SchedulerBinding();

    /// <summary>The binding if one has been created; what Dart's private <c>_instance</c> reads.</summary>
    internal static SchedulerBinding? InstanceOrNull => _instance;

    /// <summary>The last <see cref="AppLifecycleState"/> the platform reported, if any.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.lifecycleState</c>.</remarks>
    public AppLifecycleState? LifecycleState => _lifecycleState;

    /// <summary>The strategy to use when deciding whether to run a task or not.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.schedulingStrategy</c>; defaults to
    /// <see cref="Scheduler.DefaultSchedulingStrategy"/>.</remarks>
    public SchedulingStrategy SchedulingStrategy { get; set; } = Scheduler.DefaultSchedulingStrategy;

    /// <summary>The current number of transient frame callbacks scheduled.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.transientCallbackCount</c>. This is reset to zero just before all the
    /// currently scheduled transient callbacks are called, at the start of a frame.
    /// </remarks>
    public int TransientCallbackCount => _transientCallbacks.Count;

    /// <summary>
    /// Returns a task that completes after the frame completes.
    /// </summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.endOfFrame</c>. If this is called between frames, a frame is
    /// immediately scheduled if necessary. If this is called during a frame, the task completes after
    /// the current frame.
    /// </remarks>
    public Task EndOfFrame
    {
        get
        {
            if (_nextFrameCompleter is null)
            {
                if (SchedulerPhase == SchedulerPhase.Idle)
                {
                    ScheduleFrame();
                }

                _nextFrameCompleter = new TaskCompletionSource();
                AddPostFrameCallback(
                    _ =>
                    {
                        // Dart completes the completer and then clears it, and its listeners run in
                        // a later microtask. A C# continuation can run inline in `SetResult`, so the
                        // field is cleared first: otherwise a continuation that reads `EndOfFrame`
                        // again would get this same, already completed task.
                        TaskCompletionSource completer = _nextFrameCompleter!;
                        _nextFrameCompleter = null;
                        completer.SetResult();
                    },
                    debugLabel: "SchedulerBinding.completeFrame");
            }

            return _nextFrameCompleter.Task;
        }
    }

    /// <summary>Whether this scheduler has requested that <see cref="HandleBeginFrame"/> be called soon.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.hasScheduledFrame</c>.</remarks>
    public bool HasScheduledFrame => _hasScheduledFrame;

    /// <summary>The phase that the scheduler is currently operating under.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.schedulerPhase</c>.</remarks>
    public SchedulerPhase SchedulerPhase => _schedulerPhase;

    /// <summary>Whether frames are currently being scheduled when <see cref="ScheduleFrame"/> is called.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.framesEnabled</c>, derived from the app lifecycle state through
    /// <see cref="HandleAppLifecycleStateChanged"/>.
    /// </remarks>
    public bool FramesEnabled => _framesEnabled;

    /// <summary>The time stamp for the frame currently being processed.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.currentFrameTimeStamp</c>: only valid between the start of
    /// <see cref="HandleBeginFrame"/> and the end of the corresponding <see cref="HandleDrawFrame"/>,
    /// adjusted for the epoch and for <see cref="Scheduler.TimeDilation"/>. Dart asserts and then
    /// null-checks; here the read throws <see cref="InvalidOperationException"/> outside a frame.
    /// </remarks>
    public TimeSpan CurrentFrameTimeStamp
    {
        get
        {
            DebugAssertions.Assert(_currentFrameTimeStamp is not null);
            return _currentFrameTimeStamp ?? throw new InvalidOperationException(
                "SchedulerBinding.CurrentFrameTimeStamp is only valid while a frame is being produced. "
                + "Outside a frame, read SchedulerBinding.CurrentSystemFrameTimeStamp instead.");
        }
    }

    /// <summary>The raw time stamp as provided by the engine to the frame callbacks.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.currentSystemFrameTimeStamp</c>: unadjusted for the epoch and
    /// undilated, and also valid outside a frame.
    /// </remarks>
    public TimeSpan CurrentSystemFrameTimeStamp => _lastRawTimeStamp;

    /// <summary>Whether events are currently being held back from the framework.</summary>
    /// <remarks>Dart's <c>BindingBase.locked</c>.</remarks>
    public bool Locked => _lockCount > 0;

    /// <summary>
    /// Whether a frame is actually being produced. <see cref="SchedulerPhase"/> alone cannot answer
    /// this in Plumix, because <see cref="BuildScope"/> reports the build phase for builds driven
    /// outside a frame.
    /// </summary>
    internal bool IsHandlingFrame => _handlingFrame;

    /// <summary>Adds a callback that reports the <see cref="FrameTiming"/> of recently rasterized frames.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.addTimingsCallback</c>. Adding the same callback twice makes it run
    /// twice; the platform's <c>onReportTimings</c> slot is only wired while a callback is registered.
    /// </remarks>
    public void AddTimingsCallback(Action<IReadOnlyList<FrameTiming>> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _timingsCallbacks.Add(callback);
        if (_timingsCallbacks.Count == 1)
        {
            DebugAssertions.Assert(PlatformDispatcher.Instance.OnReportTimings is null);
            PlatformDispatcher.Instance.OnReportTimings = ExecuteTimingsCallbacks;
        }

        DebugAssertions.Assert(
            PlatformDispatcher.Instance.OnReportTimings == ExecuteTimingsCallbacks);
    }

    /// <summary>Removes a callback that was earlier added by <see cref="AddTimingsCallback"/>.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.removeTimingsCallback</c>; only the first occurrence goes.</remarks>
    public void RemoveTimingsCallback(Action<IReadOnlyList<FrameTiming>> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        DebugAssertions.Assert(_timingsCallbacks.Contains(callback));
        _timingsCallbacks.Remove(callback);
        if (_timingsCallbacks.Count == 0)
        {
            PlatformDispatcher.Instance.OnReportTimings = null;
        }
    }

    /// <summary>Resets the lifecycle-derived state.</summary>
    /// <remarks>Dart's <c>@visibleForTesting SchedulerBinding.resetInternalState</c>.</remarks>
    public void ResetInternalState()
    {
        _lifecycleState = null;
        _framesEnabled = true;
    }

    /// <summary>Called when the application lifecycle state changes.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.handleAppLifecycleStateChanged</c>: frames are enabled while the app
    /// is <see cref="AppLifecycleState.Resumed"/> or <see cref="AppLifecycleState.Inactive"/>, and
    /// disabled otherwise. <c>WidgetsBinding.HandleAppLifecycleStateChanged</c> calls this for every
    /// state it generates, the way Dart's override calls <c>super</c>.
    /// </remarks>
    public void HandleAppLifecycleStateChanged(AppLifecycleState state)
    {
        if (LifecycleState == state)
        {
            return;
        }

        _lifecycleState = state;
        switch (state)
        {
            case AppLifecycleState.Resumed:
            case AppLifecycleState.Inactive:
                SetFramesEnabledState(true);
                break;
            case AppLifecycleState.Hidden:
            case AppLifecycleState.Paused:
            case AppLifecycleState.Detached:
                SetFramesEnabledState(false);
                break;
        }
    }

    /// <summary>
    /// Schedules the given task with the given priority. The returned task completes with the task's
    /// result.
    /// </summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.scheduleTask</c>. The <paramref name="debugLabel"/> and
    /// <paramref name="flow"/> are used to report the task to the <see cref="Timeline"/>.
    /// </remarks>
    public Task<T> ScheduleTask<T>(
        Func<T> task,
        Priority priority,
        string? debugLabel = null,
        Flow? flow = null)
    {
        ArgumentNullException.ThrowIfNull(task);

        return ScheduleTask(() => Task.FromResult(task()), priority, debugLabel, flow);
    }

    /// <summary>
    /// <see cref="ScheduleTask{T}(Func{T}, Priority, string?, Flow?)"/> for a task that itself
    /// completes asynchronously: the returned task completes only once the task's own task does, the
    /// way Dart's <c>Completer.complete(FutureOr&lt;T&gt;)</c> chains.
    /// </summary>
    public Task<T> ScheduleTask<T>(
        Func<Task<T>> task,
        Priority priority,
        string? debugLabel = null,
        Flow? flow = null)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(priority);

        bool isFirstTask = _taskQueue.Count == 0;
        var entry = new TaskEntry<T>(task, priority.Value, debugLabel, flow);
        // Dart's `HeapPriorityQueue` orders by descending priority and leaves ties unordered; the
        // insertion sequence makes ties run in the order they were scheduled.
        _taskQueue.Enqueue(entry, (-entry.Priority, _taskSequence++));
        if (isFirstTask && !Locked)
        {
            EnsureEventLoopCallback();
        }

        return entry.Completion;
    }

    /// <summary>Called when the last <see cref="LockEvents"/> lock is released.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.unlocked</c> over <c>BindingBase.unlocked</c>: it re-arms the task
    /// queue that <see cref="ScheduleTask{T}(Func{T}, Priority, string?, Flow?)"/> left unserviced
    /// while locked.
    /// </remarks>
    public void Unlocked()
    {
        DebugAssertions.Assert(!Locked);
        if (_taskQueue.Count > 0)
        {
            EnsureEventLoopCallback();
        }

        // Dart's `GestureBinding.unlocked` delivers the pointer events that arrived while locked.
        Gestures.GestureBinding.Instance.Unlocked();
    }

    /// <summary>Called by the system when there is time to run tasks.</summary>
    /// <remarks>
    /// Dart's <c>@visibleForTesting SchedulerBinding.handleEventLoopCallback</c>: runs the
    /// highest-priority queued task if the <see cref="SchedulingStrategy"/> admits it, and returns
    /// whether tasks remain queued (true also when the strategy turned the head task away).
    /// </remarks>
    public bool HandleEventLoopCallback()
    {
        if (_taskQueue.Count == 0 || Locked)
        {
            return false;
        }

        TaskEntry entry = _taskQueue.Peek();
        if (SchedulingStrategy(entry.Priority, this))
        {
            try
            {
                _taskQueue.Dequeue();
                entry.Run();
            }
            catch (Exception exception)
            {
                string? callbackStack = null;
                if (Constants.KDebugMode)
                {
                    callbackStack = entry.DebugStack;
                }

                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    stack: exception.StackTrace,
                    library: "scheduler library",
                    context: new ErrorDescription("during a task callback"),
                    informationCollector: callbackStack is null
                        ? null
                        : () => [DescribeCallbackStack(callbackStack)]));
            }

            return _taskQueue.Count > 0;
        }

        return true;
    }

    /// <summary>
    /// Schedules the given transient frame callback. Returns the id that
    /// <see cref="CancelFrameCallbackWithId"/> takes.
    /// </summary>
    /// <remarks>Dart's <c>SchedulerBinding.scheduleFrameCallback</c>.</remarks>
    /// <param name="callback">The callback, run once at the start of the next frame.</param>
    /// <param name="rescheduling">
    /// True when this call comes from inside the callback itself, so that the debug stack of the
    /// original registration is carried forward instead of being replaced.
    /// </param>
    /// <param name="scheduleNewFrame">
    /// Whether to request a frame. Pass false when the caller has already requested one (which is
    /// what <see cref="Ticker"/> does, because it chooses between a forced and a regular frame).
    /// </param>
    public int ScheduleFrameCallback(
        Action<TimeSpan> callback,
        bool rescheduling = false,
        bool scheduleNewFrame = true)
    {
        return ScheduleFrameCallback(callback, rescheduling, scheduleNewFrame, frameOnly: false);
    }

    /// <summary>
    /// Cancels the transient frame callback with the given <paramref name="id"/>.
    /// </summary>
    /// <remarks>Dart's <c>SchedulerBinding.cancelFrameCallbackWithId</c>.</remarks>
    public void CancelFrameCallbackWithId(int id)
    {
        DebugAssertions.Assert(id > 0);
        _transientCallbacks.Remove(id);
        _removedIds.Add(id);
    }

    /// <summary>
    /// Reports any transient frame callbacks still registered, without throwing.
    /// </summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.debugAssertNoTransientCallbacks</c>: it reports the leak through
    /// <see cref="FlutterError.ReportError"/>, with the reason string itself as the exception, and
    /// always returns true.
    /// </remarks>
    public bool DebugAssertNoTransientCallbacks(string reason)
    {
        if (!Constants.KDebugMode || TransientCallbackCount == 0)
        {
            return true;
        }

        // We cache the values so that we can produce them later even if the information collector
        // is called after the problem has been resolved.
        int count = TransientCallbackCount;
        Dictionary<int, FrameCallbackEntry> callbacks = new(_transientCallbacks);
        FlutterError.ReportError(new FlutterErrorDetails(
            exception: reason,
            library: "scheduler library",
            informationCollector: () =>
            {
                List<DiagnosticsNode> nodes =
                [
                    count == 1
                        // TODO(ianh): use `callbacks` to give the stack trace of the one callback.
                        ? new ErrorDescription(
                            "There was one transient callback left. The stack trace for when it was "
                            + "registered is as follows:")
                        : new ErrorDescription(
                            $"There were {count} transient callbacks left. The stack traces for when "
                            + "they were registered are as follows:"),
                ];
                foreach (int id in callbacks.Keys)
                {
                    nodes.Add(new DiagnosticsStackTrace(
                        $"── callback {id} ──",
                        callbacks[id].DebugStack,
                        showSeparator: false));
                }

                return nodes;
            }));

        return true;
    }

    /// <summary>Asserts that there are no pending performance mode requests in debug mode.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.debugAssertNoPendingPerformanceModeRequests</c>.</remarks>
    public bool DebugAssertNoPendingPerformanceModeRequests(string reason)
    {
        if (Constants.KDebugMode && _performanceMode is not null)
        {
            throw new FlutterError(reason);
        }

        return true;
    }

    /// <summary>Asserts that there is no artificial time dilation in debug mode.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.debugAssertNoTimeDilation</c>.</remarks>
    public bool DebugAssertNoTimeDilation(string reason)
    {
        if (Constants.KDebugMode && Scheduler.TimeDilation != 1.0)
        {
            throw new FlutterError(reason);
        }

        return true;
    }

    /// <summary>
    /// Prints the stack for where the current transient callback was registered.
    /// </summary>
    /// <remarks>Dart's static <c>SchedulerBinding.debugPrintTransientCallbackRegistrationStack</c>.</remarks>
    public static void DebugPrintTransientCallbackRegistrationStack()
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (FrameCallbackEntry.DebugCurrentCallbackStack is { } stack)
        {
            Print.DebugPrint("When the current transient callback was registered, this was the stack:");
            Print.DebugPrint(string.Join(
                '\n',
                FlutterError.DefaultStackFilter(
                    FlutterError.DemangleStackTrace(stack).TrimEnd().Split('\n'))));
        }
        else
        {
            Print.DebugPrint("No transient callback is currently executing.");
        }
    }

    /// <summary>Adds a persistent frame callback.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.addPersistentFrameCallback</c>: the callback runs during the
    /// persistent-callback phase of every frame, in registration order, and cannot be unregistered.
    /// Adding it does not request a frame, and adding it twice makes it run twice.
    /// </remarks>
    public void AddPersistentFrameCallback(Action<TimeSpan> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _persistentCallbacks.Add(callback);
    }

    /// <summary>Schedules a callback for the end of this frame.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.addPostFrameCallback</c>, which does not itself request a frame. The
    /// callbacks run in registration order, exactly once; with
    /// <see cref="SchedulerDebug.DebugTracePostFrameCallbacks"/> each one is a timeline block named
    /// <paramref name="debugLabel"/>.
    /// </remarks>
    public void AddPostFrameCallback(Action<TimeSpan> callback, string debugLabel = "callback")
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (Constants.KDebugMode && SchedulerDebug.DebugTracePostFrameCallbacks)
        {
            Action<TimeSpan> originalCallback = callback;
            callback = timeStamp =>
            {
                Timeline.StartSync(debugLabel);
                try
                {
                    originalCallback(timeStamp);
                }
                finally
                {
                    Timeline.FinishSync();
                }
            };
        }

        _postFrameCallbacks.Add(callback);
    }

    /// <summary>Ensures callbacks for the platform's frame signals are registered.</summary>
    /// <remarks>
    /// Dart's <c>@protected SchedulerBinding.ensureFrameCallbacksRegistered</c>: an already-installed
    /// handler is never replaced.
    /// </remarks>
    public void EnsureFrameCallbacksRegistered()
    {
        PlatformDispatcher.Instance.OnBeginFrame ??= HandleBeginFrameFromPlatform;
        PlatformDispatcher.Instance.OnDrawFrame ??= HandleDrawFrameFromPlatform;
    }

    /// <summary>Schedules a new frame using <see cref="ScheduleFrame"/> if this object is not
    /// currently producing a frame.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.ensureVisualUpdate</c>, which schedules in the idle and post-frame
    /// phases and returns in the others. Plumix's <see cref="SchedulerPhase"/> also reports
    /// <see cref="SchedulerPhase.PersistentCallbacks"/> for builds driven outside a frame
    /// (<see cref="BuildScope"/>), so the in-frame phases only suppress the request while
    /// <see cref="IsHandlingFrame"/>; see <c>docs/ai/DIVERGENCES.md</c>.
    /// </remarks>
    public void EnsureVisualUpdate()
    {
        switch (SchedulerPhase)
        {
            case SchedulerPhase.Idle:
            case SchedulerPhase.PostFrameCallbacks:
                ScheduleFrame();
                return;
            case SchedulerPhase.TransientCallbacks:
            case SchedulerPhase.MidFrameMicrotasks:
            case SchedulerPhase.PersistentCallbacks:
                if (!IsHandlingFrame)
                {
                    ScheduleFrame();
                }

                return;
        }
    }

    /// <summary>
    /// If necessary, schedules a new frame by calling <see cref="PlatformDispatcher.ScheduleFrame"/>.
    /// </summary>
    /// <remarks>Dart's <c>SchedulerBinding.scheduleFrame</c>: a no-op while frames are disabled.</remarks>
    public void ScheduleFrame()
    {
        if (_hasScheduledFrame || !FramesEnabled)
        {
            return;
        }

        if (Constants.KDebugMode && SchedulerDebug.DebugPrintScheduleFrameStacks)
        {
            Assertions.DebugPrintStack(
                label: $"scheduleFrame() called. Current phase is {DescribePhase(SchedulerPhase)}.");
        }

        EnsureFrameCallbacksRegistered();
        PlatformDispatcher.Instance.ScheduleFrame();
        _hasScheduledFrame = true;
    }

    /// <summary>Schedules a new frame by calling <see cref="PlatformDispatcher.ScheduleFrame"/>, even when
    /// <see cref="FramesEnabled"/> is false.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.scheduleForcedFrame</c>.</remarks>
    public void ScheduleForcedFrame()
    {
        if (_hasScheduledFrame)
        {
            return;
        }

        if (Constants.KDebugMode && SchedulerDebug.DebugPrintScheduleFrameStacks)
        {
            Assertions.DebugPrintStack(
                label: $"scheduleForcedFrame() called. Current phase is {DescribePhase(SchedulerPhase)}.");
        }

        EnsureFrameCallbacksRegistered();
        PlatformDispatcher.Instance.ScheduleFrame();
        _hasScheduledFrame = true;
    }

    /// <summary>Schedule a frame to run as soon as possible, rather than waiting for the engine to
    /// request a frame in response to a system "Vsync" signal.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.scheduleWarmUpFrame</c>.</remarks>
    public void ScheduleWarmUpFrame()
    {
        if (_warmUpFrame || SchedulerPhase != SchedulerPhase.Idle)
        {
            return;
        }

        _warmUpFrame = true;
        TimelineTask? debugTimelineTask = null;
        if (!Constants.KReleaseMode)
        {
            debugTimelineTask = new TimelineTask();
            debugTimelineTask.Start("Warm-up frame");
        }

        bool hadScheduledFrame = _hasScheduledFrame;
        PlatformDispatcher.Instance.ScheduleWarmUpFrame(
            beginFrame: () =>
            {
                DebugAssertions.Assert(_warmUpFrame);
                HandleBeginFrame(null);
            },
            drawFrame: () =>
            {
                DebugAssertions.Assert(_warmUpFrame);
                HandleDrawFrame();
                // We call resetEpoch after this frame so that, in the hot reload case, the very next
                // frame pretends to have occurred immediately after this warm-up frame. The warm-up
                // frame's timestamp will typically be way in the past (the time of the last real
                // frame), so if we didn't reset the epoch we would see a sudden jump from the old
                // time in the warm-up frame to the new time in the "real" frame. The biggest problem
                // with this is that implicit animations end up being triggered at the old time and
                // then skipping every frame and finishing in the new time.
                ResetEpoch();
                _warmUpFrame = false;
                if (hadScheduledFrame)
                {
                    ScheduleFrame();
                }
            });

        // Lock events so touch events etc don't insert themselves until the scheduled frame has
        // finished.
        LockEvents(() =>
        {
            // Dart's `await endOfFrame`, which resumes in a microtask once the frame that completed
            // the future has finished. A C# `await` on the framework thread would resume inline
            // inside the post-frame phase instead.
            var resumed = new TaskCompletionSource();
            EndOfFrame.ContinueWith(
                _ => EventLoop.ScheduleMicrotask(() =>
                {
                    if (!Constants.KReleaseMode)
                    {
                        debugTimelineTask!.Finish();
                    }

                    resumed.SetResult();
                }),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return resumed.Task;
        });
    }

    /// <summary>Prepares the scheduler for a non-monotonic change to how time stamps are calculated.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.resetEpoch</c>.</remarks>
    public void ResetEpoch()
    {
        _epochStart = AdjustForEpoch(_lastRawTimeStamp);
        _firstRawTimeStampInEpoch = null;
    }

    /// <summary>Called by the engine to prepare the framework to produce a new frame.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.handleBeginFrame</c>: runs the transient callbacks. A null
    /// <paramref name="rawTimeStamp"/> marks a warm-up frame, which reuses the previous raw time stamp.
    /// The C# body runs inside <see cref="EventLoop.EnterFrameworkThread"/>.
    /// </remarks>
    public void HandleBeginFrame(TimeSpan? rawTimeStamp)
    {
        using EventLoop.FrameworkThreadScope scope = EventLoop.EnterFrameworkThread();
        _frameTimelineTask?.Start("Frame");
        _firstRawTimeStampInEpoch ??= rawTimeStamp;
        _currentFrameTimeStamp = AdjustForEpoch(rawTimeStamp ?? _lastRawTimeStamp);
        if (rawTimeStamp is not null)
        {
            _lastRawTimeStamp = rawTimeStamp.Value;
        }

        if (Constants.KDebugMode)
        {
            _debugFrameNumber += 1;
            if (SchedulerDebug.DebugPrintBeginFrameBanner || SchedulerDebug.DebugPrintEndFrameBanner)
            {
                var frameTimeStampDescription = new StringBuilder();
                if (rawTimeStamp is not null)
                {
                    DebugDescribeTimeStamp(_currentFrameTimeStamp.Value, frameTimeStampDescription);
                }
                else
                {
                    frameTimeStampDescription.Append("(warm-up frame)");
                }

                string frameNumber = _debugFrameNumber.ToString(CultureInfo.InvariantCulture);
                _debugBanner = $"▄▄▄▄▄▄▄▄ Frame {frameNumber.PadRight(7)}"
                    + $"   {frameTimeStampDescription.ToString().PadLeft(18)} ▄▄▄▄▄▄▄▄";
                if (SchedulerDebug.DebugPrintBeginFrameBanner)
                {
                    Print.DebugPrint(_debugBanner);
                }
            }
        }

        DebugAssertions.Assert(_schedulerPhase == SchedulerPhase.Idle);
        _hasScheduledFrame = false;
        _handlingFrame = true;
        try
        {
            // TRANSIENT FRAME CALLBACKS
            _frameTimelineTask?.Start("Animate");
            _schedulerPhase = SchedulerPhase.TransientCallbacks;
            Dictionary<int, FrameCallbackEntry> callbacks = _transientCallbacks;
            _transientCallbacks = [];
            foreach ((int id, FrameCallbackEntry callbackEntry) in callbacks)
            {
                if (!_removedIds.Contains(id))
                {
                    InvokeFrameCallback(
                        callbackEntry.Callback,
                        _currentFrameTimeStamp.Value,
                        callbackEntry.DebugStack);
                }
            }

            _removedIds.Clear();
        }
        finally
        {
            _schedulerPhase = SchedulerPhase.MidFrameMicrotasks;
        }
    }

    /// <summary>Asks for a performance mode for as long as the returned handle is not disposed.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.requestPerformanceMode</c>: same-mode requests are reference
    /// counted, and a request for a different mode while one is active returns null.
    /// </remarks>
    public PerformanceModeRequestHandle? RequestPerformanceMode(DartPerformanceMode mode)
    {
        // conflicting requests are not allowed.
        // If a mode is requested, it's guaranteed to be either the requested mode or null.
        if (_performanceMode is not null && _performanceMode != mode)
        {
            return null;
        }

        if (_performanceMode == mode)
        {
            DebugAssertions.Assert(_numPerformanceModeRequests > 0);
            _numPerformanceModeRequests++;
        }
        else if (_performanceMode is null)
        {
            DebugAssertions.Assert(_numPerformanceModeRequests == 0);
            _performanceMode = mode;
            _numPerformanceModeRequests = 1;
        }

        return new PerformanceModeRequestHandle(DisposePerformanceModeRequest);
    }

    /// <summary>The performance mode currently requested, in debug and profile builds.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.debugGetRequestedPerformanceMode</c>.</remarks>
    public DartPerformanceMode? DebugGetRequestedPerformanceMode()
    {
        if (!(Constants.KDebugMode || Constants.KProfileMode))
        {
            return null;
        }

        return _performanceMode;
    }

    /// <summary>Called by the engine to produce a new frame.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.handleDrawFrame</c>: runs the persistent callbacks and then the
    /// post-frame callbacks. The C# body runs inside <see cref="EventLoop.EnterFrameworkThread"/>;
    /// draining the microtasks it queued is the caller's event loop's job, as in Dart.
    /// </remarks>
    public void HandleDrawFrame()
    {
        using EventLoop.FrameworkThreadScope scope = EventLoop.EnterFrameworkThread();
        DebugAssertions.Assert(_schedulerPhase == SchedulerPhase.MidFrameMicrotasks);
        _frameTimelineTask?.Finish(); // end the "Animate" phase
        try
        {
            // PERSISTENT FRAME CALLBACKS
            _schedulerPhase = SchedulerPhase.PersistentCallbacks;
            foreach (Action<TimeSpan> callback in _persistentCallbacks.ToArray())
            {
                InvokeFrameCallback(callback, CurrentFrameTimeStamp);
            }

            // POST-FRAME CALLBACKS
            _schedulerPhase = SchedulerPhase.PostFrameCallbacks;
            Action<TimeSpan>[] localPostFrameCallbacks = [.. _postFrameCallbacks];
            _postFrameCallbacks.Clear();
            if (!Constants.KReleaseMode)
            {
                FlutterTimeline.StartSync("POST_FRAME");
            }

            try
            {
                foreach (Action<TimeSpan> callback in localPostFrameCallbacks)
                {
                    InvokeFrameCallback(callback, CurrentFrameTimeStamp);
                }
            }
            finally
            {
                if (!Constants.KReleaseMode)
                {
                    FlutterTimeline.FinishSync();
                }
            }
        }
        finally
        {
            _schedulerPhase = SchedulerPhase.Idle;
            _frameTimelineTask?.Finish(); // end the Frame
            _handlingFrame = false;
            if (Constants.KDebugMode)
            {
                if (SchedulerDebug.DebugPrintEndFrameBanner)
                {
                    Print.DebugPrint(new string('▀', _debugBanner!.Length));
                }

                _debugBanner = null;
            }

            _currentFrameTimeStamp = null;
        }
    }

    /// <summary>Locks the dispatching of events until the task returned by <paramref name="callback"/>
    /// completes.</summary>
    /// <remarks>
    /// Dart's <c>BindingBase.lockEvents</c>: the lock is taken before the callback runs, so a callback
    /// that throws synchronously leaves it held, and the callback's own task is returned. The release
    /// runs as a microtask once that task completes, as Dart's <c>whenComplete</c> listener does.
    /// </remarks>
    public Task LockEvents(Func<Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        TimelineTask? debugTimelineTask = null;
        if (!Constants.KReleaseMode)
        {
            debugTimelineTask = new TimelineTask();
            debugTimelineTask.Start("Lock events");
        }

        _lockCount += 1;
        Task future = callback();
        future.ContinueWith(
            _ => EventLoop.ScheduleMicrotask(() =>
            {
                _lockCount -= 1;
                if (!Locked)
                {
                    if (!Constants.KReleaseMode)
                    {
                        debugTimelineTask!.Finish();
                    }

                    try
                    {
                        Unlocked();
                    }
                    catch (Exception error)
                    {
                        FlutterError.ReportError(new FlutterErrorDetails(
                            exception: error,
                            stack: error.StackTrace,
                            library: "foundation",
                            context: new ErrorDescription("while handling pending events")));
                    }
                }
            }),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return future;
    }

    /// <summary>Dart's <c>SchedulerBinding.initInstances</c>.</summary>
    private void InitInstances()
    {
        _instance = this;
        if (!Constants.KReleaseMode)
        {
            AddTimingsCallback(ProfileFrameTimings);
        }
    }

    private static void ProfileFrameTimings(IReadOnlyList<FrameTiming> timings)
    {
        foreach (FrameTiming timing in timings)
        {
            ProfileFramePostEvent(timing);
        }
    }

    /// <summary>Dart's <c>SchedulerBinding.initServiceExtensions</c>.</summary>
    /// <remarks>
    /// <c>ext.flutter.timeDilation</c> reads and writes <see cref="Scheduler.TimeDilation"/> outside
    /// release builds, on the static <see cref="BindingBase"/> registry.
    /// </remarks>
    private static void InitServiceExtensions()
    {
        if (!Constants.KReleaseMode)
        {
            BindingBase.RegisterNumericServiceExtension(
                name: SchedulerServiceExtensions.TimeDilation.DartName(),
                getter: static () => Task.FromResult(Scheduler.TimeDilation),
                setter: static value =>
                {
                    Scheduler.TimeDilation = value;
                    return Task.CompletedTask;
                });
        }
    }

    /// <summary>Posts a <c>Flutter.Frame</c> event describing <paramref name="frameTiming"/>.</summary>
    /// <remarks>Dart's private <c>SchedulerBinding._profileFramePostEvent</c>.</remarks>
    private static void ProfileFramePostEvent(FrameTiming frameTiming)
    {
        BindingBase.PostEvent("Flutter.Frame", new Dictionary<string, object?>
        {
            ["number"] = frameTiming.FrameNumber,
            ["startTime"] = frameTiming.TimestampInMicroseconds(FramePhase.BuildStart),
            ["elapsed"] = (long)frameTiming.TotalSpan.TotalMicroseconds,
            ["build"] = (long)frameTiming.BuildDuration.TotalMicroseconds,
            ["raster"] = (long)frameTiming.RasterDuration.TotalMicroseconds,
            ["vsyncOverhead"] = (long)frameTiming.VsyncOverhead.TotalMicroseconds,
        });
    }

    private static DiagnosticsStackTrace DescribeCallbackStack(string callbackStack)
    {
        return new DiagnosticsStackTrace(
            "\nThis exception was thrown in the context of a scheduler callback. When the scheduler "
            + "callback was _registered_ (as opposed to when the exception was thrown), this was the stack",
            callbackStack);
    }

    /// <summary>Renders a phase the way Dart's string interpolation of an enum value does.</summary>
    private static string DescribePhase(SchedulerPhase phase) =>
        $"{nameof(SchedulerPhase)}.{Diagnostics.EnumName(phase)}";

    private static void DebugDescribeTimeStamp(TimeSpan timeStamp, StringBuilder buffer)
    {
        long inMicroseconds = timeStamp.Ticks / TimeSpan.TicksPerMicrosecond;
        long inMilliseconds = inMicroseconds / 1000;
        long inSeconds = inMilliseconds / 1000;
        long inMinutes = inSeconds / 60;
        long inHours = inMinutes / 60;
        long inDays = inHours / 24;
        CultureInfo culture = CultureInfo.InvariantCulture;
        if (inDays > 0)
        {
            buffer.Append(culture, $"{inDays}d ");
        }

        if (inHours > 0)
        {
            buffer.Append(culture, $"{inHours - inDays * 24}h ");
        }

        if (inMinutes > 0)
        {
            buffer.Append(culture, $"{inMinutes - inHours * 60}m ");
        }

        if (inSeconds > 0)
        {
            buffer.Append(culture, $"{inSeconds - inMinutes * 60}s ");
        }

        buffer.Append(culture, $"{inMilliseconds - inSeconds * 1000}");
        long microseconds = inMicroseconds - inMilliseconds * 1000;
        if (microseconds > 0)
        {
            buffer.Append(culture, $".{microseconds.ToString(culture).PadLeft(3, '0')}");
        }

        buffer.Append("ms");
    }

    private void ExecuteTimingsCallbacks(IReadOnlyList<FrameTiming> timings)
    {
        Action<IReadOnlyList<FrameTiming>>[] clonedCallbacks = [.. _timingsCallbacks];
        foreach (Action<IReadOnlyList<FrameTiming>> callback in clonedCallbacks)
        {
            try
            {
                if (_timingsCallbacks.Contains(callback))
                {
                    callback(timings);
                }
            }
            catch (Exception exception)
            {
                InformationCollector? collector = null;
                if (Constants.KDebugMode)
                {
                    collector = () =>
                    [
                        new DiagnosticsProperty<Action<IReadOnlyList<FrameTiming>>>(
                            "The TimingsCallback that gets executed was",
                            callback,
                            style: DiagnosticsTreeStyle.ErrorProperty),
                    ];
                }

                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    stack: exception.StackTrace,
                    context: new ErrorDescription("while executing callbacks for FrameTiming"),
                    informationCollector: collector));
            }
        }
    }

    private void SetFramesEnabledState(bool enabled)
    {
        if (_framesEnabled == enabled)
        {
            return;
        }

        _framesEnabled = enabled;
        if (enabled)
        {
            ScheduleFrame();
        }
    }

    // Dart's `_ensureEventLoopCallback`/`_runTasks`: service the task queue from the event loop, one
    // task per turn, until it drains.
    private void EnsureEventLoopCallback()
    {
        DebugAssertions.Assert(!Locked);
        DebugAssertions.Assert(_taskQueue.Count > 0);
        if (_hasRequestedAnEventLoopCallback)
        {
            return;
        }

        _hasRequestedAnEventLoopCallback = true;
        PlatformDispatcher.Instance.TimerRun(RunTasks);
    }

    private void RunTasks()
    {
        _hasRequestedAnEventLoopCallback = false;
        if (HandleEventLoopCallback())
        {
            // runs next task when there's time
            EnsureEventLoopCallback();
        }
    }

    private TimeSpan AdjustForEpoch(TimeSpan rawTimeStamp)
    {
        TimeSpan rawDurationSinceEpoch = _firstRawTimeStampInEpoch is null
            ? TimeSpan.Zero
            : rawTimeStamp - _firstRawTimeStampInEpoch.Value;
        // Dart's `Duration` holds whole microseconds and `num.round` rounds half away from zero.
        long sinceMicroseconds = rawDurationSinceEpoch.Ticks / TimeSpan.TicksPerMicrosecond;
        long epochStartMicroseconds = _epochStart.Ticks / TimeSpan.TicksPerMicrosecond;
        long adjusted = (long)Math.Round(
            sinceMicroseconds / Scheduler.TimeDilation,
            MidpointRounding.AwayFromZero);
        return TimeSpan.FromTicks((adjusted + epochStartMicroseconds) * TimeSpan.TicksPerMicrosecond);
    }

    // Dart's private `_handleBeginFrame`, installed as the platform's `onBeginFrame`.
    private void HandleBeginFrameFromPlatform(TimeSpan rawTimeStamp)
    {
        if (_warmUpFrame)
        {
            // "begin frame" and "draw frame" must strictly alternate. Therefore
            // _rescheduleAfterWarmUpFrame cannot possibly be true here as it is reset by
            // _handleDrawFrame.
            DebugAssertions.Assert(!_rescheduleAfterWarmUpFrame);
            _rescheduleAfterWarmUpFrame = true;
            return;
        }

        HandleBeginFrame(rawTimeStamp);
    }

    // Dart's private `_handleDrawFrame`, installed as the platform's `onDrawFrame`.
    private void HandleDrawFrameFromPlatform()
    {
        if (_rescheduleAfterWarmUpFrame)
        {
            _rescheduleAfterWarmUpFrame = false;
            // Reschedule in a post-frame callback to allow the draw-frame phase of the warm-up frame
            // to finish.
            AddPostFrameCallback(
                _ =>
                {
                    // Force an engine frame.
                    //
                    // We need to reset _hasScheduledFrame here because we cancelled the original
                    // engine frame, and therefore did not run handleBeginFrame who is responsible
                    // for resetting it. So if a frame callback set this to true in the "begin
                    // frame" part of the warm-up frame, it will still be true here and cause us to
                    // skip scheduling an engine frame.
                    _hasScheduledFrame = false;
                    ScheduleFrame();
                },
                debugLabel: "SchedulerBinding.scheduleFrame");
            return;
        }

        HandleDrawFrame();
    }

    private void DisposePerformanceModeRequest()
    {
        _numPerformanceModeRequests--;
        if (_numPerformanceModeRequests == 0)
        {
            _performanceMode = null;
            PlatformDispatcher.Instance.RequestDartPerformanceMode(DartPerformanceMode.Balanced);
        }
    }

    // Calls the given callback with the given time stamp, catching any exception and reporting it.
    private static void InvokeFrameCallback(
        Action<TimeSpan> callback,
        TimeSpan timeStamp,
        string? callbackStack = null)
    {
        DebugAssertions.Assert(FrameCallbackEntry.DebugCurrentCallbackStack is null);
        if (Constants.KDebugMode)
        {
            FrameCallbackEntry.DebugCurrentCallbackStack = callbackStack;
        }

        try
        {
            callback(timeStamp);
        }
        catch (Exception exception)
        {
            FlutterError.ReportError(new FlutterErrorDetails(
                exception: exception,
                stack: exception.StackTrace,
                library: "scheduler library",
                context: new ErrorDescription("during a scheduler callback"),
                informationCollector: callbackStack is null
                    ? null
                    : () => [DescribeCallbackStack(callbackStack)]));
        }

        if (Constants.KDebugMode)
        {
            FrameCallbackEntry.DebugCurrentCallbackStack = null;
        }
    }

    // ---------------------------------------------------------------------------------------------
    // C#-only members below: host teardown and the hand-pumped test harnesses. See
    // docs/ai/DIVERGENCES.md and the BuildOwner.FlushBuild row of docs/ai/BACKLOG.md.
    // ---------------------------------------------------------------------------------------------

    /// <summary>Resets the binding to a freshly constructed state, for test isolation.</summary>
    internal static void ResetForTests()
    {
        FrameClock.ResetForTests();
        Scheduler.ResetTimeDilationForTests();
        Instance.ResetStateForTests();
    }

    /// <summary>Runs a frame now if one was scheduled or callbacks are waiting for one.</summary>
    internal static void PumpFrameForTests(TimeSpan? timestamp = null)
    {
        SchedulerBinding binding = Instance;
        if (!binding._hasScheduledFrame
            && binding._transientCallbacks.Count == 0
            && binding._postFrameCallbacks.Count == 0)
        {
            return;
        }

        binding._hasScheduledFrame = true;
        binding.HandleFrame(timestamp ?? TimeSpan.FromSeconds(FrameClock.CurrentSeconds));
    }

    /// <summary>Whether <paramref name="callback"/> is registered as a persistent frame callback.</summary>
    /// <remarks>Lets a binding re-register its callback after a test reset this one.</remarks>
    internal bool HasPersistentFrameCallback(Action<TimeSpan> callback) =>
        _persistentCallbacks.Contains(callback);

    /// <summary>
    /// <see cref="ScheduleFrameCallback(Action{TimeSpan}, bool, bool)"/> for a callback that must
    /// only ever run inside a real frame.
    /// </summary>
    /// <remarks>
    /// <see cref="RunScheduledFrameCallbacksOutsideFrame"/> drains the transient callbacks for a
    /// harness pump that produces no frame, and a <see cref="Ticker"/> must not advance animation
    /// time there — Dart only ever ticks from <c>handleBeginFrame</c>.
    /// </remarks>
    internal int ScheduleFrameCallback(
        Action<TimeSpan> callback,
        bool rescheduling,
        bool scheduleNewFrame,
        bool frameOnly)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (scheduleNewFrame)
        {
            ScheduleFrame();
        }

        _nextFrameCallbackId += 1;
        _transientCallbacks[_nextFrameCallbackId] = FrameCallbackEntry.Create(callback, rescheduling, frameOnly);
        return _nextFrameCallbackId;
    }

    /// <summary>
    /// Reports the persistent-callback phase for a build driven outside a frame, the way a
    /// hand-pumped harness builds; restores the previous phase when disposed.
    /// </summary>
    internal IDisposable BuildScope()
    {
        return new BuildScopeToken(this);
    }

    /// <summary>
    /// Runs the callbacks queued by <see cref="ScheduleFrameCallback(Action{TimeSpan}, bool, bool)"/>,
    /// without producing a frame: a widget-test harness pumps by flushing the build, layout and paint
    /// phases by hand, so the transient callbacks a real frame would run first have to be drained
    /// explicitly (see <c>BuildOwner.FlushBuild</c>).
    /// </summary>
    internal void RunScheduledFrameCallbacksOutsideFrame()
    {
        if (_handlingFrame || _transientCallbacks.Count == 0)
        {
            return;
        }

        using EventLoop.FrameworkThreadScope scope = EventLoop.EnterFrameworkThread();

        TimeSpan timestamp = _currentFrameTimeStamp ?? AdjustForEpoch(_lastRawTimeStamp);
        Dictionary<int, FrameCallbackEntry> callbacks = _transientCallbacks;
        _transientCallbacks = [];
        foreach ((int id, FrameCallbackEntry entry) in callbacks)
        {
            if (entry.FrameOnly)
            {
                // Tickers keep their registration for the next real frame; ticking them here would
                // advance animation time outside a frame, which Dart never does.
                _transientCallbacks[id] = entry;
            }
            else if (!_removedIds.Contains(id))
            {
                InvokeFrameCallback(entry.Callback, timestamp, entry.DebugStack);
            }
        }

        // `_removedIds` is not cleared here: the frame-only entries put back above may still be
        // cancelled before the real frame runs, and ids are never reused, so nothing else can match.
    }

    /// <summary>
    /// One engine frame as the event loop runs it: begin, drain the microtasks, draw, drain again.
    /// </summary>
    internal void HandleFrame(TimeSpan rawTimeStamp)
    {
        HandleBeginFrame(rawTimeStamp);
        EventLoop.FlushMicrotasks();
        HandleDrawFrame();
        EventLoop.FlushMicrotasks();
    }

    private void ResetStateForTests()
    {
        _transientCallbacks.Clear();
        _removedIds.Clear();
        _nextFrameCallbackId = 0;
        _persistentCallbacks.Clear();
        _postFrameCallbacks.Clear();
        _taskQueue.Clear();
        _timingsCallbacks.Clear();
        PlatformDispatcher.Instance.OnReportTimings = null;
        PlatformDispatcher.Instance.OnBeginFrame = null;
        PlatformDispatcher.Instance.OnDrawFrame = null;
        _hasScheduledFrame = false;
        _handlingFrame = false;
        _hasRequestedAnEventLoopCallback = false;
        _taskSequence = 0;
        _lockCount = 0;
        _warmUpFrame = false;
        _rescheduleAfterWarmUpFrame = false;
        _nextFrameCompleter = null;
        _performanceMode = null;
        _numPerformanceModeRequests = 0;
        _epochStart = TimeSpan.Zero;
        _firstRawTimeStampInEpoch = null;
        _lastRawTimeStamp = TimeSpan.Zero;
        _debugFrameNumber = 0;
        _debugBanner = null;
        _frameTimelineTask = Constants.KReleaseMode ? null : new TimelineTask();
        SchedulingStrategy = Scheduler.DefaultSchedulingStrategy;
        _schedulerPhase = SchedulerPhase.Idle;
        _framesEnabled = true;
        _lifecycleState = null;
        _currentFrameTimeStamp = null;
        FrameCallbackEntry.DebugCurrentCallbackStack = null;
        // What initInstances installs, so a reset binding matches a freshly constructed one.
        if (!Constants.KReleaseMode)
        {
            AddTimingsCallback(ProfileFrameTimings);
        }
    }

    /// <summary>Dart's private <c>_FrameCallbackEntry</c>.</summary>
    private sealed class FrameCallbackEntry
    {
        private FrameCallbackEntry(Action<TimeSpan> callback, string? debugStack, bool frameOnly)
        {
            Callback = callback;
            DebugStack = debugStack;
            FrameOnly = frameOnly;
        }

        /// <summary>The registration stack of the transient callback currently executing.</summary>
        public static string? DebugCurrentCallbackStack { get; set; }

        public Action<TimeSpan> Callback { get; }

        public string? DebugStack { get; }

        /// <summary>C#-only; see <see cref="RunScheduledFrameCallbacksOutsideFrame"/>.</summary>
        public bool FrameOnly { get; }

        public static FrameCallbackEntry Create(Action<TimeSpan> callback, bool rescheduling, bool frameOnly)
        {
            if (!Constants.KDebugMode)
            {
                return new FrameCallbackEntry(callback, null, frameOnly);
            }

            if (rescheduling)
            {
                if (DebugCurrentCallbackStack is null)
                {
                    throw new FlutterError(
                    [
                        new ErrorSummary(
                            "scheduleFrameCallback called with rescheduling true, but no callback is in scope."),
                        new ErrorDescription(
                            "The \"rescheduling\" argument should only be set to true if the callback is "
                            + "being reregistered from within the callback itself, and only then if the "
                            + "callback itself is entirely synchronous."),
                        new ErrorHint(
                            "If this is the initial registration of the callback, or if the callback is "
                            + "asynchronous, then do not use the \"rescheduling\" argument."),
                    ]);
                }

                return new FrameCallbackEntry(callback, DebugCurrentCallbackStack, frameOnly);
            }

            // TODO(ianh): trim the frames from this library, so that the call to scheduleFrameCallback
            // is the top one
            return new FrameCallbackEntry(callback, Environment.StackTrace, frameOnly);
        }
    }

    /// <summary>Dart's private <c>_TaskEntry</c>, split so the queue can hold entries of mixed result
    /// types.</summary>
    private abstract class TaskEntry(int priority, string? debugLabel, Flow? flow)
    {
        public int Priority { get; } = priority;

        public string? DebugLabel { get; } = debugLabel;

        public Flow? Flow { get; } = flow;

        public string? DebugStack { get; } = Constants.KDebugMode ? Environment.StackTrace : null;

        public void Run()
        {
            if (!Constants.KReleaseMode)
            {
                Timeline.TimeSync(
                    DebugLabel ?? "Scheduled Task",
                    RunTask,
                    flow: Flow is not null ? Developer.Flow.Step(Flow.Id) : null);
            }
            else
            {
                RunTask();
            }
        }

        // Dart's `completer.complete(task())`.
        protected abstract void RunTask();
    }

    private sealed class TaskEntry<T>(
        Func<Task<T>> task,
        int priority,
        string? debugLabel,
        Flow? flow)
        : TaskEntry(priority, debugLabel, flow)
    {
        private readonly TaskCompletionSource<T> _completer = new();

        public Task<T> Completion => _completer.Task;

        protected override void RunTask()
        {
            Task<T> result;
            try
            {
                result = task();
            }
            catch (Exception exception)
            {
                // Dart reports the error and leaves the future hanging; a task that never completes
                // is not a usable C# contract, so the failure surfaces to the awaiter as well as to
                // the error reporting HandleEventLoopCallback does.
                _completer.TrySetException(exception);
                throw;
            }

            if (result.IsCompletedSuccessfully)
            {
                _completer.TrySetResult(result.Result);
                return;
            }

            // Dart's `Completer.complete(FutureOr<T>)` chains: the scheduled task's future resolves
            // only once the task's own future does.
            result.ContinueWith(
                (completed, state) =>
                {
                    var completer = (TaskCompletionSource<T>)state!;
                    if (completed.IsFaulted)
                    {
                        completer.TrySetException(completed.Exception!.InnerExceptions);
                    }
                    else if (completed.IsCanceled)
                    {
                        completer.TrySetCanceled();
                    }
                    else
                    {
                        completer.TrySetResult(completed.Result);
                    }
                },
                _completer,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    private sealed class BuildScopeToken : IDisposable
    {
        private readonly SchedulerBinding _binding;
        private readonly SchedulerPhase _previousPhase;

        public BuildScopeToken(SchedulerBinding binding)
        {
            _binding = binding;
            _previousPhase = binding._schedulerPhase;
            if (_previousPhase == SchedulerPhase.Idle)
            {
                binding._schedulerPhase = SchedulerPhase.PersistentCallbacks;
            }
        }

        public void Dispose()
        {
            _binding._schedulerPhase = _previousPhase;
        }
    }
}
