// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_simulation.dart

using Plumix.Physics;

namespace Plumix.Rendering;

/// <summary>
/// An implementation of scroll physics that matches iOS.
/// </summary>
/// <remarks>
/// Friction is applied while the position is in range, and a spring takes over once the position
/// leaves the <c>leadingExtent</c>/<c>trailingExtent</c> range, producing the iOS rubber-band effect.
/// </remarks>
public sealed class BouncingScrollSimulation : Simulation
{
    /// <summary>The maximum velocity that can be transferred from the friction to the spring.</summary>
    public const double MaxSpringTransferVelocity = 5000.0;

    private readonly FrictionSimulation _frictionSimulation = null!;
    private readonly Simulation _springSimulation = null!;
    private readonly double _springTime;
    private double _timeOffset;

    public BouncingScrollSimulation(
        double position,
        double velocity,
        double leadingExtent,
        double trailingExtent,
        SpringDescription spring,
        double constantDeceleration = 0,
        Tolerance? tolerance = null) : base(tolerance)
    {
        if (leadingExtent > trailingExtent)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leadingExtent),
                "leadingExtent must be less than or equal to trailingExtent.");
        }

        LeadingExtent = leadingExtent;
        TrailingExtent = trailingExtent;
        Spring = spring;

        if (position < leadingExtent)
        {
            _springSimulation = UnderscrollSimulation(position, velocity);
            _springTime = double.NegativeInfinity;
        }
        else if (position > trailingExtent)
        {
            _springSimulation = OverscrollSimulation(position, velocity);
            _springTime = double.NegativeInfinity;
        }
        else
        {
            // Taken from UIScrollView.decelerationRate (.normal = 0.998)
            // 0.998^1000 = ~0.135
            _frictionSimulation = new FrictionSimulation(
                0.135,
                position,
                velocity,
                constantDeceleration: constantDeceleration);
            double finalX = _frictionSimulation.FinalX;
            if (velocity > 0.0 && finalX > trailingExtent)
            {
                _springTime = _frictionSimulation.TimeAtX(trailingExtent);
                _springSimulation = OverscrollSimulation(
                    trailingExtent,
                    Math.Min(_frictionSimulation.DX(_springTime), MaxSpringTransferVelocity));
            }
            else if (velocity < 0.0 && finalX < leadingExtent)
            {
                _springTime = _frictionSimulation.TimeAtX(leadingExtent);
                _springSimulation = UnderscrollSimulation(
                    leadingExtent,
                    Math.Min(_frictionSimulation.DX(_springTime), MaxSpringTransferVelocity));
            }
            else
            {
                _springTime = double.PositiveInfinity;
            }
        }
    }

    /// <summary>The position where the spring pulls back when the particle underscrolls.</summary>
    public double LeadingExtent { get; }

    /// <summary>The position where the spring pulls back when the particle overscrolls.</summary>
    public double TrailingExtent { get; }

    /// <summary>The spring used when the particle is out of range.</summary>
    public SpringDescription Spring { get; }

    public override double X(double time) => SimulationFor(time).X(time - _timeOffset);

    public override double DX(double time) => SimulationFor(time).DX(time - _timeOffset);

    public override bool IsDone(double time) => SimulationFor(time).IsDone(time - _timeOffset);

    public override string ToString()
    {
        return $"BouncingScrollSimulation(leadingExtent: {LeadingExtent}, trailingExtent: {TrailingExtent})";
    }

    private Simulation UnderscrollSimulation(double x, double dx)
    {
        return new ScrollSpringSimulation(Spring, x, LeadingExtent, dx);
    }

    private Simulation OverscrollSimulation(double x, double dx)
    {
        return new ScrollSpringSimulation(Spring, x, TrailingExtent, dx);
    }

    private Simulation SimulationFor(double time)
    {
        Simulation simulation;
        if (time > _springTime)
        {
            _timeOffset = double.IsFinite(_springTime) ? _springTime : 0.0;
            simulation = _springSimulation;
        }
        else
        {
            _timeOffset = 0.0;
            simulation = _frictionSimulation;
        }

        simulation.Tolerance = Tolerance;
        return simulation;
    }
}

/// <summary>
/// An implementation of scroll physics that aligns with Android.
/// </summary>
/// <remarks>
/// Based on <c>OverScroller.java</c> from Android, in particular <c>SplineOverScroller</c>.
/// </remarks>
public sealed class ClampingScrollSimulation : Simulation
{
    // See DECELERATION_RATE.
    private static readonly double DecelerationRate = Math.Log(0.78) / Math.Log(0.9);

    // See INFLEXION.
    private const double Inflexion = 0.35;

    // See mPhysicalCoeff. This has a value of 0.84 times Earth gravity, expressed in units of
    // logical pixels per second^2.
    private const double PhysicalCoeff =
        9.80665 // g, in meters per second^2
        * 39.37 // 1 meter / 1 inch
        * 160.0 // 1 inch / 1 logical pixel
        * 0.84; // "look and feel tuning"

    private readonly double _duration;
    private readonly double _distance;

    public ClampingScrollSimulation(
        double position,
        double velocity,
        double friction = 0.015,
        Tolerance? tolerance = null) : base(tolerance)
    {
        Position = position;
        Velocity = velocity;
        Friction = friction;
        _duration = FlingDuration();
        _distance = Velocity * _duration / DecelerationRate;
    }

    /// <summary>The position of the particle at the beginning of the simulation.</summary>
    public double Position { get; }

    /// <summary>The velocity at which the particle is moving at the beginning of the simulation.</summary>
    public double Velocity { get; }

    /// <summary>The amount of friction the particle experiences as it travels.</summary>
    public double Friction { get; }

    public override double X(double time)
    {
        double t = Math.Clamp(time / _duration, 0.0, 1.0);
        return Position + (_distance * (1.0 - Math.Pow(1.0 - t, DecelerationRate)));
    }

    public override double DX(double time)
    {
        double t = Math.Clamp(time / _duration, 0.0, 1.0);
        return Velocity * Math.Pow(1.0 - t, DecelerationRate - 1.0);
    }

    public override bool IsDone(double time) => time >= _duration;

    // See getSplineFlingDuration().
    private double FlingDuration()
    {
        // See getSplineDeceleration(). That function's value is log(velocity.abs() / referenceVelocity).
        double referenceVelocity = Friction * PhysicalCoeff / Inflexion;

        // This is the value getSplineFlingDuration() would return, but in seconds.
        double androidDuration = Math.Pow(Math.Abs(Velocity) / referenceVelocity, 1 / (DecelerationRate - 1.0));

        // We finish a bit sooner than Android, in order to travel the same total distance.
        return DecelerationRate * Inflexion * androidDuration;
    }
}
