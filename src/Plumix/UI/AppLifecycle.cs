namespace Plumix.UI;

// Dart parity source:
// flutter/bin/cache/pkg/sky_engine/lib/ui/platform_dispatcher.dart

public enum AppLifecycleState
{
    Detached,
    Resumed,
    Inactive,
    Hidden,
    Paused,
}

public enum AppExitResponse
{
    Exit,
    Cancel,
}
