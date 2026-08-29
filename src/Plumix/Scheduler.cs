using System.Diagnostics;
using Avalonia.Threading;

// Dart parity source (reference): flutter/packages/flutter/lib/src/scheduler/binding.dart (approximate)

namespace Plumix;

/// <summary>The phase that the frame pipeline is currently in.</summary>
/// <remarks>Dart parity source: flutter/packages/flutter/lib/src/scheduler/binding.dart (SchedulerPhase).</remarks>
public enum SchedulerPhase
{
    /// <summary>No frame is being processed.</summary>
    Idle,

    /// <summary>Animation tickers are being ticked.</summary>
    TransientCallbacks,

    /// <summary>The frame is being built, laid out and painted.</summary>
    PersistentCallbacks,

    /// <summary>Post-frame callbacks are running.</summary>
    PostFrameCallbacks,
}

public static class Scheduler
{
    private static readonly List<Ticker> _active = [];
    private static readonly Dictionary<int, Action<TimeSpan>> _transientCallbacks = [];
    private static readonly HashSet<int> _removedIds = [];
    private static int _nextFrameCallbackId;
    private static readonly List<Action<TimeSpan>> _persistentFrameCallbacks = [];
    private static readonly Queue<Action<TimeSpan>> _postFrameCallbacks = [];
    private static readonly Queue<Action> _microtasks = [];
    private static readonly List<TaskEntry> _taskQueue = [];
    private static readonly Stopwatch _sw = Stopwatch.StartNew();

    private static DispatcherTimer? _timer;
    private static bool _running;
    private static bool _hasScheduledFrame;
    private static bool _handlingFrame;
    private static bool _microtaskDrainScheduled;
    private static bool _hasRequestedAnEventLoopCallback;
    private static int _taskSequence;

    public static event Action<TimeSpan>? BeginFrame;
    public static event Action<TimeSpan>? DrawFrame;

    public static double CurrentSeconds => _sw.Elapsed.TotalSeconds;
    public static bool HasScheduledFrame => _hasScheduledFrame;

    /// <summary>The phase the frame pipeline is currently in.</summary>
    public static SchedulerPhase Phase { get; private set; } = SchedulerPhase.Idle;

    /// <summary>
    /// The timestamp handed to the callbacks of the frame currently being produced. Dart parity
    /// source: <c>SchedulerBinding.currentFrameTimeStamp</c>.
    /// </summary>
    public static TimeSpan CurrentFrameTimeStamp { get; private set; }

    /// <summary>
    /// Whether a frame is actually being produced. <see cref="Phase"/> alone cannot answer this in
    /// Plumix, because <see cref="BuildScope"/> reports the build phase for builds driven outside a
    /// frame.
    /// </summary>
    internal static bool IsHandlingFrame => _handlingFrame;

    /// <summary>
    /// Whether frames are currently being produced. Dart parity source:
    /// <c>SchedulerBinding.framesEnabled</c>, which Flutter derives from the app lifecycle state; a
    /// Plumix host sets it when the view stops being visible.
    /// </summary>
    public static bool FramesEnabled { get; set; } = true;

    public static void ScheduleFrame()
    {
        if (!FramesEnabled)
        {
            return;
        }

        _hasScheduledFrame = true;
        EnsureRunning();
    }

    /// <summary>
    /// Schedules a frame unless one is already being produced, in which case the frame in flight
    /// already covers the update.
    /// </summary>
    /// <remarks>
    /// Dart parity source: <c>SchedulerBinding.ensureVisualUpdate</c>, which returns without
    /// scheduling in the <c>transientCallbacks</c>, <c>midFrameMicrotasks</c> and
    /// <c>persistentCallbacks</c> phases (Plumix has no separate microtask phase). Its
    /// <see cref="Phase"/> also reports
    /// <see cref="SchedulerPhase.PersistentCallbacks"/> for builds driven outside a frame, so the
    /// check is qualified with <see cref="IsHandlingFrame"/>.
    /// </remarks>
    public static void EnsureVisualUpdate()
    {
        if (IsHandlingFrame
            && Phase is SchedulerPhase.TransientCallbacks or SchedulerPhase.PersistentCallbacks)
        {
            return;
        }

        ScheduleFrame();
    }

    /// <summary>
    /// Schedules a frame even when <see cref="FramesEnabled"/> is false. Dart parity source:
    /// <c>SchedulerBinding.scheduleForcedFrame</c>.
    /// </summary>
    public static void ScheduleForcedFrame()
    {
        _hasScheduledFrame = true;
        EnsureRunning();
    }

    /// <summary>
    /// Queues <paramref name="callback"/> for the end of the next frame. Plumix also schedules that
    /// frame by default; pass <paramref name="scheduleFrame"/> as false for Flutter's behavior, where
    /// a post-frame callback only runs once something else produces a frame.
    /// </summary>
    public static void AddPostFrameCallback(Action<TimeSpan> callback, bool scheduleFrame = true)
    {
        _postFrameCallbacks.Enqueue(callback);
        if (scheduleFrame)
        {
            ScheduleFrame();
        }
    }

