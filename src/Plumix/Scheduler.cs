using System.Diagnostics;
using System.Globalization;
using System.Text;
using Avalonia.Threading;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/scheduler/binding.dart (adapted)

namespace Plumix;

/// <summary>The phase that the frame pipeline is currently in.</summary>
/// <remarks>
/// Dart parity source: flutter/packages/flutter/lib/src/scheduler/binding.dart (SchedulerPhase). The
/// values are ordered in the same order as the phases occur, so their relative index values can be
/// compared.
/// </remarks>
public enum SchedulerPhase
{
    /// <summary>No frame is being processed.</summary>
    /// <remarks>
    /// Tasks (scheduled by <see cref="Scheduler.ScheduleTask{T}"/>), microtasks (scheduled by
    /// <see cref="Scheduler.ScheduleMicrotask"/>), timer callbacks, event handlers and other
    /// callbacks run during this phase.
    /// </remarks>
    Idle,

    /// <summary>The transient callbacks (<see cref="Scheduler.ScheduleFrameCallback"/>) are running.</summary>
    /// <remarks>Typically, these callbacks handle updating objects to new animation states.</remarks>
    TransientCallbacks,

    /// <summary>Microtasks scheduled during the processing of transient callbacks are running.</summary>
    /// <remarks>This is the phase between <see cref="Scheduler.HandleBeginFrame"/> and
    /// <see cref="Scheduler.HandleDrawFrame"/>.</remarks>
    MidFrameMicrotasks,

    /// <summary>The persistent callbacks (<see cref="Scheduler.AddPersistentFrameCallback"/>) are running.</summary>
    /// <remarks>Typically, this is the build/layout/paint pipeline.</remarks>
    PersistentCallbacks,

    /// <summary>The post-frame callbacks (<see cref="Scheduler.AddPostFrameCallback"/>) are running.</summary>
    /// <remarks>
    /// Typically, these callbacks handle cleanup and scheduling of work for the next frame.
    /// </remarks>
    PostFrameCallbacks,
}

/// <summary>
/// Scheduler for running the frame pipeline and for triggering repeating callbacks.
/// </summary>
/// <remarks>
/// Dart's <c>SchedulerBinding</c>. Plumix has no <c>BindingBase</c> hierarchy, so the mixin is a
/// static class and the members Dart inherits from <c>BindingBase</c> that the scheduler needs
/// (<see cref="Locked"/>, <see cref="LockEvents"/>, <see cref="Unlocked"/>) live here too; see
/// `docs/ai/DIVERGENCES.md`.
/// </remarks>
public static class Scheduler
{
    private static readonly HashSet<int> _removedIds = [];
    private static readonly List<Action<TimeSpan>> _persistentFrameCallbacks = [];
    private static readonly List<PostFrameCallbackEntry> _postFrameCallbacks = [];
    private static readonly Queue<Action> _microtasks = [];
    private static readonly List<Action<IReadOnlyList<FrameTiming>>> _timingsCallbacks = [];

    // The only Plumix queue a foreign thread may reach: a `Task` continuation resumes on whatever
    // thread completed it, and `ScheduleMicrotask` is how it hands work back to the framework thread.
    private static readonly object MicrotaskSync = new();
    private static readonly List<TaskEntry> _taskQueue = [];
    private static readonly Stopwatch _sw = Stopwatch.StartNew();

    private static Dictionary<int, FrameCallbackEntry> _transientCallbacks = [];
    private static int _nextFrameCallbackId;
    private static DispatcherTimer? _timer;
    private static bool _running;
    private static bool _hasScheduledFrame;
    private static bool _handlingFrame;
    private static bool _microtaskDrainScheduled;
    private static bool _hasRequestedAnEventLoopCallback;
    private static int _taskSequence;
    private static int _lockCount;
    private static bool _warmUpFrame;
    private static bool _rescheduleAfterWarmUpFrame;
    private static bool _framesEnabled = true;
    private static TimeSpan? _currentFrameTimeStamp;
    private static TimeSpan? _firstRawTimeStampInEpoch;
    private static TimeSpan _epochStart = TimeSpan.Zero;
    private static TimeSpan _lastRawTimeStamp = TimeSpan.Zero;
    private static double _timeDilation = 1.0;
    private static int _debugFrameNumber;
    private static string? _debugBanner;
    private static TaskCompletionSource? _nextFrameCompleter;
    private static DartPerformanceMode? _performanceMode;
    private static int _numPerformanceModeRequests;

    static Scheduler()
    {
        PlatformDispatcher.Instance.FrameRequested += EnsureRunning;
    }

    /// <summary>
    /// Raised inside the persistent-callback phase, before the callbacks registered with
    /// <see cref="AddPersistentFrameCallback"/>.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RendererBinding</c> registers <c>drawFrame</c> as a persistent frame callback;
    /// Plumix hosts attach here instead, because a host is not a binding.
    /// </remarks>
    public static event Action<TimeSpan>? BeginFrame;

    /// <summary>Raised inside the persistent-callback phase, after <see cref="BeginFrame"/>.</summary>
    public static event Action<TimeSpan>? DrawFrame;

    /// <summary>The elapsed wall-clock time the host clock reports, in seconds.</summary>
    /// <remarks>
    /// Plumix-only: the engine hands Flutter a raw vsync timestamp, so a Plumix host reads its own
    /// clock here and passes the value to <see cref="HandleBeginFrame"/>.
    /// </remarks>
    public static double CurrentSeconds => _sw.Elapsed.TotalSeconds;

