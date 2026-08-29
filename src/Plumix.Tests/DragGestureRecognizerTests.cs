using Avalonia;
using Plumix.Gestures;
using Plumix.UI;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/test/gestures/drag_test.dart; flutter/packages/flutter/test/gestures/monodrag_test.dart (parity regression tests)

namespace Plumix.Tests;

/// <summary>
/// Ports the behaviors Flutter's own `drag_test.dart` and `monodrag_test.dart` assert against
/// `DragGestureRecognizer`, driving the recognizers through the arena and pointer router the way
/// Flutter's `GestureTester` does.
/// </summary>
public sealed class DragGestureRecognizerTests : IDisposable
{
    private readonly GestureBinding _binding = GestureBinding.Instance;

    public DragGestureRecognizerTests()
    {
        _binding.ResetForTests();
    }

    public void Dispose()
    {
        _binding.ResetForTests();
    }

    private static PointerDownEvent Down(
        int pointer,
        Point position,
        PointerButtons buttons = PointerButtons.Primary,
        PointerDeviceKind kind = PointerDeviceKind.Touch)
    {
        return new PointerDownEvent(pointer, kind, position, buttons, DateTime.UnixEpoch);
    }

    private static PointerMoveEvent Move(
        int pointer,
        Point position,
        Point delta,
        PointerButtons buttons = PointerButtons.Primary,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        double milliseconds = 0.0)
    {
        var @event = new PointerMoveEvent(
            pointer, kind, position, buttons, down: true, DateTime.UnixEpoch.AddMilliseconds(milliseconds));
        return (PointerMoveEvent)@event.WithDelta(delta);
    }

    private static PointerUpEvent Up(int pointer, Point position, PointerDeviceKind kind = PointerDeviceKind.Touch)
    {
        return new PointerUpEvent(pointer, kind, position, PointerButtons.None, DateTime.UnixEpoch);
    }

    private static PointerCancelEvent Cancel(int pointer, Point position)
    {
        return new PointerCancelEvent(
            pointer, PointerDeviceKind.Touch, position, PointerButtons.None, DateTime.UnixEpoch);
    }

    private void Route(PointerEvent @event)
    {
        _binding.PointerRouter.Route(@event);
        _binding.GestureArena.FlushDefaultResolutions();
    }

    private void Begin(GestureRecognizer recognizer, PointerDownEvent down)
    {
        recognizer.AddPointer(down);
        _binding.GestureArena.Close(down.Pointer);
        Route(down);
    }

    /// <summary>
    /// Starts a pointer with a passive competitor in the arena, so the recognizer stays `possible`
    /// and has to earn the win by exceeding its slop — a lone recognizer wins by default at close.
    /// </summary>
    private PassiveMember BeginContested(GestureRecognizer recognizer, PointerDownEvent down)
    {
        var competitor = new PassiveMember();
        recognizer.AddPointer(down);
        _binding.GestureArena.Add(down.Pointer, competitor);
        _binding.GestureArena.Close(down.Pointer);
        Route(down);
        return competitor;
    }

