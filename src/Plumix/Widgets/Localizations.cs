using System.Globalization;
using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/localizations.dart

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

    /// <summary>The BCP-47 language tag of this locale.</summary>
    /// <remarks>Dart's <c>Locale.toLanguageTag()</c>; identical to <see cref="Name"/>.</remarks>
    public string ToLanguageTag() => Name;

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

    /// <summary>Loads resources synchronously or asynchronously, as Dart's delegate future does.</summary>
    /// <remarks>Direct values load synchronously; Task-backed results notify in a microtask.</remarks>
    public virtual ValueTask<object> LoadAsync(Locale locale) => new(Load(locale));

    // ValueTask equality retains its backing-object identity: only a directly supplied result is
    // Dart's SynchronousFuture. A completed Task still delivers its listeners in a microtask.
    // Callers Preserve first so reading Result cannot consume an IValueTaskSource twice.
    internal static bool IsSynchronous<TValue>(ValueTask<TValue> result) where TValue : class =>
        result.IsCompletedSuccessfully && result.Equals(new ValueTask<TValue>(result.Result));

    public abstract bool ShouldReload(LocalizationsDelegate oldDelegate);

    public override string ToString() => $"{GetType().Name}[{ResourceType.Name}]";
}

public abstract class LocalizationsDelegate<T> : LocalizationsDelegate where T : class
{
    public override Type ResourceType => typeof(T);

    /// <summary>Loads synchronous resources. Async delegates override LoadTypedAsync instead.</summary>
    public virtual T LoadTyped(Locale locale) => throw new NotSupportedException(
        "This localization delegate supports asynchronous loading only. Use LoadTypedAsync.");

    public virtual ValueTask<T> LoadTypedAsync(Locale locale) => new(LoadTyped(locale));

    public sealed override object Load(Locale locale) => LoadTyped(locale);

    public sealed override ValueTask<object> LoadAsync(Locale locale)
    {
        ValueTask<T> result = LoadTypedAsync(locale).Preserve();
        return IsSynchronous(result)
            ? new ValueTask<object>(result.Result)
            : new ValueTask<object>(BoxAsync(result));
    }

    private static async Task<object> BoxAsync(ValueTask<T> result) => await result;
}

public abstract class WidgetsLocalizations
{
    public abstract TextDirection TextDirection { get; }

    public virtual string SearchResultsFound => "Search results found";

    public virtual string NoResultsFound => "No results found";

    public abstract string CopyButtonLabel { get; }

    public abstract string CutButtonLabel { get; }

    public abstract string PasteButtonLabel { get; }

    public abstract string SelectAllButtonLabel { get; }

    public abstract string LookUpButtonLabel { get; }

    public abstract string SearchWebButtonLabel { get; }

    public abstract string ShareButtonLabel { get; }

    public abstract string RadioButtonUnselectedLabel { get; }

    public abstract string ReorderItemToStart { get; }

    public abstract string ReorderItemToEnd { get; }

    public abstract string ReorderItemUp { get; }

    public abstract string ReorderItemDown { get; }

    public abstract string ReorderItemLeft { get; }

    public abstract string ReorderItemRight { get; }

    public static WidgetsLocalizations Of(BuildContext context)
    {
        WidgetsDebug.DebugCheckHasWidgetsLocalizations(context);
        return Localizations.Of<WidgetsLocalizations>(context);
    }
}

public class DefaultWidgetsLocalizations : WidgetsLocalizations
{
    public DefaultWidgetsLocalizations()
    {
    }

    public static LocalizationsDelegate<WidgetsLocalizations> Delegate { get; } =
        new DefaultWidgetsLocalizationsDelegate();

    public override TextDirection TextDirection => TextDirection.Ltr;

    public override string CopyButtonLabel => "Copy";

    public override string CutButtonLabel => "Cut";

    public override string PasteButtonLabel => "Paste";

    public override string SelectAllButtonLabel => "Select all";

    public override string LookUpButtonLabel => "Look Up";

    public override string SearchWebButtonLabel => "Search Web";

    public override string ShareButtonLabel => "Share";

    public override string RadioButtonUnselectedLabel => "Not selected";

    public override string ReorderItemToStart => "Move to the start";

    public override string ReorderItemToEnd => "Move to the end";

    public override string ReorderItemUp => "Move up";

    public override string ReorderItemDown => "Move down";

    public override string ReorderItemLeft => "Move left";

