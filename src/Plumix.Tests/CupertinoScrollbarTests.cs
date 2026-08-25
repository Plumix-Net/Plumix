using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoScrollbarTests
{
    private static readonly Color LightThumb = Color.FromArgb(0x59, 0x00, 0x00, 0x00);
    private static readonly Color DarkThumb = Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF);
    private static readonly Size ViewportSize = new(800, 600);
    private static readonly DateTime PressTime = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // Frame timestamps have to advance monotonically from where the scheduler's clock stands, or a
    // ticker started mid-frame (which anchors to `Scheduler.CurrentFrameTimeStamp`) never advances.
    private double _clock = Scheduler.CurrentSeconds;

    public CupertinoScrollbarTests()
    {
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void Defaults_MatchFlutter()
    {
        var scrollbar = new CupertinoScrollbar(child: new SizedBox());

        Assert.Equal(3, CupertinoScrollbar.DefaultThickness);
        Assert.Equal(8.0, CupertinoScrollbar.DefaultThicknessWhileDragging);
        Assert.Equal(1.5, CupertinoScrollbar.DefaultRadius);
        Assert.Equal(4.0, CupertinoScrollbar.DefaultRadiusWhileDragging);

        Assert.Equal(CupertinoScrollbar.DefaultThickness, scrollbar.Thickness);
        Assert.Equal(CupertinoScrollbar.DefaultThicknessWhileDragging, scrollbar.ThicknessWhileDragging);
        Assert.Equal(CupertinoScrollbar.DefaultRadius, scrollbar.Radius);
        Assert.Equal(CupertinoScrollbar.DefaultRadiusWhileDragging, scrollbar.RadiusWhileDragging);
        Assert.False(scrollbar.ThumbVisibility);
        Assert.Null(scrollbar.Controller);
        Assert.Null(scrollbar.Shape);
        Assert.Null(scrollbar.ThumbColor);
        Assert.Equal(TimeSpan.FromMilliseconds(250), scrollbar.FadeDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(1200), scrollbar.TimeToFade);
        Assert.Equal(TimeSpan.FromMilliseconds(100), scrollbar.PressDuration);
        Assert.Equal(3, scrollbar.MainAxisMargin);
        Assert.Null(scrollbar.ScrollbarOrientation);
        Assert.Null(scrollbar.Padding);
        Assert.Equal((ScrollNotificationPredicate)RawScrollbar.DefaultScrollNotificationPredicate,
            scrollbar.NotificationPredicate);

        // Dart leaves the thumb-length constants to `updateScrollbarPainter`, not the widget: the
        // widget keeps `RawScrollbar`'s own defaults.
        Assert.Equal(RawScrollbar.KMinThumbExtent, scrollbar.MinThumbLength);
        Assert.Null(scrollbar.MinOverscrollLength);
        Assert.Equal(0, scrollbar.CrossAxisMargin);
    }

    [Fact]
    public void IsARawScrollbarAndValidatesDraggingMetrics()
    {
        Assert.IsAssignableFrom<RawScrollbar>(new CupertinoScrollbar(child: new SizedBox()));

        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoScrollbar(
            child: new SizedBox(),
            thicknessWhileDragging: double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoScrollbar(
            child: new SizedBox(),
            radiusWhileDragging: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoScrollbar(
            child: new SizedBox(),
            thickness: double.PositiveInfinity));
    }

    [Fact]
    public void CustomNotificationPredicateAndOrientationSurviveToTheBaseWidget()
    {
        bool Predicate(ScrollNotification notification) => true;
        var scrollbar = new CupertinoScrollbar(
            child: new SizedBox(),
            notificationPredicate: Predicate,
            scrollbarOrientation: ScrollbarOrientation.Left,
            mainAxisMargin: 9);

        Assert.Equal(ScrollbarOrientation.Left, scrollbar.ScrollbarOrientation);
        Assert.Equal(9, scrollbar.MainAxisMargin);
        Assert.Equal((ScrollNotificationPredicate)Predicate, scrollbar.NotificationPredicate);
    }

    // Flutter: "Tapping the track area pages the Scroll View except on iOS" — the initial rrect.
    [Fact]
    public void DefaultVerticalGeometryMatchesFlutter()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(BuildScrollbar(controller, contentExtent: 1000));
        Settle(harness);

        ScrollbarPainter painter = RequirePainter(harness);
        ScrollbarGeometry geometry = painter.Geometry!.Value;
        Assert.Equal(Axis.Vertical, geometry.Axis);
        Assert.Equal(3, painter.Thickness);
        // Thumb: x = 800 - crossAxisMargin(3) - thickness(3); y = mainAxisMargin(3);
        // extent = (600 - 2 * 3) * 600 / 1000.
        Assert.Equal(794, geometry.ThumbRect.X, precision: 3);
        Assert.Equal(3, geometry.ThumbRect.Y, precision: 3);
        Assert.Equal(3, geometry.ThumbRect.Width, precision: 3);
        Assert.Equal(356.4, geometry.ThumbRect.Height, precision: 3);
        // The track rect spans the whole padded viewport, thickness + 2 * crossAxisMargin wide.
        Assert.Equal(791, geometry.TrackRect.X, precision: 3);
        Assert.Equal(0, geometry.TrackRect.Y, precision: 3);
        Assert.Equal(9, geometry.TrackRect.Width, precision: 3);
        Assert.Equal(600, geometry.TrackRect.Height, precision: 3);
    }

    // Flutter: "CupertinoScrollbar scrollOrientation works correctly".
    [Fact]
    public void LeftScrollbarOrientationMatchesFlutter()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(BuildScrollbar(
            controller,
            contentExtent: 4000,
            scrollbarOrientation: ScrollbarOrientation.Left));
        Settle(harness);

        ScrollbarGeometry geometry = RequirePainter(harness).Geometry!.Value;
        Assert.Equal(new Rect(0, 0, 9, 600), geometry.TrackRect);
        Assert.Equal(3, geometry.ThumbRect.X, precision: 3);
        Assert.Equal(3, geometry.ThumbRect.Y, precision: 3);
        Assert.Equal(3, geometry.ThumbRect.Width, precision: 3);
        Assert.Equal(89.1, geometry.ThumbRect.Height, precision: 3);
    }

    // Flutter: "Scrollbar dark mode".
    [Theory]
    [InlineData(PlatformBrightness.Light)]
    [InlineData(PlatformBrightness.Dark)]
    public void ThumbColorResolvesTheDynamicScrollbarColor(PlatformBrightness brightness)
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(BuildScrollbar(
            controller,
            contentExtent: 1000,
            brightness: brightness));
        Settle(harness);

        Color expected = brightness == PlatformBrightness.Dark ? DarkThumb : LightThumb;
        Assert.Equal(expected, RequirePainter(harness).Color);
    }

    [Fact]
    public void ThumbColorFollowsAnEnclosingCupertinoThemeOverTheMediaQuery()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: PlatformBrightness.Light),
            child: new CupertinoTheme(
                data: new CupertinoThemeData(brightness: PlatformBrightness.Dark),
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new CupertinoScrollbar(
                        controller: controller,
                        thumbVisibility: true,
                        child: BuildScrollable(controller, 1000))))));
        Settle(harness);

        Assert.Equal(DarkThumb, RequirePainter(harness).Color);
    }

    // Flutter: "On first render with thumbVisibility: false, the thumb is hidden".
    [Fact]
    public void ThumbIsHiddenUntilScrolledWhenThumbVisibilityIsFalse()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(BuildScrollbar(
            controller,
            contentExtent: 1000,
            thumbVisibility: false));
        Settle(harness);

        Assert.Equal(0, RequirePainter(harness).FadeoutOpacityAnimation.Value);
    }

    // Flutter: "Scrollbar thumb can be dragged with long press" — the thumb drag engages on pointer
    // down (`dragStartBehavior: down`, `touchSlop: 0`), and the haptic fires when the 100 ms resize
    // animation completes, not when the press starts.
    [Fact]
    public void DraggingAnimatesThicknessAndRadiusLinearlyOverTheResizeDuration()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        using var harness = new WidgetRenderHarness(BuildScrollbar(controller, contentExtent: 1000));
        Settle(harness);
        Assert.Equal(3, RequirePainter(harness).Thickness);

        PressThumb(harness);

        // t = 0 of the resize animation: still the idle thickness and radius, and no haptic yet.
        Assert.Equal(3, RequirePainter(harness).Thickness, precision: 3);
        Assert.Equal(1.5, RequirePainter(harness).Radius!.Value, precision: 3);
        Assert.Empty(platform.Log);

        AdvanceAndPump(harness, 0.05);
        Assert.Equal(5.5, RequirePainter(harness).Thickness, precision: 3);
        Assert.Equal(2.75, RequirePainter(harness).Radius!.Value, precision: 3);
        Assert.Empty(platform.Log);

        AdvanceAndPump(harness, 0.06);
        Assert.Equal(8, RequirePainter(harness).Thickness, precision: 3);
        Assert.Equal(4, RequirePainter(harness).Radius!.Value, precision: 3);

        MethodCall call = Assert.Single(platform.Log);
        Assert.Equal("HapticFeedback.vibrate", call.Method);
        Assert.Equal("HapticFeedbackType.mediumImpact", call.Arguments);

        // The dragged thumb sits `thicknessWhileDragging` wide against the right edge.
        ScrollbarGeometry geometry = RequirePainter(harness).Geometry!.Value;
        Assert.Equal(789, geometry.ThumbRect.X, precision: 3);
        Assert.Equal(8, geometry.ThumbRect.Width, precision: 3);
    }

    // Flutter: "Scrollbar thumb can be dragged with long press" — the drag moves the scroll view and
    // a slow release buzzes a second time.
    [Fact]
    public void ReleasingTheThumbShrinksItBackAndBuzzesWhenItMoved()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        using var harness = new WidgetRenderHarness(BuildScrollbar(controller, contentExtent: 1000));
        Settle(harness);

        Point start = PressThumb(harness);
        AdvanceAndPump(harness, 0.11);
        Assert.Single(platform.Log);

        var moved = new Point(start.X, start.Y + 40);
        DateTime moveTime = PressTime.AddSeconds(5);
        Dispatch(harness, new PointerMoveEvent(
            71, PointerDeviceKind.Touch, moved, PointerButtons.Primary, true, moveTime));
        Assert.True(controller.PrimaryPosition!.Pixels > 0);

        Dispatch(harness, new PointerUpEvent(
            71, PointerDeviceKind.Touch, moved, PointerButtons.None, moveTime));

        // A slow release buzzes a second time; the thumb then shrinks back to the idle thickness.
        Assert.Equal(2, platform.Log.Count);

        AdvanceAndPump(harness, 0.11);
        Assert.Equal(3, RequirePainter(harness).Thickness, precision: 3);
        Assert.Equal(1.5, RequirePainter(harness).Radius!.Value, precision: 3);
    }

    // Flutter: "Scrollbar thumb can be dragged with long press - horizontal axis".
    [Fact]
    public void HorizontalThumbCanBeDragged()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new MediaQuery(
            data: new MediaQueryData(),
            child: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new ScrollConfiguration(
                    behavior: new TestScrollBehavior(TargetPlatform.IOS),
                    child: new CupertinoScrollbar(
                        controller: controller,
                        thumbVisibility: true,
                        child: new SingleChildScrollView(
                            controller: controller,
                            scrollDirection: Axis.Horizontal,
                            child: new SizedBox(width: 4000, height: 600)))))));
        Settle(harness);

        ScrollbarGeometry geometry = RequirePainter(harness).Geometry!.Value;
        Assert.Equal(Axis.Horizontal, geometry.Axis);

        Point start = geometry.ThumbRect.Center;
        Dispatch(harness, new PointerDownEvent(
            81, PointerDeviceKind.Touch, start, PointerButtons.Primary, PressTime));
        var moved = new Point(start.X + 20, start.Y);
        Dispatch(harness, new PointerMoveEvent(
            81, PointerDeviceKind.Touch, moved, PointerButtons.Primary, true, PressTime.AddMilliseconds(20)));

        Assert.True(controller.PrimaryPosition!.Pixels > 20);
    }

    // Flutter: "Tapping the track area does not page the Scroll View on iOS".
    [Fact]
    public void TappingTheTrackDoesNotPageOnIos()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(BuildScrollbar(
            controller,
            contentExtent: 1000,
            platform: TargetPlatform.IOS));
        Settle(harness);

        TapTrackBelowThumb(harness);
        AdvanceAndPump(harness, 0.2);

        Assert.Equal(0, controller.PrimaryPosition!.Pixels);
    }

    // Flutter: "Tapping the track area pages the Scroll View except on iOS".
    [Fact]
    public void TappingTheTrackPagesOnOtherPlatforms()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(BuildScrollbar(
            controller,
            contentExtent: 1000,
            platform: TargetPlatform.Android));
        Settle(harness);

        TapTrackBelowThumb(harness);
        AdvanceAndPump(harness, 0.2);

        Assert.Equal(400, controller.PrimaryPosition!.Pixels, precision: 3);
    }

    // Flutter: "CupertinoScrollbar does not crash at zero area".
    [Fact]
    public void ZeroAreaDoesNotPaintOrCrash()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(BuildScrollbar(controller, contentExtent: 1000));

        harness.Pump(new Size(0, 0));
        Scheduler.FlushMicrotasks();
        harness.Pump(new Size(0, 0));

        Assert.Null(RequirePainter(harness).Geometry);
    }

    private Point PressThumb(WidgetRenderHarness harness)
    {
        Point point = RequirePainter(harness).Geometry!.Value.ThumbRect.Center;
        Dispatch(harness, new PointerDownEvent(
            71, PointerDeviceKind.Touch, point, PointerButtons.Primary, PressTime));
        harness.Pump(ViewportSize);
        return point;
    }

    private void TapTrackBelowThumb(WidgetRenderHarness harness)
    {
        Rect thumb = RequirePainter(harness).Geometry!.Value.ThumbRect;
        var point = new Point(thumb.Center.X, thumb.Bottom + 100);
        Dispatch(harness, new PointerDownEvent(
            72, PointerDeviceKind.Touch, point, PointerButtons.Primary, PressTime));
        Dispatch(harness, new PointerUpEvent(
            72, PointerDeviceKind.Touch, point, PointerButtons.None, PressTime.AddMilliseconds(10)));
    }

    private static void Dispatch(WidgetRenderHarness harness, PointerEvent @event) =>
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, @event);

    /// <summary>
    /// Lays out and paints, delivers the queued <c>ScrollMetricsNotification</c> (which is what gives
    /// the scrollbar its axis and starts the fade-in), then runs the fade to completion.
    /// </summary>
    private void Settle(WidgetRenderHarness harness)
    {
        harness.Pump(ViewportSize);
        Scheduler.FlushMicrotasks();
        harness.Pump(ViewportSize);
        AdvanceAndPump(harness, 0.4);
    }

    private void AdvanceAndPump(WidgetRenderHarness harness, double seconds)
    {
        // Prime any freshly started ticker, then advance the frame clock.
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(_clock));
        _clock += seconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(_clock));
        Scheduler.FlushMicrotasks();
        harness.Pump(ViewportSize);
    }

    private static ScrollbarPainter RequirePainter(WidgetRenderHarness harness)
    {
        var paint = Assert.IsType<CustomPaint>(FindWidgetObject<CustomPaint>(harness.RootElement));
        return Assert.IsType<ScrollbarPainter>(paint.ForegroundPainter);
    }

    private static Widget BuildScrollbar(
        ScrollController controller,
        double contentExtent,
        bool thumbVisibility = true,
        PlatformBrightness brightness = PlatformBrightness.Light,
        ScrollbarOrientation? scrollbarOrientation = null,
        TargetPlatform platform = TargetPlatform.IOS)
    {
        return new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: brightness),
            child: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new ScrollConfiguration(
                    behavior: new TestScrollBehavior(platform),
                    child: new CupertinoScrollbar(
                        controller: controller,
                        thumbVisibility: thumbVisibility,
                        scrollbarOrientation: scrollbarOrientation,
                        child: BuildScrollable(controller, contentExtent)))));
    }

    private static Widget BuildScrollable(ScrollController controller, double contentExtent) =>
        new SingleChildScrollView(
            controller: controller,
            child: new SizedBox(width: 800, height: contentExtent));

    private static Widget? FindWidgetObject<T>(Element? element) where T : Widget
    {
        if (element is null) return null;
        if (element.Widget is T match) return match;
        Widget? result = null;
        element.VisitChildren(child => result ??= FindWidgetObject<T>(child));
        return result;
    }

    private sealed class TestScrollBehavior : ScrollBehavior
    {
        private readonly TargetPlatform _platform;

        public TestScrollBehavior(TargetPlatform platform) => _platform = platform;

        public override TargetPlatform GetPlatform(BuildContext context) => _platform;
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _root;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new HarnessRootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }
        public Element RootElement => _root;

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void Dispose() => _root.Unmount();
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
        internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
        internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }

        internal override void Unmount()
        {
            if (_child is not null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null;
        }
    }
}
