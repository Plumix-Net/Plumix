using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/animation/tween.dart

namespace Plumix;

/// <summary>Signature for the <see cref="Animatable{T}.FromCallback"/> callback.</summary>
public delegate T AnimatableCallback<out T>(double value);

/// <summary>
/// An object that can produce a value of type <typeparamref name="T"/> given an
/// <see cref="Animation{T}"/> of <see cref="double"/> as input.
/// </summary>
public abstract class Animatable<T>
{
    protected Animatable()
    {
    }

    /// <summary>Create a new <see cref="Animatable{T}"/> from the provided <paramref name="callback"/>.</summary>
    public static Animatable<T> FromCallback(AnimatableCallback<T> callback) => new CallbackAnimatable<T>(callback);

    /// <summary>Returns the value of the object at point <paramref name="t"/>.</summary>
    public abstract T Transform(double t);

    /// <summary>The current value of this object for the given <paramref name="animation"/>.</summary>
    public T Evaluate(Animation<double> animation) => Transform(animation.Value);

    /// <summary>
    /// Returns a new <see cref="Animation{T}"/> that is driven by the given <paramref name="parent"/> but
    /// that takes on values determined by this object.
    /// </summary>
    public Animation<T> Animate(Animation<double> parent) => new AnimatedEvaluation<T>(parent, this);

    /// <summary>
    /// Returns a new <see cref="Animatable{T}"/> whose value is determined by first evaluating
    /// <paramref name="parent"/> and then evaluating this object at the result.
    /// </summary>
    public Animatable<T> Chain(Animatable<double> parent) => new ChainedEvaluation<T>(parent, this);
}

/// <summary>Dart's private <c>_CallbackAnimatable</c>, used by <see cref="Animatable{T}.FromCallback"/>.</summary>
internal sealed class CallbackAnimatable<T>(AnimatableCallback<T> callback) : Animatable<T>
{
    private readonly AnimatableCallback<T> _callback = callback;

    public override T Transform(double t) => _callback(t);
}

/// <summary>Dart's private <c>_AnimatedEvaluation</c>.</summary>
internal sealed class AnimatedEvaluation<T> : Animation<T>
{
    private readonly Animatable<T> _evaluatable;

    public AnimatedEvaluation(Animation<double> parent, Animatable<T> evaluatable)
    {
        Parent = parent;
        _evaluatable = evaluatable;
    }

    public Animation<double> Parent { get; }

    public override T Value => _evaluatable.Evaluate(Parent);

    public override AnimationStatus Status => Parent.Status;

    public override void AddListener(Action listener) => Parent.AddListener(listener);

    public override void RemoveListener(Action listener) => Parent.RemoveListener(listener);

    public override void AddStatusListener(Action<AnimationStatus> listener) => Parent.AddStatusListener(listener);

    public override void RemoveStatusListener(Action<AnimationStatus> listener)
    {
        Parent.RemoveStatusListener(listener);
    }

    public override string ToString() => $"{Parent}➩{_evaluatable}➩{Value}";
}

/// <summary>Dart's private <c>_ChainedEvaluation</c>.</summary>
internal sealed class ChainedEvaluation<T>(Animatable<double> parent, Animatable<T> evaluatable) : Animatable<T>
{
    private readonly Animatable<double> _parent = parent;
    private readonly Animatable<T> _evaluatable = evaluatable;

    public override T Transform(double t) => _evaluatable.Transform(_parent.Transform(t));

    public override string ToString() => $"{_parent}➩{_evaluatable}";
}

/// <summary>A linear interpolation between a beginning and ending value.</summary>
/// <remarks>
/// Dart's <c>begin</c>/<c>end</c> are <c>T?</c> fields. C#'s <c>T?</c> on an unconstrained type
/// parameter is only an annotation, so a value-type tween tracks whether each end is set: assigning
/// <c>null</c> (possible for reference and <see cref="Nullable{T}"/> types) clears it, and an unset end
/// reads as <c>default</c>. <see cref="DoubleTween"/> is the nullable spelling of <c>Tween&lt;double&gt;</c>.
/// Dart's default <see cref="Lerp"/> uses dynamic <c>+</c>, <c>-</c> and <c>*</c>; C# has no dynamic
/// operators over an unconstrained <typeparamref name="T"/>, so it covers the Plumix types that
/// implement them in Dart (<c>double</c>, <c>Offset</c>, <c>Size</c>, <c>EdgeInsets</c>, <c>Duration</c>)
/// and raises Dart's errors for the rest.
/// </remarks>
public class Tween<T> : Animatable<T>
{
    private T _begin = default!;
    private T _end = default!;

