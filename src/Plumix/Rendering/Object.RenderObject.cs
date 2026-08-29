using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart

namespace Plumix.Rendering;

public interface IRenderObject
{
}

/// <summary>
/// An object in the render tree.
/// </summary>
public abstract partial class RenderObject : DiagnosticableTree, IRenderObject, IHitTestTarget
{
    private readonly LayerHandle<Layer> _layerHandle = new();
    internal Layer? _layer
    {
        get => _layerHandle.Layer;
        set => _layerHandle.Layer = value;
    }

    private readonly RenderObjectSemantics _semantics;
    private bool _needsCompositingBitsUpdate;
    private bool _needsCompositedLayerUpdate;
    private bool _needsCompositingStorage;
    private bool _wasRepaintBoundaryStorage;
    private bool _didInitializeCompositing;
    private bool _debugDisposed;
    private bool _debugMutationsLocked;
    private bool _debugDoingThisPaint;

    [ThreadStatic]
    private static RenderObject? _debugActiveLayout;

    [ThreadStatic]
    private static RenderObject? _debugActivePaint;

    /// <summary>
    /// Dart's <c>RenderObject</c> constructor runs <c>_needsCompositing = isRepaintBoundary ||
    /// alwaysNeedsCompositing</c> and <c>_wasRepaintBoundary = isRepaintBoundary</c> after every
    /// subclass initializer list has run. A C# derived constructor body runs *after* the base one, so
    /// the two virtual reads are deferred to their first use instead of being done eagerly.
    /// </summary>
    private void EnsureCompositingInitialized()
    {
        if (_didInitializeCompositing)
        {
            return;
        }

        _didInitializeCompositing = true;
        _needsCompositingStorage = IsRepaintBoundary || AlwaysNeedsCompositing;
        _wasRepaintBoundaryStorage = IsRepaintBoundary;
    }

    internal bool _wasRepaintBoundary
    {
        get
        {
            EnsureCompositingInitialized();
            return _wasRepaintBoundaryStorage;
        }

        set
        {
            EnsureCompositingInitialized();
            _wasRepaintBoundaryStorage = value;
        }
    }

    internal RenderObjectSemantics Semantics => _semantics;

    /// <summary>Whether this render object has been disposed.</summary>
    /// <remarks>Flutter's <c>RenderObject.debugDisposed</c>.</remarks>
    public bool DebugDisposed => _debugDisposed;

    /// <summary>The render object currently computing layout, if any.</summary>
    /// <remarks>Flutter's <c>RenderObject.debugActiveLayout</c>.</remarks>
    public static RenderObject? DebugActiveLayout => _debugActiveLayout;

    /// <summary>The render object that is actively painting, if any.</summary>
    /// <remarks>Flutter's <c>RenderObject.debugActivePaint</c>.</remarks>
    public static RenderObject? DebugActivePaint => _debugActivePaint;

    /// <summary>Whether <see cref="Paint"/> for this render object is currently running.</summary>
    /// <remarks>Flutter's <c>RenderObject.debugDoingThisPaint</c>.</remarks>
    public bool DebugDoingThisPaint => _debugDoingThisPaint;

    /// <summary>The retained compositing layer, exposed for diagnostics and tests.</summary>
    /// <remarks>Flutter's <c>RenderObject.debugLayer</c>.</remarks>
    public Layer? DebugLayer => _layerHandle.Layer;

    /// <summary>The semantics node this render object produced, or <c>null</c> when it merged up.</summary>
    /// <remarks>Flutter's <c>RenderObject.debugSemantics</c>.</remarks>
    public SemanticsNode? SemanticsNode => _semantics.Built ? _semantics.CachedSemanticsNode : null;

    public int? SemanticsNodeId => SemanticsNode?.Id;

    /// <summary>Sends an event from this object's unmerged semantics node or its first such ancestor.</summary>
    /// <remarks>Flutter's <c>RenderObject.sendSemanticsEvent</c>.</remarks>
    public void SendSemanticsEvent(SemanticsEvent semanticsEvent)
    {
        ArgumentNullException.ThrowIfNull(semanticsEvent);
        if (Owner?.HasSemanticsOwner != true)
        {
            return;
        }

        SemanticsNode? node = _semantics.CachedSemanticsNode;
        if (node is not null && !node.IsMergedIntoParent)
        {
            SemanticsService.SendEvent(semanticsEvent with { NodeId = node.Id });
        }
        else
        {
            Parent?.SendSemanticsEvent(semanticsEvent);
        }
    }

    protected RenderObject()
    {
        _semantics = new RenderObjectSemantics(this);
    }


    /// Cause the entire subtree rooted at the given [RenderObject] to be marked
    /// dirty for layout, paint, etc, so that the effects of a hot reload can be
    /// seen, or so that the effect of changing a global debug flag (such as
    /// [debugPaintSizeEnabled]) can be applied.
    ///
    /// This is called by the [RendererBinding] in response to the
    /// `ext.flutter.reassemble` hook, which is used by development tools when the
    /// application code has changed, to cause the widget tree to pick up any
    /// changed implementations.
    ///
    /// This is expensive and should not be called except during development.
    ///
    /// See also:
    ///
    ///  * [BindingBase.reassembleApplication]
    public void Reassemble()
    {
        EnsureNotDisposedMutation();
        MarkNeedsLayout();
        MarkNeedsCompositingBitsUpdate();
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();

        VisitChildren(child => child.Reassemble());
    }

    // LAYOUT

    /// Data for use by the parent render object.
    ///
    /// The parent data is used by the render object that lays out this object
    /// (typically this object's parent in the render tree) to store information
    /// relevant to itself and to any other nodes who happen to know exactly what
    /// the data means. The parent data is opaque to the child.
    ///
    ///  * The parent data field must not be directly set, except by calling
    ///    [setupParentData] on the parent node.
    ///  * The parent data can be set before the child is added to the parent, by
    ///    calling [setupParentData] on the future parent node.
    ///  * The conventions for using the parent data depend on the layout protocol
    ///    used between the parent and child. For example, in box layout, the
    ///    parent data is completely opaque but in sector layout the child is
    ///    permitted to read some fields of the parent data.
    internal IParentData? parentData;


    /// Override to setup parent data correctly for your children.
    ///
    /// You can call this function to set up the parent data for child before the
    /// child is added to the parent's child list.
    public virtual void SetupParentData(RenderObject child)
    {
        DebugAssertCanPerformMutations();

        if (child.parentData is null)
        {
            child.parentData = new ParentData();
        }
    }

    /// The depth of this render object in the render tree.
    ///
    /// The depth of nodes in a tree monotonically increases as you traverse down
    /// the tree: a node always has a [depth] greater than its ancestors.
    /// There's no guarantee regarding depth between siblings.
    ///
    /// The [depth] of a child can be more than one greater than the [depth] of
    /// the parent, because the [depth] values are never decreased: all that
    /// matters is that it's greater than the parent. Consider a tree with a root
    /// node A, a child B, and a grandchild C. Initially, A will have [depth] 0,
    /// B [depth] 1, and C [depth] 2. If C is moved to be a child of A,
    /// sibling of B, then the numbers won't change. C's [depth] will still be 2.
    ///
    /// The depth of a node is used to ensure that nodes are processed in
    /// depth order.  The [depth] is automatically maintained by the [adoptChild]
    /// and [dropChild] methods.
    public int Depth { get; private set; }

