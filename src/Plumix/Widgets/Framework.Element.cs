using System.Collections.Immutable;
using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/framework.dart (approximate)

namespace Plumix.Widgets;

/// <summary>
/// Dart's private <c>_NullWidget</c>: the configuration of the shared placeholder element that
/// pre-fills the result list in <see cref="Element.UpdateChildren"/>.
/// </summary>
internal sealed class NullWidget : Widget
{
    public override Element CreateElement() => throw new NotImplementedException();
}

/// <summary>
/// Dart's private <c>_NullElement</c>: a single shared placeholder used to fill a
/// <c>List&lt;Element&gt;</c> before the real elements are known, so a hole left by a bug shows up as
/// this element rather than as a null.
/// </summary>
internal sealed class NullElement : Element
{
    private NullElement() : base(new NullWidget())
    {
    }

    public static readonly NullElement Instance = new();
}

public sealed class IndexedSlot<T>
{
    public IndexedSlot(int index, T? value)
    {
        Index = index;
        Value = value;
    }

    public int Index { get; }

    public T? Value { get; }

    /// <summary>
    /// Dart's <c>IndexedSlot.operator ==</c>: equal when the runtime type, the index and the
    /// (identity of the) previous sibling all match. Without it every rebuild hands
    /// <see cref="Element.UpdateChild"/> a slot that compares unequal to the one already stored, so
    /// every child of a multi-child render object is moved on every rebuild.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is IndexedSlot<T> other
            && other.Index == Index
            && EqualityComparer<T?>.Default.Equals(other.Value, Value);
    }

    /// <summary>Dart's <c>IndexedSlot.hashCode</c>: <c>Object.hash(index, value)</c>.</summary>
    public override int GetHashCode() => HashCode.Combine(Index, Value);
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

    /// <summary>
    /// The element hit an unrecoverable error while being rebuilt or while being incorporated into
    /// the tree, so its subtree is inconsistent and must never be re-incorporated. Dart's
    /// <c>_ElementLifecycle.failed</c>: final and irreversible, and reached on a best-effort basis
    /// without surfacing further errors.
    /// </summary>
    Failed,
    Defunct
}

public abstract class Element : DiagnosticableTree, BuildContext
{
    private static int _nextElementId;

    private ElementLifecycleState _lifecycleState = ElementLifecycleState.Initial;
    private HashSet<InheritedElement>? _dependencies;
    private HashSet<Element>? _debugForgottenChildrenWithGlobalKey;
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
    private int _depth;

    /// <summary>
    /// An integer guaranteed to be greater than the parent's. Dart's <c>Element.depth</c> throws
    /// before the element is mounted, because the value is only assigned by <see cref="Mount"/>.
    /// </summary>
    public int Depth
    {
        get
        {
            if (Constants.KDebugMode && _lifecycleState == ElementLifecycleState.Initial)
            {
                throw new FlutterError("Depth is only available when element has been mounted.");
            }

            return _depth;
        }

        private set => _depth = value;
    }
    public object? Slot { get; private set; }

    internal int SequenceId { get; } = Interlocked.Increment(ref _nextElementId);

    /// <summary>
    /// Whether this element needs rebuilding. Dart parity: the private <c>Element._dirty</c>, which
    /// starts out true because a freshly created element has never been built.
    /// </summary>
    public bool Dirty { get; private set; } = true;

    /// <summary>Dart's <c>Element._debugBuiltOnce</c>, read by <c>debugPrintRebuildDirtyWidgets</c>.</summary>
    private bool _debugBuiltOnce;

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

        try
        {
            OnDeactivate();
        }
        catch (Exception)
        {
            // Dart's _InactiveElements._deactivateRecursively forces the whole subtree into the
            // failed state and rethrows, so a throwing deactivate() leaves the element neither
            // active nor defunct, and it never reaches the inactive list.
            DeactivateFailedSubtreeRecursively(this);
            throw;
        }

        VisitChildren(child => child.DeactivateRecursively(isRoot: false));
        RemoveDependencies();

        if (isRoot)
        {
            Parent = null;
        }

        InheritedElements = null;
        _lifecycleState = ElementLifecycleState.Inactive;

