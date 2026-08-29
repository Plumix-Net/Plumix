using Avalonia;
using Avalonia.Media;
using Plumix.UI;
using Plumix.Foundation;
using Plumix.Painting;

// Dart parity source: flutter/packages/flutter/lib/src/painting/box_decoration.dart
// Dart parity source: flutter/packages/flutter/lib/src/painting/decoration.dart
// Dart parity source: flutter/packages/flutter/lib/src/painting/shape_decoration.dart
// Dart parity source: flutter/packages/flutter/lib/src/painting/border_radius.dart
// Dart parity source: flutter/packages/flutter/lib/src/painting/borders.dart

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
public abstract record Decoration : IDiagnosticable
{
    public abstract BoxPainter CreateBoxPainter(Action? onChanged = null);

    /// <inheritdoc />
    public virtual string ToStringShort() => Diagnostics.ObjectRuntimeType(this, "Decoration");

    /// <inheritdoc />
    public virtual DiagnosticsNode ToDiagnosticsNode(string? name = null, DiagnosticsTreeStyle? style = null)
        => new DiagnosticableNode<IDiagnosticable>(name, this, style);

    /// <inheritdoc />
    public virtual void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
    }

    /// Returns the insets to apply when using this decoration on a box that has contents.
    public virtual EdgeInsetsGeometry Padding => EdgeInsetsGeometry.Zero;

    /// Whether this decoration is complex enough to benefit from caching its painting.
    public virtual bool IsComplex => false;

    /// Tests whether the given point, on a box of the given size, would be considered a hit.
    public virtual bool HitTest(Size size, Point position, TextDirection? textDirection = null)
    {
        return true;
    }

    /// Returns the path this decoration would use to clip its contents.
    public virtual Plumix.UI.Path GetClipPath(Rect rect, TextDirection textDirection)
    {
        throw new NotSupportedException(
            $"{GetType().Name} does not expect to be used for clipping.");
    }

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

public readonly record struct Radius
{
    public Radius(double x, double y)
    {
        X = Math.Max(0.0, x);
        Y = Math.Max(0.0, y);
    }

    public double X { get; }

    public double Y { get; }

    public static Radius Zero => new(0.0, 0.0);

    public static Radius Circular(double radius)
    {
        double effectiveRadius = Math.Max(0.0, radius);
        return new Radius(effectiveRadius, effectiveRadius);
    }

    public static Radius Elliptical(double x, double y)
    {
        return new Radius(x, y);
    }

    public Radius Deflate(double amount)
    {
        return new Radius(Math.Max(0.0, X - amount), Math.Max(0.0, Y - amount));
    }

    public Radius Clamp(Radius maximum)
    {
        return new Radius(Math.Min(X, maximum.X), Math.Min(Y, maximum.Y));
    }

    public static Radius operator *(Radius radius, double factor)
    {
        return new Radius(radius.X * factor, radius.Y * factor);
    }

    public static Radius Lerp(Radius a, Radius b, double t)
    {
        return new Radius(
            a.X + ((b.X - a.X) * t),
            a.Y + ((b.Y - a.Y) * t));
    }
}

