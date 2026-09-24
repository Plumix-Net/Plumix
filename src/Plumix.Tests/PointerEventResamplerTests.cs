using Avalonia;
using Plumix.Gestures;
using Plumix.UI;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/gestures/resampler_test.dart

namespace Plumix.Tests;

public sealed class PointerEventResamplerTests
{
    [Fact]
    public void InterpolatesMovesAndFlushesRemainingEvents()
    {
        var resampler = new PointerEventResampler();
        DateTime start = DateTime.UnixEpoch;
        resampler.AddEvent(new PointerAddedEvent(7, PointerDeviceKind.Touch, default, timestampUtc: start));
        resampler.AddEvent(Down(start.AddMilliseconds(10)));
        resampler.AddEvent(Move(10, start.AddMilliseconds(20)));
        resampler.AddEvent(Move(20, start.AddMilliseconds(30)));
        var events = new List<PointerEvent>();

        resampler.Sample(start.AddMilliseconds(10), start.AddMilliseconds(20), events.Add);
        Assert.Collection(events,
            @event => Assert.IsType<PointerAddedEvent>(@event),
            @event => Assert.IsType<PointerDownEvent>(@event));

        resampler.Sample(start.AddMilliseconds(20), start.AddMilliseconds(30), events.Add);
        resampler.Sample(start.AddMilliseconds(25), start.AddMilliseconds(35), events.Add);
        Assert.Equal(10.0, Assert.IsType<PointerMoveEvent>(events[2]).Position.X);
        Assert.Equal(15.0, Assert.IsType<PointerMoveEvent>(events[3]).Position.X);
        Assert.Equal(5.0, events[3].Delta.X);
        Assert.True(resampler.HasPendingEvents);

        resampler.Stop(events.Add);
        Assert.Equal(20.0, Assert.IsType<PointerMoveEvent>(events[4]).Position.X);
        Assert.False(resampler.HasPendingEvents);
        Assert.False(resampler.IsDown);
    }

    [Fact]
    public void QuickTapEmitsEveryStateAtTheSampleTime()
    {
        var resampler = new PointerEventResampler();
        DateTime start = DateTime.UnixEpoch;
        resampler.AddEvent(new PointerAddedEvent(7, PointerDeviceKind.Touch, default, timestampUtc: start));
        resampler.AddEvent(Down(start));
        resampler.AddEvent(new PointerUpEvent(
            7, PointerDeviceKind.Touch, default, PointerButtons.None, start));
        resampler.AddEvent(new PointerRemovedEvent(7, PointerDeviceKind.Touch, default, timestampUtc: start));
        var events = new List<PointerEvent>();

        DateTime sampleTime = start.AddMilliseconds(1.5);
        resampler.Sample(sampleTime, sampleTime, events.Add);

        Assert.Collection(events,
            @event => Assert.IsType<PointerAddedEvent>(@event),
            @event => Assert.IsType<PointerDownEvent>(@event),
            @event => Assert.IsType<PointerUpEvent>(@event),
            @event => Assert.IsType<PointerRemovedEvent>(@event));
        Assert.All(events, @event => Assert.Equal(sampleTime, @event.TimestampUtc));
        Assert.False(resampler.HasPendingEvents);
        Assert.False(resampler.IsTracked);
    }

    [Fact]
    public void NextSampleTimePullsUpForwardWithInterpolatedPosition()
    {
        var resampler = new PointerEventResampler();
        DateTime start = DateTime.UnixEpoch;
        resampler.AddEvent(new PointerAddedEvent(
            7, PointerDeviceKind.Touch, default, timestampUtc: start.AddMilliseconds(1)));
        resampler.AddEvent(Down(start.AddMilliseconds(1)));
        resampler.AddEvent(Move(10, start.AddMilliseconds(2)));
        resampler.AddEvent(Move(20, start.AddMilliseconds(3)));
        resampler.AddEvent(new PointerUpEvent(
            7, PointerDeviceKind.Touch, new Point(20, 0),
            PointerButtons.None, start.AddMilliseconds(3)));
        var events = new List<PointerEvent>();

        resampler.Sample(start.AddMilliseconds(0.5), start.AddMilliseconds(1.5), events.Add);
        Assert.Empty(events);
        resampler.Sample(start.AddMilliseconds(1.5), start.AddMilliseconds(2.5), events.Add);
        Assert.Equal(5.0, Assert.IsType<PointerDownEvent>(events[1]).Position.X);
        resampler.Sample(start.AddMilliseconds(2.5), start.AddMilliseconds(3.5), events.Add);
        Assert.Equal(15.0, Assert.IsType<PointerMoveEvent>(events[2]).Position.X);
        Assert.Equal(15.0, Assert.IsType<PointerUpEvent>(events[3]).Position.X);
        Assert.Equal(start.AddMilliseconds(2.5), events[3].TimestampUtc);
        Assert.False(resampler.IsDown);
    }

    [Fact]
    public void SkippedMoveBecomesSyntheticMoveBeforeUp()
    {
        var resampler = new PointerEventResampler();
        DateTime start = DateTime.UnixEpoch;
        resampler.AddEvent(new PointerAddedEvent(
            7, PointerDeviceKind.Touch, default, timestampUtc: start.AddMilliseconds(1)));
        resampler.AddEvent(Down(start.AddMilliseconds(2)));
        resampler.AddEvent(Move(10, start.AddMilliseconds(3)));
        resampler.AddEvent(new PointerUpEvent(
            7, PointerDeviceKind.Touch, new Point(10, 0),
            PointerButtons.None, start.AddMilliseconds(4)));
        var events = new List<PointerEvent>();

        resampler.Sample(start.AddMilliseconds(2), start.AddMilliseconds(3), events.Add);
        resampler.Sample(start.AddMilliseconds(5), start.AddMilliseconds(6), events.Add);

        Assert.Equal(4, events.Count);
        Assert.Equal(10.0, Assert.IsType<PointerMoveEvent>(events[2]).Delta.X);
        Assert.IsType<PointerUpEvent>(events[3]);
        Assert.All(events.Skip(2), @event => Assert.Equal(start.AddMilliseconds(5), @event.TimestampUtc));
    }

    private static PointerDownEvent Down(DateTime timestamp) =>
        new(7, PointerDeviceKind.Touch, default, PointerButtons.Primary, timestamp);

    private static PointerMoveEvent Move(double x, DateTime timestamp) =>
        new(7, PointerDeviceKind.Touch, new Point(x, 0), PointerButtons.Primary, timestamp);
}
