using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/paragraph.dart

namespace Plumix;

public sealed partial class RenderParagraph : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, TextParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, TextParentData> _container;
    private InlineSpan _text;
    private TextScaler _textScaler = TextScaler.NoScaling;
    private TextAlign _textAlign = TextAlign.Start;
    private TextDirection _textDirection = TextDirection.Ltr;
    private bool _softWrap = true;
    private int? _maxLines;
    private TextOverflow _overflow = TextOverflow.Clip;
    private TextWidthBasis _textWidthBasis = TextWidthBasis.Parent;
    private TextHeightBehavior? _textHeightBehavior;
    private string? _locale;
    private TextLayout? _layout;
    private IReadOnlyList<PlaceholderDimensions> _placeholderDimensions = [];
    private ParagraphSource? _source;
    private List<InlineSpanSemanticsInformation>? _semanticsInfo;
    private List<InlineSpanSemanticsInformation>? _cachedCombinedSemanticsInfos;
    private Color? _selectionColor;

    public RenderParagraph(InlineSpan text, List<RenderBox>? children = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        text.DebugAssertIsValid();
        _text = text;
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, TextParentData>(this);
        if (children is not null)
        {
            AddAll(children);
        }
    }

    /// Convenience constructor building a single unstyled [TextSpan].
    public RenderParagraph(string text) : this(new TextSpan(text ?? string.Empty))
    {
    }

    /// The number of device pixels for each logical pixel.
    ///
    /// This is used by some renderers (like Flutter's `WebParagraph` on the web) to regenerate the
    /// text bitmap when the scale changes. Plumix paints text through Avalonia, which rasterises
    /// per frame at the surface scale, so changing this only records the value.
    public double DevicePixelRatio { get; set; } = 1.0;

    /// The text to display.
    public InlineSpan Text
    {
        get => _text;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            switch (_text.CompareTo(value))
            {
                case RenderComparison.Identical:
                    return;
                case RenderComparison.Metadata:
                    _text = value;
                    _cachedCombinedSemanticsInfos = null;
                    MarkNeedsSemanticsUpdate();
                    break;
                case RenderComparison.Paint:
                    _text = value;
                    _cachedCombinedSemanticsInfos = null;
                    MarkNeedsPaint();
                    MarkNeedsSemanticsUpdate();
                    break;
                default:
                    _text = value;
                    _cachedCombinedSemanticsInfos = null;
                    MarkNeedsLayout();
                    MarkNeedsSemanticsUpdate();
                    RebuildSelectableFragments();
                    break;
            }
        }
    }

    /// The flattened plain-text representation of [Text].
    public string PlainText => _text.ToPlainText(includeSemanticsLabels: false);

    /// The strategy the text and placeholders are scaled by before layout.
    public TextScaler TextScaler
    {
        get => _textScaler;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (Equals(_textScaler, value))
            {
                return;
            }

            _textScaler = value;
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
            if (value is <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Max lines must be greater than zero.");
            }

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

    /// The locale used to select region-specific glyphs.
    public string? Locale
    {
        get => _locale;
        set
        {
            if (string.Equals(_locale, value, StringComparison.Ordinal))
            {
                return;
            }

            _locale = value;
            MarkNeedsLayout();
        }
    }

    /// Whether the paragraph's text exceeded [MaxLines] during the last layout.
    public bool DidExceedMaxLines { get; private set; }

    public RenderBox? FirstChild => _container.FirstChild;

    public RenderBox? LastChild => _container.LastChild;

    public int ChildCount => _container.ChildCount;

    public void AddAll(List<RenderBox>? children) => _container.AddAll(children);

    public void RemoveAll() => _container.RemoveAll();

    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);

    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);

    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);

    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);

    public void Remove(RenderBox child) => _container.Remove(child);

    public void DefaultPaint(PaintingContext ctx, Point offset) => _container.DefaultPaint(ctx, offset);

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position)
    {
        return _container.DefaultHitTestChildren(result, position);
    }

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after)
    {
        Insert((RenderBox)child, after as RenderBox);
    }

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after)
    {
        Move((RenderBox)child, after as RenderBox);
    }

    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not TextParentData)
        {
            child.parentData = new TextParentData();
        }
    }

    private List<RenderBox> Children
    {
        get
        {
            var children = new List<RenderBox>(ChildCount);
            for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
            {
                children.Add(child);
            }

            return children;
        }
    }

    // -- Root style projection ------------------------------------------------
    //
    // Flutter's RenderParagraph exposes no font properties; every style lives on
    // the InlineSpan tree. These accessors project the root span's style so that
    // callers holding a paragraph can still read and write the resolved style.

    private TextStyle RootStyle => _text.Style ?? TextStyle.Fallback;

    private void UpdateRootStyle(TextStyle style)
    {
        Text = _text is TextSpan span
            ? new TextSpan(
                text: span.Text,
                children: span.Children,
                style: style,
                recognizer: span.Recognizer,
                mouseCursor: span.MouseCursor,
                onEnter: span.OnEnter,
                onExit: span.OnExit,
                semanticsLabel: span.SemanticsLabel,
                semanticsIdentifier: span.SemanticsIdentifier,
                locale: span.Locale,
                spellOut: span.SpellOut)
            : new TextSpan(children: [_text], style: style);
    }

    public FontFamily FontFamily
    {
        get => RootStyle.FontFamily ?? Avalonia.Media.FontFamily.Default;
        set => UpdateRootStyle(RootStyle with { FontFamily = value ?? Avalonia.Media.FontFamily.Default });
    }

    public FontStyle FontStyle
    {
        get => RootStyle.FontStyle ?? Avalonia.Media.FontStyle.Normal;
        set => UpdateRootStyle(RootStyle with { FontStyle = value });
    }

    public FontWeight FontWeight
    {
        get => RootStyle.FontWeight ?? Avalonia.Media.FontWeight.Normal;
        set => UpdateRootStyle(RootStyle with { FontWeight = value });
    }

    public double FontSize
    {
        get => RootStyle.FontSize ?? TextDefaults.DefaultFontSize;
        set => UpdateRootStyle(RootStyle with { FontSize = value });
    }

    public IBrush Foreground
    {
        get => new SolidColorBrush(RootStyle.Color ?? Colors.Black);
        set => UpdateRootStyle(RootStyle with
        {
            Color = value is ISolidColorBrush solid ? solid.Color : Colors.Black,
        });
    }

    public double? Height
    {
        get => RootStyle.Height;
        set => UpdateRootStyle(RootStyle with { Height = value });
    }

    public double LetterSpacing
    {
        get => RootStyle.LetterSpacing ?? 0;
        set => UpdateRootStyle(RootStyle with { LetterSpacing = value });
    }

    public Plumix.UI.TextDecoration? TextDecoration
    {
        get => RootStyle.Decoration;
        set => UpdateRootStyle(RootStyle with { Decoration = value });
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        return MeasureForConstraints(
            new BoxConstraints(MaxHeight: NormalizeIntrinsicExtent(height)),
            dry: true).Size.Width;
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        return MeasureForConstraints(
            new BoxConstraints(MaxHeight: NormalizeIntrinsicExtent(height)),
            dry: true).Size.Width;
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        return MeasureForConstraints(
            new BoxConstraints(MaxWidth: NormalizeIntrinsicExtent(width)),
            dry: true).Size.Height;
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        return ComputeMinIntrinsicHeight(width);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return MeasureForConstraints(constraints, dry: true).Size;
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        (TextLayout? layout, Size size, _) = MeasureForConstraints(constraints, dry: true);
        if (layout is not null)
        {
            return layout.Baseline;
        }

        double lineHeight = EstimatedLineHeight;
        return Math.Min(size.Height, lineHeight * 0.8);
    }

    protected override void PerformLayout()
    {
        (TextLayout? layout, Size size, IReadOnlyList<Rect> boxes) =
            MeasureForConstraints(Constraints, dry: false);
        _layout = layout;
        Size = size;
        RenderInlineChildrenContainerDefaults.PositionInlineChildren(Children, boxes);
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

        return Math.Min(Size.Height, EstimatedLineHeight * 0.8);
    }

    private double EstimatedLineHeight
    {
        get
        {
            TextStyle style = RootStyle;
            double fontSize = _textScaler.Scale(style.FontSize ?? TextDefaults.DefaultFontSize);
            return style.Height is > 0 ? fontSize * style.Height.Value : fontSize * 1.2;
        }
    }

    private (TextLayout? Layout, Size Size, IReadOnlyList<Rect> Boxes) MeasureForConstraints(
        BoxConstraints constraints,
        bool dry)
    {
        double maxWidth = double.IsInfinity(constraints.MaxWidth)
            ? double.PositiveInfinity
            : Math.Max(0, constraints.MaxWidth);
        double maxHeight = double.IsInfinity(constraints.MaxHeight)
            ? double.PositiveInfinity
            : Math.Max(0, constraints.MaxHeight);

        List<RenderBox> children = Children;
        _placeholderDimensions = RenderInlineChildrenContainerDefaults.LayoutInlineChildren(
            children,
            AdjustMaxWidth(maxWidth),
            dry ? ChildLayoutHelper.DryLayoutChild : ChildLayoutHelper.LayoutChild,
            dry ? ChildLayoutHelper.GetDryBaseline : ChildLayoutHelper.GetBaseline);

        var source = ParagraphSource.Build(_text, _textScaler, _placeholderDimensions, _textDirection);
        _source = source;

        try
        {
            TextLayout layout = CreateTextLayout(source, AdjustMaxWidth(maxWidth), maxHeight);
            if (ShouldTightenAlignedWidth(layout, maxWidth, constraints, source))
            {
                double tightenedWidth = Math.Max(0, Math.Min(maxWidth, layout.WidthIncludingTrailingWhitespace));
                if (tightenedWidth > 0)
                {
                    layout = CreateTextLayout(source, tightenedWidth, maxHeight);
                }
            }

            DidExceedMaxLines = _maxLines is int limit && layout.TextLines.Count >= limit
                                && (layout.Height > maxHeight || source.HasTrailingContentAfter(layout, limit));
            double layoutWidth = _textWidthBasis == TextWidthBasis.LongestLine
                ? layout.WidthIncludingTrailingWhitespace
                : layout.Width;
            Size size = constraints.Constrain(new Size(layoutWidth, layout.Height));
            return (layout, size, source.ResolvePlaceholderBoxes(layout));
        }
        catch (Exception exception) when (TextLayoutFallback.IsMissingFontManager(exception))
        {
            Size estimate = TextLayoutFallback.EstimateTextSize(
                source.PlainText,
                _textScaler.Scale(RootStyle.FontSize ?? TextDefaults.DefaultFontSize),
                maxWidth,
                RootStyle.Height,
                RootStyle.LetterSpacing ?? 0);
            return (null, constraints.Constrain(estimate), source.EstimatePlaceholderBoxes(estimate));
        }
    }

    private double AdjustMaxWidth(double maxWidth)
    {
        return _softWrap || _overflow == TextOverflow.Ellipsis ? maxWidth : double.PositiveInfinity;
    }

    private TextLayout CreateTextLayout(ParagraphSource source, double maxWidth, double maxHeight)
    {
        return new TextLayout(
            source,
            source.CreateParagraphProperties(
                ResolveTextAlignment(_textAlign, _textDirection),
                _softWrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
                _textDirection == TextDirection.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight),
            ResolveTextTrimming(_overflow),
            maxWidth,
            maxHeight,
            _maxLines ?? 0);
    }

    private bool ShouldTightenAlignedWidth(
        TextLayout layout,
        double maxWidth,
        BoxConstraints constraints,
        ParagraphSource source)
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

        if (source.PlainText.Length == 0)
        {
            return false;
        }

        Rect firstGlyph = layout.HitTestTextPosition(0);
        return firstGlyph.X > 0.01;
    }

    private static double NormalizeIntrinsicExtent(double value)
    {
        return double.IsNaN(value) || value < 0.0 ? 0.0 : value;
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_layout is null)
        {
            return;
        }

        PaintSelectionHighlights(ctx, offset);
        if (_overflow == TextOverflow.Fade && _layout.WidthIncludingTrailingWhitespace > Size.Width + 0.01)
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

        RenderInlineChildrenContainerDefaults.PaintInlineChildren(Children, ctx, offset);
        PaintSelectionHandles(ctx, offset);
    }

    protected override bool HitTestSelf(Point position) => true;

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        InlineSpan? spanHit = SpanForPosition(position);
        if (spanHit is IHitTestTarget target)
        {
            result.Add(new HitTestEntry(target));
            return true;
        }

        return RenderInlineChildrenContainerDefaults.HitTestInlineChildren(Children, result, position);
    }

    /// Returns the span the given local `position` lands on, or null when the
    /// position falls outside of every glyph.
    private InlineSpan? SpanForPosition(Point position)
    {
        if (_layout is null || _source is null || _source.PlainText.Length == 0)
        {
            return null;
        }

        if (position.X < 0 || position.Y < 0 || position.X > Size.Width || position.Y > Size.Height)
        {
            return null;
        }

        TextHitTestResult hit = _layout.HitTestPoint(position);
        int offset = Math.Clamp(hit.TextPosition, 0, Math.Max(0, _source.PlainText.Length - 1));
        Rect glyph = _layout.HitTestTextPosition(offset);
        if (!glyph.Contains(position) && !hit.IsInside)
        {
            return null;
        }

        return _text.GetSpanForPosition(new TextPosition(offset));
    }

    private int EstimateTextPosition(Point localPosition, string plain)
    {
        if (plain.Length == 0)
        {
            return 0;
        }

        TextStyle style = RootStyle;
        double fontSize = _textScaler.Scale(style.FontSize ?? TextDefaults.DefaultFontSize);
        double characterWidth = Math.Max(1.0, (fontSize * 0.55) + (style.LetterSpacing ?? 0));
        double lineHeight = EstimatedLineHeight;
        string[] lines = plain.Split('\n');
        int lineIndex = Math.Clamp((int)(localPosition.Y / Math.Max(1.0, lineHeight)), 0, lines.Length - 1);
        int offset = 0;
        for (int index = 0; index < lineIndex; index++)
        {
            offset += lines[index].Length + 1;
        }

        int column = Math.Clamp((int)Math.Round(localPosition.X / characterWidth), 0, lines[lineIndex].Length);
        return Math.Clamp(offset + column, 0, plain.Length);
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        _semanticsInfo = _text.GetSemanticsInformation();
        bool needsAssembleSemanticsNode = false;
        foreach (InlineSpanSemanticsInformation info in _semanticsInfo)
        {
            if (info.Recognizer is not null || info.SemanticsIdentifier is not null)
            {
                needsAssembleSemanticsNode = true;
                break;
            }
        }

        if (needsAssembleSemanticsNode)
        {
            configuration.ExplicitChildNodes = true;
            configuration.IsSemanticBoundary = true;
            return;
        }

        var buffer = new System.Text.StringBuilder();
        foreach (InlineSpanSemanticsInformation info in _semanticsInfo)
        {
            buffer.Append(info.SemanticsLabel ?? info.Text);
        }

        configuration.Label = buffer.ToString();
    }

    protected override void AssembleSemanticsNode(
        SemanticsNode node,
        SemanticsConfiguration config,
        IReadOnlyList<SemanticsNode> children)
    {
        _semanticsInfo ??= _text.GetSemanticsInformation();
        _cachedCombinedSemanticsInfos ??= InlineSpan.CombineSemanticsInfo(_semanticsInfo);

        var newChildren = new List<SemanticsNode>();
        double ordinal = 0.0;
        int start = 0;
        int childIndex = 0;
        RenderBox? child = FirstChild;
        foreach (InlineSpanSemanticsInformation info in _cachedCombinedSemanticsInfos)
        {
            int selectionStart = start;
            int selectionLength = info.Text.Length;
            start += selectionLength;

            if (info.IsPlaceholder)
            {
                if (childIndex < children.Count)
                {
                    bool laidOut = child?.parentData is TextParentData { InlineOffset: not null };
                    if (laidOut)
                    {
                        newChildren.Add(children[childIndex]);
                    }

                    childIndex += 1;
                }

                if (child is not null)
                {
                    child = ChildAfter(child);
                }

                continue;
            }

            if (selectionLength == 0)
            {
                continue;
            }

            Rect? bounds = BoundsForRange(selectionStart, selectionLength);
            if (bounds is null)
            {
                continue;
            }

            SemanticsNode childNode = Owner!.SemanticsOwner!.CreateDetachedNode(this);
            var configuration = new SemanticsConfiguration
            {
                SortKey = new OrdinalSortKey(ordinal),
                Label = info.SemanticsLabel ?? info.Text,
            };
            ordinal += 1;
            ApplyRecognizerSemantics(configuration, info.Recognizer);
            childNode.UpdateWith(configuration, []);
            // Flutter positions the per-run children by rect alone, in the paragraph's own coordinates.
            childNode.Rect = bounds.Value;
            newChildren.Add(childNode);
        }

        node.UpdateWith(config, newChildren);
    }

    private static void ApplyRecognizerSemantics(SemanticsConfiguration configuration, GestureRecognizer? recognizer)
    {
        switch (recognizer)
        {
            case null:
                return;
            case TapGestureRecognizer { OnTap: { } onTap }:
                configuration.AddActionHandler(SemanticsActions.Tap, () => onTap());
                configuration.Flags |= SemanticsFlags.IsLink;
                return;
            case TapGestureRecognizer:
                return;
            case LongPressGestureRecognizer { OnLongPress: { } onLongPress }:
                configuration.AddActionHandler(SemanticsActions.LongPress, () => onLongPress());
                return;
            case LongPressGestureRecognizer:
                return;
            default:
                throw new InvalidOperationException($"{recognizer.GetType().Name} is not supported.");
        }
    }

    private Rect? BoundsForRange(int start, int length)
    {
        if (_layout is null)
        {
            return null;
        }

        IReadOnlyList<Rect> rects = _layout.HitTestTextRange(start, length).ToList();
        if (rects.Count == 0)
        {
            return null;
        }

        Rect rect = rects[0];
        for (int index = 1; index < rects.Count; index += 1)
        {
            rect = rect.Union(rects[index]);
        }

        return new Rect(
            Math.Floor(rect.Left) - 4.0,
            Math.Floor(rect.Top) - 4.0,
            Math.Ceiling(rect.Width) + 8.0,
            Math.Ceiling(rect.Height) + 8.0);
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

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren()
    {
        return [Text.ToDiagnosticsNode(name: "text", style: DiagnosticsTreeStyle.Transition)];
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<TextAlign>("textAlign", TextAlign));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection));
        properties.Add(new FlagProperty(
            "softWrap",
            SoftWrap,
            ifTrue: "wrapping at box width",
            ifFalse: "no wrapping except at line break characters",
            showName: true));
        properties.Add(new EnumProperty<TextOverflow>("overflow", Overflow));
        properties.Add(new DiagnosticsProperty<TextScaler>(
            "textScaler",
            TextScaler,
            defaultValue: TextScaler.NoScaling));
        properties.Add(new StringProperty("locale", Locale, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new IntProperty("maxLines", MaxLines, ifNull: "unlimited"));
        properties.Add(new DoubleProperty("devicePixelRatio", DevicePixelRatio, defaultValue: 1.0));
    }
}