        if (Constants.KDebugMode)
        {
            DebugDeactivated();
            if (WidgetsDebug.DebugPrintGlobalKeyedWidgetLifecycle && Widget.Key is GlobalKey)
            {
                Print.DebugPrint($"Deactivated {this}");
            }
        }

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

        if (Constants.KDebugMode)
        {
            WidgetsDebug.DebugOnRebuildDirtyWidget?.Invoke(this, _debugBuiltOnce);
            if (WidgetsDebug.DebugPrintRebuildDirtyWidgets)
            {
                Print.DebugPrint(_debugBuiltOnce ? $"Rebuilding {this}" : $"Building {this}");
            }

            _debugBuiltOnce = true;
        }

        BuildOwner? owner = Owner;
        Element? previousBuildTarget = owner?.DebugCurrentBuildTarget;
        if (owner is not null)
        {
            owner.DebugCurrentBuildTarget = this;
        }

        try
        {
            PerformRebuild();
        }
        finally
        {
            if (owner is not null)
            {
                owner.DebugElementWasRebuilt(this);
                owner.DebugCurrentBuildTarget = previousBuildTarget;
            }
        }
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
        if (_lifecycleState != ElementLifecycleState.Active)
        {
            return;
        }

        DebugCheckCanMarkNeedsBuild();

        if (Dirty)
        {
            return;
        }