    public override string ReorderItemRight => "Move right";

    public static WidgetsLocalizations Load(Locale locale) => new DefaultWidgetsLocalizations();

    private sealed class DefaultWidgetsLocalizationsDelegate : LocalizationsDelegate<WidgetsLocalizations>
    {
        public override bool IsSupported(Locale locale) => true;

        public override WidgetsLocalizations LoadTyped(Locale locale) =>
            DefaultWidgetsLocalizations.Load(locale);

        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;

        public override string ToString() => "DefaultWidgetsLocalizations.delegate(en_US)";
    }
}

public sealed class Localizations : StatefulWidget
{
    public Localizations(
        Locale locale,
        IEnumerable<LocalizationsDelegate> delegates,
        Widget? child = null,
        bool isApplicationLevel = false,
        Key? key = null) : base(key)
    {
        Locale = locale ?? throw new ArgumentNullException(nameof(locale));
        Child = child;
        IsApplicationLevel = isApplicationLevel;
        Delegates = (delegates ?? throw new ArgumentNullException(nameof(delegates))).ToArray();
        if (!Delegates.Any(
                localizationsDelegate =>
                    localizationsDelegate is LocalizationsDelegate<WidgetsLocalizations>))
        {
            throw new ArgumentException(
                "Localizations requires a WidgetsLocalizations delegate.",
                nameof(delegates));
        }
    }

    public Locale Locale { get; }

    public IReadOnlyList<LocalizationsDelegate> Delegates { get; }

    public Widget? Child { get; }

    public bool IsApplicationLevel { get; }

    public static Localizations Override(
        BuildContext context,
        Widget? child = null,
        Locale? locale = null,
        IReadOnlyList<LocalizationsDelegate>? delegates = null,
        Key? key = null)
    {
        LocalizationsScope scope = context.DependOnInheritedWidgetOfExactType<LocalizationsScope>()
                                   ?? throw new InvalidOperationException(
                                       "Localizations.Override requires a Localizations ancestor.");
        var mergedDelegates = new List<LocalizationsDelegate>();
        if (delegates != null)
        {
            mergedDelegates.AddRange(delegates);
        }

        mergedDelegates.AddRange(scope.State.CurrentWidget.Delegates);
        return new Localizations(
            locale: locale ?? scope.State.Locale!,
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
        => Of<T>(context, typeof(T));

    /// <summary>Looks up resources by delegate type, returning null when the scope has none.</summary>
    public static T? Of<T>(BuildContext context, Type type) where T : class
    {
        LocalizationsScope? scope = context.DependOnInheritedWidgetOfExactType<LocalizationsScope>();
        return (T?)(scope?.State.Resources.GetValueOrDefault(type));
    }

    public static Locale LocaleOf(BuildContext context)
    {
        Locale? locale = MaybeLocaleOf(context);
        if (locale is null && Constants.KDebugMode)
        {
            throw new FlutterError(
                "Requested the Locale of a context that does not include a Localizations ancestor.\n"
                + "To request the Locale, the context used to retrieve the Localizations widget must "
                + "be that of a widget that is a descendant of a Localizations widget.");
        }

        return locale ?? throw new InvalidOperationException("No Localizations ancestor was found.");
    }

    public static Locale? MaybeLocaleOf(BuildContext context)
    {
        return context.DependOnInheritedWidgetOfExactType<LocalizationsScope>()?.State.Locale;
    }

    public override State CreateState() => new LocalizationsState();

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Locale>("locale", Locale));
        properties.Add(new IterableProperty<LocalizationsDelegate>("delegates", Delegates));
    }

    private sealed class LocalizationsState : State<Localizations>
    {
        private readonly GlobalKey _localizedResourcesScopeKey = new LabeledGlobalKey<State>(null);
        private IReadOnlyDictionary<Type, object> _resources =
            new Dictionary<Type, object>();
        private Locale? _locale;

        public Locale? Locale
        {
            get => _locale;
            private set
            {
                if (Equals(_locale, value))
                {
                    return;
                }

                PlatformDispatcher.Instance.SetApplicationLocale(value!);
                _locale = value;
            }
        }

        public Localizations CurrentWidget => (Localizations)StateWidget;

        public IReadOnlyDictionary<Type, object> Resources => _resources;

        public override void InitState()
        {
            base.InitState();
            Load(CurrentWidget.Locale);
        }

