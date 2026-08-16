using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Mirrors cupertino_ui/test/text_theme_test.dart.
public sealed class CupertinoTextThemeTests
{
    [Fact]
    public void DefaultTextTheme_MatchesTheAppleDesignResources()
    {
        var theme = new CupertinoTextThemeData();

        // TextStyle 17 -0.41
        Assert.Equal(17.0, theme.TextStyle.FontSize);
        Assert.Equal("CupertinoSystemText", theme.TextStyle.FontFamily!.Name);
        Assert.Equal(-0.41, theme.TextStyle.LetterSpacing);
        Assert.Null(theme.TextStyle.FontWeight);

        // ActionTextStyle 17 -0.41
        Assert.Equal(17.0, theme.ActionTextStyle.FontSize);
        Assert.Equal("CupertinoSystemText", theme.ActionTextStyle.FontFamily!.Name);
        Assert.Equal(-0.41, theme.ActionTextStyle.LetterSpacing);
        Assert.Null(theme.ActionTextStyle.FontWeight);

        // ActionSmallTextStyle 15 -0.23 (aka "Subheadline/Regular")
        Assert.Equal(15.0, theme.ActionSmallTextStyle.FontSize);
        Assert.Equal("CupertinoSystemText", theme.ActionSmallTextStyle.FontFamily!.Name);
        Assert.Equal(-0.23, theme.ActionSmallTextStyle.LetterSpacing);
        Assert.Null(theme.ActionSmallTextStyle.FontWeight);

        // TabLabel medium 10 -0.24
        Assert.Equal(10.0, theme.TabLabelTextStyle.FontSize);
        Assert.Equal("CupertinoSystemText", theme.TabLabelTextStyle.FontFamily!.Name);
        Assert.Equal(-0.24, theme.TabLabelTextStyle.LetterSpacing);
        Assert.Equal(FontWeight.Medium, theme.TabLabelTextStyle.FontWeight);

        // NavTitle SemiBold 17 -0.41
        Assert.Equal(17.0, theme.NavTitleTextStyle.FontSize);
        Assert.Equal("CupertinoSystemText", theme.NavTitleTextStyle.FontFamily!.Name);
        Assert.Equal(-0.41, theme.NavTitleTextStyle.LetterSpacing);
        Assert.Equal(FontWeight.SemiBold, theme.NavTitleTextStyle.FontWeight);

        // NavLargeTitle Bold 34 0.38
        Assert.Equal(34.0, theme.NavLargeTitleTextStyle.FontSize);
        Assert.Equal("CupertinoSystemDisplay", theme.NavLargeTitleTextStyle.FontFamily!.Name);
        Assert.Equal(0.38, theme.NavLargeTitleTextStyle.LetterSpacing);
        Assert.Equal(FontWeight.Bold, theme.NavLargeTitleTextStyle.FontWeight);

        // Picker Regular 21 -0.6
        Assert.Equal(21.0, theme.PickerTextStyle.FontSize);
        Assert.Equal("CupertinoSystemDisplay", theme.PickerTextStyle.FontFamily!.Name);
        Assert.Equal(-0.6, theme.PickerTextStyle.LetterSpacing);
        Assert.Equal(FontWeight.Regular, theme.PickerTextStyle.FontWeight);

        // DateTimePicker Normal 21 0.4
        Assert.Equal(21.0, theme.DateTimePickerTextStyle.FontSize);
        Assert.Equal("CupertinoSystemDisplay", theme.DateTimePickerTextStyle.FontFamily!.Name);
        Assert.Equal(0.4, theme.DateTimePickerTextStyle.LetterSpacing);
        Assert.Equal(FontWeight.Normal, theme.DateTimePickerTextStyle.FontWeight);
    }

    [Fact]
    public void DefaultTextTheme_DoesNotInheritAndPaintsTheLabelAndActionColors()
    {
        var theme = new CupertinoTextThemeData();

        Assert.False(theme.TextStyle.Inherit);
        Assert.Equal(Plumix.UI.TextDecoration.None, theme.TextStyle.Decoration);
        Assert.Equal(CupertinoColors.Label.Color, theme.TextStyle.Color);
        Assert.Equal(CupertinoColors.SystemBlue.Color, theme.ActionTextStyle.Color);
        Assert.Equal(CupertinoColors.SystemBlue.Color, theme.NavActionTextStyle.Color);
        Assert.Equal(CupertinoColors.InactiveGray.Color, theme.TabLabelTextStyle.Color);
    }

