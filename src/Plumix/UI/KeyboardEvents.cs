// Dart parity source (reference): flutter/packages/flutter/lib/src/services/hardware_keyboard.dart; flutter/packages/flutter/lib/src/widgets/focus_manager.dart (adapted)

namespace Plumix.UI;

public sealed class KeyEvent
{
    public KeyEvent(
        string key,
        bool isDown,
        bool isShiftPressed = false,
        bool isControlPressed = false,
        bool isAltPressed = false,
        bool isMetaPressed = false,
        DateTime? timestampUtc = null)
    {
        Key = key;
        IsDown = isDown;
        IsShiftPressed = isShiftPressed;
        IsControlPressed = isControlPressed;
        IsAltPressed = isAltPressed;
        IsMetaPressed = isMetaPressed;
        TimestampUtc = timestampUtc ?? DateTime.UtcNow;
    }

    public string Key { get; }

    public bool IsDown { get; }

    public bool IsShiftPressed { get; }

    public bool IsControlPressed { get; }

    public bool IsAltPressed { get; }

    public bool IsMetaPressed { get; }

    public DateTime TimestampUtc { get; }
}

// Dart parity source: flutter/packages/flutter/lib/src/services/hardware_keyboard.dart
public sealed class HardwareKeyboard
{
    private readonly List<Func<KeyEvent, bool>> _handlers = [];
    private readonly HashSet<string> _logicalKeysPressed = new(StringComparer.Ordinal);

    private HardwareKeyboard()
    {
    }

    public static HardwareKeyboard Instance { get; } = new();

    public IReadOnlySet<string> LogicalKeysPressed => _logicalKeysPressed;

    public bool IsAltPressed => _logicalKeysPressed.Contains("Alt")
                                || _logicalKeysPressed.Contains("LeftAlt")
                                || _logicalKeysPressed.Contains("RightAlt");

    public void AddHandler(Func<KeyEvent, bool> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (!_handlers.Contains(handler))
        {
            _handlers.Add(handler);
        }
    }

    public void RemoveHandler(Func<KeyEvent, bool> handler)
    {
        _handlers.Remove(handler);
    }

    internal bool HandleKeyEvent(KeyEvent keyEvent)
    {
        ArgumentNullException.ThrowIfNull(keyEvent);
        if (keyEvent.IsDown)
        {
            _logicalKeysPressed.Add(keyEvent.Key);
        }
        else
        {
            _logicalKeysPressed.Remove(keyEvent.Key);
        }

        bool handled = false;
        foreach (Func<KeyEvent, bool> handler in _handlers.ToArray())
        {
            handled |= handler(keyEvent);
        }

        return handled;
    }

    internal void ResetForTests()
    {
        _handlers.Clear();
        _logicalKeysPressed.Clear();
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/services/raw_keyboard.dart
[Obsolete("Use KeyEvent and KeyboardListener instead.")]
public abstract class RawKeyEvent
{
    protected RawKeyEvent(KeyEvent keyEvent)
    {
        KeyEvent = keyEvent ?? throw new ArgumentNullException(nameof(keyEvent));
    }

    public KeyEvent KeyEvent { get; }

    public string Key => KeyEvent.Key;

    public bool IsShiftPressed => KeyEvent.IsShiftPressed;

    public bool IsControlPressed => KeyEvent.IsControlPressed;

    public bool IsAltPressed => KeyEvent.IsAltPressed;

    public bool IsMetaPressed => KeyEvent.IsMetaPressed;

    public DateTime TimestampUtc => KeyEvent.TimestampUtc;

    internal static RawKeyEvent FromKeyEvent(KeyEvent keyEvent)
    {
        return keyEvent.IsDown
            ? new RawKeyDownEvent(keyEvent)
            : new RawKeyUpEvent(keyEvent);
    }
}

[Obsolete("Use KeyEvent and KeyboardListener instead.")]
public sealed class RawKeyDownEvent : RawKeyEvent
{
    public RawKeyDownEvent(KeyEvent keyEvent) : base(keyEvent)
    {
    }
}

[Obsolete("Use KeyEvent and KeyboardListener instead.")]
public sealed class RawKeyUpEvent : RawKeyEvent
{
    public RawKeyUpEvent(KeyEvent keyEvent) : base(keyEvent)
    {
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/services/raw_keyboard.dart
[Obsolete("Use the Focus/KeyboardListener key event pipeline instead.")]
public sealed class RawKeyboard
{
    private readonly List<Action<RawKeyEvent>> _listeners = [];

    private RawKeyboard()
    {
    }

    public static RawKeyboard Instance { get; } = new();

    public void AddListener(Action<RawKeyEvent> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        if (!_listeners.Contains(listener))
        {
            _listeners.Add(listener);
        }
    }

    public void RemoveListener(Action<RawKeyEvent> listener)
    {
        _listeners.Remove(listener);
    }

    internal void HandleKeyEvent(KeyEvent keyEvent)
    {
        RawKeyEvent rawEvent = RawKeyEvent.FromKeyEvent(keyEvent);
        foreach (Action<RawKeyEvent> listener in _listeners.ToArray())
        {
            listener(rawEvent);
        }
    }

    internal void ResetForTests()
    {
        _listeners.Clear();
    }
}
