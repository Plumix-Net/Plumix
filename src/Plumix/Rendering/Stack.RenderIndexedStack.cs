using Avalonia;
using Avalonia.Media;
using Plumix.UI;
using Plumix.Foundation;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/stack.dart (RenderIndexedStack subset)

public sealed class IndexedStackParentData : ContainerBoxParentData<RenderBox>
{
}

public sealed class RenderIndexedStack : RenderBox, IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, IndexedStackParentData> _container;
    private int? _index;
    private Alignment _alignment;

    public RenderIndexedStack(int? index = 0, Alignment alignment = default)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, IndexedStackParentData>(this);
        _index = index;
        _alignment = alignment;
    }

    public int? Index
    {
        get => _index;
        set
        {
            if (_index == value) return;
            _index = value;
            MarkNeedsLayout();
        }
    }

    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value) return;
            _alignment = value;
            MarkNeedsLayout();
        }
    }

    public int ChildCount => _container.ChildCount;
    public RenderBox? FirstChild => _container.FirstChild;
    public RenderBox? LastChild => _container.LastChild;
    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);
    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not IndexedStackParentData)
        {
            child.parentData = new IndexedStackParentData();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height) =>
        GetIntrinsicDimension(child => child.GetMinIntrinsicWidth(height));

    protected override double ComputeMaxIntrinsicWidth(double height) =>
        GetIntrinsicDimension(child => child.GetMaxIntrinsicWidth(height));

    protected override double ComputeMinIntrinsicHeight(double width) =>
        GetIntrinsicDimension(child => child.GetMinIntrinsicHeight(width));

    protected override double ComputeMaxIntrinsicHeight(double width) =>
        GetIntrinsicDimension(child => child.GetMaxIntrinsicHeight(width));

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        BoxConstraints childConstraints = BoxConstraints.Loose(constraints.Biggest);
        double maxWidth = constraints.MinWidth;
        double maxHeight = constraints.MinHeight;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            Size childSize = child.GetDryLayout(childConstraints);
            maxWidth = Math.Max(maxWidth, childSize.Width);
            maxHeight = Math.Max(maxHeight, childSize.Height);
        }

        return constraints.Constrain(new Size(maxWidth, maxHeight));
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        RenderBox? child = SelectedChild;
        if (child is null)
        {
            return null;
        }

        var data = (IndexedStackParentData)child.parentData!;
        double? childBaseline = child.GetDistanceToBaseline(baseline, onlyReal: true);
        return childBaseline.HasValue ? childBaseline.Value + data.offset.Y : null;
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        RenderBox? child = SelectedChild;
        if (child is null)
        {
            return null;
        }

        BoxConstraints childConstraints = BoxConstraints.Loose(constraints.Biggest);
        Size stackSize = ComputeDryLayout(constraints);
        Size childSize = child.GetDryLayout(childConstraints);
        double? childBaseline = child.GetDryBaseline(childConstraints, baseline);
        return childBaseline.HasValue
            ? childBaseline.Value + Alignment.AlongOffset(stackSize, childSize).Y
            : null;
    }

    protected override void PerformLayout()
    {
        var childConstraints = BoxConstraints.Loose(Constraints.Biggest);
        double maxWidth = 0.0;
        double maxHeight = 0.0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            child.Layout(childConstraints, parentUsesSize: true);
            maxWidth = Math.Max(maxWidth, child.Size.Width);
            maxHeight = Math.Max(maxHeight, child.Size.Height);
        }

        Size = Constraints.Constrain(new Size(maxWidth, maxHeight));
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            ((IndexedStackParentData)child.parentData!).offset = Alignment.AlongOffset(Size, child.Size);
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        var child = SelectedChild;
        if (child is null) return;
        var data = (IndexedStackParentData)child.parentData!;
        context.PaintChild(child, data.offset + offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        var child = SelectedChild;
        if (child is null) return false;
        var data = (IndexedStackParentData)child.parentData!;
        return result.AddWithPaintOffset(
            data.offset,
            position,
            (hitResult, transformed) => child.HitTest(hitResult, transformed));
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child)) visitor(child);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        var child = SelectedChild;
        if (child is null) return;
        visitor(child);
    }

    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);
    public void Remove(RenderBox child) => _container.Remove(child);

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) =>
        Insert((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) =>
        Move((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);

    private RenderBox? SelectedChild
    {
        get
        {
            if (!_index.HasValue || _index.Value < 0 || _index.Value >= ChildCount) return null;
            var current = FirstChild;
            for (int i = 0; i < _index.Value && current is not null; i++) current = ChildAfter(current);
            return current;
        }
    }

    private double GetIntrinsicDimension(Func<RenderBox, double> getter)
    {
        double extent = 0.0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            extent = Math.Max(extent, getter(child));
        }

        return extent;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Alignment>("alignment", Alignment));
        properties.Add(new IntProperty("index", Index));
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren()
    {
        var children = new List<DiagnosticsNode>();
        int i = 0;
        RenderBox? child = _container.FirstChild;
        while (child is not null)
        {
            children.Add(child.ToDiagnosticsNode(
                name: $"child {i + 1}",
                style: i != Index ? DiagnosticsTreeStyle.Offstage : null));
            child = ((IndexedStackParentData)child.parentData!).nextSibling;
            i += 1;
        }

        return children;
    }
}
