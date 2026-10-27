using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Threading;
using Plumix;

// C#-only infrastructure: the web engine's `flutter/platform` answers and accessibility media queries
// (engine/src/flutter/lib/web_ui/lib/src/engine/platform_dispatcher.dart: `HapticFeedback.vibrate`,
// `SystemSound.play`, `SystemNavigator.pop`, `_updateHighContrast`, `_updateReducedMotion`), with the DOM
// side in wwwroot/plumix-platform.js.
internal static partial class BrowserPlatformPlugin
{
    private const string Module = "plumix-platform";

    // The bits the web engine reports: high contrast, and reduce motion together with disable animations.
    private const int DisableAnimations = 1 << 2;
    private const int ReduceMotion = 1 << 4;
    private const int HighContrast = 1 << 5;
    private const int ReportedFeatures = DisableAnimations | ReduceMotion | HighContrast;

    private static readonly Action<bool, bool> AccessibilityChanged = HandleAccessibilityChanged;

    public static async Task InitializeAsync()
    {
        await JSHost.ImportAsync(Module, "./plumix-platform.js");
        PlumixHost.HapticFeedbackHandler = static type => Vibrate(HapticFeedbackDuration(type));
        // "There are no default system sounds on web."
        PlumixHost.SystemSoundHandler = static _ => { };
        PlumixHost.SystemNavigatorPopHandler = static _ => Exit();
        ObserveAccessibility(AccessibilityChanged);
    }

    // EnginePlatformDispatcher._getHapticFeedbackDuration.
    private static int HapticFeedbackDuration(string? type) => type switch
    {
        "HapticFeedbackType.lightImpact" => 10,
        "HapticFeedbackType.mediumImpact" => 20,
        "HapticFeedbackType.heavyImpact" => 30,
        "HapticFeedbackType.selectionClick" => 10,
        "HapticFeedbackType.successNotification" => 20,
        "HapticFeedbackType.warningNotification" => 20,
        "HapticFeedbackType.errorNotification" => 30,
        _ => 50,
    };

    // "There's no distinction on the web between reduceMotion and disableAnimations."
    private static void HandleAccessibilityChanged(bool highContrast, bool reducedMotion)
    {
        int features = (highContrast ? HighContrast : 0) | (reducedMotion ? DisableAnimations | ReduceMotion : 0);
        Dispatcher.UIThread.Post(() => PlumixHost.UpdatePlatformAccessibilityFeatures(features, ReportedFeatures));
    }

    [JSImport("vibrate", Module)]
    private static partial void Vibrate(int durationMs);

    [JSImport("exit", Module)]
    private static partial void Exit();

    [JSImport("observeAccessibility", Module)]
    private static partial void ObserveAccessibility(
        [JSMarshalAs<JSType.Function<JSType.Boolean, JSType.Boolean>>] Action<bool, bool> callback);
}