    [Fact]
    public void PrimaryColor_DrivesTheActionStyles()
    {
        var theme = new CupertinoTextThemeData(primaryColor: CupertinoColors.SystemPink);

        Assert.Equal(CupertinoColors.SystemPink.Color, theme.ActionTextStyle.Color);
        Assert.Equal(CupertinoColors.SystemPink.Color, theme.ActionSmallTextStyle.Color);
        Assert.Equal(CupertinoColors.SystemPink.Color, theme.NavActionTextStyle.Color);
        // The non-action styles keep the label color.
        Assert.Equal(CupertinoColors.Label.Color, theme.TextStyle.Color);
    }

    [Fact]
    public void ExplicitStyles_WinOverTheDefaults()
    {
        var custom = new TextStyle(FontSize: 42.0, Color: CupertinoColors.Black);
        var theme = new CupertinoTextThemeData(
            textStyle: custom,
            actionTextStyle: custom,
            actionSmallTextStyle: custom,
            tabLabelTextStyle: custom,
            navTitleTextStyle: custom,
            navLargeTitleTextStyle: custom,
            navActionTextStyle: custom,
            pickerTextStyle: custom,
            dateTimePickerTextStyle: custom);

        Assert.Equal(custom, theme.TextStyle);
        Assert.Equal(custom, theme.ActionTextStyle);
        Assert.Equal(custom, theme.ActionSmallTextStyle);
        Assert.Equal(custom, theme.TabLabelTextStyle);
        Assert.Equal(custom, theme.NavTitleTextStyle);
        Assert.Equal(custom, theme.NavLargeTitleTextStyle);
        Assert.Equal(custom, theme.NavActionTextStyle);
        Assert.Equal(custom, theme.PickerTextStyle);
        Assert.Equal(custom, theme.DateTimePickerTextStyle);
    }

    [Fact]
    public void CopyWith_ReplacesOnlyTheGivenMembers()
    {
        var custom = new TextStyle(FontSize: 42.0);
        var theme = new CupertinoTextThemeData(primaryColor: CupertinoColors.SystemPink);
        CupertinoTextThemeData copy = theme.CopyWith(navTitleTextStyle: custom);

        Assert.Equal(custom, copy.NavTitleTextStyle);
        Assert.Equal(CupertinoColors.SystemPink.Color, copy.ActionTextStyle.Color);
        Assert.Equal(theme.TextStyle, copy.TextStyle);
        Assert.Equal(theme, theme.CopyWith());
    }

    [Fact]
    public void ResolveFrom_SwitchesTheDefaultLabelColorsWithTheBrightness()
    {
        CupertinoTextThemeData? light = null;
        CupertinoTextThemeData? dark = null;

        using (new CupertinoThemeTestHarness(new MediaQuery(
                   new MediaQueryData(PlatformBrightness: PlatformBrightness.Light),
                   new Builder(context =>
                   {
                       light = new CupertinoTextThemeData().ResolveFrom(context);
                       return new SizedBox();
                   }))))
        {
        }

        using (new CupertinoThemeTestHarness(new MediaQuery(
                   new MediaQueryData(PlatformBrightness: PlatformBrightness.Dark),
                   new Builder(context =>
                   {
                       dark = new CupertinoTextThemeData().ResolveFrom(context);
                       return new SizedBox();
                   }))))
        {
        }

        Assert.Equal(CupertinoColors.Label.Color, light!.TextStyle.Color);
        Assert.Equal(CupertinoColors.InactiveGray.Color, light.TabLabelTextStyle.Color);
        Assert.Equal(CupertinoColors.Label.DarkColor, dark!.TextStyle.Color);
        Assert.Equal(CupertinoColors.InactiveGray.DarkColor, dark.TabLabelTextStyle.Color);
        Assert.Equal(CupertinoColors.Label.DarkColor, dark.NavTitleTextStyle.Color);
        Assert.Equal(CupertinoColors.Label.DarkColor, dark.NavLargeTitleTextStyle.Color);
        Assert.Equal(CupertinoColors.Label.DarkColor, dark.PickerTextStyle.Color);
        Assert.Equal(CupertinoColors.Label.DarkColor, dark.DateTimePickerTextStyle.Color);
        Assert.NotEqual(light.TextStyle, dark.TextStyle);
    }

    [Fact]
    public void ResolveFrom_KeepsTheInstanceWhenNothingChanges()
    {
        CupertinoTextThemeData? resolved = null;
        var theme = new CupertinoTextThemeData();

        using var harness = new CupertinoThemeTestHarness(new MediaQuery(
            new MediaQueryData(PlatformBrightness: PlatformBrightness.Light),
            new Builder(context =>
            {
                resolved = theme.ResolveFrom(context);
                return new SizedBox();
            })));

        Assert.Equal(theme, resolved);
    }
}
