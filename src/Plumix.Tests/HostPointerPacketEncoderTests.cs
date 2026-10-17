using Avalonia;
using Plumix.Gestures;
using Plumix.UI;
using Xunit;

// C#-only infrastructure: the host-side inverse of `PointerEventConverter` (gestures/converter.dart).

namespace Plumix.Tests;

public sealed class HostPointerPacketEncoderTests
{
    private const double Ratio = 2.0;

    private static List<PointerEvent> RoundTrip(HostPointerPacketEncoder encoder, params PointerEvent[] events)
    {
        return PointerEventConverter
            .Expand(events.Select(@event => encoder.Encode(@event, viewId: 5, Ratio)), _ => Ratio)
            .ToList();
    }

    [Fact]
    public void PositionsAreEncodedInPhysicalPixels()
    {
        var encoder = new HostPointerPacketEncoder();

        PointerData data = encoder.Encode(
            new PointerDownEvent(3, PointerDeviceKind.Mouse, new Point(10, 20), PointerButtons.Primary), 5, Ratio);

        Assert.Equal(PointerChange.Down, data.Change);
        Assert.Equal(5, data.ViewId);
        Assert.Equal(3, data.PointerIdentifier);
        Assert.Equal(20, data.PhysicalX);
        Assert.Equal(40, data.PhysicalY);
        Assert.Equal(PointerButtons.Primary, data.Buttons);
    }

    [Fact]
    public void MovesAndHoversCarryTheDeltaFromThePreviousReport()
    {
        var encoder = new HostPointerPacketEncoder();

        List<PointerEvent> events = RoundTrip(
            encoder,
            new PointerHoverEvent(1, PointerDeviceKind.Mouse, new Point(4, 4)),
            new PointerHoverEvent(1, PointerDeviceKind.Mouse, new Point(6, 5)),
            new PointerDownEvent(1, PointerDeviceKind.Mouse, new Point(10, 10)),
            new PointerMoveEvent(1, PointerDeviceKind.Mouse, new Point(13, 14)),
            new PointerUpEvent(1, PointerDeviceKind.Mouse, new Point(20, 20)));

        Assert.Equal(
            [default, new Point(2, 1), default, new Point(3, 4), default],
            events.Select(@event => @event.Delta));
        Assert.All(events, @event => Assert.Equal(5, @event.ViewId));
    }

    [Fact]
    public void ThePreviousPositionIsForgottenWhenThePointerLeaves()
    {
        var encoder = new HostPointerPacketEncoder();

        List<PointerEvent> events = RoundTrip(
            encoder,
            new PointerHoverEvent(1, PointerDeviceKind.Mouse, new Point(4, 4)),
            new PointerRemovedEvent(1, PointerDeviceKind.Mouse, new Point(4, 4)),
            new PointerHoverEvent(1, PointerDeviceKind.Mouse, new Point(9, 9)));

        Assert.Equal(default, events[2].Delta);
    }

    [Fact]
    public void ScrollSignalsKeepTheirDeltaAndRespondThroughTheEvent()
    {
        var encoder = new HostPointerPacketEncoder();
        bool? allowed = null;
        var scroll = new PointerScrollEvent(
            position: new Point(1, 2),
            scrollDelta: new Point(3, -4),
            onRespond: allow => allowed = allow);

        PointerData data = encoder.Encode(scroll, 5, Ratio);
        var back = Assert.IsType<PointerScrollEvent>(
            Assert.Single(PointerEventConverter.Expand([data], _ => Ratio)));
        back.Respond(allowPlatformDefault: true);

        Assert.Equal(PointerSignalKind.Scroll, data.SignalKind);
        Assert.Equal(new Point(3, -4), back.ScrollDelta);
        Assert.Equal(new Point(1, 2), back.Position);
        Assert.True(allowed);
    }

    [Fact]
    public void PanZoomSequencesRoundTrip()
    {
        var encoder = new HostPointerPacketEncoder();

        List<PointerEvent> events = RoundTrip(
            encoder,
            new PointerPanZoomStartEvent(-1, new Point(5, 5)),
            new PointerPanZoomUpdateEvent(
                -1, new Point(5, 5), pan: new Point(2, 3), panDelta: new Point(1, 1), scale: 1.5, rotation: 0.25),
            new PointerPanZoomEndEvent(-1, new Point(5, 5)));

        Assert.IsType<PointerPanZoomStartEvent>(events[0]);
        var update = Assert.IsType<PointerPanZoomUpdateEvent>(events[1]);
        Assert.Equal(new Point(2, 3), update.Pan);
        Assert.Equal(new Point(1, 1), update.PanDelta);
        Assert.Equal(1.5, update.Scale);
        Assert.Equal(0.25, update.Rotation);
        Assert.IsType<PointerPanZoomEndEvent>(events[2]);
    }

    [Fact]
    public void TimeStampsKeepTheEventTimeline()
    {
        var encoder = new HostPointerPacketEncoder();
        DateTime stamp = FrameClock.UtcNow;

        PointerData data = encoder.Encode(new PointerHoverEvent(1, timestampUtc: stamp), 5, Ratio);
        PointerEvent back = Assert.Single(PointerEventConverter.Expand([data], _ => Ratio));

        Assert.Equal(stamp, back.TimestampUtc);
    }

    [Fact]
    public void ReportDeliversAOneEntryPacketToThePlatformDispatcher()
    {
        PointerDataPacket? received = null;
        Action<PointerDataPacket>? previous = PlatformDispatcher.Instance.OnPointerDataPacket;
        PlatformDispatcher.Instance.OnPointerDataPacket = packet => received = packet;

        try
        {
            new HostPointerPacketEncoder().Report(new PointerAddedEvent(2, PointerDeviceKind.Mouse), 5, Ratio);

            Assert.Equal(PointerChange.Add, Assert.Single(received!.Data).Change);
        }
        finally
        {
            PlatformDispatcher.Instance.OnPointerDataPacket = previous;
        }
    }

    [Fact]
    public void FrameClockUtcNowIsMonotonicAndTheSamplingClockReadsIt()
    {
        DateTime first = FrameClock.UtcNow;
        DateTime second = new SamplingClock().Now();

        Assert.True(second >= first);
        Assert.True(FrameClock.UtcNow >= second);
        Assert.InRange((first - DateTime.UtcNow).TotalSeconds, -5, 5);
    }
}
