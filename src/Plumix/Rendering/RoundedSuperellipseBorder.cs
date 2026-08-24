using Avalonia;
using Avalonia.Media;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/rounded_rectangle_border.dart

/// A rectangular border with smooth, iOS-style rounded-superellipse corners.
public sealed record RoundedSuperellipseBorder : OutlinedBorder
{
    public RoundedSuperellipseBorder(BorderSide? side = null, BorderRadiusGeometry? borderRadius = null)
        : base(side)
    {
        BorderRadius = borderRadius ?? default;
    }

    public BorderRadiusGeometry BorderRadius { get; init; }

    public override ShapeBorder Scale(double t)
    {
        return new RoundedSuperellipseBorder(Side.Scale(t), BorderRadius * t);
    }

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        return a switch
        {
            RoundedSuperellipseBorder rounded => new RoundedSuperellipseBorder(
                BorderSide.Lerp(rounded.Side, Side, t),
                BorderRadiusGeometry.Lerp(rounded.BorderRadius, BorderRadius, t) ?? BorderRadius),
            CircleBorder circle => new RoundedSuperellipseToCircleBorder(
                BorderSide.Lerp(circle.Side, Side, t),
                BorderRadius,
                1.0 - t,
                circle.Eccentricity),
            _ => base.LerpFrom(a, t),
        };
    }

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        return b switch
        {
            RoundedSuperellipseBorder rounded => new RoundedSuperellipseBorder(
                BorderSide.Lerp(Side, rounded.Side, t),
                BorderRadiusGeometry.Lerp(BorderRadius, rounded.BorderRadius, t) ?? BorderRadius),
            CircleBorder circle => new RoundedSuperellipseToCircleBorder(
                BorderSide.Lerp(Side, circle.Side, t),
                BorderRadius,
                t,
                circle.Eccentricity),
            _ => base.LerpTo(b, t),
        };
    }

    public override OutlinedBorder CopyWith(BorderSide? side = null)
    {
        return CopyWith(side, null);
    }

    public RoundedSuperellipseBorder CopyWith(BorderSide? side, BorderRadiusGeometry? borderRadius)
    {
        return new RoundedSuperellipseBorder(side ?? Side, borderRadius ?? BorderRadius);
    }

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
    {
        var path = new Path();
        if (BorderRadius.IsZero)
        {
            path.AddRect(rect.Deflate(Side.StrokeInset));
            return path;
        }

        path.AddRSuperellipse(ResolveRSuperellipse(rect, textDirection).Deflate(Side.StrokeInset));
        return path;
    }

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null)
    {
        var path = new Path();
        if (BorderRadius.IsZero)
        {
            path.AddRect(rect);
            return path;
        }

        path.AddRSuperellipse(ResolveRSuperellipse(rect, textDirection));
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
            context.DrawRectangle(brush, null, rect);
            return;
        }

        context.DrawRSuperellipse(ResolveRSuperellipse(rect, textDirection), brush, null);
    }

    public override void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null)
    {
        if (Side.Style == BorderStyle.None)
        {
            return;
        }

        double strokeOffset = Side.StrokeOffset / 2.0;
        if (BorderRadius.IsZero)
        {
            context.DrawRectangle(Brushes.Transparent, Side.ToPen(), rect.Inflate(strokeOffset));
            return;
        }

        context.DrawRSuperellipse(
            ResolveRSuperellipse(rect, textDirection).Inflate(strokeOffset),
            null,
            Side.ToPen());
    }

    public override string ToString()
    {
        return $"RoundedSuperellipseBorder({Side}, {BorderRadius})";
    }

    private RSuperellipse ResolveRSuperellipse(Rect rect, TextDirection? textDirection)
    {
        BorderRadius radius = BorderRadius.Resolve(textDirection ?? TextDirection.Ltr);
        return RSuperellipse.FromRectAndCorners(
            rect,
            radius.TopLeftRadius,
            radius.TopRightRadius,
            radius.BottomRightRadius,
            radius.BottomLeftRadius);
    }
}

internal sealed record RoundedSuperellipseToCircleBorder : OutlinedBorder
{
    public RoundedSuperellipseToCircleBorder(
        BorderSide? side = null,
        BorderRadiusGeometry? borderRadius = null,
        double circularity = 0.0,
        double eccentricity = 0.0) : base(side)
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
        return new RoundedSuperellipseToCircleBorder(Side.Scale(t), BorderRadius * t, t, Eccentricity);
    }

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        return a switch
        {
            RoundedSuperellipseBorder rounded => CopyWithValues(
                BorderSide.Lerp(rounded.Side, Side, t),
                BorderRadiusGeometry.Lerp(rounded.BorderRadius, BorderRadius, t) ?? BorderRadius,
                Circularity * t,
                Eccentricity),
            CircleBorder circle => CopyWithValues(
                BorderSide.Lerp(circle.Side, Side, t),
                BorderRadius,
                Circularity + ((1.0 - Circularity) * (1.0 - t)),
                circle.Eccentricity),
            RoundedSuperellipseToCircleBorder other => CopyWithValues(
                BorderSide.Lerp(other.Side, Side, t),
                BorderRadiusGeometry.Lerp(other.BorderRadius, BorderRadius, t) ?? BorderRadius,
                LerpDouble(other.Circularity, Circularity, t),
                Eccentricity),
            _ => base.LerpFrom(a, t),
        };
    }

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        return b switch
        {
            RoundedSuperellipseBorder rounded => CopyWithValues(
                BorderSide.Lerp(Side, rounded.Side, t),
                BorderRadiusGeometry.Lerp(BorderRadius, rounded.BorderRadius, t) ?? BorderRadius,
                Circularity * (1.0 - t),
                Eccentricity),
            CircleBorder circle => CopyWithValues(
                BorderSide.Lerp(Side, circle.Side, t),
                BorderRadius,
                Circularity + ((1.0 - Circularity) * t),
                circle.Eccentricity),
            RoundedSuperellipseToCircleBorder other => CopyWithValues(
                BorderSide.Lerp(Side, other.Side, t),
                BorderRadiusGeometry.Lerp(BorderRadius, other.BorderRadius, t) ?? BorderRadius,
                LerpDouble(Circularity, other.Circularity, t),
                Eccentricity),
            _ => base.LerpTo(b, t),
        };
    }

    public override OutlinedBorder CopyWith(BorderSide? side = null)
    {
        return CopyWithValues(side ?? Side, BorderRadius, Circularity, Eccentricity);
    }

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
    {
        return Fallback.GetInnerPath(rect, textDirection);
    }

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null)
    {
        return Fallback.GetOuterPath(rect, textDirection);
    }

    public override bool PreferPaintInterior => true;

    public override void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null)
    {
        Fallback.PaintInterior(context, rect, brush, textDirection);
    }

    public override void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null)
    {
        Fallback.Paint(context, rect, textDirection);
    }

    private RoundedRectangleToCircleBorder Fallback =>
        new(Side, BorderRadius, Circularity, Eccentricity);

    private static RoundedSuperellipseToCircleBorder CopyWithValues(
        BorderSide side,
        BorderRadiusGeometry borderRadius,
        double circularity,
        double eccentricity)
    {
        return new RoundedSuperellipseToCircleBorder(side, borderRadius, circularity, eccentricity);
    }

    private static double LerpDouble(double a, double b, double t) => a + ((b - a) * t);
}
