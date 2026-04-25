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
        TextAlign textAlign = TextAlign.Start,
        bool softWrap = true,
        int? maxLines = null,
        TextOverflow overflow = TextOverflow.Clip,
        TextDirection textDirection = TextDirection.Ltr,
        Key? key = null) : base(key)
    {
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
    }

    public string Data { get; }

    public double? FontSize { get; }

    public Color? Color { get; }

    public FontWeight? FontWeight { get; }

    public FontStyle? FontStyle { get; }

    public FontFamily? FontFamily { get; }

    public double? Height { get; }

    public double? LetterSpacing { get; }

    public TextAlign TextAlign { get; }

    public bool SoftWrap { get; }

    public int? MaxLines { get; }

    public TextOverflow Overflow { get; }

    public TextDirection TextDirection { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        var paragraph = new RenderParagraph(Data)
        {
            TextAlign = TextAlign,
            SoftWrap = SoftWrap,
            MaxLines = MaxLines,
            Overflow = Overflow,
            TextDirection = TextDirection
        };

        ApplyResolvedTextStyle(context, paragraph);
        return paragraph;
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var paragraph = (RenderParagraph)renderObject;
        paragraph.Text = Data;
        ApplyResolvedTextStyle(context, paragraph);
        paragraph.TextAlign = TextAlign;
        paragraph.SoftWrap = SoftWrap;
        paragraph.MaxLines = MaxLines;
        paragraph.Overflow = Overflow;
        paragraph.TextDirection = TextDirection;
    }

    private void ApplyResolvedTextStyle(BuildContext context, RenderParagraph paragraph)
    {
        var defaultTextStyle = DefaultTextStyle.Of(context);
        paragraph.FontFamily = FontFamily ?? defaultTextStyle.FontFamily ?? Avalonia.Media.FontFamily.Default;
        paragraph.FontSize = FontSize ?? defaultTextStyle.FontSize ?? 14;
        paragraph.Foreground = new SolidColorBrush(Color ?? defaultTextStyle.Color ?? Colors.Black);
        paragraph.FontWeight = FontWeight ?? defaultTextStyle.FontWeight ?? Avalonia.Media.FontWeight.Normal;
        paragraph.FontStyle = FontStyle ?? defaultTextStyle.FontStyle ?? Avalonia.Media.FontStyle.Normal;
        paragraph.Height = Height ?? defaultTextStyle.Height;
        paragraph.LetterSpacing = LetterSpacing ?? defaultTextStyle.LetterSpacing ?? 0;
    }
}
