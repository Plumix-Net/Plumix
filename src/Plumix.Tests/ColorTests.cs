using Plumix.Foundation;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// Ports engine/src/flutter/testing/dart/color_test.dart (dart:ui `Color` and `ColorSpace`), plus the
// C#-only backend conversion.
public sealed class ColorTests
{
    // color_test.dart's `colorMatches`: every component within `threshold` and the same color space.
    private static void AssertColorMatches(Color expected, Color? actual, double threshold = 1 / 255.0) =>
        ColorMatchers.AssertSameColorAs(expected, actual, threshold);

    private sealed class NotAColor(uint value) : Color(value);

    // color_test.dart's `DynamicColorClass`: overrides `value`, and the base getters must read it.
    private sealed class DynamicColorClass(uint newValue) : Color(0)
    {
        public override uint Value => newValue;
    }

    [Fact]
    public void ColorAccessorsShouldWork()
    {
        var foo = new Color(0x12345678);
        Assert.Equal(0x12, foo.Alpha);
        Assert.Equal(0x34, foo.Red);
        Assert.Equal(0x56, foo.Green);
        Assert.Equal(0x78, foo.Blue);
    }

    [Fact]
    public void PaintSetToBlack()
    {
        var c = new Color(0x00000000);
        var p = new Paint { Color = c };
        Assert.Equal(new Color(0x00000000), c);
        Assert.Equal(c, p.Color);
    }

    [Fact]
    public void ColorCreatedWithOutOfBoundsValue()
    {
        // `Color(0x100 << 24)`: only the lower 32 bits are kept.
        var c = new Color(unchecked((uint)(0x100L << 24)));
        var p = new Paint { Color = c };
        Assert.Equal(new Color(0x00000000), p.Color);
    }

    [Fact]
    public void TwoColorsAreOnlyEqualIfTheyHaveTheSameRuntimeType()
    {
        Assert.Equal(new Color(0x12345678), new Color(0x12345678));
        Assert.NotEqual(new Color(0x12345678), new Color(0x87654321));
        Assert.NotEqual(new Color(0x12345678), new NotAColor(0x12345678));
        Assert.NotEqual<Color>(new NotAColor(0x12345678), new Color(0x12345678));
        Assert.Equal<Color>(new NotAColor(0x12345678), new NotAColor(0x12345678));
        Assert.True(new Color(0x12345678) == new Color(0x12345678));
        Assert.True(new Color(0x12345678) != new NotAColor(0x12345678));
    }

    [Fact]
    public void Lerp()
    {
        Assert.Equal(new Color(0x00000000), Color.Lerp(new Color(0x00000000), new Color(0xFFFFFFFF), 0.0));
        AssertColorMatches(new Color(0x7F7F7F7F), Color.Lerp(new Color(0x00000000), new Color(0xFFFFFFFF), 0.5));
        Assert.Equal(new Color(0xFFFFFFFF), Color.Lerp(new Color(0x00000000), new Color(0xFFFFFFFF), 1.0));
        Assert.Equal(new Color(0x00000000), Color.Lerp(new Color(0x00000000), new Color(0xFFFFFFFF), -0.1));
        Assert.Equal(new Color(0xFFFFFFFF), Color.Lerp(new Color(0x00000000), new Color(0xFFFFFFFF), 1.1));

        // Prevent regression: https://github.com/flutter/flutter/issues/67423
        Assert.Equal(new Color(0xFFFFFFFF), Color.Lerp(new Color(0xFFFFFFFF), new Color(0xFFFFFFFF), 0.04));
    }

    [Fact]
    public void Lerp_NullEndpointsScaleTheOtherColorsAlpha()
    {
        Assert.Null(Color.Lerp(null, null, 0.5));
        Assert.Equal(Color.From(alpha: 0.25, red: 1, green: 0, blue: 0), Color.Lerp(null, new Color(0xFFFF0000), 0.25));
        Assert.Equal(Color.From(alpha: 0.75, red: 1, green: 0, blue: 0), Color.Lerp(new Color(0xFFFF0000), null, 0.25));
    }

