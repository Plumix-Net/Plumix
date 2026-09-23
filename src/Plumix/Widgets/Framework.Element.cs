using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart

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

    public override bool DebugDoingBuild => throw new NotImplementedException();
}

/// <summary>
/// A value for <see cref="Element.Slot"/> used for children of <see cref="MultiChildRenderObjectElement"/>s:
/// the index of the child and the element that comes before it.
/// </summary>
/// <remarks>Dart's <c>IndexedSlot&lt;T extends Element?&gt;</c>.</remarks>
public sealed class IndexedSlot<T> where T : Element?
{
    public IndexedSlot(int index, T value)
    {
        Index = index;
        Value = value;
    }

    /// <summary>Information to define where the child occupying this slot fits in its parent's child list.</summary>
    public T Value { get; }

    /// <summary>The index of this slot in the parent's child list.</summary>
    public int Index { get; }

    /// <summary>
    /// Dart's <c>IndexedSlot.operator ==</c>: equal when the runtime type, the index and the
    /// (identity of the) previous sibling all match. Without it every rebuild hands
    /// <see cref="Element.UpdateChild"/> a slot that compares unequal to the one already stored, so
    /// every child of a multi-child render object is moved on every rebuild.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj?.GetType() != GetType())
        {
            return false;
        }

        return obj is IndexedSlot<T> other
            && Index == other.Index
            && ReferenceEquals(Value, other.Value);
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
    T? DependOnInheritedWidgetOfExactType<T>(object? aspect = null) where T : InheritedWidget;

    /// <summary>Registers this context as depending on <paramref name="ancestor"/>.</summary>
    InheritedWidget DependOnInheritedElement(InheritedElement ancestor, object? aspect = null);

    /// <summary>
    /// Finds the nearest ancestor whose widget's runtime type is exactly <typeparamref name="T"/>
    /// without registering a dependency. Use this to read a value once without subscribing to
    /// future changes.
    /// </summary>
    T? GetInheritedWidgetOfExactType<T>() where T : InheritedWidget;

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
    private Widget? _widget;
    private NotificationNode? _notificationTree;
    private HashSet<InheritedElement>? _dependencies;
    private HashSet<Element>? _debugForgottenChildrenWithGlobalKey;
    private bool _hadUnsatisfiedDependencies;

    protected Element(Widget widget)
    {
        _widget = widget;
        if (Constants.KDebugMode)
        {
            _debugForgottenChildrenWithGlobalKey = [];
            FoundationDebug.DebugMaybeDispatchCreated("widgets", "Element", this);
        }
    }

    /// <summary>
    /// The inherited elements in scope at this element, keyed by the exact runtime type of their
    /// widget. Rebuilt by <see cref="UpdateInheritance"/> on mount and activation, and dropped on
    /// deactivation. Dart parity: <c>Element._inheritedElements</c>, a
    /// <c>PersistentHashMap&lt;Type, InheritedElement&gt;</c> — an immutable map is the same
    /// contract, since the only writes are whole-field assignments and every element in a subtree
    /// shares the map instance its nearest <see cref="InheritedElement"/> ancestor produced.
    /// </summary>
    internal ImmutableDictionary<Type, InheritedElement>? InheritedElements { get; private protected set; }

    /// <summary>The current configuration; reading it after unmount throws.</summary>
    /// <remarks>Flutter's <c>Element.widget</c>.</remarks>
    public Widget Widget =>
        _widget ?? throw new InvalidOperationException("This element has been unmounted and no longer has a widget.");

    public Element? Parent { get; private set; }

    /// <summary>Dart's <c>Element.operator ==</c>, which is <c>@nonVirtual</c> identity.</summary>
    public sealed override bool Equals(object? obj) => ReferenceEquals(this, obj);

