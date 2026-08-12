using Avalonia;
using Avalonia.Media;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/stadium_border.dart

/// A border that fits a stadium-shaped border (a box with semicircles on the ends).
public sealed record StadiumBorder : OutlinedBorder
{
    public StadiumBorder(BorderSide? side = null)
        : base(side)
    {
    }

    public override ShapeBorder Scale(double t)
    {
        return new StadiumBorder(Side.Scale(t));
    }

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        switch (a)
        {
            case StadiumBorder stadium:
                return new StadiumBorder(BorderSide.Lerp(stadium.Side, Side, t));
            case CircleBorder circle:
                return new StadiumToCircleBorder(
                    BorderSide.Lerp(circle.Side, Side, t),
                    1.0 - t,
                    circle.Eccentricity);
            case RoundedRectangleBorder rounded:
                return new StadiumToRoundedRectangleBorder(
                    BorderSide.Lerp(rounded.Side, Side, t),
                    rounded.BorderRadius,
                    1.0 - t);
            default:
                return base.LerpFrom(a, t);
        }
    }

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        switch (b)
        {
            case StadiumBorder stadium:
                return new StadiumBorder(BorderSide.Lerp(Side, stadium.Side, t));
            case CircleBorder circle:
                return new StadiumToCircleBorder(
                    BorderSide.Lerp(Side, circle.Side, t),
                    t,
                    circle.Eccentricity);
            case RoundedRectangleBorder rounded:
                return new StadiumToRoundedRectangleBorder(
                    BorderSide.Lerp(Side, rounded.Side, t),
                    rounded.BorderRadius,
                    t);
            default:
                return base.LerpTo(b, t);
        }
    }

    public override OutlinedBorder CopyWith(BorderSide? side = null)
    {
        return new StadiumBorder(side ?? Side);
    }

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
    {
        RRect borderRect = RRect.FromRectAndRadius(rect, BoxBorder.ShortestSide(rect) / 2.0);
        var path = new Path();
        path.AddRRect(borderRect.Deflate(Side.StrokeInset));
        return path;
    }

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null)
    {
        var path = new Path();
        path.AddRRect(RRect.FromRectAndRadius(rect, BoxBorder.ShortestSide(rect) / 2.0));
        return path;
    }

    public override bool PreferPaintInterior => true;

    public override void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null)
    {
        context.DrawRRect(RRect.FromRectAndRadius(rect, BoxBorder.ShortestSide(rect) / 2.0), brush, null);
    }

    public override void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null)
    {
        if (Side.Style == BorderStyle.None)
        {
            return;
        }

        RRect borderRect = RRect.FromRectAndRadius(rect, BoxBorder.ShortestSide(rect) / 2.0);
        context.DrawRRect(borderRect.Inflate(Side.StrokeOffset / 2.0), null, Side.ToPen());
    }

    public override string ToString()
    {
        return $"StadiumBorder({Side})";
    }
}

/// Animates a [StadiumBorder] towards a [CircleBorder].
internal sealed record StadiumToCircleBorder : OutlinedBorder
{
    public StadiumToCircleBorder(BorderSide? side = null, double circularity = 0.0, double eccentricity = 0.0)
        : base(side)
    {
        Circularity = circularity;
        Eccentricity = eccentricity;
    }

    public double Circularity { get; init; }

    public double Eccentricity { get; init; }

