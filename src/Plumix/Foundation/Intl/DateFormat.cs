// Port of `package:intl` 0.20.3 `lib/src/intl/date_format.dart` and `date_format_field.dart`,
// restricted to formatting (parsing is not ported). Skeletons resolve against the pinned CLDR
// pattern table in `IntlData.g.cs`, the same data `flutter_localizations` installs into intl.

using System.Globalization;
using System.Text;

namespace Plumix.Foundation.Intl;

/// <summary>
/// Formats a date for a locale, from an ICU skeleton (<c>'yMMMd'</c>) or a literal pattern
/// (<c>'HH'</c>), exactly as Dart's <c>DateFormat</c> does.
/// </summary>
/// <remarks>
/// Dart's <c>DateFormat.y(locale)</c> and friends are named constructors that forward to
/// <c>DateFormat('y', locale)</c>; in C# they are just that constructor call.
/// </remarks>
public sealed class DateFormat
{
    private static readonly Dictionary<string, DateSymbols> CachedSymbols = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> CachedPatterns =
        new(StringComparer.Ordinal);

    private const string FieldCharacters = "GyMkSEahKHcLQdDmsvzZ";

    private List<Field>? formatFields;
    private bool? useNativeDigits;

    /// <summary>Dart's <c>DateFormat([newPattern, locale])</c>.</summary>
    /// <exception cref="ArgumentException">The locale has no date data and no usable fallback.</exception>
    public DateFormat(string? newPattern = null, string? locale = null)
    {
        Locale = Intl.VerifiedLocale(locale, LocaleExists, null)!;
        AddPattern(newPattern);
    }

    /// The locale whose data this format uses.
    public string Locale { get; }

    /// The pattern this format renders, after skeleton resolution.
    public string? Pattern { get; private set; }

    /// The symbols of <see cref="Locale"/>; Dart's <c>DateFormat.dateSymbols</c>.
    public DateSymbols DateSymbols => SymbolsFor(Locale);

    /// <summary>Whether this format renders the locale's native digits; Dart defaults it to true.</summary>
    public bool UseNativeDigits
    {
        get => useNativeDigits ??= true;
        set => useNativeDigits = value;
    }

    /// <summary>Whether date data is available for <paramref name="locale"/>.</summary>
    public static bool LocaleExists(string? locale) =>
        locale != null && IntlData.DatePatterns.ContainsKey(locale);

    /// <summary>Dart's <c>DateFormat.addPattern</c>: appends a skeleton or literal pattern.</summary>
    public DateFormat AddPattern(string? inputPattern, string separator = " ")
    {
        formatFields = null;
        if (inputPattern == null)
        {
            return this;
        }

        IReadOnlyDictionary<string, string> skeletons = PatternsFor(Locale);
        AppendPattern(skeletons.TryGetValue(inputPattern, out string? resolved) ? resolved : inputPattern,
            separator);
        return this;
    }

    /// <summary>Dart's <c>DateFormat.format</c>.</summary>
    public string Format(DartDateTime date)
    {
        var result = new StringBuilder();
        foreach (Field field in FormatFields)
        {
            result.Append(field.Format(date));
        }

        return result.ToString();
    }

    /// <summary>Dart's <c>DateFormat.format</c>, for a <see cref="DateTime"/>.</summary>
    public string Format(DateTime date) => Format(DartDateTime.FromDateTime(date));

    public override string ToString() => Pattern ?? string.Empty;

    private List<Field> FormatFields => formatFields ??= ParsePattern(Pattern ?? string.Empty);

    private void AppendPattern(string inputPattern, string separator)
    {
        Pattern = Pattern == null ? inputPattern : Pattern + separator + inputPattern;
    }

    private static DateSymbols SymbolsFor(string locale)
    {
        lock (CachedSymbols)
        {
            if (!CachedSymbols.TryGetValue(locale, out DateSymbols? symbols))
            {
                symbols = DateSymbols.Parse(IntlData.DateSymbols[locale]);
                CachedSymbols[locale] = symbols;
            }

            return symbols;
        }
    }

    private static IReadOnlyDictionary<string, string> PatternsFor(string locale)
    {
        lock (CachedPatterns)
        {
            if (!CachedPatterns.TryGetValue(locale, out IReadOnlyDictionary<string, string>? patterns))
            {
                string[] parts = IntlData.DatePatterns[locale].Split(IntlData.FieldSeparator);
                var map = new Dictionary<string, string>(parts.Length / 2, StringComparer.Ordinal);
                for (int i = 0; i + 1 < parts.Length; i += 2)
                {
                    map[parts[i]] = parts[i + 1];
                }

                patterns = map;
                CachedPatterns[locale] = patterns;
            }

            return patterns;
        }
    }

    /// Dart's `_parsePatternHelper`: quoted literals, runs of one field character, everything else.
    private List<Field> ParsePattern(string pattern)
    {
        var fields = new List<Field>();
        int index = 0;
        while (index < pattern.Length)
        {
            char start = pattern[index];
            if (start == '\'')
            {
                int end = index + 1;
                while (end < pattern.Length)
                {
                    if (pattern[end] == '\'')
                    {
                        if (end + 1 < pattern.Length && pattern[end + 1] == '\'')
                        {
                            end += 2;
                            continue;
                        }

                        end++;
                        break;
                    }

                    end++;
                }

                fields.Add(Field.Literal(PatchQuotes(pattern[index..end])));
                index = end;
            }
            else if (FieldCharacters.Contains(start, StringComparison.Ordinal))
            {
                int end = index;
                while (end < pattern.Length && pattern[end] == start)
                {
                    end++;
                }

                fields.Add(Field.Pattern(pattern[index..end], this));
                index = end;
            }
            else
            {
                int end = index;
                while (end < pattern.Length
                       && pattern[end] != '\''
                       && !FieldCharacters.Contains(pattern[end], StringComparison.Ordinal))
                {
                    end++;
                }

                fields.Add(Field.Literal(pattern[index..end]));
                index = end;
            }
        }

        return fields;
    }

