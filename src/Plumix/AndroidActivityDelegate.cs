using Plumix.UI;
using Plumix.Widgets;

// C#-only host infrastructure parity source: the lifecycle, restoration and back-callback forwarding of
// flutter/engine/src/flutter/shell/platform/android/io/flutter/embedding/android/
// FlutterActivityAndFragmentDelegate.java.

namespace Plumix;

/// <summary>
/// The framework side of an Android Activity: forwards its lifecycle, saved state and back callbacks to
/// the binding, as Flutter's <c>FlutterActivityAndFragmentDelegate</c> does for a <c>FlutterActivity</c>.
/// </summary>
/// <remarks>
/// <c>PlumixMainActivity</c> (the <c>Plumix.Android</c> package) owns one and calls it from every
/// Activity callback. An Activity that cannot derive from it creates one in its constructor and makes
/// the same calls. The class has no Android dependency, so the core package stays platform neutral.
/// <para>
/// Restoration data belongs to the process (Flutter's engine), not to an Activity: every delegate
/// shares one <c>flutter/restoration</c> handler, so a recreated Activity hands the framework the
/// bundle its predecessor saved without the framework's tree being rebuilt.
/// </para>
/// </remarks>
public sealed class AndroidActivityDelegate
{
    private static AndroidRestorationChannel? s_restorationChannel;

    private readonly AndroidRestorationChannel _restorationChannel;
    private readonly AndroidLifecycleChannel _lifecycleChannel;

    /// <summary>Creates a delegate over the process-wide binding and restoration channel.</summary>
    public AndroidActivityDelegate()
        : this(
            s_restorationChannel ??= new AndroidRestorationChannel(waitForRestorationData: true),
            WidgetsBinding.Instance.HandleAppLifecycleStateChanged)
    {
    }

    internal AndroidActivityDelegate(
        AndroidRestorationChannel restorationChannel,
        Action<AppLifecycleState> sendLifecycleState)
    {
        _restorationChannel = restorationChannel;
        _lifecycleChannel = new AndroidLifecycleChannel(sendLifecycleState);
    }

    /// <summary>
    /// Called with whether the framework handles back (<c>SystemNavigator.setFrameworkHandlesBack</c>):
    /// <c>FlutterActivity.setFrameworkHandlesBack</c>, which registers the Activity's back callback only
    /// while the framework has something to pop, so that the system handles back at the root itself.
    /// </summary>
    /// <remarks>
    /// <see cref="OnCreate"/> replays the last reported value to a recreated Activity.
    /// </remarks>
    public Action<bool>? FrameworkHandlesBackChanged { get; set; }

    /// <summary>
    /// The Activity's answer to <c>SystemNavigator.pop</c>, which the framework sends when a back press
    /// popped no route; <c>FlutterActivity</c> finishes itself (<c>PlatformPlugin.popSystemNavigator</c>).
    /// </summary>
    public Action? PopSystemNavigator { get; set; }

    /// <summary>
    /// <c>Activity.onCreate</c>: hands the framework the restoration data saved by
    /// <see cref="OnSaveInstanceState"/>, or null when the Activity starts fresh, and makes this Activity
    /// the one that answers the framework's back requests.
    /// </summary>
    /// <remarks>
    /// An absent bundle is also a response: it releases the framework's initial restoration request,
    /// which <c>WidgetsApp</c> waits for before its first restorable frame.
    /// </remarks>
    public void OnCreate(byte[]? savedRestorationData)
    {
        PlumixHost.FrameworkHandlesBackChanged -= HandleFrameworkHandlesBackChanged;
        PlumixHost.FrameworkHandlesBackChanged += HandleFrameworkHandlesBackChanged;
        PlumixHost.SystemNavigatorPopHandler = HandleSystemNavigatorPop;
        if (PlumixHost.ReportedFrameworkHandlesBack is { } frameworkHandlesBack)
        {
            FrameworkHandlesBackChanged?.Invoke(frameworkHandlesBack);
        }

        _restorationChannel.SetRestorationData(savedRestorationData);
    }

    /// <summary><c>Activity.onSaveInstanceState</c>: the restoration data to put in the saved bundle.</summary>
    public byte[]? OnSaveInstanceState() => _restorationChannel.RestorationData;

    /// <summary><c>Activity.onResume</c>.</summary>
    public void OnResume() => _lifecycleChannel.AppIsResumed();

    /// <summary><c>Activity.onPause</c>.</summary>
    public void OnPause() => _lifecycleChannel.AppIsInactive();

    /// <summary><c>Activity.onStop</c>.</summary>
    public void OnStop() => _lifecycleChannel.AppIsPaused();

    /// <summary><c>Activity.onDestroy</c>.</summary>
    public void OnDestroy()
    {
        PlumixHost.FrameworkHandlesBackChanged -= HandleFrameworkHandlesBackChanged;
        if (ReferenceEquals(PlumixHost.SystemNavigatorPopHandler?.Target, this))
        {
            PlumixHost.SystemNavigatorPopHandler = null;
        }

        _lifecycleChannel.AppIsDetached();
    }

    /// <summary><c>Activity.onWindowFocusChanged</c>.</summary>
    public void OnWindowFocusChanged(bool hasFocus)
    {
        if (hasFocus)
        {
            _lifecycleChannel.AWindowIsFocused();
        }
        else
        {
            _lifecycleChannel.NoWindowsAreFocused();
        }
    }

    /// <summary><c>OnBackAnimationCallback.onBackStarted</c> (Android 14+ predictive back).</summary>
    public void OnBackStarted(PredictiveBackEvent backEvent) =>
        _ = WidgetsBinding.Instance.HandleStartBackGesture(backEvent);

    /// <summary><c>OnBackAnimationCallback.onBackProgressed</c>.</summary>
    public void OnBackProgressed(PredictiveBackEvent backEvent) =>
        WidgetsBinding.Instance.HandleUpdateBackGestureProgress(backEvent);

    /// <summary><c>OnBackAnimationCallback.onBackCancelled</c>.</summary>
    public void OnBackCancelled() => WidgetsBinding.Instance.HandleCancelBackGesture();

    /// <summary>
    /// <c>OnBackInvokedCallback.onBackInvoked</c>: commits the back gesture in the framework. A back press
    /// that pops no route ends in <c>SystemNavigator.pop</c>, which <see cref="PopSystemNavigator"/>
    /// answers.
    /// </summary>
    public void OnBackInvoked() => EventLoop.RunAsync(() => WidgetsBinding.Instance.HandleCommitBackGesture());

    private void HandleFrameworkHandlesBackChanged(bool frameworkHandlesBack) =>
        FrameworkHandlesBackChanged?.Invoke(frameworkHandlesBack);

    private void HandleSystemNavigatorPop() => PopSystemNavigator?.Invoke();

    internal static void ResetForTests() => s_restorationChannel = null;
}
