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

    /// <summary>In debug mode, throws an <see cref="AssertionError"/> if the decoration is invalid.</summary>
    /// <remarks>Dart's <c>debugAssertIsValid</c>; always returns true, as Dart's does.</remarks>
    public virtual bool DebugAssertIsValid() => true;

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

    public static implicit operator Radius(double value) => Circular(value);

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

    /// <remarks>Flutter's <c>BorderRadius.toRSuperellipse</c>.</remarks>
    public Plumix.UI.RSuperellipse ToRSuperellipse(Avalonia.Rect rect) =>
        Plumix.UI.RSuperellipse.FromRectAndCorners(
            rect,
            TopLeftRadius,
            TopRightRadius,
            BottomRightRadius,
            BottomLeftRadius);

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

    /// <remarks>
    /// Dart's <c>BorderRadiusGeometry.resolve(TextDirection?)</c>: a purely physical radius ignores the
    /// direction, while a directional (or mixed) radius asserts that it is non-null.
    /// </remarks>
    public BorderRadius Resolve(TextDirection? direction)
    {
        System.Diagnostics.Debug.Assert(Directional == default || direction is not null);
        return direction is null or TextDirection.Ltr
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

    /// <inheritdoc />
    /// <remarks>
    /// Dart's <c>BorderRadiusGeometry.toString</c>: the physical part as <c>BorderRadius.circular</c>/
    /// <c>.all</c>/<c>.only</c>, the directional part as <c>BorderRadiusDirectional.*</c>, joined with
    /// <c> + </c> when both are non-zero, and <c>BorderRadius.zero</c> when neither is.
    /// </remarks>
    public override string ToString()
    {
        string? visual = null;
        string? logical = null;
        Radius topLeft = Physical.TopLeftRadius;
        Radius topRight = Physical.TopRightRadius;
        Radius bottomLeft = Physical.BottomLeftRadius;
        Radius bottomRight = Physical.BottomRightRadius;
        if (topLeft == topRight && topRight == bottomLeft && bottomLeft == bottomRight)
        {
            if (topLeft != Radius.Zero)
            {
                visual = topLeft.X == topLeft.Y
                    ? $"BorderRadius.circular({FormatDouble(topLeft.X)})"
                    : $"BorderRadius.all({DescribeRadius(topLeft)})";
            }
        }
        else
        {
            visual = DescribeOnly(
                "BorderRadius.only(",
                ("topLeft", topLeft),
                ("topRight", topRight),
                ("bottomLeft", bottomLeft),
                ("bottomRight", bottomRight));
        }

        Radius topStart = Radius.Circular(Directional.TopStart);
        Radius topEnd = Radius.Circular(Directional.TopEnd);
        Radius bottomEnd = Radius.Circular(Directional.BottomEnd);
        Radius bottomStart = Radius.Circular(Directional.BottomStart);
        if (topStart == topEnd && topEnd == bottomEnd && bottomEnd == bottomStart)
        {
            if (topStart != Radius.Zero)
            {
                logical = $"BorderRadiusDirectional.circular({FormatDouble(topStart.X)})";
            }
        }
        else
        {
            logical = DescribeOnly(
                "BorderRadiusDirectional.only(",
                ("topStart", topStart),
                ("topEnd", topEnd),
                ("bottomStart", bottomStart),
                ("bottomEnd", bottomEnd));
        }

        if (visual is not null && logical is not null)
        {
            return $"{visual} + {logical}";
        }

        return visual ?? logical ?? "BorderRadius.zero";
    }

    private static string DescribeOnly(string prefix, params (string Name, Radius Radius)[] corners)
    {
        var result = new System.Text.StringBuilder(prefix);
        bool comma = false;
        foreach ((string name, Radius radius) in corners)
        {
            if (radius == Radius.Zero)
            {
                continue;
            }

            if (comma)
            {
                result.Append(", ");
            }

            result.Append(name).Append(": ").Append(DescribeRadius(radius));
            comma = true;
        }

        result.Append(')');
        return result.ToString();
    }

    // dart:ui's `Radius.toString`.
    private static string DescribeRadius(Radius radius) => radius.X == radius.Y
        ? $"Radius.circular({FormatDouble(radius.X)})"
        : $"Radius.elliptical({FormatDouble(radius.X)}, {FormatDouble(radius.Y)})";

    // Dart's `toStringAsFixed(1)`.
    private static string FormatDouble(double value) =>
        value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

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

public readonly record struct BorderSide : IDiagnosticable
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

    public static BorderSide None => new(Color.FromARGB(0xFF, 0, 0, 0), 0.0, BorderStyle.None);

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

    /// <summary>Create a <see cref="Paint"/> object that, if used to stroke a line, will draw the line
    /// in this border's style.</summary>
    /// <remarks>
    /// Dart's <c>BorderSide.toPaint</c>. The <see cref="StrokeAlign"/> property is not reflected in the
    /// paint. Not all borders use this method to paint their border sides; for example, non-uniform
    /// rectangular borders are painted with filled shapes rather than strokes.
    /// </remarks>
    public Paint ToPaint()
    {
        return Style switch
        {
            BorderStyle.Solid => new Paint
            {
                Color = Color,
                StrokeWidth = Width,
                Style = PaintingStyle.Stroke,
            },
            _ => new Paint
            {
                Color = new Color(0x00000000),
                StrokeWidth = 0.0,
                Style = PaintingStyle.Stroke,
            },
        };
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

    /// <summary>Dart's <c>BorderSide.toStringShort</c>.</summary>
    public string ToStringShort() => "BorderSide";

    /// <summary>Dart's <c>BorderSide.debugFillProperties</c> (BorderSide is <c>Diagnosticable</c>).</summary>
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Add(new DiagnosticsProperty<Color>("color", Color, defaultValue: new Color(0xFF000000)));
        properties.Add(new DoubleProperty("width", Width, defaultValue: 1.0));
        properties.Add(new DoubleProperty("strokeAlign", StrokeAlign, defaultValue: StrokeAlignInside));
        properties.Add(new EnumProperty<BorderStyle>("style", Style, defaultValue: BorderStyle.Solid));
    }

    /// <summary>Dart's <c>Diagnosticable.toString</c>.</summary>
    public override string ToString()
    {
        IDiagnosticable self = this;
        return Constants.KDebugMode
            ? self.ToDiagnosticsNode(style: DiagnosticsTreeStyle.SingleLine).ToString(null, DiagnosticLevel.Info)
            : ToStringShort();
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

        Color colorA = a.Style == BorderStyle.Solid ? a.Color : a.Color.WithAlpha(0x00);
        Color colorB = b.Style == BorderStyle.Solid ? b.Color : b.Color.WithAlpha(0x00);
        return new BorderSide(
            LerpColor(colorA, colorB, t),
            width,
            BorderStyle.Solid,
            a.StrokeAlign + ((b.StrokeAlign - a.StrokeAlign) * t));
    }

    private static Color LerpColor(Color a, Color b, double t) => Color.Lerp(a, b, t);
}

