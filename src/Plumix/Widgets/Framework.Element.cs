using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/framework.dart (approximate)

namespace Plumix.Widgets;

public sealed class IndexedSlot<T>
{
    public IndexedSlot(int index, T? value)
    {
        Index = index;
        Value = value;
    }

    public int Index { get; }

    public T? Value { get; }
}

public readonly struct BuildContext
{
    internal Element Owner { get; }

    internal BuildContext(Element owner)
    {
        Owner = owner;
    }

    public T? DependOnInherited<T>(object? aspect = null) where T : InheritedWidget => Owner.DependOnInherited<T>(aspect);
}

internal enum ElementLifecycleState
{
    Initial,
    Active,
    Inactive,
    Defunct
}

public abstract class Element
{
    private static int _nextElementId;

    private ElementLifecycleState _lifecycleState = ElementLifecycleState.Initial;
    private HashSet<InheritedElement>? _dependencies;
    private bool _hadUnsatisfiedDependencies;

    public Widget Widget { get; private set; }
    public Element? Parent { get; private set; }
    public int Depth { get; private set; }
    public object? Slot { get; private set; }

    internal int SequenceId { get; } = Interlocked.Increment(ref _nextElementId);
    internal bool Dirty { get; set; }
    internal BuildOwner? Owner { get; private set; }

    internal bool IsActive => _lifecycleState == ElementLifecycleState.Active;
    internal bool IsInactive => _lifecycleState == ElementLifecycleState.Inactive;

    protected Element(Widget widget)
    {
        Widget = widget;
    }

    internal void Attach(BuildOwner owner)
    {
        if (Owner != null && !ReferenceEquals(Owner, owner))
        {
            throw new InvalidOperationException("Element cannot be attached to multiple BuildOwner instances.");
        }

        Owner = owner;
        Owner.RegisterElement(this);
    }

    internal void Mount(Element? parent, object? newSlot)
    {
        if (_lifecycleState != ElementLifecycleState.Initial)
        {
            throw new InvalidOperationException($"Cannot mount element in state {_lifecycleState}.");
        }

        Parent = parent;
        Slot = newSlot;
        Depth = (parent?.Depth ?? 0) + 1;
        _lifecycleState = ElementLifecycleState.Active;

        if (Widget.Key is GlobalKey globalKey)
        {
            Owner?.RegisterGlobalKey(globalKey, this);
        }

        OnMount();
    }

    internal void ActivateWithParent(Element parent, object? newSlot)
    {
        if (_lifecycleState != ElementLifecycleState.Inactive)
        {
            throw new InvalidOperationException($"Cannot activate element in state {_lifecycleState}.");
        }

        var hadDependencies = (_dependencies?.Count > 0) || _hadUnsatisfiedDependencies;

        Parent = parent;
        Slot = newSlot;
        Depth = parent.Depth + 1;
        _lifecycleState = ElementLifecycleState.Active;
        _dependencies?.Clear();
        _hadUnsatisfiedDependencies = false;

        OnActivate();

        VisitChildren(child => child.ActivateWithParent(this, child.Slot));

        if (hadDependencies)
        {
            DidChangeDependencies();
        }

        if (Dirty)
        {
            Owner?.ScheduleBuild(this);
        }
        else
        {
            MarkNeedsBuild();
        }
    }

    protected virtual void OnMount()
    {
    }

    protected virtual void OnActivate()
    {
    }

    protected virtual void OnDeactivate()
    {
    }

    internal virtual void UpdateSlot(object? newSlot)
    {
        Slot = newSlot;
    }

    protected virtual void OnUnmount()
    {
    }

    internal virtual void DidChangeDependencies()
    {
        MarkNeedsBuild();
    }

    internal virtual void VisitChildren(Action<Element> visitor)
    {
    }

    internal void DeactivateRecursively()
    {
        if (_lifecycleState != ElementLifecycleState.Active)
        {
            return;
        }

        Owner?.UnscheduleBuild(this);
        Dirty = false;

        OnDeactivate();

        VisitChildren(child => child.DeactivateRecursively());
        RemoveDependencies();

        Parent = null;
        _lifecycleState = ElementLifecycleState.Inactive;
        Owner?.TrackInactive(this);
    }

    internal virtual void Unmount()
    {
        if (_lifecycleState == ElementLifecycleState.Defunct)
        {
            return;
        }

        OnUnmount();

        var key = Widget.Key as GlobalKey;
        if (key != null)
        {
            Owner?.UnregisterGlobalKey(key, this);
        }

        Owner?.UnscheduleBuild(this);
        Owner?.UnregisterElement(this);

        _dependencies = null;
        _hadUnsatisfiedDependencies = false;

        Parent = null;
        Slot = null;
        Dirty = false;
        _lifecycleState = ElementLifecycleState.Defunct;
    }

