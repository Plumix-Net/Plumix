namespace Plumix.UI;

// Dart parity source:
// flutter/bin/cache/pkg/sky_engine/lib/ui/platform_dispatcher.dart

/// <summary>Whether a view gained or lost focus.</summary>
/// <remarks>dart:ui's <c>ViewFocusState</c>.</remarks>
public enum ViewFocusState
{
    /// <summary>The view does not have focus.</summary>
    Unfocused,

    /// <summary>The view has focus.</summary>
    Focused,
}

/// <summary>The direction focus travelled when a view gained focus.</summary>
/// <remarks>dart:ui's <c>ViewFocusDirection</c>.</remarks>
public enum ViewFocusDirection
{
    /// <summary>The direction is unknown or does not apply, e.g. because the view lost focus.</summary>
    Undefined,

    /// <summary>Focus entered the view moving forward, e.g. by pressing Tab.</summary>
    Forward,

    /// <summary>Focus entered the view moving backward, e.g. by pressing Shift+Tab.</summary>
    Backward,
}

/// <summary>An event sent when a view gains or loses focus.</summary>
/// <remarks>dart:ui's <c>ViewFocusEvent</c>.</remarks>
public sealed class ViewFocusEvent
{
    /// <summary>Creates a view focus event.</summary>
    public ViewFocusEvent(int viewId, ViewFocusState state, ViewFocusDirection direction)
    {
        ViewId = viewId;
        State = state;
        Direction = direction;
    }

    /// <summary>The id of the view whose focus changed.</summary>
    public int ViewId { get; }

    /// <summary>Whether the view gained or lost focus.</summary>
    public ViewFocusState State { get; }

    /// <summary>The direction focus travelled.</summary>
    public ViewFocusDirection Direction { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"ViewFocusEvent(viewId: {ViewId}, state: {State}, direction: {Direction})";
    }
}