    /// <summary>Elements use the identity hash code, like Dart's.</summary>
    public sealed override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    /// <summary>
    /// Information set by the parent to define where this child fits in its parent's child list.
    /// </summary>
    public object? Slot { get; private protected set; }

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
    }

    internal int SequenceId { get; } = Interlocked.Increment(ref _nextElementId);

    /// <summary>
    /// Dart's <c>Element._sort</c>: shallow before deep, and at equal depth clean before dirty, so a
    /// cursor walking the dirty list never has a still-dirty element behind it.
    /// </summary>
    internal static int Sort(Element a, Element b)
    {
        int diff = a.Depth - b.Depth;
        if (diff != 0)
        {
            return diff;
        }

        bool isBDirty = b.Dirty;
        if (a.Dirty != isBDirty)
        {
            return isBDirty ? -1 : 1;
        }

        // Dart returns 0 here and leaves the order to its sort. `List<T>.Sort` is not stable, so the
        // creation order is the final tiebreak; it keeps a rebuild pass reproducible.
        return a.SequenceId.CompareTo(b.SequenceId);
    }

    /// <summary>
    /// Dart's <c>Element._debugConcreteSubtype</c>: the encoding <see cref="UpdateChild"/> compares
    /// with <c>Widget._debugConcreteSubtype</c> to notice a hot reload that turned a stateful widget
    /// into a stateless one (or back).
    /// </summary>
    internal static int DebugConcreteSubtype(Element element)
    {
        return element is StatefulElement
            ? 1
            : element is StatelessElement
                ? 2
                : 0;
    }

    /// <summary>Dart's <c>_ElementLifecycle.name</c>.</summary>
    internal static string DebugLifecycleName(ElementLifecycleState state) => state switch
    {
        ElementLifecycleState.Initial => "initial",
        ElementLifecycleState.Active => "active",
        ElementLifecycleState.Inactive => "inactive",
        ElementLifecycleState.Failed => "failed",
        _ => "defunct",
    };

    /// <summary>Whether this element is currently mounted: Dart's <c>Element.mounted</c>.</summary>
    public bool Mounted => _widget is not null;

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

    /// <summary>The object that manages the lifecycle of this element.</summary>
    public BuildOwner? Owner { get; private set; }

    private BuildScope? _parentBuildScope;

    /// <summary>
    /// The <see cref="Widgets.BuildScope"/> this element is rebuilt in. Dart's
    /// <c>Element.buildScope</c>: the parent's scope by default, so a whole tree normally shares the
    /// one the root was attached with. An override must return the same instance every time — the
    /// scope of a mounted element is not allowed to change identity.
    /// </summary>
    public virtual BuildScope BuildScope =>
        _parentBuildScope ?? throw new FlutterError("BuildScope is only available once the element is attached.");

    /// <summary>Dart's <c>element._parentBuildScope != null</c>.</summary>
    internal bool HasParentBuildScope => _parentBuildScope is not null;

    public bool IsActive => _lifecycleState == ElementLifecycleState.Active;
    internal bool IsInactive => _lifecycleState == ElementLifecycleState.Inactive;
    internal ElementLifecycleState LifecycleState => _lifecycleState;

    /// <summary>Whether this element is currently building its widget or render object.</summary>
    /// <remarks>
    /// Flutter's <c>BuildContext.debugDoingBuild</c>, which <c>Element</c> leaves abstract;
    /// <see cref="ComponentElement"/> and <see cref="RenderObjectElement"/> implement it.
    /// </remarks>
    public virtual bool DebugDoingBuild => false;

    /// <summary>
    /// Assigns <paramref name="owner"/> and a fresh build scope to a parentless element.
    /// </summary>
    /// <remarks>
    /// The body of Dart's <c>RootElementMixin.assignOwner</c>, available to every element because C#
    /// has no mixins. Each root receives its own scope even when roots share an owner.
    /// </remarks>
    internal void Attach(BuildOwner owner)
    {
        Owner = owner;
        _parentBuildScope = new BuildScope();
    }

    /// <summary>
    /// Dart's <c>Element._updateBuildScopeRecursively</c>: after a reparent, re-reads the scope from
    /// the new parent and pushes the change down until a subtree already agrees with its parent.
    /// Clearing <see cref="InDirtyList"/> is what lets the element join the new scope's list; the
    /// stale entry left in the old one is skipped and dropped when that scope flushes.
    /// </summary>
    private void UpdateBuildScopeRecursively()
    {
        if (ReferenceEquals(BuildScope, Parent?.BuildScope))
        {
            return;
        }

        InDirtyList = false;
        _parentBuildScope = Parent?.BuildScope;
        VisitChildren(static child => child.UpdateBuildScopeRecursively());
    }

    /// <summary>
    /// Called whenever the application is reassembled during debugging, for example during hot
    /// reload. Dart's <c>Element.reassemble</c>.
    /// </summary>
    public virtual void Reassemble()
    {
        MarkNeedsBuild();
        VisitChildren(child => child.Reassemble());
    }

    /// <summary>Dart's <c>Element._debugIsDescendantOf</c>.</summary>
    internal bool DebugIsDescendantOf(Element target)
    {
        Element? element = this;
        while (element != null && element.Depth > target.Depth)
        {
            element = element.Parent;
        }

        return ReferenceEquals(element, target);
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

    /// <summary>
    /// The child of this element that will insert a render object into an ancestor of this element.
    /// Dart's <c>Element.renderObjectAttachingChild</c>: by default the only child, asserted to be
    /// the only one.
    /// </summary>
    public virtual Element? RenderObjectAttachingChild
    {
        get
        {
            Element? next = null;
            VisitChildren(child =>
            {
                DebugAssertions.Assert(next is null);
                next = child;
            });
            return next;
        }
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

    public virtual void VisitChildren(Action<Element> visitor)
    {
    }

    /// <summary>
    /// Walks the children that are on stage, i.e. the ones the widget inspector shows. Defaults to
    /// every child; widgets that hide part of the tree (such as <see cref="Offstage"/>) override it.
    /// </summary>
    /// <remarks>Flutter's <c>Element.debugVisitOnstageChildren</c>.</remarks>
    public virtual void DebugVisitOnstageChildren(Action<Element> visitor) => VisitChildren(visitor);

    /// <summary>Visits each direct child element of this build context.</summary>
    public virtual void VisitChildElements(Action<Element> visitor)
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

    /// <summary>
    /// Updates the given child with the given new configuration. Dart's <c>Element.updateChild</c>.
    /// </summary>
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

        Element newChild;
        if (child != null)
        {
            // A hot reload can turn a StatefulWidget into a StatelessWidget (or back). The element
            // then has the wrong concrete type for its widget, so it is guided out of the tree
            // instead of being updated.
            bool hasSameSuperclass = true;
            if (Constants.KDebugMode)
            {
                hasSameSuperclass = DebugConcreteSubtype(child) == Widget.DebugConcreteSubtype(newWidget);
            }

            if (hasSameSuperclass && ReferenceEquals(child.Widget, newWidget))
            {
                if (!Equals(child.Slot, newSlot))
                {
                    UpdateSlotForChild(child, newSlot);
                }

                newChild = child;
            }
            else if (hasSameSuperclass && Widget.CanUpdate(child.Widget, newWidget))
            {
                if (!Equals(child.Slot, newSlot))
                {
                    UpdateSlotForChild(child, newSlot);
                }

                child.Update(newWidget);
                DebugAssertions.Assert(ReferenceEquals(child.Widget, newWidget));
                if (Constants.KDebugMode)
                {
                    child.Owner!.DebugElementWasRebuilt(child);
                }

                newChild = child;
            }
            else
            {
                DeactivateChild(child);
                DebugAssertions.Assert(child.Parent is null);
                newChild = InflateWidget(newWidget, newSlot);
            }
        }
        else
        {
            newChild = InflateWidget(newWidget, newSlot);
        }

        if (Constants.KDebugMode)
        {
            if (child != null)
            {
                DebugRemoveGlobalKeyReservation(child);
            }

            if (newWidget.Key is GlobalKey key)
            {
                DebugAssertions.Assert(Owner != null);
                Owner!.DebugReserveGlobalKeyFor(this, newChild, key);
            }
        }

        return newChild;
    }

    /// <summary>
    /// Updates the children of this element to use new widgets, reusing elements whose widgets can
    /// be updated. Dart's <c>Element.updateChildren</c>, the six-phase keyed diff.
    /// </summary>
    public virtual List<Element> UpdateChildren(
        List<Element> oldChildren,
        IReadOnlyList<Widget> newWidgets,
        HashSet<Element>? forgottenChildren = null,
        IReadOnlyList<object?>? slots = null)
    {
        DebugAssertions.Assert(slots == null || newWidgets.Count == slots.Count);

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
        Array.Fill<Element>(newChildren, NullElement.Instance);

        Element? previousChild = null;

        // Update the top of the list.
        while (oldChildrenTop <= oldChildrenBottom && newChildrenTop <= newChildrenBottom)
        {
            Element? oldChild = ReplaceWithNullIfForgotten(oldChildren[oldChildrenTop]);
            Widget newWidget = newWidgets[newChildrenTop];
            DebugAssertions.Assert(oldChild == null || oldChild._lifecycleState == ElementLifecycleState.Active);
            if (oldChild == null || !Widget.CanUpdate(oldChild.Widget, newWidget))
            {
                break;
            }

            Element newChild = UpdateChild(oldChild, newWidget, SlotFor(newChildrenTop, previousChild))!;
            DebugAssertions.Assert(newChild._lifecycleState == ElementLifecycleState.Active);
            newChildren[newChildrenTop] = newChild;
            previousChild = newChild;
            newChildrenTop += 1;
            oldChildrenTop += 1;
        }

        // Scan the bottom of the list.
        while (oldChildrenTop <= oldChildrenBottom && newChildrenTop <= newChildrenBottom)
        {
            Element? oldChild = ReplaceWithNullIfForgotten(oldChildren[oldChildrenBottom]);
            Widget newWidget = newWidgets[newChildrenBottom];
            DebugAssertions.Assert(oldChild == null || oldChild._lifecycleState == ElementLifecycleState.Active);
            if (oldChild == null || !Widget.CanUpdate(oldChild.Widget, newWidget))
            {
                break;
            }

            oldChildrenBottom -= 1;
            newChildrenBottom -= 1;
        }

        // Scan the old children in the middle of the list.
        bool haveOldChildren = oldChildrenTop <= oldChildrenBottom;
        Dictionary<Key, Element>? oldKeyedChildren = null;
        if (haveOldChildren)
        {
            oldKeyedChildren = [];
            while (oldChildrenTop <= oldChildrenBottom)
            {
                Element? oldChild = ReplaceWithNullIfForgotten(oldChildren[oldChildrenTop]);
                DebugAssertions.Assert(
                    oldChild == null || oldChild._lifecycleState == ElementLifecycleState.Active);
                if (oldChild != null)
                {
                    if (oldChild.Widget.Key != null)
                    {
                        oldKeyedChildren[oldChild.Widget.Key] = oldChild;
                    }
                    else
                    {
                        DeactivateChild(oldChild);
                    }
                }

                oldChildrenTop += 1;
            }
        }

        // Update the middle of the list.
        while (newChildrenTop <= newChildrenBottom)
        {
            Element? oldChild = null;
            Widget newWidget = newWidgets[newChildrenTop];
            if (haveOldChildren)
            {
                Key? key = newWidget.Key;
                if (key != null && oldKeyedChildren!.TryGetValue(key, out Element? keyedOldChild))
                {
                    if (Widget.CanUpdate(keyedOldChild.Widget, newWidget))
                    {
                        oldChild = keyedOldChild;
                        oldKeyedChildren.Remove(key);
                    }
                }
            }

            DebugAssertions.Assert(oldChild == null || Widget.CanUpdate(oldChild.Widget, newWidget));
            Element newChild = UpdateChild(oldChild, newWidget, SlotFor(newChildrenTop, previousChild))!;
            DebugAssertions.Assert(newChild._lifecycleState == ElementLifecycleState.Active);
            DebugAssertions.Assert(
                ReferenceEquals(oldChild, newChild)
                || oldChild == null
                || oldChild._lifecycleState != ElementLifecycleState.Active);
            newChildren[newChildrenTop] = newChild;
            previousChild = newChild;
            newChildrenTop += 1;
        }

        // We've scanned the whole list.
        DebugAssertions.Assert(oldChildrenTop == oldChildrenBottom + 1);
        DebugAssertions.Assert(newChildrenTop == newChildrenBottom + 1);
        DebugAssertions.Assert(newWidgets.Count - newChildrenTop == oldChildren.Count - oldChildrenTop);
        newChildrenBottom = newWidgets.Count - 1;
        oldChildrenBottom = oldChildren.Count - 1;

        // Update the bottom of the list.
        while (oldChildrenTop <= oldChildrenBottom && newChildrenTop <= newChildrenBottom)
        {
            Element oldChild = oldChildren[oldChildrenTop];
            DebugAssertions.Assert(ReplaceWithNullIfForgotten(oldChild) != null);
            DebugAssertions.Assert(oldChild._lifecycleState == ElementLifecycleState.Active);
            Widget newWidget = newWidgets[newChildrenTop];
            DebugAssertions.Assert(Widget.CanUpdate(oldChild.Widget, newWidget));
            Element newChild = UpdateChild(oldChild, newWidget, SlotFor(newChildrenTop, previousChild))!;
            DebugAssertions.Assert(newChild._lifecycleState == ElementLifecycleState.Active);
            DebugAssertions.Assert(
                ReferenceEquals(oldChild, newChild) || oldChild._lifecycleState != ElementLifecycleState.Active);
            newChildren[newChildrenTop] = newChild;
            previousChild = newChild;
            newChildrenTop += 1;
            oldChildrenTop += 1;
        }

        // Clean up any of the remaining middle nodes from the old list.
        if (haveOldChildren && oldKeyedChildren!.Count > 0)
        {
            foreach (Element oldChild in oldKeyedChildren.Values)
            {
                if (forgottenChildren == null || !forgottenChildren.Contains(oldChild))
                {
                    DeactivateChild(oldChild);
                }
            }
        }

        DebugAssertions.Assert(Array.TrueForAll(newChildren, element => element is not NullElement));
        return [.. newChildren];
    }

    /// <summary>
    /// Adds this element to the tree in the given slot of the given parent. Dart's
    /// <c>Element.mount</c>; subclasses extend it through <see cref="OnMount"/>.
    /// </summary>
    public void Mount(Element? parent, object? newSlot)
    {
        if (Constants.KDebugMode)
        {
            if (_lifecycleState != ElementLifecycleState.Initial)
            {
                throw new AssertionError(
                    $"This element is no longer in its initial state ({DebugLifecycleName(_lifecycleState)})");
            }

            if (Parent != null)
            {
                throw new AssertionError(
                    $"This element already has a parent ({Parent}) and it shouldn't have one yet.");
            }

            if (parent != null && parent._lifecycleState != ElementLifecycleState.Active)
            {
                throw new AssertionError(
                    $"Parent ({parent}) should be null or in the active state "
                    + $"({DebugLifecycleName(parent._lifecycleState)})");
            }

            if (Slot != null)
            {
                throw new AssertionError($"This element already has a slot ({Slot}) and it shouldn't");
            }
        }

        Parent = parent;
        Slot = newSlot;
        _lifecycleState = ElementLifecycleState.Active;
        _depth = 1 + (parent?.Depth ?? 0);
        if (parent != null)
        {
            // Only assign ownership if the parent is non-null. A root element has been given its
            // owner by Attach/RootElement.AssignOwner already.
            Owner = parent.Owner;
            _parentBuildScope = parent.BuildScope;
        }
        else
        {
            Owner?.RegisterRootElement(this);
        }

        DebugAssertions.Assert(Owner != null);
        if (Widget.Key is GlobalKey key)
        {
            Owner!.RegisterGlobalKey(key, this);
        }

        UpdateInheritance();
        AttachNotificationTree();
        OnMount();
    }

    /// <summary>
    /// The subclass half of Dart's <c>mount</c>: runs after the element is active, owned, registered
    /// and inheriting. Overrides call <c>base.OnMount()</c> first, as Dart's call <c>super.mount</c>.
    /// </summary>
    protected virtual void OnMount()
    {
    }

    private void DebugRemoveGlobalKeyReservation(Element child)
    {
        DebugAssertions.Assert(Owner != null);
        Owner!.DebugRemoveGlobalKeyReservationFor(this, child);
    }

    /// <summary>
    /// Changes the widget used to configure this element. Dart's <c>Element.update</c>.
    /// </summary>
    public virtual void Update(Widget newWidget)
    {
        DebugAssertions.Assert(
            _lifecycleState == ElementLifecycleState.Active
            && !ReferenceEquals(newWidget, Widget)
            && Widget.CanUpdate(Widget, newWidget));

        // This element was told to update, so the global key reservations of its forgotten children
        // can go now; before this they still represented duplications.
        if (Constants.KDebugMode && _debugForgottenChildrenWithGlobalKey is { Count: > 0 } forgotten)
        {
            foreach (Element child in forgotten)
            {
                DebugRemoveGlobalKeyReservation(child);
            }

            forgotten.Clear();
        }

        _widget = newWidget;
    }

    /// <summary>
    /// Changes the slot that the given child occupies in its parent, together with every descendant
    /// that attaches its render object through that child. Dart's <c>Element.updateSlotForChild</c>.
    /// </summary>
    public virtual void UpdateSlotForChild(Element child, object? newSlot)
    {
        DebugAssertions.Assert(_lifecycleState == ElementLifecycleState.Active);
        DebugAssertions.Assert(ReferenceEquals(child.Parent, this));

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

    /// <summary>Called by <see cref="UpdateSlotForChild"/> when the parent changes this element's slot.</summary>
    public virtual void UpdateSlot(object? newSlot)
    {
        DebugAssertions.Assert(_lifecycleState == ElementLifecycleState.Active);
        DebugAssertions.Assert(Parent != null);
        DebugAssertions.Assert(Parent?._lifecycleState == ElementLifecycleState.Active);
        Slot = newSlot;
    }

    /// <summary>Dart's <c>Element._updateDepth</c>: depths only grow when a subtree is reparented.</summary>
    private void UpdateDepth(int parentDepth)
    {
        int expectedDepth = parentDepth + 1;
        if (_depth < expectedDepth)
        {
            _depth = expectedDepth;
            VisitChildren(child => child.UpdateDepth(expectedDepth));
        }
    }

    /// <summary>Remove <see cref="RenderObject"/> from the render tree. Dart's <c>detachRenderObject</c>.</summary>
    public virtual void DetachRenderObject()
    {
        VisitChildren(static child => child.DetachRenderObject());
        Slot = null;
    }

    /// <summary>Add <see cref="RenderObject"/> to the render tree. Dart's <c>attachRenderObject</c>.</summary>
    public virtual void AttachRenderObject(object? newSlot)
    {
        DebugAssertions.Assert(Slot == null);
        VisitChildren(child => child.AttachRenderObject(newSlot));
        Slot = newSlot;
    }

    /// <summary>
    /// Dart's <c>Element._retakeInactiveElement</c>: takes the element registered under
    /// <paramref name="key"/> away from wherever it is — its current parent, or the inactive list —
    /// so it can be reparented under this element.
    /// </summary>
    private Element? RetakeInactiveElement(GlobalKey key, Widget newWidget)
    {
        // The "inactivity" of the element being retaken here may be forward-looking: if it is taken
        // from an element that currently has it as a child, that element will soon no longer have it.
        Element? element = Owner!.GlobalKeyElement(key);
        if (element == null)
        {
            return null;
        }

        if (!Widget.CanUpdate(element.Widget, newWidget))
        {
            return null;
        }

        if (Constants.KDebugMode && WidgetsDebug.DebugPrintGlobalKeyedWidgetLifecycle)
        {
            object from = (object?)element.Parent ?? "inactive elements list";
            Print.DebugPrint($"Attempting to take {element} from {from} to put in {this}.");
        }

        Element? parent = element.Parent;
        if (parent != null)
        {
            if (Constants.KDebugMode)
            {
                if (ReferenceEquals(parent, this))
                {
                    throw new FlutterError(
                    [
                        new ErrorSummary("A GlobalKey was used multiple times inside one widget's child list."),
                        new DiagnosticsProperty<GlobalKey>("The offending GlobalKey was", key),
                        parent.DescribeElement("The parent of the widgets with that key was"),
                        element.DescribeElement("The first child to get instantiated with that key became"),
                        new DiagnosticsProperty<Widget>(
                            "The second child that was to be instantiated with that key was",
                            Widget,
                            style: DiagnosticsTreeStyle.ErrorProperty),
                        new ErrorDescription(
                            "A GlobalKey can only be specified on one widget at a time in the widget tree."),
                    ]);
                }

                parent.Owner!.DebugTrackElementThatWillNeedToBeRebuilt(parent, key);
            }

            parent.ForgetChild(element);
            parent.DeactivateChild(element);
        }

        DebugAssertions.Assert(element.Parent == null);
        Owner!.InactiveElements.Remove(element);
        return element;
    }

    /// <summary>
    /// Create an element for the given widget and add it as a child of this element in the given
    /// slot. Dart's <c>Element.inflateWidget</c>.
    /// </summary>
    public virtual Element InflateWidget(Widget newWidget, object? newSlot)
    {
        Element? inactiveChild = newWidget.Key is GlobalKey key ? RetakeInactiveElement(key, newWidget) : null;
        Element newChild = inactiveChild ?? newWidget.CreateElement();
        if (Constants.KDebugMode)
        {
            DebugCheckForCycles(newChild);
        }

        try
        {
            if (inactiveChild != null)
            {
                DebugAssertions.Assert(inactiveChild.Parent == null);
                inactiveChild.ActivateWithParent(this, newSlot);
                Element? updatedChild = UpdateChild(inactiveChild, newWidget, newSlot);
                DebugAssertions.Assert(ReferenceEquals(inactiveChild, updatedChild));
                return updatedChild!;
            }

            newChild.Mount(this, newSlot);
            DebugAssertions.Assert(newChild._lifecycleState == ElementLifecycleState.Active);
            return newChild;
        }
        catch (Exception)
        {
            // Attempt to do some clean-up if activation or mount fails, to leave the tree in a
            // reasonable state.
            DeactivateFailedChildSilently(newChild);
            throw;
        }
    }

    /// <summary>Dart's <c>Element._debugCheckForCycles</c>.</summary>
    private void DebugCheckForCycles(Element newChild)
    {
        DebugAssertions.Assert(newChild.Parent == null);
        Element node = this;
        while (node.Parent != null)
        {
            node = node.Parent;
        }

        // A match means we are about to create a cycle.
        DebugAssertions.Assert(!ReferenceEquals(node, newChild));
    }

    /// <summary>
    /// Move the given element to the list of inactive elements and detach its render object from
    /// the render tree. Dart's <c>Element.deactivateChild</c>.
    /// </summary>
    public virtual void DeactivateChild(Element child)
    {
        DebugAssertions.Assert(ReferenceEquals(child.Parent, this));
        child.Parent = null;
        child.DetachRenderObject();

        if (Owner == null)
        {
            // Dart asserts an owner here. Plumix keeps a fallback for the ownerless elements tests
            // and hosts build directly: there is no inactive list to park the child in, so the
            // subtree is torn down inline in the order _InactiveElements would use.
            Widgets.InactiveElements.DeactivateAndUnmount(child);
            return;
        }

        // This eventually calls child.Deactivate().
        Owner.InactiveElements.Add(child);
        if (Constants.KDebugMode
            && WidgetsDebug.DebugPrintGlobalKeyedWidgetLifecycle
            && child.Widget.Key is GlobalKey)
        {
            Print.DebugPrint($"Deactivated {child} (keyed child of {this})");
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
            // Do not rethrow: the subtree has already thrown a different error and the framework is
            // cleaning up on a best-effort basis.
        }
    }

    /// <summary>
    /// Dart's <c>Element._deactivateFailedSubtreeRecursively</c>: force a subtree that threw during
    /// activation or rebuild into <see cref="ElementLifecycleState.Failed"/>, best effort, never
    /// surfacing an additional error.
    /// </summary>
    internal static void DeactivateFailedSubtreeRecursively(Element element)
    {
        try
        {
            element.Deactivate();
        }
        catch (Exception)
        {
            element.EnsureDeactivated();
        }

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
    /// Remove the given child from the element's child list, in preparation for the child being
    /// reused elsewhere in the element tree. Dart's <c>Element.forgetChild</c>.
    /// </summary>
    /// <remarks>
    /// The reservation of a forgotten global-keyed child cannot be released here — the child is
    /// only really gone once this element is updated — so it is parked until <see cref="Update"/>.
    /// </remarks>
    public virtual void ForgetChild(Element child)
    {
        if (Constants.KDebugMode && child.Widget.Key is GlobalKey)
        {
            _debugForgottenChildrenWithGlobalKey?.Add(child);
        }
    }

    /// <summary>Dart's <c>Element._activateWithParent</c>.</summary>
    internal void ActivateWithParent(Element parent, object? newSlot)
    {
        DebugAssertions.Assert(_lifecycleState == ElementLifecycleState.Inactive);
        Parent = parent;
        Owner = parent.Owner;
        if (Constants.KDebugMode && WidgetsDebug.DebugPrintGlobalKeyedWidgetLifecycle)
        {
            Print.DebugPrint($"Reactivating {this} (now child of {Parent}).");
        }

        // The depth and the build scope are refreshed before activation: Activate ends by scheduling a
        // dirty element, and it has to land in the scope it is moving into.
        UpdateDepth(parent.Depth);
        UpdateBuildScopeRecursively();
        ActivateRecursively(this);
        AttachRenderObject(newSlot);
        DebugAssertions.Assert(_lifecycleState == ElementLifecycleState.Active);
    }

    private static void ActivateRecursively(Element element)
    {
        DebugAssertions.Assert(element._lifecycleState == ElementLifecycleState.Inactive);
        element.Activate();
        DebugAssertions.Assert(element._lifecycleState == ElementLifecycleState.Active);
        element.VisitChildren(ActivateRecursively);
    }

    /// <summary>
    /// Transition from the "inactive" to the "active" lifecycle state. Dart's
    /// <c>Element.activate</c>; subclasses extend it through <see cref="OnActivate"/>.
    /// </summary>
    private void Activate()
    {
        DebugAssertions.Assert(_lifecycleState == ElementLifecycleState.Inactive);
        DebugAssertions.Assert(Owner != null);
        bool hadDependencies = (_dependencies?.Count > 0) || _hadUnsatisfiedDependencies;
        _lifecycleState = ElementLifecycleState.Active;

        // The dependencies were unregistered in Deactivate, but the list was never cleared.
        _dependencies?.Clear();
        _hadUnsatisfiedDependencies = false;
        UpdateInheritance();
        AttachNotificationTree();
        if (Dirty)
        {
            Owner!.ScheduleBuildFor(this);
        }

        if (hadDependencies)
        {
            DidChangeDependencies();
        }

        OnActivate();
    }

    /// <summary>The subclass half of Dart's <c>activate</c>; overrides call <c>base.OnActivate()</c> first.</summary>
    protected virtual void OnActivate()
    {
    }

    /// <summary>
    /// Transition from the "active" to the "inactive" lifecycle state. Dart's
    /// <c>Element.deactivate</c>; the subtree walk lives in
    /// <see cref="Widgets.InactiveElements.DeactivateRecursively"/>, exactly as it does in Dart's
    /// <c>_InactiveElements</c>.
    /// </summary>
    internal void Deactivate()
    {
        DebugAssertions.Assert(_lifecycleState == ElementLifecycleState.Active);
        DebugAssertions.Assert(_widget != null);
        OnDeactivate();
        EnsureDeactivated();
    }

    /// <summary>The subclass half of Dart's <c>deactivate</c>, run before the dependencies are dropped.</summary>
    protected virtual void OnDeactivate()
    {
    }

    /// <summary>
    /// Dart's <c>Element._ensureDeactivated</c>: drops the inherited dependencies and marks the
    /// element inactive, even when <c>deactivate()</c> itself threw. The dependency set is
    /// deliberately kept so <see cref="Activate"/> can tell it had dependencies.
    /// </summary>
    private void EnsureDeactivated()
    {
        if (_dependencies is { Count: > 0 } dependencies)
        {
            foreach (InheritedElement dependency in dependencies)
            {
                dependency.RemoveDependent(this);
            }
        }

        InheritedElements = null;
        _lifecycleState = ElementLifecycleState.Inactive;
    }

    /// <summary>
    /// Dart's <c>Element.debugDeactivated</c>: called in debug builds after this element's children
    /// have been deactivated.
    /// </summary>
    public virtual void DebugDeactivated()
    {
        DebugAssertions.Assert(_lifecycleState == ElementLifecycleState.Inactive);
    }

    /// <summary>
    /// Plumix-only: tears down a parentless root element and everything below it, deepest-first,
    /// exactly the way <see cref="Widgets.InactiveElements"/> tears down a subtree the tree dropped.
    /// </summary>
    /// <remarks>
    /// Dart has no counterpart because its root element lives for the process; a Plumix host clears
    /// its root widget (and a test its harness), and nothing above the root can call
    /// <see cref="DeactivateChild"/> for it. <see cref="Unmount"/> itself is not that entry point:
    /// like Dart's, it only takes this one element from inactive to defunct.
    /// </remarks>
    public void UnmountRoot()
    {
        if (Parent is not null)
        {
            throw new AssertionError(
                $"{ToStringShort()} has a parent; only a root element can be unmounted directly.");
        }

        DetachRenderObject();
        Widgets.InactiveElements.DeactivateAndUnmount(this);
    }

    /// <summary>
    /// Dart's <c>Element.unmount</c>: the transition from inactive to defunct. Descendants are
    /// unmounted first by <see cref="Widgets.InactiveElements"/>; an override must not walk children
    /// itself.
    /// </summary>
    public virtual void Unmount()
    {
        DebugAssertions.Assert(_lifecycleState == ElementLifecycleState.Inactive);
        DebugAssertions.Assert(_widget != null);
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchDisposed(this);
        }

        OnUnmount();
        if (_widget?.Key is GlobalKey key)
        {
            Owner?.UnregisterGlobalKey(key, this);
        }

        // Release resources to reduce the severity of memory leaks caused by defunct, but
        // accidentally retained, elements.
        _widget = null;
        _dependencies = null;
        _lifecycleState = ElementLifecycleState.Defunct;
    }

    /// <summary>Plumix-only hook run at the start of <see cref="Unmount"/>, while the widget is still set.</summary>
    protected virtual void OnUnmount()
    {
    }

    /// <summary>
    /// Dart's <c>Element.debugExpectsRenderObjectForSlot</c>: whether the element occupying
    /// <paramref name="slot"/> is expected to attach its render object to an ancestor. Elements that
    /// host an independent render tree in a slot return false for that slot.
    /// </summary>
    public virtual bool DebugExpectsRenderObjectForSlot(object? slot) => true;

    /// <summary>
    /// The render object of this element or of its nearest descendant, if the element is active.
    /// Dart's <c>Element.findRenderObject</c>.
    /// </summary>
    public virtual RenderObject? FindRenderObject()
    {
        if (Constants.KDebugMode && _lifecycleState != ElementLifecycleState.Active)
        {
            throw new FlutterError(
            [
                new ErrorSummary("Cannot get renderObject of inactive element."),
                new ErrorDescription(
                    "In order for an element to have a valid renderObject, it must be active, which "
                    + "means it is part of the tree.\n"
                    + $"Instead, this element is in the _ElementLifecycle.{DebugLifecycleName(_lifecycleState)} "
                    + "state.\n"
                    + "If you called this method from a State object, consider guarding it with State.mounted."),
                DescribeElement("The findRenderObject() method was called for the following element"),
            ]);
        }

        return RenderObject;
    }

    /// <summary>
    /// The size of the render object returned by <see cref="FindRenderObject"/> when it is a
    /// <see cref="RenderBox"/>. Dart parity: <c>BuildContext.size</c>.
    /// </summary>
    public Size? Size
    {
        get
        {
            if (Constants.KDebugMode)
            {
                DebugCheckCanGetSize();
            }

            RenderObject? renderObject = FindRenderObject();
            if (Constants.KDebugMode)
            {
                DebugCheckRenderObjectHasSize(renderObject);
            }

            return renderObject is RenderBox box ? box.Size : null;
        }
    }

    private void DebugCheckCanGetSize()
    {
        if (_lifecycleState != ElementLifecycleState.Active)
        {
            throw new FlutterError(
            [
                new ErrorSummary("Cannot get size of inactive element."),
                new ErrorDescription(
                    "In order for an element to have a valid size, the element must be active, which means "
                    + "it is part of the tree.\n"
                    + $"Instead, this element is in the _ElementLifecycle.{DebugLifecycleName(_lifecycleState)} "
                    + "state."),
                DescribeElement("The size getter was called for the following element"),
            ]);
        }

        if (Owner!.DebugBuilding)
        {
            throw new FlutterError(
            [
                new ErrorSummary("Cannot get size during build."),
                new ErrorDescription(
                    "The size of this render object has not yet been determined because the framework is "
                    + "still in the process of building widgets, which means the render tree for this frame "
                    + "has not yet been determined. The size getter should only be called from paint "
                    + "callbacks or interaction event handlers (e.g. gesture callbacks)."),
                new ErrorSpacer(),
                new ErrorHint(
                    "If you need some sizing information during build to decide which widgets to build, "
                    + "consider using a LayoutBuilder widget, which can tell you the layout constraints at "
                    + "a given location in the tree. See "
                    + "<https://api.flutter.dev/flutter/widgets/LayoutBuilder-class.html> for more details."),
                new ErrorSpacer(),
                DescribeElement("The size getter was called for the following element"),
            ]);
        }
    }

    private void DebugCheckRenderObjectHasSize(RenderObject? renderObject)
    {
        if (renderObject == null)
        {
            throw new FlutterError(
            [
                new ErrorSummary("Cannot get size without a render object."),
                new ErrorHint(
                    "In order for an element to have a valid size, the element must have an associated "
                    + "render object. This element does not have an associated render object, which "
                    + "typically means that the size getter was called too early in the pipeline (e.g., "
                    + "during the build phase) before the framework has created the render tree."),
                DescribeElement("The size getter was called for the following element"),
            ]);
        }

        if (renderObject is RenderSliver)
        {
            throw new FlutterError(
            [
                new ErrorSummary("Cannot get size from a RenderSliver."),
                new ErrorHint(
                    "The render object associated with this element is a "
                    + $"{Diagnostics.DescribeType(renderObject.GetType())}, which is a subtype of RenderSliver. "
                    + "Slivers do not have a size per se. They have a more elaborate geometry description, "
                    + "which can be accessed by calling findRenderObject and then using the \"geometry\" "
                    + "getter on the resulting object."),
                DescribeElement("The size getter was called for the following element"),
                renderObject.DescribeForError("The associated render sliver was"),
            ]);
        }

        if (renderObject is not RenderBox box)
        {
            throw new FlutterError(
            [
                new ErrorSummary("Cannot get size from a render object that is not a RenderBox."),
                new ErrorHint(
                    "Instead of being a subtype of RenderBox, the render object associated with this "
                    + $"element is a {Diagnostics.DescribeType(renderObject.GetType())}. If this type of "
                    + "render object does have a size, consider calling findRenderObject and extracting its "
                    + "size manually."),
                DescribeElement("The size getter was called for the following element"),
                renderObject.DescribeForError("The associated render object was"),
            ]);
        }

        if (!box.HasSize)
        {
            throw new FlutterError(
            [
                new ErrorSummary("Cannot get size from a render object that has not been through layout."),
                new ErrorHint(
                    "The size of this render object has not yet been determined because this render object "
                    + "has not yet been through layout, which typically means that the size getter was "
                    + "called too early in the pipeline (e.g., during the build phase) before the framework "
                    + "has determined the size and position of the render objects during layout."),
                DescribeElement("The size getter was called for the following element"),
                box.DescribeForError("The render object from which the size was to be obtained was"),
            ]);
        }

        if (box.DebugNeedsLayout)
        {
            throw new FlutterError(
            [
                new ErrorSummary(
                    "Cannot get size from a render object that has been marked dirty for layout."),
                new ErrorHint(
                    "The size of this render object is ambiguous because this render object has been "
                    + "modified since it was last laid out, which typically means that the size getter was "
                    + "called too early in the pipeline (e.g., during the build phase) before the framework "
                    + "has determined the size and position of the render objects during layout."),
                DescribeElement("The size getter was called for the following element"),
                box.DescribeForError("The render object from which the size was to be obtained was"),
                new ErrorHint(
                    "Consider using debugPrintMarkNeedsLayoutStacks to determine why the render object in "
                    + "question is dirty, if you did not expect this."),
            ]);
        }
    }

    /// <summary>
    /// Asserts that this element is still active, so that an ancestor lookup made from it reads a
    /// stable tree. Flutter's <c>Element._debugCheckStateIsActiveForAncestorLookup</c>.
    /// </summary>
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
                    "To safely refer to a widget's ancestor in its dispose() method, save a reference to "
                    + "the ancestor by calling dependOnInheritedWidgetOfExactType() in the widget's "
                    + "didChangeDependencies() method."),
            ]);
        }
    }

    /// <summary>
    /// Dart's <c>Element.doesDependOnInheritedElement</c>: whether
    /// <see cref="DependOnInheritedElement"/> was previously called with <paramref name="ancestor"/>.
    /// </summary>
    public bool DoesDependOnInheritedElement(InheritedElement ancestor)
        => _dependencies?.Contains(ancestor) ?? false;

    public virtual InheritedWidget DependOnInheritedElement(InheritedElement ancestor, object? aspect = null)
    {
        _dependencies ??= [];
        _dependencies.Add(ancestor);
        ancestor.UpdateDependencies(this, aspect);
        return (InheritedWidget)ancestor.Widget;
    }

    public virtual T? DependOnInheritedWidgetOfExactType<T>(object? aspect = null) where T : InheritedWidget
    {
        DebugCheckStateIsActiveForAncestorLookup();
        InheritedElement? ancestor = LookupInheritedElement(typeof(T));
        if (ancestor != null)
        {
            return (T)DependOnInheritedElement(ancestor, aspect);
        }

        _hadUnsatisfiedDependencies = true;
        return null;
    }

    /// <summary>
    /// Finds the nearest ancestor whose widget's runtime type is exactly <typeparamref name="T"/>
    /// without registering a dependency. Use this to read a value once without subscribing to
    /// future changes.
    /// </summary>
    public virtual T? GetInheritedWidgetOfExactType<T>() where T : InheritedWidget
    {
        return GetElementForInheritedWidgetOfExactType<T>()?.Widget as T;
    }

    /// <summary>
    /// Returns the nearest inherited element whose widget's runtime type is exactly
    /// <typeparamref name="T"/>, without creating a dependency.
    /// </summary>
    public virtual InheritedElement? GetElementForInheritedWidgetOfExactType<T>() where T : InheritedWidget
    {
        DebugCheckStateIsActiveForAncestorLookup();
        return LookupInheritedElement(typeof(T));
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
    /// Called in <see cref="Mount"/> and in activation to attach this element to the notification tree.
    /// Dart's <c>Element.attachNotificationTree</c>, with <c>NotifiableElementMixin</c>'s override
    /// folded in: a <see cref="NotifiableElementMixin"/> starts a node of its own.
    /// </summary>
    public virtual void AttachNotificationTree()
    {
        _notificationTree = this is NotifiableElementMixin notifiable
            ? new NotificationNode(Parent?._notificationTree, notifiable)
            : Parent?._notificationTree;
    }

    /// <summary>
    /// Recomputes <see cref="InheritedElements"/> from the parent's. The base implementation shares
    /// the parent's map verbatim; <see cref="InheritedElement"/> overrides it to add itself.
    /// </summary>
    /// <remarks>Flutter's <c>Element._updateInheritance</c>.</remarks>
    private protected virtual void UpdateInheritance()
    {
        DebugAssertions.Assert(_lifecycleState == ElementLifecycleState.Active);
        InheritedElements = Parent?.InheritedElements;
    }

    /// <summary>
    /// Returns the nearest ancestor widget whose runtime type is exactly <typeparamref name="T"/>,
    /// without creating a dependency. A subclass of <typeparamref name="T"/> does not match.
    /// </summary>
    public virtual T? FindAncestorWidgetOfExactType<T>() where T : Widget
    {
        DebugCheckStateIsActiveForAncestorLookup();
        Element? ancestor = Parent;
        while (ancestor != null && ancestor.Widget.GetType() != typeof(T))
        {
            ancestor = ancestor.Parent;
        }

        return ancestor?.Widget as T;
    }

    /// <summary>Returns the nearest ancestor state of type <typeparamref name="T"/>.</summary>
    public virtual T? FindAncestorStateOfType<T>() where T : State
    {
        DebugCheckStateIsActiveForAncestorLookup();
        Element? ancestor = Parent;
        while (ancestor != null)
        {
            if (ancestor is StatefulElement { State: T })
            {
                break;
            }

            ancestor = ancestor.Parent;
        }

        return (ancestor as StatefulElement)?.State as T;
    }

    /// <summary>Returns the furthest ancestor state assignable to <typeparamref name="T"/>.</summary>
    public virtual T? FindRootAncestorStateOfType<T>() where T : State
    {
        DebugCheckStateIsActiveForAncestorLookup();
        Element? ancestor = Parent;
        StatefulElement? statefulAncestor = null;
        while (ancestor != null)
        {
            if (ancestor is StatefulElement { State: T } stateful)
            {
                statefulAncestor = stateful;
            }

            ancestor = ancestor.Parent;
        }

        return statefulAncestor?.State as T;
    }

    /// <summary>Returns the nearest ancestor render object assignable to <typeparamref name="T"/>.</summary>
    public virtual T? FindAncestorRenderObjectOfType<T>() where T : RenderObject
    {
        DebugCheckStateIsActiveForAncestorLookup();
        Element? ancestor = Parent;
        while (ancestor != null)
        {
            if (ancestor is RenderObjectElement { RenderObject: T renderObject })
            {
                return renderObject;
            }

            ancestor = ancestor.Parent;
        }

        return null;
    }

    /// <summary>Walks ancestor elements until <paramref name="visitor"/> returns false.</summary>
    public virtual void VisitAncestorElements(Func<Element, bool> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        DebugCheckStateIsActiveForAncestorLookup();
        Element? ancestor = Parent;
        while (ancestor != null && visitor(ancestor))
        {
            ancestor = ancestor.Parent;
        }
    }

    /// <summary>
    /// Called when a dependency of this element changes. Dart's <c>Element.didChangeDependencies</c>.
    /// </summary>
    public virtual void DidChangeDependencies()
    {
        // Otherwise MarkNeedsBuild is a no-op.
        DebugAssertions.Assert(_lifecycleState == ElementLifecycleState.Active);
        DebugCheckOwnerBuildTargetExists("didChangeDependencies");
        MarkNeedsBuild();
    }

    /// <summary>Dart's <c>Element._debugCheckOwnerBuildTargetExists</c>.</summary>
    private protected void DebugCheckOwnerBuildTargetExists(string methodName)
    {
        if (!Constants.KDebugMode || Owner!.DebugCurrentBuildTarget != null)
        {
            return;
        }

        throw new FlutterError(
        [
            new ErrorSummary(
                $"{methodName} for {Diagnostics.DescribeType(Widget.GetType())} was called at an inappropriate "
                + "time."),
            new ErrorDescription("It may only be called while the widgets are being built."),
            new ErrorHint(
                $"A possible cause of this error is when {methodName} is called during one of:\n"
                + " * network I/O event\n"
                + " * file I/O event\n"
                + " * timer\n"
                + " * microtask (caused by Future.then, async/await, scheduleMicrotask)"),
        ]);
    }

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
            chain.Add("⋯");
        }

        return string.Join(" ← ", chain);
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

    /// <summary>Starts bubbling <paramref name="notification"/> at this element.</summary>
    /// <remarks>Flutter's <c>Element.dispatchNotification</c>.</remarks>
    public virtual void DispatchNotification(Notification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _notificationTree?.DispatchNotification(notification);
    }

    /// <inheritdoc />
    public override string ToStringShort() =>
        _widget?.ToStringShort() ?? $"{Diagnostics.DescribeIdentity(this)}(DEFUNCT)";

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

        Widget? widget = _widget;
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
    /// Whether this element needs rebuilding. Dart parity: <c>Element.dirty</c>, which starts out
    /// true because a freshly created element has never been built.
    /// </summary>
    public bool Dirty { get; private set; } = true;

    /// <summary>
    /// Whether this element sits in <see cref="Widgets.BuildScope"/>'s dirty list. Dart's
    /// <c>Element._inDirtyList</c>: membership is a flag on the element rather than a side set,
    /// because the list keeps tombstones for elements that were deactivated or moved to another
    /// scope mid-flush and must not enqueue them a second time.
    /// </summary>
    internal bool InDirtyList { get; set; }

    /// <summary>Dart's <c>Element._debugBuiltOnce</c>, read by <c>debugPrintRebuildDirtyWidgets</c>.</summary>
    private bool _debugBuiltOnce;

    /// <summary>
    /// Marks the element as dirty and adds it to the global list of widgets to rebuild in the next
    /// frame. Dart's <c>Element.markNeedsBuild</c>.
    /// </summary>
    public virtual void MarkNeedsBuild()
    {
        DebugAssertions.Assert(_lifecycleState != ElementLifecycleState.Defunct);
        if (_lifecycleState != ElementLifecycleState.Active)
        {
            return;
        }

        DebugAssertions.Assert(Owner != null);
        DebugCheckCanMarkNeedsBuild();
        if (Dirty)
        {
            return;
        }

        Dirty = true;
        Owner!.ScheduleBuildFor(this);
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

        if (owner.DebugBuilding)
        {
            DebugAssertions.Assert(owner.DebugCurrentBuildTarget != null);
            DebugAssertions.Assert(owner.DebugStateLocked);
            if (owner.DebugCurrentBuildTarget is { } target && DebugIsDescendantOf(target))
            {
                return;
            }

            List<DiagnosticsNode> information =
            [
                new ErrorSummary("setState() or markNeedsBuild() called during build."),
                new ErrorDescription(
                    $"This {Diagnostics.DescribeType(Widget.GetType())} widget cannot be marked as needing to "
                    + "build because the framework is already in the process of building widgets. A widget "
                    + "can be marked as needing to be built during the build phase only if one of its "
                    + "ancestors is currently building. This exception is allowed because the framework "
                    + "builds parent widgets before children, which means a dirty descendant will always be "
                    + "built. Otherwise, the framework might not visit this widget during this build phase."),
                DescribeElement("The widget on which setState() or markNeedsBuild() was called was"),
            ];
            if (owner.DebugCurrentBuildTarget is { } buildTarget)
            {
                information.Add(buildTarget.DescribeWidget(
                    "The widget which was currently being built when the offending call was made was"));
            }

            throw new FlutterError(information);
        }

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

    /// <summary>
    /// Rebuilds this element if it is dirty, or unconditionally when <paramref name="force"/> is set.
    /// The rebuild itself is done by <see cref="PerformRebuild"/>.
    /// </summary>
    /// <remarks>Flutter's <c>Element.rebuild({bool force = false})</c>.</remarks>
    public virtual void Rebuild(bool force = false)
    {
        DebugAssertions.Assert(_lifecycleState != ElementLifecycleState.Initial);
        if (_lifecycleState != ElementLifecycleState.Active || (!Dirty && !force))
        {
            return;
        }

        if (Constants.KDebugMode)
        {
            WidgetsDebug.DebugOnRebuildDirtyWidget?.Invoke(this, _debugBuiltOnce);
            if (WidgetsDebug.DebugPrintRebuildDirtyWidgets)
            {
                if (!_debugBuiltOnce)
                {
                    Print.DebugPrint($"Building {this}");
                    _debugBuiltOnce = true;
                }
                else
                {
                    Print.DebugPrint($"Rebuilding {this}");
                }
            }
        }

        DebugAssertions.Assert(_lifecycleState == ElementLifecycleState.Active);
        DebugAssertions.Assert(Owner!.DebugStateLocked);
        Element? debugPreviousBuildTarget = null;
        if (Constants.KDebugMode)
        {
            debugPreviousBuildTarget = Owner!.DebugCurrentBuildTarget;
            Owner.DebugCurrentBuildTarget = this;
        }

        try
        {
            PerformRebuild();
        }
        finally
        {
            if (Constants.KDebugMode)
            {
                Owner!.DebugElementWasRebuilt(this);
                DebugAssertions.Assert(ReferenceEquals(Owner.DebugCurrentBuildTarget, this));
                Owner.DebugCurrentBuildTarget = debugPreviousBuildTarget;
            }
        }

        DebugAssertions.Assert(!Dirty);
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
    private bool _debugDoingBuild;

    protected ComponentElement(Widget widget) : base(widget)
    {
    }

    /// <inheritdoc />
    public override bool DebugDoingBuild => _debugDoingBuild;

    public override Element? RenderObjectAttachingChild => _child;

    protected override void OnMount()
    {
        base.OnMount();
        DebugAssertions.Assert(_child == null);
        DebugAssertions.Assert(LifecycleState == ElementLifecycleState.Active);
        FirstBuild();
        DebugAssertions.Assert(_child != null);
    }

    /// <summary>Dart's <c>ComponentElement._firstBuild</c>.</summary>
    private protected virtual void FirstBuild()
    {
        // This eventually calls PerformRebuild.
        Rebuild();
    }

    /// <summary>
    /// Produces the child widget. Dart's <c>ComponentElement.build</c>: subclasses delegate to
    /// <c>StatelessWidget.build</c>, <c>State.build</c> or <c>ProxyWidget.child</c>.
    /// </summary>
    public abstract Widget Build();

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
            if (Constants.KDebugMode)
            {
                _debugDoingBuild = true;
            }

            built = Build();
            if (Constants.KDebugMode)
            {
                _debugDoingBuild = false;
            }

            WidgetsDebug.DebugWidgetBuilderValue(Widget, built);
        }
        catch (Exception exception)
        {
            _debugDoingBuild = false;
            built = ErrorWidget.Builder(ReportBuildException(exception));
        }
        finally
        {
            // Clears the "dirty" flag.
            base.PerformRebuild();
        }

        try
        {
            _child = UpdateChild(_child, built, Slot);
            DebugAssertions.Assert(_child != null);
        }
        catch (Exception exception)
        {
            built = ErrorWidget.Builder(ReportBuildException(exception));
            try
            {
                // Dart calls `_child?.deactivate()`, which leaves the old child's descendants active and
                // registered with their inherited ancestors. Plumix tears its roots down (UnmountRoot),
                // where those stale dependents trip `InheritedElement.DebugDeactivated`, so the whole old
                // subtree is deactivated instead (see DIVERGENCES.md).
                if (_child != null)
                {
                    DeactivateChild(_child);
                }
            }
            catch (Exception)
            {
                // The old subtree is already broken; reporting a second failure would bury the first.
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
        DebugAssertions.Assert(ReferenceEquals(child, _child));
        _child = null;
        base.ForgetChild(child);
    }
}

/// <summary>An <see cref="Element"/> that uses a <see cref="StatelessWidget"/> as its configuration.</summary>
public class StatelessElement : ComponentElement
{
    public StatelessElement(StatelessWidget widget) : base(widget)
    {
    }

    public override Widget Build() => ((StatelessWidget)Widget).Build(this);

    public override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        DebugAssertions.Assert(ReferenceEquals(Widget, newWidget));
        Rebuild(force: true);
    }
}

