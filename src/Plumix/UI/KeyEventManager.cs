// Dart parity source: flutter/packages/flutter/lib/src/services/hardware_keyboard.dart

namespace Plumix.UI;

/// <summary>
/// The assembled information corresponding to a native key event.
/// </summary>
[Obsolete("Once RawKeyEvent is removed this is no longer needed. "
          + "Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public sealed class KeyMessage
{
    public KeyMessage(IReadOnlyList<KeyEvent> events, RawKeyEvent? rawEvent)
    {
        Events = events ?? throw new ArgumentNullException(nameof(events));
        RawEvent = rawEvent;
    }

    /// <summary>The `KeyEvent`s converted from the native key event, in order.</summary>
    public IReadOnlyList<KeyEvent> Events { get; }

    /// <summary>The native key event, or null for solitary synthesized events.</summary>
    public RawKeyEvent? RawEvent { get; }

    public override string ToString() => $"KeyMessage([{string.Join(", ", Events)}])";
}

/// <summary>
/// Converts a host key event into the regularized <see cref="KeyEvent"/> stream that
/// <see cref="HardwareKeyboard"/> expects, synthesizing the events needed to keep the two in sync.
/// </summary>
/// <remarks>
/// Flutter's `KeyEventManager` also arbitrates between two engine transit modes (`rawKeyData` and
/// `keyDataThenRawKeyData`). Plumix's host produces only raw host events, so this is the
/// `rawKeyData` path of `_convertRawEventAndStore` plus `handleRawKeyMessage`.
/// </remarks>
public sealed class KeyEventManager
{
    private readonly List<KeyEvent> _keyEventsSinceLastMessage = [];
    private readonly HashSet<PhysicalKeyboardKey> _skippedRawKeysPressed = [];

    private KeyEventManager()
    {
#pragma warning disable CS0618
        RawKeyboard.Instance.AddListener(ConvertRawEventAndStore);
#pragma warning restore CS0618
    }

    public static KeyEventManager Instance { get; } = new();

    /// <summary>
    /// Receives the assembled message once the raw event has been recorded and converted. Defaults
    /// to dispatching each converted event through the focus tree.
    /// </summary>
#pragma warning disable CS0618
    public Func<KeyMessage, bool>? KeyMessageHandler { get; set; }
#pragma warning restore CS0618

    /// <summary>The current frame's timestamp, stamped onto every converted event.</summary>
    public Func<TimeSpan> CurrentSystemFrameTimeStamp { get; set; } = () => TimeSpan.Zero;

    /// <summary>Processes a host key event, returning whether it was handled.</summary>
#pragma warning disable CS0618
    public bool HandleRawKeyEvent(RawKeyEvent rawEvent)
#pragma warning restore CS0618
    {
        ArgumentNullException.ThrowIfNull(rawEvent);
        bool shouldDispatch = true;
        switch (rawEvent)
        {
            case RawKeyDownEvent when !rawEvent.Data.ShouldDispatchEvent():
                shouldDispatch = false;
                _skippedRawKeysPressed.Add(rawEvent.PhysicalKey);
                break;
            case RawKeyDownEvent:
                _skippedRawKeysPressed.Remove(rawEvent.PhysicalKey);
                break;
            case RawKeyUpEvent when _skippedRawKeysPressed.Contains(rawEvent.PhysicalKey):
                _skippedRawKeysPressed.Remove(rawEvent.PhysicalKey);
                shouldDispatch = false;
                break;
        }

        // A skipped event is answered as handled without any dispatch, as in Dart.
        bool handled = true;
        if (shouldDispatch)
        {
            handled = RawKeyboard.Instance.HandleRawKeyEvent(rawEvent);
            foreach (KeyEvent keyEvent in _keyEventsSinceLastMessage)
            {
                handled = HardwareKeyboard.Instance.HandleKeyEvent(keyEvent) || handled;
            }

            handled = DispatchKeyMessage(_keyEventsSinceLastMessage, rawEvent) || handled;
            _keyEventsSinceLastMessage.Clear();
        }

        return handled;
    }

    internal void ClearState()
    {
        _keyEventsSinceLastMessage.Clear();
        _skippedRawKeysPressed.Clear();
        KeyMessageHandler = null;
#pragma warning disable CS0618
        RawKeyboard.Instance.RemoveListener(ConvertRawEventAndStore);
        RawKeyboard.Instance.AddListener(ConvertRawEventAndStore);
#pragma warning restore CS0618
    }

#pragma warning disable CS0618
    private bool DispatchKeyMessage(IReadOnlyList<KeyEvent> keyEvents, RawKeyEvent? rawEvent)
    {
        if (KeyMessageHandler == null)
        {
            return false;
        }

        var message = new KeyMessage([.. keyEvents], rawEvent);
        try
        {
            return KeyMessageHandler(message);
        }
        catch (Exception exception)
        {
            KeyboardDebug.ReportError(exception, "while processing the key message handler");
            return false;
        }
    }

    private void ConvertRawEventAndStore(RawKeyEvent rawEvent)
    {
        PhysicalKeyboardKey physicalKey = rawEvent.PhysicalKey;
        LogicalKeyboardKey logicalKey = rawEvent.LogicalKey;
        var physicalKeysPressed = new HashSet<PhysicalKeyboardKey>(HardwareKeyboard.Instance.PhysicalKeysPressed);
        var eventAfterwards = new List<KeyEvent>();
        LogicalKeyboardKey? recordedLogicalMain = HardwareKeyboard.Instance.LookUpLayout(physicalKey);
        TimeSpan timeStamp = CurrentSystemFrameTimeStamp();
        string? character = rawEvent.Character == string.Empty ? null : rawEvent.Character;
        KeyEventDeviceType deviceType = rawEvent.Data.DeviceType;

        KeyEvent? mainEvent;
        if (rawEvent is RawKeyDownEvent && recordedLogicalMain == null)
        {
            mainEvent = new KeyDownEvent(physicalKey, logicalKey, timeStamp, character, deviceType);
            physicalKeysPressed.Add(physicalKey);
        }
        else if (rawEvent is RawKeyDownEvent)
        {
            // A repeated down keeps the logical key that the first down recorded.
            mainEvent = new KeyRepeatEvent(physicalKey, recordedLogicalMain!, timeStamp, character, deviceType);
        }
        else if (recordedLogicalMain == null)
        {
            mainEvent = null;
        }
        else
        {
            mainEvent = new KeyUpEvent(physicalKey, recordedLogicalMain, timeStamp, deviceType);
            physicalKeysPressed.Remove(physicalKey);
        }

        IReadOnlySet<PhysicalKeyboardKey> rawPressed = RawKeyboard.Instance.PhysicalKeysPressed;
        foreach (PhysicalKeyboardKey key in physicalKeysPressed.Where(key => !rawPressed.Contains(key)))
        {
            if (key.Equals(physicalKey))
            {
                // Only send the extra up event after the main key down event.
                eventAfterwards.Add(new KeyUpEvent(key, logicalKey, timeStamp, deviceType, synthesized: true));
            }
            else
            {
                LogicalKeyboardKey? recorded = HardwareKeyboard.Instance.LookUpLayout(key);
                if (recorded != null)
                {
                    _keyEventsSinceLastMessage.Add(
                        new KeyUpEvent(key, recorded, timeStamp, deviceType, synthesized: true));
                }
            }
        }

        foreach (PhysicalKeyboardKey key in rawPressed.Where(key => !physicalKeysPressed.Contains(key)))
        {
            LogicalKeyboardKey? recorded = RawKeyboard.Instance.LookUpLayout(key);
            if (recorded != null)
            {
                _keyEventsSinceLastMessage.Add(
                    new KeyDownEvent(key, recorded, timeStamp, character: null, deviceType, synthesized: true));
            }
        }

        if (mainEvent != null)
        {
            _keyEventsSinceLastMessage.Add(mainEvent);
        }

        _keyEventsSinceLastMessage.AddRange(eventAfterwards);
    }
#pragma warning restore CS0618
}
