using Avalonia;
using Avalonia.Media;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/linear_border.dart

/// Defines the relative size and alignment of one [LinearBorder] edge.
public sealed record LinearBorderEdge
{
    public LinearBorderEdge(double size = 1.0, double alignment = 0.0)
    {
        if (size < 0.0 || size > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "The size argument must be between 0.0 and 1.0.");
        }

        Size = size;
        Alignment = alignment;
    }

    /// The relative size of the edge, as a fraction of the available space.
    public double Size { get; init; }

    /// Where the edge is aligned within the available space: -1.0 is at the start, 1.0 at the end.
    public double Alignment { get; init; }

    public static LinearBorderEdge? Lerp(LinearBorderEdge? a, LinearBorderEdge? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        a ??= new LinearBorderEdge(alignment: b!.Alignment, size: 0.0);
        b ??= new LinearBorderEdge(alignment: a.Alignment, size: 0.0);
        return new LinearBorderEdge(
            size: LerpDouble(a.Size, b.Size, t),
            alignment: LerpDouble(a.Alignment, b.Alignment, t));
    }

    public override string ToString()
    {
        var builder = new System.Text.StringBuilder("LinearBorderEdge(");
        if (Size != 1.0)
        {
            builder.Append(
                System.Globalization.CultureInfo.InvariantCulture,
                $"size: {Size}");
        }

        if (Alignment != 0.0)
        {
            builder.Append(Size != 1.0 ? ", " : string.Empty)
                .Append(System.Globalization.CultureInfo.InvariantCulture, $"alignment: {Alignment}");
        }

        builder.Append(')');
        return builder.ToString();
    }

    private static double LerpDouble(double a, double b, double t)
    {
        return (a * (1.0 - t)) + (b * t);
    }
}

/// An [OutlinedBorder] like [BoxBorder] that allows one or more of its four edges to be drawn.
public sealed record LinearBorder : OutlinedBorder
{
    public LinearBorder(
        BorderSide? side = null,
        LinearBorderEdge? start = null,
        LinearBorderEdge? end = null,
        LinearBorderEdge? top = null,
        LinearBorderEdge? bottom = null)
        : base(side)
    {
        Start = start;
        End = end;
        Top = top;
        Bottom = bottom;
    }

    public LinearBorderEdge? Start { get; init; }

    public LinearBorderEdge? End { get; init; }

    public LinearBorderEdge? Top { get; init; }

    public LinearBorderEdge? Bottom { get; init; }

    /// A [LinearBorder] with no edges.
    public static LinearBorder None => new();

    public static LinearBorder StartEdge(BorderSide? side = null, double alignment = 0.0, double size = 1.0)
    {
        return new LinearBorder(side, start: new LinearBorderEdge(size, alignment));
    }

    public static LinearBorder EndEdge(BorderSide? side = null, double alignment = 0.0, double size = 1.0)
    {
        return new LinearBorder(side, end: new LinearBorderEdge(size, alignment));
    }

    public static LinearBorder TopEdge(BorderSide? side = null, double alignment = 0.0, double size = 1.0)
    {
        return new LinearBorder(side, top: new LinearBorderEdge(size, alignment));
    }

    public static LinearBorder BottomEdge(BorderSide? side = null, double alignment = 0.0, double size = 1.0)
    {
        return new LinearBorder(side, bottom: new LinearBorderEdge(size, alignment));
    }

    public override ShapeBorder Scale(double t)
    {
        return new LinearBorder(Side.Scale(t));
    }

    public override EdgeInsetsGeometry Dimensions => EdgeInsetsGeometry.DirectionalOnly(
        Start is null ? 0.0 : Side.Width,
        Top is null ? 0.0 : Side.Width,
        End is null ? 0.0 : Side.Width,
        Bottom is null ? 0.0 : Side.Width);

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        if (a is LinearBorder linear)
        {
            return new LinearBorder(
                BorderSide.Lerp(linear.Side, Side, t),
                LinearBorderEdge.Lerp(linear.Start, Start, t),
                LinearBorderEdge.Lerp(linear.End, End, t),
                LinearBorderEdge.Lerp(linear.Top, Top, t),
                LinearBorderEdge.Lerp(linear.Bottom, Bottom, t));
        }

