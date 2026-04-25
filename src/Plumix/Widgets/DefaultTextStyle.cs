using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;

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
        Key? key = null) : base(key)
    {
        Style = style;
        Child = child;
    }

    public TextStyle Style { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((DefaultTextStyle)oldWidget).Style, Style);
    }

    public static TextStyle Of(BuildContext context)
    {
        return context.DependOnInherited<DefaultTextStyle>()?.Style ?? TextStyle.Fallback;
    }
}