    internal abstract void Rebuild();

    internal void MarkNeedsBuild()
    {
        if (Dirty)
        {
            return;
        }

        Dirty = true;
        Owner?.ScheduleBuild(this);
    }

    internal virtual void Update(Widget newWidget)
    {
        var oldGlobalKey = Widget.Key as GlobalKey;
        var newGlobalKey = newWidget.Key as GlobalKey;

        Widget = newWidget;

        if (!Equals(oldGlobalKey, newGlobalKey))
        {
            if (oldGlobalKey != null)
            {
                Owner?.UnregisterGlobalKey(oldGlobalKey, this);
            }

            if (newGlobalKey != null)
            {
                Owner?.RegisterGlobalKey(newGlobalKey, this);
            }
        }
    }

    internal virtual void ForgetChild(Element child)
    {
    }

    internal virtual void UpdateSlotForChild(Element child, object? newSlot)
    {
        void Visit(Element element)
        {
            element.UpdateSlot(newSlot);

            if (element.RenderObjectAttachingChild is { } descendant)
            {
                Visit(descendant);
            }
        }

        Visit(child);
    }

    internal virtual void DeactivateChild(Element child)
    {
        ForgetChild(child);

        if (Owner == null)
        {
            child.Unmount();
            return;
        }

        Owner.Deactivate(child);
    }

    internal virtual void UnmountChild(Element child)
    {
        ForgetChild(child);
        child.Unmount();
    }

    internal Element InflateWidget(Widget newWidget, object? newSlot)
    {
        var owner = Owner ?? throw new InvalidOperationException("Element is not attached to BuildOwner.");

        var inactiveElement = owner.RetakeInactiveElement(this, newWidget);
        if (inactiveElement != null)
        {
            inactiveElement.ActivateWithParent(this, newSlot);
            if (!ReferenceEquals(inactiveElement.Widget, newWidget))
            {
                inactiveElement.Update(newWidget);
            }

            return inactiveElement;
        }

        var element = newWidget.CreateElement();
        element.Attach(owner);
        element.Mount(this, newSlot);
        return element;
    }

    internal Element? UpdateChild(Element? child, Widget? newWidget, object? newSlot)
    {
        if (newWidget == null)
        {
            if (child != null)
            {
                DeactivateChild(child);
            }

            return null;
        }

        if (child != null)
        {
            if (ReferenceEquals(child.Widget, newWidget))
            {
                if (!Equals(child.Slot, newSlot))
                {
                    UpdateSlotForChild(child, newSlot);
                }

                return child;
            }

            if (Widget.CanUpdate(child.Widget, newWidget))
            {
                if (!Equals(child.Slot, newSlot))
                {
                    UpdateSlotForChild(child, newSlot);
                }

                child.Update(newWidget);
                return child;
            }

            DeactivateChild(child);
        }

        return InflateWidget(newWidget, newSlot);
    }

