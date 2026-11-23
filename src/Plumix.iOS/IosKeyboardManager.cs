using Plumix.Foundation;
using Plumix.UI;
using UIKit;

namespace Plumix.iOS;

// C#-only host infrastructure parity sources, all under
// flutter/engine/src/flutter/shell/platform/darwin/ios/framework/Source/: FlutterKeyboardManager.mm (both primary
// responders get every began/ended press; one neither handles goes to the secondary responders, the text input plugin,
// which declines, and then back to UIKit, which types it into the first responder), FlutterEmbedderKeyResponder.mm
// (KeyData with modifier and Caps Lock synchronization; iOS keeps a released modifier's flag on its key-up),
// FlutterChannelKeyResponder.mm (the `flutter/keyevent` message) and KeyCodeMap.g.mm (IosKeyCodeMap.g.cs). The
// engine spins the run loop until the framework answers; the framework answers this host's calls synchronously.

/// <summary>Sends the view controller's presses to the framework, as the engine's FlutterKeyboardManager.</summary>
internal sealed class IosKeyboardManager
{
    private readonly EmbedderKeyResponder _embedder = new();
    private readonly ChannelKeyResponder _channel = new();

    /// <summary>
    /// handlePress:nextAction: runs <paramref name="next"/> (UIKit's handling) when nobody took the press.
    /// </summary>
    public void HandlePress(UIPress press, Action next)
    {
        if (!OperatingSystem.IsIOSVersionAtLeast(13, 4) || press.Key is not { } key)
        {
            next();
            return;
        }

        bool wasHandled = false;
        if (press.Phase is UIPressPhase.Began or UIPressPhase.Ended)
        {
            bool embedderHandled = _embedder.HandlePress(press, key);
            bool channelHandled = _channel.HandlePress(press, key);
            // The secondary responder, the text input plugin, never takes a press.
            wasHandled = embedderHandled || channelHandled;
        }

        if (!wasHandled)
        {
            next();
        }
    }

    // FlutterEmbedderKeyResponder.
    private sealed class EmbedderKeyResponder
    {
        // KeyCodeMap_Internal.h's ModifierFlag.
        private const uint ControlLeft = 0x1;
        private const uint ShiftLeft = 0x2;
        private const uint ShiftRight = 0x4;
        private const uint MetaLeft = 0x8;
        private const uint MetaRight = 0x10;
        private const uint AltLeft = 0x20;
        private const uint AltRight = 0x40;
        private const uint ControlRight = 0x2000;
        private const uint CapsLock = 0x010000;
        private const uint ShiftAny = 0x020000;
        private const uint ControlAny = 0x040000;
        private const uint AltAny = 0x080000;
        private const uint MetaAny = 0x100000;
        private const uint AnyMask = ShiftAny | ControlAny | AltAny | MetaAny;
        private const uint SidedMask =
            ControlLeft | ShiftLeft | ShiftRight | MetaLeft | MetaRight | AltLeft | AltRight | ControlRight;

        // computeModifierFlagOfInterestMask: the Any flags, Caps Lock and every sided flag.
        private const uint ModifierFlagOfInterestMask = CapsLock | AnyMask | SidedMask;

        // keyCodeToModifierFlag / modifierFlagToKeyCode.
        private static readonly Dictionary<uint, uint> ModifierFlagToKeyCode = new()
        {
            [ShiftLeft] = 0xe1,
            [ShiftRight] = 0xe5,
            [ControlLeft] = 0xe0,
            [ControlRight] = 0xe4,
            [AltLeft] = 0xe2,
            [AltRight] = 0xe6,
            [MetaLeft] = 0xe3,
            [MetaRight] = 0xe7,
        };

        // The physical keys pressed, to the logical key of their down event.
        private readonly Dictionary<ulong, ulong> _pressingRecords = [];
        private uint _lastModifierFlagsOfInterest;

        public bool HandlePress(UIPress press, UIKey key) =>
            press.Phase == UIPressPhase.Began ? HandlePressBegin(press, key) : HandlePressEnd(press, key);

        private bool HandlePressBegin(UIPress press, UIKey key)
        {
            uint keyCode = (uint)key.KeyCode;
            ulong physicalKey = GetPhysicalKeyForKeyCode(keyCode);
            ulong? specialKey = IosKeyCodeMap.SpecialKeyMapping.TryGetValue(
                key.CharactersIgnoringModifiers ?? string.Empty, out ulong special)
                ? special
                : null;
            ulong logicalKey = GetLogicalKeyForEvent(key, specialKey);
            SynchronizeModifiers(press, key);

            // A repeated down of a pressed key: answered as handled with an empty event.
            if (_pressingRecords.ContainsKey(physicalKey))
            {
                SendEmptyEvent();
                return true;
            }

            _pressingRecords[physicalKey] = logicalKey;
            return SendPrimaryEvent(new KeyData(
                Timestamp(press.Timestamp),
                KeyEventType.Down,
                (long)physicalKey,
                (long)logicalKey,
                specialKey is not null ? null : GetEventCharacters(key.Characters, keyCode),
                synthesized: false));
        }

