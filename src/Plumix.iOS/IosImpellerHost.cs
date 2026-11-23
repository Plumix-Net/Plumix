using CoreAnimation;
using NImpeller;
using Plumix.Accessibility;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Plumix.Impeller;

namespace Plumix.iOS;

// C#-only host infrastructure; no Dart parity source. The engine embedder of the stage 5 prototype: owns the implicit
// FlutterView, reports metrics, lifecycle and touches to the framework, and rasterizes each composited layer tree
// through Impeller's Metal backend into the view's CAMetalLayer, as the engine's iOS shell does: the UI thread records
// the display list and MetalRasterThread draws and presents it.

/// <summary>Hosts one Plumix app in a CAMetalLayer, rendered by Impeller.</summary>
internal sealed class IosImpellerHost
{
    private static ImpellerContext? s_context;
    private static MetalRasterThread? s_rasterThread;

    private readonly FlutterView _view = new(new Size(0, 0), 1.0, 1);
    private readonly ReusableRenderView _root;
    private readonly PipelineOwner _pipeline;
    // Instantiating the binding installs its packet handler on the platform dispatcher.
    private readonly GestureBinding _gestures = GestureBinding.Instance;
    private CAMetalLayer? _layer;
    private ContainerFlowLayer? _tree;
    private FrameTimingsRecorder? _pendingTimings;
    private bool _drawPosted;
    private int _width;
    private int _height;
    private double _density = 1.0;
    private Widget? _pendingApp;
    private ViewPadding _viewPadding;
    private Brightness _platformBrightness = Brightness.Light;
    private bool _alwaysUse24HourFormat;
    private double _viewInsetBottom;

    public IosImpellerHost()
    {
        Accessibility = new AccessibilityBridge(_view.ViewId, () => _view.DevicePixelRatio);
        TouchProcessor = new IosTouchProcessor(_view.ViewId);
        _root = new ReusableRenderView(_view);
        _pipeline = new PipelineOwner(
            onSemanticsOwnerCreated: () => (_pipeline!.RootNode as RenderView)?.ScheduleInitialSemantics(),
            onSemanticsUpdate: update => (_pipeline!.RootNode as RenderView)?.UpdateSemantics(update),
            onSemanticsOwnerDisposed: () =>
            {
                (_pipeline!.RootNode as RenderView)?.ClearSemantics();
                Accessibility.Clear();
            });
        _view.SemanticsUpdated += Accessibility.AddSemanticsUpdate;
        _view.RenderRequested += HandleRenderRequested;
    }

    /// <summary>The semantics tree of this host's view.</summary>
    public AccessibilityBridge Accessibility { get; }

    /// <summary>The view's touches, as the engine's FlutterViewController dispatches them.</summary>
    public IosTouchProcessor TouchProcessor { get; }

    /// <summary>
    /// Installs the platform seams this host provides; once per process, before any binding is touched.
    /// </summary>
    public static void InstallBackend(string applicationAssembly)
    {
        if (s_context is not null)
        {
            return;
        }

        PlatformThread.Current = new IosPlatformThread();
        ImageBackend.Current = new IosImageBackend();
        ImageBackend.Assets = new IosAssetSource();
        PlatformAssetBundle.ApplicationAssemblyName = applicationAssembly;
        ImpellerBackend.Install();
        s_context = ImpellerContext.CreateMetalNew()
            ?? throw new InvalidOperationException("Impeller could not create a Metal context.");
        ImpellerBackend.Context = s_context;
        ImpellerTextShaper shaper = ImpellerBackend.InstallText(IosFonts.SystemFontPath()
            ?? throw new InvalidOperationException("CoreText reports no system font file."));
        IosFonts.AddSystemFonts(shaper);
    }

    /// <summary>
    /// Runs <paramref name="app"/> as the implicit view's root widget once the layer has a size: the engine knows the
    /// window metrics before `runApp`, so the first frame never lays out at 0x0.
    /// </summary>
    public void Run(Widget app, double density)
    {
        _density = density;
        _pendingApp = app;
        StartIfReady();
    }

    public void Attach(CAMetalLayer layer) => _layer = layer;

    /// <summary>The layer's drawable size, in physical pixels.</summary>
    public void SizeChanged(int width, int height)
    {
        _width = width;
        _height = height;
        UpdateMetrics();
        StartIfReady();
        Draw();
    }

    /// <summary>The safe area, in physical pixels: FlutterViewController.updateViewportPadding.</summary>
    public void SafeAreaChanged(int left, int top, int right, int bottom)
    {
        _viewPadding = new ViewPadding(left, top, right, bottom);
        UpdateMetrics();
    }

