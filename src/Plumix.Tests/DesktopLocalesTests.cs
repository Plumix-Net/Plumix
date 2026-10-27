using Plumix.Widgets;
using Xunit;

// C#-only host tests: the desktop engines' preferred-language readers — Windows system_utils.cc
// (GetPreferredLanguages, ParseLanguageName; mirrors system_utils_unittests.cc), Linux fl_engine.cc
// (setup_locales, parse_locale over g_get_language_names) and macOS FlutterEngine.mm (preferredLanguages).

namespace Plumix.Tests;

public sealed class DesktopLocalesTests
{
    [Fact]
    public void ParseMultiString_SplitsAtNullsAndStopsAtTheEmptyString()
    {
        Assert.Equal(["en-US", "fr-FR", "zh-Hans-CN"], DesktopLocales.ParseMultiString("en-US\0fr-FR\0zh-Hans-CN\0\0"));
        Assert.Equal(["en-US"], DesktopLocales.ParseMultiString("en-US\0\0ignored\0\0"));
        Assert.Equal(["de-DE"], DesktopLocales.ParseMultiString("de-DE"));
        Assert.Empty(DesktopLocales.ParseMultiString("\0\0"));
        Assert.Empty(DesktopLocales.ParseMultiString(""));
    }

    [Theory]
    [InlineData("en", "en", null, null)]
    [InlineData("hu-HU", "hu", "HU", null)]
    [InlineData("uz-Latn", "uz", null, "Latn")]
    [InlineData("uz-Latn-UZ", "uz", "UZ", "Latn")]
    [InlineData("en-US-x-fabricam", "en", "US", null)]
    public void ParseWindowsLanguageName_SplitsLanguageScriptAndRegion(
        string name, string language, string? region, string? script)
    {
        Assert.Equal(new Locale(language, region, script), DesktopLocales.ParseWindowsLanguageName(name));
    }

    [Fact]
    public void ParseWindowsLanguageName_WithoutALanguageIsNull()
    {
        Assert.Null(DesktopLocales.ParseWindowsLanguageName(""));
        Assert.Null(DesktopLocales.ParseWindowsLanguageName("x-private"));
    }

    [Theory]
    [InlineData("en_US.UTF-8@euro", "en", "US")]
    [InlineData("pt_BR", "pt", "BR")]
    [InlineData("de.UTF-8", "de", null)]
    [InlineData("C", "C", null)]
    public void ParseLinuxLocale_StripsCodesetAndModifier(string locale, string language, string? territory)
    {
        DesktopLocales.ParseLinuxLocale(locale, out string parsedLanguage, out string? parsedTerritory);
        Assert.Equal(language, parsedLanguage);
        Assert.Equal(territory, parsedTerritory);
    }

    [Fact]
    public void ReadLinux_PrefersTheLanguageListAndAddsEachLanguageAlone()
    {
        IReadOnlyList<Locale> locales = DesktopLocales.ReadLinux(Environment(
            ("LANGUAGE", "de_DE:pt_BR::en:de"), ("LANG", "fr_FR.UTF-8")));

        Assert.Equal([new("de", "DE"), new("de"), new("pt", "BR"), new("pt"), new("en")], locales);
    }

    [Fact]
    public void ReadLinux_FallsBackThroughLcAllLcMessagesAndLang()
    {
        Assert.Equal(
            [new Locale("sv", "SE"), new Locale("sv")],
            DesktopLocales.ReadLinux(Environment(("LANGUAGE", ""), ("LC_ALL", "sv_SE.UTF-8"), ("LANG", "en_US"))));
        Assert.Equal(
            [new Locale("ja", "JP"), new Locale("ja")],
            DesktopLocales.ReadLinux(Environment(("LC_MESSAGES", "ja_JP.eucJP"), ("LANG", "en_US"))));
        Assert.Equal(
            [new Locale("en", "GB"), new Locale("en")],
            DesktopLocales.ReadLinux(Environment(("LANG", "en_GB.UTF-8@euro"))));
    }

    [Fact]
    public void ReadLinux_SkipsCAndPosix()
    {
        Assert.Empty(DesktopLocales.ReadLinux(Environment(("LANG", "C.UTF-8"))));
        Assert.Empty(DesktopLocales.ReadLinux(Environment(("LC_ALL", "POSIX"))));
        Assert.Empty(DesktopLocales.ReadLinux(Environment()));
        Assert.Equal([new Locale("nl")], DesktopLocales.ReadLinux(Environment(("LANGUAGE", "C:nl"))));
    }

    [Theory]
    [InlineData("en-US", "en", "US", null)]
    [InlineData("zh-Hans-CN", "zh", "CN", "Hans")]
    [InlineData("zh-Hant", "zh", null, "Hant")]
    [InlineData("es-419", "es", "419", null)]
    [InlineData("de", "de", null, null)]
    [InlineData("en-GB-u-ca-gregory", "en", "GB", null)]
    [InlineData("en_US", "en", "US", null)]
    public void ParseLanguageIdentifier_SplitsTheSubtags(
        string identifier, string language, string? region, string? script)
    {
        Assert.Equal(new Locale(language, region, script), DesktopLocales.ParseLanguageIdentifier(identifier));
    }

    // Runs the CoreFoundation path for real on a macOS test host.
    [Fact]
    public void ReadMacOS_ReturnsThePreferredLanguagesOfTheUser()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        IReadOnlyList<Locale> locales = DesktopLocales.ReadMacOS();

        Assert.NotEmpty(locales);
        Assert.All(locales, locale => Assert.False(string.IsNullOrEmpty(locale.LanguageCode)));
        Assert.Equal(locales, DesktopLocales.Read());
    }

    private static Func<string, string?> Environment(params (string Name, string Value)[] variables) =>
        name => variables.FirstOrDefault(variable => variable.Name == name).Value;
}