        private bool HandlePressEnd(UIPress press, UIKey key)
        {
            SynchronizeModifiers(press, key);
            ulong physicalKey = GetPhysicalKeyForKeyCode((uint)key.KeyCode);
            // An up without a down: answered as handled with an empty event.
            if (!_pressingRecords.Remove(physicalKey, out ulong pressedLogicalKey))
            {
                SendEmptyEvent();
                return true;
            }

            return SendPrimaryEvent(new KeyData(
                Timestamp(press.Timestamp),
                KeyEventType.Up,
                (long)physicalKey,
                (long)pressedLogicalKey,
                character: null,
                synthesized: false));
        }

        // synchronizeModifiers: a synthesized event for each modifier whose flag changed without its own event.
        private void SynchronizeModifiers(UIPress press, UIKey key)
        {
            uint lastFlagsOfInterest = _lastModifierFlagsOfInterest & ModifierFlagOfInterestMask;
            uint currentFlagsOfInterest = AdjustModifiers(press, key) & ModifierFlagOfInterestMask;
            uint flagDifference = currentFlagsOfInterest ^ lastFlagsOfInterest;
            if ((flagDifference & CapsLock) != 0)
            {
                if (key.KeyCode != UIKeyboardHidUsage.KeyboardCapsLock)
                {
                    SynthesizeCapsLockTap(press.Timestamp);
                }

                flagDifference &= ~CapsLock;
            }

            while (true)
            {
                uint currentFlag = flagDifference & (uint)-(int)flagDifference;
                if (currentFlag == 0)
                {
                    break;
                }

                flagDifference &= ~currentFlag;
                if ((currentFlag & AnyMask) != 0
                    || !ModifierFlagToKeyCode.TryGetValue(currentFlag, out uint modifierKeyCode)
                    || modifierKeyCode == (uint)key.KeyCode)
                {
                    continue;
                }

                SynthesizeModifierEvent((currentFlagsOfInterest & currentFlag) != 0, press.Timestamp, modifierKeyCode);
            }

            _lastModifierFlagsOfInterest =
                (_lastModifierFlagsOfInterest & ~ModifierFlagOfInterestMask) | currentFlagsOfInterest;
        }

        private static void SynthesizeCapsLockTap(double timestamp)
        {
            SendSynthesized(KeyEventType.Down, IosKeyCodeMap.CapsLockPhysicalKey, IosKeyCodeMap.CapsLockLogicalKey,
                timestamp);
            SendSynthesized(KeyEventType.Up, IosKeyCodeMap.CapsLockPhysicalKey, IosKeyCodeMap.CapsLockLogicalKey,
                timestamp);
        }

        private void SynthesizeModifierEvent(bool isDownEvent, double timestamp, uint keyCode)
        {
            ulong physicalKey = GetPhysicalKeyForKeyCode(keyCode);
            ulong logicalKey = GetLogicalKeyForModifier(keyCode, physicalKey);
            if (physicalKey == 0 || logicalKey == 0)
            {
                return;
            }

            if (isDownEvent)
            {
                _pressingRecords[physicalKey] = logicalKey;
            }
            else
            {
                _pressingRecords.Remove(physicalKey);
            }

            SendSynthesized(isDownEvent ? KeyEventType.Down : KeyEventType.Up, physicalKey, logicalKey, timestamp);
        }

        private static void SendSynthesized(KeyEventType type, ulong physical, ulong logical, double timestamp) =>
            PlatformDispatcher.Instance.DispatchKeyData(
                new KeyData(Timestamp(timestamp), type, (long)physical, (long)logical, null, synthesized: true));

        // sendEmptyEvent: when no other event should be sent, so the framework still hears from this responder.
        private static void SendEmptyEvent() =>
            PlatformDispatcher.Instance.DispatchKeyData(
                new KeyData(TimeSpan.Zero, KeyEventType.Down, 0, 0, null, synthesized: false));

        private static bool SendPrimaryEvent(KeyData data) => PlatformDispatcher.Instance.DispatchKeyData(data);