/// <summary>An <see cref="Element"/> that uses a <see cref="StatefulWidget"/> as its configuration.</summary>
public class StatefulElement : ComponentElement
{
    private State? _state;
    private bool _didChangeDependencies;

    public StatefulElement(StatefulWidget widget) : base(widget)
    {
        _state = widget.CreateState();
        if (Constants.KDebugMode && !_state.DebugTypesAreRight(widget))
        {
            string widgetType = Diagnostics.DescribeType(widget.GetType());
            throw new FlutterError(
            [
                new ErrorSummary($"StatefulWidget.createState must return a subtype of State<{widgetType}>"),
                new ErrorDescription(
                    $"The createState function for {widgetType} returned a state of type "
                    + $"{Diagnostics.DescribeType(_state.GetType())}, which is not a subtype of "
                    + $"State<{widgetType}>, violating the contract for createState."),
            ]);
        }

        _state.AttachElement(this, widget);
    }

    /// <summary>
    /// The <see cref="Widgets.State"/> instance associated with this location in the tree. Dart's
    /// <c>StatefulElement.state</c> is <c>_state!</c>: it throws once the element is unmounted.
    /// </summary>
    public State State =>
        _state ?? throw new InvalidOperationException(
            $"{ToStringShort()} has been unmounted and no longer has a State.");

