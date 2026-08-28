using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/debug.dart

namespace Plumix.Gestures;

/// <summary>Debug flags for the gesture subsystem. Ports Dart's `gestures/debug.dart`.</summary>
public static class GestureDebug
{
    /// <summary>
    /// Prints information about gesture arena resolution: which members are added, which win and
    /// which lose. Dart's `debugPrintGestureArenaDiagnostics`.
    /// </summary>
    public static bool PrintGestureArenaDiagnostics { get; set; }

    /// <summary>
    /// Logs a message every time a gesture recognizer invokes one of its callbacks. Dart's
    /// `debugPrintRecognizerCallbacksTrace`.
    /// </summary>
    public static bool PrintRecognizerCallbacksTrace { get; set; }

    /// <summary>
    /// Prints the resampling margin between the last event time and the last sample time. Dart's
    /// `debugPrintResamplingMargin`.
    /// </summary>
    public static bool PrintResamplingMargin { get; set; }

    /// <summary>
    /// Returns true if none of the gesture library debug variables have been changed. Dart's
    /// `debugAssertAllGesturesVarsUnset`.
    /// </summary>
    public static bool AssertAllGesturesVarsUnset(string reason)
    {
        if (PrintGestureArenaDiagnostics || PrintRecognizerCallbacksTrace || PrintResamplingMargin)
        {
            throw new FlutterError(reason);
        }

        return true;
    }

    internal static void Log(string message) => Print.DebugPrint(message, null);
}
