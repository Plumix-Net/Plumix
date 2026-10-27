using Xunit;

// C#-only test infrastructure checks for Plumix's port of dart:async's `Timer`. The expected behavior
// is the Dart SDK's `Timer` contract as flutter_test's `FakeAsync` (fake_async 1.3.3) implements it.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class TimerTests
{
    [Fact]
    public void OneShotTimer_FiresOnceAtItsDeadlineAndThenIsInactive()
    {
        using var time = new FakeGestureTimers();
        int fired = 0;
        bool activeInCallback = true;
        Timer? timer = null;
        timer = new Timer(TimeSpan.FromMilliseconds(100), () =>
        {
            fired++;
            activeInCallback = timer!.IsActive;
        });

        Assert.True(timer.IsActive);
        Assert.Equal(0, timer.Tick);
        time.Elapse(TimeSpan.FromMilliseconds(99));
        Assert.Equal(0, fired);
        time.Elapse(TimeSpan.FromMilliseconds(1));
        Assert.Equal(1, fired);
        Assert.False(activeInCallback);
        Assert.False(timer.IsActive);
        Assert.Equal(1, timer.Tick);
        time.Elapse(TimeSpan.FromSeconds(1));
        Assert.Equal(1, fired);
    }

    [Fact]
    public void CancelledTimer_NeverFires()
    {
        using var time = new FakeGestureTimers();
        bool fired = false;
        var timer = new Timer(TimeSpan.FromMilliseconds(10), () => fired = true);

        timer.Cancel();
        timer.Cancel();
        time.Elapse(TimeSpan.FromSeconds(1));

        Assert.False(fired);
        Assert.False(timer.IsActive);
        Assert.Equal(0, time.PendingTimerCount);
    }

    [Fact]
    public void NegativeDuration_IsTreatedAsZero_AndRunFiresOnTheNextElapse()
    {
        using var time = new FakeGestureTimers();
        var log = new List<string>();
        _ = new Timer(TimeSpan.FromSeconds(-1), () => log.Add("negative"));
        Timer.Run(() => log.Add("run"));

        Assert.Empty(log);
        time.Elapse(TimeSpan.Zero);
        Assert.Equal(["negative", "run"], log);
    }

    [Fact]
    public void Elapse_FiresInDeadlineOrderThenCreationOrder_WithMicrotasksDrainedBetween()
    {
        using var time = new FakeGestureTimers();
        var log = new List<string>();
        _ = new Timer(TimeSpan.FromMilliseconds(30), () => log.Add("c"));
        _ = new Timer(TimeSpan.FromMilliseconds(10), () =>
        {
            log.Add("a");
            EventLoop.ScheduleMicrotask(() => log.Add("a-microtask"));
        });
        _ = new Timer(TimeSpan.FromMilliseconds(10), () => log.Add("b"));

        time.Elapse(TimeSpan.FromMilliseconds(30));

        Assert.Equal(["a", "a-microtask", "b", "c"], log);
    }

    [Fact]
    public void TimerCreatedByACallback_FiresInTheSameElapseWhenDue()
    {
        using var time = new FakeGestureTimers();
        var log = new List<string>();
        _ = new Timer(TimeSpan.FromMilliseconds(10), () =>
        {
            log.Add("outer");
            _ = new Timer(TimeSpan.FromMilliseconds(10), () => log.Add("inner"));
        });

        time.Elapse(TimeSpan.FromMilliseconds(19));
        Assert.Equal(["outer"], log);
        time.Elapse(TimeSpan.FromMilliseconds(1));
        Assert.Equal(["outer", "inner"], log);
    }

    [Fact]
    public void PeriodicTimer_TicksEveryPeriodUntilItsCallbackCancelsIt()
    {
        using var time = new FakeGestureTimers();
        var ticks = new List<int>();
        Timer timer = Timer.Periodic(TimeSpan.FromMilliseconds(100), t =>
        {
            ticks.Add(t.Tick);
            Assert.True(t.IsActive);
            if (t.Tick == 3)
            {
                t.Cancel();
            }
        });

        time.Elapse(TimeSpan.FromMilliseconds(250));
        Assert.Equal([1, 2], ticks);
        Assert.True(timer.IsActive);
        time.Elapse(TimeSpan.FromSeconds(1));
        Assert.Equal([1, 2, 3], ticks);
        Assert.False(timer.IsActive);
    }

    [Fact]
    public void NestedFakeTime_SharesOneClock_AndItsEndDropsPendingTimers()
    {
        bool fired = false;
        Timer timer;
        using (var outer = new FakeGestureTimers())
        {
            using (var inner = new FakeGestureTimers())
            {
                timer = new Timer(TimeSpan.FromMilliseconds(20), () => fired = true);
                inner.Elapse(TimeSpan.FromMilliseconds(10));
            }

            Assert.True(timer.IsActive);
            outer.Elapse(TimeSpan.FromMilliseconds(10));
            Assert.True(fired);

            timer = new Timer(TimeSpan.FromMilliseconds(20), () => throw new InvalidOperationException());
        }

        Assert.False(timer.IsActive);
    }

    [Fact]
    public void TimerCreatedOutsideFakeTime_IsNotFiredByIt()
    {
        bool fired = false;
        var real = new Timer(TimeSpan.Zero, () => fired = true);
        try
        {
            using var time = new FakeGestureTimers();
            time.Elapse(TimeSpan.FromSeconds(1));

            Assert.False(fired);
            Assert.True(real.IsActive);
        }
        finally
        {
            real.Cancel();
        }
    }

    [Fact]
    public void Elapse_RejectsNegativeDurations()
    {
        using var time = new FakeGestureTimers();
        Assert.Throws<ArgumentOutOfRangeException>(() => time.Elapse(TimeSpan.FromTicks(-1)));
    }
}