    public override Widget Build() => State.Build(this);

    public override void Reassemble()
    {
        State.Reassemble();
        base.Reassemble();
    }

    private protected override void FirstBuild()
    {
        State.RunInitState();
        State.DidChangeDependencies();
        State.MarkReady();
        base.FirstBuild();
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
        base.Update(newWidget);
        DebugAssertions.Assert(ReferenceEquals(Widget, newWidget));
        StatefulWidget oldWidget = State.WidgetOrNull!;
        State.SetWidget((StatefulWidget)Widget);
        State.RunDidUpdateWidget(oldWidget);
        Rebuild(force: true);
    }

    protected override void OnActivate()
    {
        base.OnActivate();
        State.ActivateTickerProvider();
        State.Activate();

        // Otherwise MarkNeedsBuild is a no-op.
        DebugAssertions.Assert(LifecycleState == ElementLifecycleState.Active);
        MarkNeedsBuild();
    }

    protected override void OnDeactivate()
    {
        State.Deactivate();
        base.OnDeactivate();
    }

    public override void Unmount()
    {
        base.Unmount();
        State state = State;
        state.Dispose();
        state.DisposeTickerProvider();
        state.DebugAssertDisposedCalledSuper();
        state.DetachElement();
        _state = null;
    }

    public override InheritedWidget DependOnInheritedElement(InheritedElement ancestor, object? aspect = null)
    {
        State.DebugCheckCanDependOnInherited(ancestor, this);
        return base.DependOnInheritedElement(ancestor, aspect);
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        _didChangeDependencies = true;
    }

