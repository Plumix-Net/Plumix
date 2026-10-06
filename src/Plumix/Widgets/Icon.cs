using System.Collections.Concurrent;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/icon.dart

internal static class IconFontRegistry
{
    private static readonly ConcurrentDictionary<(string Package, string Family), FontFamily> RegisteredFonts =
        new();

    /// <summary>
    /// Maps an icon font to the resource that carries its glyphs. Dart resolves `fontPackage: null`
    /// against the application's own asset bundle (Material's own icons); a null package here means
    /// the same unqualified registration.
    /// </summary>
    public static void Register(string? package, string family, string resourceUri)
    {
        RegisteredFonts[(package ?? string.Empty, family)] = new FontFamily(resourceUri);
    }

    public static FontFamily Resolve(IconData iconData)
    {
        if (string.IsNullOrWhiteSpace(iconData.FontFamily))
        {
            return FontFamily.Default;
        }

        (string Package, string Family) key = (iconData.FontPackage ?? string.Empty, iconData.FontFamily);
        if (RegisteredFonts.TryGetValue(key, out FontFamily? fontFamily))
        {
            return fontFamily;
        }

        return new FontFamily(iconData.FontPackage is null
            ? iconData.FontFamily
            : $"packages/{iconData.FontPackage}/{iconData.FontFamily}");
    }
}

public sealed class Icon : StatelessWidget
{
    public Icon(
        IconData? icon,
        double? size = null,
        Color? color = null,
        string? semanticLabel = null,
        TextDirection? textDirection = null,
        bool? applyTextScaling = null,
        FontWeight? fontWeight = null,
        Key? key = null,
        double? fill = null,
        double? weight = null,
        double? grade = null,
        double? opticalSize = null,
        IReadOnlyList<Shadow>? shadows = null,
        BlendMode? blendMode = null) : base(key)
    {
        IconData = icon;
        Size = size;
        Color = color;
        SemanticLabel = semanticLabel;
        TextDirection = textDirection;
        ApplyTextScaling = applyTextScaling;
        FontWeight = fontWeight;
        Fill = fill;
        Weight = weight;
        Grade = grade;
        OpticalSize = opticalSize;
        Shadows = shadows;
        BlendMode = blendMode;

        DebugAssertions.Assert(fill is null || (0.0 <= fill && fill <= 1.0),
            "fill == null || (0.0 <= fill && fill <= 1.0)");
        DebugAssertions.Assert(weight is null || 0.0 < weight, "weight == null || (0.0 < weight)");
        DebugAssertions.Assert(opticalSize is null || 0.0 < opticalSize,
            "opticalSize == null || (0.0 < opticalSize)");
    }

    public IconData? IconData { get; }

    public double? Size { get; }

    public double? Fill { get; }

    public double? Weight { get; }

    public double? Grade { get; }

    public double? OpticalSize { get; }

    public Color? Color { get; }

    public IReadOnlyList<Shadow>? Shadows { get; }

    public string? SemanticLabel { get; }

    public TextDirection? TextDirection { get; }

    public bool? ApplyTextScaling { get; }

    public BlendMode? BlendMode { get; }

    public FontWeight? FontWeight { get; }

