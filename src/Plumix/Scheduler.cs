using System.Diagnostics;
using Avalonia.Threading;

// Dart parity source (reference): flutter/packages/flutter/lib/src/scheduler/binding.dart; flutter/packages/flutter/lib/src/scheduler/scheduler.dart (approximate)

namespace Plumix;

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

    internal static void Add(Ticker ticker)
    {
        if (!_active.Contains(ticker))
        {
            _active.Add(ticker);
        }

        ScheduleFrame();
    }

    internal static void Remove(Ticker ticker)
    {
        _active.Remove(ticker);

        if (_active.Count == 0 && !_hasScheduledFrame && _postFrameCallbacks.Count == 0)
        {
            Stop();
        }
    }

    internal static void PumpFrameForTests(TimeSpan? timestamp = null)
    {
        if (!_hasScheduledFrame && _active.Count == 0 && _postFrameCallbacks.Count == 0)
        {
            return;
        }

        if (!_hasScheduledFrame && _active.Count > 0)
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
        if (!_hasScheduledFrame && _active.Count == 0 && _postFrameCallbacks.Count == 0)
        {
            Stop();
            return;
        }

        if (_handlingFrame)
        {
            return;
        }

        if (!_hasScheduledFrame && _active.Count > 0)
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
            TickActiveTickers(nowSeconds);
            BeginFrame?.Invoke(timestamp);
            RunPersistentFrameCallbacks(timestamp);
            DrawFrame?.Invoke(timestamp);
            RunPostFrameCallbacks(timestamp);
        }
        finally
        {
            _handlingFrame = false;
        }

        if (_active.Count > 0)
        {
            _hasScheduledFrame = true;
        }

        if (!_hasScheduledFrame && _active.Count == 0 && _postFrameCallbacks.Count == 0)
        {
            Stop();
        }
    }

    private static void TickActiveTickers(double nowSeconds)
    {
        var snapshot = _active.ToArray();
        foreach (var ticker in snapshot)
        {
            if (ticker.Active)
            {
                ticker.InternalTick(nowSeconds);
            }
        }
    }

    private static void RunPostFrameCallbacks(TimeSpan timestamp)
    {
        if (_postFrameCallbacks.Count == 0)
        {
            return;
        }

        var count = _postFrameCallbacks.Count;
        for (var index = 0; index < count; index++)
        {
            var callback = _postFrameCallbacks.Dequeue();
            callback(timestamp);
        }

        if (_postFrameCallbacks.Count > 0)
        {
            _hasScheduledFrame = true;
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
