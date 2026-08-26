using Avalonia;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/gesture_details.dart

namespace Plumix.Gestures;

/// <summary>
/// The contract for details objects that carry a pointer position. Dart's
/// `abstract interface class PositionedGestureDetails` becomes a C# interface; the fields it
/// declares live on the implementing details types.
/// </summary>
public interface IPositionedGestureDetails
{
    /// <summary>The global position at which the pointer interacts with the screen.</summary>
    Point GlobalPosition { get; }

    /// <summary>
    /// The local position in the coordinate system of the event receiver at which the pointer
    /// interacts with the screen.
    /// </summary>
    Point LocalPosition { get; }
}
