using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.UI;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/gestures/recognizer_test.dart

namespace Plumix.Tests;

public sealed class GestureRecognizerTests : IDisposable
{
    private readonly GestureBinding _binding = GestureBinding.Instance;
    private readonly FakeGestureTimers _timers = new();

    public GestureRecognizerTests() => _binding.ResetForTests();

    public void Dispose()
    {
        _timers.Dispose();
        _binding.ResetForTests();
    }

    private static PointerDownEvent Down(int pointer = 5, PointerDeviceKind kind = PointerDeviceKind.Touch,
        PointerButtons buttons = PointerButtons.Primary) =>
        new(pointer, kind, new Point(10, 10), buttons, DateTime.UnixEpoch);

    private static PointerMoveEvent Move(int pointer = 5, Point? position = null) =>
        new(pointer, PointerDeviceKind.Touch, position ?? new Point(15, 15),
            PointerButtons.Primary, down: true, DateTime.UnixEpoch);

    private static PointerUpEvent Up(int pointer = 5) =>
        new(pointer, PointerDeviceKind.Touch, new Point(15, 15), PointerButtons.None, DateTime.UnixEpoch);

    private void Begin(GestureRecognizer recognizer, int pointer = 5)
    {
        _binding.GestureArena.Add(pointer, new IndefiniteRecognizer());
        recognizer.AddPointer(Down(pointer));
        _binding.GestureArena.Close(pointer);
        Scheduler.FlushMicrotasks();
        _binding.PointerRouter.Route(Down(pointer));
    }

    [DebugOnlyFact]
    public void SmokeTest_ExposesDiagnosticTreeAndOwner()
    {
        using var recognizer = new IndefiniteRecognizer(debugOwner: 0);
        Assert.Contains("debugOwner: 0", recognizer.ToStringDeep());
        Assert.Empty(recognizer.DebugDescribeChildren());
    }

    [Fact]
    public void Lifecycle_DispatchesCreationAndDisposalOnlyInDebug()
    {
        var events = new List<ObjectEvent>();
        void Listener(ObjectEvent @event)
        {
            if (@event.Object is IndefiniteRecognizer)
            {
                events.Add(@event);
            }
        }

        FlutterMemoryAllocations.Instance.AddListener(Listener);
        IndefiniteRecognizer recognizer;
        try
        {
            recognizer = new IndefiniteRecognizer();
            recognizer.Dispose();
        }
        finally
        {
            FlutterMemoryAllocations.Instance.RemoveListener(Listener);
        }

        if (Constants.KDebugMode)
        {
            Assert.Collection(events,
                @event =>
                {
                    var created = Assert.IsType<ObjectCreated>(@event);
                    Assert.Equal("GestureRecognizer", created.ClassName);
                    Assert.Equal("package:flutter/gestures.dart", created.Library);
                    Assert.Same(recognizer, created.Object);
                },
                @event => Assert.Same(recognizer, Assert.IsType<ObjectDisposed>(@event).Object));
        }
        else
        {
            Assert.Empty(events);
        }
    }

    [Fact]
    public void OffsetPair_ArithmeticAndEventFactoriesPreserveBothCoordinateSpaces()
    {
        var first = new OffsetPair(new Point(10, 20), new Point(30, 40));
        var second = new OffsetPair(new Point(50, 60), new Point(70, 80));
        Assert.Equal(new OffsetPair(new Point(60, 80), new Point(100, 120)), second + first);
        Assert.Equal(new OffsetPair(new Point(40, 40), new Point(40, 40)), second - first);
        Assert.Equal(new OffsetPair(default, default), OffsetPair.Zero);
        Assert.Equal("OffsetPair(local: Offset(10.0, 20.0), global: Offset(30.0, 40.0))", first.ToString());

        Matrix4 transform = Matrix4.Identity();
        transform.ScaleByDouble(2, 2, 1, 1);
        var move = (PointerMoveEvent)Move().Transformed(transform);
        Assert.Equal(new OffsetPair(move.LocalPosition, move.Position), OffsetPair.FromEventPosition(move));
        Assert.Equal(new OffsetPair(move.LocalDelta, move.Delta), OffsetPair.FromEventDelta(move));
    }

