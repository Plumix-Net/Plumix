using Avalonia;
using Plumix.Foundation;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/animation/curves_test.dart

namespace Plumix.Tests;

public sealed class CurvesDartParityTests
{
    private static readonly Point Zero = new(0.0, 0.0);

    // flutter_test's `hasOneLineDescription`.
    private static void AssertOneLineDescription(object value)
    {
        string description = value.ToString()!;
        Assert.NotEmpty(description);
        Assert.DoesNotContain("\n", description);
        Assert.DoesNotContain("Instance of ", description);
        Assert.Equal(description.Trim(), description);
    }

    private static void AssertMinMax(Curve curve, Action<double, double> check)
    {
        double min = double.PositiveInfinity;
        double max = double.NegativeInfinity;
        for (int i = 0; i <= 10; i++)
        {
            double value = curve.Transform(i / 10.0);
            min = Math.Min(min, value);
            max = Math.Max(max, value);
        }

        check(min, max);
    }

    [Fact]
    public void ToStringControlTest()
    {
        AssertOneLineDescription(Curves.Linear);
        AssertOneLineDescription(new SawTooth(3));
        AssertOneLineDescription(new Interval(0.25, 0.75));
        AssertOneLineDescription(new Interval(0.25, 0.75, curve: Curves.Ease));
        AssertOneLineDescription(new Split(0.25, beginCurve: Curves.Ease));
    }

    [Fact]
    public void ToString_MatchesDartFormats()
    {
        Assert.Equal("Cubic(0.25, 0.10, 0.25, 1.00)", Curves.Ease.ToString());
        Assert.Equal("SawTooth(3)", new SawTooth(3).ToString());
        Assert.Equal("Interval(0.25⋯0.75)", new Interval(0.25, 0.75).ToString());
        Assert.Equal(
            "Interval(0.25⋯0.75)➩Cubic(0.25, 0.10, 0.25, 1.00)",
            new Interval(0.25, 0.75, curve: Curves.Ease).ToString());
        Assert.Equal("ElasticInCurve(0.4)", Curves.ElasticIn.ToString());
        Assert.Equal("FlippedCurve(Cubic(0.25, 0.10, 0.25, 1.00))", Curves.Ease.Flipped.ToString());
        Assert.StartsWith("Split#", new Split(0.25).ToString());
        Assert.Equal("[(0.25, 0.50), 0.13]", new Curve2DSample(0.125, new Point(0.25, 0.5)).ToString());
    }

    [Fact]
    public void CurveFlippedControlTest()
    {
        Curve ease = Curves.Ease;
        Curve flippedEase = ease.Flipped;
        Assert.True(flippedEase.Transform(0.0) < 0.001);
        Assert.True(flippedEase.Transform(0.5) < ease.Transform(0.5));
        Assert.True(flippedEase.Transform(1.0) > 0.999);
        AssertOneLineDescription(flippedEase);
    }

    [Fact]
    public void ThresholdHasAThreshold()
    {
        Curve step = new Threshold(0.25);
        Assert.Equal(0.0, step.Transform(0.0));
        Assert.Equal(0.0, step.Transform(0.24));
        Assert.Equal(1.0, step.Transform(0.25));
        Assert.Equal(1.0, step.Transform(0.26));
        Assert.Equal(1.0, step.Transform(1.0));
    }

    private static void AssertMaximumSlope(Curve curve, double maximumSlope)
    {
        const double delta = 0.005;
        for (double x = 0.0; x < 1.0 - delta; x += delta)
        {
            double deltaY = curve.Transform(x) - curve.Transform(x + delta);
            Assert.True(Math.Abs(deltaY) < delta * maximumSlope, $"{curve} discontinuous at {x}");
        }
    }

