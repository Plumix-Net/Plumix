using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Physics;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/animation/animation_controller.dart

namespace Plumix;

/// <summary>The direction in which an animation is running.</summary>
/// <remarks>Dart parity source: the private <c>_AnimationDirection</c> of animation_controller.dart.</remarks>
internal enum AnimationDirection
{
    /// <summary>The animation is running from beginning to end.</summary>
    Forward,

    /// <summary>The animation is running backwards, from end to beginning.</summary>
    Reverse,
}

/// <summary>Configures how an <see cref="AnimationController"/> behaves when animations are disabled.</summary>
public enum AnimationBehavior
{
    /// <summary>The <see cref="AnimationController"/> will reduce its duration when animations are disabled.</summary>
    Normal,

    /// <summary>The <see cref="AnimationController"/> will preserve its behavior.</summary>
    Preserve,
}

public sealed class AnimationController : Animation<double>, IDisposable
{
    private static readonly SpringDescription _flingSpringDescription =
        SpringDescription.WithDampingRatio(mass: 1.0, stiffness: 500.0);

    private static readonly Tolerance _flingTolerance =
        new(distance: 0.01, velocity: double.PositiveInfinity);

    private readonly List<Action> _listeners = [];
    private readonly List<Action<AnimationStatus>> _statusListeners = [];

    private Ticker? _ticker;
    private Simulation? _simulation;
    private double _value;
    private AnimationStatus _status;
    private TimeSpan? _lastElapsedDuration;
    private AnimationDirection _direction = AnimationDirection.Forward;
    private AnimationStatus _lastReportedStatus = AnimationStatus.Dismissed;

