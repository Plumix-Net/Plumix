using Foundation;
using Plumix.iOS;
using Plumix.Widgets;
using UIKit;

namespace Plumix.Sample.Impeller.iOS;

// C#-only host infrastructure; no Dart parity source. The Sample gallery on the native iOS host: one window scene
// whose root is the Plumix view controller.

public static class Program
{
    private static void Main(string[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}

[Register("AppDelegate")]
#pragma warning disable CA1711 // UIKit's naming.
public sealed class AppDelegate : UIApplicationDelegate
#pragma warning restore CA1711
{
    public override UISceneConfiguration GetConfiguration(
        UIApplication application, UISceneSession connectingSceneSession, UISceneConnectionOptions options) =>
        new(null, connectingSceneSession.Role) { DelegateType = typeof(SceneDelegate) };
}

[Register("SceneDelegate")]
public sealed class SceneDelegate : UIResponder, IUIWindowSceneDelegate
{
    [Export("window")]
    public UIWindow? Window { get; set; }

    [Export("scene:willConnectToSession:options:")]
    public void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        Window = new UIWindow((UIWindowScene)scene) { RootViewController = new GalleryViewController() };
        Window.MakeKeyAndVisible();
    }
}

public sealed class GalleryViewController : PlumixViewController
{
    protected override Widget CreateApp() => new CounterApp();
}
