using Avalonia;
using Avalonia.Media;

// Dart parity source (reference): flutter/packages/flutter/lib/src/animation/animation_controller.dart (approximate)

namespace Plumix;

// ---------- Animation primitives ----------
public delegate double Curve(double t);

public static class Curves
{
    public static double Linear(double t) => t;

    // Flutter Curves.ease: Cubic(0.25, 0.1, 0.25, 1.0).
    public static double Ease(double t) => CubicBezier(t, 0.25, 0.1, 0.25, 1.0);

    public static double EaseInOut(double t)
    {
        // простая S-кривая (smoothstep)
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }

    public static double EaseIn(double t) => t * t;
    public static double EaseOut(double t) => 1 - (1 - t) * (1 - t);

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

public abstract class Tween<T>
{
    public abstract T Lerp(T a, T b, double t);
    public T Evaluate(double t, T from, T to) => Lerp(from, to, Math.Clamp(t, 0, 1));
}

public sealed class DoubleTween : Tween<double>
{
    public override double Lerp(double a, double b, double t) => a + (b - a) * t;
}

public sealed class ColorTween : Tween<Color>
{
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

    public AnimationController(TimeSpan duration)
    {
        Duration = duration <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : duration;
        _ticker = new Ticker(OnTick);
    }

    public void Forward(double? from = null)
    {
        if (from.HasValue) SetValue(from.Value);
        _reversing = false;
        _repeat = false;
        _repeatReverse = false;
        SetStatus(AnimationStatus.Forward);
        Start();
    }

    public void Reverse(double? from = null)
    {
        if (from.HasValue) SetValue(from.Value);
        _reversing = true;
        _repeat = false;
        _repeatReverse = false;
        SetStatus(AnimationStatus.Reverse);
        Start();
    }

    public void Repeat(bool reverse = false)
    {
        _repeat = true;
        _repeatReverse = reverse;
        _reversing = false;
        SetStatus(AnimationStatus.Forward);
        Start();
    }

    public void Stop()
    {
        IsAnimating = false;
        _ticker.Stop();
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
        TimeSpan effectiveDuration = _reversing ? ReverseDuration ?? Duration : Duration;
        double delta = dt.TotalSeconds / effectiveDuration.TotalSeconds;
        if (_reversing) delta = -delta;

        double raw = _value + delta;
        if (_repeat)
        {
            if (_repeatReverse)
            {
                // пинг-понг 0→1→0→1...
                if (raw >= 1)
                {
                    raw = 2 - raw;
                    _reversing = true;
                    SetStatus(AnimationStatus.Reverse);
                }
                else if (raw <= 0)
                {
                    raw = -raw;
                    _reversing = false;
                    SetStatus(AnimationStatus.Forward);
                }
            }
            else
            {
                raw %= 1.0;
                if (raw < 0) raw += 1.0;
            }
        }
        else
        {
            if (raw >= 1.0)
            {
                _value = 1.0;
                Changed?.Invoke();
                SetStatus(AnimationStatus.Completed);
                Completed?.Invoke();
                Stop();
                return;
            }

            if (raw <= 0.0)
            {
                _value = 0.0;
                Changed?.Invoke();
                SetStatus(AnimationStatus.Dismissed);
                Dismissed?.Invoke();
                Stop();
                return;
            }
        }

        _value = Math.Clamp(raw, 0, 1);
        Changed?.Invoke();
    }

    public double Evaluate() => Curve(Math.Clamp(Value, 0, 1));

    public void Dispose() => Stop();

    private void SetStatus(AnimationStatus status)
    {
        if (_status == status)
        {
            return;
        }

        _status = status;
        StatusChanged?.Invoke(status);
    }
}
