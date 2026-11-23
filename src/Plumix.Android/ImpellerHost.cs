using Android.Views;
using NImpeller;
using Plumix.Accessibility;
using Plumix.Gestures;
using Plumix.Impeller;
using Plumix.Rendering;
using Plumix.Android.Interop;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Android;

// C#-only host infrastructure; no Dart parity source. The engine embedder of the stage 4 prototype: owns the
// implicit FlutterView, reports metrics, lifecycle and touches to the framework, and rasterizes each
// composited layer tree through Impeller into the window, the way the engine's shell does for Android.
// The UI thread records each frame's display list; GlesSurface draws it on a raster thread, as the engine does.

/// <summary>Hosts one Plumix app in an Android SurfaceView, rendered by Impeller.</summary>
internal sealed class ImpellerHost : IDisposable
{
    private readonly FlutterView _view = new(new Size(0, 0), 1.0, 1);
    private readonly ReusableRenderView _root;
    private readonly PipelineOwner _pipeline;
    // Instantiating the binding installs its packet handler on the platform dispatcher.
    private readonly GestureBinding _gestures = GestureBinding.Instance;
    // The process's raster thread and Impeller context, started by InstallBackend so the context is created
    // while the app starts. Like the engine's shell, they outlive an Activity: images in the framework's cache
    // keep textures of this context, and the framework's fonts are registered with the backend once.
    private static GlesSurface? s_surface;
    private readonly GlesSurface _surface;
    private readonly Action _contextCreated;
    private ContainerFlowLayer? _tree;
    private FrameTimingsRecorder? _pendingTimings;
    private bool _drawPosted;
    private IntPtr _window;
    private int _width;
    private int _height;
    private double _density = 1.0;
    private Widget? _pendingApp;
    private bool _builtFirstFrame;
    private int _presentedFirstFrame;
    private ViewPadding _viewPadding;
    private ViewPadding _viewInsets;
    private Brightness _platformBrightness = Brightness.Light;
    private bool _alwaysUse24HourFormat;
    private double? _physicalTouchSlop;

