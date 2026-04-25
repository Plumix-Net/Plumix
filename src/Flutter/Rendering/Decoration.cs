using Avalonia.Media;

// Dart parity source (reference): flutter/packages/flutter/lib/src/painting/box_decoration.dart; flutter/packages/flutter/lib/src/painting/borders.dart (approximate)

namespace Flutter.Rendering;

public readonly record struct BorderRadius
{
    public BorderRadius(double radius)
    {
        Radius = Math.Max(0, radius);
    }

    public double Radius { get; }

    public static BorderRadius Zero => new(0);

    public static BorderRadius Circular(double radius)
    {
        return new(Math.Max(0, radius));
    }
}

public readonly record struct BorderSide
{
    public BorderSide(Color color, double width = 1.0) : this()
    {
        Color = color;
        Width = Math.Max(0, width);
    }

    public Color Color { get; }

    public double Width { get; }
}

public sealed record ShapeBorder(
    BorderRadius BorderRadius,
    BorderSide? Side = null)
{
    public static ShapeBorder RoundedRectangle(double radius, BorderSide? side = null)
    {
        return new ShapeBorder(BorderRadius.Circular(radius), side);
    }
}

public sealed record BoxDecoration(
    Color? Color = null,
    IBrush? Brush = null,
    BorderSide? Border = null,
    BorderRadius? BorderRadius = null,
    BoxShadows? BoxShadows = null)
{
    public BorderRadius EffectiveBorderRadius => BorderRadius ?? Flutter.Rendering.BorderRadius.Zero;

    public BoxShadows EffectiveBoxShadows => BoxShadows ?? default;
}
