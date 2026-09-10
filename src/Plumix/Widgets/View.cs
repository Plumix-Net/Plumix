using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/view.dart

namespace Plumix.Widgets;

/// <summary>
/// Bootstraps a render tree that is rendered into the provided <see cref="FlutterView"/>.
/// </summary>
/// <remarks>
/// <para>
/// Flutter's <c>View</c>. The content rendered into the view is determined by <see cref="Child"/>,
/// which is given a <see cref="MediaQuery"/> built from the view, a per-view
/// <see cref="FocusScope"/> attached to <see cref="FocusManager.RootScope"/>, and a
/// <see cref="FocusTraversalGroup"/> with a <see cref="ReadingOrderTraversalPolicy"/>. Descendants
/// can access the view through <see cref="Of"/>/<see cref="MaybeOf"/> and the pipeline owner
/// driving it through <see cref="PipelineOwnerOf"/>.
/// </para>
/// <para>
/// The two deprecated constructor arguments exist only for a host that owns its own
/// <c>PipelineOwner</c>/<c>RenderView</c> pair, exactly like Flutter's implicit view over the
/// deprecated <c>RendererBinding.pipelineOwner</c>/<c>renderView</c>.
/// </para>
/// </remarks>
public sealed class View : StatefulWidget
{
    /// <summary>Creates a widget that bootstraps a render tree for <paramref name="view"/>.</summary>
    public View(
        FlutterView view,
        Widget child,
        PipelineOwner? deprecatedDoNotUseWillBeRemovedWithoutNoticePipelineOwner = null,
        RenderView? deprecatedDoNotUseWillBeRemovedWithoutNoticeRenderView = null,
        Key? key = null)
        : base(key)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(child);
        DebugCheckDeprecatedPair(
            view,
            deprecatedDoNotUseWillBeRemovedWithoutNoticePipelineOwner,
            deprecatedDoNotUseWillBeRemovedWithoutNoticeRenderView);
        ViewHandle = view;
        Child = child;
        DeprecatedPipelineOwner = deprecatedDoNotUseWillBeRemovedWithoutNoticePipelineOwner;
        DeprecatedRenderView = deprecatedDoNotUseWillBeRemovedWithoutNoticeRenderView;
    }

    /// <summary>The <see cref="FlutterView"/> into which <see cref="Child"/> is drawn.</summary>
    /// <remarks>
    /// Dart's <c>View.view</c>; C# forbids a member named after its enclosing type, see
    /// <c>docs/ai/DIVERGENCES.md</c>.
    /// </remarks>
    public FlutterView ViewHandle { get; }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget Child { get; }

    internal PipelineOwner? DeprecatedPipelineOwner { get; }

    internal RenderView? DeprecatedRenderView { get; }

    internal static void DebugCheckDeprecatedPair(FlutterView view, PipelineOwner? owner, RenderView? renderView)
    {
        if ((owner is null) != (renderView is null))
        {
            throw new AssertionError(
                "The deprecated pipeline owner and render view must be provided together or not at all.");
        }

        if (renderView is not null && !ReferenceEquals(renderView.FlutterView, view))
        {
            throw new AssertionError("The deprecated render view must render into the view of this widget.");
        }
    }

    /// <summary>
    /// Returns the <see cref="FlutterView"/> that the provided <paramref name="context"/> will
    /// render into, or <see langword="null"/> when there is none in scope.
    /// </summary>
    /// <remarks>Flutter's <c>View.maybeOf</c>. The lookup stops at a <see cref="LookupBoundary"/>.</remarks>
    public static FlutterView? MaybeOf(BuildContext context)
    {
        return LookupBoundary.DependOnInheritedWidgetOfExactType<ViewScope>(context)?.View;
    }

    /// <summary>
    /// Returns the <see cref="FlutterView"/> that the provided <paramref name="context"/> will render into.
    /// </summary>
    /// <remarks>Flutter's <c>View.of</c>. Throws a <see cref="FlutterError"/> when there is none in scope.</remarks>
    public static FlutterView Of(BuildContext context)
    {
        FlutterView? result = MaybeOf(context);
        if (result is not null)
        {
            return result;
        }

        bool hiddenByBoundary = LookupBoundary.DebugIsHidingAncestorWidgetOfExactType<ViewScope>(context);
        var information = new List<DiagnosticsNode>();
        if (hiddenByBoundary)
        {
            information.Add(new ErrorSummary(
                "View.of() was called with a context that does not have access to a View widget."));
            information.Add(new ErrorDescription(
                "The context provided to View.of() does have a View widget ancestor, but it is hidden by "
                + "a LookupBoundary."));
        }
        else
        {
            information.Add(new ErrorSummary(
                "View.of() was called with a context that does not contain a View widget."));
            information.Add(new ErrorDescription(
                "No View widget ancestor could be found starting from the context that was passed to "
                + "View.of()."));
        }

        information.Add(new ErrorDescription($"The context used was:\n  {context}"));
        information.Add(new ErrorHint(
            "This usually means that the provided context is not associated with a View."));
        throw new FlutterError(information);
    }

    /// <summary>
    /// Returns the <see cref="PipelineOwner"/> parent to which a child <see cref="View"/> should
    /// attach its own owner: the nearest enclosing view's owner, or the renderer binding's root.
    /// </summary>
    /// <remarks>Flutter's <c>View.pipelineOwnerOf</c>; unlike <see cref="Of"/> it ignores lookup boundaries.</remarks>
    public static PipelineOwner PipelineOwnerOf(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.DependOnInherited<PipelineOwnerScope>()?.PipelineOwner
               ?? RendererBinding.Instance.RootPipelineOwner;
    }

    /// <inheritdoc />
    public override State CreateState() => new ViewState();

    /// <summary>Flutter's <c>_ViewState</c>: the per-view focus scope and its platform focus plumbing.</summary>
    private sealed class ViewState : State, WidgetsBindingObserver
    {
        private readonly FocusScopeNode _scopeNode =
            new(debugLabel: Constants.KReleaseMode ? null : "View Scope");
        private readonly FocusTraversalPolicy _policy = new ReadingOrderTraversalPolicy();
        private bool _viewHasFocus;

        private View TypedWidget => (View)StateWidget;

        public override void InitState()
        {
            base.InitState();
            WidgetsBinding.Instance.AddObserver(this);
            _scopeNode.AddListener(ScopeFocusChangeListener);
        }

        public override void Dispose()
        {
            WidgetsBinding.Instance.RemoveObserver(this);
            _scopeNode.RemoveListener(ScopeFocusChangeListener);
            _scopeNode.Dispose();
            base.Dispose();
        }

        private void ScopeFocusChangeListener()
        {
            if (_viewHasFocus == _scopeNode.HasFocus || !_scopeNode.HasFocus)
            {
                return;
            }

            PlatformDispatcher.Instance.RequestViewFocusChange(
                TypedWidget.ViewHandle.ViewId,
                ViewFocusState.Focused,
                ViewFocusDirection.Forward);
        }

        public void DidChangeViewFocus(ViewFocusEvent @event)
        {
            int viewId = TypedWidget.ViewHandle.ViewId;
            _viewHasFocus = @event.State switch
            {
                ViewFocusState.Focused => @event.ViewId == viewId,
                _ => false,
            };

            if (@event.ViewId != viewId)
            {
                return;
            }

            switch (@event.State)
            {
                case ViewFocusState.Focused:
                    FocusNode nextFocus = @event.Direction switch
                    {
                        ViewFocusDirection.Forward => _policy.FindFirstFocus(_scopeNode, ignoreCurrentFocus: true)
                                                      ?? _scopeNode,
                        ViewFocusDirection.Backward => _policy.FindLastFocus(_scopeNode, ignoreCurrentFocus: true),
                        _ => _scopeNode,
                    };
                    nextFocus.RequestFocus();
                    break;
                case ViewFocusState.Unfocused:
                    FocusManager.Instance.RootScope.RequestScopeFocus();
                    break;
            }
        }

        public override Widget Build(BuildContext context)
        {
            View widget = TypedWidget;
            return new RawView(
                view: widget.ViewHandle,
                deprecatedDoNotUseWillBeRemovedWithoutNoticePipelineOwner: widget.DeprecatedPipelineOwner,
                deprecatedDoNotUseWillBeRemovedWithoutNoticeRenderView: widget.DeprecatedRenderView,
                child: MediaQuery.FromView(
                    view: widget.ViewHandle,
                    child: new FocusTraversalGroup(
                        policy: _policy,
                        parentNode: FocusManager.Instance.RootScope,
                        child: FocusScope.WithExternalFocusNode(
                            includeSemantics: false,
                            focusScopeNode: _scopeNode,
                            child: widget.Child))));
        }
    }
}

