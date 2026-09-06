using Avalonia;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/test/widgets/scroll_behavior_test.dart;
// flutter/packages/flutter/test/widgets/scrollable_test.dart; flutter/packages/flutter/test/gestures/velocity_tracker_test.dart

namespace Plumix.Tests;

public sealed class ScrollBehaviorParityTests
{
    [Fact]
    public void ScrollBehavior_VelocityTrackerBuilderUsesPlatformAndSurvivesCopyWith()
    {
        var context = new SizedBox().CreateElement();
        var ios = new FixedPlatformScrollBehavior(TargetPlatform.IOS);
        var macos = new FixedPlatformScrollBehavior(TargetPlatform.MacOS);
        var android = new FixedPlatformScrollBehavior(TargetPlatform.Android);
        PointerDownEvent pointer = PointerDown();

        Assert.IsType<IOSScrollViewFlingVelocityTracker>(ios.VelocityTrackerBuilder(context)(pointer));
        Assert.IsType<MacOSScrollViewFlingVelocityTracker>(macos.VelocityTrackerBuilder(context)(pointer));
        Assert.IsType<VelocityTracker>(android.VelocityTrackerBuilder(context)(pointer));
        Assert.IsType<IOSScrollViewFlingVelocityTracker>(
            ios.CopyWith(scrollbars: false).VelocityTrackerBuilder(context)(pointer));
    }

    [Fact]
    public void VelocityTracker_EstimatesContinuousMotionAndVelocityValueMatchesFlutterContracts()
    {
        var tracker = new VelocityTracker(PointerDeviceKind.Touch);
        DateTime start = new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);
        tracker.AddPosition(start, new Point(0, 0));
        tracker.AddPosition(start.AddMilliseconds(10), new Point(6, -3));
        tracker.AddPosition(start.AddMilliseconds(20), new Point(12, -6));

        Velocity velocity = tracker.GetVelocity();
        Assert.Equal(600, velocity.PixelsPerSecond.X, precision: 6);
        Assert.Equal(-300, velocity.PixelsPerSecond.Y, precision: 6);
        Assert.Equal(Velocity.Zero, new VelocityTracker(PointerDeviceKind.Touch).GetVelocity());

