using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/painting/strut_style.dart

namespace Plumix.Widgets;

/// Defines the strut, which sets the minimum height a line can be relative to the baseline.
///
/// Strut applies to all lines in the paragraph. Every field is nullable; a null field defers to
/// the engine default (`fontSize` 14, `fontWeight` w400, `fontStyle` normal, `forceStrutHeight`
/// false, font-specified leading).
///
/// Dart's `fontFamily` is a `String`; Plumix types it as <see cref="Avalonia.Media.FontFamily"/>,
/// the same way <see cref="TextStyle.FontFamily"/> is typed, and applies the `packages/` prefix to
/// the family name.
public sealed class StrutStyle : Diagnosticable, IEquatable<StrutStyle>
{
    private readonly IReadOnlyList<string>? _fontFamilyFallback;
    private readonly string? _package;

    /// Creates a strut style.
    ///
    /// When `Package` is non-null the family name and every fallback name are prefixed with
    /// `packages/<package>/`, exactly as Dart does.
    public StrutStyle(
        FontFamily? FontFamily = null,
        IReadOnlyList<string>? FontFamilyFallback = null,
        double? FontSize = null,
        double? Height = null,
        TextLeadingDistribution? LeadingDistribution = null,
        double? Leading = null,
        FontWeight? FontWeight = null,
        FontStyle? FontStyle = null,
        bool? ForceStrutHeight = null,
        string? DebugLabel = null,
        string? Package = null)
    {
        if (FontSize is not null && !(FontSize > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(FontSize), "fontSize == null || fontSize > 0");
        }

        if (Leading is not null && !(Leading >= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(Leading), "leading == null || leading >= 0");
        }

        if (Package is not null && FontFamily is null && FontFamilyFallback is null)
        {
            throw new ArgumentException(
                "package == null || (fontFamily != null || fontFamilyFallback != null)",
                nameof(Package));
        }

        this.FontFamily = Package is null ? FontFamily : PrefixFamily(Package, FontFamily);
        _fontFamilyFallback = FontFamilyFallback;
        _package = Package;
        this.FontSize = FontSize;
        this.Height = Height;
        this.LeadingDistribution = LeadingDistribution;
        this.Leading = Leading;
        this.FontWeight = FontWeight;
        this.FontStyle = FontStyle;
        this.ForceStrutHeight = ForceStrutHeight;
        this.DebugLabel = DebugLabel;
    }

    /// Builds a strut style that defaults to the values of `textStyle`.
    ///
    /// `Leading` and `ForceStrutHeight` never come from the text style. As in Dart, a non-null
    /// `package` is applied to an explicit `fontFamily` here and again by the main constructor.
    public static StrutStyle FromTextStyle(
        TextStyle textStyle,
        FontFamily? fontFamily = null,
        IReadOnlyList<string>? fontFamilyFallback = null,
        double? fontSize = null,
        double? height = null,
        TextLeadingDistribution? leadingDistribution = null,
        double? leading = null,
        FontWeight? fontWeight = null,
        FontStyle? fontStyle = null,
        bool? forceStrutHeight = null,
        string? debugLabel = null,
        string? package = null)
    {
        ArgumentNullException.ThrowIfNull(textStyle);
        return new StrutStyle(
            FontFamily: fontFamily is not null
                ? (package is null ? fontFamily : PrefixFamily(package, fontFamily))
                : textStyle.FontFamily,
            FontFamilyFallback: fontFamilyFallback ?? textStyle.FontFamilyFallback,
            Height: height ?? textStyle.Height,
            LeadingDistribution: leadingDistribution ?? textStyle.LeadingDistribution,
            FontSize: fontSize ?? textStyle.FontSize,
            Leading: leading,
            FontWeight: fontWeight ?? textStyle.FontWeight,
            FontStyle: fontStyle ?? textStyle.FontStyle,
            ForceStrutHeight: forceStrutHeight,
            DebugLabel: debugLabel,
            Package: package);
    }

