using Android.Content;
using Android.OS;
using Android.Views;
using Plumix.UI;

namespace Plumix.Impeller.Android;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/android/io/flutter/embedding/android/AndroidTouchProcessor.java: each
// MotionEvent becomes engine pointer data (device kind from the tool type, mouse and stylus buttons, pressure,
// stylus distance and tilt, size and radii, orientation, mouse wheel scrolls, a buttonless mouse drag as a
// trackpad pan) that the engine's PointerDataPacketConverter turns into dart:ui packets. Not ported: the
// MotionEventTracker ids, which only platform views use.

/// <summary>Sends the view's touch, mouse and stylus input to the framework.</summary>
internal sealed class AndroidTouchProcessor
{
    // Bits of the tool type folded into the device id, so a finger and a stylus with one pointer id differ.
    private const int ToolTypeBits = 3;
    private const int ToolTypeMask = (1 << ToolTypeBits) - 1;
    private const int DefaultVerticalScrollFactor = 48;
    private const int DefaultHorizontalScrollFactor = 48;
    private const int PointerDataFlagBatched = 1;
    private const int PointerDataFlagMultiple = 2;
    private const int PointerDataMultiplePointerCountShift = 8;

    private readonly int _viewId;
    private readonly PointerDataPacketConverter _converter;
    private readonly Dictionary<int, (float X, float Y)> _ongoingPans = [];
    private int _cachedVerticalScrollFactor;

    public AndroidTouchProcessor(int viewId)
    {
        _viewId = viewId;
        _converter = new PointerDataPacketConverter(id => id == _viewId);
    }

    /// <summary><c>onTouchEvent</c>: the down or up pointer alone, or every pointer of a move.</summary>
    public bool OnTouchEvent(MotionEvent motionEvent)
    {
        MotionEventActions maskedAction = motionEvent.ActionMasked;
        PointerChange? pointerChange = PointerChangeForAction(maskedAction);
        bool updateForSinglePointer = maskedAction is MotionEventActions.Down or MotionEventActions.PointerDown;
        bool updateForMultiplePointers = !updateForSinglePointer
                                         && maskedAction is MotionEventActions.Up or MotionEventActions.PointerUp;
        PointerDeviceKind deviceType = DeviceKindForToolType(motionEvent.GetToolType(motionEvent.ActionIndex));
        bool shouldRemovePointer = updateForMultiplePointers && deviceType == PointerDeviceKind.Touch;
        int originalPointerCount = motionEvent.PointerCount;
        List<EnginePointerData> packet = [];
        if (updateForSinglePointer)
        {
            AddPointerForIndex(motionEvent, motionEvent.ActionIndex, pointerChange, 0, packet);
        }
        else if (updateForMultiplePointers)
        {
            // The other fingers move to their positions in the same batch, so a lift does not lose their motion.
            for (int p = 0; p < originalPointerCount; p++)
            {
                if (p != motionEvent.ActionIndex && motionEvent.GetToolType(p) == MotionEventToolType.Finger)
                {
                    AddPointerForIndex(motionEvent, p, PointerChange.Move, PointerDataFlagBatched, packet);
                }
            }

            AddPointerForIndex(motionEvent, motionEvent.ActionIndex, pointerChange, 0, packet);
            if (shouldRemovePointer)
            {
                AddPointerForIndex(motionEvent, motionEvent.ActionIndex, PointerChange.Remove, 0, packet);
            }
        }
        else
        {
            int pointerData = PointerDataFlagMultiple | (originalPointerCount << PointerDataMultiplePointerCountShift);
            for (int p = 0; p < originalPointerCount; p++)
            {
                AddPointerForIndex(motionEvent, p, pointerChange, pointerData, packet);
            }
        }

        Dispatch(packet);
        return true;
    }