    [Fact]
    public void Lerp_SameColorSpaces()
    {
        Assert.Equal(
            Color.From(alpha: 1, red: 0.2, green: 0, blue: 0, colorSpace: ColorSpace.DisplayP3),
            Color.Lerp(
                Color.From(alpha: 1, red: 0, green: 0, blue: 0, colorSpace: ColorSpace.DisplayP3),
                Color.From(alpha: 1, red: 1, green: 0, blue: 0, colorSpace: ColorSpace.DisplayP3),
                0.2));
    }

    [Fact]
    public void Lerp_MixedColorSpacesProducesDisplayP3Result()
    {
        Color? result = Color.Lerp(
            Color.From(alpha: 1, red: 1, green: 0, blue: 0, colorSpace: ColorSpace.DisplayP3),
            Color.From(alpha: 1, red: 0, green: 0, blue: 1),
            0.0);
        Assert.Equal(ColorSpace.DisplayP3, result!.ColorSpace);
    }

    [Fact]
    public void Lerp_MixedColorSpacesSrgbXAndDisplayP3YAtZero()
    {
        Color? result = Color.Lerp(
            Color.From(alpha: 1, red: 1, green: 0, blue: 0),
            Color.From(alpha: 1, red: 0, green: 1, blue: 0, colorSpace: ColorSpace.DisplayP3),
            0.0);
        Assert.Equal(ColorSpace.DisplayP3, result!.ColorSpace);
        Color expectedP3 = Color.From(alpha: 1, red: 1, green: 0, blue: 0)
            .WithValues(colorSpace: ColorSpace.DisplayP3);
        AssertColorMatches(expectedP3, result);
    }

    [Fact]
    public void Lerp_MixedColorSpacesDisplayP3XAndSrgbYAtOne()
    {
        Color? result = Color.Lerp(
            Color.From(alpha: 1, red: 0, green: 1, blue: 0, colorSpace: ColorSpace.DisplayP3),
            Color.From(alpha: 1, red: 1, green: 0, blue: 0),
            1.0);
        Assert.Equal(ColorSpace.DisplayP3, result!.ColorSpace);
        Color expectedP3 = Color.From(alpha: 1, red: 1, green: 0, blue: 0)
            .WithValues(colorSpace: ColorSpace.DisplayP3);
        AssertColorMatches(expectedP3, result);
    }

    [Fact]
    public void Lerp_MixedColorSpacesAtMidpoint()
    {
        Color srgbRed = Color.From(alpha: 1, red: 1, green: 0, blue: 0);
        Color p3Green = Color.From(alpha: 1, red: 0, green: 1, blue: 0, colorSpace: ColorSpace.DisplayP3);
        Color? result = Color.Lerp(srgbRed, p3Green, 0.5);
        Assert.Equal(ColorSpace.DisplayP3, result!.ColorSpace);
        Color srgbRedAsP3 = srgbRed.WithValues(colorSpace: ColorSpace.DisplayP3);
        Color expected = Color.From(
            alpha: (srgbRedAsP3.A + p3Green.A) / 2,
            red: (srgbRedAsP3.R + p3Green.R) / 2,
            green: (srgbRedAsP3.G + p3Green.G) / 2,
            blue: (srgbRedAsP3.B + p3Green.B) / 2,
            colorSpace: ColorSpace.DisplayP3);
        AssertColorMatches(expected, result, threshold: 1e-4);
    }

    [Fact]
    public void Lerp_MixedColorSpacesWithDifferentAlphaValues()
    {
        Color srgbColor = Color.From(alpha: 0.5, red: 1, green: 0, blue: 0);
        Color p3Color = Color.From(alpha: 1, red: 0, green: 0, blue: 1, colorSpace: ColorSpace.DisplayP3);
        Color? result = Color.Lerp(srgbColor, p3Color, 0.5);
        Assert.Equal(ColorSpace.DisplayP3, result!.ColorSpace);
        Assert.Equal(0.75, result.A, 4);
    }

