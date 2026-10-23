using Android.App;
using Android.Content.PM;
using Android.OS;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/main.dart (platform host bootstrap, adapted)

namespace Plumix.Android;

[Activity(
    Label = "Plumix.Sample.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : PlumixMainActivity
{
    private AndroidSpellCheckService? _spellCheckService;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _spellCheckService = new AndroidSpellCheckService(this);
        DefaultSpellCheckService.PlatformHandler = _spellCheckService.Handler;
    }

    protected override void OnDestroy()
    {
        AndroidSpellCheckService? spellCheckService = _spellCheckService;
        if (spellCheckService is not null)
        {
            if (DefaultSpellCheckService.PlatformHandler == spellCheckService.Handler)
            {
                DefaultSpellCheckService.PlatformHandler = null;
            }

            spellCheckService.Dispose();
            _spellCheckService = null;
        }

        base.OnDestroy();
    }
}
