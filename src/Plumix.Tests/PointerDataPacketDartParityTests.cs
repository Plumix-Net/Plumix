using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/gestures/gesture_binding_test.dart
// Dart parity source: flutter/packages/flutter/test/gestures/locking_test.dart

namespace Plumix.Tests;

/// <summary>
/// The engine-packet half of <c>gesture_binding_test.dart</c> and <c>locking_test.dart</c>:
/// <c>PointerDataPacket</c> in, framework events out. Dart's <c>TestGestureFlutterBinding.handleEvent</c>
/// hook is a listener render object under a registered view that records every event it receives.
/// </summary>
public sealed class PointerDataPacketDartParityTests : IDisposable
{
    private const double DevicePixelRatio = 2.5;
    private static int _nextViewId = 910_000;

    private readonly int _viewId = Interlocked.Increment(ref _nextViewId);
    private readonly GestureBinding _binding = GestureBinding.Instance;
    private readonly List<PointerEvent> _handled = [];
    private readonly FlutterView _view;
    private readonly RenderView _root;
    private readonly PipelineOwner _pipeline;

    public PointerDataPacketDartParityTests()
    {
        _binding.ResetForTests();
        _view = new FlutterView(new Size(800, 600), viewId: _viewId);
        var listener = new RenderPointerListener(
            onPointerDown: _handled.Add,
            onPointerMove: _handled.Add,
            onPointerUp: _handled.Add,
            onPointerHover: _handled.Add,
            onPointerCancel: _handled.Add,
            onPointerPanZoomStart: _handled.Add,
            onPointerPanZoomUpdate: _handled.Add,
            onPointerPanZoomEnd: _handled.Add,
            onPointerSignal: _handled.Add,
            behavior: HitTestBehavior.Opaque);
        _root = new RenderView(_view) { Child = listener };
        _pipeline = new PipelineOwner(_root);
        _pipeline.Attach(_root);
        _pipeline.FlushLayout(new Size(200, 200));
        PlatformDispatcher.Instance.AddView(_view);
        RendererBinding.Instance.AddRenderView(_root);
    }

    public void Dispose()
    {
        RendererBinding.Instance.RemoveRenderView(_root);
        PlatformDispatcher.Instance.RemoveView(_view);
        _pipeline.RootNode = null;
        _binding.ResetForTests();
    }

    private PointerData Data(
        PointerChange change = PointerChange.Cancel,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        PointerButtons buttons = PointerButtons.None,
        PointerSignalKind? signalKind = null,
        int device = 0,
        double physicalX = 0.0,
        double physicalY = 0.0,
        double scrollDeltaX = 0.0,
        double scrollDeltaY = 0.0)
    {
        return new PointerData(
            viewId: _viewId,
            change: change,
            kind: kind,
            buttons: buttons,
            signalKind: signalKind,
            device: device,
            physicalX: physicalX,
            physicalY: physicalY,
            scrollDeltaX: scrollDeltaX,
            scrollDeltaY: scrollDeltaY);
    }

    private static void Send(PointerDataPacket packet)
    {
        PlatformDispatcher.Instance.OnPointerDataPacket!.Invoke(packet);
    }

    private static List<PointerEvent> Expand(IEnumerable<PointerData> data, double ratio = DevicePixelRatio)
    {
        return PointerEventConverter.Expand(data, _ => ratio).ToList();
    }

    [Fact]
    public void BindingInstallsThePacketHandlerOnThePlatformDispatcher()
    {
        Assert.NotNull(PlatformDispatcher.Instance.OnPointerDataPacket);
    }

    [Fact]
    public void PointerTapEvents()
    {
        Send(new PointerDataPacket([Data(PointerChange.Down), Data(PointerChange.Up)]));

        Assert.Equal(2, _handled.Count);
        Assert.IsType<PointerDownEvent>(_handled[0]);
        Assert.IsType<PointerUpEvent>(_handled[1]);
    }

    [Fact]
    public void PointerMoveEvents()
    {
        Send(new PointerDataPacket([Data(PointerChange.Down), Data(PointerChange.Move), Data(PointerChange.Up)]));

        Assert.Equal(3, _handled.Count);
        Assert.IsType<PointerDownEvent>(_handled[0]);
        Assert.IsType<PointerMoveEvent>(_handled[1]);
        Assert.IsType<PointerUpEvent>(_handled[2]);
    }

