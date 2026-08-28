using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/object.dart (approximate)

namespace Plumix.Rendering;

public interface IRenderObject
{
}

/// <summary>
/// An object in the render tree.
/// </summary>
public abstract partial class RenderObject : DiagnosticableTree, IRenderObject, IHitTestTarget
{
    internal bool _wasRepaintBoundary;
    internal Layer? _layer;
    private readonly RenderObjectSemantics _semantics;
    private bool _needsCompositingBitsUpdate;
    private bool _needsCompositedLayerUpdate;
    internal RenderObjectSemantics Semantics => _semantics;

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
        //Debug.Assert(_debugCanPerformMutations);

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
        if (child.Depth <= Depth)
        {
            child.Depth = Depth + 1;
            child.RedepthChildren();
        }
    }

    /// Adjust the [depth] of this node's children, if any.
    ///
    /// Override this method in subclasses with child nodes to call [redepthChild]
    /// for each child. Do not call this method directly.
    protected virtual void RedepthChildren()
    {
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
        if (!ReferenceEquals(child.Parent, this))
        {
            return;
        }

        child.Parent = null;

        if (Attached && child.Attached)
        {
            child.Detach();
        }

        MarkNeedsLayout();
        MarkNeedsCompositingBitsUpdate();
        MarkNeedsPaint();
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
        Owner = owner;
        OnAttach();

        // If the node was dirtied in some way while unattached, make sure to add
        // it to the appropriate dirty list now that an owner is available
        if (_needsLayout)
        {
            if (Parent == null || _isRelayoutBoundary == true)
            {
                Owner.RequestLayoutFor(this);
            }
            else
            {
                Owner.RequestLayout();
            }
        }

        if (_needsCompositingBitsUpdate)
        {
            _needsCompositingBitsUpdate = false;
            MarkNeedsCompositingBitsUpdate();
        }

        if (_needsPaint)
        {
            Owner.RequestPaint();
        }

        if (_semantics.ConfigProvider.Effective.IsSemanticBoundary
            && (_semantics.ParentDataDirty || !_semantics.Built))
        {
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <summary>
    /// Mark this render object as detached from its [PipelineOwner].
    /// </summary>
    public void Detach()
    {
        OnDetach();
        _layer = null;
        ClearOwnSemantics();
        Owner = null;
    }

    protected virtual void OnDetach()
    {
    }

    protected virtual void OnAttach()
    {
    }

    private bool _needsLayout = true;
    private bool _descendantNeedsLayout = true;

    /// <summary>
    /// Whether this [RenderObject] is a known relayout boundary.
    /// </summary>
    private bool? _isRelayoutBoundary;

    /// Whether [invokeLayoutCallback] for this render object is currently running.
    public bool DebugDoingThisLayoutWithCallback { get; private set; } = false;

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
        if (!constraints.IsNormalized)
        {
            throw new InvalidOperationException("RenderObject.layout requires normalized constraints.");
        }

        if (!_needsLayout
            && !_descendantNeedsLayout
            && _constraints is BoxConstraints previousConstraints
            && previousConstraints.Equals(constraints))
        {
            return;
        }

        _isRelayoutBoundary = !parentUsesSize || SizedByParent || constraints.IsTight || Parent == null;
        _debugCanParentUseSize = parentUsesSize;

        _constraints = constraints;

        if (SizedByParent)
        {
            PerformResize();
        }

        PerformLayout();

        _needsLayout = false;
        _descendantNeedsLayout = false;

        // Divergence from Flutter's `layout`, which never dirties compositing bits: Plumix's
        // property setters do not all call `markNeedsCompositingBitsUpdate`, so layout refreshes
        // them wholesale (see `docs/ai/DIVERGENCES.md`).
        MarkNeedsCompositingBitsUpdate();
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }

    protected bool SizedByParent { get; private set; } = false;

    /// <summary>
    /// Updates the render objects size using only the constraints.
    /// </summary>
    protected void PerformResize()
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

        bool wasDoingLayoutWithCallback = DebugDoingThisLayoutWithCallback;
        DebugDoingThisLayoutWithCallback = true;
        try
        {
            callback(constraints);
        }
        finally
        {
            DebugDoingThisLayoutWithCallback = wasDoingLayoutWithCallback;
        }
    }

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
        if (_needsLayout)
        {
            return;
        }

        _needsLayout = true;

        if (Parent != null)
        {
            Parent.MarkDescendantNeedsLayout();

            if (_isRelayoutBoundary == true)
            {
                Owner?.RequestLayoutFor(this);
                return;
            }

            Parent.MarkNeedsLayout();
            return;
        }

        Owner?.RequestLayoutFor(this);
    }

    protected void MarkParentNeedsLayout()
    {
        _needsLayout = true;

        var parent = this.Parent!;

        parent.MarkDescendantNeedsLayout();

        if (!DebugDoingThisLayoutWithCallback)
        {
            parent.MarkNeedsLayout();
        }
    }

    private void MarkDescendantNeedsLayout()
    {
        if (_descendantNeedsLayout)
        {
            return;
        }

        _descendantNeedsLayout = true;

        if (Parent != null)
        {
            Parent.MarkDescendantNeedsLayout();
        }
    }

    public void MarkNeedsCompositingBitsUpdate()
    {
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

        VisitChildren(static child => child.UpdateCompositingBits());

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

        if (!needsCompositing)
        {
            VisitChildren(child =>
            {
                if (child.NeedsCompositing || child.IsRepaintBoundary || child.AlwaysNeedsCompositing)
                {
                    needsCompositing = true;
                }
            });
        }

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
        Owner?.RequestSemanticsUpdateFor(this);
        Owner?.RequestSemanticsGeometryUpdateFor(this);
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
    internal bool NeedsLayoutOrDescendantNeedsLayout => _needsLayout || _descendantNeedsLayout;
    internal bool NeedsLayout => _needsLayout;
    internal bool NeedsCompositingBitsUpdate => _needsCompositingBitsUpdate;
    internal bool SemanticsParentDataDirty => _semantics.ParentDataDirty;

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
    internal bool NeedsCompositing { get; private set; }

    protected void MarkNeedsPaint()
    {
        if (_needsPaint)
        {
            return;
        }

        _needsPaint = true;

        if (IsRepaintBoundary && _wasRepaintBoundary)
        {
            Owner?.RequestPaintFor(this);
            return;
        }

        if (Parent != null)
        {
            Parent.MarkNeedsPaint();
            return;
        }

        Owner?.RequestPaintFor(this);
    }

    protected void MarkNeedsCompositedLayerUpdate()
    {
        if (_needsCompositedLayerUpdate)
        {
            return;
        }

        _needsCompositedLayerUpdate = true;

        if (_needsPaint)
        {
            return;
        }

        if (IsRepaintBoundary && _wasRepaintBoundary)
        {
            Owner?.RequestPaintFor(this);
            return;
        }

        MarkNeedsPaint();
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



    internal void HandleSkippedPaintingOnDetachedLayer()
    {
        if (!Attached || !IsRepaintBoundary || _layer is not OffsetLayer layer || layer.Parent != null)
        {
            return;
        }

        RenderObject? node = Parent;
        while (node != null)
        {
            if (node.IsRepaintBoundary)
            {
                node._needsPaint = true;
                node.Owner?.RequestPaintFor(node);

                if (node._layer is not OffsetLayer ancestorLayer || ancestorLayer.Parent != null)
                {
                    break;
                }
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
    }

    /// <summary>Whether this render object paints <paramref name="child"/> at all.</summary>
    /// <remarks>
    /// Flutter's <c>RenderObject.paintsChild</c>. A parent that zeroes the matrix in
    /// <see cref="ApplyPaintTransform"/> to signal "not painted" must return <c>false</c> here.
    /// </remarks>
    public virtual bool PaintsChild(RenderObject child) => true;

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

    public Matrix4 GetTransformTo(RenderObject? ancestor = null)
    {
        bool ancestorSpecified = ancestor is not null;
        ancestor ??= Owner?.Root
                     ?? throw new InvalidOperationException("The render object is not attached to a render tree.");

        if (!TryComputeTransformTo(ancestor, ancestorSpecified, out Matrix4 transform))
        {
            throw new InvalidOperationException(
                "The requested render object is not an ancestor of this render object.");
        }

        return transform;
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
        // assert(!_debugDisposed);
        // assert(() {
        //   if (_debugDoingThisPaint) {
        //     throw FlutterError.fromParts(<DiagnosticsNode>[
        //       ErrorSummary('Tried to paint a RenderObject reentrantly.'),
        //       describeForError(
        //         'The following RenderObject was already being painted when it was '
        //         'painted again',
        //       ),
        //       ErrorDescription(
        //         'Since this typically indicates an infinite recursion, it is '
        //         'disallowed.',
        //       ),
        //     ]);
        //   }
        //   return true;
        // }());
        // If we still need layout, then that means that we were skipped in the
        // layout phase and therefore don't need painting. We might not know that
        // yet (that is, our layer might not have been detached yet), because the
        // same node that skipped us in layout is above us in the tree (obviously)
        // and therefore may not have had a chance to paint yet (since the tree
        // paints in reverse order). In particular this will happen if they have
        // a different layer, because there's a repaint boundary between us.
        if (_needsLayout)
        {
            return;
        }

        if (_needsCompositingBitsUpdate)
        {
            throw new InvalidOperationException(
                "RenderObject.paint called before compositing bits were updated.");
        }

        // if (!kReleaseMode && debugProfilePaintsEnabled)
        // {
        //     Map<String, String>? debugTimelineArguments;
        //     assert(() {
        //         if (debugEnhancePaintTimelineArguments)
        //         {
        //             debugTimelineArguments = toDiagnosticsNode().toTimelineArguments();
        //         }
        //
        //         return true;
        //     }
        //     ());
        //     FlutterTimeline.startSync('$runtimeType', arguments: debugTimelineArguments);
        // }

        // assert(() {
        //     if (_needsCompositingBitsUpdate)
        //     {
        //         final RenderObject? parent = this.parent;
        //         if (parent != null)
        //         {
        //             bool visitedByParent = false;
        //             parent.visitChildren((RenderObject child) {
        //                 if (child == this)
        //                 {
        //                     visitedByParent = true;
        //                 }
        //             });
        //             if (!visitedByParent)
        //             {
        //                 throw FlutterError.fromParts( < DiagnosticsNode >
        //                 [
        //                     ErrorSummary(
        //                         "A RenderObject was not visited by the parent's visitChildren "
        //                     'during paint.',
        //                     ),
        //                     parent.describeForError('The parent was'),
        //                     describeForError('The child that was not visited was'),
        //                     ErrorDescription(
        //                         'A RenderObject with children must implement visitChildren and '
        //                     'call the visitor exactly once for each child; it also should not '
        //                     'paint children that were removed with dropChild.',
        //                     ),
        //                     ErrorHint('This usually indicates an error in the Plumix.Sample framework itself.'),
        //                 ]);
        //             }
        //         }
        //
        //         throw FlutterError.fromParts( < DiagnosticsNode >
        //         [
        //             ErrorSummary(
        //                 'Tried to paint a RenderObject before its compositing bits were '
        //             'updated.',
        //             ),
        //             describeForError(
        //                 'The following RenderObject was marked as having dirty compositing '
        //             'bits at the time that it was painted',
        //             ),
        //             ErrorDescription(
        //                 'A RenderObject that still has dirty compositing bits cannot be '
        //             'painted because this indicates that the tree has not yet been '
        //             'properly configured for creating the layer tree.',
        //             ),
        //             ErrorHint('This usually indicates an error in the Plumix.Sample framework itself.'),
        //         ]);
        //     }
        //
        //     return true;
        // }
        // ());
        // assert(() {
        //     _debugDoingThisPaint = true;
        //     debugLastActivePaint = _debugActivePaint;
        //     _debugActivePaint = this;
        //     assert(!isRepaintBoundary || _layerHandle.layer != null);
        //     return true;
        // }
        // ());
        _needsPaint = false;
        _needsCompositedLayerUpdate = false;

        _wasRepaintBoundary = IsRepaintBoundary;

        try
        {
            Paint(context, offset);
            Debug.Assert(!_needsLayout); // check that the paint() method didn't mark us dirty again
            Debug.Assert(!_needsPaint); // check that the paint() method didn't mark us dirty again
        }
        catch (Exception)
        {
            //_reportException('paint', e, stack);
        }

        // assert(() {
        //     debugPaint(context, offset);
        //     _debugActivePaint = debugLastActivePaint;
        //     _debugDoingThisPaint = false;
        //     return true;
        // }
        // ());
        // if (!kReleaseMode && debugProfilePaintsEnabled)
        // {
        //     FlutterTimeline.finishSync();
        // }
    }
}
