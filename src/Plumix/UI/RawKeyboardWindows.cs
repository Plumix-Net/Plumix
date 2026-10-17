// Dart parity source: flutter/packages/flutter/lib/src/services/raw_keyboard_windows.dart

using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.UI;

/// <summary>
/// Platform-specific key event data for Windows, obtained from the Windows win32 API.
/// </summary>
[Obsolete(
    "Platform specific key event data is no longer available. See KeyEvent for what is available. "
    + "Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public class RawKeyEventDataWindows : RawKeyEventData
{
    // These are not the values defined by the Windows header for each modifier. Since they
    // can't be packaged into a single int, we are re-defining them here to reduce the size
    // of the message from the embedder. Embedders should map these values to the native key codes.
    // Keep this in sync with the Flutter engine's
    // shell/platform/windows/keyboard_key_channel_handler.cc.

    /// <summary>Mask on <see cref="Modifiers"/> for whether a SHIFT key is pressed.</summary>
    public const int ModifierShift = 1 << 0;

    /// <summary>Mask on <see cref="Modifiers"/> for whether the left SHIFT key is pressed.</summary>
    public const int ModifierLeftShift = 1 << 1;

    /// <summary>Mask on <see cref="Modifiers"/> for whether the right SHIFT key is pressed.</summary>
    public const int ModifierRightShift = 1 << 2;

    /// <summary>Mask on <see cref="Modifiers"/> for whether a CTRL key is pressed.</summary>
    public const int ModifierControl = 1 << 3;

    /// <summary>Mask on <see cref="Modifiers"/> for whether the left CTRL key is pressed.</summary>
    public const int ModifierLeftControl = 1 << 4;

    /// <summary>Mask on <see cref="Modifiers"/> for whether the right CTRL key is pressed.</summary>
    public const int ModifierRightControl = 1 << 5;

    /// <summary>Mask on <see cref="Modifiers"/> for whether an ALT key is pressed.</summary>
    public const int ModifierAlt = 1 << 6;

    /// <summary>Mask on <see cref="Modifiers"/> for whether the left ALT key is pressed.</summary>
    public const int ModifierLeftAlt = 1 << 7;

    /// <summary>Mask on <see cref="Modifiers"/> for whether the right ALT key is pressed.</summary>
    public const int ModifierRightAlt = 1 << 8;

    /// <summary>Mask on <see cref="Modifiers"/> for whether the left WIN key is pressed.</summary>
    public const int ModifierLeftMeta = 1 << 9;

    /// <summary>Mask on <see cref="Modifiers"/> for whether the right WIN key is pressed.</summary>
    public const int ModifierRightMeta = 1 << 10;

    /// <summary>Mask on <see cref="Modifiers"/> for whether the CAPS LOCK key is pressed.</summary>
    public const int ModifierCaps = 1 << 11;

    /// <summary>Mask on <see cref="Modifiers"/> for whether the NUM LOCK key is pressed.</summary>
    public const int ModifierNumLock = 1 << 12;

    /// <summary>Mask on <see cref="Modifiers"/> for whether the SCROLL LOCK key is pressed.</summary>
    public const int ModifierScrollLock = 1 << 13;

    // Virtual key VK_PROCESSKEY in Win32 API.
    //
    // Key down events related to IME operations use this as keyCode.
    // Dart declares it file-private at library level (`_vkProcessKey`).
    private const int VkProcessKey = 0xe5;

    /// <summary>Creates a key event data structure specific for Windows.</summary>
    [Obsolete(
        "Platform specific key event data is no longer available. See KeyEvent for what is available. "
        + "Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
    public RawKeyEventDataWindows(int keyCode = 0, int scanCode = 0, int characterCodePoint = 0, int modifiers = 0)
    {
        KeyCode = keyCode;
        ScanCode = scanCode;
        CharacterCodePoint = characterCodePoint;
        Modifiers = modifiers;
    }

    /// <summary>The hardware key code corresponding to this key event.</summary>
    public int KeyCode { get; }

    /// <summary>The hardware scan code id corresponding to this key event.</summary>
    public int ScanCode { get; }

    /// <summary>The Unicode code point represented by the key event, if any; zero otherwise.</summary>
    public int CharacterCodePoint { get; }

    /// <summary>A mask of the current modifiers, in sync with the Windows embedder.</summary>
    public int Modifiers { get; }

    /// <inheritdoc />
    public override string KeyLabel => CharacterCodePoint == 0 ? string.Empty : FromCharCode(CharacterCodePoint);

    /// <inheritdoc />
    public override PhysicalKeyboardKey PhysicalKey =>
        KeyboardMaps.KWindowsToPhysicalKey.GetValueOrDefault(ScanCode)
        ?? new PhysicalKeyboardKey(LogicalKeyboardKey.WindowsPlane + ScanCode);

    /// <inheritdoc />
    public override LogicalKeyboardKey LogicalKey
    {
        get
        {
            // Look to see if the keyCode is a printable number pad key, so that a
            // difference between regular keys (e.g. "=") and the number pad version
            // (e.g. the "=" on the number pad) can be determined.
            LogicalKeyboardKey? numPadKey = KeyboardMaps.KWindowsNumPadMap.GetValueOrDefault(KeyCode);
            if (numPadKey != null)
            {
                return numPadKey;
            }

            // If it has a non-control-character label, then either return the existing
            // constant, or construct a new Unicode-based key from it. Don't mark it as
            // autogenerated, since the label uniquely identifies an ID from the Unicode
            // plane.
            if (KeyLabel.Length != 0 && !LogicalKeyboardKey.IsControlCharacter(KeyLabel))
            {
                long keyId = LogicalKeyboardKey.UnicodePlane | (CharacterCodePoint & LogicalKeyboardKey.ValueMask);
                return LogicalKeyboardKey.FindKeyByKeyId(keyId) ?? new LogicalKeyboardKey(keyId);
            }

            // Look to see if the keyCode is one we know about and have a mapping for.
            LogicalKeyboardKey? newKey = KeyboardMaps.KWindowsToLogicalKey.GetValueOrDefault(KeyCode);
            if (newKey != null)
            {
                return newKey;
            }

            // This is a non-printable key that we don't know about, so we mint a new
            // code.
            return new LogicalKeyboardKey((long)KeyCode | LogicalKeyboardKey.WindowsPlane);
        }
    }

    private bool IsLeftRightModifierPressed(KeyboardSide side, int anyMask, int leftMask, int rightMask)
    {
        if ((Modifiers & (leftMask | rightMask | anyMask)) == 0)
        {
            return false;
        }

        if ((Modifiers & (leftMask | rightMask | anyMask)) == anyMask)
        {
            // If only the "anyMask" bit is set, then we respond true for requests of
            // whether either left or right is pressed. Handles the case where Windows
            // supplies just the "either" modifier flag, but not the left/right flag.
            // (e.g. modifierShift but not modifierLeftShift).
            return true;
        }

        return side switch
        {
            KeyboardSide.Any => true,
            KeyboardSide.All => (Modifiers & leftMask) != 0 && (Modifiers & rightMask) != 0,
            KeyboardSide.Left => (Modifiers & leftMask) != 0,
            KeyboardSide.Right => (Modifiers & rightMask) != 0,
            _ => throw new ArgumentOutOfRangeException(nameof(side)),
        };
    }

    /// <inheritdoc />
    public override bool IsModifierPressed(ModifierKey key, KeyboardSide side = KeyboardSide.Any)
    {
        bool result;
        switch (key)
        {
            case ModifierKey.ControlModifier:
                result = IsLeftRightModifierPressed(
                    side,
                    ModifierControl,
                    ModifierLeftControl,
                    ModifierRightControl);
                break;
            case ModifierKey.ShiftModifier:
                result = IsLeftRightModifierPressed(
                    side,
                    ModifierShift,
                    ModifierLeftShift,
                    ModifierRightShift);
                break;
            case ModifierKey.AltModifier:
                result = IsLeftRightModifierPressed(side, ModifierAlt, ModifierLeftAlt, ModifierRightAlt);
                break;
            case ModifierKey.MetaModifier:
                // Windows does not provide an "any" key for win key press.
                result = IsLeftRightModifierPressed(
                    side,
                    ModifierLeftMeta | ModifierRightMeta,
                    ModifierLeftMeta,
                    ModifierRightMeta);
                break;
            case ModifierKey.CapsLockModifier:
                result = (Modifiers & ModifierCaps) != 0;
                break;
            case ModifierKey.ScrollLockModifier:
                result = (Modifiers & ModifierScrollLock) != 0;
                break;
            case ModifierKey.NumLockModifier:
                result = (Modifiers & ModifierNumLock) != 0;
                break;
            // The OS does not expose the Fn key to the drivers, it doesn't generate a key message.
            case ModifierKey.FunctionModifier:
            case ModifierKey.SymbolModifier:
                // These modifier masks are not used in Windows keyboards.
                result = false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(key));
        }

        if (Constants.KDebugMode)
        {
            DebugAssertions.Assert(
                !result || GetModifierSide(key) != null,
                $"{GetType().Name} thinks that a modifier is pressed, but can't figure out what side it's on.");
        }

        return result;
    }

    /// <inheritdoc />
    public override KeyboardSide? GetModifierSide(ModifierKey key)
    {
        KeyboardSide? FindSide(int leftMask, int rightMask, int anyMask)
        {
            int combinedMask = leftMask | rightMask;
            int combined = Modifiers & combinedMask;
            if (combined == leftMask)
            {
                return KeyboardSide.Left;
            }
            else if (combined == rightMask)
            {
                return KeyboardSide.Right;
            }
            else if (combined == combinedMask || (Modifiers & (combinedMask | anyMask)) == anyMask)
            {
                // Handles the case where Windows supplies just the "either" modifier
                // flag, but not the left/right flag. (e.g. modifierShift but not
                // modifierLeftShift).
                return KeyboardSide.All;
            }

            return null;
        }

        switch (key)
        {
            case ModifierKey.ControlModifier:
                return FindSide(ModifierLeftControl, ModifierRightControl, ModifierControl);
            case ModifierKey.ShiftModifier:
                return FindSide(ModifierLeftShift, ModifierRightShift, ModifierShift);
            case ModifierKey.AltModifier:
                return FindSide(ModifierLeftAlt, ModifierRightAlt, ModifierAlt);
            case ModifierKey.MetaModifier:
                return FindSide(ModifierLeftMeta, ModifierRightMeta, 0);
            case ModifierKey.CapsLockModifier:
            case ModifierKey.NumLockModifier:
            case ModifierKey.ScrollLockModifier:
            case ModifierKey.FunctionModifier:
            case ModifierKey.SymbolModifier:
                return KeyboardSide.All;
            default:
                throw new ArgumentOutOfRangeException(nameof(key));
        }
    }

    /// <inheritdoc />
    public override bool ShouldDispatchEvent()
    {
        // In Win32 API, down events related to IME operations use VK_PROCESSKEY as
        // keyCode. This event, as well as the following key up event (which uses a
        // normal keyCode), should be skipped, because the effect of IME operations
        // will be handled by the text input API.
        return KeyCode != VkProcessKey;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<int>("keyCode", KeyCode));
        properties.Add(new DiagnosticsProperty<int>("scanCode", ScanCode));
        properties.Add(new DiagnosticsProperty<int>("characterCodePoint", CharacterCodePoint));
        properties.Add(new DiagnosticsProperty<int>("modifiers", Modifiers));
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

        return obj is RawKeyEventDataWindows other
            && other.KeyCode == KeyCode
            && other.ScanCode == ScanCode
            && other.CharacterCodePoint == CharacterCodePoint
            && other.Modifiers == Modifiers;
    }

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(KeyCode, ScanCode, CharacterCodePoint, Modifiers);

    /// <summary>Dart's <c>String.fromCharCode</c>: one UTF-16 code unit, or a surrogate pair above the BMP.</summary>
    private static string FromCharCode(int charCode) =>
        (uint)charCode <= 0xFFFF ? ((char)charCode).ToString() : char.ConvertFromUtf32(charCode);
}
