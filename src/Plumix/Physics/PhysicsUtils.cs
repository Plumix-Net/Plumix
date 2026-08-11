// Dart parity source: flutter/packages/flutter/lib/src/physics/utils.dart

namespace Plumix.Physics;

public static class PhysicsUtils
{
    /// <summary>Whether two doubles are within a given distance of each other.</summary>
    public static bool NearEqual(double? a, double? b, double epsilon)
    {
        if (epsilon < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must not be negative.");
        }

        if (a is null || b is null)
        {
            return a is null && b is null;
        }

        return (a > b - epsilon && a < b + epsilon) || a == b;
    }

    /// <summary>Whether a double is within a given distance of zero.</summary>
    public static bool NearZero(double a, double epsilon) => NearEqual(a, 0.0, epsilon);

    /// <summary>
    /// Dart's <c>double.sign</c>: 1.0, -1.0, the value itself for zeroes, and NaN for NaN.
    /// </summary>
    internal static double Sign(double value)
    {
        if (value > 0.0)
        {
            return 1.0;
        }

        return value < 0.0 ? -1.0 : value;
    }
}
