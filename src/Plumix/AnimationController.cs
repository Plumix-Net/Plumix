using Avalonia;
using Avalonia.Media;

// Dart parity source (reference): flutter/packages/flutter/lib/src/animation/animation_controller.dart (approximate)

namespace Plumix;

// ---------- Animation primitives ----------
public delegate double Curve(double t);

public static class Curves
{
    public static double Linear(double t) => t;

    public static Curve Cubic(double x1, double y1, double x2, double y2) =>
        t => CubicBezier(t, x1, y1, x2, y2);

    public static Curve Interval(double begin, double end, Curve? curve = null)
    {
        if (begin < 0.0 || begin > 1.0 || end < 0.0 || end > 1.0 || end < begin)
        {
            throw new ArgumentOutOfRangeException(nameof(begin));
        }

        Curve effectiveCurve = curve ?? Linear;
        return t => effectiveCurve(Math.Clamp((t - begin) / Math.Max(end - begin, double.Epsilon), 0.0, 1.0));
    }

    public static Curve Flipped(Curve curve)
    {
        ArgumentNullException.ThrowIfNull(curve);
        return t => 1.0 - curve(1.0 - t);
    }

    // Flutter Material Easing.emphasizedAccelerate.
    public static Curve EmphasizedAccelerate { get; } = Cubic(0.3, 0.0, 0.8, 0.15);

    // Flutter Curves.ease: Cubic(0.25, 0.1, 0.25, 1.0).
    public static double Ease(double t) => CubicBezier(t, 0.25, 0.1, 0.25, 1.0);

    // Flutter Curves.easeInOut: Cubic(0.42, 0.0, 0.58, 1.0).
    public static double EaseInOut(double t) => CubicBezier(t, 0.42, 0.0, 0.58, 1.0);

    // Flutter Curves.easeIn: Cubic(0.42, 0.0, 1.0, 1.0).
    public static double EaseIn(double t) => CubicBezier(t, 0.42, 0.0, 1.0, 1.0);

    // Flutter Curves.easeOut: Cubic(0.0, 0.0, 0.58, 1.0).
    public static double EaseOut(double t) => CubicBezier(t, 0.0, 0.0, 0.58, 1.0);

    public static double FastOutSlowIn(double t)
    {
        t = Math.Clamp(t, 0, 1);
        double parameter = t;
        for (int i = 0; i < 8; i++)
        {
            double x = Cubic(parameter, 0.4, 0.2) - t;
            if (Math.Abs(x) < 1e-7) break;
            double derivative = CubicDerivative(parameter, 0.4, 0.2);
            if (Math.Abs(derivative) < 1e-7) break;
            parameter = Math.Clamp(parameter - (x / derivative), 0, 1);
        }
        return Cubic(parameter, 0, 1);
    }

    private static double CubicBezier(double t, double x1, double y1, double x2, double y2)
    {
        t = Math.Clamp(t, 0, 1);
        double parameter = t;
        for (int i = 0; i < 8; i++)
        {
            double x = Cubic(parameter, x1, x2) - t;
            if (Math.Abs(x) < 1e-7) break;
            double derivative = CubicDerivative(parameter, x1, x2);
            if (Math.Abs(derivative) < 1e-7) break;
            parameter = Math.Clamp(parameter - (x / derivative), 0, 1);
        }

        return Cubic(parameter, y1, y2);
    }

    private static double Cubic(double t, double firstControl, double secondControl)
    {
        double inverse = 1 - t;
        return (3 * inverse * inverse * t * firstControl)
               + (3 * inverse * t * t * secondControl)
               + (t * t * t);
    }

    private static double CubicDerivative(double t, double firstControl, double secondControl)
    {
        double inverse = 1 - t;
        return (3 * inverse * inverse * firstControl)
               + (6 * inverse * t * (secondControl - firstControl))
               + (3 * t * t * (1 - secondControl));
    }
}

public abstract class Animatable<T>
{
    public abstract T Transform(double t);

