namespace Plumix;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/platform.dart
public enum TargetPlatform
{
    Android,
    Fuchsia,
    IOS,
    Linux,
    MacOS,
    Windows,
}

public static class PlatformDefaults
{
    private static readonly AsyncLocal<TargetPlatform?> DebugOverride = new();

    public static TargetPlatform? DebugTargetPlatformOverride
    {
        get => DebugOverride.Value;
        set => DebugOverride.Value = value;
    }

    /// <summary>
    /// The process-wide platform used when <see cref="DebugTargetPlatformOverride"/> is unset;
    /// null (the default) falls through to host detection.
    /// </summary>
    /// <remarks>
    /// C#-only seam. Dart's `debugDefaultTargetPlatformOverride` is a single global that
    /// `TestWidgetsFlutterBinding` assigns once so widget tests see the mobile defaults on every
    /// host. <see cref="DebugTargetPlatformOverride"/> is <see cref="AsyncLocal{T}"/> so that a
    /// per-test override cannot leak across contexts, which means it cannot also carry a
    /// process-wide default — this property does that half.
    /// </remarks>
    public static TargetPlatform? DebugDefaultTargetPlatform { get; set; }

    public static TargetPlatform TargetPlatform
    {
        get
        {
            if (DebugTargetPlatformOverride is TargetPlatform debugOverride)
            {
                return debugOverride;
            }

            if (DebugDefaultTargetPlatform is TargetPlatform processDefault)
            {
                return processDefault;
            }

            if (OperatingSystem.IsIOS())
            {
                return TargetPlatform.IOS;
            }

            if (OperatingSystem.IsMacOS())
            {
                return TargetPlatform.MacOS;
            }

            if (OperatingSystem.IsAndroid())
            {
                return TargetPlatform.Android;
            }

            if (OperatingSystem.IsWindows())
            {
                return TargetPlatform.Windows;
            }

            if (OperatingSystem.IsLinux())
            {
                return TargetPlatform.Linux;
            }

            return TargetPlatform.Fuchsia;
        }
    }

    /// <summary>
    /// Flutter's `kIsWeb` (`foundation/constants.dart`): whether the application is running in a
    /// browser. <see cref="DebugIsWebOverride"/> is the test seam Dart gets from compiling a
    /// separate web build.
    /// </summary>
    public static bool IsWeb => DebugIsWebOverride ?? OperatingSystem.IsBrowser();

    /// <summary>Overrides <see cref="IsWeb"/> for tests; null restores the runtime answer.</summary>
    public static bool? DebugIsWebOverride
    {
        get => DebugWebOverride.Value;
        set => DebugWebOverride.Value = value;
    }

    private static readonly AsyncLocal<bool?> DebugWebOverride = new();
}
