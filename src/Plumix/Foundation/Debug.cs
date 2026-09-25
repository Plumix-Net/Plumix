// Dart parity source: flutter/packages/flutter/lib/src/foundation/debug.dart

namespace Plumix.Foundation;

/// <summary>
/// The object-lifecycle and instrumentation parts of Dart's <c>foundation/debug.dart</c>. The other members
/// (<c>debugAssertAllFoundationVarsUnset</c>, <c>debugFormatDouble</c>, ...) live with the code that
/// reads them or are not needed yet.
/// </summary>
public static class FoundationDebug
{
    /// <summary>
    /// Whether <see cref="DebugInstrumentAction{T}"/> times the actions it runs and prints the
    /// elapsed time.
    /// </summary>
    /// <remarks>Dart's <c>debugInstrumentationEnabled</c>; only read in debug builds.</remarks>
    public static bool DebugInstrumentationEnabled { get; set; }

    /// <summary>
    /// Runs <paramref name="action"/>; while <see cref="DebugInstrumentationEnabled"/> is set, prints
    /// how long it took, labelled with <paramref name="description"/>.
    /// </summary>
    /// <remarks>Dart's <c>debugInstrumentAction</c>.</remarks>
    public static async Task<T> DebugInstrumentAction<T>(string description, Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        bool instrument = Constants.KDebugMode && DebugInstrumentationEnabled;
        if (!instrument)
        {
            return await action();
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            return await action();
        }
        finally
        {
            stopwatch.Stop();
            Print.DebugPrint($"Action \"{description}\" took {DartDuration(stopwatch.Elapsed)}");
        }
    }

    /// <summary>Dart's <c>Duration.toString</c>: <c>H:MM:SS.mmmmmm</c>.</summary>
    private static string DartDuration(TimeSpan elapsed)
    {
        long microseconds = elapsed.Ticks / 10;
        long hours = microseconds / 3_600_000_000;
        long minutes = microseconds / 60_000_000 % 60;
        long seconds = microseconds / 1_000_000 % 60;
        long fraction = microseconds % 1_000_000;
        return FormattableString.Invariant($"{hours}:{minutes:00}:{seconds:00}.{fraction:000000}");
    }

    /// <summary>
    /// Dispatches an <see cref="ObjectCreated"/> event for <paramref name="object"/> when memory
    /// allocation tracking is enabled. Returns <see langword="true"/> so it can sit in a debug check.
    /// </summary>
    /// <remarks>Dart's <c>debugMaybeDispatchCreated</c>.</remarks>
    public static bool DebugMaybeDispatchCreated(string flutterLibrary, string className, object @object)
    {
        if (FlutterMemoryAllocations.KFlutterMemoryAllocationsEnabled)
        {
            FlutterMemoryAllocations.Instance.DispatchObjectCreated(
                library: $"package:flutter/{flutterLibrary}.dart",
                className: className,
                @object: @object);
        }

        return true;
    }

    /// <summary>
    /// Dispatches an <see cref="ObjectDisposed"/> event for <paramref name="object"/> when memory
    /// allocation tracking is enabled.
    /// </summary>
    /// <remarks>Dart's <c>debugMaybeDispatchDisposed</c>.</remarks>
    public static bool DebugMaybeDispatchDisposed(object @object)
    {
        if (FlutterMemoryAllocations.KFlutterMemoryAllocationsEnabled)
        {
            FlutterMemoryAllocations.Instance.DispatchObjectDisposed(@object);
        }

        return true;
    }
}
