using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Plumix.Painting;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/editable.dart

namespace Plumix.Rendering;

/// <summary>Framework-owned editable text layout, paint, caret, and selection geometry.</summary>
public sealed class RenderEditable : RenderBox
{
    private readonly RenderLeaderLayer _startHandleLeader;
    private readonly RenderLeaderLayer _endHandleLeader;
    private readonly RenderLeaderLayer _toolbarLeader;
    private TextLayout? _layout;
    private string _text = string.Empty;
    private TextSelection _selection;
    private TextRange? _composing;
    private FontFamily _fontFamily = Avalonia.Media.FontFamily.Default;
    private FontStyle _fontStyle = FontStyle.Normal;
    private FontWeight _fontWeight = FontWeight.Normal;
    private double _fontSize = 14.0;
    private IBrush _foreground = Brushes.Black;
    private TextAlign _textAlign = TextAlign.Start;
    private TextDirection _textDirection = TextDirection.Ltr;
    private bool _multiline;
    private bool _forceLine = true;
    private int? _minLines;
    private int? _maxLines = 1;
    private bool _expands;
    private double? _height;
    private double _letterSpacing;
    private Color _selectionColor = Color.FromArgb(0x66, 0x67, 0x50, 0xA4);
    private BoxHeightStyle _selectionHeightStyle = BoxHeightStyle.Tight;
    private BoxWidthStyle _selectionWidthStyle = BoxWidthStyle.Tight;
    private Color _cursorColor = Colors.Black;
    private bool _showCursor;
    private double _cursorWidth = 1.0;
    private double? _cursorHeight;
    private Radius _cursorRadius = Radius.Zero;
    private double _cursorOpacity = 1.0;
    private Point _cursorOffset;
    private bool _paintCursorAboveText;
    private IReadOnlyList<SuggestionSpan> _suggestionSpans = [];
    private Color _misspelledColor = Colors.Red;

    public RenderEditable(
        LayerLink startHandleLayerLink,
        LayerLink endHandleLayerLink,
        LayerLink? toolbarLayerLink = null)
    {
        _startHandleLeader = new RenderLeaderLayer(startHandleLayerLink);
        _endHandleLeader = new RenderLeaderLayer(endHandleLayerLink);
        _toolbarLeader = new RenderLeaderLayer(toolbarLayerLink ?? new LayerLink());
        AdoptChild(_startHandleLeader);
        AdoptChild(_endHandleLeader);
        AdoptChild(_toolbarLeader);
    }

    public ValueNotifier<bool> SelectionStartInViewport { get; } = new(true);
    public ValueNotifier<bool> SelectionEndInViewport { get; } = new(true);
    public Point? LastSecondaryTapDownPosition { get; set; }
    public string PlainText => _text;
    public double PreferredLineHeight => Math.Max(1.0, _fontSize * (_height is > 0.0 ? _height.Value : 1.2));

    public string Text
    {
        get => _text;
        set => SetLayoutValue(ref _text, value ?? string.Empty);
    }

    public TextSelection Selection
    {
        get => _selection;
        set
        {
            TextSelection next = value.Clamp(_text.Length);
            if (_selection == next) return;
            _selection = next;
            MarkNeedsPaint();
        }
    }

    public TextRange? Composing
    {
        get => _composing;
        set
        {
            TextRange? next = value?.Clamp(_text.Length);
            if (Nullable.Equals(_composing, next)) return;
            _composing = next;
            MarkNeedsPaint();
        }
    }

