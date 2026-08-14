using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity sources:
// flutter/packages/flutter/lib/src/painting/text_painter.dart
// flutter/packages/flutter/lib/src/rendering/paragraph.dart

namespace Plumix;

/// Defines how the bounds of a selection box are extended vertically.
public enum BoxHeightStyle
{
    /// Provide tight bounding boxes that fit heights per run.
    Tight,

    /// The height of the boxes will be the maximum height of all runs in the line.
    Max,

    /// Extends the top and bottom edge of the bounds to fully cover any line spacing.
    IncludeLineSpacingMiddle,

    /// Extends the top edge of the bounds to fully cover any line spacing.
    IncludeLineSpacingTop,

    /// Extends the bottom edge of the bounds to fully cover any line spacing.
    IncludeLineSpacingBottom,

    /// Calculates the height of the boxes as if the text was laid out with the strut.
    Strut,
}

/// Defines how the bounds of a selection box are extended horizontally.
public enum BoxWidthStyle
{
    /// Provide tight bounding boxes that fit widths to the runs.
    Tight,

    /// Adds up to two additional boxes to fill the line's leading and trailing space.
    Max,
}

/// A rectangle enclosing a run of text, with the direction that run flows in.
public readonly record struct TextBox(double Left, double Top, double Right, double Bottom, TextDirection Direction)
{
    public static TextBox FromRect(Rect rect, TextDirection direction)
    {
        return new TextBox(rect.Left, rect.Top, rect.Right, rect.Bottom, direction);
    }

    public Rect ToRect() => new(new Point(Left, Top), new Point(Right, Bottom));
}

/// The metrics of a single laid-out line of text.
public readonly record struct LineMetrics(
    int LineNumber,
    bool Hardbreak,
    double Ascent,
    double Descent,
    double Height,
    double Width,
    double Left,
    double Baseline);

public sealed partial class RenderParagraph
{
    /// The height a line of text is expected to occupy with the current style.
    public double PreferredLineHeight
    {
        get
        {
            if (_layout is { TextLines.Count: > 0 })
            {
                double height = _layout.TextLines[0].Height;
                if (height > 0)
                {
                    return height;
                }
            }

            return EstimatedLineHeight;
        }
    }

    /// Returns the offset at which to paint the caret for the given `position`.
    public Point GetOffsetForCaret(TextPosition position, Rect caretPrototype)
    {
        int offset = ClampOffset(position.Offset);
        if (_layout is null)
        {
            (int line, int column) = EstimateLineAndColumn(offset);
            return new Point(column * EstimatedCharacterWidth, line * EstimatedLineHeight);
        }

        Rect glyph = _layout.HitTestTextPosition(offset);
        return glyph.Position;
    }

    /// Returns the strut-independent height of the caret at `position`.
    public double GetFullHeightForCaret(TextPosition position)
    {
        if (_layout is null)
        {
            return PreferredLineHeight;
        }

        double height = _layout.HitTestTextPosition(ClampOffset(position.Offset)).Height;
        return height > 0 ? height : PreferredLineHeight;
    }

    /// The bottom-left corner of the caret box at `position`, matching Dart's
    /// private `_getOffsetForPosition`.
    internal Point GetPositionOffset(TextPosition position)
    {
        Point caret = GetOffsetForCaret(position, default);
        return caret.WithY(caret.Y + GetFullHeightForCaret(position));
    }

    /// Returns the text position closest to the given local `offset`.
    public TextPosition GetPositionForOffset(Point offset)
    {
        string plain = PlainText;
        if (_layout is null)
        {
            return new TextPosition(EstimateTextPosition(offset, plain));
        }

        TextHitTestResult hit = _layout.HitTestPoint(offset);
        int position = hit.TextPosition + (hit.IsTrailing ? 1 : 0);
        return new TextPosition(Math.Clamp(position, 0, plain.Length));
    }

    /// Returns a list of rects that bound the given `selection`.
    public IReadOnlyList<TextBox> GetBoxesForSelection(
        TextSelection selection,
        BoxHeightStyle boxHeightStyle = BoxHeightStyle.Tight,
        BoxWidthStyle boxWidthStyle = BoxWidthStyle.Tight)
    {
        string plain = PlainText;
        int start = Math.Clamp(selection.Start, 0, plain.Length);
        int end = Math.Clamp(selection.End, 0, plain.Length);
        if (end <= start)
        {
            return [];
        }

        if (_layout is null)
        {
            return EstimateBoxesForSelection(plain, start, end);
        }

        var boxes = new List<TextBox>();
        double lineTop = 0.0;
        foreach (TextLine line in _layout.TextLines)
        {
            int lineStart = line.FirstTextSourceIndex;
            int lineEnd = lineStart + line.Length;
            if (lineEnd <= start)
            {
                lineTop += line.Height;
                continue;
            }

            if (lineStart >= end)
            {
                break;
            }

            int boxStart = Math.Max(start, lineStart);
            int boxEnd = Math.Min(end, lineEnd - line.NewLineLength);
            if (boxEnd > boxStart)
            {
                AppendLineBoxes(boxes, line, lineTop, boxStart, boxEnd - boxStart, boxHeightStyle);
                if (boxWidthStyle == BoxWidthStyle.Max && end > lineEnd - line.NewLineLength)
                {
                    AppendTrailingLineBox(boxes, line, lineTop, boxHeightStyle);
                }
            }

            lineTop += line.Height;
        }

        return boxes;
    }

