using System;
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

internal sealed class LifecycleUtilitiesDemoPageState : State
{
    private AnimationController _controller = null!;
    private AppLifecycleListener _lifecycleListener = null!;
    private DisposableBuildContext<LifecycleUtilitiesDemoPageState> _disposableContext = null!;
    private AppLifecycleState? _lastLifecycleState;
    private int _statusBuildCount;

    public override void InitState()
    {
        base.InitState();
        _controller = new AnimationController(TimeSpan.FromMilliseconds(600), this);
        _disposableContext = new DisposableBuildContext<LifecycleUtilitiesDemoPageState>(this);
        _lifecycleListener = new AppLifecycleListener(onStateChange: HandleLifecycleStateChanged);
    }

    public override void Dispose()
    {
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
                        new Text("Lifecycle listener controls", fontSize: 20, color: Colors.Black),
                        new Text(
                            "Change window focus or minimize/restore the app to exercise AppLifecycleListener. "
                            + "The animation probe rebuilds only when AnimationStatus changes.",
                            color: Colors.DimGray),
                        new Container(
                            color: Color.Parse("#FFF4F7FA"),
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
                    ])));
    }

    private Widget BuildStatusReadout(BuildContext context)
    {
        _statusBuildCount++;
        return new Container(
            color: Color.Parse("#FFE7F0FA"),
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
