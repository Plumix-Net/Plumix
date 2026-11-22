using Android.OS;
using Android.Views;

namespace Plumix.Sample.Impeller.Android;

// C#-only host infrastructure; no Dart parity source. The core's UI-thread seam (Plumix/PlatformThread.cs)
// on the Android main Looper, the way the engine's platform task runner is: microtasks jump the queue,
// tasks and timers are plain Handler posts, and the frame clock's timer (the only Frame-priority user)
// ticks on Choreographer vsync callbacks instead of its 16 ms interval.

/// <summary>Runs framework work on the Android main thread.</summary>
internal sealed class AndroidPlatformThread : IPlatformThread
{
    private readonly Handler _handler = new(Looper.MainLooper!);

    internal static bool IsMainThread => Looper.MyLooper() == Looper.MainLooper;

    public void Post(Action action, PlatformTaskPriority priority)
    {
        if (priority == PlatformTaskPriority.Microtask)
        {
            _handler.PostAtFrontOfQueue(new Java.Lang.Runnable(action));
        }
        else
        {
            _handler.Post(action);
        }
    }

    public IPlatformTimer CreateTimer(PlatformTaskPriority priority, Action tick) =>
        priority == PlatformTaskPriority.Frame ? new VsyncTimer(_handler, tick) : new HandlerTimer(_handler, tick);

    public void RunOnce(Action callback, TimeSpan delay, PlatformTaskPriority priority) =>
        _handler.PostDelayed(callback, (long)Math.Max(0, delay.TotalMilliseconds));

    /// <summary>A repeating timer over Handler.postDelayed; Start restarts the countdown.</summary>
    private sealed class HandlerTimer(Handler handler, Action tick) : IPlatformTimer
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

        private void Schedule(int generation) =>
            handler.PostDelayed(() => Fire(generation), (long)Math.Max(0, Interval.TotalMilliseconds));

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

    /// <summary>A repeating timer that ticks once per Choreographer frame while started.</summary>
    private sealed class VsyncTimer(Handler handler, Action tick) : Java.Lang.Object, Choreographer.IFrameCallback,
        IPlatformTimer
    {
        private bool _running;
        private bool _posted;

        public TimeSpan Interval { get; set; }

        public bool IsOnCurrentUIThread => IsMainThread;

        public void Start()
        {
            _running = true;
            if (IsMainThread)
            {
                PostCallback();
            }
            else
            {
                handler.Post(PostCallback);
            }
        }

        public void Stop() => _running = false;

        public void DoFrame(long frameTimeNanos)
        {
            _posted = false;
            if (!_running)
            {
                return;
            }

            tick();
            if (_running)
            {
                PostCallback();
            }
        }

        private void PostCallback()
        {
            if (_running && !_posted)
            {
                _posted = true;
                Choreographer.Instance!.PostFrameCallback(this);
            }
        }
    }
}
