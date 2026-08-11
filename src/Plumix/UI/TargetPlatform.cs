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

    public static TargetPlatform TargetPlatform
    {
        get
        {
            if (DebugTargetPlatformOverride is TargetPlatform debugOverride)
            {
                return debugOverride;
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
}
