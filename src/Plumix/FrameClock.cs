using System.Diagnostics;
using Avalonia.Threading;
using Plumix.UI;

// C#-only infrastructure; no Dart parity source. Flutter's engine answers
// `PlatformDispatcher.scheduleFrame` with a vsync that calls `onBeginFrame`, drains the microtasks
// and calls `onDrawFrame`. Plumix has no engine, so this class is that vsync source: a dispatcher
// timer that runs while a frame is requested, and the monotonic clock its timestamps come from.

namespace Plumix;

/// <summary>
/// Plumix's stand-in for the engine's vsync source: it answers
/// <see cref="PlatformDispatcher.ScheduleFrame"/> by calling <see cref="PlatformDispatcher.OnBeginFrame"/>
/// and then <see cref="PlatformDispatcher.OnDrawFrame"/> on the next timer tick.
/// </summary>
public static class FrameClock
{
    private static readonly Stopwatch _sw = Stopwatch.StartNew();

    private static DispatcherTimer? _timer;
    private static bool _frameRequested;

    /// <summary>The elapsed time of the clock frame timestamps are read from, in seconds.</summary>
    /// <remarks>
    /// The engine hands Flutter a raw vsync timestamp; a Plumix host (or test) reads this clock and
    /// passes the value to <see cref="SchedulerBinding.HandleBeginFrame"/>.
    /// </remarks>
    public static double CurrentSeconds => _sw.Elapsed.TotalSeconds;

    /// <summary>Arms the timer for one frame; called by <see cref="PlatformDispatcher.ScheduleFrame"/>.</summary>
    internal static void RequestFrame()
    {
        _frameRequested = true;
        if (_timer is not null)
        {
            return;
        }

        _timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(16),
            DispatcherPriority.Render,
            (_, _) => Tick());
        _timer.Start();
    }

    /// <summary>Stops the timer and forgets the pending request.</summary>
    internal static void ResetForTests()
    {
        _frameRequested = false;
        Stop();
    }

    private static void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    private static void Tick()
    {
        if (!_frameRequested)
        {
            Stop();
            return;
        }

        if (SchedulerBinding.Instance.IsHandlingFrame)
        {
            return;
        }

        _frameRequested = false;
        // The engine's animator: the vsync starts the frame's timings recorder, the build phase starts
        // with `onBeginFrame`, and each view's `render` ends it (`FrameTimingsRecorder`).
        FrameTimingsRecorder.RecordVsync();
        try
        {
            FrameTimingsRecorder.RecordBuildStart();
            PlatformDispatcher.Instance.OnBeginFrame?.Invoke(TimeSpan.FromSeconds(CurrentSeconds));
            EventLoop.FlushMicrotasks();
            PlatformDispatcher.Instance.OnDrawFrame?.Invoke();
        }
        finally
        {
            FrameTimingsRecorder.EndFrame();
        }
    }
}
