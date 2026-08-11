// Dart parity source: flutter/packages/flutter/lib/src/physics/spring_simulation.dart

namespace Plumix.Physics;

/// <summary>A spring description, in terms of its mass, stiffness, and damping coefficient.</summary>
public sealed class SpringDescription
{
    private const double MillisecondsPerSecond = 1000.0;

    public SpringDescription(double mass, double stiffness, double damping)
    {
        Mass = mass;
        Stiffness = stiffness;
        Damping = damping;
    }

    /// <summary>Creates a spring given the mass, stiffness, and damping ratio.</summary>
    /// <param name="ratio">1.0 is critically damped, &gt; 1.0 overdamped, &lt; 1.0 underdamped.</param>
    public static SpringDescription WithDampingRatio(double mass, double stiffness, double ratio = 1.0)
    {
        return new SpringDescription(mass, stiffness, ratio * 2.0 * Math.Sqrt(mass * stiffness));
    }

    /// <summary>Creates a spring with the specified duration and bounce.</summary>
    public static SpringDescription WithDurationAndBounce(TimeSpan? duration = null, double bounce = 0.0)
    {
        TimeSpan effectiveDuration = duration ?? TimeSpan.FromMilliseconds(500);
        int milliseconds = (int)effectiveDuration.TotalMilliseconds;
        if (milliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive");
        }

        double durationInSeconds = milliseconds / MillisecondsPerSecond;
        const double mass = 1.0;
        double stiffness = 4 * Math.PI * Math.PI * mass / Math.Pow(durationInSeconds, 2);
        double dampingRatio = bounce > 0 ? 1.0 - bounce : 1 / (bounce + 1);
        double damping = dampingRatio * 2.0 * Math.Sqrt(mass * stiffness);
        return new SpringDescription(mass, stiffness, damping);
    }

    /// <summary>The mass of the spring (m). The units are arbitrary, but all springs must use the same.</summary>
    public double Mass { get; }

    /// <summary>The spring constant (k). The units of stiffness are M/T², where M is the mass unit.</summary>
    public double Stiffness { get; }

    /// <summary>The damping coefficient (c), not to be confused with the damping ratio.</summary>
    public double Damping { get; }

    /// <summary>The natural period of the spring, ignoring damping.</summary>
    public TimeSpan Duration
    {
        get
        {
            double durationInSeconds = Math.Sqrt(4 * Math.PI * Math.PI * Mass / Stiffness);
            return TimeSpan.FromMilliseconds(Math.Round(
                durationInSeconds * MillisecondsPerSecond,
                MidpointRounding.AwayFromZero));
        }
    }

    /// <summary>The bounce of the spring, derived from its damping ratio.</summary>
    public double Bounce
    {
        get
        {
            double dampingRatio = Damping / (2.0 * Math.Sqrt(Mass * Stiffness));
            return dampingRatio < 1.0 ? 1.0 - dampingRatio : (1 / dampingRatio) - 1;
        }
    }

    public override string ToString()
    {
        return $"SpringDescription(mass: {Mass:F1}, stiffness: {Stiffness:F1}, damping: {Damping:F1})";
    }
}

/// <summary>The kind of spring, as determined by its damping ratio.</summary>
public enum SpringType
{
    /// <summary>The spring does not oscillate and returns to its equilibrium as quickly as possible.</summary>
    CriticallyDamped,

    /// <summary>The spring oscillates around its equilibrium with a decaying amplitude.</summary>
    UnderDamped,

    /// <summary>The spring returns to its equilibrium without oscillating, more slowly than critical.</summary>
    OverDamped,
}

/// <summary>A spring simulation, mapping a particle attached to a spring to its position over time.</summary>
public class SpringSimulation : Simulation
{
    private readonly ISpringSolution _solution;
    private readonly bool _snapToEnd;

    public SpringSimulation(
        SpringDescription spring,
        double start,
        double end,
        double velocity,
        bool snapToEnd = false,
        Tolerance? tolerance = null) : base(tolerance)
    {
        EndPosition = end;
        _solution = SpringSolution.Create(spring, start - end, velocity);
        _snapToEnd = snapToEnd;
    }

    /// <summary>The position where the particle comes to rest.</summary>
    protected double EndPosition { get; }

    /// <summary>The kind of spring this simulation describes.</summary>
    public SpringType Type => _solution.Type;

    public override double X(double time)
    {
        return _snapToEnd && IsDone(time) ? EndPosition : EndPosition + _solution.X(time);
    }

    public override double DX(double time)
    {
        return _snapToEnd && IsDone(time) ? 0 : _solution.DX(time);
    }

