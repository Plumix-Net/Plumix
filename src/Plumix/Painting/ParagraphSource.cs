using System.Text;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/painting/text_painter.dart
// The engine's ParagraphBuilder push/pop/addPlaceholder protocol, expressed against
// Avalonia's ITextSource so that one InlineSpan tree produces one TextLayout.

namespace Plumix.Painting;

/// Flattens an [InlineSpan] tree into the styled runs and placeholder runs that
/// Avalonia's text formatter consumes.
internal sealed class ParagraphSource : ITextSource
{
    private readonly List<Segment> _segments;
    private readonly ReadOnlyMemory<char> _text;
    private readonly TextRunProperties _defaultProperties;
    private readonly TextStyle _rootStyle;
    private readonly TextScaler _scaler;

    private ParagraphSource(
        string plainText,
        List<Segment> segments,
        TextRunProperties defaultProperties,
        TextStyle rootStyle,
        TextScaler scaler)
    {
        PlainText = plainText;
        _text = plainText.AsMemory();
        _segments = segments;
        _defaultProperties = defaultProperties;
        _rootStyle = rootStyle;
        _scaler = scaler;
    }

    /// The flattened text, one 0xFFFC code unit per placeholder.
    public string PlainText { get; }

    /// Builds the source for the given span tree.
    ///
    /// `dimensions` must contain one entry per [PlaceholderSpan] in the tree, in
    /// the same order as the tree defines them.
    public static ParagraphSource Build(
        InlineSpan root,
        TextScaler scaler,
        IReadOnlyList<PlaceholderDimensions> dimensions,
        TextDirection direction)
    {
        var buffer = new StringBuilder();
        var segments = new List<Segment>();
        TextStyle rootStyle = root.Style ?? TextStyle.Fallback;
        int placeholderIndex = 0;
        Visit(root, rootStyle, buffer, segments, dimensions, scaler, ref placeholderIndex);
        string plainText = buffer.ToString();
        return new ParagraphSource(
            plainText,
            segments,
            CreateRunProperties(rootStyle, scaler),
            rootStyle,
            scaler);
    }

    private static void Visit(
        InlineSpan span,
        TextStyle inherited,
        StringBuilder buffer,
        List<Segment> segments,
        IReadOnlyList<PlaceholderDimensions> dimensions,
        TextScaler scaler,
        ref int placeholderIndex)
    {
        TextStyle effective = span.Style is null ? inherited : inherited.Merge(span.Style);
        switch (span)
        {
            case TextSpan textSpan:
                if (!string.IsNullOrEmpty(textSpan.Text))
                {
                    segments.Add(new Segment(buffer.Length, textSpan.Text.Length, effective, null));
                    buffer.Append(textSpan.Text);
                }

                if (textSpan.Children is not null)
                {
                    foreach (InlineSpan child in textSpan.Children)
                    {
                        Visit(child, effective, buffer, segments, dimensions, scaler, ref placeholderIndex);
                    }
                }

                break;
            case PlaceholderSpan placeholder:
                PlaceholderDimensions dimension = placeholderIndex < dimensions.Count
                    ? dimensions[placeholderIndex]
                    : PlaceholderDimensions.Empty;
                segments.Add(new Segment(buffer.Length, 1, effective, dimension));
                buffer.Append((char)PlaceholderSpan.PlaceholderCodeUnit);
                placeholderIndex += 1;
                break;
        }
    }

    public TextRun? GetTextRun(int textSourceIndex)
    {
        if (textSourceIndex >= PlainText.Length)
        {
            return null;
        }

        Segment segment = SegmentAt(textSourceIndex);
        if (segment.Placeholder is { } placeholder)
        {
            return new PlaceholderTextRun(placeholder, CreateRunProperties(segment.Style, _scaler));
        }

        int end = segment.Start + segment.Length;
        int nextPlaceholder = NextPlaceholderStart(textSourceIndex);
        end = Math.Min(end, nextPlaceholder);
        return new TextCharacters(
            _text.Slice(textSourceIndex, end - textSourceIndex),
            CreateRunProperties(segment.Style, _scaler));
    }

    /// Builds the paragraph-level formatting properties from the root style.
    public TextParagraphProperties CreateParagraphProperties(
        TextAlignment alignment,
        TextWrapping wrapping,
        FlowDirection flowDirection)
    {
        double scaledFontSize = _scaler.Scale(_rootStyle.FontSize ?? TextDefaults.DefaultFontSize);
        double lineHeight = _rootStyle.Height is > 0
            ? Math.Max(0.01, scaledFontSize * _rootStyle.Height.Value)
            : double.NaN;
        return new GenericTextParagraphProperties(
            flowDirection,
            alignment,
            firstLineInParagraph: true,
            alwaysCollapsible: false,
            _defaultProperties,
            wrapping,
            lineHeight,
            indent: 0,
            letterSpacing: _rootStyle.LetterSpacing ?? 0);
    }

    /// Resolves the laid-out rectangle of every placeholder, in tree order.
    ///
    /// Placeholders that were truncated away produce no entry, so the result can
    /// be shorter than the placeholder count.
    public IReadOnlyList<Rect> ResolvePlaceholderBoxes(TextLayout layout)
    {
        if (PlaceholderCount == 0)
        {
            return [];
        }

        var boxes = new List<Rect>();
        double lineTop = 0.0;
        foreach (TextLine line in layout.TextLines)
        {
            double x = line.Start;
            foreach (TextRun run in line.TextRuns)
            {
                if (run is PlaceholderTextRun placeholder)
                {
                    boxes.Add(new Rect(
                        new Point(x, lineTop + VerticalOffset(placeholder.Dimensions, line)),
                        placeholder.Size));
                    x += placeholder.Size.Width;
                    continue;
                }

                x += RunWidth(run);
            }

            lineTop += line.Height;
        }

        return boxes;
    }

