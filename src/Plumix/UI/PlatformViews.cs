using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/services/platform_views.dart

namespace Plumix.UI;

/// <summary>Converts a given point from the global coordinate system in logical pixels to the local
/// coordinate system for a box.</summary>
/// <remarks>Flutter's <c>PointTransformer</c>.</remarks>
public delegate Point PointTransformer(Point position);

/// <summary>Callback signature for when a platform view was created.</summary>
/// <remarks>Flutter's <c>PlatformViewCreatedCallback</c>; <paramref name="id"/> is the platform view's
/// unique identifier.</remarks>
public delegate void PlatformViewCreatedCallback(int id);

/// <summary>A registry responsible for generating unique identifier for platform views.</summary>
/// <remarks>
/// Flutter's <c>PlatformViewsRegistry</c>. A Flutter application has a single
/// <see cref="Instance"/> (Dart's top-level <c>platformViewsRegistry</c>).
/// </remarks>
public sealed class PlatformViewsRegistry
{
    private int _nextPlatformViewId;

    private PlatformViewsRegistry()
    {
    }

    /// <summary>The <see cref="PlatformViewsRegistry"/> responsible for generating unique identifiers
    /// for platform views.</summary>
    /// <remarks>Dart's top-level <c>platformViewsRegistry</c>.</remarks>
    public static PlatformViewsRegistry Instance { get; } = new();

    /// <summary>Allocates a unique identifier for a platform view.</summary>
    /// <remarks>
    /// A platform view identifier can refer to a platform view that was never created, a platform view
    /// that was disposed, or a platform view that is alive. Typically a platform view identifier is
    /// passed to a platform view widget which creates the platform view and manages its lifecycle.
    /// </remarks>
    public int GetNextPlatformViewId()
    {
        // On the Android side, the interface exposed to users uses 32-bit integers.
        // See https://github.com/flutter/engine/pull/39476 for more details.

        // We can safely assume that a Flutter application will not require more
        // than MAX_INT32 platform views during its lifetime.
        const int maxInt32 = 0x7FFFFFFF;
        Debug.Assert(_nextPlatformViewId <= maxInt32);
        return _nextPlatformViewId++;
    }
}

/// <summary>Provides access to the platform views service.</summary>
/// <remarks>
/// Flutter's <c>PlatformViewsService</c>. This service allows creating and controlling platform-specific
/// views.
/// </remarks>
public sealed class PlatformViewsService
{
    private static PlatformViewsService? _instance;

    private readonly Dictionary<int, Action> _focusCallbacks = [];

    private PlatformViewsService()
    {
        SystemChannels.PlatformViews.SetMethodCallHandler(OnMethodCall);
    }

    /// <summary>Dart's lazily created <c>_instance</c>: creating it registers the inbound handler.</summary>
    internal static PlatformViewsService Instance => _instance ??= new PlatformViewsService();

    internal Dictionary<int, Action> FocusCallbacks => _focusCallbacks;

    private Task<object?> OnMethodCall(MethodCall call)
    {
        switch (call.Method)
        {
            case "viewFocused":
                int id = Convert.ToInt32(call.Arguments);
                if (_focusCallbacks.TryGetValue(id, out Action? callback))
                {
                    callback();
                }

                break;
            default:
                throw new NotImplementedException(
                    $"{call.Method} was invoked but isn't implemented by PlatformViewsService");
        }

        return Task.FromResult<object?>(null);
    }

    /// <summary>Creates a controller for a new Android view.</summary>
    /// <remarks>
    /// <paramref name="id"/> is an unused unique identifier generated with
    /// <see cref="PlatformViewsRegistry"/>. <paramref name="viewType"/> is the identifier of the Android
    /// view type to be created, a factory for this view type must have been registered on the platform
    /// side. Platform view factories are typically registered by plugin code. Plugins can register a
    /// platform view factory with <c>PlatformViewRegistry#registerViewFactory</c>.
    /// <paramref name="creationParams"/> will be passed as the args argument of
    /// <c>PlatformViewFactory#create</c>; <paramref name="creationParamsCodec"/> is the codec used to
    /// encode it and must be non-null when <paramref name="creationParams"/> is not null.
    /// <paramref name="onFocus"/> is a callback that will be invoked when the Android View asks to get
    /// the input focus.
    /// The Android view will only be created after <see cref="AndroidViewController.SetSize"/> is
    /// called for the first time.
    /// The <c>id</c>, <c>viewType</c>, and <c>layoutDirection</c> parameters must not be null.
    /// This display mode is the texture layer composition (TLHC) mode with a virtual display fallback;
    /// it falls back to Hybrid Composition++ when the platform cannot use a texture.
    /// </remarks>
    public static AndroidViewController InitAndroidView(
        int id,
        string viewType,
        TextDirection layoutDirection,
        object? creationParams = null,
        IMessageCodec? creationParamsCodec = null,
        Action? onFocus = null)
    {
        Debug.Assert(creationParams is null || creationParamsCodec is not null);

        var controller = new TextureAndroidViewController(
            viewId: id,
            viewType: viewType,
            layoutDirection: layoutDirection,
            creationParams: creationParams,
            creationParamsCodec: creationParamsCodec);

        Instance._focusCallbacks[id] = onFocus ?? (() => { });
        return controller;
    }

    /// <summary>Creates a controller for a new Android view.</summary>
    /// <remarks>
    /// Same as <see cref="InitAndroidView"/>, but the view is displayed through texture layer
    /// composition with a Hybrid Composition fallback (<c>hybridFallback: true</c>).
    /// </remarks>
    public static SurfaceAndroidViewController InitSurfaceAndroidView(
        int id,
        string viewType,
        TextDirection layoutDirection,
        object? creationParams = null,
        IMessageCodec? creationParamsCodec = null,
        Action? onFocus = null)
    {
        Debug.Assert(creationParams is null || creationParamsCodec is not null);

        var controller = new SurfaceAndroidViewController(
            viewId: id,
            viewType: viewType,
            layoutDirection: layoutDirection,
            creationParams: creationParams,
            creationParamsCodec: creationParamsCodec);
        Instance._focusCallbacks[id] = onFocus ?? (() => { });
        return controller;
    }

    /// <summary>Creates a controller for a new Android view.</summary>
    /// <remarks>
    /// Same as <see cref="InitAndroidView"/>, but the view is always displayed through Hybrid
    /// Composition, which is expensive to render; use it only when texture composition does not work
    /// for the view.
    /// </remarks>
    public static ExpensiveAndroidViewController InitExpensiveAndroidView(
        int id,
        string viewType,
        TextDirection layoutDirection,
        object? creationParams = null,
        IMessageCodec? creationParamsCodec = null,
        Action? onFocus = null)
    {
        var controller = new ExpensiveAndroidViewController(
            viewId: id,
            viewType: viewType,
            layoutDirection: layoutDirection,
            creationParams: creationParams,
            creationParamsCodec: creationParamsCodec);

        Instance._focusCallbacks[id] = onFocus ?? (() => { });
        return controller;
    }

    /// <summary>Creates a controller for a new Android view.</summary>
    /// <remarks>
    /// Same as <see cref="InitAndroidView"/>, but the view is displayed through Hybrid Composition++,
    /// which requires <see cref="HybridAndroidViewController.CheckIfSupported"/> to be true.
    /// </remarks>
    public static HybridAndroidViewController InitHybridAndroidView(
        int id,
        string viewType,
        TextDirection layoutDirection,
        object? creationParams = null,
        IMessageCodec? creationParamsCodec = null,
        Action? onFocus = null)
    {
        var controller = new HybridAndroidViewController(
            viewId: id,
            viewType: viewType,
            layoutDirection: layoutDirection,
            creationParams: creationParams,
            creationParamsCodec: creationParamsCodec);

        Instance._focusCallbacks[id] = onFocus ?? (() => { });
        return controller;
    }

