using System.Runtime.CompilerServices;

// Dart parity source: flutter/packages/flutter/lib/src/scheduler/ticker.dart

namespace Plumix;

/// <summary>
/// Signature for the callback passed to the <see cref="Ticker"/> class's constructor. The argument is
/// the time elapsed from the frame timestamp when the ticker was last started to the current frame
/// timestamp.
/// </summary>
public delegate void TickerCallback(TimeSpan elapsed);

/// <summary>An interface implemented by classes that can vend <see cref="Ticker"/> objects.</summary>
public interface ITickerProvider
{
    /// <summary>Creates a ticker with the given callback.</summary>
    Ticker CreateTicker(TickerCallback onTick);
}

/// <summary>Calls its callback once per animation frame, when enabled.</summary>
public class Ticker : IDisposable
{
    private readonly TickerCallback _onTick;
    private TickerFuture? _future;
    private TimeSpan? _startTime;
    private bool _muted;
    private bool _scheduled;
    private bool _disposed;

    /// <summary>
    /// Creates a ticker that will call the provided callback once per frame while running. An optional
    /// label can be provided for debugging purposes.
    /// </summary>
    public Ticker(TickerCallback onTick, string? debugLabel = null)
    {
        _onTick = onTick ?? throw new ArgumentNullException(nameof(onTick));
        DebugLabel = debugLabel;
    }

    /// <summary>An optional label provided for debugging purposes.</summary>
    public string? DebugLabel { get; }

    /// <summary>
    /// If true, this ticker requests frames using <see cref="Scheduler.ScheduleForcedFrame"/> instead
    /// of <see cref="Scheduler.ScheduleFrame"/>.
    /// </summary>
    public bool ForceFrames { get; set; }

    /// <summary>Whether this ticker has been silenced.</summary>
    /// <remarks>
    /// While silenced, a ticker's clock can still run, but the callback will not be called. By
    /// convention this property is controlled by the object that created the ticker.
    /// </remarks>
    public bool Muted
    {
        get => _muted;
        set
        {
            if (value == _muted)
            {
                return;
            }

            _muted = value;
            if (value)
            {
                UnscheduleTick();
            }
            else if (ShouldScheduleTick)
            {
                ScheduleTick();
            }
        }
    }

    /// <summary>Whether this ticker has scheduled a call to call its callback on the next frame.</summary>
    public bool IsTicking
    {
        get
        {
            if (_future is null)
            {
                return false;
            }

            if (Muted)
            {
                return false;
            }

            if (Scheduler.FramesEnabled)
            {
                return true;
            }

            // For example, we might be in a warm-up frame or forced frame.
            return Scheduler.Phase != SchedulerPhase.Idle;
        }
    }

    /// <summary>
    /// Whether time is elapsing for this ticker. Becomes true when <see cref="Start"/> is called and
    /// false when <see cref="Stop"/> is called.
    /// </summary>
    public bool IsActive => _future is not null;

    /// <summary>Whether this ticker has already scheduled a frame callback.</summary>
    protected bool Scheduled => _scheduled;

    /// <summary>Whether a tick should be scheduled. If this is true, <see cref="ScheduleTick"/> succeeds.</summary>
    protected bool ShouldScheduleTick => !Muted && IsActive && !Scheduled;

    internal bool IsTickScheduled => _scheduled;

