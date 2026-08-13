namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/platform_menu_bar.dart

/// <summary>
/// Flutter's `MenuSerializableShortcut` mixin: a <see cref="ShortcutActivator"/> that can describe
/// itself to a platform menu channel. C# has no mixins, so it is an interface that
/// <see cref="SingleActivator"/> and <see cref="CharacterActivator"/> implement directly.
/// </summary>
public interface IMenuSerializableShortcut : ShortcutActivator
{
    ShortcutSerialization SerializeForMenu();
}

/// <summary>
/// Flutter's `ShortcutSerialization`: a platform-channel description of an activator.
/// </summary>
/// <remarks>
/// Plumix models logical keys as normalized key strings rather than Flutter's numeric
/// `LogicalKeyboardKey.keyId`, so <see cref="Trigger"/> and the `shortcutTrigger` channel entry
/// carry that string. See the keyboard-events row in `docs/ai/DIVERGENCES.md`.
/// </remarks>
public sealed class ShortcutSerialization
{
    private const string ShortcutCharacterKey = "shortcutCharacter";
    private const string ShortcutTriggerKey = "shortcutTrigger";
    private const string ShortcutModifiersKey = "shortcutModifiers";

    private const int ShortcutModifierMeta = 1 << 0;
    private const int ShortcutModifierShift = 1 << 1;
    private const int ShortcutModifierAlt = 1 << 2;
    private const int ShortcutModifierControl = 1 << 3;

    private readonly Dictionary<string, object?> _internal;

    private ShortcutSerialization(
        string? trigger,
        string? character,
        bool? alt,
        bool? control,
        bool? meta,
        bool? shift,
        Dictionary<string, object?> serialized)
    {
        Trigger = trigger;
        Character = character;
        Alt = alt;
        Control = control;
        Meta = meta;
        Shift = shift;
        _internal = serialized;
    }

    /// <summary>The normalized trigger key, set only by <see cref="Modifier"/>.</summary>
    public string? Trigger { get; }

    /// <summary>The literal character, set only by <see cref="Character"/>.</summary>
    public string? Character { get; }

    public bool? Alt { get; }

    public bool? Control { get; }

    public bool? Meta { get; }

    /// <summary>Always <see langword="null"/> for a character serialization: the character encodes shift.</summary>
    public bool? Shift { get; }

    /// <summary>Flutter's `ShortcutSerialization.character`.</summary>
    public static ShortcutSerialization ForCharacter(
        string character,
        bool alt = false,
        bool control = false,
        bool meta = false)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (character.Length != 1)
        {
            throw new ArgumentException(
                "A character shortcut must be exactly one character long.",
                nameof(character));
        }

        return new ShortcutSerialization(
            trigger: null,
            character: character,
            alt: alt,
            control: control,
            meta: meta,
            shift: null,
            serialized: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [ShortcutCharacterKey] = character,
                [ShortcutModifiersKey] = (control ? ShortcutModifierControl : 0)
                                         | (alt ? ShortcutModifierAlt : 0)
                                         | (meta ? ShortcutModifierMeta : 0)
            });
    }

    /// <summary>Flutter's `ShortcutSerialization.modifier`.</summary>
    public static ShortcutSerialization Modifier(
        string trigger,
        bool alt = false,
        bool control = false,
        bool meta = false,
        bool shift = false)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        string normalized = LogicalKeySet.NormalizeKey(trigger);
        if (normalized is "Alt" or "Control" or "Meta" or "Shift")
        {
            throw new ArgumentException(
                "Specifying a modifier key as a trigger is not allowed. "
                + "Use the provided boolean parameters instead.",
                nameof(trigger));
        }

        return new ShortcutSerialization(
            trigger: normalized,
            character: null,
            alt: alt,
            control: control,
            meta: meta,
            shift: shift,
            serialized: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [ShortcutTriggerKey] = normalized,
                [ShortcutModifiersKey] = (alt ? ShortcutModifierAlt : 0)
                                         | (control ? ShortcutModifierControl : 0)
                                         | (meta ? ShortcutModifierMeta : 0)
                                         | (shift ? ShortcutModifierShift : 0)
            });
    }

    /// <summary>Flutter's `toChannelRepresentation`; returns the backing map, as Dart does.</summary>
    public IReadOnlyDictionary<string, object?> ToChannelRepresentation() => _internal;
}