public readonly record struct BorderRadius
{
    public BorderRadius(double radius)
        : this(
            Plumix.Rendering.Radius.Circular(radius),
            Plumix.Rendering.Radius.Circular(radius),
            Plumix.Rendering.Radius.Circular(radius),
            Plumix.Rendering.Radius.Circular(radius))
    {
    }

    public BorderRadius(
        double topLeft,
        double topRight,
        double bottomRight,
        double bottomLeft)
        : this(
            Plumix.Rendering.Radius.Circular(topLeft),
            Plumix.Rendering.Radius.Circular(topRight),
            Plumix.Rendering.Radius.Circular(bottomRight),
            Plumix.Rendering.Radius.Circular(bottomLeft))
    {
    }

    public BorderRadius(
        Radius topLeft,
        Radius topRight,
        Radius bottomRight,
        Radius bottomLeft)
    {
        TopLeftRadius = topLeft;
        TopRightRadius = topRight;
        BottomRightRadius = bottomRight;
        BottomLeftRadius = bottomLeft;
    }

    public Radius TopLeftRadius { get; }

    public Radius TopRightRadius { get; }

    public Radius BottomRightRadius { get; }

    public Radius BottomLeftRadius { get; }

    public double TopLeft => TopLeftRadius.X;

    public double TopRight => TopRightRadius.X;

    public double BottomRight => BottomRightRadius.X;

    public double BottomLeft => BottomLeftRadius.X;

    public double Radius => TopLeft;

    public bool IsUniform => TopLeftRadius == TopRightRadius
                             && TopLeftRadius == BottomRightRadius
                             && TopLeftRadius == BottomLeftRadius;

    public static BorderRadius Zero => new(0);

    public static BorderRadius Circular(double radius)
    {
        return new(Math.Max(0, radius));
    }

    /// <summary>Dart's `BorderRadius.all`: the same (possibly elliptical) radius on every corner.</summary>
    public static BorderRadius All(Radius radius)
    {
        return new BorderRadius(radius, radius, radius, radius);
    }

    public static BorderRadius Only(
        double topLeft = 0.0,
        double topRight = 0.0,
        double bottomRight = 0.0,
        double bottomLeft = 0.0)
    {
        return new BorderRadius(topLeft, topRight, bottomRight, bottomLeft);
    }

    /// <summary>Dart's `BorderRadius.vertical`: one radius for both top corners, another for both bottom.</summary>
    public static BorderRadius Vertical(Radius? top = null, Radius? bottom = null)
    {
        Radius topRadius = top ?? default;
        Radius bottomRadius = bottom ?? default;
        return new BorderRadius(topRadius, topRadius, bottomRadius, bottomRadius);
    }

    /// <summary>Dart's `BorderRadius.horizontal`: one radius for both left corners, another for both right.</summary>
    public static BorderRadius Horizontal(Radius? left = null, Radius? right = null)
    {
        Radius leftRadius = left ?? default;
        Radius rightRadius = right ?? default;
        return new BorderRadius(leftRadius, rightRadius, rightRadius, leftRadius);
    }

    public static BorderRadius Only(
        Radius topLeft,
        Radius topRight,
        Radius bottomRight,
        Radius bottomLeft)
    {
        return new BorderRadius(topLeft, topRight, bottomRight, bottomLeft);
    }

    /// <summary>Dart's `BorderRadius.copyWith`: replaces only the corners that are supplied.</summary>
    public BorderRadius CopyWith(
        Radius? topLeft = null,
        Radius? topRight = null,
        Radius? bottomRight = null,
        Radius? bottomLeft = null)
    {
        return new BorderRadius(
            topLeft ?? TopLeftRadius,
            topRight ?? TopRightRadius,
            bottomRight ?? BottomRightRadius,
            bottomLeft ?? BottomLeftRadius);
    }

    public Plumix.UI.RRect ToRRect(Avalonia.Rect rect) => Plumix.UI.RRect.FromRectAndCorners(rect, this);

    public static BorderRadius operator *(BorderRadius radius, double factor)
    {
        return new BorderRadius(
            radius.TopLeftRadius * factor,
            radius.TopRightRadius * factor,
            radius.BottomRightRadius * factor,
            radius.BottomLeftRadius * factor);
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
            Plumix.Rendering.Radius.Lerp(from.TopLeftRadius, to.TopLeftRadius, t),
            Plumix.Rendering.Radius.Lerp(from.TopRightRadius, to.TopRightRadius, t),
            Plumix.Rendering.Radius.Lerp(from.BottomRightRadius, to.BottomRightRadius, t),
            Plumix.Rendering.Radius.Lerp(from.BottomLeftRadius, to.BottomLeftRadius, t));
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
                Add(Physical.TopLeftRadius, Directional.TopStart),
                Add(Physical.TopRightRadius, Directional.TopEnd),
                Add(Physical.BottomRightRadius, Directional.BottomEnd),
                Add(Physical.BottomLeftRadius, Directional.BottomStart))
            : new BorderRadius(
                Add(Physical.TopLeftRadius, Directional.TopEnd),
                Add(Physical.TopRightRadius, Directional.TopStart),
                Add(Physical.BottomRightRadius, Directional.BottomStart),
                Add(Physical.BottomLeftRadius, Directional.BottomEnd));
    }

    public bool IsZero => Physical == BorderRadius.Zero && Directional == default;

    public static BorderRadiusGeometry operator *(BorderRadiusGeometry radius, double factor)
    {
        return new BorderRadiusGeometry(
            radius.Physical * factor,
            new BorderRadiusDirectional(
                radius.Directional.TopStart * factor,
                radius.Directional.TopEnd * factor,
                radius.Directional.BottomEnd * factor,
                radius.Directional.BottomStart * factor));
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

    /// <summary>
    /// Adds a directional corner to a physical one. Directional corners are circular, so the
    /// physical corner keeps its own (possibly elliptical) shape when nothing is added to it.
    /// </summary>
    private static Radius Add(Radius radius, double directional)
    {
        return directional == 0.0
            ? radius
            : Radius.Elliptical(radius.X + directional, radius.Y + directional);
    }
}