    /// <summary>Factory method to create a <c>UiKitView</c>.</summary>
    /// <remarks>
    /// <paramref name="id"/> is an unused unique identifier generated with
    /// <see cref="PlatformViewsRegistry"/>. <paramref name="viewType"/> is the identifier of the iOS view
    /// type to be created, a factory for this view type must have been registered on the platform side.
    /// <paramref name="onFocus"/> is a callback that will be invoked when the UIKit view asks to get the
    /// input focus. <paramref name="creationParamsCodec"/> must be non-null when
    /// <paramref name="creationParams"/> is not null.
    /// </remarks>
    public static async Task<UiKitViewController> InitUiKitView(
        int id,
        string viewType,
        TextDirection layoutDirection,
        UiKitViewGestureBlockingPolicy gestureBlockingPolicy =
            UiKitViewGestureBlockingPolicy.FallbackToPluginDefault,
        object? creationParams = null,
        IMessageCodec? creationParamsCodec = null,
        Action? onFocus = null)
    {
        Debug.Assert(creationParams is null || creationParamsCodec is not null);

        string gestureBlockingPolicyValue = gestureBlockingPolicy switch
        {
            UiKitViewGestureBlockingPolicy.Eager => "eager",
            UiKitViewGestureBlockingPolicy.WaitUntilTouchesEnded => "waitUntilTouchesEnded",
            UiKitViewGestureBlockingPolicy.FallbackToPluginDefault => "fallbackToPluginDefault",
            UiKitViewGestureBlockingPolicy.DoNotBlockGesture => "doNotBlockGesture",
            _ => throw new ArgumentOutOfRangeException(nameof(gestureBlockingPolicy)),
        };

        // TODO(amirh): pass layoutDirection once the system channel supports it.
        // https://github.com/flutter/flutter/issues/133682
        var args = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["viewType"] = viewType,
            ["gestureBlockingPolicy"] = gestureBlockingPolicyValue,
        };
        if (creationParams is not null)
        {
            args["params"] = EncodeCreationParams(creationParamsCodec!, creationParams);
        }

        await SystemChannels.PlatformViews.InvokeMethod<object>("create", args);
        if (onFocus is not null)
        {
            Instance._focusCallbacks[id] = onFocus;
        }

        return new UiKitViewController(id, layoutDirection);
    }

    /// <summary>Factory method to create an <c>AppKitView</c>.</summary>
    /// <remarks>
    /// The same contract as <see cref="InitUiKitView"/>, without a gesture blocking policy.
    /// </remarks>
    public static async Task<AppKitViewController> InitAppKitView(
        int id,
        string viewType,
        TextDirection layoutDirection,
        object? creationParams = null,
        IMessageCodec? creationParamsCodec = null,
        Action? onFocus = null)
    {
        Debug.Assert(creationParams is null || creationParamsCodec is not null);

        // TODO(amirh): pass layoutDirection once the system channel supports it.
        // https://github.com/flutter/flutter/issues/133682
        var args = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["viewType"] = viewType,
        };
        if (creationParams is not null)
        {
            args["params"] = EncodeCreationParams(creationParamsCodec!, creationParams);
        }

        await SystemChannels.PlatformViews.InvokeMethod<object>("create", args);
        if (onFocus is not null)
        {
            Instance._focusCallbacks[id] = onFocus;
        }

        return new AppKitViewController(id, layoutDirection);
    }

    /// <summary>Dart's <c>Uint8List.view(paramsByteData.buffer, 0, paramsByteData.lengthInBytes)</c>.</summary>
    internal static byte[] EncodeCreationParams(IMessageCodec codec, object? creationParams)
    {
        ByteData paramsByteData = codec.EncodeMessage(creationParams)!;
        return paramsByteData.Buffer.AsSpan(0, paramsByteData.LengthInBytes).ToArray();
    }
}

/// <summary>Properties of an Android pointer.</summary>
/// <remarks>A Dart version of Android's <c>MotionEvent.PointerProperties</c>.</remarks>
public sealed class AndroidPointerProperties
{
    /// <summary>The tool type is unknown.</summary>
    public const int ToolTypeUnknown = 0;

    /// <summary>The tool type is a finger.</summary>
    public const int ToolTypeFinger = 1;

    /// <summary>The tool type is a stylus.</summary>
    public const int ToolTypeStylus = 2;

    /// <summary>The tool type is a mouse.</summary>
    public const int ToolTypeMouse = 3;

    /// <summary>The tool type is an eraser.</summary>
    public const int ToolTypeEraser = 4;

    /// <summary>Creates an <see cref="AndroidPointerProperties"/> object.</summary>
    public AndroidPointerProperties(int id, int toolType)
    {
        Id = id;
        ToolType = toolType;
    }

    /// <summary>See Android's <c>MotionEvent.PointerProperties#id</c>.</summary>
    public int Id { get; }

    /// <summary>The type of tool used to make contact such as a finger or stylus, if known.</summary>
    public int ToolType { get; }

    internal List<int> AsList() => [Id, ToolType];

    /// <inheritdoc />
    public override string ToString() =>
        $"{Diagnostics.ObjectRuntimeType(this, "AndroidPointerProperties")}(id: {Id}, toolType: {ToolType})";
}

/// <summary>Position information for an Android pointer.</summary>
/// <remarks>A Dart version of Android's <c>MotionEvent.PointerCoords</c>.</remarks>
public sealed class AndroidPointerCoords
{
    /// <summary>Creates an AndroidPointerCoords.</summary>
    public AndroidPointerCoords(
        double orientation,
        double pressure,
        double size,
        double toolMajor,
        double toolMinor,
        double touchMajor,
        double touchMinor,
        double x,
        double y)
    {
        Orientation = orientation;
        Pressure = pressure;
        Size = size;
        ToolMajor = toolMajor;
        ToolMinor = toolMinor;
        TouchMajor = touchMajor;
        TouchMinor = touchMinor;
        X = x;
        Y = y;
    }

    /// <summary>The orientation of the touch area and tool area in radians clockwise from vertical.</summary>
    public double Orientation { get; }

    /// <summary>A normalized value that describes the pressure applied to the device by a finger or
    /// other tool.</summary>
    public double Pressure { get; }

    /// <summary>A normalized value that describes the approximate size of the pointer touch area in
    /// relation to the maximum detectable size of the device.</summary>
    public double Size { get; }

    /// <summary>See Android's <c>MotionEvent.PointerCoords#toolMajor</c>.</summary>
    public double ToolMajor { get; }

    /// <summary>See Android's <c>MotionEvent.PointerCoords#toolMinor</c>.</summary>
    public double ToolMinor { get; }

    /// <summary>See Android's <c>MotionEvent.PointerCoords#touchMajor</c>.</summary>
    public double TouchMajor { get; }

    /// <summary>See Android's <c>MotionEvent.PointerCoords#touchMinor</c>.</summary>
    public double TouchMinor { get; }

    /// <summary>The X component of the pointer movement.</summary>
    public double X { get; }

    /// <summary>The Y component of the pointer movement.</summary>
    public double Y { get; }

    internal List<double> AsList() =>
        [Orientation, Pressure, Size, ToolMajor, ToolMinor, TouchMajor, TouchMinor, X, Y];

    /// <inheritdoc />
    public override string ToString() =>
        $"{Diagnostics.ObjectRuntimeType(this, "AndroidPointerCoords")}(orientation: {D(Orientation)}, "
        + $"pressure: {D(Pressure)}, size: {D(Size)}, toolMajor: {D(ToolMajor)}, "
        + $"toolMinor: {D(ToolMinor)}, touchMajor: {D(TouchMajor)}, touchMinor: {D(TouchMinor)}, "
        + $"x: {D(X)}, y: {D(Y)})";