    /// Adjust the [depth] of the given [child] to be greater than this node's own
    /// [depth].
    ///
    /// Only call this method from overrides of [redepthChildren].
    protected void RedepthChild(RenderObject child)
    {
        Debug.Assert(ReferenceEquals(child.Owner, Owner));
        if (child.Depth <= Depth)
        {
            child.Depth = Depth + 1;
            child.RedepthChildren();
        }
    }

    /// Adjust the [depth] of this node's children, if any.
    ///
    /// Do not call this method directly.
    ///
    /// Dart spells this out per child-holding mixin (`RenderObjectWithChildMixin.redepthChildren`
    /// visits `child`, `ContainerRenderObjectMixin.redepthChildren` walks the sibling chain). C# has
    /// no mixins, so the walk lives here and goes through <see cref="VisitChildren"/>, which every
    /// child-holding render object already implements.
    protected virtual void RedepthChildren()
    {
        VisitChildren(RedepthChild);
    }


    /// <summary>
    /// The parent of this render object in the render tree.
    /// </summary>
    public RenderObject? Parent { get; private set; }

    /// <summary>
    /// Called by subclasses when they decide a render object is a child.
    /// </summary>
    public void AdoptChild(RenderObject child)
    {
        EnsureNotDisposedMutation();
        Debug.Assert(child.Parent is null);
        if (Constants.KDebugMode)
        {
            for (RenderObject? node = this; node is not null; node = node.Parent)
            {
                // Indicates we are about to create a cycle.
                Debug.Assert(!ReferenceEquals(node, child));
            }
        }

        SetupParentData(child);
        MarkNeedsLayout();
        MarkNeedsCompositingBitsUpdate();
        MarkNeedsSemanticsUpdate();
        child.Parent = this;

        if (Attached)
        {
            child.Attach(Owner!);
        }

        RedepthChild(child);
    }

