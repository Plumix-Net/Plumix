using Avalonia;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/binding.dart (runApp, runWidget, WidgetsFlutterBinding,
//   scheduleAttachRootWidget, attachRootWidget, attachToBuildOwner, wrapWithDefaultView)
// Mirrors run_app_async_test.dart, run_app_test.dart, multi_view_binding_test.dart,
// multi_view_no_implicit_view_binding_test.dart and binding_attach_root_widget_test.dart.

[Collection(SchedulerTestCollection.Name)]
public sealed class WidgetsBindingBootstrapTests : IDisposable
{
    private readonly WidgetsBinding _binding = WidgetsBinding.Instance;
    private readonly PlatformDispatcher _dispatcher = PlatformDispatcher.Instance;
    private readonly Action<Action> _previousTimerRun;
    private readonly Queue<Action> _turns = [];

    public WidgetsBindingBootstrapTests()
    {
        _previousTimerRun = _dispatcher.TimerRun;
        Scheduler.ResetForTests();
        _binding.ResetRootForTests();
        _dispatcher.TimerRun = _turns.Enqueue;
        StatefulProbe.Reset();
    }

    public void Dispose()
    {
        _binding.ResetRootForTests();
        Scheduler.ResetForTests();
        _dispatcher.TimerRun = _previousTimerRun;
    }

    [Fact]
    public void EnsureInitialized_ReturnsTheProcessWidgetsFlutterBinding()
    {
        WidgetsBinding first = WidgetsFlutterBinding.EnsureInitialized();
        WidgetsBinding second = WidgetsFlutterBinding.EnsureInitialized();

        Assert.Same(_binding, first);
        Assert.Same(first, second);
        Assert.IsType<WidgetsFlutterBinding>(first);
    }

    [Fact]
    public void ConstructingABinding_DoesNotScheduleOrRegisterAFrame()
    {
        _ = new WidgetsBinding();

        Assert.False(Scheduler.HasScheduledFrame);
        Assert.Null(_dispatcher.OnBeginFrame);
        Assert.Null(_dispatcher.OnDrawFrame);
    }

    [Fact]
    public void RunApp_QueuesRootAttachmentBeforeTheWarmUpFrame()
    {
        var host = new WidgetHost();
        bool didBuild = false;

        host.RunApplication(new BuildProbe(() => didBuild = true));

        Assert.Null(_binding.RootElement);
        Assert.False(didBuild);
        Assert.Equal(3, _turns.Count);

        RunNextTurn();

        RootElement root = Assert.IsType<RootElement>(_binding.RootElement);
        var view = Assert.IsType<View>(root.ChildElement!.Widget);
        Assert.Same(host.RootFlutterView, view.ViewHandle);
        Assert.NotNull(view.DeprecatedPipelineOwner);
        Assert.Same(host.RootFlutterView, view.DeprecatedRenderView!.FlutterView);
        Assert.True(didBuild);

        DrainTurns();
    }

    [Fact]
    public void RunApp_ProvidesTheImplicitViewToDescendants()
    {
        FlutterView? foundView = null;
        var host = new WidgetHost();
        host.RunApplication(new ViewProbe(context => foundView = View.Of(context)));

        RunNextTurn();

        Assert.Same(host.RootFlutterView, foundView);
        DrainTurns();
    }

    [DebugOnlyFact]
    public void RunApp_ReportsAnExplicitViewInsideTheImplicitView()
    {
        var reported = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            var host = new WidgetHost();
            var explicitView = new FlutterView(new Size(320, 180), viewId: 42);
            host.RunApplication(new View(view: explicitView, child: new SizedBox()));

            RunNextTurn();

            Assert.Contains(
                reported,
                details => details.Exception.ToString()!.Contains("runWidget", StringComparison.Ordinal));
            DrainTurns();
        }
        finally
        {
            FlutterError.OnError = previous;
        }
    }

    [Fact]
    public void RunApp_WithoutAnImplicitView_DirectsTheCallerToRunWidget()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => PlumixExtensions.RunApp(new SizedBox()));

        Assert.Contains("RunWidget", error.Message, StringComparison.Ordinal);
        Assert.Empty(_turns);
    }

    [Fact]
    public void RunWidget_DoesNotAddAnImplicitView_AndAcceptsAnEmptyViewCollection()
    {
        var collection = new ViewCollection([]);

        PlumixExtensions.RunWidget(collection);
        Assert.Null(_binding.RootElement);

        RunNextTurn();

        RootElement root = Assert.IsType<RootElement>(_binding.RootElement);
        Assert.Same(collection, root.ChildElement!.Widget);
        Assert.Null(_dispatcher.ImplicitView);
        DrainTurns();
    }

    [Fact]
    public void RunWidget_AcceptsAnExplicitView()
    {
        var explicitView = new FlutterView(new Size(320, 180), viewId: 41);
        FlutterView? foundView = null;

        PlumixExtensions.RunWidget(new View(
            view: explicitView,
            child: new ViewProbe(context => foundView = View.Of(context))));
        RunNextTurn();

        Assert.Same(explicitView, foundView);
        Assert.Contains(RendererBinding.Instance.RenderViews, view => view.FlutterView == explicitView);
        DrainTurns();
    }

    [DebugOnlyFact]
    public void RunWidget_ReportsARenderObjectRootWithoutAView()
    {
        var reported = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            PlumixExtensions.RunWidget(new SizedBox(width: 10, height: 10));
            RunNextTurn();

            Assert.Contains(
                reported,
                details => details.Exception.ToString()!.Contains(
                    "Try wrapping your widget in a View widget",
                    StringComparison.Ordinal));
            DrainTurns();
        }
        finally
        {
            FlutterError.OnError = previous;
        }
    }

    [Fact]
    public void RepeatedRunApp_ReusesTheRootElementAndCompatibleState()
    {
        var host = new WidgetHost();
        host.RunApplication(new StatefulProbe("first"));
        DrainTurns();
        RootElement firstRoot = Assert.IsType<RootElement>(_binding.RootElement);
        StatefulProbeState firstState = Assert.IsType<StatefulProbeState>(StatefulProbe.CurrentState);

        PlumixExtensions.RunApp(new StatefulProbe("second"));
        RunNextTurn();

        Assert.Same(firstRoot, _binding.RootElement);
        Assert.Same(firstState, StatefulProbe.CurrentState);
        Assert.Equal("first", firstState.LastLabel);

        DrainTurns();

        Assert.Same(firstState, StatefulProbe.CurrentState);
        Assert.Equal("second", firstState.LastLabel);
        Assert.Equal(2, firstState.BuildCount);
    }

    [Fact]
    public void AttachRootWidget_EnablesFramesAndSchedulesTheBootstrapFrame()
    {
        Assert.False(_binding.FramesEnabled);
        Assert.False(Scheduler.HasScheduledFrame);

        _binding.AttachRootWidget(new ViewCollection([]));

        Assert.True(_binding.FramesEnabled);
        Assert.True(_binding.IsRootWidgetAttached);
        Assert.True(Scheduler.HasScheduledFrame);
        Assert.NotNull(_dispatcher.OnBeginFrame);
        Assert.NotNull(_dispatcher.OnDrawFrame);
    }

    private void RunNextTurn()
    {
        Assert.NotEmpty(_turns);
        _turns.Dequeue()();
    }

    private void DrainTurns()
    {
        while (_turns.Count > 0)
        {
            RunNextTurn();
        }
    }

    private sealed class BuildProbe(Action onBuild) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            onBuild();
            return new SizedBox();
        }
    }

    private sealed class ViewProbe(Action<BuildContext> onBuild) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            onBuild(context);
            return new SizedBox();
        }
    }

    private sealed class StatefulProbe(string label) : StatefulWidget
    {
        public string Label { get; } = label;

        public static StatefulProbeState? CurrentState { get; private set; }

        public static void Reset() => CurrentState = null;

        public override State CreateState()
        {
            CurrentState = new StatefulProbeState();
            return CurrentState;
        }
    }

    private sealed class StatefulProbeState : State<StatefulProbe>
    {
        public int BuildCount { get; private set; }

        public string? LastLabel { get; private set; }

        public override Widget Build(BuildContext context)
        {
            BuildCount += 1;
            LastLabel = Widget.Label;
            return new SizedBox();
        }
    }
}
