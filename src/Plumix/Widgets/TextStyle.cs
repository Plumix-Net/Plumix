using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using FontFeature = Plumix.UI.FontFeature;
using TextDecoration = Plumix.UI.TextDecoration;
using TextDecorationStyle = Plumix.UI.TextDecorationStyle;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/painting/text_style.dart

/// An immutable style describing how to format and paint text.
///
/// Dart's `fontFamily` is a `String` and `fontWeight`/`fontStyle`/`color` are dart:ui types; Plumix
/// uses Avalonia's <see cref="Avalonia.Media.FontFamily"/>, <see cref="Avalonia.Media.FontWeight"/>,
/// <see cref="Avalonia.Media.FontStyle"/> and <see cref="Avalonia.Media.Color"/>, the same way
/// <see cref="StrutStyle"/> does, and applies the `packages/` prefix to the family name. A
/// <see cref="Avalonia.Media.FontWeight"/> may hold any value from 1 to 1000, as Dart's does.
public class TextStyle : Diagnosticable, IEquatable<TextStyle>
{
    // Dart's `_kDefaultDebugLabel`.
    private const string DefaultDebugLabel = "unknown";

    // Dart's `_kColorForegroundWarning`.
    private const string ColorForegroundWarning =
        "Cannot provide both a color and a foreground\n"
        + "The color argument is just a shorthand for \"foreground: Paint()..color = color\".";

    // Dart's `_kColorBackgroundWarning` (which also says `= color`).
    private const string ColorBackgroundWarning =
        "Cannot provide both a backgroundColor and a background\n"
        + "The backgroundColor argument is just a shorthand for \"background: Paint()..color = color\".";

    private readonly IReadOnlyList<string>? _fontFamilyFallback;
    private readonly string? _package;

    /// Creates a text style.
    ///
    /// The `Package` argument must be non-null if the font family is defined in a package. It is
    /// combined with the `FontFamily` argument (and every `FontFamilyFallback` entry) to set the
    /// <see cref="FontFamily"/> property.
    public TextStyle(
        bool Inherit = true,
        Color? Color = null,
        Color? BackgroundColor = null,
        double? FontSize = null,
        FontWeight? FontWeight = null,
        FontStyle? FontStyle = null,
        double? LetterSpacing = null,
        double? WordSpacing = null,
        TextBaseline? TextBaseline = null,
        double? Height = null,
        TextLeadingDistribution? LeadingDistribution = null,
        Locale? Locale = null,
        Paint? Foreground = null,
        Paint? Background = null,
        IReadOnlyList<Shadow>? Shadows = null,
        IReadOnlyList<FontFeature>? FontFeatures = null,
        IReadOnlyList<FontVariation>? FontVariations = null,
        TextDecoration? Decoration = null,
        Color? DecorationColor = null,
        TextDecorationStyle? DecorationStyle = null,
        double? DecorationThickness = null,
        string? DebugLabel = null,
        FontFamily? FontFamily = null,
        IReadOnlyList<string>? FontFamilyFallback = null,
        string? Package = null,
        TextOverflow? Overflow = null)
    {
        if (Constants.KDebugMode && Color is not null && Foreground is not null)
        {
            throw new AssertionError(ColorForegroundWarning);
        }

        if (Constants.KDebugMode && BackgroundColor is not null && Background is not null)
        {
            throw new AssertionError(ColorBackgroundWarning);
        }

        this.Inherit = Inherit;
        this.Color = Color;
        this.BackgroundColor = BackgroundColor;
        this.FontSize = FontSize;
        this.FontWeight = FontWeight;
        this.FontStyle = FontStyle;
        this.LetterSpacing = LetterSpacing;
        this.WordSpacing = WordSpacing;
        this.TextBaseline = TextBaseline;
        this.Height = Height;
        this.LeadingDistribution = LeadingDistribution;
        this.Locale = Locale;
        this.Foreground = Foreground;
        this.Background = Background;
        this.Shadows = Shadows;
        this.FontFeatures = FontFeatures;
        this.FontVariations = FontVariations;
        this.Decoration = Decoration;
        this.DecorationColor = DecorationColor;
        this.DecorationStyle = DecorationStyle;
        this.DecorationThickness = DecorationThickness;
        this.DebugLabel = DebugLabel;
        // Dart interpolates a null family as the literal "null".
        this.FontFamily = Package is null
            ? FontFamily
            : new FontFamily($"packages/{Package}/{FontFamily?.Name ?? "null"}");
        _fontFamilyFallback = FontFamilyFallback;
        _package = Package;
        this.Overflow = Overflow;
    }

    /// Whether null values in this style are replaced with the values from the ancestor style
    /// (for example, in a <see cref="TextSpan"/> tree). When false, null values fall back to the
    /// paragraph defaults.
    public bool Inherit { get; }

    /// The color to use when painting the text. Mutually exclusive with <see cref="Foreground"/>.
    public Color? Color { get; }

    /// The color to use as the background for the text. Mutually exclusive with
    /// <see cref="Background"/>.
    public Color? BackgroundColor { get; }

    /// The name of the font to use when painting the text, already prefixed with
    /// `packages/&lt;package&gt;/` when a package was given.
    public FontFamily? FontFamily { get; }

    /// The ordered list of font families to fall back on when a glyph cannot be found in a higher
    /// priority family. Prefixed with `packages/&lt;package&gt;/` when a package was given.
    public IReadOnlyList<string>? FontFamilyFallback =>
        _package is null
            ? _fontFamilyFallback
            : _fontFamilyFallback?.Select(family => $"packages/{_package}/{family}").ToList();

    /// Dart's private `_package`, exposed to the framework assemblies and tests.
    internal string? Package => _package;