    public FontFamily FontFamily { get => _fontFamily; set => SetLayoutValue(ref _fontFamily, value); }
    public FontStyle FontStyle { get => _fontStyle; set => SetLayoutValue(ref _fontStyle, value); }
    public FontWeight FontWeight { get => _fontWeight; set => SetLayoutValue(ref _fontWeight, value); }
    public double FontSize { get => _fontSize; set => SetLayoutValue(ref _fontSize, value); }
    public IBrush Foreground { get => _foreground; set => SetPaintValue(ref _foreground, value); }
    public TextAlign TextAlign { get => _textAlign; set => SetLayoutValue(ref _textAlign, value); }
    public TextDirection TextDirection { get => _textDirection; set => SetLayoutValue(ref _textDirection, value); }
    public bool Multiline { get => _multiline; set => SetLayoutValue(ref _multiline, value); }
    public bool ForceLine { get => _forceLine; set => SetLayoutValue(ref _forceLine, value); }
    public int? MinLines { get => _minLines; set => SetLayoutValue(ref _minLines, value); }
    public int? MaxLines { get => _maxLines; set => SetLayoutValue(ref _maxLines, value); }
    public bool Expands { get => _expands; set => SetLayoutValue(ref _expands, value); }
    public double? Height { get => _height; set => SetLayoutValue(ref _height, value); }
    public double LetterSpacing { get => _letterSpacing; set => SetLayoutValue(ref _letterSpacing, value); }
    public Color SelectionColor { get => _selectionColor; set => SetPaintValue(ref _selectionColor, value); }
    public BoxHeightStyle SelectionHeightStyle
    {
        get => _selectionHeightStyle;
        set => SetPaintValue(ref _selectionHeightStyle, value);
    }
    public BoxWidthStyle SelectionWidthStyle
    {
        get => _selectionWidthStyle;
        set => SetPaintValue(ref _selectionWidthStyle, value);
    }
    public Color CursorColor { get => _cursorColor; set => SetPaintValue(ref _cursorColor, value); }
    public bool ShowCursor { get => _showCursor; set => SetPaintValue(ref _showCursor, value); }
    public double CursorWidth { get => _cursorWidth; set => SetPaintValue(ref _cursorWidth, value); }
    public double? CursorHeight { get => _cursorHeight; set => SetPaintValue(ref _cursorHeight, value); }
    public Radius CursorRadius { get => _cursorRadius; set => SetPaintValue(ref _cursorRadius, value); }
    public double CursorOpacity { get => _cursorOpacity; set => SetPaintValue(ref _cursorOpacity, value); }
    public Point CursorOffset { get => _cursorOffset; set => SetPaintValue(ref _cursorOffset, value); }
    public bool PaintCursorAboveText
    {
        get => _paintCursorAboveText;
        set => SetPaintValue(ref _paintCursorAboveText, value);
    }
    public IReadOnlyList<SuggestionSpan> SuggestionSpans
    {
        get => _suggestionSpans;
        set
        {
            _suggestionSpans = value ?? [];
            MarkNeedsPaint();
        }
    }
    public Color MisspelledColor { get => _misspelledColor; set => SetPaintValue(ref _misspelledColor, value); }

    public LayerLink StartHandleLayerLink
    {
        get => _startHandleLeader.Link;
        set => _startHandleLeader.Link = value;
    }

    public LayerLink EndHandleLayerLink
    {
        get => _endHandleLeader.Link;
        set => _endHandleLeader.Link = value;
    }

    public LayerLink ToolbarLayerLink
    {
        get => _toolbarLeader.Link;
        set => _toolbarLeader.Link = value;
    }

    protected override bool AlwaysNeedsCompositing => true;

