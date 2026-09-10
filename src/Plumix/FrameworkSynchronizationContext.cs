using System.Runtime.ExceptionServices;

// C#-only infrastructure; no Dart parity source. An isolate owns exactly one event loop, so every
// Dart `Future` continuation is a microtask that runs on the thread that owns the framework. .NET
// resumes an `await` on whatever thread completed the task unless a `SynchronizationContext` is
// current, so `Scheduler.EnterFrameworkThread` installs this one around every entry point that can
// start framework `async` work, and it turns each continuation into a `Scheduler` microtask.

namespace Plumix;

/// <summary>
/// The synchronization context the framework runs under: it posts every continuation to
/// <see cref="Scheduler.ScheduleMicrotask"/>, so framework <c>async</c> code resumes on the
/// framework thread at the next event-loop turn instead of on a thread-pool thread.
/// </summary>
internal sealed class FrameworkSynchronizationContext : SynchronizationContext
{
    /// <summary>The single instance; the context carries no per-thread state.</summary>
    internal static readonly FrameworkSynchronizationContext Instance = new();

    private FrameworkSynchronizationContext()
    {
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull(d);

        Scheduler.ScheduleMicrotask(() => d(state));
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull(d);

        if (Scheduler.IsFrameworkThread)
        {
            d(state);
            return;
        }

        // A foreign thread cannot run framework code itself, so it hands the callback to the
        // microtask queue and waits for the framework thread to drain it.
        using var done = new ManualResetEventSlim(false);
        ExceptionDispatchInfo? failure = null;
        Scheduler.ScheduleMicrotask(() =>
        {
            try
            {
                d(state);
            }
            catch (Exception error)
            {
                failure = ExceptionDispatchInfo.Capture(error);
            }
            finally
            {
                done.Set();
            }
        });

        done.Wait();
        failure?.Throw();
    }

    public override SynchronizationContext CreateCopy() => this;
}
