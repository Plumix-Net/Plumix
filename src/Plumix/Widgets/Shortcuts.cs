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
    /// <summary>
    /// All the keys that might be the final event to trigger this shortcut, or null if every key
    /// should be considered (for example <see cref="CharacterActivator"/>).
    /// </summary>
    IReadOnlySet<LogicalKeyboardKey>? Triggers { get; }

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

/// <summary>
/// A set of <see cref="LogicalKeyboardKey"/>s that can be used as a <see cref="ShortcutActivator"/>.
/// </summary>
public sealed class LogicalKeySet : KeySet<LogicalKeyboardKey>, ShortcutActivator, IEquatable<LogicalKeySet>
{
    private IReadOnlySet<LogicalKeyboardKey>? _triggers;

    public LogicalKeySet(
        LogicalKeyboardKey key1,
        LogicalKeyboardKey? key2 = null,
        LogicalKeyboardKey? key3 = null,
        LogicalKeyboardKey? key4 = null)
        : base(
            key1 ?? throw new ArgumentNullException(nameof(key1)),
            key2,
            key3,
            key4)
    {
    }

    public LogicalKeySet(IReadOnlySet<LogicalKeyboardKey> keys) : base(keys)
    {
    }

    public IReadOnlySet<LogicalKeyboardKey> Triggers => _triggers ??= BuildTriggers();

    public bool Accepts(KeyEvent @event, HardwareKeyboard state)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(state);
        if (@event is not (KeyDownEvent or KeyRepeatEvent))
        {
            return false;
        }

        return Triggers.Contains(@event.LogicalKey) && CheckKeyRequirements(state.LogicalKeysPressed);
    }

    public string DebugDescribeKeys()
    {
        List<LogicalKeyboardKey> sortedKeys = [.. InternalKeys];
        sortedKeys.Sort((a, b) =>
        {
            // Put the modifiers first. If it has a synonym, then it is something like shiftLeft.
            bool aIsModifier = a.Synonyms.Count > 0 || Modifiers.Contains(a);
            bool bIsModifier = b.Synonyms.Count > 0 || Modifiers.Contains(b);
            if (aIsModifier && !bIsModifier)
            {
                return -1;
            }

            if (bIsModifier && !aIsModifier)
            {
                return 1;
            }

            return string.CompareOrdinal(a.DebugName, b.DebugName);
        });

        return string.Join(" + ", sortedKeys.Select(key => key.DebugName));
    }

    bool IEquatable<LogicalKeySet>.Equals(LogicalKeySet? other) => Equals(other);

    private bool CheckKeyRequirements(IReadOnlySet<LogicalKeyboardKey> pressed)
    {
        IReadOnlySet<LogicalKeyboardKey> collapsedRequired = LogicalKeyboardKey.CollapseSynonyms(InternalKeys);
        IReadOnlySet<LogicalKeyboardKey> collapsedPressed = LogicalKeyboardKey.CollapseSynonyms(pressed);
        return collapsedRequired.Count == collapsedPressed.Count
               && !collapsedRequired.Except(collapsedPressed).Any();
    }

    private IReadOnlySet<LogicalKeyboardKey> BuildTriggers()
    {
        var result = new HashSet<LogicalKeyboardKey>();
        foreach (LogicalKeyboardKey key in InternalKeys)
        {
            if (UnmapSynonyms.TryGetValue(key, out IReadOnlyList<LogicalKeyboardKey>? unmapped))
            {
                result.UnionWith(unmapped);
            }
            else
            {
                result.Add(key);
            }
        }

        return result;
    }

    private static readonly HashSet<LogicalKeyboardKey> Modifiers =
    [
        LogicalKeyboardKey.Alt,
        LogicalKeyboardKey.Control,
        LogicalKeyboardKey.Meta,
        LogicalKeyboardKey.Shift,
    ];

    private static readonly Dictionary<LogicalKeyboardKey, IReadOnlyList<LogicalKeyboardKey>> UnmapSynonyms = new()
    {
        [LogicalKeyboardKey.Control] = [LogicalKeyboardKey.ControlLeft, LogicalKeyboardKey.ControlRight],
        [LogicalKeyboardKey.Shift] = [LogicalKeyboardKey.ShiftLeft, LogicalKeyboardKey.ShiftRight],
        [LogicalKeyboardKey.Alt] = [LogicalKeyboardKey.AltLeft, LogicalKeyboardKey.AltRight],
        [LogicalKeyboardKey.Meta] = [LogicalKeyboardKey.MetaLeft, LogicalKeyboardKey.MetaRight],
    };
}

