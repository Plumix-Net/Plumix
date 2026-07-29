using Avalonia.Media;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// flutter/packages/flutter/lib/src/material/text_theme.dart
// flutter/packages/flutter/lib/src/material/typography.dart

public record TextTheme
{
    public TextTheme(
        TextStyle? displayLarge = null,
        TextStyle? displayMedium = null,
        TextStyle? displaySmall = null,
        TextStyle? headlineLarge = null,
        TextStyle? headlineMedium = null,
        TextStyle? headlineSmall = null,
        TextStyle? titleLarge = null,
        TextStyle? titleMedium = null,
        TextStyle? titleSmall = null,
        TextStyle? bodyLarge = null,
        TextStyle? bodyMedium = null,
        TextStyle? bodySmall = null,
        TextStyle? labelLarge = null,
        TextStyle? labelMedium = null,
        TextStyle? labelSmall = null)
    {
        DisplayLarge = displayLarge ?? EmptyStyle;
        DisplayMedium = displayMedium ?? EmptyStyle;
        DisplaySmall = displaySmall ?? EmptyStyle;
        HeadlineLarge = headlineLarge ?? EmptyStyle;
        HeadlineMedium = headlineMedium ?? EmptyStyle;
        HeadlineSmall = headlineSmall ?? EmptyStyle;
        TitleLarge = titleLarge ?? EmptyStyle;
        TitleMedium = titleMedium ?? EmptyStyle;
        TitleSmall = titleSmall ?? EmptyStyle;
        BodyLarge = bodyLarge ?? EmptyStyle;
        BodyMedium = bodyMedium ?? EmptyStyle;
        BodySmall = bodySmall ?? EmptyStyle;
        LabelLarge = labelLarge ?? EmptyStyle;
        LabelMedium = labelMedium ?? EmptyStyle;
        LabelSmall = labelSmall ?? EmptyStyle;
    }

    private static TextStyle EmptyStyle { get; } = new();

    public TextStyle DisplayLarge { get; init; }

    public TextStyle DisplayMedium { get; init; }

    public TextStyle DisplaySmall { get; init; }

    public TextStyle HeadlineLarge { get; init; }

    public TextStyle HeadlineMedium { get; init; }

    public TextStyle HeadlineSmall { get; init; }

    public TextStyle TitleLarge { get; init; }

    public TextStyle TitleMedium { get; init; }

    public TextStyle TitleSmall { get; init; }

    public TextStyle BodyLarge { get; init; }

    public TextStyle BodyMedium { get; init; }

    public TextStyle BodySmall { get; init; }

    public TextStyle LabelLarge { get; init; }

    public TextStyle LabelMedium { get; init; }

    public TextStyle LabelSmall { get; init; }

    public TextTheme CopyWith(
        TextStyle? displayLarge = null,
        TextStyle? displayMedium = null,
        TextStyle? displaySmall = null,
        TextStyle? headlineLarge = null,
        TextStyle? headlineMedium = null,
        TextStyle? headlineSmall = null,
        TextStyle? titleLarge = null,
        TextStyle? titleMedium = null,
        TextStyle? titleSmall = null,
        TextStyle? bodyLarge = null,
        TextStyle? bodyMedium = null,
        TextStyle? bodySmall = null,
        TextStyle? labelLarge = null,
        TextStyle? labelMedium = null,
        TextStyle? labelSmall = null)
    {
        return new TextTheme(
            displayLarge: displayLarge ?? DisplayLarge,
            displayMedium: displayMedium ?? DisplayMedium,
            displaySmall: displaySmall ?? DisplaySmall,
            headlineLarge: headlineLarge ?? HeadlineLarge,
            headlineMedium: headlineMedium ?? HeadlineMedium,
            headlineSmall: headlineSmall ?? HeadlineSmall,
            titleLarge: titleLarge ?? TitleLarge,
            titleMedium: titleMedium ?? TitleMedium,
            titleSmall: titleSmall ?? TitleSmall,
            bodyLarge: bodyLarge ?? BodyLarge,
            bodyMedium: bodyMedium ?? BodyMedium,
            bodySmall: bodySmall ?? BodySmall,
            labelLarge: labelLarge ?? LabelLarge,
            labelMedium: labelMedium ?? LabelMedium,
            labelSmall: labelSmall ?? LabelSmall);
    }

