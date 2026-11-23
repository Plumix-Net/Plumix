using Foundation;
using UIKit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.iOS;
using Avalonia.Media;
using Avalonia.Threading;
using System.Collections.Generic;
using Plumix.UI;
using Plumix.Widgets;
using Locale = Plumix.Widgets.Locale;

// Dart parity source (reference): dart_sample/lib/main.dart (platform host bootstrap, adapted)

namespace Plumix.iOS;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
[Register("AppDelegate")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public partial class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    private const string FrameworkRestorationStateKey = "FlutterRestorationStateAppData";
    private readonly IOSRestorationPlugin _restorationPlugin = new(restorationEnabled: true);
    private readonly List<NSObject> _lifecycleObservers;

    public AppDelegate()
    {
        NSNotificationCenter center = NSNotificationCenter.DefaultCenter;
        _lifecycleObservers =
        [
            center.AddObserver(
                UIApplication.DidBecomeActiveNotification,
                _ =>
                {
                    // UIKit has finished decoding saved state before activation. Avalonia owns the
                    // view controller, so this is our equivalent of Flutter's viewWillAppear hook.
                    _restorationPlugin.MarkRestorationComplete();
                    DispatchLifecycleState(AppLifecycleState.Resumed);
                }),
            center.AddObserver(
                UIApplication.WillResignActiveNotification,
                _ => DispatchLifecycleState(AppLifecycleState.Inactive)),
            center.AddObserver(
                UIApplication.DidEnterBackgroundNotification,
                _ => DispatchLifecycleState(AppLifecycleState.Paused)),
            center.AddObserver(
                UIApplication.WillEnterForegroundNotification,
                _ => DispatchLifecycleState(AppLifecycleState.Inactive)),
            center.AddObserver(
                UIApplication.WillTerminateNotification,
                _ => DispatchLifecycleState(AppLifecycleState.Detached)),
            center.AddObserver(
                UIApplication.ContentSizeCategoryChangedNotification,
                _ => ReportTextScaleFactor()),
            center.AddObserver(NSLocale.CurrentLocaleDidChangeNotification, _ => ReportLocales()),
        ];
        ReportTextScaleFactor();
        ReportLocales();
    }

    [Export("application:shouldSaveSecureApplicationState:")]
    public bool ShouldSaveSecureApplicationState(UIApplication application, NSCoder coder) => true;

    [Export("application:shouldRestoreSecureApplicationState:")]
    public bool ShouldRestoreSecureApplicationState(UIApplication application, NSCoder coder) => true;

    [Export("application:willEncodeRestorableStateWithCoder:")]
    public void WillEncodeRestorableState(UIApplication application, NSCoder coder)
    {
        coder.Encode(_restorationPlugin.RestorationData ?? System.Array.Empty<byte>(), FrameworkRestorationStateKey);
    }

    [Export("application:didDecodeRestorableStateWithCoder:")]
    public void DidDecodeRestorableState(UIApplication application, NSCoder coder)
    {
        _restorationPlugin.RestorationData = coder.ContainsKey(FrameworkRestorationStateKey)
            ? coder.DecodeBytes(FrameworkRestorationStateKey)
            : null;
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            // The shared App creates its host in OnFrameworkInitializationCompleted, after this callback.
            .AfterSetup(_ => Dispatcher.UIThread.Post(AttachAccessibilityBridge));
    }

    private static void AttachAccessibilityBridge()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime
            {
                MainView: PlumixHost host,
            })
        {
            IOSAccessibilityBridge.Attach(host);
            IOSPlatformPlugin.Install(host);
        }
    }

    // FlutterViewController's textScaleFactor: the Dynamic Type size's body text point size relative to
    // the default Large (17pt), after Apple's typography guidelines.
    private static void ReportTextScaleFactor()
    {
        const double large = 17;
        double bodySize = UIApplication.SharedApplication.PreferredContentSizeCategory switch
        {
            var c when c == UIContentSizeCategory.ExtraSmall.GetConstant() => 14,
            var c when c == UIContentSizeCategory.Small.GetConstant() => 15,
            var c when c == UIContentSizeCategory.Medium.GetConstant() => 16,
            var c when c == UIContentSizeCategory.ExtraLarge.GetConstant() => 19,
            var c when c == UIContentSizeCategory.ExtraExtraLarge.GetConstant() => 21,
            var c when c == UIContentSizeCategory.ExtraExtraExtraLarge.GetConstant() => 23,
            var c when c == UIContentSizeCategory.AccessibilityMedium.GetConstant() => 28,
            var c when c == UIContentSizeCategory.AccessibilityLarge.GetConstant() => 33,
            var c when c == UIContentSizeCategory.AccessibilityExtraLarge.GetConstant() => 40,
            var c when c == UIContentSizeCategory.AccessibilityExtraExtraLarge.GetConstant() => 47,
            var c when c == UIContentSizeCategory.AccessibilityExtraExtraExtraLarge.GetConstant() => 53,
            _ => large,
        };
        PlatformDispatcher.Instance.UpdateTextScaleFactor(bodySize / large);
    }

    // FlutterEngine's onLocaleUpdated: the user's preferred languages, in order, split into the subtags
    // dart:ui's _updateLocales reads; identifiers without a language code are skipped.
    private static void ReportLocales()
    {
        List<Locale> locales = [];
        foreach (string identifier in NSLocale.PreferredLanguages)
        {
            using var locale = new NSLocale(identifier);
            if (string.IsNullOrEmpty(locale.LanguageCode))
            {
                continue;
            }

            locales.Add(new Locale(locale.LanguageCode, locale.CountryCode, locale.ScriptCode));
        }

        PlumixHost.UpdatePlatformLocales(locales);
    }

    private static void DispatchLifecycleState(AppLifecycleState state)
    {
        WidgetsBinding.Instance.HandleAppLifecycleStateChanged(state);
    }
}
