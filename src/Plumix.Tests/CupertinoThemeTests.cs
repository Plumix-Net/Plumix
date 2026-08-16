using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Mirrors cupertino_ui/test/theme_test.dart.
public sealed class CupertinoThemeTests
{
    private int _buildCount;
    private CupertinoThemeData? _actualTheme;
    private IconThemeData? _actualIconTheme;
    private readonly Widget _singletonThemeSubtree;

    public CupertinoThemeTests()
    {
        // One widget instance reused across pumps, so a rebuild can only come from an inherited
        // dependency — the C# equivalent of Flutter's `const` subtree.
        _singletonThemeSubtree = new Builder(context =>
        {
            _buildCount++;
            _actualTheme = CupertinoTheme.Of(context);
            _actualIconTheme = IconTheme.Of(context);
            return new SizedBox();
        });
    }

    [Fact]
    public void DefaultTheme_HasDefaults()
    {
        using var harness = TestTheme(new CupertinoThemeData(), out CupertinoThemeData theme);

        Assert.Null(theme.Brightness);
        Assert.Equal(CupertinoColors.ActiveBlue.Color, theme.PrimaryColor.Value);
        Assert.Equal(17.0, theme.TextTheme.TextStyle.FontSize);
        Assert.False(theme.ApplyThemeToAll);
    }

    [Fact]
    public void ThemeAttributes_CascadeIntoTheTextTheme()
    {
        using var harness = TestTheme(
            new CupertinoThemeData(primaryColor: CupertinoColors.SystemRed),
            out CupertinoThemeData theme);

        Assert.Equal(CupertinoColors.SystemRed.Color, theme.TextTheme.ActionTextStyle.Color);
    }

    [Fact]
    public void DependentAttribute_CanBeOverriddenFromTheCascadedValue()
    {
        using var harness = TestTheme(
            new CupertinoThemeData(
                brightness: PlatformBrightness.Dark,
                textTheme: new CupertinoTextThemeData(textStyle: new TextStyle(Color: CupertinoColors.Black))),
            out CupertinoThemeData theme);

        // The brightness still cascaded down to the background color.
        Assert.Equal(CupertinoColors.Black, theme.ScaffoldBackgroundColor.Value);
        // But not to the font color, which was overridden.
        Assert.Equal(CupertinoColors.Black, theme.TextTheme.TextStyle.Color);
    }

    [Fact]
    public void ReadingThemes_CreatesDependencies()
    {
        Color barBackground = Color.FromUInt32(0x11223344);
        using var harness = new CupertinoThemeTestHarness(new CupertinoTheme(
            new CupertinoThemeData(
                barBackgroundColor: barBackground,
                textTheme: new CupertinoTextThemeData(
                    textStyle: new TextStyle(FontFamily: new FontFamily("Skeuomorphic")))),
            _singletonThemeSubtree));

        Assert.Equal(1, _buildCount);
        Assert.Equal("Skeuomorphic", _actualTheme!.TextTheme.TextStyle.FontFamily!.Name);

        // Changing another property also triggers a rebuild.
        harness.PumpWidget(new CupertinoTheme(
            new CupertinoThemeData(
                brightness: PlatformBrightness.Light,
                barBackgroundColor: barBackground,
                textTheme: new CupertinoTextThemeData(
                    textStyle: new TextStyle(FontFamily: new FontFamily("Skeuomorphic")))),
            _singletonThemeSubtree));

        Assert.Equal(2, _buildCount);
        Assert.Equal("Skeuomorphic", _actualTheme!.TextTheme.TextStyle.FontFamily!.Name);

        harness.PumpWidget(new CupertinoTheme(
            new CupertinoThemeData(
                brightness: PlatformBrightness.Light,
                barBackgroundColor: barBackground,
                textTheme: new CupertinoTextThemeData(
                    textStyle: new TextStyle(FontFamily: new FontFamily("Flat")))),
            _singletonThemeSubtree));

        Assert.Equal(3, _buildCount);
        Assert.Equal("Flat", _actualTheme!.TextTheme.TextStyle.FontFamily!.Name);
    }

    [Fact]
    public void ReadingThemes_DoesNotRebuildWhenTheDataIsUnchanged()
    {
        var data = new CupertinoThemeData(primaryColor: CupertinoColors.SystemRed);
        using var harness = new CupertinoThemeTestHarness(
            new CupertinoTheme(data, _singletonThemeSubtree));

        Assert.Equal(1, _buildCount);

        harness.PumpWidget(new CupertinoTheme(
            new CupertinoThemeData(primaryColor: CupertinoColors.SystemRed),
            _singletonThemeSubtree));

        Assert.Equal(1, _buildCount);
    }

