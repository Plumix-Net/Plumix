// Dart parity source: flutter/packages/flutter/lib/src/services/raw_keyboard.dart

using Plumix.Foundation;

namespace Plumix.UI;

/// <summary>An enum describing the side of the keyboard that a key is on.</summary>
public enum KeyboardSide
{
    /// <summary>Matches if either the left, right or both versions of the key are pressed.</summary>
    Any,

    /// <summary>Matches the left version of the key.</summary>
    Left,

    /// <summary>Matches the right version of the key.</summary>
    Right,

    /// <summary>Matches the left and right version of the key pressed simultaneously.</summary>
    All,
}

/// <summary>An enum describing the type of modifier key that is being pressed.</summary>
public enum ModifierKey
{
    /// <summary>The CTRL modifier key.</summary>
    ControlModifier,

    /// <summary>The SHIFT modifier key.</summary>
    ShiftModifier,

    /// <summary>The ALT modifier key.</summary>
    AltModifier,

    /// <summary>The META modifier key: Windows (Win), macOS (Command), ChromeOS (Search).</summary>
    MetaModifier,

    /// <summary>The CAPS LOCK modifier key, only shown as "pressed" when the lock is on.</summary>
    CapsLockModifier,

    /// <summary>The NUM LOCK modifier key, only shown as "pressed" when the lock is on.</summary>
    NumLockModifier,

    /// <summary>The SCROLL LOCK modifier key, only shown as "pressed" when the lock is on.</summary>
    ScrollLockModifier,

    /// <summary>The FUNCTION (Fn) modifier key.</summary>
    FunctionModifier,

    /// <summary>The SYMBOL modifier key, used on some virtual keyboards.</summary>
    SymbolModifier,
}

