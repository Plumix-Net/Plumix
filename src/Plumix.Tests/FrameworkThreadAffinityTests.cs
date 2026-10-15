using Xunit;

// C#-only infrastructure; no Dart parity source. Dart gets these guarantees from the isolate's
// single event loop: every `Future` continuation is a microtask on the thread that owns the trees.

namespace Plumix.Tests;

/// <summary>
/// Covers <see cref="EventLoop.EnterFrameworkThread"/>, <see cref="EventLoop.RunAsync"/> and the
/// <c>FrameworkSynchronizationContext</c> they install: framework <c>async</c> work must resume on
/// the framework thread, at an event-loop turn, and never on a thread-pool thread.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class FrameworkThreadAffinityTests
{
    [Fact]
    public void RunAsync_ResumesTheContinuationOnTheStartingThreadAtTheNextDrain()
    {
        EventLoop.FlushMicrotasks();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int startThread = Environment.CurrentManagedThreadId;
        int resumedThread = 0;
        bool resumed = false;

        EventLoop.RunAsync(async () =>
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
        EventLoop.FlushMicrotasks();
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int startThread = Environment.CurrentManagedThreadId;
        List<int> threads = [];

        EventLoop.RunAsync(async () =>
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
        EventLoop.FlushMicrotasks();
        bool ranSynchronously = false;

        EventLoop.RunAsync(async () =>
        {
            ranSynchronously = true;
            await Task.CompletedTask;
        });

        Assert.True(ranSynchronously);
    }

    [Fact]
    public void RunAsync_RejectsANullBody()
    {
        Assert.Throws<ArgumentNullException>(() => EventLoop.RunAsync(null!));
    }

    [Fact]
    public void EnterFrameworkThread_ClaimsTheThreadAndRestoresThePreviousContext()
    {
        SynchronizationContext? outer = SynchronizationContext.Current;
        using (EventLoop.EnterFrameworkThread())
        {
            Assert.True(EventLoop.IsFrameworkThread);
            Assert.NotNull(SynchronizationContext.Current);
            Assert.NotSame(outer, SynchronizationContext.Current);

            SynchronizationContext inner = SynchronizationContext.Current!;
            using (EventLoop.EnterFrameworkThread())
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
        EventLoop.FlushMicrotasks();
        using EventLoop.FrameworkThreadScope scope = EventLoop.EnterFrameworkThread();
        SynchronizationContext context = SynchronizationContext.Current!;

        bool posted = false;
        context.Post(_ => posted = true, null);
        Assert.False(posted);
        EventLoop.FlushMicrotasks();
        Assert.True(posted);

        bool sent = false;
        context.Send(_ => sent = true, null);
        Assert.True(sent);
    }

    [Fact]
    public void FrameworkThread_IsClaimedForTheWholeFramePipeline()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            List<bool> observed = [];
            SchedulerBinding.Instance.ScheduleFrameCallback(_ => observed.Add(EventLoop.IsFrameworkThread));
            SchedulerBinding.Instance.AddPostFrameCallback(_ => observed.Add(EventLoop.IsFrameworkThread));
            SchedulerBinding.Instance.ScheduleFrame();
            SchedulerBinding.PumpFrameForTests();

            Assert.Equal([true, true], observed);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }
}