    public TextTheme Merge(TextTheme? other)
    {
        if (other is null)
        {
            return this;
        }

        return new TextTheme(
            displayLarge: MergeStyle(DisplayLarge, other.DisplayLarge),
            displayMedium: MergeStyle(DisplayMedium, other.DisplayMedium),
            displaySmall: MergeStyle(DisplaySmall, other.DisplaySmall),
            headlineLarge: MergeStyle(HeadlineLarge, other.HeadlineLarge),
            headlineMedium: MergeStyle(HeadlineMedium, other.HeadlineMedium),
            headlineSmall: MergeStyle(HeadlineSmall, other.HeadlineSmall),
            titleLarge: MergeStyle(TitleLarge, other.TitleLarge),
            titleMedium: MergeStyle(TitleMedium, other.TitleMedium),
            titleSmall: MergeStyle(TitleSmall, other.TitleSmall),
            bodyLarge: MergeStyle(BodyLarge, other.BodyLarge),
            bodyMedium: MergeStyle(BodyMedium, other.BodyMedium),
            bodySmall: MergeStyle(BodySmall, other.BodySmall),
            labelLarge: MergeStyle(LabelLarge, other.LabelLarge),
            labelMedium: MergeStyle(LabelMedium, other.LabelMedium),
            labelSmall: MergeStyle(LabelSmall, other.LabelSmall));
    }

    public TextTheme Apply(
        FontFamily? fontFamily = null,
        double fontSizeFactor = 1.0,
        double fontSizeDelta = 0.0,
        double letterSpacingFactor = 1.0,
        double letterSpacingDelta = 0.0,
        double heightFactor = 1.0,
        double heightDelta = 0.0,
        Color? displayColor = null,
        Color? bodyColor = null)
    {
        if (!double.IsFinite(fontSizeFactor)
            || !double.IsFinite(fontSizeDelta)
            || !double.IsFinite(letterSpacingFactor)
            || !double.IsFinite(letterSpacingDelta)
            || !double.IsFinite(heightFactor)
            || !double.IsFinite(heightDelta))
        {
            throw new ArgumentException("Typography scale values must be finite.");
        }

        return new TextTheme(
            displayLarge: ApplyStyle(DisplayLarge, fontFamily, displayColor, true),
            displayMedium: ApplyStyle(DisplayMedium, fontFamily, displayColor, true),
            displaySmall: ApplyStyle(DisplaySmall, fontFamily, displayColor, true),
            headlineLarge: ApplyStyle(HeadlineLarge, fontFamily, displayColor, true),
            headlineMedium: ApplyStyle(HeadlineMedium, fontFamily, displayColor, true),
            headlineSmall: ApplyStyle(HeadlineSmall, fontFamily, bodyColor, true),
            titleLarge: ApplyStyle(TitleLarge, fontFamily, bodyColor, true),
            titleMedium: ApplyStyle(TitleMedium, fontFamily, bodyColor, true),
            titleSmall: ApplyStyle(TitleSmall, fontFamily, bodyColor, true),
            bodyLarge: ApplyStyle(BodyLarge, fontFamily, bodyColor, true),
            bodyMedium: ApplyStyle(BodyMedium, fontFamily, bodyColor, true),
            bodySmall: ApplyStyle(BodySmall, fontFamily, displayColor, true),
            labelLarge: ApplyStyle(LabelLarge, fontFamily, bodyColor, true),
            labelMedium: ApplyStyle(LabelMedium, fontFamily, bodyColor, true),
            labelSmall: ApplyStyle(LabelSmall, fontFamily, bodyColor, true));

        TextStyle ApplyStyle(
            TextStyle style,
            FontFamily? family,
            Color? color,
            bool applyGeometry)
        {
            double? fontSize = style.FontSize;
            double? letterSpacing = style.LetterSpacing;
            double? height = style.Height;
            if (applyGeometry)
            {
                fontSize = fontSize.HasValue
                    ? (fontSize.Value * fontSizeFactor) + fontSizeDelta
                    : null;
                letterSpacing = letterSpacing.HasValue
                    ? (letterSpacing.Value * letterSpacingFactor) + letterSpacingDelta
                    : null;
                height = height.HasValue
                    ? (height.Value * heightFactor) + heightDelta
                    : null;
            }

            return style.CopyWith(
                fontFamily: family,
                fontSize: fontSize,
                color: color,
                height: height,
                letterSpacing: letterSpacing);
        }
    }