    /// <summary>Creates a tween with neither end set.</summary>
    public Tween()
    {
    }

    /// <summary>
    /// Creates a tween. A <c>null</c> <paramref name="begin"/>/<paramref name="end"/> leaves that end unset.
    /// </summary>
    public Tween(T? begin, T? end)
    {
        if (begin is not null)
        {
            SetBeginValue(begin);
        }

        if (end is not null)
        {
            SetEndValue(end);
        }
    }

    /// <summary>The value this variable has at the beginning of the animation.</summary>
    public virtual T? Begin
    {
        get => HasBeginValue ? _begin : default;
        set
        {
            if (value is null)
            {
                ClearBeginValue();
            }
            else
            {
                SetBeginValue(value);
            }
        }
    }

    /// <summary>The value this variable has at the end of the animation.</summary>
    public virtual T? End
    {
        get => HasEndValue ? _end : default;
        set
        {
            if (value is null)
            {
                ClearEndValue();
            }
            else
            {
                SetEndValue(value);
            }
        }
    }

    internal bool HasBeginValue { get; private set; }

    internal bool HasEndValue { get; private set; }

    /// <summary>Returns the value this variable has at the given animation clock value.</summary>
    /// <remarks>
    /// Dart's <c>@protected lerp</c>; public because <see cref="ReverseTween{T}"/> and tests call it.
    /// </remarks>
    public virtual T Lerp(double t)
    {
        DebugAssertions.Assert(HasBeginValue);
        DebugAssertions.Assert(HasEndValue);
        object? begin = Begin;
        object? end = End;
        object? result = begin switch
        {
            double b when end is double e => b + ((e - b) * t),
            Point b when end is Point e => new Point(b.X + ((e.X - b.X) * t), b.Y + ((e.Y - b.Y) * t)),
            Vector b when end is Vector e => new Vector(b.X + ((e.X - b.X) * t), b.Y + ((e.Y - b.Y) * t)),
            Size b when end is Size e => new Size(
                b.Width + ((e.Width - b.Width) * t),
                b.Height + ((e.Height - b.Height) * t)),
            EdgeInsets b when end is EdgeInsets e => new EdgeInsets(
                b.Left + ((e.Left - b.Left) * t),
                b.Top + ((e.Top - b.Top) * t),
                b.Right + ((e.Right - b.Right) * t),
                b.Bottom + ((e.Bottom - b.Bottom) * t)),
            TimeSpan b when end is TimeSpan e => b + TimeSpan.FromTicks((long)Math.Round((e - b).Ticks * t)),
            int b when end is int e => ThrowIntLerp(b, e, t),
            _ => ThrowUnsupportedLerp(),
        };
        return (T)result!;
    }

    /// <summary>
    /// Returns the interpolated value for the current value of the given animation: <see cref="Begin"/> at
    /// 0.0, <see cref="End"/> at 1.0, and <see cref="Lerp"/> otherwise.
    /// </summary>
    public override T Transform(double t)
    {
        if (t == 0.0)
        {
            return CastEnd(Begin, HasBeginValue);
        }

        if (t == 1.0)
        {
            return CastEnd(End, HasEndValue);
        }

        return Lerp(t);
    }

    public override string ToString()
    {
        string type = Diagnostics.ObjectRuntimeType(this, "Animatable");
        return $"{type}({Diagnostics.DescribeValue(Begin)} → {Diagnostics.DescribeValue(End)})";
    }

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

    // Dart's `begin as T`: a null end is a valid `T` only when `T` is nullable.
    private static T CastEnd(T? value, bool isSet)
    {
        if (!isSet && default(T) is not null)
        {
            throw new InvalidCastException(
                $"type 'Null' is not a subtype of type '{Diagnostics.DescribeType(typeof(T))}' in type cast");
        }

        return value!;
    }

