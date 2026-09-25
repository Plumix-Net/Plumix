using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/binding.dart

namespace Plumix.Widgets;

/// <summary>
/// The widget-tree ownership and application bootstrap half of Flutter's <c>WidgetsBinding</c>.
/// </summary>
public partial class WidgetsBinding
{
    private BuildOwner _buildOwner;
    private RootElement? _rootElement;
    private PipelineOwner? _implicitPipelineOwner;
    private RenderView? _implicitRenderView;
    private bool _readyToProduceFrames;

    /// <summary>Creates a widget binding.</summary>
    public WidgetsBinding()
    {
        _buildOwner = CreateBuildOwner();
    }

    /// <summary>The owner of the binding's widget tree.</summary>
    /// <remarks>Flutter's <c>WidgetsBinding.buildOwner</c>.</remarks>
    public BuildOwner BuildOwner => _buildOwner;

    /// <summary>The focus manager owned by the binding's current <see cref="BuildOwner"/>.</summary>
    /// <remarks>Flutter's <c>WidgetsBinding.focusManager</c>.</remarks>
    public FocusManager FocusManager => _buildOwner.FocusManager;

    /// <summary>The root of the binding's widget tree, or <see langword="null"/> before attachment.</summary>
    /// <remarks>Flutter's <c>WidgetsBinding.rootElement</c>.</remarks>
    public Element? RootElement => _rootElement;

    /// <summary>The legacy spelling of <see cref="RootElement"/>.</summary>
    [Obsolete("Use RootElement instead.")]
    public Element? RenderViewElement => RootElement;

    /// <summary>Whether a root widget has been attached to this binding.</summary>
    public bool IsRootWidgetAttached => _rootElement is not null;

    /// <summary>Whether scheduler frames may currently produce the widget tree.</summary>
    public bool FramesEnabled => Scheduler.FramesEnabled && _readyToProduceFrames;

    /// <summary>Wraps an application in the platform's implicit <see cref="View"/>.</summary>
    /// <remarks>Flutter's <c>WidgetsBinding.wrapWithDefaultView</c>.</remarks>
    public Widget WrapWithDefaultView(Widget rootWidget)
    {
        ArgumentNullException.ThrowIfNull(rootWidget);
        FlutterView? implicitView = PlatformDispatcher.Instance.ImplicitView;
        if (implicitView is null || _implicitPipelineOwner is null || _implicitRenderView is null)
        {
            throw new InvalidOperationException(
                "RunApp requires an implicit platform view. Use RunWidget with an explicit View "
                + "when the platform has no implicit view.");
        }

        return new View(
            view: implicitView,
            child: rootWidget,
            deprecatedDoNotUseWillBeRemovedWithoutNoticePipelineOwner: _implicitPipelineOwner,
            deprecatedDoNotUseWillBeRemovedWithoutNoticeRenderView: _implicitRenderView);
    }

    /// <summary>Queues a root attachment for the next event-loop turn.</summary>
    /// <remarks>Flutter's <c>WidgetsBinding.scheduleAttachRootWidget</c>.</remarks>
    internal void ScheduleAttachRootWidget(Widget rootWidget)
    {
        ArgumentNullException.ThrowIfNull(rootWidget);
        PlatformDispatcher.Instance.TimerRun(() => AttachRootWidget(rootWidget));
    }

    /// <summary>Attaches <paramref name="rootWidget"/> below a debug-labelled <see cref="RootWidget"/>.</summary>
    /// <remarks>Flutter's <c>WidgetsBinding.attachRootWidget</c>.</remarks>
    public void AttachRootWidget(Widget rootWidget)
    {
        ArgumentNullException.ThrowIfNull(rootWidget);
        AttachToBuildOwner(new RootWidget(
            child: rootWidget,
            debugShortDescription: "[root]"));
    }

    /// <summary>Creates or updates the binding-owned root element.</summary>
    /// <remarks>Flutter's <c>WidgetsBinding.attachToBuildOwner</c>.</remarks>
    public void AttachToBuildOwner(RootWidget widget)
    {
        ArgumentNullException.ThrowIfNull(widget);
        EnsureFrameCallback();
        bool isBootstrapFrame = _rootElement is null;
        _readyToProduceFrames = true;
        _rootElement = widget.Attach(_buildOwner, _rootElement);
        if (isBootstrapFrame)
        {
            Scheduler.EnsureVisualUpdate();
        }
    }

    /// <summary>
    /// Pumps the build and rendering pipeline to generate a frame: rebuilds the dirty widgets, runs
    /// <see cref="RendererBinding.DrawFrame"/>, then finalizes the element tree.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.drawFrame</c>, the override of <c>RendererBinding.drawFrame</c> that
    /// the renderer binding's persistent frame callback reaches. The first-frame reporting half
    /// (<c>firstFrameRasterized</c>, the <c>Flutter.FirstFrame</c> event) is not ported; see
    /// <c>docs/ai/BACKLOG.md</c>.
    /// </remarks>
    public void DrawFrame()
    {
        DebugAssertions.Assert(!DebugBuildingDirtyElements);
        if (Constants.KDebugMode)
        {
            DebugBuildingDirtyElements = true;
        }

        try
        {
            BuildDirtyWidgets();
            RendererBinding.Instance.DrawFrame();
            FinalizeTree();
        }
        finally
        {
            if (Constants.KDebugMode)
            {
                DebugBuildingDirtyElements = false;
            }
        }
    }

