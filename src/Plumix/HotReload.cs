using System.Diagnostics;
using System.Reflection.Metadata;
using Avalonia.Threading;

[assembly: MetadataUpdateHandler(typeof(Plumix.HotReloadManager))]

// Dart parity source (reference): flutter/packages/flutter/lib/src/foundation/binding.dart (reassembleApplication; delta delivery adapted to .NET MetadataUpdateHandler)

namespace Plumix;

/// Bridges .NET Hot Reload (dotnet watch, IDE Hot Reload) to the framework's
/// reassemble flow.
///
/// After the runtime applies metadata updates, every live [PlumixHost] is
/// reassembled on the UI thread: the widget tree is rebuilt and the render
/// tree is re-laid-out and repainted so the edited code takes effect, while
/// existing [Widgets.State] objects are preserved.
public static class HotReloadManager
{
    private static readonly List<WeakReference<PlumixHost>> _hosts = [];

    /// Whether the manual reassemble shortcut (Ctrl/Cmd+Shift+R in a host
    /// window) is active. Defaults to true when the process runs with hot
    /// reload enabled — the runtime allows metadata updates or a debugger is
    /// attached. Exists as a fallback for IDEs that apply hot reload deltas
    /// without invoking [MetadataUpdateHandler] callbacks (e.g. Rider,
    /// RIDER-124189).
    internal static bool IsManualReassembleAvailable { get; set; } =
        Debugger.IsAttached
        || string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_MODIFIABLE_ASSEMBLIES"),
            "debug",
            StringComparison.OrdinalIgnoreCase);

    internal static void RegisterHost(PlumixHost host)
    {
        lock (_hosts)
        {
            _hosts.Add(new WeakReference<PlumixHost>(host));
        }
    }

    internal static void ResetForTests()
    {
        lock (_hosts)
        {
            _hosts.Clear();
        }
    }

    /// Called by the runtime (on an arbitrary thread) after hot reload deltas
    /// have been applied.
    internal static void UpdateApplication(Type[]? updatedTypes)
    {
        Log($"received {updatedTypes?.Length.ToString() ?? "unknown number of"} updated type(s), scheduling reassemble");
        Dispatcher.UIThread.Post(ReassembleApplication);
    }

    /// Cause all live hosts to be reassembled, e.g. after a hot reload.
    /// Must be called on the UI thread.
    ///
    /// This is expensive and should not be called except during development.
    public static void ReassembleApplication()
    {
        List<PlumixHost> hosts = [];
        lock (_hosts)
        {
            _hosts.RemoveAll(reference =>
            {
                if (!reference.TryGetTarget(out var host))
                {
                    return true;
                }

                hosts.Add(host);
                return false;
            });
        }

        foreach (var host in hosts)
        {
            host.ReassembleApplication();
        }

        Log($"reassembled {hosts.Count} host(s)");
    }

    private static void Log(string message)
    {
        Console.WriteLine($"[Plumix] Hot reload: {message}");
    }
}
