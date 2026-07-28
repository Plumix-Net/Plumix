using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/shortcuts.dart

public enum LockState
{
    Ignored,
    Locked,
    Unlocked
}

public interface ShortcutActivator
{
    IReadOnlySet<string>? Triggers { get; }

    bool Accepts(KeyEvent @event, HardwareKeyboard state);

    string DebugDescribeKeys();

    static bool IsActivatedBy(ShortcutActivator activator, KeyEvent @event)
    {
        ArgumentNullException.ThrowIfNull(activator);
        ArgumentNullException.ThrowIfNull(@event);
        return activator.Accepts(@event, HardwareKeyboard.Instance);
    }
}

public class KeySet<T> : IEquatable<KeySet<T>> where T : notnull
{
    private readonly HashSet<T> _keys;

    public KeySet(T key1, T? key2 = default, T? key3 = default, T? key4 = default)
    {
        _keys = [key1];
        AddOptionalUnique(key2);
        AddOptionalUnique(key3);
        AddOptionalUnique(key4);
    }

    public KeySet(IReadOnlySet<T> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        if (keys.Count == 0)
        {
            throw new ArgumentException("A key set cannot be empty.", nameof(keys));
        }

        _keys = [.. keys];
    }

    public IReadOnlySet<T> Keys => new HashSet<T>(_keys);

    public bool Equals(KeySet<T>? other)
    {
        return other != null && other.GetType() == GetType() && _keys.SetEquals(other._keys);
    }

    public override bool Equals(object? obj) => obj is KeySet<T> other && Equals(other);

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (int keyHash in _keys.Select(key => key.GetHashCode()).Order())
        {
            hash = HashCode.Combine(hash, keyHash);
        }

        return hash;
    }

    protected IReadOnlySet<T> InternalKeys => _keys;

    private void AddOptionalUnique(T? key)
    {
        if (key is null)
        {
            return;
        }

        if (!_keys.Add(key))
        {
            throw new ArgumentException("Each key may appear only once in a KeySet.");
        }
    }
}

public sealed class LogicalKeySet : KeySet<string>, ShortcutActivator, IEquatable<LogicalKeySet>
{
    public LogicalKeySet(string key1, string? key2 = null, string? key3 = null, string? key4 = null)
        : base(
            key1 ?? throw new ArgumentNullException(nameof(key1)),
            key2,
            key3,
            key4)
    {
    }

    public LogicalKeySet(IReadOnlySet<string> keys) : base(keys)
    {
    }

    public bool Accepts(KeyEvent @event, HardwareKeyboard state)
    {
        if (!@event.IsDown)
        {
            return false;
        }

        HashSet<string> pressed = BuildPressedKeys(@event, state);
        return NormalizeKeys(InternalKeys).SetEquals(NormalizeKeys(pressed));
    }

    public string DebugDescribeKeys() => string.Join(" + ", InternalKeys.Order(StringComparer.Ordinal));

    public IReadOnlySet<string> Triggers => new HashSet<string>(InternalKeys, StringComparer.Ordinal);

    bool IEquatable<LogicalKeySet>.Equals(LogicalKeySet? other) => Equals(other);

    internal static HashSet<string> BuildPressedKeys(KeyEvent @event, HardwareKeyboard state)
    {
        var pressed = new HashSet<string>(state.LogicalKeysPressed, StringComparer.Ordinal)
        {
            @event.Key
        };

        AddModifierFromEvent(pressed, @event.IsControlPressed, "Control");
        AddModifierFromEvent(pressed, @event.IsShiftPressed, "Shift");
        AddModifierFromEvent(pressed, @event.IsAltPressed, "Alt");
        AddModifierFromEvent(pressed, @event.IsMetaPressed, "Meta");
        return pressed;
    }

    internal static HashSet<string> NormalizeKeys(IEnumerable<string> keys)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (string key in keys)
        {
            result.Add(NormalizeKey(key));
        }

