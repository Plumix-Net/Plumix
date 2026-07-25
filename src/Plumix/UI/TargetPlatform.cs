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
    public static TargetPlatform TargetPlatform
    {
        get
        {
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