// Dart parity source: flutter/packages/flutter/lib/src/painting/shape_decoration.dart
public sealed record ShapeDecoration : Decoration
{
    /// <summary>Creates a shape decoration.</summary>
    /// <remarks>Dart's <c>ShapeDecoration</c> constructor, including its
    /// <c>!(color != null &amp;&amp; gradient != null)</c> assert.</remarks>
    public ShapeDecoration(
        ShapeBorder Shape,
        Color? Color = null,
        Gradient? Gradient = null,
        DecorationImage? Image = null,
        IReadOnlyList<BoxShadow>? Shadows = null)
    {
        if (Constants.KDebugMode && Color is not null && Gradient is not null)
        {
            throw new AssertionError("A ShapeDecoration cannot have both a color and a gradient.");
        }

        this.Shape = Shape;
        this.Color = Color;
        this.Gradient = Gradient;
        this.Image = Image;
        this.Shadows = Shadows;
    }

    /// <summary>The shape to fill the <see cref="Color"/>, <see cref="Gradient"/>, and <see cref="Image"/>
    /// into and to cast as the <see cref="Shadows"/>.</summary>
    public ShapeBorder Shape { get; init; }

    /// <summary>The color to fill in the background of the shape.</summary>
    public Color? Color { get; init; }

    /// <summary>A gradient to use when filling the shape.</summary>
    public Gradient? Gradient { get; init; }

    /// <summary>An image to paint inside the shape (clipped to its outline).</summary>
    public DecorationImage? Image { get; init; }

