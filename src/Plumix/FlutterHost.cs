using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.Threading;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Plumix.Accessibility;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using AvaloniaTextSelection = Avalonia.Input.TextInput.TextSelection;
using FeedbackType = Plumix.UI.FeedbackType;

// C#-only infrastructure: the engine embedder of one `FlutterView` on Avalonia. It owns no framework
// behavior — frames are built, laid out, painted and composited by `WidgetsBinding.drawFrame` over
// `RendererBinding.drawFrame`; this control reports platform state to `PlatformDispatcher`, rasterizes
// the scene `FlutterView.render` hands it, records and reports `FrameTiming`s (the engine's
// `FrameTimingsRecorder`/`Shell::OnFrameRasterized`), and answers the platform channels the engine's
// plugins answer.

namespace Plumix;

public partial class PlumixHost : Control, ICustomHitTest
{
    static PlumixHost()
    {
        TextInputMethodClientRequestedEvent.AddClassHandler<PlumixHost>((host, e) =>
        {
            e.Client = host.HasTextInputClient ? host._textInputClient : null;
        });
    }

    private static int s_nextViewId = -1;

    // The engine is one per process while every Plumix host is a view of its own: the hosts attached to
    // a visual tree, in attach order, and the ones answering the process-wide `flutter/platform` and
    // `flutter/mousecursor` channels. When a holder detaches, the next attached host takes over.
    private static readonly List<PlumixHost> s_attachedHosts = [];
    private static PlumixHost? s_platformChannelHost;
    private static bool s_isMouseCursorHandlerInstalled;

    // The Android engine's back callback state (`PlatformPlugin.setFrameworkHandlesBack`). Only the
    // Android framework reports it; elsewhere back keys always go to the framework.
    private static bool s_frameworkHandlesBack = true;

    // Avalonia raises `TopLevel.BackRequested` for the platform's system back only on Android; the
    // desktop backends raise it for Alt+Left and the mouse back button, which Flutter's desktop
    // embedders do not treat as back (Alt+Left moves by word in macOS text fields).
    private static readonly bool s_listensToSystemBack = OperatingSystem.IsAndroid();

    /// <summary>
    /// Whether the Escape and BrowserBack keys run <c>popRoute</c> (C#-only, see <c>docs/ai/DIVERGENCES.md</c>):
    /// everywhere but Android, whose system back reaches the framework through <c>TopLevel.BackRequested</c>
    /// (and the predictive back callback), as it reaches <c>FlutterActivity.onBackPressed</c>. Test-settable.
    /// </summary>
    internal static bool MapsBackKeysToPopRoute { get; set; } = !OperatingSystem.IsAndroid();

    // What the framework last reported through `SystemNavigator.setFrameworkHandlesBack`; null until it
    // reports, so an Activity created later can register the engine's back callback to match.
    private static bool? s_reportedFrameworkHandlesBack;

    /// <summary>Resets the process-wide back callback state the Android framework reported. Test-only.</summary>
    internal static void ResetFrameworkHandlesBackForTests()
    {
        s_frameworkHandlesBack = true;
        s_reportedFrameworkHandlesBack = null;
        FrameworkHandlesBackChanged = null;
        MapsBackKeysToPopRoute = !OperatingSystem.IsAndroid();
        ResetPlatformPluginForTests();
    }

    /// <summary>
    /// The last <c>SystemNavigator.setFrameworkHandlesBack</c> value, or null before the framework sent one.
    /// </summary>
    internal static bool? ReportedFrameworkHandlesBack => s_reportedFrameworkHandlesBack;

    /// <summary>
    /// Raised for every <c>SystemNavigator.setFrameworkHandlesBack</c>: the Android engine's
    /// <c>PlatformPluginDelegate.setFrameworkHandlesBack</c>, which <c>FlutterActivity</c> answers by
    /// registering or unregistering its back callback.
    /// </summary>
    internal static event Action<bool>? FrameworkHandlesBackChanged;

    // The Android embedder's `ViewportMetrics.physicalTouchSlop` (`ViewConfiguration.getScaledTouchSlop`),
    // which its `FlutterView` sends with every metrics update. The other embedders send none, so
    // `FlutterView.gestureSettings` carries no touch slop there and recognizers use `kTouchSlop`.
    private static double? s_physicalTouchSlop;

    /// <summary>
    /// Records the platform's touch slop in physical pixels, or <see langword="null"/> where the platform
    /// reports none, and republishes the metrics of every attached host with it.
    /// </summary>
    internal static void UpdatePhysicalTouchSlop(double? physicalTouchSlop)
    {
        if (physicalTouchSlop is { } slop && (!double.IsFinite(slop) || slop < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(physicalTouchSlop));
        }

        if (s_physicalTouchSlop == physicalTouchSlop)
        {
            return;
        }

        s_physicalTouchSlop = physicalTouchSlop;
        foreach (PlumixHost host in s_attachedHosts.ToArray())
        {
            host.OnMetricsChanged();
        }
    }

    // Dart's engine numbers its views and hands the implicit one id 0; every Plumix host is a view of
    // its own, so ids are taken from a process-wide counter (the `RendererBinding` registry is keyed
    // by them).
    private readonly FlutterView _view = new(new Size(0, 0), 1.0, Interlocked.Increment(ref s_nextViewId));
    private readonly ReusableRenderView _root;
    private readonly PipelineOwner _pipeline;
    // Instantiating the binding installs its packet handler on the platform dispatcher.
    private readonly GestureBinding _gestureBinding = GestureBinding.Instance;
    private readonly HostPointerPacketEncoder _pointerEncoder = new();
    private readonly PlumixTextInputMethodClient _textInputClient;
    private readonly Thread _ownerThread;
    private readonly TrackpadPanZoomSynthesizer _panZoom;
    private readonly CompositorContext _compositor = new();
    private ContainerFlowLayer? _layerTree;
    private Size _layerTreeTargetSize;
    private Matrix _layerTreeTransform = Matrix.Identity;
    private List<IDisposable> _rasterResources = [];
    private FrameTimingsRecorder? _pendingFrameTimings;
    private bool _isRenderOnlyViewAttached;
    private Size _lastArrangedSize;
    private TopLevel? _attachedTopLevel;
    private IInsetsManager? _insetsManager;
    private IInputPane? _inputPane;
    private bool _isSubscribedToMouseCursor;
    private bool _isSubscribedToFeedback;
    private bool _isSubscribedToPlatformChannel;
    private bool _isAttachedToTextInputPlugin;
    private bool _allowWindowClose;
    private bool _viewHasFocus;
    private ViewFocusDirection _requestedViewFocusDirection;
    private SystemUiOverlayStyle _currentSystemUiOverlayStyle = new();
    private ApplicationSwitcherDescription? _currentApplicationSwitcherDescription;
    private string _currentMouseCursorKind = SystemMouseCursors.Basic.Kind;

    public event Action<SemanticsNode?>? SemanticsUpdated;

    /// <summary>
    /// Raised with the nodes that changed in the frame just flushed, before
    /// <see cref="SemanticsUpdated"/>.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>PipelineOwner.onSemanticsUpdate</c>: the incremental feed a platform
    /// accessibility bridge consumes, as opposed to the whole tree <see cref="SemanticsUpdated"/>
    /// republishes.
    /// </remarks>
    public event Action<SemanticsUpdate>? SemanticsUpdateProduced;

    public bool StartPredictiveBack(PredictiveBackEvent backEvent)
    {
        return WidgetsBinding.Instance.HandleStartBackGesture(backEvent);
    }

    public void UpdatePredictiveBack(PredictiveBackEvent backEvent)
    {
        WidgetsBinding.Instance.HandleUpdateBackGestureProgress(backEvent);
    }

    /// <summary>
    /// Commits the current predictive back gesture; the task completes with whether the framework
    /// handled it, so a host can fall back to its own back behavior.
    /// </summary>
    public Task<bool> CommitPredictiveBack()
    {
        using EventLoop.FrameworkThreadScope scope = EventLoop.EnterFrameworkThread();
        return WidgetsBinding.Instance.HandleCommitBackGesture();
    }

    public void CancelPredictiveBack()
    {
        WidgetsBinding.Instance.HandleCancelBackGesture();
    }

    public PlumixHost()
    {
        // Dart's deprecated `RendererBinding.pipelineOwner`/`renderView` pair, per host: the owner is
        // created exactly as Dart's (no visual-update callback — the binding's manifold answers that once
        // the owner is adopted — and semantics follow `SemanticsBinding.semanticsEnabled`). The view is
        // attached when the implicit `View` widget mounts, or by `SetRootChild` for a render-only tree.
        _root = new ReusableRenderView(_view);
        _pipeline = new PipelineOwner(
            onSemanticsOwnerCreated: () => (_pipeline!.RootNode as RenderView)?.ScheduleInitialSemantics(),
            onSemanticsUpdate: update => (_pipeline!.RootNode as RenderView)?.UpdateSemantics(update),
            onSemanticsOwnerDisposed: () =>
            {
                (_pipeline!.RootNode as RenderView)?.ClearSemantics();
                AccessibilityBridge!.Clear();
            });
        AccessibilityBridge = new AccessibilityBridge(_view.ViewId, () => _view.DevicePixelRatio);
        _view.SemanticsUpdated += HandleViewSemanticsUpdated;
        _view.RenderRequested += HandleViewRenderRequested;
        _textInputClient = new PlumixTextInputMethodClient(this);
        _ownerThread = Thread.CurrentThread;
        _panZoom = new TrackpadPanZoomSynthesizer(DispatchPointerEvent);

        AddHandler(PointerTouchPadGestureMagnifyEvent, HandleTouchPadMagnify);
        AddHandler(PointerTouchPadGestureRotateEvent, HandleTouchPadRotate);

        ClipToBounds = true;
        Focusable = true;
    }