    public Animation<T> Animate(Animation<double> parent)
    {
        return new AnimatedEvaluation<T>(
            parent ?? throw new ArgumentNullException(nameof(parent)),
            this);
    }
}

internal sealed class AnimatedEvaluation<T> : Animation<T>
{
    private readonly Animation<double> _parent;
    private readonly Animatable<T> _evaluatable;

    public AnimatedEvaluation(Animation<double> parent, Animatable<T> evaluatable)
    {
        _parent = parent;
        _evaluatable = evaluatable;
    }

    public override T Value => _evaluatable.Transform(_parent.Value);

    public override AnimationStatus Status => _parent.Status;

    public override void AddListener(Action listener) => _parent.AddListener(listener);

    public override void RemoveListener(Action listener) => _parent.RemoveListener(listener);

    public override void AddStatusListener(Action<AnimationStatus> listener)
    {
        _parent.AddStatusListener(listener);
    }

    public override void RemoveStatusListener(Action<AnimationStatus> listener)
    {
        _parent.RemoveStatusListener(listener);
    }
}

public abstract class Tween<T> : Animatable<T>
{
    private T _begin = default!;
    private T _end = default!;

    protected Tween()
    {
    }

    public T? Begin
    {
        get => HasBeginValue ? _begin : default;
        set
        {
            if (value is null)
            {
                _begin = default!;
                HasBeginValue = false;
                return;
            }

            _begin = value;
            HasBeginValue = true;
        }
    }

    public T? End
    {
        get => HasEndValue ? _end : default;
        set
        {
            if (value is null)
            {
                _end = default!;
                HasEndValue = false;
                return;
            }

            _end = value;
            HasEndValue = true;
        }
    }

    internal bool HasBeginValue { get; private set; }

    internal bool HasEndValue { get; private set; }

    public abstract T Lerp(T a, T b, double t);

    public T Evaluate(double t, T from, T to) => Lerp(from, to, Math.Clamp(t, 0, 1));

    public virtual T Evaluate(double t)
    {
        if (!HasBeginValue || !HasEndValue)
        {
            throw new InvalidOperationException("Tween begin and end values must both be set before evaluation.");
        }

        return Evaluate(t, _begin, _end);
    }

    public override T Transform(double t) => Evaluate(t);

    internal T GetBeginValue()
    {
        if (!HasBeginValue)
        {
            throw new InvalidOperationException("Tween begin value is not set.");
        }

        return _begin;
    }

    internal T GetEndValue()
    {
        if (!HasEndValue)
        {
            throw new InvalidOperationException("Tween end value is not set.");
        }

        return _end;
    }

    internal void SetBeginValue(T value)
    {
        _begin = value;
        HasBeginValue = true;
    }

    internal void SetEndValue(T value)
    {
        _end = value;
        HasEndValue = true;
    }

    internal void ClearBeginValue()
    {
        _begin = default!;
        HasBeginValue = false;
    }

    internal void ClearEndValue()
    {
        _end = default!;
        HasEndValue = false;
    }
}

public sealed class DoubleTween : Tween<double>
{
    public DoubleTween(double? begin = null, double? end = null)
    {
        Begin = begin;
        End = end;
    }

    public new double? Begin
    {
        get => HasBeginValue ? GetBeginValue() : null;
        set
        {
            if (value.HasValue)
            {
                SetBeginValue(value.Value);
            }
            else
            {
                ClearBeginValue();
            }
        }
    }

    public new double? End
    {
        get => HasEndValue ? GetEndValue() : null;
        set
        {
            if (value.HasValue)
            {
                SetEndValue(value.Value);
            }
            else
            {
                ClearEndValue();
            }
        }
    }

    public override double Lerp(double a, double b, double t) => a + (b - a) * t;
}

public sealed class ColorTween : Tween<Color>
{
    public ColorTween(Color? begin = null, Color? end = null)
    {
        Begin = begin;
        End = end;
    }