    public static TextTheme Lerp(TextTheme? a, TextTheme? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        TextTheme from = a ?? new TextTheme();
        TextTheme to = b ?? new TextTheme();
        return new TextTheme(
            displayLarge: TextStyle.Lerp(from.DisplayLarge, to.DisplayLarge, t),
            displayMedium: TextStyle.Lerp(from.DisplayMedium, to.DisplayMedium, t),
            displaySmall: TextStyle.Lerp(from.DisplaySmall, to.DisplaySmall, t),
            headlineLarge: TextStyle.Lerp(from.HeadlineLarge, to.HeadlineLarge, t),
            headlineMedium: TextStyle.Lerp(from.HeadlineMedium, to.HeadlineMedium, t),
            headlineSmall: TextStyle.Lerp(from.HeadlineSmall, to.HeadlineSmall, t),
            titleLarge: TextStyle.Lerp(from.TitleLarge, to.TitleLarge, t),
            titleMedium: TextStyle.Lerp(from.TitleMedium, to.TitleMedium, t),
            titleSmall: TextStyle.Lerp(from.TitleSmall, to.TitleSmall, t),
            bodyLarge: TextStyle.Lerp(from.BodyLarge, to.BodyLarge, t),
            bodyMedium: TextStyle.Lerp(from.BodyMedium, to.BodyMedium, t),
            bodySmall: TextStyle.Lerp(from.BodySmall, to.BodySmall, t),
            labelLarge: TextStyle.Lerp(from.LabelLarge, to.LabelLarge, t),
            labelMedium: TextStyle.Lerp(from.LabelMedium, to.LabelMedium, t),
            labelSmall: TextStyle.Lerp(from.LabelSmall, to.LabelSmall, t));
    }

    public static TextTheme Of(BuildContext context) => Theme.Of(context).TextTheme;

    public static TextTheme PrimaryOf(BuildContext context) => Theme.Of(context).PrimaryTextTheme;

    private static TextStyle MergeStyle(TextStyle current, TextStyle other) => new(
        FontFamily: other.FontFamily ?? current.FontFamily,
        FontSize: other.FontSize ?? current.FontSize,
        Color: other.Color ?? current.Color,
        FontWeight: other.FontWeight ?? current.FontWeight,
        FontStyle: other.FontStyle ?? current.FontStyle,
        Height: other.Height ?? current.Height,
        LetterSpacing: other.LetterSpacing ?? current.LetterSpacing);
}

public sealed record MaterialTextTheme : TextTheme
{
    private static readonly FontFamily DefaultBodyFontFamily = ResolveDefaultBodyFontFamily();
    private static readonly Color DefaultForeground = Color.Parse("#FF1D1B20");

    public MaterialTextTheme(
        TextStyle? bodyMedium = null,
        TextStyle? titleLarge = null,
        TextStyle? labelLarge = null,
        TextStyle? labelSmall = null,
        TextStyle? titleMedium = null,
        TextStyle? bodyLarge = null,
        TextStyle? labelMedium = null,
        TextStyle? bodySmall = null,
        TextStyle? headlineSmall = null,
        TextStyle? titleSmall = null,
        TextStyle? headlineMedium = null,
        TextStyle? displayLarge = null,
        TextStyle? displayMedium = null,
        TextStyle? displaySmall = null,
        TextStyle? headlineLarge = null) : base(
            displayLarge: displayLarge ?? DefaultDisplayLarge,
            displayMedium: displayMedium ?? DefaultDisplayMedium,
            displaySmall: displaySmall ?? DefaultDisplaySmall,
            headlineLarge: headlineLarge ?? DefaultHeadlineLarge,
            headlineMedium: headlineMedium ?? DefaultHeadlineMedium,
            headlineSmall: headlineSmall ?? DefaultHeadlineSmall,
            titleLarge: titleLarge ?? DefaultTitleLarge,
            titleMedium: titleMedium ?? DefaultTitleMedium,
            titleSmall: titleSmall ?? DefaultTitleSmall,
            bodyLarge: bodyLarge ?? DefaultBodyLarge,
            bodyMedium: bodyMedium ?? DefaultBodyMedium,
            bodySmall: bodySmall ?? DefaultBodySmall,
            labelLarge: labelLarge ?? DefaultLabelLarge,
            labelMedium: labelMedium ?? DefaultLabelMedium,
            labelSmall: labelSmall ?? DefaultLabelSmall)
    {
    }

