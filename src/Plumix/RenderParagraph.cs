using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

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
    private TextWidthBasis _textWidthBasis = TextWidthBasis.Parent;
    private TextHeightBehavior? _textHeightBehavior;
    private double? _height;
    private double _letterSpacing;
    private TextDecorationCollection? _textDecorations;
    private TextLayout? _layout;
    private ITextSelectionRegistrar? _selectionRegistrar;
    private Color _selectionColor = Color.FromArgb(0x66, 0x67, 0x50, 0xA4);
    private Color _cursorColor = Color.Parse("#FF6750A4");
    private int _selectionBaseOffset;
    private int _selectionExtentOffset;
    private bool _showCursor;
    private double _cursorWidth = 2.0;
    private double? _cursorHeight;
    private bool _selectionEnabled;

    public RenderParagraph(string text)
    {
        _text = text ?? string.Empty;
    }

    public string Text
    {
        get => _text;
        set
        {
            string next = value ?? string.Empty;
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

    public TextDecorationCollection? TextDecorations
    {
        get => _textDecorations;
        set
        {
            if (ReferenceEquals(_textDecorations, value))
            {
                return;
            }

            _textDecorations = value;
            MarkNeedsLayout();
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

    public TextWidthBasis TextWidthBasis
    {
        get => _textWidthBasis;
        set
        {
            if (_textWidthBasis == value)
            {
                return;
            }

            _textWidthBasis = value;
            MarkNeedsLayout();
        }
    }

    public TextHeightBehavior? TextHeightBehavior
    {
        get => _textHeightBehavior;
        set
        {
            if (_textHeightBehavior == value)
            {
                return;
            }

            _textHeightBehavior = value;
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

    internal ITextSelectionRegistrar? SelectionRegistrar
    {
        get => _selectionRegistrar;
        set
        {
            if (ReferenceEquals(_selectionRegistrar, value))
            {
                return;
            }

            _selectionRegistrar?.Unregister(this);
            _selectionRegistrar = value;
            if (Attached)
            {
                _selectionRegistrar?.Register(this);
            }
            MarkNeedsPaint();
        }
    }

    public Color SelectionColor
    {
        get => _selectionColor;
        set
        {
            if (_selectionColor == value)
            {
                return;
            }
            _selectionColor = value;
            MarkNeedsPaint();
        }
    }

    public Color CursorColor
    {
        get => _cursorColor;
        set
        {
            if (_cursorColor == value)
            {
                return;
            }
            _cursorColor = value;
            MarkNeedsPaint();
        }
    }

    public int SelectionBaseOffset => _selectionBaseOffset;

    public int SelectionExtentOffset => _selectionExtentOffset;

    public bool ShowCursor
    {
        get => _showCursor;
        set
        {
            if (_showCursor == value)
            {
                return;
            }
            _showCursor = value;
            MarkNeedsPaint();
        }
    }

    public double CursorWidth
    {
        get => _cursorWidth;
        set
        {
            if (Math.Abs(_cursorWidth - value) < 0.01)
            {
                return;
            }
            _cursorWidth = value;
            MarkNeedsPaint();
        }
    }

    public double? CursorHeight
    {
        get => _cursorHeight;
        set
        {
            if (_cursorHeight == value)
            {
                return;
            }
            _cursorHeight = value;
            MarkNeedsPaint();
        }
    }

    public bool SelectionEnabled
    {
        get => _selectionEnabled;
        set
        {
            if (_selectionEnabled == value)
            {
                return;
            }
            _selectionEnabled = value;
            MarkNeedsPaint();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        return MeasureForConstraints(new BoxConstraints(MaxHeight: NormalizeIntrinsicExtent(height))).Size.Width;
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        return MeasureForConstraints(new BoxConstraints(MaxHeight: NormalizeIntrinsicExtent(height))).Size.Width;
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        return MeasureForConstraints(new BoxConstraints(MaxWidth: NormalizeIntrinsicExtent(width))).Size.Height;
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        return ComputeMinIntrinsicHeight(width);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return MeasureForConstraints(constraints).Size;
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        (TextLayout? layout, Size size) = MeasureForConstraints(constraints);
        if (layout is not null)
        {
            return layout.Baseline;
        }

        double lineHeight = _height is > 0 ? _fontSize * _height.Value : _fontSize * 1.2;
        return Math.Min(size.Height, lineHeight * 0.8);
    }

    protected override void PerformLayout()
    {
        (TextLayout? layout, Size size) = MeasureForConstraints(Constraints);
        _layout = layout;
        Size = size;
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        if (_layout is not null)
        {
            return _layout.Baseline;
        }

        if (!HasSize)
        {
            return null;
        }

        double lineHeight = _height is > 0 ? _fontSize * _height.Value : _fontSize * 1.2;
        return Math.Min(Size.Height, lineHeight * 0.8);
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
            textDecorations: _textDecorations,
            flowDirection: _textDirection == TextDirection.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            maxWidth: maxWidth,
            maxHeight: maxHeight,
            lineHeight: lineHeight,
            letterSpacing: _letterSpacing,
            maxLines: _maxLines ?? 0);
    }

    private (TextLayout? Layout, Size Size) MeasureForConstraints(BoxConstraints constraints)
    {
        double maxWidth = double.IsInfinity(constraints.MaxWidth)
            ? double.PositiveInfinity
            : Math.Max(0, constraints.MaxWidth);
        double maxHeight = double.IsInfinity(constraints.MaxHeight)
            ? double.PositiveInfinity
            : Math.Max(0, constraints.MaxHeight);
        double lineHeight = _height is > 0
            ? Math.Max(0.01, _fontSize * _height.Value)
            : double.NaN;
        var typeface = new Typeface(_fontFamily, _fontStyle, _fontWeight, _fontStretch);

        try
        {
            TextLayout layout = CreateTextLayout(typeface, maxWidth, maxHeight, lineHeight);
            if (ShouldTightenAlignedWidth(layout, maxWidth, constraints))
            {
                double tightenedWidth = Math.Max(0, Math.Min(maxWidth, layout.WidthIncludingTrailingWhitespace));
                if (tightenedWidth > 0)
                {
                    layout = CreateTextLayout(typeface, tightenedWidth, maxHeight, lineHeight);
                }
            }

            double layoutWidth = _textWidthBasis == TextWidthBasis.LongestLine
                ? layout.WidthIncludingTrailingWhitespace
                : layout.Width;
            return (layout, constraints.Constrain(new Size(layoutWidth, layout.Height)));
        }
        catch (Exception exception) when (TextLayoutFallback.IsMissingFontManager(exception))
        {
            Size estimate = TextLayoutFallback.EstimateTextSize(
                _text,
                _fontSize,
                maxWidth,
                _height,
                _letterSpacing);
            return (null, constraints.Constrain(estimate));
        }
    }

    private bool ShouldTightenAlignedWidth(
        TextLayout layout,
        double maxWidth,
        BoxConstraints constraints)
    {
        if (!double.IsFinite(maxWidth) || maxWidth <= 0)
        {
            return false;
        }

        if (constraints.MinWidth >= maxWidth - 0.01)
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

    private static double NormalizeIntrinsicExtent(double value)
    {
        return double.IsNaN(value) || value < 0.0 ? 0.0 : value;
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_layout != null)
        {
            PaintSelection(ctx, offset);
            if (_overflow == TextOverflow.Fade &&
                _layout.WidthIncludingTrailingWhitespace > Size.Width + 0.01)
            {
                ctx.DrawTextLayoutWithHorizontalFade(
                    _layout,
                    offset,
                    new Rect(offset, Size),
                    fadeTowardRight: _textDirection == TextDirection.Ltr);
            }
            else
            {
                ctx.DrawTextLayout(_layout, offset);
            }
            PaintCursor(ctx, offset);
        }
    }

    protected override bool HitTestSelf(Point position)
    {
        return _selectionRegistrar is not null && _selectionEnabled;
    }

    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        if (_selectionRegistrar is null || !_selectionEnabled)
        {
            return;
        }

        switch (@event)
        {
            case PointerDownEvent { Buttons: var buttons } when buttons.HasFlag(PointerButtons.Primary):
                _selectionRegistrar.StartSelection(this, @event.Position);
                break;
            case PointerMoveEvent { Down: true, Buttons: var buttons } when buttons.HasFlag(PointerButtons.Primary):
                _selectionRegistrar.UpdateSelection(@event.Position);
                break;
            case PointerUpEvent:
            case PointerCancelEvent:
                _selectionRegistrar.EndSelection();
                break;
        }
    }

    internal void SetSelection(int baseOffset, int extentOffset)
    {
        int nextBaseOffset = Math.Clamp(baseOffset, 0, _text.Length);
        int nextExtentOffset = Math.Clamp(extentOffset, 0, _text.Length);
        if (_selectionBaseOffset == nextBaseOffset && _selectionExtentOffset == nextExtentOffset)
        {
            return;
        }

        _selectionBaseOffset = nextBaseOffset;
        _selectionExtentOffset = nextExtentOffset;
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }

    internal int GetTextPosition(Point globalPosition)
    {
        if (!TryGlobalToLocal(globalPosition, out Point localPosition))
        {
            return 0;
        }

        var clamped = new Point(
            Math.Clamp(localPosition.X, 0, Math.Max(0, Size.Width)),
            Math.Clamp(localPosition.Y, 0, Math.Max(0, Size.Height)));
        if (_layout is not null)
        {
            return Math.Clamp(_layout.HitTestPoint(clamped).TextPosition, 0, _text.Length);
        }

        return EstimateTextPosition(clamped);
    }

    internal bool ContainsGlobalPosition(Point globalPosition)
    {
        return TryGlobalToLocal(globalPosition, out Point local)
               && local.X >= 0
               && local.Y >= 0
               && local.X <= Size.Width
               && local.Y <= Size.Height;
    }

    internal double DistanceToGlobalPosition(Point globalPosition)
    {
        if (!TryGlobalToLocal(globalPosition, out Point local))
        {
            return double.PositiveInfinity;
        }

        double dx = local.X < 0 ? -local.X : local.X > Size.Width ? local.X - Size.Width : 0;
        double dy = local.Y < 0 ? -local.Y : local.Y > Size.Height ? local.Y - Size.Height : 0;
        return (dx * dx) + (dy * dy);
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        _selectionRegistrar?.Register(this);
    }

    protected override void OnDetach()
    {
        _selectionRegistrar?.Unregister(this);
        base.OnDetach();
    }

    private void PaintSelection(PaintingContext context, Point offset)
    {
        if (_layout is null || _selectionBaseOffset == _selectionExtentOffset)
        {
            return;
        }

        int start = Math.Min(_selectionBaseOffset, _selectionExtentOffset);
        int length = Math.Abs(_selectionExtentOffset - _selectionBaseOffset);
        var brush = new SolidColorBrush(_selectionColor);
        foreach (Rect rect in _layout.HitTestTextRange(start, length))
        {
            context.DrawRectangle(brush, null, new Rect(rect.Position + offset, rect.Size));
        }
    }

    private void PaintCursor(PaintingContext context, Point offset)
    {
        if (!_showCursor || _layout is null || _selectionBaseOffset != _selectionExtentOffset)
        {
            return;
        }

        Rect hit = _layout.HitTestTextPosition(_selectionExtentOffset);
        double height = _cursorHeight ?? hit.Height;
        double top = hit.Top + Math.Max(0, (hit.Height - height) / 2.0);
        context.DrawRectangle(
            new SolidColorBrush(_cursorColor),
            null,
            new Rect(offset.X + hit.X, offset.Y + top, _cursorWidth, height));
    }

    private bool TryGlobalToLocal(Point globalPosition, out Point localPosition)
    {
        localPosition = globalPosition;
        if (!TryGetTransformFromRoot(out Matrix transform) || !transform.TryInvert(out Matrix inverse))
        {
            return false;
        }

        localPosition = inverse.Transform(globalPosition);
        return true;
    }

    private int EstimateTextPosition(Point localPosition)
    {
        if (_text.Length == 0)
        {
            return 0;
        }

        double characterWidth = Math.Max(1.0, (_fontSize * 0.55) + _letterSpacing);
        double lineHeight = _height is > 0 ? _fontSize * _height.Value : _fontSize * 1.2;
        string[] lines = _text.Split('\n');
        int lineIndex = Math.Clamp((int)(localPosition.Y / Math.Max(1.0, lineHeight)), 0, lines.Length - 1);
        int offset = 0;
        for (int index = 0; index < lineIndex; index++)
        {
            offset += lines[index].Length + 1;
        }

        int column = Math.Clamp((int)Math.Round(localPosition.X / characterWidth), 0, lines[lineIndex].Length);
        return Math.Clamp(offset + column, 0, _text.Length);
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
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
