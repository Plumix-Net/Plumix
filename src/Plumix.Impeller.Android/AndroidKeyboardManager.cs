using Android.Views;
using Plumix.Foundation;
using Plumix.UI;
using KeyEvent = Android.Views.KeyEvent;

namespace Plumix.Impeller.Android;

// C#-only host infrastructure parity sources, all under flutter/engine/src/flutter/shell/platform/android/
// io/flutter/embedding: android/KeyboardManager.java (both responders get every event; an event neither handles
// goes to text input, then back through the view tree), android/KeyEmbedderResponder.java (KeyData with modifier
// and Caps Lock synchronization against the event's meta state), android/KeyChannelResponder.java and
// engine/systemchannels/KeyEventChannel.java (the `flutter/keyevent` message), and android/KeyboardMap.java,
// whose tables equal the framework's kAndroidToPhysicalKey/kAndroidToLogicalKey (UI/KeyboardMaps.g.cs). The
// engine's responders answer asynchronously; the framework answers this host's calls synchronously.

/// <summary>Sends the view's key events to the framework, as the engine's <c>KeyboardManager</c>.</summary>
internal sealed class AndroidKeyboardManager
{
    // KeyboardMap.kValueMask and kAndroidPlane.
    private const long ValueMask = 0x000ffffffffL;
    private const long AndroidPlane = 0x01100000000L;

    // KeyCharacterMap.VIRTUAL_KEYBOARD: the device id of key events the IME or `input` injects.
    private const int VirtualKeyboard = -1;

    // KeyboardMap.pressingGoals: (meta mask, [(physical, logical)]).
    private static readonly (MetaKeyStates Mask, (long Physical, long Logical)[] Keys)[] PressingGoals =
    [
        (MetaKeyStates.CtrlOn, [(0x000700e0L, 0x0200000100L), (0x000700e4L, 0x0200000101L)]),
        (MetaKeyStates.ShiftOn, [(0x000700e1L, 0x0200000102L), (0x000700e5L, 0x0200000103L)]),
        (MetaKeyStates.AltOn, [(0x000700e2L, 0x0200000104L), (0x000700e6L, 0x0200000105L)]),
    ];

    private readonly View _view;
    private readonly Func<KeyEvent, bool> _onTextInputKeyEvent;
    private readonly HashSet<KeyEvent> _redispatchedEvents = [];
    private readonly Dictionary<long, long> _pressingRecords = [];

    // KeyboardMap.getTogglingGoals: Caps Lock, whose state the embedder responder tracks.
    private readonly TogglingGoal _capsLock = new(MetaKeyStates.CapsLockOn, 0x00070039L, 0x0100000104L);
    private readonly CharacterCombiner _embedderCombiner = new();
    private readonly CharacterCombiner _channelCombiner = new();

    public AndroidKeyboardManager(View view, Func<KeyEvent, bool> onTextInputKeyEvent)
    {
        _view = view;
        _onTextInputKeyEvent = onTextInputKeyEvent;
    }

    /// <summary>
    /// <c>KeyboardManager.handleEvent</c>: true when the framework or text input took the event, or it went back
    /// through the view tree; false only for that redispatched event, which the view then handles as Android does.
    /// </summary>
    public bool HandleEvent(KeyEvent keyEvent)
    {
        if (_redispatchedEvents.Remove(keyEvent))
        {
            return false;
        }

        bool handled;
        using (EventLoop.EnterFrameworkThread())
        {
            handled = HandleEmbedderEvent(keyEvent);
            handled |= HandleChannelEvent(keyEvent);
        }

        if (!handled)
        {
            OnUnhandled(keyEvent);
        }

        return true;
    }

    /// <summary>
    /// <c>KeyboardManager.getKeyboardState</c>: the pressed keys, physical to logical, answered on
    /// <c>flutter/keyboard</c>.
    /// </summary>
    public Dictionary<object, object?> GetKeyboardState()
    {
        var state = new Dictionary<object, object?>();
        foreach ((long physical, long logical) in _pressingRecords)
        {
            state[physical] = logical;
        }

        return state;
    }

    private void OnUnhandled(KeyEvent keyEvent)
    {
        if (_onTextInputKeyEvent(keyEvent))
        {
            return;
        }

        _redispatchedEvents.Add(keyEvent);
        _view.RootView?.DispatchKeyEvent(keyEvent);
        if (_redispatchedEvents.Remove(keyEvent))
        {
            global::Android.Util.Log.Warn(
                "KeyboardManager", "A redispatched key event was consumed before reaching KeyboardManager");
        }
    }

