// C#-only infrastructure: the Avalonia host's analogue of the engine embedders' raw key channel
// responders (`FlutterChannelKeyResponder` on macOS, `KeyboardKeyChannelHandler` on Windows,
// `FlKeyChannelResponder` on Linux, `KeyChannelResponder` on Android, ...). Each embedder encodes a
// native key event as a `flutter/keyevent` JSON message in its own `keymap`, which the framework
// decodes with `RawKeyEvent.fromMessage`. Plumix's host receives Avalonia key events instead of
// native ones, so this encoder rebuilds the running platform's message from them: native codes come
// from the inverse of the `keyboard_maps.g.dart` tables, and sided modifier flags from the modifier
// keys this encoder has seen go down (Avalonia reports only sideless `KeyModifiers`).

using Avalonia.Input;
using Plumix.Foundation;
using Plumix.UI;

namespace Plumix;

#pragma warning disable CS0618 // The raw key platform classes mirror Flutter's deprecated API.
internal static class HostKeyEventMessage
{
    // Physical modifier keys held down, as the embedders track them for their sided modifier flags.
    private static readonly HashSet<PhysicalKeyboardKey> ModifiersDown = [];
    private static bool _capsLockOn;
    private static bool _numLockOn;
    private static bool _scrollLockOn;

    /// <summary>
    /// Encodes the host event as the running platform's raw key message, sends it on
    /// <see cref="SystemChannels.KeyEvent"/> and returns the framework's <c>handled</c> reply.
    /// </summary>
    public static bool Dispatch(KeyEventArgs e, bool isDown)
    {
        PhysicalKeyboardKey physicalKey = HostKeyboardMap.PhysicalKeyFor(e.PhysicalKey);
        LogicalKeyboardKey logicalKey = HostKeyboardMap.LogicalKeyFor(e.Key, e.KeySymbol);
        Dictionary<string, object?> message = Encode(
            CurrentKeymap(),
            physicalKey,
            logicalKey,
            e.KeySymbol,
            e.KeyModifiers,
            HostKeyboardMap.DeviceTypeFor(e.KeyDeviceType),
            isDown);

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

    /// <summary>Forgets the tracked modifier and lock state. Test-only.</summary>
    internal static void Reset()
    {
        ModifiersDown.Clear();
        _capsLockOn = false;
        _numLockOn = false;
        _scrollLockOn = false;
    }

    /// <summary>The <c>keymap</c> of the embedder the host stands in for.</summary>
    internal static string CurrentKeymap()
    {
        if (Constants.KIsWeb)
        {
            return "web";
        }

        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            return "macos";
        }

        if (OperatingSystem.IsIOS())
        {
            return "ios";
        }

        if (OperatingSystem.IsAndroid())
        {
            return "android";
        }

        return OperatingSystem.IsWindows() ? "windows" : "linux";
    }

