using Avalonia;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/test/gestures/events_test.dart; flutter/packages/flutter/test/gestures/gesture_binding_test.dart (parity regression tests)

namespace Plumix.Tests;

/// <summary>
/// Ports the trackpad pan/zoom behaviors Flutter's `events_test.dart` and `gesture_binding_test.dart`
/// assert: the three event classes, how a transform maps their local coordinates, which event types
/// may carry <see cref="PointerDeviceKind.Trackpad"/>, and how the binding routes a pan/zoom
/// sequence through the hit-test cache and the arena.
/// </summary>
public sealed class PointerPanZoomTests
{
    private static Matrix4 ScaleThenTranslate()
    {
        // Dart's `Matrix4.identity()..scale(2.0)..translate(10.0, 20.0)`: scale applied last.
        Matrix4 transform = Matrix4.Identity();
        transform.ScaleByDouble(2.0, 2.0, 2.0, 1.0);
        transform.TranslateByDouble(10.0, 20.0, 0.0, 1.0);
        return transform;
    }

    [Fact]
    public void PanZoomStartEvent_Transformed_MapsLocalPositionAndKeepsEveryOtherField()
    {
        var @event = new PointerPanZoomStartEvent(
            pointer: 0, position: new Point(20, 30), timestampUtc: DateTime.UnixEpoch.AddSeconds(2));
        Matrix4 transform = ScaleThenTranslate();

        var transformed = (PointerPanZoomStartEvent)@event.Transformed(transform);

        Assert.Same(@event, transformed.Original);
        Assert.Same(transform, transformed.Transform);
        Assert.Equal(new Point(60, 100), transformed.LocalPosition);
        Assert.Equal(@event.LocalDelta, transformed.LocalDelta);
        Assert.Equal(@event.Position, transformed.Position);
        Assert.Equal(@event.Delta, transformed.Delta);
        Assert.Equal(@event.Buttons, transformed.Buttons);
        Assert.Equal(@event.Down, transformed.Down);
        Assert.Equal(@event.Kind, transformed.Kind);
        Assert.Equal(@event.Pointer, transformed.Pointer);
        Assert.Equal(@event.Synthesized, transformed.Synthesized);
        Assert.Equal(@event.TimestampUtc, transformed.TimestampUtc);
    }

    [Fact]
    public void PanZoomUpdateEvent_Transformed_MapsPanAsAPointAndPanDeltaAsADelta()
    {
        var @event = new PointerPanZoomUpdateEvent(
            pointer: 0,
            position: new Point(20, 30),
            timestampUtc: DateTime.UnixEpoch.AddSeconds(2),
            pan: new Point(4, 6),
            panDelta: new Point(1, 2),
            scale: 2.5,
            rotation: 0.75);
        Matrix4 transform = ScaleThenTranslate();

        var transformed = (PointerPanZoomUpdateEvent)@event.Transformed(transform);

        Assert.Same(@event, transformed.Original);
        Assert.Equal(new Point(60, 100), transformed.LocalPosition);
        // `pan` is transformed as a point, so the translation applies: (4 + 10) * 2, (6 + 20) * 2.
        Assert.Equal(new Point(28, 52), transformed.LocalPan);
        // `panDelta` is a delta anchored on `pan`, so only the scale applies.
        Assert.Equal(new Point(2, 4), transformed.LocalPanDelta);
        Assert.Equal(@event.Pan, transformed.Pan);
        Assert.Equal(@event.PanDelta, transformed.PanDelta);
        // Scale and rotation are never transformed.
        Assert.Equal(2.5, transformed.Scale);
        Assert.Equal(0.75, transformed.Rotation);
    }

    [Fact]
    public void PanZoomEndEvent_Transformed_MapsLocalPosition()
    {
        var @event = new PointerPanZoomEndEvent(
            pointer: 0, position: new Point(20, 30), timestampUtc: DateTime.UnixEpoch.AddSeconds(2));

        var transformed = (PointerPanZoomEndEvent)@event.Transformed(ScaleThenTranslate());

        Assert.Same(@event, transformed.Original);
        Assert.Equal(new Point(60, 100), transformed.LocalPosition);
        Assert.Equal(@event.Position, transformed.Position);
    }

