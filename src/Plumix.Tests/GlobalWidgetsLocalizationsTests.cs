using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Ported from flutter_localizations/test/widgets/translations_test.dart.

public sealed class GlobalWidgetsLocalizationsTests
{
    public static TheoryData<string> AbstractWidgetsLabels() =>
        new()
        {
            nameof(WidgetsLocalizations.CopyButtonLabel),
            nameof(WidgetsLocalizations.CutButtonLabel),
            nameof(WidgetsLocalizations.PasteButtonLabel),
            nameof(WidgetsLocalizations.SelectAllButtonLabel),
            nameof(WidgetsLocalizations.LookUpButtonLabel),
            nameof(WidgetsLocalizations.SearchWebButtonLabel),
            nameof(WidgetsLocalizations.ShareButtonLabel),
            nameof(WidgetsLocalizations.RadioButtonUnselectedLabel),
            nameof(WidgetsLocalizations.ReorderItemToStart),
            nameof(WidgetsLocalizations.ReorderItemToEnd),
            nameof(WidgetsLocalizations.ReorderItemUp),
            nameof(WidgetsLocalizations.ReorderItemDown),
            nameof(WidgetsLocalizations.ReorderItemLeft),
            nameof(WidgetsLocalizations.ReorderItemRight),
        };

    public static TheoryData<string> SupportedLanguages()
    {
        var data = new TheoryData<string>();
        foreach (string language in GlobalWidgetsLocalizations.WidgetsSupportedLanguages)
        {
            data.Add(language);
        }

        return data;
    }

    private static WidgetsLocalizations Load(Locale locale) =>
        GlobalWidgetsLocalizations.Delegate.LoadTyped(locale);

    [Theory]
    [MemberData(nameof(SupportedLanguages))]
    public void TranslationsExistForEveryLanguage(string language)
    {
        var locale = new Locale(language);
        Assert.True(GlobalWidgetsLocalizations.Delegate.IsSupported(locale));

        WidgetsLocalizations localizations = Load(locale);

        Assert.NotNull(localizations.ReorderItemDown);
        Assert.NotNull(localizations.ReorderItemLeft);
        Assert.NotNull(localizations.ReorderItemRight);
        Assert.NotNull(localizations.ReorderItemToEnd);
        Assert.NotNull(localizations.ReorderItemToStart);
        Assert.NotNull(localizations.ReorderItemUp);
        Assert.NotNull(localizations.CopyButtonLabel);
        Assert.NotNull(localizations.CutButtonLabel);
        Assert.NotNull(localizations.PasteButtonLabel);
        Assert.NotNull(localizations.SelectAllButtonLabel);
        Assert.NotNull(localizations.RadioButtonUnselectedLabel);
    }

    [Fact]
    public void TextDirectionFollowsTheLanguage()
    {
        Assert.Equal(TextDirection.Ltr, Load(new Locale("en")).TextDirection);
        Assert.Equal(TextDirection.Ltr, Load(new Locale("zh")).TextDirection);
        Assert.Equal(TextDirection.Rtl, Load(new Locale("ar")).TextDirection);
        Assert.Equal(TextDirection.Rtl, Load(new Locale("fa")).TextDirection);
        Assert.Equal(TextDirection.Rtl, Load(new Locale("he")).TextDirection);
        Assert.Equal(TextDirection.Rtl, Load(new Locale("ps")).TextDirection);
        Assert.Equal(TextDirection.Rtl, Load(new Locale("ur")).TextDirection);
    }

    [Fact]
    public void SpotCheckGerman()
    {
        WidgetsLocalizations localizations = Load(new Locale("de"));

        Assert.IsType<WidgetsLocalizationDe>(localizations);
        Assert.Equal("Nach oben verschieben", localizations.ReorderItemUp);
        Assert.Equal("Kopieren", localizations.CopyButtonLabel);
    }

    [Fact]
    public void SublocalesResolveThroughScriptAndCountryCodes()
    {
        Assert.IsType<WidgetsLocalizationDeCh>(Load(new Locale("de", "CH")));
        Assert.IsType<WidgetsLocalizationZhHantHk>(Load(new Locale("zh", "HK")));
        Assert.IsType<WidgetsLocalizationSrLatn>(Load(new Locale("sr", null, "Latn")));
    }

    [Fact]
    public void DefaultWidgetsLocalizationsMatchFlutterDefaults()
    {
        var localizations = new DefaultWidgetsLocalizations();

        Assert.Equal("Move to the start", localizations.ReorderItemToStart);
        Assert.Equal("Move to the end", localizations.ReorderItemToEnd);
        Assert.Equal("Move up", localizations.ReorderItemUp);
        Assert.Equal("Move down", localizations.ReorderItemDown);
        Assert.Equal("Move left", localizations.ReorderItemLeft);
        Assert.Equal("Move right", localizations.ReorderItemRight);
        Assert.Equal("Search results found", localizations.SearchResultsFound);
        Assert.Equal("No results found", localizations.NoResultsFound);
        Assert.Equal("Copy", localizations.CopyButtonLabel);
        Assert.Equal("Cut", localizations.CutButtonLabel);
        Assert.Equal("Paste", localizations.PasteButtonLabel);
        Assert.Equal("Select all", localizations.SelectAllButtonLabel);
        Assert.Equal("Look Up", localizations.LookUpButtonLabel);
        Assert.Equal("Search Web", localizations.SearchWebButtonLabel);
        Assert.Equal("Share", localizations.ShareButtonLabel);
        Assert.Equal("Not selected", localizations.RadioButtonUnselectedLabel);
        Assert.Equal(TextDirection.Ltr, localizations.TextDirection);
        Assert.Equal(
            TextDirection.Ltr,
            DefaultWidgetsLocalizations.Delegate.LoadTyped(new Locale("ar")).TextDirection);
        Assert.IsType<DefaultWidgetsLocalizations>(
            DefaultWidgetsLocalizations.Load(new Locale("de")));
        Assert.Equal(
            "DefaultWidgetsLocalizations.delegate(en_US)",
            DefaultWidgetsLocalizations.Delegate.ToString());
    }

    [Theory]
    [MemberData(nameof(AbstractWidgetsLabels))]
    public void WidgetsLocalizationsRequiresFlutterAbstractLabels(string propertyName)
    {
        var property = typeof(WidgetsLocalizations).GetProperty(propertyName);

        Assert.NotNull(property);
        Assert.True(property.GetMethod!.IsAbstract);
    }

    [Fact]
    public void DelegateDescribesItself()
    {
        Assert.Equal(
            $"GlobalWidgetsLocalizations.delegate("
            + $"{GlobalWidgetsLocalizations.WidgetsSupportedLanguages.Count} locales)",
            GlobalWidgetsLocalizations.Delegate.ToString());
    }
}
