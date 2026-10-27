using Plumix.UI;

// C#-only host infrastructure: the platform-dependent answers of the engine's platform plugins on
// `flutter/platform` (`SystemNavigator.pop`, `SystemSound.play`, `HapticFeedback.vibrate`) and the
// accessibility feature bits an embedder reads itself.
// Android: flutter/engine/src/flutter/shell/platform/android/io/flutter/plugin/platform/PlatformPlugin.java,
// .../io/flutter/view/AccessibilityBridge.java.
// iOS: flutter/engine/src/flutter/shell/platform/darwin/ios/framework/Source/FlutterPlatformPlugin.mm.
// Web: flutter/engine/src/flutter/lib/web_ui/lib/src/engine/platform_dispatcher.dart.
// Desktop: see DesktopPlatformPlugin.cs.

namespace Plumix;

public partial class PlumixHost
{
    // `popRoute`s a back key started (Escape, BrowserBack) that have not completed yet; see
    // HandleBackButton. Their fall-through `SystemNavigator.pop` is dropped.
    private static int s_pendingKeyPopRoutes;

    // The accessibility feature bits an embedder reported through UpdatePlatformAccessibilityFeatures,
    // and which bits it owns.
    private static int s_platformAccessibilityFeatures;
    private static int s_platformAccessibilityFeaturesMask;

    /// <summary>
    /// The platform's answer to <c>SystemNavigator.pop</c>, called with its <c>animated</c> argument; null
    /// where the platform host installs none.
    /// </summary>
    /// <remarks>
    /// The engine's <c>PlatformPlugin.popSystemNavigator</c>: the Android embedder finishes the Activity
    /// (<see cref="AndroidActivityDelegate.PopSystemNavigator"/> installs it), the iOS embedder pops its
    /// navigation controller or dismisses its view controller, the web engine leaves the page through
    /// the browser history. Without a handler, the macOS and Linux desktop embedders' answer applies
    /// (the application quits; macOS asks the framework first through <c>System.requestAppExit</c>) and
    /// elsewhere, as on Windows, the method is not implemented.
    /// <para>
    /// A <c>popRoute</c> started by a back key (C#-only, see <c>docs/ai/DIVERGENCES.md</c>) that no route
    /// took does not reach the handler, so Escape at the root route never quits the application.
    /// </para>
    /// </remarks>
    public static Action<bool>? SystemNavigatorPopHandler { get; set; }

    /// <summary>The platform's answer to <c>SystemSound.play</c>; null where the platform host installs none.</summary>
    /// <remarks>
    /// The engine's <c>playSystemSound</c>: Android plays the click sound effect for
    /// <see cref="SystemSoundType.Click"/> only, iOS plays its key-press click and tick sounds, the web
    /// engine plays none. Without a handler, the desktop embedders' answer applies: an alert beep for
    /// <see cref="SystemSoundType.Alert"/> on macOS, Windows and Linux, nothing for the other types.
    /// </remarks>
    public static Action<SystemSoundType>? SystemSoundHandler { get; set; }

    /// <summary>
    /// The platform's answer to <c>HapticFeedback.vibrate</c>, called with Dart's <c>HapticFeedbackType</c>
    /// string (for example <c>HapticFeedbackType.mediumImpact</c>), or <see langword="null"/> for the plain
    /// vibration; null where the platform host installs none, as on the desktop embedders, which have no
    /// haptics.
    /// </summary>
    public static Action<string?>? HapticFeedbackHandler { get; set; }

    /// <summary>
    /// Reports the accessibility features an embedder reads from its platform, and sends the combined
    /// bit set through <see cref="PlatformDispatcher.UpdateAccessibilityFeatures"/>.
    /// </summary>
    /// <param name="features">The feature bits, in <see cref="PlatformDispatcher.UpdateAccessibilityFeatures"/>'s
    /// layout.</param>
    /// <param name="reportedFeatures">
    /// The bits the embedder owns. They replace the host's own reading (the Avalonia theme's high-contrast
    /// bit); every other bit keeps the value last reported.
    /// </param>
    /// <remarks>
    /// The engine's <c>AccessibilityBridge.sendLatestAccessibilityFlagsToFlutter</c> (Android) and the web
    /// engine's media-query listeners: each embedder reads its own set of settings and sends the whole
    /// bit field on every change.
    /// </remarks>
    public static void UpdatePlatformAccessibilityFeatures(int features, int reportedFeatures)
    {
        s_platformAccessibilityFeaturesMask = reportedFeatures;
        s_platformAccessibilityFeatures = features & reportedFeatures;
        PublishAccessibilityFeatures(hostHighContrast: null);
    }

    /// <summary>Forgets the platform plugin handlers and accessibility bits a host installed. Test-only.</summary>
    internal static void ResetPlatformPluginForTests()
    {
        SystemNavigatorPopHandler = null;
        SystemSoundHandler = null;
        HapticFeedbackHandler = null;
        s_pendingKeyPopRoutes = 0;
        s_platformAccessibilityFeatures = 0;
        s_platformAccessibilityFeaturesMask = 0;
        PlatformDispatcher.Instance.PlatformLocaleResolver = null;
    }

    /// <summary>
    /// Sends the accessibility features: the bits last reported, the host's high-contrast reading when it
    /// has one, and the embedder's bits over both.
    /// </summary>
    private static void PublishAccessibilityFeatures(bool? hostHighContrast)
    {
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        int values = dispatcher.ReportedAccessibilityFeatures;
        if (hostHighContrast is { } highContrast)
        {
            values = highContrast
                ? values | AccessibilityFeatures.KHighContrastIndex
                : values & ~AccessibilityFeatures.KHighContrastIndex;
        }

        values = (values & ~s_platformAccessibilityFeaturesMask) | s_platformAccessibilityFeatures;
        dispatcher.UpdateAccessibilityFeatures(values);
    }

    /// <summary>Answers <c>SystemNavigator.pop</c> (see <see cref="SystemNavigatorPopHandler"/>).</summary>
    private static Task<object?> PopSystemNavigator(object? arguments)
    {
        if (Volatile.Read(ref s_pendingKeyPopRoutes) > 0)
        {
            return Task.FromResult<object?>(null);
        }

        bool animated = arguments is true;
        if (SystemNavigatorPopHandler is { } popSystemNavigator)
        {
            popSystemNavigator(animated);
        }
        else if (!DesktopPlatformPlugin.PopSystemNavigator())
        {
            throw new MissingPluginException("SystemNavigator.pop");
        }

        return Task.FromResult<object?>(null);
    }

    /// <summary>
    /// Runs <paramref name="popRoute"/> for a back key, marking it as key-initiated until it completes.
    /// </summary>
    /// <remarks>
    /// The binding sends the fall-through <c>SystemNavigator.pop</c> synchronously inside the
    /// <c>popRoute</c> that no observer took, before its task completes, so the pop is recognized as this
    /// key's.
    /// </remarks>
    private static Task<bool> RunKeyPopRoute(Func<Task<bool>> popRoute)
    {
        Interlocked.Increment(ref s_pendingKeyPopRoutes);
        Task<bool> handled;
        try
        {
            handled = popRoute();
        }
        catch
        {
            Interlocked.Decrement(ref s_pendingKeyPopRoutes);
            throw;
        }

        if (handled.IsCompleted)
        {
            Interlocked.Decrement(ref s_pendingKeyPopRoutes);
        }
        else
        {
            _ = handled.ContinueWith(
                static _ => Interlocked.Decrement(ref s_pendingKeyPopRoutes),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        return handled;
    }
}
