using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Material;
using Plumix.Widgets;
using Xunit;
using ColorScheme = Plumix.Material.ColorScheme;
using CupertinoColors = Plumix.Cupertino.CupertinoColors;
using IconTheme = Plumix.Widgets.IconTheme;
using Theme = Plumix.Material.Theme;
using ThemeData = Plumix.Material.ThemeData;

namespace Plumix.Tests;

// Dart parity sources:
// material_ui/lib/src/theme.dart
// material_ui/lib/src/theme_data.dart
// Mirrors the "Cupertino theme" group of material_ui/test/theme_test.dart and
// material_ui/test/cupertino/cupertino_app_material_theme_test.dart.
public sealed class MaterialCupertinoThemeBridgeTests
{
    private int _buildCount;
    private CupertinoThemeData? _actualTheme;
    private IconThemeData? _actualIconTheme;
    private BuildContext? _context;
    private readonly Widget _singletonThemeSubtree;

    public MaterialCupertinoThemeBridgeTests()
    {
        // One widget instance reused across pumps, so a rebuild can only come from an inherited
        // dependency — the C# equivalent of Flutter's `const` subtree.
        _singletonThemeSubtree = new Builder(context =>
        {
            _buildCount++;
            _actualTheme = CupertinoTheme.Of(context);
            _actualIconTheme = IconTheme.Of(context);
            _context = context;
            return new SizedBox();
        });
    }

    [Fact]
    public void Material3_DefaultLightTheme_HasDefaults()
    {
        using var harness = TestTheme(new ThemeData(), out CupertinoThemeData theme);

        Assert.Equal(PlatformBrightness.Light, theme.Brightness);
        Assert.Equal(Color.FromUInt32(0xFF6750A4), theme.PrimaryColor.Value);
        Assert.Equal(Color.FromUInt32(0xFFFEF7FF), theme.ScaffoldBackgroundColor.Value);
        Assert.Equal(Colors.White, theme.PrimaryContrastingColor.Value);
        Assert.Equal(17.0, theme.TextTheme.TextStyle.FontSize);
    }

    [Fact]
    public void Material3_DarkTheme_HasDefaults()
    {
        using var harness = TestTheme(ThemeData.Dark, out CupertinoThemeData theme);

        Assert.Equal(PlatformBrightness.Dark, theme.Brightness);
        Assert.Equal(Color.FromUInt32(0xFFD0BCFF), theme.PrimaryColor.Value);
        Assert.Equal(Color.FromUInt32(0xFF381E72), theme.PrimaryContrastingColor.Value);
        Assert.Equal(Color.FromUInt32(0xFF141218), theme.ScaffoldBackgroundColor.Value);
        Assert.Equal(17.0, theme.TextTheme.TextStyle.FontSize);
    }

    [Fact]
    public void Material2_DefaultLightTheme_HasDefaults()
    {
        using var harness = TestTheme(
            new ThemeData(useMaterial3: false),
            out CupertinoThemeData theme);

        Assert.Equal(PlatformBrightness.Light, theme.Brightness);
        Assert.Equal(Colors.White, theme.PrimaryContrastingColor.Value);
        Assert.Equal(17.0, theme.TextTheme.TextStyle.FontSize);
    }

    [Fact]
    public void MaterialTheme_OverridesTheBrightness()
    {
        using (TestTheme(ThemeData.Dark, out _))
        {
            Assert.Equal(PlatformBrightness.Dark, CupertinoTheme.BrightnessOf(_context!));
        }

        using (TestTheme(new ThemeData(), out _))
        {
            Assert.Equal(PlatformBrightness.Light, CupertinoTheme.BrightnessOf(_context!));
        }

        // Overridable by cupertinoOverrideTheme.
        using (TestTheme(
                   new ThemeData(
                       brightness: Brightness.Light,
                       cupertinoOverrideTheme: new CupertinoThemeData(
                           brightness: PlatformBrightness.Dark)),
                   out _))
        {
            Assert.Equal(PlatformBrightness.Dark, CupertinoTheme.BrightnessOf(_context!));
        }

        using (TestTheme(
                   new ThemeData(
                       brightness: Brightness.Dark,
                       cupertinoOverrideTheme: new CupertinoThemeData(
                           brightness: PlatformBrightness.Light)),
                   out _))
        {
            Assert.Equal(PlatformBrightness.Light, CupertinoTheme.BrightnessOf(_context!));
        }
    }

