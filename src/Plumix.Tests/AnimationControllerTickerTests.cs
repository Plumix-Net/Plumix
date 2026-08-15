using Plumix.Physics;
using Xunit;

// Dart parity sources:
// flutter/packages/flutter/test/animation/animation_controller_test.dart
// flutter/packages/flutter/test/scheduler/ticker_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class AnimationControllerTickerTests : IDisposable
{
    private readonly long _baseTicks;

    public AnimationControllerTickerTests()
    {
        Scheduler.ResetForTests();
        _baseTicks = TimeSpan.FromSeconds(Scheduler.CurrentSeconds).Ticks;
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    /// <summary>
    /// Flutter's `tick(Duration)` helper: an absolute frame timestamp. The first tick after a ticker
    /// starts outside a frame reports zero elapsed time, which is the contract the tests below rely on.
    /// </summary>
    private void Tick(double seconds)
    {
        // Built from ticks so that the difference between two frame timestamps is exact; going through
        // TimeSpan.FromSeconds twice rounds each end independently.
        Scheduler.PumpFrameForTests(
            TimeSpan.FromTicks(_baseTicks + (long)Math.Round(seconds * TimeSpan.TicksPerSecond)));
    }

    [Fact]
    public void Ticker_StartAndStop_TrackActiveTickingAndResolveTheFuture()
    {
        int tickCount = 0;
        TimeSpan lastElapsed = TimeSpan.MinValue;
        using var ticker = new Ticker(elapsed =>
        {
            tickCount++;
            lastElapsed = elapsed;
        });

        Assert.False(ticker.IsTicking);
        Assert.False(ticker.IsActive);

        TickerFuture future = ticker.Start();
        Assert.True(ticker.IsTicking);
        Assert.True(ticker.IsActive);
        Assert.Equal(0, tickCount);
        Assert.Throws<InvalidOperationException>(() => ticker.Start());

        Tick(0.01);
        Assert.Equal(1, tickCount);
        Assert.Equal(TimeSpan.Zero, lastElapsed);

        Tick(0.02);
        Assert.Equal(2, tickCount);
        Assert.Equal(0.01, lastElapsed.TotalSeconds, 6);

        Assert.False(future.Task.IsCompleted);
        ticker.Stop();
        Assert.True(future.Task.IsCompleted);
        Assert.False(ticker.IsActive);
        Assert.False(ticker.IsTicking);
    }

    [Fact]
    public void Ticker_Muted_StopsCallbacksWhileTheClockKeepsRunning()
    {
        int tickCount = 0;
        TimeSpan lastElapsed = TimeSpan.MinValue;
        using var ticker = new Ticker(elapsed =>
        {
            tickCount++;
            lastElapsed = elapsed;
        });

        ticker.Start();
        Tick(0.01);
        Assert.Equal(1, tickCount);

        ticker.Muted = true;
        Tick(0.02);
        Assert.Equal(1, tickCount);
        Assert.False(ticker.IsTicking);
        Assert.True(ticker.IsActive);

        ticker.Muted = false;
        Tick(0.03);
        Assert.Equal(2, tickCount);
        Assert.True(ticker.IsTicking);
        // Time kept elapsing while the ticker was silenced.
        Assert.Equal(0.02, lastElapsed.TotalSeconds, 6);
    }

    [Fact]
    public void TickerFuture_Cancellation_NeverResolvesTheFutureAndFaultsOrCancel()
    {
        using var ticker = new Ticker(_ => { });
        TickerFuture future = ticker.Start();
        bool completedOrCanceled = false;
        future.WhenCompleteOrCancel(() => completedOrCanceled = true);

        ticker.Stop(canceled: true);

        Assert.False(future.Task.IsCompleted);
        Assert.True(future.OrCancel.IsFaulted);
        Assert.IsType<TickerCanceled>(future.OrCancel.Exception!.InnerException);

        // Dart resolves the callback in a microtask, not inside the call that canceled the ticker.
        Assert.False(completedOrCanceled);
        Scheduler.FlushMicrotasks();
        Assert.True(completedOrCanceled);
    }

    [Fact]
    public void TickerFuture_Dispose_CancelsAndCompletedFactoryIsAlreadyResolved()
    {
        var ticker = new Ticker(_ => { });
        TickerFuture future = ticker.Start();
        ticker.Dispose();

        Assert.False(future.Task.IsCompleted);
        Assert.True(future.OrCancel.IsFaulted);

        TickerFuture completed = TickerFuture.Completed();
        Assert.True(completed.Task.IsCompleted);
        Assert.True(completed.OrCancel.IsCompletedSuccessfully);
    }

    [Fact]
    public void Ticker_AbsorbTicker_KeepsTheFutureIdentityAndTheStartTime()
    {
        int firstTicks = 0;
        int secondTicks = 0;
        var first = new Ticker(_ => firstTicks++);
        using var second = new Ticker(_ => secondTicks++);

        TickerFuture future = first.Start();
        Tick(0.01);
        Assert.Equal(1, firstTicks);

        second.AbsorbTicker(first);
        Assert.True(second.IsActive);

        Tick(0.02);
        Assert.Equal(1, firstTicks);
        Assert.Equal(1, secondTicks);

        second.Stop();
        Assert.True(future.Task.IsCompleted);
    }

    [Fact]
    public void AnimationController_Defaults_MatchTheSourceConstructorSurface()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromSeconds(1));

        Assert.Equal(0.0, controller.LowerBound);
        Assert.Equal(1.0, controller.UpperBound);
        Assert.Equal(0.0, controller.Value);
        Assert.Equal(AnimationStatus.Dismissed, controller.Status);
        Assert.Equal(AnimationBehavior.Normal, controller.Behavior);
        Assert.Null(controller.LastElapsedDuration);
        Assert.False(controller.IsAnimating);
        Assert.False(controller.IsUnbounded);
        Assert.Same(controller, controller.View);

        using AnimationController unbounded = AnimationController.Unbounded(duration: TimeSpan.FromSeconds(1));
        Assert.True(double.IsNegativeInfinity(unbounded.LowerBound));
        Assert.True(double.IsPositiveInfinity(unbounded.UpperBound));
        Assert.Equal(AnimationBehavior.Preserve, unbounded.Behavior);
        Assert.True(unbounded.IsUnbounded);
    }

    [Fact]
    public void AnimationController_MissingDurations_ThrowWithTheSourceMessages()
    {
        using var controller = new AnimationController();

        Assert.Contains(
            "no default duration",
            Assert.Throws<InvalidOperationException>(() => controller.Forward()).Message);
        Assert.Contains(
            "no default duration or reverseDuration",
            Assert.Throws<InvalidOperationException>(() => controller.Reverse()).Message);
        Assert.Contains(
            "no explicit duration and no default duration",
            Assert.Throws<InvalidOperationException>(() => controller.AnimateTo(0.5)).Message);
        Assert.Contains(
            "no explicit duration and no default duration or reverseDuration",
            Assert.Throws<InvalidOperationException>(() => controller.AnimateBack(0.5)).Message);
        Assert.Contains(
            "without an explicit period",
            Assert.Throws<InvalidOperationException>(() => controller.Repeat()).Message);
    }

    [Fact]
    public void AnimationController_SettingValueDirectly_DerivesTheStatusFromTheValue()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromSeconds(1));
        List<AnimationStatus> statuses = [];
        controller.AddStatusListener(statuses.Add);

        controller.SetValue(0.0);
        Assert.Equal(AnimationStatus.Dismissed, controller.Status);
        controller.SetValue(0.5);
        Assert.Equal(AnimationStatus.Forward, controller.Status);
        controller.SetValue(1.0);
        Assert.Equal(AnimationStatus.Completed, controller.Status);
        controller.SetValue(0.5);
        Assert.Equal(AnimationStatus.Forward, controller.Status);
        controller.SetValue(0.0);
        Assert.Equal(AnimationStatus.Dismissed, controller.Status);

        Assert.Equal(
            [
                AnimationStatus.Forward,
                AnimationStatus.Completed,
                AnimationStatus.Forward,
                AnimationStatus.Dismissed,
            ],
            statuses);
    }

    [Fact]
    public void AnimationController_ForwardAndReverse_UseTheirOwnDurations()
    {
        using var controller = new AnimationController(
            duration: TimeSpan.FromMilliseconds(100),
            reverseDuration: TimeSpan.FromMilliseconds(50));

        controller.Forward();
        Tick(0.0);
        Tick(0.02);
        Assert.Equal(0.2, controller.Value, 3);
        Tick(0.05);
        Assert.Equal(0.5, controller.Value, 3);
        Tick(0.11);
        Assert.Equal(1.0, controller.Value, 3);
        Assert.Equal(AnimationStatus.Completed, controller.Status);

        controller.Reverse();
        Tick(0.11);
        Tick(0.13);
        Assert.Equal(0.6, controller.Value, 3);
        // `isDone` is a strict `>`, so a tick landing exactly on the duration still needs one more.
        Tick(0.16);
        Assert.Equal(0.0, controller.Value, 3);
        Tick(0.17);
        Assert.Equal(AnimationStatus.Dismissed, controller.Status);
    }

    [Fact]
    public void AnimationController_AnimateTo_ScalesTheDurationByTheRemainingFraction()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromMilliseconds(100));

        // Half the range, so half the default duration.
        controller.AnimateTo(0.5);
        Tick(0.0);
        Tick(0.025);
        Assert.Equal(0.25, controller.Value, 3);
        Tick(0.06);
        Assert.Equal(0.5, controller.Value, 3);
        Assert.Equal(AnimationStatus.Completed, controller.Status);
    }

    [Fact]
    public void AnimationController_AnimateToTheCurrentValue_CompletesWithoutAnimating()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromSeconds(1));
        List<AnimationStatus> statuses = [];
        controller.AddStatusListener(statuses.Add);
        controller.SetValue(0.5);
        statuses.Clear();

        TickerFuture future = controller.AnimateTo(0.5, TimeSpan.FromMilliseconds(100));

        Assert.True(future.Task.IsCompleted);
        Assert.Equal(0.5, controller.Value, 6);
        Assert.Equal(AnimationStatus.Completed, controller.Status);
        Assert.Equal([AnimationStatus.Completed], statuses);
        Assert.Equal(0, Scheduler.TransientCallbackCount);

        // Even a target at the lower bound reports `completed`, because the direction is forward.
        controller.SetValue(0.0);
        controller.AnimateTo(0.0, TimeSpan.FromMilliseconds(100));
        Assert.Equal(AnimationStatus.Completed, controller.Status);
    }

    [Fact]
    public void AnimationController_ZeroDurationAnimateTo_JumpsWithoutSchedulingAFrame()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromSeconds(1));

        controller.AnimateTo(1.0, TimeSpan.Zero);

        Assert.Equal(1.0, controller.Value, 6);
        Assert.Equal(AnimationStatus.Completed, controller.Status);
        Assert.Equal(0, Scheduler.TransientCallbackCount);
    }

    [Fact]
    public void AnimationController_AnimateTo_AlwaysReportsForwardThenCompleted()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromMilliseconds(100));
        List<AnimationStatus> statuses = [];
        controller.AddStatusListener(statuses.Add);

        controller.AnimateTo(1.0);
        Tick(0.0);
        Tick(0.2);
        Assert.Equal([AnimationStatus.Forward, AnimationStatus.Completed], statuses);

        statuses.Clear();
        controller.AnimateTo(0.5);
        Tick(0.2);
        Tick(0.4);
        Assert.Equal([AnimationStatus.Forward, AnimationStatus.Completed], statuses);
    }

    [Fact]
    public void AnimationController_Reset_ReportsDismissedFromEveryPhase()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromMilliseconds(100));
        List<AnimationStatus> statuses = [];
        controller.AddStatusListener(statuses.Add);

        controller.Reset();
        Assert.Empty(statuses);

        controller.Forward();
        Tick(0.0);
        Tick(0.05);
        controller.Reset();
        Assert.Equal([AnimationStatus.Forward, AnimationStatus.Dismissed], statuses);
        Assert.Equal(0.0, controller.Value, 6);
    }

    [Fact]
    public void AnimationController_LastElapsedDurationAndVelocity_ComeFromTheRunningSimulation()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromSeconds(1));

        Assert.Equal(0.0, controller.Velocity);
        controller.Forward();
        Tick(0.0);
        Assert.Equal(TimeSpan.Zero, controller.LastElapsedDuration);

        Tick(0.02);
        Assert.Equal(0.02, controller.LastElapsedDuration!.Value.TotalSeconds, 6);
        // A linear one-second run moves at one unit per second.
        Assert.InRange(controller.Velocity, 0.9, 1.1);

        Tick(1.1);
        Assert.Equal(AnimationStatus.Completed, controller.Status);
        Assert.Equal(0.0, controller.Velocity);
        Assert.Null(controller.LastElapsedDuration);
    }

    [Fact]
    public void AnimationController_VelocityAtTheStart_HalvesFromTheClampedCenteredDifference()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromSeconds(1));
        controller.Forward();
        Tick(0.0);

        // `_InterpolationSimulation.dx` samples symmetrically and `x` clamps at zero, so the very first
        // frame reports half the steady-state velocity. Flutter asserts the same 0.4-0.6 window.
        Assert.InRange(controller.Velocity, 0.4, 0.6);

        // A run that starts at the upper bound never starts a simulation at all.
        controller.Forward(from: 1.0);
        Assert.Equal(0.0, controller.Velocity);
    }

    [Fact]
    public void AnimationController_Fling_ReachesTheBoundsExactlyInBothDirections()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromSeconds(1));

        controller.Fling();
        Tick(0.0);
        for (int frame = 1; frame <= 60; frame++)
        {
            Tick(frame / 60.0);
        }

        Assert.Equal(1.0, controller.Value, 6);
        Assert.Equal(AnimationStatus.Completed, controller.Status);

        using var bounded = new AnimationController(
            duration: TimeSpan.FromSeconds(1),
            lowerBound: -30.0,
            upperBound: 45.0);
        bounded.Fling(velocity: -1.0);
        Tick(1.0);
        for (int frame = 1; frame <= 60; frame++)
        {
            Tick(1.0 + (frame / 60.0));
        }

        Assert.Equal(-30.0, bounded.Value, 6);
        Assert.Equal(AnimationStatus.Dismissed, bounded.Status);
    }

    [Fact]
    public void AnimationController_Fling_RejectsAnUnderdampedSpring()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromSeconds(1));

        Assert.Throws<ArgumentException>(() => controller.Fling(
            springDescription: SpringDescription.WithDampingRatio(mass: 1.0, stiffness: 500.0, ratio: 0.5)));
    }

    [Fact]
    public void AnimationController_Repeat_AlternatesDirectionAndKeepsThePhaseOfTheCurrentValue()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromMilliseconds(100));
        List<AnimationStatus> statuses = [];
        controller.AddStatusListener(statuses.Add);

        controller.Repeat(reverse: true);
        Tick(0.0);
        Tick(0.025);
        Assert.Equal(0.25, controller.Value, 3);
        Tick(0.125);
        Assert.Equal(0.75, controller.Value, 3);
        Assert.Contains(AnimationStatus.Reverse, statuses);

        // The starting value sets the phase.
        controller.Stop();
        controller.SetValue(0.5);
        controller.Repeat(reverse: true);
        Tick(0.2);
        Tick(0.25);
        Assert.Equal(1.0, controller.Value, 3);
    }

    [Fact]
    public void AnimationController_RepeatWithCount_StopsAfterTheRequestedPeriods()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromMilliseconds(100));

        Assert.Throws<ArgumentOutOfRangeException>(() => controller.Repeat(count: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => controller.Repeat(count: -1));

        controller.Repeat(count: 1);
        Tick(0.0);
        Tick(0.025);
        Assert.Equal(0.25, controller.Value, 3);
        Tick(0.099);
        Assert.Equal(0.99, controller.Value, 3);
        Tick(0.1);
        Assert.Equal(0.0, controller.Value, 3);
        Assert.False(controller.IsAnimating);
    }

    [Fact]
    public void AnimationController_RepeatWithMinAndMax_StaysInsideTheRequestedWindow()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromMilliseconds(100));

        controller.Repeat(min: 1.0, max: 1.0, reverse: true);
        Tick(0.0);
        Tick(0.05);
        Assert.Equal(1.0, controller.Value, 6);

        controller.Stop();
        controller.SetValue(0.2);
        controller.Repeat(min: 0.2, max: 0.6, reverse: true);
        Tick(0.1);
        Tick(0.15);
        Assert.Equal(0.4, controller.Value, 3);
    }

    [Fact]
    public void AnimationController_AnimateWith_RunsForwardAndAnimateBackWithRunsInReverse()
    {
        using var controller = AnimationController.Unbounded(duration: TimeSpan.FromSeconds(1));
        List<AnimationStatus> statuses = [];
        controller.AddStatusListener(statuses.Add);

        controller.AnimateWith(new IdentitySimulation());
        Tick(0.0);
        Tick(0.5);
        Assert.Equal(0.5, controller.Value, 3);
        Assert.Equal([AnimationStatus.Forward], statuses);

        statuses.Clear();
        controller.AnimateBackWith(new IdentitySimulation());
        Tick(0.5);
        Tick(1.0);
        Assert.Equal([AnimationStatus.Reverse], statuses);
    }

    [Fact]
    public void AnimationController_Toggle_FlipsWithTheCurrentDirection()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromMilliseconds(100));

        controller.Toggle();
        Tick(0.0);
        Tick(0.05);
        Assert.Equal(AnimationStatus.Forward, controller.Status);
        Assert.True(controller.Status.IsForwardOrCompleted());

        controller.Toggle();
        Assert.Equal(AnimationStatus.Reverse, controller.Status);
        Tick(0.05);
        Tick(0.15);
        Assert.Equal(0.0, controller.Value, 3);
        Assert.Equal(AnimationStatus.Dismissed, controller.Status);
    }

    [Fact]
    public void AnimationController_Stop_LeavesTheStatusAloneAndCancelsTheFuture()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromSeconds(1));
        TickerFuture future = controller.Forward();
        Tick(0.0);
        Tick(0.1);

        controller.Stop();

        Assert.Equal(AnimationStatus.Forward, controller.Status);
        Assert.False(controller.IsAnimating);
        Assert.False(future.Task.IsCompleted);
        Assert.True(future.OrCancel.IsFaulted);
        Assert.Null(controller.LastElapsedDuration);
    }

    [Fact]
    public void AnimationController_AnimationBehavior_ShortensNormalRunsWhenAnimationsAreDisabled()
    {
        AnimationController.DisableAnimations = true;
        try
        {
            using var preserve = new AnimationController(
                duration: TimeSpan.FromMilliseconds(100),
                animationBehavior: AnimationBehavior.Preserve);
            preserve.AnimateTo(1.0, TimeSpan.FromMilliseconds(100));
            Tick(0.0);
            Tick(0.05);
            Assert.Equal(0.5, preserve.Value, 3);

            using var normal = new AnimationController(duration: TimeSpan.FromMilliseconds(100));
            normal.AnimateTo(1.0, TimeSpan.FromMilliseconds(100));
            Tick(0.1);
            // The 0.05 scale makes the same request run twenty times faster.
            Tick(0.1025);
            Assert.Equal(0.5, normal.Value, 3);
        }
        finally
        {
            AnimationController.DisableAnimations = false;
        }
    }

    [Fact]
    public void AnimationController_Resync_KeepsTheRunningAnimationAndItsFuture()
    {
        var first = new TestTickerProvider();
        var second = new TestTickerProvider();
        using var controller = new AnimationController(duration: TimeSpan.FromSeconds(1), vsync: first);

        TickerFuture future = controller.Forward();
        Tick(0.0);
        Tick(0.1);
        Assert.Equal(0.1, controller.Value, 3);

        controller.Resync(second);
        Assert.True(controller.IsAnimating);

        Tick(0.2);
        Assert.Equal(0.2, controller.Value, 3);
        Assert.False(future.Task.IsCompleted);

        Tick(1.1);
        Assert.True(future.Task.IsCompleted);
        Assert.Equal(AnimationStatus.Completed, controller.Status);
    }

    [Fact]
    public void AnimationController_Dispose_CancelsTheFutureAndRejectsFurtherUse()
    {
        var controller = new AnimationController(duration: TimeSpan.FromSeconds(1));
        TickerFuture future = controller.Forward();
        Tick(0.0);

        controller.Dispose();

        Assert.False(future.Task.IsCompleted);
        Assert.True(future.OrCancel.IsFaulted);
        Assert.Throws<ObjectDisposedException>(() => controller.Forward());
        Assert.Throws<ObjectDisposedException>(() => controller.Reverse());
        Assert.Throws<ObjectDisposedException>(() => controller.AnimateTo(0.0));
        Assert.Throws<ObjectDisposedException>(() => controller.AnimateBack(0.0));
        Assert.Throws<ObjectDisposedException>(() => controller.AnimateWith(new IdentitySimulation()));
        Assert.Throws<ObjectDisposedException>(() => controller.Stop());
        Assert.Throws<ObjectDisposedException>(() => controller.Dispose());
    }

    [Fact]
    public void AnimationController_ToString_ReportsTheStatusGlyphValueAndLabel()
    {
        var controller = new AnimationController(duration: TimeSpan.FromSeconds(1), debugLabel: "probe");

        Assert.Equal("AnimationController(⏮ 0.000; paused; for probe)", controller.ToString());

        controller.Forward();
        Tick(0.0);
        Assert.Equal("AnimationController(▶ 0.000; for probe)", controller.ToString());

        controller.Dispose();
        Assert.Equal("AnimationController(▶ 0.000; paused; DISPOSED; for probe)", controller.ToString());
    }

    [Fact]
    public void AnimationController_SetValueFromAStatusCallback_IsSafe()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromMilliseconds(100));
        List<AnimationStatus> statuses = [];
        controller.AddStatusListener(status =>
        {
            statuses.Add(status);
            if (status == AnimationStatus.Completed)
            {
                controller.SetValue(0.0);
                controller.Forward();
            }
        });

        controller.Forward();
        Tick(0.0);
        Tick(0.11);

        Assert.Contains(AnimationStatus.Completed, statuses);
        Assert.Contains(AnimationStatus.Dismissed, statuses);
    }

    /// <summary>Flutter's `TestSimulation`: <c>x(t) == t</c>, never done.</summary>
    private sealed class IdentitySimulation : Simulation
    {
        public override double X(double time) => time;

        public override double DX(double time) => time;

        public override bool IsDone(double time) => false;
    }

    private sealed class TestTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(TickerCallback onTick) => new(onTick);
    }
}