    public override ShapeBorder Scale(double t)
    {
        return new StadiumToCircleBorder(Side.Scale(t), t, Eccentricity);
    }

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        switch (a)
        {
            case StadiumBorder stadium:
                return new StadiumToCircleBorder(
                    BorderSide.Lerp(stadium.Side, Side, t),
                    Circularity * t,
                    Eccentricity);
            case CircleBorder circle:
                return new StadiumToCircleBorder(
                    BorderSide.Lerp(circle.Side, Side, t),
                    Circularity + ((1.0 - Circularity) * (1.0 - t)),
                    circle.Eccentricity);
            case StadiumToCircleBorder other:
                return new StadiumToCircleBorder(
                    BorderSide.Lerp(other.Side, Side, t),
                    LerpDouble(other.Circularity, Circularity, t),
                    LerpDouble(other.Eccentricity, Eccentricity, t));
            default:
                return base.LerpFrom(a, t);
        }
    }

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        switch (b)
        {
            case StadiumBorder stadium:
                return new StadiumToCircleBorder(
                    BorderSide.Lerp(Side, stadium.Side, t),
                    Circularity * (1.0 - t),
                    Eccentricity);
            case CircleBorder circle:
                return new StadiumToCircleBorder(
                    BorderSide.Lerp(Side, circle.Side, t),
                    Circularity + ((1.0 - Circularity) * t),
                    circle.Eccentricity);
            case StadiumToCircleBorder other:
                return new StadiumToCircleBorder(
                    BorderSide.Lerp(Side, other.Side, t),
                    LerpDouble(Circularity, other.Circularity, t),
                    LerpDouble(Eccentricity, other.Eccentricity, t));
            default:
                return base.LerpTo(b, t);
        }
    }

    public override OutlinedBorder CopyWith(BorderSide? side = null)
    {
        return new StadiumToCircleBorder(side ?? Side, Circularity, Eccentricity);
    }

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
    {
        var path = new Path();
        path.AddRRect(AdjustBorderRadius(rect).ToRRect(AdjustRect(rect)).Deflate(Side.StrokeInset));
        return path;
    }

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null)
    {
        var path = new Path();
        path.AddRRect(AdjustBorderRadius(rect).ToRRect(AdjustRect(rect)));
        return path;
    }

    public override bool PreferPaintInterior => true;

    public override void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null)
    {
        context.DrawRRect(AdjustBorderRadius(rect).ToRRect(AdjustRect(rect)), brush, null);
    }

    public override void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null)
    {
        if (Side.Style == BorderStyle.None)
        {
            return;
        }

        RRect borderRect = AdjustBorderRadius(rect).ToRRect(AdjustRect(rect));
        context.DrawRRect(borderRect.Inflate(Side.StrokeOffset / 2.0), null, Side.ToPen());
    }

    public override string ToString()
    {
        string circularity = (Circularity * 100.0).ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        if (Eccentricity != 0.0)
        {
            string oval = (Eccentricity * 100.0).ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            return $"StadiumBorder({Side}, {circularity}% of the way to being a CircleBorder that is {oval}% oval)";
        }

        return $"StadiumBorder({Side}, {circularity}% of the way to being a CircleBorder)";
    }

    public bool Equals(StadiumToCircleBorder? other)
    {
        return other is not null && other.Side == Side && other.Circularity == Circularity;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Side, Circularity);
    }

    private Rect AdjustRect(Rect rect)
    {
        if (Circularity == 0.0 || rect.Width == rect.Height)
        {
            return rect;
        }

        if (rect.Width < rect.Height)
        {
            double partialDelta = (rect.Height - rect.Width) / 2.0;
            double delta = Circularity * partialDelta * (1.0 - Eccentricity);
            return new Rect(rect.Left, rect.Top + delta, rect.Width, rect.Height - (delta * 2.0));
        }
        else
        {
            double partialDelta = (rect.Width - rect.Height) / 2.0;
            double delta = Circularity * partialDelta * (1.0 - Eccentricity);
            return new Rect(rect.Left + delta, rect.Top, rect.Width - (delta * 2.0), rect.Height);
        }
    }

    private BorderRadius AdjustBorderRadius(Rect rect)
    {
        BorderRadius circleRadius = BorderRadius.Circular(BoxBorder.ShortestSide(rect) / 2.0);
        if (Eccentricity != 0.0)
        {
            if (rect.Width < rect.Height)
            {
                return BorderRadius.Lerp(
                    circleRadius,
                    UniformRadius(Radius.Elliptical(
                        rect.Width / 2.0,
                        (0.5 + (Eccentricity / 2.0)) * rect.Height / 2.0)),
                    Circularity)!.Value;
            }

            return BorderRadius.Lerp(
                circleRadius,
                UniformRadius(Radius.Elliptical(
                    (0.5 + (Eccentricity / 2.0)) * rect.Width / 2.0,
                    rect.Height / 2.0)),
                Circularity)!.Value;
        }

        return circleRadius;
    }

    private static BorderRadius UniformRadius(Radius radius)
    {
        return new BorderRadius(radius, radius, radius, radius);
    }

    private static double LerpDouble(double a, double b, double t)
    {
        return (a * (1.0 - t)) + (b * t);
    }
}

/// Animates a [StadiumBorder] towards a [RoundedRectangleBorder].
internal sealed record StadiumToRoundedRectangleBorder : OutlinedBorder
{
    public StadiumToRoundedRectangleBorder(
        BorderSide? side = null,
        BorderRadiusGeometry? borderRadius = null,
        double rectilinearity = 0.0)
        : base(side)
    {
        BorderRadius = borderRadius ?? default;
        Rectilinearity = rectilinearity;
    }

