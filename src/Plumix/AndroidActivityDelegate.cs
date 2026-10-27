using Plumix.UI;
using Plumix.Widgets;

// C#-only host infrastructure parity source: the lifecycle, restoration and back-callback forwarding of
// flutter/engine/src/flutter/shell/platform/android/io/flutter/embedding/android/
// FlutterActivityAndFragmentDelegate.java.

namespace Plumix;

/// <summary>
/// The framework side of an Android Activity: forwards its lifecycle, saved state and back callbacks to
/// the binding, as Flutter's <c>FlutterActivityAndFragmentDelegate</c> does for a <c>FlutterActivity</c>.
/// </summary>
/// <remarks>
/// <c>PlumixMainActivity</c> (the <c>Plumix.Android</c> package) owns one and calls it from every
/// Activity callback. An Activity that cannot derive from it creates one in its constructor and makes
/// the same calls. The class has no Android dependency, so the core package stays platform neutral.
/// <para>
/// Restoration data belongs to the process (Flutter's engine), not to an Activity: every delegate
/// shares one <c>flutter/restoration</c> handler, so a recreated Activity hands the framework the
/// bundle its predecessor saved without the framework's tree being rebuilt.
/// </para>
/// </remarks>
public sealed class AndroidActivityDelegate
{
    private static AndroidRestorationChannel? s_restorationChannel;

    private readonly AndroidRestorationChannel _restorationChannel;
    private readonly AndroidLifecycleChannel _lifecycleChannel;
    private bool _frameworkHandlesBack;
    private bool _imeVisible;
    private bool _backCallbackNeeded;

    /// <summary>Creates a delegate over the process-wide binding and restoration channel.</summary>
    public AndroidActivityDelegate()
        : this(
            s_restorationChannel ??= new AndroidRestorationChannel(waitForRestorationData: true),
            WidgetsBinding.Instance.HandleAppLifecycleStateChanged)
    {
    }

    internal AndroidActivityDelegate(
        AndroidRestorationChannel restorationChannel,
        Action<AppLifecycleState> sendLifecycleState)
    {
        _restorationChannel = restorationChannel;
        _lifecycleChannel = new AndroidLifecycleChannel(sendLifecycleState);
    }

    /// <summary>
    /// Called with whether the framework handles back (<c>SystemNavigator.setFrameworkHandlesBack</c>):
    /// <c>FlutterActivity.setFrameworkHandlesBack</c>, which registers the Activity's back callback only
    /// while the framework has something to pop, so that the system handles back at the root itself.
    /// </summary>
    /// <remarks>
    /// <see cref="OnCreate"/> replays the last reported value to a recreated Activity.
    /// </remarks>
    public Action<bool>? FrameworkHandlesBackChanged { get; set; }

    /// <summary>
    /// Called with whether the Activity's back callback must be registered: while the framework handles
    /// back (<see cref="FrameworkHandlesBackChanged"/>) and the soft keyboard is hidden.
    /// </summary>
    /// <remarks>
    /// C#-only: <c>PlumixMainActivity</c> registers its callback above the default priority, so that it
    /// wins over Avalonia's own <c>OnBackPressedDispatcher</c> callback, and an overlay-priority callback
    /// also wins over the keyboard's default-priority one. Stepping aside while the keyboard is shown
    /// lets back hide the keyboard first, as it does over <c>FlutterActivity</c>'s default-priority
    /// callback. See <see cref="OnImeVisibilityChanged"/>.
    /// </remarks>
    public Action<bool>? BackCallbackNeededChanged { get; set; }

    /// <summary>
    /// The Activity's answer to <c>SystemNavigator.pop</c>, which the framework sends when a back press
    /// popped no route; <c>FlutterActivity</c> finishes itself (<c>PlatformPlugin.popSystemNavigator</c>).
    /// </summary>
    public Action? PopSystemNavigator { get; set; }

    /// <summary>
    /// The Activity's answer to <c>SystemSound.play</c>: <c>PlatformPlugin.playSystemSound</c>, which plays
    /// the decor view's <c>SoundEffectConstants.CLICK</c> for <see cref="SystemSoundType.Click"/> only.
    /// </summary>
    public Action<SystemSoundType>? PlaySystemSound { get; set; }

