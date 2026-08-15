using Avalonia;
using Avalonia.Media;
using Plumix.Physics;

// Dart parity source: flutter/packages/flutter/lib/src/animation/animation_controller.dart

namespace Plumix;

// ---------- Animation primitives ----------
public delegate double Curve(double t);

public static class Curves
{
    public static double Linear(double t) => t;

    public static double Decelerate(double t)
    {
        double clamped = Math.Clamp(t, 0.0, 1.0);
        return 1.0 - ((1.0 - clamped) * (1.0 - clamped));
    }

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

    /// <summary>
    /// Remaps the <em>output</em> of <paramref name="curve"/> into
    /// <c>[<paramref name="begin"/>, <paramref name="end"/>]</c>, unlike <see cref="Interval"/>
    /// which remaps the input.
    /// </summary>
    /// <remarks>Ports the private `_TweenCurve` of Flutter's `material/menu_anchor.dart`.</remarks>
    public static Curve TweenCurve(double begin, double end, Curve? curve = null)
    {
        if (begin < 0.0 || begin > 1.0 || end < 0.0 || end > 1.0 || end < begin)
        {
            throw new ArgumentOutOfRangeException(nameof(begin));
        }

        Curve effectiveCurve = curve ?? Linear;
        return t => begin + ((end - begin) * effectiveCurve(t));
    }

