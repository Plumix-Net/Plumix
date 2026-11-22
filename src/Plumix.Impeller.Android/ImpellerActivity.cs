using Android.App;
using Android.OS;
using Android.Views;
using Widget = Plumix.Widgets.Widget;

namespace Plumix.Impeller.Android;

// C#-only host infrastructure; no Dart parity source. The prototype's Activity, the role FlutterActivity plays
// in the engine's Android embedding: a full-screen SurfaceView the ImpellerHost renders into, with lifecycle,
// insets, touches and back forwarded to it. An app derives from it, adds [Activity] and returns its root
// widget. Launch extras: `--ez perf true` logs startup and frame timings (PerfProbe).

/// <summary>Runs a Plumix app in an Android Activity, rendered by Impeller.</summary>
public abstract class ImpellerActivity : Activity, ISurfaceHolderCallback
{
    private ImpellerHost? _host;
    private AndroidLifecycleChannel? _lifecycle;
    private AndroidAccessibilityFeatures? _accessibilityFeatures;

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

        var surfaceView = new PlumixSurfaceView(this);
        surfaceView.AccessibilityProvider = new PlumixAccessibilityNodeProvider(surfaceView, _host.Accessibility);
        surfaceView.Holder!.AddCallback(this);
        surfaceView.Touch += (_, e) => e.Handled = _host.Touch(e.Event!);
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
        _host?.Dispose();
        _host = null;
        base.OnDestroy();
    }

    public void SurfaceCreated(ISurfaceHolder holder) => _host?.SurfaceCreated(holder.Surface!);

    public void SurfaceChanged(ISurfaceHolder holder, global::Android.Graphics.Format format, int width, int height) =>
        _host?.SurfaceChanged(width, height);

    public void SurfaceDestroyed(ISurfaceHolder holder) => _host?.SurfaceDestroyed();

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
