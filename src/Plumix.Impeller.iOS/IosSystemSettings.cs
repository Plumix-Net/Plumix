using System.Runtime.InteropServices;
using CoreFoundation;
using Foundation;
using ObjCRuntime;
using Plumix.UI;
using UIKit;
using Locale = Plumix.Widgets.Locale;

namespace Plumix.Impeller.iOS;

// C#-only host infrastructure parity sources:
// flutter/engine/src/flutter/shell/platform/darwin/ios/framework/Source/FlutterViewController.mm
// (onUserSettingsChanged:, textScaleFactor, brightnessMode, onAccessibilityStatusChanged:),
// .../AccessibilityFeatures.swift (flags and the observed notifications), .../FlutterEngine.mm (onLocaleUpdated:),
// flutter/engine/src/flutter/shell/platform/darwin/common/framework/Source/FlutterHourFormat.mm and
// .../ios/platform_view_ios.mm (ComputePlatformResolvedLocales).

/// <summary>The user settings, locales and accessibility features the iOS engine reports to the framework.</summary>
internal static class IosSystemSettings
{
    // The values of NSLocaleLanguageCode, NSLocaleCountryCode and NSLocaleScriptCode.
    private const string LanguageCodeKey = "kCFLocaleLanguageCodeKey";
    private const string CountryCodeKey = "kCFLocaleCountryCodeKey";
    private const string ScriptCodeKey = "kCFLocaleScriptCodeKey";

    // The feature bits the iOS engine reports (AccessibilityFeatureFlag without disableAnimations and noAnnounce,
    // which it never sets).
    private const int ReportedFeatures =
        AccessibilityFeatures.KAccessibleNavigationIndex
        | AccessibilityFeatures.KInvertColorsIndex
        | AccessibilityFeatures.KBoldTextIndex
        | AccessibilityFeatures.KReduceMotionIndex
        | AccessibilityFeatures.KHighContrastIndex
        | AccessibilityFeatures.KOnOffSwitchLabelsIndex
        | AccessibilityFeatures.KNoAutoPlayAnimatedImagesIndex
        | AccessibilityFeatures.KNoAutoPlayVideosIndex
        | AccessibilityFeatures.KDeterministicCursorIndex;

    /// <summary>The notifications after which the accessibility features are read again.</summary>
    public static IEnumerable<NSString> AccessibilityNotifications
    {
        get
        {
            yield return UIView.VoiceOverStatusDidChangeNotification;
            yield return UIView.SwitchControlStatusDidChangeNotification;
            yield return UIView.SpeakScreenStatusDidChangeNotification;
            yield return UIView.InvertColorsStatusDidChangeNotification;
            yield return UIView.ReduceMotionStatusDidChangeNotification;
            yield return UIView.BoldTextStatusDidChangeNotification;
            yield return UIView.DarkerSystemColorsStatusDidChangeNotification;
            yield return UIView.OnOffSwitchLabelsDidChangeNotification;
            yield return UIView.VideoAutoplayStatusDidChangeNotification;
            if (OperatingSystem.IsIOSVersionAtLeast(18))
            {
                yield return global::Accessibility.AXAnimatedImagesUtilities.AnimatedImagesEnabledDidChangeNotification;
                yield return global::Accessibility.AXPrefers.NonBlinkingTextInsertionIndicatorDidChangeNotification;
            }
        }
    }

    /// <summary>
    /// onUserSettingsChanged: text scale, 24-hour format and brightness of the view controller's traits.
    /// </summary>
    public static void ReportUserSettings(IosImpellerHost host, UITraitCollection traits)
    {
        PlatformDispatcher.Instance.UpdateTextScaleFactor(TextScaleFactor());
        host.SettingsChanged(
            traits.UserInterfaceStyle == UIUserInterfaceStyle.Dark ? Brightness.Dark : Brightness.Light,
            AlwaysUse24HourFormat());
    }

    /// <summary>onLocaleUpdated: the preferred languages in order, and the native locale resolution.</summary>
    public static void ReportLocales()
    {
        List<Locale> locales = [];
        foreach (string identifier in NSLocale.PreferredLanguages)
        {
            using var locale = new NSLocale(identifier);
            if (!string.IsNullOrEmpty(locale.LanguageCode))
            {
                locales.Add(new Locale(locale.LanguageCode, NullIfEmpty(locale.CountryCode),
                    NullIfEmpty(locale.ScriptCode)));
            }
        }

        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        dispatcher.PlatformLocaleResolver = ResolveLocale;
        if (locales.Count > 0)
        {
            dispatcher.UpdateLocales(locales);
        }
    }

