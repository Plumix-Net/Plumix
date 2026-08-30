using Avalonia;
using Avalonia.Media;

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
        var side = new BorderSide(color ?? Color.FromUInt32(0xFF000000), width, style);
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
        Right == Top && Bottom == Top && Left == Top && HorizontalInside == Top && VerticalInside == Top;

    private bool OuterBorderIsUniform => Right == Top && Bottom == Top && Left == Top;

    /// Creates a copy of this border but with the widths scaled by the factor `t`.
    public TableBorder Scale(double t)
    {
        return new TableBorder(
            top: ScaleSide(Top, t),
            right: ScaleSide(Right, t),
            bottom: ScaleSide(Bottom, t),
            left: ScaleSide(Left, t),
            horizontalInside: ScaleSide(HorizontalInside, t),
            verticalInside: ScaleSide(VerticalInside, t));
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
            top: LerpSide(a.Top, b.Top, t),
            right: LerpSide(a.Right, b.Right, t),
            bottom: LerpSide(a.Bottom, b.Bottom, t),
            left: LerpSide(a.Left, b.Left, t),
            horizontalInside: LerpSide(a.HorizontalInside, b.HorizontalInside, t),
            verticalInside: LerpSide(a.VerticalInside, b.VerticalInside, t));
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
        IReadOnlyList<double> columns)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(columns);

        if (columns.Count > 0 && VerticalInside.Style == BorderStyle.Solid)
        {
            var pen = new Pen(new SolidColorBrush(VerticalInside.Color), VerticalInside.Width);
            foreach (double x in columns)
            {
                context.Canvas.DrawLine(pen, new Point(rect.Left + x, rect.Top), new Point(rect.Left + x, rect.Bottom));
            }
        }

        if (rows.Count > 0 && HorizontalInside.Style == BorderStyle.Solid)
        {
            var pen = new Pen(new SolidColorBrush(HorizontalInside.Color), HorizontalInside.Width);
            foreach (double y in rows)
            {
                context.Canvas.DrawLine(pen, new Point(rect.Left, rect.Top + y), new Point(rect.Right, rect.Top + y));
            }
        }

        PaintTableBorder(context, rect);
    }

    private void PaintTableBorder(PaintingContext context, Rect rect)
    {
        if (OuterBorderIsUniform && BorderRadius != BorderRadius.Zero)
        {
            PaintRing(context, rect, BorderRadius, Top.Color, Top.Width);
            return;
        }

        var visibleColors = DistinctVisibleOuterColors();
        if (visibleColors.Count == 1 && BorderRadius != BorderRadius.Zero)
        {
            // Only the sides that are actually visible contribute an inset; the single
            // shared color is used for the whole ring.
            PaintNonUniformBorderWithRadius(context, rect, visibleColors.Single());
            return;
        }

        PaintBorderSides(context, rect);
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

    private void PaintNonUniformBorderWithRadius(PaintingContext context, Rect rect, Color color)
    {
        // Plumix's BorderSide has no strokeAlign, so every side is inset-aligned:
        // strokeInset == width and strokeOutset == 0, matching Flutter's default.
        double left = Left.Style == BorderStyle.None ? 0.0 : Left.Width;
        double top = Top.Style == BorderStyle.None ? 0.0 : Top.Width;
        double right = Right.Style == BorderStyle.None ? 0.0 : Right.Width;
        double bottom = Bottom.Style == BorderStyle.None ? 0.0 : Bottom.Width;
        var geometry = RingGeometry(rect, BorderRadius, left, top, right, bottom);
        context.Canvas.DrawGeometry(new SolidColorBrush(color), null, geometry);
    }

    private static void PaintRing(PaintingContext context, Rect rect, BorderRadius radius, Color color, double width)
    {
        var geometry = RingGeometry(rect, radius, width, width, width, width);
        context.Canvas.DrawGeometry(new SolidColorBrush(color), null, geometry);
    }

    private static Geometry RingGeometry(
        Rect rect,
        BorderRadius radius,
        double left,
        double top,
        double right,
        double bottom)
    {
        var outer = RoundedGeometry(rect, radius);
        var innerRect = new Rect(
            rect.Left + left,
            rect.Top + top,
            Math.Max(0.0, rect.Width - left - right),
            Math.Max(0.0, rect.Height - top - bottom));
        var inner = RoundedGeometry(innerRect, DeflateRadius(radius, Math.Max(Math.Max(left, top), Math.Max(right, bottom))));
        return new CombinedGeometry(GeometryCombineMode.Exclude, outer, inner);
    }

    private static Geometry RoundedGeometry(Rect rect, BorderRadius radius)
    {
        return new RectangleGeometry(rect, radius.TopLeftRadius.X, radius.TopLeftRadius.Y);
    }

    private static BorderRadius DeflateRadius(BorderRadius radius, double delta)
    {
        return new BorderRadius(
            Deflate(radius.TopLeftRadius, delta),
            Deflate(radius.TopRightRadius, delta),
            Deflate(radius.BottomRightRadius, delta),
            Deflate(radius.BottomLeftRadius, delta));
    }

    private static Radius Deflate(Radius radius, double delta) =>
        Radius.Elliptical(Math.Max(0.0, radius.X - delta), Math.Max(0.0, radius.Y - delta));

    private void PaintBorderSides(PaintingContext context, Rect rect)
    {
        PaintSide(context, Top, [
            new Point(rect.Left, rect.Top),
            new Point(rect.Right, rect.Top),
            new Point(rect.Right - Right.Width, rect.Top + Top.Width),
            new Point(rect.Left + Left.Width, rect.Top + Top.Width),
        ]);
        PaintSide(context, Right, [
            new Point(rect.Right, rect.Top),
            new Point(rect.Right, rect.Bottom),
            new Point(rect.Right - Right.Width, rect.Bottom - Bottom.Width),
            new Point(rect.Right - Right.Width, rect.Top + Top.Width),
        ]);
        PaintSide(context, Bottom, [
            new Point(rect.Right, rect.Bottom),
            new Point(rect.Left, rect.Bottom),
            new Point(rect.Left + Left.Width, rect.Bottom - Bottom.Width),
            new Point(rect.Right - Right.Width, rect.Bottom - Bottom.Width),
        ]);
        PaintSide(context, Left, [
            new Point(rect.Left, rect.Bottom),
            new Point(rect.Left, rect.Top),
            new Point(rect.Left + Left.Width, rect.Top + Top.Width),
            new Point(rect.Left + Left.Width, rect.Bottom - Bottom.Width),
        ]);
    }

    private static void PaintSide(PaintingContext context, BorderSide side, IReadOnlyList<Point> quad)
    {
        if (side.Style != BorderStyle.Solid)
        {
            return;
        }

        var brush = new SolidColorBrush(side.Color);
        if (side.Width == 0.0)
        {
            context.Canvas.DrawLine(new Pen(brush, 0.0), quad[0], quad[1]);
            return;
        }

        context.Canvas.DrawPolygon(brush, null, quad);
    }

    private static BorderSide ScaleSide(BorderSide side, double t)
    {
        double width = Math.Max(0.0, side.Width * t);
        return new BorderSide(side.Color, width, width == 0.0 ? BorderStyle.None : side.Style);
    }

    private static BorderSide LerpSide(BorderSide a, BorderSide b, double t)
    {
        if (t == 0.0) return a;
        if (t == 1.0) return b;
        double width = a.Width + ((b.Width - a.Width) * t);
        if (width < 0.0)
        {
            return BorderSide.None;
        }

        if (a.Style == b.Style)
        {
            return new BorderSide(LerpColor(a.Color, b.Color, t), width, a.Style);
        }

        Color colorA = a.Style == BorderStyle.None ? WithAlpha(a.Color, 0) : a.Color;
        Color colorB = b.Style == BorderStyle.None ? WithAlpha(b.Color, 0) : b.Color;
        return new BorderSide(LerpColor(colorA, colorB, t), width, BorderStyle.Solid);
    }

    private static Color WithAlpha(Color color, byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);

    private static Color LerpColor(Color a, Color b, double t)
    {
        static byte Mix(byte from, byte to, double amount) =>
            (byte)Math.Clamp(Math.Round(from + ((to - from) * amount)), 0, 255);
        return Color.FromArgb(Mix(a.A, b.A, t), Mix(a.R, b.R, t), Mix(a.G, b.G, t), Mix(a.B, b.B, t));
    }
}