    public BorderRadiusGeometry BorderRadius { get; init; }

    public double Rectilinearity { get; init; }

    public override ShapeBorder Scale(double t)
    {
        return new StadiumToRoundedRectangleBorder(Side.Scale(t), BorderRadius * t, t);
    }

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        switch (a)
        {
            case StadiumBorder stadium:
                return new StadiumToRoundedRectangleBorder(
                    BorderSide.Lerp(stadium.Side, Side, t),
                    BorderRadius,
                    Rectilinearity * t);
            case RoundedRectangleBorder rounded:
                return new StadiumToRoundedRectangleBorder(
                    BorderSide.Lerp(rounded.Side, Side, t),
                    BorderRadius,
                    Rectilinearity + ((1.0 - Rectilinearity) * (1.0 - t)));
            case StadiumToRoundedRectangleBorder other:
                return new StadiumToRoundedRectangleBorder(
                    BorderSide.Lerp(other.Side, Side, t),
                    BorderRadiusGeometry.Lerp(other.BorderRadius, BorderRadius, t)!.Value,
                    LerpDouble(other.Rectilinearity, Rectilinearity, t));
            default:
                return base.LerpFrom(a, t);
        }
    }

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        switch (b)
        {
            case StadiumBorder stadium:
                return new StadiumToRoundedRectangleBorder(
                    BorderSide.Lerp(Side, stadium.Side, t),
                    BorderRadius,
                    Rectilinearity * (1.0 - t));
            case RoundedRectangleBorder rounded:
                return new StadiumToRoundedRectangleBorder(
                    BorderSide.Lerp(Side, rounded.Side, t),
                    BorderRadius,
                    Rectilinearity + ((1.0 - Rectilinearity) * t));
            case StadiumToRoundedRectangleBorder other:
                return new StadiumToRoundedRectangleBorder(
                    BorderSide.Lerp(Side, other.Side, t),
                    BorderRadiusGeometry.Lerp(BorderRadius, other.BorderRadius, t)!.Value,
                    LerpDouble(Rectilinearity, other.Rectilinearity, t));
            default:
                return base.LerpTo(b, t);
        }
    }

    public override OutlinedBorder CopyWith(BorderSide? side = null)
    {
        return new StadiumToRoundedRectangleBorder(side ?? Side, BorderRadius, Rectilinearity);
    }

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
    {
        RRect borderRect = AdjustBorderRadius(rect).Resolve(textDirection ?? TextDirection.Ltr).ToRRect(rect);
        var path = new Path();
        path.AddRRect(borderRect.Deflate(LerpDouble(Side.Width, 0.0, Side.StrokeAlign)));
        return path;
    }

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null)
    {
        var path = new Path();
        path.AddRRect(AdjustBorderRadius(rect).Resolve(textDirection ?? TextDirection.Ltr).ToRRect(rect));
        return path;
    }

    public override bool PreferPaintInterior => true;

    public override void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null)
    {
        BorderRadiusGeometry adjusted = AdjustBorderRadius(rect);
        if (adjusted.IsZero)
        {
            context.DrawRectangle(brush, null, rect);
            return;
        }

        context.DrawRRect(adjusted.Resolve(textDirection ?? TextDirection.Ltr).ToRRect(rect), brush, null);
    }

    public override void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null)
    {
        if (Side.Style == BorderStyle.None)
        {
            return;
        }

        RRect borderRect = AdjustBorderRadius(rect).Resolve(textDirection ?? TextDirection.Ltr).ToRRect(rect);
        context.DrawRRect(borderRect.Inflate(Side.StrokeOffset / 2.0), null, Side.ToPen());
    }

    public override string ToString()
    {
        string rectilinearity = (Rectilinearity * 100.0)
            .ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        return $"StadiumBorder({Side}, {BorderRadius}, {rectilinearity}% of the way to being a "
               + "RoundedRectangleBorder)";
    }

    private BorderRadiusGeometry AdjustBorderRadius(Rect rect)
    {
        Radius circular = Radius.Circular(BoxBorder.ShortestSide(rect) / 2.0);
        return BorderRadiusGeometry.Lerp(
            BorderRadius,
            new BorderRadius(circular, circular, circular, circular),
            1.0 - Rectilinearity)!.Value;
    }

    private static double LerpDouble(double a, double b, double t)
    {
        return (a * (1.0 - t)) + (b * t);
    }
}
