using Avalonia;
using Plumix.UI;
using Plumix.Widgets;
using Path = Plumix.UI.Path;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/table_border.dart

/// Border specification for [Table] widgets.
///
/// This is like [BoxBorder], with the addition of two sides: the inner horizontal
/// borders between rows and the inner vertical borders between columns.
public sealed record TableBorder
{
    public TableBorder(
        BorderSide? top = null,
        BorderSide? right = null,
        BorderSide? bottom = null,
        BorderSide? left = null,
        BorderSide? horizontalInside = null,
        BorderSide? verticalInside = null,
        BorderRadius? borderRadius = null)
    {
        Top = top ?? BorderSide.None;
        Right = right ?? BorderSide.None;
        Bottom = bottom ?? BorderSide.None;
        Left = left ?? BorderSide.None;
        HorizontalInside = horizontalInside ?? BorderSide.None;
        VerticalInside = verticalInside ?? BorderSide.None;
        BorderRadius = borderRadius ?? BorderRadius.Zero;
    }

    /// A uniform border with all sides the same color and width.
    ///
    /// The sides default to black solid borders, one logical pixel wide.
    public static TableBorder All(
        Color? color = null,
        double width = 1.0,
        BorderStyle style = BorderStyle.Solid,
        BorderRadius? borderRadius = null)
    {
        var side = new BorderSide(color ?? new Color(0xFF000000), width, style);
        return new TableBorder(
            top: side,
            right: side,
            bottom: side,
            left: side,
            horizontalInside: side,
            verticalInside: side,
            borderRadius: borderRadius);
    }

    /// Creates a border for a table where all the interior sides use the same
    /// styling and all the exterior sides use the same styling.
    public static TableBorder Symmetric(
        BorderSide? inside = null,
        BorderSide? outside = null,
        BorderRadius? borderRadius = null)
    {
        return new TableBorder(
            top: outside,
            right: outside,
            bottom: outside,
            left: outside,
            horizontalInside: inside,
            verticalInside: inside,
            borderRadius: borderRadius);
    }

    public BorderSide Top { get; }

    public BorderSide Right { get; }

    public BorderSide Bottom { get; }

    public BorderSide Left { get; }

    public BorderSide HorizontalInside { get; }

    public BorderSide VerticalInside { get; }

    public BorderRadius BorderRadius { get; }

    /// The widths of the sides of this border represented as insets.
    public Thickness Dimensions => new(Left.Width, Top.Width, Right.Width, Bottom.Width);

    /// Whether all the sides of the border (outside and inside) are identical.
    public bool IsUniform =>
        AllSidesMatch(side => side.Color)
        && AllSidesMatch(side => side.Width)
        && AllSidesMatch(side => side.Style);

    private bool OuterBorderIsUniform =>
        OuterSidesMatch(side => side.Color)
        && OuterSidesMatch(side => side.Width)
        && OuterSidesMatch(side => side.Style);

    private bool AllSidesMatch<T>(Func<BorderSide, T> selector) =>
        OuterSidesMatch(selector)
        && EqualityComparer<T>.Default.Equals(selector(HorizontalInside), selector(Top))
        && EqualityComparer<T>.Default.Equals(selector(VerticalInside), selector(Top));

    private bool OuterSidesMatch<T>(Func<BorderSide, T> selector)
    {
        T topValue = selector(Top);
        return EqualityComparer<T>.Default.Equals(selector(Right), topValue)
            && EqualityComparer<T>.Default.Equals(selector(Bottom), topValue)
            && EqualityComparer<T>.Default.Equals(selector(Left), topValue);
    }

    /// Creates a copy of this border but with the widths scaled by the factor `t`.
    public TableBorder Scale(double t)
    {
        return new TableBorder(
            top: Top.Scale(t),
            right: Right.Scale(t),
            bottom: Bottom.Scale(t),
            left: Left.Scale(t),
            horizontalInside: HorizontalInside.Scale(t),
            verticalInside: VerticalInside.Scale(t));
    }

    /// Linearly interpolate between two table borders.
    public static TableBorder? Lerp(TableBorder? a, TableBorder? b, double t)
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