        public override void DidUpdateWidget(Localizations oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            var oldLocalizations = (Localizations)oldWidget;
            if (!Equals(CurrentWidget.Locale, oldLocalizations.Locale)
                || AnyDelegatesShouldReload(oldLocalizations))
            {
                Load(CurrentWidget.Locale);
            }
        }

        public override Widget Build(BuildContext context)
        {
            if (_locale is null)
            {
                return new SizedBox(width: 0.0, height: 0.0);
            }

            WidgetsLocalizations widgetsLocalizations =
                (WidgetsLocalizations)_resources[typeof(WidgetsLocalizations)];

            // A non-application-level `Localizations` marks its semantics subtree with the locale,
            // so assistive technologies read that subtree in it.
            return new Semantics(
                localeForSubtree: CurrentWidget.IsApplicationLevel ? null : CurrentWidget.Locale,
                container: !CurrentWidget.IsApplicationLevel,
                textDirection: widgetsLocalizations.TextDirection,
                child: new LocalizationsScope(
                    state: this,
                    resources: _resources,
                    key: _localizedResourcesScopeKey,
                    child: new Directionality(
                        textDirection: widgetsLocalizations.TextDirection,
                        child: CurrentWidget.Child!)));
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
            ValueTask<IReadOnlyDictionary<Type, object>> result = LoadAll(locale, CurrentWidget.Delegates);
            if (LocalizationsDelegate.IsSynchronous(result))
            {
                _resources = result.Result;
                Locale = locale;
                return;
            }

            RendererBinding.Instance.DeferFirstFrame();
            EventLoop.ScheduleMicrotask(() => EventLoop.RunAsync(async () =>
            {
                IReadOnlyDictionary<Type, object> resources;
                try
                {
                    resources = await result;
                }
                catch (Exception exception)
                {
                    // Dart forwards failed futures to the isolate's uncaught-error handler. There is no
                    // successful load to release this deferral, so preserve it and report through ours.
                    FlutterError.ReportError(new FlutterErrorDetails(
                        exception: exception,
                        stack: exception.StackTrace,
                        library: "widgets library",
                        context: new ErrorDescription("while loading localizations")));
                    return;
                }

                if (Mounted)
                {
                    SetState(() =>
                    {
                        _resources = resources;
                        Locale = locale;
                    });
                }

                RendererBinding.Instance.AllowFirstFrame();
            }));
        }

        private static ValueTask<IReadOnlyDictionary<Type, object>> LoadAll(
            Locale locale,
            IReadOnlyList<LocalizationsDelegate> allDelegates)
        {
            var resources = new Dictionary<Type, object>();
            var loadedResourceTypes = new HashSet<Type>();
            var delegates = new List<LocalizationsDelegate>();
            foreach (LocalizationsDelegate localizationsDelegate in allDelegates)
            {
                if (!loadedResourceTypes.Contains(localizationsDelegate.ResourceType)
                    && localizationsDelegate.IsSupported(locale))
                {
                    loadedResourceTypes.Add(localizationsDelegate.ResourceType);
                    delegates.Add(localizationsDelegate);
                }
            }

            var pending = new List<(Type Type, Task<object> Future)>();
            foreach (LocalizationsDelegate localizationsDelegate in delegates)
            {
                ValueTask<object> result = localizationsDelegate.LoadAsync(locale).Preserve();
                if (LocalizationsDelegate.IsSynchronous(result) && result.Result is not null)
                {
                    resources[localizationsDelegate.ResourceType] = result.Result;
                }
                else
                {
                    pending.Add((localizationsDelegate.ResourceType, result.AsTask()));
                }
            }

            return pending.Count == 0
                ? new ValueTask<IReadOnlyDictionary<Type, object>>(resources)
                : new ValueTask<IReadOnlyDictionary<Type, object>>(CompletePending());

            async Task<IReadOnlyDictionary<Type, object>> CompletePending()
            {
                object[] values = await Task.WhenAll(pending.Select(entry => entry.Future));
                for (int index = 0; index < pending.Count; index += 1)
                {
                    resources[pending[index].Type] = values[index];
                }

                return resources;
            }
        }
    }

    private sealed class LocalizationsScope : InheritedWidget
    {
        public LocalizationsScope(
            LocalizationsState state,
            IReadOnlyDictionary<Type, object> resources,
            Widget child,
            Key? key = null) : base(child, key)
        {
            State = state;
            Resources = resources;
        }

        public LocalizationsState State { get; }

