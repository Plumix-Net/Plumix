using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/icon_data.dart; flutter/packages/flutter/lib/src/widgets/icon.dart (approximate)

public sealed record IconData(
    int CodePoint,
    string? FontFamily = null,
    string? FontPackage = null,
    bool MatchTextDirection = false);

public sealed class Icon : StatelessWidget
{
    private const double DefaultIconSize = 24;

    public Icon(
        IconData? icon,
        double? size = null,
        Color? color = null,
        string? semanticLabel = null,
        TextDirection? textDirection = null,
        bool? applyTextScaling = null,
        FontWeight? fontWeight = null,
        Key? key = null) : base(key)
    {
        IconData = icon;
        Size = size;
        Color = color;
        SemanticLabel = semanticLabel;
        TextDirection = textDirection;
        ApplyTextScaling = applyTextScaling;
        FontWeight = fontWeight;

        if (size.HasValue && (!double.IsFinite(size.Value) || size.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Icon size must be finite and non-negative.");
        }
    }

    public IconData? IconData { get; }

    public double? Size { get; }

    public Color? Color { get; }

    public string? SemanticLabel { get; }

    public TextDirection? TextDirection { get; }

    public bool? ApplyTextScaling { get; }

    public FontWeight? FontWeight { get; }

    public override Widget Build(BuildContext context)
    {
        var iconTheme = IconTheme.Of(context);
        double iconSize = ResolveIconSize(context, iconTheme);

        if (IconData is null)
        {
            return new Semantics(
                label: SemanticLabel,
                child: new SizedBox(width: iconSize, height: iconSize));
        }

        var textDirection = TextDirection ?? Directionality.Of(context);
        var iconColor = Color ?? iconTheme.Color ?? Colors.Black;
        double iconOpacity = Math.Clamp(iconTheme.Opacity ?? 1.0, 0.0, 1.0);
        iconColor = ApplyOpacity(iconColor, iconOpacity);

        Widget iconWidget = new Text(
            char.ConvertFromUtf32(IconData.CodePoint),
            fontSize: iconSize,
            color: iconColor,
            fontFamily: ResolveFontFamily(IconData),
            fontWeight: FontWeight ?? Avalonia.Media.FontWeight.Normal,
            fontStyle: FontStyle.Normal,
            height: 1.0,
            letterSpacing: 0,
            softWrap: false,
            maxLines: 1,
            textDirection: textDirection);

        if (IconData.MatchTextDirection && textDirection == Plumix.UI.TextDirection.Rtl)
        {
            Matrix4 mirror = Matrix4.TranslationValues(iconSize, 0.0, 0.0);
            mirror.ScaleByDouble(-1.0, 1.0, 1.0, 1);
            iconWidget = new Transform(transform: mirror, child: iconWidget);
        }

        return new Semantics(
            label: SemanticLabel,
            child: new ExcludeSemantics(
                child: new SizedBox(
                    width: iconSize,
                    height: iconSize,
                    child: new Center(child: iconWidget))));
    }

    private double ResolveIconSize(BuildContext context, IconThemeData iconTheme)
    {
        double iconSize = Size ?? iconTheme.Size ?? DefaultIconSize;

        if (ApplyTextScaling ?? iconTheme.ApplyTextScaling ?? false)
        {
            iconSize *= MediaQuery.MaybeTextScaleFactorOf(context) ?? 1.0;
        }

        if (!double.IsFinite(iconSize) || iconSize < 0)
        {
            return DefaultIconSize;
        }

        return iconSize;
    }

    private static FontFamily ResolveFontFamily(IconData iconData)
    {
        return string.IsNullOrWhiteSpace(iconData.FontFamily)
            ? FontFamily.Default
            : new FontFamily(iconData.FontFamily);
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Clamp((int)Math.Round(color.A * opacity), 0, 255);
        return Avalonia.Media.Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}
