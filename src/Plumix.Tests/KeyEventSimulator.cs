using Plumix.Foundation;
using Plumix.UI;
using Xunit;

// Test infrastructure ported from flutter/packages/flutter_test/lib/src/event_simulation.dart
// (`KeyEventSimulator`, `simulateKeyDownEvent`/`simulateKeyUpEvent`/`simulateKeyRepeatEvent`,
// `KeySimulatorTransitModeVariant`). Dart's simulator is asynchronous because the platform message
// round-trips through the binary messenger; Plumix's messenger answers synchronously when the
// framework handler does, so these helpers return the result directly.

namespace Plumix.Tests;

#pragma warning disable CS0618 // The simulator drives Flutter's deprecated raw key API on purpose.

/// <summary>
/// A class that serves as a namespace for a bunch of keyboard-key generation utilities.
/// </summary>
internal static class KeyEventSimulator
{
    // Look up a synonym key, and just return the left version of it.
    private static LogicalKeyboardKey GetKeySynonym(LogicalKeyboardKey origin)
    {
        if (origin.Equals(LogicalKeyboardKey.Shift))
        {
            return LogicalKeyboardKey.ShiftLeft;
        }

        if (origin.Equals(LogicalKeyboardKey.Alt))
        {
            return LogicalKeyboardKey.AltLeft;
        }

        if (origin.Equals(LogicalKeyboardKey.Meta))
        {
            return LogicalKeyboardKey.MetaLeft;
        }

        if (origin.Equals(LogicalKeyboardKey.Control))
        {
            return LogicalKeyboardKey.ControlLeft;
        }

        return origin;
    }

    private static bool OsIsSupported(string platform) => platform switch
    {
        "android" or "fuchsia" or "macos" or "linux" or "web" or "ios" or "windows" => true,
        _ => false,
    };

    private static int GetScanCode(PhysicalKeyboardKey key, string platform)
    {
        DebugAssertions(OsIsSupported(platform), $"Platform {platform} not supported for key simulation");
        IReadOnlyDictionary<int, PhysicalKeyboardKey> map;
        switch (platform)
        {
            case "android":
                map = KeyboardMaps.KAndroidToPhysicalKey;
                break;
            case "fuchsia":
                map = KeyboardMaps.KFuchsiaToPhysicalKey;
                break;
            case "macos":
                map = KeyboardMaps.KMacOsToPhysicalKey;
                break;
            case "ios":
                map = KeyboardMaps.KIosToPhysicalKey;
                break;
            case "linux":
                map = KeyboardMaps.KLinuxToPhysicalKey;
                break;
            case "windows":
                map = KeyboardMaps.KWindowsToPhysicalKey;
                break;
            case "web":
                // web doesn't have int type code
                return -1;
            default:
                throw new InvalidOperationException($"Platform {platform} not supported for key simulation");
        }

        int? scanCode = null;
        foreach (int code in map.Keys)
        {
            if (key.UsbHidUsage == map[code].UsbHidUsage)
            {
                scanCode = code;
                break;
            }
        }

        DebugAssertions(scanCode != null, $"Physical key for {key} not found in {platform} scanCode map");
        return scanCode!.Value;
    }

    private static int GetKeyCode(LogicalKeyboardKey key, string platform)
    {
        DebugAssertions(OsIsSupported(platform), $"Platform {platform} not supported for key simulation");
        if (Constants.KIsWeb)
        {
            // web doesn't have int type code. This check is used to treeshake
            // keyboard map code.
            return -1;
        }

        long? keyCode = null;
        switch (platform)
        {
            case "android":
                keyCode = FindCode(KeyboardMaps.KAndroidToLogicalKey, key);
                break;
            case "fuchsia":
                foreach ((long code, LogicalKeyboardKey candidate) in KeyboardMaps.KFuchsiaToLogicalKey)
                {
                    if (key.KeyId == candidate.KeyId)
                    {
                        keyCode = code;
                        break;
                    }
                }

                break;
            case "macos":
            // macOS doesn't do key codes, just scan codes.
            case "ios":
            // iOS doesn't do key codes, just scan codes.
            case "web":
                // web doesn't have int type code.
                return -1;
            case "linux":
                keyCode = FindCode(KeyboardMaps.KGlfwToLogicalKey, key);
                break;
            case "windows":
                keyCode = FindCode(KeyboardMaps.KWindowsToLogicalKey, key);
                break;
        }

        DebugAssertions(keyCode != null, $"Key {key} not found in {platform} keyCode map");
        return unchecked((int)keyCode!.Value);
    }

