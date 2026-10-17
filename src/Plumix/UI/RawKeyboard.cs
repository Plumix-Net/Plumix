// Dart parity source: flutter/packages/flutter/lib/src/services/raw_keyboard.dart

using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.UI;

/// <summary>An enum describing the side of the keyboard that a key is on.</summary>
/// <remarks>
/// Dart deprecates this enum after v3.18.0-2.0.pre with the rest of the raw keyboard API; it stays
/// unmarked here because <see cref="RawKeyEventData"/>'s abstract members, which every platform data
/// class overrides, take it as a parameter (see the raw-keyboard row in <c>docs/ai/DIVERGENCES.md</c>).
/// </remarks>
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
/// <remarks>Deprecated in Dart with <see cref="KeyboardSide"/>; unmarked here for the same reason.</remarks>
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
/// <remarks>
/// This base class exists to have a common type to use for each of the target platforms' key event
/// data structures: <see cref="RawKeyEventDataAndroid"/>, <see cref="RawKeyEventDataFuchsia"/>,
/// <see cref="RawKeyEventDataMacOs"/>, <see cref="RawKeyEventDataIos"/>,
/// <see cref="RawKeyEventDataLinux"/>, <see cref="RawKeyEventDataWindows"/> and
/// <see cref="RawKeyEventDataWeb"/>.
/// </remarks>
[Obsolete("Platform specific key event data is no longer available. See KeyEvent for what is available. "
          + "Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public abstract class RawKeyEventData : Diagnosticable
{
    /// <summary>Abstract const constructor for subclasses.</summary>
    protected RawKeyEventData()
    {
    }

    /// <summary>Returns true if the given <see cref="ModifierKey"/> was pressed at the time of this event.</summary>
    public abstract bool IsModifierPressed(ModifierKey key, KeyboardSide side = KeyboardSide.Any);

    /// <summary>
    /// Returns a <see cref="KeyboardSide"/> enum value that describes which side or sides of the
    /// given keyboard modifier key were pressed at the time of this event, or null if it was not pressed.
    /// </summary>
    public abstract KeyboardSide? GetModifierSide(ModifierKey key);

    /// <summary>Returns true if a CTRL modifier key was pressed at the time of this event.</summary>
    public bool IsControlPressed => IsModifierPressed(ModifierKey.ControlModifier);

    /// <summary>Returns true if a SHIFT modifier key was pressed at the time of this event.</summary>
    public bool IsShiftPressed => IsModifierPressed(ModifierKey.ShiftModifier);

    /// <summary>Returns true if an ALT modifier key was pressed at the time of this event.</summary>
    public bool IsAltPressed => IsModifierPressed(ModifierKey.AltModifier);

    /// <summary>Returns true if a META modifier key was pressed at the time of this event.</summary>
    public bool IsMetaPressed => IsModifierPressed(ModifierKey.MetaModifier);

    /// <summary>Returns a map of modifier keys that were pressed at the time of this event, and the
    /// keyboard side or sides that the key was on.</summary>
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

                if (Constants.KDebugMode && side == null)
                {
                    Print.DebugPrint(
                        "Raw key data is returning inconsistent information for "
                        + "pressed modifiers. isModifierPressed returns true for "
                        + $"{RawKeyboard.DescribeModifierKey(key)} "
                        + "being pressed, but when getModifierSide is called, it says "
                        + "that no modifiers are pressed.");
                    if (this is RawKeyEventDataAndroid android)
                    {
                        Print.DebugPrint($"Android raw key metaState: {android.MetaState}");
                    }
                }
            }

            return result;
        }
    }

    /// <summary>Returns an object representing the physical location of this key on a QWERTY keyboard.</summary>
    public abstract PhysicalKeyboardKey PhysicalKey { get; }

    /// <summary>Returns an object representing the logical key that was pressed.</summary>
    public abstract LogicalKeyboardKey LogicalKey { get; }

    /// <summary>
    /// Returns the Unicode string representing the label on this key, or an empty string if there
    /// is no label.
    /// </summary>
    public abstract string KeyLabel { get; }

    /// <summary>Whether a key down event, and likewise its accompanying key up event, should be dispatched.</summary>
    public virtual bool ShouldDispatchEvent() => true;
}