    /// A strut style that disables strut behavior: zero height and zero leading.
    public static StrutStyle Disabled { get; } = new(Height: 0.0, Leading: 0.0);

    /// The name of the font to use when calculating the strut.
    public FontFamily? FontFamily { get; }

    /// The ordered list of font families to fall back on when a higher priority family cannot be
    /// found. Prefixed with `packages/<package>/` on every read when a package was given.
    public IReadOnlyList<string>? FontFamilyFallback
    {
        get
        {
            if (_package is not null && _fontFamilyFallback is not null)
            {
                return _fontFamilyFallback.Select(family => $"packages/{_package}/{family}").ToList();
            }

            return _fontFamilyFallback;
        }
    }

    /// The size of text (in logical pixels) to use when obtaining metrics from the font.
    public double? FontSize { get; }

    /// The minimum height of the strut, as a multiple of `FontSize`.
    public double? Height { get; }

    /// How the vertical space added by `Height` is distributed above and below the text.
    public TextLeadingDistribution? LeadingDistribution { get; }

    /// The typeface thickness to use when calculating the strut.
    public FontWeight? FontWeight { get; }

    /// The typeface variant to use when calculating the strut.
    public FontStyle? FontStyle { get; }

    /// The additional leading to apply to the strut as a multiple of `FontSize`.
    public double? Leading { get; }

    /// Whether the strut height should be forced.
    public bool? ForceStrutHeight { get; }

    /// A human-readable description of this strut style. Ignored by equality.
    public string? DebugLabel { get; }

    /// Describes the difference between this style and another in terms of rendering damage.
    ///
    /// Only <see cref="RenderComparison.Identical"/> and <see cref="RenderComparison.Layout"/> are
    /// ever returned.
    public RenderComparison CompareTo(StrutStyle other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (ReferenceEquals(this, other))
        {
            return RenderComparison.Identical;
        }

        if (!Equals(FontFamily, other.FontFamily)
            || FontSize != other.FontSize
            || FontWeight != other.FontWeight
            || FontStyle != other.FontStyle
            || Height != other.Height
            || Leading != other.Leading
            || ForceStrutHeight != other.ForceStrutHeight
            || !ListEquals(FontFamilyFallback, other.FontFamilyFallback)
            || (Height is not null && LeadingDistribution != other.LeadingDistribution))
        {
            return RenderComparison.Layout;
        }

        return RenderComparison.Identical;
    }

    /// Returns a new strut style that inherits every null property from `other`.
    ///
    /// `LeadingDistribution` is inherited only when the effective height is non-null.
    public StrutStyle InheritFromTextStyle(TextStyle? other)
    {
        if (other is null)
        {
            return this;
        }

        double? effectiveHeight = Height ?? other.Height;
        return new StrutStyle(
            FontFamily: FontFamily ?? other.FontFamily,
            FontFamilyFallback: FontFamilyFallback ?? other.FontFamilyFallback,
            FontSize: FontSize ?? other.FontSize,
            Height: effectiveHeight,
            Leading: Leading,
            FontWeight: FontWeight ?? other.FontWeight,
            FontStyle: FontStyle ?? other.FontStyle,
            ForceStrutHeight: ForceStrutHeight,
            DebugLabel: DebugLabel,
            LeadingDistribution: effectiveHeight is not null
                ? LeadingDistribution ?? other.LeadingDistribution
                : null);
    }

    /// Returns a new strut style whose non-null `other` values replace this style's values.
    public StrutStyle Merge(StrutStyle? other)
    {
        if (other is null)
        {
            return this;
        }

        return new StrutStyle(
            FontFamily: other.FontFamily ?? FontFamily,
            FontFamilyFallback: other.FontFamilyFallback ?? FontFamilyFallback,
            FontSize: other.FontSize ?? FontSize,
            Height: other.Height ?? Height,
            LeadingDistribution: other.LeadingDistribution ?? LeadingDistribution,
            Leading: other.Leading ?? Leading,
            FontWeight: other.FontWeight ?? FontWeight,
            FontStyle: other.FontStyle ?? FontStyle,
            ForceStrutHeight: other.ForceStrutHeight ?? ForceStrutHeight,
            DebugLabel: other.DebugLabel ?? DebugLabel,
            Package: other._package ?? _package);
    }

