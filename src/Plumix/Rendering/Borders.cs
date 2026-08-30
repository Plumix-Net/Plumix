using Avalonia;
using Avalonia.Media;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/borders.dart

/// Base class for shape outlines.
public abstract record ShapeBorder
{
    /// The widths of the sides of this border represented as an [EdgeInsetsGeometry].
    public abstract EdgeInsetsGeometry Dimensions { get; }

    /// Attempts to create a new object that represents the amalgamation of `this` border and the
    /// `other` border. Returns null if the merge is not supported.
    public virtual ShapeBorder? Add(ShapeBorder other, bool reversed = false)
    {
        return null;
    }

    /// Creates a new border consisting of the two borders on either side of the operator.
    public static ShapeBorder operator +(ShapeBorder a, ShapeBorder b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return a.Add(b) ?? b.Add(a, reversed: true) ?? new CompoundBorder([b, a]);
    }

    /// Creates a copy of this border, scaled by the factor `t`.
    public abstract ShapeBorder Scale(double t);

    /// Linearly interpolates from another border to `this`.
    public virtual ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        return a is null ? Scale(t) : null;
    }

    /// Linearly interpolates from `this` to another border.
    public virtual ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        return b is null ? Scale(1.0 - t) : null;
    }

    /// Linearly interpolates between two borders.
    public static ShapeBorder? Lerp(ShapeBorder? a, ShapeBorder? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        ShapeBorder? result = b?.LerpFrom(a, t)
                              ?? a?.LerpTo(b, t)
                              ?? b?.LerpTo(a, 1.0 - t)
                              ?? a?.LerpFrom(b, 1.0 - t);

        return result ?? (t < 0.5 ? a : b);
    }

    /// Creates a [Path] that describes the outer edge of the border.
    public abstract Path GetOuterPath(Rect rect, TextDirection? textDirection = null);

    /// Creates a [Path] that describes the inner edge of the border.
    public abstract Path GetInnerPath(Rect rect, TextDirection? textDirection = null);

    /// Whether the shape can paint its interior directly instead of via [GetOuterPath].
    public virtual bool PreferPaintInterior => false;

    /// Paints the interior of the shape; only valid when [PreferPaintInterior] is true.
    public virtual void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null)
    {
        throw new InvalidOperationException(
            $"{GetType().Name}.PreferPaintInterior returns false, so it is an error to call its "
            + "PaintInterior method.");
    }

    /// Paints the border within the given [Rect].
    public abstract void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null);
}

/// Represents the addition of two otherwise-incompatible borders.
public sealed record CompoundBorder : ShapeBorder
{
    public CompoundBorder(IReadOnlyList<ShapeBorder> borders)
    {
        ArgumentNullException.ThrowIfNull(borders);
        if (borders.Count < 2)
        {
            throw new ArgumentException("A compound border needs at least two borders.", nameof(borders));
        }

        if (borders.Any(static border => border is CompoundBorder))
        {
            throw new ArgumentException("Compound borders cannot be nested.", nameof(borders));
        }

        Borders = [.. borders];
    }

    public IReadOnlyList<ShapeBorder> Borders { get; }

    public override EdgeInsetsGeometry Dimensions =>
        Borders.Aggregate(EdgeInsetsGeometry.Zero, static (total, border) => total.Add(border.Dimensions));

    public override ShapeBorder Add(ShapeBorder other, bool reversed = false)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other is not CompoundBorder)
        {
            // Tries to merge the new border with the last (or first) border in this compound border.
            ShapeBorder ours = reversed ? Borders[^1] : Borders[0];
            ShapeBorder? merged = ours.Add(other, reversed) ?? other.Add(ours, !reversed);
            if (merged is not null)
            {
                ShapeBorder[] result = [.. Borders];
                result[reversed ? result.Length - 1 : 0] = merged;
                return new CompoundBorder(result);
            }
        }

        var mergedBorders = new List<ShapeBorder>();
        if (reversed)
        {
            mergedBorders.AddRange(Borders);
        }

        if (other is CompoundBorder otherCompound)
        {
            mergedBorders.AddRange(otherCompound.Borders);
        }
        else
        {
            mergedBorders.Add(other);
        }

        if (!reversed)
        {
            mergedBorders.AddRange(Borders);
        }

        return new CompoundBorder(mergedBorders);
    }

    public override ShapeBorder Scale(double t)
    {
        return new CompoundBorder([.. Borders.Select(border => border.Scale(t))]);
    }

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        return Lerp(a, this, t);
    }

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        return Lerp(this, b, t);
    }

    public static CompoundBorder Lerp(ShapeBorder? a, ShapeBorder? b, double t)
    {
        if (a is not CompoundBorder && b is not CompoundBorder)
        {
            throw new ArgumentException("At least one side of a compound border lerp must be compound.");
        }

        var results = new List<ShapeBorder>();
        int length = Math.Max(
            a is CompoundBorder compoundA ? compoundA.Borders.Count : 1,
            b is CompoundBorder compoundB ? compoundB.Borders.Count : 1);
        for (int index = 0; index < length; index++)
        {
            ShapeBorder? localA = BorderAt(a, index);
            ShapeBorder? localB = BorderAt(b, index);
            if (localA is not null && localB is not null)
            {
                ShapeBorder? localResult = localA.LerpTo(localB, t) ?? localB.LerpFrom(localA, t);
                if (localResult is not null)
                {
                    results.Add(localResult);
                    continue;
                }
            }

            // If we're changing from one shape to another, we stroke/fill the shape in the
            // appropriate order: the new shape fades in and the old shape fades out.
            if (localB is not null)
            {
                results.Add(localB.Scale(t));
            }

            if (localA is not null)
            {
                results.Add(localA.Scale(1.0 - t));
            }
        }

        return new CompoundBorder(results);
    }

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
    {
        for (int index = 0; index < Borders.Count - 1; index++)
        {
            rect = rect.Deflate(Borders[index].Dimensions.Resolve(textDirection ?? TextDirection.Ltr));
        }

        return Borders[^1].GetInnerPath(rect, textDirection);
    }

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null)
    {
        return Borders[0].GetOuterPath(rect, textDirection);
    }

    public override void PaintInterior(
        PaintingContext context,
        Rect rect,
        IBrush brush,
        TextDirection? textDirection = null)
    {
        Borders[0].PaintInterior(context, rect, brush, textDirection);
    }

    public override bool PreferPaintInterior => Borders.All(static border => border.PreferPaintInterior);

    public override void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null)
    {
        foreach (ShapeBorder border in Borders)
        {
            border.Paint(context, rect, textDirection);
            rect = rect.Deflate(border.Dimensions.Resolve(textDirection ?? TextDirection.Ltr));
        }
    }

    public bool Equals(CompoundBorder? other)
    {
        return other is not null && Borders.SequenceEqual(other.Borders);
    }

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (ShapeBorder border in Borders)
        {
            hash.Add(border);
        }

        return hash.ToHashCode();
    }

    public override string ToString()
    {
        // We list them in reverse order because when they're painted, the last one is painted first.
        return string.Join(" + ", Borders.Reverse().Select(static border => border.ToString()));
    }

    private static ShapeBorder? BorderAt(ShapeBorder? border, int index)
    {
        if (border is CompoundBorder compound)
        {
            return index < compound.Borders.Count ? compound.Borders[index] : null;
        }

        return index == 0 ? border : null;
    }
}

