using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Media;
using Plumix.Painting;
using Plumix.Widgets;

// C#-only infrastructure: the paragraph engine Plumix uses when no Avalonia font manager exists.
// It reproduces the metrics of Flutter's `FlutterTest` font (every glyph one em wide, ascent 0.75 em,
// descent 0.25 em), so Flutter's own text layout tests port with their expected values. Line breaking
// is a UAX #14 subset (spaces, hard breaks, placeholders, ideographs), bidi a simplified UAX #9 without
// explicit embeddings. No Dart source maps to this file.

namespace Plumix.UI;

/// A <see cref="Paragraph"/> laid out with the metrics of Flutter's `FlutterTest` font.
internal sealed class HeadlessParagraph : Paragraph
{
    private const double AscentRatio = 0.75;
    private const double DescentRatio = 0.25;
    private const double Epsilon = 1e-9;

    private readonly ParagraphContent _content;
    private readonly List<Cluster> _clusters = [];
    private readonly int _baseLevel;
    private readonly bool _strutEnabled;
    private readonly bool _forceStrutHeight;
    private readonly double _strutAscent;
    private readonly double _strutDescent;
    private readonly double _minIntrinsicWidth;
    private readonly double _maxIntrinsicWidth;
    private readonly List<Line> _lines = [];

    private bool _laidOut;
    private double _width;
    private double _height;
    private double _longestLine;
    private double _maxLineRight;
    private double _minLineLeft;
    private bool _didExceedMaxLines;

    public HeadlessParagraph(ParagraphContent content)
        : base(content.Text)
    {
        _content = content;
        _baseLevel = content.TextDirection == TextDirection.Rtl ? 1 : 0;
        BuildClusters();
        ResolveBidiLevels();
        MarkBreakOpportunities();
        (_strutEnabled, _strutAscent, _strutDescent, _forceStrutHeight) = ComputeStrut();
        (_minIntrinsicWidth, _maxIntrinsicWidth) = ComputeIntrinsicWidths();
    }

    public override double Width => _width;

    public override double Height => _height;

    public override double LongestLine => _longestLine;

    public override double MinIntrinsicWidth => _minIntrinsicWidth;

    public override double MaxIntrinsicWidth => _maxIntrinsicWidth;

    public override double AlphabeticBaseline => _lines.Count == 0 ? 0.0 : _lines[0].Baseline;

    public override double IdeographicBaseline =>
        _lines.Count == 0 ? 0.0 : _lines[0].Baseline + _lines[0].UnscaledDescent;

    public override bool DidExceedMaxLines => _didExceedMaxLines;

    public override int NumberOfLines => Text.Length == 0 ? 0 : _lines.Count;

    // -- Construction ------------------------------------------------------------

    private void BuildClusters()
    {
        string text = _content.Text;
        IReadOnlyList<ParagraphPlaceholder> placeholders = _content.Placeholders;
        int placeholderIndex = 0;
        int index = 0;
        while (index < text.Length)
        {
            ResolvedTextStyle style = _content.StyleAt(index);
            if (placeholderIndex < placeholders.Count && placeholders[placeholderIndex].Index == index)
            {
                ParagraphPlaceholder placeholder = placeholders[placeholderIndex];
                _clusters.Add(new Cluster(index, index + 1, placeholder.Width, style)
                {
                    Placeholder = placeholder,
                    FirstCodePoint = UnicodeText.ObjectReplacementCharacter,
                    Category = BidiCategory.Neutral,
                });
                placeholderIndex++;
                index++;
                continue;
            }

            int limit = _content.RunEndAt(index);
            if (placeholderIndex < placeholders.Count)
            {
                limit = Math.Min(limit, placeholders[placeholderIndex].Index);
            }

            int length = StringInfo.GetNextTextElementLength(text, index);
            length = Math.Clamp(length, 1, Math.Max(1, limit - index));
            int firstCodePoint = UnicodeText.CodePointAt(text, index);
            bool hardBreak = UnicodeText.IsHardBreak(firstCodePoint);
            double width = 0.0;
            if (!hardBreak)
            {
                int cursor = index;
                while (cursor < index + length)
                {
                    int codePoint = UnicodeText.CodePointAt(text, cursor);
                    if (!Rune.IsValid(codePoint) || !UnicodeText.IsZeroWidth(new Rune(codePoint)))
                    {
                        width += style.FontSize;
                    }

                    cursor += UnicodeText.CodePointLength(text, cursor);
                }

                width += style.LetterSpacing;
                if (firstCodePoint == ' ')
                {
                    width += style.WordSpacing;
                }
            }

            _clusters.Add(new Cluster(index, index + length, width, style)
            {
                HardBreak = hardBreak,
                HangingSpace = !hardBreak && UnicodeText.IsHangingSpace(firstCodePoint),
                FirstCodePoint = firstCodePoint,
                Category = hardBreak ? BidiCategory.Neutral : UnicodeText.GetBidiCategory(firstCodePoint),
            });
            index += length;
        }
    }