    /// <summary>A list of shadows cast by the <see cref="Shape"/>.</summary>
    public IReadOnlyList<BoxShadow>? Shadows { get; init; }

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

    public override bool IsComplex => Shadows is not null;

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

// Dart parity source: flutter/packages/flutter/lib/src/painting/shape_decoration.dart (_ShapeDecorationPainter)
internal sealed class ShapeDecorationPainter : BoxPainter
{
    private readonly ShapeDecoration _decoration;

    private Rect? _lastRect;
    private TextDirection? _lastTextDirection;
    private Plumix.UI.Path _outerPath = null!;
    private Plumix.UI.Path? _innerPath;
    private Paint? _interiorPaint;
    private int? _shadowCount;
    private List<Rect> _shadowBounds = null!;
    private List<Plumix.UI.Path> _shadowPaths = null!;
    private List<Paint> _shadowPaints = null!;
    private DecorationImagePainter? _imagePainter;

    public ShapeDecorationPainter(ShapeDecoration decoration, Action? onChanged = null) : base(onChanged)
    {
        _decoration = decoration;
    }

    private void Precache(Rect rect, TextDirection? textDirection)
    {
        if (rect == _lastRect && textDirection == _lastTextDirection)
        {
            return;
        }

        // We reach here in two cases:
        //  - the very first time we paint, in which case everything except _decoration is null
        //  - subsequent times, if the rect has changed, in which case we only need to update
        //    the features that depend on the actual rect.
        if (_interiorPaint is null && (_decoration.Color is not null || _decoration.Gradient is not null))
        {
            _interiorPaint = new Paint();
            if (_decoration.Color is { } color)
            {
                _interiorPaint.Color = color;
            }
        }

        if (_decoration.Gradient is not null)
        {
            _interiorPaint!.Shader = _decoration.Gradient.CreateShader(rect, textDirection);
        }

        if (_decoration.Shadows is { } shadows)
        {
            if (_shadowCount is null)
            {
                _shadowCount = shadows.Count;
                _shadowPaints = [.. shadows.Select(static shadow => shadow.ToPaint())];
            }

            if (_decoration.Shape.PreferPaintInterior)
            {
                _shadowBounds = [.. shadows.Select(shadow => ShiftInflate(rect, shadow))];
            }
            else
            {
                _shadowPaths = [.. shadows.Select(shadow =>
                    _decoration.Shape.GetOuterPath(ShiftInflate(rect, shadow), textDirection))];
            }
        }

        if (!_decoration.Shape.PreferPaintInterior && (_interiorPaint is not null || _shadowCount is not null))
        {
            _outerPath = _decoration.Shape.GetOuterPath(rect, textDirection);
        }

        if (_decoration.Image is not null)
        {
            _innerPath = _decoration.Shape.GetInnerPath(rect, textDirection);
        }

        _lastRect = rect;
        _lastTextDirection = textDirection;
    }

    // Dart's `rect.shift(shadow.offset).inflate(shadow.spreadRadius)`.
    internal static Rect ShiftInflate(Rect rect, BoxShadow shadow) => DartGeometry.RectFromLTRB(
        rect.Left + shadow.Offset.X - shadow.SpreadRadius,
        rect.Top + shadow.Offset.Y - shadow.SpreadRadius,
        rect.Right + shadow.Offset.X + shadow.SpreadRadius,
        rect.Bottom + shadow.Offset.Y + shadow.SpreadRadius);

