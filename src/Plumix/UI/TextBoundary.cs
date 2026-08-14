using System.Globalization;
using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity sources:
// flutter/packages/flutter/lib/src/services/text_boundary.dart
// flutter/packages/flutter/lib/src/services/text_layout_metrics.dart

/// Signature for a predicate that takes an offset into a UTF-16 string, and a
/// boolean that indicates the search direction.
public delegate bool UntilPredicate(int offset, bool forward);

/// A read-only interface for accessing visual information about the
/// implementing text.
public interface ITextLayoutMetrics
{
    /// Check if the given code unit is a white space or separator character.
    static bool IsWhitespace(int codeUnit)
    {
        switch (codeUnit)
        {
            case 0x9: // horizontal tab
            case 0xA: // line feed
            case 0xB: // vertical tab
            case 0xC: // form feed
            case 0xD: // carriage return
            case 0x1C: // file separator
            case 0x1D: // group separator
            case 0x1E: // record separator
            case 0x1F: // unit separator
            case 0x20: // space
            case 0xA0: // no-break space
            case 0x1680: // ogham space mark
            case 0x2000: // en quad
            case 0x2001: // em quad
            case 0x2002: // en space
            case 0x2003: // em space
            case 0x2004: // three-per-em space
            case 0x2005: // four-per-em space
            case 0x2006: // six-per-em space
            case 0x2007: // figure space
            case 0x2008: // punctuation space
            case 0x2009: // thin space
            case 0x200A: // hair space
            case 0x202F: // narrow no-break space
            case 0x205F: // medium mathematical space
            case 0x3000: // ideographic space
                return true;
            default:
                return false;
        }
    }

    /// Check if the given code unit is a line terminator character.
    static bool IsLineTerminator(int codeUnit)
    {
        switch (codeUnit)
        {
            case 0x0A: // line feed
            case 0x0B: // vertical feed
            case 0x0C: // form feed
            case 0x0D: // carriage return
            case 0x85: // new line
            case 0x2028: // line separator
            case 0x2029: // paragraph separator
                return true;
            default:
                return false;
        }
    }

    /// Return a [TextSelection] containing the line of the given [TextPosition].
    TextSelection GetLineAtOffset(TextPosition position);

    /// Returns the text range of the word at the given [TextPosition].
    TextRange GetWordBoundary(TextPosition position);

    /// Returns the [TextPosition] above the given offset into the text.
    TextPosition GetTextPositionAbove(TextPosition position);

    /// Returns the [TextPosition] below the given offset into the text.
    TextPosition GetTextPositionBelow(TextPosition position);
}

/// An interface for retrieving the logical text boundary (as opposed to the
/// visual boundary) at a given code unit offset in a document.
///
/// Either the [GetTextBoundaryAt] method, or both the
/// [GetLeadingTextBoundaryAt] method and the [GetTrailingTextBoundaryAt] method
/// must be implemented.
public abstract class TextBoundary
{
    /// Returns the offset of the closest text boundary before or at the given
    /// `position`, or null if no boundaries can be found.
    public virtual int? GetLeadingTextBoundaryAt(int position)
    {
        if (position < 0)
        {
            return null;
        }

        int start = GetTextBoundaryAt(position).Start;
        return start >= 0 ? start : null;
    }

    /// Returns the offset of the closest text boundary after the given
    /// `position`, or null if there is no boundary can be found after `position`.
    public virtual int? GetTrailingTextBoundaryAt(int position)
    {
        int end = GetTextBoundaryAt(Math.Max(0, position)).End;
        return end >= 0 ? end : null;
    }

    /// Returns the text boundary range that encloses the input position.
    ///
    /// The returned [TextRange] may contain `-1`, which indicates no boundaries
    /// can be found in that direction.
    public virtual TextRange GetTextBoundaryAt(int position)
    {
        int start = GetLeadingTextBoundaryAt(position) ?? -1;
        int end = GetTrailingTextBoundaryAt(position) ?? -1;
        return new TextRange(start, end);
    }
}

/// A [TextBoundary] subclass for retrieving the range of the grapheme the given
/// `position` is in.
public sealed class CharacterBoundary : TextBoundary
{
    private readonly string _text;

    public CharacterBoundary(string text)
    {
        _text = text ?? string.Empty;
    }

    public override int? GetLeadingTextBoundaryAt(int position)
    {
        if (position < 0)
        {
            return null;
        }

        return GraphemeStartAtOrBefore(Math.Min(position, _text.Length));
    }

    public override int? GetTrailingTextBoundaryAt(int position)
    {
        if (position >= _text.Length)
        {
            return null;
        }

        return GraphemeEndAfter(Math.Max(0, position));
    }