    public static Curve Threshold(double threshold)
    {
        if (threshold < 0.0 || threshold > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold));
        }

        return t =>
        {
            double clampedT = Math.Clamp(t, 0.0, 1.0);
            if (clampedT is 0.0 or 1.0)
            {
                return clampedT;
            }

            return clampedT < threshold ? 0.0 : 1.0;
        };
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

    // Flutter Curves.easeOutBack: Cubic(0.175, 0.885, 0.32, 1.275).
    public static Curve EaseOutBack { get; } = Cubic(0.175, 0.885, 0.32, 1.275);

    // Flutter Curves.easeOutCubic: Cubic(0.33, 1.0, 0.68, 1.0).
    public static Curve EaseOutCubic { get; } = Cubic(0.33, 1.0, 0.68, 1.0);

    // Flutter Curves.easeOutCirc: Cubic(0.075, 0.82, 0.165, 1.0).
    public static Curve EaseOutCirc { get; } = Cubic(0.075, 0.82, 0.165, 1.0);

    // Flutter Curves.easeInCirc: Cubic(0.6, 0.04, 0.98, 0.335).
    public static Curve EaseInCirc { get; } = Cubic(0.6, 0.04, 0.98, 0.335);

    // Flutter Curves.easeInOutQuart: Cubic(0.77, 0.0, 0.175, 1.0).
    public static Curve EaseInOutQuart { get; } = Cubic(0.77, 0.0, 0.175, 1.0);

    // Flutter Material Easing.legacyDecelerate: Cubic(0.0, 0.0, 0.2, 1.0).
    public static Curve LegacyDecelerate { get; } = Cubic(0.0, 0.0, 0.2, 1.0);

    /// <summary>
    /// A curve that progresses through <paramref name="beginCurve"/> until <paramref name="split"/> and
    /// through <paramref name="endCurve"/> afterwards, mapping each segment onto the matching output range.
    /// </summary>
    public static Curve Split(double split, Curve? beginCurve = null, Curve? endCurve = null)
    {
        if (!double.IsFinite(split) || split is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(split), "Split must be between 0.0 and 1.0.");
        }

        Curve begin = beginCurve ?? Linear;
        Curve end = endCurve ?? EaseOutCubic;
        return t =>
        {
            double clamped = Math.Clamp(t, 0.0, 1.0);
            if (clamped is 0.0 or 1.0 || clamped == split)
            {
                return clamped;
            }

            if (clamped < split)
            {
                return begin(clamped / split) * split;
            }

            return split + (end((clamped - split) / (1.0 - split)) * (1.0 - split));
        };
    }

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

    public static Curve ThreePointCubic(
        Point firstControlPoint,
        Point firstEndPoint,
        Point midpoint,
        Point secondControlPoint,
        Point secondEndPoint)
    {
        if (midpoint.X <= 0.0 || midpoint.X >= 1.0 || midpoint.Y <= 0.0 || midpoint.Y >= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(midpoint));
        }

        Curve firstCurve = Cubic(
            firstControlPoint.X / midpoint.X,
            firstControlPoint.Y / midpoint.Y,
            firstEndPoint.X / midpoint.X,
            firstEndPoint.Y / midpoint.Y);
        Curve secondCurve = Cubic(
            (secondControlPoint.X - midpoint.X) / (1.0 - midpoint.X),
            (secondControlPoint.Y - midpoint.Y) / (1.0 - midpoint.Y),
            (secondEndPoint.X - midpoint.X) / (1.0 - midpoint.X),
            (secondEndPoint.Y - midpoint.Y) / (1.0 - midpoint.Y));
        return t =>
        {
            double clamped = Math.Clamp(t, 0.0, 1.0);
            if (clamped < midpoint.X)
            {
                return firstCurve(clamped / midpoint.X) * midpoint.Y;
            }

            double transformed = (clamped - midpoint.X) / (1.0 - midpoint.X);
            return midpoint.Y + (secondCurve(transformed) * (1.0 - midpoint.Y));
        };
    }

    public static Curve EaseInOutCubicEmphasized { get; } = ThreePointCubic(
        new Point(0.05, 0.0),
        new Point(0.133333, 0.06),
        new Point(0.166666, 0.4),
        new Point(0.208333, 0.82),
        new Point(0.25, 1.0));

    // Flutter Curves.fastEaseInToSlowEaseOut.
    public static Curve FastEaseInToSlowEaseOut { get; } = ThreePointCubic(
        new Point(0.056, 0.024),
        new Point(0.108, 0.3085),
        new Point(0.198, 0.541),
        new Point(0.3655, 1.0),
        new Point(0.5465, 0.989));

    // Flutter Curves.easeInToLinear: Cubic(0.67, 0.03, 0.65, 0.09).
    public static Curve EaseInToLinear { get; } = Cubic(0.67, 0.03, 0.65, 0.09);

    // Flutter Curves.linearToEaseOut: Cubic(0.35, 0.91, 0.33, 0.97).
    public static Curve LinearToEaseOut { get; } = Cubic(0.35, 0.91, 0.33, 0.97);

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

    /// <summary>
    /// Returns a new <see cref="Animatable{T}"/> whose value is determined by first evaluating
    /// <paramref name="parent"/> and then evaluating this object.
    /// </summary>
    public Animatable<T> Chain(Animatable<double> parent)
    {
        return new ChainedEvaluation<T>(
            parent ?? throw new ArgumentNullException(nameof(parent)),
            this);
    }
}

internal sealed class ChainedEvaluation<T> : Animatable<T>
{
    private readonly Animatable<double> _parent;
    private readonly Animatable<T> _evaluatable;

    public ChainedEvaluation(Animatable<double> parent, Animatable<T> evaluatable)
    {
        _parent = parent;
        _evaluatable = evaluatable;
    }

    public override T Transform(double t) => _evaluatable.Transform(_parent.Transform(t));
}

public static class AnimationDriveExtensions
{
    /// <summary>
    /// Chains a <see cref="Tween{T}"/> (or any <see cref="Animatable{T}"/>) to this animation. Ports
    /// Flutter's <c>Animation&lt;double&gt;.drive</c>.
    /// </summary>
    public static Animation<TResult> Drive<TResult>(this Animation<double> animation, Animatable<TResult> child)
    {
        ArgumentNullException.ThrowIfNull(child);
        return child.Animate(animation ?? throw new ArgumentNullException(nameof(animation)));
    }
}

public sealed class ConstantTween<T> : Tween<T>
{
    public ConstantTween(T value)
    {
        Begin = value;
        End = value;
    }

    public override T Lerp(T a, T b, double t)
    {
        _ = b;
        _ = t;
        return a;
    }
}