    [Fact]
    public void PanZoomEvents_Untransformed_HaveNoOriginalAndLocalEqualsGlobal()
    {
        var start = new PointerPanZoomStartEvent(0, new Point(20, 30), DateTime.UnixEpoch);
        var update = new PointerPanZoomUpdateEvent(
            0, new Point(20, 30), DateTime.UnixEpoch, pan: new Point(4, 6), panDelta: new Point(1, 2));
        var end = new PointerPanZoomEndEvent(0, new Point(20, 30), DateTime.UnixEpoch);

        foreach (PointerEvent @event in new PointerEvent[] { start, update, end })
        {
            Assert.Null(@event.Original);
            Assert.Null(@event.Transform);
            Assert.Equal(@event.Position, @event.LocalPosition);
            Assert.Equal(@event.Delta, @event.LocalDelta);
        }

        Assert.Equal(update.Pan, update.LocalPan);
        Assert.Equal(update.PanDelta, update.LocalPanDelta);
    }

    [Fact]
    public void PanZoomEvents_CarryTheTrackpadKindAndOpenNoButtons()
    {
        var start = new PointerPanZoomStartEvent(3, new Point(1, 2), DateTime.UnixEpoch);
        var update = new PointerPanZoomUpdateEvent(3, new Point(1, 2), DateTime.UnixEpoch);
        var end = new PointerPanZoomEndEvent(3, new Point(1, 2), DateTime.UnixEpoch);

        foreach (PointerEvent @event in new PointerEvent[] { start, update, end })
        {
            Assert.Equal(PointerDeviceKind.Trackpad, @event.Kind);
            Assert.Equal(PointerButtons.None, @event.Buttons);
            Assert.False(@event.Down);
            Assert.False(@event is PointerSignalEvent);
        }

        // Dart's defaults: no pan, no zoom, no rotation.
        Assert.Equal(default, update.Pan);
        Assert.Equal(default, update.PanDelta);
        Assert.Equal(1.0, update.Scale);
        Assert.Equal(0.0, update.Rotation);
    }