/// <summary>
/// The lower-level version of <see cref="View"/>: it bootstraps a render tree for its view but
/// installs no <see cref="MediaQuery"/> or focus scope.
/// </summary>
/// <remarks>Flutter's <c>RawView</c>.</remarks>
public sealed class RawView : StatelessWidget
{
    /// <summary>Creates a widget that bootstraps a render tree for <paramref name="view"/>.</summary>
    public RawView(
        FlutterView view,
        Widget child,
        PipelineOwner? deprecatedDoNotUseWillBeRemovedWithoutNoticePipelineOwner = null,
        RenderView? deprecatedDoNotUseWillBeRemovedWithoutNoticeRenderView = null,
        Key? key = null)
        : base(key)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(child);
        View.DebugCheckDeprecatedPair(
            view,
            deprecatedDoNotUseWillBeRemovedWithoutNoticePipelineOwner,
            deprecatedDoNotUseWillBeRemovedWithoutNoticeRenderView);
        ViewHandle = view;
        Child = child;
        DeprecatedPipelineOwner = deprecatedDoNotUseWillBeRemovedWithoutNoticePipelineOwner;
        DeprecatedRenderView = deprecatedDoNotUseWillBeRemovedWithoutNoticeRenderView;
    }

    /// <summary>The <see cref="FlutterView"/> into which <see cref="Child"/> is drawn.</summary>
    public FlutterView ViewHandle { get; }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget Child { get; }

    internal PipelineOwner? DeprecatedPipelineOwner { get; }

    internal RenderView? DeprecatedRenderView { get; }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        return new RawViewInternal(
            view: ViewHandle,
            deprecatedPipelineOwner: DeprecatedPipelineOwner,
            deprecatedRenderView: DeprecatedRenderView,
            builder: (_, owner) => new ViewScope(
                view: ViewHandle,
                child: new PipelineOwnerScope(pipelineOwner: owner, child: Child)));
    }
}