    [Fact]
    public void CanOverrideMaterialTheme()
    {
        using var harness = TestTheme(
            new ThemeData(
                cupertinoOverrideTheme: new CupertinoThemeData(
                    scaffoldBackgroundColor: CupertinoColors.LightBackgroundGray)),
            out CupertinoThemeData theme);

        Assert.Equal(PlatformBrightness.Light, theme.Brightness);
        // We took the scaffold background override but the rest are still cascaded to the theme.
        Assert.Equal(Color.FromUInt32(0xFF6750A4), theme.PrimaryColor.Value);
        Assert.Equal(Colors.White, theme.PrimaryContrastingColor.Value);
        Assert.Equal(CupertinoColors.LightBackgroundGray, theme.ScaffoldBackgroundColor.Value);
        Assert.Equal(17.0, theme.TextTheme.TextStyle.FontSize);
    }

    [Fact]
    public void CanOverrideProperties_ThatAreIndependentOfMaterial()
    {
        using var harness = TestTheme(
            new ThemeData(
                // The bar colors ignore all things material except brightness.
                cupertinoOverrideTheme: new CupertinoThemeData(
                    barBackgroundColor: CupertinoColors.Black)),
            out CupertinoThemeData theme);

        Assert.Equal(Color.FromUInt32(0xFF6750A4), theme.PrimaryColor.Value);
        // MaterialBasedCupertinoThemeData should also function like a normal CupertinoThemeData.
        Assert.Equal(CupertinoColors.Black, theme.BarBackgroundColor.Value);
    }

    [Fact]
    public void ChangingMaterialTheme_TriggersRebuilds()
    {
        using var harness = TestTheme(
            new ThemeData(colorScheme: ColorScheme.Light(primary: Colors.Red)),
            out CupertinoThemeData theme);

        Assert.Equal(1, _buildCount);
        Assert.Equal(Colors.Red, theme.PrimaryColor.Value);

        harness.PumpWidget(new Theme(
            new ThemeData(colorScheme: ColorScheme.Light(primary: Colors.Orange)),
            _singletonThemeSubtree));

        Assert.Equal(2, _buildCount);
        Assert.Equal(Colors.Orange, _actualTheme!.PrimaryColor.Value);
    }

    [Fact]
    public void CupertinoThemeData_DoesNotOverride_TheMaterialThemesIconTheme()
    {
        Color materialIconColor = Colors.Blue;
        Color cupertinoIconColor = Colors.Black;

        using var harness = TestTheme(
            new ThemeData(
                iconTheme: new IconThemeData(Color: materialIconColor),
                cupertinoOverrideTheme: new CupertinoThemeData(primaryColor: cupertinoIconColor)),
            out _);

        Assert.Equal(1, _buildCount);
        Assert.Equal(materialIconColor, _actualIconTheme!.Color);
    }

    [Fact]
    public void ChangingCupertinoThemeOverride_TriggersRebuilds()
    {
        using var harness = TestTheme(
            new ThemeData(
                primarySwatch: MaterialColors.Purple,
                cupertinoOverrideTheme: new CupertinoThemeData(
                    primaryColor: CupertinoColors.ActiveOrange)),
            out CupertinoThemeData theme);

        Assert.Equal(1, _buildCount);
        Assert.Equal(CupertinoColors.ActiveOrange.Color, theme.PrimaryColor.Value);

        harness.PumpWidget(new Theme(
            new ThemeData(
                primarySwatch: MaterialColors.Purple,
                cupertinoOverrideTheme: new CupertinoThemeData(
                    primaryColor: CupertinoColors.ActiveGreen)),
            _singletonThemeSubtree));

        Assert.Equal(2, _buildCount);
        Assert.Equal(CupertinoColors.ActiveGreen.Color, _actualTheme!.PrimaryColor.Value);
    }