    private void ResolveBidiLevels()
    {
        int segmentStart = 0;
        for (int index = 0; index <= _clusters.Count; index++)
        {
            if (index < _clusters.Count && !_clusters[index].HardBreak)
            {
                continue;
            }

            ResolveBidiSegment(segmentStart, index);
            if (index < _clusters.Count)
            {
                _clusters[index].Level = _baseLevel;
            }

            segmentStart = index + 1;
        }
    }

    private void ResolveBidiSegment(int start, int end)
    {
        int length = end - start;
        if (length <= 0)
        {
            return;
        }

        BidiCategory embedding = _baseLevel == 1 ? BidiCategory.Right : BidiCategory.Left;
        var types = new BidiCategory[length];
        BidiCategory lastStrong = embedding;
        for (int index = 0; index < length; index++)
        {
            BidiCategory type = _clusters[start + index].Category;
            if (type == BidiCategory.Number && lastStrong == BidiCategory.Left)
            {
                type = BidiCategory.Left;
            }

            if (type is BidiCategory.Left or BidiCategory.Right)
            {
                lastStrong = type;
            }

            types[index] = type;
        }

        int cursor = 0;
        while (cursor < length)
        {
            if (types[cursor] != BidiCategory.Neutral)
            {
                cursor++;
                continue;
            }

            int runStart = cursor;
            while (cursor < length && types[cursor] == BidiCategory.Neutral)
            {
                cursor++;
            }

            BidiCategory previous = runStart == 0 ? embedding : StrongDirection(types[runStart - 1]);
            BidiCategory next = cursor == length ? embedding : StrongDirection(types[cursor]);
            BidiCategory resolved = previous == next ? previous : embedding;
            for (int index = runStart; index < cursor; index++)
            {
                types[index] = resolved;
            }
        }

        for (int index = 0; index < length; index++)
        {
            _clusters[start + index].Level = types[index] switch
            {
                BidiCategory.Left => _baseLevel == 0 ? 0 : 2,
                BidiCategory.Right => 1,
                _ => 2,
            };
        }
    }

    private static BidiCategory StrongDirection(BidiCategory type)
    {
        return type == BidiCategory.Number ? BidiCategory.Right : type;
    }

    private void MarkBreakOpportunities()
    {
        for (int index = 1; index < _clusters.Count; index++)
        {
            Cluster previous = _clusters[index - 1];
            Cluster current = _clusters[index];
            current.BreakBefore = !previous.HardBreak && !current.HardBreak && !current.HangingSpace && (
                previous.HangingSpace
                || previous.Placeholder is not null
                || current.Placeholder is not null
                || previous.FirstCodePoint == 0x200B
                || (previous.FirstCodePoint is '-' or 0x2010 && current.Category == BidiCategory.Left)
                || UnicodeText.IsIdeographicBreakClass(previous.FirstCodePoint)
                || UnicodeText.IsIdeographicBreakClass(current.FirstCodePoint));
        }
    }

    private (bool Enabled, double Ascent, double Descent, bool Force) ComputeStrut()
    {
        ParagraphStrutStyle? strut = _content.Style.StrutStyle;
        if (strut is null || !strut.IsEnabled)
        {
            return (false, 0.0, 0.0, false);
        }

        double fontSize = strut.FontSize ?? TextDefaults.DefaultFontSize;
        (double ascent, double descent) = VerticalMetrics(
            fontSize,
            strut.Height ?? TextDefaults.TextHeightNone,
            strut.LeadingDistribution);
        if (strut.Leading is { } leading)
        {
            double extra = leading * fontSize;
            ascent += extra / 2.0;
            descent += extra / 2.0;
        }

        return (true, ascent, descent, strut.ForceStrutHeight == true);
    }

    private (double Min, double Max) ComputeIntrinsicWidths()
    {
        double min = 0.0;
        double max = 0.0;
        double lineTotal = 0.0;
        double word = 0.0;
        double pendingSpaces = 0.0;
        foreach (Cluster cluster in _clusters)
        {
            if (cluster.HardBreak)
            {
                max = Math.Max(max, lineTotal);
                min = Math.Max(min, word);
                lineTotal = 0.0;
                word = 0.0;
                pendingSpaces = 0.0;
                continue;
            }

            lineTotal += cluster.Width;
            if (cluster.BreakBefore)
            {
                min = Math.Max(min, word);
                word = 0.0;
                pendingSpaces = 0.0;
            }

            if (cluster.HangingSpace)
            {
                pendingSpaces += cluster.Width;
                continue;
            }

            word += pendingSpaces + cluster.Width;
            pendingSpaces = 0.0;
        }

        return (Math.Max(min, word), Math.Max(max, lineTotal));
    }

