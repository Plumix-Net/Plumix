using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart

namespace Plumix;

/// <summary>Signature for the callback to <see cref="PipelineOwner.VisitChildren"/>.</summary>
/// <remarks>Flutter's <c>PipelineOwnerVisitor</c>.</remarks>
public delegate void PipelineOwnerVisitor(PipelineOwner child);

/// <summary>
/// Manages the rendering pipeline: which render objects asked to be visited in each phase, and in
/// what order the phases drain.
/// </summary>
/// <remarks>
/// Flutter's <c>PipelineOwner</c>. Owners can be organized in a tree with
/// <see cref="AdoptChild"/>/<see cref="DropChild"/>, where each owner drives one render tree; every
/// flush phase runs on the owner's own nodes first and then on its children. An owner may also be
/// attached to a <see cref="PipelineManifold"/>, which tells it whether semantics are being produced
/// and how to ask for a frame.
/// </remarks>
public partial class PipelineOwner : DiagnosticableTree
{
    /// <summary>Creates a pipeline owner that is not yet driving any render tree.</summary>
    /// <remarks>Flutter's <c>PipelineOwner</c> constructor.</remarks>
    public PipelineOwner(
        Action? onNeedVisualUpdate = null,
        Action? onSemanticsOwnerCreated = null,
        Action<SemanticsUpdate>? onSemanticsUpdate = null,
        Action? onSemanticsOwnerDisposed = null)
    {
        OnNeedVisualUpdate = onNeedVisualUpdate;
        OnSemanticsOwnerCreated = onSemanticsOwnerCreated;
        _onSemanticsUpdate = onSemanticsUpdate;
        OnSemanticsOwnerDisposed = onSemanticsOwnerDisposed;
        DebugAssertions.Assert(FoundationDebug.DebugMaybeDispatchCreated("rendering", "PipelineOwner", this));
    }

    /// <summary>Called when a render object of this pipeline wants to update its appearance.</summary>
    /// <remarks>
    /// Flutter's <c>PipelineOwner.onNeedVisualUpdate</c>. When it is set it takes precedence over
    /// <see cref="PipelineManifold.RequestVisualUpdate"/>.
    /// </remarks>
    public Action? OnNeedVisualUpdate { get; set; }

    /// <summary>Called whenever this owner creates a <see cref="Rendering.SemanticsOwner"/>.</summary>
    /// <remarks>Flutter's <c>PipelineOwner.onSemanticsOwnerCreated</c>.</remarks>
    public Action? OnSemanticsOwnerCreated { get; set; }

    /// <summary>Called whenever this owner disposes its <see cref="Rendering.SemanticsOwner"/>.</summary>
    /// <remarks>Flutter's <c>PipelineOwner.onSemanticsOwnerDisposed</c>.</remarks>
    public Action? OnSemanticsOwnerDisposed { get; set; }

    private Action<SemanticsUpdate>? _onSemanticsUpdate;

    /// <summary>Called whenever this owner's semantics owner emits a <see cref="SemanticsUpdate"/>.</summary>
    /// <remarks>
    /// Flutter's <c>PipelineOwner.onSemanticsUpdate</c>, which is final and read once when the
    /// semantics owner is created. Plumix's <see cref="Rendering.SemanticsOwner"/> takes the callback
    /// as a property rather than a constructor argument, so assigning this after the semantics owner
    /// exists forwards the new callback to it instead of being ignored.
    /// </remarks>
    public Action<SemanticsUpdate>? OnSemanticsUpdate
    {
        get => _onSemanticsUpdate;
        set
        {
            _onSemanticsUpdate = value;
            if (_semanticsOwner is not null)
            {
                _semanticsOwner.OnSemanticsUpdate = value;
            }
        }
    }

    private RenderObject? _rootNode;

    /// <summary>The unique object managed by this pipeline that has no parent.</summary>
    /// <remarks>Flutter's <c>PipelineOwner.rootNode</c>.</remarks>
    public virtual RenderObject? RootNode
    {
        get => _rootNode;
        set
        {
            if (ReferenceEquals(_rootNode, value))
            {
                return;
            }

            _rootNode?.Detach();
            _rootNode = value;
            _rootNode?.Attach(this);
        }
    }