    private static long? FindCode(IReadOnlyDictionary<int, LogicalKeyboardKey> map, LogicalKeyboardKey key)
    {
        foreach ((int code, LogicalKeyboardKey candidate) in map)
        {
            if (key.KeyId == candidate.KeyId)
            {
                return code;
            }
        }

        return null;
    }

    private static PhysicalKeyboardKey InferPhysicalKey(LogicalKeyboardKey key)
    {
        PhysicalKeyboardKey? result = null;
        foreach (PhysicalKeyboardKey physicalKey in PhysicalKeyboardKey.KnownPhysicalKeys)
        {
            if (physicalKey.DebugName == key.DebugName)
            {
                result = physicalKey;
                break;
            }
        }

        DebugAssertions(result != null, $"Unable to infer physical key for {key}");
        return result!;
    }

    private static (string Key, int Location) GetWebKeyLocation(LogicalKeyboardKey key, string keyLabel)
    {
        string? result = null;
        foreach ((string webKey, IReadOnlyList<LogicalKeyboardKey?> locations) in KeyboardMaps.KWebLocationMap)
        {
            int foundIndex = LastIndexOf(locations, key);
            // If foundIndex is -1, then the key is not defined in kWebLocationMap.
            // If foundIndex is 0, then the key is in the standard part of the keyboard,
            // but we have to check `keyLabel` to see if it's remapped or modified.
            if (foundIndex != -1 && foundIndex != 0)
            {
                return (webKey, foundIndex);
            }
        }

        if (keyLabel.Length > 0)
        {
            return (keyLabel, 0);
        }

        foreach (string code in KeyboardMaps.KWebToLogicalKey.Keys)
        {
            if (key.KeyId == KeyboardMaps.KWebToLogicalKey[code].KeyId)
            {
                result = code;
                break;
            }
        }

        DebugAssertions(result != null, $"Key {key} not found in web keyCode map");
        return (result!, 0);
    }

    private static int LastIndexOf(IReadOnlyList<LogicalKeyboardKey?> list, LogicalKeyboardKey key)
    {
        for (int index = list.Count - 1; index >= 0; index--)
        {
            if (key.Equals(list[index]))
            {
                return index;
            }
        }

        return -1;
    }

    private static string GetWebCode(PhysicalKeyboardKey key)
    {
        string? result = null;
        foreach ((string code, PhysicalKeyboardKey candidate) in KeyboardMaps.KWebToPhysicalKey)
        {
            if (candidate.UsbHidUsage == key.UsbHidUsage)
            {
                result = code;
                break;
            }
        }

        DebugAssertions(result != null, $"Key {key} not found in web code map");
        return result!;
    }

    private static PhysicalKeyboardKey FindPhysicalKeyByPlatform(LogicalKeyboardKey key, string platform)
    {
        DebugAssertions(OsIsSupported(platform), $"Platform {platform} not supported for key simulation");
        IEnumerable<PhysicalKeyboardKey> map;
        if (Constants.KIsWeb)
        {
            // This check is used to treeshake keymap code.
            map = KeyboardMaps.KWebToPhysicalKey.Values;
        }
        else
        {
            map = platform switch
            {
                "android" => KeyboardMaps.KAndroidToPhysicalKey.Values,
                "fuchsia" => KeyboardMaps.KFuchsiaToPhysicalKey.Values,
                "macos" => KeyboardMaps.KMacOsToPhysicalKey.Values,
                "ios" => KeyboardMaps.KIosToPhysicalKey.Values,
                "linux" => KeyboardMaps.KLinuxToPhysicalKey.Values,
                "web" => KeyboardMaps.KWebToPhysicalKey.Values,
                "windows" => KeyboardMaps.KWindowsToPhysicalKey.Values,
                _ => throw new InvalidOperationException($"Platform {platform} not supported"),
            };
        }

        PhysicalKeyboardKey? result = null;
        foreach (PhysicalKeyboardKey physicalKey in map)
        {
            if (physicalKey.DebugName == key.DebugName)
            {
                result = physicalKey;
                break;
            }
        }

        DebugAssertions(result != null, $"Physical key for {key} not found in {platform} physical key map");
        return result!;
    }

