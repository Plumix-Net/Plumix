using Plumix.UI;
using UIKit;

namespace Plumix.Impeller.iOS;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/darwin/ios/framework/Source/FlutterViewController.mm (dispatchTouches,
// PointerDataChangeFromUITouchPhase, DeviceKindFromTouchType, flushOngoingTouches): each UITouch becomes engine pointer
// data in physical pixels (force as pressure, radii, stylus tilt and orientation, the pointer's buttons), an ended or
// cancelled touch is also removed, and the engine's PointerDataPacketConverter turns the packet into dart:ui's.

/// <summary>Sends the view's touches to the framework.</summary>
internal sealed class IosTouchProcessor
{
    private readonly int _viewId;
    private readonly PointerDataPacketConverter _converter;
    // The engine uses the UITouch pointer as the device id; the converter keys its state by an int.
    private readonly Dictionary<IntPtr, int> _devices = [];
    private int _nextDevice = 1;

    public IosTouchProcessor(int viewId)
    {
        _viewId = viewId;
        _converter = new PointerDataPacketConverter(id => id == _viewId);
    }

    public void DispatchTouches(IEnumerable<UITouch> touches, UIEvent? uiEvent, UIView view, PointerChange? overridden)
    {
        double scale = view.ContentScaleFactor;
        List<EnginePointerData> packet = [];
        foreach (UITouch touch in touches)
        {
            var location = touch.LocationInView(view);
            var data = new EnginePointerData
            {
                ViewId = _viewId,
                // UITouch timestamps are seconds since boot; the engine's are microseconds.
                TimeStamp = TimeSpan.FromMicroseconds(touch.Timestamp * 1_000_000),
                Change = overridden ?? ChangeOf(touch.Phase),
                Kind = KindOf(touch.Type),
                SignalKind = PointerSignalKind.None,
                Device = DeviceOf(touch),
                PhysicalX = location.X * scale,
                PhysicalY = location.Y * scale,
                Pressure = touch.Force,
                PressureMax = touch.MaximumPossibleForce,
                RadiusMajor = touch.MajorRadius,
                RadiusMin = touch.MajorRadius - touch.MajorRadiusTolerance,
                RadiusMax = touch.MajorRadius + touch.MajorRadiusTolerance,
                // altitudeAngle is 0 for a stylus parallel to the screen and π/2 perpendicular; tilt is the reverse.
                Tilt = (Math.PI / 2) - touch.AltitudeAngle,
                Orientation = touch.GetAzimuthAngle(null) - (Math.PI / 2),
                Buttons = ButtonsOf(uiEvent),
                Scale = 1.0,
            };
            packet.Add(data);
            if (touch.Phase is UITouchPhase.Ended or UITouchPhase.Cancelled || overridden == PointerChange.Cancel)
            {
                EnginePointerData remove = data;
                remove.Change = PointerChange.Remove;
                packet.Add(remove);
                _devices.Remove(touch.Handle);
            }
        }

        PointerDataPacket converted = _converter.Convert(packet);
        if (converted.Data.Count > 0)
        {
            PlatformDispatcher.Instance.OnPointerDataPacket?.Invoke(converted);
        }
    }

    private int DeviceOf(UITouch touch)
    {
        if (!_devices.TryGetValue(touch.Handle, out int device))
        {
            _devices[touch.Handle] = device = _nextDevice++;
        }

        return device;
    }

    private static PointerChange ChangeOf(UITouchPhase phase) => phase switch
    {
        UITouchPhase.Began => PointerChange.Down,
        UITouchPhase.Moved or UITouchPhase.Stationary => PointerChange.Move,
        UITouchPhase.Ended => PointerChange.Up,
        _ => PointerChange.Cancel,
    };

    private static PointerDeviceKind KindOf(UITouchType type) => type switch
    {
        UITouchType.Stylus => PointerDeviceKind.Stylus,
        UITouchType.IndirectPointer => PointerDeviceKind.Mouse,
        _ => PointerDeviceKind.Touch,
    };

    // The pointer's mouse buttons (iOS 13.4+); touches have none.
    private static long ButtonsOf(UIEvent? uiEvent)
    {
        if (uiEvent is null || !OperatingSystem.IsIOSVersionAtLeast(13, 4))
        {
            return 0;
        }

        UIEventButtonMask mask = uiEvent.ButtonMask;
        return ((mask & UIEventButtonMask.Primary) != 0 ? (long)PointerButtons.PrimaryMouse : 0)
               | ((mask & UIEventButtonMask.Secondary) != 0 ? (long)PointerButtons.SecondaryMouse : 0);
    }
}
