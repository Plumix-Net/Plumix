// Port of `package:intl` 0.20.3 `lib/date_symbols.dart` (the subset the ported localizations use).
// The per-locale data is generated into `IntlData.g.cs` by `scripts/generate_intl_data.py` from the
// same CLDR snapshot Flutter loads (flutter_localizations `l10n/generated_date_localizations.dart`).

using System.Globalization;

namespace Plumix.Foundation.Intl;

/// <summary>
/// The date/time symbols (month names, weekday names, am/pm markers, native digits) of one locale.
/// </summary>
/// <remarks>
/// Dart's <c>DateSymbols</c> carries the standard date/time pattern lists (<c>DATEFORMATS</c>,
/// <c>TIMEFORMATS</c>, <c>DATETIMEFORMATS</c>, <c>AVAILABLEFORMATS</c>) as well; Plumix formats by
/// skeleton only, so those live in <see cref="DateFormat"/>'s pattern table instead.
/// </remarks>
public sealed class DateSymbols
{
    internal DateSymbols(
        string name,
        IReadOnlyList<string> eras,
        IReadOnlyList<string> eraNames,
        IReadOnlyList<string> narrowMonths,
        IReadOnlyList<string> standaloneNarrowMonths,
        IReadOnlyList<string> months,
        IReadOnlyList<string> standaloneMonths,
        IReadOnlyList<string> shortMonths,
        IReadOnlyList<string> standaloneShortMonths,
        IReadOnlyList<string> weekdays,
        IReadOnlyList<string> standaloneWeekdays,
        IReadOnlyList<string> shortWeekdays,
        IReadOnlyList<string> standaloneShortWeekdays,
        IReadOnlyList<string> narrowWeekdays,
        IReadOnlyList<string> standaloneNarrowWeekdays,
        IReadOnlyList<string> shortQuarters,
        IReadOnlyList<string> quarters,
        IReadOnlyList<string> amPms,
        string? zeroDigit,
        int firstDayOfWeek)
    {
        Name = name;
        Eras = eras;
        EraNames = eraNames;
        NarrowMonths = narrowMonths;
        StandaloneNarrowMonths = standaloneNarrowMonths;
        Months = months;
        StandaloneMonths = standaloneMonths;
        ShortMonths = shortMonths;
        StandaloneShortMonths = standaloneShortMonths;
        Weekdays = weekdays;
        StandaloneWeekdays = standaloneWeekdays;
        ShortWeekdays = shortWeekdays;
        StandaloneShortWeekdays = standaloneShortWeekdays;
        NarrowWeekdays = narrowWeekdays;
        StandaloneNarrowWeekdays = standaloneNarrowWeekdays;
        ShortQuarters = shortQuarters;
        Quarters = quarters;
        AmPms = amPms;
        ZeroDigit = zeroDigit;
        FirstDayOfWeek = firstDayOfWeek;
    }

    /// Dart's <c>NAME</c>: the locale these symbols belong to.
    public string Name { get; }

    /// Dart's <c>ERAS</c>.
    public IReadOnlyList<string> Eras { get; }

    /// Dart's <c>ERANAMES</c>.
    public IReadOnlyList<string> EraNames { get; }

    /// Dart's <c>NARROWMONTHS</c>.
    public IReadOnlyList<string> NarrowMonths { get; }

    /// Dart's <c>STANDALONENARROWMONTHS</c>.
    public IReadOnlyList<string> StandaloneNarrowMonths { get; }

    /// Dart's <c>MONTHS</c>.
    public IReadOnlyList<string> Months { get; }

    /// Dart's <c>STANDALONEMONTHS</c>.
    public IReadOnlyList<string> StandaloneMonths { get; }

    /// Dart's <c>SHORTMONTHS</c>.
    public IReadOnlyList<string> ShortMonths { get; }

    /// Dart's <c>STANDALONESHORTMONTHS</c>.
    public IReadOnlyList<string> StandaloneShortMonths { get; }

    /// Dart's <c>WEEKDAYS</c>, starting at Sunday.
    public IReadOnlyList<string> Weekdays { get; }

    /// Dart's <c>STANDALONEWEEKDAYS</c>, starting at Sunday.
    public IReadOnlyList<string> StandaloneWeekdays { get; }

    /// Dart's <c>SHORTWEEKDAYS</c>, starting at Sunday.
    public IReadOnlyList<string> ShortWeekdays { get; }

    /// Dart's <c>STANDALONESHORTWEEKDAYS</c>, starting at Sunday.
    public IReadOnlyList<string> StandaloneShortWeekdays { get; }

    /// Dart's <c>NARROWWEEKDAYS</c>, starting at Sunday.
    public IReadOnlyList<string> NarrowWeekdays { get; }

    /// Dart's <c>STANDALONENARROWWEEKDAYS</c>, starting at Sunday.
    public IReadOnlyList<string> StandaloneNarrowWeekdays { get; }

    /// Dart's <c>SHORTQUARTERS</c>.
    public IReadOnlyList<string> ShortQuarters { get; }

    /// Dart's <c>QUARTERS</c>.
    public IReadOnlyList<string> Quarters { get; }

    /// Dart's <c>AMPMS</c>.
    public IReadOnlyList<string> AmPms { get; }

    /// Dart's <c>ZERODIGIT</c>: the locale's digit zero when it does not use ASCII digits.
    public string? ZeroDigit { get; }

    /// Dart's <c>FIRSTDAYOFWEEK</c>, zero-based from Monday.
    public int FirstDayOfWeek { get; }

    public override string ToString() => Name;

    /// <summary>Parses one record of the packed table in <c>IntlData.g.cs</c>.</summary>
    /// <remarks>
    /// Fields are separated by U+0001 and list items by <c>'|'</c>, in the order the constructor
    /// takes them; <see cref="ZeroDigit"/> is an empty field when the locale uses ASCII digits.
    /// </remarks>
    internal static DateSymbols Parse(string packed)
    {
        string[] fields = packed.Split(IntlData.FieldSeparator);
        return new DateSymbols(
            fields[0],
            Items(fields[1]),
            Items(fields[2]),
            Items(fields[3]),
            Items(fields[4]),
            Items(fields[5]),
            Items(fields[6]),
            Items(fields[7]),
            Items(fields[8]),
            Items(fields[9]),
            Items(fields[10]),
            Items(fields[11]),
            Items(fields[12]),
            Items(fields[13]),
            Items(fields[14]),
            Items(fields[15]),
            Items(fields[16]),
            Items(fields[17]),
            fields[18].Length == 0 ? null : fields[18],
            int.Parse(fields[19], CultureInfo.InvariantCulture));

        static IReadOnlyList<string> Items(string field) => field.Split(IntlData.ItemSeparator);
    }
}
