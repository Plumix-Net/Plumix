using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Widgets;
using Xunit;
using TextStyle = Plumix.Widgets.TextStyle;

namespace Plumix.Tests;

// The dart:ui `Color` subtypes: `ColorSwatch<T> extends Color` (painting/colors.dart),
// `CupertinoDynamicColor implements Color` (cupertino_ui/colors.dart) and
// `WidgetStateColor extends Color implements WidgetStateProperty<Color>` (widgets/widget_state.dart).
// Each can sit in a plain `Color` slot and be recovered with a runtime type test.
public sealed class ColorSubtypeTests
{
    private static readonly IReadOnlySet<WidgetState> NoStates = new HashSet<WidgetState>();

    [Fact]
    public void MaterialColor_IsAColorWhoseValueIsItsPrimaryShade()
    {
        Color primary = MaterialColors.Blue;
        Assert.IsType<MaterialColor>(primary);
        Assert.Equal(MaterialColors.Blue.Shade500.Value, primary.Value);
        Assert.Equal(MaterialColors.Blue.Shade500.ToARGB32(), primary.ToARGB32());

        // Dart's `==` checks the runtime type, so a swatch never equals its own plain shade.
        Assert.NotEqual(MaterialColors.Blue.Shade500, primary);
        Assert.Equal<Color>(MaterialColors.Blue, primary);
    }

    [Fact]
    public void ColorSwatch_EqualityComparesTypeValueAndEveryShade()
    {
        var a = new ColorSwatch<int>(0xFF000000, new Dictionary<int, Color> { [1] = new(0xFF111111) });
        var b = new ColorSwatch<int>(0xFF000000, new Dictionary<int, Color> { [1] = new(0xFF111111) });
        var c = new ColorSwatch<int>(0xFF000000, new Dictionary<int, Color> { [1] = new(0xFF222222) });
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.NotEqual<Color>(new Color(0xFF000000), a);
    }

    [Fact]
    public void ColorSwatch_ToStringNamesTheRuntimeTypeAndThePrimaryColor()
    {
        Assert.Equal(
            $"MaterialColor(primary value: {new Color(0xFF2196F3)})",
            MaterialColors.Blue.ToString());
        Assert.Equal(
            $"ColorSwatch<int>(primary value: {new Color(0x12345678)})",
            new ColorSwatch<int>(0x12345678, new Dictionary<int, Color>()).ToString());
    }

    [Fact]
    public void ThemeData_KeepsTheSwatchObjectInPrimaryColor()
    {
        var theme = new ThemeData(useMaterial3: false, primarySwatch: MaterialColors.Green);
        Assert.Same(MaterialColors.Green, theme.PrimaryColor);
        Assert.Same(MaterialColors.Green, theme.ColorScheme.Primary);

        // `colorScheme.secondary` is `primarySwatch[500]!`, a plain colour, so Dart's
        // `colorScheme.secondary == primaryColor` is false and the indicator falls back to secondary.
#pragma warning disable CS0618 // Deprecated in Flutter, still defaulted by ThemeData.
        Assert.Equal(theme.ColorScheme.Secondary, theme.IndicatorColor);
#pragma warning restore CS0618
    }

    [Fact]
    public void CupertinoDynamicColor_IsAColorThatForwardsToItsEffectiveVariant()
    {
        Color color = CupertinoColors.SystemBlue;
        var dynamicColor = Assert.IsType<CupertinoDynamicColor>(color);
        Assert.Equal(dynamicColor.Color.Value, color.Value);
        Assert.Equal(dynamicColor.Color.A, color.A);
        Assert.Equal(dynamicColor.Color.ComputeLuminance(), color.ComputeLuminance());

        // Every derived colour is a plain one built from the effective variant.
        Assert.IsType<Color>(color.WithOpacity(0.5));
        Assert.IsType<Color>(color.WithAlpha(0x80));
        Assert.IsType<Color>(color.WithValues());
        Assert.Equal(dynamicColor.Color, color.WithValues());
    }

    [Fact]
    public void CupertinoDynamicColor_ResolveRecoversItFromAPlainColorSlot()
    {
        Color? resolved = null;
        Color plain = new(0xFF123456);
        Color? plainResolved = null;
        var style = new TextStyle(Color: CupertinoColors.Label);
        using var harness = new CupertinoThemeTestHarness(new CupertinoTheme(
            new CupertinoThemeData(brightness: PlatformBrightness.Dark),
            new Builder(context =>
            {
                resolved = CupertinoDynamicColor.MaybeResolve(style.Color, context);
                plainResolved = CupertinoDynamicColor.Resolve(plain, context);
                return new SizedBox();
            })));

        var dynamicResolved = Assert.IsType<CupertinoDynamicColor>(resolved);
        Assert.Equal(CupertinoColors.Label.DarkColor.Value, dynamicResolved.Value);
        Assert.Same(plain, plainResolved);
    }

    [Fact]
    public void CupertinoTextTheme_ResolvesDynamicColorsInsideCallerSuppliedStyles()
    {
        CupertinoTextThemeData? resolved = null;
        var textTheme = new CupertinoTextThemeData(
            textStyle: new TextStyle(
                Color: CupertinoColors.SystemRed,
                BackgroundColor: CupertinoColors.SystemGreen,
                DecorationColor: CupertinoColors.SystemBlue));
        using var harness = new CupertinoThemeTestHarness(new CupertinoTheme(
            new CupertinoThemeData(brightness: PlatformBrightness.Dark),
            new Builder(context =>
            {
                resolved = textTheme.ResolveFrom(context);
                return new SizedBox();
            })));

        // Dart's `_resolveTextStyle` resolves color, backgroundColor and decorationColor.
        TextStyle style = resolved!.TextStyle;
        Assert.Equal(CupertinoColors.SystemRed.DarkColor.Value, style.Color!.Value);
        Assert.Equal(CupertinoColors.SystemGreen.DarkColor.Value, style.BackgroundColor!.Value);
        Assert.Equal(CupertinoColors.SystemBlue.DarkColor.Value, style.DecorationColor!.Value);
    }