    public void DropChild(RenderObject child)
    {
        EnsureNotDisposedMutation();
        Debug.Assert(ReferenceEquals(child.Parent, this));
        Debug.Assert(child.Attached == Attached);
        Debug.Assert(child.parentData is not null);
        if (!ReferenceEquals(child.Parent, this))
        {
            return;
        }

        // A child that was not its own relayout boundary has to forget the boundary state it
        // inherited from this parent, so a later adoption re-derives it from scratch.
        if (child._isRelayoutBoundary == false)
        {
            child._isRelayoutBoundary = null;
        }

        child.parentData?.Detach();
        child.parentData = null;
        child.Parent = null;

        if (Attached && child.Attached)
        {
            child.Detach();
        }

        MarkNeedsLayout();
        MarkNeedsCompositingBitsUpdate();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>
    /// The owner for this render object (null if unattached).
    /// </summary>
    public PipelineOwner? Owner { get; internal set; }

    /// <summary>
    /// Whether the render tree this render object belongs to is attached to a [PipelineOwner].
    /// </summary>
    public bool Attached => Owner != null;

    /// <summary>
    /// Mark this render object as attached to the given owner.
    /// </summary>
    public void Attach(PipelineOwner owner)
    {
        if (_debugDisposed)
        {
            throw new AssertionError("A disposed RenderObject cannot be attached.");
        }

        if (Owner is not null)
        {
            throw new AssertionError("An attached RenderObject cannot be attached again.");
        }

        Owner = owner;

        // If the node was dirtied in some way while unattached, make sure to add it to the
        // appropriate dirty list now that an owner is available.
        if (_needsLayout && _isRelayoutBoundary is not null)
        {
            // Don't enter this block if we've never laid out at all; ScheduleInitialLayout handles it.
            _needsLayout = false;
            MarkNeedsLayout();
        }

        if (_needsCompositingBitsUpdate)
        {
            _needsCompositingBitsUpdate = false;
            MarkNeedsCompositingBitsUpdate();
        }

        if (_needsPaint && _layerHandle.Layer is not null)
        {
            // Don't enter this block if we've never painted at all.
            _needsPaint = false;
            MarkNeedsPaint();
        }

        if (_semantics.ConfigProvider.Effective.IsSemanticBoundary
            && (_semantics.ParentDataDirty || !_semantics.Built))
        {
            MarkNeedsSemanticsUpdate();
        }

        OnAttach();

        // Dart recurses from each child-holding mixin's `attach` override. C# centralizes the same
        // parent-first walk through `VisitChildren`, but preserves Dart's strict attachment assertions.
        VisitChildren(child => child.Attach(owner));
    }

    /// <summary>
    /// Mark this render object as detached from its [PipelineOwner].
    /// </summary>
    public void Detach()
    {
        if (Owner == null)
        {
            throw new AssertionError("A detached RenderObject cannot be detached again.");
        }

        OnDetach();
        ClearOwnSemantics();
        Owner = null;
        Debug.Assert(Parent is null || Attached == Parent.Attached);

        // The mirror of the recursion in `Attach`; Dart spells it out per child-holding mixin.
        VisitChildren(static child => child.Detach());
    }

    /// <summary>Releases resources owned by this render object.</summary>
    /// <remarks>Overrides must call <c>base.Dispose()</c> last.</remarks>
    public virtual void Dispose()
    {
        if (_debugDisposed)
        {
            throw new AssertionError("RenderObject.Dispose() called more than once.");
        }

        _layerHandle.Layer = null;
        _debugDisposed = true;
        _debugCanParentUseSize = null;
    }

    protected virtual void OnDetach()
    {
    }

    protected virtual void OnAttach()
    {
    }

    private bool _needsLayout = true;

    /// <summary>
    /// Whether this [RenderObject] is a known relayout boundary.
    /// </summary>
    private bool? _isRelayoutBoundary;

    /// Whether [invokeLayoutCallback] for this render object is currently running.
    public bool DebugDoingThisLayoutWithCallback { get; private set; } = false;

    /// <summary>Whether <see cref="PerformResize"/> for this render object is currently running.</summary>
    /// <remarks>Flutter's <c>RenderObject.debugDoingThisResize</c>.</remarks>
    public bool DebugDoingThisResize { get; private set; }

    /// <summary>Whether <see cref="PerformLayout"/> for this render object is currently running.</summary>
    /// <remarks>Flutter's <c>RenderObject.debugDoingThisLayout</c>.</remarks>
    public bool DebugDoingThisLayout { get; private set; }

    private IConstraints? _constraints;

    protected virtual IConstraints Constraints
    {
        get
        {
            if (_constraints == null)
            {
                throw new InvalidOperationException(
                    "A RenderObject does not have any constraints before it has been laid out.");
            }

            return _constraints!;
        }
    }


    /// <summary>
    /// Compute the layout for this render object.
    /// </summary>
    public virtual void Layout(BoxConstraints constraints, bool parentUsesSize = false)
    {
        EnsureNotDisposedMutation();
        Debug.Assert(!DebugDoingThisResize);
        Debug.Assert(!DebugDoingThisLayout);
        Debug.Assert(!_debugMutationsLocked);
        Debug.Assert(!DebugDoingThisLayoutWithCallback);
        constraints.DebugAssertIsValid(isAppliedConstraint: true);

        // Dart recomputes the boundary flag before the early-out, so that a repeat layout with the
        // same constraints but a different `parentUsesSize` still lands on the right boundary.
        _isRelayoutBoundary = !parentUsesSize || SizedByParent || constraints.IsTight || Parent == null;
        _debugCanParentUseSize = parentUsesSize;

        if (!_needsLayout
            && _constraints is BoxConstraints previousConstraints
            && previousConstraints.Equals(constraints))
        {
            if (Constants.KDebugMode)
            {
                DebugDoingThisResize = SizedByParent;
                DebugDoingThisLayout = !SizedByParent;
                RenderObject? debugSkippedActiveLayout = _debugActiveLayout;
                _debugActiveLayout = this;
                DebugResetSize();
                _debugActiveLayout = debugSkippedActiveLayout;
                DebugDoingThisLayout = false;
                DebugDoingThisResize = false;
            }

            return;
        }

        _constraints = constraints;
        _debugMutationsLocked = true;

        if (SizedByParent)
        {
            DebugDoingThisResize = true;
            try
            {
                PerformResize();
                DebugAssertDoesMeetConstraints();
            }
            catch (Exception exception)
            {
                ReportException("performResize", exception);
            }
            finally
            {
                DebugDoingThisResize = false;
            }
        }

        RenderObject? previousActiveLayout = _debugActiveLayout;
        _debugActiveLayout = this;
        DebugDoingThisLayout = true;
        try
        {
            PerformLayout();
            MarkNeedsSemanticsUpdate();
            DebugAssertDoesMeetConstraints();
        }
        catch (Exception exception)
        {
            ReportException("performLayout", exception);
        }
        finally
        {
            DebugDoingThisLayout = false;
            _debugActiveLayout = previousActiveLayout;
            _debugMutationsLocked = false;
        }

        _needsLayout = false;
        MarkNeedsPaint();
    }

    /// <summary>
    /// Relayouts this render object under the constraints it was last given, without going through
    /// the resize step.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderObject._layoutWithoutResize</c>. This is how a <see cref="PipelineOwner"/>
    /// drains its dirty list: the node is already sized, so only <see cref="PerformLayout"/> re-runs.
    /// </remarks>
    internal void LayoutWithoutResize()
    {
        Debug.Assert(_needsLayout);
        Debug.Assert(_isRelayoutBoundary == true || this is IRenderObjectWithLayoutCallback);
        Debug.Assert(!_debugMutationsLocked);
        Debug.Assert(!DebugDoingThisLayoutWithCallback);
        Debug.Assert(_debugCanParentUseSize is not null);

        _debugMutationsLocked = true;
        DebugDoingThisLayout = true;
        RenderObject? previousActiveLayout = _debugActiveLayout;
        _debugActiveLayout = this;
        try
        {
            PerformLayout();
            MarkNeedsSemanticsUpdate();
        }
        catch (Exception exception)
        {
            ReportException("performLayout", exception);
        }
        finally
        {
            _debugActiveLayout = previousActiveLayout;
            DebugDoingThisLayout = false;
            _debugMutationsLocked = false;
        }

        _needsLayout = false;
        MarkNeedsPaint();
    }

    /// <summary>Bootstraps layout for the root of a render tree.</summary>
    /// <remarks>Flutter's <c>RenderObject.scheduleInitialLayout</c>.</remarks>
    public void ScheduleInitialLayout()
    {
        EnsureNotDisposedMutation();
        Debug.Assert(Attached);
        Debug.Assert(Parent is null);
        Debug.Assert(!Owner!.DebugDoingLayout);
        Debug.Assert(_isRelayoutBoundary is null);

        _isRelayoutBoundary = true;
        _debugCanParentUseSize = false;
        Owner.RequestLayoutFor(this);
    }

    /// <summary>Hook for subclasses that cache their size, run when a layout pass is skipped.</summary>
    /// <remarks>Flutter's <c>RenderObject.debugResetSize</c>; a no-op by default.</remarks>
    protected virtual void DebugResetSize()
    {
    }

    /// <summary>
    /// Verifies that this render object's geometry satisfies the constraints it was laid out under.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderObject.debugAssertDoesMeetConstraints</c>, called after
    /// <see cref="PerformResize"/> and <see cref="PerformLayout"/>. Implemented by the layout
    /// protocols (<see cref="RenderBox"/>, <see cref="RenderSliver"/>), not by this class.
    /// </remarks>
    protected virtual void DebugAssertDoesMeetConstraints()
    {
    }

    /// <summary>
    /// Whether the constraints are the only input to the sizing algorithm (in particular, child
    /// nodes have no impact).
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderObject.sizedByParent</c>. Returning <c>false</c> is always correct, but
    /// returning <c>true</c> is more efficient because the size does not have to be recomputed when
    /// the constraints do not change. Subclasses that return <c>true</c> must not change their
    /// dimensions in <see cref="PerformLayout"/>; that work belongs in <see cref="PerformResize"/>
    /// or — for <see cref="RenderBox"/> subclasses — in <c>ComputeDryLayout</c>. When the value can
    /// change, the subclass must call <see cref="MarkNeedsLayoutForSizedByParentChange"/>.
    /// </remarks>
    protected virtual bool SizedByParent => false;

    /// <summary>
    /// Updates the render object's size using only the constraints.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderObject.performResize</c>. Called by <see cref="Layout"/> only when
    /// <see cref="SizedByParent"/> is <c>true</c>. Subclasses of <see cref="RenderBox"/> should
    /// override <c>ComputeDryLayout</c> instead of this method.
    /// </remarks>
    protected virtual void PerformResize()
    {
    }

    /// <summary>
    /// Do the work of computing the layout for this render object.
    /// </summary>
    protected virtual void PerformLayout()
    {
    }

    /// <summary>
    /// Invokes a callback that is allowed to mutate this render object's child tree during layout.
    /// </summary>
    protected void InvokeLayoutCallback<TConstraints>(
        Action<TConstraints> callback,
        TConstraints constraints)
        where TConstraints : IConstraints
    {
        ArgumentNullException.ThrowIfNull(callback);
        Debug.Assert(_debugMutationsLocked);
        Debug.Assert(DebugDoingThisLayout);
        Debug.Assert(!DebugDoingThisLayoutWithCallback);

        _debugMutationsLocked = false;
        DebugDoingThisLayoutWithCallback = true;
        try
        {
            Owner?.EnableMutationsToDirtySubtrees(() => callback(constraints));
            if (Owner is null)
            {
                callback(constraints);
            }
        }
        finally
        {
            DebugDoingThisLayoutWithCallback = false;
            _debugMutationsLocked = true;
        }
    }

    /// <summary>
    /// Whether the layout callback of this <see cref="IRenderObjectWithLayoutCallback"/> has to run the
    /// next time this render object is laid out.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderObjectWithLayoutCallbackMixin._needsRebuild</c>. The initial value must be
    /// <see langword="true"/> so the callback is not scheduled before the subtree has ever been laid
    /// out, when the constraints are still unknown.
    /// </remarks>
    private bool _needsLayoutCallbackRebuild = true;

    /// <summary>
    /// Invokes a callback that is allowed to mutate this render object's child tree during layout,
    /// without handing it the constraints.
    /// </summary>
    /// <remarks>
    /// Dart's <c>invokeLayoutCallback</c> is public on <c>RenderObject</c>; this overload is what
    /// <c>_RenderDeferredLayoutBox._doLayoutFrom</c> asks of its tree-walk parent.
    /// </remarks>
    internal void InvokeLayoutCallbackOnTreeWalkParent(Action callback)
    {
        InvokeLayoutCallback<IConstraints>(_ => callback(), Constraints);
    }

    /// <summary>Invokes <see cref="IRenderObjectWithLayoutCallback.LayoutCallback"/>.</summary>
    /// <remarks>
    /// Flutter's <c>RenderObjectWithLayoutCallbackMixin.runLayoutCallback</c>. Must be called from
    /// <see cref="PerformLayout"/>, as early as possible and before any layout work is done, so that no
    /// child render object is re-dirtied afterwards.
    /// </remarks>
    protected void RunLayoutCallback()
    {
        Debug.Assert(this is IRenderObjectWithLayoutCallback);
        Debug.Assert(DebugDoingThisLayout);
        InvokeLayoutCallback<IConstraints>(
            _ => ((IRenderObjectWithLayoutCallback)this).LayoutCallback(),
            Constraints);
        _needsLayoutCallbackRebuild = false;
    }

    /// <summary>
    /// Informs the framework that the layout callback has been updated and must run again when this
    /// render object is ready for layout, even when an ancestor chooses to skip laying out this subtree.
    /// </summary>
    /// <remarks>Flutter's <c>RenderObjectWithLayoutCallbackMixin.scheduleLayoutCallback</c>.</remarks>
    internal void ScheduleLayoutCallback()
    {
        Debug.Assert(this is IRenderObjectWithLayoutCallback);
        if (_needsLayoutCallbackRebuild)
        {
            Debug.Assert(NeedsLayout);
            return;
        }

        _needsLayoutCallbackRebuild = true;

        // Registering the node itself is what makes the callback run even when an ancestor declines to
        // lay this subtree out (an obstructed OverlayEntry with `maintainState: true`, for example), so
        // that widget-tree integrity - unique global keys above all - is maintained regardless.
        Owner?.RequestLayoutFor(this);

        // In an active tree the layout boundary still has to learn that this child's size may change.
        MarkNeedsLayoutFromScheduleCallback();
    }

    /// <summary>The <see cref="MarkNeedsLayout"/> call made by <see cref="ScheduleLayoutCallback"/>.</summary>
    /// <remarks>
    /// Dart's <c>super.markNeedsLayout()</c> inside <c>scheduleLayoutCallback</c> resolves to the class
    /// the mixin is applied on, so a subclass override of <c>markNeedsLayout</c> - Flutter's
    /// <c>_RenderDeferredLayoutBox</c> has one - is deliberately bypassed. C# has no <c>super</c>, so the
    /// bypass is a hook such a subclass overrides with its own <c>base.MarkNeedsLayout()</c>.
    /// </remarks>
    private protected virtual void MarkNeedsLayoutFromScheduleCallback() => MarkNeedsLayout();

    public virtual void VisitChildren(Action<RenderObject> visitor)
    {
    }

    /// <summary>
    /// Mark this render object's layout information as dirty, and either register
    /// this object with its [PipelineOwner], or defer to the parent, depending on
    /// whether this object is a relayout boundary or not respectively.
    /// </summary>
    public virtual void MarkNeedsLayout()
    {
        DebugAssertCanPerformMutations();
        if (_needsLayout)
        {
            Debug.Assert(DebugRelayoutBoundaryAlreadyMarkedNeedsLayout());
            return;
        }

        _needsLayout = true;

        if (Owner is { } owner && _isRelayoutBoundary == true)
        {
            owner.RequestLayoutFor(this);
            owner.RequestVisualUpdate();
        }
        else if (Parent != null)
        {
            MarkParentNeedsLayout();
        }
    }

    /// <summary>
    /// Marks this render object as needing layout without dirtying its ancestors, for a caller that is
    /// about to lay it out immediately under different constraints.
    /// </summary>
    /// <remarks>
    /// Dart's <c>RenderObject.layout</c> compares the incoming <c>Constraints</c> object itself, so a
    /// sliver whose <c>SliverConstraints</c> changed always re-lays out. Plumix's <see cref="Layout"/>
    /// takes <see cref="BoxConstraints"/>, and two different <c>SliverConstraints</c> can derive the
    /// same box constraints, so <c>RenderSliver.LayoutWithSliverConstraints</c> has to defeat the
    /// early-out explicitly. It must not go through <see cref="MarkNeedsLayout"/>, because the viewport
    /// calls it from its own <c>PerformLayout</c> and Dart forbids a parent from dirtying a descendant
    /// there.
    /// </remarks>
    internal void MarkNeedsImmediateRelayout()
    {
        _needsLayout = true;
        InvalidateLayoutCache();
    }

    /// <summary>Drops any cached layout results; overridden by <see cref="RenderBox"/>.</summary>
    private protected virtual void InvalidateLayoutCache()
    {
    }

    /// <remarks>Flutter's <c>RenderObject._debugRelayoutBoundaryAlreadyMarkedNeedsLayout</c>.</remarks>
    private bool DebugRelayoutBoundaryAlreadyMarkedNeedsLayout()
    {
        for (RenderObject? node = this; node is not null && node._isRelayoutBoundary is not null;
             node = node.Parent)
        {
            if (!node._needsLayout && !node.DebugDoingThisLayout)
            {
                return false;
            }

            if (node._isRelayoutBoundary == true)
            {
                return true;
            }
        }

        return true;
    }

    /// <summary>
    /// Marks this render object's layout information as dirty (like <see cref="MarkNeedsLayout"/>)
    /// and additionally handles the work needed when <see cref="SizedByParent"/> changed value.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderObject.markNeedsLayoutForSizedByParentChange</c>, whose documented
    /// precondition is a non-null <see cref="Parent"/> (it asserts on one). A parentless render
    /// object is already its own relayout boundary, so there is nothing left to propagate and
    /// <see cref="MarkNeedsLayout"/> alone is sufficient.
    /// </remarks>
    public void MarkNeedsLayoutForSizedByParentChange()
    {
        MarkNeedsLayout();
        if (Parent == null)
        {
            return;
        }

        MarkParentNeedsLayout();
    }

    protected void MarkParentNeedsLayout()
    {
        DebugAssertCanPerformMutations();
        _needsLayout = true;
        Debug.Assert(Parent is not null);
        RenderObject parent = Parent!;
        if (!DebugDoingThisLayoutWithCallback)
        {
            parent.MarkNeedsLayout();
        }
        else
        {
            Debug.Assert(parent.DebugDoingThisLayout);
        }

        Debug.Assert(ReferenceEquals(parent, Parent));
    }

    public void MarkNeedsCompositingBitsUpdate()
    {
        EnsureNotDisposedMutation();
        if (_needsCompositingBitsUpdate)
        {
            return;
        }

        _needsCompositingBitsUpdate = true;
        if (Parent != null)
        {
            if (Parent._needsCompositingBitsUpdate)
            {
                return;
            }

            if ((!_wasRepaintBoundary || !IsRepaintBoundary) && !Parent.IsRepaintBoundary)
            {
                Parent.MarkNeedsCompositingBitsUpdate();
                return;
            }
        }

        Owner?.RequestCompositingBitsUpdateFor(this);
    }

    public void MarkNeedsSemanticsUpdate()
    {
        EnsureNotDisposedMutation();
        Debug.Assert(!Attached || Owner?.DebugDoingSemantics != true);
        if (!Attached || Owner?.HasSemanticsOwner != true)
        {
            return;
        }

        _semantics.MarkNeedsUpdate();
    }

    internal void UpdateCompositingBits()
    {
        if (!_needsCompositingBitsUpdate)
        {
            return;
        }

        bool oldNeedsCompositing = NeedsCompositing;
        PerformUpdateCompositingBits();

        if (!IsRepaintBoundary && _wasRepaintBoundary)
        {
            _needsPaint = false;
            _needsCompositedLayerUpdate = false;
            Owner?.ForgetPaintFor(this);
            _needsCompositingBitsUpdate = false;
            MarkNeedsPaint();
            return;
        }

        _needsCompositingBitsUpdate = false;

        if (oldNeedsCompositing != NeedsCompositing)
        {
            MarkNeedsPaint();
        }
    }

    protected virtual void PerformUpdateCompositingBits()
    {
        bool needsCompositing = IsRepaintBoundary || AlwaysNeedsCompositing;

        VisitChildren(child =>
        {
            child.UpdateCompositingBits();
            if (child.NeedsCompositing)
            {
                needsCompositing = true;
            }
        });

        NeedsCompositing = needsCompositing;
    }

    protected virtual void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
    }

