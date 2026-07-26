using Avalonia;
using Plumix.UI;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/overlay.dart (_RenderTheater)

internal sealed class OverlayTheaterParentData : StackParentData
{
    public bool CanSizeOverlay { get; set; }

    public bool IsOnstage { get; set; } = true;
}

internal sealed class RenderOverlayTheater : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, OverlayTheaterParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, OverlayTheaterParentData> _container;
    private Alignment _alignment;
    private Clip _clipBehavior;
    private bool _alwaysSizeToContent;
    private bool _hasVisualOverflow;

    public RenderOverlayTheater(
        Alignment alignment,
        Clip clipBehavior,
        bool alwaysSizeToContent)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, OverlayTheaterParentData>(this);
        _alignment = alignment;
        _clipBehavior = clipBehavior;
        _alwaysSizeToContent = alwaysSizeToContent;
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

    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (_clipBehavior == value)
            {
                return;
            }

            _clipBehavior = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    public bool AlwaysSizeToContent
    {
        get => _alwaysSizeToContent;
        set
        {
            if (_alwaysSizeToContent == value)
            {
                return;
            }

            _alwaysSizeToContent = value;
            MarkNeedsLayout();
        }
    }

    public int ChildCount => _container.ChildCount;

    public RenderBox? FirstChild => _container.FirstChild;

    public RenderBox? LastChild => _container.LastChild;

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not OverlayTheaterParentData)
        {
            child.parentData = new OverlayTheaterParentData();
        }
    }

    protected override void PerformLayout()
    {
        BoxConstraints constraints = Constraints;
        RenderBox? sizeDeterminingChild = null;
        bool finiteBiggest = double.IsFinite(constraints.MaxWidth)
                             && double.IsFinite(constraints.MaxHeight);

        if (!_alwaysSizeToContent && finiteBiggest)
        {
            Size = constraints.Biggest;
        }
        else
        {
            sizeDeterminingChild = FindSizeDeterminingChild();
            sizeDeterminingChild.Layout(constraints, parentUsesSize: true);
            Size = constraints.Constrain(sizeDeterminingChild.Size);
        }

        bool hadVisualOverflow = _hasVisualOverflow;
        _hasVisualOverflow = false;
        BoxConstraints nonPositionedConstraints = BoxConstraints.Tight(Size);

        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            var parentData = (OverlayTheaterParentData)child.parentData!;
            if (!parentData.IsOnstage)
            {
                continue;
            }

            if (!parentData.IsPositioned)
            {
                if (!ReferenceEquals(child, sizeDeterminingChild))
                {
                    child.Layout(nonPositionedConstraints, parentUsesSize: true);
                }

                parentData.offset = _alignment.AlongOffset(Size, child.Size);
            }
            else
            {
                LayoutPositionedChild(child, parentData);
            }

            _hasVisualOverflow |= ChildOverflows(child, parentData.offset);
        }

        if (hadVisualOverflow != _hasVisualOverflow)
        {
            MarkNeedsSemanticsUpdate();
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (_clipBehavior == Clip.None)
        {
            PaintOnstageChildren(context, offset);
            return;
        }

        context.PushClipRect(
            new Rect(offset, Size),
            clippedContext => PaintOnstageChildren(clippedContext, offset));
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        for (RenderBox? child = LastChild; child is not null; child = ChildBefore(child))
        {
            var parentData = (OverlayTheaterParentData)child.parentData!;
            if (!parentData.IsOnstage)
            {
                continue;
            }

            Point transformedPosition = position - parentData.offset;
            if (child.HitTest(result, transformedPosition))
            {
                return true;
            }
        }

        return false;
    }

    protected override Rect? DescribeApproximatePaintClip(RenderObject? child)
    {
        return _clipBehavior == Clip.None
            ? null
            : new Rect(new Point(), Size);
    }

    protected override Rect? DescribeSemanticsClip(RenderObject? child)
    {
        return DescribeApproximatePaintClip(child);
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            visitor(child);
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            var parentData = (OverlayTheaterParentData)child.parentData!;
            if (parentData.IsOnstage)
            {
                visitor(child, parentData.offset, Matrix.Identity);
            }
        }
    }

    public void AddAll(List<RenderBox> children) => _container.AddAll(children);

    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);

    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);

    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);

    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);

    public void Remove(RenderBox child) => _container.Remove(child);

    public void DefaultPaint(PaintingContext context, Point offset)
    {
        _container.DefaultPaint(context, offset);
    }

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position)
    {
        return _container.DefaultHitTestChildren(result, position);
    }

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

    private RenderBox FindSizeDeterminingChild()
    {
        for (RenderBox? child = LastChild; child is not null; child = ChildBefore(child))
        {
            var parentData = (OverlayTheaterParentData)child.parentData!;
            if (parentData.IsOnstage
                && parentData.CanSizeOverlay
                && !parentData.IsPositioned)
            {
                return child;
            }
        }

        string reason = _alwaysSizeToContent
            ? "Overlay.AlwaysSizeToContent requires a non-positioned onstage entry with CanSizeOverlay=true."
            : "An unbounded Overlay requires a non-positioned onstage entry with CanSizeOverlay=true.";
        throw new InvalidOperationException(reason);
    }

    private void PaintOnstageChildren(PaintingContext context, Point offset)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            var parentData = (OverlayTheaterParentData)child.parentData!;
            if (parentData.IsOnstage)
            {
                context.PaintChild(child, parentData.offset + offset);
            }
        }
    }

    private void LayoutPositionedChild(RenderBox child, OverlayTheaterParentData parentData)
    {
        double? childWidth = ComputeChildExtent(
            parentData.Left,
            parentData.Right,
            parentData.Width,
            Size.Width);
        double? childHeight = ComputeChildExtent(
            parentData.Top,
            parentData.Bottom,
            parentData.Height,
            Size.Height);
        var childConstraints = new BoxConstraints(
            MinWidth: childWidth ?? 0.0,
            MaxWidth: childWidth ?? Size.Width,
            MinHeight: childHeight ?? 0.0,
            MaxHeight: childHeight ?? Size.Height);
        child.Layout(childConstraints, parentUsesSize: true);

        Point alignedOffset = _alignment.AlongOffset(Size, child.Size);
        double x = parentData.Left
                   ?? (parentData.Right.HasValue
                       ? Size.Width - parentData.Right.Value - child.Size.Width
                       : alignedOffset.X);
        double y = parentData.Top
                   ?? (parentData.Bottom.HasValue
                       ? Size.Height - parentData.Bottom.Value - child.Size.Height
                       : alignedOffset.Y);
        parentData.offset = new Point(x, y);
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

        return extent.HasValue ? Math.Max(0.0, extent.Value) : null;
    }

    private bool ChildOverflows(RenderBox child, Point offset)
    {
        return offset.X < 0.0
               || offset.Y < 0.0
               || offset.X + child.Size.Width > Size.Width
               || offset.Y + child.Size.Height > Size.Height;
    }
}
