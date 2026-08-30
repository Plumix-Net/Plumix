using Avalonia;
using Avalonia.Media;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/box_border.dart

/// Base class for [Border] and [BorderDirectional]: a border of a box, comprised of four sides.
public abstract record BoxBorder : ShapeBorder
{
    public abstract BorderSide Top { get; }

    public abstract BorderSide Bottom { get; }

    /// Whether all four sides of the border are identical.
    public abstract bool IsUniform { get; }

    public static BoxBorder FromLTRB(
        BorderSide? top = null,
        BorderSide? right = null,
        BorderSide? bottom = null,
        BorderSide? left = null)
    {
        return new Border(top, right, bottom, left);
    }

    public static BoxBorder All(
        Color? color = null,
        double width = 1.0,
        BorderStyle style = BorderStyle.Solid,
        double strokeAlign = BorderSide.StrokeAlignInside)
    {
        return Border.All(color, width, style, strokeAlign);
    }

    public static BoxBorder FromBorderSide(BorderSide side)
    {
        return Border.FromBorderSide(side);
    }

    public static BoxBorder Symmetric(BorderSide? vertical = null, BorderSide? horizontal = null)
    {
        return Border.Symmetric(vertical, horizontal);
    }

    public static BoxBorder FromSTEB(
        BorderSide? top = null,
        BorderSide? start = null,
        BorderSide? end = null,
        BorderSide? bottom = null)
    {
        return new BorderDirectional(top, start, end, bottom);
    }

    public override ShapeBorder? Add(ShapeBorder other, bool reversed = false)
    {
        return null;
    }

