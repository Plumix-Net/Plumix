// Port of `package:intl` 0.20.3 `lib/intl.dart` and `lib/src/intl_helpers.dart`, restricted to the
// locale canonicalization/verification and plural selection the ported localizations use.

namespace Plumix.Foundation.Intl;

/// <summary>Locale handling and message selection helpers; Dart's <c>Intl</c>.</summary>
public static class Intl
{
    /// <summary>Dart's <c>Intl.getCurrentLocale()</c> default: intl's own system locale default.</summary>
    public const string DefaultLocale = "en_US";

    /// <summary>Dart's <c>Intl.canonicalizedLocale</c>: <c>xx_YY</c> with an upper-case region.</summary>
    public static string CanonicalizedLocale(string? locale)
    {
        if (locale == null)
        {
            return DefaultLocale;
        }

        if (locale == "C")
        {
            return "en_ISO";
        }

        if (locale.Length < 5)
        {
            return locale;
        }

        int separator = LanguageSeparatorIndex(locale);
        if (separator == -1)
        {
            return locale;
        }

        string language = locale[..separator];
        string region = locale[(separator + 1)..];
        if (region.Length <= 3)
        {
            region = region.ToUpperInvariant();
        }

        return language + "_" + region;
    }

    /// <summary>Dart's <c>Intl.shortLocale</c>: <c>'en_US'</c> becomes <c>'en'</c>.</summary>
    public static string ShortLocale(string locale) => LanguageOnlyLocale(locale);

    /// <summary>
    /// Dart's <c>Intl.verifiedLocale</c>: the closest locale <paramref name="localeExists"/> accepts.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// No fallback exists and <paramref name="onFailure"/> is null.
    /// </exception>
    public static string? VerifiedLocale(
        string? newLocale,
        Func<string, bool> localeExists,
        Func<string, string?>? onFailure)
    {
        if (newLocale == null)
        {
            return VerifiedLocale(DefaultLocale, localeExists, onFailure);
        }

        if (localeExists(newLocale))
        {
            return newLocale;
        }

        Func<string, string>[] fallbacks =
        [
            CanonicalizedLocale,
            LanguageRegionOnlyLocale,
            LanguageOnlyLocale,
            DeprecatedLocale,
            locale => DeprecatedLocale(LanguageOnlyLocale(locale)),
            locale => DeprecatedLocale(CanonicalizedLocale(locale)),
            _ => "fallback",
        ];
        foreach (Func<string, string> fallback in fallbacks)
        {
            string candidate = fallback(newLocale);
            if (localeExists(candidate))
            {
                return candidate;
            }
        }

        if (onFailure != null)
        {
            return onFailure(newLocale);
        }

        throw new ArgumentException($"Invalid locale \"{newLocale}\"", nameof(newLocale));
    }

    /// <summary>
    /// Dart's <c>Intl.pluralLogic</c>: picks the alternative for <paramref name="howMany"/> in
    /// <paramref name="locale"/>, with an explicit 0/1/2 case winning over the CLDR category.
    /// </summary>
    public static T? PluralLogic<T>(
        double howMany,
        T? zero = default,
        T? one = default,
        T? two = default,
        T? few = default,
        T? many = default,
        T? other = default,
        string? locale = null,
        int? precision = null,
        bool useExplicitNumberCases = true)
        where T : class
    {
        double truncated = Math.Truncate(howMany);
        if (precision == null && truncated == howMany)
        {
            howMany = truncated;
        }

        if (useExplicitNumberCases && precision is null or 0)
        {
            if (howMany == 0 && zero != null)
            {
                return zero;
            }

            if (howMany == 1 && one != null)
            {
                return one;
            }

            if (howMany == 2 && two != null)
            {
                return two;
            }
        }

        string rule = VerifiedLocale(locale, PluralRules.LocaleHasPluralRules, _ => "default")!;
        return PluralRules.Select(rule, howMany, precision) switch
        {
            PluralCase.Zero => zero ?? other,
            PluralCase.One => one ?? other,
            PluralCase.Two => two ?? few ?? other,
            PluralCase.Few => few ?? other,
            PluralCase.Many => many ?? other,
            _ => other,
        };
    }

    /// <summary>
    /// Dart's <c>toBeginningOfSentenceCase</c>: upper-cases the first letter, leaving the rest alone.
    /// </summary>
    public static string? ToBeginningOfSentenceCase(string? input, string? locale = null)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return UpperCaseLetter(input[..1], locale) + input[1..];
    }

    /// Dart's `_upperCaseLetter`: a plain upper-casing plus the dotted i of Turkish and Azeri.
    private static string UpperCaseLetter(string input, string? locale)
    {
        if (locale != null
            && input == "i"
            && (locale.StartsWith("tr", StringComparison.Ordinal)
                || locale.StartsWith("az", StringComparison.Ordinal)))
        {
            return "\u0130";
        }

        return input.ToUpperInvariant();
    }

    private static int LanguageSeparatorIndex(string locale)
    {
        if (locale.Length < 3)
        {
            return -1;
        }

        if (locale[2] is '-' or '_')
        {
            return 2;
        }

        if (locale.Length < 4)
        {
            return -1;
        }

        return locale[3] is '-' or '_' ? 3 : -1;
    }

    private static string LanguageOnlyLocale(string locale)
    {
        if (locale == "invalid")
        {
            return "in";
        }

        if (locale.Length < 2)
        {
            return locale;
        }

        int separator = LanguageSeparatorIndex(locale);
        if (separator == -1)
        {
            return locale.Length < 4 ? locale.ToLowerInvariant() : locale;
        }

        return locale[..separator].ToLowerInvariant();
    }

    private static string LanguageRegionOnlyLocale(string locale)
    {
        if (locale.Length < 10)
        {
            return locale;
        }

        int separator = LanguageSeparatorIndex(locale);
        if (separator == -1)
        {
            return locale;
        }

        string language = locale[..separator];
        string subtags = locale[(separator + 1)..];
        string region = subtags[(ScriptSeparatorIndex(subtags) + 1)..];
        if (region.Length <= 3)
        {
            region = region.ToUpperInvariant();
        }

        return language + "_" + region;
    }

    private static int ScriptSeparatorIndex(string region)
    {
        if (region.Length < 5)
        {
            return -1;
        }

        return region[4] is '-' or '_' ? 4 : -1;
    }

    /// Dart's `deprecatedLocale`: the other half of a current/deprecated language-code pair.
    private static string DeprecatedLocale(string locale) => locale switch
    {
        "iw" => "he",
        "he" => "iw",
        "fil" => "tl",
        "tl" => "fil",
        "id" => "in",
        "in" => "id",
        "no" => "nb",
        "nb" => "no",
        _ => locale,
    };
}
