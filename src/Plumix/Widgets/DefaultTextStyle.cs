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

    public static TextStyle Lerp(TextStyle a, TextStyle b, double t)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        t = Math.Clamp(t, 0.0, 1.0);

        return new TextStyle(
            FontFamily: t < 0.5 ? a.FontFamily : b.FontFamily,
            FontSize: LerpNullable(a.FontSize, b.FontSize, t),
            Color: LerpColor(a.Color, b.Color, t),
            FontWeight: LerpFontWeight(a.FontWeight, b.FontWeight, t),
            FontStyle: t < 0.5 ? a.FontStyle : b.FontStyle,
            Height: LerpNullable(a.Height, b.Height, t),
            LetterSpacing: LerpNullable(a.LetterSpacing, b.LetterSpacing, t));
    }

    private static double? LerpNullable(double? a, double? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        double from = a ?? b!.Value;
        double to = b ?? a!.Value;
        return from + ((to - from) * t);
    }

    private static Color? LerpColor(Color? a, Color? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        Color from = a ?? Avalonia.Media.Color.FromArgb(0, b!.Value.R, b.Value.G, b.Value.B);
        Color to = b ?? Avalonia.Media.Color.FromArgb(0, a!.Value.R, a.Value.G, a.Value.B);
        return new ColorTween().Evaluate(t, from, to);
    }

    private static FontWeight? LerpFontWeight(FontWeight? a, FontWeight? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        int from = WeightValue(a ?? Avalonia.Media.FontWeight.Normal);
        int to = WeightValue(b ?? Avalonia.Media.FontWeight.Normal);
        int value = (int)Math.Round(from + ((to - from) * t));
        return WeightFromValue(value);
    }

    private static int WeightValue(FontWeight value)
    {
        if (value == Avalonia.Media.FontWeight.Thin) return 100;
        if (value == Avalonia.Media.FontWeight.ExtraLight) return 200;
        if (value == Avalonia.Media.FontWeight.Light) return 300;
        if (value == Avalonia.Media.FontWeight.SemiLight) return 350;
        if (value == Avalonia.Media.FontWeight.Medium) return 500;
        if (value == Avalonia.Media.FontWeight.DemiBold) return 600;
        if (value == Avalonia.Media.FontWeight.Bold) return 700;
        if (value == Avalonia.Media.FontWeight.ExtraBold) return 800;
        if (value == Avalonia.Media.FontWeight.Black) return 900;
        if (value == Avalonia.Media.FontWeight.ExtraBlack) return 950;
        return 400;
    }

    private static FontWeight WeightFromValue(int value)
    {
        if (value < 150) return Avalonia.Media.FontWeight.Thin;
        if (value < 250) return Avalonia.Media.FontWeight.ExtraLight;
        if (value < 325) return Avalonia.Media.FontWeight.Light;
        if (value < 375) return Avalonia.Media.FontWeight.SemiLight;
        if (value < 450) return Avalonia.Media.FontWeight.Normal;
        if (value < 550) return Avalonia.Media.FontWeight.Medium;
        if (value < 650) return Avalonia.Media.FontWeight.DemiBold;
        if (value < 750) return Avalonia.Media.FontWeight.Bold;
        if (value < 850) return Avalonia.Media.FontWeight.ExtraBold;
        if (value < 925) return Avalonia.Media.FontWeight.Black;
        return Avalonia.Media.FontWeight.ExtraBlack;
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
        TextAlign? textAlign = null,
        bool softWrap = true,
        TextOverflow overflow = TextOverflow.Clip,
        int? maxLines = null,
        TextWidthBasis textWidthBasis = TextWidthBasis.Parent,
        TextHeightBehavior? textHeightBehavior = null) : base(key)
    {
        if (maxLines is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLines), "Max lines must be greater than zero.");
        }

        Style = style;
        Child = child;
        TextAlign = textAlign;
        SoftWrap = softWrap;
        Overflow = overflow;
        MaxLines = maxLines;
        TextWidthBasis = textWidthBasis;
        TextHeightBehavior = textHeightBehavior;
    }

    public TextStyle Style { get; }

    public Widget Child { get; }

    public TextAlign? TextAlign { get; }

    public bool SoftWrap { get; }

    public TextOverflow Overflow { get; }

    public int? MaxLines { get; }

    public TextWidthBasis TextWidthBasis { get; }

    public TextHeightBehavior? TextHeightBehavior { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldStyle = (DefaultTextStyle)oldWidget;
        return !Equals(oldStyle.Style, Style)
               || oldStyle.TextAlign != TextAlign
               || oldStyle.SoftWrap != SoftWrap
               || oldStyle.Overflow != Overflow
               || oldStyle.MaxLines != MaxLines
               || oldStyle.TextWidthBasis != TextWidthBasis
               || oldStyle.TextHeightBehavior != TextHeightBehavior;
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