    internal void InvokeDescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        DescribeSemanticsConfiguration(configuration);
    }

    /// <summary>
    /// Assemble the <see cref="SemanticsNode"/> for this <see cref="RenderObject"/>.
    /// </summary>
    /// <remarks>
    /// If <see cref="DescribeSemanticsConfiguration"/> sets <see cref="SemanticsConfiguration.IsSemanticBoundary"/>
    /// to true, this method is called with the <paramref name="node"/> created for this render object, the
    /// <paramref name="config"/> to be applied to that node, and the <paramref name="children"/> nodes that
    /// descendants of this render object have generated. By default the method annotates the node with the
    /// configuration and adds the children to it. Subclasses can override it to add additional nodes to the tree;
    /// nodes instantiated here must be released in <see cref="ClearSemantics"/>.
    /// </remarks>
    protected virtual void AssembleSemanticsNode(
        SemanticsNode node,
        SemanticsConfiguration config,
        IReadOnlyList<SemanticsNode> children)
    {
        node.UpdateWith(config, children);
    }

    internal void InvokeAssembleSemanticsNode(
        SemanticsNode node,
        SemanticsConfiguration config,
        IReadOnlyList<SemanticsNode> children)
    {
        AssembleSemanticsNode(node, config, children);
    }

    /// <summary>
    /// Removes all semantics from this render object and its descendants.
    /// </summary>
    public void ClearSemantics()
    {
        ClearOwnSemantics();
        VisitChildren(static child => child.ClearSemantics());
    }

    /// <summary>Schedules the initial semantics pass for the root render object.</summary>
    /// <remarks>Flutter's <c>RenderObject.scheduleInitialSemantics</c>.</remarks>
    public void ScheduleInitialSemantics()
    {
        EnsureNotDisposedMutation();
        Debug.Assert(Attached);
        Debug.Assert(Parent is null);
        Debug.Assert(Owner?.DebugDoingSemantics != true);
        Owner?.RequestSemanticsUpdateFor(this);
        Owner?.RequestSemanticsGeometryUpdateFor(this);
        Owner?.RequestVisualUpdate();
    }

    /// <summary>
    /// Removes the semantics of this render object only, without walking the descendants.
    /// </summary>
    /// <remarks>
    /// This is the non-recursive half of <see cref="ClearSemantics"/>; <see cref="Detach"/> uses it because it
    /// already reaches every descendant, so the recursive form would re-walk the subtree once per level.
    /// Override this method if new <see cref="SemanticsNode"/>s are instantiated in an overridden
    /// <see cref="AssembleSemanticsNode"/>, to release those nodes.
    /// </remarks>
    protected virtual void ClearOwnSemantics()
    {
        _semantics.Clear();
    }

    protected virtual bool AlwaysNeedsCompositing => false;

    protected virtual Rect SemanticBounds => new Rect();

    internal Rect SemanticBoundsForSemantics => SemanticBounds;

    /// <summary>
    /// Visits the children that should be considered when compiling this render object's semantics,
    /// in paint order.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderObject.visitChildrenForSemantics</c>. The children's positions are read
    /// through <see cref="ApplyPaintTransform"/>, exactly as Flutter does, so an override only has to
    /// decide which children participate.
    /// </remarks>
    internal virtual void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        VisitChildren(visitor);
    }

    protected virtual Rect? DescribeSemanticsClip(RenderObject? child)
    {
        return null;
    }

    internal Rect? InvokeDescribeSemanticsClip(RenderObject? child)
    {
        return DescribeSemanticsClip(child);
    }

    protected virtual Rect? DescribeApproximatePaintClip(RenderObject? child)
    {
        return null;
    }

    internal Rect? InvokeDescribeApproximatePaintClip(RenderObject? child)
    {
        return DescribeApproximatePaintClip(child);
    }

    internal bool HasBoxConstraints => _constraints is BoxConstraints;
    internal BoxConstraints CurrentBoxConstraints => (BoxConstraints)_constraints!;
    internal bool NeedsLayout => _needsLayout;
    internal bool NeedsCompositingBitsUpdate => _needsCompositingBitsUpdate;
    internal bool SemanticsParentDataDirty => _semantics.ParentDataDirty;

    /// <remarks>Flutter's <c>RenderObject.debugNeedsLayout</c>: always <c>false</c> in release.</remarks>
    public bool DebugNeedsLayout => Constants.KDebugMode && _needsLayout;

    /// <remarks>Flutter's <c>RenderObject.debugNeedsPaint</c>: always <c>false</c> in release.</remarks>
    public bool DebugNeedsPaint => Constants.KDebugMode && _needsPaint;

    /// <remarks>Flutter's <c>RenderObject.debugNeedsCompositingBitsUpdate</c>.</remarks>
    public bool DebugNeedsCompositingBitsUpdate => Constants.KDebugMode && _needsCompositingBitsUpdate;

    /// <remarks>Flutter's <c>RenderObject.debugNeedsCompositedLayerUpdate</c>.</remarks>
    public bool DebugNeedsCompositedLayerUpdate => Constants.KDebugMode && _needsCompositedLayerUpdate;

    /// <remarks>Flutter's <c>RenderObject.debugNeedsSemanticsUpdate</c>.</remarks>
    public bool DebugNeedsSemanticsUpdate => !Constants.KReleaseMode && _semantics.ParentDataDirty;

    /// <summary>Whether the parent passed <c>parentUsesSize: true</c> to the last layout call.</summary>
    /// <remarks>Flutter's <c>RenderObject.debugCanParentUseSize</c>; throws before the first layout.</remarks>
    public bool DebugCanParentUseSize => _debugCanParentUseSize
        ?? throw new AssertionError("RenderObject.DebugCanParentUseSize read before the first layout.");

    /// <summary>The render object that lays this one out, when it is not the parent.</summary>
    /// <remarks>Flutter's <c>RenderObject.debugLayoutParent</c>.</remarks>
    protected virtual RenderObject? DebugLayoutParent => Constants.KDebugMode ? Parent : null;

    /// <summary>Whether this render object has ever been laid out.</summary>
    internal bool HasBeenLaidOut => _constraints is not null;

    /// <summary>Whether this render object is its own relayout boundary.</summary>
    internal bool IsRelayoutBoundary => _isRelayoutBoundary == true;

    /// <summary>Whether this render object has ever resolved its relayout-boundary state.</summary>
    /// <remarks>Flutter's <c>RenderObject._isRelayoutBoundary != null</c>.</remarks>
    internal bool HasRelayoutBoundaryState => _isRelayoutBoundary is not null;

    /// <summary>Whether this render object may resize without its parent relaying out.</summary>
    /// <remarks>Flutter's <c>RenderObject._isRelayoutBoundary</c>.</remarks>
    internal bool IsRelayoutBoundaryForSemantics => _isRelayoutBoundary ?? false;

    /// <summary>
    /// Whether this render object repaints separately from its parent.
    /// </summary>
    public virtual bool IsRepaintBoundary => false;


    private bool _needsPaint = true;
    internal bool NeedsPaint => _needsPaint;
    internal bool NeedsCompositedLayerUpdate => _needsCompositedLayerUpdate;
    internal bool NeedsCompositing
    {
        get
        {
            EnsureCompositingInitialized();
            return _needsCompositingStorage;
        }

        private set
        {
            EnsureCompositingInitialized();
            _needsCompositingStorage = value;
        }
    }

    public void MarkNeedsPaint()
    {
        EnsureNotDisposedMutation();
        Debug.Assert(Owner is null || !Owner.DebugDoingPaint);
        if (_needsPaint)
        {
            return;
        }

        _needsPaint = true;

        if (IsRepaintBoundary && _wasRepaintBoundary)
        {
            Debug.Assert(_layerHandle.Layer is OffsetLayer);
            if (Owner is { } owner)
            {
                owner.RequestPaintFor(this);
                owner.RequestVisualUpdate();
            }
        }
        else if (Parent != null)
        {
            Parent.MarkNeedsPaint();
        }
        else
        {
            // If we are the root of the render tree we aren't added to the dirty list: the root is
            // always told to paint regardless.
            Owner?.RequestVisualUpdate();
        }
    }

    public void MarkNeedsCompositedLayerUpdate()
    {
        EnsureNotDisposedMutation();
        Debug.Assert(Owner is null || !Owner.DebugDoingPaint);
        if (_needsCompositedLayerUpdate || _needsPaint)
        {
            return;
        }

        _needsCompositedLayerUpdate = true;

        if (IsRepaintBoundary && _wasRepaintBoundary)
        {
            // If we always have our own layer, we can just repaint ourselves without involving any
            // other nodes.
            Debug.Assert(_layerHandle.Layer is not null);
            if (Owner is { } owner)
            {
                owner.RequestPaintFor(this);
                owner.RequestVisualUpdate();
            }
        }
        else
        {
            MarkNeedsPaint();
        }
    }

    /// <summary>Debug hook invoked when a repaint boundary (or its parent) attempts to paint.</summary>
    /// <remarks>Flutter's <c>RenderObject.debugRegisterRepaintBoundaryPaint</c>; a no-op by default.</remarks>
    public virtual void DebugRegisterRepaintBoundaryPaint(bool includedParent = true, bool includedChild = false)
    {
    }

    protected virtual OffsetLayer CreateCompositedLayer(OffsetLayer? oldLayer)
    {
        return oldLayer ?? new OffsetLayer();
    }

    protected virtual void UpdateCompositedLayer(OffsetLayer layer)
    {
    }

    internal OffsetLayer EnsureCompositedLayer()
    {
        var oldLayer = _layer as OffsetLayer;
        var layer = CreateCompositedLayer(oldLayer);

        if (!ReferenceEquals(oldLayer, layer))
        {
            oldLayer?.Parent?.Remove(oldLayer);
            _layer = layer;
            _needsCompositedLayerUpdate = true;
        }

        return layer;
    }

    internal void UpdateCompositedLayerProperties()
    {
        if (!_needsCompositedLayerUpdate)
        {
            return;
        }

        if (IsRepaintBoundary && _layer is OffsetLayer layer)
        {
            UpdateCompositedLayer(layer);
        }

        _needsCompositedLayerUpdate = false;
    }

    /// <summary>
    /// Creates or reuses this repaint boundary's <see cref="OffsetLayer"/> and re-applies its
    /// properties, clearing the pending-update flag.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderObject.updateCompositedLayer(oldLayer:)</c>, which a repaint boundary runs on
    /// every repaint. C# splits the single Dart method into <see cref="CreateCompositedLayer"/> (which
    /// must reuse <c>oldLayer</c>) and <see cref="UpdateCompositedLayer"/> (which mutates it), because a
    /// C# override cannot both construct and configure through one covariant return.
    /// </remarks>
    internal OffsetLayer UpdateCompositedLayerForRepaint()
    {
        Debug.Assert(IsRepaintBoundary);
        OffsetLayer layer = EnsureCompositedLayer();
        UpdateCompositedLayer(layer);
        _needsCompositedLayerUpdate = false;
        return layer;
    }



    internal void HandleSkippedPaintingOnDetachedLayer()
    {
        Debug.Assert(Attached);
        Debug.Assert(IsRepaintBoundary);
        Debug.Assert(_needsPaint || _needsCompositedLayerUpdate);
        Debug.Assert(_layerHandle.Layer is not null);
        Debug.Assert(_layerHandle.Layer?.Attached != true);

        RenderObject? node = Parent;
        while (node != null)
        {
            if (node.IsRepaintBoundary)
            {
                if (node._layerHandle.Layer is null)
                {
                    // Looks like the subtree here has never been painted. Let it handle itself.
                    break;
                }

                if (node._layerHandle.Layer!.Attached)
                {
                    // It's the one that detached us, so it's the one that will decide to repaint us.
                    break;
                }

                node._needsPaint = true;
            }

            node = node.Parent;
        }
    }

    internal static Rect TransformRect(Matrix4 transform, Rect rect) =>
        MatrixUtils.TransformRect(transform, rect);

    internal static Rect? IntersectClip(Rect? inheritedClip, Rect? localClip, Matrix4 transform)
    {
        Rect? transformedLocalClip = null;
        if (localClip.HasValue)
        {
            transformedLocalClip = TransformRect(transform, localClip.Value);
        }

        if (!inheritedClip.HasValue)
        {
            return transformedLocalClip;
        }

        if (!transformedLocalClip.HasValue)
        {
            return inheritedClip;
        }

        var intersection = inheritedClip.Value.Intersect(transformedLocalClip.Value);
        return intersection.Width <= 0 || intersection.Height <= 0 ? null : intersection;
    }

    /// <summary>
    /// Applies the transform that would be applied when painting the given child to the given matrix.
    /// </summary>
    /// <remarks>
    /// The matrix is mutated in place and each render object post-multiplies its own step, exactly as
    /// in Flutter: <see cref="Matrix4"/> maps points as <c>M * p</c>, so the child's own step ends up
    /// rightmost and is therefore applied first.
    /// </remarks>
    public virtual void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        Debug.Assert(ReferenceEquals(child.Parent, this));
    }

    /// <summary>Whether this render object paints <paramref name="child"/> at all.</summary>
    /// <remarks>
    /// Flutter's <c>RenderObject.paintsChild</c>. A parent that zeroes the matrix in
    /// <see cref="ApplyPaintTransform"/> to signal "not painted" must return <c>false</c> here.
    /// </remarks>
    public virtual bool PaintsChild(RenderObject child)
    {
        Debug.Assert(ReferenceEquals(child.Parent, this));
        return true;
    }

    /// <summary>An estimate of the bounds within which this render object will paint.</summary>
    public virtual Rect PaintBounds => default;

    /// <summary>
    /// Attempts to make this render object (or <paramref name="descendant"/>, or
    /// <paramref name="rect"/> in this render object's coordinate system) visible on screen.
    /// </summary>
    /// <remarks>
    /// The default implementation forwards the request to the parent, substituting itself as the
    /// descendant when none was supplied, so a leaf can simply call
    /// <c>ShowOnScreen()</c> and every enclosing viewport gets a chance to scroll it into view.
    /// </remarks>
    public virtual void ShowOnScreen(
        RenderObject? descendant = null,
        Rect? rect = null,
        TimeSpan duration = default,
        Curve? curve = null)
    {
        Parent?.ShowOnScreen(
            descendant: descendant ?? this,
            rect: rect,
            duration: duration,
            curve: curve ?? Curves.Ease);
    }

    internal bool TryGetTransformFromRoot(out Matrix4 transform)
    {
        RenderObject? root = Owner?.Root;
        if (root is null)
        {
            transform = Matrix4.Identity();
            return false;
        }

        return TryComputeTransformTo(root, ancestorSpecified: false, out transform);
    }

    /// The paint offset of this render object's origin in the coordinate space of the render tree root.
    public Point GetPaintOffsetToRoot()
    {
        return MatrixUtils.TransformPoint(ComputePaintTransformToRoot(), default);
    }

    /// The paint transform from this render object to the topmost render object of its parent chain.
    ///
    /// Unlike <see cref="GetTransformTo"/> this walks the parent chain directly, so it also resolves for
    /// render objects that are not attached to a <see cref="PipelineOwner"/>.
    internal Matrix4 ComputePaintTransformToRoot()
    {
        Matrix4 transform = Matrix4.Identity();
        var renderers = new List<RenderObject>();
        for (RenderObject node = this; node.Parent is not null; node = node.Parent)
        {
            renderers.Add(node);
        }

        for (int index = renderers.Count - 1; index >= 0; index--)
        {
            renderers[index].Parent!.ApplyPaintTransform(renderers[index], transform);
        }

        return transform;
    }

    /// <summary>
    /// Applies the paint transform up the tree to <paramref name="target"/>.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderObject.getTransformTo</c>. <paramref name="target"/> does not have to be an
    /// ancestor: the two chains are walked up to their common ancestor and the target's half is
    /// inverted. Returns a zero matrix when the target half is singular, exactly as Dart does.
    /// </remarks>
    public Matrix4 GetTransformTo(RenderObject? target = null)
    {
        Debug.Assert(Attached);
        RenderObject from = this;
        RenderObject to = target
                          ?? Owner?.Root
                          ?? throw new InvalidOperationException(
                              "The render object is not attached to a render tree.");

        var fromPath = new List<RenderObject> { from };
        List<RenderObject>? toPath = null;

        while (!ReferenceEquals(from, to))
        {
            int fromDepth = from.Depth;
            int toDepth = to.Depth;

            if (fromDepth >= toDepth)
            {
                RenderObject? fromParent = from.Parent
                                           ?? throw new FlutterError(
                                               $"{target} and {this} are not in the same render tree.");
                fromPath.Add(fromParent);
                from = fromParent;
            }

            if (fromDepth <= toDepth)
            {
                RenderObject? toParent = to.Parent;
                if (toParent is null)
                {
                    Debug.Assert(target is not null);
                    throw new FlutterError($"{target} and {this} are not in the same render tree.");
                }

                (toPath ??= [to]).Add(toParent);
                to = toParent;
            }
        }

        Matrix4? fromTransform = null;
        int lastIndex = target is null ? fromPath.Count - 2 : fromPath.Count - 1;
        for (int index = lastIndex; index > 0; index -= 1)
        {
            fromTransform ??= Matrix4.Identity();
            fromPath[index].ApplyPaintTransform(fromPath[index - 1], fromTransform);
        }

        if (toPath is null)
        {
            return fromTransform ?? Matrix4.Identity();
        }

        Matrix4 toTransform = Matrix4.Identity();
        for (int index = toPath.Count - 1; index > 0; index -= 1)
        {
            toPath[index].ApplyPaintTransform(toPath[index - 1], toTransform);
        }

        if (toTransform.Invert() == 0.0)
        {
            return Matrix4.Zero();
        }

        if (fromTransform is null)
        {
            return toTransform;
        }

        fromTransform.Multiply(toTransform);
        return fromTransform;
    }

    private bool TryComputeTransformTo(RenderObject ancestor, bool ancestorSpecified, out Matrix4 transform)
    {
        transform = Matrix4.Identity();
        var renderers = new List<RenderObject>();
        for (RenderObject renderer = this; !ReferenceEquals(renderer, ancestor); renderer = renderer.Parent!)
        {
            renderers.Add(renderer);
            if (renderer.Parent is null)
            {
                return false;
            }
        }

        if (ancestorSpecified)
        {
            renderers.Add(ancestor);
        }

        for (int index = renderers.Count - 1; index > 0; index--)
        {
            renderers[index].ApplyPaintTransform(renderers[index - 1], transform);
        }

        return true;
    }

    public Point LocalToGlobal(Point point, RenderObject? ancestor = null)
    {
        return MatrixUtils.TransformPoint(GetTransformTo(ancestor), point);
    }

    /// <remarks>
    /// Flutter's <c>RenderBox.globalToLocal</c>: an unprojection rather than a plain inverse point
    /// transform, so a perspective transform maps back onto the z = 0 plane the way it was drawn from.
    /// </remarks>
    public Point GlobalToLocal(Point point, RenderObject? ancestor = null)
    {
        Matrix4 transform = GetTransformTo(ancestor);
        double determinant = transform.Invert();
        if (determinant == 0.0)
        {
            // The determinant is zero, so the transform maps the whole plane onto a line or a point.
            return default;
        }

        Vector3 localScreenOrigin = transform.PerspectiveTransform(new Vector3(0.0, 0.0, 0.0));
        Vector3 localViewDirection =
            transform.PerspectiveTransform(new Vector3(0.0, 0.0, 1.0)) - localScreenOrigin;
        if (localViewDirection.Z == 0.0)
        {
            return default;
        }

        Vector3 localScreenPoint = transform.PerspectiveTransform(new Vector3(point.X, point.Y, 0.0));
        Vector3 localPoint =
            localScreenPoint - (localViewDirection * (localScreenPoint.Z / localViewDirection.Z));
        return new Point(localPoint.X, localPoint.Y);
    }

    /// <summary>
    /// Paint this render object into the given context at the given offset.
    /// </summary>
    public abstract void Paint(PaintingContext ctx, Point offset);

    public virtual bool HitTest(BoxHitTestResult result, Point position)
    {
        return false;
    }

    public virtual void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
    }


    internal void _paintWithContext(PaintingContext context, Point offset)
    {
        EnsureNotDisposedMutation();
        if (_debugDoingThisPaint)
        {
            throw new FlutterError([
                new ErrorSummary("Tried to paint a RenderObject reentrantly."),
                DescribeForError(
                    "The following RenderObject was already being painted when it was painted again"),
                new ErrorDescription(
                    "Since this typically indicates an infinite recursion, it is disallowed."),
            ]);
        }

        // If we still need layout, then that means that we were skipped in the layout phase and
        // therefore don't need painting. We might not know that yet (that is, our layer might not have
        // been detached yet), because the same node that skipped us in layout is above us in the tree
        // (obviously) and therefore may not have had a chance to paint yet (since the tree paints in
        // reverse order). In particular this will happen if they have a different layer, because
        // there's a repaint boundary between us.
        if (_needsLayout)
        {
            return;
        }

        if (_needsCompositingBitsUpdate)
        {
            if (Parent is { } parent)
            {
                bool visitedByParent = false;
                parent.VisitChildren(child =>
                {
                    if (ReferenceEquals(child, this))
                    {
                        visitedByParent = true;
                    }
                });

                if (!visitedByParent)
                {
                    throw new FlutterError([
                        new ErrorSummary(
                            "A RenderObject was not visited by the parent's visitChildren during paint."),
                        parent.DescribeForError("The parent was"),
                        DescribeForError("The child that was not visited was"),
                        new ErrorDescription(
                            "A RenderObject with children must implement visitChildren and call the "
                            + "visitor exactly once for each child; it also should not paint children "
                            + "that were removed with dropChild."),
                        new ErrorHint("This usually indicates an error in the Plumix framework itself."),
                    ]);
                }
            }

            throw new FlutterError([
                new ErrorSummary("Tried to paint a RenderObject before its compositing bits were updated."),
                DescribeForError(
                    "The following RenderObject was marked as having dirty compositing bits at the time "
                    + "that it was painted"),
                new ErrorDescription(
                    "A RenderObject that still has dirty compositing bits cannot be painted because this "
                    + "indicates that the tree has not yet been properly configured for creating the "
                    + "layer tree."),
                new ErrorHint("This usually indicates an error in the Plumix framework itself."),
            ]);
        }

        _debugDoingThisPaint = true;
        RenderObject? debugLastActivePaint = _debugActivePaint;
        _debugActivePaint = this;
        Debug.Assert(!IsRepaintBoundary || _layerHandle.Layer is not null);

        _needsPaint = false;
        _needsCompositedLayerUpdate = false;
        _wasRepaintBoundary = IsRepaintBoundary;

        try
        {
            Paint(context, offset);
            Debug.Assert(!_needsLayout); // check that the paint() method didn't mark us dirty again
            Debug.Assert(!_needsPaint); // check that the paint() method didn't mark us dirty again
        }
        catch (Exception exception)
        {
            ReportException("paint", exception);
        }
        finally
        {
            if (Constants.KDebugMode)
            {
                DebugPaint(context, offset);
            }

            _debugActivePaint = debugLastActivePaint;
            _debugDoingThisPaint = false;
        }
    }

    /// <summary>Override point for debug-only paint overlays.</summary>
    /// <remarks>Flutter's <c>RenderObject.debugPaint</c>; a no-op by default.</remarks>
    protected virtual void DebugPaint(PaintingContext context, Point offset)
    {
    }

    /// <remarks>Flutter's <c>RenderObject._reportException</c>.</remarks>
    private void ReportException(string method, Exception exception)
    {
        FlutterError.ReportError(new FlutterErrorDetails(
            exception: exception,
            stack: exception.StackTrace,
            library: "rendering library",
            context: new ErrorDescription($"during {method}()"),
            informationCollector: () =>
            {
                var information = new List<DiagnosticsNode>();
                if (Constants.KDebugMode && DebugCreator is not null)
                {
                    information.Add(new DiagnosticsDebugCreator(DebugCreator));
                }

                information.Add(DescribeForError(
                    "The following RenderObject was being processed when the exception was fired"));
                information.Add(DescribeForError("RenderObject", DiagnosticsTreeStyle.TruncateChildren));
                return information;
            }));
    }

    /// <summary>
    /// The closest render object up the layout-parent chain that is allowed to be mutated right now,
    /// paired with whether mutating it is legal.
    /// </summary>
    /// <remarks>Flutter's <c>RenderObject._debugClosestMutationRoot</c>.</remarks>
    private (RenderObject Root, bool MutationAllowed)? DebugClosestMutationRoot()
    {
        if (DebugDoingThisLayoutWithCallback)
        {
            return (this, true);
        }

        if (Owner is { DebugAllowMutationsToDirtySubtrees: true } && _needsLayout)
        {
            return (this, true);
        }

        if (_debugMutationsLocked)
        {
            return (this, false);
        }

        return DebugLayoutParent?.DebugClosestMutationRoot();
    }

    /// <remarks>Flutter's <c>RenderObject._debugCanPerformMutations</c>.</remarks>
    private void DebugAssertCanPerformMutations()
    {
        EnsureNotDisposedMutation();
        if (!Constants.KDebugMode || Owner is not { DebugDoingLayout: true })
        {
            return;
        }

        (RenderObject Root, bool MutationAllowed)? closest = DebugClosestMutationRoot();
        if (closest?.MutationAllowed != false)
        {
            return;
        }

        RenderObject? activeLayoutRoot = closest.Value.Root;
        RenderObject debugActiveLayout = DebugActiveLayout!;
        string culpritMethodName = debugActiveLayout.DebugDoingThisLayout ? "performLayout" : "performResize";
        string culpritFullMethodName = $"{debugActiveLayout.GetType().Name}.{culpritMethodName}";

        if (ReferenceEquals(activeLayoutRoot, this))
        {
            throw new FlutterError([
                new ErrorSummary(
                    $"A {GetType().Name} was mutated in its own {culpritMethodName} implementation."),
                new ErrorDescription("A RenderObject must not re-dirty itself while still being laid out."),
                new DiagnosticsProperty<RenderObject>(
                    "The RenderObject being mutated was", this, style: DiagnosticsTreeStyle.ErrorProperty),
                new ErrorHint(
                    "Consider using the LayoutBuilder widget to dynamically change a subtree during layout."),
            ]);
        }

        bool isMutatedByAncestor = ReferenceEquals(activeLayoutRoot, debugActiveLayout);
        string description = isMutatedByAncestor
            ? $"A RenderObject must not mutate its descendants in its {culpritMethodName} method."
            : "A RenderObject must not mutate another RenderObject from a different render subtree in "
              + $"its {culpritMethodName} method.";
        var parts = new List<DiagnosticsNode>
        {
            new ErrorSummary($"A {GetType().Name} was mutated in {culpritFullMethodName}."),
            new ErrorDescription(description),
            new DiagnosticsProperty<RenderObject>(
                "The RenderObject being mutated was", this, style: DiagnosticsTreeStyle.ErrorProperty),
            new DiagnosticsProperty<RenderObject>(
                $"The {(isMutatedByAncestor ? "ancestor " : string.Empty)}RenderObject that was mutating "
                + $"the said {GetType().Name} was",
                debugActiveLayout,
                style: DiagnosticsTreeStyle.ErrorProperty),
        };

        if (!isMutatedByAncestor)
        {
            parts.Add(new DiagnosticsProperty<RenderObject>(
                "Their common ancestor was", activeLayoutRoot, style: DiagnosticsTreeStyle.ErrorProperty));
        }

        parts.Add(new ErrorHint(
            "Mutating the layout of another RenderObject may cause some RenderObjects in its subtree to "
            + "be laid out more than once. Consider using the LayoutBuilder widget to dynamically mutate "
            + "a subtree during layout."));
        throw new FlutterError(parts);
    }

    private void EnsureNotDisposedMutation()
    {
        if (!_debugDisposed)
        {
            return;
        }

        throw new FlutterError([
            new ErrorSummary("A disposed RenderObject was mutated."),
            new DiagnosticsProperty<RenderObject>("The disposed RenderObject was", this),
        ]);
    }
}
