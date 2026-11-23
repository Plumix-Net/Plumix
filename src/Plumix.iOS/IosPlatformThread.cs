using CoreAnimation;
using CoreFoundation;
using Foundation;

namespace Plumix.Impeller.iOS;

// C#-only host infrastructure; no Dart parity source. The core's UI-thread seam (PlatformThread.cs) on the iOS
// main thread, as the engine's platform task runner is the main run loop: tasks and timers go through the main
// dispatch queue, frames through a CADisplayLink (the engine's VsyncWaiterIOS).

/// <summary>The iOS main thread as the framework's UI thread.</summary>
internal sealed class IosPlatformThread : IPlatformThread
{
    internal static bool IsMainThread => NSThread.IsMain;

    public void Post(Action action, PlatformTaskPriority priority) => DispatchQueue.MainQueue.DispatchAsync(action);

    public IPlatformTimer CreateTimer(PlatformTaskPriority priority, Action tick) =>
        priority == PlatformTaskPriority.Frame ? new VsyncTimer(tick) : new DispatchTimer(tick);

    public void RunOnce(Action callback, TimeSpan delay, PlatformTaskPriority priority) =>
        DispatchQueue.MainQueue.DispatchAfter(
            new DispatchTime(DispatchTime.Now, (long)(Math.Max(0, delay.TotalMilliseconds) * 1_000_000)),
            callback);

    // A repeating timer re-armed after each tick, so a stopped or restarted timer drops its stale ticks.
    private sealed class DispatchTimer(Action tick) : IPlatformTimer
    {
        private int _generation;
        private bool _running;

        public TimeSpan Interval { get; set; }

        public bool IsOnCurrentUIThread => IsMainThread;

        public void Start()
        {
            _running = true;
            Schedule(++_generation);
        }

        public void Stop()
        {
            _running = false;
            _generation++;
        }

        private void Schedule(int generation) => DispatchQueue.MainQueue.DispatchAfter(
            new DispatchTime(DispatchTime.Now, (long)(Math.Max(0, Interval.TotalMilliseconds) * 1_000_000)),
            () => Fire(generation));

        private void Fire(int generation)
        {
            if (!_running || generation != _generation)
            {
                return;
            }

            tick();
            if (_running && generation == _generation)
            {
                Schedule(generation);
            }
        }
    }

    // VsyncWaiterIOS: a display link on the main run loop, paused while the frame clock has nothing to draw.
    private sealed class VsyncTimer : IPlatformTimer
    {
        private readonly Action _tick;
        private CADisplayLink? _link;

        public VsyncTimer(Action tick) => _tick = tick;

        public TimeSpan Interval { get; set; }

        public bool IsOnCurrentUIThread => IsMainThread;

        public void Start()
        {
            if (!IsMainThread)
            {
                DispatchQueue.MainQueue.DispatchAsync(Start);
                return;
            }

            if (_link is null)
            {
                _link = CADisplayLink.Create(_tick);
                _link.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
            }

            _link.Paused = false;
        }

        public void Stop()
        {
            if (!IsMainThread)
            {
                DispatchQueue.MainQueue.DispatchAsync(Stop);
                return;
            }

            if (_link is not null)
            {
                _link.Paused = true;
            }
        }
    }
}