public readonly record struct BorderSide
{
    public const double StrokeAlignInside = -1.0;
    public const double StrokeAlignCenter = 0.0;
    public const double StrokeAlignOutside = 1.0;

    public BorderSide(
        Color color,
        double width = 1.0,
        BorderStyle style = BorderStyle.Solid,
        double strokeAlign = StrokeAlignInside) : this()
    {
        Color = color;
        Width = Math.Max(0, width);
        Style = style;
        StrokeAlign = strokeAlign;
    }

    public Color Color { get; }

    public double Width { get; }

    public BorderStyle Style { get; }

    public double StrokeAlign { get; }

    public double StrokeInset => Width * (1.0 - ((1.0 + StrokeAlign) / 2.0));

    public double StrokeOutset => Width * ((1.0 + StrokeAlign) / 2.0);

    public double StrokeOffset => Width * StrokeAlign;

    public static BorderSide None => new(Color.FromRgb(0, 0, 0), 0.0, BorderStyle.None);

    public BorderSide CopyWith(
        Color? color = null,
        double? width = null,
        BorderStyle? style = null,
        double? strokeAlign = null) =>
        new(color ?? Color, width ?? Width, style ?? Style, strokeAlign ?? StrokeAlign);

    /// Whether the two given [BorderSide]s can be merged using [Merge].
    public static bool CanMerge(BorderSide a, BorderSide b)
    {
        if ((a.Style == BorderStyle.None && a.Width == 0.0)
            || (b.Style == BorderStyle.None && b.Width == 0.0))
        {
            return true;
        }

        return a.Style == b.Style && a.Color == b.Color;
    }

    /// Creates a [BorderSide] that represents the addition of the two given [BorderSide]s.
    public static BorderSide Merge(BorderSide a, BorderSide b)
    {
        if (!CanMerge(a, b))
        {
            throw new ArgumentException("The given border sides cannot be merged.");
        }

        bool aIsNone = a.Style == BorderStyle.None && a.Width == 0.0;
        bool bIsNone = b.Style == BorderStyle.None && b.Width == 0.0;
        if (aIsNone && bIsNone)
        {
            return None;
        }

        if (aIsNone)
        {
            return b;
        }

        if (bIsNone)
        {
            return a;
        }

        return new BorderSide(
            a.Color,
            a.Width + b.Width,
            a.Style,
            Math.Max(a.StrokeAlign, b.StrokeAlign));
    }

    /// Creates a stroke [IPen] that describes this border side, or null when nothing is painted.
    public IPen? ToPen()
    {
        return Style switch
        {
            BorderStyle.Solid => new Pen(new SolidColorBrush(Color), Width),
            _ => null,
        };
    }

    // Flutter does not carry strokeAlign through scale, so the result is always stroke-aligned inside.
    public BorderSide Scale(double t) => new(
        Color,
        Math.Max(0.0, Width * t),
        t <= 0.0 ? BorderStyle.None : Style);

    public static BorderSide Lerp(BorderSide a, BorderSide b, double t)
    {
        if (t == 0.0)
        {
            return a;
        }

        if (t == 1.0)
        {
            return b;
        }

        double width = a.Width + ((b.Width - a.Width) * t);
        if (width < 0.0)
        {
            return None;
        }

        if (a.Style == b.Style && a.StrokeAlign == b.StrokeAlign)
        {
            return new BorderSide(LerpColor(a.Color, b.Color, t), width, a.Style, a.StrokeAlign);
        }

        Color colorA = a.Style == BorderStyle.Solid ? a.Color : Color.FromArgb(0, a.Color.R, a.Color.G, a.Color.B);
        Color colorB = b.Style == BorderStyle.Solid ? b.Color : Color.FromArgb(0, b.Color.R, b.Color.G, b.Color.B);
        return new BorderSide(
            LerpColor(colorA, colorB, t),
            width,
            BorderStyle.Solid,
            a.StrokeAlign + ((b.StrokeAlign - a.StrokeAlign) * t));
    }

    private static Color LerpColor(Color a, Color b, double t) => Color.FromArgb(
        (byte)Math.Clamp(Math.Round(a.A + ((b.A - a.A) * t)), 0, 255),
        (byte)Math.Clamp(Math.Round(a.R + ((b.R - a.R) * t)), 0, 255),
        (byte)Math.Clamp(Math.Round(a.G + ((b.G - a.G) * t)), 0, 255),
        (byte)Math.Clamp(Math.Round(a.B + ((b.B - a.B) * t)), 0, 255));
}

