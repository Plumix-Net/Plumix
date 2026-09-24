using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.UI;
using Xunit;

// Covers src/Plumix/Foundation/DartStopwatch.cs (dart:core `Stopwatch` semantics) and the
// `SamplingClock.stopwatch()` seam that flutter/packages/flutter/lib/src/gestures/velocity_tracker.dart
// reads through `GestureBinding.instance.samplingClock`.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class DartStopwatchTests
{
    [Fact]
    public void NewStopwatch_IsStoppedAtZero()
    {
        TimeSpan now = TimeSpan.FromSeconds(10);
        var stopwatch = new DartStopwatch(() => now);

        now += TimeSpan.FromSeconds(1);

        Assert.False(stopwatch.IsRunning);
        Assert.Equal(TimeSpan.Zero, stopwatch.Elapsed);
    }

    [Fact]
    public void StartAndStop_DoNotCountTheTimeSpentStopped()
    {
        TimeSpan now = TimeSpan.Zero;
        var stopwatch = new DartStopwatch(() => now);

        stopwatch.Start();
        now += TimeSpan.FromMilliseconds(30);
        stopwatch.Stop();
        now += TimeSpan.FromMilliseconds(500);
        Assert.Equal(30, stopwatch.ElapsedMilliseconds);

        stopwatch.Start();
        stopwatch.Start();
        now += TimeSpan.FromMilliseconds(20);
        Assert.True(stopwatch.IsRunning);
        Assert.Equal(50, stopwatch.ElapsedMilliseconds);
        Assert.Equal(50000, stopwatch.ElapsedMicroseconds);
    }

    [Fact]
    public void Reset_ZeroesTheTimeWithoutChangingTheRunningState()
    {
        TimeSpan now = TimeSpan.Zero;
        var stopwatch = new DartStopwatch(() => now);
        stopwatch.Start();
        now += TimeSpan.FromMilliseconds(40);

        stopwatch.Reset();
        Assert.True(stopwatch.IsRunning);
        Assert.Equal(TimeSpan.Zero, stopwatch.Elapsed);
        now += TimeSpan.FromMilliseconds(5);
        Assert.Equal(5, stopwatch.ElapsedMilliseconds);

        stopwatch.Stop();
        now += TimeSpan.FromMilliseconds(5);
        stopwatch.Reset();
        Assert.False(stopwatch.IsRunning);
        Assert.Equal(TimeSpan.Zero, stopwatch.Elapsed);
    }

    [Fact]
    public void VelocityTracker_MeasuresTheSampleAgeOnTheBindingSamplingClock()
    {
        var clock = new ManualSamplingClock();
        SamplingClock previous = GestureBinding.Instance.SamplingClock;
        GestureBinding.Instance.SamplingClock = clock;
        try
        {
            var tracker = new VelocityTracker(PointerDeviceKind.Touch);
            DateTime origin = FrameworkDartTester.EventTimeOrigin;
            for (int i = 0; i < 10; i += 1)
            {
                tracker.AddPosition(origin.AddMilliseconds(8 * i), new Point(0, 10 * i));
            }

            // However long the machine takes, the sample is fresh until the sampling clock moves.
            Assert.True(tracker.GetVelocity().PixelsPerSecond.Y > 1000.0);

            clock.Time += TimeSpan.FromMilliseconds(41);
            Assert.Equal(Velocity.Zero, tracker.GetVelocity());
        }
        finally
        {
            GestureBinding.Instance.SamplingClock = previous;
        }
    }

    private sealed class ManualSamplingClock : SamplingClock
    {
        public TimeSpan Time { get; set; }

        public override DartStopwatch Stopwatch() => new(() => Time);
    }
}
