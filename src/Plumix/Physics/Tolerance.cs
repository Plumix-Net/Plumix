// Dart parity source: flutter/packages/flutter/lib/src/physics/tolerance.dart

namespace Plumix.Physics;

/// <summary>
/// Structure that specifies maximum allowable magnitudes for distances, durations, and velocity
/// differences to be considered equal.
/// </summary>
public sealed class Tolerance
{
    private const double EpsilonDefault = 1e-3;

    public Tolerance(
        double distance = EpsilonDefault,
        double time = EpsilonDefault,
        double velocity = EpsilonDefault)
    {
        Distance = distance;
        Time = time;
        Velocity = velocity;
    }

    /// <summary>A default tolerance of 0.001 for all three values.</summary>
    public static readonly Tolerance DefaultTolerance = new();

    public double Distance { get; }

    public double Time { get; }

    public double Velocity { get; }

    public override string ToString()
    {
        return $"Tolerance(distance: ±{Distance}, time: ±{Time}, velocity: ±{Velocity})";
    }
}
