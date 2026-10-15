using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// C#-only infrastructure: the engine side of `FrameTiming` (the engine's `FrameTimingsRecorder` and
// `Shell::OnFrameRasterized` batching) and the host's frame wiring, which make `WidgetsBinding`'s
// first-frame-rasterized half reachable on a real host.

[Collection(SchedulerTestCollection.Name)]
public sealed class FrameTimingsRecorderTests : IDisposable
{
    private readonly List<IReadOnlyList<FrameTiming>> _reports = [];
    private readonly List<(Action Callback, TimeSpan Delay)> _delayed = [];

    public FrameTimingsRecorderTests()
    {
        SchedulerBinding.ResetForTests();
        FrameTimingsReporter.ResetForTests();
        FrameTimingsReporter.PostDelayed = (callback, delay) => _delayed.Add((callback, delay));
    }

    public void Dispose()
    {
        FrameTimingsReporter.ResetForTests();
        SchedulerBinding.ResetForTests();
    }

    private void ListenForTimings() => SchedulerBinding.Instance.AddTimingsCallback(_reports.Add);

    [Fact]
    public void NothingIsRecordedWhileTheFrameworkHasNoTimingsCallback()
    {
        PlatformDispatcher.Instance.OnReportTimings = null;

        FrameTimingsReporter.OnFrameRasterized(Rasterize());

        Assert.Equal(0, FrameTimingsReporter.UnreportedFramesCount);
        Assert.Empty(_delayed);
    }

    [Fact]
    public void TheFirstRasterizedFrameIsReportedAtOnce_LaterOnesAfterTheBatchTime()
    {
        ListenForTimings();

        FrameTiming first = Rasterize();
        FrameTimingsReporter.OnFrameRasterized(first);
        Assert.Same(first, Assert.Single(Assert.Single(_reports)));

        FrameTimingsReporter.OnFrameRasterized(Rasterize());
        FrameTimingsReporter.OnFrameRasterized(Rasterize());
        Assert.Single(_reports);
        (Action report, TimeSpan delay) = Assert.Single(_delayed);
        Assert.Equal(TimeSpan.FromMilliseconds(Foundation.Constants.KReleaseMode ? 1000 : 100), delay);

        report();
        Assert.Equal(2, _reports[1].Count);
        Assert.Equal(0, FrameTimingsReporter.UnreportedFramesCount);
    }

    [Fact]
    public void AHundredUnreportedFramesAreReportedAtOnce()
    {
        ListenForTimings();
        FrameTimingsReporter.OnFrameRasterized(Rasterize());

        for (int frame = 0; frame < FrameTimingsReporter.MaxUnreportedFrames; frame++)
        {
            FrameTimingsReporter.OnFrameRasterized(Rasterize());
        }

        Assert.Equal(2, _reports.Count);
        Assert.Equal(FrameTimingsReporter.MaxUnreportedFrames, _reports[1].Count);
    }

    [Fact]
    public void ARecorderStampsThePhasesInOrder_AndViewsOfOneFrameShareItsNumber()
    {
        FrameTimingsRecorder.RecordVsync();
        FrameTimingsRecorder.RecordBuildStart();
        FrameTimingsRecorder first = FrameTimingsRecorder.ForRender();
        FrameTimingsRecorder second = FrameTimingsRecorder.ForRender();
        FrameTimingsRecorder.EndFrame();
        FrameTimingsRecorder unscheduled = FrameTimingsRecorder.ForRender();

        Assert.Equal(first.FrameNumber, second.FrameNumber);
        Assert.NotEqual(first.FrameNumber, unscheduled.FrameNumber);

        first.RecordRasterStart();
        FrameTiming timing = first.RecordRasterEnd();
        long[] phases =
        [
            timing.TimestampInMicroseconds(FramePhase.VsyncStart),
            timing.TimestampInMicroseconds(FramePhase.BuildStart),
            timing.TimestampInMicroseconds(FramePhase.BuildFinish),
            timing.TimestampInMicroseconds(FramePhase.RasterStart),
            timing.TimestampInMicroseconds(FramePhase.RasterFinish),
        ];
        Assert.Equal(phases.Order(), phases);
        Assert.Equal(first.FrameNumber, timing.FrameNumber);
    }

    [Fact]
    public void ARenderOnlyHostRegistersItsViewWithTheBindingAndUnregistersIt()
    {
        // rendering/binding.dart: apps that do not call runApp wire the deprecated renderView into the
        // pipeline owner tree and register it with addRenderView themselves.
        var host = new PlumixHost();
        var box = new RenderConstrainedBox(BoxConstraints.Tight(new Size(10, 10)));

        host.SetRootChild(box);
        try
        {
            Assert.Contains(RendererBinding.Instance.RenderViews, view => ReferenceEquals(view.Child, box));
            host.PumpFrameForTests(new Size(100, 50));
            Assert.False(box.DebugNeedsLayout);
        }
        finally
        {
            host.SetRootChild(null);
        }

        Assert.DoesNotContain(RendererBinding.Instance.RenderViews, view => ReferenceEquals(view.Child, box));
    }

    private static FrameTiming Rasterize()
    {
        FrameTimingsRecorder recorder = FrameTimingsRecorder.ForRender();
        recorder.RecordRasterStart();
        return recorder.RecordRasterEnd();
    }
}
