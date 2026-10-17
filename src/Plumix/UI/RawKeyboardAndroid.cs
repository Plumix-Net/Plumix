// Dart parity source: flutter/packages/flutter/lib/src/services/raw_keyboard_android.dart

using Plumix.Foundation;

namespace Plumix.UI;

/// <summary>
/// Platform-specific key event data for Android, obtained from Android's <c>KeyEvent</c> interface.
/// </summary>
/// <remarks>
/// This class is deprecated and will be removed. Platform specific key event data will no longer
/// be available. See <c>KeyEvent</c> for what is available.
/// </remarks>
[Obsolete(
    "Platform specific key event data is no longer available. See KeyEvent for what is available. "
    + "Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public class RawKeyEventDataAndroid : RawKeyEventData
{
    // Android sets the 0x80000000 bit on a character to indicate that it is a combining character,
    // so we use this mask to remove that bit to make it a valid Unicode character again.
    private const int KCombiningCharacterMask = 0x7fffffff;

    // The source code that indicates that an event came from a joystick.
    // from https://developer.android.com/reference/android/view/InputDevice.html#SOURCE_JOYSTICK
    private const int SourceJoystick = 0x01000010;

    /// <summary>Creates a key event data structure specific for Android.</summary>
    public RawKeyEventDataAndroid(
        int flags = 0,
        int codePoint = 0,
        int plainCodePoint = 0,
        int keyCode = 0,
        int scanCode = 0,
        int metaState = 0,
        int eventSource = 0,
        int vendorId = 0,
        int productId = 0,
        int deviceId = 0,
        int repeatCount = 0)
    {
        Flags = flags;
        CodePoint = codePoint;
        PlainCodePoint = plainCodePoint;
        KeyCode = keyCode;
        ScanCode = scanCode;
        MetaState = metaState;
        EventSource = eventSource;
        VendorId = vendorId;
        ProductId = productId;
        DeviceId = deviceId;
        RepeatCount = repeatCount;
    }

    /// <summary>The current set of additional flags for this event (repeat state, etc.).</summary>
    public int Flags { get; }

    /// <summary>
    /// The Unicode code point represented by the key event, if any; zero if there is none.
    /// Dead keys are represented as Unicode combining characters.
    /// </summary>
    public int CodePoint { get; }

    /// <summary>
    /// The Unicode code point represented by the key event, if any, without regard to any modifier
    /// keys which are currently pressed (<c>KeyEvent.getUnicodeChar(0)</c>); zero if there is none.
    /// </summary>
    public int PlainCodePoint { get; }

    /// <summary>The hardware key code corresponding to this key event (the physical key, not the character).</summary>
    public int KeyCode { get; }

    /// <summary>
    /// The hardware scan code id corresponding to this key event. These values vary from device to
    /// device, so this information is mainly useful for debugging.
    /// </summary>
    public int ScanCode { get; }

    /// <summary>
    /// The modifiers that were present when the key event occurred (Android's <c>getMetaState()</c>).
    /// Many of the bits are replicated as the <c>Modifier*</c> constants in this class.
    /// </summary>
    public int MetaState { get; }

    /// <summary>The source of the event (Android's <c>getSource()</c>).</summary>
    public int EventSource { get; }

    /// <summary>The vendor ID of the device that produced the event.</summary>
    public int VendorId { get; }

    /// <summary>The product ID of the device that produced the event.</summary>
    public int ProductId { get; }

    /// <summary>The ID of the device that produced the event.</summary>
    public int DeviceId { get; }

    /// <summary>The repeat count of the event.</summary>
    public int RepeatCount { get; }

    // Android only reports a single code point for the key label.
    /// <inheritdoc/>
    public override string KeyLabel =>
        PlainCodePoint == 0 ? string.Empty : FromCharCode(PlainCodePoint & KCombiningCharacterMask);

    /// <inheritdoc/>
    public override PhysicalKeyboardKey PhysicalKey
    {
        get
        {
            if (KeyboardMaps.KAndroidToPhysicalKey.TryGetValue(ScanCode, out PhysicalKeyboardKey? mapped))
            {
                return mapped;
            }

            // Android sends DPAD_UP, etc. as the keyCode for joystick DPAD events, but it doesn't set
            // the scanCode for those, so we have to detect this, and set our own DPAD physical keys.
            // The logical key will still match "arrowUp", etc.
            if ((EventSource & SourceJoystick) == SourceJoystick)
            {
                LogicalKeyboardKey? foundKey = KeyboardMaps.KAndroidToLogicalKey.GetValueOrDefault(KeyCode);
                if (LogicalKeyboardKey.ArrowUp.Equals(foundKey))
                {
                    return PhysicalKeyboardKey.ArrowUp;
                }

                if (LogicalKeyboardKey.ArrowDown.Equals(foundKey))
                {
                    return PhysicalKeyboardKey.ArrowDown;
                }

                if (LogicalKeyboardKey.ArrowLeft.Equals(foundKey))
                {
                    return PhysicalKeyboardKey.ArrowLeft;
                }

                if (LogicalKeyboardKey.ArrowRight.Equals(foundKey))
                {
                    return PhysicalKeyboardKey.ArrowRight;
                }
            }

            return new PhysicalKeyboardKey(LogicalKeyboardKey.AndroidPlane + ScanCode);
        }
    }

    /// <inheritdoc/>
    public override LogicalKeyboardKey LogicalKey
    {
        get
        {
            // Look to see if the keyCode is a printable number pad key, so that a difference between
            // regular keys (e.g. "=") and the number pad version (e.g. the "=" on the number pad) can
            // be determined.
            LogicalKeyboardKey? numPadKey = KeyboardMaps.KAndroidNumPadMap.GetValueOrDefault(KeyCode);
            if (numPadKey != null)
            {
                return numPadKey;
            }

            // If it has a non-control-character label, then either return the existing constant, or
            // construct a new Unicode-based key from it. Don't mark it as autogenerated, since the
            // label uniquely identifies an ID from the Unicode plane.
            string keyLabel = KeyLabel;
            if (keyLabel.Length != 0 && !LogicalKeyboardKey.IsControlCharacter(keyLabel))
            {
                int combinedCodePoint = PlainCodePoint & KCombiningCharacterMask;
                long keyId = LogicalKeyboardKey.UnicodePlane | (combinedCodePoint & LogicalKeyboardKey.ValueMask);
                return LogicalKeyboardKey.FindKeyByKeyId(keyId) ?? new LogicalKeyboardKey(keyId);
            }

            // Look to see if the keyCode is one we know about and have a mapping for.
            LogicalKeyboardKey? newKey = KeyboardMaps.KAndroidToLogicalKey.GetValueOrDefault(KeyCode);
            if (newKey != null)
            {
                return newKey;
            }

            return new LogicalKeyboardKey(KeyCode | LogicalKeyboardKey.AndroidPlane);
        }
    }

    private bool IsLeftRightModifierPressed(KeyboardSide side, int anyMask, int leftMask, int rightMask)
    {
        if ((MetaState & anyMask) == 0)
        {
            return false;
        }

        return side switch
        {
            KeyboardSide.Any => true,
            KeyboardSide.All => (MetaState & leftMask) != 0 && (MetaState & rightMask) != 0,
            KeyboardSide.Left => (MetaState & leftMask) != 0,
            KeyboardSide.Right => (MetaState & rightMask) != 0,
            _ => throw new ArgumentOutOfRangeException(nameof(side)),
        };
    }

    /// <inheritdoc/>
    public override bool IsModifierPressed(ModifierKey key, KeyboardSide side = KeyboardSide.Any)
    {
        return key switch
        {
            ModifierKey.ControlModifier => IsLeftRightModifierPressed(
                side,
                ModifierControl,
                ModifierLeftControl,
                ModifierRightControl),
            ModifierKey.ShiftModifier => IsLeftRightModifierPressed(
                side,
                ModifierShift,
                ModifierLeftShift,
                ModifierRightShift),
            ModifierKey.AltModifier => IsLeftRightModifierPressed(
                side,
                ModifierAlt,
                ModifierLeftAlt,
                ModifierRightAlt),
            ModifierKey.MetaModifier => IsLeftRightModifierPressed(
                side,
                ModifierMeta,
                ModifierLeftMeta,
                ModifierRightMeta),
            ModifierKey.CapsLockModifier => (MetaState & ModifierCapsLock) != 0,
            ModifierKey.NumLockModifier => (MetaState & ModifierNumLock) != 0,
            ModifierKey.ScrollLockModifier => (MetaState & ModifierScrollLock) != 0,
            ModifierKey.FunctionModifier => (MetaState & ModifierFunction) != 0,
            ModifierKey.SymbolModifier => (MetaState & ModifierSym) != 0,
            _ => throw new ArgumentOutOfRangeException(nameof(key)),
        };
    }

    /// <inheritdoc/>
    public override KeyboardSide? GetModifierSide(ModifierKey key)
    {
        KeyboardSide? FindSide(int anyMask, int leftMask, int rightMask)
        {
            int combinedMask = leftMask | rightMask;
            int combined = MetaState & combinedMask;
            if (combined == leftMask)
            {
                return KeyboardSide.Left;
            }
            else if (combined == rightMask)
            {
                return KeyboardSide.Right;
            }
            else if (combined == combinedMask)
            {
                return KeyboardSide.All;
            }

            // If the platform code sets the "any" modifier, but not a specific side, then we return
            // "all", assuming that there is only one of that modifier key on the keyboard.
            if ((MetaState & anyMask) != 0)
            {
                return KeyboardSide.All;
            }

            return null;
        }

        switch (key)
        {
            case ModifierKey.ControlModifier:
                return FindSide(ModifierControl, ModifierLeftControl, ModifierRightControl);
            case ModifierKey.ShiftModifier:
                return FindSide(ModifierShift, ModifierLeftShift, ModifierRightShift);
            case ModifierKey.AltModifier:
                return FindSide(ModifierAlt, ModifierLeftAlt, ModifierRightAlt);
            case ModifierKey.MetaModifier:
                return FindSide(ModifierMeta, ModifierLeftMeta, ModifierRightMeta);
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

    /// <inheritdoc/>
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<int>("flags", Flags));
        properties.Add(new DiagnosticsProperty<int>("codePoint", CodePoint));
        properties.Add(new DiagnosticsProperty<int>("plainCodePoint", PlainCodePoint));
        properties.Add(new DiagnosticsProperty<int>("keyCode", KeyCode));
        properties.Add(new DiagnosticsProperty<int>("scanCode", ScanCode));
        properties.Add(new DiagnosticsProperty<int>("metaState", MetaState));
    }

    /// <inheritdoc/>
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

        return obj is RawKeyEventDataAndroid other
            && other.Flags == Flags
            && other.CodePoint == CodePoint
            && other.PlainCodePoint == PlainCodePoint
            && other.KeyCode == KeyCode
            && other.ScanCode == ScanCode
            && other.MetaState == MetaState;
    }

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(Flags, CodePoint, PlainCodePoint, KeyCode, ScanCode, MetaState);

    // Dart's `String.fromCharCode`: a code point above the BMP becomes a surrogate pair, a BMP code
    // unit (including a lone surrogate) stays a single UTF-16 code unit.
    private static string FromCharCode(int charCode) =>
        charCode <= 0xFFFF ? ((char)charCode).ToString() : char.ConvertFromUtf32(charCode);

    // Modifier key masks.

    /// <summary>No modifier keys are pressed in the <see cref="MetaState"/> field.</summary>
    public const int ModifierNone = 0;

    /// <summary>Mask for <see cref="MetaState"/>: one of the ALT modifier keys is pressed.</summary>
    public const int ModifierAlt = 0x02;

    /// <summary>Mask for <see cref="MetaState"/>: the left ALT modifier key is pressed.</summary>
    public const int ModifierLeftAlt = 0x10;

    /// <summary>Mask for <see cref="MetaState"/>: the right ALT modifier key is pressed.</summary>
    public const int ModifierRightAlt = 0x20;

    /// <summary>Mask for <see cref="MetaState"/>: one of the SHIFT modifier keys is pressed.</summary>
    public const int ModifierShift = 0x01;

    /// <summary>Mask for <see cref="MetaState"/>: the left SHIFT modifier key is pressed.</summary>
    public const int ModifierLeftShift = 0x40;

    /// <summary>Mask for <see cref="MetaState"/>: the right SHIFT modifier key is pressed.</summary>
    public const int ModifierRightShift = 0x80;

    /// <summary>Mask for <see cref="MetaState"/>: the SYM modifier key is pressed.</summary>
    public const int ModifierSym = 0x04;

    /// <summary>Mask for <see cref="MetaState"/>: the Function modifier key (Fn) is pressed.</summary>
    public const int ModifierFunction = 0x08;

    /// <summary>Mask for <see cref="MetaState"/>: one of the CTRL modifier keys is pressed.</summary>
    public const int ModifierControl = 0x1000;

    /// <summary>Mask for <see cref="MetaState"/>: the left CTRL modifier key is pressed.</summary>
    public const int ModifierLeftControl = 0x2000;

    /// <summary>Mask for <see cref="MetaState"/>: the right CTRL modifier key is pressed.</summary>
    public const int ModifierRightControl = 0x4000;

    /// <summary>Mask for <see cref="MetaState"/>: one of the META modifier keys is pressed.</summary>
    public const int ModifierMeta = 0x10000;

    /// <summary>Mask for <see cref="MetaState"/>: the left META modifier key is pressed.</summary>
    public const int ModifierLeftMeta = 0x20000;

    /// <summary>Mask for <see cref="MetaState"/>: the right META modifier key is pressed.</summary>
    public const int ModifierRightMeta = 0x40000;

    /// <summary>Mask for <see cref="MetaState"/>: the CAPS LOCK modifier key is on.</summary>
    public const int ModifierCapsLock = 0x100000;

    /// <summary>Mask for <see cref="MetaState"/>: the NUM LOCK modifier key is on.</summary>
    public const int ModifierNumLock = 0x200000;

    /// <summary>Mask for <see cref="MetaState"/>: the SCROLL LOCK modifier key is on.</summary>
    public const int ModifierScrollLock = 0x400000;
}
