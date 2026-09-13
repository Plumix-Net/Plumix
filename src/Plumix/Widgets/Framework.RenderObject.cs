using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart

namespace Plumix.Widgets;

/// <summary>
/// C#-only: the element-side contract of a render-object parent. <see cref="RenderObjectElement"/> is
/// the framework's only implementer; test harness roots implement it to host a render tree without
/// a <see cref="View"/>.
/// </summary>
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

/// <summary>
/// RenderObjectWidgets provide the configuration for <see cref="RenderObjectElement"/>s, which wrap
/// <see cref="Rendering.RenderObject"/>s. Dart's <c>RenderObjectWidget</c>.
/// </summary>
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

/// <summary>
/// A superclass for <see cref="RenderObjectWidget"/>s that configure render objects with no children.
/// </summary>
public abstract class LeafRenderObjectWidget(Key? key = null) : RenderObjectWidget(key)
{
    public override Element CreateElement() => new LeafRenderObjectElement(this);
}

/// <summary>
/// A superclass for <see cref="RenderObjectWidget"/>s that configure render objects with a single child.
/// </summary>
public abstract class SingleChildRenderObjectWidget : RenderObjectWidget
{
    protected SingleChildRenderObjectWidget(Widget? child = null, Key? key = null) : base(key)
    {
        Child = child;
    }

    public Widget? Child { get; }

    public override Element CreateElement() => new SingleChildRenderObjectElement(this);
}

/// <summary>
/// A superclass for <see cref="RenderObjectWidget"/>s that configure render objects with a list of children.
/// </summary>
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

/// <summary>An <see cref="Element"/> that uses a <see cref="RenderObjectWidget"/> as its configuration.</summary>
public abstract class RenderObjectElement : Element, IRenderObjectHost
{
    private RenderObject? _renderObject;
    private bool _debugDoingBuild;
    private IRenderObjectHost? _ancestorRenderObjectElement;
    private Element? _ancestorRenderObjectHostElement;

    protected RenderObjectElement(RenderObjectWidget widget) : base(widget)
    {
    }

    /// <summary>
    /// The underlying render object for this element. Dart's <c>RenderObjectElement.renderObject</c>.
    /// </summary>
    public sealed override RenderObject RenderObject
    {
        get
        {
            if (Constants.KDebugMode && _renderObject == null)
            {
                throw new AssertionError($"{Diagnostics.DescribeType(GetType())} unmounted");
            }

            return _renderObject!;
        }
    }

    /// <summary>Kept for existing callers; identical to <see cref="RenderObject"/>.</summary>
    protected RenderObject RequireRenderObject() => RenderObject;

    public override Element? RenderObjectAttachingChild => null;

    /// <inheritdoc />
    public override bool DebugDoingBuild => _debugDoingBuild;

    protected RenderObjectWidget RenderObjectWidget => (RenderObjectWidget)Widget;