    /// <summary>
    /// Get a raw key data map given a <see cref="LogicalKeyboardKey"/> and a platform.
    /// </summary>
    public static Dictionary<string, object?> GetKeyData(
        LogicalKeyboardKey key,
        string platform,
        bool isDown = true,
        PhysicalKeyboardKey? physicalKey = null,
        string? character = null)
    {
        DebugAssertions(OsIsSupported(platform), $"Platform {platform} not supported for key simulation");

        key = GetKeySynonym(key);

        // Find a suitable physical key if none was supplied.
        physicalKey ??= FindPhysicalKeyByPlatform(key, platform);

        var result = new Dictionary<string, object?>
        {
            ["type"] = isDown ? "keydown" : "keyup",
            ["keymap"] = platform,
        };

        string resultCharacter = character ?? KeyLabel(key) ?? string.Empty;
        void AssignWeb()
        {
            (string webKey, int location) = GetWebKeyLocation(key, resultCharacter);
            PhysicalKeyboardKey actualPhysicalKey = physicalKey ?? InferPhysicalKey(key);
            result["code"] = GetWebCode(actualPhysicalKey);
            result["key"] = webKey;
            result["location"] = location;
            result["metaState"] = GetWebModifierFlags(key, isDown);
        }

        if (Constants.KIsWeb)
        {
            AssignWeb();
            return result;
        }

        int keyCode = GetKeyCode(key, platform);
        int scanCode = GetScanCode(physicalKey, platform);

        switch (platform)
        {
            case "android":
                result["keyCode"] = keyCode;
                if (resultCharacter.Length > 0)
                {
                    result["codePoint"] = (int)resultCharacter[0];
                    result["character"] = resultCharacter;
                }

                result["scanCode"] = scanCode;
                result["metaState"] = GetAndroidModifierFlags(key, isDown);
                break;
            case "fuchsia":
                result["hidUsage"] = unchecked((int)physicalKey.UsbHidUsage);
                if (resultCharacter.Length > 0)
                {
                    result["codePoint"] = (int)resultCharacter[0];
                }

                result["modifiers"] = GetFuchsiaModifierFlags(key, isDown);
                break;
            case "linux":
                result["toolkit"] = "glfw";
                result["keyCode"] = keyCode;
                result["scanCode"] = scanCode;
                result["modifiers"] = GetGlfwModifierFlags(key, isDown);
                result["unicodeScalarValues"] = resultCharacter.Length > 0 ? (int)resultCharacter[0] : 0;
                break;
            case "macos":
                result["keyCode"] = scanCode;
                if (resultCharacter.Length > 0)
                {
                    result["characters"] = resultCharacter;
                    result["charactersIgnoringModifiers"] = resultCharacter;
                }

                result["modifiers"] = GetMacOsModifierFlags(key, isDown);
                break;
            case "ios":
                result["keyCode"] = scanCode;
                result["characters"] = resultCharacter;
                result["charactersIgnoringModifiers"] = resultCharacter;
                result["modifiers"] = GetIosModifierFlags(key, isDown);
                break;
            case "windows":
                result["keyCode"] = keyCode;
                result["scanCode"] = scanCode;
                if (resultCharacter.Length > 0)
                {
                    result["characterCodePoint"] = (int)resultCharacter[0];
                }

                result["modifiers"] = GetWindowsModifierFlags(key, isDown);
                break;
            case "web":
                AssignWeb();
                break;
        }

        return result;
    }

    private static HashSet<LogicalKeyboardKey> PressedAfter(LogicalKeyboardKey newKey, bool isDown)
    {
        var pressed = new HashSet<LogicalKeyboardKey>(RawKeyboard.Instance.KeysPressed);
        if (isDown)
        {
            pressed.Add(newKey);
        }
        else
        {
            pressed.Remove(newKey);
        }

        return pressed;
    }