    public override bool IsDone(double time)
    {
        return PhysicsUtils.NearZero(_solution.X(time), Tolerance.Distance)
               && PhysicsUtils.NearZero(_solution.DX(time), Tolerance.Velocity);
    }

    public override string ToString() => $"SpringSimulation(end: {EndPosition:F1}, {Type})";
}

/// <summary>
/// A <see cref="SpringSimulation"/> where the value of <see cref="X"/> is guaranteed to have exactly
/// the end value when the simulation is done.
/// </summary>
public sealed class ScrollSpringSimulation : SpringSimulation
{
    public ScrollSpringSimulation(
        SpringDescription spring,
        double start,
        double end,
        double velocity,
        Tolerance? tolerance = null) : base(spring, start, end, velocity, tolerance: tolerance)
    {
    }

    public override double X(double time) => IsDone(time) ? EndPosition : base.X(time);
}

internal interface ISpringSolution
{
    double X(double time);

    double DX(double time);

    SpringType Type { get; }
}

internal static class SpringSolution
{
    public static ISpringSolution Create(
        SpringDescription spring,
        double initialPosition,
        double initialVelocity)
    {
        double cmk = (spring.Damping * spring.Damping) - (4 * spring.Mass * spring.Stiffness);
        if (cmk > 0.0)
        {
            return new OverdampedSolution(spring, initialPosition, initialVelocity);
        }

        if (cmk < 0.0)
        {
            return new UnderdampedSolution(spring, initialPosition, initialVelocity);
        }

        return new CriticalSolution(spring, initialPosition, initialVelocity);
    }
}

internal sealed class CriticalSolution : ISpringSolution
{
    private readonly double _r;
    private readonly double _c1;
    private readonly double _c2;

    public CriticalSolution(SpringDescription spring, double distance, double velocity)
    {
        _r = -spring.Damping / (2.0 * spring.Mass);
        _c1 = distance;
        _c2 = velocity - (_r * distance);
    }

    public SpringType Type => SpringType.CriticallyDamped;

    public double X(double time) => (_c1 + (_c2 * time)) * Math.Pow(Math.E, _r * time);

    public double DX(double time)
    {
        double power = Math.Pow(Math.E, _r * time);
        return (_r * (_c1 + (_c2 * time)) * power) + (_c2 * power);
    }
}

internal sealed class OverdampedSolution : ISpringSolution
{
    private readonly double _r1;
    private readonly double _r2;
    private readonly double _c1;
    private readonly double _c2;

    public OverdampedSolution(SpringDescription spring, double distance, double velocity)
    {
        double cmk = (spring.Damping * spring.Damping) - (4 * spring.Mass * spring.Stiffness);
        _r1 = (-spring.Damping - Math.Sqrt(cmk)) / (2.0 * spring.Mass);
        _r2 = (-spring.Damping + Math.Sqrt(cmk)) / (2.0 * spring.Mass);
        _c2 = (velocity - (_r1 * distance)) / (_r2 - _r1);
        _c1 = distance - _c2;
    }

    public SpringType Type => SpringType.OverDamped;

    public double X(double time)
    {
        return (_c1 * Math.Pow(Math.E, _r1 * time)) + (_c2 * Math.Pow(Math.E, _r2 * time));
    }

    public double DX(double time)
    {
        return (_c1 * _r1 * Math.Pow(Math.E, _r1 * time)) + (_c2 * _r2 * Math.Pow(Math.E, _r2 * time));
    }
}

internal sealed class UnderdampedSolution : ISpringSolution
{
    private readonly double _w;
    private readonly double _r;
    private readonly double _c1;
    private readonly double _c2;

    public UnderdampedSolution(SpringDescription spring, double distance, double velocity)
    {
        _w = Math.Sqrt((4.0 * spring.Mass * spring.Stiffness) - (spring.Damping * spring.Damping))
             / (2.0 * spring.Mass);
        _r = -(spring.Damping / 2.0 / spring.Mass);
        _c1 = distance;
        _c2 = (velocity - (_r * distance)) / _w;
    }

    public SpringType Type => SpringType.UnderDamped;

    public double X(double time)
    {
        return Math.Pow(Math.E, _r * time)
               * ((_c1 * Math.Cos(_w * time)) + (_c2 * Math.Sin(_w * time)));
    }

    public double DX(double time)
    {
        double power = Math.Pow(Math.E, _r * time);
        double cosine = Math.Cos(_w * time);
        double sine = Math.Sin(_w * time);
        return (power * ((_c2 * _w * cosine) - (_c1 * _w * sine)))
               + (_r * power * ((_c2 * sine) + (_c1 * cosine)));
    }
}