    [Fact]
    public void Base_RecordsDisallowedEventsAndPanZoomBypassesButtonsFilter()
    {
        int filters = 0;
        using var recognizer = new IndefiniteRecognizer(
            supportedDevices: new HashSet<PointerDeviceKind> { PointerDeviceKind.Trackpad },
            allowedButtonsFilter: _ => { filters++; return false; });
        recognizer.AddPointer(Down(kind: PointerDeviceKind.Mouse, buttons: PointerButtons.Secondary));
        Assert.Equal((PointerDeviceKind.Mouse, PointerButtons.Secondary), recognizer.Metadata(5));
        Assert.Equal(0, filters);
        Assert.Equal(1, recognizer.DisallowedDowns);
        recognizer.AddPointerPanZoom(new PointerPanZoomStartEvent(5, default, DateTime.UnixEpoch));
        Assert.Equal((PointerDeviceKind.Trackpad, PointerButtons.None), recognizer.Metadata(5));
        Assert.Equal(1, recognizer.AllowedPanZooms);
        Assert.Equal(0, filters);
        recognizer.Dispose();
        Assert.Equal((PointerDeviceKind.Trackpad, PointerButtons.None), recognizer.Metadata(5));
    }

    [Fact]
    public void Base_DefaultFilterAcceptsEveryButtonsCombination()
    {
        using var recognizer = new IndefiniteRecognizer();
        Assert.Null(recognizer.DebugOwner);
        Assert.Null(recognizer.GestureSettings);
        Assert.Null(recognizer.SupportedDevices);
        recognizer.AddPointer(Down(buttons: PointerButtons.Primary | PointerButtons.Secondary));
        Assert.Equal(1, recognizer.AllowedDowns);
        Assert.Equal(0, recognizer.DisallowedDowns);
    }

