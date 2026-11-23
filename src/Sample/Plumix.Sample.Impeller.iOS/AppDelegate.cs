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
public sealed class SceneDelegate : PlumixSceneDelegate
{
    protected override PlumixViewController CreateViewController() => new GalleryViewController();
}

public sealed class GalleryViewController : PlumixViewController
{
    // A restoration identifier turns on the framework's state restoration, as in the engine.
    public GalleryViewController()
    {
        RestorationIdentifier = "gallery";
    }

    protected override Widget CreateApp() => new CounterApp();
}
