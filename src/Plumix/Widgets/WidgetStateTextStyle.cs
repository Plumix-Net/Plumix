using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Painting;
using Plumix.UI;
using FontFeature = Plumix.UI.FontFeature;
using TextDecoration = Plumix.UI.TextDecoration;
using TextDecorationStyle = Plumix.UI.TextDecorationStyle;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/widget_state.dart

/// A text style that resolves according to a widget's interaction states.
public abstract class WidgetStateTextStyle : TextStyle, IWidgetStateProperty<TextStyle>
{
    protected WidgetStateTextStyle()
    {
    }

    /// Creates a resolver whose ordinary text-style fields remain empty.
    public static WidgetStateTextStyle ResolveWith(Func<IReadOnlySet<WidgetState>, TextStyle> callback) =>
        new ResolvingWidgetStateTextStyle(callback);

    /// Creates a style from an ordered map of state constraints.
    /// Use this only in slots that document support for state properties.
    public static WidgetStateTextStyle FromMap(
        IReadOnlyList<KeyValuePair<WidgetStatesConstraint, TextStyle>> map) => new WidgetTextStyleMapper(map);

    public abstract TextStyle Resolve(IReadOnlySet<WidgetState> states);
}

internal sealed class ResolvingWidgetStateTextStyle : WidgetStateTextStyle
{
    private readonly Func<IReadOnlySet<WidgetState>, TextStyle> _resolve;

    internal ResolvingWidgetStateTextStyle(Func<IReadOnlySet<WidgetState>, TextStyle> callback)
    {
        _resolve = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    public override TextStyle Resolve(IReadOnlySet<WidgetState> states) => _resolve(states);
}

// C# cannot inherit both TextStyle and WidgetStateMapper, so the mapper contract is delegated.
internal sealed class WidgetTextStyleMapper : WidgetStateTextStyle, IWidgetStateMapper<TextStyle>
{
    private readonly IReadOnlyList<KeyValuePair<WidgetStatesConstraint, TextStyle>> _map;
    private readonly WidgetStateMapper<TextStyle> _mapper;

    internal WidgetTextStyleMapper(IReadOnlyList<KeyValuePair<WidgetStatesConstraint, TextStyle>> map)
    {
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _mapper = new WidgetStateMapper<TextStyle>(map);
    }

    public WidgetStateMapper<TextStyle> StateMapper => _mapper;

    public override TextStyle Resolve(IReadOnlySet<WidgetState> states) => _mapper.Resolve(states)
        ?? throw new ArgumentException(
            $"The current set of widget states is {{{string.Join(", ", states)}}}.\n"
            + "None of the provided map keys matched this set, and the type \"TextStyle\" is non-nullable.\n"
            + "Consider using \"WidgetStateMapper<TextStyle?>()\", or adding the \"WidgetState.any\" key to this map.");

    public override bool Inherit => throw NoSuchMember(nameof(Inherit));

    public override Color? Color => throw NoSuchMember(nameof(Color));

    public override Color? BackgroundColor => throw NoSuchMember(nameof(BackgroundColor));

    public override FontFamily? FontFamily => throw NoSuchMember(nameof(FontFamily));

    public override double? FontSize => throw NoSuchMember(nameof(FontSize));

    public override FontWeight? FontWeight => throw NoSuchMember(nameof(FontWeight));

    public override FontStyle? FontStyle => throw NoSuchMember(nameof(FontStyle));

    public override double? LetterSpacing => throw NoSuchMember(nameof(LetterSpacing));

    public override double? WordSpacing => throw NoSuchMember(nameof(WordSpacing));

    public override TextBaseline? TextBaseline => throw NoSuchMember(nameof(TextBaseline));

    public override double? Height => throw NoSuchMember(nameof(Height));

    public override TextLeadingDistribution? LeadingDistribution => throw NoSuchMember(nameof(LeadingDistribution));

    public override Locale? Locale => throw NoSuchMember(nameof(Locale));

    public override Paint? Foreground => throw NoSuchMember(nameof(Foreground));

    public override Paint? Background => throw NoSuchMember(nameof(Background));

    public override TextDecoration? Decoration => throw NoSuchMember(nameof(Decoration));

    public override Color? DecorationColor => throw NoSuchMember(nameof(DecorationColor));

    public override TextDecorationStyle? DecorationStyle => throw NoSuchMember(nameof(DecorationStyle));

    public override double? DecorationThickness => throw NoSuchMember(nameof(DecorationThickness));

    public override string? DebugLabel => throw NoSuchMember(nameof(DebugLabel));

    public override IReadOnlyList<Shadow>? Shadows => throw NoSuchMember(nameof(Shadows));

    public override IReadOnlyList<FontFeature>? FontFeatures => throw NoSuchMember(nameof(FontFeatures));

    public override IReadOnlyList<FontVariation>? FontVariations => throw NoSuchMember(nameof(FontVariations));

    public override TextOverflow? Overflow => throw NoSuchMember(nameof(Overflow));

    public override IReadOnlyList<string>? FontFamilyFallback => throw NoSuchMember(nameof(FontFamilyFallback));

    public override TextStyle CopyWith(
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
        TextOverflow? overflow = null) => throw NoSuchMember(nameof(CopyWith));

    public override TextStyle Apply(
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
        TextOverflow? overflow = null) => throw NoSuchMember(nameof(Apply));

    public override TextStyle Merge(TextStyle? other) => throw NoSuchMember(nameof(Merge));

    public override ParagraphTextStyle GetTextStyle(
        double textScaleFactor = 1.0, TextScaler? textScaler = null) => throw NoSuchMember(nameof(GetTextStyle));

    public override ParagraphStyle GetParagraphStyle(
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
        StrutStyle? strutStyle = null) => throw NoSuchMember(nameof(GetParagraphStyle));

    public override RenderComparison CompareTo(TextStyle other) => throw NoSuchMember(nameof(CompareTo));

    public override bool Equals(TextStyle? other) => _mapper.Equals(other);

    public override bool Equals(object? obj) => _mapper.Equals(obj);

    public override int GetHashCode() => _mapper.GetHashCode();

    public override string ToString() => "WidgetStateMapper<TextStyle>({"
        + string.Join(", ", _map.Select(entry => $"{entry.Key}: {entry.Value}")) + "})";

    public override string ToString(DiagnosticLevel minLevel) => ToString();

    public override string ToStringShort() => Diagnostics.DescribeIdentity(this);

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties) =>
        DebugFillProperties(properties, "");

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties, string prefix)
    {
        properties.Add(new DiagnosticsProperty<IReadOnlyList<KeyValuePair<WidgetStatesConstraint, TextStyle>>>(
            "map", _map));
    }

    private FlutterError NoSuchMember(string memberName) => new(
    [
        new ErrorSummary(
            $"There was an attempt to access the \"{memberName}\" field of a WidgetStateMapper<TextStyle> object."),
        new ErrorDescription(ToString()),
        new ErrorDescription(
            "WidgetStateProperty objects should only be used in places that document their support."),
        new ErrorHint(
            "Double-check whether the map was used in a place that documents support for "
            + "WidgetStateProperty objects. If so, please file a bug report. (The https://pub.dev/ page "
            + "for a package contains a link to \"View/report issues\".)"),
    ]);
}
