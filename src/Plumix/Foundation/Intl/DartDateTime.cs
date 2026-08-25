// C#-only infrastructure: Dart's `DateTime` value semantics, needed because `System.DateTime` is
// limited to years 1..9999 and rejects out-of-range components, while `GlobalCupertinoLocalizations`
// formats values such as `DateTime.utc(0, 0, dayIndex)` and `DateTime.utc(1, 1, weekDay)` that rely
// on Dart's field normalization. See `intl` (0.20.3) and the Dart SDK's
// `DateTime._brokenDownDateToValue`.

namespace Plumix.Foundation.Intl;

/// <summary>
/// A calendar date/time with Dart's <c>DateTime</c> normalization: out-of-range components roll
/// over into neighbouring fields, and years outside <see cref="DateTime"/>'s range are allowed.
/// </summary>
public readonly struct DartDateTime : IEquatable<DartDateTime>
{
    private DartDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int days)
    {
        Year = year;
        Month = month;
        Day = day;
        Hour = hour;
        Minute = minute;
        Second = second;
        Millisecond = millisecond;
        DaysSinceEpoch = days;
    }

    /// The year, which may be zero or negative (proleptic Gregorian).
    public int Year { get; }

    /// The month, 1..12.
    public int Month { get; }

    /// The day of the month, 1..31.
    public int Day { get; }

    /// The hour, 0..23.
    public int Hour { get; }

    /// The minute, 0..59.
    public int Minute { get; }

    /// The second, 0..59.
    public int Second { get; }

    /// The millisecond, 0..999.
    public int Millisecond { get; }

    /// Days since 1970-01-01; negative before the epoch.
    public int DaysSinceEpoch { get; }

    /// Dart's <c>DateTime.weekday</c>: Monday is 1, Sunday is 7.
    public int Weekday => (int)Modulo(DaysSinceEpoch + 3, 7) + 1;

    /// <summary>Dart's <c>DateTime.utc(...)</c>: every component is normalized by rolling over.</summary>
    public static DartDateTime Utc(
        int year,
        int month = 1,
        int day = 1,
        int hour = 0,
        int minute = 0,
        int second = 0,
        int millisecond = 0)
    {
        long totalMonths = (long)month - 1;
        long normalizedYear = year + FloorDiv(totalMonths, 12);
        int normalizedMonth = (int)Modulo(totalMonths, 12) + 1;

        long totalMilliseconds = ((long)hour * 3600 + (long)minute * 60 + second) * 1000 + millisecond;
        long dayOffset = FloorDiv(totalMilliseconds, 86400000);
        int timeOfDay = (int)Modulo(totalMilliseconds, 86400000);

        long days = DaysFromCivil(normalizedYear, normalizedMonth, 1) + (day - 1) + dayOffset;
        (int civilYear, int civilMonth, int civilDay) = CivilFromDays(days);

        return new DartDateTime(
            civilYear,
            civilMonth,
            civilDay,
            timeOfDay / 3600000,
            timeOfDay / 60000 % 60,
            timeOfDay / 1000 % 60,
            timeOfDay % 1000,
            (int)days);
    }

    /// <summary>Converts a <see cref="DateTime"/>, dropping its kind and sub-millisecond ticks.</summary>
    public static DartDateTime FromDateTime(DateTime value) => Utc(
        value.Year,
        value.Month,
        value.Day,
        value.Hour,
        value.Minute,
        value.Second,
        value.Millisecond);

    public static implicit operator DartDateTime(DateTime value) => FromDateTime(value);

    public bool Equals(DartDateTime other) =>
        DaysSinceEpoch == other.DaysSinceEpoch
        && Hour == other.Hour
        && Minute == other.Minute
        && Second == other.Second
        && Millisecond == other.Millisecond;

    public override bool Equals(object? obj) => obj is DartDateTime other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(DaysSinceEpoch, Hour, Minute, Second, Millisecond);

    public override string ToString() =>
        $"{Year:0000}-{Month:00}-{Day:00} {Hour:00}:{Minute:00}:{Second:00}.{Millisecond:000}";

    private static long FloorDiv(long value, long divisor)
    {
        long quotient = value / divisor;
        return value % divisor != 0 && (value < 0) != (divisor < 0) ? quotient - 1 : quotient;
    }

    private static long Modulo(long value, long divisor)
    {
        long remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
    }

    /// Howard Hinnant's `days_from_civil`, valid for the whole proleptic Gregorian calendar.
    private static long DaysFromCivil(long year, int month, int day)
    {
        year -= month <= 2 ? 1 : 0;
        long era = FloorDiv(year, 400);
        long yearOfEra = year - era * 400;
        long dayOfYear = (153 * (month + (month > 2 ? -3 : 9)) + 2) / 5 + day - 1;
        long dayOfEra = yearOfEra * 365 + yearOfEra / 4 - yearOfEra / 100 + dayOfYear;
        return era * 146097 + dayOfEra - 719468;
    }

    /// Howard Hinnant's `civil_from_days`, the inverse of <see cref="DaysFromCivil"/>.
    private static (int Year, int Month, int Day) CivilFromDays(long days)
    {
        days += 719468;
        long era = FloorDiv(days, 146097);
        long dayOfEra = days - era * 146097;
        long yearOfEra = (dayOfEra - dayOfEra / 1460 + dayOfEra / 36524 - dayOfEra / 146096) / 365;
        long year = yearOfEra + era * 400;
        long dayOfYear = dayOfEra - (365 * yearOfEra + yearOfEra / 4 - yearOfEra / 100);
        long monthPrime = (5 * dayOfYear + 2) / 153;
        long day = dayOfYear - (153 * monthPrime + 2) / 5 + 1;
        long month = monthPrime + (monthPrime < 10 ? 3 : -9);
        return ((int)(year + (month <= 2 ? 1 : 0)), (int)month, (int)day);
    }
}