    private bool _needsSemantics;
    private readonly HashSet<RenderObject> _nodesNeedingLayout = [];
    private bool _shouldMergeDirtyNodes;
    private readonly HashSet<RenderObject> _nodesNeedingCompositingBitsUpdate = [];
    private readonly HashSet<RenderObject> _nodesNeedingPaint = [];
    private LinkedList<RenderObject>? _paintNodesInProgress;
    private readonly HashSet<RenderObject> _nodesNeedingSemantics = [];
    private readonly HashSet<RenderObject> _nodesNeedingSemanticsGeometryUpdate = [];
    private SemanticsOwner? _semanticsOwner;
    private int _outstandingSemanticsHandles;

    // TREE MANAGEMENT
    private readonly List<PipelineOwner> _children = [];
    private PipelineManifold? _manifold;
    private PipelineOwner? _parent;
    private bool _debugDoingChildLayout;

    /// <summary>The render objects queued for the next layout pass.</summary>
    /// <remarks>
    /// Flutter's <c>PipelineOwner.nodesNeedingLayout</c>, a <c>@protected</c> getter subclasses use to
    /// inspect the dirty list. This accessor exposes it internally to the parity tests.
    /// </remarks>
    internal IReadOnlyCollection<RenderObject> NodesNeedingLayoutForTest => _nodesNeedingLayout;

    /// <summary>The render objects queued for the next paint pass.</summary>
    /// <remarks>Flutter's <c>PipelineOwner.nodesNeedingPaint</c>.</remarks>
    internal IReadOnlyCollection<RenderObject> NodesNeedingPaintForTest => _nodesNeedingPaint;

    /// <summary>The manifold this owner is attached to, for the parity tests.</summary>
    internal PipelineManifold? ManifoldForTest => _manifold;

    /// <summary>Requests that the host schedule a new frame.</summary>
    /// <remarks>
    /// Flutter's <c>PipelineOwner.requestVisualUpdate</c>: <see cref="OnNeedVisualUpdate"/> wins, and
    /// the manifold is only asked when no callback is configured.
    /// </remarks>
    public void RequestVisualUpdate()
    {
        if (OnNeedVisualUpdate is { } onNeedVisualUpdate)
        {
            onNeedVisualUpdate();
        }
        else
        {
            _manifold?.RequestVisualUpdate();
        }
    }

    /// <summary>
    /// Whether a render object may currently dirty a subtree that is already dirty.
    /// </summary>
    /// <remarks>Flutter's <c>PipelineOwner._debugAllowMutationsToDirtySubtrees</c>.</remarks>
    internal bool DebugAllowMutationsToDirtySubtrees { get; private set; }

    /// <summary>
    /// Runs <paramref name="callback"/> with mutations to dirty subtrees temporarily allowed, and
    /// arranges for the current layout pass to re-sort its dirty list afterwards.
    /// </summary>
    /// <remarks>Flutter's <c>PipelineOwner._enableMutationsToDirtySubtrees</c>.</remarks>
    internal void EnableMutationsToDirtySubtrees(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        bool oldState = DebugAllowMutationsToDirtySubtrees;
        DebugAllowMutationsToDirtySubtrees = true;
        try
        {
            callback();
        }
        finally
        {
            _shouldMergeDirtyNodes = true;
            DebugAllowMutationsToDirtySubtrees = oldState;
        }
    }

    internal void RequestLayoutFor(RenderObject node) => _nodesNeedingLayout.Add(node);

    internal void RequestCompositingBitsUpdateFor(RenderObject node) => _nodesNeedingCompositingBitsUpdate.Add(node);

    internal void RequestPaintFor(RenderObject node) => _nodesNeedingPaint.Add(node);

    /// <summary>The object managing semantics for this pipeline owner, if any.</summary>
    /// <remarks>
    /// Flutter's <c>PipelineOwner.semanticsOwner</c>. It exists while the
    /// <see cref="PipelineManifold"/> this owner is attached to has
    /// <see cref="PipelineManifold.SemanticsEnabled"/> set, or while any handle from
    /// <see cref="EnsureSemantics"/> is still open; it reverts to <c>null</c> once neither holds. When
    /// it is <c>null</c> the owner skips every semantics step.
    /// </remarks>
    public SemanticsOwner? SemanticsOwner => _semanticsOwner;

