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
    private readonly ChannelBuffers _messageBuffers = new();
    private const string DemoChannel = "plumix/lifecycle-buffer-demo";
    private int _messageNumber;
    private string _lastMessage = "none";
    private bool _listening;

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
        _messageBuffers.ClearListener(DemoChannel);
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
                insets: EdgeInsets.All(16),
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
                            style: new TextStyle(Color: MaterialColors.Black54)),
                        new Container(
                            color: new Color(0xFFF4F7FA),
                            padding: EdgeInsets.All(12),
                            child: new Text(
                                $"Last app state: {(_lastLifecycleState is { } state ? state.Name() : "waiting")}\n"
                                + "Disposable context available: "
                                + (_disposableContext.Context is not null ? "true" : "false"))),
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
                            style: new TextStyle(Color: MaterialColors.Black54)),
                        new Container(
                            color: new Color(0xFFF4F7FA),
                            padding: EdgeInsets.All(12),
                            child: new Text(
                                $"Metrics changes: {_metricsChanges}\n"
                                + $"Platform brightness changes: {_brightnessChanges} "
                                + $"(now {MediaQuery.PlatformBrightnessOf(context).Name()})\n"
                                + $"Memory pressure events: {_memoryPressureEvents}\n"
                                + "First frame rasterized: "
                                + (WidgetsBinding.Instance.FirstFrameRasterized ? "true" : "false"))),
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
                        new Text("Messages before listening", style: new TextStyle(FontSize: 16)),
                        new Text(
                            "Queue several messages, then start listening. The default buffer keeps the latest "
                            + "message; new messages arrive immediately while listening."),
                        new Text(
                            $"Sent: {_messageNumber} · Received: {_lastMessage} · "
                            + $"Listening: {(_listening ? "true" : "false")}"),
                        new Wrap(
                            spacing: 8,
                            runSpacing: 8,
                            children:
                            [
                                new TextButton(onPressed: QueueMessage, child: new Text("Queue message")),
                                new TextButton(
                                    onPressed: _listening ? null : StartListening,
                                    child: new Text("Start listening")),
                                new TextButton(
                                    onPressed: _listening ? StopListening : null,
                                    child: new Text("Stop listening")),
                            ]),
                    ])));
    }

    private Widget BuildStatusReadout(BuildContext context)
    {
        _statusBuildCount++;
        return new Container(
            color: new Color(0xFFE7F0FA),
            padding: EdgeInsets.All(12),
            child: new Text(
                $"Animation status: {_controller.Status.Name()}\nStatusTransitionWidget builds: {_statusBuildCount}"));
    }

    private void QueueMessage()
    {
        SetState(() => _messageNumber++);
        _messageBuffers.Push(DemoChannel, new StringCodec().EncodeMessage($"Message {_messageNumber}"), _ => { });
    }

    private void StartListening()
    {
        SetState(() => _listening = true);
        _messageBuffers.SetListener(DemoChannel, (data, reply) =>
        {
            if (Mounted)
            {
                SetState(() => _lastMessage = new StringCodec().DecodeMessage(data) ?? "empty");
            }

            reply(null);
        });
    }

    private void StopListening()
    {
        _messageBuffers.ClearListener(DemoChannel);
        SetState(() => _listening = false);
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