    internal List<Element> UpdateChildren(
        List<Element> oldChildren,
        IReadOnlyList<Widget> newWidgets,
        HashSet<Element>? forgottenChildren = null,
        IReadOnlyList<object?>? slots = null)
    {
        if (slots != null && slots.Count != newWidgets.Count)
        {
            throw new ArgumentException("slots and newWidgets must have the same length.");
        }

        Element? ReplaceWithNullIfForgotten(Element child)
        {
            return forgottenChildren != null && forgottenChildren.Contains(child) ? null : child;
        }

        object? SlotFor(int newChildIndex, Element? previousChild)
        {
            return slots != null
                ? slots[newChildIndex]
                : new IndexedSlot<Element?>(newChildIndex, previousChild);
        }

        int newChildrenTop = 0;
        int oldChildrenTop = 0;
        int newChildrenBottom = newWidgets.Count - 1;
        int oldChildrenBottom = oldChildren.Count - 1;

        var newChildren = new Element[newWidgets.Count];

        Element? previousChild = null;

        while (oldChildrenTop <= oldChildrenBottom && newChildrenTop <= newChildrenBottom)
        {
            var oldChild = ReplaceWithNullIfForgotten(oldChildren[oldChildrenTop]);
            var newWidget = newWidgets[newChildrenTop];
            if (oldChild == null || !Widget.CanUpdate(oldChild.Widget, newWidget))
            {
                break;
            }

            var newChild = UpdateChild(oldChild, newWidget, SlotFor(newChildrenTop, previousChild))!;
            newChildren[newChildrenTop] = newChild;
            previousChild = newChild;
            newChildrenTop += 1;
            oldChildrenTop += 1;
        }

        while (oldChildrenTop <= oldChildrenBottom && newChildrenTop <= newChildrenBottom)
        {
            var oldChild = ReplaceWithNullIfForgotten(oldChildren[oldChildrenBottom]);
            var newWidget = newWidgets[newChildrenBottom];
            if (oldChild == null || !Widget.CanUpdate(oldChild.Widget, newWidget))
            {
                break;
            }

            oldChildrenBottom -= 1;
            newChildrenBottom -= 1;
        }

        var haveOldChildren = oldChildrenTop <= oldChildrenBottom;
        Dictionary<Key, Element>? oldKeyedChildren = null;
        if (haveOldChildren)
        {
            oldKeyedChildren = [];
            while (oldChildrenTop <= oldChildrenBottom)
            {
                var oldChild = ReplaceWithNullIfForgotten(oldChildren[oldChildrenTop]);
                if (oldChild != null)
                {
                    if (oldChild.Widget.Key != null)
                    {
                        oldKeyedChildren[oldChild.Widget.Key!] = oldChild;
                    }
                    else
                    {
                        DeactivateChild(oldChild);
                    }
                }

                oldChildrenTop += 1;
            }
        }

        while (newChildrenTop <= newChildrenBottom)
        {
            Element? oldChild = null;
            var newWidget = newWidgets[newChildrenTop];

            if (haveOldChildren)
            {
                var key = newWidget.Key;
                if (key != null && oldKeyedChildren!.TryGetValue(key, out var keyedOldChild))
                {
                    if (Widget.CanUpdate(keyedOldChild.Widget, newWidget))
                    {
                        oldChild = keyedOldChild;
                        oldKeyedChildren.Remove(key);
                    }
                }
            }

            var newChild = UpdateChild(oldChild, newWidget, SlotFor(newChildrenTop, previousChild))!;
            newChildren[newChildrenTop] = newChild;
            previousChild = newChild;
            newChildrenTop += 1;
        }

        newChildrenBottom = newWidgets.Count - 1;
        oldChildrenBottom = oldChildren.Count - 1;

        while (oldChildrenTop <= oldChildrenBottom && newChildrenTop <= newChildrenBottom)
        {
            var oldChild = oldChildren[oldChildrenTop];
            if (ReplaceWithNullIfForgotten(oldChild) == null)
            {
                oldChildrenTop += 1;
                continue;
            }

            var newWidget = newWidgets[newChildrenTop];
            var newChild = UpdateChild(oldChild, newWidget, SlotFor(newChildrenTop, previousChild))!;
            newChildren[newChildrenTop] = newChild;
            previousChild = newChild;
            newChildrenTop += 1;
            oldChildrenTop += 1;
        }

        if (haveOldChildren && oldKeyedChildren!.Count > 0)
        {
            foreach (var oldChild in oldKeyedChildren.Values)
            {
                if (forgottenChildren == null || !forgottenChildren.Contains(oldChild))
                {
                    DeactivateChild(oldChild);
                }
            }
        }

        return [..newChildren];
    }

    internal virtual T? DependOnInherited<T>(object? aspect = null) where T : InheritedWidget
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Cannot lookup inherited widgets from an inactive element.");
        }

        for (var ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ancestor is InheritedElement inheritedElement && inheritedElement.Widget is T typedWidget)
            {
                _ = DependOnInheritedElement(inheritedElement, aspect);
                return typedWidget;
            }
        }

        _hadUnsatisfiedDependencies = true;
        return null;
    }

    public virtual RenderObject? RenderObject => null;

    internal virtual Element? RenderObjectAttachingChild => null;

    internal InheritedWidget DependOnInheritedElement(InheritedElement ancestor, object? aspect)
    {
        _dependencies ??= [];
        _dependencies.Add(ancestor);
        ancestor.UpdateDependencies(this, aspect);

        return (InheritedWidget)ancestor.Widget;
    }

    private void RemoveDependencies()
    {
        if (_dependencies == null || _dependencies.Count == 0)
        {
            return;
        }

        foreach (var dependency in _dependencies)
        {
            dependency.RemoveDependent(this);
        }
    }
}

public sealed class StatelessElement : Element
{
    private Element? _child;

