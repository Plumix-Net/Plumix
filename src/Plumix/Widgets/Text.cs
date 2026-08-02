using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/text.dart (approximate)

namespace Plumix.Widgets;

public sealed class Text : LeafRenderObjectWidget
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
        TextDecorationCollection? textDecorations = null) : base(key)
    {
        if (maxLines is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLines), "Max lines must be greater than zero.");
        }

        Data = data;
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
    }

    public string Data { get; }

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

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        var defaultTextStyle = DefaultTextStyle.MaybeOf(context);
        var selection = SelectionContainer.MaybeOf(context);
        var paragraph = new RenderParagraph(Data)
        {
            TextAlign = TextAlign ?? defaultTextStyle?.TextAlign ?? Plumix.UI.TextAlign.Start,
            SoftWrap = SoftWrap ?? defaultTextStyle?.SoftWrap ?? true,
            MaxLines = defaultTextStyle?.MaxLines ?? MaxLines,
            Overflow = Overflow ?? defaultTextStyle?.Overflow ?? TextOverflow.Clip,
            TextDirection = TextDirection ?? Directionality.Of(context),
            TextWidthBasis = TextWidthBasis ?? defaultTextStyle?.TextWidthBasis ?? Plumix.UI.TextWidthBasis.Parent,
            TextHeightBehavior = TextHeightBehavior ?? defaultTextStyle?.TextHeightBehavior,
            TextDecorations = TextDecorations,
            SelectionRegistrar = selection?.Registrar,
            SelectionEnabled = selection?.Enabled ?? false,
            SelectionColor = selection?.SelectionColor ?? default,
            CursorColor = selection?.CursorColor ?? default,
            ShowCursor = selection?.ShowCursor ?? false,
            CursorWidth = selection?.CursorWidth ?? 2.0,
            CursorHeight = selection?.CursorHeight,
        };

        ApplyResolvedTextStyle(context, paragraph);
        return paragraph;
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var paragraph = (RenderParagraph)renderObject;
        paragraph.Text = Data;
        ApplyResolvedTextStyle(context, paragraph);
        var defaultTextStyle = DefaultTextStyle.MaybeOf(context);
        paragraph.TextAlign = TextAlign ?? defaultTextStyle?.TextAlign ?? Plumix.UI.TextAlign.Start;
        paragraph.SoftWrap = SoftWrap ?? defaultTextStyle?.SoftWrap ?? true;
        paragraph.MaxLines = defaultTextStyle?.MaxLines ?? MaxLines;
        paragraph.Overflow = Overflow ?? defaultTextStyle?.Overflow ?? TextOverflow.Clip;
        paragraph.TextDirection = TextDirection ?? Directionality.Of(context);
        paragraph.TextWidthBasis = TextWidthBasis
            ?? defaultTextStyle?.TextWidthBasis
            ?? Plumix.UI.TextWidthBasis.Parent;
        paragraph.TextHeightBehavior = TextHeightBehavior ?? defaultTextStyle?.TextHeightBehavior;
        paragraph.TextDecorations = TextDecorations;
        var selection = SelectionContainer.MaybeOf(context);
        paragraph.SelectionRegistrar = selection?.Registrar;
        paragraph.SelectionEnabled = selection?.Enabled ?? false;
        if (selection is not null)
        {
            paragraph.SelectionColor = selection.SelectionColor;
            paragraph.CursorColor = selection.CursorColor;
            paragraph.ShowCursor = selection.ShowCursor;
            paragraph.CursorWidth = selection.CursorWidth;
            paragraph.CursorHeight = selection.CursorHeight;
        }
        else
        {
            paragraph.ShowCursor = false;
        }
    }

    private void ApplyResolvedTextStyle(BuildContext context, RenderParagraph paragraph)
    {
        var defaultTextStyle = DefaultTextStyle.Of(context);
        double textScaleFactor = MediaQuery.MaybeTextScaleFactorOf(context) ?? 1.0;
        paragraph.FontFamily = FontFamily ?? defaultTextStyle.FontFamily ?? Avalonia.Media.FontFamily.Default;
        paragraph.FontSize = (FontSize ?? defaultTextStyle.FontSize ?? 14) * textScaleFactor;
        paragraph.Foreground = new SolidColorBrush(Color ?? defaultTextStyle.Color ?? Colors.Black);
        paragraph.FontWeight = FontWeight ?? defaultTextStyle.FontWeight ?? Avalonia.Media.FontWeight.Normal;
        paragraph.FontStyle = FontStyle ?? defaultTextStyle.FontStyle ?? Avalonia.Media.FontStyle.Normal;
        paragraph.Height = Height ?? defaultTextStyle.Height;
        paragraph.LetterSpacing = LetterSpacing ?? defaultTextStyle.LetterSpacing ?? 0;
    }
}
