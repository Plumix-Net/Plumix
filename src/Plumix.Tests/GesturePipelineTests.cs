using Avalonia;
using Avalonia.Media;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/binding.dart; flutter/packages/flutter/lib/src/gestures/arena.dart; flutter/packages/flutter/lib/src/gestures/recognizer.dart (parity regression tests)

namespace Plumix.Tests;

public sealed class GesturePipelineTests
{
    [Fact]
    public void RenderTransform_HitTest_UsesInverseTransform()
    {
        var child = new FixedHitTestBox(new Size(20, 20), hitSelf: true);
        var transform = new RenderTransform(Matrix.CreateTranslation(-10, 0), child);
        var pipeline = BuildPipeline(transform);

        var insideResult = new BoxHitTestResult();
        Assert.True(pipeline.Root.HitTest(insideResult, new Point(5, 10)));
        Assert.Contains(insideResult.Path, entry => ReferenceEquals(entry.Target, child));

        var outsideResult = new BoxHitTestResult();
        Assert.False(pipeline.Root.HitTest(outsideResult, new Point(15, 10)));
    }

    [Fact]
    public void RenderClipRect_HitTest_RejectsOutsideEffectiveClip()
    {
        var child = new FixedHitTestBox(new Size(80, 80), hitSelf: true);
        var clip = new RenderClipRect(child)
        {
            ClipRect = new Rect(0, 0, 20, 20)
        };

        var pipeline = BuildPipeline(clip);

        var insideResult = new BoxHitTestResult();
        Assert.True(pipeline.Root.HitTest(insideResult, new Point(10, 10)));

        var outsideResult = new BoxHitTestResult();
        Assert.False(pipeline.Root.HitTest(outsideResult, new Point(40, 40)));
    }

    [Fact]
    public void RenderPointerListener_Translucent_HitsWithoutChildTarget()
    {
        var listener = new RenderPointerListener(
            behavior: HitTestBehavior.Translucent,
            child: new FixedHitTestBox(new Size(80, 80), hitSelf: false));

        var pipeline = BuildPipeline(listener);

        var translucentResult = new BoxHitTestResult();
        Assert.True(pipeline.Root.HitTest(translucentResult, new Point(10, 10)));
        Assert.Contains(translucentResult.Path, entry => ReferenceEquals(entry.Target, listener));

        listener.Behavior = HitTestBehavior.DeferToChild;
        var deferResult = new BoxHitTestResult();
        Assert.False(pipeline.Root.HitTest(deferResult, new Point(10, 10)));
    }

