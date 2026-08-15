using Avalonia;
using Plumix;
using Plumix.Gestures;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Parity coverage for the physics library and the iOS bouncing scroll stack. Numbers are the ones
/// Flutter's own tests assert (<c>test/physics/*_test.dart</c>, <c>test/widgets/scroll_physics_test.dart</c>,
/// <c>test/widgets/scroll_simulation_test.dart</c>).
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class ScrollPhysicsTests
{
    private static FixedScrollMetrics Metrics(
        double pixels,
        double minScrollExtent = 0.0,
        double maxScrollExtent = 1000.0,
        double viewportDimension = 100.0,
        double devicePixelRatio = 3.0)
    {
        return new FixedScrollMetrics(
            Pixels: pixels,
            MinScrollExtent: minScrollExtent,
            MaxScrollExtent: maxScrollExtent,
            ViewportDimension: viewportDimension,
            DevicePixelRatio: devicePixelRatio);
    }

    [Fact]
    public void NearEqual_MatchesDartSemantics()
    {
        Assert.True(PhysicsUtils.NearEqual(5.0, 6.0, 2.0));
        Assert.True(PhysicsUtils.NearEqual(6.0, 5.0, 2.0));
        Assert.False(PhysicsUtils.NearEqual(5.0, 6.0, 0.5));
        Assert.False(PhysicsUtils.NearEqual(6.0, 5.0, 0.5));
        Assert.False(PhysicsUtils.NearEqual(5.0, null, 2.0));
        Assert.False(PhysicsUtils.NearEqual(null, 5.0, 2.0));
        Assert.True(PhysicsUtils.NearEqual(null, null, 2.0));
        Assert.True(PhysicsUtils.NearEqual(double.PositiveInfinity, double.PositiveInfinity, 0.1));
        Assert.True(PhysicsUtils.NearEqual(double.NegativeInfinity, double.NegativeInfinity, 0.1));
        Assert.False(PhysicsUtils.NearEqual(double.PositiveInfinity, double.NegativeInfinity, 0.1));
        Assert.False(PhysicsUtils.NearEqual(0.1, 0.11, 0.001));
        Assert.True(PhysicsUtils.NearEqual(0.1, 0.11, 0.1));
        Assert.True(PhysicsUtils.NearZero(0.0, 1e-7));
    }

    [Fact]
    public void Tolerance_DefaultsToOneThousandth()
    {
        Assert.Equal(1e-3, Tolerance.DefaultTolerance.Distance);
        Assert.Equal(1e-3, Tolerance.DefaultTolerance.Time);
        Assert.Equal(1e-3, Tolerance.DefaultTolerance.Velocity);
    }

    [Fact]
    public void FrictionSimulation_MatchesDartTrajectory()
    {
        var friction = new FrictionSimulation(0.3, 100.0, 400.0, tolerance: new Tolerance(velocity: 1.0));

        Assert.False(friction.IsDone(0.0));
        Assert.Equal(100.0, friction.X(0.0), precision: 6);
        Assert.Equal(400.0, friction.DX(0.0), precision: 6);
        Assert.InRange(friction.X(1.0), 330.0, 335.0);
        Assert.Equal(120.0, friction.DX(1.0), precision: 6);
        Assert.Equal(36.0, friction.DX(2.0), precision: 6);
        Assert.Equal(10.8, friction.DX(3.0), precision: 6);
        Assert.True(friction.DX(4.0) < 3.5);
        Assert.True(friction.IsDone(5.0));
        Assert.InRange(friction.X(5.0), 431.0, 432.0);
    }

    [Fact]
    public void FrictionSimulation_ScrollDrag_MatchesPositionsAndTimeAtX()
    {
        var friction = new FrictionSimulation(0.135, 100.0, 100.0);

        Assert.Equal(100.0, friction.X(0.0), precision: 6);
        Assert.Equal(100.0, friction.DX(0.0), precision: 6);
        Assert.Equal(110.0, friction.X(0.1), tolerance: 1.0);
        Assert.Equal(131.0, friction.X(0.5), tolerance: 1.0);
        Assert.Equal(149.0, friction.X(2.0), tolerance: 1.0);
        Assert.Equal(149.0, friction.FinalX, tolerance: 1.0);

        Assert.Equal(0.0, friction.TimeAtX(100.0));
        Assert.Equal(0.1, friction.TimeAtX(friction.X(0.1)), tolerance: 1e-6);
        Assert.Equal(0.5, friction.TimeAtX(friction.X(0.5)), tolerance: 1e-6);
        Assert.Equal(double.PositiveInfinity, friction.TimeAtX(-1.0));
        Assert.Equal(double.PositiveInfinity, friction.TimeAtX(200.0));
    }

    [Fact]
    public void FrictionSimulation_NegativeVelocityAndConstantDeceleration()
    {
        var negative = new FrictionSimulation(0.135, 100.0, -100.0);
        Assert.Equal(91.0, negative.X(0.1), tolerance: 1.0);
        Assert.Equal(68.0, negative.X(0.5), tolerance: 1.0);
        Assert.Equal(51.0, negative.X(2.0), tolerance: 1.0);
        Assert.Equal(50.0, negative.FinalX, tolerance: 1.0);
        Assert.Equal(double.PositiveInfinity, negative.TimeAtX(101.0));
        Assert.Equal(double.PositiveInfinity, negative.TimeAtX(40.0));

        var decelerated = new FrictionSimulation(0.135, 100.0, -100.0, constantDeceleration: 100);
        Assert.Equal(100.0, decelerated.X(0.0), precision: 6);
        Assert.Equal(-100.0, decelerated.DX(0.0), precision: 6);
        Assert.Equal(91.0, decelerated.X(0.1), tolerance: 1.0);
        Assert.Equal(80.0, decelerated.X(0.5), tolerance: 1.0);

        // Frozen once the constant deceleration has consumed the velocity.
        Assert.Equal(80.0, decelerated.X(2.0), tolerance: 1.0);
        Assert.Equal(80.0, decelerated.FinalX, tolerance: 1.0);
    }

    [Fact]
    public void FrictionSimulation_Through_ReachesTheRequestedEndState()
    {
        var reference = new FrictionSimulation(0.025, 10.0, 600.0);
        double endPosition = reference.X(1.0);
        double endVelocity = reference.DX(1.0);

        FrictionSimulation through = FrictionSimulation.Through(10.0, endPosition, 600.0, endVelocity);

        Assert.False(through.IsDone(0.0));
        Assert.Equal(10.0, through.X(0.0), precision: 6);
        Assert.Equal(600.0, through.DX(0.0), precision: 6);
        Assert.True(through.IsDone(1.0 + 1e-10));
        Assert.Equal(endPosition, through.X(1.0), precision: 6);
        Assert.Equal(endVelocity, through.DX(1.0), precision: 6);
    }

    [Fact]
    public void BoundedFrictionSimulation_ClampsAndFinishesAtTheBound()
    {
        var bounded = new BoundedFrictionSimulation(0.3, 100.0, 400.0, 50.0, 150.0)
        {
            Tolerance = new Tolerance(velocity: 1.0),
        };

        Assert.False(bounded.IsDone(0.0));
        Assert.Equal(100.0, bounded.X(0.0), precision: 6);
        Assert.Equal(400.0, bounded.DX(0.0), precision: 6);
        Assert.Equal(150.0, bounded.X(1.0), precision: 6);
        Assert.True(bounded.IsDone(1.0));
    }

    [Fact]
    public void ClampedSimulation_ClampsValuesButNotDoneness()
    {
        var inner = new FrictionSimulation(0.3, 100.0, 400.0, tolerance: new Tolerance(velocity: 1.0));
        var clamped = new ClampedSimulation(inner, xMin: 120.0, xMax: 200.0, dxMin: 50.0, dxMax: 300.0);

        Assert.Equal(120.0, clamped.X(0.0), precision: 6);
        Assert.Equal(300.0, clamped.DX(0.0), precision: 6);
        Assert.Equal(200.0, clamped.X(5.0), precision: 6);
        Assert.Equal(50.0, clamped.DX(5.0), precision: 6);
        Assert.Equal(inner.IsDone(5.0), clamped.IsDone(5.0));
    }

    [Fact]
    public void SpringDescription_SelectsTheSolutionFromTheDampingRatio()
    {
        Assert.Equal(
            SpringType.CriticallyDamped,
            new SpringSimulation(SpringDescription.WithDampingRatio(1.0, 100.0), 0.0, 500.0, 0.0).Type);
        Assert.Equal(
            SpringType.UnderDamped,
            new SpringSimulation(SpringDescription.WithDampingRatio(1.0, 100.0, 0.75), 0.0, 500.0, 0.0).Type);
        Assert.Equal(
            SpringType.OverDamped,
            new SpringSimulation(SpringDescription.WithDampingRatio(1.0, 100.0, 1.25), 0.0, 500.0, 0.0).Type);
        Assert.Equal(
            SpringType.CriticallyDamped,
            new SpringSimulation(new SpringDescription(1.0, 100.0, 20.0), 0.0, 500.0, 0.0).Type);
    }

    [Fact]
    public void SpringSimulation_CriticallyDamped_MatchesDartTrajectory()
    {
        var spring = new SpringSimulation(
            SpringDescription.WithDampingRatio(1.0, 100.0),
            0.0,
            500.0,
            0.0,
            tolerance: new Tolerance(distance: 0.01, velocity: 0.01));

        Assert.False(spring.IsDone(0.0));
        Assert.Equal(0.0, spring.X(0.0), precision: 6);
        Assert.Equal(0.0, spring.DX(0.0), precision: 6);
        Assert.Equal(356, Math.Floor(spring.X(0.25)));
        Assert.Equal(479, Math.Floor(spring.X(0.50)));
        Assert.Equal(497, Math.Floor(spring.X(0.75)));
        Assert.Equal(1026, Math.Floor(spring.DX(0.25)));
        Assert.Equal(168, Math.Floor(spring.DX(0.50)));
        Assert.Equal(20, Math.Floor(spring.DX(0.75)));
        Assert.InRange(spring.X(1.5), 499.0, 501.0);
        Assert.True(spring.DX(1.5) < 0.1);
        Assert.True(spring.IsDone(1.60));
    }

    [Fact]
    public void SpringSimulation_OverAndUnderDamped_MatchDartTrajectories()
    {
        var overdamped = new SpringSimulation(
            SpringDescription.WithDampingRatio(1.0, 100.0, 1.25),
            0.0,
            500.0,
            0.0,
            tolerance: new Tolerance(distance: 0.01, velocity: 0.01));
        Assert.Equal(445, Math.Floor(overdamped.X(0.5)));
        Assert.Equal(495, Math.Floor(overdamped.X(1.0)));
        Assert.Equal(499, Math.Floor(overdamped.X(1.5)));
        Assert.Equal(273, Math.Floor(overdamped.DX(0.5)));
        Assert.Equal(22, Math.Floor(overdamped.DX(1.0)));
        Assert.Equal(1, Math.Floor(overdamped.DX(1.5)));
        Assert.False(overdamped.IsDone(0.0));
        Assert.True(overdamped.IsDone(3.0));

        var underdamped = new SpringSimulation(
            SpringDescription.WithDampingRatio(1.0, 100.0, 0.25),
            0.0,
            300.0,
            0.0,
            tolerance: new Tolerance(distance: 0.01, velocity: 0.01));

        // Overshoots the end and comes back.
        Assert.Equal(325, Math.Floor(underdamped.X(1.0)));
        Assert.Equal(-65, Math.Floor(underdamped.DX(1.0)));
        Assert.Equal(0, Math.Floor(underdamped.DX(6.0)));
        Assert.Equal(299, Math.Floor(underdamped.X(6.0)));
        Assert.False(underdamped.IsDone(0.0));
        Assert.True(underdamped.IsDone(6.0));
    }

    [Fact]
    public void SpringSimulation_SnapToEnd_LandsExactlyOnTheEnd()
    {
        SpringDescription spring = SpringDescription.WithDampingRatio(1.0, 400.0);
        var tolerance = new Tolerance(distance: 0.1, velocity: 0.1);

        var loose = new SpringSimulation(spring, 0, 1, 0, tolerance: tolerance);
        Assert.True(loose.X(0.4) < 1);
        Assert.True(loose.DX(0.4) > 0);

        var snapped = new SpringSimulation(spring, 0, 1, 0, snapToEnd: true, tolerance: tolerance);
        Assert.Equal(1.0, snapped.X(0.4));
        Assert.Equal(0.0, snapped.DX(0.4));
    }

    [Fact]
    public void SpringDescription_WithDurationAndBounce_RoundTrips()
    {
        SpringDescription bouncy = SpringDescription.WithDurationAndBounce(bounce: 0.3);
        Assert.Equal(1.0, bouncy.Mass);
        Assert.Equal(157.91, bouncy.Stiffness, tolerance: 0.01);
        Assert.Equal(17.59, bouncy.Damping, tolerance: 0.01);
        Assert.Equal(0.3, bouncy.Bounce, tolerance: 0.01);
        Assert.Equal(500, bouncy.Duration.TotalMilliseconds);

        SpringDescription damped = SpringDescription.WithDurationAndBounce(bounce: -0.3);
        Assert.Equal(35.90, damped.Damping, tolerance: 0.01);
        Assert.Equal(-0.3, damped.Bounce, tolerance: 0.01);

        SpringDescription quick = SpringDescription.WithDurationAndBounce(TimeSpan.FromMilliseconds(100));
        Assert.Equal(3947.84, quick.Stiffness, tolerance: 0.01);
        Assert.Equal(125.66, quick.Damping, tolerance: 0.01);
        Assert.Equal(100, quick.Duration.TotalMilliseconds);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => SpringDescription.WithDurationAndBounce(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SpringDescription.WithDurationAndBounce(TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void BouncingScrollSimulation_WithOpenEnd_DeceleratesLikeFriction()
    {
        var simulation = new BouncingScrollSimulation(
            position: 100.0,
            velocity: 400.0,
            leadingExtent: 0.0,
            trailingExtent: double.PositiveInfinity,
            spring: SpringDescription.WithDampingRatio(1.0, 50.0, 0.5),
            tolerance: new Tolerance(velocity: 1.0));

        Assert.False(simulation.IsDone(0.0));
        Assert.Equal(100.0, simulation.X(0.0), precision: 6);
        Assert.Equal(400.0, simulation.DX(0.0), precision: 6);
        Assert.Equal(272.0, simulation.X(1.0), tolerance: 1.0);
        Assert.Equal(54.0, simulation.DX(1.0), tolerance: 1.0);
        Assert.Equal(7.0, simulation.DX(2.0), tolerance: 1.0);
        Assert.True(simulation.DX(3.0) < 1.0);
        Assert.True(simulation.IsDone(5.0));
        Assert.Equal(300.0, simulation.X(5.0), tolerance: 1.0);
    }

    [Fact]
    public void BouncingScrollSimulation_FlingPastTheEdge_SpringsBackToTheExtent()
    {
        var simulation = new BouncingScrollSimulation(
            position: 500.0,
            velocity: -7500.0,
            leadingExtent: 0.0,
            trailingExtent: 1000.0,
            spring: SpringDescription.WithDampingRatio(1.0, 170.0, 1.1),
            tolerance: new Tolerance(distance: 1.5, velocity: 45.0));

        Assert.False(simulation.IsDone(0.0));
        Assert.Equal(500.0, simulation.X(0.0), tolerance: 1.0);
        Assert.Equal(-7500.0, simulation.DX(0.0), tolerance: 1.0);

        // Still decelerating inside the range.
        Assert.False(simulation.IsDone(0.065));
        Assert.Equal(42.0, simulation.X(0.065), tolerance: 1.0);
        Assert.Equal(-6584.0, simulation.DX(0.065), tolerance: 1.0);

        // Past the leading edge: the spring has taken over.
        Assert.False(simulation.IsDone(0.1));
        Assert.Equal(-123.0, simulation.X(0.1), tolerance: 1.0);
        Assert.Equal(-2613.0, simulation.DX(0.1), tolerance: 1.0);

        // Being pulled back towards the edge.
        Assert.False(simulation.IsDone(0.5));
        Assert.Equal(-15.0, simulation.X(0.5), tolerance: 1.0);
        Assert.Equal(124.0, simulation.DX(0.5), tolerance: 1.0);

        // Querying an earlier time still reproduces the friction phase.
        Assert.Equal(500.0, simulation.X(0.0), tolerance: 1.0);
        Assert.Equal(-7500.0, simulation.DX(0.0), tolerance: 1.0);

        Assert.True(simulation.IsDone(2.0));
        Assert.Equal(0.0, simulation.X(2.0));
        Assert.Equal(0.0, simulation.DX(2.0), tolerance: 1.0);
    }

    [Theory]
    [InlineData(800.0)]
    [InlineData(-800.0)]
    public void BouncingScrollSimulation_KineticScroll_SettlesAfterTheSpringPhase(double velocity)
    {
        var simulation = new BouncingScrollSimulation(
            position: 100.0,
            velocity: velocity,
            leadingExtent: 0.0,
            trailingExtent: 300.0,
            spring: SpringDescription.WithDampingRatio(1.0, 50.0, 0.5),
            tolerance: new Tolerance(distance: 0.1, velocity: 0.5));

        Assert.False(simulation.IsDone(0.0));
        Assert.False(simulation.IsDone(0.5));
        Assert.True(simulation.IsDone(3.5));
    }

    [Fact]
    public void ClampingScrollSimulation_HasStableInitialConditionsAndOnlyDecelerates()
    {
        (double position, double velocity)[] cases =
        [
            (51.0, 2866.91537),
            (584.0, 2617.294734),
            (345.0, 1982.785934),
            (0.0, 1831.366634),
            (-156.2, 1541.57665),
            (4534.0, 1073.553798),
            (5469.0, 182.114534),
        ];

        foreach ((double position, double velocity) in cases)
        {
            var simulation = new ClampingScrollSimulation(position: position, velocity: velocity);
            Assert.Equal(position, simulation.X(0.0), precision: 6);
            Assert.Equal(velocity, simulation.DX(0.0), precision: 6);
        }

        var fling = new ClampingScrollSimulation(position: 0.0, velocity: 8000.0);
        const double delta = 1.0 / 60.0;
        double lastVelocity = fling.DX(0.0);
        for (double time = 0.0; time < 3.0; time += delta)
        {
            double velocity = fling.DX(time);
            Assert.True(velocity <= lastVelocity + 1e-9, $"velocity increased at {time}");
            Assert.True(velocity - lastVelocity > -delta * 5130.0, $"velocity jumped at {time}");
            lastVelocity = velocity;
        }

        Assert.True(fling.IsDone(3.0));
        Assert.Equal(0.0, fling.DX(3.0), tolerance: 1e-6);
    }

    [Fact]
    public void ScrollPhysics_ApplyTo_ChainsParentsInOrder()
    {
        ScrollPhysics chained = new BouncingScrollPhysics()
            .ApplyTo(new ClampingScrollPhysics())
            .ApplyTo(new RangeMaintainingScrollPhysics());

        Assert.IsType<BouncingScrollPhysics>(chained);
        Assert.IsType<ClampingScrollPhysics>(chained.Parent);
        Assert.IsType<RangeMaintainingScrollPhysics>(chained.Parent!.Parent);
        Assert.Null(chained.Parent.Parent!.Parent);
    }

    [Fact]
    public void ScrollPhysics_ApplyTo_PreservesDecelerationRate()
    {
        ScrollPhysics applied = new BouncingScrollPhysics(ScrollDecelerationRate.Fast)
            .ApplyTo(new RangeMaintainingScrollPhysics());

        var bouncing = Assert.IsType<BouncingScrollPhysics>(applied);
        Assert.Equal(ScrollDecelerationRate.Fast, bouncing.DecelerationRate);
    }

    [Fact]
    public void ScrollPhysics_ToleranceFor_ScalesWithDevicePixelRatio()
    {
        var physics = new ScrollPhysics();

        Tolerance tolerance = physics.ToleranceFor(Metrics(pixels: 0, devicePixelRatio: 3.0));
        Assert.Equal(1.0 / (0.050 * 3.0), tolerance.Velocity, precision: 10);
        Assert.Equal(1.0 / 3.0, tolerance.Distance, precision: 10);
        Assert.Equal(1e-3, tolerance.Time);
    }

    [Fact]
    public void ScrollPhysics_CreatingTheSimulationDoesNotAlterTheVelocityAtTimeZero()
    {
        FixedScrollMetrics position = Metrics(pixels: 20.0, maxScrollExtent: 100.0, viewportDimension: 500.0);

        Simulation? bouncing = new BouncingScrollPhysics().CreateBallisticSimulation(position, 1000);
        Simulation? clamping = new ClampingScrollPhysics().CreateBallisticSimulation(position, 1000);

        Assert.NotNull(bouncing);
        Assert.NotNull(clamping);
        Assert.Equal(1000.0, bouncing!.DX(0.0), tolerance: 1e-6);
        Assert.Equal(1000.0, clamping!.DX(0.0), tolerance: 1e-6);
    }

    [Fact]
    public void BouncingScrollPhysics_OverscrollIsProgressivelyHarder()
    {
        var physics = new BouncingScrollPhysics();

        double lightlyOverscrolled = physics.ApplyPhysicsToUserOffset(Metrics(pixels: -20.0), 10.0);
        double heavilyOverscrolled = physics.ApplyPhysicsToUserOffset(Metrics(pixels: -40.0), 10.0);

        Assert.InRange(lightlyOverscrolled, 1.0, 20.0);
        Assert.InRange(heavilyOverscrolled, 1.0, 20.0);
        Assert.True(Math.Abs(lightlyOverscrolled) > Math.Abs(heavilyOverscrolled));
    }

    [Fact]
    public void BouncingScrollPhysics_EasingAnOverscrollStillHasResistance()
    {
        var physics = new BouncingScrollPhysics();

        double easing = physics.ApplyPhysicsToUserOffset(Metrics(pixels: -20.0), -10.0);

        Assert.InRange(easing, -10.0, -1.0);
    }

    [Fact]
    public void BouncingScrollPhysics_NoResistanceWhenNotOverscrolled()
    {
        var physics = new BouncingScrollPhysics();
        FixedScrollMetrics position = Metrics(pixels: 300.0);

        Assert.Equal(10.0, physics.ApplyPhysicsToUserOffset(position, 10.0));
        Assert.Equal(-10.0, physics.ApplyPhysicsToUserOffset(position, -10.0));
    }

    [Fact]
    public void BouncingScrollPhysics_EasingMeetsLessResistanceThanTensioning()
    {
        var physics = new BouncingScrollPhysics();
        FixedScrollMetrics position = Metrics(pixels: -20.0);

        double easing = physics.ApplyPhysicsToUserOffset(position, -10.0);
        double tensioning = physics.ApplyPhysicsToUserOffset(position, 10.0);

        Assert.True(Math.Abs(easing) > Math.Abs(tensioning));
    }

    [Fact]
    public void BouncingScrollPhysics_FastDecelerationRate_HasNoEasingResistance()
    {
        var physics = new BouncingScrollPhysics(ScrollDecelerationRate.Fast);
        FixedScrollMetrics position = Metrics(pixels: -20.0);

        double tensioning = physics.ApplyPhysicsToUserOffset(position, 10.0);
        double easing = physics.ApplyPhysicsToUserOffset(position, -10.0);

        Assert.True(Math.Abs(tensioning) < Math.Abs(easing));
        Assert.Equal(-10.0, easing);
    }

    [Fact]
    public void BouncingScrollPhysics_OverscrollIsIndependentOfContentLength()
    {
        var physics = new BouncingScrollPhysics();

        double shortList = physics.ApplyPhysicsToUserOffset(
            Metrics(pixels: -20.0, maxScrollExtent: 10.0),
            10.0);
        double longList = physics.ApplyPhysicsToUserOffset(
            Metrics(pixels: -20.0, maxScrollExtent: 1000.0),
            10.0);

        Assert.Equal(shortList, longList);
        Assert.InRange(shortList, 1.0, 20.0);
    }

    [Fact]
    public void BouncingScrollPhysics_FrictionFactor_MatchesTheDecelerationRate()
    {
        var mobile = new BouncingScrollPhysics();
        var desktop = new BouncingScrollPhysics(ScrollDecelerationRate.Fast);

        Assert.Equal(0.52, mobile.FrictionFactor(0.0), precision: 10);
        Assert.Equal(0.26, desktop.FrictionFactor(0.0), precision: 10);
        Assert.Equal(0.1872, mobile.FrictionFactor(0.4), precision: 10);
        Assert.Equal(0.0936, desktop.FrictionFactor(0.4), precision: 10);
        Assert.Equal(0.0208, mobile.FrictionFactor(0.8), precision: 10);
        Assert.Equal(0.0104, desktop.FrictionFactor(0.8), precision: 10);
    }

    [Fact]
    public void BouncingScrollPhysics_NeverReportsBoundaryOverscroll()
    {
        var physics = new BouncingScrollPhysics();

        Assert.Equal(0.0, physics.ApplyBoundaryConditions(Metrics(pixels: 0.0), -500.0));
        Assert.Equal(0.0, physics.ApplyBoundaryConditions(Metrics(pixels: 1000.0), 1500.0));
    }

    [Fact]
    public void BouncingScrollPhysics_ExposesTheDocumentedFlingAndSpringDefaults()
    {
        var mobile = new BouncingScrollPhysics();
        var desktop = new BouncingScrollPhysics(ScrollDecelerationRate.Fast);

        Assert.Equal(100.0, mobile.MinFlingVelocity);
        Assert.Equal(8000.0, mobile.MaxFlingVelocity);
        Assert.Equal(64000.0, desktop.MaxFlingVelocity);
        Assert.Equal(18.0, mobile.MinFlingDistance);
        Assert.Equal(3.5, mobile.DragStartDistanceMotionThreshold);
        Assert.True(mobile.AllowImplicitScrolling);

        Assert.Equal(0.5, mobile.Spring.Mass);
        Assert.Equal(100.0, mobile.Spring.Stiffness);
        Assert.Equal(1.1 * 2.0 * Math.Sqrt(0.5 * 100.0), mobile.Spring.Damping, precision: 10);

        Assert.Equal(0.3, desktop.Spring.Mass);
        Assert.Equal(75.0, desktop.Spring.Stiffness);
        Assert.Equal(1.3 * 2.0 * Math.Sqrt(0.3 * 75.0), desktop.Spring.Damping, precision: 10);
    }

    [Fact]
    public void BouncingScrollPhysics_CarriedMomentum_FollowsThePowerCurve()
    {
        var physics = new BouncingScrollPhysics();

        Assert.Equal(0.0, physics.CarriedMomentum(0.0));
        Assert.Equal(
            0.000816 * Math.Pow(1000.0, 1.967),
            physics.CarriedMomentum(1000.0),
            precision: 6);
        Assert.Equal(
            -(0.000816 * Math.Pow(1000.0, 1.967)),
            physics.CarriedMomentum(-1000.0),
            precision: 6);

        // Clamped at 40000 in either direction.
        Assert.Equal(40000.0, physics.CarriedMomentum(1e6));
        Assert.Equal(-40000.0, physics.CarriedMomentum(-1e6));
    }

    [Fact]
    public void BouncingScrollPhysics_CreateBallisticSimulation_HonorsToleranceAndRange()
    {
        var physics = new BouncingScrollPhysics();

        // In range with a negligible velocity: nothing to simulate.
        Assert.Null(physics.CreateBallisticSimulation(Metrics(pixels: 200.0), 1.0));

        // Out of range with no velocity at all: the spring still has to pull it back.
        Simulation? springBack = physics.CreateBallisticSimulation(Metrics(pixels: -40.0), 0.0);
        Assert.NotNull(springBack);
        Assert.Equal(-40.0, springBack!.X(0.0), tolerance: 1e-6);
        Assert.True(springBack.X(1.0) > -40.0);
        Assert.True(springBack.IsDone(5.0));
        Assert.Equal(0.0, springBack.X(5.0));
    }

    [Fact]
    public void ClampingScrollPhysics_ApplyBoundaryConditions_ReportsEachEdgeCase()
    {
        var physics = new ClampingScrollPhysics();

        // Hit the top edge: only the part below the minimum extent is overscroll.
        Assert.Equal(-20.0, physics.ApplyBoundaryConditions(Metrics(pixels: 10.0), -20.0));

        // Hit the bottom edge: only the part past the maximum extent is overscroll.
        Assert.Equal(30.0, physics.ApplyBoundaryConditions(Metrics(pixels: 990.0), 1030.0));

        // Underscroll: already past the edge and moving further out.
        Assert.Equal(-10.0, physics.ApplyBoundaryConditions(Metrics(pixels: 0.0), -10.0));

        // Overscroll: already past the edge and moving further out.
        Assert.Equal(10.0, physics.ApplyBoundaryConditions(Metrics(pixels: 1000.0), 1010.0));

        // In range.
        Assert.Equal(0.0, physics.ApplyBoundaryConditions(Metrics(pixels: 500.0), 600.0));
    }

    [Fact]
    public void ClampingScrollPhysics_CreateBallisticSimulation_SpringsBackWhenOutOfRange()
    {
        var physics = new ClampingScrollPhysics();

        Simulation? springBack = physics.CreateBallisticSimulation(Metrics(pixels: -40.0), 0.0);
        Assert.IsType<ScrollSpringSimulation>(springBack);
        Assert.True(springBack!.IsDone(5.0));
        Assert.Equal(0.0, springBack.X(5.0));

        Assert.IsType<ClampingScrollSimulation>(
            physics.CreateBallisticSimulation(Metrics(pixels: 500.0), 2000.0));

        // Nothing to do at rest, or when already pinned against the edge the fling points at.
        Assert.Null(physics.CreateBallisticSimulation(Metrics(pixels: 500.0), 1.0));
        Assert.Null(physics.CreateBallisticSimulation(Metrics(pixels: 1000.0), 2000.0));
        Assert.Null(physics.CreateBallisticSimulation(Metrics(pixels: 0.0), -2000.0));
    }

    [Fact]
    public void RangeMaintainingScrollPhysics_EnforcesTheBoundaryWhenTheContentShrinks()
    {
        var physics = new RangeMaintainingScrollPhysics();
        FixedScrollMetrics old = Metrics(pixels: 900.0, maxScrollExtent: 1000.0);
        FixedScrollMetrics updated = Metrics(pixels: 900.0, maxScrollExtent: 400.0);

        double adjusted = physics.AdjustPositionForNewDimensions(old, updated, isScrolling: false, velocity: 0.0);

        Assert.Equal(400.0, adjusted);
    }

    [Fact]
    public void RangeMaintainingScrollPhysics_MaintainsOverscrollWhenTheContentShrinks()
    {
        var physics = new RangeMaintainingScrollPhysics();
        FixedScrollMetrics old = Metrics(pixels: 1020.0, maxScrollExtent: 1000.0);
        FixedScrollMetrics updated = Metrics(pixels: 1020.0, maxScrollExtent: 900.0);

        double adjusted = physics.AdjustPositionForNewDimensions(old, updated, isScrolling: false, velocity: 0.0);

        // The overscroll of 20 is preserved relative to the new extent.
        Assert.Equal(920.0, adjusted);
    }

    [Fact]
    public void RangeMaintainingScrollPhysics_LeavesAnAnimatingPositionAlone()
    {
        var physics = new RangeMaintainingScrollPhysics();
        FixedScrollMetrics old = Metrics(pixels: 900.0, maxScrollExtent: 1000.0);
        FixedScrollMetrics updated = Metrics(pixels: 900.0, maxScrollExtent: 400.0);

        double adjusted = physics.AdjustPositionForNewDimensions(old, updated, isScrolling: true, velocity: 120.0);

        Assert.Equal(900.0, adjusted);
    }

    [Fact]
    public void ScrollPosition_BouncingPhysics_AcceptsOverscrollAndResistsFurtherDrags()
    {
        using var position = new ScrollPosition(physics: new BouncingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);

        // The first drag past the edge is not resisted: the position is still in range.
        position.ApplyUserOffset(30);
        Assert.Equal(-30.0, position.Pixels, precision: 6);

        // Once overscrolled, the same drag moves the position much less.
        position.ApplyUserOffset(30);
        Assert.True(position.Pixels > -60.0);
        Assert.True(position.Pixels < -30.0);
        Assert.True(position.OutOfRange);
    }

    [Fact]
    public void ScrollPosition_ClampingPhysics_ReportsOverscrollAndStaysInRange()
    {
        using var position = new TestScrollPosition(new ClampingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);

        Assert.Equal(-40.0, position.CallSetPixels(-40.0));
        Assert.Equal(0.0, position.Pixels);
        Assert.False(position.OutOfRange);

        Assert.Equal(0.0, position.CallSetPixels(500.0));
        Assert.Equal(500.0, position.Pixels);
    }

    [Fact]
    public void ScrollPosition_ForcePixelsBypassesBoundariesAndResetsImpliedVelocityAfterTheFrame()
    {
        Scheduler.ResetForTests();
        try
        {
            var physics = new RecordingDeferredLoadingPhysics();
            using var position = new TestScrollPosition(physics);
            position.ApplyViewportDimension(100);
            position.ApplyContentDimensions(0, 100);
            int notifications = 0;
            position.AddListener(() => notifications += 1);

            position.CallForcePixels(0.0);
            Assert.Equal(1, notifications);

            position.CallForcePixels(600.0);
            Assert.Equal(600.0, position.Pixels);
            Assert.True(position.OutOfRange);
            Assert.True(position.RecommendDeferredLoading(default));
            Assert.Equal(600.0, physics.LastVelocity);

            position.CallForcePixels(100.0);
            Assert.True(position.RecommendDeferredLoading(default));
            Assert.Equal(-500.0, physics.LastVelocity);

            Scheduler.PumpFrameForTests();

            Assert.False(position.RecommendDeferredLoading(default));
            Assert.Equal(0.0, physics.LastVelocity);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ScrollPosition_PointerScroll_NeverOverscrollsUnderBouncingPhysics()
    {
        using var position = new ScrollPosition(physics: new BouncingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 500);

        // A wheel notch inside the range moves the full delta.
        position.ApplyPointerScrollDelta(120);
        Assert.Equal(120.0, position.Pixels);

        // Past the end and past the start the target is clamped: unlike a drag, a pointer scroll
        // must not leave the list rubber-banded with nothing to spring it back.
        position.ApplyPointerScrollDelta(900);
        Assert.Equal(500.0, position.Pixels);
        Assert.False(position.OutOfRange);

        position.ApplyPointerScrollDelta(-900);
        Assert.Equal(0.0, position.Pixels);
        Assert.False(position.OutOfRange);

        // Already pinned against the edge: nothing moves and nothing overscrolls.
        position.ApplyPointerScrollDelta(-50);
        Assert.Equal(0.0, position.Pixels);
        Assert.False(position.OutOfRange);
    }

    [Fact]
    public void ScrollPosition_BouncingPhysics_SpringsBackToTheEdgeAfterTheDragEnds()
    {
        using var position = new ScrollPosition(physics: new BouncingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);

        position.BeginDrag();
        position.ApplyUserOffset(60);
        Assert.True(position.Pixels < 0);

        position.EndDrag(0.0);
        Assert.IsType<BallisticScrollActivity>(position.Activity);

        PumpSeconds(1.5);

        Assert.Equal(0.0, position.Pixels, tolerance: 0.5);
        Assert.IsType<IdleScrollActivity>(position.Activity);
    }

    [Fact]
    public void ScrollPosition_BouncingPhysics_SettlesAnOutOfRangePositionWhenTheContentShrinks()
    {
        using var position = new ScrollPosition(
            physics: new BouncingScrollPhysics(parent: new RangeMaintainingScrollPhysics()));
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);
        position.JumpTo(900);

        // The content shrinks under the position, leaving it past the new end.
        position.ApplyContentDimensions(0, 500);
        Assert.IsType<BallisticScrollActivity>(position.Activity);

        PumpSeconds(2.0);

        Assert.Equal(500.0, position.Pixels, tolerance: 0.5);
        Assert.IsType<IdleScrollActivity>(position.Activity);
    }

    [Fact]
    public void AlwaysScrollableScrollPhysics_AcceptsUserOffsetEvenWhenTheContentFits()
    {
        // A viewport with nothing to scroll: the base physics refuse the drag.
        FixedScrollMetrics fits = Metrics(pixels: 0.0, maxScrollExtent: 0.0);
        Assert.False(new ScrollPhysics().ShouldAcceptUserOffset(fits));
        Assert.True(new ScrollPhysics().ShouldAcceptUserOffset(Metrics(pixels: 0.0)));
        Assert.True(new ScrollPhysics().ShouldAcceptUserOffset(Metrics(pixels: 10.0, maxScrollExtent: 0.0)));

        var always = new AlwaysScrollableScrollPhysics();
        Assert.True(always.ShouldAcceptUserOffset(fits));

        // The subclass short-circuits before delegating, so a refusing parent does not win.
        Assert.True(always.ApplyTo(new NeverScrollableScrollPhysics()).ShouldAcceptUserOffset(fits));
        Assert.True(always.AllowUserScrolling);
        Assert.True(always.AllowImplicitScrolling);
    }

    [Fact]
    public void NeverScrollableScrollPhysics_RefusesUserAndImplicitScrolling()
    {
        var never = new NeverScrollableScrollPhysics();
        FixedScrollMetrics scrollable = Metrics(pixels: 40.0);

        Assert.False(never.ShouldAcceptUserOffset(scrollable));
        Assert.False(never.AllowUserScrolling);
        Assert.False(never.AllowImplicitScrolling);

        // The guard runs before parent delegation, so an accepting parent is not consulted.
        Assert.False(never.ApplyTo(new AlwaysScrollableScrollPhysics()).ShouldAcceptUserOffset(scrollable));

        // Base physics allow implicit scrolling.
        Assert.True(new ScrollPhysics().AllowImplicitScrolling);
        Assert.True(new BouncingScrollPhysics().AllowImplicitScrolling);
    }

    [Fact]
    public void ScrollPhysics_ApplyTo_KeepsAlwaysAndNeverInTheChain()
    {
        ScrollPhysics chain = new BouncingScrollPhysics()
            .ApplyTo(new ClampingScrollPhysics()
                .ApplyTo(new NeverScrollableScrollPhysics()
                    .ApplyTo(new AlwaysScrollableScrollPhysics()
                        .ApplyTo(new RangeMaintainingScrollPhysics()))));

        Assert.Equal(
            "BouncingScrollPhysics -> ClampingScrollPhysics -> NeverScrollableScrollPhysics "
            + "-> AlwaysScrollableScrollPhysics -> RangeMaintainingScrollPhysics",
            chain.ToString());

        // Each applyTo returns its own type, not the base class.
        Assert.IsType<AlwaysScrollableScrollPhysics>(new AlwaysScrollableScrollPhysics().ApplyTo(null));
        Assert.IsType<NeverScrollableScrollPhysics>(new NeverScrollableScrollPhysics().ApplyTo(null));
    }

    [Fact]
    public void ScrollPosition_Hold_StopsBallisticMotionAndRemembersItsVelocity()
    {
        Scheduler.ResetForTests();
        try
        {
            using var position = new ScrollPosition(physics: new BouncingScrollPhysics());
            position.ApplyViewportDimension(100);
            position.ApplyContentDimensions(0, 1000);
            position.GoBallistic(-1200);
            Assert.IsType<BallisticScrollActivity>(position.Activity);

            IScrollHoldController hold = position.Hold();
            Assert.IsType<HoldScrollActivity>(position.Activity);
            Assert.False(position.Activity.IsScrolling);
            Assert.Equal(0.0, position.Activity.Velocity);

            // The drag started from the hold inherits the interrupted fling's momentum.
            ScrollDragController drag = position.Drag(new DragStartDetails(new Point(0, 0)));
            Assert.Equal(
                new BouncingScrollPhysics().CarriedMomentum(-1200),
                drag.CarriedVelocity!.Value,
                precision: 6);
            Assert.Equal(3.5, drag.MotionStartDistanceThreshold!.Value);
            Assert.IsType<DragScrollActivity>(position.Activity);

            // Releasing the hold after the drag replaced it is inert.
            hold.Cancel();
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ScrollPosition_Drag_UsesTheClampingDefaultsWhenThePhysicsDoNotCarryMomentum()
    {
        using var position = new ScrollPosition(physics: new ClampingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);

        ScrollDragController drag = position.Drag(new DragStartDetails(new Point(0, 0)));

        Assert.Equal(0.0, drag.CarriedVelocity!.Value);
        Assert.Null(drag.MotionStartDistanceThreshold);
    }

    [Fact]
    public void ScrollDragController_MotionStartThreshold_SwallowsOffsetsUntilItBreaks()
    {
        using var position = new ScrollPosition(physics: new BouncingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);
        DateTime start = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);
        var drag = new ScrollDragController(
            @delegate: position,
            details: new DragStartDetails(new Point(0, 0), SourceTimeStampUtc: start),
            motionStartDistanceThreshold: 3.5);

        // Small offsets accumulate without moving the position.
        Assert.Equal(0.0, Update(drag, -1.0, start.AddMilliseconds(16)));
        Assert.Equal(0.0, Update(drag, -1.0, start.AddMilliseconds(32)));
        Assert.Equal(0.0, position.Pixels);

        // Breaking the threshold at ordinary speed releases min(threshold / 3, |offset|).
        Assert.Equal(-3.5 / 3.0, Update(drag, -2.0, start.AddMilliseconds(48)), precision: 6);

        // Once broken, every later offset passes straight through.
        Assert.Equal(-4.0, Update(drag, -4.0, start.AddMilliseconds(64)), precision: 6);
    }

    [Fact]
    public void ScrollDragController_MotionStartThreshold_LetsADeliberateFlingThrough()
    {
        using var position = new ScrollPosition(physics: new BouncingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);
        DateTime start = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);
        var drag = new ScrollDragController(
            @delegate: position,
            details: new DragStartDetails(new Point(0, 0), SourceTimeStampUtc: start),
            motionStartDistanceThreshold: 3.5);

        // A single update past the big-break distance is not damped at all.
        Assert.Equal(-30.0, Update(drag, -30.0, start.AddMilliseconds(16)), precision: 6);
    }

    [Fact]
    public void ScrollDragController_MotionStartThreshold_ReArmsAfterTheDragRests()
    {
        using var position = new ScrollPosition(physics: new BouncingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);
        DateTime start = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);
        var drag = new ScrollDragController(
            @delegate: position,
            details: new DragStartDetails(new Point(0, 0), SourceTimeStampUtc: start),
            motionStartDistanceThreshold: 3.5);

        Assert.Equal(-30.0, Update(drag, -30.0, start.AddMilliseconds(16)), precision: 6);

        // A stationary finger for less than the stop threshold keeps the drag in motion.
        Assert.Equal(0.0, Update(drag, 0.0, start.AddMilliseconds(50)));
        Assert.Equal(-2.0, Update(drag, -2.0, start.AddMilliseconds(60)), precision: 6);

        // Resting past the stop threshold re-arms it, so the next small offset is swallowed again.
        Assert.Equal(0.0, Update(drag, 0.0, start.AddMilliseconds(200)));
        Assert.Equal(0.0, Update(drag, -1.0, start.AddMilliseconds(216)));
    }

    [Fact]
    public void ScrollDragController_WithoutAThreshold_AppliesEveryOffset()
    {
        using var position = new ScrollPosition(physics: new ClampingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);
        DateTime start = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);
        var drag = new ScrollDragController(
            @delegate: position,
            details: new DragStartDetails(new Point(0, 0), SourceTimeStampUtc: start));

        Assert.Equal(-1.0, Update(drag, -1.0, start.AddMilliseconds(16)), precision: 6);
        Assert.Equal(1.0, position.Pixels, tolerance: 1e-9);

        // A drag with no source timestamp (a semantics-driven scroll) bypasses the thresholds too.
        Assert.Equal(-1.0, Update(drag, -1.0, timestampUtc: null), precision: 6);
    }

    [Fact]
    public void ScrollDragController_ReversedAxis_NegatesOffsetsAndVelocities()
    {
        Scheduler.ResetForTests();
        try
        {
            using var position = new ScrollPosition(physics: new ClampingScrollPhysics())
            {
                AxisDirection = AxisDirection.Up,
            };
            position.ApplyViewportDimension(100);
            position.ApplyContentDimensions(0, 1000);
            DateTime start = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);
            var drag = new ScrollDragController(
                @delegate: position,
                details: new DragStartDetails(new Point(0, 0), SourceTimeStampUtc: start));

            // Dragging down a reversed list scrolls forward instead of backward.
            Assert.Equal(-5.0, Update(drag, 5.0, start.AddMilliseconds(16)), precision: 6);
            Assert.Equal(5.0, position.Pixels, tolerance: 1e-9);

            drag.End(new DragEndDetails(primaryVelocity: 600.0));
            Assert.IsType<BallisticScrollActivity>(position.Activity);
            Assert.Equal(600.0, position.Activity.Velocity, tolerance: 1e-6);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ScrollDragController_CarriedMomentum_IsAddedOnlyToAMatchingFling()
    {
        Scheduler.ResetForTests();
        try
        {
            DateTime start = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);

            // Same direction and comparable magnitude: the carried velocity is added.
            Assert.Equal(-1400.0, EndVelocityWithCarry(-400.0, 1000.0, start), tolerance: 1e-6);

            // Opposite direction: nothing is carried.
            Assert.Equal(1000.0, EndVelocityWithCarry(-400.0, -1000.0, start), tolerance: 1e-6);

            // Same direction but substantially slower than the carried momentum: nothing is carried.
            Assert.Equal(-100.0, EndVelocityWithCarry(-400.0, 100.0, start), tolerance: 1e-6);

            // A finger that rested too long before lifting loses the momentum.
            Assert.Equal(
                -1000.0,
                EndVelocityWithCarry(-400.0, 1000.0, start, stationaryMilliseconds: 40),
                tolerance: 1e-6);

            // A short stationary moment keeps it.
            Assert.Equal(
                -1400.0,
                EndVelocityWithCarry(-400.0, 1000.0, start, stationaryMilliseconds: 10),
                tolerance: 1e-6);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ScrollDragController_RejectsANonPositiveMotionThreshold()
    {
        using var position = new ScrollPosition();
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScrollDragController(
            @delegate: position,
            details: new DragStartDetails(new Point(0, 0)),
            motionStartDistanceThreshold: 0.0));
    }

    private static double EndVelocityWithCarry(
        double carriedVelocity,
        double primaryVelocity,
        DateTime start,
        int? stationaryMilliseconds = null)
    {
        using var position = new ScrollPosition(physics: new BouncingScrollPhysics());
        position.ApplyViewportDimension(100);
        position.ApplyContentDimensions(0, 1000);
        var drag = new ScrollDragController(
            @delegate: position,
            details: new DragStartDetails(new Point(0, 0), SourceTimeStampUtc: start),
            carriedVelocity: carriedVelocity);

        if (stationaryMilliseconds is { } idle)
        {
            Update(drag, 0.0, start.AddMilliseconds(idle));
        }

        drag.End(new DragEndDetails(primaryVelocity: primaryVelocity));
        return position.Activity.Velocity;
    }

    private static double Update(ScrollDragController drag, double primaryDelta, DateTime? timestampUtc)
    {
        return drag.Update(new DragUpdateDetails(
            GlobalPosition: new Point(0, 0),
            LocalPosition: new Point(0, 0),
            Delta: new Point(0, primaryDelta),
            PrimaryDelta: primaryDelta,
            SourceTimeStampUtc: timestampUtc));
    }

    private static void PumpSeconds(double seconds)
    {
        const double frame = 1.0 / 60.0;
        double start = Scheduler.CurrentSeconds;
        for (int step = 1; step * frame <= seconds; step++)
        {
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(start + (step * frame)));
        }
    }

    private sealed class TestScrollPosition(ScrollPhysics physics) : ScrollPosition(physics: physics)
    {
        public double CallSetPixels(double value) => SetPixels(value);

        public void CallForcePixels(double value) => ForcePixels(value);
    }

    private sealed class RecordingDeferredLoadingPhysics : ScrollPhysics
    {
        public double LastVelocity { get; private set; }

        public override bool RecommendDeferredLoading(
            double velocity,
            IScrollMetrics metrics,
            BuildContext context)
        {
            LastVelocity = velocity;
            return Math.Abs(velocity) > 400.0;
        }
    }
}
