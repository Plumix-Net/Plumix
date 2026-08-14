using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Path = Plumix.UI.Path;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/segmented_button.dart

internal sealed class SegmentedButtonRenderWidget : MultiChildRenderObjectWidget
{
    public SegmentedButtonRenderWidget(
        IReadOnlyList<Widget> children,
        IReadOnlyList<bool> segmentEnabled,
        OutlinedBorder enabledBorder,
        OutlinedBorder disabledBorder,
        Axis direction,
        TextDirection textDirection,
        bool expanded,
        double tapTargetVerticalPadding,
        Key? key = null) : base(children, key)
    {
        if (segmentEnabled.Count != children.Count)
        {
            throw new ArgumentException("Every segmented-button child needs an enabled state.", nameof(segmentEnabled));
        }

        SegmentEnabled = segmentEnabled;
        EnabledBorder = enabledBorder;
        DisabledBorder = disabledBorder;
        Direction = direction;
        TextDirection = textDirection;
        Expanded = expanded;
        TapTargetVerticalPadding = tapTargetVerticalPadding;
    }

    public IReadOnlyList<bool> SegmentEnabled { get; }
    public OutlinedBorder EnabledBorder { get; }
    public OutlinedBorder DisabledBorder { get; }
    public Axis Direction { get; }
    public TextDirection TextDirection { get; }
    public bool Expanded { get; }
    public double TapTargetVerticalPadding { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSegmentedButton(
            segmentEnabled: SegmentEnabled,
            enabledBorder: EnabledBorder,
            disabledBorder: DisabledBorder,
            direction: Direction,
            textDirection: TextDirection,
            expanded: Expanded,
            tapTargetVerticalPadding: TapTargetVerticalPadding);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var segmentedButton = (RenderSegmentedButton)renderObject;
        segmentedButton.SegmentEnabled = SegmentEnabled;
        segmentedButton.EnabledBorder = EnabledBorder;
        segmentedButton.DisabledBorder = DisabledBorder;
        segmentedButton.Direction = Direction;
        segmentedButton.TextDirection = TextDirection;
        segmentedButton.Expanded = Expanded;
        segmentedButton.TapTargetVerticalPadding = TapTargetVerticalPadding;
    }
}

internal sealed class SegmentedButtonParentData : ContainerBoxParentData<RenderBox>
{
    public Rect SurroundingRect { get; set; }
}

