using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/input_border.dart;
// flutter/packages/flutter/lib/src/material/material_state.dart (WidgetStateInputBorder)

/// Defines the appearance of an [InputDecorator]'s border.
///
/// Flutter's `InputBorder` extends `ShapeBorder` and widens `paint` with the gap parameters the
/// floating label needs. C# cannot widen an override's parameter list, so the inherited three-argument
/// [Paint] is sealed here and forwards to the gap-aware overload, which every subclass implements.
public abstract record InputBorder : ShapeBorder
{
    protected InputBorder(BorderSide? borderSide = null) => BorderSide = borderSide ?? BorderSide.None;

    /// No input border.
    public static InputBorder None { get; } = new NoInputBorder();

    /// Defines the border line's color and weight.
    public BorderSide BorderSide { get; init; }

    /// Creates a copy of this input border with the specified `borderSide`.
    public abstract InputBorder CopyWith(BorderSide? borderSide = null);

    /// True if this border will enclose the [InputDecorator]'s container.
    public abstract bool IsOutline { get; }

    public sealed override void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null)
    {
        Paint(context, rect, gapStart: null, textDirection: textDirection);
    }

    /// Paints the border, leaving a gap for the floating label when one is requested.
    ///
    /// The gap runs from `gapStart` for `gapExtent` logical pixels, opened by `gapPercentage`.
    public abstract void Paint(
        PaintingContext context,
        Rect rect,
        double? gapStart,
        double gapExtent = 0.0,
        double gapPercentage = 0.0,
        TextDirection? textDirection = null);
}

internal sealed record NoInputBorder : InputBorder
{
    public NoInputBorder() : base(BorderSide.None)
    {
    }

    public override InputBorder CopyWith(BorderSide? borderSide = null) => new NoInputBorder();

    public override bool IsOutline => false;

    public override EdgeInsetsGeometry Dimensions => EdgeInsetsGeometry.Zero;

    public override ShapeBorder Scale(double t) => new NoInputBorder();

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
    {
        var path = new Path();
        path.AddRect(rect);
        return path;
    }

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null)
    {
        var path = new Path();
        path.AddRect(rect);
        return path;
    }

    public override void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null) =>
        context.DrawRectangle(brush, null, rect);

    public override bool PreferPaintInterior => true;

    public override void Paint(
        PaintingContext context,
        Rect rect,
        double? gapStart,
        double gapExtent = 0.0,
        double gapPercentage = 0.0,
        TextDirection? textDirection = null)
    {
        // Do not paint.
    }
}

/// Draws a horizontal line at the bottom of an [InputDecorator]'s container and defines the container's
/// shape.
public record UnderlineInputBorder : InputBorder
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

    /// The radii of the border's rounded rectangle corners.
    public BorderRadius BorderRadius { get; init; }

    public override bool IsOutline => false;

    public override InputBorder CopyWith(BorderSide? borderSide = null) =>
        CopyWith(borderSide, borderRadius: null);

    public UnderlineInputBorder CopyWith(BorderSide? borderSide, BorderRadius? borderRadius) =>
        new(borderSide ?? BorderSide, borderRadius ?? BorderRadius);

    public override EdgeInsetsGeometry Dimensions => EdgeInsetsGeometry.Only(bottom: BorderSide.Width);

    // Flutter drops borderRadius here; the scaled border reverts to the default top radii.
    public override ShapeBorder Scale(double t) => new UnderlineInputBorder(BorderSide.Scale(t));

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
    {
        var path = new Path();
        path.AddRect(new Rect(rect.Left, rect.Top, rect.Width, Math.Max(0.0, rect.Height - BorderSide.Width)));
        return path;
    }

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null) =>
        BorderRadius.ToRRect(rect).ToPath();

    public override void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null) =>
        context.DrawRectangle(brush, null, rect, BorderRadius);

    public override bool PreferPaintInterior => true;

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t) => a is UnderlineInputBorder other
        ? new UnderlineInputBorder(
            BorderSide.Lerp(other.BorderSide, BorderSide, t),
            BorderRadius.Lerp(other.BorderRadius, BorderRadius, t)!.Value)
        : base.LerpFrom(a, t);

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t) => b is UnderlineInputBorder other
        ? new UnderlineInputBorder(
            BorderSide.Lerp(BorderSide, other.BorderSide, t),
            BorderRadius.Lerp(BorderRadius, other.BorderRadius, t)!.Value)
        : base.LerpTo(b, t);

    public override void Paint(
        PaintingContext context,
        Rect rect,
        double? gapStart,
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
            // This prevents the border from leaking the color due to anti-aliasing rounding errors.
            var maximum = Radius.Circular(rect.Height / 2.0);
            BorderRadius updatedBorderRadius = BorderRadius.Only(
                topLeft: Radius.Zero,
                topRight: Radius.Zero,
                bottomRight: BorderRadius.BottomRightRadius.Clamp(maximum),
                bottomLeft: BorderRadius.BottomLeftRadius.Clamp(maximum));
            BoxBorder.PaintNonUniformBorder(
                context,
                rect,
                borderRadius: updatedBorderRadius,
                textDirection: textDirection,
                shape: BoxShape.Rectangle,
                top: BorderSide.None,
                right: BorderSide.None,
                bottom: BorderSide.CopyWith(strokeAlign: BorderSide.StrokeAlignInside),
                left: BorderSide.None,
                color: BorderSide.Color);
            return;
        }

        var alignInsideOffset = new Point(0, BorderSide.Width / 2.0);
        context.DrawLine(
            BorderSide.ToPen()!,
            rect.BottomLeft - alignInsideOffset,
            rect.BottomRight - alignInsideOffset);
    }
}

