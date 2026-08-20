using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.UI;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Painting;

// Dart parity source: flutter/packages/flutter/lib/src/painting/text_painter.dart

/// Lays an <see cref="InlineSpan"/> out for measurement outside a render object.
///
/// Flutter's `TextPainter` is also the paint-time engine behind `RenderParagraph`; Plumix's
/// paragraph render objects drive Avalonia's <see cref="TextLayout"/> directly. This type retains
/// that layout so paint-time controls can draw it after measuring it. Selection boxes, hit testing
/// and caret metrics stay on `RenderParagraph`.
///
/// Like the paragraph render objects, this falls back to
/// <see cref="TextLayoutFallback.EstimateTextSize"/> on hosts with no font manager, so headless
/// test environments measure rather than throw.
public sealed class TextPainter : IDisposable
{
    private InlineSpan? _text;
    private TextDirection _textDirection;
    private TextAlign _textAlign;
    private int? _maxLines;
    private TextScaler _textScaler;
    private TextLayout? _layout;
    private Size? _size;

    public TextPainter(
        InlineSpan? text = null,
        TextAlign textAlign = TextAlign.Start,
        TextDirection textDirection = TextDirection.Ltr,
        int? maxLines = null,
        TextScaler? textScaler = null)
    {
        if (maxLines is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLines), "maxLines must be greater than zero.");
        }

        _text = text;
        _textAlign = textAlign;
        _textDirection = textDirection;
        _maxLines = maxLines;
        _textScaler = textScaler ?? TextScaler.NoScaling;
    }

    /// The span to lay out. Assigning a different span discards the previous layout.
    public InlineSpan? Text
    {
        get => _text;
        set
        {
            if (ReferenceEquals(_text, value)) return;
            _text = value;
            MarkNeedsLayout();
        }
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set { if (_textDirection != value) { _textDirection = value; MarkNeedsLayout(); } }
    }

    public TextAlign TextAlign
    {
        get => _textAlign;
        set { if (_textAlign != value) { _textAlign = value; MarkNeedsLayout(); } }
    }

    public int? MaxLines
    {
        get => _maxLines;
        set
        {
            if (value is <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "maxLines must be greater than zero.");
            }

            if (_maxLines == value) return;
            _maxLines = value;
            MarkNeedsLayout();
        }
    }

    public TextScaler TextScaler
    {
        get => _textScaler;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_textScaler.Equals(value)) return;
            _textScaler = value;
            MarkNeedsLayout();
        }
    }

    /// The size of the text as laid out by the last <see cref="Layout"/> call.
    public Size Size => _size ?? throw new InvalidOperationException(
        "TextPainter.Size was read before Layout was called.");

    public double Width => Size.Width;

    public double Height => Size.Height;

    /// The plain text of the laid-out span, or an empty string when there is no span.
    public string PlainText => _text?.ToPlainText() ?? string.Empty;

    /// Computes the visual position of the glyphs, constrained to the given width range.
    public void Layout(double minWidth = 0.0, double maxWidth = double.PositiveInfinity)
    {
        if (minWidth < 0 || double.IsNaN(minWidth))
        {
            throw new ArgumentOutOfRangeException(nameof(minWidth));
        }

        if (maxWidth < minWidth || double.IsNaN(maxWidth))
        {
            throw new ArgumentOutOfRangeException(nameof(maxWidth));
        }

        DisposeLayout();
        if (_text is null)
        {
            _size = new Size(minWidth, 0);
            return;
        }

        ParagraphSource source = ParagraphSource.Build(_text, _textScaler, [], _textDirection);
        try
        {
            _layout = new TextLayout(
                source,
                source.CreateParagraphProperties(
                    ResolveTextAlignment(_textAlign, _textDirection),
                    TextWrapping.Wrap,
                    _textDirection == TextDirection.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight),
                TextTrimming.None,
                maxWidth,
                double.PositiveInfinity,
                _maxLines ?? 0);
            _size = new Size(Math.Max(minWidth, _layout.Width), _layout.Height);
        }
        catch (Exception exception) when (TextLayoutFallback.IsMissingFontManager(exception))
        {
            TextStyle rootStyle = _text.Style ?? TextStyle.Fallback;
            Size estimate = TextLayoutFallback.EstimateTextSize(
                source.PlainText,
                _textScaler.Scale(rootStyle.FontSize ?? TextDefaults.DefaultFontSize),
                maxWidth,
                rootStyle.Height,
                rootStyle.LetterSpacing ?? 0);
            _size = new Size(Math.Max(minWidth, estimate.Width), estimate.Height);
        }
    }

    public void Paint(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_size is null)
        {
            throw new InvalidOperationException("TextPainter.Paint was called before Layout.");
        }

        if (_layout is not null)
        {
            context.DrawTextLayout(_layout, offset);
        }
    }

    public void Dispose()
    {
        DisposeLayout();
        _size = null;
    }

    private void MarkNeedsLayout()
    {
        DisposeLayout();
        _size = null;
    }

    private void DisposeLayout()
    {
        _layout = null;
    }

    private static TextAlignment ResolveTextAlignment(TextAlign align, TextDirection direction)
    {
        return align switch
        {
            TextAlign.Left => TextAlignment.Left,
            TextAlign.Right => TextAlignment.Right,
            TextAlign.Center => TextAlignment.Center,
            TextAlign.Justify => TextAlignment.Justify,
            TextAlign.End => direction == TextDirection.Rtl ? TextAlignment.Left : TextAlignment.Right,
            _ => direction == TextDirection.Rtl ? TextAlignment.Right : TextAlignment.Left,
        };
    }
}
