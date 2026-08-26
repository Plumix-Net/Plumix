using Avalonia;
using Plumix.Gestures;
using Plumix.UI;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/test/gestures/double_tap_test.dart; flutter/packages/flutter/test/gestures/serial_tap_test.dart; flutter/packages/flutter/test/gestures/multitap_test.dart (parity regression tests)

namespace Plumix.Tests;

/// <summary>
/// Ports the behaviors Flutter's own multitap tests assert against `DoubleTapGestureRecognizer`,
/// `SerialTapGestureRecognizer` and `MultiTapGestureRecognizer`.
/// </summary>
public sealed class MultitapGestureTests : IDisposable
{
    private readonly GestureBinding _binding = GestureBinding.Instance;
    private readonly FakeGestureTimers _timers = new();

    public MultitapGestureTests()
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

    private static PointerUpEvent Up(
        int pointer,
        Point position,
        PointerDeviceKind kind = PointerDeviceKind.Touch)
    {
        return new PointerUpEvent(pointer, kind, position, PointerButtons.None, DateTime.UnixEpoch);
    }

    private static PointerMoveEvent Move(int pointer, Point position)
    {
        return new PointerMoveEvent(
            pointer, PointerDeviceKind.Touch, position, PointerButtons.Primary, down: true, DateTime.UnixEpoch);
    }

    private static PointerCancelEvent Cancel(int pointer, Point position)
    {
        return new PointerCancelEvent(
            pointer, PointerDeviceKind.Touch, position, PointerButtons.None, DateTime.UnixEpoch);
    }

    private void Tap(GestureRecognizer recognizer, int pointer, Point position)
    {
        var down = Down(pointer, position);
        recognizer.AddPointer(down);
        _binding.GestureArena.Close(pointer);
        _binding.PointerRouter.Route(down);
        _binding.PointerRouter.Route(Up(pointer, position));
        _binding.GestureArena.Sweep(pointer);
    }

    private void Route(PointerEvent @event) => _binding.PointerRouter.Route(@event);

    [Fact]
    public void DoubleTap_RecognizedWithinSlopAndTimeout()
    {
        var events = new List<string>();
        var doubleTap = new DoubleTapGestureRecognizer
        {
            OnDoubleTapDown = _ => events.Add("doubleTapDown"),
            OnDoubleTap = () => events.Add("doubleTap"),
            OnDoubleTapCancel = () => events.Add("doubleTapCancel"),
        };

        try
        {
            Tap(doubleTap, 1, new Point(10.0, 10.0));
            Assert.Empty(events);

            _timers.Elapse(TimeSpan.FromMilliseconds(100));
            var down2 = Down(2, new Point(12.0, 12.0));
            doubleTap.AddPointer(down2);
            Assert.Equal(["doubleTapDown"], events);
            _binding.GestureArena.Close(2);
            Route(down2);
            Route(Up(2, new Point(12.0, 12.0)));
            _binding.GestureArena.Sweep(2);
            Assert.Equal(["doubleTapDown", "doubleTap"], events);
        }
        finally
        {
            doubleTap.Dispose();
        }
    }

    [Fact]
    public void DoubleTap_InterTapDistanceCancels()
    {
        var events = new List<string>();
        var doubleTap = new DoubleTapGestureRecognizer
        {
            OnDoubleTapDown = _ => events.Add("doubleTapDown"),
            OnDoubleTap = () => events.Add("doubleTap"),
        };

        try
        {
            Tap(doubleTap, 1, new Point(10.0, 10.0));
            _timers.Elapse(TimeSpan.FromMilliseconds(100));
            // 150 px away: past kDoubleTapSlop (100), the down is ignored entirely.
            Tap(doubleTap, 2, new Point(160.0, 10.0));
            Assert.Empty(events);
        }
        finally
        {
            doubleTap.Dispose();
        }
    }