    /// The size of fonts (in logical pixels) to use when painting the text.
    public double? FontSize { get; }

    /// The typeface thickness to use when painting the text.
    public FontWeight? FontWeight { get; }

    /// The typeface variant to use when drawing the letters (for example, italics).
    public FontStyle? FontStyle { get; }

    /// The amount of space (in logical pixels) to add between each letter.
    public double? LetterSpacing { get; }

    /// The amount of space (in logical pixels) to add at each sequence of white-space.
    public double? WordSpacing { get; }

    /// The common baseline that should be aligned between this text span and its parent.
    public TextBaseline? TextBaseline { get; }

    /// The height of this text span, as a multiple of the font size.
    public double? Height { get; }

    /// How the vertical space added by <see cref="Height"/> is distributed above and below the text.
    public TextLeadingDistribution? LeadingDistribution { get; }

    /// The locale used to select region-specific glyphs.
    public Locale? Locale { get; }

    /// The paint drawn as a foreground for the text. Mutually exclusive with <see cref="Color"/>.
    public Paint? Foreground { get; }

    /// The paint drawn as a background for the text. Mutually exclusive with
    /// <see cref="BackgroundColor"/>.
    public Paint? Background { get; }

    /// The decorations to paint near the text (for example, an underline).
    public TextDecoration? Decoration { get; }

    /// The color in which to paint the text decorations.
    public Color? DecorationColor { get; }

    /// The style in which to paint the text decorations (for example, dashed).
    public TextDecorationStyle? DecorationStyle { get; }

    /// The thickness of the decoration stroke as a multiplier of the thickness defined by the font.
    public double? DecorationThickness { get; }

    /// A human-readable description of this text style. Only maintained in debug builds; ignored
    /// by equality.
    public string? DebugLabel { get; }

    /// A list of shadows that will be painted underneath the text.
    public IReadOnlyList<Shadow>? Shadows { get; }

    /// A list of font features that affect the selection of glyphs in the font.
    public IReadOnlyList<FontFeature>? FontFeatures { get; }

    /// A list of font variations to apply to the font.
    public IReadOnlyList<FontVariation>? FontVariations { get; }

    /// How visual text overflow should be handled.
    public TextOverflow? Overflow { get; }

    // Dart's `_fontFamily`: the family without the package prefix.
    private FontFamily? UnprefixedFontFamily
    {
        get
        {
            if (_package is not null)
            {
                string fontFamilyPrefix = $"packages/{_package}/";
                if (Constants.KDebugMode && FontFamily is not null
                    && !FontFamily.Name.StartsWith(fontFamilyPrefix, StringComparison.Ordinal))
                {
                    throw new AssertionError("fontFamily?.startsWith(fontFamilyPrefix) ?? true");
                }

                return FontFamily is null ? null : new FontFamily(FontFamily.Name[fontFamilyPrefix.Length..]);
            }

            return FontFamily;
        }
    }

    /// Creates a copy of this text style but with the given fields replaced with the new values.
    ///
    /// One of `color` or `foreground` must be null, and if this has <see cref="Foreground"/>
    /// specified it will be given preference over any `color` parameter; the same holds for
    /// `backgroundColor` and `background`.
    public TextStyle CopyWith(
        bool? inherit = null,
        Color? color = null,
        Color? backgroundColor = null,
        double? fontSize = null,
        FontWeight? fontWeight = null,
        FontStyle? fontStyle = null,
        double? letterSpacing = null,
        double? wordSpacing = null,
        TextBaseline? textBaseline = null,
        double? height = null,
        TextLeadingDistribution? leadingDistribution = null,
        Locale? locale = null,
        Paint? foreground = null,
        Paint? background = null,
        IReadOnlyList<Shadow>? shadows = null,
        IReadOnlyList<FontFeature>? fontFeatures = null,
        IReadOnlyList<FontVariation>? fontVariations = null,
        TextDecoration? decoration = null,
        Color? decorationColor = null,
        TextDecorationStyle? decorationStyle = null,
        double? decorationThickness = null,
        string? debugLabel = null,
        FontFamily? fontFamily = null,
        IReadOnlyList<string>? fontFamilyFallback = null,
        string? package = null,
        TextOverflow? overflow = null)
    {
        if (Constants.KDebugMode && color is not null && foreground is not null)
        {
            throw new AssertionError(ColorForegroundWarning);
        }

        if (Constants.KDebugMode && backgroundColor is not null && background is not null)
        {
            throw new AssertionError(ColorBackgroundWarning);
        }

        string? newDebugLabel = null;
        if (Constants.KDebugMode)
        {
            if (debugLabel is not null)
            {
                newDebugLabel = debugLabel;
            }
            else if (DebugLabel is not null)
            {
                newDebugLabel = $"({DebugLabel}).copyWith";
            }
        }

        return new TextStyle(
            Inherit: inherit ?? Inherit,
            Color: Foreground is null && foreground is null ? color ?? Color : null,
            BackgroundColor: Background is null && background is null ? backgroundColor ?? BackgroundColor : null,
            FontSize: fontSize ?? FontSize,
            FontWeight: fontWeight ?? FontWeight,
            FontStyle: fontStyle ?? FontStyle,
            LetterSpacing: letterSpacing ?? LetterSpacing,
            WordSpacing: wordSpacing ?? WordSpacing,
            TextBaseline: textBaseline ?? TextBaseline,
            Height: height ?? Height,
            LeadingDistribution: leadingDistribution ?? LeadingDistribution,
            Locale: locale ?? Locale,
            Foreground: foreground ?? Foreground,
            Background: background ?? Background,
            Shadows: shadows ?? Shadows,
            FontFeatures: fontFeatures ?? FontFeatures,
            FontVariations: fontVariations ?? FontVariations,
            Decoration: decoration ?? Decoration,
            DecorationColor: decorationColor ?? DecorationColor,
            DecorationStyle: decorationStyle ?? DecorationStyle,
            DecorationThickness: decorationThickness ?? DecorationThickness,
            DebugLabel: newDebugLabel,
            FontFamily: fontFamily ?? UnprefixedFontFamily,
            FontFamilyFallback: fontFamilyFallback ?? _fontFamilyFallback,
            Package: package ?? _package,
            Overflow: overflow ?? Overflow);
    }

