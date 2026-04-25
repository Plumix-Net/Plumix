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
