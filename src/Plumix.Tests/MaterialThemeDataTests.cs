using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// material_ui/lib/src/theme_data.dart

public sealed class MaterialThemeDataTests
{
    private static readonly TargetPlatform[] MobilePlatforms =
        [TargetPlatform.Android, TargetPlatform.IOS, TargetPlatform.Fuchsia];

    private static readonly TargetPlatform[] DesktopPlatforms =
        [TargetPlatform.Linux, TargetPlatform.MacOS, TargetPlatform.Windows];

    // ---- VisualDensity ------------------------------------------------------------------

    [Fact]
    public void VisualDensity_ConstantsMatchDart()
    {
        Assert.Equal(-4.0, VisualDensity.MinimumDensity);
        Assert.Equal(4.0, VisualDensity.MaximumDensity);
        Assert.Equal(0.0, VisualDensity.Standard.Horizontal);
        Assert.Equal(0.0, VisualDensity.Standard.Vertical);
        Assert.Equal(-1.0, VisualDensity.Comfortable.Horizontal);
        Assert.Equal(-1.0, VisualDensity.Comfortable.Vertical);
        Assert.Equal(-2.0, VisualDensity.Compact.Horizontal);
        Assert.Equal(-2.0, VisualDensity.Compact.Vertical);
    }

    [Fact]
    public void VisualDensity_DefaultDensityForPlatform_IsStandardOnMobileAndCompactOnDesktop()
    {
        foreach (TargetPlatform platform in MobilePlatforms)
        {
            Assert.Equal(VisualDensity.Standard, VisualDensity.DefaultDensityForPlatform(platform));
        }

        foreach (TargetPlatform platform in DesktopPlatforms)
        {
            Assert.Equal(VisualDensity.Compact, VisualDensity.DefaultDensityForPlatform(platform));
        }
    }

    [Fact]
    public void VisualDensity_AdaptivePlatformDensity_FollowsTheAmbientPlatform()
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        try
        {
            PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
            Assert.Equal(VisualDensity.Compact, VisualDensity.AdaptivePlatformDensity);
            PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
            Assert.Equal(VisualDensity.Standard, VisualDensity.AdaptivePlatformDensity);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Theory]
    [InlineData(4.1)]
    [InlineData(-4.1)]
    public void VisualDensity_RejectsDensitiesOutsideTheLegalRange(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualDensity(horizontal: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualDensity(vertical: value));
    }

    [Fact]
    public void VisualDensity_BaseSizeAdjustment_IsFourLogicalPixelsPerUnit()
    {
        Vector adjustment = new VisualDensity(horizontal: 3.0, vertical: -2.0).BaseSizeAdjustment;
        Assert.Equal(12.0, adjustment.X);
        Assert.Equal(-8.0, adjustment.Y);
    }

    [Fact]
    public void VisualDensity_EffectiveConstraints_ClampMinimumsAndLeaveMaximaAlone()
    {
        var tight = BoxConstraints.TightFor(width: 35, height: 35);
        BoxConstraints expanded = new VisualDensity(horizontal: 4.0, vertical: 4.0)
            .EffectiveConstraints(tight);
        Assert.Equal(35.0, expanded.MinWidth);
        Assert.Equal(35.0, expanded.MinHeight);
        Assert.Equal(35.0, expanded.MaxWidth);
        Assert.Equal(35.0, expanded.MaxHeight);

        BoxConstraints contracted = new VisualDensity(horizontal: -4.0, vertical: -4.0)
            .EffectiveConstraints(tight);
        Assert.Equal(19.0, contracted.MinWidth);
        Assert.Equal(19.0, contracted.MinHeight);
        Assert.Equal(35.0, contracted.MaxWidth);
        Assert.Equal(35.0, contracted.MaxHeight);

        var small = BoxConstraints.TightFor(width: 4, height: 4);
        BoxConstraints floored = new VisualDensity(horizontal: -4.0, vertical: -4.0)
            .EffectiveConstraints(small);
        Assert.Equal(0.0, floored.MinWidth);
        Assert.Equal(0.0, floored.MinHeight);
        Assert.Equal(4.0, floored.MaxWidth);
        Assert.Equal(4.0, floored.MaxHeight);
    }