    /// <summary>
    /// Builds the <c>flutter/keyevent</c> message an embedder of <paramref name="keymap"/> would send
    /// for the event, updating the tracked modifier state.
    /// </summary>
    internal static Dictionary<string, object?> Encode(
        string keymap,
        PhysicalKeyboardKey physicalKey,
        LogicalKeyboardKey logicalKey,
        string? keySymbol,
        KeyModifiers keyModifiers,
        KeyEventDeviceType deviceType,
        bool isDown)
    {
        // GTK reports the modifier state from before the event (X11 semantics) and lets
        // `GtkKeyHelper` merge the event's own key; every other embedder reports the state after it.
        HashSet<PhysicalKeyboardKey> modifiersBefore = [.. ModifiersDown];
        bool capsLockBefore = _capsLockOn;
        bool numLockBefore = _numLockOn;
        UpdateModifierState(physicalKey, isDown, keyModifiers);

        string label = logicalKey.KeyLabel;
        string? printable = label.Length == 1 && !LogicalKeyboardKey.IsControlCharacter(label)
            ? label.ToLowerInvariant()
            : null;
        string characters = keySymbol is { Length: > 0 } && !LogicalKeyboardKey.IsControlCharacter(keySymbol)
            ? keySymbol
            : printable ?? string.Empty;
        var message = new Dictionary<string, object?>
        {
            ["type"] = isDown ? "keydown" : "keyup",
            ["keymap"] = keymap,
        };

        switch (keymap)
        {
            case "macos":
                message["keyCode"] = NativeCode(KeyboardMaps.KMacOsToPhysicalKey, physicalKey);
                message["characters"] = characters;
                message["charactersIgnoringModifiers"] = printable ?? characters;
                message["modifiers"] = MacOsModifiers(physicalKey);
                message["specifiedLogicalKey"] = logicalKey.KeyId;
                break;
            case "ios":
                message["keyCode"] = NativeCode(KeyboardMaps.KIosToPhysicalKey, physicalKey);
                message["characters"] = characters;
                message["charactersIgnoringModifiers"] = printable ?? characters;
                message["modifiers"] = IosModifiers(physicalKey);
                break;
            case "windows":
                message["keyCode"] = NativeCode(KeyboardMaps.KWindowsNumPadMap, logicalKey)
                                     ?? NativeCode(KeyboardMaps.KWindowsToLogicalKey, logicalKey) ?? 0;
                message["scanCode"] = NativeCode(KeyboardMaps.KWindowsToPhysicalKey, physicalKey);
                message["characterCodePoint"] = printable == null ? 0 : char.ConvertToUtf32(printable, 0);
                message["modifiers"] = WindowsModifiers();
                break;
            case "linux":
                int unicodeScalarValues = printable == null ? 0 : char.ConvertToUtf32(printable, 0);
                message["toolkit"] = "gtk";
                message["keyCode"] = unicodeScalarValues != 0
                    ? GdkKeyvalForCodePoint(unicodeScalarValues)
                    : NativeCode(KeyboardMaps.KGtkNumpadMap, logicalKey)
                      ?? NativeCode(KeyboardMaps.KGtkToLogicalKey, logicalKey) ?? 0;
                message["scanCode"] = NativeCode(KeyboardMaps.KLinuxToPhysicalKey, physicalKey);
                message["unicodeScalarValues"] = unicodeScalarValues;
                message["modifiers"] = GtkModifiers(modifiersBefore, capsLockBefore, numLockBefore);
                message["specifiedLogicalKey"] = logicalKey.KeyId;
                break;
            case "android":
                int codePoint = isDown && characters.Length > 0 ? char.ConvertToUtf32(characters, 0) : 0;
                message["keyCode"] = NativeCode(KeyboardMaps.KAndroidNumPadMap, logicalKey)
                                     ?? NativeCode(KeyboardMaps.KAndroidToLogicalKey, logicalKey) ?? 0;
                message["scanCode"] = NativeCode(KeyboardMaps.KAndroidToPhysicalKey, physicalKey);
                message["codePoint"] = codePoint;
                message["plainCodePoint"] = printable == null ? 0 : char.ConvertToUtf32(printable, 0);
                message["metaState"] = AndroidMetaState();
                message["source"] = AndroidSource(deviceType);
                if (codePoint != 0)
                {
                    message["character"] = characters;
                }

                break;
            default:
                (string key, int location) = WebKeyAndLocation(logicalKey, characters);
                message["code"] = WebCode(physicalKey);
                message["key"] = key;
                message["location"] = location;
                message["metaState"] = WebMetaState();
                break;
        }

        return message;
    }

