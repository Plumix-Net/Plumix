using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Tests;

// C#-only test infrastructure: shortcuts over flutter_test's event_simulation.dart, whose faithful
// port is KeyEventSimulator.cs.
//
// `Down`/`Repeat`/`Up` build a `KeyEvent` for `FocusManager.HandleKeyEvent` after pushing the
// requested modifier state into `HardwareKeyboard` directly (as synthesized events, as the raw-event
// converter would). `DispatchRaw` instead sends a real platform message through
// `KeyEventSimulator` in `rawKeyData` mode.
internal static class KeySim
{
    public static KeyDownEvent Down(
        LogicalKeyboardKey key,
        bool control = false,
        bool shift = false,
        bool alt = false,
        bool meta = false,
        string? character = null,
        bool numLock = false)
    {
        SyncModifiers(control, shift, alt, meta);
        SyncNumLock(numLock);
        return new KeyDownEvent(PhysicalFor(key), key, character: character);
    }

    public static KeyRepeatEvent Repeat(
        LogicalKeyboardKey key,
        bool control = false,
        bool shift = false,
        bool alt = false,
        bool meta = false,
        string? character = null)
    {
        SyncModifiers(control, shift, alt, meta);
        return new KeyRepeatEvent(PhysicalFor(key), key, character: character);
    }

    public static KeyUpEvent Up(
        LogicalKeyboardKey key,
        bool control = false,
        bool shift = false,
        bool alt = false,
        bool meta = false)
    {
        SyncModifiers(control, shift, alt, meta);
        return new KeyUpEvent(PhysicalFor(key), key);
    }

    /// <summary>
    /// Runs a raw platform key message through the full <c>KeyEventManager</c> pipeline in
    /// <c>rawKeyData</c> mode (<see cref="KeyEventSimulator"/>): raw state and raw listeners first,
    /// then the regularized <see cref="KeyEvent"/> stream. The requested modifiers are pressed (or
    /// released) with their own left-side messages first, the way a user holds them.
    /// </summary>
    public static bool DispatchRaw(
        LogicalKeyboardKey key,
        bool down,
        bool control = false,
        bool shift = false,
        bool alt = false,
        bool meta = false,
        string? character = null)
    {
#pragma warning disable CS0618
        KeyDataTransitMode? previous = KeyboardDebug.DebugKeyEventSimulatorTransitModeOverride;
        KeyboardDebug.DebugKeyEventSimulatorTransitModeOverride = KeyDataTransitMode.RawKeyData;
        try
        {
            SyncRawModifier(control, LogicalKeyboardKey.ControlLeft, LogicalKeyboardKey.ControlRight, key);
            SyncRawModifier(shift, LogicalKeyboardKey.ShiftLeft, LogicalKeyboardKey.ShiftRight, key);
            SyncRawModifier(alt, LogicalKeyboardKey.AltLeft, LogicalKeyboardKey.AltRight, key);
            SyncRawModifier(meta, LogicalKeyboardKey.MetaLeft, LogicalKeyboardKey.MetaRight, key);
            return down
                ? KeyEventSimulator.SimulateKeyDownEvent(key, character: character)
                : KeyEventSimulator.SimulateKeyUpEvent(key);
        }
        finally
        {
            KeyboardDebug.DebugKeyEventSimulatorTransitModeOverride = previous;
        }
    }

    private static void SyncRawModifier(
        bool pressed,
        LogicalKeyboardKey left,
        LogicalKeyboardKey right,
        LogicalKeyboardKey eventKey)
    {
        if (eventKey.Equals(left) || eventKey.Equals(right))
        {
            return;
        }

        IReadOnlySet<LogicalKeyboardKey> keysPressed = RawKeyboard.Instance.KeysPressed;
        bool isPressed = keysPressed.Contains(left) || keysPressed.Contains(right);
        if (pressed && !isPressed)
        {
            KeyEventSimulator.SimulateKeyDownEvent(left);
        }
        else if (!pressed && isPressed)
        {
            if (keysPressed.Contains(left))
            {
                KeyEventSimulator.SimulateKeyUpEvent(left);
            }

            if (keysPressed.Contains(right))
            {
                KeyEventSimulator.SimulateKeyUpEvent(right);
            }
        }
#pragma warning restore CS0618
    }

    /// <summary>
    /// The physical key a simulated event carries. Only its identity matters to the tests, and the
    /// modifiers use the real constants so that <c>_synchronizeModifiers</c> sees the same keys.
    /// </summary>
    public static PhysicalKeyboardKey PhysicalFor(LogicalKeyboardKey key)
    {
        if (ModifierPhysicalKeys.TryGetValue(key, out PhysicalKeyboardKey? modifier))
        {
            return modifier;
        }

        // `LogicalKeyboardKey.KeyA.DebugName` is "Key A" and the physical constant is `KeyA`, so
        // most keys bridge by squeezing the spaces out of the debug name.
        string name = key.DebugName.Replace(" ", string.Empty, StringComparison.Ordinal);
        return PhysicalKeyboardKey.FindKeyByGeneratedName(name) ?? new PhysicalKeyboardKey(key.KeyId);
    }

