using Android.App;
using Android.Content;
using Android.Views;
using Plumix.UI;
using Color = Plumix.UI.Color;

namespace Plumix.Android;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/android/io/flutter/plugin/platform/PlatformPlugin.java (the
// `flutter/platform` methods: clipboard, playSystemSound, vibrateHapticFeedback, setSystemChromeSystemUIOverlayStyle,
// setSystemChromeApplicationSwitcherDescription, popSystemNavigator, setFrameworkHandlesBack). The sound, haptic
// and system-bar parts follow Plumix.Avalonia.Android's AndroidPlatformPlugin, without AndroidX: the window's own
// WindowInsetsController (Android 11+) or the decor view's light-bar flags (before Android 11).

/// <summary>Answers the framework's <c>flutter/platform</c> calls for the Activity.</summary>
internal sealed class AndroidPlatformChannel : IDisposable
{
    // PlatformPlugin's clip label for Clipboard.setData.
    private const string ClipLabel = "text label?";

    private readonly Activity _activity;
    private readonly Action<bool> _frameworkHandlesBackChanged;

    public AndroidPlatformChannel(Activity activity, Action<bool> frameworkHandlesBackChanged)
    {
        _activity = activity;
        _frameworkHandlesBackChanged = frameworkHandlesBackChanged;
        SystemChannels.Platform.SetPlatformMethodCallHandler(Handle);
    }

    public void Dispose() => SystemChannels.Platform.SetPlatformMethodCallHandler(null);

    private Task<object?> Handle(MethodCall call)
    {
        switch (call.Method)
        {
            case "Clipboard.setData":
                string? text = call.Arguments is System.Collections.IDictionary data ? data["text"] as string : null;
                Clipboard?.PrimaryClip = ClipData.NewPlainText(ClipLabel, text);
                return Done;
            case "Clipboard.getData":
                return Task.FromResult<object?>(GetClipboardText(call.Arguments as string));
            case "Clipboard.hasStrings":
                // From the clip's description alone: reading the clip shows Android 12+'s paste notice.
                bool hasStrings = Clipboard is { HasPrimaryClip: true } clipboard
                                  && clipboard.PrimaryClipDescription?.HasMimeType("text/*") == true;
                return Task.FromResult<object?>(new Dictionary<string, object?> { ["value"] = hasStrings });
            case "SystemSound.play":
                if (call.Arguments is "SystemSoundType.click")
                {
                    DecorView?.PlaySoundEffect(SoundEffects.Click);
                }

                return Done;
            case "HapticFeedback.vibrate":
                Vibrate(call.Arguments as string);
                return Done;
            case "SystemChrome.setSystemUIOverlayStyle":
                ApplyOverlayStyle(SystemUiOverlayStyle.FromMap((System.Collections.IDictionary)call.Arguments!));
                return Done;
            case "SystemChrome.setApplicationSwitcherDescription":
                var description = (System.Collections.IDictionary)call.Arguments!;
                int color = description["primaryColor"] switch
                {
                    int value => value,
                    long value => unchecked((int)value),
                    _ => 0,
                };
#pragma warning disable CA1422, CS0618 // TaskDescription(label, icon, color) is what PlatformPlugin calls.
                _activity.SetTaskDescription(new ActivityManager.TaskDescription(
                    description["label"] as string, (global::Android.Graphics.Bitmap?)null,
                    new global::Android.Graphics.Color(unchecked(color | (int)0xFF000000))));
#pragma warning restore CA1422, CS0618
                return Done;
            case "SystemNavigator.setFrameworkHandlesBack":
                _frameworkHandlesBackChanged(call.Arguments is true);
                return Done;
            case "SystemNavigator.pop":
                // FlutterActivity finishes itself: as a plain Activity it has no OnBackPressedDispatcher.
                _activity.Finish();
                return Done;
            default:
                // Dart: "calls to methods that are not implemented on the shell side are ignored".
                throw new MissingPluginException(call.Method);
        }
    }

    private static Task<object?> Done => Task.FromResult<object?>(null);

    private ClipboardManager? Clipboard => _activity.GetSystemService(Context.ClipboardService) as ClipboardManager;

    private View? DecorView => _activity.Window?.DecorView;

    // PlatformPlugin.getClipboardData: the first item coerced to text, for text/plain only.
    private Dictionary<string, object?>? GetClipboardText(string? format)
    {
        if (format is not null and not "text/plain" || Clipboard is not { HasPrimaryClip: true } clipboard
            || clipboard.PrimaryClip is not { ItemCount: > 0 } clip)
        {
            return null;
        }

        string? text = clip.GetItemAt(0)?.CoerceToText(_activity)?.ToString();
        return text is null ? null : new Dictionary<string, object?> { ["text"] = text };
    }

    // PlatformPlugin.vibrateHapticFeedback: the decor view's constant for Dart's HapticFeedbackType (null is the
    // standard long press); the notification types need Android 11.
    private void Vibrate(string? type)
    {
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
            DecorView?.PerformHapticFeedback(feedback);
        }
    }

    // PlatformPlugin.setSystemChromeSystemUIOverlayStyle: icon brightness, bar colours (before Android 15, which
    // draws the app behind transparent bars) and contrast enforcement, each only when the style sets it.
    private void ApplyOverlayStyle(SystemUiOverlayStyle style)
    {
        if (_activity.Window is not { } window || window.DecorView is not { } view)
        {
            return;
        }

        bool api35 = OperatingSystem.IsAndroidVersionAtLeast(35);
        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            window.ClearFlags(WindowManagerFlags.TranslucentStatus | WindowManagerFlags.TranslucentNavigation);
        }

        if (style.StatusBarIconBrightness is { } statusIcons)
        {
            SetLightBars(window, view, status: true, light: statusIcons == Brightness.Dark);
        }

#pragma warning disable CA1422 // Ignored from Android 15, where the app draws behind the bars.
        if (style.StatusBarColor is { } statusBarColor && !api35)
        {
            window.SetStatusBarColor(ToAndroidColor(statusBarColor));
        }

        if (style.SystemStatusBarContrastEnforced is { } statusContrast && OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            window.StatusBarContrastEnforced = statusContrast;
        }

        // Navigation bar icons and colour from Android 8, as the engine does.
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            if (style.SystemNavigationBarIconBrightness is { } navigationIcons)
            {
                SetLightBars(window, view, status: false, light: navigationIcons == Brightness.Dark);
            }

            if (style.SystemNavigationBarColor is { } navigationBarColor && !api35)
            {
                window.SetNavigationBarColor(ToAndroidColor(navigationBarColor));
            }
        }

        if (style.SystemNavigationBarDividerColor is { } dividerColor && OperatingSystem.IsAndroidVersionAtLeast(28)
            && !api35)
        {
            window.NavigationBarDividerColor = ToAndroidColor(dividerColor);
        }
