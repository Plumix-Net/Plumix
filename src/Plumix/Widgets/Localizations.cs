using System.Globalization;
using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/localizations.dart
// flutter/packages/flutter/lib/src/widgets/widgets_localizations.dart

public sealed record Locale
{
    public Locale(
        string languageCode,
        string? countryCode = null,
        string? scriptCode = null)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            throw new ArgumentException("Language code cannot be null or whitespace.", nameof(languageCode));
        }

        LanguageCode = languageCode.ToLowerInvariant();
        CountryCode = string.IsNullOrWhiteSpace(countryCode) ? null : countryCode.ToUpperInvariant();
        ScriptCode = NormalizeScriptCode(scriptCode);
    }

    public string LanguageCode { get; }

    public string? CountryCode { get; }

    public string? ScriptCode { get; }

    public string Name
    {
        get
        {
            var parts = new List<string> { LanguageCode };
            if (ScriptCode != null)
            {
                parts.Add(ScriptCode);
            }

            if (CountryCode != null)
            {
                parts.Add(CountryCode);
            }

            return string.Join("-", parts);
        }
    }

    public static Locale FromCultureInfo(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        string[] parts = culture.Name.Split('-', StringSplitOptions.RemoveEmptyEntries);
        string languageCode = parts.Length == 0 ? "en" : parts[0];
        string? scriptCode = parts.FirstOrDefault(
            part => part.Length == 4 && part.All(char.IsLetter));
        string? countryCode = parts.FirstOrDefault(
            part => part.Length is 2 or 3
                    && !string.Equals(part, languageCode, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(part, scriptCode, StringComparison.OrdinalIgnoreCase));
        return new Locale(languageCode, countryCode, scriptCode);
    }

    public CultureInfo ToCultureInfo()
    {
        try
        {
            return CultureInfo.GetCultureInfo(Name);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo(LanguageCode);
        }
    }

    public override string ToString() => Name;

    private static string? NormalizeScriptCode(string? scriptCode)
    {
        if (string.IsNullOrWhiteSpace(scriptCode))
        {
            return null;
        }

        string normalized = scriptCode.ToLowerInvariant();
        return char.ToUpperInvariant(normalized[0]) + normalized[1..];
    }
}

public delegate Locale? LocaleListResolutionCallback(
    IReadOnlyList<Locale>? locales,
    IReadOnlyList<Locale> supportedLocales);

public delegate Locale? LocaleResolutionCallback(
    Locale? locale,
    IReadOnlyList<Locale> supportedLocales);

public abstract class LocalizationsDelegate
{
    public abstract Type ResourceType { get; }

    public abstract bool IsSupported(Locale locale);

    public abstract object Load(Locale locale);

    public abstract bool ShouldReload(LocalizationsDelegate oldDelegate);
}

public abstract class LocalizationsDelegate<T> : LocalizationsDelegate where T : class
{
    public sealed override Type ResourceType => typeof(T);

    public abstract T LoadTyped(Locale locale);

    public sealed override object Load(Locale locale) => LoadTyped(locale);
}

public abstract class WidgetsLocalizations
{
    public abstract TextDirection TextDirection { get; }

    public static WidgetsLocalizations Of(BuildContext context)
    {
        return Localizations.Of<WidgetsLocalizations>(context);
    }
}

public sealed class DefaultWidgetsLocalizations : WidgetsLocalizations
{
    private DefaultWidgetsLocalizations(TextDirection textDirection)
    {
        TextDirection = textDirection;
    }

    public static LocalizationsDelegate<WidgetsLocalizations> Delegate { get; } =
        new DefaultWidgetsLocalizationsDelegate();

    public override TextDirection TextDirection { get; }

    private sealed class DefaultWidgetsLocalizationsDelegate : LocalizationsDelegate<WidgetsLocalizations>
    {
        private static readonly IReadOnlySet<string> RtlLanguages = new HashSet<string>(
            ["ar", "fa", "he", "ps", "sd", "ur"],
            StringComparer.OrdinalIgnoreCase);

        public override bool IsSupported(Locale locale) => true;

        public override WidgetsLocalizations LoadTyped(Locale locale)
        {
            TextDirection textDirection = RtlLanguages.Contains(locale.LanguageCode)
                ? TextDirection.Rtl
                : TextDirection.Ltr;
            return new DefaultWidgetsLocalizations(textDirection);
        }

        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;
    }
}

public sealed class Localizations : StatefulWidget
{
    public Localizations(
        Locale locale,
        IEnumerable<LocalizationsDelegate> delegates,
        Widget child,
        bool isApplicationLevel = false,
        Key? key = null) : base(key)
    {
        Locale = locale ?? throw new ArgumentNullException(nameof(locale));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        IsApplicationLevel = isApplicationLevel;
        Delegates = (delegates ?? throw new ArgumentNullException(nameof(delegates))).ToArray();
        if (!Delegates.Any(
                localizationsDelegate =>
                    localizationsDelegate.ResourceType == typeof(WidgetsLocalizations)))
        {
            throw new ArgumentException(
                "Localizations requires a WidgetsLocalizations delegate.",
                nameof(delegates));
        }
    }