    /// <summary>
    /// Dart's <c>RenderObjectElement._findAncestorRenderObjectElement</c>: the nearest ancestor
    /// that hosts render objects, unless an ancestor on the way says (through
    /// <see cref="Element.DebugExpectsRenderObjectForSlot"/>) that the render object occupying this
    /// element's slot is not expected to attach to it — a <see cref="RootElement"/>, or the view
    /// slots of a <see cref="ViewAnchor"/>/<see cref="ViewCollection"/>.
    /// </summary>
    internal (IRenderObjectHost? host, Element? hostElement) FindAncestorRenderObjectHost()
    {
        Element? ancestor = Parent;
        while (ancestor != null && ancestor is not IRenderObjectHost)
        {
            if (Constants.KDebugMode && !ancestor.DebugExpectsRenderObjectForSlot(Slot))
            {
                ancestor = null;
            }

            ancestor = ancestor?.Parent;
        }

        if (Constants.KDebugMode && ancestor?.DebugExpectsRenderObjectForSlot(Slot) == false)
        {
            ancestor = null;
        }

        return ancestor is IRenderObjectHost host ? (host, ancestor) : (null, null);
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

        DebugAssertions.Assert(
            debugAncestorTypes.Count < result.Count || debugParentDataTypes.Count < result.Count);
        try
        {
            var information = new List<DiagnosticsNode>
            {
                new ErrorSummary("Incorrect use of ParentDataWidget."),
                new ErrorDescription(
                    "Competing ParentDataWidgets are providing parent data to the same RenderObject:"),
            };

            foreach (ParentDataElementBase ancestor in result.Where(
                         element => debugAncestorCulprits.Contains(element.GetType())))
            {
                IParentDataWidget widget = ancestor.ParentDataWidget;
                information.Add(new ErrorDescription(
                    $"- {ancestor.Widget}, which writes ParentData of type "
                    + $"{Diagnostics.DescribeType(ancestor.DebugParentDataType)}, (typically placed directly "
                    + $"inside a {Diagnostics.DescribeType(widget.DebugTypicalAncestorWidgetClass)} widget)"));
            }

            information.Add(new ErrorDescription(
                "A RenderObject can receive parent data from multiple ParentDataWidgets, but the Type of "
                + "ParentData must be unique to prevent one overwriting another."));
            information.Add(new ErrorHint(
                "Usually, this indicates that one or more of the offending ParentDataWidgets listed above "
                + "isn't placed inside a dedicated compatible ancestor widget that it isn't sharing with "
                + "another ParentDataWidget of the same type."));
            information.Add(new ErrorHint(
                "Otherwise, separating aspects of ParentData to prevent conflicts can be done using mixins, "
                + "mixing them all in on the full ParentData Object, such as KeepAlive does with "
                + "KeepAliveParentDataMixin."));
            information.Add(new ErrorDescription(
                "The ownership chain for the RenderObject that received the parent data was:\n  "
                + DebugGetCreatorChain(10)));
            throw new FlutterError(information);
        }
        catch (FlutterError error)
        {
            FrameworkErrors.ReportException(new ErrorSummary("while looking for parent data."), error);
        }
    }

    /// <summary>
    /// Dart's <c>RenderObjectElement._findAncestorParentDataElements</c>: collects every
    /// <see cref="ParentDataElementBase"/> between this element and its ancestor render object, in
    /// nearest-first order, and in debug builds reports the two ways that set can be illegal — two
    /// ancestors of the same widget type, or two ancestors writing the same parent-data type.
    /// </summary>
    private List<ParentDataElementBase> FindAncestorParentDataElements()
    {
        Element? ancestor = Parent;
        var result = new List<ParentDataElementBase>();
        var debugAncestorTypes = new HashSet<Type>();
        var debugParentDataTypes = new HashSet<Type>();
        var debugAncestorCulprits = new List<Type>();

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

    protected override void OnMount()
    {
        base.OnMount();
        if (Constants.KDebugMode)
        {
            _debugDoingBuild = true;
        }

        _renderObject = RenderObjectWidget.CreateRenderObject(this);
        DebugAssertions.Assert(!_renderObject.DebugDisposed);
        if (Constants.KDebugMode)
        {
            _debugDoingBuild = false;
        }

        DebugUpdateRenderObjectOwner();
        AttachRenderObject(Slot);

        // Clears the "dirty" flag.
        base.PerformRebuild();
    }

    public override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        DebugAssertions.Assert(ReferenceEquals(Widget, newWidget));
        DebugUpdateRenderObjectOwner();

        // Calls widget.UpdateRenderObject().
        PerformRenderObjectRebuild();
    }

    /// <summary>
    /// Stamps this element onto the render object as its <see cref="RenderObject.DebugCreator"/>, so
    /// an error reported from the render tree can name the widget that produced it. Debug only.
    /// </summary>
    /// <remarks>Flutter's <c>RenderObjectElement._debugUpdateRenderObjectOwner</c>.</remarks>
    private void DebugUpdateRenderObjectOwner()
    {
        if (Constants.KDebugMode)
        {
            RenderObject.DebugCreator = new DebugCreator(this);
        }
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
        if (Constants.KDebugMode)
        {
            _debugDoingBuild = true;
        }

        RenderObjectWidget.UpdateRenderObject(this, RenderObject);
        if (Constants.KDebugMode)
        {
            _debugDoingBuild = false;
        }

        // Clears the "dirty" flag.
        base.PerformRebuild();
    }