    private static int GetAndroidModifierFlags(LogicalKeyboardKey newKey, bool isDown)
    {
        int result = 0;
        HashSet<LogicalKeyboardKey> pressed = PressedAfter(newKey, isDown);
        if (pressed.Contains(LogicalKeyboardKey.ShiftLeft))
        {
            result |= RawKeyEventDataAndroid.ModifierLeftShift | RawKeyEventDataAndroid.ModifierShift;
        }

        if (pressed.Contains(LogicalKeyboardKey.ShiftRight))
        {
            result |= RawKeyEventDataAndroid.ModifierRightShift | RawKeyEventDataAndroid.ModifierShift;
        }

        if (pressed.Contains(LogicalKeyboardKey.MetaLeft))
        {
            result |= RawKeyEventDataAndroid.ModifierLeftMeta | RawKeyEventDataAndroid.ModifierMeta;
        }

        if (pressed.Contains(LogicalKeyboardKey.MetaRight))
        {
            result |= RawKeyEventDataAndroid.ModifierRightMeta | RawKeyEventDataAndroid.ModifierMeta;
        }

        if (pressed.Contains(LogicalKeyboardKey.ControlLeft))
        {
            result |= RawKeyEventDataAndroid.ModifierLeftControl | RawKeyEventDataAndroid.ModifierControl;
        }

        if (pressed.Contains(LogicalKeyboardKey.ControlRight))
        {
            result |= RawKeyEventDataAndroid.ModifierRightControl | RawKeyEventDataAndroid.ModifierControl;
        }

        if (pressed.Contains(LogicalKeyboardKey.AltLeft))
        {
            result |= RawKeyEventDataAndroid.ModifierLeftAlt | RawKeyEventDataAndroid.ModifierAlt;
        }

        if (pressed.Contains(LogicalKeyboardKey.AltRight))
        {
            result |= RawKeyEventDataAndroid.ModifierRightAlt | RawKeyEventDataAndroid.ModifierAlt;
        }

        if (pressed.Contains(LogicalKeyboardKey.Fn))
        {
            result |= RawKeyEventDataAndroid.ModifierFunction;
        }

        if (pressed.Contains(LogicalKeyboardKey.ScrollLock))
        {
            result |= RawKeyEventDataAndroid.ModifierScrollLock;
        }

        if (pressed.Contains(LogicalKeyboardKey.NumLock))
        {
            result |= RawKeyEventDataAndroid.ModifierNumLock;
        }

        if (pressed.Contains(LogicalKeyboardKey.CapsLock))
        {
            result |= RawKeyEventDataAndroid.ModifierCapsLock;
        }

        return result;
    }

    private static int GetGlfwModifierFlags(LogicalKeyboardKey newKey, bool isDown)
    {
        int result = 0;
        HashSet<LogicalKeyboardKey> pressed = PressedAfter(newKey, isDown);
        if (pressed.Contains(LogicalKeyboardKey.ShiftLeft) || pressed.Contains(LogicalKeyboardKey.ShiftRight))
        {
            result |= GLFWKeyHelper.ModifierShift;
        }

        if (pressed.Contains(LogicalKeyboardKey.MetaLeft) || pressed.Contains(LogicalKeyboardKey.MetaRight))
        {
            result |= GLFWKeyHelper.ModifierMeta;
        }

        if (pressed.Contains(LogicalKeyboardKey.ControlLeft) || pressed.Contains(LogicalKeyboardKey.ControlRight))
        {
            result |= GLFWKeyHelper.ModifierControl;
        }

        if (pressed.Contains(LogicalKeyboardKey.AltLeft) || pressed.Contains(LogicalKeyboardKey.AltRight))
        {
            result |= GLFWKeyHelper.ModifierAlt;
        }

        if (pressed.Contains(LogicalKeyboardKey.CapsLock))
        {
            result |= GLFWKeyHelper.ModifierCapsLock;
        }

        return result;
    }

    private static int GetWindowsModifierFlags(LogicalKeyboardKey newKey, bool isDown)
    {
        int result = 0;
        HashSet<LogicalKeyboardKey> pressed = PressedAfter(newKey, isDown);
        if (pressed.Contains(LogicalKeyboardKey.Shift))
        {
            result |= RawKeyEventDataWindows.ModifierShift;
        }

        if (pressed.Contains(LogicalKeyboardKey.ShiftLeft))
        {
            result |= RawKeyEventDataWindows.ModifierLeftShift;
        }

        if (pressed.Contains(LogicalKeyboardKey.ShiftRight))
        {
            result |= RawKeyEventDataWindows.ModifierRightShift;
        }

        if (pressed.Contains(LogicalKeyboardKey.MetaLeft))
        {
            result |= RawKeyEventDataWindows.ModifierLeftMeta;
        }

        if (pressed.Contains(LogicalKeyboardKey.MetaRight))
        {
            result |= RawKeyEventDataWindows.ModifierRightMeta;
        }

        if (pressed.Contains(LogicalKeyboardKey.Control))
        {
            result |= RawKeyEventDataWindows.ModifierControl;
        }

        if (pressed.Contains(LogicalKeyboardKey.ControlLeft))
        {
            result |= RawKeyEventDataWindows.ModifierLeftControl;
        }

        if (pressed.Contains(LogicalKeyboardKey.ControlRight))
        {
            result |= RawKeyEventDataWindows.ModifierRightControl;
        }

        if (pressed.Contains(LogicalKeyboardKey.Alt))
        {
            result |= RawKeyEventDataWindows.ModifierAlt;
        }

        if (pressed.Contains(LogicalKeyboardKey.AltLeft))
        {
            result |= RawKeyEventDataWindows.ModifierLeftAlt;
        }

        if (pressed.Contains(LogicalKeyboardKey.AltRight))
        {
            result |= RawKeyEventDataWindows.ModifierRightAlt;
        }

        if (pressed.Contains(LogicalKeyboardKey.CapsLock))
        {
            result |= RawKeyEventDataWindows.ModifierCaps;
        }

        if (pressed.Contains(LogicalKeyboardKey.NumLock))
        {
            result |= RawKeyEventDataWindows.ModifierNumLock;
        }

        if (pressed.Contains(LogicalKeyboardKey.ScrollLock))
        {
            result |= RawKeyEventDataWindows.ModifierScrollLock;
        }

        return result;
    }

