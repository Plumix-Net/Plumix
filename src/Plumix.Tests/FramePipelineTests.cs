using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/scheduler/binding.dart; flutter/packages/flutter/lib/src/rendering/object.dart (parity regression tests)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class FramePipelineTests
{
    [Fact]
    public void Scheduler_EnsureVisualUpdateSchedulesOnlyOutsideAFrame()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            var phases = new List<(SchedulerPhase Phase, bool Scheduled)>();

            SchedulerBinding.Instance.ScheduleFrameCallback(_ =>
            {
                SchedulerBinding.Instance.EnsureVisualUpdate();
                phases.Add((SchedulerBinding.Instance.SchedulerPhase, SchedulerBinding.Instance.HasScheduledFrame));
            });
            SchedulerBinding.Instance.AddPersistentFrameCallback(_ =>
            {
                SchedulerBinding.Instance.EnsureVisualUpdate();
                phases.Add((SchedulerBinding.Instance.SchedulerPhase, SchedulerBinding.Instance.HasScheduledFrame));
            });
            SchedulerBinding.Instance.AddPostFrameCallback(
                _ =>
                {
                    SchedulerBinding.Instance.EnsureVisualUpdate();
                    phases.Add((SchedulerBinding.Instance.SchedulerPhase, SchedulerBinding.Instance.HasScheduledFrame));
                });

            SchedulerBinding.PumpFrameForTests(TimeSpan.FromMilliseconds(16));

            // Dart's `ensureVisualUpdate` leaves the frame in flight to cover the update; only the
            // post-frame phase asks for another frame.
            Assert.Equal(
                [
                    (SchedulerPhase.TransientCallbacks, false),
                    (SchedulerPhase.PersistentCallbacks, false),
                    (SchedulerPhase.PostFrameCallbacks, true),
                ],
                phases);

            SchedulerBinding.ResetForTests();
            Assert.False(SchedulerBinding.Instance.HasScheduledFrame);
            SchedulerBinding.Instance.EnsureVisualUpdate();
            Assert.True(SchedulerBinding.Instance.HasScheduledFrame);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void Scheduler_RunsTransientThenPersistentThenPostFrame()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            var events = new List<string>();

            SchedulerBinding.Instance.ScheduleFrameCallback(_ => events.Add("transient"));
            SchedulerBinding.Instance.AddPersistentFrameCallback(_ => events.Add("persistent"));
            SchedulerBinding.Instance.AddPostFrameCallback(_ => events.Add("post"));

            SchedulerBinding.Instance.ScheduleFrame();
            SchedulerBinding.PumpFrameForTests(TimeSpan.FromMilliseconds(16));

            Assert.Equal(["transient", "persistent", "post"], events);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void Scheduler_PostFrameAddedDuringPostFrame_RunsOnNextFrame()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            var events = new List<string>();

            SchedulerBinding.Instance.AddPostFrameCallback(_ =>
            {
                events.Add("post-1");
                SchedulerBinding.Instance.AddPostFrameCallback(_ => events.Add("post-2"));
            });

            SchedulerBinding.Instance.ScheduleFrame();
            SchedulerBinding.PumpFrameForTests(TimeSpan.FromMilliseconds(16));

            Assert.Equal(["post-1"], events);

            SchedulerBinding.PumpFrameForTests(TimeSpan.FromMilliseconds(32));
            Assert.Equal(["post-1", "post-2"], events);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void Scheduler_RunsPersistentCallbacksInRegistrationOrderAndDuplicatesTwice()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            var events = new List<string>();

            void Persistent(TimeSpan _)
            {
                events.Add("persistent");
            }

            SchedulerBinding.Instance.AddPersistentFrameCallback(_ => events.Add("first"));
            SchedulerBinding.Instance.AddPersistentFrameCallback(Persistent);
            SchedulerBinding.Instance.AddPersistentFrameCallback(_ => events.Add("last"));
            // Dart appends without deduplicating, so a second registration runs twice.
            SchedulerBinding.Instance.AddPersistentFrameCallback(Persistent);

            SchedulerBinding.Instance.ScheduleFrame();
            SchedulerBinding.PumpFrameForTests(TimeSpan.FromMilliseconds(16));

            Assert.Equal(["first", "persistent", "last", "persistent"], events);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void BuildOwner_ScheduledBuilds_RunInsideDrawFrame()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            var owner = new BuildOwner(focusManager: FocusManager.Instance)
            {
                OnBuildScheduled = SchedulerBinding.Instance.ScheduleFrame
            };
            var element = new ProbeElement();
            element.Attach(owner);

            int drawFrames = 0;
            SchedulerBinding.Instance.AddPersistentFrameCallback(_ =>
            {
                drawFrames += 1;
                owner.BuildScope(element);
            });

            owner.BuildScope(element, () => element.Mount(parent: null, newSlot: null));

            // An element is born dirty and clears the flag by building once during mount, so the
            // first build is not the scheduler's work.
            Assert.Equal(1, element.RebuildCount);
            Assert.False(element.Dirty);

            element.MarkNeedsBuild();
            element.MarkNeedsBuild();

            Assert.Equal(0, drawFrames);
            Assert.Equal(1, element.RebuildCount);

            SchedulerBinding.PumpFrameForTests(TimeSpan.FromMilliseconds(16));

            Assert.Equal(1, drawFrames);
            Assert.Equal(2, element.RebuildCount);

            SchedulerBinding.PumpFrameForTests(TimeSpan.FromMilliseconds(32));
            Assert.Equal(1, drawFrames);
            Assert.Equal(2, element.RebuildCount);
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void HeadlessPump_DrainsBuildMicrotasksAfterComposite()
    {
        SchedulerBinding.ResetForTests();
        try
        {
            var owner = new BuildOwner(focusManager: FocusManager.Instance);
            var element = new ProbeElement();
            element.Attach(owner);
            owner.BuildScope(element, () => element.Mount(parent: null, newSlot: null));

            bool microtaskRan = false;
            element.OnRebuild = () => EventLoop.ScheduleMicrotask(() => microtaskRan = true);
            element.MarkNeedsBuild();
            var renderView = new RenderView(new FlutterView(new Size(80, 40)));
            var pipeline = new PipelineOwner(renderView);
            pipeline.Attach(renderView);
            owner.PumpFrame(() =>
            {
                Assert.False(microtaskRan);
                pipeline.FlushLayout();
                pipeline.FlushCompositingBits();
                pipeline.FlushPaint();
                Assert.False(microtaskRan);
                pipeline.CompositeFrame();
                Assert.False(microtaskRan);
            });
            Assert.True(microtaskRan);
            element.UnmountRoot();
        }
        finally
        {
            SchedulerBinding.ResetForTests();
        }
    }

    [Fact]
    public void WidgetHost_BindingFrame_BuildsDirtyWidgetsBeforeLayingOutLazyList()
    {
        // The host never lays out on its own: every frame is WidgetsBinding.drawFrame, which builds the
        // dirty widgets before RendererBinding.drawFrame lays out, so a lazy list that creates children
        // during layout never meets a dirty element outside its build scope.
        var controller = new ScrollController();
        var probe = new RebuildCounter();
        var reported = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        var host = new WidgetHost();
        try
        {
            host.RootWidget = new Directionality(
                Plumix.UI.TextDirection.Ltr,
                new Column(children:
                [
                    probe,
                    new SizedBox(
                        height: 200,
                        child: ListView.Builder(
                            itemCount: 100,
                            itemExtent: 40,
                            controller: controller,
                            itemBuilder: (_, index) => new Semantics(
                                label: $"row {index}",
                                child: new SizedBox(height: 40)))),
                ]));
            host.PumpFrameForTests(new Size(300, 400));
            Assert.Equal(1, probe.BuildCount);

            probe.State!.Poke();
            controller.JumpTo(1000);
            host.PumpFrameForTests(new Size(300, 400));

            Assert.Empty(reported);
            Assert.Equal(2, probe.BuildCount);
        }
        finally
        {
            FlutterError.OnError = previous;
            host.RootWidget = null;
            SchedulerBinding.PumpFrameForTests();
        }
    }

    private sealed class RebuildCounter : StatefulWidget
    {
        public int BuildCount { get; set; }

        public RebuildCounterState? State { get; set; }

        public override State CreateState() => new RebuildCounterState();
    }

    private sealed class RebuildCounterState : State<RebuildCounter>
    {
        public override void InitState()
        {
            base.InitState();
            Widget.State = this;
        }

        public void Poke() => SetState(() => { });

        public override Widget Build(BuildContext context)
        {
            Widget.BuildCount += 1;
            return new SizedBox(height: 20);
        }
    }

    private sealed class ProbeWidget : Widget
    {
        public override Element CreateElement()
        {
            throw new NotSupportedException("ProbeWidget does not create elements.");
        }
    }

    private sealed class ProbeElement : Element
    {
        public int RebuildCount { get; private set; }

        public Action? OnRebuild { get; set; }

        public ProbeElement() : base(new ProbeWidget())
        {
        }

        protected override void OnMount()
        {
            base.OnMount();

            // Dart's `_firstBuild`: every concrete element builds once during mount, which is what
            // clears the dirty flag an element is born with and makes MarkNeedsBuild schedulable.
            Rebuild();
        }

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            RebuildCount += 1;
            OnRebuild?.Invoke();
        }
    }

    [Fact]
    public void PipelineOwner_FlushLayout_SkipsCompositingAndTriggersSemantics()
    {
        var renderView = new RenderView(new FlutterView(new Size(800, 600)));
        var child = new ProbeRenderBox();
        renderView.Child = child;

        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        // Clear initial attachment dirties before checking layout-driven transitions.
        pipeline.FlushCompositingBits();
        pipeline.FlushSemantics();
        child.ResetCounters();

        pipeline.RequestLayout();
        pipeline.FlushLayout(new Size(320, 240));
        pipeline.FlushCompositingBits();
        pipeline.FlushSemantics();

        Assert.Equal(1, child.LayoutCount);
        Assert.Equal(0, child.CompositingUpdateCount);
        Assert.Equal(1, child.SemanticsUpdateCount);
    }

    private sealed class ProbeRenderBox : RenderBox
    {
        public int LayoutCount { get; private set; }
        public int CompositingUpdateCount { get; private set; }
        public int SemanticsUpdateCount { get; private set; }

        public void ResetCounters()
        {
            LayoutCount = 0;
            CompositingUpdateCount = 0;
            SemanticsUpdateCount = 0;
        }

        protected override void PerformLayout()
        {
            LayoutCount += 1;
            Size = Constraints.Constrain(new Size(10, 10));
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }

        protected override void PerformUpdateCompositingBits()
        {
            CompositingUpdateCount += 1;
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            SemanticsUpdateCount += 1;
        }
    }
}
