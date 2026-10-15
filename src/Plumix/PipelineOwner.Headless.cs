using Avalonia;
using Plumix.Rendering;
using Plumix.UI;

// C#-only infrastructure: conveniences for render trees pumped by hand (tests and host-less trees)
// on top of the strict `PipelineOwner` port in `PipelineOwner.cs`. Flutter drives the same steps
// through `RendererBinding` and `View`; none of these members has a Dart counterpart.

namespace Plumix;

public partial class PipelineOwner
{
    private OffsetLayer _rootLayer = new();

    /// <summary>
    /// The layer <see cref="CompositeFrame"/> composites: the root node's layer, which
    /// <see cref="RenderView"/> keeps in step from <c>ScheduleInitialPaint</c> and
    /// <c>ReplaceRootLayer</c>. Flutter's binding composites <c>RenderView.layer</c> directly.
    /// </summary>
    internal OffsetLayer RootLayer
    {
        get => _rootLayer;
        set => _rootLayer = value;
    }

    internal void ReplaceRootLayer(OffsetLayer rootLayer)
    {
        if (!rootLayer.Attached)
        {
            rootLayer.Attach(Root);
        }

        Root.ReplaceRootLayer(rootLayer);
        _rootLayer = rootLayer;
    }

    /// <summary>
    /// Creates a single-view pipeline for <paramref name="root"/>, for a tree pumped by hand, producing
    /// semantics unconditionally.
    /// </summary>
    /// <remarks>
    /// Plumix-only convenience constructor. Flutter builds the same thing out of a
    /// <c>PipelineOwner</c>, a <c>RenderView</c> and a <c>PipelineManifold</c> fed by
    /// <c>SemanticsBinding.semanticsEnabled</c>; this constructor takes a <see cref="SemanticsHandle"/>
    /// it never closes, so semantics stay on for the lifetime of the owner. The view is not attached
    /// here: <see cref="Attach(RenderObject)"/> sets <see cref="RootNode"/> later. The view is given a
    /// configuration from its <see cref="FlutterView"/> if it has none yet, so
    /// <see cref="RenderView.PrepareInitialFrame"/> can run without a <c>RendererBinding</c>.
    /// </remarks>
    public PipelineOwner(RenderView root)
        : this(onSemanticsUpdate: static _ => { })
    {
        ArgumentNullException.ThrowIfNull(root);
        if (!root.HasConfiguration)
        {
            root.Configuration = ViewConfiguration.FromView(root.FlutterView);
        }

        ViewSemanticsHandle = EnsureSemantics();
    }

    /// <summary>The never-closed handle the single-view constructor takes.</summary>
    internal SemanticsHandle? ViewSemanticsHandle { get; }

    /// <summary>The <see cref="RenderView"/> this owner drives.</summary>
    /// <remarks>
    /// Plumix-only shorthand for <see cref="RootNode"/> on an owner that manages a view. Flutter
    /// reaches the same object through <c>pipelineOwner.rootNode as RenderView</c>.
    /// </remarks>
    public RenderView Root => _rootNode as RenderView
        ?? throw new InvalidOperationException("This PipelineOwner is not managing a RenderView.");

    internal bool NeedsPaint => _nodesNeedingPaint.Count > 0;

    /// <summary>
    /// Makes <paramref name="obj"/> the <see cref="RootNode"/> of this owner and, for a
    /// <see cref="RenderView"/>, prepares its initial frame.
    /// </summary>
    /// <remarks>
    /// Plumix-only. Flutter's <c>_RawViewElement.mount</c> does the same two steps
    /// (<c>rootNode = renderObject</c>, <c>renderObject.prepareInitialFrame()</c>) for a view that
    /// a <c>View</c> widget publishes; tests that drive a bare render tree use this.
    /// </remarks>
    public void Attach(RenderObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        if (!ReferenceEquals(_rootNode, obj))
        {
            RootNode = obj;
        }
        else if (!obj.Attached)
        {
            obj.Attach(this);
        }

        if (obj is RenderView { HasRelayoutBoundaryState: false } view)
        {
            view.PrepareInitialFrame();
        }
    }

    /// <summary>Marks the root as needing layout through its normal relayout-boundary contract.</summary>
    /// <remarks>Plumix-only shorthand for <c>RootNode?.MarkNeedsLayout()</c>.</remarks>
    public void RequestLayout() => _rootNode?.MarkNeedsLayout();

