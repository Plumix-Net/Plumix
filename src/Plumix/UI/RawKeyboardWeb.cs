// Dart parity source: flutter/packages/flutter/lib/src/services/raw_keyboard_web.dart

using Plumix.Foundation;

namespace Plumix.UI;

/// <summary>Platform-specific key event data for Web.</summary>
[Obsolete(
    "Platform specific key event data is no longer available. See KeyEvent for what is available. "
    + "Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public class RawKeyEventDataWeb : RawKeyEventData
{
    /// <summary>No modifier keys are pressed in the <see cref="MetaState"/> field.</summary>
    public const int ModifierNone = 0;

    /// <summary>Mask on <see cref="MetaState"/> for whether a SHIFT key is pressed.</summary>
    public const int ModifierShift = 0x01;

    /// <summary>Mask on <see cref="MetaState"/> for whether an ALT key is pressed.</summary>
    public const int ModifierAlt = 0x02;

    /// <summary>Mask on <see cref="MetaState"/> for whether a CTRL key is pressed.</summary>
    public const int ModifierControl = 0x04;

    /// <summary>Mask on <see cref="MetaState"/> for whether a META key is pressed.</summary>
    public const int ModifierMeta = 0x08;

    /// <summary>Mask on <see cref="MetaState"/> for whether NUM LOCK is on.</summary>
    public const int ModifierNumLock = 0x10;

    /// <summary>Mask on <see cref="MetaState"/> for whether CAPS LOCK is on.</summary>
    public const int ModifierCapsLock = 0x20;

    /// <summary>Mask on <see cref="MetaState"/> for whether SCROLL LOCK is on.</summary>
    public const int ModifierScrollLock = 0x40;

    /// <summary>Creates a key event data structure specific for Web.</summary>
    [Obsolete(
        "Platform specific key event data is no longer available. See KeyEvent for what is available. "
        + "Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
    public RawKeyEventDataWeb(
        string code,
        string key,
        int location = 0,
        int metaState = ModifierNone,
        int keyCode = 0)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(key);
        Code = code;
        Key = key;
        Location = location;
        MetaState = metaState;
        KeyCode = keyCode;
    }

    /// <summary>The <c>KeyboardEvent.code</c> corresponding to this event (the physical key).</summary>
    public string Code { get; }

    /// <summary>The <c>KeyboardEvent.key</c> corresponding to this event (layout and modifier aware).</summary>
    public string Key { get; }

    /// <summary>The <c>KeyboardEvent.location</c> corresponding to this event.</summary>
    public int Location { get; }

    /// <summary>The modifiers that were present when the key event occurred.</summary>
    public int MetaState { get; }

    /// <summary>The <c>KeyboardEvent.keyCode</c> corresponding to this event.</summary>
    public int KeyCode { get; }

    /// <inheritdoc />
    public override string KeyLabel => Key == "Unidentified" ? string.Empty : UnicodeChar(Key) ?? string.Empty;

    /// <inheritdoc />
    public override PhysicalKeyboardKey PhysicalKey
    {
        get
        {
            return KeyboardMaps.KWebToPhysicalKey.GetValueOrDefault(Code)
                ?? new PhysicalKeyboardKey(LogicalKeyboardKey.WebPlane + DartStringHashCode(Code));
        }
    }

    /// <inheritdoc />
    public override LogicalKeyboardKey LogicalKey
    {
        get
        {
            // Look to see if the keyCode is a key based on location. Typically they are
            // numpad keys (versus main area keys) and left/right modifiers.
            LogicalKeyboardKey? maybeLocationKey =
                KeyboardMaps.KWebLocationMap.GetValueOrDefault(Key)?[Location];
            if (maybeLocationKey != null)
            {
                return maybeLocationKey;
            }

            // Look to see if the [key] is one we know about and have a mapping for.
            LogicalKeyboardKey? newKey = KeyboardMaps.KWebToLogicalKey.GetValueOrDefault(Key);
            if (newKey != null)
            {
                return newKey;
            }

            bool isPrintable = Key.Length == 1;
            if (isPrintable)
            {
                return new LogicalKeyboardKey(Key.ToLowerInvariant()[0]);
            }

            // This is a non-printable key that we don't know about, so we mint a new
            // key from `code`. Don't mint with `key`, because the `key` will always be
            // "Unidentified" .
            return new LogicalKeyboardKey(DartStringHashCode(Code) + LogicalKeyboardKey.WebPlane);
        }
    }

    /// <inheritdoc />
    public override bool IsModifierPressed(ModifierKey key, KeyboardSide side = KeyboardSide.Any)
    {
        return key switch
        {
            ModifierKey.ControlModifier => (MetaState & ModifierControl) != 0,
            ModifierKey.ShiftModifier => (MetaState & ModifierShift) != 0,
            ModifierKey.AltModifier => (MetaState & ModifierAlt) != 0,
            ModifierKey.MetaModifier => (MetaState & ModifierMeta) != 0,
            ModifierKey.NumLockModifier => (MetaState & ModifierNumLock) != 0,
            ModifierKey.CapsLockModifier => (MetaState & ModifierCapsLock) != 0,
            ModifierKey.ScrollLockModifier => (MetaState & ModifierScrollLock) != 0,
            // On Web, the browser doesn't report the state of the FN and SYM modifiers.
            ModifierKey.FunctionModifier or ModifierKey.SymbolModifier => false,
            _ => throw new ArgumentOutOfRangeException(nameof(key)),
        };
    }

    /// <inheritdoc />
    public override KeyboardSide? GetModifierSide(ModifierKey key)
    {
        // On Web, we don't distinguish the sides of modifier keys. Both left shift
        // and right shift, for example, are reported as the "Shift" modifier.
        //
        // See <https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/getModifierState>
        // for more information.
        return KeyboardSide.Any;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<string>("code", Code));
        properties.Add(new DiagnosticsProperty<string>("key", Key));
        properties.Add(new DiagnosticsProperty<int>("location", Location));
        properties.Add(new DiagnosticsProperty<int>("metaState", MetaState));
        properties.Add(new DiagnosticsProperty<int>("keyCode", KeyCode));
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        return obj is RawKeyEventDataWeb other
            && other.Code == Code
            && other.Key == Key
            && other.Location == Location
            && other.MetaState == MetaState
            && other.KeyCode == KeyCode;
    }

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Code, Key, Location, MetaState, KeyCode);

    /// <summary>
    /// C#-only stand-in for Dart's <c>String.hashCode</c>, which mints the fallback web-plane ids.
    /// </summary>
    /// <remarks>
    /// .NET randomizes <see cref="string.GetHashCode()"/> per process, so this reimplements the Dart
    /// VM's string hash instead: Jenkins one-at-a-time over the UTF-16 code units, finalized to the
    /// VM's 30-bit <c>String::kHashBits</c>, with 0 mapped to 1 (runtime/vm/hash.h). The result is
    /// non-negative and deterministic, and equals the Dart VM's value (checked against Dart 3.13, e.g.
    /// <c>'Unrecog1'.hashCode == 484762455</c>); dart2js/dart2wasm hash strings differently, so on the
    /// web the minted ids differ from Flutter web's.
    /// </remarks>
    private static int DartStringHashCode(string value)
    {
        uint hash = 0;
        foreach (char codeUnit in value)
        {
            hash += codeUnit;
            hash += hash << 10;
            hash ^= hash >> 6;
        }

        hash += hash << 3;
        hash ^= hash >> 11;
        hash += hash << 15;
        hash &= (1u << 30) - 1;
        return hash == 0 ? 1 : (int)hash;
    }

    /// <summary>Dart's file-private <c>_unicodeChar</c>: the key itself when it is one UTF-16 code unit.</summary>
    private static string? UnicodeChar(string key)
    {
        if (key.Length == 1)
        {
            return key.Substring(0, 1);
        }

        return null;
    }
}
