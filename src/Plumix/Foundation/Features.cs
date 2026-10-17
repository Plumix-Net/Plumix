namespace Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/_features.dart

/// <summary>
/// The framework's experimental feature flags: Dart's <c>@internal</c> top-level variables of
/// <c>foundation/_features.dart</c>.
/// </summary>
/// <remarks>
/// Dart reads the comma-separated <c>FLUTTER_ENABLED_FEATURE_FLAGS</c> compile-time environment value
/// (<c>flutter config --enable-windowing</c> passes it). C# has no compile-time environment, so the
/// same name is read from the process environment once, at first use. The flags are mutable, as in
/// Dart, where tests assign them directly.
/// </remarks>
public static class Features
{
    /// <summary>
    /// Dart's <c>debugEnabledFeatureFlags</c>: the feature flags named in
    /// <c>FLUTTER_ENABLED_FEATURE_FLAGS</c>.
    /// </summary>
    public static IReadOnlySet<string> DebugEnabledFeatureFlags { get; } = new HashSet<string>(
        (Environment.GetEnvironmentVariable("FLUTTER_ENABLED_FEATURE_FLAGS") ?? string.Empty).Split(','));

    /// <summary>
    /// Dart's <c>isWindowingEnabled</c>: whether the experimental windowing API (<c>WindowingOwner</c>,
    /// the window controllers and widgets) may be used.
    /// </summary>
    public static bool IsWindowingEnabled { get; set; } = DebugEnabledFeatureFlags.Contains("windowing");

    /// <summary>
    /// Dart's <c>isAccessibilityEvaluationsEnabled</c>: whether the experimental accessibility
    /// evaluations may run.
    /// </summary>
    public static bool IsAccessibilityEvaluationsEnabled { get; set; } =
        DebugEnabledFeatureFlags.Contains("accessibility_evaluations");
}