    [Fact]
    public void PointerHoverEvents()
    {
        var routed = new List<PointerEvent>();
        PointerRoute route = routed.Add;
        _binding.PointerRouter.AddGlobalRoute(route);

        try
        {
            Send(new PointerDataPacket(
            [
                Data(PointerChange.Add),
                Data(PointerChange.Hover),
                Data(PointerChange.Hover),
                Data(PointerChange.Remove),
                Data(PointerChange.Add),
                Data(PointerChange.Hover),
            ]));

            Assert.Equal(3, _handled.Count);
            Assert.All(_handled, @event => Assert.IsType<PointerHoverEvent>(@event));
            Assert.Equal(6, routed.Count);
            Assert.IsType<PointerAddedEvent>(routed[0]);
            Assert.IsType<PointerHoverEvent>(routed[1]);
            Assert.IsType<PointerHoverEvent>(routed[2]);
            Assert.IsType<PointerRemovedEvent>(routed[3]);
            Assert.IsType<PointerAddedEvent>(routed[4]);
            Assert.IsType<PointerHoverEvent>(routed[5]);
        }
        finally
        {
            _binding.PointerRouter.RemoveGlobalRoute(route);
        }
    }

    [Fact]
    public void PointerCancelEvents()
    {
        Send(new PointerDataPacket([Data(PointerChange.Down), Data()]));

        Assert.Equal(2, _handled.Count);
        Assert.IsType<PointerDownEvent>(_handled[0]);
        Assert.IsType<PointerCancelEvent>(_handled[1]);
    }

    [Fact]
    public void CanCancelPointers()
    {
        var events = new List<PointerEvent>();
        PointerRoute route = @event =>
        {
            events.Add(@event);
            if (@event is PointerDownEvent)
            {
                _binding.CancelPointer(@event.Pointer);
            }
        };
        _binding.PointerRouter.AddGlobalRoute(route);

        try
        {
            Send(new PointerDataPacket([Data(PointerChange.Down), Data(PointerChange.Up)]));

            Assert.Equal(2, events.Count);
            Assert.IsType<PointerDownEvent>(events[0]);
            Assert.IsType<PointerCancelEvent>(events[1]);
        }
        finally
        {
            _binding.PointerRouter.RemoveGlobalRoute(route);
        }
    }

    [Fact]
    public void CanExpandAddAndHoverPointers()
    {
        List<PointerEvent> events = Expand(
        [
            Data(PointerChange.Add, device: 24),
            Data(PointerChange.Hover, device: 24),
            Data(PointerChange.Remove, device: 24),
            Data(PointerChange.Add, device: 24),
            Data(PointerChange.Hover, device: 24),
        ]);

        Assert.Equal(5, events.Count);
        Assert.IsType<PointerAddedEvent>(events[0]);
        Assert.IsType<PointerHoverEvent>(events[1]);
        Assert.IsType<PointerRemovedEvent>(events[2]);
        Assert.IsType<PointerAddedEvent>(events[3]);
        Assert.IsType<PointerHoverEvent>(events[4]);
    }

    [Fact]
    public void CanHandleMalformedScrollingEvent()
    {
        List<PointerEvent> events = Expand([Data(PointerChange.Add, device: 24)]);
        Assert.Single(events);
        Assert.IsType<PointerAddedEvent>(events[0]);

        // A packet that contains malformed scroll events.
        PointerData Scroll(double x, double y) =>
            Data(signalKind: PointerSignalKind.Scroll, device: 24, scrollDeltaX: x, scrollDeltaY: y);
        events = Expand(
        [
            Scroll(double.PositiveInfinity, 10),
            Scroll(double.NaN, 10),
            Scroll(double.NegativeInfinity, 10),
            Scroll(10, double.PositiveInfinity),
            Scroll(10, double.NaN),
            Scroll(10, double.NegativeInfinity),
        ]);
        Assert.Empty(events);

        // A valid scroll event expands when the device pixel ratio is valid.
        PointerData[] valid = [Scroll(10, 10)];
        events = Expand(valid);
        Assert.Single(events);
        Assert.IsType<PointerScrollEvent>(events[0]);

        // And is dropped when the device pixel ratio is invalid.
        Assert.Empty(Expand(valid, ratio: 0));
    }

    [Fact]
    public void CanExpandPointerScrollEvents()
    {
        List<PointerEvent> events = Expand(
        [
            Data(PointerChange.Add),
            Data(PointerChange.Hover, signalKind: PointerSignalKind.Scroll),
        ]);

        Assert.Equal(2, events.Count);
        Assert.IsType<PointerAddedEvent>(events[0]);
        Assert.IsType<PointerScrollEvent>(events[1]);
    }

