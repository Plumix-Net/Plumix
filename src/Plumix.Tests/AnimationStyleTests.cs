using System.Globalization;
using Plumix.Foundation;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/animation/animation_style_test.dart

namespace Plumix.Tests;

public sealed class AnimationStyleTests
{
    [Fact]
    public void DefaultsCopyEqualityAndNoAnimationMatchDart()
    {
        var style = new AnimationStyle();
        Assert.Null(style.Curve);
        Assert.Null(style.Duration);
        Assert.Null(style.ReverseCurve);
        Assert.Null(style.ReverseDuration);
        Assert.Equal(style, style.CopyWith());
        Assert.Equal(style.GetHashCode(), style.CopyWith().GetHashCode());
        Assert.Equal(TimeSpan.Zero, AnimationStyle.NoAnimation.Duration);
        Assert.Equal(TimeSpan.Zero, AnimationStyle.NoAnimation.ReverseDuration);
        Assert.Null(AnimationStyle.NoAnimation.Curve);
        Assert.Null(AnimationStyle.NoAnimation.ReverseCurve);
    }

    [Fact]
    public void CopyWithOverridesEveryPropertyAndMergeKeepsUnspecifiedProperties()
    {
        var original = new AnimationStyle(
            Curve: Curves.Ease,
            Duration: TimeSpan.FromSeconds(1),
            ReverseCurve: Curves.Ease,
            ReverseDuration: TimeSpan.FromSeconds(1));
        var expected = new AnimationStyle(
            Curve: Curves.Linear,
            Duration: TimeSpan.FromSeconds(2),
            ReverseCurve: Curves.Linear,
            ReverseDuration: TimeSpan.FromSeconds(2));
        Assert.Equal(expected, original.CopyWith(
            curve: expected.Curve,
            duration: expected.Duration,
            reverseCurve: expected.ReverseCurve,
            reverseDuration: expected.ReverseDuration));
        Assert.Equal(original, original.CopyWith());
        Assert.Equal(expected.GetHashCode(), original.Merge(expected).GetHashCode());
        Assert.Equal(expected, original.Merge(expected));
        Assert.Equal(
            original.CopyWith(curve: Curves.Linear, duration: TimeSpan.FromSeconds(2)),
            original.Merge(new AnimationStyle(Curve: Curves.Linear, Duration: TimeSpan.FromSeconds(2))));
        Assert.Same(original, original.Merge(null));
    }

    [Fact]
    public void LerpPreservesIdenticalStylesAndEndpointProperties()
    {
        Assert.Null(AnimationStyle.Lerp(null, null, 0));
        var a = new AnimationStyle(Curve: Curves.Ease, Duration: TimeSpan.FromSeconds(1));
        var b = new AnimationStyle(Curve: Curves.Linear, Duration: TimeSpan.FromSeconds(2));
        Assert.Same(a, AnimationStyle.Lerp(a, a, 0.5));
        Assert.Equal(a, AnimationStyle.Lerp(a, b, 0));
        Assert.Equal(b, AnimationStyle.Lerp(a, b, 1));
        Assert.Same(a.Curve, AnimationStyle.Lerp(a, b, 0)!.Curve);
        Assert.Same(b.Curve, AnimationStyle.Lerp(a, b, 1)!.Curve);
    }

    [Fact]
    public void LerpSmoothlyTransitionsAllFourProperties()
    {
        var a = new AnimationStyle(
            Curve: Curves.Ease,
            Duration: TimeSpan.FromSeconds(1),
            ReverseCurve: Curves.BounceIn,
            ReverseDuration: TimeSpan.FromSeconds(2));
        var b = new AnimationStyle(
            Curve: Curves.Linear,
            Duration: TimeSpan.FromSeconds(2),
            ReverseCurve: Curves.BounceOut,
            ReverseDuration: TimeSpan.FromSeconds(4));
        for (int styleStep = 1; styleStep <= 5; styleStep++)
        {
            double transition = styleStep / 6.0;
            AnimationStyle style = AnimationStyle.Lerp(a, b, transition)!;
            Assert.Equal(Interpolate(a.Duration!.Value, b.Duration!.Value, transition), style.Duration);
            Assert.Equal(
                Interpolate(a.ReverseDuration!.Value, b.ReverseDuration!.Value, transition),
                style.ReverseDuration);
            for (int curveStep = 0; curveStep <= 6; curveStep++)
            {
                double t = curveStep / 6.0;
                Assert.Equal(
                    (a.Curve!.Transform(t) * (1 - transition)) + (b.Curve!.Transform(t) * transition),
                    style.Curve!.Transform(t),
                    12);
                Assert.Equal(
                    (a.ReverseCurve!.Transform(t) * (1 - transition))
                    + (b.ReverseCurve!.Transform(t) * transition),
                    style.ReverseCurve!.Transform(t),
                    12);
            }
        }
    }

    [Theory]
    [InlineData(0.5, 1_000_000)]
    [InlineData(0.25, 1_500_000)]
    [InlineData(-0.5, 3_000_000)]
    [InlineData(1.5, -1_000_000)]
    public void NullDurationsInterpolateAsZeroAndAllowExtrapolation(double t, long microseconds)
    {
        var a = new AnimationStyle(Duration: TimeSpan.FromSeconds(2), ReverseDuration: TimeSpan.FromSeconds(2));
        AnimationStyle style = AnimationStyle.Lerp(a, null, t)!;
        Assert.Equal(TimeSpan.FromTicks(microseconds * TimeSpan.TicksPerMicrosecond), style.Duration);
        Assert.Equal(style.Duration, style.ReverseDuration);
        Assert.Equal(style, AnimationStyle.Lerp(a, new AnimationStyle(), t));
    }

