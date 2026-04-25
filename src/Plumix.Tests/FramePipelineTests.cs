using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/scheduler/binding.dart; flutter/packages/flutter/lib/src/rendering/object.dart (parity regression tests)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class FramePipelineTests
{
    [Fact]
    public void Scheduler_RunsBeginThenDrawThenPostFrame()
    {
        Scheduler.ResetForTests();
        try
        {
            var events = new List<string>();

            Scheduler.BeginFrame += _ => events.Add("begin");
            Scheduler.DrawFrame += _ => events.Add("draw");
            Scheduler.AddPostFrameCallback(_ => events.Add("post"));

            Scheduler.ScheduleFrame();
            Scheduler.PumpFrameForTests(TimeSpan.FromMilliseconds(16));

            Assert.Equal(["begin", "draw", "post"], events);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scheduler_PostFrameAddedDuringPostFrame_RunsOnNextFrame()
    {
        Scheduler.ResetForTests();
        try
        {
            var events = new List<string>();

            Scheduler.AddPostFrameCallback(_ =>
            {
                events.Add("post-1");
                Scheduler.AddPostFrameCallback(_ => events.Add("post-2"));
            });

            Scheduler.ScheduleFrame();
            Scheduler.PumpFrameForTests(TimeSpan.FromMilliseconds(16));

            Assert.Equal(["post-1"], events);

            Scheduler.PumpFrameForTests(TimeSpan.FromMilliseconds(32));
            Assert.Equal(["post-1", "post-2"], events);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scheduler_RunsPersistentBetweenBeginAndDraw()
    {
        Scheduler.ResetForTests();
        try
        {
            var events = new List<string>();

            void Persistent(TimeSpan _)
            {
                events.Add("persistent");
            }

            Scheduler.BeginFrame += _ => events.Add("begin");
            Scheduler.AddPersistentFrameCallback(Persistent);
            Scheduler.DrawFrame += _ => events.Add("draw");

            Scheduler.ScheduleFrame();
            Scheduler.PumpFrameForTests(TimeSpan.FromMilliseconds(16));

            Assert.Equal(["begin", "persistent", "draw"], events);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void BuildOwner_ScheduledBuilds_RunInsideDrawFrame()
    {
        Scheduler.ResetForTests();
        try
        {
            var owner = new BuildOwner
            {
                OnBuildScheduled = Scheduler.ScheduleFrame
            };

            var drawFrames = 0;
            Scheduler.DrawFrame += _ =>
            {
                drawFrames += 1;
                owner.BuildScope();
            };

            var element = new ProbeElement();
            element.Attach(owner);
            element.Mount(parent: null, newSlot: null);

            element.MarkNeedsBuild();
            element.MarkNeedsBuild();

            Assert.Equal(0, drawFrames);
            Assert.Equal(0, element.RebuildCount);

            Scheduler.PumpFrameForTests(TimeSpan.FromMilliseconds(16));

            Assert.Equal(1, drawFrames);
            Assert.Equal(1, element.RebuildCount);

            Scheduler.PumpFrameForTests(TimeSpan.FromMilliseconds(32));
            Assert.Equal(1, drawFrames);
            Assert.Equal(1, element.RebuildCount);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    private sealed class ProbeWidget : Widget
    {
        internal override Element CreateElement()
        {
            throw new NotSupportedException("ProbeWidget does not create elements.");
        }
    }

    private sealed class ProbeElement : Element
    {
        public int RebuildCount { get; private set; }

        public ProbeElement() : base(new ProbeWidget())
        {
        }

        internal override void Rebuild()
        {
            Dirty = false;
            RebuildCount += 1;
        }
    }

    [Fact]
    public void PipelineOwner_FlushLayout_TriggersCompositingAndSemanticsPhases()
    {
        var renderView = new RenderView();
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
        Assert.Equal(1, child.CompositingUpdateCount);
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

        protected override void PerformSemantics()
        {
            SemanticsUpdateCount += 1;
        }
    }
}