    protected override void OnDeactivate()
    {
        base.OnDeactivate();
        if (Constants.KDebugMode && RenderObject.Attached)
        {
            throw new AssertionError(
                "A RenderObject was still attached when attempting to deactivate its RenderObjectElement: "
                + RenderObject);
        }
    }

    public override void Unmount()
    {
        if (Constants.KDebugMode && RenderObject.DebugDisposed)
        {
            throw new AssertionError(
                "A RenderObject was disposed prior to its owning element being unmounted: " + RenderObject);
        }

        RenderObjectWidget oldWidget = RenderObjectWidget;
        base.Unmount();
        if (RenderObject.Attached)
        {
            // Plumix-only last-resort net for a render root that never went through DetachRenderObject.
            DetachRootRenderObject(RenderObject);
        }

        if (Constants.KDebugMode && RenderObject.Attached)
        {
            throw new AssertionError(
                "A RenderObject was still attached when attempting to unmount its RenderObjectElement: "
                + RenderObject);
        }

        oldWidget.DidUnmountRenderObject(RenderObject);
        _renderObject!.Dispose();
        _renderObject = null;
    }

    /// <summary>
    /// Dart's <c>RenderObjectElement._updateParentData</c>. When the widget cannot write to this
    /// render object's parent data, the mismatch is <em>reported</em> rather than thrown — the tree is
    /// already broken, and activating an <c>ErrorWidget</c> here would only pile on further failures —
    /// and the parent data is left untouched.
    /// </summary>
    internal void UpdateParentData(IParentDataWidget parentDataWidget)
    {
        bool applyParentData = true;
        if (Constants.KDebugMode)
        {
            try
            {
                if (!parentDataWidget.DebugIsValidRenderObject(RenderObject))
                {
                    applyParentData = false;
                    throw new FlutterError(
                    [
                        new ErrorSummary("Incorrect use of ParentDataWidget."),
                        .. parentDataWidget.DebugDescribeIncorrectParentDataType(
                            parentData: RenderObject.parentData,
                            parentDataCreator: _ancestorRenderObjectHostElement?.Widget as RenderObjectWidget,
                            ownershipChain: new ErrorDescription(DebugGetCreatorChain(10))),
                    ]);
                }
            }
            catch (FlutterError error)
            {
                FrameworkErrors.ReportException(new ErrorSummary("while applying parent data."), error);
            }
        }

        if (applyParentData)
        {
            parentDataWidget.ApplyParentData(RenderObject);
        }
    }

    public override void UpdateSlot(object? newSlot)
    {
        object? oldSlot = Slot;
        DebugAssertions.Assert(!Equals(oldSlot, newSlot));
        base.UpdateSlot(newSlot);
        DebugAssertions.Assert(Equals(Slot, newSlot));
        DebugAssertions.Assert(ReferenceEquals(_ancestorRenderObjectElement, FindAncestorRenderObjectHost().host));
        _ancestorRenderObjectElement?.MoveRenderObjectChild(RenderObject, oldSlot, Slot);
    }

    public override void AttachRenderObject(object? newSlot)
    {
        DebugAssertions.Assert(_ancestorRenderObjectElement == null);
        Slot = newSlot;
        (_ancestorRenderObjectElement, _ancestorRenderObjectHostElement) = FindAncestorRenderObjectHost();
        if (Constants.KDebugMode && _ancestorRenderObjectElement == null)
        {
            // Reported, not thrown: the render object simply never joins a tree.
            FlutterError.ReportError(new FlutterErrorDetails(
                new FlutterError(
                [
                    new ErrorSummary(
                        $"The render object for {ToStringShort()} cannot find ancestor render object to attach to."),
                    new ErrorDescription(
                        "The ownership chain for the RenderObject in question was:\n  "
                        + DebugGetCreatorChain(10)),
                    new ErrorHint(
                        "Try wrapping your widget in a View widget or any other widget that is backed by a "
                        + "RenderTreeRootElement to serve as the root of the render tree."),
                ])));
        }

        _ancestorRenderObjectElement?.InsertRenderObjectChild(RenderObject, newSlot);
        foreach (ParentDataElementBase parentDataElement in FindAncestorParentDataElements())
        {
            UpdateParentData(parentDataElement.ParentDataWidget);
        }
    }

