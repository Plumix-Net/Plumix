using Plumix.UI;

namespace Plumix;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/android/io/flutter/embedding/engine/systemchannels/LifecycleChannel.java

internal enum AndroidAppLifecycleState
{
    Resumed,
    Inactive,
    Paused,
    Detached,
}

internal sealed class AndroidLifecycleChannel
{
    private readonly Action<AppLifecycleState> _sendState;
    private AndroidAppLifecycleState? _lastAndroidState;
    private AppLifecycleState? _lastFrameworkState;
    private bool _lastFocus = true;

    public AndroidLifecycleChannel(Action<AppLifecycleState> sendState)
    {
        ArgumentNullException.ThrowIfNull(sendState);
        _sendState = sendState;
    }

    public void AppIsResumed() => SendState(AndroidAppLifecycleState.Resumed, _lastFocus);

    public void AppIsInactive() => SendState(AndroidAppLifecycleState.Inactive, _lastFocus);

    public void AppIsPaused() => SendState(AndroidAppLifecycleState.Paused, _lastFocus);

    public void AppIsDetached() => SendState(AndroidAppLifecycleState.Detached, _lastFocus);

    public void AWindowIsFocused() => SendState(_lastAndroidState, hasFocus: true);

    public void NoWindowsAreFocused() => SendState(_lastAndroidState, hasFocus: false);

    private void SendState(AndroidAppLifecycleState? state, bool hasFocus)
    {
        if (_lastAndroidState == state && _lastFocus == hasFocus)
        {
            return;
        }

        if (state is null && _lastAndroidState is null)
        {
            _lastFocus = hasFocus;
            return;
        }

        AppLifecycleState frameworkState = state switch
        {
            AndroidAppLifecycleState.Resumed => hasFocus
                ? AppLifecycleState.Resumed
                : AppLifecycleState.Inactive,
            AndroidAppLifecycleState.Inactive => AppLifecycleState.Inactive,
            AndroidAppLifecycleState.Paused => AppLifecycleState.Paused,
            AndroidAppLifecycleState.Detached => AppLifecycleState.Detached,
            _ => throw new InvalidOperationException("Android lifecycle state must be initialized."),
        };

        _lastAndroidState = state;
        _lastFocus = hasFocus;
        if (_lastFrameworkState == frameworkState)
        {
            return;
        }

        _lastFrameworkState = frameworkState;
        _sendState(frameworkState);
    }
}