    [Fact]
    public void CurveIsContinuous()
    {
        Curve[] curves =
        [
            Curves.Linear, Curves.Decelerate, Curves.FastOutSlowIn, Curves.SlowMiddle, Curves.BounceIn,
            Curves.BounceOut, Curves.BounceInOut, Curves.ElasticOut, Curves.ElasticInOut, Curves.Ease,
            Curves.EaseIn, Curves.EaseInSine, Curves.EaseInQuad, Curves.EaseInCubic, Curves.EaseInQuart,
            Curves.EaseInQuint, Curves.EaseInExpo, Curves.EaseInCirc, Curves.EaseOut, Curves.EaseOutSine,
            Curves.EaseOutQuad, Curves.EaseOutCubic, Curves.EaseInOutCubicEmphasized, Curves.EaseOutQuart,
            Curves.EaseOutQuint, Curves.EaseOutExpo, Curves.EaseOutCirc, Curves.EaseInOut,
            Curves.EaseInOutSine, Curves.EaseInOutQuad, Curves.EaseInOutCubic, Curves.EaseInOutQuart,
            Curves.EaseInOutQuint, Curves.EaseInOutCirc,
        ];

        // Curves.easeInOutExpo is discontinuous at its midpoint, so not included here.
        foreach (Curve curve in curves)
        {
            AssertMaximumSlope(curve, 20.0);
        }
    }

    [Fact]
    public void BounceStaysInBounds()
    {
        foreach (Curve curve in new[] { Curves.BounceIn, Curves.BounceOut, Curves.BounceInOut })
        {
            for (int i = 0; i <= 10; i++)
            {
                Assert.InRange(curve.Transform(i / 10.0), 0.0, 1.0);
            }
        }
    }

    [Fact]
    public void ElasticOvershootsItsBounds()
    {
        AssertOneLineDescription(Curves.ElasticIn);
        AssertOneLineDescription(Curves.ElasticOut);
        AssertOneLineDescription(Curves.ElasticInOut);

        AssertMinMax(Curves.ElasticIn, (min, max) =>
        {
            Assert.True(min < 0.0);
            Assert.True(max <= 1.0);
        });
        AssertMinMax(Curves.ElasticOut, (min, max) =>
        {
            Assert.True(min >= 0.0);
            Assert.True(max > 1.0);
        });
        AssertMinMax(Curves.ElasticInOut, (min, max) =>
        {
            Assert.True(min < 0.0);
            Assert.True(max > 1.0);
        });
    }

    [Fact]
    public void BackOvershootsItsBounds()
    {
        AssertOneLineDescription(Curves.EaseInBack);
        AssertOneLineDescription(Curves.EaseOutBack);
        AssertOneLineDescription(Curves.EaseInOutBack);

        AssertMinMax(Curves.EaseInBack, (min, max) =>
        {
            Assert.True(min < 0.0);
            Assert.True(max <= 1.0);
        });
        AssertMinMax(Curves.EaseOutBack, (min, max) =>
        {
            Assert.True(min >= 0.0);
            Assert.True(max > 1.0);
        });
        AssertMinMax(Curves.EaseInOutBack, (min, max) =>
        {
            Assert.True(min < 0.0);
            Assert.True(max > 1.0);
        });
    }

    [Fact]
    public void DecelerateDoesSo()
    {
        AssertOneLineDescription(Curves.Decelerate);
        AssertMinMax(Curves.Decelerate, (min, max) =>
        {
            Assert.True(min >= 0.0);
            Assert.True(max <= 1.0);
        });

        double d1 = Curves.Decelerate.Transform(0.2) - Curves.Decelerate.Transform(0.0);
        double d2 = Curves.Decelerate.Transform(1.0) - Curves.Decelerate.Transform(0.8);
        Assert.True(d2 < d1);
    }

    [Fact]
    public void ThreePointCubicInterpolatesMidpoint()
    {
        var test = new ThreePointCubic(
            new Point(0.05, 0),
            new Point(0.133333, 0.06),
            new Point(0.166666, 0.4),
            new Point(0.208333, 0.82),
            new Point(0.25, 1));

        Assert.Equal(0.4, test.Transform(0.166666));
    }

    [Fact]
    public void CubicTransformInternalClampsValuesOutsideTheUnitInterval()
    {
        Assert.Equal(1.0, Curves.EaseOut.TransformInternal(2.0));
        Assert.Equal(0.0, Curves.EaseOut.TransformInternal(-1.0));
    }

    [Fact]
    public void CubicTransformInternalThrowsForNaN()
    {
        Assert.Throws<ArgumentException>(() => Curves.EaseOut.TransformInternal(double.NaN));
    }

