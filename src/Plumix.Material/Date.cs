using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/date.dart

public delegate bool SelectableDayPredicate(DateTime day);

public enum DatePickerMode
{
    Day,
    Year,
}

public enum DatePickerEntryMode
{
    Calendar,
    Input,
    CalendarOnly,
    InputOnly,
}

public sealed record DateTimeRange<T> where T : struct, IComparable<T>
{
    public DateTimeRange(T start, T end)
    {
        if (start.CompareTo(end) > 0)
        {
            throw new ArgumentException("Start must be on or before end.");
        }

        Start = start;
        End = end;
    }

    public T Start { get; }
    public T End { get; }

    public TimeSpan Duration => Start is DateTime start && End is DateTime end
        ? end - start
        : throw new NotSupportedException("Duration is available for DateTime-backed ranges.");
}

public abstract class CalendarDelegate<T> where T : struct, IComparable<T>
{
    public abstract T Now();
    public abstract T DateOnly(T date);

    public virtual DateTimeRange<T> DatesOnly(DateTimeRange<T> range) =>
        new(DateOnly(range.Start), DateOnly(range.End));

    public abstract bool IsSameDay(T? dateA, T? dateB);
    public abstract bool IsSameMonth(T? dateA, T? dateB);
    public abstract int MonthDelta(T startDate, T endDate);
    public abstract T AddMonthsToMonthDate(T monthDate, int monthsToAdd);
    public abstract T AddDaysToDate(T date, int days);
    public abstract int FirstDayOffset(int year, int month, MaterialLocalizations localizations);
    public abstract int GetDaysInMonth(int year, int month);
    public abstract T GetMonth(int year, int month);
    public abstract T GetDay(int year, int month, int day);
    public abstract string FormatMonthYear(T date, MaterialLocalizations localizations);
    public abstract string FormatYear(int year, MaterialLocalizations localizations);
    public abstract string FormatMediumDate(T date, MaterialLocalizations localizations);
    public abstract string FormatShortMonthDay(T date, MaterialLocalizations localizations);
    public abstract string FormatShortDate(T date, MaterialLocalizations localizations);
    public abstract string FormatFullDate(T date, MaterialLocalizations localizations);
    public abstract string FormatCompactDate(T date, MaterialLocalizations localizations);
    public abstract T? ParseCompactDate(string? input, MaterialLocalizations localizations);
    public abstract string DateHelpText(MaterialLocalizations localizations);
}

public sealed class GregorianCalendarDelegate : CalendarDelegate<DateTime>
{
    public static GregorianCalendarDelegate Instance { get; } = new();

    public override DateTime Now() => DateTime.Now;
    public override DateTime DateOnly(DateTime date) => DateUtils.DateOnly(date);
    public override bool IsSameDay(DateTime? dateA, DateTime? dateB) => DateUtils.IsSameDay(dateA, dateB);
    public override bool IsSameMonth(DateTime? dateA, DateTime? dateB) => DateUtils.IsSameMonth(dateA, dateB);
    public override int MonthDelta(DateTime startDate, DateTime endDate) => DateUtils.MonthDelta(startDate, endDate);
    public override DateTime AddMonthsToMonthDate(DateTime monthDate, int monthsToAdd) =>
        DateUtils.AddMonthsToMonthDate(monthDate, monthsToAdd);
    public override DateTime AddDaysToDate(DateTime date, int days) => DateUtils.AddDaysToDate(date, days);
    public override int FirstDayOffset(int year, int month, MaterialLocalizations localizations) =>
        DateUtils.FirstDayOffset(year, month, localizations);
    public override int GetDaysInMonth(int year, int month) => DateUtils.GetDaysInMonth(year, month);
    public override DateTime GetMonth(int year, int month) => new(year, month, 1);
    public override DateTime GetDay(int year, int month, int day) => new(year, month, day);
    public override string FormatMonthYear(DateTime date, MaterialLocalizations localizations) =>
        localizations.FormatMonthYear(date);
    public override string FormatYear(int year, MaterialLocalizations localizations) =>
        localizations.FormatYear(new DateTime(year, 1, 1));
    public override string FormatMediumDate(DateTime date, MaterialLocalizations localizations) =>
        localizations.FormatMediumDate(date);
    public override string FormatShortMonthDay(DateTime date, MaterialLocalizations localizations) =>
        localizations.FormatShortMonthDay(date);
    public override string FormatShortDate(DateTime date, MaterialLocalizations localizations) =>
        localizations.FormatShortDate(date);
    public override string FormatFullDate(DateTime date, MaterialLocalizations localizations) =>
        localizations.FormatFullDate(date);
    public override string FormatCompactDate(DateTime date, MaterialLocalizations localizations) =>
        localizations.FormatCompactDate(date);
    public override DateTime? ParseCompactDate(string? input, MaterialLocalizations localizations) =>
        localizations.ParseCompactDate(input);
    public override string DateHelpText(MaterialLocalizations localizations) => localizations.DateHelpText;
}

public static class DateUtils
{
    public static DateTime DateOnly(DateTime date) => new(date.Year, date.Month, date.Day);

    public static DateTimeRange<DateTime> DatesOnly(DateTimeRange<DateTime> range) =>
        new(DateOnly(range.Start), DateOnly(range.End));

    public static bool IsSameDay(DateTime? dateA, DateTime? dateB) =>
        dateA?.Year == dateB?.Year && dateA?.Month == dateB?.Month && dateA?.Day == dateB?.Day;

    public static bool IsSameMonth(DateTime? dateA, DateTime? dateB) =>
        dateA?.Year == dateB?.Year && dateA?.Month == dateB?.Month;

    public static int MonthDelta(DateTime startDate, DateTime endDate) =>
        ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;

    public static DateTime AddMonthsToMonthDate(DateTime monthDate, int monthsToAdd) =>
        new DateTime(monthDate.Year, monthDate.Month, 1).AddMonths(monthsToAdd);

    public static DateTime AddDaysToDate(DateTime date, int days) => DateOnly(date).AddDays(days);

    public static int FirstDayOffset(int year, int month, MaterialLocalizations localizations)
    {
        var weekdayFromSunday = (int)new DateTime(year, month, 1).DayOfWeek;
        return ((weekdayFromSunday - localizations.FirstDayOfWeekIndex) % 7 + 7) % 7;
    }

    public static int GetDaysInMonth(int year, int month) => DateTime.DaysInMonth(year, month);
}