    protected override void PerformLayout()
    {
        double caretMargin = _cursorWidth + 1.0;
        double availableWidth = double.IsFinite(Constraints.MaxWidth)
            ? Math.Max(0.0, Constraints.MaxWidth - caretMargin)
            : double.PositiveInfinity;
        double layoutWidth = _multiline ? availableWidth : double.PositiveInfinity;
        try
        {
            _layout = new TextLayout(
                text: _text,
                typeface: new Typeface(_fontFamily, _fontStyle, _fontWeight),
                fontSize: _fontSize,
                foreground: _foreground,
                textAlignment: ResolveTextAlignment(),
                textWrapping: _multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
                flowDirection: _textDirection == TextDirection.Rtl
                    ? FlowDirection.RightToLeft
                    : FlowDirection.LeftToRight,
                maxWidth: layoutWidth,
                lineHeight: PreferredLineHeight,
                letterSpacing: _letterSpacing,
                maxLines: _maxLines ?? 0);
            double width = _forceLine && double.IsFinite(Constraints.MaxWidth)
                ? Constraints.MaxWidth
                : _layout.WidthIncludingTrailingWhitespace + caretMargin;
            double minHeight = PreferredLineHeight * (_minLines ?? 0);
            double maxHeight = _maxLines.HasValue
                ? PreferredLineHeight * _maxLines.Value
                : double.PositiveInfinity;
            double height = _expands && double.IsFinite(Constraints.MaxHeight)
                ? Constraints.MaxHeight
                : Math.Clamp(Math.Max(_layout.Height, minHeight), 0.0, maxHeight);
            Size = Constraints.Constrain(new Size(width, height));
        }
        catch (Exception exception) when (TextLayoutFallback.IsMissingFontManager(exception))
        {
            _layout = null;
            Size estimated = TextLayoutFallback.EstimateTextSize(
                _text,
                _fontSize,
                availableWidth,
                _height,
                _letterSpacing);
            double estimatedHeight = _expands && double.IsFinite(Constraints.MaxHeight)
                ? Constraints.MaxHeight
                : Math.Max(estimated.Height, PreferredLineHeight * (_minLines ?? 0));
            Size = Constraints.Constrain(new Size(
                _forceLine && double.IsFinite(Constraints.MaxWidth) ? Constraints.MaxWidth : estimated.Width,
                estimatedHeight));
        }

        _startHandleLeader.Layout(BoxConstraints.Tight(new Size()), parentUsesSize: true);
        _endHandleLeader.Layout(BoxConstraints.Tight(new Size()), parentUsesSize: true);
        _toolbarLeader.Layout(BoxConstraints.Tight(new Size()), parentUsesSize: true);
    }

    public TextPosition GetPositionForPoint(Point globalPosition)
    {
        if (!TryGlobalToLocal(globalPosition, out Point local)) return new TextPosition(0);
        return GetPositionForOffset(local);
    }

    public TextPosition GetPositionForOffset(Point localPosition)
    {
        Point clamped = new(
            Math.Clamp(localPosition.X, 0.0, Math.Max(0.0, Size.Width)),
            Math.Clamp(localPosition.Y, 0.0, Math.Max(0.0, Size.Height)));
        int offset = _layout is null
            ? EstimateTextPosition(clamped)
            : Math.Clamp(_layout.HitTestPoint(clamped).TextPosition, 0, _text.Length);
        return new TextPosition(offset);
    }

    public TextSelection GetLineAtOffset(TextPosition position)
    {
        int offset = Math.Clamp(position.Offset, 0, _text.Length);
        if (!_multiline) return new TextSelection(0, _text.Length);
        int start = offset == 0 ? 0 : _text.LastIndexOf('\n', Math.Max(0, offset - 1)) + 1;
        int end = _text.IndexOf('\n', offset);
        return new TextSelection(start, end < 0 ? _text.Length : end);
    }

    public Rect GetLocalRectForCaret(TextPosition position)
    {
        int offset = Math.Clamp(position.Offset, 0, _text.Length);
        Rect hit = _layout?.HitTestTextPosition(offset) ?? new Rect(
            EstimateCaretX(offset),
            0.0,
            _cursorWidth,
            PreferredLineHeight);
        double height = _cursorHeight ?? Math.Max(PreferredLineHeight, hit.Height);
        double top = hit.Y + Math.Max(0.0, (hit.Height - height) / 2.0);
        Rect caret = new(
            Math.Clamp(hit.X, 0.0, Math.Max(0.0, Size.Width - _cursorWidth)),
            top,
            _cursorWidth,
            height);
        return new Rect(caret.Position + _cursorOffset, caret.Size);
    }

