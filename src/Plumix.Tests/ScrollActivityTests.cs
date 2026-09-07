using Avalonia;
using Plumix.Gestures;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Covers the scroll activity layer and the notification contract it owns
/// (<c>widgets/scroll_activity.dart</c>, <c>widgets/scroll_position.dart</c>,
/// <c>widgets/scroll_position_with_single_context.dart</c>).
/// </summary>
public class ScrollActivityTests
{
    [Fact]
    public void Drag_DispatchesStartUserScrollUpdateEndAndIdle_LikeFlutter()
    {
        using var recorder = new ScrollNotificationRecorder();
        using ScrollPositionWithSingleContext position = recorder.CreatePosition(new ClampingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);
        recorder.Notifications.Clear();

        IDrag drag = position.StartDrag();
        drag.DragBy(-20.0);
        drag.EndDrag();

        // scroll_notification_test.dart: a drag reports start, direction, update, end, direction.
        Assert.Equal(
            [
                nameof(ScrollStartNotification),
                nameof(UserScrollNotification),
                nameof(ScrollUpdateNotification),
                nameof(ScrollEndNotification),
                nameof(UserScrollNotification),
            ],
            recorder.Notifications.Select(n => n.GetType().Name));
        Assert.Equal(ScrollDirection.Reverse, ((UserScrollNotification)recorder.Notifications[1]).Direction);
        Assert.Equal(ScrollDirection.Idle, ((UserScrollNotification)recorder.Notifications[4]).Direction);
    }

    [Fact]
    public void DragScrollActivity_CarriesTheDragDetailsOnEveryNotification()
    {
        using var recorder = new ScrollNotificationRecorder();
        using ScrollPositionWithSingleContext position = recorder.CreatePosition(new ClampingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);
        recorder.Notifications.Clear();

        IDrag drag = position.StartDrag();
        drag.DragBy(-20.0);
        drag.EndDrag();

        ScrollStartNotification start = recorder.Notifications.OfType<ScrollStartNotification>().Single();
        ScrollUpdateNotification update = recorder.Notifications.OfType<ScrollUpdateNotification>().Single();
        ScrollEndNotification end = recorder.Notifications.OfType<ScrollEndNotification>().Single();
        Assert.True(start.HasDragDetails);
        Assert.True(update.HasDragDetails);
        Assert.Equal(-20.0, update.DragDetails!.Value.PrimaryDelta);
        Assert.Equal(20.0, update.ScrollDelta);
        // scroll_notification_test.dart: a static release reports Velocity.zero.
        Assert.Equal(Velocity.Zero, end.DragDetails!.Value.Velocity);
    }

    [Fact]
    public void Overscroll_IsReportedByTheActivity_WithTheDragDetailsAndTheBallisticVelocity()
    {
        using var recorder = new ScrollNotificationRecorder();
        using ScrollPositionWithSingleContext position = recorder.CreatePosition(new ClampingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);
        recorder.Notifications.Clear();

        IDrag drag = position.StartDrag();
        drag.DragBy(40.0);

        OverscrollNotification overscroll = recorder.Notifications.OfType<OverscrollNotification>().Single();
        Assert.Equal(-40.0, overscroll.Overscroll);
        // A drag reports its details and no velocity; a ballistic run reports its velocity instead.
        Assert.True(overscroll.HasDragDetails);
        Assert.Equal(0.0, overscroll.Velocity);
        Assert.Equal(0.0, position.Pixels);
    }

    [Fact]
    public void JumpTo_RunsAStartUpdateEndCycleAndNotifiesListenersOnce()
    {
        using var recorder = new ScrollNotificationRecorder();
        using ScrollPositionWithSingleContext position = recorder.CreatePosition(new ClampingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);
        recorder.Notifications.Clear();
        int notified = 0;
        position.AddListener(() => notified += 1);

        position.JumpTo(100.0);

        // scroll_position_test.dart: "ScrollPosition jumpTo() doesn't call notifyListeners twice".
        Assert.Equal(1, notified);
        Assert.Equal(
            [
                nameof(ScrollStartNotification),
                nameof(ScrollUpdateNotification),
                nameof(ScrollEndNotification),
            ],
            recorder.Notifications.Select(n => n.GetType().Name));
        Assert.Equal(100.0, position.Pixels);
    }

    [Fact]
    public void JumpToWithoutSettling_SkipsTheBallisticSettleButKeepsTheNotifications()
    {
        using var recorder = new ScrollNotificationRecorder();
        using ScrollPositionWithSingleContext position = recorder.CreatePosition(new BouncingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);
        recorder.Notifications.Clear();

#pragma warning disable CS0618 // Deprecated in Dart too; the port carries the deprecation.
        position.JumpToWithoutSettling(-50.0);
#pragma warning restore CS0618

        Assert.Equal(-50.0, position.Pixels);
        Assert.True(position.OutOfRange);
        // No ballistic activity was started, so the out-of-range offset stays where it was put.
        Assert.IsType<IdleScrollActivity>(position.Activity);
        Assert.Equal(3, recorder.Notifications.Count);
    }

