using Android.App;
using Android.Content.Res;
using Android.Util;
using Plumix.UI;
using Locale = Plumix.Widgets.Locale;
using AndroidViewConfiguration = Android.Views.ViewConfiguration;
using JavaLocale = Java.Util.Locale;

namespace Plumix.Impeller.Android;

// C#-only host infrastructure parity sources:
// flutter/engine/src/flutter/shell/platform/android/io/flutter/embedding/android/FlutterView.java
// (sendUserSettingsToFlutter: text scale factor with the Android 14 font scaling, 24-hour format, platform
// brightness; the viewport metrics' physicalTouchSlop),
// .../io/flutter/plugin/localization/LocalizationPlugin.java (sendLocalesToFlutter, resolveNativeLocale) and
// FlutterJNI.computePlatformResolvedLocale's conversions. Ported from Plumix.Android's PlumixMainActivity and
// AndroidPlatformPlugin, which do the same over the Avalonia host.

/// <summary>Reports the Activity's configuration to the framework, as the engine's settings and locales.</summary>
internal static class AndroidSystemSettings
{
    /// <summary>Reports everything the configuration carries; the Activity calls it on create and on changes.</summary>
    public static void Report(Activity activity, ImpellerHost host, Configuration? configuration)
    {
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        dispatcher.UpdateTextScaleFactor(configuration?.FontScale ?? 1f, CreateScaledFontSize(activity));
        if (configuration is not null)
        {
            dispatcher.UpdateLocales(ReadLocales(configuration));
        }

        dispatcher.PlatformLocaleResolver = supported => ResolveNativeLocale(activity, supported);
        bool night = configuration is not null
                     && (configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;
        double? touchSlop = AndroidViewConfiguration.Get(activity)?.ScaledTouchSlop;
        host.SettingsChanged(
            night ? Brightness.Dark : Brightness.Light,
            global::Android.Text.Format.DateFormat.Is24HourFormat(activity),
            touchSlop);
    }

    // FlutterJNI.getScaledFontSize over the DisplayMetrics of the configuration being reported.
    private static Func<double, double>? CreateScaledFontSize(Activity activity)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(34) || activity.Resources?.DisplayMetrics is not { } current)
        {
            return null;
        }

        var metrics = new DisplayMetrics();
        metrics.SetTo(current);
        return fontSize => TypedValue.ApplyDimension(ComplexUnitType.Sp, (float)fontSize, metrics) / metrics.Density;
    }

    private static List<Locale> ReadLocales(Configuration configuration)
    {
        List<Locale> locales = [];
        foreach (JavaLocale locale in PreferredLocales(configuration))
        {
            // dart:ui's _updateLocales: empty country and script codes are null; the root locale is skipped.
            if (!string.IsNullOrEmpty(locale.Language))
            {
                locales.Add(new Locale(locale.Language, locale.Country, locale.Script));
            }
        }

        return locales;
    }

    /// <summary>
    /// <c>LocalizationPlugin.resolveNativeLocale</c>: the supported locale that best matches the configuration's
    /// locale list (<c>Locale.lookup</c> over language ranges), else the first supported locale.
    /// </summary>
    private static Locale? ResolveNativeLocale(Activity activity, IReadOnlyList<Locale> supportedLocales)
    {
        if (supportedLocales.Count == 0)
        {
            return null;
        }

        // FlutterJNI.computePlatformResolvedLocale: each supported locale through Locale.Builder.
        List<JavaLocale> supported = [];
        foreach (Locale locale in supportedLocales)
        {
            var builder = new JavaLocale.Builder();
            builder.SetLanguage(locale.LanguageCode);
            if (locale.CountryCode is { } countryCode)
            {
                builder.SetRegion(countryCode);
            }

            if (locale.ScriptCode is { } scriptCode)
            {
                builder.SetScript(scriptCode);
            }

            supported.Add(builder.Build()!);
        }

        List<JavaLocale.LanguageRange> languageRanges = [];
        foreach (JavaLocale locale in PreferredLocales(activity.Resources?.Configuration))
        {
            string fullRange = locale.Language;
            if (!string.IsNullOrEmpty(locale.Script))
            {
                fullRange += "-" + locale.Script;
            }

            if (!string.IsNullOrEmpty(locale.Country))
            {
                fullRange += "-" + locale.Country;
            }

            languageRanges.Add(new JavaLocale.LanguageRange(fullRange));
            languageRanges.Add(new JavaLocale.LanguageRange(locale.Language));
            languageRanges.Add(new JavaLocale.LanguageRange(locale.Language + "-*"));
        }

        JavaLocale resolved = JavaLocale.Lookup(languageRanges, supported) ?? supported[0];
        return string.IsNullOrEmpty(resolved.Language)
            ? null
            : new Locale(resolved.Language, resolved.Country, resolved.Script);
    }

    private static IEnumerable<JavaLocale> PreferredLocales(Configuration? configuration)
    {
        if (configuration is null)
        {
            yield break;
        }

        global::Android.OS.LocaleList localeList = configuration.Locales;
        for (int index = 0; index < localeList.Size(); index++)
        {
            if (localeList.Get(index) is { } locale)
            {
                yield return locale;
            }
        }
    }
}