    [Fact]
    public void AlphaBlend()
    {
        Assert.Equal(new Color(0x00000000), Color.AlphaBlend(new Color(0x00000000), new Color(0x00000000)));
        Assert.Equal(new Color(0xFFFFFFFF), Color.AlphaBlend(new Color(0x00000000), new Color(0xFFFFFFFF)));
        Assert.Equal(new Color(0xFFFFFFFF), Color.AlphaBlend(new Color(0xFFFFFFFF), new Color(0x00000000)));
        Assert.Equal(new Color(0xFFFFFFFF), Color.AlphaBlend(new Color(0xFFFFFFFF), new Color(0xFFFFFFFF)));
        Assert.Equal(new Color(0xFF808080), Color.AlphaBlend(new Color(0x80FFFFFF), new Color(0xFF000000)));
        AssertColorMatches(new Color(0xFFBFBFBF), Color.AlphaBlend(new Color(0x80808080), new Color(0xFFFFFFFF)));
        AssertColorMatches(new Color(0xFF404040), Color.AlphaBlend(new Color(0x80808080), new Color(0xFF000000)));
        AssertColorMatches(new Color(0xFF000000), Color.AlphaBlend(new Color(0x01020304), new Color(0xFF000000)));
        AssertColorMatches(new Color(0xFF020304), Color.AlphaBlend(new Color(0x11223344), new Color(0xFF000000)));
        AssertColorMatches(new Color(0x88040608), Color.AlphaBlend(new Color(0x11223344), new Color(0x80000000)));
    }

    [Fact]
    public void AlphaBlend_KeepsColorSpace()
    {
        Assert.Equal(
            Color.From(alpha: 1, red: 0.5, green: 0.5, blue: 0.5, colorSpace: ColorSpace.DisplayP3),
            Color.AlphaBlend(
                Color.From(alpha: 0.5, red: 1, green: 1, blue: 1, colorSpace: ColorSpace.DisplayP3),
                Color.From(alpha: 1, red: 0, green: 0, blue: 0, colorSpace: ColorSpace.DisplayP3)));
    }

    [Fact]
    public void ComputeGrayLuminance()
    {
        // Each color component is at 20%.
        var lightGray = new Color(0xFF333333);
        // Relative luminance's formula is just the linearized color value for gray.
        // ((0.2 + 0.055) / 1.055) ^ 2.4.
        Assert.Equal(0.033104766570885055, lightGray.ComputeLuminance());
    }

    [Fact]
    public void ComputeColorLuminance()
    {
        var brightRed = new Color(0xFFFF3B30);
        // 0.2126 * ((1.0 + 0.055) / 1.055) ^ 2.4 +
        // 0.7152 * ((0.23137254902 +0.055) / 1.055) ^ 2.4 +
        // 0.0722 * ((0.18823529411 + 0.055) / 1.055) ^ 2.4
        Assert.Equal(0.24601329637099723, brightRed.ComputeLuminance());
    }

    [Fact]
    public void FromAndAccessors()
    {
        Color color = Color.From(alpha: 0.1, red: 0.2, green: 0.3, blue: 0.4);
        Assert.Equal(0.1, color.A);
        Assert.Equal(0.2, color.R);
        Assert.Equal(0.3, color.G);
        Assert.Equal(0.4, color.B);
        Assert.Equal(ColorSpace.SRGB, color.ColorSpace);

        Assert.Equal(26, color.Alpha);
        Assert.Equal(51, color.Red);
        Assert.Equal(77, color.Green);
        Assert.Equal(102, color.Blue);

        Assert.Equal(0x1a334d66u, color.Value);
    }

    [Fact]
    public void FromARGBAndAccessors()
    {
        Color color = Color.FromARGB(10, 20, 35, 47);
        Assert.Equal(10, color.Alpha);
        Assert.Equal(20, color.Red);
        Assert.Equal(35, color.Green);
        Assert.Equal(47, color.Blue);
    }