    private void PaintShadows(Canvas canvas, PaintingContext context, Rect rect, TextDirection? textDirection)
    {
        // The DebugHandleDisabledShadowStart and DebugHandleDisabledShadowEnd methods are used in
        // debug mode only to support BlurStyle.outer when RenderingDebug.DisableShadows is set.
        // Without these clips, the shadows would extend to the inside of the shape, which would
        // likely obscure important portions of the rendering and would cause unit tests of widgets
        // that use BlurStyle.outer to significantly diverge from the original intent.
        void DebugHandleDisabledShadowStart(BoxShadow boxShadow, Plumix.UI.Path path)
        {
            if (Constants.KDebugMode && RenderingDebug.DisableShadows && boxShadow.BlurStyle == BlurStyle.Outer)
            {
                canvas.Save();
                var clipPath = new Plumix.UI.Path { FillType = PathFillType.EvenOdd };
                clipPath.AddRect(LargestRect);
                clipPath.AddPath(path);
                canvas.ClipPath(clipPath);
            }
        }

        void DebugHandleDisabledShadowEnd(BoxShadow boxShadow)
        {
            if (Constants.KDebugMode && RenderingDebug.DisableShadows && boxShadow.BlurStyle == BlurStyle.Outer)
            {
                canvas.Restore();
            }
        }

        if (_shadowCount is not { } shadowCount)
        {
            return;
        }

        IReadOnlyList<BoxShadow> shadows = _decoration.Shadows!;
        if (_decoration.Shape.PreferPaintInterior)
        {
            for (int index = 0; index < shadowCount; index += 1)
            {
                DebugHandleDisabledShadowStart(
                    shadows[index],
                    _decoration.Shape.GetOuterPath(_shadowBounds[index], textDirection));
                _decoration.Shape.PaintInterior(context, _shadowBounds[index], _shadowPaints[index], textDirection);
                DebugHandleDisabledShadowEnd(shadows[index]);
            }
        }
        else
        {
            for (int index = 0; index < shadowCount; index += 1)
            {
                DebugHandleDisabledShadowStart(shadows[index], _shadowPaths[index]);
                canvas.DrawPath(_shadowPaths[index], _shadowPaints[index]);
                DebugHandleDisabledShadowEnd(shadows[index]);
            }
        }
    }

    private void PaintInterior(Canvas canvas, PaintingContext context, Rect rect, TextDirection? textDirection)
    {
        if (_interiorPaint is null)
        {
            return;
        }

        if (_decoration.Shape.PreferPaintInterior)
        {
            // When border is filled, the rect is reduced to avoid anti-aliasing
            // rounding error leaking the background color around the clipped shape.
            Rect adjustedRect = AdjustedRectOnOutlinedBorder(rect);
            _decoration.Shape.PaintInterior(context, adjustedRect, _interiorPaint, textDirection);
        }
        else
        {
            canvas.DrawPath(_outerPath, _interiorPaint);
        }
    }

    private Rect AdjustedRectOnOutlinedBorder(Rect rect)
    {
        if (_decoration.Shape is OutlinedBorder outlined && _decoration.Color is not null)
        {
            BorderSide side = outlined.Side;
            if (side.Color.Alpha == 255 && side.Style == BorderStyle.Solid)
            {
                double inset = side.StrokeInset / 2.0;
                return DartGeometry.RectFromLTRB(
                    rect.Left + inset,
                    rect.Top + inset,
                    rect.Right - inset,
                    rect.Bottom - inset);
            }
        }

        return rect;
    }

    private void PaintImage(PaintingContext context, ImageConfiguration configuration)
    {
        if (_decoration.Image is null)
        {
            return;
        }

        _imagePainter ??= _decoration.Image.CreatePainter(HandleImageChanged);
        _imagePainter.Paint(context, _lastRect!.Value, _innerPath, configuration);
    }

    public override void Dispose()
    {
        _imagePainter?.Dispose();
        _imagePainter = null;
    }

    public override void Paint(PaintingContext context, Point offset, ImageConfiguration configuration)
    {
        if (Constants.KDebugMode && configuration.Size is null)
        {
            throw new AssertionError("A ShapeDecoration painter needs an ImageConfiguration with a size.");
        }

        var rect = new Rect(offset, configuration.Size ?? default);
        TextDirection? textDirection = configuration.TextDirection;
        Canvas canvas = context.Canvas;
        Precache(rect, textDirection);
        PaintShadows(canvas, context, rect, textDirection);
        PaintInterior(canvas, context, rect, textDirection);
        PaintImage(context, configuration);
        _decoration.Shape.Paint(context, rect, textDirection);
    }

    // Dart's `Rect.largest`.
    internal static readonly Rect LargestRect = DartGeometry.RectFromLTRB(-1.0e9, -1.0e9, 1.0e9, 1.0e9);

