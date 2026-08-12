using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/hit_test.dart

namespace Plumix.Rendering;

/// An object that can handle events.
public interface IHitTestTarget
{
    /// Override this method to receive events.
    void HandleEvent(PointerEvent @event, HitTestEntry entry);
}