    /// <summary><c>onGenericMotionEvent</c>: hover moves and wheel scrolls of pointing devices.</summary>
    public bool OnGenericMotionEvent(MotionEvent motionEvent, Context context)
    {
        bool isPointerEvent = motionEvent.IsFromSource(InputSourceType.ClassPointer);
        bool isMovementEvent = motionEvent.ActionMasked is MotionEventActions.HoverMove or MotionEventActions.Scroll;
        if (!isPointerEvent || !isMovementEvent)
        {
            return false;
        }

        List<EnginePointerData> packet = [];
        AddPointerForIndex(
            motionEvent, motionEvent.ActionIndex, PointerChangeForAction(motionEvent.ActionMasked), 0, packet, context);
        Dispatch(packet);
        return true;
    }

    private void Dispatch(List<EnginePointerData> packet)
    {
        PointerDataPacket converted = _converter.Convert(packet);
        if (converted.Data.Count > 0)
        {
            PlatformDispatcher.Instance.OnPointerDataPacket?.Invoke(converted);
        }
    }

    private static int UniquePointerIdByType(MotionEvent motionEvent, int pointerIndex) =>
        (motionEvent.GetPointerId(pointerIndex) << ToolTypeBits)
        | ((int)motionEvent.GetToolType(pointerIndex) & ToolTypeMask);

    private void AddPointerForIndex(
        MotionEvent motionEvent,
        int pointerIndex,
        PointerChange? pointerChange,
        int pointerData,
        List<EnginePointerData> packet,
        Context? context = null)
    {
        if (pointerChange is not { } change)
        {
            return;
        }

        int pointerId = UniquePointerIdByType(motionEvent, pointerIndex);
        PointerDeviceKind pointerKind = DeviceKindForToolType(motionEvent.GetToolType(pointerIndex));
        (float X, float Y) position = (motionEvent.GetX(pointerIndex), motionEvent.GetY(pointerIndex));
        long buttons;
        if (pointerKind == PointerDeviceKind.Mouse)
        {
            buttons = (long)motionEvent.ButtonState & 0x1F;
            // A touchpad drag reaches Android as a mouse without buttons: the engine treats it as a pan.
            if (buttons == 0 && motionEvent.Source == InputSourceType.Mouse && change == PointerChange.Down)
            {
                _ongoingPans[pointerId] = position;
            }
        }
        else if (pointerKind == PointerDeviceKind.Stylus)
        {
            buttons = ((long)motionEvent.ButtonState >> 4) & 0xF;
        }
        else
        {
            buttons = 0;
        }

        bool isTrackpadPan = _ongoingPans.TryGetValue(pointerId, out (float X, float Y) panStart);
        PointerChange? panZoomType = null;
        if (isTrackpadPan)
        {
            panZoomType = PointerChangeForPanZoom(change);
            if (panZoomType is null)
            {
                return;
            }
        }

        PointerSignalKind signalKind = motionEvent.ActionMasked == MotionEventActions.Scroll
            ? PointerSignalKind.Scroll
            : PointerSignalKind.None;
        (double pressureMin, double pressureMax) = (0.0, 1.0);
        if (motionEvent.Device?.GetMotionRange(Axis.Pressure) is { } pressureRange)
        {
            (pressureMin, pressureMax) = (pressureRange.Min, pressureRange.Max);
        }

        var data = new EnginePointerData
        {
            ViewId = _viewId,
            // MotionEvent times are milliseconds; the engine's are microseconds.
            TimeStamp = TimeSpan.FromMicroseconds(motionEvent.EventTime * 1000),
            Change = isTrackpadPan ? panZoomType!.Value : change,
            Kind = isTrackpadPan ? PointerDeviceKind.Trackpad : pointerKind,
            SignalKind = signalKind,
            Device = pointerId,
            PhysicalX = isTrackpadPan ? panStart.X : position.X,
            PhysicalY = isTrackpadPan ? panStart.Y : position.Y,
            Buttons = buttons,
            Pressure = motionEvent.GetPressure(pointerIndex),
            PressureMin = pressureMin,
            PressureMax = pressureMax,
            Distance = pointerKind == PointerDeviceKind.Stylus
                ? motionEvent.GetAxisValue(Axis.Distance, pointerIndex)
                : 0.0,
            Size = motionEvent.GetSize(pointerIndex),
            RadiusMajor = motionEvent.GetToolMajor(pointerIndex),
            RadiusMinor = motionEvent.GetToolMinor(pointerIndex),
            Orientation = motionEvent.GetAxisValue(Axis.Orientation, pointerIndex),
            Tilt = pointerKind == PointerDeviceKind.Stylus ? motionEvent.GetAxisValue(Axis.Tilt, pointerIndex) : 0.0,
            PlatformData = pointerData,
            PanX = isTrackpadPan ? position.X - panStart.X : 0.0,
            PanY = isTrackpadPan ? position.Y - panStart.Y : 0.0,
            Scale = 1.0,
        };
        if (signalKind == PointerSignalKind.Scroll)
        {
            double horizontalScaleFactor = DefaultHorizontalScrollFactor;
            double verticalScaleFactor = DefaultVerticalScrollFactor;
            if (context is not null)
            {
                horizontalScaleFactor = HorizontalScrollFactor(context);
                verticalScaleFactor = VerticalScrollFactor(context);
            }

            data.ScrollDeltaX = horizontalScaleFactor * -motionEvent.GetAxisValue(Axis.Hscroll, pointerIndex);
            data.ScrollDeltaY = verticalScaleFactor * -motionEvent.GetAxisValue(Axis.Vscroll, pointerIndex);
        }

        packet.Add(data);
        if (isTrackpadPan && panZoomType == PointerChange.PanZoomEnd)
        {
            _ongoingPans.Remove(pointerId);
        }
    }