    /// <summary>Creates an animation controller.</summary>
    public AnimationController(
        double? value = null,
        TimeSpan? duration = null,
        TimeSpan? reverseDuration = null,
        string? debugLabel = null,
        double lowerBound = 0.0,
        double upperBound = 1.0,
        AnimationBehavior animationBehavior = AnimationBehavior.Normal,
        ITickerProvider? vsync = null)
    {
        if (upperBound < lowerBound)
        {
            throw new ArgumentOutOfRangeException(nameof(upperBound), "upperBound must be >= lowerBound.");
        }

        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchCreated("animation", "AnimationController", this);
        }
        LowerBound = lowerBound;
        UpperBound = upperBound;
        Duration = duration;
        ReverseDuration = reverseDuration;
        DebugLabel = debugLabel;
        Behavior = animationBehavior;
        _ticker = vsync?.CreateTicker(Tick) ?? new Ticker(Tick, debugLabel);
        InternalSetValue(value ?? lowerBound);
    }

    /// <summary>
    /// Creates an animation controller with no upper or lower bound for its value. Dart parity source:
    /// <c>AnimationController.unbounded</c>, which C# expresses as a static factory.
    /// </summary>
    public static AnimationController Unbounded(
        double value = 0.0,
        TimeSpan? duration = null,
        TimeSpan? reverseDuration = null,
        string? debugLabel = null,
        ITickerProvider? vsync = null,
        AnimationBehavior animationBehavior = AnimationBehavior.Preserve)
    {
        return new AnimationController(
            value: value,
            duration: duration,
            reverseDuration: reverseDuration,
            debugLabel: debugLabel,
            lowerBound: double.NegativeInfinity,
            upperBound: double.PositiveInfinity,
            animationBehavior: animationBehavior,
            vsync: vsync);
    }

    /// <summary>Fired whenever <see cref="Value"/> changes; an alias for <see cref="AddListener"/>.</summary>
    public event Action? Changed
    {
        add => AddListener(value!);
        remove => RemoveListener(value!);
    }

    /// <summary>Fired when an animation tick drives the status to <see cref="AnimationStatus.Completed"/>.</summary>
    public event Action? Completed;

    /// <summary>Fired when an animation tick drives the status to <see cref="AnimationStatus.Dismissed"/>.</summary>
    public event Action? Dismissed;

    /// <summary>The value at which this animation is deemed to be dismissed.</summary>
    public double LowerBound { get; }

    /// <summary>The value at which this animation is deemed to be completed.</summary>
    public double UpperBound { get; }

    /// <summary>A label that is used in the <see cref="ToString"/> output.</summary>
    public string? DebugLabel { get; }

    /// <summary>The behavior of the controller when animations are disabled.</summary>
    /// <remarks>Dart parity source: <c>AnimationController.animationBehavior</c>.</remarks>
    public AnimationBehavior Behavior { get; }

    /// <summary>The length of time this animation should last.</summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>The length of time this animation should last when going in reverse.</summary>
    public TimeSpan? ReverseDuration { get; set; }

    /// <summary>
    /// The curve <see cref="Evaluate"/> applies to <see cref="Value"/>. Plumix-only convenience for
    /// consumers that read a curved value directly instead of composing a <c>CurvedAnimation</c>.
    /// </summary>
    public Curve Curve { get; set; } = Curves.Linear;

    /// <summary>Returns an <see cref="Animation{T}"/> for this controller, so it can be passed around safely.</summary>
    public Animation<double> View => this;

    public override double Value => _value;

    public override AnimationStatus Status => _status;

    /// <summary>The amount of time that has passed between the animation starting and the most recent tick.</summary>
    public TimeSpan? LastElapsedDuration => _lastElapsedDuration;

    /// <summary>Whether this animation is currently animating in either the forward or reverse direction.</summary>
    public bool IsAnimating => _ticker is not null && _ticker.IsActive;

    /// <summary>Whether this controller's value may leave the <c>[LowerBound, UpperBound]</c> range.</summary>
    public bool IsUnbounded => double.IsNegativeInfinity(LowerBound) && double.IsPositiveInfinity(UpperBound);

    /// <summary>The rate of change of <see cref="Value"/> per second.</summary>
    /// <remarks>
    /// Returns zero when the animation is not running; the returned value comes from the running
    /// simulation, so a duration-driven animation reports its interpolated velocity as well.
    /// </remarks>
    public double Velocity => IsAnimating
        ? _simulation!.DX(_lastElapsedDuration!.Value.TotalSeconds)
        : 0.0;

    /// <summary>
    /// Stops the animation and sets the current value of the animation. Dart parity source: the
    /// <c>value</c> setter, which C# cannot express because <see cref="Animation{T}.Value"/> declares
    /// no setter to override.
    /// </summary>
    public void SetValue(double newValue)
    {
        Stop();
        InternalSetValue(newValue);
        NotifyListeners();
        CheckStatusChanged();
    }

    /// <summary>Sets the controller's value to <see cref="LowerBound"/>, stopping the animation.</summary>
    public void Reset() => SetValue(LowerBound);

    /// <summary>Starts running this animation forwards (towards the end).</summary>
    public TickerFuture Forward(double? from = null)
    {
        if (Duration is null)
        {
            throw new InvalidOperationException(
                "AnimationController.Forward() called with no default duration.\n"
                + "The \"Duration\" property should be set, either in the constructor or later, before "
                + "calling the Forward() function.");
        }

        ThrowIfDisposed(nameof(Forward));
        _direction = AnimationDirection.Forward;
        if (from.HasValue)
        {
            SetValue(from.Value);
        }

        return AnimateToInternal(UpperBound);
    }

    /// <summary>Starts running this animation in reverse (towards the beginning).</summary>
    public TickerFuture Reverse(double? from = null)
    {
        if (Duration is null && ReverseDuration is null)
        {
            throw new InvalidOperationException(
                "AnimationController.Reverse() called with no default duration or reverseDuration.\n"
                + "The \"Duration\" or \"ReverseDuration\" property should be set, either in the "
                + "constructor or later, before calling the Reverse() function.");
        }

        ThrowIfDisposed(nameof(Reverse));
        _direction = AnimationDirection.Reverse;
        if (from.HasValue)
        {
            SetValue(from.Value);
        }

        return AnimateToInternal(LowerBound);
    }

    /// <summary>Toggles the direction of this animation, based on whether it is forward or completed.</summary>
    public TickerFuture Toggle(double? from = null)
    {
        TimeSpan? duration = Duration;
        if (_status.IsForwardOrCompleted())
        {
            duration ??= ReverseDuration;
        }

        if (duration is null)
        {
            throw new InvalidOperationException(
                "AnimationController.Toggle() called with no default duration.\n"
                + "The \"Duration\" property should be set, either in the constructor or later, before "
                + "calling the Toggle() function.");
        }

        ThrowIfDisposed(nameof(Toggle));
        _direction = _status.IsForwardOrCompleted() ? AnimationDirection.Reverse : AnimationDirection.Forward;
        if (from.HasValue)
        {
            SetValue(from.Value);
        }

        return AnimateToInternal(_direction == AnimationDirection.Forward ? UpperBound : LowerBound);
    }

    /// <summary>Drives the animation from its current value to <paramref name="target"/>.</summary>
    public TickerFuture AnimateTo(double target, TimeSpan? duration = null, Curve? curve = null)
    {
        if (Duration is null && duration is null)
        {
            throw new InvalidOperationException(
                "AnimationController.AnimateTo() called with no explicit duration and no default duration.\n"
                + "Either the \"duration\" argument to the AnimateTo() method should be provided, or the "
                + "\"Duration\" property should be set, either in the constructor or later, before calling "
                + "the AnimateTo() function.");
        }

        ThrowIfDisposed(nameof(AnimateTo));
        _direction = AnimationDirection.Forward;
        return AnimateToInternal(target, duration, curve);
    }

    /// <summary>Drives the animation from its current value to <paramref name="target"/> in reverse.</summary>
    public TickerFuture AnimateBack(double target, TimeSpan? duration = null, Curve? curve = null)
    {
        if (Duration is null && ReverseDuration is null && duration is null)
        {
            throw new InvalidOperationException(
                "AnimationController.AnimateBack() called with no explicit duration and no default "
                + "duration or reverseDuration.\n"
                + "Either the \"duration\" argument to the AnimateBack() method should be provided, or "
                + "the \"Duration\" or \"ReverseDuration\" property should be set, either in the "
                + "constructor or later, before calling the AnimateBack() function.");
        }

        ThrowIfDisposed(nameof(AnimateBack));
        _direction = AnimationDirection.Reverse;
        return AnimateToInternal(target, duration, curve);
    }

    /// <summary>Drives the animation according to the given simulation, running forwards.</summary>
    public TickerFuture AnimateWith(Simulation simulation)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ThrowIfDisposed(nameof(AnimateWith));
        Stop();
        _direction = AnimationDirection.Forward;
        return StartSimulation(simulation);
    }

    /// <summary>Drives the animation according to the given simulation, running in reverse.</summary>
    public TickerFuture AnimateBackWith(Simulation simulation)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ThrowIfDisposed(nameof(AnimateBackWith));
        Stop();
        _direction = AnimationDirection.Reverse;
        return StartSimulation(simulation);
    }

    /// <summary>Starts running this animation in the forward direction, and restarts it when it completes.</summary>
    public TickerFuture Repeat(
        double? min = null,
        double? max = null,
        bool reverse = false,
        TimeSpan? period = null,
        int? count = null)
    {
        double effectiveMin = min ?? LowerBound;
        double effectiveMax = max ?? UpperBound;
        TimeSpan? effectivePeriod = period ?? Duration;
        if (effectivePeriod is null)
        {
            throw new InvalidOperationException(
                "AnimationController.Repeat() called without an explicit period and with no default "
                + "Duration.\n"
                + "Either the \"period\" argument to the Repeat() method should be provided, or the "
                + "\"Duration\" property should be set, either in the constructor or later, before "
                + "calling the Repeat() function.");
        }

        if (effectiveMax < effectiveMin || effectiveMax > UpperBound || effectiveMin < LowerBound)
        {
            throw new ArgumentOutOfRangeException(
                nameof(min),
                "Repeat() requires LowerBound <= min <= max <= UpperBound.");
        }

        if (count is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count shall be greater than zero if not null");
        }

        Stop();
        return StartSimulation(new RepeatingSimulation(
            _value,
            effectiveMin,
            effectiveMax,
            reverse,
            effectivePeriod.Value,
            SetDirection,
            count));
    }

    /// <summary>
    /// Drives the animation with a spring, within <see cref="LowerBound"/> and
    /// <see cref="UpperBound"/>.
    /// </summary>
    public TickerFuture Fling(
        double velocity = 1.0,
        SpringDescription? springDescription = null,
        AnimationBehavior? animationBehavior = null)
    {
        springDescription ??= _flingSpringDescription;
        _direction = velocity < 0.0 ? AnimationDirection.Reverse : AnimationDirection.Forward;
        double target = velocity < 0.0
            ? LowerBound - _flingTolerance.Distance
            : UpperBound + _flingTolerance.Distance;
        AnimationBehavior behavior = animationBehavior ?? Behavior;

        // The 200.0 value is arbitrary; Flutter chose it because it worked for the drawer widget.
        double scale = EnableAnimations(behavior) ? 1.0 : 200.0;
        var simulation = new SpringSimulation(
            springDescription,
            _value,
            target,
            velocity * scale,
            tolerance: _flingTolerance);
        if (simulation.Type == SpringType.UnderDamped)
        {
            throw new ArgumentException(
                "The specified spring simulation is of type SpringType.UnderDamped.\n"
                + "An underdamped spring results in oscillation rather than a fling. Consider "
                + "specifying a different springDescription, or use AnimateWith() with an explicit "
                + "SpringSimulation if an underdamped spring is intentional.",
                nameof(springDescription));
        }

        ThrowIfDisposed(nameof(Fling));
        Stop();
        return StartSimulation(simulation);
    }

    /// <summary>Stops running this animation.</summary>
    /// <param name="canceled">
    /// When true (the default) the outstanding <see cref="TickerFuture"/> never resolves and its
    /// <see cref="TickerFuture.OrCancel"/> faults; when false the future resolves.
    /// </param>
    public void Stop(bool canceled = true)
    {
        ThrowIfDisposed(nameof(Stop));
        _simulation = null;
        _lastElapsedDuration = null;
        _ticker!.Stop(canceled: canceled);
    }

    /// <summary>
    /// Switches this controller to a new <see cref="ITickerProvider"/>, preserving the running
    /// animation.
    /// </summary>
    public void Resync(ITickerProvider vsync)
    {
        ArgumentNullException.ThrowIfNull(vsync);
        ThrowIfDisposed(nameof(Resync));
        Ticker oldTicker = _ticker!;
        _ticker = vsync.CreateTicker(Tick);
        _ticker.AbsorbTicker(oldTicker);
    }

    /// <summary>Evaluates <see cref="Curve"/> at the clamped current value.</summary>
    public double Evaluate() => Curve.Transform(Math.Clamp(_value, 0.0, 1.0));

    public override void AddListener(Action listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        _listeners.Add(listener);
    }

    public override void RemoveListener(Action listener)
    {
        _listeners.Remove(listener);
    }

    public override void AddStatusListener(Action<AnimationStatus> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        _statusListeners.Add(listener);
    }

    public override void RemoveStatusListener(Action<AnimationStatus> listener)
    {
        _statusListeners.Remove(listener);
    }

    public void Dispose()
    {
        if (_ticker is null)
        {
            throw new ObjectDisposedException(
                nameof(AnimationController),
                "AnimationController.Dispose() called more than once. A given AnimationController "
                + "cannot be disposed more than once.");
        }

        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchDisposed(this);
        }

        _ticker.Dispose();
        _ticker = null;
        _statusListeners.Clear();
        _listeners.Clear();
        Completed = null;
        Dismissed = null;
    }

    public override string ToStringDetails()
    {
        string paused = IsAnimating ? string.Empty : "; paused";
        string ticker = _ticker is null ? "; DISPOSED" : _ticker.Muted ? "; silenced" : string.Empty;
        string label = string.Empty;
        if (Constants.KDebugMode && DebugLabel is not null)
        {
            label = $"; for {DebugLabel}";
        }

        string more = $"{base.ToStringDetails()} {Diagnostics.ToStringAsFixed(_value, 3)}";
        return $"{more}{paused}{ticker}{label}";
    }

    /// <summary>
    /// Drives the controller from a user gesture: the value is set without starting a simulation and
    /// the status is forced to <see cref="AnimationStatus.Reverse"/> so that a dismissing route keeps
    /// reporting a reversing transition while the pointer owns the animation.
    /// </summary>
    internal void SetValueForUserGesture(double value)
    {
        Stop();
        _value = Math.Clamp(value, LowerBound, UpperBound);
        _direction = AnimationDirection.Reverse;
        _status = AnimationStatus.Reverse;
        NotifyListeners();
        CheckStatusChanged();
    }

    private static bool EnableAnimations(AnimationBehavior behavior)
    {
        return behavior switch
        {
            AnimationBehavior.Normal => !SemanticsBinding.Instance.DisableAnimations,
            _ => true,
        };
    }

    private static TimeSpan Scale(TimeSpan duration, double factor)
    {
        return TimeSpan.FromTicks((long)Math.Round(duration.Ticks * factor));
    }

    private void ThrowIfDisposed(string member)
    {
        if (_ticker is null)
        {
            throw new ObjectDisposedException(
                nameof(AnimationController),
                $"AnimationController.{member}() called after AnimationController.Dispose(). "
                + "AnimationController methods should not be used after calling Dispose.");
        }
    }

    private void InternalSetValue(double newValue)
    {
        _value = Math.Clamp(newValue, LowerBound, UpperBound);
        if (_value == LowerBound)
        {
            _status = AnimationStatus.Dismissed;
        }
        else if (_value == UpperBound)
        {
            _status = AnimationStatus.Completed;
        }
        else
        {
            _status = _direction == AnimationDirection.Forward
                ? AnimationStatus.Forward
                : AnimationStatus.Reverse;
        }
    }

    private TickerFuture AnimateToInternal(double target, TimeSpan? duration = null, Curve? curve = null)
    {
        double scale = EnableAnimations(Behavior) ? 1.0 : 0.05;
        TimeSpan? simulationDuration = duration;
        if (simulationDuration is null)
        {
            double range = UpperBound - LowerBound;
            double remainingFraction = double.IsFinite(range) ? Math.Abs(target - _value) / range : 1.0;
            TimeSpan directionDuration = _direction == AnimationDirection.Reverse && ReverseDuration is not null
                ? ReverseDuration.Value
                : Duration!.Value;
            simulationDuration = Scale(directionDuration, remainingFraction);
        }
        else if (target == _value)
        {
            // Already at target, don't animate.
            simulationDuration = TimeSpan.Zero;
        }

        Stop();
        if (simulationDuration == TimeSpan.Zero)
        {
            if (_value != target)
            {
                _value = Math.Clamp(target, LowerBound, UpperBound);
                NotifyListeners();
            }

            _status = _direction == AnimationDirection.Forward
                ? AnimationStatus.Completed
                : AnimationStatus.Dismissed;
            CheckStatusChanged();
            NotifyTerminalStatus();
            return TickerFuture.Completed();
        }

        return StartSimulation(new InterpolationSimulation(
            _value,
            target,
            simulationDuration.Value,
            curve ?? Curves.Linear,
            scale));
    }

    private TickerFuture StartSimulation(Simulation simulation)
    {
        _simulation = simulation;
        _lastElapsedDuration = TimeSpan.Zero;
        _value = Math.Clamp(simulation.X(0.0), LowerBound, UpperBound);
        TickerFuture result = _ticker!.Start();
        _status = _direction == AnimationDirection.Forward
            ? AnimationStatus.Forward
            : AnimationStatus.Reverse;
        CheckStatusChanged();
        return result;
    }

    private void SetDirection(AnimationDirection direction)
    {
        _direction = direction;
        _status = _direction == AnimationDirection.Forward
            ? AnimationStatus.Forward
            : AnimationStatus.Reverse;
        CheckStatusChanged();
    }

    private void Tick(TimeSpan elapsed)
    {

        _lastElapsedDuration = elapsed;
        double elapsedInSeconds = elapsed.TotalSeconds;
        _value = Math.Clamp(_simulation!.X(elapsedInSeconds), LowerBound, UpperBound);
        bool done = _simulation.IsDone(elapsedInSeconds);
        if (done)
        {
            _status = _direction == AnimationDirection.Forward
                ? AnimationStatus.Completed
                : AnimationStatus.Dismissed;
            Stop(canceled: false);
        }

        NotifyListeners();
        CheckStatusChanged();
        if (done)
        {
            NotifyTerminalStatus();
        }
    }

    // Plumix convenience events for the two terminal statuses an animation can settle on. They fire
    // wherever the controller drives itself to that status, and not when the value is set directly.
    private void NotifyTerminalStatus()
    {
        if (_status == AnimationStatus.Completed)
        {
            Completed?.Invoke();
        }
        else if (_status == AnimationStatus.Dismissed)
        {
            Dismissed?.Invoke();
        }
    }

    private void CheckStatusChanged()
    {
        AnimationStatus newStatus = _status;
        if (_lastReportedStatus == newStatus)
        {
            return;
        }

        _lastReportedStatus = newStatus;
        NotifyStatusListeners(newStatus);
    }

    // Dart's `notifyListeners` snapshots the list and re-checks membership before every call, so a
    // listener removed by an earlier listener in the same notification is not invoked.
    private void NotifyListeners()
    {
        if (_listeners.Count == 0)
        {
            return;
        }

        foreach (Action listener in _listeners.ToArray())
        {
            if (_listeners.Contains(listener))
            {
                listener();
            }
        }
    }

    private void NotifyStatusListeners(AnimationStatus status)
    {
        if (_statusListeners.Count == 0)
        {
            return;
        }

        foreach (Action<AnimationStatus> listener in _statusListeners.ToArray())
        {
            if (_statusListeners.Contains(listener))
            {
                listener(status);
            }
        }
    }
}

