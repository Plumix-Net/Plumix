using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source:
// flutter/packages/flutter/lib/src/widgets/image_icon.dart

public sealed class ImageIcon : StatelessWidget
{
    private const double DefaultIconSize = 24.0;

    public ImageIcon(
        ImageProvider? image,
        double? size = null,
        Color? color = null,
        string? semanticLabel = null,
        Key? key = null) : base(key)
    {
        if (size.HasValue && (!double.IsFinite(size.Value) || size.Value < 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Image icon size must be finite and non-negative.");
        }

        Image = image;
        Size = size;
        Color = color;
        SemanticLabel = semanticLabel;
    }

    public ImageProvider? Image { get; }

    public double? Size { get; }

    public Color? Color { get; }

    public string? SemanticLabel { get; }

    public override Widget Build(BuildContext context)
    {
        IconThemeData iconTheme = IconTheme.Of(context);
        double iconSize = Size ?? iconTheme.Size ?? DefaultIconSize;

        if (Image is null)
        {
            return new Semantics(
                label: SemanticLabel,
                child: new SizedBox(width: iconSize, height: iconSize));
        }

        double iconOpacity = iconTheme.Opacity ?? 1.0;
        Color iconColor = Color ?? iconTheme.Color ?? Colors.Black;
        if (iconOpacity != 1.0)
        {
            byte alpha = (byte)Math.Clamp((int)Math.Round(iconColor.A * iconOpacity), 0, 255);
            iconColor = Avalonia.Media.Color.FromArgb(alpha, iconColor.R, iconColor.G, iconColor.B);
        }

        return new Semantics(
            label: SemanticLabel,
            child: new Plumix.Widgets.Image(
                image: Image,
                width: iconSize,
                height: iconSize,
                color: iconColor,
                fit: BoxFit.ScaleDown,
                excludeFromSemantics: true));
    }
}