    [Fact]
    public void ScrollEventsAreInLogicalPixelsAndRespondThroughTheDatum()
    {
        bool? allowed = null;
        var datum = new PointerData(
            viewId: _viewId,
            signalKind: PointerSignalKind.Scroll,
            physicalX: 25,
            physicalY: 50,
            scrollDeltaX: 5,
            scrollDeltaY: -10,
            onRespond: allow => allowed = allow);

        var @event = Assert.IsType<PointerScrollEvent>(Assert.Single(Expand([datum])));

        Assert.Equal(new Point(10, 20), @event.Position);
        Assert.Equal(new Point(2, -4), @event.ScrollDelta);
        @event.Respond(allowPlatformDefault: true);
        Assert.True(allowed);
    }

    [Fact]
    public void UnknownSignalKindsAreDropped()
    {
        Assert.Empty(Expand([Data(signalKind: PointerSignalKind.Unknown)]));
    }

    [Fact]
    public void ScrollInertiaCancelAndScaleSignalsExpand()
    {
        List<PointerEvent> events = Expand(
        [
            new PointerData(signalKind: PointerSignalKind.ScrollInertiaCancel),
            new PointerData(signalKind: PointerSignalKind.Scale, scale: 1.5),
        ]);

        Assert.IsType<PointerScrollInertiaCancelEvent>(events[0]);
        Assert.Equal(1.5, Assert.IsType<PointerScaleEvent>(events[1]).Scale);
    }

    [Fact]
    public void PositionsAndRadiiAreConvertedToLogicalPixels()
    {
        var datum = new PointerData(
            change: PointerChange.Move,
            physicalX: 50,
            physicalY: 25,
            physicalDeltaX: 5,
            physicalDeltaY: 10,
            radiusMajor: 5,
            radiusMinor: 10,
            radiusMin: 2.5,
            radiusMax: 25,
            timeStamp: TimeSpan.FromMilliseconds(40));

        var @event = Assert.IsType<PointerMoveEvent>(Assert.Single(Expand([datum])));

        Assert.Equal(new Point(20, 10), @event.Position);
        Assert.Equal(new Point(2, 4), @event.Delta);
        Assert.Equal(2, @event.RadiusMajor);
        Assert.Equal(4, @event.RadiusMinor);
        Assert.Equal(1, @event.RadiusMin);
        Assert.Equal(10, @event.RadiusMax);
        Assert.Equal(default(DateTime) + TimeSpan.FromMilliseconds(40), @event.TimestampUtc);
    }

    [Fact]
    public void ShouldSynthesizePrimaryButtonForTouchWhenNoButtonIsSet()
    {
        double location = 10.0 * DevicePixelRatio;
        List<PointerEvent> events = Expand(
        [
            Data(PointerChange.Add, physicalX: location, physicalY: location),
            Data(PointerChange.Hover, physicalX: location, physicalY: location),
            Data(PointerChange.Down, physicalX: location, physicalY: location),
            Data(PointerChange.Move, physicalX: location, physicalY: location),
            Data(PointerChange.Up, physicalX: location, physicalY: location),
        ]);

        Assert.Equal(5, events.Count);
        Assert.IsType<PointerAddedEvent>(events[0]);
        Assert.Equal(PointerButtons.None, events[0].Buttons);
        Assert.IsType<PointerHoverEvent>(events[1]);
        Assert.Equal(PointerButtons.None, events[1].Buttons);
        Assert.IsType<PointerDownEvent>(events[2]);
        Assert.Equal(PointerButtons.Primary, events[2].Buttons);
        Assert.IsType<PointerMoveEvent>(events[3]);
        Assert.Equal(PointerButtons.Primary, events[3].Buttons);
        Assert.IsType<PointerUpEvent>(events[4]);
        Assert.Equal(PointerButtons.None, events[4].Buttons);
        Assert.All(events, @event => Assert.Equal(new Point(10, 10), @event.Position));
    }