    internal static string D(double value) => BindingBase.DartDoubleToString(value);

    internal static string L<T>(IEnumerable<T> values) => $"[{string.Join(", ", values)}]";
}

/// <summary>A Dart version of Android's <c>MotionEvent</c>.</summary>
/// <remarks>
/// Used by <see cref="AndroidViewController.SendMotionEvent"/> to send <c>MotionEvent</c> objects to
/// embedded Android Views. Dart's <c>downTime</c>/<c>eventTime</c> are <c>int</c>s, which are 64-bit;
/// they are <c>long</c> here.
/// </remarks>
public sealed class AndroidMotionEvent
{
    /// <summary>Creates an AndroidMotionEvent.</summary>
    public AndroidMotionEvent(
        long downTime,
        long eventTime,
        int action,
        int pointerCount,
        IReadOnlyList<AndroidPointerProperties> pointerProperties,
        IReadOnlyList<AndroidPointerCoords> pointerCoords,
        int metaState,
        int buttonState,
        double xPrecision,
        double yPrecision,
        int deviceId,
        int edgeFlags,
        int source,
        int flags,
        int motionEventId)
    {
        Debug.Assert(pointerProperties.Count == pointerCount);
        Debug.Assert(pointerCoords.Count == pointerCount);
        DownTime = downTime;
        EventTime = eventTime;
        Action = action;
        PointerCount = pointerCount;
        PointerProperties = pointerProperties;
        PointerCoords = pointerCoords;
        MetaState = metaState;
        ButtonState = buttonState;
        XPrecision = xPrecision;
        YPrecision = yPrecision;
        DeviceId = deviceId;
        EdgeFlags = edgeFlags;
        Source = source;
        Flags = flags;
        MotionEventId = motionEventId;
    }

    /// <summary>The time (in ms) when the user originally pressed down to start a stream of position
    /// events, relative to an arbitrary timeline.</summary>
    public long DownTime { get; }

    /// <summary>The time this event occurred, relative to an arbitrary timeline.</summary>
    public long EventTime { get; }

    /// <summary>A value representing the kind of action being performed.</summary>
    public int Action { get; }

    /// <summary>The number of pointers that are part of this event.</summary>
    public int PointerCount { get; }

    /// <summary>List of <see cref="AndroidPointerProperties"/> for each pointer that is part of this
    /// event.</summary>
    public IReadOnlyList<AndroidPointerProperties> PointerProperties { get; }

    /// <summary>List of <see cref="AndroidPointerCoords"/> for each pointer that is part of this
    /// event.</summary>
    public IReadOnlyList<AndroidPointerCoords> PointerCoords { get; }

    /// <summary>The state of any meta / modifier keys that were in effect when the event was
    /// generated.</summary>
    public int MetaState { get; }

    /// <summary>The state of all buttons that are pressed such as a mouse or stylus button.</summary>
    public int ButtonState { get; }

    /// <summary>The precision of the X coordinates being reported, in physical pixels.</summary>
    public double XPrecision { get; }

    /// <summary>The precision of the Y coordinates being reported, in physical pixels.</summary>
    public double YPrecision { get; }

    /// <summary>See Android's <c>MotionEvent#getDeviceId</c>.</summary>
    public int DeviceId { get; }

    /// <summary>A bit field indicating which edges, if any, were touched by this MotionEvent.</summary>
    public int EdgeFlags { get; }

    /// <summary>The source of this event (e.g a touchpad or stylus).</summary>
    public int Source { get; }

    /// <summary>See Android's <c>MotionEvent#getFlags</c>.</summary>
    public int Flags { get; }

    /// <summary>Used to identify this MotionEvent uniquely in the Flutter Engine.</summary>
    public int MotionEventId { get; }

    internal List<object?> AsList(int viewId)
    {
        return
        [
            viewId,
            DownTime,
            EventTime,
            Action,
            PointerCount,
            PointerProperties.Select(object? (p) => p.AsList()).ToList(),
            PointerCoords.Select(object? (p) => p.AsList()).ToList(),
            MetaState,
            ButtonState,
            XPrecision,
            YPrecision,
            DeviceId,
            EdgeFlags,
            Source,
            Flags,
            MotionEventId,
        ];
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"AndroidPointerEvent(downTime: {DownTime}, eventTime: {EventTime}, action: {Action}, "
        + $"pointerCount: {PointerCount}, pointerProperties: {AndroidPointerCoords.L(PointerProperties)}, "
        + $"pointerCoords: {AndroidPointerCoords.L(PointerCoords)}, metaState: {MetaState}, "
        + $"buttonState: {ButtonState}, xPrecision: {AndroidPointerCoords.D(XPrecision)}, "
        + $"yPrecision: {AndroidPointerCoords.D(YPrecision)}, deviceId: {DeviceId}, "
        + $"edgeFlags: {EdgeFlags}, source: {Source}, flags: {Flags}, motionEventId: {MotionEventId})";
}

internal enum AndroidPlatformViewState
{
    WaitingForSize,
    Creating,
    Created,
    Disposed,
}

// Helper for converting PointerEvents into AndroidMotionEvents.
internal sealed class AndroidMotionEventConverter
{
    private PointTransformer? _pointTransformer;

    internal Dictionary<int, AndroidPointerCoords> PointerPositions { get; } = [];

    internal Dictionary<int, AndroidPointerProperties> PointerProperties { get; } = [];

    internal HashSet<int> UsedAndroidPointerIds { get; } = [];

    /// <summary>Dart's <c>late PointTransformer pointTransformer</c>: reading it unset throws.</summary>
    internal PointTransformer PointTransformer
    {
        get => _pointTransformer
            ?? throw new InvalidOperationException("Field 'pointTransformer' has not been initialized.");
        set => _pointTransformer = value;
    }

    internal long? DownTimeMillis { get; private set; }

    internal void HandlePointerDownEvent(PointerDownEvent @event)
    {
        if (PointerProperties.Count == 0)
        {
            DownTimeMillis = InMilliseconds(@event);
        }

        int androidPointerId = 0;
        while (UsedAndroidPointerIds.Contains(androidPointerId))
        {
            androidPointerId++;
        }

        UsedAndroidPointerIds.Add(androidPointerId);
        PointerProperties[@event.Pointer] = PropertiesFor(@event, androidPointerId);
    }

    internal void UpdatePointerPositions(PointerEvent @event)
    {
        Point position = PointTransformer(@event.Position);
        PointerPositions[@event.Pointer] = new AndroidPointerCoords(
            orientation: @event.Orientation,
            pressure: @event.Pressure,
            size: @event.Size,
            toolMajor: @event.RadiusMajor,
            toolMinor: @event.RadiusMinor,
            touchMajor: @event.RadiusMajor,
            touchMinor: @event.RadiusMinor,
            x: position.X,
            y: position.Y);
    }

    private void Remove(int pointer)
    {
        PointerPositions.Remove(pointer);
        UsedAndroidPointerIds.Remove(PointerProperties[pointer].Id);
        PointerProperties.Remove(pointer);
        if (PointerProperties.Count == 0)
        {
            DownTimeMillis = null;
        }
    }

    internal void HandlePointerUpEvent(PointerUpEvent @event) => Remove(@event.Pointer);

    internal void HandlePointerCancelEvent(PointerCancelEvent @event) => Remove(@event.Pointer);

