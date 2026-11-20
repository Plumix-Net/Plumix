using Avalonia;
using Plumix.Foundation;

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
    Offset GlobalPosition { get; }

    /// <summary>
    /// The local position in the coordinate system of the event receiver at which the pointer
    /// interacts with the screen.
    /// </summary>
    Offset LocalPosition { get; }
}

/// <summary>Diagnostics helpers for Dart's <c>Offset</c>-backed gesture detail properties.</summary>
internal static class GestureDetailsDiagnostics
{
    internal static DiagnosticsProperty<Offset> OffsetProperty(string name, Offset value)
    {
        string description = FormattableString.Invariant($"Offset({value.Dx:0.0}, {value.Dy:0.0})");
        return new DiagnosticsProperty<Offset>(name, value, description: description);
    }
}