    public static TextStyle DefaultDisplayLarge { get; } = Style(57, 1.12, -0.25);

    public static TextStyle DefaultDisplayMedium { get; } = Style(45, 1.16, 0.0);

    public static TextStyle DefaultDisplaySmall { get; } = Style(36, 1.22, 0.0);

    public static TextStyle DefaultHeadlineLarge { get; } = Style(32, 1.25, 0.0);

    public static TextStyle DefaultHeadlineMedium { get; } = Style(28, 1.29, 0.0);

    public static TextStyle DefaultHeadlineSmall { get; } = Style(24, 1.33, 0.0);

    public static TextStyle DefaultTitleLarge { get; } = Style(22, 1.27, 0.0);

    public static TextStyle DefaultTitleMedium { get; } = Style(16, 1.5, 0.15, FontWeight.Medium);

    public static TextStyle DefaultTitleSmall { get; } = Style(14, 1.43, 0.1, FontWeight.Medium);

    public static TextStyle DefaultBodyLarge { get; } = Style(16, 1.5, 0.5);

    public static TextStyle DefaultBodyMedium { get; } = Style(14, 1.43, 0.25);

    public static TextStyle DefaultBodySmall { get; } = Style(
        12,
        1.33,
        0.4,
        color: Color.Parse("#FF49454F"));

    public static TextStyle DefaultLabelLarge { get; } = Style(14, 1.43, 0.1, FontWeight.Medium);

    public static TextStyle DefaultLabelMedium { get; } = Style(12, 1.33, 0.5, FontWeight.Medium);

    public static TextStyle DefaultLabelSmall { get; } = Style(11, 1.45, 0.5, FontWeight.Medium);

    public static MaterialTextTheme Fallback { get; } = new();

    public static MaterialTextTheme Lerp(MaterialTextTheme a, MaterialTextTheme b, double t)
    {
        TextTheme value = TextTheme.Lerp(a, b, t);
        return new MaterialTextTheme(
            displayLarge: value.DisplayLarge,
            displayMedium: value.DisplayMedium,
            displaySmall: value.DisplaySmall,
            headlineLarge: value.HeadlineLarge,
            headlineMedium: value.HeadlineMedium,
            headlineSmall: value.HeadlineSmall,
            titleLarge: value.TitleLarge,
            titleMedium: value.TitleMedium,
            titleSmall: value.TitleSmall,
            bodyLarge: value.BodyLarge,
            bodyMedium: value.BodyMedium,
            bodySmall: value.BodySmall,
            labelLarge: value.LabelLarge,
            labelMedium: value.LabelMedium,
            labelSmall: value.LabelSmall);
    }

    private static TextStyle Style(
        double fontSize,
        double height,
        double letterSpacing,
        FontWeight? fontWeight = null,
        Color? color = null)
    {
        return new TextStyle(
            FontFamily: DefaultBodyFontFamily,
            FontSize: fontSize,
            Color: color ?? DefaultForeground,
            FontWeight: fontWeight ?? FontWeight.Normal,
            FontStyle: FontStyle.Normal,
            Height: height,
            LetterSpacing: letterSpacing);
    }