    /// <summary>
    /// flutter_test's <c>sendKeyCombination</c>: presses the key with the given modifiers held and
    /// releases everything again. A key down the framework leaves unhandled goes on to the test text
    /// input, the way <c>simulateKeyDownEvent</c> hands it to <c>TestTextInput.handleKeyDownEvent</c>:
    /// on macOS, while a client is attached, its AppKit selectors reach the framework as
    /// <c>TextInputClient.performSelectors</c> (<c>MacOSTestTextInputKeyHandler</c>).
    /// </summary>
    public static bool SendKeyCombination(
        LogicalKeyboardKey key,
        bool control = false,
        bool shift = false,
        bool alt = false,
        bool meta = false)
    {
        bool handled = FocusManager.Instance.HandleKeyEvent(Down(key, control, shift, alt, meta));
        if (!handled
            && !PlatformDefaults.IsWeb
            && PlatformDefaults.TargetPlatform == TargetPlatform.MacOS
            && Plumix.UI.TextInput.CurrentConnection is { } connection
            && MacOSStandardKeyBindings.SelectorsFor(key, control, shift, alt, meta) is { } selectors)
        {
            MacOSTestTextInputKeyHandler.SendSelectors(connection.Id, selectors);
        }

        _ = FocusManager.Instance.HandleKeyEvent(Up(key, control, shift, alt, meta));
        SyncModifiers(control: false, shift: false, alt: false, meta: false);
        return handled;
    }

    private static void SyncModifiers(bool control, bool shift, bool alt, bool meta)
    {
        SyncModifier(control, LogicalKeyboardKey.ControlLeft, LogicalKeyboardKey.ControlRight);
        SyncModifier(shift, LogicalKeyboardKey.ShiftLeft, LogicalKeyboardKey.ShiftRight);
        SyncModifier(alt, LogicalKeyboardKey.AltLeft, LogicalKeyboardKey.AltRight);
        SyncModifier(meta, LogicalKeyboardKey.MetaLeft, LogicalKeyboardKey.MetaRight);
    }

    private static void SyncModifier(bool pressed, LogicalKeyboardKey left, LogicalKeyboardKey right)
    {
        HardwareKeyboard keyboard = HardwareKeyboard.Instance;
        bool leftPressed = keyboard.IsLogicalKeyPressed(left);
        bool rightPressed = keyboard.IsLogicalKeyPressed(right);
        if (pressed)
        {
            if (!leftPressed && !rightPressed)
            {
                keyboard.HandleKeyEvent(new KeyDownEvent(PhysicalFor(left), left, synthesized: true));
            }

            return;
        }

        if (leftPressed)
        {
            keyboard.HandleKeyEvent(new KeyUpEvent(PhysicalFor(left), left, synthesized: true));
        }

        if (rightPressed)
        {
            keyboard.HandleKeyEvent(new KeyUpEvent(PhysicalFor(right), right, synthesized: true));
        }
    }

    private static void SyncNumLock(bool locked)
    {
        HardwareKeyboard keyboard = HardwareKeyboard.Instance;
        if (keyboard.LockModesEnabled.Contains(KeyboardLockMode.NumLock) == locked)
        {
            return;
        }

        // A lock mode is toggled by a key down of its logical key, so a full tap flips it.
        keyboard.HandleKeyEvent(new KeyDownEvent(
            PhysicalKeyboardKey.NumLock,
            LogicalKeyboardKey.NumLock,
            synthesized: true));
        keyboard.HandleKeyEvent(new KeyUpEvent(
            PhysicalKeyboardKey.NumLock,
            LogicalKeyboardKey.NumLock,
            synthesized: true));
    }

    private static readonly Dictionary<LogicalKeyboardKey, PhysicalKeyboardKey> ModifierPhysicalKeys = new()
    {
        [LogicalKeyboardKey.ControlLeft] = PhysicalKeyboardKey.ControlLeft,
        [LogicalKeyboardKey.ControlRight] = PhysicalKeyboardKey.ControlRight,
        [LogicalKeyboardKey.ShiftLeft] = PhysicalKeyboardKey.ShiftLeft,
        [LogicalKeyboardKey.ShiftRight] = PhysicalKeyboardKey.ShiftRight,
        [LogicalKeyboardKey.AltLeft] = PhysicalKeyboardKey.AltLeft,
        [LogicalKeyboardKey.AltRight] = PhysicalKeyboardKey.AltRight,
        [LogicalKeyboardKey.MetaLeft] = PhysicalKeyboardKey.MetaLeft,
        [LogicalKeyboardKey.MetaRight] = PhysicalKeyboardKey.MetaRight,
        [LogicalKeyboardKey.NumLock] = PhysicalKeyboardKey.NumLock,
        [LogicalKeyboardKey.CapsLock] = PhysicalKeyboardKey.CapsLock,
        [LogicalKeyboardKey.ScrollLock] = PhysicalKeyboardKey.ScrollLock,
    };
}