    [DebugOnlyFact]
    public void InvalidTransformParameterShouldAssert()
    {
        Curve[] curves =
        [
            new SawTooth(2), new Interval(0.0, 1.0), new Split(0.0), new Threshold(0.5), new ElasticInCurve(),
            new ElasticOutCurve(), new Cubic(0.42, 0.0, 0.58, 1.0), Curves.EaseInOutCubicEmphasized,
            Curves.Decelerate, Curves.BounceIn, Curves.BounceOut, Curves.BounceInOut,
        ];
        foreach (Curve curve in curves)
        {
            Assert.Throws<AssertionError>(() => curve.Transform(-0.0001));
            Assert.Throws<AssertionError>(() => curve.Transform(1.0001));
        }
    }

    [DebugOnlyFact]
    public void OutOfRangeParameter_NamesTheValueInTheAssertMessage()
    {
        AssertionError error = Assert.Throws<AssertionError>(() => new SawTooth(2).Transform(1.5));
        Assert.Contains("parametric value 1.5 is outside of [0, 1] range.", error.Message);
    }

    [Fact]
    public void CurveTransformMethodShouldReturnZeroForZeroAndOneForOne()
    {
        Curve[] curves =
        [
            new SawTooth(2), new Interval(0, 1), new Split(0.5), new Threshold(0.5), new ElasticInCurve(),
            new ElasticOutCurve(), new ElasticInOutCurve(), Curves.Linear, Curves.EaseInOutExpo,
            Curves.EaseInOutCubicEmphasized, new FlippedCurve(Curves.EaseInOutExpo), Curves.Decelerate,
            Curves.BounceIn, Curves.BounceOut, Curves.BounceInOut,
        ];
        foreach (Curve curve in curves)
        {
            Assert.Equal(0.0, curve.Transform(0.0));
            Assert.Equal(1.0, curve.Transform(1.0));
        }
    }

    [Fact]
    public void SplitInterpolatesValuesProperly()
    {
        var curve = new Split(0.3);
        const double tolerance = 1e-6;
        Assert.Equal(0.0, curve.Transform(0.0));
        Assert.Equal(0.1, curve.Transform(0.1));
        Assert.Equal(0.25, curve.Transform(0.25));
        Assert.Equal(0.3, curve.Transform(0.3));
        Assert.Equal(0.760461, curve.Transform(0.5), tolerance);
        Assert.Equal(0.962055, curve.Transform(0.75), tolerance);
        Assert.Equal(1.0, curve.Transform(1.0));
    }

    [Fact]
    public void Split_DefaultsToLinearThenEaseOutCubic()
    {
        var curve = new Split(0.5);
        Assert.Same(Curves.Linear, curve.BeginCurve);
        Assert.Same(Curves.EaseOutCubic, curve.EndCurve);
        Assert.Equal(0.5, curve.SplitPoint);
    }

    [Fact]
    public void Curves_MatchDartControlPoints()
    {
        AssertCubic(Curves.EaseOutCubic, 0.215, 0.61, 0.355, 1.0);
        AssertCubic(Curves.FastLinearToSlowEaseIn, 0.18, 1.0, 0.04, 1.0);
        AssertCubic(Curves.EaseInOutBack, 0.68, -0.55, 0.265, 1.55);
        AssertCubic(Curves.SlowMiddle, 0.15, 0.85, 0.85, 0.15);
        AssertCubic(Curves.FastOutSlowIn, 0.4, 0.0, 0.2, 1.0);
        Assert.Equal(0.4, Curves.ElasticIn.Period);
        Assert.Same(Curves.Ease, Curves.Ease);
    }

    private static void AssertCubic(Cubic cubic, double a, double b, double c, double d)
    {
        Assert.Equal(a, cubic.A);
        Assert.Equal(b, cubic.B);
        Assert.Equal(c, cubic.C);
        Assert.Equal(d, cubic.D);
    }

    private static readonly Point[] SplinePoints =
    [
        new(0.0, 0.0), new(0.01, 0.25), new(0.2, 0.25), new(0.33, 0.25), new(0.5, 1.0), new(0.66, 0.75),
        new(1.0, 1.0),
    ];

