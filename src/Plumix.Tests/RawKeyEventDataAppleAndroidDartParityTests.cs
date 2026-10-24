// Dart parity source: flutter/packages/flutter/test/services/raw_keyboard_test.dart
// (RawKeyEventDataAndroid, RawKeyEventDataFuchsia, RawKeyEventDataMacOs, RawKeyEventDataIos groups)

using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

#pragma warning disable CS0618 // The platform raw key data classes are deprecated in Flutter too.

/// <summary>
/// The <c>RawKeyEventDataAndroid</c>, <c>RawKeyEventDataFuchsia</c>, <c>RawKeyEventDataMacOs</c> and
/// <c>RawKeyEventDataIos</c> groups of <c>raw_keyboard_test.dart</c>; each test name carries its
/// group as a prefix.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class RawKeyEventDataAppleAndroidDartParityTests : IDisposable
{
    public RawKeyEventDataAppleAndroidDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        RawKeyboard.Instance.ClearKeysPressed();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
        RawKeyboard.Instance.ClearKeysPressed();
        HardwareKeyboard.Instance.ClearState();
        ServicesBinding.Instance.KeyEventManager.ResetForTests();
    }

    private readonly record struct ModifierCheck(ModifierKey Key, KeyboardSide Side);

    private static readonly ModifierKey[] ModifierKeyValues = Enum.GetValues<ModifierKey>();

    private static RawKeyEvent FromMessage(Dictionary<string, object?> message) => RawKeyEvent.FromMessage(message);

    // ---------------------------------------------------------------------------------------------
    // group('RawKeyEventDataAndroid')
    // ---------------------------------------------------------------------------------------------

    private static readonly (int Modifier, ModifierCheck Check)[] AndroidModifierTests =
    [
        (RawKeyEventDataAndroid.ModifierAlt | RawKeyEventDataAndroid.ModifierLeftAlt,
            new ModifierCheck(ModifierKey.AltModifier, KeyboardSide.Left)),
        (RawKeyEventDataAndroid.ModifierAlt | RawKeyEventDataAndroid.ModifierRightAlt,
            new ModifierCheck(ModifierKey.AltModifier, KeyboardSide.Right)),
        (RawKeyEventDataAndroid.ModifierShift | RawKeyEventDataAndroid.ModifierLeftShift,
            new ModifierCheck(ModifierKey.ShiftModifier, KeyboardSide.Left)),
        (RawKeyEventDataAndroid.ModifierShift | RawKeyEventDataAndroid.ModifierRightShift,
            new ModifierCheck(ModifierKey.ShiftModifier, KeyboardSide.Right)),
        (RawKeyEventDataAndroid.ModifierSym, new ModifierCheck(ModifierKey.SymbolModifier, KeyboardSide.All)),
        (RawKeyEventDataAndroid.ModifierFunction, new ModifierCheck(ModifierKey.FunctionModifier, KeyboardSide.All)),
        (RawKeyEventDataAndroid.ModifierControl | RawKeyEventDataAndroid.ModifierLeftControl,
            new ModifierCheck(ModifierKey.ControlModifier, KeyboardSide.Left)),
        (RawKeyEventDataAndroid.ModifierControl | RawKeyEventDataAndroid.ModifierRightControl,
            new ModifierCheck(ModifierKey.ControlModifier, KeyboardSide.Right)),
        (RawKeyEventDataAndroid.ModifierMeta | RawKeyEventDataAndroid.ModifierLeftMeta,
            new ModifierCheck(ModifierKey.MetaModifier, KeyboardSide.Left)),
        (RawKeyEventDataAndroid.ModifierMeta | RawKeyEventDataAndroid.ModifierRightMeta,
            new ModifierCheck(ModifierKey.MetaModifier, KeyboardSide.Right)),
        (RawKeyEventDataAndroid.ModifierCapsLock, new ModifierCheck(ModifierKey.CapsLockModifier, KeyboardSide.All)),
        (RawKeyEventDataAndroid.ModifierNumLock, new ModifierCheck(ModifierKey.NumLockModifier, KeyboardSide.All)),
        (RawKeyEventDataAndroid.ModifierScrollLock,
            new ModifierCheck(ModifierKey.ScrollLockModifier, KeyboardSide.All)),
    ];

    // 'modifier keys are recognized individually'
    [Fact]
    public void Android_ModifierKeysAreRecognizedIndividually()
    {
        foreach ((int modifier, ModifierCheck check) in AndroidModifierTests)
        {
            RawKeyEvent @event = FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "android",
                ["keyCode"] = 0x04,
                ["plainCodePoint"] = 0x64,
                ["codePoint"] = 0x44,
                ["scanCode"] = 0x20,
                ["metaState"] = modifier,
                ["source"] = 0x101, // Keyboard source.
                ["deviceId"] = 1,
            });
            var data = (RawKeyEventDataAndroid)@event.Data;
            foreach (ModifierKey key in ModifierKeyValues)
            {
                if (check.Key == key)
                {
                    Assert.True(
                        data.IsModifierPressed(key, side: check.Side),
                        $"{key} should be pressed with metaState {modifier}, but isn't.");
                    Assert.Equal(check.Side, data.GetModifierSide(key));
                }
                else
                {
                    Assert.False(
                        data.IsModifierPressed(key, side: check.Side),
                        $"{key} should not be pressed with metaState {modifier}.");
                }
            }
        }
    }

    // 'modifier keys are recognized when combined'
    [Fact]
    public void Android_ModifierKeysAreRecognizedWhenCombined()
    {
        foreach ((int modifier, ModifierCheck check) in AndroidModifierTests)
        {
            if (modifier == RawKeyEventDataAndroid.ModifierFunction)
            {
                // No need to combine function key with itself.
                continue;
            }

            RawKeyEvent @event = FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "android",
                ["keyCode"] = 0x04,
                ["plainCodePoint"] = 0x64,
                ["codePoint"] = 0x44,
                ["scanCode"] = 0x20,
                ["metaState"] = modifier | RawKeyEventDataAndroid.ModifierFunction,
                ["source"] = 0x101, // Keyboard source.
                ["deviceId"] = 1,
            });
            var data = (RawKeyEventDataAndroid)@event.Data;
            foreach (ModifierKey key in ModifierKeyValues)
            {
                if (check.Key == key || key == ModifierKey.FunctionModifier)
                {
                    Assert.True(
                        data.IsModifierPressed(key, side: check.Side),
                        $"{key} should be pressed with metaState {modifier} "
                        + $"and additional key {RawKeyEventDataAndroid.ModifierFunction}, but isn't.");
                    if (key != ModifierKey.FunctionModifier)
                    {
                        Assert.Equal(check.Side, data.GetModifierSide(key));
                    }
                    else
                    {
                        Assert.Equal(KeyboardSide.All, data.GetModifierSide(key));
                    }
                }
                else
                {
                    Assert.False(
                        data.IsModifierPressed(key, side: check.Side),
                        $"{key} should not be pressed with metaState {modifier} "
                        + $"and additional key {RawKeyEventDataAndroid.ModifierFunction}.");
                }
            }
        }
    }

    // 'Printable keyboard keys are correctly translated'
    [Fact]
    public void Android_PrintableKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent keyAEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "android",
            ["keyCode"] = 29,
            ["plainCodePoint"] = (int)'a',
            ["codePoint"] = (int)'A',
            ["character"] = "A",
            ["scanCode"] = 30,
            ["metaState"] = 0x0,
            ["source"] = 0x101, // Keyboard source.
            ["deviceId"] = 1,
        });
        var data = (RawKeyEventDataAndroid)keyAEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.KeyA, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.KeyA, data.LogicalKey);
        Assert.Equal("a", data.KeyLabel);
    }

    // 'Control keyboard keys are correctly translated'
    [Fact]
    public void Android_ControlKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent escapeKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "android",
            ["keyCode"] = 111,
            ["codePoint"] = 0,
            ["scanCode"] = 1,
            ["metaState"] = 0x0,
            ["source"] = 0x101, // Keyboard source.
            ["deviceId"] = 1,
        });
        var data = (RawKeyEventDataAndroid)escapeKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.Escape, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.Escape, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    // 'Modifier keyboard keys are correctly translated'
    [Fact]
    public void Android_ModifierKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent shiftLeftKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "android",
            ["keyCode"] = 59,
            ["plainCodePoint"] = 0,
            ["codePoint"] = 0,
            ["scanCode"] = 42,
            ["metaState"] = RawKeyEventDataAndroid.ModifierLeftShift,
            ["source"] = 0x101, // Keyboard source.
            ["deviceId"] = 1,
        });
        var data = (RawKeyEventDataAndroid)shiftLeftKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.ShiftLeft, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.ShiftLeft, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    // 'DPAD keys from a joystick give physical key mappings'
    [Fact]
    public void Android_DpadKeysFromAJoystickGivePhysicalKeyMappings()
    {
        RawKeyEvent joystickDpadDown = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "android",
            ["keyCode"] = 20,
            ["plainCodePoint"] = 0,
            ["codePoint"] = 0,
            ["scanCode"] = 0,
            ["metaState"] = 0,
            ["source"] = 0x1000010, // Joystick source.
            ["deviceId"] = 1,
        });
        var data = (RawKeyEventDataAndroid)joystickDpadDown.Data;
        Assert.Equal(PhysicalKeyboardKey.ArrowDown, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.ArrowDown, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    // 'Arrow keys from a keyboard give correct physical key mappings'
    [Fact]
    public void Android_ArrowKeysFromAKeyboardGiveCorrectPhysicalKeyMappings()
    {
        RawKeyEvent joystickDpadDown = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "android",
            ["keyCode"] = 20,
            ["plainCodePoint"] = 0,
            ["codePoint"] = 0,
            ["scanCode"] = 108,
            ["metaState"] = 0,
            ["source"] = 0x101, // Keyboard source.
        });
        var data = (RawKeyEventDataAndroid)joystickDpadDown.Data;
        Assert.Equal(PhysicalKeyboardKey.ArrowDown, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.ArrowDown, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    // 'DPAD center from a game pad gives physical key mappings'
    [Fact]
    public void Android_DpadCenterFromAGamePadGivesPhysicalKeyMappings()
    {
        RawKeyEvent joystickDpadCenter = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "android",
            ["keyCode"] = 23, // DPAD_CENTER code.
            ["plainCodePoint"] = 0,
            ["codePoint"] = 0,
            ["scanCode"] = 317, // Left side thumb joystick center click button.
            ["metaState"] = 0,
            ["source"] = 0x501, // Gamepad and keyboard source.
            ["deviceId"] = 1,
        });
        var data = (RawKeyEventDataAndroid)joystickDpadCenter.Data;
        Assert.Equal(PhysicalKeyboardKey.GameButtonThumbLeft, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.Select, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    // 'Device id is read from message'
    [Fact]
    public void Android_DeviceIdIsReadFromMessage()
    {
        RawKeyEvent joystickDpadCenter = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "android",
            ["keyCode"] = 23, // DPAD_CENTER code.
            ["plainCodePoint"] = 0,
            ["codePoint"] = 0,
            ["scanCode"] = 317, // Left side thumb joystick center click button.
            ["metaState"] = 0,
            ["source"] = 0x501, // Gamepad and keyboard source.
            ["deviceId"] = 10,
        });
        var data = (RawKeyEventDataAndroid)joystickDpadCenter.Data;
        Assert.Equal(10, data.DeviceId);
    }

    // 'Repeat count is passed correctly'
    [Fact]
    public void Android_RepeatCountIsPassedCorrectly()
    {
        RawKeyEvent repeatCountEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "android",
            ["keyCode"] = 29,
            ["plainCodePoint"] = (int)'a',
            ["codePoint"] = (int)'A',
            ["character"] = "A",
            ["scanCode"] = 30,
            ["metaState"] = 0x0,
            ["source"] = 0x101, // Keyboard source.
            ["repeatCount"] = 42,
        });
        var data = (RawKeyEventDataAndroid)repeatCountEvent.Data;
        Assert.Equal(42, data.RepeatCount);
    }

    // 'Key events are responded to correctly.'
    [Fact]
    public void Android_KeyEventsAreRespondedToCorrectly()
    {
        using var tester = new FrameworkDartTester();
        Assert.Empty(RawKeyboard.Instance.KeysPressed);
        // Generate the data for a regular key down event.
        Dictionary<string, object?> data = KeyEventSimulator.GetKeyData(
            LogicalKeyboardKey.KeyA,
            platform: "android");
        IReadOnlyDictionary<string, object?>? message = null;
        ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
            SystemChannels.KeyEvent.Name,
            SystemChannels.KeyEvent.Codec.EncodeMessage(data),
            reply =>
            {
                message = (IReadOnlyDictionary<string, object?>?)SystemChannels.KeyEvent.Codec.DecodeMessage(reply);
            }).GetAwaiter().GetResult();
        AssertHandledReply(false, message);
        message = null;

        // Set up a widget that will receive focused text events.
        var focusNode = new FocusNode(debugLabel: "Test Node");
        try
        {
            tester.PumpWidget(new Focus(
                focusNode: focusNode,
                onKey: (_, _) => KeyEventResult.Handled, // handle all events.
                child: new SizedBox()));
            focusNode.RequestFocus();
            tester.Pump();

            ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
                SystemChannels.KeyEvent.Name,
                SystemChannels.KeyEvent.Codec.EncodeMessage(data),
                reply =>
                {
                    message = (IReadOnlyDictionary<string, object?>?)
                        SystemChannels.KeyEvent.Codec.DecodeMessage(reply);
                }).GetAwaiter().GetResult();
            AssertHandledReply(true, message);
            tester.Binding.SetMockMessageHandler(SystemChannels.KeyEvent.Name, null);
        }
        finally
        {
            focusNode.Dispose();
        }
    }

    // equals(<String, Object?>{'handled': handled})
    private static void AssertHandledReply(bool handled, IReadOnlyDictionary<string, object?>? message)
    {
        Assert.NotNull(message);
        KeyValuePair<string, object?> entry = Assert.Single(message);
        Assert.Equal("handled", entry.Key);
        Assert.Equal(handled, entry.Value);
    }

    // 'data.toString'
    [DebugOnlyFact]
    public void Android_DataToString()
    {
        Assert.Equal(
            "RawKeyEventDataAndroid#00000("
            + "flags: 0, codePoint: 65, plainCodePoint: 97, keyCode: 29, "
            + "scanCode: 30, metaState: 0)",
            FrameworkDartTester.IgnoringHashCodes(FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "android",
                ["keyCode"] = 29,
                ["plainCodePoint"] = 97,
                ["codePoint"] = 65,
                ["character"] = "A",
                ["scanCode"] = 30,
                ["metaState"] = 0x0,
                ["source"] = 0x101, // Keyboard source.
                ["repeatCount"] = 42,
            }).Data.ToString()));
    }

    // 'data.equality'
    [Fact]
    public void Android_DataEquality()
    {
        Dictionary<string, object?> Message() => new()
        {
            ["type"] = "keydown",
            ["keymap"] = "android",
            ["keyCode"] = 29,
            ["plainCodePoint"] = 97,
            ["codePoint"] = 65,
            ["character"] = "A",
            ["scanCode"] = 30,
            ["metaState"] = 0x0,
            ["source"] = 0x101, // Keyboard source.
            ["repeatCount"] = 42,
        };

        Assert.Equal<object>(
            new RawKeyEventDataAndroid(codePoint: 65, plainCodePoint: 97, keyCode: 29, scanCode: 30),
            FromMessage(Message()).Data);

        Assert.NotEqual<object>(new RawKeyEventDataAndroid(), FromMessage(Message()).Data);
    }

    // ---------------------------------------------------------------------------------------------
    // group('RawKeyEventDataFuchsia')
    // ---------------------------------------------------------------------------------------------

    private static readonly (int Modifier, ModifierCheck Check)[] FuchsiaModifierTests =
    [
        (RawKeyEventDataFuchsia.ModifierAlt, new ModifierCheck(ModifierKey.AltModifier, KeyboardSide.Any)),
        (RawKeyEventDataFuchsia.ModifierLeftAlt, new ModifierCheck(ModifierKey.AltModifier, KeyboardSide.Left)),
        (RawKeyEventDataFuchsia.ModifierRightAlt, new ModifierCheck(ModifierKey.AltModifier, KeyboardSide.Right)),
        (RawKeyEventDataFuchsia.ModifierShift, new ModifierCheck(ModifierKey.ShiftModifier, KeyboardSide.Any)),
        (RawKeyEventDataFuchsia.ModifierLeftShift,
            new ModifierCheck(ModifierKey.ShiftModifier, KeyboardSide.Left)),
        (RawKeyEventDataFuchsia.ModifierRightShift,
            new ModifierCheck(ModifierKey.ShiftModifier, KeyboardSide.Right)),
        (RawKeyEventDataFuchsia.ModifierControl,
            new ModifierCheck(ModifierKey.ControlModifier, KeyboardSide.Any)),
        (RawKeyEventDataFuchsia.ModifierLeftControl,
            new ModifierCheck(ModifierKey.ControlModifier, KeyboardSide.Left)),
        (RawKeyEventDataFuchsia.ModifierRightControl,
            new ModifierCheck(ModifierKey.ControlModifier, KeyboardSide.Right)),
        (RawKeyEventDataFuchsia.ModifierMeta, new ModifierCheck(ModifierKey.MetaModifier, KeyboardSide.Any)),
        (RawKeyEventDataFuchsia.ModifierLeftMeta, new ModifierCheck(ModifierKey.MetaModifier, KeyboardSide.Left)),
        (RawKeyEventDataFuchsia.ModifierRightMeta,
            new ModifierCheck(ModifierKey.MetaModifier, KeyboardSide.Right)),
        (RawKeyEventDataFuchsia.ModifierCapsLock,
            new ModifierCheck(ModifierKey.CapsLockModifier, KeyboardSide.Any)),
    ];

    // 'modifier keys are recognized individually'
    [Fact]
    public void Fuchsia_ModifierKeysAreRecognizedIndividually()
    {
        foreach ((int modifier, ModifierCheck check) in FuchsiaModifierTests)
        {
            RawKeyEvent @event = FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "fuchsia",
                ["hidUsage"] = 0x04,
                ["codePoint"] = 0x64,
                ["modifiers"] = modifier,
            });
            var data = (RawKeyEventDataFuchsia)@event.Data;
            foreach (ModifierKey key in ModifierKeyValues)
            {
                if (check.Key == key)
                {
                    Assert.True(
                        data.IsModifierPressed(key, side: check.Side),
                        $"{key} should be pressed with metaState {modifier}, but isn't.");
                }
                else
                {
                    Assert.False(
                        data.IsModifierPressed(key, side: check.Side),
                        $"{key} should not be pressed with metaState {modifier}.");
                }
            }
        }
    }

    // 'modifier keys are recognized when combined'
    [Fact]
    public void Fuchsia_ModifierKeysAreRecognizedWhenCombined()
    {
        foreach ((int modifier, ModifierCheck check) in FuchsiaModifierTests)
        {
            if (modifier == RawKeyEventDataFuchsia.ModifierCapsLock)
            {
                // No need to combine caps lock key with itself.
                continue;
            }

            RawKeyEvent @event = FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "fuchsia",
                ["hidUsage"] = 0x04,
                ["codePoint"] = 0x64,
                ["modifiers"] = modifier | RawKeyEventDataFuchsia.ModifierCapsLock,
            });
            var data = (RawKeyEventDataFuchsia)@event.Data;
            foreach (ModifierKey key in ModifierKeyValues)
            {
                if (check.Key == key || key == ModifierKey.CapsLockModifier)
                {
                    Assert.True(
                        data.IsModifierPressed(key, side: check.Side),
                        $"{key} should be pressed with metaState {modifier} "
                        + $"and additional key {RawKeyEventDataFuchsia.ModifierCapsLock}, but isn't.");
                }
                else
                {
                    Assert.False(
                        data.IsModifierPressed(key, side: check.Side),
                        $"{key} should not be pressed with metaState {modifier} "
                        + $"and additional key {RawKeyEventDataFuchsia.ModifierCapsLock}.");
                }
            }
        }
    }

    // 'Printable keyboard keys are correctly translated'
    [Fact]
    public void Fuchsia_PrintableKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent keyAEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "fuchsia",
            ["hidUsage"] = 0x00070004,
            ["codePoint"] = (int)'a',
            ["character"] = "a",
        });
        var data = (RawKeyEventDataFuchsia)keyAEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.KeyA, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.KeyA, data.LogicalKey);
        Assert.Equal("a", data.KeyLabel);
    }

    // 'Control keyboard keys are correctly translated'
    [Fact]
    public void Fuchsia_ControlKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent escapeKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "fuchsia",
            ["hidUsage"] = 0x00070029,
        });
        var data = (RawKeyEventDataFuchsia)escapeKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.Escape, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.Escape, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    // 'Modifier keyboard keys are correctly translated'
    [Fact]
    public void Fuchsia_ModifierKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent shiftLeftKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "fuchsia",
            ["hidUsage"] = 0x000700e1,
        });
        var data = (RawKeyEventDataFuchsia)shiftLeftKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.ShiftLeft, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.ShiftLeft, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    // 'data.toString'
    [DebugOnlyFact]
    public void Fuchsia_DataToString()
    {
        Assert.Equal(
            "RawKeyEventDataFuchsia#00000(hidUsage: 458756, codePoint: 97, modifiers: 16)",
            FrameworkDartTester.IgnoringHashCodes(FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "fuchsia",
                ["hidUsage"] = 0x00070004,
                ["codePoint"] = 97,
                ["character"] = "a",
                ["modifiers"] = 0x10,
            }).Data.ToString()));
    }

    // 'data.equality'
    [Fact]
    public void Fuchsia_DataEquality()
    {
        Dictionary<string, object?> Message() => new()
        {
            ["type"] = "keydown",
            ["keymap"] = "fuchsia",
            ["hidUsage"] = 0x00070004,
            ["codePoint"] = 97,
            ["character"] = "a",
            ["modifiers"] = 0x10,
        };

        Assert.Equal<object>(
            new RawKeyEventDataFuchsia(hidUsage: 0x00070004, codePoint: 97, modifiers: 0x10),
            FromMessage(Message()).Data);

        Assert.NotEqual<object>(new RawKeyEventDataFuchsia(), FromMessage(Message()).Data);
    }

    // ---------------------------------------------------------------------------------------------
    // group('RawKeyEventDataMacOs')
    // ---------------------------------------------------------------------------------------------

    private static readonly (int Modifier, ModifierCheck Check)[] MacOsModifierTests =
    [
        (RawKeyEventDataMacOs.ModifierOption | RawKeyEventDataMacOs.ModifierLeftOption,
            new ModifierCheck(ModifierKey.AltModifier, KeyboardSide.Left)),
        (RawKeyEventDataMacOs.ModifierOption | RawKeyEventDataMacOs.ModifierRightOption,
            new ModifierCheck(ModifierKey.AltModifier, KeyboardSide.Right)),
        (RawKeyEventDataMacOs.ModifierShift | RawKeyEventDataMacOs.ModifierLeftShift,
            new ModifierCheck(ModifierKey.ShiftModifier, KeyboardSide.Left)),
        (RawKeyEventDataMacOs.ModifierShift | RawKeyEventDataMacOs.ModifierRightShift,
            new ModifierCheck(ModifierKey.ShiftModifier, KeyboardSide.Right)),
        (RawKeyEventDataMacOs.ModifierControl | RawKeyEventDataMacOs.ModifierLeftControl,
            new ModifierCheck(ModifierKey.ControlModifier, KeyboardSide.Left)),
        (RawKeyEventDataMacOs.ModifierControl | RawKeyEventDataMacOs.ModifierRightControl,
            new ModifierCheck(ModifierKey.ControlModifier, KeyboardSide.Right)),
        (RawKeyEventDataMacOs.ModifierCommand | RawKeyEventDataMacOs.ModifierLeftCommand,
            new ModifierCheck(ModifierKey.MetaModifier, KeyboardSide.Left)),
        (RawKeyEventDataMacOs.ModifierCommand | RawKeyEventDataMacOs.ModifierRightCommand,
            new ModifierCheck(ModifierKey.MetaModifier, KeyboardSide.Right)),
        (RawKeyEventDataMacOs.ModifierOption, new ModifierCheck(ModifierKey.AltModifier, KeyboardSide.All)),
        (RawKeyEventDataMacOs.ModifierShift, new ModifierCheck(ModifierKey.ShiftModifier, KeyboardSide.All)),
        (RawKeyEventDataMacOs.ModifierControl, new ModifierCheck(ModifierKey.ControlModifier, KeyboardSide.All)),
        (RawKeyEventDataMacOs.ModifierCommand, new ModifierCheck(ModifierKey.MetaModifier, KeyboardSide.All)),
        (RawKeyEventDataMacOs.ModifierCapsLock, new ModifierCheck(ModifierKey.CapsLockModifier, KeyboardSide.All)),
    ];

    // 'modifier keys are recognized individually'
    [Fact]
    public void MacOs_ModifierKeysAreRecognizedIndividually()
    {
        foreach ((int modifier, ModifierCheck check) in MacOsModifierTests)
        {
            RawKeyEvent @event = FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "macos",
                ["keyCode"] = 0x04,
                ["characters"] = "a",
                ["charactersIgnoringModifiers"] = "a",
                ["modifiers"] = modifier,
            });
            var data = (RawKeyEventDataMacOs)@event.Data;
            foreach (ModifierKey key in ModifierKeyValues)
            {
                if (check.Key == key)
                {
                    Assert.True(
                        data.IsModifierPressed(key, side: check.Side),
                        $"{key} should be pressed with metaState {modifier}, but isn't.");
                    Assert.Equal(check.Side, data.GetModifierSide(key));
                }
                else
                {
                    Assert.False(
                        data.IsModifierPressed(key, side: check.Side),
                        $"{key} should not be pressed with metaState {modifier}.");
                }
            }
        }
    }

    // 'modifier keys are recognized when combined'
    [Fact]
    public void MacOs_ModifierKeysAreRecognizedWhenCombined()
    {
        foreach ((int modifier, ModifierCheck check) in MacOsModifierTests)
        {
            if (modifier == RawKeyEventDataMacOs.ModifierCapsLock)
            {
                // No need to combine caps lock key with itself.
                continue;
            }

            RawKeyEvent @event = FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "macos",
                ["keyCode"] = 0x04,
                ["characters"] = "a",
                ["charactersIgnoringModifiers"] = "a",
                ["modifiers"] = modifier | RawKeyEventDataMacOs.ModifierCapsLock,
            });
            var data = (RawKeyEventDataMacOs)@event.Data;
            foreach (ModifierKey key in ModifierKeyValues)
            {
                if (check.Key == key || key == ModifierKey.CapsLockModifier)
                {
                    Assert.True(
                        data.IsModifierPressed(key, side: check.Side),
                        $"{key} should be pressed with metaState {modifier} "
                        + $"and additional key {RawKeyEventDataMacOs.ModifierCapsLock}, but isn't.");
                    if (key != ModifierKey.CapsLockModifier)
                    {
                        Assert.Equal(check.Side, data.GetModifierSide(key));
                    }
                    else
                    {
                        Assert.Equal(KeyboardSide.All, data.GetModifierSide(key));
                    }
                }
                else
                {
                    Assert.False(
                        data.IsModifierPressed(key, side: check.Side),
                        $"{key} should not be pressed with metaState {modifier} "
                        + $"and additional key {RawKeyEventDataMacOs.ModifierCapsLock}.");
                }
            }
        }
    }

    // 'Lower letter keys are correctly translated'
    [Fact]
    public void MacOs_LowerLetterKeysAreCorrectlyTranslated()
    {
        RawKeyEvent keyAEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "macos",
            ["keyCode"] = 0x00000000,
            ["characters"] = "a",
            ["charactersIgnoringModifiers"] = "a",
            ["modifiers"] = 0x0,
        });
        var data = (RawKeyEventDataMacOs)keyAEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.KeyA, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.KeyA, data.LogicalKey);
        Assert.Equal("a", data.KeyLabel);
    }

    // 'Upper letter keys are correctly translated'
    [Fact]
    public void MacOs_UpperLetterKeysAreCorrectlyTranslated()
    {
        RawKeyEvent keyAEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "macos",
            ["keyCode"] = 0x00000000,
            ["characters"] = "A",
            ["charactersIgnoringModifiers"] = "A",
            ["modifiers"] = 0x20002,
        });
        var data = (RawKeyEventDataMacOs)keyAEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.KeyA, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.KeyA, data.LogicalKey);
        Assert.Equal("A", data.KeyLabel);
    }

    // 'Control keyboard keys are correctly translated'
    [Fact]
    public void MacOs_ControlKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent escapeKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "macos",
            ["keyCode"] = 0x00000035,
            ["characters"] = "",
            ["charactersIgnoringModifiers"] = "",
            ["modifiers"] = 0x0,
        });
        var data = (RawKeyEventDataMacOs)escapeKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.Escape, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.Escape, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    // 'Modifier keyboard keys are correctly translated'
    [Fact]
    public void MacOs_ModifierKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent shiftLeftKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "macos",
            ["keyCode"] = 0x00000038,
            ["characters"] = "",
            ["charactersIgnoringModifiers"] = "",
            ["modifiers"] = RawKeyEventDataMacOs.ModifierLeftShift,
        });
        var data = (RawKeyEventDataMacOs)shiftLeftKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.ShiftLeft, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.ShiftLeft, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    // 'Unprintable keyboard keys are correctly translated'
    [Fact]
    public void MacOs_UnprintableKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent leftArrowKey = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "macos",
            ["keyCode"] = 0x0000007B,
            ["characters"] = "",
            ["charactersIgnoringModifiers"] = "", // NSLeftArrowFunctionKey = 0xF702
            ["modifiers"] = RawKeyEventDataMacOs.ModifierFunction,
        });
        var data = (RawKeyEventDataMacOs)leftArrowKey.Data;
        Assert.Equal(PhysicalKeyboardKey.ArrowLeft, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.ArrowLeft, data.LogicalKey);
    }

    // 'Multi-char keyboard keys are correctly translated'
    [Fact]
    public void MacOs_MultiCharKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent leftArrowKey = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "macos",
            ["keyCode"] = 0x00000000,
            ["characters"] = "án",
            ["charactersIgnoringModifiers"] = "án",
            ["modifiers"] = 0,
        });
        var data = (RawKeyEventDataMacOs)leftArrowKey.Data;
        Assert.Equal(PhysicalKeyboardKey.KeyA, data.PhysicalKey);
        Assert.Equal(new LogicalKeyboardKey(0x1400000000), data.LogicalKey);
    }

    // 'Prioritize logical key from specifiedLogicalKey'
    [Fact]
    public void MacOs_PrioritizeLogicalKeyFromSpecifiedLogicalKey()
    {
        RawKeyEvent digit1FromFrench = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "macos",
            ["keyCode"] = 0x00000012,
            ["characters"] = "&",
            ["charactersIgnoringModifiers"] = "&",
            ["specifiedLogicalKey"] = 0x000000031,
            ["modifiers"] = 0,
        });
        var data = (RawKeyEventDataMacOs)digit1FromFrench.Data;
        Assert.Equal(PhysicalKeyboardKey.Digit1, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.Digit1, data.LogicalKey);
    }

    // 'data.toString'
    [DebugOnlyFact]
    public void MacOs_DataToString()
    {
        Assert.Equal(
            "RawKeyEventDataMacOs#00000(characters: A, charactersIgnoringModifiers: a, keyCode: 96, modifiers: 16)",
            FrameworkDartTester.IgnoringHashCodes(FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "macos",
                ["keyCode"] = 0x00000060,
                ["characters"] = "A",
                ["charactersIgnoringModifiers"] = "a",
                ["modifiers"] = 0x10,
            }).Data.ToString()));
    }

    // 'data.equality'
    [Fact]
    public void MacOs_DataEquality()
    {
        Dictionary<string, object?> Message() => new()
        {
            ["type"] = "keydown",
            ["keymap"] = "macos",
            ["keyCode"] = 0x00000060,
            ["characters"] = "A",
            ["charactersIgnoringModifiers"] = "a",
            ["modifiers"] = 0x10,
        };

        Assert.Equal<object>(
            new RawKeyEventDataMacOs(
                keyCode: 0x00000060,
                characters: "A",
                charactersIgnoringModifiers: "a",
                modifiers: 0x10),
            FromMessage(Message()).Data);

        Assert.NotEqual<object>(new RawKeyEventDataMacOs(), FromMessage(Message()).Data);
    }

    // ---------------------------------------------------------------------------------------------
    // group('RawKeyEventDataIos')
    // ---------------------------------------------------------------------------------------------

    private static readonly (int Modifier, ModifierCheck Check)[] IosModifierTests =
    [
        (RawKeyEventDataIos.ModifierOption | RawKeyEventDataIos.ModifierLeftOption,
            new ModifierCheck(ModifierKey.AltModifier, KeyboardSide.Left)),
        (RawKeyEventDataIos.ModifierOption | RawKeyEventDataIos.ModifierRightOption,
            new ModifierCheck(ModifierKey.AltModifier, KeyboardSide.Right)),
        (RawKeyEventDataIos.ModifierShift | RawKeyEventDataIos.ModifierLeftShift,
            new ModifierCheck(ModifierKey.ShiftModifier, KeyboardSide.Left)),
        (RawKeyEventDataIos.ModifierShift | RawKeyEventDataIos.ModifierRightShift,
            new ModifierCheck(ModifierKey.ShiftModifier, KeyboardSide.Right)),
        (RawKeyEventDataIos.ModifierControl | RawKeyEventDataIos.ModifierLeftControl,
            new ModifierCheck(ModifierKey.ControlModifier, KeyboardSide.Left)),
        (RawKeyEventDataIos.ModifierControl | RawKeyEventDataIos.ModifierRightControl,
            new ModifierCheck(ModifierKey.ControlModifier, KeyboardSide.Right)),
        (RawKeyEventDataIos.ModifierCommand | RawKeyEventDataIos.ModifierLeftCommand,
            new ModifierCheck(ModifierKey.MetaModifier, KeyboardSide.Left)),
        (RawKeyEventDataIos.ModifierCommand | RawKeyEventDataIos.ModifierRightCommand,
            new ModifierCheck(ModifierKey.MetaModifier, KeyboardSide.Right)),
        (RawKeyEventDataIos.ModifierOption, new ModifierCheck(ModifierKey.AltModifier, KeyboardSide.All)),
        (RawKeyEventDataIos.ModifierShift, new ModifierCheck(ModifierKey.ShiftModifier, KeyboardSide.All)),
        (RawKeyEventDataIos.ModifierControl, new ModifierCheck(ModifierKey.ControlModifier, KeyboardSide.All)),
        (RawKeyEventDataIos.ModifierCommand, new ModifierCheck(ModifierKey.MetaModifier, KeyboardSide.All)),
        (RawKeyEventDataIos.ModifierCapsLock, new ModifierCheck(ModifierKey.CapsLockModifier, KeyboardSide.All)),
    ];

    // 'modifier keys are recognized individually'
    [Fact]
    public void Ios_ModifierKeysAreRecognizedIndividually()
    {
        foreach ((int modifier, ModifierCheck check) in IosModifierTests)
        {
            RawKeyEvent @event = FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "ios",
                ["keyCode"] = 0x04,
                ["characters"] = "a",
                ["charactersIgnoringModifiers"] = "a",
                ["modifiers"] = modifier,
            });
            var data = (RawKeyEventDataIos)@event.Data;
            foreach (ModifierKey key in ModifierKeyValues)
            {
                if (check.Key == key)
                {
                    Assert.True(
                        data.IsModifierPressed(key, side: check.Side),
                        $"{key} should be pressed with metaState {modifier}, but isn't.");
                    Assert.Equal(check.Side, data.GetModifierSide(key));
                }
                else
                {
                    Assert.False(
                        data.IsModifierPressed(key, side: check.Side),
                        $"{key} should not be pressed with metaState {modifier}.");
                }
            }
        }
    }

    // 'modifier keys are recognized when combined'
    [Fact]
    public void Ios_ModifierKeysAreRecognizedWhenCombined()
    {
        foreach ((int modifier, ModifierCheck check) in IosModifierTests)
        {
            if (modifier == RawKeyEventDataIos.ModifierCapsLock)
            {
                // No need to combine caps lock key with itself.
                continue;
            }

            RawKeyEvent @event = FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "ios",
                ["keyCode"] = 0x04,
                ["characters"] = "a",
                ["charactersIgnoringModifiers"] = "a",
                ["modifiers"] = modifier | RawKeyEventDataIos.ModifierCapsLock,
            });
            var data = (RawKeyEventDataIos)@event.Data;
            foreach (ModifierKey key in ModifierKeyValues)
            {
                if (check.Key == key || key == ModifierKey.CapsLockModifier)
                {
                    Assert.True(
                        data.IsModifierPressed(key, side: check.Side),
                        $"{key} should be pressed with metaState {modifier} "
                        + $"and additional key {RawKeyEventDataIos.ModifierCapsLock}, but isn't.");
                    if (key != ModifierKey.CapsLockModifier)
                    {
                        Assert.Equal(check.Side, data.GetModifierSide(key));
                    }
                    else
                    {
                        Assert.Equal(KeyboardSide.All, data.GetModifierSide(key));
                    }
                }
                else
                {
                    Assert.False(
                        data.IsModifierPressed(key, side: check.Side),
                        $"{key} should not be pressed with metaState {modifier} "
                        + $"and additional key {RawKeyEventDataIos.ModifierCapsLock}.");
                }
            }
        }
    }

    // 'Printable keyboard keys are correctly translated'
    [Fact]
    public void Ios_PrintableKeyboardKeysAreCorrectlyTranslated()
    {
        const string unmodifiedCharacter = "a";
        RawKeyEvent keyAEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "ios",
            ["keyCode"] = 0x00000004,
            ["characters"] = "a",
            ["charactersIgnoringModifiers"] = unmodifiedCharacter,
            ["modifiers"] = 0x0,
        });
        var data = (RawKeyEventDataIos)keyAEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.KeyA, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.KeyA, data.LogicalKey);
        Assert.Equal("a", data.KeyLabel);
    }

    // 'Control keyboard keys are correctly translated'
    [Fact]
    public void Ios_ControlKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent escapeKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "ios",
            ["keyCode"] = 0x00000029,
            ["characters"] = "",
            ["charactersIgnoringModifiers"] = "",
            ["modifiers"] = 0x0,
        });
        var data = (RawKeyEventDataIos)escapeKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.Escape, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.Escape, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    // 'Modifier keyboard keys are correctly translated'
    [Fact]
    public void Ios_ModifierKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent shiftLeftKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "ios",
            ["keyCode"] = 0x000000e1,
            ["characters"] = "",
            ["charactersIgnoringModifiers"] = "",
            ["modifiers"] = RawKeyEventDataIos.ModifierLeftShift,
        });
        var data = (RawKeyEventDataIos)shiftLeftKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.ShiftLeft, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.ShiftLeft, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    // 'Unprintable keyboard keys are correctly translated'
    [Fact]
    public void Ios_UnprintableKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent leftArrowKey = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "ios",
            ["keyCode"] = 0x00000050,
            ["characters"] = "",
            ["charactersIgnoringModifiers"] = "UIKeyInputLeftArrow",
            ["modifiers"] = RawKeyEventDataIos.ModifierFunction,
        });
        var data = (RawKeyEventDataIos)leftArrowKey.Data;
        Assert.Equal(PhysicalKeyboardKey.ArrowLeft, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.ArrowLeft, data.LogicalKey);
    }

    // 'data.toString'
    [DebugOnlyFact]
    public void Ios_DataToString()
    {
        Assert.Equal(
            "RawKeyEventDataIos#00000(characters: A, charactersIgnoringModifiers: a, keyCode: 4, modifiers: 16)",
            FrameworkDartTester.IgnoringHashCodes(FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "ios",
                ["keyCode"] = 0x00000004,
                ["characters"] = "A",
                ["charactersIgnoringModifiers"] = "a",
                ["modifiers"] = 0x10,
            }).Data.ToString()));
    }

    // 'data.equality'
    [Fact]
    public void Ios_DataEquality()
    {
        Dictionary<string, object?> Message() => new()
        {
            ["type"] = "keydown",
            ["keymap"] = "ios",
            ["keyCode"] = 0x00000004,
            ["characters"] = "A",
            ["charactersIgnoringModifiers"] = "a",
            ["modifiers"] = 0x10,
        };

        Assert.Equal<object>(
            new RawKeyEventDataIos(
                keyCode: 0x00000004,
                characters: "A",
                charactersIgnoringModifiers: "a",
                modifiers: 0x10),
            FromMessage(Message()).Data);

        Assert.NotEqual<object>(new RawKeyEventDataIos(), FromMessage(Message()).Data);
    }
}
#pragma warning restore CS0618