    [Fact]
    public void VisualDensity_EffectiveConstraints_ExpandAndContractUnboundedConstraints()
    {
        var unbounded = new BoxConstraints(
            MinWidth: 0.0,
            MaxWidth: double.PositiveInfinity,
            MinHeight: 0.0,
            MaxHeight: double.PositiveInfinity);
        BoxConstraints expanded = new VisualDensity(horizontal: 4.0, vertical: 4.0)
            .EffectiveConstraints(unbounded);
        Assert.Equal(16.0, expanded.MinWidth);
        Assert.Equal(16.0, expanded.MinHeight);
        Assert.Equal(double.PositiveInfinity, expanded.MaxWidth);
        Assert.Equal(double.PositiveInfinity, expanded.MaxHeight);

        BoxConstraints contracted = new VisualDensity(horizontal: -4.0, vertical: -4.0)
            .EffectiveConstraints(unbounded);
        Assert.Equal(0.0, contracted.MinWidth);
        Assert.Equal(0.0, contracted.MinHeight);
        Assert.Equal(double.PositiveInfinity, contracted.MaxWidth);
    }

    [Fact]
    public void VisualDensity_Lerp_DoesNotClampAndShortCircuitsOnEqualEndpoints()
    {
        var a = new VisualDensity(horizontal: 1.0, vertical: 0.5);
        var b = new VisualDensity(horizontal: 2.0, vertical: 1.0);
        Assert.Equal(a, VisualDensity.Lerp(a, b, 0.0));
        Assert.Equal(new VisualDensity(horizontal: 1.25, vertical: 0.625), VisualDensity.Lerp(a, b, 0.25));
        Assert.Equal(b, VisualDensity.Lerp(a, b, 1.0));
        Assert.Equal(a, VisualDensity.Lerp(a, a, 0.5));

        // Dart's `lerp` does not clamp `t`, so extrapolation past the endpoints is visible.
        Assert.Equal(new VisualDensity(horizontal: 2.5, vertical: 1.25), VisualDensity.Lerp(a, b, 1.5));
    }

    [Fact]
    public void VisualDensity_CopyWith_ReplacesOnlyTheGivenAxis()
    {
        var density = new VisualDensity(horizontal: 1.0, vertical: 2.0);
        Assert.Equal(new VisualDensity(horizontal: -1.0, vertical: 2.0), density.CopyWith(horizontal: -1.0));
        Assert.Equal(new VisualDensity(horizontal: 1.0, vertical: -2.0), density.CopyWith(vertical: -2.0));
        Assert.Equal(density, density.CopyWith());
    }

    [Fact]
    public void VisualDensity_ToString_UsesDartsShortForm()
    {
        Assert.Equal("VisualDensity(h: 0.0, v: 0.0)", VisualDensity.Standard.ToString());
        Assert.Equal("VisualDensity(h: -1.0, v: -1.0)", VisualDensity.Comfortable.ToString());
    }

    // ---- Platform-derived ThemeData defaults --------------------------------------------

    [Fact]
    public void ThemeData_MaterialTapTargetSize_IsPaddedOnMobileAndShrinkWrapOnDesktop()
    {
        foreach (TargetPlatform platform in MobilePlatforms)
        {
            Assert.Equal(
                MaterialTapTargetSize.Padded,
                new ThemeData(platform: platform).MaterialTapTargetSize);
        }

        foreach (TargetPlatform platform in DesktopPlatforms)
        {
            Assert.Equal(
                MaterialTapTargetSize.ShrinkWrap,
                new ThemeData(platform: platform).MaterialTapTargetSize);
        }
    }

