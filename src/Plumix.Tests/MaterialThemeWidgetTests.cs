using Plumix.Cupertino;
using Plumix.Widgets;
using Xunit;
using Brightness = Plumix.Material.Brightness;
using InheritedMaterialTheme = Plumix.Material.InheritedMaterialTheme;
using ColorScheme = Plumix.Material.ColorScheme;
using Theme = Plumix.Material.Theme;
using ThemeData = Plumix.Material.ThemeData;

namespace Plumix.Tests;

// Dart parity source: material_ui/lib/src/theme.dart
// Mirrors the "Theme.brightnessOf" and "Theme.maybeBrightnessOf" groups of
// material_ui/test/theme_test.dart, plus the `Theme`/`_InheritedTheme` split those groups depend on.
public sealed class MaterialThemeWidgetTests
{
    [Fact]
    public void Theme_IsAStatelessWidgetThatInstallsAnInheritedMaterialTheme()
    {
        // Dart's `Theme extends StatelessWidget` and builds `_InheritedTheme`; the inherited half is
        // what `Theme.of` depends on, so the widget itself must not be an `InheritedWidget`.
        var data = new ThemeData(colorScheme: ColorScheme.Light());
        var theme = new Theme(data, new SizedBox());

        Assert.IsAssignableFrom<StatelessWidget>(theme);
        Assert.IsNotAssignableFrom<InheritedWidget>(theme);

        using var harness = new CupertinoThemeTestHarness(theme);

        InheritedMaterialTheme inherited = Assert.Single(harness.FindWidgets<InheritedMaterialTheme>());
        Assert.Same(theme, inherited.Theme);
        Assert.IsAssignableFrom<InheritedTheme>(inherited);
    }

    [Fact]
    public void InheritedMaterialTheme_WrapReinstallsTheThemeAroundANewChild()
    {
        // Dart's `_InheritedTheme.wrap` returns `Theme(data: theme.data, child: child)`, which is
        // what `InheritedTheme.captureAll` replays into a new subtree.
        var data = new ThemeData(colorScheme: ColorScheme.Light());
        var child = new SizedBox();
        var inherited = new InheritedMaterialTheme(new Theme(data, new SizedBox()), new SizedBox());

        var wrapped = Assert.IsType<Theme>(inherited.Wrap(null!, child));
        Assert.Same(data, wrapped.Data);
        Assert.Same(child, wrapped.Child);
    }

    [Fact]
    public void Theme_BrightnessOf_ReadsTheMediaQueryWhenNoThemeIsPresent()
    {
        Brightness? seen = null;
        using var harness = new CupertinoThemeTestHarness(new MediaQuery(
            new MediaQueryData(PlatformBrightness: PlatformBrightness.Dark),
            new Builder(context =>
            {
                seen = Theme.BrightnessOf(context);
                return new SizedBox();
            })));

        Assert.Equal(Brightness.Dark, seen);
    }

    [Fact]
    public void Theme_BrightnessOf_PrefersTheAncestorThemeOverTheMediaQuery()
    {
        Brightness? seen = null;
        using var harness = new CupertinoThemeTestHarness(new MediaQuery(
            new MediaQueryData(PlatformBrightness: PlatformBrightness.Dark),
            new Theme(
                new ThemeData(colorScheme: ColorScheme.Light()),
                new Builder(context =>
                {
                    seen = Theme.BrightnessOf(context);
                    return new SizedBox();
                }))));

        Assert.Equal(Brightness.Light, seen);
    }

    [Fact]
    public void Theme_BrightnessOf_FallsBackToLightWithoutAThemeOrMediaQuery()
    {
        Brightness? seen = null;
        using var harness = new CupertinoThemeTestHarness(new Builder(context =>
        {
            seen = Theme.BrightnessOf(context);
            return new SizedBox();
        }));

        Assert.Equal(Brightness.Light, seen);
    }

    [Fact]
    public void Theme_MaybeBrightnessOf_ReadsTheMediaQueryWhenNoThemeIsPresent()
    {
        Brightness? seen = null;
        using var harness = new CupertinoThemeTestHarness(new MediaQuery(
            new MediaQueryData(PlatformBrightness: PlatformBrightness.Dark),
            new Builder(context =>
            {
                seen = Theme.MaybeBrightnessOf(context);
                return new SizedBox();
            })));

        Assert.Equal(Brightness.Dark, seen);
    }

    [Fact]
    public void Theme_MaybeBrightnessOf_PrefersTheAncestorThemeOverTheMediaQuery()
    {
        Brightness? seen = null;
        using var harness = new CupertinoThemeTestHarness(new MediaQuery(
            new MediaQueryData(PlatformBrightness: PlatformBrightness.Dark),
            new Theme(
                new ThemeData(colorScheme: ColorScheme.Light()),
                new Builder(context =>
                {
                    seen = Theme.MaybeBrightnessOf(context);
                    return new SizedBox();
                }))));

        Assert.Equal(Brightness.Light, seen);
    }

    [Fact]
    public void Theme_MaybeBrightnessOf_IsNullWithoutAThemeOrMediaQuery()
    {
        Brightness? seen = Brightness.Dark;
        using var harness = new CupertinoThemeTestHarness(new Builder(context =>
        {
            seen = Theme.MaybeBrightnessOf(context);
            return new SizedBox();
        }));

        Assert.Null(seen);
    }
}