    /// Linearly interpolates between two [BoxBorder]s.
    public static BoxBorder? Lerp(BoxBorder? a, BoxBorder? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        if (a is null or Border && b is null or Border)
        {
            return Border.Lerp((Border?)a, (Border?)b, t);
        }

        if (a is null or BorderDirectional && b is null or BorderDirectional)
        {
            return BorderDirectional.Lerp((BorderDirectional?)a, (BorderDirectional?)b, t);
        }

        if (b is Border && a is BorderDirectional)
        {
            (a, b) = (b, a);
            t = 1.0 - t;
        }

        if (a is Border visual && b is BorderDirectional directional)
        {
            if (directional.Start == BorderSide.None && directional.End == BorderSide.None)
            {
                return new Border(
                    BorderSide.Lerp(visual.Top, directional.Top, t),
                    BorderSide.Lerp(visual.Right, BorderSide.None, t),
                    BorderSide.Lerp(visual.Bottom, directional.Bottom, t),
                    BorderSide.Lerp(visual.Left, BorderSide.None, t));
            }

            if (visual.Left == BorderSide.None && visual.Right == BorderSide.None)
            {
                return new BorderDirectional(
                    BorderSide.Lerp(visual.Top, directional.Top, t),
                    BorderSide.Lerp(BorderSide.None, directional.Start, t),
                    BorderSide.Lerp(BorderSide.None, directional.End, t),
                    BorderSide.Lerp(visual.Bottom, directional.Bottom, t));
            }

            if (t < 0.5)
            {
                return new Border(
                    BorderSide.Lerp(visual.Top, directional.Top, t),
                    BorderSide.Lerp(visual.Right, BorderSide.None, t * 2.0),
                    BorderSide.Lerp(visual.Bottom, directional.Bottom, t),
                    BorderSide.Lerp(visual.Left, BorderSide.None, t * 2.0));
            }

            return new BorderDirectional(
                BorderSide.Lerp(visual.Top, directional.Top, t),
                BorderSide.Lerp(BorderSide.None, directional.Start, (t - 0.5) * 2.0),
                BorderSide.Lerp(BorderSide.None, directional.End, (t - 0.5) * 2.0),
                BorderSide.Lerp(visual.Bottom, directional.Bottom, t));
        }

        throw new InvalidOperationException(
            "BoxBorder.Lerp can only interpolate Border and BorderDirectional classes.\n"
            + $"BoxBorder.Lerp() was called with two objects of type {a?.GetType().Name} and "
            + $"{b?.GetType().Name}:\n  {a}\n  {b}\n"
            + "However, only Border and BorderDirectional classes are supported by this method.\n"
            + "For a more general interpolation method, consider using ShapeBorder.Lerp instead.");
    }

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
    {
        var path = new Path();
        path.AddRect(rect.Deflate(Dimensions.Resolve(textDirection ?? TextDirection.Ltr)));
        return path;
    }

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null)
    {
        var path = new Path();
        path.AddRect(rect);
        return path;
    }

    public override void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null)
    {
        context.Canvas.DrawRectangle(brush, null, rect);
    }

    public override bool PreferPaintInterior => true;

    public sealed override void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null)
    {
        Paint(context, rect, textDirection, BoxShape.Rectangle, null);
    }

    /// Paints the border within the given [Rect], honoring the box shape and border radius.
    public abstract void Paint(
        PaintingContext context,
        Rect rect,
        TextDirection? textDirection,
        BoxShape shape,
        BorderRadius? borderRadius);

    protected static void PaintUniformBorderWithRadius(
        PaintingContext context,
        Rect rect,
        BorderSide side,
        BorderRadius borderRadius)
    {
        var brush = new SolidColorBrush(side.Color);
        double width = side.Width;
        if (width == 0.0)
        {
            context.Canvas.DrawRRect(borderRadius.ToRRect(rect), null, new Pen(brush, 0.0));
            return;
        }

        RRect borderRect = borderRadius.ToRRect(rect);
        RRect inner = borderRect.Deflate(side.StrokeInset);
        RRect outer = borderRect.Inflate(side.StrokeOutset);
        context.Canvas.DrawDRRect(outer, inner, brush);
    }

    protected static void PaintUniformBorderWithCircle(PaintingContext context, Rect rect, BorderSide side)
    {
        double radius = (ShortestSide(rect) + side.StrokeOffset) / 2.0;
        context.Canvas.DrawCircle(Brushes.Transparent, side.ToPen(), rect.Center, radius);
    }

    protected static void PaintUniformBorderWithRectangle(PaintingContext context, Rect rect, BorderSide side)
    {
        context.Canvas.DrawRectangle(Brushes.Transparent, side.ToPen(), rect.Inflate(side.StrokeOffset / 2.0));
    }

    /// Paints a border with a single visible color but non-uniform widths.
    public static void PaintNonUniformBorder(
        PaintingContext context,
        Rect rect,
        BorderRadius? borderRadius,
        TextDirection? textDirection,
        BoxShape shape,
        BorderSide top,
        BorderSide right,
        BorderSide bottom,
        BorderSide left,
        Color color)
    {
        RRect borderRect;
        switch (shape)
        {
            case BoxShape.Rectangle:
                borderRect = (borderRadius ?? BorderRadius.Zero).ToRRect(rect);
                break;
            case BoxShape.Circle:
                if (borderRadius is not null)
                {
                    throw new InvalidOperationException(
                        "A circle cannot have a border radius. Remove either the shape or the borderRadius argument.");
                }

                double circleRadius = ShortestSide(rect) / 2.0;
                borderRect = RRect.FromRectAndRadius(
                    new Rect(
                        rect.Center.X - circleRadius,
                        rect.Center.Y - circleRadius,
                        circleRadius * 2.0,
                        circleRadius * 2.0),
                    Radius.Circular(rect.Width));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(shape));
        }

        var brush = new SolidColorBrush(color);
        RRect inner = borderRect.DeflateEdges(
            new Thickness(left.StrokeInset, top.StrokeInset, right.StrokeInset, bottom.StrokeInset));
        RRect outer = borderRect.InflateEdges(
            new Thickness(left.StrokeOutset, top.StrokeOutset, right.StrokeOutset, bottom.StrokeOutset));
        context.Canvas.DrawDRRect(outer, inner, brush);
    }

    internal static double ShortestSide(Rect rect)
    {
        return Math.Min(Math.Abs(rect.Width), Math.Abs(rect.Height));
    }
}

/// A border of a box, comprised of four sides: top, right, bottom, left.
public sealed record Border : BoxBorder
{
    public Border(
        BorderSide? top = null,
        BorderSide? right = null,
        BorderSide? bottom = null,
        BorderSide? left = null)
    {
        Top = top ?? BorderSide.None;
        Right = right ?? BorderSide.None;
        Bottom = bottom ?? BorderSide.None;
        Left = left ?? BorderSide.None;
    }

