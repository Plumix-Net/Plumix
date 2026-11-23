using CoreGraphics;
using Foundation;
using Plumix.UI;
using Plumix.Widgets;
using UIKit;

namespace Plumix.Impeller.iOS;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/darwin/ios/framework/Source/FlutterViewController.mm: the platform seams
// are installed once, the Metal view is created and the app runs; the application lifecycle follows UIApplication's
// notifications (resign active → inactive, background → paused, foreground → inactive, active → resumed); user
// settings follow trait and content-size changes, locales NSCurrentLocaleDidChangeNotification (FlutterEngine), the
// accessibility features their status notifications; the status bar style, its visibility, the home indicator and the
// orientations are the platform plugin's (the engine routes them through notifications); a zero-height scroll view
// turns status bar taps into `handleScrollToTop`.

/// <summary>The view controller of a Plumix app on the native iOS host.</summary>
public abstract class PlumixViewController : UIViewController
{
    // FlutterViewController.mm's kScrollViewContentSize.
    private const double ScrollViewContentSize = 2.0;

    private readonly List<NSObject> _observers = [];
    private IosImpellerHost? _host;
    private IosPlatformChannel? _platformChannel;
    private StatusBarTapDelegate? _statusBarTapDelegate;
    private IosTextInputPlugin? _textInputPlugin;
    private IosKeyboardInsetManager? _keyboardInsetManager;
    private IosAccessibilityBridge? _accessibilityBridge;
    private readonly IosKeyboardManager _keyboardManager = new();
    private UIStatusBarStyle _statusBarStyle = UIStatusBarStyle.Default;
    private bool _flutterPrefersStatusBarHidden;
    private bool _homeIndicatorHidden;
    private UIInterfaceOrientationMask _orientationPreferences = UIInterfaceOrientationMask.All;

    /// <summary>The app's root widget.</summary>
    protected abstract Widget CreateApp();

    public override void LoadView()
    {
        IosImpellerHost.InstallBackend(GetType().Assembly.GetName().Name ?? string.Empty);
        _host = new IosImpellerHost();
        View = new PlumixMetalView(_host, this)
        {
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
        };

        // Status bar taps reach the topmost scroll view's delegate as scrollViewShouldScrollToTop:.
        var scrollView = new UIScrollView
        {
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth,
            BackgroundColor = UIColor.White,
            ContentSize = new CGSize(ScrollViewContentSize, ScrollViewContentSize),
            ContentOffset = new CGPoint(ScrollViewContentSize, ScrollViewContentSize),
            Delegate = _statusBarTapDelegate = new StatusBarTapDelegate(this),
        };
        View.AddSubview(scrollView);
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        IosImpellerHost host = _host!;
        _platformChannel = new IosPlatformChannel(this);
        _textInputPlugin = new IosTextInputPlugin(this, View!);
        _platformChannel.TextInput = _textInputPlugin;
        _accessibilityBridge = new IosAccessibilityBridge(
            host.Accessibility, this, View!, () => _textInputPlugin?.ActiveView);
        _keyboardInsetManager = new IosKeyboardInsetManager(
            this, () => host.ViewInsetBottom, inset => host.ViewInsetBottom = inset);
        foreach (NSString name in new[]
                 {
                     UIKeyboard.WillChangeFrameNotification, UIKeyboard.WillShowNotification,
                     UIKeyboard.WillHideNotification,
                 })
        {
            _observers.Add(NSNotificationCenter.DefaultCenter.AddObserver(
                name, notification => _keyboardInsetManager?.HandleKeyboardNotification(notification)));
        }

        Observe(UIApplication.WillResignActiveNotification, () => host.LifecycleChanged(AppLifecycleState.Inactive));
        Observe(UIApplication.DidEnterBackgroundNotification, () => host.LifecycleChanged(AppLifecycleState.Paused));
        Observe(UIApplication.WillEnterForegroundNotification, () => host.LifecycleChanged(AppLifecycleState.Inactive));
        Observe(UIApplication.DidBecomeActiveNotification, () => host.LifecycleChanged(AppLifecycleState.Resumed));
        Observe(UIApplication.ContentSizeCategoryChangedNotification, ReportUserSettings);
        Observe(NSLocale.CurrentLocaleDidChangeNotification, IosSystemSettings.ReportLocales);
        foreach (NSString name in IosSystemSettings.AccessibilityNotifications)
        {
            Observe(name, ReportAccessibilityFeatures);
        }

        IosSystemSettings.ReportLocales();
        ReportUserSettings();
        ReportAccessibilityFeatures();
        host.Run(CreateApp(), UIScreen.MainScreen.Scale);
    }

