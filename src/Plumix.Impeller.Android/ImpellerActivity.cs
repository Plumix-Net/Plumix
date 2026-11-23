using System.Runtime.Versioning;
using Android.App;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using Android.Window;
using Plumix.UI;
using Plumix.Widgets;
using View = Android.Views.View;
using Widget = Plumix.Widgets.Widget;

namespace Plumix.Impeller.Android;

// C#-only host infrastructure; no Dart parity source. The prototype's Activity, the role FlutterActivity plays
// in the engine's Android embedding: a full-screen SurfaceView the ImpellerHost renders into, with lifecycle,
// insets, touches and back forwarded to it, the user settings and locales reported, `flutter/platform` answered,
// predictive back (FlutterActivity.setFrameworkHandlesBack, OnBackAnimationCallback) and restoration
// (`flutter/restoration`). An app derives from it, adds [Activity] and returns its root widget; for predictive back
// its manifest sets android:enableOnBackInvokedCallback="true". Launch extras: `--ez perf true` logs startup and
// frame timings (PerfProbe).

/// <summary>Runs a Plumix app in an Android Activity, rendered by Impeller.</summary>
public abstract class ImpellerActivity : Activity, ISurfaceHolderCallback
{
    private const string FrameworkRestorationBundleKey = "framework";

    // The engine's RestorationChannel outlives an Activity, like the rest of the process-wide backend.
    private static AndroidRestorationChannel? s_restorationChannel;

    private ImpellerHost? _host;
    private AndroidLifecycleChannel? _lifecycle;
    private AndroidAccessibilityFeatures? _accessibilityFeatures;
    private AndroidPlatformChannel? _platformChannel;
    private Java.Lang.Object? _backCallback;

    /// <summary>The app's root widget, created once the backend is installed.</summary>
    protected abstract Widget CreateApp();

    /// <summary>Runs after the app is scheduled to start; the framework's bindings exist.</summary>
    protected virtual void OnAppScheduled()
    {
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        PerfProbe.Enabled = Intent?.GetBooleanExtra("perf", false) == true;
        PerfProbe.Mark("activity-create");
        ImpellerHost.InstallBackend(Assets!, GetType().Assembly.GetName().Name!);
        PerfProbe.Mark("backend-installed");
        _host = new ImpellerHost();
        PerfProbe.Mark("host-created");
        _lifecycle = new AndroidLifecycleChannel(_host.LifecycleChanged);
        _platformChannel = new AndroidPlatformChannel(this, SetFrameworkHandlesBack);
        s_restorationChannel ??= new AndroidRestorationChannel(waitForRestorationData: true);
        // An absent bundle is an answer too: it releases WidgetsApp's initial restoration request.
        s_restorationChannel.SetRestorationData(savedInstanceState?.GetByteArray(FrameworkRestorationBundleKey));
        AndroidSystemSettings.Report(this, _host, Resources?.Configuration);

        var surfaceView = new PlumixSurfaceView(this);
        surfaceView.AccessibilityProvider = new PlumixAccessibilityNodeProvider(surfaceView, _host.Accessibility);
        surfaceView.Holder!.AddCallback(this);
        surfaceView.TouchProcessor = _host.TouchProcessor;
        surfaceView.SetOnApplyWindowInsetsListener(new InsetsListener(_host));
        SetContentView(surfaceView);
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            Window!.SetDecorFitsSystemWindows(false);
        }

        _host.Run(CreateApp(), Resources!.DisplayMetrics!.Density);
        _accessibilityFeatures = new AndroidAccessibilityFeatures(this, _host.Accessibility);
        if (PerfProbe.Enabled)
        {
            PerfProbe.Start();
        }

        OnAppScheduled();
    }

    protected override void OnSaveInstanceState(Bundle outState)
    {
        outState.PutByteArray(FrameworkRestorationBundleKey, s_restorationChannel?.RestorationData);
        base.OnSaveInstanceState(outState);
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        if (_host is not null)
        {
            AndroidSystemSettings.Report(this, _host, newConfig);
        }

        // The bold-text setting changes the configuration's font weight adjustment.
        _accessibilityFeatures?.Report();
    }

    protected override void OnResume()
    {
        base.OnResume();
        _lifecycle?.AppIsResumed();
    }

    protected override void OnPause()
    {
        _lifecycle?.AppIsInactive();
        base.OnPause();
    }

    protected override void OnStop()
    {
        _lifecycle?.AppIsPaused();
        base.OnStop();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus)
        {
            _lifecycle?.AWindowIsFocused();
        }
        else
        {
            _lifecycle?.NoWindowsAreFocused();
        }
    }