    private static int GetFuchsiaModifierFlags(LogicalKeyboardKey newKey, bool isDown)
    {
        int result = 0;
        HashSet<LogicalKeyboardKey> pressed = PressedAfter(newKey, isDown);
        if (pressed.Contains(LogicalKeyboardKey.ShiftLeft))
        {
            result |= RawKeyEventDataFuchsia.ModifierLeftShift;
        }

        if (pressed.Contains(LogicalKeyboardKey.ShiftRight))
        {
            result |= RawKeyEventDataFuchsia.ModifierRightShift;
        }

        if (pressed.Contains(LogicalKeyboardKey.MetaLeft))
        {
            result |= RawKeyEventDataFuchsia.ModifierLeftMeta;
        }

        if (pressed.Contains(LogicalKeyboardKey.MetaRight))
        {
            result |= RawKeyEventDataFuchsia.ModifierRightMeta;
        }

        if (pressed.Contains(LogicalKeyboardKey.ControlLeft))
        {
            result |= RawKeyEventDataFuchsia.ModifierLeftControl;
        }

        if (pressed.Contains(LogicalKeyboardKey.ControlRight))
        {
            result |= RawKeyEventDataFuchsia.ModifierRightControl;
        }

        if (pressed.Contains(LogicalKeyboardKey.AltLeft))
        {
            result |= RawKeyEventDataFuchsia.ModifierLeftAlt;
        }

        if (pressed.Contains(LogicalKeyboardKey.AltRight))
        {
            result |= RawKeyEventDataFuchsia.ModifierRightAlt;
        }

        if (pressed.Contains(LogicalKeyboardKey.CapsLock))
        {
            result |= RawKeyEventDataFuchsia.ModifierCapsLock;
        }

        return result;
    }

    private static int GetWebModifierFlags(LogicalKeyboardKey newKey, bool isDown)
    {
        int result = 0;
        HashSet<LogicalKeyboardKey> pressed = PressedAfter(newKey, isDown);
        if (pressed.Contains(LogicalKeyboardKey.ShiftLeft) || pressed.Contains(LogicalKeyboardKey.ShiftRight))
        {
            result |= RawKeyEventDataWeb.ModifierShift;
        }

        if (pressed.Contains(LogicalKeyboardKey.MetaLeft) || pressed.Contains(LogicalKeyboardKey.MetaRight))
        {
            result |= RawKeyEventDataWeb.ModifierMeta;
        }

        if (pressed.Contains(LogicalKeyboardKey.ControlLeft) || pressed.Contains(LogicalKeyboardKey.ControlRight))
        {
            result |= RawKeyEventDataWeb.ModifierControl;
        }

        if (pressed.Contains(LogicalKeyboardKey.AltLeft) || pressed.Contains(LogicalKeyboardKey.AltRight))
        {
            result |= RawKeyEventDataWeb.ModifierAlt;
        }

        if (pressed.Contains(LogicalKeyboardKey.CapsLock))
        {
            result |= RawKeyEventDataWeb.ModifierCapsLock;
        }

        if (pressed.Contains(LogicalKeyboardKey.NumLock))
        {
            result |= RawKeyEventDataWeb.ModifierNumLock;
        }

        if (pressed.Contains(LogicalKeyboardKey.ScrollLock))
        {
            result |= RawKeyEventDataWeb.ModifierScrollLock;
        }

        return result;
    }

    private static int GetMacOsModifierFlags(LogicalKeyboardKey newKey, bool isDown) =>
        GetAppleModifierFlags(
            newKey,
            isDown,
            RawKeyEventDataMacOs.ModifierShift,
            RawKeyEventDataMacOs.ModifierLeftShift,
            RawKeyEventDataMacOs.ModifierRightShift,
            RawKeyEventDataMacOs.ModifierCommand,
            RawKeyEventDataMacOs.ModifierLeftCommand,
            RawKeyEventDataMacOs.ModifierRightCommand,
            RawKeyEventDataMacOs.ModifierControl,
            RawKeyEventDataMacOs.ModifierLeftControl,
            RawKeyEventDataMacOs.ModifierRightControl,
            RawKeyEventDataMacOs.ModifierOption,
            RawKeyEventDataMacOs.ModifierLeftOption,
            RawKeyEventDataMacOs.ModifierRightOption,
            RawKeyEventDataMacOs.ModifierFunction,
            RawKeyEventDataMacOs.ModifierNumericPad,
            RawKeyEventDataMacOs.ModifierCapsLock);

