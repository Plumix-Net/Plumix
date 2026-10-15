using System.Globalization;
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
    // scheduler_test.dart: "Tasks are executed in the right order".
    [Fact]
    public void ScheduleTask_RunsTasksInTheRightOrder()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            SchedulerBinding scheduler = SchedulerBinding.Instance;
            List<int> executedTasks = [];
            int allowedPriority = 10000;
            List<SchedulerBinding> strategySchedulers = [];
            scheduler.SchedulingStrategy = (priority, binding) =>
            {
                strategySchedulers.Add(binding);
                return priority >= allowedPriority;
            };

            void ScheduleAddingTask(int x)
            {
                scheduler.ScheduleTask(
                    () =>
                    {
                        executedTasks.Add(x);
                        return x;
                    },
                    Priority.Idle + x);
            }

            void RunTurns(int count, bool expectedResult = true)
            {
                for (int index = 0; index < count; index++)
                {
                    Assert.Equal(expectedResult, scheduler.HandleEventLoopCallback());
                }
            }

            foreach (int x in new[] { 2, 23, 23, 11, 0, 80, 3 })
            {
                ScheduleAddingTask(x);
            }

            allowedPriority = 100;
            RunTurns(3);
            Assert.Empty(executedTasks);

            allowedPriority = 50;
            RunTurns(3);
            Assert.Equal([80], executedTasks);
            executedTasks.Clear();

            allowedPriority = 20;
            RunTurns(3);
            Assert.Equal([23, 23], executedTasks);
            executedTasks.Clear();

            foreach (int x in new[] { 99, 19, 5, 97 })
            {
                ScheduleAddingTask(x);
            }

            allowedPriority = 20;
            RunTurns(3);
            Assert.Equal([99, 97], executedTasks);
            executedTasks.Clear();

            allowedPriority = 10;
            RunTurns(3);
            Assert.Equal([19, 11], executedTasks);
            executedTasks.Clear();

            // A head the strategy turns away still reports that work remains.
            allowedPriority = 1;
            RunTurns(4);
            Assert.Equal([5, 3, 2], executedTasks);
            executedTasks.Clear();

            allowedPriority = 0;
            RunTurns(1, expectedResult: false);
            Assert.Equal([0], executedTasks);

            // The strategy receives the binding that consults it.
            Assert.All(strategySchedulers, binding => Assert.Same(scheduler, binding));
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public async Task ScheduleTask_ChainsAnAsynchronousTaskInsteadOfCompletingOnItsSynchronousPart()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            var inner = new TaskCompletionSource<int>();
            Task<int> scheduled = SchedulerBinding.Instance.ScheduleTask(() => inner.Task, Priority.Animation);

            Assert.False(SchedulerBinding.Instance.HandleEventLoopCallback());
            Assert.False(scheduled.IsCompleted);

            inner.SetResult(7);
            Assert.Equal(7, await scheduled);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void ScheduleFrameCallback_CanRegisterWithoutSchedulingANewFrame()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            bool ran = false;
            SchedulerBinding.Instance.ScheduleFrameCallback(_ => ran = true, scheduleNewFrame: false);
            Assert.False(SchedulerBinding.Instance.HasScheduledFrame);

            SchedulerBinding.Instance.HandleBeginFrame(TimeSpan.FromSeconds(1));
            SchedulerBinding.Instance.HandleDrawFrame();
            Assert.True(ran);

            SchedulerBinding.Instance.ScheduleFrameCallback(_ => { });
            Assert.True(SchedulerBinding.Instance.HasScheduledFrame);
            SchedulerBinding.Instance.HandleBeginFrame(TimeSpan.FromSeconds(2));
            SchedulerBinding.Instance.HandleDrawFrame();
            Assert.False(SchedulerBinding.Instance.HasScheduledFrame);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void TransientCallbacks_RunInRegistrationOrderShareOneTimestampAndAreOneShot()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            List<string> order = [];
            List<TimeSpan> stamps = [];
            int secondId = 0;

            SchedulerBinding.Instance.ScheduleFrameCallback(timeStamp =>
            {
                order.Add("first");
                stamps.Add(timeStamp);

                // A callback cancelled by an earlier callback of the same frame must not run.
                SchedulerBinding.Instance.CancelFrameCallbackWithId(secondId);
            });
            secondId = SchedulerBinding.Instance.ScheduleFrameCallback(timeStamp =>
            {
                order.Add("second");
                stamps.Add(timeStamp);
            });
            int thirdId = SchedulerBinding.Instance.ScheduleFrameCallback(timeStamp =>
            {
                order.Add("third");
                stamps.Add(timeStamp);
            });
            Assert.Equal(3, SchedulerBinding.Instance.TransientCallbackCount);
            Assert.True(thirdId > secondId);

            SchedulerBinding.Instance.HandleBeginFrame(TimeSpan.FromSeconds(1));
            SchedulerBinding.Instance.HandleDrawFrame();

            Assert.Equal(["first", "third"], order);
            Assert.Equal(2, stamps.Count);
            Assert.Equal(stamps[0], stamps[1]);
            Assert.Equal(0, SchedulerBinding.Instance.TransientCallbackCount);

            // One-shot: nothing runs on the next frame without a re-registration.
            order.Clear();
            SchedulerBinding.Instance.HandleBeginFrame(TimeSpan.FromSeconds(2));
            SchedulerBinding.Instance.HandleDrawFrame();
            Assert.Empty(order);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void FramePhases_FollowDartsOrderIncludingMidFrameMicrotasks()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            List<SchedulerPhase> phases = [];
            SchedulerBinding.Instance.ScheduleFrameCallback(_ => phases.Add(SchedulerBinding.Instance.SchedulerPhase));
            SchedulerBinding.Instance.AddPersistentFrameCallback(
                _ => phases.Add(SchedulerBinding.Instance.SchedulerPhase));
            SchedulerBinding.Instance.AddPostFrameCallback(_ => phases.Add(SchedulerBinding.Instance.SchedulerPhase));

            SchedulerBinding.Instance.HandleBeginFrame(TimeSpan.FromSeconds(1));
            Assert.Equal(SchedulerPhase.MidFrameMicrotasks, SchedulerBinding.Instance.SchedulerPhase);
            SchedulerBinding.Instance.HandleDrawFrame();

            Assert.Equal(
                [
                    SchedulerPhase.TransientCallbacks,
                    SchedulerPhase.PersistentCallbacks,
                    SchedulerPhase.PostFrameCallbacks,
                ],
                phases);
            Assert.Equal(SchedulerPhase.Idle, SchedulerBinding.Instance.SchedulerPhase);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void PersistentCallback_AddedDuringThePersistentPhaseRunsOnTheNextFrameOnly()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            int addedRuns = 0;
            bool added = false;
            void Inner(TimeSpan _) => addedRuns++;
            SchedulerBinding.Instance.AddPersistentFrameCallback(_ =>
            {
                if (!added)
                {
                    added = true;
                    SchedulerBinding.Instance.AddPersistentFrameCallback(Inner);
                }
            });

            SchedulerBinding.Instance.HandleBeginFrame(TimeSpan.FromSeconds(1));
            SchedulerBinding.Instance.HandleDrawFrame();
            Assert.Equal(0, addedRuns);

            SchedulerBinding.Instance.HandleBeginFrame(TimeSpan.FromSeconds(2));
            SchedulerBinding.Instance.HandleDrawFrame();
            Assert.Equal(1, addedRuns);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void AddPostFrameCallback_DoesNotRequestAFrameAndCallbacksAddedMidPhaseWaitForTheNextOne()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            List<string> order = [];
            SchedulerBinding.Instance.AddPostFrameCallback(_ =>
            {
                order.Add("first");
                SchedulerBinding.Instance.AddPostFrameCallback(_ => order.Add("nested"));
            });
            SchedulerBinding.Instance.AddPostFrameCallback(_ => order.Add("second"));

            // Dart's addPostFrameCallback never schedules a frame on its own.
            Assert.False(SchedulerBinding.Instance.HasScheduledFrame);

            SchedulerBinding.Instance.HandleBeginFrame(TimeSpan.FromSeconds(1));
            SchedulerBinding.Instance.HandleDrawFrame();
            Assert.Equal(["first", "second"], order);

            SchedulerBinding.Instance.HandleBeginFrame(TimeSpan.FromSeconds(2));
            SchedulerBinding.Instance.HandleDrawFrame();
            Assert.Equal(["first", "second", "nested"], order);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void CurrentFrameTimeStamp_IsAdjustedForTheEpochAndTimeDilationWhileTheRawOneIsNot()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            List<TimeSpan> adjusted = [];
            List<TimeSpan> raw = [];
            void Record(TimeSpan timeStamp)
            {
                adjusted.Add(timeStamp);
                raw.Add(SchedulerBinding.Instance.CurrentSystemFrameTimeStamp);
                SchedulerBinding.Instance.ScheduleFrameCallback(Record);
            }

            SchedulerBinding.Instance.ScheduleFrameCallback(Record);
            SchedulerBinding.Instance.ResetEpoch();

            Tick(TimeSpan.FromSeconds(2));
            Tick(TimeSpan.FromSeconds(4));
            Assert.Equal([TimeSpan.Zero, TimeSpan.FromSeconds(2)], adjusted);
            Assert.Equal([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4)], raw);

            Scheduler.TimeDilation = 2.0;
            Tick(TimeSpan.FromSeconds(6));
            Tick(TimeSpan.FromSeconds(8));
            Assert.Equal(TimeSpan.FromSeconds(2), adjusted[2]);
            Assert.Equal(TimeSpan.FromSeconds(3), adjusted[3]);
            Assert.Equal(TimeSpan.FromSeconds(8), SchedulerBinding.Instance.CurrentSystemFrameTimeStamp);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    // Dart asserts that a frame is in progress before the null check.
    [DebugOnlyFact]
    public void CurrentFrameTimeStamp_IsOnlyReadableWhileAFrameIsBeingProduced()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            Assert.Throws<AssertionError>(() => SchedulerBinding.Instance.CurrentFrameTimeStamp);

            TimeSpan? seen = null;
            SchedulerBinding.Instance.ScheduleFrameCallback(
                _ => seen = SchedulerBinding.Instance.CurrentFrameTimeStamp);
            Tick(TimeSpan.FromSeconds(1));

            Assert.Equal(TimeSpan.Zero, seen);
            Assert.Throws<AssertionError>(() => SchedulerBinding.Instance.CurrentFrameTimeStamp);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    // binding.dart: `assert(value > 0.0)`.
    [DebugOnlyFact]
    public void TimeDilation_DefaultsToOneAndRejectsNonPositiveValues()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            Assert.Equal(1.0, Scheduler.TimeDilation);
            Assert.Throws<AssertionError>(() => Scheduler.TimeDilation = 0.0);
            Assert.Throws<AssertionError>(() => Scheduler.TimeDilation = -1.0);
            Assert.Equal(1.0, Scheduler.TimeDilation);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void DebugAssertNoTimeDilation_ThrowsWithTheGivenReasonOnlyWhileDilated()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            Assert.True(SchedulerBinding.Instance.DebugAssertNoTimeDilation("reason"));

            Scheduler.TimeDilation = 2.0;
            FlutterError error = Assert.Throws<FlutterError>(
                () => SchedulerBinding.Instance.DebugAssertNoTimeDilation("reason"));
            Assert.Equal("reason", error.Message);

            Scheduler.TimeDilation = 1.0;
            Assert.True(SchedulerBinding.Instance.DebugAssertNoTimeDilation("reason"));
        }
        finally
        {
            SchedulerBinding.ResetForTests();
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
        SchedulerBinding.ResetForTests();
        List<FlutterErrorDetails> reported = [];
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            Assert.True(SchedulerBinding.Instance.DebugAssertNoTransientCallbacks("reason"));
            Assert.Empty(reported);

            SchedulerBinding.Instance.ScheduleFrameCallback(_ => { });
            Assert.True(SchedulerBinding.Instance.DebugAssertNoTransientCallbacks("reason"));
            // Dart reports the reason string itself as the exception.
            FlutterErrorDetails details = Assert.Single(reported);
            Assert.Equal("reason", details.Exception);
            Assert.Equal("scheduler library", details.Library);
        }
        finally
        {
            FlutterError.OnError = previous;
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void PerformanceMode_RefCountsSameModeRequestsAndRejectsConflictingOnes()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            PerformanceModeRequestHandle? first =
                SchedulerBinding.Instance.RequestPerformanceMode(DartPerformanceMode.Latency);
            Assert.NotNull(first);
            Assert.Equal(DartPerformanceMode.Latency, SchedulerBinding.Instance.DebugGetRequestedPerformanceMode());

            Assert.Null(SchedulerBinding.Instance.RequestPerformanceMode(DartPerformanceMode.Throughput));
            Assert.Equal(DartPerformanceMode.Latency, SchedulerBinding.Instance.DebugGetRequestedPerformanceMode());

            PerformanceModeRequestHandle? second =
                SchedulerBinding.Instance.RequestPerformanceMode(DartPerformanceMode.Latency);
            Assert.NotNull(second);

            first.Dispose();
            Assert.Equal(DartPerformanceMode.Latency, SchedulerBinding.Instance.DebugGetRequestedPerformanceMode());

            second.Dispose();
            Assert.Null(SchedulerBinding.Instance.DebugGetRequestedPerformanceMode());

            // Only the last release tells the runtime to go back to balanced.
            Assert.Equal(
                DartPerformanceMode.Balanced,
                PlatformDispatcher.Instance.LastRequestedPerformanceMode);
            // Dart asserts against a second dispose, and its release build fails the null check.
            Assert.Throws(
                Constants.KDebugMode ? typeof(AssertionError) : typeof(InvalidOperationException),
                second.Dispose);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void DebugAssertNoPendingPerformanceModeRequests_ThrowsWhileARequestIsOutstanding()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            Assert.True(SchedulerBinding.Instance.DebugAssertNoPendingPerformanceModeRequests("reason"));

            PerformanceModeRequestHandle? handle =
                SchedulerBinding.Instance.RequestPerformanceMode(DartPerformanceMode.Memory);
            Assert.Equal(
                "reason",
                Assert.Throws<FlutterError>(
                    () => SchedulerBinding.Instance.DebugAssertNoPendingPerformanceModeRequests("reason")).Message);

            handle!.Dispose();
            Assert.True(SchedulerBinding.Instance.DebugAssertNoPendingPerformanceModeRequests("reason"));
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void TimingsCallbacks_AreMultiplexedSkipRemovedOnesAndSurviveAThrowingCallback()
    {
        SchedulerBinding.ResetForTests();
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
                SchedulerBinding.Instance.RemoveTimingsCallback(Second);
                throw new InvalidOperationException("Test");
            }

            void Third(IReadOnlyList<FrameTiming> _) => order.Add("third");

            SchedulerBinding.Instance.AddTimingsCallback(First);
            SchedulerBinding.Instance.AddTimingsCallback(Second);
            SchedulerBinding.Instance.AddTimingsCallback(Third);

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

            SchedulerBinding.Instance.RemoveTimingsCallback(First);
            SchedulerBinding.Instance.RemoveTimingsCallback(Third);
            // The binding's own `Flutter.Frame` profiler keeps the platform slot wired outside release.
            Assert.Equal(Constants.KReleaseMode, PlatformDispatcher.Instance.OnReportTimings is null);
        }
        finally
        {
            FlutterError.OnError = previous;
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void FrameCallbackExceptions_AreReportedAndDoNotAbortTheRemainingCallbacks()
    {
        SchedulerBinding.ResetForTests();
        List<FlutterErrorDetails> reported = [];
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            List<string> order = [];
            SchedulerBinding.Instance.ScheduleFrameCallback(_ => throw new InvalidOperationException("transient"));
            SchedulerBinding.Instance.ScheduleFrameCallback(_ => order.Add("survivor"));

            SchedulerBinding.Instance.HandleBeginFrame(TimeSpan.FromSeconds(1));
            SchedulerBinding.Instance.HandleDrawFrame();

            Assert.Equal(["survivor"], order);
            FlutterErrorDetails details = Assert.Single(reported);
            Assert.Equal("transient", RequireException(details).Message);
            Assert.Equal("scheduler library", details.Library);
        }
        finally
        {
            FlutterError.OnError = previous;
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void ScheduleForcedFrame_WiresTheHostFrameCallbacksAndIgnoresDisabledFrames()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            SchedulerBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Paused);
            Assert.False(SchedulerBinding.Instance.FramesEnabled);

            SchedulerBinding.Instance.ScheduleFrame();
            Assert.False(SchedulerBinding.Instance.HasScheduledFrame);
            Assert.Null(PlatformDispatcher.Instance.OnBeginFrame);

            SchedulerBinding.Instance.ScheduleForcedFrame();
            Assert.True(SchedulerBinding.Instance.HasScheduledFrame);
            Assert.NotNull(PlatformDispatcher.Instance.OnBeginFrame);
            Assert.NotNull(PlatformDispatcher.Instance.OnDrawFrame);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void AppLifecycle_EnablesFramesForResumedAndInactiveAndDisablesThemOtherwise()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            Assert.Null(SchedulerBinding.Instance.LifecycleState);

            SchedulerBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Paused);
            Assert.False(SchedulerBinding.Instance.FramesEnabled);
            Assert.Equal(AppLifecycleState.Paused, SchedulerBinding.Instance.LifecycleState);

            SchedulerBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Hidden);
            Assert.False(SchedulerBinding.Instance.FramesEnabled);

            // Re-enabling frames also asks for one.
            SchedulerBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Inactive);
            Assert.True(SchedulerBinding.Instance.FramesEnabled);
            Assert.True(SchedulerBinding.Instance.HasScheduledFrame);

            SchedulerBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Detached);
            Assert.False(SchedulerBinding.Instance.FramesEnabled);

            SchedulerBinding.Instance.ResetInternalState();
            Assert.Null(SchedulerBinding.Instance.LifecycleState);
            Assert.True(SchedulerBinding.Instance.FramesEnabled);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void Ticker_StopsTickingWhilePausedAndResumesAfterTheAppIsResumed()
    {
        SchedulerBinding.ResetForTests();
        var ticker = new Ticker(_ => { });
        try
        {
            SchedulerBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
            ticker.Start();
            Assert.True(ticker.IsTicking);

            SchedulerBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Paused);
            Assert.False(ticker.IsTicking);
            Assert.True(ticker.IsActive);

            SchedulerBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
            Assert.True(ticker.IsTicking);
        }
        finally
        {
            ticker.Dispose();
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void Ticker_ForceFramesRequestsAFrameEvenWhileFramesAreDisabled()
    {
        SchedulerBinding.ResetForTests();
        var ticker = new Ticker(_ => { }) { ForceFrames = true };
        try
        {
            SchedulerBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Paused);
            Assert.False(SchedulerBinding.Instance.HasScheduledFrame);

            ticker.Start();
            Assert.True(SchedulerBinding.Instance.HasScheduledFrame);
        }
        finally
        {
            ticker.Dispose();
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void Ticker_ElapsedTimeFollowsTimeDilation()
    {
        SchedulerBinding.ResetForTests();
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
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void ScheduleWarmUpFrame_RunsBeginAndDrawOnSeparateTurnsAndLocksEventsUntilTheFrameEnds()
    {
        SchedulerBinding.ResetForTests();
        List<Action> turns = [];
        Action<Action> previousTimerRun = PlatformDispatcher.Instance.TimerRun;
        PlatformDispatcher.Instance.TimerRun = turns.Add;
        try
        {
            List<string> order = [];
            SchedulerBinding.Instance.AddPersistentFrameCallback(_ => order.Add("draw"));
            SchedulerBinding.Instance.ScheduleFrameCallback(_ =>
            {
                order.Add("begin");
                EventLoop.ScheduleMicrotask(() => order.Add("microtask"));
            });

            SchedulerBinding.Instance.ScheduleWarmUpFrame();

            // Two turns, one for the begin half and one for the draw half. A second call while the
            // warm-up frame is in flight adds none.
            Assert.Equal(2, turns.Count);
            SchedulerBinding.Instance.ScheduleWarmUpFrame();
            Assert.Equal(2, turns.Count);

            // Events are locked, so a scheduled task queues without arming a turn of its own.
            bool taskRan = false;
            Assert.True(SchedulerBinding.Instance.Locked);
            SchedulerBinding.Instance.ScheduleTask(() => taskRan = true, Priority.Animation);
            Assert.Equal(2, turns.Count);

            // The begin turn ends like any event-loop turn, by draining the microtasks it queued.
            turns[0]();
            Assert.Equal(["begin", "microtask"], order);

            turns[1]();
            Assert.Equal(["begin", "microtask", "draw"], order);
            Assert.False(SchedulerBinding.Instance.Locked);

            // Unlocking re-arms the queue that stayed unserviced while locked.
            Assert.False(taskRan);
            Assert.Equal(3, turns.Count);
            turns[2]();
            Assert.True(taskRan);
        }
        finally
        {
            PlatformDispatcher.Instance.TimerRun = previousTimerRun;
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void ScheduleWarmUpFrame_SwallowsAHostFrameThatArrivesMidWarmUpAndReschedulesIt()
    {
        SchedulerBinding.ResetForTests();
        List<Action> turns = [];
        Action<Action> previousTimerRun = PlatformDispatcher.Instance.TimerRun;
        PlatformDispatcher.Instance.TimerRun = turns.Add;
        try
        {
            int frames = 0;
            SchedulerBinding.Instance.AddPersistentFrameCallback(_ => frames++);
            SchedulerBinding.Instance.EnsureFrameCallbacksRegistered();
            SchedulerBinding.Instance.ScheduleWarmUpFrame();

            turns[0]();

            // A host frame delivered between the two halves of the warm-up frame is dropped: neither
            // half of it reaches the pipeline.
            PlatformDispatcher.Instance.OnBeginFrame!(TimeSpan.FromSeconds(1));
            Assert.False(SchedulerBinding.Instance.HasScheduledFrame);
            PlatformDispatcher.Instance.OnDrawFrame!();
            Assert.False(SchedulerBinding.Instance.HasScheduledFrame);
            Assert.Equal(0, frames);

            // The drop installs a post-frame callback that asks for the dropped frame again; it runs
            // in the warm-up frame's own post-frame phase.
            turns[1]();
            Assert.Equal(1, frames);
            Assert.True(SchedulerBinding.Instance.HasScheduledFrame);
        }
        finally
        {
            PlatformDispatcher.Instance.TimerRun = previousTimerRun;
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void EndOfFrame_CompletesOncePerFrameAndSharesOneCompleterBetweenAwaiters()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            Task first = SchedulerBinding.Instance.EndOfFrame;
            Task second = SchedulerBinding.Instance.EndOfFrame;
            Assert.Same(first, second);

            // Reading it while idle asks for the frame that will complete it.
            Assert.True(SchedulerBinding.Instance.HasScheduledFrame);
            Assert.False(first.IsCompleted);

            SchedulerBinding.Instance.HandleBeginFrame(TimeSpan.FromSeconds(1));
            SchedulerBinding.Instance.HandleDrawFrame();
            Assert.True(first.IsCompletedSuccessfully);

            Task third = SchedulerBinding.Instance.EndOfFrame;
            Assert.NotSame(first, third);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    // binding_test.dart: "Flutter.Frame event fired".
    [Fact]
    public void ReportedTimings_PostAFlutterFrameEventPerTiming()
    {
        SchedulerBinding.ResetForTests();
        List<(string Kind, IReadOnlyDictionary<string, object?> Data)> events = [];
        void OnEvent(string kind, IReadOnlyDictionary<string, object?> data) => events.Add((kind, data));

        BindingBase.EventPosted += OnEvent;
        try
        {
            PlatformDispatcher.Instance.OnReportTimings!(
            [
                new FrameTiming(
                    vsyncStart: 5000,
                    buildStart: 10000,
                    buildFinish: 15000,
                    rasterStart: 16000,
                    rasterFinish: 20000,
                    rasterFinishWallTime: 20010,
                    frameNumber: 1991),
            ]);

            (string kind, IReadOnlyDictionary<string, object?> data) = Assert.Single(events);
            Assert.Equal("Flutter.Frame", kind);
            Assert.Equal(1991L, Convert.ToInt64(data["number"], CultureInfo.InvariantCulture));
            Assert.Equal(10000L, Convert.ToInt64(data["startTime"], CultureInfo.InvariantCulture));
            Assert.Equal(15000L, Convert.ToInt64(data["elapsed"], CultureInfo.InvariantCulture));
            Assert.Equal(5000L, Convert.ToInt64(data["build"], CultureInfo.InvariantCulture));
            Assert.Equal(4000L, Convert.ToInt64(data["raster"], CultureInfo.InvariantCulture));
            Assert.Equal(5000L, Convert.ToInt64(data["vsyncOverhead"], CultureInfo.InvariantCulture));
        }
        finally
        {
            BindingBase.EventPosted -= OnEvent;
            SchedulerBinding.ResetForTests();
        }
    }

    // performance_mode_test.dart: "PerformanceModeRequestHandle dispatches memory events".
    [Fact]
    public void PerformanceModeRequestHandle_DispatchesMemoryEvents()
    {
        SchedulerBinding.ResetForTests();
        List<ObjectEvent> events = [];
        FlutterMemoryAllocations.Instance.AddListener(events.Add);
        try
        {
            PerformanceModeRequestHandle handle =
                SchedulerBinding.Instance.RequestPerformanceMode(DartPerformanceMode.Latency)!;
            handle.Dispose();

            if (Constants.KDebugMode)
            {
                Assert.Collection(
                    events,
                    @event =>
                    {
                        ObjectCreated created = Assert.IsType<ObjectCreated>(@event);
                        Assert.Same(handle, created.Object);
                        Assert.Equal("package:flutter/scheduler.dart", created.Library);
                        Assert.Equal("PerformanceModeRequestHandle", created.ClassName);
                    },
                    @event => Assert.Same(handle, Assert.IsType<ObjectDisposed>(@event).Object));
            }
            else
            {
                Assert.Empty(events);
            }
        }
        finally
        {
            FlutterMemoryAllocations.Instance.RemoveListener(events.Add);
            SchedulerBinding.ResetForTests();
        }
    }

    // foundation/binding.dart `lockEvents`: the lock is taken before the callback runs, the
    // callback's own task is returned, and the release (with `unlocked`) runs as a microtask.
    [Fact]
    public async Task LockEvents_LocksBeforeTheCallbackAndUnlocksInAMicrotaskAfterIt()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            SchedulerBinding scheduler = SchedulerBinding.Instance;
            bool lockedInsideCallback = false;
            var gate = new TaskCompletionSource();
            Task callbackTask = gate.Task;
            Task returned = scheduler.LockEvents(() =>
            {
                lockedInsideCallback = scheduler.Locked;
                return callbackTask;
            });

            Assert.True(lockedInsideCallback);
            Assert.Same(callbackTask, returned);

            gate.SetResult();
            Assert.True(scheduler.Locked);
            EventLoop.FlushMicrotasks();
            Assert.False(scheduler.Locked);

            // A faulted callback task reaches the caller instead of being swallowed.
            Task faulted = scheduler.LockEvents(() => Task.FromException(new InvalidOperationException("lock")));
            await Assert.ThrowsAsync<InvalidOperationException>(() => faulted);
            EventLoop.FlushMicrotasks();
            Assert.False(scheduler.Locked);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    // binding.dart `scheduleFrame`/`scheduleForcedFrame`: the debugPrintScheduleFrameStacks label
    // names the Dart method and the phase the way Dart interpolates an enum.
    [DebugOnlyFact]
    public void ScheduleFrame_PrintsTheDartLabelWhenScheduleFrameStacksAreTraced()
    {
        SchedulerBinding.ResetForTests();
        List<string?> printed = [];
        DebugPrintCallback previousPrint = Print.DebugPrint;
        Print.DebugPrint = (message, _) => printed.Add(message);
        SchedulerDebug.DebugPrintScheduleFrameStacks = true;
        try
        {
            SchedulerBinding.Instance.ScheduleFrame();
            Assert.Equal("scheduleFrame() called. Current phase is SchedulerPhase.idle.", printed[0]);

            SchedulerBinding.ResetForTests();
            printed.Clear();
            SchedulerBinding.Instance.ScheduleForcedFrame();
            Assert.Equal("scheduleForcedFrame() called. Current phase is SchedulerPhase.idle.", printed[0]);
        }
        finally
        {
            SchedulerDebug.DebugPrintScheduleFrameStacks = false;
            Print.DebugPrint = previousPrint;
            SchedulerBinding.ResetForTests();
        }
    }

    // binding.dart reports scheduler-callback and task errors with the stack they were thrown on.
    [Fact]
    public void CallbackAndTaskErrors_CarryTheThrownStack()
    {
        SchedulerBinding.ResetForTests();
        List<FlutterErrorDetails> reported = [];
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            SchedulerBinding.Instance.ScheduleFrameCallback(_ => throw new InvalidOperationException("frame"));
            Tick(TimeSpan.FromSeconds(1));
            Func<int> throwingTask = () => throw new InvalidOperationException("task");
            _ = SchedulerBinding.Instance.ScheduleTask(throwingTask, Priority.Touch);
            SchedulerBinding.Instance.HandleEventLoopCallback();

            Assert.Equal(2, reported.Count);
            Assert.All(reported, details =>
            {
                Assert.Equal("scheduler library", details.Library);
                Assert.False(string.IsNullOrEmpty(details.Stack));
            });
            Assert.Equal("during a scheduler callback", reported[0].Context!.ToString());
            Assert.Equal("during a task callback", reported[1].Context!.ToString());
        }
        finally
        {
            FlutterError.OnError = previous;
            SchedulerBinding.ResetForTests();
        }
    }

    // service_extensions.dart: the extension is registered under its Dart enum name.
    [Fact]
    public void SchedulerServiceExtensions_UseTheDartNames()
    {
        Assert.Equal("timeDilation", SchedulerServiceExtensions.TimeDilation.DartName());
    }

    private static Exception RequireException(FlutterErrorDetails details)
    {
        return Assert.IsAssignableFrom<Exception>(details.Exception);
    }

    private static void Tick(TimeSpan rawTimeStamp)
    {
        SchedulerBinding.Instance.HandleBeginFrame(rawTimeStamp);
        SchedulerBinding.Instance.HandleDrawFrame();
    }
}