    public ImpellerHost()
    {
        _surface = s_surface ?? throw new InvalidOperationException("Call InstallBackend first.");
        // The first frame may be ready before the context; it is drawn once the context exists.
        _contextCreated = () => PlatformThread.Current.Post(Draw, PlatformTaskPriority.Task);
        _surface.ContextCreated += _contextCreated;
        // The engine's per-view accessibility bridge: the platform-agnostic tree the Android node provider reads.
        Accessibility = new AccessibilityBridge(_view.ViewId, () => _view.DevicePixelRatio);
        TouchProcessor = new AndroidTouchProcessor(_view.ViewId);
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

    /// <summary>The semantics tree of this host's view, for <see cref="PlumixAccessibilityNodeProvider"/>.</summary>
    public AccessibilityBridge Accessibility { get; }

    /// <summary>The view's touch, mouse and stylus input, as the engine's <c>AndroidTouchProcessor</c>.</summary>
    public AndroidTouchProcessor TouchProcessor { get; }

    /// <summary>Installs the platform seams this host provides; call once, before any binding is touched.</summary>
    public static void InstallBackend(global::Android.Content.Res.AssetManager assets, string applicationAssembly)
    {
        // Once per process: an Activity created again (after back finished the last one) reuses it all.
        if (s_surface is not null)
        {
            return;
        }

        s_surface = new GlesSurface();
        ImpellerBackend.OnContextThread = s_surface.Invoke;
        ImpellerBackend.ReadPixels = s_surface.ReadPixels;
        PlatformThread.Current = new AndroidPlatformThread();
        ImageBackend.Current = new AndroidImageBackend();
        ImageBackend.Assets = new AndroidAssetSource(assets);
        // .NET for Android has no entry assembly: relative asset keys belong to the app.
        PlatformAssetBundle.ApplicationAssemblyName = applicationAssembly;
        ImpellerBackend.Install();
        PerfProbe.Mark("render-backend");
        ImpellerTextShaper shaper =
            ImpellerBackend.InstallText("/system/fonts/Roboto-Regular.ttf", "/system/fonts/DroidSans.ttf");
        PerfProbe.Mark("default-font");
        AndroidFontConfig.AddSystemFonts(shaper);
        PerfProbe.Mark("fonts-xml");
    }

    /// <summary>
    /// Runs <paramref name="app"/> as the implicit view's root widget once the surface has a size: the
    /// engine knows the window metrics before `runApp`, so the first frame never lays out at 0x0.
    /// </summary>
    public void Run(Widget app, double density)
    {
        _density = density;
        _pendingApp = app;
        StartIfReady();
    }

    private void StartIfReady()
    {
        if (_pendingApp is not { } app || _width <= 0 || _height <= 0)
        {
            return;
        }

        _pendingApp = null;
        UpdateMetrics();
        // The platform side of `flutter/textinput`; PlumixSurfaceView is its Android editor.
        HostTextInputPlugin.Instance.Attach();
        WidgetsBinding.Instance.SetImplicitView(_view, _pipeline, _root, MediaQueryData());
        PlatformDispatcher.Instance.AddView(_view);
        WidgetsBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
        PerfProbe.Mark("run-app");
        PlumixExtensions.RunApp(app);
        PerfProbe.Mark("run-app-returned");
    }

    public void SurfaceCreated(Surface surface)
    {
        _window = NativeWindow.ANativeWindow_fromSurface(global::Android.Runtime.JNIEnv.Handle, surface.Handle);
        PerfProbe.Mark("surface-created");
        _surface.Attach(_window);
        SurfaceChanged(NativeWindow.ANativeWindow_getWidth(_window), NativeWindow.ANativeWindow_getHeight(_window));
    }

    public void SurfaceChanged(int width, int height)
    {
        _width = width;
        _height = height;
        UpdateMetrics();
        StartIfReady();
        Draw();
    }

    public void SurfaceDestroyed()
    {
        _surface.Detach();
        if (_window != IntPtr.Zero)
        {
            NativeWindow.ANativeWindow_release(_window);
            _window = IntPtr.Zero;
        }
    }

    /// <summary>The system bars and cutout, in physical pixels.</summary>
    public void InsetsChanged(int left, int top, int right, int bottom, int imeBottom)
    {
        _viewPadding = new ViewPadding(left, top, right, bottom);
        // The keyboard covers the bottom: the engine reports it as view insets.
        _viewInsets = new ViewPadding(0, 0, 0, imeBottom);
        UpdateMetrics();
    }

    /// <summary>
    /// The user settings the engine sends with <c>FlutterView.sendUserSettingsToFlutter</c> and its viewport
    /// metrics: platform brightness, the 24-hour format and the touch slop (physical pixels).
    /// </summary>
    public void SettingsChanged(Brightness platformBrightness, bool alwaysUse24HourFormat, double? physicalTouchSlop)
    {
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        Brightness previous = dispatcher.PlatformBrightness;
        _platformBrightness = platformBrightness;
        _alwaysUse24HourFormat = alwaysUse24HourFormat;
        _physicalTouchSlop = physicalTouchSlop;
        UpdateMetrics();
        if (WidgetsBinding.IsImplicitView(_view) && dispatcher.PlatformBrightness != previous)
        {
            dispatcher.NotifyPlatformBrightnessChanged();
        }
    }

    public void LifecycleChanged(AppLifecycleState state) =>
        WidgetsBinding.Instance.HandleAppLifecycleStateChanged(state);

    /// <summary>Lets the framework handle a back press; false means the app should finish.</summary>
    public async Task<bool> HandleBackAsync()
    {
        using var scope = EventLoop.EnterFrameworkThread();
        return await WidgetsBinding.Instance.HandlePopRoute();
    }

    private MediaQueryData MediaQueryData()
    {
        var size = new Size(_width / _density, _height / _density);
        var viewPadding = new EdgeInsets(_viewPadding.Left / _density, _viewPadding.Top / _density,
            _viewPadding.Right / _density, _viewPadding.Bottom / _density);
        var viewInsets = new EdgeInsets(0, 0, 0, _viewInsets.Bottom / _density);
        return new MediaQueryData(
            Size: size,
            DevicePixelRatio: _density,
            Padding: Plumix.Widgets.MediaQueryData.ComputePadding(viewPadding, viewInsets),
            ViewInsets: viewInsets,
            ViewPadding: viewPadding,
            PlatformBrightness: _platformBrightness,
            AlwaysUse24HourFormat: _alwaysUse24HourFormat,
            ViewId: _view.ViewId,
            // DeviceGestureSettings.fromView: the view's physical touch slop in logical pixels.
            GestureSettings: _physicalTouchSlop is { } touchSlop
                ? new DeviceGestureSettings(TouchSlop: touchSlop / _density)
                : null);
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
            padding: Padding(),
            viewInsets: _viewInsets,
            viewPadding: _viewPadding);
        PlatformDispatcher.Instance.UpdateImplicitViewPlatformData(_view, MediaQueryData());
        PlatformDispatcher.Instance.OnMetricsChanged?.Invoke();
    }

