using System.Globalization;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/date.dart

/// <summary>
/// Signature for predicating dates for enabled date selections within a range (date_picker.dart's
/// <c>SelectableDayForRangePredicate</c>).
/// </summary>
public delegate bool SelectableDayForRangePredicate(DateTime day, DateTime? selectedStartDay, DateTime? selectedEndDay);

/// <summary>
/// Controls the calendar system used in the date picker.
/// </summary>
/// <remarks>
/// A <see cref="CalendarDelegate{T}"/> defines how dates are interpreted, formatted, and navigated
/// within the picker. Different calendar systems (e.g., Gregorian, Nepali, Hijri, Buddhist) can be
/// supported by providing custom implementations.
/// <para>
/// Dart bounds <c>T</c> by <c>DateTime</c> (<c>T extends DateTime</c>). C#'s <see cref="DateTime"/> is a
/// sealed struct, so <c>T</c> is any comparable struct and the members Dart implements in terms of
/// <c>DateTime</c> (<see cref="IsSameDay"/>, <see cref="IsSameMonth"/>) read a <see cref="DateTime"/>-backed
/// <c>T</c>; a delegate over another date type overrides them.
/// </para>
/// </remarks>
public abstract class CalendarDelegate<T> where T : struct, IComparable<T>
{
    /// <summary>Creates a calendar delegate.</summary>
    protected CalendarDelegate()
    {
    }

    /// <summary>Returns a date representing the current date and time.</summary>
    public abstract T Now();

    /// <summary>Returns a date with the date of the original, but time set to midnight.</summary>
    public abstract T DateOnly(T date);

    /// <summary>
    /// Returns a <see cref="DateTimeRange{T}"/> with the dates of the original, but with times set to
    /// midnight.
    /// </summary>
    public virtual DateTimeRange<T> DatesOnly(DateTimeRange<T> range)
    {
        return new DateTimeRange<T>(start: DateOnly(range.Start), end: DateOnly(range.End));
    }

    /// <summary>
    /// Returns true if the two dates have the same day, month, and year, or are both null.
    /// </summary>
    public virtual bool IsSameDay(T? dateA, T? dateB)
    {
        DateTime? a = AsDateTime(dateA);
        DateTime? b = AsDateTime(dateB);
        return a?.Year == b?.Year && a?.Month == b?.Month && a?.Day == b?.Day;
    }

    /// <summary>
    /// Returns true if the two dates have the same month and year, or are both null.
    /// </summary>
    public virtual bool IsSameMonth(T? dateA, T? dateB)
    {
        DateTime? a = AsDateTime(dateA);
        DateTime? b = AsDateTime(dateB);
        return a?.Year == b?.Year && a?.Month == b?.Month;
    }

    /// <summary>Determines the number of months between two dates.</summary>
    public abstract int MonthDelta(T startDate, T endDate);

    /// <summary>
    /// Returns a date that is <paramref name="monthDate"/> with the added number of months and the day
    /// set to 1 and time set to midnight.
    /// </summary>
    public abstract T AddMonthsToMonthDate(T monthDate, int monthsToAdd);

    /// <summary>Returns a date with the added number of days and time set to midnight.</summary>
    public abstract T AddDaysToDate(T date, int days);

    /// <summary>
    /// Computes the offset from the first day of the week that the first day of the
    /// <paramref name="month"/> falls on.
    /// </summary>
    public abstract int FirstDayOffset(int year, int month, MaterialLocalizations localizations);

    /// <summary>Returns the number of days in a month, according to the calendar system.</summary>
    public abstract int GetDaysInMonth(int year, int month);

    /// <summary>Returns a date with the given <paramref name="year"/> and <paramref name="month"/>.</summary>
    public abstract T GetMonth(int year, int month);

    /// <summary>
    /// Returns a date with the given <paramref name="year"/>, <paramref name="month"/>, and
    /// <paramref name="day"/>.
    /// </summary>
    public abstract T GetDay(int year, int month, int day);

    /// <summary>Formats the month and the year of the given <paramref name="date"/>.</summary>
    /// <remarks>
    /// The returned string does not contain the day of the month. This appears in the date picker
    /// invoked using <c>ShowDatePicker</c>.
    /// </remarks>
    public abstract string FormatMonthYear(T date, MaterialLocalizations localizations);

