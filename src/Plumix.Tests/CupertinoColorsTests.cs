using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Mirrors cupertino_ui/test/colors_test.dart.
public sealed class CupertinoColorsTests
{
    private static readonly Color Color0 = new Color(0xFF000000);
    private static readonly Color Color1 = new Color(0xFF000001);
    private static readonly Color Color2 = new Color(0xFF000002);
    private static readonly Color Color3 = new Color(0xFF000003);
    private static readonly Color Color4 = new Color(0xFF000004);
    private static readonly Color Color5 = new Color(0xFF000005);
    private static readonly Color Color6 = new Color(0xFF000006);
    private static readonly Color Color7 = new Color(0xFF000007);

    /// A color that depends on brightness, accessibility contrast and interface elevation.
    private static readonly CupertinoDynamicColor DynamicColor = new(
        color: Color0,
        darkColor: Color1,
        highContrastColor: Color3,
        darkHighContrastColor: Color5,
        elevatedColor: Color2,
        darkElevatedColor: Color4,
        highContrastElevatedColor: Color6,
        darkHighContrastElevatedColor: Color7);

    /// A color that uses Color0 in every circumstance.
    private static readonly CupertinoDynamicColor NotSoDynamicColor1 = new(
        color: Color0,
        darkColor: Color0,
        highContrastColor: Color0,
        darkHighContrastColor: Color0,
        elevatedColor: Color0,
        darkElevatedColor: Color0,
        highContrastElevatedColor: Color0,
        darkHighContrastElevatedColor: Color0);

    /// A color that uses Color1 in light mode and Color0 in dark mode.
    private static readonly CupertinoDynamicColor VibrancyDependentColor1 = new(
        color: Color1,
        darkColor: Color0,
        highContrastColor: Color1,
        darkHighContrastColor: Color0,
        elevatedColor: Color1,
        darkElevatedColor: Color0,
        highContrastElevatedColor: Color1,
        darkHighContrastElevatedColor: Color0);

    /// A color that uses Color1 at normal contrast and Color0 at high contrast.
    private static readonly CupertinoDynamicColor ContrastDependentColor1 = new(
        color: Color1,
        darkColor: Color1,
        highContrastColor: Color0,
        darkHighContrastColor: Color0,
        elevatedColor: Color1,
        darkElevatedColor: Color1,
        highContrastElevatedColor: Color0,
        darkHighContrastElevatedColor: Color0);

    /// A color that uses Color1 at base elevation and Color0 when elevated.
    private static readonly CupertinoDynamicColor ElevationDependentColor1 = new(
        color: Color1,
        darkColor: Color1,
        highContrastColor: Color1,
        darkHighContrastColor: Color1,
        elevatedColor: Color0,
        darkElevatedColor: Color0,
        highContrastElevatedColor: Color0,
        darkHighContrastElevatedColor: Color0);

    [Fact]
    public void Equality_ComparesEveryVariant()
    {
        Assert.Equal(
            DynamicColor,
            new CupertinoDynamicColor(
                color: Color0,
                darkColor: Color1,
                highContrastColor: Color3,
                darkHighContrastColor: Color5,
                elevatedColor: Color2,
                darkElevatedColor: Color4,
                highContrastElevatedColor: Color6,
                darkHighContrastElevatedColor: Color7));

        Assert.NotEqual(NotSoDynamicColor1, VibrancyDependentColor1);
        Assert.NotEqual(NotSoDynamicColor1, ContrastDependentColor1);
        Assert.NotEqual(
            VibrancyDependentColor1,
            new CupertinoDynamicColor(
                color: Color0,
                darkColor: Color0,
                highContrastColor: Color0,
                darkHighContrastColor: Color0,
                elevatedColor: Color0,
                darkElevatedColor: Color0,
                highContrastElevatedColor: Color0,
                darkHighContrastElevatedColor: Color0));
        Assert.True(NotSoDynamicColor1 != VibrancyDependentColor1);
        Assert.True(DynamicColor == new CupertinoDynamicColor(
            color: Color0,
            darkColor: Color1,
            highContrastColor: Color3,
            darkHighContrastColor: Color5,
            elevatedColor: Color2,
            darkElevatedColor: Color4,
            highContrastElevatedColor: Color6,
            darkHighContrastElevatedColor: Color7));
    }

