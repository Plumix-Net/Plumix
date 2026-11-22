using Android.Views;
using NImpeller;
using Plumix.Gestures;
using Plumix.Impeller;
using Plumix.Rendering;
using Plumix.Sample.Impeller.Android.Interop;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Sample.Impeller.Android;

// C#-only host infrastructure; no Dart parity source. The engine embedder of the stage 4 prototype: owns the
// implicit FlutterView, reports metrics, lifecycle and touches to the framework, and rasterizes each
// composited layer tree through Impeller into the window, the way the engine's shell does for Android.
// The framework and the raster work share the main thread here (no separate raster thread yet).

/// <summary>Hosts one Plumix app in an Android SurfaceView, rendered by Impeller.</summary>
internal sealed class ImpellerHost : IDisposable
{
    private readonly FlutterView _view = new(new Size(0, 0), 1.0, 1);
    private readonly ReusableRenderView _root;
    private readonly PipelineOwner _pipeline;
    // Instantiating the binding installs its packet handler on the platform dispatcher.
    private readonly GestureBinding _gestures = GestureBinding.Instance;
    private readonly HostPointerPacketEncoder _pointers = new();
    private readonly GlesSurface _surface = new();
    private ContainerFlowLayer? _tree;
    private FrameTimingsRecorder? _pendingTimings;
    private bool _drawPosted;
    private IntPtr _window;
    private int _width;
    private int _height;
    private double _density = 1.0;
    private Widget? _pendingApp;
    private bool _presentedFirstFrame;
    private ViewPadding _viewPadding;
    private ViewPadding _viewInsets;

    public ImpellerHost()
    {
        _root = new ReusableRenderView(_view);
        _pipeline = new PipelineOwner(
            onSemanticsOwnerCreated: () => (_pipeline!.RootNode as RenderView)?.ScheduleInitialSemantics(),
            onSemanticsUpdate: update => (_pipeline!.RootNode as RenderView)?.UpdateSemantics(update),
            onSemanticsOwnerDisposed: () => (_pipeline!.RootNode as RenderView)?.ClearSemantics());
        _view.RenderRequested += HandleRenderRequested;
    }

    /// <summary>Installs the platform seams this host provides; call once, before any binding is touched.</summary>
    public static void InstallBackend(global::Android.Content.Res.AssetManager assets)
    {
        PlatformThread.Current = new AndroidPlatformThread();
        ImageBackend.Current = new AndroidImageBackend();
        ImageBackend.Assets = new AndroidAssetSource(assets);
        // .NET for Android has no entry assembly: relative asset keys belong to this app.
        PlatformAssetBundle.ApplicationAssemblyName = typeof(ImpellerHost).Assembly.GetName().Name;
        ImpellerBackend.Install();
        ImpellerTextShaper shaper =
            ImpellerBackend.InstallText("/system/fonts/Roboto-Regular.ttf", "/system/fonts/DroidSans.ttf");
        AndroidFontConfig.AddFallbacks(shaper);
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
        PlumixExtensions.RunApp(app);
    }

    public void SurfaceCreated(Surface surface)
    {
        _window = NativeWindow.ANativeWindow_fromSurface(global::Android.Runtime.JNIEnv.Handle, surface.Handle);
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

    public void LifecycleChanged(AppLifecycleState state) =>
        WidgetsBinding.Instance.HandleAppLifecycleStateChanged(state);

    /// <summary>Reports a touch event; returns whether it was consumed.</summary>
    public bool Touch(MotionEvent motion)
    {
        MotionEventActions action = motion.ActionMasked;
        DateTime now = FrameClock.UtcNow;
        switch (action)
        {
            case MotionEventActions.Down:
            case MotionEventActions.PointerDown:
            case MotionEventActions.Up:
            case MotionEventActions.PointerUp:
            {
                int index = motion.ActionIndex;
                int pointer = motion.GetPointerId(index);
                var position = new Offset(motion.GetX(index) / _density, motion.GetY(index) / _density);
                PointerEvent pointerEvent = action is MotionEventActions.Down or MotionEventActions.PointerDown
                    ? new PointerDownEvent(pointer: pointer, kind: PointerDeviceKind.Touch, position: position,
                        timestampUtc: now, viewId: _view.ViewId)
                    : new PointerUpEvent(pointer: pointer, kind: PointerDeviceKind.Touch, position: position,
                        timestampUtc: now, viewId: _view.ViewId);
                _pointers.Report(pointerEvent, _view.ViewId, _density);
                return true;
            }
            case MotionEventActions.Move:
                for (int index = 0; index < motion.PointerCount; index++)
                {
                    var position = new Offset(motion.GetX(index) / _density, motion.GetY(index) / _density);
                    _pointers.Report(new PointerMoveEvent(pointer: motion.GetPointerId(index),
                        kind: PointerDeviceKind.Touch, position: position, timestampUtc: now,
                        viewId: _view.ViewId), _view.ViewId, _density);
                }

                return true;
            case MotionEventActions.Cancel:
                for (int index = 0; index < motion.PointerCount; index++)
                {
                    var position = new Offset(motion.GetX(index) / _density, motion.GetY(index) / _density);
                    _pointers.Report(new PointerCancelEvent(pointer: motion.GetPointerId(index),
                        kind: PointerDeviceKind.Touch, position: position, timestampUtc: now,
                        viewId: _view.ViewId), _view.ViewId, _density);
                }

                return true;
            default:
                return false;
        }
    }

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

        _tree = tree;
        // The engine's Animator::Render ends the frame's build phase; Draw reports the raster phase.
        _pendingTimings = FrameTimingsRecorder.ForRender();
        // The engine rasterizes after the frame, on its raster thread; here the next main-thread message
        // does, ahead of any other queued work, so the frame has finished before its timings are reported.
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
        if (_tree is null || !_surface.IsAttached || _width <= 0 || _height <= 0)
        {
            return;
        }

        FrameTimingsRecorder? timings = _pendingTimings;
        _pendingTimings = null;
        timings?.RecordRasterStart();
        using ImpellerDisplayList displayList = ImpellerLayerPainter.Build(_tree, _width, _height);
        _surface.Present(displayList, _width, _height);
        if (timings is not null)
        {
            // Shell::OnFrameRasterized: timings for `onReportTimings`, and the first-frame-rasterized signal.
            FrameTimingsReporter.OnFrameRasterized(timings.RecordRasterEnd());
        }
        if (!_presentedFirstFrame)
        {
            _presentedFirstFrame = true;
            // A marker for scripts that walk the app (the engine's "first frame rasterized" event).
            global::Android.Util.Log.Info("PlumixImpeller", "first frame presented");
        }
    }

    public void Dispose()
    {
        SurfaceDestroyed();
        _surface.Dispose();
    }
}