    /// <summary>
    /// Whether the binding is currently in a frame. Used to verify that frames are not scheduled
    /// redundantly; public so that test frameworks can change it. Not used in release builds.
    /// </summary>
    /// <remarks>Flutter's <c>WidgetsBinding.debugBuildingDirtyElements</c>.</remarks>
    public bool DebugBuildingDirtyElements { get; set; }

    /// <summary>Rebuilds the dirty widgets of the binding-owned tree.</summary>
    /// <remarks>
    /// The <c>buildOwner.buildScope(rootElement)</c> step of <see cref="DrawFrame"/>. A host also runs
    /// it right before a layout pass it drives outside a frame (Avalonia's render pass).
    /// </remarks>
    internal void BuildDirtyWidgets()
    {
        if (_rootElement is not null)
        {
            _buildOwner.BuildScope(_rootElement);
        }
    }

    /// <summary>Finalizes inactive elements after the host has flushed its render pipeline.</summary>
    internal void FinalizeTree()
    {
        if (_rootElement is not null)
        {
            _buildOwner.FinalizeTree();
        }
    }

    /// <summary>Reassembles the binding-owned widget tree after a hot reload.</summary>
    internal void ReassembleApplication()
    {
        if (_rootElement is not null)
        {
            _buildOwner.Reassemble(_rootElement);
        }
    }

    /// <summary>Installs the host resources used by <see cref="WrapWithDefaultView"/>.</summary>
    internal void SetImplicitView(
        FlutterView view,
        PipelineOwner pipelineOwner,
        RenderView renderView,
        MediaQueryData platformData)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(pipelineOwner);
        ArgumentNullException.ThrowIfNull(renderView);
        if (!ReferenceEquals(renderView.FlutterView, view))
        {
            throw new AssertionError("The implicit render view must render into the implicit platform view.");
        }

        _implicitPipelineOwner = pipelineOwner;
        _implicitRenderView = renderView;
        PlatformDispatcher.Instance.SetImplicitView(view, platformData);
    }

    /// <summary>Synchronously updates the root for the legacy <c>WidgetHost.RootWidget</c> adapter.</summary>
    internal void AttachRootWidgetSynchronously(Widget rootWidget)
    {
        AttachRootWidget(rootWidget);
        BuildDirtyWidgets();
        FinalizeTree();
    }

    /// <summary>Whether <paramref name="view"/> is the platform's current implicit view.</summary>
    internal static bool IsImplicitView(FlutterView view) =>
        ReferenceEquals(PlatformDispatcher.Instance.ImplicitView, view);

    /// <summary>Resets the process root between isolated widget-binding tests.</summary>
    internal void ResetRootForTests()
    {
        _rootElement?.UnmountRoot();
        _buildOwner.FocusManager.Dispose();
        _rootElement = null;
        _readyToProduceFrames = false;
        _implicitPipelineOwner = null;
        _implicitRenderView = null;
        _buildOwner = CreateBuildOwner();
        PlatformDispatcher.Instance.ClearImplicitViewForTests();
    }

    private BuildOwner CreateBuildOwner()
    {
        var owner = new BuildOwner();
        owner.OnBuildScheduled = HandleBuildScheduled;
        return owner;
    }

    private void HandleBuildScheduled()
    {
        // If we're in the process of building dirty elements, then changes should not trigger a new
        // frame.
        if (Constants.KDebugMode && DebugBuildingDirtyElements)
        {
            throw new FlutterError(
            [
                new ErrorSummary("Build scheduled during frame."),
                new ErrorDescription(
                    "While the widget tree was being built, laid out, and painted, "
                    + "a new frame was scheduled to rebuild the widget tree."),
                new ErrorHint(
                    "This might be because setState() was called from a layout or "
                    + "paint callback. "
                    + "If a change is needed to the widget tree, it should be applied "
                    + "as the tree is being built. Scheduling a change for the subsequent "
                    + "frame instead results in an interface that lags behind by one frame. "
                    + "If this was done to make your build dependent on a size measured at "
                    + "layout time, consider using a LayoutBuilder, CustomSingleChildLayout, "
                    + "or CustomMultiChildLayout. If, on the other hand, the one frame delay "
                    + "is the desired effect, for example because this is an "
                    + "animation, consider scheduling the frame in a post-frame callback "
                    + "using SchedulerBinding.addPostFrameCallback or "
                    + "using an AnimationController to trigger the animation."),
            ]);
        }

        if (_readyToProduceFrames)
        {
            Scheduler.EnsureVisualUpdate();
        }
    }

    /// <summary>
    /// Makes sure the renderer binding's persistent frame callback, which runs <see cref="DrawFrame"/>,
    /// is registered; a test that reset the scheduler drops it.
    /// </summary>
    private static void EnsureFrameCallback()
    {
        RendererBinding.Instance.EnsurePersistentFrameCallback();
    }
}

/// <summary>The concrete binding installed by <c>RunApp</c> and <c>RunWidget</c>.</summary>
/// <remarks>Flutter's <c>WidgetsFlutterBinding</c>.</remarks>
public sealed class WidgetsFlutterBinding : WidgetsBinding
{
    /// <summary>Returns the process binding, creating it through static initialization when needed.</summary>
    public static WidgetsBinding EnsureInitialized() => Instance;
}
