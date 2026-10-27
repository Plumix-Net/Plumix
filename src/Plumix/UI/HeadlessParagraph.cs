using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Media;
using Plumix.Painting;
using Plumix.Widgets;

// C#-only infrastructure: the paragraph engine Plumix uses when no Avalonia font manager exists.
// It reproduces the metrics of Flutter's `FlutterTest` font (every glyph one em wide, ascent 0.75 em,
// descent 0.25 em, visible glyphs painted as solid em boxes), so Flutter's own text layout and
// pixel-reading tests port with their expected values. Line breaking
// is a UAX #14 subset (spaces, hard breaks, placeholders, ideographs), bidi a simplified UAX #9 without
// explicit embeddings. No Dart source maps to this file.

namespace Plumix.UI;

/// A <see cref="Paragraph"/> laid out with the metrics of Flutter's `FlutterTest` font.
internal sealed class HeadlessParagraph : Paragraph
{
    private const double AscentRatio = 0.75;
    private const double DescentRatio = 0.25;
    private const double Epsilon = 1e-9;
    private const int FlutterTestFontWeight = 400;

    private readonly ParagraphContent _content;
    private readonly List<Cluster> _clusters = [];
    private readonly int _baseLevel;
    private readonly bool _strutEnabled;
    private readonly bool _forceStrutHeight;
    private readonly double _strutAscent;
    private readonly double _strutDescent;
    private readonly double _strutLeadingHalf;
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
        _strutLeadingHalf = Math.Max(content.Style.StrutStyle?.Leading ?? 0.0, 0.0)
            * (content.Style.StrutStyle?.FontSize ?? TextDefaults.DefaultFontSize) / 2.0;
        (_minIntrinsicWidth, _maxIntrinsicWidth) = ComputeIntrinsicWidths();
    }

    public override double Width => _width;

    public override double Height => _height;

    public override double LongestLine => _longestLine;

    public override double MinIntrinsicWidth => _minIntrinsicWidth;

    public override double MaxIntrinsicWidth => _maxIntrinsicWidth;

    public override double AlphabeticBaseline => _lines.Count == 0 ? 0.0 : _lines[0].Baseline;

    public override double IdeographicBaseline =>
        _lines.Count == 0 ? 0.0 : _lines[0].Ascent + _lines[0].Descent;

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
            ShapedGlyph[] glyphs = [];
            if (!hardBreak)
            {
                glyphs = ShapeGlyphs(text, index, index + length, style.FontSize);
                // SkParagraph's `Run::addLetterSpacesEvenly` spaces every glyph of a cluster.
                double letterSpacing = LetterSpacingOf(firstCodePoint, style);
                foreach (ShapedGlyph glyph in glyphs)
                {
                    width += glyph.Advance + letterSpacing;
                }

                if (firstCodePoint == ' ')
                {
                    width += style.WordSpacing;
                }
            }

            _clusters.Add(new Cluster(index, index + length, width, style)
            {
                Glyphs = glyphs,
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
                || (previous.FirstCodePoint is '-' or 0x2010 or '/' && current.Category == BidiCategory.Left)
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
                // SkParagraph's TextWrapper::lookAhead: a word too long for a whole line stays where it
                // starts, broken between clusters, once more than one of its clusters fits here.
                bool tooLong = lastBreak > start && LookAheadWordWidth(lastBreak) - _width > Epsilon;
                return lastBreak > start && !(tooLong && index - lastBreak > 1) ? lastBreak : index;
            }

            lineWidth = candidate;
            pendingSpaces = 0.0;
            hasContent = true;
        }

        return segmentEnd;
    }

    /// SkParagraph leaves cursive scripts (Arabic, Syriac, N'Ko, Mongolian, ...) unspaced, since
    /// spacing would tear their joined letters apart.
    private static double LetterSpacingOf(int codePoint, ResolvedTextStyle style) =>
        IsCursiveScript(codePoint) ? 0.0 : style.LetterSpacing;

    private static bool IsCursiveScript(int codePoint) => codePoint switch
    {
        >= 0x0600 and <= 0x077F => true, // Arabic, Syriac, Arabic Supplement
        >= 0x07C0 and <= 0x07FF => true, // N'Ko
        >= 0x0840 and <= 0x08FF => true, // Mandaic, Syriac Supplement, Arabic Extended-B/-A
        >= 0x1800 and <= 0x18AF => true, // Mongolian
        >= 0xFB50 and <= 0xFDFF => true, // Arabic Presentation Forms-A
        >= 0xFE70 and <= 0xFEFF => true, // Arabic Presentation Forms-B
        >= 0x10D00 and <= 0x10D3F => true, // Hanifi Rohingya
        >= 0x1E900 and <= 0x1E95F => true, // Adlam
        _ => false,
    };

    /// The glyphs HarfBuzz shapes the grapheme cluster `[start, end)` into with `FlutterTest`, whose
    /// glyphs (`.notdef` included) are all one em wide. A non-spacing mark composes into the preceding
    /// glyph when the font maps the canonical composition (`e` + U+0301 is `é`); otherwise it is a
    /// glyph of its own, which the default, Arabic and Hebrew shapers zero (`zero_mark_widths_by_gdef`
    /// on the classes `hb_synthesize_glyph_classes` derives) unless no base precedes it, while the
    /// Indic, Khmer, Myanmar, Thai, Hangul and USE shapers keep its advance. Spacing marks, enclosing
    /// marks and unmapped code points are full-width glyphs; default ignorables are hidden (zero
    /// width), and the Thai shaper splits SARA AM into NIKHAHIT + SARA AA.
    private static ShapedGlyph[] ShapeGlyphs(string text, int start, int end, double fontSize)
    {
        var glyphs = new List<ShapedGlyph>(1);
        bool composing = true;
        int cursor = start;
        while (cursor < end)
        {
            int codePoint = UnicodeText.CodePointAt(text, cursor);
            if (codePoint is 0x0E33 or 0x0EB3)
            {
                glyphs.Add(new ShapedGlyph(codePoint == 0x0E33 ? 0x0E4D : 0x0ECD, fontSize));
                glyphs.Add(new ShapedGlyph(codePoint == 0x0E33 ? 0x0E32 : 0x0EB2, fontSize));
                composing = false;
            }
            else if (!Rune.IsValid(codePoint))
            {
                glyphs.Add(new ShapedGlyph(codePoint, fontSize));
                composing = false;
            }
            else if (IsHiddenDefaultIgnorable(new Rune(codePoint)))
            {
                glyphs.Add(new ShapedGlyph(codePoint, 0.0));
            }
            else if (Rune.GetUnicodeCategory(new Rune(codePoint)) == UnicodeCategory.NonSpacingMark
                     && BaseBefore(text, cursor) is { } baseCodePoint)
            {
                if (composing && glyphs.Count == 1 && Compose(glyphs[0].CodePoint, codePoint) is { } composed)
                {
                    glyphs[0] = glyphs[0] with { CodePoint = composed };
                }
                else
                {
                    bool zeroed = !IsComplexShaped(codePoint) && !IsComplexShaped(baseCodePoint);
                    glyphs.Add(new ShapedGlyph(codePoint, zeroed ? 0.0 : fontSize));
                    composing = false;
                }
            }
            else
            {
                glyphs.Add(new ShapedGlyph(codePoint, fontSize));
            }

            cursor += UnicodeText.CodePointLength(text, cursor);
        }

        return [.. glyphs];
    }

    /// Format and control characters (tab aside), and the non-spacing default ignorables (variation
    /// selectors, the combining grapheme joiner, Khmer's inherent vowels, Mongolian's free variation
    /// selectors), which HarfBuzz hides behind a zero-width glyph.
    private static bool IsHiddenDefaultIgnorable(Rune rune)
    {
        int value = rune.Value;
        return Rune.GetUnicodeCategory(rune) switch
        {
            UnicodeCategory.Format => true,
            UnicodeCategory.Control => value != '\t',
            _ => value is 0x034F or 0x17B4 or 0x17B5 or (>= 0x180B and <= 0x180F) or (>= 0xFE00 and <= 0xFE0F)
                or (>= 0xE0100 and <= 0xE01EF),
        };
    }

    /// The code point the marks before `index` attach to, or null when only marks precede them, so
    /// the first of them shapes as a base.
    private static int? BaseBefore(string text, int index)
    {
        int cursor = index;
        while (cursor > 0)
        {
            cursor -= cursor > 1 && char.IsSurrogatePair(text[cursor - 2], text[cursor - 1]) ? 2 : 1;
            int codePoint = UnicodeText.CodePointAt(text, cursor);
            if (!Rune.IsValid(codePoint)
                || Rune.GetUnicodeCategory(new Rune(codePoint)) != UnicodeCategory.NonSpacingMark)
            {
                return codePoint;
            }
        }

        return null;
    }

    /// The canonical composition of `first` and `mark` when `FlutterTest` maps it.
    private static int? Compose(int first, int mark)
    {
        if (!Rune.IsValid(first))
        {
            return null;
        }

        string composed = string.Concat(char.ConvertFromUtf32(first), char.ConvertFromUtf32(mark))
            .Normalize(NormalizationForm.FormC);
        int codePoint = UnicodeText.CodePointAt(composed, 0);
        bool single = UnicodeText.CodePointLength(composed, 0) == composed.Length;
        return single && ShapeOf(codePoint) != GlyphShape.NotDef ? codePoint : null;
    }

    /// Scripts HarfBuzz shapes with its Indic, Khmer, Myanmar, Thai, Hangul or Universal Shaping
    /// Engine shapers (`hb_ot_shaper_categorize` for a font without GSUB/GPOS), none of which zero
    /// mark advances for a font without mark classes.
    private static bool IsComplexShaped(int codePoint) => codePoint switch
    {
        >= 0x0900 and <= 0x0DFF => true, // Devanagari .. Sinhala
        >= 0x0E00 and <= 0x0EFF => true, // Thai, Lao
        >= 0x0F00 and <= 0x109F => true, // Tibetan, Myanmar
        >= 0x1100 and <= 0x11FF => true, // Hangul Jamo
        >= 0x1700 and <= 0x17FF => true, // Tagalog, Hanunoo, Buhid, Tagbanwa, Khmer
        >= 0x1900 and <= 0x1AAF => true, // Limbu, Tai Le, New Tai Lue, Khmer Symbols, Buginese, Tai Tham
        >= 0x1B00 and <= 0x1C4F => true, // Balinese, Sundanese, Batak, Lepcha
        >= 0x3130 and <= 0x318F => true, // Hangul Compatibility Jamo
        >= 0xA800 and <= 0xA82F => true, // Syloti Nagri
        >= 0xA880 and <= 0xA97F => true, // Saurashtra, Devanagari Ext, Kayah Li, Rejang, Hangul Jamo Ext-A
        >= 0xA980 and <= 0xAAFF => true, // Javanese, Myanmar Ext-B, Cham, Myanmar Ext-A, Tai Viet, Meetei Ext
        >= 0xABC0 and <= 0xABFF => true, // Meetei Mayek
        >= 0xAC00 and <= 0xD7FF => true, // Hangul Syllables, Hangul Jamo Extended-B
        >= 0x11000 and <= 0x11FFF => true, // Brahmi and the other supplementary Brahmic scripts
        _ => false,
    };

    /// The word length TextWrapper::lookAhead measures from `start`: it stops at whitespace and
    /// leaves out the cluster a soft break follows, such as the last cluster of the text.
    private double LookAheadWordWidth(int start)
    {
        double width = 0.0;
        for (int index = start; index < _clusters.Count; index++)
        {
            Cluster cluster = _clusters[index];
            if (cluster.HangingSpace || cluster.HardBreak)
            {
                break;
            }

            if (index + 1 == _clusters.Count || _clusters[index + 1].BreakBefore)
            {
                break;
            }

            width += cluster.Width;
        }

        return width;
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
        var previous = new LineTextMetrics();
        double previousAscent = 0.0;
        double previousDescent = 0.0;
        for (int lineIndex = 0; lineIndex < _lines.Count; lineIndex++)
        {
            Line line = _lines[lineIndex];
            bool trimAscent = lineIndex == 0 && !behavior.ApplyHeightToFirstAscent;
            bool trimDescent = lineIndex == _lines.Count - 1 && !behavior.ApplyHeightToLastDescent;
            // `metrics` sizes the line the way TextWrapper's stretches accumulate it; `reported` is the
            // `TextLine::fMaxRunMetrics` LineMetrics reports, which counts every run of the line.
            var metrics = new LineTextMetrics();
            var reported = new LineTextMetrics();
            for (int index = line.FirstCluster; index < line.EndCluster; index++)
            {
                Cluster cluster = _clusters[index];
                if (cluster.Placeholder is not null)
                {
                    continue;
                }

                Accumulate(ref reported, cluster.Style);
                // `TextStretch::extend` ignores a hard break's run ("as the Flutter does"); only
                // `TextStretch::startFrom` takes the run of the cluster a line starts with.
                if (!cluster.HardBreak || index == line.FirstCluster)
                {
                    Accumulate(ref metrics, cluster.Style);
                }
            }

            if (line.EllipsisStyle is { } ellipsisStyle)
            {
                Accumulate(ref metrics, ellipsisStyle);
                Accumulate(ref reported, ellipsisStyle);
            }

            double ascent;
            double descent;
            if (!metrics.HasText && line.FirstCluster == _clusters.Count && lineIndex > 0)
            {
                // TextWrapper keeps `fEndLine` for the line after a trailing hard break: it repeats the
                // last line's final metrics and reports that line's metrics before the strut.
                reported = previous;
                ascent = previousAscent;
                descent = trimDescent ? previous.UnscaledDescent : previousDescent;
            }
            else
            {
                if (!metrics.HasText)
                {
                    // A line of placeholders only starts from `ParagraphImpl::computeEmptyMetrics`: the
                    // paragraph's text style (the last pushed one for an empty paragraph).
                    AccumulateEmpty(ref metrics, Text.Length == 0 ? _content.EmptyStyle : _content.DefaultStyle);
                }

                for (int index = line.FirstCluster; index < line.EndCluster; index++)
                {
                    Cluster cluster = _clusters[index];
                    if (cluster.Placeholder is not { } placeholder)
                    {
                        continue;
                    }

                    // SkParagraph's `Run::updateMetrics` aligns a placeholder against its own run's font
                    // metrics, which carry no height multiplier.
                    double fontSize = cluster.Style.FontSize;
                    (double placeholderAscent, double placeholderDescent) =
                        PlaceholderMetrics(placeholder, fontSize * AscentRatio, fontSize * DescentRatio);
                    cluster.PlaceholderAscent = placeholderAscent;
                    metrics.Ascent = Math.Max(metrics.Ascent, placeholderAscent);
                    metrics.Descent = Math.Max(metrics.Descent, placeholderDescent);
                    metrics.UnscaledAscent = Math.Max(metrics.UnscaledAscent, placeholderAscent);
                    metrics.UnscaledDescent = Math.Max(metrics.UnscaledDescent, placeholderDescent);
                    reported.Ascent = Math.Max(reported.Ascent, placeholderAscent);
                    reported.Descent = Math.Max(reported.Descent, placeholderDescent);
                }

                previous = metrics;
                ascent = trimAscent ? metrics.UnscaledAscent : metrics.Ascent;
                descent = trimDescent ? metrics.UnscaledDescent : metrics.Descent;
            }

            if (_strutEnabled)
            {
                ascent = _forceStrutHeight ? _strutAscent : Math.Max(ascent, _strutAscent);
                descent = _forceStrutHeight ? _strutDescent : Math.Max(descent, _strutDescent);
            }

            previousAscent = ascent;
            previousDescent = descent;
            line.Top = top;
            line.Ascent = ascent;
            line.Descent = descent;
            line.Baseline = top + ascent;
            line.ReportedAscent = reported.Ascent;
            line.ReportedDescent = reported.Descent;
            double finalDescent = _forceStrutHeight ? _strutDescent - _strutLeadingHalf : descent;
            line.ReportedBaseline = top + Math.Round(ascent + descent, MidpointRounding.AwayFromZero) - finalDescent;

            // SkParagraph's `InternalLineMetrics::height()` rounds each line's height to whole pixels
            // (`::round(descent - ascent + leading)`), so fractional font sizes stack integral lines.
            top += Math.Round(ascent + descent, MidpointRounding.AwayFromZero);
            PositionLine(line, lineIndex == _lines.Count - 1);
            _longestLine = Math.Max(_longestLine, line.ContentWidth);
        }

        _height = top;
    }

    private void Accumulate(ref LineTextMetrics metrics, ResolvedTextStyle style)
    {
        double fontSize = style.FontSize;
        (double ascent, double descent) = VerticalMetrics(fontSize, style.Height, style.LeadingDistribution);
        AddMetrics(ref metrics, fontSize, ascent, descent);
    }

    /// `ParagraphImpl::computeEmptyMetrics`: the style's font with its height applied proportionally, or
    /// as half leading when the strut asks for it. The paragraph's `leadingDistribution` never reaches
    /// the engine's default text style.
    private void AccumulateEmpty(ref LineTextMetrics metrics, ResolvedTextStyle style)
    {
        double fontSize = style.FontSize;
        TextLeadingDistribution distribution = _content.Style.StrutStyle?.LeadingDistribution
                                               ?? TextLeadingDistribution.Proportional;
        (double ascent, double descent) = VerticalMetrics(fontSize, style.Height, distribution);
        AddMetrics(ref metrics, fontSize, ascent, descent);
    }

    private static void AddMetrics(ref LineTextMetrics metrics, double fontSize, double ascent, double descent)
    {
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
        // SkParagraph starts a letter-spaced line half a spacing in (the `TextLine` constructor takes it
        // from the line's first cluster, in text order, that is neither a placeholder nor cursive, its hard
        // break included); `TextLine::format` replaces that shift with the alignment shift only when the
        // line is narrower than the paragraph (`delta > 0`).
        double halfSpacing = 0.0;
        for (int index = line.FirstCluster; index < line.EndCluster; index++)
        {
            Cluster cluster = _clusters[index];
            if (cluster.Placeholder is null && !IsCursiveScript(cluster.FirstCodePoint))
            {
                halfSpacing = cluster.Style.LetterSpacing / 2.0;
                break;
            }
        }

        double x0 = halfSpacing;
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
                double fraction = align switch
                {
                    TextAlign.Left => 0.0,
                    TextAlign.Right => 1.0,
                    TextAlign.Center => 0.5,
                    TextAlign.End => ltr ? 1.0 : 0.0,
                    _ => ltr ? 0.0 : 1.0,
                };
                double delta = _width - contentWidth;
                x0 = fraction == 0.0 || delta <= 0.0 ? halfSpacing : fraction * delta;
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
        line.TrailingStart = trailingStart;
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
                double placeholderTop = line.ReportedBaseline - item.Cluster.PlaceholderAscent;
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
                    double top = line.ReportedBaseline - cluster.PlaceholderAscent;
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
            double top = line.ReportedBaseline - cluster.PlaceholderAscent;
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
            Ascent: line.ReportedAscent,
            Descent: line.ReportedDescent,
            UnscaledAscent: line.ReportedAscent,
            Height: Math.Round(line.ReportedAscent + line.ReportedDescent, MidpointRounding.AwayFromZero),
            Width: line.ContentWidth,
            Left: line.Left,
            Baseline: line.ReportedBaseline,
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

    /// <remarks>
    /// Paints each line the way SkParagraph's `TextLine::paint` does: the style runs' backgrounds, their
    /// shadows, the glyphs, then their decorations. Glyphs are the outlines of the `FlutterTest` font
    /// (`kFlutterTestFont` in the engine's `runtime/test_font_data.cc`): a covered code point is a solid box
    /// one em wide from the ascent to the descent, `p` only its descent part, `É` only its ascent part,
    /// whitespace nothing, and any code point the font does not cover its `.notdef` frame. The shapes are
    /// painted with the run's foreground paint, or its color.
    /// </remarks>
    internal override Action<DrawingContext>? CreateDrawAction(Point offset)
    {
        if (!_laidOut)
        {
            return null;
        }

        // Skia operations batch into one custom draw; shader-painted glyphs are Avalonia geometry.
        var steps = new List<object>();
        Rect? bounds = null;
        void Include(Rect rect) => bounds = bounds is { } current ? current.Union(rect) : rect;
        void AddOperation(FlutterTestGlyphDraw.Operation operation)
        {
            if (steps.Count == 0 || steps[^1] is not List<FlutterTestGlyphDraw.Operation> batch)
            {
                batch = [];
                steps.Add(batch);
            }

            batch.Add(operation);
        }

        for (int lineIndex = 0; lineIndex < _lines.Count; lineIndex++)
        {
            Line line = _lines[lineIndex];
            // SkParagraph's TextLine paints its blob at floor(baseline + 0.5).
            double baseline = offset.Y + Math.Floor(line.Baseline + 0.5);
            List<StyleRun> runs = StyleRunsOf(line, lineIndex, offset);
            foreach (StyleRun run in runs)
            {
                if (run.Style.Background is { } background)
                {
                    AddOperation(new FlutterTestGlyphDraw.BackgroundOperation(run.Clip, background.Color));
                    Include(run.Clip);
                }
            }

            foreach (StyleRun run in runs)
            {
                if (run.Style.Shadows is not { Count: > 0 } shadows)
                {
                    continue;
                }

                List<FlutterTestGlyphDraw.Glyph> glyphs = GlyphsOf(run, offset.X, baseline);
                foreach (Rendering.Shadow shadow in shadows)
                {
                    // TextShadow::hasShadow.
                    if (shadow.Offset == default && shadow.BlurRadius == 0.0)
                    {
                        continue;
                    }

                    AddOperation(new FlutterTestGlyphDraw.GlyphsOperation(
                        glyphs,
                        new FlutterTestGlyphDraw.TextShadowDraw(shadow.Offset, shadow.Color, shadow.BlurSigma)));
                    Include(run.Clip.Translate(shadow.Offset).Inflate((shadow.BlurSigma * 3.0) + 1.0));
                }
            }

            foreach (StyleRun run in runs)
            {
                if (run.Style.Foreground is { Shader: { } shader })
                {
                    foreach (FlutterTestGlyphDraw.Glyph glyph in GlyphsOf(run, offset.X, baseline))
                    {
                        steps.Add((FlutterTestGlyph(ShapeOf(glyph.CodePoint), glyph.X, glyph.FontSize,
                            glyph.Baseline, glyph.FontSize), (IBrush)shader));
                    }
                }
                else
                {
                    AddOperation(new FlutterTestGlyphDraw.GlyphsOperation(GlyphsOf(run, offset.X, baseline)));
                }

                Include(run.Clip.Union(new Rect(
                    run.Left,
                    baseline - (run.Style.FontSize * AscentRatio),
                    run.Right - run.Left + run.Style.FontSize,
                    run.Style.FontSize)));
            }

            foreach (StyleRun run in runs)
            {
                ResolvedTextStyle style = run.Style;
                if (style.Decoration == TextDecoration.None)
                {
                    continue;
                }

                AddOperation(new FlutterTestGlyphDraw.DecorationOperation(
                    run.Clip,
                    run.CorrectAscent,
                    style.FontSize * AscentRatio,
                    style.FontSize,
                    style.Decoration,
                    style.DecorationStyle,
                    style.DecorationThickness ?? 1.0,
                    style.DecorationColor ?? style.Foreground?.Color ?? style.Color));
                Include(run.Clip.Inflate(style.FontSize));
            }
        }

        if (steps.Count == 0 || bounds is not { } drawBounds)
        {
            return null;
        }

        Rect inflated = drawBounds.Inflate(1.0);
        var draws = steps
            .Select(step => step is List<FlutterTestGlyphDraw.Operation> batch
                ? new FlutterTestGlyphDraw(batch, inflated)
                : step)
            .ToList();
        return context =>
        {
            foreach (object draw in draws)
            {
                if (draw is FlutterTestGlyphDraw custom)
                {
                    context.Custom(custom);
                }
                else
                {
                    (Geometry shape, IBrush brush) = ((Geometry, IBrush))draw;
                    context.DrawGeometry(brush, null, shape);
                }
            }
        };
    }

    /// <summary>
    /// SkParagraph's `TextLine::iterateThroughSingleRunByStyles` pieces of a line: consecutive visual items of
    /// one style and direction, with the `ClipContext.clip` each is painted against. LTR paragraphs leave the
    /// line's trailing spaces out of the clip (`measureTextInsideOneRun` stops at the line advance).
    /// </summary>
    private List<StyleRun> StyleRunsOf(Line line, int lineIndex, Point offset)
    {
        TextHeightBehavior behavior = _content.Style.TextHeightBehavior ?? new TextHeightBehavior();
        bool trimAscent = lineIndex == 0 && !behavior.ApplyHeightToFirstAscent;
        bool trimDescent = lineIndex == _lines.Count - 1 && !behavior.ApplyHeightToLastDescent;
        double lineHeight = line.Ascent + line.Descent;
        // InternalLineMetrics::delta(): the rounded line height minus the unrounded one.
        double delta = Math.Round(lineHeight, MidpointRounding.AwayFromZero) - lineHeight;
        var runs = new List<StyleRun>();
        StyleRun? current = null;
        foreach (Placed item in line.Visual)
        {
            if (item.Cluster is { } cluster
                && (cluster.Placeholder is not null
                    || cluster.HardBreak
                    || (_baseLevel == 0 && _clusters.IndexOf(cluster) >= line.TrailingStart)))
            {
                current = null;
                continue;
            }

            if (current is { } open
                && !open.IsEllipsis
                && !item.IsEllipsis
                && open.Level == item.Level
                && Equals(open.Style, item.Style)
                && Math.Abs(open.Right - (offset.X + item.X)) < Epsilon)
            {
                open.Items.Add(item);
                open.Right = offset.X + item.X + item.Width;
                open.Clip = open.Clip.WithWidth(open.Right - open.Left);
                continue;
            }

            ResolvedTextStyle style = item.Style;
            double fontSize = style.FontSize;
            (double ascent, double descent) = VerticalMetrics(fontSize, style.Height, style.LeadingDistribution);
            ascent = trimAscent ? fontSize * AscentRatio : ascent;
            descent = trimDescent ? fontSize * DescentRatio : descent;
            // LineMetrics::runTop and Run::calculateHeight (CSS metric style).
            double top = offset.Y + line.Top + line.Ascent - ascent + delta;
            double left = offset.X + item.X;
            current = new StyleRun(style, item.Level, item.IsEllipsis, ascent)
            {
                Left = left,
                Right = left + item.Width,
                Clip = new Rect(left, top, item.Width, ascent + descent),
            };
            current.Items.Add(item);
            runs.Add(current);
        }

        return runs;
    }

    private List<FlutterTestGlyphDraw.Glyph> GlyphsOf(StyleRun run, double offsetX, double baseline)
    {
        var glyphs = new List<FlutterTestGlyphDraw.Glyph>();
        ResolvedTextStyle style = run.Style;
        // OneLineShaper::shape fakes the weight and slant the regular, upright FlutterTest lacks.
        int weight = (int)style.FontWeight;
        bool fakeBold = weight >= 600 && weight - FlutterTestFontWeight >= 200;
        bool fakeItalic = style.FontStyle != FontStyle.Normal;
        Color color = style.Foreground?.Color ?? style.Color;
        string ellipsis = _content.Style.Ellipsis ?? string.Empty;
        foreach (Placed item in run.Items)
        {
            double x = offsetX + item.X;
            if (item.Cluster is { } cluster)
            {
                double letterSpacing = LetterSpacingOf(cluster.FirstCodePoint, style);
                foreach (ShapedGlyph glyph in cluster.Glyphs)
                {
                    // Zero-width glyphs paint nothing apart: hidden ignorables are blank, and a zeroed
                    // mark's `.notdef` frame lands on its base.
                    if (glyph.Advance > 0.0
                        && (!Rune.IsValid(glyph.CodePoint) || !Rune.IsWhiteSpace(new Rune(glyph.CodePoint))))
                    {
                        glyphs.Add(new FlutterTestGlyphDraw.Glyph(
                            glyph.CodePoint, x, baseline, style.FontSize, color, fakeBold, fakeItalic));
                    }

                    x += glyph.Advance + letterSpacing;
                }

                continue;
            }

            int cursor = 0;
            while (cursor < ellipsis.Length)
            {
                int codePoint = UnicodeText.CodePointAt(ellipsis, cursor);
                cursor += UnicodeText.CodePointLength(ellipsis, cursor);
                if (Rune.IsValid(codePoint) && UnicodeText.IsZeroWidth(new Rune(codePoint)))
                {
                    continue;
                }

                if (!Rune.IsValid(codePoint) || !Rune.IsWhiteSpace(new Rune(codePoint)))
                {
                    glyphs.Add(new FlutterTestGlyphDraw.Glyph(
                        codePoint, x, baseline, style.FontSize, color, fakeBold, fakeItalic));
                }

                x += style.FontSize;
            }
        }

        return glyphs;
    }

    /// <summary>One style run of a line and its paint clip, in paragraph-offset coordinates.</summary>
    private sealed class StyleRun(ResolvedTextStyle style, int level, bool isEllipsis, double correctAscent)
    {
        public ResolvedTextStyle Style { get; } = style;

        public int Level { get; } = level;

        public bool IsEllipsis { get; } = isEllipsis;

        /// The run's height-adjusted ascent (`Run::correctAscent`) as a positive distance.
        public double CorrectAscent { get; } = correctAscent;

        public List<Placed> Items { get; } = [];

        public double Left { get; init; }

        public double Right { get; set; }

        public Rect Clip { get; set; }
    }

    private enum GlyphShape
    {
        Box,
        DescentOnly,
        AscentOnly,
        NotDef,
    }

    // The code points `kFlutterTestFont` maps to its full `Square` glyph; `p` and `É` have their own
    // half glyphs, and the space-like code points paint nothing.
    private static readonly (int First, int Last)[] FlutterTestSquareRanges =
    [
        (0x0021, 0x0026), (0x0028, 0x006F), (0x0071, 0x007E), (0x00A1, 0x00C8), (0x00CA, 0x00FF),
        (0x0131, 0x0131), (0x0152, 0x0153), (0x0178, 0x0178), (0x0192, 0x0192), (0x02C6, 0x02C7),
        (0x02C9, 0x02C9), (0x02D8, 0x02DD), (0x0394, 0x0394), (0x03A5, 0x03A5), (0x03A7, 0x03A7),
        (0x03A9, 0x03A9), (0x03BC, 0x03BC), (0x03C0, 0x03C0), (0x2013, 0x2014), (0x2018, 0x201A),
        (0x201C, 0x201E), (0x2020, 0x2022), (0x2026, 0x2026), (0x2030, 0x2030), (0x2039, 0x203A),
        (0x2044, 0x2044), (0x2122, 0x2122), (0x2126, 0x2126), (0x2202, 0x2202), (0x2206, 0x2206),
        (0x220F, 0x220F), (0x2211, 0x2212), (0x2219, 0x221A), (0x221E, 0x221E), (0x222B, 0x222B),
        (0x2248, 0x2248), (0x2260, 0x2260), (0x2264, 0x2265), (0x22F2, 0x22F2), (0x25CA, 0x25CA),
        (0x3007, 0x3007), (0x4E00, 0x4E00), (0x4E03, 0x4E03), (0x4E09, 0x4E09), (0x4E2D, 0x4E2D),
        (0x4E5D, 0x4E5D), (0x4E8C, 0x4E8C), (0x4E94, 0x4E94), (0x516B, 0x516B), (0x516D, 0x516D),
        (0x5341, 0x5341), (0x5426, 0x5426), (0x56D7, 0x56D7), (0x56DB, 0x56DB), (0x571F, 0x571F),
        (0x6587, 0x6587), (0x662F, 0x662F), (0x6728, 0x6728), (0x672C, 0x672C), (0x6B63, 0x6B63),
        (0x6C34, 0x6C34), (0x6D4B, 0x6D4B), (0x706B, 0x706B), (0x786E, 0x786E), (0x8BD5, 0x8BD5),
        (0x91D1, 0x91D1), (0xF000, 0xF002),
    ];

    private static GlyphShape ShapeOf(int codePoint)
    {
        if (codePoint == 'p')
        {
            return GlyphShape.DescentOnly;
        }

        if (codePoint == 0x00C9)
        {
            return GlyphShape.AscentOnly;
        }

        foreach ((int first, int last) in FlutterTestSquareRanges)
        {
            if (codePoint >= first && codePoint <= last)
            {
                return GlyphShape.Box;
            }
        }

        return GlyphShape.NotDef;
    }

    // The outlines in font units (1024 per em, ascent 768, descent 256) scaled to `fontSize`.
    private static Geometry FlutterTestGlyph(GlyphShape shape, double x, double width, double baseline,
        double fontSize)
    {
        double ascent = fontSize * AscentRatio;
        double descent = fontSize * DescentRatio;
        switch (shape)
        {
            case GlyphShape.DescentOnly:
                return new RectangleGeometry(new Rect(x, baseline, Math.Max(width, 0.0), descent));
            case GlyphShape.AscentOnly:
                return new RectangleGeometry(new Rect(x, baseline - ascent, Math.Max(width, 0.0), ascent));
            case GlyphShape.NotDef:
                double unit = fontSize / 1024.0;
                var outer = new Rect(x + (128 * unit), baseline - (768 * unit), 768 * unit, 768 * unit);
                var inner = new Rect(x + (256 * unit), baseline - (640 * unit), 512 * unit, 512 * unit);
                return new CombinedGeometry(GeometryCombineMode.Exclude,
                    new RectangleGeometry(outer), new RectangleGeometry(inner));
            default:
                return new RectangleGeometry(new Rect(x, baseline - ascent, Math.Max(width, 0.0), ascent + descent));
        }
    }

    // -- Model -----------------------------------------------------------------------

    private sealed class Cluster(int start, int end, double width, ResolvedTextStyle style)
    {
        public int Start { get; } = start;

        public int End { get; } = end;

        public double Width { get; } = width;

        public ResolvedTextStyle Style { get; } = style;

        public ParagraphPlaceholder? Placeholder { get; init; }

        /// The shaped glyphs, in logical order (none for placeholders and hard breaks).
        public ShapedGlyph[] Glyphs { get; init; } = [];

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

        public double Baseline { get; set; }

        public double ReportedAscent { get; set; }

        public double ReportedDescent { get; set; }

        public double ReportedBaseline { get; set; }

        public double Left { get; set; }

        public double ContentWidth { get; set; }

        /// The index of the first trailing (hanging space or hard break) cluster.
        public int TrailingStart { get; set; }

        public List<Placed> Visual { get; set; } = [];
    }

    private readonly record struct ShapedGlyph(int CodePoint, double Advance);

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