    /// <summary>
    /// The Activity's answer to <c>HapticFeedback.vibrate</c>, called with Dart's <c>HapticFeedbackType</c>
    /// string or <see langword="null"/> for the plain vibration: <c>PlatformPlugin.vibrateHapticFeedback</c>,
    /// which performs the decor view's <c>HapticFeedbackConstants</c> for each type.
    /// </summary>
    public Action<string?>? VibrateHapticFeedback { get; set; }

    /// <summary>
    /// The Activity's locale resolution, which <see cref="PlatformDispatcher.ComputePlatformResolvedLocale"/>
    /// asks: <c>LocalizationPlugin.resolveNativeLocale</c> over the configuration's locale list.
    /// </summary>
    /// <remarks>
    /// <see cref="OnCreate"/> installs it as <see cref="PlatformDispatcher.PlatformLocaleResolver"/>.
    /// </remarks>
    public Func<IReadOnlyList<Locale>, Locale?>? ResolveLocale { get; set; }

    /// <summary>
    /// <c>Activity.onCreate</c>: hands the framework the restoration data saved by
    /// <see cref="OnSaveInstanceState"/>, or null when the Activity starts fresh, and makes this Activity
    /// the one that answers the framework's back requests.
    /// </summary>
    /// <remarks>
    /// An absent bundle is also a response: it releases the framework's initial restoration request,
    /// which <c>WidgetsApp</c> waits for before its first restorable frame.
    /// </remarks>
    public void OnCreate(byte[]? savedRestorationData)
    {
        PlumixHost.FrameworkHandlesBackChanged -= HandleFrameworkHandlesBackChanged;
        PlumixHost.FrameworkHandlesBackChanged += HandleFrameworkHandlesBackChanged;
        PlumixHost.SystemNavigatorPopHandler = HandleSystemNavigatorPop;
        PlumixHost.SystemSoundHandler = HandleSystemSound;
        PlumixHost.HapticFeedbackHandler = HandleHapticFeedback;
        PlatformDispatcher.Instance.PlatformLocaleResolver = HandleResolveLocale;
        if (PlumixHost.ReportedFrameworkHandlesBack is { } frameworkHandlesBack)
        {
            HandleFrameworkHandlesBackChanged(frameworkHandlesBack);
        }

        _restorationChannel.SetRestorationData(savedRestorationData);
    }

    /// <summary><c>Activity.onSaveInstanceState</c>: the restoration data to put in the saved bundle.</summary>
    public byte[]? OnSaveInstanceState() => _restorationChannel.RestorationData;

    /// <summary><c>Activity.onResume</c>.</summary>
    public void OnResume() => _lifecycleChannel.AppIsResumed();

    /// <summary><c>Activity.onPause</c>.</summary>
    public void OnPause() => _lifecycleChannel.AppIsInactive();

    /// <summary><c>Activity.onStop</c>.</summary>
    public void OnStop() => _lifecycleChannel.AppIsPaused();

    /// <summary><c>Activity.onDestroy</c>.</summary>
    public void OnDestroy()
    {
        PlumixHost.FrameworkHandlesBackChanged -= HandleFrameworkHandlesBackChanged;
        if (ReferenceEquals(PlumixHost.SystemNavigatorPopHandler?.Target, this))
        {
            PlumixHost.SystemNavigatorPopHandler = null;
        }

        if (ReferenceEquals(PlumixHost.SystemSoundHandler?.Target, this))
        {
            PlumixHost.SystemSoundHandler = null;
        }

        if (ReferenceEquals(PlumixHost.HapticFeedbackHandler?.Target, this))
        {
            PlumixHost.HapticFeedbackHandler = null;
        }

        if (ReferenceEquals(PlatformDispatcher.Instance.PlatformLocaleResolver?.Target, this))
        {
            PlatformDispatcher.Instance.PlatformLocaleResolver = null;
        }

        _lifecycleChannel.AppIsDetached();
    }

    /// <summary><c>Activity.onWindowFocusChanged</c>.</summary>
    public void OnWindowFocusChanged(bool hasFocus)
    {
        if (hasFocus)
        {
            _lifecycleChannel.AWindowIsFocused();
        }
        else
        {
            _lifecycleChannel.NoWindowsAreFocused();
        }
    }

