using Avalonia;
using Avalonia.Media;
using Plumix.UI;

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

public enum BorderStyle
{
    None,
    Solid,
}

// Dart parity source: flutter/packages/flutter/lib/src/painting/decoration.dart
public abstract record Decoration
{
    public abstract BoxPainter CreateBoxPainter(Action? onChanged = null);

    public virtual Decoration? LerpFrom(Decoration? a, double t)
    {
        return null;
    }

    public virtual Decoration? LerpTo(Decoration? b, double t)
    {
        return null;
    }

    public static Decoration? Lerp(Decoration? a, Decoration? b, double t)
    {
        if (ReferenceEquals(a, b) || Equals(a, b))
        {
            return a;
        }

        if (a is null)
        {
            return b!.LerpFrom(null, t) ?? b;
        }

        if (b is null)
        {
            return a.LerpTo(null, t) ?? a;
        }

        if (t == 0.0)
        {
            return a;
        }

        if (t == 1.0)
        {
            return b;
        }

        return b.LerpFrom(a, t)
               ?? a.LerpTo(b, t)
               ?? (t < 0.5
                   ? a.LerpTo(null, t * 2.0) ?? a
                   : b.LerpFrom(null, (t - 0.5) * 2.0) ?? b);
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/painting/decoration.dart
public abstract class BoxPainter : IDisposable
{
    protected BoxPainter(Action? onChanged = null)
    {
        OnChanged = onChanged;
    }

    protected Action? OnChanged { get; }

    public abstract void Paint(
        PaintingContext context,
        Point offset,
        ImageConfiguration configuration);

    public virtual void Dispose()
    {
    }
}

public readonly record struct BorderRadius
{
    public BorderRadius(double radius)
        : this(radius, radius, radius, radius)
    {
    }

    public BorderRadius(
        double topLeft,
        double topRight,
        double bottomRight,
        double bottomLeft)
    {
        TopLeft = Math.Max(0.0, topLeft);
        TopRight = Math.Max(0.0, topRight);
        BottomRight = Math.Max(0.0, bottomRight);
        BottomLeft = Math.Max(0.0, bottomLeft);
    }

    public double TopLeft { get; }

    public double TopRight { get; }

    public double BottomRight { get; }

    public double BottomLeft { get; }

    public double Radius => TopLeft;

    public bool IsUniform => TopLeft == TopRight
                             && TopLeft == BottomRight
                             && TopLeft == BottomLeft;

    public static BorderRadius Zero => new(0);

    public static BorderRadius Circular(double radius)
    {
        return new(Math.Max(0, radius));
    }

    public static BorderRadius Only(
        double topLeft = 0.0,
        double topRight = 0.0,
        double bottomRight = 0.0,
        double bottomLeft = 0.0)
    {
        return new BorderRadius(topLeft, topRight, bottomRight, bottomLeft);
    }

    public static BorderRadius? Lerp(BorderRadius? a, BorderRadius? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        BorderRadius from = a ?? Zero;
        BorderRadius to = b ?? Zero;
        return new BorderRadius(
            LerpDouble(from.TopLeft, to.TopLeft, t),
            LerpDouble(from.TopRight, to.TopRight, t),
            LerpDouble(from.BottomRight, to.BottomRight, t),
            LerpDouble(from.BottomLeft, to.BottomLeft, t));
    }

    private static double LerpDouble(double a, double b, double t)
    {
        return a + ((b - a) * t);
    }
}

public readonly record struct BorderRadiusDirectional
{
    public BorderRadiusDirectional(
        double topStart,
        double topEnd,
        double bottomEnd,
        double bottomStart)
    {
        TopStart = Math.Max(0.0, topStart);
        TopEnd = Math.Max(0.0, topEnd);
        BottomEnd = Math.Max(0.0, bottomEnd);
        BottomStart = Math.Max(0.0, bottomStart);
    }

    public double TopStart { get; }

    public double TopEnd { get; }

    public double BottomEnd { get; }

    public double BottomStart { get; }

    public static BorderRadiusDirectional Circular(double radius)
    {
        double effectiveRadius = Math.Max(0.0, radius);
        return new BorderRadiusDirectional(
            effectiveRadius,
            effectiveRadius,
            effectiveRadius,
            effectiveRadius);
    }

    public static BorderRadiusDirectional Only(
        double topStart = 0.0,
        double topEnd = 0.0,
        double bottomEnd = 0.0,
        double bottomStart = 0.0)
    {
        return new BorderRadiusDirectional(
            topStart,
            topEnd,
            bottomEnd,
            bottomStart);
    }
}

public readonly record struct BorderRadiusGeometry
{
    private BorderRadiusGeometry(
        BorderRadius physical,
        BorderRadiusDirectional directional)
    {
        Physical = physical;
        Directional = directional;
    }

    public BorderRadius Physical { get; }

    public BorderRadiusDirectional Directional { get; }

    public BorderRadius Resolve(TextDirection direction)
    {
        return direction == TextDirection.Ltr
            ? new BorderRadius(
                Physical.TopLeft + Directional.TopStart,
                Physical.TopRight + Directional.TopEnd,
                Physical.BottomRight + Directional.BottomEnd,
                Physical.BottomLeft + Directional.BottomStart)
            : new BorderRadius(
                Physical.TopLeft + Directional.TopEnd,
                Physical.TopRight + Directional.TopStart,
                Physical.BottomRight + Directional.BottomStart,
                Physical.BottomLeft + Directional.BottomEnd);
    }

    public static BorderRadiusGeometry? Lerp(
        BorderRadiusGeometry? a,
        BorderRadiusGeometry? b,
        double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        BorderRadiusGeometry from = a ?? default;
        BorderRadiusGeometry to = b ?? default;
        BorderRadius physical = BorderRadius.Lerp(from.Physical, to.Physical, t)!.Value;
        var directional = new BorderRadiusDirectional(
            LerpDouble(from.Directional.TopStart, to.Directional.TopStart, t),
            LerpDouble(from.Directional.TopEnd, to.Directional.TopEnd, t),
            LerpDouble(from.Directional.BottomEnd, to.Directional.BottomEnd, t),
            LerpDouble(from.Directional.BottomStart, to.Directional.BottomStart, t));
        return new BorderRadiusGeometry(physical, directional);
    }

    public static implicit operator BorderRadiusGeometry(BorderRadius radius)
    {
        return new BorderRadiusGeometry(radius, default);
    }

    public static implicit operator BorderRadiusGeometry(BorderRadiusDirectional radius)
    {
        return new BorderRadiusGeometry(default, radius);
    }

    private static double LerpDouble(double a, double b, double t)
    {
        return a + ((b - a) * t);
    }
}

public readonly record struct BorderSide
{
    public BorderSide(
        Color color,
        double width = 1.0,
        BorderStyle style = BorderStyle.Solid) : this()
    {
        Color = color;
        Width = Math.Max(0, width);
        Style = style;
    }

    public Color Color { get; }

    public double Width { get; }

    public BorderStyle Style { get; }
}

public sealed record BoxBorder(
    BorderSide? Left = null,
    BorderSide? Top = null,
    BorderSide? Right = null,
    BorderSide? Bottom = null)
{
    public static BoxBorder All(BorderSide side)
    {
        return new BoxBorder(side, side, side, side);
    }

    public static BoxBorder? Lerp(BoxBorder? a, BoxBorder? b, double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return new BoxBorder(
            Left: LerpSide(a?.Left, b?.Left, t),
            Top: LerpSide(a?.Top, b?.Top, t),
            Right: LerpSide(a?.Right, b?.Right, t),
            Bottom: LerpSide(a?.Bottom, b?.Bottom, t));
    }

    private static BorderSide? LerpSide(BorderSide? a, BorderSide? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        BorderSide from = a ?? TransparentSide(b!.Value);
        BorderSide to = b ?? TransparentSide(a!.Value);
        return new BorderSide(
            BoxDecoration.LerpColor(from.Color, to.Color, t)!.Value,
            from.Width + ((to.Width - from.Width) * t),
            t < 0.5 ? from.Style : to.Style);
    }

    private static BorderSide TransparentSide(BorderSide side)
    {
        return new BorderSide(Color.FromArgb(0, side.Color.R, side.Color.G, side.Color.B), 0.0, side.Style);
    }
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

    public static implicit operator ShapeBorder(BorderRadius borderRadius) =>
        new(borderRadius);
}

public sealed record ShapeDecoration(
    ShapeBorder Shape,
    Color? Color = null) : Decoration
{
    public override BoxPainter CreateBoxPainter(Action? onChanged = null)
    {
        var border = Shape.Side;
        return new BoxDecoration(
            Color: Color,
            BorderRadius: Shape.BorderRadius,
            Shape: Shape.Shape,
            BorderSides: border.HasValue ? BoxBorder.All(border.Value) : null)
            .CreateBoxPainter(onChanged);
    }

    public override Decoration? LerpFrom(Decoration? a, double t)
    {
        if (a is not ShapeDecoration from)
        {
            return base.LerpFrom(a, t);
        }

        return new ShapeDecoration(
            Shape: new ShapeBorder(
                BorderRadius.Lerp(from.Shape.BorderRadius, Shape.BorderRadius, t)!.Value,
                t < 0.5 ? from.Shape.Side : Shape.Side)
            {
                Shape = t < 0.5 ? from.Shape.Shape : Shape.Shape
            },
            Color: BoxDecoration.LerpColor(from.Color, Color, t));
    }
}

public sealed record BoxDecoration(
    Color? Color = null,
    IBrush? Brush = null,
    BorderSide? Border = null,
    BorderRadius? BorderRadius = null,
    BoxShadows? BoxShadows = null,
    DecorationImage? Image = null,
    BoxShape Shape = BoxShape.Rectangle,
    BoxBorder? BorderSides = null) : Decoration
{
    public BorderRadius EffectiveBorderRadius => BorderRadius ?? Plumix.Rendering.BorderRadius.Zero;

    public BoxShadows EffectiveBoxShadows => BoxShadows ?? default;

    public override BoxPainter CreateBoxPainter(Action? onChanged = null)
    {
        return new BoxDecorationPainter(this, onChanged);
    }

    public override Decoration? LerpFrom(Decoration? a, double t)
    {
        return a is null or BoxDecoration
            ? Lerp(a as BoxDecoration, this, t)
            : base.LerpFrom(a, t);
    }

    public override Decoration? LerpTo(Decoration? b, double t)
    {
        return b is null or BoxDecoration
            ? Lerp(this, b as BoxDecoration, t)
            : base.LerpTo(b, t);
    }

    public static BoxDecoration? Lerp(BoxDecoration? a, BoxDecoration? b, double t)
    {
        if (ReferenceEquals(a, b) || Equals(a, b)) return a;
        if (a is null) return b?.Scale(t);
        if (b is null) return a.Scale(1 - t);
        if (t == 0) return a;
        if (t == 1) return b;

        return new BoxDecoration(
            Color: LerpColor(a.Color, b.Color, t),
            Brush: t < 0.5 ? a.Brush : b.Brush,
            Border: LerpBorder(a.Border, b.Border, t),
            BorderRadius: LerpBorderRadius(a.BorderRadius, b.BorderRadius, t),
            BoxShadows: t < 0.5 ? a.BoxShadows : b.BoxShadows,
            Image: DecorationImage.Lerp(a.Image, b.Image, t),
            Shape: t < 0.5 ? a.Shape : b.Shape,
            BorderSides: BoxBorder.Lerp(a.BorderSides, b.BorderSides, t));
    }

    private BoxDecoration Scale(double factor)
    {
        return this with
        {
            Color = LerpColor(null, Color, factor),
            Border = LerpBorder(null, Border, factor),
            Image = DecorationImage.Lerp(null, Image, factor),
            BorderSides = BoxBorder.Lerp(null, BorderSides, factor),
        };
    }

    internal static Color? LerpColor(Color? a, Color? b, double t)
    {
        if (!a.HasValue && !b.HasValue) return null;
        var from = a ?? Avalonia.Media.Color.FromArgb(0, b!.Value.R, b.Value.G, b.Value.B);
        var to = b ?? Avalonia.Media.Color.FromArgb(0, a!.Value.R, a.Value.G, a.Value.B);
        return Avalonia.Media.Color.FromArgb(
            LerpChannel(from.A, to.A, t),
            LerpChannel(from.R, to.R, t),
            LerpChannel(from.G, to.G, t),
            LerpChannel(from.B, to.B, t));
    }

    private static byte LerpChannel(byte a, byte b, double t)
    {
        return (byte)Math.Clamp((int)(a + ((b - a) * t)), byte.MinValue, byte.MaxValue);
    }

    private static BorderSide? LerpBorder(BorderSide? a, BorderSide? b, double t)
    {
        if (!a.HasValue && !b.HasValue) return null;
        var from = a ?? new BorderSide(
            Avalonia.Media.Color.FromArgb(0, b!.Value.Color.R, b.Value.Color.G, b.Value.Color.B),
            0,
            b.Value.Style);
        var to = b ?? new BorderSide(
            Avalonia.Media.Color.FromArgb(0, a!.Value.Color.R, a.Value.Color.G, a.Value.Color.B),
            0,
            a.Value.Style);
        return new BorderSide(
            LerpColor(from.Color, to.Color, t)!.Value,
            from.Width + ((to.Width - from.Width) * t),
            t < 0.5 ? from.Style : to.Style);
    }

    private static BorderRadius? LerpBorderRadius(BorderRadius? a, BorderRadius? b, double t)
    {
        return Plumix.Rendering.BorderRadius.Lerp(a, b, t);
    }
}

internal sealed class BoxDecorationPainter : BoxPainter
{
    private readonly BoxDecoration _decoration;
    private DecorationImagePainter? _imagePainter;

    public BoxDecorationPainter(BoxDecoration decoration, Action? onChanged = null) : base(onChanged)
    {
        _decoration = decoration;
    }

    public override void Paint(
        PaintingContext context,
        Point offset,
        ImageConfiguration configuration)
    {
        Size size = configuration.Size ?? default;
        var rect = new Rect(offset, size);
        BorderRadius borderRadius = _decoration.EffectiveBorderRadius;
        BoxShadows boxShadows = _decoration.EffectiveBoxShadows;
        IBrush? fill = _decoration.Brush;
        if (fill is null && _decoration.Color.HasValue)
        {
            fill = new SolidColorBrush(_decoration.Color.Value);
        }

        IPen? borderPen = null;
        if (_decoration.Border.HasValue && _decoration.BorderSides is null)
        {
            BorderSide border = _decoration.Border.Value;
            if (border.Style == BorderStyle.Solid && border.Width > 0)
            {
                borderPen = new Pen(new SolidColorBrush(border.Color), border.Width);
            }
        }

        if (_decoration.Shape == BoxShape.Circle)
        {
            if (fill != null || boxShadows.Count > 0)
            {
                double side = Math.Min(rect.Width, rect.Height);
                var circleRect = new Rect(
                    rect.Center.X - (side / 2.0),
                    rect.Center.Y - (side / 2.0),
                    side,
                    side);
                context.DrawRectangle(
                    fill ?? Brushes.Transparent,
                    null,
                    circleRect,
                    side / 2.0,
                    side / 2.0,
                    boxShadows);
            }
        }
        else if (fill != null || boxShadows.Count > 0)
        {
            context.DrawRectangle(
                fill ?? Brushes.Transparent,
                null,
                rect,
                borderRadius,
                boxShadows);
        }

        if (_decoration.Image is not null)
        {
            _imagePainter ??= _decoration.Image.CreatePainter(HandleImageChanged);
            _imagePainter.Paint(
                context,
                rect,
                configuration,
                clipRadius: _decoration.BorderRadius,
                shape: _decoration.Shape);
        }

        if (_decoration.BorderSides is { } borderSides)
        {
            PaintBorderSides(context, rect, borderRadius, borderSides);
        }

        if (borderPen is null)
        {
            return;
        }

        if (_decoration.Shape == BoxShape.Circle)
        {
            double side = Math.Min(rect.Width, rect.Height);
            var circleRect = new Rect(
                rect.Center.X - (side / 2.0),
                rect.Center.Y - (side / 2.0),
                side,
                side);
            context.DrawRectangle(
                Brushes.Transparent,
                borderPen,
                circleRect,
                side / 2.0,
                side / 2.0);
        }
        else
        {
            context.DrawRectangle(Brushes.Transparent, borderPen, rect, borderRadius);
        }
    }

    private static void PaintBorderSides(
        PaintingContext context,
        Rect rect,
        BorderRadius borderRadius,
        BoxBorder border)
    {
        int visibleSideCount = CountVisibleSides(border);
        if (visibleSideCount == 1)
        {
            if (border.Bottom is { } bottom)
            {
                PaintHorizontalSide(context, rect, borderRadius, bottom, atTop: false);
                return;
            }

            if (border.Top is { } top)
            {
                PaintHorizontalSide(context, rect, borderRadius, top, atTop: true);
                return;
            }

            if (border.Left is { } left)
            {
                PaintVerticalSide(context, rect, borderRadius, left, atLeft: true);
                return;
            }

            if (border.Right is { } right)
            {
                PaintVerticalSide(context, rect, borderRadius, right, atLeft: false);
                return;
            }
        }

        PaintHorizontalSide(context, rect, BorderRadius.Zero, border.Top, atTop: true);
        PaintHorizontalSide(context, rect, BorderRadius.Zero, border.Bottom, atTop: false);
        PaintVerticalSide(context, rect, BorderRadius.Zero, border.Left, atLeft: true);
        PaintVerticalSide(context, rect, BorderRadius.Zero, border.Right, atLeft: false);
    }

    private static int CountVisibleSides(BoxBorder border)
    {
        int count = 0;
        count += IsVisible(border.Left) ? 1 : 0;
        count += IsVisible(border.Top) ? 1 : 0;
        count += IsVisible(border.Right) ? 1 : 0;
        count += IsVisible(border.Bottom) ? 1 : 0;
        return count;
    }

    private static bool IsVisible(BorderSide? side)
    {
        return side is { Style: BorderStyle.Solid };
    }

    private static void PaintHorizontalSide(
        PaintingContext context,
        Rect rect,
        BorderRadius borderRadius,
        BorderSide? side,
        bool atTop)
    {
        if (!IsVisible(side))
        {
            return;
        }

        BorderSide resolvedSide = side!.Value;
        double paintWidth = resolvedSide.Width == 0.0 ? 1.0 : resolvedSide.Width;
        double y = atTop
            ? resolvedSide.Width == 0.0 ? rect.Top - 0.5 : rect.Top
            : resolvedSide.Width == 0.0 ? rect.Bottom - 0.5 : rect.Bottom - paintWidth;
        var sideRect = new Rect(rect.Left, y, rect.Width, paintWidth);
        context.DrawRectangle(
            new SolidColorBrush(resolvedSide.Color),
            null,
            sideRect,
            borderRadius);
    }

    private static void PaintVerticalSide(
        PaintingContext context,
        Rect rect,
        BorderRadius borderRadius,
        BorderSide? side,
        bool atLeft)
    {
        if (!IsVisible(side))
        {
            return;
        }

        BorderSide resolvedSide = side!.Value;
        double paintWidth = resolvedSide.Width == 0.0 ? 1.0 : resolvedSide.Width;
        double x = atLeft
            ? resolvedSide.Width == 0.0 ? rect.Left - 0.5 : rect.Left
            : resolvedSide.Width == 0.0 ? rect.Right - 0.5 : rect.Right - paintWidth;
        var sideRect = new Rect(x, rect.Top, paintWidth, rect.Height);
        context.DrawRectangle(
            new SolidColorBrush(resolvedSide.Color),
            null,
            sideRect,
            borderRadius);
    }

    public override void Dispose()
    {
        _imagePainter?.Dispose();
        _imagePainter = null;
    }

    private void HandleImageChanged()
    {
        OnChanged?.Invoke();
    }
}