    public Rect? GetRectForComposingRange(TextRange range)
    {
        TextRange value = range.Clamp(_text.Length);
        if (value.IsCollapsed || _layout is null) return null;
        IReadOnlyList<Rect> boxes = _layout.HitTestTextRange(value.Start, value.End - value.Start).ToList();
        return boxes.Count == 0 ? null : boxes.Aggregate((left, right) => left.Union(right));
    }

    public IReadOnlyList<TextSelectionPoint> GetEndpointsForSelection(TextSelection selection)
    {
        TextSelection value = selection.Clamp(_text.Length);
        if (value.IsCollapsed)
        {
            Rect caret = GetLocalRectForCaret(new TextPosition(value.ExtentOffset));
            return [new TextSelectionPoint(new Point(caret.X, caret.Y + PreferredLineHeight), null)];
        }

        if (_layout is null)
        {
            Rect startCaret = GetLocalRectForCaret(new TextPosition(value.Start));
            Rect endCaret = GetLocalRectForCaret(new TextPosition(value.End));
            return
            [
                new TextSelectionPoint(new Point(startCaret.X, startCaret.Bottom), _textDirection),
                new TextSelectionPoint(new Point(endCaret.X, endCaret.Bottom), _textDirection),
            ];
        }

        IReadOnlyList<Rect> boxes = _layout.HitTestTextRange(value.Start, value.End - value.Start).ToList();
        if (boxes.Count == 0)
        {
            Rect caret = GetLocalRectForCaret(new TextPosition(value.ExtentOffset));
            return [new TextSelectionPoint(new Point(caret.X, caret.Bottom), null)];
        }

        Rect first = boxes[0];
        Rect last = boxes[^1];
        return
        [
            new TextSelectionPoint(new Point(Math.Clamp(first.Left, 0.0, Size.Width), first.Bottom), _textDirection),
            new TextSelectionPoint(new Point(Math.Clamp(last.Right, 0.0, Size.Width), last.Bottom), _textDirection),
        ];
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        UpdateViewportVisibility();
        if (_layout is not null)
        {
            PaintSelection(context, offset);
            PaintMisspellings(context, offset);
            if (!_paintCursorAboveText)
            {
                PaintCursor(context, offset);
            }
            context.Canvas.DrawTextLayout(_layout, offset);
            if (_paintCursorAboveText)
            {
                PaintCursor(context, offset);
            }
        }

        IReadOnlyList<TextSelectionPoint> endpoints = GetEndpointsForSelection(_selection);
        Point start = endpoints[0].Point + offset;
        Point end = (endpoints.Count == 1 ? endpoints[0] : endpoints[^1]).Point + offset;
        context.PaintChild(_startHandleLeader, start);
        context.PaintChild(_endHandleLeader, end);
        context.PaintChild(_toolbarLeader, start);
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        visitor(_startHandleLeader);
        visitor(_endHandleLeader);
        visitor(_toolbarLeader);
    }