        public IReadOnlyDictionary<Type, object> Resources { get; }

        public override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
            !ReferenceEquals(((LocalizationsScope)oldWidget).Resources, Resources);
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

/// <summary>A helper class used to manage localization resolution.</summary>
/// <remarks>
/// Flutter's <c>LocalizationsResolver</c>, which <see cref="WidgetsApp"/> uses to resolve its locale
/// from <see cref="PlatformDispatcher.Locales"/> and to follow the platform's locale changes.
/// </remarks>
public sealed class LocalizationsResolver : ChangeNotifier, WidgetsBindingObserver
{
    private Locale? _locale;
    private LocaleListResolutionCallback? _localeListResolutionCallback;
    private LocaleResolutionCallback? _localeResolutionCallback;
    private IReadOnlyList<LocalizationsDelegate>? _localizationsDelegates;
    private IReadOnlyList<Locale> _supportedLocales;

    // This is the resolved locale, and is one of the supportedLocales.
    private Locale? _resolvedLocale;

    /// <summary>
    /// Creates a resolver that determines the best-fit locale from the set of
    /// <paramref name="supportedLocales"/>.
    /// </summary>
    /// <remarks>
    /// If provided, locale resolution will attempt to use <paramref name="locale"/> as the current locale
    /// rather than the system locale. Locale resolution behavior can be overridden by providing
    /// <paramref name="localeListResolutionCallback"/> or <paramref name="localeResolutionCallback"/>.
    /// </remarks>
    public LocalizationsResolver(
        IReadOnlyList<Locale> supportedLocales,
        Locale? locale = null,
        LocaleListResolutionCallback? localeListResolutionCallback = null,
        LocaleResolutionCallback? localeResolutionCallback = null,
        IReadOnlyList<LocalizationsDelegate>? localizationsDelegates = null)
    {
        ArgumentNullException.ThrowIfNull(supportedLocales);
        _locale = locale;
        _localeListResolutionCallback = localeListResolutionCallback;
        _localeResolutionCallback = localeResolutionCallback;
        _localizationsDelegates = localizationsDelegates;
        _supportedLocales = supportedLocales;
        _resolvedLocale = ResolveLocales(PlatformDispatcher.Instance.Locales, supportedLocales);
        WidgetsBinding.Instance.AddObserver(this);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _ = WidgetsBinding.Instance.RemoveObserver(this);
        base.Dispose();
    }

    /// <summary>
    /// Replace one or more of the properties used for localization resolution and re-resolve the locale.
    /// </summary>
    /// <remarks>
    /// Like Dart, the platform locales are re-resolved only when a different
    /// <paramref name="supportedLocales"/> instance is given.
    /// </remarks>
    public void Update(
        Locale? locale,
        LocaleListResolutionCallback? localeListResolutionCallback,
        LocaleResolutionCallback? localeResolutionCallback,
        IReadOnlyList<LocalizationsDelegate>? localizationsDelegates,
        IReadOnlyList<Locale> supportedLocales)
    {
        ArgumentNullException.ThrowIfNull(supportedLocales);
        _locale = locale;
        _localeListResolutionCallback = localeListResolutionCallback;
        _localeResolutionCallback = localeResolutionCallback;
        _localizationsDelegates = localizationsDelegates;
        if (!ReferenceEquals(_supportedLocales, supportedLocales))
        {
            _supportedLocales = supportedLocales;
            UpdateResolvedLocale(PlatformDispatcher.Instance.Locales);
        }
    }

    /// <summary>
    /// The currently resolved <see cref="Widgets.Locale"/> based on the current platform locale and the
    /// provided set of <see cref="SupportedLocales"/>.
    /// </summary>
    public Locale Locale
    {
        get
        {
            Locale appLocale = _locale != null
                ? ResolveLocales([_locale], SupportedLocales)
                : _resolvedLocale!;
            if (Foundation.Constants.KDebugMode)
            {
                DebugCheckLocalizations(appLocale);
            }

            return appLocale;
        }
    }

    /// <summary>
    /// The application's delegates followed by <see cref="DefaultWidgetsLocalizations.Delegate"/>; only the
    /// first delegate of a particular <see cref="LocalizationsDelegate.ResourceType"/> is loaded, so an
    /// application delegate can override the widgets one.
    /// </summary>
    public IReadOnlyList<LocalizationsDelegate> LocalizationsDelegates =>
        [.. _localizationsDelegates ?? [], DefaultWidgetsLocalizations.Delegate];