    /// <summary>Full unabbreviated year format, e.g. 2017 rather than 17.</summary>
    public virtual string FormatYear(int year, MaterialLocalizations localizations)
    {
        return localizations.FormatYear(DateUtils.DateTimeOf(year));
    }

    /// <summary>Formats the date using a medium-width format.</summary>
    /// <remarks>
    /// Abbreviates month and days of week. This appears in the header of the date picker invoked using
    /// <c>ShowDatePicker</c>. Examples: US English: Wed, Sep 27; Russian: ср, сент. 27.
    /// </remarks>
    public abstract string FormatMediumDate(T date, MaterialLocalizations localizations);

    /// <summary>Formats the month and day of the given <paramref name="date"/>.</summary>
    /// <remarks>Examples: US English: Feb 21; Russian: 21 февр.</remarks>
    public abstract string FormatShortMonthDay(T date, MaterialLocalizations localizations);

    /// <summary>Formats the date using a short-width format.</summary>
    /// <remarks>
    /// Includes the abbreviation of the month, the day and year. Examples: US English: Feb 21, 2019;
    /// Russian: 21 февр. 2019 г.
    /// </remarks>
    public abstract string FormatShortDate(T date, MaterialLocalizations localizations);

    /// <summary>Formats day of week, month, day of month and year in a long-width format.</summary>
    /// <remarks>
    /// Does not abbreviate names. Appears in spoken announcements of the date picker invoked using
    /// <c>ShowDatePicker</c>, when accessibility mode is on.
    /// </remarks>
    public abstract string FormatFullDate(T date, MaterialLocalizations localizations);

    /// <summary>Formats the date in a compact format.</summary>
    /// <remarks>
    /// Usually just the numeric values for the for day, month and year are used. Examples: US English:
    /// 02/21/2019; Russian: 21.02.2019. See also <see cref="ParseCompactDate"/>.
    /// </remarks>
    public abstract string FormatCompactDate(T date, MaterialLocalizations localizations);

    /// <summary>Converts the given compact date formatted string into a date.</summary>
    /// <remarks>
    /// The format of the string must be a valid compact date format for the given locale. If the text
    /// doesn't represent a valid date, <c>null</c> will be returned.
    /// </remarks>
    public abstract T? ParseCompactDate(string? inputString, MaterialLocalizations localizations);

    /// <summary>
    /// The help text used on an empty <see cref="InputDatePickerFormField"/> to indicate to the user
    /// the date format being asked for.
    /// </summary>
    public abstract string DateHelpText(MaterialLocalizations localizations);

    // Dart's `T extends DateTime` makes every `T` a `DateTime`; see the class remarks.
    private static DateTime? AsDateTime(T? date) => date is { } value ? (DateTime)(object)value : null;
}

/// <summary>A <see cref="CalendarDelegate{T}"/> implementation for the Gregorian calendar system.</summary>
/// <remarks>
/// The Gregorian calendar is the most widely used civil calendar worldwide. This delegate provides
/// standard date interpretation, formatting, and navigation based on the Gregorian system. It is the
/// default calendar system for <see cref="CalendarDatePicker"/>.
/// </remarks>
public class GregorianCalendarDelegate : CalendarDelegate<DateTime>
{
    /// <summary>
    /// Creates a calendar delegate that uses the Gregorian calendar and the conventions of the current
    /// <see cref="MaterialLocalizations"/>.
    /// </summary>
    public GregorianCalendarDelegate()
    {
    }

    /// <summary>
    /// The canonical instance: Dart's <c>const GregorianCalendarDelegate()</c>, which every default
    /// <c>calendarDelegate</c> parameter shares.
    /// </summary>
    public static GregorianCalendarDelegate Instance { get; } = new();

    public override DateTime Now() => DateTime.Now;

    public override DateTime DateOnly(DateTime date) => DateUtils.DateOnly(date);

    public override int MonthDelta(DateTime startDate, DateTime endDate) => DateUtils.MonthDelta(startDate, endDate);

