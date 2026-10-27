using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using CoreFoundation;
using Foundation;
using ObjCRuntime;
using Plumix.UI;
using UIKit;
using Locale = Plumix.Widgets.Locale;

// C#-only host infrastructure parity sources:
// flutter/engine/src/flutter/shell/platform/darwin/ios/framework/Source/FlutterPlatformPlugin.mm
// (playSystemSound:, vibrateHapticFeedback:, popSystemNavigator:) and
// flutter/engine/src/flutter/shell/platform/darwin/ios/platform_view_ios.mm
// (ComputePlatformResolvedLocales).

namespace Plumix.iOS;

/// <summary>The iOS engine's platform plugin answers on <c>flutter/platform</c> and its locale resolution.</summary>
internal static class IOSPlatformPlugin
{
    // FlutterPlatformPlugin.mm's kKeyPressClickSoundId and kWheelsOfTimeSoundId; AudioToolbox's
    // kSystemSoundID_Vibrate.
    private const uint KeyPressClickSoundId = 1306;
    private const uint WheelsOfTimeSoundId = 1157;
    private const uint SystemSoundIdVibrate = 4095;

    // The values of NSLocaleLanguageCode, NSLocaleCountryCode and NSLocaleScriptCode.
    private const string LanguageCodeKey = "kCFLocaleLanguageCodeKey";
    private const string CountryCodeKey = "kCFLocaleCountryCodeKey";
    private const string ScriptCodeKey = "kCFLocaleScriptCodeKey";

    /// <summary>
    /// Installs the answers for <paramref name="host"/>'s process and the locale resolver.
    /// </summary>
    public static void Install(PlumixHost host)
    {
        PlumixHost.SystemSoundHandler = PlaySystemSound;
        PlumixHost.HapticFeedbackHandler = VibrateHapticFeedback;
        PlumixHost.SystemNavigatorPopHandler = animated => PopSystemNavigator(host, animated);
        PlatformDispatcher.Instance.PlatformLocaleResolver = ResolveLocale;
    }

    // playSystemSound: "All feedback types are specific to Android and are treated as equal on iOS"; the
    // alert has no sound.
    private static void PlaySystemSound(SystemSoundType type)
    {
        if (type == SystemSoundType.Click)
        {
            AudioToolbox.AudioServicesPlaySystemSound(KeyPressClickSoundId);
        }
        else if (type == SystemSoundType.Tick)
        {
            AudioToolbox.AudioServicesPlaySystemSound(WheelsOfTimeSoundId);
        }
    }

    // vibrateHapticFeedback: the plain vibration for no type, a feedback generator for each named one.
    private static void VibrateHapticFeedback(string? type)
    {
        switch (type)
        {
            case null:
                AudioToolbox.AudioServicesPlaySystemSound(SystemSoundIdVibrate);
                break;
            case "HapticFeedbackType.lightImpact":
                Impact(UIImpactFeedbackStyle.Light);
                break;
            case "HapticFeedbackType.mediumImpact":
                Impact(UIImpactFeedbackStyle.Medium);
                break;
            case "HapticFeedbackType.heavyImpact":
                Impact(UIImpactFeedbackStyle.Heavy);
                break;
            case "HapticFeedbackType.selectionClick":
            {
                using var generator = new UISelectionFeedbackGenerator();
                generator.SelectionChanged();
                break;
            }
            case "HapticFeedbackType.successNotification":
                Notify(UINotificationFeedbackType.Success);
                break;
            case "HapticFeedbackType.warningNotification":
                Notify(UINotificationFeedbackType.Warning);
                break;
            case "HapticFeedbackType.errorNotification":
                Notify(UINotificationFeedbackType.Error);
                break;
        }
    }

    private static void Impact(UIImpactFeedbackStyle style)
    {
        // The engine still creates the generator with `initWithStyle:`, which iOS 17.5 deprecates.
#pragma warning disable CA1422
        using var generator = new UIImpactFeedbackGenerator(style);
#pragma warning restore CA1422
        generator.ImpactOccurred();
    }

    private static void Notify(UINotificationFeedbackType type)
    {
        using var generator = new UINotificationFeedbackGenerator();
        generator.NotificationOccurred(type);
    }

    // popSystemNavigator: Apple's guidelines say not to terminate iOS applications. A view controller inside
    // a navigation controller is popped from it; one that is not the key window's root is dismissed; the
    // root itself (Avalonia's own view controller, normally) stays.
    private static void PopSystemNavigator(PlumixHost host, bool animated)
    {
        if (TopLevel.GetTopLevel(host)?.TryGetPlatformHandle() is not { } handle
            || Runtime.GetNSObject<UIView>(handle.Handle) is not { } view
            || ViewControllerOf(view) is not { } viewController)
        {
            return;
        }

        if (viewController.NavigationController is { } navigationController)
        {
            navigationController.PopViewController(animated);
        }
        else if (view.Window?.RootViewController != viewController)
        {
            viewController.DismissViewController(animated, null);
        }
    }

    private static UIViewController? ViewControllerOf(UIView view)
    {
        for (UIResponder? responder = view; responder is not null; responder = responder.NextResponder)
        {
            if (responder is UIViewController viewController)
            {
                return viewController;
            }
        }

        return null;
    }

    // PlatformViewIOS::ComputePlatformResolvedLocales: NSBundle's preferred localization among the supported
    // locale identifiers, split again into NSLocale's components.
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
        IntPtr preferred = ObjC.SendIntPtr(
            Class.GetHandle("NSBundle"),
            Selector.GetHandle("preferredLocalizationsFromArray:"),
            supported.Handle);
        string?[] result = preferred == IntPtr.Zero ? [] : CFArray.StringArrayFromHandle(preferred) ?? [];
        if (result.Length == 0 || result[0] is not { } identifier)
        {
            return null;
        }

        using var resolved = new NSLocale(identifier);
        return string.IsNullOrEmpty(resolved.LanguageCode)
            ? null
            : new Locale(resolved.LanguageCode, resolved.CountryCode, resolved.ScriptCode);
    }

    private static class ObjC
    {
        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static extern IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr argument);
    }

    private static class AudioToolbox
    {
        [DllImport("/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox")]
        internal static extern void AudioServicesPlaySystemSound(uint soundId);
    }
}
