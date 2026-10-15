using System.Diagnostics;
using System.Reflection.Metadata;
using Avalonia.Threading;
using Plumix.Foundation;

[assembly: MetadataUpdateHandler(typeof(Plumix.HotReloadManager))]

// C#-only infrastructure: the .NET side of hot reload. Flutter's tool injects the new code and then calls
// the `ext.flutter.reassemble` service extension, which runs `BindingBase.reassembleApplication`; .NET
// applies the metadata deltas itself and calls this `MetadataUpdateHandler`, which runs the same
// `BindingBase.ReassembleApplication`.

namespace Plumix;

/// <summary>
/// Bridges .NET Hot Reload (dotnet watch, IDE Hot Reload) to the framework's reassemble flow.
/// </summary>
/// <remarks>
/// After the runtime applies metadata updates, <see cref="BindingBase.ReassembleApplication"/> runs on
/// the UI thread: events are locked, the widget tree is rebuilt with every <see cref="Widgets.State"/>
/// kept, every registered render view is reassembled, and a warm-up frame shows the result — exactly
/// the chain the flutter tool's <c>ext.flutter.reassemble</c> request starts.
/// </remarks>
public static class HotReloadManager
{
    /// <summary>
    /// Whether the manual reassemble shortcut (Ctrl/Cmd+Shift+R in a host window) is active.
    /// </summary>
    /// <remarks>
    /// Defaults to true when the process runs with hot reload enabled — the runtime allows metadata
    /// updates or a debugger is attached. Exists as a fallback for IDEs that apply hot reload deltas
    /// without invoking <see cref="MetadataUpdateHandlerAttribute"/> callbacks (e.g. Rider,
    /// RIDER-124189).
    /// </remarks>
    internal static bool IsManualReassembleAvailable { get; set; } =
        Debugger.IsAttached
        || string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_MODIFIABLE_ASSEMBLIES"),
            "debug",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Called by the runtime (on an arbitrary thread) after hot reload deltas have been applied.
    /// </summary>
    internal static void UpdateApplication(Type[]? updatedTypes)
    {
        Log($"received {updatedTypes?.Length.ToString() ?? "unknown number of"} updated type(s), "
            + "scheduling reassemble");
        Dispatcher.UIThread.Post(() => _ = ReassembleApplication());
    }

    /// <summary>
    /// Reassembles the application after a hot reload: <see cref="BindingBase.ReassembleApplication"/>.
    /// Must be called on the UI thread.
    /// </summary>
    /// <remarks>This is expensive and should not be called except during development.</remarks>
    public static async Task ReassembleApplication()
    {
        await BindingBase.ReassembleApplication();
        Log("reassembled the application");
    }

    private static void Log(string message)
    {
        Console.WriteLine($"[Plumix] Hot reload: {message}");
    }
}
