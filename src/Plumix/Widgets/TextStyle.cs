using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/painting/text_style.dart (approximate)

public sealed record TextStyle(
    FontFamily? FontFamily = null,
    double? FontSize = null,
    Color? Color = null,
    FontWeight? FontWeight = null,
    FontStyle? FontStyle = null,
    double? Height = null,
    double? LetterSpacing = null,
    IReadOnlyList<string>? FontFamilyFallback = null,
    string? Package = null,
    double? WordSpacing = null,
    bool Inherit = true,
    TextBaseline? TextBaseline = null,
    TextLeadingDistribution? LeadingDistribution = null,
    Plumix.UI.TextDecoration? Decoration = null,
    Color? DecorationColor = null,
    Plumix.UI.TextDecorationStyle? DecorationStyle = null)
{
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        properties.Add(new ColorProperty("color", Color, defaultValue: null));
        properties.Add(new StringProperty("family", FontFamily?.Name, quoted: false, defaultValue: null));
        properties.Add(new IterableProperty<string>(
            "familyFallback",
            FontFamilyFallback,
            defaultValue: null));
        properties.Add(new DoubleProperty("size", FontSize, defaultValue: null));
        properties.Add(new DiagnosticsProperty<FontWeight?>("weight", FontWeight, defaultValue: null));
        properties.Add(new DiagnosticsProperty<FontStyle?>("style", FontStyle, defaultValue: null));
        properties.Add(new DoubleProperty("letterSpacing", LetterSpacing, defaultValue: null));
        properties.Add(new DoubleProperty("wordSpacing", WordSpacing, defaultValue: null));
        properties.Add(new EnumProperty<TextBaseline>("baseline", TextBaseline, defaultValue: null));
        properties.Add(new DoubleProperty("height", Height, unit: "x", defaultValue: null));
        properties.Add(new EnumProperty<TextLeadingDistribution>(
            "leadingDistribution",
            LeadingDistribution,
            defaultValue: null));
        properties.Add(new DiagnosticsProperty<bool>("inherit", Inherit));
        properties.Add(new DiagnosticsProperty<Plumix.UI.TextDecoration?>(
            "decoration",
            Decoration,
            defaultValue: null));
        properties.Add(new ColorProperty("decorationColor", DecorationColor, defaultValue: null));
        properties.Add(new EnumProperty<Plumix.UI.TextDecorationStyle>(
            "decorationStyle",
            DecorationStyle,
            defaultValue: null));
    }

    public TextStyle CopyWith(
        FontFamily? fontFamily = null,
        IReadOnlyList<string>? fontFamilyFallback = null,
        string? package = null,
        double? fontSize = null,
        Color? color = null,
        FontWeight? fontWeight = null,
        FontStyle? fontStyle = null,
        double? height = null,
        double? letterSpacing = null,
        double? wordSpacing = null,
        bool? inherit = null,
        TextBaseline? textBaseline = null,
        TextLeadingDistribution? leadingDistribution = null,
        Plumix.UI.TextDecoration? decoration = null,
        Color? decorationColor = null,
        Plumix.UI.TextDecorationStyle? decorationStyle = null)
    {
        return new TextStyle(
            FontFamily: fontFamily ?? FontFamily,
            FontFamilyFallback: fontFamilyFallback ?? FontFamilyFallback,
            Package: package ?? Package,
            FontSize: fontSize ?? FontSize,
            Color: color ?? Color,
            FontWeight: fontWeight ?? FontWeight,
            FontStyle: fontStyle ?? FontStyle,
            Height: height ?? Height,
            LetterSpacing: letterSpacing ?? LetterSpacing,
            WordSpacing: wordSpacing ?? WordSpacing,
            Inherit: inherit ?? Inherit,
            TextBaseline: textBaseline ?? TextBaseline,
            LeadingDistribution: leadingDistribution ?? LeadingDistribution,
            Decoration: decoration ?? Decoration,
            DecorationColor: decorationColor ?? DecorationColor,
            DecorationStyle: decorationStyle ?? DecorationStyle);
    }

    public TextStyle Merge(TextStyle? other)
    {
        if (other is null)
        {
            return this;
        }

        if (!other.Inherit)
        {
            return other;
        }

        return CopyWith(
            fontFamily: other.FontFamily,
            fontFamilyFallback: other.FontFamilyFallback,
            package: other.Package,
            fontSize: other.FontSize,
            color: other.Color,
            fontWeight: other.FontWeight,
            fontStyle: other.FontStyle,
            height: other.Height,
            letterSpacing: other.LetterSpacing,
            wordSpacing: other.WordSpacing,
            textBaseline: other.TextBaseline,
            leadingDistribution: other.LeadingDistribution,
            decoration: other.Decoration,
            decorationColor: other.DecorationColor,
            decorationStyle: other.DecorationStyle);
    }

    /// Describe the difference between this style and another, in terms of how
    /// much damage it will make to the rendering.
    public RenderComparison CompareTo(TextStyle other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (ReferenceEquals(this, other))
        {
            return RenderComparison.Identical;
        }

        if (Inherit != other.Inherit
            || !Equals(FontFamily, other.FontFamily)
            || FontSize != other.FontSize
            || FontWeight != other.FontWeight
            || FontStyle != other.FontStyle
            || LetterSpacing != other.LetterSpacing
            || WordSpacing != other.WordSpacing
            || TextBaseline != other.TextBaseline
            || Height != other.Height
            || LeadingDistribution != other.LeadingDistribution
            || !FontFamilyFallbackEquals(FontFamilyFallback, other.FontFamilyFallback)
            || !string.Equals(Package, other.Package, StringComparison.Ordinal))
        {
            return RenderComparison.Layout;
        }

        if (Color != other.Color
            || Decoration != other.Decoration
            || DecorationColor != other.DecorationColor
            || DecorationStyle != other.DecorationStyle)
        {
            return RenderComparison.Paint;
        }

        return RenderComparison.Identical;
    }

    private static bool FontFamilyFallbackEquals(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        return a is not null && b is not null && a.SequenceEqual(b, StringComparer.Ordinal);
    }

    /// The style information for text runs, encoded for the paragraph engine.
    ///
    /// Only the font size is scaled by `textScaler`; letter spacing, word spacing and height are not.
    public ParagraphTextStyle GetTextStyle(TextScaler? textScaler = null)
    {
        TextScaler scaler = textScaler ?? Painting.TextScaler.NoScaling;
        return new ParagraphTextStyle(
            Color: Color,
            Decoration: Decoration,
            DecorationColor: DecorationColor,
            DecorationStyle: DecorationStyle,
            FontWeight: FontWeight,
            FontStyle: FontStyle,
            TextBaseline: TextBaseline,
            FontFamily: FontFamily,
            FontFamilyFallback: FontFamilyFallback,
            FontSize: FontSize is { } size ? scaler.Scale(size) : null,
            LetterSpacing: LetterSpacing,
            WordSpacing: WordSpacing,
            Height: Height,
            LeadingDistribution: LeadingDistribution);
    }

    /// The style information for paragraphs, encoded for the paragraph engine.
    ///
    /// The `textAlign`, `textDirection`, `ellipsis`, `maxLines`, `locale` and `strutStyle` values come
    /// only from the arguments; font family, size, weight, style and height fall back to this style.
    /// The font size defaults to 14 before `textScaler` is applied. An explicit `textHeightBehavior`
    /// wins over this style's leading distribution.
    public ParagraphStyle GetParagraphStyle(
        TextAlign? textAlign = null,
        TextDirection? textDirection = null,
        TextScaler? textScaler = null,
        string? ellipsis = null,
        int? maxLines = null,
        TextHeightBehavior? textHeightBehavior = null,
        string? locale = null,
        FontFamily? fontFamily = null,
        double? fontSize = null,
        FontWeight? fontWeight = null,
        FontStyle? fontStyle = null,
        double? height = null,
        StrutStyle? strutStyle = null)
    {
        if (maxLines is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLines), "maxLines == null || maxLines > 0");
        }

        TextScaler scaler = textScaler ?? Painting.TextScaler.NoScaling;
        TextHeightBehavior? effectiveTextHeightBehavior = textHeightBehavior
                                                          ?? (LeadingDistribution is { } distribution
                                                              ? new TextHeightBehavior(
                                                                  LeadingDistribution: distribution)
                                                              : null);
        return new ParagraphStyle(
            TextAlign: textAlign,
            TextDirection: textDirection,
            FontWeight: fontWeight ?? FontWeight,
            FontStyle: fontStyle ?? FontStyle,
            FontFamily: fontFamily ?? FontFamily,
            FontSize: scaler.Scale(fontSize ?? FontSize ?? TextDefaults.DefaultFontSize),
            Height: height ?? Height,
            TextHeightBehavior: effectiveTextHeightBehavior,
            StrutStyle: strutStyle is null
                ? null
                : new ParagraphStrutStyle(
                    FontFamily: strutStyle.FontFamily,
                    FontFamilyFallback: strutStyle.FontFamilyFallback,
                    FontSize: strutStyle.FontSize is { } strutFontSize ? scaler.Scale(strutFontSize) : null,
                    Height: strutStyle.Height,
                    Leading: strutStyle.Leading,
                    LeadingDistribution: strutStyle.LeadingDistribution,
                    FontWeight: strutStyle.FontWeight,
                    FontStyle: strutStyle.FontStyle,
                    ForceStrutHeight: strutStyle.ForceStrutHeight),
            MaxLines: maxLines,
            Ellipsis: ellipsis,
            Locale: locale);
    }

    public static TextStyle Lerp(TextStyle a, TextStyle b, double t)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        t = Math.Clamp(t, 0.0, 1.0);

        return new TextStyle(
            FontFamily: t < 0.5 ? a.FontFamily : b.FontFamily,
            FontFamilyFallback: t < 0.5 ? a.FontFamilyFallback : b.FontFamilyFallback,
            Package: t < 0.5 ? a.Package : b.Package,
            FontSize: LerpNullable(a.FontSize, b.FontSize, t),
            Color: LerpColor(a.Color, b.Color, t),
            FontWeight: LerpFontWeight(a.FontWeight, b.FontWeight, t),
            FontStyle: t < 0.5 ? a.FontStyle : b.FontStyle,
            Height: LerpNullable(a.Height, b.Height, t),
            LetterSpacing: LerpNullable(a.LetterSpacing, b.LetterSpacing, t),
            WordSpacing: LerpNullable(a.WordSpacing, b.WordSpacing, t),
            Inherit: t < 0.5 ? a.Inherit : b.Inherit,
            TextBaseline: t < 0.5 ? a.TextBaseline : b.TextBaseline,
            LeadingDistribution: t < 0.5 ? a.LeadingDistribution : b.LeadingDistribution,
            Decoration: t < 0.5 ? a.Decoration : b.Decoration,
            DecorationColor: LerpColor(a.DecorationColor, b.DecorationColor, t),
            DecorationStyle: t < 0.5 ? a.DecorationStyle : b.DecorationStyle);
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