    [Fact]
    public void CupertinoThemeOverride_BlocksDerivativeChanges()
    {
        using var harness = TestTheme(
            new ThemeData(
                primarySwatch: MaterialColors.Purple,
                cupertinoOverrideTheme: new CupertinoThemeData(
                    primaryColor: CupertinoColors.ActiveOrange)),
            out CupertinoThemeData theme);

        Assert.Equal(1, _buildCount);
        Assert.Equal(CupertinoColors.ActiveOrange.Color, theme.PrimaryColor.Value);

        // Change the upstream material primary color; the override still preempts it.
        harness.PumpWidget(new Theme(
            new ThemeData(
                primarySwatch: MaterialColors.Blue,
                cupertinoOverrideTheme: new CupertinoThemeData(
                    primaryColor: CupertinoColors.SystemRed)),
            _singletonThemeSubtree));

        Assert.Equal(2, _buildCount);
        Assert.Equal(CupertinoColors.SystemRed.Color, _actualTheme!.PrimaryColor.Value);
    }

    [Fact]
    public void CupertinoOverrides_DoNotBlockDerivatives_ThatAreNotOverridden()
    {
        using var harness = TestTheme(
            new ThemeData(
                colorScheme: ColorScheme.Light(primary: Colors.Purple),
                cupertinoOverrideTheme: new CupertinoThemeData(
                    primaryContrastingColor: CupertinoColors.DestructiveRed)),
            out CupertinoThemeData theme);

        Assert.Equal(1, _buildCount);
        Assert.Equal(Colors.Purple, theme.TextTheme.ActionTextStyle.Color);
        Assert.Equal(CupertinoColors.DestructiveRed.Color, theme.PrimaryContrastingColor.Value);

        harness.PumpWidget(new Theme(
            new ThemeData(
                colorScheme: ColorScheme.Light(primary: Colors.Green),
                cupertinoOverrideTheme: new CupertinoThemeData(
                    primaryContrastingColor: CupertinoColors.DestructiveRed)),
            _singletonThemeSubtree));

        Assert.Equal(2, _buildCount);
        Assert.Equal(Colors.Green, _actualTheme!.TextTheme.ActionTextStyle.Color);
        Assert.Equal(
            CupertinoColors.DestructiveRed.Color,
            _actualTheme.PrimaryContrastingColor.Value);
    }

    [Fact]
    public void CopyWith_OnlyCopiesTheOverrides_NotTheMaterialOrCupertinoDerivatives()
    {
        using var original = TestTheme(
            new ThemeData(
                colorScheme: ColorScheme.Light(primary: Colors.Purple),
                cupertinoOverrideTheme: new CupertinoThemeData(
                    primaryContrastingColor: CupertinoColors.ActiveOrange)),
            out CupertinoThemeData originalTheme);
        CupertinoThemeData copiedTheme = originalTheme.CopyWith(
            barBackgroundColor: CupertinoColors.DestructiveRed);

        using var harness = TestTheme(
            new ThemeData(
                colorScheme: ColorScheme.Light(primary: Colors.Blue),
                cupertinoOverrideTheme: copiedTheme),
            out CupertinoThemeData theme);

        Assert.Equal(Colors.Blue, theme.PrimaryColor.Value);
        Assert.Equal(CupertinoColors.ActiveOrange.Color, theme.PrimaryContrastingColor.Value);
        Assert.Equal(CupertinoColors.DestructiveRed.Color, theme.BarBackgroundColor.Value);
    }

