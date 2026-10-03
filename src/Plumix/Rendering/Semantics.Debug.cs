// Dart parity source: flutter/packages/flutter/lib/src/semantics/debug.dart

namespace Plumix.Rendering;

/// <summary>The debug-only switches of Flutter's <c>semantics/debug.dart</c>.</summary>
/// <remarks>
/// Dart's library-level variables live on this static class because C# has no top-level fields, the
/// way <see cref="SchedulerDebug"/> hosts <c>scheduler/debug.dart</c>.
/// </remarks>
public static class SemanticsDebug
{
    /// <summary>
    /// Overrides the setting of <see cref="SemanticsBinding.DisableAnimations"/> for debugging and
    /// testing.
    /// </summary>
    /// <remarks>
    /// Dart's <c>debugSemanticsDisableAnimations</c>. This value is ignored in non-debug builds.
    /// </remarks>
    public static bool? DebugSemanticsDisableAnimations { get; set; }
}