    public override DateTime AddMonthsToMonthDate(DateTime monthDate, int monthsToAdd)
    {
        return DateUtils.AddMonthsToMonthDate(monthDate, monthsToAdd);
    }

    public override DateTime AddDaysToDate(DateTime date, int days) => DateUtils.AddDaysToDate(date, days);

    public override int FirstDayOffset(int year, int month, MaterialLocalizations localizations)
    {
        return DateUtils.FirstDayOffset(year, month, localizations);
    }

    /// <summary>
    /// Returns the number of days in a month, according to the proleptic Gregorian calendar.
    /// </summary>
    public override int GetDaysInMonth(int year, int month) => DateUtils.GetDaysInMonth(year, month);

    public override DateTime GetMonth(int year, int month) => DateUtils.DateTimeOf(year, month);

    public override DateTime GetDay(int year, int month, int day) => DateUtils.DateTimeOf(year, month, day);

    public override string FormatMonthYear(DateTime date, MaterialLocalizations localizations)
    {
        return localizations.FormatMonthYear(date);
    }

    public override string FormatMediumDate(DateTime date, MaterialLocalizations localizations)
    {
        return localizations.FormatMediumDate(date);
    }

    public override string FormatShortMonthDay(DateTime date, MaterialLocalizations localizations)
    {
        return localizations.FormatShortMonthDay(date);
    }

    public override string FormatShortDate(DateTime date, MaterialLocalizations localizations)
    {
        return localizations.FormatShortDate(date);
    }

    public override string FormatFullDate(DateTime date, MaterialLocalizations localizations)
    {
        return localizations.FormatFullDate(date);
    }

    public override string FormatCompactDate(DateTime date, MaterialLocalizations localizations)
    {
        return localizations.FormatCompactDate(date);
    }

    public override DateTime? ParseCompactDate(string? inputString, MaterialLocalizations localizations)
    {
        return localizations.ParseCompactDate(inputString);
    }

    public override string DateHelpText(MaterialLocalizations localizations)
    {
        return localizations.DateHelpText;
    }
}

/// <summary>Utility functions for working with dates.</summary>
public static class DateUtils
{
    /// <summary>Returns a date with the date of the original, but time set to midnight.</summary>
    public static DateTime DateOnly(DateTime date)
    {
        return DateTimeOf(date.Year, date.Month, date.Day);
    }

    /// <summary>
    /// Returns a <see cref="DateTimeRange{T}"/> with the dates of the original, but with times set to
    /// midnight. See also <see cref="DateOnly"/>, which does the same thing for a single date.
    /// </summary>
    public static DateTimeRange<DateTime> DatesOnly(DateTimeRange<DateTime> range)
    {
        return new DateTimeRange<DateTime>(start: DateOnly(range.Start), end: DateOnly(range.End));
    }

    /// <summary>
    /// Returns true if the two dates have the same day, month, and year, or are both null.
    /// </summary>
    public static bool IsSameDay(DateTime? dateA, DateTime? dateB)
    {
        return dateA?.Year == dateB?.Year && dateA?.Month == dateB?.Month && dateA?.Day == dateB?.Day;
    }

    /// <summary>Returns true if the two dates have the same month and year, or are both null.</summary>
    public static bool IsSameMonth(DateTime? dateA, DateTime? dateB)
    {
        return dateA?.Year == dateB?.Year && dateA?.Month == dateB?.Month;
    }