        return new TableBorder(
            top: BorderSide.Lerp(a.Top, b.Top, t),
            right: BorderSide.Lerp(a.Right, b.Right, t),
            bottom: BorderSide.Lerp(a.Bottom, b.Bottom, t),
            left: BorderSide.Lerp(a.Left, b.Left, t),
            horizontalInside: BorderSide.Lerp(a.HorizontalInside, b.HorizontalInside, t),
            verticalInside: BorderSide.Lerp(a.VerticalInside, b.VerticalInside, t));
    }

    /// Paints the border around the given [rect], with the given rows and columns.
    ///
    /// The <paramref name="rows"/> argument specifies the vertical positions between the
    /// rows, relative to the given rectangle; <paramref name="columns"/> specifies the
    /// horizontal positions between the columns. The vertical interior borders are drawn
    /// before the horizontal ones, and the outer borders are painted last.
    public void Paint(
        PaintingContext context,
        Rect rect,
        IReadOnlyList<double> rows,
        IReadOnlyList<double> columns) => Paint(context.Canvas, rect, rows, columns);

    /// <summary>Paints directly on a canvas, as in Dart's <c>TableBorder.paint</c>.</summary>
    public void Paint(
        Canvas canvas,
        Rect rect,
        IReadOnlyList<double> rows,
        IReadOnlyList<double> columns)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(columns);
        DebugAssertions.Assert(rows.Count == 0 || (rows[0] >= 0.0 && rows[^1] <= rect.Height));
        DebugAssertions.Assert(columns.Count == 0 || (columns[0] >= 0.0 && columns[^1] <= rect.Width));

        if (columns.Count > 0 || rows.Count > 0)
        {
            var paint = new Paint();
            var path = new Path();
            if (columns.Count > 0 && VerticalInside.Style == BorderStyle.Solid)
            {
                paint.Color = VerticalInside.Color;
                paint.StrokeWidth = VerticalInside.Width;
                paint.Style = PaintingStyle.Stroke;
                path.Reset();
                foreach (double x in columns)
                {
                    path.MoveTo(rect.Left + x, rect.Top);
                    path.LineTo(rect.Left + x, rect.Bottom);
                }

                canvas.DrawPath(path, paint);
            }

            if (rows.Count > 0 && HorizontalInside.Style == BorderStyle.Solid)
            {
                paint.Color = HorizontalInside.Color;
                paint.StrokeWidth = HorizontalInside.Width;
                paint.Style = PaintingStyle.Stroke;
                path.Reset();
                foreach (double y in rows)
                {
                    path.MoveTo(rect.Left, rect.Top + y);
                    path.LineTo(rect.Right, rect.Top + y);
                }

                canvas.DrawPath(path, paint);
            }
        }

        PaintTableBorder(canvas, rect);
    }

    private void PaintTableBorder(Canvas canvas, Rect rect)
    {
        if (OuterBorderIsUniform && BorderRadius != BorderRadius.Zero)
        {
            RRect outer = BorderRadius.ToRRect(rect);
            RRect inner = outer.Deflate(Top.Width);
            canvas.DrawDRRect(outer, inner, new Paint { Color = Top.Color });
            return;
        }

        var visibleColors = DistinctVisibleOuterColors();
        if (visibleColors.Count == 1 && BorderRadius != BorderRadius.Zero)
        {
            PaintNonUniformBorderWithRadius(
                canvas,
                rect,
                BorderRadius,
                visibleColors.Single(),
                Top.Style == BorderStyle.None ? BorderSide.None : Top,
                Right.Style == BorderStyle.None ? BorderSide.None : Right,
                Bottom.Style == BorderStyle.None ? BorderSide.None : Bottom,
                Left.Style == BorderStyle.None ? BorderSide.None : Left);
            return;
        }

        BorderPainting.PaintBorder(canvas, rect, Top, Right, Bottom, Left);
    }

    private HashSet<Color> DistinctVisibleOuterColors()
    {
        var colors = new HashSet<Color>();
        if (Top.Style != BorderStyle.None) colors.Add(Top.Color);
        if (Right.Style != BorderStyle.None) colors.Add(Right.Color);
        if (Bottom.Style != BorderStyle.None) colors.Add(Bottom.Color);
        if (Left.Style != BorderStyle.None) colors.Add(Left.Color);
        return colors;
    }

    private static void PaintNonUniformBorderWithRadius(
        Canvas canvas,
        Rect rect,
        BorderRadius borderRadius,
        Color color,
        BorderSide top,
        BorderSide right,
        BorderSide bottom,
        BorderSide left)
    {
        RRect borderRect = borderRadius.ToRRect(rect);
        RRect inner = borderRect.DeflateEdges(
            new Thickness(left.StrokeInset, top.StrokeInset, right.StrokeInset, bottom.StrokeInset));
        RRect outer = borderRect.InflateEdges(
            new Thickness(left.StrokeOutset, top.StrokeOutset, right.StrokeOutset, bottom.StrokeOutset));
        canvas.DrawDRRect(outer, inner, new Paint { Color = color });
    }

    public override string ToString() =>
        $"TableBorder({Top}, {Right}, {Bottom}, {Left}, {HorizontalInside}, {VerticalInside}, {BorderRadius})";
}