/// <summary>Flutter's <c>_RawViewContentBuilder</c>.</summary>
internal delegate Widget RawViewContentBuilder(BuildContext context, PipelineOwner owner);

/// <summary>Flutter's <c>_RawViewInternal</c>: the render object widget behind <see cref="RawView"/>.</summary>
internal sealed class RawViewInternal : RenderObjectWidget
{
    public RawViewInternal(
        FlutterView view,
        PipelineOwner? deprecatedPipelineOwner,
        RenderView? deprecatedRenderView,
        RawViewContentBuilder builder)
        : base(new DeprecatedRawViewKey(view, deprecatedPipelineOwner, deprecatedRenderView))
    {
        if (deprecatedRenderView is not null && !ReferenceEquals(deprecatedRenderView.FlutterView, view))
        {
            throw new AssertionError("The deprecated render view must render into the view of this widget.");
        }

        View = view;
        DeprecatedPipelineOwner = deprecatedPipelineOwner;
        DeprecatedRenderView = deprecatedRenderView;
        Builder = builder;
    }

    public FlutterView View { get; }

    public PipelineOwner? DeprecatedPipelineOwner { get; }

    public RenderView? DeprecatedRenderView { get; }

    public RawViewContentBuilder Builder { get; }

    public override Element CreateElement() => new RawViewElement(this);

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return DeprecatedRenderView ?? new RenderView(View);
    }

    // No UpdateRenderObject: the key guarantees that a widget update keeps the same view.
}

/// <summary>
/// Flutter's <c>_RawViewElement</c>: the <see cref="RenderTreeRootElement"/> that owns a view's pipeline.
/// </summary>
internal sealed class RawViewElement : RenderTreeRootElement
{
    private readonly PipelineOwner _pipelineOwner;
    private PipelineOwner? _parentPipelineOwner;
    private Element? _child;

    public RawViewElement(RawViewInternal widget) : base(widget)
    {
        _pipelineOwner = new PipelineOwner(
            onSemanticsOwnerCreated: HandleSemanticsOwnerCreated,
            onSemanticsUpdate: HandleSemanticsUpdate,
            onSemanticsOwnerDisposed: HandleSemanticsOwnerDisposed);
    }

