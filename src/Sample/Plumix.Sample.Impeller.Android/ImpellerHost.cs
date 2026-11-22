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
    private IntPtr _window;
    private int _width;
    private int _height;
    private double _density = 1.0;
    private ViewPadding _viewPadding;

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
    public static void InstallBackend()
    {
        PlatformThread.Current = new AndroidPlatformThread();
        ImpellerBackend.Install();
        ImpellerTextShaper shaper =
            ImpellerBackend.InstallText("/system/fonts/Roboto-Regular.ttf", "/system/fonts/DroidSans.ttf");
        AndroidFontConfig.AddFallbacks(shaper);
    }

    /// <summary>Runs <paramref name="app"/> as the implicit view's root widget.</summary>
    public void Run(Widget app, double density)
    {
        _density = density;
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
    public void InsetsChanged(int left, int top, int right, int bottom)
    {
        _viewPadding = new ViewPadding(left, top, right, bottom);
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
        return new MediaQueryData(
            Size: size,
            DevicePixelRatio: _density,
            Padding: viewPadding,
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
            padding: _viewPadding,
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
        Draw();
    }

    private void Draw()
    {
        if (_tree is null || !_surface.IsAttached || _width <= 0 || _height <= 0)
        {
            return;
        }

        using ImpellerDisplayList displayList = ImpellerLayerPainter.Build(_tree, _width, _height);
        _surface.Present(displayList, _width, _height);
    }

    public void Dispose()
    {
        SurfaceDestroyed();
        _surface.Dispose();
    }
}
