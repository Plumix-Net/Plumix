using System.Collections.Immutable;
using Avalonia;
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

/// <summary>
/// A handle to the location of a widget in the widget tree. Dart parity:
/// <c>BuildContext</c>, which <see cref="Element"/> implements — a build context *is* the element.
/// </summary>
public interface BuildContext
{
    /// <summary>The current configuration of the <see cref="Element"/> that is this build context.</summary>
    Widget Widget { get; }

    /// <summary>The <see cref="BuildOwner"/> for this context, managing its rebuilds.</summary>
    BuildOwner? Owner { get; }

    /// <summary>Whether the widget is currently updating the widget or render tree.</summary>
    bool DebugDoingBuild { get; }

    /// <summary>Whether the <see cref="Widget"/> this context is associated with is currently mounted.</summary>
    bool Mounted { get; }

    /// <summary>
    /// The size of the render object returned by <see cref="FindRenderObject"/> when it is a
    /// <see cref="RenderBox"/>.
    /// </summary>
    Size? Size { get; }

    /// <summary>The render object of this element, or of the nearest descendant that has one.</summary>
    RenderObject? FindRenderObject();

    /// <summary>
    /// Registers this context with the nearest ancestor whose widget's runtime type is exactly
    /// <typeparamref name="T"/> and returns that widget. A subclass of <typeparamref name="T"/> does
    /// not match, and neither does a base class of it.
    /// </summary>
    T? DependOnInherited<T>(object? aspect = null) where T : InheritedWidget;

    /// <summary>Registers this context as depending on <paramref name="ancestor"/>.</summary>
    InheritedWidget DependOnInheritedElement(InheritedElement ancestor, object? aspect = null);

    /// <summary>
    /// Finds the nearest ancestor whose widget's runtime type is exactly <typeparamref name="T"/>
    /// without registering a dependency. Use this to read a value once without subscribing to
    /// future changes.
    /// </summary>
    T? GetInherited<T>() where T : InheritedWidget;

    /// <summary>
    /// Returns the nearest inherited element whose widget's runtime type is exactly
    /// <typeparamref name="T"/>, without creating a dependency.
    /// </summary>
    InheritedElement? GetElementForInheritedWidgetOfExactType<T>() where T : InheritedWidget;

    /// <summary>
    /// Returns the nearest ancestor widget whose runtime type is exactly <typeparamref name="T"/>,
    /// without creating a dependency. A subclass of <typeparamref name="T"/> does not match.
    /// </summary>
    T? FindAncestorWidgetOfExactType<T>() where T : Widget;

    /// <summary>Returns the nearest ancestor state of type <typeparamref name="T"/>.</summary>
    T? FindAncestorStateOfType<T>() where T : State;

    /// <summary>Returns the furthest ancestor state assignable to <typeparamref name="T"/>.</summary>
    T? FindRootAncestorStateOfType<T>() where T : State;

    /// <summary>Returns the nearest ancestor render object assignable to <typeparamref name="T"/>.</summary>
    T? FindAncestorRenderObjectOfType<T>() where T : RenderObject;

    /// <summary>Walks ancestor elements until <paramref name="visitor"/> returns false.</summary>
    void VisitAncestorElements(Func<Element, bool> visitor);

    /// <summary>Visits each direct child element of this build context.</summary>
    void VisitChildElements(Action<Element> visitor);

    /// <summary>Starts bubbling <paramref name="notification"/> at this context.</summary>
    void DispatchNotification(Notification notification);

    /// <summary>
    /// Returns a description of the <see cref="Element"/> associated with this build context.
    /// <paramref name="name"/> is typically something like "The element being rebuilt was".
    /// </summary>
    DiagnosticsNode DescribeElement(
        string name,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.ErrorProperty);

    /// <summary>
    /// Returns a description of the <see cref="Widget"/> associated with this build context.
    /// <paramref name="name"/> is typically something like "The widget being rebuilt was".
    /// </summary>
    DiagnosticsNode DescribeWidget(
        string name,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.ErrorProperty);

    /// <summary>
    /// Describes a widget type that is missing from this build context's ancestry, together with
    /// the ancestors that were searched.
    /// </summary>
    List<DiagnosticsNode> DescribeMissingAncestor(Type expectedAncestorType);

