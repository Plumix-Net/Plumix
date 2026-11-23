using Foundation;
using Plumix.UI;
using Plumix.Widgets;
using UIKit;

namespace Plumix.Impeller.iOS;

// C#-only host infrastructure; no Dart parity source. The engine's FlutterViewController for the stage 5 prototype:
// installs the platform seams once, creates the Metal view and runs the app, and reports the application lifecycle
// the way FlutterEngine maps UIApplication's notifications (resign active → inactive, background → paused,
// foreground → inactive, active → resumed).

/// <summary>The view controller of a Plumix app on the native iOS host.</summary>
public abstract class PlumixViewController : UIViewController
{
    private readonly List<NSObject> _observers = [];
    private IosImpellerHost? _host;

    /// <summary>The app's root widget.</summary>
    protected abstract Widget CreateApp();

    public override void LoadView()
    {
        IosImpellerHost.InstallBackend(GetType().Assembly.GetName().Name ?? string.Empty);
        _host = new IosImpellerHost();
        View = new PlumixMetalView(_host);
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        Observe(UIApplication.WillResignActiveNotification, AppLifecycleState.Inactive);
        Observe(UIApplication.DidEnterBackgroundNotification, AppLifecycleState.Paused);
        Observe(UIApplication.WillEnterForegroundNotification, AppLifecycleState.Inactive);
        Observe(UIApplication.DidBecomeActiveNotification, AppLifecycleState.Resumed);
        _host!.BrightnessChanged(TraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark
            ? Brightness.Dark
            : Brightness.Light);
        _host.Run(CreateApp(), UIScreen.MainScreen.Scale);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (NSObject observer in _observers)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(observer);
            }

            _observers.Clear();
        }

        base.Dispose(disposing);
    }

    private void Observe(NSString name, AppLifecycleState state) =>
        _observers.Add(NSNotificationCenter.DefaultCenter.AddObserver(name, _ => _host?.LifecycleChanged(state)));
}