/// <summary>Defines the interface for raw key events.</summary>
[Obsolete("Use KeyEvent instead. Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public abstract class RawKeyEvent : Diagnosticable
{
    /// <summary>Initializes fields for subclasses, and provides a const constructor for const subclasses.</summary>
    protected RawKeyEvent(RawKeyEventData data, string? character = null, bool repeat = false)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Character = character;
        Repeat = repeat;
    }

    /// <summary>
    /// Creates a concrete <see cref="RawKeyEvent"/> class from a message in the form received on
    /// <see cref="SystemChannels.KeyEvent"/>.
    /// </summary>
    public static RawKeyEvent FromMessage(IReadOnlyDictionary<string, object?> message)
    {
        ArgumentNullException.ThrowIfNull(message);
        string? character = null;

        RawKeyEventData DataFromWeb()
        {
            string? key = message.GetValueOrDefault("key") as string;
            if (key != null && key.Length > 0 && key.Length == 1)
            {
                character = key;
            }

            return new RawKeyEventDataWeb(
                code: message.GetValueOrDefault("code") as string ?? string.Empty,
                key: key ?? string.Empty,
                location: IntOf(message, "location") ?? 0,
                metaState: IntOf(message, "metaState") ?? 0,
                keyCode: IntOf(message, "keyCode") ?? 0);
        }

        RawKeyEventData data;
        if (Constants.KIsWeb)
        {
            data = DataFromWeb();
        }
        else
        {
            string keymap = (string)message["keymap"]!;
            switch (keymap)
            {
                case "android":
                    data = new RawKeyEventDataAndroid(
                        flags: IntOf(message, "flags") ?? 0,
                        codePoint: IntOf(message, "codePoint") ?? 0,
                        keyCode: IntOf(message, "keyCode") ?? 0,
                        plainCodePoint: IntOf(message, "plainCodePoint") ?? 0,
                        scanCode: IntOf(message, "scanCode") ?? 0,
                        metaState: IntOf(message, "metaState") ?? 0,
                        eventSource: IntOf(message, "source") ?? 0,
                        vendorId: IntOf(message, "vendorId") ?? 0,
                        productId: IntOf(message, "productId") ?? 0,
                        deviceId: IntOf(message, "deviceId") ?? 0,
                        repeatCount: IntOf(message, "repeatCount") ?? 0);
                    if (message.ContainsKey("character"))
                    {
                        character = message["character"] as string;
                    }

                    break;
                case "fuchsia":
                    int fuchsiaCodePoint = IntOf(message, "codePoint") ?? 0;
                    data = new RawKeyEventDataFuchsia(
                        hidUsage: IntOf(message, "hidUsage") ?? 0,
                        codePoint: fuchsiaCodePoint,
                        modifiers: IntOf(message, "modifiers") ?? 0);
                    if (fuchsiaCodePoint != 0)
                    {
                        character = FromCharCode(fuchsiaCodePoint);
                    }

                    break;
                case "macos":
                    data = new RawKeyEventDataMacOs(
                        characters: message.GetValueOrDefault("characters") as string ?? string.Empty,
                        charactersIgnoringModifiers:
                            message.GetValueOrDefault("charactersIgnoringModifiers") as string ?? string.Empty,
                        keyCode: IntOf(message, "keyCode") ?? 0,
                        modifiers: IntOf(message, "modifiers") ?? 0,
                        specifiedLogicalKey: LongOf(message, "specifiedLogicalKey"));
                    character = message.GetValueOrDefault("characters") as string;
                    break;
                case "ios":
                    data = new RawKeyEventDataIos(
                        characters: message.GetValueOrDefault("characters") as string ?? string.Empty,
                        charactersIgnoringModifiers:
                            message.GetValueOrDefault("charactersIgnoringModifiers") as string ?? string.Empty,
                        keyCode: IntOf(message, "keyCode") ?? 0,
                        modifiers: IntOf(message, "modifiers") ?? 0);
                    if (message.GetValueOrDefault("characters") is string { Length: > 0 } characters)
                    {
                        character = characters;
                    }

                    break;
                case "linux":
                    long unicodeScalarValues = LongOf(message, "unicodeScalarValues") ?? 0;
                    data = new RawKeyEventDataLinux(
                        keyHelper: KeyHelper.Create(message.GetValueOrDefault("toolkit") as string ?? string.Empty),
                        unicodeScalarValues: unicodeScalarValues,
                        keyCode: IntOf(message, "keyCode") ?? 0,
                        scanCode: IntOf(message, "scanCode") ?? 0,
                        modifiers: IntOf(message, "modifiers") ?? 0,
                        isDown: Equals(message.GetValueOrDefault("type"), "keydown"),
                        specifiedLogicalKey: LongOf(message, "specifiedLogicalKey"));
                    if (unicodeScalarValues != 0)
                    {
                        character = FromCharCode(unchecked((int)unicodeScalarValues));
                    }

                    break;
                case "windows":
                    int characterCodePoint = IntOf(message, "characterCodePoint") ?? 0;
                    data = new RawKeyEventDataWindows(
                        keyCode: IntOf(message, "keyCode") ?? 0,
                        scanCode: IntOf(message, "scanCode") ?? 0,
                        characterCodePoint: characterCodePoint,
                        modifiers: IntOf(message, "modifiers") ?? 0);
                    if (characterCodePoint != 0)
                    {
                        character = FromCharCode(characterCodePoint);
                    }

                    break;
                case "web":
                    data = DataFromWeb();
                    break;
                default:
                    // Raw key events are not yet implemented on this platform.
                    throw new FlutterError($"Unknown keymap for key events: {keymap}");
            }
        }

        bool repeat = RawKeyboard.Instance.PhysicalKeysPressed.Contains(data.PhysicalKey);
        string type = (string)message["type"]!;
        return type switch
        {
            "keydown" => new RawKeyDownEvent(data, character, repeat),
            "keyup" => new RawKeyUpEvent(data),
            _ => throw new FlutterError($"Unknown key event type: {type}"),
        };
    }

    /// <summary>Returns true if the given <see cref="LogicalKeyboardKey"/> is pressed.</summary>
    public bool IsKeyPressed(LogicalKeyboardKey key) => RawKeyboard.Instance.KeysPressed.Contains(key);

    /// <summary>Returns true if a CTRL modifier key is pressed, regardless of which side of the
    /// keyboard it is on.</summary>
    public bool IsControlPressed => IsKeyPressed(LogicalKeyboardKey.ControlLeft)
                                    || IsKeyPressed(LogicalKeyboardKey.ControlRight);

    /// <summary>Returns true if a SHIFT modifier key is pressed, regardless of which side of the
    /// keyboard it is on.</summary>
    public bool IsShiftPressed => IsKeyPressed(LogicalKeyboardKey.ShiftLeft)
                                  || IsKeyPressed(LogicalKeyboardKey.ShiftRight);

    /// <summary>Returns true if an ALT modifier key is pressed, regardless of which side of the
    /// keyboard it is on.</summary>
    public bool IsAltPressed => IsKeyPressed(LogicalKeyboardKey.AltLeft)
                                || IsKeyPressed(LogicalKeyboardKey.AltRight);

    /// <summary>Returns true if a META modifier key is pressed, regardless of which side of the
    /// keyboard it is on.</summary>
    public bool IsMetaPressed => IsKeyPressed(LogicalKeyboardKey.MetaLeft)
                                 || IsKeyPressed(LogicalKeyboardKey.MetaRight);

    /// <summary>Returns an object representing the physical location of this key.</summary>
    public PhysicalKeyboardKey PhysicalKey => Data.PhysicalKey;

    /// <summary>Returns an object representing the logical key that was pressed.</summary>
    public LogicalKeyboardKey LogicalKey => Data.LogicalKey;

    /// <summary>Returns the Unicode character (grapheme cluster) completed by this keystroke, if any.</summary>
    public string? Character { get; }

    /// <summary>Whether this is a repeated down event. Always false for <see cref="RawKeyUpEvent"/>.</summary>
    public bool Repeat { get; }

    /// <summary>Platform-specific information about the key event.</summary>
    public RawKeyEventData Data { get; }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<LogicalKeyboardKey>("logicalKey", LogicalKey));
        properties.Add(new DiagnosticsProperty<PhysicalKeyboardKey>("physicalKey", PhysicalKey));
        if (this is RawKeyDownEvent)
        {
            properties.Add(new DiagnosticsProperty<bool>("repeat", Repeat));
        }
    }

    // Dart's `message['x'] as int?`: the JSON codec decodes small numbers as `int` and larger ones as
    // `long`, both of which are Dart `int`s.
    private static int? IntOf(IReadOnlyDictionary<string, object?> message, string key) =>
        message.GetValueOrDefault(key) switch
        {
            null => null,
            int value => value,
            long value => unchecked((int)value),
            object value => throw new InvalidCastException(
                $"type '{value.GetType().Name}' is not a subtype of type 'int?' in type cast"),
        };

    private static long? LongOf(IReadOnlyDictionary<string, object?> message, string key) =>
        message.GetValueOrDefault(key) switch
        {
            null => null,
            int value => value,
            long value => value,
            object value => throw new InvalidCastException(
                $"type '{value.GetType().Name}' is not a subtype of type 'int?' in type cast"),
        };

    // Dart's `String.fromCharCode`: a surrogate pair above the BMP, a lone code unit otherwise.
    private static string FromCharCode(int charCode) =>
        charCode is >= 0 and <= 0xFFFF ? ((char)charCode).ToString() : char.ConvertFromUtf32(charCode);
}

