using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/framework.dart (approximate)

namespace Plumix.Widgets;

internal interface IRenderObjectHost
{
    void InsertRenderObjectChild(RenderObject child, object? slot);
    void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot);
    void RemoveRenderObjectChild(RenderObject child, object? slot);
}

public interface IRenderObjectSingleChildContainer
{
    RenderObject? Child { get; set; }
}

public interface ISlottedRenderObjectContainer
{
    void SetChild(RenderObject? child, object slot);
}

public abstract class RenderObjectWidget(Key? key = null) : Widget(key)
{
    internal abstract RenderObject CreateRenderObject(BuildContext context);

    internal virtual void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
    }

    internal virtual void DidUnmountRenderObject(RenderObject renderObject)
    {
    }
}

public abstract class LeafRenderObjectWidget(Key? key = null) : RenderObjectWidget(key)
{
    internal override Element CreateElement() => new LeafRenderObjectElement(this);
}

public abstract class SingleChildRenderObjectWidget : RenderObjectWidget
{
    protected SingleChildRenderObjectWidget(Widget? child = null, Key? key = null) : base(key)
    {
        Child = child;
    }

    public Widget? Child { get; }

    internal override Element CreateElement() => new SingleChildRenderObjectElement(this);
}

public abstract class MultiChildRenderObjectWidget : RenderObjectWidget
{
    protected MultiChildRenderObjectWidget(IReadOnlyList<Widget>? children = null, Key? key = null) : base(key)
    {
        Children = children ?? [];
    }

    public IReadOnlyList<Widget> Children { get; }

    internal override Element CreateElement() => new MultiChildRenderObjectElement(this);
}

public abstract class SlottedMultiChildRenderObjectWidget<TSlot> : RenderObjectWidget
    where TSlot : notnull
{
    protected SlottedMultiChildRenderObjectWidget(Key? key = null) : base(key)
    {
    }

    public abstract IReadOnlyList<TSlot> Slots { get; }

    public abstract Widget? ChildForSlot(TSlot slot);

    internal override Element CreateElement() => new SlottedRenderObjectElement<TSlot>(this);
}

public abstract class RenderObjectElement : Element, IRenderObjectHost
{
    private RenderObject? _renderObject;
    private IRenderObjectHost? _ancestorRenderObjectHost;
    private Element? _ancestorRenderObjectHostElement;

    protected RenderObjectElement(RenderObjectWidget widget) : base(widget)
    {
    }

    public sealed override RenderObject? RenderObject => _renderObject;

    protected RenderObjectWidget RenderObjectWidget => (RenderObjectWidget)Widget;

    protected override void OnMount()
    {
        base.OnMount();
        _renderObject = RenderObjectWidget.CreateRenderObject(new BuildContext(this));
        AttachRenderObject(Slot);
    }

    protected override void OnDeactivate()
    {
        base.OnDeactivate();
        if (RequireRenderObject().Attached)
        {
            throw new AssertionError(
                $"{GetType().Name} must be detached before it is deactivated; "
                + $"{RequireRenderObject().GetType().Name} is still attached to "
                + $"{RequireRenderObject().Parent?.GetType().Name ?? "no render parent"} via "
                + $"{_ancestorRenderObjectHost?.GetType().Name ?? "no element host"}.");
        }
    }

