using Xunit;

// C#-only infrastructure; no Dart parity source. Dart gets these guarantees from the isolate's
// single event loop: every `Future` continuation is a microtask on the thread that owns the trees.

namespace Plumix.Tests;

/// <summary>
/// Covers <see cref="Scheduler.EnterFrameworkThread"/>, <see cref="Scheduler.RunAsync"/> and the
/// <c>FrameworkSynchronizationContext</c> they install: framework <c>async</c> work must resume on
/// the framework thread, at an event-loop turn, and never on a thread-pool thread.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class FrameworkThreadAffinityTests
{
    [Fact]
    public void RunAsync_ResumesTheContinuationOnTheStartingThreadAtTheNextDrain()
    {
        Scheduler.FlushMicrotasks();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int startThread = Environment.CurrentManagedThreadId;
        int resumedThread = 0;
        bool resumed = false;

        Scheduler.RunAsync(async () =>
        {
            await gate.Task;
            resumedThread = Environment.CurrentManagedThreadId;
            resumed = true;
        });

        Assert.False(resumed);
        gate.SetResult();

        // The pool thread that completes the gate only hands the continuation to the framework's
        // microtask queue; nothing runs until the queue is drained.
        Assert.True(EventLoopPump.SpinUntil(() => resumed));
        Assert.Equal(startThread, resumedThread);
    }

    [Fact]
    public void RunAsync_KeepsEveryFurtherAwaitOnTheFrameworkThread()
    {
        Scheduler.FlushMicrotasks();
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int startThread = Environment.CurrentManagedThreadId;
        List<int> threads = [];

        Scheduler.RunAsync(async () =>
        {
            await first.Task;
            threads.Add(Environment.CurrentManagedThreadId);
            await second.Task;
            threads.Add(Environment.CurrentManagedThreadId);
        });

        first.SetResult();
        Assert.True(EventLoopPump.SpinUntil(() => threads.Count == 1));
        second.SetResult();
        Assert.True(EventLoopPump.SpinUntil(() => threads.Count == 2));
        Assert.Equal([startThread, startThread], threads);
    }

    [Fact]
    public void RunAsync_RunsTheBodyUpToItsFirstAwaitSynchronously()
    {
        Scheduler.FlushMicrotasks();
        bool ranSynchronously = false;

        Scheduler.RunAsync(async () =>
        {
            ranSynchronously = true;
            await Task.CompletedTask;
        });

        Assert.True(ranSynchronously);
    }

    [Fact]
    public void RunAsync_RejectsANullBody()
    {
        Assert.Throws<ArgumentNullException>(() => Scheduler.RunAsync(null!));
    }

    [Fact]
    public void EnterFrameworkThread_ClaimsTheThreadAndRestoresThePreviousContext()
    {
        SynchronizationContext? outer = SynchronizationContext.Current;
        using (Scheduler.EnterFrameworkThread())
        {
            Assert.True(Scheduler.IsFrameworkThread);
            Assert.NotNull(SynchronizationContext.Current);
            Assert.NotSame(outer, SynchronizationContext.Current);

            SynchronizationContext inner = SynchronizationContext.Current!;
            using (Scheduler.EnterFrameworkThread())
            {
                // A nested scope is a no-op, so the context object stays the same one.
                Assert.Same(inner, SynchronizationContext.Current);
            }

            Assert.Same(inner, SynchronizationContext.Current);
        }

        Assert.Same(outer, SynchronizationContext.Current);
    }

    [Fact]
    public void FrameworkContext_PostsThroughTheMicrotaskQueueAndSendsInline()
    {
        Scheduler.FlushMicrotasks();
        using Scheduler.FrameworkThreadScope scope = Scheduler.EnterFrameworkThread();
        SynchronizationContext context = SynchronizationContext.Current!;

        bool posted = false;
        context.Post(_ => posted = true, null);
        Assert.False(posted);
        Scheduler.FlushMicrotasks();
        Assert.True(posted);

        bool sent = false;
        context.Send(_ => sent = true, null);
        Assert.True(sent);
    }

    [Fact]
    public void FrameworkThread_IsClaimedForTheWholeFramePipeline()
    {
        Scheduler.ResetForTests();
        try
        {
            List<bool> observed = [];
            Scheduler.ScheduleFrameCallback(_ => observed.Add(Scheduler.IsFrameworkThread));
            Scheduler.AddPostFrameCallback(_ => observed.Add(Scheduler.IsFrameworkThread));
            Scheduler.ScheduleFrame();
            Scheduler.PumpFrameForTests();

            Assert.Equal([true, true], observed);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }
}