    [Fact]
    public void DoubleTap_IntraTapMovePastSlopRejectsTheFirstTap()
    {
        var events = new List<string>();
        var doubleTap = new DoubleTapGestureRecognizer
        {
            OnDoubleTap = () => events.Add("doubleTap"),
        };

        try
        {
            var down1 = Down(1, new Point(10.0, 10.0));
            doubleTap.AddPointer(down1);
            _binding.GestureArena.Close(1);
            Route(down1);
            // Past kDoubleTapTouchSlop (18): the first tap is rejected.
            Route(Move(1, new Point(40.0, 10.0)));
            Route(Up(1, new Point(40.0, 10.0)));
            _binding.GestureArena.Sweep(1);

            _timers.Elapse(TimeSpan.FromMilliseconds(100));
            Tap(doubleTap, 2, new Point(10.0, 10.0));
            Assert.Empty(events);
        }
        finally
        {
            doubleTap.Dispose();
        }
    }

    [Fact]
    public void DoubleTap_InterTapDelayCancels_AndTheTimedOutTapStartsANewSequence()
    {
        var events = new List<string>();
        var doubleTap = new DoubleTapGestureRecognizer
        {
            OnDoubleTap = () => events.Add("doubleTap"),
        };

        try
        {
            Tap(doubleTap, 1, new Point(10.0, 10.0));
            _timers.Elapse(TimeSpan.FromMilliseconds(5000));
            Tap(doubleTap, 2, new Point(10.0, 10.0));
            Assert.Empty(events);

            // The timed-out second tap became a fresh first tap: a third tap completes the pair.
            _timers.Elapse(TimeSpan.FromMilliseconds(100));
            Tap(doubleTap, 3, new Point(10.0, 10.0));
            Assert.Equal(["doubleTap"], events);
        }
        finally
        {
            doubleTap.Dispose();
        }
    }

    [Fact]
    public void DoubleTap_OverRapidSecondTapRestartsTheSequence()
    {
        var events = new List<string>();
        var doubleTap = new DoubleTapGestureRecognizer
        {
            OnDoubleTap = () => events.Add("doubleTap"),
        };

        try
        {
            Tap(doubleTap, 1, new Point(10.0, 10.0));
            // 10 ms < kDoubleTapMinTime (40 ms): the second tap restarts the sequence.
            _timers.Elapse(TimeSpan.FromMilliseconds(10));
            Tap(doubleTap, 2, new Point(10.0, 10.0));
            Assert.Empty(events);

            _timers.Elapse(TimeSpan.FromMilliseconds(100));
            Tap(doubleTap, 3, new Point(10.0, 10.0));
            Assert.Equal(["doubleTap"], events);
        }
        finally
        {
            doubleTap.Dispose();
        }
    }

