using Avalonia;
using Plumix.UI;
using Xunit;

// C#-only infrastructure (see src/Plumix/TrackpadPanZoomSynthesizer.cs): Flutter's engine reports the
// pan/zoom phase itself, so there is no Dart counterpart to these tests.

namespace Plumix.Tests;

/// <summary>
/// Covers the gesture phase <see cref="TrackpadPanZoomSynthesizer"/> rebuilds from the phase-less
/// trackpad deltas an Avalonia host receives.
/// </summary>
public sealed class TrackpadPanZoomSynthesizerTests
{
    [Fact]
    public void FirstDelta_OpensTheSequenceAndTheIdleTimeoutClosesIt()
    {
        using var timers = new FakeGestureTimers();
        var events = new List<PointerEvent>();
        var synthesizer = new TrackpadPanZoomSynthesizer(events.Add);

        synthesizer.Pan(new Point(10, 20), new Point(3, 4), DateTime.UnixEpoch);

        Assert.Collection(
            events,
            @event =>
            {
                var start = Assert.IsType<PointerPanZoomStartEvent>(@event);
                Assert.Equal(new Point(10, 20), start.Position);
            },
            @event =>
            {
                var update = Assert.IsType<PointerPanZoomUpdateEvent>(@event);
                Assert.Equal(new Point(3, 4), update.Pan);
                Assert.Equal(new Point(3, 4), update.PanDelta);
            });
        Assert.True(synthesizer.IsActive);

        timers.Elapse(TrackpadPanZoomSynthesizer.DefaultIdleTimeout);

        Assert.IsType<PointerPanZoomEndEvent>(events[^1]);
        Assert.False(synthesizer.IsActive);
    }

    [Fact]
    public void LaterDeltas_AccumulateIntoOneSequenceAndRestartTheIdleTimeout()
    {
        using var timers = new FakeGestureTimers();
        var events = new List<PointerEvent>();
        var synthesizer = new TrackpadPanZoomSynthesizer(events.Add);

        synthesizer.Pan(new Point(10, 20), new Point(3, 4), DateTime.UnixEpoch);
        timers.Elapse(TimeSpan.FromMilliseconds(60));
        synthesizer.Zoom(new Point(10, 20), 1.5, DateTime.UnixEpoch);
        timers.Elapse(TimeSpan.FromMilliseconds(60));
        synthesizer.Rotate(new Point(10, 20), 0.25, DateTime.UnixEpoch);

        // Three deltas 60 ms apart never leave the 100 ms idle window, so they are one gesture.
        Assert.Single(events.OfType<PointerPanZoomStartEvent>());
        Assert.Empty(events.OfType<PointerPanZoomEndEvent>());

        PointerPanZoomUpdateEvent last = events.OfType<PointerPanZoomUpdateEvent>().Last();
        Assert.Equal(new Point(3, 4), last.Pan);
        Assert.Equal(default, last.PanDelta);
        Assert.Equal(1.5, last.Scale);
        Assert.Equal(0.25, last.Rotation);

        timers.Elapse(TrackpadPanZoomSynthesizer.DefaultIdleTimeout);
        Assert.Single(events.OfType<PointerPanZoomEndEvent>());
    }

    [Fact]
    public void ZoomFactors_MultiplyAndPanOffsetsAccumulate()
    {
        using var timers = new FakeGestureTimers();
        var events = new List<PointerEvent>();
        var synthesizer = new TrackpadPanZoomSynthesizer(events.Add);

        synthesizer.Zoom(new Point(0, 0), 2.0, DateTime.UnixEpoch);
        synthesizer.Zoom(new Point(0, 0), 1.5, DateTime.UnixEpoch);
        synthesizer.Pan(new Point(0, 0), new Point(5, 0), DateTime.UnixEpoch);
        synthesizer.Pan(new Point(0, 0), new Point(2, 3), DateTime.UnixEpoch);

        PointerPanZoomUpdateEvent last = events.OfType<PointerPanZoomUpdateEvent>().Last();
        Assert.Equal(3.0, last.Scale);
        Assert.Equal(new Point(7, 3), last.Pan);
        Assert.Equal(new Point(2, 3), last.PanDelta);

        timers.Elapse(TrackpadPanZoomSynthesizer.DefaultIdleTimeout);
    }

    [Fact]
    public void End_ClosesTheSequenceEarlyAndIsIdempotent()
    {
        using var timers = new FakeGestureTimers();
        var events = new List<PointerEvent>();
        var synthesizer = new TrackpadPanZoomSynthesizer(events.Add);

        synthesizer.Zoom(new Point(4, 5), 1.2, DateTime.UnixEpoch);
        synthesizer.End();
        synthesizer.End();

        Assert.Single(events.OfType<PointerPanZoomEndEvent>());
        Assert.Equal(new Point(4, 5), events.OfType<PointerPanZoomEndEvent>().Single().Position);

        // The cancelled idle timer must not fire a second end event.
        timers.Elapse(TrackpadPanZoomSynthesizer.DefaultIdleTimeout);
        Assert.Single(events.OfType<PointerPanZoomEndEvent>());
    }

    [Fact]
    public void EachSequence_GetsAPointerIdThatCannotCollideWithARealPointer()
    {
        using var timers = new FakeGestureTimers();
        var events = new List<PointerEvent>();
        var synthesizer = new TrackpadPanZoomSynthesizer(events.Add);

        synthesizer.Zoom(new Point(0, 0), 1.1, DateTime.UnixEpoch);
        synthesizer.End();
        synthesizer.Zoom(new Point(0, 0), 1.1, DateTime.UnixEpoch);
        synthesizer.End();

        int[] pointers = events.OfType<PointerPanZoomStartEvent>().Select(e => e.Pointer).ToArray();
        Assert.Equal(2, pointers.Length);
        Assert.All(pointers, pointer => Assert.True(pointer < 0));
        Assert.NotEqual(pointers[0], pointers[1]);

        // Every event of one sequence carries that sequence's id.
        Assert.All(events, @event => Assert.Contains(@event.Pointer, pointers));
    }
}
