// Dart parity source: flutter/packages/flutter/lib/src/services/hardware_keyboard.dart

using Plumix.Foundation;

namespace Plumix.UI;

/// <summary>
/// The type of device that a key event originated from.
/// </summary>
/// <remarks>Dart parity source: flutter/lib/ui/key.dart (`ui.KeyEventDeviceType`).</remarks>
public enum KeyEventDeviceType
{
    /// <summary>The device is a keyboard.</summary>
    Keyboard,

    /// <summary>The device is a directional pad on something like a television remote control or similar.</summary>
    DirectionalPad,

    /// <summary>The device is a gamepad button.</summary>
    Gamepad,

    /// <summary>The device is a joystick button.</summary>
    Joystick,

    /// <summary>The device is a device connected to an HDMI bus.</summary>
    Hdmi,
}

/// <summary>
/// Represents a lock mode of a keyboard, such as <see cref="NumLock"/>.
/// </summary>
public sealed class KeyboardLockMode : IEquatable<KeyboardLockMode>
{
    private KeyboardLockMode(LogicalKeyboardKey logicalKey)
    {
        LogicalKey = logicalKey;
    }

    /// <summary>The logical key that triggers this lock mode.</summary>
    public LogicalKeyboardKey LogicalKey { get; }

    /// <summary>
    /// Enabling number lock mode usually allows key presses of the number pad to input numbers,
    /// instead of acting as up, down, left, right, page up, end, etc.
    /// </summary>
    public static KeyboardLockMode NumLock { get; } = new(LogicalKeyboardKey.NumLock);

    /// <summary>
    /// Enabling scrolling lock mode usually allows key presses of the cursor keys to scroll the
    /// document instead of the cursor.
    /// </summary>
    public static KeyboardLockMode ScrollLock { get; } = new(LogicalKeyboardKey.ScrollLock);

    /// <summary>
    /// Enabling capital lock mode allows key presses of the letter keys to input uppercase letters
    /// instead of lowercase.
    /// </summary>
    public static KeyboardLockMode CapsLock { get; } = new(LogicalKeyboardKey.CapsLock);

    /// <summary>Returns the <see cref="KeyboardLockMode"/> constant from the logical key, or null.</summary>
    public static KeyboardLockMode? FindLockByLogicalKey(LogicalKeyboardKey logicalKey)
    {
        ArgumentNullException.ThrowIfNull(logicalKey);
        return KnownLockModes.GetValueOrDefault(logicalKey.KeyId);
    }

    public bool Equals(KeyboardLockMode? other) => ReferenceEquals(this, other);

    public override bool Equals(object? obj) => ReferenceEquals(this, obj);

    public override int GetHashCode() => LogicalKey.KeyId.GetHashCode();

    public override string ToString() => $"KeyboardLockMode.{LogicalKey.DebugName}";

    private static readonly Dictionary<long, KeyboardLockMode> KnownLockModes = new()
    {
        [NumLock.LogicalKey.KeyId] = NumLock,
        [ScrollLock.LogicalKey.KeyId] = ScrollLock,
        [CapsLock.LogicalKey.KeyId] = CapsLock,
    };
}

/// <summary>
/// Defines the interface for keyboard key events.
/// </summary>
public abstract class KeyEvent
{
    protected KeyEvent(
        PhysicalKeyboardKey physicalKey,
        LogicalKeyboardKey logicalKey,
        TimeSpan timeStamp,
        string? character = null,
        KeyEventDeviceType deviceType = KeyEventDeviceType.Keyboard,
        bool synthesized = false)
    {
        PhysicalKey = physicalKey ?? throw new ArgumentNullException(nameof(physicalKey));
        LogicalKey = logicalKey ?? throw new ArgumentNullException(nameof(logicalKey));
        TimeStamp = timeStamp;
        Character = character;
        DeviceType = deviceType;
        Synthesized = synthesized;
    }

    /// <summary>Returns an object representing the physical location of this key.</summary>
    public PhysicalKeyboardKey PhysicalKey { get; }

    /// <summary>Returns an object representing the logical key that was pressed.</summary>
    public LogicalKeyboardKey LogicalKey { get; }

    /// <summary>
    /// Returns the Unicode character (grapheme cluster) completed by this keystroke, if any.
    /// Always null for <see cref="KeyUpEvent"/>.
    /// </summary>
    public string? Character { get; }

    /// <summary>Time of event, relative to an arbitrary start point.</summary>
    public TimeSpan TimeStamp { get; }