    /// <summary>Whether this scheduler has requested that a new frame be produced.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.hasScheduledFrame</c>.</remarks>
    public static bool HasScheduledFrame => _hasScheduledFrame;

    /// <summary>The phase the frame pipeline is currently in.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.schedulerPhase</c>.</remarks>
    public static SchedulerPhase Phase { get; private set; } = SchedulerPhase.Idle;

    /// <summary>
    /// Speed multiplier for animations: values greater than one slow animations down.
    /// </summary>
    /// <remarks>
    /// Dart's library-level <c>timeDilation</c>. Setting it resets the epoch first, so the timestamp
    /// of the frame that follows keeps the value the previous frame reported and only the frames
    /// after that advance at the new rate.
    /// </remarks>
    public static double TimeDilation
    {
        get => _timeDilation;
        set
        {
            if (value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "timeDilation must be positive.");
            }

            if (_timeDilation == value)
            {
                return;
            }

            // Reset the epoch first so that the start of the epoch is captured with the current time
            // dilation.
            ResetEpoch();
            _timeDilation = value;
        }
    }

    /// <summary>
    /// The timestamp handed to the callbacks of the frame currently being produced.
    /// </summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.currentFrameTimeStamp</c>, which asserts it is only read between
    /// the start of <see cref="HandleBeginFrame"/> and the end of <see cref="HandleDrawFrame"/>. The
    /// value is adjusted for the epoch and for <see cref="TimeDilation"/>; the raw timestamp is
    /// <see cref="CurrentSystemFrameTimeStamp"/>.
    /// </remarks>
    public static TimeSpan CurrentFrameTimeStamp
    {
        get
        {
            if (_currentFrameTimeStamp is null)
            {
                throw new InvalidOperationException(
                    "Scheduler.CurrentFrameTimeStamp is only valid while a frame is being produced. "
                    + "Outside a frame, read Scheduler.CurrentSystemFrameTimeStamp instead.");
            }

            return _currentFrameTimeStamp.Value;
        }
    }

    /// <summary>The raw timestamp the host reported for the most recent frame.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.currentSystemFrameTimeStamp</c>: unadjusted for the epoch and
    /// undilated, and valid outside a frame as well.
    /// </remarks>
    public static TimeSpan CurrentSystemFrameTimeStamp => _lastRawTimeStamp;

    /// <summary>
    /// Whether a frame is actually being produced. <see cref="Phase"/> alone cannot answer this in
    /// Plumix, because <see cref="BuildScope"/> reports the build phase for builds driven outside a
    /// frame.
    /// </summary>
    internal static bool IsHandlingFrame => _handlingFrame;

    /// <summary>Whether frames are currently being produced.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.framesEnabled</c>, which is derived from the app lifecycle state
    /// through <see cref="HandleAppLifecycleStateChanged"/>.
    /// </remarks>
    public static bool FramesEnabled => _framesEnabled;

    /// <summary>The last lifecycle state the platform reported, if any.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.lifecycleState</c>.</remarks>
    public static AppLifecycleState? LifecycleState { get; private set; }

    /// <summary>Whether events are currently being held back from the framework.</summary>
    /// <remarks>Dart's <c>BindingBase.locked</c>.</remarks>
    public static bool Locked => _lockCount > 0;

    /// <summary>
    /// Decides whether a queued task may run now. Defaults to <see cref="DefaultSchedulingStrategy"/>.
    /// </summary>
    /// <remarks>Dart's <c>SchedulerBinding.schedulingStrategy</c>.</remarks>
    public static SchedulingStrategy SchedulingStrategy { get; set; } = DefaultSchedulingStrategy;

    /// <summary>The number of transient frame callbacks currently pending.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.transientCallbackCount</c>. It is reset to zero just before the
    /// transient callbacks run, so during the transient phase it only counts callbacks registered
    /// for the next frame.
    /// </remarks>
    public static int TransientCallbackCount => _transientCallbacks.Count;

    /// <summary>
    /// A task that completes after the frame currently being produced (or, when the pipeline is
    /// idle, the frame this getter schedules) has finished.
    /// </summary>
    /// <remarks>Dart's <c>SchedulerBinding.endOfFrame</c>.</remarks>
    public static Task EndOfFrame
    {
        get
        {
            if (_nextFrameCompleter is null)
            {
                if (Phase == SchedulerPhase.Idle)
                {
                    ScheduleFrame();
                }

                _nextFrameCompleter = new TaskCompletionSource();
                AddPostFrameCallback(
                    _ =>
                    {
                        // Cleared before completing, because a C# continuation attached to the task
                        // runs inline and would otherwise hand the next awaiter this same, already
                        // completed, completer.
                        TaskCompletionSource completer = _nextFrameCompleter!;
                        _nextFrameCompleter = null;
                        completer.SetResult();
                    },
                    debugLabel: "SchedulerBinding.completeFrame");
            }

            return _nextFrameCompleter.Task;
        }
    }

    /// <summary>
    /// Dart's `defaultSchedulingStrategy`: while transient callbacks are pending, only tasks at
    /// <see cref="Priority.Animation"/> or above run.
    /// </summary>
    public static bool DefaultSchedulingStrategy(int priority)
    {
        return TransientCallbackCount <= 0 || priority >= Priority.Animation.Value;
    }

    /// <summary>
    /// Adds a callback that reports the <see cref="FrameTiming"/> of recently rasterized frames.
    /// </summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.addTimingsCallback</c>. Adding the same callback twice makes it
    /// run twice; the host-facing slot is only wired while at least one callback is registered.
    /// </remarks>
    public static void AddTimingsCallback(Action<IReadOnlyList<FrameTiming>> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _timingsCallbacks.Add(callback);
        if (_timingsCallbacks.Count == 1)
        {
            PlatformDispatcher.Instance.OnReportTimings = ExecuteTimingsCallbacks;
        }
    }

    /// <summary>Removes a callback added with <see cref="AddTimingsCallback"/>.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.removeTimingsCallback</c>. Only the first occurrence is removed.
    /// </remarks>
    public static void RemoveTimingsCallback(Action<IReadOnlyList<FrameTiming>> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _timingsCallbacks.Remove(callback);
        if (_timingsCallbacks.Count == 0)
        {
            PlatformDispatcher.Instance.OnReportTimings = null;
        }
    }

    /// <summary>Called by a host when the app's lifecycle state changes.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.handleAppLifecycleStateChanged</c>: frames are enabled while the
    /// app is <see cref="AppLifecycleState.Resumed"/> or <see cref="AppLifecycleState.Inactive"/>,
    /// and disabled otherwise. <c>WidgetsBinding.HandleAppLifecycleStateChanged</c> calls this for
    /// every state it generates, the way Dart's override calls <c>super</c>.
    /// </remarks>
    public static void HandleAppLifecycleStateChanged(AppLifecycleState state)
    {
        if (LifecycleState == state)
        {
            return;
        }

        LifecycleState = state;
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

    /// <summary>Requests that the host produce a new frame.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.scheduleFrame</c>: a no-op while frames are disabled.</remarks>
    public static void ScheduleFrame()
    {
        if (_hasScheduledFrame || !FramesEnabled)
        {
            return;
        }

        if (SchedulerDebug.DebugPrintScheduleFrameStacks && Constants.KDebugMode)
        {
            Assertions.DebugPrintStack(label: $"ScheduleFrame() called. Current phase is {Phase}.");
        }

        EnsureFrameCallbacksRegistered();
        PlatformDispatcher.Instance.ScheduleFrame();
        _hasScheduledFrame = true;
    }

    /// <summary>
    /// Schedules a frame even when <see cref="FramesEnabled"/> is false.
    /// </summary>
    /// <remarks>Dart's <c>SchedulerBinding.scheduleForcedFrame</c>.</remarks>
    public static void ScheduleForcedFrame()
    {
        if (_hasScheduledFrame)
        {
            return;
        }

        if (SchedulerDebug.DebugPrintScheduleFrameStacks && Constants.KDebugMode)
        {
            Assertions.DebugPrintStack(label: $"ScheduleForcedFrame() called. Current phase is {Phase}.");
        }

        EnsureFrameCallbacksRegistered();
        PlatformDispatcher.Instance.ScheduleFrame();
        _hasScheduledFrame = true;
    }

    /// <summary>
    /// Schedules a frame to run as soon as possible, rather than waiting for the host's next vsync.
    /// </summary>
    /// <remarks>Dart's <c>SchedulerBinding.scheduleWarmUpFrame</c>.</remarks>
    public static void ScheduleWarmUpFrame()
    {
        if (_warmUpFrame || Phase != SchedulerPhase.Idle)
        {
            return;
        }

        _warmUpFrame = true;
        bool hadScheduledFrame = _hasScheduledFrame;
        PlatformDispatcher.Instance.ScheduleWarmUpFrame(
            () => HandleBeginFrame(null),
            () =>
            {
                HandleDrawFrame();
                ResetEpoch();
                _warmUpFrame = false;
                if (hadScheduledFrame)
                {
                    ScheduleFrame();
                }
            });

        // Lock events so touch events etc don't insert themselves until the scheduled frame has
        // finished.
        LockEvents(static () => EndOfFrame);
    }

    /// <summary>Schedules a new frame unless one is already being produced.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.ensureVisualUpdate</c>, which schedules in the <c>idle</c> and
    /// <c>postFrameCallbacks</c> phases and returns without scheduling in the others. Plumix's
    /// <see cref="Phase"/> also reports <see cref="SchedulerPhase.PersistentCallbacks"/> for builds
    /// driven outside a frame, so the check is qualified with <see cref="IsHandlingFrame"/>.
    /// </remarks>
    public static void EnsureVisualUpdate()
    {
        if (IsHandlingFrame
            && Phase is SchedulerPhase.TransientCallbacks
                or SchedulerPhase.MidFrameMicrotasks
                or SchedulerPhase.PersistentCallbacks)
        {
            return;
        }

        ScheduleFrame();
    }

    /// <summary>Ensures the host's frame callbacks are wired to this scheduler.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.ensureFrameCallbacksRegistered</c>: an already-installed handler is
    /// never replaced.
    /// </remarks>
    public static void EnsureFrameCallbacksRegistered()
    {
        PlatformDispatcher.Instance.OnBeginFrame ??= HandleBeginFrameFromHost;
        PlatformDispatcher.Instance.OnDrawFrame ??= HandleDrawFrameFromHost;
    }

    /// <summary>
    /// Queues <paramref name="callback"/> to run at the end of the frame currently being produced,
    /// or of the next frame something else produces.
    /// </summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.addPostFrameCallback</c>, which does not itself request a frame.
    /// The callbacks run in registration order, exactly once.
    /// </remarks>
    public static void AddPostFrameCallback(Action<TimeSpan> callback, string debugLabel = "callback")
    {
        ArgumentNullException.ThrowIfNull(callback);

        _postFrameCallbacks.Add(new PostFrameCallbackEntry(callback, debugLabel));
    }

    /// <summary>
    /// Runs <paramref name="callback"/> at the end of the current event-loop turn, before the next
    /// frame. Dart parity source: <c>dart:async scheduleMicrotask</c>.
    /// </summary>
    public static void ScheduleMicrotask(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        bool scheduleDrain;
        lock (MicrotaskSync)
        {
            _microtasks.Enqueue(callback);
            scheduleDrain = !_microtaskDrainScheduled;
            _microtaskDrainScheduled = true;
        }

        if (scheduleDrain)
        {
            Dispatcher.UIThread.Post(FlushMicrotasks, DispatcherPriority.Send);
        }
    }

    /// <summary>Drains every microtask queued by <see cref="ScheduleMicrotask"/>.</summary>
    public static void FlushMicrotasks()
    {
        lock (MicrotaskSync)
        {
            _microtaskDrainScheduled = false;
        }

        while (true)
        {
            Action callback;
            lock (MicrotaskSync)
            {
                if (_microtasks.Count == 0)
                {
                    return;
                }

                callback = _microtasks.Dequeue();
            }

            // Outside the lock: a microtask may queue another one, and it runs framework code that
            // must never hold a lock a foreign thread can block on.
            callback();
        }
    }

    /// <summary>
    /// Schedules <paramref name="callback"/> to run once at the beginning of the next frame, in the
    /// transient-callback phase. Returns the id that <see cref="CancelFrameCallbackWithId"/> takes.
    /// </summary>
    /// <remarks>Dart's <c>SchedulerBinding.scheduleFrameCallback</c>.</remarks>
    /// <param name="rescheduling">
    /// True when this call comes from inside the callback itself, so that the debug stack of the
    /// original registration is carried forward instead of being replaced.
    /// </param>
    /// <param name="scheduleNewFrame">
    /// Whether to request a frame. Pass false when the caller has already requested one (which is
    /// what <see cref="Ticker"/> does, because it chooses between a forced and a regular frame).
    /// </param>
    public static int ScheduleFrameCallback(
        Action<TimeSpan> callback,
        bool rescheduling = false,
        bool scheduleNewFrame = true)
    {
        return ScheduleFrameCallback(callback, rescheduling, scheduleNewFrame, frameOnly: false);
    }

    /// <summary>
    /// <see cref="ScheduleFrameCallback(Action{TimeSpan}, bool, bool)"/> for a callback that must
    /// only ever run inside a real frame.
    /// </summary>
    /// <remarks>
    /// Plumix-only: <see cref="RunScheduledFrameCallbacksOutsideFrame"/> drains the transient
    /// callbacks for a harness pump that produces no frame, and a <see cref="Ticker"/> must not
    /// advance animation time there — Dart only ever ticks from <c>handleBeginFrame</c>.
    /// </remarks>
    internal static int ScheduleFrameCallback(
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

        _nextFrameCallbackId++;
        _transientCallbacks[_nextFrameCallbackId] = FrameCallbackEntry.Create(callback, rescheduling, frameOnly);
        return _nextFrameCallbackId;
    }

    /// <summary>
    /// Cancels the transient frame callback registered under <paramref name="id"/>. Dart parity
    /// source: <c>SchedulerBinding.cancelFrameCallbackWithId</c>.
    /// </summary>
    public static void CancelFrameCallbackWithId(int id)
    {
        _transientCallbacks.Remove(id);
        _removedIds.Add(id);
    }

    /// <summary>
    /// Reports whether any transient frame callbacks are still registered, without throwing.
    /// </summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.debugAssertNoTransientCallbacks</c>: it reports the leak through
    /// <see cref="FlutterError.ReportError"/> rather than throwing, and always returns true.
    /// </remarks>
    public static bool DebugAssertNoTransientCallbacks(string reason)
    {
        if (!Constants.KDebugMode || TransientCallbackCount == 0)
        {
            return true;
        }

        int count = TransientCallbackCount;
        Dictionary<int, FrameCallbackEntry> callbacks = new(_transientCallbacks);
        FlutterError.ReportError(new FlutterErrorDetails(
            exception: new FlutterError(reason),
            library: "scheduler library",
            informationCollector: () =>
            {
                List<DiagnosticsNode> nodes =
                [
                    count == 1
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

    /// <summary>Throws when a performance-mode request is still outstanding.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.debugAssertNoPendingPerformanceModeRequests</c>.</remarks>
    public static bool DebugAssertNoPendingPerformanceModeRequests(string reason)
    {
        if (Constants.KDebugMode && _performanceMode is not null)
        {
            throw new FlutterError(reason);
        }

        return true;
    }

    /// <summary>Throws when <see cref="TimeDilation"/> is not back at 1.0.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.debugAssertNoTimeDilation</c>.</remarks>
    public static bool DebugAssertNoTimeDilation(string reason)
    {
        if (Constants.KDebugMode && TimeDilation != 1.0)
        {
            throw new FlutterError(reason);
        }

        return true;
    }

    /// <summary>Prints the stack for the transient callback currently executing, if any.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.debugPrintTransientCallbackRegistrationStack</c>.</remarks>
    public static void DebugPrintTransientCallbackRegistrationStack()
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (FrameCallbackEntry.DebugCurrentCallbackStack is { } stack)
        {
            Print.PrintLine("When the current transient callback was registered, this was the stack:");
            Print.PrintLine(string.Join(
                '\n',
                FlutterError.DefaultStackFilter(stack.TrimEnd().Split('\n'))));
        }
        else
        {
            Print.PrintLine("No transient callback is currently executing.");
        }
    }

    /// <summary>
    /// Dart's `SchedulerBinding.scheduleTask`. Queues <paramref name="task"/> to run between frames in
    /// priority order, and completes the returned task with its result.
    /// </summary>
    public static Task<T> ScheduleTask<T>(Func<T> task, Priority priority, string? debugLabel = null)
    {
        ArgumentNullException.ThrowIfNull(task);

        return ScheduleTask(() => Task.FromResult(task()), priority, debugLabel);
    }

    /// <summary>
    /// Dart's `SchedulerBinding.scheduleTask` for a task that itself completes asynchronously: the
    /// returned task completes only once the task's own task does, the way Dart's
    /// <c>Completer.complete(FutureOr&lt;T&gt;)</c> chains.
    /// </summary>
    public static Task<T> ScheduleTask<T>(Func<Task<T>> task, Priority priority, string? debugLabel = null)
    {
        ArgumentNullException.ThrowIfNull(task);

        bool isFirstTask = _taskQueue.Count == 0;
        var entry = new TaskEntry<T>(task, priority.Value, _taskSequence++, debugLabel);
        _taskQueue.Add(entry);

        // Dart uses a heap ordered by descending priority; the insertion sequence breaks ties so that
        // equal-priority tasks keep the order they were scheduled in.
        _taskQueue.Sort(static (left, right) => left.Priority != right.Priority
            ? right.Priority.CompareTo(left.Priority)
            : left.Sequence.CompareTo(right.Sequence));

        if (isFirstTask && !Locked)
        {
            EnsureEventLoopCallback();
        }

        return entry.Completion;
    }

    /// <summary>
    /// Dart's `SchedulerBinding.handleEventLoopCallback`. Runs the highest-priority queued task the
    /// current <see cref="SchedulingStrategy"/> admits, and reports whether tasks remain queued.
    /// </summary>
    public static bool HandleEventLoopCallback()
    {
        if (_taskQueue.Count == 0 || Locked)
        {
            return false;
        }

        TaskEntry entry = _taskQueue[0];
        if (SchedulingStrategy(entry.Priority))
        {
            try
            {
                _taskQueue.RemoveAt(0);
                entry.Run();
            }
            catch (Exception exception)
            {
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    library: "scheduler library",
                    context: new ErrorDescription("during a task callback"),
                    informationCollector: entry.DebugStack is null
                        ? null
                        : () =>
                        [
                            new DiagnosticsStackTrace(
                                "\nThis exception was thrown in the context of a scheduler callback. "
                                + "When the scheduler callback was _registered_ (as opposed to when the "
                                + "exception was thrown), this was the stack",
                                entry.DebugStack),
                        ]));
            }

            return _taskQueue.Count > 0;
        }

        return true;
    }

    /// <summary>Locks the dispatching of events until the task returned by the callback completes.</summary>
    /// <remarks>Dart's <c>BindingBase.lockEvents</c>.</remarks>
    public static Task LockEvents(Func<Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        Task future = callback();
        _lockCount++;
        return future.ContinueWith(
            static _ =>
            {
                _lockCount--;
                if (Locked)
                {
                    return;
                }

                try
                {
                    Unlocked();
                }
                catch (Exception exception)
                {
                    FlutterError.ReportError(new FlutterErrorDetails(
                        exception: exception,
                        library: "foundation",
                        context: new ErrorDescription("while handling pending events")));
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    /// <summary>Called when the last <see cref="LockEvents"/> lock is released.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.unlocked</c>, which re-arms the task queue that
    /// <see cref="ScheduleTask{T}(Func{T}, Priority, string?)"/> left unserviced while locked.
    /// </remarks>
    public static void Unlocked()
    {
        if (_taskQueue.Count > 0)
        {
            EnsureEventLoopCallback();
        }
    }

    /// <summary>
    /// Adds a callback that runs during the persistent-callback phase of every frame.
    /// </summary>
    /// <remarks>Dart's <c>SchedulerBinding.addPersistentFrameCallback</c>, which does not request a frame.</remarks>
    public static void AddPersistentFrameCallback(Action<TimeSpan> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (_persistentFrameCallbacks.Contains(callback))
        {
            return;
        }

        _persistentFrameCallbacks.Add(callback);
    }

    /// <summary>Removes a callback added with <see cref="AddPersistentFrameCallback"/>.</summary>
    /// <remarks>
    /// Plumix-only. Dart's persistent callbacks cannot be unregistered because a binding lives as
    /// long as the isolate; a Plumix host is disposable, so it removes its own callback on teardown.
    /// </remarks>
    public static void RemovePersistentFrameCallback(Action<TimeSpan> callback)
    {
        _persistentFrameCallbacks.Remove(callback);
    }

    /// <summary>Requests a performance mode from the runtime for as long as the handle lives.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.requestPerformanceMode</c>: same-mode requests are reference
    /// counted, and a request for a different mode while one is active returns null.
    /// </remarks>
    public static PerformanceModeRequestHandle? RequestPerformanceMode(DartPerformanceMode mode)
    {
        if (_performanceMode is not null && _performanceMode != mode)
        {
            return null;
        }

        if (_performanceMode == mode)
        {
            _numPerformanceModeRequests++;
        }
        else if (_performanceMode is null)
        {
            _performanceMode = mode;
            _numPerformanceModeRequests = 1;
        }

        return new PerformanceModeRequestHandle(DisposePerformanceModeRequest);
    }

    /// <summary>The performance mode currently requested, in debug and profile builds.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.debugGetRequestedPerformanceMode</c>.</remarks>
    public static DartPerformanceMode? DebugGetRequestedPerformanceMode()
    {
        return Constants.KDebugMode || Constants.KProfileMode ? _performanceMode : null;
    }

    /// <summary>Prepares the scheduler to compute frame timestamps against a new epoch.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.resetEpoch</c>.</remarks>
    public static void ResetEpoch()
    {
        _epochStart = AdjustForEpoch(_lastRawTimeStamp);
        _firstRawTimeStampInEpoch = null;
    }

    /// <summary>Runs the begin-frame half of a frame.</summary>
    /// <remarks>
    /// Dart's <c>SchedulerBinding.handleBeginFrame</c>. A null <paramref name="rawTimeStamp"/> marks
    /// a warm-up frame, which reuses the previous raw timestamp and does not advance the epoch.
    /// </remarks>
    public static void HandleBeginFrame(TimeSpan? rawTimeStamp)
    {
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
                string frameTimeStampDescription = rawTimeStamp is not null
                    ? DebugDescribeTimeStamp(_currentFrameTimeStamp.Value)
                    : "(warm-up frame)";
                string frameNumber = _debugFrameNumber.ToString(CultureInfo.InvariantCulture);
                _debugBanner = $"▄▄▄▄▄▄▄▄ Frame {frameNumber.PadRight(7)}"
                    + $"   {frameTimeStampDescription.PadLeft(18)} ▄▄▄▄▄▄▄▄";
                if (SchedulerDebug.DebugPrintBeginFrameBanner)
                {
                    Print.PrintLine(_debugBanner);
                }
            }
        }

        _handlingFrame = true;
        _hasScheduledFrame = false;
        try
        {
            Phase = SchedulerPhase.TransientCallbacks;
            Dictionary<int, FrameCallbackEntry> callbacks = _transientCallbacks;
            _transientCallbacks = [];
            foreach ((int id, FrameCallbackEntry entry) in callbacks)
            {
                if (!_removedIds.Contains(id))
                {
                    InvokeFrameCallback(entry.Callback, _currentFrameTimeStamp.Value, entry.DebugStack);
                }
            }

            _removedIds.Clear();
        }
        finally
        {
            Phase = SchedulerPhase.MidFrameMicrotasks;
        }
    }

    /// <summary>Runs the draw-frame half of a frame.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.handleDrawFrame</c>.</remarks>
    public static void HandleDrawFrame()
    {
        TimeSpan timestamp = _currentFrameTimeStamp ?? AdjustForEpoch(_lastRawTimeStamp);
        try
        {
            Phase = SchedulerPhase.PersistentCallbacks;
            BeginFrame?.Invoke(timestamp);
            foreach (Action<TimeSpan> callback in _persistentFrameCallbacks.ToArray())
            {
                InvokeFrameCallback(callback, timestamp);
            }

            DrawFrame?.Invoke(timestamp);

            Phase = SchedulerPhase.PostFrameCallbacks;
            PostFrameCallbackEntry[] localPostFrameCallbacks = [.. _postFrameCallbacks];
            _postFrameCallbacks.Clear();
            foreach (PostFrameCallbackEntry entry in localPostFrameCallbacks)
            {
                InvokeFrameCallback(entry.Callback, timestamp);
            }
        }
        finally
        {
            Phase = SchedulerPhase.Idle;
            _handlingFrame = false;
            if (Constants.KDebugMode)
            {
                if (SchedulerDebug.DebugPrintEndFrameBanner && _debugBanner is not null)
                {
                    Print.PrintLine(new string('▀', _debugBanner.Length));
                }

                _debugBanner = null;
            }

            _currentFrameTimeStamp = null;
            FlushMicrotasks();
        }
    }

    internal static IDisposable BuildScope()
    {
        return new BuildScopeToken();
    }

    internal static void PumpFrameForTests(TimeSpan? timestamp = null)
    {
        if (!_hasScheduledFrame && _transientCallbacks.Count == 0 && _postFrameCallbacks.Count == 0)
        {
            return;
        }

        _hasScheduledFrame = true;
        HandleFrame(timestamp ?? TimeSpan.FromSeconds(CurrentSeconds));
    }

    internal static void ResetForTests()
    {
        Stop();
        _transientCallbacks.Clear();
        _removedIds.Clear();
        _nextFrameCallbackId = 0;
        _persistentFrameCallbacks.Clear();
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
        _timeDilation = 1.0;
        _epochStart = TimeSpan.Zero;
        _firstRawTimeStampInEpoch = null;
        _lastRawTimeStamp = TimeSpan.Zero;
        _debugFrameNumber = 0;
        _debugBanner = null;
        SchedulingStrategy = DefaultSchedulingStrategy;
        Phase = SchedulerPhase.Idle;
        _framesEnabled = true;
        LifecycleState = null;
        _currentFrameTimeStamp = null;
        BeginFrame = null;
        DrawFrame = null;
    }

    /// <summary>Resets the lifecycle-derived state, without touching the frame pipeline.</summary>
    /// <remarks>Dart's <c>SchedulerBinding.resetInternalState</c>, which is test-only there too.</remarks>
    internal static void ResetInternalState()
    {
        LifecycleState = null;
        _framesEnabled = true;
    }

    /// <summary>
    /// Runs the callbacks queued by <see cref="ScheduleFrameCallback"/>, without producing a frame.
    /// Plumix-only: a widget-test harness pumps by flushing the build, layout and paint phases by
    /// hand instead of producing a frame, so the transient callbacks a real frame would run first
    /// have to be drained explicitly (see <c>BuildOwner.FlushBuild</c>).
    /// </summary>
    internal static void RunScheduledFrameCallbacksOutsideFrame()
    {
        if (_handlingFrame || _transientCallbacks.Count == 0)
        {
            return;
        }

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

    internal static void HandleFrame(TimeSpan rawTimeStamp)
    {
        HandleBeginFrame(rawTimeStamp);
        FlushMicrotasks();
        HandleDrawFrame();
    }

    private static void HandleBeginFrameFromHost(TimeSpan rawTimeStamp)
    {
        if (_warmUpFrame)
        {
            // "begin frame" and "draw frame" must strictly alternate, so this flag cannot already be
            // set here: HandleDrawFrameFromHost resets it.
            _rescheduleAfterWarmUpFrame = true;
            return;
        }

        HandleBeginFrame(rawTimeStamp);
    }

    private static void HandleDrawFrameFromHost()
    {
        if (_rescheduleAfterWarmUpFrame)
        {
            _rescheduleAfterWarmUpFrame = false;
            AddPostFrameCallback(
                _ =>
                {
                    // The original frame the host had scheduled was cancelled by the warm-up frame,
                    // and never reached HandleBeginFrame, which is what normally clears this.
                    _hasScheduledFrame = false;
                    ScheduleFrame();
                },
                debugLabel: "SchedulerBinding.scheduleFrame");
            return;
        }

        HandleDrawFrame();
    }

    private static void ExecuteTimingsCallbacks(IReadOnlyList<FrameTiming> timings)
    {
        Action<IReadOnlyList<FrameTiming>>[] clonedCallbacks = [.. _timingsCallbacks];
        foreach (Action<IReadOnlyList<FrameTiming>> callback in clonedCallbacks)
        {
            try
            {
                // A callback removed by an earlier callback in this same dispatch is skipped.
                if (_timingsCallbacks.Contains(callback))
                {
                    callback(timings);
                }
            }
            catch (Exception exception)
            {
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    context: new ErrorDescription("while executing callbacks for FrameTiming"),
                    informationCollector: () =>
                    [
                        new DiagnosticsProperty<Action<IReadOnlyList<FrameTiming>>>(
                            "The TimingsCallback that gets executed was",
                            callback,
                            style: DiagnosticsTreeStyle.ErrorProperty),
                    ]));
            }
        }
    }

    private static void SetFramesEnabledState(bool enabled)
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

    private static void DisposePerformanceModeRequest()
    {
        _numPerformanceModeRequests--;
        if (_numPerformanceModeRequests == 0)
        {
            _performanceMode = null;
            PlatformDispatcher.Instance.RequestDartPerformanceMode(DartPerformanceMode.Balanced);
        }
    }

    private static TimeSpan AdjustForEpoch(TimeSpan rawTimeStamp)
    {
        TimeSpan rawDurationSinceEpoch = _firstRawTimeStampInEpoch is null
            ? TimeSpan.Zero
            : rawTimeStamp - _firstRawTimeStampInEpoch.Value;
        double microseconds = rawDurationSinceEpoch.Ticks / (double)(TimeSpan.TicksPerMillisecond / 1000);
        // Dart rounds half away from zero; .NET's default Math.Round is banker's rounding.
        double rounded = Math.Round(microseconds / TimeDilation, MidpointRounding.AwayFromZero);
        return TimeSpan.FromTicks(
            (long)rounded * (TimeSpan.TicksPerMillisecond / 1000) + _epochStart.Ticks);
    }

    private static string DebugDescribeTimeStamp(TimeSpan timeStamp)
    {
        var buffer = new StringBuilder();
        if (timeStamp.Days > 0)
        {
            buffer.Append(CultureInfo.InvariantCulture, $"{timeStamp.Days}d ");
        }

        if ((long)timeStamp.TotalHours > 0)
        {
            buffer.Append(CultureInfo.InvariantCulture, $"{timeStamp.Hours}h ");
        }

        if ((long)timeStamp.TotalMinutes > 0)
        {
            buffer.Append(CultureInfo.InvariantCulture, $"{timeStamp.Minutes}m ");
        }

        if ((long)timeStamp.TotalSeconds > 0)
        {
            buffer.Append(CultureInfo.InvariantCulture, $"{timeStamp.Seconds}s ");
        }

        buffer.Append(CultureInfo.InvariantCulture, $"{timeStamp.Milliseconds}");
        long microseconds = timeStamp.Ticks / (TimeSpan.TicksPerMillisecond / 1000)
            - (long)timeStamp.TotalMilliseconds * 1000;
        if (microseconds > 0)
        {
            string fraction = microseconds.ToString(CultureInfo.InvariantCulture).PadLeft(3, '0');
            buffer.Append(CultureInfo.InvariantCulture, $".{fraction}");
        }

        buffer.Append("ms");
        return buffer.ToString();
    }

    private static void InvokeFrameCallback(
        Action<TimeSpan> callback,
        TimeSpan timeStamp,
        string? callbackStack = null)
    {
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
                library: "scheduler library",
                context: new ErrorDescription("during a scheduler callback"),
                informationCollector: callbackStack is null
                    ? null
                    : () =>
                    [
                        new DiagnosticsStackTrace(
                            "\nThis exception was thrown in the context of a scheduler callback. "
                            + "When the scheduler callback was _registered_ (as opposed to when the "
                            + "exception was thrown), this was the stack",
                            callbackStack),
                    ]));
        }

        if (Constants.KDebugMode)
        {
            FrameCallbackEntry.DebugCurrentCallbackStack = null;
        }
    }

    // Dart's `_ensureEventLoopCallback`/`_runTasks`: service the task queue from the event loop, one
    // task per turn, until it drains.
    private static void EnsureEventLoopCallback()
    {
        if (_hasRequestedAnEventLoopCallback)
        {
            return;
        }

        _hasRequestedAnEventLoopCallback = true;
        PlatformDispatcher.Instance.TimerRun(RunTasks);
    }

    private static void RunTasks()
    {
        _hasRequestedAnEventLoopCallback = false;
        if (HandleEventLoopCallback())
        {
            EnsureEventLoopCallback();
        }
    }

    private static void EnsureRunning()
    {
        if (_running)
        {
            return;
        }

        _timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(16),
            DispatcherPriority.Render,
            (_, _) => Tick());

        _timer.Start();
        _running = true;
    }

    private static void Stop()
    {
        if (!_running)
        {
            return;
        }

        _timer?.Stop();
        _timer = null;
        _running = false;
    }

    private static void Tick()
    {
        if (!_hasScheduledFrame && _transientCallbacks.Count == 0 && _postFrameCallbacks.Count == 0)
        {
            Stop();
            return;
        }

        if (_handlingFrame)
        {
            return;
        }

        if (_hasScheduledFrame)
        {
            PlatformDispatcher.Instance.OnBeginFrame?.Invoke(TimeSpan.FromSeconds(CurrentSeconds));
            FlushMicrotasks();
            PlatformDispatcher.Instance.OnDrawFrame?.Invoke();
        }
    }

    /// <summary>Dart's private `_FrameCallbackEntry`.</summary>
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
                            "ScheduleFrameCallback called with rescheduling true, but no callback is in scope."),
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

            return new FrameCallbackEntry(callback, Environment.StackTrace, frameOnly);
        }
    }

    private sealed record PostFrameCallbackEntry(Action<TimeSpan> Callback, string DebugLabel);

    /// <summary>Dart's private `_TaskEntry`, split so the queue can hold entries of mixed result types.</summary>
    private abstract class TaskEntry(int priority, int sequence, string? debugLabel)
    {
        public int Priority { get; } = priority;

        public int Sequence { get; } = sequence;

        public string? DebugLabel { get; } = debugLabel;

        public string? DebugStack { get; } = Constants.KDebugMode ? Environment.StackTrace : null;

        public abstract void Run();
    }

    private sealed class TaskEntry<T>(Func<Task<T>> task, int priority, int sequence, string? debugLabel)
        : TaskEntry(priority, sequence, debugLabel)
    {
        private readonly TaskCompletionSource<T> _completer = new();

        public Task<T> Completion => _completer.Task;

        public override void Run()
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
        private readonly SchedulerPhase _previousPhase = Phase;

        public BuildScopeToken()
        {
            if (Phase == SchedulerPhase.Idle)
            {
                Phase = SchedulerPhase.PersistentCallbacks;
            }
        }

        public void Dispose()
        {
            Phase = _previousPhase;
        }
    }
}

/// <summary>
/// The handle returned by <see cref="Scheduler.RequestPerformanceMode"/>; disposing it releases the
/// request.
/// </summary>
/// <remarks>Dart's <c>PerformanceModeRequestHandle</c>.</remarks>
public sealed class PerformanceModeRequestHandle
{
    private Action? _cleanup;

    internal PerformanceModeRequestHandle(Action cleanup) => _cleanup = cleanup;

    /// <summary>Releases the performance-mode request this handle stands for.</summary>
    public void Dispose()
    {
        if (_cleanup is null)
        {
            throw new InvalidOperationException("A PerformanceModeRequestHandle was disposed twice.");
        }

        _cleanup();
        _cleanup = null;
    }
}