    [Fact]
    public void ToString_ListsOnlyTheVariantsTheColorDependsOn()
    {
        Assert.Contains(
            $"CupertinoDynamicColor(*color = {Color0}*, "
            + $"darkColor = {Color1}, "
            + $"highContrastColor = {Color3}, "
            + $"darkHighContrastColor = {Color5}, "
            + $"elevatedColor = {Color2}, "
            + $"darkElevatedColor = {Color4}, "
            + $"highContrastElevatedColor = {Color6}, "
            + $"darkHighContrastElevatedColor = {Color7}",
            DynamicColor.ToString());

        Assert.Contains($"CupertinoDynamicColor(*color = {Color0}*", NotSoDynamicColor1.ToString());
        Assert.Contains(
            $"CupertinoDynamicColor(*color = {Color1}*, darkColor = {Color0}",
            VibrancyDependentColor1.ToString());
        Assert.Contains(
            $"CupertinoDynamicColor(*color = {Color1}*, highContrastColor = {Color0}",
            ContrastDependentColor1.ToString());
        Assert.Contains(
            $"CupertinoDynamicColor(*color = {Color1}*, elevatedColor = {Color0}",
            ElevationDependentColor1.ToString());
        Assert.Contains(
            $"CupertinoDynamicColor(*color = {Color0}*, "
            + $"darkColor = {Color1}, "
            + $"highContrastColor = {Color2}, "
            + $"darkHighContrastColor = {Color3}",
            CupertinoDynamicColor
                .WithBrightnessAndContrast(Color0, Color1, Color2, Color3)
                .ToString());
        Assert.Contains("UNRESOLVED", DynamicColor.ToString());
    }