    private void HandleImageChanged()
    {
        OnChanged?.Invoke();
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/painting/box_decoration.dart
public sealed record BoxDecoration : Decoration
{
    /// <summary>Creates a box decoration.</summary>
    /// <remarks>
    /// Dart's <c>BoxDecoration</c> constructor; the parameter order keeps Plumix's historical positional
    /// order, with <paramref name="BackgroundBlendMode"/> appended. The <c>backgroundBlendMode</c> assert
    /// runs here, as Dart's constructor initializer does.
    /// </remarks>
    public BoxDecoration(
        Color? Color = null,
        Gradient? Gradient = null,
        BoxBorder? Border = null,
        BorderRadiusGeometry? BorderRadius = null,
        IReadOnlyList<BoxShadow>? BoxShadows = null,
        DecorationImage? Image = null,
        BoxShape Shape = BoxShape.Rectangle,
        BlendMode? BackgroundBlendMode = null)
    {
        if (Constants.KDebugMode && BackgroundBlendMode is not null && Color is null && Gradient is null)
        {
            throw new AssertionError(
                "backgroundBlendMode applies to BoxDecoration's background color or "
                + "gradient, but no color or gradient was provided.");
        }

        this.Color = Color;
        this.Gradient = Gradient;
        this.Border = Border;
        this.BorderRadius = BorderRadius;
        this.BoxShadows = BoxShadows;
        this.Image = Image;
        this.Shape = Shape;
        this.BackgroundBlendMode = BackgroundBlendMode;
    }

    /// <summary>Creates a copy of this object but with the given fields replaced with the new values.</summary>
    public BoxDecoration CopyWith(
        Color? color = null,
        DecorationImage? image = null,
        BoxBorder? border = null,
        BorderRadiusGeometry? borderRadius = null,
        IReadOnlyList<BoxShadow>? boxShadow = null,
        Gradient? gradient = null,
        BlendMode? backgroundBlendMode = null,
        BoxShape? shape = null)
    {
        return new BoxDecoration(
            Color: color ?? Color,
            Image: image ?? Image,
            Border: border ?? Border,
            BorderRadius: borderRadius ?? BorderRadius,
            BoxShadows: boxShadow ?? BoxShadows,
            Gradient: gradient ?? Gradient,
            BackgroundBlendMode: backgroundBlendMode ?? BackgroundBlendMode,
            Shape: shape ?? Shape);
    }

    public override bool DebugAssertIsValid()
    {
        if (Constants.KDebugMode && Shape == BoxShape.Circle && BorderRadius is not null)
        {
            // Can't have a border radius if you're a circle.
            throw new AssertionError(CircleBorderRadiusMessage);
        }

        return base.DebugAssertIsValid();
    }

    internal const string CircleBorderRadiusMessage =
        "A circle cannot have a border radius. Remove either the shape or the borderRadius argument.";

    /// <summary>The color to fill in the background of the box.</summary>
    public Color? Color { get; init; }

    /// <summary>An image to paint above the background <see cref="Color"/> or <see cref="Gradient"/>.</summary>
    public DecorationImage? Image { get; init; }

    /// <summary>A border to draw above the background <see cref="Color"/>, <see cref="Gradient"/>, or
    /// <see cref="Image"/>.</summary>
    public BoxBorder? Border { get; init; }

    /// <summary>If non-null, the corners of this box are rounded by this radius.</summary>
    /// <remarks>Applies only to boxes with rectangular shapes; ignored if <see cref="Shape"/> is not
    /// <see cref="BoxShape.Rectangle"/>.</remarks>
    public BorderRadiusGeometry? BorderRadius { get; init; }

    /// <summary>A list of shadows cast by this box behind the box (Dart's <c>boxShadow</c>).</summary>
    public IReadOnlyList<BoxShadow>? BoxShadows { get; init; }

    /// <summary>A gradient to use when filling the box.</summary>
    public Gradient? Gradient { get; init; }

    /// <summary>The blend mode applied to the <see cref="Color"/> or <see cref="Gradient"/> background of
    /// the box.</summary>
    /// <remarks>If no <see cref="BackgroundBlendMode"/> is provided then the default painting blend mode
    /// is used.</remarks>
    public BlendMode? BackgroundBlendMode { get; init; }

    /// <summary>The shape to fill the background <see cref="Color"/>, <see cref="Gradient"/>, and
    /// <see cref="Image"/> into and to cast as the <see cref="BoxShadows"/>.</summary>
    public BoxShape Shape { get; init; }

    public override EdgeInsetsGeometry Padding => Border?.Dimensions ?? EdgeInsetsGeometry.Zero;

    public override Plumix.UI.Path GetClipPath(Rect rect, TextDirection textDirection)
    {
        var path = new Plumix.UI.Path();
        switch (Shape)
        {
            case BoxShape.Circle:
                Point center = rect.Center;
                double radius = DartGeometry.ShortestSide(rect) / 2.0;
                path.AddOval(DartGeometry.RectFromCircle(center, radius));
                return path;
            default:
                if (BorderRadius is { } borderRadius)
                {
                    path.AddRRect(borderRadius.Resolve(textDirection).ToRRect(rect));
                    return path;
                }

                path.AddRect(rect);
                return path;
        }
    }

    /// <summary>Returns a new box decoration that is scaled by the given factor.</summary>
    /// <remarks>Dart's <c>scale</c>; like Dart's, the result has no <see cref="BackgroundBlendMode"/>.</remarks>
    public BoxDecoration Scale(double factor)
    {
        return new BoxDecoration(
            Color: LerpColor(null, Color, factor),
            Image: DecorationImage.Lerp(null, Image, factor),
            Border: BoxBorder.Lerp(null, Border, factor),
            BorderRadius: BorderRadiusGeometry.Lerp(null, BorderRadius, factor),
            BoxShadows: BoxShadow.LerpList(null, BoxShadows, factor),
            Gradient: Gradient?.Scale(factor),
            Shape: Shape);
    }

    public override bool IsComplex => BoxShadows is not null;

    public override Decoration? LerpFrom(Decoration? a, double t) => a switch
    {
        null => Scale(t),
        BoxDecoration box => Lerp(box, this, t),
        _ => base.LerpFrom(a, t) as BoxDecoration,
    };

    public override Decoration? LerpTo(Decoration? b, double t) => b switch
    {
        null => Scale(1.0 - t),
        BoxDecoration box => Lerp(this, box, t),
        _ => base.LerpTo(b, t) as BoxDecoration,
    };

    /// <summary>Linearly interpolate between two box decorations.</summary>
    /// <remarks>Dart's <c>BoxDecoration.lerp</c>; like Dart's, the result has no
    /// <see cref="BackgroundBlendMode"/>.</remarks>
    public static BoxDecoration? Lerp(BoxDecoration? a, BoxDecoration? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        if (a is null)
        {
            return b!.Scale(t);
        }

        if (b is null)
        {
            return a.Scale(1.0 - t);
        }

        if (t == 0.0)
        {
            return a;
        }

        if (t == 1.0)
        {
            return b;
        }

        return new BoxDecoration(
            Color: LerpColor(a.Color, b.Color, t),
            Image: DecorationImage.Lerp(a.Image, b.Image, t),
            Border: BoxBorder.Lerp(a.Border, b.Border, t),
            BorderRadius: BorderRadiusGeometry.Lerp(a.BorderRadius, b.BorderRadius, t),
            BoxShadows: BoxShadow.LerpList(a.BoxShadows, b.BoxShadows, t),
            Gradient: Plumix.Rendering.Gradient.Lerp(a.Gradient, b.Gradient, t),
            Shape: t < 0.5 ? a.Shape : b.Shape);
    }

    public bool Equals(BoxDecoration? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null
               && Nullable.Equals(other.Color, Color)
               && Equals(other.Image, Image)
               && Equals(other.Border, Border)
               && Nullable.Equals(other.BorderRadius, BorderRadius)
               && ShadowList.Equals(other.BoxShadows, BoxShadows)
               && Equals(other.Gradient, Gradient)
               && other.BackgroundBlendMode == BackgroundBlendMode
               && other.Shape == Shape;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Color,
            Image,
            Border,
            BorderRadius,
            BoxShadows is null ? (int?)null : ShadowList.GetHashCode(BoxShadows),
            Gradient,
            BackgroundBlendMode,
            Shape);
    }

    internal static Color? LerpColor(Color? a, Color? b, double t) => Plumix.UI.Color.Lerp(a, b, t);

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
        properties.Add(new DiagnosticsProperty<BorderRadiusGeometry?>(
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

    public override bool HitTest(Size size, Point position, TextDirection? textDirection = null)
    {
        var bounds = new Rect(size);
        if (Constants.KDebugMode && !bounds.Contains(position))
        {
            throw new AssertionError($"The hit-test position {position} is outside the box {size}.");
        }

        switch (Shape)
        {
            case BoxShape.Rectangle:
                if (BorderRadius is { } borderRadius)
                {
                    return borderRadius.Resolve(textDirection).ToRRect(bounds).Contains(position);
                }

                return true;
            case BoxShape.Circle:
                // Circles are inscribed into our smallest dimension.
                Point center = bounds.Center;
                double deltaX = position.X - center.X;
                double deltaY = position.Y - center.Y;
                double distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
                return distance <= Math.Min(size.Width, size.Height) / 2.0;
            default:
                return true;
        }
    }

    public override BoxPainter CreateBoxPainter(Action? onChanged = null)
    {
        if (Constants.KDebugMode && onChanged is null && Image is not null)
        {
            throw new AssertionError("A BoxDecoration with an image needs an onChanged callback.");
        }

        return new BoxDecorationPainter(this, onChanged);
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/painting/box_decoration.dart (_BoxDecorationPainter)
/// <summary>An object that paints a <see cref="BoxDecoration"/> into a canvas.</summary>
internal sealed class BoxDecorationPainter : BoxPainter
{
    private readonly BoxDecoration _decoration;
    private Paint? _cachedBackgroundPaint;
    private Rect? _rectForCachedBackgroundPaint;
    private DecorationImagePainter? _imagePainter;

    public BoxDecorationPainter(BoxDecoration decoration, Action? onChanged = null) : base(onChanged)
    {
        _decoration = decoration;
    }

    private Paint GetBackgroundPaint(Rect rect, TextDirection? textDirection)
    {
        if (Constants.KDebugMode && _decoration.Gradient is null && _rectForCachedBackgroundPaint is not null)
        {
            throw new AssertionError();
        }

        if (_cachedBackgroundPaint is null
            || (_decoration.Gradient is not null && _rectForCachedBackgroundPaint != rect))
        {
            var paint = new Paint();
            if (_decoration.BackgroundBlendMode is { } blendMode)
            {
                paint.BlendMode = blendMode;
            }

            if (_decoration.Color is { } color)
            {
                paint.Color = color;
            }

            if (_decoration.Gradient is { } gradient)
            {
                paint.Shader = gradient.CreateShader(rect, textDirection);
                _rectForCachedBackgroundPaint = rect;
            }

            _cachedBackgroundPaint = paint;
        }

        return _cachedBackgroundPaint;
    }

    private void PaintBox(Canvas canvas, Rect rect, Paint paint, TextDirection? textDirection)
    {
        switch (_decoration.Shape)
        {
            case BoxShape.Circle:
                if (Constants.KDebugMode && _decoration.BorderRadius is not null)
                {
                    throw new AssertionError(BoxDecoration.CircleBorderRadiusMessage);
                }

                Point center = rect.Center;
                double radius = DartGeometry.ShortestSide(rect) / 2.0;
                canvas.DrawCircle(center, radius, paint);
                break;
            case BoxShape.Rectangle:
                if (_decoration.BorderRadius is not { } borderRadius || borderRadius == BorderRadius.Zero)
                {
                    canvas.DrawRect(rect, paint);
                }
                else
                {
                    canvas.DrawRRect(borderRadius.Resolve(textDirection).ToRRect(rect), paint);
                }

                break;
        }
    }

    private void PaintShadows(Canvas canvas, Rect rect, TextDirection? textDirection)
    {
        if (_decoration.BoxShadows is not { } boxShadows)
        {
            return;
        }

        foreach (BoxShadow boxShadow in boxShadows)
        {
            Paint paint = boxShadow.ToPaint();
            Rect bounds = ShapeDecorationPainter.ShiftInflate(rect, boxShadow);
            bool clipOuter = Constants.KDebugMode
                             && RenderingDebug.DisableShadows
                             && boxShadow.BlurStyle == BlurStyle.Outer;
            if (clipOuter)
            {
                canvas.Save();
                canvas.ClipRect(bounds);
            }

            PaintBox(canvas, bounds, paint, textDirection);
            if (clipOuter)
            {
                canvas.Restore();
            }
        }
    }

    private void PaintBackgroundColor(Canvas canvas, Rect rect, TextDirection? textDirection)
    {
        if (_decoration.Color is not null || _decoration.Gradient is not null)
        {
            // When border is filled, the rect is reduced to avoid anti-aliasing
            // rounding error leaking the background color around the clipped shape.
            Rect adjustedRect = AdjustedRectOnOutlinedBorder(rect, textDirection);
            PaintBox(canvas, adjustedRect, GetBackgroundPaint(rect, textDirection), textDirection);
        }
    }

    private static double CalculateAdjustedSide(BorderSide side)
    {
        if (side.Color.Alpha == 255 && side.Style == BorderStyle.Solid)
        {
            return side.StrokeInset;
        }

        return 0;
    }

    private Rect AdjustedRectOnOutlinedBorder(Rect rect, TextDirection? textDirection)
    {
        switch (_decoration.Border)
        {
            case null:
                return rect;
            case Border border:
                return Deflate(
                    rect,
                    CalculateAdjustedSide(border.Left) / 2.0,
                    CalculateAdjustedSide(border.Top) / 2.0,
                    CalculateAdjustedSide(border.Right) / 2.0,
                    CalculateAdjustedSide(border.Bottom) / 2.0);
            case BorderDirectional directional when textDirection is not null:
                BorderSide leftSide = textDirection == TextDirection.Rtl ? directional.End : directional.Start;
                BorderSide rightSide = textDirection == TextDirection.Rtl ? directional.Start : directional.End;
                return Deflate(
                    rect,
                    CalculateAdjustedSide(leftSide) / 2.0,
                    CalculateAdjustedSide(directional.Top) / 2.0,
                    CalculateAdjustedSide(rightSide) / 2.0,
                    CalculateAdjustedSide(directional.Bottom) / 2.0);
            default:
                return rect;
        }
    }

    private static Rect Deflate(Rect rect, double left, double top, double right, double bottom) =>
        DartGeometry.RectFromLTRB(rect.Left + left, rect.Top + top, rect.Right - right, rect.Bottom - bottom);

    private void PaintBackgroundImage(PaintingContext context, Rect rect, ImageConfiguration configuration)
    {
        if (_decoration.Image is null)
        {
            return;
        }

        _imagePainter ??= _decoration.Image.CreatePainter(HandleImageChanged);
        Plumix.UI.Path? clipPath = null;
        switch (_decoration.Shape)
        {
            case BoxShape.Circle:
                if (Constants.KDebugMode && _decoration.BorderRadius is not null)
                {
                    throw new AssertionError(BoxDecoration.CircleBorderRadiusMessage);
                }

                Point center = rect.Center;
                double radius = DartGeometry.ShortestSide(rect) / 2.0;
                Rect square = DartGeometry.RectFromCircle(center, radius);
                clipPath = new Plumix.UI.Path();
                clipPath.AddOval(square);
                break;
            case BoxShape.Rectangle:
                if (_decoration.BorderRadius is { } borderRadius)
                {
                    clipPath = new Plumix.UI.Path();
                    clipPath.AddRRect(borderRadius.Resolve(configuration.TextDirection).ToRRect(rect));
                }

                break;
        }

        _imagePainter.Paint(context, rect, clipPath, configuration);
    }

    public override void Dispose()
    {
        _imagePainter?.Dispose();
        _imagePainter = null;
    }

    /// <summary>Paint the box decoration into the given location on the given canvas.</summary>
    public override void Paint(
        PaintingContext context,
        Point offset,
        ImageConfiguration configuration)
    {
        if (Constants.KDebugMode && configuration.Size is null)
        {
            throw new AssertionError("A BoxDecoration painter needs an ImageConfiguration with a size.");
        }

        var rect = new Rect(offset, configuration.Size ?? default);
        TextDirection? textDirection = configuration.TextDirection;
        Canvas canvas = context.Canvas;
        PaintShadows(canvas, rect, textDirection);
        PaintBackgroundColor(canvas, rect, textDirection);
        PaintBackgroundImage(context, rect, configuration);
        _decoration.Border?.Paint(
            context,
            rect,
            configuration.TextDirection,
            _decoration.Shape,
            _decoration.BorderRadius?.Resolve(textDirection));
    }

    public override string ToString() => $"BoxPainter for {_decoration}";

    private void HandleImageChanged()
    {
        OnChanged?.Invoke();
    }
}