    public override DiagnosticsNode ToDiagnosticsNode(string? name = null, DiagnosticsTreeStyle? style = null)
        => new ElementDiagnosticableTreeNode(name, this, style, stateful: true);

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<State>("state", _state, defaultValue: DiagnosticsDefaults.NullValue));
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

    public override Widget Build() => ((ProxyWidget)Widget).Child;

    public override void Update(Widget newWidget)
    {
        var oldWidget = (ProxyWidget)Widget;
        DebugAssertions.Assert(!ReferenceEquals(Widget, newWidget));
        base.Update(newWidget);
        DebugAssertions.Assert(ReferenceEquals(Widget, newWidget));
        Updated(oldWidget);
        Rebuild(force: true);
    }

    /// <summary>
    /// Dart's <c>ProxyElement.updated</c>: called when the widget changed, before this element is
    /// rebuilt. The default forwards to <see cref="NotifyClients"/>.
    /// </summary>
    public virtual void Updated(ProxyWidget oldWidget)
    {
        NotifyClients(oldWidget);
    }

    /// <summary>Dart's <c>ProxyElement.notifyClients</c>.</summary>
    public abstract void NotifyClients(ProxyWidget oldWidget);
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

    /// <summary>
    /// The type of the parent data the widget writes. Dart's <c>ParentDataElement.debugParentDataType</c>,
    /// which throws outside debug builds.
    /// </summary>
    public abstract Type DebugParentDataType { get; }
}

