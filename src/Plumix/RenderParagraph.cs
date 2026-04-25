using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/paragraph.dart (approximate)

namespace Plumix;

public sealed class RenderParagraph : RenderBox
{
    private string _text;
    private FontFamily _fontFamily = Avalonia.Media.FontFamily.Default;
    private FontStyle _fontStyle = FontStyle.Normal;
    private FontWeight _fontWeight = FontWeight.Normal;
    private FontStretch _fontStretch = FontStretch.Normal;
    private double _fontSize = 20;
    private IBrush _foreground = Brushes.White;
    private TextAlign _textAlign = TextAlign.Start;
    private TextDirection _textDirection = TextDirection.Ltr;
    private bool _softWrap = true;
    private int? _maxLines;
    private TextOverflow _overflow = TextOverflow.Clip;
    private double? _height;
    private double _letterSpacing;
    private TextLayout? _layout;

    public RenderParagraph(string text)
    {
        _text = text ?? string.Empty;
    }

    public string Text
    {
        get => _text;
        set
        {
            var next = value ?? string.Empty;
            if (string.Equals(_text, next, StringComparison.Ordinal))
            {
                return;
            }

            _text = next;
            MarkNeedsLayout();
            MarkNeedsSemanticsUpdate();
        }
    }

    public Typeface Typeface
    {
        get => new Typeface(_fontFamily, _fontStyle, _fontWeight, _fontStretch);
        set
        {
            if (Equals(_fontFamily, value.FontFamily)
                && _fontStyle == value.Style
                && _fontWeight == value.Weight
                && _fontStretch == value.Stretch)
            {
                return;
            }

            _fontFamily = value.FontFamily;
            _fontStyle = value.Style;
            _fontWeight = value.Weight;
            _fontStretch = value.Stretch;
            MarkNeedsLayout();
        }
    }

    public FontFamily FontFamily
    {
        get => _fontFamily;
        set
        {
            var next = value ?? Avalonia.Media.FontFamily.Default;
            if (Equals(_fontFamily, next))
            {
                return;
            }

            _fontFamily = next;
            MarkNeedsLayout();
        }
    }

    public FontStyle FontStyle
    {
        get => _fontStyle;
        set
        {
            if (_fontStyle == value)
            {
                return;
            }

            _fontStyle = value;
            MarkNeedsLayout();
        }
    }

    public FontWeight FontWeight
    {
        get => _fontWeight;
        set
        {
            if (_fontWeight == value)
            {
                return;
            }

            _fontWeight = value;
            MarkNeedsLayout();
        }
    }

    public FontStretch FontStretch
    {
        get => _fontStretch;
        set
        {
            if (_fontStretch == value)
            {
                return;
            }

            _fontStretch = value;
            MarkNeedsLayout();
        }
    }

    public double FontSize
    {
        get => _fontSize;
        set
        {
            if (Math.Abs(_fontSize - value) < 0.01)
            {
                return;
            }

            _fontSize = value;
            MarkNeedsLayout();
        }
    }

    public IBrush Foreground
    {
        get => _foreground;
        set
        {
            var next = value ?? Brushes.White;
            if (Equals(_foreground, next))
            {
                return;
            }

            _foreground = next;
            MarkNeedsPaint();
        }
    }

    public TextAlign TextAlign
    {
        get => _textAlign;
        set
        {
            if (_textAlign == value)
            {
                return;
            }

            _textAlign = value;
            MarkNeedsLayout();
        }
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set
        {
            if (_textDirection == value)
            {
                return;
            }

            _textDirection = value;
            MarkNeedsLayout();
        }
    }

    public bool SoftWrap
    {
        get => _softWrap;
        set
        {
            if (_softWrap == value)
            {
                return;
            }

            _softWrap = value;
            MarkNeedsLayout();
        }
    }

    public int? MaxLines
    {
        get => _maxLines;
        set
        {
            if (_maxLines == value)
            {
                return;
            }

            _maxLines = value;
            MarkNeedsLayout();
        }
    }

