using Avalonia;
using Plumix.Gestures;
using Plumix.UI;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/test/gestures/tap_test.dart (parity regression tests)

namespace Plumix.Tests;

/// <summary>
/// Ports the behaviors Flutter's own `tap_test.dart` asserts against `TapGestureRecognizer`,
/// driving the recognizer through the arena and pointer router the way Flutter's GestureTester does.
/// </summary>
public sealed class TapGestureRecognizerTests : IDisposable
{
    private readonly GestureBinding _binding = GestureBinding.Instance;

    public TapGestureRecognizerTests()
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

    private static PointerUpEvent Up(
        int pointer,
        Point position,
        PointerDeviceKind kind = PointerDeviceKind.Touch)
    {
        return new PointerUpEvent(pointer, kind, position, PointerButtons.None, DateTime.UnixEpoch);
    }

    private static PointerMoveEvent Move(
        int pointer,
        Point position,
        PointerButtons buttons = PointerButtons.Primary,
        PointerDeviceKind kind = PointerDeviceKind.Touch)
    {
        return new PointerMoveEvent(pointer, kind, position, buttons, down: true, DateTime.UnixEpoch);
    }

    private static PointerCancelEvent Cancel(int pointer, Point position)
    {
        return new PointerCancelEvent(
            pointer, PointerDeviceKind.Touch, position, PointerButtons.None, DateTime.UnixEpoch);
    }

    /// <summary>
    /// Mirrors Flutter's `GestureTester.route`, which routes the event and then flushes microtasks
    /// so the arena's deferred single-member resolution runs before the assertions.
    /// </summary>
    private void Route(PointerEvent @event)
    {
        _binding.PointerRouter.Route(@event);
        _binding.GestureArena.FlushDefaultResolutions();
    }

    private sealed class PassiveArenaMember : IGestureArenaMember
    {
        public bool Accepted { get; private set; }

        public bool Rejected { get; private set; }

        public void AcceptGesture(int pointer) => Accepted = true;

        public void RejectGesture(int pointer) => Rejected = true;
    }

