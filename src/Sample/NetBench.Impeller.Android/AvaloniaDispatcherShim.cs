using Plumix.Android;

// C#-only host infrastructure; no Dart parity source. NetBench's cubits post background engine callbacks with
// Avalonia's `Dispatcher.UIThread.Post` (Avalonia is its mobile host today). The prototype keeps NetBench's
// sources unchanged, so this stand-in forwards that one call to the Impeller host's UI thread; a real native
// host needs a public UI-thread API in Plumix instead (gap list in avalonia-free-core.md).

namespace Avalonia.Threading;

/// <summary>The slice of Avalonia's Dispatcher NetBench's mobile code uses.</summary>
internal sealed class Dispatcher
{
    private Dispatcher()
    {
    }

    public static Dispatcher UIThread { get; } = new();

    public void Post(Action action) => Plumix.Android.UiThread.Post(action);
}
