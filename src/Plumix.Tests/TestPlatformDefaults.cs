using System.Runtime.CompilerServices;
using Plumix.UI;

namespace Plumix.Tests;

// C#-only test infrastructure; no Dart parity source.

/// <summary>
/// Pins the ambient <see cref="PlatformDefaults.TargetPlatform"/> to
/// <see cref="TargetPlatform.Android"/> for the whole test assembly, mirroring Flutter's
/// `TestWidgetsFlutterBinding`, which sets `debugDefaultTargetPlatformOverride` to
/// `TargetPlatform.android` so widget tests see the mobile defaults regardless of the host OS.
/// </summary>
/// <remarks>
/// Without this, `ThemeData`'s platform-derived defaults (`visualDensity`,
/// `materialTapTargetSize`) would resolve to the desktop values on a macOS or Windows dev
/// machine and to the mobile values in a Linux container, so the same assertions would disagree
/// across hosts. Tests that need a specific platform still set
/// <see cref="PlatformDefaults.DebugTargetPlatformOverride"/> themselves and restore the previous
/// value; when they restore null, resolution falls back to this default.
/// </remarks>
internal static class TestPlatformDefaults
{
    [ModuleInitializer]
    internal static void PinTargetPlatform()
    {
        PlatformDefaults.DebugDefaultTargetPlatform = TargetPlatform.Android;
    }
}
