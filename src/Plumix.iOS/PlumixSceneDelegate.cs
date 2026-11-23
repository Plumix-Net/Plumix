using Foundation;
using UIKit;

namespace Plumix.iOS;

// C#-only host infrastructure parity sources:
// flutter/engine/src/flutter/shell/platform/darwin/ios/framework/Source/FlutterSceneDelegate.mm and
// FlutterSceneLifeCycle.mm (stateRestorationActivityForScene:, scene:restoreInteractionStateWithUserActivity:).
// A scene-based app saves its state in the scene's user activity: the restoration data of the root view controller
// under its restoration identifier, with the app bundle's modification date so that state from an older build is not
// restored.

/// <summary>
/// A window scene delegate that shows a <see cref="PlumixViewController"/> and saves and restores the framework's
/// restoration data with the scene, as the engine's FlutterSceneDelegate does.
/// </summary>
public abstract class PlumixSceneDelegate : UIResponder, IUIWindowSceneDelegate
{
    // kRestorationStateAppModificationKey.
    private const string RestorationStateAppModificationKey = "mod-date";

    [Export("window")]
    public UIWindow? Window { get; set; }

    /// <summary>The scene's root view controller.</summary>
    protected abstract PlumixViewController CreateViewController();

    [Export("scene:willConnectToSession:options:")]
    public virtual void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        Window = new UIWindow((UIWindowScene)scene) { RootViewController = CreateViewController() };
        Window.MakeKeyAndVisible();
    }

    // stateRestorationActivityForScene: the scene's activity carrying the view controller's restoration data.
    [Export("stateRestorationActivityForScene:")]
    public virtual NSUserActivity? GetStateRestorationActivity(UIScene scene)
    {
        NSUserActivity activity = scene.UserActivity
                                  ?? new NSUserActivity(scene.Session.Configuration.Name ?? "PlumixScene");
        if (Window?.RootViewController is PlumixViewController { RestorationIdentifier: { } restorationId } controller
            && controller.RestorationData is { } data)
        {
            activity.AddUserInfoEntries(NSDictionary.FromObjectsAndKeys(
                [NSData.FromArray(data), NSNumber.FromInt64(LastAppModificationTime())],
                [new NSString(restorationId), new NSString(RestorationStateAppModificationKey)]));
        }

        return activity;
    }

    // scene:restoreInteractionStateWithUserActivity: nothing is restored after the app was reinstalled.
    [Export("scene:restoreInteractionStateWithUserActivity:")]
    public virtual void RestoreInteractionState(UIScene scene, NSUserActivity? stateRestorationActivity)
    {
        // UIKit calls this with no activity when it has nothing saved.
        NSDictionary? userInfo = stateRestorationActivity?.UserInfo;
        if (userInfo is null)
        {
            return;
        }

        long stateDate = (userInfo?[RestorationStateAppModificationKey] as NSNumber)?.Int64Value ?? 0;
        if (stateDate != LastAppModificationTime())
        {
            return;
        }

        if (Window?.RootViewController is PlumixViewController { RestorationIdentifier: { } restorationId } controller
            && userInfo?[restorationId] is NSData data)
        {
            controller.RestorationData = data.ToArray();
        }
    }

    /// <summary>
    /// FlutterSharedApplication.lastAppModificationTime: the executable's modification date, in seconds.
    /// </summary>
    internal static long LastAppModificationTime()
    {
        NSUrl? executable = NSBundle.MainBundle.ExecutableUrl;
        if (executable is null
            || !executable.TryGetResource(NSUrl.ContentModificationDateKey, out NSObject? value)
            || value is not NSDate date)
        {
            return 0;
        }

        return (long)date.SecondsSince1970;
    }
}
