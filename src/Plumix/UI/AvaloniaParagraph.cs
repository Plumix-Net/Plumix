using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Painting;
using Plumix.Widgets;

// C#-only infrastructure: the dart:ui `Paragraph` engine over Avalonia's text formatter. The builder's
// styled runs and placeholders become one `ITextSource`, laid out by a single `TextLayout`. What the
// formatter cannot express (per-run height and letter spacing, strut, first/last line height trimming,
// separate ideographic metrics) is recorded in docs/ai/DIVERGENCES.md. No Dart source maps to this file.

namespace Plumix.UI;

/// Chooses the paragraph engine for a built paragraph.
internal static class ParagraphBackend
{
    /// Creates the Avalonia-backed paragraph when a font manager is registered, the headless
    /// `FlutterTest`-metric paragraph otherwise.
    public static Paragraph Create(ParagraphContent content)
    {
        return HasFontManager() ? new AvaloniaParagraph(content) : new HeadlessParagraph(content);
    }

    private static bool? s_hasFontManager;

    // The first probe decides for the process: a host registers its font manager before it builds any
    // text, and a headless process never registers one, so re-probing would only repeat the exception.
    private static bool HasFontManager()
    {
        if (s_hasFontManager is { } cached)
        {
            return cached;
        }

        try
        {
            _ = FontManager.Current;
            s_hasFontManager = true;
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains(
                                                               "FontManager",
                                                               StringComparison.Ordinal))
        {
            s_hasFontManager = false;
        }

        return s_hasFontManager.Value;
    }
}

/// A <see cref="Paragraph"/> laid out by Avalonia's <see cref="TextLayout"/>.
internal sealed class AvaloniaParagraph : Paragraph
{
    private readonly ParagraphContent _content;
    private readonly ParagraphTextSource _source;
    private TextLayout? _layout;
    private double _width;
    private double? _minIntrinsicWidth;
    private double? _maxIntrinsicWidth;
    private bool _didExceedMaxLines;

    public AvaloniaParagraph(ParagraphContent content)
        : base(content.Text)
    {
        _content = content;
        _source = new ParagraphTextSource(content);
    }

    public override double Width => _width;

    public override double Height => _layout?.Height ?? 0.0;

    public override double LongestLine
    {
        get
        {
            if (_layout is null)
            {
                return 0.0;
            }

            double longest = 0.0;
            foreach (TextLine line in _layout.TextLines)
            {
                longest = Math.Max(longest, line.Width);
            }

            return longest;
        }
    }

    public override double MinIntrinsicWidth => _minIntrinsicWidth ??= MeasureMinIntrinsicWidth();

    public override double MaxIntrinsicWidth => _maxIntrinsicWidth ??= MeasureMaxIntrinsicWidth();

    public override double AlphabeticBaseline =>
        _layout is { TextLines.Count: > 0 } layout ? layout.TextLines[0].Baseline : 0.0;

    // Avalonia exposes one baseline metric; see docs/ai/DIVERGENCES.md.
    public override double IdeographicBaseline => AlphabeticBaseline;

    public override bool DidExceedMaxLines => _didExceedMaxLines;

    public override int NumberOfLines => Text.Length == 0 || _layout is null ? 0 : _layout.TextLines.Count;

    public override void Layout(ParagraphConstraints constraints)
    {
        _width = constraints.Width;
        string? ellipsis = string.IsNullOrEmpty(_content.Style.Ellipsis) ? null : _content.Style.Ellipsis;
        int maxLines = _content.Style.MaxLines ?? 0;
        TextLayout layout = CreateLayout(constraints.Width, maxLines, trim: false);
        if (ellipsis is not null)
        {
            int ellipsizedLines = maxLines;
            if (maxLines == 0)
            {
                for (int index = 0; index < layout.TextLines.Count - 1; index++)
                {
                    if (layout.TextLines[index].NewLineLength == 0)
                    {
                        ellipsizedLines = index + 1;
                        break;
                    }
                }
            }

            if (ellipsizedLines > 0)
            {
                layout = CreateLayout(constraints.Width, ellipsizedLines, trim: true);
                maxLines = ellipsizedLines;
            }
        }

        _layout = layout;
        _didExceedMaxLines = maxLines > 0 && HasContentBeyond(layout, maxLines);
    }

    private TextLayout CreateLayout(double width, int maxLines, bool trim)
    {
        bool finite = double.IsFinite(width);
        return new TextLayout(
            _source,
            _source.CreateParagraphProperties(finite ? ResolveTextAlignment() : TextAlignment.Left),
            trim ? TextTrimming.CharacterEllipsis : TextTrimming.None,
            finite ? Math.Max(0.0, width) : double.PositiveInfinity,
            double.PositiveInfinity,
            maxLines);
    }

