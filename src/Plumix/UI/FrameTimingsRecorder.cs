using System.Diagnostics;
using Avalonia.Threading;

// C#-only infrastructure: the engine side of `FrameTiming`. A port of the engine's
// `flow/frame_timings.{h,cc}` (`FrameTimingsRecorder`) and of `Shell::OnFrameRasterized`/`ReportTimings`
// (`shell/common/shell.cc`), which batch rasterized frames to `PlatformDispatcher.onReportTimings`.
// No Dart source maps to this file.

namespace Plumix.UI;

/// <summary>Records the time points of one frame as it moves from vsync through build to raster.</summary>
/// <remarks>
/// The engine's <c>FrameTimingsRecorder</c>: the vsync source starts it (<see cref="RecordVsync"/>), the
/// animator stamps the build (<see cref="RecordBuildStart"/>, <see cref="RecordBuildEnd"/>) and the
/// rasterizer stamps the raster phase and produces the <see cref="FrameTiming"/>
/// (<see cref="RecordRasterEnd"/>). Times are microseconds on the frame clock; the raster-finish
/// wall time is microseconds since the Unix epoch, as the engine's <c>CurrentWallTime</c>.
/// </remarks>
internal sealed class FrameTimingsRecorder
{
    private static int s_frameNumberGen = 1;

    private long _vsyncStart;
    private long _buildStart;
    private long _buildEnd;
    private long _rasterStart;

    /// <summary>Creates a recorder for the next frame, taking the next frame number.</summary>
    public FrameTimingsRecorder()
    {
        FrameNumber = Interlocked.Increment(ref s_frameNumberGen) - 1;
    }

    private FrameTimingsRecorder(int frameNumber)
    {
        FrameNumber = frameNumber;
    }

    /// <summary>The frame number this recorder stamps on its <see cref="FrameTiming"/>.</summary>
    public int FrameNumber { get; }

    /// <summary>The recorder of the frame the vsync source is producing, if a vsync started one.</summary>
    /// <remarks>
    /// The engine's animator keeps the recorder of the frame in flight; a frame pumped without a vsync
    /// (a warm-up frame or a test pump) has none, and <see cref="ForRender"/> starts one at render time.
    /// </remarks>
    internal static FrameTimingsRecorder? Current { get; private set; }

    /// <summary>The current frame-clock time in microseconds.</summary>
    internal static long NowMicroseconds() =>
        (long)(Stopwatch.GetElapsedTime(s_epoch).Ticks / (TimeSpan.TicksPerMillisecond / 1000.0));

    private static readonly long s_epoch = Stopwatch.GetTimestamp();

    /// <summary>The duration of the build phase: <c>FrameTimingsRecorder::GetBuildDuration</c>.</summary>
    public TimeSpan BuildDuration =>
        TimeSpan.FromTicks((_buildEnd - _buildStart) * (TimeSpan.TicksPerMillisecond / 1000));

    /// <summary>Starts the recorder of a new frame at its vsync: <c>RecordVsync</c>.</summary>
    internal static void RecordVsync()
    {
        long now = NowMicroseconds();
        Current = new FrameTimingsRecorder { _vsyncStart = now, _buildStart = now };
    }

    /// <summary>Stamps the start of the build phase of <see cref="Current"/>: <c>RecordBuildStart</c>.</summary>
    internal static void RecordBuildStart()
    {
        if (Current is { } recorder)
        {
            recorder._buildStart = NowMicroseconds();
        }
    }

    /// <summary>Forgets the frame in flight once the vsync source finished it.</summary>
    internal static void EndFrame() => Current = null;

    /// <summary>
    /// A recorder for a view the framework just rendered, with the build phase ending now:
    /// <c>RecordBuildEnd</c> on a clone of the frame's recorder, as each view's layer tree carries its own.
    /// </summary>
    internal static FrameTimingsRecorder ForRender()
    {
        long now = NowMicroseconds();
        if (Current is not { } frame)
        {
            return new FrameTimingsRecorder { _vsyncStart = now, _buildStart = now, _buildEnd = now };
        }

        return new FrameTimingsRecorder(frame.FrameNumber)
        {
            _vsyncStart = frame._vsyncStart,
            _buildStart = frame._buildStart,
            _buildEnd = now,
        };
    }