    /// <summary>Determines the number of months between two dates.</summary>
    /// <remarks>
    /// For example, the delta between <c>new DateTime(2019, 6, 15)</c> and <c>new DateTime(2020, 1, 15)</c>
    /// is <c>7</c>.
    /// </remarks>
    public static int MonthDelta(DateTime startDate, DateTime endDate)
    {
        return (endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month;
    }

    /// <summary>
    /// Returns a date that is <paramref name="monthDate"/> with the added number of months and the day
    /// set to 1 and time set to midnight.
    /// </summary>
    /// <remarks>
    /// For example, adding 3 months to January 15, 2019 gives April 1, 2019.
    /// </remarks>
    public static DateTime AddMonthsToMonthDate(DateTime monthDate, int monthsToAdd)
    {
        return DateTimeOf(monthDate.Year, monthDate.Month + monthsToAdd);
    }

    /// <summary>Returns a date with the added number of days and time set to midnight.</summary>
    public static DateTime AddDaysToDate(DateTime date, int days)
    {
        return DateTimeOf(date.Year, date.Month, date.Day + days);
    }

    /// <summary>
    /// Computes the offset from the first day of the week that the first day of the
    /// <paramref name="month"/> falls on.
    /// </summary>
    /// <remarks>
    /// For example, September 1, 2017 falls on a Friday, which in the calendar localized for United
    /// States English appears as <c>S M T W T F S / _ _ _ _ _ 1 2</c>: the offset is the number of
    /// leading blanks, i.e. 5. The Russian calendar starts the week on Monday
    /// (<c>M T W T F S S / _ _ _ _ 1 2 3</c>), so its offset is 4.
    /// <para>
    /// This consolidates <c>DateTime.weekday</c> (a 1-based index with 1 falling on Monday),
    /// <see cref="MaterialLocalizations.FirstDayOfWeekIndex"/> (a 0-based index into
    /// <see cref="MaterialLocalizations.NarrowWeekdays"/>) and that list, which always starts with
    /// Sunday and ends with Saturday.
    /// </para>
    /// </remarks>
    public static int FirstDayOffset(int year, int month, MaterialLocalizations localizations)
    {
        // 0-based day of week for the month and year, with 0 representing Monday.
        int weekdayFromMonday = DartWeekday(DateTimeOf(year, month)) - 1;

        // 0-based start of week depending on the locale, with 0 representing Sunday.
        int firstDayOfWeekIndex = localizations.FirstDayOfWeekIndex;

        // firstDayOfWeekIndex recomputed to be Monday-based, in order to compare with
        // weekdayFromMonday.
        firstDayOfWeekIndex = DartModulo(firstDayOfWeekIndex - 1, 7);

        // Number of days between the first day of week appearing on the calendar,
        // and the day corresponding to the first of the month.
        return DartModulo(weekdayFromMonday - firstDayOfWeekIndex, 7);
    }

    /// <summary>
    /// Returns the number of days in a month, according to the proleptic Gregorian calendar.
    /// </summary>
    /// <remarks>
    /// This applies the leap year logic introduced by the Gregorian reforms of 1582. It will not give
    /// valid results for dates prior to that time.
    /// </remarks>
    public static int GetDaysInMonth(int year, int month)
    {
        if (month == February)
        {
            bool isLeapYear = (year % 4 == 0) && (year % 100 != 0) || (year % 400 == 0);
            return isLeapYear ? 29 : 28;
        }

        int[] daysInMonth = [31, -1, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
        return daysInMonth[month - 1];
    }

    // Dart's `DateTime.february`.
    private const int February = 2;

    /// <summary>
    /// dart:core's <c>DateTime(year, [month = 1, day = 1])</c>: out-of-range months and days carry
    /// into the neighbouring years and months (<c>DateTime(2019, 13)</c> is January 2020,
    /// <c>DateTime(2019, 1, 0)</c> is December 31, 2018), where C#'s constructor throws.
    /// </summary>
    internal static DateTime DateTimeOf(int year, int month = 1, int day = 1)
    {
        return new DateTime(year, 1, 1).AddMonths(month - 1).AddDays(day - 1);
    }

    /// <summary>dart:core's <c>DateTime.toString</c>: <c>2019-06-15 00:00:00.000</c>.</summary>
    internal static string DartToString(DateTime date)
    {
        string y = date.Year.ToString("0000", CultureInfo.InvariantCulture);
        string m = date.Month.ToString("00", CultureInfo.InvariantCulture);
        string d = date.Day.ToString("00", CultureInfo.InvariantCulture);
        string h = date.Hour.ToString("00", CultureInfo.InvariantCulture);
        string min = date.Minute.ToString("00", CultureInfo.InvariantCulture);
        string sec = date.Second.ToString("00", CultureInfo.InvariantCulture);
        string ms = date.Millisecond.ToString("000", CultureInfo.InvariantCulture);
        int microsecond = date.Microsecond;
        string us = microsecond == 0 ? string.Empty : microsecond.ToString("000", CultureInfo.InvariantCulture);
        string utc = date.Kind == DateTimeKind.Utc ? "Z" : string.Empty;
        return $"{y}-{m}-{d} {h}:{min}:{sec}.{ms}{us}{utc}";
    }

    // dart:core's `DateTime.weekday`: 1 for Monday through 7 for Sunday.
    private static int DartWeekday(DateTime date) => (int)date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek;

    // Dart's `%`, whose result takes the sign of the divisor.
    private static int DartModulo(int value, int divisor) => ((value % divisor) + divisor) % divisor;
}

/// <summary>Mode of date entry method for the date picker dialog.</summary>
/// <remarks>
/// In <see cref="Calendar"/> mode, a calendar grid is displayed and the user taps the day they wish
/// to select. In <see cref="Input"/> mode, a <see cref="TextField"/> is displayed and the user types
/// in the date they wish to select. <see cref="CalendarOnly"/> and <see cref="InputOnly"/> are
/// variants of the above that don't allow the user to change to the mode.
/// </remarks>
public enum DatePickerEntryMode
{
    /// <summary>
    /// User picks a date from calendar grid. Can switch to <see cref="Input"/> by activating a mode
    /// button in the dialog.
    /// </summary>
    Calendar,

    /// <summary>
    /// User can input the date by typing it into a text field. Can switch to <see cref="Calendar"/> by
    /// activating a mode button in the dialog.
    /// </summary>
    Input,

    /// <summary>
    /// User can only pick a date from calendar grid. There is no user interface to switch to another
    /// mode.
    /// </summary>
    CalendarOnly,

    /// <summary>
    /// User can only input the date by typing it into a text field. There is no user interface to
    /// switch to another mode.
    /// </summary>
    InputOnly,
}

/// <summary>Initial display of a calendar date picker.</summary>
/// <remarks>Either a grid of available years or a monthly calendar.</remarks>
public enum DatePickerMode
{
    /// <summary>Choosing a month and day.</summary>
    Day,

    /// <summary>Choosing a year.</summary>
    Year,
}

/// <summary>
/// Encapsulates a start and end date that represent the range of dates.
/// </summary>
/// <remarks>
/// The range includes the <see cref="Start"/> and <see cref="End"/> dates. They may be equal to
/// indicate a date range of a single day. The start date must not be after the end date.
/// <para>
/// Dart's <c>@optionalTypeArgs</c> lets <c>DateTimeRange</c> stand for <c>DateTimeRange&lt;DateTime&gt;</c>;
/// C# spells the type argument out.
/// </para>
/// </remarks>
public sealed class DateTimeRange<T> : IEquatable<DateTimeRange<T>> where T : struct, IComparable<T>
{
    /// <summary>Creates a date range for the given start and end date.</summary>
    public DateTimeRange(T start, T end)
    {
        DebugAssertions.Assert(start.CompareTo(end) <= 0);
        Start = start;
        End = end;
    }

    /// <summary>The start of the range of dates.</summary>
    public T Start { get; }

    /// <summary>The end of the range of dates.</summary>
    public T End { get; }

    /// <summary>Returns the duration of the time between <see cref="Start"/> and <see cref="End"/>.</summary>
    /// <remarks>See <c>DateTime.difference</c> for more details.</remarks>
    public TimeSpan Duration => (DateTime)(object)End - (DateTime)(object)Start;

    public bool Equals(DateTimeRange<T>? other)
    {
        if (other is null || other.GetType() != GetType())
        {
            return false;
        }

        return EqualityComparer<T>.Default.Equals(other.Start, Start)
            && EqualityComparer<T>.Default.Equals(other.End, End);
    }

    public override bool Equals(object? obj) => obj is DateTimeRange<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public override string ToString() => $"{DisplayString(Start)} - {DisplayString(End)}";

    public static bool operator ==(DateTimeRange<T>? left, DateTimeRange<T>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(DateTimeRange<T>? left, DateTimeRange<T>? right) => !(left == right);

    // Dart interpolates `$start`, i.e. `DateTime.toString`.
    private static string DisplayString(T value) =>
        value is DateTime date ? DateUtils.DartToString(date) : value.ToString() ?? string.Empty;
}
