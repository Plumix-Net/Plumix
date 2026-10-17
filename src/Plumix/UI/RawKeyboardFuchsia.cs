// Dart parity source: flutter/packages/flutter/lib/src/services/raw_keyboard_fuchsia.dart

using Plumix.Foundation;

namespace Plumix.UI;

/// <summary>
/// Platform-specific key event data for Fuchsia, obtained from Fuchsia's <c>KeyData</c> interface.
/// </summary>
/// <remarks>
/// This class is deprecated and will be removed. Platform specific key event data will no longer
/// be available. See <c>KeyEvent</c> for what is available.
/// </remarks>
[Obsolete(
    "Platform specific key event data is no longer available. See KeyEvent for what is available. "
    + "Mirrors Flutter's deprecation after v3.18.0-2.0.pre.")]
public class RawKeyEventDataFuchsia : RawKeyEventData
{
    /// <summary>Creates a key event data structure specific for Fuchsia.</summary>
    public RawKeyEventDataFuchsia(int hidUsage = 0, int codePoint = 0, int modifiers = 0)
    {
        HidUsage = hidUsage;
        CodePoint = codePoint;
        Modifiers = modifiers;
    }

    /// <summary>The USB HID usage.</summary>
    public int HidUsage { get; }

    /// <summary>
    /// The Unicode code point represented by the key event, if any; zero if there is none.
    /// Dead keys are represented as Unicode combining characters.
    /// </summary>
    public int CodePoint { get; }

    /// <summary>
    /// The modifiers that were present when the key event occurred (Fuchsia's
    /// <c>input_event_constants.fidl</c> values). Many are replicated as the <c>Modifier*</c> constants.
    /// </summary>
    public int Modifiers { get; }

    // Fuchsia only reports a single code point for the key label.
    /// <inheritdoc/>
    public override string KeyLabel => CodePoint == 0 ? string.Empty : FromCharCode(CodePoint);

    /// <inheritdoc/>
    public override LogicalKeyboardKey LogicalKey
    {
        get
        {
            // If the key has a printable representation, then make a logical key based on that.
            if (CodePoint != 0)
            {
                long flutterId = LogicalKeyboardKey.UnicodePlane | (CodePoint & LogicalKeyboardKey.ValueMask);
                return KeyboardMaps.KFuchsiaToLogicalKey.GetValueOrDefault(flutterId)
                    ?? new LogicalKeyboardKey(
                        LogicalKeyboardKey.UnicodePlane | (CodePoint & LogicalKeyboardKey.ValueMask));
            }

            // Look to see if the hidUsage is one we know about and have a mapping for.
            LogicalKeyboardKey? newKey =
                KeyboardMaps.KFuchsiaToLogicalKey.GetValueOrDefault(HidUsage | LogicalKeyboardKey.FuchsiaPlane);
            if (newKey != null)
            {
                return newKey;
            }

            // This is a non-printable key that we don't know about, so we mint a new code.
            return new LogicalKeyboardKey(HidUsage | LogicalKeyboardKey.FuchsiaPlane);
        }
    }

    /// <inheritdoc/>
    public override PhysicalKeyboardKey PhysicalKey =>
        KeyboardMaps.KFuchsiaToPhysicalKey.GetValueOrDefault(HidUsage)
        ?? new PhysicalKeyboardKey(LogicalKeyboardKey.FuchsiaPlane + HidUsage);

