using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// - flutter/packages/flutter/lib/src/widgets/notification_listener.dart
// - flutter/packages/flutter/lib/src/widgets/scroll_notification_observer.dart
// - flutter/packages/flutter/test/widgets/scroll_notification_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ScrollNotificationObserverTests
{
    [Fact]
    public void NotificationListener_FiltersBySubtypeAndStopsBubbling()
    {
        int innerNotifications = 0;
        int outerNotifications = 0;
        using var harness = new WidgetRenderHarness(
            new NotificationListener<Notification>(
                onNotification: notification =>
                {
                    outerNotifications += 1;
                    return false;
                },
                child: new NotificationListener<TestNotification>(
                    onNotification: notification =>
                    {
                        innerNotifications += notification.Value;
                        return true;
                    },
                    child: new NotificationEmitter(new TestNotification(3)))));

        Assert.Equal(3, innerNotifications);
        Assert.Equal(0, outerNotifications);
    }

    [Fact]
    public void Notification_DispatchWithNullContext_IsANoOp()
    {
        var notification = new TestNotification(1);

        bool handled = notification.Dispatch(null);

        Assert.False(handled);
        Assert.Null(notification.Context);
    }

    [Fact]
    public void ScrollNotificationObserver_OfAndMaybeOfResolveNearestState()
    {
        ScrollNotificationObserverState? resolved = null;
        ScrollNotificationObserverState? maybeResolved = null;
        using var harness = new WidgetRenderHarness(
            new ScrollNotificationObserver(
                child: new ContextProbe(context =>
                {
                    resolved = ScrollNotificationObserver.Of(context);
                    maybeResolved = ScrollNotificationObserver.MaybeOf(context);
                })));

        Assert.NotNull(resolved);
        Assert.Same(resolved, maybeResolved);
    }

    [Fact]
    public void ScrollNotificationObserver_OfWithoutAncestorThrows()
    {
        InvalidOperationException? error = null;
        using var harness = new WidgetRenderHarness(
            new ContextProbe(context =>
            {
                error = Assert.Throws<InvalidOperationException>(
                    () => ScrollNotificationObserver.Of(context));
                Assert.Null(ScrollNotificationObserver.MaybeOf(context));
            }));

        Assert.Contains("does not contain", error!.Message);
    }

    [Fact]
    public void ScrollNotificationObserver_ListenersCanRemoveListenersDuringDispatch()
    {
        int firstCalls = 0;
        int secondCalls = 0;
        int outerCalls = 0;
        using var harness = new WidgetRenderHarness(
            new NotificationListener<ScrollNotification>(
                onNotification: notification =>
                {
                    outerCalls += 1;
                    return false;
                },
                child: new ScrollNotificationObserver(
                    child: new ObserverMutationProbe(
                        onFirst: () => firstCalls += 1,
                        onSecond: () => secondCalls += 1))));

        Assert.Equal(1, firstCalls);
        Assert.Equal(0, secondCalls);
        Assert.Equal(1, outerCalls);
    }

    [Fact]
    public void ScrollMetricsNotification_DispatchesInitiallyAndOnlyWhenDimensionsChange()
    {
        var metricsNotifications = new List<ScrollMetricsNotification>();
        var observerNotifications = new List<ScrollNotification>();
        using var harness = new WidgetRenderHarness(
            BuildMetricsTree(
                contentHeight: 120,
                metricsNotifications,
                observerNotifications));

        harness.Pump(new Size(100, 60));

        ScrollMetricsNotification initial = Assert.Single(metricsNotifications);
        Assert.Equal(0, initial.Depth);
        Assert.Equal(0, initial.Metrics.ExtentBefore);
        Assert.Equal(60, initial.Metrics.ExtentInside);
        Assert.Equal(60, initial.Metrics.ExtentAfter);
        Assert.Equal(120, initial.Metrics.ExtentTotal);
        ScrollUpdateNotification initialObserverUpdate =
            Assert.IsType<ScrollUpdateNotification>(Assert.Single(observerNotifications));
        Assert.Equal(initial.Metrics, initialObserverUpdate.Metrics);
        Assert.Equal(initial.Context, initialObserverUpdate.Context);

        harness.Pump(new Size(100, 60));

        Assert.Single(metricsNotifications);
        Assert.Single(observerNotifications);

        harness.Update(BuildMetricsTree(
            contentHeight: 180,
            metricsNotifications,
            observerNotifications));
        harness.Pump(new Size(100, 60));

        Assert.Equal(2, metricsNotifications.Count);
        Assert.Equal(120, metricsNotifications[1].Metrics.ExtentAfter);
        Assert.Equal(180, metricsNotifications[1].Metrics.ExtentTotal);
        Assert.Equal(2, observerNotifications.Count);
    }

    [Fact]
    public void ScrollMetricsNotification_DispatchesInitialZeroDimensions()
    {
        var metricsNotifications = new List<ScrollMetricsNotification>();
        var observerNotifications = new List<ScrollNotification>();
        using var harness = new WidgetRenderHarness(
            BuildMetricsTree(
                contentHeight: 0,
                metricsNotifications,
                observerNotifications));

        harness.Pump(new Size(0, 0));

        ScrollMetricsNotification notification = Assert.Single(metricsNotifications);
        Assert.Equal(0, notification.Metrics.ViewportDimension);
        Assert.Equal(0, notification.Metrics.ExtentTotal);
        Assert.Single(observerNotifications);
    }

    private static Widget BuildMetricsTree(
        double contentHeight,
        List<ScrollMetricsNotification> metricsNotifications,
        List<ScrollNotification> observerNotifications)
    {
        return new NotificationListener<ScrollMetricsNotification>(
            onNotification: notification =>
            {
                metricsNotifications.Add(notification);
                return false;
            },
            child: new ScrollNotificationObserver(
                child: new ObserverListenerProbe(
                    listener: observerNotifications.Add,
                    child: new SingleChildScrollView(
                        child: new SizedBox(width: 100, height: contentHeight)))));
    }

    private sealed class TestNotification(int value) : Notification
    {
        public int Value { get; } = value;
    }

    private sealed class NotificationEmitter(Notification notification) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            notification.Dispatch(context);
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class ContextProbe(Action<BuildContext> callback) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            callback(context);
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class ObserverMutationProbe(
        Action onFirst,
        Action onSecond) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            ScrollNotificationObserverState observer = ScrollNotificationObserver.Of(context);
            ScrollNotificationCallback? second = null;
            ScrollNotificationCallback first = notification =>
            {
                onFirst();
                observer.RemoveListener(second!);
            };
            second = notification => onSecond();
            observer.AddListener(first);
            observer.AddListener(second);
            new ScrollStartNotification(
                new FixedScrollMetrics(
                    minScrollExtent: 0,
                    maxScrollExtent: 100,
                    pixels: 0,
                    viewportDimension: 50,
                    axisDirection: AxisDirection.Down,
                    devicePixelRatio: 1.0)).Dispatch(context);
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class ObserverListenerProbe : StatefulWidget
    {
        public ObserverListenerProbe(
            ScrollNotificationCallback listener,
            Widget child)
        {
            Listener = listener;
            Child = child;
        }

        public ScrollNotificationCallback Listener { get; }

        public Widget Child { get; }

        public override State CreateState()
        {
            return new ObserverListenerProbeState();
        }
    }

    private sealed class ObserverListenerProbeState : State
    {
        private ScrollNotificationObserverState? _observer;

        private ObserverListenerProbe CurrentWidget =>
            (ObserverListenerProbe)StateWidget;

        public override void DidChangeDependencies()
        {
            base.DidChangeDependencies();
            ScrollNotificationObserverState observer =
                ScrollNotificationObserver.Of(Context);
            if (ReferenceEquals(observer, _observer))
            {
                return;
            }

            _observer?.RemoveListener(HandleNotification);
            _observer = observer;
            _observer.AddListener(HandleNotification);
        }

        public override Widget Build(BuildContext context)
        {
            return CurrentWidget.Child;
        }

        public override void Dispose()
        {
            _observer?.RemoveListener(HandleNotification);
            _observer = null;
            base.Dispose();
        }

        private void HandleNotification(ScrollNotification notification)
        {
            CurrentWidget.Listener(notification);
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _root;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget widget)
        {
            var renderView = new RenderView();
            _pipeline = new PipelineOwner(renderView);
            _pipeline.Attach(renderView);
            _root = new HarnessRootElement(renderView, widget);
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public void Update(Widget widget)
        {
            _root.UpdateWidget(widget);
            _owner.FlushBuild();
        }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
            // The position schedules its metrics notification as a microtask, because the listeners
            // that would receive it have already been built by the time layout establishes them.
            Scheduler.FlushMicrotasks();
        }

        public void Dispose()
        {
            _root.Unmount();
            Scheduler.PumpFrameForTests();
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            public override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, Slot);
            }

            public void UpdateWidget(Widget widget)
            {
                Update(widget);
            }

            public override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild();
            }

            public override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public override void ForgetChild(Element child)
            {
                if (ReferenceEquals(child, _child))
                {
                    _child = null;
                }
            }

            public override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = child as RenderBox
                                    ?? throw new InvalidOperationException("Root child must be a RenderBox.");
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }
        }
    }
}