    [Fact]
    public void ShouldNotSynthesizePrimaryButtonForTouchWhenAButtonIsSet()
    {
        double location = 10.0 * DevicePixelRatio;
        List<PointerEvent> events = Expand(
        [
            Data(PointerChange.Add, physicalX: location, physicalY: location),
            Data(PointerChange.Hover, physicalX: location, physicalY: location),
            Data(PointerChange.Down, buttons: PointerButtons.Secondary, physicalX: location, physicalY: location),
            Data(PointerChange.Move, buttons: PointerButtons.Secondary, physicalX: location, physicalY: location),
            Data(PointerChange.Up, physicalX: location, physicalY: location),
        ]);

        Assert.Equal(5, events.Count);
        Assert.Equal(PointerButtons.None, events[0].Buttons);
        Assert.Equal(PointerButtons.None, events[1].Buttons);
        Assert.IsType<PointerDownEvent>(events[2]);
        Assert.Equal(PointerButtons.Secondary, events[2].Buttons);
        Assert.IsType<PointerMoveEvent>(events[3]);
        Assert.Equal(PointerButtons.Secondary, events[3].Buttons);
        Assert.Equal(PointerButtons.None, events[4].Buttons);
    }

    [Theory]
    [InlineData(PointerDeviceKind.Stylus)]
    [InlineData(PointerDeviceKind.InvertedStylus)]
    public void ShouldSynthesizePrimaryButtonForStylusWhenNoButtonIsSet(PointerDeviceKind kind)
    {
        double location = 10.0 * DevicePixelRatio;
        List<PointerEvent> events = Expand(
        [
            Data(PointerChange.Add, kind, physicalX: location, physicalY: location),
            Data(PointerChange.Hover, kind, physicalX: location, physicalY: location),
            Data(PointerChange.Down, kind, physicalX: location, physicalY: location),
            Data(PointerChange.Move, kind, PointerButtons.SecondaryStylus, physicalX: location, physicalY: location),
            Data(PointerChange.Up, kind, physicalX: location, physicalY: location),
        ]);

        Assert.Equal(5, events.Count);
        Assert.Equal(PointerButtons.None, events[0].Buttons);
        Assert.Equal(PointerButtons.None, events[1].Buttons);
        Assert.IsType<PointerDownEvent>(events[2]);
        Assert.Equal(PointerButtons.Primary, events[2].Buttons);
        Assert.IsType<PointerMoveEvent>(events[3]);
        Assert.Equal(PointerButtons.SecondaryStylus, events[3].Buttons);
        Assert.Equal(PointerButtons.None, events[4].Buttons);
    }

    [Fact]
    public void ShouldSynthesizePrimaryButtonForUnknownDevicesWhenNoButtonIsSet()
    {
        double location = 10.0 * DevicePixelRatio;
        const PointerDeviceKind kind = PointerDeviceKind.Unknown;
        List<PointerEvent> events = Expand(
        [
            Data(PointerChange.Add, kind, physicalX: location, physicalY: location),
            Data(PointerChange.Hover, kind, physicalX: location, physicalY: location),
            Data(PointerChange.Down, kind, physicalX: location, physicalY: location),
            Data(PointerChange.Move, kind, PointerButtons.Secondary, physicalX: location, physicalY: location),
            Data(PointerChange.Up, kind, physicalX: location, physicalY: location),
        ]);

        Assert.Equal(5, events.Count);
        Assert.Equal(PointerButtons.None, events[0].Buttons);
        Assert.Equal(PointerButtons.None, events[1].Buttons);
        Assert.Equal(PointerButtons.Primary, events[2].Buttons);
        Assert.Equal(PointerButtons.Secondary, events[3].Buttons);
        Assert.Equal(PointerButtons.None, events[4].Buttons);
    }

    [Fact]
    public void ShouldNotSynthesizePrimaryButtonForMouse()
    {
        double location = 10.0 * DevicePixelRatio;
        const PointerDeviceKind kind = PointerDeviceKind.Mouse;
        List<PointerEvent> events = Expand(
        [
            Data(PointerChange.Add, kind, physicalX: location, physicalY: location),
            Data(PointerChange.Hover, kind, physicalX: location, physicalY: location),
            Data(PointerChange.Down, kind, PointerButtons.MiddleMouse, physicalX: location, physicalY: location),
            Data(
                PointerChange.Move,
                kind,
                PointerButtons.MiddleMouse | PointerButtons.SecondaryMouse,
                physicalX: location,
                physicalY: location),
            Data(PointerChange.Up, kind, physicalX: location, physicalY: location),
        ]);

        Assert.Equal(5, events.Count);
        Assert.Equal(PointerButtons.None, events[0].Buttons);
        Assert.Equal(PointerButtons.None, events[1].Buttons);
        Assert.Equal(PointerButtons.MiddleMouse, events[2].Buttons);
        Assert.Equal(PointerButtons.MiddleMouse | PointerButtons.SecondaryMouse, events[3].Buttons);
        Assert.Equal(PointerButtons.None, events[4].Buttons);
    }