    internal RenderBox? RootChild => _root.Child;

    /// <summary>
    /// The platform view this host renders into. Flutter's engine owns it and the binding hands it
    /// to the implicit <c>View</c>; the host keeps its metrics in step from <see cref="OnMetricsChanged"/>.
    /// </summary>
    public FlutterView RootFlutterView => _view;

    /// <summary>
    /// The host's own <see cref="RenderView"/>: Flutter's deprecated <c>RendererBinding.renderView</c>.
    /// </summary>
    protected ReusableRenderView RootRenderView => _root;

    /// <summary>
    /// The host's own <see cref="PipelineOwner"/>: Flutter's deprecated <c>RendererBinding.pipelineOwner</c>.
    /// </summary>
    protected PipelineOwner Pipeline => _pipeline;

    public SemanticsNode? SemanticsRoot => _pipeline.SemanticsOwner?.RootNode;

    /// <summary>
    /// The host-side mirror of this view's semantics tree, which the platform accessibility bridges
    /// read: the engine's per-view <c>AccessibilityBridge</c>.
    /// </summary>
    public AccessibilityBridge AccessibilityBridge { get; }

    /// <summary>
    /// Exposes this view's semantics tree to the platform's automation API (UI Automation, the macOS
    /// accessibility API, AT-SPI and Android's accessibility services, through Avalonia).
    /// </summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new PlumixHostAutomationPeer(this);

    /// <summary>
    /// Renders a bare render tree, without a widget layer.
    /// </summary>
    /// <remarks>
    /// Dart leaves wiring the deprecated <c>renderView</c> to "a higher level" for apps that do not call
    /// <c>runApp</c>: the first child makes the view the root of this host's pipeline owner, adopts that
    /// owner into <see cref="RendererBinding.RootPipelineOwner"/> and registers the view with
    /// <see cref="RendererBinding.AddRenderView"/> — the steps <c>_RawViewElement.mount</c> takes for a
    /// <c>View</c> widget — so the binding's frame lays it out, paints and composites it. Clearing the
    /// child undoes them.
    /// </remarks>
    public void SetRootChild(RenderBox? child)
    {
        if (child is not null && !_isRenderOnlyViewAttached)
        {
            _isRenderOnlyViewAttached = true;
            _pipeline.RootNode = _root;
            RendererBinding.Instance.RootPipelineOwner.AdoptChild(_pipeline);
            RendererBinding.Instance.AddRenderView(_root);
            _root.PrepareInitialFrame();
        }

        _root.Child = child;
        if (child is null && _isRenderOnlyViewAttached)
        {
            _isRenderOnlyViewAttached = false;
            RendererBinding.Instance.RemoveRenderView(_root);
            RendererBinding.Instance.RootPipelineOwner.DropChild(_pipeline);
            _pipeline.RootNode = null;
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!_lastArrangedSize.Equals(finalSize))
        {
            _lastArrangedSize = finalSize;
            OnMetricsChanged();
        }

        return base.ArrangeOverride(finalSize);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // A finger or button going down takes over from any trackpad gesture still in its idle
        // window; the framework must see that gesture end before the new sequence starts.
        _panZoom.End();
        Focus();
        e.Pointer.Capture(this);
        QueryNativeHitTest(e.GetPosition(this));
        DispatchPointerEvent(ToPointerDownEvent(e));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled)
        {
            return;
        }

