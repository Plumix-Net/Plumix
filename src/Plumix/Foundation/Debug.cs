// Dart parity source: flutter/packages/flutter/lib/src/foundation/debug.dart

namespace Plumix.Foundation;

/// <summary>
/// The object-lifecycle half of Dart's <c>foundation/debug.dart</c>. The other members of that file
/// (<c>debugAssertAllFoundationVarsUnset</c>, <c>debugInstrumentAction</c>,
/// <c>debugFormatDouble</c>, ...) live with the code that reads them or are not needed yet.
/// </summary>
public static class FoundationDebug
{
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
