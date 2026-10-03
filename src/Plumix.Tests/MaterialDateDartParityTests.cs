// Dart parity source: material_ui/lib/src/date.dart
// material-ui-src/test has no date_test.dart; these cover date.dart's documented contract (the doc
// examples on DateUtils, CalendarDelegate's default bodies and DateTimeRange's ==/hashCode/toString).

using Plumix.Foundation;
using Plumix.Material;
using Xunit;

namespace Plumix.Tests;

public sealed class MaterialDateDartParityTests
{
    [Fact]
    public void DateUtilsDateOnlyDropsTheTimeOfDay()
    {
        Assert.Equal(new DateTime(2024, 2, 29), DateUtils.DateOnly(new DateTime(2024, 2, 29, 17, 42, 1, 5)));

        var range = new DateTimeRange<DateTime>(
            start: new DateTime(2024, 2, 27, 8, 0, 0),
            end: new DateTime(2024, 2, 29, 23, 59, 59));
        Assert.Equal(
            new DateTimeRange<DateTime>(start: new DateTime(2024, 2, 27), end: new DateTime(2024, 2, 29)),
            DateUtils.DatesOnly(range));
    }

    [Fact]
    public void DateUtilsIsSameDayAndIsSameMonthTreatTwoNullsAsEqual()
    {
        var date = new DateTime(2024, 2, 29, 17, 42, 1);
        Assert.True(DateUtils.IsSameDay(date, new DateTime(2024, 2, 29)));
        Assert.False(DateUtils.IsSameDay(date, new DateTime(2024, 2, 28)));
        Assert.True(DateUtils.IsSameDay(null, null));
        Assert.False(DateUtils.IsSameDay(date, null));
        Assert.True(DateUtils.IsSameMonth(date, new DateTime(2024, 2, 1)));
        Assert.False(DateUtils.IsSameMonth(date, new DateTime(2023, 2, 1)));
        Assert.True(DateUtils.IsSameMonth(null, null));
        Assert.False(DateUtils.IsSameMonth(null, date));
    }

    [Fact]
    public void DateUtilsMonthDeltaMatchesTheDocExample()
    {
        Assert.Equal(7, DateUtils.MonthDelta(new DateTime(2019, 6, 15), new DateTime(2020, 1, 15)));
        Assert.Equal(-7, DateUtils.MonthDelta(new DateTime(2020, 1, 15), new DateTime(2019, 6, 15)));
    }

    [Fact]
    public void DateUtilsAddMonthsToMonthDateNormalizesLikeDartDateTime()
    {
        // The doc example: January 15, 2019 plus 3 months is April 1, 2019.
        Assert.Equal(new DateTime(2019, 4, 1), DateUtils.AddMonthsToMonthDate(new DateTime(2019, 1, 15), 3));
        // DateTime(2019, 13) is January 2020; DateTime(2019, 0) is December 2018.
        Assert.Equal(new DateTime(2020, 1, 1), DateUtils.AddMonthsToMonthDate(new DateTime(2019, 12, 31), 1));
        Assert.Equal(new DateTime(2018, 12, 1), DateUtils.AddMonthsToMonthDate(new DateTime(2019, 1, 31), -1));
    }

    [Fact]
    public void DateUtilsAddDaysToDateNormalizesLikeDartDateTime()
    {
        Assert.Equal(new DateTime(2024, 3, 1), DateUtils.AddDaysToDate(new DateTime(2024, 2, 29, 13, 0, 0), 1));
        Assert.Equal(new DateTime(2018, 12, 31), DateUtils.AddDaysToDate(new DateTime(2019, 1, 1), -1));
        Assert.Equal(new DateTime(2019, 1, 1), DateUtils.AddDaysToDate(new DateTime(2019, 1, 1, 23, 0, 0), 0));
    }

    [Fact]
    public void DateUtilsFirstDayOffsetMatchesTheDocExamples()
    {
        // September 1, 2017 is a Friday: 5 leading blanks for en_US, 4 for a Monday-first locale.
        Assert.Equal(5, DateUtils.FirstDayOffset(2017, 9, DefaultMaterialLocalizations.Instance));
        Assert.Equal(4, DateUtils.FirstDayOffset(2017, 9, new FirstDayLocalizations(1)));
        // A month that starts on the locale's first day of week has no leading blanks.
        Assert.Equal(0, DateUtils.FirstDayOffset(2024, 9, DefaultMaterialLocalizations.Instance));
        Assert.Equal(6, DateUtils.FirstDayOffset(2024, 9, new FirstDayLocalizations(1)));
        Assert.Equal(1, DateUtils.FirstDayOffset(2024, 9, new FirstDayLocalizations(6)));
    }

    [Fact]
    public void DateUtilsGetDaysInMonthAppliesTheGregorianLeapYearRules()
    {
        Assert.Equal(29, DateUtils.GetDaysInMonth(2024, 2));
        Assert.Equal(28, DateUtils.GetDaysInMonth(2023, 2));
        Assert.Equal(28, DateUtils.GetDaysInMonth(1900, 2));
        Assert.Equal(29, DateUtils.GetDaysInMonth(2000, 2));
        Assert.Equal(31, DateUtils.GetDaysInMonth(2023, 1));
        Assert.Equal(30, DateUtils.GetDaysInMonth(2023, 4));
        Assert.Equal(31, DateUtils.GetDaysInMonth(2023, 12));
    }

