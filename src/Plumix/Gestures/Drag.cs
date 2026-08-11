// Dart parity source: flutter/packages/flutter/lib/src/gestures/drag.dart

namespace Plumix.Gestures;

/// <summary>
/// Interface for objects that receive updates about drags.
/// </summary>
/// <remarks>
/// A drag object is returned by the object that owns the drag (for example a scroll position) when
/// a recognizer starts a drag, and it receives the recognizer's updates until the drag ends.
/// </remarks>
public interface IDrag
{
    /// <summary>The pointer has moved.</summary>
    void Update(DragUpdateDetails details);

    /// <summary>The pointer is no longer in contact with the screen.</summary>
    void End(DragEndDetails details);

    /// <summary>The input from the pointer is no longer directed towards this receiver.</summary>
    void Cancel();
}

/// <summary>
/// Interface for objects that hold a scrollable in place, stopping any ballistic motion without
/// yet starting a drag.
/// </summary>
public interface IScrollHoldController
{
    /// <summary>Release the hold, letting the scrollable resume ballistic motion.</summary>
    void Cancel();
}
