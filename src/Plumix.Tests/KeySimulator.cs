using Plumix.UI;

namespace Plumix.Tests;

// C#-only test infrastructure, standing in for Flutter's
// flutter_test/lib/src/event_simulation.dart (`KeyEventSimulator`, `simulateKeyDownEvent`).
//
// Flutter's simulator drives a real platform message with a modifier bitmask, which makes
// `HardwareKeyboard` record the modifiers as pressed keys before the event is dispatched. Plumix has
// no message channel, so this helper pushes the same modifier state into `HardwareKeyboard`
// directly (as synthesized events, exactly as the raw-event converter would) and then returns the
// event to dispatch.
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
    /// Runs a host key event through the full <see cref="KeyEventManager"/> pipeline: raw state and
    /// raw listeners first, then the regularized <see cref="KeyEvent"/> stream.
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
        var data = new HostRawKeyEventData(
            PhysicalFor(key),
            key,
            keyLabel: key.KeyLabel,
            isControlPressed: control,
            isShiftPressed: shift,
            isAltPressed: alt,
            isMetaPressed: meta);
        RawKeyEvent rawEvent = down
            ? new RawKeyDownEvent(
                data,
                character: character,
                repeat: RawKeyboard.Instance.PhysicalKeysPressed.Contains(PhysicalFor(key)))
            : new RawKeyUpEvent(data);
#pragma warning restore CS0618
        return KeyEventManager.Instance.HandleRawKeyEvent(rawEvent);
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
