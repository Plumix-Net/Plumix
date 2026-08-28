using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/overflow_bar.dart

public enum OverflowBarAlignment
{
    Start,
    End,
    Center,
}

public sealed class OverflowBar : MultiChildRenderObjectWidget
{
    public OverflowBar(
        IReadOnlyList<Widget>? children = null,
        double spacing = 0,
        MainAxisAlignment? alignment = null,
        double overflowSpacing = 0,
        OverflowBarAlignment overflowAlignment = OverflowBarAlignment.Start,
        VerticalDirection overflowDirection = VerticalDirection.Down,
        TextDirection? textDirection = null,
        Key? key = null) : base(children, key)
    {
        if (!double.IsFinite(spacing) || spacing < 0) throw new ArgumentOutOfRangeException(nameof(spacing));
        if (!double.IsFinite(overflowSpacing) || overflowSpacing < 0) throw new ArgumentOutOfRangeException(nameof(overflowSpacing));
        Spacing = spacing;
        Alignment = alignment;
        OverflowSpacing = overflowSpacing;
        OverflowAlignment = overflowAlignment;
        OverflowDirection = overflowDirection;
        TextDirection = textDirection;
    }

    public double Spacing { get; }
    public MainAxisAlignment? Alignment { get; }
    public double OverflowSpacing { get; }
    public OverflowBarAlignment OverflowAlignment { get; }
    public VerticalDirection OverflowDirection { get; }
    public TextDirection? TextDirection { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderOverflowBar(
            spacing: Spacing,
            alignment: Alignment,
            overflowSpacing: OverflowSpacing,
            overflowAlignment: OverflowAlignment,
            overflowDirection: OverflowDirection,
            textDirection: TextDirection ?? Directionality.Of(context));
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var bar = (RenderOverflowBar)renderObject;
        bar.Spacing = Spacing;
        bar.Alignment = Alignment;
        bar.OverflowSpacing = OverflowSpacing;
        bar.OverflowAlignment = OverflowAlignment;
        bar.OverflowDirection = OverflowDirection;
        bar.TextDirection = TextDirection ?? Directionality.Of(context);
    }
}

public sealed class OverflowBarParentData : ContainerBoxParentData<RenderBox>;