    public new Color? Begin
    {
        get => HasBeginValue ? GetBeginValue() : null;
        set
        {
            if (value.HasValue)
            {
                SetBeginValue(value.Value);
            }
            else
            {
                ClearBeginValue();
            }
        }
    }

    public new Color? End
    {
        get => HasEndValue ? GetEndValue() : null;
        set
        {
            if (value.HasValue)
            {
                SetEndValue(value.Value);
            }
            else
            {
                ClearEndValue();
            }
        }
    }

    public override Color Lerp(Color a, Color b, double t)
    {
        byte L(byte x, byte y) => (byte)(x + (y - x) * t);
        return Color.FromArgb(
            L(a.A, b.A),
            L(a.R, b.R),
            L(a.G, b.G),
            L(a.B, b.B));
    }
}

public sealed class RectTween : Tween<Rect>
{
    public RectTween(Rect? begin = null, Rect? end = null)
    {
        Begin = begin;
        End = end;
    }

    public new Rect? Begin
    {
        get => HasBeginValue ? GetBeginValue() : null;
        set
        {
            if (value.HasValue)
            {
                SetBeginValue(value.Value);
            }
            else
            {
                ClearBeginValue();
            }
        }
    }

    public new Rect? End
    {
        get => HasEndValue ? GetEndValue() : null;
        set
        {
            if (value.HasValue)
            {
                SetEndValue(value.Value);
            }
            else
            {
                ClearEndValue();
            }
        }
    }

    public override Rect Lerp(Rect a, Rect b, double t)
    {
        double x = a.X + ((b.X - a.X) * t);
        double y = a.Y + ((b.Y - a.Y) * t);
        double width = a.Width + ((b.Width - a.Width) * t);
        double height = a.Height + ((b.Height - a.Height) * t);
        return new Rect(x, y, Math.Max(0, width), Math.Max(0, height));
    }
}

public sealed class AnimationController : Animation<double>, IDisposable
{
    public event Action? Changed;
    public event Action? Completed;
    public event Action? Dismissed;
    private event Action<AnimationStatus>? StatusChanged;

    private double _value;
    private AnimationStatus _status = AnimationStatus.Dismissed;

    public override double Value => _value;

    public override AnimationStatus Status => _status;

    public bool IsAnimating { get; private set; }
    private TimeSpan _duration;
    public TimeSpan Duration
    {
        get => _duration;
        set => _duration = value <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : value;
    }
    public Curve Curve { get; set; } = Curves.Linear;

    public TimeSpan? ReverseDuration { get; set; }

    private readonly Ticker _ticker;
    private bool _reversing;
    private bool _repeat;
    private bool _repeatReverse;
    private FlingSimulation? _flingSimulation;
    private double? _animateTarget;
    private double _animateStart;
    private double _animateElapsedSeconds;
    private TimeSpan _animateDuration;
    private Curve _animateCurve = Curves.Linear;
    private TaskCompletionSource? _animateCompletion;

    public AnimationController(TimeSpan duration, ITickerProvider? vsync = null)
    {
        Duration = duration <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : duration;
        _ticker = vsync?.CreateTicker(OnTick) ?? new Ticker(OnTick);
    }

    public void Forward(double? from = null)
    {
        CancelAnimateTo();
        if (from.HasValue) SetValue(from.Value);
        _flingSimulation = null;
        _reversing = false;
        _repeat = false;
        _repeatReverse = false;
        SetStatus(AnimationStatus.Forward);
        Start();
    }

    public void Reverse(double? from = null)
    {
        CancelAnimateTo();
        if (from.HasValue) SetValue(from.Value);
        _flingSimulation = null;
        _reversing = true;
        _repeat = false;
        _repeatReverse = false;
        SetStatus(AnimationStatus.Reverse);
        Start();
    }

    public void Repeat(bool reverse = false)
    {
        CancelAnimateTo();
        _flingSimulation = null;
        _repeat = true;
        _repeatReverse = reverse;
        _reversing = false;
        SetStatus(AnimationStatus.Forward);
        Start();
    }