    private static int GetIosModifierFlags(LogicalKeyboardKey newKey, bool isDown) =>
        GetAppleModifierFlags(
            newKey,
            isDown,
            RawKeyEventDataIos.ModifierShift,
            RawKeyEventDataIos.ModifierLeftShift,
            RawKeyEventDataIos.ModifierRightShift,
            RawKeyEventDataIos.ModifierCommand,
            RawKeyEventDataIos.ModifierLeftCommand,
            RawKeyEventDataIos.ModifierRightCommand,
            RawKeyEventDataIos.ModifierControl,
            RawKeyEventDataIos.ModifierLeftControl,
            RawKeyEventDataIos.ModifierRightControl,
            RawKeyEventDataIos.ModifierOption,
            RawKeyEventDataIos.ModifierLeftOption,
            RawKeyEventDataIos.ModifierRightOption,
            RawKeyEventDataIos.ModifierFunction,
            RawKeyEventDataIos.ModifierNumericPad,
            RawKeyEventDataIos.ModifierCapsLock);

    // `_getMacOsModifierFlags` and `_getIOSModifierFlags` are the same function over each platform's
    // constants.
    private static int GetAppleModifierFlags(
        LogicalKeyboardKey newKey,
        bool isDown,
        int shift,
        int leftShift,
        int rightShift,
        int command,
        int leftCommand,
        int rightCommand,
        int control,
        int leftControl,
        int rightControl,
        int option,
        int leftOption,
        int rightOption,
        int function,
        int numericPad,
        int capsLock)
    {
        int result = 0;
        HashSet<LogicalKeyboardKey> pressed = PressedAfter(newKey, isDown);
        if (pressed.Contains(LogicalKeyboardKey.ShiftLeft))
        {
            result |= leftShift | shift;
        }

        if (pressed.Contains(LogicalKeyboardKey.ShiftRight))
        {
            result |= rightShift | shift;
        }

        if (pressed.Contains(LogicalKeyboardKey.MetaLeft))
        {
            result |= leftCommand | command;
        }

        if (pressed.Contains(LogicalKeyboardKey.MetaRight))
        {
            result |= rightCommand | command;
        }

        if (pressed.Contains(LogicalKeyboardKey.ControlLeft))
        {
            result |= leftControl | control;
        }

        if (pressed.Contains(LogicalKeyboardKey.ControlRight))
        {
            result |= rightControl | control;
        }

        if (pressed.Contains(LogicalKeyboardKey.AltLeft))
        {
            result |= leftOption | option;
        }

        if (pressed.Contains(LogicalKeyboardKey.AltRight))
        {
            result |= rightOption | option;
        }

        HashSet<LogicalKeyboardKey> functionKeys =
        [
            LogicalKeyboardKey.F1, LogicalKeyboardKey.F2, LogicalKeyboardKey.F3, LogicalKeyboardKey.F4,
            LogicalKeyboardKey.F5, LogicalKeyboardKey.F6, LogicalKeyboardKey.F7, LogicalKeyboardKey.F8,
            LogicalKeyboardKey.F9, LogicalKeyboardKey.F10, LogicalKeyboardKey.F11, LogicalKeyboardKey.F12,
            LogicalKeyboardKey.F13, LogicalKeyboardKey.F14, LogicalKeyboardKey.F15, LogicalKeyboardKey.F16,
            LogicalKeyboardKey.F17, LogicalKeyboardKey.F18, LogicalKeyboardKey.F19, LogicalKeyboardKey.F20,
            LogicalKeyboardKey.F21,
        ];
        if (pressed.Overlaps(functionKeys))
        {
            result |= function;
        }

        if (pressed.Overlaps(KeyboardMaps.KMacOsNumPadMap.Values))
        {
            result |= numericPad;
        }

        if (pressed.Contains(LogicalKeyboardKey.CapsLock))
        {
            result |= capsLock;
        }

        return result;
    }