    /// <summary>The source device type of the key event.</summary>
    public KeyEventDeviceType DeviceType { get; }

    /// <summary>Whether this event is synthesized by Plumix to synchronize key states.</summary>
    public bool Synthesized { get; }

    public virtual void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Add(new StringProperty("physicalKey", PhysicalKey.DebugName));
        properties.Add(new StringProperty("logicalKey", LogicalKey.DebugName));
        properties.Add(new StringProperty("character", Character));
        properties.Add(new DiagnosticsProperty<TimeSpan>("timeStamp", TimeStamp));
        properties.Add(new FlagProperty("synthesized", Synthesized, ifTrue: "synthesized"));
    }

    public override string ToString()
    {
        string synthesized = Synthesized ? ", synthesized" : string.Empty;
        return $"{GetType().Name}#{Diagnostics.ShortHash(this)}"
               + $"({PhysicalKey.DebugName}, {LogicalKey.DebugName}, {Character ?? "null"}{synthesized})";
    }
}

/// <summary>An event indicating that the user has pressed a key down on the keyboard.</summary>
public sealed class KeyDownEvent : KeyEvent
{
    public KeyDownEvent(
        PhysicalKeyboardKey physicalKey,
        LogicalKeyboardKey logicalKey,
        TimeSpan timeStamp = default,
        string? character = null,
        KeyEventDeviceType deviceType = KeyEventDeviceType.Keyboard,
        bool synthesized = false)
        : base(physicalKey, logicalKey, timeStamp, character, deviceType, synthesized)
    {
    }
}

/// <summary>An event indicating that the user has released a key on the keyboard.</summary>
public sealed class KeyUpEvent : KeyEvent
{
    // Dart does not expose `character` on this subclass at all, which is how it guarantees that a
    // key up never carries one; the C# constructor drops the parameter for the same reason.
    public KeyUpEvent(
        PhysicalKeyboardKey physicalKey,
        LogicalKeyboardKey logicalKey,
        TimeSpan timeStamp = default,
        KeyEventDeviceType deviceType = KeyEventDeviceType.Keyboard,
        bool synthesized = false)
        : base(physicalKey, logicalKey, timeStamp, character: null, deviceType, synthesized)
    {
    }
}

/// <summary>An event indicating that the user has been holding a key and causing repeated events.</summary>
public sealed class KeyRepeatEvent : KeyEvent
{
    // Dart omits `synthesized` here: a repeat event is never synthesized.
    public KeyRepeatEvent(
        PhysicalKeyboardKey physicalKey,
        LogicalKeyboardKey logicalKey,
        TimeSpan timeStamp = default,
        string? character = null,
        KeyEventDeviceType deviceType = KeyEventDeviceType.Keyboard)
        : base(physicalKey, logicalKey, timeStamp, character, deviceType, synthesized: false)
    {
    }
}

/// <summary>Dart's `KeyEventCallback`: receives a key event and reports whether it handled it.</summary>
public delegate bool KeyEventCallback(KeyEvent keyEvent);

/// <summary>
/// Plumix stand-in for Flutter's `debugPrintKeyboardEvents` flag plus `FlutterError.reportError`.
/// </summary>
/// <remarks>
/// Dart parity source: flutter/packages/flutter/lib/src/services/debug.dart. Plumix has no
/// `FlutterError` reporter or `debugPrint`, so both are exposed as replaceable hooks.
/// </remarks>
public static class KeyboardDebug
{
    /// <summary>Setting to true will cause extensive logging to occur when key events are received.</summary>
    public static bool DebugPrintKeyboardEvents { get; set; }

    /// <summary>Receives the keyboard debug log lines. Defaults to <see cref="Console.WriteLine(string)"/>.</summary>
    public static Action<string> DebugPrint { get; set; } = Console.WriteLine;

    /// <summary>
    /// Receives exceptions thrown by key handlers and raw key listeners, with the Dart
    /// `ErrorDescription` context string. Flutter routes these through `FlutterError.reportError`.
    /// </summary>
    public static Action<Exception, string>? OnError { get; set; }

    internal static void Log(Func<string> messageFunc, Func<IEnumerable<string>>? detailsFunc = null)
    {
        if (!DebugPrintKeyboardEvents)
        {
            return;
        }

        DebugPrint($"KEYBOARD: {messageFunc()}");
        if (detailsFunc == null)
        {
            return;
        }

        foreach (string detail in detailsFunc())
        {
            DebugPrint($"    {detail}");
        }
    }

    internal static void ReportError(Exception exception, string context)
    {
        OnError?.Invoke(exception, context);
    }
}