    // The `on TypeError` branch of Dart's lerp assert: `int * double` yields a double.
    private object ThrowIntLerp(int begin, int end, double t)
    {
        double result = begin + ((end - begin) * t);
        if (!Constants.KDebugMode)
        {
            throw new InvalidCastException(
                $"type 'double' is not a subtype of type '{Diagnostics.DescribeType(typeof(T))}' in type cast");
        }

        throw new FlutterError(
        [
            new ErrorSummary($"Cannot lerp between \"{Describe(Begin)}\" and \"{Describe(End)}\"."),
            new ErrorDescription(
                $"The type {Diagnostics.DescribeType(begin.GetType())} returned a "
                + $"{Diagnostics.DescribeType(result.GetType())} after multiplication with a double value. "
                + ApiDocsHint),
            new ErrorHint("To lerp int values, consider IntTween or StepTween instead."),
        ]);
    }

    // The `on NoSuchMethodError` branch of Dart's lerp assert.
    private object ThrowUnsupportedLerp()
    {
        object? begin = Begin;
        object? end = End;
        string beginType = begin is null ? "Null" : Diagnostics.DescribeType(begin.GetType());
        if (!Constants.KDebugMode)
        {
            throw new MissingMethodException($"Class '{beginType}' has no instance method '+'.");
        }

        DiagnosticsNode hint;
        if (begin is Color || end is Color)
        {
            hint = new ErrorHint("To lerp colors, consider ColorTween instead.");
        }
        else if (begin is Rect || end is Rect)
        {
            hint = new ErrorHint("To lerp rects, consider RectTween instead.");
        }
        else
        {
            hint = new ErrorHint(
                $"There may be a dedicated \"{beginType}Tween\" for this type, or you may need to create one.");
        }

        throw new FlutterError(
        [
            new ErrorSummary($"Cannot lerp between \"{Describe(begin)}\" and \"{Describe(end)}\"."),
            new ErrorDescription(
                $"The type {beginType} might not fully implement `+`, `-`, and/or `*`. " + ApiDocsHint),
            hint,
        ]);
    }

    // Dart's string interpolation of an arbitrary object: `Instance of 'Object'` for a plain object.
    private static string Describe(object? value)
    {
        return value is not null && value.GetType() == typeof(object)
            ? "Instance of 'Object'"
            : Diagnostics.DescribeValue(value);
    }

    private const string ApiDocsHint =
        "See \"Types with special considerations\" at https://api.flutter.dev/flutter/animation/Tween-class.html "
        + "for more information.";
}

/// <summary>A <see cref="Tween{T}"/> that evaluates its <see cref="Parent"/> in reverse.</summary>
public class ReverseTween<T> : Tween<T>
{
    public ReverseTween(Tween<T> parent)
    {
        Parent = parent;
        if (parent.HasEndValue)
        {
            SetBeginValue(parent.GetEndValue());
        }

        if (parent.HasBeginValue)
        {
            SetEndValue(parent.GetBeginValue());
        }
    }

    /// <summary>This tween's value is the same as the parent's value evaluated in reverse.</summary>
    public Tween<T> Parent { get; }

    public override T Lerp(double t) => Parent.Lerp(1.0 - t);
}

/// <summary>An interpolation between two colors, using <see cref="Color.Lerp"/>.</summary>
public class ColorTween : Tween<Color?>
{
    public ColorTween(Color? begin = null, Color? end = null)
        : base(begin, end)
    {
    }

    public override Color? Lerp(double t) => Color.Lerp(Begin, End, t);
}

/// <summary>An interpolation between two sizes, using Dart's <c>Size.lerp</c>.</summary>
public class SizeTween : Tween<Size?>
{
    public SizeTween(Size? begin = null, Size? end = null)
        : base(begin, end)
    {
    }

    public override Size? Lerp(double t) => LerpSize(Begin, End, t);

    // dart:ui `Size.lerp`.
    internal static Size? LerpSize(Size? a, Size? b, double t)
    {
        if (b is not { } bSize)
        {
            return a is { } aOnly ? new Size(aOnly.Width * (1.0 - t), aOnly.Height * (1.0 - t)) : null;
        }

        if (a is not { } aSize)
        {
            return new Size(bSize.Width * t, bSize.Height * t);
        }

        return new Size(
            LerpDouble(aSize.Width, bSize.Width, t),
            LerpDouble(aSize.Height, bSize.Height, t));
    }

