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
        var transform = new RenderTransform(Matrix4.TranslationValues(-10, 0, 0.0), child);
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
        var clip = new RenderClipRect(child, clipper: new FixedRectClipper(new Rect(0, 0, 20, 20)));

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
    public void RenderIgnorePointer_HitTest_SkipsSubtreeWhenIgnoring()
    {
        var child = new FixedHitTestBox(new Size(80, 80), hitSelf: true);
        var ignorePointer = new RenderIgnorePointer(ignoring: true, child: child);
        var pipeline = BuildPipeline(ignorePointer);

        var ignoredResult = new BoxHitTestResult();
        Assert.False(pipeline.Root.HitTest(ignoredResult, new Point(10, 10)));
        Assert.DoesNotContain(ignoredResult.Path, entry => ReferenceEquals(entry.Target, child));

        ignorePointer.Ignoring = false;
        var activeResult = new BoxHitTestResult();
        Assert.True(pipeline.Root.HitTest(activeResult, new Point(10, 10)));
        Assert.Contains(activeResult.Path, entry => ReferenceEquals(entry.Target, child));
        Assert.Contains(activeResult.Path, entry => ReferenceEquals(entry.Target, ignorePointer));
    }

    [Fact]
    public void RenderAbsorbPointer_HitTest_TerminatesAtItselfWhenAbsorbing()
    {
        var child = new FixedHitTestBox(new Size(80, 80), hitSelf: true);
        var absorbPointer = new RenderAbsorbPointer(absorbing: true, child: child);
        var pipeline = BuildPipeline(absorbPointer);

        var absorbedResult = new BoxHitTestResult();
        Assert.True(pipeline.Root.HitTest(absorbedResult, new Point(10, 10)));
        Assert.Contains(absorbedResult.Path, entry => ReferenceEquals(entry.Target, absorbPointer));
        Assert.DoesNotContain(absorbedResult.Path, entry => ReferenceEquals(entry.Target, child));

        absorbPointer.Absorbing = false;
        var activeResult = new BoxHitTestResult();
        Assert.True(pipeline.Root.HitTest(activeResult, new Point(10, 10)));
        Assert.Contains(activeResult.Path, entry => ReferenceEquals(entry.Target, child));
        Assert.Contains(activeResult.Path, entry => ReferenceEquals(entry.Target, absorbPointer));
    }

    [Fact]
    public void GestureBinding_TapRecognizer_InvokesOnTapOnPointerUp()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        int taps = 0;
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
    public void GestureBinding_DoubleTapRecognizer_BeatsCompetingTapOnSecondTap()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        using var timers = new FakeGestureTimers();
        int taps = 0;
        int doubleTaps = 0;
        int tapCancels = 0;
        var tap = new TapGestureRecognizer
        {
            OnTap = () => taps += 1,
            OnTapCancel = () => tapCancels += 1,
        };
        var doubleTap = new DoubleTapGestureRecognizer
        {
            OnDoubleTap = () => doubleTaps += 1,
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: @event =>
                {
                    tap.AddPointer(@event);
                    doubleTap.AddPointer(@event);
                },
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(80, 80), hitSelf: true));
            var pipeline = BuildPipeline(listener);
            var now = DateTime.UtcNow;
            for (int pointer = 21; pointer <= 22; pointer++)
            {
                binding.HandlePointerEvent(pipeline.Root, new PointerDownEvent(
                    pointer, PointerDeviceKind.Mouse, new Point(12, 12), PointerButtons.Primary, now));
                binding.HandlePointerEvent(pipeline.Root, new PointerUpEvent(
                    pointer, PointerDeviceKind.Mouse, new Point(12, 12), PointerButtons.None, now.AddMilliseconds(20)));
                // Past kDoubleTapMinTime (40 ms), well inside kDoubleTapTimeout (300 ms).
                timers.Elapse(TimeSpan.FromMilliseconds(80));
                now = now.AddMilliseconds(80);
            }

            Assert.Equal(1, doubleTaps);
            Assert.Equal(0, taps);
        }
        finally
        {
            tap.Dispose();
            doubleTap.Dispose();
            binding.ResetForTests();
        }
    }

    [Fact]
    public void GestureBinding_DoubleTapRecognizer_TimeoutReleasesHeldArenaToTheTap()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        using var timers = new FakeGestureTimers();
        int taps = 0;
        int doubleTaps = 0;
        var tap = new TapGestureRecognizer
        {
            OnTap = () => taps += 1,
        };
        var doubleTap = new DoubleTapGestureRecognizer
        {
            OnDoubleTap = () => doubleTaps += 1,
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: @event =>
                {
                    tap.AddPointer(@event);
                    doubleTap.AddPointer(@event);
                },
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(80, 80), hitSelf: true));
            var pipeline = BuildPipeline(listener);
            var now = DateTime.UtcNow;
            binding.HandlePointerEvent(pipeline.Root, new PointerDownEvent(
                31, PointerDeviceKind.Mouse, new Point(12, 12), PointerButtons.Primary, now));
            binding.HandlePointerEvent(pipeline.Root, new PointerUpEvent(
                31, PointerDeviceKind.Mouse, new Point(12, 12), PointerButtons.None, now.AddMilliseconds(20)));

            // The double tap holds the arena past the up; the tap fires only after the timeout.
            Assert.Equal(0, taps);
            timers.Elapse(GestureConstants.DoubleTapTimeout + TimeSpan.FromMilliseconds(1));
            Assert.Equal(1, taps);
            Assert.Equal(0, doubleTaps);
        }
        finally
        {
            tap.Dispose();
            doubleTap.Dispose();
            binding.ResetForTests();
        }
    }

    [Fact]
    public void GestureBinding_TapRecognizer_ReportsPrimaryUpAndSecondaryLifecycle()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        var events = new List<string>();
        var recognizer = new TapGestureRecognizer
        {
            OnTapUp = _ => events.Add("primary-up"),
            OnTap = () => events.Add("primary"),
            OnSecondaryTapDown = _ => events.Add("secondary-down"),
            OnSecondaryTapUp = _ => events.Add("secondary-up"),
            OnSecondaryTap = () => events.Add("secondary"),
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: recognizer.AddPointer,
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(80, 80), hitSelf: true));
            var pipeline = BuildPipeline(listener);
            var now = DateTime.UtcNow;

            binding.HandlePointerEvent(pipeline.Root, new PointerDownEvent(
                23, PointerDeviceKind.Mouse, new Point(12, 12), PointerButtons.Primary, now));
            binding.HandlePointerEvent(pipeline.Root, new PointerUpEvent(
                23, PointerDeviceKind.Mouse, new Point(12, 12), PointerButtons.None, now.AddMilliseconds(20)));
            binding.HandlePointerEvent(pipeline.Root, new PointerDownEvent(
                24, PointerDeviceKind.Mouse, new Point(12, 12), PointerButtons.Secondary, now.AddMilliseconds(40)));
            binding.HandlePointerEvent(pipeline.Root, new PointerUpEvent(
                24, PointerDeviceKind.Mouse, new Point(12, 12), PointerButtons.None, now.AddMilliseconds(60)));

            Assert.Equal(["primary-up", "primary", "secondary-down", "secondary-up", "secondary"], events);
        }
        finally
        {
            recognizer.Dispose();
            binding.ResetForTests();
        }
    }

    [Fact]
    public void GestureBinding_LongPressRecognizer_ReportsUpAfterAcceptedPress()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        var events = new List<string>();
        using var timers = new FakeGestureTimers();
        var recognizer = new LongPressGestureRecognizer(duration: TimeSpan.FromMilliseconds(500))
        {
            OnLongPress = () => events.Add("long-press"),
            OnLongPressUp = () => events.Add("long-press-up"),
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: recognizer.AddPointer,
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(80, 80), hitSelf: true));
            var pipeline = BuildPipeline(listener);
            var now = DateTime.UtcNow;
            binding.HandlePointerEvent(pipeline.Root, new PointerDownEvent(
                25, PointerDeviceKind.Mouse, new Point(12, 12), PointerButtons.Primary, now));
            timers.Elapse(TimeSpan.FromMilliseconds(500));
            binding.HandlePointerEvent(pipeline.Root, new PointerUpEvent(
                25, PointerDeviceKind.Mouse, new Point(12, 12), PointerButtons.None, now.AddMilliseconds(560)));

            Assert.Equal(["long-press", "long-press-up"], events);
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
            OnUpdate = details => deltas.Add(details.PrimaryDelta ?? 0.0)
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
                    position: new Point(34, 10),
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
    public void GestureBinding_DragRecognizer_StartsAtDownWhenItWinsTheArenaImmediately()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        var log = new List<string>();
        Point? startPosition = null;
        double totalDelta = 0.0;
        var recognizer = new VerticalDragGestureRecognizer
        {
            OnStart = details =>
            {
                log.Add("start");
                startPosition = details.GlobalPosition;
            },
            OnUpdate = details => totalDelta += details.PrimaryDelta ?? 0.0,
            OnEnd = _ => log.Add("end"),
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: recognizer.AddPointer,
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(160, 80), hitSelf: true));
            var pipeline = BuildPipeline(listener);
            DateTime start = new(2026, 8, 26, 8, 0, 0, DateTimeKind.Utc);

            binding.HandlePointerEvent(pipeline.Root, new PointerDownEvent(
                10,
                PointerDeviceKind.Touch,
                new Point(20, 20),
                PointerButtons.Primary,
                start));
            binding.HandlePointerEvent(pipeline.Root, new PointerMoveEvent(
                10,
                PointerDeviceKind.Touch,
                new Point(20, 25),
                PointerButtons.Primary,
                true,
                start.AddMilliseconds(20)));
            binding.HandlePointerEvent(pipeline.Root, new PointerUpEvent(
                10,
                PointerDeviceKind.Touch,
                new Point(20, 25),
                PointerButtons.None,
                start.AddMilliseconds(40)));

            Assert.False(recognizer.OnlyAcceptDragOnThreshold);
            Assert.Equal(new Point(20, 20), startPosition);
            Assert.Equal(5.0, totalDelta);
            Assert.Equal(["start", "end"], log);
        }
        finally
        {
            recognizer.Dispose();
            binding.ResetForTests();
        }
    }

    [Theory]
    [InlineData(DragStartBehavior.Start, 45.0, 0.0)]
    [InlineData(DragStartBehavior.Down, 20.0, 25.0)]
    public void GestureBinding_DragRecognizer_ThresholdModeBuffersAccordingToDragStartBehavior(
        DragStartBehavior dragStartBehavior,
        double expectedStartY,
        double expectedInitialDelta)
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        Point? startPosition = null;
        var deltas = new List<double>();
        var recognizer = new VerticalDragGestureRecognizer
        {
            OnlyAcceptDragOnThreshold = true,
            DragStartBehavior = dragStartBehavior,
            OnStart = details => startPosition = details.GlobalPosition,
            OnUpdate = details => deltas.Add(details.PrimaryDelta ?? 0.0),
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: recognizer.AddPointer,
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(160, 80), hitSelf: true));
            var pipeline = BuildPipeline(listener);
            DateTime start = new(2026, 8, 26, 8, 0, 0, DateTimeKind.Utc);

            binding.HandlePointerEvent(pipeline.Root, new PointerDownEvent(
                11,
                PointerDeviceKind.Touch,
                new Point(20, 20),
                PointerButtons.Primary,
                start));

            Assert.Null(startPosition);

            binding.HandlePointerEvent(pipeline.Root, new PointerMoveEvent(
                11,
                PointerDeviceKind.Touch,
                new Point(20, 45),
                PointerButtons.Primary,
                true,
                start.AddMilliseconds(20)));

            Assert.Equal(new Point(20, expectedStartY), startPosition);
            Assert.Equal(expectedInitialDelta, deltas.Sum());
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
        var velocityTracker = new RecordingVelocityTracker(PointerDeviceKind.Mouse);
        var recognizer = new HorizontalDragGestureRecognizer
        {
            OnEnd = details => velocity = details.PrimaryVelocity,
            VelocityTrackerBuilder = _ => velocityTracker,
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
                    position: new Point(34, 10),
                    buttons: PointerButtons.Primary,
                    down: true,
                    timestampUtc: start.AddMilliseconds(30)));

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerMoveEvent(
                    pointer: 8,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(58, 10),
                    buttons: PointerButtons.Primary,
                    down: true,
                    timestampUtc: start.AddMilliseconds(60)));

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerUpEvent(
                    pointer: 8,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(82, 10),
                    buttons: PointerButtons.None,
                    timestampUtc: start.AddMilliseconds(90)));

            Assert.True(velocity.HasValue);
            Assert.Equal(new[] { 10.0, 34.0, 58.0 }, velocityTracker.Positions.Select(point => point.X));
            Assert.Equal(800, velocity.Value, precision: 3);
        }
        finally
        {
            recognizer.Dispose();
            binding.ResetForTests();
        }
    }

    [Theory]
    // The unconfigured recognizer reports the estimate as-is.
    [InlineData(null, null, null, 800.0)]
    // A floor above the estimate turns the release into a plain stop, not a fling.
    [InlineData(null, 900.0, null, 0.0)]
    // A floor below it keeps the fling.
    [InlineData(null, 100.0, null, 800.0)]
    // A ceiling clamps the reported magnitude.
    [InlineData(null, null, 250.0, 250.0)]
    // A distance floor longer than the drag also rejects the fling.
    [InlineData(500.0, null, null, 0.0)]
    public void GestureBinding_HorizontalDragRecognizer_HonorsTheFlingTuningValues(
        double? minFlingDistance,
        double? minFlingVelocity,
        double? maxFlingVelocity,
        double expectedPrimaryVelocity)
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        double? velocity = null;
        var recognizer = new HorizontalDragGestureRecognizer
        {
            OnEnd = details => velocity = details.PrimaryVelocity,
            MinFlingDistance = minFlingDistance,
            MinFlingVelocity = minFlingVelocity,
            MaxFlingVelocity = maxFlingVelocity,
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: recognizer.AddPointer,
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(160, 80), hitSelf: true));
            var pipeline = BuildPipeline(listener);
            var start = new DateTime(2026, 8, 11, 8, 0, 0, DateTimeKind.Utc);

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerDownEvent(
                    pointer: 9,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(10, 10),
                    buttons: PointerButtons.Primary,
                    timestampUtc: start));

            for (int step = 1; step <= 3; step++)
            {
                binding.HandlePointerEvent(
                    pipeline.Root,
                    new PointerMoveEvent(
                        pointer: 9,
                        kind: PointerDeviceKind.Mouse,
                        position: new Point(10 + (24 * step), 10),
                        buttons: PointerButtons.Primary,
                        down: true,
                        timestampUtc: start.AddMilliseconds(30 * step)));
            }

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerUpEvent(
                    pointer: 9,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(106, 10),
                    buttons: PointerButtons.None,
                    timestampUtc: start.AddMilliseconds(120)));

            Assert.True(velocity.HasValue);
            Assert.Equal(expectedPrimaryVelocity, velocity!.Value, precision: 3);
        }
        finally
        {
            recognizer.Dispose();
            binding.ResetForTests();
        }
    }

    [Fact]
    public void GestureBinding_ThresholdOnlyDragRecognizer_ReportsCancelWhenThePointerNeverDrags()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        var log = new List<string>();
        var recognizer = new VerticalDragGestureRecognizer
        {
            OnlyAcceptDragOnThreshold = true,
            OnDown = _ => log.Add("down"),
            OnStart = _ => log.Add("start"),
            OnEnd = _ => log.Add("end"),
            OnCancel = () => log.Add("cancel"),
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: recognizer.AddPointer,
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(160, 80), hitSelf: true));
            var pipeline = BuildPipeline(listener);
            var start = new DateTime(2026, 8, 11, 8, 0, 0, DateTimeKind.Utc);

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerDownEvent(
                    pointer: 10,
                    kind: PointerDeviceKind.Touch,
                    position: new Point(20, 20),
                    buttons: PointerButtons.Primary,
                    timestampUtc: start));
            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerUpEvent(
                    pointer: 10,
                    kind: PointerDeviceKind.Touch,
                    position: new Point(20, 22),
                    buttons: PointerButtons.None,
                    timestampUtc: start.AddMilliseconds(40)));

            // A tap must complete the down with exactly one cancel, so anything holding state from
            // the down (a scrollable's hold activity, for example) is released.
            Assert.Equal(["down", "cancel"], log);
        }
        finally
        {
            recognizer.Dispose();
            binding.ResetForTests();
        }
    }

    private sealed class RecordingVelocityTracker(PointerDeviceKind kind) : VelocityTracker(kind)
    {
        public List<Point> Positions { get; } = [];

        public override void AddPosition(DateTime timestampUtc, Point position)
        {
            Positions.Add(position);
            base.AddPosition(timestampUtc, position);
        }
    }

    [Fact]
    public void GestureBinding_ArenaConflict_HorizontalDragBeatsTap()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        int taps = 0;
        int dragUpdates = 0;
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
                new PointerMoveEvent(
                    3,
                    PointerDeviceKind.Mouse,
                    new Point(100, 12),
                    PointerButtons.Primary,
                    down: true,
                    DateTime.UtcNow));

            binding.HandlePointerEvent(
                pipeline.Root,
                new PointerUpEvent(
                    3,
                    PointerDeviceKind.Mouse,
                    new Point(100, 12),
                    PointerButtons.None,
                    DateTime.UtcNow));

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

        int enters = 0;
        int exits = 0;
        int hovers = 0;

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
