using Avalonia.Threading;

// C#-only host infrastructure; no Dart parity source. Avalonia's UI-thread dispatcher behind
// IPlatformThread (PlatformThread.cs).

namespace Plumix;

/// <summary>The Avalonia dispatcher as the framework's UI thread.</summary>
internal sealed class AvaloniaPlatformThread : IPlatformThread
{
    internal static readonly AvaloniaPlatformThread Instance = new();

    private AvaloniaPlatformThread()
    {
    }

    // Event-loop tasks (and the wake-up of the Timer queue) run below the Send priority microtasks drain
    // at, which keeps the microtask-before-next-turn ordering, and above Input. A Dart timer is an
    // ordinary event-loop task, never idle work: Avalonia runs jobs at or below Input only when the
    // platform reports no pending input, and Android's dispatcher runs them from the looper's idle
    // handler, which never fires while an animation keeps the main thread busy. At background priority
    // the warm-up frame's draw half never ran behind an indeterminate progress indicator, and every
    // vsync frame was dropped while that warm-up frame was pending.
    internal static DispatcherPriority ToPriority(PlatformTaskPriority priority) => priority switch
    {
        PlatformTaskPriority.Microtask => DispatcherPriority.Send,
        PlatformTaskPriority.Frame => DispatcherPriority.Render,
        _ => DispatcherPriority.Default,
    };

    public void Post(Action action, PlatformTaskPriority priority) =>
        Dispatcher.UIThread.Post(action, ToPriority(priority));

    public IPlatformTimer CreateTimer(PlatformTaskPriority priority, Action tick) =>
        new Timer(new DispatcherTimer(ToPriority(priority), Dispatcher.UIThread), tick);

    public void RunOnce(Action callback, TimeSpan delay, PlatformTaskPriority priority) =>
        DispatcherTimer.RunOnce(callback, delay, ToPriority(priority));

    private sealed class Timer : IPlatformTimer
    {
        private readonly DispatcherTimer _timer;

        internal Timer(DispatcherTimer timer, Action tick)
        {
            _timer = timer;
            _timer.Tick += (_, _) => tick();
        }

        public TimeSpan Interval
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        // A timer armed before the host claimed the UI thread belongs to another thread's dispatcher.
        public bool IsOnCurrentUIThread => ReferenceEquals(_timer.Dispatcher, Dispatcher.UIThread);

        public void Start() => _timer.Start();

        public void Stop() => _timer.Stop();
    }
}
