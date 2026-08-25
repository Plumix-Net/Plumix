using Plumix.Foundation.Intl;
using Xunit;

namespace Plumix.Tests;

// Covers Plumix's `package:intl` 0.20.3 subset (`src/Plumix/Foundation/Intl`). Every expectation
// below was taken from Dart by running the corresponding intl call against the pinned versions.
public sealed class IntlTests
{
    [Theory]
    [InlineData(0, 0, 0, -1, 11, 30, 2)]
    [InlineData(0, 0, 1, -1, 12, 1, 3)]
    [InlineData(1, 1, 7, 1, 1, 7, 7)]
    [InlineData(1, 1, 3, 1, 1, 3, 3)]
    [InlineData(2019, 3, 25, 2019, 3, 25, 1)]
    public void DartDateTimeNormalizesLikeDart(
        int year,
        int month,
        int day,
        int expectedYear,
        int expectedMonth,
        int expectedDay,
        int expectedWeekday)
    {
        DartDateTime value = DartDateTime.Utc(year, month, day);

        Assert.Equal(expectedYear, value.Year);
        Assert.Equal(expectedMonth, value.Month);
        Assert.Equal(expectedDay, value.Day);
        Assert.Equal(expectedWeekday, value.Weekday);
    }

    [Fact]
    public void DartDateTimeRollsOverTimeComponents()
    {
        DartDateTime value = DartDateTime.Utc(2019, 3, 25, 25, 61, 61, 1001);

        Assert.Equal(2019, value.Year);
        Assert.Equal(3, value.Month);
        Assert.Equal(26, value.Day);
        Assert.Equal(2, value.Hour);
        Assert.Equal(2, value.Minute);
        Assert.Equal(2, value.Second);
        Assert.Equal(1, value.Millisecond);
    }

    [Fact]
    public void DartDateTimeConvertsFromDateTime()
    {
        DartDateTime value = new DateTime(2019, 3, 25, 13, 45, 6, 7);

        Assert.Equal(2019, value.Year);
        Assert.Equal(13, value.Hour);
        Assert.Equal(7, value.Millisecond);
        Assert.Equal(1, value.Weekday);
    }

    [Theory]
    [InlineData("en_US", "2019", "0", "7", "Wed", "Mon, Mar 25", "05", "7", "07", "9")]
    [InlineData("ru", "2019", "0", "7", "ср", "пн, 25 мар.", "05", "7", "07", "9")]
    [InlineData("zh", "2019年", "0年", "7日", "周三", "3月25日周一", "05", "7", "07", "9")]
    [InlineData("fr", "2019", "0", "7", "mer.", "lun. 25 mars", "05", "7", "07", "9")]
    [InlineData("de", "2019", "0", "7", "Mi", "Mo., 25. März", "05", "7", "07", "9")]
    [InlineData("ja", "2019年", "0年", "7日", "水", "3月25日(月)", "05", "7", "07", "9")]
    [InlineData("ko", "2019년", "0년", "7일", "수", "3월 25일 (월)", "05", "7", "07", "9")]
    [InlineData(
        "fa", "۲۰۱۹", "۰", "۷", "چهارشنبه", "دوشنبه ۲۵ مارس", "۰۵", "۷", "۰۷", "۹")]
    [InlineData("hi", "2019", "0", "7", "बुध", "सोम, 25 मार्च", "05", "7", "07", "9")]
    public void DateFormatSkeletonsMatchDart(
        string locale,
        string year,
        string yearZero,
        string day,
        string weekday,
        string mediumDate,
        string hour,
        string minute,
        string paddedMinute,
        string second)
    {
        Assert.Equal(year, new DateFormat("y", locale).Format(DartDateTime.Utc(2019)));
        Assert.Equal(yearZero, new DateFormat("y", locale).Format(DartDateTime.Utc(0)));
        Assert.Equal(day, new DateFormat("d", locale).Format(DartDateTime.Utc(0, 0, 7)));
        Assert.Equal(weekday, new DateFormat("E", locale).Format(DartDateTime.Utc(1, 1, 3)));
        Assert.Equal(
            mediumDate,
            new DateFormat("MMMEd", locale).Format(DartDateTime.Utc(2019, 3, 25)));
        Assert.Equal(hour, new DateFormat("HH", locale).Format(DartDateTime.Utc(0, 0, 0, 5)));
        Assert.Equal(minute, new DateFormat("m", locale).Format(DartDateTime.Utc(0, 0, 0, 0, 7)));
        Assert.Equal(
            paddedMinute,
            new DateFormat("mm", locale).Format(DartDateTime.Utc(0, 0, 0, 0, 7)));
        Assert.Equal(
            second,
            new DateFormat("s", locale).Format(DartDateTime.Utc(0, 0, 0, 0, 0, 9)));
    }

    [Fact]
    public void DateFormatResolvesSkeletonsAgainstTheLocaleAndKeepsLiteralPatterns()
    {
        // 'd' is a skeleton in the pattern table; for zh it resolves to 'd日'.
        Assert.Equal("d日", new DateFormat("d", "zh").Pattern);

        // 'HH' is not a skeleton, so it stays a literal pattern in every locale.
        Assert.Equal("HH", new DateFormat("HH", "zh").Pattern);
    }

    [Fact]
    public void DateFormatFormatsQuotedLiteralsAndUnknownFields()
    {
        var format = new DateFormat("'at' HH':'mm", "en_US");

        Assert.Equal("at 05:07", format.Format(DartDateTime.Utc(0, 0, 0, 5, 7)));
    }