    public override BorderSide Top { get; }

    public BorderSide Right { get; }

    public override BorderSide Bottom { get; }

    public BorderSide Left { get; }

    public static Border FromBorderSide(BorderSide side)
    {
        return new Border(side, side, side, side);
    }

    public static Border Symmetric(BorderSide? vertical = null, BorderSide? horizontal = null)
    {
        return new Border(horizontal, vertical, horizontal, vertical);
    }

    public static Border All(
        Color? color = null,
        double width = 1.0,
        BorderStyle style = BorderStyle.Solid,
        double strokeAlign = BorderSide.StrokeAlignInside)
    {
        return FromBorderSide(new BorderSide(color ?? Color.FromRgb(0, 0, 0), width, style, strokeAlign));
    }

    /// Creates a [Border] that represents the addition of the two given [Border]s.
    public static Border Merge(Border a, Border b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return new Border(
            BorderSide.Merge(a.Top, b.Top),
            BorderSide.Merge(a.Right, b.Right),
            BorderSide.Merge(a.Bottom, b.Bottom),
            BorderSide.Merge(a.Left, b.Left));
    }

    public override EdgeInsetsGeometry Dimensions =>
        EdgeInsetsGeometry.FromLTRB(Left.StrokeInset, Top.StrokeInset, Right.StrokeInset, Bottom.StrokeInset);

    public override bool IsUniform => ColorIsUniform && WidthIsUniform && StyleIsUniform && StrokeAlignIsUniform;

    private bool ColorIsUniform =>
        Left.Color == Top.Color && Bottom.Color == Top.Color && Right.Color == Top.Color;

    private bool WidthIsUniform =>
        Left.Width == Top.Width && Bottom.Width == Top.Width && Right.Width == Top.Width;

    private bool StyleIsUniform =>
        Left.Style == Top.Style && Bottom.Style == Top.Style && Right.Style == Top.Style;

    private bool StrokeAlignIsUniform =>
        Left.StrokeAlign == Top.StrokeAlign
        && Bottom.StrokeAlign == Top.StrokeAlign
        && Right.StrokeAlign == Top.StrokeAlign;

    private IReadOnlyList<Color> DistinctVisibleColors()
    {
        var colors = new List<Color>(4);
        AddVisible(colors, Top);
        AddVisible(colors, Right);
        AddVisible(colors, Bottom);
        AddVisible(colors, Left);
        return colors;

        static void AddVisible(List<Color> target, BorderSide side)
        {
            if (side.Style != BorderStyle.None && !target.Contains(side.Color))
            {
                target.Add(side.Color);
            }
        }
    }

    private bool HasHairlineBorder =>
        IsHairline(Top) || IsHairline(Right) || IsHairline(Bottom) || IsHairline(Left);

    private static bool IsHairline(BorderSide side)
    {
        return side.Style == BorderStyle.Solid && side.Width == 0.0;
    }

    public override ShapeBorder? Add(ShapeBorder other, bool reversed = false)
    {
        if (other is not Border otherBorder
            || !BorderSide.CanMerge(Top, otherBorder.Top)
            || !BorderSide.CanMerge(Right, otherBorder.Right)
            || !BorderSide.CanMerge(Bottom, otherBorder.Bottom)
            || !BorderSide.CanMerge(Left, otherBorder.Left))
        {
            return null;
        }

        return Merge(this, otherBorder);
    }

