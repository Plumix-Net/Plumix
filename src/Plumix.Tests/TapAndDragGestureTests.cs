using Avalonia;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/tap_and_drag.dart
// Mirrors flutter/packages/flutter/test/gestures/tap_and_drag_test.dart.

namespace Plumix.Tests;

public sealed class TapAndDragGestureTests : IDisposable
{
    private static readonly TimeSpan ConsecutiveTapDelay = TimeSpan.FromMilliseconds(150);

    private readonly FakeGestureTimers _timers = new();
    private readonly GestureBinding _binding = GestureBinding.Instance;
    private readonly List<string> _events = [];

    public TapAndDragGestureTests()
    {
        _binding.ResetForTests();
    }

    public void Dispose()
    {
        _binding.ResetForTests();
        _timers.Dispose();
    }

    [Fact]
    public void RecognizesConsecutiveTapsAndResetsAfterTheTimeout()
    {
        using var recognizer = CreatePanRecognizer();
        var pipeline = Attach(recognizer);

        Tap(pipeline, 1, new Point(10, 10));
        Assert.Equal(["down#1", "up#1"], _events);

        _events.Clear();
        _timers.Elapse(ConsecutiveTapDelay);
        Tap(pipeline, 2, new Point(12, 12));
        Assert.Equal(["down#2", "up#2"], _events);

        _events.Clear();
        _timers.Elapse(ConsecutiveTapDelay);
        Tap(pipeline, 3, new Point(12, 12));
        Assert.Equal(["down#3", "up#3"], _events);

        // A gap longer than kDoubleTapTimeout restarts the series.
        _events.Clear();
        _timers.Elapse(TimeSpan.FromMilliseconds(1000));
        Tap(pipeline, 4, new Point(12, 12));
        Assert.Equal(["down#1", "up#1"], _events);
    }

    [Fact]
    public void ResetsTheSeriesWhenTapsAreFartherApartThanTheDoubleTapSlop()
    {
        using var recognizer = CreatePanRecognizer();
        var pipeline = Attach(recognizer);

        Tap(pipeline, 1, new Point(10, 10));
        _events.Clear();
        _timers.Elapse(TimeSpan.FromMilliseconds(100));
        // (130,130) is ~169.7 logical pixels away, past the 100.0 kDoubleTapSlop.
        Tap(pipeline, 2, new Point(130, 130));

        Assert.Equal(["down#1", "up#1"], _events);
    }

    [Fact]
    public void ResetsWhenTheConsecutiveTapCountReachesMaxConsecutiveTap()
    {
        using var recognizer = CreatePanRecognizer();
        var pipeline = Attach(recognizer);

        Tap(pipeline, 1, new Point(10, 10));
        Tap(pipeline, 2, new Point(10, 10));
        Tap(pipeline, 3, new Point(10, 10));
        _events.Clear();
        Tap(pipeline, 4, new Point(10, 10));

        Assert.Equal(["down#1", "up#1"], _events);
    }

    [Fact]
    public void RecognizesADragPastThePanSlopAndCarriesTheTapCount()
    {
        using var recognizer = CreatePanRecognizer();
        var pipeline = Attach(recognizer);

        Tap(pipeline, 1, new Point(10, 10));
        _timers.Elapse(ConsecutiveTapDelay);
        Tap(pipeline, 2, new Point(10, 10));
        _events.Clear();

        Down(pipeline, 3, new Point(10, 10));
        // |(30,35)| is ~46.1, past the 36.0 pan slop.
        Move(pipeline, 3, new Point(40, 45));
        Up(pipeline, 3, new Point(40, 45));

        Assert.Equal(["down#3", "panstart#3", "panupdate#3", "panend#3"], _events);
    }