    /// <summary>
    /// Starts the clock for this ticker. If the ticker is not <see cref="Muted"/>, this also starts
    /// calling the ticker's callback once per animation frame.
    /// </summary>
    /// <returns>
    /// A future that resolves once the ticker <see cref="Stop"/>s ticking. If the ticker is disposed,
    /// the future does not resolve; <see cref="TickerFuture.OrCancel"/> faults instead.
    /// </returns>
    public TickerFuture Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsActive)
        {
            throw new InvalidOperationException(
                "A ticker was started twice. A ticker that is already active cannot be started again "
                + $"without first stopping it. The affected ticker was {this}.");
        }

        _future = new TickerFuture();
        if (ShouldScheduleTick)
        {
            ScheduleTick();
        }

        if (Scheduler.IsHandlingFrame
            && Scheduler.Phase > SchedulerPhase.Idle
            && Scheduler.Phase < SchedulerPhase.PostFrameCallbacks)
        {
            _startTime = Scheduler.CurrentFrameTimeStamp;
        }

        return _future;
    }

    /// <summary>Stops calling this ticker's callback.</summary>
    /// <param name="canceled">
    /// When false (the default) the future returned by <see cref="Start"/> resolves. When true it does
    /// not, and <see cref="TickerFuture.OrCancel"/> faults with a <see cref="TickerCanceled"/>.
    /// </param>
    public void Stop(bool canceled = false)
    {
        if (!IsActive)
        {
            return;
        }

        // The future is taken into a local so that IsTicking is false when it is actually completed.
        TickerFuture localFuture = _future!;
        _future = null;
        _startTime = null;

        UnscheduleTick();
        if (canceled)
        {
            localFuture.Cancel(this);
        }
        else
        {
            localFuture.Complete();
        }
    }

    /// <summary>Schedules a tick for the next frame. Only call this when <see cref="ShouldScheduleTick"/>.</summary>
    protected virtual void ScheduleTick(bool rescheduling = false)
    {
        if (ForceFrames)
        {
            Scheduler.ScheduleForcedFrame();
        }
        else
        {
            Scheduler.ScheduleFrame();
        }

        _scheduled = true;
        Scheduler.AddTicker(this);
    }

    /// <summary>Cancels the frame callback that was requested by <see cref="ScheduleTick"/>, if any.</summary>
    protected virtual void UnscheduleTick()
    {
        if (!_scheduled)
        {
            return;
        }

        _scheduled = false;
        Scheduler.RemoveTicker(this);
    }

    /// <summary>
    /// Makes this ticker take the state of another ticker, and disposes the other ticker. This
    /// maintains the identity of the <see cref="TickerFuture"/> returned by the original ticker's
    /// <see cref="Start"/> when that ticker is active.
    /// </summary>
    public void AbsorbTicker(Ticker originalTicker)
    {
        ArgumentNullException.ThrowIfNull(originalTicker);
        if (IsActive)
        {
            throw new InvalidOperationException("A ticker can only absorb another ticker while inactive.");
        }

        if (originalTicker._future is not null)
        {
            _future = originalTicker._future;
            _startTime = originalTicker._startTime;
            if (ShouldScheduleTick)
            {
                ScheduleTick();
            }

            // So that it does not get canceled when the original ticker is disposed.
            originalTicker._future = null;
            originalTicker.UnscheduleTick();
        }

        originalTicker.Dispose();
    }

    /// <summary>
    /// Releases the resources used by this object. It is legal to call this while the ticker is
    /// active, in which case the future returned by <see cref="Start"/> does not resolve and
    /// <see cref="TickerFuture.OrCancel"/> faults with a <see cref="TickerCanceled"/>.
    /// </summary>
    public virtual void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_future is not null)
        {
            TickerFuture localFuture = _future;
            _future = null;
            UnscheduleTick();
            localFuture.Cancel(this);
        }

        GC.SuppressFinalize(this);
    }

    public override string ToString() => $"{GetType().Name}({DebugLabel ?? string.Empty})";

    internal void InternalTick(TimeSpan timeStamp)
    {
        _scheduled = false;

        _startTime ??= timeStamp;
        _onTick(timeStamp - _startTime.Value);

        // The callback may have scheduled another tick already, for example by calling stop then start.
        if (ShouldScheduleTick)
        {
            ScheduleTick(rescheduling: true);
        }
    }
}

/// <summary>Dart parity source: the private <c>_WidgetTicker</c> of `widgets/ticker_provider.dart`.</summary>
internal sealed class WidgetTicker(TickerCallback onTick, Action<Ticker> onDisposed, string? debugLabel = null)
    : Ticker(onTick, debugLabel)
{
    public override void Dispose()
    {
        onDisposed(this);
        base.Dispose();
    }
}

/// <summary>An object representing an ongoing <see cref="Ticker"/> sequence.</summary>
/// <remarks>
/// Completes successfully when the ticker is stopped with <c>canceled: false</c>. If the ticker is
/// disposed without being stopped, or stopped with <c>canceled: true</c>, the primary future never
/// completes, exactly as in Flutter; <see cref="OrCancel"/> faults instead.
/// </remarks>
public sealed class TickerFuture
{
    private readonly TaskCompletionSource _primaryCompleter = new();
    private TaskCompletionSource? _secondaryCompleter;
    private List<Action>? _completionCallbacks;
    private List<Action>? _completeOnlyCallbacks;

    // null means unresolved, true means complete, false means canceled.
    private bool? _completed;

    internal TickerFuture()
    {
    }

    /// <summary>
    /// Creates a <see cref="TickerFuture"/> that represents an already-complete ticker sequence, for
    /// objects that normally defer to a ticker but can skip it for a zero-duration animation.
    /// </summary>
    public static TickerFuture Completed()
    {
        var future = new TickerFuture();
        future.Complete();
        return future;
    }

    /// <summary>The underlying task, which resolves only when the ticker sequence completes.</summary>
    public Task Task => _primaryCompleter.Task;