    private (double Ascent, double Descent) VerticalMetrics(
        double fontSize,
        double height,
        TextLeadingDistribution? distribution)
    {
        double ascent = fontSize * AscentRatio;
        double descent = fontSize * DescentRatio;
        if (height != TextDefaults.TextHeightNone)
        {
            double total = height * fontSize;
            if ((distribution ?? _content.DefaultLeadingDistribution) == TextLeadingDistribution.Even)
            {
                double leading = total - fontSize;
                ascent += leading / 2.0;
                descent += leading / 2.0;
            }
            else
            {
                ascent = total * AscentRatio;
                descent = total * DescentRatio;
            }
        }

        return (ascent, descent);
    }

    // -- Layout ----------------------------------------------------------------------

    public override void Layout(ParagraphConstraints constraints)
    {
        _width = constraints.Width;
        _lines.Clear();
        _didExceedMaxLines = false;
        int? maxLines = _content.Style.MaxLines;
        string? ellipsis = string.IsNullOrEmpty(_content.Style.Ellipsis) ? null : _content.Style.Ellipsis;
        int count = _clusters.Count;
        int index = 0;
        while (index < count)
        {
            if (maxLines is { } limit && _lines.Count >= limit)
            {
                _didExceedMaxLines = true;
                break;
            }

            int hardEnd = index;
            while (hardEnd < count && !_clusters[hardEnd].HardBreak)
            {
                hardEnd++;
            }

            int segmentEnd = hardEnd < count ? hardEnd + 1 : count;
            int lineEnd = FitLine(index, segmentEnd);
            bool softBreak = lineEnd < segmentEnd;
            bool isLastAllowedLine = maxLines is { } allowed && _lines.Count == allowed - 1;
            if (ellipsis is not null && ((isLastAllowedLine && lineEnd < count) || (maxLines is null && softBreak)))
            {
                _lines.Add(CreateEllipsizedLine(index, hardEnd, ellipsis));
                _didExceedMaxLines = true;
                break;
            }

            _lines.Add(CreateLine(index, lineEnd));
            index = lineEnd;
        }

        bool endsWithHardBreak = count > 0 && _clusters[count - 1].HardBreak;
        if (!_didExceedMaxLines
            && (count == 0 || endsWithHardBreak)
            && (maxLines is not { } lineLimit || _lines.Count < lineLimit))
        {
            _lines.Add(new Line
            {
                FirstCluster = count,
                EndCluster = count,
                Start = Text.Length,
                End = Text.Length,
                ContentEnd = Text.Length,
            });
        }

        FinishLines();
        _laidOut = true;
    }

    /// Returns the cluster index the line starting at `start` ends at (exclusive).
    private int FitLine(int start, int segmentEnd)
    {
        if (!double.IsFinite(_width))
        {
            return segmentEnd;
        }

        double lineWidth = 0.0;
        double pendingSpaces = 0.0;
        bool hasContent = false;
        int lastBreak = -1;
        for (int index = start; index < segmentEnd; index++)
        {
            Cluster cluster = _clusters[index];
            if (cluster.HardBreak)
            {
                continue;
            }

            if (index > start && cluster.BreakBefore)
            {
                lastBreak = index;
            }

            if (cluster.HangingSpace)
            {
                pendingSpaces += cluster.Width;
                continue;
            }

            double candidate = lineWidth + pendingSpaces + cluster.Width;
            if (hasContent && candidate - _width > Epsilon)
            {
                return lastBreak > start ? lastBreak : index;
            }

            lineWidth = candidate;
            pendingSpaces = 0.0;
            hasContent = true;
        }

        return segmentEnd;
    }

    private Line CreateLine(int start, int end)
    {
        Cluster last = _clusters[end - 1];
        return new Line
        {
            FirstCluster = start,
            EndCluster = end,
            Start = _clusters[start].Start,
            End = last.End,
            ContentEnd = last.HardBreak ? last.Start : last.End,
            HardBreak = last.HardBreak,
        };
    }

