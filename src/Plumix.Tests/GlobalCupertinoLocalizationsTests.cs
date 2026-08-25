using Plumix.Cupertino;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/l10n/translations_test.dart,
// cupertino_ui/test/l10n/date_picker_test.dart

public sealed class GlobalCupertinoLocalizationsTests
{
    public static TheoryData<string> SupportedLanguages()
    {
        var data = new TheoryData<string>();
        foreach (string language in GlobalCupertinoLocalizations.CupertinoSupportedLanguages)
        {
            data.Add(language);
        }

        return data;
    }

    private static CupertinoLocalizations Load(Locale locale) =>
        GlobalCupertinoLocalizations.Delegate.LoadTyped(locale);

    [Theory]
    [MemberData(nameof(SupportedLanguages))]
    public void TranslationsExistForEveryLanguage(string language)
    {
        var locale = new Locale(language);
        Assert.True(GlobalCupertinoLocalizations.Delegate.IsSupported(locale));

        CupertinoLocalizations localizations = Load(locale);

        foreach (int year in new[] { 0, 1, 2, 10 })
        {
            Assert.NotNull(localizations.DatePickerYear(year));
        }

        foreach (int month in new[] { 1, 2, 11, 12 })
        {
            Assert.NotNull(localizations.DatePickerMonth(month));
            Assert.NotNull(localizations.DatePickerStandaloneMonth(month));
        }

        foreach (int day in new[] { 0, 1, 2, 10 })
        {
            Assert.NotNull(localizations.DatePickerDayOfMonth(day));
        }

        Assert.NotNull(localizations.DatePickerDayOfMonth(0, 1));
        Assert.NotNull(localizations.DatePickerDayOfMonth(1, 2));
        Assert.NotNull(localizations.DatePickerDayOfMonth(2, 3));
        Assert.NotNull(localizations.DatePickerDayOfMonth(10, 4));

        Assert.NotNull(localizations.DatePickerMediumDate(new DateTime(2019, 3, 25)));

        foreach (int hour in new[] { 0, 1, 2, 10 })
        {
            Assert.NotNull(localizations.DatePickerHour(hour));
            string? label = localizations.DatePickerHourSemanticsLabel(hour);
            Assert.NotNull(label);
            Assert.DoesNotContain("$hour", label, StringComparison.Ordinal);
            Assert.NotNull(localizations.TimerPickerHour(hour));
            Assert.NotNull(localizations.TimerPickerHourLabel(hour));
        }

        foreach (int minute in new[] { 0, 1, 2, 10 })
        {
            Assert.NotNull(localizations.DatePickerMinute(minute));
            string? label = localizations.DatePickerMinuteSemanticsLabel(minute);
            Assert.NotNull(label);
            Assert.DoesNotContain("$minute", label, StringComparison.Ordinal);
            Assert.NotNull(localizations.TimerPickerMinute(minute));
            Assert.NotNull(localizations.TimerPickerMinuteLabel(minute));
            Assert.NotNull(localizations.TimerPickerSecond(minute));
            Assert.NotNull(localizations.TimerPickerSecondLabel(minute));
        }

        Assert.NotNull(localizations.AnteMeridiemAbbreviation);
        Assert.NotNull(localizations.PostMeridiemAbbreviation);
        Assert.NotNull(localizations.AlertDialogLabel);
        Assert.NotNull(localizations.CutButtonLabel);
        Assert.NotNull(localizations.CopyButtonLabel);
        Assert.NotNull(localizations.PasteButtonLabel);
        Assert.NotNull(localizations.SelectAllButtonLabel);

        string tabLabel = localizations.TabSemanticsLabel(2, 5);
        Assert.DoesNotContain("$tabIndex", tabLabel, StringComparison.Ordinal);
        Assert.DoesNotContain("$tabCount", tabLabel, StringComparison.Ordinal);
        Assert.Throws<ArgumentOutOfRangeException>(() => localizations.TabSemanticsLabel(0, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => localizations.TabSemanticsLabel(2, 0));
    }

    [Fact]
    public void SpotCheckFrench()
    {
        var locale = new Locale("fr");
        Assert.True(GlobalCupertinoLocalizations.Delegate.IsSupported(locale));

        CupertinoLocalizations localizations = Load(locale);

        Assert.IsType<CupertinoLocalizationFr>(localizations);
        Assert.Equal("Alerte", localizations.AlertDialogLabel);
        Assert.Equal("1 heure", localizations.DatePickerHourSemanticsLabel(1));
        Assert.Equal("12 heures", localizations.DatePickerHourSemanticsLabel(12));
        Assert.Equal("Coller", localizations.PasteButtonLabel);
        Assert.Equal(DatePickerDateOrder.Dmy, localizations.DatePickerDateOrder);
        Assert.Equal("s", localizations.TimerPickerSecondLabel(20));
        Assert.Equal("Tout sélectionner", localizations.SelectAllButtonLabel);
        Assert.Equal("10", localizations.TimerPickerMinute(10));
    }

    [Fact]
    public void SpotCheckChinese()
    {
        var locale = new Locale("zh");
        Assert.True(GlobalCupertinoLocalizations.Delegate.IsSupported(locale));

        CupertinoLocalizations localizations = Load(locale);

        Assert.IsType<CupertinoLocalizationZh>(localizations);
        Assert.Equal("提醒", localizations.AlertDialogLabel);
        Assert.Equal("1 点", localizations.DatePickerHourSemanticsLabel(1));
        Assert.Equal("12 点", localizations.DatePickerHourSemanticsLabel(12));
        Assert.Equal("粘贴", localizations.PasteButtonLabel);
        Assert.Equal(DatePickerDateOrder.Ymd, localizations.DatePickerDateOrder);
        Assert.Equal("秒", localizations.TimerPickerSecondLabel(20));
        Assert.Equal("全选", localizations.SelectAllButtonLabel);
        Assert.Equal("10", localizations.TimerPickerMinute(10));
        Assert.Equal("查询", localizations.LookUpButtonLabel);
    }

    [Fact]
    public void NorwegianBokmalMatchesNorwegian()
    {
        CupertinoLocalizations norwegian = Load(new Locale("no"));
        Assert.IsType<CupertinoLocalizationNo>(norwegian);

        CupertinoLocalizations bokmal = Load(new Locale("nb"));
        Assert.IsType<CupertinoLocalizationNb>(bokmal);

        Assert.Equal(norwegian.PasteButtonLabel, bokmal.PasteButtonLabel);
        Assert.Equal(norwegian.CopyButtonLabel, bokmal.CopyButtonLabel);
        Assert.Equal(norwegian.CutButtonLabel, bokmal.CutButtonLabel);
    }

    [Fact]
    public void FinnishTranslationForTabLabel()
    {
        CupertinoLocalizations localizations = Load(new Locale("fi"));

        Assert.IsType<CupertinoLocalizationFi>(localizations);
        Assert.Equal("Välilehti 1 kautta 2", localizations.TabSemanticsLabel(1, 2));
    }

    [Fact]
    public void RussianSpellCheckReplacementsLabel()
    {
        CupertinoLocalizations localizations = Load(new Locale("ru"));

        Assert.Equal("Варианты замены не найдены", localizations.NoSpellCheckReplacementsLabel);
    }

    [Fact]
    public void KoreanButtonLabelsAndDateTimeOrder()
    {
        CupertinoLocalizations localizations = Load(new Locale("ko"));

        Assert.IsType<CupertinoLocalizationKo>(localizations);
        Assert.Equal("잘라내기", localizations.CutButtonLabel);
        Assert.Equal("복사", localizations.CopyButtonLabel);
        Assert.Equal("붙여넣기", localizations.PasteButtonLabel);
        Assert.Equal(DatePickerDateTimeOrder.DateDayPeriodTime, localizations.DatePickerDateTimeOrder);
    }

    [Fact]
    public void DatePickerDayOfMonthUsesTheCurrentLocaleForWeekdays()
    {
        CupertinoLocalizations localizations = Load(new Locale("zh"));

        Assert.IsType<CupertinoLocalizationZh>(localizations);
        Assert.Equal("1日", localizations.DatePickerDayOfMonth(1));
        Assert.Equal("周二 1日", localizations.DatePickerDayOfMonth(1, 2));
    }

    [Fact]
    public void RussianMonthFormsFollowTheStandaloneDistinction()
    {
        CupertinoLocalizations localizations = Load(new Locale("ru", "RU"));

        // `monthYear` mode shows the standalone form, `date` mode the genitive one.
        Assert.Equal("Май", localizations.DatePickerStandaloneMonth(5));
        Assert.Equal("мая", localizations.DatePickerMonth(5));
    }

    [Fact]
    public void ArabicUsesNativeDigits()
    {
        CupertinoLocalizations localizations = Load(new Locale("ar"));

        Assert.Equal("٠٥", localizations.DatePickerMinute(5));
        Assert.Equal("١٠", localizations.DatePickerHour(10));
    }

    [Fact]
    public void SublocalesResolveThroughScriptAndCountryCodes()
    {
        Assert.IsType<CupertinoLocalizationZhHantHk>(Load(new Locale("zh", "HK")));
        Assert.IsType<CupertinoLocalizationZhHantTw>(Load(new Locale("zh", "TW")));
        Assert.IsType<CupertinoLocalizationZhHans>(Load(new Locale("zh", null, "Hans")));
        Assert.IsType<CupertinoLocalizationZhHant>(Load(new Locale("zh", null, "Hant")));
        Assert.IsType<CupertinoLocalizationSrLatn>(Load(new Locale("sr", null, "Latn")));
        Assert.IsType<CupertinoLocalizationDeCh>(Load(new Locale("de", "CH")));
        Assert.IsType<CupertinoLocalizationEsUs>(Load(new Locale("es", "US")));

        // An unlisted country falls back to the bare language bundle.
        Assert.IsType<CupertinoLocalizationDe>(Load(new Locale("de", "AT")));
    }

    [Fact]
    public void UnsupportedLanguageIsNotSupported()
    {
        Assert.False(GlobalCupertinoLocalizations.Delegate.IsSupported(new Locale("qaa")));
    }

    [Fact]
    public void DelegateIsCachedAndDescribesItself()
    {
        var locale = new Locale("de");
        Assert.Same(Load(locale), Load(new Locale("de")));
        Assert.False(GlobalCupertinoLocalizations.Delegate.ShouldReload(
            GlobalCupertinoLocalizations.Delegate));
        Assert.Equal(
            $"GlobalCupertinoLocalizations.delegate("
            + $"{GlobalCupertinoLocalizations.CupertinoSupportedLanguages.Count} locales)",
            GlobalCupertinoLocalizations.Delegate.ToString());
    }

    [Fact]
    public void DelegatesIncludeTheWidgetsDelegate()
    {
        Assert.Equal(
            [GlobalCupertinoLocalizations.Delegate, GlobalWidgetsLocalizations.Delegate],
            GlobalCupertinoLocalizations.Delegates);
    }

    [Fact]
    public void TimerPickerLabelListsDropTheAbsentPluralForms()
    {
        CupertinoLocalizations english = Load(new Locale("en"));

        // Dart lists every non-null plural form, so a locale whose `one` and `other` forms are the
        // same string lists it twice; `DefaultCupertinoLocalizations` lists it once.
        Assert.Equal(["hour", "hours"], english.TimerPickerHourLabels);
        Assert.Equal(["min.", "min."], english.TimerPickerMinuteLabels);
        Assert.Equal(["sec.", "sec."], english.TimerPickerSecondLabels);
    }
}
