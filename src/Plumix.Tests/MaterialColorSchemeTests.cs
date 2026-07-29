using Avalonia.Media;
using Plumix.Material;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class MaterialColorSchemeTests
{
    [Fact]
    public void ColorScheme_Constructor_UsesFlutterRoleFallbacks()
    {
        var scheme = ColorScheme.Light();

        Assert.Equal(scheme.Primary, scheme.PrimaryContainer);
        Assert.Equal(scheme.OnPrimary, scheme.OnPrimaryContainer);
        Assert.Equal(scheme.Secondary, scheme.Tertiary);
        Assert.Equal(scheme.OnSecondary, scheme.OnTertiary);
        Assert.Equal(scheme.Surface, scheme.SurfaceContainerHighest);
        Assert.Equal(scheme.OnSurface, scheme.OnSurfaceVariant);
        Assert.Equal(scheme.OnBackground, scheme.Outline);
        Assert.Equal(Colors.Black, scheme.Shadow);
        Assert.Equal(scheme.Primary, scheme.SurfaceTint);
    }

    [Fact]
    public void ColorScheme_FromSeed_MatchesFlutterTonalSpotLightBlue()
    {
        var scheme = ColorScheme.FromSeed(Color.Parse("#FF2196F3"));

        Assert.Equal(Color.Parse("#FF36618E"), scheme.Primary);
        Assert.Equal(Colors.White, scheme.OnPrimary);
        Assert.Equal(Color.Parse("#FFD1E4FF"), scheme.PrimaryContainer);
        Assert.Equal(Color.Parse("#FF194975"), scheme.OnPrimaryContainer);
        Assert.Equal(Color.Parse("#FF535F70"), scheme.Secondary);
        Assert.Equal(Color.Parse("#FF6B5778"), scheme.Tertiary);
        Assert.Equal(Color.Parse("#FFBA1A1A"), scheme.Error);
        Assert.Equal(Color.Parse("#FFF8F9FF"), scheme.Surface);
        Assert.Equal(Color.Parse("#FFECEEF4"), scheme.SurfaceContainer);
        Assert.Equal(Color.Parse("#FF191C20"), scheme.OnSurface);
        Assert.Equal(Color.Parse("#FF73777F"), scheme.Outline);
        Assert.Equal(Color.Parse("#FFA0CAFD"), scheme.InversePrimary);
        Assert.Equal(Brightness.Light, scheme.Brightness);
    }

    [Fact]
    public void ColorScheme_FromSeed_MatchesFlutterTonalSpotDarkBlue()
    {
        var scheme = ColorScheme.FromSeed(Color.Parse("#FF2196F3"), Brightness.Dark);

        Assert.Equal(Color.Parse("#FFA0CAFD"), scheme.Primary);
        Assert.Equal(Color.Parse("#FF003258"), scheme.OnPrimary);
        Assert.Equal(Color.Parse("#FF194975"), scheme.PrimaryContainer);
        Assert.Equal(Color.Parse("#FFD1E4FF"), scheme.OnPrimaryContainer);
        Assert.Equal(Color.Parse("#FFBBC7DB"), scheme.Secondary);
        Assert.Equal(Color.Parse("#FFD6BEE4"), scheme.Tertiary);
        Assert.Equal(Color.Parse("#FFFFB4AB"), scheme.Error);
        Assert.Equal(Color.Parse("#FF111418"), scheme.Surface);
        Assert.Equal(Color.Parse("#FF1D2024"), scheme.SurfaceContainer);
        Assert.Equal(Color.Parse("#FFE1E2E8"), scheme.OnSurface);
        Assert.Equal(Color.Parse("#FF8D9199"), scheme.Outline);
        Assert.Equal(Brightness.Dark, scheme.Brightness);
    }

    [Fact]
    public void ColorScheme_FromSeed_SupportsEveryFlutterVariant()
    {
        DynamicSchemeVariant[] variants = Enum.GetValues<DynamicSchemeVariant>();
        var primaries = new HashSet<Color>();

        foreach (DynamicSchemeVariant variant in variants)
        {
            var scheme = ColorScheme.FromSeed(
                seedColor: Color.Parse("#FF6559F5"),
                dynamicSchemeVariant: variant);
            primaries.Add(scheme.Primary);
        }

        Assert.True(primaries.Count >= 8);
        Assert.Equal(
            Color.Parse("#FF4C3CDB"),
            ColorScheme.FromSeed(
                seedColor: Color.Parse("#FF6559F5"),
                dynamicSchemeVariant: DynamicSchemeVariant.Fidelity).Primary);
    }

    [Fact]
    public void ColorScheme_FromSeed_ExplicitRolesOverrideGeneratedValues()
    {
        var scheme = ColorScheme.FromSeed(
            seedColor: Colors.Blue,
            primary: Colors.Red,
            tertiaryContainer: Colors.Green,
            surfaceContainerHighest: Colors.Yellow,
            scrim: Colors.White);

        Assert.Equal(Colors.Red, scheme.Primary);
        Assert.Equal(Colors.Green, scheme.TertiaryContainer);
        Assert.Equal(Colors.Yellow, scheme.SurfaceContainerHighest);
        Assert.Equal(Colors.White, scheme.Scrim);
    }

    [Theory]
    [InlineData(-1.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ColorScheme_FromSeed_RejectsInvalidContrast(double contrastLevel)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ColorScheme.FromSeed(Colors.Blue, contrastLevel: contrastLevel));
    }

    [Fact]
    public void ColorScheme_CopyWith_AndLerp_CoverExtendedRoles()
    {
        var begin = ColorScheme.Material3Light;
        var end = begin.CopyWith(
            brightness: Brightness.Dark,
            primaryFixed: Colors.Black,
            tertiaryContainer: Colors.Red,
            surfaceContainerHighest: Colors.Blue,
            scrim: Colors.White);

        var midpoint = ColorScheme.Lerp(begin, end, 0.5);

        Assert.Equal(Brightness.Dark, midpoint.Brightness);
        Assert.NotEqual(begin.PrimaryFixed, midpoint.PrimaryFixed);
        Assert.NotEqual(begin.TertiaryContainer, midpoint.TertiaryContainer);
        Assert.NotEqual(begin.SurfaceContainerHighest, midpoint.SurfaceContainerHighest);
        Assert.NotEqual(begin.Scrim, midpoint.Scrim);
        Assert.Equal(end, ColorScheme.Lerp(begin, end, 1.0));
    }

    [Fact]
    public void ThemeData_ColorScheme_DrivesLegacyColorSurface()
    {
        var scheme = ColorScheme.FromSeed(Color.Parse("#FF006495"), Brightness.Dark);
        var theme = new ThemeData(colorScheme: scheme);

        Assert.Equal(scheme, theme.ColorScheme);
        Assert.Equal(scheme.Brightness, theme.Brightness);
        Assert.Equal(scheme.Surface, theme.PrimaryColor);
        Assert.Equal(scheme.Secondary, theme.SecondaryColor);
        Assert.Equal(scheme.PrimaryContainer, theme.PrimaryContainerColor);
        Assert.Equal(scheme.OnPrimaryContainer, theme.OnPrimaryContainerColor);
        Assert.Equal(scheme.SurfaceContainer, theme.SurfaceContainerColor);
        Assert.Equal(scheme.OnSurface, theme.OnSurfaceColor);
        Assert.Equal(scheme.Outline, theme.DividerColor);
        Assert.Equal(scheme.Error, theme.ErrorColor);
    }

    [Fact]
    public void ThemeData_ColorSchemeSeed_GeneratesScheme_AndGuardsConflicts()
    {
        Color seed = Color.Parse("#FF006495");
        var theme = new ThemeData(colorSchemeSeed: seed, brightness: Brightness.Dark);

        Assert.Equal(ColorScheme.FromSeed(seed, Brightness.Dark), theme.ColorScheme);
        Assert.Throws<ArgumentException>(
            () => new ThemeData(colorSchemeSeed: seed, colorScheme: ColorScheme.Light()));
        Assert.Throws<ArgumentException>(
            () => new ThemeData(colorSchemeSeed: seed, primaryColor: Colors.Red));
        Assert.Throws<ArgumentException>(
            () => new ThemeData(brightness: Brightness.Dark, colorScheme: ColorScheme.Light()));
    }

    [Fact]
    public void TextTheme_ExposesCompleteMaterial2021TypeScale()
    {
        TextTheme theme = MaterialTextTheme.Fallback;

        Assert.Equal(57, theme.DisplayLarge.FontSize);
        Assert.Equal(45, theme.DisplayMedium.FontSize);
        Assert.Equal(36, theme.DisplaySmall.FontSize);
        Assert.Equal(32, theme.HeadlineLarge.FontSize);
        Assert.Equal(28, theme.HeadlineMedium.FontSize);
        Assert.Equal(24, theme.HeadlineSmall.FontSize);
        Assert.Equal(22, theme.TitleLarge.FontSize);
        Assert.Equal(16, theme.TitleMedium.FontSize);
        Assert.Equal(14, theme.TitleSmall.FontSize);
        Assert.Equal(16, theme.BodyLarge.FontSize);
        Assert.Equal(14, theme.BodyMedium.FontSize);
        Assert.Equal(12, theme.BodySmall.FontSize);
        Assert.Equal(14, theme.LabelLarge.FontSize);
        Assert.Equal(12, theme.LabelMedium.FontSize);
        Assert.Equal(11, theme.LabelSmall.FontSize);
    }

    [Fact]
    public void TextTheme_CopyMergeApplyAndLerp_FollowFlutterComposition()
    {
        TextTheme begin = MaterialTextTheme.Fallback;
        TextTheme merged = begin.Merge(new TextTheme(
            titleLarge: new TextStyle(Color: Colors.Red),
            bodyMedium: new TextStyle(FontWeight: FontWeight.Bold)));
        TextTheme applied = merged.Apply(
            fontSizeFactor: 2.0,
            letterSpacingDelta: 1.0,
            displayColor: Colors.Blue,
            bodyColor: Colors.Green);
        TextTheme midpoint = TextTheme.Lerp(begin, applied, 0.5);

        Assert.Equal(22, merged.TitleLarge.FontSize);
        Assert.Equal(Colors.Red, merged.TitleLarge.Color);
        Assert.Equal(FontWeight.Bold, merged.BodyMedium.FontWeight);
        Assert.Equal(44, applied.TitleLarge.FontSize);
        Assert.Equal(Colors.Green, applied.TitleLarge.Color);
        Assert.Equal(Colors.Blue, applied.DisplayLarge.Color);
        Assert.Equal(33, midpoint.TitleLarge.FontSize);
    }

    [Fact]
    public void Typography_Material2021_UsesSchemeColorsAndPlatformFont()
    {
        var scheme = ColorScheme.Material3Dark;
        Typography typography = Typography.Material2021(
            platform: TargetPlatform.Windows,
            colorScheme: scheme);

        Assert.Equal(scheme.Surface, typography.Black.BodyMedium.Color);
        Assert.Equal(scheme.OnSurface, typography.White.BodyMedium.Color);
        Assert.Equal(new FontFamily("Segoe UI"), typography.EnglishLike.BodyMedium.FontFamily);
        Assert.Same(
            typography.Dense,
            typography.GeometryThemeFor(ScriptCategory.Dense));
    }

    [Fact]
    public void ThemeData_Lerp_InterpolatesColorSchemeAndTypography()
    {
        var begin = new ThemeData(colorScheme: ColorScheme.Material3Light);
        var end = new ThemeData(colorScheme: ColorScheme.Material3Dark);

        ThemeData midpoint = ThemeData.Lerp(begin, end, 0.5);

        Assert.Equal(
            ColorScheme.Lerp(begin.ColorScheme, end.ColorScheme, 0.5),
            midpoint.ColorScheme);
        Assert.Equal(
            Typography.Lerp(begin.Typography, end.Typography, 0.5),
            midpoint.Typography);
    }
}