/// <summary>Dart parity source: the private <c>_InterpolationSimulation</c> of animation_controller.dart.</summary>
internal sealed class InterpolationSimulation : Simulation
{
    private readonly double _begin;
    private readonly double _end;
    private readonly Curve _curve;
    private readonly double _durationInSeconds;

    public InterpolationSimulation(double begin, double end, TimeSpan duration, Curve curve, double scale)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        _begin = begin;
        _end = end;
        _curve = curve;
        _durationInSeconds = duration.TotalSeconds * scale;
    }

    public override double X(double time)
    {
        double t = Math.Clamp(time / _durationInSeconds, 0.0, 1.0);
        return t switch
        {
            0.0 => _begin,
            1.0 => _end,
            _ => _begin + ((_end - _begin) * _curve.Transform(t)),
        };
    }

    public override double DX(double time)
    {
        double epsilon = Tolerance.Time;
        return (X(time + epsilon) - X(time - epsilon)) / (2 * epsilon);
    }

    public override bool IsDone(double time) => time > _durationInSeconds;
}

/// <summary>Dart parity source: the private <c>_RepeatingSimulation</c> of animation_controller.dart.</summary>
internal sealed class RepeatingSimulation : Simulation
{
    private readonly double _min;
    private readonly double _max;
    private readonly bool _reverse;
    private readonly int? _count;
    private readonly Action<AnimationDirection> _directionSetter;
    private readonly double _periodInSeconds;
    private readonly double _initialT;

