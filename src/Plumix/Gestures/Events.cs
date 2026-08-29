using Avalonia;
using Plumix.Rendering;
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
        Matrix4? transform,
        Point? transformedEndPosition = null)
    {
        if (transform is not { } matrix)
        {
            return untransformedDelta;
        }

        Point end = transformedEndPosition ?? MatrixUtils.TransformPoint(matrix, untransformedEndPosition);
        Point start = MatrixUtils.TransformPoint(matrix, untransformedEndPosition - untransformedDelta);
        return end - start;
    }

    /// <summary>
    /// A copy of <paramref name="transform"/> with the z row and column reset to <c>(0, 0, 1, 0)</c>,
    /// so it can be inverted for hit testing without the perspective divide flattening the plane.
    /// </summary>
    /// <remarks>Flutter's <c>PointerEvent.removePerspectiveTransform</c>.</remarks>
    public static Matrix4 RemovePerspectiveTransform(Matrix4 transform)
    {
        var vector = new Vector4(0.0, 0.0, 1.0, 0.0);
        Matrix4 result = transform.Clone();
        result.SetColumn(2, vector);
        result.SetRow(2, vector);
        return result;
    }

    /// <summary>The straight-line length of the offset, Dart's `Offset.distance`.</summary>
    public static double Distance(this Point offset)
    {
        return Math.Sqrt((offset.X * offset.X) + (offset.Y * offset.Y));
    }

    /// <summary>
    /// The square of <see cref="Distance"/>, Dart's `Offset.distanceSquared`. Cheaper than
    /// <see cref="Distance"/> when only comparisons against a squared threshold are needed.
    /// </summary>
    public static double DistanceSquared(this Point offset)
    {
        return (offset.X * offset.X) + (offset.Y * offset.Y);
    }
}
