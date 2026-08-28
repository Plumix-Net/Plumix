using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.UI;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/test/gestures/pointer_router_test.dart

namespace Plumix.Tests;

/// <summary>Ports the behaviors Flutter's own `pointer_router_test.dart` asserts.</summary>
public sealed class PointerRouterTests
{
    private static PointerDownEvent Down(int pointer = 1, Point position = default)
    {
        return new PointerDownEvent(
            pointer, PointerDeviceKind.Touch, position, PointerButtons.Primary, DateTime.UnixEpoch);
    }

    [Fact]
    public void Route_CallsRoutesInTheOrderTheyWereAdded()
    {
        var router = new PointerRouter();
        var log = new List<string>();
        router.AddRoute(1, _ => log.Add("first"));
        router.AddRoute(1, _ => log.Add("second"));
        router.AddGlobalRoute(_ => log.Add("global"));

        router.Route(Down());

        Assert.Equal(["first", "second", "global"], log);
    }

    [Fact]
    public void Route_IgnoresRoutesForOtherPointers()
    {
        var router = new PointerRouter();
        int calls = 0;
        router.AddRoute(2, _ => calls++);

        router.Route(Down(pointer: 1));

        Assert.Equal(0, calls);
    }

    [Fact]
    public void AddRoute_RejectsTheSameRouteTwiceForOnePointer()
    {
        var router = new PointerRouter();
        void Route(PointerEvent _)
        {
        }

        router.AddRoute(1, Route);

        Assert.Throws<InvalidOperationException>(() => router.AddRoute(1, Route));
    }

    [Fact]
    public void RemoveRoute_RequiresThatTheRouteWasAdded()
    {
        var router = new PointerRouter();

        Assert.Throws<InvalidOperationException>(() => router.RemoveRoute(1, _ => { }));
        Assert.Throws<InvalidOperationException>(() => router.RemoveGlobalRoute(_ => { }));
    }

    [Fact]
    public void Route_RemovedReentrantlyTakesEffectImmediately()
    {
        var router = new PointerRouter();
        var log = new List<string>();
        PointerRoute? second = null;
        void First(PointerEvent _)
        {
            log.Add("first");
            router.RemoveRoute(1, second!);
        }

        second = _ => log.Add("second");
        router.AddRoute(1, First);
        router.AddRoute(1, second);

        router.Route(Down());

        Assert.Equal(["first"], log);
    }

    [Fact]
    public void Route_AddedReentrantlyTakesEffectOnTheNextEvent()
    {
        var router = new PointerRouter();
        var log = new List<string>();
        bool added = false;
        router.AddRoute(1, _ =>
        {
            log.Add("first");
            if (added)
            {
                return;
            }

            added = true;
            router.AddRoute(1, _ => log.Add("late"));
        });

        router.Route(Down());
        Assert.Equal(["first"], log);

        router.Route(Down());
        Assert.Equal(["first", "first", "late"], log);
    }

    [Fact]
    public void Route_ReportsAThrowingRouteAndKeepsDispatching()
    {
        var router = new PointerRouter();
        var log = new List<string>();
        router.AddRoute(1, _ => throw new InvalidOperationException("boom"));
        router.AddRoute(1, _ => log.Add("second"));

        var reported = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            router.Route(Down());
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        Assert.Equal(["second"], log);
        FlutterErrorDetails details = Assert.Single(reported);
        Assert.Equal("gesture library", details.Library);
        Assert.IsType<InvalidOperationException>(details.Exception);
    }

    [Fact]
    public void Route_AppliesThePerRouteTransformToTheEvent()
    {
        var router = new PointerRouter();
        Point? localPosition = null;
        Matrix4 transform = Matrix4.Translation(new Vector3(-10.0, -20.0, 0.0));
        router.AddRoute(1, @event => localPosition = @event.LocalPosition, transform);

        router.Route(Down(position: new Point(30.0, 50.0)));

        Assert.NotNull(localPosition);
        Assert.Equal(20.0, localPosition!.Value.X, 6);
        Assert.Equal(30.0, localPosition!.Value.Y, 6);
    }

    [Fact]
    public void DebugGlobalRouteCount_TracksGlobalRoutes()
    {
        var router = new PointerRouter();
        void Route(PointerEvent _)
        {
        }

        Assert.Equal(0, router.DebugGlobalRouteCount);
        router.AddGlobalRoute(Route);
        Assert.Equal(1, router.DebugGlobalRouteCount);
        router.RemoveGlobalRoute(Route);
        Assert.Equal(0, router.DebugGlobalRouteCount);
    }
}

/// <summary>
/// Ports the behaviors Flutter's own `arena_test.dart` asserts against `GestureArenaManager`,
/// including the microtask Dart uses to resolve the last member standing.
/// </summary>
public sealed class GestureArenaTests : IDisposable
{
    private readonly GestureBinding _binding = GestureBinding.Instance;

    public GestureArenaTests()
    {
        _binding.ResetForTests();
    }

    public void Dispose()
    {
        _binding.ResetForTests();
    }

    [Fact]
    public void SoleMember_WinsOnlyOnceTheDeferredResolutionRuns()
    {
        var member = new RecordingMember();
        _binding.GestureArena.Add(1, member);
        _binding.GestureArena.Close(1);

        Assert.False(member.Accepted);

        _binding.GestureArena.FlushDefaultResolutions();
        Assert.True(member.Accepted);
    }

    [Fact]
    public void FirstMemberToAcceptWhileOpen_WinsAtClose()
    {
        var first = new RecordingMember();
        var second = new RecordingMember();
        GestureArenaEntry firstEntry = _binding.GestureArena.Add(1, first);
        _binding.GestureArena.Add(1, second);

        firstEntry.Resolve(GestureDisposition.Accepted);
        Assert.False(first.Accepted);

        _binding.GestureArena.Close(1);
        Assert.True(first.Accepted);
        Assert.True(second.Rejected);
    }

    [Fact]
    public void Sweep_GivesTheWinToTheFirstMember()
    {
        var first = new RecordingMember();
        var second = new RecordingMember();
        _binding.GestureArena.Add(1, first);
        _binding.GestureArena.Add(1, second);
        _binding.GestureArena.Close(1);

        _binding.GestureArena.Sweep(1);

        Assert.True(first.Accepted);
        Assert.True(second.Rejected);
    }

    [Fact]
    public void HeldArena_DelaysTheSweepUntilRelease()
    {
        var first = new RecordingMember();
        var second = new RecordingMember();
        _binding.GestureArena.Add(1, first);
        _binding.GestureArena.Add(1, second);
        _binding.GestureArena.Close(1);
        _binding.GestureArena.Hold(1);

        _binding.GestureArena.Sweep(1);
        Assert.False(first.Accepted);

        _binding.GestureArena.Release(1);
        Assert.True(first.Accepted);
        Assert.True(second.Rejected);
    }

    [Fact]
    public void Add_OnAClosedArenaIsRejected()
    {
        _binding.GestureArena.Add(1, new RecordingMember());
        _binding.GestureArena.Close(1);

        Assert.Throws<InvalidOperationException>(
            () => _binding.GestureArena.Add(1, new RecordingMember()));
    }

    private sealed class RecordingMember : IGestureArenaMember
    {
        public bool Accepted { get; private set; }

        public bool Rejected { get; private set; }

        public void AcceptGesture(int pointer) => Accepted = true;

        public void RejectGesture(int pointer) => Rejected = true;
    }
}