    /// <summary>Whether a semantics tree is being produced at all.</summary>
    /// <remarks>Flutter's <c>PipelineOwner.semanticsOwner != null</c>.</remarks>
    internal bool HasSemanticsOwner => _semanticsOwner is not null;

    /// <summary>The number of open handles from <see cref="EnsureSemantics"/>.</summary>
    /// <remarks>Flutter's deprecated <c>PipelineOwner.debugOutstandingSemanticsHandles</c>.</remarks>
    internal int DebugOutstandingSemanticsHandles => _outstandingSemanticsHandles;

    /// <summary>
    /// Opens a semantics handle, forcing this owner to produce a semantics tree until the handle is
    /// closed.
    /// </summary>
    /// <param name="listener">
    /// Notified whenever the semantics tree updates, for as long as the handle is open.
    /// </param>
    /// <remarks>Flutter's deprecated <c>PipelineOwner.ensureSemantics</c>.</remarks>
    public SemanticsHandle EnsureSemantics(Action? listener = null)
    {
        _outstandingSemanticsHandles += 1;
        UpdateSemanticsOwner();
        return new LocalSemanticsHandle(this, listener);
    }

    /// <summary>The handle <see cref="EnsureSemantics"/> returns.</summary>
    /// <remarks>Dart's private <c>_LocalSemanticsHandle</c>.</remarks>
    private sealed class LocalSemanticsHandle : SemanticsHandle
    {
        public LocalSemanticsHandle(PipelineOwner owner, Action? listener)
            : base(() => Release(owner, listener), "rendering", "_LocalSemanticsHandle")
        {
            if (listener is not null)
            {
                owner._semanticsOwner!.AddListener(listener);
            }
        }

        private static void Release(PipelineOwner owner, Action? listener)
        {
            if (listener is not null)
            {
                owner._semanticsOwner!.RemoveListener(listener);
            }

            owner.DidDisposeSemanticsHandle();
        }
    }

    private void UpdateSemanticsOwner()
    {
        if ((_manifold?.SemanticsEnabled ?? false) || _outstandingSemanticsHandles > 0)
        {
            if (_semanticsOwner is null)
            {
                if (_onSemanticsUpdate is null)
                {
                    throw new AssertionError(
                        "Attempted to enable semantics without configuring an onSemanticsUpdate callback.");
                }

                _semanticsOwner = new SemanticsOwner { OnSemanticsUpdate = _onSemanticsUpdate };
                OnSemanticsOwnerCreated?.Invoke();
            }
        }
        else if (_semanticsOwner is not null)
        {
            _semanticsOwner.Dispose();
            _semanticsOwner = null;
            OnSemanticsOwnerDisposed?.Invoke();
        }
    }

    private void DidDisposeSemanticsHandle()
    {
        Debug.Assert(_semanticsOwner is not null);
        _outstandingSemanticsHandles -= 1;
        UpdateSemanticsOwner();
    }

    internal void RequestSemanticsGeometryUpdateFor(RenderObject node)
    {
        if (!_nodesNeedingSemanticsGeometryUpdate.Add(node))
        {
            return;
        }

        _needsSemantics = true;
        RequestVisualUpdate();
    }

    internal void RequestSemanticsUpdateFor(RenderObject node)
    {
        if (!_nodesNeedingSemantics.Add(node))
        {
            return;
        }

        _needsSemantics = true;
        RequestVisualUpdate();
    }

    internal void ForgetSemanticsUpdateFor(RenderObject node)
    {
        _nodesNeedingSemantics.Remove(node);
        _needsSemantics = _nodesNeedingSemantics.Count > 0
                          || _nodesNeedingSemanticsGeometryUpdate.Count > 0;
    }

    internal int PendingSemanticsNodeCount => _nodesNeedingSemantics.Count;

    /// <summary>
    /// Whether this pipeline owner is currently running <see cref="FlushLayout()"/>. Ports Flutter's
    /// <c>PipelineOwner.debugDoingLayout</c>, which gates the layout-phase-only entry points —
    /// notably <c>RawGestureDetectorState.ReplaceGestureRecognizers</c>.
    /// </summary>
    public bool DebugDoingLayout { get; private set; }

