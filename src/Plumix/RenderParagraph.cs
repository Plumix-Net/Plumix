using Avalonia;
using Avalonia.Media;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/paragraph.dart

namespace Plumix;

/// A render object that displays a paragraph of text.
public sealed partial class RenderParagraph : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, TextParentData>,
    IRenderObjectContainer
{
    private const string EllipsisText = "\u2026";

    private readonly RenderBoxContainerDefaultsMixin<RenderBox, TextParentData> _container;
    private readonly TextPainter _textPainter;
    private TextPainter? _textIntrinsicsCache;
    private bool _softWrap = true;
    private TextOverflow _overflow = TextOverflow.Clip;
    private double _devicePixelRatio = 1.0;
    private bool _needsClipping;
    private OverflowShader? _overflowShader;
    private List<PlaceholderDimensions>? _placeholderDimensions;
    private List<InlineSpanSemanticsInformation>? _semanticsInfo;
    private List<InlineSpanSemanticsInformation>? _cachedCombinedSemanticsInfos;
    private Color? _selectionColor;

    /// Creates a paragraph render object.
    ///
    /// The [MaxLines] property may be null (and indeed defaults to null), but if it is not null, it
    /// must be greater than zero.
    public RenderParagraph(InlineSpan text, List<RenderBox>? children = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        text.DebugAssertIsValid();
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, TextParentData>(this);
        _textPainter = new TextPainter(
            text: text,
            textAlign: TextAlign.Start,
            textDirection: TextDirection.Ltr,
            textScaler: TextScaler.NoScaling,
            textWidthBasis: TextWidthBasis.Parent);
        if (children is not null)
        {
            AddAll(children);
        }
    }

    /// Convenience constructor building a single unstyled [TextSpan].
    public RenderParagraph(string text) : this(new TextSpan(text ?? string.Empty))
    {
    }

    /// The text painter this paragraph lays out and paints with.
    internal TextPainter TextPainter => _textPainter;

    // Intrinsics cannot be calculated without a full layout for alignments other than
    // TextAlign.start, so this second painter keeps intrinsic queries away from `_textPainter`.
    private TextPainter TextIntrinsics
    {
        get
        {
            TextPainter painter = _textIntrinsicsCache ??= new TextPainter();
            painter.Text = _textPainter.Text;
            painter.TextAlign = _textPainter.TextAlign;
            painter.TextDirection = _textPainter.TextDirection;
            painter.TextScaler = _textPainter.TextScaler;
            painter.MaxLines = _textPainter.MaxLines;
            painter.Ellipsis = _textPainter.Ellipsis;
            painter.Locale = _textPainter.Locale;
            painter.StrutStyle = _textPainter.StrutStyle;
            painter.TextWidthBasis = _textPainter.TextWidthBasis;
            painter.TextHeightBehavior = _textPainter.TextHeightBehavior;
            return painter;
        }
    }

    /// The number of device pixels for each logical pixel.
    ///
    /// This is used by some renderers (like Flutter's `WebParagraph` on the web) to regenerate the
    /// text bitmap when the scale changes. Plumix paints text through Avalonia, which rasterises per
    /// frame at the surface scale, so changing this only records the value.
    public double DevicePixelRatio
    {
        get => _devicePixelRatio;
        set
        {
            if (_devicePixelRatio == value)
            {
                return;
            }

            _devicePixelRatio = value;
        }
    }

    /// The text to display.
    public InlineSpan Text
    {
        get => _textPainter.Text!;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            switch (_textPainter.Text!.CompareTo(value))
            {
                case RenderComparison.Identical:
                    return;
                case RenderComparison.Metadata:
                    _textPainter.Text = value;
                    _cachedCombinedSemanticsInfos = null;
                    MarkNeedsSemanticsUpdate();
                    break;
                case RenderComparison.Paint:
                    _textPainter.Text = value;
                    _cachedCombinedSemanticsInfos = null;
                    MarkNeedsPaint();
                    MarkNeedsSemanticsUpdate();
                    break;
                case RenderComparison.Layout:
                    _textPainter.Text = value;
                    _overflowShader = null;
                    _cachedCombinedSemanticsInfos = null;
                    MarkNeedsLayout();
                    RemoveSelectionRegistrarSubscription();
                    DisposeSelectableFragments();
                    UpdateSelectionRegistrarSubscription();
                    break;
            }
        }
    }

    /// The flattened plain-text representation of [Text].
    public string PlainText => _textPainter.PlainText;

    /// How the text should be aligned horizontally.
    public TextAlign TextAlign
    {
        get => _textPainter.TextAlign;
        set
        {
            if (_textPainter.TextAlign == value)
            {
                return;
            }

            _textPainter.TextAlign = value;
            MarkNeedsPaint();
        }
    }

    /// The directionality of the text.
    ///
    /// This decides how the [TextAlign.Start], [TextAlign.End], and [TextAlign.Justify] values of
    /// [TextAlign] are interpreted.
    ///
    /// This is also used to disambiguate how to render bidirectional text. For example, if the
    /// [Text] is an English phrase followed by a Hebrew phrase, in a [TextDirection.Ltr] context the
    /// English phrase will be on the left and the Hebrew phrase to its right, while in a
    /// [TextDirection.Rtl] context, the English phrase will be on the right and the Hebrew phrase on
    /// its left.
    public TextDirection TextDirection
    {
        get => _textPainter.TextDirection!.Value;
        set
        {
            if (_textPainter.TextDirection == value)
            {
                return;
            }

            _textPainter.TextDirection = value;
            MarkNeedsLayout();
        }
    }

    /// Whether the text should break at soft line breaks.
    ///
    /// If false, the glyphs in the text will be positioned as if there was unlimited horizontal
    /// space.
    ///
    /// If [SoftWrap] is false, [Overflow] and [TextAlign] may have unexpected effects.
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

    /// How visual overflow should be handled.
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
            _textPainter.Ellipsis = value == TextOverflow.Ellipsis ? EllipsisText : null;
            MarkNeedsLayout();
        }
    }

    /// The font scaling strategy to use when laying out and rendering the text.
    public TextScaler TextScaler
    {
        get => _textPainter.TextScaler;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (Equals(_textPainter.TextScaler, value))
            {
                return;
            }

            _textPainter.TextScaler = value;
            _overflowShader = null;
            MarkNeedsLayout();
        }
    }

    /// An optional maximum number of lines for the text to span, wrapping if necessary. If the text
    /// exceeds the given number of lines, it will be truncated according to [Overflow] and
    /// [SoftWrap].
    public int? MaxLines
    {
        get => _textPainter.MaxLines;
        set
        {
            if (value is <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Max lines must be greater than zero.");
            }

            if (_textPainter.MaxLines == value)
            {
                return;
            }

            _textPainter.MaxLines = value;
            _overflowShader = null;
            MarkNeedsLayout();
        }
    }

    /// Used by this paragraph's internal [TextPainter] to select a locale-specific font.
    public string? Locale
    {
        get => _textPainter.Locale;
        set
        {
            if (_textPainter.Locale == value)
            {
                return;
            }

            _textPainter.Locale = value;
            _overflowShader = null;
            MarkNeedsLayout();
        }
    }

    /// The strut style to use. Strut style defines the strut, which sets minimum vertical layout
    /// metrics.
    public StrutStyle? StrutStyle
    {
        get => _textPainter.StrutStyle;
        set
        {
            if (_textPainter.StrutStyle == value)
            {
                return;
            }

            _textPainter.StrutStyle = value;
            _overflowShader = null;
            MarkNeedsLayout();
        }
    }

    /// Defines how to measure the width of the rendered text.
    public TextWidthBasis TextWidthBasis
    {
        get => _textPainter.TextWidthBasis;
        set
        {
            if (_textPainter.TextWidthBasis == value)
            {
                return;
            }

            _textPainter.TextWidthBasis = value;
            _overflowShader = null;
            MarkNeedsLayout();
        }
    }

    /// Defines how to apply [TextStyle.Height] over and under text.
    public TextHeightBehavior? TextHeightBehavior
    {
        get => _textPainter.TextHeightBehavior;
        set
        {
            if (_textPainter.TextHeightBehavior == value)
            {
                return;
            }

            _textPainter.TextHeightBehavior = value;
            _overflowShader = null;
            MarkNeedsLayout();
        }
    }

    /// Whether this paragraph currently has an overflow fade shader. Dart's `debugHasOverflowShader`.
    internal bool DebugHasOverflowShader => _overflowShader is not null;

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

    public override void VisitChildren(Action<RenderObject> visitor) => _container.VisitChildren(visitor);

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

    private TextStyle RootStyle => Text.Style ?? TextStyle.Fallback;

    private void UpdateRootStyle(TextStyle style)
    {
        InlineSpan text = Text;
        Text = text is TextSpan span
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
            : new TextSpan(children: [text], style: style);
    }

    public FontFamily FontFamily
    {
        get => RootStyle.FontFamily ?? Avalonia.Media.FontFamily.Default;
        set => UpdateRootStyle(RootStyle.CopyWith(fontFamily: value ?? Avalonia.Media.FontFamily.Default));
    }

    public FontStyle FontStyle
    {
        get => RootStyle.FontStyle ?? Avalonia.Media.FontStyle.Normal;
        set => UpdateRootStyle(RootStyle.CopyWith(fontStyle: value));
    }

    public FontWeight FontWeight
    {
        get => RootStyle.FontWeight ?? Avalonia.Media.FontWeight.Normal;
        set => UpdateRootStyle(RootStyle.CopyWith(fontWeight: value));
    }

    public double FontSize
    {
        get => RootStyle.FontSize ?? TextDefaults.DefaultFontSize;
        set => UpdateRootStyle(RootStyle.CopyWith(fontSize: value));
    }

    public IBrush Foreground
    {
        get => new SolidColorBrush(RootStyle.Color ?? Colors.Black);
        set => UpdateRootStyle(RootStyle.CopyWith(
            color: value is ISolidColorBrush solid ? solid.Color : Colors.Black));
    }

    public double? Height
    {
        get => RootStyle.Height;
        set => UpdateRootStyle(RootStyle.CopyWith(height: value));
    }

    public double LetterSpacing
    {
        get => RootStyle.LetterSpacing ?? 0;
        set => UpdateRootStyle(RootStyle.CopyWith(letterSpacing: value));
    }

    public Plumix.UI.TextDecoration? TextDecoration
    {
        get => RootStyle.Decoration;
        set => UpdateRootStyle(RootStyle.CopyWith(decoration: value));
    }

    // -- Layout --------------------------------------------------------------------

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        List<PlaceholderDimensions> placeholderDimensions = RenderInlineChildrenContainerDefaults.LayoutInlineChildren(
            Children,
            double.PositiveInfinity,
            (child, _) => new Size(child.GetMinIntrinsicWidth(double.PositiveInfinity), 0.0),
            ChildLayoutHelper.GetDryBaseline);
        TextPainter intrinsics = TextIntrinsics;
        intrinsics.SetPlaceholderDimensions(placeholderDimensions);
        intrinsics.Layout();
        return intrinsics.MinIntrinsicWidth;
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        List<PlaceholderDimensions> placeholderDimensions = RenderInlineChildrenContainerDefaults.LayoutInlineChildren(
            Children,
            double.PositiveInfinity,
            // Height and baseline is irrelevant as all text will be laid out in a single line.
            (child, _) => new Size(child.GetMaxIntrinsicWidth(double.PositiveInfinity), 0.0),
            ChildLayoutHelper.GetDryBaseline);
        TextPainter intrinsics = TextIntrinsics;
        intrinsics.SetPlaceholderDimensions(placeholderDimensions);
        intrinsics.Layout();
        return intrinsics.MaxIntrinsicWidth;
    }

    /// An estimate of the height of a line in the text. See [TextPainter.PreferredLineHeight].
    ///
    /// This does not require the layout to be updated.
    public double PreferredLineHeight => _textPainter.PreferredLineHeight;

    private double ComputeIntrinsicHeight(double width)
    {
        TextPainter intrinsics = TextIntrinsics;
        intrinsics.SetPlaceholderDimensions(RenderInlineChildrenContainerDefaults.LayoutInlineChildren(
            Children,
            width,
            ChildLayoutHelper.DryLayoutChild,
            ChildLayoutHelper.GetDryBaseline));
        intrinsics.Layout(width, AdjustMaxWidth(width));
        return intrinsics.Height;
    }

    protected override double ComputeMinIntrinsicHeight(double width) => ComputeIntrinsicHeight(width);

    protected override double ComputeMaxIntrinsicHeight(double width) => ComputeIntrinsicHeight(width);

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        LayoutTextWithConstraints(Constraints);
        // TODO(garyq): Since our metric for ideographic baseline is currently inaccurate and the non-
        // alphabetic baselines are based off of the alphabetic baseline, we use the alphabetic for
        // now to produce correct layouts. We should eventually change this back to pass the
        // `baseline` property when the ideographic baseline is properly implemented
        // (https://github.com/flutter/flutter/issues/22625).
        return _textPainter.ComputeDistanceToActualBaseline(TextBaseline.Alphabetic);
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        TextPainter intrinsics = TextIntrinsics;
        intrinsics.SetPlaceholderDimensions(RenderInlineChildrenContainerDefaults.LayoutInlineChildren(
            Children,
            constraints.MaxWidth,
            ChildLayoutHelper.DryLayoutChild,
            ChildLayoutHelper.GetDryBaseline));
        intrinsics.Layout(constraints.MinWidth, AdjustMaxWidth(constraints.MaxWidth));
        return intrinsics.ComputeDistanceToActualBaseline(TextBaseline.Alphabetic);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        TextPainter intrinsics = TextIntrinsics;
        intrinsics.SetPlaceholderDimensions(RenderInlineChildrenContainerDefaults.LayoutInlineChildren(
            Children,
            constraints.MaxWidth,
            ChildLayoutHelper.DryLayoutChild,
            ChildLayoutHelper.GetDryBaseline));
        intrinsics.Layout(constraints.MinWidth, AdjustMaxWidth(constraints.MaxWidth));
        return constraints.Constrain(intrinsics.Size);
    }

    private double AdjustMaxWidth(double maxWidth)
    {
        return _softWrap || _overflow == TextOverflow.Ellipsis ? maxWidth : double.PositiveInfinity;
    }

    private void LayoutTextWithConstraints(BoxConstraints constraints)
    {
        _textPainter.SetPlaceholderDimensions(_placeholderDimensions);
        _textPainter.Layout(constraints.MinWidth, AdjustMaxWidth(constraints.MaxWidth));
    }

    protected override void PerformLayout()
    {
        BoxConstraints constraints = Constraints;
        if (_lastSelectableFragments is not null)
        {
            foreach (SelectableFragment fragment in _lastSelectableFragments)
            {
                fragment.DidChangeParagraphLayout();
            }
        }

        _placeholderDimensions = RenderInlineChildrenContainerDefaults.LayoutInlineChildren(
            Children,
            constraints.MaxWidth,
            ChildLayoutHelper.LayoutChild,
            ChildLayoutHelper.GetBaseline);
        LayoutTextWithConstraints(constraints);
        RenderInlineChildrenContainerDefaults.PositionInlineChildren(
            Children,
            _textPainter.InlinePlaceholderBoxes!.Select(box => box.ToRect()).ToList());

        // We grab _textPainter.size and _textPainter.didExceedMaxLines here because assigning to `size`
        // will trigger us to validate our intrinsic sizes, which will change _textPainter's layout
        // because the intrinsic size calculations are destructive, which would mean we would have to
        // re-lay out the text painter.
        Size textSize = _textPainter.Size;
        bool textDidExceedMaxLines = _textPainter.DidExceedMaxLines;
        Size = constraints.Constrain(textSize);

        bool didOverflowHeight = Size.Height < textSize.Height || textDidExceedMaxLines;
        bool didOverflowWidth = Size.Width < textSize.Width;
        // TODO(abarth): We're only measuring the sizes of the line boxes here. If the glyphs draw
        // outside the line boxes, we won't notice. https://github.com/flutter/flutter/issues/35994
        bool hasVisualOverflow = didOverflowWidth || didOverflowHeight;
        if (!hasVisualOverflow)
        {
            _needsClipping = false;
            _overflowShader = null;
            return;
        }

        switch (_overflow)
        {
            case TextOverflow.Visible:
                _needsClipping = false;
                _overflowShader = null;
                break;
            case TextOverflow.Clip:
            case TextOverflow.Ellipsis:
                _needsClipping = true;
                _overflowShader = null;
                break;
            case TextOverflow.Fade:
                _needsClipping = true;
                using (var fadeSizePainter = new TextPainter(
                           text: new TextSpan(style: _textPainter.Text!.Style, text: EllipsisText),
                           textDirection: TextDirection,
                           textScaler: TextScaler,
                           locale: Locale))
                {
                    fadeSizePainter.Layout();
                    if (didOverflowWidth)
                    {
                        (double fadeStart, double fadeEnd) = TextDirection switch
                        {
                            TextDirection.Rtl => (fadeSizePainter.Width, 0.0),
                            _ => (Size.Width - fadeSizePainter.Width, Size.Width),
                        };
                        _overflowShader = new OverflowShader(new Point(fadeStart, 0.0), new Point(fadeEnd, 0.0));
                    }
                    else
                    {
                        double fadeEnd = Size.Height;
                        double fadeStart = fadeEnd - (fadeSizePainter.Height / 2.0);
                        _overflowShader = new OverflowShader(new Point(0.0, fadeStart), new Point(0.0, fadeEnd));
                    }
                }

                break;
        }
    }

    // -- Paint -------------------------------------------------------------------------

    public override void Paint(PaintingContext context, Point offset)
    {
        // Text alignment only triggers repaint so it's possible the text layout has been invalidated
        // but performLayout wasn't called at this point. Make sure the TextPainter has a valid layout.
        LayoutTextWithConstraints(Constraints);
        if (Constants.KDebugMode && RenderingDebug.RepaintTextRainbowEnabled)
        {
            context.Canvas.DrawRectangle(
                new SolidColorBrush(RenderingDebug.CurrentRepaintColor.ToColor()),
                null,
                new Rect(offset, Size));
        }

        if (_lastSelectableFragments is not null)
        {
            if (_needsClipping)
            {
                context.Canvas.Save();
                context.Canvas.ClipRect(new Rect(offset, Size));
            }

            PaintSelectionHighlights(context, offset);
            if (_needsClipping)
            {
                context.Canvas.Restore();
            }
        }

        if (_needsClipping)
        {
            var bounds = new Rect(offset, Size);
            context.Canvas.Save();
            if (_overflowShader is { } shader)
            {
                // Dart saves a layer here and modulates it with the shader after the text and the inline
                // children are painted; Plumix applies the same gradient as an opacity mask up front.
                context.Canvas.PushOpacityMask(shader.CreateBrush(Size), bounds);
            }

            context.Canvas.ClipRect(bounds);
        }

        if (Constants.KDebugMode)
        {
            _textPainter.DebugPaintTextLayoutBoxes = RenderingDebug.PaintTextLayoutBoxes;
        }

        _textPainter.Paint(context.Canvas, offset);
        RenderInlineChildrenContainerDefaults.PaintInlineChildren(Children, context, offset);

        if (_needsClipping)
        {
            context.Canvas.Restore();
        }

        PaintSelectionHandles(context, offset);
    }

    /// <inheritdoc />
    /// <remarks>Dart's <c>RenderParagraph.applyPaintTransform</c>: the inline child's offset.</remarks>
    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        RenderInlineChildrenContainerDefaults.DefaultApplyPaintTransform((RenderBox)child, transform);
    }

    // -- Hit testing ---------------------------------------------------------------------

    protected override bool HitTestSelf(Point position) => true;

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        GlyphInfo? glyph = _textPainter.GetClosestGlyphForOffset(position);
        // The hit-test can't fall through the horizontal gaps between visually adjacent characters
        // on the same line, even with a large letter-spacing or text justification, as
        // graphemeClusterLayoutBounds.width is the advance width to the next character, so there's no
        // gap between their graphemeClusterLayoutBounds rects.
        InlineSpan? spanHit = glyph is not null && Contains(glyph.GraphemeClusterLayoutBounds, position)
            ? _textPainter.Text!.GetSpanForPosition(new TextPosition(glyph.GraphemeClusterCodeUnitRange.Start))
            : null;
        switch (spanHit)
        {
            case IHitTestTarget span:
                result.Add(new HitTestEntry(span));
                return true;
            default:
                return RenderInlineChildrenContainerDefaults.HitTestInlineChildren(Children, result, position);
        }
    }

    // Dart's `Rect.contains`: the left and top edges are inside, the right and bottom edges are not.
    private static bool Contains(Rect rect, Point point)
    {
        return point.X >= rect.Left && point.X < rect.Right && point.Y >= rect.Top && point.Y < rect.Bottom;
    }

    // -- Semantics -------------------------------------------------------------------------

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        _semanticsInfo = Text.GetSemanticsInformation();
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
        configuration.TextDirection = TextDirection;
    }

    protected override void AssembleSemanticsNode(
        SemanticsNode node,
        SemanticsConfiguration config,
        IReadOnlyList<SemanticsNode> children)
    {
        _semanticsInfo ??= Text.GetSemanticsInformation();
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
        IReadOnlyList<TextBox> rects = GetBoxesForSelection(new TextSelection(start, start + length));
        if (rects.Count == 0)
        {
            return null;
        }

        Rect rect = rects[0].ToRect();
        for (int index = 1; index < rects.Count; index += 1)
        {
            rect = rect.Union(rects[index].ToRect());
        }

        return new Rect(
            Math.Floor(rect.Left) - 4.0,
            Math.Floor(rect.Top) - 4.0,
            Math.Ceiling(rect.Width) + 8.0,
            Math.Ceiling(rect.Height) + 8.0);
    }

    // -- Diagnostics -----------------------------------------------------------------------

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

    /// The linear white-to-transparent gradient Dart builds for `TextOverflow.fade`, in paragraph
    /// coordinates.
    private readonly record struct OverflowShader(Point From, Point To)
    {
        public IBrush CreateBrush(Size size)
        {
            double width = Math.Max(size.Width, double.Epsilon);
            double height = Math.Max(size.Height, double.Epsilon);
            return new LinearGradientBrush
            {
                StartPoint = new RelativePoint(From.X / width, From.Y / height, RelativeUnit.Relative),
                EndPoint = new RelativePoint(To.X / width, To.Y / height, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.FromUInt32(0xFFFFFFFF), 0),
                    new GradientStop(Color.FromUInt32(0x00FFFFFF), 1),
                },
            };
        }
    }
}