        // adjustModifiers: iOS still reports a released modifier's flag on its key-up, and no sided flags at all.
        private uint AdjustModifiers(UIPress press, UIKey key)
        {
            bool keyDown = press.Phase is UIPressPhase.Began or UIPressPhase.Changed or UIPressPhase.Stationary;
            uint keyCode = (uint)key.KeyCode;
            uint pressedModifiers = (uint)key.ModifierFlags | (_lastModifierFlagsOfInterest & SidedMask);
            pressedModifiers = FixSidedFlags(ShiftAny, ShiftLeft, ShiftRight, 0xe1, 0xe5, keyCode, keyDown,
                pressedModifiers);
            pressedModifiers = FixSidedFlags(ControlAny, ControlLeft, ControlRight, 0xe0, 0xe4, keyCode, keyDown,
                pressedModifiers);
            pressedModifiers = FixSidedFlags(AltAny, AltLeft, AltRight, 0xe2, 0xe6, keyCode, keyDown,
                pressedModifiers);
            pressedModifiers = FixSidedFlags(MetaAny, MetaLeft, MetaRight, 0xe3, 0xe7, keyCode, keyDown,
                pressedModifiers);
            if (key.KeyCode == UIKeyboardHidUsage.KeyboardCapsLock && !keyDown
                && (_lastModifierFlagsOfInterest & CapsLock) != 0)
            {
                pressedModifiers &= ~CapsLock;
            }

            return pressedModifiers;
        }

        private static uint FixSidedFlags(
            uint anyFlag, uint leftSide, uint rightSide, uint leftKeyCode, uint rightKeyCode, uint keyCode,
            bool isKeyDown, uint modifiersPressed)
        {
            uint newModifiers = modifiersPressed;
            if (isKeyDown)
            {
                if (keyCode == leftKeyCode)
                {
                    newModifiers |= leftSide | anyFlag;
                }
                else if (keyCode == rightKeyCode)
                {
                    newModifiers |= rightSide | anyFlag;
                }
            }
            else if (keyCode == leftKeyCode)
            {
                newModifiers &= ~leftSide;
                if ((newModifiers & rightSide) == 0)
                {
                    newModifiers &= ~anyFlag;
                }
            }
            else if (keyCode == rightKeyCode)
            {
                newModifiers &= ~rightSide;
                if ((newModifiers & leftSide) == 0)
                {
                    newModifiers &= ~anyFlag;
                }
            }

            if ((newModifiers & anyFlag) == 0)
            {
                newModifiers &= ~(leftSide | rightSide);
            }

            return newModifiers;
        }

        private static ulong KeyOfPlane(ulong baseKey, ulong plane) => plane | (baseKey & IosKeyCodeMap.ValueMask);

        private static ulong GetPhysicalKeyForKeyCode(uint keyCode) =>
            IosKeyCodeMap.KeyCodeToPhysicalKey.TryGetValue(keyCode, out ulong physical)
                ? physical
                : KeyOfPlane(keyCode, IosKeyCodeMap.IosPlane);

        private static ulong GetLogicalKeyForModifier(uint keyCode, ulong hidCode) =>
            IosKeyCodeMap.KeyCodeToLogicalKey.TryGetValue(keyCode, out ulong logical)
                ? logical
                : KeyOfPlane(hidCode, IosKeyCodeMap.IosPlane);

        // GetLogicalKeyForEvent: the special key, else the key code's table entry, else the lower-cased label in the
        // Unicode plane, else the key code in the iOS plane.
        private static ulong GetLogicalKeyForEvent(UIKey key, ulong? specialKey)
        {
            if (specialKey is { } special)
            {
                return special;
            }

            uint keyCode = (uint)key.KeyCode;
            if (IosKeyCodeMap.KeyCodeToLogicalKey.TryGetValue(keyCode, out ulong logical))
            {
                return logical;
            }

            string? keyLabel = GetEventCharacters(key.CharactersIgnoringModifiers, keyCode);
            if (keyLabel is { Length: > 0 } && !IsControlCharacter(keyLabel) && !IsUnprintableKey(keyLabel))
            {
                ulong codeUnit = keyLabel[0];
                if (keyLabel.Length == 2)
                {
                    codeUnit = (codeUnit << 16) | keyLabel[1];
                }

                return KeyOfPlane(ToLower(codeUnit), IosKeyCodeMap.UnicodePlane);
            }

            return KeyOfPlane(keyCode, IosKeyCodeMap.IosPlane);
        }

        private static bool IsControlCharacter(string label) =>
            label.Length <= 1 && (label[0] <= 0x1f || (label[0] >= 0x7f && label[0] <= 0x9f));

        // NSUpArrowFunctionKey (0xF700) to the end of the function-key private use range.
        private static bool IsUnprintableKey(string label) =>
            label.Length <= 1 && label[0] >= 0xF700 && label[0] <= 0xF8FF;

