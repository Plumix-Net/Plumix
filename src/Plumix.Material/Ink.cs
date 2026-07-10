using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/ink_decoration.dart

/// <summary>
/// Draws a decoration beneath its child so a descendant <see cref="InkWell"/>
/// or <see cref="InkResponse"/> remains visible above an opaque surface.
/// </summary>
public sealed class Ink : StatelessWidget
{
    public Ink(
        Thickness? padding = null,
        Color? color = null,
        BoxDecoration? decoration = null,
        double? width = null,
        double? height = null,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        if (color.HasValue && decoration is not null)
        {
            throw new ArgumentException("Cannot provide both color and decoration.", nameof(decoration));
        }

        ValidateDimension(width, nameof(width));
        ValidateDimension(height, nameof(height));
        ValidatePadding(padding);

        Padding = padding;
        Decoration = decoration ?? (color.HasValue ? new BoxDecoration(Color: color) : null);
        Width = width;
        Height = height;
        Child = child;
    }

    public Thickness? Padding { get; }

    public BoxDecoration? Decoration { get; }

    public double? Width { get; }

    public double? Height { get; }

    public Widget? Child { get; }

    /// <summary>Creates an ink surface backed by a <see cref="DecorationImage"/>.</summary>
    public static Ink Image(
        ImageProvider image,
        Thickness? padding = null,
        ImageErrorListener? onImageError = null,
        ColorFilter? colorFilter = null,
        BoxFit? fit = null,
        Alignment alignment = default,
        Rect? centerSlice = null,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        bool matchTextDirection = false,
        double? width = null,
        double? height = null,
        Widget? child = null,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        return new Ink(
            padding: padding,
            decoration: new BoxDecoration(
                Image: new DecorationImage(
                    image: image,
                    onError: onImageError,
                    colorFilter: colorFilter,
                    fit: fit,
                    alignment: alignment,
                    centerSlice: centerSlice,
                    repeat: repeat,
                    matchTextDirection: matchTextDirection)),
            width: width,
            height: height,
            child: child,
            key: key);
    }

    public override Widget Build(BuildContext context)
    {
        Widget content = Child ?? new ConstrainedBox(BoxConstraints.Expand());
        if (Padding.HasValue)
        {
            content = new Padding(Padding.Value, content);
        }

        if (Decoration is not null)
        {
            content = new DecoratedBox(Decoration, content);
        }

        if (Width.HasValue || Height.HasValue)
        {
            content = new ConstrainedBox(BoxConstraints.TightFor(Width, Height), content);
        }

        return content;
    }

    private static void ValidateDimension(double? value, string parameterName)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Ink dimensions must be finite and non-negative.");
        }
    }

    private static void ValidatePadding(Thickness? padding)
    {
        if (padding.HasValue
            && (padding.Value.Left < 0
                || padding.Value.Top < 0
                || padding.Value.Right < 0
                || padding.Value.Bottom < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(padding), "Ink padding must be non-negative.");
        }
    }
}