    public TextOverflow Overflow
    {
        get => _overflow;
        set
        {
            if (_overflow == value)
            {
                return;
            }

            _overflow = value;
            MarkNeedsLayout();
        }
    }

    public double? Height
    {
        get => _height;
        set
        {
            if (_height == value)
            {
                return;
            }

            _height = value;
            MarkNeedsLayout();
        }
    }

    public double LetterSpacing
    {
        get => _letterSpacing;
        set
        {
            if (Math.Abs(_letterSpacing - value) < 0.01)
            {
                return;
            }

            _letterSpacing = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        var maxWidth = double.IsInfinity(Constraints.MaxWidth)
            ? double.PositiveInfinity
            : Math.Max(0, Constraints.MaxWidth);
        var maxHeight = double.IsInfinity(Constraints.MaxHeight)
            ? double.PositiveInfinity
            : Math.Max(0, Constraints.MaxHeight);
        var lineHeight = _height is > 0
            ? Math.Max(0.01, _fontSize * _height.Value)
            : double.NaN;
        var typeface = new Typeface(_fontFamily, _fontStyle, _fontWeight, _fontStretch);

        try
        {
            _layout = CreateTextLayout(typeface, maxWidth, maxHeight, lineHeight);

            if (ShouldTightenAlignedWidth(_layout, maxWidth))
            {
                var tightenedWidth = Math.Max(0, Math.Min(maxWidth, _layout.WidthIncludingTrailingWhitespace));
                if (tightenedWidth > 0)
                {
                    _layout = CreateTextLayout(typeface, tightenedWidth, maxHeight, lineHeight);
                }
            }

            Size = Constraints.Constrain(new Size(_layout.Width, _layout.Height));
        }
        catch (Exception exception) when (TextLayoutFallback.IsMissingFontManager(exception))
        {
            _layout = null;
            Size = Constraints.Constrain(TextLayoutFallback.EstimateTextSize(
                _text,
                _fontSize,
                maxWidth,
                _height,
                _letterSpacing));
        }
    }

    private TextLayout CreateTextLayout(Typeface typeface, double maxWidth, double maxHeight, double lineHeight)
    {
        return new TextLayout(
            text: _text,
            typeface: typeface,
            fontSize: _fontSize,
            foreground: _foreground,
            textAlignment: ResolveTextAlignment(_textAlign, _textDirection),
            textWrapping: _softWrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
            textTrimming: ResolveTextTrimming(_overflow),
            flowDirection: _textDirection == TextDirection.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            maxWidth: maxWidth,
            maxHeight: maxHeight,
            lineHeight: lineHeight,
            letterSpacing: _letterSpacing,
            maxLines: _maxLines ?? 0);
    }

    private bool ShouldTightenAlignedWidth(TextLayout layout, double maxWidth)
    {
        if (!double.IsFinite(maxWidth) || maxWidth <= 0)
        {
            return false;
        }

        if (Constraints.MinWidth >= maxWidth - 0.01)
        {
            return false;
        }

        if (_textAlign is not (TextAlign.Center or TextAlign.Right or TextAlign.End))
        {
            return false;
        }

        if (string.IsNullOrEmpty(_text))
        {
            return false;
        }

        var firstGlyph = layout.HitTestTextPosition(0);
        return firstGlyph.X > 0.01;
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_layout != null)
        {
            ctx.DrawTextLayout(_layout, offset);
        }
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        configuration.IsSemanticBoundary = true;
        configuration.Label = _text;
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
            _ => direction == TextDirection.Rtl ? TextAlignment.Right : TextAlignment.Left
        };
    }

    private static TextTrimming ResolveTextTrimming(TextOverflow overflow)
    {
        return overflow switch
        {
            TextOverflow.Ellipsis => TextTrimming.CharacterEllipsis,
            _ => TextTrimming.None
        };
    }
}
