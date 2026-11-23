using Android.App;
using Android.Content.PM;
using Plumix.Android;
using Plumix.Widgets;

namespace Plumix.Sample.Android;

// C#-only host infrastructure; no Dart parity source. The Sample gallery on the native Android host.
// Launch extras: `--es app demo` opens the Impeller backend demo, `--es route /tabs` opens one gallery page (for
// walking the routes from a script), `--ez perf true` logs startup and frame timings.

[Activity(
    Label = "Plumix Sample",
    MainLauncher = true,
    Exported = true,
    Theme = "@android:style/Theme.DeviceDefault.NoActionBar",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
                           | ConfigChanges.SmallestScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden
                           | ConfigChanges.UiMode | ConfigChanges.Density)]
public sealed class MainActivity : PlumixActivity
{
    protected override Widget CreateApp() =>
        Intent?.GetStringExtra("app") == "demo" ? DemoApp.Create() : new CounterApp();

    protected override void OnAppScheduled()
    {
        if (Intent?.GetStringExtra("route") is { } route)
        {
            SchedulerBinding.Instance.AddPostFrameCallback(timeStamp =>
                _ = SampleNavigationObservers.PageRoutes.Navigator?.PushNamed(route));
        }
    }
}
