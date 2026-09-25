using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/binding.dart

namespace Plumix;

/// <summary>
/// The glue between the render tree and the platform: the root of the <see cref="PipelineOwner"/>
/// tree, the registry of <see cref="RenderView"/>s that are currently on screen, the frame that lays
/// out, paints and composites them, and mouse tracking across those views.
/// </summary>
/// <remarks>
/// <para>
/// Flutter's <c>RendererBinding</c> mixin. Dart composes every binding into one object; Plumix's
/// bindings are separate singletons, so the members Dart reaches through that composition are
/// spelled out: the persistent frame callback runs <see cref="WidgetsBinding.DrawFrame"/> (Dart's
/// override of <see cref="DrawFrame"/>, which calls back into it), the gesture binding's final
/// hit-test entry is added by <see cref="HitTestInView"/>, and the semantics binding forwards
/// actions to <see cref="PerformSemanticsAction"/>.
/// </para>
/// <para>
/// Each Plumix host keeps its own <see cref="PipelineOwner"/>/<see cref="ReusableRenderView"/> pair
/// (Flutter's deprecated <c>pipelineOwner</c>/<c>renderView</c>), adopted by
/// <see cref="RootPipelineOwner"/> through the implicit <c>View</c>, and draws the frame this
/// binding composited from its own Avalonia render pass — see <c>docs/ai/DIVERGENCES.md</c>.
/// </para>
/// </remarks>
public sealed class RendererBinding
{
    private static RendererBinding? _instance;

    private readonly OrderedDictionary<int, RenderView> _viewIdToRenderView = [];
    private readonly Action<TimeSpan> _persistentFrameCallback;
    private BindingPipelineManifold? _manifold;
    private MouseTracker? _mouseTracker;
    private bool _debugMouseTrackerUpdateScheduled;
    private int _firstFrameDeferredCount;
    private bool _firstFrameSent;

    static RendererBinding()
    {
        _ = new RendererBinding();
    }

    /// <summary>Flutter's <c>RendererBinding.initInstances</c>.</summary>
    private RendererBinding()
    {
        _instance = this;
        _persistentFrameCallback = HandlePersistentFrameCallback;
        RootPipelineOwner = CreateRootPipelineOwner();
        // Dart installs handleMetricsChanged, handleTextScaleFactorChanged and
        // handlePlatformBrightnessChanged on the platform dispatcher here. Plumix hosts call the
        // widgets binding's overrides directly, which call these in turn (docs/ai/DIVERGENCES.md).
        Scheduler.AddPersistentFrameCallback(_persistentFrameCallback);
        InitMouseTracker();
        if (Constants.KIsWeb)
        {
            Scheduler.AddPostFrameCallback(HandleWebFirstFrame, debugLabel: "RendererBinding.webFirstFrame");
        }

        RootPipelineOwner.Attach(Manifold);
        InitServiceExtensions();
    }