    private static void AssertSplineValues(CatmullRomSpline curve)
    {
        const double tolerance = 1e-6;
        AssertPoint(new Point(0.0, 0.0), curve.Transform(0.0), tolerance);
        AssertPoint(new Point(0.0966945, 0.2626806), curve.Transform(0.25), tolerance);
        AssertPoint(new Point(0.33, 0.25), curve.Transform(0.5), tolerance);
        AssertPoint(new Point(0.570260, 0.883085), curve.Transform(0.75), tolerance);
        AssertPoint(new Point(1.0, 1.0), curve.Transform(1.0), tolerance);
    }

    private static void AssertPoint(Point expected, Point actual, double tolerance)
    {
        Assert.Equal(expected.X, actual.X, tolerance);
        Assert.Equal(expected.Y, actual.Y, tolerance);
    }

    [Fact]
    public void CatmullRomSplineInterpolatesValuesProperly()
    {
        AssertSplineValues(new CatmullRomSpline(
            SplinePoints,
            startHandle: new Point(0.0, -0.3),
            endHandle: new Point(1.3, 1.3)));
    }

    [Fact]
    public void CatmullRomSplineInterpolatesValuesProperlyWhenPrecomputed()
    {
        AssertSplineValues(CatmullRomSpline.Precompute(
            SplinePoints,
            startHandle: new Point(0.0, -0.3),
            endHandle: new Point(1.3, 1.3)));
    }

    private static IEnumerable<Action> SplineContractViolations(
        Func<IReadOnlyList<Point>, double, Point?, Point?, CatmullRomSpline> create,
        bool sample)
    {
        Point[] four = [Zero, Zero, Zero, Zero];
        yield return () => create([], 0.0, null, null);
        yield return () => create([Zero], 0.0, null, null);
        yield return () => create([Zero, Zero], 0.0, null, null);
        yield return () => create([Zero, Zero, Zero], 0.0, null, null);
        yield return () => create(four, -1.0, null, null);
        yield return () => create(four, 2.0, null, null);

        Action Sampled(Func<CatmullRomSpline> build) => sample ? () => build().GenerateSamples() : () => build();

        var infiniteX = new Point(double.PositiveInfinity, 0.0);
        var infiniteY = new Point(0.0, double.PositiveInfinity);
        yield return Sampled(() => create([infiniteX, Zero, Zero, Zero], 0.0, null, null));
        yield return Sampled(() => create([infiniteY, Zero, Zero, Zero], 0.0, null, null));
        yield return Sampled(() => create(four, 0.0, new Point(0.0, double.PositiveInfinity), null));
        yield return Sampled(() => create(four, 0.0, null, new Point(0.0, double.PositiveInfinity)));
    }

    [DebugOnlyFact]
    public void CatmullRomSplineEnforcesContract()
    {
        foreach (Action violation in SplineContractViolations(
                     (points, tension, start, end) =>
                         new CatmullRomSpline(points, tension: tension, startHandle: start, endHandle: end),
                     sample: true))
        {
            Assert.ThrowsAny<AssertionError>(violation);
        }
    }

    [DebugOnlyFact]
    public void CatmullRomSplineEnforcesContractWhenPrecomputed()
    {
        foreach (Action violation in SplineContractViolations(
                     (points, tension, start, end) =>
                         CatmullRomSpline.Precompute(points, tension: tension, startHandle: start, endHandle: end),
                     sample: false))
        {
            Assert.ThrowsAny<AssertionError>(violation);
        }
    }

    private static readonly Point[] CurvePoints = [new(0.2, 0.25), new(0.33, 0.25), new(0.5, 1.0), new(0.8, 0.75)];

    private static void AssertCatmullRomCurveValues(CatmullRomCurve curve)
    {
        const double tolerance = 1e-6;
        Assert.Equal(0.0, curve.Transform(0.0), tolerance);
        Assert.Equal(0.012874734350170863, curve.Transform(0.01), tolerance);
        Assert.Equal(0.24989646045277542, curve.Transform(0.2), tolerance);
        Assert.Equal(0.250037698527661, curve.Transform(0.33), tolerance);
        Assert.Equal(0.9999057323235939, curve.Transform(0.5), tolerance);
        Assert.Equal(0.9357294964536621, curve.Transform(0.6), tolerance);
        Assert.Equal(0.7500423402378034, curve.Transform(0.8), tolerance);
        Assert.Equal(1.0, curve.Transform(1.0), tolerance);
    }