        return result;
    }

    internal static string NormalizeKey(string key)
    {
        string normalized = key switch
        {
            "LeftControl" or "RightControl" or "ControlLeft" or "ControlRight" or "Ctrl" => "Control",
            "LeftShift" or "RightShift" or "ShiftLeft" or "ShiftRight" => "Shift",
            "LeftAlt" or "RightAlt" or "AltLeft" or "AltRight" => "Alt",
            "LeftMeta" or "RightMeta" or "MetaLeft" or "MetaRight"
                or "Command" or "Windows" => "Meta",
            "ArrowLeft" => "Left",
            "ArrowRight" => "Right",
            "ArrowUp" => "Up",
            "ArrowDown" => "Down",
            _ => key
        };

        if (normalized.Length == 4
            && normalized.StartsWith("Key", StringComparison.Ordinal)
            && char.IsLetter(normalized[3]))
        {
            return char.ToUpperInvariant(normalized[3]).ToString();
        }

        if (normalized.Length == 2
            && normalized[0] == 'D'
            && char.IsDigit(normalized[1]))
        {
            return normalized[1].ToString();
        }

        if (normalized.Length == 6
            && normalized.StartsWith("Digit", StringComparison.Ordinal)
            && char.IsDigit(normalized[5]))
        {
            return normalized[5].ToString();
        }

        return normalized;
    }

    private static void AddModifierFromEvent(HashSet<string> pressed, bool isPressed, string key)
    {
        if (isPressed)
        {
            pressed.Add(key);
        }
    }
}

public sealed class SingleActivator : ShortcutActivator, IEquatable<SingleActivator>
{
    public SingleActivator(
        string trigger,
        bool control = false,
        bool shift = false,
        bool alt = false,
        bool meta = false,
        bool includeRepeats = true,
        LockState numLock = LockState.Ignored)
    {
        Trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
        if (IsModifierKey(Trigger))
        {
            throw new ArgumentException("SingleActivator trigger must not be a modifier key.", nameof(trigger));
        }

        Control = control;
        Shift = shift;
        Alt = alt;
        Meta = meta;
        IncludeRepeats = includeRepeats;
        NumLock = numLock;
    }

    public string Trigger { get; }

    public bool Control { get; }

    public bool Shift { get; }

    public bool Alt { get; }

    public bool Meta { get; }

    public bool IncludeRepeats { get; }

    public LockState NumLock { get; }

    public IReadOnlySet<string> Triggers => new HashSet<string>(StringComparer.Ordinal) { Trigger };

    public bool Accepts(KeyEvent @event, HardwareKeyboard state)
    {
        return @event.IsDown
               && (IncludeRepeats || !@event.IsRepeat)
               && string.Equals(
                   LogicalKeySet.NormalizeKey(@event.Key),
                   LogicalKeySet.NormalizeKey(Trigger),
                   StringComparison.Ordinal)
               && @event.IsControlPressed == Control
               && @event.IsShiftPressed == Shift
               && @event.IsAltPressed == Alt
               && @event.IsMetaPressed == Meta
               && NumLock switch
               {
                   LockState.Locked => @event.IsNumLockOn,
                   LockState.Unlocked => !@event.IsNumLockOn,
                   _ => true
               };
    }

    public string DebugDescribeKeys()
    {
        var keys = new List<string>();
        if (Control)
        {
            keys.Add("Control");
        }

        if (Alt)
        {
            keys.Add("Alt");
        }

        if (Meta)
        {
            keys.Add("Meta");
        }

        if (Shift)
        {
            keys.Add("Shift");
        }

        keys.Add(Trigger);
        return string.Join(" + ", keys);
    }

    public bool Equals(SingleActivator? other)
    {
        return other != null
               && string.Equals(Trigger, other.Trigger, StringComparison.Ordinal)
               && Control == other.Control
               && Shift == other.Shift
               && Alt == other.Alt
               && Meta == other.Meta
               && IncludeRepeats == other.IncludeRepeats
               && NumLock == other.NumLock;
    }

    public override bool Equals(object? obj) => obj is SingleActivator other && Equals(other);

    public override int GetHashCode()
    {
        return HashCode.Combine(Trigger, Control, Shift, Alt, Meta, IncludeRepeats, NumLock);
    }

    private static bool IsModifierKey(string key)
    {
        string normalized = LogicalKeySet.NormalizeKey(key);
        return normalized is "Control" or "Shift" or "Alt" or "Meta";
    }
}