    private bool HasContentBeyond(TextLayout layout, int maxLines)
    {
        if (layout.TextLines.Count < maxLines)
        {
            return false;
        }

        TextLine last = layout.TextLines[^1];
        return last.HasCollapsed || last.FirstTextSourceIndex + last.Length < Text.Length;
    }

    private TextAlignment ResolveTextAlignment()
    {
        bool rtl = _content.TextDirection == TextDirection.Rtl;
        return (_content.Style.TextAlign ?? TextAlign.Start) switch
        {
            TextAlign.Left => TextAlignment.Left,
            TextAlign.Right => TextAlignment.Right,
            TextAlign.Center => TextAlignment.Center,
            TextAlign.Justify => TextAlignment.Justify,
            TextAlign.End => rtl ? TextAlignment.Left : TextAlignment.Right,
            _ => rtl ? TextAlignment.Right : TextAlignment.Left,
        };
    }

    private double MeasureMaxIntrinsicWidth()
    {
        TextLayout layout = CreateLayout(double.PositiveInfinity, 0, trim: false);
        return layout.WidthIncludingTrailingWhitespace;
    }

    private double MeasureMinIntrinsicWidth()
    {
        TextLayout layout = CreateLayout(0.0, 0, trim: false);
        double widest = 0.0;
        foreach (TextLine line in layout.TextLines)
        {
            widest = Math.Max(widest, line.Width);
        }

        return widest;
    }

    public override IReadOnlyList<TextBox> GetBoxesForRange(
        int start,
        int end,
        BoxHeightStyle boxHeightStyle = BoxHeightStyle.Tight,
        BoxWidthStyle boxWidthStyle = BoxWidthStyle.Tight)
    {
        var boxes = new List<TextBox>();
        if (_layout is null || end <= start)
        {
            return boxes;
        }

        double widest = 0.0;
        foreach (TextLine line in _layout.TextLines)
        {
            widest = Math.Max(widest, line.Start + line.WidthIncludingTrailingWhitespace);
        }

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
            int boxEnd = Math.Min(end, lineEnd);
            int lineFirstBox = boxes.Count;
            foreach (TextBounds bounds in line.GetTextBounds(boxStart, boxEnd - boxStart))
            {
                Rect rect = bounds.Rectangle;
                bool tight = boxHeightStyle == BoxHeightStyle.Tight && rect.Height > 0;
                double top = tight ? lineTop + Math.Max(0.0, rect.Y) : lineTop;
                double height = tight ? rect.Height : line.Height;
                TextDirection direction = bounds.FlowDirection == FlowDirection.RightToLeft
                    ? TextDirection.Rtl
                    : TextDirection.Ltr;
                boxes.Add(new TextBox(rect.X, top, rect.X + rect.Width, top + height, direction));
            }

            if (boxWidthStyle == BoxWidthStyle.Max && boxes.Count > lineFirstBox && end > lineEnd)
            {
                TextBox last = boxes[^1];
                if (last.Right < widest)
                {
                    boxes.Add(new TextBox(last.Right, last.Top, widest, last.Bottom, last.Direction));
                }
            }

            lineTop += line.Height;
        }