    internal AndroidMotionEvent? ToAndroidMotionEvent(PointerEvent @event)
    {
        List<int> pointers = PointerPositions.Keys.ToList();
        int pointerIdx = pointers.IndexOf(@event.Pointer);
        int numPointers = pointers.Count;

        // This value must match the value in engine's FlutterView.java.
        // This flag indicates whether the original Android pointer events were batched together.
        const int kPointerDataFlagBatched = 1;

        // This value must match the value in engine's FlutterView.java.
        // This flag indicates that the pointer event has a multiple pointer count.
        const int kPointerDataFlagMultiple = 2;

        const int kPointerDataFlagMask = 0xff;
        const int kPointerDataMultiplePointerCountShift = 8;

        // Android MotionEvents batch all down/up/move events together, while Flutter creates one
        // PointerEvent per pointer. Since the engine flattens Android events, ignore batched events
        // that are not the last of the batch.
        int platformDataFlag = @event.PlatformData & kPointerDataFlagMask;
        if (platformDataFlag == kPointerDataFlagBatched)
        {
            return null;
        }

        if (platformDataFlag == kPointerDataFlagMultiple)
        {
            int originalPointerCount = @event.PlatformData >> kPointerDataMultiplePointerCountShift;
            // Ignore the event if this is not the last pointer of the original multi-pointer event.
            if (pointerIdx != originalPointerCount - 1)
            {
                return null;
            }
        }

        int action;
        switch (@event)
        {
            case PointerDownEvent when numPointers == 1:
                action = AndroidViewController.ActionDown;
                break;
            case PointerUpEvent when numPointers == 1:
                action = AndroidViewController.ActionUp;
                break;
            case PointerDownEvent:
                action = AndroidViewController.PointerAction(pointerIdx, AndroidViewController.ActionPointerDown);
                break;
            case PointerUpEvent:
                action = AndroidViewController.PointerAction(pointerIdx, AndroidViewController.ActionPointerUp);
                break;
            case PointerMoveEvent:
                action = AndroidViewController.ActionMove;
                break;
            case PointerCancelEvent:
                action = AndroidViewController.ActionCancel;
                break;
            default:
                return null;
        }

        return new AndroidMotionEvent(
            downTime: DownTimeMillis!.Value,
            eventTime: InMilliseconds(@event),
            action: action,
            pointerCount: PointerPositions.Count,
            pointerProperties: pointers.Select(i => PointerProperties[i]).ToList(),
            pointerCoords: pointers.Select(i => PointerPositions[i]).ToList(),
            metaState: 0,
            buttonState: 0,
            xPrecision: 1.0,
            yPrecision: 1.0,
            deviceId: 0,
            edgeFlags: 0,
            source: SourceFor(@event),
            flags: 0,
            motionEventId: @event.EmbedderId);
    }

    internal static int SourceFor(PointerEvent @event)
    {
        return @event.Kind switch
        {
            PointerDeviceKind.Touch => AndroidViewController.InputDeviceSourceTouchScreen,
            PointerDeviceKind.Trackpad => AndroidViewController.InputDeviceSourceTouchPad,
            PointerDeviceKind.Mouse => AndroidViewController.InputDeviceSourceMouse,
            PointerDeviceKind.Stylus => AndroidViewController.InputDeviceSourceStylus,
            PointerDeviceKind.InvertedStylus => AndroidViewController.InputDeviceSourceStylus,
            PointerDeviceKind.Unknown => AndroidViewController.InputDeviceSourceUnknown,
            _ => AndroidViewController.InputDeviceSourceUnknown,
        };
    }

    internal static AndroidPointerProperties PropertiesFor(PointerEvent @event, int pointerId)
    {
        int toolType = @event.Kind switch
        {
            PointerDeviceKind.Touch => AndroidPointerProperties.ToolTypeFinger,
            PointerDeviceKind.Trackpad => AndroidPointerProperties.ToolTypeFinger,
            PointerDeviceKind.Mouse => AndroidPointerProperties.ToolTypeMouse,
            PointerDeviceKind.Stylus => AndroidPointerProperties.ToolTypeStylus,
            PointerDeviceKind.InvertedStylus => AndroidPointerProperties.ToolTypeEraser,
            PointerDeviceKind.Unknown => AndroidPointerProperties.ToolTypeUnknown,
            _ => AndroidPointerProperties.ToolTypeUnknown,
        };
        return new AndroidPointerProperties(id: pointerId, toolType: toolType);
    }

    /// <summary>Dart's <c>event.timeStamp.inMilliseconds</c> over Plumix's UTC time stamp.</summary>
    private static long InMilliseconds(PointerEvent @event) =>
        @event.TimestampUtc.Ticks / TimeSpan.TicksPerMillisecond;
}

internal sealed class CreationParams(object? data, IMessageCodec codec)
{
    public object? Data { get; } = data;

    public IMessageCodec Codec { get; } = codec;
}

/// <summary>Controls an Android view that is composed using a GL texture.</summary>
/// <remarks>
/// Flutter's <c>AndroidViewController</c>. Typically created with
/// <see cref="PlatformViewsService.InitAndroidView"/>. Dart's constructor is private and Dart classes
/// double as interfaces (its test fake <c>implements AndroidViewController</c>); C# has no implicit
/// interfaces, so the constructor is protected and every public member is virtual
/// (<c>docs/ai/DIVERGENCES.md</c>).
/// </remarks>
public abstract class AndroidViewController : PlatformViewController
{
    /// <summary>Action code for when a primary pointer touched the screen.</summary>
    public const int ActionDown = 0;

    /// <summary>Action code for when a primary pointer stopped touching the screen.</summary>
    public const int ActionUp = 1;

    /// <summary>Action code for when the event only includes information about pointer movement.</summary>
    public const int ActionMove = 2;

    /// <summary>Action code for when a motion event has been canceled.</summary>
    public const int ActionCancel = 3;

    /// <summary>Action code for when a secondary pointer touched the screen.</summary>
    public const int ActionPointerDown = 5;

    /// <summary>Action code for when a secondary pointer stopped touching the screen.</summary>
    public const int ActionPointerUp = 6;

    /// <summary>Android's <c>View.LAYOUT_DIRECTION_LTR</c> value.</summary>
    public const int AndroidLayoutDirectionLtr = 0;

    /// <summary>Android's <c>View.LAYOUT_DIRECTION_RTL</c> value.</summary>
    public const int AndroidLayoutDirectionRtl = 1;

    /// <summary>Android's <c>InputDevice.SOURCE_UNKNOWN</c>.</summary>
    public const int InputDeviceSourceUnknown = 0;

    /// <summary>Android's <c>InputDevice.SOURCE_TOUCHSCREEN</c>.</summary>
    public const int InputDeviceSourceTouchScreen = 4098;

    /// <summary>Android's <c>InputDevice.SOURCE_MOUSE</c>.</summary>
    public const int InputDeviceSourceMouse = 8194;

    /// <summary>Android's <c>InputDevice.SOURCE_STYLUS</c>.</summary>
    public const int InputDeviceSourceStylus = 16386;

    /// <summary>Android's <c>InputDevice.SOURCE_TOUCHPAD</c>.</summary>
    public const int InputDeviceSourceTouchPad = 1048584;

    private readonly AndroidMotionEventConverter _motionEventConverter = new();
    private readonly List<PlatformViewCreatedCallback> _platformViewCreatedCallbacks = [];

    /// <summary>Dart's private <c>AndroidViewController._</c>.</summary>
    protected AndroidViewController(
        int viewId,
        string viewType,
        TextDirection layoutDirection,
        object? creationParams = null,
        IMessageCodec? creationParamsCodec = null)
    {
        Debug.Assert(creationParams is null || creationParamsCodec is not null);
        ViewId = viewId;
        ViewType = viewType;
        LayoutDirectionValue = layoutDirection;
        CreationParamsValue = creationParams is null
            ? null
            : new CreationParams(creationParams, creationParamsCodec!);
    }

