// Dart parity source: flutter/packages/flutter/lib/src/services/hardware_keyboard.dart

using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.UI;

/// <summary>
/// The mode in which information of key messages is delivered.
/// </summary>
/// <remarks>
/// This enum is deprecated and will be removed. There is no direct substitute planned, since this
/// enum will no longer be necessary once <see cref="RawKeyEvent"/> and associated APIs are removed.
/// </remarks>
[Obsolete("No longer supported. Transit mode is always key data only. "
          + "Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public enum KeyDataTransitMode
{
    /// <summary>Key event information is delivered as raw key data.</summary>
    RawKeyData,

    /// <summary>Key event information is delivered as converted key data, followed by raw key data.</summary>
    KeyDataThenRawKeyData,
}

/// <summary>
/// The assembled information converted from a native key message.
/// </summary>
[Obsolete("No longer supported. Once RawKeyEvent is removed, it will no longer be needed. "
          + "Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public sealed class KeyMessage
{
    /// <summary>Create a <see cref="KeyMessage"/> by specifying all information.</summary>
    public KeyMessage(IReadOnlyList<KeyEvent> events, RawKeyEvent? rawEvent)
    {
        Events = events;
        RawEvent = rawEvent;
    }

    /// <summary>The list of <see cref="KeyEvent"/>s converted from the native key message.</summary>
    /// <remarks>A native key message is converted into multiple <see cref="KeyEvent"/>s in a regular
    /// format. Usually there is exactly one event; it can be empty, or contain synthesized events.</remarks>
    public IReadOnlyList<KeyEvent> Events { get; }

    /// <summary>The native key message in the form of a raw key event.</summary>
    /// <remarks>It is null if the platform does not use raw key events, or for solitary synthesized
    /// events.</remarks>
    public RawKeyEvent? RawEvent { get; }

    public override string ToString() => $"KeyMessage([{string.Join(", ", Events)}])";
}

/// <summary>
/// The signature for <see cref="KeyEventManager.KeyMessageHandler"/>.
/// </summary>
/// <remarks>The parameter is the converted key message; the return value indicates whether the
/// message is handled.</remarks>
[Obsolete("No longer supported. Once KeyEvent is the only event type, it will no longer be needed. "
          + "Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public delegate bool KeyMessageHandler(KeyMessage message);

/// <summary>
/// A singleton class that processes key messages from the platform and dispatches converted messages
/// accordingly.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="KeyEventManager"/> receives platform key messages by <see cref="HandleKeyData"/> and
/// <see cref="HandleRawKeyMessage"/>, sends converted events to <see cref="HardwareKeyboard"/> and
/// <see cref="RawKeyboard"/> for recording keeping, and then dispatches the <see cref="KeyMessage"/> to
/// <see cref="KeyMessageHandler"/>, the global message handler.
/// </para>
/// <para>
/// The engine sends key data in one of two modes, decided by the first message: <c>rawKeyData</c>
/// (only raw key messages, converted here into <see cref="KeyEvent"/>s) or
/// <c>keyDataThenRawKeyData</c> (each native event as zero or more <see cref="KeyData"/>s, followed
/// by one raw key message that ends the native event).
/// </para>
/// <para>
/// Dart's binding owns the instance (<c>ServicesBinding.keyEventManager</c>); Plumix's keyboards are
/// process singletons, so the manager is one too, reached through <see cref="Instance"/> or
/// <see cref="ServicesBinding.KeyEventManager"/>.
/// </para>
/// </remarks>
[Obsolete("No longer supported. Once RawKeyEvent is removed, will no longer be needed. "
          + "Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public sealed class KeyEventManager
{
    private readonly HardwareKeyboard _hardwareKeyboard;
    private readonly RawKeyboard _rawKeyboard;

    // The [KeyDataTransitMode] of the current platform.
    //
    // The `_transitMode` is guaranteed to be non-null during key event callbacks.
    //
    // The `_transitMode` is null before the first event, after which it is inferred
    // and will not change throughout the lifecycle of the application.
    //
    // The `_transitMode` can be reset to null in tests using [clearState].
    private KeyDataTransitMode? _transitMode;

    // The accumulated [KeyEvent]s since the last time the message is dispatched.
    //
    // If _transitMode is [KeyDataTransitMode.keyDataThenRawKeyData], then [KeyEvent]s
    // are directly received from [handleKeyData]. If _transitMode is
    // [KeyDataTransitMode.rawKeyData], then [KeyEvent]s are converted from the raw
    // key data of [handleRawKeyMessage]. Either way, the accumulated key
    // events are only dispatched along with the next [KeyMessage] when a
    // dispatchable [RawKeyEvent] is available.
    private readonly List<KeyEvent> _keyEventsSinceLastMessage = [];

    // When a RawKeyDownEvent is skipped ([RawKeyEventData.shouldDispatchEvent]
    // is false), its physical key will be recorded here, so that its up event
    // can also be properly skipped.
    private readonly HashSet<PhysicalKeyboardKey> _skippedRawKeysPressed = [];

    /// <summary>Create an instance.</summary>
    /// <remarks>This is typically only called by <see cref="ServicesBinding"/>.</remarks>
    public KeyEventManager(HardwareKeyboard hardwareKeyboard, RawKeyboard rawKeyboard)
    {
        _hardwareKeyboard = hardwareKeyboard ?? throw new ArgumentNullException(nameof(hardwareKeyboard));
        _rawKeyboard = rawKeyboard ?? throw new ArgumentNullException(nameof(rawKeyboard));
    }

    /// <summary>The process-wide manager, Dart's <c>ServicesBinding.instance.keyEventManager</c>.</summary>
    public static KeyEventManager Instance { get; } = new(HardwareKeyboard.Instance, RawKeyboard.Instance);

    /// <summary>The global entrance which handles all key events sent to Flutter.</summary>
    /// <remarks>
    /// Typical applications use <c>WidgetsBinding</c>, where this field is set by the focus system
    /// (see <c>FocusManager</c>) on startup to a function that dispatches incoming events to the
    /// focus system, including <c>FocusNode.OnKeyEvent</c>, <c>FocusNode.OnKey</c>, and
    /// <c>Shortcuts</c>. In this case, the application does not need to read, assign, or invoke
    /// this value.
    /// </remarks>
    public KeyMessageHandler? KeyMessageHandler { get; set; }

    /// <summary>Dispatch a key data to global and leaf listeners.</summary>
    /// <remarks>This method is the handler to the global <c>onKeyData</c> API.</remarks>
    public bool HandleKeyData(KeyData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _transitMode ??= KeyDataTransitMode.KeyDataThenRawKeyData;
        switch (_transitMode)
        {
            case KeyDataTransitMode.RawKeyData:
                DebugAssertions.Assert(false, "Should never encounter KeyData when transitMode is rawKeyData.");
                return false;
            case KeyDataTransitMode.KeyDataThenRawKeyData:
                // Having 0 as the physical and logical ID indicates an empty key data
                // (the only occasion either field can be 0,) transmitted to ensure
                // that the transit mode is correctly inferred. These events should be
                // ignored.
                if (data.Physical == 0 && data.Logical == 0)
                {
                    return false;
                }

                DebugAssertions.Assert(data.Physical != 0 && data.Logical != 0);
                KeyEvent @event = EventFromData(data);
                if (data.Synthesized && _keyEventsSinceLastMessage.Count == 0)
                {
                    // Dispatch the event instantly if both conditions are met:
                    //
                    // - The event is synthesized, therefore the result does not matter.
                    // - The current queue is empty, therefore the order does not matter.
                    //
                    // This allows solitary synthesized `KeyEvent`s to be dispatched,
                    // since they won't be followed by `RawKeyEvent`s.
                    _hardwareKeyboard.HandleKeyEvent(@event);
                    DispatchKeyMessage([@event], null);
                }
                else
                {
                    // Otherwise, postpone key event dispatching until the next raw
                    // event. Normal key presses always send 0 or more `KeyEvent`s first,
                    // then 1 `RawKeyEvent`.
                    _keyEventsSinceLastMessage.Add(@event);
                }

                return false;
        }

        return false;
    }

    private bool DispatchKeyMessage(List<KeyEvent> keyEvents, RawKeyEvent? rawEvent)
    {
        if (KeyMessageHandler != null)
        {
            var message = new KeyMessage(keyEvents, rawEvent);
            try
            {
                return KeyMessageHandler(message);
            }
            catch (Exception exception)
            {
                InformationCollector? collector = null;
                if (Constants.KDebugMode)
                {
                    collector = () => [new DiagnosticsProperty<KeyMessage>("KeyMessage", message)];
                }

                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    stack: exception.StackTrace,
                    library: "services library",
                    context: new ErrorDescription("while processing the key message handler"),
                    informationCollector: collector));
            }
        }

        return false;
    }

    /// <summary>Handles a raw key message.</summary>
    /// <remarks>This method is the handler to <see cref="SystemChannels.KeyEvent"/>, processing the
    /// JSON form of the native key message and returns the responds for the channel.</remarks>
    public Task<object?> HandleRawKeyMessage(object? message)
    {
        if (_transitMode == null)
        {
            _transitMode = KeyDataTransitMode.RawKeyData;
            // Convert raw events using a listener so that conversion only occurs if
            // the raw event should be dispatched.
            _rawKeyboard.AddListener(ConvertRawEventAndStore);
        }

        RawKeyEvent rawEvent = RawKeyEvent.FromMessage((IReadOnlyDictionary<string, object?>)message!);

        bool shouldDispatch = true;
        if (rawEvent is RawKeyDownEvent)
        {
            if (!rawEvent.Data.ShouldDispatchEvent())
            {
                shouldDispatch = false;
                _skippedRawKeysPressed.Add(rawEvent.PhysicalKey);
            }
            else
            {
                _skippedRawKeysPressed.Remove(rawEvent.PhysicalKey);
            }
        }
        else if (rawEvent is RawKeyUpEvent)
        {
            if (_skippedRawKeysPressed.Contains(rawEvent.PhysicalKey))
            {
                _skippedRawKeysPressed.Remove(rawEvent.PhysicalKey);
                shouldDispatch = false;
            }
        }

        bool handled = true;
        if (shouldDispatch)
        {
            // The following `handleRawKeyEvent` will call `_convertRawEventAndStore`
            // unless the event is not dispatched.
            handled = _rawKeyboard.HandleRawKeyEvent(rawEvent);

            foreach (KeyEvent @event in _keyEventsSinceLastMessage)
            {
                handled = _hardwareKeyboard.HandleKeyEvent(@event) || handled;
            }

            if (_transitMode == KeyDataTransitMode.RawKeyData)
            {
                DebugAssertions.Assert(
                    _rawKeyboard.PhysicalKeysPressed.SetEquals(_hardwareKeyboard.PhysicalKeysPressed),
                    $"RawKeyboard reported {DescribeSet(_rawKeyboard.PhysicalKeysPressedInOrder)}, "
                    + $"while HardwareKeyboard reported {DescribeSet(_hardwareKeyboard.PhysicalKeysPressedInOrder)}");
            }

            handled = DispatchKeyMessage(_keyEventsSinceLastMessage, rawEvent) || handled;
            _keyEventsSinceLastMessage.Clear();
        }

        return Task.FromResult<object?>(new Dictionary<string, object?> { ["handled"] = handled });
    }

    // Convert the raw event to key events, including synthesizing events for
    // modifiers, and store the key events in `_keyEventsSinceLastMessage`.
    //
    // This is only called when `_transitMode` is `rawKeyEvent` and if the raw
    // event should be dispatched.
    private void ConvertRawEventAndStore(RawKeyEvent rawEvent)
    {
        PhysicalKeyboardKey physicalKey = rawEvent.PhysicalKey;
        LogicalKeyboardKey logicalKey = rawEvent.LogicalKey;
        // The physical keys that are pressed before the raw event, in Dart's insertion order.
        var physicalKeysPressed = new List<PhysicalKeyboardKey>(_hardwareKeyboard.PhysicalKeysPressedInOrder);
        var eventAfterwards = new List<KeyEvent>();
        LogicalKeyboardKey? recordedLogicalMain = _hardwareKeyboard.LookUpLayout(physicalKey);
        TimeSpan timeStamp = SchedulerBinding.Instance.CurrentSystemFrameTimeStamp;
        string? character = rawEvent.Character == string.Empty ? null : rawEvent.Character;
        KeyEventDeviceType deviceType = ConvertDeviceType(rawEvent);
        KeyEvent? mainEvent;
        if (rawEvent is RawKeyDownEvent)
        {
            if (recordedLogicalMain == null)
            {
                mainEvent = new KeyDownEvent(
                    physicalKey,
                    logicalKey,
                    timeStamp,
                    character,
                    deviceType);
                physicalKeysPressed.Add(physicalKey);
            }
            else
            {
                DebugAssertions.Assert(physicalKeysPressed.Contains(physicalKey));
                mainEvent = new KeyRepeatEvent(
                    physicalKey,
                    recordedLogicalMain,
                    timeStamp,
                    character,
                    deviceType);
            }
        }
        else
        {
            DebugAssertions.Assert(
                rawEvent is RawKeyUpEvent,
                $"Unexpected subclass of RawKeyEvent: {rawEvent.GetType().Name}");
            if (recordedLogicalMain == null)
            {
                mainEvent = null;
            }
            else
            {
                mainEvent = new KeyUpEvent(
                    physicalKey,
                    recordedLogicalMain,
                    timeStamp,
                    deviceType);
                physicalKeysPressed.Remove(physicalKey);
            }
        }

        IReadOnlySet<PhysicalKeyboardKey> rawPhysicalKeysPressed = _rawKeyboard.PhysicalKeysPressed;
        foreach (PhysicalKeyboardKey key in physicalKeysPressed.Where(key => !rawPhysicalKeysPressed.Contains(key)))
        {
            if (key.Equals(physicalKey))
            {
                // Somehow, a down event is dispatched but the key is absent from
                // keysPressed. Synthesize a up event for the key, but this event must
                // be added after the main key down event.
                eventAfterwards.Add(new KeyUpEvent(
                    key,
                    logicalKey,
                    timeStamp,
                    deviceType,
                    synthesized: true));
            }
            else
            {
                _keyEventsSinceLastMessage.Add(new KeyUpEvent(
                    key,
                    _hardwareKeyboard.LookUpLayout(key)!,
                    timeStamp,
                    deviceType,
                    synthesized: true));
            }
        }

        foreach (PhysicalKeyboardKey key in _rawKeyboard.PhysicalKeysPressedInOrder
                     .Where(key => !physicalKeysPressed.Contains(key)))
        {
            _keyEventsSinceLastMessage.Add(new KeyDownEvent(
                key,
                _rawKeyboard.LookUpLayout(key)!,
                timeStamp,
                character: null,
                deviceType,
                synthesized: true));
        }

        if (mainEvent != null)
        {
            _keyEventsSinceLastMessage.Add(mainEvent);
        }

        _keyEventsSinceLastMessage.AddRange(eventAfterwards);
    }

    private static KeyEventDeviceType ConvertDeviceType(RawKeyEvent rawEvent)
    {
        RawKeyEventData data = rawEvent.Data;
        // Device type is only available from Android.
        if (data is not RawKeyEventDataAndroid android)
        {
            return KeyEventDeviceType.Keyboard;
        }

        return android.EventSource switch
        {
            // https://developer.android.com/reference/android/view/InputDevice#SOURCE_KEYBOARD
            0x00000101 => KeyEventDeviceType.Keyboard,
            // https://developer.android.com/reference/android/view/InputDevice#SOURCE_DPAD
            0x00000201 => KeyEventDeviceType.DirectionalPad,
            // https://developer.android.com/reference/android/view/InputDevice#SOURCE_GAMEPAD
            0x00000401 => KeyEventDeviceType.Gamepad,
            // https://developer.android.com/reference/android/view/InputDevice#SOURCE_JOYSTICK
            0x01000010 => KeyEventDeviceType.Joystick,
            // https://developer.android.com/reference/android/view/InputDevice#SOURCE_HDMI
            0x02000001 => KeyEventDeviceType.Hdmi,
            _ => KeyEventDeviceType.Keyboard,
        };
    }

    /// <summary>Reset the inferred platform transit mode and related states.</summary>
    /// <remarks>
    /// This method is only used in unit tests. In release mode, this is a no-op.
    /// </remarks>
    public void ClearState()
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        _transitMode = null;
        _rawKeyboard.RemoveListener(ConvertRawEventAndStore);
        _keyEventsSinceLastMessage.Clear();
    }

    private static KeyEvent EventFromData(KeyData keyData)
    {
        PhysicalKeyboardKey physicalKey = PhysicalKeyboardKey.FindKeyByCode(keyData.Physical)
                                          ?? new PhysicalKeyboardKey(keyData.Physical);
        LogicalKeyboardKey logicalKey = LogicalKeyboardKey.FindKeyByKeyId(keyData.Logical)
                                        ?? new LogicalKeyboardKey(keyData.Logical);
        TimeSpan timeStamp = keyData.TimeStamp;
        switch (keyData.Type)
        {
            case KeyEventType.Down:
                return new KeyDownEvent(
                    physicalKey,
                    logicalKey,
                    timeStamp,
                    keyData.Character,
                    keyData.DeviceType,
                    keyData.Synthesized);
            case KeyEventType.Up:
                DebugAssertions.Assert(keyData.Character == null);
                return new KeyUpEvent(
                    physicalKey,
                    logicalKey,
                    timeStamp,
                    keyData.DeviceType,
                    keyData.Synthesized);
            case KeyEventType.Repeat:
                return new KeyRepeatEvent(
                    physicalKey,
                    logicalKey,
                    timeStamp,
                    keyData.Character,
                    keyData.DeviceType);
            default:
                throw new ArgumentOutOfRangeException(nameof(keyData));
        }
    }

    // Dart's `Set.toString`: `{a, b}`.
    private static string DescribeSet(IEnumerable<PhysicalKeyboardKey> keys) => $"{{{string.Join(", ", keys)}}}";
}