    /// <summary>Update the layout information for all dirty render objects.</summary>
    /// <remarks>Flutter's <c>PipelineOwner.flushLayout</c>.</remarks>
    public void FlushLayout()
    {
        using EventLoop.FrameworkThreadScope scope = EventLoop.EnterFrameworkThread();
        if (!Constants.KReleaseMode)
        {
            Dictionary<string, object?>? debugTimelineArguments = null;
            if (Constants.KDebugMode && RenderingDebug.EnhanceLayoutTimelineArguments)
            {
                debugTimelineArguments = new Dictionary<string, object?>
                {
                    ["dirty count"] = _nodesNeedingLayout.Count.ToString(CultureInfo.InvariantCulture),
                    ["dirty list"] = DebugDescribeDirtyList(_nodesNeedingLayout),
                };
            }

            FlutterTimeline.StartSync(
                "LAYOUT" + DebugRootSuffixForTimelineEventNames,
                arguments: debugTimelineArguments);
        }

        DebugDoingLayout = true;
        try
        {
            if (_nodesNeedingLayout.Count > 0)
            {
                FlushLayoutNodes();
            }

            _debugDoingChildLayout = true;
            foreach (PipelineOwner child in _children.ToArray())
            {
                child.FlushLayout();
            }

            Debug.Assert(
                _nodesNeedingLayout.Count == 0,
                "Child PipelineOwners must not dirty nodes in their parent.");
        }
        finally
        {
            _shouldMergeDirtyNodes = false;
            DebugDoingLayout = false;
            _debugDoingChildLayout = false;
            if (!Constants.KReleaseMode)
            {
                FlutterTimeline.FinishSync();
            }
        }
    }

    // Dart's `_debugRootSuffixForTimelineEventNames`.
    private string DebugRootSuffixForTimelineEventNames => _parent is null ? " (root)" : "";

    // Dart's `'$_nodesNeedingLayout'` / `'$_nodesNeedingPaint'`: a list's `toString`.
    private static string DebugDescribeDirtyList(IEnumerable<RenderObject> nodes) =>
        "[" + string.Join(", ", nodes) + "]";

    private void FlushLayoutNodes()
    {
        _shouldMergeDirtyNodes = false;

        while (_nodesNeedingLayout.Count > 0)
        {
            List<RenderObject> dirtyNodes = [.. _nodesNeedingLayout.OrderBy(static node => node.Depth)];
            _nodesNeedingLayout.Clear();

            for (int index = 0; index < dirtyNodes.Count; index += 1)
            {
                if (_shouldMergeDirtyNodes)
                {
                    // A layout callback dirtied nodes mid-pass: fold what is left of this pass back
                    // into the dirty list so the merged set is re-sorted by depth.
                    _shouldMergeDirtyNodes = false;
                    if (_nodesNeedingLayout.Count > 0)
                    {
                        for (int rest = index; rest < dirtyNodes.Count; rest += 1)
                        {
                            _nodesNeedingLayout.Add(dirtyNodes[rest]);
                        }

                        break;
                    }
                }

                RenderObject node = dirtyNodes[index];
                if (!node.Attached || !ReferenceEquals(node.Owner, this))
                {
                    continue;
                }

                if (!node.NeedsLayout)
                {
                    continue;
                }

                node.LayoutWithoutResize();
            }

            _shouldMergeDirtyNodes = false;
        }

        _shouldMergeDirtyNodes = false;
    }

    /// <summary>Updates the <see cref="RenderObject.NeedsCompositing"/> bits.</summary>
    /// <remarks>Flutter's <c>PipelineOwner.flushCompositingBits</c>.</remarks>
    public void FlushCompositingBits()
    {
        using EventLoop.FrameworkThreadScope scope = EventLoop.EnterFrameworkThread();
        if (!Constants.KReleaseMode)
        {
            FlutterTimeline.StartSync("UPDATING COMPOSITING BITS" + DebugRootSuffixForTimelineEventNames);
        }

        if (_nodesNeedingCompositingBitsUpdate.Count > 0)
        {
            FlushCompositingBitsNodes();
        }

        foreach (PipelineOwner child in _children.ToArray())
        {
            child.FlushCompositingBits();
        }

        Debug.Assert(
            _nodesNeedingCompositingBitsUpdate.Count == 0,
            "Child PipelineOwners must not dirty nodes in their parent.");
        if (!Constants.KReleaseMode)
        {
            FlutterTimeline.FinishSync();
        }
    }