    /// <summary>Stamps the start of the raster phase: <c>RecordRasterStart</c>.</summary>
    public void RecordRasterStart() => _rasterStart = NowMicroseconds();

    /// <summary>Stamps the end of the raster phase and returns the frame's timing: <c>RecordRasterEnd</c>.</summary>
    public FrameTiming RecordRasterEnd()
    {
        long rasterEnd = NowMicroseconds();
        long wallTime = (DateTime.UtcNow - DateTime.UnixEpoch).Ticks / (TimeSpan.TicksPerMillisecond / 1000);
        return new FrameTiming(
            vsyncStart: _vsyncStart,
            buildStart: _buildStart,
            buildFinish: _buildEnd,
            rasterStart: _rasterStart,
            rasterFinish: rasterEnd,
            rasterFinishWallTime: wallTime,
            frameNumber: FrameNumber);
    }
}

/// <summary>Batches rasterized frame timings to <see cref="PlatformDispatcher.OnReportTimings"/>.</summary>
/// <remarks>
/// The engine's <c>Shell::OnFrameRasterized</c>/<c>Shell::ReportTimings</c>: nothing is recorded while the
/// framework has no <c>onReportTimings</c> callback; the first rasterized frame, and any batch that reaches
/// 100 frames, is reported at once; otherwise the batch is reported 100 ms later (1 s in release), so the
/// last frames of an animation still arrive.
/// </remarks>
internal static class FrameTimingsReporter
{
    /// <summary>The batch size that is reported immediately: the engine's 100-frame threshold.</summary>
    internal const int MaxUnreportedFrames = 100;

    private static readonly List<FrameTiming> s_unreported = [];
    private static bool s_firstFrameRasterized;
    private static bool s_reportScheduled;

    /// <summary>The batch latency: <c>kBatchTimeInMilliseconds</c>.</summary>
    internal static TimeSpan BatchTime =>
        TimeSpan.FromMilliseconds(Foundation.Constants.KReleaseMode ? 1000 : 100);

    /// <summary>
    /// Replaces the delayed post the batch latency uses (the engine's raster task runner); a test drives
    /// it by hand.
    /// </summary>
    internal static Action<Action, TimeSpan> PostDelayed { get; set; } = DefaultPostDelayed;

    /// <summary>The timings recorded but not yet reported.</summary>
    internal static int UnreportedFramesCount => s_unreported.Count;

    /// <summary>Records a rasterized frame and reports the batch when due: <c>Shell::OnFrameRasterized</c>.</summary>
    internal static void OnFrameRasterized(FrameTiming timing)
    {
        if (PlatformDispatcher.Instance.OnReportTimings is null)
        {
            return;
        }

        s_unreported.Add(timing);
        if (!s_firstFrameRasterized || UnreportedFramesCount >= MaxUnreportedFrames)
        {
            s_firstFrameRasterized = true;
            ReportTimings();
        }
        else if (!s_reportScheduled)
        {
            // Also make sure that frame times get reported with a max latency of 1 second. Otherwise,
            // the timings of last few frames of an animation may never be reported until the next
            // animation starts.
            s_reportScheduled = true;
            PostDelayed(
                static () =>
                {
                    s_reportScheduled = false;
                    if (UnreportedFramesCount > 0)
                    {
                        ReportTimings();
                    }
                },
                BatchTime);
        }
    }

    /// <summary>Forgets every recorded timing and the first-frame state.</summary>
    internal static void ResetForTests()
    {
        s_unreported.Clear();
        s_firstFrameRasterized = false;
        s_reportScheduled = false;
        PostDelayed = DefaultPostDelayed;
    }

    private static void ReportTimings()
    {
        FrameTiming[] timings = [.. s_unreported];
        s_unreported.Clear();
        PlatformDispatcher.Instance.ReportTimings(timings);
    }

    private static void DefaultPostDelayed(Action callback, TimeSpan delay)
    {
        // The engine posts this report to the UI task runner, not to idle time: at background priority
        // Android would hold the batch for as long as an animation keeps the main thread busy.
        DispatcherTimer.RunOnce(callback, delay, PlatformDispatcher.EventLoopTaskPriority);
    }
}
