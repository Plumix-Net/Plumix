// Dart parity source: flutter/packages/flutter/test/services/raw_keyboard_test.dart (RawKeyEventDataWindows,
// RawKeyEventDataLinux-GLFW, RawKeyEventDataLinux-GTK, RawKeyEventDataWeb groups)

using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

#pragma warning disable CS0618 // The raw key event data classes are deprecated, as in Dart.
[Collection(SchedulerTestCollection.Name)]
public sealed class RawKeyEventDataDesktopWebDartParityTests : IDisposable
{
    public RawKeyEventDataDesktopWebDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        RawKeyboard.Instance.ClearKeysPressed();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
        RawKeyboard.Instance.ClearKeysPressed();
    }

    // The test file's `_ModifierCheck`.
    private readonly record struct ModifierCheck(ModifierKey Key, KeyboardSide Side);

    private static RawKeyEvent FromMessage(Dictionary<string, object?> message) => RawKeyEvent.FromMessage(message);

    private static string IgnoringHashCodes(string value) => FrameworkDartTester.IgnoringHashCodes(value);

    // ---- group('RawKeyEventDataWindows') ----

    private static readonly Dictionary<int, ModifierCheck> WindowsModifierTests = new()
    {
        [RawKeyEventDataWindows.ModifierLeftAlt] = new(ModifierKey.AltModifier, KeyboardSide.Left),
        [RawKeyEventDataWindows.ModifierRightAlt] = new(ModifierKey.AltModifier, KeyboardSide.Right),
        [RawKeyEventDataWindows.ModifierLeftShift] = new(ModifierKey.ShiftModifier, KeyboardSide.Left),
        [RawKeyEventDataWindows.ModifierRightShift] = new(ModifierKey.ShiftModifier, KeyboardSide.Right),
        [RawKeyEventDataWindows.ModifierLeftControl] = new(ModifierKey.ControlModifier, KeyboardSide.Left),
        [RawKeyEventDataWindows.ModifierRightControl] = new(ModifierKey.ControlModifier, KeyboardSide.Right),
        [RawKeyEventDataWindows.ModifierLeftMeta] = new(ModifierKey.MetaModifier, KeyboardSide.Left),
        [RawKeyEventDataWindows.ModifierRightMeta] = new(ModifierKey.MetaModifier, KeyboardSide.Right),
        [RawKeyEventDataWindows.ModifierShift] = new(ModifierKey.ShiftModifier, KeyboardSide.All),
        [RawKeyEventDataWindows.ModifierControl] = new(ModifierKey.ControlModifier, KeyboardSide.All),
        [RawKeyEventDataWindows.ModifierAlt] = new(ModifierKey.AltModifier, KeyboardSide.All),
        [RawKeyEventDataWindows.ModifierCaps] = new(ModifierKey.CapsLockModifier, KeyboardSide.All),
        [RawKeyEventDataWindows.ModifierNumLock] = new(ModifierKey.NumLockModifier, KeyboardSide.All),
        [RawKeyEventDataWindows.ModifierScrollLock] = new(ModifierKey.ScrollLockModifier, KeyboardSide.All),
    };

    [Fact]
    public void Windows_ModifierKeysAreRecognizedIndividually()
    {
        foreach (int modifier in WindowsModifierTests.Keys)
        {
            RawKeyEvent @event = FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "windows",
                ["keyCode"] = 0x04,
                ["characterCodePoint"] = 0,
                ["scanCode"] = 0x04,
                ["modifiers"] = modifier,
            });
            var data = (RawKeyEventDataWindows)@event.Data;
            foreach (ModifierKey key in Enum.GetValues<ModifierKey>())
            {
                if (WindowsModifierTests[modifier].Key == key)
                {
                    Assert.True(
                        data.IsModifierPressed(key, side: WindowsModifierTests[modifier].Side),
                        $"{key} should be pressed with modifier {modifier}, but isn't.");
                    Assert.Equal(WindowsModifierTests[modifier].Side, data.GetModifierSide(key));
                }
                else
                {
                    Assert.False(
                        data.IsModifierPressed(key, side: WindowsModifierTests[modifier].Side),
                        $"{key} should not be pressed with metaState {modifier}.");
                }
            }
        }
    }

    [Fact]
    public void Windows_ModifierKeysAreRecognizedWhenCombined()
    {
        foreach (int modifier in WindowsModifierTests.Keys)
        {
            if (modifier == RawKeyEventDataWindows.ModifierCaps)
            {
                // No need to combine caps lock key with itself.
                continue;
            }

            RawKeyEvent @event = FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "windows",
                ["keyCode"] = 0x04,
                ["characterCodePoint"] = 0,
                ["scanCode"] = 0x04,
                ["modifiers"] = modifier | RawKeyEventDataWindows.ModifierCaps,
            });
            var data = (RawKeyEventDataWindows)@event.Data;
            foreach (ModifierKey key in Enum.GetValues<ModifierKey>())
            {
                if (WindowsModifierTests[modifier].Key == key || key == ModifierKey.CapsLockModifier)
                {
                    Assert.True(
                        data.IsModifierPressed(key, side: WindowsModifierTests[modifier].Side),
                        $"{key} should be pressed with metaState {modifier} "
                        + $"and additional key {RawKeyEventDataWindows.ModifierCaps}, but isn't.");
                    if (key != ModifierKey.CapsLockModifier)
                    {
                        Assert.Equal(WindowsModifierTests[modifier].Side, data.GetModifierSide(key));
                    }
                    else
                    {
                        Assert.Equal(KeyboardSide.All, data.GetModifierSide(key));
                    }
                }
                else
                {
                    Assert.False(
                        data.IsModifierPressed(key, side: WindowsModifierTests[modifier].Side),
                        $"{key} should not be pressed with metaState {modifier} "
                        + $"and additional key {RawKeyEventDataWindows.ModifierCaps}.");
                }
            }
        }
    }

    [Fact]
    public void Windows_PrintableKeyboardKeysAreCorrectlyTranslated()
    {
        const int unmodifiedCharacter = 97; // ASCII value for 'a'.
        RawKeyEvent keyAEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "windows",
            ["keyCode"] = 0x00000000,
            ["characterCodePoint"] = unmodifiedCharacter,
            ["scanCode"] = 0x0000001e,
            ["modifiers"] = 0x0,
        });
        var data = (RawKeyEventDataWindows)keyAEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.KeyA, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.KeyA, data.LogicalKey);
        Assert.Equal("a", data.KeyLabel);
    }

    [Fact]
    public void Windows_ControlKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent escapeKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "windows",
            ["keyCode"] = 27, // keycode for escape key
            ["scanCode"] = 0x00000001, // scanCode for escape key
            ["characterCodePoint"] = 0,
            ["modifiers"] = 0x0,
        });
        var data = (RawKeyEventDataWindows)escapeKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.Escape, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.Escape, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    [Fact]
    public void Windows_ModifierKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent shiftLeftKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "windows",
            ["keyCode"] = 160, // keyCode for left shift.
            ["scanCode"] = 0x0000002a, // scanCode for left shift.
            ["characterCodePoint"] = 0,
            ["modifiers"] = RawKeyEventDataWindows.ModifierLeftShift,
        });
        var data = (RawKeyEventDataWindows)shiftLeftKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.ShiftLeft, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.ShiftLeft, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    [Fact]
    public void Windows_UnprintableKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent leftArrowKey = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "windows",
            ["keyCode"] = 37, // keyCode for left arrow.
            ["scanCode"] = 0x0000e04b, // scanCode for left arrow.
            ["characterCodePoint"] = 0,
            ["modifiers"] = 0,
        });
        var data = (RawKeyEventDataWindows)leftArrowKey.Data;
        Assert.Equal(PhysicalKeyboardKey.ArrowLeft, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.ArrowLeft, data.LogicalKey);
    }

    [Theory]
    [MemberData(
        nameof(KeySimulatorTransitModeVariant.KeyDataThenRawKeyData),
        MemberType = typeof(KeySimulatorTransitModeVariant))]
    public void Windows_Win32VkProcessKeyEventsAreSkipped(KeyDataTransitMode mode)
    {
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            using var tester = new FrameworkDartTester();
            const string platform = "windows";
            bool lastHandled = true;
            var events = new List<RawKeyEvent>();

            // Test both code paths: addListener, and FocusNode.onKey.
            RawKeyboard.Instance.AddListener(events.Add);
            try
            {
                var node = new FocusNode(onKey: (_, @event) =>
                {
                    events.Add(@event);
                    return KeyEventResult.Ignored;
                });
                try
                {
                    tester.PumpWidget(new RawKeyboardListener(focusNode: node, child: new Container()));
                    node.RequestFocus();
                    tester.PumpAndSettle();

                    // Dispatch an arbitrary key press for the correct transit mode.
                    KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA);
                    KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.KeyA);
                    Assert.Equal(4, events.Count);
                    events.Clear();

                    // Simulate raw events because VK_PROCESSKEY does not exist in the key mapping.
                    void SimulateKeyEventMessage(string type, int keyCode, int scanCode)
                    {
                        Task delivery = ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
                            SystemChannels.KeyEvent.Name,
                            SystemChannels.KeyEvent.Codec.EncodeMessage(new Dictionary<string, object?>
                            {
                                ["type"] = type,
                                ["keymap"] = platform,
                                ["keyCode"] = keyCode,
                                ["scanCode"] = scanCode,
                                ["modifiers"] = 0,
                            }),
                            data =>
                            {
                                var decoded = (IReadOnlyDictionary<string, object?>)
                                    SystemChannels.KeyEvent.Codec.DecodeMessage(data)!;
                                lastHandled = (bool)decoded["handled"]!;
                            });
                        delivery.GetAwaiter().GetResult();
                    }

                    SimulateKeyEventMessage("keydown", 229, 30);
                    Assert.Empty(events);
                    Assert.True(lastHandled);
                    Assert.Empty(RawKeyboard.Instance.KeysPressed);
                    SimulateKeyEventMessage("keyup", 65, 30);
                    Assert.Empty(events);
                    Assert.True(lastHandled);
                    Assert.Empty(RawKeyboard.Instance.KeysPressed);
                }
                finally
                {
                    node.Dispose();
                }
            }
            finally
            {
                RawKeyboard.Instance.RemoveListener(events.Add);
            }
        });
    }

    [Fact]
    public void Windows_DataToString()
    {
        Assert.Equal(
            "RawKeyEventDataWindows#00000(keyCode: 16, scanCode: 30, characterCodePoint: 10, modifiers: 32)",
            IgnoringHashCodes(FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "windows",
                ["keyCode"] = 0x00000010,
                ["characterCodePoint"] = 10,
                ["scanCode"] = 0x0000001e,
                ["modifiers"] = 0x20,
            }).Data.ToString()));
    }

    [Fact]
    public void Windows_DataEquality()
    {
        Assert.Equal<RawKeyEventData>(
            new RawKeyEventDataWindows(
                keyCode: 0x00000010,
                scanCode: 0x1e,
                modifiers: 0x20,
                characterCodePoint: 10),
            FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "windows",
                ["keyCode"] = 0x00000010,
                ["characterCodePoint"] = 10,
                ["scanCode"] = 0x0000001e,
                ["modifiers"] = 0x20,
            }).Data);

        Assert.NotEqual<RawKeyEventData>(
            new RawKeyEventDataWindows(),
            FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "windows",
                ["keyCode"] = 0x00000010,
                ["characterCodePoint"] = 10,
                ["scanCode"] = 0x0000001e,
                ["modifiers"] = 0x20,
            }).Data);
    }

    // ---- group('RawKeyEventDataLinux-GLFW') ----

    private static readonly Dictionary<int, ModifierCheck> GlfwModifierTests = new()
    {
        [GLFWKeyHelper.ModifierAlt] = new(ModifierKey.AltModifier, KeyboardSide.All),
        [GLFWKeyHelper.ModifierShift] = new(ModifierKey.ShiftModifier, KeyboardSide.All),
        [GLFWKeyHelper.ModifierControl] = new(ModifierKey.ControlModifier, KeyboardSide.All),
        [GLFWKeyHelper.ModifierMeta] = new(ModifierKey.MetaModifier, KeyboardSide.All),
        [GLFWKeyHelper.ModifierNumericPad] = new(ModifierKey.NumLockModifier, KeyboardSide.All),
        [GLFWKeyHelper.ModifierCapsLock] = new(ModifierKey.CapsLockModifier, KeyboardSide.All),
    };

    // How modifiers are interpreted depends upon the keyCode for GLFW.
    private static int GlfwKeyCodeForModifier(int modifier, bool isLeft) => modifier switch
    {
        GLFWKeyHelper.ModifierAlt => isLeft ? 342 : 346,
        GLFWKeyHelper.ModifierShift => isLeft ? 340 : 344,
        GLFWKeyHelper.ModifierControl => isLeft ? 341 : 345,
        GLFWKeyHelper.ModifierMeta => isLeft ? 343 : 347,
        GLFWKeyHelper.ModifierNumericPad => 282,
        GLFWKeyHelper.ModifierCapsLock => 280,
        _ => 65, // keyA
    };

    [Fact]
    public void LinuxGlfw_ModifierKeysAreRecognizedIndividually()
    {
        LinuxModifierKeysAreRecognizedIndividually("glfw", GlfwModifierTests, GlfwKeyCodeForModifier);
    }

    [Fact]
    public void LinuxGlfw_ModifierKeysAreRecognizedWhenCombined()
    {
        LinuxModifierKeysAreRecognizedWhenCombined("glfw", GlfwModifierTests, GLFWKeyHelper.ModifierControl);
    }

    [Fact]
    public void LinuxGlfw_PrintableKeyboardKeysAreCorrectlyTranslated()
    {
        LinuxPrintableKeyboardKeysAreCorrectlyTranslated("glfw");
    }

    [Fact]
    public void LinuxGlfw_CodePointsWithTwoUnicodeScalarValuesAreAllowed()
    {
        LinuxCodePointsWithTwoUnicodeScalarValuesAreAllowed("glfw");
    }

    [Fact]
    public void LinuxGlfw_CodePointsWithMoreThanThreeUnicodeScalarValuesAreNotAllowed()
    {
        LinuxCodePointsWithMoreThanThreeUnicodeScalarValuesAreNotAllowed("glfw");
    }

    [Fact]
    public void LinuxGlfw_ControlKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent escapeKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "linux",
            ["toolkit"] = "glfw",
            ["keyCode"] = 256,
            ["scanCode"] = 0x00000009,
            ["unicodeScalarValues"] = 0,
            ["modifiers"] = 0x0,
        });
        var data = (RawKeyEventDataLinux)escapeKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.Escape, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.Escape, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    [Fact]
    public void LinuxGlfw_ModifierKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent shiftLeftKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "linux",
            ["toolkit"] = "glfw",
            ["keyCode"] = 340,
            ["scanCode"] = 0x00000032,
            ["unicodeScalarValues"] = 0,
        });
        var data = (RawKeyEventDataLinux)shiftLeftKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.ShiftLeft, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.ShiftLeft, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    [Fact]
    public void LinuxGlfw_DataToString()
    {
        Assert.Equal(
            "RawKeyEventDataLinux#00000(toolkit: GLFW, unicodeScalarValues: 1114111, scanCode: 38, keyCode: 65, "
            + "modifiers: 16, isDown: true)",
            IgnoringHashCodes(FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "linux",
                ["toolkit"] = "glfw",
                ["keyCode"] = 65,
                ["scanCode"] = 0x00000026,
                ["unicodeScalarValues"] = 0x10FFFF,
                ["modifiers"] = 0x10,
            }).Data.ToString()));
    }

    [Fact]
    public void LinuxGlfw_DataEquality()
    {
        Assert.Equal<RawKeyEventData>(
            new RawKeyEventDataLinux(
                keyHelper: KeyHelper.Create("glfw"),
                unicodeScalarValues: 0x10FFFF,
                keyCode: 65,
                scanCode: 0x26,
                modifiers: 0x10,
                isDown: true),
            FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "linux",
                ["toolkit"] = "glfw",
                ["keyCode"] = 65,
                ["scanCode"] = 0x00000026,
                ["unicodeScalarValues"] = 0x10FFFF,
                ["modifiers"] = 0x10,
            }).Data);

        Assert.NotEqual<RawKeyEventData>(
            new RawKeyEventDataLinux(keyHelper: KeyHelper.Create("glfw"), isDown: true),
            FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "linux",
                ["toolkit"] = "glfw",
                ["keyCode"] = 65,
                ["scanCode"] = 0x00000026,
                ["unicodeScalarValues"] = 0x10FFFF,
                ["modifiers"] = 0x10,
            }).Data);
    }

    // ---- group('RawKeyEventDataLinux-GTK') ----

    private static readonly Dictionary<int, ModifierCheck> GtkModifierTests = new()
    {
        [GtkKeyHelper.ModifierMod1] = new(ModifierKey.AltModifier, KeyboardSide.All),
        [GtkKeyHelper.ModifierShift] = new(ModifierKey.ShiftModifier, KeyboardSide.All),
        [GtkKeyHelper.ModifierControl] = new(ModifierKey.ControlModifier, KeyboardSide.All),
        [GtkKeyHelper.ModifierMeta] = new(ModifierKey.MetaModifier, KeyboardSide.All),
        [GtkKeyHelper.ModifierMod2] = new(ModifierKey.NumLockModifier, KeyboardSide.All),
        [GtkKeyHelper.ModifierCapsLock] = new(ModifierKey.CapsLockModifier, KeyboardSide.All),
    };

    // How modifiers are interpreted depends upon the keyCode for GTK.
    private static int GtkKeyCodeForModifier(int modifier, bool isLeft) => modifier switch
    {
        GtkKeyHelper.ModifierShift => isLeft ? 65505 : 65506,
        GtkKeyHelper.ModifierControl => isLeft ? 65507 : 65508,
        GtkKeyHelper.ModifierMeta => isLeft ? 65515 : 65516,
        GtkKeyHelper.ModifierMod1 => 65513,
        GtkKeyHelper.ModifierMod2 => 65407,
        GtkKeyHelper.ModifierCapsLock => 65509,
        _ => 65, // keyA
    };

    [Fact]
    public void LinuxGtk_ModifierKeysAreRecognizedIndividually()
    {
        LinuxModifierKeysAreRecognizedIndividually("gtk", GtkModifierTests, GtkKeyCodeForModifier);
    }

    [Fact]
    public void LinuxGtk_ModifierKeysAreRecognizedWhenCombined()
    {
        LinuxModifierKeysAreRecognizedWhenCombined("gtk", GtkModifierTests, GtkKeyHelper.ModifierControl);
    }

    [Fact]
    public void LinuxGtk_PrintableKeyboardKeysAreCorrectlyTranslated()
    {
        LinuxPrintableKeyboardKeysAreCorrectlyTranslated("gtk");
    }

    [Fact]
    public void LinuxGtk_CodePointsWithTwoUnicodeScalarValuesAreAllowed()
    {
        LinuxCodePointsWithTwoUnicodeScalarValuesAreAllowed("gtk");
    }

    [Fact]
    public void LinuxGtk_CodePointsWithMoreThanThreeUnicodeScalarValuesAreNotAllowed()
    {
        LinuxCodePointsWithMoreThanThreeUnicodeScalarValuesAreNotAllowed("gtk");
    }

    [Fact]
    public void LinuxGtk_ControlKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent escapeKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "linux",
            ["toolkit"] = "gtk",
            ["keyCode"] = 65307,
            ["scanCode"] = 0x00000009,
            ["unicodeScalarValues"] = 0,
            ["modifiers"] = 0x0,
        });
        var data = (RawKeyEventDataLinux)escapeKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.Escape, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.Escape, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    [Fact]
    public void LinuxGtk_ModifierKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent shiftLeftKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "linux",
            ["toolkit"] = "gtk",
            ["keyCode"] = 65505,
            ["scanCode"] = 0x00000032,
            ["unicodeScalarValues"] = 0,
        });
        var data = (RawKeyEventDataLinux)shiftLeftKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.ShiftLeft, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.ShiftLeft, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
    }

    [Fact]
    public void LinuxGtk_PrioritizeLogicalKeyFromSpecifiedLogicalKey()
    {
        RawKeyEvent digit1FromFrench = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "linux",
            ["toolkit"] = "gtk",
            ["keyCode"] = 0x6c6,
            ["scanCode"] = 0x26,
            ["unicodeScalarValues"] = 0x424,
            ["specifiedLogicalKey"] = 0x61,
        });
        var data = (RawKeyEventDataLinux)digit1FromFrench.Data;
        Assert.Equal(PhysicalKeyboardKey.KeyA, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.KeyA, data.LogicalKey);
    }

    [Fact]
    public void LinuxGtk_DataToString()
    {
        Assert.Equal(
            "RawKeyEventDataLinux#00000(toolkit: GTK, unicodeScalarValues: 113, scanCode: 38, keyCode: 65, "
            + "modifiers: 16, isDown: true)",
            IgnoringHashCodes(FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "linux",
                ["toolkit"] = "gtk",
                ["keyCode"] = 65,
                ["scanCode"] = 0x00000026,
                ["unicodeScalarValues"] = 113,
                ["modifiers"] = 0x10,
            }).Data.ToString()));
    }

    [Fact]
    public void LinuxGtk_DataEquality()
    {
        Dictionary<string, object?> Message() => new()
        {
            ["type"] = "keydown",
            ["keymap"] = "linux",
            ["toolkit"] = "gtk",
            ["keyCode"] = 65,
            ["scanCode"] = 0x00000026,
            ["unicodeScalarValues"] = 113,
            ["modifiers"] = 0x10,
        };

        Assert.Equal<RawKeyEventData>(
            new RawKeyEventDataLinux(
                keyHelper: KeyHelper.Create("gtk"),
                unicodeScalarValues: 113,
                keyCode: 65,
                scanCode: 0x26,
                modifiers: 0x10,
                isDown: true),
            FromMessage(Message()).Data);

        Assert.NotEqual<RawKeyEventData>(
            new RawKeyEventDataLinux(
                keyHelper: KeyHelper.Create("glfw"),
                unicodeScalarValues: 113,
                keyCode: 65,
                scanCode: 0x26,
                modifiers: 0x10,
                isDown: true),
            FromMessage(Message()).Data);

        Assert.NotEqual<RawKeyEventData>(
            new RawKeyEventDataLinux(keyHelper: KeyHelper.Create("gtk"), isDown: true),
            FromMessage(Message()).Data);
    }

    // ---- Shared bodies of the GLFW and GTK groups (the Dart groups repeat them verbatim). ----

    private static void LinuxModifierKeysAreRecognizedIndividually(
        string toolkit,
        Dictionary<int, ModifierCheck> modifierTests,
        Func<int, bool, int> keyCodeForModifier)
    {
        foreach (int modifier in modifierTests.Keys)
        {
            foreach (bool isDown in new[] { true, false })
            {
                foreach (bool isLeft in new[] { true, false })
                {
                    RawKeyEvent @event = FromMessage(new Dictionary<string, object?>
                    {
                        ["type"] = isDown ? "keydown" : "keyup",
                        ["keymap"] = "linux",
                        ["toolkit"] = toolkit,
                        ["keyCode"] = keyCodeForModifier(modifier, isLeft),
                        ["scanCode"] = 0x00000026,
                        ["unicodeScalarValues"] = 97,
                        // GLFW/GTK modifiers don't include the current key event.
                        ["modifiers"] = isDown ? 0 : modifier,
                    });
                    var data = (RawKeyEventDataLinux)@event.Data;
                    string side = isLeft ? "left" : "right";
                    string state = isDown ? "down" : "up";
                    foreach (ModifierKey key in Enum.GetValues<ModifierKey>())
                    {
                        if (modifierTests[modifier].Key == key)
                        {
                            Assert.True(
                                data.IsModifierPressed(key, side: modifierTests[modifier].Side) == isDown,
                                $"{side} {key} {(isDown ? "should" : "should not")} be pressed with metaState "
                                + $"{modifier}, when key is {state}, but isn't.");
                            Assert.Equal(modifierTests[modifier].Side, data.GetModifierSide(key));
                        }
                        else
                        {
                            Assert.False(
                                data.IsModifierPressed(key, side: modifierTests[modifier].Side),
                                $"{side} {key} should not be pressed with metaState {modifier}, "
                                + $"when key is {state}, but is.");
                        }
                    }
                }
            }
        }
    }

    private static void LinuxModifierKeysAreRecognizedWhenCombined(
        string toolkit,
        Dictionary<int, ModifierCheck> modifierTests,
        int modifierControl)
    {
        foreach (int modifier in modifierTests.Keys)
        {
            if (modifier == modifierControl)
            {
                // No need to combine CTRL key with itself.
                continue;
            }

            RawKeyEvent @event = FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "linux",
                ["toolkit"] = toolkit,
                ["keyCode"] = 65,
                ["scanCode"] = 0x00000026,
                ["unicodeScalarValues"] = 97,
                ["modifiers"] = modifier | modifierControl,
            });
            var data = (RawKeyEventDataLinux)@event.Data;
            foreach (ModifierKey key in Enum.GetValues<ModifierKey>())
            {
                if (modifierTests[modifier].Key == key || key == ModifierKey.ControlModifier)
                {
                    Assert.True(
                        data.IsModifierPressed(key, side: modifierTests[modifier].Side),
                        $"{key} should be pressed with metaState {modifier} "
                        + $"and additional key {modifierControl}, but isn't.");
                    if (key != ModifierKey.ControlModifier)
                    {
                        Assert.Equal(modifierTests[modifier].Side, data.GetModifierSide(key));
                    }
                    else
                    {
                        Assert.Equal(KeyboardSide.All, data.GetModifierSide(key));
                    }
                }
                else
                {
                    Assert.False(
                        data.IsModifierPressed(key, side: modifierTests[modifier].Side),
                        $"{key} should not be pressed with metaState {modifier} "
                        + $"and additional key {modifierControl}.");
                }
            }
        }
    }

    private static void LinuxPrintableKeyboardKeysAreCorrectlyTranslated(string toolkit)
    {
        RawKeyEvent keyAEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "linux",
            ["toolkit"] = toolkit,
            ["keyCode"] = 65,
            ["scanCode"] = 0x00000026,
            ["unicodeScalarValues"] = 113,
            ["modifiers"] = 0x0,
        });
        var data = (RawKeyEventDataLinux)keyAEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.KeyA, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.KeyQ, data.LogicalKey);
        Assert.Equal("q", data.KeyLabel);
    }

    private static void LinuxCodePointsWithTwoUnicodeScalarValuesAreAllowed(string toolkit)
    {
        RawKeyEvent keyAEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "linux",
            ["toolkit"] = toolkit,
            ["keyCode"] = 65,
            ["scanCode"] = 0x00000026,
            ["unicodeScalarValues"] = 0x10FFFF,
            ["modifiers"] = 0x0,
        });
        var data = (RawKeyEventDataLinux)keyAEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.KeyA, data.PhysicalKey);
        Assert.Equal(0x10FFFF, data.LogicalKey.KeyId);
        Assert.Equal("\U0010FFFF", data.KeyLabel);
    }

    private static void LinuxCodePointsWithMoreThanThreeUnicodeScalarValuesAreNotAllowed(string toolkit)
    {
        // |keyCode| and |scanCode| are arbitrary values. This test should fail due to an invalid
        // |unicodeScalarValues|.
        void CreateFailingKey()
        {
            FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "linux",
                ["toolkit"] = toolkit,
                ["keyCode"] = 65,
                ["scanCode"] = 0x00000026,
                ["unicodeScalarValues"] = 0x1F00000000L,
                ["modifiers"] = 0x0,
            });
        }

        Assert.Throws<AssertionError>(CreateFailingKey);
    }

    // ---- group('RawKeyEventDataWeb') ----

    private static readonly Dictionary<int, ModifierKey> WebModifierTests = new()
    {
        [RawKeyEventDataWeb.ModifierAlt] = ModifierKey.AltModifier,
        [RawKeyEventDataWeb.ModifierShift] = ModifierKey.ShiftModifier,
        [RawKeyEventDataWeb.ModifierControl] = ModifierKey.ControlModifier,
        [RawKeyEventDataWeb.ModifierMeta] = ModifierKey.MetaModifier,
        [RawKeyEventDataWeb.ModifierCapsLock] = ModifierKey.CapsLockModifier,
        [RawKeyEventDataWeb.ModifierNumLock] = ModifierKey.NumLockModifier,
        [RawKeyEventDataWeb.ModifierScrollLock] = ModifierKey.ScrollLockModifier,
    };

    private static readonly Dictionary<string, LogicalKeyboardKey> WebModifierTestsWithNoLocation = new()
    {
        ["Alt"] = LogicalKeyboardKey.AltLeft,
        ["Shift"] = LogicalKeyboardKey.ShiftLeft,
        ["Control"] = LogicalKeyboardKey.ControlLeft,
        ["Meta"] = LogicalKeyboardKey.MetaLeft,
    };

    [Fact]
    public void Web_ModifierKeysAreRecognizedIndividually()
    {
        foreach (int modifier in WebModifierTests.Keys)
        {
            RawKeyEvent @event = FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "web",
                ["code"] = "RandomCode",
                ["metaState"] = modifier,
            });
            var data = (RawKeyEventDataWeb)@event.Data;
            foreach (ModifierKey key in Enum.GetValues<ModifierKey>())
            {
                if (WebModifierTests[modifier] == key)
                {
                    Assert.True(
                        data.IsModifierPressed(key),
                        $"{key} should be pressed with metaState {modifier}, but isn't.");
                }
                else
                {
                    Assert.False(
                        data.IsModifierPressed(key),
                        $"{key} should not be pressed with metaState {modifier}.");
                }
            }
        }
    }

    [Fact]
    public void Web_ModifierKeysAreRecognizedWhenCombined()
    {
        foreach (int modifier in WebModifierTests.Keys)
        {
            if (modifier == RawKeyEventDataWeb.ModifierMeta)
            {
                // No need to combine meta key with itself.
                continue;
            }

            RawKeyEvent @event = FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "web",
                ["code"] = "RandomCode",
                ["metaState"] = modifier | RawKeyEventDataWeb.ModifierMeta,
            });
            var data = (RawKeyEventDataWeb)@event.Data;
            foreach (ModifierKey key in Enum.GetValues<ModifierKey>())
            {
                if (WebModifierTests[modifier] == key || key == ModifierKey.MetaModifier)
                {
                    Assert.True(
                        data.IsModifierPressed(key),
                        $"{key} should be pressed with metaState {modifier} "
                        + $"and additional key {RawKeyEventDataWeb.ModifierMeta}, but isn't.");
                }
                else
                {
                    Assert.False(
                        data.IsModifierPressed(key),
                        $"{key} should not be pressed with metaState {modifier} "
                        + $"and additional key {RawKeyEventDataWeb.ModifierMeta}.");
                }
            }
        }
    }

    [Fact]
    public void Web_ModifierKeysWithNoLocationAreMappedToLeft()
    {
        foreach (string modifierKey in WebModifierTestsWithNoLocation.Keys)
        {
            RawKeyEvent @event = FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "web",
                ["key"] = modifierKey,
                ["location"] = 0,
            });
            var data = (RawKeyEventDataWeb)@event.Data;
            Assert.Equal(WebModifierTestsWithNoLocation[modifierKey], data.LogicalKey);
        }
    }

    [Fact]
    public void Web_LowerLetterKeysAreCorrectlyTranslated()
    {
        RawKeyEvent keyAEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "web",
            ["code"] = "KeyA",
            ["key"] = "a",
            ["location"] = 0,
            ["metaState"] = 0x0,
            ["keyCode"] = 0x41,
        });
        var data = (RawKeyEventDataWeb)keyAEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.KeyA, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.KeyA, data.LogicalKey);
        Assert.Equal("a", data.KeyLabel);
        Assert.Equal(0x41, data.KeyCode);
    }

    [Fact]
    public void Web_UpperLetterKeysAreCorrectlyTranslated()
    {
        RawKeyEvent keyAEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "web",
            ["code"] = "KeyA",
            ["key"] = "A",
            ["location"] = 0,
            ["metaState"] = 0x1, // Shift
            ["keyCode"] = 0x41,
        });
        var data = (RawKeyEventDataWeb)keyAEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.KeyA, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.KeyA, data.LogicalKey);
        Assert.Equal("A", data.KeyLabel);
        Assert.Equal(0x41, data.KeyCode);
    }

    [Fact]
    public void Web_ControlKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent escapeKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "web",
            ["code"] = "Escape",
            ["key"] = "Escape",
            ["location"] = 0,
            ["metaState"] = 0x0,
            ["keyCode"] = 0x1B,
        });
        var data = (RawKeyEventDataWeb)escapeKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.Escape, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.Escape, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
        Assert.Equal(0x1B, data.KeyCode);
    }

    [Fact]
    public void Web_ModifierKeyboardKeysAreCorrectlyTranslated()
    {
        RawKeyEvent shiftKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "web",
            ["code"] = "ShiftLeft",
            ["key"] = "Shift",
            ["location"] = 1,
            ["metaState"] = RawKeyEventDataWeb.ModifierShift,
            ["keyCode"] = 0x10,
        });
        var data = (RawKeyEventDataWeb)shiftKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.ShiftLeft, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.ShiftLeft, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
        Assert.Equal(0x10, data.KeyCode);
    }

    [Fact]
    public void Web_EscKeysGeneratedByOlderBrowsersAreCorrectlyTranslated()
    {
        RawKeyEvent escapeKeyEvent = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "web",
            ["code"] = "Esc",
            ["key"] = "Esc",
            ["location"] = 0,
            ["metaState"] = 0x0,
            ["keyCode"] = 0x1B,
        });
        var data = (RawKeyEventDataWeb)escapeKeyEvent.Data;
        Assert.Equal(PhysicalKeyboardKey.Escape, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.Escape, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
        Assert.Equal(0x1B, data.KeyCode);
    }

    [Fact]
    public void Web_ArrowKeysFromAKeyboardGiveCorrectPhysicalKeyMappings()
    {
        RawKeyEvent arrowKeyDown = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "web",
            ["code"] = "ArrowDown",
            ["key"] = "ArrowDown",
            ["location"] = 0,
            ["metaState"] = 0x0,
            ["keyCode"] = 0x28,
        });
        var data = (RawKeyEventDataWeb)arrowKeyDown.Data;
        Assert.Equal(PhysicalKeyboardKey.ArrowDown, data.PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.ArrowDown, data.LogicalKey);
        Assert.Empty(data.KeyLabel);
        Assert.Equal(0x28, data.KeyCode);
    }

    [Fact]
    public void Web_UnrecognizedKeysAreMappedToWebPlane()
    {
        RawKeyEvent arrowKeyDown = FromMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "web",
            ["code"] = "Unrecog1",
            ["key"] = "Unrecog2",
            ["location"] = 0,
            ["metaState"] = 0x0,
        });
        var data = (RawKeyEventDataWeb)arrowKeyDown.Data;
        // This might be easily broken on Web if the code fails to acknowledge
        // that JavaScript doesn't handle 64-bit bit-wise operation.
        Assert.True(data.PhysicalKey.UsbHidUsage > 0x01700000000);
        Assert.True(data.PhysicalKey.UsbHidUsage < 0x01800000000);
        Assert.True(data.LogicalKey.KeyId > 0x01700000000);
        Assert.True(data.LogicalKey.KeyId < 0x01800000000);
        Assert.Empty(data.KeyLabel);
        Assert.Equal(0, data.KeyCode);
    }

    [Fact]
    public void Web_DataToString()
    {
        Assert.Equal(
            "RawKeyEventDataWeb#00000(code: KeyA, key: a, location: 2, metaState: 16, keyCode: 65)",
            IgnoringHashCodes(FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "web",
                ["code"] = "KeyA",
                ["key"] = "a",
                ["location"] = 2,
                ["metaState"] = 0x10,
                ["keyCode"] = 0x41,
            }).Data.ToString()));

        // Without location
        Assert.Equal(
            "RawKeyEventDataWeb#00000(code: KeyA, key: a, location: 0, metaState: 16, keyCode: 65)",
            IgnoringHashCodes(FromMessage(new Dictionary<string, object?>
            {
                ["type"] = "keydown",
                ["keymap"] = "web",
                ["code"] = "KeyA",
                ["key"] = "a",
                ["metaState"] = 0x10,
                ["keyCode"] = 0x41,
            }).Data.ToString()));
    }

    [Fact]
    public void Web_DataEquality()
    {
        Dictionary<string, object?> Message() => new()
        {
            ["type"] = "keydown",
            ["keymap"] = "web",
            ["code"] = "KeyA",
            ["key"] = "a",
            ["location"] = 2,
            ["metaState"] = 0x10,
            ["keyCode"] = 0x41,
        };

        Assert.Equal<RawKeyEventData>(
            new RawKeyEventDataWeb(
                key: "a",
                code: "KeyA",
                location: 2,
                metaState: 0x10,
                keyCode: 0x41),
            FromMessage(Message()).Data);

        Assert.NotEqual<RawKeyEventData>(
            new RawKeyEventDataWeb(code: "KeyA", key: "a"),
            FromMessage(Message()).Data);
    }
}
#pragma warning restore CS0618