    /// <summary>The user settings the engine sends on <c>flutter/settings</c> that live in the view's data.</summary>
    public void SettingsChanged(Brightness brightness, bool alwaysUse24HourFormat)
    {
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        Brightness previous = dispatcher.PlatformBrightness;
        _platformBrightness = brightness;
        _alwaysUse24HourFormat = alwaysUse24HourFormat;
        UpdateMetrics();
        if (WidgetsBinding.IsImplicitView(_view) && dispatcher.PlatformBrightness != previous)
        {
            dispatcher.NotifyPlatformBrightnessChanged();
        }
    }

    /// <summary>The keyboard's physical bottom inset (updateViewportMetricsWithInset:).</summary>
    public double ViewInsetBottom
    {
        get => _viewInsetBottom;
        set
        {
            if (value != _viewInsetBottom)
            {
                _viewInsetBottom = value;
                UpdateMetrics();
            }
        }
    }

    public void LifecycleChanged(AppLifecycleState state) =>
        WidgetsBinding.Instance.HandleAppLifecycleStateChanged(state);

    /// <summary>FlutterEngine.onStatusBarTap: <c>handleScrollToTop</c> on <c>flutter/status_bar</c>.</summary>
    public void StatusBarTapped() =>
        _ = SystemChannels.StatusBar.BinaryMessenger.HandlePlatformMessage(
            SystemChannels.StatusBar.Name,
            SystemChannels.StatusBar.Codec.EncodeMethodCall(new MethodCall("handleScrollToTop")),
            null);

    private void StartIfReady()
    {
        if (_pendingApp is not { } app || _width <= 0 || _height <= 0)
        {
            return;
        }

        _pendingApp = null;
        UpdateMetrics();
        WidgetsBinding.Instance.SetImplicitView(_view, _pipeline, _root, MediaQueryData());
        PlatformDispatcher.Instance.AddView(_view);
        WidgetsBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
        PlumixExtensions.RunApp(app);
    }

    private MediaQueryData MediaQueryData()
    {
        var viewPadding = new EdgeInsets(_viewPadding.Left / _density, _viewPadding.Top / _density,
            _viewPadding.Right / _density, _viewPadding.Bottom / _density);
        var viewInsets = new EdgeInsets(0, 0, 0, _viewInsetBottom / _density);
        return new MediaQueryData(
            Size: new Size(_width / _density, _height / _density),
            DevicePixelRatio: _density,
            Padding: Plumix.Widgets.MediaQueryData.ComputePadding(viewPadding, viewInsets),
            ViewInsets: viewInsets,
            ViewPadding: viewPadding,
            PlatformBrightness: _platformBrightness,
            AlwaysUse24HourFormat: _alwaysUse24HourFormat,
            ViewId: _view.ViewId);
    }

    private void UpdateMetrics()
    {
        if (_width <= 0 || _height <= 0)
        {
            return;
        }

        _view.UpdateMetrics(
            physicalSize: new Size(_width, _height),
            devicePixelRatio: _density,
            padding: new ViewPadding(_viewPadding.Left, _viewPadding.Top, _viewPadding.Right,
                Math.Max(0, _viewPadding.Bottom - _viewInsetBottom)),
            viewInsets: new ViewPadding(0, 0, 0, _viewInsetBottom),
            viewPadding: _viewPadding);
        PlatformDispatcher.Instance.UpdateImplicitViewPlatformData(_view, MediaQueryData());
        PlatformDispatcher.Instance.OnMetricsChanged?.Invoke();
    }

    private void HandleRenderRequested(Scene scene, Size? physicalSize)
    {
        if (scene.TakeLayerTree() is not { } tree)
        {
            return;
        }

        _tree = tree;
        _pendingTimings = FrameTimingsRecorder.ForRender();
        // The engine rasterizes after the frame; the next main-thread message draws, once the frame has finished.
        if (!_drawPosted)
        {
            _drawPosted = true;
            PlatformThread.Current.Post(() =>
            {
                _drawPosted = false;
                Draw();
            }, PlatformTaskPriority.Microtask);
        }
    }

    private void Draw()
    {
        if (_tree is null || _layer is null || s_context is null || _width <= 0 || _height <= 0)
        {
            return;
        }

        FrameTimingsRecorder? timings = _pendingTimings;
        _pendingTimings = null;
        timings?.RecordRasterStart();
        // Recording the display list is CPU work on the UI thread; the raster thread draws and presents it.
        ImpellerDisplayList displayList = ImpellerLayerPainter.Build(_tree, _width, _height);
        s_rasterThread ??= new MetalRasterThread();
        s_rasterThread.Present(_layer, displayList, () => Presented(timings));
    }

    // On the raster thread, after the present: Shell::OnFrameRasterized's timings for `onReportTimings`.
    private static void Presented(FrameTimingsRecorder? timings)
    {
        if (timings is not null)
        {
            FrameTiming timing = timings.RecordRasterEnd();
            PlatformThread.Current.Post(() => FrameTimingsReporter.OnFrameRasterized(timing),
                PlatformTaskPriority.Task);
        }
    }
}