// Dart parity source: flutter/packages/flutter/lib/src/painting/shape_decoration.dart
public sealed record ShapeDecoration(
    ShapeBorder Shape,
    Color? Color = null,
    Gradient? Gradient = null,
    DecorationImage? Image = null,
    IReadOnlyList<BoxShadow>? Shadows = null) : Decoration
{
    /// Creates a shape decoration configured to match a [BoxDecoration].
    public static ShapeDecoration FromBoxDecoration(BoxDecoration source)
    {
        ArgumentNullException.ThrowIfNull(source);
        ShapeBorder shape;
        switch (source.Shape)
        {
            case BoxShape.Circle:
                shape = source.Border is { } circleBorder
                    ? new CircleBorder(circleBorder.Top)
                    : new CircleBorder();
                break;
            default:
                shape = source.BorderRadius is { } radius
                    ? new RoundedRectangleBorder(source.Border?.Top ?? BorderSide.None, radius)
                    : source.Border ?? new Border();
                break;
        }

        return new ShapeDecoration(
            Shape: shape,
            Color: source.Color,
            Gradient: source.Gradient,
            Image: source.Image,
            Shadows: source.BoxShadows);
    }

    public override EdgeInsetsGeometry Padding => Shape.Dimensions;

    public override bool IsComplex => Shadows is { Count: > 0 };

    public override Plumix.UI.Path GetClipPath(Rect rect, TextDirection textDirection)
    {
        return Shape.GetOuterPath(rect, textDirection);
    }

    public override bool HitTest(Size size, Point position, TextDirection? textDirection = null)
    {
        return Shape.GetOuterPath(new Rect(new Point(0, 0), size), textDirection).Contains(position);
    }

    public override BoxPainter CreateBoxPainter(Action? onChanged = null)
    {
        return new ShapeDecorationPainter(this, onChanged);
    }

    public override Decoration? LerpFrom(Decoration? a, double t)
    {
        return a switch
        {
            BoxDecoration box => Lerp(FromBoxDecoration(box), this, t),
            ShapeDecoration or null => Lerp(a as ShapeDecoration, this, t),
            _ => base.LerpFrom(a, t),
        };
    }

    public override Decoration? LerpTo(Decoration? b, double t)
    {
        return b switch
        {
            BoxDecoration box => Lerp(this, FromBoxDecoration(box), t),
            ShapeDecoration or null => Lerp(this, b as ShapeDecoration, t),
            _ => base.LerpTo(b, t),
        };
    }

    public static ShapeDecoration? Lerp(ShapeDecoration? a, ShapeDecoration? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        if (a is not null && b is not null)
        {
            if (t == 0.0)
            {
                return a;
            }

            if (t == 1.0)
            {
                return b;
            }
        }

        // Dart bridges a plain color into a uniform gradient of the other side's kind, so a
        // color-to-gradient transition interpolates instead of cross-fading.
        Gradient? aGradient = a?.Gradient;
        Gradient? bGradient = b?.Gradient;
        if (aGradient is null && bGradient is not null && a?.Color is { } aColor)
        {
            aGradient = bGradient.FromColor(aColor);
        }
        else if (bGradient is null && aGradient is not null && b?.Color is { } bColor)
        {
            bGradient = aGradient.FromColor(bColor);
        }

        Gradient? gradient = Plumix.Rendering.Gradient.Lerp(aGradient, bGradient, t);
        return new ShapeDecoration(
            Shape: ShapeBorder.Lerp(a?.Shape, b?.Shape, t)!,
            Color: gradient is null ? BoxDecoration.LerpColor(a?.Color, b?.Color, t) : null,
            Gradient: gradient,
            Image: DecorationImage.Lerp(a?.Image, b?.Image, t),
            Shadows: BoxShadow.LerpList(a?.Shadows, b?.Shadows, t));
    }

    public bool Equals(ShapeDecoration? other)
    {
        return other is not null
               && Shape.Equals(other.Shape)
               && Nullable.Equals(Color, other.Color)
               && Equals(Gradient, other.Gradient)
               && Equals(Image, other.Image)
               && ShadowList.Equals(Shadows, other.Shadows);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Shape, Color, Gradient, Image, ShadowList.GetHashCode(Shadows));
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        ArgumentNullException.ThrowIfNull(properties);
        properties.DefaultDiagnosticsTreeStyle = DiagnosticsTreeStyle.Whitespace;
        properties.Add(new ColorProperty("color", Color, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<Gradient>(
            "gradient",
            Gradient,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<DecorationImage>(
            "image",
            Image,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new IterableProperty<BoxShadow>(
            "shadows",
            Shadows,
            defaultValue: DiagnosticsDefaults.NullValue,
            style: DiagnosticsTreeStyle.Whitespace));
        properties.Add(new DiagnosticsProperty<ShapeBorder>("shape", Shape));
    }
}

internal sealed class ShapeDecorationPainter : BoxPainter
{
    private readonly ShapeDecoration _decoration;
    private DecorationImagePainter? _imagePainter;

    public ShapeDecorationPainter(ShapeDecoration decoration, Action? onChanged = null) : base(onChanged)
    {
        _decoration = decoration;
    }

    public override void Paint(PaintingContext context, Point offset, ImageConfiguration configuration)
    {
        Size size = configuration.Size ?? default;
        var rect = new Rect(offset, size);
        TextDirection? textDirection = configuration.TextDirection;

        IBrush? fill = _decoration.Gradient?.CreateShader(rect, textDirection);
        if (fill is null && _decoration.Color.HasValue)
        {
            fill = new SolidColorBrush(_decoration.Color.Value);
        }

        RRect? outerRRect = TryResolveRRect(_decoration.Shape, rect, textDirection);
        if (_decoration.Shadows is { Count: > 0 } || fill is not null)
        {
            PaintInterior(context, rect, fill ?? Brushes.Transparent, textDirection, outerRRect);
        }

        if (_decoration.Image is not null)
        {
            _imagePainter ??= _decoration.Image.CreatePainter(HandleImageChanged);
            RRect? innerRRect = outerRRect?.Deflate(
                _decoration.Shape is OutlinedBorder outlined ? outlined.Side.StrokeInset : 0.0);
            _imagePainter.Paint(context, rect, configuration, clipRadius: innerRRect?.Radii);
        }

        _decoration.Shape.Paint(context, rect, textDirection);
    }

    private void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection,
        RRect? outerRRect)
    {
        if (_decoration.Shadows is { Count: > 0 } shadows && outerRRect is { } shadowRect)
        {
            context.DrawRectangle(brush, null, shadowRect.Rect, shadowRect.Radii, shadows.ToAvalonia());
            return;
        }

        if (_decoration.Shape.PreferPaintInterior)
        {
            _decoration.Shape.PaintInterior(context, rect, brush, textDirection);
            return;
        }

        context.DrawPath(_decoration.Shape.GetOuterPath(rect, textDirection), brush, null);
    }

    /// Resolves the rounded rectangle that matches the shape's outer path, when there is one.
    internal static RRect? TryResolveRRect(ShapeBorder shape, Rect rect, TextDirection? textDirection)
    {
        switch (shape)
        {
            case RoundedRectangleBorder rounded:
                return rounded.BorderRadius.Resolve(textDirection ?? TextDirection.Ltr).ToRRect(rect);
            case RoundedSuperellipseBorder rounded:
                return rounded.BorderRadius.Resolve(textDirection ?? TextDirection.Ltr).ToRRect(rect);
            case StadiumBorder:
                return RRect.FromRectAndRadius(rect, BoxBorder.ShortestSide(rect) / 2.0);
            case CircleBorder { Eccentricity: 0.0 }:
            {
                double radius = BoxBorder.ShortestSide(rect) / 2.0;
                var circleRect = new Rect(
                    rect.Center.X - radius,
                    rect.Center.Y - radius,
                    radius * 2.0,
                    radius * 2.0);
                return RRect.FromRectAndRadius(circleRect, radius);
            }

            case BoxBorder:
                return RRect.FromRectAndCorners(rect, BorderRadius.Zero);
            default:
                return null;
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

public sealed record BoxDecoration(
    Color? Color = null,
    Gradient? Gradient = null,
    BoxBorder? Border = null,
    BorderRadius? BorderRadius = null,
    IReadOnlyList<BoxShadow>? BoxShadows = null,
    DecorationImage? Image = null,
    BoxShape Shape = BoxShape.Rectangle) : Decoration
{
    public BorderRadius EffectiveBorderRadius => BorderRadius ?? Plumix.Rendering.BorderRadius.Zero;

    public override EdgeInsetsGeometry Padding => Border?.Dimensions ?? EdgeInsetsGeometry.Zero;

    public override bool IsComplex => BoxShadows is { Count: > 0 };

    public override bool HitTest(Size size, Point position, TextDirection? textDirection = null)
    {
        var rect = new Rect(new Point(0, 0), size);
        switch (Shape)
        {
            case BoxShape.Rectangle:
                if (BorderRadius is { } radius)
                {
                    var path = new Plumix.UI.Path();
                    path.AddRRect(radius.ToRRect(rect));
                    return path.Contains(position);
                }

                return true;
            case BoxShape.Circle:
                double deltaX = position.X - (size.Width / 2.0);
                double deltaY = position.Y - (size.Height / 2.0);
                double distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
                return distance <= Math.Min(size.Width, size.Height) / 2.0;
            default:
                return true;
        }
    }

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
            Gradient: Plumix.Rendering.Gradient.Lerp(a.Gradient, b.Gradient, t),
            Border: BoxBorder.Lerp(a.Border, b.Border, t),
            BorderRadius: LerpBorderRadius(a.BorderRadius, b.BorderRadius, t),
            BoxShadows: BoxShadow.LerpList(a.BoxShadows, b.BoxShadows, t),
            Image: DecorationImage.Lerp(a.Image, b.Image, t),
            Shape: t < 0.5 ? a.Shape : b.Shape);
    }

    private BoxDecoration Scale(double factor)
    {
        return this with
        {
            Color = LerpColor(null, Color, factor),
            Gradient = Gradient?.Scale(factor),
            Border = (BoxBorder?)Border?.Scale(factor),
            BorderRadius = Plumix.Rendering.BorderRadius.Lerp(null, BorderRadius, factor),
            BoxShadows = BoxShadow.LerpList(null, BoxShadows, factor),
            Image = DecorationImage.Lerp(null, Image, factor),
        };
    }

    public bool Equals(BoxDecoration? other)
    {
        return other is not null
               && Nullable.Equals(Color, other.Color)
               && Equals(Gradient, other.Gradient)
               && Equals(Border, other.Border)
               && Nullable.Equals(BorderRadius, other.BorderRadius)
               && ShadowList.Equals(BoxShadows, other.BoxShadows)
               && Equals(Image, other.Image)
               && Shape == other.Shape;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Color,
            Gradient,
            Border,
            BorderRadius,
            ShadowList.GetHashCode(BoxShadows),
            Image,
            Shape);
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

    private static BorderRadius? LerpBorderRadius(BorderRadius? a, BorderRadius? b, double t)
    {
        return Plumix.Rendering.BorderRadius.Lerp(a, b, t);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        ArgumentNullException.ThrowIfNull(properties);
        properties.DefaultDiagnosticsTreeStyle = DiagnosticsTreeStyle.Whitespace;
        properties.EmptyBodyDescription = "<no decorations specified>";

        properties.Add(new ColorProperty("color", Color, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<DecorationImage>(
            "image",
            Image,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<BoxBorder>(
            "border",
            Border,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<BorderRadius?>(
            "borderRadius",
            BorderRadius,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new IterableProperty<BoxShadow>(
            "boxShadow",
            BoxShadows,
            defaultValue: DiagnosticsDefaults.NullValue,
            style: DiagnosticsTreeStyle.Whitespace));
        properties.Add(new DiagnosticsProperty<Gradient>(
            "gradient",
            Gradient,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new EnumProperty<BoxShape>("shape", Shape, defaultValue: BoxShape.Rectangle));
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
        BoxShadows boxShadows = _decoration.BoxShadows.ToAvalonia();
        IBrush? fill = _decoration.Gradient?.CreateShader(rect, configuration.TextDirection);
        if (fill is null && _decoration.Color.HasValue)
        {
            fill = new SolidColorBrush(_decoration.Color.Value);
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

        _decoration.Border?.Paint(
            context,
            rect,
            configuration.TextDirection,
            _decoration.Shape,
            _decoration.BorderRadius);
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
