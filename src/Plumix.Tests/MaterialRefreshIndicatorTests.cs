using System.Reflection;
using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialRefreshIndicatorTests : IDisposable
{
    private static readonly Size Viewport = new(320, 400);

    public MaterialRefreshIndicatorTests()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void RefreshProgressIndicator_UsesFlutterDefaultsAndDeterminateArrowhead()
    {
        var widget = new RefreshProgressIndicator(value: 0.75);
        Assert.Equal(RefreshProgressIndicator.DefaultStrokeWidth, widget.StrokeWidth);
        Assert.Equal(2.0, widget.Elevation);
        Assert.Equal(new Thickness(4), widget.IndicatorMargin);
        Assert.Equal(new Thickness(12), widget.IndicatorPadding);

        using var harness = new WidgetRenderHarness(Wrap(widget));
        harness.Pump(new Size(120, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(render);
        Assert.Equal(17.0, ReadProperty<Size>(render!, "Size").Width, 3);
        Assert.Equal(2.5, ReadProperty<double>(render, "StrokeWidth"), 3);
        Assert.True(ReadProperty<double>(render, "ArrowheadScale") > 0.99);
        Assert.Null(render.GetType().GetProperty("Value")!.GetValue(render));
    }

    [Fact]
    public void RefreshProgressIndicator_ResolvesThemeAndWidgetPrecedence()
    {
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.DarkOrange,
            CanvasColor = Colors.LightBlue,
            ProgressIndicatorTheme = new ProgressIndicatorThemeData(
                Color: Colors.MediumPurple,
                RefreshBackgroundColor: Colors.SeaGreen,
                StrokeAlign: -1,
                StrokeCap: StrokeCap.Round)
        };

        using var themed = new WidgetRenderHarness(Wrap(new RefreshProgressIndicator(value: 0.4), theme));
        themed.Pump(new Size(120, 120));
        object? themedRender = FindDescendantByTypeName(themed.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(themedRender);
        Assert.Equal(Colors.MediumPurple, ReadProperty<Color>(themedRender!, "ValueColor"));
        Assert.Equal(-1.0, ReadProperty<double>(themedRender, "StrokeAlign"), 3);
        Assert.Equal(StrokeCap.Round, ReadProperty<StrokeCap?>(themedRender, "StrokeCap"));
        Assert.Equal(Colors.SeaGreen, FindCircleDecorationColor(themed.RenderView));

        using var explicitHarness = new WidgetRenderHarness(Wrap(
            new RefreshProgressIndicator(
                value: 0.4,
                color: Colors.Crimson,
                backgroundColor: Colors.Gold,
                strokeAlign: 0.5,
                strokeCap: StrokeCap.Square),
            theme));
        explicitHarness.Pump(new Size(120, 120));
        object? explicitRender = FindDescendantByTypeName(explicitHarness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(explicitRender);
        Assert.Equal(Colors.Crimson, ReadProperty<Color>(explicitRender!, "ValueColor"));
        Assert.Equal(0.5, ReadProperty<double>(explicitRender, "StrokeAlign"), 3);
        Assert.Equal(StrokeCap.Square, ReadProperty<StrokeCap?>(explicitRender, "StrokeCap"));
        Assert.Equal(Colors.Gold, FindCircleDecorationColor(explicitHarness.RenderView));
    }

    [Fact]
    public void RefreshProgressIndicator_ValidatesNumericContractsAndBuildsPercentSemantics()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RefreshProgressIndicator(value: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RefreshProgressIndicator(strokeWidth: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RefreshProgressIndicator(strokeAlign: double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RefreshProgressIndicator(elevation: -1));

        using var harness = new WidgetRenderHarness(Wrap(
            new RefreshProgressIndicator(value: 0.37, semanticsLabel: "Refreshing")));
        var root = harness.PumpAndGetSemantics(new Size(120, 120));
        var semantics = FindSemantics(root, node => node.Label?.Contains("Refreshing") == true);
        Assert.NotNull(semantics);
        Assert.Contains("37%", semantics!.Label);
    }

    [Fact]
    public async Task RefreshIndicator_FollowsDragArmedSnapRefreshDoneLifecycle()
    {
        var refreshGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var statuses = new List<RefreshIndicatorStatus?>();
        var emitter = new NotificationEmitter();
        var widget = RefreshIndicator.NoSpinner(
            onRefresh: () => refreshGate.Task,
            onStatusChange: statuses.Add,
            child: emitter);

        using var harness = new WidgetRenderHarness(Wrap(widget));
        harness.Pump(Viewport);
        var state = harness.FindState<RefreshIndicatorState>();
        var emitterState = harness.FindState<NotificationEmitterState>();
        var metrics = new ScrollMetricsSnapshot(0, 0, 600, 400, AxisDirection.Down);

        emitterState.Dispatch(new ScrollStartNotification(metrics, hasDragDetails: true));
        Assert.Equal(RefreshIndicatorStatus.Drag, state.Status);

        emitterState.Dispatch(new OverscrollNotification(metrics, overscroll: -120, hasDragDetails: true));
        Assert.Equal(RefreshIndicatorStatus.Armed, state.Status);
        Assert.Equal(1.0, state.PositionValue, 3);

        emitterState.Dispatch(new ScrollEndNotification(metrics));
        Assert.Equal(RefreshIndicatorStatus.Snap, state.Status);

        PumpAnimation(harness, TimeSpan.FromMilliseconds(160));
        Assert.Equal(RefreshIndicatorStatus.Refresh, state.Status);

        refreshGate.SetResult();
        await Task.Yield();
        await Task.Yield();
        Assert.Equal(RefreshIndicatorStatus.Done, state.Status);

        PumpAnimation(harness, TimeSpan.FromMilliseconds(210));
        Assert.Null(state.Status);
        Assert.Equal(
            [
                RefreshIndicatorStatus.Drag,
                RefreshIndicatorStatus.Armed,
                RefreshIndicatorStatus.Snap,
                RefreshIndicatorStatus.Refresh,
                RefreshIndicatorStatus.Done,
            ],
            statuses);
    }

    [Fact]
    public void RefreshIndicator_CancelsShortDragAndRejectsHorizontalNotifications()
    {
        var emitter = new NotificationEmitter();
        using var harness = new WidgetRenderHarness(Wrap(new RefreshIndicator(
            onRefresh: () => Task.CompletedTask,
            child: emitter)));
        harness.Pump(Viewport);
        var state = harness.FindState<RefreshIndicatorState>();
        var emitterState = harness.FindState<NotificationEmitterState>();

        var horizontal = new ScrollMetricsSnapshot(0, 0, 600, 400, AxisDirection.Right);
        emitterState.Dispatch(new ScrollStartNotification(horizontal, hasDragDetails: true));
        Assert.Null(state.Status);

        var vertical = horizontal with { AxisDirection = AxisDirection.Down };
        emitterState.Dispatch(new ScrollStartNotification(vertical, hasDragDetails: true));
        emitterState.Dispatch(new OverscrollNotification(vertical, overscroll: -20, hasDragDetails: true));
        emitterState.Dispatch(new ScrollEndNotification(vertical));
        Assert.Equal(RefreshIndicatorStatus.Canceled, state.Status);

        PumpAnimation(harness, TimeSpan.FromMilliseconds(210));
        Assert.Null(state.Status);
    }

    [Fact]
    public void RefreshIndicator_AdaptiveIOS_UsesCupertinoSpinnerWhileAndroidUsesMaterialSpinner()
    {
        var iosEmitter = new NotificationEmitter();
        using var ios = new WidgetRenderHarness(Wrap(
            RefreshIndicator.Adaptive(() => Task.CompletedTask, iosEmitter),
            ThemeData.Light with { Platform = TargetPlatform.IOS }));
        ios.Pump(Viewport);
        BeginDrag(ios.FindState<NotificationEmitterState>());
        ios.Pump(Viewport);
        Assert.NotNull(FindDescendantByTypeName(ios.RenderView, "RenderCupertinoActivityIndicator"));
        Assert.Null(FindDescendantByTypeName(ios.RenderView, "RenderCircularProgressIndicator"));

        var androidEmitter = new NotificationEmitter();
        using var android = new WidgetRenderHarness(Wrap(
            RefreshIndicator.Adaptive(() => Task.CompletedTask, androidEmitter),
            ThemeData.Light with { Platform = TargetPlatform.Android }));
        android.Pump(Viewport);
        BeginDrag(android.FindState<NotificationEmitterState>());
        android.Pump(Viewport);
        Assert.NotNull(FindDescendantByTypeName(android.RenderView, "RenderCircularProgressIndicator"));
        Assert.Null(FindDescendantByTypeName(android.RenderView, "RenderCupertinoActivityIndicator"));
    }

    [Fact]
    public async Task RefreshIndicator_Show_IsIdempotentWhileRefreshIsRunning()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = new WidgetRenderHarness(Wrap(new RefreshIndicator(
            onRefresh: () => gate.Task,
            child: new SizedBox(width: 100, height: 100))));
        harness.Pump(Viewport);
        var state = harness.FindState<RefreshIndicatorState>();

        var first = state.Show();
        var second = state.Show(atTop: false);
        Assert.Same(first, second);
        PumpAnimation(harness, TimeSpan.FromMilliseconds(160));
        Assert.Equal(RefreshIndicatorStatus.Refresh, state.Status);

        gate.SetResult();
        await first;
    }

    [Fact]
    public void RefreshIndicator_ReceivesRealScrollableOverscrollFromPointerDrag()
    {
        using var harness = new WidgetRenderHarness(Wrap(RefreshIndicator.NoSpinner(
            onRefresh: () => Task.CompletedTask,
            child: new ScrollConfiguration(
                behavior: new ScrollBehavior().CopyWith(
                    dragDevices: new HashSet<PointerDeviceKind>
                    {
                        PointerDeviceKind.Touch,
                        PointerDeviceKind.Mouse,
                    }),
                child: ListView.Builder(
                    itemCount: 30,
                    itemExtent: 54,
                    addAutomaticKeepAlives: false,
                    itemBuilder: (_, index) => new SizedBox(height: 54, child: new Text($"row {index}")))))));
        harness.Pump(Viewport);
        var state = harness.FindState<RefreshIndicatorState>();
        var binding = GestureBinding.Instance;
        var now = DateTime.UtcNow;

        binding.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            991, PointerDeviceKind.Mouse, new Point(160, 100), PointerButtons.Primary, now));
        binding.HandlePointerEvent(harness.RenderView, new PointerMoveEvent(
            991, PointerDeviceKind.Mouse, new Point(160, 140), PointerButtons.Primary, true, now.AddMilliseconds(16)));
        binding.HandlePointerEvent(harness.RenderView, new PointerMoveEvent(
            991, PointerDeviceKind.Mouse, new Point(160, 260), PointerButtons.Primary, true, now.AddMilliseconds(32)));
        harness.Pump(Viewport);

        Assert.Equal(RefreshIndicatorStatus.Armed, state.Status);
        Assert.True(state.PositionValue >= 1.0 / 1.5);

        binding.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            991, PointerDeviceKind.Mouse, new Point(160, 260), PointerButtons.None, now.AddMilliseconds(48)));
    }

    private static void BeginDrag(NotificationEmitterState emitter)
    {
        var metrics = new ScrollMetricsSnapshot(0, 0, 600, 400, AxisDirection.Down);
        emitter.Dispatch(new ScrollStartNotification(metrics, hasDragDetails: true));
        emitter.Dispatch(new OverscrollNotification(metrics, overscroll: -80, hasDragDetails: true));
    }

    private static void PumpAnimation(WidgetRenderHarness harness, TimeSpan elapsed)
    {
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds) + elapsed);
        harness.Pump(Viewport);
    }

    private static Widget Wrap(Widget child, ThemeData? theme = null) =>
        new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(Size: Viewport),
                new Theme(theme ?? ThemeData.Light, child)));

    private static Color? FindCircleDecorationColor(RenderObject root)
    {
        foreach (var render in FindDescendants(root))
        {
            var property = render.GetType().GetProperty("Decoration", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property?.GetValue(render) is BoxDecoration { Shape: BoxShape.Circle } decoration)
            {
                return decoration.Color;
            }
        }
        return null;
    }

    private static object? FindDescendantByTypeName(RenderObject? root, string typeName)
    {
        if (root is null) return null;
        if (root.GetType().Name == typeName) return root;
        object? result = null;
        root.VisitChildren(child => result ??= FindDescendantByTypeName(child, typeName));
        return result;
    }

    private static IEnumerable<RenderObject> FindDescendants(RenderObject root)
    {
        yield return root;
        var children = new List<RenderObject>();
        root.VisitChildren(children.Add);
        foreach (var child in children)
        foreach (var descendant in FindDescendants(child))
        {
            yield return descendant;
        }
    }

    private static T ReadProperty<T>(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return Assert.IsAssignableFrom<T>(property!.GetValue(target));
    }

    private static SemanticsNode? FindSemantics(SemanticsNode? root, Func<SemanticsNode, bool> predicate)
    {
        if (root is null || predicate(root)) return root;
        foreach (var child in root.Children)
        {
            var result = FindSemantics(child, predicate);
            if (result is not null) return result;
        }
        return null;
    }

    private sealed class NotificationEmitter : StatefulWidget
    {
        public override State CreateState() => new NotificationEmitterState();
    }

    private sealed class NotificationEmitterState : State
    {
        public void Dispatch(Notification notification) => notification.Dispatch(Context);
        public override Widget Build(BuildContext context) => new SizedBox(width: 100, height: 800);
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner.RootNode;
        }

        public T FindState<T>() where T : State
        {
            var states = new List<T>();
            CollectStates(_rootElement, states);
            return Assert.Single(states);
        }

        public void Dispose() => _rootElement.Unmount();

        private static void CollectStates<T>(Element element, List<T> states) where T : State
        {
            if (element is StatefulElement stateful && stateful.State is T state) states.Add(state);
            element.VisitChildren(child => CollectStates(child, states));
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;
            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) => _renderView = renderView;
            public override RenderObject? RenderObject => _child?.RenderObject;
            internal override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }
}