    /// <summary>The callback that resolves the application locale from the preferred list.</summary>
    public LocaleListResolutionCallback? LocaleListResolutionCallback => _localeListResolutionCallback;

    /// <summary>The callback that resolves the application locale from the first preferred locale.</summary>
    public LocaleResolutionCallback? LocaleResolutionCallback => _localeResolutionCallback;

    /// <summary>The locales the application has been localized for.</summary>
    public IReadOnlyList<Locale> SupportedLocales => _supportedLocales;

    /// <inheritdoc />
    public void DidChangeLocales(IReadOnlyList<Locale>? locales)
    {
        UpdateResolvedLocale(locales);
    }

    /// <inheritdoc />
    public override string ToString() => nameof(LocalizationsResolver);

    // Recompute the resolved locale based on preferredLocales and supportedLocales, update the resolved
    // locale, and notify listeners only if the resolved locale changed. Called from both Update (when
    // locale resolution parameters change) and DidChangeLocales (when system locales change).
    private void UpdateResolvedLocale(IReadOnlyList<Locale>? preferredLocales)
    {
        Locale newLocale = ResolveLocales(preferredLocales, SupportedLocales);
        if (newLocale != _resolvedLocale)
        {
            _resolvedLocale = newLocale;
            NotifyListeners();
        }
    }

    private Locale ResolveLocales(IReadOnlyList<Locale>? preferredLocales, IReadOnlyList<Locale> supportedLocales)
    {
        // Attempt to use localeListResolutionCallback.
        if (LocaleListResolutionCallback != null)
        {
            Locale? locale = LocaleListResolutionCallback(preferredLocales, supportedLocales);
            if (locale != null)
            {
                return locale;
            }
        }

        // localeListResolutionCallback failed, falling back to localeResolutionCallback.
        if (LocaleResolutionCallback != null)
        {
            Locale? locale = LocaleResolutionCallback(
                preferredLocales is { Count: > 0 } ? preferredLocales[0] : null,
                supportedLocales);
            if (locale != null)
            {
                return locale;
            }
        }

        // Both callbacks failed, falling back to default algorithm.
        return Localizations.BasicLocaleListResolution(preferredLocales, supportedLocales);
    }

    private void DebugCheckLocalizations(Locale locale)
    {
        var unsupportedTypes = new HashSet<Type>(LocalizationsDelegates.Select(static entry => entry.ResourceType));
        foreach (LocalizationsDelegate entry in LocalizationsDelegates)
        {
            if (!unsupportedTypes.Contains(entry.ResourceType))
            {
                continue;
            }

            if (entry.IsSupported(locale))
            {
                unsupportedTypes.Remove(entry.ResourceType);
            }
        }

        if (unsupportedTypes.Count == 0)
        {
            return;
        }

        FlutterError.ReportError(new FlutterErrorDetails(
            exception: $"Warning: This application's locale, {locale}, is not supported by all of its "
                       + "localization delegates.",
            library: "widgets",
            informationCollector: () => DebugUnsupportedLocalizationsInformation(locale, unsupportedTypes)));
    }

    private IEnumerable<DiagnosticsNode> DebugUnsupportedLocalizationsInformation(
        Locale locale,
        HashSet<Type> unsupportedTypes)
    {
        foreach (Type unsupportedType in unsupportedTypes)
        {
            yield return new ErrorDescription(
                $"• A {unsupportedType.Name} delegate that supports the {locale} locale was not found.");
        }

        yield return new ErrorSpacer();
        if (unsupportedTypes.Count == 1 && unsupportedTypes.Single().Name == "CupertinoLocalizations")
        {
            // We previously explicitly avoided checking for this class so it's not uncommon for applications
            // to have omitted importing the required delegate.
            yield return new ErrorHint(
                "If the application is built using GlobalMaterialLocalizations.delegate, consider using "
                + "GlobalMaterialLocalizations.delegates (plural) instead, as that will automatically declare "
                + "the appropriate Cupertino localizations.");
            yield return new ErrorSpacer();
        }

        yield return new ErrorHint(
            $"The declared supported locales for this app are: {string.Join(", ", SupportedLocales)}");
        yield return new ErrorSpacer();
        yield return new ErrorDescription(
            "See https://flutter.dev/to/internationalization/ for more "
            + "information about configuring an app's locale, supportedLocales, "
            + "and localizationsDelegates parameters.");
    }
}