        Dirty = true;
        Owner?.ScheduleBuild(this);
    }

    /// <summary>
    /// Dart's debug block inside <c>Element.markNeedsBuild</c>: dirtying an element that the current
    /// build will not reach afterwards, or dirtying anything at all while the tree is locked, is a
    /// mistake the framework can name precisely.
    /// </summary>
    private void DebugCheckCanMarkNeedsBuild()
    {
        if (!Constants.KDebugMode || Owner is not { } owner)
        {
            return;
        }

        // Dart also rejects a markNeedsBuild() during build when the element is not a descendant of
        // the element currently being built. That branch is not armed yet: Plumix has no per-BuildScope
        // dirty list, and `TransitionRoute.HandleStatusChanged` re-enters `NavigatorState.SetState`
        // from an animation status callback that can run inside a build. See docs/ai/BACKLOG.md.
        if (owner.DebugStateLocked)
        {
            throw new FlutterError(
            [
                new ErrorSummary("setState() or markNeedsBuild() called when widget tree was locked."),
                new ErrorDescription(
                    $"This {Diagnostics.DescribeType(Widget.GetType())} widget cannot be marked as needing to "
                    + "build because the framework is locked."),
                DescribeElement("The widget on which setState() or markNeedsBuild() was called was"),
            ]);
        }
    }

    /// <summary>Dart's <c>Element._debugIsDescendantOf</c>.</summary>
    private bool IsDescendantOf(Element target)
    {
        Element? element = this;
        while (element != null && element.Depth > target.Depth)
        {
            element = element.Parent;
        }

        return ReferenceEquals(element, target);
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
        if (Constants.KDebugMode && _debugForgottenChildrenWithGlobalKey is { Count: > 0 } forgotten)
        {
            foreach (Element child in forgotten)
            {
                Owner?.DebugRemoveGlobalKeyReservationFor(this, child);
            }

            forgotten.Clear();
        }

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

    /// <summary>
    /// Dart's <c>Element.forgetChild</c>. The reservation of a forgotten global-keyed child cannot be
    /// released here — the child is only really gone once this element is updated — so it is parked
    /// until <see cref="Update"/> runs.
    /// </summary>
    public virtual void ForgetChild(Element child)
    {
        if (Constants.KDebugMode && child.Widget.Key is GlobalKey)
        {
            (_debugForgottenChildrenWithGlobalKey ??= []).Add(child);
        }
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
        Element newChild = inactiveElement ?? newWidget.CreateElement();
        try
        {
            if (inactiveElement != null)
            {
                inactiveElement.ActivateWithParent(this, newSlot);
                if (!ReferenceEquals(inactiveElement.Widget, newWidget))
                {
                    inactiveElement.Update(newWidget);
                }

                return inactiveElement;
            }

            newChild.Attach(owner);
            newChild.Mount(this, newSlot);
            return newChild;
        }
        catch (Exception)
        {
            // Dart's inflateWidget: attempt some clean-up if activation or mount fails, so the tree
            // is left in a reasonable state, then rethrow.
            DeactivateFailedChildSilently(newChild);
            throw;
        }
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

                DebugReserveGlobalKey(child, newWidget, child);
                return child;
            }

            if (Widget.CanUpdate(child.Widget, newWidget))
            {
                if (!Equals(child.Slot, newSlot))
                {
                    UpdateSlotForChild(child, newSlot);
                }

                child.Update(newWidget);
                Owner?.DebugElementWasRebuilt(child);
                DebugReserveGlobalKey(child, newWidget, child);
                return child;
            }

            DeactivateChild(child);
        }

        Element inflated = InflateWidget(newWidget, newSlot);
        DebugReserveGlobalKey(child, newWidget, inflated);
        return inflated;
    }

    /// <summary>
    /// Dart's trailing assert in <c>Element.updateChild</c>: release the reservation the outgoing
    /// child held, then reserve the incoming widget's global key against this parent, so
    /// <c>BuildOwner.finalizeTree</c> can spot a key claimed by two parents in one frame.
    /// </summary>
    private void DebugReserveGlobalKey(Element? oldChild, Widget newWidget, Element newChild)
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (oldChild is not null)
        {
            Owner?.DebugRemoveGlobalKeyReservationFor(this, oldChild);
        }

        if (newWidget.Key is GlobalKey globalKey)
        {
            Owner?.DebugReserveGlobalKeyFor(this, newChild, globalKey);
        }
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
        Array.Fill(newChildren, NullElement.Instance);

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

        if (Constants.KDebugMode && Array.IndexOf(newChildren, NullElement.Instance) >= 0)
        {
            throw new AssertionError(
                "UpdateChildren left a placeholder in the child list: every slot must be filled by the "
                + "six-phase diff.");
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

    /// <summary>
    /// The render object at or below this element. Dart's <c>Element.renderObject</c> walks down
    /// <see cref="RenderObjectAttachingChild"/> until it reaches a <see cref="RenderObjectElement"/>,
    /// and gives up at a defunct element or when the chain runs out (an element outside a view).
    /// </summary>
    public virtual RenderObject? RenderObject
    {
        get
        {
            Element? current = this;
            while (current is not null)
            {
                if (current._lifecycleState == ElementLifecycleState.Defunct)
                {
                    break;
                }

                if (current is RenderObjectElement renderObjectElement)
                {
                    return renderObjectElement.RenderObject;
                }

                current = current.RenderObjectAttachingChild;
            }

            return null;
        }
    }

    public virtual Element? RenderObjectAttachingChild => null;

    public virtual InheritedWidget DependOnInheritedElement(InheritedElement ancestor, object? aspect = null)
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
        if (Constants.KDebugMode && Owner is { DebugStateLocked: true })
        {
            throw new FlutterError(
            [
                new ErrorSummary("visitChildElements() called during build."),
                new ErrorDescription(
                    "The BuildContext.visitChildElements() method can't be called during build because the "
                    + "child list is still being updated at that point, so the children might not be "
                    + "constructed yet, or might be old children that are going to be replaced."),
            ]);
        }

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

    /// <summary>
    /// Dart's <c>Element.doesDependOnInheritedElement</c>: whether
    /// <see cref="DependOnInheritedElement"/> was previously called with <paramref name="ancestor"/>.
    /// </summary>
    protected bool DoesDependOnInheritedElement(InheritedElement ancestor)
        => _dependencies?.Contains(ancestor) ?? false;

    /// <summary>
    /// Dart's <c>Element.debugDeactivated</c>: called in debug builds after this element's children
    /// have been deactivated.
    /// </summary>
    public virtual void DebugDeactivated()
    {
        if (Constants.KDebugMode && _lifecycleState != ElementLifecycleState.Inactive)
        {
            throw new AssertionError($"{ToStringShort()} was expected to be inactive when deactivated.");
        }
    }

    /// <summary>
    /// Dart's <c>Element.debugExpectsRenderObjectForSlot</c>: whether the element occupying
    /// <paramref name="slot"/> is expected to attach its render object to an ancestor. Elements that
    /// host an independent render tree in a slot return false for that slot.
    /// </summary>
    public virtual bool DebugExpectsRenderObjectForSlot(object? slot) => true;

    /// <summary>
    /// Dart's <c>Element._deactivateFailedSubtreeRecursively</c>: force a subtree that threw during
    /// activation or rebuild into <see cref="ElementLifecycleState.Failed"/>, best effort, never
    /// surfacing an additional error.
    /// </summary>
    private static void DeactivateFailedSubtreeRecursively(Element element)
    {
        try
        {
            element.OnDeactivate();
        }
        catch (Exception)
        {
            // Dart calls _ensureDeactivated() here; the state assignment below covers it.
        }

        element.EnsureDeactivated();
        element._lifecycleState = ElementLifecycleState.Failed;
        try
        {
            element.VisitChildren(DeactivateFailedSubtreeRecursively);
        }
        catch (Exception)
        {
            // Keep walking siblings even when one child's visitChildren throws.
        }
    }

    /// <summary>
    /// Dart's <c>Element._deactivateFailedChildSilently</c>: used by <see cref="InflateWidget"/> when
    /// mounting or activating a child threw, so the tree is left in a reasonable state.
    /// </summary>
    private void DeactivateFailedChildSilently(Element child)
    {
        try
        {
            child.Parent = null;
            child.DetachRenderObject();
            DeactivateFailedSubtreeRecursively(child);
        }
        catch (Exception)
        {
            // Do not rethrow.
        }
    }

    /// <summary>
    /// Dart's <c>Element._ensureDeactivated</c>: drops the inherited dependencies and marks the
    /// element inactive, even when <c>deactivate()</c> itself threw. The dependency set is
    /// deliberately kept so <see cref="ActivateRecursively"/> can tell it had dependencies.
    /// </summary>
    private void EnsureDeactivated()
    {
        RemoveDependencies();
        InheritedElements = null;
        _lifecycleState = ElementLifecycleState.Inactive;
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

/// <summary>
/// An <see cref="Element"/> that composes other elements: it has exactly one child, produced by
/// <see cref="Build"/>. Dart parity: <c>ComponentElement</c>, the shared base of
/// <see cref="StatelessElement"/>, <see cref="StatefulElement"/> and <see cref="ProxyElement"/>.
/// </summary>
public abstract class ComponentElement : Element
{
    private Element? _child;

    protected ComponentElement(Widget widget) : base(widget)
    {
    }

    public override Element? RenderObjectAttachingChild => _child;

    protected override void OnMount()
    {
        base.OnMount();
        if (_child is not null)
        {
            throw new AssertionError("A ComponentElement must not have a child before it is mounted.");
        }

        FirstBuild();
        if (_child is null)
        {
            throw new AssertionError("A ComponentElement must have a child once it has been mounted.");
        }
    }

    /// <summary>Dart's <c>ComponentElement._firstBuild</c>.</summary>
    private protected virtual void FirstBuild()
    {
        Rebuild();
    }

    /// <summary>
    /// Produces the child widget. Dart's <c>ComponentElement.build</c>: subclasses delegate to
    /// <c>StatelessWidget.build</c>, <c>State.build</c> or <c>ProxyWidget.child</c>.
    /// </summary>
    protected abstract Widget Build();

    /// <summary>
    /// Dart's <c>ComponentElement.performRebuild</c>. A throwing <see cref="Build"/> is reported and
    /// replaced by <see cref="ErrorWidget.Builder"/> rather than propagating, and the dirty flag is
    /// cleared only after <see cref="Build"/> ran, so a <see cref="Element.MarkNeedsBuild"/> issued
    /// during the build is ignored instead of scheduling a second pass.
    /// </summary>
    protected override void PerformRebuild()
    {
        Widget built;
        try
        {
            DebugDoingBuild = true;
            built = Build();
            DebugDoingBuild = false;
            WidgetsDebug.DebugWidgetBuilderValue(Widget, built);
        }
        catch (Exception exception)
        {
            DebugDoingBuild = false;
            built = ErrorWidget.Builder(ReportBuildException(exception));
        }
        finally
        {
            base.PerformRebuild();
        }

        try
        {
            _child = UpdateChild(_child, built, Slot);
        }
        catch (Exception exception)
        {
            built = ErrorWidget.Builder(ReportBuildException(exception));
            try
            {
                if (_child is not null)
                {
                    DeactivateChild(_child);
                }
            }
            catch (Exception)
            {
                // Dart swallows this: the old subtree is already broken, and reporting a second
                // failure here would bury the original one.
            }

            _child = UpdateChild(null, built, Slot);
        }
    }

    private FlutterErrorDetails ReportBuildException(Exception exception)
    {
        return FrameworkErrors.ReportException(
            new ErrorDescription($"building {this}"),
            exception,
            informationCollector: () => Constants.KDebugMode
                ? [new DiagnosticsDebugCreator(new DebugCreator(this))]
                : []);
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
        base.ForgetChild(child);
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

public class StatelessElement : ComponentElement
{
    public StatelessElement(StatelessWidget widget) : base(widget)
    {
    }

    protected override Widget Build() => ((StatelessWidget)Widget).Build(this);

    public override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        Rebuild(force: true);
    }
}

public class StatefulElement : ComponentElement
{
    private bool _didChangeDependencies;

    public State State { get; private set; }

    public StatefulElement(StatefulWidget widget) : base(widget)
    {
        State = widget.CreateState();
        State.AttachElement(this, widget);
    }

    protected override Widget Build() => State.Build(this);

    private protected override void FirstBuild()
    {
        State.RunInitState();
        State.DidChangeDependencies();
        State.MarkReady();
        base.FirstBuild();
    }

    protected override void OnActivate()
    {
        base.OnActivate();
        State.ActivateTickerProvider();
        State.Activate();
        MarkNeedsBuild();
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

        base.PerformRebuild();
    }

    public override void Update(Widget newWidget)
    {
        var old = (StatefulWidget)Widget;
        base.Update(newWidget);
        State.SetWidget((StatefulWidget)newWidget);
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

    public override InheritedWidget DependOnInheritedElement(InheritedElement ancestor, object? aspect = null)
    {
        State.DebugCheckCanDependOnInherited(ancestor, this);
        return base.DependOnInheritedElement(ancestor, aspect);
    }

    public override DiagnosticsNode ToDiagnosticsNode(string? name = null, DiagnosticsTreeStyle? style = null)
        => new ElementDiagnosticableTreeNode(name, this, style, stateful: true);

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<State>("state", State, defaultValue: DiagnosticsDefaults.NullValue));
    }

    public override void Unmount()
    {
        base.Unmount();
        try
        {
            State.Dispose();
            State.DebugAssertDisposedCalledSuper();
        }
        finally
        {
            State.DisposeTickerProvider();
            State.DetachElement();
        }
    }
}

/// <summary>
/// An <see cref="Element"/> that uses a <see cref="ProxyWidget"/> as its configuration and simply
/// inflates that widget's child. Dart parity: <c>ProxyElement</c>.
/// </summary>
public abstract class ProxyElement : ComponentElement
{
    protected ProxyElement(ProxyWidget widget) : base(widget)
    {
    }

    protected override Widget Build() => ((ProxyWidget)Widget).Child;

    public override void Update(Widget newWidget)
    {
        var old = (ProxyWidget)Widget;
        base.Update(newWidget);
        Updated(old);
        Rebuild(force: true);
    }

    /// <summary>
    /// Dart's <c>ProxyElement.updated</c>: called when the widget changed, before this element is
    /// rebuilt. The default forwards to <see cref="NotifyClients"/>.
    /// </summary>
    protected virtual void Updated(ProxyWidget oldWidget)
    {
        NotifyClients(oldWidget);
    }

    /// <summary>Dart's <c>ProxyElement.notifyClients</c>.</summary>
    protected abstract void NotifyClients(ProxyWidget oldWidget);
}

/// <summary>
/// The non-generic half of <see cref="ParentDataElement{T}"/>, so that
/// <see cref="RenderObjectElement"/> can walk its parent-data ancestors without knowing the
/// <c>ParentData</c> type argument. C#-only: Dart reaches the same members through
/// <c>ParentDataElement&lt;ParentData&gt;</c>.
/// </summary>
public abstract class ParentDataElementBase : ProxyElement
{
    private protected ParentDataElementBase(ProxyWidget widget) : base(widget)
    {
    }

    internal abstract IParentDataWidget ParentDataWidget { get; }

    /// <summary>Dart's <c>ParentDataElement.debugParentDataType</c>.</summary>
    internal abstract Type DebugParentDataType { get; }
}

public sealed class ParentDataElement<T> : ParentDataElementBase where T : IParentData
{
    public ParentDataElement(ParentDataWidget<T> widget) : base(widget)
    {
    }

    internal override IParentDataWidget ParentDataWidget => (IParentDataWidget)Widget;

    internal override Type DebugParentDataType
    {
        get
        {
            if (!Constants.KDebugMode)
            {
                throw new NotSupportedException("DebugParentDataType is only supported in debug builds");
            }

            return typeof(T);
        }
    }

    protected override void NotifyClients(ProxyWidget oldWidget)
    {
        ApplyParentData((ParentDataWidget<T>)Widget);
    }

    /// <summary>
    /// Dart's <c>ParentDataElement.applyWidgetOutOfTurn</c>: applies the parent data of a widget that
    /// is not this element's configuration, outside the build phase. Only legal for widgets whose
    /// <see cref="ParentDataWidget{T}.DebugCanApplyOutOfTurn"/> is true, and only when the widget
    /// wraps the same child.
    /// </summary>
    public void ApplyWidgetOutOfTurn(ParentDataWidget<T> newWidget)
    {
        if (Constants.KDebugMode)
        {
            if (!newWidget.DebugCanApplyOutOfTurn())
            {
                throw new AssertionError(
                    $"{Diagnostics.ObjectRuntimeType(newWidget, "ParentDataWidget")} does not allow its parent "
                    + "data to be applied out of turn.");
            }

            if (!ReferenceEquals(newWidget.Child, ((ParentDataWidget<T>)Widget).Child))
            {
                throw new AssertionError(
                    "applyWidgetOutOfTurn can only be used with a widget that wraps the same child.");
            }
        }

        ApplyParentData(newWidget);
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

public class InheritedElement : ProxyElement
{
    private readonly Dictionary<Element, object?> _dependents = [];

    public InheritedElement(InheritedWidget widget) : base(widget)
    {
    }

    private protected override void UpdateInheritance()
    {
        ImmutableDictionary<Type, InheritedElement> incomingWidgets =
            Parent?.InheritedElements ?? ImmutableDictionary<Type, InheritedElement>.Empty;
        InheritedElements = incomingWidgets.SetItem(Widget.GetType(), this);
    }

    /// <summary>
    /// Dart's <c>InheritedElement.updated</c>: notify the dependents only when the widget says the
    /// change is observable.
    /// </summary>
    protected override void Updated(ProxyWidget oldWidget)
    {
        if (((InheritedWidget)Widget).InvokeUpdateShouldNotify((InheritedWidget)oldWidget))
        {
            base.Updated(oldWidget);
        }
    }

    /// <summary>Dart's <c>InheritedElement.debugDeactivated</c>: every dependent must have unregistered.</summary>
    public override void DebugDeactivated()
    {
        base.DebugDeactivated();
        if (Constants.KDebugMode && _dependents.Count > 0)
        {
            throw new AssertionError(
                $"{Diagnostics.ObjectRuntimeType(this, "InheritedElement")} still has "
                + $"{_dependents.Count} dependent(s) after being deactivated.");
        }
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

    /// <summary>Dart's <c>InheritedElement.notifyClients</c>.</summary>
    protected override void NotifyClients(ProxyWidget oldWidget)
    {
        if (_dependents.Count == 0)
        {
            return;
        }

        var inheritedOldWidget = (InheritedWidget)oldWidget;
        foreach (var dependent in _dependents.Keys.ToArray())
        {
            NotifyDependent(inheritedOldWidget, dependent);
        }
    }

    public virtual void NotifyDependent(InheritedWidget _, Element dependent)
    {
        dependent.DidChangeDependencies();
    }

    public override void Unmount()
    {
        base.Unmount();
        _dependents.Clear();
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