    [Fact]
    public void GregorianCalendarDelegateInheritsCalendarDelegateDefaults()
    {
        CalendarDelegate<DateTime> calendar = new GregorianCalendarDelegate();
        MaterialLocalizations localizations = DefaultMaterialLocalizations.Instance;

        Assert.True(calendar.IsSameDay(new DateTime(2024, 2, 29, 1, 0, 0), new DateTime(2024, 2, 29)));
        Assert.True(calendar.IsSameDay(null, null));
        Assert.False(calendar.IsSameDay(new DateTime(2024, 2, 29), null));
        Assert.True(calendar.IsSameMonth(new DateTime(2024, 2, 29), new DateTime(2024, 2, 1)));
        Assert.False(calendar.IsSameMonth(new DateTime(2024, 2, 29), new DateTime(2024, 3, 1)));
        Assert.Equal("2017", calendar.FormatYear(2017, localizations));
        Assert.Equal(
            new DateTimeRange<DateTime>(start: new DateTime(2024, 1, 1), end: new DateTime(2024, 1, 2)),
            calendar.DatesOnly(new DateTimeRange<DateTime>(
                start: new DateTime(2024, 1, 1, 10, 0, 0),
                end: new DateTime(2024, 1, 2, 10, 0, 0))));
    }

    [Fact]
    public void GregorianCalendarDelegateDelegatesToDateUtilsAndLocalizations()
    {
        var calendar = new GregorianCalendarDelegate();
        MaterialLocalizations localizations = DefaultMaterialLocalizations.Instance;
        var date = new DateTime(2024, 2, 29, 17, 42, 1);

        Assert.Equal(new DateTime(2024, 2, 29), calendar.DateOnly(date));
        Assert.Equal(7, calendar.MonthDelta(new DateTime(2019, 6, 15), new DateTime(2020, 1, 15)));
        Assert.Equal(new DateTime(2019, 4, 1), calendar.AddMonthsToMonthDate(new DateTime(2019, 1, 15), 3));
        Assert.Equal(new DateTime(2024, 3, 1), calendar.AddDaysToDate(date, 1));
        Assert.Equal(5, calendar.FirstDayOffset(2017, 9, localizations));
        Assert.Equal(29, calendar.GetDaysInMonth(2024, 2));
        Assert.Equal(new DateTime(2020, 1, 1), calendar.GetMonth(2019, 13));
        Assert.Equal(new DateTime(2024, 3, 1), calendar.GetDay(2024, 2, 30));
        Assert.Equal("February 2024", calendar.FormatMonthYear(date, localizations));
        Assert.Equal(localizations.FormatMediumDate(date), calendar.FormatMediumDate(date, localizations));
        Assert.Equal(localizations.FormatShortMonthDay(date), calendar.FormatShortMonthDay(date, localizations));
        Assert.Equal(localizations.FormatShortDate(date), calendar.FormatShortDate(date, localizations));
        Assert.Equal(localizations.FormatFullDate(date), calendar.FormatFullDate(date, localizations));
        Assert.Equal("02/29/2024", calendar.FormatCompactDate(date, localizations));
        Assert.Equal(new DateTime(2024, 2, 29), calendar.ParseCompactDate("02/29/2024", localizations));
        Assert.Null(calendar.ParseCompactDate("02/30/2024", localizations));
        Assert.Equal(localizations.DateHelpText, calendar.DateHelpText(localizations));
    }

    [Fact]
    public void DateTimeRangeDurationEqualityHashCodeAndToString()
    {
        var range = new DateTimeRange<DateTime>(start: new DateTime(2024, 2, 27), end: new DateTime(2024, 2, 29));
        var same = new DateTimeRange<DateTime>(start: new DateTime(2024, 2, 27), end: new DateTime(2024, 2, 29));
        var other = new DateTimeRange<DateTime>(start: new DateTime(2024, 2, 27), end: new DateTime(2024, 3, 1));

        Assert.Equal(TimeSpan.FromDays(2), range.Duration);
        Assert.True(range == same);
        Assert.False(range != same);
        Assert.Equal(range.GetHashCode(), same.GetHashCode());
        Assert.False(range == other);
        Assert.False(range.Equals(null));
        Assert.Equal("2024-02-27 00:00:00.000 - 2024-02-29 00:00:00.000", range.ToString());
        Assert.Equal(
            "2024-02-27 10:05:03.007 - 2024-02-27 10:05:03.007Z",
            new DateTimeRange<DateTime>(
                start: new DateTime(2024, 2, 27, 10, 5, 3, 7),
                end: new DateTime(2024, 2, 27, 10, 5, 3, 7, DateTimeKind.Utc)).ToString());

        // A single-day range is allowed.
        Assert.Equal(TimeSpan.Zero, new DateTimeRange<DateTime>(start: range.Start, end: range.Start).Duration);
    }

    [DebugOnlyFact]
    public void DateTimeRangeAssertsStartIsNotAfterEnd()
    {
        Assert.Throws<AssertionError>(() => new DateTimeRange<DateTime>(
            start: new DateTime(2024, 3, 1),
            end: new DateTime(2024, 2, 29)));
    }

    private sealed class FirstDayLocalizations(int firstDayOfWeekIndex) : DefaultMaterialLocalizations
    {
        public override int FirstDayOfWeekIndex => firstDayOfWeekIndex;
    }
}