    /// <summary>The unique identifier of the Android view controlled by this controller.</summary>
    public override int ViewId { get; }

    internal string ViewType { get; }

    internal TextDirection LayoutDirectionValue { get; private set; }

    internal AndroidPlatformViewState State { get; private set; } = AndroidPlatformViewState.WaitingForSize;

    internal CreationParams? CreationParamsValue { get; }

    private static int GetAndroidDirection(TextDirection direction)
    {
        return direction switch
        {
            TextDirection.Ltr => AndroidLayoutDirectionLtr,
            TextDirection.Rtl => AndroidLayoutDirectionRtl,
            _ => AndroidLayoutDirectionLtr,
        };
    }

    /// <summary>Creates a masked Android MotionEvent action value for an indexed pointer.</summary>
    public static int PointerAction(int pointerId, int action) =>
        ((pointerId << 8) & 0xff00) | (action & 0xff);

    /// <summary>Sends the message to dispose the platform view.</summary>
    private protected abstract Task SendDisposeMessage();

    /// <summary>True if <see cref="SendCreateMessage"/> can only be called with a non-null size.</summary>
    private protected abstract bool CreateRequiresSize { get; }

    /// <summary>Sends the message to create the platform view with an initial
    /// <paramref name="size"/>.</summary>
    /// <remarks>If <see cref="CreateRequiresSize"/> is true, <paramref name="size"/> is non-null.</remarks>
    private protected abstract Task SendCreateMessage(Size? size, Point? position = null);

    /// <summary>Sends the message to resize the platform view to <paramref name="size"/>.</summary>
    private protected abstract Task<Size> SendResizeMessage(Size size);

    /// <summary>Whether the platform view requires view composition, which is the case for Hybrid
    /// Composition.</summary>
    public virtual bool RequiresViewComposition => false;

    /// <inheritdoc />
    public override bool AwaitingCreation => State == AndroidPlatformViewState.WaitingForSize;

    /// <inheritdoc />
    public override async Task Create(Size? size = null, Point? position = null)
    {
        Debug.Assert(State != AndroidPlatformViewState.Disposed, "trying to create a disposed Android view");
        Debug.Assert(
            State == AndroidPlatformViewState.WaitingForSize,
            $"Android view is already sized. View id: {ViewId}");

        if (CreateRequiresSize && size is null)
        {
            // Wait for a setSize call.
            return;
        }

        State = AndroidPlatformViewState.Creating;
        await SendCreateMessage(size: size, position: position);
        State = AndroidPlatformViewState.Created;

        foreach (PlatformViewCreatedCallback callback in _platformViewCreatedCallbacks.ToList())
        {
            callback(ViewId);
        }
    }

    /// <summary>Sizes the Android View.</summary>
    /// <remarks>
    /// <paramref name="size"/> is the view's new size in logical pixel. It must be greater than zero.
    /// The first time a size is set triggers the creation of the Android view. Returns the buffer size
    /// corresponding to the size of the underlying texture, which may differ from the requested size.
    /// </remarks>
    public virtual async Task<Size> SetSize(Size size)
    {
        Debug.Assert(State != AndroidPlatformViewState.Disposed, $"Android view is disposed. View id: {ViewId}");
        if (State == AndroidPlatformViewState.WaitingForSize)
        {
            // Either `create` hasn't been called, or it couldn't run due to missing size information,
            // so create the view now.
            await Create(size: size);
            return size;
        }

        return await SendResizeMessage(size);
    }

    /// <summary>Sets the offset of the platform view.</summary>
    /// <remarks>
    /// <paramref name="off"/> is the view's new offset in logical pixel. On Android, this allows the
    /// Android native view to draw the a11y highlights in the same location on the screen as the
    /// platform view widget in the Flutter framework.
    /// </remarks>
    public abstract Task SetOffset(Point off);

    /// <summary>Returns the texture entry id that the Android view is rendering into.</summary>
    /// <remarks>Returns null if the Android view has not been successfully created, if it has been
    /// disposed, or if the implementation does not use textures.</remarks>
    public abstract int? TextureId { get; }

    /// <summary>True if the view is created and can be interacted with.</summary>
    public virtual bool IsCreated => State == AndroidPlatformViewState.Created;

    /// <summary>The created callbacks that are invoked after the platform view has been created.</summary>
    /// <remarks>Dart marks this <c>@visibleForTesting</c>.</remarks>
    public virtual IList<PlatformViewCreatedCallback> CreatedCallbacks => _platformViewCreatedCallbacks;

    /// <summary>Sends an Android MotionEvent to the platform view.</summary>
    /// <remarks>
    /// The Android MotionEvent object is created with MotionEvent.obtain; see the documentation of that
    /// method for each parameter's meaning.
    /// </remarks>
    public virtual async Task SendMotionEvent(AndroidMotionEvent @event)
    {
        await SystemChannels.PlatformViews.InvokeMethod<object>("touch", @event.AsList(ViewId));
    }

    /// <summary>Converts a given point from the global coordinate system in logical pixels to the local
    /// coordinate system for this box.</summary>
    /// <remarks>
    /// This is required to convert a <see cref="PointerEvent"/> to an <see cref="AndroidMotionEvent"/>.
    /// It is typically provided by using <c>RenderBox.GlobalToLocal</c>.
    /// </remarks>
    public virtual PointTransformer PointTransformer
    {
        get => _motionEventConverter.PointTransformer;
        set => _motionEventConverter.PointTransformer = value;
    }

    /// <summary>Adds a callback that will get invoke after the platform view has been created.</summary>
    public virtual void AddOnPlatformViewCreatedListener(PlatformViewCreatedCallback listener)
    {
        Debug.Assert(State != AndroidPlatformViewState.Disposed);
        _platformViewCreatedCallbacks.Add(listener);
    }

    /// <summary>Removes a callback added with <see cref="AddOnPlatformViewCreatedListener"/>.</summary>
    public virtual void RemoveOnPlatformViewCreatedListener(PlatformViewCreatedCallback listener)
    {
        Debug.Assert(State != AndroidPlatformViewState.Disposed);
        _platformViewCreatedCallbacks.Remove(listener);
    }

    /// <summary>Sets the layout direction for the Android view.</summary>
    public virtual async Task SetLayoutDirection(TextDirection layoutDirection)
    {
        Debug.Assert(
            State != AndroidPlatformViewState.Disposed,
            $"trying to set a layout direction for a disposed Android view. View id: {ViewId}");

        if (layoutDirection == LayoutDirectionValue)
        {
            return;
        }

        LayoutDirectionValue = layoutDirection;

        // If the view was not yet created we just update _layoutDirection and return, as the new
        // direction will be used in _create.
        if (State == AndroidPlatformViewState.WaitingForSize)
        {
            return;
        }

        await SystemChannels.PlatformViews.InvokeMethod<object>(
            "setDirection",
            new Dictionary<string, object?>
            {
                ["id"] = ViewId,
                ["direction"] = GetAndroidDirection(layoutDirection),
            });
    }