internal sealed class RenderSegmentedButton : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, SegmentedButtonParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, SegmentedButtonParentData> _children;
    private IReadOnlyList<bool> _segmentEnabled;
    private OutlinedBorder _enabledBorder;
    private OutlinedBorder _disabledBorder;
    private Axis _direction;
    private TextDirection _textDirection;
    private bool _expanded;
    private double _tapTargetVerticalPadding;

    public RenderSegmentedButton(
        IReadOnlyList<bool> segmentEnabled,
        OutlinedBorder enabledBorder,
        OutlinedBorder disabledBorder,
        Axis direction,
        TextDirection textDirection,
        bool expanded,
        double tapTargetVerticalPadding)
    {
        _children = new RenderBoxContainerDefaultsMixin<RenderBox, SegmentedButtonParentData>(this);
        _segmentEnabled = segmentEnabled;
        _enabledBorder = enabledBorder;
        _disabledBorder = disabledBorder;
        _direction = direction;
        _textDirection = textDirection;
        _expanded = expanded;
        _tapTargetVerticalPadding = tapTargetVerticalPadding;
    }

    public IReadOnlyList<bool> SegmentEnabled
    {
        get => _segmentEnabled;
        set
        {
            if (_segmentEnabled.SequenceEqual(value))
            {
                return;
            }
            _segmentEnabled = value;
            MarkNeedsPaint();
        }
    }

    public OutlinedBorder EnabledBorder
    {
        get => _enabledBorder;
        set
        {
            if (Equals(_enabledBorder, value))
            {
                return;
            }
            _enabledBorder = value;
            MarkNeedsPaint();
        }
    }

    public OutlinedBorder DisabledBorder
    {
        get => _disabledBorder;
        set
        {
            if (Equals(_disabledBorder, value))
            {
                return;
            }
            _disabledBorder = value;
            MarkNeedsPaint();
        }
    }

    public Axis Direction
    {
        get => _direction;
        set
        {
            if (_direction == value)
            {
                return;
            }
            _direction = value;
            MarkNeedsLayout();
        }
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set
        {
            if (_textDirection == value)
            {
                return;
            }
            _textDirection = value;
            MarkNeedsLayout();
        }
    }

    public bool Expanded
    {
        get => _expanded;
        set
        {
            if (_expanded == value)
            {
                return;
            }
            _expanded = value;
            MarkNeedsLayout();
        }
    }

    public double TapTargetVerticalPadding
    {
        get => _tapTargetVerticalPadding;
        set
        {
            if (_tapTargetVerticalPadding == value)
            {
                return;
            }
            _tapTargetVerticalPadding = value;
            MarkNeedsPaint();
        }
    }

    public int ChildCount => _children.ChildCount;
    public RenderBox? FirstChild => _children.FirstChild;
    public RenderBox? LastChild => _children.LastChild;

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not SegmentedButtonParentData)
        {
            child.parentData = new SegmentedButtonParentData();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        return ChildCount * MaximumIntrinsic(static (child, extent) => child.GetMinIntrinsicWidth(extent), height);
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        return ChildCount * MaximumIntrinsic(static (child, extent) => child.GetMaxIntrinsicWidth(extent), height);
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        return MaximumIntrinsic(static (child, extent) => child.GetMinIntrinsicHeight(extent), width);
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        return MaximumIntrinsic(static (child, extent) => child.GetMaxIntrinsicHeight(extent), width);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        if (ChildCount == 0)
        {
            return constraints.Smallest;
        }

        Size childSize = CalculateChildSize(constraints);
        return OverallSize(constraints, childSize);
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        if (ChildCount == 0)
        {
            return null;
        }

        Size childSize = CalculateChildSize(constraints);
        BoxConstraints childConstraints = BoxConstraints.Tight(childSize);
        double? result = null;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            double? childBaseline = child.GetDryBaseline(childConstraints, baseline);
            if (childBaseline.HasValue)
            {
                result = !result.HasValue ? childBaseline : Math.Min(result.Value, childBaseline.Value);
            }
        }
        return result;
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        double? result = null;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            double? childBaseline = child.GetDistanceToBaseline(baseline, onlyReal: true);
            if (!childBaseline.HasValue)
            {
                continue;
            }

            var data = (SegmentedButtonParentData)child.parentData!;
            double positionedBaseline = data.offset.Y + childBaseline.Value;
            result = !result.HasValue ? positionedBaseline : Math.Min(result.Value, positionedBaseline);
        }
        return result;
    }

    protected override void PerformLayout()
    {
        if (ChildCount == 0)
        {
            Size = Constraints.Smallest;
            return;
        }

        Size childSize = CalculateChildSize(Constraints);
        BoxConstraints childConstraints = BoxConstraints.Tight(childSize);
        foreach (RenderBox child in Children())
        {
            child.Layout(childConstraints, parentUsesSize: true);
        }

        Size = OverallSize(Constraints, childSize);
        double position = 0.0;
        IEnumerable<RenderBox> positionedChildren = TextDirection == TextDirection.Ltr
            ? Children()
            : Children().Reverse();
        foreach (RenderBox child in positionedChildren)
        {
            var data = (SegmentedButtonParentData)child.parentData!;
            data.offset = Direction == Axis.Horizontal
                ? new Point(position, 0.0)
                : new Point(0.0, position);
            data.SurroundingRect = new Rect(data.offset, childSize);
            position += Direction == Axis.Horizontal ? childSize.Width : childSize.Height;
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (ChildCount == 0 || Size.Width <= 0.0 || Size.Height <= 0.0)
        {
            return;
        }

        Rect borderRect = LocalBorderRect;
        Path innerPath = EnabledBorder.GetInnerPath(borderRect, TextDirection);
        foreach (RenderBox child in Children())
        {
            var data = (SegmentedButtonParentData)child.parentData!;
            context.PushClipPath(
                innerPath,
                clipped => clipped.PaintChild(child, offset + (Vector)data.offset),
                geometryOffset: offset);
        }

        PaintDividers(context, offset, borderRect);
        PaintOuterBorder(context, offset, borderRect);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        for (RenderBox? child = LastChild; child is not null; child = ChildBefore(child))
        {
            var data = (SegmentedButtonParentData)child.parentData!;
            if (!data.SurroundingRect.Contains(position))
            {
                continue;
            }
            if (child.HitTest(result, position - (Vector)data.offset))
            {
                return true;
            }
        }
        return false;
    }

    public void AddAll(List<RenderBox> children) => _children.AddAll(children);
    public RenderBox? ChildBefore(RenderBox child) => _children.ChildBefore(child);
    public RenderBox? ChildAfter(RenderBox child) => _children.ChildAfter(child);
    public void DefaultPaint(PaintingContext context, Point offset) => _children.DefaultPaint(context, offset);
    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) =>
        _children.DefaultHitTestChildren(result, position);

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        foreach (RenderBox child in Children())
        {
            visitor(child);
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        foreach (RenderBox child in Children())
        {
            var data = (SegmentedButtonParentData)child.parentData!;
            visitor(child);
        }
    }

    public void Insert(RenderBox child, RenderBox? after = null) => _children.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _children.Move(child, after);
    public void Remove(RenderBox child) => _children.Remove(child);
    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) =>
        Insert((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) =>
        Move((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);

    private Rect LocalBorderRect => new(
        new Point(0.0, TapTargetVerticalPadding / 2.0),
        new Size(Size.Width, Math.Max(0.0, Size.Height - TapTargetVerticalPadding)));

    private double MaximumIntrinsic(Func<RenderBox, double, double> getter, double extent)
    {
        double result = 0.0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            result = Math.Max(result, getter(child, extent));
        }
        return result;
    }

    private Size CalculateChildSize(BoxConstraints constraints)
    {
        int count = Math.Max(ChildCount, 1);
        if (Direction == Axis.Horizontal)
        {
            double maximumWidth = constraints.MaxWidth / count;
            double childWidth = Expanded
                ? maximumWidth
                : Math.Max(
                    constraints.MinWidth / count,
                    MaximumIntrinsic(
                        static (child, extent) => child.GetMaxIntrinsicWidth(extent),
                        double.PositiveInfinity));
            childWidth = Math.Min(childWidth, maximumWidth);
            double childHeight = MaximumIntrinsic(
                static (child, extent) => child.GetMaxIntrinsicHeight(extent),
                childWidth);
            return new Size(childWidth, childHeight);
        }

        double maximumHeight = constraints.MaxHeight / count;
        double verticalChildHeight = Expanded
            ? maximumHeight
            : Math.Max(
                constraints.MinHeight / count,
                MaximumIntrinsic(
                    static (child, extent) => child.GetMaxIntrinsicHeight(extent),
                    double.PositiveInfinity));
        verticalChildHeight = Math.Min(verticalChildHeight, maximumHeight);
        double verticalChildWidth = 0.0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            verticalChildWidth = Math.Max(
                verticalChildWidth,
                child.GetMaxIntrinsicWidth(verticalChildWidth));
        }
        if (constraints.HasTightWidth && verticalChildWidth < constraints.MaxWidth)
        {
            verticalChildWidth = constraints.MaxWidth;
        }
        return new Size(verticalChildWidth, verticalChildHeight);
    }

    private Size OverallSize(BoxConstraints constraints, Size childSize)
    {
        return constraints.Constrain(Direction == Axis.Horizontal
            ? new Size(childSize.Width * ChildCount, childSize.Height)
            : new Size(childSize.Width, childSize.Height * ChildCount));
    }

    private IEnumerable<RenderBox> Children()
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            yield return child;
        }
    }

    private void PaintDividers(PaintingContext context, Point offset, Rect borderRect)
    {
        List<RenderBox> children = Children().ToList();
        for (int index = 0; index < children.Count - 1; index++)
        {
            bool enabled = SegmentEnabled[index] || SegmentEnabled[index + 1];
            BorderSide side = (enabled ? EnabledBorder.Side : DisabledBorder.Side)
                .CopyWith(strokeAlign: BorderSide.StrokeAlignCenter);
            if (side.Style == BorderStyle.None)
            {
                continue;
            }

            var data = (SegmentedButtonParentData)children[index].parentData!;
            var pen = new Pen(new SolidColorBrush(side.Color), side.Width);
            if (Direction == Axis.Horizontal)
            {
                double x = TextDirection == TextDirection.Ltr
                    ? data.SurroundingRect.Right
                    : data.SurroundingRect.Left;
                context.DrawLine(
                    pen,
                    offset + new Vector(x, borderRect.Top),
                    offset + new Vector(x, borderRect.Bottom));
                continue;
            }

            double y = TextDirection == TextDirection.Ltr
                ? data.SurroundingRect.Bottom
                : data.SurroundingRect.Top;
            Path clipPath = EnabledBorder.GetInnerPath(borderRect, TextDirection);
            context.PushClipPath(
                clipPath,
                clipped => clipped.DrawLine(
                    pen,
                    offset + new Vector(borderRect.Left, y),
                    offset + new Vector(borderRect.Right, y)),
                geometryOffset: offset);
        }
    }

    private void PaintOuterBorder(PaintingContext context, Point offset, Rect borderRect)
    {
        bool allEnabled = SegmentEnabled.All(static enabled => enabled);
        bool allDisabled = SegmentEnabled.All(static enabled => !enabled);
        Rect paintedRect = new(offset + (Vector)borderRect.Position, borderRect.Size);
        if (allEnabled)
        {
            EnabledBorder.Paint(context, paintedRect, TextDirection);
            return;
        }
        if (allDisabled)
        {
            DisabledBorder.Paint(context, paintedRect, TextDirection);
            return;
        }

        double outset = Math.Max(EnabledBorder.Side.StrokeOutset, DisabledBorder.Side.StrokeOutset);
        List<RenderBox> children = Children().ToList();
        for (int index = 0; index < children.Count; index++)
        {
            var data = (SegmentedButtonParentData)children[index].parentData!;
            Rect localClip = Direction == Axis.Horizontal
                ? new Rect(
                    data.SurroundingRect.Left,
                    borderRect.Top,
                    data.SurroundingRect.Width,
                    borderRect.Height)
                : new Rect(
                    borderRect.Left,
                    data.SurroundingRect.Top,
                    borderRect.Width,
                    data.SurroundingRect.Height);
            Rect clipRect = new Rect(offset + (Vector)localClip.Position, localClip.Size).Inflate(outset);
            OutlinedBorder border = SegmentEnabled[index] ? EnabledBorder : DisabledBorder;
            context.PushClipRect(clipRect, clipped => border.Paint(clipped, paintedRect, TextDirection));
        }
    }
}