    internal override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        RenderObjectWidget.UpdateRenderObject(new BuildContext(this), RequireRenderObject());
    }

    internal override void Rebuild()
    {
        Dirty = false;
        RenderObjectWidget.UpdateRenderObject(new BuildContext(this), RequireRenderObject());
    }

    internal override void UpdateSlot(object? newSlot)
    {
        object? oldSlot = Slot;
        base.UpdateSlot(newSlot);

        if (_ancestorRenderObjectHost != null && !Equals(oldSlot, newSlot))
        {
            _ancestorRenderObjectHost.MoveRenderObjectChild(RequireRenderObject(), oldSlot, newSlot);
        }
    }

    internal void UpdateParentData(IParentDataWidget parentDataWidget)
    {
        var renderObject = RequireRenderObject();
        if (!parentDataWidget.DebugIsValidRenderObject(renderObject))
        {
            return;
        }

        parentDataWidget.ApplyParentData(renderObject);
    }

    protected RenderObject RequireRenderObject()
    {
        return _renderObject ?? throw new InvalidOperationException("RenderObjectElement is not mounted.");
    }

    internal override void AttachRenderObject(object? newSlot)
    {
        if (_ancestorRenderObjectHost != null)
        {
            throw new AssertionError("A RenderObjectElement cannot attach its render object twice.");
        }

        base.UpdateSlot(newSlot);
        (_ancestorRenderObjectHost, _ancestorRenderObjectHostElement) = FindAncestorRenderObjectHost();
        if (_ancestorRenderObjectHost == null)
        {
            throw new InvalidOperationException($"RenderObject host not found for {GetType().Name}.");
        }

        _ancestorRenderObjectHost.InsertRenderObjectChild(RequireRenderObject(), newSlot);
        ApplyParentDataFromAncestors();
    }

    private (IRenderObjectHost? host, Element? hostElement) FindAncestorRenderObjectHost()
    {
        for (var ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ancestor is IRenderObjectHost host)
            {
                return (host, ancestor);
            }
        }

        return (null, null);
    }

    private void ApplyParentDataFromAncestors()
    {
        for (var ancestor = Parent;
             ancestor != null && !ReferenceEquals(ancestor, _ancestorRenderObjectHostElement);
             ancestor = ancestor.Parent)
        {
            if (ancestor is ParentDataElementBase parentDataElement)
            {
                UpdateParentData(parentDataElement.ParentDataWidget);
            }
        }
    }

    internal override void DetachRenderObject()
    {
        if (_ancestorRenderObjectHost != null)
        {
            _ancestorRenderObjectHost.RemoveRenderObjectChild(RequireRenderObject(), Slot);
        }

        _ancestorRenderObjectHost = null;
        _ancestorRenderObjectHostElement = null;
        base.UpdateSlot(null);
    }

    internal override void Unmount()
    {
        if (_renderObject is null)
        {
            base.Unmount();
            return;
        }

        RenderObject renderObject = _renderObject;
        RenderObjectWidget oldWidget = RenderObjectWidget;
        base.Unmount();
        if (renderObject.Attached)
        {
            // Root adapters must normally detach from their PipelineOwner in DetachRenderObject. Keep
            // direct test/host adapters safe when they attach the render root outside IRenderObjectHost.
            if (renderObject.Parent is IRenderObjectSingleChildContainer singleChildContainer
                && ReferenceEquals(singleChildContainer.Child, renderObject))
            {
                singleChildContainer.Child = null;
            }
            else if (renderObject.Parent is IRenderObjectContainer container)
            {
                container.Remove(renderObject);
            }
            else
            {
                renderObject.Detach();
            }
        }

        if (renderObject.Attached)
        {
            throw new AssertionError("A RenderObjectElement cannot dispose an attached render object.");
        }

        oldWidget.DidUnmountRenderObject(renderObject);
        renderObject.Dispose();
        _renderObject = null;
    }

    public abstract void InsertRenderObjectChild(RenderObject child, object? slot);
    public abstract void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot);
    public abstract void RemoveRenderObjectChild(RenderObject child, object? slot);
}

public sealed class LeafRenderObjectElement : RenderObjectElement
{
    public LeafRenderObjectElement(LeafRenderObjectWidget widget) : base(widget)
    {
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        throw new InvalidOperationException("LeafRenderObjectElement cannot host children.");
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        throw new InvalidOperationException("LeafRenderObjectElement cannot host children.");
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        throw new InvalidOperationException("LeafRenderObjectElement cannot host children.");
    }
}

public sealed class SingleChildRenderObjectElement : RenderObjectElement
{
    private Element? _child;

    public SingleChildRenderObjectElement(SingleChildRenderObjectWidget widget) : base(widget)
    {
    }

    protected override void OnMount()
    {
        base.OnMount();
        _child = UpdateChild(_child, ((SingleChildRenderObjectWidget)Widget).Child, null);
    }