public sealed class CurveTween : Tween<double>
{
    public CurveTween(Curve curve)
    {
        Curve = curve ?? throw new ArgumentNullException(nameof(curve));
        Begin = 0.0;
        End = 1.0;
    }

    public Curve Curve { get; }

    public override double Lerp(double a, double b, double t)
    {
        return a + ((b - a) * Curve(Math.Clamp(t, 0.0, 1.0)));
    }
}

public sealed record TweenSequenceItem<T>
{
    public TweenSequenceItem(Animatable<T> tween, double weight)
    {
        Tween = tween ?? throw new ArgumentNullException(nameof(tween));
        Weight = weight > 0.0 && double.IsFinite(weight)
            ? weight
            : throw new ArgumentOutOfRangeException(nameof(weight));
    }

    public Animatable<T> Tween { get; }

    public double Weight { get; }
}

public sealed class TweenSequence<T> : Animatable<T>
{
    private readonly IReadOnlyList<TweenSequenceEntry> _entries;

    public TweenSequence(IReadOnlyList<TweenSequenceItem<T>> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
        {
            throw new ArgumentException("A tween sequence needs at least one item.", nameof(items));
        }

        double totalWeight = items.Sum(static item => item.Weight);
        double start = 0.0;
        var entries = new List<TweenSequenceEntry>(items.Count);
        foreach (TweenSequenceItem<T> item in items)
        {
            double end = start + (item.Weight / totalWeight);
            entries.Add(new TweenSequenceEntry(item.Tween, start, end));
            start = end;
        }

        _entries = entries;
    }

    public override T Transform(double t)
    {
        double clamped = Math.Clamp(t, 0.0, 1.0);
        TweenSequenceEntry entry = _entries[^1];
        foreach (TweenSequenceEntry candidate in _entries)
        {
            if (clamped <= candidate.End)
            {
                entry = candidate;
                break;
            }
        }

        double localT = entry.End == entry.Start
            ? 1.0
            : Math.Clamp((clamped - entry.Start) / (entry.End - entry.Start), 0.0, 1.0);
        return entry.Tween.Transform(localT);
    }

    private sealed record TweenSequenceEntry(Animatable<T> Tween, double Start, double End);
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

public sealed class VectorTween : Tween<Vector>
{
    public VectorTween(Vector? begin = null, Vector? end = null)
    {
        Begin = begin;
        End = end;
    }

    public new Vector? Begin
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

    public new Vector? End
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

    public override Vector Lerp(Vector a, Vector b, double t)
    {
        return new Vector(
            a.X + ((b.X - a.X) * t),
            a.Y + ((b.Y - a.Y) * t));
    }
}

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

    /// <summary>
    /// Whether the platform asks for animations to be disabled. Dart parity source:
    /// <c>SemanticsBinding.instance.disableAnimations</c>; Plumix has no bindings, so this is a static
    /// hook that hosts set and tests override the way Flutter's
    /// <c>debugSemanticsDisableAnimations</c> does.
    /// </summary>
    public static bool DisableAnimations { get; set; }

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
    public double Evaluate() => Curve(Math.Clamp(_value, 0.0, 1.0));

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

        _ticker.Dispose();
        _ticker = null;
        _statusListeners.Clear();
        _listeners.Clear();
        Completed = null;
        Dismissed = null;
    }

    public override string ToString()
    {
        string paused = IsAnimating ? string.Empty : "; paused";
        string ticker = _ticker is null ? "; DISPOSED" : _ticker.Muted ? "; silenced" : string.Empty;
        string label = DebugLabel is null ? string.Empty : $"; for {DebugLabel}";
        string glyph = _status switch
        {
            AnimationStatus.Forward => "▶",
            AnimationStatus.Reverse => "◀",
            AnimationStatus.Completed => "⏭",
            _ => "⏮",
        };
        string value = _value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        return $"{nameof(AnimationController)}({glyph} {value}{paused}{ticker}{label})";
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
            AnimationBehavior.Normal => !DisableAnimations,
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
            _ => _begin + ((_end - _begin) * _curve(t)),
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