    [Fact]
    public void RecognizesTap_FiringDownAtCloseAndUpTapAtUp()
    {
        var events = new List<string>();
        var tap = new TapGestureRecognizer
        {
            OnTapDown = _ => events.Add("down"),
            OnTapUp = _ => events.Add("up"),
            OnTap = () => events.Add("tap"),
            OnTapCancel = () => events.Add("cancel"),
        };

        try
        {
            var down = Down(1, new Point(10.0, 10.0));
            tap.AddPointer(down);
            _binding.GestureArena.Close(1);
            // Dart resolves the sole remaining member in a microtask, so nothing has fired yet.
            Assert.Empty(events);
            Route(down);
            Assert.Equal(["down"], events);
            Route(Up(1, new Point(11.0, 9.0)));
            Assert.Equal(["down", "up", "tap"], events);
            _binding.GestureArena.Sweep(1);
            Assert.Equal(["down", "up", "tap"], events);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void SupportedDevices_FilterOutOtherKinds()
    {
        int taps = 0;
        var tap = new TapGestureRecognizer
        {
            OnTap = () => taps++,
            SupportedDevices = new HashSet<PointerDeviceKind>
            {
                PointerDeviceKind.Mouse,
                PointerDeviceKind.Stylus,
            },
        };

        try
        {
            var touchDown = Down(1, new Point(10.0, 10.0));
            tap.AddPointer(touchDown);
            _binding.GestureArena.Close(1);
            Route(touchDown);
            Route(Up(1, new Point(11.0, 9.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Equal(0, taps);

            var mouseDown = Down(2, new Point(10.0, 10.0), kind: PointerDeviceKind.Mouse);
            tap.AddPointer(mouseDown);
            _binding.GestureArena.Close(2);
            Route(mouseDown);
            Route(Up(2, new Point(11.0, 9.0), kind: PointerDeviceKind.Mouse));
            _binding.GestureArena.Sweep(2);
            Assert.Equal(1, taps);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void Details_CarryKindAndPositions()
    {
        TapDownDetails? downDetails = null;
        TapUpDetails? upDetails = null;
        var tap = new TapGestureRecognizer
        {
            OnTapDown = details => downDetails = details,
            OnTapUp = details => upDetails = details,
        };

        try
        {
            var down = Down(1, new Point(5.0, 5.0), kind: PointerDeviceKind.Mouse);
            tap.AddPointer(down);
            _binding.GestureArena.Close(1);
            Route(down);
            Route(Up(1, new Point(8.0, 6.0), kind: PointerDeviceKind.Mouse));
            _binding.GestureArena.Sweep(1);

            Assert.Equal(PointerDeviceKind.Mouse, downDetails!.Kind);
            Assert.Equal(new Point(5.0, 5.0), downDetails.GlobalPosition);
            Assert.Equal(PointerDeviceKind.Mouse, upDetails!.Kind);
            Assert.Equal(new Point(8.0, 6.0), upDetails.GlobalPosition);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void OverlappingPointers_FirstUpWinsOnce()
    {
        int taps = 0;
        var tap = new TapGestureRecognizer
        {
            OnTap = () => taps++,
        };

        try
        {
            var down1 = Down(1, new Point(10.0, 10.0));
            tap.AddPointer(down1);
            _binding.GestureArena.Close(1);
            Route(down1);

            var down2 = Down(2, new Point(15.0, 15.0));
            tap.AddPointer(down2);
            _binding.GestureArena.Close(2);
            Route(down2);

            Route(Up(1, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Equal(1, taps);

            Route(Up(2, new Point(15.0, 15.0)));
            _binding.GestureArena.Sweep(2);
            Assert.Equal(1, taps);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void OverlappingPointers_NonPrimaryUpFiresNothing()
    {
        int taps = 0;
        var tap = new TapGestureRecognizer
        {
            OnTap = () => taps++,
        };

        try
        {
            var down1 = Down(1, new Point(10.0, 10.0));
            tap.AddPointer(down1);
            _binding.GestureArena.Close(1);
            Route(down1);

            var down2 = Down(2, new Point(15.0, 15.0));
            tap.AddPointer(down2);
            _binding.GestureArena.Close(2);
            Route(down2);

            Route(Up(2, new Point(15.0, 15.0)));
            _binding.GestureArena.Sweep(2);
            Assert.Equal(0, taps);

            Route(Up(1, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Equal(1, taps);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void MovePastTouchSlop_AfterWinning_FiresSpontaneousCancel()
    {
        var events = new List<string>();
        var tap = new TapGestureRecognizer
        {
            OnTapDown = _ => events.Add("down"),
            OnTap = () => events.Add("tap"),
            OnTapCancel = () => events.Add("cancel"),
        };

        try
        {
            var down = Down(1, new Point(10.0, 10.0));
            tap.AddPointer(down);
            _binding.GestureArena.Close(1);
            Route(down);
            Assert.Equal(["down"], events);

            // ~21 px: past the default 18 px post-accept slop, so the won tap self-rejects.
            Route(Move(1, new Point(25.0, 25.0)));
            Assert.Equal(["down", "cancel"], events);

            Route(Up(1, new Point(25.0, 25.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Equal(["down", "cancel"], events);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void ShortMove_DoesNotCancelTheTap()
    {
        var events = new List<string>();
        var tap = new TapGestureRecognizer
        {
            OnTap = () => events.Add("tap"),
            OnTapCancel = () => events.Add("cancel"),
        };

        try
        {
            var down = Down(1, new Point(10.0, 10.0));
            tap.AddPointer(down);
            _binding.GestureArena.Close(1);
            Route(down);
            // ~17 px: inside the default 18 px slop.
            Route(Move(1, new Point(22.0, 22.0)));
            Route(Up(1, new Point(22.0, 22.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Equal(["tap"], events);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void CompetingArena_TapDownFiresAtTheDeadline()
    {
        using var timers = new FakeGestureTimers();
        var events = new List<string>();
        var tap = new TapGestureRecognizer
        {
            OnTapDown = _ => events.Add("down"),
            OnTapCancel = () => events.Add("cancel"),
        };
        var passive = new PassiveArenaMember();

        try
        {
            var down = Down(1, new Point(10.0, 10.0));
            tap.AddPointer(down);
            _binding.GestureArena.Add(1, passive);
            _binding.GestureArena.Close(1);
            Route(down);
            Assert.Empty(events);

            timers.Elapse(GestureConstants.PressTimeout);
            Assert.Equal(["down"], events);

            // A pointer cancel after the deadline fires the cancel with Dart's empty reason.
            Route(Cancel(1, new Point(10.0, 10.0)));
            Assert.Equal(["down", "cancel"], events);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void HeldArena_TapFiresOnlyWhenTheHoldingMemberRejects()
    {
        var events = new List<string>();
        var tap = new TapGestureRecognizer
        {
            OnTapDown = _ => events.Add("down"),
            OnTapUp = _ => events.Add("up"),
            OnTap = () => events.Add("tap"),
        };
        var passive = new PassiveArenaMember();

        try
        {
            var down = Down(1, new Point(10.0, 10.0));
            tap.AddPointer(down);
            GestureArenaEntry entry = _binding.GestureArena.Add(1, passive);
            _binding.GestureArena.Hold(1);
            _binding.GestureArena.Close(1);
            Route(down);
            Route(Up(1, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Empty(events);

            entry.Resolve(GestureDisposition.Rejected);
            _binding.GestureArena.FlushDefaultResolutions();
            Assert.Equal(["down", "up", "tap"], events);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void TwoTapRecognizers_FirstAddedWinsAndLoserStaysSilent()
    {
        var events = new List<string>();
        var tapA = new TapGestureRecognizer
        {
            OnTapDown = _ => events.Add("tapADown"),
            OnTapUp = _ => events.Add("tapAUp"),
            OnTap = () => events.Add("tapATap"),
            OnTapCancel = () => events.Add("tapACancel"),
        };
        var tapB = new TapGestureRecognizer
        {
            OnTapDown = _ => events.Add("tapBDown"),
            OnTapUp = _ => events.Add("tapBUp"),
            OnTap = () => events.Add("tapBTap"),
            OnTapCancel = () => events.Add("tapBCancel"),
        };

        try
        {
            var down = Down(1, new Point(10.0, 10.0));
            tapA.AddPointer(down);
            tapB.AddPointer(down);
            _binding.GestureArena.Close(1);
            Route(down);
            Assert.Empty(events);

            Route(Up(1, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Equal(["tapADown", "tapAUp", "tapATap"], events);
        }
        finally
        {
            tapA.Dispose();
            tapB.Dispose();
        }
    }

    [Fact]
    public void ButtonChangeBeforeTapDown_CancelsSilentlyAndAllowsTheNextTap()
    {
        using var timers = new FakeGestureTimers();
        var events = new List<string>();
        var tap = new TapGestureRecognizer
        {
            OnTapDown = _ => events.Add("down"),
            OnTapUp = _ => events.Add("up"),
            OnTapCancel = () => events.Add("cancel"),
        };
        var passive = new PassiveArenaMember();

        try
        {
            var down = Down(1, new Point(10.0, 10.0));
            tap.AddPointer(down);
            _binding.GestureArena.Add(1, passive);
            _binding.GestureArena.Close(1);
            Route(down);
            Route(Move(1, new Point(10.0, 10.0), buttons: PointerButtons.Primary | PointerButtons.Secondary));
            Route(Up(1, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Empty(events);

            var down2 = Down(2, new Point(10.0, 10.0));
            tap.AddPointer(down2);
            _binding.GestureArena.Close(2);
            Route(down2);
            Route(Up(2, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(2);
            Assert.Equal(["down", "up"], events);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void ButtonChangeAfterTapDown_FiresCancel()
    {
        using var timers = new FakeGestureTimers();
        var events = new List<string>();
        var tap = new TapGestureRecognizer
        {
            OnTapDown = _ => events.Add("down"),
            OnTapUp = _ => events.Add("up"),
            OnTapCancel = () => events.Add("cancel"),
        };
        var passive = new PassiveArenaMember();

        try
        {
            var down = Down(1, new Point(10.0, 10.0));
            tap.AddPointer(down);
            _binding.GestureArena.Add(1, passive);
            _binding.GestureArena.Close(1);
            Route(down);
            timers.Elapse(GestureConstants.PressTimeout);
            Assert.Equal(["down"], events);

            Route(Move(1, new Point(10.0, 10.0), buttons: PointerButtons.Primary | PointerButtons.Secondary));
            Assert.Equal(["down", "cancel"], events);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void SecondaryOnlyRecognizer_DoesNotCompeteForPrimaryTaps()
    {
        var events = new List<string>();
        var primary = new TapGestureRecognizer
        {
            OnTapDown = _ => events.Add("primaryDown"),
            OnTapUp = _ => events.Add("primaryUp"),
        };
        var secondary = new TapGestureRecognizer
        {
            OnSecondaryTapDown = _ => events.Add("secondaryDown"),
        };

        try
        {
            var down = Down(1, new Point(10.0, 10.0));
            primary.AddPointer(down);
            secondary.AddPointer(down);
            _binding.GestureArena.Close(1);
            // The secondary recognizer refused the pointer, so the primary one wins by default —
            // in the microtask Dart schedules, which the next `Route` flushes.
            Assert.Empty(events);
            Route(down);
            Assert.Equal(["primaryDown"], events);
            Route(Up(1, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Equal(["primaryDown", "primaryUp"], events);
        }
        finally
        {
            primary.Dispose();
            secondary.Dispose();
        }
    }

    [Fact]
    public void SecondaryTap_FiresSecondaryCallbacksWithUpBeforeTap()
    {
        var events = new List<string>();
        var tap = new TapGestureRecognizer
        {
            OnTapDown = _ => events.Add("down"),
            OnSecondaryTapDown = _ => events.Add("secondaryDown"),
            OnSecondaryTapUp = _ => events.Add("secondaryUp"),
            OnSecondaryTap = () => events.Add("secondaryTap"),
        };

        try
        {
            var down = Down(1, new Point(10.0, 10.0), buttons: PointerButtons.Secondary);
            tap.AddPointer(down);
            _binding.GestureArena.Close(1);
            Route(down);
            Route(Up(1, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Equal(["secondaryDown", "secondaryUp", "secondaryTap"], events);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void TertiaryTap_FiresTertiaryCallbacksWithoutATapEquivalent()
    {
        var events = new List<string>();
        var tap = new TapGestureRecognizer
        {
            OnTertiaryTapDown = _ => events.Add("tertiaryDown"),
            OnTertiaryTapUp = _ => events.Add("tertiaryUp"),
            OnTertiaryTapCancel = () => events.Add("tertiaryCancel"),
        };

        try
        {
            var down = Down(1, new Point(10.0, 10.0), buttons: PointerButtons.Middle);
            tap.AddPointer(down);
            _binding.GestureArena.Close(1);
            Route(down);
            Route(Up(1, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Equal(["tertiaryDown", "tertiaryUp"], events);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void CombinedButtons_AreRefusedEntirely()
    {
        var events = new List<string>();
        var tap = new TapGestureRecognizer
        {
            OnTapDown = _ => events.Add("down"),
            OnSecondaryTapDown = _ => events.Add("secondaryDown"),
        };

        try
        {
            var down = Down(
                1, new Point(10.0, 10.0), buttons: PointerButtons.Primary | PointerButtons.Secondary);
            tap.AddPointer(down);
            _binding.GestureArena.Close(1);
            Route(down);
            Route(Up(1, new Point(10.0, 10.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Empty(events);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void TapMove_ReportsPositionsAndDelta_WithNullPostAcceptSlop()
    {
        var moves = new List<TapMoveDetails>();
        var tap = new TapGestureRecognizer(postAcceptSlopTolerance: null)
        {
            OnTapMove = details => moves.Add(details),
        };

        try
        {
            var down = Down(1, new Point(10.0, 10.0));
            tap.AddPointer(down);
            _binding.GestureArena.Close(1);
            Route(down);
            Route((PointerMoveEvent)Move(1, new Point(60.0, 10.0)).WithDelta(new Point(50.0, 0.0)));
            Route((PointerMoveEvent)Move(1, new Point(70.0, 20.0)).WithDelta(new Point(10.0, 10.0)));

            Assert.Equal(2, moves.Count);
            Assert.Equal(new Point(60.0, 10.0), moves[0].GlobalPosition);
            Assert.Equal(new Point(50.0, 0.0), moves[0].Delta);
            Assert.Equal(new Point(70.0, 20.0), moves[1].GlobalPosition);
            Assert.Equal(new Point(10.0, 10.0), moves[1].Delta);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void LosingRecognizerWithoutSentTapDown_FiresNoCancel()
    {
        using var timers = new FakeGestureTimers();
        var events = new List<string>();
        var tap = new TapGestureRecognizer
        {
            OnTapDown = _ => events.Add("down"),
            OnTapCancel = () => events.Add("cancel"),
        };
        var passive = new PassiveArenaMember();

        try
        {
            var down = Down(1, new Point(10.0, 10.0));
            tap.AddPointer(down);
            _binding.GestureArena.Add(1, passive);
            _binding.GestureArena.Close(1);
            Route(down);

            // Past the pre-accept slop before any tap-down was sent: silent rejection.
            Route(Move(1, new Point(60.0, 10.0)));
            Route(Up(1, new Point(60.0, 10.0)));
            _binding.GestureArena.Sweep(1);
            Assert.Empty(events);
            Assert.True(passive.Accepted);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void PrimaryPointerRecognizer_TracksStateAcrossASequence()
    {
        var tap = new TapGestureRecognizer
        {
            OnTap = () => { },
        };

        try
        {
            Assert.Equal(GestureRecognizerState.Ready, tap.State);
            Assert.Null(tap.PrimaryPointer);
            Assert.Null(tap.InitialPosition);

            var down = Down(7, new Point(10.0, 11.0));
            tap.AddPointer(down);
            Assert.Equal(GestureRecognizerState.Possible, tap.State);
            Assert.Equal(7, tap.PrimaryPointer);
            Assert.Equal(new Point(10.0, 11.0), tap.InitialPosition!.Value.Global);

            _binding.GestureArena.Close(7);
            Route(down);
            Route(Up(7, new Point(10.0, 11.0)));
            _binding.GestureArena.Sweep(7);

            // Won and completed: the recognizer is ready again, keeping the last primary pointer.
            Assert.Equal(GestureRecognizerState.Ready, tap.State);
            Assert.Equal(7, tap.PrimaryPointer);
            Assert.Null(tap.InitialPosition);
        }
        finally
        {
            tap.Dispose();
        }
    }

    [Fact]
    public void GestureSettings_ChangeTheEffectiveSlopLazily()
    {
        var tap = new TapGestureRecognizer();
        try
        {
            Assert.Equal(GestureConstants.TouchSlop, tap.PreAcceptSlopTolerance);
            tap.GestureSettings = new DeviceGestureSettings(TouchSlop: 5.0);
            Assert.Equal(5.0, tap.PreAcceptSlopTolerance);
            Assert.Equal(5.0, tap.PostAcceptSlopTolerance);
        }
        finally
        {
            tap.Dispose();
        }
    }
}