    public bool Equals(StrutStyle? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null
               && other.GetType() == GetType()
               && Equals(other.FontFamily, FontFamily)
               && other.FontSize == FontSize
               && other.FontWeight == FontWeight
               && other.FontStyle == FontStyle
               && other.Height == Height
               && other.Leading == Leading
               && other.ForceStrutHeight == ForceStrutHeight
               && (Height is null || LeadingDistribution == other.LeadingDistribution)
               && ListEquals(other.FontFamilyFallback, FontFamilyFallback);
    }

    public override bool Equals(object? obj) => Equals(obj as StrutStyle);

    public override int GetHashCode()
    {
        return HashCode.Combine(FontFamily, FontSize, FontWeight, FontStyle, Height, Leading, ForceStrutHeight);
    }

    public static bool operator ==(StrutStyle? left, StrutStyle? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(StrutStyle? left, StrutStyle? right) => !(left == right);

    public override string ToStringShort() => Diagnostics.ObjectRuntimeType(this, "StrutStyle");

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        DebugFillProperties(properties, prefix: string.Empty);
    }

    /// Adds every property of this strut style, each name prefixed with `prefix`.
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties, string prefix)
    {
        base.DebugFillProperties(properties);
        if (DebugLabel is not null)
        {
            properties.Add(new MessageProperty($"{prefix}debugLabel", DebugLabel));
        }

        object nullDefault = DiagnosticsDefaults.NullValue;
        string? weightDescription = FontWeight is null ? null : $"w{(int)FontWeight.Value}";
        var styles = new List<DiagnosticsNode>
        {
            new StringProperty($"{prefix}family", FontFamily?.Name, defaultValue: nullDefault, quoted: false),
            new IterableProperty<string>($"{prefix}familyFallback", FontFamilyFallback, defaultValue: nullDefault),
            new DoubleProperty($"{prefix}size", FontSize, defaultValue: nullDefault),
            new DiagnosticsProperty<FontWeight?>(
                $"{prefix}weight",
                FontWeight,
                description: weightDescription,
                defaultValue: nullDefault),
            new EnumProperty<FontStyle>($"{prefix}style", FontStyle, defaultValue: nullDefault),
            new DoubleProperty($"{prefix}height", Height, unit: "x", defaultValue: nullDefault),
            new FlagProperty(
                $"{prefix}forceStrutHeight",
                ForceStrutHeight,
                ifTrue: $"{prefix}<strut height forced>",
                ifFalse: $"{prefix}<strut height normal>"),
        };
        if (Height is not null)
        {
            styles.Add(new EnumProperty<TextLeadingDistribution>(
                $"{prefix}leadingDistribution",
                LeadingDistribution,
                defaultValue: nullDefault));
        }

        bool styleSpecified = styles.Any(node => !node.IsFiltered(DiagnosticLevel.Info));
        foreach (DiagnosticsNode style in styles)
        {
            properties.Add(style);
        }

        if (!styleSpecified)
        {
            properties.Add(new FlagProperty(
                "forceStrutHeight",
                ForceStrutHeight,
                ifTrue: $"{prefix}<strut height forced>",
                ifFalse: $"{prefix}<strut height normal>"));
        }
    }

    private static FontFamily? PrefixFamily(string package, FontFamily? family)
    {
        // Dart interpolates a null family as the literal "null".
        return new FontFamily($"packages/{package}/{family?.Name ?? "null"}");
    }

    private static bool ListEquals(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
    {
        if (a is null)
        {
            return b is null;
        }

        return b is not null && a.SequenceEqual(b, StringComparer.Ordinal);
    }
}