    private bool IsLeftRightModifierPressed(KeyboardSide side, int anyMask, int leftMask, int rightMask)
    {
        if ((Modifiers & anyMask) == 0)
        {
            return false;
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

    /// <inheritdoc/>
    public override bool IsModifierPressed(ModifierKey key, KeyboardSide side = KeyboardSide.Any)
    {
        switch (key)
        {
            case ModifierKey.ControlModifier:
                return IsLeftRightModifierPressed(
                    side,
                    ModifierControl,
                    ModifierLeftControl,
                    ModifierRightControl);
            case ModifierKey.ShiftModifier:
                return IsLeftRightModifierPressed(
                    side,
                    ModifierShift,
                    ModifierLeftShift,
                    ModifierRightShift);
            case ModifierKey.AltModifier:
                return IsLeftRightModifierPressed(side, ModifierAlt, ModifierLeftAlt, ModifierRightAlt);
            case ModifierKey.MetaModifier:
                return IsLeftRightModifierPressed(side, ModifierMeta, ModifierLeftMeta, ModifierRightMeta);
            case ModifierKey.CapsLockModifier:
                return (Modifiers & ModifierCapsLock) != 0;
            case ModifierKey.NumLockModifier:
            case ModifierKey.ScrollLockModifier:
            case ModifierKey.FunctionModifier:
            case ModifierKey.SymbolModifier:
                // Fuchsia doesn't have masks for these keys (yet).
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(key));
        }
    }

    /// <inheritdoc/>
    public override KeyboardSide? GetModifierSide(ModifierKey key)
    {
        KeyboardSide? FindSide(int anyMask, int leftMask, int rightMask)
        {
            int combined = Modifiers & anyMask;
            if (combined == leftMask)
            {
                return KeyboardSide.Left;
            }
            else if (combined == rightMask)
            {
                return KeyboardSide.Right;
            }
            else if (combined == anyMask)
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
                return (Modifiers & ModifierCapsLock) == 0 ? null : KeyboardSide.All;
            case ModifierKey.NumLockModifier:
            case ModifierKey.ScrollLockModifier:
            case ModifierKey.FunctionModifier:
            case ModifierKey.SymbolModifier:
                // Fuchsia doesn't support these modifiers, so they can't be pressed.
                return null;
            default:
                throw new ArgumentOutOfRangeException(nameof(key));
        }
    }

    /// <inheritdoc/>
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<int>("hidUsage", HidUsage));
        properties.Add(new DiagnosticsProperty<int>("codePoint", CodePoint));
        properties.Add(new DiagnosticsProperty<int>("modifiers", Modifiers));
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

        return obj is RawKeyEventDataFuchsia other
            && other.HidUsage == HidUsage
            && other.CodePoint == CodePoint
            && other.Modifiers == Modifiers;
    }

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(HidUsage, CodePoint, Modifiers);

    // Dart's `String.fromCharCode`: a code point above the BMP becomes a surrogate pair, a BMP code
    // unit (including a lone surrogate) stays a single UTF-16 code unit.
    private static string FromCharCode(int charCode) =>
        charCode <= 0xFFFF ? ((char)charCode).ToString() : char.ConvertFromUtf32(charCode);

    // Keyboard modifier masks for Fuchsia modifiers.

    /// <summary>The <see cref="Modifiers"/> field equals this value when no modifier keys are pressed.</summary>
    public const int ModifierNone = 0x0;

    /// <summary>Mask for <see cref="Modifiers"/>: the CAPS LOCK modifier key is on.</summary>
    public const int ModifierCapsLock = 0x1;

    /// <summary>Mask for <see cref="Modifiers"/>: the left SHIFT modifier key is pressed.</summary>
    public const int ModifierLeftShift = 0x2;

    /// <summary>Mask for <see cref="Modifiers"/>: the right SHIFT modifier key is pressed.</summary>
    public const int ModifierRightShift = 0x4;

    /// <summary>Mask for <see cref="Modifiers"/>: one of the SHIFT modifier keys is pressed.</summary>
    public const int ModifierShift = ModifierLeftShift | ModifierRightShift;

    /// <summary>Mask for <see cref="Modifiers"/>: the left CTRL modifier key is pressed.</summary>
    public const int ModifierLeftControl = 0x8;

    /// <summary>Mask for <see cref="Modifiers"/>: the right CTRL modifier key is pressed.</summary>
    public const int ModifierRightControl = 0x10;

    /// <summary>Mask for <see cref="Modifiers"/>: one of the CTRL modifier keys is pressed.</summary>
    public const int ModifierControl = ModifierLeftControl | ModifierRightControl;

    /// <summary>Mask for <see cref="Modifiers"/>: the left ALT modifier key is pressed.</summary>
    public const int ModifierLeftAlt = 0x20;

    /// <summary>Mask for <see cref="Modifiers"/>: the right ALT modifier key is pressed.</summary>
    public const int ModifierRightAlt = 0x40;

    /// <summary>Mask for <see cref="Modifiers"/>: one of the ALT modifier keys is pressed.</summary>
    public const int ModifierAlt = ModifierLeftAlt | ModifierRightAlt;

    /// <summary>Mask for <see cref="Modifiers"/>: the left META modifier key is pressed.</summary>
    public const int ModifierLeftMeta = 0x80;

    /// <summary>Mask for <see cref="Modifiers"/>: the right META modifier key is pressed.</summary>
    public const int ModifierRightMeta = 0x100;

    /// <summary>Mask for <see cref="Modifiers"/>: one of the META modifier keys is pressed.</summary>
    public const int ModifierMeta = ModifierLeftMeta | ModifierRightMeta;
}
