using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/binding.dart (adapted)
// View registry and root pipeline owner only; the frame flow stays in the hosts (docs/ai/DIVERGENCES.md).

namespace Plumix;

/// <summary>
/// The glue between the render tree and the platform: the root of the <see cref="PipelineOwner"/>
/// tree and the registry of <see cref="RenderView"/>s that are currently on screen.
/// </summary>
/// <remarks>
/// <para>
/// Flutter's <c>RendererBinding</c>, limited to what the widget layer's <c>View</c> machinery
/// talks to: <see cref="RootPipelineOwner"/>, <see cref="RenderViews"/>, <see cref="AddRenderView"/>
/// / <see cref="RemoveRenderView"/>, <see cref="CreateViewConfigurationFor"/> and
/// <see cref="HandleMetricsChanged"/>. Flutter's binding also drives the frame
/// (<c>drawFrame</c> flushes the root owner and composites every view); Plumix's hosts do that per
/// host, because each host renders through its own Avalonia control — see
/// <c>docs/ai/DIVERGENCES.md</c>.
/// </para>
/// <para>
/// One binding serves every host in the process, as one Flutter binding serves every view.
/// </para>
/// </remarks>
public sealed class RendererBinding
{
    private readonly Dictionary<int, RenderView> _viewIdToRenderView = [];
    private readonly HostPipelineManifold _manifold;

    private RendererBinding()
    {
        // Dart's `_BindingPipelineManifold` forwards `requestVisualUpdate` to `ensureVisualUpdate`
        // and `semanticsEnabled` to the semantics binding. Plumix has no semantics binding yet: the
        // hosts force semantics on through their own pipeline owner, so views that are not backed by
        // a host start with semantics off until `SetSemanticsEnabled` is called.
        _manifold = new HostPipelineManifold(onNeedVisualUpdate: Scheduler.EnsureVisualUpdate);
        RootPipelineOwner = CreateRootPipelineOwner();
        RootPipelineOwner.Attach(_manifold);
    }

    /// <summary>The ambient renderer binding.</summary>
    /// <remarks>Flutter's <c>RendererBinding.instance</c>.</remarks>
    public static RendererBinding Instance { get; } = new();

    /// <summary>
    /// The <see cref="PipelineOwner"/> that is the root of the pipeline owner tree.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RendererBinding.rootPipelineOwner</c>. It manages no root node of its own; the
    /// owners that <c>View</c> widgets create are adopted as its children.
    /// </remarks>
    public PipelineOwner RootPipelineOwner { get; }

    /// <summary>The <see cref="RenderView"/>s managed by this binding, in registration order.</summary>
    /// <remarks>Flutter's <c>RendererBinding.renderViews</c>.</remarks>
    public IEnumerable<RenderView> RenderViews => _viewIdToRenderView.Values;

    /// <summary>
    /// Whether the pipeline owners attached to <see cref="RootPipelineOwner"/> produce semantics.
    /// </summary>
    /// <remarks>
    /// Plumix-only: Flutter reads this from <c>SemanticsBinding.semanticsEnabled</c>, which the
    /// platform's accessibility state or an open <c>SemanticsHandle</c> turns on.
    /// </remarks>
    public bool SemanticsEnabled => _manifold.SemanticsEnabled;

    /// <summary>Turns semantics on or off for every owner under <see cref="RootPipelineOwner"/>.</summary>
    /// <remarks>Plumix-only; see <see cref="SemanticsEnabled"/>.</remarks>
    public void SetSemanticsEnabled(bool value) => _manifold.SetSemanticsEnabled(value);

    /// <summary>Creates the <see cref="PipelineOwner"/> that becomes <see cref="RootPipelineOwner"/>.</summary>
    /// <remarks>Flutter's <c>RendererBinding.createRootPipelineOwner</c>.</remarks>
    private static PipelineOwner CreateRootPipelineOwner() => new DefaultRootPipelineOwner();

    /// <summary>
    /// Adds a <see cref="RenderView"/> to this binding, so that it is configured from its view's
    /// metrics and kept in step with them by <see cref="HandleMetricsChanged"/>.
    /// </summary>
    /// <remarks>Flutter's <c>RendererBinding.addRenderView</c>.</remarks>
    public void AddRenderView(RenderView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        int viewId = view.FlutterView.ViewId;
        if (_viewIdToRenderView.ContainsValue(view))
        {
            throw new AssertionError("The RenderView has already been added to the RendererBinding.");
        }

        if (_viewIdToRenderView.ContainsKey(viewId))
        {
            throw new AssertionError($"A RenderView for view {viewId} has already been added to the RendererBinding.");
        }

        _viewIdToRenderView[viewId] = view;
        view.Configuration = CreateViewConfigurationFor(view);
    }