/// <summary>An <see cref="Element"/> that uses a <see cref="ParentDataWidget{T}"/> as its configuration.</summary>
public class ParentDataElement<T> : ParentDataElementBase where T : IParentData
{
    public ParentDataElement(ParentDataWidget<T> widget) : base(widget)
    {
    }

    internal override IParentDataWidget ParentDataWidget => (IParentDataWidget)Widget;

    public override Type DebugParentDataType
    {
        get
        {
            if (!Constants.KDebugMode)
            {
                throw new NotSupportedException("debugParentDataType is only supported in debug builds");
            }

            return typeof(T);
        }
    }

    private void ApplyParentData(ParentDataWidget<T> widget)
    {
        void ApplyParentDataToChild(Element child)
        {
            if (child is RenderObjectElement renderObjectElement)
            {
                renderObjectElement.UpdateParentData(widget);
            }
            else if (child.RenderObjectAttachingChild != null)
            {
                ApplyParentDataToChild(child.RenderObjectAttachingChild);
            }
        }

        if (RenderObjectAttachingChild != null)
        {
            ApplyParentDataToChild(RenderObjectAttachingChild);
        }
    }

    /// <summary>
    /// Dart's <c>ParentDataElement.applyWidgetOutOfTurn</c>: applies the parent data of a widget that
    /// is not this element's configuration, outside the build phase. Only legal for widgets whose
    /// <see cref="ParentDataWidget{T}.DebugCanApplyOutOfTurn"/> is true, and only when the widget
    /// wraps the same child.
    /// </summary>
    public void ApplyWidgetOutOfTurn(ParentDataWidget<T> newWidget)
    {
        DebugAssertions.Assert(newWidget.DebugCanApplyOutOfTurn());
        DebugAssertions.Assert(ReferenceEquals(newWidget.Child, ((ParentDataWidget<T>)Widget).Child));
        ApplyParentData(newWidget);
    }