    private void FlushCompositingBitsNodes()
    {
        var dirtyNodes = _nodesNeedingCompositingBitsUpdate
            .OrderBy(static node => node.Depth)
            .ToArray();

        foreach (RenderObject node in dirtyNodes)
        {
            if (node.NeedsCompositingBitsUpdate && ReferenceEquals(node.Owner, this))
            {
                node.UpdateCompositingBits();
            }
        }

        _nodesNeedingCompositingBitsUpdate.Clear();
    }

    /// <summary>
    /// Whether this pipeline owner is currently running <see cref="FlushPaint"/>. Ports Flutter's
    /// <c>PipelineOwner.debugDoingPaint</c>, which gates paint-phase-only reads such as
    /// <see cref="RenderObject"/> geometry published to descendants.
    /// </summary>
    public bool DebugDoingPaint { get; private set; }

    /// <summary>
    /// Whether this pipeline owner is currently running <see cref="FlushSemantics"/>.
    /// </summary>
    /// <remarks>Flutter's <c>PipelineOwner._debugDoingSemantics</c>.</remarks>
    public bool DebugDoingSemantics { get; private set; }

    public void FlushPaint()
    {
        using EventLoop.FrameworkThreadScope scope = EventLoop.EnterFrameworkThread();
        if (!Constants.KReleaseMode)
        {
            Dictionary<string, object?>? debugTimelineArguments = null;
            if (Constants.KDebugMode && RenderingDebug.EnhancePaintTimelineArguments)
            {
                debugTimelineArguments = new Dictionary<string, object?>
                {
                    ["dirty count"] = _nodesNeedingPaint.Count.ToString(CultureInfo.InvariantCulture),
                    ["dirty list"] = DebugDescribeDirtyList(_nodesNeedingPaint),
                };
            }

            FlutterTimeline.StartSync(
                "PAINT" + DebugRootSuffixForTimelineEventNames,
                arguments: debugTimelineArguments);
        }

        DebugDoingPaint = true;
        try
        {
            if (_nodesNeedingPaint.Count > 0)
            {
                FlushPaintNodes();
            }

            foreach (PipelineOwner child in _children.ToArray())
            {
                child.FlushPaint();
            }

            Debug.Assert(
                _nodesNeedingPaint.Count == 0,
                "Child PipelineOwners must not dirty nodes in their parent.");
        }
        finally
        {
            DebugDoingPaint = false;
            if (!Constants.KReleaseMode)
            {
                FlutterTimeline.FinishSync();
            }
        }
    }

    private void FlushPaintNodes()
    {
        var dirtyNodes = new LinkedList<RenderObject>(
            _nodesNeedingPaint.OrderByDescending(static node => node.Depth));
        _nodesNeedingPaint.Clear();
        _paintNodesInProgress = dirtyNodes;
        try
        {
            while (dirtyNodes.First is { } entry)
            {
                RenderObject node = entry.Value;
                dirtyNodes.RemoveFirst();
                Debug.Assert(node._layer is not null);
                if ((!node.NeedsPaint && !node.NeedsCompositedLayerUpdate) || !ReferenceEquals(node.Owner, this))
                {
                    continue;
                }

                if (node._layer is { Attached: true })
                {
                    Debug.Assert(node.IsRepaintBoundary);
                    if (node.NeedsPaint)
                    {
                        PaintingContext.RepaintCompositedChild(node);
                    }
                    else
                    {
                        PaintingContext.UpdateLayerProperties(node);
                    }
                }
                else
                {
                    node.HandleSkippedPaintingOnDetachedLayer();
                }
            }
        }
        finally
        {
            _paintNodesInProgress = null;
        }
    }

