using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Foundation;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/object.dart (approximate)

namespace Plumix;

public sealed class PipelineOwner : DiagnosticableTree
{
    public RenderView Root { get; }
    public Action? OnNeedVisualUpdate { get; set; }
    internal SemanticsOwner SemanticsOwner => _semanticsOwner;
    internal OffsetLayer RootLayer => _rootLayer;

    private bool _needsLayout;
    private bool _needsCompositingBitsUpdate;
    private bool _needsPaint;
    private bool _needsSemantics;
    private readonly HashSet<RenderObject> _nodesNeedingLayout = [];
    private readonly HashSet<RenderObject> _nodesNeedingCompositingBitsUpdate = [];
    private readonly HashSet<RenderObject> _nodesNeedingPaint = [];
    private readonly HashSet<RenderObject> _nodesNeedingSemantics = [];
    private readonly HashSet<RenderObject> _nodesNeedingSemanticsGeometryUpdate = [];
    private readonly SemanticsOwner _semanticsOwner = new();
    private OffsetLayer _rootLayer = new();

    internal bool NeedsPaint => _needsPaint;

    public PipelineOwner(RenderView root)
    {
        Root = root;
        Root.ScheduleInitialPaint(_rootLayer);
    }

    public void Attach(RenderObject obj)
    {
        obj.Attach(this);
        
        obj.VisitChildren(Attach);
    }

    public void RequestLayout()
    {
        RequestLayoutFor(Root);
    }

    internal void RequestLayoutFor(RenderObject node)
    {
        if (!_nodesNeedingLayout.Add(node))
        {
            return;
        }

        _needsLayout = true;
        OnNeedVisualUpdate?.Invoke();
    }

    public void RequestCompositingBitsUpdate()
    {
        RequestCompositingBitsUpdateFor(Root);
    }

    internal void RequestCompositingBitsUpdateFor(RenderObject node)
    {
        if (!_nodesNeedingCompositingBitsUpdate.Add(node))
        {
            return;
        }

        _needsCompositingBitsUpdate = true;
        OnNeedVisualUpdate?.Invoke();
    }

    public void RequestPaint()
    {
        RequestPaintFor(Root);
    }

    internal void RequestPaintFor(RenderObject node)
    {
        if (!_nodesNeedingPaint.Add(node))
        {
            return;
        }

        _needsPaint = true;
        OnNeedVisualUpdate?.Invoke();
    }

    public void RequestSemanticsUpdate()
    {
        RequestSemanticsUpdateFor(Root);
        RequestSemanticsGeometryUpdateFor(Root);
    }

    /// <summary>Whether a semantics tree is being produced at all.</summary>
    /// <remarks>Flutter's <c>PipelineOwner.semanticsOwner != null</c>.</remarks>
    internal bool HasSemanticsOwner => true;

    internal void RequestSemanticsGeometryUpdateFor(RenderObject node)
    {
        if (!_nodesNeedingSemanticsGeometryUpdate.Add(node))
        {
            return;
        }

        _needsSemantics = true;
        OnNeedVisualUpdate?.Invoke();
    }

    internal void RequestSemanticsUpdateFor(RenderObject node)
    {
        if (!_nodesNeedingSemantics.Add(node))
        {
            return;
        }

        _needsSemantics = true;
        OnNeedVisualUpdate?.Invoke();
    }

    internal void ForgetSemanticsUpdateFor(RenderObject node)
    {
        _nodesNeedingSemantics.Remove(node);
        _needsSemantics = _nodesNeedingSemantics.Count > 0;
    }

    internal int PendingSemanticsNodeCount => _nodesNeedingSemantics.Count;

    /// <summary>
    /// Whether this pipeline owner is currently running <see cref="FlushLayout"/>. Ports Flutter's
    /// <c>PipelineOwner.debugDoingLayout</c>, which gates the layout-phase-only entry points —
    /// notably <c>RawGestureDetectorState.ReplaceGestureRecognizers</c>.
    /// </summary>
    public bool DebugDoingLayout { get; private set; }

    public void FlushLayout(Size rootSize)
    {
        if (!_needsLayout) return;

        DebugDoingLayout = true;
        try
        {
            FlushLayoutNodes(rootSize);
        }
        finally
        {
            DebugDoingLayout = false;
        }
    }

    private void FlushLayoutNodes(Size rootSize)
    {
        var constraints = new BoxConstraints(0, rootSize.Width, 0, rootSize.Height);
        
        while (_nodesNeedingLayout.Count > 0)
        {
            var dirtyNodes = _nodesNeedingLayout
                .OrderBy(static node => node.Depth)
                .ToArray();

            _nodesNeedingLayout.Clear();
            _needsLayout = false;

            foreach (var node in dirtyNodes)
            {
                if (!node.Attached || !ReferenceEquals(node.Owner, this))
                {
                    continue;
                }

                if (ReferenceEquals(node, Root))
                {
                    Root.Layout(constraints);
                    continue;
                }

                if (!node.NeedsLayoutOrDescendantNeedsLayout)
                {
                    continue;
                }

                if (!node.HasBoxConstraints)
                {
                    RequestLayout();
                    continue;
                }

                node.Layout(node.CurrentBoxConstraints);
            }
        }

        _needsLayout = false;
    }