    private Line CreateEllipsizedLine(int start, int contentEnd, string ellipsis)
    {
        double width = 0.0;
        double pendingSpaces = 0.0;
        int kept = start;
        ResolvedTextStyle ellipsisStyle = start < contentEnd ? _clusters[start].Style : _content.StyleAt(
            Math.Max(0, _clusters[start].Start - 1));
        for (int index = start; index < contentEnd; index++)
        {
            Cluster cluster = _clusters[index];
            if (cluster.HangingSpace)
            {
                pendingSpaces += cluster.Width;
                continue;
            }

            double candidate = width + pendingSpaces + cluster.Width;
            if (double.IsFinite(_width) && candidate + EllipsisWidth(ellipsis, cluster.Style) - _width > Epsilon)
            {
                break;
            }

            width = candidate;
            pendingSpaces = 0.0;
            kept = index + 1;
            ellipsisStyle = cluster.Style;
        }

        int startOffset = _clusters[start].Start;
        int endOffset = kept > start ? _clusters[kept - 1].End : startOffset;
        return new Line
        {
            FirstCluster = start,
            EndCluster = kept,
            Start = startOffset,
            End = endOffset,
            ContentEnd = endOffset,
            EllipsisStyle = ellipsisStyle,
            EllipsisWidth = EllipsisWidth(ellipsis, ellipsisStyle),
        };
    }

    private static double EllipsisWidth(string ellipsis, ResolvedTextStyle style)
    {
        double width = 0.0;
        int index = 0;
        while (index < ellipsis.Length)
        {
            int codePoint = UnicodeText.CodePointAt(ellipsis, index);
            if (!Rune.IsValid(codePoint) || !UnicodeText.IsZeroWidth(new Rune(codePoint)))
            {
                width += style.FontSize;
            }

            index += UnicodeText.CodePointLength(ellipsis, index);
        }

        return width + style.LetterSpacing;
    }

    private void FinishLines()
    {
        TextHeightBehavior behavior = _content.Style.TextHeightBehavior ?? new TextHeightBehavior();
        double top = 0.0;
        _longestLine = 0.0;
        _maxLineRight = double.NegativeInfinity;
        _minLineLeft = double.PositiveInfinity;
        for (int lineIndex = 0; lineIndex < _lines.Count; lineIndex++)
        {
            Line line = _lines[lineIndex];
            bool trimAscent = lineIndex == 0 && !behavior.ApplyHeightToFirstAscent;
            bool trimDescent = lineIndex == _lines.Count - 1 && !behavior.ApplyHeightToLastDescent;
            var metrics = new LineTextMetrics();
            for (int index = line.FirstCluster; index < line.EndCluster; index++)
            {
                Cluster cluster = _clusters[index];
                if (cluster.Placeholder is null)
                {
                    Accumulate(ref metrics, cluster.Style, trimAscent, trimDescent);
                }
            }

            if (line.EllipsisStyle is { } ellipsisStyle)
            {
                Accumulate(ref metrics, ellipsisStyle, trimAscent, trimDescent);
            }

            if (!metrics.HasText)
            {
                for (int index = line.FirstCluster; index < line.EndCluster; index++)
                {
                    Accumulate(ref metrics, _clusters[index].Style, trimAscent, trimDescent);
                }
            }

            if (!metrics.HasText)
            {
                ResolvedTextStyle emptyStyle = line.Start > 0
                    ? _content.StyleAt(line.Start - 1)
                    : _content.DefaultStyle;
                Accumulate(ref metrics, emptyStyle, trimAscent, trimDescent);
            }

            double ascent = metrics.Ascent;
            double descent = metrics.Descent;
            for (int index = line.FirstCluster; index < line.EndCluster; index++)
            {
                Cluster cluster = _clusters[index];
                if (cluster.Placeholder is not { } placeholder)
                {
                    continue;
                }

                (double placeholderAscent, double placeholderDescent) =
                    PlaceholderMetrics(placeholder, metrics.Ascent, metrics.Descent);
                cluster.PlaceholderAscent = placeholderAscent;
                ascent = Math.Max(ascent, placeholderAscent);
                descent = Math.Max(descent, placeholderDescent);
            }

            if (_strutEnabled)
            {
                ascent = _forceStrutHeight ? _strutAscent : Math.Max(ascent, _strutAscent);
                descent = _forceStrutHeight ? _strutDescent : Math.Max(descent, _strutDescent);
            }

            line.Top = top;
            line.Ascent = ascent;
            line.Descent = descent;
            line.UnscaledAscent = metrics.UnscaledAscent;
            line.UnscaledDescent = metrics.UnscaledDescent;
            line.Baseline = top + ascent;
            top += ascent + descent;
            PositionLine(line, lineIndex == _lines.Count - 1);
            _longestLine = Math.Max(_longestLine, line.ContentWidth);
        }

        _height = top;
    }

    private void Accumulate(ref LineTextMetrics metrics, ResolvedTextStyle style, bool trimAscent, bool trimDescent)
    {
        double fontSize = style.FontSize;
        (double ascent, double descent) = VerticalMetrics(fontSize, style.Height, style.LeadingDistribution);
        if (trimAscent)
        {
            ascent = fontSize * AscentRatio;
        }

        if (trimDescent)
        {
            descent = fontSize * DescentRatio;
        }

        metrics.Ascent = metrics.HasText ? Math.Max(metrics.Ascent, ascent) : ascent;
        metrics.Descent = metrics.HasText ? Math.Max(metrics.Descent, descent) : descent;
        metrics.UnscaledAscent = Math.Max(metrics.UnscaledAscent, fontSize * AscentRatio);
        metrics.UnscaledDescent = Math.Max(metrics.UnscaledDescent, fontSize * DescentRatio);
        metrics.HasText = true;
    }

