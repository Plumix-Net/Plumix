using Avalonia.Media;

// Dart parity source (reference): flutter/packages/flutter/lib/src/painting/box_decoration.dart; flutter/packages/flutter/lib/src/painting/borders.dart (approximate)

namespace Plumix.Rendering;

public enum BoxShape
{
    Rectangle,
    Circle,
}

public enum DecorationPosition
{
    Background,
    Foreground,
}

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
    public BoxShape Shape { get; init; } = BoxShape.Rectangle;

    public static ShapeBorder RoundedRectangle(double radius, BorderSide? side = null)
    {
        return new ShapeBorder(BorderRadius.Circular(radius), side);
    }

    public static ShapeBorder Circle(BorderSide? side = null) =>
        new(BorderRadius.Circular(9999), side) { Shape = BoxShape.Circle };

    public static ShapeBorder Stadium(BorderSide? side = null) =>
        new(BorderRadius.Circular(9999), side);
}

public sealed record BoxDecoration(
    Color? Color = null,
    IBrush? Brush = null,
    BorderSide? Border = null,
    BorderRadius? BorderRadius = null,
    BoxShadows? BoxShadows = null,
    DecorationImage? Image = null,
    BoxShape Shape = BoxShape.Rectangle)
{
    public BorderRadius EffectiveBorderRadius => BorderRadius ?? Plumix.Rendering.BorderRadius.Zero;

    public BoxShadows EffectiveBoxShadows => BoxShadows ?? default;

    public static BoxDecoration? Lerp(BoxDecoration? a, BoxDecoration? b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        if (ReferenceEquals(a, b) || Equals(a, b)) return a;
        if (a is null) return b?.Scale(t);
        if (b is null) return a.Scale(1 - t);
        if (t <= 0) return a;
        if (t >= 1) return b;

        return new BoxDecoration(
            Color: LerpColor(a.Color, b.Color, t),
            Brush: t < 0.5 ? a.Brush : b.Brush,
            Border: LerpBorder(a.Border, b.Border, t),
            BorderRadius: LerpBorderRadius(a.BorderRadius, b.BorderRadius, t),
            BoxShadows: t < 0.5 ? a.BoxShadows : b.BoxShadows,
            Image: DecorationImage.Lerp(a.Image, b.Image, t),
            Shape: t < 0.5 ? a.Shape : b.Shape);
    }

    private BoxDecoration Scale(double factor)
    {
        return this with
        {
            Color = LerpColor(null, Color, factor),
            Border = LerpBorder(null, Border, factor),
            Image = DecorationImage.Lerp(null, Image, factor),
        };
    }

    private static Color? LerpColor(Color? a, Color? b, double t)
    {
        if (!a.HasValue && !b.HasValue) return null;
        var from = a ?? Avalonia.Media.Color.FromArgb(0, b!.Value.R, b.Value.G, b.Value.B);
        var to = b ?? Avalonia.Media.Color.FromArgb(0, a!.Value.R, a.Value.G, a.Value.B);
        return new ColorTween().Evaluate(t, from, to);
    }

    private static BorderSide? LerpBorder(BorderSide? a, BorderSide? b, double t)
    {
        if (!a.HasValue && !b.HasValue) return null;
        var from = a ?? new BorderSide(Avalonia.Media.Color.FromArgb(0, b!.Value.Color.R, b.Value.Color.G, b.Value.Color.B), 0);
        var to = b ?? new BorderSide(Avalonia.Media.Color.FromArgb(0, a!.Value.Color.R, a.Value.Color.G, a.Value.Color.B), 0);
        return new BorderSide(
            LerpColor(from.Color, to.Color, t)!.Value,
            from.Width + ((to.Width - from.Width) * t));
    }

    private static BorderRadius? LerpBorderRadius(BorderRadius? a, BorderRadius? b, double t)
    {
        if (!a.HasValue && !b.HasValue) return null;
        var from = a?.Radius ?? 0;
        var to = b?.Radius ?? 0;
        return new BorderRadius(from + ((to - from) * t));
    }
}