    private static bool SimulateKeyEventByRawEvent(Func<Dictionary<string, object?>> buildKeyData)
    {
        bool? result = null;
        Task delivery = ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
            SystemChannels.KeyEvent.Name,
            SystemChannels.KeyEvent.Codec.EncodeMessage(buildKeyData()),
            data =>
            {
                if (data == null)
                {
                    result = false;
                    return;
                }

                var decoded = (IReadOnlyDictionary<string, object?>)SystemChannels.KeyEvent.Codec.DecodeMessage(data)!;
                result = (bool)decoded["handled"]!;
            });
        // A handler that throws faults the delivery; surface it like Dart's awaited future does.
        delivery.GetAwaiter().GetResult();
        return result ?? throw new InvalidOperationException("The key event message was not answered synchronously.");
    }

    private static readonly Lazy<Dictionary<string, PhysicalKeyboardKey>> DebugNameToPhysicalKey = new(() =>
    {
        var result = new Dictionary<string, PhysicalKeyboardKey>();
        foreach (PhysicalKeyboardKey key in PhysicalKeyboardKey.KnownPhysicalKeys)
        {
            string debugName = key.DebugName;
            result.TryAdd(debugName, key);
        }

        return result;
    });

    private static PhysicalKeyboardKey FindPhysicalKey(LogicalKeyboardKey key)
    {
        PhysicalKeyboardKey? result = DebugNameToPhysicalKey.Value.GetValueOrDefault(key.DebugName);
        DebugAssertions(result != null, $"Physical key for {key} not found in known physical keys");
        return result!;
    }

    private const KeyDataTransitMode DefaultTransitMode = KeyDataTransitMode.KeyDataThenRawKeyData;

    // The simulation transit mode for [simulateKeyDownEvent], [simulateKeyUpEvent],
    // and [simulateKeyRepeatEvent].
    //
    // Simulation transit mode is the mode that simulated key events are constructed
    // and delivered. For detailed introduction, see [KeyDataTransitMode] and
    // its values.
    //
    // The `_transitMode` defaults to [KeyDataTransitMode.keyDataThenRawKeyData], and can
    // be overridden with [debugKeyEventSimulatorTransitModeOverride]. In widget tests, it
    // is often set with [KeySimulationModeVariant].
    private static KeyDataTransitMode TransitModeValue =>
        KeyboardDebug.DebugKeyEventSimulatorTransitModeOverride ?? DefaultTransitMode;

    /// <summary>The simulation transit mode the simulator currently uses.</summary>
    public static KeyDataTransitMode TransitMode => TransitModeValue;

    private static string DefaultPlatform =>
        Constants.KIsWeb ? "web" : Diagnostics.EnumName(PlatformDefaults.TargetPlatform).ToLowerInvariant();

    /// <summary>
    /// Simulates sending a hardware key down event.
    /// </summary>
    /// <remarks>
    /// This only simulates key presses coming from a physical keyboard, not from a soft keyboard.
    /// Specify <paramref name="platform"/> to simulate the key event on a specific platform; it
    /// defaults to <see cref="PlatformDefaults.TargetPlatform"/>.
    /// </remarks>
    public static bool SimulateKeyDownEvent(
        LogicalKeyboardKey key,
        string? platform = null,
        PhysicalKeyboardKey? physicalKey = null,
        string? character = null)
    {
        bool SimulateByRawEvent() => SimulateKeyEventByRawEvent(() =>
        {
            platform ??= DefaultPlatform;
            return GetKeyData(key, platform, physicalKey: physicalKey, character: character);
        });

        switch (TransitModeValue)
        {
            case KeyDataTransitMode.RawKeyData:
                return SimulateByRawEvent();
            default:
                LogicalKeyboardKey logicalKey = GetKeySynonym(key);
                bool resultByKeyEvent = ServicesBinding.Instance.KeyEventManager.HandleKeyData(new KeyData(
                    type: KeyEventType.Down,
                    physical: (physicalKey ?? FindPhysicalKey(logicalKey)).UsbHidUsage,
                    logical: logicalKey.KeyId,
                    timeStamp: TimeSpan.Zero,
                    character: character ?? KeyLabel(key),
                    synthesized: false));
                return SimulateByRawEvent() || resultByKeyEvent;
        }
    }

    /// <summary>
    /// Simulates sending a hardware key up event through the system channel.
    /// </summary>
    public static bool SimulateKeyUpEvent(
        LogicalKeyboardKey key,
        string? platform = null,
        PhysicalKeyboardKey? physicalKey = null)
    {
        bool SimulateByRawEvent() => SimulateKeyEventByRawEvent(() =>
        {
            platform ??= DefaultPlatform;
            return GetKeyData(key, platform, isDown: false, physicalKey: physicalKey);
        });

        switch (TransitModeValue)
        {
            case KeyDataTransitMode.RawKeyData:
                return SimulateByRawEvent();
            default:
                LogicalKeyboardKey logicalKey = GetKeySynonym(key);
                bool resultByKeyEvent = ServicesBinding.Instance.KeyEventManager.HandleKeyData(new KeyData(
                    type: KeyEventType.Up,
                    physical: (physicalKey ?? FindPhysicalKey(logicalKey)).UsbHidUsage,
                    logical: logicalKey.KeyId,
                    timeStamp: TimeSpan.Zero,
                    character: null,
                    synthesized: false));
                return SimulateByRawEvent() || resultByKeyEvent;
        }
    }

    /// <summary>
    /// Simulates sending a hardware key repeat event through the system channel.
    /// </summary>
    public static bool SimulateKeyRepeatEvent(
        LogicalKeyboardKey key,
        string? platform = null,
        PhysicalKeyboardKey? physicalKey = null,
        string? character = null)
    {
        bool SimulateByRawEvent() => SimulateKeyEventByRawEvent(() =>
        {
            platform ??= DefaultPlatform;
            return GetKeyData(key, platform, physicalKey: physicalKey, character: character);
        });

        switch (TransitModeValue)
        {
            case KeyDataTransitMode.RawKeyData:
                return SimulateByRawEvent();
            default:
                LogicalKeyboardKey logicalKey = GetKeySynonym(key);
                bool resultByKeyEvent = ServicesBinding.Instance.KeyEventManager.HandleKeyData(new KeyData(
                    type: KeyEventType.Repeat,
                    physical: (physicalKey ?? FindPhysicalKey(logicalKey)).UsbHidUsage,
                    logical: logicalKey.KeyId,
                    timeStamp: TimeSpan.Zero,
                    character: character ?? KeyLabel(key),
                    synthesized: false));
                return SimulateByRawEvent() || resultByKeyEvent;
        }
    }

    // event_simulation.dart's top-level `_keyLabel`.
    private static string? KeyLabel(LogicalKeyboardKey key)
    {
        string keyLabel = key.KeyLabel;
        if (keyLabel.Length == 1)
        {
            return keyLabel.ToLowerInvariant();
        }

        return null;
    }

    private static void DebugAssertions(bool condition, string message)
    {
        if (Constants.KDebugMode && !condition)
        {
            throw new AssertionError(message);
        }
    }
}