        if (HotReloadManager.IsManualReassembleAvailable
            && e.Key == Key.R
            && e.KeyModifiers.HasFlag(KeyModifiers.Shift)
            && (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta))
            && !e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            e.Handled = true;
            _ = HotReloadManager.ReassembleApplication();
            return;
        }

        if (DispatchHostKeyEvent(e, isDown: true))
        {
            e.Handled = true;
            return;
        }

        // The engine hands keys the framework left unhandled to the platform text input plugin.
        if (HasTextInputClient && TextInputPlugin.HandleKeyEvent(
                HostKeyboardMap.LogicalKeyFor(e.Key, e.KeySymbol),
                control: e.KeyModifiers.HasFlag(KeyModifiers.Control),
                shift: e.KeyModifiers.HasFlag(KeyModifiers.Shift),
                alt: e.KeyModifiers.HasFlag(KeyModifiers.Alt),
                meta: e.KeyModifiers.HasFlag(KeyModifiers.Meta)))
        {
            e.Handled = true;
            return;
        }

        // Avalonia's `Key.Back` is Backspace on every platform (Android's KEYCODE_DEL); Android's
        // KEYCODE_BACK arrives as `Key.Escape`, and, left unhandled, comes back as the system back.
        bool isBackKey = e.Key is Key.Escape or Key.BrowserBack;
        if (isBackKey && MapsBackKeysToPopRoute && HandleBackButton(fromKey: true))
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Answers the system back request Avalonia raises on the <see cref="TopLevel"/>: Android's
    /// <c>OnBackPressedDispatcher</c> callback.
    /// </summary>
    /// <remarks>
    /// The request is left unhandled when the framework does not handle back, so the platform closes the
    /// app itself, as the engine's disabled back callback lets it.
    /// </remarks>
    internal void HandleBackRequested(RoutedEventArgs e)
    {
        if (!e.Handled && HandleBackButton(fromKey: false))
        {
            e.Handled = true;
        }
    }

    private void HandleTopLevelBackRequested(object? sender, RoutedEventArgs e) => HandleBackRequested(e);

    /// <summary>
    /// The engine's back button: a <c>popRoute</c> message, sent only while the framework reported it
    /// handles back (<c>SystemNavigator.setFrameworkHandlesBack</c>). Returns whether the host must
    /// treat the back press as consumed. A back key's unanswered pop does not reach the platform's
    /// <c>SystemNavigator.pop</c> (see <see cref="SystemNavigatorPopHandler"/>).
    /// </summary>
    private static bool HandleBackButton(bool fromKey)
    {
        if (!s_frameworkHandlesBack)
        {
            return false;
        }

        // The platform does not wait on `popRoute`; Avalonia needs the answer now: a pop an observer is
        // still deciding stays with the framework.
        Task<bool> handled;
        using (EventLoop.EnterFrameworkThread())
        {
            handled = fromKey
                ? RunKeyPopRoute(WidgetsBinding.Instance.HandlePopRoute)
                : WidgetsBinding.Instance.HandlePopRoute();
        }

        return !handled.IsCompleted || handled is { IsCompletedSuccessfully: true, Result: true };
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (e.Handled)
        {
            return;
        }

        if (DispatchHostKeyEvent(e, isDown: false))
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Sends the event to the framework as the running platform's embedder would: a raw key message on
    /// <c>flutter/keyevent</c>, which <c>KeyEventManager.HandleRawKeyMessage</c> decodes, records and
    /// regularizes into the <see cref="KeyEvent"/> stream before dispatching both.
    /// </summary>
    private static bool DispatchHostKeyEvent(KeyEventArgs e, bool isDown) => HostKeyEventMessage.Dispatch(e, isDown);

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (e.Handled || IsControlText(e.Text))
        {
            return;
        }

        // The committed text goes to the platform text input plugin, which reports the new editing
        // state to the framework, as the engine's `insertText:` does.
        if (HasTextInputClient)
        {
            TextInputPlugin.InsertText(e.Text);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Avalonia raises the characters of an unhandled key press as text input (iOS's Delete key as
    /// U+007F, Backspace as U+0008); no engine commits control characters as text.
    /// </summary>
    internal static bool IsControlText([NotNullWhen(false)] string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        foreach (char character in text)
        {
            if (!char.IsControl(character))
            {
                return false;
            }
        }

        return true;
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);

        DispatchPointerEvent(new PointerAddedEvent(
            pointer: unchecked((int)e.Pointer.Id),
            kind: ToPointerKind(e.Pointer.Type),
            position: e.GetPosition(this),
            timestampUtc: FrameClock.UtcNow));
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        DispatchPointerEvent(new PointerRemovedEvent(
            pointer: unchecked((int)e.Pointer.Id),
            kind: ToPointerKind(e.Pointer.Type),
            position: e.GetPosition(this),
            timestampUtc: FrameClock.UtcNow));
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var pointer = e.GetCurrentPoint(this);
        var position = pointer.Position;
        var buttons = ToPointerButtons(pointer.Properties);
        var kind = ToPointerKind(e.Pointer.Type);

        if (buttons == PointerButtons.None)
        {
            DispatchPointerEvent(new PointerHoverEvent(
                pointer: unchecked((int)e.Pointer.Id),
                kind: kind,
                position: position,
                buttons: buttons,
                timestampUtc: FrameClock.UtcNow));
            return;
        }

        DispatchPointerEvent(new PointerMoveEvent(
            pointer: unchecked((int)e.Pointer.Id),
            kind: kind,
            position: position,
            buttons: buttons,
            timestampUtc: FrameClock.UtcNow));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        DispatchPointerEvent(new PointerUpEvent(
            pointer: unchecked((int)e.Pointer.Id),
            kind: ToPointerKind(e.Pointer.Type),
            position: e.GetPosition(this),
            buttons: ToPointerButtons(e.GetCurrentPoint(this).Properties),
            timestampUtc: FrameClock.UtcNow));

        if (e.Pointer.Captured == this)
        {
            e.Pointer.Capture(null);
        }
    }

    /// <summary>
    /// Turns a trackpad pinch into the zoom component of a synthesized pan/zoom gesture. Avalonia
    /// forwards <c>NSEvent.magnification</c>, an additive increment on the current zoom, in both
    /// components of <see cref="PointerDeltaEventArgs.Delta"/>.
    /// </summary>
    private void HandleTouchPadMagnify(object? sender, PointerDeltaEventArgs e)
    {
        _panZoom.Zoom(e.GetPosition(this), 1.0 + e.Delta.Y, FrameClock.UtcNow);
    }

    /// <summary>
    /// Turns a trackpad rotation into the rotation component of a synthesized pan/zoom gesture.
    /// Avalonia forwards <c>NSEvent.rotation</c> in degrees, counterclockwise positive; Flutter
    /// reports radians the other way round, the same conversion its macOS embedder applies.
    /// </summary>
    private void HandleTouchPadRotate(object? sender, PointerDeltaEventArgs e)
    {
        _panZoom.Rotate(e.GetPosition(this), -e.Delta.Y * Math.PI / 180.0, FrameClock.UtcNow);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        bool allowPlatformDefault = true;
        DispatchPointerEvent(new PointerScrollEvent(
            device: unchecked((int)e.Pointer.Id),
            kind: ToPointerKind(e.Pointer.Type),
            position: e.GetPosition(this),
            scrollDelta: new Point(-e.Delta.X * 40.0, -e.Delta.Y * 40.0),
            timestampUtc: FrameClock.UtcNow,
            onRespond: allow => allowPlatformDefault = allow));
        if (!allowPlatformDefault)
        {
            e.Handled = true;
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        _panZoom.End();

        DispatchPointerEvent(new PointerCancelEvent(
            pointer: unchecked((int)e.Pointer.Id),
            kind: ToPointerKind(e.Pointer.Type),
            timestampUtc: FrameClock.UtcNow));
    }

    /// <summary>
    /// The whole view receives pointer input, as the engine hands every event inside a FlutterView to
    /// the framework, whatever the scene draws there.
    /// </summary>
    /// <remarks>
    /// Avalonia otherwise hit-tests the drawn content, and native draws (exact solid paths, shadows,
    /// hairlines) are not hit-testable, so the pointer over them would fall through to the controls behind.
    /// </remarks>
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

    /// <summary>
    /// The engine's rasterizer: draws the layer tree <see cref="FlutterView.Render"/> last handed to this
    /// host, and reports the frame's timings the first time that tree is drawn.
    /// </summary>
    /// <remarks>
    /// Frames are produced by the binding (<c>WidgetsBinding.drawFrame</c>); this pass never builds, lays
    /// out or paints. Avalonia may render again without a new frame (an expose, a resize before the
    /// forced frame arrives), which redraws the last scene, as the engine shows its last frame.
    /// </remarks>
    public override void Render(DrawingContext context)
    {
        FrameTimingsRecorder? timings = _pendingFrameTimings;
        _pendingFrameTimings = null;
        if (timings is not null)
        {
            // `Rasterizer::DoDraw`: the UI-time lap is the frame's build duration.
            _compositor.UiTime.SetLapTime(timings.BuildDuration);
            timings.RecordRasterStart();
        }

        RasterizeLayerTree(context);
        if (timings is not null)
        {
            FrameTimingsReporter.OnFrameRasterized(timings.RecordRasterEnd());
        }
    }

    /// <summary>
    /// Draws the last layer tree <see cref="FlutterView.Render"/> handed to this host: the engine's
    /// rasterizer. The scene is in physical pixels and Avalonia's drawing context in logical ones, so
    /// the tree is drawn under the inverse of the view's device-pixel-ratio transform.
    /// </summary>
    private void RasterizeLayerTree(DrawingContext context)
    {
        if (_layerTree is not { } layerTree)
        {
            return;
        }

        List<IDisposable> previousResources = _rasterResources;
        _rasterResources = SceneRasterizer.DrawFrame(
            context,
            layerTree,
            _layerTreeTargetSize,
            _layerTreeTransform,
            _compositor,
            TextureRegistry.Instance);
        // The previous frame's raster outputs stay alive until this frame no longer draws from them.
        foreach (IDisposable resource in previousResources)
        {
            resource.Dispose();
        }
    }

    /// <summary>
    /// Whether a windowing owner manages this host's window: its controller owns the window's title, close
    /// requests and destruction, so the host neither answers them itself nor reports the app detached.
    /// </summary>
    private protected virtual bool IsWindowingView => false;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AvaloniaDispatcherClock.EnsureSynchronized();
        if (!s_attachedHosts.Contains(this))
        {
            s_attachedHosts.Add(this);
        }

        PlatformDispatcher.Instance.AddView(_view);
        WidgetsBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
        AttachMouseCursorListener();
        AttachFeedbackListener();
        AttachPlatformChannelHandler();
        AttachTextInputPlugin();
        AttachMetricSources();
        PlatformDispatcher.Instance.ViewFocusChangeRequested -= HandleViewFocusChangeRequested;
        PlatformDispatcher.Instance.ViewFocusChangeRequested += HandleViewFocusChangeRequested;
        TextureRegistry.Instance.TextureFrameAvailable -= HandleTextureFrameAvailable;
        TextureRegistry.Instance.TextureFrameAvailable += HandleTextureFrameAvailable;
        if (IsFocused && (_attachedTopLevel is not WindowBase window || window.IsActive))
        {
            ReportViewFocus(ViewFocusState.Focused, ViewFocusDirection.Undefined);
        }
        OnMetricsChanged();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ReportViewFocus(ViewFocusState.Unfocused, ViewFocusDirection.Undefined);
        PlatformDispatcher.Instance.ViewFocusChangeRequested -= HandleViewFocusChangeRequested;
        TextureRegistry.Instance.TextureFrameAvailable -= HandleTextureFrameAvailable;
        DetachMetricSources();
        DetachPlatformChannelHandler();
        DetachFeedbackListener();
        DetachTextInputPlugin();
        DetachMouseCursorListener();
        s_attachedHosts.Remove(this);
        if (!WidgetsBinding.IsImplicitView(_view))
        {
            PlatformDispatcher.Instance.RemoveView(_view);
        }

        if (s_attachedHosts.Count == 0)
        {
            WidgetsBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Detached);
        }

        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>
    /// Called when the host's size, scale or insets changed. Writes the new metrics to
    /// <see cref="RootFlutterView"/> and invokes <see cref="PlatformDispatcher.OnMetricsChanged"/>, as the
    /// engine's <c>_updateWindowMetrics</c> does: the renderer binding reconfigures the registered views
    /// and the widgets binding tells its observers.
    /// </summary>
    protected virtual void OnMetricsChanged()
    {
        MediaQueryData data = GetMediaQueryData();
        double scale = data.DevicePixelRatio;
        _view.UpdateMetrics(
            physicalSize: data.PhysicalSize,
            devicePixelRatio: scale,
            physicalConstraints: GetViewPhysicalConstraints(data),
            padding: ToViewPadding(data.Padding, scale),
            viewInsets: ToViewPadding(data.ViewInsets, scale),
            viewPadding: ToViewPadding(data.ViewPadding, scale),
            systemGestureInsets: ToViewPadding(data.SystemGestureInsets, scale),
            gestureSettings: data.GestureSettings,
            displayFeatures: data.DisplayFeatures,
            displayCornerRadii: data.DisplayCornerRadii);
        PlatformDispatcher.Instance.UpdateImplicitViewPlatformData(_view, data);
        PlatformDispatcher.Instance.OnMetricsChanged?.Invoke();
        ReportAccessibilityFeatures();
    }

    /// <summary>
    /// The physical constraints the view reports with new metrics; <see langword="null"/>, a tight
    /// constraint to the host's size, unless the host sizes itself to the view's content.
    /// </summary>
    private protected virtual BoxConstraints? GetViewPhysicalConstraints(MediaQueryData data) => null;

    /// <summary>
    /// Called after <see cref="FlutterView.Render"/> handed this host a scene of the given physical size.
    /// </summary>
    private protected virtual void OnViewRendered(Size physicalSize)
    {
    }

    /// <summary>
    /// The platform theme changed: reports the new brightness through
    /// <see cref="PlatformDispatcher.NotifyPlatformBrightnessChanged"/> when it differs (the engine's
    /// <c>_updateUserSettingsData</c>), and the high-contrast bit through the accessibility features.
    /// </summary>
    private void OnPlatformSettingsChanged()
    {
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        Brightness previous = dispatcher.PlatformBrightness;
        dispatcher.UpdateImplicitViewPlatformData(_view, GetMediaQueryData());
        if (WidgetsBinding.IsImplicitView(_view) && dispatcher.PlatformBrightness != previous)
        {
            dispatcher.NotifyPlatformBrightnessChanged();
        }

        ReportAccessibilityFeatures();
    }

    /// <summary>
    /// Reports the platform's accessibility features to the dispatcher, as the engine's
    /// <c>_updateAccessibilityFeatures</c> does. Avalonia exposes only the high-contrast theme
    /// variant, so that is the one bit this host reads, unless an embedder reported it through
    /// <see cref="UpdatePlatformAccessibilityFeatures"/>; every other bit keeps the value last reported.
    /// </summary>
    private void ReportAccessibilityFeatures()
    {
        if (_attachedTopLevel is null)
        {
            return;
        }

        PublishAccessibilityFeatures(ResolveHighContrast());
    }

    /// <summary>Logical insets to the physical pixels a <see cref="FlutterView"/> reports.</summary>
    private static Thickness ToViewPadding(Thickness logical, double devicePixelRatio)
    {
        return new Thickness(
            logical.Left * devicePixelRatio,
            logical.Top * devicePixelRatio,
            logical.Right * devicePixelRatio,
            logical.Bottom * devicePixelRatio);
    }

    /// <summary>
    /// Flutter's engine answers <c>PlatformDispatcher.requestViewFocusChange</c> by moving native
    /// focus to the view's window; the host that owns the view takes keyboard focus.
    /// </summary>
    private void HandleViewFocusChangeRequested(ViewFocusEvent @event)
    {
        if (@event.ViewId != _view.ViewId || @event.State != ViewFocusState.Focused)
        {
            return;
        }

        _requestedViewFocusDirection = @event.Direction;
        try
        {
            if (_attachedTopLevel is WindowBase window && !window.IsActive)
            {
                window.Activate();
            }

            if (!IsFocused)
            {
                KeyModifiers modifiers = @event.Direction == ViewFocusDirection.Backward
                    ? KeyModifiers.Shift
                    : KeyModifiers.None;
                Focus(NavigationMethod.Tab, modifiers);
            }

            if (IsFocused && (_attachedTopLevel is not WindowBase activeWindow || activeWindow.IsActive))
            {
                ReportViewFocus(ViewFocusState.Focused, @event.Direction);
            }
        }
        finally
        {
            _requestedViewFocusDirection = ViewFocusDirection.Undefined;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// The platform's half of view focus: the host reports keyboard focus and the containing window's
    /// activation state. Avalonia's Tab navigation and Shift modifier identify the entry direction.
    /// </remarks>
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        if (e.Source is not null && !ReferenceEquals(e.Source, this))
        {
            return;
        }

        ViewFocusDirection direction = _requestedViewFocusDirection != ViewFocusDirection.Undefined
            ? _requestedViewFocusDirection
            : e.NavigationMethod == NavigationMethod.Tab
                ? e.KeyModifiers.HasFlag(KeyModifiers.Shift)
                    ? ViewFocusDirection.Backward
                    : ViewFocusDirection.Forward
                : ViewFocusDirection.Undefined;
        ReportViewFocus(ViewFocusState.Focused, direction);
    }

    /// <inheritdoc />
    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        if (e.Source is not null && !ReferenceEquals(e.Source, this))
        {
            return;
        }

        ReportViewFocus(ViewFocusState.Unfocused, ViewFocusDirection.Undefined);
    }

    private void ReportViewFocus(ViewFocusState state, ViewFocusDirection direction)
    {
        bool focused = state == ViewFocusState.Focused;
        if (_viewHasFocus == focused)
        {
            return;
        }

        _viewHasFocus = focused;
        WidgetsBinding.Instance.HandleViewFocusChanged(new ViewFocusEvent(_view.ViewId, state, direction));
    }

    /// <summary>The platform text input plugin this host forwards its IME and unhandled keys to.
    /// </summary>
    protected virtual HostTextInputPlugin TextInputPlugin => HostTextInputPlugin.Instance;

    /// <summary>Whether the plugin has a client attached that edits in this host's view.</summary>
    private bool HasTextInputClient =>
        TextInputPlugin.ActiveModel is not null && TextInputPlugin.ViewId == _view.ViewId;

    private protected void AttachTextInputPlugin()
    {
        if (_isAttachedToTextInputPlugin)
        {
            return;
        }

        _isAttachedToTextInputPlugin = true;
        HostTextInputPlugin plugin = TextInputPlugin;
        plugin.ClientChanged += HandleTextInputClientChanged;
        plugin.EditingStateChanged += _textInputClient.NotifyEditingStateChanged;
        plugin.CaretRectChanged += _textInputClient.NotifyCursorRectangleChanged;
        plugin.ShowRequested += HandleTextInputShowRequested;
        plugin.MarkedTextDiscarded += HandleTextInputMarkedTextDiscarded;
        plugin.Attach();
        HandleTextInputClientChanged();
    }

    private protected void DetachTextInputPlugin()
    {
        if (!_isAttachedToTextInputPlugin)
        {
            return;
        }

        _isAttachedToTextInputPlugin = false;
        HostTextInputPlugin plugin = TextInputPlugin;
        plugin.ClientChanged -= HandleTextInputClientChanged;
        plugin.EditingStateChanged -= _textInputClient.NotifyEditingStateChanged;
        plugin.CaretRectChanged -= _textInputClient.NotifyCursorRectangleChanged;
        plugin.ShowRequested -= HandleTextInputShowRequested;
        plugin.MarkedTextDiscarded -= HandleTextInputMarkedTextDiscarded;
        plugin.Detach();
    }

    /// <summary>
    /// The attached client changed: the IME options follow its configuration, and Avalonia re-queries
    /// this host's IME client, which exists only while a client edits in this view.
    /// </summary>
    private void HandleTextInputClientChanged()
    {
        IDictionary? configuration = HasTextInputClient ? TextInputPlugin.Configuration : null;
        string? inputType = TextInputPlugin.InputType;
        TextInputOptions.SetContentType(
            this,
            configuration is not null ? ResolveContentType(inputType) : TextInputContentType.Normal);
        TextInputOptions.SetReturnKeyType(
            this,
            configuration is not null
                ? ResolveReturnKeyType(TextInputPlugin.InputAction)
                : TextInputReturnKeyType.Default);
        TextInputOptions.SetMultiline(this, configuration is not null && inputType == "TextInputType.multiline");
        TextInputOptions.SetIsSensitive(this, configuration?["obscureText"] is true);
        TextInputOptions.SetShowSuggestions(this, configuration?["enableSuggestions"] as bool?);
        RaiseEvent(new TextInputMethodClientRequeryRequestedEventArgs
        {
            RoutedEvent = InputMethod.TextInputMethodClientRequeryRequestedEvent,
        });
        _textInputClient.RefreshOptions();
    }

    private void HandleTextInputShowRequested()
    {
        if (HasTextInputClient)
        {
            _textInputClient.RequestInputPane();
        }
    }

    private void HandleTextInputMarkedTextDiscarded()
    {
        if (HasTextInputClient)
        {
            _textInputClient.RefreshOptions();
        }
    }

    private static TextInputContentType ResolveContentType(string? inputType)
    {
        return inputType switch
        {
            "TextInputType.number" => TextInputContentType.Number,
            "TextInputType.phone" => TextInputContentType.Digits,
            "TextInputType.emailAddress" => TextInputContentType.Email,
            "TextInputType.url" => TextInputContentType.Url,
            _ => TextInputContentType.Normal,
        };
    }

    private static TextInputReturnKeyType ResolveReturnKeyType(string? inputAction)
    {
        return inputAction switch
        {
            "TextInputAction.none" => TextInputReturnKeyType.Return,
            "TextInputAction.search" => TextInputReturnKeyType.Search,
            "TextInputAction.done" => TextInputReturnKeyType.Done,
            "TextInputAction.go" => TextInputReturnKeyType.Go,
            "TextInputAction.next" => TextInputReturnKeyType.Next,
            "TextInputAction.send" => TextInputReturnKeyType.Send,
            _ => TextInputReturnKeyType.Default,
        };
    }

    protected MediaQueryData GetMediaQueryData()
    {
        var viewPadding = _insetsManager?.SafeAreaPadding ?? default;
        // Avalonia assigns Bounds after ArrangeOverride returns; metrics published during that
        // callback must use its finalSize, including the first layout and subsequent resizes.
        var size = _lastArrangedSize;
        Point origin = _attachedTopLevel is null ? default : this.TranslatePoint(default, _attachedTopLevel) ?? default;
        var viewInsets = ResolveViewInsets(
            _inputPane?.State ?? InputPaneState.Closed, _inputPane?.OccludedRect ?? default, new Rect(origin, size));
        double scale = _attachedTopLevel?.RenderScaling ?? 1.0;
        if (!double.IsFinite(scale) || scale <= 0)
        {
            scale = 1.0;
        }

        return new MediaQueryData(
            Size: size,
            DevicePixelRatio: scale,
            Padding: MediaQueryData.ComputePadding(viewPadding, viewInsets),
            ViewInsets: viewInsets,
            ViewPadding: viewPadding,
            PlatformBrightness: ResolvePlatformBrightness(),
            ViewId: _view.ViewId,
            // `DeviceGestureSettings.fromView`: the view's physical touch slop in logical pixels.
            GestureSettings: s_physicalTouchSlop is { } touchSlop
                ? new DeviceGestureSettings(TouchSlop: touchSlop / scale)
                : null);
    }

    protected void ScheduleVisualUpdate()
    {
        SchedulerBinding.Instance.EnsureVisualUpdate();
    }

    /// <summary>
    /// Performs a semantics action the platform sent for a node of this host's view, and reports
    /// whether a handler ran.
    /// </summary>
    /// <remarks>
    /// The engine's <c>onSemanticsActionEvent</c> delivery: the <c>SemanticsBinding</c> action
    /// listeners see the action first, then this host's semantics owner performs it — which is where
    /// <c>RendererBinding.PerformSemanticsAction</c> routes an action for this view.
    /// </remarks>
    public bool PerformSemanticsAction(int nodeId, SemanticsActions action)
    {
        return PerformSemanticsActionCore(nodeId, action, arguments: null);
    }

    /// <summary>
    /// The box of the semantics node <paramref name="nodeId"/> in the coordinate space of the view
    /// <paramref name="viewId"/>, in logical pixels, or <c>null</c> when either is unknown.
    /// </summary>
    /// <remarks>
    /// C#-only convenience over this host's own pipeline owner, with the root-transform undo of
    /// <see cref="RendererBinding.GetRectOfSemanticsNodeInViewCoordinates"/>. It is a query rather
    /// than the semantics-action path, so another view or an unknown node answers <c>null</c> where
    /// the binding's lookup asserts.
    /// </remarks>
    public Rect? GetRectOfSemanticsNodeInViewCoordinates(int viewId, int nodeId)
    {
        if (viewId != _view.ViewId)
        {
            return null;
        }

        // The node's ancestor walk ends in the root's device-pixel-ratio transform; undo it exactly
        // as RendererBinding.GetRectOfSemanticsNodeInViewCoordinates does.
        if (_pipeline.SemanticsOwner?.GetRectOfSemanticsNode(nodeId) is not { } rect)
        {
            return null;
        }

        Matrix4? rootInverse = _root.HasConfiguration ? Matrix4.TryInvert(_root.Configuration.ToMatrix()) : null;
        return rootInverse is null ? rect : MatrixUtils.TransformRect(rootInverse, rect);
    }

    public bool PerformCustomSemanticsAction(int nodeId, CustomSemanticsAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return PerformSemanticsActionCore(
            nodeId,
            SemanticsActions.CustomAction,
            CustomSemanticsAction.GetIdentifier(action));
    }

    private bool PerformSemanticsActionCore(int nodeId, SemanticsActions action, object? arguments)
    {
        // Like AccessibilityBridge.DispatchAction, an action for a node the framework no longer
        // holds never reaches the action listeners, which may look the node up.
        if (action == SemanticsActions.None || !RendererBinding.Instance.HasSemanticsNode(_view.ViewId, nodeId))
        {
            return false;
        }

        SemanticsBinding.Instance.NotifySemanticsActionListeners(
            new SemanticsActionEvent(action, _view.ViewId, nodeId, arguments));
        return _pipeline.SemanticsOwner?.PerformAction(nodeId, action, arguments) ?? false;
    }

    /// <summary>
    /// Lays the host out at <paramref name="viewport"/> (as an Avalonia arrange would) and pumps one
    /// binding frame, which builds, lays out, paints and composites this host's view.
    /// </summary>
    internal void PumpFrameForTests(Size? viewport = null)
    {
        if (viewport is { } size && !_lastArrangedSize.Equals(size))
        {
            _lastArrangedSize = size;
            OnMetricsChanged();
        }

        // A test that reset the scheduler dropped the renderer binding's persistent frame callback.
        RendererBinding.Instance.EnsurePersistentFrameCallback();
        SchedulerBinding.Instance.ScheduleForcedFrame();
        SchedulerBinding.PumpFrameForTests();
    }

    private void AttachMetricSources()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (ReferenceEquals(topLevel, _attachedTopLevel))
        {
            return;
        }

        DetachMetricSources();

        _attachedTopLevel = topLevel;
        if (_attachedTopLevel == null)
        {
            return;
        }

        if (_attachedTopLevel is WindowBase windowBase)
        {
            windowBase.Activated += HandleWindowActivated;
            windowBase.Deactivated += HandleWindowDeactivated;
        }

        if (_attachedTopLevel is Window window)
        {
            window.PropertyChanged += HandleWindowPropertyChanged;
            if (!IsWindowingView)
            {
                window.Closing += HandleWindowClosing;
            }
        }

        if (!IsWindowingView)
        {
            ApplyApplicationSwitcherDescription();
        }

        _attachedTopLevel.ActualThemeVariantChanged += HandleActualThemeVariantChanged;
        HostSystemFonts.Attach(_attachedTopLevel);
        if (s_listensToSystemBack)
        {
            _attachedTopLevel.BackRequested += HandleTopLevelBackRequested;
        }

        _insetsManager = _attachedTopLevel.InsetsManager;
        if (_insetsManager != null)
        {
            _insetsManager.SafeAreaChanged += HandleSafeAreaChanged;
            ApplySystemUiOverlayStyle();
        }

        _inputPane = _attachedTopLevel.InputPane;
        if (_inputPane != null)
        {
            _inputPane.StateChanged += HandleInputPaneStateChanged;
        }

    }

    private void DetachMetricSources()
    {
        if (_attachedTopLevel is WindowBase windowBase)
        {
            windowBase.Activated -= HandleWindowActivated;
            windowBase.Deactivated -= HandleWindowDeactivated;
        }

        if (_attachedTopLevel is Window window)
        {
            window.PropertyChanged -= HandleWindowPropertyChanged;
            window.Closing -= HandleWindowClosing;
        }

        if (_insetsManager != null)
        {
            _insetsManager.SafeAreaChanged -= HandleSafeAreaChanged;
        }

        if (_inputPane != null)
        {
            _inputPane.StateChanged -= HandleInputPaneStateChanged;
        }

        if (_attachedTopLevel != null)
        {
            _attachedTopLevel.ActualThemeVariantChanged -= HandleActualThemeVariantChanged;
            _attachedTopLevel.BackRequested -= HandleTopLevelBackRequested;
        }

        _attachedTopLevel = null;
        _insetsManager = null;
        _inputPane = null;
    }

    private void HandleWindowActivated(object? sender, EventArgs e)
    {
        AppLifecycleState state = sender is Window { WindowState: WindowState.Minimized }
            ? AppLifecycleState.Hidden
            : AppLifecycleState.Resumed;
        WidgetsBinding.Instance.HandleAppLifecycleStateChanged(state);
        UpdateViewFocusForWindowActivation(active: state == AppLifecycleState.Resumed);
        // Avalonia has no locale-change notification; the user changes the system languages in another
        // window, so the preferred languages (and the culture) are re-read when this one comes back.
        System.Globalization.CultureInfo.CurrentCulture.ClearCachedData();
        ReportLocales();
    }

    /// <summary>
    /// Reports the platform's preferred locale list, as the engine's <c>_updateLocales</c> does:
    /// the list an embedder reported through <see cref="UpdatePlatformLocales"/>, else the desktop OS's
    /// preferred languages (<see cref="DesktopLocales"/>), else the system UI culture.
    /// <see cref="PlatformDispatcher.UpdateLocales"/> notifies only a change.
    /// </summary>
    /// <remarks>
    /// The mobile embedders (Android, iOS) report their list themselves; the desktop list is read again on
    /// every call, which the host makes when its window is activated.
    /// </remarks>
    internal static void ReportLocales()
    {
        PlatformDispatcher.Instance.UpdateLocales(
            s_platformLocales
            ?? DesktopLocales.Read()
            ?? [Locale.FromCultureInfo(System.Globalization.CultureInfo.CurrentUICulture)]);
    }

    // The OS's ordered preferred-language list, once an embedder reported one; null until then.
    private static IReadOnlyList<Locale>? s_platformLocales;

    /// <summary>
    /// Reports the platform's preferred locales, in the user's order of preference, as the engine's
    /// localization <c>setLocale</c> message does, and notifies the framework when they changed.
    /// </summary>
    /// <remarks>
    /// An embedder that can read the OS's preferred-language list calls this at start-up and whenever the
    /// list changes (Android's <c>Configuration.getLocales</c> through <see cref="AndroidActivityDelegate"/>,
    /// iOS's <c>NSLocale.preferredLanguages</c> on <c>NSCurrentLocaleDidChangeNotification</c>). From then on
    /// the hosts report this list instead of the system UI culture. An empty list is ignored, as the iOS
    /// engine sends none.
    /// </remarks>
    public static void UpdatePlatformLocales(IReadOnlyList<Locale> locales)
    {
        ArgumentNullException.ThrowIfNull(locales);
        if (locales.Count == 0)
        {
            return;
        }

        s_platformLocales = [.. locales];
        ReportLocales();
    }

    /// <summary>Forgets the locale list an embedder reported. Test-only.</summary>
    internal static void ResetPlatformLocalesForTests() => s_platformLocales = null;

    internal void UpdateViewFocusForWindowActivation(bool active)
    {
        if (!active)
        {
            ReportViewFocus(ViewFocusState.Unfocused, ViewFocusDirection.Undefined);
            return;
        }

        if (IsFocused)
        {
            ReportViewFocus(ViewFocusState.Focused, _requestedViewFocusDirection);
        }
    }

    private void HandleWindowDeactivated(object? sender, EventArgs e)
    {
        UpdateViewFocusForWindowActivation(active: false);
        AppLifecycleState state = sender is Window { WindowState: WindowState.Minimized }
            ? AppLifecycleState.Hidden
            : AppLifecycleState.Inactive;
        WidgetsBinding.Instance.HandleAppLifecycleStateChanged(state);
    }

    private static void HandleWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Window.WindowStateProperty || sender is not Window window)
        {
            return;
        }

        WidgetsBinding.Instance.HandleAppLifecycleStateChanged(
            window.WindowState == WindowState.Minimized
                ? AppLifecycleState.Hidden
                : window.IsActive
                    ? AppLifecycleState.Resumed
                    : AppLifecycleState.Inactive);
    }

    private async void HandleWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowWindowClose || sender is not Window window)
        {
            return;
        }

        e.Cancel = true;
        AppExitResponse response = await WidgetsBinding.Instance.HandleRequestAppExit();
        if (response == AppExitResponse.Exit)
        {
            _allowWindowClose = true;
            window.Close();
        }
    }

    private void HandleSafeAreaChanged(object? sender, SafeAreaChangedArgs e)
    {
        OnMetricsChanged();
    }

    private void HandleInputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        OnMetricsChanged();
    }

    private void HandleActualThemeVariantChanged(object? sender, EventArgs e)
    {
        OnPlatformSettingsChanged();
    }

    private Brightness ResolvePlatformBrightness()
    {
        return _attachedTopLevel?.ActualThemeVariant == ThemeVariant.Dark
            ? Brightness.Dark
            : Brightness.Light;
    }

    private bool ResolveHighContrast()
    {
        string? themeKey = _attachedTopLevel?.ActualThemeVariant.Key.ToString();
        return themeKey?.Contains("HighContrast", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Registers the platform side of Flutter's `flutter/mousecursor` channel. Dart's engine
    /// implements `activateSystemCursor`; here it maps the cursor kind onto Avalonia's cursor set.
    /// </summary>
    private void AttachMouseCursorListener()
    {
        if (_isSubscribedToMouseCursor)
        {
            return;
        }

        // The engine's cursor is process-wide; every attached host shows the cursor last activated.
        if (!s_isMouseCursorHandlerInstalled)
        {
            SystemChannels.MouseCursor.SetPlatformMethodCallHandler(HandleMouseCursorMethodCall);
            s_isMouseCursorHandlerInstalled = true;
        }

        PlumixHost? current = s_attachedHosts.Find(host => host != this && host._isSubscribedToMouseCursor);
        if (current is not null)
        {
            _currentMouseCursorKind = current._currentMouseCursorKind;
        }

        _isSubscribedToMouseCursor = true;
        ApplyMouseCursor();
    }

    private void DetachMouseCursorListener()
    {
        if (!_isSubscribedToMouseCursor)
        {
            return;
        }

        _isSubscribedToMouseCursor = false;
        Cursor = null;
        if (!s_attachedHosts.Exists(host => host != this && host._isSubscribedToMouseCursor))
        {
            SystemChannels.MouseCursor.SetPlatformMethodCallHandler(null);
            s_isMouseCursorHandlerInstalled = false;
        }
    }

    private static Task<object?> HandleMouseCursorMethodCall(MethodCall call)
    {
        if (call.Method != "activateSystemCursor")
        {
            throw new MissingPluginException($"{call.Method} is not implemented on flutter/mousecursor.");
        }

        // The standard codec decodes a map as `Dictionary<object, object?>`, so the arguments are
        // read through the non-generic interface.
        string kind = call.Arguments is System.Collections.IDictionary arguments
            ? arguments["kind"] as string ?? SystemMouseCursors.Basic.Kind
            : SystemMouseCursors.Basic.Kind;

        foreach (PlumixHost host in s_attachedHosts)
        {
            if (host._isSubscribedToMouseCursor)
            {
                host._currentMouseCursorKind = kind;
                host.ApplyMouseCursor();
            }
        }

        return Task.FromResult<object?>(null);
    }

    private void ApplyMouseCursor()
    {
        Cursor = ResolveAvaloniaCursor(_currentMouseCursorKind);
    }

    /// <summary>
    /// Maps a Flutter cursor kind onto the closest Avalonia standard cursor. Avalonia has no
    /// counterpart for a few of Flutter's kinds, which fall back to the arrow.
    /// </summary>
    private static Cursor ResolveAvaloniaCursor(string kind)
    {
        return kind switch
        {
            "none" => new Cursor(StandardCursorType.None),
            "click" => new Cursor(StandardCursorType.Hand),
            "forbidden" or "noDrop" => new Cursor(StandardCursorType.No),
            "wait" => new Cursor(StandardCursorType.Wait),
            "progress" => new Cursor(StandardCursorType.AppStarting),
            "help" => new Cursor(StandardCursorType.Help),
            "text" => new Cursor(StandardCursorType.Ibeam),
            "precise" or "cell" => new Cursor(StandardCursorType.Cross),
            "move" or "allScroll" or "grab" or "grabbing" => new Cursor(StandardCursorType.SizeAll),
            "alias" or "copy" => new Cursor(StandardCursorType.DragCopy),
            "resizeLeftRight" or "resizeColumn" or "resizeLeft" or "resizeRight" =>
                new Cursor(StandardCursorType.SizeWestEast),
            "resizeUpDown" or "resizeRow" or "resizeUp" or "resizeDown" =>
                new Cursor(StandardCursorType.SizeNorthSouth),
            "resizeUpLeftDownRight" or "resizeUpLeft" or "resizeDownRight" =>
                new Cursor(StandardCursorType.TopLeftCorner),
            "resizeUpRightDownLeft" or "resizeUpRight" or "resizeDownLeft" =>
                new Cursor(StandardCursorType.TopRightCorner),
            _ => new Cursor(StandardCursorType.Arrow),
        };
    }

    private void AttachFeedbackListener()
    {
        if (_isSubscribedToFeedback)
        {
            return;
        }

        Feedback.FeedbackTriggered += HandleFeedbackTriggered;
        _isSubscribedToFeedback = true;
    }

    private void DetachFeedbackListener()
    {
        if (!_isSubscribedToFeedback)
        {
            return;
        }

        Feedback.FeedbackTriggered -= HandleFeedbackTriggered;
        _isSubscribedToFeedback = false;
    }

    private void HandleFeedbackTriggered(FeedbackType type)
    {
        OnFrameworkFeedback(type);
    }

    protected virtual void OnFrameworkFeedback(FeedbackType type)
    {
    }

    /// <summary>
    /// Registers this host as the platform side of <c>flutter/platform</c>. Flutter's platform side is
    /// the engine; in Plumix the host adapter answers the channel directly.
    /// </summary>
    private protected void AttachPlatformChannelHandler()
    {
        if (_isSubscribedToPlatformChannel)
        {
            return;
        }

        // A windowing view never takes the channel from a host that answers it already.
        if (IsWindowingView && s_platformChannelHost is not null)
        {
            return;
        }

        if (s_platformChannelHost is { } previous && previous != this)
        {
            previous._isSubscribedToPlatformChannel = false;
        }

        SystemChannels.Platform.SetPlatformMethodCallHandler(HandlePlatformMethodCall);
        s_platformChannelHost = this;
        _isSubscribedToPlatformChannel = true;
    }

    private protected void DetachPlatformChannelHandler()
    {
        if (!_isSubscribedToPlatformChannel)
        {
            return;
        }

        SystemChannels.Platform.SetPlatformMethodCallHandler(null);
        _isSubscribedToPlatformChannel = false;
        s_platformChannelHost = null;
        // The next attached host answers the channel from here on.
        s_attachedHosts.Find(host => host != this)?.AttachPlatformChannelHandler();
    }

    private Task<object?> HandlePlatformMethodCall(MethodCall call)
    {
        switch (call.Method)
        {
            case "Clipboard.setData":
                return SetClipboardDataAsync(call.Arguments);
            case "Clipboard.getData":
                return GetClipboardDataAsync(call.Arguments);
            case "Clipboard.hasStrings":
                return HasClipboardStringsAsync(call.Arguments);
            case "SystemSound.play":
                OnFrameworkSystemSound(ParseSystemSoundType(call.Arguments as string));
                return Task.FromResult<object?>(null);
            case "HapticFeedback.vibrate":
                OnFrameworkHapticFeedback(call.Arguments as string);
                return Task.FromResult<object?>(null);
            case "SystemNavigator.setFrameworkHandlesBack":
                s_frameworkHandlesBack = call.Arguments is true;
                s_reportedFrameworkHandlesBack = s_frameworkHandlesBack;
                FrameworkHandlesBackChanged?.Invoke(s_frameworkHandlesBack);
                return Task.FromResult<object?>(null);
            case "SystemNavigator.pop":
                return PopSystemNavigator(call.Arguments);
            case "SystemChrome.setSystemUIOverlayStyle":
                _currentSystemUiOverlayStyle =
                    SystemUiOverlayStyle.FromMap((System.Collections.IDictionary)call.Arguments!);
                ApplySystemUiOverlayStyle();
                return Task.FromResult<object?>(null);
            case "SystemChrome.setApplicationSwitcherDescription":
                var description = (System.Collections.IDictionary)call.Arguments!;
                _currentApplicationSwitcherDescription = new ApplicationSwitcherDescription(
                    label: description["label"] as string,
                    primaryColor: description["primaryColor"] switch
                    {
                        int value => value,
                        long value => value,
                        _ => null,
                    });
                ApplyApplicationSwitcherDescription();
                return Task.FromResult<object?>(null);
            default:
                // Dart: "calls to methods that are not implemented on the shell side are ignored".
                throw new MissingPluginException(call.Method);
        }
    }

    private async Task<object?> SetClipboardDataAsync(object? arguments)
    {
        string? text = arguments is System.Collections.IDictionary data ? data["text"] as string : null;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(text);
        }

        return null;
    }

    private async Task<object?> GetClipboardDataAsync(object? format)
    {
        if (format is not "text/plain")
        {
            return null;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        string? text = clipboard is null ? null : await clipboard.TryGetTextAsync();
        return text is null ? null : new Dictionary<string, object?> { ["text"] = text };
    }

    private async Task<object?> HasClipboardStringsAsync(object? format)
    {
        if (format is not "text/plain")
        {
            return new Dictionary<string, object?> { ["value"] = false };
        }

        IClipboard? clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        using IAsyncDataTransfer? data = clipboard is null ? null : await clipboard.TryGetDataAsync();
        return new Dictionary<string, object?> { ["value"] = OffersStrings(data) };
    }

    /// <summary>
    /// Whether the clipboard offers text, answered from its formats alone, as the engine answers
    /// from <c>UIPasteboard.hasStrings</c> and <c>ClipboardManager.hasPrimaryClip</c>. Reading the
    /// text instead shows iOS's paste-permission alert, and every <c>EditableText</c> asks on mount.
    /// </summary>
    internal static bool OffersStrings(IAsyncDataTransfer? data) =>
        data is not null && data.Formats.Any(format => format is DataFormat<string>);

    private static SystemSoundType ParseSystemSoundType(string? type) => type switch
    {
        "SystemSoundType.tick" => SystemSoundType.Tick,
        "SystemSoundType.alert" => SystemSoundType.Alert,
        _ => SystemSoundType.Click,
    };

    /// <summary>
    /// Receives <c>SystemSound.play</c>: runs <see cref="SystemSoundHandler"/>, or the desktop embedders'
    /// answer when no platform host installed one.
    /// </summary>
    protected virtual void OnFrameworkSystemSound(SystemSoundType type)
    {
        if (SystemSoundHandler is { } playSystemSound)
        {
            playSystemSound(type);
        }
        else
        {
            DesktopPlatformPlugin.PlaySystemSound(type);
        }
    }

    /// <summary>
    /// Receives <c>HapticFeedback.vibrate</c>. <paramref name="type"/> is Dart's
    /// <c>HapticFeedbackType</c> string (for example <c>HapticFeedbackType.mediumImpact</c>), or
    /// <c>null</c> for the plain system default. Runs <see cref="HapticFeedbackHandler"/>.
    /// </summary>
    protected virtual void OnFrameworkHapticFeedback(string? type)
    {
        HapticFeedbackHandler?.Invoke(type);
    }

    private void ApplyApplicationSwitcherDescription()
    {
        if (_currentApplicationSwitcherDescription == null)
        {
            return;
        }

        if (_attachedTopLevel is Window window)
        {
            window.Title = _currentApplicationSwitcherDescription.Label ?? string.Empty;
        }

        OnFrameworkApplicationSwitcherDescription(_currentApplicationSwitcherDescription);
    }

    protected virtual void OnFrameworkApplicationSwitcherDescription(
        ApplicationSwitcherDescription description)
    {
    }

    private void ApplySystemUiOverlayStyle()
    {
        if (_insetsManager == null)
        {
            return;
        }

        var style = _currentSystemUiOverlayStyle;
        bool shouldDisplayEdgeToEdge = ShouldDisplayEdgeToEdge(style);
        if (_insetsManager.DisplayEdgeToEdgePreference != shouldDisplayEdgeToEdge)
        {
            _insetsManager.DisplayEdgeToEdgePreference = shouldDisplayEdgeToEdge;
        }

        var fallbackSystemBarColor = style.StatusBarColor ?? style.SystemNavigationBarColor;
        if (fallbackSystemBarColor != null)
        {
            if (_attachedTopLevel != null)
            {
                TopLevel.SetSystemBarColor(_attachedTopLevel, new SolidColorBrush(fallbackSystemBarColor!));
            }

            _insetsManager.SystemBarColor = fallbackSystemBarColor!;
        }

        var iconBrightness = style.StatusBarIconBrightness ?? style.SystemNavigationBarIconBrightness;
        if (iconBrightness.HasValue)
        {
            var systemBarTheme = iconBrightness.Value == Brightness.Dark
                ? SystemBarTheme.Light
                : SystemBarTheme.Dark;
            TrySetInsetsManagerSystemBarTheme(_insetsManager, systemBarTheme);
        }

        TryApplyAndroidSystemBarColors(_insetsManager, style.StatusBarColor, style.SystemNavigationBarColor);
    }

    private static bool ShouldDisplayEdgeToEdge(SystemUiOverlayStyle style)
    {
        static bool IsTransparentOrUnset(Color? color) => color == null || color!.Alpha == 0;

        return IsTransparentOrUnset(style.StatusBarColor)
               && IsTransparentOrUnset(style.SystemNavigationBarColor);
    }

    private static void TrySetInsetsManagerSystemBarTheme(IInsetsManager insetsManager, SystemBarTheme theme)
    {
        var property = insetsManager.GetType().GetProperty(
            "SystemBarTheme",
            BindingFlags.Instance | BindingFlags.Public);

        if (property?.CanWrite != true)
        {
            return;
        }

        if (property.PropertyType == typeof(SystemBarTheme))
        {
            property.SetValue(insetsManager, theme);
            return;
        }

        if (property.PropertyType == typeof(SystemBarTheme?))
        {
            property.SetValue(insetsManager, theme);
        }
    }

    private static void TryApplyAndroidSystemBarColors(
        IInsetsManager insetsManager,
        Color? statusBarColor,
        Color? navigationBarColor)
    {
        if (statusBarColor == null && navigationBarColor == null)
        {
            return;
        }

        var activityField = insetsManager.GetType().GetField(
            "_activity",
            BindingFlags.Instance | BindingFlags.NonPublic);
        object? activity = activityField?.GetValue(insetsManager);
        if (activity == null)
        {
            return;
        }

        var windowProperty = activity.GetType().GetProperty(
            "Window",
            BindingFlags.Instance | BindingFlags.Public);
        object? window = windowProperty?.GetValue(activity);
        if (window == null)
        {
            return;
        }

        if (statusBarColor != null)
        {
            TrySetAndroidWindowColor(window, "SetStatusBarColor", statusBarColor!);
        }

        if (navigationBarColor != null)
        {
            TrySetAndroidWindowColor(window, "SetNavigationBarColor", navigationBarColor!);
        }
    }

    private static void TrySetAndroidWindowColor(object window, string methodName, Color color)
    {
        var method = window.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(candidate =>
                string.Equals(candidate.Name, methodName, StringComparison.Ordinal)
                && candidate.GetParameters().Length == 1);
        if (method == null)
        {
            return;
        }

        var parameterType = method.GetParameters()[0].ParameterType;
        object? argument = CreateAndroidColorArgument(parameterType, color);
        if (argument == null)
        {
            return;
        }

        method.Invoke(window, [argument]);
    }

    private static object? CreateAndroidColorArgument(Type parameterType, Color color)
    {
        int argb = unchecked((int)ToArgb(color));
        if (parameterType == typeof(int))
        {
            return argb;
        }

        var constructor = parameterType.GetConstructor([typeof(int)]);
        if (constructor != null)
        {
            return constructor.Invoke([argb]);
        }

        return null;
    }

    private static uint ToArgb(Color color)
    {
        return color.ToARGB32();
    }

    /// <summary>
    /// The keyboard inset is the part of the view the input pane covers, measured from the view's
    /// bottom, as the engine computes <c>viewInsets.bottom</c> from the keyboard's frame. The pane's
    /// rect alone is not it: on iOS a hidden keyboard keeps its full-height frame below the screen, and
    /// with a hardware keyboard only the shortcut bar of an "open" one is on screen.
    /// </summary>
    /// <param name="state">The input pane's state; a closed pane covers nothing.</param>
    /// <param name="occludedRect">The pane's rect in top-level coordinates.</param>
    /// <param name="hostRect">The view's rect in top-level coordinates.</param>
    internal static Thickness ResolveViewInsets(InputPaneState state, Rect occludedRect, Rect hostRect)
    {
        if (state != InputPaneState.Open || occludedRect.Height <= 0 || hostRect.Height <= 0)
        {
            return default;
        }

        Rect covered = hostRect.Intersect(occludedRect);
        return covered.Height <= 0
            ? default
            : new Thickness(0, 0, 0, Math.Clamp(hostRect.Bottom - covered.Top, 0.0, hostRect.Height));
    }

    /// <summary>
    /// Reports a pointer event the way an engine embedder does: as physical-pixel pointer data in a
    /// packet to <see cref="PlatformDispatcher.OnPointerDataPacket"/>, which the gesture binding converts
    /// back and hit-tests in the view named by the entry's view id.
    /// </summary>
    private void DispatchPointerEvent(PointerEvent @event)
    {
        _pointerEncoder.Report(@event, _view.ViewId, _view.DevicePixelRatio);
    }

    /// <summary>
    /// Asks the framework whether a platform view lies under <paramref name="position"/>, the query
    /// the engine makes through <see cref="PlatformDispatcher.OnHitTest"/> before it hands a touch
    /// sequence to a native view, and publishes the answer as <see cref="PlatformViewHitTested"/>.
    /// </summary>
    internal void QueryNativeHitTest(Point position)
    {
        HitTestResponse? response = PlatformDispatcher.Instance.OnHitTest?.Invoke(
            new HitTestRequest(_view.ViewId, position));
        if (response is { } answer)
        {
            PlatformViewHitTested?.Invoke(position, answer.HasPlatformView);
        }
    }

    /// <summary>
    /// Raised when a pointer goes down, with its position and whether the framework's hit-test path there
    /// contains a native platform view (the engine's <c>HitTestResponse.hasPlatformView</c>).
    /// </summary>
    public event Action<Point, bool>? PlatformViewHitTested;

    /// <summary>
    /// Republishes every semantics update of this host's view, which <c>RendererBinding.DrawFrame</c>'s
    /// flush of the pipeline owner tree sends through <see cref="FlutterView.UpdateSemantics"/>.
    /// </summary>
    private void HandleViewSemanticsUpdated(SemanticsUpdate update)
    {
        AccessibilityBridge.AddSemanticsUpdate(update);
        SemanticsUpdateProduced?.Invoke(update);
        SemanticsUpdated?.Invoke(_pipeline.SemanticsOwner?.RootNode);
    }

    /// <summary>
    /// <c>FlutterView.render</c> for this host: takes the scene's layer tree, ends the frame's build phase
    /// on its timings recorder (the engine's <c>Animator::Render</c>), and asks Avalonia to rasterize it.
    /// </summary>
    private void HandleViewRenderRequested(Scene scene, Size? physicalSize)
    {
        ContainerFlowLayer? layerTree = scene.TakeLayerTree();
        if (layerTree == null)
        {
            return;
        }

        double devicePixelRatio = _pipeline.RootNode is RenderView { HasConfiguration: true } renderView
            ? renderView.Configuration.DevicePixelRatio
            : _view.DevicePixelRatio;
        Size physical = physicalSize ?? new Size(Bounds.Width * devicePixelRatio, Bounds.Height * devicePixelRatio);
        _layerTree = layerTree;
        _layerTreeTransform = devicePixelRatio > 0.0
            ? Matrix.CreateScale(1.0 / devicePixelRatio, 1.0 / devicePixelRatio)
            : Matrix.Identity;
        _layerTreeTargetSize = devicePixelRatio > 0.0
            ? new Size(physical.Width / devicePixelRatio, physical.Height / devicePixelRatio)
            : Bounds.Size;
        _pendingFrameTimings = FrameTimingsRecorder.ForRender();
        OnViewRendered(physical);

        // The scheduler is process-wide and a frame can be pumped from any thread, while an Avalonia
        // control may only be touched from the thread that created it.
        if (ReferenceEquals(Thread.CurrentThread, _ownerThread))
        {
            InvalidateVisual();
        }
        else
        {
            Dispatcher.UIThread.Post(InvalidateVisual);
        }
    }

    private void HandleTextureFrameAvailable(long textureId)
    {
        if (ReferenceEquals(Thread.CurrentThread, _ownerThread))
        {
            InvalidateVisual();
        }
        else
        {
            Dispatcher.UIThread.Post(InvalidateVisual);
        }
    }

    private static bool IsReassembleShortcut(KeyEventArgs e)
    {
        return (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta))
               && e.KeyModifiers.HasFlag(KeyModifiers.Shift)
               && e.Key == Key.R;
    }

    private PointerDownEvent ToPointerDownEvent(PointerPressedEventArgs e)
    {
        return new PointerDownEvent(
            pointer: unchecked((int)e.Pointer.Id),
            kind: ToPointerKind(e.Pointer.Type),
            position: e.GetPosition(this),
            buttons: ToPointerButtons(e.GetCurrentPoint(this).Properties),
            timestampUtc: FrameClock.UtcNow);
    }

    private static PointerButtons ToPointerButtons(PointerPointProperties properties)
    {
        var buttons = PointerButtons.None;

        if (properties.IsLeftButtonPressed)
        {
            buttons |= PointerButtons.Primary;
        }

        if (properties.IsRightButtonPressed)
        {
            buttons |= PointerButtons.Secondary;
        }

        if (properties.IsMiddleButtonPressed)
        {
            buttons |= PointerButtons.MiddleMouse;
        }

        if (properties.IsXButton1Pressed)
        {
            buttons |= PointerButtons.BackMouse;
        }

        if (properties.IsXButton2Pressed)
        {
            buttons |= PointerButtons.ForwardMouse;
        }

        return buttons;
    }

    private static PointerDeviceKind ToPointerKind(PointerType type)
    {
        return type switch
        {
            PointerType.Mouse => PointerDeviceKind.Mouse,
            PointerType.Touch => PointerDeviceKind.Touch,
            PointerType.Pen => PointerDeviceKind.Stylus,
            _ => PointerDeviceKind.Unknown
        };
    }

    /// <summary>
    /// The host's Avalonia IME client: the <c>NSTextInputClient</c> side of the engine's macOS text
    /// input plugin, answered from <see cref="HostTextInputPlugin"/>'s editing model.
    /// </summary>
    private sealed class PlumixTextInputMethodClient : TextInputMethodClient
    {
        private readonly PlumixHost _host;

        public PlumixTextInputMethodClient(PlumixHost host)
        {
            _host = host;
        }

        public override Visual TextViewVisual => _host;

        public override bool SupportsPreedit => true;

        public override bool SupportsSurroundingText => Model is not null;

        public override string SurroundingText => Model?.GetText() ?? string.Empty;

        /// <summary>The caret rect in this host's coordinates: the plugin reports it in the view's
        /// physical pixels, the space the framework's editable transform maps into.</summary>
        public override Rect CursorRectangle
        {
            get
            {
                if (Model is null || Plugin.GetCaretRect() is not { } rect)
                {
                    return default;
                }

                double devicePixelRatio = _host._view.DevicePixelRatio;
                return devicePixelRatio > 0
                    ? new Rect(
                        rect.X / devicePixelRatio,
                        rect.Y / devicePixelRatio,
                        rect.Width / devicePixelRatio,
                        rect.Height / devicePixelRatio)
                    : rect;
            }
        }

        public override AvaloniaTextSelection Selection
        {
            get
            {
                TextInputModel.Range selection = Model?.Selection ?? default;
                return new AvaloniaTextSelection(selection.Start, selection.End);
            }
            set => Plugin.SetSelection(value.Start, value.End);
        }

        private HostTextInputPlugin Plugin => _host.TextInputPlugin;

        private TextInputModel? Model => _host.HasTextInputClient ? Plugin.ActiveModel : null;

        /// <summary>Avalonia's preedit maps onto the macOS plugin's marked text: non-empty text is
        /// <c>setMarkedText:</c> with the cursor inside it, and clearing it removes the marked text and
        /// ends composing (<c>unmarkText</c>).</summary>
        public override void SetPreeditText(string? preeditText, int? cursorPos)
        {
            if (Model is not { } model)
            {
                return;
            }

            if (!string.IsNullOrEmpty(preeditText))
            {
                Plugin.SetMarkedText(preeditText, Math.Clamp(cursorPos ?? preeditText.Length, 0, preeditText.Length));
            }
            else if (model.Composing)
            {
                Plugin.SetMarkedText(string.Empty, 0);
                Plugin.UnmarkText();
            }
        }

        public override void SetPreeditText(string? preeditText)
        {
            SetPreeditText(preeditText, null);
        }

        public void NotifyEditingStateChanged()
        {
            RaiseSurroundingTextChanged();
            RaiseSelectionChanged();
        }

        public void NotifyCursorRectangleChanged()
        {
            RaiseCursorRectangleChanged();
        }

        public void RequestInputPane()
        {
            RaiseInputPaneActivationRequested();
        }

        public void RefreshOptions()
        {
            RequestReset();
        }
    }
}
