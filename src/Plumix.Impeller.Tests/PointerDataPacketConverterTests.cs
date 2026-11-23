using Plumix.UI;
using Xunit;

namespace Plumix.Impeller.Tests;

// C#-only host infrastructure tests; parity source:
// flutter/engine/src/flutter/lib/ui/window/pointer_data_packet_converter_unittests.cc.

public sealed class PointerDataPacketConverterTests
{
    private const int ViewId = 0;

    [Fact]
    public void CanSynthesizeDownAndUp()
    {
        IReadOnlyList<PointerData> result = Convert(
            Touch(PointerChange.Add, 0, 0.0, 0.0, 0),
            Touch(PointerChange.Down, 0, 3.0, 0.0, 1),
            Touch(PointerChange.Up, 0, 3.0, 4.0, 0),
            Touch(PointerChange.Remove, 0, 3.0, 4.0, 0));

        Assert.Equal(6, result.Count);
        AssertChange(result[0], PointerChange.Add, synthesized: false);
        AssertChange(result[1], PointerChange.Hover, synthesized: true);
        Assert.Equal((3.0, 0.0), (result[1].PhysicalDeltaX, result[1].PhysicalDeltaY));
        Assert.Equal(PointerButtons.None, result[1].Buttons);
        AssertChange(result[2], PointerChange.Down, synthesized: false);
        Assert.Equal(1, result[2].PointerIdentifier);
        Assert.Equal((PointerButtons)1, result[2].Buttons);
        AssertChange(result[3], PointerChange.Move, synthesized: true);
        Assert.Equal(1, result[3].PointerIdentifier);
        Assert.Equal((0.0, 4.0), (result[3].PhysicalDeltaX, result[3].PhysicalDeltaY));
        Assert.Equal((PointerButtons)1, result[3].Buttons);
        AssertChange(result[4], PointerChange.Up, synthesized: false);
        Assert.Equal(1, result[4].PointerIdentifier);
        Assert.Equal(PointerButtons.None, result[4].Buttons);
        AssertChange(result[5], PointerChange.Remove, synthesized: false);
    }

    [Fact]
    public void CanUpdatePointerIdentifier()
    {
        IReadOnlyList<PointerData> result = Convert(
            Touch(PointerChange.Add, 0, 0.0, 0.0, 0),
            Touch(PointerChange.Down, 0, 0.0, 0.0, 1),
            Touch(PointerChange.Up, 0, 0.0, 0.0, 0),
            Touch(PointerChange.Down, 0, 0.0, 0.0, 1),
            Touch(PointerChange.Move, 0, 3.0, 0.0, 1),
            Touch(PointerChange.Up, 0, 3.0, 0.0, 0),
            Touch(PointerChange.Remove, 0, 3.0, 0.0, 0));

        Assert.Equal(7, result.Count);
        Assert.Equal(
            [0, 1, 1, 2, 2, 2, 0],
            result.Select(data => data.PointerIdentifier));
        Assert.All(result, data => Assert.False(data.Synthesized));
        Assert.Equal((3.0, 0.0), (result[4].PhysicalDeltaX, result[4].PhysicalDeltaY));
    }

    [Fact]
    public void CanSynthesizeAdd()
    {
        IReadOnlyList<PointerData> result = Convert(
            Touch(PointerChange.Down, 0, 330.0, 450.0, 1),
            Touch(PointerChange.Up, 0, 0.0, 0.0, 0));

        Assert.Equal(4, result.Count);
        AssertChange(result[0], PointerChange.Add, synthesized: true);
        Assert.Equal((330.0, 450.0), (result[0].PhysicalX, result[0].PhysicalY));
        Assert.Equal(PointerButtons.None, result[0].Buttons);
        AssertChange(result[1], PointerChange.Down, synthesized: false);
        AssertChange(result[2], PointerChange.Move, synthesized: true);
        Assert.Equal((-330.0, -450.0), (result[2].PhysicalDeltaX, result[2].PhysicalDeltaY));
        Assert.Equal((PointerButtons)1, result[2].Buttons);
        AssertChange(result[3], PointerChange.Up, synthesized: false);
        Assert.Equal((0.0, 0.0), (result[3].PhysicalX, result[3].PhysicalY));
    }