    private RawViewInternal TypedWidget => (RawViewInternal)Widget;

    private PipelineOwner EffectivePipelineOwner => TypedWidget.DeprecatedPipelineOwner ?? _pipelineOwner;

    /// <summary>Dart's <c>_RawViewElement.renderObject</c>, narrowed to the view.</summary>
    private RenderView RenderViewObject => (RenderView)RequireRenderObject();

    /// <summary>The pipeline owner this element's view is driven by; for tests.</summary>
    internal PipelineOwner PipelineOwnerForTest => EffectivePipelineOwner;

    private void HandleSemanticsOwnerCreated()
    {
        (EffectivePipelineOwner.RootNode as RenderView)?.ScheduleInitialSemantics();
    }

    private void HandleSemanticsOwnerDisposed()
    {
        (EffectivePipelineOwner.RootNode as RenderView)?.ClearSemantics();
    }

    private void HandleSemanticsUpdate(SemanticsUpdate update)
    {
        TypedWidget.View.UpdateSemantics(update);
    }

    /// <summary>Dart's <c>_RawViewElement._updateChild</c>.</summary>
    private void UpdateChildFromBuilder()
    {
        try
        {
            Widget child = TypedWidget.Builder(this, EffectivePipelineOwner);
            _child = UpdateChild(_child, child, null);
        }
        catch (Exception exception)
        {
            var details = new FlutterErrorDetails(
                exception,
                stack: exception.StackTrace,
                library: "widgets library",
                context: new ErrorDescription($"building {this}"),
                informationCollector: Constants.KDebugMode
                    ? () => [new DiagnosticsDebugCreator(new DebugCreator(this))]
                    : null);
            FlutterError.ReportError(details);
            Widget error = ErrorWidget.Builder(details);
            _child = UpdateChild(null, error, Slot);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dart's <c>_RawViewElement.mount</c>, after <c>RenderObjectElement.mount</c> created the view.
    /// </remarks>
    protected override void OnMount()
    {
        base.OnMount();
        if (EffectivePipelineOwner.RootNode is not null)
        {
            throw new AssertionError(
                "The pipeline owner of a View must not be managing a root node when it mounts.");
        }

        EffectivePipelineOwner.RootNode = RenderViewObject;
        AttachView();
        UpdateChildFromBuilder();
        RenderViewObject.PrepareInitialFrame();
        if (EffectivePipelineOwner.SemanticsOwner is not null)
        {
            RenderViewObject.ScheduleInitialSemantics();
        }
    }

    /// <summary>Dart's <c>_RawViewElement._attachView</c>.</summary>
    private void AttachView(PipelineOwner? parentPipelineOwner = null)
    {
        if (_parentPipelineOwner is not null)
        {
            throw new AssertionError("The view is already attached to a parent pipeline owner.");
        }

        parentPipelineOwner ??= View.PipelineOwnerOf(this);
        parentPipelineOwner.AdoptChild(EffectivePipelineOwner);
        RendererBinding.Instance.AddRenderView(RenderViewObject);
        _parentPipelineOwner = parentPipelineOwner;
    }

    /// <summary>Dart's <c>_RawViewElement._detachView</c>.</summary>
    private void DetachView()
    {
        PipelineOwner? parentPipelineOwner = _parentPipelineOwner;
        if (parentPipelineOwner is not null)
        {
            RendererBinding.Instance.RemoveRenderView(RenderViewObject);
            parentPipelineOwner.DropChild(EffectivePipelineOwner);
            _parentPipelineOwner = null;
        }
    }

    /// <inheritdoc />
    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        if (_parentPipelineOwner is null)
        {
            // This element is detached and does not have a parent pipeline owner to update.
            return;
        }

        PipelineOwner newParentPipelineOwner = View.PipelineOwnerOf(this);
        if (!ReferenceEquals(newParentPipelineOwner, _parentPipelineOwner))
        {
            DetachView();
            AttachView(newParentPipelineOwner);
        }
    }

    /// <inheritdoc />
    protected override void PerformRebuild()
    {
        base.PerformRebuild();
        UpdateChildFromBuilder();
    }

    /// <inheritdoc />
    /// <remarks>Dart's <c>_RawViewElement.activate</c>.</remarks>
    protected override void OnActivate()
    {
        base.OnActivate();
        if (EffectivePipelineOwner.RootNode is not null)
        {
            throw new AssertionError(
                "The pipeline owner of a View must not be managing a root node when it activates.");
        }

        EffectivePipelineOwner.RootNode = RenderViewObject;
        AttachView();
    }

    /// <inheritdoc />
    /// <remarks>Dart's <c>_RawViewElement.deactivate</c>.</remarks>
    protected override void OnDeactivate()
    {
        DetachView();
        if (!ReferenceEquals(EffectivePipelineOwner.RootNode, RenderViewObject))
        {
            throw new AssertionError(
                "The pipeline owner of a View must be managing the view when it deactivates.");
        }

        EffectivePipelineOwner.RootNode = null;
        base.OnDeactivate();
    }

    /// <inheritdoc />
    public override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        UpdateChildFromBuilder();
    }

