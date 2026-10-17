// Dart parity source: flutter/packages/flutter/test/services/raw_keyboard_test.dart (RawKeyboard group)

using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Each Dart `testWidgets(..., variant: KeySimulatorTransitModeVariant.x())` is a theory over the
// variant's values whose body runs inside `KeySimulatorTransitModeVariant.Run` (the variant's
// setUp/tearDown). Tests without a variant get flutter_test's per-test keyboard reset from the class
// constructor/Dispose. `tester.binding.defaultBinaryMessenger.handlePlatformMessage` is
// `ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage`.
//
// Not ported, because they only run on the web (`skip: !isBrowser`) and Plumix's `kIsWeb` is false:
// - 'CapsLock should not be release when remapped on Web'
// - 'Allows inconsistent modifier for Web - Alt graph'
// - 'Allows inconsistent modifier for Web - Alt right'

namespace Plumix.Tests;

#pragma warning disable CS0618 // The raw keyboard API is deprecated in Flutter; these tests cover it.
public sealed class RawKeyboardDartParityTests : IDisposable
{
    public RawKeyboardDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        RawKeyboard.Instance.KeyEventHandler = null;
        FocusManager.Instance.ResetForTests();
    }

    public static TheoryData<KeyDataTransitMode> RawKeyDataVariant => KeySimulatorTransitModeVariant.RawKeyData;

    public static TheoryData<KeyDataTransitMode> AllVariant => KeySimulatorTransitModeVariant.All;

    [Theory]
    [MemberData(nameof(RawKeyDataVariant))]
    public void TheCorrectCharacterIsProduced(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            foreach (string platform in new[] { "linux", "android", "macos", "fuchsia", "windows", "ios" })
            {
                string character = string.Empty;

                void HandleKey(RawKeyEvent @event)
                {
                    Assert.True(@event.Character == character, $"on {platform}");
                }

                RawKeyboard.Instance.AddListener(HandleKey);
                character = "a";
                KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA, platform: platform);
                character = "`";
                KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Backquote, platform: platform);
                RawKeyboard.Instance.RemoveListener(HandleKey);
            }
        });

    [Theory]
    [MemberData(nameof(RawKeyDataVariant))]
    public void NoCharacterIsProducedForNonPrintables(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            foreach (string platform in new[] { "linux", "android", "macos", "fuchsia", "windows", "web", "ios" })
            {
                void HandleKey(RawKeyEvent @event)
                {
                    Assert.True(@event.Character == null, $"on {platform}");
                }

                RawKeyboard.Instance.AddListener(HandleKey);
                KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.ShiftLeft, platform: platform);
                RawKeyboard.Instance.RemoveListener(HandleKey);
            }
        });

    [Theory]
    [MemberData(nameof(RawKeyDataVariant))]
    public void KeysPressedIsMaintained(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            foreach (string platform in new[] { "linux", "android", "macos", "fuchsia", "windows", "ios" })
            {
                bool linux = platform == "linux";
                RawKeyboard.Instance.ClearKeysPressed();
                ExpectKeysPressed([], platform);
                KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.ShiftLeft, platform: platform);
                ExpectKeysPressed(
                    // Linux doesn't have a concept of left/right keys, so they're all
                    // shown as down when either is pressed.
                    [LogicalKeyboardKey.ShiftLeft, .. When(linux, LogicalKeyboardKey.ShiftRight)],
                    platform);
                KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.ControlLeft, platform: platform);
                ExpectKeysPressed(
                    [
                        LogicalKeyboardKey.ShiftLeft,
                        .. When(linux, LogicalKeyboardKey.ShiftRight),
                        LogicalKeyboardKey.ControlLeft,
                        .. When(linux, LogicalKeyboardKey.ControlRight),
                    ],
                    platform);
                KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA, platform: platform);
                ExpectKeysPressed(
                    [
                        LogicalKeyboardKey.ShiftLeft,
                        .. When(linux, LogicalKeyboardKey.ShiftRight),
                        LogicalKeyboardKey.ControlLeft,
                        .. When(linux, LogicalKeyboardKey.ControlRight),
                        LogicalKeyboardKey.KeyA,
                    ],
                    platform);
                KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.KeyA, platform: platform);
                ExpectKeysPressed(
                    [
                        LogicalKeyboardKey.ShiftLeft,
                        .. When(linux, LogicalKeyboardKey.ShiftRight),
                        LogicalKeyboardKey.ControlLeft,
                        .. When(linux, LogicalKeyboardKey.ControlRight),
                    ],
                    platform);
                KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.ControlLeft, platform: platform);
                ExpectKeysPressed(
                    [LogicalKeyboardKey.ShiftLeft, .. When(linux, LogicalKeyboardKey.ShiftRight)],
                    platform);
                KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.ShiftLeft, platform: platform);
                ExpectKeysPressed([], platform);
                // The Fn key isn't mapped on linux or Windows.
                if (platform != "linux" && platform != "windows" && platform != "ios")
                {
                    bool macos = platform == "macos";
                    KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Fn, platform: platform);
                    ExpectKeysPressed([.. When(!macos, LogicalKeyboardKey.Fn)], platform);
                    KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.F12, platform: platform);
                    ExpectKeysPressed([.. When(!macos, LogicalKeyboardKey.Fn), LogicalKeyboardKey.F12], platform);
                    KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Fn, platform: platform);
                    ExpectKeysPressed([LogicalKeyboardKey.F12], platform);
                    KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.F12, platform: platform);
                    ExpectKeysPressed([], platform);
                }
            }
        });

    [Theory]
    [MemberData(nameof(RawKeyDataVariant))]
    public void KeysPressedIsCorrectWhenModifierIsReleasedBeforeKey(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            foreach (string platform in new[] { "linux", "android", "macos", "fuchsia", "windows", "ios" })
            {
                bool linux = platform == "linux";
                RawKeyboard.Instance.ClearKeysPressed();
                ExpectKeysPressed([], platform);
                KeyEventSimulator.SimulateKeyDownEvent(
                    LogicalKeyboardKey.ShiftLeft,
                    platform: platform,
                    physicalKey: PhysicalKeyboardKey.ShiftLeft);
                ExpectKeysPressed(
                    // Linux doesn't have a concept of left/right keys, so they're all
                    // shown as down when either is pressed.
                    [LogicalKeyboardKey.ShiftLeft, .. When(linux, LogicalKeyboardKey.ShiftRight)],
                    platform);
                // TODO(gspencergoog): Switch to capital A when the new key event code
                // is finished that can simulate real keys.
                // https://github.com/flutter/flutter/issues/33521
                // This should really be done with a simulated capital A, but the event
                // simulation code doesn't really support that, since it only can
                // simulate events that appear in the key maps (and capital letters
                // don't appear there).
                KeyEventSimulator.SimulateKeyDownEvent(
                    LogicalKeyboardKey.KeyA,
                    platform: platform,
                    physicalKey: PhysicalKeyboardKey.KeyA);
                ExpectKeysPressed(
                    [
                        LogicalKeyboardKey.ShiftLeft,
                        .. When(linux, LogicalKeyboardKey.ShiftRight),
                        LogicalKeyboardKey.KeyA,
                    ],
                    platform);
                KeyEventSimulator.SimulateKeyUpEvent(
                    LogicalKeyboardKey.ShiftLeft,
                    platform: platform,
                    physicalKey: PhysicalKeyboardKey.ShiftLeft);
                ExpectKeysPressed([LogicalKeyboardKey.KeyA], platform);
                KeyEventSimulator.SimulateKeyUpEvent(
                    LogicalKeyboardKey.KeyA,
                    platform: platform,
                    physicalKey: PhysicalKeyboardKey.KeyA);
                ExpectKeysPressed([], platform);
            }
        });

    [Theory]
    [MemberData(nameof(RawKeyDataVariant))]
    public void KeysPressedModifiersAreSynchronizedWithKeyEventsOnMacOS(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            Assert.Empty(RawKeyboard.Instance.KeysPressed);
            // Generate the data for a regular key down event.
            Dictionary<string, object?> data = KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "macos");
            // Change the modifiers so that they show the shift key as already down
            // when this event is received, but it's not in keysPressed yet.
            data["modifiers"] = (int)data["modifiers"]!
                                | RawKeyEventDataMacOs.ModifierLeftShift
                                | RawKeyEventDataMacOs.ModifierShift;
            // dispatch the modified data.
            HandlePlatformMessage(data);
            ExpectKeysPressed([LogicalKeyboardKey.ShiftLeft, LogicalKeyboardKey.KeyA]);
        });

    [Theory]
    [MemberData(nameof(RawKeyDataVariant))]
    public void KeysPressedModifiersAreSynchronizedWithKeyEventsOnIOS(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            Assert.Empty(RawKeyboard.Instance.KeysPressed);
            // Generate the data for a regular key down event.
            Dictionary<string, object?> data = KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "ios");
            // Change the modifiers so that they show the shift key as already down
            // when this event is received, but it's not in keysPressed yet.
            data["modifiers"] = (int)data["modifiers"]!
                                | RawKeyEventDataMacOs.ModifierLeftShift
                                | RawKeyEventDataMacOs.ModifierShift;
            // dispatch the modified data.
            HandlePlatformMessage(data);
            ExpectKeysPressed([LogicalKeyboardKey.ShiftLeft, LogicalKeyboardKey.KeyA]);
        });

    [Theory]
    [MemberData(nameof(RawKeyDataVariant))]
    public void KeysPressedModifiersAreSynchronizedWithKeyEventsOnWindows(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            Assert.Empty(RawKeyboard.Instance.KeysPressed);
            // Generate the data for a regular key down event.
            Dictionary<string, object?> data =
                KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "windows");
            // Change the modifiers so that they show the shift key as already down
            // when this event is received, but it's not in keysPressed yet.
            data["modifiers"] = (int)data["modifiers"]!
                                | RawKeyEventDataWindows.ModifierLeftShift
                                | RawKeyEventDataWindows.ModifierShift;
            // dispatch the modified data.
            HandlePlatformMessage(data);
            ExpectKeysPressed([LogicalKeyboardKey.ShiftLeft, LogicalKeyboardKey.KeyA]);
        });

    [Theory]
    [MemberData(nameof(RawKeyDataVariant))]
    public void KeysPressedModifiersAreSynchronizedWithKeyEventsOnAndroid(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            Assert.Empty(RawKeyboard.Instance.KeysPressed);
            // Generate the data for a regular key down event.
            Dictionary<string, object?> data =
                KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "android");
            // Change the modifiers so that they show the shift key as already down
            // when this event is received, but it's not in keysPressed yet.
            data["metaState"] = (int)data["metaState"]!
                                | RawKeyEventDataAndroid.ModifierLeftShift
                                | RawKeyEventDataAndroid.ModifierShift;
            // dispatch the modified data.
            HandlePlatformMessage(data);
            ExpectKeysPressed([LogicalKeyboardKey.ShiftLeft, LogicalKeyboardKey.KeyA]);
        });

    [Theory]
    [MemberData(nameof(RawKeyDataVariant))]
    public void KeysPressedModifiersAreSynchronizedWithKeyEventsOnFuchsia(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            Assert.Empty(RawKeyboard.Instance.KeysPressed);
            // Generate the data for a regular key down event.
            Dictionary<string, object?> data =
                KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "fuchsia");
            // Change the modifiers so that they show the shift key as already down
            // when this event is received, but it's not in keysPressed yet.
            data["modifiers"] = (int)data["modifiers"]! | RawKeyEventDataFuchsia.ModifierLeftShift;
            // dispatch the modified data.
            HandlePlatformMessage(data);
            ExpectKeysPressed([LogicalKeyboardKey.ShiftLeft, LogicalKeyboardKey.KeyA]);
        });

    [Theory]
    [MemberData(nameof(RawKeyDataVariant))]
    public void KeysPressedModifiersAreSynchronizedWithKeyEventsOnLinuxGLFW(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            Assert.Empty(RawKeyboard.Instance.KeysPressed);
            // Generate the data for a regular key down event.
            Dictionary<string, object?> data = KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "linux");
            // Change the modifiers so that they show the shift key as already down
            // when this event is received, but it's not in keysPressed yet.
            data["modifiers"] = (int)data["modifiers"]! | GLFWKeyHelper.ModifierShift;
            // dispatch the modified data.
            HandlePlatformMessage(data);
            ExpectKeysPressed(
            [
                LogicalKeyboardKey.ShiftLeft,
                // Linux doesn't have a concept of left/right keys, so they're all
                // shown as down when either is pressed.
                LogicalKeyboardKey.ShiftRight,
                LogicalKeyboardKey.KeyA,
            ]);
        });

    private static void SimulateGtkKeyEvent(bool keyDown, int scancode, int keycode, int modifiers)
    {
        var data = new Dictionary<string, object?>
        {
            ["type"] = keyDown ? "keydown" : "keyup",
            ["keymap"] = "linux",
            ["toolkit"] = "gtk",
            ["scanCode"] = scancode,
            ["keyCode"] = keycode,
            ["modifiers"] = modifiers,
        };
        // Dispatch an empty key data to disable HardwareKeyboard sanity check,
        // since we're only testing if the raw keyboard can handle the message.
        // In a real application, the embedder responder will send the correct key data
        // (which is tested in the engine).
        ServicesBinding.Instance.KeyEventManager.HandleKeyData(new KeyData(
            type: KeyEventType.Down,
            timeStamp: TimeSpan.Zero,
            logical: 0,
            physical: 0,
            character: null,
            synthesized: false));
        HandlePlatformMessage(data);
    }

    // Regression test for https://github.com/flutter/flutter/issues/93278 .
    //
    // GTK has some weird behavior where the tested key event sequence will
    // result in a AltRight down event without Alt bitmask.
    [Theory]
    [MemberData(nameof(RawKeyDataVariant))]
    public void KeysPressedModifiersAreSynchronizedWithKeyEventsOnLinuxGTKDownEvents(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            Assert.Empty(RawKeyboard.Instance.KeysPressed);

            SimulateGtkKeyEvent(true, 0x6c /*AltRight*/, 0xffea /*AltRight*/, 0x2000000);
            SimulateGtkKeyEvent(true, 0x32 /*ShiftLeft*/, 0xfe08 /*NextGroup*/, 0x2000008 /*MOD3*/);
            SimulateGtkKeyEvent(false, 0x6c /*AltRight*/, 0xfe03 /*AltRight*/, 0x2002008 /*MOD3|Reserve14*/);
            SimulateGtkKeyEvent(true, 0x6c /*AltRight*/, 0xfe03 /*AltRight*/, 0x2002000 /*Reserve14*/);
            ExpectKeysPressed([LogicalKeyboardKey.AltRight]);
        });

    // Regression test for https://github.com/flutter/flutter/issues/114591 .
    //
    // On Linux, CapsLock can be remapped to a non-modifier key.
    [Theory]
    [MemberData(nameof(RawKeyDataVariant))]
    public void CapsLockShouldNotBeReleaseWhenRemappedOnLinux(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            Assert.Empty(RawKeyboard.Instance.KeysPressed);

            SimulateGtkKeyEvent(true, 0x42 /*CapsLock*/, 0xff08 /*Backspace*/, 0x2000000);
            ExpectKeysPressed([LogicalKeyboardKey.Backspace]);
        });

    [Fact]
    public void KeysPressedModifiersAreSynchronizedWithKeyEventsOnWeb()
    {
        Assert.Empty(RawKeyboard.Instance.KeysPressed);
        // Generate the data for a regular key down event. Change the modifiers so
        // that they show the shift key as already down when this event is
        // received, but it's not in keysPressed yet.
        Dictionary<string, object?> data = KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "web");
        data["metaState"] = (int)data["metaState"]! | RawKeyEventDataWeb.ModifierShift;
        // Dispatch the modified data.
        HandlePlatformMessage(data);
        ExpectKeysPressed([LogicalKeyboardKey.ShiftLeft, LogicalKeyboardKey.KeyA]);

        // Generate the data for a regular key up event. Don't set the modifiers
        // for shift so that they show the shift key as already up when this event
        // is received, and it's in keysPressed.
        Dictionary<string, object?> data2 =
            KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "web", isDown: false);
        data2["metaState"] = 0;
        // Dispatch the modified data.
        HandlePlatformMessage(data2);
        ExpectKeysPressed([]);

        // Press right modifier key
        Dictionary<string, object?> data3 =
            KeyEventSimulator.GetKeyData(LogicalKeyboardKey.ShiftRight, platform: "web");
        // (Dart modifies `data` here, not `data3`; kept as is.)
        data["metaState"] = (int)data["metaState"]! | RawKeyEventDataWeb.ModifierShift;
        // Dispatch the modified data.
        HandlePlatformMessage(data3);
        ExpectKeysPressed([LogicalKeyboardKey.ShiftRight]);

        // Release the key
        Dictionary<string, object?> data4 =
            KeyEventSimulator.GetKeyData(LogicalKeyboardKey.ShiftRight, platform: "web", isDown: false);
        data4["metaState"] = 0;
        // Dispatch the modified data.
        HandlePlatformMessage(data4);
        ExpectKeysPressed([]);
    }

    [Theory]
    [MemberData(nameof(RawKeyDataVariant))]
    public void SidedModifiersWithoutASideSetReturnAllSidesOnAndroid(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            Assert.Empty(RawKeyboard.Instance.KeysPressed);
            // Generate the data for a regular key down event.
            Dictionary<string, object?> data =
                KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "android");
            // Set only the generic "down" modifier, without setting a side.
            data["metaState"] = (int)data["metaState"]!
                                | RawKeyEventDataAndroid.ModifierShift
                                | RawKeyEventDataAndroid.ModifierAlt
                                | RawKeyEventDataAndroid.ModifierControl
                                | RawKeyEventDataAndroid.ModifierMeta;
            // dispatch the modified data.
            HandlePlatformMessage(data);
            ExpectKeysPressed(AllSidesAnd(LogicalKeyboardKey.KeyA));
        });

    [Fact]
    public void SidedModifiersWithoutASideSetReturnAllSidesOnMacOS()
    {
        Assert.Empty(RawKeyboard.Instance.KeysPressed);
        // Generate the data for a regular key down event.
        Dictionary<string, object?> data = KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "macos");
        // Set only the generic "shift down" modifier, without setting a side.
        data["modifiers"] = (int)data["modifiers"]!
                            | RawKeyEventDataMacOs.ModifierShift
                            | RawKeyEventDataMacOs.ModifierOption
                            | RawKeyEventDataMacOs.ModifierCommand
                            | RawKeyEventDataMacOs.ModifierControl;
        // dispatch the modified data.
        HandlePlatformMessage(data);
        ExpectKeysPressed(AllSidesAnd(LogicalKeyboardKey.KeyA));
    }

    [Fact]
    public void SidedModifiersWithoutASideSetReturnAllSidesOnIOS()
    {
        Assert.Empty(RawKeyboard.Instance.KeysPressed);
        // Generate the data for a regular key down event.
        Dictionary<string, object?> data = KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "ios");
        // Set only the generic "shift down" modifier, without setting a side.
        data["modifiers"] = (int)data["modifiers"]!
                            | RawKeyEventDataIos.ModifierShift
                            | RawKeyEventDataIos.ModifierOption
                            | RawKeyEventDataIos.ModifierCommand
                            | RawKeyEventDataIos.ModifierControl;
        // dispatch the modified data.
        HandlePlatformMessage(data);
        ExpectKeysPressed(AllSidesAnd(LogicalKeyboardKey.KeyA));
    }

    [Fact]
    public void RepeatEvents()
    {
        Assert.Empty(RawKeyboard.Instance.KeysPressed);
        RawKeyEvent? receivedEvent = null;
        RawKeyboard.Instance.KeyEventHandler = @event =>
        {
            receivedEvent = @event;
            return true;
        };

        // Dispatch a down event.
        Dictionary<string, object?> downData =
            KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "windows");
        HandlePlatformMessage(downData);
        Assert.False(receivedEvent!.Repeat);

        // Dispatch another down event, which should be recognized as a repeat.
        HandlePlatformMessage(downData);
        Assert.True(receivedEvent!.Repeat);

        // Dispatch an up event.
        HandlePlatformMessage(
            KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, isDown: false, platform: "windows"));
        Assert.False(receivedEvent!.Repeat);

        RawKeyboard.Instance.KeyEventHandler = null;
    }

    [Fact]
    public void SidedModifiersWithoutASideSetReturnAllSidesOnWindows()
    {
        Assert.Empty(RawKeyboard.Instance.KeysPressed);
        // Generate the data for a regular key down event.
        Dictionary<string, object?> data =
            KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "windows");
        // Set only the generic "shift down" modifier, without setting a side.
        // Windows doesn't have a concept of "either" for the Windows (meta) key.
        data["modifiers"] = (int)data["modifiers"]!
                            | RawKeyEventDataWindows.ModifierShift
                            | RawKeyEventDataWindows.ModifierAlt
                            | RawKeyEventDataWindows.ModifierControl;
        // dispatch the modified data.
        HandlePlatformMessage(data);
        ExpectKeysPressed(
        [
            LogicalKeyboardKey.ShiftLeft,
            LogicalKeyboardKey.ShiftRight,
            LogicalKeyboardKey.AltLeft,
            LogicalKeyboardKey.AltRight,
            LogicalKeyboardKey.ControlLeft,
            LogicalKeyboardKey.ControlRight,
            LogicalKeyboardKey.KeyA,
        ]);
    }

    [Fact]
    public void SidedModifiersWithoutASideSetReturnAllSidesOnLinuxGLFW()
    {
        Assert.Empty(RawKeyboard.Instance.KeysPressed);
        // Generate the data for a regular key down event.
        Dictionary<string, object?> data = KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "linux");
        // Set only the generic "shift down" modifier, without setting a side.
        // Windows doesn't have a concept of "either" for the Windows (meta) key.
        data["modifiers"] = (int)data["modifiers"]!
                            | GLFWKeyHelper.ModifierShift
                            | GLFWKeyHelper.ModifierAlt
                            | GLFWKeyHelper.ModifierControl
                            | GLFWKeyHelper.ModifierMeta;
        // dispatch the modified data.
        HandlePlatformMessage(data);
        ExpectKeysPressed(AllSidesAnd(LogicalKeyboardKey.KeyA));
    }

    [Fact]
    public void SidedModifiersWithoutASideSetReturnLeftSidesOnWeb()
    {
        Assert.Empty(RawKeyboard.Instance.KeysPressed);
        // Generate the data for a regular key down event.
        Dictionary<string, object?> data = KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "web");
        // Set only the generic "shift down" modifier, without setting a side.
        data["metaState"] = (int)data["metaState"]!
                            | RawKeyEventDataWeb.ModifierShift
                            | RawKeyEventDataWeb.ModifierAlt
                            | RawKeyEventDataWeb.ModifierControl
                            | RawKeyEventDataWeb.ModifierMeta;
        // dispatch the modified data.
        HandlePlatformMessage(data);
        ExpectKeysPressed(
        [
            LogicalKeyboardKey.ShiftLeft,
            LogicalKeyboardKey.AltLeft,
            LogicalKeyboardKey.ControlLeft,
            LogicalKeyboardKey.MetaLeft,
            LogicalKeyboardKey.KeyA,
        ]);
    }

    [Fact]
    public void RawKeyboardAssertsIfNoKeysAreInKeysPressedAfterReceivingAKeyDownEvent()
    {
        Dictionary<string, object?> keyEventMessage;
        if (Constants.KIsWeb)
        {
            keyEventMessage = new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "web",
                ["code"] = "ShiftLeft", // Left shift code
                ["metaState"] = 0x0, // No shift key metaState set!
            };
        }
        else
        {
            keyEventMessage = new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "windows",
                ["keyCode"] = 16, // Left shift key keyCode
                ["characterCodePoint"] = 0,
                ["scanCode"] = 42,
                ["modifiers"] = 0x0, // No shift key metaState set!
            };
        }

        AssertionError error = Assert.Throws<AssertionError>(() => HandlePlatformMessage(keyEventMessage));
        Assert.Contains(
            "Attempted to send a key down event when no keys are in keysPressed",
            error.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void AllowsInconsistentModifierForIOS()
    {
        // Use `testWidgets` for clean-ups.
        var events = new List<RawKeyEvent>();
        RawKeyboard.Instance.AddListener(events.Add);
        try
        {
            HandlePlatformMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "ios",
                ["keyCode"] = 0x00000039,
                ["characters"] = string.Empty,
                ["charactersIgnoringModifiers"] = string.Empty,
                ["modifiers"] = 0,
            });

            RawKeyEvent capsLockKey = Assert.Single(events);
            var data = (RawKeyEventDataIos)capsLockKey.Data;
            Assert.Equal(PhysicalKeyboardKey.CapsLock, data.PhysicalKey);
            Assert.Equal(LogicalKeyboardKey.CapsLock, data.LogicalKey);
            Assert.Contains(LogicalKeyboardKey.CapsLock, RawKeyboard.Instance.KeysPressed);
        }
        finally
        {
            RawKeyboard.Instance.RemoveListener(events.Add);
        }
    }

    [Fact]
    public void AllowsInconsistentModifierForAndroid()
    {
        // Use `testWidgets` for clean-ups.
        var events = new List<RawKeyEvent>();
        RawKeyboard.Instance.AddListener(events.Add);
        try
        {
            HandlePlatformMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "android",
                ["keyCode"] = 115,
                ["plainCodePoint"] = 0,
                ["codePoint"] = 0,
                ["scanCode"] = 58,
                ["metaState"] = 0,
                ["source"] = 0x101,
                ["deviceId"] = 1,
            });

            RawKeyEvent capsLockKey = Assert.Single(events);
            var data = (RawKeyEventDataAndroid)capsLockKey.Data;
            Assert.Equal(PhysicalKeyboardKey.CapsLock, data.PhysicalKey);
            Assert.Equal(LogicalKeyboardKey.CapsLock, data.LogicalKey);
            Assert.Contains(LogicalKeyboardKey.CapsLock, RawKeyboard.Instance.KeysPressed);
        }
        finally
        {
            RawKeyboard.Instance.RemoveListener(events.Add);
        }
    }

    [Theory]
    [MemberData(nameof(AllVariant))]
    public void DispatchEventsToAllHandlers(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            using var tester = new FrameworkDartTester();
            var focusNode = new FocusNode();
            var logs = new List<int>();

            try
            {
                tester.PumpWidget(new RawKeyboardListener(
                    autofocus: true,
                    focusNode: focusNode,
                    child: new Container(),
                    onKey: _ => logs.Add(1)));

                // Only the Service binding handler.

                Assert.False(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA));
                Assert.Equal([1], logs);
                logs.Clear();

                // Add a handler.

                void Handler2(RawKeyEvent _) => logs.Add(2);

                RawKeyboard.Instance.AddListener(Handler2);

                Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.KeyA));
                Assert.Equal([1, 2], logs);
                logs.Clear();

                // Add another handler.

                void Handler3(RawKeyEvent _) => logs.Add(3);

                RawKeyboard.Instance.AddListener(Handler3);

                Assert.False(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA));
                Assert.Equal([1, 2, 3], logs);
                logs.Clear();

                // Add handler2 again.

                RawKeyboard.Instance.AddListener(Handler2);

                Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.KeyA));
                Assert.Equal([1, 2, 3, 2], logs);
                logs.Clear();

                // Remove handler2 once.

                RawKeyboard.Instance.RemoveListener(Handler2);
                Assert.False(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA));
                Assert.Equal([1, 3, 2], logs);
                logs.Clear();
            }
            finally
            {
                focusNode.Dispose();
            }
        });

    [Fact]
    public void ExceptionsFromRawKeyboardListenersAreCaughtAndReported()
    {
        // Dart throws the integer 1; C# throws a sentinel exception and checks its identity.
        var thrown = new InvalidOperationException("1");

        void ThrowingListener(RawKeyEvent @event)
        {
            throw thrown;
        }

        // When the listener throws an error...
        RawKeyboard.Instance.AddListener(ThrowingListener);

        // Simulate a key down event.
        FlutterErrorDetails? record = null;
        RunWhileOverridingOnError(
            () => KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA),
            onError: details => record = details);

        // ... the error should be caught.
        Assert.NotNull(record);
        Assert.Same(thrown, record.Exception);
        Dictionary<string, DiagnosticsNode> infos = GroupDiagnosticsByName(record.InformationCollector!());
        Assert.IsType<DiagnosticsProperty<RawKeyEvent>>(infos["Event"]);

        // But the exception should not interrupt recording the state.
        // Now the key handler no longer throws an error.
        RawKeyboard.Instance.RemoveListener(ThrowingListener);
        record = null;

        // Simulate a key up event.
        RunWhileOverridingOnError(
            () => KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.KeyA),
            onError: details => record = details);
        // If the previous state (key down) wasn't recorded, this key up event will
        // trigger assertions.
        Assert.Null(record);
    }

    // ---- Helpers ----

    /// <summary>
    /// <c>tester.binding.defaultBinaryMessenger.handlePlatformMessage(SystemChannels.keyEvent.name,
    /// SystemChannels.keyEvent.codec.encodeMessage(data), (ByteData? data) {})</c>, awaited.
    /// </summary>
    internal static void HandlePlatformMessage(IReadOnlyDictionary<string, object?> data) =>
        ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
                SystemChannels.KeyEvent.Name,
                SystemChannels.KeyEvent.Codec.EncodeMessage(data),
                _ => { })
            .GetAwaiter()
            .GetResult();

    internal static void RunWhileOverridingOnError(Action body, FlutterExceptionHandler onError)
    {
        FlutterExceptionHandler? oldFlutterErrorOnError = FlutterError.OnError;
        FlutterError.OnError = onError;
        try
        {
            body();
        }
        finally
        {
            FlutterError.OnError = oldFlutterErrorOnError;
        }
    }

    internal static Dictionary<string, DiagnosticsNode> GroupDiagnosticsByName(IEnumerable<DiagnosticsNode> infos)
    {
        // Dart's `Map.fromIterable`: a later node with the same name replaces an earlier one.
        var result = new Dictionary<string, DiagnosticsNode>();
        foreach (DiagnosticsNode node in infos)
        {
            result[node.Name ?? string.Empty] = node;
        }

        return result;
    }

    private static void ExpectKeysPressed(IEnumerable<LogicalKeyboardKey> expected, string? platform = null)
    {
        var expectedSet = new HashSet<LogicalKeyboardKey>(expected);
        IReadOnlySet<LogicalKeyboardKey> actual = RawKeyboard.Instance.KeysPressed;
        Assert.True(
            expectedSet.SetEquals(actual),
            $"Expected {{{string.Join(", ", expectedSet)}}}, got {{{string.Join(", ", actual)}}}"
            + (platform == null ? string.Empty : $" on {platform}"));
    }

    private static IEnumerable<LogicalKeyboardKey> When(bool condition, LogicalKeyboardKey key) =>
        condition ? [key] : [];

    private static List<LogicalKeyboardKey> AllSidesAnd(LogicalKeyboardKey key) =>
    [
        LogicalKeyboardKey.ShiftLeft,
        LogicalKeyboardKey.ShiftRight,
        LogicalKeyboardKey.AltLeft,
        LogicalKeyboardKey.AltRight,
        LogicalKeyboardKey.ControlLeft,
        LogicalKeyboardKey.ControlRight,
        LogicalKeyboardKey.MetaLeft,
        LogicalKeyboardKey.MetaRight,
        key,
    ];
}
#pragma warning restore CS0618
