using Avalonia.Threading;

// C#-only infrastructure. Dart's gesture recognizers use `dart:async`'s `Timer`, whose `isActive`
// flag and `FakeAsync`-driven test clock have no .NET counterpart. `GestureTimer` supplies both: the
// `IsActive` flag the tap-series tracker reads after a timer has fired, and a `Factory` seam that
// tests install to drive deadlines deterministically.

namespace Plumix.Gestures;

/// <summary>A cancellable one-shot timer with an <see cref="IsActive"/> flag, like Dart's `Timer`.</summary>
public abstract class GestureTimer : IDisposable
{
    /// <summary>
    /// Creates the timers every gesture recognizer uses. Tests replace this with a manual
    /// implementation; assigning null restores the dispatcher-backed default.
    /// </summary>
    public static Func<TimeSpan, Action, GestureTimer> Factory
    {
        get => FactoryOverride ?? DefaultFactory;
        set => FactoryOverride = value;
    }

    private static Func<TimeSpan, Action, GestureTimer>? FactoryOverride;

    /// <summary>Restores the dispatcher-backed default factory.</summary>
    public static void ResetFactory() => FactoryOverride = null;

    /// <summary>Starts a timer through the current <see cref="Factory"/>.</summary>
    public static GestureTimer Start(TimeSpan duration, Action callback) => Factory(duration, callback);

    /// <summary>Whether the timer has neither fired nor been cancelled.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Cancels the timer; the callback will not run.</summary>
    public virtual void Cancel() => IsActive = false;

    public void Dispose() => Cancel();

    /// <summary>Runs the callback once, if the timer is still active.</summary>
    protected void Fire(Action callback)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        callback();
    }

    private static GestureTimer DefaultFactory(TimeSpan duration, Action callback)
    {
        return new DispatcherGestureTimer(duration, callback);
    }
}

/// <summary>The default <see cref="GestureTimer"/>: an Avalonia dispatcher timer.</summary>
public sealed class DispatcherGestureTimer : GestureTimer
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromTicks(1) };

    public DispatcherGestureTimer(TimeSpan duration, Action callback)
    {
        _timer.Interval = duration <= TimeSpan.Zero ? TimeSpan.FromTicks(1) : duration;
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            Fire(callback);
        };
        _timer.Start();
    }

    public override void Cancel()
    {
        _timer.Stop();
        base.Cancel();
    }
}