    /// Returns the text range of the word enclosing `position`.
    public TextRange GetWordBoundary(TextPosition position)
    {
        string plain = PlainText;
        if (plain.Length == 0)
        {
            return new TextRange(0, 0);
        }

        int offset = Math.Clamp(position.Offset, 0, plain.Length - 1);
        int characterClass = ClassifyCharacter(plain[offset]);
        int start = offset;
        int end = offset;
        while (start > 0 && ClassifyCharacter(plain[start - 1]) == characterClass)
        {
            start -= 1;
        }

        while (end < plain.Length && ClassifyCharacter(plain[end]) == characterClass)
        {
            end += 1;
        }

        return new TextRange(start, end);
    }

    /// Returns the content range of the line enclosing `position`, excluding the
    /// trailing line terminator.
    public TextRange GetLineBoundary(TextPosition position)
    {
        string plain = PlainText;
        if (_layout is null)
        {
            return EstimateLineBoundary(plain, Math.Clamp(position.Offset, 0, plain.Length));
        }

        int offset = Math.Clamp(position.Offset, 0, plain.Length);
        int lineStart = 0;
        foreach (TextLine line in _layout.TextLines)
        {
            int start = line.FirstTextSourceIndex;
            int end = start + line.Length;
            lineStart = start;
            if (offset < end || end >= plain.Length)
            {
                return new TextRange(start, Math.Max(start, end - line.NewLineLength));
            }
        }

        return new TextRange(lineStart, plain.Length);
    }

    /// Returns the text position one line above `position`.
    public TextPosition GetTextPositionAbove(TextPosition position)
    {
        return GetTextPositionVertical(position, -0.5 * PreferredLineHeight);
    }

    /// Returns the text position one line below `position`.
    public TextPosition GetTextPositionBelow(TextPosition position)
    {
        return GetTextPositionVertical(position, 1.5 * PreferredLineHeight);
    }

    /// Returns the metrics of every laid-out line.
    public IReadOnlyList<LineMetrics> ComputeLineMetrics()
    {
        if (_layout is null)
        {
            return EstimateLineMetrics(PlainText);
        }

        var metrics = new List<LineMetrics>(_layout.TextLines.Count);
        double lineTop = 0.0;
        for (int index = 0; index < _layout.TextLines.Count; index += 1)
        {
            TextLine line = _layout.TextLines[index];
            metrics.Add(new LineMetrics(
                LineNumber: index,
                Hardbreak: line.NewLineLength > 0,
                Ascent: line.Baseline,
                Descent: Math.Max(0.0, line.Height - line.Baseline),
                Height: line.Height,
                Width: line.Width,
                Left: line.Start,
                Baseline: lineTop + line.Baseline));
            lineTop += line.Height;
        }

        return metrics;
    }

    /// A [TextBoundary] that walks whole words, skipping the separators between
    /// them, matching Dart's `TextPainter.wordBoundaries.moveByWordBoundary`.
    public TextBoundary MoveByWordBoundary => new WordMovementBoundary(PlainText);

    private TextPosition GetTextPositionVertical(TextPosition position, double dy)
    {
        Point caret = GetOffsetForCaret(position, default);
        return GetPositionForOffset(new Point(caret.X, caret.Y + dy));
    }

    private void AppendLineBoxes(
        List<TextBox> boxes,
        TextLine line,
        double lineTop,
        int start,
        int length,
        BoxHeightStyle boxHeightStyle)
    {
        foreach (TextBounds bounds in line.GetTextBounds(start, length))
        {
            Rect rect = bounds.Rectangle;
            double top = boxHeightStyle == BoxHeightStyle.Tight ? lineTop + Math.Max(0.0, rect.Y) : lineTop;
            double height = boxHeightStyle == BoxHeightStyle.Tight && rect.Height > 0 ? rect.Height : line.Height;
            var direction = bounds.FlowDirection == FlowDirection.RightToLeft
                ? TextDirection.Rtl
                : TextDirection.Ltr;
            boxes.Add(TextBox.FromRect(new Rect(rect.X, top, rect.Width, height), direction));
        }
    }

    private void AppendTrailingLineBox(
        List<TextBox> boxes,
        TextLine line,
        double lineTop,
        BoxHeightStyle boxHeightStyle)
    {
        if (boxes.Count == 0)
        {
            return;
        }

        TextBox last = boxes[^1];
        double lineRight = line.Start + line.WidthIncludingTrailingWhitespace;
        if (lineRight <= last.Right + 0.01)
        {
            return;
        }

        double top = boxHeightStyle == BoxHeightStyle.Tight ? last.Top : lineTop;
        double bottom = boxHeightStyle == BoxHeightStyle.Tight ? last.Bottom : lineTop + line.Height;
        boxes.Add(new TextBox(last.Right, top, lineRight, bottom, last.Direction));
    }

