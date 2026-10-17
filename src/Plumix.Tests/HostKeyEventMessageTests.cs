using KeyModifiers = Avalonia.Input.KeyModifiers;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// C#-only test coverage for src/Plumix/HostKeyEventMessage.cs, the Avalonia host's encoder of
// `flutter/keyevent` messages: every message it builds is decoded with the ported
// `RawKeyEvent.FromMessage` and must describe the same key and modifier state.

namespace Plumix.Tests;

#pragma warning disable CS0618 // The raw key platform classes mirror Flutter's deprecated API.
public sealed class HostKeyEventMessageTests : IDisposable
{
    public HostKeyEventMessageTests()
    {
        HostKeyEventMessage.Reset();
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        HostKeyEventMessage.Reset();
        FocusManager.Instance.ResetForTests();
    }

    public static TheoryData<string> Keymaps => new() { "macos", "ios", "windows", "linux", "android", "web" };

    // The keymaps whose raw data reports which side of a modifier is down.
    public static TheoryData<string> SidedKeymaps => new() { "macos", "ios", "windows", "android" };

    public static TheoryData<string, string> RoundTripKeys
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (string keymap in Keymaps)
            {
                foreach (string key in new[] { "KeyA", "Digit1", "Enter", "Numpad1", "F5", "ShiftRight" })
                {
                    data.Add(keymap, key);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(RoundTripKeys))]
    public void RoundTripPreservesThePhysicalAndLogicalKey(string keymap, string key)
    {
        (PhysicalKeyboardKey physical, LogicalKeyboardKey logical, string? keySymbol, KeyModifiers modifiers) =
            key switch
            {
                "KeyA" => (PhysicalKeyboardKey.KeyA, LogicalKeyboardKey.KeyA, "a", KeyModifiers.None),
                "Digit1" => (PhysicalKeyboardKey.Digit1, LogicalKeyboardKey.Digit1, "1", KeyModifiers.None),
                "Enter" => (PhysicalKeyboardKey.Enter, LogicalKeyboardKey.Enter, "\r", KeyModifiers.None),
                "Numpad1" => (PhysicalKeyboardKey.Numpad1, LogicalKeyboardKey.Numpad1, "1", KeyModifiers.None),
                "F5" => (PhysicalKeyboardKey.F5, LogicalKeyboardKey.F5, (string?)null, KeyModifiers.None),
                _ => (PhysicalKeyboardKey.ShiftRight, LogicalKeyboardKey.ShiftRight, null, KeyModifiers.Shift),
            };

        foreach (bool isDown in new[] { true, false })
        {
            KeyModifiers eventModifiers = isDown ? modifiers : KeyModifiers.None;
            RawKeyEvent @event = Decode(keymap, physical, logical, keySymbol, eventModifiers, isDown);
            Assert.Equal(physical, @event.PhysicalKey);
            Assert.Equal(logical, @event.LogicalKey);
            Assert.IsType(isDown ? typeof(RawKeyDownEvent) : typeof(RawKeyUpEvent), @event);
        }
    }

    [Theory]
    [MemberData(nameof(Keymaps))]
    public void APrintableKeyCarriesItsCharacterOnKeyDown(string keymap)
    {
        RawKeyEvent down = DecodeKeyA(keymap, "a", KeyModifiers.None);
        Assert.Equal("a", down.Character);

        RawKeyEvent enter = Decode(
            keymap,
            PhysicalKeyboardKey.Enter,
            LogicalKeyboardKey.Enter,
            "\r",
            KeyModifiers.None);
        // macOS forwards `characters` verbatim, so a key without one has the empty character, which
        // KeyEventManager turns into null.
        Assert.True(string.IsNullOrEmpty(enter.Character));
    }