    [Fact]
    public void ThemeData_VisualDensity_FollowsTheThemesPlatformNotTheHost()
    {
        foreach (TargetPlatform platform in MobilePlatforms)
        {
            Assert.Equal(VisualDensity.Standard, new ThemeData(platform: platform).VisualDensity);
        }

        foreach (TargetPlatform platform in DesktopPlatforms)
        {
            Assert.Equal(VisualDensity.Compact, new ThemeData(platform: platform).VisualDensity);
        }
    }

    // ---- Diagnostics ------------------------------------------------------------------

    [Fact]
    public void ThemeData_DiagnosticsIncludeAllPropertiesInDartOrder()
    {
        string[] expectedNames =
        [
            "adaptations",
            "applyElevationOverlayColor",
            "cupertinoOverrideTheme",
            "extensions",
            "inputDecorationTheme",
            "materialTapTargetSize",
            "pageTransitionsTheme",
            "platform",
            "scrollbarTheme",
            "splashFactory",
            "useMaterial3",
            "visualDensity",
            "canvasColor",
            "cardColor",
            "colorScheme",
            "disabledColor",
            "dividerColor",
            "focusColor",
            "highlightColor",
            "hintColor",
            "hoverColor",
            "primaryColorDark",
            "primaryColorLight",
            "primaryColor",
            "scaffoldBackgroundColor",
            "secondaryHeaderColor",
            "shadowColor",
            "splashColor",
            "unselectedWidgetColor",
            "iconTheme",
            "primaryIconTheme",
            "primaryTextTheme",
            "textTheme",
            "typography",
            "actionIconTheme",
            "appBarTheme",
            "badgeTheme",
            "bannerTheme",
            "bottomAppBarTheme",
            "bottomNavigationBarTheme",
            "bottomSheetTheme",
            "buttonTheme",
            "cardTheme",
            "carouselViewTheme",
            "checkboxTheme",
            "chipTheme",
            "dataTableTheme",
            "datePickerTheme",
            "dialogTheme",
            "dividerTheme",
            "drawerTheme",
            "dropdownMenuTheme",
            "elevatedButtonTheme",
            "expansionTileTheme",
            "filledButtonTheme",
            "floatingActionButtonTheme",
            "iconButtonTheme",
            "listTileTheme",
            "menuBarTheme",
            "menuButtonTheme",
            "menuTheme",
            "navigationBarTheme",
            "navigationDrawerTheme",
            "navigationRailTheme",
            "outlinedButtonTheme",
            "popupMenuTheme",
            "progressIndicatorTheme",
            "radioTheme",
            "searchBarTheme",
            "searchViewTheme",
            "segmentedButtonTheme",
            "sliderTheme",
            "snackBarTheme",
            "switchTheme",
            "tabBarTheme",
            "textButtonTheme",
            "textSelectionTheme",
            "timePickerTheme",
            "toggleButtonsTheme",
            "tooltipTheme",
            "buttonBarTheme",
            "dialogBackgroundColor",
            "indicatorColor",
        ];
        var diagnostics = new DiagnosticPropertiesBuilder();

        new ThemeData().DebugFillProperties(diagnostics);

        Assert.Equal(expectedNames, diagnostics.Properties.Select(property => property.Name));
        Assert.Equal(expectedNames.Length, expectedNames.Distinct().Count());
        Assert.IsType<IterableProperty<Adaptation>>(diagnostics.Properties[0]);
        Assert.IsType<EnumProperty<TargetPlatform>>(diagnostics.Properties[7]);
        Assert.IsType<ColorProperty>(diagnostics.Properties[12]);
        Assert.IsType<DiagnosticsProperty<ColorScheme>>(diagnostics.Properties[14]);
    }

