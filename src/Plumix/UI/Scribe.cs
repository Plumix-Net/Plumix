using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/services/scribe.dart

namespace Plumix.UI;

/// <summary>Allows access to Android's stylus handwriting input feature, Scribe.</summary>
public static class Scribe
{
    private static MethodChannel Channel => SystemChannels.Scribe;

    /// <summary>Returns true if the feature is available on the current platform and enabled for
    /// the current user.</summary>
    /// <exception cref="FlutterError">The platform returned no result.</exception>
    public static async Task<bool> IsFeatureAvailable()
    {
        object? result = await Channel.InvokeMethod<object>("Scribe.isFeatureAvailable");
        if (result is not bool value)
        {
            throw new FlutterError("MethodChannel.invokeMethod unexpectedly returned null.");
        }

        return value;
    }

    /// <summary>Returns true if the input method supports stylus handwriting input and it is
    /// enabled.</summary>
    /// <exception cref="FlutterError">The platform returned no result.</exception>
    public static async Task<bool> IsStylusHandwritingAvailable()
    {
        object? result = await Channel.InvokeMethod<object>("Scribe.isStylusHandwritingAvailable");
        if (result is not bool value)
        {
            throw new FlutterError("MethodChannel.invokeMethod unexpectedly returned null.");
        }

        return value;
    }

    /// <summary>Tell Android to begin receiving stylus handwriting input.</summary>
    public static Task StartStylusHandwriting()
    {
        return Channel.InvokeMethod<object>("Scribe.startStylusHandwriting");
    }
}