    /// <summary>Tells the web host's service worker that the first frame was produced.</summary>
    /// <remarks>Flutter's private <c>RendererBinding._handleWebFirstFrame</c>.</remarks>
    private static void HandleWebFirstFrame(TimeSpan timeStamp)
    {
        Scheduler.RunAsync(static async () =>
        {
            try
            {
                await new MethodChannel("flutter/service_worker").InvokeMethod<object?>("first-frame");
            }
            catch (Exception exception)
            {
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    stack: exception.StackTrace,
                    library: "rendering library",
                    context: new ErrorDescription("while sending the first-frame event")));
            }
        });
    }

    /// <summary>Registers the rendering library's service extensions.</summary>
    /// <remarks>
    /// Flutter's <c>RendererBinding.initServiceExtensions</c>, which <c>BindingBase</c> runs right after
    /// <c>initInstances</c>. <c>invertOversizedImages</c> is not registered: Plumix's image painting has
    /// no oversized-image check for the flag to drive (see <c>docs/ai/BACKLOG.md</c>).
    /// </remarks>
    private void InitServiceExtensions()
    {
        if (Constants.KDebugMode)
        {
            RegisterRepaintingBoolExtension(
                "debugPaint",
                static () => RenderingDebug.PaintSizeEnabled,
                static value => RenderingDebug.PaintSizeEnabled = value);
            RegisterRepaintingBoolExtension(
                "debugPaintBaselinesEnabled",
                static () => RenderingDebug.PaintBaselinesEnabled,
                static value => RenderingDebug.PaintBaselinesEnabled = value);
            BindingBase.RegisterBoolServiceExtension(
                name: "repaintRainbow",
                getter: static () => Task.FromResult(RenderingDebug.RepaintRainbowEnabled),
                setter: value =>
                {
                    bool repaint = RenderingDebug.RepaintRainbowEnabled && !value;
                    RenderingDebug.RepaintRainbowEnabled = value;
                    if (repaint)
                    {
                        _ = ForceRepaint();
                    }

                    return Task.CompletedTask;
                });
            BindingBase.RegisterServiceExtension(
                "debugDumpLayerTree",
                static _ => Task.FromResult(new Dictionary<string, object?>
                {
                    ["data"] = RenderingDebug.DebugCollectLayerTrees(),
                }));
            RegisterRepaintingBoolExtension(
                "debugDisableClipLayers",
                static () => RenderingDebug.DisableClipLayers,
                static value => RenderingDebug.DisableClipLayers = value);
            RegisterRepaintingBoolExtension(
                "debugDisablePhysicalShapeLayers",
                static () => RenderingDebug.DisablePhysicalShapeLayers,
                static value => RenderingDebug.DisablePhysicalShapeLayers = value);
            RegisterRepaintingBoolExtension(
                "debugDisableOpacityLayers",
                static () => RenderingDebug.DisableOpacityLayers,
                static value => RenderingDebug.DisableOpacityLayers = value);
        }

        if (!Constants.KReleaseMode)
        {
            // These service extensions work in debug or profile mode.
            BindingBase.RegisterServiceExtension(
                "debugDumpRenderTree",
                static _ => Task.FromResult(new Dictionary<string, object?>
                {
                    ["data"] = RenderingDebug.DebugCollectRenderTrees(),
                }));
            BindingBase.RegisterServiceExtension(
                "debugDumpSemanticsTreeInTraversalOrder",
                static _ => Task.FromResult(new Dictionary<string, object?>
                {
                    ["data"] = RenderingDebug.DebugCollectSemanticsTrees(DebugSemanticsDumpOrder.TraversalOrder),
                }));
            BindingBase.RegisterServiceExtension(
                "debugDumpSemanticsTreeInInverseHitTestOrder",
                static _ => Task.FromResult(new Dictionary<string, object?>
                {
                    ["data"] = RenderingDebug.DebugCollectSemanticsTrees(DebugSemanticsDumpOrder.InverseHitTest),
                }));
            BindingBase.RegisterBoolServiceExtension(
                name: "profileRenderObjectPaints",
                getter: static () => Task.FromResult(RenderingDebug.ProfilePaintsEnabled),
                setter: static value =>
                {
                    if (RenderingDebug.ProfilePaintsEnabled != value)
                    {
                        RenderingDebug.ProfilePaintsEnabled = value;
                    }

                    return Task.CompletedTask;
                });
            BindingBase.RegisterBoolServiceExtension(
                name: "profileRenderObjectLayouts",
                getter: static () => Task.FromResult(RenderingDebug.ProfileLayoutsEnabled),
                setter: static value =>
                {
                    if (RenderingDebug.ProfileLayoutsEnabled != value)
                    {
                        RenderingDebug.ProfileLayoutsEnabled = value;
                    }

                    return Task.CompletedTask;
                });
        }
    }

    /// <summary>
    /// A debug flag extension whose setter repaints every view when the value changes, without waiting
    /// for that frame.
    /// </summary>
    private void RegisterRepaintingBoolExtension(string name, Func<bool> getter, Action<bool> setter)
    {
        BindingBase.RegisterBoolServiceExtension(
            name: name,
            getter: () => Task.FromResult(getter()),
            setter: value =>
            {
                if (getter() == value)
                {
                    return Task.CompletedTask;
                }

                setter(value);
                _ = ForceRepaint();
                return Task.CompletedTask;
            });
    }

    /// <summary>The ambient renderer binding.</summary>
    /// <remarks>Flutter's <c>RendererBinding.instance</c>.</remarks>
    public static RendererBinding Instance => _instance!;

    private BindingPipelineManifold Manifold => _manifold ??= new BindingPipelineManifold(this);

    /// <summary>Tracks the mouse annotations and cursors in every registered view.</summary>
    /// <remarks>Flutter's <c>RendererBinding.mouseTracker</c>.</remarks>
    public MouseTracker MouseTracker => _mouseTracker!;

    /// <summary>Creates the <see cref="PipelineOwner"/> that becomes <see cref="RootPipelineOwner"/>.</summary>
    /// <remarks>
    /// Flutter's <c>RendererBinding.createRootPipelineOwner</c>. The Dart hook is overridable; the
    /// Plumix binding is a sealed singleton, so there is nothing to override it from.
    /// </remarks>
    private static PipelineOwner CreateRootPipelineOwner() => new DefaultRootPipelineOwner();

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
    /// Adds a <see cref="RenderView"/> to this binding, so that it is configured from its view's
    /// metrics, kept in step with them by <see cref="HandleMetricsChanged"/>, and composited by
    /// <see cref="DrawFrame"/>.
    /// </summary>
    /// <remarks>Flutter's <c>RendererBinding.addRenderView</c>.</remarks>
    public void AddRenderView(RenderView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        int viewId = view.FlutterView.ViewId;
        DebugAssert(!_viewIdToRenderView.ContainsValue(view), "!_viewIdToRenderView.containsValue(view)");
        DebugAssert(!_viewIdToRenderView.ContainsKey(viewId), "!_viewIdToRenderView.containsKey(viewId)");
        _viewIdToRenderView[viewId] = view;
        view.Configuration = CreateViewConfigurationFor(view);
    }

    /// <summary>Removes a <see cref="RenderView"/> previously added with <see cref="AddRenderView"/>.</summary>
    /// <remarks>Flutter's <c>RendererBinding.removeRenderView</c>.</remarks>
    public void RemoveRenderView(RenderView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        int viewId = view.FlutterView.ViewId;
        DebugAssert(
            _viewIdToRenderView.TryGetValue(viewId, out RenderView? registered) && ReferenceEquals(registered, view),
            "_viewIdToRenderView[viewId] == view");
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
    /// gets a fresh configuration, and a frame is forced if any of them has content.
    /// </summary>
    /// <remarks>Flutter's <c>RendererBinding.handleMetricsChanged</c>.</remarks>
    public void HandleMetricsChanged()
    {
        bool forceFrame = false;
        foreach (RenderView view in RenderViews)
        {
            forceFrame = forceFrame || view.Child is not null;
            view.Configuration = CreateViewConfigurationFor(view);
        }

        if (forceFrame)
        {
            Scheduler.ScheduleForcedFrame();
        }
    }

    /// <summary>Called when the platform text scale factor changes.</summary>
    /// <remarks>
    /// Flutter's <c>RendererBinding.handleTextScaleFactorChanged</c>, which does nothing; the widgets
    /// binding's override notifies its observers.
    /// </remarks>
    public void HandleTextScaleFactorChanged()
    {
    }

    /// <summary>Called when the platform brightness changes.</summary>
    /// <remarks>
    /// Flutter's <c>RendererBinding.handlePlatformBrightnessChanged</c>, which does nothing; the
    /// widgets binding's override notifies its observers.
    /// </remarks>
    public void HandlePlatformBrightnessChanged()
    {
    }

    /// <summary>Creates a <see cref="MouseTracker"/> that manages state for mouse pointers.</summary>
    /// <remarks>
    /// Flutter's <c>RendererBinding.initMouseTracker</c>, which tests call to replace the tracker.
    /// </remarks>
    public void InitMouseTracker(MouseTracker? tracker = null)
    {
        _mouseTracker?.Dispose();
        _mouseTracker = tracker ?? new MouseTracker((position, viewId) =>
        {
            var result = new HitTestResult();
            HitTestInView(result, position, viewId);
            return result;
        });
    }

    /// <summary>Performs a semantics action on the view named by <paramref name="action"/>.</summary>
    /// <remarks>
    /// Flutter's <c>RendererBinding.performSemanticsAction</c>: an unknown view, or a view without a
    /// semantics owner, ignores the action.
    /// </remarks>
    public void PerformSemanticsAction(SemanticsActionEvent action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_viewIdToRenderView.TryGetValue(action.ViewId, out RenderView? renderView))
        {
            renderView.Owner?.SemanticsOwner?.PerformAction(action.NodeId, action.Type, action.Arguments);
        }
    }

    /// <summary>
    /// The box of the semantics node <paramref name="nodeId"/> in the coordinate space of the view
    /// <paramref name="viewId"/>, in logical pixels, or <see langword="null"/> when either is unknown.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RendererBinding.getRectOfSemanticsNodeInViewCoordinates</c>. Dart asserts on each
    /// of the three failed lookups and pre-multiplies the inverse of the root's device-pixel-ratio
    /// transform; see <c>docs/ai/DIVERGENCES.md</c> for why Plumix does neither.
    /// </remarks>
    public Rect? GetRectOfSemanticsNodeInViewCoordinates(int viewId, int nodeId)
    {
        if (!_viewIdToRenderView.TryGetValue(viewId, out RenderView? renderView))
        {
            return null;
        }

        SemanticsOwner? semanticsOwner = renderView.Owner?.SemanticsOwner;
        return semanticsOwner?.GetSemanticsNode(nodeId)?.GlobalRect;
    }

    /// <summary>
    /// Replaces the composed <c>drawFrame</c> the persistent frame callback runs, the way
    /// flutter_test's binding overrides it. A test harness that owns its own build owner builds its
    /// tree here and then calls <see cref="DrawFrame"/>.
    /// </summary>
    internal Action? DrawFrameOverrideForTests { get; set; }

    private void HandlePersistentFrameCallback(TimeSpan timeStamp)
    {
        // Dart calls the composed binding's `drawFrame`, which is the widgets binding's override.
        if (DrawFrameOverrideForTests is { } drawFrameOverride)
        {
            drawFrameOverride();
        }
        else
        {
            WidgetsBinding.Instance.DrawFrame();
        }

        ScheduleMouseTrackerUpdate();
    }

    /// <summary>
    /// Queues Flutter's post-frame mouse recheck after the render trees have produced a frame.
    /// </summary>
    /// <remarks>Flutter's private <c>RendererBinding._scheduleMouseTrackerUpdate</c>.</remarks>
    internal void ScheduleMouseTrackerUpdate()
    {
        DebugAssert(!_debugMouseTrackerUpdateScheduled, "!_debugMouseTrackerUpdateScheduled");
        if (Constants.KDebugMode)
        {
            _debugMouseTrackerUpdateScheduled = true;
        }

        Scheduler.AddPostFrameCallback(
            _ =>
            {
                DebugAssert(_debugMouseTrackerUpdateScheduled, "_debugMouseTrackerUpdateScheduled");
                if (Constants.KDebugMode)
                {
                    _debugMouseTrackerUpdateScheduled = false;
                }

                _mouseTracker!.UpdateAllDevices();
            },
            debugLabel: "RendererBinding.mouseTrackerUpdate");
    }

    /// <summary>
    /// Whether frames produced by <see cref="DrawFrame"/> are sent to the platform.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RendererBinding.sendFramesToEngine</c>: false only while the first frame is
    /// deferred with <see cref="DeferFirstFrame"/> and has not been sent yet.
    /// </remarks>
    public bool SendFramesToEngine => _firstFrameSent || _firstFrameDeferredCount == 0;

    /// <summary>Tells the framework not to send the first frames to the platform until there is a
    /// corresponding call to <see cref="AllowFirstFrame"/>.</summary>
    /// <remarks>
    /// Flutter's <c>RendererBinding.deferFirstFrame</c>. Calls after the first frame was sent have no
    /// effect.
    /// </remarks>
    public void DeferFirstFrame()
    {
        DebugAssert(_firstFrameDeferredCount >= 0, "_firstFrameDeferredCount >= 0");
        _firstFrameDeferredCount += 1;
    }

    /// <summary>Called after <see cref="DeferFirstFrame"/> to tell the framework that it is ok to
    /// send the first frame to the platform now.</summary>
    /// <remarks>
    /// Flutter's <c>RendererBinding.allowFirstFrame</c>: schedules a warm-up frame whenever the first
    /// frame has not been sent yet, even if other deferrals are still outstanding.
    /// </remarks>
    public void AllowFirstFrame()
    {
        DebugAssert(_firstFrameDeferredCount > 0, "_firstFrameDeferredCount > 0");
        _firstFrameDeferredCount -= 1;
        // Always schedule a warm up frame even if the deferral count is not down to
        // zero yet since the removal of a deferral may uncover new deferrals that
        // are lower in the widget tree.
        if (!_firstFrameSent)
        {
            Scheduler.ScheduleWarmUpFrame();
        }
    }

    /// <summary>Call this to pretend that no frames have been sent to the platform yet.</summary>
    /// <remarks>Flutter's <c>RendererBinding.resetFirstFrameSent</c>.</remarks>
    public void ResetFirstFrameSent()
    {
        _firstFrameSent = false;
    }

    /// <summary>
    /// Pumps the rendering pipeline to generate a frame: layout, compositing bits and paint for the
    /// whole pipeline owner tree, then — unless the first frame is deferred — composites every
    /// registered <see cref="RenderView"/> and sends the semantics update.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RendererBinding.drawFrame</c>. It runs from the persistent frame callback, wrapped
    /// by <see cref="WidgetsBinding.DrawFrame"/>, which builds dirty widgets first and finalizes the
    /// element tree afterwards.
    /// </remarks>
    public void DrawFrame()
    {
        RootPipelineOwner.FlushLayout();
        RootPipelineOwner.FlushCompositingBits();
        RootPipelineOwner.FlushPaint();
        if (SendFramesToEngine)
        {
            foreach (RenderView renderView in RenderViews.ToArray())
            {
                renderView.CompositeFrame(); // this sends the bits to the GPU
            }

            RootPipelineOwner.FlushSemantics(); // this sends the semantics to the OS.
            _firstFrameSent = true;
        }
    }

    /// <summary>
    /// Reassembles every registered render view after a hot reload, then waits for the warm-up
    /// frame that shows the result.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RendererBinding.performReassemble</c> (the <c>FlutterTimeline</c> section is not
    /// ported; see <c>docs/ai/BACKLOG.md</c>).
    /// </remarks>
    public async Task PerformReassemble()
    {
        foreach (RenderView renderView in RenderViews.ToArray())
        {
            renderView.Reassemble();
        }

        Scheduler.ScheduleWarmUpFrame();
        await Scheduler.EndOfFrame;
    }

    /// <summary>
    /// Hit-tests the registered view identified by <paramref name="viewId"/> into
    /// <paramref name="result"/>, then adds the gesture binding as the final target.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RendererBinding.hitTestInView</c> over <c>GestureBinding.hitTestInView</c>: the
    /// gesture binding is the last entry even when the view id has no registered render view.
    /// </remarks>
    public void HitTestInView(HitTestResult result, Point position, int viewId)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (_viewIdToRenderView.TryGetValue(viewId, out RenderView? renderView))
        {
            renderView.HitTest(result as BoxHitTestResult ?? new BoxHitTestResult(result), position);
        }

        result.Add(new HitTestEntry(GestureBinding.Instance));
    }

    /// <summary>Marks every render object below the registered views as needing paint.</summary>
    /// <remarks>
    /// Flutter's private <c>RendererBinding._forceRepaint</c>, which the debug-paint service
    /// extensions call; the views themselves are not marked.
    /// </remarks>
    internal Task ForceRepaint()
    {
        void Visitor(RenderObject child)
        {
            child.MarkNeedsPaint();
            child.VisitChildren(Visitor);
        }

        foreach (RenderView renderView in RenderViews.ToArray())
        {
            renderView.VisitChildren(Visitor);
        }

        return Scheduler.EndOfFrame;
    }

    /// <summary>
    /// Re-registers the persistent frame callback and a fresh mouse tracker after a test reset the
    /// scheduler.
    /// </summary>
    internal void ResetForTests()
    {
        _debugMouseTrackerUpdateScheduled = false;
        _firstFrameDeferredCount = 0;
        _firstFrameSent = false;
        DrawFrameOverrideForTests = null;
        Scheduler.AddPersistentFrameCallback(_persistentFrameCallback);
        InitMouseTracker();
    }

    /// <summary>Makes sure the persistent frame callback survives a test that reset the scheduler.</summary>
    internal void EnsurePersistentFrameCallback()
    {
        Scheduler.AddPersistentFrameCallback(_persistentFrameCallback);
    }

    private static void DebugAssert(bool condition, string expression)
    {
        if (Constants.KDebugMode && !condition)
        {
            throw new AssertionError($"'{expression}': is not true.");
        }
    }

    /// <summary>
    /// Flutter's <c>_BindingPipelineManifold</c>: semantics follow <see cref="SemanticsBinding"/>,
    /// and visual updates ask the scheduler for a frame.
    /// </summary>
    private sealed class BindingPipelineManifold : PipelineManifold, IDisposable
    {
        private readonly ChangeNotifier _notifier = new();

        public BindingPipelineManifold(RendererBinding binding)
        {
            _ = binding;
            SemanticsBinding.Instance.AddSemanticsEnabledListener(_notifier.NotifyListeners);
        }

        public override bool SemanticsEnabled => SemanticsBinding.Instance.SemanticsEnabled;

        public override void RequestVisualUpdate() => Scheduler.EnsureVisualUpdate();

        public override void AddListener(Action listener) => _notifier.AddListener(listener);

        public override void RemoveListener(Action listener) => _notifier.RemoveListener(listener);

        public void Dispose()
        {
            SemanticsBinding.Instance.RemoveSemanticsEnabledListener(_notifier.NotifyListeners);
            _notifier.Dispose();
        }
    }

    /// <summary>Flutter's <c>_DefaultRootPipelineOwner</c>: a root owner that refuses a root node.</summary>
    private sealed class DefaultRootPipelineOwner : PipelineOwner
    {
        public DefaultRootPipelineOwner()
            : base(onSemanticsUpdate: OnSemanticsUpdate)
        {
        }

        public override RenderObject? RootNode
        {
            get => base.RootNode;
            set
            {
                if (!Constants.KDebugMode)
                {
                    return;
                }

                throw new FlutterError(
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

        private static void OnSemanticsUpdate(SemanticsUpdate update)
        {
            DebugAssert(false, "false");
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

/// <summary>A concrete binding for applications that use the rendering layer directly.</summary>
/// <remarks>
/// Flutter's <c>RenderingFlutterBinding</c>. Plumix's bindings are process singletons that exist
/// from first use, so <see cref="EnsureInitialized"/> only returns the renderer binding.
/// </remarks>
public static class RenderingFlutterBinding
{
    /// <summary>Returns the renderer binding, creating it if necessary.</summary>
    /// <remarks>Flutter's <c>RenderingFlutterBinding.ensureInitialized</c>.</remarks>
    public static RendererBinding EnsureInitialized() => RendererBinding.Instance;
}