    public override ShapeBorder Scale(double t)
    {
        return new Border(Top.Scale(t), Right.Scale(t), Bottom.Scale(t), Left.Scale(t));
    }

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        return a is Border border ? Lerp(border, this, t) : base.LerpFrom(a, t);
    }

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        return b is Border border ? Lerp(this, border, t) : base.LerpTo(b, t);
    }

    public static Border? Lerp(Border? a, Border? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        if (a is null)
        {
            return (Border)b!.Scale(t);
        }

        if (b is null)
        {
            return (Border)a.Scale(1.0 - t);
        }

        return new Border(
            BorderSide.Lerp(a.Top, b.Top, t),
            BorderSide.Lerp(a.Right, b.Right, t),
            BorderSide.Lerp(a.Bottom, b.Bottom, t),
            BorderSide.Lerp(a.Left, b.Left, t));
    }

    public override void Paint(
        PaintingContext context,
        Rect rect,
        TextDirection? textDirection,
        BoxShape shape,
        BorderRadius? borderRadius)
    {
        if (IsUniform)
        {
            switch (Top.Style)
            {
                case BorderStyle.None:
                    return;
                case BorderStyle.Solid:
                    switch (shape)
                    {
                        case BoxShape.Circle:
                            if (borderRadius is not null)
                            {
                                throw new InvalidOperationException(
                                    "A circle cannot have a border radius. Remove either the shape or the "
                                    + "borderRadius argument.");
                            }

                            PaintUniformBorderWithCircle(context, rect, Top);
                            break;
                        case BoxShape.Rectangle:
                            if (borderRadius is { } radius && radius != BorderRadius.Zero)
                            {
                                PaintUniformBorderWithRadius(context, rect, Top, radius);
                                return;
                            }

                            PaintUniformBorderWithRectangle(context, rect, Top);
                            break;
                    }

                    return;
            }
        }

        if (StyleIsUniform && Top.Style == BorderStyle.None)
        {
            return;
        }

        IReadOnlyList<Color> visibleColors = DistinctVisibleColors();
        bool hasHairlineBorder = HasHairlineBorder;
        if (visibleColors.Count == 1
            && !hasHairlineBorder
            && (shape == BoxShape.Circle || (borderRadius is { } cornerRadius && cornerRadius != BorderRadius.Zero)))
        {
            PaintNonUniformBorder(
                context,
                rect,
                borderRadius,
                textDirection,
                shape,
                Top.Style == BorderStyle.None ? BorderSide.None : Top,
                Right.Style == BorderStyle.None ? BorderSide.None : Right,
                Bottom.Style == BorderStyle.None ? BorderSide.None : Bottom,
                Left.Style == BorderStyle.None ? BorderSide.None : Left,
                visibleColors[0]);
            return;
        }

        if (hasHairlineBorder && borderRadius is { } hairlineRadius && hairlineRadius != BorderRadius.Zero)
        {
            throw new InvalidOperationException(
                "A hairline border like `BorderSide(width: 0.0, style: BorderStyle.solid)` can only be drawn when "
                + "BorderRadius is zero or null.");
        }

        if (borderRadius is { } uniformRadius && uniformRadius != BorderRadius.Zero)
        {
            throw new InvalidOperationException(
                "A borderRadius can only be given on borders with uniform colors.");
        }

        if (shape != BoxShape.Rectangle)
        {
            throw new InvalidOperationException(
                "A Border can only be drawn as a circle on borders with uniform colors.");
        }

        if (!StrokeAlignIsUniform || Top.StrokeAlign != BorderSide.StrokeAlignInside)
        {
            throw new InvalidOperationException(
                "A Border can only draw strokeAlign different than BorderSide.strokeAlignInside on borders with "
                + "uniform colors.");
        }

        BorderPainting.PaintBorder(context, rect, Top, Right, Bottom, Left);
    }

    public override string ToString()
    {
        if (IsUniform)
        {
            return $"Border.all({Top})";
        }

        var arguments = new List<string>(4);
        if (Top != BorderSide.None)
        {
            arguments.Add($"top: {Top}");
        }

        if (Right != BorderSide.None)
        {
            arguments.Add($"right: {Right}");
        }

        if (Bottom != BorderSide.None)
        {
            arguments.Add($"bottom: {Bottom}");
        }

        if (Left != BorderSide.None)
        {
            arguments.Add($"left: {Left}");
        }

        return $"Border({string.Join(", ", arguments)})";
    }
}

/// A border of a box, comprised of four sides, the lateral sides of which flip with text direction.
public sealed record BorderDirectional : BoxBorder
{
    public BorderDirectional(
        BorderSide? top = null,
        BorderSide? start = null,
        BorderSide? end = null,
        BorderSide? bottom = null)
    {
        Top = top ?? BorderSide.None;
        Start = start ?? BorderSide.None;
        End = end ?? BorderSide.None;
        Bottom = bottom ?? BorderSide.None;
    }