    [Theory]
    [MemberData(nameof(SidedKeymaps))]
    public void AHeldRightModifierIsReportedOnTheRight(string keymap)
    {
        RawKeyEvent shiftDown = Decode(
            keymap,
            PhysicalKeyboardKey.ShiftRight,
            LogicalKeyboardKey.ShiftRight,
            null,
            KeyModifiers.Shift);
        RawKeyEvent keyDown = DecodeKeyA(keymap, "A", KeyModifiers.Shift);

        Assert.True(shiftDown.Data.IsModifierPressed(ModifierKey.ShiftModifier, KeyboardSide.Right));
        Assert.True(keyDown.Data.IsModifierPressed(ModifierKey.ShiftModifier, KeyboardSide.Right));
        Assert.False(keyDown.Data.IsModifierPressed(ModifierKey.ShiftModifier, KeyboardSide.Left));
        Assert.Equal(KeyboardSide.Right, keyDown.Data.GetModifierSide(ModifierKey.ShiftModifier));
    }

    [Theory]
    [MemberData(nameof(Keymaps))]
    public void RawKeyboardSynchronizesAHeldRightModifierToTheRightKey(string keymap)
    {
        RawKeyboard raw = RawKeyboard.Instance;
        raw.HandleRawKeyEvent(Decode(
            keymap,
            PhysicalKeyboardKey.ShiftRight,
            LogicalKeyboardKey.ShiftRight,
            null,
            KeyModifiers.Shift));
        raw.HandleRawKeyEvent(DecodeKeyA(keymap, "A", KeyModifiers.Shift));

        Assert.Contains(LogicalKeyboardKey.ShiftRight, raw.KeysPressed);
        Assert.Contains(LogicalKeyboardKey.KeyA, raw.KeysPressed);
        if (keymap == "linux")
        {
            // GTK's modifier state has no sides, so Dart marks both sides down.
            Assert.Contains(LogicalKeyboardKey.ShiftLeft, raw.KeysPressed);
        }
        else
        {
            Assert.DoesNotContain(LogicalKeyboardKey.ShiftLeft, raw.KeysPressed);
        }

        // Releasing the key and then the modifier clears everything.
        raw.HandleRawKeyEvent(Decode(
            keymap,
            PhysicalKeyboardKey.KeyA,
            LogicalKeyboardKey.KeyA,
            null,
            KeyModifiers.Shift,
            isDown: false));
        raw.HandleRawKeyEvent(Decode(
            keymap,
            PhysicalKeyboardKey.ShiftRight,
            LogicalKeyboardKey.ShiftRight,
            null,
            KeyModifiers.None,
            isDown: false));
        Assert.Empty(raw.KeysPressed);
    }

    [Theory]
    [MemberData(nameof(SidedKeymaps))]
    public void AModifierPressedWhileUnfocusedIsReportedOnTheLeft(string keymap)
    {
        // The control key went down before the window had focus: only Avalonia's flag reports it.
        RawKeyEvent keyDown = DecodeKeyA(keymap, "a", KeyModifiers.Control);

        Assert.True(keyDown.Data.IsModifierPressed(ModifierKey.ControlModifier, KeyboardSide.Left));
        Assert.False(keyDown.Data.IsModifierPressed(ModifierKey.ControlModifier, KeyboardSide.Right));

        RawKeyboard.Instance.HandleRawKeyEvent(keyDown);
        Assert.Equal(
            new HashSet<LogicalKeyboardKey> { LogicalKeyboardKey.ControlLeft, LogicalKeyboardKey.KeyA },
            RawKeyboard.Instance.KeysPressed);

        // A modifier released while unfocused is dropped once the flag disappears.
        RawKeyEvent keyUp = Decode(
            keymap,
            PhysicalKeyboardKey.KeyA,
            LogicalKeyboardKey.KeyA,
            null,
            KeyModifiers.None,
            isDown: false);
        Assert.False(keyUp.Data.IsControlPressed);
        RawKeyboard.Instance.HandleRawKeyEvent(keyUp);
        Assert.Empty(RawKeyboard.Instance.KeysPressed);
    }