    /// Creates a copy of this text style replacing or altering the specified properties.
    ///
    /// The non-numeric properties are replaced with the given values when non-null. Numeric
    /// properties are multiplied by the given factors and then incremented by the given deltas;
    /// a numeric property that is null must be left with its default factor and delta.
    /// `fontWeightDelta` moves the weight by that many steps of 100, clamped to w100–w900.
    public TextStyle Apply(
        Color? color = null,
        Color? backgroundColor = null,
        TextDecoration? decoration = null,
        Color? decorationColor = null,
        TextDecorationStyle? decorationStyle = null,
        double decorationThicknessFactor = 1.0,
        double decorationThicknessDelta = 0.0,
        FontFamily? fontFamily = null,
        IReadOnlyList<string>? fontFamilyFallback = null,
        double fontSizeFactor = 1.0,
        double fontSizeDelta = 0.0,
        int fontWeightDelta = 0,
        FontStyle? fontStyle = null,
        double letterSpacingFactor = 1.0,
        double letterSpacingDelta = 0.0,
        double wordSpacingFactor = 1.0,
        double wordSpacingDelta = 0.0,
        double heightFactor = 1.0,
        double heightDelta = 0.0,
        TextBaseline? textBaseline = null,
        TextLeadingDistribution? leadingDistribution = null,
        Locale? locale = null,
        IReadOnlyList<Shadow>? shadows = null,
        IReadOnlyList<FontFeature>? fontFeatures = null,
        IReadOnlyList<FontVariation>? fontVariations = null,
        string? package = null,
        TextOverflow? overflow = null)
    {
        if (Constants.KDebugMode)
        {
            DebugAssert(
                FontSize is not null || (fontSizeFactor == 1.0 && fontSizeDelta == 0.0),
                "fontSize != null || (fontSizeFactor == 1.0 && fontSizeDelta == 0.0)");
            DebugAssert(
                FontWeight is not null || fontWeightDelta == 0.0,
                "fontWeight != null || fontWeightDelta == 0.0");
            DebugAssert(
                LetterSpacing is not null || (letterSpacingFactor == 1.0 && letterSpacingDelta == 0.0),
                "letterSpacing != null || (letterSpacingFactor == 1.0 && letterSpacingDelta == 0.0)");
            DebugAssert(
                WordSpacing is not null || (wordSpacingFactor == 1.0 && wordSpacingDelta == 0.0),
                "wordSpacing != null || (wordSpacingFactor == 1.0 && wordSpacingDelta == 0.0)");
            DebugAssert(
                DecorationThickness is not null
                || (decorationThicknessFactor == 1.0 && decorationThicknessDelta == 0.0),
                "decorationThickness != null || "
                + "(decorationThicknessFactor == 1.0 && decorationThicknessDelta == 0.0)");
        }

        string? modifiedDebugLabel = null;
        if (Constants.KDebugMode && DebugLabel is not null)
        {
            modifiedDebugLabel = $"({DebugLabel}).apply";
        }

        return new TextStyle(
            Inherit: Inherit,
            Color: Foreground is null ? color ?? Color : null,
            BackgroundColor: Background is null ? backgroundColor ?? BackgroundColor : null,
            FontFamily: fontFamily ?? UnprefixedFontFamily,
            FontFamilyFallback: fontFamilyFallback ?? _fontFamilyFallback,
            FontSize: FontSize is { } size ? (size * fontSizeFactor) + fontSizeDelta : null,
            FontWeight: FontWeight is { } weight
                ? FontWeightFromIndex(Math.Clamp(FontWeightIndex(weight) + fontWeightDelta, 0, 8))
                : null,
            FontStyle: fontStyle ?? FontStyle,
            LetterSpacing: LetterSpacing is { } letter ? (letter * letterSpacingFactor) + letterSpacingDelta : null,
            WordSpacing: WordSpacing is { } word ? (word * wordSpacingFactor) + wordSpacingDelta : null,
            TextBaseline: textBaseline ?? TextBaseline,
            Height: Height is null || Height == TextDefaults.TextHeightNone
                ? Height
                : (Height.Value * heightFactor) + heightDelta,
            LeadingDistribution: leadingDistribution ?? LeadingDistribution,
            Locale: locale ?? Locale,
            Foreground: Foreground,
            Background: Background,
            Shadows: shadows ?? Shadows,
            FontFeatures: fontFeatures ?? FontFeatures,
            FontVariations: fontVariations ?? FontVariations,
            Decoration: decoration ?? Decoration,
            DecorationColor: decorationColor ?? DecorationColor,
            DecorationStyle: decorationStyle ?? DecorationStyle,
            DecorationThickness: DecorationThickness is { } thickness
                ? (thickness * decorationThicknessFactor) + decorationThicknessDelta
                : null,
            Overflow: overflow ?? Overflow,
            Package: package ?? _package,
            DebugLabel: modifiedDebugLabel);
    }

    /// Returns a new text style that is a combination of this style and the given `other` style.
    ///
    /// If the given `other` text style has its <see cref="Inherit"/> set to true, its null
    /// properties are replaced with the non-null properties of this text style. If `other` has
    /// <see cref="Inherit"/> set to false, it is returned unmodified.
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