/// <summary>
/// flutter_test's <c>KeySimulatorTransitModeVariant</c>: runs a test once per
/// <see cref="KeyDataTransitMode"/>. Use <see cref="All"/> (or <see cref="RawKeyData"/> /
/// <see cref="KeyDataThenRawKeyData"/>) as xunit <c>MemberData</c>, and wrap the body in
/// <see cref="Run"/>, which does the variant's <c>setUp</c>/<c>tearDown</c>.
/// </summary>
internal static class KeySimulatorTransitModeVariant
{
    public static TheoryData<KeyDataTransitMode> All =>
        new() { KeyDataTransitMode.RawKeyData, KeyDataTransitMode.KeyDataThenRawKeyData };

    public static TheoryData<KeyDataTransitMode> RawKeyData => new() { KeyDataTransitMode.RawKeyData };

    public static TheoryData<KeyDataTransitMode> KeyDataThenRawKeyData =>
        new() { KeyDataTransitMode.KeyDataThenRawKeyData };

    /// <summary>Dart's <c>describeValue</c>.</summary>
    public static string DescribeValue(KeyDataTransitMode value) => value switch
    {
        KeyDataTransitMode.RawKeyData => "RawKeyEvent",
        KeyDataTransitMode.KeyDataThenRawKeyData => "ui.KeyData then RawKeyEvent",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    /// <summary>Runs <paramref name="body"/> under <paramref name="value"/>, then tears the keyboard
    /// state down exactly as the variant does.</summary>
    public static void Run(KeyDataTransitMode value, Action body)
    {
        KeyDataTransitMode? previous = KeyboardDebug.DebugKeyEventSimulatorTransitModeOverride;
        KeyboardDebug.DebugKeyEventSimulatorTransitModeOverride = value;
        try
        {
            body();
        }
        finally
        {
            RawKeyboard.Instance.ClearKeysPressed();
            HardwareKeyboard.Instance.ClearState();
            ServicesBinding.Instance.KeyEventManager.ClearState();
            KeyboardDebug.DebugKeyEventSimulatorTransitModeOverride = previous;
        }
    }
}
#pragma warning restore CS0618
