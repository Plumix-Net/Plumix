using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/custom_layout.dart

public sealed class MultiChildLayoutParentData : ContainerBoxParentData<RenderBox>
{
    public object? Id { get; set; }
}

/// <summary>Controls the size, constraints, and positions of a custom multi-child layout.</summary>
public abstract class MultiChildLayoutDelegate
{
    private Dictionary<object, RenderBox>? _idToChild;
    private HashSet<RenderBox>? _childrenNeedingLayout;

    protected MultiChildLayoutDelegate(IListenable? relayout = null)
    {
        Relayout = relayout;
    }

    internal IListenable? Relayout { get; }

    public bool HasChild(object childId)
    {
        ArgumentNullException.ThrowIfNull(childId);
        return RequireChildMap().ContainsKey(childId);
    }

    public Size LayoutChild(object childId, BoxConstraints constraints)
    {
        ArgumentNullException.ThrowIfNull(childId);
        if (!constraints.IsNormalized)
        {
            throw new InvalidOperationException(
                $"MultiChildLayoutDelegate returned non-normalized constraints for child '{childId}'.");
        }

        Dictionary<object, RenderBox> children = RequireChildMap();
        if (!children.TryGetValue(childId, out RenderBox? child))
        {
            throw new InvalidOperationException(
                $"MultiChildLayoutDelegate tried to lay out a non-existent child with id '{childId}'.");
        }

        HashSet<RenderBox> pendingChildren = RequirePendingChildren();
        if (!pendingChildren.Remove(child))
        {
            throw new InvalidOperationException(
                $"MultiChildLayoutDelegate tried to lay out child '{childId}' more than once.");
        }

        child.Layout(constraints, parentUsesSize: true);
        return child.Size;
    }

    public void PositionChild(object childId, Point offset)
    {
        ArgumentNullException.ThrowIfNull(childId);
        Dictionary<object, RenderBox> children = RequireChildMap();
        if (!children.TryGetValue(childId, out RenderBox? child))
        {
            throw new InvalidOperationException(
                $"MultiChildLayoutDelegate tried to position a non-existent child with id '{childId}'.");
        }

        ((MultiChildLayoutParentData)child.parentData!).offset = offset;
    }

    public virtual Size GetSize(BoxConstraints constraints) => constraints.Biggest;

    public abstract void PerformLayout(Size size);

    public abstract bool ShouldRelayout(MultiChildLayoutDelegate oldDelegate);

    internal void CallPerformLayout(Size size, RenderBox? firstChild)
    {
        Dictionary<object, RenderBox>? previousIdToChild = _idToChild;
        HashSet<RenderBox>? previousChildrenNeedingLayout = _childrenNeedingLayout;
        var children = new Dictionary<object, RenderBox>();
        var pendingChildren = new HashSet<RenderBox>();

        try
        {
            _idToChild = children;
            _childrenNeedingLayout = pendingChildren;
            for (RenderBox? child = firstChild; child is not null;)
            {
                var parentData = (MultiChildLayoutParentData)child.parentData!;
                object id = parentData.Id ?? throw new InvalidOperationException(
                    "Every child of CustomMultiChildLayout must be wrapped in LayoutId.");
                if (!children.TryAdd(id, child))
                {
                    throw new InvalidOperationException(
                        $"Every LayoutId in CustomMultiChildLayout must be unique; duplicate id '{id}'.");
                }

                pendingChildren.Add(child);
                child = parentData.nextSibling;
            }

            PerformLayout(size);
            if (pendingChildren.Count > 0)
            {
                string ids = string.Join(
                    ", ",
                    children.Where(entry => pendingChildren.Contains(entry.Value)).Select(entry => entry.Key));
                throw new InvalidOperationException(
                    $"MultiChildLayoutDelegate must lay out every child exactly once; missing: {ids}.");
            }
        }
        finally
        {
            _idToChild = previousIdToChild;
            _childrenNeedingLayout = previousChildrenNeedingLayout;
        }
    }

    private Dictionary<object, RenderBox> RequireChildMap()
    {
        return _idToChild ?? throw new InvalidOperationException(
            "MultiChildLayoutDelegate child APIs can only be used from PerformLayout.");
    }

    private HashSet<RenderBox> RequirePendingChildren()
    {
        return _childrenNeedingLayout ?? throw new InvalidOperationException(
            "MultiChildLayoutDelegate child APIs can only be used from PerformLayout.");
    }
}

/// <summary>Defers the layout of multiple children to a <see cref="MultiChildLayoutDelegate"/>.</summary>
public sealed class RenderCustomMultiChildLayoutBox : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, MultiChildLayoutParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, MultiChildLayoutParentData> _container;
    private MultiChildLayoutDelegate _delegate;

    public RenderCustomMultiChildLayoutBox(
        MultiChildLayoutDelegate @delegate,
        List<RenderBox>? children = null)
    {
        _delegate = @delegate ?? throw new ArgumentNullException(nameof(@delegate));
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, MultiChildLayoutParentData>(this);
        if (children is not null)
        {
            AddAll(children);
        }
    }

    public MultiChildLayoutDelegate Delegate
    {
        get => _delegate;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_delegate, value))
            {
                return;
            }

            MultiChildLayoutDelegate oldDelegate = _delegate;
            if (value.GetType() != oldDelegate.GetType() || value.ShouldRelayout(oldDelegate))
            {
                MarkNeedsLayout();
            }

            if (Attached)
            {
                oldDelegate.Relayout?.RemoveListener(MarkNeedsLayout);
                value.Relayout?.AddListener(MarkNeedsLayout);
            }

            _delegate = value;
        }
    }

    public RenderBox? FirstChild => _container.FirstChild;
    public RenderBox? LastChild => _container.LastChild;
    public int ChildCount => _container.ChildCount;

    public void AddAll(List<RenderBox> children) => _container.AddAll(children);
    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);
    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);
    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);
    public void Remove(RenderBox child) => _container.Remove(child);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not MultiChildLayoutParentData)
        {
            child.parentData = new MultiChildLayoutParentData();
        }
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        _delegate.Relayout?.AddListener(MarkNeedsLayout);
    }

    protected override void OnDetach()
    {
        _delegate.Relayout?.RemoveListener(MarkNeedsLayout);
        base.OnDetach();
    }

    protected override void PerformLayout()
    {
        Size = Constraints.Constrain(_delegate.GetSize(Constraints));
        _delegate.CallPerformLayout(Size, FirstChild);
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        DefaultPaint(context, offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        return DefaultHitTestChildren(result, position);
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
            var parentData = (MultiChildLayoutParentData)child.parentData!;
            visitor(child, parentData.offset, Matrix.Identity);
        }
    }

    public void DefaultPaint(PaintingContext context, Point offset) => _container.DefaultPaint(context, offset);

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
}
