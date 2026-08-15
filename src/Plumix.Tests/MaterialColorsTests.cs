using Avalonia.Media;
using Plumix.Material;
using Plumix.Painting;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Mirrors `material_ui/test/colors_test.dart`, the `ColorSwatch`/`MaterialColor` sections of
/// `flutter/test/painting/colors_test.dart`, and the `fromSwatch`/`primarySwatch` derivation
/// asserted by `material_ui/test/theme_data_test.dart`.
/// </summary>
public sealed class MaterialColorsTests
{
    private static readonly int[] PrimaryKeys = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900];
    private static readonly int[] AccentKeys = [100, 200, 400, 700];

    [Fact]
    public void MaterialColor_BasicFunctionality()
    {
        var color = new MaterialColor(
            500,
            new Dictionary<int, Color>
            {
                [50] = Color.FromUInt32(50),
                [100] = Color.FromUInt32(100),
                [200] = Color.FromUInt32(200),
                [300] = Color.FromUInt32(300),
                [400] = Color.FromUInt32(400),
                [500] = Color.FromUInt32(500),
                [600] = Color.FromUInt32(600),
                [700] = Color.FromUInt32(700),
                [800] = Color.FromUInt32(800),
                [900] = Color.FromUInt32(900),
            });

        Assert.Equal(500u, color.Value);
        foreach (int key in PrimaryKeys)
        {
            Assert.Equal(Color.FromUInt32((uint)key), color[key]!.Value);
        }

        Assert.Equal(color[50]!.Value, color.Shade50);
        Assert.Equal(color[100]!.Value, color.Shade100);
        Assert.Equal(color[200]!.Value, color.Shade200);
        Assert.Equal(color[300]!.Value, color.Shade300);
        Assert.Equal(color[400]!.Value, color.Shade400);
        Assert.Equal(color[500]!.Value, color.Shade500);
        Assert.Equal(color[600]!.Value, color.Shade600);
        Assert.Equal(color[700]!.Value, color.Shade700);
        Assert.Equal(color[800]!.Value, color.Shade800);
        Assert.Equal(color[900]!.Value, color.Shade900);
    }

    [Fact]
    public void MaterialAccentColor_ExposesItsFourShades()
    {
        MaterialAccentColor accent = MaterialColors.RedAccent;

        Assert.Equal(accent[100]!.Value, accent.Shade100);
        Assert.Equal(accent[200]!.Value, accent.Shade200);
        Assert.Equal(accent[400]!.Value, accent.Shade400);
        Assert.Equal(accent[700]!.Value, accent.Shade700);
        Assert.Null(accent[500]);
    }

    [Fact]
    public void ColorSwatches_DoNotContainDuplicates()
    {
        foreach (MaterialColor swatch in MaterialColors.Primaries.Append(MaterialColors.Grey))
        {
            Assert.Equal(
                PrimaryKeys.Length,
                PrimaryKeys.Select(key => swatch[key]!.Value).Distinct().Count());
        }

        foreach (MaterialAccentColor swatch in MaterialColors.Accents)
        {
            Assert.Equal(
                AccentKeys.Length,
                AccentKeys.Select(key => swatch[key]!.Value).Distinct().Count());
        }
    }

    [Fact]
    public void ColorSwatchColors_AreOpaqueAndEqualTheirPrimaryColor()
    {
        foreach (MaterialColor swatch in MaterialColors.Primaries.Append(MaterialColors.Grey))
        {
            Assert.Equal(swatch.Primary, swatch.Shade500);
            foreach (int key in PrimaryKeys)
            {
                Assert.Equal(0xFF, swatch[key]!.Value.A);
            }
        }

        foreach (MaterialAccentColor swatch in MaterialColors.Accents)
        {
            Assert.Equal(swatch.Primary, swatch.Shade200);
            foreach (int key in AccentKeys)
            {
                Assert.Equal(0xFF, swatch[key]!.Value.A);
            }
        }
    }

    [Fact]
    public void Palette_MatchesTheDartShapeAndConstants()
    {
        Assert.Equal(18, MaterialColors.Primaries.Count);
        Assert.Equal(16, MaterialColors.Accents.Count);
        Assert.DoesNotContain(MaterialColors.Grey, MaterialColors.Primaries);
        Assert.Contains(MaterialColors.Brown, MaterialColors.Primaries);
        Assert.Contains(MaterialColors.BlueGrey, MaterialColors.Primaries);

        // Dart's `Colors.transparent` is 0x00000000, not Avalonia's 0x00FFFFFF.
        Assert.Equal(0x00000000u, MaterialColors.Transparent.ToUInt32());
        Assert.Equal(0xFF000000u, MaterialColors.Black.ToUInt32());
        Assert.Equal(0xDD000000u, MaterialColors.Black87.ToUInt32());
        Assert.Equal(0x8A000000u, MaterialColors.Black54.ToUInt32());
        Assert.Equal(0x61000000u, MaterialColors.Black38.ToUInt32());
        Assert.Equal(0xFFFFFFFFu, MaterialColors.White.ToUInt32());
        Assert.Equal(0xB3FFFFFFu, MaterialColors.White70.ToUInt32());
        Assert.Equal(0x99FFFFFFu, MaterialColors.White60.ToUInt32());
        Assert.Equal(0x62FFFFFFu, MaterialColors.White38.ToUInt32());

        Assert.Equal(0xFF2196F3u, MaterialColors.Blue.Value);
        Assert.Equal(0xFFBBDEFBu, MaterialColors.Blue.Shade100.ToUInt32());
        Assert.Equal(0xFF1976D2u, MaterialColors.Blue.Shade700.ToUInt32());
        Assert.Equal(0xFFD32F2Fu, MaterialColors.Red.Shade700.ToUInt32());
        Assert.Equal(0xFF64FFDAu, MaterialColors.TealAccent.Shade200.ToUInt32());

        // `grey` is the only swatch carrying the two extra Material 2 shades.
        Assert.Equal(0xFFD6D6D6u, MaterialColors.Grey[350]!.Value.ToUInt32());
        Assert.Equal(0xFF303030u, MaterialColors.Grey[850]!.Value.ToUInt32());
        Assert.Null(MaterialColors.Blue[350]);
    }

    [Fact]
    public void ColorSwatch_EqualityIsStructuralOverTheSwatchTable()
    {
        var swatch = new Dictionary<string, Color>
        {
            ["2259 C"] = Color.FromUInt32(0xFF027223),
            ["2273 C"] = Color.FromUInt32(0xFF257226),
        };
        var first = new ColorSwatch<string>(0xFF027223, swatch);
        var second = new ColorSwatch<string>(
            0xFF027223,
            new Dictionary<string, Color>
            {
                ["2259 C"] = Color.FromUInt32(0xFF027223),
                ["2273 C"] = Color.FromUInt32(0xFF257226),
            });

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal(Color.FromUInt32(0xFF027223), first["2259 C"]!.Value);
        Assert.Null(first["missing"]);
        Assert.Equal(swatch.Keys, first.Keys);

        // A different primary, a different table, or a different runtime type all break equality.
        Assert.NotEqual(first, new ColorSwatch<string>(0xFF257226, swatch));
        Assert.NotEqual(
            first,
            new ColorSwatch<string>(0xFF027223, new Dictionary<string, Color> { ["2259 C"] = Colors.Black }));
        Assert.NotEqual<ColorSwatch<int>>(
            MaterialColors.Blue,
            new ColorSwatch<int>(MaterialColors.Blue.Value, new Dictionary<int, Color>()));
    }

    [Fact]
    public void ColorSwatch_LerpInterpolatesEveryShadeAndClamps()
    {
        var a = new ColorSwatch<int>(
            0x00000000,
            new Dictionary<int, Color> { [1] = Color.FromUInt32(0x00000000) });
        var b = new ColorSwatch<int>(
            0xFFFFFFFF,
            new Dictionary<int, Color> { [1] = Color.FromUInt32(0xFFFFFFFF) });

        Assert.Equal(0x00000000u, ColorSwatch<int>.Lerp(a, b, 0.0)!.Value);
        Assert.Equal(0x7F7F7F7Fu, ColorSwatch<int>.Lerp(a, b, 0.5)!.Value);
        Assert.Equal(0x7F7F7F7Fu, ColorSwatch<int>.Lerp(a, b, 0.5)![1]!.Value.ToUInt32());
        Assert.Equal(0xFFFFFFFFu, ColorSwatch<int>.Lerp(a, b, 1.0)!.Value);
        Assert.Equal(0x00000000u, ColorSwatch<int>.Lerp(a, b, -0.1)!.Value);
        Assert.Equal(0xFFFFFFFFu, ColorSwatch<int>.Lerp(a, b, 1.1)!.Value);

        // A null endpoint fades the other one out; identical endpoints short-circuit.
        Assert.Null(ColorSwatch<int>.Lerp(null, null, 0.0));
        Assert.Same(a, ColorSwatch<int>.Lerp(a, a, 0.5));
        Assert.Equal(0x7FFFFFFFu, ColorSwatch<int>.Lerp(null, b, 0.5)!.Value);
        Assert.Equal(0x7FFFFFFFu, ColorSwatch<int>.Lerp(b, null, 0.5)!.Value);
    }

    [Fact]
    public void ColorScheme_FromSwatchLightDefaults()
    {
        ColorScheme scheme = ColorScheme.FromSwatch();

        Assert.Equal(Brightness.Light, scheme.Brightness);
        Assert.Equal(MaterialColors.Blue.Primary, scheme.Primary);
        Assert.Equal(MaterialColors.Blue.Primary, scheme.Secondary);
        Assert.Equal(MaterialColors.White, scheme.Surface);
        Assert.Equal(MaterialColors.Red.Shade700, scheme.Error);
        Assert.Equal(MaterialColors.White, scheme.OnPrimary);
        Assert.Equal(MaterialColors.White, scheme.OnSecondary);
        Assert.Equal(MaterialColors.Black, scheme.OnSurface);
        Assert.Equal(MaterialColors.White, scheme.OnError);
        Assert.Equal(MaterialColors.Blue.Shade200, scheme.Background);
        Assert.Equal(MaterialColors.White, scheme.OnBackground);

        // Every role `fromSwatch` leaves unset resolves through the fallback chain.
        Assert.Equal(scheme.Primary, scheme.PrimaryContainer);
        Assert.Equal(scheme.OnPrimary, scheme.OnPrimaryContainer);
        Assert.Equal(scheme.Secondary, scheme.SecondaryContainer);
        Assert.Equal(scheme.Secondary, scheme.Tertiary);
        Assert.Equal(scheme.OnSecondary, scheme.OnTertiary);
        Assert.Equal(scheme.Error, scheme.ErrorContainer);
        Assert.Equal(scheme.Surface, scheme.SurfaceContainerHighest);
        Assert.Equal(scheme.OnSurface, scheme.OnSurfaceVariant);
        Assert.Equal(scheme.OnBackground, scheme.Outline);
        Assert.Equal(scheme.OnBackground, scheme.OutlineVariant);
        Assert.Equal(MaterialColors.Black, scheme.Shadow);
        Assert.Equal(MaterialColors.Black, scheme.Scrim);
        Assert.Equal(scheme.OnSurface, scheme.InverseSurface);
        Assert.Equal(scheme.Surface, scheme.OnInverseSurface);
        Assert.Equal(scheme.OnPrimary, scheme.InversePrimary);
        Assert.Equal(scheme.Primary, scheme.SurfaceTint);
    }

    [Fact]
    public void ColorScheme_FromSwatchDarkDefaults()
    {
        ColorScheme scheme = ColorScheme.FromSwatch(brightness: Brightness.Dark);

        Assert.Equal(Brightness.Dark, scheme.Brightness);
        Assert.Equal(MaterialColors.Blue.Primary, scheme.Primary);
        Assert.Equal(MaterialColors.TealAccent.Shade200, scheme.Secondary);
        Assert.Equal(MaterialColors.Grey.Shade800, scheme.Surface);
        Assert.Equal(MaterialColors.White, scheme.OnPrimary);
        // `tealAccent[200]` is a light color, so its `on` color is black.
        Assert.Equal(MaterialColors.Black, scheme.OnSecondary);
        Assert.Equal(MaterialColors.White, scheme.OnSurface);
        Assert.Equal(MaterialColors.Black, scheme.OnError);
        Assert.Equal(MaterialColors.Grey.Shade700, scheme.Background);
        // `onBackground` follows the *primary* brightness, not the background's.
        Assert.Equal(MaterialColors.White, scheme.OnBackground);
    }

    [Fact]
    public void ColorScheme_FromSwatchHonorsEveryOverride()
    {
        ColorScheme scheme = ColorScheme.FromSwatch(
            primarySwatch: MaterialColors.Yellow,
            accentColor: MaterialColors.Black,
            cardColor: MaterialColors.Lime.Shade100,
            backgroundColor: MaterialColors.Brown.Shade200,
            errorColor: MaterialColors.Purple.Shade400);

        Assert.Equal(MaterialColors.Yellow.Primary, scheme.Primary);
        Assert.Equal(MaterialColors.Black, scheme.Secondary);
        Assert.Equal(MaterialColors.Lime.Shade100, scheme.Surface);
        Assert.Equal(MaterialColors.Brown.Shade200, scheme.Background);
        Assert.Equal(MaterialColors.Purple.Shade400, scheme.Error);
        // `yellow` is a light color and the accent is black, so the `on` colors flip.
        Assert.Equal(MaterialColors.Black, scheme.OnPrimary);
        Assert.Equal(MaterialColors.White, scheme.OnSecondary);
        Assert.Equal(MaterialColors.Black, scheme.OnBackground);
    }

    [Fact]
    public void ThemeData_Material2LightDerivesTheSchemeFromTheDefaultSwatch()
    {
        var theme = new ThemeData(useMaterial3: false);

        Assert.Equal(MaterialColors.Blue.Primary, theme.ColorScheme.Primary);
        Assert.Equal(MaterialColors.Blue.Primary, theme.ColorScheme.Secondary);
        Assert.Equal(MaterialColors.White, theme.ColorScheme.Surface);
        Assert.Equal(MaterialColors.Red.Shade700, theme.ColorScheme.Error);
        Assert.Equal(MaterialColors.Blue.Shade200, theme.ColorScheme.Background);

        Assert.Equal(MaterialColors.Blue.Primary, theme.PrimaryColor);
        Assert.Equal(MaterialColors.Blue.Shade100, theme.PrimaryColorLight);
        Assert.Equal(MaterialColors.Blue.Shade700, theme.PrimaryColorDark);
        Assert.Equal(MaterialColors.Grey.Shade50, theme.CanvasColor);
        Assert.Equal(theme.CanvasColor, theme.ScaffoldBackgroundColor);
        Assert.Equal(MaterialColors.White, theme.CardColor);
        Assert.Equal(Color.FromUInt32(0x1F000000), theme.DividerColor);
        Assert.False(theme.ApplyElevationOverlayColor);
    }

    [Fact]
    public void ThemeData_Material2DarkDerivesTheSchemeFromTheDefaultSwatch()
    {
        var theme = new ThemeData(useMaterial3: false, brightness: Brightness.Dark);

        Assert.Equal(MaterialColors.Blue.Primary, theme.ColorScheme.Primary);
        Assert.Equal(MaterialColors.TealAccent.Shade200, theme.ColorScheme.Secondary);
        Assert.Equal(MaterialColors.Grey.Shade800, theme.ColorScheme.Surface);
        Assert.Equal(MaterialColors.Grey.Shade700, theme.ColorScheme.Background);

        Assert.Equal(MaterialColors.Grey.Shade900, theme.PrimaryColor);
        Assert.Equal(MaterialColors.Grey.Shade500, theme.PrimaryColorLight);
        Assert.Equal(MaterialColors.Black, theme.PrimaryColorDark);
        Assert.Equal(MaterialColors.Grey[850]!.Value, theme.CanvasColor);
        Assert.Equal(theme.CanvasColor, theme.ScaffoldBackgroundColor);
        Assert.Equal(MaterialColors.Grey.Shade800, theme.CardColor);
        Assert.Equal(Color.FromUInt32(0x1FFFFFFF), theme.DividerColor);
        Assert.False(theme.ApplyElevationOverlayColor);
    }

    [Fact]
    public void ThemeData_PrimarySwatchDrivesTheMaterial2Scheme()
    {
        var theme = new ThemeData(useMaterial3: false, primarySwatch: MaterialColors.Green);

        Assert.Equal(MaterialColors.Green.Primary, theme.ColorScheme.Primary);
        Assert.Equal(MaterialColors.Green.Primary, theme.ColorScheme.Secondary);
        Assert.Equal(MaterialColors.Green.Shade200, theme.ColorScheme.Background);
        Assert.Equal(MaterialColors.Green.Primary, theme.PrimaryColor);
        Assert.Equal(MaterialColors.Green.Shade100, theme.PrimaryColorLight);
        Assert.Equal(MaterialColors.Green.Shade700, theme.PrimaryColorDark);
    }

    [Fact]
    public void ThemeData_PrimarySwatchStillFeedsTheMaterial3PrimaryShades()
    {
        var theme = new ThemeData(primarySwatch: MaterialColors.Green);

        // The M3 scheme itself ignores the swatch...
        Assert.Equal(ColorScheme.Material3Light.Primary, theme.ColorScheme.Primary);
        Assert.Equal(theme.ColorScheme.Primary, theme.PrimaryColor);
        // ...but `primaryColorLight`/`primaryColorDark` are still swatch-derived.
        Assert.Equal(MaterialColors.Green.Shade100, theme.PrimaryColorLight);
        Assert.Equal(MaterialColors.Green.Shade700, theme.PrimaryColorDark);
    }

    [Fact]
    public void ThemeData_Material3DefaultsKeepTheBlueSwatchShades()
    {
        var theme = new ThemeData();

        Assert.Equal(MaterialColors.Blue.Shade100, theme.PrimaryColorLight);
        Assert.Equal(MaterialColors.Blue.Shade700, theme.PrimaryColorDark);
        Assert.Equal(theme.ColorScheme.Surface, theme.CanvasColor);
        Assert.Equal(theme.ColorScheme.Surface, theme.CardColor);
        Assert.Equal(theme.ColorScheme.Outline, theme.DividerColor);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ThemeData_OpacityRampColorsIgnoreTheSchemeInBothModes(bool useMaterial3)
    {
        var light = new ThemeData(useMaterial3: useMaterial3);
        var dark = new ThemeData(useMaterial3: useMaterial3, brightness: Brightness.Dark);

        Assert.Equal(MaterialColors.Black38, light.DisabledColor);
        Assert.Equal(MaterialColors.White38, dark.DisabledColor);
        Assert.Equal(MaterialColors.Black54, light.UnselectedWidgetColor);
        Assert.Equal(MaterialColors.White70, dark.UnselectedWidgetColor);
        Assert.Equal(Color.FromUInt32(0x99000000), light.HintColor);
        Assert.Equal(MaterialColors.White60, dark.HintColor);
        Assert.Equal(Color.FromUInt32(0x1F000000), light.FocusColor);
        Assert.Equal(Color.FromUInt32(0x1FFFFFFF), dark.FocusColor);
        Assert.Equal(Color.FromUInt32(0x0A000000), light.HoverColor);
        Assert.Equal(Color.FromUInt32(0x0AFFFFFF), dark.HoverColor);
        Assert.Equal(MaterialColors.Black, light.ShadowColor);
    }

    [Fact]
    public void ThemeData_ColorSchemeSeedRejectsPrimarySwatch()
    {
        Assert.Throws<ArgumentException>(() => new ThemeData(
            colorSchemeSeed: MaterialColors.Blue,
            primarySwatch: MaterialColors.Green));
    }

    [Fact]
    public void ThemeData_ApplyElevationOverlayColorFollowsTheBrightnessArgument()
    {
        Assert.False(new ThemeData().ApplyElevationOverlayColor);
        Assert.True(new ThemeData(brightness: Brightness.Dark).ApplyElevationOverlayColor);
        Assert.False(new ThemeData(useMaterial3: false, brightness: Brightness.Dark)
            .ApplyElevationOverlayColor);
        // Flutter reads the nullable `brightness` argument here, not the resolved brightness, so a
        // dark scheme without an explicit `brightness` leaves the overlay off.
        Assert.False(new ThemeData(colorScheme: ColorScheme.Material3Dark)
            .ApplyElevationOverlayColor);
    }

    [Theory]
    [InlineData("White", "Light")]
    [InlineData("Black", "Dark")]
    [InlineData("Blue", "Dark")]
    [InlineData("Yellow", "Light")]
    [InlineData("DeepOrange", "Dark")]
    [InlineData("Orange", "Light")]
    [InlineData("Lime", "Light")]
    [InlineData("Grey", "Light")]
    [InlineData("Teal", "Dark")]
    [InlineData("Indigo", "Dark")]
    public void EstimateBrightnessForColor_MatchesTheMaterialPalette(string name, string expected)
    {
        Color color = name switch
        {
            "White" => MaterialColors.White,
            "Black" => MaterialColors.Black,
            "Blue" => MaterialColors.Blue,
            "Yellow" => MaterialColors.Yellow,
            "DeepOrange" => MaterialColors.DeepOrange,
            "Orange" => MaterialColors.Orange,
            "Lime" => MaterialColors.Lime,
            "Grey" => MaterialColors.Grey,
            "Teal" => MaterialColors.Teal,
            _ => MaterialColors.Indigo,
        };

        Brightness expectedBrightness = expected == "Light" ? Brightness.Light : Brightness.Dark;
        Assert.Equal(expectedBrightness, ThemeData.EstimateBrightnessForColor(color));
    }
}