public sealed class RenderOverflowBar : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, OverflowBarParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, OverflowBarParentData> _container;
    private double _spacing;
    private MainAxisAlignment? _alignment;
    private double _overflowSpacing;
    private OverflowBarAlignment _overflowAlignment;
    private VerticalDirection _overflowDirection;
    private TextDirection _textDirection;

    public RenderOverflowBar(
        double spacing,
        MainAxisAlignment? alignment,
        double overflowSpacing,
        OverflowBarAlignment overflowAlignment,
        VerticalDirection overflowDirection,
        TextDirection textDirection)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, OverflowBarParentData>(this);
        _spacing = spacing;
        _alignment = alignment;
        _overflowSpacing = overflowSpacing;
        _overflowAlignment = overflowAlignment;
        _overflowDirection = overflowDirection;
        _textDirection = textDirection;
    }

    public double Spacing { get => _spacing; set { if (_spacing != value) { _spacing = value; MarkNeedsLayout(); } } }
    public MainAxisAlignment? Alignment { get => _alignment; set { if (_alignment != value) { _alignment = value; MarkNeedsLayout(); } } }
    public double OverflowSpacing { get => _overflowSpacing; set { if (_overflowSpacing != value) { _overflowSpacing = value; MarkNeedsLayout(); } } }
    public OverflowBarAlignment OverflowAlignment { get => _overflowAlignment; set { if (_overflowAlignment != value) { _overflowAlignment = value; MarkNeedsLayout(); } } }
    public VerticalDirection OverflowDirection { get => _overflowDirection; set { if (_overflowDirection != value) { _overflowDirection = value; MarkNeedsLayout(); } } }
    public TextDirection TextDirection { get => _textDirection; set { if (_textDirection != value) { _textDirection = value; MarkNeedsLayout(); } } }

    public int ChildCount => _container.ChildCount;
    public RenderBox? FirstChild => _container.FirstChild;
    public RenderBox? LastChild => _container.LastChild;
    public void AddAll(List<RenderBox> children) => _container.AddAll(children);
    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);
    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not OverflowBarParentData) child.parentData = new OverflowBarParentData();
    }

    protected override double ComputeMinIntrinsicHeight(double width) =>
        ComputeIntrinsicHeight(width, minimum: true);

    protected override double ComputeMaxIntrinsicHeight(double width) =>
        ComputeIntrinsicHeight(width, minimum: false);

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        if (FirstChild is null)
        {
            return 0.0;
        }

        double width = 0.0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            width += child.GetMinIntrinsicWidth(double.PositiveInfinity);
        }

        return width + (Spacing * (ChildCount - 1));
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        if (FirstChild is null)
        {
            return 0.0;
        }

        double width = 0.0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            width += child.GetMaxIntrinsicWidth(double.PositiveInfinity);
        }

        return width + (Spacing * (ChildCount - 1));
    }

    /// <summary>
    /// The children stack vertically once their minimum widths no longer fit, so the intrinsic height is the
    /// sum of the child heights in that case and the tallest child otherwise.
    /// </summary>
    private double ComputeIntrinsicHeight(double width, bool minimum)
    {
        if (FirstChild is null)
        {
            return 0.0;
        }

        double barWidth = 0.0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            barWidth += child.GetMinIntrinsicWidth(double.PositiveInfinity);
        }

        barWidth += Spacing * (ChildCount - 1);

        double height = 0.0;
        if (barWidth > width)
        {
            for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
            {
                height += minimum
                    ? child.GetMinIntrinsicHeight(width)
                    : child.GetMaxIntrinsicHeight(width);
            }

            return height + (OverflowSpacing * (ChildCount - 1));
        }

        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            height = Math.Max(
                height,
                minimum ? child.GetMinIntrinsicHeight(width) : child.GetMaxIntrinsicHeight(width));
        }

        return height;
    }

    protected override void PerformLayout()
    {
        if (ChildCount == 0)
        {
            Size = Constraints.Smallest;
            return;
        }

        var children = new List<RenderBox>(ChildCount);
        var childConstraints = new BoxConstraints(MaxWidth: Constraints.MaxWidth, MaxHeight: Constraints.MaxHeight);
        double totalWidth = 0.0;
        double maxHeight = 0.0;
        double totalHeight = 0.0;
        for (var child = FirstChild; child is not null; child = ChildAfter(child))
        {
            child.Layout(childConstraints, parentUsesSize: true);
            children.Add(child);
            totalWidth += child.Size.Width;
            totalHeight += child.Size.Height;
            maxHeight = Math.Max(maxHeight, child.Size.Height);
        }
        totalWidth += Spacing * Math.Max(0, children.Count - 1);

        if (!Constraints.HasBoundedWidth || totalWidth <= Constraints.MaxWidth)
        {
            double overallWidth = Alignment.HasValue && Constraints.HasBoundedWidth
                ? Constraints.MaxWidth
                : totalWidth;
            Size = Constraints.Constrain(new Size(overallWidth, maxHeight));
            PositionHorizontal(children);
            return;
        }

        totalHeight += OverflowSpacing * Math.Max(0, children.Count - 1);
        Size = Constraints.Constrain(new Size(Constraints.MaxWidth, totalHeight));
        PositionOverflow(children);
    }

    private void PositionHorizontal(IReadOnlyList<RenderBox> children)
    {
        double childrenWidth = children.Sum(child => child.Size.Width);
        double actualWidth = childrenWidth + Spacing * Math.Max(0, children.Count - 1);
        double layoutSpacing = Spacing;
        double firstWidth = children[0].Size.Width;
        bool rtl = TextDirection == TextDirection.Rtl;
        double x = Alignment switch
        {
            MainAxisAlignment.Center => rtl
                ? Size.Width - ((Size.Width - actualWidth) / 2) - firstWidth
                : (Size.Width - actualWidth) / 2,
            MainAxisAlignment.End => rtl ? actualWidth - firstWidth : Size.Width - actualWidth,
            MainAxisAlignment.SpaceAround => rtl
                ? Size.Width - ((Size.Width - childrenWidth) / children.Count / 2) - firstWidth
                : (Size.Width - childrenWidth) / children.Count / 2,
            MainAxisAlignment.SpaceEvenly => rtl
                ? Size.Width - ((Size.Width - childrenWidth) / (children.Count + 1)) - firstWidth
                : (Size.Width - childrenWidth) / (children.Count + 1),
            _ => rtl ? Size.Width - firstWidth : 0,
        };
        if (Alignment == MainAxisAlignment.SpaceBetween && children.Count > 1)
        {
            layoutSpacing = (Size.Width - childrenWidth) / (children.Count - 1);
        }
        else if (Alignment == MainAxisAlignment.SpaceAround)
        {
            layoutSpacing = (Size.Width - childrenWidth) / children.Count;
        }
        else if (Alignment == MainAxisAlignment.SpaceEvenly)
        {
            layoutSpacing = (Size.Width - childrenWidth) / (children.Count + 1);
        }
        for (int index = 0; index < children.Count; index++)
        {
            var child = children[index];
            ((OverflowBarParentData)child.parentData!).offset = new Point(
                x,
                (Size.Height - child.Size.Height) / 2);
            if (!rtl)
            {
                x += child.Size.Width + layoutSpacing;
            }
            else
            {
                int nextIndex = index + 1;
                if (nextIndex < children.Count)
                {
                    x -= children[nextIndex].Size.Width + layoutSpacing;
                }
            }
        }
    }

    private void PositionOverflow(IReadOnlyList<RenderBox> children)
    {
        double y = OverflowDirection == VerticalDirection.Down ? 0.0 : Size.Height;
        foreach (var child in children)
        {
            if (OverflowDirection == VerticalDirection.Up) y -= child.Size.Height;
            double x = ResolveOverflowX(child.Size.Width);
            ((OverflowBarParentData)child.parentData!).offset = new Point(x, y);
            y += OverflowDirection == VerticalDirection.Down
                ? child.Size.Height + OverflowSpacing
                : -OverflowSpacing;
        }
    }

    private double ResolveOverflowX(double childWidth)
    {
        if (OverflowAlignment == OverflowBarAlignment.Center) return (Size.Width - childWidth) / 2;
        double start = TextDirection == TextDirection.Ltr ? 0.0 : Size.Width - childWidth;
        double end = TextDirection == TextDirection.Ltr ? Size.Width - childWidth : 0.0;
        return OverflowAlignment == OverflowBarAlignment.Start ? start : end;
    }

    public override void Paint(PaintingContext ctx, Point offset) => DefaultPaint(ctx, offset);
    protected override bool HitTestChildren(BoxHitTestResult result, Point position) => DefaultHitTestChildren(result, position);
    public void DefaultPaint(PaintingContext ctx, Point offset) => _container.DefaultPaint(ctx, offset);
    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) => _container.DefaultHitTestChildren(result, position);
    public override void VisitChildren(Action<RenderObject> visitor) { for (var child = FirstChild; child is not null; child = ChildAfter(child)) visitor(child); }
    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        for (var child = FirstChild; child is not null; child = ChildAfter(child)) { visitor(child); }
    }
    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);
    public void Remove(RenderBox child) => _container.Remove(child);
    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) => Insert((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) => Move((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => _container.DebugDescribeChildren();
}