    /// <summary>
    /// Runs <paramref name="callback"/> at the end of the current event-loop turn, before the next
    /// frame. Dart parity source: <c>dart:async scheduleMicrotask</c>.
    /// </summary>
    public static void ScheduleMicrotask(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _microtasks.Enqueue(callback);
        if (_microtaskDrainScheduled)
        {
            return;
        }

        _microtaskDrainScheduled = true;
        Dispatcher.UIThread.Post(FlushMicrotasks, DispatcherPriority.Send);
    }

    /// <summary>Drains every microtask queued by <see cref="ScheduleMicrotask"/>.</summary>
    public static void FlushMicrotasks()
    {
        _microtaskDrainScheduled = false;
        while (_microtasks.Count > 0)
        {
            _microtasks.Dequeue()();
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
    /// Decides whether a queued task may run now. Defaults to <see cref="DefaultSchedulingStrategy"/>.
    /// </summary>
    public static SchedulingStrategy SchedulingStrategy { get; set; } = DefaultSchedulingStrategy;

    /// <summary>The number of transient frame callbacks pending, i.e. the number of ticking tickers.</summary>
    public static int TransientCallbackCount => _active.Count + _transientCallbacks.Count;

    /// <summary>
    /// Schedules <paramref name="callback"/> to run once at the beginning of the next frame, in the
    /// transient-callback phase, and schedules that frame. Returns the id that
    /// <see cref="CancelFrameCallbackWithId"/> takes. Dart parity source:
    /// <c>SchedulerBinding.scheduleFrameCallback</c>.
    /// </summary>
    /// <param name="rescheduling">
    /// True when this call comes from inside a frame callback that is rescheduling itself; the frame
    /// currently being produced already covers it, so no additional frame is requested.
    /// </param>
    public static int ScheduleFrameCallback(Action<TimeSpan> callback, bool rescheduling = false)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _nextFrameCallbackId++;
        _transientCallbacks[_nextFrameCallbackId] = callback;
        if (!rescheduling)
        {
            ScheduleFrame();
        }

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
    /// Dart's `SchedulerBinding.scheduleTask`. Queues <paramref name="task"/> to run between frames in
    /// priority order, and completes the returned task with its result.
    /// </summary>
    public static Task<T> ScheduleTask<T>(Func<T> task, Priority priority, string? debugLabel = null)
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

        if (isFirstTask)
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
        if (_taskQueue.Count == 0)
        {
            return false;
        }

        var entry = _taskQueue[0];
        if (SchedulingStrategy(entry.Priority))
        {
            _taskQueue.RemoveAt(0);
            entry.Run();
        }

        return _taskQueue.Count > 0;
    }

    public static void AddPersistentFrameCallback(Action<TimeSpan> callback)
    {
        if (_persistentFrameCallbacks.Contains(callback))
        {
            return;
        }

        _persistentFrameCallbacks.Add(callback);
    }

    public static void RemovePersistentFrameCallback(Action<TimeSpan> callback)
    {
        _persistentFrameCallbacks.Remove(callback);
    }

    // Flutter runs every build inside the persistent-callback phase of a frame. Plumix hosts do the
    // same, but tests drive `BuildOwner.FlushBuild` directly, so a build scope reports the build
    // phase on its own when no frame is running.
    internal static IDisposable BuildScope()
    {
        return new BuildScopeToken();
    }

    internal static void AddTicker(Ticker ticker)
    {
        if (!_active.Contains(ticker))
        {
            _active.Add(ticker);
        }
    }

    internal static void RemoveTicker(Ticker ticker)
    {
        _active.Remove(ticker);

        if (!HasTickingTickers() && !_hasScheduledFrame && _postFrameCallbacks.Count == 0
            && _transientCallbacks.Count == 0)
        {
            Stop();
        }
    }

    internal static void PumpFrameForTests(TimeSpan? timestamp = null)
    {
        if (!_hasScheduledFrame && !HasTickingTickers() && _postFrameCallbacks.Count == 0
            && _transientCallbacks.Count == 0)
        {
            return;
        }

        if (!_hasScheduledFrame && HasTickingTickers())
        {
            _hasScheduledFrame = true;
        }

        if (_hasScheduledFrame)
        {
            HandleFrame(timestamp?.TotalSeconds ?? CurrentSeconds);
        }
    }

    internal static void ResetForTests()
    {
        Stop();
        _active.Clear();
        _transientCallbacks.Clear();
        _removedIds.Clear();
        _nextFrameCallbackId = 0;
        _persistentFrameCallbacks.Clear();
        _postFrameCallbacks.Clear();
        _taskQueue.Clear();
        _hasScheduledFrame = false;
        _handlingFrame = false;
        _hasRequestedAnEventLoopCallback = false;
        _taskSequence = 0;
        SchedulingStrategy = DefaultSchedulingStrategy;
        Phase = SchedulerPhase.Idle;
        FramesEnabled = true;
        CurrentFrameTimeStamp = TimeSpan.Zero;
        BeginFrame = null;
        DrawFrame = null;
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
        Dispatcher.UIThread.Post(RunTasks, DispatcherPriority.Background);
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
        if (!_hasScheduledFrame && !HasTickingTickers() && _postFrameCallbacks.Count == 0
            && _transientCallbacks.Count == 0)
        {
            Stop();
            return;
        }

        if (_handlingFrame)
        {
            return;
        }

        if (!_hasScheduledFrame && HasTickingTickers())
        {
            _hasScheduledFrame = true;
        }

        if (_hasScheduledFrame)
        {
            HandleFrame(CurrentSeconds);
        }
    }

    private static void HandleFrame(double nowSeconds)
    {
        _handlingFrame = true;
        _hasScheduledFrame = false;

        var timestamp = TimeSpan.FromSeconds(nowSeconds);
        CurrentFrameTimeStamp = timestamp;

        try
        {
            Phase = SchedulerPhase.TransientCallbacks;
            TickActiveTickers(timestamp);
            RunTransientFrameCallbacks(timestamp);
            Phase = SchedulerPhase.PersistentCallbacks;
            BeginFrame?.Invoke(timestamp);
            RunPersistentFrameCallbacks(timestamp);
            DrawFrame?.Invoke(timestamp);
            Phase = SchedulerPhase.PostFrameCallbacks;
            RunPostFrameCallbacks(timestamp);
        }
        finally
        {
            Phase = SchedulerPhase.Idle;
            _handlingFrame = false;
            FlushMicrotasks();
        }

        if (HasTickingTickers())
        {
            _hasScheduledFrame = true;
        }

        if (!_hasScheduledFrame && !HasTickingTickers() && _postFrameCallbacks.Count == 0
            && _transientCallbacks.Count == 0)
        {
            Stop();
        }
    }

    private static void RunTransientFrameCallbacks(TimeSpan timestamp)
    {
        if (_transientCallbacks.Count == 0)
        {
            return;
        }

        Dictionary<int, Action<TimeSpan>> callbacks = new(_transientCallbacks);
        _transientCallbacks.Clear();
        foreach ((int id, Action<TimeSpan> callback) in callbacks)
        {
            // A callback cancelled by an earlier callback in this same frame must not run.
            if (!_removedIds.Contains(id))
            {
                callback(timestamp);
            }
        }

        _removedIds.Clear();
    }

    private static void TickActiveTickers(TimeSpan timestamp)
    {
        var snapshot = _active.ToArray();
        foreach (Ticker ticker in snapshot)
        {
            // A ticker stopped or muted by an earlier callback in this same frame has already
            // unscheduled itself, which is Flutter's `cancelFrameCallbackWithId`.
            if (ticker.IsTickScheduled && ticker.IsTicking)
            {
                ticker.InternalTick(timestamp);
            }
        }
    }

    private static bool HasTickingTickers()
    {
        return _active.Any(ticker => ticker.IsTicking);
    }

    private static void RunPostFrameCallbacks(TimeSpan timestamp)
    {
        if (_postFrameCallbacks.Count == 0)
        {
            return;
        }

        int count = _postFrameCallbacks.Count;
        for (int index = 0; index < count; index++)
        {
            var callback = _postFrameCallbacks.Dequeue();
            callback(timestamp);
        }

        if (_postFrameCallbacks.Count > 0)
        {
            _hasScheduledFrame = true;
        }
    }

    /// <summary>Dart's private `_TaskEntry`, split so the queue can hold entries of mixed result types.</summary>
    private abstract class TaskEntry(int priority, int sequence, string? debugLabel)
    {
        public int Priority { get; } = priority;

        public int Sequence { get; } = sequence;

        public string? DebugLabel { get; } = debugLabel;

        public abstract void Run();
    }

    private sealed class TaskEntry<T>(Func<T> task, int priority, int sequence, string? debugLabel)
        : TaskEntry(priority, sequence, debugLabel)
    {
        private readonly TaskCompletionSource<T> _completer = new();

        public Task<T> Completion => _completer.Task;

        public override void Run()
        {
            try
            {
                _completer.TrySetResult(task());
            }
            catch (Exception exception)
            {
                // Dart reports the error and leaves the future hanging; a task that never completes is
                // not a usable C# contract, so the failure surfaces to the awaiter instead.
                _completer.TrySetException(exception);
            }
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

    private static void RunPersistentFrameCallbacks(TimeSpan timestamp)
    {
        if (_persistentFrameCallbacks.Count == 0)
        {
            return;
        }

        var snapshot = _persistentFrameCallbacks.ToArray();
        foreach (var callback in snapshot)
        {
            callback(timestamp);
        }
    }
}
