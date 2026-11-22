using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;
using NetBench.Features.Shell.Presentation.Mobile;
using NetBench.Mobile.Platform;
using Plumix.Impeller.Android;
using Widget = Plumix.Widgets.Widget;

namespace NetBench.Impeller.Android;

// C#-only host infrastructure; no Dart parity source. NetBench's mobile app on the Impeller host: the root
// widget NetBench's Avalonia bootstrap (Mobile/MobileApp.axaml.cs) creates, and the platform services its
// Android head provides. Launch extras: `--ez perf true` logs startup and frame timings.

[Activity(
    Label = "NetBench Impeller",
    MainLauncher = true,
    Exported = true,
    Theme = "@android:style/Theme.DeviceDefault.NoActionBar",
    ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
                           | ConfigChanges.SmallestScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden
                           | ConfigChanges.UiMode | ConfigChanges.Density)]
public sealed class MainActivity : ImpellerActivity
{
    protected override Widget CreateApp()
    {
        // Before the first build: MobileShell reads the platform services when it is created.
        PlatformServices.Current = new ImpellerPlatformServices(this);
        return new MobileShell();
    }

    /// <summary>NetBench's Android services; sharing files needs a FileProvider the prototype lacks.</summary>
    private sealed class ImpellerPlatformServices(Activity activity) : IPlatformServices
    {
        public Task ShareTextAsync(string text, string title)
        {
            var intent = new Intent(Intent.ActionSend);
            intent.SetType("text/plain");
            intent.PutExtra(Intent.ExtraText, text);
            activity.StartActivity(Intent.CreateChooser(intent, title));
            return Task.CompletedTask;
        }

        public Task ShareFileAsync(string path, string mimeType, string title) =>
            Task.FromException(new PlatformNotSupportedException("The prototype has no FileProvider."));

        public void SetKeepScreenOn(bool keepOn) => activity.RunOnUiThread(() =>
        {
            if (keepOn)
            {
                activity.Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
            }
            else
            {
                activity.Window?.ClearFlags(WindowManagerFlags.KeepScreenOn);
            }
        });

        public Task<bool> OpenUrlAsync(Uri uri)
        {
            try
            {
                activity.StartActivity(new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(uri.AbsoluteUri)));
                return Task.FromResult(true);
            }
            catch (ActivityNotFoundException)
            {
                return Task.FromResult(false);
            }
        }
    }
}
