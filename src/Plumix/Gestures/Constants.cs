// Dart parity source: flutter/packages/flutter/lib/src/gestures/constants.dart

namespace Plumix.Gestures;

/// Modeled after Android's ViewConfiguration:
/// https://github.com/android/platform_frameworks_base/blob/master/core/java/android/view/ViewConfiguration.java
public static class GestureConstants
{
    /// The time that must elapse before a tap gesture sends onTapDown, if there's
    /// any doubt that the gesture is a tap.
    public static readonly TimeSpan PressTimeout = TimeSpan.FromMilliseconds(100);

    /// Maximum length of time between a tap down and a tap up for the gesture to be
    /// considered a tap. (Currently not honored by the TapGestureRecognizer.)
    public static readonly TimeSpan HoverTapTimeout = TimeSpan.FromMilliseconds(150);

    /// Maximum distance between the down and up pointers for a tap. (Currently not
    /// honored by the [TapGestureRecognizer]; [PrimaryPointerGestureRecognizer],
    /// which TapGestureRecognizer inherits from, uses [kTouchSlop].)
    public const double HoverTapSlop = 20.0;

    /// The time before a long press gesture attempts to win.
    public static readonly TimeSpan LongPressTimeout = TimeSpan.FromMilliseconds(500);

    /// The maximum time from the start of the first tap to the start of the second
    /// tap in a double-tap gesture.
    public static readonly TimeSpan DoubleTapTimeout = TimeSpan.FromMilliseconds(300);

    /// The minimum time from the end of the first tap to the start of the second
    /// tap in a double-tap gesture.
    public static readonly TimeSpan DoubleTapMinTime = TimeSpan.FromMilliseconds(40);

    /// The maximum distance that the first touch in a double-tap gesture can travel
    /// before deciding that it is not part of a double-tap gesture.
    public const double DoubleTapTouchSlop = TouchSlop;

    /// Distance between the initial position of the first touch and the start
    /// position of a potential second touch for the second touch to be considered
    /// the second touch of a double-tap gesture.
    public const double DoubleTapSlop = 100.0;

    /// The time for which zoom controls (e.g. in a map interface) are to be
    /// displayed on the screen, from the moment they were last requested.
    public static readonly TimeSpan ZoomControlsTimeout = TimeSpan.FromMilliseconds(3000);

    /// The distance a touch has to travel for the framework to be confident that
    /// the gesture is a scroll gesture, or, inversely, the maximum distance that a
    /// touch can travel before the framework becomes confident that it is not a tap.
    public const double TouchSlop = 18.0;

    /// The distance a touch has to travel for the framework to be confident that
    /// the gesture is a paging gesture.
    public const double PagingTouchSlop = TouchSlop * 2.0;

    /// The distance a touch has to travel for the framework to be confident that
    /// the gesture is a panning gesture.
    public const double PanSlop = TouchSlop * 2.0;

    /// The distance a touch has to travel for the framework to be confident that
    /// the gesture is a scale gesture.
    public const double ScaleSlop = TouchSlop;

    /// The margin around a dialog, popup menu, or other window-like widget inside
    /// which we do not consider a tap to dismiss the widget.
    public const double WindowTouchSlop = 16.0;

    /// The minimum velocity for a touch to consider that touch to trigger a fling
    /// gesture.
    public const double MinFlingVelocity = 50.0;

    /// Drag gesture fling velocities are clipped to this value.
    public const double MaxFlingVelocity = 8000.0;

    /// The maximum time from the start of the first tap to the start of the second
    /// tap in a jump-tap gesture.
    public static readonly TimeSpan JumpTapTimeout = TimeSpan.FromMilliseconds(500);

    /// Like [TouchSlop], but for more precise pointers like mice and trackpads.
    public const double PrecisePointerHitSlop = 1.0;

    /// Like [PanSlop], but for more precise pointers like mice and trackpads.
    public const double PrecisePointerPanSlop = PrecisePointerHitSlop * 2.0;

    /// Like [ScaleSlop], but for more precise pointers like mice and trackpads.
    public const double PrecisePointerScaleSlop = PrecisePointerHitSlop;
}
