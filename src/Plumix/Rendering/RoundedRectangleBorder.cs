using Avalonia;
using Avalonia.Media;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/rounded_rectangle_border.dart

/// A rectangular border with rounded corners.
public sealed record RoundedRectangleBorder : OutlinedBorder
{
    public RoundedRectangleBorder(BorderSide? side = null, BorderRadiusGeometry? borderRadius = null)
        : base(side)
    {
        BorderRadius = borderRadius ?? default;
    }

    public BorderRadiusGeometry BorderRadius { get; init; }

    public override ShapeBorder Scale(double t)
    {
        return new RoundedRectangleBorder(Side.Scale(t), BorderRadius * t);
    }

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        switch (a)
        {
            case RoundedRectangleBorder rounded:
                return new RoundedRectangleBorder(
                    BorderSide.Lerp(rounded.Side, Side, t),
                    BorderRadiusGeometry.Lerp(rounded.BorderRadius, BorderRadius, t)!.Value);
            case CircleBorder circle:
                return new RoundedRectangleToCircleBorder(
                    BorderSide.Lerp(circle.Side, Side, t),
                    BorderRadius,
                    1.0 - t,
                    circle.Eccentricity);
            default:
                return base.LerpFrom(a, t);
        }
    }

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        switch (b)
        {
            case RoundedRectangleBorder rounded:
                return new RoundedRectangleBorder(
                    BorderSide.Lerp(Side, rounded.Side, t),
                    BorderRadiusGeometry.Lerp(BorderRadius, rounded.BorderRadius, t)!.Value);
            case CircleBorder circle:
                return new RoundedRectangleToCircleBorder(
                    BorderSide.Lerp(Side, circle.Side, t),
                    BorderRadius,
                    t,
                    circle.Eccentricity);
            default:
                return base.LerpTo(b, t);
        }
    }

    public override OutlinedBorder CopyWith(BorderSide? side = null)
    {
        return CopyWith(side, null);
    }

    public RoundedRectangleBorder CopyWith(BorderSide? side, BorderRadiusGeometry? borderRadius)
    {
        return new RoundedRectangleBorder(side ?? Side, borderRadius ?? BorderRadius);
    }

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
    {
        RRect borderRect = BorderRadius.Resolve(textDirection ?? TextDirection.Ltr).ToRRect(rect);
        RRect adjustedRect = borderRect.Deflate(Side.StrokeInset);
        var path = new Path();
        path.AddRRect(adjustedRect);
        return path;
    }

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null)
    {
        var path = new Path();
        path.AddRRect(BorderRadius.Resolve(textDirection ?? TextDirection.Ltr).ToRRect(rect));
        return path;
    }

    public override bool PreferPaintInterior => true;

    public override void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null)
    {
        if (BorderRadius.IsZero)
        {
            context.Canvas.DrawRectangle(brush, null, rect);
            return;
        }

        context.Canvas.DrawRRect(BorderRadius.Resolve(textDirection ?? TextDirection.Ltr).ToRRect(rect), brush, null);
    }

    public override void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null)
    {
        if (Side.Style == BorderStyle.None)
        {
            return;
        }

        BorderRadius resolved = BorderRadius.Resolve(textDirection ?? TextDirection.Ltr);
        if (Side.Width == 0.0)
        {
            context.Canvas.DrawRRect(resolved.ToRRect(rect), null, Side.ToPen());
            return;
        }

        RRect borderRect = resolved.ToRRect(rect);
        RRect inner = borderRect.Deflate(Side.StrokeInset);
        RRect outer = borderRect.Inflate(Side.StrokeOutset);
        context.Canvas.DrawDRRect(outer, inner, new SolidColorBrush(Side.Color));
    }

    public override string ToString()
    {
        return $"RoundedRectangleBorder({Side}, {BorderRadius})";
    }
}

/// Animates a [RoundedRectangleBorder] towards a [CircleBorder].
internal sealed record RoundedRectangleToCircleBorder : OutlinedBorder
{
    public RoundedRectangleToCircleBorder(
        BorderSide? side = null,
        BorderRadiusGeometry? borderRadius = null,
        double circularity = 0.0,
        double eccentricity = 0.0)
        : base(side)
    {
        BorderRadius = borderRadius ?? default;
        Circularity = circularity;
        Eccentricity = eccentricity;
    }

    public BorderRadiusGeometry BorderRadius { get; init; }

    public double Circularity { get; init; }

    public double Eccentricity { get; init; }