    private static void UpdateModifierState(PhysicalKeyboardKey physicalKey, bool isDown, KeyModifiers keyModifiers)
    {
        if (SidedModifiers.ContainsKey(physicalKey))
        {
            if (isDown)
            {
                ModifiersDown.Add(physicalKey);
            }
            else
            {
                ModifiersDown.Remove(physicalKey);
            }
        }

        if (isDown && physicalKey.Equals(PhysicalKeyboardKey.CapsLock))
        {
            _capsLockOn = !_capsLockOn;
        }
        else if (isDown && physicalKey.Equals(PhysicalKeyboardKey.NumLock))
        {
            _numLockOn = !_numLockOn;
        }
        else if (isDown && physicalKey.Equals(PhysicalKeyboardKey.ScrollLock))
        {
            _scrollLockOn = !_scrollLockOn;
        }

        // A modifier pressed or released while the window did not have focus never reached the
        // tracker; Avalonia's sideless flags tell it apart, so reconcile toward them (the left key
        // stands for a modifier whose side is unknown, as Flutter's `KeyboardSide.any` does).
        Reconcile(keyModifiers.HasFlag(KeyModifiers.Shift), PhysicalKeyboardKey.ShiftLeft,
            PhysicalKeyboardKey.ShiftRight, physicalKey);
        Reconcile(keyModifiers.HasFlag(KeyModifiers.Control), PhysicalKeyboardKey.ControlLeft,
            PhysicalKeyboardKey.ControlRight, physicalKey);
        Reconcile(keyModifiers.HasFlag(KeyModifiers.Alt), PhysicalKeyboardKey.AltLeft,
            PhysicalKeyboardKey.AltRight, physicalKey);
        Reconcile(keyModifiers.HasFlag(KeyModifiers.Meta), PhysicalKeyboardKey.MetaLeft,
            PhysicalKeyboardKey.MetaRight, physicalKey);
    }

    private static void Reconcile(
        bool hostPressed,
        PhysicalKeyboardKey left,
        PhysicalKeyboardKey right,
        PhysicalKeyboardKey eventKey)
    {
        // The event's own modifier key is authoritative: hosts disagree on whether a modifier's
        // flag is already set (or still set) on its own down (or up) event.
        if (eventKey.Equals(left) || eventKey.Equals(right))
        {
            return;
        }

        bool trackedPressed = ModifiersDown.Contains(left) || ModifiersDown.Contains(right);
        if (hostPressed && !trackedPressed)
        {
            ModifiersDown.Add(left);
        }
        else if (!hostPressed && trackedPressed)
        {
            ModifiersDown.Remove(left);
            ModifiersDown.Remove(right);
        }
    }

    private static int MacOsModifiers(PhysicalKeyboardKey physicalKey)
    {
        int modifiers = SidedFlags(
            RawKeyEventDataMacOs.ModifierShift, RawKeyEventDataMacOs.ModifierLeftShift,
            RawKeyEventDataMacOs.ModifierRightShift,
            RawKeyEventDataMacOs.ModifierControl, RawKeyEventDataMacOs.ModifierLeftControl,
            RawKeyEventDataMacOs.ModifierRightControl,
            RawKeyEventDataMacOs.ModifierOption, RawKeyEventDataMacOs.ModifierLeftOption,
            RawKeyEventDataMacOs.ModifierRightOption,
            RawKeyEventDataMacOs.ModifierCommand, RawKeyEventDataMacOs.ModifierLeftCommand,
            RawKeyEventDataMacOs.ModifierRightCommand);
        if (_capsLockOn)
        {
            modifiers |= RawKeyEventDataMacOs.ModifierCapsLock;
        }

        if (IsNumPad(physicalKey))
        {
            modifiers |= RawKeyEventDataMacOs.ModifierNumericPad;
        }

        if (IsFunctionKey(physicalKey))
        {
            modifiers |= RawKeyEventDataMacOs.ModifierFunction;
        }

        return modifiers;
    }

    private static int IosModifiers(PhysicalKeyboardKey physicalKey)
    {
        int modifiers = SidedFlags(
            RawKeyEventDataIos.ModifierShift, RawKeyEventDataIos.ModifierLeftShift,
            RawKeyEventDataIos.ModifierRightShift,
            RawKeyEventDataIos.ModifierControl, RawKeyEventDataIos.ModifierLeftControl,
            RawKeyEventDataIos.ModifierRightControl,
            RawKeyEventDataIos.ModifierOption, RawKeyEventDataIos.ModifierLeftOption,
            RawKeyEventDataIos.ModifierRightOption,
            RawKeyEventDataIos.ModifierCommand, RawKeyEventDataIos.ModifierLeftCommand,
            RawKeyEventDataIos.ModifierRightCommand);
        if (_capsLockOn)
        {
            modifiers |= RawKeyEventDataIos.ModifierCapsLock;
        }

        if (IsNumPad(physicalKey))
        {
            modifiers |= RawKeyEventDataIos.ModifierNumericPad;
        }

        if (IsFunctionKey(physicalKey))
        {
            modifiers |= RawKeyEventDataIos.ModifierFunction;
        }

        return modifiers;
    }