    public void Fling(double velocity = 1.0)
    {
        if (double.IsNaN(velocity) || double.IsInfinity(velocity) || velocity == 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(velocity), "Fling velocity must be finite and non-zero.");
        }

        CancelAnimateTo();
        _reversing = velocity < 0.0;
        _repeat = false;
        _repeatReverse = false;
        _flingSimulation = new FlingSimulation(
            initialValue: Value,
            target: _reversing ? -0.01 : 1.01,
            initialVelocity: velocity);
        SetStatus(_reversing ? AnimationStatus.Reverse : AnimationStatus.Forward);
        Start();
    }

    public Task AnimateTo(double target, TimeSpan? duration = null, Curve? curve = null)
    {
        if (!double.IsFinite(target) || target is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(target), "Animation target must be between 0.0 and 1.0.");
        }

        if (duration.HasValue && duration.Value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        CancelAnimateTo();
        double distance = Math.Abs(target - _value);
        TimeSpan effectiveDuration = duration ?? TimeSpan.FromTicks((long)(Duration.Ticks * distance));
        if (distance <= 0.000001 || effectiveDuration <= TimeSpan.Zero)
        {
            _value = target;
            SetTerminalValueAndStatus(AnimationStatus.Completed);
            return Task.CompletedTask;
        }

        _flingSimulation = null;
        _repeat = false;
        _repeatReverse = false;
        _animateStart = _value;
        _animateTarget = target;
        _animateElapsedSeconds = 0.0;
        _animateDuration = effectiveDuration;
        _animateCurve = curve ?? Curves.Linear;
        _animateCompletion = new TaskCompletionSource();
        _reversing = false;
        SetStatus(AnimationStatus.Forward);
        Start();
        return _animateCompletion.Task;
    }

    public void Stop()
    {
        CancelAnimateTo();
        IsAnimating = false;
        _ticker.Stop();
        _flingSimulation = null;
    }

    public void SetValue(double value)
    {
        double next = Math.Clamp(value, 0, 1);
        if (Math.Abs(_value - next) <= 0.000001) return;
        _value = next;
        if (!IsAnimating)
        {
            SetStatus(next <= 0.0
                ? AnimationStatus.Dismissed
                : next >= 1.0
                    ? AnimationStatus.Completed
                    : _status);
        }
        Changed?.Invoke();
    }

    public override void AddListener(Action listener)
    {
        Changed += listener;
    }

    public override void RemoveListener(Action listener)
    {
        Changed -= listener;
    }

    public override void AddStatusListener(Action<AnimationStatus> listener)
    {
        StatusChanged += listener;
    }

    public override void RemoveStatusListener(Action<AnimationStatus> listener)
    {
        StatusChanged -= listener;
    }

    private void Start()
    {
        if (IsAnimating) return;
        IsAnimating = true;
        _ticker.Start();
    }

    private void OnTick(TimeSpan dt)
    {
        if (_animateTarget.HasValue)
        {
            TickAnimateTo(dt);
            return;
        }

        if (_flingSimulation is not null)
        {
            TickFling(dt);
            return;
        }

        TimeSpan effectiveDuration = _repeat
            ? Duration
            : _reversing
                ? ReverseDuration ?? Duration
                : Duration;
        double delta = dt.TotalSeconds / effectiveDuration.TotalSeconds;
        double raw;
        if (_repeat)
        {
            if (_repeatReverse)
            {
                double phase = _reversing ? 2.0 - _value : _value;
                phase = (phase + delta) % 2.0;
                bool reversing = phase > 1.0;
                raw = reversing ? 2.0 - phase : phase;
                _reversing = reversing;
                SetStatus(reversing ? AnimationStatus.Reverse : AnimationStatus.Forward);
            }
            else
            {
                raw = _value + delta;
                raw %= 1.0;
                if (raw < 0) raw += 1.0;
            }
        }
        else
        {
            raw = _value + (_reversing ? -delta : delta);
            if (raw >= 1.0)
            {
                _value = 1.0;
                Stop();
                SetTerminalValueAndStatus(AnimationStatus.Completed);
                Completed?.Invoke();
                return;
            }

            if (raw <= 0.0)
            {
                _value = 0.0;
                Stop();
                SetTerminalValueAndStatus(AnimationStatus.Dismissed);
                Dismissed?.Invoke();
                return;
            }
        }

        _value = Math.Clamp(raw, 0, 1);
        Changed?.Invoke();
    }

    public double Evaluate() => Curve(Math.Clamp(Value, 0, 1));

    public void Dispose()
    {
        Stop();
        _ticker.Dispose();
    }

    private void TickAnimateTo(TimeSpan delta)
    {
        double target = _animateTarget!.Value;
        _animateElapsedSeconds += delta.TotalSeconds;
        double progress = Math.Clamp(
            _animateElapsedSeconds / _animateDuration.TotalSeconds,
            0.0,
            1.0);
        double transformed = _animateCurve(progress);
        _value = Math.Clamp(_animateStart + ((target - _animateStart) * transformed), 0.0, 1.0);
        if (progress < 1.0)
        {
            Changed?.Invoke();
            return;
        }

        TaskCompletionSource? completion = _animateCompletion;
        bool reversed = _reversing;
        ClearAnimateTo();
        IsAnimating = false;
        _ticker.Stop();
        SetTerminalValueAndStatus(reversed ? AnimationStatus.Dismissed : AnimationStatus.Completed);
        completion?.TrySetResult();
        if (reversed)
        {
            Dismissed?.Invoke();
        }
        else
        {
            Completed?.Invoke();
        }
    }

    private void CancelAnimateTo()
    {
        TaskCompletionSource? completion = _animateCompletion;
        ClearAnimateTo();
        completion?.TrySetCanceled();
    }

    private void ClearAnimateTo()
    {
        _animateTarget = null;
        _animateCompletion = null;
        _animateElapsedSeconds = 0.0;
    }

    private void SetStatus(AnimationStatus status)
    {
        if (_status == status)
        {
            return;
        }

        _status = status;
        StatusChanged?.Invoke(status);
    }

    private void SetTerminalValueAndStatus(AnimationStatus status)
    {
        bool statusChanged = _status != status;
        _status = status;
        Changed?.Invoke();
        if (statusChanged)
        {
            StatusChanged?.Invoke(status);
        }
    }

    private void TickFling(TimeSpan delta)
    {
        FlingSimulation simulation = _flingSimulation!;
        simulation.ElapsedSeconds += delta.TotalSeconds;
        const double angularFrequency = 22.360679774997898;
        double displacement = simulation.InitialValue - simulation.Target;
        double coefficient = simulation.InitialVelocity + angularFrequency * displacement;
        double exponential = Math.Exp(-angularFrequency * simulation.ElapsedSeconds);
        double position = simulation.Target
                          + (displacement + coefficient * simulation.ElapsedSeconds) * exponential;
        _value = Math.Clamp(position, 0.0, 1.0);

        if (Math.Abs(position - simulation.Target) < 0.01)
        {
            AnimationStatus terminalStatus = _reversing
                ? AnimationStatus.Dismissed
                : AnimationStatus.Completed;
            _value = _reversing ? 0.0 : 1.0;
            Stop();
            SetTerminalValueAndStatus(terminalStatus);
            if (_reversing)
            {
                Dismissed?.Invoke();
            }
            else
            {
                Completed?.Invoke();
            }

            return;
        }

        Changed?.Invoke();
    }

    private sealed class FlingSimulation(double initialValue, double target, double initialVelocity)
    {
        public double InitialValue { get; } = initialValue;

        public double Target { get; } = target;

        public double InitialVelocity { get; } = initialVelocity;

        public double ElapsedSeconds { get; set; }
    }
}