    public StatelessElement(StatelessWidget widget) : base(widget)
    {
    }

    public override RenderObject? RenderObject => _child?.RenderObject;

    internal override Element? RenderObjectAttachingChild => _child;

    protected override void OnMount()
    {
        base.OnMount();
        Rebuild();
    }

    internal override void Rebuild()
    {
        Dirty = false;
        var childWidget = ((StatelessWidget)Widget).Build(new BuildContext(this));
        _child = UpdateChild(_child, childWidget, Slot);
    }

    internal override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        Rebuild();
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    internal override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _child))
        {
            _child = null;
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

public sealed class StatefulElement : Element
{
    private Element? _child;
    private bool _didChangeDependencies;

    public State State { get; }

    public StatefulElement(StatefulWidget widget) : base(widget)
    {
        State = widget.CreateState();
        State.Element = this;
    }

    public override RenderObject? RenderObject => _child?.RenderObject;

    internal override Element? RenderObjectAttachingChild => _child;

    protected override void OnMount()
    {
        base.OnMount();
        State.InitState();
        State.DidChangeDependencies();
        Rebuild();
    }

    protected override void OnActivate()
    {
        base.OnActivate();
        State.Activate();
    }

    protected override void OnDeactivate()
    {
        State.Deactivate();
        base.OnDeactivate();
    }

    internal override void Rebuild()
    {
        Dirty = false;

        if (_didChangeDependencies)
        {
            State.DidChangeDependencies();
            _didChangeDependencies = false;
        }

        var widget = State.Build(new BuildContext(this));
        _child = UpdateChild(_child, widget, Slot);
    }

    internal override void Update(Widget newWidget)
    {
        var old = (StatefulWidget)Widget;
        base.Update(newWidget);
        State.DidUpdateWidget(old);
        Rebuild();
    }

    internal override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        _didChangeDependencies = true;
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    internal override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _child))
        {
            _child = null;
        }
    }

    internal override void Unmount()
    {
        if (_child != null)
        {
            UnmountChild(_child);
            _child = null;
        }

        State.Dispose();
        base.Unmount();
    }
}

public class InheritedElement : Element
{
    private Element? _child;
    private readonly Dictionary<Element, object?> _dependents = [];

    public InheritedElement(InheritedWidget widget) : base(widget)
    {
    }

    public override RenderObject? RenderObject => _child?.RenderObject;

    internal override Element? RenderObjectAttachingChild => _child;

    protected override void OnMount()
    {
        base.OnMount();
        Rebuild();
    }

    internal override void Rebuild()
    {
        Dirty = false;
        var child = ((InheritedWidget)Widget).Build(new BuildContext(this));
        _child = UpdateChild(_child, child, Slot);
    }

    internal override void Update(Widget newWidget)
    {
        var old = (InheritedWidget)Widget;
        base.Update(newWidget);
        if (((InheritedWidget)newWidget).UpdateShouldNotify(old))
        {
            NotifyClients(old);
        }

        Rebuild();
    }

    protected object? GetDependencies(Element dependent)
    {
        _dependents.TryGetValue(dependent, out var dependencies);
        return dependencies;
    }

    protected void SetDependencies(Element dependent, object? value)
    {
        _dependents[dependent] = value;
    }

    internal virtual void UpdateDependencies(Element dependent, object? aspect)
    {
        SetDependencies(dependent, value: null);
    }

    internal virtual void RemoveDependent(Element dependent)
    {
        _dependents.Remove(dependent);
    }

    protected void NotifyClients(InheritedWidget oldWidget)
    {
        if (_dependents.Count == 0)
        {
            return;
        }

        foreach (var dependent in _dependents.Keys.ToArray())
        {
            NotifyDependent(oldWidget, dependent);
        }
    }

    internal virtual void NotifyDependent(InheritedWidget _, Element dependent)
    {
        dependent.DidChangeDependencies();
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    internal override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _child))
        {
            _child = null;
        }
    }

    internal override void Unmount()
    {
        if (_child != null)
        {
            UnmountChild(_child);
            _child = null;
        }

        _dependents.Clear();
        base.Unmount();
    }
}

public sealed class InheritedModelElement<TAspect> : InheritedElement
{
    public InheritedModelElement(InheritedModel<TAspect> widget) : base(widget)
    {
    }

    private InheritedModel<TAspect> InheritedModelWidget => (InheritedModel<TAspect>)Widget;