    public void FlushCompositingBits()
    {
        if (!_needsCompositingBitsUpdate)
        {
            return;
        }

        while (_nodesNeedingCompositingBitsUpdate.Count > 0)
        {
            var dirtyNodes = _nodesNeedingCompositingBitsUpdate
                .OrderBy(static node => node.Depth)
                .ToArray();

            _nodesNeedingCompositingBitsUpdate.Clear();
            _needsCompositingBitsUpdate = false;

            foreach (var node in dirtyNodes)
            {
                if (!node.Attached || !ReferenceEquals(node.Owner, this))
                {
                    continue;
                }

                if (!node.NeedsCompositingBitsUpdate)
                {
                    continue;
                }

                node.UpdateCompositingBits();
            }
        }

        _needsCompositingBitsUpdate = false;
    }

    /// <summary>
    /// Whether this pipeline owner is currently running <see cref="FlushPaint"/>. Ports Flutter's
    /// <c>PipelineOwner.debugDoingPaint</c>, which gates paint-phase-only reads such as
    /// <see cref="RenderObject"/> geometry published to descendants.
    /// </summary>
    public bool DebugDoingPaint { get; private set; }

    public void FlushPaint()
    {
        if (!_needsPaint)
        {
            return;
        }

        DebugDoingPaint = true;
        try
        {
            FlushPaintNodes();
        }
        finally
        {
            DebugDoingPaint = false;
        }

        _needsPaint = false;
    }

    /// <summary>
    /// Samples the front-most painted system-overlay annotations at the status and navigation bars,
    /// matching Flutter's post-paint system chrome update.
    /// </summary>
    public void UpdateSystemUiOverlayStyle(Size viewportSize)
    {
        if (viewportSize.Width <= 0.0 || viewportSize.Height <= 0.0)
        {
            return;
        }

        double sampleX = viewportSize.Width / 2.0;
        SystemUiOverlayStyle? statusStyle = _rootLayer.Find<SystemUiOverlayStyle>(new Point(sampleX, 0.0));
        SystemUiOverlayStyle? navigationStyle = _rootLayer.Find<SystemUiOverlayStyle>(
            new Point(sampleX, Math.Max(0.0, viewportSize.Height - 1.0)));
        if (statusStyle is null && navigationStyle is null)
        {
            return;
        }

        SystemUiOverlayStyle current = SystemChrome.CurrentSystemUiOverlayStyle;
        SystemChrome.SetSystemUiOverlayStyle(new SystemUiOverlayStyle(
            StatusBarColor: statusStyle?.StatusBarColor ?? current.StatusBarColor,
            NavigationBarColor: navigationStyle?.NavigationBarColor ?? current.NavigationBarColor,
            StatusBarIconBrightness: statusStyle?.StatusBarIconBrightness ?? current.StatusBarIconBrightness,
            NavigationBarIconBrightness:
                navigationStyle?.NavigationBarIconBrightness ?? current.NavigationBarIconBrightness,
            StatusBarBrightness: statusStyle?.StatusBarBrightness ?? current.StatusBarBrightness));
    }

    private void FlushPaintNodes()
    {
        while (_nodesNeedingPaint.Count > 0)
        {
            var dirtyNodes = _nodesNeedingPaint
                .OrderByDescending(static node => node.Depth)
                .ToArray();

            _nodesNeedingPaint.Clear();
            _needsPaint = false;

            foreach (var node in dirtyNodes)
            {
                if (!node.Attached || !ReferenceEquals(node.Owner, this))
                {
                    continue;
                }

                if (!node.NeedsPaint && !node.NeedsCompositedLayerUpdate)
                {
                    continue;
                }

                if (!node.IsRepaintBoundary
                    || node._layer is not OffsetLayer layer
                    || (layer.Parent == null && node.Parent != null))
                {
                    node.HandleSkippedPaintingOnDetachedLayer();
                    continue;
                }

                if (node.NeedsPaint)
                {
                    if (node.NeedsCompositedLayerUpdate)
                    {
                        node.UpdateCompositedLayerProperties();
                    }

                    layer.RemoveAllChildren();
                    node._paintWithContext(new PaintingContext(layer), new Point(0, 0));
                }
                else
                {
                    node.UpdateCompositedLayerProperties();
                }
            }
        }

        _needsPaint = false;
    }

    public void CompositeFrame(DrawingContext context)
    {
        bool hasBackdropFilters = _rootLayer.ContainsBackdropFilter;
        try
        {
            if (hasBackdropFilters)
            {
                CaptureBackdropInputs();
            }

            if (!_rootLayer.ContainsMagnifier)
            {
                _rootLayer.AddToScene(context, new Point(0, 0));
                return;
            }

            Layer.BeginMagnifierBackdropCapture();
            try
            {
                BackdropCapture backdrop = CaptureScene();
                Layer.EndMagnifierBackdropCapture(backdrop);
                _rootLayer.AddToScene(context, new Point(0, 0));
            }
            finally
            {
                Layer.ClearMagnifierBackdrop();
            }
        }
        finally
        {
            if (hasBackdropFilters)
            {
                ClearBackdropInputs();
            }
        }
    }

