using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Shared one-dimensional equalized layout used by Flutter's ToggleButtons and SegmentedButton.
internal sealed class SegmentedControlLayout : MultiChildRenderObjectWidget
{
    public SegmentedControlLayout(
        IReadOnlyList<Widget> children,
        Axis direction,
        TextDirection textDirection,
        VerticalDirection verticalDirection = VerticalDirection.Down,
        bool expanded = false,
        Key? key = null) : base(children, key)
    {
        Direction = direction;
        TextDirection = textDirection;
        VerticalDirection = verticalDirection;
        Expanded = expanded;
    }

    public Axis Direction { get; }
    public TextDirection TextDirection { get; }
    public VerticalDirection VerticalDirection { get; }
    public bool Expanded { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSegmentedControlLayout(
            direction: Direction,
            textDirection: TextDirection,
            verticalDirection: VerticalDirection,
            expanded: Expanded);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var layout = (RenderSegmentedControlLayout)renderObject;
        layout.Direction = Direction;
        layout.TextDirection = TextDirection;
        layout.VerticalDirection = VerticalDirection;
        layout.Expanded = Expanded;
    }
}

internal sealed class SegmentedControlParentData : ContainerBoxParentData<RenderBox>;

internal sealed class RenderSegmentedControlLayout : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, SegmentedControlParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, SegmentedControlParentData> _container;
    private Axis _direction;
    private TextDirection _textDirection;
    private VerticalDirection _verticalDirection;
    private bool _expanded;

    public RenderSegmentedControlLayout(
        Axis direction,
        TextDirection textDirection,
        VerticalDirection verticalDirection,
        bool expanded)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, SegmentedControlParentData>(this);
        _direction = direction;
        _textDirection = textDirection;
        _verticalDirection = verticalDirection;
        _expanded = expanded;
    }

    public Axis Direction
    {
        get => _direction;
        set { if (_direction != value) { _direction = value; MarkNeedsLayout(); } }
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set { if (_textDirection != value) { _textDirection = value; MarkNeedsLayout(); } }
    }

    public VerticalDirection VerticalDirection
    {
        get => _verticalDirection;
        set { if (_verticalDirection != value) { _verticalDirection = value; MarkNeedsLayout(); } }
    }

    public bool Expanded
    {
        get => _expanded;
        set { if (_expanded != value) { _expanded = value; MarkNeedsLayout(); } }
    }

    public int ChildCount => _container.ChildCount;
    public RenderBox? FirstChild => _container.FirstChild;
    public RenderBox? LastChild => _container.LastChild;
    public void AddAll(List<RenderBox> children) => _container.AddAll(children);
    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);
    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not SegmentedControlParentData)
        {
            child.parentData = new SegmentedControlParentData();
        }
    }

    protected override void PerformLayout()
    {
        if (ChildCount == 0)
        {
            Size = Constraints.Smallest;
            return;
        }

        var measured = new List<(RenderBox Child, Size Size)>(ChildCount);
        var loose = new BoxConstraints(
            MaxWidth: Constraints.MaxWidth,
            MaxHeight: Constraints.MaxHeight);
        double maxWidth = 0.0;
        double maxHeight = 0.0;
        double totalWidth = 0.0;
        double totalHeight = 0.0;
        for (var child = FirstChild; child is not null; child = ChildAfter(child))
        {
            child.Layout(loose, parentUsesSize: true);
            measured.Add((child, child.Size));
            maxWidth = Math.Max(maxWidth, child.Size.Width);
            maxHeight = Math.Max(maxHeight, child.Size.Height);
            totalWidth += child.Size.Width;
            totalHeight += child.Size.Height;
        }

        if (Direction == Axis.Horizontal)
        {
            double equalWidth = Expanded && Constraints.HasBoundedWidth
                ? Constraints.MaxWidth / ChildCount
                : double.NaN;
            foreach (var item in measured)
            {
                double width = double.IsNaN(equalWidth) ? item.Size.Width : equalWidth;
                item.Child.Layout(BoxConstraints.Tight(new Size(width, maxHeight)), parentUsesSize: true);
            }
            totalWidth = double.IsNaN(equalWidth) ? totalWidth : Constraints.MaxWidth;
            Size = Constraints.Constrain(new Size(totalWidth, maxHeight));
            PositionHorizontal(measured);
        }
        else
        {
            double equalHeight = Expanded && Constraints.HasBoundedHeight
                ? Constraints.MaxHeight / ChildCount
                : double.NaN;
            foreach (var item in measured)
            {
                double height = double.IsNaN(equalHeight) ? item.Size.Height : equalHeight;
                item.Child.Layout(BoxConstraints.Tight(new Size(maxWidth, height)), parentUsesSize: true);
            }
            totalHeight = double.IsNaN(equalHeight) ? totalHeight : Constraints.MaxHeight;
            Size = Constraints.Constrain(new Size(maxWidth, totalHeight));
            PositionVertical(measured);
        }
    }

    private void PositionHorizontal(IReadOnlyList<(RenderBox Child, Size Size)> measured)
    {
        if (TextDirection == TextDirection.Ltr)
        {
            double x = 0.0;
            foreach (var item in measured)
            {
                ((SegmentedControlParentData)item.Child.parentData!).offset = new Point(
                    x,
                    (Size.Height - item.Child.Size.Height) / 2);
                x += item.Child.Size.Width;
            }
            return;
        }

        double right = Size.Width;
        foreach (var item in measured)
        {
            right -= item.Child.Size.Width;
            ((SegmentedControlParentData)item.Child.parentData!).offset = new Point(
                right,
                (Size.Height - item.Child.Size.Height) / 2);
        }
    }

    private void PositionVertical(IReadOnlyList<(RenderBox Child, Size Size)> measured)
    {
        if (VerticalDirection == VerticalDirection.Down)
        {
            double y = 0.0;
            foreach (var item in measured)
            {
                ((SegmentedControlParentData)item.Child.parentData!).offset = new Point(
                    (Size.Width - item.Child.Size.Width) / 2,
                    y);
                y += item.Child.Size.Height;
            }
            return;
        }

        double bottom = Size.Height;
        foreach (var item in measured)
        {
            bottom -= item.Child.Size.Height;
            ((SegmentedControlParentData)item.Child.parentData!).offset = new Point(
                (Size.Width - item.Child.Size.Width) / 2,
                bottom);
        }
    }

    public override void Paint(PaintingContext ctx, Point offset) => DefaultPaint(ctx, offset);

    protected override bool HitTestChildren(BoxHitTestResult result, Point position) =>
        DefaultHitTestChildren(result, position);

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
            var data = (SegmentedControlParentData)child.parentData!;
            visitor(child, data.offset, Matrix.Identity);
        }
    }

    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);
    public void Remove(RenderBox child) => _container.Remove(child);
    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) => Insert((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) => Move((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);
}