    [Fact]
    public void ScrollMetricsNotification_FiresOncePerRealDimensionChange()
    {
        Scheduler.ResetForTests();
        try
        {
            using var recorder = new ScrollNotificationRecorder();
            using ScrollPositionWithSingleContext position = recorder.CreatePosition(new ClampingScrollPhysics());
            position.ApplyViewportDimension(600);
            position.ApplyContentDimensions(0, 600);
            Scheduler.FlushMicrotasks();
            Assert.Single(recorder.Notifications.OfType<ScrollMetricsNotification>());

            // The same dimensions again change nothing, so nothing is dispatched.
            recorder.Notifications.Clear();
            position.ApplyViewportDimension(600);
            position.ApplyContentDimensions(0, 600);
            Scheduler.FlushMicrotasks();
            Assert.Empty(recorder.Notifications);

            // Shrinking the content is a real change.
            position.ApplyContentDimensions(0, 400);
            Scheduler.FlushMicrotasks();
            ScrollMetricsNotification metrics =
                Assert.Single(recorder.Notifications.OfType<ScrollMetricsNotification>());
            Assert.Equal(400.0, metrics.Metrics.ExtentAfter);
            Assert.Equal(1000.0, metrics.Metrics.ExtentTotal);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void BeginActivity_PairsStartAndEndAroundScrollingActivitiesOnly()
    {
        using var recorder = new ScrollNotificationRecorder();
        using ScrollPositionWithSingleContext position = recorder.CreatePosition(new ClampingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);
        recorder.Notifications.Clear();

        // Idle -> hold is not scrolling either way: no start, no end.
        IScrollHoldController hold = position.Hold();
        Assert.Empty(recorder.Notifications);

        // Hold -> drag starts a scroll.
        IDrag drag = position.StartDrag();
        Assert.Single(recorder.Notifications.OfType<ScrollStartNotification>());
        Assert.Empty(recorder.Notifications.OfType<ScrollEndNotification>());

        // Drag -> idle ends it exactly once.
        drag.EndDrag();
        Assert.Single(recorder.Notifications.OfType<ScrollStartNotification>());
        Assert.Single(recorder.Notifications.OfType<ScrollEndNotification>());
        hold.Cancel();
    }

    [Fact]
    public void ActivityContract_MatchesFlutterPerActivity()
    {
        using var context = new ScrollNotificationRecorder();
        using ScrollPositionWithSingleContext position = context.CreatePosition(new ClampingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);

        var idle = new IdleScrollActivity(position);
        Assert.False(idle.ShouldIgnorePointer);
        Assert.False(idle.IsScrolling);
        Assert.Equal(0.0, idle.Velocity);

        bool canceled = false;
        var held = new HoldScrollActivity(position, () => canceled = true);
        Assert.False(held.ShouldIgnorePointer);
        Assert.False(held.IsScrolling);
        Assert.Equal(0.0, held.Velocity);
        held.Dispose();
        Assert.True(canceled);
    }

    [Fact]
    public void DragScrollActivity_DoesNotIgnorePointersForATrackpadDrag()
    {
        using var recorder = new ScrollNotificationRecorder();
        using ScrollPositionWithSingleContext position = recorder.CreatePosition(new ClampingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);

        position.Drag(new DragStartDetails(default, Kind: PointerDeviceKind.Touch));
        Assert.True(position.Activity.ShouldIgnorePointer);

        position.Drag(new DragStartDetails(default, Kind: PointerDeviceKind.Trackpad));
        Assert.False(position.Activity.ShouldIgnorePointer);
    }

    [Fact]
    public void DrivenScrollActivity_LetsASubclassOverrideApplyMoveTo()
    {
        Scheduler.ResetForTests();
        try
        {
            using var recorder = new ScrollNotificationRecorder();
            using ScrollPositionWithSingleContext position = recorder.CreatePosition(new ClampingScrollPhysics());
            position.ApplyViewportDimension(100);
            position.ApplyContentDimensions(0, 100);
            recorder.Notifications.Clear();

            // scroll_activity_test.dart: the base activity overscrolls past maxScrollExtent, and a
            // subclass that clamps in applyMoveTo does not.
            var clamping = new ClampingDrivenScrollActivity(
                position,
                from: 0.0,
                to: 400.0,
                duration: TimeSpan.FromMilliseconds(100),
                curve: Curves.Linear,
                vsync: recorder.Vsync);
            position.BeginActivity(clamping);
            PumpSeconds(0.2);

            Assert.Empty(recorder.Notifications.OfType<OverscrollNotification>());
            Assert.Equal(100.0, position.Pixels);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void DrivenScrollActivity_FromSimulation_StartsAtZeroAndFollowsTheSimulation()
    {
        Scheduler.ResetForTests();
        try
        {
            using var recorder = new ScrollNotificationRecorder();
            using ScrollPositionWithSingleContext position = recorder.CreatePosition(new BouncingScrollPhysics());
            position.ApplyViewportDimension(100);
            position.ApplyContentDimensions(0, 10000);

            DrivenScrollActivity activity = DrivenScrollActivity.FromSimulation(
                position,
                new GravitySimulation(9.8),
                recorder.Vsync);
            position.BeginActivity(activity);
            // scroll_activity_test.dart: the .simulation constructor starts the controller at 0.0.
            Assert.Equal(0.0, position.Pixels);

            PumpSeconds(1.0);
            Assert.Equal(9.8 / 2.0, position.Pixels, tolerance: 0.5);

            PumpSeconds(1.0);
            Assert.Equal(9.8 * 2.0, position.Pixels, tolerance: 1.0);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void AnimateTo_CompletesItsTaskWhenTheActivityIsReplaced()
    {
        Scheduler.ResetForTests();
        try
        {
            using var recorder = new ScrollNotificationRecorder();
            using ScrollPositionWithSingleContext position = recorder.CreatePosition(new ClampingScrollPhysics());
            position.ApplyViewportDimension(100);
            position.ApplyContentDimensions(0, 1000);

            Task animation = position.AnimateTo(500.0, TimeSpan.FromSeconds(1), Curves.Linear);
            Assert.IsType<DrivenScrollActivity>(position.Activity);
            Assert.False(animation.IsCompleted);

            // A jump replaces the driven activity, which completes the animation's task.
            position.JumpTo(20.0);
            Assert.True(animation.IsCompleted);
            Assert.Equal(20.0, position.Pixels);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void AnimateTo_JumpsWhenTheTargetIsAlreadyWithinTheTolerance()
    {
        using var recorder = new ScrollNotificationRecorder();
        using ScrollPositionWithSingleContext position = recorder.CreatePosition(new ClampingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);

        Task animation = position.AnimateTo(1e-9, TimeSpan.FromSeconds(1), Curves.Linear);

        Assert.True(animation.IsCompleted);
        Assert.IsType<IdleScrollActivity>(position.Activity);
    }

    [Fact]
    public void Absorb_MovesTheActivityAndTheDragOntoTheNewPosition()
    {
        Scheduler.ResetForTests();
        try
        {
            using var recorder = new ScrollNotificationRecorder();
            ScrollPositionWithSingleContext oldPosition = recorder.CreatePosition(new ClampingScrollPhysics());
            oldPosition.ApplyViewportDimension(100);
            oldPosition.ApplyContentDimensions(0, 1000);
            ScrollDragController drag = oldPosition.Drag(new DragStartDetails(default));
            ScrollActivity dragActivity = oldPosition.Activity;

            using ScrollPositionWithSingleContext position =
                recorder.CreatePosition(new ClampingScrollPhysics(), oldPosition: oldPosition);

            Assert.Same(dragActivity, position.Activity);
            Assert.Same(position, dragActivity.Delegate);
            Assert.Same(position, drag.Delegate);
            Assert.Null(oldPosition.Activity);
            oldPosition.Dispose();

            // The moved drag now scrolls the new position.
            drag.DragBy(-15.0);
            Assert.Equal(15.0, position.Pixels);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void PointerScroll_ReportsAWholeScrollCycleAndSettlesIdle()
    {
        using var recorder = new ScrollNotificationRecorder();
        using ScrollPositionWithSingleContext position = recorder.CreatePosition(new ClampingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);
        recorder.Notifications.Clear();

        position.PointerScroll(120.0);

        Assert.Equal(120.0, position.Pixels);
        Assert.Equal(
            [
                nameof(UserScrollNotification),
                nameof(ScrollStartNotification),
                nameof(ScrollUpdateNotification),
                nameof(ScrollEndNotification),
                nameof(UserScrollNotification),
            ],
            recorder.Notifications.Select(n => n.GetType().Name));
        Assert.IsType<IdleScrollActivity>(position.Activity);
    }

    private double _clock;

    /// <summary>Runs frames on a monotonic clock of its own, so repeated calls keep advancing.</summary>
    private void PumpSeconds(double seconds)
    {
        const double frame = 1.0 / 60.0;
        double target = _clock + seconds;
        while (_clock + frame <= target + 1e-9)
        {
            _clock += frame;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(_clock));
        }
    }

    /// <summary>A driven activity that refuses to leave the scroll extents, as Flutter's test does.</summary>
    private sealed class ClampingDrivenScrollActivity(
        ScrollPositionWithSingleContext position,
        double from,
        double to,
        TimeSpan duration,
        Curve curve,
        ITickerProvider vsync) : DrivenScrollActivity(position, from, to, duration, curve, vsync)
    {
        private readonly ScrollPositionWithSingleContext _position = position;

        protected override bool ApplyMoveTo(double value)
        {
            return base.ApplyMoveTo(
                Math.Clamp(value, _position.MinScrollExtent, _position.MaxScrollExtent));
        }
    }

    /// <summary>
    /// Flutter's <c>GravitySimulation</c>, which <c>physics/</c> has no counterpart for yet; only
    /// the constant-acceleration case the scroll activity test needs is modelled here.
    /// </summary>
    private sealed class GravitySimulation(double acceleration) : Simulation
    {
        public override double X(double time) => 0.5 * acceleration * time * time;

        public override double DX(double time) => acceleration * time;

        public override bool IsDone(double time) => false;
    }
}