    [Fact]
    public void TrackpadKind_IsRejectedByTheEventTypesThatCannotCarryIt()
    {
        var position = new Point(1, 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => new PointerDownEvent(
            1, PointerDeviceKind.Trackpad, position, PointerButtons.Primary, DateTime.UnixEpoch));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PointerMoveEvent(
            1, PointerDeviceKind.Trackpad, position, PointerButtons.Primary, down: true, DateTime.UnixEpoch));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PointerUpEvent(
            1, PointerDeviceKind.Trackpad, position, PointerButtons.None, DateTime.UnixEpoch));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PointerCancelEvent(
            1, PointerDeviceKind.Trackpad, position, PointerButtons.None, DateTime.UnixEpoch));
    }

    [Fact]
    public void TrackpadKind_IsAcceptedByHoverAndSignalEvents()
    {
        var position = new Point(1, 2);
        Assert.NotNull(new PointerHoverEvent(
            1, PointerDeviceKind.Trackpad, position, PointerButtons.None, DateTime.UnixEpoch));
        Assert.NotNull(new PointerScrollEvent(
            1, PointerDeviceKind.Trackpad, position, PointerButtons.None, default, DateTime.UnixEpoch));
        Assert.NotNull(new PointerScrollInertiaCancelEvent(
            1, PointerDeviceKind.Trackpad, position, PointerButtons.None, DateTime.UnixEpoch));
    }

    [Fact]
    public void SlopValues_ForTrackpad_UseTheImpreciseConstants()
    {
        Assert.Equal(
            GestureConstants.TouchSlop,
            PointerEventUtils.ComputeHitSlop(PointerDeviceKind.Trackpad, null));
        Assert.Equal(
            GestureConstants.PanSlop,
            PointerEventUtils.ComputePanSlop(PointerDeviceKind.Trackpad, null));
        Assert.Equal(
            GestureConstants.ScaleSlop,
            PointerEventUtils.ComputeScaleSlop(PointerDeviceKind.Trackpad));

        var settings = new DeviceGestureSettings(TouchSlop: 1.0);
        Assert.Equal(1.0, PointerEventUtils.ComputeHitSlop(PointerDeviceKind.Trackpad, settings));
        Assert.Equal(2.0, PointerEventUtils.ComputePanSlop(PointerDeviceKind.Trackpad, settings));
    }

    [Fact]
    public void Binding_RoutesTheWholeSequenceToThePathHitTestedAtTheStart()
    {
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();

        try
        {
            var log = new List<string>();
            var listener = new RenderPointerListener(
                onPointerPanZoomStart: _ => log.Add("start"),
                onPointerPanZoomUpdate: e => log.Add($"update {e.Pan.X},{e.Pan.Y} x{e.Scale}"),
                onPointerPanZoomEnd: _ => log.Add("end"),
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(80, 80), hitSelf: true));
            PipelineOwner pipeline = BuildPipeline(listener);

            binding.HandlePointerEvent(
                pipeline.Root, new PointerPanZoomStartEvent(1, new Point(12, 12), DateTime.UnixEpoch));
            // The update lands outside the listener; it must still reach the cached path, because a
            // pan/zoom gesture keeps the hit-test result it opened with.
            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerPanZoomUpdateEvent(
                    1, new Point(150, 150), DateTime.UnixEpoch, pan: new Point(4, 5), scale: 2.0));
            binding.HandlePointerEvent(
                pipeline.Root, new PointerPanZoomEndEvent(1, new Point(150, 150), DateTime.UnixEpoch));

            Assert.Equal(["start", "update 4,5 x2", "end"], log);
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void Binding_ClosesTheArenaOnStartAndSweepsItOnEnd()
    {
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();

        var recognizer = new PanGestureRecognizer();
        try
        {
            int starts = 0;
            recognizer.OnStart = _ => starts += 1;
            var listener = new RenderPointerListener(
                onPointerPanZoomStart: recognizer.AddPointerPanZoom,
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(80, 80), hitSelf: true));
            PipelineOwner pipeline = BuildPipeline(listener);

            binding.HandlePointerEvent(
                pipeline.Root, new PointerPanZoomStartEvent(1, new Point(12, 12), DateTime.UnixEpoch));
            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerPanZoomUpdateEvent(
                    1, new Point(12, 12), DateTime.UnixEpoch, pan: new Point(50, 0), panDelta: new Point(50, 0)));

            // Sole competitor: closing the arena on the start event let it win once it moved.
            Assert.Equal(1, starts);

            binding.HandlePointerEvent(
                pipeline.Root, new PointerPanZoomEndEvent(1, new Point(12, 12), DateTime.UnixEpoch));

            // The sweep on the end event drained the arena, so a second gesture with the same
            // pointer id starts clean rather than tripping the hit-test cache.
            binding.HandlePointerEvent(
                pipeline.Root, new PointerPanZoomStartEvent(1, new Point(12, 12), DateTime.UnixEpoch));
            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerPanZoomUpdateEvent(
                    1, new Point(12, 12), DateTime.UnixEpoch, pan: new Point(50, 0), panDelta: new Point(50, 0)));

            Assert.Equal(2, starts);
        }
        finally
        {
            recognizer.Dispose();
            binding.ResetForTests();
        }
    }

    private static PipelineOwner BuildPipeline(RenderBox child)
    {
        var root = new RenderView
        {
            Child = child
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(200, 200));
        return pipeline;
    }

    private sealed class FixedHitTestBox(Size size, bool hitSelf) : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(size);
        }

        protected override bool HitTestSelf(Point position)
        {
            return hitSelf;
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }
}