    [Fact]
    public void DoubleTap_ArenaRejectDuringSecondTapFiresCancel()
    {
        var events = new List<string>();
        var doubleTap = new DoubleTapGestureRecognizer
        {
            OnDoubleTapDown = _ => events.Add("doubleTapDown"),
            OnDoubleTap = () => events.Add("doubleTap"),
            OnDoubleTapCancel = () => events.Add("doubleTapCancel"),
        };
        var competitor = new CapturingArenaMember();

        try
        {
            Tap(doubleTap, 1, new Point(10.0, 10.0));
            _timers.Elapse(TimeSpan.FromMilliseconds(100));

            var down2 = Down(2, new Point(10.0, 10.0));
            doubleTap.AddPointer(down2);
            GestureArenaEntry competitorEntry = _binding.GestureArena.Add(2, competitor);
            _binding.GestureArena.Close(2);
            Route(down2);
            Assert.Equal(["doubleTapDown"], events);

            // Another member wins the second tap's arena: the double tap is canceled.
            competitorEntry.Resolve(GestureDisposition.Accepted);
            Assert.Equal(["doubleTapDown", "doubleTapCancel"], events);

            Route(Up(2, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(2);
            Assert.Equal(["doubleTapDown", "doubleTapCancel"], events);
        }
        finally
        {
            doubleTap.Dispose();
        }
    }

    [Fact]
    public void DoubleTap_WithoutCallbacksIsANoOp()
    {
        var doubleTap = new DoubleTapGestureRecognizer();
        var tapEvents = new List<string>();
        var tap = new TapGestureRecognizer
        {
            OnTap = () => tapEvents.Add("tap"),
        };

        try
        {
            var down = Down(1, new Point(10.0, 10.0));
            tap.AddPointer(down);
            doubleTap.AddPointer(down);
            _binding.GestureArena.Close(1);
            // The callback-less double tap refused the pointer, so the tap won at close.
            Route(down);
            Route(Up(1, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Equal(["tap"], tapEvents);
        }
        finally
        {
            doubleTap.Dispose();
            tap.Dispose();
        }
    }

    [Fact]
    public void SerialTap_CountsTapsInASeries()
    {
        var log = new List<string>();
        var serial = new SerialTapGestureRecognizer
        {
            OnSerialTapDown = details => log.Add($"down#{details.Count}"),
            OnSerialTapCancel = details => log.Add($"cancel#{details.Count}"),
            OnSerialTapUp = details => log.Add($"up#{details.Count}"),
        };

        try
        {
            Tap(serial, 1, new Point(10.0, 10.0));
            _timers.Elapse(TimeSpan.FromMilliseconds(150));
            Tap(serial, 2, new Point(10.0, 10.0));
            _timers.Elapse(TimeSpan.FromMilliseconds(150));
            Tap(serial, 3, new Point(10.0, 10.0));
            Assert.Equal(["down#1", "up#1", "down#2", "up#2", "down#3", "up#3"], log);
        }
        finally
        {
            serial.Dispose();
        }
    }

    [Fact]
    public void SerialTap_WinsOverAPlainTapRecognizer()
    {
        var log = new List<string>();
        var serial = new SerialTapGestureRecognizer
        {
            OnSerialTapDown = details => log.Add($"down#{details.Count}"),
            OnSerialTapUp = details => log.Add($"up#{details.Count}"),
        };
        var tap = new TapGestureRecognizer
        {
            OnTap = () => log.Add("tap"),
        };

        try
        {
            var down = Down(1, new Point(10.0, 10.0));
            tap.AddPointer(down);
            serial.AddPointer(down);
            _binding.GestureArena.Close(1);
            Route(down);
            Route(Up(1, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Equal(["down#1", "up#1"], log);
        }
        finally
        {
            serial.Dispose();
            tap.Dispose();
        }
    }

    [Fact]
    public void SerialTap_TimeoutBetweenTapsResetsTheCount()
    {
        var log = new List<string>();
        var serial = new SerialTapGestureRecognizer
        {
            OnSerialTapDown = details => log.Add($"down#{details.Count}"),
            OnSerialTapUp = details => log.Add($"up#{details.Count}"),
        };

        try
        {
            Tap(serial, 1, new Point(10.0, 10.0));
            _timers.Elapse(TimeSpan.FromMilliseconds(1000));
            Tap(serial, 2, new Point(10.0, 10.0));
            Assert.Equal(["down#1", "up#1", "down#1", "up#1"], log);
        }
        finally
        {
            serial.Dispose();
        }
    }

    [Fact]
    public void SerialTap_FarApartTapsStartANewSeries()
    {
        var log = new List<string>();
        var serial = new SerialTapGestureRecognizer
        {
            OnSerialTapDown = details => log.Add($"down#{details.Count}"),
            OnSerialTapUp = details => log.Add($"up#{details.Count}"),
        };

        try
        {
            Tap(serial, 1, new Point(10.0, 10.0));
            _timers.Elapse(TimeSpan.FromMilliseconds(150));
            Tap(serial, 2, new Point(160.0, 10.0));
            Assert.Equal(["down#1", "up#1", "down#1", "up#1"], log);
        }
        finally
        {
            serial.Dispose();
        }
    }

    [Fact]
    public void SerialTap_PointerCancelFiresCancelAndResets()
    {
        var log = new List<string>();
        var serial = new SerialTapGestureRecognizer
        {
            OnSerialTapDown = details => log.Add($"down#{details.Count}"),
            OnSerialTapCancel = details => log.Add($"cancel#{details.Count}"),
            OnSerialTapUp = details => log.Add($"up#{details.Count}"),
        };

        try
        {
            var down = Down(1, new Point(10.0, 10.0));
            serial.AddPointer(down);
            _binding.GestureArena.Close(1);
            Route(down);
            Route(Cancel(1, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Equal(["down#1", "cancel#1"], log);

            _timers.Elapse(TimeSpan.FromMilliseconds(150));
            Tap(serial, 2, new Point(10.0, 10.0));
            Assert.Equal(["down#1", "cancel#1", "down#1", "up#1"], log);
        }
        finally
        {
            serial.Dispose();
        }
    }

    [Fact]
    public void SerialTap_InterleavedTapsCancelTheFirstSequence()
    {
        var log = new List<string>();
        var serial = new SerialTapGestureRecognizer
        {
            OnSerialTapDown = details => log.Add($"down#{details.Count}"),
            OnSerialTapCancel = details => log.Add($"cancel#{details.Count}"),
            OnSerialTapUp = details => log.Add($"up#{details.Count}"),
        };

        try
        {
            var down1 = Down(1, new Point(10.0, 10.0));
            serial.AddPointer(down1);
            _binding.GestureArena.Close(1);
            Route(down1);

            var down2 = Down(2, new Point(10.0, 10.0));
            serial.AddPointer(down2);
            _binding.GestureArena.Close(2);
            Route(down2);
            Route(Up(2, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(2);
            Assert.Equal(["down#1", "cancel#1", "down#1", "up#1"], log);
        }
        finally
        {
            serial.Dispose();
        }
    }

    [Fact]
    public void SerialTap_WithoutCallbacksIsANoOp()
    {
        var serial = new SerialTapGestureRecognizer();
        try
        {
            var down = Down(1, new Point(10.0, 10.0));
            serial.AddPointer(down);
            Assert.False(serial.IsTrackingPointerInSeries);
        }
        finally
        {
            serial.Dispose();
        }
    }

    [Fact]
    public void MultiTap_TracksEachPointerIndependently()
    {
        var log = new List<string>();
        var multiTap = new MultiTapGestureRecognizer(longTapDelay: GestureConstants.LongPressTimeout)
        {
            OnTapDown = (pointer, _) => log.Add($"tap-down {pointer}"),
            OnTapUp = (pointer, _) => log.Add($"tap-up {pointer}"),
            OnTap = pointer => log.Add($"tap {pointer}"),
            OnTapCancel = pointer => log.Add($"tap-cancel {pointer}"),
            OnLongTapDown = (pointer, _) => log.Add($"long-tap-down {pointer}"),
        };

        try
        {
            var down5 = Down(5, new Point(10.0, 10.0));
            multiTap.AddPointer(down5);
            _binding.GestureArena.Close(5);
            Assert.Equal(["tap-down 5"], log);
            Route(down5);

            var down6 = Down(6, new Point(30.0, 30.0));
            multiTap.AddPointer(down6);
            _binding.GestureArena.Close(6);
            Route(down6);
            Assert.Equal(["tap-down 5", "tap-down 6"], log);

            Route(Up(5, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(5);
            Assert.Equal(["tap-down 5", "tap-down 6", "tap-up 5", "tap 5"], log);

            _timers.Elapse(GestureConstants.LongPressTimeout + GestureConstants.PressTimeout);
            Assert.Equal(["tap-down 5", "tap-down 6", "tap-up 5", "tap 5", "long-tap-down 6"], log);

            // Past the hit slop: the remaining pointer's tap is canceled.
            Route(Move(6, new Point(70.0, 30.0)));
            Assert.Equal(
                ["tap-down 5", "tap-down 6", "tap-up 5", "tap 5", "long-tap-down 6", "tap-cancel 6"],
                log);
        }
        finally
        {
            multiTap.Dispose();
        }
    }

    private sealed class CapturingArenaMember : IGestureArenaMember
    {
        public void AcceptGesture(int pointer)
        {
        }

        public void RejectGesture(int pointer)
        {
        }
    }
}