    [Fact]
    public void ReportsADragWithNoUpdateWhenThePointerPassesTapToleranceButNotTheDragMinimum()
    {
        using var recognizer = CreatePanRecognizer();
        var pipeline = Attach(recognizer);

        Down(pipeline, 1, new Point(10, 10));
        // The move carries a zero delta, so the accumulated drag distance stays 0 while the
        // straight-line distance from the origin (21.2) passes the 18.0 hit slop.
        _binding.HandlePointerEvent(pipeline.Root, new PointerMoveEvent(
            pointer: 1,
            kind: PointerDeviceKind.Touch,
            position: new Point(25, 25),
            buttons: PointerButtons.Primary,
            down: true,
            timestampUtc: DateTime.UtcNow));
        Up(pipeline, 1, new Point(25, 25));

        Assert.Equal(["down#1", "panstart#1", "panend#1"], _events);
    }

    [Fact]
    public void HorizontalRecognizerAcceptsAnOffAxisDragOnceItHasWonTheArena()
    {
        using var recognizer = CreateHorizontalRecognizer();
        var pipeline = Attach(recognizer);

        Down(pipeline, 1, new Point(10, 10));
        // The horizontal component (15) stays under the 18.0 hit slop, but the two-axis distance
        // (~38.1) passes the 36.0 pan slop and the recognizer is alone in the arena.
        Move(pipeline, 1, new Point(25, 45));
        Up(pipeline, 1, new Point(25, 45));

        Assert.Equal(
            ["down#1", "horizontaldragstart#1", "horizontaldragupdate#1", "horizontaldragend#1"],
            _events);
    }

    [Fact]
    public void MouseDragPastThePrecisePanSlopBeatsATapRecognizer()
    {
        using var recognizer = CreatePanRecognizer();
        using var taps = new TapGestureRecognizer();
        int tapCount = 0;
        taps.OnTap = () => tapCount += 1;

        var pipeline = Attach(recognizer, taps);

        Down(pipeline, 1, new Point(10, 10), PointerDeviceKind.Mouse);
        // |(5,5)| is ~7.07, past the 2.0 precise-pointer pan slop.
        Move(pipeline, 1, new Point(15, 15), PointerDeviceKind.Mouse);
        Up(pipeline, 1, new Point(15, 15), PointerDeviceKind.Mouse);

        Assert.Equal(["down#1", "panstart#1", "panupdate#1", "panend#1"], _events);
        Assert.Equal(0, tapCount);
    }

    [Fact]
    public void EagerVictoryOnDragCancelsACompetingPanRecognizer()
    {
        using var recognizer = CreatePanRecognizer();
        using var competing = new PanGestureRecognizer();
        competing.OnCancel = () => _events.Add("pancancel");

        var pipeline = Attach(recognizer, competing);

        Down(pipeline, 1, new Point(10, 10));
        Move(pipeline, 1, new Point(40, 45));
        Up(pipeline, 1, new Point(40, 45));

        Assert.Equal(["pancancel", "down#1", "panstart#1", "panupdate#1", "panend#1"], _events);
    }

    [Fact]
    public void EagerVictoryOnDragDisabledLosesToACompetingPanRecognizer()
    {
        using var recognizer = CreatePanRecognizer();
        recognizer.EagerVictoryOnDrag = false;
        using var competing = new PanGestureRecognizer();
        competing.OnStart = _ => _events.Add("panstart");
        competing.OnEnd = _ => _events.Add("panend");

        var pipeline = Attach(recognizer, competing);

        Down(pipeline, 1, new Point(10, 10));
        Move(pipeline, 1, new Point(40, 45));
        Up(pipeline, 1, new Point(40, 45));

        Assert.Equal(["panstart", "panend"], _events);
    }

    [Fact]
    public void FiresCancelAndResetsTheSeriesForAPointerCancelEvent()
    {
        using var recognizer = CreatePanRecognizer();
        var pipeline = Attach(recognizer);

        Down(pipeline, 1, new Point(10, 10));
        _binding.HandlePointerEvent(pipeline.Root, new PointerCancelEvent(
            pointer: 1,
            kind: PointerDeviceKind.Touch,
            position: new Point(10, 10),
            buttons: PointerButtons.None,
            timestampUtc: DateTime.UtcNow));

        Assert.Equal(["down#1", "cancel"], _events);

        // The cancelled tap does not count towards the next series.
        _events.Clear();
        _timers.Elapse(TimeSpan.FromMilliseconds(100));
        Tap(pipeline, 2, new Point(12, 12));
        Assert.Equal(["down#1", "up#1"], _events);
    }