    // KeyChannelResponder.handleEvent and KeyEventChannel.encodeKeyEvent.
    private bool HandleChannelEvent(KeyEvent keyEvent)
    {
        if (keyEvent.Action is not (KeyEventActions.Down or KeyEventActions.Up))
        {
            return false;
        }

        char complexCharacter = _channelCombiner.Apply(keyEvent.UnicodeChar);
        var message = new Dictionary<string, object?>
        {
            ["type"] = keyEvent.Action == KeyEventActions.Down ? "keydown" : "keyup",
            ["keymap"] = "android",
            ["flags"] = (int)keyEvent.Flags,
            ["plainCodePoint"] = keyEvent.GetUnicodeChar(0),
            ["codePoint"] = keyEvent.UnicodeChar,
            ["keyCode"] = (int)keyEvent.KeyCode,
            ["scanCode"] = keyEvent.ScanCode,
            ["metaState"] = (int)keyEvent.MetaState,
            ["character"] = complexCharacter.ToString(),
            ["source"] = (int)keyEvent.Source,
            ["deviceId"] = keyEvent.DeviceId,
            ["repeatCount"] = keyEvent.RepeatCount,
        };
        bool handled = false;
        _ = ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
            SystemChannels.KeyEvent.Name,
            SystemChannels.KeyEvent.Codec.EncodeMessage(message),
            reply =>
            {
                if (SystemChannels.KeyEvent.Codec.DecodeMessage(reply) is IReadOnlyDictionary<string, object?> map
                    && map.GetValueOrDefault("handled") is bool value)
                {
                    handled = value;
                }
            });
        return handled;
    }

    // KeyEmbedderResponder.handleEvent: an event that sends nothing is answered with an empty synthesized one.
    private bool HandleEmbedderEvent(KeyEvent keyEvent)
    {
        if (HandleEmbedderEventImpl(keyEvent, out bool handled))
        {
            return handled;
        }

        SynthesizeEvent(isDown: true, logicalKey: 0, physicalKey: 0, timestamp: 0);
        return true;
    }

    private bool HandleEmbedderEventImpl(KeyEvent keyEvent, out bool handled)
    {
        handled = false;
        if (keyEvent.ScanCode == 0 && keyEvent.KeyCode == 0)
        {
            return false;
        }

        long physicalKey = PhysicalKeyOf(keyEvent);
        long logicalKey = LogicalKeyOf(keyEvent);
        List<Action> postSynchronizeEvents = [];
        foreach ((MetaKeyStates mask, (long Physical, long Logical)[] keys) in PressingGoals)
        {
            SynchronizePressingKey(
                keys, (keyEvent.MetaState & mask) != 0, logicalKey, physicalKey, keyEvent, postSynchronizeEvents);
        }

        SynchronizeTogglingKey(_capsLock, (keyEvent.MetaState & _capsLock.Mask) != 0, logicalKey, keyEvent);

        bool isDownEvent;
        switch (keyEvent.Action)
        {
            case KeyEventActions.Down:
                isDownEvent = true;
                break;
            case KeyEventActions.Up:
                isDownEvent = false;
                break;
            default:
                return false;
        }

        KeyEventType type;
        string? character = null;
        bool hasRecord = _pressingRecords.TryGetValue(physicalKey, out long lastLogicalRecord);
        if (isDownEvent)
        {
            if (!hasRecord)
            {
                type = KeyEventType.Down;
            }
            else if (keyEvent.RepeatCount > 0)
            {
                type = KeyEventType.Repeat;
            }
            else
            {
                SynthesizeEvent(isDown: false, lastLogicalRecord, physicalKey, keyEvent.EventTime);
                type = KeyEventType.Down;
            }

            char complexChar = _embedderCombiner.Apply(keyEvent.UnicodeChar);
            if (complexChar != 0)
            {
                character = complexChar.ToString();
            }
        }
        else if (!hasRecord)
        {
            return false;
        }
        else
        {
            type = KeyEventType.Up;
        }

        if (type != KeyEventType.Repeat)
        {
            UpdatePressingState(physicalKey, isDownEvent ? logicalKey : null);
        }

        if (type == KeyEventType.Down && logicalKey == _capsLock.LogicalKey)
        {
            _capsLock.Enabled = !_capsLock.Enabled;
        }

        handled = SendKeyData(new KeyData(
            Timestamp(keyEvent.EventTime),
            type,
            physicalKey,
            logicalKey,
            character,
            synthesized: false,
            DeviceTypeOf(keyEvent.Source)));
        foreach (Action postSynchronizeEvent in postSynchronizeEvents)
        {
            postSynchronizeEvent();
        }

        return true;
    }

    private static long KeyOfPlane(long key, long plane) => plane | (key & ValueMask);

    private static long PhysicalKeyOf(KeyEvent keyEvent)
    {
        int scanCode = keyEvent.ScanCode;
        if (scanCode == 0)
        {
            return KeyOfPlane((long)keyEvent.KeyCode, AndroidPlane);
        }

        return KeyboardMaps.KAndroidToPhysicalKey.TryGetValue(scanCode, out PhysicalKeyboardKey? key)
            ? key.UsbHidUsage
            : KeyOfPlane(scanCode, AndroidPlane);
    }

    private static long LogicalKeyOf(KeyEvent keyEvent) =>
        KeyboardMaps.KAndroidToLogicalKey.TryGetValue((int)keyEvent.KeyCode, out LogicalKeyboardKey? key)
            ? key.KeyId
            : KeyOfPlane((long)keyEvent.KeyCode, AndroidPlane);

    // KeyEmbedderResponder's switch on the event's source; anything else is a keyboard.
    private static KeyEventDeviceType DeviceTypeOf(InputSourceType source) => source switch
    {
        InputSourceType.Dpad => KeyEventDeviceType.DirectionalPad,
        InputSourceType.Gamepad => KeyEventDeviceType.Gamepad,
        InputSourceType.Joystick => KeyEventDeviceType.Joystick,
        InputSourceType.Hdmi => KeyEventDeviceType.Hdmi,
        _ => KeyEventDeviceType.Keyboard,
    };

    // The engine puts the event's uptime milliseconds in KeyData's timestamp, which dart:ui reads as microseconds.
    private static TimeSpan Timestamp(long eventTime) => TimeSpan.FromMicroseconds(eventTime);

    private void UpdatePressingState(long physicalKey, long? logicalKey)
    {
        if (logicalKey is { } logical)
        {
            if (!_pressingRecords.TryAdd(physicalKey, logical))
            {
                throw new InvalidOperationException("The key was not empty");
            }
        }
        else if (!_pressingRecords.Remove(physicalKey))
        {
            throw new InvalidOperationException("The key was empty");
        }
    }

    private void SynchronizePressingKey(
        (long Physical, long Logical)[] keys,
        bool truePressed,
        long eventLogicalKey,
        long eventPhysicalKey,
        KeyEvent keyEvent,
        List<Action> postSynchronize)
    {
        bool[] nowStates = new bool[keys.Length];
        bool?[] preEventStates = new bool?[keys.Length];
        bool postEventAnyPressed = false;
        long eventTime = keyEvent.EventTime;
        for (int keyIdx = 0; keyIdx < keys.Length; keyIdx++)
        {
            (long physical, long logical) = keys[keyIdx];
            nowStates[keyIdx] = _pressingRecords.ContainsKey(physical);
            if (logical == eventLogicalKey)
            {
                switch (EventTypeOf(keyEvent))
                {
                    case KeyEventType.Down:
                        preEventStates[keyIdx] = false;
                        postEventAnyPressed = true;
                        if (!truePressed)
                        {
                            postSynchronize.Add(() => SynthesizeEvent(false, logical, eventPhysicalKey, eventTime));
                        }

                        break;
                    case KeyEventType.Up:
                        preEventStates[keyIdx] = nowStates[keyIdx];
                        break;
                    case KeyEventType.Repeat:
                        if (!truePressed)
                        {
                            postSynchronize.Add(() => SynthesizeEvent(false, logical, eventPhysicalKey, eventTime));
                        }

                        preEventStates[keyIdx] = nowStates[keyIdx];
                        postEventAnyPressed = true;
                        break;
                }
            }
            else
            {
                postEventAnyPressed = postEventAnyPressed || nowStates[keyIdx];
            }
        }

        bool isVirtualKeyboard = keyEvent.DeviceId == VirtualKeyboard;
        if (truePressed)
        {
            for (int keyIdx = 0; keyIdx < keys.Length; keyIdx++)
            {
                if (preEventStates[keyIdx] is not null)
                {
                    continue;
                }

                if (postEventAnyPressed || isVirtualKeyboard)
                {
                    preEventStates[keyIdx] = nowStates[keyIdx];
                }
                else
                {
                    preEventStates[keyIdx] = true;
                    postEventAnyPressed = true;
                }
            }

            if (!postEventAnyPressed && !isVirtualKeyboard)
            {
                preEventStates[0] = true;
            }
        }
        else
        {
            for (int keyIdx = 0; keyIdx < keys.Length; keyIdx++)
            {
                preEventStates[keyIdx] ??= false;
            }
        }

        for (int keyIdx = 0; keyIdx < keys.Length; keyIdx++)
        {
            if (nowStates[keyIdx] != preEventStates[keyIdx])
            {
                SynthesizeEvent(preEventStates[keyIdx]!.Value, keys[keyIdx].Logical, keys[keyIdx].Physical, eventTime);
            }
        }
    }

    private void SynchronizeTogglingKey(TogglingGoal goal, bool trueEnabled, long eventLogicalKey, KeyEvent keyEvent)
    {
        if (goal.LogicalKey == eventLogicalKey || goal.Enabled == trueEnabled)
        {
            return;
        }

        bool firstIsDown = !_pressingRecords.ContainsKey(goal.PhysicalKey);
        if (firstIsDown)
        {
            goal.Enabled = !goal.Enabled;
        }

        SynthesizeEvent(firstIsDown, goal.LogicalKey, goal.PhysicalKey, keyEvent.EventTime);
        if (!firstIsDown)
        {
            goal.Enabled = !goal.Enabled;
        }

        SynthesizeEvent(!firstIsDown, goal.LogicalKey, goal.PhysicalKey, keyEvent.EventTime);
    }

    private static KeyEventType EventTypeOf(KeyEvent keyEvent) => keyEvent.Action switch
    {
        KeyEventActions.Down => keyEvent.RepeatCount > 0 ? KeyEventType.Repeat : KeyEventType.Down,
        KeyEventActions.Up => KeyEventType.Up,
        _ => throw new InvalidOperationException("Unexpected event type"),
    };

    private void SynthesizeEvent(bool isDown, long logicalKey, long physicalKey, long timestamp)
    {
        if (physicalKey != 0 && logicalKey != 0)
        {
            UpdatePressingState(physicalKey, isDown ? logicalKey : null);
        }

        SendKeyData(new KeyData(
            Timestamp(timestamp),
            isDown ? KeyEventType.Down : KeyEventType.Up,
            physicalKey,
            logicalKey,
            character: null,
            synthesized: true));
    }

    // The engine sends the packet on `flutter/keydata`; dart:ui hands it to PlatformDispatcher.onKeyData.
    private static bool SendKeyData(KeyData data) => PlatformDispatcher.Instance.DispatchKeyData(data);

    private sealed class TogglingGoal(MetaKeyStates mask, long physicalKey, long logicalKey)
    {
        public MetaKeyStates Mask { get; } = mask;

        public long PhysicalKey { get; } = physicalKey;

        public long LogicalKey { get; } = logicalKey;

        public bool Enabled { get; set; }
    }

    /// <summary>
    /// <c>KeyboardManager.CharacterCombiner</c>: a dead key's accent combines with the next character, as
    /// <c>KeyCharacterMap.getDeadChar</c> composes them.
    /// </summary>
    private sealed class CharacterCombiner
    {
        // KeyCharacterMap.COMBINING_ACCENT and COMBINING_ACCENT_MASK.
        private const int CombiningAccent = unchecked((int)0x80000000);
        private const int CombiningAccentMask = 0x7FFFFFFF;

        private int _combiningCharacter;

        public char Apply(int newCharacterCodePoint)
        {
            char complexCharacter = (char)newCharacterCodePoint;
            if ((newCharacterCodePoint & CombiningAccent) != 0)
            {
                int plainCodePoint = newCharacterCodePoint & CombiningAccentMask;
                _combiningCharacter = _combiningCharacter != 0
                    ? KeyCharacterMap.GetDeadChar(_combiningCharacter, plainCodePoint)
                    : plainCodePoint;
            }
            else if (_combiningCharacter != 0)
            {
                int combinedChar = KeyCharacterMap.GetDeadChar(_combiningCharacter, newCharacterCodePoint);
                if (combinedChar > 0)
                {
                    complexCharacter = (char)combinedChar;
                }

                _combiningCharacter = 0;
            }

            return complexCharacter;
        }
    }
}