    public Locale Locale { get; }

    public IReadOnlyList<LocalizationsDelegate> Delegates { get; }

    public Widget Child { get; }

    public bool IsApplicationLevel { get; }

    public static Localizations Override(
        BuildContext context,
        Widget child,
        Locale? locale = null,
        IReadOnlyList<LocalizationsDelegate>? delegates = null,
        Key? key = null)
    {
        LocalizationsScope scope = context.DependOnInherited<LocalizationsScope>()
                                   ?? throw new InvalidOperationException(
                                       "Localizations.Override requires a Localizations ancestor.");
        var mergedDelegates = new List<LocalizationsDelegate>();
        if (delegates != null)
        {
            mergedDelegates.AddRange(delegates);
        }

        mergedDelegates.AddRange(scope.Delegates);
        return new Localizations(
            locale: locale ?? scope.Locale,
            delegates: mergedDelegates,
            child: child,
            key: key);
    }

    public static T Of<T>(BuildContext context) where T : class
    {
        return MaybeOf<T>(context)
               ?? throw new InvalidOperationException(
                   $"No localization resource of type {typeof(T).Name} was found.");
    }

    public static T? MaybeOf<T>(BuildContext context) where T : class
    {
        LocalizationsScope? scope = context.DependOnInherited<LocalizationsScope>();
        return scope?.Resources.GetValueOrDefault(typeof(T)) as T;
    }

    public static Locale LocaleOf(BuildContext context)
    {
        return MaybeLocaleOf(context)
               ?? throw new InvalidOperationException("No Localizations ancestor was found.");
    }

    public static Locale? MaybeLocaleOf(BuildContext context)
    {
        return context.DependOnInherited<LocalizationsScope>()?.Locale;
    }

    public override State CreateState() => new LocalizationsState();

    private sealed class LocalizationsState : State
    {
        private IReadOnlyDictionary<Type, object> _resources =
            new Dictionary<Type, object>();
        private Locale _locale = null!;

        private Localizations CurrentWidget => (Localizations)StateWidget;

        public override void InitState()
        {
            Load(CurrentWidget.Locale);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldLocalizations = (Localizations)oldWidget;
            if (!Equals(CurrentWidget.Locale, oldLocalizations.Locale)
                || AnyDelegatesShouldReload(oldLocalizations))
            {
                Load(CurrentWidget.Locale);
            }
        }

        public override Widget Build(BuildContext context)
        {
            WidgetsLocalizations widgetsLocalizations =
                (WidgetsLocalizations)_resources[typeof(WidgetsLocalizations)];
            return new LocalizationsScope(
                locale: _locale,
                delegates: CurrentWidget.Delegates,
                resources: _resources,
                child: new Directionality(
                    textDirection: widgetsLocalizations.TextDirection,
                    child: CurrentWidget.Child));
        }

        private bool AnyDelegatesShouldReload(Localizations oldWidget)
        {
            if (CurrentWidget.Delegates.Count != oldWidget.Delegates.Count)
            {
                return true;
            }

            for (int index = 0; index < CurrentWidget.Delegates.Count; index += 1)
            {
                LocalizationsDelegate currentDelegate = CurrentWidget.Delegates[index];
                LocalizationsDelegate oldDelegate = oldWidget.Delegates[index];
                if (currentDelegate.GetType() != oldDelegate.GetType()
                    || currentDelegate.ShouldReload(oldDelegate))
                {
                    return true;
                }
            }

            return false;
        }

        private void Load(Locale locale)
        {
            var resources = new Dictionary<Type, object>();
            var loadedResourceTypes = new HashSet<Type>();
            foreach (LocalizationsDelegate localizationsDelegate in CurrentWidget.Delegates)
            {
                if (!localizationsDelegate.IsSupported(locale)
                    || !loadedResourceTypes.Add(localizationsDelegate.ResourceType))
                {
                    continue;
                }

                resources[localizationsDelegate.ResourceType] = localizationsDelegate.Load(locale);
            }

            if (!resources.ContainsKey(typeof(WidgetsLocalizations)))
            {
                throw new InvalidOperationException(
                    $"No WidgetsLocalizations delegate supports locale {locale}.");
            }

            _locale = locale;
            _resources = resources;
        }
    }

    public static Locale Resolve(
        IReadOnlyList<Locale>? preferredLocales,
        IReadOnlyList<Locale> supportedLocales,
        LocaleListResolutionCallback? localeListResolutionCallback = null,
        LocaleResolutionCallback? localeResolutionCallback = null)
    {
        ArgumentNullException.ThrowIfNull(supportedLocales);
        if (supportedLocales.Count == 0)
        {
            throw new ArgumentException("Supported locales cannot be empty.", nameof(supportedLocales));
        }

        Locale? resolved = localeListResolutionCallback?.Invoke(preferredLocales, supportedLocales);
        resolved ??= localeResolutionCallback?.Invoke(preferredLocales?.FirstOrDefault(), supportedLocales);
        return resolved ?? BasicLocaleListResolution(preferredLocales, supportedLocales);
    }