        return boxes;
    }

    public override IReadOnlyList<TextBox> GetBoxesForPlaceholders()
    {
        var boxes = new List<TextBox>();
        if (_layout is null || _content.Placeholders.Count == 0)
        {
            return boxes;
        }

        double lineTop = 0.0;
        foreach (TextLine line in _layout.TextLines)
        {
            double x = line.Start;
            foreach (TextRun run in line.TextRuns)
            {
                if (run is PlaceholderTextRun placeholder)
                {
                    double top = lineTop + PlaceholderTop(placeholder.Placeholder, line);
                    boxes.Add(new TextBox(
                        x,
                        top,
                        x + placeholder.Size.Width,
                        top + placeholder.Size.Height,
                        _content.TextDirection));
                    x += placeholder.Size.Width;
                    continue;
                }

                x += run is DrawableTextRun drawable ? drawable.Size.Width : 0.0;
            }

            lineTop += line.Height;
        }

        return boxes;
    }

    private static double PlaceholderTop(ParagraphPlaceholder placeholder, TextLine line)
    {
        double height = placeholder.Height;
        return placeholder.Alignment switch
        {
            PlaceholderAlignment.Top => 0.0,
            PlaceholderAlignment.Bottom => line.Height - height,
            PlaceholderAlignment.Middle => (line.Height - height) / 2.0,
            PlaceholderAlignment.AboveBaseline => line.Baseline - height,
            PlaceholderAlignment.BelowBaseline => line.Baseline,
            _ => line.Baseline - placeholder.BaselineOffset,
        };
    }

    public override TextPosition GetPositionForOffset(Point offset)
    {
        if (_layout is null)
        {
            return new TextPosition(0);
        }

        TextHitTestResult hit = _layout.HitTestPoint(offset);
        int position = Math.Clamp(hit.TextPosition + (hit.IsTrailing ? 1 : 0), 0, Text.Length);
        return new TextPosition(position, hit.IsTrailing ? TextAffinity.Upstream : TextAffinity.Downstream);
    }

    public override GlyphInfo? GetGlyphInfoAt(int codeUnitOffset)
    {
        if (_layout is null || codeUnitOffset < 0 || codeUnitOffset >= Text.Length)
        {
            return null;
        }

        int clusterStart = codeUnitOffset;
        int clusterLength = 1;
        int cursor = 0;
        while (cursor < Text.Length)
        {
            int length = StringInfo.GetNextTextElementLength(Text, cursor);
            if (codeUnitOffset < cursor + length)
            {
                clusterStart = cursor;
                clusterLength = length;
                break;
            }

            cursor += length;
        }

        double lineTop = 0.0;
        foreach (TextLine line in _layout.TextLines)
        {
            int lineStart = line.FirstTextSourceIndex;
            int lineEnd = lineStart + line.Length;
            if (clusterStart >= lineStart && clusterStart < lineEnd)
            {
                IReadOnlyList<TextBounds> bounds = line.GetTextBounds(clusterStart, clusterLength);
                if (bounds.Count == 0)
                {
                    return null;
                }

                Rect rect = bounds[0].Rectangle;
                TextDirection direction = bounds[0].FlowDirection == FlowDirection.RightToLeft
                    ? TextDirection.Rtl
                    : TextDirection.Ltr;
                return new GlyphInfo(
                    new Rect(rect.X, lineTop, rect.Width, line.Height),
                    new TextRange(clusterStart, clusterStart + clusterLength),
                    direction);
            }

            lineTop += line.Height;
        }

        return null;
    }

    public override GlyphInfo? GetClosestGlyphInfoForOffset(Point offset)
    {
        if (_layout is null || Text.Length == 0)
        {
            return null;
        }

        TextHitTestResult hit = _layout.HitTestPoint(offset);
        return GetGlyphInfoAt(Math.Clamp(hit.TextPosition, 0, Text.Length - 1));
    }

    private protected override TextRange GetLineRange(int offset)
    {
        if (_layout is null)
        {
            return TextRange.Empty;
        }

        foreach (TextLine line in _layout.TextLines)
        {
            int start = line.FirstTextSourceIndex;
            int contentEnd = start + line.Length - line.NewLineLength;
            if (start <= offset && offset <= contentEnd)
            {
                return new TextRange(start, contentEnd);
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
        return lineNumber >= 0 && lineNumber < NumberOfLines ? MetricsFor(lineNumber) : null;
    }

    private LineMetrics MetricsFor(int index)
    {
        IReadOnlyList<TextLine> lines = _layout!.TextLines;
        double top = 0.0;
        for (int cursor = 0; cursor < index; cursor++)
        {
            top += lines[cursor].Height;
        }

        TextLine line = lines[index];
        return new LineMetrics(
            HardBreak: line.NewLineLength > 0 || index == lines.Count - 1,
            Ascent: line.Baseline,
            Descent: Math.Max(0.0, line.Height - line.Baseline),
            UnscaledAscent: line.Baseline,
            Height: line.Height,
            Width: line.Width,
            Left: line.Start,
            Baseline: top + line.Baseline,
            LineNumber: index);
    }

    public override int? GetLineNumberAt(int codeUnitOffset)
    {
        if (_layout is null || codeUnitOffset < 0)
        {
            return null;
        }

        for (int index = 0; index < _layout.TextLines.Count; index++)
        {
            TextLine line = _layout.TextLines[index];
            if (codeUnitOffset >= line.FirstTextSourceIndex
                && codeUnitOffset < line.FirstTextSourceIndex + line.Length)
            {
                return index;
            }
        }

        return null;
    }

    internal override Action<DrawingContext>? CreateDrawAction(Point offset)
    {
        TextLayout? layout = _layout;
        return layout is null ? null : context => layout.Draw(context, offset);
    }
}

/// Exposes a built paragraph's styled runs and placeholders to Avalonia's text formatter.
internal sealed class ParagraphTextSource : ITextSource
{
    private readonly ParagraphContent _content;
    private readonly ReadOnlyMemory<char> _text;

    public ParagraphTextSource(ParagraphContent content)
    {
        _content = content;
        _text = content.Text.AsMemory();
    }

    public TextRun? GetTextRun(int textSourceIndex)
    {
        if (textSourceIndex >= _content.Text.Length)
        {
            return null;
        }

        foreach (ParagraphPlaceholder placeholder in _content.Placeholders)
        {
            if (placeholder.Index == textSourceIndex)
            {
                return new PlaceholderTextRun(placeholder, CreateRunProperties(placeholder.Style));
            }
        }

        int end = _content.RunEndAt(textSourceIndex);
        foreach (ParagraphPlaceholder placeholder in _content.Placeholders)
        {
            if (placeholder.Index > textSourceIndex)
            {
                end = Math.Min(end, placeholder.Index);
                break;
            }
        }

        return new TextCharacters(
            _text.Slice(textSourceIndex, end - textSourceIndex),
            CreateRunProperties(_content.StyleAt(textSourceIndex)));
    }

    /// Builds the paragraph-level formatting properties from the paragraph's default style.
    public TextParagraphProperties CreateParagraphProperties(TextAlignment alignment)
    {
        ResolvedTextStyle style = _content.DefaultStyle;
        double lineHeight = style.Height != TextDefaults.TextHeightNone
            ? Math.Max(0.01, style.FontSize * style.Height)
            : double.NaN;
        FlowDirection flowDirection = _content.TextDirection == TextDirection.Rtl
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;
        return new GenericTextParagraphProperties(
            flowDirection,
            alignment,
            firstLineInParagraph: true,
            alwaysCollapsible: false,
            CreateRunProperties(style),
            TextWrapping.Wrap,
            lineHeight,
            indent: 0,
            letterSpacing: style.LetterSpacing);
    }

    private static TextRunProperties CreateRunProperties(ResolvedTextStyle style)
    {
        var typeface = new Typeface(style.FontFamily, style.FontStyle, style.FontWeight);
        return new GenericTextRunProperties(
            typeface,
            style.FontSize,
            ResolveDecorations(style),
            style.Foreground is { } foreground ? PaintBrush(foreground) : new SolidColorBrush(style.Color),
            style.Background is { } background ? PaintBrush(background) : null,
            fontFeatures: ResolveFontFeatures(style.FontFeatures));
    }

    // A `Paint` reaches Avalonia as its shader, or else as a solid brush of its color.
    private static IBrush PaintBrush(Paint paint) => paint.Shader ?? new SolidColorBrush(paint.Color);

    private static FontFeatureCollection? ResolveFontFeatures(IReadOnlyList<FontFeature>? features)
    {
        if (features is not { Count: > 0 })
        {
            return null;
        }

        var collection = new FontFeatureCollection();
        foreach (FontFeature feature in features)
        {
            collection.Add(new Avalonia.Media.FontFeature { Tag = feature.Feature, Value = feature.Value });
        }

        return collection;
    }

    private static TextDecorationCollection? ResolveDecorations(ResolvedTextStyle style)
    {
        if (style.Decoration == TextDecoration.None)
        {
            return null;
        }

        var decorations = new TextDecorationCollection();
        if (style.Decoration.HasFlag(TextDecoration.Underline))
        {
            decorations.Add(new Avalonia.Media.TextDecoration { Location = TextDecorationLocation.Underline });
        }

        if (style.Decoration.HasFlag(TextDecoration.Overline))
        {
            decorations.Add(new Avalonia.Media.TextDecoration { Location = TextDecorationLocation.Overline });
        }

        if (style.Decoration.HasFlag(TextDecoration.LineThrough))
        {
            decorations.Add(new Avalonia.Media.TextDecoration { Location = TextDecorationLocation.Strikethrough });
        }

        return decorations;
    }
}

/// A text run that reserves the space an inline placeholder occupies.
///
/// The widget itself is painted by `RenderParagraph` after the glyphs, so this run only contributes
/// metrics to the line.
internal sealed class PlaceholderTextRun : DrawableTextRun
{
    public PlaceholderTextRun(ParagraphPlaceholder placeholder, TextRunProperties properties)
    {
        Placeholder = placeholder;
        Properties = properties;
    }

    public ParagraphPlaceholder Placeholder { get; }

    public override Size Size => new(Placeholder.Width, Placeholder.Height);

    public override double Baseline => Placeholder.Alignment switch
    {
        PlaceholderAlignment.AboveBaseline => Placeholder.Height,
        PlaceholderAlignment.BelowBaseline => 0.0,
        PlaceholderAlignment.Baseline => Placeholder.BaselineOffset,
        _ => Placeholder.Height,
    };

    public override int Length => 1;

    public override TextRunProperties Properties { get; }

    public override void Draw(DrawingContext drawingContext, Point origin)
    {
        // Intentionally empty: RenderParagraph paints the inline child itself.
    }
}
