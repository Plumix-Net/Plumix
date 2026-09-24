using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/painting/text_painter.dart

namespace Plumix.Painting;

/// A [TextBoundary] subclass for locating word breaks.
///
/// The underlying implementation uses the paragraph's UAX #29 word boundaries. Obtain one from
/// [TextPainter.WordBoundaries].
public sealed class WordBoundary : TextBoundary
{
    private readonly InlineSpan _text;
    private readonly Paragraph _paragraph;
    private TextBoundary? _moveByWordBoundary;

    internal WordBoundary(InlineSpan text, Paragraph paragraph)
    {
        _text = text;
        _paragraph = paragraph;
    }

    public override TextRange GetTextBoundaryAt(int position)
    {
        return _paragraph.GetWordBoundary(new TextPosition(Math.Max(position, 0)));
    }

    /// A [TextBoundary] that can be used to perform word deletion and word traversal (moving the
    /// cursor) on the text.
    ///
    /// Unlike [GetTextBoundaryAt], this boundary skips the spaces and punctuation between words, so
    /// it stops only at the start or end of a word, or at a hard line break.
    public TextBoundary MoveByWordBoundary =>
        _moveByWordBoundary ??= new UntilTextBoundary(this, SkipSpacesAndPunctuations);

    private static int CodePointFromSurrogates(int highSurrogate, int lowSurrogate)
    {
        TextPainter.DebugAssert(
            TextPainter.IsHighSurrogate(highSurrogate),
            $"U+{highSurrogate:X4}) is not a high surrogate.");
        TextPainter.DebugAssert(
            TextPainter.IsLowSurrogate(lowSurrogate),
            $"U+{lowSurrogate:X4}) is not a low surrogate.");
        const int @base = 0x010000 - (0xD800 << 10) - 0xDC00;
        return (highSurrogate << 10) + lowSurrogate + @base;
    }

    // The Runes API is not used: this walks the InlineSpan tree, not a flattened string.
    private int? CodePointAt(int index)
    {
        int? codeUnitAtIndex = _text.CodeUnitAt(index);
        if (codeUnitAtIndex is not { } codeUnit)
        {
            return null;
        }

        return (codeUnit & 0xFC00) switch
        {
            0xD800 => CodePointFromSurrogates(codeUnit, _text.CodeUnitAt(index + 1)!.Value),
            0xDC00 => CodePointFromSurrogates(_text.CodeUnitAt(index - 1)!.Value, codeUnit),
            _ => codeUnit,
        };
    }

    /// Whether the code point is a hard line break (Unicode line breaking classes BK, LF and NL).
    /// CR is not: it only breaks in the CR LF pair.
    internal static bool IsNewline(int codePoint)
    {
        return codePoint is 0x000A or 0x0085 or 0x000B or 0x000C or 0x2028 or 0x2029;
    }

    private bool SkipSpacesAndPunctuations(int offset, bool forward)
    {
        // Use code point since some punctuations are supplementary characters.
        int? innerCodePoint = CodePointAt(forward ? offset - 1 : offset);
        int? outerCodeUnit = _text.CodeUnitAt(forward ? offset : offset - 1);
        // Make sure the hard break rules in UAX#29 take precedence over the ones we add below. Luckily
        // there're only 4 hard break rules for word breaks, and dictionary based breaking does not
        // introduce new hard breaks: https://unicode.org/reports/tr29/#WB4
        bool hardBreakRulesApply = innerCodePoint is null
                                   || outerCodeUnit is null
                                   || IsNewline(innerCodePoint.Value)
                                   || IsNewline(outerCodeUnit.Value);
        return hardBreakRulesApply || !UnicodeText.IsSpaceSeparatorOrPunctuation(innerCodePoint!.Value);
    }
}

/// A text boundary that keeps moving past an inner boundary until the predicate accepts the offset.
internal sealed class UntilTextBoundary : TextBoundary
{
    private readonly TextBoundary _textBoundary;
    private readonly UntilPredicate _predicate;

    public UntilTextBoundary(TextBoundary textBoundary, UntilPredicate predicate)
    {
        _textBoundary = textBoundary;
        _predicate = predicate;
    }

    public override int? GetLeadingTextBoundaryAt(int position)
    {
        if (position < 0)
        {
            return null;
        }

        int? offset = _textBoundary.GetLeadingTextBoundaryAt(position);
        return offset is null || _predicate(offset.Value, false)
            ? offset
            : GetLeadingTextBoundaryAt(offset.Value - 1);
    }

    public override int? GetTrailingTextBoundaryAt(int position)
    {
        int? offset = _textBoundary.GetTrailingTextBoundaryAt(Math.Max(position, 0));
        return offset is null || _predicate(offset.Value, true)
            ? offset
            : GetTrailingTextBoundaryAt(offset.Value);
    }
}

/// An object that paints a [TextSpan] tree into a [Canvas].
///
/// To use a [TextPainter], follow these steps:
///
/// 1. Create a [TextSpan] tree and pass it to the [TextPainter] constructor.
/// 2. Call [Layout] to prepare the paragraph.
/// 3. Call [Paint] as often as desired to paint the paragraph.
/// 4. Call [Dispose] when the object will no longer be accessed to release native resources.
///
/// If the width of the area into which the text is being painted changes, return to step 2. If the
/// text to be painted changes, return to step 1.
public sealed class TextPainter : IDisposable
{
    private bool _debugNeedsRelayout = true;
    private LayoutCacheWithOffset? _layoutCache;
    private bool _rebuildParagraphForPaint = true;
    private string? _debugMarkNeedsLayoutCallStack;
    private string? _cachedPlainText;
    private IReadOnlyList<PlaceholderDimensions>? _placeholderDimensions;
    private Paragraph? _layoutTemplate;
    private LineCaretMetrics _caretMetrics;
    private bool _disposed;

    private InlineSpan? _text;
    private TextAlign _textAlign;
    private TextDirection? _textDirection;
    private TextScaler _textScaler;
    private string? _ellipsis;
    private string? _locale;
    private int? _maxLines;
    private StrutStyle? _strutStyle;
    private TextWidthBasis _textWidthBasis;
    private TextHeightBehavior? _textHeightBehavior;

