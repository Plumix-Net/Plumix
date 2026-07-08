using Avalonia;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/viewport.dart (PageView viewport subset)

public sealed class PageViewportParentData : ContainerBoxParentData<RenderBox>;

internal sealed class RenderPageViewport : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, PageViewportParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, PageViewportParentData> _container;
    private PageController _controller;
    private double _page;
    private Axis _axis;
    private bool _reverse;
    private Clip _clipBehavior;

    public RenderPageViewport(PageController controller, double page, Axis axis, bool reverse, Clip clipBehavior)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, PageViewportParentData>(this);
        _controller = controller;
        _page = page;
        _axis = axis;
        _reverse = reverse;
        _clipBehavior = clipBehavior;
    }

    public PageController Controller
    {
        get => _controller;
        set
        {
            if (ReferenceEquals(_controller, value)) return;
            _controller.DetachViewport();
            _controller = value;
            MarkNeedsLayout();
        }
    }

    public Axis Axis { get => _axis; set { if (_axis != value) { _axis = value; MarkNeedsLayout(); } } }
    public double Page
    {
        get => _page;
        set
        {
            if (Math.Abs(_page - value) <= 0.000001) return;
            _page = value;
            MarkNeedsLayout();
        }
    }
    public bool Reverse { get => _reverse; set { if (_reverse != value) { _reverse = value; MarkNeedsLayout(); } } }
    public Clip ClipBehavior { get => _clipBehavior; set { if (_clipBehavior != value) { _clipBehavior = value; MarkNeedsPaint(); } } }

    public int ChildCount => _container.ChildCount;
    public RenderBox? FirstChild => _container.FirstChild;
    public RenderBox? LastChild => _container.LastChild;
    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);
    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);
    public void AddAll(List<RenderBox> children) => _container.AddAll(children);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not PageViewportParentData) child.parentData = new PageViewportParentData();
    }

    protected override void PerformLayout()
    {
        var fallback = Constraints.Constrain(new Size(
            Constraints.HasBoundedWidth ? Constraints.MaxWidth : 0,
            Constraints.HasBoundedHeight ? Constraints.MaxHeight : 0));
        Size = fallback;
        var mainExtent = Axis == Axis.Horizontal ? Size.Width : Size.Height;
        var pageExtent = mainExtent * Controller.ViewportFraction;
        var crossExtent = Axis == Axis.Horizontal ? Size.Height : Size.Width;
        var sidePadding = (mainExtent - pageExtent) / 2;
        Controller.AttachViewport(mainExtent, ChildCount);

        var index = 0;
        for (var child = FirstChild; child is not null; child = ChildAfter(child), index++)
        {
            var childConstraints = Axis == Axis.Horizontal
                ? BoxConstraints.Tight(new Size(pageExtent, crossExtent))
                : BoxConstraints.Tight(new Size(crossExtent, pageExtent));
            child.Layout(childConstraints, parentUsesSize: true);
            var logicalOffset = (index - Page) * pageExtent;
            if (Reverse) logicalOffset = -logicalOffset;
            ((PageViewportParentData)child.parentData!).offset = Axis == Axis.Horizontal
                ? new Point(sidePadding + logicalOffset, 0)
                : new Point(0, sidePadding + logicalOffset);
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        void PaintChildren(PaintingContext paintingContext)
        {
            var bounds = new Rect(default, Size);
            for (var child = FirstChild; child is not null; child = ChildAfter(child))
            {
                var childOffset = ((PageViewportParentData)child.parentData!).offset;
                var childRect = new Rect(childOffset, child.Size);
                if (bounds.Intersect(childRect).Width <= 0 || bounds.Intersect(childRect).Height <= 0) continue;
                paintingContext.PaintChild(child, offset + childOffset);
            }
        }

        if (ClipBehavior == Clip.None) PaintChildren(context);
        else context.PushClipRect(new Rect(offset, Size), PaintChildren);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position) =>
        _container.DefaultHitTestChildren(result, position);

    public void DefaultPaint(PaintingContext context, Point offset) => _container.DefaultPaint(context, offset);

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) =>
        _container.DefaultHitTestChildren(result, position);

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (var child = FirstChild; child is not null; child = ChildAfter(child)) visitor(child);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        var selected = (int)Math.Round(Page);
        var index = 0;
        for (var child = FirstChild; child is not null; child = ChildAfter(child), index++)
        {
            if (index != selected) continue;
            visitor(child, ((PageViewportParentData)child.parentData!).offset, Matrix.Identity);
        }
    }

    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);
    public void Remove(RenderBox child) => _container.Remove(child);
    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) => Insert((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) => Move((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);
}