    /// <inheritdoc />
    public override void VisitChildren(Action<Element> visitor)
    {
        if (_child is not null)
        {
            visitor(_child);
        }
    }

    /// <inheritdoc />
    public override void ForgetChild(Element child)
    {
        if (!ReferenceEquals(child, _child))
        {
            throw new AssertionError("RawViewElement.ForgetChild was called for an element that is not its child.");
        }

        _child = null;
        base.ForgetChild(child);
    }

    /// <inheritdoc />
    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        if (slot is not null)
        {
            throw new AssertionError("A View expects a null slot for its child.");
        }

        RenderViewObject.Child = (RenderBox)child;
    }

    /// <inheritdoc />
    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        throw new AssertionError("A View's child cannot be moved.");
    }

    /// <inheritdoc />
    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        if (slot is not null)
        {
            throw new AssertionError("A View expects a null slot for its child.");
        }

        if (!ReferenceEquals(RenderViewObject.Child, child))
        {
            throw new AssertionError(
                "RemoveRenderObjectChild was called for a render object that is not the view's child.");
        }

        RenderViewObject.Child = null;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dart's <c>_RawViewElement.unmount</c>. Plumix additionally detaches the view and clears the
    /// pipeline root here, because the host may still be holding the owner this element created.
    /// </remarks>
    public override void Unmount()
    {
        if (_parentPipelineOwner is not null)
        {
            DetachView();
        }

        if (ReferenceEquals(EffectivePipelineOwner.RootNode, RenderViewObject))
        {
            EffectivePipelineOwner.RootNode = null;
        }

        if (!ReferenceEquals(EffectivePipelineOwner, TypedWidget.DeprecatedPipelineOwner))
        {
            EffectivePipelineOwner.Dispose();
        }

        base.Unmount();
    }
}

/// <summary>
/// Flutter's <c>_ViewScope</c>: publishes the <see cref="FlutterView"/> to <see cref="View.MaybeOf"/>.
/// </summary>
internal sealed class ViewScope : InheritedWidget
{
    public ViewScope(FlutterView view, Widget child) : base(child)
    {
        View = view;
    }

    public FlutterView View { get; }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(View, ((ViewScope)oldWidget).View);
    }
}

/// <summary>
/// Flutter's <c>_PipelineOwnerScope</c>: publishes the owner to <see cref="View.PipelineOwnerOf"/>.
/// </summary>
internal sealed class PipelineOwnerScope : InheritedWidget
{
    public PipelineOwnerScope(PipelineOwner pipelineOwner, Widget child) : base(child)
    {
        PipelineOwner = pipelineOwner;
    }

    public PipelineOwner PipelineOwner { get; }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(PipelineOwner, ((PipelineOwnerScope)oldWidget).PipelineOwner);
    }
}

/// <summary>
/// A widget that manages a set of child widgets with a stateless configuration: an optional
/// <c>child</c> in the regular tree plus any number of <c>views</c>, each the root of an
/// independent render tree.
/// </summary>
/// <remarks>
/// Flutter's <c>_MultiChildComponentWidget</c>. Plumix exposes the type so <see cref="ViewCollection"/>
/// can derive from it; only framework code can construct it.
/// </remarks>
public class MultiChildComponentWidget : Widget
{
    internal MultiChildComponentWidget(IReadOnlyList<Widget>? views = null, Widget? child = null, Key? key = null)
        : base(key)
    {
        ViewWidgets = views ?? [];
        ChildWidget = child;
    }

