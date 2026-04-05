using Avalonia;
using Avalonia.Media;
using Flutter.Foundation;
using Flutter.UI;

namespace Flutter.Widgets;

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
        var iconSize = ResolveIconSize(context, iconTheme);

        if (IconData is null)
        {
            return new SizedBox(width: iconSize, height: iconSize);
        }

        var textDirection = TextDirection ?? Directionality.Of(context);
        var iconColor = Color ?? iconTheme.Color ?? Colors.Black;

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

        if (IconData.MatchTextDirection && textDirection == Flutter.UI.TextDirection.Rtl)
        {
            iconWidget = new Transform(
                transform: Matrix.CreateTranslation(iconSize, 0) * new Matrix(-1, 0, 0, 1, 0, 0),
                child: iconWidget);
        }

        return new SizedBox(
            width: iconSize,
            height: iconSize,
            child: new Center(child: iconWidget));
    }

    private double ResolveIconSize(BuildContext context, IconThemeData iconTheme)
    {
        var iconSize = Size ?? iconTheme.Size ?? DefaultIconSize;

        if (ApplyTextScaling ?? false)
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
}
