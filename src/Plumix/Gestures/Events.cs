using Avalonia;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/events.dart
// Dart declares these as top-level functions in `events.dart`; C# needs a containing type, so they
// live on `PointerEventUtils`. The event classes themselves are in `Plumix/UI/PointerEvents.cs`.

namespace Plumix.Gestures;

public static class PointerEventUtils
{
    /// <summary>
    /// Returns the distance a pointer of the given kind must travel before the framework is
    /// confident the gesture is not a tap.
    /// </summary>
    public static double ComputeHitSlop(PointerDeviceKind kind, DeviceGestureSettings? settings)
    {
        return kind == PointerDeviceKind.Mouse
            ? GestureConstants.PrecisePointerHitSlop
            : settings?.TouchSlop ?? GestureConstants.TouchSlop;
    }

    /// <summary>
    /// Returns the distance a pointer of the given kind must travel before the framework is
    /// confident the gesture is a pan.
    /// </summary>
    public static double ComputePanSlop(PointerDeviceKind kind, DeviceGestureSettings? settings)
    {
        return kind == PointerDeviceKind.Mouse
            ? GestureConstants.PrecisePointerPanSlop
            : settings?.PanSlop ?? GestureConstants.PanSlop;
    }

    /// <summary>
    /// Returns the distance a pointer of the given kind must travel before the framework is
    /// confident the gesture is a scale.
    /// </summary>
    public static double ComputeScaleSlop(PointerDeviceKind kind)
    {
        return kind == PointerDeviceKind.Mouse
            ? GestureConstants.PrecisePointerScaleSlop
            : GestureConstants.ScaleSlop;
    }

    /// <summary>
    /// Transforms a delta expressed in one coordinate space into another by transforming both ends
    /// of the delta and subtracting. A null transform leaves the delta untouched.
    /// </summary>
    public static Point TransformDeltaViaPositions(
        Point untransformedEndPosition,
        Point untransformedDelta,
        Matrix? transform,
        Point? transformedEndPosition = null)
    {
        if (transform is not { } matrix)
        {
            return untransformedDelta;
        }

        Point end = transformedEndPosition ?? matrix.Transform(untransformedEndPosition);
        Point start = matrix.Transform(untransformedEndPosition - untransformedDelta);
        return end - start;
    }

    /// <summary>The straight-line length of the offset, Dart's `Offset.distance`.</summary>
    public static double Distance(this Point offset)
    {
        return Math.Sqrt((offset.X * offset.X) + (offset.Y * offset.Y));
    }
}
