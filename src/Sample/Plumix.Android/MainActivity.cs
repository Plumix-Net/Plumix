using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Window;
using Avalonia;
using Avalonia.Android;
using Plumix.UI;
using Plumix.Widgets;
using System;
using System.Runtime.Versioning;

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
    private PredictiveBackCallback? _predictiveBackCallback;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        if (!OperatingSystem.IsAndroidVersionAtLeast(34))
        {
            return;
        }

        _predictiveBackCallback = new PredictiveBackCallback(this);
        OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(
            IOnBackInvokedDispatcher.PriorityDefault,
            _predictiveBackCallback);
    }

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
        PredictiveBackCallback? callback = _predictiveBackCallback;
        if (callback is not null && OperatingSystem.IsAndroidVersionAtLeast(34))
        {
            OnBackInvokedDispatcher.UnregisterOnBackInvokedCallback(callback);
            callback.Dispose();
            _predictiveBackCallback = null;
        }

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

    [SupportedOSPlatform("android34.0")]
    private sealed class PredictiveBackCallback : Java.Lang.Object, IOnBackAnimationCallback
    {
        private readonly MainActivity _activity;

        public PredictiveBackCallback(MainActivity activity)
        {
            _activity = activity;
        }

        public void OnBackStarted(BackEvent backEvent)
        {
            _ = WidgetsBinding.Instance.HandleStartBackGesture(ToFrameworkEvent(backEvent));
        }

        public void OnBackProgressed(BackEvent backEvent)
        {
            WidgetsBinding.Instance.HandleUpdateBackGestureProgress(ToFrameworkEvent(backEvent));
        }

        public void OnBackCancelled()
        {
            WidgetsBinding.Instance.HandleCancelBackGesture();
        }

        public void OnBackInvoked()
        {
            if (!WidgetsBinding.Instance.HandleCommitBackGesture())
            {
                _activity.Finish();
            }
        }

        private static PredictiveBackEvent ToFrameworkEvent(BackEvent backEvent)
        {
            return new PredictiveBackEvent(
                progress: backEvent.Progress,
                swipeEdge: backEvent.SwipeEdge == BackEventEdge.Right
                    ? SwipeEdge.Right
                    : SwipeEdge.Left,
                touchOffset: new Point(backEvent.TouchX, backEvent.TouchY));
        }
    }
}