/// A [ShapeBorder] that draws an outline with the width and color specified by [Side].
public abstract record OutlinedBorder : ShapeBorder
{
    protected OutlinedBorder(BorderSide? side = null)
    {
        Side = side ?? BorderSide.None;
    }

    public BorderSide Side { get; init; }

    public override EdgeInsetsGeometry Dimensions => EdgeInsets.All(Math.Max(Side.StrokeInset, 0.0));

    /// Returns a copy of this OutlinedBorder that draws its outline with the specified [side].
    public abstract OutlinedBorder CopyWith(BorderSide? side = null);

    /// Linearly interpolates between two [OutlinedBorder]s.
    public static OutlinedBorder? Lerp(OutlinedBorder? a, OutlinedBorder? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        ShapeBorder? result = b?.LerpFrom(a, t)
                              ?? a?.LerpTo(b, t)
                              ?? b?.LerpTo(a, 1.0 - t)
                              ?? a?.LerpFrom(b, 1.0 - t);

        return (OutlinedBorder?)result ?? (t < 0.5 ? a : b);
    }
}

/// Paints a border around the given rectangle, one side at a time.
public static class BorderPainting
{
    // Dart parity source: flutter/packages/flutter/lib/src/painting/borders.dart paintBorder.
    public static void PaintBorder(
        PaintingContext context,
        Rect rect,
        BorderSide? top = null,
        BorderSide? right = null,
        BorderSide? bottom = null,
        BorderSide? left = null)
    {
        BorderSide topSide = top ?? BorderSide.None;
        BorderSide rightSide = right ?? BorderSide.None;
        BorderSide bottomSide = bottom ?? BorderSide.None;
        BorderSide leftSide = left ?? BorderSide.None;

        if (topSide.Style == BorderStyle.Solid)
        {
            PaintSide(
                context,
                topSide,
                new Point(rect.Left, rect.Top),
                new Point(rect.Right, rect.Top),
                new Point(rect.Right - rightSide.Width, rect.Top + topSide.Width),
                new Point(rect.Left + leftSide.Width, rect.Top + topSide.Width));
        }

        if (rightSide.Style == BorderStyle.Solid)
        {
            PaintSide(
                context,
                rightSide,
                new Point(rect.Right, rect.Top),
                new Point(rect.Right, rect.Bottom),
                new Point(rect.Right - rightSide.Width, rect.Bottom - bottomSide.Width),
                new Point(rect.Right - rightSide.Width, rect.Top + topSide.Width));
        }

        if (bottomSide.Style == BorderStyle.Solid)
        {
            PaintSide(
                context,
                bottomSide,
                new Point(rect.Right, rect.Bottom),
                new Point(rect.Left, rect.Bottom),
                new Point(rect.Left + leftSide.Width, rect.Bottom - bottomSide.Width),
                new Point(rect.Right - rightSide.Width, rect.Bottom - bottomSide.Width));
        }

        if (leftSide.Style == BorderStyle.Solid)
        {
            PaintSide(
                context,
                leftSide,
                new Point(rect.Left, rect.Bottom),
                new Point(rect.Left, rect.Top),
                new Point(rect.Left + leftSide.Width, rect.Top + topSide.Width),
                new Point(rect.Left + leftSide.Width, rect.Bottom - bottomSide.Width));
        }
    }

    private static void PaintSide(
        PaintingContext context,
        BorderSide side,
        Point start,
        Point end,
        Point innerEnd,
        Point innerStart)
    {
        var brush = new SolidColorBrush(side.Color);
        if (side.Width == 0.0)
        {
            context.Canvas.DrawLine(new Pen(brush, 0.0), start, end);
            return;
        }

        context.Canvas.DrawPolygon(brush, null, [start, end, innerEnd, innerStart]);
    }
}