    /// <summary>Converts the <see cref="PointerEvent"/> and sends an Android MotionEvent to the view.</summary>
    /// <remarks>
    /// This method can only be used if a <see cref="PointTransformer"/> is provided to
    /// <see cref="PointTransformer"/>. Hover events are ignored.
    /// </remarks>
    public override async Task DispatchPointerEvent(PointerEvent @event)
    {
        if (@event is PointerHoverEvent)
        {
            return;
        }

        if (@event is PointerDownEvent downEvent)
        {
            _motionEventConverter.HandlePointerDownEvent(downEvent);
        }

        _motionEventConverter.UpdatePointerPositions(@event);

        AndroidMotionEvent? androidEvent = _motionEventConverter.ToAndroidMotionEvent(@event);

        if (@event is PointerUpEvent upEvent)
        {
            _motionEventConverter.HandlePointerUpEvent(upEvent);
        }
        else if (@event is PointerCancelEvent cancelEvent)
        {
            _motionEventConverter.HandlePointerCancelEvent(cancelEvent);
        }

        if (androidEvent is not null)
        {
            await SendMotionEvent(androidEvent);
        }
    }

    /// <summary>Clears the focus from the Android View if it is focused.</summary>
    public override Task ClearFocus()
    {
        if (State != AndroidPlatformViewState.Created)
        {
            return Task.CompletedTask;
        }

        return SystemChannels.PlatformViews.InvokeMethod<object>("clearFocus", ViewId);
    }

    /// <summary>Disposes the Android view.</summary>
    /// <remarks>
    /// The <see cref="AndroidViewController"/> object is unusable after calling this. The identifier of
    /// the platform view cannot be reused after the view is disposed.
    /// </remarks>
    public override async Task Dispose()
    {
        AndroidPlatformViewState state = State;
        State = AndroidPlatformViewState.Disposed;
        _platformViewCreatedCallbacks.Clear();
        PlatformViewsService.Instance.FocusCallbacks.Remove(ViewId);
        if (state is AndroidPlatformViewState.Creating or AndroidPlatformViewState.Created)
        {
            await SendDisposeMessage();
        }
    }
}

/// <summary>Controls an Android view that is composed using a GL texture.</summary>
/// <remarks>
/// Flutter's <c>SurfaceAndroidViewController</c>. This controller is created from the
/// <see cref="PlatformViewsService.InitSurfaceAndroidView"/> factory, and is defined for backward
/// compatibility.
/// </remarks>
public class SurfaceAndroidViewController : AndroidViewController
{
    private AndroidViewControllerInternals _internals = new TextureAndroidViewControllerInternals();

    internal SurfaceAndroidViewController(
        int viewId,
        string viewType,
        TextDirection layoutDirection,
        object? creationParams = null,
        IMessageCodec? creationParamsCodec = null)
        : base(viewId, viewType, layoutDirection, creationParams, creationParamsCodec)
    {
    }

    private protected override bool CreateRequiresSize => true;

    private protected override async Task SendCreateMessage(Size? size, Point? position = null)
    {
        Debug.Assert(
            !size!.Value.IsEmpty,
            $"trying to create {nameof(TextureAndroidViewController)} without setting a valid size.");

        object? response = await AndroidViewControllerInternals.SendCreateMessage(
            viewId: ViewId,
            viewType: ViewType,
            hybrid: false,
            hybridFallback: true,
            layoutDirection: LayoutDirectionValue,
            creationParams: CreationParamsValue,
            size: size,
            position: position);
        if (response is int textureId)
        {
            ((TextureAndroidViewControllerInternals)_internals).TextureIdValue = textureId;
        }
        else
        {
            // A null response indicates fallback to Hybrid Composition, so swap out the implementation.
            _internals = new HybridAndroidViewControllerInternals();
        }
    }

    /// <inheritdoc />
    public override int? TextureId => _internals.TextureId;

    /// <inheritdoc />
    public override bool RequiresViewComposition => _internals.RequiresViewComposition;

    private protected override Task SendDisposeMessage() => _internals.SendDisposeMessage(viewId: ViewId);

    private protected override Task<Size> SendResizeMessage(Size size) =>
        _internals.SetSize(size, viewId: ViewId, viewState: State);

    /// <inheritdoc />
    public override Task SetOffset(Point off) => _internals.SetOffset(off, viewId: ViewId, viewState: State);
}

/// <summary>Controls an Android view that is composed using the Android view hierarchy.</summary>
/// <remarks>
/// Flutter's <c>ExpensiveAndroidViewController</c>. This controller is created from the
/// <see cref="PlatformViewsService.InitExpensiveAndroidView"/> factory.
/// </remarks>
public class ExpensiveAndroidViewController : AndroidViewController
{
    private readonly AndroidViewControllerInternals _internals = new HybridAndroidViewControllerInternals();

    internal ExpensiveAndroidViewController(
        int viewId,
        string viewType,
        TextDirection layoutDirection,
        object? creationParams = null,
        IMessageCodec? creationParamsCodec = null)
        : base(viewId, viewType, layoutDirection, creationParams, creationParamsCodec)
    {
    }

    private protected override bool CreateRequiresSize => false;

    private protected override async Task SendCreateMessage(Size? size, Point? position = null)
    {
        await AndroidViewControllerInternals.SendCreateMessage(
            viewId: ViewId,
            viewType: ViewType,
            hybrid: true,
            layoutDirection: LayoutDirectionValue,
            creationParams: CreationParamsValue,
            position: position);
    }

    /// <inheritdoc />
    public override int? TextureId => _internals.TextureId;

    /// <inheritdoc />
    public override bool RequiresViewComposition => _internals.RequiresViewComposition;

    private protected override Task SendDisposeMessage() => _internals.SendDisposeMessage(viewId: ViewId);

    private protected override Task<Size> SendResizeMessage(Size size) =>
        _internals.SetSize(size, viewId: ViewId, viewState: State);

    /// <inheritdoc />
    public override Task SetOffset(Point off) => _internals.SetOffset(off, viewId: ViewId, viewState: State);
}

/// <summary>Controls an Android view that is composed using Hybrid Composition++ (HCPP).</summary>
/// <remarks>
/// Flutter's <c>HybridAndroidViewController</c>. This controller is created from the
/// <see cref="PlatformViewsService.InitHybridAndroidView"/> factory.
/// </remarks>
public class HybridAndroidViewController : AndroidViewController
{
    private readonly AndroidViewControllerInternals _internals = new Hybrid2AndroidViewControllerInternals();

    internal HybridAndroidViewController(
        int viewId,
        string viewType,
        TextDirection layoutDirection,
        object? creationParams = null,
        IMessageCodec? creationParamsCodec = null)
        : base(viewId, viewType, layoutDirection, creationParams, creationParamsCodec)
    {
    }

    /// <summary>Perform a runtime check to determine if HCPP mode is supported on the current
    /// device.</summary>
    public static Task<bool> CheckIfSupported() =>
        Hybrid2AndroidViewControllerInternals.CheckIfSurfaceControlEnabled();

    private protected override bool CreateRequiresSize => false;

    private protected override async Task SendCreateMessage(Size? size, Point? position = null)
    {
        await AndroidViewControllerInternals.SendCreateMessage(
            viewId: ViewId,
            viewType: ViewType,
            hybrid: true,
            layoutDirection: LayoutDirectionValue,
            creationParams: CreationParamsValue,
            position: position,
            useNewController: true);
    }

    /// <inheritdoc />
    public override int? TextureId => _internals.TextureId;

    /// <inheritdoc />
    public override bool RequiresViewComposition => _internals.RequiresViewComposition;

    private protected override Task SendDisposeMessage() => _internals.SendDisposeMessage(viewId: ViewId);

    private protected override Task<Size> SendResizeMessage(Size size) =>
        _internals.SetSize(size, viewId: ViewId, viewState: State);

    /// <inheritdoc />
    public override Task SetOffset(Point off) => _internals.SetOffset(off, viewId: ViewId, viewState: State);

