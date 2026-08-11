// Dart parity source: flutter/packages/flutter/lib/src/physics/clamped_simulation.dart

namespace Plumix.Physics;

/// <summary>
/// A simulation that applies limits to another simulation.
/// </summary>
/// <remarks>
/// The limits are only applied to the other simulation's outputs: <see cref="IsDone"/> reports the
/// unclamped simulation's state.
/// </remarks>
public sealed class ClampedSimulation : Simulation
{
    public ClampedSimulation(
        Simulation simulation,
        double xMin = double.NegativeInfinity,
        double xMax = double.PositiveInfinity,
        double dxMin = double.NegativeInfinity,
        double dxMax = double.PositiveInfinity) : base(simulation.Tolerance)
    {
        if (xMax < xMin)
        {
            throw new ArgumentOutOfRangeException(nameof(xMax), "xMax must be greater than or equal to xMin.");
        }

        if (dxMax < dxMin)
        {
            throw new ArgumentOutOfRangeException(nameof(dxMax), "dxMax must be greater than or equal to dxMin.");
        }

        InnerSimulation = simulation;
        XMin = xMin;
        XMax = xMax;
        DXMin = dxMin;
        DXMax = dxMax;
    }

    /// <summary>The simulation being clamped.</summary>
    public Simulation InnerSimulation { get; }

    public double XMin { get; }

    public double XMax { get; }

    public double DXMin { get; }

    public double DXMax { get; }

    public override double X(double time) => Math.Clamp(InnerSimulation.X(time), XMin, XMax);

    public override double DX(double time) => Math.Clamp(InnerSimulation.DX(time), DXMin, DXMax);

    public override bool IsDone(double time) => InnerSimulation.IsDone(time);

    public override string ToString()
    {
        return $"ClampedSimulation(simulation: {InnerSimulation}, "
               + $"x: {XMin:F1}..{XMax:F1}, dx: {DXMin:F1}..{DXMax:F1})";
    }
}
