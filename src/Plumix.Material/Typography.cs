using Avalonia.Media;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// material_ui/lib/src/text_theme.dart
// material_ui/lib/src/typography.dart

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
        IReadOnlyList<string>? fontFamilyFallback = null,
        string? package = null,
        double fontSizeFactor = 1.0,
        double fontSizeDelta = 0.0,
        double letterSpacingFactor = 1.0,
        double letterSpacingDelta = 0.0,
        double wordSpacingFactor = 1.0,
        double wordSpacingDelta = 0.0,
        double heightFactor = 1.0,
        double heightDelta = 0.0,
        Color? displayColor = null,
        Color? bodyColor = null,
        Plumix.UI.TextDecoration? decoration = null,
        Color? decorationColor = null,
        Plumix.UI.TextDecorationStyle? decorationStyle = null)
    {
        if (!double.IsFinite(fontSizeFactor)
            || !double.IsFinite(fontSizeDelta)
            || !double.IsFinite(letterSpacingFactor)
            || !double.IsFinite(letterSpacingDelta)
            || !double.IsFinite(wordSpacingFactor)
            || !double.IsFinite(wordSpacingDelta)
            || !double.IsFinite(heightFactor)
            || !double.IsFinite(heightDelta))
        {
            throw new ArgumentException("Typography scale values must be finite.");
        }

        return new TextTheme(
            displayLarge: ApplyStyle(DisplayLarge, displayColor),
            displayMedium: ApplyStyle(DisplayMedium, displayColor),
            displaySmall: ApplyStyle(DisplaySmall, displayColor),
            headlineLarge: ApplyStyle(HeadlineLarge, displayColor),
            headlineMedium: ApplyStyle(HeadlineMedium, displayColor),
            headlineSmall: ApplyStyle(HeadlineSmall, bodyColor),
            titleLarge: ApplyStyle(TitleLarge, bodyColor),
            titleMedium: ApplyStyle(TitleMedium, bodyColor),
            titleSmall: ApplyStyle(TitleSmall, bodyColor),
            bodyLarge: ApplyStyle(BodyLarge, bodyColor),
            bodyMedium: ApplyStyle(BodyMedium, bodyColor),
            bodySmall: ApplyStyle(BodySmall, displayColor),
            labelLarge: ApplyStyle(LabelLarge, bodyColor),
            labelMedium: ApplyStyle(LabelMedium, bodyColor),
            labelSmall: ApplyStyle(LabelSmall, bodyColor));

        TextStyle ApplyStyle(TextStyle style, Color? color)
        {
            double? fontSize = style.FontSize;
            double? letterSpacing = style.LetterSpacing;
            double? wordSpacing = style.WordSpacing;
            double? height = style.Height;
            fontSize = fontSize.HasValue
                ? (fontSize.Value * fontSizeFactor) + fontSizeDelta
                : null;
            letterSpacing = letterSpacing.HasValue
                ? (letterSpacing.Value * letterSpacingFactor) + letterSpacingDelta
                : null;
            wordSpacing = wordSpacing.HasValue
                ? (wordSpacing.Value * wordSpacingFactor) + wordSpacingDelta
                : null;
            height = height.HasValue
                ? (height.Value * heightFactor) + heightDelta
                : null;

            return style.CopyWith(
                fontFamily: fontFamily,
                fontFamilyFallback: fontFamilyFallback,
                package: package,
                fontSize: fontSize,
                color: color,
                height: height,
                letterSpacing: letterSpacing,
                wordSpacing: wordSpacing,
                decoration: decoration,
                decorationColor: decorationColor,
                decorationStyle: decorationStyle);
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

    private static TextStyle MergeStyle(TextStyle current, TextStyle other)
    {
        if (!other.Inherit)
        {
            return other;
        }

        return new TextStyle(
            FontFamily: other.FontFamily ?? current.FontFamily,
            FontFamilyFallback: other.FontFamilyFallback ?? current.FontFamilyFallback,
            Package: other.Package ?? current.Package,
            FontSize: other.FontSize ?? current.FontSize,
            Color: other.Color ?? current.Color,
            FontWeight: other.FontWeight ?? current.FontWeight,
            FontStyle: other.FontStyle ?? current.FontStyle,
            Height: other.Height ?? current.Height,
            LetterSpacing: other.LetterSpacing ?? current.LetterSpacing,
            WordSpacing: other.WordSpacing ?? current.WordSpacing,
            Inherit: current.Inherit,
            TextBaseline: other.TextBaseline ?? current.TextBaseline,
            LeadingDistribution: other.LeadingDistribution ?? current.LeadingDistribution,
            Decoration: other.Decoration ?? current.Decoration,
            DecorationColor: other.DecorationColor ?? current.DecorationColor,
            DecorationStyle: other.DecorationStyle ?? current.DecorationStyle);
    }
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

public sealed record Typography
{
    private static readonly IReadOnlyList<string> LinuxFontFallback =
    [
        "Ubuntu",
        "Adwaita Sans",
        "Cantarell",
        "DejaVu Sans",
        "Liberation Sans",
        "Arial",
    ];

    public Typography(
        TargetPlatform? platform = TargetPlatform.Android,
        TextTheme? black = null,
        TextTheme? white = null,
        TextTheme? englishLike = null,
        TextTheme? dense = null,
        TextTheme? tall = null)
        : this(CreateMaterial2018Values(platform, black, white, englishLike, dense, tall))
    {
    }

    public Typography(
        TextTheme black,
        TextTheme white,
        TextTheme englishLike,
        TextTheme dense,
        TextTheme tall)
    {
        Black = black ?? throw new ArgumentNullException(nameof(black));
        White = white ?? throw new ArgumentNullException(nameof(white));
        EnglishLike = englishLike ?? throw new ArgumentNullException(nameof(englishLike));
        Dense = dense ?? throw new ArgumentNullException(nameof(dense));
        Tall = tall ?? throw new ArgumentNullException(nameof(tall));
    }

    private Typography(
        (TextTheme Black, TextTheme White, TextTheme EnglishLike, TextTheme Dense, TextTheme Tall) values)
        : this(values.Black, values.White, values.EnglishLike, values.Dense, values.Tall)
    {
    }

    public TextTheme Black { get; init; }

    public TextTheme White { get; init; }

    public TextTheme EnglishLike { get; init; }

    public TextTheme Dense { get; init; }

    public TextTheme Tall { get; init; }

    public static TextTheme BlackMountainView { get; } = CreatePlatformTheme(PlatformTypeface.MountainView, true);

    public static TextTheme WhiteMountainView { get; } = CreatePlatformTheme(PlatformTypeface.MountainView, false);

    public static TextTheme BlackRedmond { get; } = CreatePlatformTheme(PlatformTypeface.Redmond, true);

    public static TextTheme WhiteRedmond { get; } = CreatePlatformTheme(PlatformTypeface.Redmond, false);

    public static TextTheme BlackHelsinki { get; } = CreatePlatformTheme(PlatformTypeface.Helsinki, true);

    public static TextTheme WhiteHelsinki { get; } = CreatePlatformTheme(PlatformTypeface.Helsinki, false);

    public static TextTheme BlackCupertino { get; } = CreatePlatformTheme(PlatformTypeface.Cupertino, true);

    public static TextTheme WhiteCupertino { get; } = CreatePlatformTheme(PlatformTypeface.Cupertino, false);

    public static TextTheme BlackRedwoodCity { get; } = CreatePlatformTheme(PlatformTypeface.RedwoodCity, true);

    public static TextTheme WhiteRedwoodCity { get; } = CreatePlatformTheme(PlatformTypeface.RedwoodCity, false);

    public static TextTheme EnglishLike2014 { get; } = CreateEnglishLike2014();

    public static TextTheme Dense2014 { get; } = CreateDense2014();

    public static TextTheme Tall2014 { get; } = CreateTall2014();

    public static TextTheme EnglishLike2018 { get; } = CreateEnglishLike2018();

    public static TextTheme Dense2018 { get; } = CreateDense2018();

    public static TextTheme Tall2018 { get; } = CreateTall2018();

    public static TextTheme EnglishLike2021 { get; } = Create2021Geometry(TextBaseline.Alphabetic);

    public static TextTheme Dense2021 { get; } = Create2021Geometry(TextBaseline.Ideographic);

    public static TextTheme Tall2021 { get; } = Create2021Geometry(TextBaseline.Alphabetic);

    public static Typography Material2014(
        TargetPlatform? platform = TargetPlatform.Android,
        TextTheme? black = null,
        TextTheme? white = null,
        TextTheme? englishLike = null,
        TextTheme? dense = null,
        TextTheme? tall = null)
    {
        (TextTheme platformBlack, TextTheme platformWhite) = WithPlatform(platform, black, white);
        return new Typography(
            platformBlack,
            platformWhite,
            englishLike ?? EnglishLike2014,
            dense ?? Dense2014,
            tall ?? Tall2014);
    }

    public static Typography Material2018(
        TargetPlatform? platform = TargetPlatform.Android,
        TextTheme? black = null,
        TextTheme? white = null,
        TextTheme? englishLike = null,
        TextTheme? dense = null,
        TextTheme? tall = null)
    {
        return new Typography(CreateMaterial2018Values(platform, black, white, englishLike, dense, tall));
    }

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
        (TextTheme platformBlack, TextTheme platformWhite) = WithPlatform(platform, black, white);
        Color dark = effectiveScheme.Brightness == Brightness.Light
            ? effectiveScheme.OnSurface
            : effectiveScheme.Surface;
        Color light = effectiveScheme.Brightness == Brightness.Light
            ? effectiveScheme.Surface
            : effectiveScheme.OnSurface;
        return new Typography(
            platformBlack.Apply(displayColor: dark, bodyColor: dark, decorationColor: dark),
            platformWhite.Apply(displayColor: light, bodyColor: light, decorationColor: light),
            englishLike ?? EnglishLike2021,
            dense ?? Dense2021,
            tall ?? Tall2021);
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
            black ?? Black,
            white ?? White,
            englishLike ?? EnglishLike,
            dense ?? Dense,
            tall ?? Tall);
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
            TextTheme.Lerp(a.Black, b.Black, t),
            TextTheme.Lerp(a.White, b.White, t),
            TextTheme.Lerp(a.EnglishLike, b.EnglishLike, t),
            TextTheme.Lerp(a.Dense, b.Dense, t),
            TextTheme.Lerp(a.Tall, b.Tall, t));
    }

    private static (TextTheme Black, TextTheme White, TextTheme EnglishLike, TextTheme Dense, TextTheme Tall)
        CreateMaterial2018Values(
        TargetPlatform? platform,
        TextTheme? black,
        TextTheme? white,
        TextTheme? englishLike,
        TextTheme? dense,
        TextTheme? tall)
    {
        (TextTheme platformBlack, TextTheme platformWhite) = WithPlatform(platform, black, white);
        return (
            platformBlack,
            platformWhite,
            englishLike ?? EnglishLike2018,
            dense ?? Dense2018,
            tall ?? Tall2018);
    }

    private static (TextTheme Black, TextTheme White) WithPlatform(
        TargetPlatform? platform,
        TextTheme? black,
        TextTheme? white)
    {
        if (platform is null)
        {
            if (black is null || white is null)
            {
                throw new ArgumentException("Black and white themes are required when platform is null.");
            }

            return (black, white);
        }

        return platform.Value switch
        {
            TargetPlatform.IOS => (black ?? BlackCupertino, white ?? WhiteCupertino),
            TargetPlatform.Android or TargetPlatform.Fuchsia =>
                (black ?? BlackMountainView, white ?? WhiteMountainView),
            TargetPlatform.Windows => (black ?? BlackRedmond, white ?? WhiteRedmond),
            TargetPlatform.MacOS => (black ?? BlackRedwoodCity, white ?? WhiteRedwoodCity),
            TargetPlatform.Linux => (black ?? BlackHelsinki, white ?? WhiteHelsinki),
            _ => throw new ArgumentOutOfRangeException(nameof(platform)),
        };
    }

    private static TextTheme CreatePlatformTheme(PlatformTypeface typeface, bool black)
    {
        Color displayColor = black ? Color.FromArgb(0x8A, 0, 0, 0) : Color.FromArgb(0xB3, 255, 255, 255);
        Color bodyColor = black ? Color.FromArgb(0xDD, 0, 0, 0) : Colors.White;
        Color strongColor = black ? Colors.Black : Colors.White;
        return new TextTheme(
            displayLarge: PlatformStyle(typeface, displayColor, true),
            displayMedium: PlatformStyle(typeface, displayColor, true),
            displaySmall: PlatformStyle(typeface, displayColor, true),
            headlineLarge: PlatformStyle(typeface, displayColor, true),
            headlineMedium: PlatformStyle(typeface, displayColor, true),
            headlineSmall: PlatformStyle(typeface, bodyColor, true),
            titleLarge: PlatformStyle(typeface, bodyColor, true),
            titleMedium: PlatformStyle(typeface, bodyColor, false),
            titleSmall: PlatformStyle(typeface, strongColor, false),
            bodyLarge: PlatformStyle(typeface, bodyColor, false),
            bodyMedium: PlatformStyle(typeface, bodyColor, false),
            bodySmall: PlatformStyle(typeface, displayColor, false),
            labelLarge: PlatformStyle(typeface, bodyColor, false),
            labelMedium: PlatformStyle(typeface, strongColor, false),
            labelSmall: PlatformStyle(typeface, strongColor, false));
    }

    private static TextStyle PlatformStyle(
        PlatformTypeface typeface,
        Color color,
        bool display)
    {
        FontFamily family = typeface switch
        {
            PlatformTypeface.MountainView or PlatformTypeface.Helsinki => new FontFamily("Roboto"),
            PlatformTypeface.Redmond => new FontFamily("Segoe UI"),
            PlatformTypeface.RedwoodCity => new FontFamily(".AppleSystemUIFont"),
            PlatformTypeface.Cupertino => new FontFamily(
                display ? "CupertinoSystemDisplay" : "CupertinoSystemText"),
            _ => throw new ArgumentOutOfRangeException(nameof(typeface)),
        };
        return new TextStyle(
            FontFamily: family,
            FontFamilyFallback: typeface == PlatformTypeface.Helsinki ? LinuxFontFallback : null,
            Color: color,
            Decoration: Plumix.UI.TextDecoration.None);
    }

    private static TextTheme CreateEnglishLike2014() => new(
        displayLarge: GeometryStyle(112, FontWeight.Thin),
        displayMedium: GeometryStyle(56, FontWeight.Normal),
        displaySmall: GeometryStyle(45, FontWeight.Normal),
        headlineLarge: GeometryStyle(40, FontWeight.Normal),
        headlineMedium: GeometryStyle(34, FontWeight.Normal),
        headlineSmall: GeometryStyle(24, FontWeight.Normal),
        titleLarge: GeometryStyle(20, FontWeight.Medium),
        titleMedium: GeometryStyle(16, FontWeight.Normal),
        titleSmall: GeometryStyle(14, FontWeight.Medium, 0.1),
        bodyLarge: GeometryStyle(14, FontWeight.Medium),
        bodyMedium: GeometryStyle(14, FontWeight.Normal),
        bodySmall: GeometryStyle(12, FontWeight.Normal),
        labelLarge: GeometryStyle(14, FontWeight.Medium),
        labelMedium: GeometryStyle(12, FontWeight.Normal),
        labelSmall: GeometryStyle(10, FontWeight.Normal, 1.5));

    private static TextTheme CreateDense2014() => CreateLegacyDense(
        displayLargeWeight: FontWeight.Thin,
        displayMediumWeight: FontWeight.Normal,
        bodyLargeWeight: FontWeight.Medium,
        sizes: [112, 56, 45, 40, 34, 24, 21, 17, 15, 15, 15, 13, 15, 12, 11]);

    private static TextTheme CreateTall2014() => CreateLegacyTall(
        sizes: [112, 56, 45, 40, 34, 24, 21, 17, 15, 15, 15, 13, 15, 12, 11]);

    private static TextTheme CreateEnglishLike2018() => new(
        displayLarge: GeometryStyle(96, FontWeight.Light, -1.5),
        displayMedium: GeometryStyle(60, FontWeight.Light, -0.5),
        displaySmall: GeometryStyle(48, FontWeight.Normal, 0.0),
        headlineLarge: GeometryStyle(40, FontWeight.Normal, 0.25),
        headlineMedium: GeometryStyle(34, FontWeight.Normal, 0.25),
        headlineSmall: GeometryStyle(24, FontWeight.Normal, 0.0),
        titleLarge: GeometryStyle(20, FontWeight.Medium, 0.15),
        titleMedium: GeometryStyle(16, FontWeight.Normal, 0.15),
        titleSmall: GeometryStyle(14, FontWeight.Medium, 0.1),
        bodyLarge: GeometryStyle(16, FontWeight.Normal, 0.5),
        bodyMedium: GeometryStyle(14, FontWeight.Normal, 0.25),
        bodySmall: GeometryStyle(12, FontWeight.Normal, 0.4),
        labelLarge: GeometryStyle(14, FontWeight.Medium, 1.25),
        labelMedium: GeometryStyle(11, FontWeight.Normal, 1.5),
        labelSmall: GeometryStyle(10, FontWeight.Normal, 1.5));

    private static TextTheme CreateDense2018() => CreateLegacyDense(
        displayLargeWeight: FontWeight.Thin,
        displayMediumWeight: FontWeight.Thin,
        bodyLargeWeight: FontWeight.Normal,
        sizes: [96, 60, 48, 40, 34, 24, 21, 17, 15, 17, 15, 13, 15, 12, 11]);

    private static TextTheme CreateTall2018() => CreateLegacyTall(
        sizes: [96, 60, 48, 40, 34, 24, 21, 17, 15, 17, 15, 13, 15, 12, 11]);

    private static TextTheme CreateLegacyDense(
        FontWeight displayLargeWeight,
        FontWeight displayMediumWeight,
        FontWeight bodyLargeWeight,
        IReadOnlyList<double> sizes) => new(
        displayLarge: GeometryStyle(sizes[0], displayLargeWeight, baseline: TextBaseline.Ideographic),
        displayMedium: GeometryStyle(sizes[1], displayMediumWeight, baseline: TextBaseline.Ideographic),
        displaySmall: GeometryStyle(sizes[2], FontWeight.Normal, baseline: TextBaseline.Ideographic),
        headlineLarge: GeometryStyle(sizes[3], FontWeight.Normal, baseline: TextBaseline.Ideographic),
        headlineMedium: GeometryStyle(sizes[4], FontWeight.Normal, baseline: TextBaseline.Ideographic),
        headlineSmall: GeometryStyle(sizes[5], FontWeight.Normal, baseline: TextBaseline.Ideographic),
        titleLarge: GeometryStyle(sizes[6], FontWeight.Medium, baseline: TextBaseline.Ideographic),
        titleMedium: GeometryStyle(sizes[7], FontWeight.Normal, baseline: TextBaseline.Ideographic),
        titleSmall: GeometryStyle(sizes[8], FontWeight.Medium, baseline: TextBaseline.Ideographic),
        bodyLarge: GeometryStyle(sizes[9], bodyLargeWeight, baseline: TextBaseline.Ideographic),
        bodyMedium: GeometryStyle(sizes[10], FontWeight.Normal, baseline: TextBaseline.Ideographic),
        bodySmall: GeometryStyle(sizes[11], FontWeight.Normal, baseline: TextBaseline.Ideographic),
        labelLarge: GeometryStyle(sizes[12], FontWeight.Medium, baseline: TextBaseline.Ideographic),
        labelMedium: GeometryStyle(sizes[13], FontWeight.Normal, baseline: TextBaseline.Ideographic),
        labelSmall: GeometryStyle(sizes[14], FontWeight.Normal, baseline: TextBaseline.Ideographic));

    private static TextTheme CreateLegacyTall(IReadOnlyList<double> sizes) => new(
        displayLarge: GeometryStyle(sizes[0], FontWeight.Normal),
        displayMedium: GeometryStyle(sizes[1], FontWeight.Normal),
        displaySmall: GeometryStyle(sizes[2], FontWeight.Normal),
        headlineLarge: GeometryStyle(sizes[3], FontWeight.Normal),
        headlineMedium: GeometryStyle(sizes[4], FontWeight.Normal),
        headlineSmall: GeometryStyle(sizes[5], FontWeight.Normal),
        titleLarge: GeometryStyle(sizes[6], FontWeight.Bold),
        titleMedium: GeometryStyle(sizes[7], FontWeight.Normal),
        titleSmall: GeometryStyle(sizes[8], FontWeight.Medium),
        bodyLarge: GeometryStyle(sizes[9], FontWeight.Bold),
        bodyMedium: GeometryStyle(sizes[10], FontWeight.Normal),
        bodySmall: GeometryStyle(sizes[11], FontWeight.Normal),
        labelLarge: GeometryStyle(sizes[12], FontWeight.Bold),
        labelMedium: GeometryStyle(sizes[13], FontWeight.Normal),
        labelSmall: GeometryStyle(sizes[14], FontWeight.Normal));

    private static TextTheme Create2021Geometry(TextBaseline baseline) => new(
        displayLarge: GeometryStyle(57, FontWeight.Normal, -0.25, 1.12, baseline, true),
        displayMedium: GeometryStyle(45, FontWeight.Normal, 0.0, 1.16, baseline, true),
        displaySmall: GeometryStyle(36, FontWeight.Normal, 0.0, 1.22, baseline, true),
        headlineLarge: GeometryStyle(32, FontWeight.Normal, 0.0, 1.25, baseline, true),
        headlineMedium: GeometryStyle(28, FontWeight.Normal, 0.0, 1.29, baseline, true),
        headlineSmall: GeometryStyle(24, FontWeight.Normal, 0.0, 1.33, baseline, true),
        titleLarge: GeometryStyle(22, FontWeight.Normal, 0.0, 1.27, baseline, true),
        titleMedium: GeometryStyle(16, FontWeight.Medium, 0.15, 1.5, baseline, true),
        titleSmall: GeometryStyle(14, FontWeight.Medium, 0.1, 1.43, baseline, true),
        bodyLarge: GeometryStyle(16, FontWeight.Normal, 0.5, 1.5, baseline, true),
        bodyMedium: GeometryStyle(14, FontWeight.Normal, 0.25, 1.43, baseline, true),
        bodySmall: GeometryStyle(12, FontWeight.Normal, 0.4, 1.33, baseline, true),
        labelLarge: GeometryStyle(14, FontWeight.Medium, 0.1, 1.43, baseline, true),
        labelMedium: GeometryStyle(12, FontWeight.Medium, 0.5, 1.33, baseline, true),
        labelSmall: GeometryStyle(11, FontWeight.Medium, 0.5, 1.45, baseline, true));

    private static TextStyle GeometryStyle(
        double fontSize,
        FontWeight fontWeight,
        double? letterSpacing = null,
        double? height = null,
        TextBaseline baseline = TextBaseline.Alphabetic,
        bool evenLeading = false)
    {
        return new TextStyle(
            FontSize: fontSize,
            FontWeight: fontWeight,
            Height: height,
            LetterSpacing: letterSpacing,
            Inherit: false,
            TextBaseline: baseline,
            LeadingDistribution: evenLeading ? TextLeadingDistribution.Even : null);
    }

    private enum PlatformTypeface
    {
        MountainView,
        Redmond,
        Helsinki,
        Cupertino,
        RedwoodCity,
    }
}

public enum ScriptCategory
{
    EnglishLike,
    Dense,
    Tall,
}