        return base.LerpFrom(a, t);
    }

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        if (b is LinearBorder linear)
        {
            return new LinearBorder(
                BorderSide.Lerp(Side, linear.Side, t),
                LinearBorderEdge.Lerp(Start, linear.Start, t),
                LinearBorderEdge.Lerp(End, linear.End, t),
                LinearBorderEdge.Lerp(Top, linear.Top, t),
                LinearBorderEdge.Lerp(Bottom, linear.Bottom, t));
        }

        return base.LerpTo(b, t);
    }

    public override OutlinedBorder CopyWith(BorderSide? side = null)
    {
        return CopyWith(side, null, null, null, null);
    }

    public LinearBorder CopyWith(
        BorderSide? side,
        LinearBorderEdge? start,
        LinearBorderEdge? end,
        LinearBorderEdge? top,
        LinearBorderEdge? bottom)
    {
        return new LinearBorder(
            side ?? Side,
            start ?? Start,
            end ?? End,
            top ?? Top,
            bottom ?? Bottom);
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

    public override void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null)
    {
        Thickness insets = Dimensions.Resolve(textDirection ?? TextDirection.Ltr);
        bool rtl = textDirection == TextDirection.Rtl;

        if (Start is { } startEdge && startEdge.Size != 0.0 && Side.Style != BorderStyle.None)
        {
            var insetRect = new Rect(
                rect.Left,
                rect.Top + insets.Top,
                rect.Width,
                rect.Height - insets.Top - insets.Bottom);
            double x = rtl ? rect.Right - insets.Right : rect.Left;
            double width = rtl ? insets.Right : insets.Left;
            double height = insetRect.Height * startEdge.Size;
            double y = (insetRect.Height - height) * ((startEdge.Alignment + 1.0) / 2.0);
            DrawEdge(context, new Rect(x, y, width, height), Side.Color);
        }

        if (End is { } endEdge && endEdge.Size != 0.0 && Side.Style != BorderStyle.None)
        {
            var insetRect = new Rect(
                rect.Left,
                rect.Top + insets.Top,
                rect.Width,
                rect.Height - insets.Top - insets.Bottom);
            double x = rtl ? rect.Left : rect.Right - insets.Right;
            double width = rtl ? insets.Left : insets.Right;
            double height = insetRect.Height * endEdge.Size;
            double y = (insetRect.Height - height) * ((endEdge.Alignment + 1.0) / 2.0);
            DrawEdge(context, new Rect(x, y, width, height), Side.Color);
        }

        if (Top is { } topEdge && topEdge.Size != 0.0 && Side.Style != BorderStyle.None)
        {
            double width = rect.Width * topEdge.Size;
            double startX = (rect.Width - width) * ((topEdge.Alignment + 1.0) / 2.0);
            double x = rtl ? rect.Width - startX - width : startX;
            DrawEdge(context, new Rect(x, rect.Top, width, insets.Top), Side.Color);
        }

        if (Bottom is { } bottomEdge && bottomEdge.Size != 0.0 && Side.Style != BorderStyle.None)
        {
            double width = rect.Width * bottomEdge.Size;
            double startX = (rect.Width - width) * ((bottomEdge.Alignment + 1.0) / 2.0);
            double x = rtl ? rect.Width - startX - width : startX;
            DrawEdge(context, new Rect(x, rect.Bottom - insets.Bottom, width, Side.Width), Side.Color);
        }
    }

    public override string ToString()
    {
        if (Equals(None))
        {
            return "LinearBorder.none";
        }

        var builder = new System.Text.StringBuilder($"LinearBorder(side: {Side}");
        if (Start is not null)
        {
            builder.Append($", start: {Start}");
        }

        if (End is not null)
        {
            builder.Append($", end: {End}");
        }

        if (Top is not null)
        {
            builder.Append($", top: {Top}");
        }

        if (Bottom is not null)
        {
            builder.Append($", bottom: {Bottom}");
        }

        builder.Append(')');
        return builder.ToString();
    }

    private static void DrawEdge(PaintingContext context, Rect rect, Color color)
    {
        var brush = new SolidColorBrush(color);
        if (rect.Width == 0.0)
        {
            context.DrawLine(new Pen(brush, 0.0), rect.TopLeft, new Point(rect.Left, rect.Bottom));
            return;
        }

        if (rect.Height == 0.0)
        {
            context.DrawLine(new Pen(brush, 0.0), rect.TopLeft, new Point(rect.Right, rect.Top));
            return;
        }

        context.DrawRectangle(brush, null, rect);
    }
}
