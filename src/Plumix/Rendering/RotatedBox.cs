using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/rotated_box.dart

namespace Plumix.Rendering;

public sealed class RenderRotatedBox : RenderProxyBox
{
    private const double QuarterTurnRadians = Math.PI / 2.0;

    private int _quarterTurns;
    private Matrix4 _paintTransform = Matrix4.Identity();

    public RenderRotatedBox(int quarterTurns, RenderBox? child = null)
    {
        _quarterTurns = quarterTurns;
        Child = child;
    }

    public int QuarterTurns
    {
        get => _quarterTurns;
        set
        {
            if (_quarterTurns == value)
            {
                return;
            }

            _quarterTurns = value;
            MarkNeedsLayout();
        }
    }

    private bool IsVertical => QuarterTurns % 2 != 0;

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        if (Child is null)
        {
            return 0.0;
        }

        return IsVertical
            ? Child.GetMinIntrinsicHeight(height)
            : Child.GetMinIntrinsicWidth(height);
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        if (Child is null)
        {
            return 0.0;
        }

        return IsVertical
            ? Child.GetMaxIntrinsicHeight(height)
            : Child.GetMaxIntrinsicWidth(height);
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        if (Child is null)
        {
            return 0.0;
        }

        return IsVertical
            ? Child.GetMinIntrinsicWidth(width)
            : Child.GetMinIntrinsicHeight(width);
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        if (Child is null)
        {
            return 0.0;
        }

        return IsVertical
            ? Child.GetMaxIntrinsicWidth(width)
            : Child.GetMaxIntrinsicHeight(width);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        if (Child is null)
        {
            return constraints.Smallest;
        }

        Size childSize = Child.GetDryLayout(IsVertical ? constraints.Flipped : constraints);
        return IsVertical ? childSize.Flipped : childSize;
    }

    protected override void PerformLayout()
    {
        _paintTransform = Matrix4.Identity();
        if (Child is null)
        {
            Size = Constraints.Smallest;
            return;
        }

        Child.Layout(IsVertical ? Constraints.Flipped : Constraints, parentUsesSize: true);
        Size = IsVertical
            ? new Size(Child.Size.Height, Child.Size.Width)
            : Child.Size;

        _paintTransform = Matrix4.Identity();
        _paintTransform.TranslateByDouble(Size.Width / 2.0, Size.Height / 2.0, 0, 1);
        _paintTransform.RotateZ(QuarterTurnRadians * (QuarterTurns % 4));
        _paintTransform.TranslateByDouble(-Child.Size.Width / 2.0, -Child.Size.Height / 2.0, 0, 1);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child is null)
        {
            return false;
        }

        return result.AddWithPaintTransform(
            _paintTransform,
            position,
            (hitResult, hitPosition) => Child.HitTest(hitResult, hitPosition));
    }

    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (Child != null)
        {
            visitor(Child);
        }
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        if (Child is not null)
        {
            transform.Multiply(_paintTransform);
        }
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        return null;
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Child is null)
        {
            return;
        }

        Layer = ctx.PushTransform(
            NeedsCompositing,
            offset,
            _paintTransform,
            (transformedContext, transformedOffset) => transformedContext.PaintChild(Child, transformedOffset),
            Layer as TransformLayer);
    }

    private static Matrix CreateRotationMatrix(double radians)
    {
        double sine = Math.Sin(radians);
        if (sine == 1.0)
        {
            return new Matrix(0, 1, -1, 0, 0, 0);
        }

        if (sine == -1.0)
        {
            return new Matrix(0, -1, 1, 0, 0, 0);
        }

        double cosine = Math.Cos(radians);
        if (cosine == -1.0)
        {
            return new Matrix(-1, 0, 0, -1, 0, 0);
        }

        return new Matrix(cosine, sine, -sine, cosine, 0, 0);
    }
}
