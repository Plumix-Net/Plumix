using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// C#-only test infrastructure: contracts from flutter_test/test/widget_tester_test.dart,
// bindings_test.dart, finders_test.dart, and restoration_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class FrameworkDartTesterFrameTests
{
    [Fact]
    public void AwaitedGestureEvents_DrainMicrotasksBetweenDownAndUpWithoutPumping()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<string>();
        tester.PumpWidget(new Listener(
            behavior: HitTestBehavior.Opaque,
            onPointerDown: _ =>
            {
                log.Add("down");
                EventLoop.ScheduleMicrotask(() => log.Add("down microtask"));
            },
            onPointerUp: _ =>
            {
                log.Add("up");
                EventLoop.ScheduleMicrotask(() => log.Add("up microtask"));
            },
            child: new SizedBox()));
        int frames = 0;
        tester.View.RenderRequested += (_, _) => frames++;

        tester.TapAt(new Point(20, 20));

        Assert.Equal(["down", "down microtask", "up", "up microtask"], log);
        Assert.Equal(0, frames);
    }

    [Fact]
    public void IdlePump_DrainsMicrotasksWithoutRunningFrameOrPostFrameCallbacks()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpAndSettle();
        int frames = 0;
        bool postFrame = false;
        bool microtask = false;
        tester.View.RenderRequested += (_, _) => frames++;
        SchedulerBinding.Instance.AddPostFrameCallback(_ => postFrame = true);
        EventLoop.ScheduleMicrotask(() => microtask = true);

        tester.Pump(TimeSpan.FromMilliseconds(123));

        Assert.True(microtask);
        Assert.False(postFrame);
        Assert.Equal(0, frames);
        Assert.Equal(1, tester.PumpAndSettle());
        Assert.Equal(0, frames);
        SchedulerBinding.Instance.ScheduleFrame();
        tester.Pump();
        Assert.True(postFrame);
        Assert.Equal(1, frames);
    }

    [Fact]
    public void MicrotaskSchedulingAFrame_DuringIdlePumpLeavesItForTheNextPump()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpAndSettle();
        int frames = 0;
        tester.View.RenderRequested += (_, _) => frames++;
        EventLoop.ScheduleMicrotask(SchedulerBinding.Instance.ScheduleFrame);

        tester.Pump();
        Assert.Equal(0, frames);
        Assert.True(SchedulerBinding.Instance.HasScheduledFrame);
        tester.Pump();
        Assert.Equal(1, frames);
    }

    [Fact]
    public void ScheduledFrame_DrainsMicrotasksBeforeBetweenAndAfterFrameCallbacks()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpAndSettle();
        var log = new List<string>();
        EventLoop.ScheduleMicrotask(() => log.Add("before begin"));
        SchedulerBinding.Instance.ScheduleFrameCallback(_ =>
        {
            log.Add("transient");
            EventLoop.ScheduleMicrotask(() => log.Add("between begin and draw"));
        });
        SchedulerBinding.Instance.AddPostFrameCallback(_ =>
        {
            log.Add("post frame");
            EventLoop.ScheduleMicrotask(() => log.Add("after draw"));
        });

        tester.Pump();

        Assert.Equal(["before begin", "transient", "between begin and draw", "post frame", "after draw"], log);
    }

    [Fact]
    public void ElapsedFakeTimer_CanScheduleTheCurrentPumpFrame()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpAndSettle();
        int frames = 0;
        tester.View.RenderRequested += (_, _) => frames++;
        using GestureTimer timer = GestureTimer.Start(
            TimeSpan.FromMilliseconds(10), SchedulerBinding.Instance.ScheduleFrame);

        tester.Pump(TimeSpan.FromMilliseconds(10));

        Assert.Equal(1, frames);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void PumpWidget_StopsAtTheRequestedPhase(int phaseValue)
    {
        using var tester = new FrameworkDartTester();
        var phase = (EnginePhase)phaseValue;
        var probe = new FrameProbe();
        probe.MarkNeedsCompositingBitsUpdate();
        int frames = 0;
        tester.View.RenderRequested += (_, _) => frames++;

        tester.PumpWidget(new ProbeWidget(probe), phase: phase);

        Assert.Same(probe, tester.ElementOfType<ProbeWidget>().FindRenderObject());
        Assert.Equal(phase >= EnginePhase.Layout, probe.HasSize);
        Assert.Equal(phase >= EnginePhase.Paint, probe.PaintCount > 0);
        Assert.Equal(phase >= EnginePhase.Composite ? 1 : 0, frames);
        Assert.Equal(phase >= EnginePhase.FlushSemantics, probe.SemanticsNode is not null);
        if (phase >= EnginePhase.Layout)
        {
            Assert.Equal(phase < EnginePhase.CompositingBits, probe.NeedsCompositingBitsUpdate);
        }
    }

    [Fact]
    public void BuildOnlyPump_FinalizesRemovedStateAndRestoresFullPhaseOnTheNextPump()
    {
        using var tester = new FrameworkDartTester();
        bool disposed = false;
        tester.PumpWidget(new DisposeProbe(() => disposed = true));
        var probe = new FrameProbe();
        tester.PumpWidget(new ProbeWidget(probe), phase: EnginePhase.Build);
        Assert.True(disposed);
        Assert.False(probe.HasSize);

        SchedulerBinding.Instance.ScheduleFrame();
        tester.Pump();
        Assert.True(probe.HasSize);
        Assert.NotNull(probe.SemanticsNode);
    }

    [Fact]
    public void StateChanges_ScheduleAFrameThroughTheBuildOwner()
    {
        using var tester = new FrameworkDartTester();
        StateSetter? update = null;
        int builds = 0;
        tester.PumpWidget(new StatefulBuilder((_, setState) =>
        {
            update = setState;
            builds++;
            return new SizedBox();
        }));
        tester.PumpAndSettle();
        int previousBuilds = builds;

        update!(() => { });
        Assert.True(SchedulerBinding.Instance.HasScheduledFrame);
        tester.Pump();
        Assert.Equal(previousBuilds + 1, builds);
    }

    [Fact]
    public void Semantics_AreEnabledByDefaultAndCanBeExplicitlyDisabledAndReenabled()
    {
        using (var tester = new FrameworkDartTester())
        {
            tester.PumpWidget(new Directionality(
                TextDirection.Ltr, new Semantics(label: "default", child: new SizedBox())));
            Assert.True(SemanticsBinding.Instance.SemanticsEnabled);
            Assert.NotNull(tester.SemanticsRootNode);
            Assert.True(tester.TestTextInput.IsRegistered);
        }

        using (var tester = new FrameworkDartTester(semanticsEnabled: false))
        {
            Assert.False(SemanticsBinding.Instance.SemanticsEnabled);
            Assert.Null(tester.SemanticsRootNode);
            Assert.Throws<InvalidOperationException>(() => tester.Semantics);
            Assert.Throws<InvalidOperationException>(() => Find.BySemanticsLabel("label"));
            tester.PumpWidget(new Directionality(
                TextDirection.Ltr, new Semantics(label: "label", child: new SizedBox())));
            for (int i = 0; i < 2; i++)
            {
                SemanticsHandle handle = tester.EnsureSemantics();
                try
                {
                    tester.Pump();
                    Finds.OneWidget(Find.BySemanticsLabel("label"));
                }
                finally
                {
                    handle.Dispose();
                }

                Assert.Null(tester.SemanticsRootNode);
            }
        }
    }

    [Fact]
    public void DefaultView_IsReusedAfterAnExplicitViewAndAViewlessTree()
    {
        using var tester = new FrameworkDartTester();
        RenderView original = tester.RenderView;
        PipelineOwner owner = original.Owner!;
        Assert.IsType<ReusableRenderView>(original);

        tester.PumpWidget(new View(tester.View, new SizedBox()), wrapWithView: false);
        Assert.NotSame(original, tester.RenderView);
        tester.PumpWidget(new ViewCollection(views: []), wrapWithView: false);
        Assert.DoesNotContain(RendererBinding.Instance.RenderViews, view => ReferenceEquals(view, original));
        tester.PumpWidget(new SizedBox());
        Assert.Same(original, tester.RenderView);
        Assert.Same(owner, tester.RenderView.Owner);
        Assert.NotNull(tester.SemanticsRootNode);
    }

    [Fact]
    public void RestartAndRestore_PreservesAnExplicitViewRoot()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new View(tester.View, new RootRestorationScope(
            restorationId: "root",
            child: new Directionality(TextDirection.Ltr, new ListView(
                restorationId: "list",
                children: Enumerable.Range(0, 50)
                    .Select(_ => (Widget)new SizedBox(height: 50)).ToList())))), wrapWithView: false);
        ScrollableState before = tester.State<ScrollableState>();
        before.Position.JumpTo(100);
        tester.Pump();

        tester.RestartAndRestore();

        Assert.Single(tester.ElementsOfType<View>());
        ScrollableState after = tester.State<ScrollableState>();
        Assert.NotSame(before, after);
        Assert.Equal(100, after.Position.Pixels);
    }

    [Fact]
    public void PumpAndSettle_UsesFlutterAnimationTimingAndValidatesDurations()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpAndSettle();
        using var controller = new AnimationController(duration: TimeSpan.FromSeconds(1));
        controller.Forward();
        Assert.Equal(5, tester.PumpAndSettle(TimeSpan.FromMilliseconds(300)));
        Assert.Equal(1, tester.PumpAndSettle());
        Assert.Throws<AssertionError>(() => tester.PumpAndSettle(TimeSpan.Zero));
        Assert.Throws<AssertionError>(() => tester.PumpAndSettle(timeout: TimeSpan.Zero));
    }

    private sealed class ProbeWidget(FrameProbe probe) : LeafRenderObjectWidget
    {
        public override RenderObject CreateRenderObject(BuildContext context) => probe;
    }

    private sealed class FrameProbe : RenderBox
    {
        public int PaintCount { get; private set; }

        protected override void PerformLayout() => Size = Constraints.Biggest;

        public override void Paint(PaintingContext ctx, Point offset) => PaintCount++;

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.IsSemanticBoundary = true;
            configuration.Label = "probe";
            configuration.TextDirection = TextDirection.Ltr;
        }
    }

    private sealed class DisposeProbe(Action onDispose) : StatefulWidget
    {
        public Action OnDispose { get; } = onDispose;
        public override State CreateState() => new DisposeProbeState();
    }

    private sealed class DisposeProbeState : State<DisposeProbe>
    {
        public override Widget Build(BuildContext context) => new SizedBox();

        public override void Dispose()
        {
            Widget.OnDispose();
            base.Dispose();
        }
    }
}
