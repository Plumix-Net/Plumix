using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Mirrors cupertino_ui/test/icon_theme_data_test.dart and the icon-theme assertions in theme_test.dart.
public sealed class CupertinoIconThemeDataTests
{
    [Fact]
    public void IconThemeOf_ReturnsConcreteDataUnchanged()
    {
        var data = new IconThemeData(
            Color: Color.FromUInt32(0xAAAAAAAA),
            Size: 16.0,
            Opacity: 0.5,
            Fill: 0.0,
            Weight: 400.0,
            Grade: 0.0,
            OpticalSize: 48.0,
            ApplyTextScaling: true);
        IconThemeData? retrieved = null;

        using var harness = new CupertinoThemeTestHarness(new IconTheme(
            data,
            new Builder(context =>
            {
                retrieved = IconTheme.Of(context);
                return new SizedBox();
            })));

        Assert.Same(data, retrieved);
    }

    [Fact]
    public void IconThemeOf_ResolvesDynamicColorAgainstConsumerContext()
    {
        IconThemeData? retrieved = null;
        using var harness = new CupertinoThemeTestHarness(new IconTheme(
            new CupertinoIconThemeData(Color: CupertinoColors.SystemBlue),
            new MediaQuery(
                new MediaQueryData(PlatformBrightness: PlatformBrightness.Dark),
                new Builder(context =>
                {
                    retrieved = IconTheme.Of(context);
                    return new SizedBox();
                }))));

        Assert.IsType<CupertinoIconThemeData>(retrieved);
        Assert.True(retrieved!.IsConcrete);
        Assert.Equal(CupertinoColors.SystemBlue.DarkColor, retrieved.Color);
    }

    [Fact]
    public void CupertinoTheme_ResolvesPrimaryColorAtNestedConsumerOverrides()
    {
        Color baseColor = Color.FromUInt32(0xFF102030);
        Color elevatedColor = Color.FromUInt32(0xFF405060);
        Color highContrastElevatedColor = Color.FromUInt32(0xFF708090);
        var dynamicColor = new CupertinoDynamicColor(
            color: baseColor,
            darkColor: baseColor,
            highContrastColor: baseColor,
            darkHighContrastColor: baseColor,
            elevatedColor: elevatedColor,
            darkElevatedColor: elevatedColor,
            highContrastElevatedColor: highContrastElevatedColor,
            darkHighContrastElevatedColor: elevatedColor);
        IconThemeData? retrieved = null;

        using var harness = new CupertinoThemeTestHarness(new CupertinoTheme(
            new CupertinoThemeData(primaryColor: dynamicColor),
            new MediaQuery(
                new MediaQueryData(HighContrast: true),
                new CupertinoUserInterfaceLevel(
                    CupertinoUserInterfaceLevelData.Elevated,
                    new Builder(context =>
                    {
                        retrieved = IconTheme.Of(context);
                        return new SizedBox();
                    })))));

        Assert.Equal(highContrastElevatedColor, retrieved!.Color);
    }

    [Fact]
    public void CopyWith_PreservesSubtypeAndDynamicColorUntilResolution()
    {
        CupertinoIconThemeData copied = new CupertinoIconThemeData(
            Color: CupertinoColors.SystemGreen,
            Size: 16.0).CopyWith(weight: 500.0);
        IconThemeData? retrieved = null;

        using var harness = new CupertinoThemeTestHarness(new IconTheme(
            copied,
            new MediaQuery(
                new MediaQueryData(PlatformBrightness: PlatformBrightness.Dark),
                new Builder(context =>
                {
                    retrieved = IconTheme.Of(context);
                    return new SizedBox();
                }))));

        Assert.IsType<CupertinoIconThemeData>(retrieved);
        Assert.Equal(16.0, retrieved!.Size);
        Assert.Equal(500.0, retrieved.Weight);
        Assert.Equal(CupertinoColors.SystemGreen.DarkColor, retrieved.Color);
    }

    [Fact]
    public void EqualityAndDiagnosticsIncludeTheDynamicColor()
    {
        var first = new CupertinoIconThemeData(Color: CupertinoColors.SystemRed);
        var same = new CupertinoIconThemeData(Color: CupertinoColors.SystemRed);
        var different = new CupertinoIconThemeData(Color: CupertinoColors.SystemGreen);

        Assert.Equal(first, same);
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(first, different);
        Assert.NotEqual<IconThemeData>(first, new IconThemeData(Color: CupertinoColors.SystemRed.Color));

        var diagnostics = new DiagnosticPropertiesBuilder();
        first.DebugFillProperties(diagnostics);
        Assert.Equal(2, diagnostics.Properties.Count(property => property.Name == "color"));
    }
}
