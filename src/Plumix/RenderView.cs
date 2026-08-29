using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Foundation;
using Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/view.dart (approximate)

namespace Plumix;

public sealed class RenderView : RenderBox, IRenderObjectSingleChildContainer
{
    private RenderBox? _child;

    public override bool IsRepaintBoundary => true;

    public RenderBox? Child
    {
        get => _child;
        set
        {
            if (ReferenceEquals(_child, value))
            {
                return;
            }

            if (_child != null)
            {
                DropChild(_child);
            }

            _child = value;

            if (_child != null)
            {
                AdoptChild(_child);
            }

            MarkNeedsLayout();
        }
    }

    RenderObject? IRenderObjectSingleChildContainer.Child
    {
        get => Child;
        set => Child = (RenderBox?)value;
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not BoxParentData)
        {
            child.parentData = new BoxParentData();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    protected override void PerformLayout()
    {
        if (_child != null)
        {
            _child.Layout(Constraints, parentUsesSize: true);
            Size = Constraints.Constrain(_child.Size);
            ((BoxParentData)_child.parentData!).offset = new Point(0, 0);
        }
        else
        {
            Size = Constraints.Constrain(new Size());
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Mirrors <see cref="PerformLayout"/> so that
    /// <see cref="Rendering.RenderingDebug.CheckIntrinsicSizes"/> does not report the view itself.
    /// </remarks>
    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return _child is null
            ? constraints.Constrain(new Size())
            : constraints.Constrain(_child.GetDryLayout(constraints));
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_child != null)
        {
            ctx.PaintChild(_child, offset);
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (_child == null)
        {
            return false;
        }

        return _child.HitTest(result, position);
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        configuration.IsSemanticBoundary = true;
    }

    internal void ScheduleInitialPaint(OffsetLayer rootLayer)
    {
        if (!rootLayer.Attached)
        {
            rootLayer.Attach(this);
        }

        _layer = rootLayer;
    }

    internal void ReplaceRootLayer(OffsetLayer rootLayer)
    {
        if (ReferenceEquals(_layer, rootLayer))
        {
            return;
        }

        if (_layer is Layer oldRootLayer && oldRootLayer.Attached)
        {
            oldRootLayer.Detach();
        }

        if (!rootLayer.Attached)
        {
            rootLayer.Attach(this);
        }

        _layer = rootLayer;
        MarkNeedsPaint();
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => DebugDescribeSingleChild(Child);
}
