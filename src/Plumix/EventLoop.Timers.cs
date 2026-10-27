using Avalonia.Threading;
using Plumix.UI;

// C#-only infrastructure; no Dart parity source. A Dart isolate's event loop owns the timer queue
// behind dart:async's `Timer`, and flutter_test runs every widget test inside `FakeAsync`, whose
// clock only moves when the test elapses it. This file is both: the queue `Timer` joins, with
// deadlines on `FrameClock`'s monotonic clock and one Avalonia dispatcher timer armed for the
// earliest, and the fake-time mode the test harness switches it to.

namespace Plumix;

public static partial class EventLoop
{
    private static readonly TimerQueue RealTimerQueue = new(fake: false);

    private static TimerQueue? _fakeTimerQueue;
    private static int _fakeTimeDepth;

    /// <summary>The queue a new <see cref="Timer"/> joins: the fake one while a test elapses time.</summary>
    internal static TimerQueue CurrentTimerQueue => _fakeTimerQueue ?? RealTimerQueue;

    /// <summary>
    /// Runs every <see cref="Timer"/> created until the returned scope ends on a fake clock that only
    /// <see cref="ElapseFakeTime"/> moves, the way flutter_test's <c>FakeAsync</c> zone does.
    /// </summary>
    /// <remarks>
    /// Nested scopes share the outermost one's clock. When the outermost scope ends, the timers still
    /// pending on the fake clock are dropped and stop being active, like a finished <c>FakeAsync</c>
    /// zone's. Timers created before the scope stay on the real clock.
    /// </remarks>
    internal static FakeTimeScope UseFakeTime() => new();

    /// <summary>
    /// <c>FakeAsync.elapse</c>: advances the fake clock by <paramref name="duration"/>, firing the
    /// timers that fall due in deadline order (creation order on ties), each with the clock at its
    /// deadline, and draining the microtasks before and after each one.
    /// </summary>
    internal static void ElapseFakeTime(TimeSpan duration)
    {
        TimerQueue queue = _fakeTimerQueue
            ?? throw new InvalidOperationException("Fake time is not in use; see EventLoop.UseFakeTime.");
        queue.Elapse(duration);
    }

    /// <summary>The number of active timers on the fake clock: <c>FakeAsync.pendingTimers.length</c>.</summary>
    internal static int PendingFakeTimerCount => _fakeTimerQueue?.Count ?? 0;

    /// <summary>The scope <see cref="UseFakeTime"/> returns.</summary>
    internal readonly struct FakeTimeScope : IDisposable
    {
        private readonly bool _active;

        public FakeTimeScope()
        {
            _active = true;
            if (_fakeTimeDepth++ == 0)
            {
                _fakeTimerQueue = new TimerQueue(fake: true);
            }
        }

        public void Dispose()
        {
            if (!_active || _fakeTimeDepth == 0)
            {
                return;
            }

            if (--_fakeTimeDepth == 0)
            {
                TimerQueue queue = _fakeTimerQueue!;
                _fakeTimerQueue = null;
                queue.Clear();
            }
        }
    }
}

/// <summary>
/// The pending <see cref="Timer"/>s of one clock, ordered by deadline and then creation.
/// </summary>
internal sealed class TimerQueue
{
    // A periodic timer of zero duration would otherwise keep a fake elapse from ever returning.
    private const int MaxFakeFiresPerElapse = 100_000;

    private static readonly Comparer<Timer> DeadlineOrder = Comparer<Timer>.Create(static (a, b) =>
    {
        int byDeadline = a.Deadline.CompareTo(b.Deadline);
        return byDeadline != 0 ? byDeadline : a.Sequence.CompareTo(b.Sequence);
    });

    private readonly SortedSet<Timer> _timers = new(DeadlineOrder);

    // A timer may be created or cancelled from a foreign thread; callbacks always run on the
    // framework thread, outside the lock.
    private readonly object _sync = new();
    private readonly bool _fake;
    private TimeSpan _fakeNow;
    private DispatcherTimer? _wakeUp;
    private TimeSpan? _wakeUpDeadline;

    public TimerQueue(bool fake)
    {
        _fake = fake;
    }

