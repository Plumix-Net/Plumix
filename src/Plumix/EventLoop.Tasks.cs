using Avalonia.Threading;
using Plumix.UI;

// C#-only infrastructure; no Dart parity source. The engine hands work it finished off the UI thread
// (an image decode, say) back to Dart by posting a task to the UI task runner
// (`fml::TaskRunner::PostTask`): the Dart callback runs on a later turn of the isolate's event loop,
// with the microtasks drained after it. flutter_test's `FakeAsync` never runs those turns; a test
// lets them run through `WidgetTester.runAsync`.

namespace Plumix;

public static partial class EventLoop
{
    private static readonly Queue<Action> _tasks = [];

    /// <summary>The number of engine tasks posted by <see cref="PostTask"/> and not yet run.</summary>
    internal static int PendingTaskCount
    {
        get
        {
            lock (MicrotaskSync)
            {
                return _tasks.Count;
            }
        }
    }

    /// <summary>
    /// Runs <paramref name="callback"/> on a later turn of the event loop, the way the engine posts the
    /// completion of work it did off the framework thread to the UI task runner.
    /// </summary>
    /// <remarks>
    /// The task is not a <see cref="Timer"/>, so a test's fake clock never runs it: like an engine task
    /// under flutter_test's <c>FakeAsync</c>, it waits for the real event loop, which a test turns
    /// with <see cref="RunPendingTasks"/>.
    /// </remarks>
    internal static void PostTask(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        lock (MicrotaskSync)
        {
            _tasks.Enqueue(callback);
        }

        Dispatcher.UIThread.Post(() => RunNextTask(), PlatformDispatcher.EventLoopTaskPriority);
    }

    /// <summary>
    /// Turns the real event loop until no task posted by <see cref="PostTask"/> is left, draining the
    /// microtasks after each one: what flutter_test's <c>runAsync</c> lets the engine do.
    /// </summary>
    internal static void RunPendingTasks()
    {
        while (RunNextTask())
        {
        }
    }

    private static bool RunNextTask()
    {
        using FrameworkThreadScope scope = EnterFrameworkThread();
        Action callback;
        lock (MicrotaskSync)
        {
            if (_tasks.Count == 0)
            {
                return false;
            }

            callback = _tasks.Dequeue();
        }

        try
        {
            callback();
        }
        finally
        {
            FlushMicrotasks();
        }

        return true;
    }
}
