using Plumix.UI;

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
        LogicalKeyboardKey? trigger,
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

    /// <summary>The trigger key, set only by <see cref="Modifier"/>.</summary>
    public LogicalKeyboardKey? Trigger { get; }

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
        LogicalKeyboardKey trigger,
        bool alt = false,
        bool control = false,
        bool meta = false,
        bool shift = false)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        if (SingleActivator.IsModifierKey(trigger))
        {
            throw new ArgumentException(
                "Specifying a modifier key as a trigger is not allowed. "
                + "Use the provided boolean parameters instead.",
                nameof(trigger));
        }

        return new ShortcutSerialization(
            trigger: trigger,
            character: null,
            alt: alt,
            control: control,
            meta: meta,
            shift: shift,
            serialized: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [ShortcutTriggerKey] = trigger.KeyId,
                [ShortcutModifiersKey] = (alt ? ShortcutModifierAlt : 0)
                                         | (control ? ShortcutModifierControl : 0)
                                         | (meta ? ShortcutModifierMeta : 0)
                                         | (shift ? ShortcutModifierShift : 0)
            });
    }

    /// <summary>Flutter's `toChannelRepresentation`; returns the backing map, as Dart does.</summary>
    public IReadOnlyDictionary<string, object?> ToChannelRepresentation() => _internal;
}
