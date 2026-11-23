using System;
using System.Collections.Generic;
using Android.OS;
using Android.Util;
using Plumix.UI;
using AndroidProcess = Android.OS.Process;

namespace Plumix.Android;

// C#-only host infrastructure; no Dart parity source. Stage 4 measurement probe (`--ez perf true`): logs the
// time from process start to the first rasterized frame, then every frame's build and raster durations from
// the framework's own FrameTimings, so the Avalonia and Impeller hosts are measured by the same code.
// Uses only public API and android.os, so the Avalonia sample host can compile the same file for a baseline.

/// <summary>Logs frame timings under the <c>PlumixPerf</c> tag.</summary>
public static class PerfProbe
{
    private const string Tag = "PlumixPerf";
    private static bool s_started;
    private static bool s_firstReported;

    /// <summary>Whether startup marks are logged (set before the host starts).</summary>
    public static bool Enabled { get; set; }

    /// <summary>Logs a startup phase with the time since the process started.</summary>
    public static void Mark(string phase)
    {
        if (Enabled)
        {
            long sinceStartMs = SystemClock.ElapsedRealtime() - AndroidProcess.StartElapsedRealtime;
            Log.Info(Tag, $"mark {phase} ms={sinceStartMs}");
        }
    }

    /// <summary>Starts logging; call once the framework's bindings exist.</summary>
    public static void Start()
    {
        if (s_started)
        {
            return;
        }

        s_started = true;
        SchedulerBinding.Instance.AddTimingsCallback(Report);
    }

    private static void Report(IReadOnlyList<FrameTiming> timings)
    {
        foreach (FrameTiming timing in timings)
        {
            if (!s_firstReported)
            {
                s_firstReported = true;
                // Process start in wall-clock microseconds, from the elapsed-realtime clock Android keeps.
                long nowWall = (DateTime.UtcNow - DateTime.UnixEpoch).Ticks / 10;
                long sinceStartMs = SystemClock.ElapsedRealtime() - AndroidProcess.StartElapsedRealtime;
                long processStartWall = nowWall - (sinceStartMs * 1000);
                long firstFrameWall = timing.TimestampInMicroseconds(FramePhase.RasterFinishWallTime);
                Log.Info(Tag, $"first-frame ms={(firstFrameWall - processStartWall) / 1000.0:F0}");
            }

            Log.Info(Tag, $"frame n={timing.FrameNumber} build={timing.BuildDuration.TotalMilliseconds:F2} "
                          + $"raster={timing.RasterDuration.TotalMilliseconds:F2} "
                          + $"total={timing.TotalSpan.TotalMilliseconds:F2}");
        }
    }
}