    public override TextRange GetTextBoundaryAt(int position)
    {
        if (position < 0)
        {
            return new TextRange(-1, GetTrailingTextBoundaryAt(position) ?? -1);
        }

        if (position >= _text.Length)
        {
            return new TextRange(GetLeadingTextBoundaryAt(position) ?? -1, -1);
        }

        int start = GraphemeStartAtOrBefore(position);
        return start == position
            ? new TextRange(start, GetTrailingTextBoundaryAt(position) ?? -1)
            : new TextRange(start, GraphemeEndAfter(position));
    }

    private int GraphemeStartAtOrBefore(int position)
    {
        if (_text.Length == 0)
        {
            return 0;
        }

        int[] boundaries = StringInfo.ParseCombiningCharacters(_text);
        int start = 0;
        foreach (int boundary in boundaries)
        {
            if (boundary > position)
            {
                break;
            }

            start = boundary;
        }

        return start;
    }

    private int GraphemeEndAfter(int position)
    {
        if (_text.Length == 0)
        {
            return 0;
        }

        int[] boundaries = StringInfo.ParseCombiningCharacters(_text);
        foreach (int boundary in boundaries)
        {
            if (boundary > position)
            {
                return boundary;
            }
        }

        return _text.Length;
    }
}

/// A [TextBoundary] subclass for locating closest line breaks to a given
/// `position`.
public sealed class LineBoundary : TextBoundary
{
    private readonly ITextLayoutMetrics _textLayout;

    public LineBoundary(ITextLayoutMetrics textLayout)
    {
        _textLayout = textLayout ?? throw new ArgumentNullException(nameof(textLayout));
    }

    public override TextRange GetTextBoundaryAt(int position)
    {
        TextSelection line = _textLayout.GetLineAtOffset(new TextPosition(Math.Max(position, 0)));
        return new TextRange(line.Start, line.End);
    }
}

/// A [TextBoundary] subclass that uses words as logical boundaries.
public sealed class WordBoundary : TextBoundary
{
    private readonly ITextLayoutMetrics _textLayout;

    public WordBoundary(ITextLayoutMetrics textLayout)
    {
        _textLayout = textLayout ?? throw new ArgumentNullException(nameof(textLayout));
    }

    public override TextRange GetTextBoundaryAt(int position)
    {
        return _textLayout.GetWordBoundary(new TextPosition(Math.Max(position, 0)));
    }
}

/// A text boundary that uses paragraphs as logical boundaries.
///
/// A paragraph is defined as the range between line terminators. If no
/// line terminators exist then the paragraph boundary is the entire document.
public sealed class ParagraphBoundary : TextBoundary
{
    private readonly string _text;

    public ParagraphBoundary(string text)
    {
        _text = text ?? string.Empty;
    }

    public override int? GetLeadingTextBoundaryAt(int position)
    {
        if (position < 0 || _text.Length == 0)
        {
            return null;
        }

        if (position >= _text.Length)
        {
            return _text.Length;
        }

        if (position == 0)
        {
            return 0;
        }

        int index = position;
        if (index > 1 && _text[index] == 0x0A && _text[index - 1] == 0x0D)
        {
            index -= 2;
        }
        else if (ITextLayoutMetrics.IsLineTerminator(_text[index]))
        {
            index -= 1;
        }

        while (index > 0)
        {
            if (ITextLayoutMetrics.IsLineTerminator(_text[index]))
            {
                return index + 1;
            }

            index -= 1;
        }

        return Math.Max(index, 0);
    }

    public override int? GetTrailingTextBoundaryAt(int position)
    {
        if (position >= _text.Length || _text.Length == 0)
        {
            return null;
        }

        if (position < 0)
        {
            return 0;
        }

        int index = position;
        while (!ITextLayoutMetrics.IsLineTerminator(_text[index]))
        {
            index += 1;
            if (index == _text.Length)
            {
                return index;
            }
        }

        return index < _text.Length - 1 && _text[index] == 0x0D && _text[index + 1] == 0x0A
            ? index + 2
            : index + 1;
    }
}

/// A text boundary that uses the entire document as logical boundary.
public sealed class DocumentBoundary : TextBoundary
{
    private readonly string _text;

    public DocumentBoundary(string text)
    {
        _text = text ?? string.Empty;
    }

    public override int? GetLeadingTextBoundaryAt(int position) => position < 0 ? null : 0;

    public override int? GetTrailingTextBoundaryAt(int position)
    {
        return position >= _text.Length ? null : _text.Length;
    }
}
