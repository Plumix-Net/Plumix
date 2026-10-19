using Avalonia;
using Plumix.Widgets;
using Xunit;

// C#-only test infrastructure: flutter_test's build/render/finalize ordering for isolated trees.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class IsolatedFrameTests
{
    [Fact]
    public void IsolatedViewRegistration_PreservesMetricsAndSupportsOutOfOrderDisposal()
    {
        RenderView CreateView(double width)
        {
            var view = new FlutterView(new Size(width, 40), viewId: int.MaxValue - 123);
            return new RenderView(view, configuration: ViewConfiguration.FromView(view));
        }

        RendererBinding binding = RendererBinding.Instance;
        RenderView original = CreateView(10);
        binding.AddRenderView(original);
        try
        {
            RenderView first = CreateView(20);
            ViewConfiguration configuration = first.Configuration;
            using var firstRegistration = new TestRenderViewRegistration(first);
            Assert.Same(configuration, first.Configuration);
            RenderView second = CreateView(30);
            using var secondRegistration = new TestRenderViewRegistration(second);
            firstRegistration.Dispose();
            Assert.Contains(second, binding.RenderViews);
            Assert.DoesNotContain(first, binding.RenderViews);

            secondRegistration.Dispose();
            Assert.Contains(original, binding.RenderViews);
            Assert.DoesNotContain(first, binding.RenderViews);
            Assert.DoesNotContain(second, binding.RenderViews);
        }
        finally
        {
            binding.RemoveRenderView(original);
        }
    }

    [Fact]
    public void DirectBuildScope_DoesNotChangeTheSchedulerPhaseOrRunCallbacks()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpAndSettle();
        bool transient = false;
        SchedulerBinding.Instance.ScheduleFrameCallback(_ => transient = true);

        tester.Owner.BuildScope(tester.Root, () =>
        {
            Assert.Equal(SchedulerPhase.Idle, SchedulerBinding.Instance.SchedulerPhase);
            Assert.False(transient);
        });

        Assert.False(transient);
        tester.Pump();
        Assert.True(transient);
    }

    [Fact]
    public void IsolatedFrame_RendersBeforeFinalizingAndRunsMicrotasksAfterPostFrame()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<string>();
        tester.PumpWidget(new FrameworkDartFlipWidget(new DisposeProbe(log), new SizedBox()));
        tester.State<FrameworkDartFlipWidgetState>().Flip();
        SchedulerBinding.Instance.ScheduleFrameCallback(_ =>
        {
            Assert.Equal(SchedulerPhase.TransientCallbacks, SchedulerBinding.Instance.SchedulerPhase);
            log.Add("transient");
            EventLoop.ScheduleMicrotask(() => log.Add("mid-frame microtask"));
        });
        SchedulerBinding.Instance.AddPostFrameCallback(_ =>
        {
            Assert.Equal(SchedulerPhase.PostFrameCallbacks, SchedulerBinding.Instance.SchedulerPhase);
            log.Add("post-frame");
        });

        tester.Owner.PumpFrame(() =>
        {
            Assert.Equal(SchedulerPhase.PersistentCallbacks, SchedulerBinding.Instance.SchedulerPhase);
            Assert.DoesNotContain("dispose", log);
            log.Add("render");
            EventLoop.ScheduleMicrotask(() => log.Add("render microtask"));
        });

        Assert.Equal(
            ["transient", "mid-frame microtask", "render", "dispose", "post-frame", "render microtask"],
            log);
        Assert.Equal(SchedulerPhase.Idle, SchedulerBinding.Instance.SchedulerPhase);
    }

    [Fact]
    public void FailedRender_SkipsFinalizationButRestoresOverrideAndFinishesTheFrame()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<string>();
        tester.PumpWidget(new FrameworkDartFlipWidget(new DisposeProbe(log), new SizedBox()));
        tester.State<FrameworkDartFlipWidgetState>().Flip();
        var failure = new InvalidOperationException("render failed");
        Action previous = () => { };
        RendererBinding.Instance.DrawFrameOverrideForTests = previous;
        SchedulerBinding.Instance.AddPostFrameCallback(_ => log.Add("post-frame"));
        try
        {
            tester.Owner.PumpFrame(() => throw failure);

            Assert.Same(failure, tester.TakeException());
            Assert.Equal(["post-frame"], log);
            Assert.Same(previous, RendererBinding.Instance.DrawFrameOverrideForTests);
            Assert.Equal(SchedulerPhase.Idle, SchedulerBinding.Instance.SchedulerPhase);
        }
        finally
        {
            RendererBinding.Instance.DrawFrameOverrideForTests = null;
        }

        tester.Owner.PumpFrame();
        Assert.Equal(["post-frame", "dispose"], log);
    }

    [Fact]
    public void IsolatedFrames_TickOncePerFrameWithoutAdvancingTimeAndHonorCancellation()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpAndSettle();
        int ticks = 0;
        TimeSpan timestamp = SchedulerBinding.Instance.CurrentSystemFrameTimeStamp;
        using var ticker = new Ticker(_ => ticks++);
        ticker.Start();
        int canceled = SchedulerBinding.Instance.ScheduleFrameCallback(
            _ => throw new InvalidOperationException("canceled"));
        SchedulerBinding.Instance.CancelFrameCallbackWithId(canceled);

        tester.Owner.PumpFrame();
        Assert.Equal(1, ticks);
        tester.Owner.PumpFrame();
        Assert.Equal(2, ticks);
        Assert.Equal(timestamp, SchedulerBinding.Instance.CurrentSystemFrameTimeStamp);
        ticker.Stop();
    }

    private sealed class DisposeProbe(List<string> log) : StatefulWidget
    {
        public List<string> Log { get; } = log;

        public override State CreateState() => new DisposeProbeState();
    }

    private sealed class DisposeProbeState : State<DisposeProbe>
    {
        public override Widget Build(BuildContext context) => new SizedBox();

        public override void Dispose()
        {
            Assert.Equal(SchedulerPhase.PersistentCallbacks, SchedulerBinding.Instance.SchedulerPhase);
            Widget.Log.Add("dispose");
            base.Dispose();
        }
    }
}