    public override void NotifyClients(ProxyWidget oldWidget)
    {
        ApplyParentData((ParentDataWidget<T>)Widget);
    }
}

/// <summary>An <see cref="Element"/> that uses an <see cref="InheritedWidget"/> as its configuration.</summary>
public class InheritedElement : ProxyElement
{
    private readonly Dictionary<Element, object?> _dependents = [];

    public InheritedElement(InheritedWidget widget) : base(widget)
    {
    }

    private protected override void UpdateInheritance()
    {
        DebugAssertions.Assert(LifecycleState == ElementLifecycleState.Active);
        ImmutableDictionary<Type, InheritedElement> incomingWidgets =
            Parent?.InheritedElements ?? ImmutableDictionary<Type, InheritedElement>.Empty;
        InheritedElements = incomingWidgets.SetItem(Widget.GetType(), this);
    }

    /// <summary>Dart's <c>InheritedElement.debugDeactivated</c>: every dependent must have unregistered.</summary>
    public override void DebugDeactivated()
    {
        DebugAssertions.Assert(_dependents.Count == 0);
        base.DebugDeactivated();
    }

    /// <summary>Returns the dependencies value recorded for <paramref name="dependent"/>.</summary>
    public virtual object? GetDependencies(Element dependent)
    {
        _dependents.TryGetValue(dependent, out object? dependencies);
        return dependencies;
    }

