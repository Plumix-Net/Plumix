// C#-only infrastructure; no Dart parity source. The engine runs the UI isolate's event loop on the
// platform's UI task runner; Plumix's EventLoop, frame clock and gesture timers post to the host's UI
// thread through this seam, so the framework names no host type (docs/product avalonia-free-core.md,
// stage 3). AvaloniaPlatformThread is the Avalonia host's implementation.

namespace Plumix;

/// <summary>How urgently work posted to the UI thread runs, highest first.</summary>
internal enum PlatformTaskPriority
{
    /// <summary>Microtasks: before anything else the thread has queued.</summary>
    Microtask,

    /// <summary>Event-loop tasks and timers: after input, before rendering work.</summary>
    Task,

    /// <summary>Frame production (the frame clock's vsync stand-in).</summary>
    Frame,
}

/// <summary>A repeating timer on the host's UI thread.</summary>
internal interface IPlatformTimer
{
    TimeSpan Interval { get; set; }

    /// <summary>Whether the timer runs on the thread the host currently treats as its UI thread.</summary>
    bool IsOnCurrentUIThread { get; }

    void Start();

    void Stop();
}

/// <summary>The host's UI thread, as the framework's event loop sees it.</summary>
internal interface IPlatformThread
{
    /// <summary>Queues <paramref name="action"/> to run on the UI thread.</summary>
    void Post(Action action, PlatformTaskPriority priority);

    /// <summary>A stopped timer that calls <paramref name="tick"/> on the UI thread every interval.</summary>
    IPlatformTimer CreateTimer(PlatformTaskPriority priority, Action tick);

    /// <summary>Runs <paramref name="callback"/> once on the UI thread after <paramref name="delay"/>.</summary>
    void RunOnce(Action callback, TimeSpan delay, PlatformTaskPriority priority);
}

/// <summary>The UI thread the framework posts to.</summary>
internal static class PlatformThread
{
    /// <summary>The host's UI thread, installed by the host package (Plumix.Avalonia's AvaloniaBackend).</summary>
    internal static IPlatformThread Current { get; set; } = MissingHost.Instance;

    // Before a host is installed, anything that needs the UI thread fails with a clear message.
    private sealed class MissingHost : IPlatformThread
    {
        internal static readonly MissingHost Instance = new();

        public void Post(Action action, PlatformTaskPriority priority) => throw NoHost();

        public IPlatformTimer CreateTimer(PlatformTaskPriority priority, Action tick) => throw NoHost();

        public void RunOnce(Action callback, TimeSpan delay, PlatformTaskPriority priority) => throw NoHost();

        private static InvalidOperationException NoHost() => new(
            "No Plumix host is running. Start the app through a host package such as Plumix.Avalonia "
            + "(PlumixApplication, WidgetHost).");
    }
}