    [Fact]
    public void GtkModifiersAreTheStateBeforeTheEvent()
    {
        int ShiftBit(Dictionary<string, object?> message) =>
            (int)message["modifiers"]! & GtkKeyHelper.ModifierShift;

        Dictionary<string, object?> shiftDown = Encode(
            "linux",
            PhysicalKeyboardKey.ShiftLeft,
            LogicalKeyboardKey.ShiftLeft,
            null,
            KeyModifiers.Shift);
        Assert.Equal(0, ShiftBit(shiftDown));
        // GtkKeyHelper merges the event's own modifier key into the state.
        Assert.True(RawKeyEvent.FromMessage(shiftDown).Data.IsShiftPressed);

        Dictionary<string, object?> keyDown = Encode(
            "linux",
            PhysicalKeyboardKey.KeyA,
            LogicalKeyboardKey.KeyA,
            "A",
            KeyModifiers.Shift);
        Assert.Equal(GtkKeyHelper.ModifierShift, ShiftBit(keyDown));

        Dictionary<string, object?> shiftUp = Encode(
            "linux",
            PhysicalKeyboardKey.ShiftLeft,
            LogicalKeyboardKey.ShiftLeft,
            null,
            KeyModifiers.None,
            isDown: false);
        Assert.Equal(GtkKeyHelper.ModifierShift, ShiftBit(shiftUp));
        Assert.False(RawKeyEvent.FromMessage(shiftUp).Data.IsShiftPressed);

        Dictionary<string, object?> nextKey = Encode(
            "linux",
            PhysicalKeyboardKey.KeyB,
            LogicalKeyboardKey.KeyB,
            "b",
            KeyModifiers.None);
        Assert.Equal(0, ShiftBit(nextKey));
    }

    [Theory]
    [MemberData(nameof(Keymaps))]
    public void CapsLockTogglesOnEachPress(string keymap)
    {
        bool CapsLockOn(bool isDown, PhysicalKeyboardKey physical, LogicalKeyboardKey logical, string? symbol) =>
            Decode(keymap, physical, logical, symbol, KeyModifiers.None, isDown)
                .Data.IsModifierPressed(ModifierKey.CapsLockModifier);

        PhysicalKeyboardKey caps = PhysicalKeyboardKey.CapsLock;
        LogicalKeyboardKey capsLogical = LogicalKeyboardKey.CapsLock;
        bool gtk = keymap == "linux";

        // The first press turns the lock on and the second turns it off. GTK reports the state from
        // before each event, and GtkKeyHelper merges a lock key's own press in and its release out.
        Assert.True(CapsLockOn(true, caps, capsLogical, null));
        Assert.Equal(!gtk, CapsLockOn(false, caps, capsLogical, null));
        Assert.True(CapsLockOn(true, PhysicalKeyboardKey.KeyA, LogicalKeyboardKey.KeyA, "A"));
        Assert.True(CapsLockOn(false, PhysicalKeyboardKey.KeyA, LogicalKeyboardKey.KeyA, null));

        Assert.Equal(gtk, CapsLockOn(true, caps, capsLogical, null));
        Assert.False(CapsLockOn(false, caps, capsLogical, null));
        Assert.False(CapsLockOn(true, PhysicalKeyboardKey.KeyA, LogicalKeyboardKey.KeyA, "a"));
    }

    private static Dictionary<string, object?> Encode(
        string keymap,
        PhysicalKeyboardKey physical,
        LogicalKeyboardKey logical,
        string? keySymbol,
        KeyModifiers modifiers,
        bool isDown = true) =>
        HostKeyEventMessage.Encode(
            keymap,
            physical,
            logical,
            keySymbol,
            modifiers,
            KeyEventDeviceType.Keyboard,
            isDown);

    private static RawKeyEvent Decode(
        string keymap,
        PhysicalKeyboardKey physical,
        LogicalKeyboardKey logical,
        string? keySymbol,
        KeyModifiers modifiers,
        bool isDown = true) =>
        RawKeyEvent.FromMessage(Encode(keymap, physical, logical, keySymbol, modifiers, isDown));

    private static RawKeyEvent DecodeKeyA(string keymap, string? keySymbol, KeyModifiers modifiers) =>
        Decode(keymap, PhysicalKeyboardKey.KeyA, LogicalKeyboardKey.KeyA, keySymbol, modifiers);
}
#pragma warning restore CS0618
