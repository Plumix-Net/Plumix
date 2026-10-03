using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// C#-only test infrastructure: flutter_test's `TestFlutterView` (window.dart) and the parts of
// `TestWidgetsFlutterBinding` (binding.dart) the ported tests reach through `tester.binding`.

namespace Plumix.Tests;

internal sealed partial class FrameworkDartTester
{
    private Size? _surfaceSize;

    /// <summary>
    /// Dart's <c>TestWidgetsFlutterBinding.createViewConfigurationFor</c> under a surface size: the
    /// render view is laid out for <see cref="_surfaceSize"/> while the view's own metrics (and so
    /// <see cref="MediaQuery"/>) stay as they are.
    /// </summary>
    private void ApplySurfaceSize()
    {
        if (_surfaceSize is not { } size || !RendererBinding.Instance.RenderViews.Any(IsOwnRenderView))
        {
            return;
        }

        BoxConstraints constraints = BoxConstraints.Tight(size);
        double devicePixelRatio = _flutterView.DevicePixelRatio;
        RenderView.Configuration = new ViewConfiguration(
            physicalConstraints: constraints * devicePixelRatio,
            logicalConstraints: constraints,
            devicePixelRatio: devicePixelRatio);
    }

    private bool IsOwnRenderView(RenderView view) => ReferenceEquals(view.FlutterView, _flutterView);

    internal void SetSurfaceSize(Size? size)
    {
        if (Equals(_surfaceSize, size))
        {
            return;
        }

        _surfaceSize = size;
        // Dart's setSurfaceSize calls handleMetricsChanged, which re-creates every view configuration.
        WidgetsBinding.Instance.HandleMetricsChanged();
    }
}

/// <summary>
/// flutter_test's <c>TestFlutterView</c> (window.dart): the tester's <see cref="FlutterView"/> with
/// settable metrics. Setting a metric notifies the binding the way the engine's
/// <c>onMetricsChanged</c> does (<see cref="WidgetsBinding.HandleMetricsChanged"/>), so render views
/// are reconfigured and <c>MediaQuery.fromView</c> rebuilds; each <c>Reset*</c> restores the value the
/// tester started with.
/// </summary>
/// <remarks>
/// All values are physical pixels, as in Dart (<c>tester.view.viewInsets = FakeViewPadding(bottom: 1000)</c>
/// is <c>tester.View.ViewInsets = new Thickness(0, 0, 0, 1000)</c>). The view converts implicitly to
/// <see cref="FlutterView"/>, so it can be passed wherever the framework wants one.
/// </remarks>
internal sealed class TestFlutterView
{
    private readonly FlutterView _view;
    private readonly Size _initialPhysicalSize;
    private readonly double _initialDevicePixelRatio;
    private readonly Thickness _initialPadding;
    private readonly Thickness _initialViewInsets;
    private readonly Thickness _initialViewPadding;
    private readonly Thickness _initialSystemGestureInsets;

    public TestFlutterView(FlutterView view)
    {
        _view = view;
        _initialPhysicalSize = view.PhysicalSize;
        _initialDevicePixelRatio = view.DevicePixelRatio;
        _initialPadding = view.Padding;
        _initialViewInsets = view.ViewInsets;
        _initialViewPadding = view.ViewPadding;
        _initialSystemGestureInsets = view.SystemGestureInsets;
    }

    /// <summary>The framework view this test view drives.</summary>
    public FlutterView FlutterView => _view;

    public int ViewId => _view.ViewId;

    /// <summary>Dart's <c>TestFlutterView.platformDispatcher</c>.</summary>
    public TestPlatformDispatcher PlatformDispatcher => TestPlatformDispatcher.Instance;

    /// <summary>Dart's <c>physicalSize</c>.</summary>
    public Size PhysicalSize
    {
        get => _view.PhysicalSize;
        set => Update(() => _view.UpdateMetrics(physicalSize: value));
    }

    /// <summary>Dart's <c>physicalConstraints</c> (tight around <see cref="PhysicalSize"/>).</summary>
    public BoxConstraints PhysicalConstraints => _view.PhysicalConstraints;

    /// <summary>Dart's <c>devicePixelRatio</c>.</summary>
    public double DevicePixelRatio
    {
        get => _view.DevicePixelRatio;
        set => Update(() => _view.UpdateMetrics(devicePixelRatio: value));
    }

    /// <summary>Dart's <c>padding</c>, in physical pixels.</summary>
    public Thickness Padding
    {
        get => _view.Padding;
        set => Update(() => _view.UpdateMetrics(padding: value));
    }

    /// <summary>Dart's <c>viewInsets</c>, in physical pixels.</summary>
    public Thickness ViewInsets
    {
        get => _view.ViewInsets;
        set => Update(() => _view.UpdateMetrics(viewInsets: value));
    }

    /// <summary>Dart's <c>viewPadding</c>, in physical pixels.</summary>
    public Thickness ViewPadding
    {
        get => _view.ViewPadding;
        set => Update(() => _view.UpdateMetrics(viewPadding: value));
    }

    /// <summary>Dart's <c>systemGestureInsets</c>, in physical pixels.</summary>
    public Thickness SystemGestureInsets
    {
        get => _view.SystemGestureInsets;
        set => Update(() => _view.UpdateMetrics(systemGestureInsets: value));
    }