public sealed class CharacterActivator : ShortcutActivator, IEquatable<CharacterActivator>
{
    public CharacterActivator(
        string character,
        bool control = false,
        bool alt = false,
        bool meta = false,
        bool includeRepeats = true)
    {
        Character = character ?? throw new ArgumentNullException(nameof(character));
        Control = control;
        Alt = alt;
        Meta = meta;
        IncludeRepeats = includeRepeats;
    }

    public string Character { get; }

    public bool Control { get; }

    public bool Alt { get; }

    public bool Meta { get; }

    public bool IncludeRepeats { get; }

    public IReadOnlySet<string>? Triggers => null;

    public bool Accepts(KeyEvent @event, HardwareKeyboard state)
    {
        return @event.IsDown
               && (IncludeRepeats || !@event.IsRepeat)
               && string.Equals(@event.Character, Character, StringComparison.Ordinal)
               && @event.IsControlPressed == Control
               && @event.IsAltPressed == Alt
               && @event.IsMetaPressed == Meta;
    }

    public string DebugDescribeKeys()
    {
        var keys = new List<string>();
        if (Control)
        {
            keys.Add("Control");
        }

        if (Alt)
        {
            keys.Add("Alt");
        }

        if (Meta)
        {
            keys.Add("Meta");
        }

        keys.Add($"'{Character}'");
        return string.Join(" + ", keys);
    }

    public bool Equals(CharacterActivator? other)
    {
        return other != null
               && string.Equals(Character, other.Character, StringComparison.Ordinal)
               && Control == other.Control
               && Alt == other.Alt
               && Meta == other.Meta
               && IncludeRepeats == other.IncludeRepeats;
    }

    public override bool Equals(object? obj) => obj is CharacterActivator other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Character, Control, Alt, Meta, IncludeRepeats);
}

public sealed class ShortcutManager : ChangeNotifier
{
    private IReadOnlyDictionary<ShortcutActivator, Intent> _shortcuts;

    public ShortcutManager(
        IReadOnlyDictionary<ShortcutActivator, Intent>? shortcuts = null,
        bool modal = false)
    {
        _shortcuts = shortcuts ?? new Dictionary<ShortcutActivator, Intent>();
        Modal = modal;
    }

    public IReadOnlyDictionary<ShortcutActivator, Intent> Shortcuts
    {
        get => _shortcuts;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (MapsEqual(_shortcuts, value))
            {
                return;
            }

            _shortcuts = value;
            NotifyListeners();
        }
    }

    public bool Modal { get; }

    public KeyEventResult HandleKeypress(BuildContext context, KeyEvent @event)
    {
        Intent? intent = Find(@event, HardwareKeyboard.Instance);
        FlutterAction? action = intent == null ? null : Actions.MaybeFind(context, intent);
        if (intent != null && action != null)
        {
            (bool enabled, object? result) = Actions.Of(context)
                .InvokeActionIfEnabled(action, intent, context);
            if (enabled)
            {
                return action.ToKeyEventResultObject(intent, result);
            }
        }

        return Modal ? KeyEventResult.SkipRemainingHandlers : KeyEventResult.Ignored;
    }

    private static bool MapsEqual(
        IReadOnlyDictionary<ShortcutActivator, Intent> first,
        IReadOnlyDictionary<ShortcutActivator, Intent> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        foreach ((ShortcutActivator activator, Intent intent) in first)
        {
            if (!second.TryGetValue(activator, out Intent? otherIntent)
                || !Equals(intent, otherIntent))
            {
                return false;
            }
        }

        return true;
    }

    private Intent? Find(KeyEvent @event, HardwareKeyboard state)
    {
        foreach ((ShortcutActivator activator, Intent intent) in _shortcuts)
        {
            IReadOnlySet<string>? triggers = activator.Triggers;
            if (triggers != null
                && !triggers.Any(
                    trigger => string.Equals(
                        LogicalKeySet.NormalizeKey(trigger),
                        LogicalKeySet.NormalizeKey(@event.Key),
                        StringComparison.Ordinal)))
            {
                continue;
            }

            if (activator.Accepts(@event, state))
            {
                return intent;
            }
        }

        return null;
    }
}

