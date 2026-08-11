// Dart parity source: flutter/packages/flutter/lib/src/physics/simulation.dart

namespace Plumix.Physics;

/// <summary>
/// The base class for all simulations. A simulation models an object's position as a function of
/// time.
/// </summary>
/// <remarks>
/// Simulations may be stateful, so query them with non-decreasing time values.
/// </remarks>
public abstract class Simulation
{
    protected Simulation(Tolerance? tolerance = null)
    {
        Tolerance = tolerance ?? Tolerance.DefaultTolerance;
    }

    /// <summary>How close to the actual end of the simulation a value is considered final.</summary>
    public Tolerance Tolerance { get; set; }

    /// <summary>The position of the object in the simulation at the given time.</summary>
    public abstract double X(double time);

    /// <summary>The velocity of the object in the simulation at the given time.</summary>
    public abstract double DX(double time);

    /// <summary>Whether the simulation is "done" at the given time.</summary>
    public abstract bool IsDone(double time);

    public override string ToString() => GetType().Name;
}
