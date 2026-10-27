using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using Plumix.UI;
using JavaLocale = Java.Util.Locale;
using Locale = Plumix.Widgets.Locale;

// C#-only host infrastructure parity sources:
// flutter/engine/src/flutter/shell/platform/android/io/flutter/plugin/platform/PlatformPlugin.java
// (playSystemSound, vibrateHapticFeedback) and
// flutter/engine/src/flutter/shell/platform/android/io/flutter/plugin/localization/LocalizationPlugin.java
// (resolveNativeLocale) with FlutterJNI.computePlatformResolvedLocale's conversions.

namespace Plumix.Android;

/// <summary>The Android engine's platform plugin answers that need an Activity.</summary>
internal static class AndroidPlatformPlugin
{
    /// <summary><c>PlatformPlugin.playSystemSound</c>: the decor view's click sound, for clicks only.</summary>
    public static void PlaySystemSound(Activity activity, SystemSoundType type)
    {
        if (type == SystemSoundType.Click && activity.Window?.DecorView is { } view)
        {
            view.PlaySoundEffect(SoundEffects.Click);
        }
    }

    /// <summary>
    /// <c>PlatformPlugin.vibrateHapticFeedback</c>: the decor view's haptic feedback constant for Dart's
    /// <c>HapticFeedbackType</c> (<see langword="null"/> is the standard long press); the notification
    /// types need Android 11.
    /// </summary>
    public static void VibrateHapticFeedback(Activity activity, string? type)
    {
        if (activity.Window?.DecorView is not { } view)
        {
            return;
        }

        bool api30 = OperatingSystem.IsAndroidVersionAtLeast(30);
        FeedbackConstants? constant = type switch
        {
            null => FeedbackConstants.LongPress,
            "HapticFeedbackType.lightImpact" => FeedbackConstants.VirtualKey,
            "HapticFeedbackType.mediumImpact" => FeedbackConstants.KeyboardTap,
            "HapticFeedbackType.heavyImpact" => FeedbackConstants.ContextClick,
            "HapticFeedbackType.selectionClick" => FeedbackConstants.ClockTick,
            "HapticFeedbackType.successNotification" when api30 => FeedbackConstants.Confirm,
            "HapticFeedbackType.warningNotification" when api30 => FeedbackConstants.KeyboardTap,
            "HapticFeedbackType.errorNotification" when api30 => FeedbackConstants.Reject,
            _ => null,
        };
        if (constant is { } feedback)
        {
            view.PerformHapticFeedback(feedback);
        }
    }

    /// <summary>
    /// <c>LocalizationPlugin.resolveNativeLocale</c>: the supported locale that best matches the
    /// configuration's locale list (<c>Locale.lookup</c> over language ranges on Android 8+, a manual
    /// match below), else the first supported locale; null for no supported locales.
    /// </summary>
    public static Locale? ResolveNativeLocale(Context context, IReadOnlyList<Locale> supportedLocales)
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

        List<JavaLocale> preferred = ReadPreferredLocales(context.Resources?.Configuration);
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            List<JavaLocale.LanguageRange> languageRanges = [];
            foreach (JavaLocale locale in preferred)
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

            if (JavaLocale.Lookup(languageRanges, supported) is { } platformResolvedLocale)
            {
                return ToFrameworkLocale(platformResolvedLocale);
            }
        }
        else
        {
            foreach (JavaLocale preferredLocale in preferred)
            {
                // Exact match, then a supported language-only tag, then any locale of the language.
                JavaLocale? match = supported.FirstOrDefault(preferredLocale.Equals)
                    ?? supported.FirstOrDefault(locale => preferredLocale.Language == locale.ToLanguageTag())
                    ?? supported.FirstOrDefault(locale => preferredLocale.Language == locale.Language);
                if (match is not null)
                {
                    return ToFrameworkLocale(match);
                }
            }
        }

        return ToFrameworkLocale(supported[0]);
    }

    // The configuration's locale list; Android 6 has a single locale.
    private static List<JavaLocale> ReadPreferredLocales(Configuration? configuration)
    {
        List<JavaLocale> locales = [];
        if (configuration is null)
        {
            return locales;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(24))
        {
            LocaleList localeList = configuration.Locales;
            for (int index = 0; index < localeList.Size(); index++)
            {
                if (localeList.Get(index) is { } locale)
                {
                    locales.Add(locale);
                }
            }
        }
        else
        {
#pragma warning disable CA1422 // Configuration.locale is the API 23 source of the locale.
            if (configuration.Locale is { } locale)
            {
                locales.Add(locale);
            }
#pragma warning restore CA1422
        }

        return locales;
    }

    // dart:ui's computePlatformResolvedLocale: the engine's language, country and script strings, with
    // empty ones as null.
    private static Locale? ToFrameworkLocale(JavaLocale locale) =>
        string.IsNullOrEmpty(locale.Language) ? null : new Locale(locale.Language, locale.Country, locale.Script);
}
