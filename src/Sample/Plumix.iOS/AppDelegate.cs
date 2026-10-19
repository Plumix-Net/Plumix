using Foundation;
using UIKit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.iOS;
using Avalonia.Media;
using Avalonia.Threading;
using System.Collections.Generic;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/main.dart (platform host bootstrap, adapted)

namespace Plumix.iOS;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
[Register("AppDelegate")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public partial class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    private const string FrameworkRestorationStateKey = "FlutterRestorationStateAppData";
    private readonly IOSRestorationPlugin _restorationPlugin = new(restorationEnabled: true);
    private readonly List<NSObject> _lifecycleObservers;

    public AppDelegate()
    {
        NSNotificationCenter center = NSNotificationCenter.DefaultCenter;
        _lifecycleObservers =
        [
            center.AddObserver(
                UIApplication.DidBecomeActiveNotification,
                _ =>
                {
                    // UIKit has finished decoding saved state before activation. Avalonia owns the
                    // view controller, so this is our equivalent of Flutter's viewWillAppear hook.
                    _restorationPlugin.MarkRestorationComplete();
                    DispatchLifecycleState(AppLifecycleState.Resumed);
                }),
            center.AddObserver(
                UIApplication.WillResignActiveNotification,
                _ => DispatchLifecycleState(AppLifecycleState.Inactive)),
            center.AddObserver(
                UIApplication.DidEnterBackgroundNotification,
                _ => DispatchLifecycleState(AppLifecycleState.Paused)),
            center.AddObserver(
                UIApplication.WillEnterForegroundNotification,
                _ => DispatchLifecycleState(AppLifecycleState.Inactive)),
            center.AddObserver(
                UIApplication.WillTerminateNotification,
                _ => DispatchLifecycleState(AppLifecycleState.Detached)),
        ];
    }

    [Export("application:shouldSaveSecureApplicationState:")]
    public bool ShouldSaveSecureApplicationState(UIApplication application, NSCoder coder) => true;

    [Export("application:shouldRestoreSecureApplicationState:")]
    public bool ShouldRestoreSecureApplicationState(UIApplication application, NSCoder coder) => true;

    [Export("application:willEncodeRestorableStateWithCoder:")]
    public void WillEncodeRestorableState(UIApplication application, NSCoder coder)
    {
        coder.Encode(_restorationPlugin.RestorationData ?? System.Array.Empty<byte>(), FrameworkRestorationStateKey);
    }

    [Export("application:didDecodeRestorableStateWithCoder:")]
    public void DidDecodeRestorableState(UIApplication application, NSCoder coder)
    {
        _restorationPlugin.RestorationData = coder.ContainsKey(FrameworkRestorationStateKey)
            ? coder.DecodeBytes(FrameworkRestorationStateKey)
            : null;
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            // The shared App creates its host in OnFrameworkInitializationCompleted, after this callback.
            .AfterSetup(_ => Dispatcher.UIThread.Post(AttachAccessibilityBridge));
    }

    private static void AttachAccessibilityBridge()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime
            {
                MainView: PlumixHost host,
            })
        {
            IOSAccessibilityBridge.Attach(host);
        }
    }

    private static void DispatchLifecycleState(AppLifecycleState state)
    {
        WidgetsBinding.Instance.HandleAppLifecycleStateChanged(state);
    }
}