    /// <summary>Marks the root's compositing bits as needing an update.</summary>
    /// <remarks>Plumix-only shorthand for <c>RootNode?.MarkNeedsCompositingBitsUpdate()</c>.</remarks>
    public void RequestCompositingBitsUpdate() => _rootNode?.MarkNeedsCompositingBitsUpdate();

    /// <summary>Marks the root as needing paint through its normal repaint-boundary contract.</summary>
    /// <remarks>Plumix-only shorthand for <c>RootNode?.MarkNeedsPaint()</c>.</remarks>
    public void RequestPaint() => _rootNode?.MarkNeedsPaint();

    public void RequestSemanticsUpdate()
    {
        if (_rootNode is not { } root)
        {
            return;
        }

        RequestSemanticsUpdateFor(root);
        RequestSemanticsGeometryUpdateFor(root);
    }

    /// <summary>
    /// Update the layout information for all dirty render objects, laying the <see cref="Root"/> view
    /// out under <paramref name="rootSize"/>.
    /// </summary>
    /// <remarks>
    /// The single-view entry point for trees pumped by hand. Flutter's <c>RenderView</c> reads its size
    /// from its own <c>configuration</c>, which <c>RendererBinding</c> writes from the platform view's
    /// metrics; this writes that configuration from <paramref name="rootSize"/> (at the view's device
    /// pixel ratio) first, then runs <see cref="FlushLayout()"/>.
    /// </remarks>
    public void FlushLayout(Size rootSize)
    {
        using EventLoop.FrameworkThreadScope scope = EventLoop.EnterFrameworkThread();
        if (_rootNode is RenderView configuredView)
        {
            BoxConstraints logicalConstraints = new BoxConstraints(0, rootSize.Width, 0, rootSize.Height);
            double devicePixelRatio = configuredView.FlutterView.DevicePixelRatio;
            configuredView.Configuration = new ViewConfiguration(
                physicalConstraints: logicalConstraints * devicePixelRatio,
                logicalConstraints: logicalConstraints,
                devicePixelRatio: devicePixelRatio);
        }

        FlushLayout();
    }

    /// <summary>
    /// Composites the layer tree into a scene that nothing renders: runs every layer's composition-time
    /// work (<see cref="FollowerLayer"/> transforms, composition callbacks, dirty flags and retained
    /// engine layers) the way <see cref="RenderView.CompositeFrame"/> does.
    /// </summary>
    /// <remarks>
    /// Plumix-only. Flutter's <c>RendererBinding.drawFrame</c> composites every registered <c>RenderView</c>
    /// after flushing paint; this is that composite for tests and host-less trees that pump the pipeline
    /// by hand: it builds this owner's root layer into a scene and disposes it, then does the same for
    /// its child owners (one per nested <c>View</c>).
    /// </remarks>
    public void CompositeFrame()
    {
        using EventLoop.FrameworkThreadScope scope = EventLoop.EnterFrameworkThread();
        try
        {
            CompositeFrameHeadless();
        }
        finally
        {
            RenderingDebug.AdvanceRepaintColorForFrame();
            if (SchedulerBinding.Instance.SchedulerPhase == SchedulerPhase.Idle)
            {
                EventLoop.FlushMicrotasks();
            }
        }
    }

    private void CompositeFrameHeadless()
    {
        _rootLayer.BuildScene(new SceneBuilder()).Dispose();
        foreach (PipelineOwner child in _children.ToArray())
        {
            child.CompositeFrameHeadless();
        }
    }


    /// <summary>A textual representation of the render tree rooted at <see cref="RootNode"/>.</summary>
    /// <remarks>
    /// A single-root convenience helper. For Flutter's binding-wide printing function, use
    /// <see cref="RenderingDebug.DebugDumpRenderTree"/>.
    /// </remarks>
    public string DebugDumpRenderTree() => _rootNode?.ToStringDeep() ?? string.Empty;

    /// <summary>
    /// A deep dump of the semantics-fragment tree the compiler builds behind the render tree, one
    /// entry per render object that contributes to semantics.
    /// </summary>
    /// <remarks>
    /// A single-root convenience helper. For Flutter's binding-wide printing function, use
    /// <see cref="RenderingDebug.DebugDumpRenderObjectSemanticsTree"/>.
    /// </remarks>
    public string DebugDumpRenderObjectSemanticsTree() => _rootNode?.Semantics.ToStringDeep() ?? string.Empty;
}