    private static int WindowsModifiers()
    {
        int modifiers = SidedFlags(
            RawKeyEventDataWindows.ModifierShift, RawKeyEventDataWindows.ModifierLeftShift,
            RawKeyEventDataWindows.ModifierRightShift,
            RawKeyEventDataWindows.ModifierControl, RawKeyEventDataWindows.ModifierLeftControl,
            RawKeyEventDataWindows.ModifierRightControl,
            RawKeyEventDataWindows.ModifierAlt, RawKeyEventDataWindows.ModifierLeftAlt,
            RawKeyEventDataWindows.ModifierRightAlt,
            0, RawKeyEventDataWindows.ModifierLeftMeta,
            RawKeyEventDataWindows.ModifierRightMeta);
        modifiers |= _capsLockOn ? RawKeyEventDataWindows.ModifierCaps : 0;
        modifiers |= _numLockOn ? RawKeyEventDataWindows.ModifierNumLock : 0;
        modifiers |= _scrollLockOn ? RawKeyEventDataWindows.ModifierScrollLock : 0;
        return modifiers;
    }

    private static int AndroidMetaState()
    {
        int metaState = SidedFlags(
            RawKeyEventDataAndroid.ModifierShift, RawKeyEventDataAndroid.ModifierLeftShift,
            RawKeyEventDataAndroid.ModifierRightShift,
            RawKeyEventDataAndroid.ModifierControl, RawKeyEventDataAndroid.ModifierLeftControl,
            RawKeyEventDataAndroid.ModifierRightControl,
            RawKeyEventDataAndroid.ModifierAlt, RawKeyEventDataAndroid.ModifierLeftAlt,
            RawKeyEventDataAndroid.ModifierRightAlt,
            RawKeyEventDataAndroid.ModifierMeta, RawKeyEventDataAndroid.ModifierLeftMeta,
            RawKeyEventDataAndroid.ModifierRightMeta);
        metaState |= _capsLockOn ? RawKeyEventDataAndroid.ModifierCapsLock : 0;
        metaState |= _numLockOn ? RawKeyEventDataAndroid.ModifierNumLock : 0;
        metaState |= _scrollLockOn ? RawKeyEventDataAndroid.ModifierScrollLock : 0;
        return metaState;
    }

    private static int GtkModifiers(HashSet<PhysicalKeyboardKey> modifiersDown, bool capsLockOn, bool numLockOn)
    {
        int modifiers = 0;
        modifiers |= AnySide(modifiersDown, PhysicalKeyboardKey.ShiftLeft, PhysicalKeyboardKey.ShiftRight)
            ? GtkKeyHelper.ModifierShift
            : 0;
        modifiers |= AnySide(modifiersDown, PhysicalKeyboardKey.ControlLeft, PhysicalKeyboardKey.ControlRight)
            ? GtkKeyHelper.ModifierControl
            : 0;
        modifiers |= AnySide(modifiersDown, PhysicalKeyboardKey.AltLeft, PhysicalKeyboardKey.AltRight)
            ? GtkKeyHelper.ModifierMod1
            : 0;
        modifiers |= AnySide(modifiersDown, PhysicalKeyboardKey.MetaLeft, PhysicalKeyboardKey.MetaRight)
            ? GtkKeyHelper.ModifierMeta
            : 0;
        modifiers |= capsLockOn ? GtkKeyHelper.ModifierCapsLock : 0;
        modifiers |= numLockOn ? GtkKeyHelper.ModifierMod2 : 0;
        return modifiers;
    }