    internal override void Rebuild()
    {
        base.Rebuild();
        _child = UpdateChild(_child, ((SingleChildRenderObjectWidget)Widget).Child, null);
    }

    internal override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        _child = UpdateChild(_child, ((SingleChildRenderObjectWidget)Widget).Child, null);
    }

    internal override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _child))
        {
            _child = null;
        }
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        if (slot != null)
        {
            throw new InvalidOperationException("SingleChildRenderObjectElement expects null slot.");
        }

        if (RequireRenderObject() is not IRenderObjectSingleChildContainer container)
        {
            throw new InvalidOperationException(
                "SingleChildRenderObjectElement requires render object implementing IRenderObjectSingleChildContainer.");
        }

        container.Child = child;
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        if (!Equals(oldSlot, newSlot))
        {
            throw new InvalidOperationException("SingleChildRenderObjectElement does not support moving children.");
        }
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        if (slot != null)
        {
            throw new InvalidOperationException("SingleChildRenderObjectElement expects null slot.");
        }

        if (RequireRenderObject() is not IRenderObjectSingleChildContainer container)
        {
            throw new InvalidOperationException(
                "SingleChildRenderObjectElement requires render object implementing IRenderObjectSingleChildContainer.");
        }

        if (ReferenceEquals(container.Child, child))
        {
            container.Child = null;
        }
    }

    internal override void Unmount()
    {
        if (_child != null)
        {
            UnmountChild(_child);
            _child = null;
        }

        base.Unmount();
    }
}

public class MultiChildRenderObjectElement : RenderObjectElement
{
    private List<Element> _children = [];
    private readonly HashSet<Element> _forgottenChildren = [];

    public MultiChildRenderObjectElement(MultiChildRenderObjectWidget widget) : base(widget)
    {
    }

    /// <summary>The child elements, in the order their widgets were supplied.</summary>
    protected IReadOnlyList<Element> Children => _children;

    protected override void OnMount()
    {
        base.OnMount();

        var widgets = ((MultiChildRenderObjectWidget)Widget).Children;
        _children = new List<Element>(widgets.Count);

        Element? previousChild = null;
        for (int index = 0; index < widgets.Count; index++)
        {
            var newChild = InflateWidget(widgets[index], new IndexedSlot<Element?>(index, previousChild));
            EnsureChildHasAssociatedRenderObject(newChild);
            _children.Add(newChild);
            previousChild = newChild;
        }
    }

    internal override void Rebuild()
    {
        base.Rebuild();
        _children = UpdateChildren(_children, ((MultiChildRenderObjectWidget)Widget).Children, _forgottenChildren);
        _forgottenChildren.Clear();
        EnsureChildrenHaveAssociatedRenderObjects();
    }

    internal override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        _children = UpdateChildren(_children, ((MultiChildRenderObjectWidget)Widget).Children, _forgottenChildren);
        _forgottenChildren.Clear();
        EnsureChildrenHaveAssociatedRenderObjects();
    }

    internal override void ForgetChild(Element child)
    {
        _forgottenChildren.Add(child);
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        foreach (var child in _children)
        {
            if (!_forgottenChildren.Contains(child))
            {
                visitor(child);
            }
        }
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        if (slot is not IndexedSlot<Element?> indexedSlot)
        {
            throw new InvalidOperationException("MultiChildRenderObjectElement requires IndexedSlot.");
        }

        RequireContainer().Insert(child, indexedSlot.Value?.RenderObject);
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        if (newSlot is not IndexedSlot<Element?> indexedSlot)
        {
            throw new InvalidOperationException("MultiChildRenderObjectElement requires IndexedSlot.");
        }

        RequireContainer().Move(child, indexedSlot.Value?.RenderObject);
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        RequireContainer().Remove(child);
    }

    private IRenderObjectContainer RequireContainer()
    {
        if (RequireRenderObject() is IRenderObjectContainer container)
        {
            return container;
        }

        throw new InvalidOperationException(
            $"{RequireRenderObject().GetType().Name} must implement {nameof(IRenderObjectContainer)} for MultiChildRenderObjectElement.");
    }

    private static void EnsureChildHasAssociatedRenderObject(Element child)
    {
        if (child.RenderObject == null)
        {
            throw new InvalidOperationException(
                $"Child element {child.GetType().Name} does not expose an associated RenderObject.");
        }
    }

    private void EnsureChildrenHaveAssociatedRenderObjects()
    {
        foreach (var child in _children)
        {
            if (!_forgottenChildren.Contains(child))
            {
                EnsureChildHasAssociatedRenderObject(child);
            }
        }
    }

    internal override void Unmount()
    {
        foreach (var child in _children)
        {
            if (!_forgottenChildren.Contains(child))
            {
                UnmountChild(child);
            }
        }

        _children.Clear();
        _forgottenChildren.Clear();
        base.Unmount();
    }
}

