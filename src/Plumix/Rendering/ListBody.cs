using Avalonia;
using Plumix.UI;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/list_body.dart

public sealed class ListBodyParentData : ContainerBoxParentData<RenderBox>;

public sealed class RenderListBody : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, ListBodyParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, ListBodyParentData> _container;
    private AxisDirection _axisDirection;

    public RenderListBody(AxisDirection axisDirection = AxisDirection.Down)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, ListBodyParentData>(this);
        _axisDirection = axisDirection;
    }

    public AxisDirection AxisDirection
    {
        get => _axisDirection;
        set
        {
            if (_axisDirection == value) return;
            _axisDirection = value;
            MarkNeedsLayout();
        }
    }

    public Axis MainAxis => ScrollDirectionUtils.AxisDirectionToAxis(AxisDirection);

    public int ChildCount => _container.ChildCount;

    public RenderBox? FirstChild => _container.FirstChild;

    public RenderBox? LastChild => _container.LastChild;

    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);

    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);

    public void AddAll(List<RenderBox> children) => _container.AddAll(children);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not ListBodyParentData) child.parentData = new ListBodyParentData();
    }

    protected override void PerformLayout()
    {
        ValidateConstraints();
        double mainAxisExtent = 0.0;
        var child = FirstChild;

        if (MainAxis == Axis.Horizontal)
        {
            double crossAxisExtent = Constraints.HasBoundedHeight
                ? Constraints.MaxHeight
                : MeasureCrossAxis(horizontal: true);
            var childConstraints = BoxConstraints.TightFor(height: crossAxisExtent);
            while (child is not null)
            {
                child.Layout(childConstraints, parentUsesSize: true);
                ((ListBodyParentData)child.parentData!).offset = new Point(mainAxisExtent, 0);
                mainAxisExtent += child.Size.Width;
                child = ChildAfter(child);
            }

            Size = Constraints.Constrain(new Size(mainAxisExtent, crossAxisExtent));
            if (AxisDirection == AxisDirection.Left) ReverseOffsets(horizontal: true, mainAxisExtent);
            return;
        }

        double verticalCrossAxisExtent = Constraints.HasBoundedWidth
            ? Constraints.MaxWidth
            : MeasureCrossAxis(horizontal: false);
        var verticalChildConstraints = BoxConstraints.TightFor(width: verticalCrossAxisExtent);
        while (child is not null)
        {
            child.Layout(verticalChildConstraints, parentUsesSize: true);
            ((ListBodyParentData)child.parentData!).offset = new Point(0, mainAxisExtent);
            mainAxisExtent += child.Size.Height;
            child = ChildAfter(child);
        }

        Size = Constraints.Constrain(new Size(verticalCrossAxisExtent, mainAxisExtent));
        if (AxisDirection == AxisDirection.Up) ReverseOffsets(horizontal: false, mainAxisExtent);
    }

    public override void Paint(PaintingContext ctx, Point offset) => _container.DefaultPaint(ctx, offset);

    protected override bool HitTestChildren(BoxHitTestResult result, Point position) =>
        _container.DefaultHitTestChildren(result, position);

    public void DefaultPaint(PaintingContext ctx, Point offset) => _container.DefaultPaint(ctx, offset);

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) =>
        _container.DefaultHitTestChildren(result, position);

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (var child = FirstChild; child is not null; child = ChildAfter(child)) visitor(child);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        for (var child = FirstChild; child is not null; child = ChildAfter(child))
        {
            visitor(child, ((ListBodyParentData)child.parentData!).offset, Matrix.Identity);
        }
    }

    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);

    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);

    public void Remove(RenderBox child) => _container.Remove(child);

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) =>
        Insert((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) =>
        Move((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);

    private void ValidateConstraints()
    {
        bool boundedMainAxis = MainAxis == Axis.Horizontal
            ? Constraints.HasBoundedWidth
            : Constraints.HasBoundedHeight;
        if (boundedMainAxis)
        {
            throw new InvalidOperationException("RenderListBody must have unlimited space along its main axis.");
        }

    }

    private double MeasureCrossAxis(bool horizontal)
    {
        double extent = 0.0;
        var probe = new BoxConstraints();
        for (var child = FirstChild; child is not null; child = ChildAfter(child))
        {
            child.Layout(probe, parentUsesSize: true);
            extent = Math.Max(extent, horizontal ? child.Size.Height : child.Size.Width);
        }

        return extent;
    }

    private void ReverseOffsets(bool horizontal, double mainAxisExtent)
    {
        for (var child = FirstChild; child is not null; child = ChildAfter(child))
        {
            var parentData = (ListBodyParentData)child.parentData!;
            parentData.offset = horizontal
                ? new Point(mainAxisExtent - parentData.offset.X - child.Size.Width, 0)
                : new Point(0, mainAxisExtent - parentData.offset.Y - child.Size.Height);
        }
    }
}
