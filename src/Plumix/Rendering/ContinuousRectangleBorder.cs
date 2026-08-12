using Avalonia;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/continuous_rectangle_border.dart

/// A rectangular border with smooth continuous transitions between the straight sides and rounded corners.
public sealed record ContinuousRectangleBorder : OutlinedBorder
{
    public ContinuousRectangleBorder(BorderSide? side = null, BorderRadiusGeometry? borderRadius = null)
        : base(side)
    {
        BorderRadius = borderRadius ?? default;
    }

    public BorderRadiusGeometry BorderRadius { get; init; }

    public override EdgeInsetsGeometry Dimensions => EdgeInsets.All(Side.Width);

    public override ShapeBorder Scale(double t)
    {
        return new ContinuousRectangleBorder(Side.Scale(t), BorderRadius * t);
    }

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        if (a is ContinuousRectangleBorder continuous)
        {
            return new ContinuousRectangleBorder(
                BorderSide.Lerp(continuous.Side, Side, t),
                BorderRadiusGeometry.Lerp(continuous.BorderRadius, BorderRadius, t)!.Value);
        }

        return base.LerpFrom(a, t);
    }

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        if (b is ContinuousRectangleBorder continuous)
        {
            return new ContinuousRectangleBorder(
                BorderSide.Lerp(Side, continuous.Side, t),
                BorderRadiusGeometry.Lerp(BorderRadius, continuous.BorderRadius, t)!.Value);
        }

        return base.LerpTo(b, t);
    }

    public override OutlinedBorder CopyWith(BorderSide? side = null)
    {
        return CopyWith(side, null);
    }

    public ContinuousRectangleBorder CopyWith(BorderSide? side, BorderRadiusGeometry? borderRadius)
    {
        return new ContinuousRectangleBorder(side ?? Side, borderRadius ?? BorderRadius);
    }

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
    {
        return GetPath(BorderRadius.Resolve(textDirection ?? TextDirection.Ltr).ToRRect(rect).Deflate(Side.Width));
    }

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null)
    {
        return GetPath(BorderRadius.Resolve(textDirection ?? TextDirection.Ltr).ToRRect(rect));
    }

    public override void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null)
    {
        if (rect.Width <= 0.0 || rect.Height <= 0.0)
        {
            return;
        }

        if (Side.Style == BorderStyle.None)
        {
            return;
        }

        context.DrawPath(GetOuterPath(rect, textDirection), null, Side.ToPen());
    }

    public override string ToString()
    {
        return $"ContinuousRectangleBorder({Side}, {BorderRadius})";
    }

    private static double ClampToShortest(RRect rrect, double value)
    {
        return value > rrect.ShortestSide ? rrect.ShortestSide : value;
    }

    private static Path GetPath(RRect rrect)
    {
        double left = rrect.Left;
        double right = rrect.Right;
        double top = rrect.Top;
        double bottom = rrect.Bottom;

        double tlRadiusX = Math.Max(0.0, ClampToShortest(rrect, rrect.TopLeft.X));
        double tlRadiusY = Math.Max(0.0, ClampToShortest(rrect, rrect.TopLeft.Y));
        double trRadiusX = Math.Max(0.0, ClampToShortest(rrect, rrect.TopRight.X));
        double trRadiusY = Math.Max(0.0, ClampToShortest(rrect, rrect.TopRight.Y));
        double blRadiusX = Math.Max(0.0, ClampToShortest(rrect, rrect.BottomLeft.X));
        double blRadiusY = Math.Max(0.0, ClampToShortest(rrect, rrect.BottomLeft.Y));
        double brRadiusX = Math.Max(0.0, ClampToShortest(rrect, rrect.BottomRight.X));
        double brRadiusY = Math.Max(0.0, ClampToShortest(rrect, rrect.BottomRight.Y));

        var path = new Path();
        path.MoveTo(left, top + tlRadiusX);
        path.CubicTo(left, top, left, top, left + tlRadiusY, top);
        path.LineTo(right - trRadiusX, top);
        path.CubicTo(right, top, right, top, right, top + trRadiusY);
        path.LineTo(right, bottom - brRadiusX);
        path.CubicTo(right, bottom, right, bottom, right - brRadiusY, bottom);
        path.LineTo(left + blRadiusX, bottom);
        path.CubicTo(left, bottom, left, bottom, left, bottom - blRadiusY);
        path.Close();
        return path;
    }
}