    private void CaptureBackdropInputs()
    {
        PrepareBackdropCaptures(CaptureBackdropInput);
    }

    internal void PrepareBackdropCaptures(Func<BackdropFilterLayer, BackdropCapture> capture)
    {
        ArgumentNullException.ThrowIfNull(capture);
        var filters = new List<BackdropFilterLayer>();
        _rootLayer.CollectBackdropFilters(filters);
        var groupedBackdrops = new Dictionary<BackdropKey, BackdropCapture>();
        foreach (BackdropFilterLayer filter in filters)
        {
            if (filter.BackdropKey != null
                && groupedBackdrops.TryGetValue(filter.BackdropKey, out BackdropCapture? groupedBackdrop))
            {
                filter.Backdrop = groupedBackdrop;
                continue;
            }

            BackdropCapture backdrop = capture(filter)
                ?? throw new InvalidOperationException("Backdrop capture must return an image.");
            filter.Backdrop = backdrop;
            if (filter.BackdropKey != null)
            {
                groupedBackdrops[filter.BackdropKey] = backdrop;
            }
        }
    }

    internal void ClearBackdropInputs()
    {
        var filters = new List<BackdropFilterLayer>();
        _rootLayer.CollectBackdropFilters(filters);
        var captures = new HashSet<BackdropCapture>();
        foreach (BackdropFilterLayer filter in filters)
        {
            if (filter.Backdrop != null)
            {
                captures.Add(filter.Backdrop);
                filter.Backdrop = null;
            }
        }

        foreach (BackdropCapture capture in captures)
        {
            capture.Dispose();
        }
    }

    private BackdropCapture CaptureBackdropInput(BackdropFilterLayer filter)
    {
        Layer.BeginBackdropCapture(filter);
        try
        {
            return CaptureScene();
        }
        finally
        {
            Layer.ClearBackdropCapture();
        }
    }

    private BackdropCapture CaptureScene()
    {
        int width = Math.Max(1, (int)Math.Ceiling(Root.Size.Width));
        int height = Math.Max(1, (int)Math.Ceiling(Root.Size.Height));
        var bounds = new Rect(0.0, 0.0, width, height);
        var image = new RenderTargetBitmap(
            new PixelSize(width, height),
            new Vector(96.0, 96.0));
        try
        {
            using DrawingContext backdropContext = image.CreateDrawingContext();
            _rootLayer.AddToScene(backdropContext, new Point(0, 0));
            return new BackdropCapture(image, bounds, ownsImage: true);
        }
        catch
        {
            image.Dispose();
            throw;
        }
    }

    public void FlushSemantics()
    {
        if (!_needsSemantics)
        {
            return;
        }

        // Phase 1 and 2: rebuild the fragment tree top-down, so that one parent change invalidates
        // the subtree once instead of once per descendant.
        RenderObject[] nodesToProcess = _nodesNeedingSemantics
            .Where(node => node.Attached && ReferenceEquals(node.Owner, this) && !node.NeedsLayout)
            .OrderBy(static node => node.Depth)
            .ToArray();
        _nodesNeedingSemantics.RemoveWhere(node =>
            node.Attached && ReferenceEquals(node.Owner, this) && !node.NeedsLayout);

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
                    target.EnsureSemanticsNode(_semanticsOwner);
                }
            }
        }

        _semanticsOwner.SendSemanticsUpdate();
        _needsSemantics = _nodesNeedingSemantics.Count > 0
                          || _nodesNeedingSemanticsGeometryUpdate.Count > 0;
    }

    internal void ForgetPaintFor(RenderObject node)
    {
        _nodesNeedingPaint.Remove(node);
    }

    internal void ReplaceRootLayer(OffsetLayer rootLayer)
    {
        _rootLayer = rootLayer;
        Root.ReplaceRootLayer(rootLayer);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<RenderObject>(
            "rootNode",
            Root,
            defaultValue: DiagnosticsDefaults.NullValue));
    }

    /// <summary>A textual representation of the render tree rooted at <see cref="Root"/>.</summary>
    /// <remarks>
    /// Flutter's <c>debugDumpRenderTree()</c> prints through <c>debugPrint</c> from
    /// <c>rendering/binding.dart</c>; Plumix has neither the binding globals nor <c>debugPrint</c>, so
    /// the dump is returned instead of printed (see <c>docs/ai/DIVERGENCES.md</c>).
    /// </remarks>
    public string DebugDumpRenderTree() => Root.ToStringDeep();

    /// <summary>
    /// A deep dump of the semantics-fragment tree the compiler builds behind the render tree, one
    /// entry per render object that contributes to semantics.
    /// </summary>
    /// <remarks>
    /// Flutter's top-level <c>debugDumpRenderObjectSemanticsTree()</c>, which joins the dump of
    /// every render view's <c>_semantics</c>. Plumix owns exactly one root per pipeline.
    /// </remarks>
    public string DebugDumpRenderObjectSemanticsTree() => Root.Semantics.ToStringDeep();
}