    [Fact]
    public void DateFormatFallsBackToTheDefaultLocaleWhenNoneIsGiven()
    {
        Assert.Equal("en_US", new DateFormat("y").Locale);
        Assert.Equal("en", new DateFormat("y", "en_CH").Locale);
        Assert.True(DateFormat.LocaleExists("en_GB"));
        Assert.False(DateFormat.LocaleExists("qaa"));
        Assert.Throws<ArgumentException>(() => new DateFormat("y", "qaa"));
    }

    [Fact]
    public void DateFormatExposesTheLocaleSymbols()
    {
        DateSymbols symbols = new DateFormat("y", "ru").DateSymbols;

        Assert.Equal("мая", symbols.Months[4]);
        Assert.Equal("май", symbols.StandaloneMonths[4]);
        Assert.Null(symbols.ZeroDigit);
        Assert.Equal("٠", new DateFormat("y", "ar").DateSymbols.ZeroDigit);
    }

    [Theory]
    [InlineData("en_US", "1,234,567", "5")]
    [InlineData("ru", "1\u00a0234\u00a0567", "5")]
    [InlineData("de", "1.234.567", "5")]
    [InlineData("hi", "12,34,567", "5")]
    [InlineData("fa", "۱٬۲۳۴٬۵۶۷", "۵")]
    public void NumberFormatDecimalPatternMatchesDart(string locale, string grouped, string single)
    {
        NumberFormat format = NumberFormat.DecimalPattern(locale);

        Assert.Equal(grouped, format.Format(1234567));
        Assert.Equal(single, format.Format(5));
    }

    [Fact]
    public void NumberFormatFallsBackAndFormatsNegatives()
    {
        Assert.Equal("en_US", NumberFormat.DecimalPattern().Locale);
        Assert.Equal("-1,000", NumberFormat.DecimalPattern("en_US").Format(-1000));
        Assert.Equal("0", NumberFormat.DecimalPattern("en_US").Format(0));
    }

    [Theory]
    [InlineData("en", "other one other other other other other other")]
    [InlineData("ru", "many one few few many many one many")]
    [InlineData("ar", "zero one two few few many many other")]
    [InlineData("cy", "zero one two few other other other other")]
    [InlineData("fr", "one one other other other other other other")]
    [InlineData("ja", "other other other other other other other other")]
    [InlineData("lv", "zero one other other other zero one zero")]
    [InlineData("pl", "many one few few many many many many")]
    public void PluralRulesMatchDart(string locale, string expected)
    {
        int[] counts = [0, 1, 2, 3, 5, 11, 21, 100];
        string[] cases = counts
            .Select(count => PluralRules.Select(locale, count).ToString().ToLowerInvariant())
            .ToArray();

        Assert.Equal(expected, string.Join(' ', cases));
    }

    [Fact]
    public void PluralLogicPrefersTheExplicitNumberCases()
    {
        // Russian's rule maps 0 to `many` and 2 to `few`, but an explicit case wins.
        Assert.Equal("zero", Plural(0, "ru"));
        Assert.Equal("one", Plural(1, "ru"));
        Assert.Equal("two", Plural(2, "ru"));
        Assert.Equal("few", Plural(3, "ru"));
        Assert.Equal("many", Plural(5, "ru"));
    }

    [Fact]
    public void PluralLogicFallsBackToOtherForAbsentForms()
    {
        Assert.Equal(
            "other",
            Intl.PluralLogic(5, few: null, many: null, other: "other", locale: "ru"));

        // Dart's `two` falls back to `few` before `other`.
        Assert.Equal(
            "few",
            Intl.PluralLogic(2, two: null, few: "few", other: "other", locale: "ar"));

        // An unknown locale uses the default rule, which is always `other`.
        Assert.Equal("other", Intl.PluralLogic(1, other: "other", locale: "qaa"));
    }

    [Theory]
    [InlineData("zh_Hant_HK", "zh_Hant_HK")]
    [InlineData("en-us", "en_US")]
    [InlineData("de", "de")]
    [InlineData("C", "en_ISO")]
    [InlineData(null, "en_US")]
    public void CanonicalizedLocaleMatchesDart(string? locale, string expected)
    {
        Assert.Equal(expected, Intl.CanonicalizedLocale(locale));
    }

    [Fact]
    public void VerifiedLocaleFallsBackThroughDartsChain()
    {
        string[] known = ["en", "he", "zh_TW"];
        bool Exists(string locale) => known.Contains(locale);

        Assert.Equal("en", Intl.VerifiedLocale("en", Exists, null));
        Assert.Equal("en", Intl.VerifiedLocale("en_CA", Exists, null));
        Assert.Equal("zh_TW", Intl.VerifiedLocale("zh-tw", Exists, null));

        // `iw` is the deprecated code for Hebrew.
        Assert.Equal("he", Intl.VerifiedLocale("iw", Exists, null));
        Assert.Equal("missing", Intl.VerifiedLocale("qaa", Exists, _ => "missing"));
        Assert.Throws<ArgumentException>(() => Intl.VerifiedLocale("qaa", Exists, null));
    }

    [Fact]
    public void ToBeginningOfSentenceCaseMatchesDart()
    {
        Assert.Equal("Мая", Intl.ToBeginningOfSentenceCase("мая"));
        Assert.Equal("İstanbul", Intl.ToBeginningOfSentenceCase("istanbul", "tr"));
        Assert.Equal("Istanbul", Intl.ToBeginningOfSentenceCase("istanbul"));
        Assert.Equal("", Intl.ToBeginningOfSentenceCase(""));
        Assert.Null(Intl.ToBeginningOfSentenceCase(null));
    }

    private static string? Plural(int howMany, string locale) => Intl.PluralLogic(
        howMany,
        zero: "zero",
        one: "one",
        two: "two",
        few: "few",
        many: "many",
        other: "other",
        locale: locale);
}