    [Fact]
    public void CopyWith_Works()
    {
        var originalTheme = new CupertinoThemeData(
            brightness: PlatformBrightness.Dark,
            applyThemeToAll: true);

        using var harness = TestTheme(
            originalTheme.CopyWith(primaryColor: CupertinoColors.SystemGreen, applyThemeToAll: false),
            out CupertinoThemeData theme);

        Assert.Equal(PlatformBrightness.Dark, theme.Brightness);
        Assert.Equal(CupertinoColors.SystemGreen.DarkColor, theme.PrimaryColor.Value);
        // Now check calculated derivatives.
        Assert.Equal(CupertinoColors.SystemGreen.DarkColor, theme.TextTheme.ActionTextStyle.Color);
        Assert.Equal(CupertinoColors.Black, theme.ScaffoldBackgroundColor.Value);
        Assert.False(theme.ApplyThemeToAll);
    }

    [Fact]
    public void Theme_HasADefaultIconThemeDerivedFromThePrimaryColor()
    {
        CupertinoDynamicColor primaryColor = CupertinoColors.SystemRed;
        var themeData = new CupertinoThemeData(primaryColor: primaryColor);

        using (TestIconTheme(themeData, out IconThemeData lightIconTheme))
        {
            Assert.Equal(primaryColor.Color, lightIconTheme.Color);
        }

        // Works in dark mode when primaryColor is a CupertinoDynamicColor.
        using (TestIconTheme(
                   themeData.CopyWith(brightness: PlatformBrightness.Dark),
                   out IconThemeData darkIconTheme))
        {
            Assert.Equal(primaryColor.DarkColor, darkIconTheme.Color);
        }
    }

    [Fact]
    public void IconThemeOf_CreatesADependencyOnTheIconTheme()
    {
        using var harness = new CupertinoThemeTestHarness(new CupertinoTheme(
            new CupertinoThemeData(primaryColor: CupertinoColors.DestructiveRed),
            _singletonThemeSubtree));

        Assert.Equal(1, _buildCount);
        Assert.Equal(CupertinoColors.DestructiveRed.Color, _actualIconTheme!.Color);

        harness.PumpWidget(new CupertinoTheme(
            new CupertinoThemeData(primaryColor: CupertinoColors.ActiveOrange),
            _singletonThemeSubtree));

        Assert.Equal(2, _buildCount);
        Assert.Equal(CupertinoColors.ActiveOrange.Color, _actualIconTheme!.Color);
    }

    [Fact]
    public void CupertinoThemeData_Equality()
    {
        var a = new CupertinoThemeData(brightness: PlatformBrightness.Dark);
        CupertinoThemeData b = a.CopyWith();
        CupertinoThemeData c = a.CopyWith(brightness: PlatformBrightness.Light);

        Assert.Equal(a, b);
        Assert.Equal(b, a);
        Assert.NotEqual(a, c);
        Assert.NotEqual(c, a);
        Assert.NotEqual(b, c);
        Assert.NotEqual(c, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void NoDefaultCupertinoThemeData_Equality()
    {
        var a = new NoDefaultCupertinoThemeData();
        NoDefaultCupertinoThemeData b = a.CopyWith();
        NoDefaultCupertinoThemeData c = a.CopyWith(brightness: PlatformBrightness.Light);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        // Dart compares `runtimeType`, so the defaulted subclass never equals the raw form.
        Assert.NotEqual<object>(a, new CupertinoThemeData());
    }

    [Fact]
    public void NoDefault_ReportsOnlyTheValuesThatWereSpecified()
    {
        var theme = new CupertinoThemeData(primaryColor: CupertinoColors.SystemRed);
        NoDefaultCupertinoThemeData raw = theme.NoDefault();

        Assert.Equal(CupertinoColors.SystemRed, raw.PrimaryColor);
        Assert.Null(raw.PrimaryContrastingColor);
        Assert.Null(raw.TextTheme);
        Assert.Null(raw.BarBackgroundColor);
        Assert.Null(raw.ScaffoldBackgroundColor);
        Assert.Null(raw.SelectionHandleColor);
        Assert.Null(raw.ApplyThemeToAll);
        Assert.Same(raw, raw.NoDefault());
    }

    [Theory]
    [InlineData(PlatformBrightness.Light)]
    [InlineData(PlatformBrightness.Dark)]
    public void Of_ResolvesColors(PlatformBrightness brightness)
    {
        var data = new CupertinoThemeData(
            brightness: brightness,
            primaryColor: CupertinoColors.SystemRed);

        Assert.Equal(CupertinoColors.SystemRed.Color, data.PrimaryColor.Value);

        using var harness = TestTheme(data, out CupertinoThemeData theme);

        Assert.Equal(Variant(CupertinoColors.SystemRed, brightness), theme.PrimaryColor.Value);
    }

    [Theory]
    [InlineData(PlatformBrightness.Light)]
    [InlineData(PlatformBrightness.Dark)]
    public void Of_ResolvesDefaultValues(PlatformBrightness brightness)
    {
        CupertinoDynamicColor primaryColor = CupertinoColors.SystemRed;
        var data = new CupertinoThemeData(brightness: brightness, primaryColor: primaryColor);
        CupertinoDynamicColor barBackgroundColor = CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xF0F9F9F9),
            Color.FromUInt32(0xF01D1D1D));

        using var harness = TestTheme(data, out CupertinoThemeData theme);

        Assert.Equal(CupertinoColors.White, theme.PrimaryContrastingColor.Value);
        Assert.Equal(Variant(barBackgroundColor, brightness), theme.BarBackgroundColor.Value);
        Assert.Equal(
            Variant(CupertinoColors.SystemBackground, brightness),
            theme.ScaffoldBackgroundColor.Value);
        Assert.Equal(Variant(CupertinoColors.SystemBlue, brightness), theme.SelectionHandleColor.Value);
        Assert.Equal(Variant(CupertinoColors.Label, brightness), theme.TextTheme.TextStyle.Color);
        Assert.Equal(Variant(primaryColor, brightness), theme.TextTheme.ActionTextStyle.Color);
        Assert.Equal(
            Variant(CupertinoColors.InactiveGray, brightness),
            theme.TextTheme.TabLabelTextStyle.Color);
        Assert.Equal(Variant(CupertinoColors.Label, brightness), theme.TextTheme.NavTitleTextStyle.Color);
        Assert.Equal(
            Variant(CupertinoColors.Label, brightness),
            theme.TextTheme.NavLargeTitleTextStyle.Color);
        Assert.Equal(Variant(primaryColor, brightness), theme.TextTheme.NavActionTextStyle.Color);
        Assert.Equal(Variant(CupertinoColors.Label, brightness), theme.TextTheme.PickerTextStyle.Color);
        Assert.Equal(
            Variant(CupertinoColors.Label, brightness),
            theme.TextTheme.DateTimePickerTextStyle.Color);
    }