    public RepeatingSimulation(
        double initialValue,
        double min,
        double max,
        bool reverse,
        TimeSpan period,
        Action<AnimationDirection> directionSetter,
        int? count)
    {
        _min = min;
        _max = max;
        _reverse = reverse;
        _directionSetter = directionSetter;
        _count = count;
        _periodInSeconds = period.TotalSeconds;
        _initialT = max == min
            ? 0.0
            : (Math.Clamp(initialValue, min, max) - min) / (max - min) * period.TotalSeconds;
        if (_periodInSeconds <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(period));
        }
    }

    private double ExitTimeInSeconds => (_count!.Value * _periodInSeconds) - _initialT;

    public override double X(double time)
    {
        double totalTimeInSeconds = time + _initialT;
        double t = totalTimeInSeconds / _periodInSeconds % 1.0;
        bool isPlayingReverse = (long)(totalTimeInSeconds / _periodInSeconds) % 2 != 0;
        if (_reverse && isPlayingReverse)
        {
            _directionSetter(AnimationDirection.Reverse);
            return _max + ((_min - _max) * t);
        }

        _directionSetter(AnimationDirection.Forward);
        return _min + ((_max - _min) * t);
    }

    public override double DX(double time) => (_max - _min) / _periodInSeconds;

    public override bool IsDone(double time) => _count is not null && time >= ExitTimeInSeconds;
}
