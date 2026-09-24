using Avalonia;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/events.dart
// Dart declares these as top-level functions in `events.dart`; C# needs a containing type, so they
// live on `PointerEventUtils`. The event classes themselves (with the static
// `PointerEvent.transformPosition`/`transformDeltaViaPositions`/`removePerspectiveTransform`) are in
// `Plumix/UI/PointerEvents.cs`, and the `k*Button` constants are members of `PointerButtons`.

namespace Plumix.Gestures;

public static class PointerEventUtils
{
    /// <summary>
    /// The largest unsigned value a Dart VM small integer holds, <c>kMaxUnsignedSMI</c> from
    /// <c>foundation/_bitfield_io.dart</c>; <see cref="NthMouseButton"/> masks with it.
    /// </summary>
    private const long MaxUnsignedSmi = 0x3FFFFFFFFFFFFFFF;

    /// <summary>
    /// The bit of the <paramref name="number"/>th mouse button, counting the primary button as 1.
    /// Dart's <c>nthMouseButton</c>; <paramref name="number"/> is at most 62.
    /// </summary>
    public static PointerButtons NthMouseButton(int number)
    {
        return (PointerButtons)(((long)PointerButtons.PrimaryMouse << (number - 1)) & MaxUnsignedSmi);
    }

    /// <summary>
    /// The bit of the <paramref name="number"/>th stylus button, counting the button closest to the
    /// tip as 1. Dart's <c>nthStylusButton</c>; <paramref name="number"/> is at most 62.
    /// </summary>
    public static PointerButtons NthStylusButton(int number)
    {
        return (PointerButtons)(((long)PointerButtons.PrimaryStylus << (number - 1)) & MaxUnsignedSmi);
    }

    /// <summary>
    /// Returns the button of <paramref name="buttons"/> with the smallest integer: the lowest set
    /// bit, or <see cref="PointerButtons.None"/> when no bit is set. Dart's <c>smallestButton</c>.
    /// </summary>
    public static PointerButtons SmallestButton(PointerButtons buttons)
    {
        long value = (long)buttons;
        return (PointerButtons)(value & -value);
    }

    /// <summary>
    /// Returns whether <paramref name="buttons"/> contains one and only one button. Dart's
    /// <c>isSingleButton</c>.
    /// </summary>
    public static bool IsSingleButton(PointerButtons buttons)
    {
        return buttons != PointerButtons.None && SmallestButton(buttons) == buttons;
    }

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