    public override BorderSide Top { get; }

    public BorderSide Start { get; }

    public BorderSide End { get; }

    public override BorderSide Bottom { get; }

    /// Creates a [BorderDirectional] that represents the addition of the two given [BorderDirectional]s.
    public static BorderDirectional Merge(BorderDirectional a, BorderDirectional b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return new BorderDirectional(
            BorderSide.Merge(a.Top, b.Top),
            BorderSide.Merge(a.Start, b.Start),
            BorderSide.Merge(a.End, b.End),
            BorderSide.Merge(a.Bottom, b.Bottom));
    }

    public override EdgeInsetsGeometry Dimensions => EdgeInsetsGeometry.DirectionalOnly(
        Start.StrokeInset,
        Top.StrokeInset,
        End.StrokeInset,
        Bottom.StrokeInset);

    public override bool IsUniform => ColorIsUniform && WidthIsUniform && StyleIsUniform && StrokeAlignIsUniform;

    private bool ColorIsUniform =>
        Start.Color == Top.Color && Bottom.Color == Top.Color && End.Color == Top.Color;

    private bool WidthIsUniform =>
        Start.Width == Top.Width && Bottom.Width == Top.Width && End.Width == Top.Width;

    private bool StyleIsUniform =>
        Start.Style == Top.Style && Bottom.Style == Top.Style && End.Style == Top.Style;

    private bool StrokeAlignIsUniform =>
        Start.StrokeAlign == Top.StrokeAlign
        && Bottom.StrokeAlign == Top.StrokeAlign
        && End.StrokeAlign == Top.StrokeAlign;

    private IReadOnlyList<Color> DistinctVisibleColors()
    {
        var colors = new List<Color>(4);
        AddVisible(colors, Top);
        AddVisible(colors, End);
        AddVisible(colors, Bottom);
        AddVisible(colors, Start);
        return colors;

        static void AddVisible(List<Color> target, BorderSide side)
        {
            if (side.Style != BorderStyle.None && !target.Contains(side.Color))
            {
                target.Add(side.Color);
            }
        }
    }

    private bool HasHairlineBorder =>
        IsHairline(Top) || IsHairline(End) || IsHairline(Bottom) || IsHairline(Start);

    private static bool IsHairline(BorderSide side)
    {
        return side.Style == BorderStyle.Solid && side.Width == 0.0;
    }

    public override ShapeBorder? Add(ShapeBorder other, bool reversed = false)
    {
        if (other is BorderDirectional otherDirectional)
        {
            if (!BorderSide.CanMerge(Top, otherDirectional.Top)
                || !BorderSide.CanMerge(Start, otherDirectional.Start)
                || !BorderSide.CanMerge(End, otherDirectional.End)
                || !BorderSide.CanMerge(Bottom, otherDirectional.Bottom))
            {
                return null;
            }

            return Merge(this, otherDirectional);
        }

        if (other is not Border otherBorder)
        {
            return null;
        }

        if (!BorderSide.CanMerge(otherBorder.Top, Top) || !BorderSide.CanMerge(otherBorder.Bottom, Bottom))
        {
            return null;
        }

        if (Start != BorderSide.None || End != BorderSide.None)
        {
            if (otherBorder.Left != BorderSide.None || otherBorder.Right != BorderSide.None)
            {
                return null;
            }

            return new BorderDirectional(
                BorderSide.Merge(otherBorder.Top, Top),
                Start,
                End,
                BorderSide.Merge(otherBorder.Bottom, Bottom));
        }

        return new Border(
            BorderSide.Merge(otherBorder.Top, Top),
            otherBorder.Right,
            BorderSide.Merge(otherBorder.Bottom, Bottom),
            otherBorder.Left);
    }