    [Fact]
    public void ThemeData_DiagnosticsUseFallbackDefaultsAndDebugLevel()
    {
        var fallbackDiagnostics = new DiagnosticPropertiesBuilder();
        new ThemeData().DebugFillProperties(fallbackDiagnostics);

        DiagnosticsNode cupertinoOverride = Assert.Single(
            fallbackDiagnostics.Properties,
            property => property.Name == "cupertinoOverrideTheme");
        DiagnosticsNode useMaterial3 = Assert.Single(
            fallbackDiagnostics.Properties,
            property => property.Name == "useMaterial3");
        DiagnosticsNode applyElevationOverlay = Assert.Single(
            fallbackDiagnostics.Properties,
            property => property.Name == "applyElevationOverlayColor");
        Assert.Equal(DiagnosticLevel.Fine, cupertinoOverride.Level);
        Assert.Equal(DiagnosticLevel.Fine, useMaterial3.Level);
        Assert.Equal(DiagnosticLevel.Debug, applyElevationOverlay.Level);

        var changedDiagnostics = new DiagnosticPropertiesBuilder();
        new ThemeData(useMaterial3: false).DebugFillProperties(changedDiagnostics);
        DiagnosticsNode changedUseMaterial3 = Assert.Single(
            changedDiagnostics.Properties,
            property => property.Name == "useMaterial3");
        Assert.Equal(DiagnosticLevel.Debug, changedUseMaterial3.Level);
    }

    [Fact]
    public void ThemeData_ToStringIsCompactAndSingleLine()
    {
        ThemeData light = ThemeData.From(ColorScheme.Light());
        ThemeData dark = ThemeData.From(ColorScheme.Dark());

        Assert.True(light.ToString().Length < 200);
        Assert.True(dark.ToString().Length < 200);
        Assert.DoesNotContain('\n', dark.ToString());
    }

    // ---- estimateBrightnessForColor -----------------------------------------------------

    [Fact]
    public void EstimateBrightnessForColor_MatchesDartsTable()
    {
        Assert.Equal(Brightness.Light, ThemeData.EstimateBrightnessForColor(Colors.White));
        Assert.Equal(Brightness.Light, ThemeData.EstimateBrightnessForColor(MaterialColors.Yellow));
        Assert.Equal(Brightness.Light, ThemeData.EstimateBrightnessForColor(MaterialColors.Orange));
        Assert.Equal(Brightness.Light, ThemeData.EstimateBrightnessForColor(MaterialColors.Lime));
        Assert.Equal(Brightness.Light, ThemeData.EstimateBrightnessForColor(MaterialColors.Grey));
        Assert.Equal(Brightness.Dark, ThemeData.EstimateBrightnessForColor(Colors.Black));
        Assert.Equal(Brightness.Dark, ThemeData.EstimateBrightnessForColor(MaterialColors.Blue));
        Assert.Equal(Brightness.Dark, ThemeData.EstimateBrightnessForColor(MaterialColors.DeepOrange));
        Assert.Equal(Brightness.Dark, ThemeData.EstimateBrightnessForColor(MaterialColors.Teal));
        Assert.Equal(Brightness.Dark, ThemeData.EstimateBrightnessForColor(MaterialColors.Indigo));
    }

    // ---- Constructor guards -------------------------------------------------------------

    [Fact]
    public void ThemeData_ColorSchemeSeed_IsExclusiveWithSchemeSwatchAndPrimaryColor()
    {
        Assert.Throws<ArgumentException>(() => new ThemeData(
            colorSchemeSeed: MaterialColors.Blue,
            colorScheme: ColorScheme.Light()));
        Assert.Throws<ArgumentException>(() => new ThemeData(
            colorSchemeSeed: MaterialColors.Blue,
            primaryColor: MaterialColors.Green));
        Assert.Throws<ArgumentException>(() => new ThemeData(
            colorSchemeSeed: MaterialColors.Blue,
            primarySwatch: MaterialColors.Green));
    }

