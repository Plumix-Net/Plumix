using System.Globalization;
using System.Text;

// C#-only infrastructure: the Unicode properties the headless paragraph engine and
// `TextPainter`/`WordBoundary` need where Dart relies on ICU inside the engine (UAX #29 word
// boundaries, grapheme clusters, a simplified UAX #9 bidi resolution and UAX #14 break
// opportunities). No Dart source maps to this file.

namespace Plumix.UI;

/// The bidi category the headless engine resolves levels from.
internal enum BidiCategory
{
    /// Strong left-to-right.
    Left,

    /// Strong right-to-left (Hebrew, Arabic and the other RTL scripts).
    Right,

    /// European number: a weak type that keeps its own run direction.
    Number,

    /// Everything else: whitespace, punctuation, symbols, emoji and placeholders.
    Neutral,
}

/// Unicode text properties used by the paragraph layer.
internal static class UnicodeText
{
    /// The object replacement character a placeholder occupies in the text.
    public const char ObjectReplacementCharacter = '\uFFFC';

    /// Whether the code point draws no advance: combining marks, format characters (ZWJ, ZWSP,
    /// bidi controls) and variation selectors.
    public static bool IsZeroWidth(Rune rune)
    {
        switch (Rune.GetUnicodeCategory(rune))
        {
            case UnicodeCategory.NonSpacingMark:
            case UnicodeCategory.EnclosingMark:
            case UnicodeCategory.SpacingCombiningMark:
            case UnicodeCategory.Format:
                return true;
            case UnicodeCategory.Control:
                return rune.Value != '\t';
            default:
                return false;
        }
    }

    /// Whether the code unit is one of the hard line break characters the engine honors
    /// (LF, VT, FF, NEL, LS, PS). CR is folded into the grapheme cluster it forms with LF.
    public static bool IsHardBreak(int codePoint)
    {
        return codePoint is 0x000A or 0x000B or 0x000C or 0x000D or 0x0085 or 0x2028 or 0x2029;
    }

    /// Whether the code point is a `Space_Separator` or a tab: whitespace that hangs at the end of
    /// a line instead of wrapping.
    public static bool IsHangingSpace(int codePoint)
    {
        if (codePoint == '\t')
        {
            return true;
        }

        if (codePoint is 0x00A0 or 0x2007 or 0x202F)
        {
            return false;
        }

        return codePoint <= 0x10FFFF
               && Rune.IsValid(codePoint)
               && Rune.GetUnicodeCategory(new Rune(codePoint)) == UnicodeCategory.SpaceSeparator;
    }

    /// Whether the string matches Dart's `\p{Space_Separator}`.
    public static bool IsSpaceSeparator(int codePoint)
    {
        return Rune.IsValid(codePoint)
               && Rune.GetUnicodeCategory(new Rune(codePoint)) == UnicodeCategory.SpaceSeparator;
    }

    /// Whether the code point matches Dart's `[\p{Space_Separator}\p{Punctuation}]`.
    public static bool IsSpaceSeparatorOrPunctuation(int codePoint)
    {
        if (!Rune.IsValid(codePoint))
        {
            return false;
        }

        switch (Rune.GetUnicodeCategory(new Rune(codePoint)))
        {
            case UnicodeCategory.SpaceSeparator:
            case UnicodeCategory.ConnectorPunctuation:
            case UnicodeCategory.DashPunctuation:
            case UnicodeCategory.OpenPunctuation:
            case UnicodeCategory.ClosePunctuation:
            case UnicodeCategory.InitialQuotePunctuation:
            case UnicodeCategory.FinalQuotePunctuation:
            case UnicodeCategory.OtherPunctuation:
                return true;
            default:
                return false;
        }
    }