    /// <summary>
    /// <c>Activity.onCreate</c> and <c>Activity.onConfigurationChanged</c>: reports the user's font size
    /// (<c>Configuration.fontScale</c>) as the platform text scale factor, as
    /// <c>FlutterView.sendUserSettingsToFlutter</c> does.
    /// </summary>
    /// <remarks>
    /// An Activity that does not list <c>fontScale</c> in its <c>configChanges</c> is recreated when the
    /// setting changes, and its <c>onCreate</c> reports the new value.
    /// </remarks>
    public void OnConfigurationChanged(float fontScale) => OnConfigurationChanged(fontScale, null);

    /// <summary>
    /// <c>Activity.onCreate</c> and <c>Activity.onConfigurationChanged</c>: reports the user's font size
    /// (<c>Configuration.fontScale</c>) together with the configuration's font scaling function, as
    /// <c>FlutterView.sendUserSettingsToFlutter</c> does on Android 14+.
    /// </summary>
    /// <param name="fontScale">The configuration's <c>fontScale</c>.</param>
    /// <param name="scaledFontSize">
    /// The engine's <c>FlutterJNI.getScaledFontSize</c> for this configuration: an sp font size converted by
    /// <c>TypedValue.applyDimension(COMPLEX_UNIT_SP)</c> over a copy of its <c>DisplayMetrics</c>, divided by
    /// the density. Android 14+ scales fonts nonlinearly; <see langword="null"/> keeps linear scaling.
    /// </param>
    /// <remarks>
    /// <c>SystemTextScaler</c> asks it through <see cref="PlatformDispatcher.ScaleFontSize"/>.
    /// </remarks>
    public void OnConfigurationChanged(float fontScale, Func<double, double>? scaledFontSize) =>
        PlatformDispatcher.Instance.UpdateTextScaleFactor(fontScale, scaledFontSize);

    /// <summary>
    /// <c>Activity.onCreate</c> and <c>Activity.onConfigurationChanged</c>: reports the configuration's
    /// preferred locales (<c>Configuration.getLocales</c>, in order), as the engine's
    /// <c>LocalizationPlugin.sendLocalesToFlutter</c> does.
    /// </summary>
    /// <remarks>
    /// An Activity that does not list <c>locale</c> in its <c>configChanges</c> is recreated when the
    /// user changes the system languages, and its <c>onCreate</c> reports the new list.
    /// </remarks>
    public void OnLocalesChanged(IReadOnlyList<Locale> locales) => PlumixHost.UpdatePlatformLocales(locales);