    [Fact]
    public void DragEndReportsTheLastMovedToPosition()
    {
        using var recognizer = CreateHorizontalRecognizer();
        TapDragEndDetails? endDetails = null;
        recognizer.OnDragEnd = details => endDetails = details;

        var pipeline = Attach(recognizer);

        Down(pipeline, 1, new Point(10, 10));
        Move(pipeline, 1, new Point(50, 20));
        Move(pipeline, 1, new Point(90, 30));
        Move(pipeline, 1, new Point(120, 45));
        Up(pipeline, 1, new Point(120, 45));

        Assert.NotNull(endDetails);
        Assert.Equal(new Point(120, 45), endDetails!.GlobalPosition);
        Assert.Equal(Velocity.Zero, endDetails.Velocity);
        Assert.Equal(0.0, endDetails.PrimaryVelocity);
    }

    [Fact]
    public void DeadlineClaimsTheGestureOnlyForAConsecutiveTapGreaterThanOne()
    {
        using var recognizer = CreatePanRecognizer();
        var pipeline = Attach(recognizer);

        // A single tap does not resolve the arena when the press deadline elapses.
        Down(pipeline, 1, new Point(10, 10));
        _timers.Elapse(GestureConstants.PressTimeout);
        Assert.Equal(["down#1"], _events);
        Up(pipeline, 1, new Point(10, 10));

        _events.Clear();
        _timers.Elapse(ConsecutiveTapDelay);

        // The second tap in a series claims the gesture as soon as the deadline elapses, so a
        // competing long press cannot win while the pointer is held.
        using var longPress = new LongPressGestureRecognizer();
        longPress.OnLongPress = () => _events.Add("longpress");
        var second = Attach(recognizer, longPress);
        Down(second, 2, new Point(10, 10));
        _timers.Elapse(GestureConstants.PressTimeout);

        Assert.Equal(["down#2"], _events);
    }

    [Fact]
    public void IgnoresANonPrimaryButtonAndASecondConcurrentPointer()
    {
        using var recognizer = CreatePanRecognizer();
        var pipeline = Attach(recognizer);

        Down(pipeline, 1, new Point(10, 10));
        Down(pipeline, 2, new Point(12, 12));
        Up(pipeline, 1, new Point(11, 9));
        Up(pipeline, 2, new Point(13, 11));

        Assert.Equal(["down#1", "up#1"], _events);
    }

    [Fact]
    public void SecondaryButtonDoesNotStartTheGesture()
    {
        using var recognizer = CreatePanRecognizer();
        var pipeline = Attach(recognizer);

        _binding.HandlePointerEvent(pipeline.Root, new PointerDownEvent(
            pointer: 1,
            kind: PointerDeviceKind.Mouse,
            position: new Point(10, 10),
            buttons: PointerButtons.Secondary,
            timestampUtc: DateTime.UtcNow));
        Up(pipeline, 1, new Point(10, 10), PointerDeviceKind.Mouse);

        Assert.Empty(_events);
    }

    // -- Helpers ---------------------------------------------------------------------------------

    private TapAndPanGestureRecognizer CreatePanRecognizer()
    {
        var recognizer = new TapAndPanGestureRecognizer
        {
            DragStartBehavior = DragStartBehavior.Down,
            MaxConsecutiveTap = 3,
        };
        recognizer.OnTapDown = details => _events.Add($"down#{details.ConsecutiveTapCount}");
        recognizer.OnTapUp = details => _events.Add($"up#{details.ConsecutiveTapCount}");
        recognizer.OnDragStart = details => _events.Add($"panstart#{details.ConsecutiveTapCount}");
        recognizer.OnDragUpdate = details => _events.Add($"panupdate#{details.ConsecutiveTapCount}");
        recognizer.OnDragEnd = details => _events.Add($"panend#{details.ConsecutiveTapCount}");
        recognizer.OnCancel = () => _events.Add("cancel");
        return recognizer;
    }

