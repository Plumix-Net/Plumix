using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart (host integration extras)

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
public sealed class PipelineOwner : DiagnosticableTree
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
    }

    /// <summary>
    /// Creates the single-view pipeline a Plumix host drives, rooted at <paramref name="root"/> and
    /// producing semantics unconditionally.
    /// </summary>
    /// <remarks>
    /// Plumix-only convenience constructor. Flutter builds the same thing out of a
    /// <c>PipelineOwner</c>, a <c>RenderView</c> and a <c>PipelineManifold</c> fed by
    /// <c>SemanticsBinding.semanticsEnabled</c>; no Plumix host reports platform accessibility state
    /// yet (see <c>docs/ai/BACKLOG.md</c>), so this constructor takes a <see cref="SemanticsHandle"/>
    /// it never closes and semantics stay on for the lifetime of the owner.
    /// </remarks>
    public PipelineOwner(RenderView root)
        : this(onSemanticsUpdate: static _ => { })
    {
        ArgumentNullException.ThrowIfNull(root);

        // Deliberately not through the `RootNode` setter: Plumix hosts and tests attach the root in a
        // second step (`Attach(RenderObject)`), and `RenderObject.Attach` rejects a second attach.
        _rootNode = root;
        root.ScheduleInitialPaint(_rootLayer);
        ViewSemanticsHandle = EnsureSemantics();
    }

    /// <summary>The never-closed handle the single-view constructor takes.</summary>
    internal SemanticsHandle? ViewSemanticsHandle { get; }

    /// <summary>The <see cref="RenderView"/> this owner drives.</summary>
    /// <remarks>
    /// Plumix-only shorthand for <see cref="RootNode"/> on an owner built with the single-view
    /// constructor. Flutter reaches the same object through <c>pipelineOwner.rootNode as RenderView</c>.
    /// </remarks>
    public RenderView Root => (RenderView)_rootNode!;

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
    public RenderObject? RootNode
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

    internal OffsetLayer RootLayer => _rootLayer;

    private bool _needsLayout;
    private bool _needsCompositingBitsUpdate;
    private bool _needsPaint;
    private bool _needsSemantics;
    private readonly HashSet<RenderObject> _nodesNeedingLayout = [];
    private bool _shouldMergeDirtyNodes;
    private readonly HashSet<RenderObject> _nodesNeedingCompositingBitsUpdate = [];
    private readonly HashSet<RenderObject> _nodesNeedingPaint = [];
    private readonly HashSet<RenderObject> _nodesNeedingSemantics = [];
    private readonly HashSet<RenderObject> _nodesNeedingSemanticsGeometryUpdate = [];
    private SemanticsOwner? _semanticsOwner;
    private int _outstandingSemanticsHandles;
    private OffsetLayer _rootLayer = new();

    // TREE MANAGEMENT
    private readonly List<PipelineOwner> _children = [];
    private PipelineManifold? _manifold;
    private PipelineOwner? _parent;
    private bool _debugDoingChildLayout;

    internal bool NeedsPaint => _needsPaint;

    /// <summary>The render objects queued for the next layout pass.</summary>
    /// <remarks>
    /// Flutter's <c>PipelineOwner.nodesNeedingLayout</c>, a <c>@protected</c> getter subclasses use to
    /// inspect the dirty list. <see cref="PipelineOwner"/> is sealed here, so it is internal instead.
    /// </remarks>
    internal IReadOnlyCollection<RenderObject> NodesNeedingLayoutForTest => _nodesNeedingLayout;

    /// <summary>The render objects queued for the next paint pass.</summary>
    /// <remarks>Flutter's <c>PipelineOwner.nodesNeedingPaint</c>.</remarks>
    internal IReadOnlyCollection<RenderObject> NodesNeedingPaintForTest => _nodesNeedingPaint;

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

    public void Attach(RenderObject obj)
    {
        obj.Attach(this);

        if (ReferenceEquals(obj, _rootNode) && obj is RenderView { HasRelayoutBoundaryState: false } view)
        {
            // Dart's `RenderView.prepareInitialFrame` runs `scheduleInitialLayout` right after the
            // root is attached; without it the root never enters the owner's dirty list, because
            // `RenderObject.attach` deliberately skips a node that has never been laid out.
            view.ScheduleInitialLayout();
        }
    }

    /// <summary>Marks the whole render tree as needing layout.</summary>
    /// <remarks>
    /// Dart has no equivalent: its root enters the dirty list through <c>scheduleInitialLayout</c> and
    /// stays there via <c>markNeedsLayout</c>. Hosts and tests use this to force a full pass, so it
    /// dirties the root itself rather than only enqueueing it — enqueueing alone would be undone by
    /// the unchanged-constraints early-out in <see cref="RenderObject.Layout"/>.
    /// </remarks>
    public void RequestLayout()
    {
        if (_rootNode is not { } root)
        {
            return;
        }

        root.MarkNeedsLayout();
        RequestLayoutFor(root);
    }

    internal void RequestLayoutFor(RenderObject node)
    {
        if (!_nodesNeedingLayout.Add(node))
        {
            return;
        }

        _needsLayout = true;
        RequestVisualUpdate();
    }

    public void RequestCompositingBitsUpdate()
    {
        if (_rootNode is { } root)
        {
            RequestCompositingBitsUpdateFor(root);
        }
    }

    internal void RequestCompositingBitsUpdateFor(RenderObject node)
    {
        if (!_nodesNeedingCompositingBitsUpdate.Add(node))
        {
            return;
        }

        _needsCompositingBitsUpdate = true;
        RequestVisualUpdate();
    }

    public void RequestPaint()
    {
        if (_rootNode is { } root)
        {
            RequestPaintFor(root);
        }
    }

    internal void RequestPaintFor(RenderObject node)
    {
        if (!_nodesNeedingPaint.Add(node))
        {
            return;
        }

        _needsPaint = true;
        RequestVisualUpdate();
    }

    public void RequestSemanticsUpdate()
    {
        if (_rootNode is not { } root)
        {
            return;
        }

        RequestSemanticsUpdateFor(root);
        RequestSemanticsGeometryUpdateFor(root);
    }

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
        if (listener is not null)
        {
            _semanticsOwner!.AddListener(listener);
        }

        return new SemanticsHandle(() =>
        {
            if (listener is not null)
            {
                _semanticsOwner!.RemoveListener(listener);
            }

            DidDisposeSemanticsHandle();
        });
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
        FlushLayoutCore(null);
        FlushMicrotasksOutsideFrame();
    }

    /// <summary>
    /// Update the layout information for all dirty render objects, laying the <see cref="Root"/> view
    /// out under <paramref name="rootSize"/>.
    /// </summary>
    /// <remarks>
    /// The single-view entry point Plumix hosts call. Flutter's <c>RenderView</c> reads its size from
    /// its own <c>configuration</c>, so <c>flushLayout</c> takes no argument there.
    /// </remarks>
    public void FlushLayout(Size rootSize)
    {
        FlushLayoutCore(rootSize);
        FlushMicrotasksOutsideFrame();
    }

    /// <summary>
    /// Layout builds widgets (<c>LayoutBuilder</c> and friends), and those builds queue microtasks —
    /// an autofocus request, for example. Inside a frame the scheduler drains the queue once the frame
    /// ends; when the pipeline is driven directly, the event-loop turn ends here instead.
    /// </summary>
    private static void FlushMicrotasksOutsideFrame()
    {
        if (Scheduler.Phase == SchedulerPhase.Idle)
        {
            Scheduler.FlushMicrotasks();
        }
    }

    private void FlushLayoutCore(Size? rootSize)
    {
        if (rootSize is { } viewSize && _rootNode is RenderView configuredView)
        {
            // Flutter's `RendererBinding` writes the `ViewConfiguration` onto the `RenderView`
            // whenever the platform view's metrics change; Plumix hosts hand the size to the frame
            // instead, so the owner keeps the configuration in step here.
            BoxConstraints logicalConstraints = new BoxConstraints(0, viewSize.Width, 0, viewSize.Height);
            double devicePixelRatio = configuredView.FlutterView?.DevicePixelRatio
                ?? (configuredView.HasConfiguration ? configuredView.Configuration.DevicePixelRatio : 1.0);
            configuredView.Configuration = new ViewConfiguration(
                physicalConstraints: logicalConstraints * devicePixelRatio,
                logicalConstraints: logicalConstraints,
                devicePixelRatio: devicePixelRatio);
        }

        DebugDoingLayout = true;
        try
        {
            if (_needsLayout)
            {
                FlushLayoutNodes(rootSize);
            }

            _debugDoingChildLayout = true;
            foreach (PipelineOwner child in _children.ToArray())
            {
                child.FlushLayoutCore(rootSize);
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
        }
    }

    private void FlushLayoutNodes(Size? rootSize)
    {
        BoxConstraints? constraints = rootSize is null
            ? null
            : _rootNode is RenderView { HasConfiguration: true } configuredRootView
                ? configuredRootView.Configuration.LogicalConstraints
                : new BoxConstraints(0, rootSize.Value.Width, 0, rootSize.Value.Height);
        _shouldMergeDirtyNodes = false;

        while (_nodesNeedingLayout.Count > 0)
        {
            List<RenderObject> dirtyNodes = [.. _nodesNeedingLayout.OrderBy(static node => node.Depth)];
            _nodesNeedingLayout.Clear();
            _needsLayout = false;

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

                if (constraints is { } rootConstraints
                    && ReferenceEquals(node, _rootNode)
                    && node is RenderView rootView)
                {
                    rootView.Layout(rootConstraints);
                    continue;
                }

                if (!node.NeedsLayout)
                {
                    continue;
                }

                if (node.IsRelayoutBoundary || node is IRenderObjectWithLayoutCallback)
                {
                    node.LayoutWithoutResize();
                }
                else if (node.HasBoxConstraints)
                {
                    node.Layout(node.CurrentBoxConstraints);
                }
                else
                {
                    RequestLayout();
                }
            }

            _shouldMergeDirtyNodes = false;
        }

        _shouldMergeDirtyNodes = false;
        _needsLayout = false;
    }

    /// <summary>Updates the <see cref="RenderObject.NeedsCompositing"/> bits.</summary>
    /// <remarks>Flutter's <c>PipelineOwner.flushCompositingBits</c>.</remarks>
    public void FlushCompositingBits()
    {
        if (_needsCompositingBitsUpdate)
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
    }

    private void FlushCompositingBitsNodes()
    {
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

    /// <summary>
    /// Whether this pipeline owner is currently running <see cref="FlushSemantics"/>.
    /// </summary>
    /// <remarks>Flutter's <c>PipelineOwner._debugDoingSemantics</c>.</remarks>
    public bool DebugDoingSemantics { get; private set; }

    public void FlushPaint()
    {
        DebugDoingPaint = true;
        try
        {
            if (_needsPaint || _rootNode is { NeedsPaint: true })
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
        while (_nodesNeedingPaint.Count > 0 || _rootNode is { NeedsPaint: true })
        {
            List<RenderObject> dirtyNodes =
                [.. _nodesNeedingPaint.OrderByDescending(static node => node.Depth)];

            // Flutter's `markNeedsPaint` never enqueues the root — "the root is always told to paint
            // regardless" — so the root is appended here (last, because the list is deepest-first)
            // instead of relying on it having registered itself.
            if (_rootNode is { NeedsPaint: true } root && !dirtyNodes.Contains(root))
            {
                dirtyNodes.Add(root);
            }

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

                Debug.Assert(node._layer is not null);
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

            RenderingDebug.AdvanceRepaintColorForFrame();
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

    /// <summary>Compiles the semantics for the render objects marked as needing a semantics update.</summary>
    /// <remarks>Flutter's <c>PipelineOwner.flushSemantics</c>.</remarks>
    public void FlushSemantics()
    {
        if (_semanticsOwner is null)
        {
            return;
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
                    target.EnsureSemanticsNode(_semanticsOwner!);
                }
            }
        }

        _semanticsOwner!.SendSemanticsUpdate();
        _needsSemantics = _nodesNeedingSemantics.Count > 0
                          || _nodesNeedingSemanticsGeometryUpdate.Count > 0;
    }

    internal void ForgetPaintFor(RenderObject node)
    {
        _nodesNeedingPaint.Remove(node);
    }

    internal void ReplaceRootLayer(OffsetLayer rootLayer)
    {
        Root.ReplaceRootLayer(rootLayer);
        _rootLayer = rootLayer;
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

    /// <summary>A textual representation of the render tree rooted at <see cref="RootNode"/>.</summary>
    /// <remarks>
    /// Flutter's <c>debugDumpRenderTree()</c> prints through <c>debugPrint</c> from
    /// <c>rendering/binding.dart</c>; Plumix has neither the binding globals nor <c>debugPrint</c>, so
    /// the dump is returned instead of printed (see <c>docs/ai/DIVERGENCES.md</c>).
    /// </remarks>
    public string DebugDumpRenderTree() => _rootNode?.ToStringDeep() ?? string.Empty;

    /// <summary>
    /// A deep dump of the semantics-fragment tree the compiler builds behind the render tree, one
    /// entry per render object that contributes to semantics.
    /// </summary>
    /// <remarks>
    /// Flutter's top-level <c>debugDumpRenderObjectSemanticsTree()</c>, which joins the dump of
    /// every render view's <c>_semantics</c>. Plumix owns exactly one root per pipeline.
    /// </remarks>
    public string DebugDumpRenderObjectSemanticsTree() => _rootNode?.Semantics.ToStringDeep() ?? string.Empty;
}
