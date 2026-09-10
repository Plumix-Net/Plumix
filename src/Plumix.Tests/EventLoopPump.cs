// C#-only test infrastructure; no Dart parity source. Flutter's own tests get the same effect from
// `flutter_test`'s fake async zone, which runs the isolate's microtasks whenever the test pumps.

namespace Plumix.Tests;

/// <summary>
/// Turns the framework's event loop while a test waits for framework <c>async</c> work.
/// </summary>
/// <remarks>
/// Framework continuations resume as <see cref="Scheduler.ScheduleMicrotask"/> microtasks on the
/// framework thread (<see cref="Scheduler.EnterFrameworkThread"/>), so a test that blocks on a task
/// without turning the loop would wait forever. These helpers do the turning.
/// </remarks>
internal static class EventLoopPump
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Drains the microtask queue until <paramref name="task"/> completes, then awaits it.</summary>
    public static async Task<T> WaitFor<T>(Task<T> task, TimeSpan? timeout = null)
    {
        await WaitUntil(() => task.IsCompleted, timeout);
        return await task;
    }

    /// <summary>Drains the microtask queue until <paramref name="task"/> completes, then awaits it.</summary>
    public static async Task WaitFor(Task task, TimeSpan? timeout = null)
    {
        await WaitUntil(() => task.IsCompleted, timeout);
        await task;
    }

    /// <summary>
    /// The synchronous counterpart of <see cref="WaitUntil"/>, for tests that are not <c>async</c>.
    /// Returns whether <paramref name="predicate"/> held before the timeout.
    /// </summary>
    public static bool SpinUntil(Func<bool> predicate, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        TimeSpan deadline = timeout ?? DefaultTimeout;
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            Scheduler.FlushMicrotasks();
            if (predicate())
            {
                return true;
            }

            if (elapsed.Elapsed > deadline)
            {
                return false;
            }

            Thread.Sleep(1);
        }
    }

    /// <summary>
    /// Runs one event-loop turn: lets a foreign thread hand its continuation to the framework
    /// synchronization context, then drains the microtask queue.
    /// </summary>
    public static async Task Turn()
    {
        await Task.Delay(1);
        Scheduler.FlushMicrotasks();
    }

    /// <summary>
    /// Drains the microtask queue until <paramref name="predicate"/> holds, and throws
    /// <see cref="TimeoutException"/> if it never does.
    /// </summary>
    public static async Task WaitUntil(Func<bool> predicate, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        TimeSpan deadline = timeout ?? DefaultTimeout;
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            Scheduler.FlushMicrotasks();
            if (predicate())
            {
                return;
            }

            if (elapsed.Elapsed > deadline)
            {
                throw new TimeoutException("The framework never reached the awaited state.");
            }

            // A `Task` a foreign thread completes hands its continuation back through the framework
            // synchronization context, so the queue is only observable after that thread runs.
            await Task.Delay(1);
        }
    }
}