    [Fact]
    public void ThemeData_BrightnessMustMatchColorSchemeBrightness()
    {
        Assert.Throws<ArgumentException>(() => new ThemeData(
            colorScheme: ColorScheme.Light(),
            brightness: Brightness.Dark));
        Assert.Throws<ArgumentException>(() => new ThemeData(
            colorScheme: ColorScheme.Dark(),
            brightness: Brightness.Light));
    }

    // ---- Material 3 defaults ------------------------------------------------------------

    [Fact]
    public void ThemeData_DefaultM3Light_DerivesItsColorsFromTheScheme()
    {
        var theme = new ThemeData();
        Assert.True(theme.UseMaterial3);
        Assert.Equal(Brightness.Light, theme.Brightness);
        Assert.Equal(theme.ColorScheme.Primary, theme.PrimaryColor);
        Assert.Equal(theme.ColorScheme.Surface, theme.CanvasColor);
        Assert.Equal(theme.ColorScheme.Surface, theme.ScaffoldBackgroundColor);
        Assert.Equal(theme.ColorScheme.Surface, theme.CardColor);
        Assert.Equal(theme.ColorScheme.Outline, theme.DividerColor);
        Assert.Equal(theme.ColorScheme.Surface, theme.DialogBackgroundColor);
        Assert.Equal(theme.ColorScheme.OnPrimary, theme.IndicatorColor);
        Assert.False(theme.ApplyElevationOverlayColor);
    }

    [Fact]
    public void ThemeData_DefaultM3Dark_UsesSurfaceForPrimaryColorAndTurnsOnTheElevationOverlay()
    {
        var theme = new ThemeData(brightness: Brightness.Dark);
        Assert.Equal(Brightness.Dark, theme.Brightness);
        Assert.Equal(theme.ColorScheme.Surface, theme.PrimaryColor);
        Assert.Equal(theme.ColorScheme.Surface, theme.CanvasColor);
        Assert.Equal(theme.ColorScheme.Outline, theme.DividerColor);
        Assert.Equal(theme.ColorScheme.OnSurface, theme.IndicatorColor);
        Assert.True(theme.ApplyElevationOverlayColor);
    }

    [Fact]
    public void ThemeData_SecondaryHeaderColor_FallsBackToTheSwatch()
    {
        Assert.Equal(
            MaterialColors.Blue.Shade50,
            new ThemeData(useMaterial3: false).SecondaryHeaderColor);
        Assert.Equal(
            MaterialColors.Grey.Shade700,
            new ThemeData(useMaterial3: false, brightness: Brightness.Dark).SecondaryHeaderColor);
        Assert.Equal(
            MaterialColors.Green.Shade50,
            new ThemeData(useMaterial3: false, primarySwatch: MaterialColors.Green).SecondaryHeaderColor);
    }

    [Fact]
    public void ThemeData_ButtonTheme_TakesItsDefaultsFromTheResolvedTheme()
    {
        var light = new ThemeData(useMaterial3: false);
        Assert.Equal(MaterialColors.Grey.Shade300, light.ButtonTheme.ButtonColor);
        Assert.Equal(light.MaterialTapTargetSize, light.ButtonTheme.MaterialTapTargetSize);

        var dark = new ThemeData(useMaterial3: false, brightness: Brightness.Dark);
        Assert.Equal(MaterialColors.Blue.Shade600, dark.ButtonTheme.ButtonColor);
    }

    // ---- ThemeData.From / Fallback ------------------------------------------------------

    [Fact]
    public void ThemeDataFrom_LightScheme_SetsSurfaceBackedColorsAndLeavesTheOverlayOff()
    {
        ColorScheme scheme = ColorScheme.Light();
        ThemeData theme = ThemeData.From(scheme);
        Assert.Equal(Brightness.Light, theme.Brightness);
        Assert.Equal(scheme.Primary, theme.PrimaryColor);
        Assert.Equal(scheme.Surface, theme.CanvasColor);
        Assert.Equal(scheme.Surface, theme.ScaffoldBackgroundColor);
        Assert.Equal(scheme.Surface, theme.CardColor);
        Assert.Equal(scheme.Surface, theme.DialogBackgroundColor);
        Assert.False(theme.ApplyElevationOverlayColor);
    }