    /// <summary>Describes the ownership chain from this element back towards the root.</summary>
    DiagnosticsNode DescribeOwnershipChain(string name);
}

internal enum ElementLifecycleState
{
    Initial,
    Active,
    Inactive,
    Defunct
}

public abstract class Element : DiagnosticableTree, BuildContext
{
    private static int _nextElementId;

    private ElementLifecycleState _lifecycleState = ElementLifecycleState.Initial;
    private HashSet<InheritedElement>? _dependencies;
    private bool _hadUnsatisfiedDependencies;

    /// <summary>
    /// The inherited elements in scope at this element, keyed by the exact runtime type of their
    /// widget. Rebuilt by <see cref="UpdateInheritance"/> on mount and activation, and dropped on
    /// deactivation. Dart parity: <c>Element._inheritedElements</c>, a
    /// <c>PersistentHashMap&lt;Type, InheritedElement&gt;</c> — an immutable map is the same
    /// contract, since the only writes are whole-field assignments and every element in a subtree
    /// shares the map instance its nearest <see cref="InheritedElement"/> ancestor produced.
    /// </summary>
    internal ImmutableDictionary<Type, InheritedElement>? InheritedElements { get; private protected set; }

    public Widget Widget { get; private set; }
    public Element? Parent { get; private set; }
    public int Depth { get; private set; }
    public object? Slot { get; private set; }

    internal int SequenceId { get; } = Interlocked.Increment(ref _nextElementId);

    /// <summary>
    /// Whether this element needs rebuilding. Dart parity: the private <c>Element._dirty</c>, which
    /// starts out true because a freshly created element has never been built.
    /// </summary>
    public bool Dirty { get; private set; } = true;

    public BuildOwner? Owner { get; private set; }

    public bool IsActive => _lifecycleState == ElementLifecycleState.Active;
    internal bool IsInactive => _lifecycleState == ElementLifecycleState.Inactive;
    public bool Mounted =>
        _lifecycleState is ElementLifecycleState.Active or ElementLifecycleState.Inactive;

    /// <summary>Whether this element is currently running <see cref="PerformRebuild"/>.</summary>
    /// <remarks>Flutter's <c>Element.debugDoingBuild</c>.</remarks>
    public bool DebugDoingBuild { get; protected set; }

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

    public void Mount(Element? parent, object? newSlot)
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

        UpdateInheritance();