    /// <summary>
    /// A task that resolves when this future resolves, and faults with <see cref="TickerCanceled"/>
    /// when the ticker is canceled.
    /// </summary>
    /// <remarks>
    /// If this property is never accessed then canceling the ticker throws no exceptions. Once it is
    /// accessed, a canceled ticker faults the returned task.
    /// </remarks>
    public Task OrCancel
    {
        get
        {
            if (_secondaryCompleter is null)
            {
                _secondaryCompleter = new TaskCompletionSource();
                if (_completed == true)
                {
                    _secondaryCompleter.TrySetResult();
                }
                else if (_completed == false)
                {
                    _secondaryCompleter.TrySetException(new TickerCanceled());
                }
            }

            return _secondaryCompleter.Task;
        }
    }

    /// <summary>
    /// Calls <paramref name="callback"/> either when this future resolves or when the ticker is
    /// canceled.
    /// </summary>
    /// <remarks>
    /// Calling this registers an exception handler for <see cref="OrCancel"/>, so canceling the ticker
    /// does not surface an unobserved task exception. Like Dart's <c>then</c>, the callback runs in a
    /// microtask rather than inside the tick that resolved the future.
    /// </remarks>
    public void WhenCompleteOrCancel(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        // Materializing `orCancel` here is what observes a cancellation, exactly as Dart's onError arm
        // does; without it a canceled ticker would surface an unobserved task exception.
        ObserveOrCancel();
        if (_completed is not null)
        {
            Scheduler.ScheduleMicrotask(callback);
            return;
        }

        (_completionCallbacks ??= []).Add(callback);
    }

    /// <summary>
    /// Calls <paramref name="callback"/> only when this future resolves, matching Dart code that
    /// chains <c>whenComplete</c> on the <c>TickerFuture</c> itself: a canceled ticker never
    /// resolves the primary future, so the callback never runs for it.
    /// </summary>
    public void WhenComplete(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (_completed == true)
        {
            Scheduler.ScheduleMicrotask(callback);
            return;
        }

        if (_completed == false)
        {
            return;
        }

        (_completeOnlyCallbacks ??= []).Add(callback);
    }

    /// <summary>
    /// Lets a <see cref="TickerFuture"/> be awaited the way Dart awaits the <c>Future</c> it
    /// implements.
    /// </summary>
    public TaskAwaiter GetAwaiter() => Task.GetAwaiter();

    public override string ToString()
    {
        string state = _completed switch
        {
            null => "active",
            true => "complete",
            _ => "canceled",
        };
        return $"{nameof(TickerFuture)}({state})";
    }

    internal void Complete()
    {
        _completed = true;
        _primaryCompleter.TrySetResult();
        _secondaryCompleter?.TrySetResult();
        FlushCompletionCallbacks();
        FlushCompleteOnlyCallbacks();
    }

    internal void Cancel(Ticker ticker)
    {
        _completed = false;
        _secondaryCompleter?.TrySetException(new TickerCanceled(ticker));
        FlushCompletionCallbacks();
        _completeOnlyCallbacks = null;
    }

    private void ObserveOrCancel()
    {
        Task orCancel = OrCancel;
        if (orCancel.IsCompleted)
        {
            _ = orCancel.Exception;
            return;
        }

        _ = orCancel.ContinueWith(
            static task => _ = task.Exception,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private void FlushCompletionCallbacks()
    {
        List<Action>? callbacks = _completionCallbacks;
        _completionCallbacks = null;
        Flush(callbacks);
    }

    private void FlushCompleteOnlyCallbacks()
    {
        List<Action>? callbacks = _completeOnlyCallbacks;
        _completeOnlyCallbacks = null;
        Flush(callbacks);
    }

    private static void Flush(List<Action>? callbacks)
    {
        if (callbacks is null)
        {
            return;
        }

        foreach (Action callback in callbacks)
        {
            Scheduler.ScheduleMicrotask(callback);
        }
    }
}

/// <summary>
/// Exception thrown by <see cref="Ticker"/> objects on the <see cref="TickerFuture.OrCancel"/> task
/// when the ticker is canceled.
/// </summary>
public sealed class TickerCanceled : Exception
{
    /// <summary>Creates a canceled-ticker exception.</summary>
    public TickerCanceled(Ticker? ticker = null)
        : base(ticker is not null
            ? $"This ticker was canceled: {ticker}"
            : "The ticker was canceled before the \"orCancel\" property was first used.")
    {
        Ticker = ticker;
    }

    /// <summary>Reference to the <see cref="Ticker"/> object that was canceled, when known.</summary>
    public Ticker? Ticker { get; }
}