/// Draws a rounded rectangle around an [InputDecorator]'s container.
public record OutlineInputBorder : InputBorder
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

    /// Horizontal padding on either side of the border's [InputDecoration.labelText] width gap.
    public double GapPadding { get; init; }

    /// The radii of the border's rounded rectangle corners.
    public BorderRadius BorderRadius { get; init; }

    internal static bool CornersAreCircular(BorderRadius borderRadius) =>
        borderRadius.TopLeftRadius.X == borderRadius.TopLeftRadius.Y
        && borderRadius.BottomLeftRadius.X == borderRadius.BottomLeftRadius.Y
        && borderRadius.TopRightRadius.X == borderRadius.TopRightRadius.Y
        && borderRadius.BottomRightRadius.X == borderRadius.BottomRightRadius.Y;

    public override bool IsOutline => true;

    public override InputBorder CopyWith(BorderSide? borderSide = null) =>
        CopyWith(borderSide, borderRadius: null, gapPadding: null);

    public OutlineInputBorder CopyWith(BorderSide? borderSide, BorderRadius? borderRadius, double? gapPadding) =>
        new(borderSide ?? BorderSide, borderRadius ?? BorderRadius, gapPadding ?? GapPadding);

    public override EdgeInsetsGeometry Dimensions => EdgeInsetsGeometry.All(BorderSide.StrokeInset);

    public override ShapeBorder Scale(double t) =>
        new OutlineInputBorder(BorderSide.Scale(t), BorderRadius * t, GapPadding * t);

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t) => a is OutlineInputBorder other
        ? new OutlineInputBorder(
            BorderSide.Lerp(other.BorderSide, BorderSide, t),
            BorderRadius.Lerp(other.BorderRadius, BorderRadius, t)!.Value,
            other.GapPadding)
        : base.LerpFrom(a, t);

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t) => b is OutlineInputBorder other
        ? new OutlineInputBorder(
            BorderSide.Lerp(BorderSide, other.BorderSide, t),
            BorderRadius.Lerp(BorderRadius, other.BorderRadius, t)!.Value,
            other.GapPadding)
        : base.LerpTo(b, t);

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null) =>
        BorderRadius.ToRRect(rect).Deflate(BorderSide.StrokeInset).ToPath();

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null) =>
        BorderRadius.ToRRect(rect).ToPath();

    public override void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null) =>
        context.DrawRectangle(brush, null, rect, BorderRadius);

    public override bool PreferPaintInterior => true;

    public override void Paint(
        PaintingContext context,
        Rect rect,
        double? gapStart,
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

        IPen pen = BorderSide.ToPen()!;
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
            // Because the path is painted with a butt stroke cap, the horizontal coordinate is moved
            // based on strokeOffset to respect strokeAlign.
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
}

/// Draws an arbitrary [ShapeBorder] around an [InputDecorator]'s container, opening a gap in the top
/// edge for the floating label.
public record ShapedInputBorder : InputBorder
{
    public ShapedInputBorder(
        ShapeBorder shape,
        BorderSide? borderSide = null,
        double gapPadding = 4.0)
        : base(borderSide ?? new BorderSide(Colors.Black))
    {
        ArgumentNullException.ThrowIfNull(shape);
        if (gapPadding < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(gapPadding), "gapPadding must not be negative.");
        }