    protected override bool HitTestSelf(Point position) => true;

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        configuration.IsSemanticBoundary = true;
        configuration.Label = _text;
    }

    private void UpdateViewportVisibility()
    {
        IReadOnlyList<TextSelectionPoint> endpoints = GetEndpointsForSelection(_selection);
        SelectionStartInViewport.Value = ContainsInflated(endpoints[0].Point);
        SelectionEndInViewport.Value = ContainsInflated(endpoints[^1].Point);
    }

    private bool ContainsInflated(Point point)
    {
        return point.X >= -0.5 && point.Y >= -0.5 && point.X <= Size.Width + 0.5 && point.Y <= Size.Height + 0.5;
    }

    private void PaintSelection(PaintingContext context, Point offset)
    {
        if (_layout is null || _selection.IsCollapsed) return;
        var brush = new SolidColorBrush(_selectionColor);
        foreach (Rect rect in _layout.HitTestTextRange(_selection.Start, _selection.End - _selection.Start))
        {
            context.Canvas.DrawRectangle(brush, null, new Rect(rect.Position + offset, rect.Size));
        }
    }

    private void PaintCursor(PaintingContext context, Point offset)
    {
        if (!_showCursor || !_selection.IsCollapsed || _cursorOpacity <= 0.0) return;
        Rect caret = GetLocalRectForCaret(new TextPosition(_selection.ExtentOffset));
        Color color = Color.FromArgb(
            (byte)Math.Round(_cursorColor.A * Math.Clamp(_cursorOpacity, 0.0, 1.0)),
            _cursorColor.R,
            _cursorColor.G,
            _cursorColor.B);
        context.Canvas.DrawRectangle(
            new SolidColorBrush(color),
            null,
            new Rect(caret.Position + offset, caret.Size),
            _cursorRadius.X,
            _cursorRadius.Y);
    }

    private void PaintMisspellings(PaintingContext context, Point offset)
    {
        if (_layout is null || _suggestionSpans.Count == 0) return;
        var pen = new Pen(new SolidColorBrush(_misspelledColor), 1.0);
        foreach (SuggestionSpan span in _suggestionSpans)
        {
            TextRange range = span.Range.Clamp(_text.Length);
            foreach (Rect rect in _layout.HitTestTextRange(range.Start, Math.Max(0, range.End - range.Start)))
            {
                double y = offset.Y + rect.Bottom;
                double x = offset.X + rect.Left;
                while (x < offset.X + rect.Right)
                {
                    context.Canvas.DrawLine(
                        pen,
                        new Point(x, y),
                        new Point(Math.Min(x + 2.0, offset.X + rect.Right), y - 1.5));
                    x += 4.0;
                }
            }
        }
    }

    private bool TryGlobalToLocal(Point globalPosition, out Point localPosition)
    {
        localPosition = globalPosition;
        if (!TryGetTransformFromRoot(out Matrix4 transform))
        {
            return false;
        }

        Matrix4? inverse = Matrix4.TryInvert(transform);
        if (inverse is null)
        {
            return false;
        }

        localPosition = MatrixUtils.TransformPoint(inverse, globalPosition);
        return true;
    }

    private int EstimateTextPosition(Point localPosition)
    {
        double width = Math.Max(1.0, (_fontSize * 0.55) + _letterSpacing);
        return Math.Clamp((int)Math.Round(localPosition.X / width), 0, _text.Length);
    }

    private double EstimateCaretX(int offset) => Math.Clamp(offset, 0, _text.Length) * Math.Max(1.0, _fontSize * 0.55);

    private TextAlignment ResolveTextAlignment()
    {
        return _textAlign switch
        {
            TextAlign.Left => TextAlignment.Left,
            TextAlign.Right => TextAlignment.Right,
            TextAlign.Center => TextAlignment.Center,
            TextAlign.Justify => TextAlignment.Justify,
            TextAlign.End => _textDirection == TextDirection.Rtl ? TextAlignment.Left : TextAlignment.Right,
            _ => _textDirection == TextDirection.Rtl ? TextAlignment.Right : TextAlignment.Left,
        };
    }

    private void SetLayoutValue<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        MarkNeedsLayout();
    }

    private void SetPaintValue<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        MarkNeedsPaint();
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new ColorProperty("cursorColor", CursorColor));
        properties.Add(new DiagnosticsProperty<bool>("showCursor", ShowCursor));
        properties.Add(new IntProperty("maxLines", MaxLines));
        properties.Add(new IntProperty("minLines", MinLines));
        properties.Add(new DiagnosticsProperty<bool>("expands", Expands, defaultValue: false));
        properties.Add(new ColorProperty("selectionColor", SelectionColor));
        properties.Add(new DiagnosticsProperty<TextSelection>("selection", Selection));
    }
}