    /// <summary>Compiles the semantics for the render objects marked as needing a semantics update.</summary>
    /// <remarks>Flutter's <c>PipelineOwner.flushSemantics</c>.</remarks>
    public void FlushSemantics()
    {
        using EventLoop.FrameworkThreadScope scope = EventLoop.EnterFrameworkThread();
        if (_semanticsOwner is null)
        {
            return;
        }

        if (!Constants.KReleaseMode)
        {
            FlutterTimeline.StartSync("SEMANTICS" + DebugRootSuffixForTimelineEventNames);
        }

        DebugDoingSemantics = true;
        try
        {
            if (_needsSemantics)
            {
                FlushSemanticsNodes();
            }

            foreach (PipelineOwner child in _children.ToArray())
            {
                child.FlushSemantics();
            }

            Debug.Assert(
                _nodesNeedingSemanticsGeometryUpdate.Count == 0,
                "Child PipelineOwners must not dirty nodes' geometry in their parent.");
        }
        finally
        {
            DebugDoingSemantics = false;
            if (!Constants.KReleaseMode)
            {
                FlutterTimeline.FinishSync();
            }
        }
    }

    private void FlushSemanticsNodes()
    {
        // Phase 1 and 2: rebuild the fragment tree top-down, so that one parent change invalidates
        // the subtree once instead of once per descendant.
        RenderObject[] nodesToProcess = _nodesNeedingSemantics
            .Where(node => node.Attached && ReferenceEquals(node.Owner, this) && !node.NeedsLayout)
            .OrderBy(static node => node.Depth)
            .ToArray();

        // Dart clears the whole queue here, including the nodes the filter rejected, and relies on
        // them re-queueing through `markNeedsSemanticsUpdate` once they are laid out again. Plumix
        // keeps the rejected nodes queued instead: `MarkNeedsSemanticsUpdate` short-circuits on a
        // fragment that is already dirty, so a node dropped while its layout was pending would never
        // come back. See `docs/ai/DIVERGENCES.md`.
        _nodesNeedingSemantics.RemoveWhere(node =>
            node.Attached && ReferenceEquals(node.Owner, this) && !node.NeedsLayout);

        if (!Constants.KReleaseMode)
        {
            FlutterTimeline.StartSync("Semantics.updateChildren");
        }

        foreach (RenderObject node in nodesToProcess)
        {
            // A render object whose parent data is dirty is either blocked by a sibling or hidden by
            // its parent's `VisitChildrenForSemantics`. Updating it now would leave a gap of dirty
            // parent data behind when the branch rejoins the tree.
            if (node.Semantics.ParentDataDirty)
            {
                continue;
            }

            node.Semantics.UpdateChildren();
        }

        if (!Constants.KReleaseMode)
        {
            FlutterTimeline.FinishSync();
        }

        if (!Constants.KReleaseMode)
        {
            FlutterTimeline.StartSync("Semantics.ensureGeometry");
        }

        // Phase 3: recompute the geometry of everything whose transform, clip or size may have moved.
        RenderObject[] nodesToProcessGeometry = _nodesNeedingSemanticsGeometryUpdate
            .Where(node => node.Attached
                           && ReferenceEquals(node.Owner, this)
                           && !node.NeedsLayout
                           && !node.Semantics.ParentDataDirty)
            .ToArray();
        _nodesNeedingSemanticsGeometryUpdate.Clear();

        foreach (RenderObject node in nodesToProcessGeometry)
        {
            RenderObjectSemantics semantics = node.Semantics;
            if (semantics.ShouldFormSemanticsNode && semantics.GeometryDirty)
            {
                continue;
            }

            if (semantics.ShouldFormSemanticsNode
                && (node.IsRelayoutBoundaryForSemantics
                    || semantics.Geometry?.Rect.Size != node.SemanticBoundsForSemantics.Size))
            {
                // A relayout boundary can change size without its parent relaying out, so its own
                // geometry has to be dropped too. Plumix also drops it whenever the render object's
                // semantic bounds no longer match the cached rect, because a render object can
                // override `SemanticBounds` and change it without any layout at all.
                semantics.Geometry = null;
                continue;
            }

            if (!semantics.ContributesToSemanticsTree)
            {
                // This render object only presents its subtree in the merge-up, so every node the
                // merge-up carries needs fresh geometry.
                foreach (RenderObjectSemantics child in semantics.MergeUpRenderObjectSemantics)
                {
                    if (child.ShouldFormSemanticsNode)
                    {
                        child.Geometry = null;
                    }
                    else
                    {
                        foreach (RenderObjectSemantics nodeInSubtree in child.NodeFormingChildren)
                        {
                            nodeInSubtree.Geometry = null;
                        }
                    }
                }

                continue;
            }

            foreach (RenderObjectSemantics child in semantics.NodeFormingChildren)
            {
                child.Geometry = null;
            }
        }

        object treeShapeToken = new();
        var nodeToEnsureGeometry = new HashSet<RenderObjectSemantics>();
        foreach (RenderObject node in nodesToProcessGeometry)
        {
            node.Semantics.ComputeAncestorInfo(treeShapeToken);
            if (node.Semantics.FirstAncestorNodeWithCleanGeometry is { } ancestor)
            {
                nodeToEnsureGeometry.Add(ancestor);
            }
        }

        foreach (RenderObjectSemantics semantics in nodeToEnsureGeometry
                     .OrderBy(static semantics => semantics.RenderObject.Depth))
        {
            semantics.EnsureGeometry();
        }

        if (!Constants.KReleaseMode)
        {
            FlutterTimeline.FinishSync();
        }

        if (!Constants.KReleaseMode)
        {
            FlutterTimeline.StartSync("Semantics.ensureSemanticsNode");
        }

        // Phase 4: produce the semantics nodes, bottom-up.
        foreach (RenderObject node in nodesToProcess.Reverse())
        {
            node.Semantics.ComputeAncestorInfo(treeShapeToken);
            var targets = new List<RenderObjectSemantics>();
            if (node.Semantics.GeometryDirty)
            {
                if (node.Semantics.FirstAncestorNodeWithCleanGeometry is { } ancestor)
                {
                    targets.Add(ancestor);
                }
            }
            else
            {
                // A boundary that became invisible has to be removed from its parent's children, so
                // the parent is rebuilt as well.
                if (node.Semantics.Geometry?.IsVisible == false && !node.Semantics.IsRoot)
                {
                    if (node.Semantics.ParentInSemanticsTree is { } parentInSemanticsTree)
                    {
                        if (!parentInSemanticsTree.GeometryDirty)
                        {
                            targets.Add(parentInSemanticsTree);
                        }
                        else if (parentInSemanticsTree.FirstAncestorNodeWithCleanGeometry is { } cleanAncestor)
                        {
                            targets.Add(cleanAncestor);
                        }
                    }
                }

                targets.Add(node.Semantics);
            }

            foreach (RenderObjectSemantics target in targets)
            {
                if (!target.ParentDataDirty)
                {
                    target.EnsureSemanticsNode(_semanticsOwner!);
                }
            }
        }

        if (!Constants.KReleaseMode)
        {
            FlutterTimeline.FinishSync();
        }

        _semanticsOwner!.SendSemanticsUpdate();
        _needsSemantics = _nodesNeedingSemantics.Count > 0
                          || _nodesNeedingSemanticsGeometryUpdate.Count > 0;
    }

