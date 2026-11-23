using System.Runtime.Versioning;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Views.Accessibility;
using Plumix.Accessibility;
using Plumix.UI;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/android/io/flutter/view/AccessibilityBridge.java (the
// accessibility feature flags: the state and touch-exploration listeners, the transition-animation-scale and
// display-inversion setting observers, setBoldTextFlag and the Android 14 contrast listener). The native
// host's copy of Plumix.Avalonia.Android's reader: it reports to the platform dispatcher itself and turns semantics on
// and off with the accessibility state, as the engine's accessibilityStateChangeListener does.

namespace Plumix.Android;

/// <summary>
/// Reads the platform settings behind dart:ui's accessibility features, reports them to the platform dispatcher
/// whenever one changes, and activates the view's accessibility bridge while an accessibility service runs.
/// </summary>
/// <remarks>Every report reads all settings again, which also covers the bold-text configuration change.</remarks>
internal sealed class AndroidAccessibilityFeatures : IDisposable
{
    // AccessibilityBridge.BOLD_TEXT_WEIGHT_ADJUSTMENT, DISABLED_TRANSITION_ANIMATION_SCALE and
    // DEFAULT_TRANSITION_ANIMATION_SCALE.
    private const int BoldTextWeightAdjustment = 300;
    private const float DisabledTransitionAnimationScale = 0.0f;
    private const float DefaultTransitionAnimationScale = 1.0f;

    // Settings.Secure.ACCESSIBILITY_DISPLAY_INVERSION_ENABLED.
    private const string DisplayInversionEnabled = "accessibility_display_inversion_enabled";

    // The accessibility feature bits the Android engine reports: its `AccessibilityFeature`s not marked
    // "NOT SUPPORTED", and the no-announce bit it always sets.
    private const int ReportedFeatures =
        AccessibilityFeatures.KAccessibleNavigationIndex
        | AccessibilityFeatures.KInvertColorsIndex
        | AccessibilityFeatures.KDisableAnimationsIndex
        | AccessibilityFeatures.KBoldTextIndex
        | AccessibilityFeatures.KHighContrastIndex
        | AccessibilityFeatures.KNoAnnounceIndex;

    private readonly Activity _activity;
    private readonly AccessibilityBridge _bridge;
    private readonly AccessibilityManager? _accessibilityManager;
    private readonly ContentResolver? _contentResolver;
    private readonly StateListener _stateListener;
    private readonly SettingObserver _settingObserver;
    private readonly Java.Lang.Object? _contrastListener;

    public AndroidAccessibilityFeatures(Activity activity, AccessibilityBridge bridge)
    {
        _activity = activity;
        _bridge = bridge;
        _accessibilityManager = activity.GetSystemService(Context.AccessibilityService) as AccessibilityManager;
        _contentResolver = activity.ContentResolver;

        _stateListener = new StateListener(this);
        _accessibilityManager?.AddAccessibilityStateChangeListener(_stateListener);
        _accessibilityManager?.AddTouchExplorationStateChangeListener(_stateListener);

        _settingObserver = new SettingObserver(this, new Handler(Looper.MainLooper!));
        if (_contentResolver is not null)
        {
            if (Settings.Global.GetUriFor(Settings.Global.TransitionAnimationScale) is { } animationScale)
            {
                _contentResolver.RegisterContentObserver(animationScale, false, _settingObserver);
            }

            if (Settings.Secure.GetUriFor(DisplayInversionEnabled) is { } displayInversion)
            {
                _contentResolver.RegisterContentObserver(displayInversion, false, _settingObserver);
            }
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(34))
        {
            _contrastListener = RegisterContrastListener();
        }

        Report();
    }

    /// <summary>Reads every setting, reports the features and follows the accessibility state.</summary>
    public void Report()
    {
        bool accessibilityEnabled = _accessibilityManager?.IsEnabled == true;
        int features = AccessibilityFeatures.KNoAnnounceIndex;
        features |= accessibilityEnabled ? AccessibilityFeatures.KAccessibleNavigationIndex : 0;
        features |= ReadSecureInt(DisplayInversionEnabled) == 1 ? AccessibilityFeatures.KInvertColorsIndex : 0;
        features |= ReadTransitionAnimationScale() == DisabledTransitionAnimationScale
            ? AccessibilityFeatures.KDisableAnimationsIndex
            : 0;
        features |= OperatingSystem.IsAndroidVersionAtLeast(31) && IsBoldText(_activity.Resources?.Configuration)
            ? AccessibilityFeatures.KBoldTextIndex
            : 0;
        features |= OperatingSystem.IsAndroidVersionAtLeast(34) && IsHighContrast()
            ? AccessibilityFeatures.KHighContrastIndex
            : 0;
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        dispatcher.UpdateAccessibilityFeatures(
            (dispatcher.ReportedAccessibilityFeatures & ~ReportedFeatures) | features);

        // The engine's accessibilityStateChangeListener: semantics follow the accessibility state.
        if (accessibilityEnabled)
        {
            _bridge.Activate();
        }
        else
        {
            _bridge.Deactivate();
        }
    }

    public void Dispose()
    {
        _accessibilityManager?.RemoveAccessibilityStateChangeListener(_stateListener);
        _accessibilityManager?.RemoveTouchExplorationStateChangeListener(_stateListener);
        _contentResolver?.UnregisterContentObserver(_settingObserver);
        if (OperatingSystem.IsAndroidVersionAtLeast(34))
        {
            UnregisterContrastListener();
        }

        _stateListener.Dispose();
        _settingObserver.Dispose();
    }

    private int ReadSecureInt(string name)
    {
        try
        {
            return Settings.Secure.GetInt(_contentResolver, name);
        }
        catch (Settings.SettingNotFoundException)
        {
            return 0;
        }
    }

    private float ReadTransitionAnimationScale() =>
        Settings.Global.GetFloat(
            _contentResolver,
            Settings.Global.TransitionAnimationScale,
            DefaultTransitionAnimationScale);

    // AccessibilityBridge.Api31Impl.isBoldText.
    [SupportedOSPlatform("android31.0")]
    private static bool IsBoldText(Configuration? configuration) =>
        configuration is not null
        && configuration.FontWeightAdjustment != Configuration.FontWeightAdjustmentUndefined
        && configuration.FontWeightAdjustment >= BoldTextWeightAdjustment;

    // AccessibilityBridge.setHighContrastFlag: any contrast above standard.
    [SupportedOSPlatform("android34.0")]
    private bool IsHighContrast() =>
        _activity.GetSystemService(Context.UiModeService) is UiModeManager uiModeManager
        && uiModeManager.Contrast > 0.0f;

    [SupportedOSPlatform("android34.0")]
    private Java.Lang.Object? RegisterContrastListener()
    {
        if (_activity.GetSystemService(Context.UiModeService) is not UiModeManager uiModeManager
            || _activity.MainExecutor is not { } executor)
        {
            return null;
        }

        var listener = new ContrastListener(this);
        uiModeManager.AddContrastChangeListener(executor, listener);
        return listener;
    }

    [SupportedOSPlatform("android34.0")]
    private void UnregisterContrastListener()
    {
        if (_contrastListener is ContrastListener listener
            && _activity.GetSystemService(Context.UiModeService) is UiModeManager uiModeManager)
        {
            uiModeManager.RemoveContrastChangeListener(listener);
            listener.Dispose();
        }
    }

    private sealed class StateListener(AndroidAccessibilityFeatures owner)
        : Java.Lang.Object,
            AccessibilityManager.IAccessibilityStateChangeListener,
            AccessibilityManager.ITouchExplorationStateChangeListener
    {
        public void OnAccessibilityStateChanged(bool enabled) => owner.Report();

        public void OnTouchExplorationStateChanged(bool enabled) => owner.Report();
    }

    private sealed class SettingObserver(AndroidAccessibilityFeatures owner, Handler handler)
        : ContentObserver(handler)
    {
        public override void OnChange(bool selfChange) => owner.Report();
    }

    [SupportedOSPlatform("android34.0")]
    private sealed class ContrastListener(AndroidAccessibilityFeatures owner)
        : Java.Lang.Object, UiModeManager.IContrastChangeListener
    {
        public void OnContrastChanged(float contrast) => owner.Report();
    }
}