    // dart:ui `_lerpDouble`.
    internal static double LerpDouble(double a, double b, double t) => (a * (1.0 - t)) + (b * t);
}

/// <summary>An interpolation between two rectangles, using Dart's <c>Rect.lerp</c>.</summary>
public class RectTween : Tween<Rect?>
{
    public RectTween(Rect? begin = null, Rect? end = null)
        : base(begin, end)
    {
    }

    public override Rect? Lerp(double t) => LerpRect(Begin, End, t);

    // dart:ui `Rect.lerp`. Avalonia's `Rect` cannot hold a negative extent, so an inverted result
    // collapses to zero width/height (see DIVERGENCES, TextSelectionToolbarAnchors row).
    internal static Rect? LerpRect(Rect? a, Rect? b, double t)
    {
        if (b is not { } bRect)
        {
            if (a is not { } aOnly)
            {
                return null;
            }

            double k = 1.0 - t;
            return FromLTRB(aOnly.Left * k, aOnly.Top * k, aOnly.Right * k, aOnly.Bottom * k);
        }

        if (a is not { } aRect)
        {
            return FromLTRB(bRect.Left * t, bRect.Top * t, bRect.Right * t, bRect.Bottom * t);
        }

        return FromLTRB(
            SizeTween.LerpDouble(aRect.Left, bRect.Left, t),
            SizeTween.LerpDouble(aRect.Top, bRect.Top, t),
            SizeTween.LerpDouble(aRect.Right, bRect.Right, t),
            SizeTween.LerpDouble(aRect.Bottom, bRect.Bottom, t));
    }

    private static Rect FromLTRB(double left, double top, double right, double bottom)
    {
        return new Rect(left, top, Math.Max(0.0, right - left), Math.Max(0.0, bottom - top));
    }
}

/// <summary>An interpolation between two integers that rounds.</summary>
public class IntTween : Tween<int>
{
    public IntTween()
    {
    }

    public IntTween(int begin, int end)
        : base(begin, end)
    {
    }

    public override int Lerp(double t) => (int)Math.Round(Begin + ((End - Begin) * t), MidpointRounding.AwayFromZero);
}

/// <summary>An interpolation between two integers that floors.</summary>
public class StepTween : Tween<int>
{
    public StepTween()
    {
    }

    public StepTween(int begin, int end)
        : base(begin, end)
    {
    }

    public override int Lerp(double t) => (int)Math.Floor(Begin + ((End - Begin) * t));
}

/// <summary>A tween with a constant value.</summary>
public class ConstantTween<T> : Tween<T>
{
    public ConstantTween(T value)
        : base(value, value)
    {
    }

    public override T Lerp(double t) => Begin!;

    public override string ToString()
    {
        string type = Diagnostics.ObjectRuntimeType(this, "ConstantTween");
        return $"{type}(value: {Diagnostics.DescribeValue(Begin)})";
    }
}

/// <summary>Transforms the value of the given animation by the given curve.</summary>
public class CurveTween : Animatable<double>
{
    public CurveTween(Curve curve)
    {
        Curve = curve;
    }

    /// <summary>The curve to use when transforming the value of the animation.</summary>
    public Curve Curve { get; set; }

    public override double Transform(double t)
    {
        if (t == 0.0 || t == 1.0)
        {
            DebugAssertions.Assert(Math.Round(Curve.Transform(t), MidpointRounding.AwayFromZero) == t);
            return t;
        }

        return Curve.Transform(t);
    }

    public override string ToString() => $"{Diagnostics.ObjectRuntimeType(this, "CurveTween")}(curve: {Curve})";
}

/// <summary>
/// C#-only: the nullable spelling of Dart's <c>Tween&lt;double&gt;</c>, whose <c>begin</c>/<c>end</c> are
/// <c>double?</c> (see <see cref="Tween{T}"/>'s remarks).
/// </summary>
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
}

/// <summary>
/// C#-only: the nullable spelling of Dart's <c>Tween&lt;Offset&gt;</c> over Avalonia's <see cref="Vector"/>
/// (see <see cref="Tween{T}"/>'s remarks).
/// </summary>
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
}
