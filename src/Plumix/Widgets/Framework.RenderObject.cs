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
    public abstract RenderObject CreateRenderObject(BuildContext context);

    public virtual void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
    }

    public virtual void DidUnmountRenderObject(RenderObject renderObject)
    {
    }
}

public abstract class LeafRenderObjectWidget(Key? key = null) : RenderObjectWidget(key)
{
    public override Element CreateElement() => new LeafRenderObjectElement(this);
}

public abstract class SingleChildRenderObjectWidget : RenderObjectWidget
{
    protected SingleChildRenderObjectWidget(Widget? child = null, Key? key = null) : base(key)
    {
        Child = child;
    }

    public Widget? Child { get; }

    public override Element CreateElement() => new SingleChildRenderObjectElement(this);
}

public abstract class MultiChildRenderObjectWidget : RenderObjectWidget
{
    protected MultiChildRenderObjectWidget(IReadOnlyList<Widget>? children = null, Key? key = null) : base(key)
    {
        Children = children ?? [];
    }

    public IReadOnlyList<Widget> Children { get; }

    public override Element CreateElement() => new MultiChildRenderObjectElement(this);
}

public abstract class SlottedMultiChildRenderObjectWidget<TSlot> : RenderObjectWidget
    where TSlot : notnull
{
    protected SlottedMultiChildRenderObjectWidget(Key? key = null) : base(key)
    {
    }

    public abstract IReadOnlyList<TSlot> Slots { get; }

    public abstract Widget? ChildForSlot(TSlot slot);

    public override Element CreateElement() => new SlottedRenderObjectElement<TSlot>(this);
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
        DebugDoingBuild = true;
        try
        {
            _renderObject = RenderObjectWidget.CreateRenderObject(this);
        }
        finally
        {
            DebugDoingBuild = false;
        }

        DebugUpdateRenderObjectOwner();
        AttachRenderObject(Slot);

        // Dart's RenderObjectElement.mount clears the dirty flag itself rather than building.
        base.PerformRebuild();
    }

    /// <summary>
    /// Stamps this element onto the render object as its <see cref="RenderObject.DebugCreator"/>, so
    /// an error reported from the render tree can name the widget that produced it. Debug only.
    /// </summary>
    /// <remarks>Flutter's <c>RenderObjectElement._debugUpdateRenderObjectOwner</c>.</remarks>
    private void DebugUpdateRenderObjectOwner()
    {
        if (Constants.KDebugMode && _renderObject is not null)
        {
            _renderObject.DebugCreator = new DebugCreator(this);
        }
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

    public override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        DebugUpdateRenderObjectOwner();
        PerformRenderObjectRebuild();
    }

    protected override void PerformRebuild() => PerformRenderObjectRebuild();

    /// <summary>
    /// Pushes the widget's configuration into the render object and clears <see cref="Element.Dirty"/>.
    /// </summary>
    /// <remarks>
    /// Dart's private <c>RenderObjectElement._performRebuild</c>. <c>update</c> goes through this
    /// rather than through the virtual <c>performRebuild</c>, so a subclass that rebuilds children
    /// there (a sliver or list-wheel adaptor) is not asked to rebuild them twice.
    /// </remarks>
    private void PerformRenderObjectRebuild()
    {
        DebugDoingBuild = true;
        try
        {
            RenderObjectWidget.UpdateRenderObject(this, RequireRenderObject());
        }
        finally
        {
            DebugDoingBuild = false;
        }

        base.PerformRebuild();
    }

    public override void UpdateSlot(object? newSlot)
    {
        object? oldSlot = Slot;
        base.UpdateSlot(newSlot);

        if (_ancestorRenderObjectHost != null && !Equals(oldSlot, newSlot))
        {
            _ancestorRenderObjectHost.MoveRenderObjectChild(RequireRenderObject(), oldSlot, newSlot);
        }
    }

    /// <summary>
    /// Dart's <c>RenderObjectElement._updateParentData</c>. When the widget cannot write to this
    /// render object's parent data, the mismatch is <em>reported</em> rather than thrown — the tree is
    /// already broken, and activating an <c>ErrorWidget</c> here would only pile on further failures —
    /// and the parent data is left untouched.
    /// </summary>
    internal void UpdateParentData(IParentDataWidget parentDataWidget)
    {
        var renderObject = RequireRenderObject();
        bool applyParentData = true;
        if (Constants.KDebugMode && !parentDataWidget.DebugIsValidRenderObject(renderObject))
        {
            applyParentData = false;
            var error = new FlutterError(
            [
                new ErrorSummary("Incorrect use of ParentDataWidget."),
                .. parentDataWidget.DebugDescribeIncorrectParentDataType(
                    parentData: renderObject.parentData,
                    parentDataCreator: _ancestorRenderObjectHostElement is RenderObjectElement ancestorElement
                        ? (RenderObjectWidget)ancestorElement.Widget
                        : null,
                    ownershipChain: new ErrorDescription(DebugGetCreatorChain(10))),
            ]);
            FrameworkErrors.ReportException(new ErrorSummary("while applying parent data."), error);
        }

        if (applyParentData)
        {
            parentDataWidget.ApplyParentData(renderObject);
        }
    }

    protected RenderObject RequireRenderObject()
    {
        return _renderObject ?? throw new InvalidOperationException("RenderObjectElement is not mounted.");
    }

    public override void AttachRenderObject(object? newSlot)
    {
        if (_ancestorRenderObjectHost != null)
        {
            throw new AssertionError("A RenderObjectElement cannot attach its render object twice.");
        }

        base.UpdateSlot(newSlot);
        (_ancestorRenderObjectHost, _ancestorRenderObjectHostElement) = FindAncestorRenderObjectHost();
        if (_ancestorRenderObjectHost == null)
        {
            throw new FlutterError(
            [
                new ErrorSummary(
                    $"The render object for {ToStringShort()} cannot find ancestor render object "
                    + "to attach to."),
                new ErrorDescription(
                    "The ownership chain for the RenderObject in question was:\n  "
                    + DebugGetCreatorChain(10)),
                new ErrorHint(
                    "Try wrapping your widget in a View widget or any other widget that is backed "
                    + "by a RenderTreeRootElement to serve as the root of the render tree."),
            ]);
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

    /// <summary>
    /// Dart's <c>RenderObjectElement._findAncestorParentDataElements</c>: collects every
    /// <see cref="ParentDataElementBase"/> between this element and its ancestor render object, in
    /// nearest-first order, and in debug builds reports the two ways that set can be illegal — two
    /// ancestors of the same widget type, or two ancestors writing the same parent-data type.
    /// </summary>
    private List<ParentDataElementBase> FindAncestorParentDataElements()
    {
        var result = new List<ParentDataElementBase>();
        var debugAncestorTypes = new HashSet<Type>();
        var debugParentDataTypes = new HashSet<Type>();
        var debugAncestorCulprits = new List<Type>();

        Element? ancestor = Parent;
        while (ancestor != null && ancestor is not RenderObjectElement)
        {
            if (ancestor is ParentDataElementBase parentDataElement)
            {
                if (Constants.KDebugMode
                    && (!debugAncestorTypes.Add(parentDataElement.GetType())
                        || !debugParentDataTypes.Add(parentDataElement.DebugParentDataType)))
                {
                    debugAncestorCulprits.Add(parentDataElement.GetType());
                }

                result.Add(parentDataElement);
            }

            ancestor = ancestor.Parent;
        }

        if (Constants.KDebugMode && result.Count > 0 && ancestor != null)
        {
            DebugCheckCompetingAncestors(result, debugAncestorTypes, debugParentDataTypes, debugAncestorCulprits);
        }

        return result;
    }

    /// <summary>Dart's <c>RenderObjectElement._debugCheckCompetingAncestors</c>.</summary>
    private void DebugCheckCompetingAncestors(
        List<ParentDataElementBase> result,
        HashSet<Type> debugAncestorTypes,
        HashSet<Type> debugParentDataTypes,
        List<Type> debugAncestorCulprits)
    {
        if (debugAncestorTypes.Count == result.Count && debugParentDataTypes.Count == result.Count)
        {
            return;
        }

        var information = new List<DiagnosticsNode>
        {
            new ErrorSummary("Incorrect use of ParentDataWidget."),
            new ErrorDescription("Competing ParentDataWidgets are providing parent data to the same RenderObject:"),
        };

        foreach (ParentDataElementBase ancestor in result.Where(
                     element => debugAncestorCulprits.Contains(element.GetType())))
        {
            IParentDataWidget widget = ancestor.ParentDataWidget;
            information.Add(new ErrorDescription(
                $"- {ancestor.Widget}, which writes ParentData of type "
                + $"{Diagnostics.DescribeType(ancestor.DebugParentDataType)}, (typically placed directly "
                + $"inside a {Diagnostics.DescribeType(widget.DebugTypicalAncestorWidgetType)} widget)"));
        }

        information.Add(new ErrorDescription(
            "A RenderObject can receive parent data from multiple ParentDataWidgets, but the Type of "
            + "ParentData must be unique to prevent one overwriting another."));
        information.Add(new ErrorHint(
            "Usually, this indicates that one or more of the offending ParentDataWidgets listed above isn't "
            + "placed inside a dedicated compatible ancestor widget that it isn't sharing with another "
            + "ParentDataWidget of the same type."));
        information.Add(new ErrorHint(
            "Otherwise, separating aspects of ParentData to prevent conflicts can be done using mixins, "
            + "mixing them all in on the full ParentData Object, such as KeepAlive does with "
            + "KeepAliveParentDataMixin."));
        information.Add(new ErrorDescription(
            "The ownership chain for the RenderObject that received the parent data was:\n  "
            + DebugGetCreatorChain(10)));

        FrameworkErrors.ReportException(
            new ErrorSummary("while looking for parent data."),
            new FlutterError(information));
    }

    private void ApplyParentDataFromAncestors()
    {
        foreach (ParentDataElementBase parentDataElement in FindAncestorParentDataElements())
        {
            UpdateParentData(parentDataElement.ParentDataWidget);
        }
    }

    public override void DetachRenderObject()
    {
        if (_ancestorRenderObjectHost != null)
        {
            _ancestorRenderObjectHost.RemoveRenderObjectChild(RequireRenderObject(), Slot);
        }

        _ancestorRenderObjectHost = null;
        _ancestorRenderObjectHostElement = null;
        base.UpdateSlot(null);
    }

    public override void Unmount()
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

public class SingleChildRenderObjectElement : RenderObjectElement
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

    protected override void PerformRebuild()
    {
        base.PerformRebuild();
        _child = UpdateChild(_child, ((SingleChildRenderObjectWidget)Widget).Child, null);
    }

    public override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        _child = UpdateChild(_child, ((SingleChildRenderObjectWidget)Widget).Child, null);
    }

    public override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _child))
        {
            _child = null;
        }
    }

    public override void VisitChildren(Action<Element> visitor)
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

    protected override void PerformRebuild()
    {
        base.PerformRebuild();
        _children = UpdateChildren(_children, ((MultiChildRenderObjectWidget)Widget).Children, _forgottenChildren);
        _forgottenChildren.Clear();
        EnsureChildrenHaveAssociatedRenderObjects();
    }

    public override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        _children = UpdateChildren(_children, ((MultiChildRenderObjectWidget)Widget).Children, _forgottenChildren);
        _forgottenChildren.Clear();
        EnsureChildrenHaveAssociatedRenderObjects();
    }

    public override void ForgetChild(Element child)
    {
        _forgottenChildren.Add(child);
    }

    public override void VisitChildren(Action<Element> visitor)
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

    public override void Unmount()
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

    protected override void PerformRebuild()
    {
        base.PerformRebuild();
        UpdateSlotChildren();
    }

    public override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        UpdateSlotChildren();
    }

    public override void ForgetChild(Element child)
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

    public override void VisitChildren(Action<Element> visitor)
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

    public override void Unmount()
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

/// <summary>
/// A wrapper for the <see cref="Element"/> that created a <see cref="RenderObject"/>. Setting one
/// as <see cref="RenderObject.DebugCreator"/> is what lets a rendering-library error name the
/// widget chain that produced the offending render object.
/// </summary>
/// <remarks>Flutter's <c>DebugCreator</c>.</remarks>
public sealed class DebugCreator
{
    /// <summary>Creates a creator marker for <paramref name="element"/>.</summary>
    public DebugCreator(Element element)
    {
        ArgumentNullException.ThrowIfNull(element);

        Element = element;
    }

    /// <summary>The element that created the render object.</summary>
    public Element Element { get; }

    /// <inheritdoc />
    public override string ToString() => Element.DebugGetCreatorChain(12);
}
