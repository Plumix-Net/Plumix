namespace Plumix.UI;

// Dart parity source (reference):
// flutter/bin/cache/pkg/sky_engine/lib/ui/platform_dispatcher.dart (view-focus subset, adapted)

/// <summary>
/// The framework's side of the platform: the engine-level requests the framework can make of a
/// host, independent of any particular view.
/// </summary>
/// <remarks>
/// dart:ui's <c>PlatformDispatcher</c>, limited to the view-focus request the widget layer makes.
/// Flutter forwards <see cref="RequestViewFocusChange"/> to the engine, which moves native focus to
/// the view's window; Plumix has no engine, so the request is published through
/// <see cref="ViewFocusChangeRequested"/> and the host that owns the named view answers it. The
/// reverse direction — the platform telling the framework that a view gained or lost focus — goes
/// through <c>WidgetsBinding.HandleViewFocusChanged</c>, like every other platform message.
/// </remarks>
public sealed class PlatformDispatcher
{
    private PlatformDispatcher()
    {
    }

    /// <summary>The ambient platform dispatcher.</summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.instance</c>.</remarks>
    public static PlatformDispatcher Instance { get; } = new();

    /// <summary>
    /// Raised by <see cref="RequestViewFocusChange"/>. A host that owns the view named by the event
    /// gives it native focus; a test harness can record the requests.
    /// </summary>
    public event Action<ViewFocusEvent>? ViewFocusChangeRequested;

    /// <summary>Requests that the platform move focus to, or away from, the view <paramref name="viewId"/>.</summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.requestViewFocusChange</c>.</remarks>
    public void RequestViewFocusChange(int viewId, ViewFocusState state, ViewFocusDirection direction)
    {
        ViewFocusChangeRequested?.Invoke(new ViewFocusEvent(viewId, state, direction));
    }
}