    internal static FontFamily ResolveDefaultBodyFontFamily()
    {
        if (OperatingSystem.IsIOS() || OperatingSystem.IsMacOS())
        {
            return new FontFamily(".AppleSystemUIFont");
        }

        if (OperatingSystem.IsAndroid())
        {
            return new FontFamily("Roboto");
        }

        if (OperatingSystem.IsWindows())
        {
            return new FontFamily("Segoe UI");
        }

        if (OperatingSystem.IsLinux())
        {
            return new FontFamily("Noto Sans");
        }

        return FontFamily.Default;
    }
}

public sealed record Typography(
    TextTheme Black,
    TextTheme White,
    TextTheme EnglishLike,
    TextTheme Dense,
    TextTheme Tall)
{
    public static Typography Material2021(
        TargetPlatform? platform = TargetPlatform.Android,
        ColorScheme? colorScheme = null,
        TextTheme? black = null,
        TextTheme? white = null,
        TextTheme? englishLike = null,
        TextTheme? dense = null,
        TextTheme? tall = null)
    {
        ColorScheme effectiveScheme = colorScheme ?? ColorScheme.Light();
        FontFamily fontFamily = ResolveFontFamily(platform);
        TextTheme geometry = CreateGeometry(fontFamily);
        TextTheme darkGlyphs = black ?? geometry.Apply(
            displayColor: effectiveScheme.Brightness == Brightness.Light
                ? effectiveScheme.OnSurface
                : effectiveScheme.Surface,
            bodyColor: effectiveScheme.Brightness == Brightness.Light
                ? effectiveScheme.OnSurface
                : effectiveScheme.Surface);
        TextTheme lightGlyphs = white ?? geometry.Apply(
            displayColor: effectiveScheme.Brightness == Brightness.Light
                ? effectiveScheme.Surface
                : effectiveScheme.OnSurface,
            bodyColor: effectiveScheme.Brightness == Brightness.Light
                ? effectiveScheme.Surface
                : effectiveScheme.OnSurface);
        return new Typography(
            Black: darkGlyphs,
            White: lightGlyphs,
            EnglishLike: englishLike ?? geometry,
            Dense: dense ?? geometry,
            Tall: tall ?? geometry);
    }

    public TextTheme GeometryThemeFor(ScriptCategory category) => category switch
    {
        ScriptCategory.EnglishLike => EnglishLike,
        ScriptCategory.Dense => Dense,
        ScriptCategory.Tall => Tall,
        _ => throw new ArgumentOutOfRangeException(nameof(category)),
    };

    public Typography CopyWith(
        TextTheme? black = null,
        TextTheme? white = null,
        TextTheme? englishLike = null,
        TextTheme? dense = null,
        TextTheme? tall = null)
    {
        return new Typography(
            Black: black ?? Black,
            White: white ?? White,
            EnglishLike: englishLike ?? EnglishLike,
            Dense: dense ?? Dense,
            Tall: tall ?? Tall);
    }

    public static Typography Lerp(Typography a, Typography b, double t)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new Typography(
            Black: TextTheme.Lerp(a.Black, b.Black, t),
            White: TextTheme.Lerp(a.White, b.White, t),
            EnglishLike: TextTheme.Lerp(a.EnglishLike, b.EnglishLike, t),
            Dense: TextTheme.Lerp(a.Dense, b.Dense, t),
            Tall: TextTheme.Lerp(a.Tall, b.Tall, t));
    }

    private static TextTheme CreateGeometry(FontFamily fontFamily)
    {
        var defaults = MaterialTextTheme.Fallback;
        return defaults.Apply(fontFamily: fontFamily, displayColor: Colors.Black, bodyColor: Colors.Black);
    }

    private static FontFamily ResolveFontFamily(TargetPlatform? platform) => platform switch
    {
        TargetPlatform.IOS or TargetPlatform.MacOS => new FontFamily(".AppleSystemUIFont"),
        TargetPlatform.Android or TargetPlatform.Fuchsia => new FontFamily("Roboto"),
        TargetPlatform.Windows => new FontFamily("Segoe UI"),
        TargetPlatform.Linux => new FontFamily("Noto Sans"),
        null => MaterialTextTheme.ResolveDefaultBodyFontFamily(),
        _ => MaterialTextTheme.ResolveDefaultBodyFontFamily(),
    };
}

public enum ScriptCategory
{
    EnglishLike,
    Dense,
    Tall,
}