    /// <summary>
    /// <c>Activity.onCreate</c> and <c>Activity.onConfigurationChanged</c>: reports the display's touch slop
    /// (<c>ViewConfiguration.getScaledTouchSlop</c>, in physical pixels), which the engine's <c>FlutterView</c>
    /// sends with its viewport metrics as <c>physicalTouchSlop</c>.
    /// </summary>
    /// <remarks>
    /// Every host view republishes its metrics, so <c>MediaQueryData.GestureSettings</c> carries the slop in
    /// logical pixels and the gesture recognizers use it instead of <c>kTouchSlop</c>.
    /// </remarks>
    public void OnScaledTouchSlopChanged(int scaledTouchSlop)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(scaledTouchSlop);
        PlumixHost.UpdatePhysicalTouchSlop(scaledTouchSlop);
    }

    /// <summary>
    /// Reports the accessibility features the Activity reads from the platform, as the engine's
    /// <c>AccessibilityBridge</c> does, together with the no-announce bit, which the Android engine always
    /// sets. The features Android does not support (reduce motion, on/off switch labels, the auto-play and
    /// cursor bits) stay clear.
    /// </summary>
    /// <param name="accessibleNavigation">An assistive technology inspects the window.</param>
    /// <param name="invertColors">The secure <c>accessibility_display_inversion_enabled</c> setting.</param>
    /// <param name="disableAnimations">The global <c>transition_animation_scale</c> is 0.</param>
    /// <param name="boldText">The configuration's <c>fontWeightAdjustment</c> is 300 or more (Android 12+).</param>
    /// <param name="highContrast"><c>UiModeManager.getContrast</c> is above 0 (Android 14+).</param>
    public void OnAccessibilityFeaturesChanged(
        bool accessibleNavigation,
        bool invertColors,
        bool disableAnimations,
        bool boldText,
        bool highContrast)
    {
        int features = AccessibilityFeatures.KNoAnnounceIndex;
        features |= accessibleNavigation ? AccessibilityFeatures.KAccessibleNavigationIndex : 0;
        features |= invertColors ? AccessibilityFeatures.KInvertColorsIndex : 0;
        features |= disableAnimations ? AccessibilityFeatures.KDisableAnimationsIndex : 0;
        features |= boldText ? AccessibilityFeatures.KBoldTextIndex : 0;
        features |= highContrast ? AccessibilityFeatures.KHighContrastIndex : 0;
        PlumixHost.UpdatePlatformAccessibilityFeatures(features, AndroidAccessibilityFeatures);
    }

    // The accessibility feature bits the Android engine reports: its `AccessibilityFeature`s not marked
    // "NOT SUPPORTED".
    internal const int AndroidAccessibilityFeatures =
        AccessibilityFeatures.KAccessibleNavigationIndex
        | AccessibilityFeatures.KInvertColorsIndex
        | AccessibilityFeatures.KDisableAnimationsIndex
        | AccessibilityFeatures.KBoldTextIndex
        | AccessibilityFeatures.KHighContrastIndex
        | AccessibilityFeatures.KNoAnnounceIndex;

    /// <summary><c>OnBackAnimationCallback.onBackStarted</c> (Android 14+ predictive back).</summary>
    public void OnBackStarted(PredictiveBackEvent backEvent) =>
        _ = WidgetsBinding.Instance.HandleStartBackGesture(backEvent);

    /// <summary><c>OnBackAnimationCallback.onBackProgressed</c>.</summary>
    public void OnBackProgressed(PredictiveBackEvent backEvent) =>
        WidgetsBinding.Instance.HandleUpdateBackGestureProgress(backEvent);

    /// <summary><c>OnBackAnimationCallback.onBackCancelled</c>.</summary>
    public void OnBackCancelled() => WidgetsBinding.Instance.HandleCancelBackGesture();

    /// <summary>
    /// <c>OnBackInvokedCallback.onBackInvoked</c>: commits the back gesture in the framework. A back press
    /// that pops no route ends in <c>SystemNavigator.pop</c>, which <see cref="PopSystemNavigator"/>
    /// answers.
    /// </summary>
    public void OnBackInvoked() => EventLoop.RunAsync(() => WidgetsBinding.Instance.HandleCommitBackGesture());

    /// <summary>
    /// The soft keyboard was shown or hidden (the window's IME insets). Updates
    /// <see cref="BackCallbackNeededChanged"/>; repeated calls with the same value change nothing.
    /// </summary>
    public void OnImeVisibilityChanged(bool imeVisible)
    {
        _imeVisible = imeVisible;
        UpdateBackCallbackNeeded();
    }

    private void HandleFrameworkHandlesBackChanged(bool frameworkHandlesBack)
    {
        _frameworkHandlesBack = frameworkHandlesBack;
        FrameworkHandlesBackChanged?.Invoke(frameworkHandlesBack);
        UpdateBackCallbackNeeded();
    }

    private void UpdateBackCallbackNeeded()
    {
        bool needed = _frameworkHandlesBack && !_imeVisible;
        if (needed != _backCallbackNeeded)
        {
            _backCallbackNeeded = needed;
            BackCallbackNeededChanged?.Invoke(needed);
        }
    }

    private void HandleSystemNavigatorPop(bool animated) => PopSystemNavigator?.Invoke();

    private void HandleSystemSound(SystemSoundType type) => PlaySystemSound?.Invoke(type);

    private void HandleHapticFeedback(string? type) => VibrateHapticFeedback?.Invoke(type);

    private Locale? HandleResolveLocale(IReadOnlyList<Locale> supportedLocales) =>
        ResolveLocale?.Invoke(supportedLocales);

    internal static void ResetForTests() => s_restorationChannel = null;
}