        OnMount();
    }

    /// <summary>
    /// Recomputes <see cref="InheritedElements"/> from the parent's. The base implementation shares
    /// the parent's map verbatim; <see cref="InheritedElement"/> overrides it to add itself.
    /// </summary>
    /// <remarks>Flutter's <c>Element._updateInheritance</c>.</remarks>
    private protected virtual void UpdateInheritance()
    {
        InheritedElements = Parent?.InheritedElements;
    }

    internal void ActivateWithParent(Element parent, object? newSlot)
    {
        ActivateRecursively(parent, newSlot);
        AttachRenderObject(newSlot);
    }

    private void ActivateRecursively(Element parent, object? newSlot)
    {
        if (_lifecycleState != ElementLifecycleState.Inactive)
        {
            throw new InvalidOperationException($"Cannot activate element in state {_lifecycleState}.");
        }

        bool hadDependencies = (_dependencies?.Count > 0) || _hadUnsatisfiedDependencies;

        Parent = parent;
        Depth = parent.Depth + 1;
        _lifecycleState = ElementLifecycleState.Active;
        _dependencies?.Clear();
        _hadUnsatisfiedDependencies = false;
        UpdateInheritance();

        OnActivate();

        VisitChildren(child => child.ActivateRecursively(this, child.Slot));

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

    public virtual void UpdateSlot(object? newSlot)
    {
        Slot = newSlot;
    }

    protected virtual void OnUnmount()
    {
    }

    public virtual void DidChangeDependencies()
    {
        MarkNeedsBuild();
    }

    public virtual void VisitChildren(Action<Element> visitor)
    {
    }

    /// <summary>
    /// Walks the children that are on stage, i.e. the ones the widget inspector shows. Defaults to
    /// every child; widgets that hide part of the tree (such as <see cref="Offstage"/>) override it.
    /// </summary>
    /// <remarks>Flutter's <c>Element.debugVisitOnstageChildren</c>.</remarks>
    public virtual void DebugVisitOnstageChildren(Action<Element> visitor) => VisitChildren(visitor);

    public virtual void AttachRenderObject(object? newSlot)
    {
        if (Slot is not null)
        {
            throw new AssertionError("An Element with a slot cannot attach its render object again.");
        }

        VisitChildren(child => child.AttachRenderObject(newSlot));
        Slot = newSlot;
    }

    public virtual void DetachRenderObject()
    {
        VisitChildren(static child => child.DetachRenderObject());
        Slot = null;
    }

    internal void DeactivateRecursively(bool isRoot = true)
    {
        if (_lifecycleState != ElementLifecycleState.Active)
        {
            return;
        }

        Owner?.UnscheduleBuild(this);
        Dirty = false;

        OnDeactivate();

        VisitChildren(child => child.DeactivateRecursively(isRoot: false));
        RemoveDependencies();

        if (isRoot)
        {
            Parent = null;
        }

        InheritedElements = null;
        _lifecycleState = ElementLifecycleState.Inactive;
        Owner?.TrackInactive(this);
    }

    public virtual void Unmount()
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

    /// <summary>
    /// Rebuilds this element if it is dirty, or unconditionally when <paramref name="force"/> is set.
    /// The rebuild itself is done by <see cref="PerformRebuild"/>.
    /// </summary>
    /// <remarks>Flutter's <c>Element.rebuild({bool force = false})</c>.</remarks>
    public void Rebuild(bool force = false)
    {
        if (_lifecycleState == ElementLifecycleState.Initial)
        {
            throw new AssertionError("Cannot rebuild an element that has not been mounted.");
        }

        if (_lifecycleState != ElementLifecycleState.Active || (!Dirty && !force))
        {
            return;
        }

        PerformRebuild();
    }

    /// <summary>
    /// Rebuilds the element's subtree and clears <see cref="Dirty"/>. Only <see cref="Rebuild"/>
    /// calls it. Subclasses chain to <c>base.PerformRebuild()</c> after running their build step, so
    /// that a <see cref="MarkNeedsBuild"/> made while building is ignored the way Dart's is.
    /// </summary>
    /// <remarks>Flutter's <c>@protected Element.performRebuild()</c>.</remarks>
    protected virtual void PerformRebuild()
    {
        Dirty = false;
    }

    public virtual void MarkNeedsBuild()
    {
        if (Dirty)
        {
            return;
        }

        Dirty = true;
        Owner?.ScheduleBuild(this);
    }

    /// Called whenever the application is reassembled during debugging, for
    /// example during hot reload.
    ///
    /// This method should rerun any initialization logic that depends on
    /// global state, for example, image loading from asset bundles (since the
    /// asset bundle may have changed).
    ///
    /// See also:
    ///
    ///  * [State.Reassemble]
    ///  * [BuildOwner.Reassemble]
    public virtual void Reassemble()
    {
        MarkNeedsBuild();
        VisitChildren(child => child.Reassemble());
    }

    public virtual void Update(Widget newWidget)
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
        else if (!ReferenceEquals(oldGlobalKey, newGlobalKey))
        {
            oldGlobalKey?.DetachElement(this);
            newGlobalKey?.AttachElement(this);
        }
    }

    public virtual void ForgetChild(Element child)
    {
    }

    public virtual void UpdateSlotForChild(Element child, object? newSlot)
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

    public virtual void DeactivateChild(Element child)
    {
        ForgetChild(child);
        child.Parent = null;
        child.DetachRenderObject();

        if (Owner == null)
        {
            child.Unmount();
            return;
        }

        Owner.Deactivate(child);
    }

    public virtual void UnmountChild(Element child)
    {
        ForgetChild(child);
        if (child.IsActive)
        {
            child.Parent = null;
            child.DetachRenderObject();
            if (child.RenderObject?.Attached != true)
            {
                child.DeactivateRecursively();
            }
        }

        child.Unmount();
    }

    public Element InflateWidget(Widget newWidget, object? newSlot)
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

    public virtual Element? UpdateChild(Element? child, Widget? newWidget, object? newSlot)
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

    public List<Element> UpdateChildren(
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

        bool haveOldChildren = oldChildrenTop <= oldChildrenBottom;
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

    /// <summary>
    /// Asserts that this element is still active, so that an ancestor lookup made from it reads a
    /// stable tree. Every ancestor lookup on <see cref="BuildContext"/> runs it first, which is what
    /// makes a lookup from <c>State.Dispose</c> an error rather than a silent stale read.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>Element._debugCheckStateIsActiveForAncestorLookup</c>, which returns
    /// <c>true</c> only so that it can sit inside an <c>assert(...)</c>; C# has no such wrapper, so
    /// this one returns nothing.
    /// </remarks>
    private void DebugCheckStateIsActiveForAncestorLookup()
    {
        if (Constants.KDebugMode && _lifecycleState != ElementLifecycleState.Active)
        {
            throw new FlutterError(
            [
                new ErrorSummary("Looking up a deactivated widget's ancestor is unsafe."),
                new ErrorDescription(
                    "At this point the state of the widget's element tree is no longer stable."),
                new ErrorHint(
                    "To safely refer to a widget's ancestor in its Dispose() method, save a reference "
                    + "to the ancestor by calling DependOnInherited() in the widget's "
                    + "DidChangeDependencies() method."),
            ]);
        }
    }

    public virtual T? DependOnInherited<T>(object? aspect = null) where T : InheritedWidget
    {
        DebugCheckStateIsActiveForAncestorLookup();

        if (LookupInheritedElement(typeof(T)) is { } ancestor)
        {
            return (T)DependOnInheritedElement(ancestor, aspect);
        }

        _hadUnsatisfiedDependencies = true;
        return null;
    }

    /// <summary>
    /// Reads <see cref="InheritedElements"/> for <paramref name="widgetType"/>, which must be the
    /// exact runtime type of the sought widget. Dart parity: <c>_inheritedElements?[T]</c>.
    /// </summary>
    private InheritedElement? LookupInheritedElement(Type widgetType)
    {
        if (InheritedElements is not { } inheritedElements)
        {
            return null;
        }

        return inheritedElements.TryGetValue(widgetType, out InheritedElement? element) ? element : null;
    }

    public virtual RenderObject? RenderObject => null;

    public virtual Element? RenderObjectAttachingChild => null;

    public InheritedWidget DependOnInheritedElement(InheritedElement ancestor, object? aspect = null)
    {
        _dependencies ??= [];
        _dependencies.Add(ancestor);
        ancestor.UpdateDependencies(this, aspect);

        return (InheritedWidget)ancestor.Widget;
    }

    /// <summary>
    /// The size of the render object returned by <see cref="FindRenderObject"/> when it is a
    /// <see cref="RenderBox"/>. Dart parity: <c>BuildContext.size</c>.
    /// </summary>
    public Size? Size => RenderObject is RenderBox box ? box.Size : null;

    public RenderObject? FindRenderObject()
    {
        if (Constants.KDebugMode && _lifecycleState != ElementLifecycleState.Active)
        {
            throw new FlutterError(
            [
                new ErrorSummary("Cannot get renderObject of inactive element."),
                new ErrorDescription(
                    "In order for an element to have a valid renderObject, it must be active, which "
                    + "means it is part of the tree."),
                new ErrorDescription($"Instead, this element is in the {_lifecycleState} state."),
                new ErrorHint(
                    "If you called this method from a State object, consider guarding it with "
                    + "State.Mounted."),
                DescribeElement("The findRenderObject() method was called for the following element"),
            ]);
        }

        return RenderObject;
    }

    /// <summary>
    /// Finds the nearest ancestor whose widget's runtime type is exactly <typeparamref name="T"/>
    /// without registering a dependency. Use this to read a value once without subscribing to
    /// future changes.
    /// </summary>
    public T? GetInherited<T>() where T : InheritedWidget
    {
        return GetElementForInheritedWidgetOfExactType<T>()?.Widget as T;
    }

    /// <summary>
    /// Returns the nearest inherited element whose widget's runtime type is exactly
    /// <typeparamref name="T"/>, without creating a dependency.
    /// </summary>
    public InheritedElement? GetElementForInheritedWidgetOfExactType<T>() where T : InheritedWidget
    {
        DebugCheckStateIsActiveForAncestorLookup();
        return LookupInheritedElement(typeof(T));
    }

    /// <summary>
    /// Returns the nearest ancestor widget whose runtime type is exactly <typeparamref name="T"/>,
    /// without creating a dependency. A subclass of <typeparamref name="T"/> does not match.
    /// </summary>
    public T? FindAncestorWidgetOfExactType<T>() where T : Widget
    {
        DebugCheckStateIsActiveForAncestorLookup();
        for (Element? ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ancestor.Widget.GetType() == typeof(T))
            {
                return (T)ancestor.Widget;
            }
        }

        return null;
    }

    /// <summary>Walks ancestor elements until <paramref name="visitor"/> returns false.</summary>
    public void VisitAncestorElements(Func<Element, bool> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        DebugCheckStateIsActiveForAncestorLookup();
        for (Element? ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (!visitor(ancestor))
            {
                return;
            }
        }
    }

    /// <summary>Returns the nearest ancestor state of type <typeparamref name="T"/>.</summary>
    public T? FindAncestorStateOfType<T>() where T : State
    {
        DebugCheckStateIsActiveForAncestorLookup();
        for (Element? ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ancestor is StatefulElement statefulElement && statefulElement.State is T state)
            {
                return state;
            }
        }

        return null;
    }

    /// <summary>Returns the furthest ancestor state assignable to <typeparamref name="T"/>.</summary>
    public T? FindRootAncestorStateOfType<T>() where T : State
    {
        DebugCheckStateIsActiveForAncestorLookup();
        T? result = null;
        for (Element? ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ancestor is StatefulElement statefulElement && statefulElement.State is T state)
            {
                result = state;
            }
        }

        return result;
    }

    /// <summary>Returns the nearest ancestor render object assignable to <typeparamref name="T"/>.</summary>
    public T? FindAncestorRenderObjectOfType<T>() where T : RenderObject
    {
        DebugCheckStateIsActiveForAncestorLookup();
        for (Element? ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ancestor is RenderObjectElement { RenderObject: T renderObject })
            {
                return renderObject;
            }
        }

        return null;
    }

    /// <summary>Visits each direct child element of this build context.</summary>
    public void VisitChildElements(Action<Element> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        VisitChildren(visitor);
    }

    /// <summary>Starts bubbling <paramref name="notification"/> at this element.</summary>
    /// <remarks>Flutter's <c>Element.dispatchNotification</c>.</remarks>
    public void DispatchNotification(Notification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _ = notification.Dispatch(this);
    }

    /// <summary>
    /// Whether this element has been unmounted. Only meaningful in debug builds, where the
    /// lifecycle state is tracked.
    /// </summary>
    /// <remarks>Flutter's <c>Element.debugIsDefunct</c>.</remarks>
    public bool DebugIsDefunct =>
        Constants.KDebugMode && _lifecycleState == ElementLifecycleState.Defunct;

    /// <summary>Whether this element is part of the tree. Debug builds only.</summary>
    /// <remarks>Flutter's <c>Element.debugIsActive</c>.</remarks>
    public bool DebugIsActive =>
        Constants.KDebugMode && _lifecycleState == ElementLifecycleState.Active;

    /// <summary>
    /// Describes what caused this element to be created, by naming each element from this one up to
    /// <paramref name="limit"/> ancestors, joined with a leftwards arrow. A chain that was cut short
    /// ends in a midline ellipsis.
    /// </summary>
    /// <remarks>Flutter's <c>Element.debugGetCreatorChain</c>.</remarks>
    public string DebugGetCreatorChain(int limit)
    {
        var chain = new List<string>();
        Element? node = this;
        while (chain.Count < limit && node != null)
        {
            chain.Add(node.ToStringShort());
            node = node.Parent;
        }

        if (node != null)
        {
            chain.Add("\u22ef");
        }

        return string.Join(" \u2190 ", chain);
    }

    /// <summary>The parent chain from this element back to the root of the tree.</summary>
    /// <remarks>Flutter's <c>Element.debugGetDiagnosticChain</c>.</remarks>
    public List<Element> DebugGetDiagnosticChain()
    {
        var chain = new List<Element> { this };
        Element? node = Parent;
        while (node != null)
        {
            chain.Add(node);
            node = node.Parent;
        }

        return chain;
    }

    /// <inheritdoc />
    public List<DiagnosticsNode> DescribeMissingAncestor(Type expectedAncestorType)
    {
        ArgumentNullException.ThrowIfNull(expectedAncestorType);

        var information = new List<DiagnosticsNode>();
        var ancestors = new List<Element>();
        VisitAncestorElements(element =>
        {
            ancestors.Add(element);
            return true;
        });

        string ancestorName = Diagnostics.DescribeType(expectedAncestorType);
        information.Add(new DiagnosticsProperty<Element>(
            $"The specific widget that could not find a {ancestorName} ancestor was",
            this,
            style: DiagnosticsTreeStyle.ErrorProperty));

        if (ancestors.Count > 0)
        {
            information.Add(DescribeElements("The ancestors of this widget were", ancestors));
        }
        else
        {
            information.Add(new ErrorDescription(
                "This widget is the root of the tree, so it has no ancestors, let alone a "
                + $"\"{ancestorName}\" ancestor."));
        }

        return information;
    }

    /// <summary>Returns a block naming each of <paramref name="elements"/>.</summary>
    /// <remarks>Flutter's <c>Element.describeElements</c>.</remarks>
    public static DiagnosticsNode DescribeElements(string name, IEnumerable<Element> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);

        return new DiagnosticsBlock(
            name: name,
            children: elements.Select(element => new DiagnosticsProperty<Element>(string.Empty, element)),
            allowTruncate: true);
    }

    /// <inheritdoc />
    public DiagnosticsNode DescribeElement(
        string name,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.ErrorProperty)
    {
        return new DiagnosticsProperty<Element>(name, this, style: style);
    }

    /// <inheritdoc />
    public DiagnosticsNode DescribeWidget(
        string name,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.ErrorProperty)
    {
        return new DiagnosticsProperty<Element>(name, this, style: style);
    }

    /// <inheritdoc />
    public DiagnosticsNode DescribeOwnershipChain(string name)
    {
        return new StringProperty(name, DebugGetCreatorChain(10));
    }

    /// <inheritdoc />
    public override string ToStringShort() =>
        DebugIsDefunct ? $"{Diagnostics.DescribeIdentity(this)}(DEFUNCT)" : Widget.ToStringShort();

    /// <inheritdoc />
    public override DiagnosticsNode ToDiagnosticsNode(string? name = null, DiagnosticsTreeStyle? style = null)
        => new ElementDiagnosticableTreeNode(name, this, style);

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.DefaultDiagnosticsTreeStyle = DiagnosticsTreeStyle.Dense;
        if (_lifecycleState != ElementLifecycleState.Initial)
        {
            properties.Add(new ObjectFlagProperty<int>("depth", Depth, ifNull: "no depth"));
        }

        // Dart nulls out `Element._widget` in unmount(); Plumix keeps the field, so the defunct
        // state is what stands in for "no widget" here and in ToStringShort.
        Widget? widget = DebugIsDefunct ? null : Widget;
        properties.Add(new ObjectFlagProperty<Widget>("widget", widget, ifNull: "no widget"));
        properties.Add(new DiagnosticsProperty<Key>(
            "key",
            widget?.Key,
            showName: false,
            defaultValue: DiagnosticsDefaults.NullValue,
            level: DiagnosticLevel.Hidden));
        widget?.DebugFillProperties(properties);
        properties.Add(new FlagProperty("dirty", value: Dirty, ifTrue: "dirty"));
        if (_dependencies is not { Count: > 0 } dependencies)
        {
            return;
        }

        List<InheritedElement> sorted = [.. dependencies];
        sorted.Sort((a, b) => string.CompareOrdinal(a.ToStringShort(), b.ToStringShort()));
        string description = "["
            + string.Join(
                ", ",
                sorted.Select(element => element.Widget.ToDiagnosticsNode(style: DiagnosticsTreeStyle.Sparse)))
            + "]";
        properties.Add(new DiagnosticsProperty<HashSet<InheritedElement>>(
            "dependencies",
            dependencies,
            description: description));
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren()
    {
        var children = new List<DiagnosticsNode>();
        VisitChildren(child => children.Add(child.ToDiagnosticsNode()));
        return children;
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

/// <summary>
/// The <see cref="DiagnosticsNode"/> an <see cref="Element"/> describes itself with: a tree node
/// that also reports the widget's runtime type and whether the element is stateful, so a tooling
/// client can tell the two apart without walking the properties.
/// </summary>
/// <remarks>Flutter's private <c>_ElementDiagnosticableTreeNode</c>.</remarks>
internal sealed class ElementDiagnosticableTreeNode : DiagnosticableTreeNode
{
    public ElementDiagnosticableTreeNode(
        string? name,
        Element value,
        DiagnosticsTreeStyle? style,
        bool stateful = false)
        : base(name, value, style)
    {
        Stateful = stateful;
    }

    /// <summary>Whether the element this node describes is a <see cref="StatefulElement"/>.</summary>
    public bool Stateful { get; }

    /// <inheritdoc />
    public override Dictionary<string, object?> ToJsonMap(DiagnosticsSerializationDelegate serializationDelegate)
    {
        Dictionary<string, object?> json = base.ToJsonMap(serializationDelegate);
        var element = (Element)TypedValue;
        if (!element.DebugIsDefunct)
        {
            json["widgetRuntimeType"] = Diagnostics.DescribeType(element.Widget.GetType());
        }

        json["stateful"] = Stateful;
        return json;
    }
}

public sealed class StatelessElement : Element
{
    private Element? _child;

    public StatelessElement(StatelessWidget widget) : base(widget)
    {
    }

    public override RenderObject? RenderObject => _child?.RenderObject;

    public override Element? RenderObjectAttachingChild => _child;

    protected override void OnMount()
    {
        base.OnMount();
        Rebuild();
    }

    protected override void PerformRebuild()
    {
        Widget childWidget;
        DebugDoingBuild = true;
        try
        {
            childWidget = ((StatelessWidget)Widget).Build(this);
        }
        finally
        {
            DebugDoingBuild = false;

            // Dart clears the dirty flag only after build() has run, so a MarkNeedsBuild made while
            // building is swallowed instead of scheduling a second pass.
            base.PerformRebuild();
        }

        _child = UpdateChild(_child, childWidget, Slot);
    }

    public override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        Rebuild(force: true);
    }

    public override void VisitChildren(Action<Element> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    public override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _child))
        {
            _child = null;
        }
    }

    public override void Unmount()
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

    public override Element? RenderObjectAttachingChild => _child;

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
        State.ActivateTickerProvider();
        State.Activate();
    }

    protected override void OnDeactivate()
    {
        State.Deactivate();
        base.OnDeactivate();
    }

    protected override void PerformRebuild()
    {
        if (_didChangeDependencies)
        {
            State.DidChangeDependencies();
            _didChangeDependencies = false;
        }

        Widget widget;
        DebugDoingBuild = true;
        try
        {
            widget = State.Build(this);
        }
        finally
        {
            DebugDoingBuild = false;
            base.PerformRebuild();
        }

        _child = UpdateChild(_child, widget, Slot);
    }

    public override void Update(Widget newWidget)
    {
        var old = (StatefulWidget)Widget;
        base.Update(newWidget);
        State.DidUpdateWidget(old);
        Rebuild(force: true);
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        _didChangeDependencies = true;
    }

    public override void Reassemble()
    {
        State.Reassemble();
        base.Reassemble();
    }

    public override void VisitChildren(Action<Element> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    public override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _child))
        {
            _child = null;
        }
    }

    /// <inheritdoc />
    public override DiagnosticsNode ToDiagnosticsNode(string? name = null, DiagnosticsTreeStyle? style = null)
        => new ElementDiagnosticableTreeNode(name, this, style, stateful: true);

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<State>("state", State, defaultValue: DiagnosticsDefaults.NullValue));
    }

    public override void Unmount()
    {
        if (_child != null)
        {
            UnmountChild(_child);
            _child = null;
        }

        try
        {
            State.Dispose();
        }
        finally
        {
            State.DisposeTickerProvider();
        }
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

    /// <summary>
    /// Adds this element to the inherited scope under the exact runtime type of its widget, so a
    /// lookup for a base type does not find it and a lookup for a subclass does not find a base.
    /// </summary>
    /// <remarks>Flutter's <c>InheritedElement._updateInheritance</c>.</remarks>
    private protected override void UpdateInheritance()
    {
        ImmutableDictionary<Type, InheritedElement> incomingWidgets =
            Parent?.InheritedElements ?? ImmutableDictionary<Type, InheritedElement>.Empty;
        InheritedElements = incomingWidgets.SetItem(Widget.GetType(), this);
    }

    public override RenderObject? RenderObject => _child?.RenderObject;

    public override Element? RenderObjectAttachingChild => _child;

    protected override void OnMount()
    {
        base.OnMount();
        Rebuild();
    }

    protected override void PerformRebuild()
    {
        Widget child;
        DebugDoingBuild = true;
        try
        {
            child = ((InheritedWidget)Widget).Build(this);
        }
        finally
        {
            DebugDoingBuild = false;
            base.PerformRebuild();
        }

        _child = UpdateChild(_child, child, Slot);
    }

    public override void Update(Widget newWidget)
    {
        var old = (InheritedWidget)Widget;
        base.Update(newWidget);
        if (((InheritedWidget)newWidget).InvokeUpdateShouldNotify(old))
        {
            NotifyClients(old);
        }

        Rebuild(force: true);
    }

    protected object? GetDependencies(Element dependent)
    {
        _dependents.TryGetValue(dependent, out object? dependencies);
        return dependencies;
    }

    protected void SetDependencies(Element dependent, object? value)
    {
        _dependents[dependent] = value;
    }

    public virtual void UpdateDependencies(Element dependent, object? aspect)
    {
        SetDependencies(dependent, value: null);
    }

    public virtual void RemoveDependent(Element dependent)
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

    public virtual void NotifyDependent(InheritedWidget _, Element dependent)
    {
        dependent.DidChangeDependencies();
    }

    public override void VisitChildren(Action<Element> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    public override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _child))
        {
            _child = null;
        }
    }

    public override void Unmount()
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

    public override void UpdateDependencies(Element dependent, object? aspect)
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

    public override void NotifyDependent(InheritedWidget oldWidget, Element dependent)
    {
        var dependencies = GetDependencies(dependent) as HashSet<TAspect>;
        if (dependencies == null)
        {
            return;
        }

        if (dependencies.Count == 0
            || InheritedModelWidget.InvokeUpdateShouldNotifyDependent((InheritedModel<TAspect>)oldWidget, dependencies))
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

    public override void Update(Widget newWidget)
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

    protected override void PerformRebuild()
    {
        if (_dirty)
        {
            NotifyClients(InheritedNotifierWidget);
            _dirty = false;
        }

        base.PerformRebuild();
    }

    public override void Unmount()
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

    public override Element? RenderObjectAttachingChild => _child;

    protected override void OnMount()
    {
        base.OnMount();
        Rebuild();
    }

    protected override void PerformRebuild()
    {
        Widget child = ((ProxyWidget)Widget).Child;
        base.PerformRebuild();
        _child = UpdateChild(_child, child, Slot);
    }

    public override void Update(Widget newWidget)
    {
        var old = (ProxyWidget)Widget;
        base.Update(newWidget);
        Updated(old);
        Rebuild(force: true);
    }

    protected virtual void Updated(ProxyWidget oldWidget)
    {
    }

    public override void VisitChildren(Action<Element> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    public override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _child))
        {
            _child = null;
        }
    }

    public override void Unmount()
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

    protected override void PerformRebuild()
    {
        base.PerformRebuild();
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