    public static Locale BasicLocaleListResolution(
        IReadOnlyList<Locale>? preferredLocales,
        IReadOnlyList<Locale> supportedLocales)
    {
        ArgumentNullException.ThrowIfNull(supportedLocales);
        if (supportedLocales.Count == 0)
        {
            throw new ArgumentException("Supported locales cannot be empty.", nameof(supportedLocales));
        }

        if (preferredLocales == null || preferredLocales.Count == 0)
        {
            return supportedLocales[0];
        }

        var allSupportedLocales = new Dictionary<string, Locale>(StringComparer.OrdinalIgnoreCase);
        var languageAndCountryLocales = new Dictionary<string, Locale>(StringComparer.OrdinalIgnoreCase);
        var languageAndScriptLocales = new Dictionary<string, Locale>(StringComparer.OrdinalIgnoreCase);
        var languageLocales = new Dictionary<string, Locale>(StringComparer.OrdinalIgnoreCase);
        var countryLocales = new Dictionary<string, Locale>(StringComparer.OrdinalIgnoreCase);
        foreach (Locale supportedLocale in supportedLocales)
        {
            allSupportedLocales.TryAdd(FullLocaleKey(supportedLocale), supportedLocale);
            languageAndScriptLocales.TryAdd(
                LanguageAndScriptKey(supportedLocale),
                supportedLocale);
            languageAndCountryLocales.TryAdd(
                LanguageAndCountryKey(supportedLocale),
                supportedLocale);
            languageLocales.TryAdd(supportedLocale.LanguageCode, supportedLocale);
            if (supportedLocale.CountryCode != null)
            {
                countryLocales.TryAdd(supportedLocale.CountryCode, supportedLocale);
            }
        }

        Locale? matchesLanguageCode = null;
        Locale? matchesCountryCode = null;
        for (int localeIndex = 0; localeIndex < preferredLocales.Count; localeIndex += 1)
        {
            Locale preferredLocale = preferredLocales[localeIndex];
            if (allSupportedLocales.ContainsKey(FullLocaleKey(preferredLocale)))
            {
                return preferredLocale;
            }

            if (preferredLocale.ScriptCode != null
                && languageAndScriptLocales.TryGetValue(
                    LanguageAndScriptKey(preferredLocale),
                    out Locale? languageAndScript))
            {
                return languageAndScript;
            }

            if (preferredLocale.CountryCode != null
                && languageAndCountryLocales.TryGetValue(
                    LanguageAndCountryKey(preferredLocale),
                    out Locale? languageAndCountry))
            {
                return languageAndCountry;
            }

            if (matchesLanguageCode != null)
            {
                return matchesLanguageCode;
            }

            if (languageLocales.TryGetValue(preferredLocale.LanguageCode, out Locale? language))
            {
                matchesLanguageCode = language;
                bool nextLocaleHasSameLanguage =
                    localeIndex + 1 < preferredLocales.Count
                    && string.Equals(
                        preferredLocales[localeIndex + 1].LanguageCode,
                        preferredLocale.LanguageCode,
                        StringComparison.OrdinalIgnoreCase);
                if (localeIndex == 0 && !nextLocaleHasSameLanguage)
                {
                    return matchesLanguageCode;
                }
            }

            if (matchesCountryCode == null
                && preferredLocale.CountryCode != null
                && countryLocales.TryGetValue(preferredLocale.CountryCode, out Locale? country))
            {
                matchesCountryCode = country;
            }
        }

        return matchesLanguageCode ?? matchesCountryCode ?? supportedLocales[0];
    }

    private static string FullLocaleKey(Locale locale)
    {
        return $"{locale.LanguageCode}_{locale.ScriptCode}_{locale.CountryCode}";
    }

    private static string LanguageAndScriptKey(Locale locale)
    {
        return $"{locale.LanguageCode}_{locale.ScriptCode}";
    }

    private static string LanguageAndCountryKey(Locale locale)
    {
        return $"{locale.LanguageCode}_{locale.CountryCode}";
    }
}

internal sealed class LocalizationsScope : InheritedWidget
{
    public LocalizationsScope(
        Locale locale,
        IReadOnlyList<LocalizationsDelegate> delegates,
        IReadOnlyDictionary<Type, object> resources,
        Widget child,
        Key? key = null) : base(key)
    {
        Locale = locale;
        Delegates = delegates;
        Resources = resources;
        Child = child;
    }

    public Locale Locale { get; }

    public IReadOnlyList<LocalizationsDelegate> Delegates { get; }

    public IReadOnlyDictionary<Type, object> Resources { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldScope = (LocalizationsScope)oldWidget;
        return !Equals(oldScope.Locale, Locale)
               || !ReferenceEquals(oldScope.Resources, Resources);
    }
}
