using Android.App;
using Android.Content.PM;
using Plumix.Android;
using Plumix.Widgets;

namespace PlumixTemplateApp.Android;

// The Activity handles configuration changes itself, as Flutter's does: the widget tree relayouts instead of the
// Activity being recreated.
[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Exported = true,
    Theme = "@android:style/Theme.DeviceDefault.NoActionBar",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
                           | ConfigChanges.SmallestScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden
                           | ConfigChanges.UiMode | ConfigChanges.Density)]
public sealed class MainActivity : PlumixActivity
{
    protected override Widget CreateApp() => new MyApp();
}
