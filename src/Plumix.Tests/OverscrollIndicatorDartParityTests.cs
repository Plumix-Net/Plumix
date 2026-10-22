using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/widgets/overscroll_indicator_test.dart

namespace Plumix.Tests;

public sealed partial class OverscrollIndicatorTests
{
    [DebugOnlyFact]
    public void Glow_UpstreamSlowDragPaintsTheExactWhiteAlphaAndSettles()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new Directionality(TextDirection.Ltr,
            new ScrollConfiguration(new FixedPlatformScrollBehavior(TargetPlatform.Android),
                new CustomScrollView(slivers:
                [
                    new SliverToBoxAdapter(new SizedBox(height: 2000.0)),
                ]))));
        Assert.DoesNotContain(tester.RecordPaint(Find.ByType<CustomPaint>()), call => call.Method == "drawCircle");
        tester.Drag(tester.ElementOfType<Scrollable>(), new Vector(0.0, 100.0));
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.DoesNotContain(tester.RecordPaint(Find.ByType<CustomPaint>()), call => call.Method == "drawCircle");
        TestGesture stationary = tester.StartGesture(new Point(200.0, 200.0), PointerDeviceKind.Touch);
        tester.Pump(TimeSpan.FromMilliseconds(100));
        stationary.Up();
        Assert.DoesNotContain(tester.RecordPaint(Find.ByType<CustomPaint>()), call => call.Method == "drawCircle");

        SlowGlowDrag(tester, new Point(200.0, 200.0), new Vector(0.0, 5.0));
        CanvasCall circle = Assert.Single(tester.RecordPaint(Find.ByType<CustomPaint>()),
            call => call.Method == "drawCircle");
        Assert.Equal(new Color(0x0DFFFFFF), circle.Color);
        tester.PumpAndSettle(TimeSpan.FromSeconds(1));
        Assert.DoesNotContain(tester.RecordPaint(Find.ByType<CustomPaint>()), call => call.Method == "drawCircle");
    }

    [DebugOnlyFact]
    public void Glow_UpstreamNestedScrollableReachesBothEligiblePainters()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new Directionality(TextDirection.Ltr,
            new ScrollConfiguration(new FixedPlatformScrollBehavior(TargetPlatform.Android),
                new GlowingOverscrollIndicator(AxisDirection.Down, new Color(0x0DFFFFFF),
                    notificationPredicate: notification => notification.Depth == 1,
                    child: new SingleChildScrollView(scrollDirection: Axis.Horizontal,
                        child: new SizedBox(width: 600.0,
                            child: new CustomScrollView(slivers:
                            [
                                new SliverToBoxAdapter(new SizedBox(height: 2000.0)),
                            ])))))));
        SlowGlowDrag(tester, new Point(200.0, 200.0), new Vector(0.0, 5.0));
        Element[] painters = tester.ElementsOfType<CustomPaint>().ToArray();
        Assert.Equal(3, painters.Length);
        Assert.Contains(PaintRecording.Record(painters[0].FindRenderObject()!), call => call.Method == "drawCircle");
        Assert.Contains(PaintRecording.Record(painters[^1].FindRenderObject()!), call => call.Method == "drawCircle");
        tester.PumpAndSettle(TimeSpan.FromSeconds(1));
    }

    private static void SlowGlowDrag(FrameworkDartTester tester, Point start, Vector offset)
    {
        TestGesture gesture = tester.StartGesture(start, PointerDeviceKind.Touch);
        for (int index = 0; index < 10; index++)
        {
            gesture.MoveBy(offset);
            tester.Pump(TimeSpan.FromMilliseconds(20));
        }

        gesture.Up();
    }

    [Fact]
    public void Glow_SampleSupportsMouseDragsThroughItsFrameworkScrollable()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(Wrap(new ScrollPhysicsDemoPage()));
        var element = (StatefulElement)tester.ElementsOfType<GlowingOverscrollIndicator>().Single(
            candidate => ((GlowingOverscrollIndicator)candidate.Widget).Color == new Color(0xFF1565C0));
        var state = (GlowingOverscrollIndicatorState)element.State;
        Point start = tester.GetCenter(element);
        TestGesture gesture = tester.StartGesture(start, PointerDeviceKind.Mouse);
        for (int index = 0; index < 10; index++)
        {
            gesture.MoveBy(new Vector(0.0, 5.0));
            tester.Pump(TimeSpan.FromMilliseconds(20));
        }

        Assert.True(state.LeadingController.GlowOpacity > 0.0);
        gesture.Up();
        tester.PumpAndSettle();
        Assert.Equal(0.0, state.LeadingController.GlowOpacity);
    }

    [Fact]
    public void Glow_RepeatedPullMutatesTweensWithoutRestartingAnimation()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new GlowController(Colors.White, Axis.Vertical);
        controller.Pull(20.0, 200.0, 100.0, 200.0);
        AnimationPump.Advance(0.08);
        double curve = Curves.Decelerate.Transform(80.0 / 167.0);
        double firstOpacity = 0.08 * curve;
        Assert.Equal(firstOpacity, controller.GlowOpacity, 8);

        controller.Pull(10.0, 200.0, 100.0, 200.0);
        double end = firstOpacity + (0.08 - firstOpacity) * curve + 0.04;
        Assert.Equal(firstOpacity + (end - firstOpacity) * curve, controller.GlowOpacity, 8);
        AnimationPump.Advance(0.087001);
        Assert.Equal(end, controller.GlowOpacity, 8);

        int changes = 0;
        controller.AddListener(() => changes++);
        controller.Pull(10.0, 200.0, 100.0, 200.0);
        Assert.Equal(end + 0.04, controller.GlowOpacity, 8);
        Assert.Equal(1, changes);
        AnimationPump.Advance(0.05);
        Assert.Equal(1, changes);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(100.0)]
    [InlineData(101.0)]
    public void Glow_WeakAbsorbStartsAtPointThreeAndRoundsDuration(double velocity)
    {
        using var controller = new GlowController(Colors.White, Axis.Vertical);
        controller.AbsorbImpact(velocity);
        Assert.Equal(0.3, controller.GlowOpacity, 8);
        Assert.Equal(GlowState.Absorb, controller.State);
        AnimationPump.Advance(0.001);
        Assert.Equal(0.3, controller.GlowOpacity, 8);
        AnimationPump.Advance(0.001001);
        Assert.Equal(GlowState.Recede, controller.State);
        AnimationPump.Advance(0.600001);
        Assert.Equal(GlowState.Idle, controller.State);
        Assert.Equal(0.0, controller.GlowOpacity);
        Assert.Equal(0.0, controller.GlowSize);
    }

    [Fact]
    public void Glow_StrongAbsorbClampsVelocityAndRecedesAfterTwoHundredMilliseconds()
    {
        using var controller = new GlowController(Colors.White, Axis.Vertical);
        controller.AbsorbImpact(20000.0);
        AnimationPump.Advance(0.1);
        Assert.Equal(0.45, controller.GlowOpacity, 8);
        Assert.Equal(0.75, controller.GlowSize, 8);
        AnimationPump.Advance(0.1);
        Assert.Equal(0.5, controller.GlowOpacity, 8);
        Assert.Equal(1.0, controller.GlowSize, 8);
        Assert.Equal(GlowState.Absorb, controller.State);
        AnimationPump.Advance(0.000001);
        Assert.Equal(GlowState.Recede, controller.State);
        AnimationPump.Advance(0.3);
        Assert.Equal(0.125, controller.GlowOpacity, 8);
        AnimationPump.Advance(0.300001);
        Assert.Equal(GlowState.Idle, controller.State);
    }

    [Fact]
    public void Glow_PullHoldResetsItsTimerAndUsesTwoSecondDecay()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new GlowController(Colors.White, Axis.Vertical);
        controller.Pull(30.0, 200.0, 100.0, 200.0);
        AnimationPump.Advance(0.1);
        timers.Elapse(TimeSpan.FromMilliseconds(100));
        controller.Pull(10.0, 200.0, 100.0, 200.0);
        AnimationPump.Advance(0.067);
        timers.Elapse(TimeSpan.FromMilliseconds(166));
        Assert.Equal(GlowState.Pull, controller.State);
        double peak = controller.GlowOpacity;
        timers.Elapse(TimeSpan.FromMilliseconds(1));
        Assert.Equal(GlowState.Recede, controller.State);
        AnimationPump.Advance(1.0);
        Assert.Equal(peak * 0.25, controller.GlowOpacity, 8);
        AnimationPump.Advance(1.000001);
        Assert.Equal(GlowState.Idle, controller.State);
        controller.Pull(30.0, 200.0, 100.0, 200.0);
        AnimationPump.Advance(0.167);
        Assert.Equal(0.12, controller.GlowOpacity, 8);
    }

    [Fact]
    public void Glow_ScrollEndUsesSixHundredMillisecondsAndCancelsPullTimer()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new GlowController(Colors.White, Axis.Vertical);
        controller.Pull(30.0, 200.0, 100.0, 200.0);
        AnimationPump.Advance(0.167);
        controller.ScrollEnd();
        double peak = controller.GlowOpacity;
        timers.Elapse(TimeSpan.FromSeconds(1));
        AnimationPump.Advance(0.3);
        Assert.Equal(peak * 0.25, controller.GlowOpacity, 8);
        controller.ScrollEnd();
        AnimationPump.Advance(0.300001);
        Assert.Equal(GlowState.Idle, controller.State);
    }

    [Fact]
    public void Glow_DisplacementTicksIndependentlyAndStopsWithoutSnapping()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new GlowController(Colors.White, Axis.Vertical);
        controller.Pull(30.0, 200.0, 100.0, 200.0);
        AnimationPump.Advance(0.167);
        controller.Pull(10.0, 200.0, 0.0, 200.0);
        AnimationPump.Prime();
        Assert.Equal(0.5, controller.Displacement);
        AnimationPump.Advance(0.016667);
        Assert.Equal(0.25, controller.Displacement, 8);
        AnimationPump.Advance(0.15);
        Assert.InRange(controller.Displacement, 0.0, 0.001);
        Assert.NotEqual(0.0, controller.Displacement);
        double stopped = controller.Displacement;
        AnimationPump.Advance(0.1);
        Assert.Equal(stopped, controller.Displacement);
    }

    [Fact]
    public void Glow_AbsorbPreservesDisplacementTargetAndTicker()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new GlowController(Colors.White, Axis.Vertical);
        controller.Pull(30.0, 200.0, 0.0, 200.0);
        AnimationPump.Advance(0.016667);
        controller.AbsorbImpact(100.0);
        Assert.Equal(0.5, controller.Displacement);
        AnimationPump.Advance(0.016667);
        Assert.Equal(0.25, controller.Displacement, 8);
        timers.Elapse(TimeSpan.FromMilliseconds(200));
        Assert.Equal(GlowState.Recede, controller.State);
    }

    [DebugOnlyTheory]
    [InlineData(0.0, 50.0)]
    [InlineData(30.0, 20.0)]
    [InlineData(50.0, 0.0)]
    [InlineData(60.0, 0.0)]
    public void Glow_PaintOffsetStaysInViewportPixels(double scrolled, double expectedOffset)
    {
        using var controller = new GlowController(new Color(0x00FFFFFF), Axis.Vertical);
        controller.AbsorbImpact(1000.0);
        AnimationPump.Advance(0.01);
        controller.PaintOffset = 50.0;
        controller.PaintOffsetScrollPixels = -Math.Min(scrolled, 50.0);
        var context = new PaintingContext(new ContainerLayer());
        controller.Paint(context, new Size(800.0, 600.0));
        IReadOnlyList<CanvasCall> calls = context.Canvas.DebugCalls;
        Assert.Equal(new[] { "save", "translate", "scale", "clipRect", "drawCircle", "restore" },
            calls.Select(call => call.Method));
        Assert.Equal(expectedOffset, calls[1].Dy);
        Assert.Equal(800.0 * 1.5, calls[4].Radius);
        Assert.Equal(400.0, calls[4].Center!.Value.X);
        Assert.Equal(255, calls[4].Color!.Red);
        Assert.Equal(Colors.White.WithOpacity(controller.GlowOpacity).Alpha, calls[4].Color!.Alpha);
        context.DebugStopRecordingIfNeeded();
    }

    [Fact]
    public void Glow_UsesGlobalDragPositionInTheNotificationRenderBox()
    {
        using var timers = new FakeGestureTimers();
        var key = new LabeledGlobalKey<NotificationEmitterState>("translated-glow");
        using var harness = new WidgetRenderHarness(Wrap(new Padding(
            new EdgeInsets(20.0, 30.0, 0.0, 0.0),
            new GlowingOverscrollIndicator(AxisDirection.Down, Colors.White,
                child: new NotificationEmitter(key)))));
        harness.Pump(Viewport);
        NotificationEmitterState emitter = key.CurrentState!;
        var box = (RenderBox)emitter.Context.FindRenderObject()!;
        Point global = box.LocalToGlobal(new Point(box.Size.Width * 0.25, 0.0));
        var details = new DragUpdateDetails(global, new Point(10000.0, 10000.0), default, 20.0);
        emitter.Dispatch(new OverscrollNotification(Metrics(), -20.0, dragDetails: details));
        AnimationPump.Advance(0.016667);
        Assert.Equal(0.375, harness.FindState<GlowingOverscrollIndicatorState>().LeadingController.Displacement, 8);
    }

    [DebugOnlyTheory]
    [InlineData(AxisDirection.Down)]
    [InlineData(AxisDirection.Up)]
    [InlineData(AxisDirection.Left)]
    [InlineData(AxisDirection.Right)]
    public void Glow_BothSidesPaintAndSettleForEveryAxisDirection(AxisDirection direction)
    {
        using var timers = new FakeGestureTimers();
        var key = new LabeledGlobalKey<NotificationEmitterState>("both-edges");
        using var harness = new WidgetRenderHarness(Wrap(new GlowingOverscrollIndicator(
            direction, Colors.White, new NotificationEmitter(key))));
        harness.Pump(Viewport);
        var details = new DragUpdateDetails(new Point(120.0, 100.0), default, default, 20.0);
        key.CurrentState!.Dispatch(new OverscrollNotification(Metrics(axisDirection: direction),
            -30.0, dragDetails: details));
        key.CurrentState.Dispatch(new OverscrollNotification(Metrics(axisDirection: direction),
            30.0, dragDetails: details));
        AnimationPump.Advance(0.1);
        GlowingOverscrollIndicatorState state = harness.FindState<GlowingOverscrollIndicatorState>();
        var painter = Assert.IsType<GlowingOverscrollIndicatorPainter>(
            harness.FindRenderObject<RenderCustomPaint>().ForegroundPainter);
        var context = new PaintingContext(new ContainerLayer());
        painter.Paint(context, Viewport);
        Assert.Equal(2, context.Canvas.DebugCalls.Count(call => call.Method == "drawCircle"));
        context.DebugStopRecordingIfNeeded();
        state.LeadingController.ScrollEnd();
        state.TrailingController.ScrollEnd();
        AnimationPump.Advance(0.6);
        Assert.Equal(0.0, state.LeadingController.GlowOpacity);
        Assert.Equal(0.0, state.TrailingController.GlowOpacity);
    }

    [Fact]
    public void Glow_ZeroAreaAndNullChildDoNotCrash()
    {
        using var harness = new WidgetRenderHarness(Wrap(new GlowingOverscrollIndicator(
            AxisDirection.Down, Colors.White)));
        harness.Pump(new Size(0.0, 0.0));
        Assert.Equal(new Size(0.0, 0.0), harness.FindRenderObject<RenderCustomPaint>().Size);
    }

    [Fact]
    public void Glow_DisposeCancelsBothTickersAndTheHoldTimer()
    {
        using var timers = new FakeGestureTimers();
        var controller = new GlowController(Colors.White, Axis.Vertical);
        controller.Pull(30.0, 200.0, 0.0, 200.0);
        AnimationPump.Advance(0.01);
        controller.Dispose();
        timers.Elapse(TimeSpan.FromSeconds(3));
        AnimationPump.Advance(3.0);
        Assert.Equal(0, SchedulerBinding.Instance.TransientCallbackCount);
    }

    [Fact]
    public void Glow_WidgetUpdatesPreserveControllersAndApplyColorAxisAndShowFlags()
    {
        using var timers = new FakeGestureTimers();
        using var harness = new WidgetRenderHarness(Wrap(new GlowingOverscrollIndicator(
            AxisDirection.Down, Colors.White, new SizedBox())));
        harness.Pump(Viewport);
        GlowingOverscrollIndicatorState state = harness.FindState<GlowingOverscrollIndicatorState>();
        GlowController controller = state.LeadingController;
        controller.Pull(30.0, 200.0, 100.0, 200.0);
        AnimationPump.Advance(0.1);
        double opacity = controller.GlowOpacity;

        harness.Update(Wrap(new GlowingOverscrollIndicator(
            AxisDirection.Left, Colors.Green, new SizedBox(), showTrailing: false)));
        Assert.Same(state, harness.FindState<GlowingOverscrollIndicatorState>());
        Assert.Same(controller, state.LeadingController);
        Assert.Equal(opacity, controller.GlowOpacity);
        Assert.Equal(Colors.Green, controller.Color);
        Assert.Equal(Colors.Green, state.TrailingController.Color);
        Assert.Equal(Axis.Horizontal, controller.Axis);
        var painter = Assert.IsType<GlowingOverscrollIndicatorPainter>(
            harness.FindRenderObject<RenderCustomPaint>().ForegroundPainter);
        Assert.Same(controller, painter.LeadingController);
        Assert.Null(painter.TrailingController);
        Assert.Equal(AxisDirection.Left, painter.AxisDirection);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    public void Glow_PredicateControlsNestedNotificationDepth(int depth, int expectedDepth)
    {
        using var timers = new FakeGestureTimers();
        var key = new LabeledGlobalKey<NotificationEmitterState>("depth-glow");
        using var harness = new WidgetRenderHarness(Wrap(new GlowingOverscrollIndicator(
            AxisDirection.Down, Colors.White, new NotificationEmitter(key),
            notificationPredicate: notification => notification.Depth == expectedDepth)));
        harness.Pump(Viewport);
        var details = new DragUpdateDetails(new Point(100.0, 100.0), default, default, 20.0);
        key.CurrentState!.Dispatch(new OverscrollNotification(Metrics(), -30.0,
            depth: depth + 1, dragDetails: details));
        Assert.Equal(GlowState.Idle, harness.FindState<GlowingOverscrollIndicatorState>().LeadingController.State);
        key.CurrentState.Dispatch(new OverscrollNotification(Metrics(), -30.0,
            depth: depth, dragDetails: details));
        Assert.Equal(GlowState.Pull, harness.FindState<GlowingOverscrollIndicatorState>().LeadingController.State);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Glow_PaintOffsetCorrectionTracksTheSelectedScrollEdge(bool leading)
    {
        using var timers = new FakeGestureTimers();
        var key = new LabeledGlobalKey<NotificationEmitterState>("offset-glow");
        using var harness = new WidgetRenderHarness(Wrap(new NotificationListener<OverscrollIndicatorNotification>(
            onNotification: notification => { notification.PaintOffset = 50.0; return false; },
            child: new GlowingOverscrollIndicator(AxisDirection.Down, Colors.White, new NotificationEmitter(key)))));
        harness.Pump(Viewport);
        var details = new DragUpdateDetails(new Point(120.0, 100.0), default, default, 20.0);
        key.CurrentState!.Dispatch(new OverscrollNotification(
            OffsetMetrics(leading ? -100.0 : 600.0), leading ? -30.0 : 30.0, dragDetails: details));
        GlowingOverscrollIndicatorState state = harness.FindState<GlowingOverscrollIndicatorState>();
        GlowController controller = leading ? state.LeadingController : state.TrailingController;
        Assert.Equal(50.0, controller.PaintOffset);
        foreach (double scroll in new[] { 0.0, 30.0, 50.0, 60.0 })
        {
            double pixels = leading ? -100.0 + scroll : 600.0 - scroll;
            key.CurrentState.Dispatch(new ScrollUpdateNotification(OffsetMetrics(pixels), scrollDelta: 0.0));
            Assert.Equal(Math.Max(0.0, 50.0 - scroll),
                controller.PaintOffset + controller.PaintOffsetScrollPixels);
        }
    }

    [Fact]
    public void Glow_ConfirmationCanVetoAndThenAcceptTheNextEventOnEitherSide()
    {
        using var timers = new FakeGestureTimers();
        var key = new LabeledGlobalKey<NotificationEmitterState>("veto-glow");
        var sides = new List<bool>();
        using var harness = new WidgetRenderHarness(Wrap(new NotificationListener<OverscrollIndicatorNotification>(
            onNotification: notification =>
            {
                sides.Add(notification.Leading);
                if (sides.Count == 1)
                {
                    notification.DisallowIndicator();
                }

                return false;
            },
            child: new GlowingOverscrollIndicator(AxisDirection.Down, Colors.White, new NotificationEmitter(key)))));
        harness.Pump(Viewport);
        var details = new DragUpdateDetails(new Point(120.0, 100.0), default, default, 20.0);
        GlowingOverscrollIndicatorState state = harness.FindState<GlowingOverscrollIndicatorState>();
        key.CurrentState!.Dispatch(new OverscrollNotification(Metrics(), -20.0, dragDetails: details));
        Assert.Equal(GlowState.Idle, state.LeadingController.State);
        key.CurrentState.Dispatch(new OverscrollNotification(Metrics(), -20.0, dragDetails: details));
        Assert.Equal(GlowState.Pull, state.LeadingController.State);
        key.CurrentState.Dispatch(new OverscrollNotification(Metrics(), 20.0, dragDetails: details));
        Assert.Equal(GlowState.Pull, state.TrailingController.State);
        Assert.Equal(new[] { true, true, false }, sides);
        key.CurrentState.Dispatch(new ScrollEndNotification(Metrics()));
        Assert.Equal(GlowState.Pull, state.LeadingController.State);
        key.CurrentState.Dispatch(new ScrollEndNotification(Metrics(), new DragEndDetails(Velocity.Zero, 0.0)));
        Assert.Equal(GlowState.Recede, state.LeadingController.State);
        Assert.Equal(GlowState.Recede, state.TrailingController.State);
    }

    [DebugOnlyTheory]
    [InlineData(true, true, "both sides")]
    [InlineData(true, false, "leading side only")]
    [InlineData(false, true, "trailing side only")]
    [InlineData(false, false, "neither side (!)")]
    public void Glow_DiagnosticsDescribeVisibilityAndNotificationSide(bool leading, bool trailing, string description)
    {
        var indicator = new GlowingOverscrollIndicator(AxisDirection.Down, Colors.White,
            showLeading: leading, showTrailing: trailing);
        var properties = new DiagnosticPropertiesBuilder();
        indicator.DebugFillProperties(properties);
        Assert.Equal(description, properties.Properties.Single(property => property.Name == "show").ToDescription());
        string side = leading ? "leading" : "trailing";
        Assert.Equal($"OverscrollIndicatorNotification(depth: 0 (local), side: {side} edge)",
            new OverscrollIndicatorNotification(leading).ToString());
    }

    private static FixedScrollMetrics OffsetMetrics(double pixels) => new(
        minScrollExtent: -100.0, maxScrollExtent: 600.0, pixels: pixels,
        viewportDimension: 200.0, axisDirection: AxisDirection.Down, devicePixelRatio: 1.0);
}