    [Fact]
    public void ToString_UsesTheDebugLabelWhenOneWasGiven()
    {
        Assert.StartsWith("systemBlue(", CupertinoColors.SystemBlue.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void MaybeResolve_ReturnsNullForANullColor()
    {
        Color? resolved = null;
        using var harness = new CupertinoThemeTestHarness(new Builder(context =>
        {
            resolved = CupertinoDynamicColor.MaybeResolve(null, context);
            return new SizedBox();
        }));

        Assert.Null(resolved);
    }

    [Fact]
    public void WithBrightness_ProducesAColorThatOnlyDependsOnVibrancy()
    {
        Assert.Equal(VibrancyDependentColor1, CupertinoDynamicColor.WithBrightness(Color1, Color0));
    }

    [Fact]
    public void WithBrightnessAndContrast_ProducesAColorThatDependsOnContrastAndVibrancy()
    {
        Assert.Equal(
            ContrastDependentColor1,
            CupertinoDynamicColor.WithBrightnessAndContrast(Color1, Color1, Color0, Color0));

        Assert.Equal(
            new CupertinoDynamicColor(
                color: Color0,
                darkColor: Color1,
                highContrastColor: Color2,
                darkHighContrastColor: Color3,
                elevatedColor: Color0,
                darkElevatedColor: Color1,
                highContrastElevatedColor: Color2,
                darkHighContrastElevatedColor: Color3),
            CupertinoDynamicColor.WithBrightnessAndContrast(Color0, Color1, Color2, Color3));
    }

    [Fact]
    public void PlainColor_ResolvesToItself()
    {
        Assert.Equal(Color4, Resolve(Color4, child => child));
    }

    [Fact]
    public void NonDynamicColor_ResolvesWithoutClaimingAnyDependency()
    {
        // No MediaQuery, no CupertinoTheme and no CupertinoUserInterfaceLevel ancestor: a color that
        // does not vary must resolve anyway.
        ColorMatchers.AssertSameColorAs(Color0, Resolve(NotSoDynamicColor1, child => child));
    }

    [Fact]
    public void VibrancyDependentColor_FollowsBrightnessAndPrefersTheTheme()
    {
        ColorMatchers.AssertSameColorAs(
            Color1,
            Resolve(VibrancyDependentColor1, child => new MediaQuery(new MediaQueryData(), child)));

        ColorMatchers.AssertSameColorAs(
            Color0,
            Resolve(
                VibrancyDependentColor1,
                child => new MediaQuery(
                    new MediaQueryData(PlatformBrightness: PlatformBrightness.Dark),
                    child)));

        // CupertinoTheme takes precedence over MediaQuery.
        ColorMatchers.AssertSameColorAs(
            Color1,
            Resolve(
                VibrancyDependentColor1,
                child => new CupertinoTheme(
                    new CupertinoThemeData(brightness: PlatformBrightness.Light),
                    new MediaQuery(
                        new MediaQueryData(PlatformBrightness: PlatformBrightness.Dark),
                        child))));
    }

    [Fact]
    public void ContrastDependentColor_FollowsTheAccessibilityContrastSetting()
    {
        ColorMatchers.AssertSameColorAs(
            Color1,
            Resolve(ContrastDependentColor1, child => new MediaQuery(new MediaQueryData(), child)));

        ColorMatchers.AssertSameColorAs(
            Color0,
            Resolve(
                ContrastDependentColor1,
                child => new MediaQuery(new MediaQueryData(HighContrast: true), child)));
    }

    [Fact]
    public void ElevationDependentColor_FollowsTheUserInterfaceLevel()
    {
        ColorMatchers.AssertSameColorAs(
            Color1,
            Resolve(
                ElevationDependentColor1,
                child => new CupertinoUserInterfaceLevel(CupertinoUserInterfaceLevelData.Base, child)));

        ColorMatchers.AssertSameColorAs(
            Color0,
            Resolve(
                ElevationDependentColor1,
                child => new CupertinoUserInterfaceLevel(CupertinoUserInterfaceLevelData.Elevated, child)));
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 1)]
    [InlineData(false, true, 2)]
    [InlineData(true, true, 3)]
    [InlineData(true, false, 4, true)]
    [InlineData(false, true, 5, true)]
    [InlineData(true, true, 6, true)]
    [InlineData(false, false, 7, true)]
    public void ColorWithAllThreeDependencies_SelectsEveryVariant(
        bool dark,
        bool highContrast,
        int expectedIndex,
        bool elevated = false)
    {
        CupertinoDynamicColor rainbow = new(
            color: Color0,
            darkColor: Color1,
            highContrastColor: Color2,
            darkHighContrastColor: Color3,
            elevatedColor: Color7,
            darkElevatedColor: Color4,
            highContrastElevatedColor: Color5,
            darkHighContrastElevatedColor: Color6);
        Color[] palette = [Color0, Color1, Color2, Color3, Color4, Color5, Color6, Color7];

        Color resolved = Resolve(
            rainbow,
            child => new MediaQuery(
                new MediaQueryData(
                    PlatformBrightness: dark ? PlatformBrightness.Dark : PlatformBrightness.Light,
                    HighContrast: highContrast),
                new CupertinoUserInterfaceLevel(
                    elevated ? CupertinoUserInterfaceLevelData.Elevated : CupertinoUserInterfaceLevelData.Base,
                    child)));

        ColorMatchers.AssertSameColorAs(palette[expectedIndex], resolved);
    }

    [Fact]
    public void ResolveFrom_KeepsEveryVariantAndOnlySwapsTheEffectiveValue()
    {
        CupertinoDynamicColor? resolved = null;
        using var harness = new CupertinoThemeTestHarness(new MediaQuery(
            new MediaQueryData(PlatformBrightness: PlatformBrightness.Dark, HighContrast: true),
            new CupertinoUserInterfaceLevel(
                CupertinoUserInterfaceLevelData.Elevated,
                new Builder(context =>
                {
                    resolved = DynamicColor.ResolveFrom(context);
                    return new SizedBox();
                }))));

        Assert.NotNull(resolved);
        ColorMatchers.AssertSameColorAs(Color7, resolved!);
        Assert.Equal(Color0, resolved.Color);
        Assert.Equal(Color1, resolved.DarkColor);
        Assert.Equal(Color3, resolved.HighContrastColor);
        Assert.Equal(Color2, resolved.ElevatedColor);
        // Dart prints the resolving element's widget, not UNRESOLVED.
        Assert.Contains("resolved by: Builder", resolved.ToString());
        // The effective value is part of equality, so a resolved color differs from its source.
        Assert.NotEqual(DynamicColor, resolved);
    }

    [Fact]
    public void Palette_MatchesTheAppleSystemColorTable()
    {
        Assert.Equal(new Color(0xFFFFFFFF), CupertinoColors.White);
        Assert.Equal(new Color(0xFF000000), CupertinoColors.Black);
        Assert.Equal(new Color(0x00000000), CupertinoColors.Transparent);
        Assert.Equal(new Color(0xFFE5E5EA), CupertinoColors.LightBackgroundGray);
        Assert.Equal(new Color(0xFFEFEFF4), CupertinoColors.ExtraLightBackgroundGray);
        Assert.Equal(new Color(0xFF171717), CupertinoColors.DarkBackgroundGray);

        Assert.Equal(Color.FromARGB(255, 0, 122, 255), CupertinoColors.SystemBlue.Color);
        Assert.Equal(Color.FromARGB(255, 10, 132, 255), CupertinoColors.SystemBlue.DarkColor);
        Assert.Equal(Color.FromARGB(255, 0, 64, 221), CupertinoColors.SystemBlue.HighContrastColor);
        Assert.Equal(Color.FromARGB(255, 64, 156, 255), CupertinoColors.SystemBlue.DarkHighContrastColor);
        // withBrightnessAndContrast mirrors the base variants onto the elevated ones.
        Assert.Equal(CupertinoColors.SystemBlue.Color, CupertinoColors.SystemBlue.ElevatedColor);
        Assert.Equal(CupertinoColors.SystemBlue.DarkColor, CupertinoColors.SystemBlue.DarkElevatedColor);

        Assert.Equal(new Color(0xFF999999), CupertinoColors.InactiveGray.Color);
        Assert.Equal(new Color(0xFF757575), CupertinoColors.InactiveGray.DarkColor);

        Assert.Equal(Color.FromARGB(255, 0, 0, 0), CupertinoColors.Label.Color);
        Assert.Equal(Color.FromARGB(255, 255, 255, 255), CupertinoColors.Label.DarkColor);
        Assert.Equal(Color.FromARGB(153, 60, 60, 67), CupertinoColors.SecondaryLabel.Color);
        Assert.Equal(Color.FromARGB(153, 235, 235, 245), CupertinoColors.SecondaryLabel.DarkColor);

        Assert.Equal(Color.FromARGB(73, 60, 60, 67), CupertinoColors.Separator.Color);
        Assert.Equal(Color.FromARGB(153, 84, 84, 88), CupertinoColors.Separator.DarkColor);
        Assert.Equal(Color.FromARGB(153, 210, 210, 210), CupertinoColors.Separator.DarkElevatedColor);

        Assert.Equal(Color.FromARGB(255, 255, 255, 255), CupertinoColors.SystemBackground.Color);
        Assert.Equal(Color.FromARGB(255, 0, 0, 0), CupertinoColors.SystemBackground.DarkColor);
        Assert.Equal(Color.FromARGB(255, 28, 28, 30), CupertinoColors.SystemBackground.DarkElevatedColor);

        Assert.Equal(Color.FromARGB(255, 142, 142, 147), CupertinoColors.SystemGrey.Color);
        Assert.Equal(Color.FromARGB(255, 242, 242, 247), CupertinoColors.SystemGrey6.Color);
        Assert.Equal(Color.FromARGB(255, 28, 28, 30), CupertinoColors.SystemGrey6.DarkColor);
    }

    [Fact]
    public void Palette_AliasesPointAtTheSystemColors()
    {
        Assert.Equal(CupertinoColors.SystemBlue, CupertinoColors.ActiveBlue);
        Assert.Equal(CupertinoColors.SystemGreen, CupertinoColors.ActiveGreen);
        Assert.Equal(CupertinoColors.SystemOrange, CupertinoColors.ActiveOrange);
        Assert.Equal(CupertinoColors.SystemRed, CupertinoColors.DestructiveRed);
    }

    private static Color Resolve(Color color, Func<Widget, Widget> wrap)
    {
        Color resolved = null!;
        using var harness = new CupertinoThemeTestHarness(wrap(new Builder(context =>
        {
            resolved = CupertinoDynamicColor.Resolve(color, context);
            return new SizedBox();
        })));

        return resolved;
    }
}
