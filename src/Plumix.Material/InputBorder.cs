using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Path = Plumix.UI.Path;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/input_border.dart;
// flutter/packages/flutter/lib/src/material/material_state.dart (WidgetStateInputBorder)

public abstract class InputBorder
{
    protected InputBorder(BorderSide? borderSide = null) => BorderSide = borderSide ?? BorderSide.None;

    public BorderSide BorderSide { get; }

    public abstract bool IsOutline { get; }

    public abstract InputBorder CopyWith(BorderSide? borderSide = null);

    public abstract InputBorder Scale(double t);

    public abstract Thickness Dimensions { get; }

    public abstract Path GetOuterPath(Rect rect, TextDirection? textDirection = null);

    public abstract Path GetInnerPath(Rect rect, TextDirection? textDirection = null);

    public virtual bool PreferPaintInterior => true;

    public abstract void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null);

    public abstract void Paint(
        PaintingContext context,
        Rect rect,
        double? gapStart = null,
        double gapExtent = 0.0,
        double gapPercentage = 0.0,
        TextDirection? textDirection = null);

    protected virtual InputBorder? LerpFrom(InputBorder a, double t) => null;

    protected virtual InputBorder? LerpTo(InputBorder b, double t) => null;

    public static InputBorder None { get; } = new NoInputBorder();

    public static InputBorder Lerp(InputBorder a, InputBorder b, double t)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (t <= 0.0)
        {
            return a;
        }

        if (t >= 1.0)
        {
            return b;
        }

        InputBorder? result = b.LerpFrom(a, t) ?? a.LerpTo(b, t);
        if (result is not null)
        {
            return result;
        }

        // ShapeBorder.lerp fallback: scale out, then scale in around the midpoint.
        return t < 0.5 ? a.Scale(1.0 - (t * 2.0)) : b.Scale((t - 0.5) * 2.0);
    }
}

internal sealed class NoInputBorder : InputBorder
{
    public NoInputBorder() : base(BorderSide.None)
    {
    }

    public override bool IsOutline => false;

    public override InputBorder CopyWith(BorderSide? borderSide = null) => this;

    public override InputBorder Scale(double t) => this;

    public override Thickness Dimensions => default;

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null)
    {
        var path = new Path();
        path.AddRect(rect);
        return path;
    }

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null) =>
        GetOuterPath(rect, textDirection);

    public override void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null) =>
        context.DrawRectangle(brush, null, rect);

    public override void Paint(
        PaintingContext context,
        Rect rect,
        double? gapStart = null,
        double gapExtent = 0.0,
        double gapPercentage = 0.0,
        TextDirection? textDirection = null)
    {
    }

    public override bool Equals(object? obj) => obj is NoInputBorder;

    public override int GetHashCode() => typeof(NoInputBorder).GetHashCode();
}

public class UnderlineInputBorder : InputBorder
{
    public UnderlineInputBorder(BorderSide? borderSide = null, BorderRadius? borderRadius = null)
        : base(borderSide ?? new BorderSide(Colors.Black))
    {
        BorderRadius = borderRadius ?? BorderRadius.Only(
            topLeft: Radius.Circular(4.0),
            topRight: Radius.Circular(4.0),
            bottomRight: Radius.Zero,
            bottomLeft: Radius.Zero);
    }

    public BorderRadius BorderRadius { get; }

    public override bool IsOutline => false;

    public override InputBorder CopyWith(BorderSide? borderSide = null) =>
        CopyWith(borderSide, borderRadius: null);

    public UnderlineInputBorder CopyWith(BorderSide? borderSide, BorderRadius? borderRadius) =>
        new(borderSide ?? BorderSide, borderRadius ?? BorderRadius);

    // Flutter drops borderRadius here; the scaled border reverts to the default top radii.
    public override InputBorder Scale(double t) => new UnderlineInputBorder(BorderSide.Scale(t));