    /// Produces deterministic placeholder rectangles when no font manager is
    /// available and the paragraph size had to be estimated.
    public IReadOnlyList<Rect> EstimatePlaceholderBoxes(Size size)
    {
        if (PlaceholderCount == 0)
        {
            return [];
        }

        var boxes = new List<Rect>();
        double x = 0.0;
        foreach (Segment segment in _segments)
        {
            if (segment.Placeholder is not { } placeholder)
            {
                x += segment.Length * Math.Max(0.1, _scaler.Scale(
                    segment.Style.FontSize ?? TextDefaults.DefaultFontSize) * 0.6);
                continue;
            }

            boxes.Add(new Rect(new Point(x, 0), placeholder.Size));
            x += placeholder.Size.Width;
        }

        return boxes;
    }

    /// Whether laying out with `maxLines` dropped content that would otherwise
    /// have been visible.
    public bool HasTrailingContentAfter(TextLayout layout, int maxLines)
    {
        if (layout.TextLines.Count < maxLines)
        {
            return false;
        }

        TextLine last = layout.TextLines[^1];
        return last.HasCollapsed
               || last.FirstTextSourceIndex + last.Length < PlainText.Length;
    }

    private int PlaceholderCount => _segments.Count(segment => segment.Placeholder is not null);

    private Segment SegmentAt(int index)
    {
        for (int i = 0; i < _segments.Count; i += 1)
        {
            Segment segment = _segments[i];
            if (index >= segment.Start && index < segment.Start + segment.Length)
            {
                return segment;
            }
        }

        return new Segment(index, PlainText.Length - index, _rootStyle, null);
    }

    private int NextPlaceholderStart(int index)
    {
        foreach (Segment segment in _segments)
        {
            if (segment.Placeholder is not null && segment.Start > index)
            {
                return segment.Start;
            }
        }

        return PlainText.Length;
    }

    private static double RunWidth(TextRun run)
    {
        return run is DrawableTextRun drawable ? drawable.Size.Width : 0.0;
    }

    private static double VerticalOffset(PlaceholderDimensions dimensions, TextLine line)
    {
        double height = dimensions.Size.Height;
        return dimensions.Alignment switch
        {
            PlaceholderAlignment.Top => 0.0,
            PlaceholderAlignment.Bottom => line.Height - height,
            PlaceholderAlignment.Middle => (line.Height - height) / 2.0,
            PlaceholderAlignment.AboveBaseline => line.Baseline - height,
            PlaceholderAlignment.BelowBaseline => line.Baseline,
            _ => line.Baseline - (dimensions.BaselineOffset ?? height),
        };
    }

    private static TextRunProperties CreateRunProperties(TextStyle style, TextScaler scaler)
    {
        var typeface = new Typeface(
            style.FontFamily ?? FontFamily.Default,
            style.FontStyle ?? Avalonia.Media.FontStyle.Normal,
            style.FontWeight ?? Avalonia.Media.FontWeight.Normal);
        return new GenericTextRunProperties(
            typeface,
            scaler.Scale(style.FontSize ?? TextDefaults.DefaultFontSize),
            ResolveDecorations(style),
            new SolidColorBrush(style.Color ?? Colors.Black));
    }

    private static TextDecorationCollection? ResolveDecorations(TextStyle style)
    {
        if (style.Decoration is not { } decoration || decoration == UI.TextDecoration.None)
        {
            return null;
        }

        var decorations = new TextDecorationCollection();
        if (decoration.HasFlag(UI.TextDecoration.Underline))
        {
            decorations.Add(new Avalonia.Media.TextDecoration { Location = TextDecorationLocation.Underline });
        }

        if (decoration.HasFlag(UI.TextDecoration.Overline))
        {
            decorations.Add(new Avalonia.Media.TextDecoration { Location = TextDecorationLocation.Overline });
        }

        if (decoration.HasFlag(UI.TextDecoration.LineThrough))
        {
            decorations.Add(new Avalonia.Media.TextDecoration { Location = TextDecorationLocation.Strikethrough });
        }

        return decorations;
    }

    private readonly record struct Segment(
        int Start,
        int Length,
        TextStyle Style,
        PlaceholderDimensions? Placeholder);
}

/// A text run that reserves the space an inline widget occupies.
///
/// The widget itself is painted by [RenderParagraph] after the glyphs, so this
/// run only contributes metrics to the line.
internal sealed class PlaceholderTextRun : DrawableTextRun
{
    public PlaceholderTextRun(PlaceholderDimensions dimensions, TextRunProperties properties)
    {
        Dimensions = dimensions;
        Properties = properties;
    }

    public PlaceholderDimensions Dimensions { get; }

    public override Size Size => Dimensions.Size;

    public override double Baseline => Dimensions.Alignment switch
    {
        PlaceholderAlignment.AboveBaseline => Dimensions.Size.Height,
        PlaceholderAlignment.BelowBaseline => 0.0,
        PlaceholderAlignment.Baseline => Dimensions.BaselineOffset ?? Dimensions.Size.Height,
        _ => Dimensions.Size.Height,
    };

    public override int Length => 1;

    public override TextRunProperties Properties { get; }

    public override void Draw(DrawingContext drawingContext, Point origin)
    {
        // Intentionally empty: RenderParagraph paints the inline child itself.
    }
}
