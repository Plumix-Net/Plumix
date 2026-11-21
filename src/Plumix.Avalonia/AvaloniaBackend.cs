// C#-only host infrastructure; no Dart parity source. Installs the Avalonia implementations of the
// framework's internal backend seams (docs/product avalonia-free-core.md, stage 3) when the host
// assembly loads, as AvaloniaWindowingRegistration installs the windowing owner.

namespace Plumix;

/// <summary>Registers the Avalonia backend with the framework, once per process.</summary>
internal static class AvaloniaBackend
{
    private static int s_registered;

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void EnsureRegistered()
    {
        if (Interlocked.Exchange(ref s_registered, 1) != 0)
        {
            return;
        }

        PlatformThread.Current = AvaloniaPlatformThread.Instance;
    }
}