    private static (double Ascent, double Descent) PlaceholderMetrics(
        ParagraphPlaceholder placeholder,
        double textAscent,
        double textDescent)
    {
        double height = placeholder.Height;
        switch (placeholder.Alignment)
        {
            case PlaceholderAlignment.Baseline:
                return (placeholder.BaselineOffset, height - placeholder.BaselineOffset);
            case PlaceholderAlignment.AboveBaseline:
                return (height, 0.0);
            case PlaceholderAlignment.BelowBaseline:
                return (0.0, height);
            case PlaceholderAlignment.Top:
                return (textAscent, height - textAscent);
            case PlaceholderAlignment.Middle:
                double middle = (textAscent - textDescent) / 2.0;
                return (middle + (height / 2.0), (height / 2.0) - middle);
            default:
                return (height - textDescent, textDescent);
        }
    }

    private void PositionLine(Line line, bool isLastLine)
    {
        int trailingStart = line.EndCluster;
        while (trailingStart > line.FirstCluster
               && (_clusters[trailingStart - 1].HardBreak || _clusters[trailingStart - 1].HangingSpace))
        {
            trailingStart--;
        }

        var sequence = new List<Placed>();
        double contentWidth = 0.0;
        double hangWidth = 0.0;
        int justifiableSpaces = 0;
        for (int index = line.FirstCluster; index < line.EndCluster; index++)
        {
            Cluster cluster = _clusters[index];
            bool trailing = index >= trailingStart;
            if (trailing)
            {
                hangWidth += cluster.Width;
            }
            else
            {
                contentWidth += cluster.Width;
                if (cluster.HangingSpace)
                {
                    justifiableSpaces++;
                }
            }

            if (index == trailingStart && line.EllipsisStyle is not null)
            {
                sequence.Add(EllipsisItem(line));
            }

            sequence.Add(new Placed
            {
                Cluster = cluster,
                Width = cluster.Width,
                Level = trailing ? _baseLevel : cluster.Level,
                Style = cluster.Style,
            });
        }

        if (line.EllipsisStyle is not null && trailingStart == line.EndCluster)
        {
            sequence.Add(EllipsisItem(line));
        }

        if (line.EllipsisStyle is not null)
        {
            contentWidth += line.EllipsisWidth;
        }

        List<Placed> visual = Reorder(sequence);
        bool ltr = _baseLevel == 0;
        TextAlign align = _content.Style.TextAlign ?? TextAlign.Start;
        double x0 = 0.0;
        double extraPerSpace = 0.0;
        if (double.IsFinite(_width))
        {
            bool justify = align == TextAlign.Justify
                           && !line.HardBreak
                           && !isLastLine
                           && justifiableSpaces > 0
                           && _width > contentWidth;
            if (justify)
            {
                extraPerSpace = (_width - contentWidth) / justifiableSpaces;
                contentWidth = _width;
            }
            else
            {
                x0 = align switch
                {
                    TextAlign.Left => 0.0,
                    TextAlign.Right => _width - contentWidth,
                    TextAlign.Center => (_width - contentWidth) / 2.0,
                    TextAlign.End => ltr ? _width - contentWidth : 0.0,
                    _ => ltr ? 0.0 : _width - contentWidth,
                };
            }
        }

        double x = ltr ? x0 : x0 - hangWidth;
        _minLineLeft = Math.Min(_minLineLeft, x);
        foreach (Placed item in visual)
        {
            item.X = x;
            bool trailing = item.Cluster is { } cluster && _clusters.IndexOf(cluster) >= trailingStart;
            if (extraPerSpace > 0.0 && item.Cluster is { HangingSpace: true } && !trailing)
            {
                item.Width += extraPerSpace;
            }

            x += item.Width;
        }

        _maxLineRight = Math.Max(_maxLineRight, x);
        line.Left = x0;
        line.ContentWidth = contentWidth;
        line.Visual = visual;
    }

    private static Placed EllipsisItem(Line line)
    {
        return new Placed
        {
            IsEllipsis = true,
            Width = line.EllipsisWidth,
            Style = line.EllipsisStyle!,
        };
    }