    private static string PatchQuotes(string pattern) =>
        pattern == "''" ? "'" : pattern[1..^1].Replace("''", "'", StringComparison.Ordinal);

    private string LocalizeDigits(string text)
    {
        string zeroDigit = UseNativeDigits ? DateSymbols.ZeroDigit ?? "0" : "0";
        if (zeroDigit[0] == '0')
        {
            return text;
        }

        char[] digits = text.ToCharArray();
        for (int i = 0; i < digits.Length; i++)
        {
            digits[i] = (char)(digits[i] + zeroDigit[0] - '0');
        }

        return new string(digits);
    }

    /// Dart's `_DateFormatField` hierarchy, collapsed into one type: a literal or a pattern field.
    private sealed class Field
    {
        private readonly string pattern;
        private readonly DateFormat? parent;

        private Field(string pattern, DateFormat? parent)
        {
            this.pattern = pattern;
            this.parent = parent;
        }

        private int Width => pattern.Length;

        private DateSymbols Symbols => parent!.DateSymbols;

        public static Field Literal(string text) => new(text, null);

        public static Field Pattern(string text, DateFormat parent) => new(text, parent);

        public string Format(DartDateTime date) => parent == null ? pattern : FormatField(date);

        private string FormatField(DartDateTime date) => pattern[0] switch
        {
            'a' => Symbols.AmPms[date.Hour is >= 12 and < 24 ? 1 : 0],
            'c' => FormatStandaloneDay(date),
            'd' => PadTo(Width, date.Day),
            'D' => PadTo(Width, DayOfYear(date)),
            'E' => FormatDayOfWeek(date),
            'G' => Width >= 4 ? Symbols.EraNames[date.Year > 0 ? 1 : 0] : Symbols.Eras[date.Year > 0 ? 1 : 0],
            'h' => PadTo(Width, Format1To12Hours(date)),
            'H' => PadTo(Width, date.Hour),
            'K' => PadTo(Width, date.Hour % 12),
            'k' => PadTo(Width, date.Hour == 0 ? 24 : date.Hour),
            'L' => FormatStandaloneMonth(date),
            'M' => FormatMonth(date),
            'm' => PadTo(Width, date.Minute),
            'Q' => FormatQuarter(date),
            'S' => FormatFractionalSeconds(date),
            's' => PadTo(Width, date.Second),
            'y' => FormatYear(date),
            _ => string.Empty,
        };

        private string FormatYear(DartDateTime date)
        {
            int year = date.Year < 0 ? -date.Year : date.Year;
            return Width == 2 ? PadTo(2, year % 100) : PadTo(Width, year);
        }

        private string FormatMonth(DartDateTime date) => Width switch
        {
            5 => Symbols.NarrowMonths[date.Month - 1],
            4 => Symbols.Months[date.Month - 1],
            3 => Symbols.ShortMonths[date.Month - 1],
            _ => PadTo(Width, date.Month),
        };

        private string FormatStandaloneMonth(DartDateTime date) => Width switch
        {
            5 => Symbols.StandaloneNarrowMonths[date.Month - 1],
            4 => Symbols.StandaloneMonths[date.Month - 1],
            3 => Symbols.StandaloneShortMonths[date.Month - 1],
            _ => PadTo(Width, date.Month),
        };

        private string FormatStandaloneDay(DartDateTime date) => Width switch
        {
            5 => Symbols.StandaloneNarrowWeekdays[date.Weekday % 7],
            4 => Symbols.StandaloneWeekdays[date.Weekday % 7],
            3 => Symbols.StandaloneShortWeekdays[date.Weekday % 7],
            _ => PadTo(1, date.Day),
        };

        private string FormatDayOfWeek(DartDateTime date)
        {
            IReadOnlyList<string> names = Width switch
            {
                <= 3 => Symbols.ShortWeekdays,
                4 => Symbols.Weekdays,
                5 => Symbols.NarrowWeekdays,
                _ => throw new NotSupportedException("\"Short\" weekdays are currently not supported."),
            };
            return names[date.Weekday % 7];
        }

        private string FormatQuarter(DartDateTime date)
        {
            int quarter = (date.Month - 1) / 3;
            return Width switch
            {
                4 => Symbols.Quarters[quarter],
                3 => Symbols.ShortQuarters[quarter],
                _ => PadTo(Width, quarter + 1),
            };
        }

        private string FormatFractionalSeconds(DartDateTime date)
        {
            string basic = PadTo(3, date.Millisecond);
            return Width - 3 > 0 ? basic + PadTo(Width - 3, 0) : basic;
        }

        private static int Format1To12Hours(DartDateTime date)
        {
            int hours = date.Hour > 12 ? date.Hour - 12 : date.Hour;
            return hours == 0 ? 12 : hours;
        }

        private static int DayOfYear(DartDateTime date)
        {
            int[] cumulative = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334];
            bool leap = date.Year % 4 == 0 && (date.Year % 100 != 0 || date.Year % 400 == 0);
            return cumulative[date.Month - 1] + date.Day + (leap && date.Month > 2 ? 1 : 0);
        }

        private string PadTo(int width, int value) =>
            parent!.LocalizeDigits(value.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0'));
    }
}