/// <summary>
/// A shortcut key combination of a single key and modifiers.
/// </summary>
public sealed class SingleActivator : IMenuSerializableShortcut, IEquatable<SingleActivator>
{
    public SingleActivator(
        LogicalKeyboardKey trigger,
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

    public LogicalKeyboardKey Trigger { get; }

    public bool Control { get; }

    public bool Shift { get; }

    public bool Alt { get; }

    public bool Meta { get; }

    public bool IncludeRepeats { get; }

    public LockState NumLock { get; }

    public IReadOnlySet<LogicalKeyboardKey> Triggers => new HashSet<LogicalKeyboardKey> { Trigger };

    public bool Accepts(KeyEvent @event, HardwareKeyboard state)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(state);
        return (@event is KeyDownEvent || (IncludeRepeats && @event is KeyRepeatEvent))
               && Trigger.Equals(@event.LogicalKey)
               && ShouldAcceptModifiers(state.LogicalKeysPressed)
               && ShouldAcceptNumLock(state);
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

        keys.Add(Trigger.DebugName);
        return string.Join(" + ", keys);
    }

    /// <summary>Flutter's `SingleActivator.serializeForMenu`; `numLock`/`includeRepeats` are not serialized.</summary>
    public ShortcutSerialization SerializeForMenu()
    {
        return ShortcutSerialization.Modifier(
            Trigger,
            shift: Shift,
            alt: Alt,
            meta: Meta,
            control: Control);
    }

    public bool Equals(SingleActivator? other)
    {
        return other != null
               && Trigger.Equals(other.Trigger)
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

    internal static bool IsModifierKey(LogicalKeyboardKey key)
    {
        return ModifierTriggers.Contains(key);
    }

    private bool ShouldAcceptModifiers(IReadOnlySet<LogicalKeyboardKey> pressed)
    {
        return Control == pressed.Overlaps(ActivatorModifiers.ControlSynonyms)
               && Shift == pressed.Overlaps(ActivatorModifiers.ShiftSynonyms)
               && Alt == pressed.Overlaps(ActivatorModifiers.AltSynonyms)
               && Meta == pressed.Overlaps(ActivatorModifiers.MetaSynonyms);
    }

    private bool ShouldAcceptNumLock(HardwareKeyboard state)
    {
        return NumLock switch
        {
            LockState.Locked => state.LockModesEnabled.Contains(KeyboardLockMode.NumLock),
            LockState.Unlocked => !state.LockModesEnabled.Contains(KeyboardLockMode.NumLock),
            _ => true,
        };
    }

    private static readonly HashSet<LogicalKeyboardKey> ModifierTriggers =
    [
        LogicalKeyboardKey.Control,
        LogicalKeyboardKey.ControlLeft,
        LogicalKeyboardKey.ControlRight,
        LogicalKeyboardKey.Shift,
        LogicalKeyboardKey.ShiftLeft,
        LogicalKeyboardKey.ShiftRight,
        LogicalKeyboardKey.Alt,
        LogicalKeyboardKey.AltLeft,
        LogicalKeyboardKey.AltRight,
        LogicalKeyboardKey.Meta,
        LogicalKeyboardKey.MetaLeft,
        LogicalKeyboardKey.MetaRight,
    ];
}

/// <summary>Dart's `_controlSynonyms` and friends from `shortcuts.dart`.</summary>
internal static class ActivatorModifiers
{
    public static readonly HashSet<LogicalKeyboardKey> ControlSynonyms =
        [.. LogicalKeyboardKey.ExpandSynonyms(new HashSet<LogicalKeyboardKey> { LogicalKeyboardKey.Control })];

    public static readonly HashSet<LogicalKeyboardKey> ShiftSynonyms =
        [.. LogicalKeyboardKey.ExpandSynonyms(new HashSet<LogicalKeyboardKey> { LogicalKeyboardKey.Shift })];

    public static readonly HashSet<LogicalKeyboardKey> AltSynonyms =
        [.. LogicalKeyboardKey.ExpandSynonyms(new HashSet<LogicalKeyboardKey> { LogicalKeyboardKey.Alt })];

