// Dart parity source: flutter/packages/flutter/lib/src/gestures/gesture_settings.dart

namespace Plumix.Gestures;

/// <summary>Per-device gesture tuning that overrides the framework touch-slop default.</summary>
public readonly record struct DeviceGestureSettings(double? TouchSlop = null);