    [Theory]
    [InlineData(PointerSignalKind.Scroll)]
    [InlineData(PointerSignalKind.ScrollInertiaCancel)]
    [InlineData(PointerSignalKind.Scale)]
    public void CanConvertPointerSignals(PointerSignalKind kind)
    {
        IReadOnlyList<PointerData> result = Convert(
            Mouse(PointerChange.Add, PointerSignalKind.None, 0, 0.0, 0.0, 0.0, 0.0, 0),
            Mouse(PointerChange.Add, PointerSignalKind.None, 1, 0.0, 0.0, 0.0, 0.0, 0),
            Mouse(PointerChange.Down, PointerSignalKind.None, 1, 0.0, 0.0, 0.0, 0.0, 1),
            Mouse(PointerChange.Hover, kind, 0, 34.0, 34.0, 30.0, 0.0, 0),
            Mouse(PointerChange.Hover, kind, 1, 49.0, 49.0, 50.0, 0.0, 0),
            Mouse(PointerChange.Hover, kind, 2, 10.0, 20.0, 30.0, 40.0, 0));

        Assert.Equal(9, result.Count);
        // A hover to the signal's position for the hovering device 0, a move for the pressed device 1.
        AssertChange(result[3], PointerChange.Hover, synthesized: true);
        Assert.Equal(PointerSignalKind.None, result[3].SignalKind);
        Assert.Equal((34.0, 34.0), (result[3].PhysicalDeltaX, result[3].PhysicalDeltaY));
        Assert.Equal(kind, result[4].SignalKind);
        Assert.Equal(30.0, result[4].ScrollDeltaX);
        AssertChange(result[5], PointerChange.Move, synthesized: true);
        Assert.Equal(1, result[5].Device);
        Assert.Equal((PointerButtons)1, result[5].Buttons);
        Assert.Equal(kind, result[6].SignalKind);
        Assert.Equal(50.0, result[6].ScrollDeltaX);
        // Device 2 was never added: the converter adds it where the signal happens.
        AssertChange(result[7], PointerChange.Add, synthesized: true);
        Assert.Equal((10.0, 20.0), (result[7].PhysicalX, result[7].PhysicalY));
        Assert.Equal(kind, result[8].SignalKind);
        Assert.Equal((30.0, 40.0), (result[8].ScrollDeltaX, result[8].ScrollDeltaY));
    }

    [Fact]
    public void CanConvertTrackpadGesture()
    {
        IReadOnlyList<PointerData> result = Convert(
            Trackpad(PointerChange.PanZoomStart, 0, 0.0, 0.0),
            Trackpad(PointerChange.PanZoomUpdate, 0, 3.0, 4.0),
            Trackpad(PointerChange.PanZoomEnd, 0, 0.0, 0.0));

        Assert.Equal(4, result.Count);
        AssertChange(result[0], PointerChange.Add, synthesized: true);
        AssertChange(result[1], PointerChange.PanZoomStart, synthesized: false);
        AssertChange(result[2], PointerChange.PanZoomUpdate, synthesized: false);
        Assert.Equal((3.0, 4.0), (result[2].PanX, result[2].PanY));
        Assert.Equal((3.0, 4.0), (result[2].PanDeltaX, result[2].PanDeltaY));
        AssertChange(result[3], PointerChange.PanZoomEnd, synthesized: false);
    }

    [Fact]
    public void DropsDataForAnUnknownView()
    {
        EnginePointerData other = Touch(PointerChange.Down, 0, 1.0, 1.0, 1);
        other.ViewId = 7;

        Assert.Empty(Convert(other));
    }

    private static IReadOnlyList<PointerData> Convert(params EnginePointerData[] packet) =>
        new PointerDataPacketConverter(id => id == ViewId).Convert(packet).Data;

    private static void AssertChange(PointerData data, PointerChange change, bool synthesized)
    {
        Assert.Equal(change, data.Change);
        Assert.Equal(synthesized, data.Synthesized);
    }

    private static EnginePointerData Touch(PointerChange change, int device, double x, double y, long buttons) => new()
    {
        ViewId = ViewId,
        Change = change,
        Kind = PointerDeviceKind.Touch,
        Device = device,
        PhysicalX = x,
        PhysicalY = y,
        Buttons = buttons,
    };

    private static EnginePointerData Mouse(
        PointerChange change,
        PointerSignalKind signalKind,
        int device,
        double x,
        double y,
        double scrollDeltaX,
        double scrollDeltaY,
        long buttons) => new()
    {
        ViewId = ViewId,
        Change = change,
        Kind = PointerDeviceKind.Mouse,
        SignalKind = signalKind,
        Device = device,
        PhysicalX = x,
        PhysicalY = y,
        ScrollDeltaX = scrollDeltaX,
        ScrollDeltaY = scrollDeltaY,
        Buttons = buttons,
    };

    private static EnginePointerData Trackpad(PointerChange change, int device, double panX, double panY) => new()
    {
        ViewId = ViewId,
        Change = change,
        Kind = PointerDeviceKind.Trackpad,
        Device = device,
        PanX = panX,
        PanY = panY,
        Scale = 1.0,
    };
}