    [Theory]
    [InlineData(19, 39, 0.5, 2)]
    [InlineData(-19, -39, 0.5, -2)]
    [InlineData(0, 10, 0.5, 1)]
    [InlineData(0, -10, 0.5, -1)]
    public void DurationLerpReadsWholeMicrosecondsAndRoundsAwayFromZero(
        long aTicks, long bTicks, double t, long expectedMicroseconds)
    {
        var a = new AnimationStyle(Duration: TimeSpan.FromTicks(aTicks));
        var b = new AnimationStyle(Duration: TimeSpan.FromTicks(bTicks));
        Assert.Equal(
            TimeSpan.FromTicks(expectedMicroseconds * TimeSpan.TicksPerMicrosecond),
            AnimationStyle.Lerp(a, b, t)!.Duration);
    }

    [Fact]
    public void BlendedCurvesHaveValueEqualityAndEqualCurvesBypassFurtherInterpolation()
    {
        var a = new AnimationStyle(Curve: Curves.Ease, ReverseCurve: Curves.BounceIn);
        var b = new AnimationStyle(Curve: Curves.Linear, ReverseCurve: Curves.BounceOut);
        AnimationStyle first = AnimationStyle.Lerp(a, b, 0.25)!;
        AnimationStyle second = AnimationStyle.Lerp(a, b, 0.25)!;
        Assert.NotSame(first.Curve, second.Curve);
        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, AnimationStyle.Lerp(a, b, 0.5));
        AnimationStyle nested = AnimationStyle.Lerp(first, second, 0.5)!;
        Assert.Same(first.Curve, nested.Curve);
        Assert.Same(first.ReverseCurve, nested.ReverseCurve);
        Assert.Same(first.Curve, AnimationStyle.Lerp(first, second, 1)!.Curve);
    }

    [Fact]
    public void NullCurvesUseLinearAndCurveDescriptionIsCultureIndependent()
    {
        var a = new AnimationStyle();
        var b = new AnimationStyle(Curve: Curves.Ease, ReverseCurve: Curves.BounceOut);
        AnimationStyle style = AnimationStyle.Lerp(a, b, 0.25)!;
        Assert.Equal((0.4 * 0.75) + (Curves.Ease.Transform(0.4) * 0.25), style.Curve!.Transform(0.4));
        Assert.Equal(
            (0.4 * 0.75) + (Curves.BounceOut.Transform(0.4) * 0.25),
            style.ReverseCurve!.Transform(0.4));
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal($"_LerpedCurve({Curves.Linear}, {Curves.Ease}, t: 0.25)", style.Curve.ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [DebugOnlyFact]
    public void BuiltInCurveDescriptionsKeepDartPrivateNames()
    {
        Assert.Equal("_Linear", Curves.Linear.ToString());
        Assert.Equal("_DecelerateCurve", Curves.Decelerate.ToString());
        Assert.Equal("_BounceInCurve", Curves.BounceIn.ToString());
        Assert.Equal("_BounceOutCurve", Curves.BounceOut.ToString());
        Assert.Equal("_BounceInOutCurve", Curves.BounceInOut.ToString());
    }

    [DebugOnlyFact]
    public void DebugPropertiesMatchFlutterDescriptionsAndFilterUnsetValues()
    {
        var defaults = new DiagnosticPropertiesBuilder();
        new AnimationStyle().DebugFillProperties(defaults);
        Assert.DoesNotContain(defaults.Properties, node => !node.IsFiltered(DiagnosticLevel.Info));
        var properties = new DiagnosticPropertiesBuilder();
        new AnimationStyle(
            Curve: Curves.EaseInOut,
            Duration: TimeSpan.FromSeconds(1),
            ReverseCurve: Curves.BounceInOut,
            ReverseDuration: TimeSpan.FromSeconds(2)).DebugFillProperties(properties);
        Assert.Equal(
            new[]
            {
                "curve: Cubic(0.42, 0.00, 0.58, 1.00)",
                "duration: 0:00:01.000000",
                "reverseCurve: _BounceInOutCurve",
                "reverseDuration: 0:00:02.000000",
            },
            properties.Properties
                .Where(node => !node.IsFiltered(DiagnosticLevel.Info))
                .Select(node => node.ToString()));
    }

    [DebugOnlyTheory]
    [InlineData(0, "0:00:00.000000")]
    [InlineData(1234567, "0:00:01.234567")]
    [InlineData(-1234567, "-0:00:01.234567")]
    [InlineData(90000000000, "25:00:00.000000")]
    public void DurationDiagnosticsUseTotalHoursAndMicroseconds(long microseconds, string expected)
    {
        var property = new DiagnosticsProperty<TimeSpan>(
            "duration", TimeSpan.FromTicks(microseconds * TimeSpan.TicksPerMicrosecond));
        Assert.Equal(expected, property.ToDescription());
    }

    private static TimeSpan Interpolate(TimeSpan a, TimeSpan b, double t)
    {
        double microseconds = (a.Ticks / TimeSpan.TicksPerMicrosecond * (1 - t))
                              + (b.Ticks / TimeSpan.TicksPerMicrosecond * t);
        return TimeSpan.FromTicks(
            (long)Math.Round(microseconds, MidpointRounding.AwayFromZero) * TimeSpan.TicksPerMicrosecond);
    }
}