    internal IReadOnlyList<Widget> ViewWidgets { get; }

    internal Widget? ChildWidget { get; }

    /// <inheritdoc />
    public override Element CreateElement() => new MultiChildComponentElement(this);
}

/// <summary>
/// A collection of sibling <see cref="View"/>s, for the root of a widget tree that renders into
/// several views.
/// </summary>
/// <remarks>
/// Flutter's <c>ViewCollection</c>. Every entry must be a <see cref="View"/> or another collection.
/// </remarks>
public sealed class ViewCollection : MultiChildComponentWidget
{
    /// <summary>Creates a collection of <paramref name="views"/>.</summary>
    public ViewCollection(IReadOnlyList<Widget> views, Key? key = null) : base(views: views, key: key)
    {
        ArgumentNullException.ThrowIfNull(views);
    }

    /// <summary>The views to render.</summary>
    public IReadOnlyList<Widget> Views => ViewWidgets;
}

/// <summary>
/// Decorates a <see cref="Child"/> widget with a side <see cref="ViewWidget"/>: the child stays
/// in the surrounding render tree, the view becomes the root of an independent one.
/// </summary>
/// <remarks>Flutter's <c>ViewAnchor</c>. The view sits behind a <see cref="LookupBoundary"/>.</remarks>
public sealed class ViewAnchor : StatelessWidget
{
    /// <summary>Creates a view anchor.</summary>
    public ViewAnchor(Widget child, Widget? view = null, Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(child);
        Child = child;
        ViewWidget = view;
    }

    /// <summary>The <see cref="View"/> (or collection) rendered into an independent render tree.</summary>
    /// <remarks>Dart's <c>ViewAnchor.view</c>.</remarks>
    public Widget? ViewWidget { get; }

    /// <summary>The widget below this widget in the regular tree.</summary>
    public Widget Child { get; }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        List<Widget> views = ViewWidget is null ? [] : [new LookupBoundary(child: ViewWidget)];
        return new MultiChildComponentWidget(views: views, child: Child);
    }
}

/// <summary>Flutter's <c>_MultiChildComponentElement</c>.</summary>
internal sealed class MultiChildComponentElement : Element
{
    private static readonly object ViewSlot = new();

    private List<Element> _viewElements = [];
    private readonly HashSet<Element> _forgottenViewElements = [];
    private Element? _childElement;

    public MultiChildComponentElement(MultiChildComponentWidget widget) : base(widget)
    {
    }

    private MultiChildComponentWidget TypedWidget => (MultiChildComponentWidget)Widget;

    private void DebugAssertChildren()
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (_viewElements.Count != TypedWidget.ViewWidgets.Count)
        {
            throw new AssertionError("The view elements are out of step with the view widgets.");
        }

        if ((_childElement is null) != (TypedWidget.ChildWidget is null))
        {
            throw new AssertionError("The child element is out of step with the child widget.");
        }