#pragma warning disable CA1422, CS0672, CS0618 // The prototype keeps the pre-predictive back path.
    public override async void OnBackPressed()
    {
        if (_host is null || !await _host.HandleBackAsync())
        {
            Finish();
        }
    }
#pragma warning restore CA1422, CS0672, CS0618

    protected override void OnDestroy()
    {
        _lifecycle?.AppIsDetached();
        _accessibilityFeatures?.Dispose();
        _accessibilityFeatures = null;
        SetFrameworkHandlesBack(false);
        _platformChannel?.Dispose();
        _platformChannel = null;
        _host?.Dispose();
        _host = null;
        base.OnDestroy();
    }

    // FlutterActivity.setFrameworkHandlesBack: the back callback is registered only while the framework has a
    // route to pop, so at the root the system's own back (and its back-to-home animation) applies. Below Android 13,
    // or without the manifest flag, the press arrives through OnBackPressed instead.
    private void SetFrameworkHandlesBack(bool handles)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            return;
        }

        if (handles && _backCallback is null)
        {
            _backCallback = OperatingSystem.IsAndroidVersionAtLeast(34)
                ? new BackAnimationCallback()
                : new BackInvokedCallback();
            OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(IOnBackInvokedDispatcher.PriorityDefault,
                (IOnBackInvokedCallback)_backCallback);
        }
        else if (!handles && _backCallback is IOnBackInvokedCallback callback)
        {
            OnBackInvokedDispatcher.UnregisterOnBackInvokedCallback(callback);
            _backCallback.Dispose();
            _backCallback = null;
        }
    }

    public void SurfaceCreated(ISurfaceHolder holder) => _host?.SurfaceCreated(holder.Surface!);

    public void SurfaceChanged(ISurfaceHolder holder, global::Android.Graphics.Format format, int width, int height) =>
        _host?.SurfaceChanged(width, height);

    public void SurfaceDestroyed(ISurfaceHolder holder) => _host?.SurfaceDestroyed();

    /// <summary>Android 13: a back press commits the framework's back gesture.</summary>
    [SupportedOSPlatform("android33.0")]
    private sealed class BackInvokedCallback : Java.Lang.Object, IOnBackInvokedCallback
    {
        public void OnBackInvoked() => EventLoop.RunAsync(() => WidgetsBinding.Instance.HandleCommitBackGesture());
    }

    /// <summary>Android 14+: the predictive back gesture's progress goes to the framework.</summary>
    [SupportedOSPlatform("android34.0")]
    private sealed class BackAnimationCallback : Java.Lang.Object, IOnBackAnimationCallback
    {
        public void OnBackStarted(BackEvent backEvent) =>
            _ = WidgetsBinding.Instance.HandleStartBackGesture(ToFrameworkEvent(backEvent));

        public void OnBackProgressed(BackEvent backEvent) =>
            WidgetsBinding.Instance.HandleUpdateBackGestureProgress(ToFrameworkEvent(backEvent));

        public void OnBackCancelled() => WidgetsBinding.Instance.HandleCancelBackGesture();

        public void OnBackInvoked() => EventLoop.RunAsync(() => WidgetsBinding.Instance.HandleCommitBackGesture());

        private static PredictiveBackEvent ToFrameworkEvent(BackEvent backEvent) => new(
            progress: backEvent.Progress,
            swipeEdge: backEvent.SwipeEdge == BackEventEdge.Right ? SwipeEdge.Right : SwipeEdge.Left,
            touchOffset: new Offset(backEvent.TouchX, backEvent.TouchY));
    }

    private sealed class InsetsListener(ImpellerHost host) : Java.Lang.Object, View.IOnApplyWindowInsetsListener
    {
        public WindowInsets OnApplyWindowInsets(View view, WindowInsets insets)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                global::Android.Graphics.Insets bars = insets.GetInsets(
                    WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout());
                global::Android.Graphics.Insets ime = insets.GetInsets(WindowInsets.Type.Ime());
                host.InsetsChanged(bars.Left, bars.Top, bars.Right, bars.Bottom, ime.Bottom);
            }

            return insets;
        }
    }
}
