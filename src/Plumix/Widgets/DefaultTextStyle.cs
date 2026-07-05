using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/text.dart (approximate)

public sealed record TextStyle(
    FontFamily? FontFamily = null,
    double? FontSize = null,
    Color? Color = null,
    FontWeight? FontWeight = null,
    FontStyle? FontStyle = null,
    double? Height = null,
    double? LetterSpacing = null)
{
    public TextStyle CopyWith(
        FontFamily? fontFamily = null,
        double? fontSize = null,
        Color? color = null,
        FontWeight? fontWeight = null,
        FontStyle? fontStyle = null,
        double? height = null,
        double? letterSpacing = null)
    {
        return new TextStyle(
            FontFamily: fontFamily ?? FontFamily,
            FontSize: fontSize ?? FontSize,
            Color: color ?? Color,
            FontWeight: fontWeight ?? FontWeight,
            FontStyle: fontStyle ?? FontStyle,
            Height: height ?? Height,
            LetterSpacing: letterSpacing ?? LetterSpacing);
    }

    internal static TextStyle Fallback { get; } = new(
        FontFamily: Avalonia.Media.FontFamily.Default,
        FontSize: 14,
        Color: Colors.Black,
        FontWeight: Avalonia.Media.FontWeight.Normal,
        FontStyle: Avalonia.Media.FontStyle.Normal);
}

public sealed class DefaultTextStyle : InheritedWidget
{
    public DefaultTextStyle(
        TextStyle style,
        Widget child,
        Key? key = null,
        bool? softWrap = null,
        TextOverflow? overflow = null) : base(key)
    {
        Style = style;
        Child = child;
        SoftWrap = softWrap;
        Overflow = overflow;
    }

    public TextStyle Style { get; }

    public Widget Child { get; }

    public bool? SoftWrap { get; }

    public TextOverflow? Overflow { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldStyle = (DefaultTextStyle)oldWidget;
        return !Equals(oldStyle.Style, Style)
               || oldStyle.SoftWrap != SoftWrap
               || oldStyle.Overflow != Overflow;
    }

    public static TextStyle Of(BuildContext context)
    {
        return context.DependOnInherited<DefaultTextStyle>()?.Style ?? TextStyle.Fallback;
    }

    internal static DefaultTextStyle? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<DefaultTextStyle>();
    }
}