    private List<Placed> Reorder(List<Placed> sequence)
    {
        var visual = new List<Placed>(sequence);
        if (visual.Count < 2)
        {
            return visual;
        }

        foreach (Placed item in visual)
        {
            if (item.IsEllipsis)
            {
                item.Level = _baseLevel;
            }
        }

        int maxLevel = visual.Max(item => item.Level);
        int minLevel = visual.Min(item => item.Level);
        int lowestOdd = minLevel % 2 == 1 ? minLevel : minLevel + 1;
        for (int level = maxLevel; level >= lowestOdd; level--)
        {
            int index = 0;
            while (index < visual.Count)
            {
                if (visual[index].Level < level)
                {
                    index++;
                    continue;
                }

                int runEnd = index;
                while (runEnd < visual.Count && visual[runEnd].Level >= level)
                {
                    runEnd++;
                }

                visual.Reverse(index, runEnd - index);
                index = runEnd;
            }
        }

        return visual;
    }

    // -- Queries -------------------------------------------------------------------------

    public override IReadOnlyList<TextBox> GetBoxesForRange(
        int start,
        int end,
        BoxHeightStyle boxHeightStyle = BoxHeightStyle.Tight,
        BoxWidthStyle boxWidthStyle = BoxWidthStyle.Tight)
    {
        var boxes = new List<TextBox>();
        if (!_laidOut || start >= end)
        {
            return boxes;
        }

        foreach (Line line in _lines)
        {
            int lineFirstBox = boxes.Count;
            TextBox? current = null;
            ResolvedTextStyle? currentStyle = null;
            bool currentIsPlaceholder = false;
            foreach (Placed item in line.Visual)
            {
                if (item.IsEllipsis || item.Cluster is not { } cluster || cluster.End <= start || cluster.Start >= end)
                {
                    Flush();
                    continue;
                }

                (double top, double bottom) = BoxVertical(line, item, boxHeightStyle);
                TextDirection direction = item.Level % 2 == 1 ? TextDirection.Rtl : TextDirection.Ltr;
                bool isPlaceholder = cluster.Placeholder is not null;
                if (current is { } box
                    && !isPlaceholder
                    && !currentIsPlaceholder
                    && Equals(currentStyle, cluster.Style)
                    && box.Direction == direction
                    && Math.Abs(box.Right - item.X) < Epsilon
                    && box.Top == top
                    && box.Bottom == bottom)
                {
                    current = box with { Right = item.X + item.Width };
                    continue;
                }

                Flush();
                current = new TextBox(item.X, top, item.X + item.Width, bottom, direction);
                currentStyle = cluster.Style;
                currentIsPlaceholder = isPlaceholder;
            }

            Flush();
            if (boxWidthStyle == BoxWidthStyle.Max && boxes.Count > lineFirstBox)
            {
                AddMaxWidthBoxes(boxes, lineFirstBox, start < line.Start, end > line.End);
            }

            void Flush()
            {
                if (current is { } finished)
                {
                    boxes.Add(finished);
                }

                current = null;
                currentStyle = null;
                currentIsPlaceholder = false;
            }
        }

        return boxes;
    }

    private void AddMaxWidthBoxes(List<TextBox> boxes, int lineFirstBox, bool startedBefore, bool continuesAfter)
    {
        TextBox first = boxes[lineFirstBox];
        TextBox last = boxes[^1];
        bool ltr = _baseLevel == 0;
        bool fillRight = ltr ? continuesAfter : startedBefore;
        bool fillLeft = ltr ? startedBefore : continuesAfter;
        if (fillRight && last.Right < _maxLineRight - Epsilon)
        {
            boxes.Add(new TextBox(last.Right, last.Top, _maxLineRight, last.Bottom, last.Direction));
        }

        if (fillLeft && first.Left > _minLineLeft + Epsilon)
        {
            boxes.Insert(lineFirstBox, new TextBox(_minLineLeft, first.Top, first.Left, first.Bottom, first.Direction));
        }
    }

    private (double Top, double Bottom) BoxVertical(Line line, Placed item, BoxHeightStyle boxHeightStyle)
    {
        double lineTop = line.Top;
        double lineBottom = line.Top + line.Ascent + line.Descent;
        if (item.Cluster?.Placeholder is { } placeholder)
        {
            if (boxHeightStyle is BoxHeightStyle.Tight or BoxHeightStyle.Strut)
            {
                double placeholderTop = line.Baseline - item.Cluster.PlaceholderAscent;
                return (placeholderTop, placeholderTop + placeholder.Height);
            }

            return (lineTop, lineBottom);
        }

        double fontSize = item.Style.FontSize;
        (double Top, double Bottom) tight = (
            line.Baseline - (fontSize * AscentRatio),
            line.Baseline + (fontSize * DescentRatio));
        return boxHeightStyle switch
        {
            BoxHeightStyle.Tight => tight,
            BoxHeightStyle.Strut => _strutEnabled
                ? (line.Baseline - _strutAscent, line.Baseline + _strutDescent)
                : tight,
            _ => (lineTop, lineBottom),
        };
    }

