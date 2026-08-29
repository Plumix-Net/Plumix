using Avalonia.Media;
using Plumix.Painting;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/painting/colors.dart (HSVColor)

namespace Plumix.Tests;

public sealed class HSVColorTests
{
    [Fact]
    public void FromAHSV_KeepsEveryChannel()
    {
        HSVColor color = HSVColor.FromAHSV(0.7, 28.0, 0.3, 0.6);

        Assert.Equal(0.7, color.Alpha);
        Assert.Equal(28.0, color.Hue);
        Assert.Equal(0.3, color.Saturation);
        Assert.Equal(0.6, color.Value);
    }

    [Theory]
    [InlineData(-0.1, 0.0, 0.0, 0.0)]
    [InlineData(1.1, 0.0, 0.0, 0.0)]
    [InlineData(0.0, -1.0, 0.0, 0.0)]
    [InlineData(0.0, 361.0, 0.0, 0.0)]
    [InlineData(0.0, 0.0, -0.1, 0.0)]
    [InlineData(0.0, 0.0, 1.1, 0.0)]
    [InlineData(0.0, 0.0, 0.0, -0.1)]
    [InlineData(0.0, 0.0, 0.0, 1.1)]
    public void Constructor_RejectsOutOfRangeChannels(double alpha, double hue, double saturation, double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HSVColor(alpha, hue, saturation, value));
    }

    [Fact]
    public void ToColor_RoundTripsThroughFromColor()
    {
        Color red = Color.FromArgb(255, 255, 0, 0);

        HSVColor hsv = HSVColor.FromColor(red);

        Assert.Equal(0.0, hsv.Hue);
        Assert.Equal(1.0, hsv.Saturation);
        Assert.Equal(1.0, hsv.Value);
        Assert.Equal(red, hsv.ToColor());
    }

    [Fact]
    public void FromColor_ReadsHueFromTheDominantChannel()
    {
        Assert.Equal(120.0, HSVColor.FromColor(Color.FromArgb(255, 0, 255, 0)).Hue);
        Assert.Equal(240.0, HSVColor.FromColor(Color.FromArgb(255, 0, 0, 255)).Hue);
    }

    [Fact]
    public void FromColor_OfGrey_HasZeroHueAndSaturation()
    {
        HSVColor grey = HSVColor.FromColor(Color.FromArgb(255, 128, 128, 128));

        Assert.Equal(0.0, grey.Hue);
        Assert.Equal(0.0, grey.Saturation);
    }

    [Fact]
    public void FromColor_OfBlack_HasZeroValue()
    {
        HSVColor black = HSVColor.FromColor(Color.FromArgb(255, 0, 0, 0));

        Assert.Equal(0.0, black.Hue);
        Assert.Equal(0.0, black.Saturation);
        Assert.Equal(0.0, black.Value);
    }

    [Fact]
    public void WithHue_MovesOnlyTheHue()
    {
        HSVColor original = HSVColor.FromAHSV(0.4, 60.0, 1.0, 1.0);

        HSVColor rotated = original.WithHue(62.0);

        Assert.Equal(62.0, rotated.Hue);
        Assert.Equal(original.Alpha, rotated.Alpha);
        Assert.Equal(original.Saturation, rotated.Saturation);
        Assert.Equal(original.Value, rotated.Value);
        Assert.Equal(60.0, original.Hue);
    }

    [Fact]
    public void With_ReplacesTheRequestedChannelOnly()
    {
        HSVColor original = HSVColor.FromAHSV(0.4, 60.0, 1.0, 1.0);

        Assert.Equal(0.25, original.WithAlpha(0.25).Alpha);
        Assert.Equal(0.5, original.WithSaturation(0.5).Saturation);
        Assert.Equal(0.75, original.WithValue(0.75).Value);
    }

    [Fact]
    public void Lerp_WithNullOperand_ScalesTheAlphaOfTheOther()
    {
        HSVColor color = HSVColor.FromAHSV(1.0, 90.0, 0.5, 0.5);

        HSVColor fromNull = HSVColor.Lerp(null, color, 0.25)!;
        HSVColor toNull = HSVColor.Lerp(color, null, 0.25)!;

        Assert.Equal(0.25, fromNull.Alpha);
        Assert.Equal(90.0, fromNull.Hue);
        Assert.Equal(0.75, toNull.Alpha);
        Assert.Equal(90.0, toNull.Hue);
    }

    [Fact]
    public void Lerp_WithBothNull_ReturnsNull()
    {
        Assert.Null(HSVColor.Lerp(null, null, 0.5));
    }

    [Fact]
    public void Lerp_InterpolatesEveryChannelSeparately()
    {
        HSVColor a = HSVColor.FromAHSV(0.0, 0.0, 0.0, 0.0);
        HSVColor b = HSVColor.FromAHSV(1.0, 100.0, 1.0, 1.0);

        HSVColor mid = HSVColor.Lerp(a, b, 0.5)!;

        Assert.Equal(0.5, mid.Alpha);
        Assert.Equal(50.0, mid.Hue);
        Assert.Equal(0.5, mid.Saturation);
        Assert.Equal(0.5, mid.Value);
    }

    [Fact]
    public void Lerp_WrapsTheHueIntoTheZeroTo360Range()
    {
        HSVColor a = HSVColor.FromAHSV(1.0, 0.0, 1.0, 1.0);
        HSVColor b = HSVColor.FromAHSV(1.0, 360.0, 1.0, 1.0);

        // lerpDouble(0, 360, 1.0) is 360, which Dart's `% 360.0` folds back onto 0.
        Assert.Equal(0.0, HSVColor.Lerp(a, b, 1.0)!.Hue);
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Assert.Equal(HSVColor.FromAHSV(0.4, 60.0, 1.0, 1.0), HSVColor.FromAHSV(0.4, 60.0, 1.0, 1.0));
        Assert.NotEqual(HSVColor.FromAHSV(0.4, 60.0, 1.0, 1.0), HSVColor.FromAHSV(0.4, 62.0, 1.0, 1.0));
    }

    [Fact]
    public void ToString_MatchesTheDartFormat()
    {
        Assert.Equal("HSVColor(0.4, 60.0, 1.0, 1.0)", HSVColor.FromAHSV(0.4, 60.0, 1.0, 1.0).ToString());
    }
}