    /// <inheritdoc />
    public override async Task SendMotionEvent(AndroidMotionEvent @event)
    {
        await SystemChannels.PlatformViews2.InvokeMethod<object>("touch", @event.AsList(ViewId));
    }
}

/// <summary>Controls an Android view that is rendered as a texture.</summary>
/// <remarks>
/// Flutter's <c>TextureAndroidViewController</c>. This is typically used by <c>AndroidView</c> to
/// display a View in the Android view hierarchy. The platform view is created by calling
/// <see cref="AndroidViewController.Create"/> with an initial size. The controller is typically
/// created with <see cref="PlatformViewsService.InitAndroidView"/>.
/// </remarks>
public class TextureAndroidViewController : AndroidViewController
{
    private AndroidViewControllerInternals _internals = new TextureAndroidViewControllerInternals();

    internal TextureAndroidViewController(
        int viewId,
        string viewType,
        TextDirection layoutDirection,
        object? creationParams = null,
        IMessageCodec? creationParamsCodec = null)
        : base(viewId, viewType, layoutDirection, creationParams, creationParamsCodec)
    {
    }

    private protected override bool CreateRequiresSize => true;

    private protected override async Task SendCreateMessage(Size? size, Point? position = null)
    {
        Debug.Assert(
            !size!.Value.IsEmpty,
            $"trying to create {nameof(TextureAndroidViewController)} without setting a valid size.");

        object? response = await AndroidViewControllerInternals.SendCreateMessage(
            viewId: ViewId,
            viewType: ViewType,
            hybrid: false,
            layoutDirection: LayoutDirectionValue,
            creationParams: CreationParamsValue,
            size: size,
            position: position);
        if (response is int textureId)
        {
            ((TextureAndroidViewControllerInternals)_internals).TextureIdValue = textureId;
        }
        else
        {
            // A null response indicates fallback to Hybrid Composition++, so swap out the
            // implementation.
            _internals = new Hybrid2AndroidViewControllerInternals();
        }
    }

    /// <inheritdoc />
    public override int? TextureId => _internals.RequiresViewComposition ? null : _internals.TextureId;

    /// <inheritdoc />
    public override bool RequiresViewComposition => _internals.RequiresViewComposition;

    private protected override Task SendDisposeMessage() => _internals.SendDisposeMessage(viewId: ViewId);

    private protected override Task<Size> SendResizeMessage(Size size)
    {
        if (RequiresViewComposition)
        {
            return Task.FromResult(size);
        }

        return _internals.SetSize(size, viewId: ViewId, viewState: State);
    }

    /// <inheritdoc />
    public override Task SetOffset(Point off)
    {
        if (RequiresViewComposition)
        {
            return Task.CompletedTask;
        }

        return _internals.SetOffset(off, viewId: ViewId, viewState: State);
    }
}

// The base class for an implementation of AndroidViewController.
//
// Subclasses should correspond to different rendering modes for platform views, and contain
// mode-specific logic.
internal abstract class AndroidViewControllerInternals
{
    // Sends a create message with the given parameters, and returns the result if any.
    //
    // This uses a dynamic return because depending on the mode that is selected, the native side
    // will return different types.
    internal static Task<object?> SendCreateMessage(
        int viewId,
        string viewType,
        TextDirection layoutDirection,
        bool hybrid,
        bool hybridFallback = false,
        bool useNewController = false,
        CreationParams? creationParams = null,
        Size? size = null,
        Point? position = null)
    {
        var args = new Dictionary<string, object?>
        {
            ["id"] = viewId,
            ["viewType"] = viewType,
            ["direction"] = layoutDirection == TextDirection.Rtl
                ? AndroidViewController.AndroidLayoutDirectionRtl
                : AndroidViewController.AndroidLayoutDirectionLtr,
        };
        if (hybrid)
        {
            args["hybrid"] = hybrid;
        }

        if (size is { } sizeValue)
        {
            args["width"] = sizeValue.Width;
            args["height"] = sizeValue.Height;
        }

        if (hybridFallback)
        {
            args["hybridFallback"] = hybridFallback;
        }

        if (position is { } positionValue)
        {
            args["left"] = positionValue.X;
            args["top"] = positionValue.Y;
        }

        if (creationParams is not null)
        {
            args["params"] = PlatformViewsService.EncodeCreationParams(creationParams.Codec, creationParams.Data);
        }

        if (useNewController)
        {
            return SystemChannels.PlatformViews2.InvokeMethod<object>("create", args);
        }

        return SystemChannels.PlatformViews.InvokeMethod<object>("create", args);
    }

    internal abstract int? TextureId { get; }

    internal abstract bool RequiresViewComposition { get; }

    internal abstract Task<Size> SetSize(Size size, int viewId, AndroidPlatformViewState viewState);

    internal abstract Task SetOffset(Point offset, int viewId, AndroidPlatformViewState viewState);

    internal abstract Task SendDisposeMessage(int viewId);
}

// An AndroidViewController implementation for views whose contents are displayed via a texture rather
// than directly in a native view.
//
// This is used for both Virtual Display and Texture Layer Hybrid Composition.
internal sealed class TextureAndroidViewControllerInternals : AndroidViewControllerInternals
{
    // The current offset of the platform view.
    private Point _offset = default;

    internal int? TextureIdValue { get; set; }

    internal override int? TextureId => TextureIdValue;

    internal override bool RequiresViewComposition => false;

    internal override async Task<Size> SetSize(Size size, int viewId, AndroidPlatformViewState viewState)
    {
        Debug.Assert(
            viewState != AndroidPlatformViewState.WaitingForSize,
            $"Android view must have an initial size. View id: {viewId}");
        Debug.Assert(!size.IsEmpty);

        Dictionary<object, object?>? meta = await SystemChannels.PlatformViews.InvokeMapMethod<object, object?>(
            "resize",
            new Dictionary<string, object?>
            {
                ["id"] = viewId,
                ["width"] = size.Width,
                ["height"] = size.Height,
            });
        Debug.Assert(meta is not null);
        Debug.Assert(meta!.ContainsKey("width"));
        Debug.Assert(meta.ContainsKey("height"));
        return new Size((double)meta["width"]!, (double)meta["height"]!);
    }

    internal override async Task SetOffset(Point offset, int viewId, AndroidPlatformViewState viewState)
    {
        if (offset == _offset)
        {
            return;
        }

        // Don't set the offset unless the Android view has been created.
        // The implementation of this method channel throws if the Android view for this viewId
        // isn't addressable.
        if (viewState != AndroidPlatformViewState.Created)
        {
            return;
        }

        _offset = offset;

        await SystemChannels.PlatformViews.InvokeMethod<object>(
            "offset",
            new Dictionary<string, object?>
            {
                ["id"] = viewId,
                ["top"] = offset.Y,
                ["left"] = offset.X,
            });
    }

    internal override Task SendDisposeMessage(int viewId)
    {
        return SystemChannels.PlatformViews.InvokeMethod<object>(
            "dispose",
            new Dictionary<string, object?> { ["id"] = viewId, ["hybrid"] = false });
    }
}

// An AndroidViewController implementation for views whose contents require composition of native
// views.
//
// This is used for Hybrid Composition.
internal sealed class HybridAndroidViewControllerInternals : AndroidViewControllerInternals
{
    internal override int? TextureId =>
        throw new NotImplementedException("Not supported for hybrid composition.");

    internal override bool RequiresViewComposition => true;

    internal override Task<Size> SetSize(Size size, int viewId, AndroidPlatformViewState viewState)
    {
        throw new NotImplementedException("Not supported for hybrid composition.");
    }

    internal override Task SetOffset(Point offset, int viewId, AndroidPlatformViewState viewState)
    {
        throw new NotImplementedException("Not supported for hybrid composition.");
    }

