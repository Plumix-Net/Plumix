namespace Plumix.Impeller.Android;

// C#-only host infrastructure; no Dart parity source. A public way for app code to reach the framework's UI
// thread from a background thread (a network engine's callbacks), which Plumix's public API does not offer:
// Avalonia apps use Avalonia's Dispatcher, Flutter apps never need one (Dart isolates post messages).

/// <summary>Posts work to the thread the framework runs on.</summary>
public static class UiThread
{
    /// <summary>Runs <paramref name="action"/> on the UI thread, after the work already queued there.</summary>
    public static void Post(Action action) => PlatformThread.Current.Post(action, PlatformTaskPriority.Task);
}
