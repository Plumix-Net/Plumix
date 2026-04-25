using Avalonia;
using Avalonia.Media;

// Dart parity source (reference): flutter/packages/flutter/lib/src/animation/animation_controller.dart (approximate)

namespace Plumix;

// ---------- Animation primitives ----------
public delegate double Curve(double t);

public static class Curves
{
    public static double Linear(double t) => t;

    public static double EaseInOut(double t)
    {
        // простая S-кривая (smoothstep)
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }

    public static double EaseIn(double t) => t * t;
    public static double EaseOut(double t) => 1 - (1 - t) * (1 - t);
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
        var x = a.X + ((b.X - a.X) * t);
        var y = a.Y + ((b.Y - a.Y) * t);
        var width = a.Width + ((b.Width - a.Width) * t);
        var height = a.Height + ((b.Height - a.Height) * t);
        return new Rect(x, y, Math.Max(0, width), Math.Max(0, height));
    }
}

public sealed class AnimationController : IDisposable
{
    public event Action? Changed;
    public event Action? Completed;
    public event Action? Dismissed;

    public double Value { get; private set; } // 0..1
    public bool IsAnimating { get; private set; }
    public TimeSpan Duration { get; }
    public Curve Curve { get; set; } = Curves.Linear;

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
        if (from.HasValue) Value = Math.Clamp(from.Value, 0, 1);
        _reversing = false;
        _repeat = false;
        _repeatReverse = false;
        Start();
    }

    public void Reverse(double? from = null)
    {
        if (from.HasValue) Value = Math.Clamp(from.Value, 0, 1);
        _reversing = true;
        _repeat = false;
        _repeatReverse = false;
        Start();
    }

    public void Repeat(bool reverse = false)
    {
        _repeat = true;
        _repeatReverse = reverse;
        _reversing = false;
        Start();
    }

    public void Stop()
    {
        IsAnimating = false;
        _ticker.Stop();
    }

    private void Start()
    {
        if (IsAnimating) return;
        IsAnimating = true;
        _ticker.Start();
    }

    private void OnTick(TimeSpan dt)
    {
        double delta = dt.TotalSeconds / Duration.TotalSeconds;
        if (_reversing) delta = -delta;

        double raw = Value + delta;
        if (_repeat)
        {
            if (_repeatReverse)
            {
                // пинг-понг 0→1→0→1...
                if (raw >= 1)
                {
                    raw = 2 - raw;
                    _reversing = true;
                }
                else if (raw <= 0)
                {
                    raw = -raw;
                    _reversing = false;
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
                Value = 1.0;
                Changed?.Invoke();
                Completed?.Invoke();
                Stop();
                return;
            }

            if (raw <= 0.0)
            {
                Value = 0.0;
                Changed?.Invoke();
                Dismissed?.Invoke();
                Stop();
                return;
            }
        }

        Value = Math.Clamp(raw, 0, 1);
        Changed?.Invoke();
    }

    public double Evaluate() => Curve(Math.Clamp(Value, 0, 1));

    public void Dispose() => Stop();
}
