using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace Plumix.Sample.Impeller.Android;

// C#-only host infrastructure; no Dart parity source. The prototype's only Activity: a full-screen
// SurfaceView the ImpellerHost renders into, with lifecycle, insets, touches and back forwarded to it.

[Activity(
    Label = "Plumix Impeller",
    MainLauncher = true,
    Exported = true,
    Theme = "@android:style/Theme.DeviceDefault.NoActionBar",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
                           | ConfigChanges.SmallestScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden
                           | ConfigChanges.UiMode | ConfigChanges.Density)]
public sealed class MainActivity : Activity, ISurfaceHolderCallback
{
    private ImpellerHost? _host;
    private AndroidLifecycleChannel? _lifecycle;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        ImpellerHost.InstallBackend(Assets!);
        _host = new ImpellerHost();
        _lifecycle = new AndroidLifecycleChannel(_host.LifecycleChanged);

        var surfaceView = new SurfaceView(this);
        surfaceView.Holder!.AddCallback(this);
        surfaceView.Touch += (_, e) => e.Handled = _host.Touch(e.Event!);
        surfaceView.SetOnApplyWindowInsetsListener(new InsetsListener(_host));
        SetContentView(surfaceView);
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            Window!.SetDecorFitsSystemWindows(false);
        }

        _host.Run(DemoApp.Create(), Resources!.DisplayMetrics!.Density);
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
                host.InsetsChanged(bars.Left, bars.Top, bars.Right, bars.Bottom);
            }

            return insets;
        }
    }
}