    [Fact]
    public void BrightnessOf_PrefersTheThemeAndFallsBackToTheMediaQuery()
    {
        PlatformBrightness? fromTheme = null;
        PlatformBrightness? fromMediaQuery = null;
        PlatformBrightness? maybeWithoutAncestors = null;

        using var themed = new CupertinoThemeTestHarness(new MediaQuery(
            new MediaQueryData(PlatformBrightness: PlatformBrightness.Light),
            new CupertinoTheme(
                new CupertinoThemeData(brightness: PlatformBrightness.Dark),
                new Builder(context =>
                {
                    fromTheme = CupertinoTheme.BrightnessOf(context);
                    return new SizedBox();
                }))));

        using var unthemed = new CupertinoThemeTestHarness(new MediaQuery(
            new MediaQueryData(PlatformBrightness: PlatformBrightness.Dark),
            new Builder(context =>
            {
                fromMediaQuery = CupertinoTheme.BrightnessOf(context);
                return new SizedBox();
            })));

        using var bare = new CupertinoThemeTestHarness(new Builder(context =>
        {
            maybeWithoutAncestors = CupertinoTheme.MaybeBrightnessOf(context);
            return new SizedBox();
        }));

        Assert.Equal(PlatformBrightness.Dark, fromTheme);
        Assert.Equal(PlatformBrightness.Dark, fromMediaQuery);
        Assert.Null(maybeWithoutAncestors);
    }

    [Fact]
    public void InheritedCupertinoTheme_WrapsCapturedSubtreesBackIntoATheme()
    {
        CupertinoThemeData? captured = null;
        var inherited = new InheritedCupertinoTheme(
            new CupertinoTheme(
                new CupertinoThemeData(primaryColor: CupertinoColors.SystemPink),
                new SizedBox()),
            new SizedBox());

        using var harness = new CupertinoThemeTestHarness(inherited.Wrap(
            default,
            new Builder(context =>
            {
                captured = CupertinoTheme.Of(context);
                return new SizedBox();
            })));

        Assert.Equal(CupertinoColors.SystemPink.Color, captured!.PrimaryColor.Value);
    }

    private static Color Variant(CupertinoDynamicColor color, PlatformBrightness brightness)
    {
        return brightness == PlatformBrightness.Dark ? color.DarkColor : color.Color;
    }

    private CupertinoThemeTestHarness TestTheme(CupertinoThemeData data, out CupertinoThemeData theme)
    {
        var harness = new CupertinoThemeTestHarness(new CupertinoTheme(data, _singletonThemeSubtree));
        theme = _actualTheme!;
        return harness;
    }

    private CupertinoThemeTestHarness TestIconTheme(CupertinoThemeData data, out IconThemeData iconTheme)
    {
        var harness = new CupertinoThemeTestHarness(new CupertinoTheme(data, _singletonThemeSubtree));
        iconTheme = _actualIconTheme!;
        return harness;
    }
}