    [Fact]
    public void ThemeDataFrom_DarkScheme_UsesSurfaceForPrimaryColorAndTurnsOnTheOverlay()
    {
        ColorScheme scheme = ColorScheme.Dark();
        ThemeData theme = ThemeData.From(scheme);
        Assert.Equal(Brightness.Dark, theme.Brightness);
        Assert.Equal(scheme.Surface, theme.PrimaryColor);
        Assert.Equal(scheme.Surface, theme.CardColor);
        Assert.True(theme.ApplyElevationOverlayColor);
    }

    [Fact]
    public void ThemeDataFallback_IsTheLightTheme()
    {
        Assert.Equal(Brightness.Light, ThemeData.Fallback.Brightness);
        Assert.True(ThemeData.Fallback.UseMaterial3);
    }

    // ---- Brightness is derived, not stored ----------------------------------------------

    [Fact]
    public void ThemeData_Brightness_ReadsThroughTheColorScheme()
    {
        var theme = new ThemeData(brightness: Brightness.Dark);
        Assert.Equal(theme.ColorScheme.Brightness, theme.Brightness);
    }

    [Fact]
    public void CopyWith_Brightness_RewritesOnlyTheSchemeAndLeavesDerivedColorsAlone()
    {
        ColorScheme lightScheme = ColorScheme.Light();
        ThemeData theme = ThemeData.From(lightScheme).CopyWith(brightness: Brightness.Dark);
        Assert.Equal(Brightness.Dark, theme.Brightness);
        Assert.Equal(Brightness.Dark, theme.ColorScheme.Brightness);
        Assert.Equal(lightScheme.Primary, theme.PrimaryColor);
        Assert.Equal(lightScheme.Surface, theme.CanvasColor);
        Assert.Equal(lightScheme.Surface, theme.ScaffoldBackgroundColor);
        Assert.False(theme.ApplyElevationOverlayColor);
    }

    // ---- Equality ------------------------------------------------------------------------

