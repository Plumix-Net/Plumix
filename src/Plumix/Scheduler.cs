using System.Diagnostics;
using Avalonia.Threading;

// Dart parity source (reference): flutter/packages/flutter/lib/src/scheduler/binding.dart (approximate)

namespace Plumix;

/// <summary>The phase that the frame pipeline is currently in.</summary>
/// <remarks>Dart parity source: flutter/packages/flutter/lib/src/scheduler/binding.dart (SchedulerPhase).</remarks>
public enum SchedulerPhase
{
    /// <summary>No frame is being processed.</summary>
    Idle,

    /// <summary>Animation tickers are being ticked.</summary>
    TransientCallbacks,

    /// <summary>The frame is being built, laid out and painted.</summary>
    PersistentCallbacks,

    /// <summary>Post-frame callbacks are running.</summary>
    PostFrameCallbacks,
}

public static class Scheduler
{
    private static readonly List<Ticker> _active = [];
    private static readonly List<Action<TimeSpan>> _persistentFrameCallbacks = [];
    private static readonly Queue<Action<TimeSpan>> _postFrameCallbacks = [];
    private static readonly Stopwatch _sw = Stopwatch.StartNew();

    private static DispatcherTimer? _timer;
    private static bool _running;
    private static bool _hasScheduledFrame;
    private static bool _handlingFrame;

    public static event Action<TimeSpan>? BeginFrame;
    public static event Action<TimeSpan>? DrawFrame;

    public static double CurrentSeconds => _sw.Elapsed.TotalSeconds;
    public static bool HasScheduledFrame => _hasScheduledFrame;

    /// <summary>The phase the frame pipeline is currently in.</summary>
    public static SchedulerPhase Phase { get; private set; } = SchedulerPhase.Idle;

    public static void ScheduleFrame()
    {
        _hasScheduledFrame = true;
        EnsureRunning();
    }

    public static void AddPostFrameCallback(Action<TimeSpan> callback)
    {
        _postFrameCallbacks.Enqueue(callback);
        ScheduleFrame();
    }

    public static void AddPersistentFrameCallback(Action<TimeSpan> callback)
    {
        if (_persistentFrameCallbacks.Contains(callback))
        {
            return;
        }

        _persistentFrameCallbacks.Add(callback);
    }

    public static void RemovePersistentFrameCallback(Action<TimeSpan> callback)
    {
        _persistentFrameCallbacks.Remove(callback);
    }

    // Flutter runs every build inside the persistent-callback phase of a frame. Plumix hosts do the
    // same, but tests drive `BuildOwner.FlushBuild` directly, so a build scope reports the build
    // phase on its own when no frame is running.
    internal static IDisposable BuildScope()
    {
        return new BuildScopeToken();
    }

    internal static void Add(Ticker ticker)
    {
        if (!_active.Contains(ticker))
        {
            _active.Add(ticker);
        }

        if (ticker.IsTicking)
        {
            ScheduleFrame();
        }
    }

    internal static void Remove(Ticker ticker)
    {
        _active.Remove(ticker);

        if (!HasTickingTickers() && !_hasScheduledFrame && _postFrameCallbacks.Count == 0)
        {
            Stop();
        }
    }

    internal static void TickerSchedulingChanged()
    {
        if (HasTickingTickers())
        {
            ScheduleFrame();
            return;
        }

        if (!_hasScheduledFrame && _postFrameCallbacks.Count == 0)
        {
            Stop();
        }
    }

    internal static void PumpFrameForTests(TimeSpan? timestamp = null)
    {
        if (!_hasScheduledFrame && !HasTickingTickers() && _postFrameCallbacks.Count == 0)
        {
            return;
        }

        if (!_hasScheduledFrame && HasTickingTickers())
        {
            _hasScheduledFrame = true;
        }

        if (_hasScheduledFrame)
        {
            HandleFrame(timestamp?.TotalSeconds ?? CurrentSeconds);
        }
    }

    internal static void ResetForTests()
    {
        Stop();
        _active.Clear();
        _persistentFrameCallbacks.Clear();
        _postFrameCallbacks.Clear();
        _hasScheduledFrame = false;
        _handlingFrame = false;
        Phase = SchedulerPhase.Idle;
        BeginFrame = null;
        DrawFrame = null;
    }

    private static void EnsureRunning()
    {
        if (_running)
        {
            return;
        }

        _timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(16),
            DispatcherPriority.Render,
            (_, _) => Tick());

        _timer.Start();
        _running = true;
    }

    private static void Stop()
    {
        if (!_running)
        {
            return;
        }

        _timer?.Stop();
        _timer = null;
        _running = false;
    }

    private static void Tick()
    {
        if (!_hasScheduledFrame && !HasTickingTickers() && _postFrameCallbacks.Count == 0)
        {
            Stop();
            return;
        }

        if (_handlingFrame)
        {
            return;
        }

        if (!_hasScheduledFrame && HasTickingTickers())
        {
            _hasScheduledFrame = true;
        }

        if (_hasScheduledFrame)
        {
            HandleFrame(CurrentSeconds);
        }
    }

    private static void HandleFrame(double nowSeconds)
    {
        _handlingFrame = true;
        _hasScheduledFrame = false;

        var timestamp = TimeSpan.FromSeconds(nowSeconds);

        try
        {
            Phase = SchedulerPhase.TransientCallbacks;
            TickActiveTickers(nowSeconds);
            Phase = SchedulerPhase.PersistentCallbacks;
            BeginFrame?.Invoke(timestamp);
            RunPersistentFrameCallbacks(timestamp);
            DrawFrame?.Invoke(timestamp);
            Phase = SchedulerPhase.PostFrameCallbacks;
            RunPostFrameCallbacks(timestamp);
        }
        finally
        {
            Phase = SchedulerPhase.Idle;
            _handlingFrame = false;
        }

        if (HasTickingTickers())
        {
            _hasScheduledFrame = true;
        }

        if (!_hasScheduledFrame && !HasTickingTickers() && _postFrameCallbacks.Count == 0)
        {
            Stop();
        }
    }

    private static void TickActiveTickers(double nowSeconds)
    {
        var snapshot = _active.ToArray();
        foreach (var ticker in snapshot)
        {
            if (ticker.IsTicking)
            {
                ticker.InternalTick(nowSeconds);
            }
        }
    }

    private static bool HasTickingTickers()
    {
        return _active.Any(ticker => ticker.IsTicking);
    }

    private static void RunPostFrameCallbacks(TimeSpan timestamp)
    {
        if (_postFrameCallbacks.Count == 0)
        {
            return;
        }

        int count = _postFrameCallbacks.Count;
        for (int index = 0; index < count; index++)
        {
            var callback = _postFrameCallbacks.Dequeue();
            callback(timestamp);
        }

        if (_postFrameCallbacks.Count > 0)
        {
            _hasScheduledFrame = true;
        }
    }

    private sealed class BuildScopeToken : IDisposable
    {
        private readonly SchedulerPhase _previousPhase = Phase;

        public BuildScopeToken()
        {
            if (Phase == SchedulerPhase.Idle)
            {
                Phase = SchedulerPhase.PersistentCallbacks;
            }
        }

        public void Dispose()
        {
            Phase = _previousPhase;
        }
    }

    private static void RunPersistentFrameCallbacks(TimeSpan timestamp)
    {
        if (_persistentFrameCallbacks.Count == 0)
        {
            return;
        }

        var snapshot = _persistentFrameCallbacks.ToArray();
        foreach (var callback in snapshot)
        {
            callback(timestamp);
        }
    }
}
