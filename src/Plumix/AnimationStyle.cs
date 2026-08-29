using Plumix.Foundation;

namespace Plumix;

// Dart parity source: flutter/packages/flutter/lib/src/animation/animation_style.dart

/// Used to override the default parameters of an animation.
///
/// If <see cref="Duration"/> and <see cref="ReverseDuration"/> are set to
/// <see cref="TimeSpan.Zero"/>, the corresponding animation will be disabled.
///
/// All of the parameters are optional. If no parameters are specified, the
/// default animation will be used.
public sealed record AnimationStyle(
    TimeSpan? Duration = null,
    TimeSpan? ReverseDuration = null,
    Curve? Curve = null,
    Curve? ReverseCurve = null) : IDiagnosticable
{
    /// An instance of Animation Style class with no animation.
    public static AnimationStyle NoAnimation { get; } = new(TimeSpan.Zero, TimeSpan.Zero);

    /// Creates a new [AnimationStyle] based on the current selection, with the
    /// provided parameters overridden.
    public AnimationStyle CopyWith(
        TimeSpan? duration = null,
        TimeSpan? reverseDuration = null,
        Curve? curve = null,
        Curve? reverseCurve = null)
    {
        return new AnimationStyle(
            Duration: duration ?? Duration,
            ReverseDuration: reverseDuration ?? ReverseDuration,
            Curve: curve ?? Curve,
            ReverseCurve: reverseCurve ?? ReverseCurve);
    }

    /// Creates a new [AnimationStyle] that is a combination of this animation
    /// style and the given `other` animation style.
    ///
    /// If `other` is non-null, its non-null properties are used to override the
    /// corresponding properties of this style. Returns this animation style if
    /// `other` is null.
    public AnimationStyle Merge(AnimationStyle? other)
    {
        if (other is null)
        {
            return this;
        }

        return CopyWith(
            duration: other.Duration,
            reverseDuration: other.ReverseDuration,
            curve: other.Curve,
            reverseCurve: other.ReverseCurve);
    }

    /// Linearly interpolate between two animation styles.
    public static AnimationStyle? Lerp(AnimationStyle? a, AnimationStyle? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new AnimationStyle(
            Duration: LerpValue(a?.Duration, b?.Duration, t, LerpDuration),
            ReverseDuration: LerpValue(a?.ReverseDuration, b?.ReverseDuration, t, LerpDuration),
            Curve: LerpValue(a?.Curve, b?.Curve, t, LerpedCurve),
            ReverseCurve: LerpValue(a?.ReverseCurve, b?.ReverseCurve, t, LerpedCurve));
    }

    private static TimeSpan? LerpValue(
        TimeSpan? a,
        TimeSpan? b,
        double t,
        Func<TimeSpan?, TimeSpan?, double, TimeSpan> lerp)
    {
        if (a == b || t == 0.0)
        {
            return a;
        }

        return t == 1.0 ? b : lerp(a, b, t);
    }

    private static Curve? LerpValue(
        Curve? a,
        Curve? b,
        double t,
        Func<Curve?, Curve?, double, Curve> lerp)
    {
        if (a == b || t == 0.0)
        {
            return a;
        }

        return t == 1.0 ? b : lerp(a, b, t);
    }

    private static TimeSpan LerpDuration(TimeSpan? a, TimeSpan? b, double t)
    {
        double microseconds = ((a?.Ticks ?? 0) / (double)TimeSpan.TicksPerMicrosecond * (1.0 - t))
                              + ((b?.Ticks ?? 0) / (double)TimeSpan.TicksPerMicrosecond * t);
        return TimeSpan.FromTicks((long)Math.Round(microseconds, MidpointRounding.AwayFromZero)
                                  * TimeSpan.TicksPerMicrosecond);
    }

    /// Ports Dart's private `_LerpedCurve`: the weighted average of the two
    /// curves' transforms, with a null curve standing in as `Curves.linear`.
    private static Curve LerpedCurve(Curve? a, Curve? b, double t)
    {
        Curve first = a ?? Curves.Linear;
        Curve second = b ?? Curves.Linear;
        return progress => (first(progress) * (1.0 - t)) + (second(progress) * t);
    }

    /// <inheritdoc />
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        properties.Add(new DiagnosticsProperty<Curve>("curve", Curve, defaultValue: null));
        properties.Add(new DiagnosticsProperty<TimeSpan?>("duration", Duration, defaultValue: null));
        properties.Add(new DiagnosticsProperty<Curve>("reverseCurve", ReverseCurve, defaultValue: null));
        properties.Add(
            new DiagnosticsProperty<TimeSpan?>("reverseDuration", ReverseDuration, defaultValue: null));
    }
}
