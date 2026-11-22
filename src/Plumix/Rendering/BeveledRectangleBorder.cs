using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/beveled_rectangle_border.dart

/// A rectangular border with flattened or "beveled" corners.
public sealed record BeveledRectangleBorder : OutlinedBorder
{
    public BeveledRectangleBorder(BorderSide? side = null, BorderRadiusGeometry? borderRadius = null)
        : base(side)
    {
        BorderRadius = borderRadius ?? default;
    }

    public BorderRadiusGeometry BorderRadius { get; init; }

    public override ShapeBorder Scale(double t)
    {
        return new BeveledRectangleBorder(Side.Scale(t), BorderRadius * t);
    }

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        if (a is BeveledRectangleBorder beveled)
        {
            return new BeveledRectangleBorder(
                BorderSide.Lerp(beveled.Side, Side, t),
                BorderRadiusGeometry.Lerp(beveled.BorderRadius, BorderRadius, t)!.Value);
        }

        return base.LerpFrom(a, t);
    }

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        if (b is BeveledRectangleBorder beveled)
        {
            return new BeveledRectangleBorder(
                BorderSide.Lerp(Side, beveled.Side, t),
                BorderRadiusGeometry.Lerp(BorderRadius, beveled.BorderRadius, t)!.Value);
        }

        return base.LerpTo(b, t);
    }

    public override OutlinedBorder CopyWith(BorderSide? side = null)
    {
        return CopyWith(side, null);
    }

    public BeveledRectangleBorder CopyWith(BorderSide? side, BorderRadiusGeometry? borderRadius)
    {
        return new BeveledRectangleBorder(side ?? Side, borderRadius ?? BorderRadius);
    }

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
    {
        return GetPath(
            BorderRadius.Resolve(textDirection ?? TextDirection.Ltr).ToRRect(rect).Deflate(Side.StrokeInset));
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

        RRect borderRect = BorderRadius.Resolve(textDirection ?? TextDirection.Ltr).ToRRect(rect);
        RRect adjustedRect = borderRect.Inflate(Side.StrokeOutset);
        Path path = GetPath(adjustedRect);
        path.AddPath(GetInnerPath(rect, textDirection));
        context.Canvas.DrawPath(path, Side.ToPaint());
    }

    public override string ToString()
    {
        return $"BeveledRectangleBorder({Side}, {BorderRadius})";
    }

    private static Path GetPath(RRect rrect)
    {
        Offset centerLeft = new(rrect.Left, rrect.Center.Dy);
        Offset centerRight = new(rrect.Right, rrect.Center.Dy);
        Offset centerTop = new(rrect.Center.Dx, rrect.Top);
        Offset centerBottom = new(rrect.Center.Dx, rrect.Bottom);

        double tlRadiusX = Math.Max(0.0, rrect.TopLeft.X);
        double tlRadiusY = Math.Max(0.0, rrect.TopLeft.Y);
        double trRadiusX = Math.Max(0.0, rrect.TopRight.X);
        double trRadiusY = Math.Max(0.0, rrect.TopRight.Y);
        double blRadiusX = Math.Max(0.0, rrect.BottomLeft.X);
        double blRadiusY = Math.Max(0.0, rrect.BottomLeft.Y);
        double brRadiusX = Math.Max(0.0, rrect.BottomRight.X);
        double brRadiusY = Math.Max(0.0, rrect.BottomRight.Y);

        Offset[] vertices =
        [
            new(rrect.Left, Math.Min(centerLeft.Dy, rrect.Top + tlRadiusY)),
            new(Math.Min(centerTop.Dx, rrect.Left + tlRadiusX), rrect.Top),
            new(Math.Max(centerTop.Dx, rrect.Right - trRadiusX), rrect.Top),
            new(rrect.Right, Math.Min(centerRight.Dy, rrect.Top + trRadiusY)),
            new(rrect.Right, Math.Max(centerRight.Dy, rrect.Bottom - brRadiusY)),
            new(Math.Max(centerBottom.Dx, rrect.Right - brRadiusX), rrect.Bottom),
            new(Math.Min(centerBottom.Dx, rrect.Left + blRadiusX), rrect.Bottom),
            new(rrect.Left, Math.Max(centerLeft.Dy, rrect.Bottom - blRadiusY)),
        ];

        var path = new Path();
        path.AddPolygon(vertices, close: true);
        return path;
    }
}