    internal override void UpdateDependencies(Element dependent, object? aspect)
    {
        var dependencies = GetDependencies(dependent) as HashSet<TAspect>;
        if (dependencies != null && dependencies.Count == 0)
        {
            return;
        }

        if (aspect == null)
        {
            SetDependencies(dependent, new HashSet<TAspect>());
            return;
        }

        if (aspect is not TAspect typedAspect)
        {
            throw new InvalidOperationException($"InheritedModel aspect must be of type {typeof(TAspect).Name}.");
        }

        dependencies ??= [];
        dependencies.Add(typedAspect);
        SetDependencies(dependent, dependencies);
    }

    internal override void NotifyDependent(InheritedWidget oldWidget, Element dependent)
    {
        var dependencies = GetDependencies(dependent) as HashSet<TAspect>;
        if (dependencies == null)
        {
            return;
        }

        if (dependencies.Count == 0
            || InheritedModelWidget.UpdateShouldNotifyDependent((InheritedModel<TAspect>)oldWidget, dependencies))
        {
            dependent.DidChangeDependencies();
        }
    }
}

public sealed class InheritedNotifierElement<TNotifier> : InheritedElement where TNotifier : class, IListenable
{
    private bool _dirty;

    public InheritedNotifierElement(InheritedNotifier<TNotifier> widget) : base(widget)
    {
    }

    private InheritedNotifier<TNotifier> InheritedNotifierWidget => (InheritedNotifier<TNotifier>)Widget;

    protected override void OnMount()
    {
        InheritedNotifierWidget.Notifier?.AddListener(HandleUpdate);
        base.OnMount();
    }

    internal override void Update(Widget newWidget)
    {
        var oldNotifier = InheritedNotifierWidget.Notifier;
        var newNotifier = ((InheritedNotifier<TNotifier>)newWidget).Notifier;
        if (!ReferenceEquals(oldNotifier, newNotifier))
        {
            oldNotifier?.RemoveListener(HandleUpdate);
            newNotifier?.AddListener(HandleUpdate);
        }

        base.Update(newWidget);
    }

    internal override void Rebuild()
    {
        if (_dirty)
        {
            NotifyClients(InheritedNotifierWidget);
            _dirty = false;
        }

        base.Rebuild();
    }

    internal override void Unmount()
    {
        InheritedNotifierWidget.Notifier?.RemoveListener(HandleUpdate);
        base.Unmount();
    }

    private void HandleUpdate()
    {
        _dirty = true;
        MarkNeedsBuild();
    }
}

public class ProxyElement : Element
{
    private Element? _child;

    public ProxyElement(ProxyWidget widget) : base(widget)
    {
    }

    public override RenderObject? RenderObject => _child?.RenderObject;

    internal override Element? RenderObjectAttachingChild => _child;

    protected override void OnMount()
    {
        base.OnMount();
        Rebuild();
    }

    internal override void Rebuild()
    {
        Dirty = false;
        _child = UpdateChild(_child, ((ProxyWidget)Widget).Child, Slot);
    }

    internal override void Update(Widget newWidget)
    {
        var old = (ProxyWidget)Widget;
        base.Update(newWidget);
        Updated(old);
        Rebuild();
    }

    protected virtual void Updated(ProxyWidget oldWidget)
    {
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    internal override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _child))
        {
            _child = null;
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

internal abstract class ParentDataElementBase : ProxyElement
{
    protected ParentDataElementBase(ProxyWidget widget) : base(widget)
    {
    }

    internal abstract IParentDataWidget ParentDataWidget { get; }
}

internal sealed class ParentDataElement<T> : ParentDataElementBase where T : IParentData
{
    public ParentDataElement(ParentDataWidget<T> widget) : base(widget)
    {
    }

    internal override IParentDataWidget ParentDataWidget => (IParentDataWidget)Widget;

    internal override void Rebuild()
    {
        base.Rebuild();
        ApplyParentData((ParentDataWidget<T>)Widget);
    }

    protected override void Updated(ProxyWidget oldWidget)
    {
        ApplyParentData((ParentDataWidget<T>)Widget);
    }

    private void ApplyParentData(ParentDataWidget<T> widget)
    {
        void ApplyParentDataToChild(Element child)
        {
            if (child is RenderObjectElement renderObjectElement)
            {
                renderObjectElement.UpdateParentData(widget);
                return;
            }

            if (child.RenderObjectAttachingChild != null)
            {
                ApplyParentDataToChild(child.RenderObjectAttachingChild);
            }
        }

        if (RenderObjectAttachingChild != null)
        {
            ApplyParentDataToChild(RenderObjectAttachingChild);
        }
    }
}