        var horizontal = new Velocity(new Vector(7, 0));
        var second = new Velocity(new Vector(12, 0));
        Assert.Equal(new Vector(19, 0), (horizontal + second).PixelsPerSecond);
        Assert.Equal(new Vector(5, 0), (second - horizontal).PixelsPerSecond);
        Assert.Equal(new Vector(-7, 0), (-horizontal).PixelsPerSecond);
        Assert.Equal(5, horizontal.ClampMagnitude(0, 5).PixelsPerSecond.Length, precision: 6);
    }

    [Fact]
    public void PlatformVelocityTrackersUseFlutterWeightsAndValidateSamples()
    {
        DateTime start = new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);
        var ios = new IOSScrollViewFlingVelocityTracker(PointerDeviceKind.Touch);
        var macos = new MacOSScrollViewFlingVelocityTracker(PointerDeviceKind.Touch);
        foreach (VelocityTracker tracker in new VelocityTracker[] { ios, macos })
        {
            tracker.AddPosition(start, new Point(0, 0));
            tracker.AddPosition(start.AddMilliseconds(10), new Point(10, 0));
            tracker.AddPosition(start.AddMilliseconds(20), new Point(30, 0));
            tracker.AddPosition(start.AddMilliseconds(30), new Point(60, 0));
        }

        Assert.Equal(1450, ios.GetVelocity().PixelsPerSecond.X, precision: 6);
        Assert.Equal(2050, macos.GetVelocity().PixelsPerSecond.X, precision: 6);
        Assert.Throws<InvalidOperationException>(
            () => new IOSScrollViewFlingVelocityTracker(PointerDeviceKind.Touch).GetVelocity());
        Assert.Throws<ArgumentException>(() => ios.AddPosition(start, new Point(0, 0)));
    }

    [Fact]
    public void VelocityTrackersAssumeZeroAfterMotionStops()
    {
        DateTime start = new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);
        VelocityTracker[] trackers =
        [
            new VelocityTracker(PointerDeviceKind.Touch),
            new IOSScrollViewFlingVelocityTracker(PointerDeviceKind.Touch),
            new MacOSScrollViewFlingVelocityTracker(PointerDeviceKind.Touch),
        ];
        foreach (VelocityTracker tracker in trackers)
        {
            tracker.AddPosition(start, new Point(0, 0));
            tracker.AddPosition(start.AddMilliseconds(10), new Point(1, 0));
            tracker.AddPosition(start.AddMilliseconds(20), new Point(2, 0));
            tracker.AddPosition(start.AddMilliseconds(30), new Point(3, 0));
        }

        Thread.Sleep(50);

        Assert.All(trackers, tracker => Assert.Equal(Velocity.Zero, tracker.GetVelocity()));
    }

    [Fact]
    public void Scrollable_DefaultShiftFlipsOnlyMouseWheelAndReportsAcceptance()
    {
        using var controller = new ScrollController();
        using var harness = BuildHorizontalHarness(controller, new ScrollBehavior().CopyWith(scrollbars: false));
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        HardwareKeyboard.Instance.ClearState();
        harness.Pump(new Size(200, 100));

        bool? allowPlatformDefault = null;
        DispatchScroll(binding, harness.RenderView, PointerDeviceKind.Mouse, allow => allowPlatformDefault = allow);
        Assert.Equal(0, controller.Offset, precision: 6);
        Assert.True(allowPlatformDefault);

        FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ShiftLeft, shift: true));
        DispatchScroll(binding, harness.RenderView, PointerDeviceKind.Mouse, allow => allowPlatformDefault = allow);
        Assert.Equal(20, controller.Offset, precision: 6);
        Assert.False(allowPlatformDefault);

        DispatchScroll(binding, harness.RenderView, PointerDeviceKind.Trackpad);
        Assert.Equal(20, controller.Offset, precision: 6);

        FocusManager.Instance.HandleKeyEvent(KeySim.Up(LogicalKeyboardKey.ShiftLeft));
        DispatchScroll(binding, harness.RenderView, PointerDeviceKind.Mouse);
        Assert.Equal(20, controller.Offset, precision: 6);
        binding.ResetForTests();
        HardwareKeyboard.Instance.ClearState();
    }

    [Fact]
    public void Scrollable_CustomModifierFlipsWithOtherKeysPressedAndUsesBehaviorTracker()
    {
        int trackerCreations = 0;
        var behavior = new CustomTrackerScrollBehavior(() => trackerCreations += 1).CopyWith(
            scrollbars: false,
            pointerAxisModifiers: new HashSet<LogicalKeyboardKey> { LogicalKeyboardKey.AltLeft });
        using var controller = new ScrollController();
        using var harness = BuildHorizontalHarness(controller, behavior);
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        HardwareKeyboard.Instance.ClearState();
        harness.Pump(new Size(200, 100));

        FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.AltLeft, alt: true));
        FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Space, alt: true));
        DispatchScroll(binding, harness.RenderView, PointerDeviceKind.Mouse);
        Assert.Equal(20, controller.Offset, precision: 6);

        DateTime now = DateTime.UtcNow;
        binding.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            2, PointerDeviceKind.Touch, new Point(50, 50), PointerButtons.Primary, now));
        Assert.Equal(1, trackerCreations);
        binding.HandlePointerEvent(harness.RenderView, new PointerCancelEvent(
            2, PointerDeviceKind.Touch, new Point(50, 50), PointerButtons.None, now.AddMilliseconds(1)));

        FocusManager.Instance.HandleKeyEvent(KeySim.Up(LogicalKeyboardKey.AltLeft));
        FocusManager.Instance.HandleKeyEvent(KeySim.Up(LogicalKeyboardKey.Space));
        binding.ResetForTests();
        HardwareKeyboard.Instance.ClearState();
    }

    private static WidgetRenderHarness BuildHorizontalHarness(
        ScrollController controller,
        ScrollBehavior behavior)
    {
        return new WidgetRenderHarness(
            new Directionality(
                TextDirection.Ltr,
                new ScrollConfiguration(
                    behavior,
                    new SingleChildScrollView(
                        controller: controller,
                        scrollDirection: Axis.Horizontal,
                        child: new SizedBox(width: 1000, height: 100)))));
    }

    private static void DispatchScroll(
        GestureBinding binding,
        RenderView renderView,
        PointerDeviceKind kind,
        Action<bool>? onRespond = null)
    {
        binding.HandlePointerEvent(renderView, new PointerScrollEvent(
            pointer: 1,
            kind: kind,
            position: new Point(50, 50),
            buttons: PointerButtons.None,
            scrollDelta: new Point(0, 20),
            timestampUtc: DateTime.UtcNow,
            onRespond: onRespond));
    }

    private static PointerDownEvent PointerDown()
    {
        return new PointerDownEvent(
            pointer: 1,
            kind: PointerDeviceKind.Touch,
            position: default,
            buttons: PointerButtons.Primary,
            timestampUtc: DateTime.UtcNow);
    }

    private sealed class FixedPlatformScrollBehavior(TargetPlatform platform) : ScrollBehavior
    {
        public override TargetPlatform GetPlatform(BuildContext context) => platform;
    }

    private sealed class CustomTrackerScrollBehavior(Action onCreate) : ScrollBehavior
    {
        public override GestureVelocityTrackerBuilder VelocityTrackerBuilder(BuildContext context)
        {
            return @event =>
            {
                onCreate();
                return new VelocityTracker(@event.Kind);
            };
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly PipelineOwner _pipeline;
        private readonly RootElement _root;

        public WidgetRenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new RootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
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

        public void Dispose() => _root.Unmount();

        private sealed class RootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public RootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            public override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            protected override void PerformRebuild()
            {
                base.PerformRebuild();
                _child = UpdateChild(_child, Widget, Slot);
            }

            public override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild(force: true);
            }

            public override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            public override void VisitChildren(Action<Element> visitor)
            {
                if (_child != null)
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

            public override void Unmount()
            {
                if (_child != null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }
}