    private float HorizontalScrollFactor(Context context) =>
        OperatingSystem.IsAndroidVersionAtLeast(26)
            ? global::Android.Views.ViewConfiguration.Get(context)!.ScaledHorizontalScrollFactor
            : VerticalScrollFactorPre26(context);

    private float VerticalScrollFactor(Context context) =>
        OperatingSystem.IsAndroidVersionAtLeast(26)
            ? global::Android.Views.ViewConfiguration.Get(context)!.ScaledVerticalScrollFactor
            : VerticalScrollFactorPre26(context);

    // Before Android 8 one wheel step scrolls a preferred list item's height.
    private int VerticalScrollFactorPre26(Context context)
    {
        if (_cachedVerticalScrollFactor == 0)
        {
            var outValue = new global::Android.Util.TypedValue();
            if (context.Theme?.ResolveAttribute(global::Android.Resource.Attribute.ListPreferredItemHeight, outValue,
                    true) != true)
            {
                return DefaultVerticalScrollFactor;
            }

            _cachedVerticalScrollFactor = (int)outValue.GetDimension(context.Resources!.DisplayMetrics);
        }

        return _cachedVerticalScrollFactor;
    }

    private static PointerChange? PointerChangeForAction(MotionEventActions maskedAction) => maskedAction switch
    {
        MotionEventActions.Down or MotionEventActions.PointerDown => PointerChange.Down,
        MotionEventActions.Up or MotionEventActions.PointerUp => PointerChange.Up,
        MotionEventActions.Move => PointerChange.Move,
        MotionEventActions.HoverMove or MotionEventActions.Scroll => PointerChange.Hover,
        MotionEventActions.Cancel => PointerChange.Cancel,
        _ => null,
    };

    private static PointerChange? PointerChangeForPanZoom(PointerChange pointerChange) => pointerChange switch
    {
        PointerChange.Down => PointerChange.PanZoomStart,
        PointerChange.Move => PointerChange.PanZoomUpdate,
        PointerChange.Up or PointerChange.Cancel => PointerChange.PanZoomEnd,
        _ => null,
    };

    private static PointerDeviceKind DeviceKindForToolType(MotionEventToolType toolType) => toolType switch
    {
        MotionEventToolType.Finger => PointerDeviceKind.Touch,
        MotionEventToolType.Stylus => PointerDeviceKind.Stylus,
        MotionEventToolType.Mouse => PointerDeviceKind.Mouse,
        MotionEventToolType.Eraser => PointerDeviceKind.InvertedStylus,
        _ => PointerDeviceKind.Unknown,
    };
}