    /// <summary>Forwards <see cref="FlutterView.RenderRequested"/>.</summary>
    public event Action<Scene, Size?>? RenderRequested
    {
        add => _view.RenderRequested += value;
        remove => _view.RenderRequested -= value;
    }

    public static implicit operator FlutterView(TestFlutterView view) => view._view;

    /// <summary>Dart's <c>resetPhysicalSize</c>.</summary>
    public void ResetPhysicalSize() => PhysicalSize = _initialPhysicalSize;

    /// <summary>Dart's <c>resetDevicePixelRatio</c>.</summary>
    public void ResetDevicePixelRatio() => DevicePixelRatio = _initialDevicePixelRatio;

    /// <summary>Dart's <c>resetPadding</c>.</summary>
    public void ResetPadding() => Padding = _initialPadding;

    /// <summary>Dart's <c>resetViewInsets</c>.</summary>
    public void ResetViewInsets() => ViewInsets = _initialViewInsets;

    /// <summary>Dart's <c>resetViewPadding</c>.</summary>
    public void ResetViewPadding() => ViewPadding = _initialViewPadding;

    /// <summary>Dart's <c>resetSystemGestureInsets</c>.</summary>
    public void ResetSystemGestureInsets() => SystemGestureInsets = _initialSystemGestureInsets;

    /// <summary>Dart's <c>reset</c>: every metric back to the tester's initial value.</summary>
    public void Reset()
    {
        Update(() => _view.UpdateMetrics(
            physicalSize: _initialPhysicalSize,
            devicePixelRatio: _initialDevicePixelRatio,
            padding: _initialPadding,
            viewInsets: _initialViewInsets,
            viewPadding: _initialViewPadding,
            systemGestureInsets: _initialSystemGestureInsets));
    }

    /// <summary>
    /// <see cref="FlutterView.UpdateMetrics"/> as is: the metrics change without notifying the binding,
    /// for tests that drive <c>didChangeMetrics</c> themselves.
    /// </summary>
    public void UpdateMetrics(
        Size? physicalSize = null,
        double? devicePixelRatio = null,
        int? viewId = null,
        BoxConstraints? physicalConstraints = null,
        Thickness? padding = null,
        Thickness? viewInsets = null,
        Thickness? viewPadding = null,
        Thickness? systemGestureInsets = null)
    {
        _view.UpdateMetrics(
            physicalSize: physicalSize,
            devicePixelRatio: devicePixelRatio,
            viewId: viewId,
            physicalConstraints: physicalConstraints,
            padding: padding,
            viewInsets: viewInsets,
            viewPadding: viewPadding,
            systemGestureInsets: systemGestureInsets);
    }

    public override string ToString() => _view.ToString();

    private static void Update(Action update)
    {
        update();
        WidgetsBinding.Instance.HandleMetricsChanged();
    }
}

/// <summary>
/// The slice of flutter_test's <c>TestWidgetsFlutterBinding</c> the ported tests reach through
/// <c>tester.binding</c>: the surface size and the mock handlers of the default binary messenger.
/// </summary>
internal sealed class FrameworkDartTestBinding
{
    private readonly FrameworkDartTester _tester;

    public FrameworkDartTestBinding(FrameworkDartTester tester)
    {
        _tester = tester;
    }

    /// <summary>Dart's <c>binding.platformDispatcher</c>.</summary>
    public TestPlatformDispatcher PlatformDispatcher => TestPlatformDispatcher.Instance;

    /// <summary>Dart's <c>binding.defaultBinaryMessenger</c>.</summary>
    public BinaryMessenger DefaultBinaryMessenger => ServicesBinding.Instance.DefaultBinaryMessenger;

    /// <summary>
    /// Dart's <c>binding.setSurfaceSize(size)</c>: lays the view out for <paramref name="size"/> (logical
    /// pixels), or for the view's own size again when null. The view's metrics are left alone.
    /// </summary>
    public void SetSurfaceSize(Size? size) => _tester.SetSurfaceSize(size);

    /// <summary>
    /// Dart's <c>binding.defaultBinaryMessenger.setMockMethodCallHandler(channel, handler)</c>: null
    /// removes the handler.
    /// </summary>
    public void SetMockMethodCallHandler(MethodChannel channel, Func<MethodCall, Task<object?>>? handler)
    {
        ArgumentNullException.ThrowIfNull(channel);
        channel.SetPlatformMethodCallHandler(handler);
    }

    /// <summary>
    /// Dart's <c>binding.defaultBinaryMessenger.setMockMessageHandler(channel, handler)</c>: null removes
    /// the handler.
    /// </summary>
    public void SetMockMessageHandler(string channel, MessageHandler? handler) =>
        DefaultBinaryMessenger.SetPlatformMessageHandler(channel, handler);

    /// <summary>
    /// Dart's <c>binding.takeAnnouncements</c>; see <see cref="FrameworkDartTester.TakeAnnouncements"/>.
    /// </summary>
    public List<CapturedAccessibilityAnnouncement> TakeAnnouncements() => _tester.TakeAnnouncements();
}