    // traitCollectionDidChange: → onUserSettingsChanged:. iOS 17 deprecates the override for trait registration.
#pragma warning disable CA1422
    public override void TraitCollectionDidChange(UITraitCollection? previousTraitCollection)
    {
        base.TraitCollectionDidChange(previousTraitCollection);
        ReportUserSettings();
    }
#pragma warning restore CA1422

    // pressesBegan: and friends: each press goes to the keyboard manager; one nobody took goes on to UIKit.
    public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        foreach (UIPress press in presses)
        {
            _keyboardManager.HandlePress(press, () => base.PressesBegan(new NSSet<UIPress>(press), evt));
        }
    }

    public override void PressesChanged(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        foreach (UIPress press in presses)
        {
            _keyboardManager.HandlePress(press, () => base.PressesChanged(new NSSet<UIPress>(press), evt));
        }
    }

    public override void PressesEnded(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        foreach (UIPress press in presses)
        {
            _keyboardManager.HandlePress(press, () => base.PressesEnded(new NSSet<UIPress>(press), evt));
        }
    }

    public override void PressesCancelled(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        foreach (UIPress press in presses)
        {
            _keyboardManager.HandlePress(press, () => base.PressesCancelled(new NSSet<UIPress>(press), evt));
        }
    }

    public override UIStatusBarStyle PreferredStatusBarStyle() => _statusBarStyle;

    public override bool PrefersStatusBarHidden() => _flutterPrefersStatusBarHidden;

    public override bool PrefersHomeIndicatorAutoHidden => _homeIndicatorHidden;

    public override bool ShouldAutorotate() => true;

    public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations() => _orientationPreferences;

    /// <summary>The platform plugin, for the text input's share and search actions.</summary>
    internal IosPlatformChannel? PlatformChannel => _platformChannel;

    /// <summary>onPreferredStatusBarStyleUpdated:.</summary>
    internal UIStatusBarStyle StatusBarStyle
    {
        set
        {
            if (value != _statusBarStyle)
            {
                _statusBarStyle = value;
                SetNeedsStatusBarAppearanceUpdate();
            }
        }
    }

    /// <summary>setPrefersStatusBarHidden:.</summary>
    internal bool FlutterPrefersStatusBarHidden
    {
        set
        {
            if (value != _flutterPrefersStatusBarHidden)
            {
                _flutterPrefersStatusBarHidden = value;
                SetNeedsStatusBarAppearanceUpdate();
            }
        }
    }

    /// <summary>setIsHomeIndicatorHidden:.</summary>
    internal bool HomeIndicatorHidden
    {
        set
        {
            if (value != _homeIndicatorHidden)
            {
                _homeIndicatorHidden = value;
                SetNeedsUpdateOfHomeIndicatorAutoHidden();
            }
        }
    }

    /// <summary>
    /// performOrientationUpdate: iOS 16 asks the window scenes for the geometry; earlier versions rotate to the first
    /// allowed orientation when the current one is not allowed.
    /// </summary>
    internal void PerformOrientationUpdate(UIInterfaceOrientationMask preferences)
    {
        if (preferences == _orientationPreferences)
        {
            return;
        }

        _orientationPreferences = preferences;
        if (OperatingSystem.IsIOSVersionAtLeast(16))
        {
            foreach (UIScene scene in UIApplication.SharedApplication.ConnectedScenes)
            {
                if (scene is UIWindowScene windowScene)
                {
                    windowScene.RequestGeometryUpdate(
                        new UIWindowSceneGeometryPreferencesIOS(preferences),
                        error => Console.Error.WriteLine($"Failed to change device orientation: {error}"));
                    SetNeedsUpdateOfSupportedInterfaceOrientations();
                }
            }

            return;
        }

        if (View?.Window?.WindowScene is not { } current)
        {
            return;
        }

        var currentMask = (UIInterfaceOrientationMask)(1UL << (int)current.InterfaceOrientation);
        if ((preferences & currentMask) != 0)
        {
            return;
        }

#pragma warning disable CA1422 // The pre-iOS 16 path of the engine.
        AttemptRotationToDeviceOrientation();
#pragma warning restore CA1422
        UIInterfaceOrientation? target =
            (preferences & UIInterfaceOrientationMask.Portrait) != 0 ? UIInterfaceOrientation.Portrait
            : (preferences & UIInterfaceOrientationMask.PortraitUpsideDown) != 0
                ? UIInterfaceOrientation.PortraitUpsideDown
            : (preferences & UIInterfaceOrientationMask.LandscapeLeft) != 0 ? UIInterfaceOrientation.LandscapeLeft
            : (preferences & UIInterfaceOrientationMask.LandscapeRight) != 0 ? UIInterfaceOrientation.LandscapeRight
            : null;
        if (target is { } orientation)
        {
            // "No official API but more like a workaround": key-value coding on the read-only property.
            UIDevice.CurrentDevice.SetValueForKey(NSNumber.FromInt64((long)orientation), new NSString("orientation"));
        }
    }

    /// <summary>
    /// popSystemNavigator: Apple's guidelines say not to terminate iOS applications. A controller inside a navigation
    /// controller is popped from it; one that is not the key window's root is dismissed.
    /// </summary>
    internal void PopSystemNavigator(bool animated)
    {
        if (NavigationController is { } navigationController)
        {
            navigationController.PopViewController(animated);
        }
        else if (View?.Window?.WindowScene?.KeyWindow?.RootViewController != this)
        {
            DismissViewController(animated, null);
        }
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
            _platformChannel?.Dispose();
            _platformChannel = null;
            _textInputPlugin?.Dispose();
            _textInputPlugin = null;
            _keyboardInsetManager?.Dispose();
            _keyboardInsetManager = null;
            _accessibilityBridge?.Dispose();
            _accessibilityBridge = null;
        }

        base.Dispose(disposing);
    }

    private void ReportUserSettings()
    {
        if (_host is { } host)
        {
            IosSystemSettings.ReportUserSettings(host, TraitCollection);
        }
    }

    // onAccessibilityStatusChanged: semantics follow VoiceOver, Switch Control and Speak Screen.
    private void ReportAccessibilityFeatures()
    {
        bool enabled = IosSystemSettings.ReportAccessibilityFeatures();
        if (_host is not { } host)
        {
            return;
        }

        if (enabled)
        {
            host.Accessibility.Activate();
        }
        else
        {
            host.Accessibility.Deactivate();
        }
    }

    /// <summary>
    /// flutterViewAccessibilityDidCall: UIKit asking the view about accessibility means some assistive technology runs
    /// (Voice Control has no status API), so semantics turn on while no tree is shown yet.
    /// </summary>
    internal void FlutterViewAccessibilityDidCall()
    {
        if (ViewIfLoaded is not { } view || IosAccessibilityBridge.GetAccessibilityElements(view) is null)
        {
            _host?.Accessibility.Activate();
        }
    }

    /// <summary>isPresentingViewControllerAnimating: set while a presentation runs.</summary>
    internal bool IsPresentingViewControllerAnimating { get; private set; }

    public override void PresentViewController(
        UIViewController viewControllerToPresent, bool animated, Action? completionHandler)
    {
        IsPresentingViewControllerAnimating = true;
        base.PresentViewController(viewControllerToPresent, animated, () =>
        {
            IsPresentingViewControllerAnimating = false;
            completionHandler?.Invoke();
        });
    }

    private void Observe(NSString name, Action handler) =>
        _observers.Add(NSNotificationCenter.DefaultCenter.AddObserver(name, _ => handler()));

    private sealed class StatusBarTapDelegate(PlumixViewController owner) : UIScrollViewDelegate
    {
        // scrollViewShouldScrollToTop: status bar taps before the UI is visible are ignored.
        public override bool ShouldScrollToTop(UIScrollView scrollView)
        {
            if (owner.IsViewLoaded)
            {
                owner._host?.StatusBarTapped();
            }

            return false;
        }
    }
}
