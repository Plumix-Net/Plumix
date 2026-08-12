using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/services/mouse_tracking.dart

namespace Plumix.UI;

/// Signature for listening to [PointerEnterEvent] events.
public delegate void PointerEnterEventListener(PointerEnterEvent @event);

/// Signature for listening to [PointerExitEvent] events.
public delegate void PointerExitEventListener(PointerExitEvent @event);

/// Signature for listening to [PointerHoverEvent] events.
public delegate void PointerHoverEventListener(PointerHoverEvent @event);

/// The annotation object used to annotate regions that are interested in mouse
/// movements.
///
/// To use an annotation, return this object as a hit-test result from an object
/// in the hit-test chain.
public interface IMouseTrackerAnnotation
{
    /// Triggered when a mouse pointer, with or without buttons pressed, has
    /// entered the region and [ValidForMouseTracker] is true.
    PointerEnterEventListener? OnEnter { get; }

    /// Triggered when a mouse pointer, with or without buttons pressed, has
    /// exited the region and [ValidForMouseTracker] is true.
    PointerExitEventListener? OnExit { get; }

    /// The mouse cursor for mouse pointers that are hovering over the region.
    ///
    /// When a mouse enters the region, its cursor will be changed to this cursor.
    /// When the mouse region is exited, the cursor will be set by the region found
    /// at the new location.
    ///
    /// Defaults to [MouseCursor.Defer], deferring the choice of cursor to the next
    /// region behind it in hit-test order.
    MouseCursor Cursor { get; }

    /// Whether this is included when [MouseTracker] collects the list of annotations.
    ///
    /// If [ValidForMouseTracker] is false, this object is excluded from the
    /// current annotation list even if it's included in the hit test, affecting
    /// mouse-related behavior such as enter events, exit events, and mouse cursors.
    bool ValidForMouseTracker { get; }
}
