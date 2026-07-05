using Plumix.Foundation;

namespace Plumix;

// Dart parity source: flutter/packages/flutter/lib/src/animation/animation_style.dart

public sealed record AnimationStyle(
    TimeSpan? Duration = null,
    TimeSpan? ReverseDuration = null,
    Curve? Curve = null,
    Curve? ReverseCurve = null)
{
    public static AnimationStyle NoAnimation { get; } = new(
        TimeSpan.Zero,
        TimeSpan.Zero,
        Curves.Linear,
        Curves.Linear);
}