    public override IReadOnlyList<TextBox> GetBoxesForPlaceholders()
    {
        var boxes = new List<TextBox>();
        if (!_laidOut)
        {
            return boxes;
        }

        var placed = new List<(int Index, TextBox Box)>();
        foreach (Line line in _lines)
        {
            foreach (Placed item in line.Visual)
            {
                if (item.Cluster is { Placeholder: { } placeholder } cluster)
                {
                    double top = line.Baseline - cluster.PlaceholderAscent;
                    TextDirection direction = item.Level % 2 == 1 ? TextDirection.Rtl : TextDirection.Ltr;
                    placed.Add((cluster.Start, new TextBox(
                        item.X,
                        top,
                        item.X + item.Width,
                        top + placeholder.Height,
                        direction)));
                }
            }
        }

        placed.Sort((a, b) => a.Index.CompareTo(b.Index));
        boxes.AddRange(placed.Select(entry => entry.Box));
        return boxes;
    }

    public override TextPosition GetPositionForOffset(Point offset)
    {
        if (!_laidOut || _lines.Count == 0)
        {
            return new TextPosition(0);
        }

        Line line = LineForY(offset.Y);
        List<Placed> items = line.Visual.Where(item => item.IsEllipsis || !item.Cluster!.HardBreak).ToList();
        if (items.Count == 0)
        {
            return new TextPosition(line.Start);
        }

        Placed first = items[0];
        Placed last = items[^1];
        if (offset.X < first.X)
        {
            return LeftEdgePosition(first, line);
        }

        foreach (Placed item in items)
        {
            if (offset.X < item.X + item.Width)
            {
                double middle = item.X + (item.Width / 2.0);
                return offset.X < middle ? LeftEdgePosition(item, line) : RightEdgePosition(item, line);
            }
        }

        return RightEdgePosition(last, line);
    }

    private static TextPosition LeftEdgePosition(Placed item, Line line)
    {
        if (item.IsEllipsis)
        {
            return new TextPosition(line.End, TextAffinity.Upstream);
        }

        Cluster cluster = item.Cluster!;
        return item.Level % 2 == 0
            ? new TextPosition(cluster.Start)
            : new TextPosition(cluster.End, TextAffinity.Upstream);
    }

    private static TextPosition RightEdgePosition(Placed item, Line line)
    {
        if (item.IsEllipsis)
        {
            return new TextPosition(line.End, TextAffinity.Upstream);
        }

        Cluster cluster = item.Cluster!;
        return item.Level % 2 == 0
            ? new TextPosition(cluster.End, TextAffinity.Upstream)
            : new TextPosition(cluster.Start);
    }

    private Line LineForY(double y)
    {
        foreach (Line line in _lines)
        {
            if (y < line.Top + line.Ascent + line.Descent)
            {
                return line;
            }
        }

        return _lines[^1];
    }

    public override GlyphInfo? GetGlyphInfoAt(int codeUnitOffset)
    {
        if (!_laidOut || codeUnitOffset < 0)
        {
            return null;
        }

        foreach (Line line in _lines)
        {
            foreach (Placed item in line.Visual)
            {
                if (item.Cluster is { } cluster && cluster.Start <= codeUnitOffset && codeUnitOffset < cluster.End)
                {
                    return GlyphInfoFor(line, item);
                }
            }
        }

        return null;
    }

    public override GlyphInfo? GetClosestGlyphInfoForOffset(Point offset)
    {
        if (!_laidOut)
        {
            return null;
        }

        Line? bestLine = null;
        double bestDistance = double.PositiveInfinity;
        foreach (Line line in _lines)
        {
            if (!line.Visual.Any(item => item.Cluster is not null))
            {
                continue;
            }

            double top = line.Top;
            double bottom = line.Top + line.Ascent + line.Descent;
            double distance = offset.Y < top ? top - offset.Y : offset.Y >= bottom ? offset.Y - bottom : 0.0;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestLine = line;
            }
        }

        if (bestLine is null)
        {
            return null;
        }

        List<Placed> items = bestLine.Visual
            .Where(item => item.Cluster is { HardBreak: false })
            .ToList();
        if (items.Count == 0)
        {
            items = bestLine.Visual.Where(item => item.Cluster is not null).ToList();
        }

        Placed? best = null;
        double bestHorizontal = double.PositiveInfinity;
        foreach (Placed item in items)
        {
            double left = item.X;
            double right = item.X + item.Width;
            double distance = offset.X < left ? left - offset.X : offset.X >= right ? offset.X - right : 0.0;
            if (distance < bestHorizontal)
            {
                bestHorizontal = distance;
                best = item;
            }
        }

