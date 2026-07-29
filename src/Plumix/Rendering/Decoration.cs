using Avalonia;
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
    BoxShape Shape = BoxShape.Rectangle) : Decoration
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
        if (!a.HasValue && !b.HasValue) return null;
        double from = a?.Radius ?? 0;
        double to = b?.Radius ?? 0;
        return new BorderRadius(from + ((to - from) * t));
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
        double radius = _decoration.EffectiveBorderRadius.Radius;
        BoxShadows boxShadows = _decoration.EffectiveBoxShadows;
        IBrush? fill = _decoration.Brush;
        if (fill is null && _decoration.Color.HasValue)
        {
            fill = new SolidColorBrush(_decoration.Color.Value);
        }

        IPen? borderPen = null;
        if (_decoration.Border.HasValue)
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
            context.DrawRectangle(fill ?? Brushes.Transparent, null, rect, radius, radius, boxShadows);
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
            context.DrawRectangle(Brushes.Transparent, borderPen, rect, radius, radius);
        }
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
