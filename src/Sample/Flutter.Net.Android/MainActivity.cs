using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

// Dart parity source (reference): dart_sample/lib/main.dart (platform host bootstrap, adapted)

namespace Flutter.Net.Android;

[Activity(
    Label = "Flutter.Net.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    // protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    // {
    //     return base.CustomizeAppBuilder(builder)
    //         .WithInterFont();
    // }
}