    [Fact]
    public void CatmullRomCurveInterpolatesGivenPointsCorrectly()
    {
        AssertCatmullRomCurveValues(new CatmullRomCurve(CurvePoints));
    }

    [Fact]
    public void CatmullRomCurveInterpolatesGivenPointsCorrectlyWhenPrecomputed()
    {
        AssertCatmullRomCurveValues(CatmullRomCurve.Precompute(CurvePoints));
    }

    private static readonly Point[][] InvalidCurvePoints =
    [
        // Not monotonically increasing in X.
        [new(0.2, 0.25), new(0.01, 0.25)],
        // X values not between 0.0 and 1.0.
        [new(0.2, 0.25), new(1.01, 0.25)],
        // Not a function at x = 0.
        [new(0.05, 0.50), new(0.50, 0.50), new(0.75, 0.75)],
        // Not a function at x = 1.
        [new(0.25, 0.25), new(0.50, 0.50), new(0.95, 0.51)],
        // Not a function in between.
        [new(0.5, 0.05), new(0.5, 0.95)],
    ];

    [DebugOnlyFact]
    public void CatmullRomCurveEnforcesContract()
    {
        Assert.ThrowsAny<AssertionError>(() => new CatmullRomCurve([]));
        Assert.ThrowsAny<AssertionError>(() => new CatmullRomCurve([Zero]));
        Assert.ThrowsAny<AssertionError>(() => new CatmullRomCurve([Zero, Zero]));

        foreach (Point[] points in InvalidCurvePoints)
        {
            Assert.False(CatmullRomCurve.ValidateControlPoints(points));
            Assert.ThrowsAny<AssertionError>(() => new CatmullRomCurve(points));
        }
    }

    [DebugOnlyFact]
    public void CatmullRomCurveEnforcesContractWhenPrecomputed()
    {
        Assert.ThrowsAny<AssertionError>(() => CatmullRomCurve.Precompute([]));
        Assert.ThrowsAny<AssertionError>(() => CatmullRomCurve.Precompute([Zero]));
        Assert.ThrowsAny<AssertionError>(() => CatmullRomCurve.Precompute([Zero, Zero]));

        foreach (Point[] points in InvalidCurvePoints)
        {
            Assert.ThrowsAny<AssertionError>(() => CatmullRomCurve.Precompute(points));
        }
    }

    [DebugOnlyFact]
    public void CatmullRomCurve_ValidationReasonsNameTheOffendingPoint()
    {
        var reasons = new List<string>();
        Assert.False(CatmullRomCurve.ValidateControlPoints(
            [new Point(0.2, 0.25), new Point(1.01, 0.25)],
            reasons: reasons));
        Assert.Equal(
            "Control points must have X values between 0.0 and 1.0, exclusive. "
            + "Point 3 has an x value (1.01) which is outside the range.",
            Assert.Single(reasons));

        AssertionError error = Assert.ThrowsAny<AssertionError>(() => new CatmullRomCurve([new Point(0.2, 0.25)]));
        Assert.Equal(
            "control points [Offset(0.2, 0.3)] could not be validated:\n"
            + "  There must be at least two points supplied to create a valid curve.",
            error.Message);
    }

    [Fact]
    public void Curve2D_FindInverseAndGenerateSamplesFollowTheSpline()
    {
        var spline = new CatmullRomSpline(
            SplinePoints,
            startHandle: new Point(0.0, -0.3),
            endHandle: new Point(1.3, 1.3));
        double t = spline.FindInverse(0.33);
        Assert.Equal(0.33, spline.Transform(t).X, 1e-5);

        List<Curve2DSample> samples = spline.GenerateSamples().ToList();
        Assert.Equal(0.0, samples[0].T);
        Assert.Equal(1.0, samples[^1].T);
        for (int i = 1; i < samples.Count; i++)
        {
            Assert.True(samples[i].T > samples[i - 1].T);
        }
    }
}