    /// Creates a text painter that paints the given text.
    ///
    /// The `text` and `textDirection` arguments are optional but [Text] and [TextDirection] must be
    /// non-null before calling [Layout]. `textScaleFactor` is deprecated in favor of `textScaler`;
    /// the two cannot both be specified.
    public TextPainter(
        InlineSpan? text = null,
        TextAlign textAlign = TextAlign.Start,
        TextDirection? textDirection = null,
        double textScaleFactor = 1.0,
        TextScaler? textScaler = null,
        int? maxLines = null,
        string? ellipsis = null,
        string? locale = null,
        StrutStyle? strutStyle = null,
        TextWidthBasis textWidthBasis = TextWidthBasis.Parent,
        TextHeightBehavior? textHeightBehavior = null)
    {
        text?.DebugAssertIsValid();
        DebugAssert(maxLines is null || maxLines > 0, "maxLines == null || maxLines > 0");
        DebugAssert(textScaleFactor == 1.0 || textScaler is null, "Use textScaler instead.");
        _text = text;
        _textAlign = textAlign;
        _textDirection = textDirection;
        _textScaler = textScaler ?? TextScaler.Linear(textScaleFactor);
        _maxLines = maxLines;
        _ellipsis = ellipsis;
        _locale = locale;
        _strutStyle = strutStyle;
        _textWidthBasis = textWidthBasis;
        _textHeightBehavior = textHeightBehavior;
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchCreated("painting", "TextPainter", this);
        }
    }

    /// Computes the width of a configured [TextPainter].
    ///
    /// This is a convenience method that creates a text painter with the supplied parameters, lays
    /// it out with the supplied `minWidth` and `maxWidth`, and returns its [TextPainter.Width] making
    /// sure to dispose the underlying resources. Doing this operation is expensive and should be
    /// avoided whenever it is possible to preserve the [TextPainter] to paint the text or get other
    /// information about it.
    public static double ComputeWidth(
        InlineSpan text,
        TextDirection textDirection,
        TextAlign textAlign = TextAlign.Start,
        double textScaleFactor = 1.0,
        TextScaler? textScaler = null,
        int? maxLines = null,
        string? ellipsis = null,
        string? locale = null,
        StrutStyle? strutStyle = null,
        TextWidthBasis textWidthBasis = TextWidthBasis.Parent,
        TextHeightBehavior? textHeightBehavior = null,
        double minWidth = 0.0,
        double maxWidth = double.PositiveInfinity)
    {
        DebugAssert(textScaleFactor == 1.0 || textScaler is null, "Use textScaler instead.");
        var painter = new TextPainter(
            text: text,
            textAlign: textAlign,
            textDirection: textDirection,
            textScaler: textScaler ?? TextScaler.Linear(textScaleFactor),
            maxLines: maxLines,
            ellipsis: ellipsis,
            locale: locale,
            strutStyle: strutStyle,
            textWidthBasis: textWidthBasis,
            textHeightBehavior: textHeightBehavior);
        try
        {
            painter.Layout(minWidth, maxWidth);
            return painter.Width;
        }
        finally
        {
            painter.Dispose();
        }
    }

    /// Computes the max intrinsic width of a configured [TextPainter].
    ///
    /// This is a convenience method that creates a text painter with the supplied parameters, lays
    /// it out with the supplied `minWidth` and `maxWidth`, and returns its
    /// [TextPainter.MaxIntrinsicWidth] making sure to dispose the underlying resources.
    public static double ComputeMaxIntrinsicWidth(
        InlineSpan text,
        TextDirection textDirection,
        TextAlign textAlign = TextAlign.Start,
        double textScaleFactor = 1.0,
        TextScaler? textScaler = null,
        int? maxLines = null,
        string? ellipsis = null,
        string? locale = null,
        StrutStyle? strutStyle = null,
        TextWidthBasis textWidthBasis = TextWidthBasis.Parent,
        TextHeightBehavior? textHeightBehavior = null,
        double minWidth = 0.0,
        double maxWidth = double.PositiveInfinity)
    {
        DebugAssert(textScaleFactor == 1.0 || textScaler is null, "Use textScaler instead.");
        var painter = new TextPainter(
            text: text,
            textAlign: textAlign,
            textDirection: textDirection,
            textScaler: textScaler ?? TextScaler.Linear(textScaleFactor),
            maxLines: maxLines,
            ellipsis: ellipsis,
            locale: locale,
            strutStyle: strutStyle,
            textWidthBasis: textWidthBasis,
            textHeightBehavior: textHeightBehavior);
        try
        {
            painter.Layout(minWidth, maxWidth);
            return painter.MaxIntrinsicWidth;
        }
        finally
        {
            painter.Dispose();
        }
    }

    /// Marks this text painter's layout information as dirty and removes cached information.
    ///
    /// Uses this method to notify text painter to relayout in the case of layout changes in engine.
    /// In most cases, updating text painter properties in framework will automatically invoke this
    /// method.
    public void MarkNeedsLayout()
    {
        if (Constants.KDebugMode && _layoutCache is not null)
        {
            _debugMarkNeedsLayoutCallStack ??= Environment.StackTrace;
        }

        _layoutCache?.Paragraph.Dispose();
        _layoutCache = null;
    }

    /// The (potentially styled) text to paint.
    ///
    /// After this is set, you must call [Layout] before the next call to [Paint]. This and
    /// [TextDirection] must be non-null before you call [Layout].
    ///
    /// The [InlineSpan] this provides is in the form of a tree that may contain multiple instances
    /// of [TextSpan]s and [WidgetSpan]s. To obtain a plain text representation of the contents of
    /// this [TextPainter], use [PlainText].
    public InlineSpan? Text
    {
        get => _text;
        set
        {
            value?.DebugAssertIsValid();
            if (Equals(_text, value))
            {
                return;
            }

            if (!Equals(_text?.Style, value?.Style))
            {
                _layoutTemplate?.Dispose();
                _layoutTemplate = null;
            }

            RenderComparison comparison = value is null
                ? RenderComparison.Layout
                : _text?.CompareTo(value) ?? RenderComparison.Layout;

            _text = value;
            _cachedPlainText = null;

            if (comparison >= RenderComparison.Layout)
            {
                MarkNeedsLayout();
            }
            else if (comparison >= RenderComparison.Paint)
            {
                // Don't invalidate the layout cache here: the text layout is not affected and the
                // paragraph is rebuilt on the next paint.
                _rebuildParagraphForPaint = true;
            }

            // Neither relayout or repaint is needed.
        }
    }

    /// Returns a plain text version of the text to paint.
    ///
    /// This uses [InlineSpan.ToPlainText] to get the full contents of all nodes in the tree.
    public string PlainText
    {
        get
        {
            _cachedPlainText ??= _text?.ToPlainText(includeSemanticsLabels: false);
            return _cachedPlainText ?? string.Empty;
        }
    }

    /// How the text should be aligned horizontally.
    ///
    /// After this is set, you must call [Layout] before the next call to [Paint].
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

    /// The default directionality of the text.
    ///
    /// This controls how the [TextAlign.Start], [TextAlign.End], and [TextAlign.Justify] values of
    /// [TextAlign] are resolved. After this is set, you must call [Layout] before the next call to
    /// [Paint]. This and [Text] must be non-null before you call [Layout].
    public TextDirection? TextDirection
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
            _layoutTemplate?.Dispose();
            // Shouldn't really matter, but for strict correctness...
            _layoutTemplate = null;
        }
    }

    /// Deprecated. Will be removed in a future version of Flutter. Use [TextScaler] instead.
    [Obsolete("Use textScaler instead. Use of textScaleFactor was deprecated in preparation for the upcoming "
              + "nonlinear text scaling support. This feature was deprecated after v3.12.0-2.0.pre.")]
    public double TextScaleFactor
    {
        get => TextScaler.TextScaleFactor;
        set => TextScaler = TextScaler.Linear(value);
    }

    /// The font scaling strategy to use when laying out and rendering the text.
    ///
    /// After this is set, you must call [Layout] before the next call to [Paint].
    public TextScaler TextScaler
    {
        get => _textScaler;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (Equals(value, _textScaler))
            {
                return;
            }

            _textScaler = value;
            MarkNeedsLayout();
            _layoutTemplate?.Dispose();
            _layoutTemplate = null;
        }
    }

    /// The string used to ellipsize overflowing text. Setting this to a non-empty string will cause
    /// this string to be substituted for the remaining text if the text can not fit within the
    /// specified maximum width.
    ///
    /// The value may be null. If it is not null, then it must not be empty.
    ///
    /// After this is set, you must call [Layout] before the next call to [Paint].
    public string? Ellipsis
    {
        get => _ellipsis;
        set
        {
            DebugAssert(value is null || value.Length > 0, "value == null || value.isNotEmpty");
            if (_ellipsis == value)
            {
                return;
            }

            _ellipsis = value;
            MarkNeedsLayout();
        }
    }

    /// The locale used to select region-specific glyphs.
    public string? Locale
    {
        get => _locale;
        set
        {
            if (_locale == value)
            {
                return;
            }

            _locale = value;
            MarkNeedsLayout();
        }
    }

    /// An optional maximum number of lines for the text to span, wrapping if necessary.
    ///
    /// If the text exceeds the given number of lines, it is truncated such that subsequent lines are
    /// dropped. After this is set, you must call [Layout] before the next call to [Paint].
    public int? MaxLines
    {
        get => _maxLines;
        set
        {
            DebugAssert(value is null || value > 0, "value == null || value > 0");
            if (_maxLines == value)
            {
                return;
            }

            _maxLines = value;
            MarkNeedsLayout();
        }
    }

    /// The strut style to use. Strut style defines the strut, which sets minimum vertical layout
    /// metrics.
    ///
    /// Omitting or providing null will disable strut. Omitting or providing null for any properties
    /// of [StrutStyle] will result in default values being used. It is highly recommended to at
    /// least specify a [StrutStyle.FontSize].
    public StrutStyle? StrutStyle
    {
        get => _strutStyle;
        set
        {
            if (_strutStyle == value)
            {
                return;
            }

            _strutStyle = value;
            MarkNeedsLayout();
        }
    }

    /// Defines how to measure the width of the rendered text.
    public TextWidthBasis TextWidthBasis
    {
        get => _textWidthBasis;
        set
        {
            if (_textWidthBasis == value)
            {
                return;
            }

            if (Constants.KDebugMode)
            {
                _debugNeedsRelayout = true;
            }

            _textWidthBasis = value;
        }
    }

    /// Defines how to apply [TextStyle.Height] over and under text.
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

    /// Whether [Paint] draws the layout box of every character, for debugging.
    public bool DebugPaintTextLayoutBoxes { get; set; }

    /// An ordered list of [TextBox]es that bound the positions of the placeholders in the paragraph.
    ///
    /// Each box corresponds to a [PlaceholderSpan] in the order they were defined in the
    /// [InlineSpan] tree. Returns null before the first layout.
    public IReadOnlyList<TextBox>? InlinePlaceholderBoxes
    {
        get
        {
            LayoutCacheWithOffset? layout = _layoutCache;
            if (layout is null)
            {
                return null;
            }

            Point offset = layout.PaintOffset;
            if (!double.IsFinite(offset.X) || !double.IsFinite(offset.Y))
            {
                return [];
            }

            IReadOnlyList<TextBox> rawBoxes = layout.InlinePlaceholderBoxes;
            if (offset == default)
            {
                return rawBoxes;
            }

            return rawBoxes.Select(box => ShiftTextBox(box, offset)).ToList();
        }
    }

    /// Sets the dimensions of each placeholder in [Text].
    ///
    /// The number of [PlaceholderDimensions] provided should be the same as the number of
    /// [PlaceholderSpan]s in [Text]. Passing in an empty or null list does nothing.
    ///
    /// If [Layout] is attempted without setting the placeholder dimensions, the placeholders will be
    /// ignored in the text layout and no valid [InlinePlaceholderBoxes] will be returned.
    public void SetPlaceholderDimensions(IReadOnlyList<PlaceholderDimensions>? value)
    {
        if (value is null || value.Count == 0 || ListEquals(value, _placeholderDimensions))
        {
            return;
        }

        if (Constants.KDebugMode)
        {
            int placeholderCount = 0;
            Text!.VisitChildren(span =>
            {
                if (span is PlaceholderSpan)
                {
                    placeholderCount += 1;
                }

                return value.Count >= placeholderCount;
            });
            DebugAssert(placeholderCount == value.Count, "placeholderCount == value.length");
        }

        _placeholderDimensions = value;
        MarkNeedsLayout();
    }

    private ParagraphStyle CreateParagraphStyle(TextAlign? textAlignOverride = null)
    {
        DebugAssert(
            _textDirection is not null,
            "TextPainter.textDirection must be set to a non-null value before using the TextPainter.");
        TextStyle baseStyle = _text?.Style ?? new TextStyle();
        return baseStyle.GetParagraphStyle(
            textAlign: textAlignOverride ?? _textAlign,
            textDirection: _textDirection,
            textScaler: _textScaler,
            maxLines: _maxLines,
            textHeightBehavior: _textHeightBehavior,
            ellipsis: _ellipsis,
            locale: _locale,
            strutStyle: _strutStyle);
    }

    // This ParagraphStyle is only used for the layout template, so the text alignment is always left.
    private Paragraph CreateLayoutTemplate()
    {
        var builder = new ParagraphBuilder(CreateParagraphStyle(TextAlign.Left));
        ParagraphTextStyle? textStyle = _text?.Style?.GetTextStyle(textScaler: _textScaler);
        if (textStyle is not null)
        {
            builder.PushStyle(textStyle);
        }

        builder.AddText(" ");
        Paragraph paragraph = builder.Build();
        paragraph.Layout(new ParagraphConstraints(double.PositiveInfinity));
        return paragraph;
    }

    private Paragraph GetOrCreateLayoutTemplate() => _layoutTemplate ??= CreateLayoutTemplate();

    /// The height of a space in [Text] in logical pixels.
    ///
    /// Not every line of text in [Text] will have this height, but this height is "typical" for text
    /// in [Text] and useful for sizing other objects relative a typical line of text. Obtaining this
    /// value does not require calling [Layout].
    public double PreferredLineHeight => GetOrCreateLayoutTemplate().Height;

    /// The width at which decreasing the width of the text would prevent it from painting itself
    /// completely within its bounds.
    ///
    /// Valid only after [Layout] has been called.
    public double MinIntrinsicWidth
    {
        get
        {
            DebugAssertTextLayoutIsValid();
            return _layoutCache!.Layout.MinIntrinsicLineExtent;
        }
    }

    /// The width at which increasing the width of the text no longer decreases the height.
    ///
    /// Valid only after [Layout] has been called.
    public double MaxIntrinsicWidth
    {
        get
        {
            DebugAssertTextLayoutIsValid();
            return _layoutCache!.Layout.MaxIntrinsicLineExtent;
        }
    }

    /// The horizontal space required to paint this text.
    ///
    /// Valid only after [Layout] has been called.
    public double Width
    {
        get
        {
            DebugAssertTextLayoutIsValid();
            DebugAssert(!_debugNeedsRelayout, "!_debugNeedsRelayout");
            return _layoutCache!.ContentWidth;
        }
    }

    /// The vertical space required to paint this text.
    ///
    /// Valid only after [Layout] has been called.
    public double Height
    {
        get
        {
            DebugAssertTextLayoutIsValid();
            return _layoutCache!.Layout.Height;
        }
    }

    /// The amount of space required to paint this text.
    ///
    /// Valid only after [Layout] has been called.
    public Size Size
    {
        get
        {
            DebugAssertTextLayoutIsValid();
            DebugAssert(!_debugNeedsRelayout, "!_debugNeedsRelayout");
            return new Size(Width, Height);
        }
    }

    /// Returns the distance from the top of the text to the first baseline of the given type.
    ///
    /// Valid only after [Layout] has been called.
    public double ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        DebugAssertTextLayoutIsValid();
        return _layoutCache!.Layout.GetDistanceToBaseline(baseline);
    }

    /// Whether any text was truncated or ellipsized.
    ///
    /// If [MaxLines] is not null, this is true if there were more lines to be drawn than the given
    /// [MaxLines], and thus at least one line was omitted in the output; otherwise it is false.
    ///
    /// If [MaxLines] is null, this is true if [Ellipsis] is not null and there was a line that
    /// overflowed the `maxWidth` argument passed to [Layout].
    ///
    /// Valid only after [Layout] has been called.
    public bool DidExceedMaxLines
    {
        get
        {
            DebugAssertTextLayoutIsValid();
            return _layoutCache!.Paragraph.DidExceedMaxLines;
        }
    }

    // Creates a paragraph using the current configurations in this class. Assign the returned value
    // to the layout cache instead of disposing it, as the paragraph can be reused.
    private Paragraph CreateParagraph(InlineSpan text)
    {
        var builder = new ParagraphBuilder(CreateParagraphStyle());
        text.Build(builder, _textScaler, _placeholderDimensions);
        if (Constants.KDebugMode)
        {
            _debugMarkNeedsLayoutCallStack = null;
        }

        _rebuildParagraphForPaint = false;
        return builder.Build();
    }

    /// Computes the visual position of the glyphs for painting the text.
    ///
    /// The text will layout with a width that's as close to its max intrinsic width (or its longest
    /// line, if [TextWidthBasis] is set to [TextWidthBasis.LongestLine]) as possible while still
    /// being greater than or equal to `minWidth` and less than or equal to `maxWidth`.
    ///
    /// The [Text] and [TextDirection] properties must be non-null before this is called.
    public void Layout(double minWidth = 0.0, double maxWidth = double.PositiveInfinity)
    {
        DebugAssert(!double.IsNaN(maxWidth), "!maxWidth.isNaN");
        DebugAssert(!double.IsNaN(minWidth), "!minWidth.isNaN");
        if (Constants.KDebugMode)
        {
            _debugNeedsRelayout = false;
        }

        LayoutCacheWithOffset? cachedLayout = _layoutCache;
        if (cachedLayout is not null && cachedLayout.ResizeToFit(minWidth, maxWidth, _textWidthBasis))
        {
            return;
        }

        InlineSpan text = _text
                          ?? throw new InvalidOperationException(
                              "TextPainter.text must be set to a non-null value before using the TextPainter.");
        TextDirection textDirection = _textDirection
                                      ?? throw new InvalidOperationException(
                                          "TextPainter.textDirection must be set to a non-null value before "
                                          + "using the TextPainter.");

        double paintOffsetAlignment = ComputePaintOffsetFraction(_textAlign, textDirection);
        // Try to avoid laying out the paragraph with maxWidth=double.infinity when the text is not
        // left-aligned, so we don't have to deal with an infinite paint offset.
        bool adjustMaxWidth = !double.IsFinite(maxWidth) && paintOffsetAlignment != 0;
        double? adjustedMaxWidth = !adjustMaxWidth ? maxWidth : cachedLayout?.Layout.MaxIntrinsicLineExtent;
        double layoutMaxWidth = adjustedMaxWidth ?? maxWidth;

        // Only rebuild the paragraph when there're layout changes, even when `_rebuildParagraphForPaint`
        // is true. It's best to not eagerly rebuild the paragraph to avoid the extra work, because:
        // 1. the text color could change again before `paint` is called (so one of the paragraph
        //    rebuilds is unnecessary)
        // 2. the user could be measuring the text layout so `paint` will never be called.
        Paragraph paragraph = cachedLayout?.Paragraph ?? CreateParagraph(text);
        paragraph.Layout(new ParagraphConstraints(layoutMaxWidth));
        var layout = new PainterTextLayout(paragraph, textDirection, this);
        double contentWidth = layout.ContentWidthFor(minWidth, maxWidth, _textWidthBasis);

        LayoutCacheWithOffset newLayoutCache;
        // Call layout again if the paragraph was laid out with infinite width and the text is not
        // left-aligned.
        if (adjustedMaxWidth is null && double.IsFinite(minWidth))
        {
            DebugAssert(double.IsInfinity(maxWidth), "maxWidth.isInfinite");
            double newInputWidth = layout.MaxIntrinsicLineExtent;
            paragraph.Layout(new ParagraphConstraints(newInputWidth));
            newLayoutCache = new LayoutCacheWithOffset(layout, paintOffsetAlignment, newInputWidth, contentWidth);
        }
        else
        {
            newLayoutCache = new LayoutCacheWithOffset(layout, paintOffsetAlignment, layoutMaxWidth, contentWidth);
        }

        _layoutCache = newLayoutCache;
    }

    /// Paints the text onto the given canvas at the given offset.
    ///
    /// Valid only after [Layout] has been called.
    /// <exception cref="InvalidOperationException">[Layout] has not been called.</exception>
    public void Paint(Canvas canvas, Point offset)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        LayoutCacheWithOffset layoutCache = _layoutCache
                                            ?? throw new InvalidOperationException(
                                                "TextPainter.paint called when text geometry was not yet "
                                                + "calculated.\nPlease call layout() before paint() to position "
                                                + "the text before painting it.");

        if (!double.IsFinite(layoutCache.PaintOffset.X) || !double.IsFinite(layoutCache.PaintOffset.Y))
        {
            return;
        }

        if (_rebuildParagraphForPaint)
        {
            Size? debugSize = null;
            if (Constants.KDebugMode)
            {
                debugSize = Size;
            }

            Paragraph paragraph = layoutCache.Paragraph;
            // Unfortunately even if we know that there is only paint changes, there's no API to only
            // make those updates so the paragraph has to be recreated and re-laid out.
            DebugAssert(!double.IsNaN(layoutCache.LayoutMaxWidth), "!layoutCache.layoutMaxWidth.isNaN");
            Paragraph rebuilt = CreateParagraph(_text!);
            rebuilt.Layout(new ParagraphConstraints(layoutCache.LayoutMaxWidth));
            layoutCache.Layout.Paragraph = rebuilt;
            DebugAssert(paragraph.Width == rebuilt.Width, "paragraph.width == layoutCache.layout._paragraph.width");
            paragraph.Dispose();
            DebugAssert(debugSize == Size, "debugSize == size");
        }

        DebugAssert(!_rebuildParagraphForPaint, "!_rebuildParagraphForPaint");
        if (Constants.KDebugMode && DebugPaintTextLayoutBoxes)
        {
            DebugPaintCharacterLayoutBoxes(canvas, offset);
        }

        Point paintOffset = layoutCache.PaintOffset;
        canvas.DrawParagraph(layoutCache.Paragraph, new Point(offset.X + paintOffset.X, offset.Y + paintOffset.Y));
    }

    private void DebugPaintCharacterLayoutBoxes(Canvas canvas, Point offset)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromUInt32(0xFF00FFFF)), 1.0);
        IReadOnlyList<TextBox> textBoxes = GetBoxesForSelection(new TextSelection(0, PlainText.Length));
        foreach (TextBox textBox in textBoxes)
        {
            Rect rect = textBox.ToRect();
            canvas.DrawRectangle(null, pen, rect.Translate(new Vector(offset.X, offset.Y)));
        }
    }

    /// Returns true iff the given value is a valid UTF-16 high (first) surrogate. The value must be
    /// a UTF-16 code unit, meaning it must be in the range 0x0000-0xFFFF.
    public static bool IsHighSurrogate(int value)
    {
        DebugAssert(IsUtf16(value), "_isUTF16(value)");
        return (value & 0xFC00) == 0xD800;
    }

    /// Returns true iff the given value is a valid UTF-16 low (second) surrogate. The value must be
    /// a UTF-16 code unit, meaning it must be in the range 0x0000-0xFFFF.
    public static bool IsLowSurrogate(int value)
    {
        DebugAssert(IsUtf16(value), "_isUTF16(value)");
        return (value & 0xFC00) == 0xDC00;
    }

    private static bool IsUtf16(int value) => value >= 0x0 && value <= 0xFFFFF;

    /// Returns the closest offset after `offset` at which the input cursor can be positioned.
    public int? GetOffsetAfter(int offset)
    {
        int? nextCodeUnit = _text!.CodeUnitAt(offset);
        if (nextCodeUnit is null)
        {
            return null;
        }

        // TODO(goderbauer): doesn't handle extended grapheme clusters with more than one Unicode
        // scalar value (https://github.com/flutter/flutter/issues/13404).
        return IsHighSurrogate(nextCodeUnit.Value) ? offset + 2 : offset + 1;
    }

    /// Returns the closest offset before `offset` at which the input cursor can be positioned.
    public int? GetOffsetBefore(int offset)
    {
        int? prevCodeUnit = _text!.CodeUnitAt(offset - 1);
        if (prevCodeUnit is null)
        {
            return null;
        }

        // TODO(goderbauer): doesn't handle extended grapheme clusters with more than one Unicode
        // scalar value (https://github.com/flutter/flutter/issues/13404).
        return IsLowSurrogate(prevCodeUnit.Value) ? offset - 2 : offset - 1;
    }

    private static double ComputePaintOffsetFraction(TextAlign textAlign, TextDirection textDirection)
    {
        return (textAlign, textDirection) switch
        {
            (TextAlign.Left, _) => 0.0,
            (TextAlign.Right, _) => 1.0,
            (TextAlign.Center, _) => 0.5,
            (TextAlign.Start or TextAlign.Justify, UI.TextDirection.Ltr) => 0.0,
            (TextAlign.Start or TextAlign.Justify, UI.TextDirection.Rtl) => 1.0,
            (TextAlign.End, UI.TextDirection.Ltr) => 1.0,
            _ => 0.0,
        };
    }

    /// Returns the offset at which to paint the caret.
    ///
    /// Valid only after [Layout] has been called.
    public Point GetOffsetForCaret(TextPosition position, Rect caretPrototype)
    {
        DebugAssertTextLayoutIsValid();
        LayoutCacheWithOffset layoutCache = _layoutCache!;
        LineCaretMetrics? caretMetrics = ComputeCaretMetrics(position);

        if (caretMetrics is not { } metrics)
        {
            double paintOffsetAlignment = ComputePaintOffsetFraction(_textAlign, _textDirection!.Value);
            // The full width is not (width - caretPrototype.width), because RenderEditable reserves
            // cursor width on the right. Ideally this should be handled by RenderEditable instead.
            double dx = paintOffsetAlignment == 0 ? 0 : paintOffsetAlignment * layoutCache.ContentWidth;
            return new Point(dx, 0.0);
        }

        Point rawOffset = metrics.WritingDirection == UI.TextDirection.Ltr
            ? metrics.Offset
            : new Point(metrics.Offset.X - caretPrototype.Width, metrics.Offset.Y);
        // If offset.dx is outside of the advertised content area, then the associated glyph belongs to
        // a trailing whitespace character. Ideally the behavior should be handled by higher-level
        // implementations (for instance, RenderEditable reserves width for showing the caret, it's
        // best to handle the clamping there).
        double adjustedDx = ClampDouble(rawOffset.X + layoutCache.PaintOffset.X, 0, layoutCache.ContentWidth);
        return new Point(adjustedDx, rawOffset.Y + layoutCache.PaintOffset.Y);
    }

    /// Returns the strut bounded height of the glyph at the given `position`.
    ///
    /// Valid only after [Layout] has been called.
    public double GetFullHeightForCaret(TextPosition position, Rect caretPrototype)
    {
        // The if condition is derived from skparagraph's TextLine.cpp (L1244-L1246 at 0086a17e), which is
        // set by the engine's lib/ui/text/paragraph_builder.cc (L381-L390 at a821b879).
        if (StrutDisabled && ComputeCaretMetrics(position)?.Height is { } height)
        {
            return height;
        }

        IReadOnlyList<TextBox> boxes = GetOrCreateLayoutTemplate()
            .GetBoxesForRange(0, 1, boxHeightStyle: BoxHeightStyle.Strut);
        // boxes is currently empty because height is non-zero while fontSize or textScaler is 0.0.
        return boxes.Count == 0 ? PreferredLineHeight : boxes.Single().ToRect().Height;
    }

    private bool StrutDisabled
    {
        get
        {
            StrutStyle? strutStyle = _strutStyle;
            if (strutStyle is null || strutStyle == Widgets.StrutStyle.Disabled)
            {
                return true;
            }

            return strutStyle.FontSize == 0.0;
        }
    }

    // Checks if the [position] and [bias] parameters are valid.
    private bool IsNewlineAtOffset(int offset)
    {
        return 0 <= offset && offset < PlainText.Length && WordBoundary.IsNewline(PlainText[offset]);
    }

    private LineCaretMetrics? ComputeCaretMetrics(TextPosition position)
    {
        DebugAssertTextLayoutIsValid();
        DebugAssert(!_debugNeedsRelayout, "!_debugNeedsRelayout");
        LayoutCacheWithOffset cachedLayout = _layoutCache!;
        Paragraph paragraph = cachedLayout.Paragraph;

        // If the paragraph is empty, return null. The caret should be placed at the start of the
        // paragraph.
        if (paragraph.NumberOfLines < 1)
        {
            return null;
        }

        (int offset, bool anchorToLeadingEdge) = position switch
        {
            { Offset: 0 } => (0, true),
            { Affinity: TextAffinity.Downstream } => (position.Offset, true),
            { Affinity: TextAffinity.Upstream } when IsNewlineAtOffset(position.Offset - 1) => (position.Offset, true),
            _ => (position.Offset - 1, false),
        };

        int caretPositionCacheKey = anchorToLeadingEdge ? offset : -offset - 1;
        if (caretPositionCacheKey == cachedLayout.PreviousCaretPositionKey)
        {
            return _caretMetrics;
        }

        GlyphInfo? glyphInfo = paragraph.GetGlyphInfoAt(offset);

        if (glyphInfo is null)
        {
            // If the glyph isn't laid out, then the position points to a character that is not laid
            // out (the character is not in the visible text), or it's the end of the text. Use the
            // metrics of the end of the text instead.
            Paragraph template = GetOrCreateLayoutTemplate();
            DebugAssert(template.NumberOfLines == 1, "template.numberOfLines == 1");
            double baselineOffset = template.GetLineMetricsAt(0)!.Value.Baseline;
            return cachedLayout.Layout.EndOfTextCaretMetrics.Shift(new Point(0.0, -baselineOffset));
        }

        TextRange graphemeRange = glyphInfo.GraphemeClusterCodeUnitRange;

        // Work around a SkParagraph bug (https://github.com/flutter/flutter/issues/120836#issuecomment-1937343854):
        // placeholders with a size of (0, 0) always have a rect of Rect.zero and a range of (0, 0).
        if (graphemeRange.IsCollapsed)
        {
            DebugAssert(graphemeRange.Start == 0, "graphemeRange.start == 0");
            return ComputeCaretMetrics(new TextPosition(offset + 1));
        }

        if (anchorToLeadingEdge && graphemeRange.Start != offset)
        {
            DebugAssert(graphemeRange.End > graphemeRange.Start + 1, "graphemeRange.end > graphemeRange.start + 1");
            // Addresses the case where the offset points to a multi-code-unit grapheme that doesn't
            // start at `offset`.
            return ComputeCaretMetrics(new TextPosition(graphemeRange.End));
        }

        IReadOnlyList<TextBox> boxes = paragraph.GetBoxesForRange(
            graphemeRange.Start,
            graphemeRange.End,
            boxHeightStyle: BoxHeightStyle.Strut);

        bool anchorToLeft = glyphInfo.WritingDirection == UI.TextDirection.Ltr
            ? anchorToLeadingEdge
            : !anchorToLeadingEdge;
        TextBox box = anchorToLeft ? boxes[0] : boxes[^1];
        var metrics = new LineCaretMetrics(
            new Point(anchorToLeft ? box.Left : box.Right, box.Top),
            box.Direction,
            box.Bottom - box.Top);

        cachedLayout.PreviousCaretPositionKey = caretPositionCacheKey;
        _caretMetrics = metrics;
        return metrics;
    }

    /// Returns a list of rects that bound the given selection.
    ///
    /// The `boxHeightStyle` and `boxWidthStyle` arguments may be used to select the shape of the
    /// [TextBox]es. These properties default to [BoxHeightStyle.Tight] and [BoxWidthStyle.Tight]
    /// respectively.
    ///
    /// A given selection might have more than one rect if the [TextPainter] contains multiple
    /// [InlineSpan]s or bidirectional text, because logically contiguous text might not be visually
    /// contiguous.
    ///
    /// Leading or trailing newline characters will be represented by zero-width `TextBox`es.
    ///
    /// The method only returns `TextBox`es of glyphs that are entirely enclosed by the given
    /// `selection`: a multi-code-unit glyph will be excluded if only part of its code units are in
    /// `selection`.
    public IReadOnlyList<TextBox> GetBoxesForSelection(
        TextSelection selection,
        BoxHeightStyle boxHeightStyle = BoxHeightStyle.Tight,
        BoxWidthStyle boxWidthStyle = BoxWidthStyle.Tight)
    {
        DebugAssertTextLayoutIsValid();
        DebugAssert(selection.IsValid, "selection.isValid");
        DebugAssert(!_debugNeedsRelayout, "!_debugNeedsRelayout");
        LayoutCacheWithOffset cachedLayout = _layoutCache!;
        Point offset = cachedLayout.PaintOffset;
        if (!double.IsFinite(offset.X) || !double.IsFinite(offset.Y))
        {
            return [];
        }

        IReadOnlyList<TextBox> boxes = cachedLayout.Paragraph.GetBoxesForRange(
            selection.Start,
            selection.End,
            boxHeightStyle,
            boxWidthStyle);
        return offset == default ? boxes : boxes.Select(box => ShiftTextBox(box, offset)).ToList();
    }

    /// Returns the [GlyphInfo] of the glyph closest to the given `offset` in the paragraph
    /// coordinate system, or null if the text is empty, or is entirely clipped or ellipsized away.
    public GlyphInfo? GetClosestGlyphForOffset(Point offset)
    {
        DebugAssertTextLayoutIsValid();
        DebugAssert(!_debugNeedsRelayout, "!_debugNeedsRelayout");
        LayoutCacheWithOffset cachedLayout = _layoutCache!;
        Point paintOffset = cachedLayout.PaintOffset;
        GlyphInfo? rawGlyphInfo = cachedLayout.Paragraph.GetClosestGlyphInfoForOffset(
            new Point(offset.X - paintOffset.X, offset.Y - paintOffset.Y));
        if (rawGlyphInfo is null || paintOffset == default)
        {
            return rawGlyphInfo;
        }

        return new GlyphInfo(
            rawGlyphInfo.GraphemeClusterLayoutBounds.Translate(new Vector(paintOffset.X, paintOffset.Y)),
            rawGlyphInfo.GraphemeClusterCodeUnitRange,
            rawGlyphInfo.WritingDirection);
    }

    /// Returns the position within the text for the given pixel offset.
    public TextPosition GetPositionForOffset(Point offset)
    {
        DebugAssertTextLayoutIsValid();
        DebugAssert(!_debugNeedsRelayout, "!_debugNeedsRelayout");
        LayoutCacheWithOffset cachedLayout = _layoutCache!;
        Point paintOffset = cachedLayout.PaintOffset;
        return cachedLayout.Paragraph.GetPositionForOffset(
            new Point(offset.X - paintOffset.X, offset.Y - paintOffset.Y));
    }

    /// Returns the text range of the word at the given offset. Characters not part of a word, such
    /// as spaces, symbols, and punctuation, have word breaks on both sides. In such cases, this
    /// method will return a text range that contains the given text position.
    ///
    /// Word boundaries are defined more precisely in Unicode Standard Annex #29
    /// <http://www.unicode.org/reports/tr29/#Word_Boundaries>.
    public TextRange GetWordBoundary(TextPosition position)
    {
        DebugAssertTextLayoutIsValid();
        return _layoutCache!.Paragraph.GetWordBoundary(position);
    }

    /// Returns a [TextBoundary] that can be used to perform word boundary analysis on the current
    /// [Text].
    ///
    /// This [TextBoundary] uses word boundary rules defined in Unicode Standard Annex #29.
    ///
    /// Currently word boundary analysis can only be performed after [Layout] has been called.
    public WordBoundary WordBoundaries => new(Text!, _layoutCache!.Paragraph);

    /// Returns the text range of the line at the given offset.
    ///
    /// The newline (if any) is not returned as part of the range.
    public TextRange GetLineBoundary(TextPosition position)
    {
        DebugAssertTextLayoutIsValid();
        return _layoutCache!.Paragraph.GetLineBoundary(position);
    }

    private static LineMetrics ShiftLineMetrics(LineMetrics metrics, Point offset)
    {
        DebugAssert(double.IsFinite(offset.X), "offset.dx.isFinite");
        DebugAssert(double.IsFinite(offset.Y), "offset.dy.isFinite");
        return metrics with { Left = metrics.Left + offset.X, Baseline = metrics.Baseline + offset.Y };
    }

    private static TextBox ShiftTextBox(TextBox box, Point offset)
    {
        DebugAssert(double.IsFinite(offset.X), "offset.dx.isFinite");
        DebugAssert(double.IsFinite(offset.Y), "offset.dy.isFinite");
        return TextBox.FromLTRBD(
            box.Left + offset.X,
            box.Top + offset.Y,
            box.Right + offset.X,
            box.Bottom + offset.Y,
            box.Direction);
    }

    /// Returns the full list of [LineMetrics] that describe in detail the various metrics of each
    /// laid out line.
    ///
    /// The [LineMetrics] list is presented in the order of the lines they represent. For example,
    /// the first line is in the zeroth index.
    ///
    /// [LineMetrics] contains measurements such as ascent, descent, baseline, and width for the
    /// line as a whole, and may be useful for aligning additional widgets to a particular line.
    ///
    /// Valid only after [Layout] has been called.
    public IReadOnlyList<LineMetrics> ComputeLineMetrics()
    {
        DebugAssertTextLayoutIsValid();
        DebugAssert(!_debugNeedsRelayout, "!_debugNeedsRelayout");
        LayoutCacheWithOffset layout = _layoutCache!;
        Point offset = layout.PaintOffset;
        if (!double.IsFinite(offset.X) || !double.IsFinite(offset.Y))
        {
            return [];
        }

        IReadOnlyList<LineMetrics> rawMetrics = layout.LineMetrics;
        return offset == default
            ? rawMetrics
            : rawMetrics.Select(metrics => ShiftLineMetrics(metrics, offset)).ToList();
    }

    /// Whether this object has been disposed or not.
    ///
    /// Only for use when asserts are enabled.
    public bool DebugDisposed
    {
        get
        {
            if (!Constants.KDebugMode)
            {
                throw new InvalidOperationException("debugDisposed only available when asserts are on.");
            }

            return _disposed;
        }
    }

    /// Releases the resources associated with this painter.
    ///
    /// After disposal this painter is unusable.
    public void Dispose()
    {
        DebugAssert(!DebugDisposed, "!debugDisposed");
        if (Constants.KDebugMode)
        {
            _disposed = true;
            FoundationDebug.DebugMaybeDispatchDisposed(this);
        }

        _layoutTemplate?.Dispose();
        _layoutTemplate = null;
        _layoutCache?.Paragraph.Dispose();
        _layoutCache = null;
        _text = null;
    }

    private bool DebugAssertTextLayoutIsValid()
    {
        if (!Constants.KDebugMode)
        {
            return true;
        }

        DebugAssert(!DebugDisposed, "!debugDisposed");
        if (_layoutCache is null)
        {
            throw new FlutterError([
                new ErrorSummary("Text layout not available"),
                _debugMarkNeedsLayoutCallStack is not null
                    ? new DiagnosticsStackTrace(
                        "The calls that first invalidated the text layout were",
                        _debugMarkNeedsLayoutCallStack)
                    : new ErrorDescription("The TextPainter has never been laid out."),
            ]);
        }

        return true;
    }

    internal static void DebugAssert(bool condition, string message)
    {
        if (Constants.KDebugMode && !condition)
        {
            throw new AssertionError(message);
        }
    }

    private static double ClampDouble(double value, double min, double max)
    {
        DebugAssert(min <= max, "min <= max");
        return value < min ? min : value > max ? max : value;
    }

    private static bool ListEquals(IReadOnlyList<PlaceholderDimensions> a, IReadOnlyList<PlaceholderDimensions>? b)
    {
        if (b is null || a.Count != b.Count)
        {
            return false;
        }

        if (ReferenceEquals(a, b))
        {
            return true;
        }

        for (int index = 0; index < a.Count; index++)
        {
            if (!Equals(a[index], b[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// A [TextPainter] layout: the laid-out paragraph and the writing direction it was laid out
    /// with. Dart's `_TextLayout`.
    private sealed class PainterTextLayout
    {
        private readonly TextPainter _painter;
        private LineCaretMetrics? _endOfTextCaretMetrics;

        public PainterTextLayout(Paragraph paragraph, TextDirection writingDirection, TextPainter painter)
        {
            Paragraph = paragraph;
            WritingDirection = writingDirection;
            _painter = painter;
        }

        // This field is not final because the owner TextPainter could create a new Paragraph with the
        // exact same text layout (for example, when only the color of the text is changed).
        //
        // The creator of this layout is responsible for disposing this object.
        public Paragraph Paragraph { get; set; }

        // TextDirection.ltr is the default, as in ParagraphStyle.
        public TextDirection WritingDirection { get; }

        public double Width => Paragraph.Width;

        public double Height => Paragraph.Height;

        public double MinIntrinsicLineExtent => Paragraph.MinIntrinsicWidth;

        public double MaxIntrinsicLineExtent => Paragraph.MaxIntrinsicWidth;

        public double LongestLine => Paragraph.LongestLine;

        public double GetDistanceToBaseline(TextBaseline baseline)
        {
            return baseline == TextBaseline.Alphabetic ? Paragraph.AlphabeticBaseline : Paragraph.IdeographicBaseline;
        }

        public LineCaretMetrics EndOfTextCaretMetrics =>
            _endOfTextCaretMetrics ??= ComputeEndOfTextCaretAnchorOffset();

        // Computes the LineCaretMetrics for the end of text.
        private LineCaretMetrics ComputeEndOfTextCaretAnchorOffset()
        {
            string rawString = _painter.PlainText;
            int lastLineIndex = Paragraph.NumberOfLines - 1;
            DebugAssert(lastLineIndex >= 0, "lastLineIndex >= 0");
            LineMetrics lineMetrics = Paragraph.GetLineMetricsAt(lastLineIndex)!.Value;
            // Trailing white spaces don't contribute to the line width and thus require special
            // handling when they're present.
            // Luckily they have the same bidi embedding level as the paragraph as per
            // https://unicode.org/reports/tr9/#L1, so we can anchor the caret to the last logical
            // trailing space.
            char lastCodeUnit = rawString[^1];
            bool hasTrailingSpaces = lastCodeUnit switch
            {
                // A horizontal tab is a whitespace but it doesn't match the Space_Separator category.
                '\u0009' => true,
                // Non-breaking spaces are Space_Separators but they're not trailing white spaces.
                '\u00A0' or '\u2007' or '\u202F' => false,
                _ => UnicodeText.IsSpaceSeparator(lastCodeUnit),
            };

            double baseline = lineMetrics.Baseline;
            GlyphInfo? lastGlyph = hasTrailingSpaces ? Paragraph.GetGlyphInfoAt(rawString.Length - 1) : null;
            double dx;
            double height;
            if (hasTrailingSpaces && lastGlyph is not null)
            {
                Rect glyphBounds = lastGlyph.GraphemeClusterLayoutBounds;
                DebugAssert(glyphBounds.Width != 0.0 || glyphBounds.Height != 0.0, "!glyphBounds.isEmpty");
                dx = WritingDirection == UI.TextDirection.Ltr ? glyphBounds.Right : glyphBounds.Left;
                height = glyphBounds.Height;
            }
            else
            {
                dx = WritingDirection == UI.TextDirection.Ltr
                    ? lineMetrics.Left + lineMetrics.Width
                    : lineMetrics.Left;
                height = lineMetrics.Height;
            }

            // Anchor the caret to the baseline of the last line.
            return new LineCaretMetrics(new Point(dx, baseline), WritingDirection, height);
        }

        public double ContentWidthFor(double minWidth, double maxWidth, TextWidthBasis widthBasis)
        {
            return widthBasis switch
            {
                TextWidthBasis.LongestLine => ClampDouble(LongestLine, minWidth, maxWidth),
                _ => ClampDouble(MaxIntrinsicLineExtent, minWidth, maxWidth),
            };
        }
    }

    // This class stores the current text layout and the corresponding paintOffset and contentWidth,
    // as well as some cached text metrics values that depends on the current text layout, which
    // will be invalidated as soon as the text layout is invalidated.
    private sealed class LayoutCacheWithOffset
    {
        private IReadOnlyList<TextBox>? _cachedInlinePlaceholderBoxes;
        private IReadOnlyList<LineMetrics>? _cachedLineMetrics;

        public LayoutCacheWithOffset(
            PainterTextLayout layout,
            double textAlignment,
            double layoutMaxWidth,
            double contentWidth)
        {
            DebugAssert(textAlignment >= 0.0 && textAlignment <= 1.0, "textAlignment >= 0.0 && textAlignment <= 1.0");
            DebugAssert(!double.IsNaN(layoutMaxWidth), "!layoutMaxWidth.isNaN");
            DebugAssert(!double.IsNaN(contentWidth), "!contentWidth.isNaN");
            Layout = layout;
            TextAlignment = textAlignment;
            LayoutMaxWidth = layoutMaxWidth;
            ContentWidth = contentWidth;
        }

        public PainterTextLayout Layout { get; }

        // The input width used to lay out the paragraph.
        public double LayoutMaxWidth { get; }

        // The content width the text painter should report in TextPainter.width. This is also used
        // to compute paintOffset.
        public double ContentWidth { get; private set; }

        // The effective text alignment in the TextPainter's canvas. The value is within the [0, 1]
        // interval: 0 for left aligned and 1 for right aligned.
        public double TextAlignment { get; }

        // The paintOffset of the paragraph in the TextPainter's canvas.
        //
        // It's coordinate values are guaranteed to not be NaN.
        public Point PaintOffset
        {
            get
            {
                if (TextAlignment == 0)
                {
                    return default;
                }

                if (!double.IsFinite(Paragraph.Width))
                {
                    return new Point(double.PositiveInfinity, 0.0);
                }

                double dx = TextAlignment * (ContentWidth - Paragraph.Width);
                DebugAssert(!double.IsNaN(dx), "!dx.isNaN");
                return new Point(dx, 0);
            }
        }

        public Paragraph Paragraph => Layout.Paragraph;

        // Try to resize the contentWidth to fit the new input constraints, by just adjusting the
        // paint offset (so no line-breaking changes needed).
        //
        // Returns false if the new constraints require the text layout library to re-compute the line
        // breaks.
        public bool ResizeToFit(double minWidth, double maxWidth, TextWidthBasis widthBasis)
        {
            DebugAssert(double.IsFinite(Layout.MaxIntrinsicLineExtent), "layout.maxIntrinsicLineExtent.isFinite");
            DebugAssert(minWidth <= maxWidth, "minWidth <= maxWidth");
            // The assumption here is that if a Paragraph's width is already >= its maxIntrinsicWidth,
            // further increasing the input width does not change its layout (but may change the paint
            // offset if it's not left-aligned). This is true even for TextAlign.justify: when
            // width >= maxIntrinsicWidth TextAlign.justify will behave exactly the same as
            // TextAlign.start.
            //
            // An exception to this is when the text is not left-aligned, and the input width is
            // double.infinity. Since the resulting Paragraph will have a width of double.infinity and
            // the paint offset will be infinite as well (the painter "stretches" the text box to the
            // infinite width).
            if (maxWidth == ContentWidth && minWidth == ContentWidth)
            {
                ContentWidth = Layout.ContentWidthFor(minWidth, maxWidth, widthBasis);
                return true;
            }

            // Special case:
            // When the text is not left-aligned, the paragraph has infinite width and the layout is
            // laid out with double.infinity, the paint offset is infinite. Since minWidth is finite
            // here the paragraph has to be laid out again.
            if (!double.IsFinite(PaintOffset.X) && !double.IsFinite(Paragraph.Width) && double.IsFinite(minWidth))
            {
                DebugAssert(PaintOffset.X == double.PositiveInfinity, "paintOffset.dx == double.infinity");
                DebugAssert(Paragraph.Width == double.PositiveInfinity, "paragraph.width == double.infinity");
                return false;
            }

            double maxIntrinsicWidth = Paragraph.MaxIntrinsicWidth;
            // Skip line breaking if the input width remains the same, or there will be no soft breaks.
            bool skipLineBreaking = maxWidth == LayoutMaxWidth
                                    || ((Paragraph.Width - maxIntrinsicWidth) > -Constants.PrecisionErrorTolerance
                                        && (maxWidth - maxIntrinsicWidth) > -Constants.PrecisionErrorTolerance);
            if (skipLineBreaking)
            {
                // Adjust the content width in case the TextWidthBasis changed.
                ContentWidth = Layout.ContentWidthFor(minWidth, maxWidth, widthBasis);
                return true;
            }

            return false;
        }

        // ---- Cached Values ----

        public IReadOnlyList<TextBox> InlinePlaceholderBoxes =>
            _cachedInlinePlaceholderBoxes ??= Paragraph.GetBoxesForPlaceholders();

        public IReadOnlyList<LineMetrics> LineMetrics =>
            _cachedLineMetrics ??= Paragraph.ComputeLineMetrics();

        // Holds the TextPosition the last caret metrics were computed with. When new caret metrics
        // are requested for the same position, the cached value is returned.
        public int? PreviousCaretPositionKey { get; set; }
    }

    /// The caret position and height of a text position within a line. Dart's `_LineCaretMetrics`.
    private readonly record struct LineCaretMetrics(Point Offset, TextDirection WritingDirection, double Height)
    {
        public LineCaretMetrics Shift(Point offset)
        {
            return offset == default
                ? this
                : this with { Offset = new Point(offset.X + Offset.X, offset.Y + Offset.Y) };
        }
    }
}
