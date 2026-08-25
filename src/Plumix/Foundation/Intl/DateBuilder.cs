// Port of `package:intl` 0.20.3 `lib/src/intl/date_builder.dart`.

using System.Globalization;

namespace Plumix.Foundation.Intl;

/// <summary>
/// Accumulates the fields a <see cref="DateFormat"/> reads while parsing, then builds the date.
/// </summary>
/// <remarks>
/// Dart's <c>DateBuilder</c> also retries construction to work around <c>DateTime</c>'s local time
/// zone (its <c>_correctForErrors</c>); <see cref="DartDateTime"/> has no time zone, so the retry
/// loop can never change the result and is not ported.
/// </remarks>
internal sealed class DateBuilder
{
    private readonly string locale;
    private DartDateTime? date;
    private bool hasAmbiguousCentury;

    public DateBuilder(string locale)
    {
        this.locale = locale;
    }

    public int Year { get; set; } = 1970;

    public int Month { get; set; } = 1;

    public int Day { get; set; } = 1;

    public int DayOfYear { get; set; }

    public int Hour { get; set; }

    public int Minute { get; set; }

    public int Second { get; set; }

    public int FractionalSecond { get; set; }

    public bool Pm { get; set; }

    public bool DateOnly { get; set; }

    public int Hour24 => Pm ? Hour + 12 : Hour;

    public int DayOrDayOfYear => DayOfYear == 0 ? Day : DayOfYear;

    private bool HasCentury => !hasAmbiguousCentury || Year < 0 || Year >= 100;

    public void SetYear(int value) => Year = value;

    public void SetMonth(int value) => Month = value;

    public void SetDay(int value) => Day = value;

    public void SetDayOfYear(int value) => DayOfYear = value;

    public void SetHour(int value) => Hour = value;

    public void SetMinute(int value) => Minute = value;

    public void SetSecond(int value) => Second = value;

    public void SetFractionalSecond(int value) => FractionalSecond = value;

    public void SetHasAmbiguousCentury(bool isAmbiguous) => hasAmbiguousCentury = isAmbiguous;

    /// <summary>Dart's <c>DateBuilder.verify</c>: the strict-parse range and round-trip checks.</summary>
    public void Verify(string input)
    {
        Verify(Month, 1, 12, "month", input);
        Verify(Hour24, 0, 23, "hour", input);
        Verify(Minute, 0, 59, "minute", input);
        Verify(Second, 0, 59, "second", input);
        Verify(FractionalSecond, 0, 999, "fractional second", input);

        DartDateTime parsed = AsDate();
        int minimumHour = DateOnly && parsed.Hour == 1 ? 0 : parsed.Hour;
        Verify(Hour24, minimumHour, parsed.Hour, "hour", input, parsed);
        if (DayOfYear > 0)
        {
            int correspondingDay = DayOfYearOf(parsed);
            Verify(DayOfYear, correspondingDay, correspondingDay, "dayOfYear", input, parsed);
        }
        else
        {
            Verify(Day, parsed.Day, parsed.Day, "day", input, parsed);
        }

        Verify(EstimatedYear, parsed.Year, parsed.Year, "year", input, parsed);
    }

    /// <summary>Dart's <c>DateBuilder.asDate</c>.</summary>
    public DartDateTime AsDate() => date ??= DartDateTime.Utc(
        EstimatedYear, Month, DayOrDayOfYear, Hour24, Minute, Second, FractionalSecond);

    /// Dart's `DateBuilder._estimatedYear`: a two-digit year lands in the window around today.
    private int EstimatedYear
    {
        get
        {
            if (HasCentury)
            {
                return Year;
            }

            const int lookBehindYears = 80;
            DartDateTime now = DartDateTime.FromDateTime(DateTime.UtcNow);
            DartDateTime lowerDate = OffsetYear(now, -lookBehindYears);
            DartDateTime upperDate = OffsetYear(now, 100 - lookBehindYears);
            int lowerCentury = lowerDate.Year / 100 * 100;
            int upperCentury = upperDate.Year / 100 * 100;
            return Preliminary(upperCentury + Year).CompareTo(upperDate) <= 0
                ? upperCentury + Year
                : lowerCentury + Year;
        }
    }

    private DartDateTime Preliminary(int year) => DartDateTime.Utc(
        year, Month, DayOrDayOfYear, Hour24, Minute, Second, FractionalSecond);

    private static DartDateTime OffsetYear(DartDateTime value, int offsetYears) => DartDateTime.Utc(
        value.Year + offsetYears,
        value.Month,
        value.Day,
        value.Hour,
        value.Minute,
        value.Second,
        value.Millisecond);

    private static int DayOfYearOf(DartDateTime value)
    {
        int[] cumulative = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334];
        bool leap = value.Year % 4 == 0 && (value.Year % 100 != 0 || value.Year % 400 == 0);
        return cumulative[value.Month - 1] + value.Day + (leap && value.Month > 2 ? 1 : 0);
    }

    private void Verify(
        int value,
        int minimum,
        int maximum,
        string description,
        string originalInput,
        DartDateTime? parsed = null)
    {
        if (value >= minimum && value <= maximum)
        {
            return;
        }

        string parsedDescription = parsed == null ? string.Empty : $" Date parsed as {parsed}.";
        throw new FormatException(string.Create(
            CultureInfo.InvariantCulture,
            $"Error parsing {originalInput}, invalid {description} value: {value} in {locale}."
            + $" Expected value between {minimum} and {maximum}.{parsedDescription}."));
    }
}
