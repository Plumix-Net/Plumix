using System.Runtime.Versioning;
using Android.OS;
using Android.Window;
using Avalonia;
using Avalonia.Android;
using Plumix.UI;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/android/io/flutter/embedding/android/FlutterActivity.java
// (Activity callbacks forwarded to FlutterActivityAndFragmentDelegate, OnBackAnimationCallback).

namespace Plumix.Android;

/// <summary>
/// The Activity of a Plumix application on Android: an <see cref="AvaloniaMainActivity"/> that hands the
/// framework its lifecycle, saved restoration data and (on Android 14+) predictive back gestures.
/// </summary>
/// <remarks>
/// Flutter's <c>FlutterActivity</c>. Derive the application's launcher Activity from it; overrides must
/// call the base implementation. An Activity that needs another base class drives an
/// <see cref="AndroidActivityDelegate"/> from the same callbacks instead. Predictive back also needs
/// <c>android:enableOnBackInvokedCallback="true"</c> on the manifest's <c>application</c> element.
/// </remarks>
public class PlumixMainActivity : AvaloniaMainActivity
{
    private const string FrameworkRestorationBundleKey = "framework";

    private AndroidActivityDelegate? _delegate;
    private Java.Lang.Object? _backCallback;

    /// <summary>The delegate the Activity callbacks are forwarded to.</summary>
    protected AndroidActivityDelegate Delegate => _delegate ??= new AndroidActivityDelegate();

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // FlutterActivity finishes itself for `SystemNavigator.pop`: it is a plain Activity, so
        // PlatformPlugin.popSystemNavigator never reaches an OnBackPressedDispatcher. Going through
        // Avalonia's dispatcher here would hand the press back to the framework.
        Delegate.PopSystemNavigator = Finish;
        if (OperatingSystem.IsAndroidVersionAtLeast(34))
        {
            Delegate.FrameworkHandlesBackChanged = SetFrameworkHandlesBack;
        }

        Delegate.OnCreate(savedInstanceState?.GetByteArray(FrameworkRestorationBundleKey));
        base.OnCreate(savedInstanceState);
    }

    protected override void OnSaveInstanceState(Bundle outState)
    {
        outState.PutByteArray(FrameworkRestorationBundleKey, Delegate.OnSaveInstanceState());
        base.OnSaveInstanceState(outState);
    }

    protected override void OnResume()
    {
        base.OnResume();
        Delegate.OnResume();
    }

    protected override void OnPause()
    {
        Delegate.OnPause();
        base.OnPause();
    }

    protected override void OnStop()
    {
        Delegate.OnStop();
        base.OnStop();
    }

    protected override void OnDestroy()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(34))
        {
            UnregisterBackCallback();
        }

        Delegate.OnDestroy();
        base.OnDestroy();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        Delegate.OnWindowFocusChanged(hasFocus);
    }

    // FlutterActivity.setFrameworkHandlesBack: the callback is registered only while the framework has
    // a route to pop, so the system's own back (and its back-to-home animation) applies at the root.
    // Below Android 14 the press reaches the framework through Avalonia's TopLevel.BackRequested.
    [SupportedOSPlatform("android34.0")]
    private void SetFrameworkHandlesBack(bool frameworkHandlesBack)
    {
        if (frameworkHandlesBack)
        {
            RegisterBackCallback();
        }
        else
        {
            UnregisterBackCallback();
        }
    }

    [SupportedOSPlatform("android34.0")]
    private void RegisterBackCallback()
    {
        if (_backCallback is not null)
        {
            return;
        }

        // FlutterActivity uses PRIORITY_DEFAULT, but AvaloniaActivity also registers an androidx
        // OnBackPressedDispatcher callback (in onStart, re-registered whenever it toggles), which would
        // take the press first and skip the predictive-back gesture events.
        var callback = new BackAnimationCallback(this);
        OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(IOnBackInvokedDispatcher.PriorityOverlay, callback);
        _backCallback = callback;
    }

    [SupportedOSPlatform("android34.0")]
    private void UnregisterBackCallback()
    {
        if (_backCallback is not BackAnimationCallback callback)
        {
            return;
        }

        OnBackInvokedDispatcher.UnregisterOnBackInvokedCallback(callback);
        callback.Dispose();
        _backCallback = null;
    }

    [SupportedOSPlatform("android34.0")]
    private sealed class BackAnimationCallback(PlumixMainActivity activity) : Java.Lang.Object, IOnBackAnimationCallback
    {
        public void OnBackStarted(BackEvent backEvent) => activity.Delegate.OnBackStarted(ToFrameworkEvent(backEvent));

        public void OnBackProgressed(BackEvent backEvent) =>
            activity.Delegate.OnBackProgressed(ToFrameworkEvent(backEvent));

        public void OnBackCancelled() => activity.Delegate.OnBackCancelled();

        public void OnBackInvoked() => activity.Delegate.OnBackInvoked();

        private static PredictiveBackEvent ToFrameworkEvent(BackEvent backEvent) => new(
            progress: backEvent.Progress,
            swipeEdge: backEvent.SwipeEdge == BackEventEdge.Right ? SwipeEdge.Right : SwipeEdge.Left,
            touchOffset: new Point(backEvent.TouchX, backEvent.TouchY));
    }
}