public sealed class Shortcuts : StatefulWidget
{
    public Shortcuts(
        IReadOnlyDictionary<ShortcutActivator, Intent> shortcuts,
        Widget child,
        string? debugLabel = null,
        bool includeSemantics = true,
        Key? key = null) : base(key)
    {
        ShortcutsMap = shortcuts ?? throw new ArgumentNullException(nameof(shortcuts));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        DebugLabel = debugLabel;
        IncludeSemantics = includeSemantics;
    }

    public Shortcuts(
        ShortcutManager manager,
        Widget child,
        string? debugLabel = null,
        bool includeSemantics = true,
        Key? key = null) : base(key)
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
        ShortcutsMap = new Dictionary<ShortcutActivator, Intent>();
        Child = child ?? throw new ArgumentNullException(nameof(child));
        DebugLabel = debugLabel;
        IncludeSemantics = includeSemantics;
    }

    public ShortcutManager? Manager { get; }

    public IReadOnlyDictionary<ShortcutActivator, Intent> ShortcutsMap { get; }

    public IReadOnlyDictionary<ShortcutActivator, Intent> EffectiveShortcuts =>
        Manager?.Shortcuts ?? ShortcutsMap;

    public Widget Child { get; }

    public string? DebugLabel { get; }

    public bool IncludeSemantics { get; }

    public override State CreateState() => new ShortcutsState();

    private sealed class ShortcutsState : State
    {
        private ShortcutManager? _internalManager;

        private Shortcuts Current => (Shortcuts)StateWidget;

        private ShortcutManager EffectiveManager => Current.Manager ?? _internalManager!;

        public override void InitState()
        {
            if (Current.Manager == null)
            {
                _internalManager = new ShortcutManager(Current.ShortcutsMap);
            }
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldShortcuts = (Shortcuts)oldWidget;
            if (!ReferenceEquals(oldShortcuts.Manager, Current.Manager))
            {
                if (Current.Manager != null)
                {
                    _internalManager?.Dispose();
                    _internalManager = null;
                }
                else
                {
                    _internalManager ??= new ShortcutManager();
                }
            }

            if (_internalManager != null)
            {
                _internalManager.Shortcuts = Current.ShortcutsMap;
            }
        }

        public override Widget Build(BuildContext context)
        {
            return new Focus(
                child: Current.Child,
                includeSemantics: Current.IncludeSemantics,
                canRequestFocus: false,
                skipTraversal: true,
                onKeyEvent: HandleKeyEvent);
        }

        public override void Dispose()
        {
            _internalManager?.Dispose();
        }

        private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
        {
            Element? focusedElement = FocusManager.Instance.PrimaryFocus?.AttachmentElement;
            return focusedElement == null
                ? KeyEventResult.Ignored
                : EffectiveManager.HandleKeypress(new BuildContext(focusedElement), @event);
        }
    }
}

public sealed class CallbackShortcuts : StatelessWidget
{
    public CallbackShortcuts(
        IReadOnlyDictionary<ShortcutActivator, System.Action> bindings,
        Widget child,
        Key? key = null) : base(key)
    {
        Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public IReadOnlyDictionary<ShortcutActivator, System.Action> Bindings { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        var intents = new Dictionary<ShortcutActivator, Intent>();
        var actions = new Dictionary<Type, FlutterAction>();
        foreach ((ShortcutActivator activator, System.Action callback) in Bindings)
        {
            var intent = new VoidCallbackIntent(callback);
            intents.Add(activator, intent);
        }

        actions.Add(typeof(VoidCallbackIntent), new VoidCallbackAction());
        return new Shortcuts(
            shortcuts: intents,
            child: new Actions(actions, Child));
    }
}

public sealed class ShortcutRegistryEntry : IDisposable
{
    private ShortcutRegistry? _registry;

    internal ShortcutRegistryEntry(ShortcutRegistry registry)
    {
        _registry = registry;
    }

