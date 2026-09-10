using Plumix.Foundation;
using Plumix.UI;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/scheduler/binding.dart
// (mirrors flutter/packages/flutter/test/scheduler/{scheduler,binding,performance_mode,animation,
// time_dilation,debug,ticker}_test.dart)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SchedulerBindingTests
{
    [Fact]
    public void ScheduleTask_RunsInDescendingPriorityOrderOneTaskPerEventLoopTurn()
    {
        Scheduler.ResetForTests();
        try
        {
            List<int> executedTasks = [];
            int allowedPriority = 10000;
            Scheduler.SchedulingStrategy = priority => priority >= allowedPriority;

            foreach (int priority in new[] { 2, 23, 23, 11, 0, 80, 3 })
            {
                int captured = priority;
                Scheduler.ScheduleTask(
                    () =>
                    {
                        executedTasks.Add(captured);
                        return captured;
                    },
                    Priority.Idle + captured);
            }

            // Nothing runs while the strategy rejects the head of the queue, and the callback keeps
            // reporting that there is still work.
            Assert.True(Scheduler.HandleEventLoopCallback());
            Assert.True(Scheduler.HandleEventLoopCallback());
            Assert.Empty(executedTasks);

            allowedPriority = 20;
            for (int index = 0; index < 3; index++)
            {
                Assert.True(Scheduler.HandleEventLoopCallback());
            }

            Assert.Equal([80, 23, 23], executedTasks);

            executedTasks.Clear();
            allowedPriority = 0;
            for (int index = 0; index < 3; index++)
            {
                Assert.True(Scheduler.HandleEventLoopCallback());
            }

            // The queue drains on the last task, which is what reports false.
            Assert.False(Scheduler.HandleEventLoopCallback());
            Assert.Equal([11, 3, 2, 0], executedTasks);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ScheduleTask_LaterHigherPriorityTasksPreemptQueuedOnes()
    {
        Scheduler.ResetForTests();
        try
        {
            List<int> executedTasks = [];
            int allowedPriority = 100;
            Scheduler.SchedulingStrategy = priority => priority >= allowedPriority;

            foreach (int priority in new[] { 99, 19, 5, 97 })
            {
                int captured = priority;
                Scheduler.ScheduleTask(
                    () =>
                    {
                        executedTasks.Add(captured);
                        return captured;
                    },
                    Priority.Idle + captured);
            }

            allowedPriority = 20;
            for (int index = 0; index < 2; index++)
            {
                Assert.True(Scheduler.HandleEventLoopCallback());
            }

            Assert.Equal([99, 97], executedTasks);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public async Task ScheduleTask_ChainsAnAsynchronousTaskInsteadOfCompletingOnItsSynchronousPart()
    {
        Scheduler.ResetForTests();
        try
        {
            var inner = new TaskCompletionSource<int>();
            Task<int> scheduled = Scheduler.ScheduleTask(() => inner.Task, Priority.Animation);

            Assert.False(Scheduler.HandleEventLoopCallback());
            Assert.False(scheduled.IsCompleted);

            inner.SetResult(7);
            Assert.Equal(7, await scheduled);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ScheduleFrameCallback_CanRegisterWithoutSchedulingANewFrame()
    {
        Scheduler.ResetForTests();
        try
        {
            bool ran = false;
            Scheduler.ScheduleFrameCallback(_ => ran = true, scheduleNewFrame: false);
            Assert.False(Scheduler.HasScheduledFrame);

            Scheduler.HandleBeginFrame(TimeSpan.FromSeconds(1));
            Scheduler.HandleDrawFrame();
            Assert.True(ran);

            Scheduler.ScheduleFrameCallback(_ => { });
            Assert.True(Scheduler.HasScheduledFrame);
            Scheduler.HandleBeginFrame(TimeSpan.FromSeconds(2));
            Scheduler.HandleDrawFrame();
            Assert.False(Scheduler.HasScheduledFrame);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void TransientCallbacks_RunInRegistrationOrderShareOneTimestampAndAreOneShot()
    {
        Scheduler.ResetForTests();
        try
        {
            List<string> order = [];
            List<TimeSpan> stamps = [];
            int secondId = 0;

            Scheduler.ScheduleFrameCallback(timeStamp =>
            {
                order.Add("first");
                stamps.Add(timeStamp);

                // A callback cancelled by an earlier callback of the same frame must not run.
                Scheduler.CancelFrameCallbackWithId(secondId);
            });
            secondId = Scheduler.ScheduleFrameCallback(timeStamp =>
            {
                order.Add("second");
                stamps.Add(timeStamp);
            });
            int thirdId = Scheduler.ScheduleFrameCallback(timeStamp =>
            {
                order.Add("third");
                stamps.Add(timeStamp);
            });
            Assert.Equal(3, Scheduler.TransientCallbackCount);
            Assert.True(thirdId > secondId);

            Scheduler.HandleBeginFrame(TimeSpan.FromSeconds(1));
            Scheduler.HandleDrawFrame();

            Assert.Equal(["first", "third"], order);
            Assert.Equal(2, stamps.Count);
            Assert.Equal(stamps[0], stamps[1]);
            Assert.Equal(0, Scheduler.TransientCallbackCount);

            // One-shot: nothing runs on the next frame without a re-registration.
            order.Clear();
            Scheduler.HandleBeginFrame(TimeSpan.FromSeconds(2));
            Scheduler.HandleDrawFrame();
            Assert.Empty(order);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void FramePhases_FollowDartsOrderIncludingMidFrameMicrotasks()
    {
        Scheduler.ResetForTests();
        try
        {
            List<SchedulerPhase> phases = [];
            Scheduler.ScheduleFrameCallback(_ => phases.Add(Scheduler.Phase));
            Scheduler.AddPersistentFrameCallback(_ => phases.Add(Scheduler.Phase));
            Scheduler.AddPostFrameCallback(_ => phases.Add(Scheduler.Phase));

            Scheduler.HandleBeginFrame(TimeSpan.FromSeconds(1));
            Assert.Equal(SchedulerPhase.MidFrameMicrotasks, Scheduler.Phase);
            Scheduler.HandleDrawFrame();

            Assert.Equal(
                [
                    SchedulerPhase.TransientCallbacks,
                    SchedulerPhase.PersistentCallbacks,
                    SchedulerPhase.PostFrameCallbacks,
                ],
                phases);
            Assert.Equal(SchedulerPhase.Idle, Scheduler.Phase);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void PersistentCallback_AddedDuringThePersistentPhaseRunsOnTheNextFrameOnly()
    {
        Scheduler.ResetForTests();
        try
        {
            int addedRuns = 0;
            bool added = false;
            void Inner(TimeSpan _) => addedRuns++;
            Scheduler.AddPersistentFrameCallback(_ =>
            {
                if (!added)
                {
                    added = true;
                    Scheduler.AddPersistentFrameCallback(Inner);
                }
            });

            Scheduler.HandleBeginFrame(TimeSpan.FromSeconds(1));
            Scheduler.HandleDrawFrame();
            Assert.Equal(0, addedRuns);

            Scheduler.HandleBeginFrame(TimeSpan.FromSeconds(2));
            Scheduler.HandleDrawFrame();
            Assert.Equal(1, addedRuns);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void AddPostFrameCallback_DoesNotRequestAFrameAndCallbacksAddedMidPhaseWaitForTheNextOne()
    {
        Scheduler.ResetForTests();
        try
        {
            List<string> order = [];
            Scheduler.AddPostFrameCallback(_ =>
            {
                order.Add("first");
                Scheduler.AddPostFrameCallback(_ => order.Add("nested"));
            });
            Scheduler.AddPostFrameCallback(_ => order.Add("second"));

            // Dart's addPostFrameCallback never schedules a frame on its own.
            Assert.False(Scheduler.HasScheduledFrame);

            Scheduler.HandleBeginFrame(TimeSpan.FromSeconds(1));
            Scheduler.HandleDrawFrame();
            Assert.Equal(["first", "second"], order);

            Scheduler.HandleBeginFrame(TimeSpan.FromSeconds(2));
            Scheduler.HandleDrawFrame();
            Assert.Equal(["first", "second", "nested"], order);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void CurrentFrameTimeStamp_IsAdjustedForTheEpochAndTimeDilationWhileTheRawOneIsNot()
    {
        Scheduler.ResetForTests();
        try
        {
            List<TimeSpan> adjusted = [];
            List<TimeSpan> raw = [];
            void Record(TimeSpan timeStamp)
            {
                adjusted.Add(timeStamp);
                raw.Add(Scheduler.CurrentSystemFrameTimeStamp);
                Scheduler.ScheduleFrameCallback(Record);
            }

            Scheduler.ScheduleFrameCallback(Record);
            Scheduler.ResetEpoch();

            Tick(TimeSpan.FromSeconds(2));
            Tick(TimeSpan.FromSeconds(4));
            Assert.Equal([TimeSpan.Zero, TimeSpan.FromSeconds(2)], adjusted);
            Assert.Equal([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4)], raw);

            Scheduler.TimeDilation = 2.0;
            Tick(TimeSpan.FromSeconds(6));
            Tick(TimeSpan.FromSeconds(8));
            Assert.Equal(TimeSpan.FromSeconds(2), adjusted[2]);
            Assert.Equal(TimeSpan.FromSeconds(3), adjusted[3]);
            Assert.Equal(TimeSpan.FromSeconds(8), Scheduler.CurrentSystemFrameTimeStamp);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void CurrentFrameTimeStamp_IsOnlyReadableWhileAFrameIsBeingProduced()
    {
        Scheduler.ResetForTests();
        try
        {
            Assert.Throws<InvalidOperationException>(() => Scheduler.CurrentFrameTimeStamp);

            TimeSpan? seen = null;
            Scheduler.ScheduleFrameCallback(_ => seen = Scheduler.CurrentFrameTimeStamp);
            Tick(TimeSpan.FromSeconds(1));

            Assert.Equal(TimeSpan.Zero, seen);
            Assert.Throws<InvalidOperationException>(() => Scheduler.CurrentFrameTimeStamp);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void TimeDilation_DefaultsToOneAndRejectsNonPositiveValues()
    {
        Scheduler.ResetForTests();
        try
        {
            Assert.Equal(1.0, Scheduler.TimeDilation);
            Assert.Throws<ArgumentOutOfRangeException>(() => Scheduler.TimeDilation = 0.0);
            Assert.Throws<ArgumentOutOfRangeException>(() => Scheduler.TimeDilation = -1.0);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void DebugAssertNoTimeDilation_ThrowsWithTheGivenReasonOnlyWhileDilated()
    {
        Scheduler.ResetForTests();
        try
        {
            Assert.True(Scheduler.DebugAssertNoTimeDilation("reason"));

            Scheduler.TimeDilation = 2.0;
            FlutterError error = Assert.Throws<FlutterError>(() => Scheduler.DebugAssertNoTimeDilation("reason"));
            Assert.Equal("reason", error.Message);

            Scheduler.TimeDilation = 1.0;
            Assert.True(Scheduler.DebugAssertNoTimeDilation("reason"));
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void DebugAssertAllSchedulerVarsUnset_ChecksOnlyTheTwoBannerFlags()
    {
        try
        {
            Assert.True(SchedulerDebug.DebugAssertAllSchedulerVarsUnset("reason"));

            SchedulerDebug.DebugPrintScheduleFrameStacks = true;
            SchedulerDebug.DebugTracePostFrameCallbacks = true;
            Assert.True(SchedulerDebug.DebugAssertAllSchedulerVarsUnset("reason"));

            SchedulerDebug.DebugPrintBeginFrameBanner = true;
            Assert.Equal(
                "reason",
                Assert.Throws<FlutterError>(
                    () => SchedulerDebug.DebugAssertAllSchedulerVarsUnset("reason")).Message);
            SchedulerDebug.DebugPrintBeginFrameBanner = false;

            SchedulerDebug.DebugPrintEndFrameBanner = true;
            Assert.Throws<FlutterError>(() => SchedulerDebug.DebugAssertAllSchedulerVarsUnset("reason"));
        }
        finally
        {
            SchedulerDebug.DebugPrintBeginFrameBanner = false;
            SchedulerDebug.DebugPrintEndFrameBanner = false;
            SchedulerDebug.DebugPrintScheduleFrameStacks = false;
            SchedulerDebug.DebugTracePostFrameCallbacks = false;
        }
    }

    [Fact]
    public void DebugAssertNoTransientCallbacks_ReportsTheLeakInsteadOfThrowing()
    {
        Scheduler.ResetForTests();
        List<FlutterErrorDetails> reported = [];
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            Assert.True(Scheduler.DebugAssertNoTransientCallbacks("reason"));
            Assert.Empty(reported);

            Scheduler.ScheduleFrameCallback(_ => { });
            Assert.True(Scheduler.DebugAssertNoTransientCallbacks("reason"));
            Assert.Equal("reason", RequireException(Assert.Single(reported)).Message);
        }
        finally
        {
            FlutterError.OnError = previous;
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void PerformanceMode_RefCountsSameModeRequestsAndRejectsConflictingOnes()
    {
        Scheduler.ResetForTests();
        try
        {
            PerformanceModeRequestHandle? first = Scheduler.RequestPerformanceMode(DartPerformanceMode.Latency);
            Assert.NotNull(first);
            Assert.Equal(DartPerformanceMode.Latency, Scheduler.DebugGetRequestedPerformanceMode());

            Assert.Null(Scheduler.RequestPerformanceMode(DartPerformanceMode.Throughput));
            Assert.Equal(DartPerformanceMode.Latency, Scheduler.DebugGetRequestedPerformanceMode());

            PerformanceModeRequestHandle? second = Scheduler.RequestPerformanceMode(DartPerformanceMode.Latency);
            Assert.NotNull(second);

            first.Dispose();
            Assert.Equal(DartPerformanceMode.Latency, Scheduler.DebugGetRequestedPerformanceMode());

            second.Dispose();
            Assert.Null(Scheduler.DebugGetRequestedPerformanceMode());

            // Only the last release tells the runtime to go back to balanced.
            Assert.Equal(
                DartPerformanceMode.Balanced,
                PlatformDispatcher.Instance.LastRequestedPerformanceMode);
            Assert.Throws<InvalidOperationException>(second.Dispose);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void DebugAssertNoPendingPerformanceModeRequests_ThrowsWhileARequestIsOutstanding()
    {
        Scheduler.ResetForTests();
        try
        {
            Assert.True(Scheduler.DebugAssertNoPendingPerformanceModeRequests("reason"));

            PerformanceModeRequestHandle? handle = Scheduler.RequestPerformanceMode(DartPerformanceMode.Memory);
            Assert.Equal(
                "reason",
                Assert.Throws<FlutterError>(
                    () => Scheduler.DebugAssertNoPendingPerformanceModeRequests("reason")).Message);

            handle!.Dispose();
            Assert.True(Scheduler.DebugAssertNoPendingPerformanceModeRequests("reason"));
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void TimingsCallbacks_AreMultiplexedSkipRemovedOnesAndSurviveAThrowingCallback()
    {
        Scheduler.ResetForTests();
        List<FlutterErrorDetails> reported = [];
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            List<string> order = [];
            void Second(IReadOnlyList<FrameTiming> _) => order.Add("second");
            void First(IReadOnlyList<FrameTiming> _)
            {
                order.Add("first");
                Scheduler.RemoveTimingsCallback(Second);
                throw new InvalidOperationException("Test");
            }

            void Third(IReadOnlyList<FrameTiming> _) => order.Add("third");

            Scheduler.AddTimingsCallback(First);
            Scheduler.AddTimingsCallback(Second);
            Scheduler.AddTimingsCallback(Third);

            var timing = new FrameTiming(
                vsyncStart: 5000,
                buildStart: 10000,
                buildFinish: 15000,
                rasterStart: 16000,
                rasterFinish: 20000,
                rasterFinishWallTime: 20010,
                frameNumber: 1991);
            PlatformDispatcher.Instance.ReportTimings([timing]);

            // The callback removed by an earlier callback of the same dispatch is skipped, and one
            // throwing callback neither stops the rest nor escapes.
            Assert.Equal(["first", "third"], order);
            Assert.Equal("Test", RequireException(Assert.Single(reported)).Message);

            Assert.Equal(1991, timing.FrameNumber);
            Assert.Equal(10000, timing.TimestampInMicroseconds(FramePhase.BuildStart));
            Assert.Equal(TimeSpan.FromMilliseconds(5), timing.BuildDuration);
            Assert.Equal(TimeSpan.FromMilliseconds(4), timing.RasterDuration);
            Assert.Equal(TimeSpan.FromMilliseconds(5), timing.VsyncOverhead);
            Assert.Equal(TimeSpan.FromMilliseconds(15), timing.TotalSpan);

            Scheduler.RemoveTimingsCallback(First);
            Scheduler.RemoveTimingsCallback(Third);
            Assert.Null(PlatformDispatcher.Instance.OnReportTimings);
        }
        finally
        {
            FlutterError.OnError = previous;
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void FrameCallbackExceptions_AreReportedAndDoNotAbortTheRemainingCallbacks()
    {
        Scheduler.ResetForTests();
        List<FlutterErrorDetails> reported = [];
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            List<string> order = [];
            Scheduler.ScheduleFrameCallback(_ => throw new InvalidOperationException("transient"));
            Scheduler.ScheduleFrameCallback(_ => order.Add("survivor"));

            Scheduler.HandleBeginFrame(TimeSpan.FromSeconds(1));
            Scheduler.HandleDrawFrame();

            Assert.Equal(["survivor"], order);
            FlutterErrorDetails details = Assert.Single(reported);
            Assert.Equal("transient", RequireException(details).Message);
            Assert.Equal("scheduler library", details.Library);
        }
        finally
        {
            FlutterError.OnError = previous;
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ScheduleForcedFrame_WiresTheHostFrameCallbacksAndIgnoresDisabledFrames()
    {
        Scheduler.ResetForTests();
        try
        {
            Scheduler.HandleAppLifecycleStateChanged(AppLifecycleState.Paused);
            Assert.False(Scheduler.FramesEnabled);

            Scheduler.ScheduleFrame();
            Assert.False(Scheduler.HasScheduledFrame);
            Assert.Null(PlatformDispatcher.Instance.OnBeginFrame);

            Scheduler.ScheduleForcedFrame();
            Assert.True(Scheduler.HasScheduledFrame);
            Assert.NotNull(PlatformDispatcher.Instance.OnBeginFrame);
            Assert.NotNull(PlatformDispatcher.Instance.OnDrawFrame);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void AppLifecycle_EnablesFramesForResumedAndInactiveAndDisablesThemOtherwise()
    {
        Scheduler.ResetForTests();
        try
        {
            Assert.Null(Scheduler.LifecycleState);

            Scheduler.HandleAppLifecycleStateChanged(AppLifecycleState.Paused);
            Assert.False(Scheduler.FramesEnabled);
            Assert.Equal(AppLifecycleState.Paused, Scheduler.LifecycleState);

            Scheduler.HandleAppLifecycleStateChanged(AppLifecycleState.Hidden);
            Assert.False(Scheduler.FramesEnabled);

            // Re-enabling frames also asks for one.
            Scheduler.HandleAppLifecycleStateChanged(AppLifecycleState.Inactive);
            Assert.True(Scheduler.FramesEnabled);
            Assert.True(Scheduler.HasScheduledFrame);

            Scheduler.HandleAppLifecycleStateChanged(AppLifecycleState.Detached);
            Assert.False(Scheduler.FramesEnabled);

            Scheduler.ResetInternalState();
            Assert.Null(Scheduler.LifecycleState);
            Assert.True(Scheduler.FramesEnabled);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Ticker_StopsTickingWhilePausedAndResumesAfterTheAppIsResumed()
    {
        Scheduler.ResetForTests();
        var ticker = new Ticker(_ => { });
        try
        {
            Scheduler.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
            ticker.Start();
            Assert.True(ticker.IsTicking);

            Scheduler.HandleAppLifecycleStateChanged(AppLifecycleState.Paused);
            Assert.False(ticker.IsTicking);
            Assert.True(ticker.IsActive);

            Scheduler.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
            Assert.True(ticker.IsTicking);
        }
        finally
        {
            ticker.Dispose();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Ticker_ForceFramesRequestsAFrameEvenWhileFramesAreDisabled()
    {
        Scheduler.ResetForTests();
        var ticker = new Ticker(_ => { }) { ForceFrames = true };
        try
        {
            Scheduler.HandleAppLifecycleStateChanged(AppLifecycleState.Paused);
            Assert.False(Scheduler.HasScheduledFrame);

            ticker.Start();
            Assert.True(Scheduler.HasScheduledFrame);
        }
        finally
        {
            ticker.Dispose();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Ticker_ElapsedTimeFollowsTimeDilation()
    {
        Scheduler.ResetForTests();
        var elapsed = new List<TimeSpan>();
        var ticker = new Ticker(elapsed.Add);
        try
        {
            Scheduler.TimeDilation = 2.0;
            ticker.Start();

            Tick(TimeSpan.FromSeconds(1));
            Tick(TimeSpan.FromSeconds(1.01));
            Tick(TimeSpan.FromSeconds(1.02));

            // The first frame primes the start time; each 10 ms raw step is then 5 ms of ticker time.
            Assert.Equal(TimeSpan.Zero, elapsed[0]);
            Assert.Equal(TimeSpan.FromMilliseconds(5), elapsed[1]);
            Assert.Equal(TimeSpan.FromMilliseconds(10), elapsed[2]);
        }
        finally
        {
            ticker.Dispose();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ScheduleWarmUpFrame_RunsBeginAndDrawOnSeparateTurnsAndLocksEventsUntilTheFrameEnds()
    {
        Scheduler.ResetForTests();
        List<Action> turns = [];
        Action<Action> previousTimerRun = PlatformDispatcher.Instance.TimerRun;
        PlatformDispatcher.Instance.TimerRun = turns.Add;
        try
        {
            List<string> order = [];
            Scheduler.AddPersistentFrameCallback(_ => order.Add("draw"));
            Scheduler.ScheduleFrameCallback(_ =>
            {
                order.Add("begin");
                Scheduler.ScheduleMicrotask(() => order.Add("microtask"));
            });

            Scheduler.ScheduleWarmUpFrame();

            // Two turns, one for the begin half and one for the draw half. A second call while the
            // warm-up frame is in flight adds none.
            Assert.Equal(2, turns.Count);
            Scheduler.ScheduleWarmUpFrame();
            Assert.Equal(2, turns.Count);

            // Events are locked, so a scheduled task queues without arming a turn of its own.
            bool taskRan = false;
            Assert.True(Scheduler.Locked);
            Scheduler.ScheduleTask(() => taskRan = true, Priority.Animation);
            Assert.Equal(2, turns.Count);

            turns[0]();
            Assert.Equal(["begin"], order);

            turns[1]();
            Assert.Equal(["begin", "microtask", "draw"], order);
            Assert.False(Scheduler.Locked);

            // Unlocking re-arms the queue that stayed unserviced while locked.
            Assert.False(taskRan);
            Assert.Equal(3, turns.Count);
            turns[2]();
            Assert.True(taskRan);
        }
        finally
        {
            PlatformDispatcher.Instance.TimerRun = previousTimerRun;
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ScheduleWarmUpFrame_SwallowsAHostFrameThatArrivesMidWarmUpAndReschedulesIt()
    {
        Scheduler.ResetForTests();
        List<Action> turns = [];
        Action<Action> previousTimerRun = PlatformDispatcher.Instance.TimerRun;
        PlatformDispatcher.Instance.TimerRun = turns.Add;
        try
        {
            int frames = 0;
            Scheduler.AddPersistentFrameCallback(_ => frames++);
            Scheduler.EnsureFrameCallbacksRegistered();
            Scheduler.ScheduleWarmUpFrame();

            turns[0]();

            // A host frame delivered between the two halves of the warm-up frame is dropped: neither
            // half of it reaches the pipeline.
            PlatformDispatcher.Instance.OnBeginFrame!(TimeSpan.FromSeconds(1));
            PlatformDispatcher.Instance.OnDrawFrame!();
            Assert.Equal(0, frames);

            // The drop installs a post-frame callback that asks for the dropped frame again; it runs
            // in the warm-up frame's own post-frame phase.
            turns[1]();
            Assert.Equal(1, frames);
            Assert.True(Scheduler.HasScheduledFrame);
        }
        finally
        {
            PlatformDispatcher.Instance.TimerRun = previousTimerRun;
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void EndOfFrame_CompletesOncePerFrameAndSharesOneCompleterBetweenAwaiters()
    {
        Scheduler.ResetForTests();
        try
        {
            Task first = Scheduler.EndOfFrame;
            Task second = Scheduler.EndOfFrame;
            Assert.Same(first, second);

            // Reading it while idle asks for the frame that will complete it.
            Assert.True(Scheduler.HasScheduledFrame);
            Assert.False(first.IsCompleted);

            Scheduler.HandleBeginFrame(TimeSpan.FromSeconds(1));
            Scheduler.HandleDrawFrame();
            Assert.True(first.IsCompletedSuccessfully);

            Task third = Scheduler.EndOfFrame;
            Assert.NotSame(first, third);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    private static Exception RequireException(FlutterErrorDetails details)
    {
        return Assert.IsAssignableFrom<Exception>(details.Exception);
    }

    private static void Tick(TimeSpan rawTimeStamp)
    {
        Scheduler.HandleBeginFrame(rawTimeStamp);
        Scheduler.HandleDrawFrame();
    }
}