    [Fact]
    public void PointerPanZoomEvents()
    {
        Send(new PointerDataPacket(
        [
            Data(PointerChange.PanZoomStart),
            Data(PointerChange.PanZoomUpdate),
            Data(PointerChange.PanZoomEnd),
        ]));

        Assert.Equal(3, _handled.Count);
        Assert.IsType<PointerPanZoomStartEvent>(_handled[0]);
        Assert.IsType<PointerPanZoomUpdateEvent>(_handled[1]);
        Assert.IsType<PointerPanZoomEndEvent>(_handled[2]);
    }

    [Fact]
    public void PanZoomUpdatesAreInLogicalPixels()
    {
        var datum = new PointerData(
            change: PointerChange.PanZoomUpdate,
            panX: 25,
            panY: 50,
            panDeltaX: 5,
            panDeltaY: 10,
            scale: 2,
            rotation: 0.5);

        var @event = Assert.IsType<PointerPanZoomUpdateEvent>(Assert.Single(Expand([datum])));

        Assert.Equal(new Point(10, 20), @event.Pan);
        Assert.Equal(new Point(2, 4), @event.PanDelta);
        Assert.Equal(2, @event.Scale);
        Assert.Equal(0.5, @event.Rotation);
    }

    [Fact]
    public void ErrorHandling()
    {
        var reports = new List<string>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = details => reports.Add(details.ToString());
        Action<PointerEvent> thrower = @event => throw new InvalidOperationException($"zipzapzooey {@event}");
        GestureBinding.PointerEventReceived += thrower;

        try
        {
            Send(new PointerDataPacket([Data(PointerChange.Down), Data(PointerChange.Up)]));

            Assert.Single(reports);
            Assert.Contains("while handling a pointer data", reports[0].Replace("\n", " "));
            Assert.Contains("zipzapzooey", reports[0]);
            Assert.Contains("PointerDownEvent", reports[0]);

            // Failure happens on the first message, remaining messages aren't processed.
            Assert.DoesNotContain("PointerUpEvent", reports[0]);
        }
        finally
        {
            GestureBinding.PointerEventReceived -= thrower;
            FlutterError.OnError = previous;
        }
    }

    [Fact]
    public void PointerEventConverterProcessesViewIds()
    {
        const int startId = 987654;
        PointerData[] data =
        [
            new(viewId: startId + 0, change: PointerChange.Cancel),
            new(viewId: startId + 1, change: PointerChange.Add),
            new(viewId: startId + 2, change: PointerChange.Remove),
            new(viewId: startId + 3, change: PointerChange.Hover),
            new(viewId: startId + 4, change: PointerChange.Down),
            new(viewId: startId + 5, change: PointerChange.Move),
            new(viewId: startId + 6, change: PointerChange.Up),
            new(viewId: startId + 7, change: PointerChange.PanZoomStart),
            new(viewId: startId + 8, change: PointerChange.PanZoomUpdate),
            new(viewId: startId + 9, change: PointerChange.PanZoomEnd),
        ];
        var viewIds = new List<int>();
        double? Getter(int viewId)
        {
            viewIds.Add(viewId);
            return viewId / 10.0;
        }

        List<PointerEvent> events = PointerEventConverter.Expand(data, Getter).ToList();

        int[] expected = Enumerable.Range(0, 10).Select(index => startId + index).ToArray();
        Assert.Equal(expected, viewIds);
        Assert.Equal(10, events.Count);
        Assert.Equal(expected, events.Select(@event => @event.ViewId));
    }

    [Fact]
    public void EntriesOfAViewThatNoLongerExistsAreDropped()
    {
        var data = new[] { new PointerData(viewId: 1, change: PointerChange.Add), new PointerData(viewId: 2) };

        List<PointerEvent> events = PointerEventConverter.Expand(data, id => id == 2 ? null : 1.0).ToList();

        Assert.Equal(1, Assert.Single(events).ViewId);
    }

    [Fact]
    public void ExpandIsLazy()
    {
        int calls = 0;
        IEnumerable<PointerEvent> events = PointerEventConverter.Expand(
            [new PointerData(change: PointerChange.Add)],
            _ =>
            {
                calls++;
                return 1.0;
            });

        Assert.Equal(0, calls);
        _ = events.ToList();
        Assert.Equal(1, calls);
    }