    [Fact]
    public void FromRGBO_KeepsTheExactOpacity()
    {
        Color color = Color.FromRGBO(10, 20, 30, 0.08);
        Assert.Equal(0.08, color.A);
        Assert.Equal(20, color.Alpha);
        Assert.Equal(10 / 255.0, color.R);
    }

    [Fact]
    public void ConstructorAndAccessors()
    {
        var color = new Color(0xffeeddcc);
        Assert.Equal(0xff, color.Alpha);
        Assert.Equal(0xee, color.Red);
        Assert.Equal(0xdd, color.Green);
        Assert.Equal(0xcc, color.Blue);
    }

    [Fact]
    public void P3ToExtendedSrgb()
    {
        Color p3 = Color.From(alpha: 1, red: 1, green: 0, blue: 0, colorSpace: ColorSpace.DisplayP3);
        Color srgb = p3.WithValues(colorSpace: ColorSpace.ExtendedSRGB);
        Assert.Equal(1.0, srgb.A);
        Assert.Equal(1.0931, srgb.R, 1e-4);
        Assert.Equal(-0.22684034705162098, srgb.G, 1e-4);
        Assert.Equal(-0.15007957816123998, srgb.B, 1e-4);
        Assert.Equal(ColorSpace.ExtendedSRGB, srgb.ColorSpace);
    }

    [Fact]
    public void P3ToSrgb()
    {
        Color p3 = Color.From(alpha: 1, red: 1, green: 0, blue: 0, colorSpace: ColorSpace.DisplayP3);
        Color srgb = p3.WithValues(colorSpace: ColorSpace.SRGB);
        Assert.Equal(1.0, srgb.A);
        Assert.Equal(1, srgb.R, 1e-4);
        Assert.Equal(0, srgb.G, 1e-4);
        Assert.Equal(0, srgb.B, 1e-4);
        Assert.Equal(ColorSpace.SRGB, srgb.ColorSpace);
    }

    [Fact]
    public void ExtendedSrgbToP3()
    {
        Color srgb = Color.From(alpha: 1, red: 1.0931, green: -0.2268, blue: -0.1501, colorSpace: ColorSpace.ExtendedSRGB);
        Color p3 = srgb.WithValues(colorSpace: ColorSpace.DisplayP3);
        Assert.Equal(1.0, p3.A);
        Assert.Equal(1, p3.R, 1e-4);
        Assert.Equal(0, p3.G, 1e-4);
        Assert.Equal(0, p3.B, 1e-4);
        Assert.Equal(ColorSpace.DisplayP3, p3.ColorSpace);
    }

    [Fact]
    public void ExtendedSrgbToP3Clamped()
    {
        Color srgb = Color.From(alpha: 1, red: 2, green: 0, blue: 0, colorSpace: ColorSpace.ExtendedSRGB);
        Color p3 = srgb.WithValues(colorSpace: ColorSpace.DisplayP3);
        Assert.Equal(1.0, srgb.A);
        Assert.True(p3.R <= 1.0);
        Assert.True(p3.G <= 1.0);
        Assert.True(p3.B <= 1.0);
        Assert.True(p3.R >= 0.0);
        Assert.True(p3.G >= 0.0);
        Assert.True(p3.B >= 0.0);
    }

    [Fact]
    public void HashConsidersColorSpace()
    {
        Color srgb = Color.From(alpha: 1, red: 1, green: 0, blue: 0);
        Color p3 = Color.From(alpha: 1, red: 1, green: 0, blue: 0, colorSpace: ColorSpace.DisplayP3);
        Assert.NotEqual(srgb.GetHashCode(), p3.GetHashCode());
    }

    [Fact]
    public void EqualityConsidersColorSpace()
    {
        Color srgb = Color.From(alpha: 1, red: 1, green: 0, blue: 0);
        Color p3 = Color.From(alpha: 1, red: 1, green: 0, blue: 0, colorSpace: ColorSpace.DisplayP3);
        Assert.NotEqual(srgb, p3);
    }