public sealed class SlottedRenderObjectElement<TSlot> : RenderObjectElement
    where TSlot : notnull
{
    private readonly Dictionary<TSlot, Element> _children = [];

    public SlottedRenderObjectElement(SlottedMultiChildRenderObjectWidget<TSlot> widget) : base(widget)
    {
    }

    private SlottedMultiChildRenderObjectWidget<TSlot> SlottedWidget =>
        (SlottedMultiChildRenderObjectWidget<TSlot>)Widget;

    protected override void OnMount()
    {
        base.OnMount();
        UpdateSlotChildren();
    }

    internal override void Rebuild()
    {
        base.Rebuild();
        UpdateSlotChildren();
    }

    internal override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        UpdateSlotChildren();
    }

    internal override void ForgetChild(Element child)
    {
        TSlot? forgottenSlot = default;
        bool found = false;
        foreach ((TSlot slot, Element element) in _children)
        {
            if (!ReferenceEquals(element, child))
            {
                continue;
            }

            forgottenSlot = slot;
            found = true;
            break;
        }

        if (found)
        {
            _children.Remove(forgottenSlot!);
        }
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        foreach (Element child in _children.Values)
        {
            visitor(child);
        }
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        RequireContainer().SetChild(child, RequireSlot(slot));
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        object resolvedOldSlot = RequireSlot(oldSlot);
        object resolvedNewSlot = RequireSlot(newSlot);
        if (Equals(resolvedOldSlot, resolvedNewSlot))
        {
            return;
        }

        ISlottedRenderObjectContainer container = RequireContainer();
        container.SetChild(null, resolvedOldSlot);
        container.SetChild(child, resolvedNewSlot);
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        RequireContainer().SetChild(null, RequireSlot(slot));
    }

    internal override void Unmount()
    {
        foreach (Element child in _children.Values.ToList())
        {
            UnmountChild(child);
        }

        _children.Clear();
        base.Unmount();
    }

    private void UpdateSlotChildren()
    {
        var activeSlots = new HashSet<TSlot>(SlottedWidget.Slots);
        foreach (TSlot oldSlot in _children.Keys.Where(slot => !activeSlots.Contains(slot)).ToList())
        {
            Element oldChild = _children[oldSlot];
            UpdateChild(oldChild, null, oldSlot);
            _children.Remove(oldSlot);
        }

        foreach (TSlot slot in SlottedWidget.Slots)
        {
            _children.TryGetValue(slot, out Element? oldChild);
            Element? newChild = UpdateChild(oldChild, SlottedWidget.ChildForSlot(slot), slot);
            if (newChild is null)
            {
                _children.Remove(slot);
            }
            else
            {
                _children[slot] = newChild;
            }
        }
    }

    private ISlottedRenderObjectContainer RequireContainer()
    {
        if (RequireRenderObject() is ISlottedRenderObjectContainer container)
        {
            return container;
        }

        throw new InvalidOperationException(
            $"{RequireRenderObject().GetType().Name} must implement {nameof(ISlottedRenderObjectContainer)}.");
    }

    private static object RequireSlot(object? slot)
    {
        return slot ?? throw new InvalidOperationException("A slotted render child requires a non-null slot.");
    }
}
