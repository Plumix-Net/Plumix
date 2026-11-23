using Foundation;
using Plumix.iOS;
using Plumix.Widgets;
using UIKit;

namespace PlumixTemplateApp.iOS;

public static class Program
{
    private static void Main(string[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}

[Register("AppDelegate")]
public sealed class AppDelegate : UIApplicationDelegate
{
    public override UISceneConfiguration GetConfiguration(
        UIApplication application, UISceneSession connectingSceneSession, UISceneConnectionOptions options) =>
        new(null, connectingSceneSession.Role) { DelegateType = typeof(SceneDelegate) };
}

// One window scene whose root is the Plumix view controller; the scene also saves and restores the framework's
// restoration data.
[Register("SceneDelegate")]
public sealed class SceneDelegate : PlumixSceneDelegate
{
    protected override PlumixViewController CreateViewController() => new MainViewController();
}

public sealed class MainViewController : PlumixViewController
{
    // A restoration identifier turns on the framework's state restoration (MaterialApp.restorationScopeId).
    public MainViewController()
    {
        RestorationIdentifier = "main";
    }

    protected override Widget CreateApp() => new MyApp();
}
