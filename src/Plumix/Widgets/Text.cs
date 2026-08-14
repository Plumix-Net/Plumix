using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/text.dart

namespace Plumix.Widgets;

/// A run of text with a single style.
///
/// The [Text] widget displays a string of text with single style. The string
/// might break across multiple lines or might all be displayed on the same line
/// depending on the layout constraints.
///
/// The [Style] argument is optional. When omitted, the text will use the style
/// from the closest enclosing [DefaultTextStyle].
///
/// Using the [Text.Rich] constructor, the [Text] widget can display a paragraph
/// with differently styled [TextSpan]s and inline [WidgetSpan]s.
public sealed class Text : StatelessWidget
{
    public Text(
        string data,
        double? fontSize = null,
        Color? color = null,
        FontWeight? fontWeight = null,
        FontStyle? fontStyle = null,
        FontFamily? fontFamily = null,
        double? height = null,
        double? letterSpacing = null,
        TextAlign? textAlign = null,
        bool? softWrap = null,
        int? maxLines = null,
        TextOverflow? overflow = null,
        TextDirection? textDirection = null,
        TextWidthBasis? textWidthBasis = null,
        TextHeightBehavior? textHeightBehavior = null,
        Key? key = null,
        TextDecorationCollection? textDecorations = null,
        TextStyle? style = null,
        string? semanticsLabel = null,
        double? textScaleFactor = null,
        TextScaler? textScaler = null,
        string? locale = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (maxLines is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLines), "Max lines must be greater than zero.");
        }

        if (textScaleFactor is not null && textScaler is not null)
        {
            throw new ArgumentException(
                "TextScaleFactor and TextScaler cannot both be specified.",
                nameof(textScaleFactor));
        }

        Data = data;
        TextSpan = null;
        FontSize = fontSize;
        Color = color;
        FontWeight = fontWeight;
        FontStyle = fontStyle;
        FontFamily = fontFamily;
        Height = height;
        LetterSpacing = letterSpacing;
        TextAlign = textAlign;
        SoftWrap = softWrap;
        MaxLines = maxLines;
        Overflow = overflow;
        TextDirection = textDirection;
        TextWidthBasis = textWidthBasis;
        TextHeightBehavior = textHeightBehavior;
        TextDecorations = textDecorations;
        Style = style;
        SemanticsLabel = semanticsLabel;
        TextScaleFactor = textScaleFactor;
        TextScaler = textScaler;
        Locale = locale;
    }

    private Text(
        InlineSpan textSpan,
        TextStyle? style,
        TextAlign? textAlign,
        bool? softWrap,
        int? maxLines,
        TextOverflow? overflow,
        TextDirection? textDirection,
        TextWidthBasis? textWidthBasis,
        TextHeightBehavior? textHeightBehavior,
        string? semanticsLabel,
        double? textScaleFactor,
        TextScaler? textScaler,
        string? locale,
        Key? key) : base(key)
    {
        ArgumentNullException.ThrowIfNull(textSpan);
        if (maxLines is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLines), "Max lines must be greater than zero.");
        }


        if (textScaleFactor is not null && textScaler is not null)
        {
            throw new ArgumentException(
                "TextScaleFactor and TextScaler cannot both be specified.",
                nameof(textScaleFactor));
        }

        Data = null;
        TextSpan = textSpan;
        Style = style;
        TextAlign = textAlign;
        SoftWrap = softWrap;
        MaxLines = maxLines;
        Overflow = overflow;
        TextDirection = textDirection;
        TextWidthBasis = textWidthBasis;
        TextHeightBehavior = textHeightBehavior;
        SemanticsLabel = semanticsLabel;
        TextScaleFactor = textScaleFactor;
        TextScaler = textScaler;
        Locale = locale;
    }

    /// Creates a text widget with an [InlineSpan].
    ///
    /// The following subclasses of [InlineSpan] may be used to build rich text:
    ///
    /// * [TextSpan]s define text and children [InlineSpan]s.
    /// * [WidgetSpan]s define embedded inline widgets.
    public static Text Rich(
        InlineSpan textSpan,
        TextStyle? style = null,
        TextAlign? textAlign = null,
        bool? softWrap = null,
        int? maxLines = null,
        TextOverflow? overflow = null,
        TextDirection? textDirection = null,
        TextWidthBasis? textWidthBasis = null,
        TextHeightBehavior? textHeightBehavior = null,
        string? semanticsLabel = null,
        double? textScaleFactor = null,
        TextScaler? textScaler = null,
        string? locale = null,
        Key? key = null)
    {
        return new Text(
            textSpan,
            style,
            textAlign,
            softWrap,
            maxLines,
            overflow,
            textDirection,
            textWidthBasis,
            textHeightBehavior,
            semanticsLabel,
            textScaleFactor,
            textScaler,
            locale,
            key);
    }

    /// The text to display.
    ///
    /// This will be null if a [TextSpan] is provided instead.
    public string? Data { get; }

    /// The text to display as an [InlineSpan].
    ///
    /// This will be null if [Data] is provided instead.
    public InlineSpan? TextSpan { get; }

    /// If non-null, the style to use for this text.
    public TextStyle? Style { get; }

    public double? FontSize { get; }

    public Color? Color { get; }

    public FontWeight? FontWeight { get; }

    public FontStyle? FontStyle { get; }

    public FontFamily? FontFamily { get; }

    public double? Height { get; }

    public double? LetterSpacing { get; }

    public TextAlign? TextAlign { get; }

    public bool? SoftWrap { get; }

    public int? MaxLines { get; }

    public TextOverflow? Overflow { get; }

    public TextDirection? TextDirection { get; }

    public TextWidthBasis? TextWidthBasis { get; }

    public TextHeightBehavior? TextHeightBehavior { get; }

    public TextDecorationCollection? TextDecorations { get; }

    /// An alternative semantics label for this text.
    public string? SemanticsLabel { get; }

    /// The font scaling strategy to use when laying the text out.
    public TextScaler? TextScaler { get; }

    /// The deprecated linear font scale compatibility value.
    public double? TextScaleFactor { get; }

    /// Used to select a font when the same Unicode character can be rendered
    /// differently, depending on the locale.
    public string? Locale { get; }

    private UI.TextDecoration? ResolveDecoration()
    {
        if (TextDecorations is not { Count: > 0 } decorations)
        {
            return null;
        }

        UI.TextDecoration resolved = UI.TextDecoration.None;
        foreach (Avalonia.Media.TextDecoration decoration in decorations)
        {
            resolved |= decoration.Location switch
            {
                TextDecorationLocation.Overline => UI.TextDecoration.Overline,
                TextDecorationLocation.Strikethrough => UI.TextDecoration.LineThrough,
                _ => UI.TextDecoration.Underline,
            };
        }

        return resolved;
    }

    public override Widget Build(BuildContext context)
    {
        DefaultTextStyle? ambient = DefaultTextStyle.MaybeOf(context);
        TextStyle effectiveTextStyle = ResolveEffectiveStyle(context);
        var effectiveTextSpan = new TextSpan(
            style: effectiveTextStyle,
            text: Data,
            locale: Locale,
            children: TextSpan is null ? null : [TextSpan]);
        ISelectionRegistrar? registrar = SelectionContainer.MaybeOf(context);
        DefaultSelectionStyle selectionStyle = DefaultSelectionStyle.Of(context);
        TextScaler effectiveTextScaler = TextScaler
                                         ?? (TextScaleFactor is { } textScaleFactor
                                             ? Painting.TextScaler.Linear(textScaleFactor)
                                             : MediaQuery.TextScalerOf(context));

        Widget result = new RichText(
            text: effectiveTextSpan,
            selectionConfiguration: registrar,
            textAlign: TextAlign ?? ambient?.TextAlign ?? UI.TextAlign.Start,
            textDirection: TextDirection,
            softWrap: SoftWrap ?? ambient?.SoftWrap ?? true,
            overflow: Overflow ?? ambient?.Overflow ?? TextOverflow.Clip,
            textScaler: effectiveTextScaler,
            maxLines: ambient?.MaxLines ?? MaxLines,
            locale: Locale,
            textWidthBasis: TextWidthBasis ?? ambient?.TextWidthBasis ?? UI.TextWidthBasis.Parent,
            textHeightBehavior: TextHeightBehavior ?? ambient?.TextHeightBehavior,
            selectionColor: registrar is null
                ? null
                : selectionStyle.SelectionColor ?? DefaultSelectionStyle.DefaultColor);

        if (registrar is not null)
        {
            result = new MouseRegion(
                cursor: selectionStyle.MouseCursor ?? SystemMouseCursors.Text,
                child: result);
        }

        if (SemanticsLabel is not null)
        {
            result = new Semantics(
                label: SemanticsLabel,
                child: new ExcludeSemantics(child: result));
        }

        return result;
    }

    private TextStyle ResolveEffectiveStyle(BuildContext context)
    {
        TextStyle ambientStyle = DefaultTextStyle.Of(context);
        TextStyle inline = new(
            FontFamily: FontFamily,
            FontSize: FontSize,
            Color: Color,
            FontWeight: FontWeight,
            FontStyle: FontStyle,
            Height: Height,
            LetterSpacing: LetterSpacing);
        TextStyle widgetStyle = Style is null ? inline : Style.Merge(inline);
        TextStyle effective = widgetStyle.Inherit
            ? ambientStyle.Merge(widgetStyle)
            : widgetStyle;
        if (effective.FontFamily is null && ambientStyle.FontFamilyFallback is { Count: > 0 } fallback)
        {
            var familyNames = new List<string> { Avalonia.Media.FontFamily.Default.Name };
            familyNames.AddRange(fallback);
            effective = effective with { FontFamily = new FontFamily(string.Join(',', familyNames)) };
        }

        return effective with
        {
            FontSize = effective.FontSize ?? TextDefaults.DefaultFontSize,
            Color = effective.Color ?? Colors.Black,
            Decoration = ResolveDecoration() ?? effective.Decoration,
        };
    }
}