    // The engine's padding: the view padding minus what the keyboard already covers.
    private ViewPadding Padding() => new(_viewPadding.Left, _viewPadding.Top, _viewPadding.Right,
        Math.Max(0, _viewPadding.Bottom - _viewInsets.Bottom));

    private void HandleRenderRequested(Scene scene, Size? physicalSize)
    {
        if (scene.TakeLayerTree() is not { } tree)
        {
            return;
        }

        if (_tree is null)
        {
            PerfProbe.Mark("first-scene");
        }

        _tree = tree;
        // The engine's Animator::Render ends the frame's build phase; Draw reports the raster phase.
        _pendingTimings = FrameTimingsRecorder.ForRender();
        // The engine hands the layer tree to its raster thread after the frame; here the next main-thread
        // message records the display list (ahead of other queued work, once the frame has finished) and
        // GlesSurface's raster thread draws it.
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
        if (_tree is null || !_surface.IsAttached || !_surface.IsContextReady || _width <= 0 || _height <= 0)
        {
            return;
        }

        FrameTimingsRecorder? timings = _pendingTimings;
        _pendingTimings = null;
        timings?.RecordRasterStart();
        if (!_builtFirstFrame)
        {
            PerfProbe.Mark("first-raster-start");
        }

        // Recording the display list is CPU work on the UI thread; the raster thread draws and swaps it.
        ImpellerDisplayList displayList = ImpellerLayerPainter.Build(_tree, _width, _height);
        if (!_builtFirstFrame)
        {
            _builtFirstFrame = true;
            PerfProbe.Mark("first-display-list-built");
        }

        _surface.Present(displayList, _width, _height, () => Presented(timings));
    }

    // On the raster thread, after the swap.
    private void Presented(FrameTimingsRecorder? timings)
    {
        if (timings is not null)
        {
            // Shell::OnFrameRasterized: timings for `onReportTimings`, and the first-frame-rasterized signal.
            FrameTiming timing = timings.RecordRasterEnd();
            PlatformThread.Current.Post(() => FrameTimingsReporter.OnFrameRasterized(timing),
                PlatformTaskPriority.Task);
        }

        if (Interlocked.Exchange(ref _presentedFirstFrame, 1) == 0)
        {
            PerfProbe.Mark("first-frame-presented");
            // A marker for scripts that walk the app (the engine's "first frame rasterized" event).
            global::Android.Util.Log.Info("PlumixImpeller", "first frame presented");
        }
    }

    public void Dispose()
    {
        _surface.ContextCreated -= _contextCreated;
        SurfaceDestroyed();
    }
}