    [Fact]
    public void CupertinoIconThemeData_ResolvesItsColorOnlyWhenItIsDynamic()
    {
        var plain = new CupertinoIconThemeData(Color: new Color(0xFF654321));
        IconThemeData? plainResolved = null;
        IconThemeData? dynamicResolved = null;
        var dynamicTheme = new CupertinoIconThemeData(Color: CupertinoColors.SystemBlue);
        using var harness = new CupertinoThemeTestHarness(new CupertinoTheme(
            new CupertinoThemeData(brightness: PlatformBrightness.Dark),
            new Builder(context =>
            {
                plainResolved = plain.Resolve(context);
                dynamicResolved = dynamicTheme.Resolve(context);
                return new SizedBox();
            })));

        Assert.Same(plain, plainResolved);
        Assert.Equal(CupertinoColors.SystemBlue.DarkColor.Value, dynamicResolved!.Color!.Value);
    }

    [Fact]
    public void CreateCupertinoColorProperty_DescribesADynamicColorByItsLabel()
    {
        DiagnosticsNode dynamicProperty = CupertinoColors.CreateCupertinoColorProperty("color", CupertinoColors.Label);
        Assert.IsType<DiagnosticsProperty<CupertinoDynamicColor>>(dynamicProperty);
        Assert.Equal("label", dynamicProperty.ToDescription());

        DiagnosticsNode plainProperty = CupertinoColors.CreateCupertinoColorProperty("color", new Color(0xFF000000));
        Assert.IsType<ColorProperty>(plainProperty);
    }

    [Fact]
    public void WidgetStateColor_ResolveWithTakesItsComponentsFromTheEmptyStateSet()
    {
        Color color = WidgetStateColor.ResolveWith(states =>
            states.Contains(WidgetState.Pressed) ? new Color(0xFF00FF00) : new Color(0xFFFF0000));

        Assert.IsAssignableFrom<WidgetStateColor>(color);
        Assert.Equal(0xFFFF0000u, color.Value);
        Assert.Equal(
            new Color(0xFF00FF00),
            WidgetStateProperty<Color>.ResolveAs(color, new HashSet<WidgetState> { WidgetState.Pressed }));
        Assert.Equal(new Color(0xFFFF0000), WidgetStateProperty<Color>.ResolveAs(color, NoStates));
    }

    [Fact]
    public void WidgetStateProperty_ResolveAsPassesPlainValuesThrough()
    {
        var plain = new Color(0xFF123456);
        Assert.Same(plain, WidgetStateProperty<Color>.ResolveAs(plain, NoStates));
        Assert.Null(WidgetStateProperty<Color?>.ResolveAs(null, NoStates));
    }

    [Fact]
    public void WidgetStateColor_TransparentIsTransparentForEveryState()
    {
        Assert.Equal(0x00000000u, WidgetStateColor.Transparent.Value);
        Assert.Equal(
            new Color(0x00000000),
            WidgetStateColor.Transparent.Resolve(new HashSet<WidgetState> { WidgetState.Hovered }));
    }

    [Fact]
    public void WidgetStateColor_FromMapPerformsAccurateEqualityChecks()
    {
        // widget_state_property_test.dart: '.fromMap() constructors perform accurate equality checks'.
        var white = new Color(0xFFFFFFFF);
        var black = new Color(0xFF000000);
        WidgetStateColor color1 = WidgetStateColor.FromMap(
        [
            new((WidgetStatesConstraint)WidgetState.Focused | WidgetState.Hovered, white),
            new(WidgetStatesConstraint.Any, black),
        ]);
        WidgetStateColor color2 = WidgetStateColor.FromMap(
        [
            new((WidgetStatesConstraint)WidgetState.Focused | WidgetState.Hovered, white),
            new(WidgetStatesConstraint.Any, black),
        ]);
        WidgetStateColor color3 = WidgetStateColor.FromMap(
        [
            new((WidgetStatesConstraint)WidgetState.Focused | WidgetState.Hovered, black),
            new(WidgetStatesConstraint.Any, white),
        ]);
        Assert.True(color1 == color2);
        Assert.False(color1 == color3);
        Assert.Equal(white, color1.Resolve(new HashSet<WidgetState> { WidgetState.Hovered }));
        Assert.Equal(black, color1.Resolve(NoStates));
    }

    [Fact]
    public void WidgetStateColor_FromMapRefusesToActAsAPlainColor()
    {
        WidgetStateColor color = WidgetStateColor.FromMap([new(WidgetStatesConstraint.Any, new Color(0))]);
        FlutterError error = Assert.Throws<FlutterError>(() => color.Value);
        Assert.Contains("WidgetStateMapper<Color>", error.Message);
        Assert.Throws<FlutterError>(() => color.A);
    }

    [Fact]
    public void WidgetStateColor_SitsInAPlainColorSlotAndIsResolvedByTheWidget()
    {
        var themeColor = WidgetStateColor.ResolveWith(_ => new Color(0xFF0000FF));
        var data = new AppBarThemeData(BackgroundColor: themeColor);
        Assert.Same(themeColor, data.BackgroundColor);
        Assert.IsAssignableFrom<WidgetStateColor>(data.BackgroundColor);
    }
}
