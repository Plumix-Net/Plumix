using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/dismissible.dart
// flutter/packages/flutter/lib/src/widgets/size_changed_layout_notifier.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class DismissibleSizeChangedLayoutTests : IDisposable
{
    private double _clock = Scheduler.CurrentSeconds;

    public DismissibleSizeChangedLayoutTests()
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
    public void Dismissible_ExposesFlutterDefaultsAndGuards()
    {
        var child = new SizedBox(width: 100, height: 40);
        var key = new ValueKey<string>("row");
        var dismissible = new Dismissible(key, child);

        Assert.Same(key, dismissible.Key);
        Assert.Same(child, dismissible.Child);
        Assert.Null(dismissible.Background);
        Assert.Null(dismissible.SecondaryBackground);
        Assert.Null(dismissible.ConfirmDismiss);
        Assert.Null(dismissible.OnResize);
        Assert.Null(dismissible.OnUpdate);
        Assert.Null(dismissible.OnDismissed);
        Assert.Equal(DismissDirection.Horizontal, dismissible.Direction);
        Assert.Equal(Dismissible.DefaultResizeDuration, dismissible.ResizeDuration);
        Assert.Empty(dismissible.DismissThresholds);
        Assert.Equal(Dismissible.DefaultMovementDuration, dismissible.MovementDuration);
        Assert.Equal(0.0, dismissible.CrossAxisEndOffset);
        Assert.Equal(DragStartBehavior.Start, dismissible.DragStartBehavior);
        Assert.Equal(HitTestBehavior.Opaque, dismissible.Behavior);
        Assert.Null(new Dismissible(
            key,
            child,
            resizeDuration: null).ResizeDuration);

        Assert.Throws<ArgumentNullException>(() => new Dismissible(null!, child));
        Assert.Throws<ArgumentNullException>(() => new Dismissible(key, null!));
        Assert.Throws<ArgumentException>(() => new Dismissible(
            key,
            child,
            secondaryBackground: new SizedBox()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dismissible(
            key,
            child,
            dismissThresholds: new Dictionary<DismissDirection, double>
            {
                [DismissDirection.StartToEnd] = -0.1,
            }));
    }

    [Fact]
    public async Task Dismissible_DragReportsThresholdSlidesAndDismissesAfterResize()
    {
        var updates = new List<DismissUpdateDetails>();
        int resizeCount = 0;
        var dismissedDirections = new List<DismissDirection>();
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new UnconstrainedBox(
                alignment: Alignment.TopLeft,
                child: new Dismissible(
                    key: new ValueKey<string>("row"),
                    child: new SizedBox(width: 100, height: 40),
                    background: new ColoredBox(
                        color: Colors.Green,
                        child: new SizedBox(width: 100, height: 40)),
                    secondaryBackground: new ColoredBox(
                        color: Colors.Red,
                        child: new SizedBox(width: 100, height: 40)),
                    movementDuration: TimeSpan.FromMilliseconds(100),
                    resizeDuration: TimeSpan.FromMilliseconds(1000),
                    onUpdate: updates.Add,
                    onResize: () => resizeCount++,
                    onDismissed: dismissedDirections.Add))));
        var viewport = new Size(240, 120);
        harness.Pump(viewport);

        DateTime start = new(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc);
        DispatchDrag(
            harness.RenderView,
            pointer: 11,
            start,
            [new Point(35, 20), new Point(65, 20), new Point(85, 20)]);
        harness.Pump(viewport);

        var translation = Assert.Single(FindDescendants<RenderFractionalTranslation>(harness.RenderView));
        Assert.Equal(0.75, translation.Translation.X, precision: 6);
        Assert.Equal(0.0, translation.Translation.Y);
        Assert.Contains(updates, details =>
            details.Direction == DismissDirection.StartToEnd
            && details.Reached
            && !details.PreviousReached
            && details.Progress > 0.4);
        Assert.Single(FindDescendants<RenderClipRect>(harness.RenderView));

        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerUpEvent(
                pointer: 11,
                kind: PointerDeviceKind.Mouse,
                position: new Point(85, 20),
                buttons: PointerButtons.None,
                timestampUtc: start.AddMilliseconds(600)));

        PumpAnimation(harness, viewport, 0.2);
        await Task.Yield();
        PumpAnimation(harness, viewport, 0.2);
        await Task.Yield();
        PumpAnimation(harness, viewport, 1.0);
        await Task.Yield();

        Assert.Equal([DismissDirection.StartToEnd], dismissedDirections);
        Assert.True(resizeCount > 0);
    }

    [Fact]
    public async Task Dismissible_ConfirmVetoReturnsToOriginAndDirectionalDragRejectsWrongSide()
    {
        int confirmations = 0;
        int dismissals = 0;
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Rtl,
            new UnconstrainedBox(
                alignment: Alignment.TopLeft,
                child: new Dismissible(
                    key: new ValueKey<string>("rtl-row"),
                    child: new SizedBox(width: 100, height: 40),
                    direction: DismissDirection.StartToEnd,
                    movementDuration: TimeSpan.FromMilliseconds(80),
                    confirmDismiss: direction =>
                    {
                        confirmations++;
                        Assert.Equal(DismissDirection.StartToEnd, direction);
                        return Task.FromResult<bool?>(false);
                    },
                    onDismissed: _ => dismissals++))));
        var viewport = new Size(240, 120);
        harness.Pump(viewport);
        DateTime start = new(2026, 7, 21, 13, 0, 0, DateTimeKind.Utc);

        DispatchDrag(
            harness.RenderView,
            pointer: 21,
            start,
            [new Point(35, 20), new Point(65, 20), new Point(85, 20)]);
        harness.Pump(viewport);
        var translation = Assert.Single(FindDescendants<RenderFractionalTranslation>(harness.RenderView));
        Assert.Equal(0.0, translation.Translation.X);

        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerCancelEvent(
                pointer: 21,
                kind: PointerDeviceKind.Mouse,
                position: new Point(85, 20),
                buttons: PointerButtons.None,
                timestampUtc: start.AddMilliseconds(500)));

        DateTime secondStart = start.AddSeconds(1);
        DispatchDrag(
            harness.RenderView,
            pointer: 22,
            secondStart,
            [new Point(-15, 20), new Point(-45, 20), new Point(-65, 20)]);
        harness.Pump(viewport);
        translation = Assert.Single(FindDescendants<RenderFractionalTranslation>(harness.RenderView));
        Assert.True(translation.Translation.X < -0.4);

        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerUpEvent(
                pointer: 22,
                kind: PointerDeviceKind.Mouse,
                position: new Point(-65, 20),
                buttons: PointerButtons.None,
                timestampUtc: secondStart.AddMilliseconds(600)));
        PumpAnimation(harness, viewport, 0.2);
        await Task.Yield();
        PumpAnimation(harness, viewport, 0.2);

        translation = Assert.Single(FindDescendants<RenderFractionalTranslation>(harness.RenderView));
        Assert.Equal(0.0, translation.Translation.X, precision: 6);
        Assert.Equal(1, confirmations);
        Assert.Equal(0, dismissals);
    }

    [Fact]
    public void SizeChangedLayoutNotifier_SkipsInitialLayoutAndDispatchesOnlyForSizeChanges()
    {
        int notifications = 0;
        var key = new LabeledGlobalKey<ResizableState>("resizable");
        using var harness = new WidgetRenderHarness(new NotificationListener<SizeChangedLayoutNotification>(
            onNotification: _ =>
            {
                notifications++;
                return false;
            },
            child: new UnconstrainedBox(
                alignment: Alignment.TopLeft,
                child: new SizeChangedLayoutNotifier(
                    child: new ResizableWidget(key)))));
        var viewport = new Size(240, 120);

        harness.Pump(viewport);
        Assert.Equal(0, notifications);

        key.CurrentState!.Resize(80, 30);
        harness.Pump(viewport);
        Assert.Equal(1, notifications);

        key.CurrentState.Resize(80, 30);
        harness.Pump(viewport);
        Assert.Equal(1, notifications);

        key.CurrentState.Resize(60, 50);
        harness.Pump(viewport);
        Assert.Equal(2, notifications);
    }

    [Fact]
    public void DragRecognizer_ReportsAxisProjectedFlingVelocityAndHonorsDragStartBehavior()
    {
        Point? startPosition = null;
        Velocity? velocity = null;
        double? primaryVelocity = null;
        var recognizer = new HorizontalDragGestureRecognizer
        {
            DragStartBehavior = DragStartBehavior.Start,
            OnStart = details => startPosition = details.GlobalPosition,
            OnEnd = details =>
            {
                velocity = details.Velocity;
                primaryVelocity = details.PrimaryVelocity;
            },
        };

        try
        {
            var listener = new RenderPointerListener(
                onPointerDown: recognizer.AddPointer,
                behavior: HitTestBehavior.Opaque,
                child: new FixedHitTestBox(new Size(160, 80)));
            var renderView = BuildRenderView(listener, new Size(160, 80));
            DateTime start = new(2026, 7, 21, 14, 0, 0, DateTimeKind.Utc);

            GestureBinding.Instance.HandlePointerEvent(renderView, new PointerDownEvent(
                31,
                PointerDeviceKind.Mouse,
                new Point(10, 10),
                PointerButtons.Primary,
                start));
            GestureBinding.Instance.HandlePointerEvent(renderView, new PointerMoveEvent(
                31,
                PointerDeviceKind.Mouse,
                new Point(19, 11.5),
                PointerButtons.Primary,
                true,
                start.AddMilliseconds(30)));
            GestureBinding.Instance.HandlePointerEvent(renderView, new PointerMoveEvent(
                31,
                PointerDeviceKind.Mouse,
                new Point(28, 13),
                PointerButtons.Primary,
                true,
                start.AddMilliseconds(60)));
            GestureBinding.Instance.HandlePointerEvent(renderView, new PointerMoveEvent(
                31,
                PointerDeviceKind.Mouse,
                new Point(37, 14.5),
                PointerButtons.Primary,
                true,
                start.AddMilliseconds(90)));
            GestureBinding.Instance.HandlePointerEvent(renderView, new PointerUpEvent(
                31,
                PointerDeviceKind.Mouse,
                new Point(70, 20),
                PointerButtons.None,
                start.AddMilliseconds(100)));

            Assert.Equal(new Point(10, 10), startPosition);
            Assert.NotNull(velocity);

            // considerFling projects onto the recognizer's axis: the cross-axis component of the
            // estimate (50 px/s here) is dropped, and the primary velocity is the axis component.
            Assert.Equal(300.0, velocity.Value.PixelsPerSecond.X, precision: 3);
            Assert.Equal(0.0, velocity.Value.PixelsPerSecond.Y, precision: 3);
            Assert.Equal(300.0, primaryVelocity!.Value, precision: 3);
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    [Fact]
    public void AnimationController_FlingUsesFlutterSpringAndCompletesInBothDirections()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromMilliseconds(200));
        var terminalStatuses = new List<AnimationStatus>();
        controller.AddStatusListener(status =>
        {
            if (status is AnimationStatus.Completed or AnimationStatus.Dismissed)
            {
                terminalStatuses.Add(status);
            }
        });

        controller.SetValue(0.25);
        controller.Fling(1.0);
        PumpScheduler(1.0);

        Assert.Equal(1.0, controller.Value);
        Assert.Equal(AnimationStatus.Completed, controller.Status);
        Assert.False(controller.IsAnimating);

        controller.SetValue(0.75);
        controller.Fling(-1.0);
        PumpScheduler(1.0);

        Assert.Equal(0.0, controller.Value);
        Assert.Equal(AnimationStatus.Dismissed, controller.Status);
        Assert.False(controller.IsAnimating);
        Assert.Equal([AnimationStatus.Completed, AnimationStatus.Dismissed], terminalStatuses);
    }

    private static void DispatchDrag(
        RenderView renderView,
        int pointer,
        DateTime start,
        IReadOnlyList<Point> positions)
    {
        GestureBinding.Instance.HandlePointerEvent(renderView, new PointerDownEvent(
            pointer,
            PointerDeviceKind.Mouse,
            new Point(10, 20),
            PointerButtons.Primary,
            start));
        for (int index = 0; index < positions.Count; index++)
        {
            GestureBinding.Instance.HandlePointerEvent(renderView, new PointerMoveEvent(
                pointer,
                PointerDeviceKind.Mouse,
                positions[index],
                PointerButtons.Primary,
                true,
                start.AddMilliseconds((index + 1) * 150)));
        }
    }

    private void PumpAnimation(WidgetRenderHarness harness, Size viewport, double seconds)
    {
        _clock += 0.01;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(_clock));
        harness.Pump(viewport);
        _clock += seconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(_clock));
        harness.Pump(viewport);
    }

    private void PumpScheduler(double seconds)
    {
        _clock += 0.01;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(_clock));
        _clock += seconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(_clock));
    }

    private static RenderView BuildRenderView(RenderBox child, Size size)
    {
        var renderView = new RenderView { Child = child };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(size);
        return renderView;
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is T match)
        {
            result.Add(match);
        }

        root?.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private sealed class ResizableWidget(Key? key = null) : StatefulWidget(key)
    {
        public override State CreateState() => new ResizableState();
    }

    private sealed class ResizableState : State
    {
        private double _width = 40;
        private double _height = 20;

        public void Resize(double width, double height)
        {
            SetState(() =>
            {
                _width = width;
                _height = height;
            });
        }

        public override Widget Build(BuildContext context) => new SizedBox(width: _width, height: _height);
    }

    private sealed class FixedHitTestBox(Size desiredSize) : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(desiredSize);
        }

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext context, Point offset)
        {
        }
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

        public void Dispose() => _rootElement.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            internal override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            internal override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, Slot);
            }

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = (RenderBox)child;
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

            internal override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }
}