    /// <summary>The clock deadlines are measured on.</summary>
    public TimeSpan Now => _fake ? _fakeNow : FrameClock.Elapsed;

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _timers.Count;
            }
        }
    }

    public void Add(Timer timer)
    {
        lock (_sync)
        {
            timer.Deadline = Now + timer.Duration;
            timer.Queue = this;
            _timers.Add(timer);
        }

        if (!_fake)
        {
            ArmWakeUp();
        }
    }

    public void Remove(Timer timer)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(timer.Queue, this))
            {
                return;
            }

            _timers.Remove(timer);
            timer.Queue = null;
        }
    }

    /// <summary>Drops every pending timer; none of them stays active.</summary>
    public void Clear()
    {
        lock (_sync)
        {
            foreach (Timer timer in _timers)
            {
                timer.Queue = null;
            }

            _timers.Clear();
        }
    }

    /// <summary>See <see cref="EventLoop.ElapseFakeTime"/>.</summary>
    public void Elapse(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "May not be negative.");
        }

        TimeSpan end = _fakeNow + duration;
        EventLoop.FlushMicrotasks();
        int fires = 0;
        while (TakeDue(end) is { } timer)
        {
            if (++fires > MaxFakeFiresPerElapse)
            {
                throw new InvalidOperationException("A timer keeps firing without letting fake time advance.");
            }

            if (timer.Deadline > _fakeNow)
            {
                _fakeNow = timer.Deadline;
            }

            Fire(timer);
        }

        if (end > _fakeNow)
        {
            _fakeNow = end;
        }
    }

    // Removes and returns the earliest timer due by `limit`; a one-shot timer stops being active here.
    private Timer? TakeDue(TimeSpan limit)
    {
        lock (_sync)
        {
            if (_timers.Count == 0 || _timers.Min!.Deadline > limit)
            {
                return null;
            }

            Timer timer = _timers.Min!;
            _timers.Remove(timer);
            if (!timer.IsPeriodic)
            {
                timer.Queue = null;
            }

            return timer;
        }
    }

    private bool TryTake(Timer timer)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(timer.Queue, this) || !_timers.Remove(timer))
            {
                return false;
            }

            if (!timer.IsPeriodic)
            {
                timer.Queue = null;
            }

            return true;
        }
    }

    // Runs a timer just taken off the queue. A periodic timer goes back on the queue first, so its
    // callback can cancel it, with the deadline of the next whole period still ahead of the clock.
    private void Fire(Timer timer)
    {
        timer.Tick += 1;
        if (timer.IsPeriodic)
        {
            TimeSpan now = Now;
            if (timer.Duration > TimeSpan.Zero)
            {
                timer.Deadline += timer.Duration;
                while (timer.Deadline < now)
                {
                    timer.Tick += 1;
                    timer.Deadline += timer.Duration;
                }
            }
            else if (!_fake)
            {
                // Once per turn of the event loop, never twice in one wake-up.
                timer.Deadline = now + TimeSpan.FromTicks(1);
            }

            lock (_sync)
            {
                if (!ReferenceEquals(timer.Queue, this))
                {
                    return;
                }

                _timers.Add(timer);
            }
        }

        using (EventLoop.EnterFrameworkThread())
        {
            timer.Invoke();
        }

        EventLoop.FlushMicrotasks();
    }

    // The real clock: one dispatcher timer, armed for the earliest deadline.
    private void ArmWakeUp()
    {
        lock (_sync)
        {
            if (_timers.Count == 0)
            {
                _wakeUp?.Stop();
                _wakeUpDeadline = null;
                return;
            }

            TimeSpan deadline = _timers.Min!.Deadline;
            if (_wakeUpDeadline is { } armed && armed <= deadline)
            {
                return;
            }

            TimeSpan delay = deadline - Now;
            if (delay < TimeSpan.FromMilliseconds(1))
            {
                delay = TimeSpan.Zero;
            }

            _wakeUpDeadline = deadline;
            // A timer armed before the host claimed the UI thread belongs to another thread's dispatcher.
            Dispatcher uiThread = Dispatcher.UIThread;
            if (_wakeUp is not null && !ReferenceEquals(_wakeUp.Dispatcher, uiThread))
            {
                _wakeUp.Stop();
                _wakeUp = null;
            }

            if (_wakeUp is null)
            {
                _wakeUp = new DispatcherTimer(PlatformDispatcher.EventLoopTaskPriority, uiThread);
                _wakeUp.Tick += (_, _) => WakeUp();
            }

            _wakeUp.Stop();
            _wakeUp.Interval = delay;
            _wakeUp.Start();
        }
    }

    private void WakeUp()
    {
        Timer[] due;
        TimeSpan now = Now;
        lock (_sync)
        {
            _wakeUp?.Stop();
            _wakeUpDeadline = null;
            due = _timers.TakeWhile(timer => timer.Deadline <= now).ToArray();
        }

        try
        {
            // Each timer due at the wake-up fires once, in deadline order; one a callback cancels is
            // skipped, and one a callback creates waits for the next wake-up.
            foreach (Timer timer in due)
            {
                if (TryTake(timer))
                {
                    Fire(timer);
                }
            }
        }
        finally
        {
            ArmWakeUp();
        }
    }
}
