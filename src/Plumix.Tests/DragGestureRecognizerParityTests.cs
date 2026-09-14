using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// flutter/packages/flutter/test/gestures/drag_test.dart
// flutter/packages/flutter/test/gestures/monodrag_test.dart

namespace Plumix.Tests;

public sealed partial class DragGestureRecognizerTests
{
    private static DragGestureRecognizer AxisRecognizer(int axis) => axis switch
    {
        0 => new HorizontalDragGestureRecognizer(),
        1 => new VerticalDragGestureRecognizer(),
        _ => new PanGestureRecognizer()
    };

    private static Point AxisPoint(int axis, double value) => axis switch
    {
        0 => new Point(value, 0),
        1 => new Point(0, value),
        _ => new Point(value, value)
    };

    [Fact]
    public void DragDetails_HaveOneLineDescriptions()
    {
        object[] details =
        [
            new DragDownDetails(),
            new DragStartDetails(),
            new DragUpdateDetails(GlobalPosition: default, LocalPosition: default, Delta: default, PrimaryDelta: null),
            new DragEndDetails()
        ];
        foreach (object detail in details)
        {
            string description = detail.ToString()!;
            Assert.NotEmpty(description);
            Assert.DoesNotContain("\n", description);
            Assert.DoesNotContain("\r", description);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Constructors_PreserveEveryDefault(int axis)
    {
        using DragGestureRecognizer drag = AxisRecognizer(axis);
        Assert.Null(drag.DebugOwner);
        Assert.Null(drag.SupportedDevices);
        Assert.Equal(DragStartBehavior.Start, drag.DragStartBehavior);
        Assert.Equal(MultitouchDragStrategy.LatestPointer, drag.MultitouchDragStrategy);
        Assert.False(drag.OnlyAcceptDragOnThreshold);
        Assert.Same(DragGestureRecognizer.DefaultVelocityTrackerBuilder, drag.VelocityTrackerBuilder);
        Assert.Null(drag.MinFlingDistance);
        Assert.Null(drag.MinFlingVelocity);
        Assert.Null(drag.MaxFlingVelocity);
        Assert.True(drag.AllowedButtonsFilter(PointerButtons.Primary));
        Assert.False(drag.AllowedButtonsFilter(PointerButtons.None));
        Assert.False(drag.AllowedButtonsFilter(PointerButtons.Secondary));
        Assert.False(drag.AllowedButtonsFilter(PointerButtons.Primary | PointerButtons.Secondary));
        Assert.Null(drag.OnDown);
        Assert.Null(drag.OnStart);
        Assert.Null(drag.OnUpdate);
        Assert.Null(drag.OnEnd);
        Assert.Null(drag.OnCancel);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Constructors_ForwardOwnerDevicesAndButtonFilter(int axis)
    {
        object owner = new object();
        var devices = new HashSet<PointerDeviceKind> { PointerDeviceKind.Mouse };
        AllowedButtonsFilter filter = buttons => buttons == PointerButtons.Secondary;
        using DragGestureRecognizer drag = axis switch
        {
            0 => new HorizontalDragGestureRecognizer(
                debugOwner: owner, supportedDevices: devices, allowedButtonsFilter: filter),
            1 => new VerticalDragGestureRecognizer(
                debugOwner: owner, supportedDevices: devices, allowedButtonsFilter: filter),
            _ => new PanGestureRecognizer(
                debugOwner: owner, supportedDevices: devices, allowedButtonsFilter: filter)
        };
        Assert.Same(owner, drag.DebugOwner);
        Assert.Same(devices, drag.SupportedDevices);
        Assert.Same(filter, drag.AllowedButtonsFilter);
        int starts = 0;
        drag.OnStart = _ => starts++;
        Begin(drag, Down(1, default, PointerButtons.Secondary));
        Route(Up(1, default));
        Assert.Equal(0, starts);
        Begin(drag, Down(2, default, PointerButtons.Secondary, PointerDeviceKind.Mouse));
        Assert.Equal(1, starts);
        Route(Up(2, default, PointerDeviceKind.Mouse));
    }

    [Theory]
    [InlineData(PointerButtons.Primary, "primary")]
    [InlineData(PointerButtons.Secondary, "secondary")]
    public void DifferentButtonFilters_CanCoexist(PointerButtons buttons, string expected)
    {
        var log = new List<string>();
        using var primary = new HorizontalDragGestureRecognizer(
            allowedButtonsFilter: value => value == PointerButtons.Primary) { OnStart = _ => log.Add("primary") };
        using var secondary = new HorizontalDragGestureRecognizer(
            allowedButtonsFilter: value => value == PointerButtons.Secondary) { OnStart = _ => log.Add("secondary") };
        PointerDownEvent down = Down(1, default, buttons);
        primary.AddPointer(down);
        secondary.AddPointer(down);
        _binding.GestureArena.Close(1);
        Route(down);
        Assert.Equal([expected], log);
        Route(Up(1, default));
    }

    [Fact]
    public void DebugTimestamp_IsBuildGatedAndAcceptToleratesNullAfterRejection()
    {
        using var drag = new ExposedVerticalDrag();
        DateTime timestamp = DateTime.UnixEpoch.AddDays(10);
        var down = new PointerDownEvent(1, PointerDeviceKind.Touch, default, PointerButtons.Primary, timestamp);
        Assert.Null(drag.DebugLastPendingEventTimestamp);
        drag.AddAllowed(down);
        Assert.Equal(Constants.KDebugMode ? timestamp : (DateTime?)null, drag.DebugLastPendingEventTimestamp);
        drag.AcceptGesture(1);
        Assert.Null(drag.DebugLastPendingEventTimestamp);
        drag.RejectGesture(1);
        Assert.Null(drag.DebugLastPendingEventTimestamp);
        drag.AcceptGesture(1);
        Assert.Null(drag.DebugLastPendingEventTimestamp);
    }

    [Fact]
    public void DuplicateAcceptance_UsesDartsDebugContract()
    {
        using var drag = new ExposedVerticalDrag();
        drag.AddAllowed(Down(1, default));
        drag.AcceptGesture(1);
        if (Constants.KDebugMode)
        {
            Assert.Throws<InvalidOperationException>(() => drag.AcceptGesture(1));
        }
        else
        {
            drag.AcceptGesture(1);
        }
    }

    [Fact]
    public void ReadyStateAssertion_IsElidedOutsideDebug()
    {
        using var drag = new ExposedVerticalDrag();
        if (Constants.KDebugMode)
        {
            Assert.Throws<InvalidOperationException>(() => drag.Handle(Up(1, default)));
        }
        else
        {
            drag.Handle(Up(1, default));
        }
    }

    private sealed class ExposedVerticalDrag : VerticalDragGestureRecognizer
    {
        public void AddAllowed(PointerDownEvent @event) => AddAllowedPointer(@event);
        public void Handle(PointerEvent @event) => HandleEvent(@event);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AverageBoundaryPointers_ReproducesFlutterSequenceAcrossFrames(int axis)
    {
        Scheduler.ResetForTests();
        try
        {
            using DragGestureRecognizer drag = AxisRecognizer(axis);
            drag.MultitouchDragStrategy = MultitouchDragStrategy.AverageBoundaryPointers;
            var updates = new List<Point>();
            drag.OnUpdate = details => updates.Add(details.Delta);
            Begin(drag, Down(5, default));
            Route(Move(5, AxisPoint(axis, 100), AxisPoint(axis, 100)));
            Begin(drag, Down(6, default));
            Route(Move(6, AxisPoint(axis, 110), AxisPoint(axis, 110)));
            Begin(drag, Down(7, default));
            Route(Move(7, AxisPoint(axis, -100), AxisPoint(axis, -100)));
            Begin(drag, Down(8, default));
            Route(Move(8, AxisPoint(axis, -110), AxisPoint(axis, -110)));
            Route(Move(5, AxisPoint(axis, 120), AxisPoint(axis, 20)));
            Route(Move(7, AxisPoint(axis, -120), AxisPoint(axis, -20)));
            Scheduler.HandleBeginFrame(TimeSpan.FromMilliseconds(100));
            Scheduler.HandleDrawFrame();
            Route(Move(6, AxisPoint(axis, 120), AxisPoint(axis, 10)));
            Route(Move(8, AxisPoint(axis, -120), AxisPoint(axis, -10)));
            Route(Move(5, AxisPoint(axis, 130), AxisPoint(axis, 10)));
            Route(Move(7, AxisPoint(axis, -130), AxisPoint(axis, -10)));
            double[] expected = axis == 2
                ? [100, 5, -205.0 / 3, -110.0 / 3, 5, -5, 2.5, -2.5, 2.5, -2.5]
                : [100, 10, -100, -10, 10, -10, 10, -10, 0, 0];
            Assert.Equal(expected.Length, updates.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                Point point = AxisPoint(axis, expected[i]);
                Assert.Equal(point.X, updates[i].X, 10);
                Assert.Equal(point.Y, updates[i].Y, 10);
            }

            foreach (int pointer in new[] { 5, 6, 7, 8 })
            {
                Route(Up(pointer, default));
            }
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ChangingMultitouchStrategy_ClearsPriorFrameMovement()
    {
        using var drag = new HorizontalDragGestureRecognizer();
        drag.MultitouchDragStrategy = MultitouchDragStrategy.AverageBoundaryPointers;
        var updates = new List<double?>();
        drag.OnUpdate = details => updates.Add(details.PrimaryDelta);
        Begin(drag, Down(1, default));
        Begin(drag, Down(2, default));
        Route(Move(1, new Point(100, 0), new Point(100, 0)));
        Route(Move(2, new Point(100, 0), new Point(100, 0)));
        drag.MultitouchDragStrategy = MultitouchDragStrategy.SumAllPointers;
        Route(Move(2, new Point(110, 0), new Point(10, 0)));
        drag.MultitouchDragStrategy = MultitouchDragStrategy.AverageBoundaryPointers;
        Route(Move(1, new Point(110, 0), new Point(10, 0)));
        Route(Move(2, new Point(120, 0), new Point(10, 0)));
        Assert.Equal(new double?[] { 100, 0, 10, 10, 0 }, updates);
        Route(Up(1, default));
        Route(Up(2, default));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SourceTimestampsAndFullPositions_ArePreservedEvenForZeroDelta(int axis)
    {
        using DragGestureRecognizer drag = AxisRecognizer(axis);
        DragStartDetails? start = null;
        var updates = new List<DragUpdateDetails>();
        drag.OnStart = details => start = details;
        drag.OnUpdate = updates.Add;
        var down = new PointerDownEvent(
            1, PointerDeviceKind.Touch, new Point(10, 10), PointerButtons.Primary,
            DateTime.UnixEpoch.AddMilliseconds(100));
        Begin(drag, down);
        Route(Move(1, new Point(20, 25), new Point(10, 15), milliseconds: 200));
        Route(Move(1, new Point(20, 25), default, milliseconds: 300));
        DragStartDetails started = Assert.IsType<DragStartDetails>(start);
        Assert.Equal(down.TimestampUtc, started.SourceTimeStampUtc);
        Assert.Equal(DateTime.UnixEpoch.AddMilliseconds(200), updates[0].SourceTimeStampUtc);
        Assert.Equal(DateTime.UnixEpoch.AddMilliseconds(300), updates[1].SourceTimeStampUtc);
        Assert.Equal(new Point(20, 25), updates[0].GlobalPosition);
        Assert.Equal(new Point(20, 25), updates[0].LocalPosition);
        Assert.Equal(default, updates[1].Delta);
        Route(Up(1, default));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void FlingThresholds_AreStrictAndRespectOverrides(int axis)
    {
        using DragGestureRecognizer drag = AxisRecognizer(axis);
        drag.OnStart = _ => { };
        Begin(drag, Down(1, default));
        Vector speed = axis == 1 ? new Vector(0, 50) : new Vector(50, 0);
        Vector distance = axis == 1 ? new Vector(0, 18) : new Vector(18, 0);
        var estimate = new VelocityEstimate(speed, 1, TimeSpan.FromMilliseconds(20), distance);
        Assert.False(drag.IsFlingGesture(estimate, PointerDeviceKind.Touch));
        Assert.False(drag.IsFlingGesture(estimate with { PixelsPerSecond = speed * 2 }, PointerDeviceKind.Touch));
        Assert.False(drag.IsFlingGesture(estimate with { Offset = distance * 2 }, PointerDeviceKind.Touch));
        Assert.True(drag.IsFlingGesture(
            estimate with { PixelsPerSecond = speed * 2, Offset = distance * 2 }, PointerDeviceKind.Touch));
        drag.MinFlingVelocity = 49;
        drag.MinFlingDistance = 17;
        Assert.True(drag.IsFlingGesture(estimate, PointerDeviceKind.Touch));
        drag.MinFlingVelocity = 30;
        drag.MaxFlingVelocity = 40;
        DragEndDetails fling = Assert.IsType<DragEndDetails>(drag.ConsiderFling(estimate, PointerDeviceKind.Touch));
        Assert.Equal(40, fling.Velocity.PixelsPerSecond.Length, 8);
        Route(Up(1, default));
    }

    [Theory]
    [InlineData(-8000, -8000, -5656.85424949238, -5656.85424949238)]
    [InlineData(-8000, 6000, -6400, 4800)]
    [InlineData(500, 1000, 500, 1000)]
    public void PanFling_ClampsMagnitudeIsotropically(double x, double y, double expectedX, double expectedY)
    {
        using var drag = new PanGestureRecognizer { OnStart = _ => { } };
        Begin(drag, Down(1, new Point(10, 20)));
        var estimate = new VelocityEstimate(new Vector(x, y), 1, TimeSpan.FromMilliseconds(20), new Vector(50, 50));
        DragEndDetails fling = Assert.IsType<DragEndDetails>(drag.ConsiderFling(estimate, PointerDeviceKind.Touch));
        Assert.Equal(expectedX, fling.Velocity.PixelsPerSecond.X, 8);
        Assert.Equal(expectedY, fling.Velocity.PixelsPerSecond.Y, 8);
        Assert.Null(fling.PrimaryVelocity);
        Assert.Equal(new Point(10, 20), fling.GlobalPosition);
        Route(Up(1, default));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void QuickFlick_UsesThreeRealSamplesAndIgnoresSynthesizedWobble(bool synthesizedWobble)
    {
        using var drag = new HorizontalDragGestureRecognizer();
        DragEndDetails? end = null;
        drag.OnEnd = details => end = details;
        Begin(drag, Down(1, default));
        Route(Move(1, new Point(10, 0), new Point(10, 0), milliseconds: 10));
        Route(Move(1, new Point(20, 0), new Point(10, 0), milliseconds: 20));
        if (synthesizedWobble)
        {
            var wobble = new PointerMoveEvent(
                1, PointerDeviceKind.Touch, new Point(21, 1), PointerButtons.Primary, true,
                DateTime.UnixEpoch.AddMilliseconds(30)) { Synthesized = true };
            Route(wobble);
        }

        Route(Up(1, default));
        DragEndDetails ended = Assert.IsType<DragEndDetails>(end);
        Assert.Equal(1000, ended.Velocity.PixelsPerSecond.X, 6);
        Assert.Equal(0, ended.Velocity.PixelsPerSecond.Y);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ButtonChange_ResetsForTheNextDrag(bool accepted)
    {
        using var drag = new HorizontalDragGestureRecognizer();
        var log = new List<string>();
        drag.OnStart = _ => log.Add("start");
        drag.OnEnd = _ => log.Add("end");
        drag.OnCancel = () => log.Add("cancel");
        if (accepted)
        {
            Begin(drag, Down(1, default));
        }
        else
        {
            BeginContested(drag, Down(1, default));
        }

        Route(Move(1, new Point(1, 0), new Point(1, 0), PointerButtons.Secondary));
        Assert.Equal(accepted ? new[] { "start", "end" } : ["cancel"], log);
        log.Clear();
        Begin(drag, Down(2, default));
        Route(Up(2, default));
        Assert.Equal(["start", "end"], log);
    }

    [Fact]
    public void PendingPointerUp_AfterAnotherPointerWins_DoesNotCrash()
    {
        using var vertical = new VerticalDragGestureRecognizer { OnStart = _ => { }, OnEnd = _ => { } };
        using var horizontal = new HorizontalDragGestureRecognizer { OnStart = _ => { } };
        Begin(vertical, Down(90, new Point(10, 10)));
        PointerDownEvent down = Down(91, new Point(20, 20));
        horizontal.AddPointer(down);
        vertical.AddPointer(down);
        _binding.GestureArena.Close(91);
        Route(down);
        Route(Up(90, new Point(10, 10)));
        Route(Up(91, new Point(20, 20)));
    }

    [Fact]
    public void DisposingCompetitor_AcceptsDrag_AndDisposalWithPendingPointerIsSafe()
    {
        using var drag = new HorizontalDragGestureRecognizer();
        using var tap = new TapGestureRecognizer { OnTap = () => { } };
        using var tap2 = new TapGestureRecognizer { OnTap = () => { } };
        int starts = 0;
        drag.OnStart = _ => starts++;
        drag.OnEnd = _ => { };
        PointerDownEvent down = Down(5, default);
        drag.AddPointer(down);
        tap.AddPointer(down);
        _binding.GestureArena.Close(5);
        Route(down);
        down = Down(6, default);
        drag.AddPointer(down);
        tap2.AddPointer(down);
        _binding.GestureArena.Close(6);
        Route(down);
        Assert.Equal(0, starts);
        tap.Dispose();
        _binding.GestureArena.FlushDefaultResolutions();
        Assert.Equal(1, starts);
        Route(Up(5, default));
        drag.Dispose();
    }

    [Fact]
    public void SubclassedPan_CanBeatCustomScrollViewThroughMountedDetector()
    {
        Scheduler.ResetForTests();
        int starts = 0;
        var harness = new ScrollSemanticsHarness(
            new Directionality(TextDirection.Ltr,
                new CustomScrollView(slivers:
                [
                    new SliverToBoxAdapter(child: new RawGestureDetector(
                        behavior: HitTestBehavior.Translucent,
                        gestures: new Dictionary<Type, IGestureRecognizerFactory>
                        {
                            [typeof(EagerPan)] = new GestureRecognizerFactoryWithHandlers<EagerPan>(
                                () => new EagerPan(), drag => drag.OnStart = _ => starts++)
                        },
                        child: new SizedBox(width: 100, height: 100)))
                ])));
        try
        {
            harness.Pump(new Size(200, 200));
            _binding.HandlePointerEvent(harness.RenderView, Down(1, new Point(50, 50)));
            _binding.HandlePointerEvent(
                harness.RenderView, Move(1, new Point(80, 69), new Point(30, 19), milliseconds: 16));
            Assert.Equal(1, starts);
            _binding.HandlePointerEvent(harness.RenderView, Up(1, new Point(80, 69)));
        }
        finally
        {
            harness.RootElement.UnmountRoot();
            Scheduler.ResetForTests();
        }
    }

    private sealed class EagerPan : PanGestureRecognizer
    {
        public override bool HasSufficientGlobalDistanceToAccept(PointerDeviceKind kind, double? touchSlop) =>
            Math.Abs(GlobalDistanceMoved) > PointerEventUtils.ComputeHitSlop(kind, GestureSettings);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AllPendingPointersUp_CancelsOnce_WithFlutterTapOrdering(bool reverse)
    {
        var log = new List<string>();
        using var drag = new HorizontalDragGestureRecognizer
        {
            OnDown = _ => log.Add("downD"),
            OnStart = _ => log.Add("startD"),
            OnCancel = () => log.Add("cancelD")
        };
        using var tap = new TapGestureRecognizer
        {
            OnTapDown = _ => log.Add("downT"),
            OnTapUp = _ => log.Add("upT"),
            OnTapCancel = () => { }
        };
        foreach (int pointer in new[] { 1, 2 })
        {
            PointerDownEvent down = Down(pointer, default);
            drag.AddPointer(down);
            tap.AddPointer(down);
            _binding.GestureArena.Close(pointer);
            Route(down);
        }

        Assert.Equal(["downD"], log);
        log.Clear();
        int first = reverse ? 2 : 1;
        Route(Up(first, default));
        _binding.GestureArena.Sweep(first);
        Assert.Equal(reverse ? Array.Empty<string>() : ["downT", "upT"], log);
        log.Clear();
        int last = reverse ? 1 : 2;
        Route(Up(last, default));
        _binding.GestureArena.Sweep(last);
        Assert.Equal(reverse ? new[] { "cancelD", "downT", "upT" } : ["cancelD"], log);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstPendingPointerLeaves_RemainingPointerCanAccept(bool buttonChange)
    {
        var log = new List<string>();
        using var drag = new HorizontalDragGestureRecognizer
        {
            OnStart = _ => log.Add("start"),
            OnUpdate = _ => log.Add("update"),
            OnEnd = _ => log.Add("end")
        };
        using var tap = new TapGestureRecognizer { OnTapUp = _ => { }, OnTapCancel = () => { } };
        foreach (int pointer in new[] { 1, 2 })
        {
            PointerDownEvent down = Down(pointer, default);
            drag.AddPointer(down);
            tap.AddPointer(down);
            _binding.GestureArena.Close(pointer);
            Route(down);
        }

        if (buttonChange)
        {
            Route(Move(1, new Point(-0.1, -0.1), new Point(-0.1, -0.1), PointerButtons.Secondary));
            Assert.Equal(["start"], log);
            log.Clear();
        }
        else
        {
            Route(Up(1, default));
            _binding.GestureArena.Sweep(1);
            Assert.Empty(log);
        }

        Route(Move(2, new Point(100, 100), new Point(100, 100)));
        Assert.Equal(buttonChange ? new[] { "update" } : ["start"], log);
        log.Clear();
        Route(Up(2, default));
        Assert.Equal(["end"], log);
    }

    [Fact]
    public void FirstPointerAccepts_JoiningPointerLeaves_OnlyLastPointerEnds()
    {
        var log = new List<string>();
        using var drag = new HorizontalDragGestureRecognizer
        {
            OnStart = _ => log.Add("start"),
            OnEnd = _ => log.Add("end")
        };
        using var tap = new TapGestureRecognizer { OnTapUp = _ => { }, OnTapCancel = () => { } };
        foreach (int pointer in new[] { 1, 2 })
        {
            PointerDownEvent down = Down(pointer, default);
            drag.AddPointer(down);
            tap.AddPointer(down);
            _binding.GestureArena.Close(pointer);
            Route(down);
        }

        Route(Move(1, new Point(100, 100), new Point(100, 100)));
        Assert.Equal(["start"], log);
        Route(Up(2, default));
        Assert.Equal(["start"], log);
        Route(Up(1, default));
        Assert.Equal(["start", "end"], log);
    }

    [Fact]
    public void DefaultWinningFourthPointer_IntermediateRejections_DoNotEndUntilLastCancel()
    {
        int starts = 0;
        int ends = 0;
        using var drag = new VerticalDragGestureRecognizer
        {
            OnStart = _ => starts++,
            OnEnd = _ => ends++
        };
        using var tap = new TapGestureRecognizer { OnTapUp = _ => { }, OnTapCancel = () => { } };
        for (int pointer = 1; pointer <= 4; pointer++)
        {
            PointerDownEvent down = Down(pointer, default);
            if (pointer != 4)
            {
                tap.AddPointer(down);
            }

            drag.AddPointer(down);
            _binding.GestureArena.Close(pointer);
            Route(down);
        }

        Assert.Equal(1, starts);
        Route(Up(2, default));
        _binding.GestureArena.Sweep(2);
        Route(Cancel(4, default));
        Route(Cancel(3, default));
        Assert.Equal(0, ends);
        Route(Cancel(1, default));
        Assert.Equal(1, ends);
    }

    [Theory]
    [InlineData(PointerButtons.Primary, false)]
    [InlineData(PointerButtons.Secondary, true)]
    public void PrimaryPan_OnlyCompetesWithPrimaryTap(PointerButtons buttons, bool immediateTapDown)
    {
        int tapDowns = 0;
        using var pan = new PanGestureRecognizer { OnStart = _ => { } };
        using var tap = new TapGestureRecognizer
        {
            OnTapDown = _ => tapDowns++,
            OnSecondaryTapDown = _ => tapDowns++
        };
        PointerDownEvent down = Down(1, default, buttons);
        pan.AddPointer(down);
        tap.AddPointer(down);
        _binding.GestureArena.Close(1);
        Route(down);
        Assert.Equal(immediateTapDown ? 1 : 0, tapDowns);
        Route(Up(1, default));
        _binding.GestureArena.Sweep(1);
        Assert.Equal(1, tapDowns);
    }

    [Fact]
    public void PanZoomVelocitySamples_UseGlobalPan_AndIgnoreSynthesizedSamples()
    {
        var tracker = new RecordingVelocityTracker();
        using var drag = new PanGestureRecognizer
        {
            OnStart = _ => { },
            VelocityTrackerBuilder = _ => tracker
        };
        Matrix4 transform = Matrix4.Diagonal3Values(2, 2, 1);
        var pointer = new PanZoomPointer(1);
        PointerEvent start = pointer.Start(new Point(20, 30)).Transformed(transform);
        drag.AddPointerPanZoom((PointerPanZoomStartEvent)start);
        _binding.GestureArena.Close(1);
        Route(start);
        Route(pointer.Update(new Point(20, 30), new Point(10, 15), milliseconds: 10).Transformed(transform));
        var synthesized = new PointerPanZoomUpdateEvent(
            1, new Point(20, 30), DateTime.UnixEpoch.AddMilliseconds(20),
            pan: new Point(12, 18), panDelta: new Point(2, 3)) { Synthesized = true };
        Route(synthesized.Transformed(transform));
        Assert.Equal(new[] { default(Point), new Point(10, 15) }, tracker.Positions);
        Route(pointer.End(new Point(20, 30)));
    }

    private sealed class RecordingVelocityTracker() : VelocityTracker(PointerDeviceKind.Trackpad)
    {
        public List<Point> Positions { get; } = [];
        public override void AddPosition(DateTime timestampUtc, Point position) => Positions.Add(position);
    }
}
