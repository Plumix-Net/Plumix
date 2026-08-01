using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/main.dart (platform host bootstrap, adapted)

namespace Plumix.Android;

[Activity(
    Label = "Plumix.Sample.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    private readonly AndroidLifecycleChannel _lifecycleChannel = new(
        WidgetsBinding.Instance.HandleAppLifecycleStateChanged);

    protected override void OnResume()
    {
        base.OnResume();
        _lifecycleChannel.AppIsResumed();
    }

    protected override void OnPause()
    {
        _lifecycleChannel.AppIsInactive();
        base.OnPause();
    }

    protected override void OnStop()
    {
        _lifecycleChannel.AppIsPaused();
        base.OnStop();
    }

    protected override void OnDestroy()
    {
        _lifecycleChannel.AppIsDetached();
        base.OnDestroy();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus)
        {
            _lifecycleChannel.AWindowIsFocused();
        }
        else
        {
            _lifecycleChannel.NoWindowsAreFocused();
        }
    }

    // protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    // {
    //     return base.CustomizeAppBuilder(builder)
    //         .WithInterFont();
    // }
}