    public override Widget Build(BuildContext context)
    {
        DebugAssertions.Assert(TextDirection is not null || WidgetsDebug.DebugCheckHasDirectionality(context));
        var textDirection = TextDirection ?? Directionality.Of(context);
        var iconTheme = IconTheme.Of(context);
        bool applyTextScaling = ApplyTextScaling ?? iconTheme.ApplyTextScaling ?? false;
        double tentativeIconSize = Size ?? iconTheme.Size ?? TextDefaults.DefaultFontSize;
        double iconSize = applyTextScaling
            ? MediaQuery.TextScalerOf(context).Scale(tentativeIconSize)
            : tentativeIconSize;
        double? iconFill = Fill ?? iconTheme.Fill;
        double? iconWeight = Weight ?? iconTheme.Weight;
        double? iconGrade = Grade ?? iconTheme.Grade;
        double? iconOpticalSize = OpticalSize ?? iconTheme.OpticalSize;
        IReadOnlyList<Shadow>? iconShadows = Shadows ?? iconTheme.Shadows;

        if (IconData is null)
        {
            return new Semantics(
                label: SemanticLabel,
                child: new SizedBox(width: iconSize, height: iconSize));
        }

        Color? iconColor = Color ?? iconTheme.Color!;
        double iconOpacity = iconTheme.Opacity ?? 1.0;
        if (iconOpacity != 1.0)
        {
            iconColor = iconColor!.WithOpacity(iconColor.Opacity * iconOpacity);
        }

        Paint? foreground = null;
        if (BlendMode is { } blendMode)
        {
            foreground = new Paint { BlendMode = blendMode, Color = iconColor! };
            iconColor = null;
        }

        var fontVariations = new List<FontVariation>();
        if (iconFill is { } fill)
        {
            fontVariations.Add(new FontVariation("FILL", fill));
        }
        if (iconWeight is { } weight)
        {
            fontVariations.Add(new FontVariation("wght", weight));
        }
        if (iconGrade is { } grade)
        {
            fontVariations.Add(new FontVariation("GRAD", grade));
        }
        if (iconOpticalSize is { } opticalSize)
        {
            fontVariations.Add(new FontVariation("opsz", opticalSize));
        }

        // The registry resolves Dart asset families to Avalonia resource URIs before shaping.
        var fontStyle = new TextStyle(
            FontVariations: fontVariations,
            Inherit: false,
            Color: iconColor,
            FontSize: iconSize,
            FontFamily: IconData.FontFamily is null ? null : ResolveFontFamily(IconData),
            FontWeight: FontWeight,
            FontFamilyFallback: IconData.FontPackage is null
                ? IconData.FontFamilyFallback
                : IconData.FontFamilyFallback?.Select(family => $"packages/{IconData.FontPackage}/{family}").ToList(),
            Shadows: iconShadows,
            Height: 1.0,
            LeadingDistribution: TextLeadingDistribution.Even,
            Foreground: foreground);

        Widget iconWidget = new RichText(
            overflow: TextOverflow.Visible,
            textDirection: textDirection,
            text: new TextSpan(text: char.ConvertFromUtf32(IconData.CodePoint), style: fontStyle));

        if (IconData.MatchTextDirection && textDirection == Plumix.UI.TextDirection.Rtl)
        {
            var mirror = Matrix4.Identity();
            mirror.ScaleByDouble(-1.0, 1.0, 1.0, 1);
            iconWidget = new Transform(
                transform: mirror,
                alignment: Alignment.Center,
                transformHitTests: false,
                child: iconWidget);
        }

        return new Semantics(
            label: SemanticLabel,
            child: new ExcludeSemantics(
                child: new SizedBox(
                    width: iconSize,
                    height: iconSize,
                    child: new Center(child: iconWidget))));
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        object nullDefault = DiagnosticsDefaults.NullValue;
        properties.Add(new IconDataProperty("icon", IconData, ifNull: "<empty>", showName: false));
        properties.Add(new DoubleProperty("size", Size, defaultValue: nullDefault));
        properties.Add(new DoubleProperty("fill", Fill, defaultValue: nullDefault));
        properties.Add(new DoubleProperty("weight", Weight, defaultValue: nullDefault));
        properties.Add(new DoubleProperty("grade", Grade, defaultValue: nullDefault));
        properties.Add(new DoubleProperty("opticalSize", OpticalSize, defaultValue: nullDefault));
        properties.Add(new ColorProperty("color", Color, defaultValue: nullDefault));
        properties.Add(new IterableProperty<Shadow>("shadows", Shadows, defaultValue: nullDefault));
        properties.Add(new StringProperty("semanticLabel", SemanticLabel, defaultValue: nullDefault));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<bool?>("applyTextScaling", ApplyTextScaling, defaultValue: nullDefault));
    }

    public static FontFamily ResolveFontFamily(IconData iconData)
    {
        return IconFontRegistry.Resolve(iconData);
    }

}