    internal void ForgetPaintFor(RenderObject node)
    {
        _nodesNeedingPaint.Remove(node);
        // Plumix tears isolated trees down before paint. Cancel entries already in the snapshot
        // as well, before their render objects release the layer that FlushPaint must assert on.
        _paintNodesInProgress?.Remove(node);
    }

    /// <summary>
    /// Marks this pipeline owner as attached to <paramref name="manifold"/>.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>PipelineOwner.attach</c>. Typically called only on the root owner; children are
    /// attached to their parent's manifold by <see cref="AdoptChild"/>.
    /// </remarks>
    public void Attach(PipelineManifold manifold)
    {
        ArgumentNullException.ThrowIfNull(manifold);
        if (_manifold is not null)
        {
            throw new AssertionError("An attached PipelineOwner cannot be attached again.");
        }

        _manifold = manifold;
        _manifold.AddListener(UpdateSemanticsOwner);
        UpdateSemanticsOwner();

        foreach (PipelineOwner child in _children.ToArray())
        {
            child.Attach(manifold);
        }
    }

    /// <summary>Marks this pipeline owner as detached from its <see cref="PipelineManifold"/>.</summary>
    /// <remarks>
    /// Flutter's <c>PipelineOwner.detach</c>. The semantics owner is deliberately left alone so its
    /// clients survive a re-attach; it is reconciled in <see cref="Attach(PipelineManifold)"/> or
    /// released in <see cref="Dispose"/>.
    /// </remarks>
    public void Detach()
    {
        if (_manifold is null)
        {
            throw new AssertionError("A detached PipelineOwner cannot be detached again.");
        }

        _manifold.RemoveListener(UpdateSemanticsOwner);
        _manifold = null;

        foreach (PipelineOwner child in _children.ToArray())
        {
            child.Detach();
        }
    }