        return best is null ? null : GlyphInfoFor(bestLine, best);
    }

    private static GlyphInfo GlyphInfoFor(Line line, Placed item)
    {
        Cluster cluster = item.Cluster!;
        TextDirection direction = item.Level % 2 == 1 ? TextDirection.Rtl : TextDirection.Ltr;
        if (cluster.Placeholder is { } placeholder)
        {
            double top = line.Baseline - cluster.PlaceholderAscent;
            TextRange range = placeholder.Width == 0.0 && placeholder.Height == 0.0
                ? new TextRange(0, 0)
                : new TextRange(cluster.Start, cluster.End);
            return new GlyphInfo(new Rect(item.X, top, item.Width, placeholder.Height), range, direction);
        }

        double fontSize = item.Style.FontSize;
        var bounds = new Rect(
            new Point(item.X, line.Baseline - (fontSize * AscentRatio)),
            new Point(item.X + item.Width, line.Baseline + (fontSize * DescentRatio)));
        return new GlyphInfo(bounds, new TextRange(cluster.Start, cluster.End), direction);
    }

    private protected override TextRange GetLineRange(int offset)
    {
        if (!_laidOut)
        {
            return TextRange.Empty;
        }

        foreach (Line line in _lines)
        {
            if (line.Start <= offset && offset <= line.ContentEnd)
            {
                return new TextRange(line.Start, line.ContentEnd);
            }
        }

        return TextRange.Empty;
    }

    public override IReadOnlyList<LineMetrics> ComputeLineMetrics()
    {
        var metrics = new List<LineMetrics>();
        for (int index = 0; index < NumberOfLines; index++)
        {
            metrics.Add(MetricsFor(index));
        }

        return metrics;
    }

    public override LineMetrics? GetLineMetricsAt(int lineNumber)
    {
        return _laidOut && lineNumber >= 0 && lineNumber < NumberOfLines ? MetricsFor(lineNumber) : null;
    }

    private LineMetrics MetricsFor(int index)
    {
        Line line = _lines[index];
        return new LineMetrics(
            HardBreak: line.HardBreak || index == _lines.Count - 1,
            Ascent: line.Ascent,
            Descent: line.Descent,
            UnscaledAscent: line.UnscaledAscent,
            Height: line.Ascent + line.Descent,
            Width: line.ContentWidth,
            Left: line.Left,
            Baseline: line.Baseline,
            LineNumber: index);
    }

    public override int? GetLineNumberAt(int codeUnitOffset)
    {
        if (!_laidOut || codeUnitOffset < 0)
        {
            return null;
        }

        for (int index = 0; index < _lines.Count; index++)
        {
            Line line = _lines[index];
            if (line.Start <= codeUnitOffset && codeUnitOffset < line.End)
            {
                return index;
            }
        }

        return null;
    }

    internal override Action<DrawingContext>? CreateDrawAction(Point offset)
    {
        // No font manager: there are no glyphs to draw.
        return null;
    }

    // -- Model -----------------------------------------------------------------------

    private sealed class Cluster(int start, int end, double width, ResolvedTextStyle style)
    {
        public int Start { get; } = start;

        public int End { get; } = end;

        public double Width { get; } = width;

        public ResolvedTextStyle Style { get; } = style;

        public ParagraphPlaceholder? Placeholder { get; init; }

        public bool HardBreak { get; init; }

        public bool HangingSpace { get; init; }

        public int FirstCodePoint { get; init; }

        public BidiCategory Category { get; init; }

        public int Level { get; set; }

        public bool BreakBefore { get; set; }

        /// The distance from the line baseline to the top of a placeholder, set during layout.
        public double PlaceholderAscent { get; set; }
    }

    private sealed class Line
    {
        public int FirstCluster { get; init; }

        public int EndCluster { get; init; }

        public int Start { get; init; }

        public int End { get; init; }

        public int ContentEnd { get; init; }

        public bool HardBreak { get; init; }

        public ResolvedTextStyle? EllipsisStyle { get; init; }

        public double EllipsisWidth { get; init; }

        public double Top { get; set; }

        public double Ascent { get; set; }

        public double Descent { get; set; }

        public double UnscaledAscent { get; set; }

        public double UnscaledDescent { get; set; }

        public double Baseline { get; set; }

        public double Left { get; set; }

        public double ContentWidth { get; set; }

        public List<Placed> Visual { get; set; } = [];
    }

    private sealed class Placed
    {
        public Cluster? Cluster { get; init; }

        public bool IsEllipsis { get; init; }

        public double X { get; set; }

        public double Width { get; set; }

        public int Level { get; set; }

        public required ResolvedTextStyle Style { get; init; }
    }

    private struct LineTextMetrics
    {
        public bool HasText;
        public double Ascent;
        public double Descent;
        public double UnscaledAscent;
        public double UnscaledDescent;
    }
}