    public override Thickness Dimensions => new(0, 0, 0, BorderSide.Width);

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null) =>
        BorderRadius.ToRRect(rect).ToPath();

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
    {
        var path = new Path();
        path.AddRect(new Rect(rect.Left, rect.Top, rect.Width, Math.Max(0.0, rect.Height - BorderSide.Width)));
        return path;
    }

    public override void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null) =>
        context.DrawRectangle(brush, null, rect, BorderRadius);

    public override void Paint(
        PaintingContext context,
        Rect rect,
        double? gapStart = null,
        double gapExtent = 0.0,
        double gapPercentage = 0.0,
        TextDirection? textDirection = null)
    {
        if (BorderSide.Style == BorderStyle.None)
        {
            return;
        }

        if (BorderRadius.BottomLeftRadius != Radius.Zero || BorderRadius.BottomRightRadius != Radius.Zero)
        {
            // The top radii are dropped so the painted border never bleeds across the top edge.
            var maximum = Radius.Circular(rect.Height / 2.0);
            var updated = BorderRadius.Only(
                topLeft: Radius.Zero,
                topRight: Radius.Zero,
                bottomRight: BorderRadius.BottomRightRadius.Clamp(maximum),
                bottomLeft: BorderRadius.BottomLeftRadius.Clamp(maximum));
            PaintNonUniformBottomBorder(context, rect, updated, BorderSide.Width, BorderSide.Color);
            return;
        }

        var alignInsideOffset = new Point(0, BorderSide.Width / 2.0);
        context.DrawLine(
            new Pen(new SolidColorBrush(BorderSide.Color), BorderSide.Width),
            rect.BottomLeft - alignInsideOffset,
            rect.BottomRight - alignInsideOffset);
    }

    /// Fills the ring between the outer rounded rect and the rect inset by the bottom border width,
    /// matching Flutter's `BoxBorder.paintNonUniformBorder` for a bottom-only side.
    private static void PaintNonUniformBottomBorder(
        PaintingContext context,
        Rect rect,
        BorderRadius borderRadius,
        double width,
        Color color)
    {
        RRect outer = borderRadius.ToRRect(rect);
        RRect inner = borderRadius.ToRRect(
            new Rect(rect.Left, rect.Top, rect.Width, Math.Max(0.0, rect.Height - width)));
        Path outerPath = outer.ToPath();
        Path innerPath = inner.ToPath();
        var ring = new Path { FillType = PathFillType.EvenOdd };
        ring.AddPath(outerPath);
        ring.AddPath(innerPath);
        context.DrawGeometry(new SolidColorBrush(color), null, ring.ToGeometry());
    }

    protected override InputBorder? LerpFrom(InputBorder a, double t) => a is UnderlineInputBorder other
        ? new UnderlineInputBorder(
            BorderSide.Lerp(other.BorderSide, BorderSide, t),
            BorderRadius.Lerp(other.BorderRadius, BorderRadius, t)!.Value)
        : null;

    protected override InputBorder? LerpTo(InputBorder b, double t) => b is UnderlineInputBorder other
        ? new UnderlineInputBorder(
            BorderSide.Lerp(BorderSide, other.BorderSide, t),
            BorderRadius.Lerp(BorderRadius, other.BorderRadius, t)!.Value)
        : null;

    public override bool Equals(object? obj) => obj is UnderlineInputBorder other
                                                && other.GetType() == GetType()
                                                && other.BorderSide == BorderSide
                                                && other.BorderRadius == BorderRadius;

    public override int GetHashCode() => HashCode.Combine(BorderSide, BorderRadius);
}

public class OutlineInputBorder : InputBorder
{
    public OutlineInputBorder(
        BorderSide? borderSide = null,
        BorderRadius? borderRadius = null,
        double gapPadding = 4.0)
        : base(borderSide ?? new BorderSide(Colors.Black))
    {
        if (gapPadding < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(gapPadding), "gapPadding must not be negative.");
        }

