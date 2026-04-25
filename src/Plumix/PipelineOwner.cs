using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/object.dart (approximate)

namespace Plumix;

public sealed class PipelineOwner
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

    public void FlushLayout(Size rootSize)
    {
        if (!_needsLayout) return;

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

    public void FlushPaint()
    {
        if (!_needsPaint)
        {
            return;
        }

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
        _rootLayer.AddToScene(context, new Point(0, 0));
    }

    public void FlushSemantics()
    {
        if (!_needsSemantics)
        {
            return;
        }

        while (_nodesNeedingSemantics.Count > 0)
        {
            var nodesToProcess = _nodesNeedingSemantics
                .Where(node =>
                    node.Attached
                    && ReferenceEquals(node.Owner, this)
                    && !node.NeedsLayout)
                .OrderBy(static node => node.Depth)
                .ToArray();

            var deferredNodes = _nodesNeedingSemantics
                .Where(node =>
                    node.Attached
                    && ReferenceEquals(node.Owner, this)
                    && node.NeedsLayout)
                .ToArray();

            _nodesNeedingSemantics.Clear();
            foreach (var deferred in deferredNodes)
            {
                _nodesNeedingSemantics.Add(deferred);
            }

            _needsSemantics = _nodesNeedingSemantics.Count > 0;

            if (nodesToProcess.Length == 0)
            {
                break;
            }

            // Phase 1/2: rebuild semantics fragments and propagate parent-data context.
            foreach (var node in nodesToProcess)
            {
                if (node.Parent != null && node.SemanticsParentDataDirty)
                {
                    continue;
                }

                node.UpdateSemanticsChildrenFromCachedParentData();
            }

            // Phase 3: compute semantics geometry with final transforms/clips.
            foreach (var node in nodesToProcess)
            {
                if (node.Parent != null && node.SemanticsParentDataDirty)
                {
                    continue;
                }

                node.EnsureSemanticsGeometry();
            }

            // Phase 4: compile dirty semantics boundaries in reverse depth order first.
            var scratchOutput = new List<SemanticsNode>();
            foreach (var node in nodesToProcess.Reverse())
            {
                if (node.Parent != null && node.SemanticsParentDataDirty)
                {
                    continue;
                }

                scratchOutput.Clear();
                node.EnsureSemanticsNode(_semanticsOwner, scratchOutput);
            }

            // Compile the final semantics root from staged data.
            var compiledRoots = new List<SemanticsNode>();
            Root.EnsureSemanticsNode(_semanticsOwner, compiledRoots);
            _semanticsOwner.UpdateRoot(compiledRoots);
        }

        _needsSemantics = _nodesNeedingSemantics.Count > 0;
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
}
