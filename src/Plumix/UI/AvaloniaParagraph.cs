using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.Widgets;

// C#-only infrastructure: the dart:ui `Paragraph` engine over Avalonia's text formatter. The builder's
// styled runs are shaped with per-run spacing/axes, then wrapped by TextLayout. Framework-owned
// vertical metrics and glyph painting implement the dart:ui contracts. No Dart source maps to this file.

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

    /// <summary>
    /// Runs the one-time font-manager probe now, so a process that installs a render backend later (the
    /// test assembly's raster backend) keeps the engine the probe chose.
    /// </summary>
    internal static void EnsureProbed() => _ = HasFontManager();

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

/// A paragraph whose shaping/fallback comes from Avalonia and whose vertical layout belongs to Plumix.
internal sealed class AvaloniaParagraph : Paragraph
{
    private readonly ParagraphContent _content;
    private readonly ParagraphTextSource _source;
    private readonly List<(int Start, int End)> _clusters = [];
    private readonly List<ParagraphLine> _lines = [];
    private ParagraphShaping? _shaping;
    private TextLayout? _layout;
    private Picture? _paintedPicture;
    private double _width;
    private double _height;
    private double? _minIntrinsicWidth;
    private double? _maxIntrinsicWidth;
    private bool _didExceedMaxLines;
    private (double Ascent, double Descent)? _strut;
    private (double Ascent, double Descent)? _strutBox;

    public AvaloniaParagraph(ParagraphContent content) : base(content.Text)
    {
        _content = content;
        _source = new ParagraphTextSource(content);
        for (int index = 0; index < Text.Length;)
        {
            int length = StringInfo.GetNextTextElementLength(Text, index);
            _clusters.Add((index, index + length));
            index += length;
        }
    }

    public override double Width => _width;
    public override double Height => _height;
    public override double LongestLine => _lines.Count == 0 ? 0 : _lines.Max(line => line.Native.Width);
    public override double MinIntrinsicWidth => _minIntrinsicWidth ??= MeasureIntrinsic(0);
    public override double MaxIntrinsicWidth => _maxIntrinsicWidth ??= MeasureIntrinsic(double.PositiveInfinity);
    public override double AlphabeticBaseline => _lines.Count == 0 ? 0 : _lines[0].Baseline;
    public override double IdeographicBaseline =>
        _lines.Count == 0 ? 0 : _lines[0].Ascent + _lines[0].Descent;
    public override bool DidExceedMaxLines => _didExceedMaxLines;
    public override int NumberOfLines => Text.Length == 0 ? 0 : _lines.Count;

