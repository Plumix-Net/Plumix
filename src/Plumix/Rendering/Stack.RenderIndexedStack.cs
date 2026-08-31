using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;
using System.Diagnostics;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/stack.dart

namespace Plumix.Rendering;

public sealed class RenderIndexedStack : RenderStack
{
    private int? _index;

    public RenderIndexedStack(
        List<RenderBox>? children = null,
        AlignmentGeometry? alignment = null,
        TextDirection? textDirection = null,
        StackFit fit = StackFit.Loose,
        Clip clipBehavior = Clip.HardEdge,
        int? index = 0)
        : base(children, alignment, fit, clipBehavior, textDirection)
    {
        _index = index;
    }

    public int? Index
    {
        get => _index;
        set
        {
            if (_index == value)
            {
                return;
            }

            _index = value;
            MarkNeedsLayout();
        }
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        RenderBox? child = ChildAtIndex();
        if (child is null)
        {
            return null;
        }

        var childParentData = (StackParentData)child.parentData!;
        double? childBaseline = child.GetDistanceToBaseline(baseline, onlyReal: true);
        return childBaseline.HasValue ? childBaseline.Value + childParentData.offset.Y : null;
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        RenderBox? child = ChildAtIndex();
        if (child is null)
        {
            return null;
        }

        return BaselineForChild(
            child,
            NonPositionedConstraintsFor(constraints),
            ComputeSize(constraints, ChildLayoutHelper.DryLayoutChild),
            ResolvedAlignment,
            baseline);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        RenderBox? child = ChildAtIndex();
        if (child is null)
        {
            return false;
        }

        var childParentData = (StackParentData)child.parentData!;
        return result.AddWithPaintOffset(
            childParentData.offset,
            position,
            (hitResult, transformed) => child.HitTest(hitResult, transformed));
    }

    protected override void PaintStack(PaintingContext context, Point offset)
    {
        RenderBox? child = ChildAtIndex();
        if (child is null)
        {
            return;
        }

        var childParentData = (StackParentData)child.parentData!;
        context.PaintChild(child, childParentData.offset + offset);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        RenderBox? child = ChildAtIndex();
        if (child is not null)
        {
            visitor(child);
        }
    }

    private RenderBox? ChildAtIndex()
    {
        if (!Index.HasValue)
        {
            return null;
        }

        RenderBox? child = FirstChild;
        for (int i = 0; i < Index.Value && child is not null; i++)
        {
            child = ChildAfter(child);
        }

        Debug.Assert(FirstChild is null || child is not null);
        return child;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new IntProperty("index", Index));
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren()
    {
        var children = new List<DiagnosticsNode>();
        int i = 0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            children.Add(child.ToDiagnosticsNode(
                name: $"child {i + 1}",
                style: i != Index ? DiagnosticsTreeStyle.Offstage : null));
            i += 1;
        }

        return children;
    }
}