    [Fact]
    public void MaterialThemes_WithNoCupertinoOverrides_CanAlsoBeCopyWithed()
    {
        using var original = TestTheme(
            new ThemeData(colorScheme: ColorScheme.Light(primary: Colors.Purple)),
            out CupertinoThemeData originalTheme);
        CupertinoThemeData copiedTheme = originalTheme.CopyWith(
            primaryContrastingColor: CupertinoColors.DestructiveRed);

        using var harness = TestTheme(
            new ThemeData(
                colorScheme: ColorScheme.Light(primary: Colors.Blue),
                cupertinoOverrideTheme: copiedTheme),
            out CupertinoThemeData theme);

        Assert.Equal(Colors.Blue, theme.PrimaryColor.Value);
        Assert.Equal(CupertinoColors.DestructiveRed.Color, theme.PrimaryContrastingColor.Value);
    }

    [Fact]
    public void MaterialBasedCupertinoThemeData_DefersSelectionHandleColor_ToTheMaterialTheme()
    {
        Color handleColor = Color.FromUInt32(0xFF00FF00);
        using var harness = TestTheme(
            new ThemeData(
                textSelectionTheme: new TextSelectionThemeData(SelectionHandleColor: handleColor)),
            out CupertinoThemeData theme);

        Assert.Equal(handleColor, theme.SelectionHandleColor.Value);
    }

    [Fact]
    public void ThemeData_CupertinoOverrideTheme_IsStoredWithoutDefaults()
    {
        var data = new ThemeData(
            cupertinoOverrideTheme: new CupertinoThemeData(
                primaryColor: CupertinoColors.ActiveGreen));

        Assert.NotNull(data.CupertinoOverrideTheme);
        Assert.IsNotType<CupertinoThemeData>(data.CupertinoOverrideTheme);
        Assert.Equal(CupertinoColors.ActiveGreen, data.CupertinoOverrideTheme!.PrimaryColor);
        Assert.Null(data.CupertinoOverrideTheme.BarBackgroundColor);
        Assert.Null(new ThemeData().CupertinoOverrideTheme);
    }

    [Fact]
    public void ThemeData_Lerp_SnapsCupertinoOverrideTheme_AtTheHalfway()
    {
        var a = new ThemeData(
            cupertinoOverrideTheme: new CupertinoThemeData(
                primaryColor: CupertinoColors.ActiveOrange));
        var b = new ThemeData(
            cupertinoOverrideTheme: new CupertinoThemeData(
                primaryColor: CupertinoColors.ActiveGreen));

        Assert.Equal(
            CupertinoColors.ActiveOrange,
            ThemeData.Lerp(a, b, 0.4).CupertinoOverrideTheme!.PrimaryColor);
        Assert.Equal(
            CupertinoColors.ActiveGreen,
            ThemeData.Lerp(a, b, 0.6).CupertinoOverrideTheme!.PrimaryColor);
    }

    [Fact]
    public void CupertinoTheme_CreatesAMaterialTheme_WithColorsBasedOffTheCupertinoTheme()
    {
        ThemeData? appliedTheme = null;
        using var harness = new CupertinoThemeTestHarness(new CupertinoTheme(
            new CupertinoThemeData(primaryColor: CupertinoColors.ActiveGreen),
            new Builder(context =>
            {
                appliedTheme = Theme.Of(context);
                return new SizedBox();
            })));

        Assert.Equal(CupertinoColors.ActiveGreen.Color, appliedTheme!.ColorScheme.Primary);
    }

    [Fact]
    public void Theme_WithoutAnyAncestor_FallsBackToTheLightTheme()
    {
        ThemeData? appliedTheme = null;
        using var harness = new CupertinoThemeTestHarness(new Builder(context =>
        {
            appliedTheme = Theme.Of(context);
            return new SizedBox();
        }));

        Assert.Equal(ThemeData.Light.ColorScheme.Primary, appliedTheme!.ColorScheme.Primary);
        Assert.Equal(Brightness.Light, appliedTheme.Brightness);
    }

    private CupertinoThemeTestHarness TestTheme(ThemeData data, out CupertinoThemeData theme)
    {
        var harness = new CupertinoThemeTestHarness(new Theme(data, _singletonThemeSubtree));
        theme = _actualTheme!;
        return harness;
    }
}
