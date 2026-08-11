using Plumix;
using Xunit;

namespace Plumix.Tests;

public sealed class CurvesSplitTests
{
    [Fact]
    public void Split_MapsEachSegmentOntoItsOutputRange()
    {
        Curve curve = Curves.Split(0.4, endCurve: Curves.Linear);

        Assert.Equal(0.0, curve(0.0), precision: 6);
        Assert.Equal(0.4, curve(0.4), precision: 6);
        Assert.Equal(1.0, curve(1.0), precision: 6);
        // The leading half of each segment lands halfway through the matching output range.
        Assert.Equal(0.2, curve(0.2), precision: 6);
        Assert.Equal(0.7, curve(0.7), precision: 6);
    }

    [Fact]
    public void Split_UsesEaseOutCubicAfterTheSplitByDefault()
    {
        Curve curve = Curves.Split(0.5);

        Assert.Equal(0.5, curve(0.5), precision: 6);
        Assert.Equal(0.5 + (Curves.EaseOutCubic(0.5) * 0.5), curve(0.75), precision: 6);
        // easeOutCubic decelerates, so the second segment is ahead of a linear ramp.
        Assert.True(curve(0.75) > 0.75);
    }

    [Fact]
    public void Split_AppliesTheBeginCurveBeforeTheSplit()
    {
        Curve curve = Curves.Split(0.5, beginCurve: Curves.EaseOutCubic, endCurve: Curves.Linear);

        Assert.Equal(Curves.EaseOutCubic(0.5) * 0.5, curve(0.25), precision: 6);
    }

    [Fact]
    public void Split_RejectsOutOfRangeSplitPoints()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Curves.Split(-0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Curves.Split(1.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Curves.Split(double.NaN));
    }

    [Fact]
    public void LegacyDecelerateAndEaseOutCubic_MatchTheirCubicEndpoints()
    {
        Assert.Equal(0.0, Curves.LegacyDecelerate(0.0), precision: 6);
        Assert.Equal(1.0, Curves.LegacyDecelerate(1.0), precision: 6);
        // Cubic(0, 0, 0.2, 1) decelerates from the start.
        Assert.True(Curves.LegacyDecelerate(0.5) > 0.5);

        Assert.Equal(0.0, Curves.EaseOutCubic(0.0), precision: 6);
        Assert.Equal(1.0, Curves.EaseOutCubic(1.0), precision: 6);
        Assert.True(Curves.EaseOutCubic(0.5) > 0.5);
    }
}