        BorderRadius = borderRadius ?? BorderRadius.Circular(4.0);
        GapPadding = gapPadding;
    }

    public BorderRadius BorderRadius { get; }

    public double GapPadding { get; }

    public override bool IsOutline => true;

    public override InputBorder CopyWith(BorderSide? borderSide = null) =>
        CopyWith(borderSide, borderRadius: null, gapPadding: null);

    public OutlineInputBorder CopyWith(BorderSide? borderSide, BorderRadius? borderRadius, double? gapPadding) =>
        new(borderSide ?? BorderSide, borderRadius ?? BorderRadius, gapPadding ?? GapPadding);

    public override InputBorder Scale(double t) =>
        new OutlineInputBorder(BorderSide.Scale(t), BorderRadius * t, GapPadding * t);

    public override Thickness Dimensions => new(BorderSide.StrokeInset);

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null) =>
        BorderRadius.ToRRect(rect).ToPath();

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null) =>
        BorderRadius.ToRRect(rect).Deflate(BorderSide.StrokeInset).ToPath();

    public override void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null) =>
        context.DrawRectangle(brush, null, rect, BorderRadius);

    internal static bool CornersAreCircular(BorderRadius borderRadius) =>
        borderRadius.TopLeftRadius.X == borderRadius.TopLeftRadius.Y
        && borderRadius.BottomLeftRadius.X == borderRadius.BottomLeftRadius.Y
        && borderRadius.TopRightRadius.X == borderRadius.TopRightRadius.Y
        && borderRadius.BottomRightRadius.X == borderRadius.BottomRightRadius.Y;

    public override void Paint(
        PaintingContext context,
        Rect rect,
        double? gapStart = null,
        double gapExtent = 0.0,
        double gapPercentage = 0.0,
        TextDirection? textDirection = null)
    {
        if (gapPercentage < 0.0 || gapPercentage > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(gapPercentage));
        }

        if (!CornersAreCircular(BorderRadius))
        {
            throw new InvalidOperationException("OutlineInputBorder requires circular corner radii.");
        }

        if (BorderSide.Style == BorderStyle.None)
        {
            return;
        }

        var pen = new Pen(new SolidColorBrush(BorderSide.Color), BorderSide.Width);
        RRect outer = BorderRadius.ToRRect(rect);
        RRect center = outer.Inflate(BorderSide.StrokeOffset / 2.0);

        if (gapStart is null || gapExtent <= 0.0 || gapPercentage == 0.0)
        {
            context.DrawRectangle(Brushes.Transparent, pen, center.Rect, center.Radii);
            return;
        }

        double extent = (gapExtent + (GapPadding * 2.0)) * gapPercentage;
        double start = textDirection == TextDirection.Rtl
            ? gapStart.Value + GapPadding - extent
            : gapStart.Value - GapPadding;
        Path path = GapBorderPath(center, outer.Width, Math.Max(0.0, start), extent);
        context.DrawGeometry(null, pen, path.ToGeometry());
    }

    private Path GapBorderPath(RRect center, double outerWidth, double start, double extent)
    {
        RRect scaled = center.ScaleRadii();
        const double cornerArcSweep = Math.PI / 2.0;
        var path = new Path();

        if (scaled.TopLeft != Radius.Zero)
        {
            double topLeftSweep = Math.Acos(Math.Clamp(1.0 - (start / scaled.TopLeft.X), 0.0, 1.0));
            path.AddArc(scaled.TopLeftCorner(), Math.PI, topLeftSweep);
        }
        else
        {
            path.MoveTo(scaled.Left + (BorderSide.StrokeOffset / 2.0), scaled.Top);
        }

        if (start > scaled.TopLeft.X)
        {
            path.LineTo(start, scaled.Top);
        }

        const double topRightArcStart = 3.0 * Math.PI / 2.0;
        if (start + extent < outerWidth - scaled.TopRight.X)
        {
            path.MoveTo(start + extent, scaled.Top);
            path.LineTo(scaled.Right - scaled.TopRight.X, scaled.Top);
            if (scaled.TopRight != Radius.Zero)
            {
                path.AddArc(scaled.TopRightCorner(), topRightArcStart, cornerArcSweep);
            }
        }
        else if (start + extent < outerWidth)
        {
            double dx = outerWidth - (start + extent);
            double sweep = Math.Asin(Math.Clamp(1.0 - (dx / scaled.TopRight.X), 0.0, 1.0));
            path.AddArc(scaled.TopRightCorner(), topRightArcStart + sweep, cornerArcSweep - sweep);
        }

        if (scaled.BottomRight != Radius.Zero)
        {
            path.MoveTo(scaled.Right, scaled.Top + scaled.TopRight.Y);
        }

        path.LineTo(scaled.Right, scaled.Bottom - scaled.BottomRight.Y);
        if (scaled.BottomRight != Radius.Zero)
        {
            path.AddArc(scaled.BottomRightCorner(), 0.0, cornerArcSweep);
        }

        path.LineTo(scaled.Left + scaled.BottomLeft.X, scaled.Bottom);
        if (scaled.BottomLeft != Radius.Zero)
        {
            path.AddArc(scaled.BottomLeftCorner(), Math.PI / 2.0, cornerArcSweep);
        }

        path.LineTo(scaled.Left, scaled.Top + scaled.TopLeft.Y);
        return path;
    }

    protected override InputBorder? LerpFrom(InputBorder a, double t) => a is OutlineInputBorder other
        ? new OutlineInputBorder(
            BorderSide.Lerp(other.BorderSide, BorderSide, t),
            BorderRadius.Lerp(other.BorderRadius, BorderRadius, t)!.Value,
            other.GapPadding)
        : null;

    protected override InputBorder? LerpTo(InputBorder b, double t) => b is OutlineInputBorder other
        ? new OutlineInputBorder(
            BorderSide.Lerp(BorderSide, other.BorderSide, t),
            BorderRadius.Lerp(BorderRadius, other.BorderRadius, t)!.Value,
            other.GapPadding)
        : null;

    public override bool Equals(object? obj) => obj is OutlineInputBorder other
                                                && other.GetType() == GetType()
                                                && other.BorderSide == BorderSide
                                                && other.BorderRadius == BorderRadius
                                                && other.GapPadding.Equals(GapPadding);

    public override int GetHashCode() => HashCode.Combine(BorderSide, BorderRadius, GapPadding);
}