    /// <summary>Removes a <see cref="RenderView"/> previously added with <see cref="AddRenderView"/>.</summary>
    /// <remarks>Flutter's <c>RendererBinding.removeRenderView</c>.</remarks>
    public void RemoveRenderView(RenderView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        int viewId = view.FlutterView.ViewId;
        if (!_viewIdToRenderView.TryGetValue(viewId, out RenderView? registered) || !ReferenceEquals(registered, view))
        {
            throw new AssertionError("The RenderView was not added to the RendererBinding.");
        }

        _viewIdToRenderView.Remove(viewId);
    }

    /// <summary>Returns a <see cref="ViewConfiguration"/> configured for <paramref name="renderView"/>.</summary>
    /// <remarks>Flutter's <c>RendererBinding.createViewConfigurationFor</c>.</remarks>
    public ViewConfiguration CreateViewConfigurationFor(RenderView renderView)
    {
        ArgumentNullException.ThrowIfNull(renderView);
        return ViewConfiguration.FromView(renderView.FlutterView);
    }

    /// <summary>
    /// Called when the platform's view metrics changed: every registered <see cref="RenderView"/>
    /// gets a fresh configuration, and a frame is scheduled if any of them has content.
    /// </summary>
    /// <remarks>Flutter's <c>RendererBinding.handleMetricsChanged</c>.</remarks>
    public void HandleMetricsChanged()
    {
        bool forceFrame = false;
        foreach (RenderView view in _viewIdToRenderView.Values.ToArray())
        {
            forceFrame = forceFrame || view.Child is not null;
            view.Configuration = CreateViewConfigurationFor(view);
        }

        if (forceFrame)
        {
            Scheduler.EnsureVisualUpdate();
        }
    }

    /// <summary>Flutter's <c>_DefaultRootPipelineOwner</c>: a root owner that refuses a root node.</summary>
    private sealed class DefaultRootPipelineOwner : PipelineOwner
    {
        public DefaultRootPipelineOwner()
            : base(onSemanticsUpdate: static _ => throw new AssertionError(
                "The default root pipeline owner does not produce semantics."))
        {
        }

        public override RenderObject? RootNode
        {
            get => base.RootNode;
            set => throw new FlutterError(
            [
                new ErrorSummary("Cannot set a rootNode on the default root pipeline owner."),
                new ErrorDescription(
                    "By default, the RendererBinding.rootPipelineOwner is not configured to manage a "
                    + "root node because this pipeline owner does not define a proper onSemanticsUpdate "
                    + "callback to handle semantics for that node."),
                new ErrorHint(
                    "Typically, the root pipeline owner does not manage a root node. Instead, properly "
                    + "configured child pipeline owners (which do manage root nodes) are added to it. "
                    + "Alternatively, if you do want to set a root node for the root pipeline owner, "
                    + "override RendererBinding.createRootPipelineOwner to create a pipeline owner that "
                    + "is configured to properly handle semantics for the provided root node."),
            ]);
        }
    }
}

/// <summary>
/// The <see cref="RenderView"/> behind a host's own pipeline: it survives the <c>View</c> widget
/// that publishes it, so the host can attach another root widget later.
/// </summary>
/// <remarks>
/// Flutter's <c>_ReusableRenderView</c>, which backs the deprecated <c>RendererBinding.renderView</c>
/// the implicit <c>View</c> is handed through
/// <c>View.deprecatedDoNotUseWillBeRemovedWithoutNoticeRenderView</c>. Plumix's hosts each own one.
/// </remarks>
public sealed class ReusableRenderView : RenderView
{
    private bool _initialFramePrepared;

    /// <summary>Creates a reusable render view for <paramref name="view"/>.</summary>
    public ReusableRenderView(FlutterView view) : base(view)
    {
    }

    /// <inheritdoc />
    public override void PrepareInitialFrame()
    {
        if (_initialFramePrepared)
        {
            return;
        }

        base.PrepareInitialFrame();
        _initialFramePrepared = true;
    }

    /// <inheritdoc />
    public override void ScheduleInitialSemantics()
    {
        ClearSemantics();
        base.ScheduleInitialSemantics();
    }

    /// <inheritdoc />
    /// <remarks>Deliberately does not dispose the view itself: it is reused by the next root widget.</remarks>
    public override void Dispose()
    {
        Child = null;
    }
}