    internal override Task SendDisposeMessage(int viewId)
    {
        return SystemChannels.PlatformViews.InvokeMethod<object>(
            "dispose",
            new Dictionary<string, object?> { ["id"] = viewId, ["hybrid"] = true });
    }
}

// An AndroidViewController implementation for Hybrid Composition++ (HCPP).
internal sealed class Hybrid2AndroidViewControllerInternals : AndroidViewControllerInternals
{
    internal override int? TextureId =>
        throw new NotImplementedException("Not supported for hybrid composition.");

    internal override bool RequiresViewComposition => true;

    internal override Task<Size> SetSize(Size size, int viewId, AndroidPlatformViewState viewState)
    {
        throw new NotImplementedException("Not supported for hybrid composition.");
    }

    internal override Task SetOffset(Point offset, int viewId, AndroidPlatformViewState viewState)
    {
        throw new NotImplementedException("Not supported for hybrid composition.");
    }

    internal override Task SendDisposeMessage(int viewId)
    {
        return SystemChannels.PlatformViews2.InvokeMethod<object>(
            "dispose",
            new Dictionary<string, object?> { ["id"] = viewId, ["hybrid"] = true });
    }

    internal static async Task<bool> CheckIfSurfaceControlEnabled()
    {
        return (await SystemChannels.PlatformViews2.InvokeMethod<object>(
            "isSurfaceControlEnabled",
            new Dictionary<string, object?>()) as bool?)!.Value;
    }
}

/// <summary>Base class for iOS and macOS view controllers.</summary>
/// <remarks>
/// Flutter's <c>DarwinPlatformViewController</c>. View controllers are used to create and interact
/// with the UIView or NSView underlying a platform view.
/// </remarks>
public abstract class DarwinPlatformViewController
{
    private bool _debugDisposed;

    /// <summary>Public default for subclasses to override.</summary>
    protected DarwinPlatformViewController(int id, TextDirection layoutDirection)
    {
        Id = id;
        LayoutDirection = layoutDirection;
    }

    /// <summary>The unique identifier of the iOS view controlled by this controller.</summary>
    /// <remarks>This identifier is typically generated by
    /// <see cref="PlatformViewsRegistry.GetNextPlatformViewId"/>.</remarks>
    public int Id { get; }

    internal TextDirection LayoutDirection { get; private set; }

    /// <summary>Sets the layout direction for the iOS UIView.</summary>
    public virtual Task SetLayoutDirection(TextDirection layoutDirection)
    {
        Debug.Assert(
            !_debugDisposed,
            $"trying to set a layout direction for a disposed iOS UIView. View id: {Id}");

        if (layoutDirection == LayoutDirection)
        {
            return Task.CompletedTask;
        }

        LayoutDirection = layoutDirection;

        // TODO(amirh): invoke the iOS platform views channel direction method once available.
        return Task.CompletedTask;
    }

    /// <summary>Accept an active gesture.</summary>
    /// <remarks>
    /// When a touch sequence is happening on the embedded UIView all touch events are delayed. Calling
    /// <see cref="AcceptGesture"/> will release the delayed events to the embedded UIView and makes it
    /// consume any following touch events for the pointers involved in the active gesture.
    /// </remarks>
    public virtual Task AcceptGesture()
    {
        return SystemChannels.PlatformViews.InvokeMethod<object>(
            "acceptGesture",
            new Dictionary<string, object?> { ["id"] = Id });
    }

    /// <summary>Rejects an active gesture.</summary>
    /// <remarks>
    /// When a touch sequence is happening on the embedded UIView all touch events are delayed. Calling
    /// <see cref="RejectGesture"/> will drop the buffered touch events and prevent any future touch
    /// events for the pointers that are part of the active touch sequence from arriving to the embedded
    /// view.
    /// </remarks>
    public virtual Task RejectGesture()
    {
        return SystemChannels.PlatformViews.InvokeMethod<object>(
            "rejectGesture",
            new Dictionary<string, object?> { ["id"] = Id });
    }

    /// <summary>Disposes the view.</summary>
    /// <remarks>
    /// The <see cref="UiKitViewController"/> object is unusable after calling this. The <c>id</c> of
    /// the platform view cannot be reused after the view is disposed.
    /// </remarks>
    public virtual async Task Dispose()
    {
        _debugDisposed = true;
        await SystemChannels.PlatformViews.InvokeMethod<object>("dispose", Id);
        PlatformViewsService.Instance.FocusCallbacks.Remove(Id);
    }
}

/// <summary>How an embedded UIView should treat the touches it receives while a gesture is
/// ambiguous.</summary>
/// <remarks>Flutter's <c>UiKitViewGestureBlockingPolicy</c>.</remarks>
public enum UiKitViewGestureBlockingPolicy
{
    /// <summary>Flutter blocks all gestures on the embedded UIView as soon as the touch begins.</summary>
    Eager,

    /// <summary>Flutter only blocks the gestures when the touches end.</summary>
    WaitUntilTouchesEnded,

    /// <summary>Flutter does not block any gestures on the UIView, so it receives every touch.</summary>
    DoNotBlockGesture,

    /// <summary>Flutter uses the plugin default (either <see cref="Eager"/> or
    /// <see cref="WaitUntilTouchesEnded"/>).</summary>
    FallbackToPluginDefault,
}

/// <summary>Controller for an iOS platform view.</summary>
/// <remarks>Flutter's <c>UiKitViewController</c>. View controllers create and interact with the
/// underlying UIView. Typically created with <see cref="PlatformViewsService.InitUiKitView"/>.</remarks>
public class UiKitViewController : DarwinPlatformViewController
{
    internal UiKitViewController(int id, TextDirection layoutDirection)
        : base(id, layoutDirection)
    {
    }
}

/// <summary>Controller for a macOS platform view.</summary>
/// <remarks>Flutter's <c>AppKitViewController</c>. Typically created with
/// <see cref="PlatformViewsService.InitAppKitView"/>.</remarks>
public class AppKitViewController : DarwinPlatformViewController
{
    internal AppKitViewController(int id, TextDirection layoutDirection)
        : base(id, layoutDirection)
    {
    }
}

/// <summary>An interface for controlling a single platform view.</summary>
/// <remarks>
/// Flutter's <c>PlatformViewController</c>. Used by <c>PlatformViewSurface</c> to interface with the
/// platform view it embeds.
/// </remarks>
public abstract class PlatformViewController
{
    /// <summary>The viewId associated with this controller.</summary>
    /// <remarks>
    /// The viewId should always be unique and non-negative. See also
    /// <see cref="PlatformViewsRegistry"/>, which is a helper for managing platform view ids.
    /// </remarks>
    public abstract int ViewId { get; }

    /// <summary>True if <see cref="Create"/> has not been successfully called the platform view.</summary>
    /// <remarks>This can indicate either that <see cref="Create"/> was never called, or that
    /// <see cref="Create"/> was deferred for implementation-specific reasons.</remarks>
    public virtual bool AwaitingCreation => false;

    /// <summary>Dispatches the <paramref name="event"/> to the platform view.</summary>
    public abstract Task DispatchPointerEvent(PointerEvent @event);

    /// <summary>Creates the platform view with the initial <paramref name="size"/>.</summary>
    /// <remarks><paramref name="size"/> is the view's initial size in logical pixel.
    /// <paramref name="position"/> is the view's initial position in logical pixels.</remarks>
    public virtual Task Create(Size? size = null, Point? position = null) => Task.CompletedTask;

    /// <summary>Disposes the platform view.</summary>
    /// <remarks>The <see cref="PlatformViewController"/> is unusable after calling dispose.</remarks>
    public abstract Task Dispose();

    /// <summary>Clears the view's focus on the platform side.</summary>
    public abstract Task ClearFocus();
}
