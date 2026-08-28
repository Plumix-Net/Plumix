using Avalonia;
using Plumix.Gestures;
using Plumix.UI;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/test/gestures/long_press_test.dart (parity regression tests)

namespace Plumix.Tests;

/// <summary>
/// Ports the behaviors Flutter's own `long_press_test.dart` asserts against
/// `LongPressGestureRecognizer`, driving the recognizer through the arena and pointer router the way
/// Flutter's `GestureTester` does.
/// </summary>
public sealed class LongPressGestureRecognizerTests : IDisposable
{
    private readonly GestureBinding _binding = GestureBinding.Instance;
    private readonly FakeGestureTimers _timers = new();

    public LongPressGestureRecognizerTests()
    {
        _binding.ResetForTests();
    }

    public void Dispose()
    {
        _timers.Dispose();
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
        PointerButtons buttons = PointerButtons.Primary)
    {
        return new PointerMoveEvent(
            pointer, PointerDeviceKind.Touch, position, buttons, down: true, DateTime.UnixEpoch);
    }

    private static PointerUpEvent Up(int pointer, Point position)
    {
        return new PointerUpEvent(
            pointer, PointerDeviceKind.Touch, position, PointerButtons.None, DateTime.UnixEpoch);
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

    private LongPressGestureRecognizer Primary(List<string> log, TimeSpan? duration = null)
    {
        return new LongPressGestureRecognizer(duration: duration)
        {
            OnLongPressDown = _ => log.Add("down"),
            OnLongPressCancel = () => log.Add("cancel"),
            OnLongPressStart = _ => log.Add("start"),
            OnLongPress = () => log.Add("longPress"),
            OnLongPressMoveUpdate = _ => log.Add("move"),
            OnLongPressEnd = _ => log.Add("end"),
            OnLongPressUp = () => log.Add("up"),
        };
    }

    [Fact]
    public void RecognizesLongPress_AfterTheDefaultFiveHundredMillisecondDeadline()
    {
        var log = new List<string>();
        LongPressGestureRecognizer recognizer = Primary(log);
        try
        {
            PointerDownEvent down = Down(5, new Point(10.0, 10.0));
            recognizer.AddPointer(down);
            _binding.GestureArena.Close(5);
            Route(down);
            Assert.Equal(["down"], log);

            _timers.Elapse(TimeSpan.FromMilliseconds(300.0));
            Assert.Equal(["down"], log);

            _timers.Elapse(TimeSpan.FromMilliseconds(200.0));
            Assert.Equal(["down", "start", "longPress"], log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void RecognizesLongPress_WithAnAlteredDuration()
    {
        var log = new List<string>();
        LongPressGestureRecognizer recognizer = Primary(log, TimeSpan.FromMilliseconds(100.0));
        try
        {
            PointerDownEvent down = Down(5, new Point(10.0, 10.0));
            recognizer.AddPointer(down);
            _binding.GestureArena.Close(5);
            Route(down);

            _timers.Elapse(TimeSpan.FromMilliseconds(50.0));
            Assert.Equal(["down"], log);

            _timers.Elapse(TimeSpan.FromMilliseconds(50.0));
            Assert.Equal(["down", "start", "longPress"], log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void UpBeforeTheDeadline_CancelsTheLongPress()
    {
        var log = new List<string>();
        LongPressGestureRecognizer recognizer = Primary(log);
        try
        {
            PointerDownEvent down = Down(5, new Point(10.0, 10.0));
            recognizer.AddPointer(down);
            _binding.GestureArena.Close(5);
            Route(down);
            _timers.Elapse(TimeSpan.FromMilliseconds(300.0));
            Route(Up(5, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(5);

            Assert.Equal(["down", "cancel"], log);

            _timers.Elapse(TimeSpan.FromSeconds(1.0));
            Assert.Equal(["down", "cancel"], log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void MovingPastTheTouchSlopBeforeAcceptance_Cancels()
    {
        var log = new List<string>();
        LongPressGestureRecognizer recognizer = Primary(log);
        try
        {
            PointerDownEvent down = Down(5, new Point(10.0, 10.0));
            recognizer.AddPointer(down);
            _binding.GestureArena.Close(5);
            Route(down);
            _timers.Elapse(TimeSpan.FromMilliseconds(300.0));
            Route(Move(5, new Point(100.0, 200.0)));

            Assert.Equal(["down", "cancel"], log);

            Route(Up(5, new Point(100.0, 200.0)));
            Assert.Equal(["down", "cancel"], log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void MovingAfterAcceptance_ReportsOffsetsFromTheOriginAndDoesNotCancel()
    {
        var log = new List<string>();
        LongPressMoveUpdateDetails? update = null;
        LongPressGestureRecognizer recognizer = Primary(log);
        recognizer.OnLongPressMoveUpdate = details =>
        {
            log.Add("move");
            update = details;
        };

        try
        {
            PointerDownEvent down = Down(5, new Point(10.0, 10.0));
            recognizer.AddPointer(down);
            _binding.GestureArena.Close(5);
            Route(down);
            _timers.Elapse(TimeSpan.FromMilliseconds(500.0));
            Route(Move(5, new Point(100.0, 200.0)));
            Route(Up(5, new Point(100.0, 200.0)));

            Assert.Equal(["down", "start", "longPress", "move", "end", "up"], log);
            Assert.NotNull(update);
            Assert.Equal(new Point(100.0, 200.0), update!.Value.GlobalPosition);
            Assert.Equal(new Point(90.0, 190.0), update!.Value.OffsetFromOrigin);
            Assert.Equal(new Point(90.0, 190.0), update!.Value.LocalOffsetFromOrigin);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void DoesNotRecognize_WhenMoreThanOneButtonIsPressed()
    {
        var log = new List<string>();
        LongPressGestureRecognizer recognizer = Primary(log);
        try
        {
            PointerDownEvent down = Down(
                5, new Point(10.0, 10.0), PointerButtons.Secondary | PointerButtons.Middle);
            recognizer.AddPointer(down);
            _binding.GestureArena.Close(5);
            Route(down);
            _timers.Elapse(TimeSpan.FromSeconds(1.0));
            Route(Up(5, new Point(10.0, 10.0)));

            Assert.Empty(log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void ButtonChangeBeforeAcceptance_Cancels()
    {
        var log = new List<string>();
        LongPressGestureRecognizer recognizer = Primary(log);
        try
        {
            PointerDownEvent down = Down(5, new Point(10.0, 10.0));
            recognizer.AddPointer(down);
            _binding.GestureArena.Close(5);
            Route(down);
            Route(Move(5, new Point(10.0, 10.0), PointerButtons.Middle));

            Assert.Equal(["down", "cancel"], log);

            _timers.Elapse(TimeSpan.FromSeconds(1.0));
            Assert.Equal(["down", "cancel"], log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void ButtonChangeAfterAcceptance_IsIgnoredAndTheEndStillFires()
    {
        var log = new List<string>();
        LongPressGestureRecognizer recognizer = Primary(log);
        try
        {
            PointerDownEvent down = Down(5, new Point(10.0, 10.0));
            recognizer.AddPointer(down);
            _binding.GestureArena.Close(5);
            Route(down);
            _timers.Elapse(TimeSpan.FromMilliseconds(500.0));
            log.Clear();

            Route(Move(5, new Point(10.0, 10.0), PointerButtons.Secondary));
            Assert.Equal(["move"], log);

            Route(Up(5, new Point(10.0, 10.0)));
            Assert.Equal(["move", "end", "up"], log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void SupportedDevices_FiltersByDeviceKind()
    {
        var log = new List<string>();
        LongPressGestureRecognizer recognizer = Primary(log);
        recognizer.SupportedDevices = new HashSet<PointerDeviceKind> { PointerDeviceKind.Mouse };
        try
        {
            PointerDownEvent touch = Down(5, new Point(10.0, 10.0));
            recognizer.AddPointer(touch);
            _binding.GestureArena.Close(5);
            _timers.Elapse(TimeSpan.FromSeconds(2.0));
            Assert.Empty(log);

            PointerDownEvent mouse = Down(6, new Point(10.0, 10.0), kind: PointerDeviceKind.Mouse);
            recognizer.AddPointer(mouse);
            _binding.GestureArena.Close(6);
            Route(mouse);
            _timers.Elapse(TimeSpan.FromMilliseconds(500.0));
            Assert.Equal(["down", "start", "longPress"], log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void SecondaryAndTertiaryButtons_DispatchTheirOwnCallbacks()
    {
        var log = new List<string>();
        var recognizer = new LongPressGestureRecognizer
        {
            OnLongPress = () => log.Add("primary"),
            OnSecondaryLongPressDown = _ => log.Add("secondaryDown"),
            OnSecondaryLongPressStart = _ => log.Add("secondaryStart"),
            OnSecondaryLongPress = () => log.Add("secondary"),
            OnSecondaryLongPressEnd = _ => log.Add("secondaryEnd"),
            OnSecondaryLongPressUp = () => log.Add("secondaryUp"),
            OnTertiaryLongPressDown = _ => log.Add("tertiaryDown"),
            OnTertiaryLongPress = () => log.Add("tertiary"),
        };

        try
        {
            PointerDownEvent secondary = Down(5, new Point(10.0, 10.0), PointerButtons.Secondary);
            recognizer.AddPointer(secondary);
            _binding.GestureArena.Close(5);
            Route(secondary);
            _timers.Elapse(TimeSpan.FromMilliseconds(500.0));
            Route(Up(5, new Point(10.0, 10.0)));
            Assert.Equal(
                ["secondaryDown", "secondaryStart", "secondary", "secondaryEnd", "secondaryUp"],
                log);

            log.Clear();
            PointerDownEvent tertiary = Down(6, new Point(10.0, 10.0), PointerButtons.Middle);
            recognizer.AddPointer(tertiary);
            _binding.GestureArena.Close(6);
            Route(tertiary);
            _timers.Elapse(TimeSpan.FromMilliseconds(500.0));
            Assert.Equal(["tertiaryDown", "tertiary"], log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void APrimaryOnlyRecognizer_DoesNotCompeteForASecondaryDown()
    {
        var log = new List<string>();
        LongPressGestureRecognizer recognizer = Primary(log);
        try
        {
            PointerDownEvent down = Down(5, new Point(10.0, 10.0), PointerButtons.Secondary);
            recognizer.AddPointer(down);
            _binding.GestureArena.Close(5);
            _timers.Elapse(TimeSpan.FromSeconds(1.0));

            Assert.Empty(log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void EndDetails_CarryTheTrackedVelocity()
    {
        LongPressEndDetails? end = null;
        var recognizer = new LongPressGestureRecognizer
        {
            OnLongPressEnd = details => end = details,
        };

        try
        {
            PointerDownEvent down = Down(5, new Point(10.0, 10.0));
            recognizer.AddPointer(down);
            _binding.GestureArena.Close(5);
            Route(down);
            _timers.Elapse(TimeSpan.FromMilliseconds(500.0));
            Route(Up(5, new Point(30.0, 10.0)));

            Assert.NotNull(end);
            Assert.Equal(new Point(30.0, 10.0), end!.Value.GlobalPosition);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void PointerCancelBeforeAcceptance_ReportsCancel()
    {
        var log = new List<string>();
        LongPressGestureRecognizer recognizer = Primary(log);
        try
        {
            PointerDownEvent down = Down(5, new Point(10.0, 10.0));
            recognizer.AddPointer(down);
            _binding.GestureArena.Close(5);
            Route(down);
            Route(Cancel(5, new Point(10.0, 10.0)));

            Assert.Equal(["down", "cancel"], log);
        }
        finally
        {
            recognizer.Dispose();
        }
    }
}
