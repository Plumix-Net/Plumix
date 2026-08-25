// Port of `package:intl` 0.20.3 `lib/src/intl/number_format.dart` and `lib/number_symbols.dart`,
// restricted to what the ported localizations use: `NumberFormat.decimalPattern(locale)` applied to
// whole numbers (picker labels, tab indices). Fractions, currencies, percent, exponential notation
// and parsing are not ported. Per-locale data lives in `IntlData.g.cs`.

using System.Globalization;
using System.Text;

namespace Plumix.Foundation.Intl;

/// <summary>The number symbols of one locale: separators, native digits and the decimal pattern.</summary>
public sealed class NumberSymbols
{
    private NumberSymbols(
        string name,
        string decimalSeparator,
        string groupSeparator,
        string zeroDigit,
        string plusSign,
        string minusSign,
        string decimalPattern)
    {
        Name = name;
        DecimalSeparator = decimalSeparator;
        GroupSeparator = groupSeparator;
        ZeroDigit = zeroDigit;
        PlusSign = plusSign;
        MinusSign = minusSign;
        DecimalPattern = decimalPattern;
    }

    /// Dart's <c>NAME</c>.
    public string Name { get; }

    /// Dart's <c>DECIMAL_SEP</c>.
    public string DecimalSeparator { get; }

    /// Dart's <c>GROUP_SEP</c>.
    public string GroupSeparator { get; }

    /// Dart's <c>ZERO_DIGIT</c>.
    public string ZeroDigit { get; }

    /// Dart's <c>PLUS_SIGN</c>.
    public string PlusSign { get; }

    /// Dart's <c>MINUS_SIGN</c>.
    public string MinusSign { get; }

    /// Dart's <c>DECIMAL_PATTERN</c>.
    public string DecimalPattern { get; }

    public override string ToString() => Name;

    internal static NumberSymbols Parse(string packed)
    {
        string[] fields = packed.Split(IntlData.FieldSeparator);
        return new NumberSymbols(fields[0], fields[1], fields[2], fields[3], fields[4], fields[5], fields[6]);
    }
}

/// <summary>
/// Formats whole numbers for a locale, following that locale's CLDR decimal pattern.
/// </summary>
public sealed class NumberFormat
{
    private static readonly Dictionary<string, NumberSymbols> CachedSymbols = new(StringComparer.Ordinal);

    private readonly int groupingSize;
    private readonly int finalGroupingSize;
    private readonly int minimumIntegerDigits;

    private NumberFormat(string locale, NumberSymbols symbols)
    {
        Locale = locale;
        Symbols = symbols;
        (groupingSize, finalGroupingSize, minimumIntegerDigits) = ParsePattern(symbols.DecimalPattern);
    }

    /// The locale whose symbols this format uses.
    public string Locale { get; }

    /// The symbols this format uses.
    public NumberSymbols Symbols { get; }

    /// <summary>Dart's <c>NumberFormat.decimalPattern([locale])</c>.</summary>
    public static NumberFormat DecimalPattern(string? locale = null)
    {
        string verified = Intl.VerifiedLocale(locale, LocaleExists, null)!;
        return new NumberFormat(verified, SymbolsFor(verified));
    }

    /// <summary>Whether number symbols are available for <paramref name="locale"/>.</summary>
    public static bool LocaleExists(string? locale) =>
        locale != null && IntlData.NumberSymbols.ContainsKey(locale);

    /// <summary>Dart's <c>NumberFormat.format</c> for a whole number.</summary>
    public string Format(long number)
    {
        var buffer = new StringBuilder();
        if (number < 0)
        {
            buffer.Append(Symbols.MinusSign);
        }

        string digits = Math.Abs(number).ToString(CultureInfo.InvariantCulture)
            .PadLeft(minimumIntegerDigits, '0');
        buffer.Append(Group(digits));
        return LocalizeDigits(buffer.ToString());
    }

    private string Group(string digits)
    {
        if (finalGroupingSize <= 0 || digits.Length <= finalGroupingSize)
        {
            return digits;
        }

        var groups = new List<string>();
        int end = digits.Length;
        int size = finalGroupingSize;
        while (end > size)
        {
            groups.Insert(0, digits.Substring(end - size, size));
            end -= size;
            size = groupingSize <= 0 ? size : groupingSize;
        }

        groups.Insert(0, digits[..end]);
        return string.Join(Symbols.GroupSeparator, groups);
    }

    private string LocalizeDigits(string text)
    {
        int zero = Symbols.ZeroDigit[0];
        if (zero == '0')
        {
            return text;
        }

        char[] digits = text.ToCharArray();
        for (int i = 0; i < digits.Length; i++)
        {
            if (digits[i] is >= '0' and <= '9')
            {
                digits[i] = (char)(digits[i] + zero - '0');
            }
        }

        return new string(digits);
    }

    private static NumberSymbols SymbolsFor(string locale)
    {
        lock (CachedSymbols)
        {
            if (!CachedSymbols.TryGetValue(locale, out NumberSymbols? symbols))
            {
                symbols = NumberSymbols.Parse(IntlData.NumberSymbols[locale]);
                CachedSymbols[locale] = symbols;
            }

            return symbols;
        }
    }

    /// Dart's `NumberFormatParser.parseTrunkCharacter`, for the integer part of a decimal pattern.
    private static (int GroupingSize, int FinalGroupingSize, int MinimumIntegerDigits) ParsePattern(
        string pattern)
    {
        int grouping = 3;
        bool groupingSetExplicitly = false;
        int groupingCount = -1;
        int zeroDigitCount = 0;
        foreach (char character in pattern)
        {
            if (character == '.')
            {
                break;
            }

            switch (character)
            {
                case '#':
                    if (groupingCount >= 0)
                    {
                        groupingCount++;
                    }

                    break;
                case '0':
                    zeroDigitCount++;
                    if (groupingCount >= 0)
                    {
                        groupingCount++;
                    }

                    break;
                case ',':
                    if (groupingCount > 0)
                    {
                        groupingSetExplicitly = true;
                        grouping = groupingCount;
                    }

                    groupingCount = 0;
                    break;
            }
        }

        int finalGrouping = Math.Max(0, groupingCount);
        return (groupingSetExplicitly ? grouping : finalGrouping, finalGrouping, zeroDigitCount);
    }
}
