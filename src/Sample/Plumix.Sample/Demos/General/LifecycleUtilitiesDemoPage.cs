using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/lifecycle_utilities_demo_page.dart

public sealed class LifecycleUtilitiesDemoPage : StatefulWidget
{
    public override State CreateState() => new LifecycleUtilitiesDemoPageState();
}

internal sealed class LifecycleUtilitiesDemoPageState : State, WidgetsBindingObserver
{
    private AnimationController _controller = null!;
    private AppLifecycleListener _lifecycleListener = null!;
    private DisposableBuildContext<LifecycleUtilitiesDemoPageState> _disposableContext = null!;
    private AppLifecycleState? _lastLifecycleState;
    private int _statusBuildCount;
    private int _metricsChanges;
    private int _brightnessChanges;
    private int _memoryPressureEvents;

    public override void InitState()
    {
        base.InitState();
        _controller = new AnimationController(duration: TimeSpan.FromMilliseconds(600), vsync: this);
        _disposableContext = new DisposableBuildContext<LifecycleUtilitiesDemoPageState>(this);
        _lifecycleListener = new AppLifecycleListener(onStateChange: HandleLifecycleStateChanged);
        WidgetsBinding.Instance.AddObserver(this);
        WidgetsBinding.Instance.WaitUntilFirstFrameRasterized.ContinueWith(
            _ => EventLoop.ScheduleMicrotask(() =>
            {
                if (Mounted)
                {
                    SetState(static () => { });
                }
            }),
            TaskScheduler.Default);
    }

    public void DidChangeMetrics() => SetState(() => _metricsChanges++);

    public void DidChangePlatformBrightness() => SetState(() => _brightnessChanges++);

    public void DidHaveMemoryPressure() => SetState(() => _memoryPressureEvents++);

    public override void Dispose()
    {
        WidgetsBinding.Instance.RemoveObserver(this);
        _lifecycleListener.Dispose();
        _disposableContext.Dispose();
        _controller.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new SingleChildScrollView(
            child: new Padding(
                insets: new Thickness(16),
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 12,
                    children:
                    [
                        new Text(
                            "Lifecycle listener controls",
                            style: new TextStyle(
                                FontSize: 20,
                                Color: Colors.Black)),
                        new Text(
                            "Change window focus or minimize/restore the app to exercise AppLifecycleListener. "
                            + "The animation probe rebuilds only when AnimationStatus changes.",
                            style: new TextStyle(Color: Colors.DimGray)),
                        new Container(
                            color: new Color(0xFFF4F7FA),
                            padding: new Thickness(12),
                            child: new Text(
                                $"Last app state: {_lastLifecycleState?.ToString() ?? "waiting"}\n"
                                + $"Disposable context available: {_disposableContext.Context is not null}")),
                        new DemoStatusTransition(
                            animation: _controller,
                            builder: BuildStatusReadout),
                        new Wrap(
                            spacing: 8,
                            runSpacing: 8,
                            children:
                            [
                                new TextButton(
                                    onPressed: () => _controller.Forward(from: 0),
                                    child: new Text("Forward")),
                                new TextButton(
                                    onPressed: () => _controller.Reverse(from: 1),
                                    child: new Text("Reverse")),
                            ]),
                        new Text("Binding events", style: new TextStyle(FontSize: 16, Color: Colors.Black)),
                        new Text(
                            "Resize the window or switch the system theme: WidgetsBindingObserver hears "
                            + "didChangeMetrics and didChangePlatformBrightness from the platform.",
                            style: new TextStyle(Color: Colors.DimGray)),
                        new Container(
                            color: new Color(0xFFF4F7FA),
                            padding: new Thickness(12),
                            child: new Text(
                                $"Metrics changes: {_metricsChanges}\n"
                                + $"Platform brightness changes: {_brightnessChanges} "
                                + $"(now {MediaQuery.PlatformBrightnessOf(context)})\n"
                                + $"Memory pressure events: {_memoryPressureEvents}\n"
                                + $"First frame rasterized: {WidgetsBinding.Instance.FirstFrameRasterized}")),
                        new Wrap(
                            spacing: 8,
                            runSpacing: 8,
                            children:
                            [
                                // Dart calls PaintingBinding.instance.handleMemoryPressure(), which the
                                // composed binding dispatches to the widgets binding's override.
                                new TextButton(
                                    onPressed: () => WidgetsBinding.Instance.HandleMemoryPressure(),
                                    child: new Text("Simulate memory pressure")),
                            ]),
                    ])));
    }

    private Widget BuildStatusReadout(BuildContext context)
    {
        _statusBuildCount++;
        return new Container(
            color: new Color(0xFFE7F0FA),
            padding: new Thickness(12),
            child: new Text(
                $"Animation status: {_controller.Status}\nStatusTransitionWidget builds: {_statusBuildCount}"));
    }

    private void HandleLifecycleStateChanged(AppLifecycleState state)
    {
        if (_disposableContext.Context is null)
        {
            return;
        }

        SetState(() => _lastLifecycleState = state);
    }

    private sealed class DemoStatusTransition(
        Animation<double> animation,
        Func<BuildContext, Widget> builder) : StatusTransitionWidget(animation)
    {
        public override Widget Build(BuildContext context)
        {
            return builder(context);
        }
    }
}
