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
        var defaultTextStyle = DefaultTextStyle.MaybeOf(context);
        var selection = SelectionContainer.MaybeOf(context);
        var paragraph = new RenderParagraph(Data)
        {
            TextAlign = TextAlign,
            SoftWrap = defaultTextStyle?.SoftWrap ?? SoftWrap,
            MaxLines = MaxLines,
            Overflow = defaultTextStyle?.Overflow ?? Overflow,
            TextDirection = TextDirection,
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
        paragraph.TextAlign = TextAlign;
        var defaultTextStyle = DefaultTextStyle.MaybeOf(context);
        paragraph.SoftWrap = defaultTextStyle?.SoftWrap ?? SoftWrap;
        paragraph.MaxLines = MaxLines;
        paragraph.Overflow = defaultTextStyle?.Overflow ?? Overflow;
        paragraph.TextDirection = TextDirection;
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
