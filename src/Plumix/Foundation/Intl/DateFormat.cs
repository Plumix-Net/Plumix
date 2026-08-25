// Port of `package:intl` 0.20.3 `lib/src/intl/date_format.dart` and `date_format_field.dart`.
// Skeletons resolve against the pinned CLDR pattern table in `IntlData.g.cs`, the same data
// `flutter_localizations` installs into intl. Loose parsing (`parseLoose`) is not ported.

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
    private bool? dateOnly;

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
        dateOnly = null;
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

    /// <summary>Dart's <c>DateFormat.parse</c>: reads the fields this pattern names, in order.</summary>
    /// <exception cref="FormatException">The input does not match the pattern.</exception>
    public DartDateTime Parse(string inputString) => ParseCore(inputString, strict: false);

    /// <summary>
    /// Dart's <c>DateFormat.parseStrict</c>: <see cref="Parse"/>, but the whole input must be
    /// consumed and every field must round-trip through the resulting date.
    /// </summary>
    /// <exception cref="FormatException">The input does not match the pattern.</exception>
    public DartDateTime ParseStrict(string inputString) => ParseCore(inputString, strict: true);

    /// <summary>Dart's <c>DateFormat.tryParseStrict</c>.</summary>
    public DartDateTime? TryParseStrict(string inputString)
    {
        try
        {
            return ParseStrict(inputString);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public override string ToString() => Pattern ?? string.Empty;

    /// <summary>Dart's <c>DateFormat.dateOnly</c>: no field of this pattern carries a time.</summary>
    public bool DateOnly => dateOnly ??= FormatFields.TrueForAll(each => each.ForDate);

    private List<Field> FormatFields => formatFields ??= ParsePattern(Pattern ?? string.Empty);

    /// Dart's `DateFormat._parse`.
    private DartDateTime ParseCore(string inputString, bool strict)
    {
        var dateFields = new DateBuilder(Locale) { DateOnly = DateOnly };
        var stack = new StringStack(inputString);
        foreach (Field field in FormatFields)
        {
            field.Parse(stack, dateFields);
        }

        if (strict && !stack.AtEnd)
        {
            throw new FormatException($"Characters remaining after date parsing in {inputString}");
        }

        if (strict)
        {
            dateFields.Verify(inputString);
        }

        return dateFields.AsDate();
    }

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

    /// Dart's `DateFormat.localeZeroCodeUnit`: the locale's digit zero, or ASCII zero.
    private char LocaleZero
    {
        get
        {
            string zeroDigit = UseNativeDigits ? DateSymbols.ZeroDigit ?? "0" : "0";
            return zeroDigit[0];
        }
    }

    private string LocalizeDigits(string text)
    {
        char zeroDigit = LocaleZero;
        if (zeroDigit == '0')
        {
            return text;
        }

        char[] digits = text.ToCharArray();
        for (int i = 0; i < digits.Length; i++)
        {
            digits[i] = (char)(digits[i] + zeroDigit - '0');
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

        /// Dart's `_DateFormatField.forDate`: literals count as date-only.
        public bool ForDate => parent == null
                               || "cdDEGLMQvyZz".Contains(pattern[0], StringComparison.Ordinal);

        public string Format(DartDateTime date) => parent == null ? pattern : FormatField(date);

        /// <summary>Dart's <c>_DateFormatField.parse</c>.</summary>
        public void Parse(StringStack input, DateBuilder builder)
        {
            if (parent == null)
            {
                ParseLiteral(input);
                return;
            }

            try
            {
                ParseField(input, builder);
            }
            catch (Exception exception) when (exception is FormatException or ArgumentOutOfRangeException
                                                  or IndexOutOfRangeException or OverflowException)
            {
                throw FormatError(input);
            }
        }

        /// Dart's `_DateFormatField.parseLiteral`.
        private void ParseLiteral(StringStack input)
        {
            string found = input.Read(Width);
            if (!string.Equals(found, pattern, StringComparison.Ordinal))
            {
                throw FormatError(input);
            }
        }

        /// Dart's `_DateFormatPatternField.parseField`.
        private void ParseField(StringStack input, DateBuilder builder)
        {
            switch (pattern[0])
            {
                case 'a':
                    ParseAmPm(input, builder);
                    break;
                case 'c':
                    ParseStandaloneDay(input);
                    break;
                case 'd':
                    HandleNumericField(input, builder.SetDay);
                    break;
                case 'D':
                    HandleNumericField(input, builder.SetDayOfYear);
                    break;
                case 'E':
                    ParseEnumeratedString(input, Width >= 4 ? Symbols.Weekdays : Symbols.ShortWeekdays);
                    break;
                case 'G':
                    ParseEnumeratedString(input, Width >= 4 ? Symbols.EraNames : Symbols.Eras);
                    break;
                case 'h':
                    HandleNumericField(input, builder.SetHour);
                    if (builder.Hour == 12)
                    {
                        builder.Hour = 0;
                    }

                    break;
                case 'H':
                case 'K':
                    HandleNumericField(input, builder.SetHour);
                    break;
                case 'k':
                    HandleNumericField(input, builder.SetHour, -1);
                    break;
                case 'L':
                    ParseStandaloneMonth(input, builder);
                    break;
                case 'M':
                    ParseMonth(input, builder);
                    break;
                case 'm':
                    HandleNumericField(input, builder.SetMinute);
                    break;
                case 'S':
                    HandleNumericField(input, builder.SetFractionalSecond);
                    break;
                case 's':
                    HandleNumericField(input, builder.SetSecond);
                    break;
                case 'y':
                    HandleNumericField(input, builder.SetYear);
                    builder.SetHasAmbiguousCentury(Width == 2);
                    break;
                default:
                    // 'Q' (quarter), 'v'/'z'/'Z' (time zones): Dart reads nothing for these.
                    break;
            }
        }

        private void ParseMonth(StringStack input, DateBuilder builder)
        {
            IReadOnlyList<string>? possibilities = Width switch
            {
                5 => Symbols.NarrowMonths,
                4 => Symbols.Months,
                3 => Symbols.ShortMonths,
                _ => null,
            };
            if (possibilities == null)
            {
                HandleNumericField(input, builder.SetMonth);
                return;
            }

            builder.Month = ParseEnumeratedString(input, possibilities) + 1;
        }

        private void ParseStandaloneMonth(StringStack input, DateBuilder builder)
        {
            IReadOnlyList<string>? possibilities = Width switch
            {
                5 => Symbols.StandaloneNarrowMonths,
                4 => Symbols.StandaloneMonths,
                3 => Symbols.StandaloneShortMonths,
                _ => null,
            };
            if (possibilities == null)
            {
                HandleNumericField(input, builder.SetMonth);
                return;
            }

            builder.Month = ParseEnumeratedString(input, possibilities) + 1;
        }

        private void ParseStandaloneDay(StringStack input)
        {
            IReadOnlyList<string>? possibilities = Width switch
            {
                5 => Symbols.StandaloneNarrowWeekdays,
                4 => Symbols.StandaloneWeekdays,
                3 => Symbols.StandaloneShortWeekdays,
                _ => null,
            };
            if (possibilities == null)
            {
                HandleNumericField(input, _ => { });
                return;
            }

            ParseEnumeratedString(input, possibilities);
        }

        private void ParseAmPm(StringStack input, DateBuilder builder)
        {
            if (ParseEnumeratedString(input, Symbols.AmPms) == 1)
            {
                builder.Pm = true;
            }
        }

        /// Dart's `_DateFormatPatternField.parseEnumeratedString`: the longest match wins.
        private int ParseEnumeratedString(StringStack input, IReadOnlyList<string> possibilities)
        {
            int longest = -1;
            for (int i = 0; i < possibilities.Count; i++)
            {
                if (!string.Equals(input.Peek(possibilities[i].Length), possibilities[i],
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (longest < 0 || possibilities[i].Length >= possibilities[longest].Length)
                {
                    longest = i;
                }
            }

            if (longest < 0)
            {
                throw FormatError(input);
            }

            input.Pop(possibilities[longest].Length);
            return longest;
        }

        /// Dart's `_DateFormatPatternField.handleNumericField`.
        private void HandleNumericField(StringStack input, Action<int> setter, int offset = 0)
        {
            setter(NextInteger(input) + offset);
        }

        /// Dart's `_DateFormatPatternField._nextInteger`, over the locale's own digits.
        private int NextInteger(StringStack input)
        {
            char zero = parent!.LocaleZero;
            string rest = input.PeekAll();
            int length = 0;
            while (length < rest.Length && rest[length] >= zero && rest[length] <= (char)(zero + 9))
            {
                length++;
            }

            if (length == 0)
            {
                throw FormatError(input);
            }

            input.Pop(length);
            int result = 0;
            for (int i = 0; i < length; i++)
            {
                result = checked(result * 10 + (rest[i] - zero));
            }

            return result;
        }

        private FormatException FormatError(StringStack input) =>
            new($"Trying to read {pattern} from {input}");

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
