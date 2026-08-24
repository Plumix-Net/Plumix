using Avalonia;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/gestures/multidrag_test.dart

public sealed class MultiDragGestureRecognizerTests
{
    [Fact]
    public void ImmediateRecognizer_SoleArenaMemberStartsIndependentDragsAtPointerDown()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        var drags = new List<RecordingDrag>();
        var recognizer = new ImmediateMultiDragGestureRecognizer
        {
            OnStart = position =>
            {
                var drag = new RecordingDrag(position);
                drags.Add(drag);
                return drag;
            },
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: recognizer.AddPointer,
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(80, 80), hitSelf: true));
            var pipeline = BuildPipeline(listener);
            DateTime now = DateTime.UtcNow;

            Down(binding, pipeline, pointer: 1, new Point(10, 10), now);
            Down(binding, pipeline, pointer: 2, new Point(20, 20), now);
            Assert.Equal(2, drags.Count);
            Assert.Equal(new Point(10, 10), drags[0].StartPosition);
            Assert.Equal(new Point(20, 20), drags[1].StartPosition);
            Assert.Equal(default(Point), drags[0].Updates.Single().Delta);

            Move(binding, pipeline, pointer: 1, new Point(16, 14), now);
            Assert.Equal(new Point(6, 4), drags[0].Updates[1].Delta);
            Assert.Single(drags[1].Updates);

            Up(binding, pipeline, pointer: 1, new Point(16, 14), now);
            Up(binding, pipeline, pointer: 2, new Point(20, 20), now);
            Assert.Equal(1, drags[0].Ends);
            Assert.Equal(1, drags[1].Ends);
            Assert.Equal(0, drags.Sum(drag => drag.Cancels));
        }
        finally
        {
            recognizer.Dispose();
            binding.ResetForTests();
        }
    }

    [Fact]
    public void HorizontalAndVerticalRecognizers_CompeteUsingAxisSpecificTouchSlop()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        var horizontalDrags = new List<RecordingDrag>();
        var verticalDrags = new List<RecordingDrag>();
        var horizontal = new HorizontalMultiDragGestureRecognizer
        {
            OnStart = position =>
            {
                var drag = new RecordingDrag(position);
                horizontalDrags.Add(drag);
                return drag;
            },
        };
        var vertical = new VerticalMultiDragGestureRecognizer
        {
            OnStart = position =>
            {
                var drag = new RecordingDrag(position);
                verticalDrags.Add(drag);
                return drag;
            },
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: @event =>
                {
                    horizontal.AddPointer(@event);
                    vertical.AddPointer(@event);
                },
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(80, 80), hitSelf: true));
            var pipeline = BuildPipeline(listener);
            DateTime now = DateTime.UtcNow;

            Down(binding, pipeline, pointer: 1, new Point(10, 10), now);
            Assert.Empty(horizontalDrags);
            Assert.Empty(verticalDrags);

            Move(binding, pipeline, pointer: 1, new Point(14, 40), now);
            RecordingDrag verticalDrag = Assert.Single(verticalDrags);
            Assert.Empty(horizontalDrags);
            Assert.Equal(new Point(10, 10), verticalDrag.StartPosition);
            Assert.Equal(new Point(4, 30), verticalDrag.Updates.Single().Delta);
            Up(binding, pipeline, pointer: 1, new Point(14, 40), now);
            Assert.Equal(1, verticalDrag.Ends);

            Down(binding, pipeline, pointer: 2, new Point(10, 10), now);
            Move(binding, pipeline, pointer: 2, new Point(40, 14), now);
            RecordingDrag horizontalDrag = Assert.Single(horizontalDrags);
            Assert.Single(verticalDrags);
            Assert.Equal(new Point(30, 4), horizontalDrag.Updates.Single().Delta);
            Up(binding, pipeline, pointer: 2, new Point(40, 14), now);
            Assert.Equal(1, horizontalDrag.Ends);
        }
        finally
        {
            horizontal.Dispose();
            vertical.Dispose();
            binding.ResetForTests();
        }
    }

    [Fact]
    public void DelayedRecognizer_RejectsMovementPastTouchSlopBeforeDeadline()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        using var timers = new FakeGestureTimers();
        int starts = 0;
        var recognizer = new DelayedMultiDragGestureRecognizer(TimeSpan.FromSeconds(1))
        {
            OnStart = _ =>
            {
                starts++;
                return new RecordingDrag(default);
            },
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: recognizer.AddPointer,
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(80, 80), hitSelf: true));
            var pipeline = BuildPipeline(listener);
            DateTime now = DateTime.UtcNow;

            Down(binding, pipeline, pointer: 3, new Point(10, 10), now);
            Move(binding, pipeline, pointer: 3, new Point(40, 10), now);
            timers.Elapse(TimeSpan.FromSeconds(2));
            Up(binding, pipeline, pointer: 3, new Point(40, 10), now);

            Assert.Equal(0, starts);
        }
        finally
        {
            recognizer.Dispose();
            binding.ResetForTests();
        }
    }

    [Fact]
    public void DelayedRecognizer_DeadlineStartsTheDragAndDeliversBufferedMovement()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        using var timers = new FakeGestureTimers();
        var drags = new List<RecordingDrag>();
        var recognizer = new DelayedMultiDragGestureRecognizer(TimeSpan.FromSeconds(1))
        {
            OnStart = position =>
            {
                var drag = new RecordingDrag(position);
                drags.Add(drag);
                return drag;
            },
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: recognizer.AddPointer,
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(80, 80), hitSelf: true));
            var pipeline = BuildPipeline(listener);
            DateTime now = DateTime.UtcNow;

            Down(binding, pipeline, pointer: 4, new Point(10, 10), now);
            Move(binding, pipeline, pointer: 4, new Point(15, 10), now);
            Assert.Empty(drags);

            timers.Elapse(TimeSpan.FromSeconds(1));
            Assert.Single(drags);
            Assert.Equal(new Point(10, 10), drags[0].StartPosition);
            Assert.Equal(new Point(5, 0), drags[0].Updates.Single().Delta);

            Move(binding, pipeline, pointer: 4, new Point(45, 10), now);
            Assert.Equal(new Point(30, 0), drags[0].Updates[1].Delta);
            Up(binding, pipeline, pointer: 4, new Point(45, 10), now);
            Assert.Equal(1, drags[0].Ends);
        }
        finally
        {
            recognizer.Dispose();
            binding.ResetForTests();
        }
    }

    private static void Down(
        GestureBinding binding,
        TestPipeline pipeline,
        int pointer,
        Point position,
        DateTime timestamp) => binding.HandlePointerEvent(
            pipeline.Root,
            new PointerDownEvent(
                pointer,
                PointerDeviceKind.Touch,
                position,
                PointerButtons.Primary,
                timestamp));

    private static void Move(
        GestureBinding binding,
        TestPipeline pipeline,
        int pointer,
        Point position,
        DateTime timestamp) => binding.HandlePointerEvent(
            pipeline.Root,
            new PointerMoveEvent(
                pointer,
                PointerDeviceKind.Touch,
                position,
                PointerButtons.Primary,
                down: true,
                timestamp));

    private static void Up(
        GestureBinding binding,
        TestPipeline pipeline,
        int pointer,
        Point position,
        DateTime timestamp) => binding.HandlePointerEvent(
            pipeline.Root,
            new PointerUpEvent(
                pointer,
                PointerDeviceKind.Touch,
                position,
                PointerButtons.None,
                timestamp));

    private static TestPipeline BuildPipeline(RenderBox child)
    {
        var root = new RenderView { Child = child };
        var owner = new PipelineOwner(root);
        owner.Attach(root);
        owner.RequestLayout();
        owner.FlushLayout(new Size(100, 100));
        return new TestPipeline(root, owner);
    }

    private sealed record TestPipeline(RenderView Root, PipelineOwner Owner);

    private sealed class RecordingDrag : Drag
    {
        public RecordingDrag(Point startPosition)
        {
            StartPosition = startPosition;
        }

        public Point StartPosition { get; }

        public List<DragUpdateDetails> Updates { get; } = [];

        public int Ends { get; private set; }

        public int Cancels { get; private set; }

        public override void Update(DragUpdateDetails details) => Updates.Add(details);

        public override void End(DragEndDetails details) => Ends++;

        public override void Cancel() => Cancels++;
    }

    private sealed class FixedHitTestBox : RenderBox
    {
        private readonly Size _desiredSize;
        private readonly bool _hitSelf;

        public FixedHitTestBox(Size desiredSize, bool hitSelf)
        {
            _desiredSize = desiredSize;
            _hitSelf = hitSelf;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_desiredSize);
        }

        protected override bool HitTestSelf(Point position) => _hitSelf;

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }
}