    [Fact]
    public void ThemeData_EqualsAndHashCode_TreatAnUnchangedCopyAsEqual()
    {
        var theme = new ThemeData();
        ThemeData copy = theme with { };
        Assert.Equal(theme, copy);
        Assert.Equal(theme.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void ThemeData_EqualsAndHashCode_IncludeFocusAndHoverColor()
    {
        var black = new ThemeData(focusColor: Colors.Black);
        var white = new ThemeData(focusColor: Colors.White);
        Assert.NotEqual(black, white);
        Assert.NotEqual(black.GetHashCode(), white.GetHashCode());

        var hoverBlack = new ThemeData(hoverColor: Colors.Black);
        var hoverWhite = new ThemeData(hoverColor: Colors.White);
        Assert.NotEqual(hoverBlack, hoverWhite);
        Assert.NotEqual(hoverBlack.GetHashCode(), hoverWhite.GetHashCode());
    }

    [Fact]
    public void ThemeData_ExplicitlyDefaultComponentThemeEqualsTheImplicitOne()
    {
        // Dart stores the resolved component theme, so passing `const AppBarThemeData()` is
        // indistinguishable from letting it default.
        Assert.Equal(new ThemeData(), new ThemeData(appBarTheme: new AppBarThemeData()));
    }

    // ---- Localize -------------------------------------------------------------------------

    [Fact]
    public void Localize_MergesTheGeometryOverTheThemeAndCachesOnIdentity()
    {
        var theme = new ThemeData();
        TextTheme geometry = Typography.Material2021().Tall;

        ThemeData first = ThemeData.Localize(theme, geometry);
        ThemeData second = ThemeData.Localize(theme, geometry);
        Assert.Same(first, second);

        // A structurally equal but distinct base theme is a different cache key.
        ThemeData third = ThemeData.Localize(new ThemeData(), geometry);
        Assert.NotSame(first, third);
    }

    [Fact]
    public void Localize_EvictsInInsertionOrderPastFiveEntries()
    {
        TextTheme geometry = Typography.Material2021().Tall;
        var first = new ThemeData();
        ThemeData localizedFirst = ThemeData.Localize(first, geometry);

        // Five further distinct base themes push the first one out of the FIFO cache.
        for (int i = 0; i < 5; i++)
        {
            ThemeData.Localize(new ThemeData(), geometry);
        }

        Assert.NotSame(localizedFirst, ThemeData.Localize(first, geometry));
    }

    // ---- Theme extensions -----------------------------------------------------------------

    [Fact]
    public void Extensions_AreLookedUpByTypeAndReturnNullWhenAbsent()
    {
        var theme = new ThemeData(extensions: [new ProbeExtension(Colors.Black)]);
        Assert.Equal(Colors.Black, theme.Extension<ProbeExtension>()!.Value);
        Assert.Null(new ThemeData().Extension<ProbeExtension>());
    }

    [Fact]
    public void Lerp_KeepsUnpairedExtensionsAndInterpolatesPairedOnes()
    {
        var a = new ThemeData(extensions: [new ProbeExtension(Colors.Black)]);
        var b = new ThemeData(extensions: [new ProbeExtension(Colors.White)]);
        Assert.Equal(
            Color.FromArgb(0xFF, 0x7F, 0x7F, 0x7F),
            ThemeData.Lerp(a, b, 0.5).Extension<ProbeExtension>()!.Value);

        // Present only on `a`: kept unlerped. Present only on `b`: carried over unlerped.
        Assert.Equal(
            Colors.Black,
            ThemeData.Lerp(a, new ThemeData(), 0.5).Extension<ProbeExtension>()!.Value);
        Assert.Equal(
            Colors.White,
            ThemeData.Lerp(new ThemeData(), b, 0.5).Extension<ProbeExtension>()!.Value);
    }

    // ---- Lerp -----------------------------------------------------------------------------

    [Fact]
    public void Lerp_TakesBrightnessFromTheNearerEndpointAndInterpolatesColors()
    {
        ThemeData dark = ThemeData.Dark;
        ThemeData light = ThemeData.Light;
        ThemeData quarter = ThemeData.Lerp(dark, light, 0.25);
        Assert.Equal(Brightness.Dark, quarter.Brightness);
        Assert.Equal(
            ColorScheme.Lerp(dark.ColorScheme, light.ColorScheme, 0.25).Primary,
            quarter.ColorScheme.Primary);
    }

    [Fact]
    public void Lerp_CoversTheNewlyPortedColors()
    {
        var a = new ThemeData(useMaterial3: false, primarySwatch: MaterialColors.Blue);
        var b = new ThemeData(useMaterial3: false, primarySwatch: MaterialColors.Green);
        ThemeData mid = ThemeData.Lerp(a, b, 0.5);
        Assert.NotEqual(a.SecondaryHeaderColor, mid.SecondaryHeaderColor);
        Assert.NotEqual(b.SecondaryHeaderColor, mid.SecondaryHeaderColor);
        Assert.Equal(a.DialogBackgroundColor, mid.DialogBackgroundColor);
    }

    [Fact]
    public void Lerp_ShortCircuitsOnIdenticalThemes()
    {
        var theme = new ThemeData();
        Assert.Same(theme, ThemeData.Lerp(theme, theme, 0.5));
    }

    private sealed class ProbeExtension(Color value) : ThemeExtension<ProbeExtension>
    {
        public Color Value { get; } = value;

        public override ProbeExtension Lerp(ProbeExtension? other, double t)
        {
            return other is null ? this : new ProbeExtension(new ColorTween().Evaluate(t, Value, other.Value));
        }
    }
}