    public override ShapeBorder Scale(double t)
    {
        return new RoundedRectangleToCircleBorder(Side.Scale(t), BorderRadius * t, t, Eccentricity);
    }

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        switch (a)
        {
            case RoundedRectangleBorder rounded:
                return new RoundedRectangleToCircleBorder(
                    BorderSide.Lerp(rounded.Side, Side, t),
                    BorderRadiusGeometry.Lerp(rounded.BorderRadius, BorderRadius, t) ?? BorderRadius,
                    Circularity * t,
                    Eccentricity);
            case CircleBorder circle:
                return new RoundedRectangleToCircleBorder(
                    BorderSide.Lerp(circle.Side, Side, t),
                    BorderRadius,
                    Circularity + ((1.0 - Circularity) * (1.0 - t)),
                    circle.Eccentricity);
            case RoundedRectangleToCircleBorder other:
                return new RoundedRectangleToCircleBorder(
                    BorderSide.Lerp(other.Side, Side, t),
                    BorderRadiusGeometry.Lerp(other.BorderRadius, BorderRadius, t) ?? BorderRadius,
                    LerpDouble(other.Circularity, Circularity, t),
                    Eccentricity);
            default:
                return base.LerpFrom(a, t);
        }
    }

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        switch (b)
        {
            case RoundedRectangleBorder rounded:
                return new RoundedRectangleToCircleBorder(
                    BorderSide.Lerp(Side, rounded.Side, t),
                    BorderRadiusGeometry.Lerp(BorderRadius, rounded.BorderRadius, t) ?? BorderRadius,
                    Circularity * (1.0 - t),
                    Eccentricity);
            case CircleBorder circle:
                return new RoundedRectangleToCircleBorder(
                    BorderSide.Lerp(Side, circle.Side, t),
                    BorderRadius,
                    Circularity + ((1.0 - Circularity) * t),
                    circle.Eccentricity);
            case RoundedRectangleToCircleBorder other:
                return new RoundedRectangleToCircleBorder(
                    BorderSide.Lerp(Side, other.Side, t),
                    BorderRadiusGeometry.Lerp(BorderRadius, other.BorderRadius, t) ?? BorderRadius,
                    LerpDouble(Circularity, other.Circularity, t),
                    Eccentricity);
            default:
                return base.LerpTo(b, t);
        }
    }

    public override OutlinedBorder CopyWith(BorderSide? side = null)
    {
        return new RoundedRectangleToCircleBorder(side ?? Side, BorderRadius, Circularity, Eccentricity);
    }

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
    {
        return BuildPath(
            AdjustRect(rect),
            AdjustBorderRadius(rect, textDirection),
            -LerpDouble(Side.Width, 0.0, Side.StrokeAlign));
    }

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null)
    {
        return BuildPath(AdjustRect(rect), AdjustBorderRadius(rect, textDirection), null);
    }

    public override bool PreferPaintInterior => true;

    public override void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null)
    {
        BorderRadius adjustedRadius = AdjustBorderRadius(rect, textDirection);
        if (adjustedRadius == Rendering.BorderRadius.Zero)
        {
            context.Canvas.DrawRectangle(brush, null, AdjustRect(rect));
            return;
        }

        DrawShape(context, AdjustRect(rect), adjustedRadius, brush, null, null);
    }

    public override void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null)
    {
        if (Side.Style == BorderStyle.None)
        {
            return;
        }

        DrawShape(
            context,
            AdjustRect(rect),
            AdjustBorderRadius(rect, textDirection),
            null,
            Side.ToPen(),
            Side.StrokeOffset / 2.0);
    }

    public override string ToString()
    {
        string circularity = (Circularity * 100.0).ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        if (Eccentricity != 0.0)
        {
            string oval = (Eccentricity * 100.0).ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            return $"RoundedRectangleBorder({Side}, {BorderRadius}, {circularity}% of the way to being a "
                   + $"CircleBorder that is {oval}% oval)";
        }

        return $"RoundedRectangleBorder({Side}, {BorderRadius}, {circularity}% of the way to being a CircleBorder)";
    }

    public bool Equals(RoundedRectangleToCircleBorder? other)
    {
        return other is not null
               && other.Side == Side
               && other.BorderRadius == BorderRadius
               && other.Circularity == Circularity;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Side, BorderRadius, Circularity);
    }

    private static Path BuildPath(Rect rect, BorderRadius radius, double? inflation)
    {
        RRect rrect = radius.ToRRect(rect);
        if (inflation is { } amount)
        {
            rrect = rrect.Inflate(amount);
        }

        var path = new Path();
        path.AddRRect(rrect);
        return path;
    }

    private static void DrawShape(
        PaintingContext context,
        Rect rect,
        BorderRadius radius,
        IBrush? brush,
        IPen? pen,
        double? inflation)
    {
        RRect rrect = radius.ToRRect(rect);
        if (inflation is { } amount)
        {
            rrect = rrect.Inflate(amount);
        }

        context.Canvas.DrawRRect(rrect, brush, pen);
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

    private BorderRadius AdjustBorderRadius(Rect rect, TextDirection? textDirection)
    {
        BorderRadius resolvedRadius = BorderRadius.Resolve(textDirection ?? TextDirection.Ltr);
        if (Circularity == 0.0)
        {
            return resolvedRadius;
        }

        if (Eccentricity != 0.0)
        {
            if (rect.Width < rect.Height)
            {
                return Rendering.BorderRadius.Lerp(
                    resolvedRadius,
                    UniformRadius(Radius.Elliptical(
                        rect.Width / 2.0,
                        (0.5 + (Eccentricity / 2.0)) * rect.Height / 2.0)),
                    Circularity)!.Value;
            }

            return Rendering.BorderRadius.Lerp(
                resolvedRadius,
                UniformRadius(Radius.Elliptical(
                    (0.5 + (Eccentricity / 2.0)) * rect.Width / 2.0,
                    rect.Height / 2.0)),
                Circularity)!.Value;
        }

        return Rendering.BorderRadius.Lerp(
            resolvedRadius,
            Rendering.BorderRadius.Circular(BoxBorder.ShortestSide(rect) / 2.0),
            Circularity)!.Value;
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