#pragma warning restore CA1422

        if (style.SystemNavigationBarContrastEnforced is { } navigationContrast
            && OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            window.NavigationBarContrastEnforced = navigationContrast;
        }
    }

    // Dark icons on a light bar: WindowInsetsController appearance on Android 11+, the decor view's flags before
    // (WindowInsetsControllerCompat; light navigation bars from Android 8, which the caller checks).
    private static void SetLightBars(Window window, View view, bool status, bool light)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(30) && window.InsetsController is { } controller)
        {
            var appearance = status
                ? WindowInsetsControllerAppearance.LightStatusBars
                : WindowInsetsControllerAppearance.LightNavigationBars;
            controller.SetSystemBarsAppearance(light ? (int)appearance : 0, (int)appearance);
            return;
        }

#pragma warning disable CA1422, CS0618 // The pre-Android 11 light-bar flags.
        SystemUiFlags flag = status ? SystemUiFlags.LightStatusBar
            : OperatingSystem.IsAndroidVersionAtLeast(26) ? SystemUiFlags.LightNavigationBar
            : 0;
        var flags = (SystemUiFlags)view.SystemUiVisibility;
        view.SystemUiVisibility = (StatusBarVisibility)(light ? flags | flag : flags & ~flag);
#pragma warning restore CA1422, CS0618
    }

    private static global::Android.Graphics.Color ToAndroidColor(Color color) =>
        new(unchecked((int)color.ToARGB32()));
}