    /// <summary>Sets the value returned by <see cref="GetDependencies"/> for <paramref name="dependent"/>.</summary>
    public virtual void SetDependencies(Element dependent, object? value)
    {
        _dependents[dependent] = value;
    }

    /// <summary>Called by <see cref="Element.DependOnInheritedElement"/> when a new dependent is added.</summary>
    public virtual void UpdateDependencies(Element dependent, object? aspect)
    {
        SetDependencies(dependent, value: null);
    }

    /// <summary>Called by <see cref="NotifyClients"/> for each dependent.</summary>
    public virtual void NotifyDependent(InheritedWidget oldWidget, Element dependent)
    {
        dependent.DidChangeDependencies();
    }

    /// <summary>Called by <see cref="Element.Deactivate"/> to remove the provided dependent.</summary>
    public virtual void RemoveDependent(Element dependent)
    {
        _dependents.Remove(dependent);
    }

    /// <summary>
    /// Dart's <c>InheritedElement.updated</c>: notify the dependents only when the widget says the
    /// change is observable.
    /// </summary>
    public override void Updated(ProxyWidget oldWidget)
    {
        if (((InheritedWidget)Widget).UpdateShouldNotify((InheritedWidget)oldWidget))
        {
            base.Updated(oldWidget);
        }
    }

    /// <summary>Dart's <c>InheritedElement.notifyClients</c>.</summary>
    public override void NotifyClients(ProxyWidget oldWidget)
    {
        DebugCheckOwnerBuildTargetExists("notifyClients");
        var inheritedOldWidget = (InheritedWidget)oldWidget;
        foreach (Element dependent in _dependents.Keys)
        {
            if (Constants.KDebugMode)
            {
                // Check that it really is our descendant.
                Element? ancestor = dependent.Parent;
                while (!ReferenceEquals(ancestor, this) && ancestor != null)
                {
                    ancestor = ancestor.Parent;
                }

                DebugAssertions.Assert(ReferenceEquals(ancestor, this));

                // Check that it really depends on us.
                DebugAssertions.Assert(dependent.DoesDependOnInheritedElement(this));
            }

            NotifyDependent(inheritedOldWidget, dependent);
        }
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
            || InheritedModelWidget.InvokeUpdateShouldNotifyDependent(
                (InheritedModel<TAspect>)oldWidget,
                dependencies))
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