    private TapAndHorizontalDragGestureRecognizer CreateHorizontalRecognizer()
    {
        var recognizer = new TapAndHorizontalDragGestureRecognizer
        {
            DragStartBehavior = DragStartBehavior.Down,
            MaxConsecutiveTap = 3,
        };
        recognizer.OnTapDown = details => _events.Add($"down#{details.ConsecutiveTapCount}");
        recognizer.OnTapUp = details => _events.Add($"up#{details.ConsecutiveTapCount}");
        recognizer.OnDragStart = details => _events.Add($"horizontaldragstart#{details.ConsecutiveTapCount}");
        recognizer.OnDragUpdate = details => _events.Add($"horizontaldragupdate#{details.ConsecutiveTapCount}");
        recognizer.OnDragEnd = details => _events.Add($"horizontaldragend#{details.ConsecutiveTapCount}");
        recognizer.OnCancel = () => _events.Add("cancel");
        return recognizer;
    }

    private static PipelineOwner Attach(params GestureRecognizer[] recognizers)
    {
        var listener = new RenderPointerListener(
            onPointerDown: @event =>
            {
                foreach (GestureRecognizer recognizer in recognizers)
                {
                    recognizer.AddPointer(@event);
                }
            },
            behavior: HitTestBehavior.Opaque,
            child: new SolidHitTestBox(new Size(400, 400)));
        var root = new RenderView { Child = listener };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(400, 400));
        return pipeline;
    }

    private void Tap(PipelineOwner pipeline, int pointer, Point position)
    {
        Down(pipeline, pointer, position);
        Up(pipeline, pointer, position);
    }

    private void Down(
        PipelineOwner pipeline,
        int pointer,
        Point position,
        PointerDeviceKind kind = PointerDeviceKind.Touch)
    {
        _binding.HandlePointerEvent(pipeline.Root, new PointerDownEvent(
            pointer: pointer,
            kind: kind,
            position: position,
            buttons: PointerButtons.Primary,
            timestampUtc: DateTime.UtcNow));
    }

    private void Move(
        PipelineOwner pipeline,
        int pointer,
        Point position,
        PointerDeviceKind kind = PointerDeviceKind.Touch)
    {
        _binding.HandlePointerEvent(pipeline.Root, new PointerMoveEvent(
            pointer: pointer,
            kind: kind,
            position: position,
            buttons: PointerButtons.Primary,
            down: true,
            timestampUtc: DateTime.UtcNow));
    }

    private void Up(
        PipelineOwner pipeline,
        int pointer,
        Point position,
        PointerDeviceKind kind = PointerDeviceKind.Touch)
    {
        _binding.HandlePointerEvent(pipeline.Root, new PointerUpEvent(
            pointer: pointer,
            kind: kind,
            position: position,
            buttons: PointerButtons.None,
            timestampUtc: DateTime.UtcNow));
    }

    private sealed class SolidHitTestBox(Size size) : RenderBox
    {
        protected override void PerformLayout() => Size = Constraints.Constrain(size);

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }
}

/// <summary>
/// Replaces the dispatcher-backed <see cref="GestureTimer"/> with a virtual clock, the way Flutter's
/// gesture tests run under `FakeAsync`.
/// </summary>
internal sealed class FakeGestureTimers : IDisposable
{
    private readonly List<PendingTimer> _pending = [];
    private TimeSpan _now;

    public FakeGestureTimers()
    {
        GestureTimer.Factory = Create;
    }

    public void Elapse(TimeSpan duration)
    {
        _now += duration;
        PendingTimer[] due = _pending.Where(entry => entry.Due <= _now).ToArray();
        foreach (PendingTimer entry in due)
        {
            _pending.Remove(entry);
            entry.Timer.FireNow();
        }
    }

    public void Dispose()
    {
        _pending.Clear();
        GestureTimer.ResetFactory();
    }

    private GestureTimer Create(TimeSpan duration, Action callback)
    {
        var timer = new ManualGestureTimer(callback);
        _pending.Add(new PendingTimer(_now + duration, timer));
        return timer;
    }

    private sealed record PendingTimer(TimeSpan Due, ManualGestureTimer Timer);

    private sealed class ManualGestureTimer(Action callback) : GestureTimer
    {
        public void FireNow() => Fire(callback);
    }
}