    public override ShapeBorder Scale(double t)
    {
        return new BorderDirectional(Top.Scale(t), Start.Scale(t), End.Scale(t), Bottom.Scale(t));
    }

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        return a is BorderDirectional border ? Lerp(border, this, t) : base.LerpFrom(a, t);
    }

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        return b is BorderDirectional border ? Lerp(this, border, t) : base.LerpTo(b, t);
    }

    public static BorderDirectional? Lerp(BorderDirectional? a, BorderDirectional? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        if (a is null)
        {
            return (BorderDirectional)b!.Scale(t);
        }

        if (b is null)
        {
            return (BorderDirectional)a.Scale(1.0 - t);
        }

        return new BorderDirectional(
            BorderSide.Lerp(a.Top, b.Top, t),
            BorderSide.Lerp(a.Start, b.Start, t),
            BorderSide.Lerp(a.End, b.End, t),
            BorderSide.Lerp(a.Bottom, b.Bottom, t));
    }

    public override void Paint(
        PaintingContext context,
        Rect rect,
        TextDirection? textDirection,
        BoxShape shape,
        BorderRadius? borderRadius)
    {
        if (IsUniform)
        {
            switch (Top.Style)
            {
                case BorderStyle.None:
                    return;
                case BorderStyle.Solid:
                    switch (shape)
                    {
                        case BoxShape.Circle:
                            if (borderRadius is not null)
                            {
                                throw new InvalidOperationException(
                                    "A circle cannot have a border radius. Remove either the shape or the "
                                    + "borderRadius argument.");
                            }

                            PaintUniformBorderWithCircle(context, rect, Top);
                            break;
                        case BoxShape.Rectangle:
                            if (borderRadius is { } radius && radius != BorderRadius.Zero)
                            {
                                PaintUniformBorderWithRadius(context, rect, Top, radius);
                                return;
                            }

                            PaintUniformBorderWithRectangle(context, rect, Top);
                            break;
                    }

                    return;
            }
        }

        if (StyleIsUniform && Top.Style == BorderStyle.None)
        {
            return;
        }

        if (textDirection is null)
        {
            throw new InvalidOperationException(
                "Non-uniform BorderDirectional objects require a TextDirection when painting.");
        }

        (BorderSide left, BorderSide right) = textDirection == TextDirection.Rtl
            ? (End, Start)
            : (Start, End);

        IReadOnlyList<Color> visibleColors = DistinctVisibleColors();
        bool hasHairlineBorder = HasHairlineBorder;
        if (visibleColors.Count == 1
            && !hasHairlineBorder
            && (shape == BoxShape.Circle || (borderRadius is { } cornerRadius && cornerRadius != BorderRadius.Zero)))
        {
            PaintNonUniformBorder(
                context,
                rect,
                borderRadius,
                textDirection,
                shape,
                Top.Style == BorderStyle.None ? BorderSide.None : Top,
                right.Style == BorderStyle.None ? BorderSide.None : right,
                Bottom.Style == BorderStyle.None ? BorderSide.None : Bottom,
                left.Style == BorderStyle.None ? BorderSide.None : left,
                visibleColors[0]);
            return;
        }

        if (hasHairlineBorder && borderRadius is { } hairlineRadius && hairlineRadius != BorderRadius.Zero)
        {
            throw new InvalidOperationException(
                "A side like `BorderSide(width: 0.0, style: BorderStyle.solid)` can only be drawn when BorderRadius "
                + "is zero or null.");
        }

        if (borderRadius is not null)
        {
            throw new InvalidOperationException(
                "A borderRadius can only be given for borders with uniform colors.");
        }

        if (shape != BoxShape.Rectangle)
        {
            throw new InvalidOperationException(
                "A Border can only be drawn as a circle on borders with uniform colors.");
        }

        if (!StrokeAlignIsUniform || Top.StrokeAlign != BorderSide.StrokeAlignInside)
        {
            throw new InvalidOperationException(
                "A Border can only draw strokeAlign different than strokeAlignInside on borders with uniform "
                + "colors.");
        }

        BorderPainting.PaintBorder(context, rect, Top, right, Bottom, left);
    }

    public override string ToString()
    {
        var arguments = new List<string>(4);
        if (Top != BorderSide.None)
        {
            arguments.Add($"top: {Top}");
        }

        if (Start != BorderSide.None)
        {
            arguments.Add($"start: {Start}");
        }

        if (End != BorderSide.None)
        {
            arguments.Add($"end: {End}");
        }

        if (Bottom != BorderSide.None)
        {
            arguments.Add($"bottom: {Bottom}");
        }

        return $"BorderDirectional({string.Join(", ", arguments)})";
    }
}