        string? mergedDebugLabel = null;
        if (Constants.KDebugMode && (other.DebugLabel is not null || DebugLabel is not null))
        {
            mergedDebugLabel =
                $"({DebugLabel ?? DefaultDebugLabel}).merge({other.DebugLabel ?? DefaultDebugLabel})";
        }

        return CopyWith(
            color: other.Color,
            backgroundColor: other.BackgroundColor,
            fontSize: other.FontSize,
            fontWeight: other.FontWeight,
            fontStyle: other.FontStyle,
            letterSpacing: other.LetterSpacing,
            wordSpacing: other.WordSpacing,
            textBaseline: other.TextBaseline,
            height: other.Height,
            leadingDistribution: other.LeadingDistribution,
            locale: other.Locale,
            foreground: other.Foreground,
            background: other.Background,
            shadows: other.Shadows,
            fontFeatures: other.FontFeatures,
            fontVariations: other.FontVariations,
            decoration: other.Decoration,
            decorationColor: other.DecorationColor,
            decorationStyle: other.DecorationStyle,
            decorationThickness: other.DecorationThickness,
            debugLabel: mergedDebugLabel,
            fontFamily: other.UnprefixedFontFamily,
            fontFamilyFallback: other._fontFamilyFallback,
            package: other._package,
            overflow: other.Overflow);
    }

    /// Interpolate between two text styles for animated transitions.
    ///
    /// Interpolation between two <see cref="TextStyle"/>s with different <see cref="Inherit"/>
    /// values throws in debug builds when a field is unspecified in both styles, because such
    /// fields may jump during the transition.
    public static TextStyle? Lerp(TextStyle? a, TextStyle? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        string? lerpDebugLabel = null;
        if (Constants.KDebugMode)
        {
            string fraction = t.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            lerpDebugLabel =
                $"lerp({a?.DebugLabel ?? DefaultDebugLabel} ⎯{fraction}→ {b?.DebugLabel ?? DefaultDebugLabel})";
        }

        if (a is null)
        {
            return LerpFromNull(b!, t, lerpDebugLabel);
        }

        if (b is null)
        {
            return LerpToNull(a, t, lerpDebugLabel);
        }

        if (Constants.KDebugMode && a.Inherit != b.Inherit)
        {
            ThrowIfInheritMismatchHasUnspecifiedFields(a, b);
        }

        return new TextStyle(
            Inherit: t < 0.5 ? a.Inherit : b.Inherit,
            Color: a.Foreground is null && b.Foreground is null
                ? ColorUtilities.Lerp(a.Color, b.Color, t)
                : null,
            BackgroundColor: a.Background is null && b.Background is null
                ? ColorUtilities.Lerp(a.BackgroundColor, b.BackgroundColor, t)
                : null,
            FontSize: LerpDouble(a.FontSize ?? b.FontSize, b.FontSize ?? a.FontSize, t),
            FontWeight: LerpFontWeight(a.FontWeight, b.FontWeight, t),
            FontStyle: t < 0.5 ? a.FontStyle : b.FontStyle,
            LetterSpacing: LerpDouble(a.LetterSpacing ?? b.LetterSpacing, b.LetterSpacing ?? a.LetterSpacing, t),
            WordSpacing: LerpDouble(a.WordSpacing ?? b.WordSpacing, b.WordSpacing ?? a.WordSpacing, t),
            TextBaseline: t < 0.5 ? a.TextBaseline : b.TextBaseline,
            Height: LerpDouble(a.Height ?? b.Height, b.Height ?? a.Height, t),
            LeadingDistribution: t < 0.5 ? a.LeadingDistribution : b.LeadingDistribution,
            Locale: t < 0.5 ? a.Locale : b.Locale,
            Foreground: a.Foreground is not null || b.Foreground is not null
                ? t < 0.5
                    ? a.Foreground ?? new Paint { Color = a.Color!.Value }
                    : b.Foreground ?? new Paint { Color = b.Color!.Value }
                : null,
            Background: a.Background is not null || b.Background is not null
                ? t < 0.5
                    ? a.Background ?? new Paint { Color = a.BackgroundColor!.Value }
                    : b.Background ?? new Paint { Color = b.BackgroundColor!.Value }
                : null,
            Shadows: Shadow.LerpList(a.Shadows, b.Shadows, t),
            FontFeatures: t < 0.5 ? a.FontFeatures : b.FontFeatures,
            FontVariations: LerpFontVariations(a.FontVariations, b.FontVariations, t),
            Decoration: t < 0.5 ? a.Decoration : b.Decoration,
            DecorationColor: ColorUtilities.Lerp(a.DecorationColor, b.DecorationColor, t),
            DecorationStyle: t < 0.5 ? a.DecorationStyle : b.DecorationStyle,
            DecorationThickness: LerpDouble(
                a.DecorationThickness ?? b.DecorationThickness,
                b.DecorationThickness ?? a.DecorationThickness,
                t),
            DebugLabel: lerpDebugLabel,
            FontFamily: t < 0.5 ? a.UnprefixedFontFamily : b.UnprefixedFontFamily,
            FontFamilyFallback: t < 0.5 ? a._fontFamilyFallback : b._fontFamilyFallback,
            Package: t < 0.5 ? a._package : b._package,
            Overflow: t < 0.5 ? a.Overflow : b.Overflow);
    }

    private static TextStyle LerpFromNull(TextStyle b, double t, string? lerpDebugLabel)
    {
        bool first = t < 0.5;
        return new TextStyle(
            Inherit: b.Inherit,
            Color: ColorUtilities.Lerp(null, b.Color, t),
            BackgroundColor: ColorUtilities.Lerp(null, b.BackgroundColor, t),
            FontSize: first ? null : b.FontSize,
            FontWeight: LerpFontWeight(null, b.FontWeight, t),
            FontStyle: first ? null : b.FontStyle,
            LetterSpacing: first ? null : b.LetterSpacing,
            WordSpacing: first ? null : b.WordSpacing,
            TextBaseline: first ? null : b.TextBaseline,
            Height: first ? null : b.Height,
            LeadingDistribution: first ? null : b.LeadingDistribution,
            Locale: first ? null : b.Locale,
            Foreground: first ? null : b.Foreground,
            Background: first ? null : b.Background,
            Shadows: first ? null : b.Shadows,
            FontFeatures: first ? null : b.FontFeatures,
            FontVariations: LerpFontVariations(null, b.FontVariations, t),
            Decoration: first ? null : b.Decoration,
            DecorationColor: ColorUtilities.Lerp(null, b.DecorationColor, t),
            DecorationStyle: first ? null : b.DecorationStyle,
            DecorationThickness: first ? null : b.DecorationThickness,
            DebugLabel: lerpDebugLabel,
            FontFamily: first ? null : b.UnprefixedFontFamily,
            FontFamilyFallback: first ? null : b._fontFamilyFallback,
            Package: first ? null : b._package,
            Overflow: first ? null : b.Overflow);
    }

    private static TextStyle LerpToNull(TextStyle a, double t, string? lerpDebugLabel)
    {
        bool first = t < 0.5;
        return new TextStyle(
            Inherit: a.Inherit,
            Color: ColorUtilities.Lerp(a.Color, null, t),
            // Dart passes the arguments in this order too.
            BackgroundColor: ColorUtilities.Lerp(null, a.BackgroundColor, t),
            FontSize: first ? a.FontSize : null,
            FontWeight: LerpFontWeight(a.FontWeight, null, t),
            FontStyle: first ? a.FontStyle : null,
            LetterSpacing: first ? a.LetterSpacing : null,
            WordSpacing: first ? a.WordSpacing : null,
            TextBaseline: first ? a.TextBaseline : null,
            Height: first ? a.Height : null,
            LeadingDistribution: first ? a.LeadingDistribution : null,
            Locale: first ? a.Locale : null,
            Foreground: first ? a.Foreground : null,
            Background: first ? a.Background : null,
            Shadows: first ? a.Shadows : null,
            FontFeatures: first ? a.FontFeatures : null,
            FontVariations: LerpFontVariations(a.FontVariations, null, t),
            Decoration: first ? a.Decoration : null,
            DecorationColor: ColorUtilities.Lerp(a.DecorationColor, null, t),
            DecorationStyle: first ? a.DecorationStyle : null,
            DecorationThickness: first ? a.DecorationThickness : null,
            DebugLabel: lerpDebugLabel,
            FontFamily: first ? a.UnprefixedFontFamily : null,
            FontFamilyFallback: first ? a._fontFamilyFallback : null,
            Package: first ? a._package : null,
            Overflow: first ? a.Overflow : null);
    }

    private static void ThrowIfInheritMismatchHasUnspecifiedFields(TextStyle a, TextStyle b)
    {
        var nullFields = new List<string>();
        if (a.Foreground is null && b.Foreground is null && a.Color is null && b.Color is null)
        {
            nullFields.Add("color");
        }

        if (a.Background is null && b.Background is null
            && a.BackgroundColor is null && b.BackgroundColor is null)
        {
            nullFields.Add("backgroundColor");
        }

        if (a.FontSize is null && b.FontSize is null)
        {
            nullFields.Add("fontSize");
        }

        if (a.LetterSpacing is null && b.LetterSpacing is null)
        {
            nullFields.Add("letterSpacing");
        }

        if (a.WordSpacing is null && b.WordSpacing is null)
        {
            nullFields.Add("wordSpacing");
        }

        if (a.Height is null && b.Height is null)
        {
            nullFields.Add("height");
        }

        if (a.DecorationColor is null && b.DecorationColor is null)
        {
            nullFields.Add("decorationColor");
        }

        if (a.DecorationThickness is null && b.DecorationThickness is null)
        {
            nullFields.Add("decorationThickness");
        }

        if (nullFields.Count == 0)
        {
            return;
        }

        throw new FlutterError(
        [
            new ErrorSummary("Failed to interpolate TextStyles with different inherit values."),
            new ErrorSpacer(),
            new ErrorDescription("The TextStyles being interpolated were:"),
            a.ToDiagnosticsNode(name: "from", style: DiagnosticsTreeStyle.SingleLine),
            b.ToDiagnosticsNode(name: "to", style: DiagnosticsTreeStyle.SingleLine),
            new ErrorDescription(
                "The following fields are unspecified in both TextStyles:\n"
                + string.Join(", ", nullFields.Select(name => $"\"{name}\""))
                + ".\nWhen \"inherit\" changes during the transition, these fields may observe abrupt value "
                + "changes as a result, causing \"jump\"s in the transition."),
            new ErrorSpacer(),
            new ErrorHint(
                "In general, TextStyle.lerp only works well when both TextStyles have the same \"inherit\" "
                + "value, and specify the same fields."),
            new ErrorHint(
                "If the TextStyles were directly created by you, consider bringing them to parity to ensure a "
                + "smooth transition."),
            new ErrorSpacer(),
            new ErrorHint(
                "If one of the TextStyles being lerped is significantly more elaborate than the other, and has "
                + "\"inherited\" set to false, it is often because it is merged with another TextStyle before "
                + "being lerped. Comparing the \"debugLabel\"s of the two TextStyles may help identify if that "
                + "was the case."),
            new ErrorHint(
                "For example, you may see this error message when trying to lerp between \"ThemeData()\" and "
                + "\"Theme.of(context)\". This is because TextStyles from \"Theme.of(context)\" are merged with "
                + "TextStyles from another theme and thus are more elaborate than the TextStyles from "
                + "\"ThemeData()\" (which is reflected in their \"debugLabel\"s -- TextStyles from "
                + "\"Theme.of(context)\" should have labels in the form of \"(<A TextStyle>).merge(<Another "
                + "TextStyle>)\"). It is recommended to only lerp ThemeData with matching TextStyles."),
        ]);
    }

    /// The style information for text runs, encoded for use by the paragraph engine.
    ///
    /// Only the font size is scaled; `textScaleFactor` is Dart's deprecated linear factor and
    /// cannot be combined with a `textScaler`.
    public ParagraphTextStyle GetTextStyle(double textScaleFactor = 1.0, TextScaler? textScaler = null)
    {
        TextScaler scaler = textScaler ?? TextScaler.NoScaling;
        if (Constants.KDebugMode && !(scaler == TextScaler.NoScaling || textScaleFactor == 1.0))
        {
            throw new AssertionError(
                "textScaleFactor is deprecated and cannot be specified when textScaler is specified.");
        }

        double? fontSize = FontSize switch
        {
            null => null,
            { } size when scaler == TextScaler.NoScaling => size * textScaleFactor,
            { } size => scaler.Scale(size),
        };
        return new ParagraphTextStyle(
            Color: Color,
            Decoration: Decoration,
            DecorationColor: DecorationColor,
            DecorationStyle: DecorationStyle,
            DecorationThickness: DecorationThickness,
            FontWeight: FontWeight,
            FontStyle: FontStyle,
            TextBaseline: TextBaseline,
            LeadingDistribution: LeadingDistribution,
            FontFamily: FontFamily,
            FontFamilyFallback: FontFamilyFallback,
            FontSize: fontSize,
            LetterSpacing: LetterSpacing,
            WordSpacing: WordSpacing,
            Height: Height,
            Locale: Locale?.ToLanguageTag(),
            Foreground: Foreground,
            Background: Background ?? (BackgroundColor is { } backgroundColor
                ? new Paint { Color = backgroundColor }
                : null),
            Shadows: Shadows,
            FontFeatures: FontFeatures,
            FontVariations: FontVariations);
    }

    /// The style information for paragraphs, encoded for use by the paragraph engine.
    ///
    /// The `textAlign`, `textDirection`, `ellipsis`, `maxLines` and `locale` values come only from
    /// the arguments (this style's <see cref="Locale"/> is not used); font family, size, weight,
    /// style and height fall back to this style. The font size defaults to 14 before `textScaler` is
    /// applied. An explicit `textHeightBehavior` wins over this style's leading distribution.
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
        if (Constants.KDebugMode && maxLines is <= 0)
        {
            throw new AssertionError("maxLines == null || maxLines > 0");
        }

        TextScaler scaler = textScaler ?? TextScaler.NoScaling;
        TextLeadingDistribution? leadingDistribution = LeadingDistribution;
        TextHeightBehavior? effectiveTextHeightBehavior = textHeightBehavior
                                                          ?? (leadingDistribution is { } distribution
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

    /// Describe the difference between this style and another, in terms of how much damage it will
    /// make to the rendering.
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
            || !Equals(Locale, other.Locale)
            || !ReferenceEquals(Foreground, other.Foreground)
            || !ReferenceEquals(Background, other.Background)
            || !ListEquals(Shadows, other.Shadows)
            || !ListEquals(FontFeatures, other.FontFeatures)
            || !ListEquals(FontVariations, other.FontVariations)
            || !ListEquals(FontFamilyFallback, other.FontFamilyFallback)
            || Overflow != other.Overflow)
        {
            return RenderComparison.Layout;
        }

        if (Color != other.Color
            || BackgroundColor != other.BackgroundColor
            || Decoration != other.Decoration
            || DecorationColor != other.DecorationColor
            || DecorationStyle != other.DecorationStyle
            || DecorationThickness != other.DecorationThickness)
        {
            return RenderComparison.Paint;
        }

        return RenderComparison.Identical;
    }

    public virtual bool Equals(TextStyle? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null
               && other.GetType() == GetType()
               && other.Inherit == Inherit
               && other.Color == Color
               && other.BackgroundColor == BackgroundColor
               && other.FontSize == FontSize
               && other.FontWeight == FontWeight
               && other.FontStyle == FontStyle
               && other.LetterSpacing == LetterSpacing
               && other.WordSpacing == WordSpacing
               && other.TextBaseline == TextBaseline
               && other.Height == Height
               && other.LeadingDistribution == LeadingDistribution
               && Equals(other.Locale, Locale)
               && ReferenceEquals(other.Foreground, Foreground)
               && ReferenceEquals(other.Background, Background)
               && ListEquals(other.Shadows, Shadows)
               && ListEquals(other.FontFeatures, FontFeatures)
               && ListEquals(other.FontVariations, FontVariations)
               && other.Decoration == Decoration
               && other.DecorationColor == DecorationColor
               && other.DecorationStyle == DecorationStyle
               && other.DecorationThickness == DecorationThickness
               && Equals(other.FontFamily, FontFamily)
               && ListEquals(other.FontFamilyFallback, FontFamilyFallback)
               && string.Equals(other._package, _package, StringComparison.Ordinal)
               && other.Overflow == Overflow;
    }

    public override bool Equals(object? obj) => Equals(obj as TextStyle);

    public override int GetHashCode()
    {
        IReadOnlyList<string>? fontFamilyFallback = FontFamilyFallback;
        int fontHash = HashCode.Combine(
            DecorationStyle,
            DecorationThickness,
            FontFamily,
            fontFamilyFallback is null ? (int?)null : HashAll(fontFamilyFallback),
            _package,
            Overflow);
        var hash = new HashCode();
        hash.Add(Inherit);
        hash.Add(Color);
        hash.Add(BackgroundColor);
        hash.Add(FontSize);
        hash.Add(FontWeight);
        hash.Add(FontStyle);
        hash.Add(LetterSpacing);
        hash.Add(WordSpacing);
        hash.Add(TextBaseline);
        hash.Add(Height);
        hash.Add(LeadingDistribution);
        hash.Add(Locale);
        hash.Add(Foreground);
        hash.Add(Background);
        hash.Add(Shadows is null ? (int?)null : HashAll(Shadows));
        hash.Add(FontFeatures is null ? (int?)null : HashAll(FontFeatures));
        hash.Add(FontVariations is null ? (int?)null : HashAll(FontVariations));
        hash.Add(Decoration);
        hash.Add(DecorationColor);
        hash.Add(fontHash);
        return hash.ToHashCode();
    }

    public static bool operator ==(TextStyle? left, TextStyle? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(TextStyle? left, TextStyle? right) => !(left == right);

    public override string ToStringShort() => Diagnostics.ObjectRuntimeType(this, "TextStyle");

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        DebugFillProperties(properties, prefix: string.Empty);
    }

    /// Adds all properties prefixing property names with the optional `prefix`.
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties, string prefix)
    {
        base.DebugFillProperties(properties);
        if (DebugLabel is not null)
        {
            properties.Add(new MessageProperty($"{prefix}debugLabel", DebugLabel));
        }

        // Dart's `defaultValue: null`.
        object nullDefault = DiagnosticsDefaults.NullValue;
        var styles = new List<DiagnosticsNode>
        {
            new ColorProperty($"{prefix}color", Color, defaultValue: nullDefault),
            new ColorProperty($"{prefix}backgroundColor", BackgroundColor, defaultValue: nullDefault),
            new StringProperty($"{prefix}family", FontFamily?.Name, defaultValue: nullDefault, quoted: false),
            new IterableProperty<string>($"{prefix}familyFallback", FontFamilyFallback, defaultValue: nullDefault),
            new DoubleProperty($"{prefix}size", FontSize, defaultValue: nullDefault),
        };
        string? weightDescription = null;
        if (FontWeight is { } fontWeight)
        {
            weightDescription = $"{FontWeightIndex(fontWeight) + 1}00";
        }

        // TODO(jacobr): switch this to use enumProperty which will either cause the weight description
        // to change to w600 from 600 or require existing enumProperty to handle this special case.
        styles.Add(new DiagnosticsProperty<FontWeight?>(
            $"{prefix}weight",
            FontWeight,
            description: weightDescription,
            defaultValue: nullDefault));
        styles.Add(new EnumProperty<FontStyle>($"{prefix}style", FontStyle, defaultValue: nullDefault));
        styles.Add(new DoubleProperty($"{prefix}letterSpacing", LetterSpacing, defaultValue: nullDefault));
        styles.Add(new DoubleProperty($"{prefix}wordSpacing", WordSpacing, defaultValue: nullDefault));
        styles.Add(new EnumProperty<TextBaseline>($"{prefix}baseline", TextBaseline, defaultValue: nullDefault));
        styles.Add(new DoubleProperty($"{prefix}height", Height, unit: "x", defaultValue: nullDefault));
        styles.Add(new EnumProperty<TextLeadingDistribution>(
            $"{prefix}leadingDistribution",
            LeadingDistribution,
            defaultValue: nullDefault));
        styles.Add(new DiagnosticsProperty<Locale>($"{prefix}locale", Locale, defaultValue: nullDefault));
        styles.Add(new DiagnosticsProperty<Paint>($"{prefix}foreground", Foreground, defaultValue: nullDefault));
        styles.Add(new DiagnosticsProperty<Paint>($"{prefix}background", Background, defaultValue: nullDefault));
        if (Decoration is not null || DecorationColor is not null || DecorationStyle is not null
            || DecorationThickness is not null)
        {
            var decorationDescription = new List<string>();
            if (DecorationStyle is { } decorationStyle)
            {
                decorationDescription.Add(Diagnostics.EnumName(decorationStyle));
            }

            // Hide decorationColor from the default text view as it is shown in the terse decoration
            // summary as well.
            styles.Add(new ColorProperty(
                $"{prefix}decorationColor",
                DecorationColor,
                defaultValue: nullDefault,
                level: DiagnosticLevel.Fine));

            if (DecorationColor is { } decorationColor)
            {
                decorationDescription.Add(decorationColor.ToDartString());
            }

            // Intentionally collide with the property 'decoration' added below. Tools that show hidden
            // properties could choose the first property matching the name to disambiguate.
            styles.Add(new DiagnosticsProperty<TextDecoration?>(
                $"{prefix}decoration",
                Decoration,
                defaultValue: nullDefault,
                level: DiagnosticLevel.Hidden));
            if (Decoration is { } decoration)
            {
                decorationDescription.Add(ParagraphTextStyle.DescribeDecoration(decoration));
            }

            if (Constants.KDebugMode && decorationDescription.Count == 0)
            {
                throw new AssertionError("decorationDescription.isNotEmpty");
            }

            styles.Add(new MessageProperty($"{prefix}decoration", string.Join(" ", decorationDescription)));
            styles.Add(new DoubleProperty(
                $"{prefix}decorationThickness",
                DecorationThickness,
                unit: "x",
                defaultValue: nullDefault));
        }

        bool styleSpecified = styles.Any(node => !node.IsFiltered(DiagnosticLevel.Info));
        properties.Add(new DiagnosticsProperty<bool>(
            $"{prefix}inherit",
            Inherit,
            level: !styleSpecified && Inherit ? DiagnosticLevel.Fine : DiagnosticLevel.Info));
        foreach (DiagnosticsNode style in styles)
        {
            properties.Add(style);
        }

        if (!styleSpecified)
        {
            properties.Add(new FlagProperty(
                "inherit",
                Inherit,
                ifTrue: $"{prefix}<all styles inherited>",
                ifFalse: $"{prefix}<no style specified>"));
        }

        // Dart adds the overflow property to `styles` after they were copied into `properties`, so it
        // is never reported.
        styles.Add(new EnumProperty<TextOverflow>($"{prefix}overflow", Overflow, defaultValue: nullDefault));
    }

    /// Interpolate between two lists of <see cref="FontVariation"/> objects.
    ///
    /// Dart's top-level `lerpFontVariations`. Variations are paired by position while their axes
    /// match; the remaining ones are paired by axis (in no particular order), and an axis present on
    /// only one side switches at `t == 0.5`.
    public static IReadOnlyList<FontVariation>? LerpFontVariations(
        IReadOnlyList<FontVariation>? a,
        IReadOnlyList<FontVariation>? b,
        double t)
    {
        if (t == 0.0)
        {
            return a;
        }

        if (t == 1.0)
        {
            return b;
        }

        if (a is null || a.Count == 0 || b is null || b.Count == 0)
        {
            // If one side is empty, that means anything on the other side will use the default
            // values for the axes, so there's nothing to interpolate.
            return t < 0.5 ? a : b;
        }

        var result = new List<FontVariation>();
        int index = 0;
        int minLength = Math.Min(a.Count, b.Count);
        for (; index < minLength; index += 1)
        {
            // The usual case is that the lists have the same axes in the same order.
            if (!string.Equals(a[index].Axis, b[index].Axis, StringComparison.Ordinal))
            {
                break;
            }

            result.Add(FontVariation.Lerp(a[index], b[index], t)!);
        }

        int maxLength = Math.Max(a.Count, b.Count);
        if (index < maxLength)
        {
            // If we get here, the lists are not the same axes in the same order, so do it the hard
            // way: pair the remaining variations by axis, the last duplicate winning.
            var axes = new HashSet<string>(StringComparer.Ordinal);
            var aVariations = new Dictionary<string, FontVariation>(StringComparer.Ordinal);
            for (int indexA = index; indexA < a.Count; indexA += 1)
            {
                aVariations[a[indexA].Axis] = a[indexA];
                axes.Add(a[indexA].Axis);
            }

            var bVariations = new Dictionary<string, FontVariation>(StringComparer.Ordinal);
            for (int indexB = index; indexB < b.Count; indexB += 1)
            {
                bVariations[b[indexB].Axis] = b[indexB];
                axes.Add(b[indexB].Axis);
            }

            foreach (string axis in axes)
            {
                FontVariation? variation = FontVariation.Lerp(
                    aVariations.GetValueOrDefault(axis),
                    bVariations.GetValueOrDefault(axis),
                    t);
                if (variation is not null)
                {
                    result.Add(variation);
                }
            }
        }

        return result;
    }

    // dart:ui's deprecated `FontWeight.index`: `(value ~/ 100 - 1).clamp(0, 8)`.
    internal static int FontWeightIndex(FontWeight weight) => Math.Clamp(((int)weight / 100) - 1, 0, 8);

    // dart:ui `FontWeight.values[index]`.
    private static FontWeight FontWeightFromIndex(int index) => (FontWeight)((index + 1) * 100);

    /// dart:ui `FontWeight.lerp`: rounds the interpolated value and clamps it to w100–w900, with a
    /// null side standing for w400. The result need not be a multiple of 100.
    internal static FontWeight? LerpFontWeight(FontWeight? a, FontWeight? b, double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        int from = (int)(a ?? Avalonia.Media.FontWeight.Normal);
        int to = (int)(b ?? Avalonia.Media.FontWeight.Normal);
        // dart:ui `_lerpInt`, then `round()` (half away from zero) and `clamp(100, 900)`.
        double value = from + ((to - from) * t);
        return (FontWeight)Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 100, 900);
    }

    // dart:ui `lerpDouble`.
    private static double? LerpDouble(double? a, double? b, double t)
    {
        if (a == b || (a is { } x && double.IsNaN(x) && b is { } y && double.IsNaN(y)))
        {
            return a;
        }

        double from = a ?? 0.0;
        double to = b ?? 0.0;
        return (from * (1.0 - t)) + (to * t);
    }

    private static void DebugAssert(bool condition, string message)
    {
        if (!condition)
        {
            throw new AssertionError(message);
        }
    }

    private static bool ListEquals<T>(IReadOnlyList<T>? a, IReadOnlyList<T>? b)
    {
        if (a is null)
        {
            return b is null;
        }

        if (b is null || a.Count != b.Count)
        {
            return false;
        }

        if (ReferenceEquals(a, b))
        {
            return true;
        }

        return a.SequenceEqual(b);
    }

    private static int HashAll<T>(IReadOnlyList<T> values)
    {
        var hash = new HashCode();
        foreach (T value in values)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    /// The style every Plumix text path falls back to when no ancestor provides one.
    internal static TextStyle Fallback { get; } = new(
        FontFamily: Avalonia.Media.FontFamily.Default,
        FontSize: 14,
        Color: Colors.Black,
        FontWeight: Avalonia.Media.FontWeight.Normal,
        FontStyle: Avalonia.Media.FontStyle.Normal);
}
