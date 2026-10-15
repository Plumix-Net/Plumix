using Avalonia.Threading;

// C#-only infrastructure; no Dart parity source. A Dart isolate owns exactly one event loop: every
// `Future` continuation is a microtask (dart:async `scheduleMicrotask`) on the thread that owns the
// framework. .NET has neither, so this class keeps the microtask queue, claims the framework thread
// and installs `FrameworkSynchronizationContext` so `await`s resume there — see
// `docs/ai/DIVERGENCES.md`.

namespace Plumix;

/// <summary>
/// The framework's event loop: the microtask queue and the thread that owns the framework.
/// </summary>
/// <remarks>
/// Dart gets both from its isolate; <see cref="SchedulerBinding"/> and the other bindings only use
/// them. See <c>docs/ai/DIVERGENCES.md</c>.
/// </remarks>
public static class EventLoop
{
    private static readonly Queue<Action> _microtasks = [];

    // The only Plumix queue a foreign thread may reach: a `Task` continuation resumes on whatever
    // thread completed it, and `ScheduleMicrotask` is how it hands work back to the framework thread.
    private static readonly object MicrotaskSync = new();

    private static bool _microtaskDrainScheduled;

    [ThreadStatic]
    private static int _frameworkThreadDepth;

    private static int _frameworkThreadId;

    /// <summary>
    /// Whether the calling thread is the one that owns the framework — the thread the frame
    /// pipeline, the build scope and the microtask queue run on.
    /// </summary>
    /// <remarks>
    /// Dart has no counterpart, because an isolate owns its trees outright. The framework claims the
    /// thread the first time it enters <see cref="EnterFrameworkThread"/>.
    /// </remarks>
    public static bool IsFrameworkThread =>
        Volatile.Read(ref _frameworkThreadId) == Environment.CurrentManagedThreadId;

    /// <summary>
    /// The number of microtasks queued and not yet run, flutter_test's <c>FakeAsync.microtaskCount</c>.
    /// </summary>
    internal static int MicrotaskCount
    {
        get
        {
            lock (MicrotaskSync)
            {
                return _microtasks.Count;
            }
        }
    }

    /// <summary>
    /// Claims the calling thread as the framework thread for the lifetime of the returned scope and
    /// installs <see cref="FrameworkSynchronizationContext"/> on it, so an <c>await</c> inside the
    /// framework code that runs in the scope resumes as a microtask on this thread rather than on a
    /// thread-pool thread. Nested scopes are no-ops.
    /// </summary>
    internal static FrameworkThreadScope EnterFrameworkThread() => new();

    /// <summary>
    /// Starts framework <c>async</c> work on the framework thread and discards the task the way a
    /// Dart <c>unawaited</c> call does: <paramref name="body"/> runs up to its first <c>await</c>
    /// inside <see cref="EnterFrameworkThread"/>, so every continuation of it resumes as a microtask
    /// on this thread instead of on a thread-pool thread.
    /// </summary>
    /// <remarks>
    /// Use it at every fire-and-forget start of framework <c>async</c> work that a caller outside the
    /// frame pipeline can reach — a public <c>State</c> method, say — since the awaits inside inherit
    /// whatever context the starting thread had.
    /// </remarks>
    public static void RunAsync(Func<Task> body)
    {
        ArgumentNullException.ThrowIfNull(body);

        using FrameworkThreadScope scope = EnterFrameworkThread();
        _ = body();
    }

    /// <summary>
    /// Runs <paramref name="callback"/> at the end of the current event-loop turn, before the next
    /// frame: dart:async's <c>scheduleMicrotask</c>.
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
        using FrameworkThreadScope scope = EnterFrameworkThread();
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
    /// The scope <see cref="EnterFrameworkThread"/> returns: it claims the thread, swaps in the
    /// framework synchronization context and restores the previous one when it ends.
    /// </summary>
    internal readonly struct FrameworkThreadScope : IDisposable
    {
        private readonly SynchronizationContext? _previous;
        private readonly bool _outermost;

        public FrameworkThreadScope()
        {
            _outermost = _frameworkThreadDepth == 0;
            _frameworkThreadDepth += 1;
            if (!_outermost)
            {
                _previous = null;
                return;
            }

            _previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(FrameworkSynchronizationContext.Instance);
            Volatile.Write(ref _frameworkThreadId, Environment.CurrentManagedThreadId);
        }

        public void Dispose()
        {
            _frameworkThreadDepth -= 1;
            if (_outermost)
            {
                SynchronizationContext.SetSynchronizationContext(_previous);
            }
        }
    }
}