    /// Whether `text` holds an unpaired surrogate, which the engine rejects with an argument error.
    public static bool HasUnpairedSurrogate(string text)
    {
        for (int index = 0; index < text.Length; index++)
        {
            char c = text[index];
            if (char.IsHighSurrogate(c))
            {
                if (index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1]))
                {
                    return true;
                }

                index++;
            }
            else if (char.IsLowSurrogate(c))
            {
                return true;
            }
        }

        return false;
    }

    /// The code point at `index`, or the lone surrogate's value when it is unpaired.
    public static int CodePointAt(string text, int index)
    {
        char c = text[index];
        if (char.IsHighSurrogate(c) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
        {
            return char.ConvertToUtf32(c, text[index + 1]);
        }

        return c;
    }

    /// The UTF-16 length of the code point at `index`.
    public static int CodePointLength(string text, int index)
    {
        char c = text[index];
        return char.IsHighSurrogate(c) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]) ? 2 : 1;
    }

    /// The bidi category of a code point.
    public static BidiCategory GetBidiCategory(int codePoint)
    {
        if (IsRightToLeft(codePoint))
        {
            return BidiCategory.Right;
        }

        if (codePoint is >= '0' and <= '9')
        {
            return BidiCategory.Number;
        }

        if (!Rune.IsValid(codePoint))
        {
            return BidiCategory.Neutral;
        }

        var rune = new Rune(codePoint);
        if (Rune.IsDigit(rune))
        {
            return BidiCategory.Number;
        }

        return Rune.IsLetter(rune) || Rune.GetUnicodeCategory(rune) == UnicodeCategory.SpacingCombiningMark
            ? BidiCategory.Left
            : BidiCategory.Neutral;
    }

    private static bool IsRightToLeft(int codePoint)
    {
        return codePoint is >= 0x0590 and <= 0x08FF
            or >= 0xFB1D and <= 0xFDFF
            or >= 0xFE70 and <= 0xFEFF
            or >= 0x10800 and <= 0x10FFF
            or >= 0x1E800 and <= 0x1EFFF
            || codePoint == 0x200F;
    }

    /// Whether a line may break before the code point regardless of its neighbors: ideographs,
    /// kana, Hangul syllables and emoji (UAX #14 classes ID/H2/H3).
    public static bool IsIdeographicBreakClass(int codePoint)
    {
        return codePoint is >= 0x2E80 and <= 0x2FFF
            or >= 0x3040 and <= 0x30FF
            or >= 0x3400 and <= 0x4DBF
            or >= 0x4E00 and <= 0x9FFF
            or >= 0xAC00 and <= 0xD7AF
            or >= 0xF900 and <= 0xFAFF
            or >= 0x1F000 and <= 0x1FAFF
            or >= 0x20000 and <= 0x3FFFD;
    }

    // -- UAX #29 word boundaries ------------------------------------------------

    private enum WordBreakProperty
    {
        Other,
        CR,
        LF,
        Newline,
        Extend,
        ZWJ,
        RegionalIndicator,
        Format,
        Katakana,
        HebrewLetter,
        ALetter,
        SingleQuote,
        DoubleQuote,
        MidNumLet,
        MidLetter,
        MidNum,
        Numeric,
        ExtendNumLet,
        WSegSpace,
        ExtendedPictographic,
    }

    /// Returns the UAX #29 word range containing the code unit at `index`.
    ///
    /// An index outside the text yields a collapsed range at the nearest end.
    public static (int Start, int End) GetWordRange(string text, int index)
    {
        if (text.Length == 0)
        {
            return (0, 0);
        }

        if (index < 0)
        {
            return (0, 0);
        }

        if (index >= text.Length)
        {
            return (text.Length, text.Length);
        }

        // Snap into the start of a surrogate pair.
        if (char.IsLowSurrogate(text[index]) && index > 0 && char.IsHighSurrogate(text[index - 1]))
        {
            index--;
        }

        int start = index;
        while (start > 0 && !IsWordBoundary(text, start))
        {
            start--;
        }

        int end = index + CodePointLength(text, index);
        while (end < text.Length && !IsWordBoundary(text, end))
        {
            end++;
        }

        return (start, end);
    }

    /// Whether there is a UAX #29 word boundary before the code unit at `position`.
    public static bool IsWordBoundary(string text, int position)
    {
        if (position <= 0 || position >= text.Length)
        {
            return true;
        }

        // Never split a surrogate pair.
        if (char.IsLowSurrogate(text[position]) && char.IsHighSurrogate(text[position - 1]))
        {
            return false;
        }

        int previousIndex = PreviousCodePointStart(text, position);
        WordBreakProperty before = GetWordBreakProperty(CodePointAt(text, previousIndex));
        WordBreakProperty after = GetWordBreakProperty(CodePointAt(text, position));

        // WB3, WB3a, WB3b.
        if (before == WordBreakProperty.CR && after == WordBreakProperty.LF)
        {
            return false;
        }

        if (IsNewlineProperty(before) || IsNewlineProperty(after))
        {
            return true;
        }

        // WB3c.
        if (before == WordBreakProperty.ZWJ && after == WordBreakProperty.ExtendedPictographic)
        {
            return false;
        }

        // WB3d.
        if (before == WordBreakProperty.WSegSpace && after == WordBreakProperty.WSegSpace)
        {
            return false;
        }

        // WB4: ignore Extend/Format/ZWJ after anything but a newline.
        if (after is WordBreakProperty.Extend or WordBreakProperty.Format or WordBreakProperty.ZWJ)
        {
            return false;
        }

        int leftIndex = SkipIgnorableBackward(text, position);
        if (leftIndex < 0)
        {
            return true;
        }

        WordBreakProperty left = GetWordBreakProperty(CodePointAt(text, leftIndex));
        int rightIndex = position;
        WordBreakProperty right = after;
        WordBreakProperty? leftLeft = PropertyBefore(text, leftIndex);
        WordBreakProperty? rightRight = PropertyAfter(text, rightIndex);

        // WB5.
        if (IsAHLetter(left) && IsAHLetter(right))
        {
            return false;
        }

        // WB6, WB7.
        if (IsAHLetter(left) && IsMidLetterQ(right) && rightRight is { } rr && IsAHLetter(rr))
        {
            return false;
        }

        if (IsMidLetterQ(left) && IsAHLetter(right) && leftLeft is { } ll && IsAHLetter(ll))
        {
            return false;
        }

        // WB7a, WB7b, WB7c.
        if (left == WordBreakProperty.HebrewLetter && right == WordBreakProperty.SingleQuote)
        {
            return false;
        }

        if (left == WordBreakProperty.HebrewLetter
            && right == WordBreakProperty.DoubleQuote
            && rightRight == WordBreakProperty.HebrewLetter)
        {
            return false;
        }

        if (left == WordBreakProperty.DoubleQuote
            && right == WordBreakProperty.HebrewLetter
            && leftLeft == WordBreakProperty.HebrewLetter)
        {
            return false;
        }

        // WB8, WB9, WB10.
        if ((left == WordBreakProperty.Numeric || IsAHLetter(left))
            && (right == WordBreakProperty.Numeric || IsAHLetter(right)))
        {
            return false;
        }

        // WB11, WB12.
        if (left == WordBreakProperty.Numeric
            && IsMidNumQ(right)
            && rightRight == WordBreakProperty.Numeric)
        {
            return false;
        }

        if (IsMidNumQ(left)
            && right == WordBreakProperty.Numeric
            && leftLeft == WordBreakProperty.Numeric)
        {
            return false;
        }

        // WB13, WB13a, WB13b.
        if (left == WordBreakProperty.Katakana && right == WordBreakProperty.Katakana)
        {
            return false;
        }

        if ((IsAHLetter(left) || left is WordBreakProperty.Numeric or WordBreakProperty.Katakana
                or WordBreakProperty.ExtendNumLet)
            && right == WordBreakProperty.ExtendNumLet)
        {
            return false;
        }

        if (left == WordBreakProperty.ExtendNumLet
            && (IsAHLetter(right) || right is WordBreakProperty.Numeric or WordBreakProperty.Katakana))
        {
            return false;
        }

        // WB15, WB16: pair regional indicators.
        if (left == WordBreakProperty.RegionalIndicator && right == WordBreakProperty.RegionalIndicator)
        {
            int count = 0;
            int cursor = leftIndex;
            while (cursor >= 0
                   && GetWordBreakProperty(CodePointAt(text, cursor)) == WordBreakProperty.RegionalIndicator)
            {
                count++;
                cursor = cursor == 0 ? -1 : SkipIgnorableBackward(text, cursor);
            }

            return count % 2 == 0;
        }

        // WB999.
        return true;
    }

    private static bool IsNewlineProperty(WordBreakProperty property)
    {
        return property is WordBreakProperty.CR or WordBreakProperty.LF or WordBreakProperty.Newline;
    }

    private static bool IsAHLetter(WordBreakProperty property)
    {
        return property is WordBreakProperty.ALetter or WordBreakProperty.HebrewLetter;
    }

    private static bool IsMidLetterQ(WordBreakProperty property)
    {
        return property is WordBreakProperty.MidLetter or WordBreakProperty.MidNumLet or WordBreakProperty.SingleQuote;
    }

    private static bool IsMidNumQ(WordBreakProperty property)
    {
        return property is WordBreakProperty.MidNum or WordBreakProperty.MidNumLet or WordBreakProperty.SingleQuote;
    }

    private static int PreviousCodePointStart(string text, int position)
    {
        int index = position - 1;
        if (index > 0 && char.IsLowSurrogate(text[index]) && char.IsHighSurrogate(text[index - 1]))
        {
            index--;
        }

        return index;
    }

    /// The start of the nearest code point before `position` that WB4 does not ignore, or -1.
    private static int SkipIgnorableBackward(string text, int position)
    {
        int index = position;
        while (index > 0)
        {
            index = PreviousCodePointStart(text, index);
            WordBreakProperty property = GetWordBreakProperty(CodePointAt(text, index));
            if (property is WordBreakProperty.Extend or WordBreakProperty.Format or WordBreakProperty.ZWJ)
            {
                continue;
            }

            return index;
        }

        return -1;
    }

    private static WordBreakProperty? PropertyBefore(string text, int index)
    {
        int previous = SkipIgnorableBackward(text, index);
        return previous < 0 ? null : GetWordBreakProperty(CodePointAt(text, previous));
    }

    private static WordBreakProperty? PropertyAfter(string text, int index)
    {
        int cursor = index + CodePointLength(text, index);
        while (cursor < text.Length)
        {
            WordBreakProperty property = GetWordBreakProperty(CodePointAt(text, cursor));
            if (property is WordBreakProperty.Extend or WordBreakProperty.Format or WordBreakProperty.ZWJ)
            {
                cursor += CodePointLength(text, cursor);
                continue;
            }

            return property;
        }

        return null;
    }

    private static WordBreakProperty GetWordBreakProperty(int codePoint)
    {
        switch (codePoint)
        {
            case 0x000D:
                return WordBreakProperty.CR;
            case 0x000A:
                return WordBreakProperty.LF;
            case 0x000B:
            case 0x000C:
            case 0x0085:
            case 0x2028:
            case 0x2029:
                return WordBreakProperty.Newline;
            case 0x200D:
                return WordBreakProperty.ZWJ;
            case 0x200C:
                return WordBreakProperty.Extend;
            case 0x0027:
                return WordBreakProperty.SingleQuote;
            case 0x0022:
                return WordBreakProperty.DoubleQuote;
            case 0x002E:
            case 0x2018:
            case 0x2019:
            case 0x2024:
            case 0xFE52:
            case 0xFF07:
            case 0xFF0E:
                return WordBreakProperty.MidNumLet;
            case 0x003A:
            case 0x00B7:
            case 0x0387:
            case 0x055F:
            case 0x05F4:
            case 0x2027:
            case 0xFE13:
            case 0xFE55:
            case 0xFF1A:
                return WordBreakProperty.MidLetter;
            case 0x002C:
            case 0x003B:
            case 0x037E:
            case 0x0589:
            case 0x060C:
            case 0x060D:
            case 0x066C:
            case 0x07F8:
            case 0x2044:
            case 0xFE10:
            case 0xFE14:
            case 0xFE50:
            case 0xFE54:
            case 0xFF0C:
            case 0xFF1B:
                return WordBreakProperty.MidNum;
            case 0x005F:
            case 0x203F:
            case 0x2040:
            case 0x2054:
            case 0xFE33:
            case 0xFE34:
            case 0xFE4D:
            case 0xFE4E:
            case 0xFE4F:
            case 0xFF3F:
                return WordBreakProperty.ExtendNumLet;
        }

        if (codePoint is >= 0x1F1E6 and <= 0x1F1FF)
        {
            return WordBreakProperty.RegionalIndicator;
        }

        if (codePoint is >= 0x1F3FB and <= 0x1F3FF)
        {
            return WordBreakProperty.Extend;
        }

        if (!Rune.IsValid(codePoint))
        {
            return WordBreakProperty.Other;
        }

        var rune = new Rune(codePoint);
        UnicodeCategory category = Rune.GetUnicodeCategory(rune);
        switch (category)
        {
            case UnicodeCategory.NonSpacingMark:
            case UnicodeCategory.EnclosingMark:
            case UnicodeCategory.SpacingCombiningMark:
                return WordBreakProperty.Extend;
            case UnicodeCategory.Format:
                return codePoint == 0x200B ? WordBreakProperty.Other : WordBreakProperty.Format;
            case UnicodeCategory.SpaceSeparator:
                return codePoint is 0x00A0 or 0x2007 or 0x202F ? WordBreakProperty.Other : WordBreakProperty.WSegSpace;
            case UnicodeCategory.DecimalDigitNumber:
                return WordBreakProperty.Numeric;
        }

        if (codePoint is >= 0x30A0 and <= 0x30FF or >= 0x31F0 and <= 0x31FF or >= 0xFF66 and <= 0xFF9D)
        {
            return WordBreakProperty.Katakana;
        }

        if (codePoint is >= 0x05D0 and <= 0x05EA or >= 0x05EF and <= 0x05F2 or >= 0xFB1D and <= 0xFB4F)
        {
            return WordBreakProperty.HebrewLetter;
        }

        if (IsExtendedPictographic(codePoint))
        {
            return WordBreakProperty.ExtendedPictographic;
        }

        if (Rune.IsLetter(rune)
            && !(codePoint is >= 0x3040 and <= 0x309F)
            && !(codePoint is >= 0x3400 and <= 0x9FFF)
            && !(codePoint is >= 0xF900 and <= 0xFAFF)
            && !(codePoint is >= 0x20000 and <= 0x3FFFD)
            && !(codePoint is >= 0x0E00 and <= 0x0EFF))
        {
            return WordBreakProperty.ALetter;
        }

        return WordBreakProperty.Other;
    }

    private static bool IsExtendedPictographic(int codePoint)
    {
        return codePoint is 0x00A9 or 0x00AE or 0x203C or 0x2049 or 0x2122 or 0x2139 or 0x2328 or 0x23CF
                   or 0x24C2 or 0x25B6 or 0x25C0 or 0x2B50 or 0x2B55 or 0x3030 or 0x303D or 0x3297 or 0x3299
               || codePoint is >= 0x2194 and <= 0x2199
                   or >= 0x21A9 and <= 0x21AA
                   or >= 0x231A and <= 0x231B
                   or >= 0x23E9 and <= 0x23F3
                   or >= 0x23F8 and <= 0x23FA
                   or >= 0x25AA and <= 0x25AB
                   or >= 0x25FB and <= 0x25FE
                   or >= 0x2600 and <= 0x27BF
                   or >= 0x2934 and <= 0x2935
                   or >= 0x2B05 and <= 0x2B07
                   or >= 0x2B1B and <= 0x2B1C
                   or >= 0x1F000 and <= 0x1FAFF
                   or >= 0x1FC00 and <= 0x1FFFD;
    }
}