    private static int WebMetaState()
    {
        int metaState = RawKeyEventDataWeb.ModifierNone;
        metaState |= AnySide(ModifiersDown, PhysicalKeyboardKey.ShiftLeft, PhysicalKeyboardKey.ShiftRight)
            ? RawKeyEventDataWeb.ModifierShift
            : 0;
        metaState |= AnySide(ModifiersDown, PhysicalKeyboardKey.AltLeft, PhysicalKeyboardKey.AltRight)
            ? RawKeyEventDataWeb.ModifierAlt
            : 0;
        metaState |= AnySide(ModifiersDown, PhysicalKeyboardKey.ControlLeft, PhysicalKeyboardKey.ControlRight)
            ? RawKeyEventDataWeb.ModifierControl
            : 0;
        metaState |= AnySide(ModifiersDown, PhysicalKeyboardKey.MetaLeft, PhysicalKeyboardKey.MetaRight)
            ? RawKeyEventDataWeb.ModifierMeta
            : 0;
        metaState |= _numLockOn ? RawKeyEventDataWeb.ModifierNumLock : 0;
        metaState |= _capsLockOn ? RawKeyEventDataWeb.ModifierCapsLock : 0;
        metaState |= _scrollLockOn ? RawKeyEventDataWeb.ModifierScrollLock : 0;
        return metaState;
    }

    // Each modifier contributes its sideless bit plus the bit of each held side, as the embedders
    // report them.
    private static int SidedFlags(
        int shift, int leftShift, int rightShift,
        int control, int leftControl, int rightControl,
        int alt, int leftAlt, int rightAlt,
        int meta, int leftMeta, int rightMeta)
    {
        int flags = 0;
        flags |= Sided(PhysicalKeyboardKey.ShiftLeft, PhysicalKeyboardKey.ShiftRight, shift, leftShift, rightShift);
        flags |= Sided(
            PhysicalKeyboardKey.ControlLeft, PhysicalKeyboardKey.ControlRight, control, leftControl, rightControl);
        flags |= Sided(PhysicalKeyboardKey.AltLeft, PhysicalKeyboardKey.AltRight, alt, leftAlt, rightAlt);
        flags |= Sided(PhysicalKeyboardKey.MetaLeft, PhysicalKeyboardKey.MetaRight, meta, leftMeta, rightMeta);
        return flags;
    }

    private static int Sided(PhysicalKeyboardKey left, PhysicalKeyboardKey right, int any, int leftBit, int rightBit)
    {
        int flags = 0;
        if (ModifiersDown.Contains(left))
        {
            flags |= any | leftBit;
        }

        if (ModifiersDown.Contains(right))
        {
            flags |= any | rightBit;
        }

        return flags;
    }

    private static bool AnySide(
        HashSet<PhysicalKeyboardKey> keys,
        PhysicalKeyboardKey left,
        PhysicalKeyboardKey right) =>
        keys.Contains(left) || keys.Contains(right);

    private static bool IsNumPad(PhysicalKeyboardKey physicalKey) =>
        KeyboardMaps.KMacOsNumPadMap.ContainsKey(NativeCode(KeyboardMaps.KMacOsToPhysicalKey, physicalKey));

    private static bool IsFunctionKey(PhysicalKeyboardKey physicalKey) =>
        FunctionKeys.Contains(physicalKey);

    // GDK keyvals: Latin-1 characters are their own keyvals; any other Unicode character is
    // `0x01000000 | codePoint` (gdk_unicode_to_keyval).
    private static int GdkKeyvalForCodePoint(int codePoint) =>
        codePoint is >= 0x20 and <= 0x7e or >= 0xa0 and <= 0xff ? codePoint : 0x01000000 | codePoint;

    private static int AndroidSource(KeyEventDeviceType deviceType) => deviceType switch
    {
        KeyEventDeviceType.DirectionalPad => 0x00000201,
        KeyEventDeviceType.Gamepad => 0x00000401,
        KeyEventDeviceType.Joystick => 0x01000010,
        KeyEventDeviceType.Hdmi => 0x02000001,
        _ => 0x00000101,
    };

    private static string WebCode(PhysicalKeyboardKey physicalKey) =>
        WebCodes.Value.GetValueOrDefault(physicalKey.UsbHidUsage) ?? "Unidentified";

    // The browser's `KeyboardEvent.key` and `location` for the event.
    private static (string Key, int Location) WebKeyAndLocation(LogicalKeyboardKey logicalKey, string characters)
    {
        foreach ((string key, IReadOnlyList<LogicalKeyboardKey?> locations) in KeyboardMaps.KWebLocationMap)
        {
            for (int location = locations.Count - 1; location > 0; location--)
            {
                if (logicalKey.Equals(locations[location]))
                {
                    return (key, location);
                }
            }
        }

        if (characters.Length > 0)
        {
            return (characters, 0);
        }

        return (WebKeys.Value.GetValueOrDefault(logicalKey.KeyId) ?? "Unidentified", 0);
    }

