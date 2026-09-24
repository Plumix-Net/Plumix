// C#-only infrastructure: dart:core's `Stopwatch`. `System.Diagnostics.Stopwatch` is sealed and always
// reads the machine clock, while Flutter's `SamplingClock.stopwatch()` must be replaceable so that
// flutter_test can hand out stopwatches driven by its fake clock (`_TestSamplingClock`). This class
// keeps Dart's semantics (a new stopwatch is stopped at zero; `reset` keeps the running state) over a
// replaceable time source.

namespace Plumix.Foundation;

/// <summary>A stopwatch that measures time while it is running, with dart:core's semantics.</summary>
public class DartStopwatch
{
    private readonly Func<TimeSpan> _now;
    private TimeSpan _start;
    private TimeSpan? _stop;

    /// <summary>Creates a stopped stopwatch that reads the machine's monotonic clock.</summary>
    public DartStopwatch() : this(null)
    {
    }

    /// <summary>
    /// Creates a stopped stopwatch that reads <paramref name="now"/>, a monotonic time source, or the
    /// machine's monotonic clock when it is null.
    /// </summary>
    public DartStopwatch(Func<TimeSpan>? now)
    {
        _now = now ?? (() => System.Diagnostics.Stopwatch.GetElapsedTime(0));
        _start = TimeSpan.Zero;
        _stop = TimeSpan.Zero;
    }

    /// <summary>The time measured so far.</summary>
    public TimeSpan Elapsed => (_stop ?? _now()) - _start;

    /// <summary>The time measured so far, in whole milliseconds.</summary>
    public long ElapsedMilliseconds => (long)Elapsed.TotalMilliseconds;

    /// <summary>The time measured so far, in whole microseconds.</summary>
    public long ElapsedMicroseconds => (long)Elapsed.TotalMicroseconds;

    /// <summary>Whether the stopwatch is measuring time.</summary>
    public bool IsRunning => _stop is null;

    /// <summary>
    /// Starts the stopwatch. The time it was stopped for is not counted; starting a running
    /// stopwatch does nothing.
    /// </summary>
    public void Start()
    {
        if (_stop is { } stop)
        {
            _start += _now() - stop;
            _stop = null;
        }
    }

    /// <summary>Stops the stopwatch; stopping a stopped stopwatch does nothing.</summary>
    public void Stop()
    {
        _stop ??= _now();
    }

    /// <summary>Resets the elapsed time to zero without changing whether the stopwatch runs.</summary>
    public void Reset()
    {
        _start = _stop ?? _now();
    }
}
