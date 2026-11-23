using System.Collections;
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;
using Plumix.UI;
using UIKit;

namespace Plumix.iOS;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/darwin/ios/framework/Source/FlutterPlatformPlugin.mm (the
// `flutter/platform` methods). The engine posts the status bar style and the orientation preferences as
// notifications its FlutterViewController observes; here the plugin calls the view controller directly. The
// system context menu belongs to the text input plugin and is answered there.

/// <summary>Answers the framework's <c>flutter/platform</c> calls for the view controller.</summary>
internal sealed class IosPlatformChannel : IDisposable
{
    // FlutterPlatformPlugin.mm's kKeyPressClickSoundId and kWheelsOfTimeSoundId; AudioToolbox's
    // kSystemSoundID_Vibrate.
    private const uint KeyPressClickSoundId = 1306;
    private const uint WheelsOfTimeSoundId = 1157;
    private const uint SystemSoundIdVibrate = 4095;
    private const string TextPlainFormat = "text/plain";
    private const string SearchUrlPrefix = "x-web-search://?";

    private readonly PlumixViewController _viewController;

    // enableViewControllerBasedStatusBarAppearance: Info.plist's UIViewControllerBasedStatusBarAppearance,
    // YES when absent.
    private readonly bool _viewControllerBasedStatusBarAppearance =
        NSBundle.MainBundle.ObjectForInfoDictionary("UIViewControllerBasedStatusBarAppearance") is not NSNumber value
        || value.BoolValue;

    private UITextField? _textField;

    public IosPlatformChannel(PlumixViewController viewController)
    {
        _viewController = viewController;
        SystemChannels.Platform.SetPlatformMethodCallHandler(Handle);
    }

    /// <summary>The text input plugin's edit menu, for <c>ContextMenu.*</c>.</summary>
    public IosTextInputPlugin? TextInput { get; set; }

    public void Dispose() => SystemChannels.Platform.SetPlatformMethodCallHandler(null);

    private Task<object?> Handle(MethodCall call)
    {
        object? args = call.Arguments;
        switch (call.Method)
        {
            case "SystemSound.play":
                PlaySystemSound(args as string);
                return Done;
            case "HapticFeedback.vibrate":
                VibrateHapticFeedback(args as string);
                return Done;
            case "SystemChrome.setPreferredOrientations":
                SetPreferredOrientations(args as IList);
                return Done;
            case "SystemChrome.setApplicationSwitcherDescription":
                // "No counterpart on iOS but is a benign operation."
                return Done;
            case "SystemChrome.setEnabledSystemUIOverlays":
                SetEnabledSystemUIOverlays(args as IList ?? Array.Empty<object>());
                return Done;
            case "SystemChrome.setEnabledSystemUIMode":
                SetEnabledSystemUIMode(args as string);
                return Done;
            case "SystemChrome.restoreSystemUIOverlays":
                return Done;
            case "SystemChrome.setSystemUIOverlayStyle":
                SetSystemUIOverlayStyle(args as IDictionary);
                return Done;
            case "SystemNavigator.pop":
                _viewController.PopSystemNavigator(args is true);
                return Done;
            case "Clipboard.getData":
                return Task.FromResult<object?>(GetClipboardData(args as string));
            case "Clipboard.setData":
                // setClipboardData: a non-string `text` is written as the string "null".
                UIPasteboard.General.String = (args as IDictionary)?["text"] as string ?? "null";
                return Done;
            case "Clipboard.hasStrings":
                return Task.FromResult<object?>(
                    new Dictionary<string, object?> { ["value"] = UIPasteboard.General.HasStrings });
            case "LiveText.isLiveTextInputAvailable":
                _textField ??= new UITextField();
                return Task.FromResult<object?>(
                    _textField.CanPerform(new Selector("captureTextFromCamera:"), null));
            case "SearchWeb.invoke":
                SearchWeb(args as string ?? string.Empty);
                return Done;
            case "LookUp.invoke":
                _viewController.PresentViewController(
                    new UIReferenceLibraryViewController(args as string ?? string.Empty), true, null);
                return Done;
            case "Share.invoke":
                ShowShareViewController(args as string);
                return Done;
            case "ContextMenu.showSystemContextMenu":
                if (OperatingSystem.IsIOSVersionAtLeast(16) && TextInput?.ShowEditMenu(args as IDictionary) != true)
                {
                    Console.Error.WriteLine(
                        "Only text input supports system context menu for now. Ensure the system context menu is "
                        + "shown with an active text input connection.");
                }

                return Done;
            case "ContextMenu.hideSystemContextMenu":
                if (OperatingSystem.IsIOSVersionAtLeast(16))
                {
                    TextInput?.HideEditMenu();
                }

                return Done;
            default:
                throw new MissingPluginException(call.Method);
        }
    }

    private static Task<object?> Done => Task.FromResult<object?>(null);

    // playSystemSound: "All feedback types are specific to Android and are treated as equal on iOS".
    private static void PlaySystemSound(string? soundType)
    {
        if (soundType == "SystemSoundType.click")
        {
            AudioServicesPlaySystemSound(KeyPressClickSoundId);
        }
        else if (soundType == "SystemSoundType.tick")
        {
            AudioServicesPlaySystemSound(WheelsOfTimeSoundId);
        }
    }

    private static void VibrateHapticFeedback(string? feedbackType)
    {
        switch (feedbackType)
        {
            case null:
                AudioServicesPlaySystemSound(SystemSoundIdVibrate);
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
                new UISelectionFeedbackGenerator().SelectionChanged();
                break;
            case "HapticFeedbackType.successNotification":
                new UINotificationFeedbackGenerator().NotificationOccurred(UINotificationFeedbackType.Success);
                break;
            case "HapticFeedbackType.warningNotification":
                new UINotificationFeedbackGenerator().NotificationOccurred(UINotificationFeedbackType.Warning);
                break;
            case "HapticFeedbackType.errorNotification":
                new UINotificationFeedbackGenerator().NotificationOccurred(UINotificationFeedbackType.Error);
                break;
        }
    }

    private static void Impact(UIImpactFeedbackStyle style)
    {
        // The engine creates the generator with `initWithStyle:`, which iOS 17.5 deprecates.
#pragma warning disable CA1422
        new UIImpactFeedbackGenerator(style).ImpactOccurred();
#pragma warning restore CA1422
    }

    // setSystemChromePreferredOrientations: an empty list allows all; an unknown-only list changes nothing.
    private void SetPreferredOrientations(IList? orientations)
    {
        UIInterfaceOrientationMask mask = 0;
        if (orientations is null || orientations.Count == 0)
        {
            mask = UIInterfaceOrientationMask.All;
        }
        else
        {
            foreach (object? orientation in orientations)
            {
                mask |= orientation switch
                {
                    "DeviceOrientation.portraitUp" => UIInterfaceOrientationMask.Portrait,
                    "DeviceOrientation.portraitDown" => UIInterfaceOrientationMask.PortraitUpsideDown,
                    "DeviceOrientation.landscapeLeft" => UIInterfaceOrientationMask.LandscapeLeft,
                    "DeviceOrientation.landscapeRight" => UIInterfaceOrientationMask.LandscapeRight,
                    _ => 0,
                };
            }
        }

        if (mask != 0)
        {
            _viewController.PerformOrientationUpdate(mask);
        }
    }

    private void SetEnabledSystemUIOverlays(IList overlays)
    {
        bool statusBarShouldBeHidden = !overlays.Contains("SystemUiOverlay.top");
        _viewController.HomeIndicatorHidden = !overlays.Contains("SystemUiOverlay.bottom");
        SetStatusBarHidden(statusBarShouldBeHidden);
    }

    private void SetEnabledSystemUIMode(string? mode)
    {
        bool edgeToEdge = mode == "SystemUiMode.edgeToEdge";
        SetStatusBarHidden(!edgeToEdge);
        _viewController.HomeIndicatorHidden = !edgeToEdge;
    }

    private void SetStatusBarHidden(bool hidden)
    {
        if (_viewControllerBasedStatusBarAppearance)
        {
            _viewController.FlutterPrefersStatusBarHidden = hidden;
        }
        else
        {
#pragma warning disable CA1422 // The engine sets the application's status bar when view-controller based is off.
            UIApplication.SharedApplication.StatusBarHidden = hidden;
#pragma warning restore CA1422
        }
    }

    // setSystemChromeSystemUIOverlayStyle: only statusBarBrightness matters on iOS.
    private void SetSystemUIOverlayStyle(IDictionary? message)
    {
        UIStatusBarStyle style;
        switch (message?["statusBarBrightness"])
        {
            case "Brightness.dark":
                style = UIStatusBarStyle.LightContent;
                break;
            case "Brightness.light":
                style = UIStatusBarStyle.DarkContent;
                break;
            default:
                return;
        }

        if (_viewControllerBasedStatusBarAppearance)
        {
            _viewController.StatusBarStyle = style;
        }
        else
        {
#pragma warning disable CA1422
            UIApplication.SharedApplication.StatusBarStyle = style;
#pragma warning restore CA1422
        }
    }

    // getClipboardData: the pasteboard's string, for text/plain (or no format) only.
    private static Dictionary<string, object?>? GetClipboardData(string? format)
    {
        if (format is not null && format != TextPlainFormat)
        {
            return null;
        }

        string? text = UIPasteboard.General.String;
        return text is null ? null : new Dictionary<string, object?> { ["text"] = text };
    }

    /// <summary>searchWeb:, also for the text input's edit menu.</summary>
    internal static void SearchWeb(string searchTerm)
    {
        string escaped = new NSString(searchTerm).CreateStringByAddingPercentEncoding(
            NSUrlUtilities_NSCharacterSet.UrlHostAllowedCharacterSet) ?? string.Empty;
        if (NSUrl.FromString(SearchUrlPrefix + escaped) is { } url)
        {
            UIApplication.SharedApplication.OpenUrl(url, new UIApplicationOpenUrlOptions(), null);
        }
    }

    /// <summary>showShareViewController:, also for the text input's edit menu.</summary>
    internal void ShowShareViewController(string? content)
    {
        NSObject item = content is null ? NSNull.Null : new NSString(content);
        var activity = new UIActivityViewController([item], null);
        if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad
            && activity.PopoverPresentationController is { } popover)
        {
            // On iPad the share sheet is a popover from the selection, as the engine computes it from the text
            // input view's caret rects.
            popover.SourceView = _viewController.View!;
            popover.SourceRect = TextInput?.SelectionRectInView() ?? default;
        }

        _viewController.PresentViewController(activity, true, null);
    }

    [DllImport("/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox")]
    private static extern void AudioServicesPlaySystemSound(uint soundId);
}