        Shape = shape;
        GapPadding = gapPadding;
    }

    /// The shape that outlines the container.
    public ShapeBorder Shape { get; init; }

    /// Horizontal padding on either side of the border's [InputDecoration.labelText] width gap.
    public double GapPadding { get; init; }

    public override bool IsOutline => true;

    public override InputBorder CopyWith(BorderSide? borderSide = null) =>
        CopyWith(borderSide, shape: null, gapPadding: null);

    public ShapedInputBorder CopyWith(BorderSide? borderSide, ShapeBorder? shape, double? gapPadding) =>
        new(shape ?? Shape, borderSide ?? BorderSide, gapPadding ?? GapPadding);

    public override EdgeInsetsGeometry Dimensions => EdgeInsetsGeometry.All(BorderSide.Width);

    public override ShapeBorder Scale(double t) =>
        new ShapedInputBorder(Shape.Scale(t), BorderSide.Scale(t), GapPadding * t);

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t) => a is ShapedInputBorder other
        ? new ShapedInputBorder(
            Lerp(other.Shape, Shape, t)!,
            BorderSide.Lerp(other.BorderSide, BorderSide, t),
            other.GapPadding)
        : base.LerpFrom(a, t);

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t) => b is ShapedInputBorder other
        ? new ShapedInputBorder(
            Lerp(Shape, other.Shape, t)!,
            BorderSide.Lerp(BorderSide, other.BorderSide, t),
            other.GapPadding)
        : base.LerpTo(b, t);

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null) =>
        Shape.GetInnerPath(rect.Deflate(BorderSide.Width), textDirection);

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null) =>
        Shape.GetOuterPath(rect, textDirection);

    public override bool PreferPaintInterior => Shape.PreferPaintInterior;

    public override void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null)
    {
        if (Shape.PreferPaintInterior)
        {
            Shape.PaintInterior(context, rect, brush, textDirection);
            return;
        }

        context.DrawGeometry(brush, null, Shape.GetOuterPath(rect, textDirection).ToGeometry());
    }

    public override void Paint(
        PaintingContext context,
        Rect rect,
        double? gapStart,
        double gapExtent = 0.0,
        double gapPercentage = 0.0,
        TextDirection? textDirection = null)
    {
        if (gapPercentage < 0.0 || gapPercentage > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(gapPercentage));
        }

        Rect deflatedRect = rect.Deflate(BorderSide.Width / 2.0);
        if (gapStart is null || gapExtent <= 0.0 || gapPercentage == 0.0)
        {
            if (Shape is OutlinedBorder outlined)
            {
                outlined.CopyWith(BorderSide).Paint(context, deflatedRect, textDirection);
                return;
            }

            context.DrawGeometry(
                null,
                BorderSide.ToPen(),
                Shape.GetOuterPath(deflatedRect, textDirection).ToGeometry());
            return;
        }

        double extent = (gapExtent + (GapPadding * 2.0)) * gapPercentage;
        double start = textDirection == TextDirection.Rtl
            ? gapStart.Value + GapPadding - extent
            : gapStart.Value - GapPadding;
        Path path = GapBorderPath(deflatedRect, Math.Max(0.0, start), extent, textDirection);
        context.DrawGeometry(null, BorderSide.ToPen(), path.ToGeometry());
    }

    private Path GapBorderPath(Rect rect, double start, double extent, TextDirection? textDirection)
    {
        Path outerPath = Shape.GetOuterPath(rect, textDirection);
        if (start <= 0.0 && extent <= 0.0)
        {
            return outerPath;
        }

        double gapRight = start + extent;
        var gapRect = new Path();
        // The band extends slightly beyond the top edge to ensure a clean cut, and is kept short so
        // that only the top edge is affected.
        gapRect.AddRect(new Rect(
            new Point(Math.Clamp(start, rect.Left, rect.Right), rect.Top - 1.0),
            new Point(Math.Clamp(gapRight, rect.Left, rect.Right), rect.Top + 1.0)));
        return Path.Combine(PathOperation.Difference, outerPath, gapRect);
    }
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

public abstract record MaterialStateOutlineInputBorder : OutlineInputBorder, IStateInputBorder
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

    private sealed record ResolverMaterialStateOutlineInputBorder : MaterialStateOutlineInputBorder
    {
        private readonly Func<MaterialState, InputBorder> _resolver;

        public ResolverMaterialStateOutlineInputBorder(Func<MaterialState, InputBorder> resolver) =>
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));

        public override InputBorder Resolve(MaterialState states) =>
            _resolver(states)
            ?? throw new InvalidOperationException("A state input-border resolver cannot return null.");
    }
}

public abstract record MaterialStateUnderlineInputBorder : UnderlineInputBorder, IStateInputBorder
{
    protected MaterialStateUnderlineInputBorder(
        BorderSide? borderSide = null,
        BorderRadius? borderRadius = null) : base(borderSide, borderRadius)
    {
    }

    public abstract InputBorder Resolve(MaterialState states);

    public static MaterialStateUnderlineInputBorder ResolveWith(Func<MaterialState, InputBorder> resolver) =>
        new ResolverMaterialStateUnderlineInputBorder(resolver);

    private sealed record ResolverMaterialStateUnderlineInputBorder : MaterialStateUnderlineInputBorder
    {
        private readonly Func<MaterialState, InputBorder> _resolver;

        public ResolverMaterialStateUnderlineInputBorder(Func<MaterialState, InputBorder> resolver) =>
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));

        public override InputBorder Resolve(MaterialState states) =>
            _resolver(states)
            ?? throw new InvalidOperationException("A state input-border resolver cannot return null.");
    }
}