    [Fact]
    public void PointerDataPrintsLikeDart()
    {
        var datum = new PointerData(viewId: 3, physicalX: 1.5, physicalY: 2);

        Assert.Equal("PointerData(viewId: 3, x: 1.5, y: 2)", datum.ToString());
        Assert.Contains("physicalX: 1.5, physicalY: 2,", datum.ToStringFull());
        Assert.StartsWith("PointerData(embedderId: 0, timeStamp: ", datum.ToStringFull());
        Assert.EndsWith("viewId: 3)", datum.ToStringFull());
    }

    [Fact]
    public async Task PointerEventsAreLockedDuringReassemble()
    {
        var packet = new PointerDataPacket([Data(PointerChange.Down), Data(PointerChange.Up)]);
        bool tested = false;

        Task locked = SchedulerBinding.Instance.LockEvents(() =>
        {
            Send(packet);
            Assert.Empty(_handled);
            tested = true;
            return Task.CompletedTask;
        });
        await EventLoopPump.WaitFor(locked);
        Assert.True(EventLoopPump.SpinUntil(() => !SchedulerBinding.Instance.Locked));

        Assert.True(tested);
        Assert.Equal(2, _handled.Count);
        Assert.IsType<PointerDownEvent>(_handled[0]);
        Assert.IsType<PointerUpEvent>(_handled[1]);
    }

    [Fact]
    public async Task HostEventsWaitWhileLockedToo()
    {
        Task locked = SchedulerBinding.Instance.LockEvents(() =>
        {
            _binding.HandlePointerEvent(_root, new PointerHoverEvent(1, PointerDeviceKind.Mouse, new Point(4, 4)));
            Assert.Empty(_handled);
            return Task.CompletedTask;
        });
        await EventLoopPump.WaitFor(locked);
        Assert.True(EventLoopPump.SpinUntil(() => !SchedulerBinding.Instance.Locked));

        Assert.IsType<PointerHoverEvent>(Assert.Single(_handled));
    }

    [Fact]
    public void PacketEventsKeepTheDeltaTheEngineReported()
    {
        var received = new List<PointerEvent>();
        PointerRoute route = received.Add;
        _binding.PointerRouter.AddGlobalRoute(route);

        try
        {
            Send(new PointerDataPacket(
            [
                new PointerData(viewId: _viewId, change: PointerChange.Hover, kind: PointerDeviceKind.Mouse),
                new PointerData(
                    viewId: _viewId,
                    change: PointerChange.Hover,
                    kind: PointerDeviceKind.Mouse,
                    physicalX: 25,
                    physicalY: 25,
                    physicalDeltaX: 5,
                    physicalDeltaY: 10),
            ]));

            Assert.Equal([default, new Point(5, 10)], received.Select(@event => @event.Delta));
        }
        finally
        {
            _binding.PointerRouter.RemoveGlobalRoute(route);
        }
    }

    [Fact]
    public void TouchPacketsAreResampledWhenResamplingIsEnabled()
    {
        TimeSpan future = DateTime.UtcNow.AddSeconds(5) - default(DateTime);
        _binding.ResamplingEnabled = true;

        Send(new PointerDataPacket([new PointerData(viewId: _viewId, change: PointerChange.Down, timeStamp: future)]));
        Assert.Empty(_handled);

        // Disabling resampling dispatches what it still holds before the next event.
        _binding.ResamplingEnabled = false;
        Send(new PointerDataPacket(
            [new PointerData(viewId: _viewId, change: PointerChange.Up, timeStamp: future + TimeSpan.FromSeconds(1))]));
        Assert.IsType<PointerDownEvent>(_handled[0]);
        Assert.IsType<PointerUpEvent>(_handled[1]);
    }

    [Fact]
    public void PacketEventsHitTestTheViewNamedByTheirViewId()
    {
        Send(new PointerDataPacket(
        [
            new PointerData(viewId: _viewId + 1, change: PointerChange.Down),
            new PointerData(viewId: _viewId + 1, change: PointerChange.Up),
        ]));

        // No view named by the entries exists, so their ratio lookup drops them.
        Assert.Empty(_handled);

        Send(new PointerDataPacket([Data(PointerChange.Down), Data(PointerChange.Up)]));
        Assert.Equal(2, _handled.Count);
    }
}