        // toLower: ASCII and Latin-1 letters only, independent of locale.
        private static ulong ToLower(ulong n)
        {
            if (n is >= 0x41 and <= 0x5a)
            {
                return n - 0x41 + 0x61;
            }

            if (n is >= 0xc0 and <= 0xde && n != 0xf7)
            {
                return n - 0xc0 + 0xe0;
            }

            return n;
        }
    }

    // FlutterChannelKeyResponder: the RawKeyEvent message, with sided modifier bits the platform does not report.
    private sealed class ChannelKeyResponder
    {
        private const int AlphaShift = 0x10000;
        private const int Shift = 0x20000;
        private const int LeftShift = 0x02;
        private const int RightShift = 0x04;
        private const int Control = 0x40000;
        private const int LeftControl = 0x01;
        private const int RightControl = 0x2000;
        private const int Option = 0x80000;
        private const int LeftOption = 0x20;
        private const int RightOption = 0x40;
        private const int Command = 0x100000;
        private const int LeftCommand = 0x08;
        private const int RightCommand = 0x10;
        private const int NumericPad = 0x200000;
        private const int Mask = AlphaShift | Shift | LeftShift | RightShift | Control | LeftControl
                                 | RightControl | Option | LeftOption | RightOption | Command | LeftCommand
                                 | RightCommand | NumericPad;

        private int _pressedModifiers;

        public bool HandlePress(UIPress press, UIKey key)
        {
            string type = press.Phase == UIPressPhase.Began ? "keydown" : "keyup";
            uint keyCode = (uint)key.KeyCode;
            var message = new Dictionary<string, object?>
            {
                ["keymap"] = "ios",
                ["type"] = type,
                ["keyCode"] = (int)keyCode,
                ["modifiers"] = AdjustModifiers(press, key),
                ["characters"] = GetEventCharacters(key.Characters, keyCode) ?? string.Empty,
                ["charactersIgnoringModifiers"] =
                    GetEventCharacters(key.CharactersIgnoringModifiers, keyCode) ?? string.Empty,
            };
            bool handled = false;
            _ = ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
                SystemChannels.KeyEvent.Name,
                SystemChannels.KeyEvent.Codec.EncodeMessage(message),
                reply =>
                {
                    // No reply (no listener) counts as handled, as in the engine.
                    object? decoded = reply is null ? null : SystemChannels.KeyEvent.Codec.DecodeMessage(reply);
                    handled = decoded is not IReadOnlyDictionary<string, object?> map
                              || map.GetValueOrDefault("handled") is not false;
                });
            return handled;
        }

        private int AdjustModifiers(UIPress press, UIKey key)
        {
            UpdatePressedModifiers(press, key);
            return _pressedModifiers | ((int)key.ModifierFlags & ~Mask);
        }

        private void UpdatePressedModifiers(UIPress press, UIKey key)
        {
            bool isKeyDown = press.Phase == UIPressPhase.Began;
            int modifier = key.KeyCode switch
            {
                UIKeyboardHidUsage.KeyboardCapsLock => AlphaShift,
                UIKeyboardHidUsage.KeypadNumLock => NumericPad,
                UIKeyboardHidUsage.KeyboardLeftShift => LeftShift,
                UIKeyboardHidUsage.KeyboardRightShift => RightShift,
                UIKeyboardHidUsage.KeyboardLeftControl => LeftControl,
                UIKeyboardHidUsage.KeyboardRightControl => RightControl,
                UIKeyboardHidUsage.KeyboardLeftAlt => LeftOption,
                UIKeyboardHidUsage.KeyboardRightAlt => RightOption,
                UIKeyboardHidUsage.KeyboardLeftGui => LeftCommand,
                UIKeyboardHidUsage.KeyboardRightGui => RightCommand,
                _ => 0,
            };
            if (modifier == 0)
            {
                return;
            }

            Update(modifier, isKeyDown);
            Update(Shift, (_pressedModifiers & (RightShift | LeftShift)) != 0);
            Update(Control, (_pressedModifiers & (RightControl | LeftControl)) != 0);
            Update(Option, (_pressedModifiers & (RightOption | LeftOption)) != 0);
            Update(Command, (_pressedModifiers & (RightCommand | LeftCommand)) != 0);
        }

        private void Update(int modifier, bool isOn) =>
            _pressedModifiers = isOn ? _pressedModifiers | modifier : _pressedModifiers & ~modifier;
    }

    /// <summary>getEventCharacters: no characters for empty labels and function keys.</summary>
    private static string? GetEventCharacters(string? characters, uint keyCode) =>
        string.IsNullOrEmpty(characters) || IosKeyCodeMap.FunctionKeyCodes.Contains(keyCode) ? null : characters;

    // UIPress.timestamp is seconds since boot; the framework takes microseconds.
    private static TimeSpan Timestamp(double timestamp) => TimeSpan.FromMicroseconds(timestamp * 1000000.0);
}