    // Regression test for https://github.com/flutter/flutter/issues/41257
    // CupertinoDynamicColor was overriding base class and calling super(0).
    [Fact]
    public void SubclassOfColorCanOverrideValue()
    {
        var color = new DynamicColorClass(0xF0E0D0C0);
        Assert.Equal(0xF0E0D0C0, color.Value);
        // Call base class member, make sure it uses overridden value.
        Assert.Equal(0xE0, color.Red);
    }

    [Fact]
    public void ToARGB32ConvertsToA32BitInteger()
    {
        Color color = Color.From(alpha: 0.1, red: 0.2, green: 0.3, blue: 0.4);
        Assert.Equal(0x1a334d66u, color.ToARGB32());
    }

    [Fact]
    public void WithValues_ReturnsThisWhenNothingChanges()
    {
        var color = new Color(0xFF102030);
        Assert.Same(color, color.WithValues());
        Assert.Same(color, color.WithValues(colorSpace: ColorSpace.SRGB));
        Assert.Equal(Color.From(alpha: 0.5, red: color.R, green: color.G, blue: color.B), color.WithValues(alpha: 0.5));
    }

    [Fact]
    public void WithOpacity_QuantizesLikeWithAlphaAndRoundsHalfAwayFromZero()
    {
        var color = new Color(0xFF102030);
        // 255 * 0.3 = 76.5, which Dart's `round()` takes to 77 (C#'s default banker's rounding gives 76).
        Assert.Equal(new Color(0x4D102030), color.WithOpacity(0.3));
        Assert.Equal(Color.FromARGB(128, 0x10, 0x20, 0x30), color.WithOpacity(0.5));
    }

    [DebugOnlyFact]
    public void WithOpacity_AssertsTheOpacityRange() =>
        Assert.Throws<AssertionError>(() => new Color(0xFF102030).WithOpacity(1.5));

    [Fact]
    public void WithChannel_ReplacesOneEightBitChannel()
    {
        var color = new Color(0x11223344);
        Assert.Equal(new Color(0x99223344), color.WithAlpha(0x99));
        Assert.Equal(new Color(0x11993344), color.WithRed(0x99));
        Assert.Equal(new Color(0x11229944), color.WithGreen(0x99));
        Assert.Equal(new Color(0x11223399), color.WithBlue(0x99));
    }

    [Fact]
    public void Opacity_IsTheEightBitAlphaOverTwoFiftyFive()
    {
        Assert.Equal(0x80 / 255.0, new Color(0x80000000).Opacity);
        Assert.Equal(0.5, Color.From(alpha: 0.5, red: 0, green: 0, blue: 0).A);
    }

    [Theory]
    [InlineData(-0.5, 0)]
    [InlineData(0.5, 128)]
    [InlineData(0.3, 77)]
    [InlineData(1.5, 255)]
    public void GetAlphaFromOpacity_ClampsAndRounds(double opacity, int expected) =>
        Assert.Equal(expected, Color.GetAlphaFromOpacity(opacity));

    [Fact]
    public void ToString_MatchesDartUi()
    {
        Assert.Equal(
            "Color(alpha: 1.0000, red: 0.3922, green: 0.5843, blue: 0.9294, colorSpace: ColorSpace.sRGB)",
            new Color(0xFF6495ED).ToString());
        Assert.Equal(
            "Color(alpha: 0.5000, red: 1.0000, green: 0.0000, blue: 0.0000, colorSpace: ColorSpace.displayP3)",
            Color.From(alpha: 0.5, red: 1, green: 0, blue: 0, colorSpace: ColorSpace.DisplayP3).ToString());
        Assert.Equal(
            "ColorSpace.extendedSRGB",
            Color.From(1, 0, 0, 0, ColorSpace.ExtendedSRGB).ToString().Split("colorSpace: ")[1].TrimEnd(')'));
    }

    [Fact]
    public void BackendConversion_QuantizesToTheEightBitValue()
    {
        Avalonia.Media.Color backend = Color.From(alpha: 0.5, red: 1, green: 0, blue: 0);
        Assert.Equal(0x80FF0000u, backend.ToUInt32());
        Assert.Equal(new Color(0x80FF0000), (Color)backend);
    }
}