    public override void Layout(ParagraphConstraints constraints)
    {
        _shaping ??= new ParagraphShaping(_content, _source);
        _width = constraints.Width;
        _layout?.Dispose();
        _paintedPicture = null;
        string? ellipsis = string.IsNullOrEmpty(_content.Style.Ellipsis) ? null : _content.Style.Ellipsis;
        int maxLines = _content.Style.MaxLines ?? 0;
        TextLayout layout = CreateLayout(constraints.Width, maxLines, null);
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
                layout.Dispose();
                layout = CreateLayout(constraints.Width, ellipsizedLines, ellipsis);
                maxLines = ellipsizedLines;
            }
        }

        _layout = layout;
        TextLine last = layout.TextLines[^1];
        _didExceedMaxLines = maxLines > 0 && layout.TextLines.Count >= maxLines
                            && (last.HasCollapsed || last.FirstTextSourceIndex + last.Length < Text.Length);
        ComputeVerticalMetrics();
    }

    private TextLayout CreateLayout(double width, int maxLines, string? ellipsis)
    {
        _shaping ??= new ParagraphShaping(_content, _source);
        return new TextLayout(
            _shaping,
            _source.CreateParagraphProperties(double.IsFinite(width) ? ResolveTextAlignment() : TextAlignment.Left),
            ellipsis is null ? TextTrimming.None : new ParagraphEllipsis(ellipsis),
            double.IsFinite(width) ? Math.Max(0.0, width) : double.PositiveInfinity,
            double.PositiveInfinity,
            maxLines);
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

    private double MeasureIntrinsic(double width)
    {
        using TextLayout layout = CreateLayout(width, 0, null);
        return width == 0 ? layout.TextLines.Max(line => line.Width) : layout.WidthIncludingTrailingWhitespace;
    }

    private (double Ascent, double Descent) ScaledMetrics(
        double ascent, double descent, double gap, double size, double height,
        TextLeadingDistribution? distribution)
    {
        if (height == TextDefaults.TextHeightNone)
        {
            return (ascent + gap / 2.0, descent + gap / 2.0);
        }

        double target = size * height;
        return (distribution ?? _content.DefaultLeadingDistribution) == TextLeadingDistribution.Even
            ? (ascent + (target - ascent - descent) / 2.0, descent + (target - ascent - descent) / 2.0)
            : (target * (ascent + gap / 2.0) / (ascent + descent + gap),
                target * (descent + gap / 2.0) / (ascent + descent + gap));
    }

    private void ComputeVerticalMetrics()
    {
        _strut = null;
        _strutBox = null;
        if (_content.Style.StrutStyle is { IsEnabled: true } strut)
        {
            var style = _content.DefaultStyle with
            {
                FontFamily = strut.FontFamily ?? FontFamily.Default,
                FontFamilyFallback = strut.FontFamilyFallback,
                FontWeight = strut.FontWeight ?? FontWeight.Normal,
                FontStyle = strut.FontStyle ?? FontStyle.Normal,
                FontSize = strut.FontSize ?? TextDefaults.DefaultFontSize,
            };
            TextRunProperties properties = ParagraphTextSource.CreateRunProperties(style);
            var metrics = new TextMetrics(properties.Typeface.GlyphTypeface, style.FontSize);
            double ascent = -metrics.Ascent;
            double descent = metrics.Descent;
            if (strut.Height is double height && height != TextDefaults.TextHeightNone)
            {
                double target = height * style.FontSize;
                if ((strut.LeadingDistribution ?? _content.DefaultLeadingDistribution) == TextLeadingDistribution.Even)
                {
                    double extra = (target - ascent - descent) / 2.0;
                    ascent += extra;
                    descent += extra;
                }
                else
                {
                    double multiplier = target / (ascent + descent + metrics.LineGap);
                    ascent *= multiplier;
                    descent *= multiplier;
                }
            }

            double leading = Math.Max(strut.Leading ?? 0.0, 0.0) * style.FontSize / 2.0;
            _strutBox = (ascent, descent);
            _strut = (ascent + leading, descent + leading);
        }

        _lines.Clear();
        double top = 0.0;
        TextHeightBehavior behavior = _content.Style.TextHeightBehavior ?? new TextHeightBehavior();
        IReadOnlyList<TextLine> lines = _layout!.TextLines;
        for (int index = 0; index < lines.Count; index++)
        {
            TextLine native = lines[index];
            double ascent = double.NegativeInfinity;
            double descent = double.NegativeInfinity;
            double unscaledAscent = 0;
            double unscaledDescent = 0;
            double reportedAscent = double.NegativeInfinity;
            double reportedDescent = double.NegativeInfinity;
            foreach (ShapedTextRun run in native.TextRuns.OfType<ShapedTextRun>())
            {
                if (Text.Length == 0)
                {
                    continue;
                }

                Accumulate(_shaping!.Style(run), run.TextMetrics, run.ShapedBuffer.GlyphTypeface);
            }

            if (double.IsNegativeInfinity(ascent))
            {
                ResolvedTextStyle style = (Text.Length == 0 ? _content.EmptyStyle
                    : _content.StyleAt(Math.Max(0, native.FirstTextSourceIndex - 1)))
                    with { LeadingDistribution = _content.EmptyLineLeadingDistribution };
                TextRunProperties properties = ParagraphTextSource.CreateRunProperties(style);
                Accumulate(style, new TextMetrics(properties.Typeface.GlyphTypeface, style.FontSize));
            }

            double textAscent = ascent;
            double textDescent = descent;
            foreach (PlaceholderTextRun run in native.TextRuns.OfType<PlaceholderTextRun>())
            {
                (double a, double d) = PlaceholderMetrics(run.Placeholder, textAscent, textDescent);
                ascent = Math.Max(ascent, a);
                descent = Math.Max(descent, d);
                (double runA, double runD) = PlaceholderMetrics(run.Placeholder, reportedAscent, reportedDescent);
                reportedAscent = Math.Max(reportedAscent, runA);
                reportedDescent = Math.Max(reportedDescent, runD);
            }

            if (_strut is { } strutMetrics)
            {
                bool force = _content.Style.StrutStyle!.ForceStrutHeight == true;
                ascent = force ? strutMetrics.Ascent : Math.Max(ascent, strutMetrics.Ascent);
                descent = force ? strutMetrics.Descent : Math.Max(descent, strutMetrics.Descent);
                if (force)
                {
                    unscaledAscent = _strutBox!.Value.Ascent;
                    unscaledDescent = _strutBox.Value.Descent;
                }
            }

            double height = Math.Round(ascent + descent, MidpointRounding.AwayFromZero);
            double metricsDescent = _content.Style.StrutStyle?.ForceStrutHeight == true && _strutBox is { } rawStrut
                ? rawStrut.Descent : descent;
            _lines.Add(new ParagraphLine(native, top, ascent, descent, unscaledAscent,
                unscaledDescent, height, textAscent, textDescent, reportedAscent, reportedDescent, metricsDescent));
            top += height;

            void Accumulate(ResolvedTextStyle style, TextMetrics metrics, GlyphTypeface? face = null)
            {
                double a = -metrics.Ascent;
                double d = metrics.Descent;
                double gap = metrics.LineGap;
                if (face is not null && _shaping!.Variable(face, style)?.Metrics is { } varied)
                {
                    (a, d, gap) = varied;
                }

                (double scaledA, double scaledD) = ScaledMetrics(a, d, gap,
                    style.FontSize, style.Height, style.LeadingDistribution);
                reportedAscent = Math.Max(reportedAscent, scaledA);
                reportedDescent = Math.Max(reportedDescent, scaledD);
                if (index == 0 && !behavior.ApplyHeightToFirstAscent)
                {
                    scaledA = a;
                }

                if (index == lines.Count - 1 && !behavior.ApplyHeightToLastDescent)
                {
                    scaledD = d;
                }

                ascent = Math.Max(ascent, scaledA);
                descent = Math.Max(descent, scaledD);
                unscaledAscent = Math.Max(unscaledAscent, a);
                unscaledDescent = Math.Max(unscaledDescent, d);
            }
        }

        _height = top;
    }

    private static (double Ascent, double Descent) PlaceholderMetrics(
        ParagraphPlaceholder placeholder, double textAscent, double textDescent) => placeholder.Alignment switch
    {
        PlaceholderAlignment.AboveBaseline => (placeholder.Height, 0),
        PlaceholderAlignment.BelowBaseline => (0, placeholder.Height),
        PlaceholderAlignment.Baseline => (placeholder.BaselineOffset, placeholder.Height - placeholder.BaselineOffset),
        PlaceholderAlignment.Top => (textAscent, placeholder.Height - textAscent),
        PlaceholderAlignment.Middle => ((textAscent - textDescent + placeholder.Height) / 2.0,
            (textDescent - textAscent + placeholder.Height) / 2.0),
        _ => (placeholder.Height - textDescent, textDescent),
    };

    private IEnumerable<(GlyphInfo Glyph, ResolvedTextStyle Style, bool Placeholder)> Glyphs(ParagraphLine line)
    {
        foreach ((int start, int end) in _clusters)
        {
            if (start < line.Native.FirstTextSourceIndex
                || start >= line.Native.FirstTextSourceIndex + line.Native.Length)
            {
                continue;
            }

            GlyphInfo? glyph = GlyphFor(line, start, end);
            if (glyph is not null)
            {
                yield return (glyph, _content.StyleAt(start), _content.Placeholders.Any(item => item.Index == start));
            }
        }
    }

    private GlyphInfo? GlyphFor(ParagraphLine line, int start, int end)
    {
        Rect? clusterBounds = null;
        TextDirection? direction = null;
        int covered = 0;
        foreach (TextBounds bounds in line.Native.GetTextBounds(start, end - start))
        {
            foreach (TextRunBounds runBounds in bounds.TextRunBounds)
            {
                double ascent;
                double descent;
                double delta = 0.0;
                if (runBounds.TextRun is ShapedTextRun shaped)
                {
                    if (!System.Runtime.InteropServices.MemoryMarshal.TryGetString(shaped.Text,
                            out string? text, out int runStart, out int runLength)
                        || !ReferenceEquals(text, Text))
                    {
                        continue;
                    }

                    int overlap = Math.Min(end, runStart + runLength) - Math.Max(start, runStart);
                    if (overlap <= 0)
                    {
                        continue;
                    }

                    covered += overlap;
                    ascent = -shaped.TextMetrics.Ascent;
                    descent = shaped.TextMetrics.Descent;
                    ResolvedTextStyle style = _shaping!.Style(shaped);
                    if (_shaping.Variable(shaped.ShapedBuffer.GlyphTypeface, style)?.Metrics is { } varied)
                    {
                        ascent = varied.Ascent;
                        descent = varied.Descent;
                    }

                    if (style.Height > TextDefaults.TextHeightNone)
                    {
                        delta = line.Height - line.Ascent - line.Descent;
                    }
                }
                else if (runBounds.TextRun is PlaceholderTextRun placeholder)
                {
                    covered++;
                    (ascent, descent) = PlaceholderMetrics(placeholder.Placeholder, line.TextAscent, line.TextDescent);
                }
                else
                {
                    continue;
                }

                Rect rect = runBounds.Rectangle;
                var part = new Rect(rect.X, line.Baseline + delta - ascent, rect.Width, ascent + descent);
                clusterBounds = clusterBounds is { } previous ? previous.Union(part) : part;
                direction ??= bounds.FlowDirection == FlowDirection.RightToLeft ? TextDirection.Rtl : TextDirection.Ltr;
            }
        }

        return clusterBounds is { } visible && covered >= end - start
            ? new GlyphInfo(visible, new TextRange(start, end), direction!.Value) : null;
    }

    public override GlyphInfo? GetGlyphInfoAt(int codeUnitOffset)
    {
        if (codeUnitOffset < 0 || codeUnitOffset >= Text.Length)
        {
            return null;
        }

        foreach ((int start, int end) in _clusters)
        {
            if (codeUnitOffset < start || codeUnitOffset >= end)
            {
                continue;
            }

            foreach (ParagraphLine line in _lines)
            {
                GlyphInfo? glyph = GlyphFor(line, start, end);
                if (glyph is not null)
                {
                    return glyph;
                }
            }

            break;
        }

        return null;
    }

    public override GlyphInfo? GetClosestGlyphInfoForOffset(Point offset)
    {
        ParagraphLine? line = LineForY(offset.Y);
        if (line is null || Text.Length == 0)
        {
            return null;
        }

        GlyphInfo? closest = null;
        double distance = double.PositiveInfinity;
        foreach ((GlyphInfo glyph, _, _) in Glyphs(line))
        {
            Rect bounds = glyph.GraphemeClusterLayoutBounds;
            double current = offset.X < bounds.Left ? bounds.Left - offset.X
                : offset.X > bounds.Right ? offset.X - bounds.Right : 0;
            if (current < distance)
            {
                closest = glyph;
                distance = current;
            }
        }

        return closest;
    }

    private ParagraphLine? LineForY(double y) =>
        _lines.FirstOrDefault(line => y < line.Top + line.Height) ?? _lines.LastOrDefault();

    public override TextPosition GetPositionForOffset(Point offset)
    {
        ParagraphLine? line = LineForY(offset.Y);
        if (line is null)
        {
            return new TextPosition(0);
        }

        CharacterHit hit = line.Native.GetCharacterHitFromDistance(offset.X);
        return new TextPosition(Math.Clamp(hit.FirstCharacterIndex + hit.TrailingLength, 0, Text.Length),
            hit.TrailingLength > 0 ? TextAffinity.Upstream : TextAffinity.Downstream);
    }

    public override IReadOnlyList<TextBox> GetBoxesForRange(
        int start, int end, BoxHeightStyle boxHeightStyle = BoxHeightStyle.Tight,
        BoxWidthStyle boxWidthStyle = BoxWidthStyle.Tight)
    {
        var boxes = new List<TextBox>();
        if (start < 0 || end <= start)
        {
            return boxes;
        }

        double left = _lines.Count == 0 ? 0 : _lines.Min(line => line.Native.Start);
        double right = _lines.Count == 0 ? 0 : _lines.Max(line => line.Native.Start + line.Native.Width);
        for (int index = 0; index < _lines.Count; index++)
        {
            ParagraphLine line = _lines[index];
            int first = boxes.Count;
            ResolvedTextStyle? previousStyle = null;
            bool previousPlaceholder = false;
            foreach ((GlyphInfo glyph, ResolvedTextStyle style, bool placeholder) in Glyphs(line)
                         .OrderBy(item => item.Glyph.GraphemeClusterLayoutBounds.X))
            {
                TextRange range = glyph.GraphemeClusterCodeUnitRange;
                if (range.Start < start || range.End > end)
                {
                    continue;
                }

                Rect rect = glyph.GraphemeClusterLayoutBounds;
                (double top, double bottom) = BoxVertical(index, rect, boxHeightStyle, placeholder);
                var box = new TextBox(rect.Left, top, rect.Right, bottom, glyph.WritingDirection);
                if (boxes.Count > first && !placeholder && !previousPlaceholder && Equals(previousStyle, style)
                    && boxes[^1].Direction == box.Direction && boxes[^1].Top == top && boxes[^1].Bottom == bottom
                    && Math.Abs(boxes[^1].Right - box.Left) < 0.001)
                {
                    boxes[^1] = boxes[^1] with { Right = box.Right };
                }
                else
                {
                    boxes.Add(box);
                }

                previousStyle = style;
                previousPlaceholder = placeholder;
            }

            if (boxWidthStyle == BoxWidthStyle.Max && boxes.Count > first)
            {
                bool startedBefore = start < line.Native.FirstTextSourceIndex;
                bool continuesAfter = end > line.Native.FirstTextSourceIndex + line.Native.Length;
                bool ltr = _content.TextDirection == TextDirection.Ltr;
                TextBox firstBox = boxes[first];
                TextBox lastBox = boxes[^1];
                if ((ltr ? continuesAfter : startedBefore) && lastBox.Right < right)
                {
                    boxes.Add(new TextBox(lastBox.Right, lastBox.Top, right, lastBox.Bottom, lastBox.Direction));
                }

                if ((ltr ? startedBefore : continuesAfter) && firstBox.Left > left)
                {
                    boxes.Insert(first, new TextBox(
                        left, firstBox.Top, firstBox.Left, firstBox.Bottom, firstBox.Direction));
                }
            }
        }

        return boxes;
    }

    private (double Top, double Bottom) BoxVertical(int index, Rect tight, BoxHeightStyle style, bool placeholder)
    {
        ParagraphLine line = _lines[index];
        if (style == BoxHeightStyle.Tight || (placeholder && style == BoxHeightStyle.Strut))
        {
            return (tight.Top, tight.Bottom);
        }

        if (style == BoxHeightStyle.Strut)
        {
            return _strutBox is { } strut ? (line.Baseline - strut.Ascent, line.Baseline + strut.Descent)
                : (tight.Top, tight.Bottom);
        }

        double delta = line.Height - line.Ascent - line.Descent;
        double extraAscent = line.Ascent - line.UnscaledAscent;
        double first = index == 0 ? extraAscent : 0.0;
        double last = index == _lines.Count - 1 ? extraAscent : 0.0;
        return style switch
        {
            BoxHeightStyle.Max => (line.Top + delta, line.Top + line.Height),
            BoxHeightStyle.IncludeLineSpacingTop =>
                (line.Top + delta + first, line.Top + line.Height),
            BoxHeightStyle.IncludeLineSpacingMiddle =>
                (line.Top + delta + (extraAscent + first) / 2.0,
                    line.Top + line.Height + (extraAscent - last) / 2.0),
            _ => (line.Top + delta + extraAscent, line.Top + line.Height + extraAscent - last),
        };
    }

    public override IReadOnlyList<TextBox> GetBoxesForPlaceholders()
    {
        var boxes = new List<(int Index, TextBox Box)>();
        foreach (ParagraphLine line in _lines)
        {
            foreach ((GlyphInfo glyph, _, bool placeholder) in Glyphs(line))
            {
                if (placeholder)
                {
                    Rect rect = glyph.GraphemeClusterLayoutBounds;
                    boxes.Add((glyph.GraphemeClusterCodeUnitRange.Start,
                        new TextBox(rect.Left, rect.Top, rect.Right, rect.Bottom, glyph.WritingDirection)));
                }
            }
        }

        return boxes.OrderBy(item => item.Index).Select(item => item.Box).ToArray();
    }

    private protected override TextRange GetLineRange(int offset)
    {
        foreach (ParagraphLine line in _lines)
        {
            int start = line.Native.FirstTextSourceIndex;
            int end = start + line.Native.Length - line.Native.NewLineLength;
            if (start <= offset && offset <= end)
            {
                return new TextRange(start, end);
            }
        }

        return TextRange.Empty;
    }

    public override int? GetLineNumberAt(int codeUnitOffset)
    {
        for (int index = 0; index < _lines.Count; index++)
        {
            TextLine line = _lines[index].Native;
            if (codeUnitOffset >= line.FirstTextSourceIndex && codeUnitOffset < line.FirstTextSourceIndex + line.Length
                && GetGlyphInfoAt(codeUnitOffset) is not null)
            {
                return index;
            }
        }

        return null;
    }

    private LineMetrics MetricsFor(int index)
    {
        ParagraphLine line = _lines[index];
        // The pinned native getMetrics uses max RUN metrics, excluding strut and edge trimming.
        // Its unscaledAscent deliberately equals ascent (SkParagraph's outstanding TODO).
        return new LineMetrics(line.Native.NewLineLength > 0 || index == _lines.Count - 1,
            line.ReportedAscent, line.ReportedDescent, line.ReportedAscent,
            Math.Round(line.ReportedAscent + line.ReportedDescent, MidpointRounding.AwayFromZero),
            line.Native.Width, line.Native.Start, line.Top + line.Height - line.MetricsDescent, index);
    }

    public override IReadOnlyList<LineMetrics> ComputeLineMetrics() =>
        Enumerable.Range(0, NumberOfLines).Select(MetricsFor).ToArray();

    public override LineMetrics? GetLineMetricsAt(int lineNumber) =>
        lineNumber >= 0 && lineNumber < NumberOfLines ? MetricsFor(lineNumber) : null;

    internal override Action<DrawingContext>? CreateDrawAction(Point offset)
    {
        if (_layout is null)
        {
            return null;
        }

        Picture picture = _paintedPicture ??= RecordPaint();
        return context => picture.Playback(context, offset);
    }

    private Picture RecordPaint()
    {
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        var outlines = new Dictionary<ShapedTextRun, Geometry>();
        foreach (ParagraphLine line in _lines)
        {
            // SkParagraph paints the complete line's backgrounds, then its shadows, glyphs and
            // decorations. A later span's shadow must never cover an earlier span's foreground.
            for (int phase = 0; phase < 4; phase++)
            {
                double x = line.Native.Start;
                foreach (TextRun run in line.Native.TextRuns)
                {
                    if (run is ShapedTextRun shaped)
                    {
                        ResolvedTextStyle style = _shaping!.Style(shaped);
                        double y = line.Baseline - shaped.Baseline;
                        canvas.Save();
                        canvas.Translate(x, y);
                        PaintGlyphs(canvas, shaped, style, phase, outlines);
                        canvas.Restore();
                    }

                    x += run is DrawableTextRun drawable ? drawable.Size.Width : 0;
                }
            }
        }

        return recorder.EndRecording();
    }

    private void PaintGlyphs(Canvas canvas, ShapedTextRun run, ResolvedTextStyle style, int phase,
        Dictionary<ShapedTextRun, Geometry> outlines)
    {
        GlyphRun glyphs = run.GlyphRun;
        switch (phase)
        {
            case 0:
                if (style.Background is { } background)
                {
                    canvas.DrawTextGeometry(new RectangleGeometry(glyphs.Bounds), background);
                }

                break;
            case 1:
                if (style.Shadows is { } shadows)
                {
                    foreach (Rendering.Shadow shadow in shadows)
                    {
                        canvas.Save();
                        canvas.Translate(shadow.Offset.X, shadow.Offset.Y);
                        canvas.DrawTextGeometry(Outline(), new Paint
                        {
                            Color = shadow.Color,
                            MaskFilter = MaskFilter.Blur(BlurStyle.Normal, shadow.BlurSigma),
                        });
                        canvas.Restore();
                    }
                }

                break;
            case 2:
                if (_shaping!.Variable(run.ShapedBuffer.GlyphTypeface, style) is not null
                    || style.Foreground is not null)
                {
                    canvas.DrawTextGeometry(Outline(), style.Foreground ?? new Paint { Color = style.Color });
                }
                else
                {
                    // Keep color/bitmap glyphs on the native draw path. Copy pooled shaping data so
                    // the recorded picture survives paragraph relayout/disposal.
                    var retained = new GlyphRun(glyphs.GlyphTypeface, glyphs.FontRenderingEmSize,
                        glyphs.Characters, glyphs.GlyphInfos.ToArray(), glyphs.BaselineOrigin, glyphs.BiDiLevel);
                    var brush = new SolidColorBrush(style.Color);
                    canvas.AddDrawCommand(context => context.DrawGlyphRun(brush, retained));
                }

                break;
            case 3:
                ParagraphDecorations.Paint(canvas, run, style);
                break;
        }

        Geometry Outline()
        {
            if (!outlines.TryGetValue(run, out Geometry? geometry))
            {
                VariableFont? variable = _shaping!.Variable(run.ShapedBuffer.GlyphTypeface, style);
                geometry = variable is null ? glyphs.BuildGeometry() : variable.Outline(glyphs);
                outlines.Add(run, geometry);
            }

            return geometry;
        }
    }

    private protected override void DisposeCore()
    {
        _layout?.Dispose();
        _shaping?.Dispose();
    }

    private sealed record ParagraphLine(
        TextLine Native, double Top, double Ascent, double Descent, double UnscaledAscent,
        double UnscaledDescent, double Height, double TextAscent, double TextDescent,
        double ReportedAscent, double ReportedDescent, double MetricsDescent)
    {
        internal double Baseline => Top + Ascent;
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
        double lineHeight = double.NaN;
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
            letterSpacing: 0.0);
    }

    internal static TextRunProperties CreateRunProperties(ResolvedTextStyle style)
    {
        var typeface = new Typeface(ResolveFontFamily(style), style.FontStyle, style.FontWeight);
        return new GenericTextRunProperties(
            typeface,
            style.FontSize,
            ResolveDecorations(style),
            style.Foreground is { } foreground ? PaintBrush(foreground) : new SolidColorBrush(style.Color),
            style.Background is { } background ? PaintBrush(background) : null,
            cultureInfo: style.Locale is string locale ? CultureInfo.GetCultureInfo(locale) : null,
            fontFeatures: ResolveFontFeatures(style.FontFeatures));
    }

    // The engine tries `fontFamilyFallback` after the family; Avalonia reads the same order from a
    // comma-separated family name.
    private static FontFamily ResolveFontFamily(ResolvedTextStyle style)
    {
        if (style.FontFamilyFallback is not { Count: > 0 } fallback)
        {
            return style.FontFamily;
        }

        var familyNames = new List<string> { style.FontFamily.Name };
        familyNames.AddRange(fallback);
        return new FontFamily(string.Join(',', familyNames));
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