/// <summary>
/// Base class for platform-specific key event data.
/// </summary>
[Obsolete("Use KeyEvent and HardwareKeyboard instead. Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public abstract class RawKeyEventData
{
    protected RawKeyEventData()
    {
    }

    /// <summary>Returns true if the given <see cref="ModifierKey"/> was pressed at the time of this event.</summary>
    public abstract bool IsModifierPressed(ModifierKey key, KeyboardSide side = KeyboardSide.Any);

    /// <summary>Returns the side of the given modifier key that was pressed, or null if it was not pressed.</summary>
    public abstract KeyboardSide? GetModifierSide(ModifierKey key);

    /// <summary>An object representing the physical location of this key.</summary>
    public abstract PhysicalKeyboardKey PhysicalKey { get; }

    /// <summary>An object representing the logical key that was pressed.</summary>
    public abstract LogicalKeyboardKey LogicalKey { get; }

    /// <summary>Returns the Unicode string representing the label on this key, or an empty string.</summary>
    public abstract string KeyLabel { get; }

    /// <summary>Returns true if a CTRL modifier key was pressed at the time of this event.</summary>
    public bool IsControlPressed => IsModifierPressed(ModifierKey.ControlModifier);

    /// <summary>Returns true if a SHIFT modifier key was pressed at the time of this event.</summary>
    public bool IsShiftPressed => IsModifierPressed(ModifierKey.ShiftModifier);

    /// <summary>Returns true if an ALT modifier key was pressed at the time of this event.</summary>
    public bool IsAltPressed => IsModifierPressed(ModifierKey.AltModifier);

    /// <summary>Returns true if a META modifier key was pressed at the time of this event.</summary>
    public bool IsMetaPressed => IsModifierPressed(ModifierKey.MetaModifier);

    /// <summary>Returns a map of modifier keys that were pressed at the time of this event.</summary>
    public IReadOnlyDictionary<ModifierKey, KeyboardSide> ModifiersPressed
    {
        get
        {
            var result = new Dictionary<ModifierKey, KeyboardSide>();
            foreach (ModifierKey key in Enum.GetValues<ModifierKey>())
            {
                if (!IsModifierPressed(key))
                {
                    continue;
                }

                KeyboardSide? side = GetModifierSide(key);
                if (side != null)
                {
                    result[key] = side.Value;
                }
                else
                {
                    KeyboardDebug.Log(() =>
                        "Raw key data is returning inconsistent information for pressed modifiers. "
                        + $"IsModifierPressed returns true for {key} being pressed, but when "
                        + "GetModifierSide is called, it says that no modifiers are pressed.");
                }
            }

            return result;
        }
    }

    /// <summary>
    /// The device type this event came from. Flutter derives it from the platform payload
    /// (`KeyEventManager._convertDeviceType`); the Plumix host reports it directly.
    /// </summary>
    public virtual KeyEventDeviceType DeviceType => KeyEventDeviceType.Keyboard;

    /// <summary>Whether a key down event should be dispatched. Overridden by hosts that swallow keys.</summary>
    public virtual bool ShouldDispatchEvent() => true;
}

/// <summary>
/// Key event data produced by the Plumix host adapter.
/// </summary>
/// <remarks>
/// Flutter ships one <c>RawKeyEventData</c> subclass per embedding (Android, macOS, Windows, ...),
/// each decoding that platform's native modifier bitmask. Plumix has a single host bridge that has
/// already normalized modifiers, so it has a single subclass carrying that normalized state.
/// </remarks>
[Obsolete("Use KeyEvent and HardwareKeyboard instead. Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public sealed class HostRawKeyEventData : RawKeyEventData
{
    public HostRawKeyEventData(
        PhysicalKeyboardKey physicalKey,
        LogicalKeyboardKey logicalKey,
        string keyLabel = "",
        bool isControlPressed = false,
        bool isShiftPressed = false,
        bool isAltPressed = false,
        bool isMetaPressed = false,
        bool isCapsLockOn = false,
        bool isNumLockOn = false,
        bool isScrollLockOn = false,
        KeyEventDeviceType deviceType = KeyEventDeviceType.Keyboard,
        bool shouldDispatchEvent = true)
    {
        PhysicalKey = physicalKey ?? throw new ArgumentNullException(nameof(physicalKey));
        LogicalKey = logicalKey ?? throw new ArgumentNullException(nameof(logicalKey));
        KeyLabel = keyLabel ?? throw new ArgumentNullException(nameof(keyLabel));
        IsControlDown = isControlPressed;
        IsShiftDown = isShiftPressed;
        IsAltDown = isAltPressed;
        IsMetaDown = isMetaPressed;
        IsCapsLockOn = isCapsLockOn;
        IsNumLockOn = isNumLockOn;
        IsScrollLockOn = isScrollLockOn;
        HostDeviceType = deviceType;
        HostShouldDispatchEvent = shouldDispatchEvent;
    }

    public override PhysicalKeyboardKey PhysicalKey { get; }

    public override LogicalKeyboardKey LogicalKey { get; }

    public override string KeyLabel { get; }

    public bool IsControlDown { get; }

    public bool IsShiftDown { get; }

    public bool IsAltDown { get; }

    public bool IsMetaDown { get; }

    public bool IsCapsLockOn { get; }

    public bool IsNumLockOn { get; }

    public bool IsScrollLockOn { get; }

    /// <summary>The device type reported by the host.</summary>
    public KeyEventDeviceType HostDeviceType { get; }

    /// <summary>Whether the host wants this key dispatched (false for keys the platform swallows).</summary>
    public bool HostShouldDispatchEvent { get; }

    public override KeyEventDeviceType DeviceType => HostDeviceType;

    public override bool ShouldDispatchEvent() => HostShouldDispatchEvent;

    public override bool IsModifierPressed(ModifierKey key, KeyboardSide side = KeyboardSide.Any)
    {
        bool pressed = key switch
        {
            ModifierKey.ControlModifier => IsControlDown,
            ModifierKey.ShiftModifier => IsShiftDown,
            ModifierKey.AltModifier => IsAltDown,
            ModifierKey.MetaModifier => IsMetaDown,
            ModifierKey.CapsLockModifier => IsCapsLockOn,
            ModifierKey.NumLockModifier => IsNumLockOn,
            ModifierKey.ScrollLockModifier => IsScrollLockOn,
            _ => false,
        };

        if (!pressed)
        {
            return false;
        }

        // The host reports a single modifier bit per modifier, without a side, so only sideless
        // queries and the lock modifiers' `all` can be satisfied.
        return side == KeyboardSide.Any || side == GetModifierSide(key);
    }

    public override KeyboardSide? GetModifierSide(ModifierKey key)
    {
        if (!IsModifierPressed(key))
        {
            return null;
        }

        return key switch
        {
            ModifierKey.CapsLockModifier
                or ModifierKey.NumLockModifier
                or ModifierKey.ScrollLockModifier => KeyboardSide.All,
            _ => KeyboardSide.Any,
        };
    }
}

/// <summary>Base class for raw key events.</summary>
[Obsolete("Use KeyEvent and KeyboardListener instead. Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public abstract class RawKeyEvent : Diagnosticable
{
    protected RawKeyEvent(RawKeyEventData data, string? character = null, bool repeat = false)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Character = character;
        Repeat = repeat;
    }

    /// <summary>Platform-specific information about the key event.</summary>
    public RawKeyEventData Data { get; }

    /// <summary>Returns the Unicode character (grapheme cluster) completed by this keystroke, if any.</summary>
    public string? Character { get; }

    /// <summary>Whether this is a repeated down event. Always false for <see cref="RawKeyUpEvent"/>.</summary>
    public bool Repeat { get; }

    /// <summary>Returns an object representing the physical location of this key.</summary>
    public PhysicalKeyboardKey PhysicalKey => Data.PhysicalKey;

    /// <summary>Returns an object representing the logical key that was pressed.</summary>
    public LogicalKeyboardKey LogicalKey => Data.LogicalKey;

    /// <summary>Returns true if the given <see cref="LogicalKeyboardKey"/> is pressed.</summary>
    public bool IsKeyPressed(LogicalKeyboardKey key) => RawKeyboard.Instance.KeysPressed.Contains(key);

    /// <summary>Returns true if a CTRL modifier key is pressed, regardless of which side.</summary>
    public bool IsControlPressed => IsKeyPressed(LogicalKeyboardKey.ControlLeft)
                                    || IsKeyPressed(LogicalKeyboardKey.ControlRight);

    /// <summary>Returns true if a SHIFT modifier key is pressed, regardless of which side.</summary>
    public bool IsShiftPressed => IsKeyPressed(LogicalKeyboardKey.ShiftLeft)
                                 || IsKeyPressed(LogicalKeyboardKey.ShiftRight);

    /// <summary>Returns true if an ALT modifier key is pressed, regardless of which side.</summary>
    public bool IsAltPressed => IsKeyPressed(LogicalKeyboardKey.AltLeft)
                                || IsKeyPressed(LogicalKeyboardKey.AltRight);

    /// <summary>Returns true if a META modifier key is pressed, regardless of which side.</summary>
    public bool IsMetaPressed => IsKeyPressed(LogicalKeyboardKey.MetaLeft)
                                 || IsKeyPressed(LogicalKeyboardKey.MetaRight);

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new StringProperty("logicalKey", LogicalKey.DebugName));
        properties.Add(new StringProperty("physicalKey", PhysicalKey.DebugName));
        if (this is RawKeyDownEvent)
        {
            properties.Add(new DiagnosticsProperty<bool>("repeat", Repeat));
        }
    }

    public override string ToString() =>
        $"{GetType().Name}#{Diagnostics.ShortHash(this)}({LogicalKey.DebugName}, {PhysicalKey.DebugName})";
}

/// <summary>The user has pressed a key on the keyboard.</summary>
[Obsolete("Use KeyEvent and KeyboardListener instead. Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public sealed class RawKeyDownEvent : RawKeyEvent
{
    public RawKeyDownEvent(RawKeyEventData data, string? character = null, bool repeat = false)
        : base(data, character, repeat)
    {
    }
}

/// <summary>The user has released a key on the keyboard.</summary>
[Obsolete("Use KeyEvent and KeyboardListener instead. Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public sealed class RawKeyUpEvent : RawKeyEvent
{
    public RawKeyUpEvent(RawKeyEventData data, string? character = null)
        : base(data, character, repeat: false)
    {
    }
}

/// <summary>
/// An interface for listening to raw key events.
/// </summary>
[Obsolete("Use HardwareKeyboard instead. Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public sealed class RawKeyboard
{
    private readonly Dictionary<PhysicalKeyboardKey, LogicalKeyboardKey> _keysPressed = [];
    private readonly List<Action<RawKeyEvent>> _listeners = [];

    private RawKeyboard()
    {
    }

    public static RawKeyboard Instance { get; } = new();

    /// <summary>Returns the set of logical keys that are pressed.</summary>
    public IReadOnlySet<LogicalKeyboardKey> KeysPressed => new HashSet<LogicalKeyboardKey>(_keysPressed.Values);

    /// <summary>Returns the set of physical keys that are pressed.</summary>
    public IReadOnlySet<PhysicalKeyboardKey> PhysicalKeysPressed => new HashSet<PhysicalKeyboardKey>(_keysPressed.Keys);

    /// <summary>Returns the logical key that corresponds to the given pressed physical key.</summary>
    public LogicalKeyboardKey? LookUpLayout(PhysicalKeyboardKey physicalKey)
    {
        ArgumentNullException.ThrowIfNull(physicalKey);
        return _keysPressed.GetValueOrDefault(physicalKey);
    }

    /// <summary>Register a listener that is called every time the user presses or releases a key.</summary>
    public void AddListener(Action<RawKeyEvent> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        _listeners.Add(listener);
    }

    /// <summary>Stop calling the given listener every time the user presses or releases a key.</summary>
    public void RemoveListener(Action<RawKeyEvent> listener)
    {
        _listeners.Remove(listener);
    }

    /// <summary>Records the key state and dispatches to listeners. Always returns false.</summary>
    public bool HandleRawKeyEvent(RawKeyEvent keyEvent)
    {
        ArgumentNullException.ThrowIfNull(keyEvent);
        switch (keyEvent)
        {
            case RawKeyDownEvent:
                _keysPressed[keyEvent.PhysicalKey] = keyEvent.LogicalKey;
                break;
            case RawKeyUpEvent:
                // Use the physical key in the key up event to find the physical key from the key
                // down event, even if the logical keys don't match.
                _keysPressed.Remove(keyEvent.PhysicalKey);
                break;
        }

        SynchronizeModifiers(keyEvent);

        foreach (Action<RawKeyEvent> listener in _listeners.ToArray())
        {
            if (!_listeners.Contains(listener))
            {
                continue;
            }

            try
            {
                listener(keyEvent);
            }
            catch (Exception exception)
            {
                KeyboardDebug.ReportError(exception, "while processing a raw key listener");
            }
        }

        return false;
    }

    /// <summary>Clears the list of keys returned from <see cref="KeysPressed"/>. Test-only.</summary>
    internal void ClearKeysPressed() => _keysPressed.Clear();

    internal void ClearListeners() => _listeners.Clear();

    /// <summary>
    /// Synchronizes the key state with the modifier flags carried by the event, so that pressing a
    /// key while a modifier was already held still records that modifier as pressed.
    /// </summary>
    private void SynchronizeModifiers(RawKeyEvent keyEvent)
    {
        IReadOnlyDictionary<ModifierKey, KeyboardSide> modifiersPressed = keyEvent.Data.ModifiersPressed;
        var modifierKeys = new Dictionary<PhysicalKeyboardKey, LogicalKeyboardKey>();
        var anySideKeys = new HashSet<PhysicalKeyboardKey>();
        var keysPressedAfterEvent = new HashSet<PhysicalKeyboardKey>(_keysPressed.Keys);
        if (keyEvent is RawKeyDownEvent)
        {
            keysPressedAfterEvent.Add(keyEvent.PhysicalKey);
        }

        ModifierKey? thisKeyModifier = null;
        foreach (ModifierKey key in Enum.GetValues<ModifierKey>())
        {
            IReadOnlySet<PhysicalKeyboardKey>? thisModifierKeys =
                ModifierKeyMap.GetValueOrDefault((key, KeyboardSide.All));
            if (thisModifierKeys == null)
            {
                continue;
            }

            if (thisModifierKeys.Contains(keyEvent.PhysicalKey))
            {
                thisKeyModifier = key;
            }

            if (modifiersPressed.TryGetValue(key, out KeyboardSide anySide) && anySide == KeyboardSide.Any)
            {
                anySideKeys.UnionWith(thisModifierKeys);
                if (thisModifierKeys.Any(keysPressedAfterEvent.Contains))
                {
                    continue;
                }
            }

            IReadOnlySet<PhysicalKeyboardKey>? mappedKeys = !modifiersPressed.TryGetValue(
                key,
                out KeyboardSide side)
                ? new HashSet<PhysicalKeyboardKey>()
                : ModifierKeyMap.GetValueOrDefault((key, side));
            if (mappedKeys == null)
            {
                KeyboardDebug.Log(() =>
                    $"Host key support is producing unsupported modifier combinations for modifier {key} "
                    + $"on side {side}.");
                continue;
            }

            foreach (PhysicalKeyboardKey physicalModifier in mappedKeys)
            {
                modifierKeys[physicalModifier] = AllModifiers[physicalModifier];
            }
        }

        foreach (PhysicalKeyboardKey physicalKey in AllModifiersExceptFn.Keys)
        {
            if (!anySideKeys.Contains(physicalKey))
            {
                _keysPressed.Remove(physicalKey);
            }
        }

        if (PlatformDefaults.TargetPlatform is not (TargetPlatform.Fuchsia or TargetPlatform.MacOS))
        {
            // On Fuchsia and macOS, the Fn key is not considered a modifier key.
            _keysPressed.Remove(PhysicalKeyboardKey.Fn);
        }

        foreach ((PhysicalKeyboardKey physicalKey, LogicalKeyboardKey logicalKey) in modifierKeys)
        {
            _keysPressed[physicalKey] = logicalKey;
        }

        // A modifier key down whose own modifier flag was not reported still has to land in the
        // pressed state, or the key would be missing from `keysPressed` for its whole tap sequence.
        if (keyEvent is RawKeyDownEvent
            && thisKeyModifier != null
            && !_keysPressed.ContainsKey(keyEvent.PhysicalKey)
            && AllModifiersExceptFn.TryGetValue(keyEvent.PhysicalKey, out LogicalKeyboardKey? recovered))
        {
            _keysPressed[keyEvent.PhysicalKey] = recovered;
        }
    }

    private static readonly Dictionary<(ModifierKey Modifier, KeyboardSide Side),
        IReadOnlySet<PhysicalKeyboardKey>> ModifierKeyMap = BuildModifierKeyMap();

    private static readonly Dictionary<PhysicalKeyboardKey, LogicalKeyboardKey> AllModifiersExceptFn = new()
    {
        [PhysicalKeyboardKey.AltLeft] = LogicalKeyboardKey.AltLeft,
        [PhysicalKeyboardKey.AltRight] = LogicalKeyboardKey.AltRight,
        [PhysicalKeyboardKey.ShiftLeft] = LogicalKeyboardKey.ShiftLeft,
        [PhysicalKeyboardKey.ShiftRight] = LogicalKeyboardKey.ShiftRight,
        [PhysicalKeyboardKey.ControlLeft] = LogicalKeyboardKey.ControlLeft,
        [PhysicalKeyboardKey.ControlRight] = LogicalKeyboardKey.ControlRight,
        [PhysicalKeyboardKey.MetaLeft] = LogicalKeyboardKey.MetaLeft,
        [PhysicalKeyboardKey.MetaRight] = LogicalKeyboardKey.MetaRight,
        [PhysicalKeyboardKey.CapsLock] = LogicalKeyboardKey.CapsLock,
        [PhysicalKeyboardKey.NumLock] = LogicalKeyboardKey.NumLock,
        [PhysicalKeyboardKey.ScrollLock] = LogicalKeyboardKey.ScrollLock,
    };

    private static readonly Dictionary<PhysicalKeyboardKey, LogicalKeyboardKey> AllModifiers =
        new(AllModifiersExceptFn) { [PhysicalKeyboardKey.Fn] = LogicalKeyboardKey.Fn };

    private static Dictionary<(ModifierKey, KeyboardSide), IReadOnlySet<PhysicalKeyboardKey>> BuildModifierKeyMap()
    {
        var map = new Dictionary<(ModifierKey, KeyboardSide), IReadOnlySet<PhysicalKeyboardKey>>();

        void AddSided(ModifierKey modifier, PhysicalKeyboardKey left, PhysicalKeyboardKey right)
        {
            map[(modifier, KeyboardSide.Left)] = new HashSet<PhysicalKeyboardKey> { left };
            map[(modifier, KeyboardSide.Right)] = new HashSet<PhysicalKeyboardKey> { right };
            map[(modifier, KeyboardSide.All)] = new HashSet<PhysicalKeyboardKey> { left, right };
            map[(modifier, KeyboardSide.Any)] = new HashSet<PhysicalKeyboardKey> { left };
        }

        AddSided(ModifierKey.AltModifier, PhysicalKeyboardKey.AltLeft, PhysicalKeyboardKey.AltRight);
        AddSided(ModifierKey.ShiftModifier, PhysicalKeyboardKey.ShiftLeft, PhysicalKeyboardKey.ShiftRight);
        AddSided(ModifierKey.ControlModifier, PhysicalKeyboardKey.ControlLeft, PhysicalKeyboardKey.ControlRight);
        AddSided(ModifierKey.MetaModifier, PhysicalKeyboardKey.MetaLeft, PhysicalKeyboardKey.MetaRight);
        map[(ModifierKey.CapsLockModifier, KeyboardSide.All)] =
            new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.CapsLock };
        map[(ModifierKey.NumLockModifier, KeyboardSide.All)] =
            new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.NumLock };
        map[(ModifierKey.ScrollLockModifier, KeyboardSide.All)] =
            new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.ScrollLock };
        map[(ModifierKey.FunctionModifier, KeyboardSide.All)] =
            new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.Fn };
        // `symbolModifier` is deliberately absent: it has no key representation on any platform.
        return map;
    }
}
