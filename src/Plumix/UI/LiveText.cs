// Dart parity source: flutter/packages/flutter/lib/src/services/live_text.dart

namespace Plumix.UI;

/// <summary>Utility methods for interacting with the system's Live Text (iOS's scan-text input).
/// </summary>
public static class LiveText
{
    /// <summary>Returns true if Live Text input is available on the device; false when the
    /// platform answers nothing.</summary>
    public static async Task<bool> IsLiveTextInputAvailable()
    {
        object? result = await SystemChannels.Platform.InvokeMethod<object>("LiveText.isLiveTextInputAvailable");
        return result as bool? ?? false;
    }

    /// <summary>Starts Live Text input. If any other input method is shown, it is switched to Live
    /// Text input.</summary>
    public static async Task StartLiveTextInput()
    {
        await SystemChannels.TextInput.InvokeMethod<object>("TextInput.startLiveTextInput");
    }
}
