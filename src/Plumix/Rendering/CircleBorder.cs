using Avalonia;
using Avalonia.Media;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/circle_border.dart

/// A border that fits a circle within the available space.
public record CircleBorder : OutlinedBorder
{
    public CircleBorder(BorderSide? side = null, double eccentricity = 0.0)
        : base(side)
    {
        if (eccentricity < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(eccentricity),
                $"The eccentricity argument {eccentricity} is not greater than or equal to zero.");
        }

        if (eccentricity > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(eccentricity),
                $"The eccentricity argument {eccentricity} is not less than or equal to one.");
        }

        Eccentricity = eccentricity;
    }

    /// How much the circle deviates from a perfect circle: 0 is a circle, 1 fills the available space.
    public double Eccentricity { get; init; }

    public override ShapeBorder Scale(double t)
    {
        return new CircleBorder(Side.Scale(t), Eccentricity);
    }

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        if (a is CircleBorder circle)
        {
            return new CircleBorder(
                BorderSide.Lerp(circle.Side, Side, t),
                Math.Clamp(LerpDouble(circle.Eccentricity, Eccentricity, t), 0.0, 1.0));
        }

        return base.LerpFrom(a, t);
    }

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        if (b is CircleBorder circle)
        {
            return new CircleBorder(
                BorderSide.Lerp(Side, circle.Side, t),
                Math.Clamp(LerpDouble(Eccentricity, circle.Eccentricity, t), 0.0, 1.0));
        }

        return base.LerpTo(b, t);
    }

    public override OutlinedBorder CopyWith(BorderSide? side = null)
    {
        return CopyWith(side, null);
    }

    public virtual CircleBorder CopyWith(BorderSide? side, double? eccentricity)
    {
        return new CircleBorder(side ?? Side, eccentricity ?? Eccentricity);
    }

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
    {
        var path = new Path();
        path.AddOval(AdjustRect(rect).Deflate(Side.StrokeInset));
        return path;
    }

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null)
    {
        var path = new Path();
        path.AddOval(AdjustRect(rect));
        return path;
    }

    public override bool PreferPaintInterior => true;

    public override void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null)
    {
        if (Eccentricity == 0.0)
        {
            context.DrawCircle(brush, null, rect.Center, BoxBorder.ShortestSide(rect) / 2.0);
            return;
        }

        context.DrawOval(AdjustRect(rect), brush, null);
    }

    public override void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null)
    {
        if (Side.Style == BorderStyle.None)
        {
            return;
        }

        if (Eccentricity == 0.0)
        {
            context.DrawCircle(
                Brushes.Transparent,
                Side.ToPen(),
                rect.Center,
                (BoxBorder.ShortestSide(rect) + Side.StrokeOffset) / 2.0);
            return;
        }

        context.DrawOval(AdjustRect(rect).Inflate(Side.StrokeOffset / 2.0), null, Side.ToPen());
    }

    public override string ToString()
    {
        return Eccentricity != 0.0
            ? $"CircleBorder({Side}, eccentricity: {Eccentricity})"
            : $"CircleBorder({Side})";
    }

    protected Rect AdjustRect(Rect rect)
    {
        if (Eccentricity == 0.0 || rect.Width == rect.Height)
        {
            double radius = BoxBorder.ShortestSide(rect) / 2.0;
            return new Rect(
                rect.Center.X - radius,
                rect.Center.Y - radius,
                radius * 2.0,
                radius * 2.0);
        }

        if (rect.Width < rect.Height)
        {
            double delta = (1.0 - Eccentricity) * (rect.Height - rect.Width) / 2.0;
            return new Rect(rect.Left, rect.Top + delta, rect.Width, rect.Height - (delta * 2.0));
        }
        else
        {
            double delta = (1.0 - Eccentricity) * (rect.Width - rect.Height) / 2.0;
            return new Rect(rect.Left + delta, rect.Top, rect.Width - (delta * 2.0), rect.Height);
        }
    }

    private protected static double LerpDouble(double a, double b, double t)
    {
        return (a * (1.0 - t)) + (b * t);
    }
}