    public override void DetachRenderObject()
    {
        if (_ancestorRenderObjectElement != null)
        {
            _ancestorRenderObjectElement.RemoveRenderObjectChild(RenderObject, Slot);
            _ancestorRenderObjectElement = null;
            _ancestorRenderObjectHostElement = null;
        }

        if (_renderObject is { Attached: true } rootRenderObject)
        {
            // Plumix-only: Dart's render root is always owned by an element, so `detachRenderObject`
            // always has an ancestor element to remove it from. A Plumix host (and every test
            // harness) can attach the render root to its `RenderView` directly, outside
            // `IRenderObjectHost`, and that attachment has to come off here or the element is
            // deactivated with a live render object.
            DetachRootRenderObject(rootRenderObject);
        }

        Slot = null;
    }

    private static void DetachRootRenderObject(RenderObject renderObject)
    {
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

    /// <summary>Insert the given child into <see cref="RenderObject"/> at the given slot.</summary>
    public abstract void InsertRenderObjectChild(RenderObject child, object? slot);

    /// <summary>Move the given child from the given old slot to the given new slot.</summary>
    public abstract void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot);

    /// <summary>Remove the given child from <see cref="RenderObject"/>.</summary>
    public abstract void RemoveRenderObjectChild(RenderObject child, object? slot);

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<RenderObject>(
            "renderObject",
            _renderObject,
            defaultValue: DiagnosticsDefaults.NullValue));
    }
}

/// <summary>An <see cref="Element"/> that uses a <see cref="LeafRenderObjectWidget"/> as its configuration.</summary>
public class LeafRenderObjectElement : RenderObjectElement
{
    public LeafRenderObjectElement(LeafRenderObjectWidget widget) : base(widget)
    {
    }

    public override void ForgetChild(Element child)
    {
        DebugAssertions.Assert(false);
        base.ForgetChild(child);
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        DebugAssertions.Assert(false);
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        DebugAssertions.Assert(false);
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        DebugAssertions.Assert(false);
    }

    public override List<DiagnosticsNode> DebugDescribeChildren() => Widget.DebugDescribeChildren();
}

/// <summary>
/// An <see cref="Element"/> that uses a <see cref="SingleChildRenderObjectWidget"/> as its configuration.
/// </summary>
public class SingleChildRenderObjectElement : RenderObjectElement
{
    private Element? _child;

    public SingleChildRenderObjectElement(SingleChildRenderObjectWidget widget) : base(widget)
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
        DebugAssertions.Assert(ReferenceEquals(child, _child));
        _child = null;
        base.ForgetChild(child);
    }

    protected override void OnMount()
    {
        base.OnMount();
        _child = UpdateChild(_child, ((SingleChildRenderObjectWidget)Widget).Child, null);
    }

    public override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        DebugAssertions.Assert(ReferenceEquals(Widget, newWidget));
        _child = UpdateChild(_child, ((SingleChildRenderObjectWidget)Widget).Child, null);
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        var renderObject = (IRenderObjectSingleChildContainer)RenderObject;
        DebugAssertions.Assert(slot == null);
        renderObject.Child = child;
        DebugAssertions.Assert(ReferenceEquals(renderObject, RenderObject));
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        DebugAssertions.Assert(false);
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        var renderObject = (IRenderObjectSingleChildContainer)RenderObject;
        DebugAssertions.Assert(slot == null);
        DebugAssertions.Assert(ReferenceEquals(renderObject.Child, child));
        renderObject.Child = null;
        DebugAssertions.Assert(ReferenceEquals(renderObject, RenderObject));
    }
}

/// <summary>
/// An <see cref="Element"/> that uses a <see cref="MultiChildRenderObjectWidget"/> as its configuration.
/// </summary>
public class MultiChildRenderObjectElement : RenderObjectElement
{
    private List<Element> _children = [];
    private readonly HashSet<Element> _forgottenChildren = [];

    public MultiChildRenderObjectElement(MultiChildRenderObjectWidget widget) : base(widget)
    {
        DebugAssertions.Assert(!WidgetsDebug.DebugChildrenHaveDuplicateKeys(widget, widget.Children));
    }