    // In theory, child list modifications are also disallowed between child layout and paint as well
    // as between paint and semantics. Since the flush methods are usually called back to back, this
    // gets close enough.
    private bool DebugAllowChildListModifications =>
        !_debugDoingChildLayout && !DebugDoingPaint && !DebugDoingSemantics;

    /// <summary>Adds <paramref name="child"/> to this pipeline owner.</summary>
    /// <remarks>
    /// Flutter's <c>PipelineOwner.adoptChild</c>. Each phase runs on this owner's own nodes before it
    /// runs on the children's; no assumption may be made about the order between children. No child
    /// may be added once this owner has started flushing its children, until the frame ends.
    /// <para>
    /// Dart keeps the parent link (<c>_debugParent</c>) only outside release builds, because it uses
    /// it purely for asserts. Plumix keeps it in every build mode: <see cref="DropChild"/> and
    /// <see cref="Dispose"/> validate against it, and a release-only null would make them wrong rather
    /// than merely unchecked.
    /// </para>
    /// </remarks>
    public void AdoptChild(PipelineOwner child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (child._parent is not null)
        {
            throw new AssertionError("A PipelineOwner that already has a parent cannot be adopted.");
        }

        if (_children.Contains(child))
        {
            throw new AssertionError("A PipelineOwner cannot be adopted twice by the same parent.");
        }

        if (!DebugAllowChildListModifications)
        {
            throw new AssertionError("Cannot modify child list after layout.");
        }

        _children.Add(child);
        child._parent = this;

        if (_manifold is not null)
        {
            child.Attach(_manifold);
        }
    }

    /// <summary>Removes a child pipeline owner previously added with <see cref="AdoptChild"/>.</summary>
    /// <remarks>Flutter's <c>PipelineOwner.dropChild</c>.</remarks>
    public void DropChild(PipelineOwner child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (!ReferenceEquals(child._parent, this) || !_children.Contains(child))
        {
            throw new AssertionError("A PipelineOwner can only be dropped by the parent that adopted it.");
        }

        if (!DebugAllowChildListModifications)
        {
            throw new AssertionError("Cannot modify child list after layout.");
        }

        _children.Remove(child);
        child._parent = null;

        if (_manifold is not null)
        {
            child.Detach();
        }
    }

    /// <summary>Calls <paramref name="visitor"/> for each immediate child of this pipeline owner.</summary>
    /// <remarks>Flutter's <c>PipelineOwner.visitChildren</c>.</remarks>
    public void VisitChildren(PipelineOwnerVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        foreach (PipelineOwner child in _children.ToArray())
        {
            visitor(child);
        }
    }

    /// <summary>Releases the resources held by this pipeline owner.</summary>
    /// <remarks>
    /// Flutter's <c>PipelineOwner.dispose</c>. The owner must already be out of the owner tree — no
    /// parent, no children — and detached from any <see cref="PipelineManifold"/>. It is unusable
    /// afterwards.
    /// </remarks>
    public void Dispose()
    {
        if (_children.Count > 0 || _rootNode is not null || _manifold is not null || _parent is not null)
        {
            throw new AssertionError(
                "A PipelineOwner must be removed from the pipeline owner tree and detached from its "
                + "manifold before it is disposed.");
        }

        DebugAssertions.Assert(FoundationDebug.DebugMaybeDispatchDisposed(this));
        _semanticsOwner?.Dispose();
        _semanticsOwner = null;
        _nodesNeedingLayout.Clear();
        _nodesNeedingCompositingBitsUpdate.Clear();
        _nodesNeedingPaint.Clear();
        _nodesNeedingSemantics.Clear();
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren()
    {
        return [.. _children.Select(static child => child.ToDiagnosticsNode())];
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<RenderObject>(
            "rootNode",
            RootNode,
            defaultValue: DiagnosticsDefaults.NullValue));
    }
}