        if (_childElement is not null && _viewElements.Contains(_childElement))
        {
            throw new AssertionError("The child element must not also be a view element.");
        }
    }

    /// <inheritdoc />
    public override void AttachRenderObject(object? newSlot)
    {
        base.AttachRenderObject(newSlot);
        DebugCheckMustAttachRenderObject(newSlot);
    }

    /// <inheritdoc />
    protected override void OnMount()
    {
        base.OnMount();
        DebugCheckMustAttachRenderObject(Slot);
        if (_viewElements.Count > 0 || _childElement is not null)
        {
            throw new AssertionError("A MultiChildComponentElement must have no children when it mounts.");
        }

        Rebuild();
        DebugAssertChildren();
    }

    /// <inheritdoc />
    public override void UpdateSlot(object? newSlot)
    {
        base.UpdateSlot(newSlot);
        DebugCheckMustAttachRenderObject(newSlot);
    }

    private void DebugCheckMustAttachRenderObject(object? slot)
    {
        if (!Constants.KDebugMode || TypedWidget.ChildWidget is not null)
        {
            return;
        }

        // This element only has views (a ViewCollection), so it must not be attached to an ancestor
        // that wants a render object in this slot.
        bool hasAncestorRenderObjectElement = false;
        bool ancestorWantsRenderObject = true;
        VisitAncestorElements(ancestor =>
        {
            if (!ancestor.DebugExpectsRenderObjectForSlot(slot))
            {
                ancestorWantsRenderObject = false;
                return false;
            }

            if (ancestor is IRenderObjectHost)
            {
                hasAncestorRenderObjectElement = true;
                return false;
            }

            return true;
        });

        if (hasAncestorRenderObjectElement && ancestorWantsRenderObject)
        {
            FlutterError.ReportError(new FlutterErrorDetails(
                new FlutterError(
                [
                    new ErrorSummary(
                        $"The Element for {ToStringShort()} cannot be inserted into slot \"{slot}\" "
                        + "of its ancestor. "),
                    new ErrorDescription(
                        "The ownership chain for the Element in question was:\n  " + DebugGetCreatorChain(10)),
                    new ErrorDescription(
                        "This Element allows the creation of multiple independent render trees, which cannot "
                        + "be attached to an ancestor in an existing render tree. However, an ancestor "
                        + "RenderObject is expecting that a child will be attached."),
                    new ErrorHint(
                        $"Try moving the subtree that contains the {ToStringShort()} widget into the view "
                        + "property of a ViewAnchor widget or to the root of the widget tree, where it is not "
                        + "expected to attach its RenderObject to its ancestor."),
                ]),
                library: "widgets library"));
        }
    }

    /// <inheritdoc />
    public override void Update(Widget newWidget)
    {
        bool newHasChild = ((MultiChildComponentWidget)newWidget).ChildWidget is not null;
        if (newHasChild != (TypedWidget.ChildWidget is not null))
        {
            throw new AssertionError("A MultiChildComponentWidget cannot gain or lose its child on update.");
        }

        base.Update(newWidget);
        Rebuild(force: true);
        DebugAssertChildren();
    }

    /// <inheritdoc />
    public override bool DebugExpectsRenderObjectForSlot(object? slot) => !ReferenceEquals(slot, ViewSlot);

    /// <inheritdoc />
    protected override void PerformRebuild()
    {
        _childElement = UpdateChild(_childElement, TypedWidget.ChildWidget, Slot);
        IReadOnlyList<Widget> views = TypedWidget.ViewWidgets;
        _viewElements = UpdateChildren(
            _viewElements,
            views,
            forgottenChildren: _forgottenViewElements,
            slots: views.Select(static _ => ViewSlot).ToList());
        _forgottenViewElements.Clear();
        base.PerformRebuild();
        DebugAssertChildren();
    }

    /// <inheritdoc />
    public override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _childElement))
        {
            _childElement = null;
        }
        else
        {
            if (!_viewElements.Contains(child) || _forgottenViewElements.Contains(child))
            {
                throw new AssertionError("ForgetChild was called for an element that is not a view child.");
            }

            _forgottenViewElements.Add(child);
        }

        base.ForgetChild(child);
    }

    /// <inheritdoc />
    public override void VisitChildren(Action<Element> visitor)
    {
        if (_childElement is not null)
        {
            visitor(_childElement);
        }

        foreach (Element child in _viewElements)
        {
            if (!_forgottenViewElements.Contains(child))
            {
                visitor(child);
            }
        }
    }

    /// <inheritdoc />
    public override Element? RenderObjectAttachingChild => _childElement;

    /// <inheritdoc />
    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren()
    {
        var children = new List<DiagnosticsNode>();
        if (_childElement is not null)
        {
            children.Add(_childElement.ToDiagnosticsNode());
        }

        for (int i = 0; i < _viewElements.Count; i += 1)
        {
            children.Add(_viewElements[i].ToDiagnosticsNode(
                name: $"view {i + 1}",
                style: DiagnosticsTreeStyle.Offstage));
        }

        return children;
    }
}

/// <summary>
/// Flutter's <c>_DeprecatedRawViewKey</c>: a <see cref="GlobalKey"/> whose identity is the
/// (view, deprecated owner, deprecated render view) triple, so a <see cref="RawView"/> rebuilt for
/// the same view keeps its element and two widgets for one view collide.
/// </summary>
internal sealed record DeprecatedRawViewKey(FlutterView View, PipelineOwner? Owner, RenderView? RenderView)
    : GlobalKey<State>
{
    /// <inheritdoc />
    public override string ToString() => $"[_DeprecatedRawViewKey {Diagnostics.DescribeIdentity(View)}]";
}
