using Avalonia;
using Avalonia.Media;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/stack.dart (approximate)

namespace Plumix.Rendering;

public enum StackFit
{
    Loose,
    Expand,
    Passthrough
}

public sealed class StackParentData : ContainerBoxParentData<RenderBox>
{
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double? Right { get; set; }
    public double? Bottom { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }

    public bool IsPositioned =>
        Left.HasValue
        || Top.HasValue
        || Right.HasValue
        || Bottom.HasValue
        || Width.HasValue
        || Height.HasValue;
}

public sealed class RenderStack : RenderBox, IRenderBoxContainerDefaultsMixin<RenderBox, StackParentData>, IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, StackParentData> _container;
    private Alignment _alignment;
    private StackFit _fit;

    public RenderStack(
        List<RenderBox>? children = null,
        Alignment alignment = default,
        StackFit fit = StackFit.Loose)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, StackParentData>(this);
        _alignment = alignment;
        _fit = fit;

        if (children != null)
        {
            AddAll(children);
        }
    }

    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }

            _alignment = value;
            MarkNeedsLayout();
        }
    }

    public StackFit Fit
    {
        get => _fit;
        set
        {
            if (_fit == value)
            {
                return;
            }

            _fit = value;
            MarkNeedsLayout();
        }
    }

    public int ChildCount => _container.ChildCount;
    public RenderBox? FirstChild => _container.FirstChild;
    public RenderBox? LastChild => _container.LastChild;
    public void AddAll(List<RenderBox> children) => _container.AddAll(children);
    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);
    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not StackParentData)
        {
            child.parentData = new StackParentData();
        }
    }

    protected override void PerformLayout()
    {
        var constraints = Constraints;
        var hasNonPositionedChild = false;
        var maxWidth = 0.0;
        var maxHeight = 0.0;

        var nonPositionedConstraints = _fit switch
        {
            StackFit.Loose => BoxConstraints.Loose(constraints.Biggest),
            StackFit.Expand => BoxConstraints.Tight(constraints.Biggest),
            StackFit.Passthrough => constraints,
            _ => constraints
        };

        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child))
        {
            var childParentData = (StackParentData)child.parentData!;
            if (childParentData.IsPositioned)
            {
                continue;
            }

            hasNonPositionedChild = true;
            child.Layout(nonPositionedConstraints, parentUsesSize: true);
            maxWidth = Math.Max(maxWidth, child.Size.Width);
            maxHeight = Math.Max(maxHeight, child.Size.Height);
        }

        if (hasNonPositionedChild)
        {
            Size = constraints.Constrain(new Size(maxWidth, maxHeight));
        }
        else
        {
            Size = constraints.Biggest;
            if (double.IsPositiveInfinity(Size.Width) || double.IsPositiveInfinity(Size.Height))
            {
                Size = constraints.Constrain(new Size(
                    double.IsPositiveInfinity(Size.Width) ? 0.0 : Size.Width,
                    double.IsPositiveInfinity(Size.Height) ? 0.0 : Size.Height));
            }
        }

        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child))
        {
            var childParentData = (StackParentData)child.parentData!;
            if (!childParentData.IsPositioned)
            {
                childParentData.offset = _alignment.AlongOffset(Size, child.Size);
                continue;
            }

            LayoutPositionedChild(child, childParentData);
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        DefaultPaint(ctx, offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        return DefaultHitTestChildren(result, position);
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child))
        {
            visitor(child);
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child))
        {
            var childParentData = (StackParentData)child.parentData!;
            visitor(child, childParentData.offset, Matrix.Identity);
        }
    }

    public void DefaultPaint(PaintingContext ctx, Point offset)
    {
        _container.DefaultPaint(ctx, offset);
    }

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position)
    {
        return _container.DefaultHitTestChildren(result, position);
    }

    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);
    public void Remove(RenderBox child) => _container.Remove(child);

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after)
    {
        Insert((RenderBox)child, after as RenderBox);
    }

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after)
    {
        Move((RenderBox)child, after as RenderBox);
    }

    void IRenderObjectContainer.Remove(RenderObject child)
    {
        Remove((RenderBox)child);
    }

    private void LayoutPositionedChild(RenderBox child, StackParentData childParentData)
    {
        var childWidth = ComputeChildExtent(
            leading: childParentData.Left,
            trailing: childParentData.Right,
            extent: childParentData.Width,
            availableExtent: Size.Width);
        var childHeight = ComputeChildExtent(
            leading: childParentData.Top,
            trailing: childParentData.Bottom,
            extent: childParentData.Height,
            availableExtent: Size.Height);

        var childConstraints = new BoxConstraints(
            MinWidth: childWidth ?? 0.0,
            MaxWidth: childWidth ?? Size.Width,
            MinHeight: childHeight ?? 0.0,
            MaxHeight: childHeight ?? Size.Height);
        child.Layout(childConstraints, parentUsesSize: true);

        var alignedOffset = _alignment.AlongOffset(Size, child.Size);
        var x = childParentData.Left
                ?? (childParentData.Right.HasValue
                    ? Size.Width - childParentData.Right.Value - child.Size.Width
                    : alignedOffset.X);
        var y = childParentData.Top
                ?? (childParentData.Bottom.HasValue
                    ? Size.Height - childParentData.Bottom.Value - child.Size.Height
                    : alignedOffset.Y);
        childParentData.offset = new Point(x, y);
    }

    private static double? ComputeChildExtent(
        double? leading,
        double? trailing,
        double? extent,
        double availableExtent)
    {
        if (leading.HasValue && trailing.HasValue)
        {
            return Math.Max(0.0, availableExtent - leading.Value - trailing.Value);
        }

        if (extent.HasValue)
        {
            return Math.Max(0.0, extent.Value);
        }

        return null;
    }
}