/// <summary>
/// Manages key events from hardware keyboards.
/// </summary>
/// <remarks>
/// The key event stream consists of key tap sequences: one <see cref="KeyDownEvent"/>, zero or more
/// <see cref="KeyRepeatEvent"/>s, and one <see cref="KeyUpEvent"/> in order, all with the same
/// physical and logical key. Desynchronization is repaired with <see cref="KeyEvent.Synthesized"/>
/// events.
/// </remarks>
public sealed class HardwareKeyboard
{
    private readonly Dictionary<PhysicalKeyboardKey, LogicalKeyboardKey> _pressedKeys = [];
    private readonly HashSet<KeyboardLockMode> _lockModes = [];
    private List<KeyEventCallback> _handlers = [];
    private List<KeyEventCallback>? _modifiedHandlers;
    private bool _duringDispatch;

    private HardwareKeyboard()
    {
    }

    public static HardwareKeyboard Instance { get; } = new();

    /// <summary>The set of physical keys currently pressed.</summary>
    public IReadOnlySet<PhysicalKeyboardKey> PhysicalKeysPressed => new HashSet<PhysicalKeyboardKey>(_pressedKeys.Keys);

    /// <summary>The set of logical keys currently pressed.</summary>
    public IReadOnlySet<LogicalKeyboardKey> LogicalKeysPressed => new HashSet<LogicalKeyboardKey>(_pressedKeys.Values);

    /// <summary>Returns the logical key that corresponds to the given pressed physical key.</summary>
    public LogicalKeyboardKey? LookUpLayout(PhysicalKeyboardKey physicalKey)
    {
        ArgumentNullException.ThrowIfNull(physicalKey);
        return _pressedKeys.GetValueOrDefault(physicalKey);
    }

    /// <summary>The set of locking modes enabled.</summary>
    public IReadOnlySet<KeyboardLockMode> LockModesEnabled => _lockModes;

    /// <summary>Returns true if the given <see cref="LogicalKeyboardKey"/> is pressed.</summary>
    public bool IsLogicalKeyPressed(LogicalKeyboardKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _pressedKeys.ContainsValue(key);
    }

    /// <summary>Returns true if the given <see cref="PhysicalKeyboardKey"/> is pressed.</summary>
    public bool IsPhysicalKeyPressed(PhysicalKeyboardKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _pressedKeys.ContainsKey(key);
    }

    /// <summary>Returns true if a logical CTRL modifier key is pressed, regardless of which side.</summary>
    public bool IsControlPressed => IsLogicalKeyPressed(LogicalKeyboardKey.ControlLeft)
                                    || IsLogicalKeyPressed(LogicalKeyboardKey.ControlRight);

    /// <summary>Returns true if a logical SHIFT modifier key is pressed, regardless of which side.</summary>
    public bool IsShiftPressed => IsLogicalKeyPressed(LogicalKeyboardKey.ShiftLeft)
                                 || IsLogicalKeyPressed(LogicalKeyboardKey.ShiftRight);

    /// <summary>Returns true if a logical ALT modifier key is pressed, regardless of which side.</summary>
    public bool IsAltPressed => IsLogicalKeyPressed(LogicalKeyboardKey.AltLeft)
                                || IsLogicalKeyPressed(LogicalKeyboardKey.AltRight);

    /// <summary>Returns true if a logical META modifier key is pressed, regardless of which side.</summary>
    public bool IsMetaPressed => IsLogicalKeyPressed(LogicalKeyboardKey.MetaLeft)
                                 || IsLogicalKeyPressed(LogicalKeyboardKey.MetaRight);

    /// <summary>Register a listener that is called every time a hardware key event occurs.</summary>
    public void AddHandler(KeyEventCallback handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (_duringDispatch)
        {
            _modifiedHandlers ??= [.. _handlers];
            _modifiedHandlers.Add(handler);
        }
        else
        {
            _handlers.Add(handler);
        }
    }

    /// <summary>Stop calling the given listener every time a hardware key event occurs.</summary>
    public void RemoveHandler(KeyEventCallback handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (_duringDispatch)
        {
            _modifiedHandlers ??= [.. _handlers];
            _modifiedHandlers.Remove(handler);
        }
        else
        {
            _handlers.Remove(handler);
        }
    }