    public static readonly HashSet<LogicalKeyboardKey> MetaSynonyms =
        [.. LogicalKeyboardKey.ExpandSynonyms(new HashSet<LogicalKeyboardKey> { LogicalKeyboardKey.Meta })];
}

/// <summary>
/// A shortcut combination that is triggered by a key event producing a specific character.
/// </summary>
public sealed class CharacterActivator : IMenuSerializableShortcut, IEquatable<CharacterActivator>
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

    public IReadOnlySet<LogicalKeyboardKey>? Triggers => null;

    public bool Accepts(KeyEvent @event, HardwareKeyboard state)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(state);

        // Does not look for shift, since the character will encode that.
        return string.Equals(@event.Character, Character, StringComparison.Ordinal)
               && (@event is KeyDownEvent || (IncludeRepeats && @event is KeyRepeatEvent))
               && Control == state.LogicalKeysPressed.Overlaps(ActivatorModifiers.ControlSynonyms)
               && Alt == state.LogicalKeysPressed.Overlaps(ActivatorModifiers.AltSynonyms)
               && Meta == state.LogicalKeysPressed.Overlaps(ActivatorModifiers.MetaSynonyms);
    }

    public string DebugDescribeKeys()
    {
        var keys = new List<string>();
        if (Alt)
        {
            keys.Add("Alt");
        }

        if (Control)
        {
            keys.Add("Control");
        }

        if (Meta)
        {
            keys.Add("Meta");
        }

        keys.Add($"'{Character}'");
        return string.Join(" + ", keys);
    }

    /// <summary>Flutter's `CharacterActivator.serializeForMenu`.</summary>
    public ShortcutSerialization SerializeForMenu()
    {
        return ShortcutSerialization.ForCharacter(
            Character,
            alt: Alt,
            control: Control,
            meta: Meta);
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

public class ShortcutManager : ChangeNotifier
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
            _indexedShortcutsCache = null;
            NotifyListeners();
        }
    }

    public bool Modal { get; }

    public virtual KeyEventResult HandleKeypress(BuildContext context, KeyEvent @event)
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

    private IndexedShortcutMap? _indexedShortcutsCache;

    private IndexedShortcutMap IndexedShortcuts => _indexedShortcutsCache ??= IndexShortcuts(_shortcuts);

    /// <summary>
    /// Dart keys `_indexedShortcuts` by a nullable `LogicalKeyboardKey`; .NET dictionaries reject a
    /// null key, so the untriggered activators live in their own list.
    /// </summary>
    private sealed record IndexedShortcutMap(
        Dictionary<LogicalKeyboardKey, List<(ShortcutActivator Activator, Intent Intent)>> ByTrigger,
        List<(ShortcutActivator Activator, Intent Intent)> WithoutTrigger);

    private static IndexedShortcutMap IndexShortcuts(IReadOnlyDictionary<ShortcutActivator, Intent> source)
    {
        var byTrigger = new Dictionary<LogicalKeyboardKey, List<(ShortcutActivator, Intent)>>();
        var withoutTrigger = new List<(ShortcutActivator, Intent)>();
        foreach ((ShortcutActivator activator, Intent intent) in source)
        {
            IReadOnlySet<LogicalKeyboardKey>? triggers = activator.Triggers;
            if (triggers == null)
            {
                withoutTrigger.Add((activator, intent));
                continue;
            }

            foreach (LogicalKeyboardKey trigger in triggers)
            {
                if (!byTrigger.TryGetValue(trigger, out List<(ShortcutActivator, Intent)>? pairs))
                {
                    pairs = [];
                    byTrigger[trigger] = pairs;
                }

                pairs.Add((activator, intent));
            }
        }

        return new IndexedShortcutMap(byTrigger, withoutTrigger);
    }

    private Intent? Find(KeyEvent @event, HardwareKeyboard state)
    {
        IndexedShortcutMap indexed = IndexedShortcuts;
        List<(ShortcutActivator Activator, Intent Intent)> candidates =
        [
            .. indexed.ByTrigger.GetValueOrDefault(@event.LogicalKey) ?? [],
            .. indexed.WithoutTrigger,
        ];

        foreach ((ShortcutActivator activator, Intent intent) in candidates)
        {
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
