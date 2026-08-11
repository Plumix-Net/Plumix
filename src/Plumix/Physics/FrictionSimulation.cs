// Dart parity source: flutter/packages/flutter/lib/src/physics/friction_simulation.dart

namespace Plumix.Physics;

/// <summary>A simulation that applies a drag to slow a particle down.</summary>
/// <remarks>
/// Models a particle affected by fluid drag, e.g. air resistance. The <c>drag</c> constant is the
/// ratio of the identically named parameter of a friction simulation.
/// </remarks>
public class FrictionSimulation : Simulation
{
    private readonly double _drag;
    private readonly double _dragLog;
    private readonly double _x;
    private readonly double _v;
    private readonly double _constantDeceleration;

    // Needs to be infinity for the Newton's method call in the constructor.
    private double _finalTime = double.PositiveInfinity;

    public FrictionSimulation(
        double drag,
        double position,
        double velocity,
        Tolerance? tolerance = null,
        double constantDeceleration = 0) : base(tolerance)
    {
        _drag = drag;
        _dragLog = Math.Log(drag);
        _x = position;
        _v = velocity;
        _constantDeceleration = constantDeceleration * PhysicsUtils.Sign(velocity);

        _finalTime = NewtonsMethod(
            initialGuess: 0,
            target: 0,
            f: DX,
            df: time => (_v * Math.Pow(_drag, time) * _dragLog) - _constantDeceleration,
            iterations: 10);
    }

    /// <summary>
    /// Creates a friction simulation with the specified positions and velocities.
    /// </summary>
    public static FrictionSimulation Through(
        double startPosition,
        double endPosition,
        double startVelocity,
        double endVelocity)
    {
        return new FrictionSimulation(
            DragFor(startPosition, endPosition, startVelocity, endVelocity),
            startPosition,
            startVelocity,
            tolerance: new Tolerance(velocity: Math.Abs(endVelocity)));
    }

    /// <summary>The value of <see cref="X"/> at <c>double.PositiveInfinity</c>.</summary>
    public double FinalX
    {
        get
        {
            if (_constantDeceleration == 0)
            {
                return _x - (_v / _dragLog);
            }

            return X(_finalTime);
        }
    }

    public override double X(double time)
    {
        if (time > _finalTime)
        {
            return FinalX;
        }

        return _x
               + (_v * Math.Pow(_drag, time) / _dragLog)
               - (_v / _dragLog)
               - (_constantDeceleration / 2 * time * time);
    }

    public override double DX(double time)
    {
        if (time > _finalTime)
        {
            return 0;
        }

        return (_v * Math.Pow(_drag, time)) - (_constantDeceleration * time);
    }

    /// <summary>The time at which the value of <c>x(time)</c> will equal <paramref name="x"/>.</summary>
    /// <returns><c>double.PositiveInfinity</c> if the simulation never reaches <paramref name="x"/>.</returns>
    public double TimeAtX(double x)
    {
        if (x == _x)
        {
            return 0.0;
        }

        if (_v == 0.0 || (_v > 0 ? x < _x || x > FinalX : x > _x || x < FinalX))
        {
            return double.PositiveInfinity;
        }

        return NewtonsMethod(target: x, initialGuess: 0, f: X, df: DX, iterations: 10);
    }

    public override bool IsDone(double time) => Math.Abs(DX(time)) < Tolerance.Velocity;

    public override string ToString()
    {
        return $"FrictionSimulation(cₓ: {_drag:F1}, x₀: {_x:F1}, dx₀: {_v:F1})";
    }

    private static double DragFor(
        double startPosition,
        double endPosition,
        double startVelocity,
        double endVelocity)
    {
        return Math.Pow(Math.E, (startVelocity - endVelocity) / (startPosition - endPosition));
    }

    private static double NewtonsMethod(
        double initialGuess,
        double target,
        Func<double, double> f,
        Func<double, double> df,
        int iterations)
    {
        double guess = initialGuess;
        for (int i = 0; i < iterations; i++)
        {
            guess -= (f(guess) - target) / df(guess);
        }

        return guess;
    }
}

/// <summary>A <see cref="FrictionSimulation"/> that clamps the particle to a range of positions.</summary>
public sealed class BoundedFrictionSimulation : FrictionSimulation
{
    private readonly double _minX;
    private readonly double _maxX;

    public BoundedFrictionSimulation(
        double drag,
        double position,
        double velocity,
        double minX,
        double maxX) : base(drag, position, velocity)
    {
        if (Math.Clamp(position, minX, maxX) != position)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "position must be within [minX, maxX].");
        }

        _minX = minX;
        _maxX = maxX;
    }

    public override double X(double time) => Math.Clamp(base.X(time), _minX, _maxX);

    public override bool IsDone(double time)
    {
        return base.IsDone(time)
               || Math.Abs(X(time) - _minX) < Tolerance.Distance
               || Math.Abs(X(time) - _maxX) < Tolerance.Distance;
    }

    public override string ToString()
    {
        return $"BoundedFrictionSimulation(x: {_minX:F1}..{_maxX:F1})";
    }
}
