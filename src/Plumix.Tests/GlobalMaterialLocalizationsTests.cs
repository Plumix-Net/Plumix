using Plumix.Cupertino;
using Plumix.Material;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter_localizations/test/material/translations_test.dart,
// flutter_localizations/test/material/date_time_test.dart,
// material_ui/test/localizations_test.dart

public sealed class GlobalMaterialLocalizationsTests
{
    public static TheoryData<string> SupportedLanguages()
    {
        var data = new TheoryData<string>();
        foreach (string language in GlobalMaterialLocalizations.MaterialSupportedLanguages)
        {
            data.Add(language);
        }

        return data;
    }

    private static MaterialLocalizations Load(Locale locale) =>
        GlobalMaterialLocalizations.Delegate.LoadTyped(locale);

    [Theory]
    [MemberData(nameof(SupportedLanguages))]
    public void TranslationsExistForEveryLanguage(string language)
    {
        var locale = new Locale(language);
        Assert.True(GlobalMaterialLocalizations.Delegate.IsSupported(locale));

        MaterialLocalizations localizations = Load(locale);

        Assert.NotNull(localizations.OpenAppDrawerTooltip);
        Assert.NotNull(localizations.BackButtonTooltip);
        Assert.NotNull(localizations.CloseButtonTooltip);
        Assert.NotNull(localizations.NextMonthTooltip);
        Assert.NotNull(localizations.PreviousMonthTooltip);
        Assert.NotNull(localizations.NextPageTooltip);
        Assert.NotNull(localizations.PreviousPageTooltip);
        Assert.NotNull(localizations.FirstPageTooltip);
        Assert.NotNull(localizations.LastPageTooltip);
        Assert.NotNull(localizations.ShowMenuTooltip);
        Assert.NotNull(localizations.LicensesPageTitle);
        Assert.NotNull(localizations.RowsPerPageTitle);
        Assert.NotNull(localizations.CancelButtonLabel);
        Assert.NotNull(localizations.CloseButtonLabel);
        Assert.NotNull(localizations.ContinueButtonLabel);
        Assert.NotNull(localizations.CopyButtonLabel);
        Assert.NotNull(localizations.CutButtonLabel);
        Assert.NotNull(localizations.OkButtonLabel);
        Assert.NotNull(localizations.PasteButtonLabel);
        Assert.NotNull(localizations.SelectAllButtonLabel);
        Assert.NotNull(localizations.ViewLicensesButtonLabel);
        Assert.NotNull(localizations.DrawerLabel);
        Assert.NotNull(localizations.PopupMenuLabel);
        Assert.NotNull(localizations.DialogLabel);
        Assert.NotNull(localizations.AlertDialogLabel);
        Assert.NotNull(localizations.CollapsedIconTapHint);
        Assert.NotNull(localizations.ExpandedIconTapHint);
        Assert.NotNull(localizations.ExpansionTileExpandedHint);
        Assert.NotNull(localizations.ExpansionTileCollapsedHint);
        Assert.NotNull(localizations.CollapsedHint);
        Assert.NotNull(localizations.ExpandedHint);
        Assert.NotNull(localizations.RefreshIndicatorSemanticLabel);
        Assert.NotNull(localizations.SelectedDateLabel);

        foreach (int remaining in new[] { 0, 1, 10 })
        {
            string text = localizations.RemainingTextFieldCharacterCount(remaining);
            Assert.NotNull(text);
            Assert.DoesNotContain("TBD", text, StringComparison.Ordinal);
            Assert.DoesNotContain("$remainingCount", text, StringComparison.Ordinal);
        }

        Assert.Contains("FOO", localizations.AboutListTileTitle("FOO"), StringComparison.Ordinal);

        foreach (int count in new[] { 0, 1, 2, 100 })
        {
            string title = localizations.SelectedRowCountTitle(count);
            Assert.NotNull(title);
            Assert.DoesNotContain("$selectedRowCount", title, StringComparison.Ordinal);
        }

        foreach (bool approximate in new[] { true, false })
        {
            string title = localizations.PageRowsInfoTitle(1, 10, 100, approximate);
            Assert.NotNull(title);
            Assert.DoesNotContain("$firstRow", title, StringComparison.Ordinal);
            Assert.DoesNotContain("$lastRow", title, StringComparison.Ordinal);
            Assert.DoesNotContain("$rowCount", title, StringComparison.Ordinal);
        }

        string tabLabel = localizations.TabLabel(2, 5);
        Assert.NotNull(tabLabel);
        Assert.DoesNotContain("$tabIndex", tabLabel, StringComparison.Ordinal);
        Assert.DoesNotContain("$tabCount", tabLabel, StringComparison.Ordinal);
        Assert.Throws<ArgumentOutOfRangeException>(() => localizations.TabLabel(0, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => localizations.TabLabel(2, 0));

        var tenOClock = new TimeOfDay(10, 0);
        Assert.NotNull(localizations.FormatHour(tenOClock));
        Assert.NotNull(localizations.FormatMinute(tenOClock));
        Assert.NotNull(localizations.FormatTimeOfDay(tenOClock));
        Assert.NotNull(localizations.FormatYear(new DateTime(2018, 8, 1)));
        Assert.NotNull(localizations.FormatMediumDate(new DateTime(2018, 8, 1)));
        Assert.NotNull(localizations.FormatFullDate(new DateTime(2018, 8, 1)));
        Assert.NotNull(localizations.FormatMonthYear(new DateTime(2018, 8, 1)));
        Assert.Equal(7, localizations.NarrowWeekdays.Count);
        Assert.InRange(localizations.FirstDayOfWeekIndex, 0, 6);
        Assert.NotNull(localizations.FormatDecimal(123));
    }

    [Fact]
    public void UsesTheExactLocaleWhenItExists()
    {
        Assert.Equal("10 000", Load(new Locale("pt", "PT")).FormatDecimal(10000));
    }

    [Fact]
    public void FallsBackToTheLanguageCodeWhenTheExactLocaleIsMissing()
    {
        Assert.Equal("10.000", Load(new Locale("pt", "XX")).FormatDecimal(10000));
    }

    [Fact]
    public void FailsWhenNeitherTheLanguageCodeNorTheExactLocaleAreAvailable()
    {
        var locale = new Locale("xx", "XX");
        Assert.False(GlobalMaterialLocalizations.Delegate.IsSupported(locale));
        Assert.Throws<InvalidOperationException>(() => Load(locale));
    }

    [Fact]
    public void FormatHourFollowsTheLocalesHourFormat()
    {
        // h
        Assert.Equal("10", Load(new Locale("en", "US")).FormatHour(new TimeOfDay(10, 0)));
        Assert.Equal("8", Load(new Locale("en", "US")).FormatHour(new TimeOfDay(20, 0)));

        // HH
        Assert.Equal("09", Load(new Locale("de")).FormatHour(new TimeOfDay(9, 0)));
        Assert.Equal("20", Load(new Locale("de")).FormatHour(new TimeOfDay(20, 0)));
        Assert.Equal("09", Load(new Locale("en", "GB")).FormatHour(new TimeOfDay(9, 0)));
        Assert.Equal("20", Load(new Locale("en", "GB")).FormatHour(new TimeOfDay(20, 0)));

        // H
        Assert.Equal("9", Load(new Locale("es")).FormatHour(new TimeOfDay(9, 0)));
        Assert.Equal("20", Load(new Locale("es")).FormatHour(new TimeOfDay(20, 0)));
        Assert.Equal("۹", Load(new Locale("fa")).FormatHour(new TimeOfDay(9, 0)));
        Assert.Equal("۲۰", Load(new Locale("fa")).FormatHour(new TimeOfDay(20, 0)));
    }

    [Fact]
    public void FormatMinutePadsToTwoDigits()
    {
        Assert.Equal("32", Load(new Locale("en", "US")).FormatMinute(new TimeOfDay(1, 32)));
    }

    [Fact]
    public void FormatTimeOfDayFollowsTheLocalesTimeOfDayFormat()
    {
        // h_colon_mm_space_a
        Assert.Equal("9:32 AM", Load(new Locale("en")).FormatTimeOfDay(new TimeOfDay(9, 32)));
        Assert.Equal("8:32 PM", Load(new Locale("en")).FormatTimeOfDay(new TimeOfDay(20, 32)));

        // HH_colon_mm
        Assert.Equal("09:32", Load(new Locale("de")).FormatTimeOfDay(new TimeOfDay(9, 32)));
        Assert.Equal("09:32", Load(new Locale("en", "ZA")).FormatTimeOfDay(new TimeOfDay(9, 32)));

        // H_colon_mm
        Assert.Equal("9:32", Load(new Locale("es")).FormatTimeOfDay(new TimeOfDay(9, 32)));
        Assert.Equal("20:32", Load(new Locale("es")).FormatTimeOfDay(new TimeOfDay(20, 32)));
        Assert.Equal("9:32", Load(new Locale("ja")).FormatTimeOfDay(new TimeOfDay(9, 32)));
        Assert.Equal("20:32", Load(new Locale("ja")).FormatTimeOfDay(new TimeOfDay(20, 32)));

        // HH_dot_mm
        Assert.Equal("20.32", Load(new Locale("fi")).FormatTimeOfDay(new TimeOfDay(20, 32)));
        Assert.Equal("09.32", Load(new Locale("fi")).FormatTimeOfDay(new TimeOfDay(9, 32)));
        Assert.Equal("09.32", Load(new Locale("da")).FormatTimeOfDay(new TimeOfDay(9, 32)));

        // frenchCanadian
        Assert.Equal("09 h 32", Load(new Locale("fr", "CA")).FormatTimeOfDay(new TimeOfDay(9, 32)));

        // a_space_h_colon_mm
        Assert.Equal("上午 9:32", Load(new Locale("zh")).FormatTimeOfDay(new TimeOfDay(9, 32)));
        Assert.Equal("9:32 AM", Load(new Locale("ta")).FormatTimeOfDay(new TimeOfDay(9, 32)));
    }

    [Fact]
    public void AlwaysUse24HourFormatFoldsThe12HourFormatsOntoHhColonMm()
    {
        MaterialLocalizations english = Load(new Locale("en"));
        Assert.Equal(TimeOfDayFormat.HHColonMm, english.TimeOfDayFormat(alwaysUse24HourFormat: true));
        Assert.Equal("20:32", english.FormatTimeOfDay(new TimeOfDay(20, 32), alwaysUse24HourFormat: true));

        // A 24-hour format is returned unchanged.
        MaterialLocalizations finnish = Load(new Locale("fi"));
        Assert.Equal(TimeOfDayFormat.HHDotMm, finnish.TimeOfDayFormat(alwaysUse24HourFormat: true));
    }

    [Fact]
    public void DateFormattersFollowTheLocale()
    {
        var date = new DateTime(2018, 8, 1);

        MaterialLocalizations english = Load(new Locale("en"));
        Assert.Equal("2018", english.FormatYear(date));
        Assert.Equal("Wed, Aug 1", english.FormatMediumDate(date));
        Assert.Equal("Wednesday, August 1, 2018", english.FormatFullDate(date));
        Assert.Equal("August 2018", english.FormatMonthYear(date));

        MaterialLocalizations german = Load(new Locale("de"));
        Assert.Equal("2018", german.FormatYear(date));
        Assert.Equal("Mi., 1. Aug.", german.FormatMediumDate(date));
        Assert.Equal("Mittwoch, 1. August 2018", german.FormatFullDate(date));
        Assert.Equal("August 2018", german.FormatMonthYear(date));

        MaterialLocalizations serbian = Load(new Locale("sr"));
        Assert.Equal("2018.", serbian.FormatYear(date));
        Assert.Equal("сре 1. авг", serbian.FormatMediumDate(date));
        Assert.Equal(
            "среда, 1. август 2018.",
            serbian.FormatFullDate(date));
        Assert.Equal("август 2018.", serbian.FormatMonthYear(date));

        MaterialLocalizations serbianLatin = Load(new Locale("sr", null, "Latn"));
        Assert.Equal("2018.", serbianLatin.FormatYear(date));
        Assert.Equal("sre 1. avg", serbianLatin.FormatMediumDate(date));
        Assert.Equal("sreda, 1. avgust 2018.", serbianLatin.FormatFullDate(date));
        Assert.Equal("avgust 2018.", serbianLatin.FormatMonthYear(date));
    }

    [Fact]
    public void SpotCheckFormatMediumDateAndFormatFullDate()
    {
        var date = new DateTime(2015, 7, 23);

        MaterialLocalizations english = Load(new Locale("en"));
        Assert.Equal("Thu, Jul 23", english.FormatMediumDate(date));
        Assert.Equal("Thursday, July 23, 2015", english.FormatFullDate(date));

        MaterialLocalizations britishEnglish = Load(new Locale("en", "GB"));
        Assert.Equal("Thu 23 Jul", britishEnglish.FormatMediumDate(date));
        Assert.Equal("Thursday, 23 July 2015", britishEnglish.FormatFullDate(date));

        MaterialLocalizations spanish = Load(new Locale("es"));
        Assert.Equal("jue, 23 jul", spanish.FormatMediumDate(date));
        Assert.Equal("jueves, 23 de julio de 2015", spanish.FormatFullDate(date));

        MaterialLocalizations german = Load(new Locale("de"));
        Assert.Equal("Do., 23. Juli", german.FormatMediumDate(date));
        Assert.Equal("Donnerstag, 23. Juli 2015", german.FormatFullDate(date));

        MaterialLocalizations russian = Load(new Locale("ru"));
        Assert.Equal("чт, 23 июл.", russian.FormatMediumDate(date));

        // The space before 'г.' is a narrow no-break space (U+202F).
        Assert.Equal(
            "четверг, 23 июля 2015 г.",
            russian.FormatFullDate(date));
    }

    [Fact]
    public void SpotCheckTranslations()
    {
        MaterialLocalizations chinese = Load(new Locale("zh"));
        Assert.IsType<MaterialLocalizationZh>(chinese);
        Assert.Equal("第一页", chinese.FirstPageTooltip);
        Assert.Equal("最后一页", chinese.LastPageTooltip);

        MaterialLocalizations zulu = Load(new Locale("zu"));
        Assert.Equal("Ikhasi lokuqala", zulu.FirstPageTooltip);
        Assert.Equal("Ikhasi lokugcina", zulu.LastPageTooltip);

        MaterialLocalizations english = Load(new Locale("en"));
        Assert.IsType<MaterialLocalizationEn>(english);
        Assert.Equal("double tap to collapse", english.ExpansionTileExpandedHint);
        Assert.Equal("Share", english.ShareButtonLabel);
    }

    [Fact]
    public void SpotCheckSelectedRowCountTranslations()
    {
        MaterialLocalizations english = Load(new Locale("en"));
        Assert.Equal("No items selected", english.SelectedRowCountTitle(0));
        Assert.Equal("1 item selected", english.SelectedRowCountTitle(1));
        Assert.Equal("2 items selected", english.SelectedRowCountTitle(2));
        Assert.Equal("10,000 items selected", english.SelectedRowCountTitle(10000));
        Assert.Equal("123,456,789 items selected", english.SelectedRowCountTitle(123456789));

        MaterialLocalizations spanish = Load(new Locale("es"));
        Assert.Equal("No se han seleccionado elementos", spanish.SelectedRowCountTitle(0));
        Assert.Equal("1\u00A0elemento seleccionado", spanish.SelectedRowCountTitle(1));
        Assert.Equal("2\u00A0elementos seleccionados", spanish.SelectedRowCountTitle(2));
        Assert.Equal("10.000\u00A0elementos seleccionados", spanish.SelectedRowCountTitle(10000));
        Assert.Equal(
            "123.456.789\u00A0elementos seleccionados",
            spanish.SelectedRowCountTitle(123456789));

        // Romanian distinguishes `few` from `other`.
        MaterialLocalizations romanian = Load(new Locale("ro"));
        Assert.Equal("Nu există elemente selectate", romanian.SelectedRowCountTitle(0));
        Assert.Equal("Un articol selectat", romanian.SelectedRowCountTitle(1));
        Assert.Equal("2 articole selectate", romanian.SelectedRowCountTitle(2));
        Assert.Equal("29 de articole selectate", romanian.SelectedRowCountTitle(29));
        Assert.Equal("10.000 de articole selectate", romanian.SelectedRowCountTitle(10000));
        Assert.Equal("10.019 articole selectate", romanian.SelectedRowCountTitle(10019));
        Assert.Equal("123.456.789 de articole selectate", romanian.SelectedRowCountTitle(123456789));
    }

    [Fact]
    public void ChineseTranslationsSpotCheck()
    {
        MaterialLocalizations chinese = Load(new Locale("zh"));
        Assert.IsType<MaterialLocalizationZh>(chinese);
        Assert.Equal("提醒", chinese.AlertDialogLabel);
        Assert.Equal("上午", chinese.AnteMeridiemAbbreviation);
        Assert.Equal("关闭", chinese.CloseButtonLabel);
        Assert.Equal("确定", chinese.OkButtonLabel);
        Assert.Equal("查询", chinese.LookUpButtonLabel);

        MaterialLocalizations simplified = Load(new Locale("zh", null, "Hans"));
        Assert.IsType<MaterialLocalizationZhHans>(simplified);
        Assert.Equal("提醒", simplified.AlertDialogLabel);

        MaterialLocalizations traditional = Load(new Locale("zh", null, "Hant"));
        Assert.IsType<MaterialLocalizationZhHant>(traditional);
        Assert.Equal("通知", traditional.AlertDialogLabel);
        Assert.Equal("關閉", traditional.CloseButtonLabel);
        Assert.Equal("確定", traditional.OkButtonLabel);

        MaterialLocalizations taiwan = Load(new Locale("zh", "TW", "Hant"));
        Assert.IsType<MaterialLocalizationZhHantTw>(taiwan);
        Assert.Equal("警告", taiwan.AlertDialogLabel);

        MaterialLocalizations hongKong = Load(new Locale("zh", "HK", "Hant"));
        Assert.IsType<MaterialLocalizationZhHantHk>(hongKong);
        Assert.Equal("通知", hongKong.AlertDialogLabel);
    }

    [Fact]
    public void ChineseResolution()
    {
        Assert.IsType<MaterialLocalizationZh>(Load(new Locale("zh")));
        Assert.IsType<MaterialLocalizationZhHantTw>(Load(new Locale("zh", "TW")));
        Assert.IsType<MaterialLocalizationZhHantHk>(Load(new Locale("zh", "HK")));
        Assert.IsType<MaterialLocalizationZhHans>(Load(new Locale("zh", null, "Hans")));
        Assert.IsType<MaterialLocalizationZhHant>(Load(new Locale("zh", null, "Hant")));
        Assert.IsType<MaterialLocalizationZhHantTw>(Load(new Locale("zh", "TW", "Hant")));
        Assert.IsType<MaterialLocalizationZhHantHk>(Load(new Locale("zh", "HK", "Hant")));
    }

    [Fact]
    public void SerbianResolution()
    {
        Assert.IsType<MaterialLocalizationSr>(Load(new Locale("sr")));
        Assert.IsType<MaterialLocalizationSrCyrl>(Load(new Locale("sr", null, "Cyrl")));
        Assert.IsType<MaterialLocalizationSrLatn>(Load(new Locale("sr", null, "Latn")));
        Assert.IsType<MaterialLocalizationSr>(Load(new Locale("sr", "SR")));
        Assert.IsType<MaterialLocalizationSrCyrl>(Load(new Locale("sr", "SR", "Cyrl")));
        Assert.IsType<MaterialLocalizationSrLatn>(Load(new Locale("sr", "SR", "Latn")));
        Assert.IsType<MaterialLocalizationSrCyrl>(Load(new Locale("sr", "US", "Cyrl")));
        Assert.IsType<MaterialLocalizationSrLatn>(Load(new Locale("sr", "US", "Latn")));
        Assert.IsType<MaterialLocalizationSr>(Load(new Locale("sr", "US")));
    }

    [Fact]
    public void MiscResolution()
    {
        Assert.IsType<MaterialLocalizationEn>(Load(new Locale("en")));
        Assert.IsType<MaterialLocalizationEn>(Load(new Locale("en", null, "Cyrl")));
        Assert.IsType<MaterialLocalizationEn>(Load(new Locale("en", "US")));
        Assert.IsType<MaterialLocalizationEnAu>(Load(new Locale("en", "AU")));
        Assert.IsType<MaterialLocalizationEnGb>(Load(new Locale("en", "GB")));
        Assert.IsType<MaterialLocalizationEnSg>(Load(new Locale("en", "SG")));

        // An unlisted country falls back to the bare language bundle.
        Assert.IsType<MaterialLocalizationEn>(Load(new Locale("en", "MX")));
    }

    [Fact]
    public void FinnishTranslationForTabLabel()
    {
        MaterialLocalizations localizations = Load(new Locale("fi"));

        Assert.IsType<MaterialLocalizationFi>(localizations);
        Assert.Equal("Välilehti 1 kautta 2", localizations.TabLabel(1, 2));
    }

    [Fact]
    public void KoreanButtonLabels()
    {
        MaterialLocalizations localizations = Load(new Locale("ko"));

        Assert.IsType<MaterialLocalizationKo>(localizations);
        Assert.Equal("잘라내기", localizations.CutButtonLabel);
        Assert.Equal("복사", localizations.CopyButtonLabel);
        Assert.Equal("붙여넣기", localizations.PasteButtonLabel);
    }

    [Fact]
    public void ItalianDateHelpText()
    {
        MaterialLocalizations localizations = Load(new Locale("it"));

        Assert.IsType<MaterialLocalizationIt>(localizations);
        Assert.Equal("gg/mm/aaaa", localizations.DateHelpText);
    }

    [Fact]
    public void BasqueTimeOfDayFormat()
    {
        MaterialLocalizations localizations = Load(new Locale("eu"));

        Assert.IsType<MaterialLocalizationEu>(localizations);
        Assert.Equal(TimeOfDayFormat.HHColonMm, localizations.TimeOfDayFormat());
    }

    [Fact]
    public void NarrowWeekdaysAndFirstDayOfWeekComeFromTheLocale()
    {
        MaterialLocalizations english = Load(new Locale("en"));
        Assert.Equal(["S", "M", "T", "W", "T", "F", "S"], english.NarrowWeekdays);
        Assert.Equal(0, english.FirstDayOfWeekIndex);

        // Most of Europe starts the week on Monday.
        MaterialLocalizations german = Load(new Locale("de"));
        Assert.Equal(1, german.FirstDayOfWeekIndex);
        Assert.Equal(["S", "M", "D", "M", "D", "F", "S"], german.NarrowWeekdays);
    }

    [Fact]
    public void FormatCompactDateAndParseCompactDateRoundTrip()
    {
        MaterialLocalizations english = Load(new Locale("en"));
        Assert.Equal("7/23/2015", english.FormatCompactDate(new DateTime(2015, 7, 23)));
        Assert.Equal(new DateTime(2015, 7, 23), english.ParseCompactDate("7/23/2015"));

        MaterialLocalizations german = Load(new Locale("de"));
        Assert.Equal("23.7.2015", german.FormatCompactDate(new DateTime(2015, 7, 23)));
        Assert.Equal(new DateTime(2015, 7, 23), german.ParseCompactDate("23.7.2015"));
    }

    [Fact]
    public void ParseCompactDateRejectsInvalidText()
    {
        MaterialLocalizations english = Load(new Locale("en"));

        Assert.Null(english.ParseCompactDate(null));
        Assert.NotNull(english.ParseCompactDate("10/05/2023"));

        // Trailing garbage, an impossible day, and a non-date all fail rather than throw.
        Assert.Null(english.ParseCompactDate("10/05/2023666777889"));
        Assert.Null(english.ParseCompactDate("13/32/2023"));
        Assert.Null(english.ParseCompactDate("not a date"));
        Assert.Null(english.ParseCompactDate(string.Empty));
    }

    [Fact]
    public void PersianUsesNativeDigits()
    {
        MaterialLocalizations persian = Load(new Locale("fa"));

        Assert.Equal("۰۵", persian.FormatMinute(new TimeOfDay(1, 5)));
        Assert.Equal("۱۰", persian.FormatDecimal(10));

        // `ar`'s CLDR *number* symbols use ASCII digits even though its date symbols do not.
        Assert.Equal("05", Load(new Locale("ar")).FormatMinute(new TimeOfDay(1, 5)));
    }

    [Fact]
    public void RawPlaceholdersAreFilledIn()
    {
        MaterialLocalizations english = Load(new Locale("en"));

        Assert.Equal("About Plumix", english.AboutListTileTitle("Plumix"));
        Assert.Equal("Close Sheet", english.ScrimOnTapHint("Sheet"));
        Assert.Equal("Start date Jul 23", english.DateRangeStartDateSemanticLabel("Jul 23"));
        Assert.Equal("End date Jul 24", english.DateRangeEndDateSemanticLabel("Jul 24"));
        Assert.Equal("1–10 of about 100", english.PageRowsInfoTitle(1, 10, 100, true));
        Assert.Equal("1–10 of 100", english.PageRowsInfoTitle(1, 10, 100, false));
        Assert.Equal("Tab 2 of 5", english.TabLabel(2, 5));
        Assert.Equal("No licenses", english.LicensesPackageDetailText(0));
        Assert.Equal("1 license", english.LicensesPackageDetailText(1));
        Assert.Equal("2 licenses", english.LicensesPackageDetailText(2));
        Assert.Equal("No characters remaining", english.RemainingTextFieldCharacterCount(0));
        Assert.Equal("1 character remaining", english.RemainingTextFieldCharacterCount(1));
        Assert.Equal("10 characters remaining", english.RemainingTextFieldCharacterCount(10));
    }

    [Fact]
    public void ScriptCategoryComesFromTheBundle()
    {
        Assert.Equal(ScriptCategory.EnglishLike, Load(new Locale("en")).ScriptCategory);
        Assert.Equal(ScriptCategory.Dense, Load(new Locale("ja")).ScriptCategory);
        Assert.Equal(ScriptCategory.Tall, Load(new Locale("th")).ScriptCategory);
    }

    [Fact]
    public void UnsupportedLanguageIsNotSupported()
    {
        Assert.False(GlobalMaterialLocalizations.Delegate.IsSupported(new Locale("qaa")));
    }

    [Fact]
    public void DelegateIsCachedAndDescribesItself()
    {
        var locale = new Locale("de");
        Assert.Same(Load(locale), Load(new Locale("de")));
        Assert.False(GlobalMaterialLocalizations.Delegate.ShouldReload(
            GlobalMaterialLocalizations.Delegate));
        Assert.Equal(
            "GlobalMaterialLocalizations.delegate("
            + $"{GlobalMaterialLocalizations.MaterialSupportedLanguages.Count} locales)",
            GlobalMaterialLocalizations.Delegate.ToString());
    }

    [Fact]
    public void DelegatesCoverCupertinoMaterialAndWidgets()
    {
        Assert.Equal(
            [
                GlobalCupertinoLocalizations.Delegate,
                GlobalMaterialLocalizations.Delegate,
                GlobalWidgetsLocalizations.Delegate,
            ],
            GlobalMaterialLocalizations.Delegates);
    }
}