    /// <summary>
    /// Query the engine and update key states, so that they are accurate. Flutter asks the platform
    /// over the `flutter/keyboard` channel; Plumix has no channels, so the host supplies the map of
    /// pressed physical usage codes to logical key ids directly.
    /// </summary>
    public void SyncKeyboardState(IReadOnlyDictionary<long, long>? keyboardState)
    {
        if (keyboardState == null)
        {
            return;
        }

        foreach ((long physical, long logical) in keyboardState)
        {
            _pressedKeys[new PhysicalKeyboardKey(physical)] = new LogicalKeyboardKey(logical);
        }
    }

    /// <summary>
    /// Process a new <see cref="KeyEvent"/> by recording the state changes and dispatching to handlers.
    /// </summary>
    public bool HandleKeyEvent(KeyEvent keyEvent)
    {
        ArgumentNullException.ThrowIfNull(keyEvent);
        KeyboardDebug.Log(() => $"Key event received: {keyEvent}");
        KeyboardDebug.Log(() => "Pressed state before processing the event:", DebugPressedKeysDetails);
        LogEventIfIrregular(keyEvent);

        switch (keyEvent)
        {
            case KeyDownEvent:
                _pressedKeys[keyEvent.PhysicalKey] = keyEvent.LogicalKey;
                KeyboardLockMode? lockMode = KeyboardLockMode.FindLockByLogicalKey(keyEvent.LogicalKey);
                if (lockMode != null)
                {
                    if (!_lockModes.Remove(lockMode))
                    {
                        _lockModes.Add(lockMode);
                    }
                }

                break;
            case KeyUpEvent:
                _pressedKeys.Remove(keyEvent.PhysicalKey);
                break;
            case KeyRepeatEvent:
                // Update the logical key in case it has changed.
                _pressedKeys[keyEvent.PhysicalKey] = keyEvent.LogicalKey;
                break;
        }

        KeyboardDebug.Log(() => "Pressed state after processing the event:", DebugPressedKeysDetails);
        return DispatchKeyEvent(keyEvent);
    }

    /// <summary>Clears all pressed keys, lock modes and handlers. Test-only.</summary>
    internal void ClearState()
    {
        _pressedKeys.Clear();
        _lockModes.Clear();
        _handlers.Clear();
        _modifiedHandlers = null;
    }

    private IEnumerable<string> DebugPressedKeysDetails()
    {
        if (_pressedKeys.Count == 0)
        {
            return ["Empty"];
        }

        return _pressedKeys.Select(entry => $"{entry.Key.DebugName}: {entry.Value.DebugName}");
    }

    private void LogEventIfIrregular(KeyEvent keyEvent)
    {
        const string common = "This is typically either due to "
                              + "https://github.com/flutter/flutter/issues/125975, or a bug in the "
                              + "host's key event conciliation logic.";
        switch (keyEvent)
        {
            case KeyDownEvent when _pressedKeys.ContainsKey(keyEvent.PhysicalKey):
                KeyboardDebug.Log(() =>
                    $"ERROR: Received unexpected {keyEvent.GetType().Name} for key that is already pressed.\n"
                    + $"{common}\n    Event: {keyEvent}\n"
                    + $"    Pressed logical key: {_pressedKeys[keyEvent.PhysicalKey].DebugName}");
                break;
            case KeyRepeatEvent or KeyUpEvent when !_pressedKeys.ContainsKey(keyEvent.PhysicalKey):
                KeyboardDebug.Log(() =>
                    $"ERROR: Received unexpected {keyEvent.GetType().Name} for key that is not pressed:\n"
                    + $"{common}\n    Event: {keyEvent}");
                break;
            case KeyRepeatEvent or KeyUpEvent
                when !_pressedKeys[keyEvent.PhysicalKey].Equals(keyEvent.LogicalKey):
                KeyboardDebug.Log(() =>
                    $"ERROR: Received unexpected {keyEvent.GetType().Name} for key with mismatched logical key:\n"
                    + $"{common}\n    Event: {keyEvent}\n"
                    + $"    Pressed logical key: {_pressedKeys[keyEvent.PhysicalKey].DebugName}");
                break;
        }
    }

    private bool DispatchKeyEvent(KeyEvent keyEvent)
    {
        _duringDispatch = true;
        bool handled = false;
        foreach (KeyEventCallback handler in _handlers)
        {
            try
            {
                bool thisResult = handler(keyEvent);
                handled = handled || thisResult;
            }
            catch (Exception exception)
            {
                KeyboardDebug.ReportError(exception, "while processing a key handler");
            }
        }

        _duringDispatch = false;
        if (_modifiedHandlers != null)
        {
            _handlers = _modifiedHandlers;
            _modifiedHandlers = null;
        }

        return handled;
    }
}