    /// <summary>
    /// The current list of children of this element, without the ones that have been forgotten.
    /// Dart's <c>MultiChildRenderObjectElement.children</c>.
    /// </summary>
    public IEnumerable<Element> Children => _children.Where(child => !_forgottenChildren.Contains(child));

    private IRenderObjectContainer Container => (IRenderObjectContainer)RenderObject;

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        IRenderObjectContainer renderObject = Container;
        renderObject.Insert(child, after: ((IndexedSlot<Element?>)slot!).Value?.RenderObject);
        DebugAssertions.Assert(ReferenceEquals(renderObject, RenderObject));
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        IRenderObjectContainer renderObject = Container;
        DebugAssertions.Assert(ReferenceEquals(child.Parent, renderObject));
        renderObject.Move(child, after: ((IndexedSlot<Element?>)newSlot!).Value?.RenderObject);
        DebugAssertions.Assert(ReferenceEquals(renderObject, RenderObject));
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        IRenderObjectContainer renderObject = Container;
        DebugAssertions.Assert(ReferenceEquals(child.Parent, renderObject));
        renderObject.Remove(child);
        DebugAssertions.Assert(ReferenceEquals(renderObject, RenderObject));
    }

    public override void VisitChildren(Action<Element> visitor)
    {
        foreach (Element child in _children)
        {
            if (!_forgottenChildren.Contains(child))
            {
                visitor(child);
            }
        }
    }

    public override void ForgetChild(Element child)
    {
        DebugAssertions.Assert(_children.Contains(child));
        DebugAssertions.Assert(!_forgottenChildren.Contains(child));
        _forgottenChildren.Add(child);
        base.ForgetChild(child);
    }

    /// <summary>Dart's <c>MultiChildRenderObjectElement._debugCheckHasAssociatedRenderObject</c>.</summary>
    private static void DebugCheckHasAssociatedRenderObject(Element newChild)
    {
        if (!Constants.KDebugMode || newChild.RenderObject != null)
        {
            return;
        }

        FlutterError.ReportError(new FlutterErrorDetails(
            new FlutterError(
            [
                new ErrorSummary(
                    "The children of `MultiChildRenderObjectElement` must each has an associated render object."),
                new ErrorHint(
                    $"This typically means that the `{newChild.Widget}` or its children\n"
                    + "are not a subtype of `RenderObjectWidget`."),
                newChild.DescribeElement("The following element does not have an associated render object"),
                new DiagnosticsDebugCreator(new DebugCreator(newChild)),
            ])));
    }

    public override Element InflateWidget(Widget newWidget, object? newSlot)
    {
        Element newChild = base.InflateWidget(newWidget, newSlot);
        DebugCheckHasAssociatedRenderObject(newChild);
        return newChild;
    }

    protected override void OnMount()
    {
        base.OnMount();
        IReadOnlyList<Widget> widgets = ((MultiChildRenderObjectWidget)Widget).Children;

        // Dart fills a local list and assigns `_children` after the loop. Plumix records each child as
        // it inflates, so a child that throws (a reported error rethrown by the host) leaves its
        // already-inflated siblings reachable by the failed-subtree walk instead of orphaned.
        _children = new List<Element>(widgets.Count);
        Element? previousChild = null;
        for (int i = 0; i < widgets.Count; i += 1)
        {
            Element newChild = InflateWidget(widgets[i], new IndexedSlot<Element?>(i, previousChild));
            _children.Add(newChild);
            previousChild = newChild;
        }
    }

    public override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        var multiChildRenderObjectWidget = (MultiChildRenderObjectWidget)Widget;
        DebugAssertions.Assert(ReferenceEquals(Widget, newWidget));
        DebugAssertions.Assert(
            !WidgetsDebug.DebugChildrenHaveDuplicateKeys(Widget, multiChildRenderObjectWidget.Children));
        _children = UpdateChildren(_children, multiChildRenderObjectWidget.Children, _forgottenChildren);
        _forgottenChildren.Clear();
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
        if (RenderObject is ISlottedRenderObjectContainer container)
        {
            return container;
        }

        throw new InvalidOperationException(
            $"{RenderObject.GetType().Name} must implement {nameof(ISlottedRenderObjectContainer)}.");
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
