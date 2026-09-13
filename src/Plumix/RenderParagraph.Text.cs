using Avalonia;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/paragraph.dart

namespace Plumix;

// The text-metric queries of RenderParagraph. Each one makes sure the text painter is laid out with
// the current constraints and delegates to it, as Dart does.
public sealed partial class RenderParagraph
{
    /// Returns the offset at which to paint the caret.
    ///
    /// Valid only after [RenderObject.Layout] has been called.
    public Point GetOffsetForCaret(TextPosition position, Rect caretPrototype)
    {
        LayoutTextWithConstraints(Constraints);
        return _textPainter.GetOffsetForCaret(position, caretPrototype);
    }

    /// Returns the strut bounded height of the glyph at the given `position`.
    ///
    /// Valid only after [RenderObject.Layout] has been called.
    public double GetFullHeightForCaret(TextPosition position)
    {
        LayoutTextWithConstraints(Constraints);
        return _textPainter.GetFullHeightForCaret(position, default);
    }

    /// Returns a list of rects that bound the given selection.
    ///
    /// The `boxHeightStyle` and `boxWidthStyle` arguments may be used to select the shape of the
    /// [TextBox]es. These properties default to [BoxHeightStyle.Tight] and [BoxWidthStyle.Tight].
    ///
    /// A given selection might have more than one rect if the [RenderParagraph] contains multiple
    /// [InlineSpan]s or bidirectional text, because logically contiguous text might not be visually
    /// contiguous.
    ///
    /// Valid only after [RenderObject.Layout] has been called.
    public IReadOnlyList<TextBox> GetBoxesForSelection(
        TextSelection selection,
        BoxHeightStyle boxHeightStyle = BoxHeightStyle.Tight,
        BoxWidthStyle boxWidthStyle = BoxWidthStyle.Tight)
    {
        LayoutTextWithConstraints(Constraints);
        return _textPainter.GetBoxesForSelection(selection, boxHeightStyle, boxWidthStyle);
    }

    /// Returns the position within the text for the given pixel offset.
    ///
    /// Valid only after [RenderObject.Layout] has been called.
    public TextPosition GetPositionForOffset(Point offset)
    {
        LayoutTextWithConstraints(Constraints);
        return _textPainter.GetPositionForOffset(offset);
    }

    /// Returns the text range of the word at the given offset. Characters not part of a word, such
    /// as spaces, symbols, and punctuation, have word breaks on both sides. In such cases, this
    /// method will return a text range that contains the given text position.
    ///
    /// Word boundaries are defined more precisely in Unicode Standard Annex #29
    /// <http://www.unicode.org/reports/tr29/#Word_Boundaries>.
    ///
    /// Valid only after [RenderObject.Layout] has been called.
    public TextRange GetWordBoundary(TextPosition position)
    {
        LayoutTextWithConstraints(Constraints);
        return _textPainter.GetWordBoundary(position);
    }

    /// Returns the size of the text as laid out.
    ///
    /// This can differ from [RenderBox.Size] if the text overflowed or if the [BoxConstraints]
    /// provided by the parent [RenderObject] forced the layout to be bigger than necessary for the
    /// given [Text].
    ///
    /// Valid only after [RenderObject.Layout] has been called.
    public Size TextSize => _textPainter.Size;

    /// Whether the text was truncated or ellipsized as laid out.
    ///
    /// This returns the [TextPainter.DidExceedMaxLines] of the underlying [TextPainter].
    ///
    /// Valid only after [RenderObject.Layout] has been called.
    public bool DidExceedMaxLines => _textPainter.DidExceedMaxLines;

    /// The bottom-left corner of the caret box at `position`. Dart's `_getOffsetForPosition`.
    internal Point GetPositionOffset(TextPosition position)
    {
        Point caret = GetOffsetForCaret(position, default);
        return new Point(caret.X, caret.Y + GetFullHeightForCaret(position));
    }

    /// Dart's `_getLineAtOffset`.
    internal TextRange GetLineAtOffset(TextPosition position) => _textPainter.GetLineBoundary(position);

    /// Dart's `_getTextPositionAbove`.
    internal TextPosition GetTextPositionAbove(TextPosition position)
    {
        // -0.5 of preferredLineHeight points to the middle of the line above.
        double preferredLineHeight = _textPainter.PreferredLineHeight;
        double verticalOffset = -0.5 * preferredLineHeight;
        return GetTextPositionVertical(position, verticalOffset);
    }

    /// Dart's `_getTextPositionBelow`.
    internal TextPosition GetTextPositionBelow(TextPosition position)
    {
        // 1.5 of preferredLineHeight points to the middle of the line below.
        double preferredLineHeight = _textPainter.PreferredLineHeight;
        double verticalOffset = 1.5 * preferredLineHeight;
        return GetTextPositionVertical(position, verticalOffset);
    }

    private TextPosition GetTextPositionVertical(TextPosition position, double verticalOffset)
    {
        Point caretOffset = _textPainter.GetOffsetForCaret(position, default);
        var caretOffsetTranslated = new Point(caretOffset.X, caretOffset.Y + verticalOffset);
        return _textPainter.GetPositionForOffset(caretOffsetTranslated);
    }
}