    [Fact]
    public void Callback_ReportsExceptionAndStackThenAllowsAnotherCallback()
    {
        using var recognizer = new IndefiniteRecognizer();
        var reported = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            Assert.Null(recognizer.Call<string>(() => throw new InvalidOperationException("callback failed")));
            Assert.Equal("next", recognizer.Call(() => "next"));
            FlutterErrorDetails details = Assert.Single(reported);
            Assert.Equal("gesture", details.Library);
            Assert.Equal("while handling a gesture", details.Context?.ToDescription());
            Assert.Contains("callback failed", Assert.IsType<InvalidOperationException>(details.Exception).Message);
            Assert.NotNull(details.Stack);
            Assert.Equal(Constants.KDebugMode, details.InformationCollector is not null);
            if (details.InformationCollector is { } collector)
            {
                Assert.Equal(new[] { "Handler", "Recognizer" }, collector().Select(node => node.Name));
            }
        }
        finally
        {
            FlutterError.OnError = previous;
        }
    }

    [Fact]
    public void Sequence_PanZoomHooksAreNoOpsAndDisallowedDownRejectsActiveEntries()
    {
        using var recognizer = new SequenceRecognizer();
        recognizer.AddPointerPanZoom(new PointerPanZoomStartEvent(5, default, DateTime.UnixEpoch));
        _binding.PointerRouter.Route(new PointerPanZoomEndEvent(5, default, DateTime.UnixEpoch));
        Assert.Empty(recognizer.Events);
        Assert.Empty(recognizer.Stops);
        recognizer.AddPointer(Down());
        recognizer.SupportedDevices = new HashSet<PointerDeviceKind> { PointerDeviceKind.Mouse };
        recognizer.AddPointer(Down(6));
        Assert.Equal(new[] { 5 }, recognizer.Rejections);
    }

    [Fact]
    public void Sequence_RoutesTransformedEventsAndStopsOnlyTheLastPointer()
    {
        using var recognizer = new SequenceRecognizer();
        Matrix4 transform = Matrix4.Identity();
        transform.ScaleByDouble(2, 2, 1, 1);
        recognizer.AddPointer((PointerDownEvent)Down().Transformed(transform));
        recognizer.AddPointer(Down(6));
        _binding.PointerRouter.Route(Move());
        Assert.Equal(new Point(30, 30), Assert.Single(recognizer.Events).LocalPosition);
        _binding.PointerRouter.Route(Up());
        recognizer.Stop(5);
        Assert.Empty(recognizer.Stops);
        _binding.PointerRouter.Route(new PointerCancelEvent(
            6, PointerDeviceKind.Touch, default, PointerButtons.None, DateTime.UnixEpoch));
        Assert.Equal(new[] { 6 }, recognizer.Stops);
        int count = recognizer.Events.Count;
        _binding.PointerRouter.Route(Move());
        Assert.Equal(count, recognizer.Events.Count);
    }

    [Fact]
    public void Sequence_PanZoomTerminalHelperRemovesExplicitlyTrackedRoute()
    {
        using var recognizer = new SequenceRecognizer();
        recognizer.Track(5);
        _binding.PointerRouter.Route(new PointerPanZoomEndEvent(5, default, DateTime.UnixEpoch));
        Assert.Equal(new[] { 5 }, recognizer.Stops);
    }

    [Fact]
    public void Sequence_ResolveSnapshotsEntriesBeforeReentrantArenaCallbacks()
    {
        using var recognizer = new SequenceRecognizer();
        recognizer.AddPointer(Down());
        recognizer.OnReject = pointer =>
        {
            if (pointer == 5)
            {
                recognizer.AddPointer(Down(6));
            }
        };
        recognizer.Resolve(GestureDisposition.Rejected);
        Assert.Equal(new[] { 5 }, recognizer.Rejections);
        recognizer.ResolveOne(6, GestureDisposition.Rejected);
        recognizer.ResolveOne(6, GestureDisposition.Rejected);
        Assert.Equal(new[] { 5, 6 }, recognizer.Rejections);
    }

    [Fact]
    public void Sequence_DisposeRejectsArenasAndRemovesRoutesWithoutLastPointerCallback()
    {
        var recognizer = new SequenceRecognizer();
        recognizer.AddPointer(Down());
        recognizer.AddPointer(Down(6));
        recognizer.Dispose();
        Assert.Equal(new[] { 5, 6 }, recognizer.Rejections);
        Assert.Empty(recognizer.Stops);
        _binding.PointerRouter.Route(Move());
        Assert.Empty(recognizer.Events);
    }

    [Fact]
    public void Team_WinsThroughTheAssignedCaptain()
    {
        using var first = new SequenceRecognizer();
        using var captain = new SequenceRecognizer();
        var team = new GestureArenaTeam { Captain = captain };
        first.Team = team;
        captain.Team = team;
        first.AddPointer(Down());
        captain.AddPointer(Down());
        _binding.GestureArena.Close(5);
        first.Resolve(GestureDisposition.Accepted);
        Assert.Equal(new[] { 5 }, captain.Acceptances);
        Assert.Equal(new[] { 5 }, first.Rejections);
    }

    [Fact]
    public void Team_ContractChecksAreDebugOnlyAndIncludeUnresolvedStoppedPointers()
    {
        using var recognizer = new SequenceRecognizer();
        recognizer.AddPointer(Down());
        recognizer.Stop(5);
        var team = new GestureArenaTeam();
        if (Constants.KDebugMode)
        {
            Assert.Throws<InvalidOperationException>(() => recognizer.Team = team);
            recognizer.Resolve(GestureDisposition.Rejected);
            Assert.Throws<InvalidOperationException>(() => recognizer.Team = null);
            recognizer.Team = team;
            Assert.Throws<InvalidOperationException>(() => recognizer.Team = new GestureArenaTeam());
        }
        else
        {
            recognizer.Team = team;
            recognizer.Team = null;
            Assert.Null(recognizer.Team);
        }
    }

    [Fact]
    public void Primary_WinningArenaResetsStateAndRetainsPrimaryPointer()
    {
        using var recognizer = new PrimaryRecognizer<PointerUpEvent>();
        Assert.Equal(GestureRecognizerState.Ready, recognizer.State);
        Assert.Null(recognizer.PrimaryPointer);
        Assert.Null(recognizer.InitialPosition);
        Begin(recognizer);
        Assert.Equal(GestureRecognizerState.Possible, recognizer.State);
        Assert.Equal(5, recognizer.PrimaryPointer);
        Assert.Equal(new OffsetPair(new Point(10, 10), new Point(10, 10)), recognizer.InitialPosition);
        _binding.PointerRouter.Route(Up());
        Assert.Equal(GestureRecognizerState.Ready, recognizer.State);
        Assert.Equal(5, recognizer.PrimaryPointer);
        Assert.Null(recognizer.InitialPosition);
        Assert.Equal(new[] { "accepted" }, recognizer.Resolutions);
    }

    [Fact]
    public void Primary_LosingArenaStaysDefunctUntilUpThenResets()
    {
        using var recognizer = new PrimaryRecognizer<PointerMoveEvent>(GestureDisposition.Rejected);
        Begin(recognizer);
        _binding.PointerRouter.Route(Move());
        Assert.Equal(GestureRecognizerState.Defunct, recognizer.State);
        Assert.Equal(5, recognizer.PrimaryPointer);
        Assert.NotNull(recognizer.InitialPosition);
        _binding.PointerRouter.Route(Up());
        Assert.Equal(GestureRecognizerState.Ready, recognizer.State);
        Assert.Equal(5, recognizer.PrimaryPointer);
        Assert.Null(recognizer.InitialPosition);
        Assert.Equal(new[] { "rejected" }, recognizer.Resolutions);
    }

    [Fact]
    public void Primary_RecycledAcceptanceDoesNotSuppressNextPreAcceptSlopRejection()
    {
        using var recognizer = new PrimaryRecognizer<PointerUpEvent>(pre: 15, post: 1000);
        Begin(recognizer);
        _binding.PointerRouter.Route(Up());
        Begin(recognizer, 6);
        _binding.PointerRouter.Route(Move(6, new Point(100, 200)));
        _binding.PointerRouter.Route(Up(6));
        Assert.Equal(new[] { "accepted", "rejected" }, recognizer.Resolutions);
        Assert.Equal(GestureRecognizerState.Ready, recognizer.State);
    }

    [Fact]
    public void Primary_GlobalPostAcceptSlopIgnoresLocalScalingAndWaitsForAdditionalPointer()
    {
        using var recognizer = new PrimaryRecognizer<PointerDownEvent>(pre: null, post: 5);
        _binding.GestureArena.Add(5, new IndefiniteRecognizer());
        Matrix4 transform = Matrix4.Identity();
        transform.ScaleByDouble(0.1, 0.1, 1, 1);
        recognizer.AddPointer((PointerDownEvent)Down().Transformed(transform));
        recognizer.AddPointer(Down(6));
        _binding.PointerRouter.Route(Down());
        _binding.GestureArena.Close(5);
        Scheduler.FlushMicrotasks();
        Assert.Equal(new[] { "accepted" }, recognizer.Resolutions);
        _binding.PointerRouter.Route(Move(position: new Point(15, 10)));
        Assert.Equal(GestureRecognizerState.Possible, recognizer.State);
        _binding.PointerRouter.Route(Move(position: new Point(16, 10)));
        // The primary route stops at global slop even though its local displacement is only 0.6.
        Assert.Equal(GestureRecognizerState.Possible, recognizer.State);
        Assert.NotNull(recognizer.InitialPosition);
        _binding.PointerRouter.Route(Up(6));
        Assert.Equal(GestureRecognizerState.Ready, recognizer.State);
        Assert.Null(recognizer.InitialPosition);
        Assert.Equal(5, recognizer.PrimaryPointer);
    }

    [Fact]
    public void Primary_AcceptanceCancelsDeadlineAndIgnoresDisallowedExtraPointer()
    {
        using var recognizer = new PrimaryRecognizer<PointerDownEvent>(deadline: TimeSpan.FromMilliseconds(20));
        _binding.GestureArena.Add(5, new IndefiniteRecognizer());
        recognizer.AddPointer(Down());
        _binding.PointerRouter.Route(Down());
        _binding.GestureArena.Close(5);
        Scheduler.FlushMicrotasks();
        recognizer.SupportedDevices = new HashSet<PointerDeviceKind> { PointerDeviceKind.Touch };
        recognizer.AddPointer(Down(6, PointerDeviceKind.Mouse));
        _timers.Elapse(TimeSpan.FromSeconds(1));
        Assert.Empty(recognizer.Deadlines);
        Assert.Equal(new[] { "accepted" }, recognizer.Resolutions);
        _binding.PointerRouter.Route(Up());
        Assert.Equal(GestureRecognizerState.Ready, recognizer.State);
    }

    [Theory]
    [InlineData(-1.0, null, false)]
    [InlineData(-1.0, 5.0, true)]
    [InlineData(5.0, null, true)]
    [InlineData(null, null, false)]
    public void Primary_DefaultExplicitAndNullSlopMatchFlutter(double? pre, double? settings, bool rejects)
    {
        using var recognizer = new PrimaryRecognizer<PointerUpEvent>(pre: pre, post: null);
        Assert.Equal(pre == -1.0 ? 18.0 : pre, recognizer.PreAcceptSlopTolerance);
        if (settings is { } touchSlop)
        {
            recognizer.GestureSettings = new DeviceGestureSettings(touchSlop);
        }

        Begin(recognizer);
        _binding.PointerRouter.Route(Move(position: pre is null ? new Point(100, 200) : null));
        _binding.PointerRouter.Route(Up());
        Assert.Equal(new[] { rejects ? "rejected" : "accepted" }, recognizer.Resolutions);
    }

    [Fact]
    public void Primary_DeadlineUsesOriginalDownAndStopsAfterAcceptanceOrDisposal()
    {
        var recognizer = new PrimaryRecognizer<PointerUpEvent>(deadline: TimeSpan.FromMilliseconds(20));
        var down = Down();
        recognizer.AddPointer(down);
        _timers.Elapse(TimeSpan.FromMilliseconds(20));
        Assert.Same(down, Assert.Single(recognizer.Deadlines));
        recognizer.AcceptGesture(5);
        recognizer.Dispose();
        _timers.Elapse(TimeSpan.FromSeconds(1));
        Assert.Single(recognizer.Deadlines);
    }

    [Fact]
    public void Primary_ToleranceValidationIsDebugOnlyIncludingNaN()
    {
        foreach (double value in new[] { -2.0, double.NaN })
        {
            if (Constants.KDebugMode)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new PrimaryRecognizer<PointerUpEvent>(pre: value));
                Assert.Throws<ArgumentOutOfRangeException>(() => new PrimaryRecognizer<PointerUpEvent>(post: value));
            }
            else
            {
                using var recognizer = new PrimaryRecognizer<PointerUpEvent>(pre: value, post: value);
                Assert.Equal(value, recognizer.PreAcceptSlopTolerance);
                Assert.Equal(value, recognizer.PostAcceptSlopTolerance);
            }
        }
    }

    private sealed class IndefiniteRecognizer : GestureRecognizer
    {
        public IndefiniteRecognizer(object? debugOwner = null,
            IReadOnlySet<PointerDeviceKind>? supportedDevices = null, AllowedButtonsFilter? allowedButtonsFilter = null)
            : base(debugOwner: debugOwner,
                supportedDevices: supportedDevices,
                allowedButtonsFilter: allowedButtonsFilter)
        {
        }

        public int AllowedDowns { get; private set; }
        public int DisallowedDowns { get; private set; }
        public int AllowedPanZooms { get; private set; }
        public override string DebugDescription => "indefinite";
        public override void AcceptGesture(int pointer) { }
        public override void RejectGesture(int pointer) { }
        public (PointerDeviceKind, PointerButtons) Metadata(int pointer) =>
            (GetKindForPointer(pointer), GetButtonsForPointer(pointer));
        public T? Call<T>(Func<T> callback) => InvokeCallback("test", callback);
        protected override void AddAllowedPointer(PointerDownEvent @event) => AllowedDowns++;
        protected override void HandleNonAllowedPointer(PointerDownEvent @event) => DisallowedDowns++;
        protected override void AddAllowedPointerPanZoom(PointerPanZoomStartEvent @event) => AllowedPanZooms++;
    }

    private sealed class SequenceRecognizer : OneSequenceGestureRecognizer
    {
        public List<PointerEvent> Events { get; } = [];
        public List<int> Stops { get; } = [];
        public List<int> Rejections { get; } = [];
        public List<int> Acceptances { get; } = [];
        public Action<int>? OnReject { get; set; }
        public override string DebugDescription => "sequence";
        public void Track(int pointer) => StartTrackingPointer(pointer);
        public void Stop(int pointer) => StopTrackingPointer(pointer);
        public void ResolveOne(int pointer, GestureDisposition disposition) => ResolvePointer(pointer, disposition);
        public override void AcceptGesture(int pointer) => Acceptances.Add(pointer);
        public override void RejectGesture(int pointer)
        {
            Rejections.Add(pointer);
            OnReject?.Invoke(pointer);
        }

        protected override void HandleEvent(PointerEvent @event)
        {
            Events.Add(@event);
            StopTrackingIfPointerNoLongerDown(@event);
        }

        protected override void DidStopTrackingLastPointer(int pointer) => Stops.Add(pointer);
    }

    private sealed class PrimaryRecognizer<T> : PrimaryPointerGestureRecognizer where T : PointerEvent
    {
        private readonly GestureDisposition _disposition;
        public PrimaryRecognizer(GestureDisposition disposition = GestureDisposition.Accepted,
            double? pre = -1.0, double? post = -1.0, TimeSpan? deadline = null)
            : base(deadline: deadline, preAcceptSlopTolerance: pre, postAcceptSlopTolerance: post)
        {
            _disposition = disposition;
        }

        public List<string> Resolutions { get; } = [];
        public List<PointerDownEvent> Deadlines { get; } = [];
        public override string DebugDescription => "primary";
        public override void AcceptGesture(int pointer)
        {
            base.AcceptGesture(pointer);
            Resolutions.Add("accepted");
        }

        public override void RejectGesture(int pointer)
        {
            base.RejectGesture(pointer);
            Resolutions.Add("rejected");
        }

        protected override void HandlePrimaryPointer(PointerEvent @event)
        {
            if (@event is T)
            {
                Resolve(_disposition);
            }
        }

        protected override void DidExceedDeadlineWithEvent(PointerDownEvent @event) => Deadlines.Add(@event);
    }
}