    /// <summary>
    /// onAccessibilityStatusChanged: the feature flags; returns whether semantics should be on (always in the
    /// simulator, where the engine cannot tell whether the accessibility inspector runs).
    /// </summary>
    public static bool ReportAccessibilityFeatures()
    {
        int flags = 0;
        bool voiceOver = UIAccessibility.IsVoiceOverRunning;
        bool switchControl = UIAccessibility.IsSwitchControlRunning;
        flags |= voiceOver || switchControl ? AccessibilityFeatures.KAccessibleNavigationIndex : 0;
        flags |= UIAccessibility.IsInvertColorsEnabled ? AccessibilityFeatures.KInvertColorsIndex : 0;
        flags |= UIAccessibility.IsBoldTextEnabled ? AccessibilityFeatures.KBoldTextIndex : 0;
        flags |= UIAccessibility.IsReduceMotionEnabled ? AccessibilityFeatures.KReduceMotionIndex : 0;
        flags |= UIAccessibility.DarkerSystemColorsEnabled ? AccessibilityFeatures.KHighContrastIndex : 0;
        flags |= UIAccessibility.IsOnOffSwitchLabelsEnabled ? AccessibilityFeatures.KOnOffSwitchLabelsIndex : 0;
        flags |= OperatingSystem.IsIOSVersionAtLeast(18) && !AXAnimatedImagesEnabled()
            ? AccessibilityFeatures.KNoAutoPlayAnimatedImagesIndex
            : 0;
        flags |= !UIAccessibility.IsVideoAutoplayEnabled ? AccessibilityFeatures.KNoAutoPlayVideosIndex : 0;
        flags |= OperatingSystem.IsIOSVersionAtLeast(18) && AXPrefersNonBlinkingTextInsertionIndicator()
            ? AccessibilityFeatures.KDeterministicCursorIndex
            : 0;
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        dispatcher.UpdateAccessibilityFeatures((dispatcher.ReportedAccessibilityFeatures & ~ReportedFeatures) | flags);
        return ObjCRuntime.Runtime.Arch == Arch.SIMULATOR
               || voiceOver || switchControl || UIAccessibility.IsSpeakScreenEnabled;
    }

    // textScaleFactor: the Dynamic Type size's body text point size relative to the default Large (17pt), after
    // Apple's typography guidelines.
    private static double TextScaleFactor()
    {
        const double large = 17;
        NSString category = UIApplication.SharedApplication.PreferredContentSizeCategory;
        double body = category switch
        {
            _ when category == UIContentSizeCategory.ExtraSmall.GetConstant() => 14,
            _ when category == UIContentSizeCategory.Small.GetConstant() => 15,
            _ when category == UIContentSizeCategory.Medium.GetConstant() => 16,
            _ when category == UIContentSizeCategory.ExtraLarge.GetConstant() => 19,
            _ when category == UIContentSizeCategory.ExtraExtraLarge.GetConstant() => 21,
            _ when category == UIContentSizeCategory.ExtraExtraExtraLarge.GetConstant() => 23,
            _ when category == UIContentSizeCategory.AccessibilityMedium.GetConstant() => 28,
            _ when category == UIContentSizeCategory.AccessibilityLarge.GetConstant() => 33,
            _ when category == UIContentSizeCategory.AccessibilityExtraLarge.GetConstant() => 40,
            _ when category == UIContentSizeCategory.AccessibilityExtraExtraLarge.GetConstant() => 47,
            _ when category == UIContentSizeCategory.AccessibilityExtraExtraExtraLarge.GetConstant() => 53,
            _ => large,
        };
        return body / large;
    }

    // FlutterHourFormat.isAlwaysUse24HourFormat: iOS applies its 24-Hour Time setting only to formatters of the
    // current locale, so the hour template's pattern for that locale tells whether a day period is shown.
    private static bool AlwaysUse24HourFormat() =>
        NSDateFormatter.GetDateFormatFromTemplate("j", 0, NSLocale.CurrentLocale)?.Contains('a') != true;

    // PlatformViewIOS::ComputePlatformResolvedLocales: NSBundle's preferred localization among the supported locale
    // identifiers, split again into NSLocale's components.
    private static Locale? ResolveLocale(IReadOnlyList<Locale> supportedLocales)
    {
        string[] identifiers = new string[supportedLocales.Count];
        for (int index = 0; index < supportedLocales.Count; index++)
        {
            Locale locale = supportedLocales[index];
            using NSDictionary components = NSDictionary.FromObjectsAndKeys(
                [
                    new NSString(locale.LanguageCode),
                    new NSString(locale.CountryCode ?? string.Empty),
                    new NSString(locale.ScriptCode ?? string.Empty),
                ],
                [new NSString(LanguageCodeKey), new NSString(CountryCodeKey), new NSString(ScriptCodeKey)]);
            identifiers[index] = NSLocale.LocaleIdentifierFromComponents(components);
        }

        using NSArray supported = NSArray.FromStrings(identifiers);
        // `+[NSBundle preferredLocalizationsFromArray:]` has no binding.
        IntPtr preferred = SendIntPtr(
            Class.GetHandle("NSBundle"), Selector.GetHandle("preferredLocalizationsFromArray:"), supported.Handle);
        string?[] result = preferred == IntPtr.Zero ? [] : CFArray.StringArrayFromHandle(preferred) ?? [];
        if (result.Length == 0 || result[0] is not { } identifier)
        {
            return null;
        }

        using var resolved = new NSLocale(identifier);
        return string.IsNullOrEmpty(resolved.LanguageCode)
            ? null
            : new Locale(resolved.LanguageCode, NullIfEmpty(resolved.CountryCode), NullIfEmpty(resolved.ScriptCode));
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr argument);

    // Accessibility.framework's C functions behind Swift's AccessibilitySettings (iOS 18).
    [DllImport("/System/Library/Frameworks/Accessibility.framework/Accessibility")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool AXAnimatedImagesEnabled();

    [DllImport("/System/Library/Frameworks/Accessibility.framework/Accessibility")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool AXPrefersNonBlinkingTextInsertionIndicator();
}