    private int ClampOffset(int offset) => Math.Clamp(offset, 0, PlainText.Length);

    /// Classifies a character the way ICU's word iterator groups runs: letters and
    /// digits form words, whitespace forms separators, everything else is symbolic.
    private static int ClassifyCharacter(char value)
    {
        if (char.IsLetterOrDigit(value) || value == '_')
        {
            return 0;
        }

        return ITextLayoutMetrics.IsWhitespace(value) ? 1 : 2;
    }

    // -- Estimation fallbacks --------------------------------------------------
    //
    // Hosts without a font manager (including the test harness) leave `_layout`
    // null; every text-metric query then falls back to the same monospaced
    // estimate `EstimateTextPosition` already uses, so the queries round-trip.

    private double EstimatedCharacterWidth
    {
        get
        {
            TextStyle style = RootStyle;
            double fontSize = _textScaler.Scale(style.FontSize ?? TextDefaults.DefaultFontSize);
            return Math.Max(1.0, (fontSize * 0.55) + (style.LetterSpacing ?? 0));
        }
    }

    private (int Line, int Column) EstimateLineAndColumn(int offset)
    {
        string plain = PlainText;
        int line = 0;
        int lineStart = 0;
        for (int index = 0; index < offset && index < plain.Length; index += 1)
        {
            if (plain[index] == '\n')
            {
                line += 1;
                lineStart = index + 1;
            }
        }

        return (line, offset - lineStart);
    }

    private IReadOnlyList<TextBox> EstimateBoxesForSelection(string plain, int start, int end)
    {
        double characterWidth = EstimatedCharacterWidth;
        double lineHeight = EstimatedLineHeight;
        var boxes = new List<TextBox>();
        int lineStart = 0;
        int line = 0;
        for (int index = 0; index <= plain.Length; index += 1)
        {
            bool isBreak = index == plain.Length || plain[index] == '\n';
            if (!isBreak)
            {
                continue;
            }

            int boxStart = Math.Max(start, lineStart);
            int boxEnd = Math.Min(end, index);
            if (boxEnd > boxStart)
            {
                boxes.Add(new TextBox(
                    (boxStart - lineStart) * characterWidth,
                    line * lineHeight,
                    (boxEnd - lineStart) * characterWidth,
                    (line + 1) * lineHeight,
                    _textDirection));
            }

            lineStart = index + 1;
            line += 1;
        }

        return boxes;
    }

    private static TextRange EstimateLineBoundary(string plain, int offset)
    {
        int start = plain.LastIndexOf('\n', Math.Max(0, Math.Min(offset, plain.Length) - 1));
        start = start < 0 ? 0 : start + 1;
        int end = plain.IndexOf('\n', Math.Min(offset, plain.Length));
        return new TextRange(start, end < 0 ? plain.Length : end);
    }

    private IReadOnlyList<LineMetrics> EstimateLineMetrics(string plain)
    {
        double lineHeight = EstimatedLineHeight;
        double characterWidth = EstimatedCharacterWidth;
        string[] lines = plain.Split('\n');
        var metrics = new List<LineMetrics>(lines.Length);
        for (int index = 0; index < lines.Length; index += 1)
        {
            metrics.Add(new LineMetrics(
                LineNumber: index,
                Hardbreak: index < lines.Length - 1,
                Ascent: lineHeight * 0.8,
                Descent: lineHeight * 0.2,
                Height: lineHeight,
                Width: lines[index].Length * characterWidth,
                Left: 0.0,
                Baseline: (index * lineHeight) + (lineHeight * 0.8)));
        }

        return metrics;
    }

    private sealed class WordMovementBoundary : TextBoundary
    {
        private readonly string _text;

        public WordMovementBoundary(string text)
        {
            _text = text;
        }

        public override int? GetLeadingTextBoundaryAt(int position)
        {
            if (position < 0)
            {
                return null;
            }

            int index = Math.Min(position, _text.Length - 1);
            while (index >= 0 && ITextLayoutMetrics.IsWhitespace(_text[index]))
            {
                index -= 1;
            }

            while (index >= 0 && !ITextLayoutMetrics.IsWhitespace(_text[index]))
            {
                index -= 1;
            }

            return index + 1;
        }

        public override int? GetTrailingTextBoundaryAt(int position)
        {
            if (position >= _text.Length)
            {
                return null;
            }

            int index = Math.Max(0, position);
            while (index < _text.Length && ITextLayoutMetrics.IsWhitespace(_text[index]))
            {
                index += 1;
            }

            while (index < _text.Length && !ITextLayoutMetrics.IsWhitespace(_text[index]))
            {
                index += 1;
            }

            return index;
        }
    }
}