    [Fact]
    public void GestureBinding_TapRecognizer_InvokesOnTapOnPointerUp()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        var taps = 0;
        var recognizer = new TapGestureRecognizer
        {
            OnTap = () => taps += 1
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: recognizer.AddPointer,
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(80, 80), hitSelf: true));
            var pipeline = BuildPipeline(listener);

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerDownEvent(
                    pointer: 1,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(12, 12),
                    buttons: PointerButtons.Primary,
                    timestampUtc: DateTime.UtcNow));

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerUpEvent(
                    pointer: 1,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(12, 12),
                    buttons: PointerButtons.None,
                    timestampUtc: DateTime.UtcNow));

            Assert.Equal(1, taps);
        }
        finally
        {
            recognizer.Dispose();
            binding.ResetForTests();
        }
    }

    [Fact]
    public void GestureBinding_HorizontalDragRecognizer_ProducesPrimaryDelta()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        var deltas = new List<double>();
        var recognizer = new HorizontalDragGestureRecognizer
        {
            OnUpdate = details => deltas.Add(details.PrimaryDelta)
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: recognizer.AddPointer,
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(120, 80), hitSelf: true));
            var pipeline = BuildPipeline(listener);

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerDownEvent(
                    pointer: 7,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(8, 8),
                    buttons: PointerButtons.Primary,
                    timestampUtc: DateTime.UtcNow));

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerMoveEvent(
                    pointer: 7,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(40, 10),
                    buttons: PointerButtons.Primary,
                    down: true,
                    timestampUtc: DateTime.UtcNow));

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerMoveEvent(
                    pointer: 7,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(70, 11),
                    buttons: PointerButtons.Primary,
                    down: true,
                    timestampUtc: DateTime.UtcNow));

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerUpEvent(
                    pointer: 7,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(70, 11),
                    buttons: PointerButtons.None,
                    timestampUtc: DateTime.UtcNow));

            Assert.NotEmpty(deltas);
            Assert.True(deltas.Sum() > 0);
        }
        finally
        {
            recognizer.Dispose();
            binding.ResetForTests();
        }
    }

    [Fact]
    public void GestureBinding_HorizontalDragRecognizer_ReportsPrimaryVelocityInPixelsPerSecond()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        double? velocity = null;
        var recognizer = new HorizontalDragGestureRecognizer
        {
            OnEnd = details => velocity = details.PrimaryVelocity
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: recognizer.AddPointer,
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(160, 80), hitSelf: true));
            var pipeline = BuildPipeline(listener);
            var start = new DateTime(2026, 4, 12, 8, 0, 0, DateTimeKind.Utc);

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerDownEvent(
                    pointer: 8,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(10, 10),
                    buttons: PointerButtons.Primary,
                    timestampUtc: start));

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerMoveEvent(
                    pointer: 8,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(50, 10),
                    buttons: PointerButtons.Primary,
                    down: true,
                    timestampUtc: start.AddMilliseconds(100)));

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerUpEvent(
                    pointer: 8,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(80, 10),
                    buttons: PointerButtons.None,
                    timestampUtc: start.AddMilliseconds(150)));

            Assert.True(velocity.HasValue);
            Assert.Equal(600, velocity.Value, precision: 3);
        }
        finally
        {
            recognizer.Dispose();
            binding.ResetForTests();
        }
    }

    [Fact]
    public void GestureBinding_ArenaConflict_HorizontalDragBeatsTap()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        var taps = 0;
        var dragUpdates = 0;
        var tap = new TapGestureRecognizer
        {
            OnTap = () => taps += 1
        };
        var drag = new HorizontalDragGestureRecognizer
        {
            OnUpdate = _ => dragUpdates += 1
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: @event =>
                {
                    tap.AddPointer(@event);
                    drag.AddPointer(@event);
                },
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(160, 80), hitSelf: true));
            var pipeline = BuildPipeline(listener);

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerDownEvent(3, PointerDeviceKind.Mouse, new Point(10, 10), PointerButtons.Primary, DateTime.UtcNow));

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerMoveEvent(3, PointerDeviceKind.Mouse, new Point(90, 12), PointerButtons.Primary, down: true, DateTime.UtcNow));

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerUpEvent(3, PointerDeviceKind.Mouse, new Point(90, 12), PointerButtons.None, DateTime.UtcNow));

            Assert.Equal(0, taps);
            Assert.True(dragUpdates > 0);
        }
        finally
        {
            tap.Dispose();
            drag.Dispose();
            binding.ResetForTests();
        }
    }

    [Fact]
    public void GestureBinding_PointerSignal_DispatchesToListener()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        Point? scrollDelta = null;
        var listener = new RenderPointerListener(
            behavior: HitTestBehavior.Opaque,
            onPointerSignal: @event =>
            {
                if (@event is PointerScrollEvent scroll)
                {
                    scrollDelta = scroll.ScrollDelta;
                }
            },
            child: new FixedHitTestBox(new Size(140, 80), hitSelf: true));
        var pipeline = BuildPipeline(listener);

        binding.HandlePointerEvent(
            pipeline.Root,
            new PointerScrollEvent(
                pointer: 44,
                kind: PointerDeviceKind.Mouse,
                position: new Point(30, 30),
                buttons: PointerButtons.None,
                scrollDelta: new Point(0, -1),
                timestampUtc: DateTime.UtcNow));

        Assert.Equal(new Point(0, -1), scrollDelta);
        binding.ResetForTests();
    }

    [Fact]
    public void GestureBinding_HoverDispatchesPointerEnterAndPointerExitTransitions()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        var enters = 0;
        var exits = 0;
        var hovers = 0;

        var listener = new RenderPointerListener(
            behavior: HitTestBehavior.Opaque,
            onPointerEnter: _ => enters += 1,
            onPointerExit: _ => exits += 1,
            onPointerHover: _ => hovers += 1,
            child: new FixedHitTestBox(new Size(80, 80), hitSelf: true));
        var pipeline = BuildPipeline(listener);

        binding.HandlePointerEvent(
            pipeline.Root,
            new PointerHoverEvent(
                pointer: 91,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 10),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow));

        binding.HandlePointerEvent(
            pipeline.Root,
            new PointerHoverEvent(
                pointer: 91,
                kind: PointerDeviceKind.Mouse,
                position: new Point(14, 14),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow));

        binding.HandlePointerEvent(
            pipeline.Root,
            new PointerHoverEvent(
                pointer: 91,
                kind: PointerDeviceKind.Mouse,
                position: new Point(140, 140),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow));

        Assert.Equal(1, enters);
        Assert.Equal(1, exits);
        Assert.Equal(2, hovers);
        binding.ResetForTests();
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

    private sealed class FixedHitTestBox : RenderBox
    {
        private readonly Size _size;
        private readonly bool _hitSelf;

        public FixedHitTestBox(Size size, bool hitSelf)
        {
            _size = size;
            _hitSelf = hitSelf;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        protected override bool HitTestSelf(Point position)
        {
            return _hitSelf;
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }
}
