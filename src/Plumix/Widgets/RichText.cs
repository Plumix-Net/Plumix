using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart

namespace Plumix.Widgets;

/// A paragraph of rich text.
///
/// The [RichText] widget displays text that uses multiple different styles. The
/// text to display is described using a tree of [TextSpan] objects, each of
/// which has an associated style that is used for that subtree. The text might
/// break across multiple lines or might all be displayed on the same line
/// depending on the layout constraints.
///
/// Text displayed in a [RichText] widget must be explicitly styled. When
/// picking which style to use, consider using [DefaultTextStyle.Of] the current
/// [BuildContext] to provide defaults.
///
/// See also:
///
///  * [TextStyle], which discusses how to style text.
///  * [TextSpan], which is used to describe the text in a paragraph.
///  * [Text], which automatically applies the ambient styles described by a
///    [DefaultTextStyle] to a single string.
public sealed class RichText : MultiChildRenderObjectWidget
{
    /// Creates a paragraph of rich text.
    ///
    /// The [MaxLines] property may be null (and indeed defaults to null), but if
    /// it is not null, it must be greater than zero.
    public RichText(
        InlineSpan text,
        TextAlign textAlign = TextAlign.Start,
        TextDirection? textDirection = null,
        bool softWrap = true,
        TextOverflow overflow = TextOverflow.Clip,
        TextScaler? textScaler = null,
        double textScaleFactor = 1.0,
        int? maxLines = null,
        string? locale = null,
        TextWidthBasis textWidthBasis = TextWidthBasis.Parent,
        TextHeightBehavior? textHeightBehavior = null,
        ITextSelectionRegistrar? selectionRegistrar = null,
        Avalonia.Media.Color? selectionColor = null,
        Key? key = null)
        : base(WidgetSpan.ExtractFromInlineSpan(text, ResolveTextScaler(textScaler, textScaleFactor)), key)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (maxLines is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLines), "Max lines must be greater than zero.");
        }

        if (selectionRegistrar is not null && selectionColor is null)
        {
            throw new ArgumentException(
                "A selection registrar requires a selection color.",
                nameof(selectionColor));
        }

        Text = text;
        TextAlign = textAlign;
        TextDirection = textDirection;
        SoftWrap = softWrap;
        Overflow = overflow;
        TextScaler = ResolveTextScaler(textScaler, textScaleFactor);
        MaxLines = maxLines;
        Locale = locale;
        TextWidthBasis = textWidthBasis;
        TextHeightBehavior = textHeightBehavior;
        SelectionRegistrar = selectionRegistrar;
        SelectionColor = selectionColor;
    }

    internal RichText(
        InlineSpan text,
        SelectionContainer? selectionConfiguration,
        TextAlign textAlign = TextAlign.Start,
        TextDirection? textDirection = null,
        bool softWrap = true,
        TextOverflow overflow = TextOverflow.Clip,
        TextScaler? textScaler = null,
        double textScaleFactor = 1.0,
        int? maxLines = null,
        string? locale = null,
        TextWidthBasis textWidthBasis = TextWidthBasis.Parent,
        TextHeightBehavior? textHeightBehavior = null,
        Avalonia.Media.Color? selectionColor = null,
        Key? key = null)
        : this(
            text,
            textAlign,
            textDirection,
            softWrap,
            overflow,
            textScaler,
            textScaleFactor,
            maxLines,
            locale,
            textWidthBasis,
            textHeightBehavior,
            selectionConfiguration?.Registrar,
            selectionColor,
            key)
    {
        SelectionConfiguration = selectionConfiguration;
    }

    /// The text to display in this widget.
    public InlineSpan Text { get; }

    /// How the text should be aligned horizontally.
    public TextAlign TextAlign { get; }

    /// The directionality of the text.
    ///
    /// This decides how [TextAlign.Start], [TextAlign.End] and [TextAlign.Justify]
    /// values are interpreted. Defaults to the ambient [Directionality], if any.
    public TextDirection? TextDirection { get; }

    /// Whether the text should break at soft line breaks.
    public bool SoftWrap { get; }

    /// How visual overflow should be handled.
    public TextOverflow Overflow { get; }

    /// The font scaling strategy to use when laying the text out.
    public TextScaler TextScaler { get; }

    /// The deprecated linear font scale compatibility value.
    public double TextScaleFactor => TextScaler.TextScaleFactor;

    /// An optional maximum number of lines for the text to span.
    public int? MaxLines { get; }

    /// Used to select a font when the same Unicode character can be rendered
    /// differently, depending on the locale.
    public string? Locale { get; }

    /// Defines how to measure the width of the rendered text.
    public TextWidthBasis TextWidthBasis { get; }

    /// Defines how to apply [TextStyle.Height] over and under text.
    public TextHeightBehavior? TextHeightBehavior { get; }

    /// The [ITextSelectionRegistrar] this rich text subscribes to.
    public ITextSelectionRegistrar? SelectionRegistrar { get; }

    /// The color to use when painting the selection.
    public Avalonia.Media.Color? SelectionColor { get; }

    /// The ambient selection configuration this paragraph participates in.
    ///
    /// Flutter carries selection state entirely through `SelectionRegistrar`;
    /// Plumix's selection stack additionally owns the caret configuration, so the
    /// container is passed through here (see `docs/ai/DIVERGENCES.md`).
    internal SelectionContainer? SelectionConfiguration { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        var paragraph = new RenderParagraph(Text)
        {
            TextAlign = TextAlign,
            TextDirection = TextDirection ?? Directionality.Of(context),
            SoftWrap = SoftWrap,
            Overflow = Overflow,
            TextScaler = TextScaler,
            MaxLines = MaxLines,
            Locale = Locale,
            TextWidthBasis = TextWidthBasis,
            TextHeightBehavior = TextHeightBehavior,
            SelectionRegistrar = SelectionRegistrar,
        };

        ApplySelectionConfiguration(paragraph);
        return paragraph;
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var paragraph = (RenderParagraph)renderObject;
        paragraph.Text = Text;
        paragraph.TextAlign = TextAlign;
        paragraph.TextDirection = TextDirection ?? Directionality.Of(context);
        paragraph.SoftWrap = SoftWrap;
        paragraph.Overflow = Overflow;
        paragraph.TextScaler = TextScaler;
        paragraph.MaxLines = MaxLines;
        paragraph.Locale = Locale;
        paragraph.TextWidthBasis = TextWidthBasis;
        paragraph.TextHeightBehavior = TextHeightBehavior;
        paragraph.SelectionRegistrar = SelectionRegistrar;
        ApplySelectionConfiguration(paragraph);
    }

    private void ApplySelectionConfiguration(RenderParagraph paragraph)
    {
        if (SelectionColor is { } color)
        {
            paragraph.SelectionColor = color;
        }

        SelectionContainer? selection = SelectionConfiguration;
        paragraph.SelectionEnabled = selection?.Enabled ?? false;
        if (selection is null)
        {
            paragraph.ShowCursor = false;
            return;
        }

        paragraph.CursorColor = selection.CursorColor;
        paragraph.ShowCursor = selection.ShowCursor;
        paragraph.CursorWidth = selection.CursorWidth;
        paragraph.CursorHeight = selection.CursorHeight;
    }

    private static TextScaler ResolveTextScaler(TextScaler? textScaler, double textScaleFactor)
    {
        if (textScaleFactor != 1.0)
        {
            if (textScaler is not null && !ReferenceEquals(textScaler, Painting.TextScaler.NoScaling))
            {
                throw new ArgumentException(
                    "TextScaleFactor cannot be specified with a non-default TextScaler.",
                    nameof(textScaleFactor));
            }

            return Painting.TextScaler.Linear(textScaleFactor);
        }

        return textScaler ?? Painting.TextScaler.NoScaling;
    }
}