/// <summary>The user has pressed a key on the keyboard.</summary>
[Obsolete("Use KeyDownEvent instead. Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public sealed class RawKeyDownEvent : RawKeyEvent
{
    /// <summary>Creates a key event that represents the user pressing a key.</summary>
    public RawKeyDownEvent(RawKeyEventData data, string? character = null, bool repeat = false)
        : base(data, character, repeat)
    {
    }
}

/// <summary>The user has released a key on the keyboard.</summary>
[Obsolete("Use KeyUpEvent instead. Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public sealed class RawKeyUpEvent : RawKeyEvent
{
    /// <summary>Creates a key event that represents the user releasing a key.</summary>
    public RawKeyUpEvent(RawKeyEventData data, string? character = null)
        : base(data, character, repeat: false)
    {
    }
}

/// <summary>A callback type used by <see cref="RawKeyboard.KeyEventHandler"/> to send key events to a handler
/// that can determine if the key has been handled or not.</summary>
[Obsolete("Use KeyEventCallback instead. Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public delegate bool RawKeyEventHandler(RawKeyEvent @event);

/// <summary>
/// An interface for listening to raw key events.
/// </summary>
[Obsolete("Use HardwareKeyboard instead. Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public sealed class RawKeyboard
{
    private readonly List<Action<RawKeyEvent>> _listeners = [];
    private readonly OrderedDictionary<PhysicalKeyboardKey, LogicalKeyboardKey> _keysPressed = [];
    private RawKeyEventHandler? _cachedKeyEventHandler;
    private KeyMessageHandler? _cachedKeyMessageHandler;

    private RawKeyboard()
    {
    }

    /// <summary>The <see cref="RawKeyboard"/> instance to which RawKeyboard events are sent.</summary>
    public static RawKeyboard Instance { get; } = new();

    /// <summary>Register a listener that is called every time the user presses or releases a hardware
    /// keyboard key.</summary>
    /// <remarks>Since the listeners have no way to indicate what they did with the event, listeners are
    /// assumed to not handle the key event.</remarks>
    public void AddListener(Action<RawKeyEvent> listener)
    {
        _listeners.Add(listener);
    }

    /// <summary>Stop calling the given listener every time the user presses or releases a hardware
    /// keyboard key.</summary>
    public void RemoveListener(Action<RawKeyEvent> listener)
    {
        _listeners.Remove(listener);
    }

    /// <summary>A handler for raw hardware keyboard events that will stop propagation if the handler
    /// returns true.</summary>
    /// <remarks>
    /// This property is only a wrapper over <c>KeyEventManager.KeyMessageHandler</c>, and is kept only
    /// for backward compatibility.
    /// </remarks>
    public RawKeyEventHandler? KeyEventHandler
    {
        get
        {
            KeyEventManager keyEventManager = ServicesBinding.Instance.KeyEventManager;
            if (keyEventManager.KeyMessageHandler != _cachedKeyMessageHandler)
            {
                _cachedKeyMessageHandler = keyEventManager.KeyMessageHandler;
                _cachedKeyEventHandler = _cachedKeyMessageHandler == null
                    ? null
                    : _ =>
                    {
                        DebugAssertions.Assert(
                            false,
                            "The RawKeyboard.instance.keyEventHandler assigned by Flutter is a dummy "
                            + "callback kept for compatibility and should not be directly called. Use "
                            + "ServicesBinding.instance!.keyMessageHandler instead.");
                        return true;
                    };
            }

            return _cachedKeyEventHandler;
        }
        set
        {
            _cachedKeyEventHandler = value;
            _cachedKeyMessageHandler = value == null
                ? null
                : message => message.RawEvent != null && value(message.RawEvent);
            ServicesBinding.Instance.KeyEventManager.KeyMessageHandler = _cachedKeyMessageHandler;
        }
    }

    /// <summary>Process a new <see cref="RawKeyEvent"/> by recording the state changes and dispatching to
    /// listeners.</summary>
    public bool HandleRawKeyEvent(RawKeyEvent @event)
    {
        if (@event is RawKeyDownEvent)
        {
            _keysPressed[@event.PhysicalKey] = @event.LogicalKey;
        }
        else if (@event is RawKeyUpEvent)
        {
            // Use the physical key in the key up event to find the physical key from
            // the corresponding key down event and remove it, even if the logical
            // keys don't match.
            _keysPressed.Remove(@event.PhysicalKey);
        }

        // Make sure that the modifiers reflect reality, in case a modifier key was
        // pressed/released while the app didn't have focus.
        SynchronizeModifiers(@event);
        DebugAssertions.Assert(
            @event is not RawKeyDownEvent || _keysPressed.Count > 0,
            "Attempted to send a key down event when no keys are in keysPressed. "
            + "This state can occur if the key event being sent doesn't properly "
            + $"set its modifier flags. This was the event: {@event} and its data: {@event.Data}");
        // Send the event to passive listeners.
        foreach (Action<RawKeyEvent> listener in _listeners.ToList())
        {
            try
            {
                if (_listeners.Contains(listener))
                {
                    listener(@event);
                }
            }
            catch (Exception exception)
            {
                InformationCollector? collector = null;
                if (Constants.KDebugMode)
                {
                    collector = () => [new DiagnosticsProperty<RawKeyEvent>("Event", @event)];
                }

                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    stack: exception.StackTrace,
                    library: "services library",
                    context: new ErrorDescription("while processing a raw key listener"),
                    informationCollector: collector));
            }
        }

        return false;
    }

    private static readonly Dictionary<ModifierSidePair, IReadOnlySet<PhysicalKeyboardKey>> ModifierKeyMap =
        new()
        {
            [new(ModifierKey.AltModifier, KeyboardSide.Left)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.AltLeft },
            [new(ModifierKey.AltModifier, KeyboardSide.Right)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.AltRight },
            [new(ModifierKey.AltModifier, KeyboardSide.All)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.AltLeft, PhysicalKeyboardKey.AltRight },
            [new(ModifierKey.AltModifier, KeyboardSide.Any)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.AltLeft },
            [new(ModifierKey.ShiftModifier, KeyboardSide.Left)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.ShiftLeft },
            [new(ModifierKey.ShiftModifier, KeyboardSide.Right)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.ShiftRight },
            [new(ModifierKey.ShiftModifier, KeyboardSide.All)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.ShiftLeft, PhysicalKeyboardKey.ShiftRight },
            [new(ModifierKey.ShiftModifier, KeyboardSide.Any)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.ShiftLeft },
            [new(ModifierKey.ControlModifier, KeyboardSide.Left)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.ControlLeft },
            [new(ModifierKey.ControlModifier, KeyboardSide.Right)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.ControlRight },
            [new(ModifierKey.ControlModifier, KeyboardSide.All)] =
                new HashSet<PhysicalKeyboardKey>
                {
                    PhysicalKeyboardKey.ControlLeft, PhysicalKeyboardKey.ControlRight,
                },
            [new(ModifierKey.ControlModifier, KeyboardSide.Any)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.ControlLeft },
            [new(ModifierKey.MetaModifier, KeyboardSide.Left)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.MetaLeft },
            [new(ModifierKey.MetaModifier, KeyboardSide.Right)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.MetaRight },
            [new(ModifierKey.MetaModifier, KeyboardSide.All)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.MetaLeft, PhysicalKeyboardKey.MetaRight },
            [new(ModifierKey.MetaModifier, KeyboardSide.Any)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.MetaLeft },
            [new(ModifierKey.CapsLockModifier, KeyboardSide.All)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.CapsLock },
            [new(ModifierKey.NumLockModifier, KeyboardSide.All)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.NumLock },
            [new(ModifierKey.ScrollLockModifier, KeyboardSide.All)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.ScrollLock },
            [new(ModifierKey.FunctionModifier, KeyboardSide.All)] =
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.Fn },
            // The symbolModifier doesn't have a key representation on any of the
            // platforms, so don't map it here.
        };

    // The map of all modifier keys except Fn, since that is treated differently
    // on some platforms.
    private static readonly OrderedDictionary<PhysicalKeyboardKey, LogicalKeyboardKey> AllModifiersExceptFn = new()
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

    // The map of all modifier keys that are represented in modifier key bit
    // masks on all platforms, so that they can be cleared out of pressedKeys when
    // synchronizing.
    private static readonly OrderedDictionary<PhysicalKeyboardKey, LogicalKeyboardKey> AllModifiers =
        BuildAllModifiers();

    private static OrderedDictionary<PhysicalKeyboardKey, LogicalKeyboardKey> BuildAllModifiers()
    {
        var result = new OrderedDictionary<PhysicalKeyboardKey, LogicalKeyboardKey>
        {
            [PhysicalKeyboardKey.Fn] = LogicalKeyboardKey.Fn,
        };
        foreach ((PhysicalKeyboardKey physicalKey, LogicalKeyboardKey logicalKey) in AllModifiersExceptFn)
        {
            result[physicalKey] = logicalKey;
        }

        return result;
    }

    private void SynchronizeModifiers(RawKeyEvent @event)
    {
        // Compare modifier states to the ground truth as specified by
        // [RawKeyEvent.data.modifiersPressed] and update unsynchronized ones.
        //
        // This function will update the state of modifier keys in `_keysPressed` so
        // that they match the ones given by [RawKeyEvent.data.modifiersPressed].
        // For a `modifiersPressed` result of anything but [KeyboardSide.any], the
        // states in `_keysPressed` will be updated to exactly match the result,
        // i.e. exactly one of "no down", "left down", "right down" or "both down".
        //
        // If `modifiersPressed` returns [KeyboardSide.any], the states in
        // `_keysPressed` will be updated to a rough match, i.e. "either side down"
        // or "no down". If `_keysPressed` has no modifier down, a
        // [KeyboardSide.any] will synchronize by forcing the left modifier down. If
        // `_keysPressed` has any modifier down, a [KeyboardSide.any] will not cause
        // a state change.
        IReadOnlyDictionary<ModifierKey, KeyboardSide> modifiersPressed = @event.Data.ModifiersPressed;
        var modifierKeys = new OrderedDictionary<PhysicalKeyboardKey, LogicalKeyboardKey>();
        // Physical keys that whose modifiers are pressed at any side.
        var anySideKeys = new HashSet<PhysicalKeyboardKey>();
        var keysPressedAfterEvent = new HashSet<PhysicalKeyboardKey>(_keysPressed.Keys);
        if (@event is RawKeyDownEvent)
        {
            keysPressedAfterEvent.Add(@event.PhysicalKey);
        }

        ModifierKey? thisKeyModifier = null;
        foreach (ModifierKey key in Enum.GetValues<ModifierKey>())
        {
            IReadOnlySet<PhysicalKeyboardKey>? thisModifierKeys =
                ModifierKeyMap.GetValueOrDefault(new ModifierSidePair(key, KeyboardSide.All));
            if (thisModifierKeys == null)
            {
                continue;
            }

            if (thisModifierKeys.Contains(@event.PhysicalKey))
            {
                thisKeyModifier = key;
            }

            bool hasSide = modifiersPressed.TryGetValue(key, out KeyboardSide side);
            if (hasSide && side == KeyboardSide.Any)
            {
                anySideKeys.UnionWith(thisModifierKeys);
                if (thisModifierKeys.Any(keysPressedAfterEvent.Contains))
                {
                    continue;
                }
            }

            IReadOnlySet<PhysicalKeyboardKey>? mappedKeys = !hasSide
                ? new HashSet<PhysicalKeyboardKey>()
                : ModifierKeyMap.GetValueOrDefault(new ModifierSidePair(key, side));
            if (Constants.KDebugMode && mappedKeys == null)
            {
                Print.DebugPrint(
                    $"Platform key support for {Environment.OSVersion.Platform.ToString().ToLowerInvariant()} "
                    + "is producing unsupported modifier combinations for "
                    + $"modifier {DescribeModifierKey(key)} on side {DescribeKeyboardSide(side)}.");
                if (@event.Data is RawKeyEventDataAndroid android)
                {
                    Print.DebugPrint($"Android raw key metaState: {android.MetaState}");
                }
            }

            if (mappedKeys == null)
            {
                continue;
            }

            foreach (PhysicalKeyboardKey physicalModifier in mappedKeys)
            {
                modifierKeys[physicalModifier] = AllModifiers[physicalModifier];
            }
        }

        // On Linux, CapsLock key can be mapped to a non-modifier logical key:
        // https://github.com/flutter/flutter/issues/114591.
        // This is also affecting Flutter Web on Linux.
        bool nonModifierCapsLock = @event.Data is RawKeyEventDataLinux or RawKeyEventDataWeb
                                   && _keysPressed.TryGetValue(
                                       PhysicalKeyboardKey.CapsLock,
                                       out LogicalKeyboardKey? capsLockLogical)
                                   && !capsLockLogical.Equals(LogicalKeyboardKey.CapsLock);
        foreach (PhysicalKeyboardKey physicalKey in AllModifiersExceptFn.Keys)
        {
            bool skipReleasingKey = nonModifierCapsLock && physicalKey.Equals(PhysicalKeyboardKey.CapsLock);
            if (!anySideKeys.Contains(physicalKey) && !skipReleasingKey)
            {
                _keysPressed.Remove(physicalKey);
            }
        }

        if (@event.Data is not RawKeyEventDataFuchsia && @event.Data is not RawKeyEventDataMacOs)
        {
            // On Fuchsia and macOS, the Fn key is not considered a modifier key.
            _keysPressed.Remove(PhysicalKeyboardKey.Fn);
        }

        foreach ((PhysicalKeyboardKey physicalKey, LogicalKeyboardKey logicalKey) in modifierKeys)
        {
            _keysPressed[physicalKey] = logicalKey;
        }

        // In rare cases, the event presses a modifier key but the key does not
        // exist in the modifier list. Enforce the pressing state.
        if (@event is RawKeyDownEvent
            && thisKeyModifier != null
            && !_keysPressed.ContainsKey(@event.PhysicalKey))
        {
            // So far this inconsistency is only found on Linux GTK for AltRight in a
            // rare case. (See https://github.com/flutter/flutter/issues/93278 .) In
            // other cases, this inconsistency will be caught by an assertion later.
            if ((@event.Data is RawKeyEventDataLinux && @event.PhysicalKey.Equals(PhysicalKeyboardKey.AltRight))
                || @event.Data is RawKeyEventDataIos
                || @event.Data is RawKeyEventDataAndroid)
            {
                // On iOS and Android, the CapsLock modifier flag could be missing on the
                // key down event, so the CapsLock logical key needs to be added here.
                // See https://github.com/flutter/flutter/issues/158962 .
                if (AllModifiersExceptFn.TryGetValue(@event.PhysicalKey, out LogicalKeyboardKey? logicalKey))
                {
                    _keysPressed[@event.PhysicalKey] = logicalKey;
                }
            }

            // On Web, PhysicalKeyboardKey.altRight can be mapped to
            // LogicalKeyboardKey.altGraph or LogicalKeyboardKey.altRight:
            // https://github.com/flutter/flutter/issues/113836.
            // And the modifier flag might be missing:
            // https://github.com/flutter/flutter/issues/113836.
            if (@event.Data is RawKeyEventDataWeb && @event.PhysicalKey.Equals(PhysicalKeyboardKey.AltRight))
            {
                _keysPressed[@event.PhysicalKey] = @event.LogicalKey;
            }
        }
    }

    /// <summary>Returns the set of keys currently pressed.</summary>
    public IReadOnlySet<LogicalKeyboardKey> KeysPressed => new HashSet<LogicalKeyboardKey>(_keysPressed.Values);

    /// <summary>Returns the set of physical keys currently pressed.</summary>
    public IReadOnlySet<PhysicalKeyboardKey> PhysicalKeysPressed =>
        new HashSet<PhysicalKeyboardKey>(_keysPressed.Keys);

    /// <summary>
    /// The physical keys currently pressed, in the order they were recorded — the iteration order of
    /// Dart's <c>physicalKeysPressed</c>, which <c>KeyEventManager</c> relies on.
    /// </summary>
    internal IReadOnlyList<PhysicalKeyboardKey> PhysicalKeysPressedInOrder => [.. _keysPressed.Keys];

    /// <summary>Returns the logical key that corresponds to the given pressed physical key.</summary>
    /// <returns>Null if the physical key is not currently pressed.</returns>
    public LogicalKeyboardKey? LookUpLayout(PhysicalKeyboardKey physicalKey) =>
        _keysPressed.GetValueOrDefault(physicalKey);

    /// <summary>Clears the list of keys returned from <see cref="KeysPressed"/>.</summary>
    /// <remarks>This is used by the testing framework to make sure tests are hermetic.</remarks>
    public void ClearKeysPressed() => _keysPressed.Clear();

    /// <summary>
    /// C#-only: drops every listener. Plumix's test harness replaces the focus manager per test, and
    /// <c>KeyboardListener</c> states that outlived their tree would otherwise keep receiving events.
    /// </summary>
    internal void ClearListeners() => _listeners.Clear();

    internal static string DescribeModifierKey(ModifierKey key) => $"ModifierKey.{Diagnostics.EnumName(key)}";

    private static string DescribeKeyboardSide(KeyboardSide side) => $"KeyboardSide.{Diagnostics.EnumName(side)}";

    private readonly record struct ModifierSidePair(ModifierKey Modifier, KeyboardSide? Side);
}