    private static int NativeCode(IReadOnlyDictionary<int, PhysicalKeyboardKey> map, PhysicalKeyboardKey key) =>
        InversePhysical(map).GetValueOrDefault(key.UsbHidUsage);

    private static int? NativeCode(IReadOnlyDictionary<int, LogicalKeyboardKey> map, LogicalKeyboardKey key) =>
        InverseLogical(map).TryGetValue(key.KeyId, out int code) ? code : null;

    private static readonly Dictionary<object, Dictionary<long, int>> InverseMaps = [];

    // The first native code that maps to each key, the code an embedder sends for it.
    private static Dictionary<long, int> InversePhysical(IReadOnlyDictionary<int, PhysicalKeyboardKey> map)
    {
        if (!InverseMaps.TryGetValue(map, out Dictionary<long, int>? inverse))
        {
            inverse = [];
            foreach ((int code, PhysicalKeyboardKey key) in map)
            {
                inverse.TryAdd(key.UsbHidUsage, code);
            }

            InverseMaps[map] = inverse;
        }

        return inverse;
    }

    private static Dictionary<long, int> InverseLogical(IReadOnlyDictionary<int, LogicalKeyboardKey> map)
    {
        if (!InverseMaps.TryGetValue(map, out Dictionary<long, int>? inverse))
        {
            inverse = [];
            foreach ((int code, LogicalKeyboardKey key) in map)
            {
                inverse.TryAdd(key.KeyId, code);
            }

            InverseMaps[map] = inverse;
        }

        return inverse;
    }

    private static readonly Lazy<Dictionary<long, string>> WebCodes = new(() =>
    {
        var codes = new Dictionary<long, string>();
        foreach ((string code, PhysicalKeyboardKey key) in KeyboardMaps.KWebToPhysicalKey)
        {
            codes.TryAdd(key.UsbHidUsage, code);
        }

        return codes;
    });

    private static readonly Lazy<Dictionary<long, string>> WebKeys = new(() =>
    {
        var keys = new Dictionary<long, string>();
        foreach ((string name, LogicalKeyboardKey key) in KeyboardMaps.KWebToLogicalKey)
        {
            keys.TryAdd(key.KeyId, name);
        }

        return keys;
    });

    private static readonly Dictionary<PhysicalKeyboardKey, bool> SidedModifiers = new()
    {
        [PhysicalKeyboardKey.ShiftLeft] = true,
        [PhysicalKeyboardKey.ShiftRight] = true,
        [PhysicalKeyboardKey.ControlLeft] = true,
        [PhysicalKeyboardKey.ControlRight] = true,
        [PhysicalKeyboardKey.AltLeft] = true,
        [PhysicalKeyboardKey.AltRight] = true,
        [PhysicalKeyboardKey.MetaLeft] = true,
        [PhysicalKeyboardKey.MetaRight] = true,
    };

    private static readonly HashSet<PhysicalKeyboardKey> FunctionKeys =
    [
        PhysicalKeyboardKey.F1, PhysicalKeyboardKey.F2, PhysicalKeyboardKey.F3, PhysicalKeyboardKey.F4,
        PhysicalKeyboardKey.F5, PhysicalKeyboardKey.F6, PhysicalKeyboardKey.F7, PhysicalKeyboardKey.F8,
        PhysicalKeyboardKey.F9, PhysicalKeyboardKey.F10, PhysicalKeyboardKey.F11, PhysicalKeyboardKey.F12,
        PhysicalKeyboardKey.F13, PhysicalKeyboardKey.F14, PhysicalKeyboardKey.F15, PhysicalKeyboardKey.F16,
        PhysicalKeyboardKey.F17, PhysicalKeyboardKey.F18, PhysicalKeyboardKey.F19, PhysicalKeyboardKey.F20,
        PhysicalKeyboardKey.F21,
    ];
}
#pragma warning restore CS0618