    public void ReplaceAll(IReadOnlyDictionary<ShortcutActivator, Intent> shortcuts)
    {
        if (_registry == null)
        {
            throw new ObjectDisposedException(nameof(ShortcutRegistryEntry));
        }

        _registry.ReplaceAll(this, shortcuts);
    }

    public void Dispose()
    {
        ShortcutRegistry? registry = _registry;
        _registry = null;
        registry?.Remove(this);
    }
}

public sealed class ShortcutRegistry : ChangeNotifier
{
    private readonly Dictionary<ShortcutRegistryEntry, IReadOnlyDictionary<ShortcutActivator, Intent>> _entries = [];
    private bool _notificationScheduled;

    public IReadOnlyDictionary<ShortcutActivator, Intent> Shortcuts
    {
        get
        {
            var combined = new Dictionary<ShortcutActivator, Intent>();
            foreach (IReadOnlyDictionary<ShortcutActivator, Intent> entry in _entries.Values)
            {
                foreach ((ShortcutActivator activator, Intent intent) in entry)
                {
                    if (!combined.TryAdd(activator, intent))
                    {
                        throw new InvalidOperationException(
                            $"A shortcut for {activator.DebugDescribeKeys()} is registered more than once.");
                    }
                }
            }

            return combined;
        }
    }

    public ShortcutRegistryEntry AddAll(IReadOnlyDictionary<ShortcutActivator, Intent> shortcuts)
    {
        ArgumentNullException.ThrowIfNull(shortcuts);
        var entry = new ShortcutRegistryEntry(this);
        _entries.Add(entry, shortcuts);
        NotifyListenersNextFrame();
        return entry;
    }

    public static ShortcutRegistry Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "ShortcutRegistry.Of() requires a ShortcutRegistrar ancestor.");
    }

    public static ShortcutRegistry? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<ShortcutRegistrarScope>()?.Registry;
    }

    public override void Dispose()
    {
        _entries.Clear();
        base.Dispose();
    }

    internal void ReplaceAll(
        ShortcutRegistryEntry entry,
        IReadOnlyDictionary<ShortcutActivator, Intent> shortcuts)
    {
        ArgumentNullException.ThrowIfNull(shortcuts);
        if (!_entries.ContainsKey(entry))
        {
            throw new InvalidOperationException("The shortcut registry entry is no longer registered.");
        }

        _entries[entry] = shortcuts;
        NotifyListenersNextFrame();
    }

    internal void Remove(ShortcutRegistryEntry entry)
    {
        if (_entries.Remove(entry))
        {
            NotifyListenersNextFrame();
        }
    }

    private void NotifyListenersNextFrame()
    {
        if (_notificationScheduled)
        {
            return;
        }

        _notificationScheduled = true;
        Scheduler.AddPostFrameCallback(_ =>
        {
            _notificationScheduled = false;
            NotifyListeners();
        });
    }
}

public sealed class ShortcutRegistrar : StatefulWidget
{
    public ShortcutRegistrar(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Widget Child { get; }

    public override State CreateState() => new ShortcutRegistrarState();

    private sealed class ShortcutRegistrarState : State
    {
        private readonly ShortcutRegistry _registry = new();
        private readonly ShortcutManager _manager = new();

        private ShortcutRegistrar Current => (ShortcutRegistrar)StateWidget;

        public override void InitState()
        {
            _registry.AddListener(HandleShortcutsChanged);
            HandleShortcutsChanged();
        }

        public override Widget Build(BuildContext context)
        {
            return new Shortcuts(
                manager: _manager,
                child: new ShortcutRegistrarScope(_registry, Current.Child));
        }

        public override void Dispose()
        {
            _registry.RemoveListener(HandleShortcutsChanged);
            _registry.Dispose();
            _manager.Dispose();
        }

        private void HandleShortcutsChanged()
        {
            _manager.Shortcuts = _registry.Shortcuts;
        }
    }
}

internal sealed class ShortcutRegistrarScope : InheritedWidget
{
    public ShortcutRegistrarScope(ShortcutRegistry registry, Widget child)
    {
        Registry = registry;
        Child = child;
    }

    public ShortcutRegistry Registry { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((ShortcutRegistrarScope)oldWidget).Registry, Registry);
    }
}