    [Fact]
    public void HorizontalDrag_AcceptsOnceTheGlobalDistancePassesTheTouchSlop()
    {
        var log = new List<string>();
        var recognizer = new HorizontalDragGestureRecognizer
        {
            OnDown = _ => log.Add("down"),
            OnStart = _ => log.Add("start"),
            OnUpdate = _ => log.Add("update"),
            OnEnd = _ => log.Add("end"),
            OnCancel = () => log.Add("cancel"),
        };

        try
        {
            BeginContested(recognizer, Down(1, new Point(10.0, 10.0)));
            Assert.Equal(["down"], log);

            Route(Move(1, new Point(24.0, 10.0), new Point(14.0, 0.0)));
            Assert.Equal(["down"], log);

            Route(Move(1, new Point(34.0, 10.0), new Point(10.0, 0.0)));
            Assert.Equal(["down", "start"], log);

            Route(Move(1, new Point(44.0, 10.0), new Point(10.0, 0.0)));
            Assert.Equal(["down", "start", "update"], log);

            Route(Up(1, new Point(44.0, 10.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Equal(["down", "start", "update", "end"], log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void HorizontalDrag_UsesTheOnePixelHitSlopForAMouse()
    {
        var log = new List<string>();
        var recognizer = new HorizontalDragGestureRecognizer
        {
            OnStart = _ => log.Add("start"),
        };

        try
        {
            BeginContested(recognizer, Down(1, new Point(10.0, 10.0), kind: PointerDeviceKind.Mouse));
            Route(Move(
                1, new Point(10.5, 10.0), new Point(0.5, 0.0), kind: PointerDeviceKind.Mouse));
            Assert.Empty(log);

            Route(Move(
                1, new Point(12.0, 10.0), new Point(1.5, 0.0), kind: PointerDeviceKind.Mouse));
            Assert.Equal(["start"], log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void Pan_UsesThePanSlopWhichIsTwiceTheTouchSlop()
    {
        var log = new List<string>();
        var recognizer = new PanGestureRecognizer
        {
            OnStart = _ => log.Add("start"),
        };

        try
        {
            BeginContested(recognizer, Down(1, new Point(10.0, 10.0)));
            Route(Move(1, new Point(38.0, 10.0), new Point(28.0, 0.0)));
            Assert.Empty(log);

            Route(Move(1, new Point(52.0, 10.0), new Point(14.0, 0.0)));
            Assert.Equal(["start"], log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void HorizontalDrag_IgnoresCrossAxisMovementInsteadOfRejectingItself()
    {
        var log = new List<string>();
        var recognizer = new HorizontalDragGestureRecognizer
        {
            OnStart = _ => log.Add("start"),
            OnCancel = () => log.Add("cancel"),
        };

        try
        {
            BeginContested(recognizer, Down(1, new Point(10.0, 10.0)));
            // A purely vertical drag contributes nothing to the horizontal recognizer's global
            // distance, so it neither accepts nor rejects: Dart leaves that to the arena.
            Route(Move(1, new Point(10.0, 100.0), new Point(0.0, 90.0)));
            Assert.Empty(log);
            Assert.Equal(0.0, recognizer.GlobalDistanceMoved, 6);

            Route(Up(1, new Point(10.0, 100.0)));
            Assert.Equal(["cancel"], log);
            _binding.GestureArena.Sweep(1);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void GlobalDistanceMoved_AccumulatesSignedByThePrimaryAxis()
    {
        var recognizer = new VerticalDragGestureRecognizer
        {
            OnUpdate = _ => { },
        };

        try
        {
            BeginContested(recognizer, Down(1, new Point(10.0, 10.0)));
            Route(Move(1, new Point(10.0, 20.0), new Point(0.0, 10.0)));
            Assert.Equal(10.0, recognizer.GlobalDistanceMoved, 6);

            Route(Move(1, new Point(10.0, 14.0), new Point(0.0, -6.0)));
            Assert.Equal(4.0, recognizer.GlobalDistanceMoved, 6);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void DragStartBehaviorStart_ReportsTheWinningPositionAndNoSyntheticUpdate()
    {
        var log = new List<string>();
        DragStartDetails? start = null;
        var recognizer = new HorizontalDragGestureRecognizer
        {
            DragStartBehavior = DragStartBehavior.Start,
            OnStart = details =>
            {
                log.Add("start");
                start = details;
            },
            OnUpdate = _ => log.Add("update"),
        };
        var competitor = new PassiveMember();

        try
        {
            PointerDownEvent down = Down(1, new Point(10.0, 10.0));
            recognizer.AddPointer(down);
            _binding.GestureArena.Add(1, competitor);
            _binding.GestureArena.Close(1);
            Route(down);
            Route(Move(1, new Point(40.0, 10.0), new Point(30.0, 0.0)));

            Assert.Equal(["start"], log);
            Assert.Equal(new Point(40.0, 10.0), start!.Value.GlobalPosition);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void DragStartBehaviorDown_ReportsTheDownPositionAndReplaysThePendingOffset()
    {
        var log = new List<string>();
        DragStartDetails? start = null;
        DragUpdateDetails? update = null;
        var recognizer = new HorizontalDragGestureRecognizer
        {
            DragStartBehavior = DragStartBehavior.Down,
            OnStart = details =>
            {
                log.Add("start");
                start = details;
            },
            OnUpdate = details =>
            {
                log.Add("update");
                update = details;
            },
        };
        var competitor = new PassiveMember();

        try
        {
            PointerDownEvent down = Down(1, new Point(10.0, 10.0));
            recognizer.AddPointer(down);
            _binding.GestureArena.Add(1, competitor);
            _binding.GestureArena.Close(1);
            Route(down);
            Route(Move(1, new Point(40.0, 10.0), new Point(30.0, 0.0)));

            Assert.Equal(["start", "update"], log);
            Assert.Equal(new Point(10.0, 10.0), start!.Value.GlobalPosition);
            Assert.Equal(new Point(30.0, 0.0), update!.Value.Delta);
            Assert.Equal(30.0, update!.Value.PrimaryDelta);
            Assert.Equal(new Point(40.0, 10.0), update!.Value.GlobalPosition);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void OnlyAcceptDragOnThreshold_WithholdsTheDragUntilTheThresholdIsMet()
    {
        var log = new List<string>();
        var recognizer = new VerticalDragGestureRecognizer
        {
            OnlyAcceptDragOnThreshold = true,
            OnStart = _ => log.Add("start"),
            OnEnd = _ => log.Add("end"),
        };

        try
        {
            Begin(recognizer, Down(1, new Point(10.0, 10.0)));
            Route(Up(1, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(1);

            Assert.Empty(log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void OnlyAcceptDragOnThresholdFalse_StartsAndEndsWhenTheArenaIsSwept()
    {
        var log = new List<string>();
        var recognizer = new VerticalDragGestureRecognizer
        {
            OnStart = _ => log.Add("start"),
            OnEnd = _ => log.Add("end"),
        };

        try
        {
            Begin(recognizer, Down(1, new Point(10.0, 10.0)));
            Route(Up(1, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(1);

            Assert.Equal(["start", "end"], log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void ButtonChangeBeforeAcceptance_CancelsImmediately()
    {
        var log = new List<string>();
        var recognizer = new HorizontalDragGestureRecognizer
        {
            OnDown = _ => log.Add("down"),
            OnStart = _ => log.Add("start"),
            OnCancel = () => log.Add("cancel"),
        };

        try
        {
            BeginContested(recognizer, Down(1, new Point(10.0, 10.0)));
            // The button change is checked before the slop, so it cancels rather than accepting.
            Route(Move(1, new Point(100.0, 10.0), new Point(90.0, 0.0), PointerButtons.Secondary));

            Assert.Equal(["down", "cancel"], log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void ButtonChangeAfterAcceptance_EndsTheDragImmediately()
    {
        var log = new List<string>();
        var recognizer = new HorizontalDragGestureRecognizer
        {
            OnDown = _ => log.Add("down"),
            OnStart = _ => log.Add("start"),
            OnUpdate = _ => log.Add("update"),
            OnEnd = _ => log.Add("end"),
        };

        try
        {
            BeginContested(recognizer, Down(1, new Point(10.0, 10.0)));
            Route(Move(1, new Point(40.0, 10.0), new Point(30.0, 0.0)));
            Assert.Equal(["down", "start"], log);

            Route(Move(1, new Point(70.0, 10.0), new Point(30.0, 0.0), PointerButtons.Secondary));
            Assert.Equal(["down", "start", "end"], log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void ARecognizerWithNoCallbacks_NeverCompetes()
    {
        var recognizer = new HorizontalDragGestureRecognizer();
        var competitor = new PassiveMember();

        try
        {
            PointerDownEvent down = Down(1, new Point(10.0, 10.0));
            recognizer.AddPointer(down);
            _binding.GestureArena.Add(1, competitor);
            _binding.GestureArena.Close(1);
            _binding.GestureArena.FlushDefaultResolutions();

            Assert.True(competitor.Accepted);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void ASecondaryButtonDrag_DoesNotTriggerAPrimaryDrag()
    {
        var log = new List<string>();
        var recognizer = new HorizontalDragGestureRecognizer
        {
            OnDown = _ => log.Add("down"),
            OnStart = _ => log.Add("start"),
        };

        try
        {
            BeginContested(recognizer, Down(1, new Point(10.0, 10.0), PointerButtons.Secondary));
            Route(Move(1, new Point(100.0, 10.0), new Point(90.0, 0.0), PointerButtons.Secondary));
            Route(Up(1, new Point(100.0, 10.0)));

            Assert.Empty(log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void MultiplePointers_ReportOneCancelOnlyAfterTheLastPointerIsGone()
    {
        var log = new List<string>();
        var recognizer = new HorizontalDragGestureRecognizer
        {
            OnDown = _ => log.Add("down"),
            OnCancel = () => log.Add("cancel"),
        };

        try
        {
            BeginContested(recognizer, Down(1, new Point(10.0, 10.0)));
            PointerDownEvent second = Down(2, new Point(12.0, 10.0));
            recognizer.AddPointer(second);
            _binding.GestureArena.Add(2, new PassiveMember());
            _binding.GestureArena.Close(2);
            Route(second);
            Assert.Equal(["down"], log);

            Route(Cancel(1, new Point(10.0, 10.0)));
            Assert.Equal(["down"], log);

            Route(Cancel(2, new Point(12.0, 10.0)));
            Assert.Equal(["down", "cancel"], log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void SumAllPointers_ReportsEveryPointersDelta()
    {
        var deltas = new List<double>();
        var recognizer = new HorizontalDragGestureRecognizer
        {
            MultitouchDragStrategy = MultitouchDragStrategy.SumAllPointers,
            OnUpdate = details => deltas.Add(details.PrimaryDelta ?? 0.0),
        };

        try
        {
            BeginContested(recognizer, Down(1, new Point(10.0, 10.0)));
            PointerDownEvent second = Down(2, new Point(50.0, 10.0));
            recognizer.AddPointer(second);
            _binding.GestureArena.Add(2, new PassiveMember());
            _binding.GestureArena.Close(2);
            Route(second);
            // The move that clears the slop only starts the drag; updates begin with the next one.
            Route(Move(1, new Point(40.0, 10.0), new Point(30.0, 0.0)));

            Route(Move(1, new Point(50.0, 10.0), new Point(10.0, 0.0)));
            Route(Move(2, new Point(70.0, 10.0), new Point(20.0, 0.0)));

            Assert.Equal([10.0, 20.0], deltas);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void LatestPointer_TracksTheNewestPointerAndFallsBackToTheFirstAcceptedOne()
    {
        var deltas = new List<double>();
        var recognizer = new HorizontalDragGestureRecognizer
        {
            OnUpdate = details => deltas.Add(details.PrimaryDelta ?? 0.0),
        };

        try
        {
            BeginContested(recognizer, Down(1, new Point(10.0, 10.0)));
            Route(Move(1, new Point(40.0, 10.0), new Point(30.0, 0.0)));

            PointerDownEvent second = Down(2, new Point(50.0, 10.0));
            recognizer.AddPointer(second);
            _binding.GestureArena.Close(2);
            Route(second);

            // Pointer 2 is now the active one: pointer 1's moves are ignored.
            Route(Move(1, new Point(45.0, 10.0), new Point(5.0, 0.0)));
            Route(Move(2, new Point(57.0, 10.0), new Point(7.0, 0.0)));
            Assert.Equal([7.0], deltas);

            // When it lifts, the first accepted pointer takes over again.
            Route(Up(2, new Point(57.0, 10.0)));
            Route(Move(1, new Point(48.0, 10.0), new Point(3.0, 0.0)));
            Assert.Equal([7.0, 3.0], deltas);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void PanEndDetails_CarryANullPrimaryVelocity()
    {
        DragEndDetails? end = null;
        var recognizer = new PanGestureRecognizer
        {
            OnEnd = details => end = details,
        };

        try
        {
            Begin(recognizer, Down(1, new Point(10.0, 10.0)));
            Route(Move(1, new Point(60.0, 10.0), new Point(50.0, 0.0), milliseconds: 10.0));
            Route(Move(1, new Point(110.0, 10.0), new Point(50.0, 0.0), milliseconds: 20.0));
            Route(Move(1, new Point(160.0, 10.0), new Point(50.0, 0.0), milliseconds: 30.0));
            Route(Up(1, new Point(160.0, 10.0)));

            Assert.NotNull(end);
            Assert.Null(end!.Value.PrimaryVelocity);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void HorizontalFling_ClampsThePrimaryVelocityAndZeroesTheCrossAxis()
    {
        DragEndDetails? end = null;
        var recognizer = new HorizontalDragGestureRecognizer
        {
            MaxFlingVelocity = 1000.0,
            OnEnd = details => end = details,
        };

        try
        {
            Begin(recognizer, Down(1, new Point(0.0, 0.0)));
            for (int step = 1; step <= 4; step++)
            {
                Route(Move(
                    1,
                    new Point(step * 100.0, 0.0),
                    new Point(100.0, 0.0),
                    milliseconds: step * 10.0));
            }

            Route(Up(1, new Point(400.0, 0.0)));

            Assert.NotNull(end);
            Assert.Equal(1000.0, end!.Value.PrimaryVelocity!.Value, 6);
            Assert.Equal(1000.0, end!.Value.Velocity.PixelsPerSecond.X, 6);
            Assert.Equal(0.0, end!.Value.Velocity.PixelsPerSecond.Y, 6);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void ANonFlingRelease_ReportsZeroVelocityAtTheLastPosition()
    {
        DragEndDetails? end = null;
        var recognizer = new HorizontalDragGestureRecognizer
        {
            OnEnd = details => end = details,
        };

        try
        {
            Begin(recognizer, Down(1, new Point(10.0, 10.0)));
            Route(Move(1, new Point(40.0, 10.0), new Point(30.0, 0.0)));
            Route(Up(1, new Point(40.0, 10.0)));

            Assert.NotNull(end);
            Assert.Equal(0.0, end!.Value.PrimaryVelocity!.Value, 6);
            Assert.Equal(Velocity.Zero, end!.Value.Velocity);
            Assert.Equal(new Point(40.0, 10.0), end!.Value.GlobalPosition);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void DebugDescriptions_MatchTheDartStrings()
    {
        using var vertical = new VerticalDragGestureRecognizer();
        using var horizontal = new HorizontalDragGestureRecognizer();
        using var pan = new PanGestureRecognizer();

        Assert.Equal("vertical drag", vertical.DebugDescription);
        Assert.Equal("horizontal drag", horizontal.DebugDescription);
        Assert.Equal("pan", pan.DebugDescription);
    }

    /// <summary>
    /// A trackpad pan/zoom sequence, tracking the cumulative pan so each update can report the
    /// delta since the previous one. Mirrors Flutter's `TestPointer.panZoomStart/Update/End`.
    /// </summary>
    private sealed class PanZoomPointer(int pointer)
    {
        private Point _pan;

        public PointerPanZoomStartEvent Start(Point position)
        {
            _pan = default;
            return new PointerPanZoomStartEvent(pointer, position, DateTime.UnixEpoch);
        }

        public PointerPanZoomUpdateEvent Update(
            Point position,
            Point pan = default,
            double scale = 1.0,
            double rotation = 0.0,
            double milliseconds = 0.0)
        {
            Point panDelta = pan - _pan;
            _pan = pan;
            return new PointerPanZoomUpdateEvent(
                pointer,
                position,
                DateTime.UnixEpoch.AddMilliseconds(milliseconds),
                pan: pan,
                panDelta: panDelta,
                scale: scale,
                rotation: rotation);
        }

        public PointerPanZoomEndEvent End(Point position, double milliseconds = 0.0)
        {
            return new PointerPanZoomEndEvent(
                pointer, position, DateTime.UnixEpoch.AddMilliseconds(milliseconds));
        }
    }

    /// <summary>The pan/zoom counterpart of <see cref="BeginContested"/>.</summary>
    private PassiveMember BeginContestedPanZoom(GestureRecognizer recognizer, PointerPanZoomStartEvent start)
    {
        var competitor = new PassiveMember();
        recognizer.AddPointerPanZoom(start);
        _binding.GestureArena.Add(start.Pointer, competitor);
        _binding.GestureArena.Close(start.Pointer);
        Route(start);
        return competitor;
    }

    [Fact]
    public void Pan_RecognizesATrackpadPanZoomGestureOncePanSlopIsExceeded()
    {
        using var pan = new PanGestureRecognizer();
        var log = new List<string>();
        Point? updatedDelta = null;
        pan.OnStart = _ => log.Add("start");
        pan.OnUpdate = details =>
        {
            log.Add("update");
            updatedDelta = details.Delta;
        };
        pan.OnEnd = _ => log.Add("end");

        var pointer = new PanZoomPointer(2);
        BeginContestedPanZoom(pan, pointer.Start(new Point(10, 10)));
        Assert.Empty(log);

        // 28.28 logical pixels of pan: short of the 36-pixel pan slop a trackpad shares with touch.
        Route(pointer.Update(new Point(10, 10), pan: new Point(20, 20)));
        Assert.Empty(log);

        // 42.43 pixels clears it; `DragStartBehavior.Start` folds the pending offset into the start
        // position, so the accepting update reports no delta of its own.
        Route(pointer.Update(new Point(10, 10), pan: new Point(30, 30)));
        Assert.Equal(["start"], log);
        Assert.Null(updatedDelta);

        Route(pointer.Update(new Point(10, 10), pan: new Point(30, 25)));
        Assert.Equal(["start", "update"], log);
        Assert.Equal(new Point(0, -5), updatedDelta);

        Route(pointer.End(new Point(10, 10)));
        Assert.Equal(["start", "update", "end"], log);
    }

    [Fact]
    public void Pan_LetsATouchJoinAnAcceptedTrackpadPanZoomDrag()
    {
        using var pan = new PanGestureRecognizer();
        var log = new List<string>();
        Point? updatedDelta = null;
        pan.OnStart = _ => log.Add("start");
        pan.OnUpdate = details =>
        {
            log.Add("update");
            updatedDelta = details.Delta;
        };
        pan.OnEnd = _ => log.Add("end");

        var trackpad = new PanZoomPointer(2);
        BeginContestedPanZoom(pan, trackpad.Start(new Point(10, 10)));
        Route(trackpad.Update(new Point(10, 10), pan: new Point(30, 30)));
        Assert.Equal(["start"], log);

        // The touch joins the drag already in progress: no second start, and its move is reported
        // as an ordinary update.
        PointerDownEvent down = Down(3, new Point(20, 20));
        pan.AddPointer(down);
        _binding.GestureArena.Close(3);
        Route(down);
        Assert.Equal(["start"], log);

        Route(Move(3, new Point(25, 25), new Point(5, 5)));
        Assert.Equal(["start", "update"], log);
        Assert.Equal(new Point(5, 5), updatedDelta);

        Route(Up(3, new Point(25, 25)));
        Assert.Equal(["start", "update"], log);

        Route(trackpad.End(new Point(10, 10)));
        Assert.Equal(["start", "update", "end"], log);
    }

    [Fact]
    public void Pan_LetsATrackpadPanZoomJoinAnAcceptedTouchDrag()
    {
        using var pan = new PanGestureRecognizer();
        var log = new List<string>();
        Point? updatedDelta = null;
        pan.OnStart = _ => log.Add("start");
        pan.OnUpdate = details =>
        {
            log.Add("update");
            updatedDelta = details.Delta;
        };
        pan.OnEnd = _ => log.Add("end");

        PointerDownEvent down = Down(1, new Point(10, 10));
        BeginContested(pan, down);
        Route(Move(1, new Point(60, 60), new Point(50, 50)));
        Assert.Equal(["start"], log);
        Route(Move(1, new Point(70, 70), new Point(10, 10)));
        Assert.Equal(["start", "update"], log);
        Assert.Equal(new Point(10, 10), updatedDelta);

        var trackpad = new PanZoomPointer(2);
        pan.AddPointerPanZoom(trackpad.Start(new Point(10, 10)));
        _binding.GestureArena.Close(2);
        Route(trackpad.Start(new Point(10, 10)));
        Assert.Equal(["start", "update"], log);

        // Already accepted, so the first update is reported straight through without re-checking
        // the pan slop.
        Route(trackpad.Update(new Point(10, 10), pan: new Point(20, 20)));
        Assert.Equal(new Point(20, 20), updatedDelta);
        Route(trackpad.Update(new Point(10, 10), pan: new Point(30, 30)));
        Assert.Equal(new Point(10, 10), updatedDelta);

        // The touch pointer is still tracked, so the gesture does not end yet.
        Route(trackpad.End(new Point(10, 10)));
        Assert.DoesNotContain("end", log);

        Route(Up(1, new Point(70, 70)));
        Assert.Contains("end", log);
    }

    [Fact]
    public void HorizontalDrag_UsesTheTouchSlopNotThePanSlopForATrackpadGesture()
    {
        using var horizontal = new HorizontalDragGestureRecognizer();
        bool started = false;
        horizontal.OnStart = _ => started = true;

        var pointer = new PanZoomPointer(2);
        BeginContestedPanZoom(horizontal, pointer.Start(new Point(10, 10)));

        // 17 pixels along the axis: under the 18-pixel hit slop a one-axis recognizer uses.
        Route(pointer.Update(new Point(10, 10), pan: new Point(17, 0)));
        Assert.False(started);

        Route(pointer.Update(new Point(10, 10), pan: new Point(19, 0)));
        Assert.True(started);
    }

    [Fact]
    public void PanZoom_IsRejectedWhenTheDeviceKindIsNotSupported()
    {
        using var pan = new PanGestureRecognizer
        {
            SupportedDevices = new HashSet<PointerDeviceKind> { PointerDeviceKind.Touch }
        };
        bool started = false;
        pan.OnStart = _ => started = true;

        var pointer = new PanZoomPointer(2);
        // `SupportedDevices` excludes the trackpad, so the gesture is never tracked and its updates
        // never reach the recognizer.
        pan.AddPointerPanZoom(pointer.Start(new Point(10, 10)));
        _binding.GestureArena.Close(2);
        Route(pointer.Start(new Point(10, 10)));
        Route(pointer.Update(new Point(10, 10), pan: new Point(50, 50)));

        Assert.False(started);
    }

    [Fact]
    public void PanZoom_ReportsAFlingVelocityMeasuredInPanSpace()
    {
        using var pan = new PanGestureRecognizer();
        DragEndDetails? endDetails = null;
        pan.OnStart = _ => { };
        pan.OnEnd = details => endDetails = details;

        var pointer = new PanZoomPointer(2);
        BeginContestedPanZoom(pan, pointer.Start(new Point(10, 10)));
        for (int i = 1; i <= 10; i++)
        {
            Route(pointer.Update(new Point(10, 10), pan: new Point(i * 20, 0), milliseconds: i * 20));
        }

        Route(pointer.End(new Point(10, 10), milliseconds: 220));

        Assert.True(endDetails.HasValue);
        DragEndDetails details = endDetails.Value;
        // 20 logical pixels every 20 ms is 1000 px/s along x, tracked from the pan offsets rather
        // than from the stationary contact position.
        Assert.Equal(1000.0, details.Velocity.PixelsPerSecond.X, 1);
        Assert.Equal(0.0, details.Velocity.PixelsPerSecond.Y, 1);
    }

    private sealed class PassiveMember : IGestureArenaMember
    {
        public bool Accepted { get; private set; }

        public bool Rejected { get; private set; }

        public void AcceptGesture(int pointer) => Accepted = true;

        public void RejectGesture(int pointer) => Rejected = true;
    }
}
