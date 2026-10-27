// C#-only infrastructure; no Flutter parity source. The port of dart:async's `Timer`
// (sdk/lib/async/timer.dart), which every Flutter library imports from the language runtime. A Dart
// timer is an event of the isolate's event loop; Plumix's is an entry in `EventLoop`'s timer queue
// (EventLoop.Timers.cs), which the test harness drives the way flutter_test's `FakeAsync` does.

namespace Plumix;

/// <summary>
/// A countdown timer that runs a callback on the framework's event loop: dart:async's <c>Timer</c>.
/// </summary>
/// <remarks>
/// The callback runs on the framework thread, after the microtasks queued before it have drained, and
/// the microtasks it queues drain before the next timer fires. Deadlines are read from the event
/// loop's clock: <see cref="FrameClock"/>'s monotonic clock in an app, the fake clock a test elapses
/// in tests.
/// </remarks>
public sealed class Timer
{
    private static long _nextSequence;

    private readonly Action? _callback;
    private readonly Action<Timer>? _periodicCallback;

    /// <summary>
    /// Creates a new timer that calls <paramref name="callback"/> once, after
    /// <paramref name="duration"/>. A negative duration is treated like <see cref="TimeSpan.Zero"/>.
    /// </summary>
    /// <remarks>Dart's <c>Timer(Duration duration, void Function() callback)</c>.</remarks>
    public Timer(TimeSpan duration, Action callback)
        : this(duration, callback ?? throw new ArgumentNullException(nameof(callback)), null)
    {
    }

    private Timer(TimeSpan duration, Action? callback, Action<Timer>? periodicCallback)
    {
        _callback = callback;
        _periodicCallback = periodicCallback;
        Duration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
        Sequence = Interlocked.Increment(ref _nextSequence);
        EventLoop.CurrentTimerQueue.Add(this);
    }

    /// <summary>
    /// Creates a new repeating timer that calls <paramref name="callback"/> every
    /// <paramref name="duration"/> until it is cancelled.
    /// </summary>
    /// <remarks>
    /// Dart's <c>Timer.periodic</c>. Like the VM's timers, a periodic timer is isochronous: it fires
    /// as soon as possible after each multiple of <paramref name="duration"/> since it started, and
    /// when the event loop falls behind it skips the missed multiples, which <see cref="Tick"/>
    /// counts.
    /// </remarks>
    public static Timer Periodic(TimeSpan duration, Action<Timer> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return new Timer(duration, null, callback);
    }

    /// <summary>
    /// Runs <paramref name="callback"/> asynchronously, as soon as possible: a zero-duration timer.
    /// </summary>
    /// <remarks>Dart's <c>Timer.run</c>.</remarks>
    public static void Run(Action callback) => _ = new Timer(TimeSpan.Zero, callback);

    /// <summary>
    /// Whether the timer is still active: a one-shot timer until its callback runs or it is
    /// cancelled, a periodic timer until it is cancelled.
    /// </summary>
    public bool IsActive => Queue is not null;

    /// <summary>
    /// The number of durations preceding the most recent timer event: 0 before a one-shot timer fires
    /// and 1 after; for a periodic timer, the number of whole durations since it started at its
    /// latest callback, which can exceed the number of callbacks when some were skipped.
    /// </summary>
    public int Tick { get; internal set; }

    /// <summary>The (clamped) duration, the period of a periodic timer.</summary>
    internal TimeSpan Duration { get; }

    internal bool IsPeriodic => _periodicCallback is not null;

    /// <summary>Creation order: timers due at the same time fire in it.</summary>
    internal long Sequence { get; }

    /// <summary>Where a timer on the fake clock was created; null on the real clock.</summary>
    internal System.Diagnostics.StackTrace? CreationStackTrace { get; set; }

    /// <summary>The event-loop time at which the timer fires next.</summary>
    internal TimeSpan Deadline { get; set; }

    /// <summary>The queue the timer is pending in; null once it is no longer active.</summary>
    internal TimerQueue? Queue { get; set; }

    /// <summary>Cancels the timer. A cancelled timer never calls its callback again.</summary>
    /// <remarks>Calling it on a timer that already fired or was cancelled does nothing.</remarks>
    public void Cancel() => Queue?.Remove(this);

    internal void Invoke()
    {
        if (_periodicCallback is not null)
        {
            _periodicCallback(this);
        }
        else
        {
            _callback!();
        }
    }
}