// Dart parity source: flutter/packages/flutter/lib/src/material/material_state.dart
// (WidgetStateInputBorder, MaterialStateOutlineInputBorder, MaterialStateUnderlineInputBorder).
//
// InputDecoration resolves these borders from its current interactive states before any default
// side is applied, exactly as Flutter treats a WidgetStateProperty<InputBorder>.
public interface IStateInputBorder
{
    InputBorder Resolve(MaterialState states);
}

public abstract class MaterialStateOutlineInputBorder : OutlineInputBorder, IStateInputBorder
{
    protected MaterialStateOutlineInputBorder(
        BorderSide? borderSide = null,
        BorderRadius? borderRadius = null,
        double gapPadding = 4.0) : base(borderSide, borderRadius, gapPadding)
    {
    }

    public abstract InputBorder Resolve(MaterialState states);

    public static MaterialStateOutlineInputBorder ResolveWith(Func<MaterialState, InputBorder> resolver) =>
        new ResolverMaterialStateOutlineInputBorder(resolver);

    private sealed class ResolverMaterialStateOutlineInputBorder : MaterialStateOutlineInputBorder
    {
        private readonly Func<MaterialState, InputBorder> _resolver;

        public ResolverMaterialStateOutlineInputBorder(Func<MaterialState, InputBorder> resolver) =>
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));

        public override InputBorder Resolve(MaterialState states) =>
            _resolver(states)
            ?? throw new InvalidOperationException("A state input-border resolver cannot return null.");
    }
}

public abstract class MaterialStateUnderlineInputBorder : UnderlineInputBorder, IStateInputBorder
{
    protected MaterialStateUnderlineInputBorder(
        BorderSide? borderSide = null,
        BorderRadius? borderRadius = null) : base(borderSide, borderRadius)
    {
    }

    public abstract InputBorder Resolve(MaterialState states);

    public static MaterialStateUnderlineInputBorder ResolveWith(Func<MaterialState, InputBorder> resolver) =>
        new ResolverMaterialStateUnderlineInputBorder(resolver);

    private sealed class ResolverMaterialStateUnderlineInputBorder : MaterialStateUnderlineInputBorder
    {
        private readonly Func<MaterialState, InputBorder> _resolver;

        public ResolverMaterialStateUnderlineInputBorder(Func<MaterialState, InputBorder> resolver) =>
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));

        public override InputBorder Resolve(MaterialState states) =>
            _resolver(states)
            ?? throw new InvalidOperationException("A state input-border resolver cannot return null.");
    }
}
